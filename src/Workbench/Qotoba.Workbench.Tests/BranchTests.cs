using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Store;

namespace Qotoba.Workbench.Tests;

/// <summary>
/// A session finds its item from the checkout it runs in; nobody types an id. The branch belongs to the opened top item,
/// Claude keeps that convention itself, and the Workbench only records the name.
/// </summary>
public class BranchTests : IDisposable
{
    private readonly OpsFixture f = new();
    public void Dispose() => f.Dispose();

    private static HookEvent Start(string session, string cwd) => new() { HookEventName = "SessionStart", SessionId = session, Cwd = cwd, Source = "startup" };

    private async Task RegisterCheckoutAsync(string branch = "main", string? item = null) =>
        await f.Ops().RegisterMachineAsync(new MachineRegistration("test-pc", "Windows", "1", "machine", "http://localhost:5291", [new CheckoutInfo(@"D:\repo", branch, item, false, null)], f.Clock.Now));

    /// <summary>Login screen with two children, the first open.</summary>
    private async Task<(Item top, Item form, Item remember)> LoginScreenAsync()
    {
        var top = await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        var form = await f.Ops().AddItemAsync(new AddItemRequest("task", "Form and validation", top.Id, null, null, null), Actor.Owner);
        var remember = await f.Ops().AddItemAsync(new AddItemRequest("task", "Remember me", top.Id, null, null, null), Actor.Owner);
        await f.Ops().PatchItemAsync(top.Id, new ItemPatch(null, null, true));
        await f.Ops().PatchItemAsync(form.Id, new ItemPatch(null, null, true));
        return (top, form, remember);
    }

    [Fact]
    public async Task SessionAttachesToTheOpenNodeOfItsCheckout()
    {
        await RegisterCheckoutAsync();
        var (top, form, _) = await LoginScreenAsync();
        var reply = await f.Ops().SessionStartedAsync(Start("s1", @"D:\repo\src\App"));
        using var db = f.Db();
        Assert.Equal(form.Id, db.Sessions.Single().ItemId);
        Assert.Contains("Form and validation", reply.HookSpecificOutput!.AdditionalContext);
        Assert.Contains($"Under {top.Id}", reply.HookSpecificOutput!.AdditionalContext);
        Assert.Contains("wb/1-login-screen", reply.HookSpecificOutput!.AdditionalContext);
        Assert.Contains("branch_note", reply.HookSpecificOutput!.AdditionalContext);
    }

    [Fact]
    public async Task NothingOpenMeansNoItem()
    {
        await RegisterCheckoutAsync();
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        var reply = await f.Ops().SessionStartedAsync(Start("s1", @"D:\repo"));
        using var db = f.Db();
        Assert.Null(db.Sessions.Single().ItemId);
        Assert.Contains("items_write", reply.HookSpecificOutput!.AdditionalContext);
    }

    [Fact]
    public async Task ABranchNoteRecordsTheTopItemsBranchAndBindsTheCheckout()
    {
        await RegisterCheckoutAsync();
        var (top, form, _) = await LoginScreenAsync();
        await f.Ops().SessionStartedAsync(Start("s1", @"D:\repo"));
        await f.Ops().HelloAsync("s1", @"D:\repo", null);
        var msg = await f.Ops().BranchNoteAsync("s1", form.Id, "wb/1-login-screen", null);
        Assert.Equal($"{top.Id} is on wb/1-login-screen", msg);
        using var db = f.Db();
        var t = db.Items.Single(i => i.Id == top.Id);
        Assert.Equal("wb/1-login-screen", t.Branch);
        Assert.Equal(@"D:\repo", t.Checkout);
        Assert.Null(db.Items.Single(i => i.Id == form.Id).Branch);
        var bound = Json.ReadList<Checkout>(db.Machines.Single().CheckoutsJson).Single();
        Assert.Equal(top.Id, bound.ItemId);
        Assert.Equal("wb/1-login-screen", bound.Branch);
    }

