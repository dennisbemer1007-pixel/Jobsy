using Jobsy.Web.Services;

namespace Jobsy.Web.Services.Careers;

/// <summary>
/// Vacancy fit for one career step (Dependency E). <see cref="GateOpen"/> mirrors
/// <c>CandidateFitGate.IsOpen</c>: without a finished culture or values test there is no count.
/// </summary>
public readonly record struct CareerStepVacancyFit(bool GateOpen, int GoodCount)
{
    public static CareerStepVacancyFit None { get; } = new(false, 0);

    /// <summary>The count line is only allowed with an open gate and at least one match (03 §2).</summary>
    public bool ShowsCount => GateOpen && GoodCount > 0;
}

/// <summary>Seam so the step detail can be rendered with a fake in tests.</summary>
public interface ICareerStepVacancyFit
{
    Task<CareerStepVacancyFit> GetAsync(
        IReadOnlyList<string> stepKeys,
        CancellationToken ct = default);
}

/// <summary>
/// Counts the candidate's matched vacancies whose title matches the step keys and whose band is
/// "good" (the kandidaat-banen green band). Read-only: no seeding, no new endpoint.
/// </summary>
public sealed class CareerStepVacancyFitService : ICareerStepVacancyFit
{
    private const string GoodBand = "green";

    private readonly JobsyApiClient _api;
    private readonly CandidateMatchProfileService _gate;

    public CareerStepVacancyFitService(JobsyApiClient api, CandidateMatchProfileService gate)
    {
        _api = api;
        _gate = gate;
    }

    public async Task<CareerStepVacancyFit> GetAsync(
        IReadOnlyList<string> stepKeys,
        CancellationToken ct = default)
    {
        var keys = (stepKeys ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .ToList();

        if (keys.Count == 0)
        {
            return CareerStepVacancyFit.None;
        }

        bool gateOpen;
        try
        {
            var gate = await _gate.RefreshAsync(ct);
            gateOpen = gate.FitGateOpen;
        }
        catch (Exception)
        {
            return CareerStepVacancyFit.None;
        }

        if (!gateOpen)
        {
            return new CareerStepVacancyFit(GateOpen: false, GoodCount: 0);
        }

        try
        {
            var matched = await _api.GetMyMatchedVacanciesAsync(ct);
            var count = matched.Count(v =>
                string.Equals(v.ColorBand, GoodBand, StringComparison.OrdinalIgnoreCase)
                && keys.Any(k => v.Title.Contains(k, StringComparison.OrdinalIgnoreCase)));

            return new CareerStepVacancyFit(GateOpen: true, GoodCount: count);
        }
        catch (Exception)
        {
            return new CareerStepVacancyFit(GateOpen: true, GoodCount: 0);
        }
    }
}
