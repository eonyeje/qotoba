using System.Text.Json;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Store;

namespace Qotoba.Workbench.Tests;

/// <summary>FTR, BUG and PER: always raised by Claude, attached to an item or the project, moved on by Claude, pruned by the owner in chat.</summary>
public class ReportTests : IDisposable
{
    private readonly OpsFixture f = new();
    public void Dispose() => f.Dispose();

    private static Dictionary<string, JsonElement> Sections(string json) => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private async Task LoginAsync()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Form", "WB-1", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1.1");
    }

    [Fact]
    public async Task AnFtrFromChatGetsTheOwnersIdSchemeAndAttachesToTheItem()
    {
        await LoginAsync();
        var r = await f.Ops().RaiseReportAsync("s1", "FTR", "A custom style for the login button", null, "You want the button to take the app's accent and a rounded shape.", "chat", null, null, null);
        Assert.Equal("WB-FTR-001", r.Id);
        Assert.Equal("Open", r.State);
        Assert.Equal("WB-1.1", r.AttachItemId);
        Assert.Equal("chat", r.SourceType);
        var second = await f.Ops().RaiseReportAsync("s1", "FTR", "Remember the last user name", "wb", null, "chat", null, null, null);
        Assert.Equal("WB-FTR-002", second.Id);
        Assert.Null(second.AttachItemId);
        using var db = f.Db();
        Assert.Contains("[[fpb:WB-FTR-001]]", db.Items.Single(i => i.Id == "WB-1.1").HistoryJson);
        Assert.Empty(db.Cards);
    }

    [Fact]
    public async Task ABugReadFromCodeStartsAsVerifyFirstAndADuplicateIsRefused()
    {
        await LoginAsync();
        var bug = await f.Ops().RaiseReportAsync("s1", "BUG", "The submit button stays disabled after a failed attempt, so a second try needs a reload", null, "Seen while wiring the form.", "read", Sections("""{"symptom":"Button disabled after failure","cause":{"text":"IsBusy never resets on the error path"}}"""), null, "serious");
        Assert.Equal("WB-BUG-001", bug.Id);
        Assert.Equal("Verify first", bug.State);
        Assert.Equal(SeverityLevel.Serious, bug.Severity);
        var ex = await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().RaiseReportAsync("s1", "BUG", "the submit button stays disabled after a failed attempt, so a second try needs a reload", null, null, null, null, null, null));
        Assert.Equal("report.dup", ex.Code);
        Assert.Contains("WB-BUG-001", ex.Message);
    }

    [Fact]
    public async Task UpdateMovesTheStateRecordsWhatIsLeftAndWhoTookItOn()
    {
        await LoginAsync();
        var bug = await f.Ops().RaiseReportAsync("s1", "BUG", "A six-digit colour reads past the end", null, null, "check", null, null, null);
        await f.Ops().UpdateReportAsync("s1", bug.Id, "in progress", "the parser; the widget after it", "WB-1.1", null, null, null, null);
        using (var db = f.Db())
        {
            var r = db.Reports.Single();
            Assert.Equal("In progress", r.State);
            Assert.Equal("the parser; the widget after it", r.Left);
            Assert.Equal("WB-1.1", r.TakenOnItemId);
        }
        await f.Ops().UpdateReportAsync("s1", bug.Id, "Won't do", "the owner: by design, not a defect", null, null, null, null, null);
        using var db2 = f.Db();
        Assert.Equal("Won't do", db2.Reports.Single().State);
        var ex = await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().UpdateReportAsync("s1", bug.Id, "sort of done", null, null, null, null, null, null));
        Assert.Equal("report.state", ex.Code);
    }

    [Fact]
    public async Task LinksAreTypedInTheRegistersWords()
    {
        await LoginAsync();
        var a = await f.Ops().RaiseReportAsync("s1", "FTR", "Universal values", null, null, null, null, null, null);
        var b = await f.Ops().RaiseReportAsync("s1", "FTR", "Values without instances", null, null, null, null, [new(a.Id, "split from", "section 8")], null);
        await f.Ops().LinkAsync("s1", "WB-1.1", b.Id, "needs", "the form binds to values");
        var bad = await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().LinkAsync("s1", "WB-1.1", a.Id, "sort of", null));
        Assert.Equal("edge.type", bad.Code);
        var text = await f.Ops().ReportTextAsync(b.Id);
        Assert.Contains($"fpb:{b.Id} split from fpb:{a.Id}", text);
        Assert.Contains($"item:WB-1.1 needs fpb:{b.Id}", text);
        var list = await f.Ops().ReportListAsync("WB-1", "open");
        Assert.Contains(a.Id, list); Assert.Contains(b.Id, list);
    }

    [Fact]
    public async Task TheBriefNamesTheOpenFpbsOnTheItem()
    {
        await LoginAsync();
        await f.Ops().RaiseReportAsync("s1", "BUG", "Validation runs twice on blur", null, null, "check", null, null, null);
        await f.Ops().RaiseReportAsync("s1", "PER", "Every keystroke relays out the whole form", null, null, "check", null, null, null);
        var brief = await f.Ops().BriefAsync("s1");
        Assert.Contains("WB-BUG-001 · Open · Validation runs twice", brief);
        Assert.Contains("WB-PER-001", brief);
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
