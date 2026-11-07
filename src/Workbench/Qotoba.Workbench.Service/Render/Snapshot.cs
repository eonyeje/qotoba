using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Store;
using Qotoba.Workbench.Store.Ops;

namespace Qotoba.Workbench.Service.Render;

/// <summary>
/// Projects the store into the exact object the prototype's app.js renders (window.WB).
/// Nothing appears here that the prototype does not already show; a field the store knows but the prototype
/// does not draw stays out until the owner asks for it.
/// </summary>
public sealed class Snapshot(WorkbenchDb db, IClock clock, Fmt fmt, OpsOptions options)
{
    private DateTimeOffset Now => clock.Now;
    private static readonly Dictionary<string, object?> Empty = [];

    public async Task<Dictionary<string, object?>> BuildAsync(CancellationToken ct = default)
    {
        var projects = await db.Projects.OrderBy(p => p.Order).ToListAsync(ct);
        var lanes = await db.Lanes.ToListAsync(ct);
        var items = await db.Items.OrderBy(i => i.Order).ToListAsync(ct);
        var cards = await db.Cards.ToListAsync(ct);
        var questions = await db.Questions.ToListAsync(ct);
        var assumptions = await db.Assumptions.ToListAsync(ct);
        var founds = await db.Founds.ToListAsync(ct);
        var rulings = await db.Rulings.OrderBy(r => r.Date).ToListAsync(ct);
        var proposals = await db.Proposals.ToListAsync(ct);
        var sessions = await db.Sessions.ToListAsync(ct);
        var runs = await db.Runs.ToListAsync(ct);
        var machines = await db.Machines.ToListAsync(ct);
        var tokens = await db.Tokens.ToListAsync(ct);
        var files = await db.Files.ToListAsync(ct);
        var types = await db.Types.ToListAsync(ct);
        var nyw = await db.NotYetWritten.ToListAsync(ct);
        var comments = await db.Comments.ToListAsync(ct);
        var commits = await db.Commits.ToListAsync(ct);
        var branches = await db.Branches.ToListAsync(ct);
        var claudemd = await db.ClaudeMd.ToListAsync(ct);
        var edges = await db.Edges.ToListAsync(ct);
        var reports = await db.Reports.ToListAsync(ct);
        var implLines = await db.ImplLines.OrderBy(l => l.Order).ToListAsync(ct);
        var burns = await db.Settings.Where(s => s.Key.StartsWith("burn:")).ToDictionaryAsync(s => s.Key[5..], s => Json.ReadList<BurnSample>(s.Value), ct);
        var decisionsMeasured = await db.Cards.CountAsync(c => c.AnsweredAt != null && c.Type != CardType.Info, ct);
        var firstDecision = await db.Cards.Where(c => c.AnsweredAt != null).OrderBy(c => c.AnsweredAt).Select(c => c.AnsweredAt).FirstOrDefaultAsync(ct);

        var byItem = items.ToDictionary(i => i.Id);
        var children = items.GroupBy(i => i.ParentId ?? "").ToDictionary(g => g.Key, g => g.OrderBy(i => i.Order).Select(i => i.Id).ToList());
        var runsByItem = runs.GroupBy(r => r.ItemId ?? "").ToDictionary(g => g.Key, g => g.OrderBy(r => r.At).ToList());
        var cardsById = cards.ToDictionary(c => c.Id);
        var sessionsByItem = sessions.GroupBy(s => s.ItemId ?? "").ToDictionary(g => g.Key, g => g.ToList());

        var wb = new Dictionary<string, object?>
        {
            ["meta"] = new
            {
                programme = options.Programme, owner = options.Owner, now = fmt.Stamp(Now), today = fmt.Day(Now),
                avgDecisionSec = AvgDecisionSec(cards), decisionsMeasured, decisionsSince = firstDecision is null ? fmt.Day(Now) : fmt.Day(firstDecision.Value),
                workLeft = new { coarseWeeks = projects.Where(p => p.State == ProjectState.Coarse).Sum(p => p.CoarseWeeks), measuredWeeks = MeasuredWeeks(items) }
            },
            ["budgets"] = new object[]
            {
                new { key = "questions", label = "Open questions per item", value = Caps.OpenQuestionsPerItem, def = 2, note = "A third question is refused until one is answered." },
                new { key = "found", label = "Found items per session", value = Caps.FoundPerSession, def = 5, note = "Found items go to the item's tray, never into the tree." },
                new { key = "planSteps", label = "Steps per plan", value = Caps.PlanSteps, def = 7, note = "A longer plan is refused; split the item instead." },
                new { key = "noteLines", label = "Lines in a Claude note", value = Caps.NoteLines, def = 2, note = "Anything longer belongs in a question or the packet." }
            },
            ["projects"] = projects.Select(p => ProjectView(p, items, lanes, cards, edges, burns)).ToList(),
            ["lanes"] = lanes.GroupBy(l => l.Key).ToDictionary(g => g.Key, g => (object?)LaneView(g.First(), items, sessions)),
            ["items"] = items.ToDictionary(i => i.Id, i => (object?)ItemView(i, children, runsByItem, sessionsByItem, comments, edges, questions, cardsById)),
            ["questions"] = questions.ToDictionary(q => q.Id, q => (object?)QuestionView(q, byItem)),
            ["assumptions"] = assumptions.ToDictionary(a => a.Id, a => (object?)new { session = a.SessionId, item = a.ItemId, state = AssumptionState(a.State), text = a.Text }),
            ["found"] = founds.ToDictionary(f => f.Id, f => (object?)new
            {
                kind = f.Kind.ToString(), title = f.Title, session = f.SessionId, item = f.ItemId, state = FoundState(f.State), found = fmt.DayTime(f.At),
                why = f.Why, evidence = f.EvidenceRef, suggest = f.SuggestItemId, promoted = f.PromotedTo
            }),
            ["rulings"] = rulings.ToDictionary(r => r.Id, r => (object?)new
            {
                project = r.ProjectId ?? "wb", title = r.Title, date = fmt.Day(r.Date), neverReopened = r.NeverReopened, text = r.Text, origin = r.Origin,
                question = r.QuestionId is null ? null : "question:" + r.QuestionId, item = r.ItemId is null ? null : "item:" + r.ItemId,
                related = Json.ReadList<string>(r.RelatedJson), reopenNote = r.ReopenNote
            }),
            ["proposed"] = ProposedView(cards, proposals),
            ["files"] = files.ToDictionary(f => f.Id, f => (object?)new
            {
                path = f.Path.Replace('\\', '/'), repo = f.Repo, proj = string.IsNullOrEmpty(f.Folder) ? f.Repo : f.Folder, project = f.ProjectId, folder = f.Folder,
                branch = branches.FirstOrDefault(b => b.Name == f.Branch && (b.Checkout == null || b.Checkout == f.Checkout))?.Id, branchName = f.Branch,
                seen = f.Seen, newLines = f.NewLines, item = f.ItemId, machine = f.MachineId,
                lastSeen = f.LastSeenAt is null ? null : fmt.DayTime(f.LastSeenAt.Value), added = f.AddedAt is null ? null : fmt.DayTime(f.AddedAt.Value),
                lines = Json.ReadList<string>(f.LinesJson), diff = Json.ReadList<string>(f.DiffJson)
            }),
            ["types"] = types.ToDictionary(t => t.Id, t => (object?)new { kind = t.Kind, ns = t.Ns, at = t.At, seen = t.Seen, newIn = t.NewIn, item = t.ItemId, summary = t.Summary, beyondPlan = t.BeyondPlan }),
            ["nyw"] = NotYetWrittenView(nyw, items),
            ["comments"] = comments.ToDictionary(c => c.Id, c => (object?)new
            {
                file = c.FileId, line = c.Line, symbol = c.Symbol, symbolKind = c.SymbolKind, text = c.Text, state = c.State.ToString().ToLowerInvariant(), item = c.ItemId,
                handled = c.HandledJson is null ? null : Json.Read<object>(c.HandledJson, new { }), staleWhy = c.StaleWhy, question = c.QuestionId, versions = Json.Read<object>(c.VersionsJson, new List<object>())
            }),
            ["commits"] = commits.ToDictionary(c => c.Id, c => (object?)new
            {
                msg = c.Msg, date = fmt.DayTime(c.Date), session = c.SessionId, branch = c.BranchId, files = Json.ReadList<string>(c.FilesJson), plus = c.Plus, minus = c.Minus,
                diff = Json.ReadList<string>(c.DiffJson), proves = c.ProvesCommentId, stales = c.StalesCommentId
            }),
            ["branches"] = branches.ToDictionary(b => b.Id, b => (object?)new
            {
                name = b.Name, item = b.ItemId, machine = b.MachineId, state = b.State, ahead = b.Ahead, behind = b.Behind, pushed = b.Pushed, merged = b.MergedCommit is null ? null : "commit:" + b.MergedCommit,
                worktree = b.Worktree, commits = Json.ReadList<string>(b.CommitsJson), dirty = b.Dirty
            }),
            ["claudemd"] = claudemd.ToDictionary(c => c.Id, c => (object?)new { title = c.Title, file = c.FileRef, n = c.N }),
            ["runs"] = runs.ToDictionary(r => r.Id, r => (object?)RunView(r)),
            ["sessions"] = sessions.ToDictionary(s => s.Id, s => (object?)SessionView(s, byItem, cards, runs, questions, assumptions, founds)),
            ["machines"] = machines.ToDictionary(m => m.Id, m => (object?)MachineView(m, machines, items, sessions)),
            ["tokens"] = tokens.Select(t => new { client = t.Client, kind = t.Kind.ToString(), issued = fmt.DayTime(t.Issued), last = t.LastUsed is null ? "never" : fmt.Time(t.LastUsed.Value) }).ToList(),
            ["install"] = new[]
            {
                "Run the Workbench service: dotnet run --project src/Workbench/Qotoba.Workbench.Service",
                "Run the Host on this machine: dotnet run --project src/Workbench/Qotoba.Workbench.Host -- serve",
                "Open the URL the service prints; it carries the token once.",
                "Start claude in the Qotoba repository: the hooks and the MCP server are configured in .claude/settings.json and .mcp.json.",
                "Check: the session appears on Now within a second of starting."
            },
            ["cards"] = cards.ToDictionary(c => c.Id, c => (object?)CardView(c, byItem)),
            ["packets"] = cards.Where(c => c.Type == CardType.Packet).ToDictionary(c => c.Id, c => (object?)PacketView(c, assumptions, founds, comments)),
            ["plans"] = cards.Where(c => c.Type == CardType.Plan).ToDictionary(c => c.Id, c => (object?)PlanView(c)),
            ["stucks"] = cards.Where(c => c.Type == CardType.Stuck).ToDictionary(c => c.Id, c => (object?)StuckView(c)),
            ["settings"] = new
            {
                notifications = new object[]
                {
                    new { t = "One OS notification per blocking card", on = true, note = "Question, Approve, Plan and Stuck cards. Info cards never notify." },
                    new { t = "Quiet hours", on = true, note = "22:30 to 06:30; cards wait in the inbox." },
                    new { t = "Daily summary at 07:00", on = false, note = "One line: queue size and time to clear it." }
                },
                addresses = new object[]
                {
                    new { t = "Database", v = StorePaths.DbPath(null) },
                    new { t = "Service", v = "http://localhost:5290/api" },
                    new { t = "Hook endpoint", v = "http://localhost:5290/hooks" },
                    new { t = "MCP endpoint", v = "http://localhost:5290/mcp" }
                }
            },
            ["chat"] = new
            {
                explain = "Talk opens a design chat in claude.ai with this card's brief already typed. Chat never writes to the Workbench: the only way back is a list of proposed rulings, one line each, that you tick on a Record card in the inbox.",
                last = LastChat(cards)
            },
            ["register"] = Empty,
            ["reportOrder"] = ReportOrder,
            ["reportFolded"] = ReportFolded,
            ["subtypes"] = Subtypes,
            ["states"] = States,
            ["fpbs"] = reports.ToDictionary(r => r.Id, r => (object?)ReportView(r, implLines, items, byItem)),
            ["edges"] = edges.Select(e => new { id = e.Id, from = e.From, to = e.To, type = e.Type.ToString().ToLowerInvariant(), hard = e.Hard, reason = e.Reason, by = e.By == Actor.Owner ? "you" : "claude", session = e.SessionId, confirmed = e.Confirmed, min = e.Min }).ToList(),
            ["cardWhy"] = cards.ToDictionary(c => c.Id, c => (object?)c.Why),
            ["cardOrigin"] = cards.ToDictionary(c => c.Id, c => (object?)new { step = c.OriginStep, from = Json.ReadList<string>(c.OriginFromJson), fromText = c.OriginText })
        };
        return wb;
    }

