using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qotoba.Workbench.Contracts;

/// <summary>The JSON Claude Code POSTs to an http hook. Only the fields the Workbench reads.</summary>
public sealed class HookEvent
{
    [JsonPropertyName("session_id")] public string SessionId { get; set; } = "";
    [JsonPropertyName("hook_event_name")] public string HookEventName { get; set; } = "";
    [JsonPropertyName("cwd")] public string? Cwd { get; set; }
    [JsonPropertyName("transcript_path")] public string? TranscriptPath { get; set; }
    [JsonPropertyName("source")] public string? Source { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }
    [JsonPropertyName("prompt")] public string? Prompt { get; set; }
    [JsonPropertyName("session_title")] public string? SessionTitle { get; set; }
    [JsonPropertyName("tool_name")] public string? ToolName { get; set; }
    [JsonPropertyName("tool_input")] public JsonElement? ToolInput { get; set; }
    [JsonPropertyName("tool_response")] public JsonElement? ToolResponse { get; set; }
    [JsonPropertyName("tool_use_id")] public string? ToolUseId { get; set; }
    [JsonPropertyName("duration_ms")] public long? DurationMs { get; set; }
    [JsonPropertyName("last_assistant_message")] public string? LastAssistantMessage { get; set; }
    [JsonPropertyName("stop_hook_active")] public bool? StopHookActive { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("notification_type")] public string? NotificationType { get; set; }
}

/// <summary>What a hook may answer. Empty means "carry on".</summary>
public sealed class HookReply
{
    [JsonPropertyName("continue")] public bool? Continue { get; set; }
    [JsonPropertyName("stopReason")] public string? StopReason { get; set; }
    [JsonPropertyName("decision")] public string? Decision { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }
    [JsonPropertyName("systemMessage")] public string? SystemMessage { get; set; }
    [JsonPropertyName("hookSpecificOutput")] public HookSpecific? HookSpecificOutput { get; set; }

    public static HookReply Ok() => new();
    public static HookReply Block(string reason) => new() { Decision = "block", Reason = reason };
    public static HookReply Context(string eventName, string context) =>
        new() { HookSpecificOutput = new HookSpecific { HookEventName = eventName, AdditionalContext = context } };
    public static HookReply Deny(string reason) =>
        new() { HookSpecificOutput = new HookSpecific { HookEventName = "PreToolUse", PermissionDecision = "deny", PermissionDecisionReason = reason } };
}

public sealed class HookSpecific
{
    [JsonPropertyName("hookEventName")] public string HookEventName { get; set; } = "";
    [JsonPropertyName("additionalContext")] public string? AdditionalContext { get; set; }
    [JsonPropertyName("permissionDecision")] public string? PermissionDecision { get; set; }
    [JsonPropertyName("permissionDecisionReason")] public string? PermissionDecisionReason { get; set; }
}
