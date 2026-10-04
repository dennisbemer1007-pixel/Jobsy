namespace Jobsy.Core.Rules;

public static class TokenExportFileNames
{
    public static string Purchases(int? year, int? quarter, int? month)
    {
        if (month is int m and >= 1 and <= 12)
        {
            var y = year ?? DateTime.UtcNow.Year;
            return $"token-aankopen-{y}-{m:00}.csv";
        }

        return $"token-aankopen-{(year?.ToString() ?? "all")}-Q{(quarter?.ToString() ?? "all")}.csv";
    }
}
