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
    RoleFitVacancyFit? VacancyFit = null)
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

