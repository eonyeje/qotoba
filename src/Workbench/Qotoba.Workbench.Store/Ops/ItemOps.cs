using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>
/// The work tree: a plain WBS. The owner shapes it, on the tree or by dictating in chat; Claude writes what the owner
/// dictated. An item is a unit of development work at any depth, never an FTR, BUG or PER.
/// </summary>
public sealed partial class WorkbenchOps
{
    /* ---------------------------------------------------------------- adding items */

    public async Task<Item> AddItemAsync(AddItemRequest req, Actor actor, CancellationToken ct = default)
    {
        if (!req.Kind.Equals("task", StringComparison.OrdinalIgnoreCase) && !req.Kind.Equals("item", StringComparison.OrdinalIgnoreCase))
            throw new WorkbenchRuleException("item.kind", "An item is a unit of work. An FTR, BUG or PER is raised by Claude from chat, never added here.");
        if (string.IsNullOrWhiteSpace(req.Title)) throw new WorkbenchRuleException("item.title", "A title, in the owner's words.");
        Item? parent = null; Project? project = null;
        if (req.Place is { Length: > 0 } place)
        {
            parent = await db.Items.FindAsync([place], ct);
            project = parent is null ? await db.Projects.FindAsync([place], ct) : await ProjectOfAsync(parent, ct);
        }
        project ??= req.Project is null ? null : await db.Projects.FindAsync([req.Project], ct);
        project ??= await db.Projects.OrderBy(p => p.Order).FirstAsync(ct);
        var it = await NewItemAsync(project, parent, req.Title, req.CoarseSize, actor, null, ct);
        await SaveAsync("item", it.Id, ct);
        return it;
    }

    private async Task<Item> NewItemAsync(Project project, Item? parent, string title, string? size, Actor actor, Session? s, CancellationToken ct)
    {
        string id;
        if (parent is null) { id = Ids.Child(project.Code, null, project.NextItemNumber); project.NextItemNumber++; }
        else
        {
            var n = await db.Items.CountAsync(i => i.ParentId == parent.Id, ct) + 1;
            while (await db.Items.AnyAsync(i => i.Id == Ids.Child(project.Code, parent.Id, n), ct) || db.Items.Local.Any(i => i.Id == Ids.Child(project.Code, parent.Id, n))) n++;
            id = Ids.Child(project.Code, parent.Id, n);
        }
        var order = parent is null
            ? await db.Items.CountAsync(i => i.ProjectId == project.Id && i.ParentId == null, ct) + db.Items.Local.Count(i => i.ProjectId == project.Id && i.ParentId == null && db.Entry(i).State == EntityState.Added)
            : await db.Items.CountAsync(i => i.ParentId == parent.Id, ct) + db.Items.Local.Count(i => i.ParentId == parent.Id && db.Entry(i).State == EntityState.Added);
        var it = new Item
        {
            Id = id, ProjectId = project.Id, ParentId = parent?.Id, Order = order, Title = title.Trim(),
            State = ItemState.Coarse, CoarseSize = string.IsNullOrWhiteSpace(size) ? "~1 week" : size!, Created = Now
        };
        var who = actor == Actor.Owner ? "You added the item" : $"Added from your chat by session {Short(s?.Id ?? "")}";
        it.HistoryJson = Json.Write(new List<HistoryLine> { new(fmt.DayTime(Now), "added", parent is null ? $"{who} under {project.Name}" : $"{who} under [[item:{parent.Id}]]") });
        db.Items.Add(it);
        Audit(actor, "add", "item", it.Id, it.Title);
        return it;
    }

