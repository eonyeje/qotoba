using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>
/// What the Host reports about a checkout: + comments with their anchors, public types, the branch and its commits.
/// The Workbench reads + comments and never edits them; a comment is handled only by a //+= line a session wrote.
/// </summary>
public sealed partial class WorkbenchOps
{
    public async Task IndexAsync(CommentIndexPost post, CancellationToken ct = default)
    {
        var machine = await db.Machines.FindAsync([post.Machine], ct);
        var item = post.Item ?? (machine is null ? null : await ItemForCwdAsync(machine, post.Checkout, ct))
                   ?? await db.Branches.Where(b => b.Name == post.Branch && b.ItemId != null).Select(b => b.ItemId).FirstOrDefaultAsync(ct)
                   ?? ItemFromBranchName(post.Branch);
        var project = item is null ? null : (await db.Items.FindAsync([item], ct))?.ProjectId;
        var repo = Path.GetFileName(post.Checkout.TrimEnd('\\', '/'));
        var seenIds = new HashSet<string>();

        foreach (var f in post.Files)
        {
            var id = await FileIdAsync(f.Path, ct);
            seenIds.Add(id);
            var file = await db.Files.FindAsync([id], ct);
            var isNew = file is null;
            if (file is null) { file = new FileSeen { Id = id, Path = f.Path, AddedAt = Now }; db.Files.Add(file); }
            file.Path = f.Path; file.Repo = repo; file.Folder = Folder(f.Path); file.Branch = post.Branch; file.MachineId = post.Machine; file.Checkout = post.Checkout;
            file.ItemId ??= item; file.ProjectId ??= project; file.IndexedAt = Now;
            file.LinesJson = Json.Write(f.Lines.Take(400).ToList());
            file.DiffJson = Json.Write(f.DiffFromMain.Take(400).ToList());
            var changed = file.ContentHash != f.Hash;
            file.ContentHash = f.Hash;
            if (isNew || (changed && file.SeenHash != f.Hash))
            {
                file.Seen = false;
                file.NewLines = f.DiffFromMain.Count(l => l.StartsWith('+') && !l.StartsWith("+++"));
                if (file.NewLines == 0) file.NewLines = f.LineCount;
            }
            await UpsertCommentsAsync(file, f, item, post, ct);
        }

        // comments in files the Host no longer reports: their lines are gone
        var gone = await db.Comments.Where(c => c.State == CommentState.Open && !seenIds.Contains(c.FileId)).Join(db.Files.Where(x => x.MachineId == post.Machine && x.Checkout == post.Checkout), c => c.FileId, x => x.Id, (c, _) => c).ToListAsync(ct);
        foreach (var c in gone) c.State = CommentState.Removed;

        foreach (var t in post.Types)
        {
            var fid = await FileIdAsync(t.Path, ct);
            var at = $"file:{fid}:{t.From}-{t.To}";
            var row = await db.Types.FindAsync([t.Name], ct);
            if (row is null) { row = new TypeSeen { Id = t.Name, Seen = false }; db.Types.Add(row); }
            else if (row.At != at) row.Seen = false;
            row.Kind = t.Kind; row.Ns = t.Namespace; row.At = at; row.Summary = t.Summary; row.ItemId ??= item;
        }

        if (post.BranchState is { } b && b.Name != "main" && b.Name != "master")
        {
            var bid = "br-" + Ids.Hash(post.Checkout + "|" + b.Name)[..8];
            var br = await db.Branches.FindAsync([bid], ct);
            if (br is null) { br = new Branch { Id = bid, Name = b.Name }; db.Branches.Add(br); }
            br.Name = b.Name; br.ItemId = item; br.MachineId = post.Machine; br.Ahead = b.Ahead; br.Behind = b.Behind; br.Pushed = b.Pushed; br.Dirty = b.Dirty;
            br.Checkout = post.Checkout; br.ReportedAt = Now; br.Worktree = post.Checkout;
            br.State = b.MergedInto is not null ? "merged" : b.Dirty ? "local · uncommitted changes" : b.Pushed ? "pushed" : "local";
            br.MergedCommit = b.MergedInto;
            br.CommitsJson = Json.Write(post.Commits.Select(c => "commit:" + c.Id).ToList());
            foreach (var c in post.Commits)
            {
                var row = await db.Commits.FindAsync([c.Id], ct);
                if (row is null) { row = new Commit { Id = c.Id, Msg = c.Message, Date = c.Date }; db.Commits.Add(row); }
                row.Msg = c.Message; row.Date = c.Date; row.BranchId = bid; row.FilesJson = Json.Write(c.Files); row.Plus = c.Plus; row.Minus = c.Minus;
                row.SessionId ??= await db.Sessions.Where(s => s.MachineId == post.Machine && s.Started <= c.Date && (s.Ended == null || s.Ended >= c.Date)).OrderByDescending(s => s.Started).Select(s => s.Id).FirstOrDefaultAsync(ct);
            }
        }
        if (machine is not null) { machine.LastSeen = Now; machine.Online = true; }
        await SaveAsync("files", post.Checkout, ct);
    }

