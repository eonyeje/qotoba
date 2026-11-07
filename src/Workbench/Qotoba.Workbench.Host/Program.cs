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
 * Registers this machine with the Service, heartbeats, runs the git lines of an approved packet,
 * reads file lines for pulled code, and runs checks on request. Everything else lives in the Service.
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

app.MapPost("/git", async (GitInstruction ins) =>
{
    var cwd = string.IsNullOrWhiteSpace(ins.Checkout) ? (checkouts.FirstOrDefault() ?? Environment.CurrentDirectory) : ins.Checkout;
    if (!Directory.Exists(cwd)) return Results.Json(new GitResult(false, "No such checkout: " + cwd));
    var log = new System.Text.StringBuilder();
    foreach (var raw in ins.Lines)
    {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith('#')) continue;
        if (line.StartsWith("cd ", StringComparison.OrdinalIgnoreCase)) { var d = line[3..].Trim().Trim('"'); if (Directory.Exists(d)) cwd = d; else { log.AppendLine("no such folder: " + d); return Results.Json(new GitResult(false, log.ToString())); } continue; }
        if (!line.StartsWith("git ", StringComparison.OrdinalIgnoreCase)) { log.AppendLine("refused (only git lines run here): " + line); return Results.Json(new GitResult(false, log.ToString())); }
        var (code, output) = await RunAsync("git", line[4..], cwd, TimeSpan.FromMinutes(5));
        log.AppendLine("$ " + line); log.AppendLine(output.Trim());
        if (code != 0) { log.AppendLine($"exit {code}"); return Results.Json(new GitResult(false, log.ToString())); }
    }
    return Results.Json(new GitResult(true, log.ToString()));
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

app.MapPost("/run", async (RunRequest req) =>
{
    var cwd = string.IsNullOrWhiteSpace(req.Checkout) ? (checkouts.FirstOrDefault() ?? Environment.CurrentDirectory) : req.Checkout;
    var sw = Stopwatch.StartNew();
    var parts = req.Command.Split(' ', 2);
    var (code, output) = await RunAsync(parts[0], parts.Length > 1 ? parts[1] : "", cwd, TimeSpan.FromMinutes(20));
    sw.Stop();
    var lines = output.Replace("\r\n", "\n").Split('\n');
    var tail = string.Join("\n", lines.TakeLast(40));
    int passed = 0, total = 0;
    foreach (var l in lines)
    {
        var m = System.Text.RegularExpressions.Regex.Match(l, @"Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)");
        if (m.Success) { passed = int.Parse(m.Groups[2].Value); total = int.Parse(m.Groups[4].Value); }
    }
    var report = new RunReport(req.Check, req.Item, req.Session, name, code == 0 ? "pass" : "fail", passed, total, Math.Round(sw.Elapsed.TotalSeconds, 1), req.Command, tail, 1, code, null, null, req.Step);
    try { var r = await http.PostAsJsonAsync("api/runs", report); return Results.Json(new { ok = r.IsSuccessStatusCode, exit = code, passed, total, tail }); }
    catch (Exception ex) { return Results.Json(new { ok = false, exit = code, error = ex.Message, tail }); }
});

app.MapGet("/open", (string file, int line) =>
{
    var rider = Environment.GetEnvironmentVariable("QOTOBA_RIDER") ?? "rider64";
    try { Process.Start(new ProcessStartInfo(rider, $"--line {line} \"{file}\"") { UseShellExecute = true }); return Results.Ok(new { ok = true }); }
    catch (Exception ex) { return Results.Json(new { ok = false, error = ex.Message }); }
});

var heartbeat = Task.Run(async () =>
{
    await RegisterAsync();
    using var t = new PeriodicTimer(TimeSpan.FromSeconds(60));
    while (await t.WaitForNextTickAsync()) await RegisterAsync();
});

Console.WriteLine($"qotoba-host {Version} · {name} · listening on http://localhost:{port} · workbench {workbench}");
app.Run();

string? Arg(string flag) { var i = Array.IndexOf(args0, flag); return i >= 0 && i + 1 < args0.Length ? args0[i + 1] : null; }
List<string> Args(string flag) { var l = new List<string>(); for (var i = 0; i < args0.Length - 1; i++) if (args0[i] == flag) l.Add(args0[i + 1]); return l; }

static string OsName() => OperatingSystem.IsWindows() ? "Windows " + Environment.OSVersion.Version.Build switch { >= 26100 => "11 24H2", >= 22000 => "11", _ => "10" } : Environment.OSVersion.VersionString;

async Task<List<CheckoutInfo>> DescribeCheckoutsAsync()
{
    var list = new List<CheckoutInfo>();
    foreach (var c in checkouts)
    {
        var (_, branch) = await RunAsync("git", "rev-parse --abbrev-ref HEAD", c, TimeSpan.FromSeconds(10));
        var (_, wt) = await RunAsync("git", "rev-parse --git-dir", c, TimeSpan.FromSeconds(10));
        list.Add(new CheckoutInfo(c, branch.Trim(), null, wt.Contains("worktrees"), null));
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

sealed record RunRequest(string Check, string Command, string? Checkout, string? Item, string? Session, int? Step);
