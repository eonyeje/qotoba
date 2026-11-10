namespace Qotoba.Workbench.Model;

/// <summary>
/// What reaches the owner. Three kinds carry a decision: a Page (a custom HTML page with its own saved answers, for a
/// change that has no designer), a Stuck session, and a Deviation that must be reported. Info is the quiet list.
/// </summary>
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
    /// <summary>Type-specific payload: page, deviation, stuck or info. Shapes live in Contracts.</summary>
    public string PayloadJson { get; set; } = "{}";
    public string? RulingId { get; set; }
    public string? ReportId { get; set; }
}

/// <summary>A line on the item: what a session proceeded on without asking. Information, never a card.</summary>
public class Assumption
{
    public required string Id { get; set; }
    public required string SessionId { get; set; }
    public required string ItemId { get; set; }
    public required string Text { get; set; }
    public AssumptionState State { get; set; } = AssumptionState.Unreviewed;
    public DateTimeOffset At { get; set; }
}

/// <summary>A decision in the owner's words, recorded the moment it is said. The Rulings screen lists them; every brief carries the ones that apply.</summary>
public class Ruling
{
    public required string Id { get; set; }          // "R-9"
    public string? ProjectId { get; set; }
    public required string Title { get; set; }
    public required string Text { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Origin { get; set; } = "set by you";
    public string? SessionId { get; set; }
    public string? ItemId { get; set; }
    public string RelatedJson { get; set; } = "[]";
    public bool NeverReopened { get; set; } = true;
    public string? ReopenNote { get; set; }
    /// <summary>Withdrawn on the owner's word (R-16): it leaves the rulings table and every brief, and stays in history struck through with the reason.</summary>
    public DateTimeOffset? WithdrawnAt { get; set; }
    public string? WithdrawnWhy { get; set; }
    /// <summary>The later ruling that replaces this one, when the owner said so.</summary>
    public string? SupersededById { get; set; }
}
