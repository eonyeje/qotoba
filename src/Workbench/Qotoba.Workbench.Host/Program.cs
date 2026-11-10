using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Qotoba.Workbench.Contracts;

/*
 * qotoba-host · the only process that touches a disk for the Workbench.
 * Registers this machine with the Service, heartbeats, says which branch a checkout is on, reads file lines for
 * pulled code, opens a file in Rider, indexes //+ comments, and on the owner's click makes a worktree for a second
 * Rider window. It runs no git on its own and no tests. Everything else lives in the Service.
 *
 *   qotoba-host serve [--workbench http://localhost:5290] [--port 5291] [--name <machine>] [--checkout <path>]...
 *   qotoba-host register ...   (same flags; registers once and exits)
 */

var args0 = args.Length == 0 ? ["serve"] : args;
var cmd = args0[0];
var workbench = Arg("--workbench") ?? Environment.GetEnvironmentVariable("QOTOBA_WB_URL") ?? "http://localhost:5290";
var port = int.TryParse(Arg("--port"), out var p) ? p : 5291;
var name = Arg("--name") ?? Environment.GetEnvironmentVariable("QOTOBA_WB_MACHINE") ?? Environment.MachineName.ToLowerInvariant();
var checkouts = Args("--checkout");
if (checkouts.Count == 0 && Directory.Exists(Path.Combine(Environment.CurrentDirectory, ".git"))) checkouts.Add(Environment.CurrentDirectory);
await DiscoverWorktreesAsync();
var tokenPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".qotoba-workbench", "token");
var token = Environment.GetEnvironmentVariable("QOTOBA_WB_TOKEN") ?? (File.Exists(tokenPath) ? File.ReadAllText(tokenPath).Trim() : "");
const string Version = "0.1.0";

var http = new HttpClient { BaseAddress = new Uri(workbench.TrimEnd('/') + "/") };
http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

async Task<bool> RegisterAsync()
{
    var reg = new MachineRegistration(name, OsName(), Version, "machine", $"http://localhost:{port}", await DescribeCheckoutsAsync(), DateTimeOffset.UtcNow);
    try
    {
        var r = await http.PostAsJsonAsync("api/machines", reg);
        Console.WriteLine(r.IsSuccessStatusCode ? $"registered {name} with {workbench} · {reg.Checkouts.Count} checkout(s)" : $"registration failed: {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return r.IsSuccessStatusCode;
    }
    catch (Exception ex) { Console.WriteLine("Workbench not reachable: " + ex.Message); return false; }
}

if (cmd == "register") { Environment.Exit(await RegisterAsync() ? 0 : 1); return; }
if (cmd != "serve") { Console.WriteLine("usage: qotoba-host serve|register [--workbench url] [--port n] [--name machine] [--checkout path]..."); return; }

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });
builder.WebHost.UseUrls($"http://localhost:{port}");
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
var app = builder.Build();

app.Use(async (ctx, next) =>
{
    var auth = ctx.Request.Headers.Authorization.ToString();
    if (string.IsNullOrEmpty(token) || auth == "Bearer " + token) { await next(); return; }
    ctx.Response.StatusCode = 401;
});

app.MapGet("/health", () => Results.Ok(new { ok = true, machine = name, version = Version, at = DateTimeOffset.UtcNow }));

// the branch a checkout is on right now (the Service asks when a session starts)
app.MapGet("/branch", async (string checkout) =>
{
    if (!Directory.Exists(checkout)) return Results.NotFound();
    var (code, output) = await RunAsync("git", "rev-parse --abbrev-ref HEAD", checkout, TimeSpan.FromSeconds(10));
    return code == 0 ? Results.Text(output.Trim()) : Results.NotFound();
});

