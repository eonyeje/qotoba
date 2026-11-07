using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

public sealed partial class WorkbenchOps
{
    /* ---------------------------------------------------------------- owner structure controls */

    public async Task<Item> AddItemAsync(AddItemRequest req, Actor actor, CancellationToken ct = default)
    {
        Rules.OnlyOwnerCreatesStructure(actor, "task");
        if (!req.Kind.Equals("task", StringComparison.OrdinalIgnoreCase)) throw new WorkbenchRuleException("item.kind", "FTR, BUG and PER are reports: use the report endpoint.");
        if (string.IsNullOrWhiteSpace(req.Title)) throw new WorkbenchRuleException("item.title", "A title, in your words.");
        Item? parent = null; Project? project = null;
        if (req.Place is { Length: > 0 } place)
        {
            parent = await db.Items.FindAsync([place], ct);
            project = parent is null ? await db.Projects.FindAsync([place], ct) : await ProjectOfAsync(parent, ct);
        }
        project ??= req.Project is null ? null : await db.Projects.FindAsync([req.Project], ct);
        project ??= await db.Projects.OrderBy(p => p.Order).FirstAsync(ct);

        string id;
        if (parent is null) { id = Ids.Child(project.Code, null, project.NextItemNumber); project.NextItemNumber++; }
        else
        {
            var n = await db.Items.CountAsync(i => i.ParentId == parent.Id, ct) + 1;
            while (await db.Items.AnyAsync(i => i.Id == Ids.Child(project.Code, parent.Id, n), ct)) n++;
            id = Ids.Child(project.Code, parent.Id, n);
        }
        var order = parent is null
            ? await db.Items.CountAsync(i => i.ProjectId == project.Id && i.ParentId == null, ct)
            : await db.Items.CountAsync(i => i.ParentId == parent.Id, ct);
        var it = new Item
        {
            Id = id, ProjectId = project.Id, ParentId = parent?.Id, Order = order, Title = req.Title.Trim(),
            State = ItemState.Coarse, CoarseSize = string.IsNullOrWhiteSpace(req.CoarseSize) ? "~1 week" : req.CoarseSize!, Created = Now,
            ImplementsJson = Json.Write((req.Implements ?? []).Select(r => r.StartsWith("fpb:") ? r : "fpb:" + r).ToList())
        };
        it.HistoryJson = Json.Write(new List<HistoryLine> { new(fmt.DayTime(Now), "opened", parent is null ? $"You added the item under {project.Name}" : $"You added the item under [[item:{parent.Id}]]") });
        db.Items.Add(it);
        if (req.FromFound is not null && await db.Founds.FindAsync([req.FromFound], ct) is { } found) { found.State = FoundState.Task; found.PromotedTo = "item:" + it.Id; }
        Audit(Actor.Owner, "add", "item", it.Id, it.Title);
        await SaveAsync("item", it.Id, ct);
        return it;
    }

    public async Task<Item> PatchItemAsync(string id, ItemPatch patch, CancellationToken ct = default)
    {
        var it = await RequireItemAsync(id, ct);
        if (patch.Title is { Length: > 0 } t && t != it.Title) { await HistoryAsync(id, "renamed", $"You renamed it from “{it.Title}”", ct); it.Title = t.Trim(); }
        if (patch.ParentId is not null && patch.ParentId != it.ParentId)
        {
            var np = patch.ParentId == "" ? null : await RequireItemAsync(patch.ParentId, ct);
            it.ParentId = np?.Id;
            if (np is not null) it.ProjectId = np.ProjectId;
            await HistoryAsync(id, "moved", np is null ? "You moved it to the top level" : $"You moved it under [[item:{np.Id}]]", ct);
        }
        if (patch.Open == true) await OpenNodeAsync(it, ct);
        Audit(Actor.Owner, "patch", "item", id);
        await SaveAsync("item", id, ct);
        return it;
    }

