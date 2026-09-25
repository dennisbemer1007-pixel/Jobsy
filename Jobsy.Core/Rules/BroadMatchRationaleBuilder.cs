namespace Jobsy.Core.Rules;

/// <summary>
/// Short candidate-facing onderbouwing for broader (non-title-exact) vacancy matches.
/// Composes opleiding, competenties/drijfveren (Wie ben ik? / DISC/OCEAN) and transferable skills.
/// </summary>
public static class BroadMatchRationaleBuilder
{
    public static string? TryBuild(
        ProfileVacancyMatchInput input,
        double experience01,
        double? competency01,
        double? interest01,
        VacancyOccupationMatch.Fit? occupationFit,
        bool isBroadMatch)
    {
        if (!isBroadMatch)
        {
            return null;
        }

        var parts = new List<string>();

        var education = DescribeEducation(input);
        if (education is not null)
        {
            parts.Add(education);
        }

        var drivers = DescribeDrivers(input, competency01, interest01, occupationFit);
        if (drivers is not null)
        {
            parts.Add(drivers);
        }

        var transferable = DescribeTransferable(input, experience01);
        if (transferable is not null)
        {
            parts.Add(transferable);
        }

        if (parts.Count == 0)
        {
            return $"Deze rol past bij jouw brede profiel — niet alleen op titel, maar op achtergrond, werkstijl en overdraagbare ervaring.";
        }

        return string.Join(" ", parts);
    }

    private static string? DescribeEducation(ProfileVacancyMatchInput input)
    {
        var levels = (input.CandidateEducations ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e)
                        && !string.Equals(e, EducationLevelLabels.None, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();
        if (levels.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(input.RequiredEducation)
            && EducationLevelLabels.CandidateMeetsRequirement(input.CandidateEducations, input.RequiredEducation))
        {
            return $"Jouw opleidingsniveau ({JoinNl(levels)}) sluit aan bij wat deze vacature vraagt.";
        }

        return $"Met jouw opleiding ({JoinNl(levels)}) kun je deze richting aan, ook als de functietitel anders klinkt.";
    }

    private static string? DescribeDrivers(
        ProfileVacancyMatchInput input,
        double? competency01,
        double? interest01,
        VacancyOccupationMatch.Fit? occupationFit)
    {
        if (occupationFit is { } occ)
        {
            return $"Je drijfveren en beroepen-kompas wijzen naar {occ.Title.ToLowerInvariant()} — die lijn zie je terug in deze rol.";
        }

        if (competency01 is >= 0.65 && input.CandidateCompetencies is { IsComplete: true })
        {
            var tip = BestCompetencyLabel(input);
            return tip is null
                ? "Jouw competenties en werkstijl (Wie ben ik?) passen bij wat deze baan van je vraagt."
                : $"Jouw sterke {tip.ToLowerInvariant()} uit Wie ben ik? past bij wat deze baan vraagt.";
        }

        if (interest01 is >= 0.55)
        {
            return "Wat jij wilt in werk sluit inhoudelijk aan bij deze vacature, ook zonder exacte functietitel-match.";
        }

        if (input.CandidateCultureScores is { IsComplete: true }
            && CulturePersonalityFitRules.PersonalityFit01(
                input.CandidateCultureScores, input.VacancyTitle, input.VacancyDescription) >= 0.6)
        {
            return "Hoe jij graag werkt en in een team past, sluit aan bij deze functie.";
        }

        if (input.CandidateValuesScores is { IsComplete: true }
            && SchwartzValuesFitRules.Fit01(
                input.CandidateValuesScores, input.VacancyTitle, input.VacancyDescription) >= 0.6)
        {
            return "Jouw waarden en drijfveren sluiten aan bij wat deze vacature belooft.";
        }

        return null;
    }

    private static string? DescribeTransferable(ProfileVacancyMatchInput input, double experience01)
    {
        var shared = TransferableSkillRules.SharedDomainLabels(
            input.CandidateRoles,
            input.WorkTypes,
            input.VacancyTitle,
            input.VacancyDescription);
        if (shared.Count > 0)
        {
            return $"Overdraagbare ervaring in {JoinNl(shared)} maakt de overstap logisch.";
        }

        if (experience01 >= 0.5 && (input.CandidateRoles?.Count ?? 0) > 0)
        {
            return "Vaardigheden uit eerdere rollen zijn overdraagbaar naar deze vacature.";
        }

        return null;
    }

    private static string? BestCompetencyLabel(ProfileVacancyMatchInput input)
    {
        if (input.CandidateCompetencies is not { IsComplete: true } cand)
        {
            return null;
        }

        var targets = input.VacancyCompetencies ?? VacancyCompetencyProfile.Infer(
            input.WorkTypes, input.VacancyTitle, input.VacancyDescription);
        string? best = null;
        var bestDelta = int.MinValue;
        foreach (var category in CompetencyTestCatalog.CategoryCodes)
        {
            var delta = cand.Get(category) - targets.Get(category);
            if (delta >= -8 && delta > bestDelta)
            {
                bestDelta = delta;
                best = category;
            }
        }

        return best switch
        {
            CompetencyTestCatalog.Samenwerken => "samenwerken",
            CompetencyTestCatalog.Resultaatgerichtheid => "resultaatgerichtheid",
            CompetencyTestCatalog.Stressbestendigheid => "stressbestendigheid",
            CompetencyTestCatalog.Innovatie => "probleemoplossen",
            _ => null
        };
    }

    private static string JoinNl(IReadOnlyList<string> items)
        => items.Count switch
        {
            0 => "",
            1 => items[0].ToLowerInvariant(),
            2 => $"{items[0].ToLowerInvariant()} en {items[1].ToLowerInvariant()}",
            _ => string.Join(", ", items.Take(items.Count - 1).Select(i => i.ToLowerInvariant()))
                 + " en " + items[^1].ToLowerInvariant()
        };
}