    private async Task UpsertCommentsAsync(FileSeen file, IndexedFile f, string? item, CommentIndexPost post, CancellationToken ct)
    {
        var existing = await db.Comments.Where(c => c.FileId == file.Id).ToListAsync(ct);
        var matched = new HashSet<string>();
        var latestCommit = post.Commits.Where(c => c.Files.Any(p => p.Replace('\\', '/').EndsWith(f.Path.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))).OrderByDescending(c => c.Date).FirstOrDefault();

        foreach (var ic in f.Comments.Where(c => !c.Proof))
        {
            var hit = existing.FirstOrDefault(c => !matched.Contains(c.Id) && c.TextHash == ic.Hash && c.Symbol == ic.Symbol)
                   ?? existing.FirstOrDefault(c => !matched.Contains(c.Id) && c.Symbol == ic.Symbol && c.State != CommentState.Removed);
            if (hit is null)
            {
                hit = new Comment { Id = "c-" + Ids.Short(), FileId = file.Id, Line = ic.Line, Symbol = ic.Symbol, SymbolKind = ic.SymbolKind, Text = ic.Text, TextHash = ic.Hash, State = CommentState.Open, ItemId = item, LastSeenInIndex = Now };
                hit.VersionsJson = Json.Write(new List<CommentVersion> { new(fmt.DayTime(Now), ic.Text, null) });
                db.Comments.Add(hit); existing.Add(hit);
            }
            else
            {
                if (hit.TextHash != ic.Hash)
                {
                    var v = Json.ReadList<CommentVersion>(hit.VersionsJson);
                    v.Add(new CommentVersion(fmt.DayTime(Now), ic.Text, hit.Text));
                    hit.VersionsJson = Json.Write(v); hit.Text = ic.Text; hit.TextHash = ic.Hash;
                    if (hit.State == CommentState.Handled) { hit.State = CommentState.Open; hit.StaleWhy = null; }
                }
                hit.Line = ic.Line; hit.SymbolKind = ic.SymbolKind; hit.LastSeenInIndex = Now;
                if (hit.State == CommentState.Removed) hit.State = CommentState.Open;
            }
            matched.Add(hit.Id);
        }
        foreach (var proof in f.Comments.Where(c => c.Proof))
        {
            var target = existing.Where(c => c.Symbol == proof.Symbol && c.State != CommentState.Removed).OrderBy(c => Math.Abs(c.Line - proof.Line)).FirstOrDefault();
            if (target is null) continue;
            matched.Add(target.Id);
            if (target.State != CommentState.Handled)
            {
                target.State = CommentState.Handled; target.ProofSessionId = proof.ProofSession;
                target.HandledJson = Json.Write(new HandledInfo(fmt.Day(Now), fmt.Time(Now), proof.ProofSession, proof.Text, latestCommit is null ? null : "commit:" + latestCommit.Id));
                if (latestCommit is not null && await db.Commits.FindAsync([latestCommit.Id], ct) is { } cm) cm.ProvesCommentId = target.Id;
            }
        }
        foreach (var c in existing.Where(c => !matched.Contains(c.Id)))
        {
            if (c.State == CommentState.Open) c.State = CommentState.Removed;
            else if (c.State == CommentState.Handled && c.LastSeenInIndex is not null) { c.State = CommentState.Stale; c.StaleWhy = "the + comment is gone but its //+= proof line is not there either"; }
        }
        foreach (var c in existing.Where(c => c.State == CommentState.Handled && matched.Contains(c.Id)))
            if (file.ContentHash != "" && c.HandledJson is not null && latestCommit is not null && Json.Read<HandledInfo?>(c.HandledJson, null) is { } h && h.Proof is not null && h.Proof != "commit:" + latestCommit.Id)
            { c.State = CommentState.Stale; c.StaleWhy = $"the code under it changed in {latestCommit.Id} after it was handled"; if (await db.Commits.FindAsync([latestCommit.Id], ct) is { } cm2) cm2.StalesCommentId = c.Id; }
    }

    public async Task MarkSeenAsync(string? fileId, string? typeId, CancellationToken ct = default)
    {
        if (fileId is not null && await db.Files.FindAsync([fileId], ct) is { } f) { f.Seen = true; f.NewLines = 0; f.LastSeenAt = Now; f.SeenHash = f.ContentHash; }
        if (typeId is not null && await db.Types.FindAsync([typeId], ct) is { } t) t.Seen = true;
        Audit(Actor.Owner, "seen", fileId is not null ? "file" : "type", fileId ?? typeId ?? "");
        await SaveAsync("seen", fileId ?? typeId ?? "", ct);
    }

    private async Task<string> FileIdAsync(string path, CancellationToken ct)
    {
        var stem = Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Replace(' ', '-');
        var norm = path.Replace('\\', '/');
        var same = await db.Files.Where(f => f.Id == stem || f.Id.StartsWith(stem + "-")).ToListAsync(ct);
        var mine = same.FirstOrDefault(f => f.Path.Replace('\\', '/').Equals(norm, StringComparison.OrdinalIgnoreCase));
        if (mine is not null) return mine.Id;
        if (same.All(f => f.Id != stem)) return stem;
        return stem + "-" + Ids.Hash(norm)[..4];
    }

    private static string Folder(string path)
    {
        var parts = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[^2] : "";
    }

    private static string? ItemFromBranchName(string branch)
    {
        var m = System.Text.RegularExpressions.Regex.Match(branch, @"^[a-z]+/([0-9]+(?:\.[0-9]+)*)-", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? null : null; // the code can only say the number; the project code comes from the checkout, so the Host passes the item when it knows it
    }
}
