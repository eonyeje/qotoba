using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>
/// The three things that reach the owner: a custom page with its own saved answers, a stuck session, a deviation that
/// must be reported. Everything else a session does is written down for the owner to browse, never pushed.
/// </summary>
public sealed partial class WorkbenchOps
{
    private Card NewCard(CardType type, string headline, string? itemId, string? sessionId) => new()
    {
        Id = Ids.Card(type), Type = type, Headline = headline, ItemId = itemId, SessionId = sessionId, At = Now, State = CardState.Open
    };

    /* ---------------------------------------------------------------- pages */

    /// <summary>
    /// Claude wrote a page for a change that has no designer: a concept, a design across several classes, a data change,
    /// a review. The owner answers on the page itself; approving makes the answers the agreed design.
    /// </summary>
    public async Task<Card> SubmitPageAsync(string sessionId, string? itemId, string title, string html, string? purpose, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = itemId is null ? (s.ItemId is null ? null : await db.Items.FindAsync([s.ItemId], ct)) : await RequireItemAsync(itemId, ct);
        if (string.IsNullOrWhiteSpace(title)) throw new WorkbenchRuleException("page.title", "A page has a title the owner recognises.");
        if (string.IsNullOrWhiteSpace(html) || html.Length < 40) throw new WorkbenchRuleException("page.body", "A page has a body: HTML with the questions beside their answers, sections folded.");
        if (html.Length > 2_000_000) throw new WorkbenchRuleException("page.size", "A page is at most 2 MB.");
        var payload = new PagePayload { Title = title.Trim(), Html = html, Purpose = purpose?.Trim(), Machine = s.MachineId };
        var card = NewCard(CardType.Page, payload.Title, it?.Id, s.Id);
        card.ProjectId = it?.ProjectId;
        card.WeightMin = it is null ? 0 : await WorkBehindAsync(it, null, ct);
        card.Why = purpose is { Length: > 0 } ? purpose.Trim() : "A change with no designer: read it, answer on it, approve it.";
        card.OriginStep = "a page from the session";
        card.OriginFromJson = Json.Write(new List<string> { "session:" + s.Id });
        card.PayloadJson = Json.Write(payload);
        db.Cards.Add(card);
        s.State = SessionState.Waiting; s.WaitingSince ??= Now;
        await LedgerAsync(s, "page", $"{payload.Title} · {card.Id}");
        if (it is not null) await HistoryAsync(it.Id, "page", $"[[card:{card.Id}]] {payload.Title}", ct);
        Audit(Actor.Claude, "page", "card", card.Id, it?.Id);
        await SaveAsync("card", card.Id, ct);
        return card;
    }

    /// <summary>The owner answered something on a page. The page chose the key; the Workbench keeps every answer with the card.</summary>
    public async Task<PressResult> PageAnswerAsync(string cardId, PageAnswer answer, CancellationToken ct = default)
    {
        var card = await RequireCardAsync(cardId, ct);
        if (card.Type != CardType.Page) return new PressResult(false, "Only a page keeps answers.");
        if (string.IsNullOrWhiteSpace(answer.Key)) return new PressResult(false, "The page gave no key for that answer.");
        var pg = Json.Read(card.PayloadJson, new PagePayload());
        pg.Answers[answer.Key.Trim()] = answer.Value ?? "";
        pg.AnswersAt = fmt.DayTime(Now);
        card.PayloadJson = Json.Write(pg);
        Audit(Actor.Owner, "answer", "card", card.Id, answer.Key);
        await SaveAsync("card", card.Id, ct);
        return new PressResult(true, "Saved");
    }

    /// <summary>What the owner has answered on a page so far, for the session that wrote it.</summary>
    public async Task<string> PageAnswersAsync(string cardId, CancellationToken ct = default)
    {
        var card = await RequireCardAsync(cardId, ct);
        if (card.Type != CardType.Page) throw new WorkbenchRuleException("page.kind", $"{cardId} is not a page.");
        var pg = Json.Read(card.PayloadJson, new PagePayload());
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{card.Id} · {pg.Title} · {(pg.Approved ? "approved " + pg.ApprovedAt : card.State == CardState.Answered ? "changes asked: " + card.Answer : "open")}");
        if (pg.Answers.Count == 0) sb.AppendLine("No answers yet.");
        foreach (var (k, v) in pg.Answers) sb.AppendLine($"- {k}: {v}");
        return sb.ToString();
    }

    /* ---------------------------------------------------------------- deviations */