    private async Task OpenNodeAsync(Item it, CancellationToken ct)
    {
        var siblings = await db.Items.Where(i => i.ParentId == it.ParentId && i.ProjectId == it.ProjectId).ToListAsync(ct);
        Rules.MayOpen(it, siblings);
        it.IsOpen = true;
        if (it.State == ItemState.Coarse) it.State = ItemState.Planning;
        var p = await ProjectOfAsync(it, ct);
        if (p.State == ProjectState.Coarse) p.State = ProjectState.Open;
        await HistoryAsync(it.Id, "opened", "You opened the node", ct);
    }

    /* ---------------------------------------------------------------- Claude's slots */

    public async Task<Card> SubmitPlanAsync(string sessionId, string itemId, List<StepInput> steps, List<SettleInput>? settle, string goal, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        var modelSteps = steps.Select((x, i) => new Step(i + 1, x.Text, x.EstMin, StepState.Todo, x.Proof, x.Needs, null)).ToList();
        Rules.PlanFits(modelSteps);
        var p = await ProjectOfAsync(it, ct);
        var factor = it.Factor ?? p.Factor ?? 1.2;
        var own = (settle ?? []).Select((q, i) => new SettleQ("s" + (i + 1), q.Question, q.Options, q.Recommended, q.FromReport, null)).ToList();
        var fromReports = await DrawSettleFromReportsAsync(it, own.Count, ct);
        var fromEdges = await DrawSettleFromEdgesAsync(it, own.Count + fromReports.Count, ct);
        var top = await TopOfAsync(it, ct);
        var payload = new PlanPayload
        {
            Steps = steps.Select(x => new PlanStep(x.Text, x.EstMin, x.Needs, x.Proof)).ToList(),
            Settle = own.Concat(fromReports).Concat(fromEdges).ToList(),
            Branch = top.Branch ?? BranchNameFor(p, top), Checkout = top.Checkout ?? s.Cwd, Factor = factor, Goal = goal, Machine = s.MachineId,
            FactorNote = p.Factor is null ? "the programme default 1.2 applies" : null
        };
        var total = steps.Sum(x => x.EstMin);
        var card = NewCard(CardType.Plan, $"Plan: {it.Title.ToLowerInvariant()}, {steps.Count} step{(steps.Count == 1 ? "" : "s")}", it.Id, s.Id);
        card.ProjectId = it.ProjectId;
        card.WeightMin = (int)Math.Round(total * factor);
        card.Why = $"{total} Claude-min, about {Fmt.Minutes(card.WeightMin)} at factor {factor:0.0}." + (payload.Settle.Count > 0 ? $" {payload.Settle.Count} settle question{(payload.Settle.Count == 1 ? " has a default" : "s have defaults")}." : "");
        card.OriginStep = "plan, before step 1";
        card.OriginFromJson = Json.Write(new List<string> { "session:" + s.Id });
        card.PayloadJson = Json.Write(payload);
        db.Cards.Add(card);
        it.PlanCardId = card.Id;
        it.EstMin = total;
        it.Goal = string.IsNullOrWhiteSpace(it.Goal) ? goal : it.Goal;
        if (it.State == ItemState.Coarse) it.State = ItemState.Planning;
        s.ItemId ??= it.Id;
        await LedgerAsync(s, "plan", $"Plan submitted · {steps.Count} steps · {card.Id}");
        await HistoryAsync(it.Id, "plan submitted", $"[[card:{card.Id}]] by [[session:{s.Id}]]", ct);
        Audit(Actor.Claude, "plan", "card", card.Id, it.Id);
        await SaveAsync("card", card.Id, ct);
        return card;
    }

