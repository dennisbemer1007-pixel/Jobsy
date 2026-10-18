using Jobsy.Core.Careers;
using Jobsy.Web.Models;

namespace Jobsy.Web.Onboarding;

/// <summary>Fallback dream-job cards for onboarding step 6 when API suggestions are empty.</summary>
public static class OnboardingDreamSuggestionPicker
{
    private static readonly string[] DefaultTitles =
    [
        "Verpleegkundige",
        "Logistiek medewerker",
        "Horecamedewerker",
        "Administratief medewerker",
    ];

    public static IReadOnlyList<CareerDreamOptionApiItem> BuildFitSuggestions(
        IReadOnlyList<CareerDreamOptionApiItem> fromApi,
        string? education,
        int max = 4)
    {
        if (fromApi.Count > 0)
        {
            return fromApi.Take(max).ToList();
        }

        var list = new List<CareerDreamOptionApiItem>();
        var seed = string.IsNullOrWhiteSpace(education) ? "logistiek" : education;
        foreach (var hit in OccupationCatalog.Shared.Search(seed, 8, education))
        {
            if (list.Any(item => string.Equals(item.Title, hit.Title, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            list.Add(new CareerDreamOptionApiItem
            {
                Title = hit.Title,
                Description = hit.Description,
                ReasonKey = "Onboarding.Step6.Suggest.ReasonProfile",
            });
            if (list.Count >= max)
            {
                break;
            }
        }

        if (list.Count >= 2)
        {
            return list;
        }

        foreach (var title in DefaultTitles)
        {
            if (list.Any(item => string.Equals(item.Title, title, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            list.Add(new CareerDreamOptionApiItem { Title = title, ReasonKey = "Onboarding.Step6.Suggest.ReasonPopular" });
            if (list.Count >= max)
            {
                break;
            }
        }

        return list;
    }

    public static IReadOnlyList<string> BuildRegionalTitles(IEnumerable<string> nearbyVacancyTitles, int max = 3)
    {
        return nearbyVacancyTitles
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Select(title => title.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();
    }
}
