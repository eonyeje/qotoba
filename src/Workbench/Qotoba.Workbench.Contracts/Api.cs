namespace Qotoba.Workbench.Contracts;

/// <summary>A press on a card in the inbox. The only way the owner answers anything.</summary>
public sealed record PressRequest(string Card, string Action, string? Value = null, string? Text = null, string? Target = null, int? Index = null);

public sealed record PressResult(bool Ok, string Message, string? Navigate = null);

/// <summary>Owner structure controls. Never reachable by Claude.</summary>
public sealed record AddItemRequest(string Kind, string Title, string? Place, string? CoarseSize, List<string>? Implements, string? Project, string? FromFound = null, string? Subtype = null);
public sealed record ItemPatch(string? Title, string? ParentId, bool? Open);
public sealed record ReportPatch(string? Title, string? Severity, string? Subtype, string? State, int? ChosenFix, TargetInput? Target, string? AttachItemId = null);
public sealed record TargetInput(double V, string Unit, string Text);
public sealed record EdgePatch(bool Confirmed);

/* Report sections as Claude writes them: a free object per section, the author added by the service. */
public sealed record ImplLineInput(string Kind, string Text, string? Rec, string? Why, string? Cost, List<OptionInput>? Options);
public sealed record RulingRequest(string Title, string Text, string? Project);
public sealed record SeenRequest(string? File, string? Type, string? Commit);

/// <summary>What the Host sends when it registers and heartbeats.</summary>
public sealed record MachineRegistration(string Id, string Os, string HostVersion, string Kind, string HostUrl, List<CheckoutInfo> Checkouts, DateTimeOffset Clock);
public sealed record CheckoutInfo(string Path, string Branch, string? Item, bool Worktree, string? Note);

/// <summary>A run reported by the Host after it executed a check.</summary>
public sealed record RunReport(string Check, string? Item, string? Session, string Machine, string Result, int Passed, int Total,
    double DurationSec, string Cmd, string OutputTail, int Attempt, int? ExitCode, string? Expected, string? Value, int? Step);

public sealed record GitInstruction(string Checkout, List<string> Lines);
public sealed record GitResult(bool Ok, string Output);
