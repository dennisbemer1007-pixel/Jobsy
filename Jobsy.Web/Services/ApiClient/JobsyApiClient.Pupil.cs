using System.Net.Http.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<IReadOnlyList<PupilSchoolOptionDto>> GetPupilSchoolsAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<List<PupilSchoolOptionDto>>("api/pupil/schools", ct) ?? [];

    public async Task<IReadOnlyList<PupilClassOptionDto>?> GetPupilClassesAsync(
        Guid schoolId,
        CancellationToken ct = default)
        => await GetApiJsonAsync<List<PupilClassOptionDto>>(
            $"api/pupil/schools/{schoolId:D}/classes", ct);

    public async Task<PupilProgressStateDto?> GetPupilProgressAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<PupilProgressStateDto>("api/pupil/progress", ct);

    public async Task<PupilAnswerResponse?> SavePupilAnswerAsync(
        string itemId,
        int value,
        CancellationToken ct = default)
    {
        using var response = await PutApiJsonAsync(
            $"api/pupil/progress/answers/{Uri.EscapeDataString(itemId)}",
            new PupilAnswerRequest(value),
            ct);
        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<PupilAnswerResponse>(response.Content, ct);
    }

    public async Task<PupilChipsResponse?> SavePupilChipsAsync(
        PupilChipsRequest request,
        CancellationToken ct = default)
    {
        using var response = await PutApiJsonAsync("api/pupil/progress/chips", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await ReadApiJsonAsync<PupilChipsResponse>(response.Content, ct);
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(body, null, response.StatusCode);
    }

    public async Task<PupilPuzzleResponse?> SavePupilPuzzleAsync(
        string puzzleKey,
        PupilPuzzleRequest request,
        CancellationToken ct = default)
    {
        using var response = await PutApiJsonAsync(
            $"api/pupil/progress/puzzles/{Uri.EscapeDataString(puzzleKey)}",
            request,
            ct);
        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<PupilPuzzleResponse>(response.Content, ct);
    }

    public async Task<PupilResultPageDto?> GetPupilResultAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<PupilResultPageDto>("api/pupil/result", ct);

    /// <summary>Raw result response so callers can handle 409 <c>not_completed</c> / result-pending.</summary>
    public Task<HttpResponseMessage> GetPupilResultResponseAsync(CancellationToken ct = default)
        => _http.GetAsync("api/pupil/result", ct);

    public async Task<PupilDreamJobResponse?> SavePupilDreamJobAsync(string key, CancellationToken ct = default)
    {
        using var response = await PutApiJsonAsync("api/pupil/dreamjob", new PupilDreamJobRequest(key), ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await ReadApiJsonAsync<PupilDreamJobResponse>(response.Content, ct);
    }

    public async Task<PupilG78PassportWebDto?> GetPupilG78PassportAsync(
        string? displayName = null,
        string? language = null,
        CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            query.Add($"naam={Uri.EscapeDataString(displayName)}");
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            query.Add($"lang={Uri.EscapeDataString(language)}");
        }

        var q = query.Count == 0
            ? "api/pupil/passport/g78"
            : $"api/pupil/passport/g78?{string.Join("&", query)}";
        return await GetApiJsonAsync<PupilG78PassportWebDto>(q, ct);
    }

    public async Task<PupilVoPassportDto?> GetPupilVoPassportAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<PupilVoPassportDto>("api/pupil/vo-passport", ct);

    public async Task<PupilVoPassportDto?> SavePupilVoPassportAsync(
        PupilVoPassportSaveRequest request,
        CancellationToken ct = default)
    {
        using var response = await PutApiJsonAsync("api/pupil/vo-passport", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await ReadApiJsonAsync<PupilVoPassportDto>(response.Content, ct);
    }

    /// <summary>
    /// Story PDF from the API (<c>GET /leerling/pdf</c>). The web host proxies this
    /// so the pupil link does not 404 on the web origin.
    /// </summary>
    public async Task<(byte[]? Bytes, string FileName, int StatusCode)> GetPupilStoryPdfAsync(
        string? queryString = null,
        CancellationToken ct = default)
    {
        var path = string.IsNullOrWhiteSpace(queryString) ? "leerling/pdf" : $"leerling/pdf{queryString}";
        using var response = await _http.GetAsync(path, ct);
        if (!response.IsSuccessStatusCode)
        {
            return (null, "lobsy-verhaal.pdf", (int)response.StatusCode);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? "lobsy-verhaal.pdf";
        return (bytes, fileName, (int)response.StatusCode);
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
