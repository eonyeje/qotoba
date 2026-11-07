using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

public sealed partial class WorkbenchOps
{
    private Card NewCard(CardType type, string headline, string? itemId, string? sessionId) => new()
    {
        Id = Ids.Card(type), Type = type, Headline = headline, ItemId = itemId, SessionId = sessionId, At = Now, State = CardState.Open
    };

    /* ---------------------------------------------------------------- questions */

    public async Task<Card> AskAsync(string sessionId, string itemId, int step, string text, List<OptionInput> options,
        string? context, string? codeRef, string? commentId, List<string>? tried, CancellationToken ct = default, string? implLine = null)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        Rules.QuestionIsAnchored(itemId, step);
        var opts = options.Select(o => new QuestionOption(o.Text.Trim(), o.Recommended, o.Why.Trim(), o.CostToChange, o.Support)).ToList();
        Rules.QuestionIsClosed(opts);
        var open = await db.Questions.CountAsync(q => q.ItemId == it.Id && q.State == QuestionState.Open, ct);
        Rules.QuestionMayBeAsked(open);
        Rules.TextFits(text, "A question");

        var n = await db.Questions.CountAsync(ct) + 1;
        var q = new Question
        {
            Id = Ids.Question(n), ItemId = it.Id, SessionId = s.Id, StepN = step, Text = text.Trim(),
            Shape = commentId is not null ? "comment" : "design", Context = context, CodeRef = codeRef, CommentId = commentId,
            OptionsJson = Json.Write(opts), TriedJson = Json.Write(tried ?? []), Asked = Now,
            Cost = opts.First(o => o.Rec).Cost, UnblockMin = await WorkBehindAsync(it, step, ct)
        };
        db.Questions.Add(q);
        var card = NewCard(CardType.Question, q.Text, it.Id, s.Id);
        card.QuestionId = q.Id;
        card.ProjectId = it.ProjectId;
        card.WeightMin = q.UnblockMin;
        card.Why = $"Blocking question on {StepLabel(it, step)} of {it.Title}";
        card.OriginStep = StepLabel(it, step);
        card.OriginFromJson = Json.Write(new List<string> { "session:" + s.Id });
        card.EvidenceJson = Json.Write(new List<object> { new { text = $"questions {open + 1}/{Caps.OpenQuestionsPerItem}", tone = open + 1 >= Caps.OpenQuestionsPerItem ? "warn" : "neutral" } });
        db.Cards.Add(card);
        q.CardId = card.Id;
        if (implLine is not null) await LineAskedAsync(implLine, card, q, ct);
        if (commentId is not null && await db.Comments.FindAsync([commentId], ct) is { } cm) { cm.QuestionId = q.Id; q.Shape = "comment"; }
        var stepRec = List<Step>(it.StepsJson).FirstOrDefault(x => x.N == step);
        if (stepRec is not null)
        {
            var steps = List<Step>(it.StepsJson);
            steps[steps.FindIndex(x => x.N == step)] = stepRec with { State = StepState.Blocked };
            it.StepsJson = Json.Write(steps);
        }
        s.State = SessionState.Waiting; s.WaitingSince ??= Now;
        await LedgerAsync(s, "ask", $"{q.Id} · {(q.Text.Length > 60 ? q.Text[..60] + "…" : q.Text)} · questions {open + 1}/{Caps.OpenQuestionsPerItem}");
        await HistoryAsync(it.Id, "question asked", $"[[question:{q.Id}]] on [[card:{card.Id}]]", ct);
        Audit(Actor.Claude, "ask", "card", card.Id, q.Id);
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
            if (c.State is CardState.Answered or CardState.Approved or CardState.Dismissed) return c.Answer ?? c.State.ToString().ToLowerInvariant();
            if (Now >= deadline) return null;
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    /* ---------------------------------------------------------------- packets, stuck, proposals, info */

