using Jobsy.Core.Rules;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>Region label + disclaimer for the national future-jobs outlook block.</summary>
public static class FutureJobsRegionDisplay
{
    public static string RegionLabel(CultureState culture, CandidatePreferences? prefs)
    {
        var workRegion = WorkRegionRules.Sanitize(prefs?.WorkRegion);
        if (!string.IsNullOrWhiteSpace(workRegion))
        {
            return workRegion;
        }

        var city = Jobsy.Core.Contracts.LobsyCvModelFactory.ExtractCity(prefs?.HomeAddress);
        var cityLabel = WorkRegionRules.Sanitize(city);
        if (!string.IsNullOrWhiteSpace(cityLabel))
        {
            return cityLabel.Contains(" e.o.", StringComparison.OrdinalIgnoreCase)
                ? cityLabel
                : $"{cityLabel} e.o.";
        }

        return culture["FutureJobs.Region"];
    }

    /// <summary>Westland/Haaglanden disclaimer is shown for all candidates (national dataset).</summary>
    public static string RegionNote(CultureState culture)
        => culture["FutureJobs.RegionNote"];
}
