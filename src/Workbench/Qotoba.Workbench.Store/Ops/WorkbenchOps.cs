using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store.Ops;

/// <summary>
/// Everything that changes the store goes through here: hooks, MCP tools, owner presses and the Host.
/// One instance per request. Rules are enforced before anything is written.
/// </summary>
public sealed partial class WorkbenchOps(WorkbenchDb db, IClock clock, IChangeNotifier notify, IHostClient host, OpsOptions options, Fmt fmt)
{
    public WorkbenchDb Db => db;
    public Fmt Fmt => fmt;
    public OpsOptions Options => options;
    private DateTimeOffset Now => clock.Now;

    private static readonly Regex ItemIdRx = new(@"\b([A-Z]{2,5}-\d+(?:\.\d+)*)\b", RegexOptions.Compiled);

    private async Task SaveAsync(string kind, string id, CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
        notify.Changed(kind, id);
    }

    private static List<T> List<T>(string? json) => Json.ReadList<T>(json);

    private void Audit(Actor actor, string verb, string kind, string id, string? detail = null) => db.Log(actor, verb, kind, id, detail);

    private async Task HistoryAsync(string itemId, string evt, string text, CancellationToken ct = default)
    {
        var it = await db.Items.FindAsync([itemId], ct);
        if (it is null) return;
        var h = List<HistoryLine>(it.HistoryJson);
        h.Add(new HistoryLine(fmt.DayTime(Now), evt, text));
        it.HistoryJson = Json.Write(h);
    }

    private async Task LedgerAsync(Session s, string kind, string text)
    {
        var l = List<LedgerLine>(s.LedgerJson);
        l.Add(new LedgerLine(fmt.Time(Now), kind, text));
        if (l.Count > 400) l.RemoveRange(0, l.Count - 400);
        s.LedgerJson = Json.Write(l);
        s.LastEvent = Now;
        await Task.CompletedTask;
    }

    private async Task<Session> RequireSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var s = await db.Sessions.FindAsync([sessionId], ct)
            ?? throw new WorkbenchRuleException("session.unknown", $"Session {sessionId} is not known. Call session_hello first with the id from your SessionStart context.");
        if (!s.Hello) throw new WorkbenchRuleException("session.hello", "Call session_hello first.");
        return s;
    }

    private async Task<Item> RequireItemAsync(string itemId, CancellationToken ct = default) =>
        await db.Items.FindAsync([itemId], ct) ?? throw new WorkbenchRuleException("item.unknown", $"There is no item {itemId}. Only the owner creates items; ask on a card if one is missing.");

    private async Task<Card> RequireCardAsync(string cardId, CancellationToken ct = default) =>
        await db.Cards.FindAsync([cardId], ct) ?? throw new WorkbenchRuleException("card.unknown", $"There is no card {cardId}.");

    private async Task<Project> ProjectOfAsync(Item it, CancellationToken ct = default) =>
        await db.Projects.FindAsync([it.ProjectId], ct) ?? throw new WorkbenchRuleException("project.unknown", "The item's project is missing.");

    /// <summary>Minutes of estimated work left on an item from a step onward, at the lane or project factor.</summary>
    private async Task<int> WorkBehindAsync(Item it, int? fromStep, CancellationToken ct = default)
    {
        var steps = List<Step>(it.StepsJson);
        var est = steps.Where(s => fromStep is null || s.N >= fromStep).Where(s => s.State != StepState.Done).Sum(s => s.EstMin);
        if (est == 0 && it.EstMin is int e) est = Math.Max(0, e - it.SoMin);
        var factor = it.Factor ?? (await ProjectOfAsync(it, ct)).Factor ?? 1.2;
        return (int)Math.Round(est * factor);
    }

    private static string StepLabel(Item it, int? n)
    {
        var steps = List<Step>(it.StepsJson);
        return n is null ? "" : $"step {n} of {Math.Max(steps.Count, n.Value)}";
    }
}
