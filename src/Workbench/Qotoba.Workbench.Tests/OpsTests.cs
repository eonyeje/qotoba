using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Store;
using Qotoba.Workbench.Store.Ops;

namespace Qotoba.Workbench.Tests;

/// <summary>A fresh in-memory SQLite store per test, driven exactly as hooks, MCP tools and presses drive it.</summary>
public sealed class OpsFixture : IDisposable
{
    public readonly SqliteConnection Conn = new("DataSource=:memory:");
    public readonly FakeClock Clock = new();
    public readonly FakeHost Host = new();
    public readonly OpsOptions Options = new() { MachineId = "test-pc", Owner = "Owner", Programme = "Qotoba", TimeZone = "UTC" };

    public OpsFixture()
    {
        Conn.Open();
        using var db = Db();
        db.Database.EnsureCreated();
        ProgrammeSeed.ApplyAsync(db, Clock.Now).GetAwaiter().GetResult();
    }

    public WorkbenchDb Db() => new(new DbContextOptionsBuilder<WorkbenchDb>().UseSqlite(Conn).Options);
    public WorkbenchOps Ops() => new(Db(), Clock, new NoNotifier(), Host, Options, new Fmt(Options, Clock));
    public void Dispose() => Conn.Dispose();

    /// <summary>A session that said hello, attached to an item.</summary>
    public async Task<string> SessionOnAsync(string session, string item)
    {
        await Ops().SessionStartedAsync(new HookEvent { HookEventName = "SessionStart", SessionId = session, Cwd = @"D:\repo", Source = "startup" });
        await Ops().HelloAsync(session, @"D:\repo", item);
        return session;
    }
}

public sealed class FakeClock : IClock { public DateTimeOffset Now { get; set; } = new(2026, 11, 7, 9, 0, 0, TimeSpan.Zero); }

/// <summary>The Host only reads: lines of a file, the branch a checkout is on, opening a file in Rider. It runs no git and no tests.</summary>
public sealed class FakeHost : IHostClient
{
    public Task<string?> ReadLinesAsync(string machineId, string checkout, string file, int from, int to, CancellationToken ct = default) => Task.FromResult<string?>("line");
    /// <summary>The files the fake machine holds, keyed "checkout|file"; the last read says which checkout was asked.</summary>
    public Dictionary<string, string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Reads { get; } = [];
    public Task<HostFile?> ReadFileAsync(string machineId, string checkout, string file, CancellationToken ct = default)
    {
        Reads.Add(checkout + "|" + file);
        return Task.FromResult(Files.TryGetValue(checkout + "|" + file, out var body) ? new HostFile(System.Text.Encoding.UTF8.GetBytes(body), "text/html") : null);
    }
    public Task<bool> IsOnlineAsync(string machineId, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> OpenAsync(string machineId, string file, int line, CancellationToken ct = default) => Task.FromResult(true);
    public string? Branch { get; set; }
    public Task<string?> BranchAsync(string machineId, string checkout, CancellationToken ct = default) => Task.FromResult(Branch);
    public List<WorktreeRequest> Worktrees { get; } = [];
    public Task<WorktreeResult> WorktreeAsync(string machineId, WorktreeRequest req, CancellationToken ct = default) { Worktrees.Add(req); return Task.FromResult(new WorktreeResult(true, req.Folder, req.Branch, true, "ok")); }
}

public class OpsTests : IDisposable
{
    private readonly OpsFixture f = new();
    public void Dispose() => f.Dispose();

    private static HookEvent Hook(string name, string session, string? prompt = null, string? last = null) =>
        new() { HookEventName = name, SessionId = session, Cwd = @"D:\repo", Prompt = prompt, LastAssistantMessage = last };

    [Fact]
    public async Task SessionStartGivesABriefAndNoCard()
    {
        var reply = await f.Ops().SessionStartedAsync(Hook("SessionStart", "s1"));
        Assert.Contains("session_hello", reply.HookSpecificOutput!.AdditionalContext);
        Assert.Contains("never refuses", reply.HookSpecificOutput!.AdditionalContext);
        using var db = f.Db();
        Assert.Empty(db.Cards);
        Assert.Equal(SessionState.Working, db.Sessions.Single().State);
    }

