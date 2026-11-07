using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Store;
using Qotoba.Workbench.Store.Ops;

namespace Qotoba.Workbench.Service.Render;

/// <summary>Keeps one burn-down point per project per day: measured hours of work left. Seven days are kept.</summary>
public sealed class DailySampler(IServiceScopeFactory scopes, Fmt fmt, IClock clock, ILogger<DailySampler> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), ct);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        do
        {
            try { await SampleAsync(ct); } catch (Exception ex) { log.LogWarning(ex, "burn-down sample failed"); }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    public async Task SampleAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkbenchDb>();
        var today = fmt.Day(clock.Now);
        var items = await db.Items.Where(i => i.State != ItemState.Done).ToListAsync(ct);
        foreach (var p in await db.Projects.Where(p => p.State == ProjectState.Open).ToListAsync(ct))
        {
            var minutes = items.Where(i => i.ProjectId == p.Id && i.ParentId == null).Sum(i => Math.Max(0, (int)Math.Round((i.EstMin ?? 0) * (i.Factor ?? 1.2)) - i.SoMin));
            var hours = (int)Math.Round(minutes / 60.0);
            var key = "burn:" + p.Id;
            var row = await db.Settings.FindAsync([key], ct);
            var list = Json.ReadList<Snapshot.BurnSample>(row?.Value);
            list.RemoveAll(b => b.Day == today);
            list.Add(new Snapshot.BurnSample(today, hours));
            if (list.Count > 7) list.RemoveRange(0, list.Count - 7);
            if (row is null) db.Settings.Add(new Setting { Key = key, Value = Json.Write(list) }); else row.Value = Json.Write(list);
        }
        await db.SaveChangesAsync(ct);
    }
}
