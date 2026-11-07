using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

public sealed partial class WorkbenchOps
{
    /// <summary>The owner pressed something on a card. The only path by which anything is answered.</summary>
    public async Task<PressResult> PressAsync(PressRequest req, CancellationToken ct = default)
    {
        var card = await RequireCardAsync(req.Card, ct);
        if (req.Action == "write" && card.Type != CardType.Question)
        {
            if (string.IsNullOrWhiteSpace(req.Text)) return new PressResult(false, "Write a line first.");
            await TellSessionAsync(card, req.Text.Trim(), ct);
            Audit(Actor.Owner, "write", "card", card.Id, req.Text);
            await SaveAsync("card", card.Id, ct);
            return new PressResult(true, card.SessionId is null ? "Noted" : $"Sent to session {Short(card.SessionId)}");
        }
        var result = card.Type switch
        {
            CardType.Question => await PressQuestionAsync(card, req, ct),
            CardType.Packet => await PressPacketAsync(card, req, ct),
            CardType.Plan => await PressPlanAsync(card, req, ct),
            CardType.Stuck => await PressStuckAsync(card, req, ct),
            CardType.Proposals => await PressProposalsAsync(card, req, ct),
            CardType.Info => await PressInfoAsync(card, req, ct),
            _ => new PressResult(false, "Unknown card kind.")
        };
        Audit(Actor.Owner, req.Action, "card", card.Id, req.Value ?? req.Text);
        await SaveAsync("card", card.Id, ct);
        return result;
    }

    private async Task<Session?> SessionOf(Card c, CancellationToken ct) => c.SessionId is null ? null : await db.Sessions.FindAsync([c.SessionId], ct);

    private async Task TellSessionAsync(Card c, string text, CancellationToken ct)
    {
        var s = await SessionOf(c, ct);
        if (s is null) return;
        db.Messages.Add(new OwnerMessage { SessionId = s.Id, CardId = c.Id, Text = text, At = Now });
        if (!await db.Cards.AnyAsync(x => x.SessionId == s.Id && x.State == CardState.Open && x.Type != CardType.Info && x.Id != c.Id, ct))
        { s.State = SessionState.Working; s.WaitingSince = null; s.Stuck = false; }
    }

    private async Task<PressResult> PressQuestionAsync(Card card, PressRequest req, CancellationToken ct)
    {
        var q = card.QuestionId is null ? null : await db.Questions.FindAsync([card.QuestionId], ct);
        if (q is null) return new PressResult(false, "The card has no question.");
        var opts = List<QuestionOption>(q.OptionsJson);
        var s = await SessionOf(card, ct);
        switch (req.Action)
        {
            case "pick":
            case "recommended":
            {
                var idx = req.Action == "recommended" ? opts.FindIndex(o => o.Rec) : (req.Index ?? -1);
                if (idx < 0 || idx >= opts.Count) return new PressResult(false, "Pick one of the options.");
                return await AnswerAsync(card, q, opts[idx].T, req.Action == "recommended" ? "Assumed the recommended option" : "Picked", ct);
            }
            case "write":
                if (string.IsNullOrWhiteSpace(req.Text)) return new PressResult(false, "Say it in your own words first.");
                return await AnswerAsync(card, q, req.Text.Trim(), "Answered in your words", ct);
            case "defer":
                card.State = CardState.Deferred; card.DeferredUntil = Now.AddHours(24);
                return new PressResult(true, $"Deferred for 24 hours · it comes back on {fmt.Day(Now.AddHours(24))}");
            case "talk":
                return new PressResult(true, "Talk opens a design chat with this card's brief. Chat never writes to the Workbench.");
            default: return new PressResult(false, "Unknown action " + req.Action);
        }

        async Task<PressResult> AnswerAsync(Card c, Question qq, string answer, string how, CancellationToken k)
        {
            qq.State = QuestionState.Answered; qq.Answer = answer; qq.AnsweredAt = Now;
            c.State = CardState.Answered; c.Answer = answer; c.AnsweredAt = Now;
            var it = await db.Items.FindAsync([qq.ItemId], k);
            if (it is not null && qq.StepN is int n)
            {
                var steps = List<Step>(it.StepsJson);
                var i = steps.FindIndex(x => x.N == n);
                if (i >= 0 && steps[i].State == StepState.Blocked) { steps[i] = steps[i] with { State = StepState.Current }; it.StepsJson = Json.Write(steps); }
                if (it.State == ItemState.Blocked) it.State = ItemState.Building;
                await HistoryAsync(it.Id, "question answered", $"[[question:{qq.Id}]]: {answer}", k);
            }
            await TellSessionAsync(c, $"Answer to {qq.Id} ({qq.Text}): {answer}", k);
            return new PressResult(true, $"{how} · sent to session {(s is null ? "" : Short(s.Id))} on {s?.MachineId}");
        }
    }

