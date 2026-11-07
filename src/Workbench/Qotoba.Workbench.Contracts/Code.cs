namespace Qotoba.Workbench.Contracts;

/* What the Host posts after indexing a checkout: + comments, public types, the branch and its commits. */

public sealed record CommentIndexPost(string Machine, string Checkout, string Branch, string? Item, List<IndexedFile> Files, List<IndexedType> Types, BranchInfo? BranchState, List<CommitInfo> Commits);

public sealed record IndexedFile(string Path, string Hash, int LineCount, List<string> Lines, List<string> DiffFromMain, List<IndexedComment> Comments);

public sealed record IndexedComment(int Line, string Text, bool Proof, string? ProofSession, string Symbol, string SymbolKind, string Hash);

public sealed record IndexedType(string Name, string Kind, string Namespace, string Path, int From, int To, string Summary);

public sealed record BranchInfo(string Name, int Ahead, int Behind, bool Pushed, bool Dirty, string? MergedInto);

public sealed record CommitInfo(string Id, string Message, DateTimeOffset Date, List<string> Files, int Plus, int Minus);

public sealed record OpenRequest(string Machine, string File, int Line);
