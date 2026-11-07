using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Tests;

/// <summary>Reports are information; their When implementing lines move only through a task's plan, cards and runs.</summary>
public class ReportTests : IDisposable
{
    private readonly OpsFixture f = new();
    public void Dispose() => f.Dispose();

    private static Dictionary<string, JsonElement> Sections(string json) => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private static List<ImplLineInput> Lines() =>
    [
        new("decision", "Which fix", null, null, null, [new("Fix 2: attach skipped tokens as trivia", true, "Low risk, regression check exists", "low", null), new("Fix 1: stop recovery at the comment", false, "Leaves a stray quote", "medium", null)]),
        new("decision", "May recovery discard tokens?", null, null, null, [new("Never discard", true, "Every file prints back", "high, every recovery path", null), new("Discard and record a finding", false, "Simpler", "high", null)]),
        new("proof", "The specimen with a trailing comment round-trips", null, null, null, null),
        new("dependency", "Depends on the round-trip check", "fpb:FTR-1", "It proves the fix", null, null)
    ];

    private async Task<(Report bug, Item task)> BugTakenOnAsync()
    {
        var ops = f.Ops();
        await ops.AddReportAsync(new AddItemRequest("FTR", "Round-trip check for every file", "wb", null, null, null), Actor.Owner);
        var bug = await ops.AddReportAsync(new AddItemRequest("BUG", "Recovery drops the trailing comment", "wb", null, null, null, null, "wrong result"), Actor.Owner);
        await ops.HelloAsync("s1", null, null);
        await ops.WriteReportAsync("s1", bug.Id, Sections("{\"symptom\":{\"text\":\"The comment after an unterminated string vanishes.\",\"expected\":\"It stays as trivia.\"}}"), Lines());
        var task = await f.Ops().AddItemAsync(new AddItemRequest("task", "Keep comments when recovery skips a line", "wb", null, [bug.Id], null), Actor.Owner);
        return (bug, task);
    }

