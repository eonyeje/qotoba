using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>
/// FTR, BUG and PER reports. Information only: nothing here ever waits on the owner. The "When implementing" lines are
/// the sole source a task's plan draws decisions from; a line moves only when a plan, a card, a run or a packet moves it.
/// </summary>
public sealed partial class WorkbenchOps
{
    private static readonly Dictionary<ReportKind, string[]> SectionKeys = new()
    {
        [ReportKind.FTR] = ["goal", "story", "scope", "design", "acceptance", "parity", "risks", "impact", "plan", "note"],
        [ReportKind.BUG] = ["symptom", "reproduce", "evidence", "environment", "frequency", "cause", "fixes", "workaround", "acceptance", "note"],
        [ReportKind.PER] = ["headline", "target", "comparison", "series", "profile", "resources", "cause", "fixes", "guard", "environment", "note"]
    };
    private static readonly Dictionary<ReportKind, string> FirstState = new() { [ReportKind.FTR] = "idea", [ReportKind.BUG] = "reported", [ReportKind.PER] = "observed" };
    private static readonly HashSet<string> OwnerSections = ["goal", "story", "scope", "acceptance", "symptom", "target"];

    /* ---------------------------------------------------------------- owner creates a report */

    public async Task<Report> AddReportAsync(AddItemRequest req, Actor actor, CancellationToken ct = default)
    {
        Rules.OnlyOwnerCreatesStructure(actor, "report");
        if (!Enum.TryParse<ReportKind>(req.Kind, true, out var kind)) throw new WorkbenchRuleException("report.kind", "Kind is FTR, BUG or PER.");
        if (string.IsNullOrWhiteSpace(req.Title)) throw new WorkbenchRuleException("report.title", "A title, in your words.");
        Item? attach = null; Project? project = null;
        if (req.Place is { Length: > 0 } place)
        {
            attach = await db.Items.FindAsync([place], ct);
            project = attach is null ? await db.Projects.FindAsync([place], ct) : await ProjectOfAsync(attach, ct);
        }
        project ??= req.Project is null ? null : await db.Projects.FindAsync([req.Project], ct);
        project ??= await db.Projects.OrderBy(p => p.Order).FirstAsync(ct);

        var n = await db.Reports.CountAsync(r => r.Kind == kind, ct) + 1;
        while (await db.Reports.AnyAsync(r => r.Id == Ids.Report(kind, n), ct)) n++;
        var r = new Report
        {
            Id = Ids.Report(kind, n), Kind = kind, Title = req.Title.Trim(), ProjectId = project.Id, AttachItemId = attach?.Id,
            State = FirstState[kind], Subtype = req.Subtype, Created = Now, SourceType = req.FromFound is null ? "created" : "promoted",
            SeverityBy = Author.Claude
        };
        var sections = new JsonObject();
        if (req.FromFound is not null && await db.Founds.FindAsync([req.FromFound], ct) is { } f)
        {
            f.State = FoundState.Promoted; f.PromotedTo = "fpb:" + r.Id;
            r.SourceFoundId = f.Id; r.SourceSessionId = f.SessionId;
            if (attach is null && f.ItemId is not null) r.AttachItemId = f.ItemId;
            var key = kind == ReportKind.BUG ? "symptom" : kind == ReportKind.PER ? "headline" : "story";
            sections[key] = new JsonObject { ["author"] = "claude", ["session"] = f.SessionId, ["text"] = f.Why };
            if (f.EvidenceRef is not null && kind == ReportKind.BUG) sections["evidence"] = new JsonObject { ["author"] = "machine", ["run"] = f.EvidenceRef, ["lines"] = 0 };
        }
        r.SectionsJson = sections.ToJsonString();
        r.HistoryJson = Json.Write(new List<object[]> { new object[] { fmt.DayTime(Now), "you", req.FromFound is null ? "You created the report" : $"You promoted it from the found tray ([[found:{req.FromFound}]])" } });
        db.Reports.Add(r);
        Audit(Actor.Owner, "add", "report", r.Id, r.Title);
        await SaveAsync("report", r.Id, ct);
        return r;
    }

