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
}

public sealed class FakeClock : IClock { public DateTimeOffset Now { get; set; } = new(2026, 11, 7, 9, 0, 0, TimeSpan.Zero); }

public sealed class FakeHost : IHostClient
{
    public List<GitInstruction> Ran { get; } = [];
    public bool Ok { get; set; } = true;
    public Task<GitResult> RunGitAsync(string machineId, GitInstruction instruction, CancellationToken ct = default) { Ran.Add(instruction); return Task.FromResult(new GitResult(Ok, Ok ? "merged a1b2c3d" : "conflict")); }
    public Task<string?> ReadLinesAsync(string machineId, string checkout, string file, int from, int to, CancellationToken ct = default) => Task.FromResult<string?>("line");
    public Task<bool> IsOnlineAsync(string machineId, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> OpenAsync(string machineId, string file, int line, CancellationToken ct = default) => Task.FromResult(true);
    public string? Branch { get; set; }
    public Task<string?> BranchAsync(string machineId, string checkout, CancellationToken ct = default) => Task.FromResult(Branch);
}

public class OpsTests : IDisposable
{
    private readonly OpsFixture f = new();
    public void Dispose() => f.Dispose();

    private static HookEvent Hook(string name, string session, string? prompt = null, string? last = null) =>
        new() { HookEventName = name, SessionId = session, Cwd = @"D:\repo", Prompt = prompt, LastAssistantMessage = last };

    private static List<OptionInput> Options() =>
    [
        new("Keep 40 lines", true, "Matches the cap.", "small", null),
        new("Stream live", false, "Needs a push channel.", "large", null)
    ];

    [Fact]
    public async Task SessionStartMakesAnInfoCardAndABrief()
    {
        var reply = await f.Ops().SessionStartedAsync(Hook("SessionStart", "s1"));
        Assert.Contains("session_hello", reply.HookSpecificOutput!.AdditionalContext);
        using var db = f.Db();
        Assert.Single(db.Cards.Where(c => c.Type == CardType.Info));
        Assert.Equal(SessionState.Working, db.Sessions.Single().State);
    }

    [Fact]
    public async Task OwnerAddsItemClaudeCannot()
    {
        var it = await f.Ops().AddItemAsync(new AddItemRequest("task", "Hello card", "wb", "~3 days", null, null), Actor.Owner);
        Assert.Equal("WB-1", it.Id);
        var child = await f.Ops().AddItemAsync(new AddItemRequest("task", "Child", "WB-1", null, null, null), Actor.Owner);
        Assert.Equal("WB-1.1", child.Id);
        await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().AddItemAsync(new AddItemRequest("task", "x", "wb", null, null, null), Actor.Claude));
    }