    [Fact]
    public async Task TheBranchTellsALaterSessionWhichTopItemItIsOn()
    {
        await RegisterCheckoutAsync();
        var (top, form, _) = await LoginScreenAsync();
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Rendering", "wb", null, null, null), Actor.Owner);
        await f.Ops().SessionStartedAsync(Start("s1", @"D:\repo"));
        await f.Ops().HelloAsync("s1", @"D:\repo", null);
        await f.Ops().BranchNoteAsync("s1", form.Id, "wb/1-login-screen", null);
        // the Host re-registers the checkout on the branch with no item named: the binding survives
        await RegisterCheckoutAsync("wb/1-login-screen");
        f.Host.Branch = "wb/1-login-screen";
        var s2 = await f.Ops().SessionStartedAsync(Start("s2", @"D:\repo\src"));
        Assert.Contains("Branch: wb/1-login-screen", s2.HookSpecificOutput!.AdditionalContext);
        using var db = f.Db();
        Assert.Equal(form.Id, db.Sessions.Single(s => s.Id == "s2").ItemId);
    }

    [Fact]
    public async Task OpenInANewWindowMakesAWorktreeOnTheItemsBranchAndOpensTheNode()
    {
        await RegisterCheckoutAsync();
        var top = await f.Ops().AddItemAsync(new AddItemRequest("task", "Rendering engine", "wb", null, null, null), Actor.Owner);
        var (opened, folder, branch, msg) = await f.Ops().OpenInWindowAsync(top.Id);
        Assert.Equal(top.Id, opened!.Id);
        Assert.True(opened.IsOpen);
        Assert.Equal("wb/1-rendering-engine", branch);
        Assert.Equal("D:" + Path.DirectorySeparatorChar + "repo-1-rendering-engine", folder);
        Assert.Contains("new folder", msg);
        var req = Assert.Single(f.Host.Worktrees);
        Assert.Equal("D:" + Path.DirectorySeparatorChar + "repo", req.Checkout);
        using var db = f.Db();
        var t = db.Items.Single(i => i.Id == top.Id);
        Assert.Equal(folder, t.Checkout); Assert.Equal(branch, t.Branch);
        var cos = Json.ReadList<Checkout>(db.Machines.Single().CheckoutsJson);
        Assert.Contains(cos, c => c.Path == folder && c.ItemId == top.Id && c.Worktree);
        // a session started in that folder belongs to the item
        var s = await f.Ops().SessionStartedAsync(Start("s9", folder + Path.DirectorySeparatorChar + "src"));
        Assert.Contains("Rendering engine", s.HookSpecificOutput!.AdditionalContext);
    }

    [Fact]
    public async Task AnOpenProjectWithNoItemIsWhatASessionBelongsTo()
    {
        await RegisterCheckoutAsync();
        // the seed leaves only Workbench open; the owner opens Language to work on it
        using (var db = f.Db()) { foreach (var p in db.Projects) p.State = ProjectState.Coarse; db.SaveChanges(); }
        await f.Ops().OpenProjectAsync("LNG");
        var reply = await f.Ops().SessionStartedAsync(Start("s1", @"D:\repo"));
        Assert.Contains("Project Language (LNG) · open, no item yet", reply.HookSpecificOutput!.AdditionalContext);
        await f.Ops().HelloAsync("s1", @"D:\repo", null);
        var made = await f.Ops().AddItemsAsync("s1", null, [new("Parser", null, null)]);
        Assert.Equal("LNG-1", made[0].Id);
        var r = await f.Ops().RaiseReportAsync("s1", "FTR", "A printer that keeps comments", null, null, "chat", null, null, null);
        Assert.Equal("lang", r.ProjectId); Assert.Null(r.AttachItemId);
        using var db2 = f.Db();
        Assert.Equal("lang", db2.Sessions.Single().ProjectId);
    }