    /* ---------------------------------------------------------------- views */

    private object ProjectView(Project p, List<Item> items, List<Lane> lanes, List<Card> cards, List<Edge> edges, Dictionary<string, List<BurnSample>> burns)
    {
        var mine = items.Where(i => i.ProjectId == p.Id).ToList();
        var top = mine.Where(i => i.ParentId is null).OrderBy(i => i.Order).Select(i => i.Id).ToList();
        var next = cards.Where(c => c.ProjectId == p.Id && c.State == CardState.Open && c.Type != CardType.Info).OrderByDescending(c => c.WeightMin).FirstOrDefault();
        var measured = MeasuredWeeks(mine);
        var open = p.State == ProjectState.Open;
        var burn = burns.GetValueOrDefault(p.Id) ?? [];
        return new
        {
            id = p.Id, code = p.Code, name = p.Name, color = p.Color, state = open ? "open" : "coarse", coarseWeeks = p.CoarseWeeks, measuredWeeks = measured,
            factor = open ? (p.Factor ?? LaneAverage(lanes.Where(l => l.ProjectId == p.Id))) : null, purpose = p.Purpose,
            children = open ? top : new List<string>(), lanes = lanes.Where(l => l.ProjectId == p.Id).Select(l => l.Key).OrderBy(k => k).ToList(),
            burn = burn.Count >= 2 ? burn.Select(b => b.H).ToArray() : [], burnDays = burn.Count >= 2 ? burn.Select(b => b.Day).ToArray() : [],
            criticalPath = WorkbenchOps.CriticalPath(items, edges, p.Id), nextDecision = next is null ? null : "card:" + next.Id, repo = p.Repo, closedWhy = p.ClosedWhy,
            preview = open ? null : top
        };
    }