// the owner asked for an item in a new Rider window: check its branch out as a worktree beside the main checkout
app.MapPost("/worktree", async (WorktreeRequest req) =>
{
    if (!Directory.Exists(req.Checkout)) return Results.Json(new WorktreeResult(false, req.Folder, req.Branch, false, "No such checkout: " + req.Checkout));
    var folder = Path.GetFullPath(req.Folder);
    if (Directory.Exists(folder))
    {
        var (c0, b0) = await RunAsync("git", "rev-parse --abbrev-ref HEAD", folder, TimeSpan.FromSeconds(10));
        if (c0 == 0) { if (!checkouts.Contains(folder, StringComparer.OrdinalIgnoreCase)) checkouts.Add(folder); _ = RegisterAsync(); return Results.Json(new WorktreeResult(true, folder, b0.Trim(), false, "already there, on " + b0.Trim())); }
        return Results.Json(new WorktreeResult(false, folder, req.Branch, false, "The folder exists and is not a checkout: " + folder));
    }
    var (has, _) = await RunAsync("git", "rev-parse --verify --quiet refs/heads/" + req.Branch, req.Checkout, TimeSpan.FromSeconds(10));
    var (code, output) = has == 0
        ? await RunAsync("git", $"worktree add \"{folder}\" {req.Branch}", req.Checkout, TimeSpan.FromMinutes(2))
        : await RunAsync("git", $"worktree add -b {req.Branch} \"{folder}\" main", req.Checkout, TimeSpan.FromMinutes(2));
    if (code != 0) return Results.Json(new WorktreeResult(false, folder, req.Branch, false, output.Trim()));
    checkouts.Add(folder);
    _ = RegisterAsync();
    return Results.Json(new WorktreeResult(true, folder, req.Branch, true, output.Trim()));
});

app.MapGet("/lines", async (string checkout, string file, int from, int to) =>
{
    var path = Path.IsPathRooted(file) ? file : Path.Combine(checkout, file);
    if (!File.Exists(path)) return Results.NotFound();
    var lines = await File.ReadAllLinesAsync(path);
    from = Math.Max(1, from); to = Math.Min(lines.Length, Math.Max(from, to));
    if (to - from + 1 > 40) to = from + 39;
    return Results.Text(string.Join("\n", lines.Skip(from - 1).Take(to - from + 1)));
});

// a whole file of a checkout, for a document the owner opens in the Workbench pane; never outside the checkout
app.MapGet("/file", (string checkout, string file) =>
{
    if (!Directory.Exists(checkout)) return Results.NotFound();
    var root = Path.GetFullPath(checkout).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    var path = Path.GetFullPath(Path.Combine(root, file.Replace('/', Path.DirectorySeparatorChar)));
    if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return Results.NotFound();
    var ext = Path.GetExtension(path).ToLowerInvariant();
    var type = ext switch
    {
        ".html" or ".htm" => "text/html; charset=utf-8", ".css" => "text/css; charset=utf-8", ".js" or ".mjs" => "text/javascript; charset=utf-8",
        ".json" => "application/json; charset=utf-8", ".svg" => "image/svg+xml", ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".gif" => "image/gif",
        ".webp" => "image/webp", ".woff" => "font/woff", ".woff2" => "font/woff2", ".pdf" => "application/pdf", _ => "text/plain; charset=utf-8"
    };
    return Results.File(path, type);
});

app.MapGet("/open", (string file, int line) =>
{
    var rider = Environment.GetEnvironmentVariable("QOTOBA_RIDER") ?? "rider64";
    try { Process.Start(new ProcessStartInfo(rider, $"--line {line} \"{file}\"") { UseShellExecute = true }); return Results.Ok(new { ok = true }); }
    catch (Exception ex) { return Results.Json(new { ok = false, error = ex.Message }); }
});

async Task IndexAllAsync()
{
    foreach (var c in checkouts)
    {
        try
        {
            var post = await Qotoba.Workbench.Host.Indexer.IndexAsync(name, c, null, (a, cwd, _) => RunAsync("git", a, cwd, TimeSpan.FromSeconds(60)));
            var r = await http.PostAsJsonAsync("api/index", post);
            if (!r.IsSuccessStatusCode) Console.WriteLine($"index of {c} refused: {(int)r.StatusCode}");
        }
        catch (Exception ex) { Console.WriteLine($"index of {c} failed: {ex.Message}"); }
    }
}

