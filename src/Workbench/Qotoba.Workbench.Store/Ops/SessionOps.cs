using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

public sealed partial class WorkbenchOps
{
    /// <summary>SessionStart hook. Creates or resumes the session and hands back the brief as additional context.</summary>
    public async Task<HookReply> SessionStartedAsync(HookEvent e, CancellationToken ct = default)
    {
        var s = await db.Sessions.FindAsync([e.SessionId], ct);
        var machine = await EnsureLocalMachineAsync(ct);
        var isNew = s is null;
        if (s is null)
        {
            s = new Session { Id = e.SessionId, Started = Now, MachineId = machine.Id, Title = e.SessionTitle ?? "" };
            db.Sessions.Add(s);
        }
        s.State = SessionState.Working;
        s.Cwd = e.Cwd;
        s.TranscriptPath = e.TranscriptPath;
        s.MachineId ??= machine.Id;
        if (e.SessionTitle is { Length: > 0 } t) s.Title = t;
        s.ItemId ??= await ItemForCwdAsync(machine, e.Cwd, ct);
        await LedgerAsync(s, "start", isNew ? $"Session started on {machine.Id} ({e.Source})" : $"Session resumed ({e.Source})");

        if (isNew)
        {
            var card = NewCard(CardType.Info, $"Session {Short(s.Id)} started on {machine.Id}", null, s.Id);
            card.ProjectId = s.ItemId is null ? null : (await db.Items.FindAsync([s.ItemId], ct))?.ProjectId;
            card.ItemId = s.ItemId;
            card.Why = "A new Claude Code session reached the Workbench through the SessionStart hook.";
            card.PayloadJson = Json.Write(new InfoPayload
            {
                Body = s.ItemId is null
                    ? $"Started in {e.Cwd}. No item is attached yet; name one in the prompt (for example WB-1) or let the session call session_hello."
                    : $"Working on {s.ItemId} from {e.Cwd}.",
                Chip = "session:" + s.Id,
                Origin = $"SessionStart hook from {machine.Id}"
            });
            db.Cards.Add(card);
        }
        Audit(Actor.System, isNew ? "started" : "resumed", "session", s.Id, e.Cwd);
        await SaveAsync("session", s.Id, ct);
        return HookReply.Context("SessionStart", await BriefAsync(s.Id, ct));
    }

    /// <summary>MCP: the session says hello with the id it was given. Required before any other tool.</summary>
    public async Task<string> HelloAsync(string sessionId, string? cwd, string? itemId, CancellationToken ct = default)
    {
        var machine = await EnsureLocalMachineAsync(ct);
        var s = await db.Sessions.FindAsync([sessionId], ct);
        if (s is null)
        {
            s = new Session { Id = sessionId, Started = Now, MachineId = machine.Id, Cwd = cwd };
            db.Sessions.Add(s);
            await LedgerAsync(s, "start", $"Session said hello from {machine.Id}");
        }
        s.Hello = true;
        if (itemId is not null)
        {
            var it = await RequireItemAsync(itemId, ct);
            Rules.OneHomeMachine(it.HomeMachineId, machine.Id);
            s.ItemId = it.Id;
            it.HomeMachineId ??= machine.Id;
        }
        s.ItemId ??= await ItemForCwdAsync(machine, cwd, ct);
        await SaveAsync("session", s.Id, ct);
        return await BriefAsync(s.Id, ct);
    }

    public async Task SessionPromptAsync(HookEvent e, CancellationToken ct = default)
    {
        var s = await db.Sessions.FindAsync([e.SessionId], ct);
        if (s is null) { await SessionStartedAsync(e, ct); s = await db.Sessions.FindAsync([e.SessionId], ct); }
        if (s is null) return;
        s.TurnStarted = Now;
        s.Turns++;
        s.State = SessionState.Working;
        s.WaitingSince = null;
        if (e.Prompt is { } p)
        {
            var m = ItemIdRx.Match(p);
            if (m.Success && await db.Items.AnyAsync(i => i.Id == m.Value, ct)) s.ItemId = m.Value;
            await LedgerAsync(s, "prompt", p.Length > 80 ? p[..80] + "…" : p);
        }
        await SaveAsync("session", s.Id, ct);
    }

