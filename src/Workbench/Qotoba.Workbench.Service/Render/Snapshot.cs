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
        var items = await db.Items.OrderBy(i => i.Order).ToListAsync(ct);
        var cards = await db.Cards.ToListAsync(ct);
        var assumptions = await db.Assumptions.ToListAsync(ct);
        var rulings = await db.Rulings.OrderBy(r => r.Date).ToListAsync(ct);
        var sessions = await db.Sessions.ToListAsync(ct);
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
        var burns = await db.Settings.Where(s => s.Key.StartsWith("burn:")).ToDictionaryAsync(s => s.Key[5..], s => Json.ReadList<BurnSample>(s.Value), ct);
        var decisionsMeasured = await db.Cards.CountAsync(c => c.AnsweredAt != null && c.Type != CardType.Info, ct);
        var firstDecision = await db.Cards.Where(c => c.AnsweredAt != null).OrderBy(c => c.AnsweredAt).Select(c => c.AnsweredAt).FirstOrDefaultAsync(ct);

        var byItem = items.ToDictionary(i => i.Id);
        var children = items.GroupBy(i => i.ParentId ?? "").ToDictionary(g => g.Key, g => g.OrderBy(i => i.Order).Select(i => i.Id).ToList());
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
            ["budgets"] = new object[] { new { key = "noteLines", label = "Lines in a Claude note", value = Caps.NoteLines, def = 2, note = "Anything longer belongs on the item's page." } },
            ["projects"] = projects.Select(p => ProjectView(p, items, cards, edges, burns, sessions, reports)).ToList(),
            ["lanes"] = new Dictionary<string, object?>(),
            ["items"] = items.ToDictionary(i => i.Id, i => (object?)ItemView(i, branches, children, sessionsByItem, comments, edges, reports, cardsById)),
            ["questions"] = new Dictionary<string, object?>(),
            ["assumptions"] = assumptions.ToDictionary(a => a.Id, a => (object?)new { session = a.SessionId, item = a.ItemId, state = AssumptionState(a.State), text = a.Text }),
            ["found"] = new Dictionary<string, object?>(),
            ["rulings"] = rulings.ToDictionary(r => r.Id, r => (object?)new
            {
                project = r.ProjectId ?? "wb", title = r.Title, date = fmt.Day(r.Date), neverReopened = r.NeverReopened, text = r.Text, origin = r.Origin,
                question = (string?)null, session = r.SessionId, item = r.ItemId is null ? null : "item:" + r.ItemId,
                related = Json.ReadList<string>(r.RelatedJson), reopenNote = r.ReopenNote
            }),
            ["proposed"] = new { chat = "", when = "", turns = 0, items = new List<object>(), card = (string?)null, item = (string?)null },
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
            ["runs"] = new Dictionary<string, object?>(),
            ["sessions"] = sessions.ToDictionary(s => s.Id, s => (object?)SessionView(s, byItem, cards, assumptions)),
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
            ["packets"] = new Dictionary<string, object?>(),
            ["plans"] = new Dictionary<string, object?>(),
            ["pages"] = cards.Where(c => c.Type == CardType.Page).ToDictionary(c => c.Id, c => (object?)PageView(c)),
            ["deviations"] = cards.Where(c => c.Type == CardType.Deviation).ToDictionary(c => c.Id, c => (object?)DeviationView(c)),
            ["stucks"] = cards.Where(c => c.Type == CardType.Stuck).ToDictionary(c => c.Id, c => (object?)StuckView(c)),
            ["settings"] = new
            {
                notifications = new object[]
                {
                    new { t = "One OS notification per card", on = true, note = "Page, Deviation and Stuck cards. Info cards never notify." },
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
                explain = "You direct Claude in chat. What you decide there is recorded straight away as a decision; what you ask for is written into the tree or the register.",
                last = ""
            },
            ["register"] = Empty,
            ["reportOrder"] = ReportOrder,
            ["reportFolded"] = ReportFolded,
            ["subtypes"] = Subtypes,
            ["states"] = States,
            ["fpbs"] = reports.ToDictionary(r => r.Id, r => (object?)ReportView(r, edges, rulings)),
            ["edges"] = edges.Select(e => new { id = e.Id, from = e.From, to = e.To, type = e.Type.ToString().ToLowerInvariant(), hard = e.Hard, reason = e.Reason, by = e.By == Actor.Owner ? "you" : "claude", session = e.SessionId, confirmed = e.Confirmed, min = e.Min }).ToList(),
            ["cardWhy"] = cards.ToDictionary(c => c.Id, c => (object?)c.Why),
            ["cardOrigin"] = cards.ToDictionary(c => c.Id, c => (object?)new { step = c.OriginStep, from = Json.ReadList<string>(c.OriginFromJson), fromText = c.OriginText })
        };
        return wb;
    }

    /* ---------------------------------------------------------------- views */

    private object ProjectView(Project p, List<Item> items, List<Card> cards, List<Edge> edges, Dictionary<string, List<BurnSample>> burns, List<Session> sessions, List<Report> reports)
    {
        var mine = items.Where(i => i.ProjectId == p.Id).ToList();
        var leaves = mine.Where(i => !mine.Any(k => k.ParentId == i.Id)).ToList();                 // progress counts the leaves, so a parent is not counted twice
        var doneLeaves = leaves.Count(i => i.State == Model.ItemState.Done);
        double LeafProgress(Item i)
        {
            if (i.State == Model.ItemState.Done) return 100;
            var st = Json.ReadList<Step>(i.StepsJson);
            return st.Count == 0 ? 0 : 100.0 * st.Count(x => x.State == StepState.Done) / st.Count;
        }
        var pct = leaves.Count == 0 ? 0 : (int)Math.Round(leaves.Average(LeafProgress));
        var leftMin = leaves.Where(i => i.State != Model.ItemState.Done).Sum(i =>
        {
            var est = i.EstMin ?? Json.ReadList<Step>(i.StepsJson).Sum(x => x.EstMin);
            return Math.Max(0, (int)Math.Round(est * (i.Factor ?? p.Factor ?? 1.2)) - i.SoMin);
        });
        var unsized = leaves.Count(i => i.State != Model.ItemState.Done && (i.EstMin ?? 0) == 0 && Json.ReadList<Step>(i.StepsJson).Count == 0);
        var ids = mine.Select(i => i.Id).ToHashSet();
        var mySessions = sessions.Where(s => s.ItemId is not null && ids.Contains(s.ItemId)).ToList();
        var weekAgo = Now.AddDays(-7);
        var paceMin = mySessions.Where(s => (s.LastEvent ?? s.Started) >= weekAgo).Sum(s => s.Minutes) / 7.0;
        var etaDays = paceMin <= 0 || leftMin == 0 ? (double?)null : Math.Round(leftMin / paceMin, 1);
        var now = mySessions.Where(s => s.State != SessionState.Ended && s.LastEvent is { } le && (Now - le) < TimeSpan.FromHours(6)).Select(s => s.Id).ToList();
        var waiting = cards.Where(c => c.ProjectId == p.Id && c.State == CardState.Open && c.Type != CardType.Info).Select(c => "card:" + c.Id).ToList();
        var fpbs = reports.Where(r => r.ProjectId == p.Id).ToList();
        var lastEvent = mySessions.Select(s => s.LastEvent ?? s.Started).DefaultIfEmpty().Max();
        var top = mine.Where(i => i.ParentId is null).OrderBy(i => i.Order).Select(i => i.Id).ToList();
        var next = cards.Where(c => c.ProjectId == p.Id && c.State == CardState.Open && c.Type != CardType.Info).OrderByDescending(c => c.WeightMin).FirstOrDefault();
        var measured = MeasuredWeeks(mine);
        var open = p.State == ProjectState.Open;
        var burn = burns.GetValueOrDefault(p.Id) ?? [];
        return new
        {
            id = p.Id, code = p.Code, name = p.Name, color = p.Color, state = open ? "open" : "coarse", coarseWeeks = p.CoarseWeeks, measuredWeeks = measured,
            isOpen = p.IsOpen, branch = p.Branch ?? WorkbenchOps.BranchNameFor(p), branchRecorded = p.Branch is not null, checkout = p.Checkout,
            factor = open ? p.Factor : null, purpose = p.Purpose,
            children = open ? top : new List<string>(), lanes = new List<string>(),
            burn = burn.Count >= 2 ? burn.Select(b => b.H).ToArray() : [], burnDays = burn.Count >= 2 ? burn.Select(b => b.Day).ToArray() : [],
            criticalPath = WorkbenchOps.CriticalPath(items, edges, p.Id), nextDecision = next is null ? null : "card:" + next.Id, repo = p.Repo, closedWhy = p.ClosedWhy,
            preview = open ? null : top,
            results = Json.ReadList<CheckResult>(p.ResultsJson).Select(r => new { check = r.Check, result = r.Result, passed = r.Passed, total = r.Total, dur = r.DurationSec, at = r.At, cmd = r.Cmd, session = r.SessionId, item = r.ItemId, prev = r.Prev is null ? null : new { result = r.Prev.Result, passed = r.Prev.Passed, total = r.Prev.Total, dur = r.Prev.DurationSec, at = r.Prev.At } }).ToList(),
            done = doneLeaves, total = leaves.Count, pct, leftMin, leftHours = Math.Round(leftMin / 60.0, 1), unsized,
            paceMinPerDay = (int)Math.Round(paceMin), etaDays, sessionsNow = now, waiting,
            fpbOpen = fpbs.Count(r => !ReportStates.IsClosed(r.State)), fpbTotal = fpbs.Count,
            lastActivity = lastEvent == default ? null : fmt.DayTime(lastEvent),
            itemsByState = new { done = mine.Count(i => i.State == Model.ItemState.Done), building = mine.Count(i => i.State is Model.ItemState.Building or Model.ItemState.Blocked or Model.ItemState.Planning), notStarted = mine.Count(i => i.State is Model.ItemState.Coarse or Model.ItemState.Planned) }
        };
    }


    /// <summary>One point of the burn-down: measured hours of work left on a day.</summary>
    public sealed record BurnSample(string Day, int H);

    private object ItemView(Item i, List<Branch> branches, Dictionary<string, List<string>> children, Dictionary<string, List<Session>> sessionsByItem,
        List<Comment> comments, List<Edge> edges, List<Report> reports, Dictionary<string, Card> cards)
    {
        var steps = Json.ReadList<Step>(i.StepsJson);
        var kids = children.GetValueOrDefault(i.Id, []);
        var mySessions = sessionsByItem.GetValueOrDefault(i.Id, []);
        var est = i.EstMin ?? steps.Sum(s => s.EstMin);
        var factor = i.Factor ?? 1.2;
        var forecast = est == 0 ? (int?)null : (int)Math.Round(est * factor);
        var itemRef = "item:" + i.Id;
        var needs = edges.Where(e => e.From == itemRef && e.Type == EdgeType.Needs).Select(e => new { @ref = e.To, min = e.Min ?? 0, note = (string?)null }).ToList();
        var blocks = edges.Where(e => e.To == itemRef && e.Type == EdgeType.Needs).Select(e => new { @ref = e.From, min = e.Min ?? 0, note = (string?)null }).ToList();
        var decisions = Json.ReadList<Decision>(i.DecisionsJson).Select(d => new { t = d.T, a = d.A, how = d.How, @ref = d.Ref, from = d.From, date = d.Date, ruling = d.Ruling }).ToList();
        var openCards = cards.Values.Where(c => c.ItemId == i.Id && c.State == CardState.Open && c.Type != CardType.Info).Select(c => "card:" + c.Id).ToList();
        var done = steps.Count(s => s.State == StepState.Done);
        return new
        {
            kind = "task", title = i.Title, project = i.ProjectId, parent = i.ParentId, children = kids,
            state = StateWord(i), isOpen = i.IsOpen, lane = (string?)null, machine = i.HomeMachineId, branch = i.Branch is null ? null : (branches.FirstOrDefault(b => b.Name == i.Branch)?.Id ?? i.Branch), branchName = i.Branch, coarse = i.CoarseSize, goal = i.Goal,
            steps = steps.Select(s => new { t = s.Text, est = s.EstMin, state = s.State.ToString().ToLowerInvariant(), proof = (string?)null, needs = s.Needs, note = s.Note }).ToList(),
            progress = steps.Count == 0 ? (i.State == Model.ItemState.Done ? 100 : 0) : (int)Math.Round(100.0 * done / steps.Count),
            checks = new List<string>(),
            changes = new { files = new List<object>(), types = new List<string>() },
            time = new { so = i.SoMin, est, factor, forecast },
            needs, blocks,
            commentScope = comments.Where(c => c.ItemId == i.Id).Select(c => c.Id).ToList(),
            note = i.Note is null ? null : new { text = i.Note, date = i.NoteAt is null ? "" : fmt.DayTime(i.NoteAt.Value), session = i.NoteSessionId is null ? null : "session:" + i.NoteSessionId },
            outcome = i.Outcome,
            claims = new List<object>(),
            rulings = new List<string>(),
            sessions = mySessions.OrderBy(s => s.Started).Select(s => new object[] { "session:" + s.Id, s.Minutes }).ToList(),
            packet = (string?)null,
            plan = (string?)null,
            cards = openCards,
            fpbs = reports.Where(r => r.AttachItemId == i.Id || r.TakenOnItemId == i.Id).Select(r => r.Id).ToList(),
            implements = new List<string>(),
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

    private object SessionView(Session s, Dictionary<string, Item> items, List<Card> cards, List<Assumption> assumptions)
    {
        var it = s.ItemId is null ? null : items.GetValueOrDefault(s.ItemId);
        var steps = it is null ? [] : Json.ReadList<Step>(it.StepsJson);
        var cur = steps.FirstOrDefault(x => x.State == StepState.Current) ?? (s.StepN is int n ? steps.FirstOrDefault(x => x.N == n) : null);
        var stepText = cur is null ? "" : $"{cur.N} of {steps.Count} · {cur.Text}";
        var waitingOn = cards.Where(c => c.SessionId == s.Id && c.State == CardState.Open && c.Type != CardType.Info).OrderByDescending(c => c.WeightMin).Select(c => "card:" + c.Id).ToList();
        var minutes = s.Minutes + (s.TurnStarted is { } ts && s.State != SessionState.Ended ? Fmt.MinutesBetween(ts, Now) : 0);
        return new
        {
            title = string.IsNullOrEmpty(s.Title) ? (it?.Title ?? "Session " + s.Id[..Math.Min(4, s.Id.Length)]) : s.Title,
            machine = s.MachineId, item = s.ItemId, project = s.ProjectId, lane = (string?)null, state = s.State.ToString().ToLowerInvariant(), stuck = s.Stuck,
            started = fmt.DayTime(s.Started), ended = s.Ended is null ? null : fmt.DayTime(s.Ended.Value), minutes, turns = s.Turns, tools = s.Tools,
            step = stepText, stepMin = s.StepMin, lastAction = s.LastAction, waitingSince = s.WaitingSince is null ? null : fmt.Time(s.WaitingSince.Value), waitingOn,
            checks = new List<string>(), questions = new List<string>(),
            assumptions = assumptions.Where(a => a.SessionId == s.Id).Select(a => a.Id).ToList(),
            found = new List<string>(), packet = (string?)null,
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
            clock, clockWarn = m.Id != reference.Id && Math.Abs(m.ClockSkewSec) >= 1.0, token = fmt.DayTime(m.Registered), kind = m.Kind.ToString().ToLowerInvariant(), ephemeral = m.Ephemeral,
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
            type = c.Type.ToString().ToLowerInvariant(), question = (string?)null, item = c.ItemId, project = c.ProjectId, session = c.SessionId, weight = c.WeightMin,
            waiting = Fmt.Minutes(waitingMin), at = SameDay(c.At) ? fmt.Time(c.At) : fmt.DayTime(c.At), headline = c.Headline, why = c.Why,
            evidence = Json.Read<List<object>>(c.EvidenceJson, []),
            approved = c.State == CardState.Approved ? fmt.DayTime(c.AnsweredAt ?? Now) : null,
            answered = c.State == CardState.Answered ? fmt.DayTime(c.AnsweredAt ?? Now) : null,
            answer = c.Answer, preview = c.State == CardState.Preview ? true : (bool?)null,
            deferred = c.State == CardState.Deferred ? true : (bool?)null, dismissed = c.State == CardState.Dismissed ? true : (bool?)null,
            age = Fmt.Minutes(waitingMin), chip = info?.Chip, body = info?.Body, origin = info?.Origin, ruling = info?.Ruling
        };
    }

    private object PageView(Card c)
    {
        var pg = Json.Read(c.PayloadJson, new PagePayload());
        return new
        {
            item = c.ItemId, session = c.SessionId, machine = pg.Machine, title = pg.Title, html = pg.Html, purpose = pg.Purpose,
            answers = pg.Answers, answersAt = pg.AnswersAt, approved = pg.Approved ? pg.ApprovedAt ?? "approved" : null,
            state = pg.Approved ? "approved" : c.State == CardState.Answered ? "changes asked" : c.State == CardState.Dismissed ? "dismissed" : "waiting"
        };
    }

    private static object DeviationView(Card c)
    {
        var dv = Json.Read(c.PayloadJson, new DeviationPayload());
        return new { item = c.ItemId, session = c.SessionId, machine = dv.Machine, what = dv.What, why = dv.Why, agreed = dv.Agreed, instead = dv.Instead, options = dv.Options, decided = dv.Decided };
    }

    private static object StuckView(Card c)
    {
        var st = Json.Read(c.PayloadJson, new StuckPayload());
        return new
        {
            item = c.ItemId, session = c.SessionId, machine = st.Machine, step = $"step {st.Step} of {st.Of}", check = (string?)null, checkName = st.CheckName, lastOutput = st.LastOutput,
            attempts = st.Attempts.Select(a => new { t = a.T, result = a.Result, run = (string?)null, output = a.Output }).ToList(), diagnosis = st.Diagnosis, reassign = st.Reassign
        };
    }

    private object ReportView(Report r, List<Edge> edges, List<Ruling> rulings)
    {
        var sections = Json.Read<Dictionary<string, object>>(r.SectionsJson, []);
        var decided = rulings.Where(x => x.NeverReopened && (x.Text.Contains(r.Id, StringComparison.OrdinalIgnoreCase) || x.RelatedJson.Contains("fpb:" + r.Id))).Select(x => "ruling:" + x.Id).ToList();
        var parts = sections.TryGetValue("parts", out var pv) && pv is System.Text.Json.JsonElement pe && pe.ValueKind == System.Text.Json.JsonValueKind.Object && pe.TryGetProperty("rows", out var rows) && rows.ValueKind == System.Text.Json.JsonValueKind.Array
            ? rows.EnumerateArray().Select(x => new { t = x.TryGetProperty("t", out var t) ? t.GetString() : x.TryGetProperty("part", out var pp) ? pp.GetString() : "", state = x.TryGetProperty("state", out var st) ? st.GetString() : "", note = x.TryGetProperty("note", out var nt) ? nt.GetString() : null }).ToList() : [];
        var me = "fpb:" + r.Id;
        var links = edges.Where(e => e.From == me || e.To == me).Select(e => new { id = e.Id, other = e.From == me ? e.To : e.From, type = e.Type.ToString().ToLowerInvariant(), outward = e.From == me, reason = e.Reason }).ToList();
        return new
        {
            kind = r.Kind.ToString(), subtype = r.Subtype, title = r.Title, project = r.ProjectId, attach = r.AttachItemId is null ? "project:" + r.ProjectId : "item:" + r.AttachItemId,
            takenOn = r.TakenOnItemId, state = r.State, left = r.Left, closed = ReportStates.IsClosed(r.State), machine = (string?)null, branch = (string?)null, lane = (string?)null,
            severity = r.Severity is null ? null : new { level = r.Severity.ToString()!.ToLowerInvariant(), reason = r.SeverityReason, by = r.SeverityBy == Author.Me ? "you" : "claude", confirmed = r.SeverityBy == Author.Me },
            time = new { est = r.EstMin, estBy = r.EstBy, actual = 0, factor = (double?)null, forecast = (int?)null },
            source = new { type = r.SourceType, session = r.SourceSessionId, found = (string?)null, date = fmt.DayTime(r.Created) },
            changed = r.Changed is null ? null : fmt.DayTime(r.Changed.Value),
            sections, links, parts, steps = new List<object>(), checks = new List<string>(), changes = (object?)null, sessions = new List<string>(), rulings = decided,
            history = Json.Read<List<object>>(r.HistoryJson, []), takenOnBy = r.TakenOnItemId is null ? new List<string>() : [r.TakenOnItemId]
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

    private static readonly object ReportOrder = new Dictionary<string, object>
    {
        ["FTR"] = new[] { new[] { "why", "Why" }, new[] { "parts", "Parts" }, new[] { "evidence", "Evidence" }, new[] { "story", "Story" }, new[] { "scope", "Scope" }, new[] { "design", "Design" }, new[] { "acceptance", "Acceptance" }, new[] { "parity", "Parity" }, new[] { "risks", "Risks" }, new[] { "impact", "Impact" }, new[] { "plan", "Plan" }, new[] { "note", "Claude's note" } },
        ["BUG"] = new[] { new[] { "why", "Why" }, new[] { "symptom", "Symptom" }, new[] { "parts", "Parts" }, new[] { "evidence", "Evidence" }, new[] { "reproduce", "Reproduce" }, new[] { "environment", "Environment" }, new[] { "frequency", "Frequency" }, new[] { "cause", "Cause" }, new[] { "fixes", "Possible fixes" }, new[] { "workaround", "Workaround" }, new[] { "acceptance", "Acceptance" }, new[] { "note", "Claude's note" } },
        ["PER"] = new[] { new[] { "why", "Why" }, new[] { "headline", "Headline" }, new[] { "parts", "Parts" }, new[] { "evidence", "Evidence" }, new[] { "target", "Target" }, new[] { "comparison", "Comparison" }, new[] { "series", "Series" }, new[] { "profile", "Profile" }, new[] { "resources", "Resources" }, new[] { "cause", "Cause" }, new[] { "fixes", "Possible fixes" }, new[] { "guard", "Guard" }, new[] { "environment", "Environment and method" }, new[] { "note", "Claude's note" } }
    };
    private static readonly object ReportFolded = new Dictionary<string, string[]>
    {
        ["FTR"] = ["story", "scope", "design", "acceptance", "parity", "risks", "impact", "plan", "note"], ["BUG"] = ["reproduce", "environment", "frequency", "cause", "fixes", "workaround", "acceptance", "note"], ["PER"] = ["target", "comparison", "series", "profile", "resources", "cause", "fixes", "guard", "environment", "note"]
    };
    private static readonly object Subtypes = new Dictionary<string, string[]>
    {
        ["FTR"] = ["new construct", "new screen or component", "API or service", "migration or parity", "tooling", "documentation"],
        ["BUG"] = ["crash", "wrong result", "regression", "build or tooling break", "flaky", "data", "UI or visual", "security"],
        ["PER"] = ["latency", "startup", "throughput", "memory", "allocations", "CPU", "size", "build time", "tool responsiveness", "battery"]
    };
    private static readonly object States = new Dictionary<string, string[]>
    {
        ["FTR"] = ReportStates.All, ["BUG"] = ReportStates.All, ["PER"] = ReportStates.All
    };
}
