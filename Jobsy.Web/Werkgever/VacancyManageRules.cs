using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Werkgever;

/// <summary>Client-side tab/filter helpers for the Vacatures table (file 03).</summary>
public static class VacancyManageRules
{
    public const int BulkMax = 50;
    public const int PageSize = 25;
    public const string ColumnsStorageKey = "wg.vacatures.columns";

    public static readonly string[] EmploymentTypes =
        ["Fulltime", "Parttime", "Flex", "Bijbaan"];

    public static bool MatchesTab(VacancyListItem v, string tab, DateOnly today)
    {
        var status = v.Status ?? "";
        return tab.ToLowerInvariant() switch
        {
            "actief" => string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase),
            "wacht" => string.Equals(status, "PendingApproval", StringComparison.OrdinalIgnoreCase),
            "concept" => string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase),
            "verloopt" => IsExpiringSoon(v, today),
            "gesloten" => string.Equals(status, "Archived", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Fulfilled", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    public static bool IsExpiringSoon(VacancyListItem v, DateOnly today)
    {
        if (!string.Equals(v.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var until = today.AddDays(WerkgeverDashboardRules.VacanciesExpiringDays);
        return v.EndDate >= today && v.EndDate <= until;
    }

    public static string EmploymentTypeLabel(VacancyListItem v)
    {
        if (v.CategoryFields is not null
            && v.CategoryFields.TryGetValue(VacancyCategoryExtraFields.ContractType, out var contract)
            && !string.IsNullOrWhiteSpace(contract))
        {
            return contract.Trim() switch
            {
                "Oproep" => "Bijbaan",
                "Parttime" => "Parttime",
                "Fulltime" => "Fulltime",
                _ => contract.Trim()
            };
        }

        if (v.FlexibleTimes)
        {
            return "Flex";
        }

        if (v.MinHoursPerWeek is decimal min && v.MaxHoursPerWeek is decimal max)
        {
            return HoursRangeRules.Categorize((min + max) / 2m) switch
            {
                HoursCategory.FullTime => "Fulltime",
                HoursCategory.PartTimeSmall or HoursCategory.PartTimeLarge => "Parttime",
                HoursCategory.SideJob => "Bijbaan",
                _ => "Parttime"
            };
        }

        return "Flex";
    }

    public static string StatusCss(VacancyListItem v)
    {
        var status = v.Status ?? "";
        if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return "ok";
        }

        if (string.Equals(status, "PendingApproval", StringComparison.OrdinalIgnoreCase))
        {
            return "warn";
        }

        return "neutral";
    }

    public static string FormatShortName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "";
        }

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return parts[0];
        }

        var first = parts[0];
        var last = parts[^1];
        var initial = first.Length > 0 ? first[0] : '?';
        return $"{initial}. {last}";
    }

    public static decimal EstimatePendingTokens(
        VacancyListItem v,
        IReadOnlyDictionary<string, decimal> costs)
    {
        var total = 0m;
        if (v.RequestedHighlight)
        {
            total += v.CategoryHighlightCostTokens
                ?? costs.GetValueOrDefault("Highlight", VacancyProductRules.DefaultHighlightCostTokens);
        }

        // PushBom cost needs a preview; count base cost as a floor when requested.
        if (v.RequestedPushBom)
        {
            total += v.CategoryPushBomCostTokens
                ?? costs.GetValueOrDefault("PushBom", 3m);
        }

        return total;
    }

    public static string BulkSummary(int ok, int fail)
        => $"{ok} gelukt, {fail} niet gelukt";
}