    public async Task<Report> PatchReportAsync(string id, ReportPatch patch, CancellationToken ct = default)
    {
        var r = await db.Reports.FindAsync([id], ct) ?? throw new WorkbenchRuleException("report.unknown", $"There is no report {id}.");
        var h = Json.Read<List<object[]>>(r.HistoryJson, []);
        if (patch.Title is { Length: > 0 } t) { r.Title = t.Trim(); h.Add([fmt.DayTime(Now), "you", "You renamed the report"]); }
        if (patch.Severity is { Length: > 0 } s && Enum.TryParse<SeverityLevel>(s, true, out var lvl)) { r.Severity = lvl; r.SeverityBy = Author.Me; h.Add([fmt.DayTime(Now), "you", $"You set the severity to {s.ToLowerInvariant()}"]); }
        if (patch.Subtype is { Length: > 0 } st) r.Subtype = st;
        if (patch.State is { Length: > 0 } state) { r.State = state; h.Add([fmt.DayTime(Now), "you", $"You set the state to {state}"]); }
        if (patch.ChosenFix is int fx) { r.ChosenFix = fx; h.Add([fmt.DayTime(Now), "you", $"You chose fix {fx + 1} as the recommended one"]); await SetSectionField(r, "fixes", "chosen", fx); }
        if (patch.Target is { } tg)
        {
            r.TargetJson = Json.Write(tg);
            var sections = JsonNode.Parse(r.SectionsJson)?.AsObject() ?? new JsonObject();
            sections["target"] = new JsonObject { ["author"] = "me", ["v"] = tg.V, ["unit"] = tg.Unit, ["text"] = tg.Text, ["by"] = "you", ["date"] = fmt.Day(Now) };
            r.SectionsJson = sections.ToJsonString();
            h.Add([fmt.DayTime(Now), "you", $"You set the target: {tg.V} {tg.Unit}"]);
        }
        if (patch.AttachItemId is not null) r.AttachItemId = patch.AttachItemId == "" ? null : patch.AttachItemId;
        r.HistoryJson = Json.Write(h);
        Audit(Actor.Owner, "patch", "report", id);
        await SaveAsync("report", id, ct);
        return r;
    }

    private Task SetSectionField(Report r, string section, string field, object value)
    {
        var sections = JsonNode.Parse(r.SectionsJson)?.AsObject() ?? new JsonObject();
        if (sections[section] is JsonObject o) { o[field] = JsonValue.Create(value); r.SectionsJson = sections.ToJsonString(); }
        return Task.CompletedTask;
    }

    /* ---------------------------------------------------------------- Claude writes sections and lines */

    public async Task<Report> WriteReportAsync(string sessionId, string reportId, Dictionary<string, JsonElement>? sections, List<ImplLineInput>? lines, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var r = await db.Reports.FindAsync([reportId], ct) ?? throw new WorkbenchRuleException("report.unknown", $"There is no report {reportId}. Only the owner creates reports; use found_add to suggest one.");
        var allowed = SectionKeys[r.Kind];
        var store = JsonNode.Parse(r.SectionsJson)?.AsObject() ?? new JsonObject();
        var written = new List<string>();
        foreach (var (key, value) in sections ?? [])
        {
            if (!allowed.Contains(key)) throw new WorkbenchRuleException("report.section", $"A {r.Kind} report has no section '{key}'. Sections: {string.Join(", ", allowed)}.");
            if (value.ValueKind != JsonValueKind.Object) throw new WorkbenchRuleException("report.section", $"Section '{key}' is an object.");
            var node = JsonNode.Parse(value.GetRawText())!.AsObject();
            if (key == "fixes" && node["rows"] is JsonArray rows && rows.Count > Caps.Fixes) throw new WorkbenchRuleException("report.fixes", $"At most {Caps.Fixes} possible fixes; drop the weakest.");
            if (key == "note" && node["text"]?.GetValue<string>() is { } nt) Rules.NoteFits(nt);
            if (node["text"]?.GetValue<string>() is { } tx && key != "note") Rules.TextFits(tx, $"The {key} section");
            if (store[key] is JsonObject old && old["author"]?.GetValue<string>() == "me" && OwnerSections.Contains(key))
                throw new WorkbenchRuleException("report.owner", $"The owner wrote the {key} section; it is theirs. Put your view in the note or a found item.");
            node["author"] = key is "evidence" or "environment" or "frequency" or "headline" or "comparison" or "series" or "profile" or "resources" ? "machine" : "claude";
            node["session"] = s.Id;
            store[key] = node;
            written.Add(key);
        }
        r.SectionsJson = store.ToJsonString();

        if (lines is { Count: > 0 })
        {
            var existing = await db.ImplLines.Where(l => l.ReportId == r.Id).ToListAsync(ct);
            var open = existing.Where(l => l.Status == ImplStatus.Open).ToList();
            db.ImplLines.RemoveRange(open);
            var order = existing.Count(l => l.Status != ImplStatus.Open);
            foreach (var li in lines)
            {
                if (!Enum.TryParse<ImplKind>(li.Kind, true, out var kind)) throw new WorkbenchRuleException("impl.kind", "A line is a decision, proof, dependency or precondition.");
                Rules.TextFits(li.Text, "A When implementing line");
                if (kind == ImplKind.Decision && (li.Options is null || li.Options.Count < 2)) throw new WorkbenchRuleException("impl.options", "A decision line carries at least two options, one recommended, each with a reason and a cost.");
                if (kind == ImplKind.Dependency && (li.Rec is null || !li.Rec.Contains(':'))) throw new WorkbenchRuleException("impl.dependency", "A dependency line names its target in rec as item:<id> or fpb:<id>.");
                var opts = (li.Options ?? []).Select(o => new QuestionOption(o.Text, o.Recommended, o.Why, o.CostToChange, o.Support)).ToList();
                var rec = li.Rec ?? opts.FirstOrDefault(o => o.Rec)?.T;
                var cost = li.Cost ?? opts.FirstOrDefault(o => o.Rec)?.Cost;
                db.ImplLines.Add(new ImplLine
                {
                    Id = $"{r.Id}-L{r.NextLine++}", ReportId = r.Id, Order = ++order, Kind = kind, Text = li.Text.Trim(),
                    Rec = kind == ImplKind.Dependency ? null : rec, Ref = kind == ImplKind.Dependency ? NormaliseRef(li.Rec!) : null,
                    Why = li.Why ?? opts.FirstOrDefault(o => o.Rec)?.Why, Cost = cost, OptionsJson = Json.Write(opts), Status = ImplStatus.Open
                });
            }
        }
        var h = Json.Read<List<object[]>>(r.HistoryJson, []);
        h.Add([fmt.DayTime(Now), "session:" + s.Id, written.Count > 0 ? "Wrote " + string.Join(", ", written) + (lines is { Count: > 0 } ? $" and {lines.Count} When implementing lines" : "") : $"Wrote {lines?.Count ?? 0} When implementing lines"]);
        r.HistoryJson = Json.Write(h);
        await LedgerAsync(s, "report", $"{r.Id}: {string.Join(", ", written)}");
        Audit(Actor.Claude, "write", "report", r.Id, string.Join(",", written));
        await SaveAsync("report", r.Id, ct);
        return r;
    }