    public async Task<Card> SubmitPacketAsync(string sessionId, string itemId, string outcome,
        List<CheckInput>? checks, List<ClaimInput>? claims, List<BeyondInput>? beyond, string? note, List<string>? files, int plus, int minus, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var it = await RequireItemAsync(itemId, ct);
        Rules.TextFits(outcome, "The outcome");
        if (note is not null) Rules.NoteFits(note);
        var top = await TopOfAsync(it, ct);
        var closesTop = await TopFinishesWithAsync(top, it, ct);
        var instruction = MergeInstruction(top, it, closesTop);
        var runs = await db.Runs.Where(r => r.SessionId == s.Id).ToListAsync(ct);
        var payload = new PacketPayload
        {
            Outcome = outcome.Trim(), ActualMin = s.Minutes, EstMin = it.EstMin ?? 0,
            Checks = (checks ?? []).Select(c => new PacketCheck(c.Label, c.RunId is null ? null : NormaliseRunRef(c.RunId), c.Text)).ToList(),
            Claims = (claims ?? []).Select(c => new PacketClaim(c.Text, c.RunId is null ? null : NormaliseRunRef(c.RunId))).ToList(),
            Beyond = (beyond ?? []).Select(b => new PacketBeyond(b.What, b.TypeName is null ? null : "type:" + b.TypeName, b.Why)).ToList(),
            Note = note?.Trim(), NoteDate = fmt.DayTime(Now), Instruction = instruction, Checkout = top.Checkout, Machine = s.MachineId,
            Files = files ?? [], Plus = plus, Minus = minus,
            Deliver = top.Branch is null ? "No branch: Approve closes the item without a merge" : $"Host on {s.MachineId} merges {top.Branch} into main" + (closesTop ? $" and deletes it; {top.Id} is finished" : "")
        };
        if (payload.Checks.Count == 0 && runs.Count > 0)
            payload.Checks = runs.GroupBy(r => r.Check).Select(g => g.OrderByDescending(r => r.At).First()).Select(r => new PacketCheck(r.Check, "run:" + r.Id, r.Total > 0 ? $"{r.Check} {r.Passed}/{r.Total}" : r.Check)).ToList();
        var unver = payload.Claims.Count(c => c.Proof is null);
        var unrev = await db.Assumptions.CountAsync(a => a.SessionId == s.Id && a.State == AssumptionState.Unreviewed, ct);
        var tray = await db.Founds.CountAsync(f => f.SessionId == s.Id && f.State == FoundState.InTray, ct);
        var failing = payload.Checks.Count(c => c.Ref is not null && runs.Any(r => "run:" + r.Id == c.Ref && r.Result == RunResult.Fail));
        var card = NewCard(CardType.Packet, payload.Outcome, it.Id, s.Id);
        card.ProjectId = it.ProjectId;
        card.WeightMin = await UnblockedByAsync(it, ct);
        card.Why = failing > 0 ? $"{failing} check not passing." : (payload.Checks.Count > 0 ? $"All {payload.Checks.Count} checks passed." : "No checks were run.")
            + (unrev > 0 ? $" {unrev} assumption{(unrev == 1 ? "" : "s")} to review." : "") + (tray > 0 ? $" {tray} found item{(tray == 1 ? "" : "s")} in the tray." : "") + (unver > 0 ? $" {unver} claim{(unver == 1 ? "" : "s")} without a run." : "");
        card.OriginStep = $"packet, after step {List<Step>(it.StepsJson).Count} of {List<Step>(it.StepsJson).Count}";
        card.OriginFromJson = Json.Write(new List<string> { "session:" + s.Id });
        var ev = new List<object>();
        ev.Add(new { text = $"checks {payload.Checks.Count - failing}/{payload.Checks.Count}", tone = failing > 0 ? "bad" : "good" });
        if (files is { Count: > 0 }) ev.Add(new { text = $"{files.Count} files · +{plus} −{minus}", tone = "neutral" });
        card.EvidenceJson = Json.Write(ev);
        card.PayloadJson = Json.Write(payload);
        db.Cards.Add(card);
        it.PacketCardId = card.Id;
        it.State = ItemState.Review;
        s.PacketCardId = card.Id;
        await LedgerAsync(s, "packet", $"Packet drawn · {card.Id}");
        await HistoryAsync(it.Id, "packet drawn", $"[[card:{card.Id}]] by [[session:{s.Id}]]", ct);
        Audit(Actor.Claude, "packet", "card", card.Id, it.Id);
        await SaveAsync("card", card.Id, ct);
        return card;
    }

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
            Attempts = attempts.Select(a => new StuckAttempt(a.Tried, a.Result, a.RunId is null ? null : NormaliseRunRef(a.RunId))).ToList(),
            CheckRef = attempts.LastOrDefault()?.RunId is { } r ? NormaliseRunRef(r) : null
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

