namespace Qotoba.Workbench.Model;

/// <summary>
/// A document that belongs to an item or a project: a design page, a reference, a specimen set. The file lives in the
/// project's repository, on the node's branch until it merges; the Workbench keeps no copy and no machine path, only
/// the project and the repo-relative path (or a URL for a document outside any repository). Where it is viewed, the
/// Workbench works out how to reach it: through a Host with a checkout, or from the repository's remote.
/// </summary>
public class Document
{
    public required string Id { get; set; }          // "doc-1a2b3c4d"
    public required string ProjectId { get; set; }
    /// <summary>The item it belongs to; null when it belongs to the project itself.</summary>
    public string? ItemId { get; set; }
    public required string Title { get; set; }
    /// <summary>Repo-relative, forward slashes: "docs/design/qdl.html". Null for an external document.</summary>
    public string? Path { get; set; }
    /// <summary>An absolute URL for a document outside any repository.</summary>
    public string? Url { get; set; }
    public string? AddedBySessionId { get; set; }
    public DateTimeOffset At { get; set; }
}