    [Fact]
    public async Task AProjectOpenedAsAWholeHoldsTheBranchEverythingUnderItShares()
    {
        await RegisterCheckoutAsync();
        await f.Ops().OpenProjectAsync("WB");
        var top = await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        var s1 = await f.Ops().SessionStartedAsync(Start("s1", @"D:\repo"));
        Assert.Contains("Project Workbench (WB) · open, no item yet", s1.HookSpecificOutput!.AdditionalContext);
        Assert.Contains("`wb/workbench`", s1.HookSpecificOutput!.AdditionalContext);
        await f.Ops().HelloAsync("s1", @"D:\repo", null);
        var (_, msg) = await f.Ops().OpenNodeFromChatAsync("s1", top.Id);
        Assert.Contains("wb/workbench (project WB, open as a whole)", msg);
        Assert.Equal("project WB is on wb/workbench", await f.Ops().BranchNoteAsync("s1", top.Id, "wb/workbench", null));
        using (var db = f.Db())
        {
            Assert.Equal("wb/workbench", db.Projects.Single(p => p.Id == "wb").Branch);
            Assert.Null(db.Items.Single(i => i.Id == top.Id).Branch);
            Assert.Equal("wb", Json.ReadList<Checkout>(db.Machines.Single().CheckoutsJson).Single().ItemId);
        }
        // a later session on that branch lands on the open item under the project, with the project's branch
        f.Host.Branch = "wb/workbench";
        var s2 = await f.Ops().SessionStartedAsync(Start("s2", @"D:\repo\src"));
        Assert.Contains($"## Item {top.Id}", s2.HookSpecificOutput!.AdditionalContext);
        Assert.Contains("Branch: wb/workbench in D:" + Path.DirectorySeparatorChar + "repo, shared by everything under project WB", s2.HookSpecificOutput!.AdditionalContext);
        // the owner says the whole is done: the project is no longer open and the branch is off the record
        var done = await f.Ops().ItemDoneAsync("s1", "WB", "The Workbench is reviewed as a whole", null);
        Assert.Contains("no longer open as a whole", done);
        using var db2 = f.Db();
        var wb = db2.Projects.Single(p => p.Id == "wb");
        Assert.False(wb.IsOpen); Assert.Null(wb.Branch);
        Assert.Null(Json.ReadList<Checkout>(db2.Machines.Single().CheckoutsJson).Single().ItemId);
    }

    [Fact]
    public async Task AProjectOpensInANewWindowOnItsOwnBranch()
    {
        await RegisterCheckoutAsync();
        var (opened, folder, branch, msg) = await f.Ops().OpenInWindowAsync("wb");
        Assert.Null(opened);
        Assert.Equal("wb/workbench", branch);
        Assert.Equal("D:" + Path.DirectorySeparatorChar + "repo-workbench", folder);
        Assert.Contains("Workbench opened as a whole", msg);
        using (var db = f.Db())
        {
            var wb = db.Projects.Single(p => p.Id == "wb");
            Assert.True(wb.IsOpen); Assert.Equal(branch, wb.Branch); Assert.Equal(folder, wb.Checkout);
            Assert.Contains(Json.ReadList<Checkout>(db.Machines.Single().CheckoutsJson), c => c.Path == folder && c.ItemId == "wb" && c.Worktree);
        }
        var s = await f.Ops().SessionStartedAsync(Start("s9", folder + Path.DirectorySeparatorChar + "src"));
        Assert.Contains("Project Workbench (WB) · open, no item yet", s.HookSpecificOutput!.AdditionalContext);
        Assert.Contains("Branch: wb/workbench in " + folder, s.HookSpecificOutput!.AdditionalContext);
        // the owner's word in chat opens it too, and the brief names the branch
        await f.Ops().HelloAsync("s9", folder, null);
        Assert.Contains("is already open as a whole · on wb/workbench", await f.Ops().OpenProjectFromChatAsync("s9", "WB"));
    }

    [Fact]
    public void BranchNamesAreProjectCodeNumberAndSlug()
    {
        var p = new Project { Id = "lng", Code = "LNG", Name = "Language" };
        Assert.Equal("lng/3-parser-and-printer", Store.Ops.WorkbenchOps.BranchNameFor(p, new Item { Id = "LNG-3", ProjectId = "lng", Title = "Parser and printer" }));
        Assert.Equal("lng/3", Store.Ops.WorkbenchOps.BranchNameFor(p, new Item { Id = "LNG-3", ProjectId = "lng", Title = "!!!" }));
        Assert.Equal("lng/language", Store.Ops.WorkbenchOps.BranchNameFor(p));
    }
}
