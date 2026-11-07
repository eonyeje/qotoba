using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Qotoba.Workbench.Contracts;

namespace Qotoba.Workbench.Host;

/// <summary>
/// Reads a checkout the way the Workbench wants it: files carrying + comments (with the symbol each sits under),
/// public types in files changed on the branch, the branch's state and its commits. Nothing is written.
/// </summary>
public static class Indexer
{
    private static readonly string[] Skip = ["\\bin\\", "\\obj\\", "/bin/", "/obj/", "\\node_modules\\", "/node_modules/", "\\.git\\", "/.git/"];

    public static async Task<CommentIndexPost> IndexAsync(string machine, string checkout, string? item, Func<string, string, string, Task<(int code, string output)>> git, CancellationToken ct = default)
    {
        var branch = (await git("rev-parse --abbrev-ref HEAD", checkout, "")).output.Trim();
        var main = (await git("rev-parse --verify --quiet main", checkout, "")).code == 0 ? "main" : "master";
        var changed = branch == main ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : (await git($"diff --name-only {main}...HEAD", checkout, "")).output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim().Replace('/', Path.DirectorySeparatorChar)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var files = new List<IndexedFile>();
        var types = new List<IndexedType>();
        foreach (var path in Directory.EnumerateFiles(checkout, "*.cs", SearchOption.AllDirectories))
        {
            if (Skip.Any(s => path.Contains(s, StringComparison.OrdinalIgnoreCase))) continue;
            var rel = Path.GetRelativePath(checkout, path);
            string text;
            try { text = await File.ReadAllTextAsync(path, ct); } catch { continue; }
            if (!text.Contains("//+") && !changed.Contains(rel)) continue;
            var tree = CSharpSyntaxTree.ParseText(text, cancellationToken: ct);
            var root = await tree.GetRootAsync(ct);
            var comments = new List<IndexedComment>();
            foreach (var trivia in root.DescendantTrivia().Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia)))
            {
                var raw = trivia.ToString();
                if (!raw.StartsWith("//+")) continue;
                var proof = raw.StartsWith("//+=");
                var body = raw[(proof ? 4 : 3)..].Trim();
                string? proofSession = null;
                if (proof)
                {
                    var sp = body.IndexOf(' ');
                    var first = sp < 0 ? body : body[..sp];
                    if (first.Length >= 4 && first.All(ch => char.IsLetterOrDigit(ch) || ch == '-')) { proofSession = first; body = sp < 0 ? "" : body[(sp + 1)..].Trim(); }
                }
                var token = trivia.Token;
                var member = token.Parent?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                var (symbol, kind) = Describe(member);
                var line = tree.GetLineSpan(trivia.Span).StartLinePosition.Line + 1;
                comments.Add(new IndexedComment(line, body, proof, proofSession, symbol, kind, Sha(body + "|" + symbol)));
            }
            if (changed.Contains(rel))
                foreach (var td in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(t => t.Modifiers.Any(SyntaxKind.PublicKeyword)))
                {
                    var span = tree.GetLineSpan(td.Span);
                    var ns = td.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";
                    var kind = td switch { RecordDeclarationSyntax r => (r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? "record struct" : "record"), InterfaceDeclarationSyntax => "interface", EnumDeclarationSyntax => "enum", StructDeclarationSyntax => "struct", _ => "class" };
                    if (td.Modifiers.Any(SyntaxKind.SealedKeyword)) kind = "sealed " + kind;
                    var doc = td.GetLeadingTrivia().Select(t => t.ToString()).Where(s => s.Contains("<summary>")).FirstOrDefault();
                    var summary = doc is null ? "" : System.Text.RegularExpressions.Regex.Replace(doc, @"<[^>]+>|///", " ").Replace("\r", " ").Replace("\n", " ").Trim();
                    types.Add(new IndexedType(td.Identifier.Text, kind, ns, rel, span.StartLinePosition.Line + 1, span.EndLinePosition.Line + 1, summary.Length > 160 ? summary[..160] : summary));
                }
            if (comments.Count == 0 && !changed.Contains(rel)) continue;
            if (comments.Count == 0) continue; // changed files without comments contribute types only
            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            var diff = branch == main ? [] : (await git($"diff {main}...HEAD -- \"{rel}\"", checkout, "")).output.Replace("\r\n", "\n").Split('\n').Take(400).ToList();
            files.Add(new IndexedFile(rel, Sha(text), lines.Count, lines.Take(400).ToList(), diff, comments));
        }

        BranchInfo? bi = null; var commits = new List<CommitInfo>();
        if (branch != main && branch != "HEAD")
        {
            var counts = (await git($"rev-list --left-right --count {main}...HEAD", checkout, "")).output.Trim().Split('\t', ' ', StringSplitOptions.RemoveEmptyEntries);
            var behind = counts.Length == 2 && int.TryParse(counts[0], out var b0) ? b0 : 0;
            var ahead = counts.Length == 2 && int.TryParse(counts[1], out var a0) ? a0 : 0;
            var dirty = (await git("status --porcelain", checkout, "")).output.Trim().Length > 0;
            var remote = (await git($"rev-parse --verify --quiet origin/{branch}", checkout, "")).code == 0;
            var pushed = remote && (await git($"rev-list --count origin/{branch}..HEAD", checkout, "")).output.Trim() == "0";
            var mergedInto = (await git($"branch --merged {main} --list {branch}", checkout, "")).output.Trim().Length > 0 && ahead == 0 ? main : null;
            bi = new BranchInfo(branch, ahead, behind, pushed, dirty, mergedInto);
            var log = (await git($"log {main}..HEAD --format=%h%x1f%s%x1f%cI --shortstat --max-count=30", checkout, "")).output.Replace("\r\n", "\n").Split('\n');
            CommitInfo? cur = null;
            foreach (var l in log)
            {
                if (l.Contains('\u001f'))
                {
                    if (cur is not null) commits.Add(cur);
                    var p = l.Split('\u001f');
                    cur = new CommitInfo(p[0].Trim(), p.Length > 1 ? p[1] : "", p.Length > 2 && DateTimeOffset.TryParse(p[2], out var d) ? d : DateTimeOffset.UtcNow, [], 0, 0);
                }
                else if (cur is not null && l.Contains("changed"))
                {
                    var plus = System.Text.RegularExpressions.Regex.Match(l, @"(\d+) insertion"); var minus = System.Text.RegularExpressions.Regex.Match(l, @"(\d+) deletion");
                    cur = cur with { Plus = plus.Success ? int.Parse(plus.Groups[1].Value) : 0, Minus = minus.Success ? int.Parse(minus.Groups[1].Value) : 0 };
                }
            }
            if (cur is not null) commits.Add(cur);
            foreach (var c in commits.Take(30).ToList())
            {
                var names = (await git($"show --name-only --format= {c.Id}", checkout, "")).output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                commits[commits.IndexOf(c)] = c with { Files = names };
            }
        }
        return new CommentIndexPost(machine, checkout, branch, item, files, types, bi, commits);
    }

    private static (string symbol, string kind) Describe(MemberDeclarationSyntax? m) => m switch
    {
        MethodDeclarationSyntax x => (x.Identifier.Text, "method"),
        PropertyDeclarationSyntax x => (x.Identifier.Text, "property"),
        FieldDeclarationSyntax x => (x.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "field", x.Modifiers.Any(SyntaxKind.ConstKeyword) ? "constant" : "field"),
        ConstructorDeclarationSyntax x => (x.Identifier.Text, "constructor"),
        RecordDeclarationSyntax x => (x.Identifier.Text, "record"),
        ClassDeclarationSyntax x => (x.Identifier.Text, "class"),
        StructDeclarationSyntax x => (x.Identifier.Text, "struct"),
        InterfaceDeclarationSyntax x => (x.Identifier.Text, "interface"),
        EnumDeclarationSyntax x => (x.Identifier.Text, "enum"),
        BaseNamespaceDeclarationSyntax x => (x.Name.ToString(), "namespace"),
        _ => ("file", "file")
    };

    private static string Sha(string s) => Convert.ToHexStringLower(SHA1.HashData(Encoding.UTF8.GetBytes(s)))[..12];
}