    private static double? LaneAverage(IEnumerable<Lane> lanes) { var l = lanes.ToList(); return l.Count == 0 ? null : Math.Round(l.Average(x => x.Factor), 1); }

    /// <summary>One point of the burn-down: measured hours of work left on a day.</summary>
    public sealed record BurnSample(string Day, int H);

    private object LaneView(Lane l, List<Item> items, List<Session> sessions)
    {
        var path = items.Where(i => i.LaneKey == l.Key && i.IsOpen).OrderBy(i => i.Id.Length).Select(i => "item:" + i.Id).ToList();
        var state = l.State;
        var live = sessions.Where(s => s.LaneKey == l.Key || (s.ItemId is not null && path.Contains("item:" + s.ItemId))).ToList();
        if (live.Any(s => s.Stuck)) state = "stuck";
        else if (live.Any(s => s.State == SessionState.Waiting)) state = "waiting on you";
        else if (live.Any(s => s.State == SessionState.Working)) state = "working";
        else if (l.Proposed) state = "proposed by plan";
        return new { id = l.Key, name = l.Name, factor = l.Factor, was = l.Was, path, machine = l.MachineId is null ? null : "machine:" + l.MachineId, state, proposed = l.Proposed };
    }

    private object ItemView(Item i, Dictionary<string, List<string>> children, Dictionary<string, List<Run>> runsByItem, Dictionary<string, List<Session>> sessionsByItem,
        List<Comment> comments, List<Edge> edges, List<Question> questions, Dictionary<string, Card> cards)
    {
        var steps = Json.ReadList<Step>(i.StepsJson);
        var kids = children.GetValueOrDefault(i.Id, []);
        var myRuns = runsByItem.GetValueOrDefault(i.Id, []);
        var mySessions = sessionsByItem.GetValueOrDefault(i.Id, []);
        var est = i.EstMin ?? steps.Sum(s => s.EstMin);
        var factor = i.Factor ?? 1.2;
        var forecast = est == 0 ? (int?)null : (int)Math.Round(est * factor);
        var itemRef = "item:" + i.Id;
        var needs = edges.Where(e => e.From == itemRef && e.Type == EdgeType.Needs).Select(e => new { @ref = e.To, min = e.Min ?? 0, note = e.Confirmed ? null : "proposed by Claude" }).ToList();
        var blocks = edges.Where(e => e.To == itemRef && e.Type == EdgeType.Needs).Select(e => new { @ref = e.From, min = e.Min ?? 0, note = (string?)null }).ToList();
        var rulingRefs = questions.Where(q => q.ItemId == i.Id && q.RulingId is not null).Select(q => "ruling:" + q.RulingId).Distinct().ToList();
        var decisions = Json.ReadList<Decision>(i.DecisionsJson).Select(d => new { t = d.T, a = d.A, how = d.How, @ref = d.Ref, from = d.From, date = d.Date, ruling = d.Ruling }).ToList();
        return new
        {
            kind = "task", title = i.Title, project = i.ProjectId, parent = i.ParentId, children = kids,
            state = StateWord(i), isOpen = i.IsOpen, lane = i.LaneKey, machine = i.HomeMachineId, branch = i.Branch, coarse = i.CoarseSize, goal = i.Goal,
            steps = steps.Select(s => new { t = s.Text, est = s.EstMin, state = s.State.ToString().ToLowerInvariant(), proof = s.ProofRef ?? (s.State == StepState.Done ? null : "nyw:" + NywKey(i.Id, s.N)), needs = s.Needs, note = s.Note }).ToList(),
            checks = myRuns.Select(r => "run:" + r.Id).ToList(),
            changes = new { files = new List<object>(), types = new List<string>() },
            time = new { so = i.SoMin, est, factor, forecast },
            needs, blocks,
            commentScope = comments.Where(c => c.ItemId == i.Id).Select(c => c.Id).ToList(),
            note = i.Note is null ? null : new { text = i.Note, date = i.NoteAt is null ? "" : fmt.DayTime(i.NoteAt.Value), session = i.NoteSessionId is null ? null : "session:" + i.NoteSessionId },
            claims = Json.ReadList<Claim>(i.ClaimsJson).Select(c => new { t = c.Text, proof = c.ProofRef }).ToList(),
            rulings = rulingRefs,
            sessions = mySessions.OrderBy(s => s.Started).Select(s => new object[] { "session:" + s.Id, s.Minutes }).ToList(),
            packet = i.PacketCardId is null ? null : "card:" + i.PacketCardId,
            plan = i.PlanCardId is not null && cards.TryGetValue(i.PlanCardId, out var pc) && pc.State == CardState.Open ? "card:" + i.PlanCardId : null,
            implements = Json.ReadList<string>(i.ImplementsJson),
            decisions,
            history = Json.ReadList<HistoryLine>(i.HistoryJson).Select(h => new[] { h.Date, h.Event, h.Text }).ToList(),
            info = new { tpl = kids.Count > 0 ? "FTR" : "task", goal = i.Goal },
            preview = i.Preview
        };
    }