    private async Task<PressResult> PressPacketAsync(Card card, PressRequest req, CancellationToken ct)
    {
        var pk = Json.Read(card.PayloadJson, new PacketPayload());
        var it = card.ItemId is null ? null : await db.Items.FindAsync([card.ItemId], ct);
        switch (req.Action)
        {
            case "approve":
            {
                if (pk.Approved) return new PressResult(false, "Already approved.");
                var machine = pk.Machine ?? options.MachineId;
                var git = await host.RunGitAsync(machine, new GitInstruction(pk.Checkout ?? "", pk.Instruction), ct);
                pk.GitOutput = git.Output;
                if (!git.Ok)
                {
                    card.PayloadJson = Json.Write(pk);
                    if (it is not null) await HistoryAsync(it.Id, "approve failed", "The Host reported: " + Trim(git.Output, 200), ct);
                    return new PressResult(false, "The Host could not run the instruction: " + Trim(git.Output, 160));
                }
                pk.Approved = true; pk.Merged = true;
                var sha = System.Text.RegularExpressions.Regex.Match(git.Output, @"\b[0-9a-f]{7,40}\b");
                if (sha.Success)
                {
                    pk.MergedCommit = sha.Value[..Math.Min(7, sha.Value.Length)];
                    if (await db.Commits.FindAsync([pk.MergedCommit], ct) is null)
                        db.Commits.Add(new Commit { Id = pk.MergedCommit, Msg = "Merge " + (it?.Id ?? "") + " " + (it?.Title ?? ""), Date = Now, SessionId = card.SessionId, FilesJson = Json.Write(pk.Files), Plus = pk.Plus, Minus = pk.Minus, DiffJson = Json.Write(git.Output.Replace("\r\n", "\n").Split('\n').TakeLast(40).ToList()) });
                }
                card.PayloadJson = Json.Write(pk);
                card.State = CardState.Approved; card.AnsweredAt = Now; card.Answer = "approved";
                if (it is not null)
                {
                    it.State = ItemState.Done; it.IsOpen = false; it.SoMin = Math.Max(it.SoMin, pk.ActualMin);
                    var steps = List<Step>(it.StepsJson).Select(st => st.State == StepState.Done ? st : st with { State = StepState.Done }).ToList();
                    it.StepsJson = Json.Write(steps);
                    await HistoryAsync(it.Id, "packet approved", pk.MergedCommit is null ? "Merged" : $"Merged as [[commit:{pk.MergedCommit}]]", ct);
                    await RecomputeFactorAsync(it, pk, ct);
                }
                await TellSessionAsync(card, "Packet approved and merged by the owner. Nothing further runs on this branch.", ct);
                return new PressResult(true, $"Instruction sent to the Host on {machine}" + (pk.MergedCommit is null ? "" : $" · merged as {pk.MergedCommit}"));
            }
            case "changes":
                if (string.IsNullOrWhiteSpace(req.Text)) return new PressResult(false, "Say what to change.");
                await TellSessionAsync(card, "Changes asked on the packet: " + req.Text.Trim(), ct);
                if (it is not null) { it.State = ItemState.Building; await HistoryAsync(it.Id, "changes asked", req.Text.Trim(), ct); }
                card.State = CardState.Answered; card.Answer = "changes: " + req.Text.Trim(); card.AnsweredAt = Now;
                return new PressResult(true, "Sent to the session; the packet comes back when it is ready");
            case "all-fine":
            {
                var list = await db.Assumptions.Where(a => a.SessionId == card.SessionId && a.State == AssumptionState.Unreviewed).ToListAsync(ct);
                foreach (var a in list) a.State = AssumptionState.Fine;
                return new PressResult(true, $"All {list.Count} fine");
            }
            case "flip":
            {
                var a = req.Target is null ? null : await db.Assumptions.FindAsync([req.Target], ct);
                if (a is null) return new PressResult(false, "Which assumption?");
                a.State = AssumptionState.Flipped;
                await TellSessionAsync(card, "Assumption flipped by the owner (do the opposite): " + a.Text, ct);
                return new PressResult(true, "Flipped · the session is told to do the opposite");
            }
            case "found-task":
            case "found-later":
            case "found-drop":
            {
                var f = req.Target is null ? null : await db.Founds.FindAsync([req.Target], ct);
                if (f is null) return new PressResult(false, "Which found item?");
                f.State = req.Action switch { "found-task" => FoundState.Task, "found-later" => FoundState.Later, _ => FoundState.Dropped };
                return new PressResult(true, req.Action switch { "found-task" => "Add it where you want it in the tree", "found-later" => $"{f.Title} kept for later", _ => $"{f.Title} dropped" },
                    req.Action == "found-task" ? "#/work?sheet=add&found=" + f.Id : null);
            }
            default: return new PressResult(false, "Unknown action " + req.Action);
        }
    }

