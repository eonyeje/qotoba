using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Qotoba.Workbench.Contracts;

namespace Qotoba.Workbench.Tests;

/// <summary>The three surfaces on one origin, against a temporary data folder.</summary>
public sealed class ServiceFactory : WebApplicationFactory<Program>
{
    public string DataDir { get; } = Path.Combine(Path.GetTempPath(), "qwb-tests-" + Guid.NewGuid().ToString("N"));
    public string Token => File.ReadAllText(Path.Combine(DataDir, "token")).Trim();

    public ServiceFactory()
    {
        // The service reads its options as soon as the builder exists, before the factory's configuration callbacks run,
        // so the test folder goes in through the environment, which the default configuration reads first.
        Directory.CreateDirectory(DataDir);
        var ui = Path.Combine(DataDir, "ui"); Directory.CreateDirectory(ui);
        File.WriteAllText(Path.Combine(ui, "index.html"), "<!doctype html><title>t</title>");
        Environment.SetEnvironmentVariable("Workbench__DataDir", DataDir);
        Environment.SetEnvironmentVariable("Workbench__UiDir", ui);
        Environment.SetEnvironmentVariable("Workbench__MachineId", "test-pc");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Workbench:DataDir"] = DataDir, ["Workbench:MachineId"] = "test-pc", ["Urls"] = "http://localhost:0"
        }));
    }

    public HttpClient Authed()
    {
        var c = CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        return c;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { Directory.Delete(DataDir, true); } catch { }
    }
}

public class ServiceTests : IClassFixture<ServiceFactory>
{
    private readonly ServiceFactory f;
    public ServiceTests(ServiceFactory factory) => f = factory;

    [Fact]
    public async Task ApiRefusesWithoutTokenAndAcceptsWithIt()
    {
        var anon = f.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/snapshot")).StatusCode);
        var ok = await f.Authed().GetAsync("/api/snapshot");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task SnapshotHasEveryCollectionThePrototypeReads()
    {
        var wb = await f.Authed().GetFromJsonAsync<JsonElement>("/api/snapshot");
        foreach (var key in new[] { "meta", "budgets", "projects", "lanes", "items", "questions", "assumptions", "found", "rulings", "proposed", "files", "types", "nyw", "comments", "commits", "branches", "claudemd", "runs", "sessions", "machines", "tokens", "install", "cards", "packets", "plans", "stucks", "settings", "chat", "register", "reportOrder", "reportFolded", "subtypes", "states", "fpbs", "edges", "cardWhy", "cardOrigin" })
            Assert.True(wb.TryGetProperty(key, out _), "missing " + key);
        Assert.Equal(8, wb.GetProperty("projects").GetArrayLength());
        Assert.Equal("open", wb.GetProperty("projects")[0].GetProperty("state").GetString());
    }

    [Fact]
    public async Task HooksAnswerSessionStartWithTheBrief()
    {
        var r = await f.Authed().PostAsJsonAsync("/hooks", new { session_id = "h1", hook_event_name = "SessionStart", cwd = "D:/x", source = "startup" });
        var reply = await r.Content.ReadFromJsonAsync<HookReply>();
        Assert.Contains("session_hello", reply!.HookSpecificOutput!.AdditionalContext);
    }

    [Fact]
    public async Task McpListsTheCappedToolsAndNoStructureTools()
    {
        var c = f.Authed();
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        var init = await c.PostAsJsonAsync("/mcp", new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new { protocolVersion = "2025-06-18", capabilities = new { }, clientInfo = new { name = "t", version = "0" } } });
        Assert.Equal(HttpStatusCode.OK, init.StatusCode);
        var list = await c.PostAsJsonAsync("/mcp", new { jsonrpc = "2.0", id = 2, method = "tools/list" });
        var text = await list.Content.ReadAsStringAsync();
        if (text.StartsWith("event:")) text = text.Split('\n').First(l => l.StartsWith("data:"))[5..].Trim();
        var names = JsonDocument.Parse(text).RootElement.GetProperty("result").GetProperty("tools").EnumerateArray().Select(t => t.GetProperty("name").GetString()).ToList();
        Assert.Contains("session_hello", names);
        Assert.Contains("question_ask", names);
        Assert.Contains("packet_submit", names);
        Assert.DoesNotContain(names, n => n!.Contains("item_create") || n.Contains("project") || n.Contains("lane"));
    }

    [Fact]
    public async Task OwnerAddsAnItemAndItShowsInTheSnapshot()
    {
        var c = f.Authed();
        var r = await c.PostAsJsonAsync("/api/items", new AddItemRequest("task", "Snapshot test item", "wb", "~1 week", null, null));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var id = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
        var wb = await c.GetFromJsonAsync<JsonElement>("/api/snapshot");
        Assert.Equal("Snapshot test item", wb.GetProperty("items").GetProperty(id).GetProperty("title").GetString());
        Assert.Equal("task", wb.GetProperty("items").GetProperty(id).GetProperty("kind").GetString());
    }
}