    private static string StateWord(Item i) => i.State switch
    {
        Model.ItemState.Coarse => "coarse", Model.ItemState.Planned => "planned", Model.ItemState.Planning => "planning", Model.ItemState.Building => "building",
        Model.ItemState.Review => "review", Model.ItemState.Blocked => "blocked", Model.ItemState.Paused => "paused", _ => "done"
    };

    private object QuestionView(Question q, Dictionary<string, Item> items)
    {
        var it = items.GetValueOrDefault(q.ItemId);
        var stepsCount = it is null ? 0 : Json.ReadList<Step>(it.StepsJson).Count;
        var opts = Json.ReadList<QuestionOption>(q.OptionsJson);
        return new
        {
            item = q.ItemId, session = q.SessionId, state = q.State switch { QuestionState.Open => "open", QuestionState.Answered => "answered", _ => "not asked" },
            card = q.CardId, shape = q.Shape, @short = q.Short ?? q.Text, text = q.Text,
            blocks = q.StepN is null ? "" : $"step {q.StepN} of {Math.Max(stepsCount, q.StepN.Value)}", unblockMin = q.UnblockMin,
            asked = fmt.DayTime(q.Asked), waiting = q.State == QuestionState.Open ? Fmt.Minutes(Fmt.MinutesBetween(q.Asked, Now)) : "",
            context = q.Context, code = q.CodeRef, unseenFile = q.UnseenFile ? q.CodeRef?.Split(':').Take(2).Aggregate((a, b) => a + ":" + b) : null, comment = q.CommentId,
            options = opts.Select(o => new { t = o.T, rec = o.Rec ? true : (bool?)null, why = o.Why, support = o.Support ?? [], cost = o.Cost }).ToList(),
            cost = q.Cost, tried = Json.ReadList<string>(q.TriedJson),
            answered = q.AnsweredAt is null ? null : fmt.DayTime(q.AnsweredAt.Value), answer = q.Answer, ruling = q.RulingId, report = q.ReportId
        };
    }

