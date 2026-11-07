namespace Qotoba.Workbench.Model;

/// <summary>A Claude Code session, identified by Claude's own session id, measured through hooks.</summary>
public class Session
{
    public required string Id { get; set; }
    public string Title { get; set; } = "";
    public string? MachineId { get; set; }
    public string? ItemId { get; set; }
    public SessionState State { get; set; } = SessionState.Working;
    public bool Stuck { get; set; }
    public DateTimeOffset Started { get; set; }
    public DateTimeOffset? Ended { get; set; }
    public DateTimeOffset? LastEvent { get; set; }
    public DateTimeOffset? TurnStarted { get; set; }
    public int Minutes { get; set; }
    public int Turns { get; set; }
    public int Tools { get; set; }
    public int? StepN { get; set; }
    public int StepMin { get; set; }
    public string? LastAction { get; set; }
    public DateTimeOffset? WaitingSince { get; set; }
    public string? Cwd { get; set; }
    public string? TranscriptPath { get; set; }
    public string LedgerJson { get; set; } = "[]";
    public bool Hello { get; set; }
}

public sealed record LedgerLine(string At, string Kind, string Text);

public class Machine
{
    public required string Id { get; set; }
    public string Os { get; set; } = "";
    public string HostVersion { get; set; } = "";
    public bool Online { get; set; }
    public DateTimeOffset? LastSeen { get; set; }
    public double ClockSkewSec { get; set; }
    public MachineKind Kind { get; set; } = MachineKind.Machine;
    public bool Ephemeral { get; set; }
    public string CheckoutsJson { get; set; } = "[]";
    public DateTimeOffset Registered { get; set; }
    public string? HostUrl { get; set; }
}

public sealed record Checkout(string Path, string Branch, string? ItemId, bool Worktree, string? Note);

public class Token
{
    public required string Id { get; set; }
    public required string Client { get; set; }
    public TokenKind Kind { get; set; }
    public required string Hash { get; set; }
    public DateTimeOffset Issued { get; set; }
    public DateTimeOffset? LastUsed { get; set; }
}
