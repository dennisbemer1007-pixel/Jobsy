using System.Text.Json;

namespace Jobsy.Core.Reports.Values;

public static class ValuesDeepReportJson
{
    public const int CurrentReportVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static string Serialize(ValuesDeepReport report)
        => JsonSerializer.Serialize(report, Options);

    public static ValuesDeepReport? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ValuesDeepReport>(json, Options);
        }
        catch
        {
            return null;
        }
    }
}