    private object RunView(Run r) => new
    {
        check = r.Check, item = r.ItemId, session = r.SessionId, machine = r.MachineId, also = r.Also, result = r.Result.ToString().ToLowerInvariant(),
        passed = r.Total > 0 ? r.Passed : (int?)null, total = r.Total > 0 ? r.Total : (int?)null, dur = r.DurationSec, at = fmt.DayTime(r.At), cmd = r.Cmd,
        @out = r.OutputTail.Replace("\r\n", "\n").Split('\n'), attempt = r.Attempt, exitCode = r.ExitCode, expected = r.Expected, value = r.Value, seed = r.Seed
    };

    private object SessionView(Session s, Dictionary<string, Item> items, List<Card> cards, List<Run> runs, List<Question> questions, List<Assumption> assumptions, List<Found> founds)
    {
        var it = s.ItemId is null ? null : items.GetValueOrDefault(s.ItemId);
        var steps = it is null ? [] : Json.ReadList<Step>(it.StepsJson);
        var cur = steps.FirstOrDefault(x => x.State == StepState.Current) ?? (s.StepN is int n ? steps.FirstOrDefault(x => x.N == n) : null);
        var stepText = cur is null ? (it?.PlanCardId is not null && it.State == Model.ItemState.Planning ? "plan submitted · waiting for approval" : "") : $"{cur.N} of {steps.Count} · {cur.Text}";
        var waitingOn = cards.Where(c => c.SessionId == s.Id && c.State == CardState.Open && c.Type != CardType.Info).OrderByDescending(c => c.WeightMin).Select(c => "card:" + c.Id).ToList();
        var minutes = s.Minutes + (s.TurnStarted is { } ts && s.State != SessionState.Ended ? Fmt.MinutesBetween(ts, Now) : 0);
        return new
        {
            title = string.IsNullOrEmpty(s.Title) ? (it?.Title ?? "Session " + s.Id[..Math.Min(4, s.Id.Length)]) : s.Title,
            machine = s.MachineId, item = s.ItemId, lane = s.LaneKey ?? it?.LaneKey, state = s.State.ToString().ToLowerInvariant(), stuck = s.Stuck,
            started = fmt.DayTime(s.Started), ended = s.Ended is null ? null : fmt.DayTime(s.Ended.Value), minutes, turns = s.Turns, tools = s.Tools,
            step = stepText, stepMin = s.StepMin, lastAction = s.LastAction, waitingSince = s.WaitingSince is null ? null : fmt.Time(s.WaitingSince.Value), waitingOn,
            checks = runs.Where(r => r.SessionId == s.Id).Select(r => "run:" + r.Id).ToList(),
            questions = questions.Where(q => q.SessionId == s.Id).Select(q => "question:" + q.Id).ToList(),
            assumptions = assumptions.Where(a => a.SessionId == s.Id).Select(a => a.Id).ToList(),
            found = founds.Where(f => f.SessionId == s.Id).Select(f => "found:" + f.Id).ToList(),
            packet = s.PacketCardId is null ? null : "card:" + s.PacketCardId,
            ledger = Json.ReadList<LedgerLine>(s.LedgerJson).Select(l => new[] { l.At, l.Kind, l.Text }).ToList()
        };
    }