    [Fact]
    public async Task TheOwnerAddsItemsOnTheTreeAndNeverAnFpb()
    {
        var it = await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", "~3 days", null, null), Actor.Owner);
        Assert.Equal("WB-1", it.Id);
        var child = await f.Ops().AddItemAsync(new AddItemRequest("task", "Form", "WB-1", null, null, null), Actor.Owner);
        Assert.Equal("WB-1.1", child.Id);
        var ex = await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().AddItemAsync(new AddItemRequest("BUG", "x", "wb", null, null, null), Actor.Owner));
        Assert.Equal("item.kind", ex.Code);
    }

    [Fact]
    public async Task ClaudeWritesTheBreakdownTheOwnerDictated()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");
        var made = await f.Ops().AddItemsAsync("s1", "WB-1", [new("Form and validation", "~3 days", null), new("Remember me", null, [new("Token store", null, null)])]);
        Assert.Equal(["WB-1.1", "WB-1.2", "WB-1.2.1"], made.Select(i => i.Id));
        using var db = f.Db();
        Assert.Contains("from your chat", db.Items.Single(i => i.Id == "WB-1.1").HistoryJson);
        Assert.Contains("broken down", db.Items.Single(i => i.Id == "WB-1").HistoryJson);
    }

    [Fact]
    public async Task OpeningASiblingTakesYouToTheOneAlreadyOpen()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Rendering", "wb", null, null, null), Actor.Owner);
        var (a, m1) = await f.Ops().PatchItemAsync("WB-1", new ItemPatch(null, null, true));
        Assert.Equal("WB-1", a.Id); Assert.Contains("opened", m1);
        var (b, m2) = await f.Ops().PatchItemAsync("WB-2", new ItemPatch(null, null, true));
        Assert.Equal("WB-1", b.Id);
        Assert.Contains("Login screen is open at this level", m2);
        using var db = f.Db();
        Assert.False(db.Items.Single(i => i.Id == "WB-2").IsOpen);
    }

    [Fact]
    public async Task ClaudeOpensTheNodeTheOwnerNamedInChat()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Form", "WB-1", null, null, null), Actor.Owner);
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Remember me", "WB-1", null, null, null), Actor.Owner);
        await f.Ops().SessionStartedAsync(Hook("SessionStart", "s1"));
        await f.Ops().HelloAsync("s1", @"D:\repo", null);
        var (top, m1) = await f.Ops().OpenNodeFromChatAsync("s1", "WB-1");
        Assert.True(top.IsOpen); Assert.Contains("wb/1-login-screen", m1);
        var (form, _) = await f.Ops().OpenNodeFromChatAsync("s1", "WB-1.1");
        Assert.Equal("WB-1.1", form.Id);
        var (again, m3) = await f.Ops().OpenNodeFromChatAsync("s1", "WB-1.2");
        Assert.Equal("WB-1.1", again.Id); Assert.Contains("Form is open at this level", m3);
        using var db = f.Db();
        Assert.Equal("WB-1.1", db.Sessions.Single().ItemId);
        Assert.Contains("from your chat", db.Items.Single(i => i.Id == "WB-1").HistoryJson);
        Assert.Empty(db.Cards);
    }

    [Fact]
    public async Task StepsGiveProgressAndDoneIsDataNotACard()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.Ops().PatchItemAsync("WB-1", new ItemPatch(null, null, true));
        await f.SessionOnAsync("s1", "WB-1");
        await f.Ops().StepsWriteAsync("s1", "WB-1", [new("Draw the form", 30, null, null), new("Validate", 20, null, null), new("Wire the button", 10, null, null)], "The form logs in");
        await f.Ops().StepUpdateAsync("s1", "WB-1", 1, "done", null);
        using (var db = f.Db())
        {
            var it = db.Items.Single(i => i.Id == "WB-1");
            Assert.Equal(60, it.EstMin);
            Assert.Equal(ItemState.Building, it.State);
            Assert.Single(Json.ReadList<Step>(it.StepsJson), s => s.State == StepState.Done);
        }
        // a revision keeps the state of the steps whose text stayed
        await f.Ops().StepsWriteAsync("s1", "WB-1", [new("Draw the form", 30, null, null), new("Validate", 20, null, null), new("Wire the button", 10, null, null), new("Remember me", 15, null, null)], null);
        await f.Ops().SessionPromptAsync(Hook("UserPromptSubmit", "s1", "go"));
        f.Clock.Now = f.Clock.Now.AddMinutes(45);
        await f.Ops().SessionStopAsync(Hook("Stop", "s1"));
        var msg = await f.Ops().ItemDoneAsync("s1", "WB-1", "The login form validates and logs in", null);
        Assert.Contains("45 min against 75", msg);
        using var db2 = f.Db();
        var done = db2.Items.Single(i => i.Id == "WB-1");
        Assert.Equal(ItemState.Done, done.State);
        Assert.False(done.IsOpen);
        Assert.Equal("The login form validates and logs in", done.Outcome);
        Assert.Empty(db2.Cards);
        Assert.NotNull(db2.Projects.Single(p => p.Id == "wb").Factor);
    }

    [Fact]
    public async Task APageKeepsTheOwnersAnswersAndApprovingMakesThemDecisions()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Virtual collection", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");
        var card = await f.Ops().SubmitPageAsync("s1", "WB-1", "Virtual collection design", "<h1>Design</h1><p>Three classes and how they relate, with the choices beside each.</p>", "Spans several classes; no designer covers it");
        Assert.StartsWith("pg-", card.Id);
        Assert.Equal(CardType.Page, card.Type);
        await f.Ops().PageAnswerAsync(card.Id, new PageAnswer("window-owner", "the collection, not the view"));
        await f.Ops().PageAnswerAsync(card.Id, new PageAnswer("window-owner", "the collection"));
        await f.Ops().PageAnswerAsync(card.Id, new PageAnswer("slide-only", "yes"));
        var answers = await f.Ops().PageAnswersAsync(card.Id);
        Assert.Contains("window-owner: the collection", answers);
        Assert.DoesNotContain("not the view", answers);
        var r = await f.Ops().PressAsync(new PressRequest(card.Id, "approve"));
        Assert.True(r.Ok, r.Message);
        Assert.Contains("2 answers", r.Message);
        using var db = f.Db();
        var it = db.Items.Single(i => i.Id == "WB-1");
        Assert.Equal(2, Json.ReadList<Decision>(it.DecisionsJson).Count);
        Assert.Equal(CardState.Approved, db.Cards.Single().State);
        var m = db.Messages.Single();
        Assert.Contains("agreed design", m.Text);
        Assert.Contains("slide-only: yes", m.Text);
        Assert.Equal(SessionState.Working, db.Sessions.Single().State);
    }

    [Fact]
    public async Task ChangesAskedOnAPageReachTheSessionInTheOwnersWords()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Virtual collection", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");
        var card = await f.Ops().SubmitPageAsync("s1", "WB-1", "Design", "<p>A page with enough body to pass the size check, with sections.</p>", null);
        var r = await f.Ops().PressAsync(new PressRequest(card.Id, "changes", Text: "one window only, no skip and take"));
        Assert.True(r.Ok);
        var reply = await f.Ops().SessionStopAsync(Hook("Stop", "s1"));
        Assert.Equal("block", reply.Decision);
        Assert.Contains("one window only", reply.Reason);
        var waited = await f.Ops().WaitAnswerAsync(card.Id, TimeSpan.FromSeconds(1));
        Assert.Contains("one window only", waited);
    }

    [Fact]
    public async Task ADeviationIsAllowedOrSentBack()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");
        var c1 = await f.Ops().ReportDeviationAsync("s1", "WB-1", "A static helper for date parsing", "The parser is called from three widgets", "No statics", "An instance on the culture", ["Keep the static", "Instance on the culture"]);
        Assert.StartsWith("dv-", c1.Id);
        var r = await f.Ops().PressAsync(new PressRequest(c1.Id, "pick", Index: 1));
        Assert.True(r.Ok);
        using (var db = f.Db())
        {
            Assert.Contains("Instance on the culture", db.Messages.Single().Text);
            Assert.Single(Json.ReadList<Decision>(db.Items.Single().DecisionsJson));
        }
        var c2 = await f.Ops().ReportDeviationAsync("s1", "WB-1", "Skip the remember-me token", "The vault is not there yet", null, null, null);
        var back = await f.Ops().PressAsync(new PressRequest(c2.Id, "back", Text: "no, build the vault first"));
        Assert.True(back.Ok);
        using var db2 = f.Db();
        Assert.Equal(CardState.Answered, db2.Cards.Single(c => c.Id == c2.Id).State);
        Assert.Contains("build the vault first", db2.Messages.OrderBy(m => m.Id).Last().Text);
    }

    [Fact]
    public async Task StuckStopsTheSessionAndRetryFreesIt()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");
        await f.Ops().StepsWriteAsync("s1", "WB-1", [new("one", 30, null, null), new("two", 30, null, null)], null);
        var card = await f.Ops().ReportStuckAsync("s1", "WB-1", 2, "tests · LoginTests.Submits", [new("retried", "fail", "expected 200 got 500"), new("cleared the cache", "fail", "same"), new("rebuilt", "fail", "same")], "The handler never registers.");
        using (var db = f.Db())
        {
            Assert.Equal(ItemState.Blocked, db.Items.Single().State);
            Assert.True(db.Sessions.Single().Stuck);
        }
        var r = await f.Ops().PressAsync(new PressRequest(card.Id, "retry"));
        Assert.True(r.Ok);
        using var db2 = f.Db();
        Assert.Equal(ItemState.Building, db2.Items.Single().State);
        Assert.False(db2.Sessions.Single().Stuck);
    }

    [Fact]
    public async Task TheHookNeverRefusesGitAndWritesItDown()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");
        var input = System.Text.Json.JsonDocument.Parse("""{"command":"git merge wb/1-login-screen && git push origin main"}""").RootElement;
        var pre = await f.Ops().SessionPreToolAsync(new HookEvent { HookEventName = "PreToolUse", SessionId = "s1", ToolName = "Bash", ToolInput = input });
        Assert.Null(pre.HookSpecificOutput?.PermissionDecision);
        await f.Ops().SessionPostToolAsync(new HookEvent { HookEventName = "PostToolUse", SessionId = "s1", ToolName = "Bash", ToolInput = input }, false);
        using var db = f.Db();
        Assert.Contains("pushed", db.Items.Single().HistoryJson);
    }

    [Fact]
    public async Task TestResultsAreKeptPerProjectWithThePreviousOne()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");
        var ev = new HookEvent { HookEventName = "PostToolUse", SessionId = "s1", ToolName = "Bash", DurationMs = 4200,
            ToolInput = System.Text.Json.JsonDocument.Parse("""{"command":"dotnet test src/Workbench/Qotoba.Workbench.Tests"}""").RootElement,
            ToolResponse = System.Text.Json.JsonDocument.Parse("""{"stdout":"Passed!  - Failed:     0, Passed:    41, Skipped:     0, Total:    41"}""").RootElement };
        await f.Ops().SessionPostToolAsync(ev, false);
        ev.ToolResponse = System.Text.Json.JsonDocument.Parse("""{"stdout":"Failed!  - Failed:     1, Passed:    40, Skipped:     0, Total:    41"}""").RootElement;
        await f.Ops().SessionPostToolAsync(ev, true);
        using var db = f.Db();
        var results = Json.ReadList<CheckResult>(db.Projects.Single(p => p.Id == "wb").ResultsJson);
        var r = Assert.Single(results);
        Assert.StartsWith("tests", r.Check);
        Assert.Equal("fail", r.Result); Assert.Equal(40, r.Passed);
        Assert.NotNull(r.Prev); Assert.Equal("pass", r.Prev!.Result); Assert.Equal(41, r.Prev.Passed);
        Assert.Empty(db.Cards);
    }

    [Fact]
    public async Task ADecisionSaidInChatIsRecordedStraightAway()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");
        var r = await f.Ops().RecordDecisionAsync("s1", "A virtual window does one thing: slide. Skip and take are the RangeCollection's.", null, null);
        Assert.Equal("R-1", r.Id);
        Assert.StartsWith("said in chat", r.Origin);
        var brief = await f.Ops().BriefAsync("s1");
        Assert.Contains("A virtual window does one thing", brief);
        await f.Ops().WithdrawRulingAsync(r.Id, "no longer true");
        Assert.DoesNotContain("A virtual window does one thing", await f.Ops().BriefAsync("s1"));
        using var db = f.Db();
        Assert.Empty(db.Cards);
    }

    [Fact]
    public async Task ADecisionNamedByProjectCodeLandsOnTheProjectById()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Design QDL", "lang", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "LNG-1");
        var byCode = await f.Ops().RecordDecisionAsync("s1", "Never refer to tasks by their ids.", null, "LNG");
        var byId = await f.Ops().RecordDecisionAsync("s1", "Only I can initiate a rule being removed.", null, "wb");
        var mine = await f.Ops().RecordDecisionAsync("s1", "Rulings are process, not results.", null, null);
        var wrong = await f.Ops().RecordDecisionAsync("s1", "A code nobody knows still records.", null, "NOPE");
        Assert.Equal("lang", byCode.ProjectId);
        Assert.Equal("wb", byId.ProjectId);
        Assert.Equal("lang", mine.ProjectId);
        Assert.Equal("lang", wrong.ProjectId);
    }

    [Fact]
    public async Task ADecisionIsWithdrawnOnTheOwnersWordAndStaysInHistoryStruckThrough()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Design QDL", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");
        var old = await f.Ops().RecordDecisionAsync("s1", "Variables in QDL are written as label = part.", null, null);
        var now = await f.Ops().RecordDecisionAsync("s1", "Variables in QDL are written with a dollar sign, $body.", null, null);
        Assert.Contains("label = part", await f.Ops().BriefAsync("s1"));

        // never on Claude's own judgement: the reason is the owner's words, and a decision cannot replace itself
        await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().WithdrawDecisionAsync("s1", old.Id, " ", null));
        await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().WithdrawDecisionAsync("s1", old.Id, "owner: superseded", old.Id));

        var msg = await f.Ops().WithdrawDecisionAsync("s1", old.Id, "owner: rulings are process, not design results; the dollar sign stands", now.Id);
        Assert.Contains($"replaced by {now.Id}", msg);
        var brief = await f.Ops().BriefAsync("s1");
        Assert.DoesNotContain("label = part", brief);
        Assert.Contains("dollar sign", brief);
        using var db = f.Db();
        var r = await db.Rulings.FindAsync(old.Id);
        Assert.NotNull(r);                                   // history keeps it
        Assert.NotNull(r!.WithdrawnAt);
        Assert.Equal(now.Id, r.SupersededById);
        Assert.StartsWith("owner:", r.WithdrawnWhy);
        var item = await db.Items.FindAsync("WB-1");
        Assert.Contains("withdrawn", item!.HistoryJson);
        Assert.Empty(db.Cards);
    }

    [Fact]
    public async Task TheOwnersOwnWordsReachTheSessionOnAnyCard()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");
        await f.Ops().StepsWriteAsync("s1", "WB-1", [new("one", 30, null, null)], null);
        var card = await f.Ops().ReportStuckAsync("s1", "WB-1", 1, "build", [new("a", "fail", null), new("b", "fail", null)], "x");
        var r = await f.Ops().PressAsync(new PressRequest(card.Id, "write", Text: "delete the obj folder and build again"));
        Assert.True(r.Ok);
        using var db = f.Db();
        Assert.Contains("delete the obj folder", db.Messages.Single().Text);
    }
}
