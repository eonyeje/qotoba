namespace Qotoba.Workbench.Model;

/// <summary>A top-level project. Only the owner creates one.</summary>
public class Project
{
    public required string Id { get; set; }          // "wb"
    public required string Code { get; set; }        // "WB"
    public required string Name { get; set; }
    public string Color { get; set; } = "wb";
    public ProjectState State { get; set; } = ProjectState.Coarse;
    public int CoarseWeeks { get; set; }
    public double? Factor { get; set; }
    public string Purpose { get; set; } = "";
    public string? Repo { get; set; }
    /// <summary>The owner opened the project itself, to work on it as a whole. It then holds the branch, and every item under it shares that branch while the project stays open.</summary>
    public bool IsOpen { get; set; }
    /// <summary>The branch of a project opened as a whole, once Claude or the Host has made it. Recorded, never run.</summary>
    public string? Branch { get; set; }
    public string? Checkout { get; set; }
    public string? HomeMachineId { get; set; }
    public string? ClosedWhy { get; set; }
    public int Order { get; set; }
    public int NextItemNumber { get; set; } = 1;
    /// <summary>The last result of each check (build, tests, a named suite) with the one before it, so the next run can be compared. Tests run per project, never per item.</summary>
    public string ResultsJson { get; set; } = "[]";
    public DateTimeOffset Created { get; set; }
}

/// <summary>The last result of one check on a project, and the one before it.</summary>
public sealed record CheckResult(string Check, string Result, int Passed, int Total, double DurationSec, string At, string Cmd, string? SessionId, string? ItemId, CheckResult? Prev);

/// <summary>An item of work in the owner's tree: a unit of development work at any depth. Shaped by the owner, written by Claude when the owner dictates it in chat.</summary>
public class Item
{
    public required string Id { get; set; }          // "WB-2.2"
    public required string ProjectId { get; set; }
    public string? ParentId { get; set; }
    public int Order { get; set; }
    public required string Title { get; set; }
    public ItemState State { get; set; } = ItemState.Coarse;
    public bool IsOpen { get; set; }
    public string? HomeMachineId { get; set; }
    /// <summary>The git branch of an opened top-level item (its children share it). A convention Claude keeps itself; the Workbench only records the name.</summary>
    public string? Branch { get; set; }
    /// <summary>Where that branch is checked out on the home machine, when known.</summary>
    public string? Checkout { get; set; }
    public string CoarseSize { get; set; } = "~1 week";
    public string Goal { get; set; } = "";
    /// <summary>Claude's own steps for the item, written and updated by its sessions. They give the item its progress.</summary>
    public string StepsJson { get; set; } = "[]";
    public int SoMin { get; set; }
    public int? EstMin { get; set; }
    public double? Factor { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset? NoteAt { get; set; }
    public string? NoteSessionId { get; set; }
    /// <summary>One sentence from the session that finished it: what is now true.</summary>
    public string? Outcome { get; set; }
    public string HistoryJson { get; set; } = "[]";
    public string DecisionsJson { get; set; } = "[]";
    public bool Preview { get; set; }
    public DateTimeOffset Created { get; set; }
}

/// <summary>A decision settled on this item, with where it was settled.</summary>
public sealed record Decision(string T, string A, string How, string? Ref, string? From, string Date, string? Ruling);

public sealed record Step(int N, string Text, int EstMin, StepState State, string? ProofRef, List<string>? Needs, string? Note, string? ProofText = null);
public sealed record HistoryLine(string Date, string Event, string Text);
