using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Store.Ops;

namespace Qotoba.Workbench.Mcp;

/// <summary>
/// The capped slots a Claude Code session may fill. There is no tool that creates a project, an item or a lane.
/// Every tool takes the Claude session id so calls from several sessions never mix.
/// </summary>
[McpServerToolType]
public static class WorkbenchTools
{
    private static string Err(WorkbenchRuleException ex) => $"REFUSED ({ex.Code}): {ex.Message}";

    [McpServerTool(Name = "session_hello"), Description("Say hello with your session id (from the SessionStart context). Required before any other Workbench tool. Returns your brief.")]
    public static async Task<string> SessionHello(WorkbenchOps ops,
        [Description("Your Claude Code session id")] string session,
        [Description("The working directory you were started in")] string? cwd = null,
        [Description("The item you are working on, if the prompt named one (for example WB-1)")] string? item = null)
    {
        try { return await ops.HelloAsync(session, cwd, item); } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "brief_get"), Description("The brief for this session: item, goal, steps, open cards, rulings, how to work.")]
    public static async Task<string> BriefGet(WorkbenchOps ops, [Description("Your session id")] string session)
    {
        try { return await ops.BriefAsync(session); } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "rulings_search"), Description("Search the owner's rulings before proposing anything. Returns id, title and text.")]
    public static async Task<string> RulingsSearch(WorkbenchOps ops, [Description("Words to look for")] string query)
    {
        var q = query.ToLowerInvariant();
        var hits = await ops.Db.Rulings.Where(r => r.Text.ToLower().Contains(q) || r.Title.ToLower().Contains(q)).OrderByDescending(r => r.Date).Take(12).ToListAsync();
        return hits.Count == 0 ? "No ruling mentions that." : string.Join("\n", hits.Select(r => $"{r.Id} · {r.Title}: {r.Text}"));
    }

    [McpServerTool(Name = "item_get"), Description("An item as the owner sees it: title, goal, state, steps with proofs, note, claims.")]
    public static async Task<string> ItemGet(WorkbenchOps ops, [Description("Item id, for example WB-2.1")] string item)
    {
        var it = await ops.Db.Items.FindAsync(item);
        if (it is null) return $"REFUSED (item.unknown): there is no item {item}.";
        var steps = Json.ReadList<Step>(it.StepsJson);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{it.Id} · {it.Title} · {it.State.ToString().ToLowerInvariant()} · home {it.HomeMachineId ?? "none"} · est {it.EstMin?.ToString() ?? "none"} Claude-min · so far {it.SoMin} min");
        if (!string.IsNullOrWhiteSpace(it.Goal)) sb.AppendLine("Goal: " + it.Goal);
        foreach (var s in steps) sb.AppendLine($"  {s.N}. [{s.State.ToString().ToLowerInvariant()}] {s.Text} · {s.EstMin} min" + (s.ProofRef is null ? "" : " · proof " + s.ProofRef));
        if (it.Note is not null) sb.AppendLine("Note: " + it.Note);
        foreach (var c in Json.ReadList<Claim>(it.ClaimsJson)) sb.AppendLine($"Claim: {c.Text} · {(c.ProofRef ?? "unverified")}");
        return sb.ToString();
    }

    [McpServerTool(Name = "plan_submit"), Description("Submit a plan for your item: at most 7 steps, each with an estimate in Claude-minutes and what proves it. Settle questions carry a recommended default so the owner can accept them all at once. The owner approves the plan on a card; wait for it with card_wait.")]
    public static async Task<string> PlanSubmit(WorkbenchOps ops,
        [Description("Your session id")] string session,
        [Description("Item id")] string item,
        [Description("One sentence: what is true when the item is done")] string goal,
        [Description("The steps, in order")] List<StepInput> steps,
        [Description("Questions you can default; each has options and the index of the recommended one")] List<SettleInput>? settle = null,
        [Description("Lane key, if the owner named one")] string? lane = null)
    {
        try { var c = await ops.SubmitPlanAsync(session, item, steps, settle, goal, lane); return $"Plan card {c.Id} is in the owner's inbox. Wait for it with card_wait."; }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "step_update"), Description("Mark a step current, done (with the run that proves it), blocked or todo.")]
    public static async Task<string> StepUpdate(WorkbenchOps ops, string session, string item,
        [Description("Step number from the approved plan")] int step,
        [Description("todo, current, done or blocked")] string state,
        [Description("Run id that proves the step, required for done")] string? proofRun = null,
        [Description("A short note on the step")] string? note = null)
    {
        try { await ops.StepUpdateAsync(session, item, step, state, proofRun, note); return "ok"; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "question_ask"), Description("Ask the owner a closed question that blocks a step: 2 to 4 options, each with a reason and a cost to change later, exactly one recommended. At most 2 open per item. Prefer assume() and carry on when the recommended answer is cheap to change.")]
    public static async Task<string> QuestionAsk(WorkbenchOps ops, string session, string item,
        [Description("The step this blocks")] int step,
        [Description("The question, one sentence")] string text,
        [Description("The options")] List<OptionInput> options,
        [Description("Two or three sentences of context; reference files as file:<id>:<from>-<to>, rulings as ruling:R-n")] string? context = null,
        [Description("The code the question is about, as file:<id>:<from>-<to> (pulled, never pasted)")] string? codeRef = null,
        [Description("What you tried before asking, one line each")] List<string>? tried = null,
        [Description("The When implementing line this settles, if it comes from a report (for example BUG-2-L1)")] string? implLine = null,
        [Description("The + comment this is about, if the question comes from one")] string? comment = null)
    {
        try { var c = await ops.AskAsync(session, item, step, text, options, context, codeRef, comment, tried, default, implLine); return $"Question card {c.Id} is in the owner's inbox. Wait with card_wait, or continue on other steps."; }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "report_get"), Description("A report (FTR, BUG or PER) with its sections and its When implementing lines and their ids and statuses.")]
    public static async Task<string> ReportGet(WorkbenchOps ops, [Description("Report id, for example BUG-2")] string report)
    {
        var r = await ops.Db.Reports.FindAsync(report);
        if (r is null) return $"REFUSED (report.unknown): there is no report {report}.";
        var lines = await ops.Db.ImplLines.Where(l => l.ReportId == report).OrderBy(l => l.Order).ToListAsync();
        var takers = await ops.Db.Items.Where(i => i.ImplementsJson.Contains("fpb:" + report)).Select(i => i.Id).ToListAsync();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{r.Id} · {r.Kind} · {r.Title} · state {r.State} · severity {(r.Severity?.ToString().ToLowerInvariant() ?? "not set")} · taken on by {(takers.Count == 0 ? "no task yet" : string.Join(", ", takers))}");
        sb.AppendLine("Sections: " + r.SectionsJson);
        foreach (var l in lines) sb.AppendLine($"  {l.Id} [{l.Kind.ToString().ToLowerInvariant()} · {l.Status.ToString().ToLowerInvariant()}] {l.Text}" + (l.Rec is null ? "" : $" · rec {l.Rec}") + (l.Note is null ? "" : $" · {l.Note}"));
        return sb.ToString();
    }

    [McpServerTool(Name = "report_write"), Description("Write sections of a report the owner created, and its When implementing lines: decisions (options, one recommended, each with a reason and a cost to change), proofs (acceptance lines a run must prove), dependencies (rec = item:<id> or fpb:<id>), preconditions. Sections are objects keyed by the report's section names; text fields are at most 3 lines, the note 2, possible fixes 3.")]
    public static async Task<string> ReportWrite(WorkbenchOps ops, string session, [Description("Report id")] string report,
        [Description("Sections keyed by name, each an object, e.g. {\"symptom\":{\"text\":\"...\",\"expected\":\"...\",\"actual\":\"...\"}}")] Dictionary<string, System.Text.Json.JsonElement>? sections = null,
        [Description("The When implementing lines; replaces the lines no task has drawn yet")] List<ImplLineInput>? lines = null)
    {
        try { var r = await ops.WriteReportAsync(session, report, sections, lines); return $"{r.Id} updated · {await ops.Db.ImplLines.CountAsync(l => l.ReportId == r.Id)} lines"; }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "impl_prove"), Description("A run proves a When implementing proof line (or a precondition). Pass the line id and the run id.")]
    public static async Task<string> ImplProve(WorkbenchOps ops, string session, [Description("Line id, for example BUG-2-L3")] string line, [Description("Run id")] string run)
    {
        try { await ops.ProveLineAsync(session, line, run); return "ok"; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "impl_drop"), Description("Drop a When implementing line that no longer applies, with the reason. Settled and proved lines cannot be dropped.")]
    public static async Task<string> ImplDrop(WorkbenchOps ops, string session, string line, string why)
    {
        try { await ops.DropLineAsync(session, line, why); return "ok"; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "dependency_propose"), Description("Propose that one item or report needs another. The owner confirms it in the next plan's settle pass; until then it is a proposal.")]
    public static async Task<string> DependencyPropose(WorkbenchOps ops, string session,
        [Description("The dependant: item:WB-2.3 or fpb:BUG-1")] string from, [Description("What it needs: item:WB-2.1 or fpb:FTR-3")] string to,
        [Description("needs, related or duplicate")] string type, [Description("Why, up to three lines")] string reason, [Description("Hard: nothing can start before it")] bool hard = false)
    {
        try { var e = await ops.ProposeEdgeAsync(session, from, to, type, reason, hard); return e.Id; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "code_pull"), Description("Pull up to 20 lines of a file from the session's checkout, to cite in a question as file:<id>:<from>-<to>. Code is pulled, never pasted into a card.")]
    public static async Task<string> CodePull(WorkbenchOps ops, IHostClient host, string session, [Description("Path, repo-relative or absolute")] string file, int from, int to)
    {
        var s = await ops.Db.Sessions.FindAsync(session);
        if (s is null) return "REFUSED (session.unknown): call session_hello first.";
        if (to - from + 1 > Caps.PulledCodeLines) to = from + Caps.PulledCodeLines - 1;
        var text = await host.ReadLinesAsync(s.MachineId ?? ops.Options.MachineId, s.Cwd ?? "", file, from, to);
        return text ?? "The Host could not read that file.";
    }

    [McpServerTool(Name = "card_wait"), Description("Wait for the owner to answer a card. Returns the answer, or 'no answer yet' after the timeout (max 600 s). Call again to keep waiting.")]
    public static async Task<string> CardWait(WorkbenchOps ops, [Description("Card id")] string card, [Description("Seconds to wait, up to 600")] int seconds = 300, CancellationToken ct = default)
    {
        try
        {
            var a = await ops.WaitAnswerAsync(card, TimeSpan.FromSeconds(Math.Clamp(seconds, 5, 600)), ct);
            return a ?? "no answer yet";
        }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "assume"), Description("Record an assumption you are proceeding on. The owner reviews them in a batch on your packet; a flipped one means do the opposite.")]
    public static async Task<string> Assume(WorkbenchOps ops, string session, string item, [Description("One sentence")] string text)
    {
        try { var a = await ops.AssumeAsync(session, item, text); return a.Id; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "found_add"), Description("Something you noticed that is not your task: a bug, a feature, a performance problem. At most 5 per session; it goes to the owner's tray, never into the tree.")]
    public static async Task<string> FoundAdd(WorkbenchOps ops, string session, string item,
        [Description("FTR, BUG or PER")] string kind, [Description("Title, one line")] string title, [Description("Why it matters, up to three lines")] string why,
        [Description("Evidence: run:<id> or file:<id>:<from>-<to>")] string? evidence = null, [Description("Where it probably belongs, an item id")] string? suggestItem = null)
    {
        try { var f = await ops.FoundAsync(session, item, kind, title, why, evidence, suggestItem); return f.Id; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "note_write"), Description("Your note on the item, at most 2 lines. Replaces the previous note.")]
    public static async Task<string> NoteWrite(WorkbenchOps ops, string session, string item, string text)
    {
        try { await ops.NoteAsync(session, item, text); return "ok"; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "claim_add"), Description("A claim about the work, with the run that proves it. A claim without a run shows as unverified.")]
    public static async Task<string> ClaimAdd(WorkbenchOps ops, string session, string item, string text, [Description("Run id")] string? run = null)
    {
        try { await ops.ClaimAsync(session, item, text, run); return "ok"; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "run_record"), Description("Record a check you ran (build, tests, specimens, smoke) with its result and the last lines of output. Prefer letting the hooks record dotnet test and build automatically.")]
    public static async Task<string> RunRecord(WorkbenchOps ops, string session, string? item,
        [Description("Check name: build, tests, specimens, round-trip, smoke, or your own")] string check,
        [Description("pass, fail or warn")] string result, [Description("The exact command")] string cmd,
        [Description("The last 40 lines of output")] string outputTail, int passed = 0, int total = 0, double durationSec = 0, int? exitCode = null, int? step = null)
    {
        try
        {
            var s = await ops.Db.Sessions.FindAsync(session);
            var r = await ops.RecordRunAsync(new RunReport(check, item ?? s?.ItemId, session, s?.MachineId ?? ops.Options.MachineId, result, passed, total, durationSec, cmd, outputTail, 1, exitCode, null, null, step));
            return "run:" + r.Id;
        }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "packet_submit"), Description("Finish: submit the packet for the owner's Approve. Say the outcome in one sentence, list the git lines Approve should run (commit on your branch, merge, push), the checks with their runs, the claims, anything built beyond the plan, and a note of at most 2 lines.")]
    public static async Task<string> PacketSubmit(WorkbenchOps ops, string session, string item,
        [Description("One sentence: what is now true")] string outcome,
        [Description("The git lines the Host runs on Approve, one per entry")] List<string> instruction,
        [Description("Absolute path of the checkout the git lines run in")] string checkout,
        List<CheckInput>? checks = null, List<ClaimInput>? claims = null, List<BeyondInput>? beyond = null, string? note = null,
        [Description("Files changed, repo-relative")] List<string>? files = null, int plus = 0, int minus = 0)
    {
        try { var c = await ops.SubmitPacketAsync(session, item, outcome, instruction, checkout, checks, claims, beyond, note, files, plus, minus); return $"Packet card {c.Id} is in the owner's inbox. You are done; a later session continues if the owner asks for changes."; }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "stuck_report"), Description("A check failed three times on the same step: report what you tried and your diagnosis, then stop. The owner reassigns, allows one more try, or drops the step.")]
    public static async Task<string> StuckReport(WorkbenchOps ops, string session, string item, int step,
        [Description("The check that fails, for example tests · HookOrderingTests.TwoMachinesInterleave")] string checkName,
        List<AttemptInput> attempts, [Description("Your diagnosis in up to three lines")] string diagnosis)
    {
        try { var c = await ops.ReportStuckAsync(session, item, step, checkName, attempts, diagnosis); return $"Stuck card {c.Id} is in the owner's inbox. Stop now."; }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "ruling_propose"), Description("From a design chat: propose rulings in the owner's words, one line each. They reach the owner as one Record card; nothing is written until the owner ticks them.")]
    public static async Task<string> RulingPropose(WorkbenchOps ops, string session, [Description("What the chat was about")] string chatTitle, List<string> rulings)
    {
        try { var c = await ops.ProposeRulingsAsync(session, chatTitle, rulings); return c.Id; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }
}
