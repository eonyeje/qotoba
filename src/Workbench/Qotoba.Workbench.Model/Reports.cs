namespace Qotoba.Workbench.Model;

/// <summary>An FTR, BUG or PER report. Information only: nothing on it ever waits on the owner.</summary>
public class Report
{
    public required string Id { get; set; }          // "BUG-2"
    public ReportKind Kind { get; set; }
    public string? Subtype { get; set; }
    public required string Title { get; set; }
    public required string ProjectId { get; set; }
    public string? AttachItemId { get; set; }
    public string State { get; set; } = "";
    public SeverityLevel? Severity { get; set; }
    public string? SeverityReason { get; set; }
    public Author SeverityBy { get; set; } = Author.Claude;
    public string SourceType { get; set; } = "created";  // imported | created | promoted | split | duplicate
    public string? SourceSessionId { get; set; }
    public string? SourceFoundId { get; set; }
    public DateTimeOffset Created { get; set; }
    public int? EstMin { get; set; }
    public string? EstBy { get; set; }
    /// <summary>Section payloads keyed by section name, each an object with an author field and the section's own fields.</summary>
    public string SectionsJson { get; set; } = "{}";
    public string HistoryJson { get; set; } = "[]";
    public int? ChosenFix { get; set; }
    public string? TargetJson { get; set; }
    public int NextLine { get; set; } = 1;
}

/// <summary>One "When implementing" line. Written by Claude at report time; moved only by plans, cards, runs and packets.</summary>
public class ImplLine
{
    public required string Id { get; set; }
    public required string ReportId { get; set; }
    public int Order { get; set; }
    public ImplKind Kind { get; set; }
    public required string Text { get; set; }
    public string? Rec { get; set; }
    public string? Why { get; set; }
    public string? Cost { get; set; }
    public string OptionsJson { get; set; } = "[]";
    public ImplStatus Status { get; set; } = ImplStatus.Open;
    public string? Ref { get; set; }
    public string? Via { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset? Date { get; set; }
    public string? QuestionId { get; set; }
    /// <summary>The task that took this line on, once a plan has drawn it.</summary>
    public string? ItemId { get; set; }
}

/// <summary>A dependency between items and reports. Claude proposes, the owner confirms through a plan's settle pass.</summary>
public class Edge
{
    public required string Id { get; set; }
    public required string From { get; set; }        // "item:WB-2.2" or "fpb:BUG-1"
    public required string To { get; set; }
    public EdgeType Type { get; set; }
    public bool Hard { get; set; }
    public string? Reason { get; set; }
    public Actor By { get; set; } = Actor.Claude;
    public string? SessionId { get; set; }
    public bool Confirmed { get; set; }
    public int? Min { get; set; }
}