var heartbeat = Task.Run(async () =>
{
    await RegisterAsync();
    await IndexAllAsync();
    using var t = new PeriodicTimer(TimeSpan.FromSeconds(60));
    while (await t.WaitForNextTickAsync()) { await RegisterAsync(); await IndexAllAsync(); }
});

// a save in any checkout re-indexes it a few seconds later
var watchers = new List<FileSystemWatcher>();
var pending = 0;
foreach (var c in checkouts.Where(Directory.Exists))
{
    var w = new FileSystemWatcher(c, "*.cs") { IncludeSubdirectories = true, EnableRaisingEvents = true, NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName };
    FileSystemEventHandler h = async (_, e) =>
    {
        if (e.FullPath.Contains("\\bin\\") || e.FullPath.Contains("\\obj\\")) return;
        if (Interlocked.Exchange(ref pending, 1) == 1) return;
        await Task.Delay(3000);
        Interlocked.Exchange(ref pending, 0);
        await IndexAllAsync();
    };
    w.Changed += h; w.Created += h; w.Deleted += h; w.Renamed += (s2, e2) => h(s2, e2);
    watchers.Add(w);
}

Console.WriteLine($"qotoba-host {Version} · {name} · listening on http://localhost:{port} · workbench {workbench}");
app.Run();

string? Arg(string flag) { var i = Array.IndexOf(args0, flag); return i >= 0 && i + 1 < args0.Length ? args0[i + 1] : null; }
List<string> Args(string flag) { var l = new List<string>(); for (var i = 0; i < args0.Length - 1; i++) if (args0[i] == flag) l.Add(args0[i + 1]); return l; }

static string OsName() => OperatingSystem.IsWindows() ? "Windows " + Environment.OSVersion.Version.Build switch { >= 26100 => "11 24H2", >= 22000 => "11", _ => "10" } : Environment.OSVersion.VersionString;

/// the worktrees that already hang off a checkout are checkouts too: a second Rider window may be open on one
async Task DiscoverWorktreesAsync()
{
    foreach (var c in checkouts.ToList())
    {
        var (code, output) = await RunAsync("git", "worktree list --porcelain", c, TimeSpan.FromSeconds(10));
        if (code != 0) continue;
        foreach (var line in output.Split('\n'))
        {
            if (!line.StartsWith("worktree ")) continue;
            var path = Path.GetFullPath(line[9..].Trim());
            if (Directory.Exists(path) && !checkouts.Contains(path, StringComparer.OrdinalIgnoreCase)) checkouts.Add(path);
        }
    }
}

async Task<List<CheckoutInfo>> DescribeCheckoutsAsync()
{
    var list = new List<CheckoutInfo>();
    foreach (var c in checkouts)
    {
        var (_, branch) = await RunAsync("git", "rev-parse --abbrev-ref HEAD", c, TimeSpan.FromSeconds(10));
        var (_, wt) = await RunAsync("git", "rev-parse --git-dir", c, TimeSpan.FromSeconds(10));
        // the remote says which repository this is a checkout of, so a document recorded against the project is found on any machine
        var (rc, remote) = await RunAsync("git", "remote get-url origin", c, TimeSpan.FromSeconds(10));
        list.Add(new CheckoutInfo(c, branch.Trim(), null, wt.Contains("worktrees"), null, rc == 0 && remote.Trim().Length > 0 ? remote.Trim() : null));
    }
    return list;
}

static async Task<(int code, string output)> RunAsync(string file, string arguments, string cwd, TimeSpan timeout)
{
    var psi = new ProcessStartInfo(file, arguments) { WorkingDirectory = cwd, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
    try
    {
        using var proc = Process.Start(psi)!;
        var so = proc.StandardOutput.ReadToEndAsync();
        var se = proc.StandardError.ReadToEndAsync();
        using var cts = new CancellationTokenSource(timeout);
        try { await proc.WaitForExitAsync(cts.Token); }
        catch (OperationCanceledException) { try { proc.Kill(true); } catch { } return (124, "timed out after " + timeout); }
        return (proc.ExitCode, (await so) + (await se));
    }
    catch (Exception ex) { return (127, ex.Message); }
}