    public async Task ProveLineAsync(string sessionId, string lineId, string runId, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var l = await db.ImplLines.FindAsync([lineId], ct) ?? throw new WorkbenchRuleException("impl.unknown", $"There is no line {lineId}.");
        var id = runId.Replace("run:", "");
        var run = await db.Runs.FindAsync([id], ct) ?? throw new WorkbenchRuleException("run.unknown", $"No run {id} is recorded. A line is proved by a run the Workbench saw.");
        if (run.Result == RunResult.Fail) throw new WorkbenchRuleException("impl.failed", $"Run {id} failed; it proves nothing.");
        l.Status = ImplStatus.Proved; l.Ref = "run:" + id; l.Date = Now; l.Note = null; l.ItemId ??= s.ItemId;
        await MarkAcceptanceProvedAsync(l.ReportId, l.Text, "run:" + id, ct);
        await LedgerAsync(s, "prove", $"{l.ReportId} · {Trim(l.Text, 50)} · run {id}");
        await AdvanceReportStateAsync(l.ReportId, ct);
        await SaveAsync("report", l.ReportId, ct);
    }

    public async Task DropLineAsync(string sessionId, string lineId, string why, CancellationToken ct = default)
    {
        var s = await RequireSessionAsync(sessionId, ct);
        var l = await db.ImplLines.FindAsync([lineId], ct) ?? throw new WorkbenchRuleException("impl.unknown", $"There is no line {lineId}.");
        if (l.Status is ImplStatus.Settled or ImplStatus.Proved) throw new WorkbenchRuleException("impl.final", "A settled or proved line is not dropped by a session.");
        l.Status = ImplStatus.Dropped; l.Note = "Dropped: " + why.Trim(); l.Date = Now;
        await LedgerAsync(s, "drop", $"{l.ReportId} · {Trim(l.Text, 50)}");
        await SaveAsync("report", l.ReportId, ct);
    }

    /* ---------------------------------------------------------------- how lines move with the task */