    /// <summary>Claude writes the breakdown the owner dictated in chat: items under a parent, any depth, in one call.</summary>
    public async Task<List<Item>> AddItemsAsync(string sessionId, string? parentId, List<ItemInput> items, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        if (items.Count == 0) throw new WorkbenchRuleException("items.empty", "Nothing to add.");
        Item? parent = null; Project? project = null;
        if (parentId is { Length: > 0 })
        {
            parent = await db.Items.FindAsync([parentId], ct);
            project = parent is null ? await db.Projects.FindAsync([parentId], ct) ?? await db.Projects.FirstOrDefaultAsync(p => p.Code == parentId.ToUpperInvariant(), ct) : await ProjectOfAsync(parent, ct);
            if (project is null) throw new WorkbenchRuleException("item.parent", $"There is no item or project {parentId}.");
        }
        else if (s.ItemId is not null) { parent = await db.Items.FindAsync([s.ItemId], ct); project = parent is null ? null : await ProjectOfAsync(parent, ct); }
        else if (s.ProjectId is not null) project = await db.Projects.FindAsync([s.ProjectId], ct);
        if (project is null) throw new WorkbenchRuleException("item.parent", "Say which item or project the new items go under.");
        var made = new List<Item>();
        async Task WalkAsync(Item? under, List<ItemInput> list)
        {
            foreach (var x in list)
            {
                if (string.IsNullOrWhiteSpace(x.Title)) throw new WorkbenchRuleException("item.title", "Every item has a title.");
                var it = await NewItemAsync(project, under, x.Title, x.Size, Actor.Claude, s, ct);
                made.Add(it);
                if (x.Children is { Count: > 0 }) await WalkAsync(it, x.Children);
            }
        }
        await WalkAsync(parent, items);
        await LedgerAsync(s, "items", $"{made.Count} item{(made.Count == 1 ? "" : "s")} added under {parent?.Id ?? project.Code}");
        if (parent is not null) await HistoryAsync(parent.Id, "broken down", $"{made.Count} item{(made.Count == 1 ? "" : "s")} added from your chat", ct);
        await SaveAsync("item", made[0].Id, ct);
        return made;
    }

    /* ---------------------------------------------------------------- owner structure controls */

    public async Task<(Item item, string message)> PatchItemAsync(string id, ItemPatch patch, CancellationToken ct = default)
    {
        var it = await RequireItemAsync(id, ct);
        var message = "Saved";
        if (patch.Title is { Length: > 0 } t && t != it.Title) { await HistoryAsync(id, "renamed", $"You renamed it from “{it.Title}”", ct); it.Title = t.Trim(); message = "Renamed"; }
        if (patch.ParentId is not null && patch.ParentId != it.ParentId)
        {
            var np = patch.ParentId == "" ? null : await RequireItemAsync(patch.ParentId, ct);
            it.ParentId = np?.Id;
            if (np is not null) it.ProjectId = np.ProjectId;
            await HistoryAsync(id, "moved", np is null ? "You moved it to the top level" : $"You moved it under [[item:{np.Id}]]", ct);
            message = "Moved";
        }
        if (patch.Open == true) { var (opened, msg) = await OpenNodeAsync(it, ct); it = opened; message = msg; }
        Audit(Actor.Owner, "patch", "item", id);
        await SaveAsync("item", id, ct);
        return (it, message);
    }

    /// <summary>The owner said in chat which node to work on: the session opens it (and keeps the branch convention itself). Same rule as the tree: a sibling already open is the one that opens.</summary>
    public async Task<(Item item, string message)> OpenNodeFromChatAsync(string sessionId, string itemId, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        var wasOpen = it.IsOpen;
        var (opened, message) = await OpenNodeAsync(it, ct);
        if (opened.Id == it.Id && !wasOpen) { var h = List<HistoryLine>(it.HistoryJson); h[^1] = h[^1] with { Text = $"Opened from your chat by session {Short(s.Id)}" }; it.HistoryJson = Json.Write(h); }
        opened.HomeMachineId ??= s.MachineId;
        s.ItemId = (await DeepestOpenAsync(await TopOfAsync(opened, ct), ct)).Id;
        await LedgerAsync(s, "open", message);
        Audit(Actor.Claude, "open", "item", opened.Id);
        await SaveAsync("item", opened.Id, ct);
        var hold = await BranchOfAsync(opened, ct);
        return (opened, message + (hold.Recorded ? $" · on {hold.Branch} ({hold.Holder})" : $" · branch convention: {hold.Branch} ({hold.Holder}); create or check it out yourself and record it with branch_note"));
    }

