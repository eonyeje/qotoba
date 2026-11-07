namespace Qotoba.Workbench.Model;

/// <summary>A line the owner typed for a session ("Write your own", "Ask for changes", "Message"). Delivered at the session's next stop.</summary>
public class OwnerMessage
{
    public long Id { get; set; }
    public required string SessionId { get; set; }
    public string? CardId { get; set; }
    public required string Text { get; set; }
    public DateTimeOffset At { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string Via { get; set; } = "stop";   // stop | hello | resume
}
