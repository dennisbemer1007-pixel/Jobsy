namespace Jobsy.Core.Rules;

public sealed record RoleFitCheckSnapshot(
    string JobTitle,
    int MatchPercent,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> ActionSteps,
    IReadOnlyList<string> SearchKeys,
    bool FromDeepAnalysis,
    bool FromOpenAi = false,
    RoleFitVacancyFit? VacancyFit = null,
    IReadOnlyList<RoleFitSimilarRole>? SimilarRoles = null,
    CareerPathPlan? CareerPath = null,
    IReadOnlyList<RoleFitStoredTrainingOffer>? TrainingOffers = null,
    IReadOnlyList<RoleFitStoredDirectVacancy>? DirectVacancies = null)
{
    public string MapQuery
    {
        get
        {
            if (SearchKeys.Count > 0)
            {
                return string.Join(' ', SearchKeys.Take(3));
            }

            return JobTitle;
        }
    }

    public bool ShowDeepUpsell => !FromDeepAnalysis;
}

public sealed record RoleFitVacancyFit(
    Guid VacancyId,
    string BarrierKind,
    int? CulturePercent,
    string? CultureBand,
    string? CultureLabel,
    string? CultureWhy,
    IReadOnlyList<VacancyBarrierCheckItem> FormalItems,
    bool AvailabilityOk,
    bool ShowFormalBlock,
    bool ShowUpskill);

public sealed record RoleFitSimilarRole(string Title, string Why, int FitPercent);

public sealed record RoleFitStoredTrainingOffer(
    Guid OfferId,
    string Title,
    string ProviderName,
    string Kind,
    string Network,
    string Region,
    string CtaLabel,
    string Advice);

public sealed record RoleFitStoredDirectVacancy(
    Guid Id,
    string Title,
    string CompanyName,
    int MatchPercent,
    string Href);