    /// <summary>Called when a plan is submitted: the taken-on reports' open lines become settle defaults or wait for a card.</summary>
    private async Task<List<SettleQ>> DrawSettleFromReportsAsync(Item it, int startIndex, CancellationToken ct)
    {
        var settle = new List<SettleQ>();
        var reports = Json.ReadList<string>(it.ImplementsJson).Select(r => r.Replace("fpb:", "")).ToList();
        if (reports.Count == 0) return settle;
        var lines = await db.ImplLines.Where(l => reports.Contains(l.ReportId) && l.Status == ImplStatus.Open).OrderBy(l => l.ReportId).ThenBy(l => l.Order).ToListAsync(ct);
        var n = startIndex;
        foreach (var l in lines)
        {
            l.ItemId = it.Id;
            var from = "fpb:" + l.ReportId;
            switch (l.Kind)
            {
                case ImplKind.Decision:
                {
                    var opts = Json.ReadList<QuestionOption>(l.OptionsJson);
                    var high = (l.Cost ?? "").StartsWith("high", StringComparison.OrdinalIgnoreCase) || (l.Cost ?? "").StartsWith("large", StringComparison.OrdinalIgnoreCase);
                    if (high) { l.Status = ImplStatus.Scheduled; l.Note = $"Will be asked on a Question Card when {it.Title} reaches its step"; break; }
                    var texts = opts.Count >= 2 ? opts.Select(o => o.T).ToList() : [l.Rec ?? "Yes", "No"];
                    var rec = Math.Max(0, opts.FindIndex(o => o.Rec));
                    settle.Add(new SettleQ("s" + (++n), l.Text, texts, rec, from, null, l.Id));
                    l.Status = ImplStatus.Scheduled; l.Note = $"Will be a default in the settle pass of {it.Title}";
                    break;
                }
                case ImplKind.Dependency:
                    settle.Add(new SettleQ("s" + (++n), l.Text, ["Accept", "Remove"], 0, from, null, l.Id));
                    l.Status = ImplStatus.Scheduled; l.Note = $"Will be confirmed with the plan of {it.Title}";
                    break;
                case ImplKind.Precondition:
                    settle.Add(new SettleQ("s" + (++n), l.Text, ["In place", "Not yet"], 0, from, null, l.Id));
                    l.Status = ImplStatus.Scheduled; l.Note = $"Will be confirmed with the plan of {it.Title}";
                    break;
                case ImplKind.Proof:
                    l.Status = ImplStatus.Scheduled; l.Note = $"Will be proved by a run of {it.Title}";
                    break;
            }
            l.Date = Now;
        }
        return settle;
    }

    /// <summary>Called when a plan is approved: every settle entry drawn from a line settles it, with the plan card as its reference.</summary>
    private async Task SettleLinesAsync(Item it, Card planCard, List<SettleQ> settle, CancellationToken ct)
    {
        foreach (var q in settle.Where(q => q.Line is not null))
        {
            var l = await db.ImplLines.FindAsync([q.Line!], ct);
            if (l is null) continue;
            var choice = q.Chosen is int c && c >= 0 && c < q.Options.Count ? q.Options[c] : q.Options[q.Rec];
            var target = l.Ref ?? l.Rec ?? "";
            l.Date = Now; l.Ref = "card:" + planCard.Id; l.Via = "settle";
            if (l.Kind == ImplKind.Dependency)
            {
                if (choice == "Accept" && target.Contains(':'))
                {
                    if (!await db.Edges.AnyAsync(e => e.From == "item:" + it.Id && e.To == target, ct))
                        db.Edges.Add(new Edge { Id = "e-" + Ids.Short(), From = "item:" + it.Id, To = target, Type = EdgeType.Needs, Reason = l.Why ?? l.Text, By = Actor.Claude, Confirmed = true, SessionId = planCard.SessionId });
                    l.Status = ImplStatus.Settled; l.Note = $"Accepted with the plan for {it.Id}";
                }
                else { l.Status = ImplStatus.Dropped; l.Note = "Removed by the owner in the settle pass"; }
            }
            else if (l.Kind == ImplKind.Precondition)
            {
                if (choice == "In place") { l.Status = ImplStatus.Settled; l.Note = $"Confirmed by the owner with the plan for {it.Id}"; }
                else { l.Status = ImplStatus.Scheduled; l.Note = "Not yet in place; the plan starts without it"; }
            }
            else
            {
                l.Status = ImplStatus.Settled; l.Note = $"{Trim(choice, 60)} · plan for {it.Id}";
                await RememberChosenFixAsync(l.ReportId, choice, ct);
            }
        }
        await AdvanceReportsOfAsync(it, ct);
    }