    /// <summary>PreToolUse: git only on a delivered press (R-05). Everything else passes.</summary>
    public async Task<HookReply> SessionPreToolAsync(HookEvent e, CancellationToken ct = default)
    {
        if (e.ToolName != "Bash" || e.ToolInput is null) return HookReply.Ok();
        var cmd = e.ToolInput.Value.TryGetProperty("command", out var c) ? c.GetString() ?? "" : "";
        var lower = cmd.ToLowerInvariant();
        if (lower.Contains("git push") && (lower.Contains(" main") || lower.Contains(" master")))
            return HookReply.Deny("Pushing to main is done by the Workbench after a packet is approved (ruling R-05). Submit a packet instead.");
        if (lower.Contains("git merge") || lower.Contains("git rebase"))
            return HookReply.Deny("Merging is done by the Host after the owner approves the packet (ruling R-05). Submit a packet instead.");
        if (lower.Contains("git push --force") || lower.Contains("git push -f"))
            return HookReply.Deny("Force pushes are never run by a session.");
        var s = await db.Sessions.FindAsync([e.SessionId], ct);
        if (s is not null) { await LedgerAsync(s, "tool", "bash: " + (cmd.Length > 70 ? cmd[..70] + "…" : cmd)); await SaveAsync("session", s.Id, ct); }
        return HookReply.Ok();
    }

    public async Task SessionPostToolAsync(HookEvent e, bool failed, CancellationToken ct = default)
    {
        var s = await db.Sessions.FindAsync([e.SessionId], ct);
        if (s is null) return;
        s.Tools++;
        s.LastEvent = Now;
        if (e.ToolName == "Bash" && e.ToolInput is { } input && input.TryGetProperty("command", out var c))
        {
            var cmd = c.GetString() ?? "";
            if (cmd.Contains("dotnet test") || cmd.Contains("dotnet build"))
                await RunFromBashAsync(s, cmd, e, failed, ct);
        }
        else if (e.ToolName is "Edit" or "Write" or "MultiEdit")
        {
            var path = e.ToolInput is { } ti && ti.TryGetProperty("file_path", out var fp) ? fp.GetString() : null;
            if (path is not null) await LedgerAsync(s, "write", Path.GetFileName(path));
        }
        await SaveAsync("session", s.Id, ct);
    }

    /// <summary>Stop: deliver owner messages (blocking the stop so the session reads them), measure the turn, mark waiting.</summary>
    public async Task<HookReply> SessionStopAsync(HookEvent e, CancellationToken ct = default)
    {
        var s = await db.Sessions.FindAsync([e.SessionId], ct);
        if (s is null) return HookReply.Ok();
        if (s.TurnStarted is { } ts) { s.Minutes += Fmt.MinutesBetween(ts, Now); s.TurnStarted = null; }
        s.LastEvent = Now;
        if (e.LastAssistantMessage is { Length: > 0 } last) s.LastAction = last.Length > 120 ? last[..120] + "…" : last;

        var pending = await db.Messages.Where(m => m.SessionId == s.Id && m.DeliveredAt == null).OrderBy(m => m.At).ToListAsync(ct);
        if (pending.Count > 0 && e.StopHookActive != true)
        {
            foreach (var m in pending) m.DeliveredAt = Now;
            await LedgerAsync(s, "answer", $"{pending.Count} owner message(s) delivered at stop");
            await SaveAsync("session", s.Id, ct);
            var text = string.Join("\n\n", pending.Select(m => (m.CardId is null ? "" : $"[{m.CardId}] ") + m.Text));
            return HookReply.Block("The owner answered in the Workbench. Act on this, then continue:\n\n" + text);
        }

        var waitingOn = await db.Cards.Where(c => c.SessionId == s.Id && c.State == CardState.Open && c.Type != CardType.Info).CountAsync(ct);
        if (waitingOn > 0) { s.State = SessionState.Waiting; s.WaitingSince ??= Now; }
        else s.State = SessionState.Working;
        await LedgerAsync(s, "stop", waitingOn > 0 ? $"Turn ended · waiting on {waitingOn} card(s)" : "Turn ended");
        await SaveAsync("session", s.Id, ct);
        return HookReply.Ok();
    }

