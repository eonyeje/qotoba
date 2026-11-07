using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Store;

namespace Qotoba.Workbench.Tests;

/// <summary>
/// The branch belongs to the opened top item; its children share it; a session finds its item from the checkout.
/// The owner opens "Login screen" and the items under it, and never names anything to a session.
/// </summary>
public class BranchTests : IDisposable
{
    private readonly OpsFixture f = new();
    public void Dispose() => f.Dispose();

    private static HookEvent Start(string session, string cwd) => new() { HookEventName = "SessionStart", SessionId = session, Cwd = cwd, Source = "startup" };

    private async Task RegisterCheckoutAsync() =>
        await f.Ops().RegisterMachineAsync(new MachineRegistration("test-pc", "Windows", "1", "machine", "http://localhost:5291", [new CheckoutInfo(@"D:\repo", "main", null, false, null)], f.Clock.Now));

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
        Assert.Contains("Branch: none yet", reply.HookSpecificOutput!.AdditionalContext);
    }

    [Fact]
    public async Task NothingOpenMeansNoItem()
    {
        await RegisterCheckoutAsync();
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Login screen", "wb", null, null, null), Actor.Owner);
        await f.Ops().SessionStartedAsync(Start("s1", @"D:\repo"));
        using var db = f.Db();
        Assert.Null(db.Sessions.Single().ItemId);
    }

    [Fact]
    public async Task PlanApprovalChecksOutTheTopItemsBranch()
    {
        await RegisterCheckoutAsync();
        var (top, form, _) = await LoginScreenAsync();
        await f.Ops().SessionStartedAsync(Start("s1", @"D:\repo"));
        await f.Ops().HelloAsync("s1", @"D:\repo", null);
        var plan =await f.Ops().SubmitPlanAsync("s1", form.Id, [new("Draw the form", 30, null, "tests")], null, "The form validates");
        var pl = Json.Read(plan.PayloadJson, new PlanPayload());
        Assert.Equal("wb/1-login-screen", pl.Branch);
        var r = await f.Ops().PressAsync(new PressRequest(plan.Id, "approve"));
        Assert.True(r.Ok, r.Message);
        Assert.Contains("wb/1-login-screen", r.Message);
        var git = Assert.Single(f.Host.Ran);
        Assert.Equal(@"D:\repo", git.Checkout);
        Assert.Equal("git checkout wb/1-login-screen", git.Lines[0]);
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
    public async Task TheBindingSurvivesTheHostsNextHeartbeat()
    {
        await RegisterCheckoutAsync();
        var (top, form, _) = await LoginScreenAsync();
        await f.Ops().SessionStartedAsync(Start("s1", @"D:\repo"));
        await f.Ops().HelloAsync("s1", @"D:\repo", null);
        var plan =await f.Ops().SubmitPlanAsync("s1", form.Id, [new("Draw the form", 30, null, null)], null, "goal");
        await f.Ops().PressAsync(new PressRequest(plan.Id, "approve"));
        await f.Ops().RegisterMachineAsync(new MachineRegistration("test-pc", "Windows", "1", "machine", "http://localhost:5291", [new CheckoutInfo(@"D:\repo", "wb/1-login-screen", null, false, null)], f.Clock.Now));
        using var db = f.Db();
        Assert.Equal(top.Id, Json.ReadList<Checkout>(db.Machines.Single().CheckoutsJson).Single().ItemId);
        var s2 = await f.Ops().SessionStartedAsync(Start("s2", @"D:\repo\src"));
        Assert.Contains("Branch: wb/1-login-screen", s2.HookSpecificOutput!.AdditionalContext);
    }

    [Fact]
    public async Task ChildrenShareTheBranchAndTheLastPacketDeletesIt()
    {
        await RegisterCheckoutAsync();
        var (top, form, remember) = await LoginScreenAsync();
        await f.Ops().SessionStartedAsync(Start("s1", @"D:\repo"));
        await f.Ops().HelloAsync("s1", @"D:\repo", null);
        var plan =await f.Ops().SubmitPlanAsync("s1", form.Id, [new("Draw the form", 30, null, null)], null, "goal");
        await f.Ops().PressAsync(new PressRequest(plan.Id, "approve"));
        var pk = await f.Ops().SubmitPacketAsync("s1", form.Id, "The form validates", null, null, null, null, null, 0, 0);
        var r = await f.Ops().PressAsync(new PressRequest(pk.Id, "approve"));
        Assert.True(r.Ok, r.Message);
        var merge = f.Host.Ran[^1];
        Assert.Equal(["git checkout main", "git merge --no-ff wb/1-login-screen -m \"Merge WB-1.1: Form and validation\"", "git rev-parse --short HEAD", "- git push origin main", "git checkout wb/1-login-screen", "git merge main"], merge.Lines);
        using (var db = f.Db())
        {
            Assert.Equal(ItemState.Done, db.Items.Single(i => i.Id == form.Id).State);
            Assert.True(db.Items.Single(i => i.Id == top.Id).IsOpen);
            Assert.Equal("wb/1-login-screen", db.Items.Single(i => i.Id == top.Id).Branch);
        }

        // the second child opens on the same branch: no git at all
        await f.Ops().PatchItemAsync(remember.Id, new ItemPatch(null, null, true));
        var s2 = await f.Ops().SessionStartedAsync(Start("s2", @"D:\repo"));
        Assert.Contains("Remember me", s2.HookSpecificOutput!.AdditionalContext);
        await f.Ops().HelloAsync("s2", @"D:\repo", null);
        var runs = f.Host.Ran.Count;
        var plan2 = await f.Ops().SubmitPlanAsync("s2", remember.Id, [new("Keep the token", 20, null, null)], null, "goal");
        await f.Ops().PressAsync(new PressRequest(plan2.Id, "approve"));
        Assert.Equal(runs, f.Host.Ran.Count);

        // its packet finishes Login screen: merged, branch deleted, checkout back on main
        var pk2 = await f.Ops().SubmitPacketAsync("s2", remember.Id, "Remember me works", null, null, null, null, null, 0, 0);
        r = await f.Ops().PressAsync(new PressRequest(pk2.Id, "approve"));
        Assert.True(r.Ok, r.Message);
        Assert.Contains("git branch -d wb/1-login-screen", f.Host.Ran[^1].Lines);
        using (var db = f.Db())
        {
            var t = db.Items.Single(i => i.Id == top.Id);
            Assert.Equal(ItemState.Done, t.State);
            Assert.False(t.IsOpen);
            Assert.Null(t.Branch);
            var co = Json.ReadList<Checkout>(db.Machines.Single().CheckoutsJson).Single();
            Assert.Null(co.ItemId);
            Assert.Equal("main", co.Branch);
        }
    }

    [Fact]
    public async Task BranchNamesAreProjectCodeNumberAndSlug()
    {
        var p = new Project { Id = "lng", Code = "LNG", Name = "Language" };
        Assert.Equal("lng/3-parser-and-printer", Store.Ops.WorkbenchOps.BranchNameFor(p, new Item { Id = "LNG-3", ProjectId = "lng", Title = "Parser and printer" }));
        Assert.Equal("lng/3", Store.Ops.WorkbenchOps.BranchNameFor(p, new Item { Id = "LNG-3", ProjectId = "lng", Title = "!!!" }));
    }
}