    [Fact]
    public async Task OwnerCreatesReportClaudeWritesSectionsAndLines()
    {
        var (bug, task) = await BugTakenOnAsync();
        using var db = f.Db();
        var r = db.Reports.Single(x => x.Id == bug.Id);
        Assert.Equal("BUG-1", r.Id);
        Assert.Contains("\"author\":\"claude\"", r.SectionsJson);
        Assert.Equal(4, db.ImplLines.Count(l => l.ReportId == bug.Id));
        Assert.All(db.ImplLines, l => Assert.Equal(ImplStatus.Open, l.Status));
        Assert.Contains("fpb:BUG-1", db.Items.Single(i => i.Id == task.Id).ImplementsJson);
        await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().AddReportAsync(new AddItemRequest("BUG", "x", "wb", null, null, null), Actor.Claude));
    }

    [Fact]
    public async Task ReportNeverGetsACardOfItsOwn()
    {
        var (bug, _) = await BugTakenOnAsync();
        using var db = f.Db();
        Assert.DoesNotContain(db.Cards, c => c.ReportId == bug.Id && c.Type != CardType.Info);
    }

    [Fact]
    public async Task PlanDrawsLowCostDecisionsAndDependenciesIntoTheSettlePassAndApprovalSettlesThem()
    {
        var (bug, task) = await BugTakenOnAsync();
        await f.Ops().HelloAsync("s2", null, task.Id);
        var plan = await f.Ops().SubmitPlanAsync("s2", task.Id, [new("Attach the trivia", 40, null, "tests"), new("Round-trip specimen", 20, null, "round-trip")], null, "The file prints back with its comment.", null);
        var pl = Json.Read(plan.PayloadJson, new PlanPayload());
        Assert.Equal(2, pl.Settle.Count);                         // the low-cost decision and the dependency; the high-cost one waits for a card
        Assert.All(pl.Settle, q => Assert.Equal("fpb:" + bug.Id, q.From));
        using (var db = f.Db())
        {
            var lines = db.ImplLines.Where(l => l.ReportId == bug.Id).OrderBy(l => l.Order).ToList();
            Assert.Equal(ImplStatus.Scheduled, lines[0].Status);
            Assert.Contains("settle pass", lines[0].Note);
            Assert.Equal(ImplStatus.Scheduled, lines[1].Status);
            Assert.Contains("Question Card", lines[1].Note);
            Assert.Equal(ImplStatus.Scheduled, lines[2].Status);
            Assert.Contains("proved by a run", lines[2].Note);
        }
        var r = await f.Ops().PressAsync(new PressRequest(plan.Id, "approve"));
        Assert.True(r.Ok, r.Message);
        using (var db = f.Db())
        {
            var lines = db.ImplLines.Where(l => l.ReportId == bug.Id).OrderBy(l => l.Order).ToList();
            Assert.Equal(ImplStatus.Settled, lines[0].Status);
            Assert.Equal("card:" + plan.Id, lines[0].Ref);
            Assert.Equal(ImplStatus.Settled, lines[3].Status);
            Assert.Single(db.Edges.Where(e => e.From == "item:" + task.Id && e.To == "fpb:FTR-1" && e.Confirmed));
            Assert.Equal(2, Json.ReadList<Decision>(db.Items.Single(i => i.Id == task.Id).DecisionsJson).Count);
            Assert.Equal("fixing", db.Reports.Single(x => x.Id == bug.Id).State);
        }
    }

    [Fact]
    public async Task HighCostLineIsAskedOnACardAndSettledByTheAnswer()
    {
        var (bug, task) = await BugTakenOnAsync();
        await f.Ops().HelloAsync("s2", null, task.Id);
        var plan = await f.Ops().SubmitPlanAsync("s2", task.Id, [new("Attach the trivia", 40, null, "tests"), new("Round-trip specimen", 20, null, "round-trip")], null, "goal", null);
        await f.Ops().PressAsync(new PressRequest(plan.Id, "approve"));
        string lineId;
        using (var db = f.Db()) lineId = db.ImplLines.Single(l => l.ReportId == bug.Id && l.Text.StartsWith("May recovery")).Id;
        var card = await f.Ops().AskAsync("s2", task.Id, 1, "May recovery discard the tokens it skips?", [new("Never discard", true, "Every file prints back", "high", null), new("Discard", false, "Simpler", "high", null)], null, null, null, null, default, lineId);
        using (var db = f.Db())
        {
            var l = db.ImplLines.Single(x => x.Id == lineId);
            Assert.Equal(ImplStatus.Asked, l.Status);
            Assert.Equal("card:" + card.Id, l.Ref);
            Assert.Equal(bug.Id, db.Cards.Single(c => c.Id == card.Id).ReportId);
        }
        await f.Ops().PressAsync(new PressRequest(card.Id, "recommended"));
        using (var db = f.Db())
        {
            var l = db.ImplLines.Single(x => x.Id == lineId);
            Assert.Equal(ImplStatus.Settled, l.Status);
            Assert.Contains("Never discard", l.Note);
        }
    }

    [Fact]
    public async Task ProofLineIsProvedOnlyByAPassingRun()
    {
        var (bug, task) = await BugTakenOnAsync();
        await f.Ops().HelloAsync("s2", null, task.Id);
        string lineId;
        using (var db = f.Db()) lineId = db.ImplLines.Single(l => l.ReportId == bug.Id && l.Kind == ImplKind.Proof).Id;
        var fail = await f.Ops().RecordRunAsync(new RunReport("round-trip", task.Id, "s2", "test-pc", "fail", 0, 1, 1, "qwb check", "x", 1, 1, null, null, 2));
        await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().ProveLineAsync("s2", lineId, fail.Id));
        var pass = await f.Ops().RecordRunAsync(new RunReport("round-trip", task.Id, "s2", "test-pc", "pass", 1, 1, 1, "qwb check", "ok", 1, 0, null, null, 2));
        await f.Ops().ProveLineAsync("s2", lineId, pass.Id);
        using var db2 = f.Db();
        Assert.Equal(ImplStatus.Proved, db2.ImplLines.Single(x => x.Id == lineId).Status);
        Assert.Equal("run:" + pass.Id, db2.ImplLines.Single(x => x.Id == lineId).Ref);
    }

    [Fact]
    public async Task OwnerSectionsAreNotOverwrittenAndFixesAreCapped()
    {
        var ops = f.Ops();
        var bug = await ops.AddReportAsync(new AddItemRequest("BUG", "A bug", "wb", null, null, null), Actor.Owner);
        await ops.PatchReportAsync(bug.Id, new ReportPatch(null, "serious", null, null, null, null));
        await ops.HelloAsync("s1", null, null);
        var four = "{\"fixes\":{\"rows\":[{\"t\":\"a\"},{\"t\":\"b\"},{\"t\":\"c\"},{\"t\":\"d\"}]}}";
        var ex = await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().WriteReportAsync("s1", bug.Id, Sections(four), null));
        Assert.Equal("report.fixes", ex.Code);
        using var db = f.Db();
        var r = db.Reports.Single(x => x.Id == bug.Id);
        Assert.Equal(SeverityLevel.Serious, r.Severity);
        Assert.Equal(Author.Me, r.SeverityBy);
    }

    [Fact]
    public async Task FoundItemPromotesToAReportWithItsEvidence()
    {
        var ops = f.Ops();
        await ops.AddItemAsync(new AddItemRequest("task", "Hello card", "wb", null, null, null), Actor.Owner);
        await ops.HelloAsync("s1", null, "WB-1");
        var found = await ops.FoundAsync("s1", "WB-1", "PER", "Tree query takes 400 ms", "Opening an item reloads the whole tree.", null, null);
        var per = await f.Ops().AddReportAsync(new AddItemRequest("PER", found.Title, "WB-1", null, null, null, found.Id), Actor.Owner);
        using var db = f.Db();
        Assert.Equal("promoted", db.Reports.Single(x => x.Id == per.Id).SourceType);
        Assert.Equal(FoundState.Promoted, db.Founds.Single().State);
        Assert.Equal("fpb:" + per.Id, db.Founds.Single().PromotedTo);
        Assert.Contains("headline", db.Reports.Single(x => x.Id == per.Id).SectionsJson);
    }

    [Fact]
    public async Task DependencyProposalWaitsForTheSettlePass()
    {
        var ops = f.Ops();
        await ops.AddItemAsync(new AddItemRequest("task", "A", "wb", null, null, null), Actor.Owner);
        await ops.AddItemAsync(new AddItemRequest("task", "B", "wb", null, null, null), Actor.Owner);
        await ops.HelloAsync("s1", null, "WB-2");
        var e = await ops.ProposeEdgeAsync("s1", "WB-2", "WB-1", "needs", "B reads the table A creates", true);
        Assert.False(e.Confirmed);
        var plan = await f.Ops().SubmitPlanAsync("s1", "WB-2", [new("one", 30, null, null)], null, "g", null);
        var pl = Json.Read(plan.PayloadJson, new PlanPayload());
        Assert.Single(pl.Settle, q => q.Edge == e.Id);
        await f.Ops().PressAsync(new PressRequest(plan.Id, "approve"));
        using var db = f.Db();
        Assert.True(db.Edges.Single().Confirmed);
        var path = Store.Ops.WorkbenchOps.CriticalPath(db.Items.ToList(), db.Edges.ToList(), "wb");
        Assert.Equal(["item:WB-1", "item:WB-2"], path);
    }

    [Fact]
    public async Task HostIndexCreatesFilesCommentsAndHandlesThemByProofLines()
    {
        var ops = f.Ops();
        await ops.AddItemAsync(new AddItemRequest("task", "Runs table", "wb", null, null, null), Actor.Owner);
        var file = new IndexedFile(@"src\Runs\RunRecord.cs", "h1", 3, ["// a", "//+ keep this immutable", "class RunRecord {}"], ["+//+ keep this immutable"],
            [new IndexedComment(2, "keep this immutable", false, null, "RunRecord", "class", "c1")]);
        await ops.IndexAsync(new CommentIndexPost("test-pc", @"D:\repo", "wb/1-runs", "WB-1", [file], [new IndexedType("RunRecord", "sealed record", "Qotoba.Runs", @"src\Runs\RunRecord.cs", 3, 3, "A run")],
            new BranchInfo("wb/1-runs", 2, 0, false, false, null), [new CommitInfo("abc1234", "WB-1 runs table", DateTimeOffset.UtcNow, [@"src/Runs/RunRecord.cs"], 10, 1)]));
        using (var db = f.Db())
        {
            var fs = db.Files.Single();
            Assert.Equal("runrecord", fs.Id);
            Assert.False(fs.Seen);
            Assert.Equal(CommentState.Open, db.Comments.Single().State);
            Assert.Equal("WB-1", db.Comments.Single().ItemId);
            Assert.Single(db.Branches);
            Assert.Single(db.Commits);
            Assert.Single(db.Types);
        }
        await f.Ops().MarkSeenAsync("runrecord", null);
        var handled = file with { Hash = "h2", Comments = [.. file.Comments, new IndexedComment(3, "done: rows never update", true, "s1", "RunRecord", "class", "p1")] };
        await f.Ops().IndexAsync(new CommentIndexPost("test-pc", @"D:\repo", "wb/1-runs", "WB-1", [handled], [], new BranchInfo("wb/1-runs", 3, 0, true, false, null), [new CommitInfo("def5678", "WB-1 handled", DateTimeOffset.UtcNow, [@"src/Runs/RunRecord.cs"], 1, 0)]));
        using var db2 = f.Db();
        var c = db2.Comments.Single();
        Assert.Equal(CommentState.Handled, c.State);
        Assert.Contains("def5678", c.HandledJson);
        Assert.False(db2.Files.Single().Seen);     // the file changed after it was seen
        Assert.Equal("pushed", db2.Branches.Single().State);
    }
}
