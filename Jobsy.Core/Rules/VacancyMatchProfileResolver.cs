using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Resolves effective vacancy-side match inputs (§3.4–3.6): explicit employer answers override infer.
/// </summary>
public static class VacancyMatchProfileResolver
{
    public sealed record Resolved(
        CompetencyScores VacancyCompetencies,
        bool VacancyCompetenciesExplicit,
        CulturePersonalityScores? EffectiveCulture,
        bool CultureExplicit,
        VacancyWorkContextSnapshot? WorkContext,
        VacancyMatchExtrasSnapshot? Extras);

    public static Resolved Resolve(
        IReadOnlyList<string>? workTypes,
        string? title,
        string? description,
        VacancyMatchExtrasSnapshot? extras,
        CulturePersonalityScores? companyCulture,
        IReadOnlyList<string>? culturePillars)
    {
        extras ??= VacancyMatchExtrasSnapshot.Empty;

        var inferred = VacancyCompetencyProfile.Infer(workTypes, title, description);
        var competencies = ResolveCompetencies(inferred, extras);
        var explicitComp = !extras.UseInferredCompetencies
                           && extras.CompetencyTargets is not null
                           && HasAnyCompetencyTarget(extras.CompetencyTargets);

        var culture = ResolveCulture(companyCulture, extras.CultureDelta, culturePillars);
        var cultureExplicit = extras.CultureDelta is { DiffersFromCompany: true, DimensionPercents.Count: > 0 }
                              || (companyCulture?.Autonomy is not null && !CulturePillarCatalog.HasProfile(culturePillars));

        return new Resolved(
            competencies,
            explicitComp,
            culture,
            cultureExplicit,
            extras.WorkContext,
            extras);
    }

    public static VacancyMatchExtrasSnapshot ParseExtras(
        string? workContextJson,
        string? competencyTargetsJson,
        string? cultureDeltaJson,
        bool useInferredCompetencies)
        => VacancyMatchExtrasJson.Parse(
               workContextJson,
               competencyTargetsJson,
               cultureDeltaJson,
               useInferredCompetencies)
           ?? VacancyMatchExtrasSnapshot.Empty;

    private static CompetencyScores ResolveCompetencies(
        CompetencyScores inferred,
        VacancyMatchExtrasSnapshot extras)
    {
        if (extras.UseInferredCompetencies || extras.CompetencyTargets is not { } t)
        {
            return inferred;
        }

        return new CompetencyScores(
            t.Samenwerken ?? inferred.Samenwerken ?? 60,
            t.Resultaatgerichtheid ?? inferred.Resultaatgerichtheid ?? 60,
            t.Stressbestendigheid ?? inferred.Stressbestendigheid ?? 60,
            t.Innovatie ?? inferred.Innovatie ?? 60);
    }

    private static bool HasAnyCompetencyTarget(VacancyCompetencyTargetsSnapshot t)
        => t.Samenwerken is not null
           || t.Resultaatgerichtheid is not null
           || t.Stressbestendigheid is not null
           || t.Innovatie is not null;

    private static CulturePersonalityScores? ResolveCulture(
        CulturePersonalityScores? companyCulture,
        VacancyCultureDeltaSnapshot? delta,
        IReadOnlyList<string>? pillars)
    {
        if (CulturePillarCatalog.HasProfile(pillars))
        {
            return null;
        }

        if (companyCulture is null)
        {
            return null;
        }

        if (delta is not { DiffersFromCompany: true } || delta.DimensionPercents is not { Count: > 0 })
        {
            return companyCulture;
        }

        int Pick(string code, int fallback) =>
            delta.DimensionPercents.TryGetValue(code, out var v) ? Math.Clamp(v, 0, 100) : fallback;

        return new CulturePersonalityScores(
            Pick("Autonomy", companyCulture.Autonomy ?? 50),
            Pick("Informal", companyCulture.Informal ?? 50),
            Pick("Collaboration", companyCulture.Collaboration ?? 50),
            Pick("Flexibility", companyCulture.Flexibility ?? 50),
            Pick("Innovation", companyCulture.Innovation ?? 50),
            Pick("PeopleFirst", companyCulture.PeopleFirst ?? 50),
            companyCulture.Openness,
            companyCulture.Conscientiousness,
            companyCulture.Extraversion,
            companyCulture.Agreeableness,
            companyCulture.EmotionalStability);
    }
}
