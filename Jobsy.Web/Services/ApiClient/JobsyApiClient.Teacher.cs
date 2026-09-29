using System.Net.Http.Json;
using Jobsy.Core.Contracts.Scholen;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<IReadOnlyList<TeacherAssignedClassDto>> GetTeacherClassesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<TeacherAssignedClassDto>>("api/teacher/classes", ct) ?? [];

    public async Task<TeacherClassOverviewDto?> GetTeacherOverviewAsync(Guid classId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<TeacherClassOverviewDto>($"api/teacher/classes/{classId}/overview", ct);

    public async Task<IReadOnlyList<TeacherCodeRowDto>> GetTeacherCodesAsync(Guid classId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<TeacherCodeRowDto>>($"api/teacher/classes/{classId}/codes", ct) ?? [];

    public async Task<TeacherGroupInsightsDto?> GetTeacherGroupAsync(Guid classId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<TeacherGroupInsightsDto>($"api/teacher/classes/{classId}/group", ct);

    public async Task<TeacherDreamJobsDto?> GetTeacherDreamJobsAsync(Guid classId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<TeacherDreamJobsDto>($"api/teacher/classes/{classId}/dreamjobs", ct);

    public async Task<TeacherCodeDetailDto?> GetTeacherCodeDetailAsync(
        Guid classId,
        Guid codeId,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<TeacherCodeDetailDto>($"api/teacher/classes/{classId}/codes/{codeId}", ct);

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
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<SchoolPortalCodeRowDto>(cancellationToken: ct);
    }

    public async Task<SchoolPortalClassDetailDto?> SetTeacherTestWindowAsync(
        Guid classId,
        string action,
        DateOnly? closesOn = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/teacher/classes/{classId}/test-window",
            new TestWindowRequest(action, closesOn),
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<SchoolPortalClassDetailDto>(cancellationToken: ct);
    }

    public async Task DownloadTeacherCodeListPdfAsync(IJSRuntime js, Guid classId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/teacher/classes/{classId}/codelist.pdf", ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        await SendBrowserDownloadAsync(js, $"codelijst-{classId:N}.pdf", Convert.ToBase64String(bytes), "application/pdf");
    }

    public async Task DownloadTeacherCodeListCsvAsync(IJSRuntime js, Guid classId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/teacher/classes/{classId}/codelist.csv", ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        await SendBrowserDownloadAsync(
            js, $"codelijst-{classId:N}.csv", Convert.ToBase64String(bytes), "text/csv;charset=utf-8");
    }
}
