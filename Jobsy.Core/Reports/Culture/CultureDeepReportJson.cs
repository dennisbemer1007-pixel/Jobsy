using System.Text.Json;

namespace Jobsy.Core.Reports.Culture;

public static class CultureDeepReportJson
{
    public const int CurrentReportVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static string Serialize(CultureDeepReport report)
        => JsonSerializer.Serialize(report, Options);

    public static CultureDeepReport? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CultureDeepReport>(json, Options);
        }
        catch
        {
            return null;
        }
    }
}