    public async Task SessionEndedAsync(HookEvent e, CancellationToken ct = default)
    {
        var s = await db.Sessions.FindAsync([e.SessionId], ct);
        if (s is null) return;
        if (s.TurnStarted is { } ts) { s.Minutes += Fmt.MinutesBetween(ts, Now); s.TurnStarted = null; }
        s.State = SessionState.Ended;
        s.Ended = Now;
        await LedgerAsync(s, "end", $"Session ended ({e.Reason})");
        Audit(Actor.System, "ended", "session", s.Id, e.Reason);
        await SaveAsync("session", s.Id, ct);
    }

    /// <summary>What a session needs to start: item, goal, steps, rulings, open cards, how to work.</summary>
    public async Task<string> BriefAsync(string sessionId, CancellationToken ct = default)
    {
        var s = await db.Sessions.FindAsync([sessionId], ct);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Qotoba Workbench brief");
        sb.AppendLine($"Your session id is `{sessionId}`. Call the MCP tool `session_hello` with it before any other Workbench tool, and pass it as `session` to every tool.");
        sb.AppendLine();
        Item? it = s?.ItemId is null ? null : await db.Items.FindAsync([s.ItemId], ct);
        if (it is null)
        {
            sb.AppendLine("No item is attached to this session. Only the owner creates items. If the prompt names one (for example WB-1), call `session_hello` with `item`.");
        }
        else
        {
            var p = await ProjectOfAsync(it, ct);
            sb.AppendLine($"## Item {it.Id} · {it.Title}");
            sb.AppendLine($"Project {p.Name} ({p.Code}) · state {it.State.ToString().ToLowerInvariant()} · home machine {it.HomeMachineId ?? "not set"}");
            if (!string.IsNullOrWhiteSpace(it.Goal)) sb.AppendLine("Goal: " + it.Goal);
            var steps = List<Step>(it.StepsJson);
            if (steps.Count > 0)
            {
                sb.AppendLine("Steps:");
                foreach (var st in steps) sb.AppendLine($"  {st.N}. [{st.State.ToString().ToLowerInvariant()}] {st.Text} · {st.EstMin} Claude-min");
            }
            else sb.AppendLine("No plan yet: submit one with `plan_submit` (at most 7 steps, each with an estimate).");
            var open = await db.Cards.Where(c => c.ItemId == it.Id && c.State == CardState.Open && c.Type != CardType.Info).ToListAsync(ct);
            if (open.Count > 0)
            {
                sb.AppendLine("Open cards on this item (wait for them with `card_wait`):");
                foreach (var c in open) sb.AppendLine($"  {c.Id} · {c.Type.ToString().ToLowerInvariant()} · {c.Headline}");
            }
            var notes = await db.Messages.Where(m => m.SessionId == sessionId && m.DeliveredAt == null).ToListAsync(ct);
            if (notes.Count > 0)
            {
                sb.AppendLine("Messages from the owner:");
                foreach (var m in notes) { sb.AppendLine("  " + m.Text); m.DeliveredAt = Now; m.Via = "hello"; }
                await db.SaveChangesAsync(ct);
            }
        }
        var rulings = await db.Rulings.OrderByDescending(r => r.Date).Take(12).ToListAsync(ct);
        if (rulings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Rulings (search more with `rulings_search`)");
            foreach (var r in rulings) sb.AppendLine($"- {r.Id} {r.Title}: {r.Text}");
        }
        sb.AppendLine();
        sb.AppendLine("## How to work");
        sb.AppendLine($"- You never create projects, items or lanes. You fill capped slots on your item: a plan (≤{Caps.PlanSteps} steps), questions (≤{Caps.OpenQuestionsPerItem} open), assumptions, found items (≤{Caps.FoundPerSession}), a note (≤{Caps.NoteLines} lines), claims with runs, and a packet at the end.");
        sb.AppendLine("- Ask a question only when a step is blocked; give 2 to 4 options, each with a reason and a cost to change, and recommend one. Otherwise assume the recommended answer with `assume` and carry on.");
        sb.AppendLine("- Never say tests passed: run them, and the Workbench records the run. A claim without a run shows as unverified.");
        sb.AppendLine("- Git: commit on your item's branch; never merge or push to main. Submit a packet with `packet_submit`; the owner's Approve runs the git instruction.");
        sb.AppendLine("- When stuck after three attempts on a check, call `stuck_report` and stop.");
        return sb.ToString();
    }

