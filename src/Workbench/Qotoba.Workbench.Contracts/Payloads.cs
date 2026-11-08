namespace Qotoba.Workbench.Contracts;

/* Card payloads, stored as JSON on Card.PayloadJson and rendered by the UI's card views. */

/// <summary>A custom page Claude wrote for a change that has no designer. The owner's answers are its state; approving makes it the agreed design.</summary>
public sealed class PagePayload
{
    public string Title { get; set; } = "";
    /// <summary>The page body: HTML the UI renders in a sandboxed frame. Its controls post answers back by key.</summary>
    public string Html { get; set; } = "";
    public string? Purpose { get; set; }
    public Dictionary<string, string> Answers { get; set; } = [];
    public string? AnswersAt { get; set; }
    public bool Approved { get; set; }
    public string? ApprovedAt { get; set; }
    public string? Machine { get; set; }
}

/// <summary>Claude must leave the agreed design, or has done something that must be reported (a helper type, a static, a surface change).</summary>
public sealed class DeviationPayload
{
    public string What { get; set; } = "";
    public string Why { get; set; } = "";
    public string? Agreed { get; set; }
    public string? Instead { get; set; }
    public List<string> Options { get; set; } = [];
    public string? Machine { get; set; }
    public string? Decided { get; set; }
}

public sealed record StuckAttempt(string T, string Result, string? Output);

public sealed class StuckPayload
{
    public int Step { get; set; }
    public int Of { get; set; }
    public string CheckName { get; set; } = "";
    public string? LastOutput { get; set; }
    public List<StuckAttempt> Attempts { get; set; } = [];
    public string Diagnosis { get; set; } = "";
    public List<string> Reassign { get; set; } = [];
    public string? Machine { get; set; }
}

public sealed class InfoPayload
{
    public string Body { get; set; } = "";
    public string? Chip { get; set; }
    public string Origin { get; set; } = "";
    public string? Ruling { get; set; }
}

/* MCP tool inputs */

public sealed record StepInput(string Text, int EstMin, List<string>? Needs, string? Proof);
public sealed record AttemptInput(string Tried, string Result, string? Output);
public sealed record ItemInput(string Title, string? Size, List<ItemInput>? Children);
public sealed record LinkInput(string To, string Type, string? Reason);
