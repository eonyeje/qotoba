using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>
/// FTR, BUG and PER: the register. Always written by Claude, an FTR from the owner's words in chat, a BUG or PER from
/// Claude's own checks. Attached to the item it came up in, or to the project. Information only; the owner prunes in chat.
/// </summary>
public sealed partial class WorkbenchOps
{
    /// <summary>The sections a report may carry, headline first. Everything after the first few is for the next session and folds.</summary>
    public static readonly Dictionary<ReportKind, string[]> SectionKeys = new()
    {
        [ReportKind.FTR] = ["why", "story", "scope", "design", "acceptance", "parity", "risks", "impact", "plan", "parts", "evidence", "note"],
        [ReportKind.BUG] = ["why", "symptom", "reproduce", "evidence", "environment", "frequency", "cause", "fixes", "workaround", "acceptance", "parts", "note"],
        [ReportKind.PER] = ["why", "headline", "target", "comparison", "series", "profile", "resources", "cause", "fixes", "guard", "environment", "parts", "evidence", "note"]
    };

    /// <summary>Claude raises an FPB: the owner said it in chat (FTR) or a check found it (BUG, PER).</summary>
    public async Task<Report> RaiseReportAsync(string sessionId, string kind, string title, string? attach, string? why, string? source,
        Dictionary<string, JsonElement>? sections, List<LinkInput>? links, string? severity, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        if (!Enum.TryParse<ReportKind>(kind, true, out var k)) throw new WorkbenchRuleException("report.kind", "Kind is FTR, BUG or PER.");
        if (string.IsNullOrWhiteSpace(title)) throw new WorkbenchRuleException("report.title", "A title: the capability for an FTR, the symptom as a causal sentence for a BUG, the wasteful mechanism for a PER.");
        Rules.TextFits(title, "The title");
        Item? item = null; Project? project = null;
        var at = attach ?? s.ItemId;
        if (at is { Length: > 0 })
        {
            item = await db.Items.FindAsync([at], ct);
            project = item is null ? await db.Projects.FindAsync([at], ct) ?? await db.Projects.FirstOrDefaultAsync(p => p.Code == at.ToUpperInvariant(), ct) : await ProjectOfAsync(item, ct);
            if (project is null) throw new WorkbenchRuleException("report.attach", $"There is no item or project {at} to attach it to.");
        }
        project ??= await db.Projects.OrderBy(p => p.Order).FirstAsync(ct);
        var dup = await db.Reports.Where(r => r.ProjectId == project.Id && r.Kind == k && r.Title.ToLower() == title.Trim().ToLower()).FirstOrDefaultAsync(ct);
        if (dup is not null) throw new WorkbenchRuleException("report.dup", $"{dup.Id} already says that: \"{dup.Title}\". Update it with fpb_update instead.");

        var n = await db.Reports.CountAsync(r => r.ProjectId == project.Id && r.Kind == k, ct) + 1;
        while (await db.Reports.AnyAsync(r => r.Id == Ids.Report(project.Code, k, n), ct)) n++;
        var r = new Report
        {
            Id = Ids.Report(project.Code, k, n), Kind = k, Title = title.Trim(), ProjectId = project.Id, AttachItemId = item?.Id,
            State = k == ReportKind.BUG && (source ?? "").Contains("read", StringComparison.OrdinalIgnoreCase) ? "Verify first" : "Open",
            SourceType = source?.Trim() is { Length: > 0 } src ? src : (k == ReportKind.FTR ? "chat" : "check"), SourceSessionId = s.Id, Created = Now
        };
        if (severity is { Length: > 0 } sv && Enum.TryParse<SeverityLevel>(sv, true, out var lvl)) { r.Severity = lvl; r.SeverityBy = Author.Claude; }
        var store = new JsonObject();
        if (!string.IsNullOrWhiteSpace(why)) store["why"] = new JsonObject { ["author"] = "claude", ["session"] = s.Id, ["text"] = why.Trim() };
        WriteSections(r.Kind, store, sections, s.Id);
        r.SectionsJson = store.ToJsonString();
        r.HistoryJson = Json.Write(new List<object[]> { new object[] { fmt.DayTime(Now), "session:" + s.Id, r.SourceType == "chat" ? "Raised from your chat" : "Raised by the session from its checks" } });
        db.Reports.Add(r);
        foreach (var l in links ?? []) await LinkCoreAsync(s, "fpb:" + r.Id, l.To, l.Type, l.Reason, ct);
        if (item is not null) await HistoryAsync(item.Id, k.ToString(), $"[[fpb:{r.Id}]] {r.Title}", ct);
        await LedgerAsync(s, "fpb", $"{r.Id} · {Trim(r.Title, 60)}");
        Audit(Actor.Claude, "raise", "report", r.Id, r.Title);
        await SaveAsync("report", r.Id, ct);
        return r;
    }

