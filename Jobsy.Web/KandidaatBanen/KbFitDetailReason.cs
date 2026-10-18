using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>Concrete fit copy for vacancy detail (no percentages in text).</summary>
public static class KbFitDetailReason
{
    public static string For(VacancyListItem vacancy, string kind, CultureState culture)
    {
        var animalCare = IsAnimalCare(vacancy);
        var key = kind.ToLowerInvariant() switch
        {
            "culture" => animalCare ? "Kb.WhyDetail.AnimalCare.Culture" : "Kb.WhyDetail.Generic.Culture",
            "values" => animalCare ? "Kb.WhyDetail.AnimalCare.Values" : "Kb.WhyDetail.Generic.Values",
            "competency" => animalCare ? "Kb.WhyDetail.AnimalCare.Competency" : "Kb.WhyDetail.Generic.Competency",
            "interest" => animalCare ? "Kb.WhyDetail.AnimalCare.Interest" : "Kb.WhyDetail.Generic.Interest",
            _ => "Kb.WhyDetail.Generic.Culture"
        };

        return culture[key];
    }

    private static bool IsAnimalCare(VacancyListItem vacancy)
    {
        var seed = string.Join(' ', new[] { vacancy.CategoryName, vacancy.Title }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return seed.Contains("dieren", StringComparison.OrdinalIgnoreCase)
               || seed.Contains("pension", StringComparison.OrdinalIgnoreCase)
               || seed.Contains("veter", StringComparison.OrdinalIgnoreCase)
               || seed.Contains("dierenverzorg", StringComparison.OrdinalIgnoreCase);
    }
}
