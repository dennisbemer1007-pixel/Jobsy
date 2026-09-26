using System.Text.Json;

namespace Jobsy.Core.Reports.Competence;

/// <summary>Stable JSON (de)serialization for <see cref="CompetenceDeepReport"/>.</summary>
public static class CompetenceDeepReportJson
{
    public const int CurrentReportVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize(CompetenceDeepReport report) => JsonSerializer.Serialize(report, Options);

    public static CompetenceDeepReport? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CompetenceDeepReport>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
