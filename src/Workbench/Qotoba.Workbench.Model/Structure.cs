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
    public string? ClosedWhy { get; set; }
    public int Order { get; set; }
    public int NextItemNumber { get; set; } = 1;
    public DateTimeOffset Created { get; set; }
}

/// <summary>A parallel path of open work inside a project. Proposed by a plan, confirmed by the owner.</summary>
public class Lane
{
    public required string Id { get; set; }          // "wb:A"
    public required string ProjectId { get; set; }
    public required string Key { get; set; }         // "A"
    public string Name { get; set; } = "";
    public double Factor { get; set; } = 1.0;
    public double? Was { get; set; }
    public string? MachineId { get; set; }
    public string State { get; set; } = "";
    public bool Proposed { get; set; }
}

/// <summary>A task in the owner's tree. Only the owner creates one; Claude fills its capped slots.</summary>
public class Item
{
    public required string Id { get; set; }          // "WB-2.2"
    public required string ProjectId { get; set; }
    public string? ParentId { get; set; }
    public int Order { get; set; }
    public required string Title { get; set; }
    public ItemState State { get; set; } = ItemState.Coarse;
    public bool IsOpen { get; set; }
    public string? LaneKey { get; set; }
    public string? HomeMachineId { get; set; }
    public string? Branch { get; set; }
    public string CoarseSize { get; set; } = "~1 week";
    public string Goal { get; set; } = "";
    public string StepsJson { get; set; } = "[]";
    public int SoMin { get; set; }
    public int? EstMin { get; set; }
    public double? Factor { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset? NoteAt { get; set; }
    public string? NoteSessionId { get; set; }
    public string ClaimsJson { get; set; } = "[]";
    public string ImplementsJson { get; set; } = "[]";
    public string? PlanCardId { get; set; }
    public string? PacketCardId { get; set; }
    public string HistoryJson { get; set; } = "[]";
    public string DecisionsJson { get; set; } = "[]";
    public bool Preview { get; set; }
    public DateTimeOffset Created { get; set; }
}

/// <summary>A decision settled on this task: from a settle pass or a question card, possibly drawn from a report line.</summary>
public sealed record Decision(string T, string A, string How, string? Ref, string? From, string Date, string? Ruling);

public sealed record Step(int N, string Text, int EstMin, StepState State, string? ProofRef, List<string>? Needs, string? Note, string? ProofText = null);
public sealed record Claim(string Text, string? ProofRef);
public sealed record HistoryLine(string Date, string Event, string Text);