    private object MachineView(Machine m, List<Machine> all, List<Item> items, List<Session> sessions)
    {
        var reference = all.OrderBy(x => x.Registered).First();
        var clock = m.Id == reference.Id ? "reference clock" : $"{Math.Abs(m.ClockSkewSec):0.0} s {(m.ClockSkewSec < 0 ? "behind" : "ahead of")} {reference.Id}";
        return new
        {
            os = m.Os, host = m.HostVersion, online = m.Online && m.LastSeen is { } ls && (Now - ls) < TimeSpan.FromMinutes(3), lastSeen = m.LastSeen is null ? "never" : fmt.Time(m.LastSeen.Value),
            clock, clockWarn = Math.Abs(m.ClockSkewSec) >= 1.0, token = fmt.DayTime(m.Registered), kind = m.Kind.ToString().ToLowerInvariant(), ephemeral = m.Ephemeral,
            checkouts = Json.ReadList<Checkout>(m.CheckoutsJson).Select(c => new { path = c.Path, branch = c.Branch, item = c.ItemId, worktree = c.Worktree, note = c.Note }).ToList(),
            homed = items.Where(i => i.HomeMachineId == m.Id).Select(i => i.Id).ToList(),
            sessions = sessions.Where(s => s.MachineId == m.Id).Select(s => s.Id).ToList()
        };
    }

    private object CardView(Card c, Dictionary<string, Item> items)
    {
        var info = c.Type == CardType.Info ? Json.Read(c.PayloadJson, new InfoPayload()) : null;
        var waitingMin = Fmt.MinutesBetween(c.At, c.AnsweredAt ?? Now);
        return new
        {
            type = c.Type.ToString().ToLowerInvariant(), question = c.QuestionId, item = c.ItemId, project = c.ProjectId, session = c.SessionId, weight = c.WeightMin,
            waiting = Fmt.Minutes(waitingMin), at = SameDay(c.At) ? fmt.Time(c.At) : fmt.DayTime(c.At), headline = c.Headline, why = c.Why,
            evidence = Json.Read<List<object>>(c.EvidenceJson, []),
            approved = c.State == CardState.Approved ? fmt.DayTime(c.AnsweredAt ?? Now) : null,
            answered = c.State == CardState.Answered ? fmt.DayTime(c.AnsweredAt ?? Now) : null,
            answer = c.Answer, preview = c.State == CardState.Preview ? true : (bool?)null,
            deferred = c.State == CardState.Deferred ? true : (bool?)null, dismissed = c.State == CardState.Dismissed ? true : (bool?)null,
            age = Fmt.Minutes(waitingMin), chip = info?.Chip, body = info?.Body, origin = info?.Origin, ruling = info?.Ruling
        };
    }

    private object PacketView(Card c, List<Assumption> assumptions, List<Found> founds, List<Comment> comments)
    {
        var pk = Json.Read(c.PayloadJson, new PacketPayload());
        return new
        {
            item = c.ItemId, session = c.SessionId, machine = pk.Machine, state = pk.Approved ? "approved" : "waiting", outcome = pk.Outcome,
            time = new { actual = pk.ActualMin, est = pk.EstMin, factor = pk.EstMin > 0 ? Math.Round(pk.ActualMin / (double)pk.EstMin, 2) : (double?)null },
            checks = pk.Checks.Select(x => new { label = x.Label, @ref = x.Ref, text = x.Text }).ToList(),
            assumptions = assumptions.Where(a => a.SessionId == c.SessionId).Select(a => a.Id).ToList(),
            found = founds.Where(f => f.SessionId == c.SessionId).Select(f => f.Id).ToList(),
            commentsHandled = comments.Where(x => x.ItemId == c.ItemId && x.State == CommentState.Handled).Select(x => x.Id).ToList(),
            commentsOpen = comments.Where(x => x.ItemId == c.ItemId && x.State == CommentState.Open).Select(x => x.Id).ToList(),
            beyond = pk.Beyond.Select(b => new { t = b.T, type = b.Type, why = b.Why }).ToList(),
            claims = pk.Claims.Select(x => new { t = x.T, proof = x.Proof }).ToList(),
            note = pk.Note is null ? null : new { text = pk.Note, date = pk.NoteDate },
            deliver = pk.Deliver, toast = $"Instruction sent to the Host on {pk.Machine}", instruction = pk.Instruction,
            approved = pk.Approved ? (c.AnsweredAt is null ? "approved" : fmt.DayTime(c.AnsweredAt.Value)) : null,
            merged = pk.MergedCommit is null ? null : "commit:" + pk.MergedCommit, mergedCommit = pk.MergedCommit, gitOutput = pk.GitOutput,
            files = pk.Files, plus = pk.Plus, minus = pk.Minus
        };
    }

