using System.Security.Cryptography;
using System.Text;

namespace Qotoba.Workbench.Model;

public static class Ids
{
    /// <summary>Child id under a parent: "WB-2" + 3 → "WB-2.3"; project root: "WB" + 4 → "WB-4".</summary>
    public static string Child(string projectCode, string? parentId, int n) =>
        parentId is null ? $"{projectCode}-{n}" : $"{parentId}.{n}";

    public static string Report(ReportKind kind, int n) => $"{kind}-{n}";
    public static string Ruling(int n) => $"R-{n}";
    public static string Question(int n) => $"Q-{n}";
    public static string Proposal(int n) => $"P-{n}";

    public static string Short() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4));
    public static string Token() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static string Card(CardType type) => type switch
    {
        CardType.Question => "q-" + Short(),
        CardType.Packet => "pk-" + Short(),
        CardType.Plan => "pl-" + Short(),
        CardType.Stuck => "st-" + Short(),
        CardType.Proposals => "rp-" + Short(),
        _ => "i-" + Short()
    };
}
