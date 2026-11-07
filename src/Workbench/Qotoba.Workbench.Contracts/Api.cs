namespace Qotoba.Workbench.Contracts;

/// <summary>A press on a card in the inbox.</summary>
public sealed record PressRequest(string Card, string Action, string? Value = null, string? Text = null, string? Target = null, int? Index = null);

public sealed record PressResult(bool Ok, string Message, string? Navigate = null);

/// <summary>Owner structure controls on the tree. Claude writes the same things from chat through its own tools.</summary>
public sealed record AddItemRequest(string Kind, string Title, string? Place, string? CoarseSize, List<string>? Implements, string? Project, string? FromFound = null, string? Subtype = null);
public sealed record ItemPatch(string? Title, string? ParentId, bool? Open);
public sealed record ReportPatch(string? Title, string? Severity, string? Subtype, string? State, int? ChosenFix, TargetInput? Target, string? AttachItemId = null);
public sealed record TargetInput(double V, string Unit, string Text);
public sealed record EdgePatch(bool Confirmed);
public sealed record RulingRequest(string Title, string Text, string? Project);
public sealed record SeenRequest(string? File, string? Type, string? Commit);

/// <summary>One answer the owner gave on a page card: a key the page chose and the value.</summary>
public sealed record PageAnswer(string Key, string Value);

/// <summary>What the Host sends when it registers and heartbeats.</summary>
public sealed record MachineRegistration(string Id, string Os, string HostVersion, string Kind, string HostUrl, List<CheckoutInfo> Checkouts, DateTimeOffset Clock);
public sealed record CheckoutInfo(string Path, string Branch, string? Item, bool Worktree, string? Note);

/// <summary>A check result a session reports itself (the hooks report dotnet test and build on their own).</summary>
public sealed record ResultReport(string Check, string? Project, string? Item, string? Session, string Result, int Passed, int Total, double DurationSec, string Cmd);
