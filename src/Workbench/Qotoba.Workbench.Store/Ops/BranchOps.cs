using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>
/// The branch belongs to the opened top-level item and the items under it share it. That is a convention Claude keeps
/// itself, on the owner's word; the Workbench only records which branch an item is on. A session finds its item from the
/// checkout it starts in: the checkout's bound item, else its branch, else the one open top item; then the open node
/// under it. Nobody types an id.
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

    /// <summary>The deepest open node under a top item: the item a session works.</summary>
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
    /// The owner asked for an item in a new Rider window. The Host checks the item's top branch out as a worktree beside
    /// the main checkout; the node opens; the add-in then opens that folder as a new window of the running Rider.
    /// </summary>
    public async Task<(Item item, string folder, string branch, string message)> OpenInWindowAsync(string itemId, CancellationToken ct = default)
    {
        var it = await RequireItemAsync(itemId, ct);
        var top = await TopOfAsync(it, ct);
        var p = await ProjectOfAsync(top, ct);
        var branch = top.Branch ?? BranchNameFor(p, top);
        var machines = await db.Machines.Where(m => m.Online).ToListAsync(ct);
        var machine = machines.FirstOrDefault(m => m.Id == top.HomeMachineId) ?? machines.FirstOrDefault(m => m.Id == options.MachineId) ?? machines.FirstOrDefault()
            ?? throw new WorkbenchRuleException("machine.none", "No Host is online to prepare the folder.");
        var checkouts = List<Checkout>(machine.CheckoutsJson);
        var root = checkouts.FirstOrDefault(c => !c.Worktree) ?? checkouts.FirstOrDefault() ?? throw new WorkbenchRuleException("checkout.none", $"The Host on {machine.Id} knows no checkout.");
        var folder = checkouts.FirstOrDefault(c => c.Branch == branch && c.Path != root.Path)?.Path
            ?? Path.Combine(Path.GetDirectoryName(root.Path.TrimEnd('\\', '/')) ?? root.Path, Path.GetFileName(root.Path.TrimEnd('\\', '/')) + "-" + BranchSlug(branch));
        var wt = await host.WorktreeAsync(machine.Id, new WorktreeRequest(root.Path, branch, folder), ct);
        if (!wt.Ok) throw new WorkbenchRuleException("worktree", "The Host could not prepare the folder: " + Trim(wt.Output, 200));
        top.Branch = branch; top.Checkout = wt.Folder; top.HomeMachineId ??= machine.Id;
        var list = checkouts.Where(c => !c.Path.Equals(wt.Folder, StringComparison.OrdinalIgnoreCase)).ToList();
        list.Add(new Checkout(wt.Folder, branch, top.Id, true, "opened in a new Rider window"));
        machine.CheckoutsJson = Json.Write(list);
        var (opened, msg) = it.IsOpen ? (it, $"{it.Id} is already open") : await OpenNodeAsync(it, ct);
        await HistoryAsync(top.Id, "new window", $"{branch} checked out in {wt.Folder} on {machine.Id}" + (wt.Created ? "" : " (already there)"), ct);
        Audit(Actor.Owner, "window", "item", opened.Id, wt.Folder);
        await SaveAsync("item", top.Id, ct);
        return (opened, wt.Folder, branch, msg + (wt.Created ? $" · {branch} checked out in a new folder" : $" · folder already on {wt.Branch}"));
    }

    private static string BranchSlug(string branch) { var i = branch.IndexOf('/'); return i < 0 ? branch : branch[(i + 1)..]; }

    /// <summary>A session says which branch the opened top item is on, after it created or checked it out itself. Recorded, never acted on.</summary>
    public async Task<string> BranchNoteAsync(string sessionId, string itemId, string branch, string? checkout, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        var top = await TopOfAsync(it, ct);
        top.Branch = branch.Trim();
        top.Checkout = checkout ?? s.Cwd ?? top.Checkout;
        top.HomeMachineId ??= s.MachineId;
        if (s.MachineId is not null && await db.Machines.FindAsync([s.MachineId], ct) is { } m && top.Checkout is not null)
        {
            var list = List<Checkout>(m.CheckoutsJson);
            var i = list.FindIndex(c => c.Path.Equals(top.Checkout, StringComparison.OrdinalIgnoreCase));
            if (i >= 0) list[i] = list[i] with { Branch = top.Branch, ItemId = top.Id }; else list.Add(new Checkout(top.Checkout, top.Branch, top.Id, false, null));
            m.CheckoutsJson = Json.Write(list);
        }
        await HistoryAsync(top.Id, "branch", $"On {top.Branch}" + (top.Checkout is null ? "" : $" in {top.Checkout}"), ct);
        await LedgerAsync(s, "branch", $"{top.Id} on {top.Branch}");
        await SaveAsync("item", top.Id, ct);
        return $"{top.Id} is on {top.Branch}";
    }
}