    private async Task RecomputeFactorAsync(Item it, PacketPayload pk, CancellationToken ct)
    {
        if (pk.EstMin <= 0 || pk.ActualMin <= 0) return;
        var f = Math.Round(pk.ActualMin / (double)pk.EstMin, 2);
        it.Factor = f;
        if (it.LaneKey is { } lk && await db.Lanes.FirstOrDefaultAsync(l => l.ProjectId == it.ProjectId && l.Key == lk, ct) is { } lane)
        {
            var was = lane.Factor;
            lane.Was = was; lane.Factor = Math.Round((was + f) / 2, 1);
            if (Math.Abs(lane.Factor - was) >= 0.1)
                await InfoAsync($"Lane {lk} factor is now {lane.Factor:0.0} (was {was:0.0})",
                    $"{it.Id} took {pk.ActualMin} minutes against {pk.EstMin} Claude-minutes. Lane {lk} forecasts now use {lane.Factor:0.0}.",
                    "lane:" + lk, "Computed by the Workbench after the packet was approved", it.Id, null, null, ct);
        }
    }

    private async Task<PressResult> PressPlanAsync(Card card, PressRequest req, CancellationToken ct)
    {
        var pl = Json.Read(card.PayloadJson, new PlanPayload());
        var it = card.ItemId is null ? null : await db.Items.FindAsync([card.ItemId], ct);
        if (it is null) return new PressResult(false, "The plan has no item.");
        switch (req.Action)
        {
            case "settle-all":
                foreach (var q in pl.Settle.ToList()) pl.Settle[pl.Settle.IndexOf(q)] = q with { Chosen = q.Rec };
                pl.Settled = true;
                card.PayloadJson = Json.Write(pl);
                return new PressResult(true, $"{pl.Settle.Count} defaults accepted");
            case "settle":
            {
                if (req.Target is null || req.Index is null) return new PressResult(false, "Which question and option?");
                var i = pl.Settle.FindIndex(q => q.Id == req.Target);
                if (i < 0) return new PressResult(false, "Unknown settle question.");
                pl.Settle[i] = pl.Settle[i] with { Chosen = req.Index };
                card.PayloadJson = Json.Write(pl);
                return new PressResult(true, "Chosen");
            }
            case "approve":
            {
                if (pl.Approved) return new PressResult(false, "Already approved.");
                foreach (var q in pl.Settle.ToList()) if (q.Chosen is null) pl.Settle[pl.Settle.IndexOf(q)] = q with { Chosen = q.Rec };
                pl.Settled = true; pl.Approved = true; pl.ApprovedAt = fmt.DayTime(Now);
                card.PayloadJson = Json.Write(pl);
                card.State = CardState.Approved; card.Answer = "approved"; card.AnsweredAt = Now;
                it.StepsJson = Json.Write(pl.Steps.Select((s, i) => new Step(i + 1, s.T, s.Est, i == 0 ? StepState.Current : StepState.Todo, s.Proof is { Length: > 0 } p && (p.StartsWith("run:") || p.StartsWith("nyw:")) ? p : null, s.Needs, null, s.Proof)).ToList());
                it.EstMin = pl.Steps.Sum(s => s.Est);
                it.Factor ??= pl.Factor;
                it.State = ItemState.Building;
                it.IsOpen = true;
                if (pl.Lane is { } lane) { it.LaneKey = lane; await EnsureLaneAsync(it.ProjectId, lane, pl.Machine, ct); }
                var p = await ProjectOfAsync(it, ct);
                if (p.State == ProjectState.Coarse) p.State = ProjectState.Open;
                foreach (var q in pl.Settle)
                {
                    var choice = q.Chosen is int c && c >= 0 && c < q.Options.Count ? q.Options[c] : q.Options[q.Rec];
                    await HistoryAsync(it.Id, "settled", $"{q.Q} → {choice}" + (q.From is null ? "" : $" (from [[{q.From}]])"), ct);
                }
                await HistoryAsync(it.Id, "plan approved", $"{pl.Steps.Count} steps, {it.EstMin} Claude-min", ct);
                var settled = string.Join("\n", pl.Settle.Select(q => $"- {q.Q}: {(q.Chosen is int c2 && c2 < q.Options.Count ? q.Options[c2] : q.Options[q.Rec])}"));
                await TellSessionAsync(card, "Plan approved by the owner. Start step 1." + (settled.Length > 0 ? "\nSettled defaults:\n" + settled : ""), ct);
                return new PressResult(true, $"Plan approved · {it.Id} opens" + (pl.Lane is null ? "" : $" in lane {pl.Lane}") + (pl.Machine is null ? "" : $" on {pl.Machine}"));
            }
            case "change":
                if (string.IsNullOrWhiteSpace(req.Text)) return new PressResult(false, "Say what to change.");
                await TellSessionAsync(card, "Change the plan: " + req.Text.Trim(), ct);
                card.State = CardState.Answered; card.Answer = "change: " + req.Text.Trim(); card.AnsweredAt = Now;
                return new PressResult(true, "Sent to the session; a new plan card comes back");
            default: return new PressResult(false, "Unknown action " + req.Action);
        }
    }

