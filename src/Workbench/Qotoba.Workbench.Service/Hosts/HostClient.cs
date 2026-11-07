using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Store;
using Qotoba.Workbench.Store.Ops;

namespace Qotoba.Workbench.Service.Hosts;

/// <summary>Reaches the local Host over HTTP. The Service never touches a checkout itself.</summary>
public sealed class HostClient(HttpClient http, IServiceScopeFactory scopes, IOptions<WorkbenchOptions> options, Auth.TokenAuth auth) : IHostClient
{
    private async Task<string> UrlFor(string machineId, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkbenchDb>();
        var m = await db.Machines.FindAsync([machineId], ct);
        return (m?.HostUrl ?? options.Value.HostUrl).TrimEnd('/');
    }

    private HttpRequestMessage Req(HttpMethod method, string url, object? body)
    {
        var r = new HttpRequestMessage(method, url);
        r.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.DevToken);
        if (body is not null) r.Content = JsonContent.Create(body);
        return r;
    }

    public async Task<GitResult> RunGitAsync(string machineId, GitInstruction instruction, CancellationToken ct = default)
    {
        try
        {
            using var res = await http.SendAsync(Req(HttpMethod.Post, await UrlFor(machineId, ct) + "/git", instruction), ct);
            var body = await res.Content.ReadFromJsonAsync<GitResult>(ct);
            return body ?? new GitResult(false, "The Host answered with nothing.");
        }
        catch (Exception ex) { return new GitResult(false, "The Host on " + machineId + " is not reachable: " + ex.Message); }
    }

    public async Task<string?> ReadLinesAsync(string machineId, string checkout, string file, int from, int to, CancellationToken ct = default)
    {
        try
        {
            using var res = await http.SendAsync(Req(HttpMethod.Get, $"{await UrlFor(machineId, ct)}/lines?checkout={Uri.EscapeDataString(checkout)}&file={Uri.EscapeDataString(file)}&from={from}&to={to}", null), ct);
            return res.IsSuccessStatusCode ? await res.Content.ReadAsStringAsync(ct) : null;
        }
        catch { return null; }
    }

    public async Task<bool> IsOnlineAsync(string machineId, CancellationToken ct = default)
    {
        try
        {
            using var res = await http.SendAsync(Req(HttpMethod.Get, await UrlFor(machineId, ct) + "/health", null), ct);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