    /// <summary>Opens a node. When a sibling is already open, that sibling is the one that opens: the owner is taken to it, nothing is refused.</summary>
    private async Task<(Item item, string message)> OpenNodeAsync(Item it, CancellationToken ct)
    {
        if (it.IsOpen) return (it, $"{it.Id} is already open");
        var siblings = await db.Items.Where(i => i.ParentId == it.ParentId && i.ProjectId == it.ProjectId).ToListAsync(ct);
        var clash = Rules.OpenClash(it, siblings);
        if (clash is not null) return (clash, $"{clash.Title} is open at this level; opened it");
        it.IsOpen = true;
        if (it.State == ItemState.Coarse) it.State = ItemState.Planning;
        var p = await ProjectOfAsync(it, ct);
        if (p.State == ProjectState.Coarse) p.State = ProjectState.Open;
        await HistoryAsync(it.Id, "opened", "You opened the node", ct);
        return (it, $"{it.Id} opened");
    }

    /* ---------------------------------------------------------------- what a session writes on its item */

    /// <summary>Claude's own steps for the item: as many as it needs, each with an estimate. They give the item its progress; no card.</summary>
    public async Task StepsWriteAsync(string sessionId, string itemId, List<StepInput> steps, string? goal, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        var old = List<Step>(it.StepsJson);
        var modelSteps = steps.Select((x, i) =>
        {
            var prev = old.FirstOrDefault(o => string.Equals(o.Text.Trim(), x.Text.Trim(), StringComparison.OrdinalIgnoreCase));
            return new Step(i + 1, x.Text.Trim(), x.EstMin, prev?.State ?? (i == 0 ? StepState.Current : StepState.Todo), null, x.Needs, prev?.Note, x.Proof);
        }).ToList();
        Rules.StepsHaveEstimates(modelSteps);
        it.StepsJson = Json.Write(modelSteps);
        it.EstMin = modelSteps.Sum(x => x.EstMin);
        if (!string.IsNullOrWhiteSpace(goal)) it.Goal = goal.Trim();
        if (it.State is ItemState.Coarse or ItemState.Planning or ItemState.Planned) it.State = ItemState.Building;
        it.HomeMachineId ??= s.MachineId;
        s.ItemId ??= it.Id;
        await LedgerAsync(s, "steps", $"{modelSteps.Count} steps · {it.EstMin} Claude-min");
        await HistoryAsync(it.Id, old.Count == 0 ? "steps" : "steps revised", $"{modelSteps.Count} steps, {it.EstMin} Claude-min, by [[session:{s.Id}]]", ct);
        Audit(Actor.Claude, "steps", "item", it.Id);
        await SaveAsync("item", it.Id, ct);
    }

    public async Task StepUpdateAsync(string sessionId, string itemId, int n, string state, string? note, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        var steps = List<Step>(it.StepsJson);
        var i = steps.FindIndex(x => x.N == n);
        if (i < 0) throw new WorkbenchRuleException("step.unknown", $"There is no step {n} on {itemId}. Write the steps first with steps_write.");
        if (!Enum.TryParse<StepState>(state, true, out var st)) throw new WorkbenchRuleException("step.state", "State is todo, current, done or blocked.");
        if (st == StepState.Current) for (var k = 0; k < steps.Count; k++) if (steps[k].State == StepState.Current) steps[k] = steps[k] with { State = StepState.Todo };
        steps[i] = steps[i] with { State = st, Note = note ?? steps[i].Note };
        it.StepsJson = Json.Write(steps);
        if (st == StepState.Current) { s.StepN = n; s.StepMin = 0; }
        if (it.State is ItemState.Planned or ItemState.Planning or ItemState.Coarse && st == StepState.Current) it.State = ItemState.Building;
        if (it.State == ItemState.Blocked && st != StepState.Blocked && steps.All(x => x.State != StepState.Blocked)) it.State = ItemState.Building;
        await LedgerAsync(s, "step", $"Step {n} {st.ToString().ToLowerInvariant()}: {steps[i].Text}");
        await SaveAsync("item", it.Id, ct);
    }

