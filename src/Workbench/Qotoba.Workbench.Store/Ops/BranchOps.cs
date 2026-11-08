using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>
/// The branch belongs to the highest open node: the project when the owner opened it as a whole, else the opened
/// top-level item. Everything under that node shares it. That is a convention Claude keeps itself, on the owner's word;
/// the Workbench only records which branch a node is on. A session finds its node from the checkout it starts in: the
/// checkout's bound node, else its branch, else the one open top item; then the open node under it. Nobody types an id.
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
        var slug = Slug(top.Title);
        return $"{p.Code.ToLowerInvariant()}/{number}" + (slug.Length == 0 ? "" : "-" + slug);
    }

    /// <summary>"wb/workbench": the branch of a project the owner opened as a whole.</summary>
    public static string BranchNameFor(Project p)
    {
        var slug = Slug(p.Name);
        return $"{p.Code.ToLowerInvariant()}/" + (slug.Length == 0 ? p.Id.ToLowerInvariant() : slug);
    }

    private static string Slug(string text)
    {
        var slug = new string(text.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        return slug.Length > 40 ? slug[..40].TrimEnd('-') : slug;
    }

    /// <summary>A project by id or code, or null when the id names something else (an item, say).</summary>
    public async Task<Project?> FindProjectAsync(string idOrCode, CancellationToken ct = default) =>
        await db.Projects.FindAsync([idOrCode], ct) ?? await db.Projects.FirstOrDefaultAsync(x => x.Code == idOrCode.ToUpperInvariant(), ct);

    /// <summary>The branch an item works on and who holds it: the project when it is open as a whole, else the item's top ancestor. Recorded is false while only the convention exists.</summary>
    public async Task<BranchHold> BranchOfAsync(Item it, CancellationToken ct = default)
    {
        var p = await ProjectOfAsync(it, ct);
        if (p.IsOpen) return new BranchHold(p.Branch ?? BranchNameFor(p), $"project {p.Code}, open as a whole", p.Checkout, p.Branch is not null);
        var top = await TopOfAsync(it, ct);
        return new BranchHold(top.Branch ?? BranchNameFor(p, top), top.Id, top.Checkout, top.Branch is not null);
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
        Project? whole = null;                                   // a checkout on a project opened as a whole
        if (hit?.ItemId is not null)
        {
            top = await db.Items.FirstOrDefaultAsync(i => i.Id == hit.ItemId && i.IsOpen, ct);
            if (top is null) whole = await db.Projects.FirstOrDefaultAsync(x => x.Id == hit.ItemId && x.IsOpen, ct);
        }
        if (top is null && whole is null && hit is not null)
        {
            var branch = await host.BranchAsync(m.Id, hit.Path, ct) ?? hit.Branch;
            if (!string.IsNullOrEmpty(branch) && branch != "main" && branch != "master")
            {
                top = await db.Items.FirstOrDefaultAsync(i => i.Branch == branch && i.IsOpen && i.ParentId == null, ct);
                whole ??= top is null ? await db.Projects.FirstOrDefaultAsync(x => x.Branch == branch && x.IsOpen, ct) : null;
            }
        }
        if (whole is not null)
        {
            var tops = await db.Items.Where(i => i.ProjectId == whole.Id && i.ParentId == null && i.IsOpen).ToListAsync(ct);
            if (tops.Count != 1) return null;                    // the session belongs to the project itself
            top = tops[0];
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
    /// The owner asked for a node in a new Rider window: an item, or a project to open as a whole. The Host checks the
    /// node's branch out as a worktree beside the main checkout; the node opens; the add-in then opens that folder as a
    /// new window of the running Rider.
    /// </summary>
    public async Task<(Item? item, string folder, string branch, string message)> OpenInWindowAsync(string nodeId, CancellationToken ct = default)
    {
        var proj = await FindProjectAsync(nodeId, ct);
        var it = proj is null ? await RequireItemAsync(nodeId, ct) : null;
        var p = proj ?? await ProjectOfAsync(it!, ct);
        var top = it is null ? null : await TopOfAsync(it, ct);
        var wholeProject = proj is not null || p.IsOpen;        // the project holds the branch
        var branch = wholeProject ? p.Branch ?? BranchNameFor(p) : top!.Branch ?? BranchNameFor(p, top);
        var home = wholeProject ? p.HomeMachineId : top!.HomeMachineId;
        var machines = await db.Machines.Where(m => m.Online).ToListAsync(ct);
        var machine = machines.FirstOrDefault(m => m.Id == home) ?? machines.FirstOrDefault(m => m.Id == options.MachineId) ?? machines.FirstOrDefault()
            ?? throw new WorkbenchRuleException("machine.none", "No Host is online to prepare the folder.");
        var checkouts = List<Checkout>(machine.CheckoutsJson);
        var root = checkouts.FirstOrDefault(c => !c.Worktree) ?? checkouts.FirstOrDefault() ?? throw new WorkbenchRuleException("checkout.none", $"The Host on {machine.Id} knows no checkout.");
        var folder = checkouts.FirstOrDefault(c => c.Branch == branch && c.Path != root.Path)?.Path
            ?? Path.Combine(Path.GetDirectoryName(root.Path.TrimEnd('\\', '/')) ?? root.Path, Path.GetFileName(root.Path.TrimEnd('\\', '/')) + "-" + BranchSlug(branch));
        var wt = await host.WorktreeAsync(machine.Id, new WorktreeRequest(root.Path, branch, folder), ct);
        if (!wt.Ok) throw new WorkbenchRuleException("worktree", "The Host could not prepare the folder: " + Trim(wt.Output, 200));
        string holder;
        if (wholeProject) { p.Branch = branch; p.Checkout = wt.Folder; p.HomeMachineId ??= machine.Id; holder = p.Id; }
        else { top!.Branch = branch; top.Checkout = wt.Folder; top.HomeMachineId ??= machine.Id; holder = top.Id; }
        var list = checkouts.Where(c => !c.Path.Equals(wt.Folder, StringComparison.OrdinalIgnoreCase)).ToList();
        list.Add(new Checkout(wt.Folder, branch, holder, true, "opened in a new Rider window"));
        machine.CheckoutsJson = Json.Write(list);
        Item? opened = null; string msg;
        if (proj is not null)
        {
            msg = p.IsOpen ? $"{p.Name} is already open as a whole" : $"{p.Name} opened as a whole";
            p.IsOpen = true; if (p.State != ProjectState.Open) { p.State = ProjectState.Open; p.ClosedWhy = null; }
            Audit(Actor.Owner, "window", "project", p.Id, wt.Folder);
            await SaveAsync("project", p.Id, ct);
        }
        else
        {
            (opened, msg) = it!.IsOpen ? (it, $"{it.Id} is already open") : await OpenNodeAsync(it, ct);
            await HistoryAsync(top!.Id, "new window", $"{branch} checked out in {wt.Folder} on {machine.Id}" + (wt.Created ? "" : " (already there)"), ct);
            Audit(Actor.Owner, "window", "item", opened.Id, wt.Folder);
            await SaveAsync("item", top.Id, ct);
            if (wholeProject) await SaveAsync("project", p.Id, ct);
        }
        return (opened, wt.Folder, branch, msg + (wt.Created ? $" · {branch} checked out in a new folder" : $" · folder already on {wt.Branch}"));
    }

    private static string BranchSlug(string branch) { var i = branch.IndexOf('/'); return i < 0 ? branch : branch[(i + 1)..]; }

    /// <summary>The project a session belongs to when no item is open: the checkout's bound node's project, the project on the checkout's branch, else the one project open as a whole.</summary>
    public async Task<string?> ProjectForCwdAsync(Machine m, string? cwd, CancellationToken ct = default)
    {
        var hit = CheckoutHolding(m, cwd);
        if (hit?.ItemId is not null)
        {
            if (await db.Projects.FindAsync([hit.ItemId], ct) is { } bp) return bp.Id;
            if (await db.Items.FindAsync([hit.ItemId], ct) is { } bound) return bound.ProjectId;
        }
        if (hit is not null)
        {
            var branch = await host.BranchAsync(m.Id, hit.Path, ct) ?? hit.Branch;
            if (!string.IsNullOrEmpty(branch) && branch != "main" && branch != "master" && await db.Projects.FirstOrDefaultAsync(x => x.Branch == branch && x.IsOpen, ct) is { } onBranch) return onBranch.Id;
        }
        var open = await db.Projects.Where(p => p.IsOpen).ToListAsync(ct);
        return open.Count == 1 ? open[0].Id : null;
    }

    /// <summary>
    /// The owner opens a project as a whole: it holds the branch (convention "wb/workbench") that everything under it
    /// shares, and sessions in the checkout belong to it until an item under it opens.
    /// </summary>
    public async Task<Project> OpenProjectAsync(string projectId, CancellationToken ct = default)
    {
        var p = await FindProjectAsync(projectId, ct) ?? throw new WorkbenchRuleException("project.unknown", $"There is no project {projectId}.");
        if (p.IsOpen) return p;
        p.IsOpen = true;
        if (p.State != ProjectState.Open) { p.State = ProjectState.Open; p.ClosedWhy = null; }
        Audit(Actor.Owner, "open", "project", p.Id);
        await SaveAsync("project", p.Id, ct);
        return p;
    }

    /// <summary>The owner said in chat to work on the whole project: the session opens it and belongs to it.</summary>
    public async Task<string> OpenProjectFromChatAsync(string sessionId, string projectId, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var p = await FindProjectAsync(projectId, ct) ?? throw new WorkbenchRuleException("project.unknown", $"There is no project {projectId}.");
        var was = p.IsOpen;
        if (!was) await OpenProjectAsync(p.Id, ct);
        p.HomeMachineId ??= s.MachineId;
        s.ProjectId = p.Id;
        if (s.ItemId is not null && (await db.Items.FindAsync([s.ItemId], ct))?.ProjectId != p.Id) s.ItemId = null;
        var msg = $"{p.Name} ({p.Code}) {(was ? "is already open" : "opened")} as a whole";
        await LedgerAsync(s, "open", msg);
        if (!was) Audit(Actor.Claude, "open", "project", p.Id);
        await SaveAsync("session", s.Id, ct);
        return msg + (p.Branch is null ? $" · branch convention: {BranchNameFor(p)}, shared by everything in it; create or check it out yourself and record it with branch_note" : $" · on {p.Branch}");
    }

    /// <summary>The owner said the work on the whole project is done: it is no longer open and its branch is gone from the record. Only on their word, through Claude.</summary>
    public async Task<string> ProjectDoneAsync(Session s, Project p, string outcome, CancellationToken ct = default)
    {
        Rules.TextFits(outcome, "The outcome");
        var branch = p.Branch;
        p.IsOpen = false; p.Branch = null; p.Checkout = null;
        if (s.MachineId is not null && await db.Machines.FindAsync([s.MachineId], ct) is { } m)
        {
            var list = List<Checkout>(m.CheckoutsJson).Select(c => c.ItemId == p.Id ? c with { ItemId = null } : c).ToList();
            m.CheckoutsJson = Json.Write(list);
        }
        if (s.ProjectId == p.Id && s.ItemId is null) s.ProjectId = null;
        await LedgerAsync(s, "done", $"{p.Code} as a whole · {outcome.Trim()}");
        Audit(Actor.Claude, "done", "project", p.Id, outcome.Trim());
        await SaveAsync("project", p.Id, ct);
        return $"{p.Name} is no longer open as a whole" + (branch is null ? "" : $" · {branch} is off the record");
    }

    /// <summary>A session says which branch the opened node is on, after it created or checked it out itself. The node is the project when it is open as a whole, else the item's top ancestor. Recorded, never acted on.</summary>
    public async Task<string> BranchNoteAsync(string sessionId, string nodeId, string branch, string? checkout, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var proj = await FindProjectAsync(nodeId, ct);
        Item? top = null;
        if (proj is null)
        {
            var it = await RequireItemAsync(nodeId, ct);
            var p = await ProjectOfAsync(it, ct);
            if (p.IsOpen) proj = p; else top = await TopOfAsync(it, ct);
        }
        var name = branch.Trim();
        var folder = checkout ?? s.Cwd ?? (proj?.Checkout ?? top?.Checkout);
        string holder, label;
        if (proj is not null) { proj.Branch = name; proj.Checkout = folder; proj.HomeMachineId ??= s.MachineId; holder = proj.Id; label = $"project {proj.Code}"; }
        else { top!.Branch = name; top.Checkout = folder; top.HomeMachineId ??= s.MachineId; holder = top.Id; label = top.Id; }
        if (s.MachineId is not null && await db.Machines.FindAsync([s.MachineId], ct) is { } m && folder is not null)
        {
            var list = List<Checkout>(m.CheckoutsJson);
            var i = list.FindIndex(c => c.Path.Equals(folder, StringComparison.OrdinalIgnoreCase));
            if (i >= 0) list[i] = list[i] with { Branch = name, ItemId = holder }; else list.Add(new Checkout(folder, name, holder, false, null));
            m.CheckoutsJson = Json.Write(list);
        }
        if (top is not null) await HistoryAsync(top.Id, "branch", $"On {name}" + (folder is null ? "" : $" in {folder}"), ct);
        await LedgerAsync(s, "branch", $"{label} on {name}");
        await SaveAsync(proj is not null ? "project" : "item", holder, ct);
        return $"{label} is on {name}";
    }
}

/// <summary>The branch a node works on, who holds it, where it is checked out, and whether it is recorded or still only the convention.</summary>
public sealed record BranchHold(string Branch, string Holder, string? Checkout, bool Recorded);
