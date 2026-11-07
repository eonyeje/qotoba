using Qotoba.Workbench.Contracts;

namespace Qotoba.Workbench.Store.Ops;

public interface IClock { DateTimeOffset Now { get; } }

public sealed class SystemClock : IClock { public DateTimeOffset Now => DateTimeOffset.UtcNow; }

/// <summary>Raised after every change so the UI can refetch. The Service's EventHub implements it.</summary>
public interface IChangeNotifier { void Changed(string kind, string id); }

public sealed class NoNotifier : IChangeNotifier { public void Changed(string kind, string id) { } }

/// <summary>The only way the Service reaches a disk: through the Host on a machine.</summary>
public interface IHostClient
{
    Task<GitResult> RunGitAsync(string machineId, GitInstruction instruction, CancellationToken ct = default);
    Task<string?> ReadLinesAsync(string machineId, string checkout, string file, int from, int to, CancellationToken ct = default);
    Task<bool> IsOnlineAsync(string machineId, CancellationToken ct = default);
    Task<bool> OpenAsync(string machineId, string file, int line, CancellationToken ct = default);
    /// <summary>The branch a checkout is on right now, or null when the Host cannot say.</summary>
    Task<string?> BranchAsync(string machineId, string checkout, CancellationToken ct = default);
}

public sealed class NoHost : IHostClient
{
    public Task<GitResult> RunGitAsync(string machineId, GitInstruction instruction, CancellationToken ct = default) =>
        Task.FromResult(new GitResult(false, "No Host is registered for " + machineId + "."));
    public Task<string?> ReadLinesAsync(string machineId, string checkout, string file, int from, int to, CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<bool> IsOnlineAsync(string machineId, CancellationToken ct = default) => Task.FromResult(false);
    public Task<bool> OpenAsync(string machineId, string file, int line, CancellationToken ct = default) => Task.FromResult(false);
    public Task<string?> BranchAsync(string machineId, string checkout, CancellationToken ct = default) => Task.FromResult<string?>(null);
}

public sealed class OpsOptions
{
    public string MachineId { get; set; } = Environment.MachineName.ToLowerInvariant();
    public string Owner { get; set; } = "Owner";
    public string Programme { get; set; } = "Qotoba";
    public string TimeZone { get; set; } = "Africa/Lagos";
}

/// <summary>Dates and durations exactly as the prototype prints them.</summary>
public sealed class Fmt(OpsOptions options, IClock clock)
{
    private readonly TimeZoneInfo _tz = Resolve(options.TimeZone);

    private static TimeZoneInfo Resolve(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { return TimeZoneInfo.Local; }
    }

    public DateTimeOffset Local(DateTimeOffset t) => TimeZoneInfo.ConvertTime(t, _tz);
    public DateTimeOffset NowLocal => Local(clock.Now);

    /// <summary>"6 Nov 04:51"</summary>
    public string DayTime(DateTimeOffset t) { var l = Local(t); return $"{l.Day} {l:MMM} {l:HH:mm}"; }
    /// <summary>"6 Nov"</summary>
    public string Day(DateTimeOffset t) { var l = Local(t); return $"{l.Day} {l:MMM}"; }
    /// <summary>"04:51"</summary>
    public string Time(DateTimeOffset t) => Local(t).ToString("HH:mm");
    /// <summary>"Fri 6 Nov 2026 · 05:12"</summary>
    public string Stamp(DateTimeOffset t) { var l = Local(t); return $"{l:ddd} {l.Day} {l:MMM yyyy} · {l:HH:mm}"; }

    /// <summary>Minutes between two moments, never negative.</summary>
    public static int MinutesBetween(DateTimeOffset a, DateTimeOffset b) => Math.Max(0, (int)Math.Round((b - a).TotalMinutes));

    /// <summary>"46 min", "1 h 14 min"</summary>
    public static string Minutes(int m)
    {
        if (m < 60) return m + " min";
        var h = m / 60; var r = m % 60;
        return r == 0 ? h + " h" : $"{h} h {r:00} min";
    }
}
