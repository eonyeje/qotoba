using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Store.Ops;

namespace Qotoba.Workbench.Mcp;

/// <summary>
/// What a Claude Code session writes into the Workbench, on the owner's word or from its own checks. Every tool takes the
/// Claude session id so calls from several sessions never mix. Nothing here asks the owner except page_submit,
/// deviation_report and stuck_report.
/// </summary>
[McpServerToolType]
public static class WorkbenchTools
{
    private static string Err(WorkbenchRuleException ex) => $"REFUSED ({ex.Code}): {ex.Message}";

    [McpServerTool(Name = "session_hello"), Description("Say hello with your session id (from the SessionStart context). Required before any other Workbench tool. Returns your brief.")]
    public static async Task<string> SessionHello(WorkbenchOps ops,
        [Description("Your Claude Code session id")] string session,
        [Description("The working directory you were started in")] string? cwd = null,
        [Description("The item you are working on, if the owner named one")] string? item = null)
    {
        try { return await ops.HelloAsync(session, cwd, item); } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "brief_get"), Description("The brief for this session: item, steps, open FPBs, decisions that apply, how to work.")]
    public static async Task<string> BriefGet(WorkbenchOps ops, [Description("Your session id")] string session)
    {
        try { return await ops.BriefAsync(session); } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    /* ---------------------------------------------------------------- the work tree */

    [McpServerTool(Name = "items_write"), Description("Write the breakdown the owner dictated in chat: items under an item or project, any depth. The owner shapes the tree; you write it. Never invent items the owner did not ask for.")]
    public static async Task<string> ItemsWrite(WorkbenchOps ops, string session,
        [Description("The items, each with a title, an optional size (~half a day, ~3 days, ~1 week, ~3 weeks) and optional children")] List<ItemInput> items,
        [Description("The item id or project code they go under; defaults to your item")] string? under = null)
    {
        try { var made = await ops.AddItemsAsync(session, under, items); return string.Join("\n", made.Select(i => $"{i.Id} · {i.Title}")); }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "item_open"), Description("The owner said in chat which item to work on: open that node. If a sibling is already open, that one is opened instead and you are told. You keep the branch convention yourself (one branch per opened top item) and record it with branch_note.")]
    public static async Task<string> ItemOpen(WorkbenchOps ops, string session, [Description("Item id, for example WB-1.2")] string item)
    {
        try { var (it, message) = await ops.OpenNodeFromChatAsync(session, item); return $"{it.Id} · {it.Title} · {message}"; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "item_get"), Description("An item as the owner sees it: title, goal, state, steps, note, decisions, open FPBs.")]
    public static async Task<string> ItemGet(WorkbenchOps ops, [Description("Item id, for example WB-2.1")] string item)
    {
        var it = await ops.Db.Items.FindAsync(item);
        if (it is null) return $"REFUSED (item.unknown): there is no item {item}.";
        var steps = Json.ReadList<Step>(it.StepsJson);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{it.Id} · {it.Title} · {it.State.ToString().ToLowerInvariant()}{(it.IsOpen ? " · open" : "")} · est {it.EstMin?.ToString() ?? "none"} Claude-min · so far {it.SoMin} min" + (it.Branch is null ? "" : $" · branch {it.Branch}"));
        if (!string.IsNullOrWhiteSpace(it.Goal)) sb.AppendLine("Goal: " + it.Goal);
        foreach (var s in steps) sb.AppendLine($"  {s.N}. [{s.State.ToString().ToLowerInvariant()}] {s.Text} · {s.EstMin} min" + (s.Note is null ? "" : " · " + s.Note));
        if (it.Note is not null) sb.AppendLine("Note: " + it.Note);
        if (it.Outcome is not null) sb.AppendLine("Outcome: " + it.Outcome);
        foreach (var d in Json.ReadList<Decision>(it.DecisionsJson)) sb.AppendLine($"Decided: {d.T} → {d.A} ({d.How}, {d.Date})");
        var kids = await ops.Db.Items.Where(i => i.ParentId == it.Id).OrderBy(i => i.Order).ToListAsync();
        foreach (var k in kids) sb.AppendLine($"  under it: {k.Id} · {k.Title} · {k.State.ToString().ToLowerInvariant()}{(k.IsOpen ? " · open" : "")}");
        var fpbs = await ops.Db.Reports.Where(r => r.AttachItemId == it.Id || r.TakenOnItemId == it.Id).OrderBy(r => r.Id).ToListAsync();
        foreach (var r in fpbs) sb.AppendLine($"  FPB: {r.Id} · {r.State} · {r.Title}");
        return sb.ToString();
    }

    [McpServerTool(Name = "steps_write"), Description("Your own steps for the item, each with an estimate in Claude-minutes. As many as the work needs; nobody approves them. They give the item its progress. Rewriting keeps the state of steps whose text did not change.")]
    public static async Task<string> StepsWrite(WorkbenchOps ops, string session, string item,
        [Description("The steps, in order")] List<StepInput> steps,
        [Description("One sentence: what is true when the item is done")] string? goal = null)
    {
        try { await ops.StepsWriteAsync(session, item, steps, goal); return "ok"; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "step_update"), Description("Mark a step current, done, blocked or todo.")]
    public static async Task<string> StepUpdate(WorkbenchOps ops, string session, string item,
        [Description("Step number")] int step,
        [Description("todo, current, done or blocked")] string state,
        [Description("A short note on the step")] string? note = null)
    {
        try { await ops.StepUpdateAsync(session, item, step, state, note); return "ok"; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "item_done"), Description("The item is finished: say in one sentence what is now true. Done is written down; nothing waits on the owner. Commit, merge and push only when the owner says so.")]
    public static async Task<string> ItemDone(WorkbenchOps ops, string session, string item,
        [Description("One sentence: what is now true")] string outcome,
        [Description("A note of at most two lines, if something should be known")] string? note = null)
    {
        try { return await ops.ItemDoneAsync(session, item, outcome, note); } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "note_write"), Description("Your note on the item, at most 2 lines. Replaces the previous note.")]
    public static async Task<string> NoteWrite(WorkbenchOps ops, string session, string item, string text)
    {
        try { await ops.NoteAsync(session, item, text); return "ok"; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "assume"), Description("Record an assumption you are proceeding on, one sentence. It is a line on the item, not a card. If it turns out to matter, report a deviation.")]
    public static async Task<string> Assume(WorkbenchOps ops, string session, string item, string text)
    {
        try { var a = await ops.AssumeAsync(session, item, text); return a.Id; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "branch_note"), Description("Record which branch the opened top item is on, after you created or checked it out yourself (the convention: one branch per opened top item, shared by everything under it). Recorded only; the Workbench runs no git.")]
    public static async Task<string> BranchNote(WorkbenchOps ops, string session, string item, [Description("The branch name, for example wb/1-login-screen")] string branch, [Description("The checkout folder, if not your working directory")] string? checkout = null)
    {
        try { return await ops.BranchNoteAsync(session, item, branch, checkout); } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    /* ---------------------------------------------------------------- decisions */

    [McpServerTool(Name = "decision_record"), Description("The owner stated a rule or made a decision in chat: record it straight away, in their own words, one to three lines. Not every answer is a decision; a rule that would otherwise live only in the conversation is.")]
    public static async Task<string> DecisionRecord(WorkbenchOps ops, string session,
        [Description("The decision in the owner's words")] string text,
        [Description("The item it was said about; defaults to yours")] string? item = null,
        [Description("The project it applies to when it is wider than one item")] string? project = null)
    {
        try { var r = await ops.RecordDecisionAsync(session, text, item, project); return $"{r.Id} recorded"; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "rulings_search"), Description("Search the owner's decisions before proposing anything. Returns id and text.")]
    public static async Task<string> RulingsSearch(WorkbenchOps ops, [Description("Words to look for")] string query)
    {
        var q = query.ToLowerInvariant();
        var hits = await ops.Db.Rulings.Where(r => r.NeverReopened && (r.Text.ToLower().Contains(q) || r.Title.ToLower().Contains(q))).OrderByDescending(r => r.Date).Take(12).ToListAsync();
        return hits.Count == 0 ? "No decision mentions that." : string.Join("\n", hits.Select(r => $"{r.Id} · {r.Text}"));
    }

    /* ---------------------------------------------------------------- FPBs */

    [McpServerTool(Name = "fpb_raise"), Description("Raise an FTR, BUG or PER. An FTR when the owner asks for a feature or change in chat; a BUG or PER from your own checks, never for code you have not committed yet. Attach it to the item it came up in, or to the project. Title: the capability (FTR), the symptom as a causal sentence (BUG), the wasteful mechanism (PER). Search first: a duplicate is refused.")]
    public static async Task<string> FpbRaise(WorkbenchOps ops, string session,
        [Description("FTR, BUG or PER")] string kind,
        [Description("The title, one line")] string title,
        [Description("One paragraph: why it exists, for the owner")] string? why = null,
        [Description("Item id or project code to attach to; defaults to your item")] string? attach = null,
        [Description("Where it came from: chat, check, run, read (a BUG found by reading code, not run, starts as Verify first)")] string? source = null,
        [Description("Sections for the next session, keyed by name: FTR story/scope/design/acceptance/plan; BUG symptom/reproduce/evidence/cause/fixes/workaround/acceptance; PER headline/target/comparison/series/cause/fixes/guard. Each a string or an object with text")] Dictionary<string, System.Text.Json.JsonElement>? sections = null,
        [Description("Links to other items or FPBs: to, type (needs, split from, replaced by, carried by, found in, duplicate, related), reason")] List<LinkInput>? links = null,
        [Description("blocking, serious, minor or cosmetic")] string? severity = null)
    {
        try { var r = await ops.RaiseReportAsync(session, kind, title, attach, why, source, sections, links, severity); return $"{r.Id} raised · {r.State} · attached to {r.AttachItemId ?? r.ProjectId}"; }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "fpb_update"), Description("Move an FPB on: state (Open, Verify first, Designing, Designed, In progress, Done, Exists, Won't do), what is left or the ruling behind Won't do, which item took it on, more sections, links, a two-line note. The owner saying 'not a bug' or 'drop it' in chat is a Won't do with their words as the reason.")]
    public static async Task<string> FpbUpdate(WorkbenchOps ops, string session, [Description("FPB id, for example WB-BUG-003")] string fpb,
        string? state = null, [Description("What is left, what it waits on, or the reason")] string? left = null,
        [Description("The item that took it on, or empty to clear")] string? takenOn = null, string? title = null,
        Dictionary<string, System.Text.Json.JsonElement>? sections = null, List<LinkInput>? links = null, string? note = null)
    {
        try { var r = await ops.UpdateReportAsync(session, fpb, state, left, takenOn, title, sections, links, note); return $"{r.Id} · {r.State}" + (r.Left is null ? "" : $" · {r.Left}"); }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "fpb_get"), Description("One FPB with its state, links and sections.")]
    public static async Task<string> FpbGet(WorkbenchOps ops, [Description("FPB id")] string fpb)
    {
        try { return await ops.ReportTextAsync(fpb); } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "fpb_list"), Description("The FPBs on an item (and everything under it) or on a project, one line each. Read them before starting work in an area: a known bug is worked with or fixed, never rediscovered.")]
    public static async Task<string> FpbList(WorkbenchOps ops, [Description("Item id or project code; empty for all")] string? at = null, [Description("open (not done), or one state word")] string? state = null)
    {
        try { return await ops.ReportListAsync(at, state); } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "link_add"), Description("A typed link between items and FPBs: needs, split from, replaced by, carried by, found in, duplicate, related. Recorded; the owner removes one in chat.")]
    public static async Task<string> LinkAdd(WorkbenchOps ops, string session,
        [Description("From: an item id or FPB id")] string from, [Description("To: an item id or FPB id")] string to,
        [Description("needs, split from, replaced by, carried by, found in, duplicate or related")] string type, [Description("Why, one line")] string? reason = null)
    {
        try { var e = await ops.LinkAsync(session, from, to, type, reason); return e.Id; } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    /* ---------------------------------------------------------------- the three things that reach the owner */

    [McpServerTool(Name = "page_submit"), Description("A custom page for a change that has no designer: a concept, a design across several classes, a data change, a review. HTML the owner reads in the inbox and answers on. Every question sits beside its own answer control; sections start collapsed; no bare ids. The page posts answers with window.parent.postMessage({type:'wb-answer', key, value}, '*'). Wait with card_wait; the owner's Approve makes the answers the agreed design.")]
    public static async Task<string> PageSubmit(WorkbenchOps ops, string session,
        [Description("A title the owner recognises")] string title,
        [Description("The page body as HTML (a fragment or a full document)")] string html,
        [Description("One line: why this needs the owner")] string? purpose = null,
        [Description("The item it is about; defaults to yours")] string? item = null)
    {
        try { var c = await ops.SubmitPageAsync(session, item, title, html, purpose); return $"Page {c.Id} is in the owner's inbox. Wait with card_wait; read answers any time with page_answers."; }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "page_answers"), Description("What the owner has answered on a page so far, and whether it is approved.")]
    public static async Task<string> PageAnswers(WorkbenchOps ops, [Description("The page card id")] string card)
    {
        try { return await ops.PageAnswersAsync(card); } catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "deviation_report"), Description("You must leave the agreed design, or you did something the owner asked to be told about (a helper type, a static, a public surface change). Say what, why, what was agreed and what you would do instead, with options if there is a choice. Wait with card_wait.")]
    public static async Task<string> DeviationReport(WorkbenchOps ops, string session, string item,
        [Description("What deviates, one line")] string what, [Description("Why, up to three lines")] string why,
        [Description("What was agreed")] string? agreed = null, [Description("What you would do instead")] string? instead = null,
        [Description("Options the owner can pick from, if there is a choice")] List<string>? options = null)
    {
        try { var c = await ops.ReportDeviationAsync(session, item, what, why, agreed, instead, options); return $"Deviation card {c.Id} is in the owner's inbox. Wait with card_wait."; }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "stuck_report"), Description("A check failed three times on the same step: report what you tried and your diagnosis, then stop. The owner reassigns, allows one more try, or drops the step.")]
    public static async Task<string> StuckReport(WorkbenchOps ops, string session, string item, int step,
        [Description("The check that fails, for example tests · HookOrderingTests.TwoMachinesInterleave")] string checkName,
        [Description("Each attempt: what you tried, the result, the last lines of output")] List<AttemptInput> attempts, [Description("Your diagnosis in up to three lines")] string diagnosis)
    {
        try { var c = await ops.ReportStuckAsync(session, item, step, checkName, attempts, diagnosis); return $"Stuck card {c.Id} is in the owner's inbox. Stop now."; }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "card_wait"), Description("Wait for the owner to answer a card. Returns the answer (for a page: approved or the change asked, plus every answer on it), or 'no answer yet' after the timeout (max 600 s). Call again to keep waiting.")]
    public static async Task<string> CardWait(WorkbenchOps ops, [Description("Card id")] string card, [Description("Seconds to wait, up to 600")] int seconds = 300, CancellationToken ct = default)
    {
        try { return await ops.WaitAnswerAsync(card, TimeSpan.FromSeconds(Math.Clamp(seconds, 5, 600)), ct) ?? "no answer yet"; }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    /* ---------------------------------------------------------------- checks and code */

    [McpServerTool(Name = "result_record"), Description("Record the result of a check you ran that the hooks do not see (specimens, a smoke run, a benchmark). dotnet test and build are recorded by the hooks on their own. Results are kept per project, the last one with the one before it.")]
    public static async Task<string> ResultRecord(WorkbenchOps ops, string session,
        [Description("Check name: specimens, round-trip, smoke, bench, or your own")] string check,
        [Description("pass, fail or warn")] string result, [Description("The exact command")] string cmd,
        int passed = 0, int total = 0, double durationSec = 0, [Description("Project code; defaults to your item's")] string? project = null)
    {
        try { var r = await ops.RecordResultAsync(new ResultReport(check, project, null, session, result, passed, total, durationSec, cmd)); return $"{r.Check} {r.Result}" + (r.Prev is null ? "" : $" (was {r.Prev.Result})"); }
        catch (WorkbenchRuleException ex) { return Err(ex); }
    }

    [McpServerTool(Name = "code_pull"), Description("Pull up to 20 lines of a file from the session's checkout, to show in a page. Code is pulled, never pasted from memory.")]
    public static async Task<string> CodePull(WorkbenchOps ops, IHostClient host, string session, [Description("Path, repo-relative or absolute")] string file, int from, int to)
    {
        var s = await ops.Db.Sessions.FindAsync(session);
        if (s is null) return "REFUSED (session.unknown): call session_hello first.";
        if (to - from + 1 > Caps.PulledCodeLines) to = from + Caps.PulledCodeLines - 1;
        var text = await host.ReadLinesAsync(s.MachineId ?? ops.Options.MachineId, s.Cwd ?? "", file, from, to);
        return text ?? "The Host could not read that file.";
    }
}
