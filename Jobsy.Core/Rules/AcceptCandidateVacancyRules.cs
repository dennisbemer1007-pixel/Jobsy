using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Rules;

/// <summary>
/// Stage (internship) and volunteer vacancies: free accept, no Maqqie route (hours/refund).
/// Paid job vacancies use pilot/standard accept pricing and may offer Zelf/Maqqie.
/// </summary>
public static class AcceptCandidateVacancyRules
{
    public static bool IsVolunteerOrInternship(VacancyKind kind)
        => kind is VacancyKind.Internship or VacancyKind.Volunteer;

    public static bool ChargesTokensOnAccept(VacancyKind kind)
        => !IsVolunteerOrInternship(kind);

    public static bool SupportsEmploymentModeChoice(VacancyKind kind)
        => ChargesTokensOnAccept(kind);

    public static decimal ResolveAcceptCostTokens(
        VacancyKind kind,
        FlexCommercialSettings settings,
        DateOnly todayUtc)
        => ChargesTokensOnAccept(kind)
            ? AcceptCandidatePricingRules.ResolveCostTokens(settings, todayUtc)
            : 0m;

    public static decimal ResolveAcceptCostTokens(
        VacancyKind kind,
        FlexCommercialSettingsDto settings,
        DateOnly todayUtc)
        => ChargesTokensOnAccept(kind)
            ? AcceptCandidatePricingRules.ResolveCostTokens(settings, todayUtc)
            : 0m;
}
