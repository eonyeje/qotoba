using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>Typed links between items and FPBs, in the register's words. Claude records them; the owner removes one in chat or on the page.</summary>
public sealed partial class WorkbenchOps
{
    public async Task<Edge> LinkAsync(string sessionId, string from, string to, string type, string? reason, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var e = await LinkCoreAsync(s, from, to, type, reason, ct);
        await SaveAsync("edge", e.Id, ct);
        return e;
    }

    private async Task<Edge> LinkCoreAsync(Session s, string from, string to, string type, string? reason, CancellationToken ct)
    {
        var et = ParseLinkType(type);
        from = NormaliseRef(from); to = NormaliseRef(to);
        if (!await RefExistsAsync(from, ct) || !await RefExistsAsync(to, ct)) throw new WorkbenchRuleException("edge.ref", $"Both ends must be an existing item or FPB: {from}, {to}.");
        if (from == to) throw new WorkbenchRuleException("edge.self", "A thing does not link to itself.");
        var existing = await db.Edges.FirstOrDefaultAsync(e => e.From == from && e.To == to && e.Type == et, ct);
        if (existing is not null) { if (reason is not null) existing.Reason = reason.Trim(); return existing; }
        if (reason is not null) Rules.TextFits(reason, "The reason");
        var e = new Edge { Id = "e-" + Ids.Short(), From = from, To = to, Type = et, Reason = reason?.Trim(), By = Actor.Claude, SessionId = s.Id, Confirmed = true };
        if (et == EdgeType.Needs && from.StartsWith("item:") && await db.Items.FindAsync([from[5..]], ct) is { } blocked) e.Min = await WorkBehindAsync(blocked, null, ct);
        db.Edges.Add(e);
        await LedgerAsync(s, "link", $"{from} {LinkWord(et)} {to}");
        return e;
    }

    private static EdgeType ParseLinkType(string type)
    {
        var t = type.Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
        return t switch
        {
            "needs" or "waitson" or "dependson" or "blockedby" => EdgeType.Needs,
            "splitfrom" => EdgeType.SplitFrom,
            "replacedby" or "supersededby" => EdgeType.ReplacedBy,
            "carriedby" or "absorbedby" => EdgeType.CarriedBy,
            "foundin" or "raisedby" or "foundbuilding" or "founddesigning" => EdgeType.FoundIn,
            "duplicate" or "duplicates" or "duplicateof" => EdgeType.Duplicate,
            "related" or "relatesto" or "seealso" => EdgeType.Related,
            _ => throw new WorkbenchRuleException("edge.type", "Type is needs, split from, replaced by, carried by, found in, duplicate or related.")
        };
    }

    /// <summary>The owner links two things on the tree.</summary>
    public async Task<Edge> AddEdgeAsync(string from, string to, CancellationToken ct = default)
    {
        from = NormaliseRef(from); to = NormaliseRef(to);
        if (!await RefExistsAsync(from, ct) || !await RefExistsAsync(to, ct)) throw new WorkbenchRuleException("edge.ref", "Name an existing item or FPB.");
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

    private static string NormaliseRef(string r)
    {
        if (r.Contains(':')) return r;
        var u = r.ToUpperInvariant();
        return u.Contains("-FTR-") || u.Contains("-BUG-") || u.Contains("-PER-") || u.StartsWith("FTR-") || u.StartsWith("BUG-") || u.StartsWith("PER-") ? "fpb:" + r : "item:" + r;
    }

    private async Task<bool> RefExistsAsync(string r, CancellationToken ct) =>
        r.StartsWith("item:") ? await db.Items.AnyAsync(i => i.Id == r.Substring(5), ct) || db.Items.Local.Any(i => i.Id == r.Substring(5))
        : r.StartsWith("fpb:") && (await db.Reports.AnyAsync(x => x.Id == r.Substring(4), ct) || db.Reports.Local.Any(x => x.Id == r.Substring(4)));

    /// <summary>Longest chain of unfinished work through needs edges and the open path, as item refs.</summary>
    public static List<string> CriticalPath(List<Item> items, List<Edge> edges, string projectId)
    {
        var mine = items.Where(i => i.ProjectId == projectId && i.State != ItemState.Done && !i.Preview).ToDictionary(i => i.Id);
        if (mine.Count == 0) return [];
        int Weight(Item i) => Math.Max(1, (int)Math.Round(((i.EstMin ?? 0) - i.SoMin) * (i.Factor ?? 1.2)));
        var blockedBy = new Dictionary<string, List<string>>();
        foreach (var e in edges.Where(e => e.Type == EdgeType.Needs && e.From.StartsWith("item:") && e.To.StartsWith("item:")))
        {
            var a = e.To[5..]; var b = e.From[5..];
            if (mine.ContainsKey(a) && mine.ContainsKey(b)) (blockedBy[a] = blockedBy.GetValueOrDefault(a) ?? []).Add(b);
        }
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