    private async Task<Machine> EnsureLocalMachineAsync(CancellationToken ct)
    {
        var m = await db.Machines.FindAsync([options.MachineId], ct);
        if (m is null)
        {
            m = new Machine { Id = options.MachineId, Os = OsName(), Registered = Now, Online = true, LastSeen = Now, HostVersion = "" };
            db.Machines.Add(m);
        }
        return m;
    }

    private static string OsName() => Environment.OSVersion.Platform == PlatformID.Win32NT
        ? "Windows " + Environment.OSVersion.Version.Major
        : Environment.OSVersion.VersionString;

    private async Task<string?> ItemForCwdAsync(Machine m, string? cwd, CancellationToken ct)
    {
        if (cwd is null) return null;
        var checkouts = List<Checkout>(m.CheckoutsJson);
        var hit = checkouts.Where(c => c.ItemId is not null && cwd.StartsWith(c.Path, StringComparison.OrdinalIgnoreCase)).OrderByDescending(c => c.Path.Length).FirstOrDefault();
        if (hit?.ItemId is not null && await db.Items.AnyAsync(i => i.Id == hit.ItemId, ct)) return hit.ItemId;
        return null;
    }

    private async Task RunFromBashAsync(Session s, string cmd, HookEvent e, bool failed, CancellationToken ct)
    {
        var text = e.ToolResponse?.ToString() ?? "";
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var tail = string.Join("\n", lines.TakeLast(Caps.RunTailLines));
        var passed = 0; var total = 0; var fail = failed;
        foreach (var l in lines)
        {
            var m = System.Text.RegularExpressions.Regex.Match(l, @"Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)");
            if (m.Success) { passed = int.Parse(m.Groups[2].Value); total = int.Parse(m.Groups[4].Value); if (int.Parse(m.Groups[1].Value) > 0) fail = true; }
            if (l.Contains("Build FAILED") || l.Contains("error CS")) fail = true;
        }
        var check = cmd.Contains("dotnet test") ? "tests" : "build";
        var run = new Run
        {
            Id = Ids.Short(), Check = check, ItemId = s.ItemId, SessionId = s.Id, MachineId = s.MachineId,
            Result = fail ? RunResult.Fail : RunResult.Pass, Passed = passed, Total = total,
            DurationSec = Math.Round((e.DurationMs ?? 0) / 1000.0, 1), At = Now, Cmd = cmd, OutputTail = tail, ExitCode = fail ? 1 : 0
        };
        db.Runs.Add(run);
        await LedgerAsync(s, "check", $"{check} {(fail ? "failed" : "passed")}{(total > 0 ? $" {passed}/{total}" : "")} · run {run.Id}");
        notify.Changed("run", run.Id);
    }

    private static string Short(string id) => id.Length > 4 ? id[..4] : id;
}