    public async Task StepUpdateAsync(string sessionId, string itemId, int n, string state, string? proofRef, string? note, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        var steps = List<Step>(it.StepsJson);
        var i = steps.FindIndex(x => x.N == n);
        if (i < 0) throw new WorkbenchRuleException("step.unknown", $"There is no step {n} on {itemId}. Steps come from the approved plan.");
        if (!Enum.TryParse<StepState>(state, true, out var st)) throw new WorkbenchRuleException("step.state", "State is todo, current, done or blocked.");
        if (st == StepState.Done && string.IsNullOrWhiteSpace(proofRef) && string.IsNullOrWhiteSpace(steps[i].ProofRef))
            throw new WorkbenchRuleException("step.proof", "A step is done when a run proves it. Pass the run id as proof, or leave the step current.");
        if (st == StepState.Current) for (var k = 0; k < steps.Count; k++) if (steps[k].State == StepState.Current) steps[k] = steps[k] with { State = StepState.Todo };
        steps[i] = steps[i] with { State = st, ProofRef = proofRef is null ? steps[i].ProofRef : NormaliseRunRef(proofRef), Note = note ?? steps[i].Note };
        it.StepsJson = Json.Write(steps);
        if (st == StepState.Current) { s.StepN = n; s.StepMin = 0; }
        if (it.State is ItemState.Planned or ItemState.Planning && st == StepState.Current) it.State = ItemState.Building;
        await LedgerAsync(s, "step", $"Step {n} {st.ToString().ToLowerInvariant()}: {steps[i].Text}");
        await SaveAsync("item", it.Id, ct);
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

    public async Task ClaimAsync(string sessionId, string itemId, string text, string? runId, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        Rules.TextFits(text, "A claim");
        var claims = List<Claim>(it.ClaimsJson);
        string? proof = null;
        if (runId is not null)
        {
            var id = runId.Replace("run:", "");
            if (!await db.Runs.AnyAsync(r => r.Id == id, ct)) throw new WorkbenchRuleException("run.unknown", $"No run {id} is recorded. Claims cite runs the Workbench saw.");
            proof = "run:" + id;
        }
        claims.Add(new Claim(text.Trim(), proof));
        it.ClaimsJson = Json.Write(claims);
        await LedgerAsync(s, "claim", (proof is null ? "unverified: " : "") + text.Trim());
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

    public async Task<Found> FoundAsync(string sessionId, string itemId, string kind, string title, string why, string? evidenceRef, string? suggestItem, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        var count = await db.Founds.CountAsync(f => f.SessionId == s.Id, ct);
        Rules.FoundMayBeAdded(count);
        if (!Enum.TryParse<ReportKind>(kind, true, out var k)) throw new WorkbenchRuleException("found.kind", "Kind is FTR, BUG or PER.");
        Rules.TextFits(why, "Why it matters");
        var f = new Found { Id = "f-" + Ids.Short(), SessionId = s.Id, ItemId = it.Id, Kind = k, Title = title.Trim(), Why = why.Trim(), EvidenceRef = evidenceRef, SuggestItemId = suggestItem, At = Now };
        db.Founds.Add(f);
        await LedgerAsync(s, "found", $"{title.Trim()} · {f.Id}");
        await SaveAsync("found", f.Id, ct);
        return f;
    }

    public async Task<Run> RecordRunAsync(RunReport r, CancellationToken ct = default)
    {
        if (!Enum.TryParse<RunResult>(r.Result, true, out var res)) throw new WorkbenchRuleException("run.result", "Result is pass, fail or warn.");
        var tail = r.OutputTail.Length > Caps.RunOutputBytes ? r.OutputTail[^Caps.RunOutputBytes..] : r.OutputTail;
        var run = new Run
        {
            Id = Ids.Short(), Check = r.Check, ItemId = r.Item, SessionId = r.Session, MachineId = r.Machine, Result = res,
            Passed = r.Passed, Total = r.Total, DurationSec = r.DurationSec, At = Now, Cmd = r.Cmd, OutputTail = tail,
            Attempt = r.Attempt, ExitCode = r.ExitCode, Expected = r.Expected, Value = r.Value, StepN = r.Step
        };
        db.Runs.Add(run);
        if (r.Session is not null && await db.Sessions.FindAsync([r.Session], ct) is { } s)
            await LedgerAsync(s, "check", $"{r.Check} {res.ToString().ToLowerInvariant()}{(r.Total > 0 ? $" {r.Passed}/{r.Total}" : "")} · run {run.Id}");
        await SaveAsync("run", run.Id, ct);
        return run;
    }

    private static string NormaliseRunRef(string r) => r.StartsWith("run:") || r.StartsWith("nyw:") ? r : "run:" + r;
}
