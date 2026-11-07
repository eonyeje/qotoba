using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>Dependencies: Claude proposes, the owner confirms through the plan's settle pass (or removes).</summary>
public sealed partial class WorkbenchOps
{
    public async Task<Edge> ProposeEdgeAsync(string sessionId, string from, string to, string type, string reason, bool hard, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        if (!Enum.TryParse<EdgeType>(type, true, out var et)) throw new WorkbenchRuleException("edge.type", "Type is needs, related or duplicate.");
        from = NormaliseRef(from); to = NormaliseRef(to);
        if (!await RefExistsAsync(from, ct) || !await RefExistsAsync(to, ct)) throw new WorkbenchRuleException("edge.ref", "Both ends must be an existing item (item:WB-1) or report (fpb:BUG-1).");
        if (from == to) throw new WorkbenchRuleException("edge.self", "A thing does not depend on itself.");
        if (await db.Edges.AnyAsync(e => e.From == from && e.To == to && e.Type == et, ct)) throw new WorkbenchRuleException("edge.dup", "That link already exists.");
        Rules.TextFits(reason, "The reason");
        var e = new Edge { Id = "e-" + Ids.Short(), From = from, To = to, Type = et, Hard = hard, Reason = reason.Trim(), By = Actor.Claude, SessionId = s.Id, Confirmed = false };
        if (et == EdgeType.Needs && from.StartsWith("item:") && await db.Items.FindAsync([from[5..]], ct) is { } blocked) e.Min = await WorkBehindAsync(blocked, null, ct);
        db.Edges.Add(e);
        await LedgerAsync(s, "edge", $"{from} {et.ToString().ToLowerInvariant()} {to}");
        await SaveAsync("edge", e.Id, ct);
        return e;
    }

    /// <summary>The owner links two things directly; no confirmation is needed since it is theirs.</summary>
    public async Task<Edge> AddEdgeAsync(string from, string to, CancellationToken ct = default)
    {
        from = NormaliseRef(from); to = NormaliseRef(to);
        if (!await RefExistsAsync(from, ct) || !await RefExistsAsync(to, ct)) throw new WorkbenchRuleException("edge.ref", "Name an existing item (WB-2.3) or report (BUG-1).");
        if (from == to) throw new WorkbenchRuleException("edge.self", "A thing does not depend on itself.");
        var e = await db.Edges.FirstOrDefaultAsync(x => x.From == from && x.To == to && x.Type == EdgeType.Needs, ct);
        if (e is null) { e = new Edge { Id = "e-" + Ids.Short(), From = from, To = to, Type = EdgeType.Needs, By = Actor.Owner, Confirmed = true, Reason = "set by you" }; db.Edges.Add(e); }
        else { e.Confirmed = true; e.By = Actor.Owner; }
        Audit(Actor.Owner, "add", "edge", e.Id);
        await SaveAsync("edge", e.Id, ct);
        return e;
    }

    public async Task PatchEdgeAsync(string id, EdgePatch patch, CancellationToken ct = default)
    {
        var e = await db.Edges.FindAsync([id], ct) ?? throw new WorkbenchRuleException("edge.unknown", "No such link.");
        if (patch.Confirmed) { e.Confirmed = true; e.By = Actor.Owner; }
        else db.Edges.Remove(e);
        Audit(Actor.Owner, patch.Confirmed ? "confirm" : "remove", "edge", id);
        await SaveAsync("edge", id, ct);
    }

    private async Task<List<SettleQ>> DrawSettleFromEdgesAsync(Item it, int startIndex, CancellationToken ct)
    {
        var refId = "item:" + it.Id;
        var pending = await db.Edges.Where(e => e.From == refId && !e.Confirmed && e.Type == EdgeType.Needs).ToListAsync(ct);
        var n = startIndex;
        var list = new List<SettleQ>();
        foreach (var e in pending)
        {
            var title = await TitleOfAsync(e.To, ct);
            list.Add(new SettleQ("s" + (++n), $"Depends on {title}" + (e.Reason is null ? "" : $": {e.Reason}"), ["Accept", "Remove"], 0, null, null, null, e.Id));
        }
        return list;
    }

