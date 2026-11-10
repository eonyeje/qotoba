using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>
/// Documents belong to an item or a project. The file lives in the project's repository; the Workbench stores the project and
/// the repo-relative path, never a machine path and never a copy, and works out where to read it from each time: a checkout
/// on a Host that is online, or the repository's remote when none is. The ref is the node's branch while it is open, main after.
/// </summary>
public sealed partial class WorkbenchOps
{
    /// <summary>The node a document op names: an item id, a project code, or (empty) the session's own item or project.</summary>
    private async Task<(Project project, Item? item)> DocNodeAsync(Session s, string? node, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(node))
        {
            if (s.ItemId is not null) { var mine = await RequireItemAsync(s.ItemId, ct); return (await ProjectOfAsync(mine, ct), mine); }
            if (s.ProjectId is not null) { var mp = await db.Projects.FindAsync([s.ProjectId], ct); if (mp is not null) return (mp, null); }
            throw new WorkbenchRuleException("doc.node", "Say which item or project the document belongs to.");
        }
        var it = await db.Items.FindAsync([node.Trim()], ct);
        if (it is not null) return (await ProjectOfAsync(it, ct), it);
        var p = await FindProjectAsync(node.Trim(), ct) ?? throw new WorkbenchRuleException("doc.node", $"There is no item or project {node}.");
        return (p, null);
    }

    /// <summary>"docs/design/qdl.html": forward slashes, no leading "./" or "/", no "..".</summary>
    public static string NormalisePath(string path)
    {
        var p = path.Trim().Replace('\\', '/');
        while (p.StartsWith("./")) p = p[2..];
        p = p.TrimStart('/');
        if (p.Length == 0 || p.Split('/').Any(seg => seg == "..")) throw new WorkbenchRuleException("doc.path", "The path is relative to the repository root, with no '..'.");
        return p;
    }

    /// <summary>Record a document on a node. The same title or the same path on that node updates the entry (a rename or a move).</summary>
    public async Task<string> DocAddAsync(string sessionId, string? node, string title, string? path, string? url, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var (p, it) = await DocNodeAsync(s, node, ct);
        title = title.Trim();
        if (title.Length == 0) throw new WorkbenchRuleException("doc.title", "A document needs a title.");
        var hasPath = !string.IsNullOrWhiteSpace(path); var hasUrl = !string.IsNullOrWhiteSpace(url);
        if (hasPath == hasUrl) throw new WorkbenchRuleException("doc.where", "Give either a repo-relative path or a URL, not both and not neither.");
        if (hasUrl && !Uri.TryCreate(url!.Trim(), UriKind.Absolute, out _)) throw new WorkbenchRuleException("doc.url", "The URL must be absolute.");
        var norm = hasPath ? NormalisePath(path!) : null;
        var onNode = await db.Documents.Where(d => d.ProjectId == p.Id && d.ItemId == (it == null ? null : it.Id)).ToListAsync(ct);
        var d = onNode.FirstOrDefault(x => x.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
             ?? onNode.FirstOrDefault(x => norm != null && x.Path != null && x.Path.Equals(norm, StringComparison.OrdinalIgnoreCase))
             ?? onNode.FirstOrDefault(x => hasUrl && x.Url != null && x.Url.Equals(url!.Trim(), StringComparison.OrdinalIgnoreCase));
        var isNew = d is null;
        if (d is null) { d = new Document { Id = "doc-" + Ids.Short(), ProjectId = p.Id, ItemId = it?.Id, Title = title, At = Now }; db.Documents.Add(d); }
        d.Title = title; d.Path = norm; d.Url = hasUrl ? url!.Trim() : null; d.AddedBySessionId = s.Id;
        var where = norm ?? d.Url!;
        var on = it is null ? $"project {p.Code}" : it.Id;
        if (it is not null) await HistoryAsync(it.Id, "document", $"{(isNew ? "document" : "document updated")}: {title} · {where}", ct);
        await LedgerAsync(s, "document", $"{title} on {on}");
        Audit(Actor.Claude, isNew ? "add" : "update", "document", d.Id, $"{on} · {where}");
        await SaveAsync("document", d.Id, ct);
        return $"{(isNew ? "Recorded" : "Updated")} on {on}: {title} · {where}" + (norm is null ? "" : $" · opens from the Workbench on any machine with a checkout of {p.Name}");
    }

    /// <summary>Remove a document entry on the owner's word. The file stays where it is.</summary>
    public async Task<string> DocRemoveAsync(string sessionId, string? node, string titleOrPath, string? reason, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var (p, it) = await DocNodeAsync(s, node, ct);
        var key = titleOrPath.Trim();
        var onNode = await db.Documents.Where(d => d.ProjectId == p.Id && d.ItemId == (it == null ? null : it.Id)).ToListAsync(ct);
        var d = onNode.FirstOrDefault(x => x.Title.Equals(key, StringComparison.OrdinalIgnoreCase))
             ?? onNode.FirstOrDefault(x => x.Path != null && x.Path.Equals(key.Replace('\\', '/').TrimStart('/'), StringComparison.OrdinalIgnoreCase))
             ?? throw new WorkbenchRuleException("doc.unknown", $"There is no document \"{key}\" on {(it is null ? p.Code : it.Id)}.");
        db.Documents.Remove(d);
        var on = it is null ? $"project {p.Code}" : it.Id;
        if (it is not null) await HistoryAsync(it.Id, "document", $"document removed: {d.Title}" + (reason is null ? "" : $" · {reason}"), ct);
        await LedgerAsync(s, "document", $"{d.Title} removed from {on}");
        Audit(Actor.Claude, "remove", "document", d.Id, reason ?? on);
        await SaveAsync("document", d.Id, ct);
        return $"Removed from {on}: {d.Title}. The file is untouched.";
    }

    /// <summary>The documents of a project, or of an item and everything under it, one line each.</summary>
    public async Task<string> DocListAsync(string node, CancellationToken ct = default)
    {
        var it = await db.Items.FindAsync([node.Trim()], ct);
        List<Document> docs; string on;
        if (it is not null)
        {
            var ids = new HashSet<string> { it.Id };
            var all = await db.Items.Where(i => i.ProjectId == it.ProjectId).ToListAsync(ct);
            bool grew = true;
            while (grew) { grew = false; foreach (var x in all) if (x.ParentId is not null && ids.Contains(x.ParentId) && ids.Add(x.Id)) grew = true; }
            docs = await db.Documents.Where(d => d.ItemId != null && ids.Contains(d.ItemId)).OrderBy(d => d.At).ToListAsync(ct);
            on = it.Id;
        }
        else
        {
            var p = await FindProjectAsync(node.Trim(), ct) ?? throw new WorkbenchRuleException("doc.node", $"There is no item or project {node}.");
            docs = await db.Documents.Where(d => d.ProjectId == p.Id).OrderBy(d => d.ItemId).ThenBy(d => d.At).ToListAsync(ct);
            on = $"project {p.Code}";
        }
        if (docs.Count == 0) return $"No documents on {on} yet.";
        return string.Join("\n", docs.Select(d => $"{d.Title} · {d.Path ?? d.Url} · on {d.ItemId ?? "the project"} · {fmt.Day(d.At)}"));
    }

    /// <summary>The ref a project's documents are read at: the open node's branch, else main.</summary>
    public async Task<string> DocRefAsync(Project p, Item? it, CancellationToken ct = default)
    {
        if (p.IsOpen) return p.Branch ?? BranchNameFor(p);
        if (it is not null)
        {
            var top = await TopOfAsync(it, ct);
            if (top.IsOpen) return top.Branch ?? BranchNameFor(p, top);
        }
        return "main";
    }

    /// <summary>
    /// Where a project's files can be read right now: checkouts of that repository on machines whose Host is online, the
    /// node's own checkout first. Pure, so the snapshot and the document reader share it.
    /// </summary>
    public static List<DocPlace> DocPlaces(Project p, Item? it, IEnumerable<Item> items, IEnumerable<Machine> machines)
    {
        var mine = items.Where(i => i.ProjectId == p.Id).ToList();
        var known = new List<string>();
        void Add(string? path) { if (!string.IsNullOrWhiteSpace(path) && !known.Contains(path, StringComparer.OrdinalIgnoreCase)) known.Add(path); }
        Add(it?.Checkout); Add(p.Checkout);
        foreach (var i in mine) Add(i.Checkout);
        var prefix = p.Code + "-";
        var places = new List<DocPlace>();
        string? remote = null;
        foreach (var m in machines.Where(m => m.Online))
        {
            foreach (var c in Json.ReadList<Checkout>(m.CheckoutsJson))
            {
                var ours = known.Contains(c.Path, StringComparer.OrdinalIgnoreCase) || (c.ItemId is not null && c.ItemId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                if (!ours) continue;
                remote ??= c.Remote;
                places.Add(new DocPlace(m.Id, c.Path, c.Remote));
            }
        }
        // a second checkout of the same repository (another machine, a worktree) is recognised by its remote
        if (remote is not null)
            foreach (var m in machines.Where(m => m.Online))
                foreach (var c in Json.ReadList<Checkout>(m.CheckoutsJson))
                    if (c.Remote is not null && SameRemote(c.Remote, remote) && !places.Any(x => x.Machine == m.Id && x.Checkout.Equals(c.Path, StringComparison.OrdinalIgnoreCase)))
                        places.Add(new DocPlace(m.Id, c.Path, c.Remote));
        // the node's own checkout first, then the rest in registration order
        return places.OrderBy(x => known.FindIndex(k => k.Equals(x.Checkout, StringComparison.OrdinalIgnoreCase)) is var ix && ix >= 0 ? ix : int.MaxValue).ToList();
    }

    public static bool SameRemote(string a, string b) => RemoteKey(a) == RemoteKey(b);

    /// <summary>"github.com/owner/repo", case kept, from any of the ways git writes a remote.</summary>
    public static string RemotePath(string remote)
    {
        var r = remote.Trim();
        if (r.StartsWith("git@")) r = r[4..].Replace(':', '/');
        r = r.Replace("https://", "").Replace("http://", "").Replace("ssh://git@", "").Replace("ssh://", "");
        if (r.EndsWith(".git")) r = r[..^4];
        return r.TrimEnd('/');
    }

    /// <summary>The same, lower-cased, to tell two spellings of one repository apart from two repositories.</summary>
    public static string RemoteKey(string remote) => RemotePath(remote).ToLowerInvariant();

    /// <summary>The raw-file URL of a path at a ref, for the remotes we know how to read (GitHub); null otherwise.</summary>
    public static string? RawUrl(string remote, string @ref, string path)
    {
        var key = RemotePath(remote);
        if (!key.StartsWith("github.com/", StringComparison.OrdinalIgnoreCase)) return null;
        var parts = key.Split('/');
        if (parts.Length < 3) return null;
        return $"https://raw.githubusercontent.com/{parts[1]}/{parts[2]}/{Uri.EscapeDataString(@ref).Replace("%2F", "/")}/{string.Join('/', path.Split('/').Select(Uri.EscapeDataString))}";
    }
}

/// <summary>A checkout of a project's repository on a machine whose Host is online.</summary>
public sealed record DocPlace(string Machine, string Checkout, string? Remote);
