using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

public sealed partial class WorkbenchOps
{
    /// <summary>The owner pressed something on a card. Every card also takes the owner's own words with "write".</summary>
    public async Task<PressResult> PressAsync(PressRequest req, CancellationToken ct = default)
    {
        var card = await RequireCardAsync(req.Card, ct);
        var result = card.Type switch
        {
            CardType.Page => await PressPageAsync(card, req, ct),
            CardType.Deviation => await PressDeviationAsync(card, req, ct),
            CardType.Stuck => await PressStuckAsync(card, req, ct),
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

    private async Task<PressResult> WriteAsync(Card card, string? text, string prefix, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return new PressResult(false, "Write a line first.");
        await TellSessionAsync(card, prefix + text.Trim(), ct);
        card.State = CardState.Answered; card.Answer = text.Trim(); card.AnsweredAt = Now;
        return new PressResult(true, card.SessionId is null ? "Noted" : $"Sent to session {Short(card.SessionId)}");
    }

    private async Task<PressResult> PressPageAsync(Card card, PressRequest req, CancellationToken ct)
    {
        var pg = Json.Read(card.PayloadJson, new PagePayload());
        var it = card.ItemId is null ? null : await db.Items.FindAsync([card.ItemId], ct);
        switch (req.Action)
        {
            case "approve":
            {
                if (pg.Approved) return new PressResult(false, "Already approved.");
                pg.Approved = true; pg.ApprovedAt = fmt.DayTime(Now);
                card.PayloadJson = Json.Write(pg);
                card.State = CardState.Approved; card.Answer = "approved"; card.AnsweredAt = Now;
                if (it is not null)
                {
                    var decisions = Json.ReadList<Decision>(it.DecisionsJson);
                    foreach (var (k, v) in pg.Answers) decisions.Add(new Decision(k, v, "page", "card:" + card.Id, null, fmt.DayTime(Now), null));
                    it.DecisionsJson = Json.Write(decisions);
                    if (it.State == ItemState.Coarse) it.State = ItemState.Planning;
                    await HistoryAsync(it.Id, "page approved", $"[[card:{card.Id}]] {pg.Title}" + (pg.Answers.Count > 0 ? $" · {pg.Answers.Count} answers" : ""), ct);
                }
                var answers = pg.Answers.Count == 0 ? "" : "\nAnswers:\n" + string.Join("\n", pg.Answers.Select(a => $"- {a.Key}: {a.Value}"));
                await TellSessionAsync(card, $"The owner approved the page \"{pg.Title}\". It is the agreed design." + answers, ct);
                return new PressResult(true, $"Approved · {pg.Answers.Count} answer{(pg.Answers.Count == 1 ? "" : "s")} go with it");
            }
            case "changes":
            case "write":
            {
                var answers = pg.Answers.Count == 0 ? "" : "\nAnswers so far:\n" + string.Join("\n", pg.Answers.Select(a => $"- {a.Key}: {a.Value}"));
                var r = await WriteAsync(card, req.Text, $"Changes asked on the page \"{pg.Title}\": ", ct);
                if (r.Ok && answers.Length > 0) { var s = await SessionOf(card, ct); if (s is not null) db.Messages.Add(new OwnerMessage { SessionId = s.Id, CardId = card.Id, Text = answers.TrimStart(), At = Now }); }
                if (r.Ok && it is not null) await HistoryAsync(it.Id, "changes asked", req.Text!.Trim(), ct);
                return r.Ok ? new PressResult(true, "Sent to the session; the page comes back changed") : r;
            }
            case "dismiss":
                card.State = CardState.Dismissed; card.AnsweredAt = Now;
                await TellSessionAsync(card, $"The owner dismissed the page \"{pg.Title}\" without approving it.", ct);
                return new PressResult(true, "Dismissed");
            default: return new PressResult(false, "Unknown action " + req.Action);
        }
    }

    private async Task<PressResult> PressDeviationAsync(Card card, PressRequest req, CancellationToken ct)
    {
        var dv = Json.Read(card.PayloadJson, new DeviationPayload());
        var it = card.ItemId is null ? null : await db.Items.FindAsync([card.ItemId], ct);
        switch (req.Action)
        {
            case "allow":
                dv.Decided = "allowed"; card.PayloadJson = Json.Write(dv);
                card.State = CardState.Approved; card.Answer = "allowed"; card.AnsweredAt = Now;
                if (it is not null)
                {
                    var decisions = Json.ReadList<Decision>(it.DecisionsJson);
                    decisions.Add(new Decision(dv.What, "allowed", "deviation", "card:" + card.Id, null, fmt.DayTime(Now), null));
                    it.DecisionsJson = Json.Write(decisions);
                    await HistoryAsync(it.Id, "deviation allowed", dv.What, ct);
                }
                await TellSessionAsync(card, "Deviation allowed by the owner: " + dv.What, ct);
                return new PressResult(true, "Allowed · the session carries on");
            case "pick":
            {
                var idx = req.Index ?? -1;
                if (idx < 0 || idx >= dv.Options.Count) return new PressResult(false, "Pick one of the options.");
                dv.Decided = dv.Options[idx]; card.PayloadJson = Json.Write(dv);
                card.State = CardState.Answered; card.Answer = dv.Decided; card.AnsweredAt = Now;
                if (it is not null)
                {
                    var decisions = Json.ReadList<Decision>(it.DecisionsJson);
                    decisions.Add(new Decision(dv.What, dv.Decided, "deviation", "card:" + card.Id, null, fmt.DayTime(Now), null));
                    it.DecisionsJson = Json.Write(decisions);
                    await HistoryAsync(it.Id, "deviation decided", $"{dv.What} → {dv.Decided}", ct);
                }
                await TellSessionAsync(card, $"On the deviation \"{dv.What}\" the owner chose: {dv.Decided}", ct);
                return new PressResult(true, "Chosen · sent to the session");
            }
            case "back":
            case "write":
            {
                var r = await WriteAsync(card, req.Text, $"Sent back on the deviation \"{dv.What}\": ", ct);
                if (r.Ok) { dv.Decided = "sent back"; card.PayloadJson = Json.Write(dv); if (it is not null) await HistoryAsync(it.Id, "deviation sent back", req.Text!.Trim(), ct); }
                return r.Ok ? new PressResult(true, "Sent back to the session") : r;
            }
            default: return new PressResult(false, "Unknown action " + req.Action);
        }
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
                await HistoryAsync(it.Id, "reassigned", $"You moved it to [[machine:{req.Value}]]", ct);
                return new PressResult(true, $"{it.Id} moves to {req.Value}");
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
            case "write":
                return await WriteAsync(card, req.Text, "On the stuck step, the owner says: ", ct);
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
            case "write":
                if (string.IsNullOrWhiteSpace(req.Text)) return new PressResult(false, "Write the note first.");
                await TellSessionAsync(card, "Note from the owner: " + req.Text.Trim(), ct);
                return new PressResult(true, card.SessionId is null ? "Noted" : "Sent to the session");
            case "make-ruling":
            {
                var text = req.Text?.Trim() is { Length: > 0 } t ? t : info.Ruling;
                if (string.IsNullOrWhiteSpace(text)) return new PressResult(false, "There is nothing to record.");
                var r = await NewRulingAsync(text, card.ProjectId, card.ItemId, null, "set by you", ct);
                card.RulingId = r.Id; card.State = CardState.Dismissed; card.AnsweredAt = Now;
                return new PressResult(true, $"Recorded as {r.Id}", "#/ruling/" + r.Id);
            }
            default: return new PressResult(false, "Unknown action " + req.Action);
        }
    }

    /* ---------------------------------------------------------------- decisions */

    private async Task<Ruling> NewRulingAsync(string text, string? projectId, string? itemId, string? sessionId, string origin, CancellationToken ct)
    {
        var n = await db.Rulings.CountAsync(ct) + 1;
        while (await db.Rulings.AnyAsync(r => r.Id == Ids.Ruling(n), ct)) n++;
        var r = new Ruling { Id = Ids.Ruling(n), ProjectId = projectId, Title = Trim(text, 60), Text = text.Trim(), Date = Now, Origin = origin, ItemId = itemId, SessionId = sessionId };
        db.Rulings.Add(r);
        return r;
    }

    /// <summary>The owner typed a ruling on the Rulings screen.</summary>
    public async Task<Ruling> AddRulingAsync(RulingRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Text)) throw new WorkbenchRuleException("ruling.text", "A ruling is in your words.");
        var r = await NewRulingAsync(req.Text, req.Project, null, null, "set by you", ct);
        if (!string.IsNullOrWhiteSpace(req.Title)) r.Title = req.Title.Trim();
        Audit(Actor.Owner, "add", "ruling", r.Id);
        await SaveAsync("ruling", r.Id, ct);
        return r;
    }

    /// <summary>A session heard a decision in chat and records it straight away, in the owner's words. The owner prunes in the ledger or in chat.</summary>
    public async Task<Ruling> RecordDecisionAsync(string sessionId, string text, string? itemId, string? projectId, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        if (string.IsNullOrWhiteSpace(text)) throw new WorkbenchRuleException("ruling.text", "Record the decision in the owner's own words.");
        Rules.TextFits(text, "A decision");
        var it = itemId is not null ? await RequireItemAsync(itemId, ct) : s.ItemId is null ? null : await db.Items.FindAsync([s.ItemId], ct);
        var r = await NewRulingAsync(text, projectId ?? it?.ProjectId, it?.Id, s.Id, $"said in chat, recorded by session {Short(s.Id)}", ct);
        if (it is not null) await HistoryAsync(it.Id, "decision", $"[[ruling:{r.Id}]] {r.Title}", ct);
        await LedgerAsync(s, "decision", $"{r.Id} · {r.Title}");
        Audit(Actor.Claude, "record", "ruling", r.Id, it?.Id);
        await SaveAsync("ruling", r.Id, ct);
        return r;
    }

    /// <summary>The owner overturns or withdraws a recorded decision.</summary>
    public async Task WithdrawRulingAsync(string id, string? note, CancellationToken ct = default)
    {
        var r = await db.Rulings.FindAsync([id], ct) ?? throw new WorkbenchRuleException("ruling.unknown", $"There is no ruling {id}.");
        r.NeverReopened = false; r.ReopenNote = string.IsNullOrWhiteSpace(note) ? $"Withdrawn by you on {fmt.Day(Now)}" : note.Trim();
        Audit(Actor.Owner, "withdraw", "ruling", id);
        await SaveAsync("ruling", id, ct);
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
        var known = List<Checkout>(m.CheckoutsJson);
        m.CheckoutsJson = Json.Write(reg.Checkouts.Select(c => new Checkout(c.Path, c.Branch, c.Item ?? known.FirstOrDefault(k => k.Path.Equals(c.Path, StringComparison.OrdinalIgnoreCase))?.ItemId, c.Worktree, c.Note)).ToList());
        if (isNew) await InfoAsync($"{m.Id} registered with Host {reg.HostVersion}", $"A Host joined on {reg.Os} with {reg.Checkouts.Count} checkout{(reg.Checkouts.Count == 1 ? "" : "s")}.", "machine:" + m.Id, $"Host on {m.Id}", null, null, null, ct);
        await SaveAsync("machine", m.Id, ct);
        return m;
    }

    private static string Trim(string s, int n) => s.Length <= n ? s : s[..(n - 1)].TrimEnd() + "…";
}
