namespace Qotoba.Workbench.Model;

/// <summary>What the owner has and has not seen of a file. Content is pulled from the Host; only seen-state is stored.</summary>
public class FileSeen
{
    public required string Id { get; set; }          // "runrecord"
    public required string Path { get; set; }
    public string Repo { get; set; } = "";
    public string? ProjectId { get; set; }
    public string Folder { get; set; } = "";
    public string? Branch { get; set; }
    public bool Seen { get; set; }
    public int NewLines { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? AddedAt { get; set; }
    public string? SeenCommit { get; set; }
    public string LinesJson { get; set; } = "[]";
    public string DiffJson { get; set; } = "[]";
    public string ContentHash { get; set; } = "";
    public string? SeenHash { get; set; }
    public string? MachineId { get; set; }
    public string? Checkout { get; set; }
    public string? ItemId { get; set; }
    public DateTimeOffset? IndexedAt { get; set; }
}

public class TypeSeen
{
    public required string Id { get; set; }          // "RunRecord"
    public string Kind { get; set; } = "";
    public string Ns { get; set; } = "";
    public string? At { get; set; }                  // "file:runrecord:11-37"
    public bool Seen { get; set; }
    public string? NewIn { get; set; }
    public string? ItemId { get; set; }
    public string Summary { get; set; } = "";
    public bool BeyondPlan { get; set; }
}

/// <summary>A planned piece of code that is not yet written; drawn as a dashed chip.</summary>
public class NotYetWritten
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string Where { get; set; } = "";
    public string Step { get; set; } = "";
}

public class Comment
{
    public required string Id { get; set; }
    public required string FileId { get; set; }
    public int Line { get; set; }
    public string Symbol { get; set; } = "";
    public string SymbolKind { get; set; } = "";
    public required string Text { get; set; }
    public string TextHash { get; set; } = "";
    public CommentState State { get; set; } = CommentState.Open;
    public string? ItemId { get; set; }
    public string? HandledJson { get; set; }
    public string? StaleWhy { get; set; }
    public string? QuestionId { get; set; }
    public string VersionsJson { get; set; } = "[]";
    public DateTimeOffset? LastSeenInIndex { get; set; }
    public string? ProofSessionId { get; set; }
}

public sealed record CommentVersion(string Date, string Text, string? Above);
public sealed record HandledInfo(string Date, string Time, string? Session, string? Note, string? Proof);

public class Commit
{
    public required string Id { get; set; }
    public required string Msg { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? SessionId { get; set; }
    public string? BranchId { get; set; }
    public string FilesJson { get; set; } = "[]";
    public int Plus { get; set; }
    public int Minus { get; set; }
    public string DiffJson { get; set; } = "[]";
    public string? ProvesCommentId { get; set; }
    public string? StalesCommentId { get; set; }
}

public class Branch
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? ItemId { get; set; }
    public string? MachineId { get; set; }
    public string State { get; set; } = "local";
    public int Ahead { get; set; }
    public int Behind { get; set; }
    public bool Pushed { get; set; }
    public string? MergedCommit { get; set; }
    public string? Worktree { get; set; }
    public string CommitsJson { get; set; } = "[]";
    public bool Dirty { get; set; }
    public string? Checkout { get; set; }
    public DateTimeOffset? ReportedAt { get; set; }
}

public class ClaudeMdSection
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? FileRef { get; set; }
    public string N { get; set; } = "";
}

public class Setting
{
    public required string Key { get; set; }
    public string Value { get; set; } = "";
}

public class ActionLog
{
    public long Id { get; set; }
    public DateTimeOffset At { get; set; }
    public Actor Actor { get; set; }
    public required string Verb { get; set; }
    public required string RecordKind { get; set; }
    public required string RecordId { get; set; }
    public string? Detail { get; set; }
}
