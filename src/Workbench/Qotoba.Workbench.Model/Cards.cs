namespace Qotoba.Workbench.Model;

/// <summary>Everything the owner acts on. Only three pushed kinds exist: Question, Action (packet, plan, stuck) and Info.</summary>
public class Card
{
    public required string Id { get; set; }
    public CardType Type { get; set; }
    public string? ItemId { get; set; }
    public string? ProjectId { get; set; }
    public string? SessionId { get; set; }
    public int WeightMin { get; set; }
    public DateTimeOffset At { get; set; }
    public required string Headline { get; set; }
    public string Why { get; set; } = "";
    public string? OriginStep { get; set; }
    public string OriginFromJson { get; set; } = "[]";
    public string? OriginText { get; set; }
    public string EvidenceJson { get; set; } = "[]";
    public CardState State { get; set; } = CardState.Open;
    public string? Answer { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
    public DateTimeOffset? DeferredUntil { get; set; }
    /// <summary>Type-specific payload: plan, packet, stuck, proposals or info body. Shapes live in Contracts.</summary>
    public string PayloadJson { get; set; } = "{}";
    public string? QuestionId { get; set; }
    public string? RulingId { get; set; }
    public string? ReportId { get; set; }
}

public class Question
{
    public required string Id { get; set; }          // "Q-7"
    public required string ItemId { get; set; }
    public string? SessionId { get; set; }
    public string? CardId { get; set; }
    public int? StepN { get; set; }
    public required string Text { get; set; }
    public string Shape { get; set; } = "design";    // design | unseen | comment
    public string? Short { get; set; }
    public string? Context { get; set; }
    public string? CodeRef { get; set; }
    public bool UnseenFile { get; set; }
    public string? CommentId { get; set; }
    public string OptionsJson { get; set; } = "[]";
    public string? Cost { get; set; }
    public string TriedJson { get; set; } = "[]";
    public int UnblockMin { get; set; }
    public DateTimeOffset Asked { get; set; }
    public QuestionState State { get; set; } = QuestionState.Open;
    public string? Answer { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
    public string? RulingId { get; set; }
    public string? ReportId { get; set; }
}

public sealed record QuestionOption(string T, bool Rec, string Why, string? Cost, List<string>? Support);

public class Assumption
{
    public required string Id { get; set; }
    public required string SessionId { get; set; }
    public required string ItemId { get; set; }
    public required string Text { get; set; }
    public AssumptionState State { get; set; } = AssumptionState.Unreviewed;
    public DateTimeOffset At { get; set; }
}

public class Found
{
    public required string Id { get; set; }
    public required string SessionId { get; set; }
    public required string ItemId { get; set; }
    public ReportKind Kind { get; set; }
    public required string Title { get; set; }
    public string Why { get; set; } = "";
    public string? EvidenceRef { get; set; }
    public string? SuggestItemId { get; set; }
    public FoundState State { get; set; } = FoundState.InTray;
    public string? PromotedTo { get; set; }
    public DateTimeOffset At { get; set; }
}

public class Ruling
{
    public required string Id { get; set; }          // "R-9"
    public string? ProjectId { get; set; }
    public required string Title { get; set; }
    public required string Text { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Origin { get; set; } = "set by you";
    public string? QuestionId { get; set; }
    public string? ItemId { get; set; }
    public string RelatedJson { get; set; } = "[]";
    public bool NeverReopened { get; set; } = true;
    public string? ReopenNote { get; set; }
}

public class Proposal
{
    public required string Id { get; set; }          // "P-1"
    public required string Text { get; set; }
    public bool Ticked { get; set; } = true;
    public string? CardId { get; set; }
    public string? SessionId { get; set; }
    public string ChatTitle { get; set; } = "";
    public DateTimeOffset At { get; set; }
    public string? RulingId { get; set; }
}
