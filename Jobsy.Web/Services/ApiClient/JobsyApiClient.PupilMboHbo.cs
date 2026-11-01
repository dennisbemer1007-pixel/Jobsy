using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<PupilMboHboOverviewDto?> GetMboHboOverviewAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<PupilMboHboOverviewDto>("api/pupil/mbo-hbo/overview", ct);

    public async Task<PupilMboHboIntroResponse?> MarkMboHboIntroSeenAsync(CancellationToken ct = default)
    {
        using var response = await PostApiJsonAsync("api/pupil/mbo-hbo/intro", new { }, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PupilMboHboIntroResponse>(cancellationToken: ct);
    }

    public async Task<PupilMboHboQuestionDto?> GetMboHboQuestionAsync(
        string blockKey,
        string? itemId = null,
        CancellationToken ct = default)
    {
        var q = string.IsNullOrWhiteSpace(itemId)
            ? ""
            : $"?itemId={Uri.EscapeDataString(itemId)}";
        return await GetApiJsonAsync<PupilMboHboQuestionDto>(
            $"api/pupil/mbo-hbo/blocks/{Uri.EscapeDataString(blockKey)}/question{q}",
            ct);
    }

    public async Task<PupilMboHboAnswerResponse?> SaveMboHboAnswerAsync(
        string blockKey,
        string itemId,
        int value,
        CancellationToken ct = default)
    {
        using var response = await PutApiJsonAsync(
            $"api/pupil/mbo-hbo/blocks/{Uri.EscapeDataString(blockKey)}/answers/{Uri.EscapeDataString(itemId)}",
            new PupilMboHboAnswerRequest(value),
            ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PupilMboHboAnswerResponse>(cancellationToken: ct);
    }

    public async Task<PupilMboHboAnswerResponse?> SaveMboHboVakAnswerAsync(
        string itemId,
        int leuk,
        int goed,
        CancellationToken ct = default)
    {
        using var response = await PutApiJsonAsync(
            $"api/pupil/mbo-hbo/blocks/vakken/answers/{Uri.EscapeDataString(itemId)}",
            new PupilMboHboVakAnswerRequest(leuk, goed),
            ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PupilMboHboAnswerResponse>(cancellationToken: ct);
    }

    public async Task<PupilMboHboOutcomeDto?> GetMboHboOutcomeAsync(string blockKey, CancellationToken ct = default)
        => await GetApiJsonAsync<PupilMboHboOutcomeDto>(
            $"api/pupil/mbo-hbo/blocks/{Uri.EscapeDataString(blockKey)}/outcome",
            ct);

    public async Task<PupilMboHboPassportDto?> GetMboHboPassportAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<PupilMboHboPassportDto>("api/pupil/mbo-hbo/passport", ct);

    public async Task<TeacherMboHboClassProgressDto?> GetTeacherMboHboProgressAsync(
        Guid classId,
        CancellationToken ct = default)
        => await GetApiJsonAsync<TeacherMboHboClassProgressDto>(
            $"api/teacher/classes/{classId:D}/mbo-hbo-progress",
            ct);
}
