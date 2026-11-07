namespace Qotoba.Workbench.Model;

/// <summary>
/// The few shape checks that stop a session flooding the Workbench. None of them ever refuses the owner.
/// Each throws <see cref="WorkbenchRuleException"/> with a stable code.
/// </summary>
public static class Rules
{
    public static void NoteFits(string text)
    {
        if (Json.LineCount(text) > Caps.NoteLines)
            throw new WorkbenchRuleException("note.cap", $"A note is at most {Caps.NoteLines} lines. Anything longer belongs on the item's page.");
    }

    public static void TextFits(string text, string what)
    {
        if (Json.LineCount(text) > Caps.TextLines)
            throw new WorkbenchRuleException("text.cap", $"{what} is at most {Caps.TextLines} lines.");
    }

    public static void OnlyOwnerCreatesProjects(Actor actor)
    {
        if (actor != Actor.Owner)
            throw new WorkbenchRuleException("structure.owner", "Only the owner creates a project.");
    }

    public static void StepsHaveEstimates(IReadOnlyList<Step> steps)
    {
        if (steps.Count == 0) throw new WorkbenchRuleException("steps.empty", "Write at least one step.");
        if (steps.Any(s => s.EstMin <= 0)) throw new WorkbenchRuleException("steps.estimate", "Every step carries an estimate in Claude-minutes.");
    }

    public static void OneHomeMachine(string? currentHome, string requestedMachine)
    {
        if (currentHome is not null && currentHome != requestedMachine)
            throw new WorkbenchRuleException("machine.home",
                $"This item's home machine is {currentHome}. Reassign it from the Stuck card before working on {requestedMachine}.");
    }

    /// <summary>
    /// One open node per level is how the owner works, not a lock: when a sibling is already open, opening another takes
    /// the owner to the one that is open. Returns that sibling, or null when the node may open.
    /// </summary>
    public static Item? OpenClash(Item node, IEnumerable<Item> siblings)
    {
        if (node.Preview) throw new WorkbenchRuleException("open.preview", "A preview node is not real yet.");
        if (node.State == ItemState.Done) throw new WorkbenchRuleException("open.done", "This node is done.");
        return siblings.FirstOrDefault(s => s.Id != node.Id && s.IsOpen);
    }
}
