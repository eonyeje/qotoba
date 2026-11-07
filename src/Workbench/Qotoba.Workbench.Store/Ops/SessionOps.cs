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
        s.ItemId ??= await ItemForCwdAsync(machine, cwd ?? s.Cwd, ct);
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

    /// <summary>PreToolUse: the Workbench only writes down what the session runs. It never refuses anything; what the owner says in chat stands.</summary>
    public async Task<HookReply> SessionPreToolAsync(HookEvent e, CancellationToken ct = default)
    {
        if (e.ToolName != "Bash" || e.ToolInput is null) return HookReply.Ok();
        var cmd = e.ToolInput.Value.TryGetProperty("command", out var c) ? c.GetString() ?? "" : "";
        var s = await db.Sessions.FindAsync([e.SessionId], ct);
        if (s is not null && cmd.Contains("git ")) { await LedgerAsync(s, "git", cmd.Length > 90 ? cmd[..90] + "…" : cmd); await SaveAsync("session", s.Id, ct); }
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
            if (cmd.Contains("dotnet test") || cmd.Contains("dotnet build")) await ResultFromBashAsync(s, cmd, e, failed, ct);
            else if (cmd.Contains("git push") || cmd.Contains("git merge") || cmd.Contains("git commit")) await GitFromBashAsync(s, cmd, failed, ct);
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

    /// <summary>What a session needs to start: its item, the decisions that apply, the three ways to reach the owner.</summary>
    public async Task<string> BriefAsync(string sessionId, CancellationToken ct = default)
    {
        var s = await db.Sessions.FindAsync([sessionId], ct);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Qotoba Workbench brief");
        sb.AppendLine($"Your session id is `{sessionId}`. Call the MCP tool `session_hello` with it before any other Workbench tool, and pass it as `session` to every tool.");
        sb.AppendLine();
        Item? it = s?.ItemId is null ? null : await db.Items.FindAsync([s.ItemId], ct);
        Project? p = it is null ? null : await ProjectOfAsync(it, ct);
        if (it is null)
        {
            sb.AppendLine("No item is attached to this session: nothing is open in the tree for this checkout. When the owner names the work in chat, write it into the tree with `items_write` and call `session_hello` again; if they open a node meanwhile, `session_hello` picks it up.");
        }
        else
        {
            sb.AppendLine($"## Item {it.Id} · {it.Title}");
            sb.AppendLine($"Project {p!.Name} ({p.Code}) · state {it.State.ToString().ToLowerInvariant()}" + (it.HomeMachineId is null ? "" : $" · home machine {it.HomeMachineId}"));
            var top = await TopOfAsync(it, ct);
            if (top.Id != it.Id) sb.AppendLine($"Under {top.Id} · {top.Title}");
            sb.AppendLine(top.Branch is null
                ? $"Branch: none recorded yet. The convention: the opened top item has one branch, `{BranchNameFor(p, top)}`, that everything under it shares; create or check it out yourself and record it with `branch_note`."
                : $"Branch: {top.Branch}" + (top.Checkout is null ? "" : $" in {top.Checkout}") + $", shared by everything under {top.Id}.");
            if (!string.IsNullOrWhiteSpace(it.Goal)) sb.AppendLine("Goal: " + it.Goal);
            var steps = List<Step>(it.StepsJson);
            if (steps.Count > 0)
            {
                sb.AppendLine("Steps:");
                foreach (var st in steps) sb.AppendLine($"  {st.N}. [{st.State.ToString().ToLowerInvariant()}] {st.Text} · {st.EstMin} Claude-min");
            }
            else sb.AppendLine("No steps yet: write yours with `steps_write` once you know them. They give the item its progress; nobody approves them.");
            var fpbs = await db.Reports.Where(r => (r.AttachItemId == it.Id || r.TakenOnItemId == it.Id) && r.State != "Done" && r.State != "Exists" && r.State != "Won't do").ToListAsync(ct);
            if (fpbs.Count > 0)
            {
                sb.AppendLine("Open FPBs on this item:");
                foreach (var r in fpbs) sb.AppendLine($"  {r.Id} · {r.State} · {r.Title}");
            }
            var open = await db.Cards.Where(c => c.ItemId == it.Id && c.State == CardState.Open && c.Type != CardType.Info).ToListAsync(ct);
            if (open.Count > 0)
            {
                sb.AppendLine("Open cards on this item (wait for them with `card_wait`):");
                foreach (var c in open) sb.AppendLine($"  {c.Id} · {c.Type.ToString().ToLowerInvariant()} · {c.Headline}");
            }
            var decided = List<Decision>(it.DecisionsJson);
            if (decided.Count > 0)
            {
                sb.AppendLine("Decided on this item:");
                foreach (var d in decided.TakeLast(12)) sb.AppendLine($"  - {d.T}: {d.A} ({d.How}, {d.Date})");
            }
        }
        var notes = s is null ? [] : await db.Messages.Where(m => m.SessionId == sessionId && m.DeliveredAt == null).ToListAsync(ct);
        if (notes.Count > 0)
        {
            sb.AppendLine("Messages from the owner:");
            foreach (var m in notes) { sb.AppendLine("  " + m.Text); m.DeliveredAt = Now; m.Via = "hello"; }
            await db.SaveChangesAsync(ct);
        }
        var rulings = await db.Rulings.Where(r => r.NeverReopened && (p == null || r.ProjectId == null || r.ProjectId == p.Id)).OrderByDescending(r => r.Date).Take(15).ToListAsync(ct);
        if (rulings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Decisions that apply (search more with `rulings_search`)");
            foreach (var r in rulings) sb.AppendLine($"- {r.Id} {r.Text}");
        }
        sb.AppendLine();
        sb.AppendLine("## How to work");
        sb.AppendLine("- The owner directs you in chat. The Workbench writes down what you do; it never refuses anything the owner says. Commit, merge and push when the owner says so, on the item's branch, and never otherwise.");
        sb.AppendLine("- Write what the owner dictates: items with `items_write`, the node they name with `item_open`, an FTR with `fpb_raise` when they ask for a feature or change, a decision with `decision_record` the moment they state a rule. A BUG or PER you raise yourself, from your own checks, never for code you have not committed yet.");
        sb.AppendLine("- Three things reach the owner's inbox, nothing else: a page (`page_submit`) for a change that has no designer, so they can answer on it and approve it; `deviation_report` when you must leave the agreed design or did something they asked to be told about (a helper type, a static); `stuck_report` after three failed attempts on one check. A small blocked question is not a card: take the recommended answer, record it with `assume`, carry on.");
        sb.AppendLine("- Never say tests passed: run them. The hooks record dotnet test and build on the project.");
        sb.AppendLine("- Nothing closes, merges or pushes on its own. When the work is finished, say so in chat and advise: tests green, FPBs fixed, ready to merge, push and mark done. The owner decides. Only then commit, merge, push as they said, and record it with `item_done` in one sentence of what is now true.");
        sb.AppendLine("- Every id you mention in chat comes with its blurb. Keep notes to two lines.");
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

    private async Task ResultFromBashAsync(Session s, string cmd, HookEvent e, bool failed, CancellationToken ct)
    {
        var text = e.ToolResponse?.ToString() ?? "";
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var passed = 0; var total = 0; var fail = failed;
        foreach (var l in lines)
        {
            var m = System.Text.RegularExpressions.Regex.Match(l, @"Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)");
            if (m.Success) { passed = int.Parse(m.Groups[2].Value); total = int.Parse(m.Groups[4].Value); if (int.Parse(m.Groups[1].Value) > 0) fail = true; }
            if (l.Contains("Build FAILED") || l.Contains("error CS")) fail = true;
        }
        var check = cmd.Contains("dotnet test") ? "tests" : "build";
        var scope = System.Text.RegularExpressions.Regex.Match(cmd, @"(?:--project\s+|dotnet (?:test|build)\s+)([^\s]+\.(?:csproj|slnx|sln))");
        if (scope.Success) check += " · " + Path.GetFileNameWithoutExtension(scope.Groups[1].Value);
        await RecordResultAsync(new ResultReport(check, null, s.ItemId, s.Id, fail ? "fail" : "pass", passed, total, Math.Round((e.DurationMs ?? 0) / 1000.0, 1), cmd), ct);
    }

    /// <summary>A commit, merge or push the session ran on the owner's word is written against its item. Nothing is checked or refused.</summary>
    private async Task GitFromBashAsync(Session s, string cmd, bool failed, CancellationToken ct)
    {
        var verb = cmd.Contains("git push") ? "pushed" : cmd.Contains("git merge") ? "merged" : "committed";
        if (failed) verb += " (failed)";
        await LedgerAsync(s, "git", $"{verb}: {(cmd.Length > 80 ? cmd[..80] + "…" : cmd)}");
        if (s.ItemId is not null) await HistoryAsync(s.ItemId, verb, $"By [[session:{s.Id}]] on your word", ct);
    }

    private static string Short(string id) => id.Length > 4 ? id[..4] : id;
}
