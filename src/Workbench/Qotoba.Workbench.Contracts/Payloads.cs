namespace Qotoba.Workbench.Contracts;

/* Card payloads, stored as JSON on Card.PayloadJson and rendered by the prototype's card views. */

public sealed record PlanStep(string T, int Est, List<string>? Needs, string? Proof);
public sealed record SettleQ(string Id, string Q, List<string> Options, int Rec, string? From, int? Chosen, string? Line = null, string? Edge = null);

public sealed class PlanPayload
{
    public List<PlanStep> Steps { get; set; } = [];
    public List<SettleQ> Settle { get; set; } = [];
    public string? Lane { get; set; }
    public double Factor { get; set; } = 1.2;
    public string? FactorNote { get; set; }
    public string Goal { get; set; } = "";
    public string? OpensBranch { get; set; }
    public string? Machine { get; set; }
    public bool Approved { get; set; }
    public bool Settled { get; set; }
    public string? ApprovedAt { get; set; }
}

public sealed record PacketCheck(string Label, string? Ref, string Text);
public sealed record PacketBeyond(string T, string? Type, string Why);
public sealed record PacketClaim(string T, string? Proof);

public sealed class PacketPayload
{
    public string Outcome { get; set; } = "";
    public int ActualMin { get; set; }
    public int EstMin { get; set; }
    public List<PacketCheck> Checks { get; set; } = [];
    public List<PacketBeyond> Beyond { get; set; } = [];
    public List<PacketClaim> Claims { get; set; } = [];
    public string? Note { get; set; }
    public string? NoteDate { get; set; }
    public string Deliver { get; set; } = "";
    public List<string> Instruction { get; set; } = [];
    public string? Checkout { get; set; }
    public string? Machine { get; set; }
    public List<string> Files { get; set; } = [];
    public int Plus { get; set; }
    public int Minus { get; set; }
    public bool Approved { get; set; }
    public bool Merged { get; set; }
    public string? MergedCommit { get; set; }
    public string? GitOutput { get; set; }
}

public sealed record StuckAttempt(string T, string Result, string? Run);

public sealed class StuckPayload
{
    public int Step { get; set; }
    public int Of { get; set; }
    public string? CheckRef { get; set; }
    public string CheckName { get; set; } = "";
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

public sealed class ProposalsPayload
{
    public string ChatTitle { get; set; } = "";
    public string When { get; set; } = "";
    public int Turns { get; set; }
}

/* MCP tool inputs */

public sealed record OptionInput(string Text, bool Recommended, string Why, string? CostToChange, List<string>? Support);
public sealed record StepInput(string Text, int EstMin, List<string>? Needs, string? Proof);
public sealed record SettleInput(string Question, List<string> Options, int Recommended, string? FromReport);
public sealed record AttemptInput(string Tried, string Result, string? RunId);
public sealed record ClaimInput(string Text, string? RunId);
public sealed record CheckInput(string Label, string? RunId, string Text);
public sealed record BeyondInput(string What, string? TypeName, string Why);
