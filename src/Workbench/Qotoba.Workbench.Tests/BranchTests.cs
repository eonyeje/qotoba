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
    public void BranchNamesAreProjectCodeNumberAndSlug()
    {
        var p = new Project { Id = "lng", Code = "LNG", Name = "Language" };
        Assert.Equal("lng/3-parser-and-printer", Store.Ops.WorkbenchOps.BranchNameFor(p, new Item { Id = "LNG-3", ProjectId = "lng", Title = "Parser and printer" }));
        Assert.Equal("lng/3", Store.Ops.WorkbenchOps.BranchNameFor(p, new Item { Id = "LNG-3", ProjectId = "lng", Title = "!!!" }));
    }
}
