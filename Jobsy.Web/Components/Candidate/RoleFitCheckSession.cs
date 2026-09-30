using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Components.Candidate;

/// <summary>
/// Shared load / evaluate state for <see cref="Pages.Candidate.RoleFitCheckPanel"/> and passport Fit tab.
/// Markup stays in each view; unlock rules and API calls live here.
/// </summary>
public sealed class RoleFitCheckSession
{
    private bool _loadStarted;

    public bool Loading { get; private set; } = true;
    public bool Busy { get; private set; }
    public string? Message { get; private set; }
    public string JobTitle { get; set; } = "";
    public RoleFitCheckState? State { get; private set; }
    public Guid? ActiveVacancyId { get; set; }

    public bool IsUnlocked => State is { IsUnlocked: true };
    public RoleFitCheckResult? LastResult => State?.LastResult;
    public bool NeedsRecheck => State?.NeedsRecheck == true;
    public string LockMessage => State?.LockMessage ?? "";

    public async Task LoadAsync(
        JobsyApiClient api,
        Guid? vacancyId = null,
        string? errorFallback = null,
        bool force = false)
    {
        if (!force && _loadStarted && State is not null)
        {
            return;
        }

        _loadStarted = true;
        Loading = true;
        Message = null;
        if (vacancyId is Guid vid)
        {
            ActiveVacancyId = vid;
        }

        try
        {
            State = await api.GetMyRoleFitAsync();
            // Prefill vacancy title only — never auto-POST role-fit (AI or scoring).
            if (vacancyId is Guid
                && State?.LastResult is { VacancyId: Guid } lastVacancy
                && lastVacancy.VacancyId == vacancyId)
            {
                JobTitle = lastVacancy.JobTitle;
            }
            else if (State?.LastResult is { } last && string.IsNullOrWhiteSpace(JobTitle))
            {
                JobTitle = last.JobTitle;
            }
            else if (vacancyId is Guid && string.IsNullOrWhiteSpace(JobTitle))
            {
                ActiveVacancyId = vacancyId;
            }
        }
        catch
        {
            Message = errorFallback;
        }
        finally
        {
            Loading = false;
        }
    }

    public Task EvaluateAsync(JobsyApiClient api, string? errorFallback = null)
        => RunEvaluateAsync(api, JobTitle, ActiveVacancyId, errorFallback);

    public Task EvaluateSimilarAsync(JobsyApiClient api, string title, string? errorFallback = null)
    {
        JobTitle = title;
        ActiveVacancyId = null;
        return RunEvaluateAsync(api, title, vacancyId: null, errorFallback);
    }

    private async Task RunEvaluateAsync(
        JobsyApiClient api,
        string title,
        Guid? vacancyId,
        string? errorFallback)
    {
        Busy = true;
        Message = null;
        try
        {
            State = await api.EvaluateRoleFitAsync(title, vacancyId);
        }
        catch (Exception ex)
        {
            Message = string.IsNullOrWhiteSpace(ex.Message) ? errorFallback : ex.Message;
        }
        finally
        {
            Busy = false;
        }
    }

    public void SetMessage(string? message) => Message = message;
}