    /// <summary>Claude moves an FPB on: a new state, what is left, who took it on, more sections, links.</summary>
    public async Task<Report> UpdateReportAsync(string sessionId, string reportId, string? state, string? left, string? takenOn, string? title,
        Dictionary<string, JsonElement>? sections, List<LinkInput>? links, string? note, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var r = await db.Reports.FindAsync([reportId], ct) ?? throw new WorkbenchRuleException("report.unknown", $"There is no FPB {reportId}.");
        var h = Json.Read<List<object[]>>(r.HistoryJson, []);
        var changed = new List<string>();
        if (state is { Length: > 0 })
        {
            var st = ReportStates.Normalise(state) ?? throw new WorkbenchRuleException("report.state", "State is one of: " + string.Join(", ", ReportStates.All) + ".");
            if (st != r.State) { h.Add([fmt.DayTime(Now), "session:" + s.Id, $"{r.State} → {st}" + (left is null ? "" : $": {left.Trim()}")]); r.State = st; changed.Add(st); }
        }
        if (left is not null) { r.Left = left.Trim().Length == 0 ? null : left.Trim(); if (!changed.Any()) changed.Add("left"); }
        if (title is { Length: > 0 } && title.Trim() != r.Title) { h.Add([fmt.DayTime(Now), "session:" + s.Id, $"Retitled from “{r.Title}”"]); r.Title = title.Trim(); changed.Add("title"); }
        if (takenOn is not null)
        {
            var it = takenOn.Length == 0 ? null : await RequireItemAsync(takenOn, ct);
            r.TakenOnItemId = it?.Id;
            if (it is not null) { h.Add([fmt.DayTime(Now), "session:" + s.Id, $"Taken on by [[item:{it.Id}]]"]); if (r.State is "Open" or "Verify first" or "Designed") r.State = "In progress"; changed.Add("taken on"); }
        }
        if (sections is { Count: > 0 })
        {
            var store = JsonNode.Parse(r.SectionsJson)?.AsObject() ?? new JsonObject();
            var written = WriteSections(r.Kind, store, sections, s.Id);
            r.SectionsJson = store.ToJsonString();
            if (written.Count > 0) { h.Add([fmt.DayTime(Now), "session:" + s.Id, "Wrote " + string.Join(", ", written)]); changed.AddRange(written); }
        }
        if (note is not null)
        {
            Rules.NoteFits(note);
            var store = JsonNode.Parse(r.SectionsJson)?.AsObject() ?? new JsonObject();
            store["note"] = new JsonObject { ["author"] = "claude", ["session"] = s.Id, ["text"] = note.Trim(), ["date"] = fmt.DayTime(Now) };
            r.SectionsJson = store.ToJsonString();
            changed.Add("note");
        }
        foreach (var l in links ?? []) await LinkCoreAsync(s, "fpb:" + r.Id, l.To, l.Type, l.Reason, ct);
        r.Changed = Now;
        r.HistoryJson = Json.Write(h);
        await LedgerAsync(s, "fpb", $"{r.Id}: {string.Join(", ", changed.DefaultIfEmpty("touched"))}");
        Audit(Actor.Claude, "update", "report", r.Id, string.Join(",", changed));
        await SaveAsync("report", r.Id, ct);
        return r;
    }

    private static List<string> WriteSections(ReportKind kind, JsonObject store, Dictionary<string, JsonElement>? sections, string sessionId)
    {
        var written = new List<string>();
        var allowed = SectionKeys[kind];
        foreach (var (key, value) in sections ?? [])
        {
            if (!allowed.Contains(key)) throw new WorkbenchRuleException("report.section", $"A {kind} has no section '{key}'. Sections: {string.Join(", ", allowed)}.");
            JsonObject node;
            if (value.ValueKind == JsonValueKind.Object) node = JsonNode.Parse(value.GetRawText())!.AsObject();
            else if (value.ValueKind == JsonValueKind.String) node = new JsonObject { ["text"] = value.GetString() };
            else throw new WorkbenchRuleException("report.section", $"Section '{key}' is an object or a string.");
            if (key == "fixes" && node["rows"] is JsonArray rows && rows.Count > Caps.Fixes) throw new WorkbenchRuleException("report.fixes", $"At most {Caps.Fixes} possible fixes; drop the weakest.");
            node["author"] = key is "evidence" or "environment" or "frequency" or "comparison" or "series" or "profile" or "resources" ? "machine" : "claude";
            node["session"] = sessionId;
            store[key] = node;
            written.Add(key);
        }
        return written;
    }

