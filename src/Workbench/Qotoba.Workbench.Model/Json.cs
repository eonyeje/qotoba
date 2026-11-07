using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qotoba.Workbench.Model;

/// <summary>One JSON convention for every stored blob: camelCase, enums as camelCase strings, nulls dropped.</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = false
    };

    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Read<T>(string? json, T fallback) =>
        string.IsNullOrWhiteSpace(json) ? fallback : (JsonSerializer.Deserialize<T>(json, Options) ?? fallback);

    public static List<T> ReadList<T>(string? json) => Read(json, new List<T>());

    public static int LineCount(string text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : text.Replace("\r\n", "\n").Trim().Split('\n').Length;
}
