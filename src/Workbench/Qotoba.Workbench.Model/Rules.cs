namespace Qotoba.Workbench.Model;

/// <summary>
/// The rules that keep the Workbench quiet. Each method throws <see cref="WorkbenchRuleException"/> with a stable code.
/// They are pure: callers pass the counts they already know.
/// </summary>
public static class Rules
{
    public static void QuestionMayBeAsked(int openQuestionsOnItem)
    {
        if (openQuestionsOnItem >= Caps.OpenQuestionsPerItem)
            throw new WorkbenchRuleException("questions.cap",
                $"This item already has {Caps.OpenQuestionsPerItem} open questions. A third is refused until one is answered.");
    }

    public static void QuestionIsClosed(IReadOnlyList<QuestionOption> options)
    {
        if (options.Count < Caps.QuestionOptionsMin || options.Count > Caps.QuestionOptionsMax)
            throw new WorkbenchRuleException("questions.options",
                $"A question carries {Caps.QuestionOptionsMin} to {Caps.QuestionOptionsMax} options, each with a reason.");
        if (options.Count(o => o.Rec) != 1)
            throw new WorkbenchRuleException("questions.recommendation", "Exactly one option is recommended.");
        if (options.Any(o => string.IsNullOrWhiteSpace(o.Why)))
            throw new WorkbenchRuleException("questions.why", "Every option says why.");
    }

    public static void QuestionIsAnchored(string? itemId, int? stepN)
    {
        if (string.IsNullOrWhiteSpace(itemId) || stepN is null or < 1)
            throw new WorkbenchRuleException("questions.anchor", "A question is born from a task step. Name the item and the step it blocks.");
    }

    public static void PlanFits(IReadOnlyList<Step> steps)
    {
        if (steps.Count == 0) throw new WorkbenchRuleException("plan.empty", "A plan has at least one step.");
        if (steps.Count > Caps.PlanSteps)
            throw new WorkbenchRuleException("plan.cap", $"A plan has at most {Caps.PlanSteps} steps. Split the item instead.");
        if (steps.Any(s => s.EstMin <= 0))
            throw new WorkbenchRuleException("plan.estimate", "Every step carries an estimate in Claude-minutes.");
    }

    public static void FoundMayBeAdded(int foundThisSession)
    {
        if (foundThisSession >= Caps.FoundPerSession)
            throw new WorkbenchRuleException("found.cap",
                $"This session has already found {Caps.FoundPerSession} things. Put the rest in the packet note.");
    }

    public static void NoteFits(string text)
    {
        if (Json.LineCount(text) > Caps.NoteLines)
            throw new WorkbenchRuleException("note.cap", $"A note is at most {Caps.NoteLines} lines. Anything longer belongs in a question or the packet.");
    }

    public static void TextFits(string text, string what)
    {
        if (Json.LineCount(text) > Caps.TextLines)
            throw new WorkbenchRuleException("text.cap", $"{what} is at most {Caps.TextLines} lines.");
    }

    public static void OnlyOwnerCreatesStructure(Actor actor, string what)
    {
        if (actor != Actor.Owner)
            throw new WorkbenchRuleException("structure.owner", $"Only the owner creates a {what}. Claude never creates structure.");
    }

    public static void OneHomeMachine(string? currentHome, string requestedMachine)
    {
        if (currentHome is not null && currentHome != requestedMachine)
            throw new WorkbenchRuleException("machine.home",
                $"This task's home machine is {currentHome}. Reassign it from the Stuck card before working on {requestedMachine}.");
    }

    /// <summary>One open node per level: a sibling may open only when no sibling is already open.</summary>
    public static void MayOpen(Item node, IEnumerable<Item> siblings)
    {
        if (node.Preview) throw new WorkbenchRuleException("open.preview", "A preview node is not real yet.");
        if (node.IsOpen) throw new WorkbenchRuleException("open.already", "This node is already open.");
        if (node.State == ItemState.Done) throw new WorkbenchRuleException("open.done", "This node is done. Add a BUG under it instead.");
        if (node.State == ItemState.Review) throw new WorkbenchRuleException("open.review", "This node is in review. Approve or change its packet first.");
        var clash = siblings.FirstOrDefault(s => s.Id != node.Id && s.IsOpen);
        if (clash is not null)
            throw new WorkbenchRuleException("open.one", $"One open node per level: {clash.Title} is already open.");
    }

    public static void ReportNeverWaits(Card card)
    {
        if (card.ReportId is not null && card.ItemId is null && card.Type != CardType.Info)
            throw new WorkbenchRuleException("report.waits", "A report never waits on the owner. Decisions come from the task that takes it on.");
    }
}