    private async Task SettleEdgesAsync(List<SettleQ> settle, CancellationToken ct)
    {
        foreach (var q in settle.Where(q => q.Edge is not null))
        {
            var e = await db.Edges.FindAsync([q.Edge!], ct);
            if (e is null) continue;
            var choice = q.Chosen is int c && c >= 0 && c < q.Options.Count ? q.Options[c] : q.Options[q.Rec];
            if (choice == "Accept") { e.Confirmed = true; e.By = Actor.Owner; } else db.Edges.Remove(e);
        }
    }

    private static string NormaliseRef(string r) => r.Contains(':') ? r : (r.StartsWith("FTR-") || r.StartsWith("BUG-") || r.StartsWith("PER-") ? "fpb:" + r : "item:" + r);

    private async Task<bool> RefExistsAsync(string r, CancellationToken ct) =>
        r.StartsWith("item:") ? await db.Items.AnyAsync(i => i.Id == r.Substring(5), ct) : r.StartsWith("fpb:") && await db.Reports.AnyAsync(x => x.Id == r.Substring(4), ct);

    private async Task<string> TitleOfAsync(string r, CancellationToken ct) =>
        r.StartsWith("item:") ? (await db.Items.FindAsync([r[5..]], ct))?.Title ?? r : (await db.Reports.FindAsync([r[4..]], ct))?.Title ?? r;

    /// <summary>Longest chain of unfinished work through confirmed needs edges and the open path, as item refs.</summary>
    public static List<string> CriticalPath(List<Item> items, List<Edge> edges, string projectId)
    {
        var mine = items.Where(i => i.ProjectId == projectId && i.State != ItemState.Done && !i.Preview).ToDictionary(i => i.Id);
        if (mine.Count == 0) return [];
        int Weight(Item i) => Math.Max(1, (int)Math.Round(((i.EstMin ?? 0) - i.SoMin) * (i.Factor ?? 1.2)));
        var blockedBy = new Dictionary<string, List<string>>();      // to (prerequisite) -> froms (dependants)
        foreach (var e in edges.Where(e => e.Type == EdgeType.Needs && e.Confirmed && e.From.StartsWith("item:") && e.To.StartsWith("item:")))
        {
            var a = e.To[5..]; var b = e.From[5..];
            if (mine.ContainsKey(a) && mine.ContainsKey(b)) (blockedBy[a] = blockedBy.GetValueOrDefault(a) ?? []).Add(b);
        }
        // children run inside their parent: an open parent's current child precedes its later siblings
        foreach (var g in mine.Values.Where(i => i.ParentId is not null).GroupBy(i => i.ParentId!))
        {
            var ordered = g.OrderBy(i => i.Order).ToList();
            for (var k = 0; k + 1 < ordered.Count; k++) (blockedBy[ordered[k].Id] = blockedBy.GetValueOrDefault(ordered[k].Id) ?? []).Add(ordered[k + 1].Id);
        }
        var memo = new Dictionary<string, (int w, List<string> path)>();
        (int w, List<string> path) Best(string id, HashSet<string> seen)
        {
            if (memo.TryGetValue(id, out var m)) return m;
            if (!seen.Add(id)) return (0, []);
            var best = (w: 0, path: new List<string>());
            foreach (var next in blockedBy.GetValueOrDefault(id) ?? [])
            {
                var b = Best(next, seen);
                if (b.w > best.w) best = b;
            }
            seen.Remove(id);
            var res = (Weight(mine[id]) + best.w, new List<string> { id }.Concat(best.path).ToList());
            memo[id] = res;
            return res;
        }
        var top = mine.Values.Select(i => Best(i.Id, [])).OrderByDescending(x => x.w).FirstOrDefault();
        var path = top.path ?? [];
        if (path.Count <= 1)
        {
            var open = mine.Values.Where(i => i.IsOpen).OrderBy(i => i.Id.Length).ThenBy(i => i.Order).Select(i => i.Id).ToList();
            if (open.Count > path.Count) path = open;
        }
        return path.Select(p => "item:" + p).ToList();
    }
}
