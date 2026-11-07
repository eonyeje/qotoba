using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>
/// The branch belongs to the opened top-level item. It is created in the owner's checkout when the item's first plan
/// is approved; the items under it share it; every approved packet merges it into main and it carries on; when the
/// top item is finished the branch is deleted and the checkout is back on main. A session finds its item from the
/// checkout it starts in: the checkout's bound item, else its branch, else the one open top item; then the open node
/// under it. The owner never names an item to a session.
/// </summary>
public sealed partial class WorkbenchOps
{
    /// <summary>The top-level ancestor of an item (itself when it is top-level).</summary>
    public async Task<Item> TopOfAsync(Item it, CancellationToken ct = default)
    {
        var node = it;
        for (var guard = 0; node.ParentId is not null && guard < 32; guard++)
            node = await db.Items.FindAsync([node.ParentId], ct) ?? throw new WorkbenchRuleException("item.parent", $"{node.Id} has no parent {node.ParentId}.");
        return node;
    }

    /// <summary>"wb/1-login-screen": project code, the item's number, a slug of its title.</summary>
    public static string BranchNameFor(Project p, Item top)
    {
        var number = top.Id.Contains('-') ? top.Id[(top.Id.IndexOf('-') + 1)..] : top.Id;
        var slug = new string(top.Title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        if (slug.Length > 40) slug = slug[..40].TrimEnd('-');
        return $"{p.Code.ToLowerInvariant()}/{number}" + (slug.Length == 0 ? "" : "-" + slug);
    }

    /// <summary>The deepest open node under a top item: the task a session works.</summary>
    public async Task<Item> DeepestOpenAsync(Item top, CancellationToken ct = default)
    {
        var node = top;
        for (var guard = 0; guard < 32; guard++)
        {
            var child = await db.Items.Where(i => i.ParentId == node.Id && i.IsOpen).OrderBy(i => i.Order).FirstOrDefaultAsync(ct);
            if (child is null) return node;
            node = child;
        }
        return node;
    }

    /// <summary>The checkout on a machine that holds a path, longest match first.</summary>
    private static Checkout? CheckoutHolding(Machine m, string? path)
    {
        if (path is null) return null;
        return List<Checkout>(m.CheckoutsJson)
            .Where(c => path.StartsWith(c.Path, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.Path.Length).FirstOrDefault();
    }

    /// <summary>
    /// Which item a session in this folder works on. The checkout's bound item, else the open top item on the checkout's
    /// branch, else the one open top item when there is exactly one; then the open node under it. Null when nothing is open.
    /// </summary>
    public async Task<string?> ItemForCwdAsync(Machine m, string? cwd, CancellationToken ct = default)
    {
        var hit = CheckoutHolding(m, cwd);
        Item? top = null;
        if (hit?.ItemId is not null) top = await db.Items.FirstOrDefaultAsync(i => i.Id == hit.ItemId && i.IsOpen, ct);
        if (top is null && hit is not null)
        {
            var branch = await host.BranchAsync(m.Id, hit.Path, ct) ?? hit.Branch;
            if (!string.IsNullOrEmpty(branch) && branch != "main" && branch != "master")
                top = await db.Items.FirstOrDefaultAsync(i => i.Branch == branch && i.IsOpen && i.ParentId == null, ct);
        }
        if (top is null)
        {
            var opens = await db.Items.Where(i => i.ParentId == null && i.IsOpen).ToListAsync(ct);
            if (opens.Count == 1) top = opens[0];
        }
        if (top is null) return null;
        return (await DeepestOpenAsync(top, ct)).Id;
    }

    /// <summary>
    /// Makes sure the top item has its branch checked out on the machine. Runs git through the Host; never fails the
    /// caller. Returns one line for the owner's toast and the item's history.
    /// </summary>
    public async Task<string> EnsureBranchAsync(Item top, string machineId, string? cwdHint, CancellationToken ct = default)
    {
        var p = await ProjectOfAsync(top, ct);
        var branch = top.Branch ?? BranchNameFor(p, top);
        var m = await db.Machines.FindAsync([machineId], ct);
        var checkouts = m is null ? [] : List<Checkout>(m.CheckoutsJson);
        var mine = checkouts.FirstOrDefault(c => c.ItemId == top.Id)
                   ?? (top.Checkout is null ? null : checkouts.FirstOrDefault(c => c.Path.Equals(top.Checkout, StringComparison.OrdinalIgnoreCase)))
                   ?? CheckoutHolding(m ?? new Machine { Id = machineId }, cwdHint)
                   ?? checkouts.FirstOrDefault(c => c.ItemId is null);
        var path = mine?.Path ?? top.Checkout ?? cwdHint;
        if (path is null)
        {
            await HistoryAsync(top.Id, "no checkout", $"No checkout is known on {machineId}; the branch {branch} is not created yet", ct);
            return $"no checkout known on {machineId}, branch not created";
        }
        if (top.Branch == branch && top.Checkout is not null && top.HomeMachineId == machineId && mine?.ItemId == top.Id)
            return $"on branch {branch}";

        var git = await host.RunGitAsync(machineId, new GitInstruction(path, [$"git checkout {branch}"]), ct);
        if (!git.Ok) git = await host.RunGitAsync(machineId, new GitInstruction(path, ["git checkout main", $"git checkout -b {branch}"]), ct);
        if (!git.Ok)
        {
            await HistoryAsync(top.Id, "branch not created", "The Host reported: " + Trim(git.Output, 200), ct);
            return "branch not created: " + Trim(git.Output, 120);
        }
        top.Branch = branch; top.Checkout = path; top.HomeMachineId ??= machineId;
        if (m is not null)
        {
            var list = checkouts.Select(c => c.Path.Equals(path, StringComparison.OrdinalIgnoreCase) ? c with { Branch = branch, ItemId = top.Id } : c).ToList();
            if (!list.Any(c => c.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) list.Add(new Checkout(path, branch, top.Id, false, null));
            m.CheckoutsJson = Json.Write(list);
        }
        await HistoryAsync(top.Id, "branch", $"{branch} checked out in {path} on {machineId}", ct);
        Audit(Actor.System, "branch", "item", top.Id, branch);
        return $"branch {branch} checked out on {machineId}";
    }

    /// <summary>True when approving this item's packet finishes its top item: every other node under the top is done and the top has no work of its own left.</summary>
    public async Task<bool> TopFinishesWithAsync(Item top, Item it, CancellationToken ct = default)
    {
        var all = await db.Items.Where(i => i.ProjectId == top.ProjectId).ToListAsync(ct);
        var under = new List<Item>();
        var frontier = new Queue<string>([top.Id]);
        while (frontier.Count > 0)
        {
            var id = frontier.Dequeue();
            foreach (var c in all.Where(i => i.ParentId == id)) { under.Add(c); frontier.Enqueue(c.Id); }
        }
        if (under.Any(c => c.Id != it.Id && c.State != ItemState.Done)) return false;
        if (it.Id == top.Id) return true;
        var ownSteps = List<Step>(top.StepsJson);
        return ownSteps.Count == 0 || ownSteps.All(s => s.State == StepState.Done);
    }

    /// <summary>The git lines the Host runs when a packet is approved. Lines starting with "- " may fail without stopping the rest.</summary>
    public static List<string> MergeInstruction(Item top, Item it, bool closesTop)
    {
        if (top.Branch is null) return [];
        var title = it.Title.Replace('"', '\'');
        var lines = new List<string>
        {
            "git checkout main",
            $"git merge --no-ff {top.Branch} -m \"Merge {it.Id}: {title}\"",
            "git rev-parse --short HEAD",
            "- git push origin main"
        };
        if (closesTop) { lines.Add($"git branch -d {top.Branch}"); lines.Add($"- git push origin --delete {top.Branch}"); }
        else { lines.Add($"git checkout {top.Branch}"); lines.Add("git merge main"); }
        return lines;
    }

    /// <summary>The top item is finished: closed, its branch gone, its checkout free.</summary>
    public async Task CloseTopAsync(Item top, CancellationToken ct = default)
    {
        var branch = top.Branch;
        top.State = ItemState.Done; top.IsOpen = false; top.Branch = null;
        if (top.HomeMachineId is not null && await db.Machines.FindAsync([top.HomeMachineId], ct) is { } m)
            m.CheckoutsJson = Json.Write(List<Checkout>(m.CheckoutsJson).Select(c => c.ItemId == top.Id ? c with { Branch = "main", ItemId = null } : c).ToList());
        top.Checkout = null;
        await HistoryAsync(top.Id, "closed", branch is null ? "Everything under it is done" : $"Everything under it is done; branch {branch} deleted, the checkout is on main", ct);
        Audit(Actor.System, "closed", "item", top.Id, branch);
    }
}
