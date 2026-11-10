using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Store.Ops;

namespace Qotoba.Workbench.Tests;

/// <summary>Documents belong to items and projects; the file stays in the repository and is reached from wherever the owner looks.</summary>
public class DocumentTests : IDisposable
{
    private readonly OpsFixture f = new();
    public void Dispose() => f.Dispose();

    [Fact]
    public async Task ADocumentIsRecordedOnAProjectOrAnItemAndListedWithoutAnyMachinePath()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Design QDL", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");

        var msg = await f.Ops().DocAddAsync("s1", "WB", "QDL design", @".\docs\design\qdl.html", null);
        Assert.Contains("project WB", msg);
        Assert.Contains("docs/design/qdl.html", msg);

        // the session's own item when no node is named
        await f.Ops().DocAddAsync("s1", null, "QDL syntax reference", "docs/design/qdl-syntax.html", null);
        var onItem = await f.Ops().DocListAsync("WB-1");
        Assert.Contains("QDL syntax reference", onItem);
        Assert.DoesNotContain("QDL design ·", onItem);
        var onProject = await f.Ops().DocListAsync("WB");
        Assert.Contains("QDL design", onProject);
        Assert.Contains("QDL syntax reference", onProject);

        // the same title on the same node is a move, not a second entry
        await f.Ops().DocAddAsync("s1", "WB", "QDL design", "docs/qdl/index.html", null);
        using (var db = f.Db())
        {
            var docs = await db.Documents.Where(d => d.ProjectId == "wb").ToListAsync();
            Assert.Equal(2, docs.Count);
            Assert.Equal("docs/qdl/index.html", docs.Single(d => d.Title == "QDL design").Path);
            Assert.All(docs, d => Assert.DoesNotContain(@"D:\", d.Path!));
        }

        var removed = await f.Ops().DocRemoveAsync("s1", "WB", "QDL design", "superseded");
        Assert.Contains("untouched", removed);
        Assert.DoesNotContain("QDL design ·", await f.Ops().DocListAsync("WB"));
    }

    [Fact]
    public async Task APathMustBeRepoRelativeAndADocumentNeedsAPathOrAUrlNotBoth()
    {
        await f.Ops().AddItemAsync(new AddItemRequest("task", "Design QDL", "wb", null, null, null), Actor.Owner);
        await f.SessionOnAsync("s1", "WB-1");
        await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().DocAddAsync("s1", "WB", "Escape", "../secrets.txt", null));
        await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().DocAddAsync("s1", "WB", "Neither", null, null));
        await Assert.ThrowsAsync<WorkbenchRuleException>(() => f.Ops().DocAddAsync("s1", "WB", "Both", "docs/a.html", "https://example.org/a"));
        var external = await f.Ops().DocAddAsync("s1", "WB", "ANTLR grammar reference", null, "https://github.com/antlr/antlr4/blob/dev/doc/index.md");
        Assert.Contains("https://github.com/antlr", external);
    }

    [Fact]
    public async Task TheNodesOwnCheckoutIsReadFirstAndASecondCheckoutIsKnownByItsRemote()
    {
        var top = await f.Ops().AddItemAsync(new AddItemRequest("task", "Design QDL", "wb", null, null, null), Actor.Owner);
        var remote = "git@github.com:eonyeje/Qotoba.git";
        await f.Ops().RegisterMachineAsync(new MachineRegistration("test-pc", "Windows", "1", "machine", "http://localhost:5291",
            [new CheckoutInfo(@"D:\repo", "main", null, false, null, remote), new CheckoutInfo(@"D:\repo-design-qdl", "wb/1-design-qdl", top.Id, true, null, remote)], f.Clock.Now));
        await f.Ops().RegisterMachineAsync(new MachineRegistration("laptop", "Windows", "1", "machine", "http://laptop:5291",
            [new CheckoutInfo(@"C:\src\Qotoba", "main", null, false, null, "https://github.com/eonyeje/Qotoba.git"), new CheckoutInfo(@"C:\src\Other", "main", null, false, null, "https://github.com/eonyeje/Other.git")], f.Clock.Now));

        using var db = f.Db();
        var p = await db.Projects.FindAsync("wb");
        var it = await db.Items.FindAsync(top.Id);
        var items = await db.Items.ToListAsync();
        var machines = await db.Machines.ToListAsync();

        var places = WorkbenchOps.DocPlaces(p!, it, items, machines);
        Assert.Equal([@"D:\repo-design-qdl", @"D:\repo", @"C:\src\Qotoba"], places.Select(x => x.Checkout).ToArray());
        Assert.DoesNotContain(places, x => x.Checkout == @"C:\src\Other");

        Assert.Equal("github.com/eonyeje/qotoba", WorkbenchOps.RemoteKey(remote));
        Assert.True(WorkbenchOps.SameRemote(remote, "https://github.com/eonyeje/Qotoba"));
        Assert.Equal("https://raw.githubusercontent.com/eonyeje/Qotoba/wb/1-design-qdl/docs/design/qdl.html", WorkbenchOps.RawUrl(remote, "wb/1-design-qdl", "docs/design/qdl.html"));
        Assert.Null(WorkbenchOps.RawUrl("https://dev.azure.com/x/y/_git/z", "main", "a.html"));
    }

    [Fact]
    public async Task ADocumentIsReadAtTheNodesBranchWhileItIsOpenAndAtMainAfter()
    {
        var top = await f.Ops().AddItemAsync(new AddItemRequest("task", "Design QDL", "wb", null, null, null), Actor.Owner);
        using (var db = f.Db())
        {
            var p = await db.Projects.FindAsync("wb");
            var it = await db.Items.FindAsync(top.Id);
            Assert.Equal("main", await f.Ops().DocRefAsync(p!, it));
        }
        await f.Ops().PatchItemAsync(top.Id, new ItemPatch(null, null, true));
        using (var db = f.Db())
        {
            var p = await db.Projects.FindAsync("wb");
            var it = await db.Items.FindAsync(top.Id);
            Assert.Equal("wb/1-design-qdl", await f.Ops().DocRefAsync(p!, it));
        }
        await f.Ops().OpenProjectAsync("wb");
        using (var db = f.Db())
        {
            var p = await db.Projects.FindAsync("wb");
            Assert.Equal("wb/workbench", await f.Ops().DocRefAsync(p!, null));
        }
    }
}
