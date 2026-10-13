using System.Net.Http.Json;
using Jobsy.Core.Contracts.Scholen;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<IReadOnlyList<TeacherAssignedClassDto>> GetTeacherClassesAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<List<TeacherAssignedClassDto>>("api/teacher/classes", ct) ?? [];

    public async Task<TeacherClassOverviewDto?> GetTeacherOverviewAsync(Guid classId, CancellationToken ct = default)
        => await GetApiJsonAsync<TeacherClassOverviewDto>($"api/teacher/classes/{classId}/overview", ct);

    public async Task<IReadOnlyList<TeacherCodeRowDto>> GetTeacherCodesAsync(Guid classId, CancellationToken ct = default)
        => await GetApiJsonAsync<List<TeacherCodeRowDto>>($"api/teacher/classes/{classId}/codes", ct) ?? [];

    public async Task<TeacherGroupInsightsDto?> GetTeacherGroupAsync(Guid classId, CancellationToken ct = default)
        => await GetApiJsonAsync<TeacherGroupInsightsDto>($"api/teacher/classes/{classId}/group", ct);

    public async Task<TeacherDreamJobsDto?> GetTeacherDreamJobsAsync(Guid classId, CancellationToken ct = default)
        => await GetApiJsonAsync<TeacherDreamJobsDto>($"api/teacher/classes/{classId}/dreamjobs", ct);

    public async Task<TeacherCodeDetailDto?> GetTeacherCodeDetailAsync(
        Guid classId,
        Guid codeId,
        CancellationToken ct = default)
        => await GetApiJsonAsync<TeacherCodeDetailDto>($"api/teacher/classes/{classId}/codes/{codeId}", ct);

    public async Task<SchoolPortalCodeRowDto?> ReplaceTeacherCodeAsync(
        Guid classId,
        Guid codeId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/teacher/classes/{classId}/codes/{codeId}/replace", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ActionError(body));
        }

        return await ReadApiJsonAsync<SchoolPortalCodeRowDto>(response.Content, ct);
    }

    public async Task<SchoolPortalClassDetailDto?> SetTeacherTestWindowAsync(
        Guid classId,
        string action,
        DateOnly? closesOn = null,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/teacher/classes/{classId}/test-window",
            new TestWindowRequest(action, closesOn),
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ActionError(body));
        }

        return await ReadApiJsonAsync<SchoolPortalClassDetailDto>(response.Content, ct);
    }

    public Task DownloadTeacherCodeListPdfAsync(IJSRuntime js, Guid classId, CancellationToken ct = default)
        => DownloadNamedFileAsync($"api/teacher/classes/{classId}/codelist.pdf", js, "codelijst.pdf", ct);

    public Task DownloadTeacherCodeListCsvAsync(IJSRuntime js, Guid classId, CancellationToken ct = default)
        => DownloadNamedFileAsync($"api/teacher/classes/{classId}/codelist.csv", js, "codelijst.csv", ct);

    public async Task DownloadTeacherPupilReportPdfAsync(
        IJSRuntime js,
        Guid classId,
        Guid codeId,
        string className,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/teacher/classes/{classId}/codes/{codeId}/report.pdf", ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var safe = new string((className ?? "klas")
            .Select(c => c is ' ' or '/' or '.' or '+' ? '-' : c)
            .Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            .ToArray()).Trim('-');
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = "klas";
        }

        await SendBrowserDownloadAsync(
            js,
            $"lobsy-ontdekkingsreis-{safe.ToLowerInvariant()}.pdf",
            Convert.ToBase64String(bytes),
            "application/pdf");
    }
}