    private static object PlanView(Card c)
    {
        var pl = Json.Read(c.PayloadJson, new PlanPayload());
        return new
        {
            item = c.ItemId, session = c.SessionId, machine = pl.Machine, lane = pl.Lane, factor = pl.Factor, factorNote = pl.FactorNote, goal = pl.Goal,
            steps = pl.Steps.Select(s => new { t = s.T, est = s.Est, needs = s.Needs ?? [], proof = s.Proof }).ToList(),
            settle = pl.Settle.Select(s => new { id = s.Id, q = s.Q, options = s.Options, rec = s.Rec, from = s.From, chosen = s.Chosen }).ToList(),
            opensBranch = pl.OpensBranch, approved = pl.Approved ? pl.ApprovedAt ?? "approved" : null, settled = pl.Settled
        };
    }

    private static object StuckView(Card c)
    {
        var st = Json.Read(c.PayloadJson, new StuckPayload());
        return new
        {
            item = c.ItemId, session = c.SessionId, machine = st.Machine, step = $"step {st.Step} of {st.Of}", check = st.CheckRef, checkName = st.CheckName,
            attempts = st.Attempts.Select(a => new { t = a.T, result = a.Result, run = a.Run }).ToList(), diagnosis = st.Diagnosis, reassign = st.Reassign
        };
    }

    private object ProposedView(List<Card> cards, List<Proposal> proposals)
    {
        var card = cards.Where(c => c.Type == CardType.Proposals).OrderByDescending(c => c.At).FirstOrDefault();
        if (card is null) return new { chat = "", when = "", turns = 0, items = new List<object>(), card = (string?)null, item = (string?)null };
        var pp = Json.Read(card.PayloadJson, new ProposalsPayload());
        return new
        {
            chat = pp.ChatTitle, when = pp.When, turns = pp.Turns,
            items = proposals.Where(p => p.CardId == card.Id).OrderBy(p => p.Id).Select(p => new { id = p.Id, text = p.Text, ticked = p.Ticked, ruling = p.RulingId }).ToList(),
            card = card.Id, item = card.ItemId
        };
    }

    private object ReportView(Report r, List<ImplLine> lines, List<Item> items, Dictionary<string, Item> byItem)
    {
        var takers = items.Where(i => Json.ReadList<string>(i.ImplementsJson).Contains("fpb:" + r.Id)).Select(i => i.Id).ToList();
        var sections = Json.Read<Dictionary<string, object>>(r.SectionsJson, []);
        var impl = lines.Where(l => l.ReportId == r.Id).Select(l => new
        {
            id = l.Id, kind = l.Kind.ToString().ToLowerInvariant(), text = l.Text, rec = l.Rec, why = l.Why, cost = l.Cost, status = l.Status.ToString().ToLowerInvariant(),
            @ref = l.Ref, via = l.Via, note = l.Note, date = l.Date is null ? null : fmt.DayTime(l.Date.Value), q = l.QuestionId, item = l.ItemId,
            options = Json.ReadList<QuestionOption>(l.OptionsJson).Select(o => new { t = o.T, why = o.Why, cost = o.Cost, rec = o.Rec }).ToList()
        }).ToList();
        if (impl.Count > 0) sections["implementing"] = new { author = "claude", lines = impl };
        if (r.ChosenFix is int cf && sections.TryGetValue("fixes", out var fx) && fx is System.Text.Json.JsonElement fe && fe.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            var d = Json.Read<Dictionary<string, object>>(fe.GetRawText(), []);
            d["chosen"] = cf; sections["fixes"] = d;
        }
        return new
        {
            kind = r.Kind.ToString(), subtype = r.Subtype, title = r.Title, project = r.ProjectId, attach = r.AttachItemId is null ? "project:" + r.ProjectId : "item:" + r.AttachItemId,
            state = r.State, machine = (string?)null, branch = (string?)null, lane = (string?)null,
            severity = r.Severity is null ? null : new { level = r.Severity.ToString()!.ToLowerInvariant(), reason = r.SeverityReason, by = r.SeverityBy == Author.Me ? "you" : "claude", confirmed = r.SeverityBy == Author.Me },
            time = new { est = r.EstMin, estBy = r.EstBy, actual = 0, factor = (double?)null, forecast = (int?)null },
            source = new { type = r.SourceType, session = r.SourceSessionId, found = r.SourceFoundId, date = fmt.DayTime(r.Created) },
            sections, steps = new List<object>(), checks = new List<string>(), changes = (object?)null, sessions = new List<string>(), rulings = new List<string>(),
            history = Json.Read<List<object>>(r.HistoryJson, []), takenOnBy = takers
        };
    }

    private static string NywKey(string itemId, int n) => itemId.Replace('.', '-') + "-s" + n;