    private async Task EnsureLaneAsync(string projectId, string key, string? machine, CancellationToken ct)
    {
        var id = projectId + ":" + key;
        var lane = await db.Lanes.FindAsync([id], ct);
        if (lane is null) db.Lanes.Add(new Lane { Id = id, ProjectId = projectId, Key = key, Name = key, Factor = 1.2, MachineId = machine, State = "open", Proposed = false });
        else { lane.MachineId ??= machine; lane.Proposed = false; }
    }

    private async Task<PressResult> PressStuckAsync(Card card, PressRequest req, CancellationToken ct)
    {
        var st = Json.Read(card.PayloadJson, new StuckPayload());
        var it = card.ItemId is null ? null : await db.Items.FindAsync([card.ItemId], ct);
        switch (req.Action)
        {
            case "reassign":
                if (req.Value is null || it is null) return new PressResult(false, "Which machine?");
                it.HomeMachineId = req.Value;
                card.State = CardState.Answered; card.Answer = "reassign " + req.Value; card.AnsweredAt = Now;
                await TellSessionAsync(card, $"The owner moved {it.Id} to {req.Value}. Stop here; a session on {req.Value} continues.", ct);
                await HistoryAsync(it.Id, "reassigned", $"You moved it to [[machine:{req.Value}]] (one home machine)", ct);
                return new PressResult(true, $"{it.Id} moves to {req.Value}: worktree recreated there (one home machine)");
            case "retry":
                card.State = CardState.Answered; card.Answer = "retry"; card.AnsweredAt = Now;
                if (it is not null && it.State == ItemState.Blocked) it.State = ItemState.Building;
                await TellSessionAsync(card, $"One more attempt allowed on step {st.Step}. If it fails again, report stuck and stop.", ct);
                return new PressResult(true, $"One more attempt allowed · the session retries step {st.Step}");
            case "drop":
                card.State = CardState.Answered; card.Answer = "drop"; card.AnsweredAt = Now;
                if (it is not null)
                {
                    var steps = List<Step>(it.StepsJson);
                    var i = steps.FindIndex(x => x.N == st.Step);
                    if (i >= 0) { steps[i] = steps[i] with { State = StepState.Done, Note = "dropped by the owner" }; it.StepsJson = Json.Write(steps); }
                    it.State = ItemState.Building;
                    await HistoryAsync(it.Id, "step dropped", $"You dropped step {st.Step}", ct);
                }
                await TellSessionAsync(card, $"Step {st.Step} is dropped by the owner. Move to the next step.", ct);
                return new PressResult(true, $"Step {st.Step} dropped · the session moves on");
            default: return new PressResult(false, "Unknown action " + req.Action);
        }
    }