    /// <summary>Claude must leave the agreed design, or did something that must be reported (a helper type, a static, a surface change).</summary>
    public async Task<Card> ReportDeviationAsync(string sessionId, string itemId, string what, string why, string? agreed, string? instead, List<string>? options, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        Rules.TextFits(what, "What deviates");
        Rules.TextFits(why, "Why");
        var payload = new DeviationPayload { What = what.Trim(), Why = why.Trim(), Agreed = agreed?.Trim(), Instead = instead?.Trim(), Options = options ?? [], Machine = s.MachineId };
        var card = NewCard(CardType.Deviation, payload.What, it.Id, s.Id);
        card.ProjectId = it.ProjectId;
        card.WeightMin = await WorkBehindAsync(it, null, ct);
        card.Why = "The session must leave the agreed design, or has done something you asked to be told about.";
        card.OriginStep = StepLabel(it, s.StepN);
        card.OriginFromJson = Json.Write(new List<string> { "session:" + s.Id });
        card.PayloadJson = Json.Write(payload);
        db.Cards.Add(card);
        s.State = SessionState.Waiting; s.WaitingSince ??= Now;
        await LedgerAsync(s, "deviation", $"{payload.What} · {card.Id}");
        await HistoryAsync(it.Id, "deviation", $"[[card:{card.Id}]] {payload.What}", ct);
        Audit(Actor.Claude, "deviation", "card", card.Id, it.Id);
        await SaveAsync("card", card.Id, ct);
        return card;
    }

    /* ---------------------------------------------------------------- stuck */

    public async Task<Card> ReportStuckAsync(string sessionId, string itemId, int step, string checkName, List<AttemptInput> attempts, string diagnosis, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        if (attempts.Count < 2) throw new WorkbenchRuleException("stuck.attempts", "Report stuck after at least two attempts, and stop after three.");
        var steps = List<Step>(it.StepsJson);
        var others = await db.Machines.Where(m => m.Id != s.MachineId && m.Online).Select(m => m.Id).ToListAsync(ct);
        var payload = new StuckPayload
        {
            Step = step, Of = steps.Count, CheckName = checkName, Diagnosis = diagnosis.Trim(), Machine = s.MachineId, Reassign = others,
            Attempts = attempts.Select(a => new StuckAttempt(a.Tried, a.Result, a.Output)).ToList(),
            LastOutput = attempts.LastOrDefault()?.Output
        };
        var card = NewCard(CardType.Stuck, $"{checkName} failed {Words(attempts.Count)} times on {s.MachineId}", it.Id, s.Id);
        card.ProjectId = it.ProjectId;
        card.WeightMin = await WorkBehindAsync(it, step, ct);
        card.Why = $"Failed {Words(attempts.Count)} times on the same step. The session stopped instead of trying again.";
        card.OriginStep = StepLabel(it, step);
        card.OriginFromJson = Json.Write(new List<string> { "session:" + s.Id, "machine:" + s.MachineId });
        card.EvidenceJson = Json.Write(new List<object> { new { text = $"{attempts.Count} attempts", tone = "bad" } });
        card.PayloadJson = Json.Write(payload);
        db.Cards.Add(card);
        var i = steps.FindIndex(x => x.N == step);
        if (i >= 0) { steps[i] = steps[i] with { State = StepState.Blocked }; it.StepsJson = Json.Write(steps); }
        it.State = ItemState.Blocked;
        s.Stuck = true; s.State = SessionState.Waiting; s.WaitingSince ??= Now;
        await LedgerAsync(s, "stuck", $"Check failed {attempts.Count} times; stopped · {card.Id}");
        await HistoryAsync(it.Id, "stuck", $"[[card:{card.Id}]] on step {step}", ct);
        Audit(Actor.Claude, "stuck", "card", card.Id, it.Id);
        await SaveAsync("card", card.Id, ct);
        return card;
    }

    /* ---------------------------------------------------------------- info and waiting */

    public async Task<Card> InfoAsync(string headline, string body, string? chip, string origin, string? itemId, string? sessionId, string? rulingText, CancellationToken ct = default)
    {
        var card = NewCard(CardType.Info, headline, itemId, sessionId);
        card.ProjectId = itemId is null ? null : (await db.Items.FindAsync([itemId], ct))?.ProjectId;
        card.PayloadJson = Json.Write(new InfoPayload { Body = body, Chip = chip, Origin = origin, Ruling = rulingText });
        db.Cards.Add(card);
        await SaveAsync("card", card.Id, ct);
        return card;
    }

    /// <summary>MCP: block until the owner answers, or the timeout passes. Polls the store; cheap at one session.</summary>
    public async Task<string?> WaitAnswerAsync(string cardId, TimeSpan timeout, CancellationToken ct = default)
    {
        var deadline = Now + timeout;
        while (true)
        {
            db.ChangeTracker.Clear();
            var c = await db.Cards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cardId, ct);
            if (c is null) throw new WorkbenchRuleException("card.unknown", $"There is no card {cardId}.");
            if (c.State is CardState.Answered or CardState.Approved or CardState.Dismissed)
            {
                if (c.Type == CardType.Page) return (c.State == CardState.Approved ? "approved" : c.Answer ?? "answered") + "\n" + await PageAnswersAsync(cardId, ct);
                return c.Answer ?? c.State.ToString().ToLowerInvariant();
            }
            if (Now >= deadline) return null;
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    private static string Words(int n) => n switch { 2 => "two", 3 => "three", 4 => "four", _ => n.ToString() };
}