    /// <summary>A settled "which fix" decision marks the chosen row in the report's Possible fixes, so the hero says "Fix 2 settled".</summary>
    private async Task RememberChosenFixAsync(string reportId, string choice, CancellationToken ct)
    {
        var m = System.Text.RegularExpressions.Regex.Match(choice, @"\bFix\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success) return;
        var r = await db.Reports.FindAsync([reportId], ct);
        if (r is null) return;
        r.ChosenFix = int.Parse(m.Groups[1].Value) - 1;
        await SetSectionField(r, "fixes", "chosen", r.ChosenFix.Value);
    }

    /// <summary>A proved line whose text matches an acceptance line marks that line as proved by the run.</summary>
    private async Task MarkAcceptanceProvedAsync(string reportId, string lineText, string runRef, CancellationToken ct)
    {
        var r = await db.Reports.FindAsync([reportId], ct);
        if (r is null) return;
        var sections = JsonNode.Parse(r.SectionsJson)?.AsObject();
        if (sections?["acceptance"] is not JsonObject acc || acc["lines"] is not JsonArray lines) return;
        var hit = false;
        foreach (var n in lines.OfType<JsonObject>())
        {
            var t = n["t"]?.GetValue<string>() ?? "";
            if (string.Equals(t.Trim(), lineText.Trim(), StringComparison.OrdinalIgnoreCase) || lineText.Contains(t, StringComparison.OrdinalIgnoreCase)) { n["run"] = runRef; hit = true; }
        }
        if (hit) r.SectionsJson = sections.ToJsonString();
    }

    /// <summary>Called when a question card that cites a line is asked or answered.</summary>
    private async Task LineAskedAsync(string lineId, Card card, Question q, CancellationToken ct)
    {
        var l = await db.ImplLines.FindAsync([lineId], ct) ?? throw new WorkbenchRuleException("impl.unknown", $"There is no line {lineId}.");
        l.Status = ImplStatus.Asked; l.Ref = "card:" + card.Id; l.QuestionId = q.Id; l.Date = Now; l.Note = $"Asked on {fmt.DayTime(Now)}"; l.ItemId ??= q.ItemId;
        q.ReportId = l.ReportId; card.ReportId = l.ReportId;
    }

    private async Task LineAnsweredAsync(Question q, string answer, CancellationToken ct)
    {
        var l = await db.ImplLines.FirstOrDefaultAsync(x => x.QuestionId == q.Id, ct);
        if (l is null) return;
        l.Status = ImplStatus.Settled; l.Date = Now; l.Via = "question";
        l.Note = $"Answered on a question card at step {q.StepN} of {q.ItemId}: {Trim(answer, 60)}";
        if (q.RulingId is not null) l.Ref = "ruling:" + q.RulingId;
        await AdvanceReportStateAsync(l.ReportId, ct);
    }

    /// <summary>Report state follows its takers: planned when a plan is approved, done when every taker's packet is approved, verified when every proof is proved.</summary>
    private async Task AdvanceReportsOfAsync(Item it, CancellationToken ct)
    {
        foreach (var rid in Json.ReadList<string>(it.ImplementsJson).Select(r => r.Replace("fpb:", "")))
            await AdvanceReportStateAsync(rid, ct);
    }

    private async Task AdvanceReportStateAsync(string reportId, CancellationToken ct)
    {
        var r = await db.Reports.FindAsync([reportId], ct);
        if (r is null) return;
        var takers = await db.Items.Where(i => i.ImplementsJson.Contains("fpb:" + reportId)).ToListAsync(ct);
        if (takers.Count == 0) return;
        var lines = await db.ImplLines.Where(l => l.ReportId == reportId).ToListAsync(ct);
        var allDone = takers.All(t => t.State == ItemState.Done);
        var proofs = lines.Where(l => l.Kind == ImplKind.Proof).ToList();
        var allProved = proofs.Count > 0 && proofs.All(p => p.Status == ImplStatus.Proved);
        var building = takers.Any(t => t.State is ItemState.Building or ItemState.Blocked or ItemState.Review);
        r.State = r.Kind switch
        {
            ReportKind.BUG => allDone && allProved ? "verified" : allDone ? "fixed" : building ? "fixing" : lines.Any(l => l.Status != ImplStatus.Open) ? "located" : r.State,
            ReportKind.PER => allDone && allProved ? "guarded" : allDone ? "measured" : building ? "improving" : lines.Any(l => l.Status != ImplStatus.Open) ? "planned" : r.State,
            _ => allDone ? "done" : building ? "building" : lines.Any(l => l.Status != ImplStatus.Open) ? "planned" : r.State
        };
    }
}