    private async Task<PressResult> PressProposalsAsync(Card card, PressRequest req, CancellationToken ct)
    {
        var props = await db.Proposals.Where(p => p.CardId == card.Id).OrderBy(p => p.Id).ToListAsync(ct);
        switch (req.Action)
        {
            case "tick":
            {
                var p = props.FirstOrDefault(x => x.Id == req.Target);
                if (p is null) return new PressResult(false, "Which line?");
                p.Ticked = req.Value != "false";
                return new PressResult(true, p.Ticked ? "Ticked" : "Unticked");
            }
            case "record":
            {
                var n = await db.Rulings.CountAsync(ct);
                var made = 0;
                foreach (var p in props.Where(x => x.Ticked && x.RulingId is null))
                {
                    var r = new Ruling { Id = Ids.Ruling(++n), ProjectId = card.ProjectId, Title = Trim(p.Text, 60), Text = p.Text, Date = Now, Origin = "from a chat proposal", ItemId = card.ItemId };
                    db.Rulings.Add(r); p.RulingId = r.Id; made++;
                }
                card.State = CardState.Answered; card.Answer = $"recorded {made}"; card.AnsweredAt = Now;
                return new PressResult(true, $"{made} ruling{(made == 1 ? "" : "s")} recorded in your words");
            }
            case "discard":
                card.State = CardState.Dismissed; card.AnsweredAt = Now;
                return new PressResult(true, "Batch discarded · the chat stays in claude.ai");
            default: return new PressResult(false, "Unknown action " + req.Action);
        }
    }