    /// <summary>Planned proofs that no run has produced yet, drawn as dashed chips, plus any the store holds.</summary>
    private static Dictionary<string, object?> NotYetWrittenView(List<NotYetWritten> stored, List<Item> items)
    {
        var d = stored.ToDictionary(n => n.Id, n => (object?)new { title = n.Title, where = n.Where, step = n.Step });
        foreach (var it in items)
            foreach (var s in Json.ReadList<Step>(it.StepsJson))
                if (s.ProofRef is null && s.State != StepState.Done)
                    d[NywKey(it.Id, s.N)] = new { title = string.IsNullOrWhiteSpace(s.ProofText) ? "proof of step " + s.N : s.ProofText, where = it.Title, step = $"Step {s.N} of {it.Id}: {s.Text}" };
        return d;
    }

    /* ---------------------------------------------------------------- helpers and static tables */

    private bool SameDay(DateTimeOffset t) => fmt.Local(t).Date == fmt.NowLocal.Date;
    private static string AssumptionState(AssumptionState s) => s switch { Model.AssumptionState.Fine => "fine", Model.AssumptionState.Flipped => "flipped", _ => "unreviewed" };
    private static string FoundState(FoundState s) => s switch { Model.FoundState.InTray => "in tray", Model.FoundState.Promoted => "promoted", Model.FoundState.Task => "task", Model.FoundState.Later => "later", _ => "dropped" };

    private static int AvgDecisionSec(List<Card> cards)
    {
        var answered = cards.Where(c => c.AnsweredAt is not null && c.Type != CardType.Info).Select(c => (c.AnsweredAt!.Value - c.At).TotalSeconds).Where(s => s > 0 && s < 600).ToList();
        return answered.Count == 0 ? 48 : (int)Math.Round(answered.Average());
    }

    private static int MeasuredWeeks(List<Item> items)
    {
        var minutes = items.Where(i => i.ParentId is null).Sum(i => Math.Max(i.SoMin, (int)Math.Round((i.EstMin ?? 0) * (i.Factor ?? 1.2))));
        return (int)Math.Round(minutes / (60.0 * 40));
    }

    private string LastChat(List<Card> cards)
    {
        var c = cards.Where(x => x.Type == CardType.Proposals).OrderByDescending(x => x.At).FirstOrDefault();
        if (c is null) return "No design chat has proposed rulings yet.";
        var pp = Json.Read(c.PayloadJson, new ProposalsPayload());
        return $"Last design chat: '{pp.ChatTitle}', {pp.When}, produced {c.Headline.Split(' ')[0]} proposed rulings.";
    }

    private static readonly object ReportOrder = new Dictionary<string, object>
    {
        ["FTR"] = new[] { new[] { "implementing", "When implementing" }, new[] { "goal", "Goal" }, new[] { "story", "Story" }, new[] { "scope", "Scope" }, new[] { "design", "Design" }, new[] { "acceptance", "Acceptance" }, new[] { "parity", "Parity" }, new[] { "risks", "Risks" }, new[] { "impact", "Impact" }, new[] { "plan", "Plan" }, new[] { "note", "Claude's note" } },
        ["BUG"] = new[] { new[] { "implementing", "When implementing" }, new[] { "symptom", "Symptom" }, new[] { "reproduce", "Reproduce" }, new[] { "evidence", "Evidence" }, new[] { "environment", "Environment" }, new[] { "frequency", "Frequency" }, new[] { "cause", "Cause" }, new[] { "fixes", "Possible fixes" }, new[] { "workaround", "Workaround" }, new[] { "acceptance", "Acceptance" }, new[] { "note", "Claude's note" } },
        ["PER"] = new[] { new[] { "implementing", "When implementing" }, new[] { "headline", "Headline" }, new[] { "target", "Target" }, new[] { "comparison", "Comparison" }, new[] { "series", "Series" }, new[] { "profile", "Profile" }, new[] { "resources", "Resources" }, new[] { "cause", "Cause" }, new[] { "fixes", "Possible fixes" }, new[] { "guard", "Guard" }, new[] { "environment", "Environment and method" }, new[] { "note", "Claude's note" } }
    };
    private static readonly object ReportFolded = new Dictionary<string, string[]>
    {
        ["FTR"] = ["story", "scope", "design", "parity", "risks", "impact", "plan", "note"], ["BUG"] = ["environment", "workaround"], ["PER"] = ["profile", "resources", "environment"]
    };
    private static readonly object Subtypes = new Dictionary<string, string[]>
    {
        ["FTR"] = ["new construct", "new screen or component", "API or service", "migration or parity", "tooling", "documentation"],
        ["BUG"] = ["crash", "wrong result", "regression", "build or tooling break", "flaky", "data", "UI or visual", "security"],
        ["PER"] = ["latency", "startup", "throughput", "memory", "allocations", "CPU", "size", "build time", "tool responsiveness", "battery"]
    };
    private static readonly object States = new Dictionary<string, string[]>
    {
        ["FTR"] = ["idea", "shaped", "designed", "planned", "building", "review", "done", "dropped"],
        ["BUG"] = ["reported", "confirmed", "located", "fixing", "fixed", "verified", "closed", "cannot reproduce", "won't fix"],
        ["PER"] = ["observed", "targeted", "analysed", "planned", "improving", "measured", "guarded", "done", "accepted as is"]
    };
}
