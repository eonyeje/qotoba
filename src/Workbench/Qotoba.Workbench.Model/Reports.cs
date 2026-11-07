namespace Qotoba.Workbench.Model;

/// <summary>
/// An FTR, BUG or PER: separate data attached to the item it came up in, or to the project when no item exists yet.
/// Always written by Claude: an FTR from the owner's words in chat, a BUG or PER from Claude's own runs and checks.
/// Information only: nothing on it ever waits on the owner.
/// </summary>
public class Report
{
    public required string Id { get; set; }          // "WB-BUG-003"
    public ReportKind Kind { get; set; }
    public string? Subtype { get; set; }
    public required string Title { get; set; }
    public required string ProjectId { get; set; }
    /// <summary>The item it is attached to; null when it is attached to the project itself.</summary>
    public string? AttachItemId { get; set; }
    /// <summary>The item that took it on, once one does.</summary>
    public string? TakenOnItemId { get; set; }
    /// <summary>One of the eight register words: Open, Verify first, Designing, Designed, In progress, Done, Exists, Won't do.</summary>
    public string State { get; set; } = "Open";
    /// <summary>What is left, what it waits on, or the ruling behind Won't do: the first clause of the old register's Notes.</summary>
    public string? Left { get; set; }
    public SeverityLevel? Severity { get; set; }
    public string? SeverityReason { get; set; }
    public Author SeverityBy { get; set; } = Author.Claude;
    public string SourceType { get; set; } = "chat";  // chat | check | run | split | duplicate
    public string? SourceSessionId { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? Changed { get; set; }
    public int? EstMin { get; set; }
    public string? EstBy { get; set; }
    /// <summary>Section payloads keyed by section name, each an object with an author field and the section's own fields.</summary>
    public string SectionsJson { get; set; } = "{}";
    public string HistoryJson { get; set; } = "[]";
}

/// <summary>The eight register states, as the owner set them for Quanta.</summary>
public static class ReportStates
{
    public static readonly string[] All = ["Open", "Verify first", "Designing", "Designed", "In progress", "Done", "Exists", "Won't do"];
    public static bool IsClosed(string s) => s is "Done" or "Exists" or "Won't do";
    public static string? Normalise(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim().ToLowerInvariant().Replace("'", "'");
        foreach (var w in All) if (string.Equals(w, s.Trim(), StringComparison.OrdinalIgnoreCase)) return w;
        return t switch
        {
            "wont do" or "won't do" or "wontdo" or "declined" or "not approved" => "Won't do",
            "verify" or "verify-first" or "verifyfirst" => "Verify first",
            "in-progress" or "inprogress" or "building" or "progress" => "In progress",
            "done" or "fixed" or "closed" or "complete" => "Done",
            "open" or "raised" or "reported" => "Open",
            _ => null
        };
    }
}

/// <summary>A typed link between items and FPBs, in the register's own words: needs, split from, replaced by, carried by, found in.</summary>
public class Edge
{
    public required string Id { get; set; }
    public required string From { get; set; }        // "item:WB-2.2" or "fpb:WB-BUG-001"
    public required string To { get; set; }
    public EdgeType Type { get; set; }
    public bool Hard { get; set; }
    public string? Reason { get; set; }
    public Actor By { get; set; } = Actor.Claude;
    public string? SessionId { get; set; }
    public bool Confirmed { get; set; } = true;
    public int? Min { get; set; }
}