    /// <summary>The owner renames, re-attaches or re-states an FPB on its page. Rare: they usually say it in chat and Claude does it.</summary>
    public async Task<Report> PatchReportAsync(string id, ReportPatch patch, CancellationToken ct = default)
    {
        var r = await db.Reports.FindAsync([id], ct) ?? throw new WorkbenchRuleException("report.unknown", $"There is no FPB {id}.");
        var h = Json.Read<List<object[]>>(r.HistoryJson, []);
        if (patch.Title is { Length: > 0 } t) { r.Title = t.Trim(); h.Add([fmt.DayTime(Now), "you", "You renamed it"]); }
        if (patch.Severity is { Length: > 0 } s && Enum.TryParse<SeverityLevel>(s, true, out var lvl)) { r.Severity = lvl; r.SeverityBy = Author.Me; h.Add([fmt.DayTime(Now), "you", $"You set the severity to {s.ToLowerInvariant()}"]); }
        if (patch.Subtype is { Length: > 0 } st) r.Subtype = st;
        if (patch.State is { Length: > 0 } state) { var ns = ReportStates.Normalise(state) ?? state; h.Add([fmt.DayTime(Now), "you", $"{r.State} → {ns}"]); r.State = ns; }
        if (patch.AttachItemId is not null) r.AttachItemId = patch.AttachItemId == "" ? null : patch.AttachItemId;
        r.Changed = Now;
        r.HistoryJson = Json.Write(h);
        Audit(Actor.Owner, "patch", "report", id);
        await SaveAsync("report", id, ct);
        return r;
    }

    /// <summary>One FPB as a session reads it.</summary>
    public async Task<string> ReportTextAsync(string reportId, CancellationToken ct = default)
    {
        var r = await db.Reports.FindAsync([reportId], ct) ?? throw new WorkbenchRuleException("report.unknown", $"There is no FPB {reportId}.");
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{r.Id} · {r.Kind} · {r.Title}");
        sb.AppendLine($"State: {r.State}" + (r.Left is null ? "" : $" · left: {r.Left}") + $" · attached to {(r.AttachItemId ?? r.ProjectId)}" + (r.TakenOnItemId is null ? "" : $" · taken on by {r.TakenOnItemId}") + $" · raised {fmt.DayTime(r.Created)} ({r.SourceType})");
        var links = await db.Edges.Where(e => e.From == "fpb:" + r.Id || e.To == "fpb:" + r.Id).ToListAsync(ct);
        foreach (var e in links) sb.AppendLine($"Link: {e.From} {LinkWord(e.Type)} {e.To}" + (e.Reason is null ? "" : $" ({e.Reason})"));
        var sections = JsonNode.Parse(r.SectionsJson)?.AsObject();
        if (sections is not null)
            foreach (var key in SectionKeys[r.Kind])
                if (sections[key] is JsonObject o) sb.AppendLine($"[{key}] " + (o["text"]?.ToString() ?? o.ToJsonString()));
        return sb.ToString();
    }

    /// <summary>The FPBs on an item or a project, one line each.</summary>
    public async Task<string> ReportListAsync(string? at, string? state, CancellationToken ct = default)
    {
        IQueryable<Report> q = db.Reports;
        if (at is { Length: > 0 })
        {
            var item = await db.Items.FindAsync([at], ct);
            if (item is not null)
            {
                var ids = new List<string> { item.Id };
                var frontier = new Queue<string>([item.Id]);
                while (frontier.Count > 0) { var id = frontier.Dequeue(); foreach (var c in await db.Items.Where(i => i.ParentId == id).Select(i => i.Id).ToListAsync(ct)) { ids.Add(c); frontier.Enqueue(c); } }
                q = q.Where(r => r.AttachItemId != null && ids.Contains(r.AttachItemId) || r.TakenOnItemId != null && ids.Contains(r.TakenOnItemId));
            }
            else
            {
                var p = await db.Projects.FindAsync([at], ct) ?? await db.Projects.FirstOrDefaultAsync(x => x.Code == at.ToUpperInvariant(), ct) ?? throw new WorkbenchRuleException("report.attach", $"There is no item or project {at}.");
                q = q.Where(r => r.ProjectId == p.Id);
            }
        }
        if (state is { Length: > 0 })
        {
            var st = ReportStates.Normalise(state);
            q = st == "Open" && state.Equals("open", StringComparison.OrdinalIgnoreCase) ? q.Where(r => r.State != "Done" && r.State != "Exists" && r.State != "Won't do") : q.Where(r => r.State == st);
        }
        var list = await q.OrderBy(r => r.Id).ToListAsync(ct);
        return list.Count == 0 ? "No FPBs." : string.Join("\n", list.Select(r => $"{r.Id} · {r.State} · {r.Title}" + (r.Left is null ? "" : $" · left: {r.Left}") + $" · on {r.AttachItemId ?? r.ProjectId}"));
    }

    private static string LinkWord(EdgeType t) => t switch
    {
        EdgeType.Needs => "needs", EdgeType.SplitFrom => "split from", EdgeType.ReplacedBy => "replaced by", EdgeType.CarriedBy => "carried by",
        EdgeType.FoundIn => "found in", EdgeType.Duplicate => "duplicates", _ => "relates to"
    };
}
