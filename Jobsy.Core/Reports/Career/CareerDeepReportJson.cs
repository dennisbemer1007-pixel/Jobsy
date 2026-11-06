using System.Text.Json;

namespace Jobsy.Core.Reports.Career;

public static class CareerDeepReportJson
{
    public const int CurrentReportVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static string Serialize(CareerDeepReport report)
        => JsonSerializer.Serialize(report, Options);

    public static CareerDeepReport? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CareerDeepReport>(json, Options);
        }
        catch
        {
            return null;
        }
    }
}