    private async Task<PressResult> PressInfoAsync(Card card, PressRequest req, CancellationToken ct)
    {
        var info = Json.Read(card.PayloadJson, new InfoPayload());
        switch (req.Action)
        {
            case "dismiss":
                card.State = CardState.Dismissed; card.AnsweredAt = Now;
                return new PressResult(true, "Dismissed");
            case "note":
                if (string.IsNullOrWhiteSpace(req.Text)) return new PressResult(false, "Write the note first.");
                await TellSessionAsync(card, "Note from the owner: " + req.Text.Trim(), ct);
                return new PressResult(true, card.SessionId is null ? "Noted" : "Sent to the session");
            case "make-ruling":
            {
                var text = req.Text?.Trim() is { Length: > 0 } t ? t : info.Ruling;
                if (string.IsNullOrWhiteSpace(text)) return new PressResult(false, "There is nothing to record.");
                var n = await db.Rulings.CountAsync(ct) + 1;
                var r = new Ruling { Id = Ids.Ruling(n), ProjectId = card.ProjectId, Title = Trim(text, 60), Text = text, Date = Now, Origin = "set by you", ItemId = card.ItemId };
                db.Rulings.Add(r);
                card.RulingId = r.Id; card.State = CardState.Dismissed; card.AnsweredAt = Now;
                return new PressResult(true, $"Recorded as {r.Id}", "#/ruling/" + r.Id);
            }
            default: return new PressResult(false, "Unknown action " + req.Action);
        }
    }

    public async Task<Ruling> AddRulingAsync(RulingRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Text)) throw new WorkbenchRuleException("ruling.text", "A ruling is in your words.");
        var n = await db.Rulings.CountAsync(ct) + 1;
        var r = new Ruling { Id = Ids.Ruling(n), ProjectId = req.Project, Title = string.IsNullOrWhiteSpace(req.Title) ? Trim(req.Text, 60) : req.Title.Trim(), Text = req.Text.Trim(), Date = Now, Origin = "set by you" };
        db.Rulings.Add(r);
        Audit(Actor.Owner, "add", "ruling", r.Id);
        await SaveAsync("ruling", r.Id, ct);
        return r;
    }

    public async Task MessageSessionAsync(string sessionId, string text, CancellationToken ct = default)
    {
        var s = await db.Sessions.FindAsync([sessionId], ct) ?? throw new WorkbenchRuleException("session.unknown", "No such session.");
        db.Messages.Add(new OwnerMessage { SessionId = s.Id, Text = text.Trim(), At = Now });
        Audit(Actor.Owner, "message", "session", s.Id);
        await SaveAsync("session", s.Id, ct);
    }

    public async Task<Machine> RegisterMachineAsync(MachineRegistration reg, CancellationToken ct = default)
    {
        var m = await db.Machines.FindAsync([reg.Id], ct);
        var isNew = m is null;
        if (m is null) { m = new Machine { Id = reg.Id, Registered = Now }; db.Machines.Add(m); }
        m.Os = reg.Os; m.HostVersion = reg.HostVersion; m.Online = true; m.LastSeen = Now; m.HostUrl = reg.HostUrl;
        m.Kind = reg.Kind.Equals("cloud", StringComparison.OrdinalIgnoreCase) ? MachineKind.Cloud : MachineKind.Machine;
        m.ClockSkewSec = Math.Round((reg.Clock - Now).TotalSeconds, 1);
        m.CheckoutsJson = Json.Write(reg.Checkouts.Select(c => new Checkout(c.Path, c.Branch, c.Item, c.Worktree, c.Note)).ToList());
        if (isNew) await InfoAsync($"{m.Id} registered with Host {reg.HostVersion}", $"A Host joined on {reg.Os} with {reg.Checkouts.Count} checkout{(reg.Checkouts.Count == 1 ? "" : "s")}.", "machine:" + m.Id, $"Host on {m.Id}", null, null, null, ct);
        await SaveAsync("machine", m.Id, ct);
        return m;
    }

    private static string Trim(string s, int n) => s.Length <= n ? s : s[..(n - 1)].TrimEnd() + "…";
}