    [Fact]
    public async Task PlanCardApprovalOpensTheItemAndTellsTheSession()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Hello card", "wb", null, null, null), Actor.Owner);
        await f.Ops().HelloAsync("s1", null, "WB-1");
        var card = await f.Ops().SubmitPlanAsync("s1", "WB-1", [new("one", 30, null, "tests"), new("two", 20, null, "smoke")], [new("Name the machine?", ["headline", "body"], 0, null)], "goal");
        var r = await f.Ops().PressAsync(new PressRequest(card.Id, "approve"));
        Assert.True(r.Ok, r.Message);
        using var db = f.Db();
        var it = db.Items.Single();
        Assert.Equal(ItemState.Building, it.State);
        Assert.True(it.IsOpen);
        Assert.Equal(2, Json.ReadList<Step>(it.StepsJson).Count);
        var stop = await f.Ops().SessionStopAsync(Hook("Stop", "s1"));
        Assert.Equal("block", stop.Decision);
        Assert.Contains("headline", stop.Reason);
        var again = await f.Ops().SessionStopAsync(Hook("Stop", "s1"));
        Assert.Null(again.Decision);
    }

    [Fact]
    public async Task EightStepPlanIsRefused()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Hello card", "wb", null, null, null), Actor.Owner);
        await f.Ops().HelloAsync("s1", null, "WB-1");
        var steps = Enumerable.Range(1, 8).Select(i => new StepInput("s" + i, 10, null, null)).ToList();
        var ex = await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().SubmitPlanAsync("s1", "WB-1", steps, null, "g"));
        Assert.Equal("plan.cap", ex.Code);
    }

    [Fact]
    public async Task QuestionsAreCappedAndAnsweredThroughThePress()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Hello card", "wb", null, null, null), Actor.Owner);
        await f.Ops().HelloAsync("s1", null, "WB-1");
        var plan = await f.Ops().SubmitPlanAsync("s1", "WB-1", [new("one", 30, null, null), new("two", 20, null, null), new("three", 20, null, null)], null, "goal");
        await f.Ops().PressAsync(new PressRequest(plan.Id, "approve"));
        var q1 = await f.Ops().AskAsync("s1", "WB-1", 2, "Output after or live?", Options(), null, null, null, null);
        await f.Ops().AskAsync("s1", "WB-1", 3, "Second?", Options(), null, null, null, null);
        var ex = await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().AskAsync("s1", "WB-1", 3, "Third?", Options(), null, null, null, null));
        Assert.Equal("questions.cap", ex.Code);

        var r = await f.Ops().PressAsync(new PressRequest(q1.Id, "recommended"));
        Assert.True(r.Ok);
        var answer = await f.Ops().WaitAnswerAsync(q1.Id, TimeSpan.FromSeconds(1));
        Assert.Equal("Keep 40 lines", answer);
        using var db = f.Db();
        Assert.Equal(QuestionState.Answered, db.Questions.First(q => q.CardId == q1.Id).State);
        Assert.Single(db.Messages.Where(m => m.SessionId == "s1" && m.DeliveredAt == null && m.CardId == q1.Id));
    }

    [Fact]
    public async Task PacketApproveRunsGitThroughTheHostAndClosesTheItem()
    {
        await f.Ops().RegisterMachineAsync(new MachineRegistration("test-pc", "Windows", "1", "machine", "http://localhost:5291", [new CheckoutInfo(@"D:\repo", "main", null, false, null)], f.Clock.Now));
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Hello card", "wb", null, null, null), Actor.Owner);
        await f.Ops().HelloAsync("s1", @"D:\repo", "WB-1");
        var plan = await f.Ops().SubmitPlanAsync("s1", "WB-1", [new("one", 30, null, null)], null, "goal");
        await f.Ops().PressAsync(new PressRequest(plan.Id, "approve"));
        Assert.Single(f.Host.Ran);                                   // the branch was checked out
        await f.Ops().AssumeAsync("s1", "WB-1", "Times are in the owner zone.");
        var pk = await f.Ops().SubmitPacketAsync("s1", "WB-1", "Hello card lands", null, [new("A claim", null)], null, "done", null, 0, 0);
        var r = await f.Ops().PressAsync(new PressRequest(pk.Id, "all-fine"));
        Assert.True(r.Ok);
        r = await f.Ops().PressAsync(new PressRequest(pk.Id, "approve"));
        Assert.True(r.Ok, r.Message);
        Assert.Equal(2, f.Host.Ran.Count);                          // then the merge
        Assert.Contains(f.Host.Ran[1].Lines, l => l.StartsWith("git merge --no-ff wb/1-hello-card"));
        Assert.Contains(f.Host.Ran[1].Lines, l => l == "git branch -d wb/1-hello-card");
        using var db = f.Db();
        Assert.Equal(ItemState.Done, db.Items.Single().State);
        Assert.Null(db.Items.Single().Branch);
        Assert.Equal(CardState.Approved, db.Cards.Single(c => c.Id == pk.Id).State);
        Assert.All(db.Assumptions, a => Assert.Equal(AssumptionState.Fine, a.State));
    }

    [Fact]
    public async Task PacketApproveFailsSoftlyWhenTheHostCannot()
    {
        await f.Ops().RegisterMachineAsync(new MachineRegistration("test-pc", "Windows", "1", "machine", "http://localhost:5291", [new CheckoutInfo(@"D:\repo", "main", null, false, null)], f.Clock.Now));
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Hello card", "wb", null, null, null), Actor.Owner);
        await f.Ops().HelloAsync("s1", @"D:\repo", "WB-1");
        var plan = await f.Ops().SubmitPlanAsync("s1", "WB-1", [new("one", 30, null, null)], null, "goal");
        await f.Ops().PressAsync(new PressRequest(plan.Id, "approve"));
        var pk = await f.Ops().SubmitPacketAsync("s1", "WB-1", "Hello card lands", null, null, null, null, null, 0, 0);
        f.Host.Ok = false;
        var r = await f.Ops().PressAsync(new PressRequest(pk.Id, "approve"));
        Assert.False(r.Ok);
        using var db = f.Db();
        Assert.Equal(CardState.Open, db.Cards.Single(c => c.Id == pk.Id).State);
        Assert.NotEqual(ItemState.Done, db.Items.Single().State);
    }

    [Fact]
    public async Task FoundItemsAreCappedAtFive()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Hello card", "wb", null, null, null), Actor.Owner);
        await f.Ops().HelloAsync("s1", null, "WB-1");
        for (var i = 0; i < 5; i++) await f.Ops().FoundAsync("s1", "WB-1", "BUG", "f" + i, "why", null, null);
        var ex = await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().FoundAsync("s1", "WB-1", "BUG", "f6", "why", null, null));
        Assert.Equal("found.cap", ex.Code);
    }

    [Fact]
    public async Task GitToMainIsDeniedBeforeTheTool()
    {
        await f.Ops().SessionStartedAsync(Hook("SessionStart", "s1"));
        var e = Hook("PreToolUse", "s1");
        e.ToolName = "Bash";
        e.ToolInput = System.Text.Json.JsonDocument.Parse("{\"command\":\"git push origin main\"}").RootElement;
        var reply = await f.Ops().SessionPreToolAsync(e);
        Assert.Equal("deny", reply.HookSpecificOutput!.PermissionDecision);
        e.ToolInput = System.Text.Json.JsonDocument.Parse("{\"command\":\"git commit -m x\"}").RootElement;
        Assert.Null((await f.Ops().SessionPreToolAsync(e)).HookSpecificOutput);
    }

    [Fact]
    public async Task ProposedRulingsAreRecordedOnlyWhenTicked()
    {
        await f.Ops().HelloAsync("s1", null, null);
        var card = await f.Ops().ProposeRulingsAsync("s1", "Inbox deferral", ["Cards defer for 24 h.", "Info cards never notify."]);
        using (var db = f.Db()) { var second = db.Proposals.OrderBy(p => p.Id).Last(); await f.Ops().PressAsync(new PressRequest(card.Id, "tick", "false", null, second.Id)); }
        var r = await f.Ops().PressAsync(new PressRequest(card.Id, "record"));
        Assert.True(r.Ok);
        using var db2 = f.Db();
        Assert.Single(db2.Rulings);
        Assert.Equal("Cards defer for 24 h.", db2.Rulings.Single().Text);
    }

    [Fact]
    public async Task MinutesAreMeasuredFromPromptToStop()
    {
        await f.Ops().SessionStartedAsync(Hook("SessionStart", "s1"));
        await f.Ops().SessionPromptAsync(Hook("UserPromptSubmit", "s1", prompt: "go"));
        f.Clock.Now = f.Clock.Now.AddMinutes(7);
        await f.Ops().SessionStopAsync(Hook("Stop", "s1"));
        using var db = f.Db();
        Assert.Equal(7, db.Sessions.Single().Minutes);
    }
}