    public async Task<Card> ProposeRulingsAsync(string sessionId, string chatTitle, List<string> texts, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        if (texts.Count == 0 || texts.Count > Caps.ProposalsPerSession) throw new WorkbenchRuleException("proposals.count", $"Propose 1 to {Caps.ProposalsPerSession} rulings, one line each.");
        var card = NewCard(CardType.Proposals, $"{texts.Count} ruling{(texts.Count == 1 ? "" : "s")} proposed from chat", s.ItemId, s.Id);
        card.ProjectId = s.ItemId is null ? null : (await db.Items.FindAsync([s.ItemId], ct))?.ProjectId;
        card.Why = "Chat never writes to the Workbench. Tick the lines you mean and Record; the rest are discarded.";
        card.OriginStep = "the design chat";
        card.OriginText = $"{chatTitle}, {fmt.DayTime(Now)}";
        card.PayloadJson = Json.Write(new ProposalsPayload { ChatTitle = chatTitle, When = fmt.DayTime(Now) });
        db.Cards.Add(card);
        var n = await db.Proposals.CountAsync(ct);
        foreach (var t in texts)
        {
            Rules.TextFits(t, "A proposed ruling");
            db.Proposals.Add(new Proposal { Id = Ids.Proposal(++n), Text = t.Trim(), CardId = card.Id, SessionId = s.Id, ChatTitle = chatTitle, At = Now });
        }
        await LedgerAsync(s, "propose", $"{texts.Count} rulings proposed · {card.Id}");
        await SaveAsync("card", card.Id, ct);
        return card;
    }

    public async Task<Card> InfoAsync(string headline, string body, string? chip, string origin, string? itemId, string? sessionId, string? rulingText, CancellationToken ct = default)
    {
        var card = NewCard(CardType.Info, headline, itemId, sessionId);
        card.ProjectId = itemId is null ? null : (await db.Items.FindAsync([itemId], ct))?.ProjectId;
        card.PayloadJson = Json.Write(new InfoPayload { Body = body, Chip = chip, Origin = origin, Ruling = rulingText });
        db.Cards.Add(card);
        await SaveAsync("card", card.Id, ct);
        return card;
    }

    private async Task<int> UnblockedByAsync(Item it, CancellationToken ct)
    {
        var ref_ = "item:" + it.Id;
        var blocked = await db.Edges.Where(e => e.To == ref_ && e.Type == EdgeType.Needs).ToListAsync(ct);
        var total = 0;
        foreach (var e in blocked)
        {
            if (e.Min is int m) { total += m; continue; }
            var other = await db.Items.FindAsync([e.From.Replace("item:", "")], ct);
            if (other is not null) total += await WorkBehindAsync(other, null, ct);
        }
        if (total == 0)
        {
            var next = await db.Items.Where(i => i.ParentId == it.ParentId && i.Order > it.Order && i.State != ItemState.Done).OrderBy(i => i.Order).FirstOrDefaultAsync(ct);
            if (next is not null) total = await WorkBehindAsync(next, null, ct);
        }
        return total;
    }

    private static string Words(int n) => n switch { 2 => "two", 3 => "three", 4 => "four", _ => n.ToString() };
}