    /// <summary>The session finished the item: what is now true, in one sentence. Done is data; nothing waits on the owner.</summary>
    public async Task<string> ItemDoneAsync(string sessionId, string itemId, string outcome, string? note, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        if (await FindProjectAsync(itemId, ct) is { } proj) return await ProjectDoneAsync(s, proj, outcome, ct);
        var it = await RequireItemAsync(itemId, ct);
        Rules.TextFits(outcome, "The outcome");
        var steps = List<Step>(it.StepsJson).Select(st => st.State == StepState.Done ? st : st with { State = StepState.Done }).ToList();
        it.StepsJson = Json.Write(steps);
        it.Outcome = outcome.Trim();
        if (note is not null) { Rules.NoteFits(note); it.Note = note.Trim(); it.NoteAt = Now; it.NoteSessionId = s.Id; }
        it.State = ItemState.Done; it.IsOpen = false;
        it.SoMin = Math.Max(it.SoMin, await db.Sessions.Where(x => x.ItemId == it.Id).SumAsync(x => x.Minutes, ct));
        var factorNote = "";
        if (it.EstMin is int est && est > 0 && it.SoMin > 0)
        {
            it.Factor = Math.Round(it.SoMin / (double)est, 2);
            var p = await ProjectOfAsync(it, ct);
            var was = p.Factor;
            p.Factor = was is null ? Math.Round(it.Factor.Value, 1) : Math.Round((was.Value + it.Factor.Value) / 2, 1);
            factorNote = $" · {it.SoMin} min against {est} estimated";
        }
        await LedgerAsync(s, "done", $"{it.Id} done · {it.Outcome}");
        await HistoryAsync(it.Id, "done", $"{it.Outcome}{factorNote} · by [[session:{s.Id}]]", ct);
        Audit(Actor.Claude, "done", "item", it.Id);
        await SaveAsync("item", it.Id, ct);
        return $"{it.Id} is done{factorNote}";
    }

    public async Task NoteAsync(string sessionId, string itemId, string text, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        Rules.NoteFits(text);
        it.Note = text.Trim(); it.NoteAt = Now; it.NoteSessionId = s.Id;
        await LedgerAsync(s, "note", $"Note written ({Json.LineCount(text)} line{(Json.LineCount(text) == 1 ? "" : "s")})");
        await SaveAsync("item", it.Id, ct);
    }

    public async Task<Assumption> AssumeAsync(string sessionId, string itemId, string text, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        Rules.TextFits(text, "An assumption");
        var a = new Assumption { Id = "a-" + Ids.Short(), SessionId = s.Id, ItemId = it.Id, Text = text.Trim(), At = Now };
        db.Assumptions.Add(a);
        await LedgerAsync(s, "assume", text.Trim());
        await SaveAsync("assumption", a.Id, ct);
        return a;
    }

    /* ---------------------------------------------------------------- check results, per project */

    /// <summary>The last result of a check on a project, kept with the one before it so the next run can be compared. Tests run per project, never per item.</summary>
    public async Task<CheckResult> RecordResultAsync(ResultReport r, CancellationToken ct = default)
    {
        if (!Enum.TryParse<RunResult>(r.Result, true, out var res)) throw new WorkbenchRuleException("result.kind", "Result is pass, fail or warn.");
        Session? s = r.Session is null ? null : await db.Sessions.FindAsync([r.Session], ct);
        Item? it = (r.Item ?? s?.ItemId) is { } iid ? await db.Items.FindAsync([iid], ct) : null;
        Project? p = r.Project is null ? null : await db.Projects.FindAsync([r.Project], ct) ?? await db.Projects.FirstOrDefaultAsync(x => x.Code == r.Project.ToUpperInvariant(), ct);
        p ??= it is null ? null : await ProjectOfAsync(it, ct);
        p ??= await db.Projects.OrderBy(x => x.Order).FirstAsync(ct);
        var list = List<CheckResult>(p.ResultsJson);
        var prev = list.FirstOrDefault(x => x.Check == r.Check);
        var now = new CheckResult(r.Check, res.ToString().ToLowerInvariant(), r.Passed, r.Total, r.DurationSec, fmt.DayTime(Now), r.Cmd, s?.Id, it?.Id, prev is null ? null : prev with { Prev = null });
        list.RemoveAll(x => x.Check == r.Check);
        list.Insert(0, now);
        p.ResultsJson = Json.Write(list);
        if (s is not null) await LedgerAsync(s, "check", $"{r.Check} {now.Result}{(r.Total > 0 ? $" {r.Passed}/{r.Total}" : "")}" + (prev is null ? "" : $" (was {prev.Result}{(prev.Total > 0 ? $" {prev.Passed}/{prev.Total}" : "")})"));
        await SaveAsync("project", p.Id, ct);
        return now;
    }
}
