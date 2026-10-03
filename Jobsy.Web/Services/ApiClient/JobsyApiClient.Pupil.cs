using System.Net.Http.Json;
using Jobsy.Core.Contracts.Scholen;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<IReadOnlyList<PupilSchoolOptionDto>> GetPupilSchoolsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<PupilSchoolOptionDto>>("api/pupil/schools", ct) ?? [];

    public async Task<IReadOnlyList<PupilClassOptionDto>?> GetPupilClassesAsync(
        Guid schoolId,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<PupilClassOptionDto>>(
            $"api/pupil/schools/{schoolId:D}/classes", ct);

    public async Task<PupilProgressStateDto?> GetPupilProgressAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", ct);

    public async Task<PupilAnswerResponse?> SavePupilAnswerAsync(
        string itemId,
        int value,
        CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync(
            $"api/pupil/progress/answers/{Uri.EscapeDataString(itemId)}",
            new PupilAnswerRequest(value),
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PupilAnswerResponse>(cancellationToken: ct);
    }

    public async Task<PupilChipsResponse?> SavePupilChipsAsync(
        PupilChipsRequest request,
        CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync("api/pupil/progress/chips", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<PupilChipsResponse>(cancellationToken: ct);
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(body, null, response.StatusCode);
    }

    public async Task<PupilResultPageDto?> GetPupilResultAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<PupilResultPageDto>("api/pupil/result", ct);

    /// <summary>Raw result response so callers can handle 409 <c>not_completed</c> / result-pending.</summary>
    public Task<HttpResponseMessage> GetPupilResultResponseAsync(CancellationToken ct = default)
        => _http.GetAsync("api/pupil/result", ct);

    public async Task<PupilDreamJobResponse?> SavePupilDreamJobAsync(string key, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync("api/pupil/dreamjob", new PupilDreamJobRequest(key), ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PupilDreamJobResponse>(cancellationToken: ct);
    }

    public async Task ClearTeacherLoginPauseAsync(Guid classId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync(
            $"api/teacher/classes/{classId:D}/login-pause/clear",
            null,
            ct);
        response.EnsureSuccessStatusCode();
    }
}
