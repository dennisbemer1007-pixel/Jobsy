using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

/// <summary>Dream-job slug to the label teachers and school admins already see.</summary>
public static class PupilDreamJobText
{
    public static string Label(CultureState culture, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "—";
        }

        var trimmed = key.Trim();
        if (string.Equals(trimmed, ClassResultsAggregator.UndecidedDreamJobKey, StringComparison.OrdinalIgnoreCase))
        {
            return culture["Leraar.DreamJobs.Undecided"];
        }

        var resource = "DreamJob." + trimmed;
        var localized = culture[resource];
        if (!string.IsNullOrWhiteSpace(localized)
            && !string.Equals(localized, resource, StringComparison.OrdinalIgnoreCase))
        {
            return localized;
        }

        var title = DreamJobCatalog.All.FirstOrDefault(job =>
            string.Equals(job.Key, trimmed, StringComparison.OrdinalIgnoreCase))?.TitleNl;
        return string.IsNullOrWhiteSpace(title) ? trimmed : title;
    }
}
