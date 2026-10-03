using System.Net.Http.Json;
using Jobsy.Core.Contracts.Scholen;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<SchoolDashboardDto?> GetSchoolDashboardAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<SchoolDashboardDto>("api/school/dashboard", ct);

    public async Task<IReadOnlyList<SchoolTodoItemDto>> GetSchoolTodosAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<List<SchoolTodoItemDto>>("api/school/todos", ct) ?? [];

    public async Task<SchoolProfileDto?> GetSchoolProfileAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<SchoolProfileDto>("api/school/profile", ct);

    public async Task<SchoolPrivacyDto?> GetSchoolPrivacyAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<SchoolPrivacyDto>("api/school/privacy", ct);

    public async Task DeleteSchoolYearDataAsync(string confirmPhrase, CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            "api/school/privacy/delete-year",
            new DeleteSchoolYearRequest(confirmPhrase),
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
    }

    public async Task<IReadOnlyList<SchoolPortalClassListItemDto>> GetSchoolClassesAsync(
        int? schoolYearStart = null,
        CancellationToken ct = default)
    {
        var url = schoolYearStart is int y
            ? $"api/school/classes?schoolYearStart={y}"
            : "api/school/classes";
        return await GetApiJsonAsync<List<SchoolPortalClassListItemDto>>(url, ct) ?? [];
    }

    public async Task<SchoolPortalClassDetailDto?> GetSchoolClassAsync(Guid classId, CancellationToken ct = default)
        => await GetApiJsonAsync<SchoolPortalClassDetailDto>($"api/school/classes/{classId}", ct);

    public async Task<SchoolPortalClassDetailDto?> CreateSchoolClassAsync(
        CreateSchoolClassRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync("api/school/classes", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await ReadApiJsonAsync<SchoolPortalClassDetailDto>(response.Content, ct);
    }

    public async Task<SchoolPortalClassDetailDto?> UpdateSchoolClassAsync(
        Guid classId,
        UpdateSchoolClassRequest request,
        CancellationToken ct = default)
    {
        var response = await PutApiJsonAsync($"api/school/classes/{classId}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await ReadApiJsonAsync<SchoolPortalClassDetailDto>(response.Content, ct);
    }

    public async Task DeleteSchoolClassAsync(Guid classId, string confirmName, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync(
            $"api/school/classes/{classId}?confirmName={Uri.EscapeDataString(confirmName)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
    }

    public async Task<SchoolPortalClassDetailDto?> AddSchoolClassCodesAsync(
        Guid classId,
        int count,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/school/classes/{classId}/codes", new AddCodesRequest(count), ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await ReadApiJsonAsync<SchoolPortalClassDetailDto>(response.Content, ct);
    }

    public async Task<SchoolPortalCodeRowDto?> ReplaceSchoolCodeAsync(
        Guid classId,
        Guid codeId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/school/classes/{classId}/codes/{codeId}/replace", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await ReadApiJsonAsync<SchoolPortalCodeRowDto>(response.Content, ct);
    }

    public async Task DeleteSchoolCodeAsync(Guid classId, Guid codeId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/school/classes/{classId}/codes/{codeId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
    }

    public async Task DownloadSchoolCodeListPdfAsync(IJSRuntime js, Guid classId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/school/classes/{classId}/codelist.pdf", ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        await SendBrowserDownloadAsync(js, $"codelijst-{classId:N}.pdf", Convert.ToBase64String(bytes), "application/pdf");
    }

    public async Task DownloadSchoolCodeListCsvAsync(IJSRuntime js, Guid classId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/school/classes/{classId}/codelist.csv", ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        await SendBrowserDownloadAsync(
            js, $"codelijst-{classId:N}.csv", Convert.ToBase64String(bytes), "text/csv;charset=utf-8");
    }

    public async Task<SchoolPortalClassDetailDto?> ConfirmSchoolParentalAsync(
        Guid classId,
        bool confirmed,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/school/classes/{classId}/parental-confirmation",
            new ParentalConfirmationRequest(confirmed),
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await ReadApiJsonAsync<SchoolPortalClassDetailDto>(response.Content, ct);
    }

    public async Task<SchoolPortalClassDetailDto?> SetSchoolTestWindowAsync(
        Guid classId,
        string action,
        DateOnly? closesOn = null,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/school/classes/{classId}/test-window",
            new TestWindowRequest(action, closesOn),
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await ReadApiJsonAsync<SchoolPortalClassDetailDto>(response.Content, ct);
    }

    public async Task<SchoolPortalResultsDto?> GetSchoolClassResultsAsync(
        Guid classId,
        CancellationToken ct = default)
        => await GetApiJsonAsync<SchoolPortalResultsDto>($"api/school/classes/{classId}/results", ct);

    public async Task<IReadOnlyList<SchoolPortalTeacherListItemDto>> GetSchoolTeachersAsync(
        Guid? classId = null,
        CancellationToken ct = default)
    {
        var url = classId is Guid id
            ? $"api/school/teachers?classId={id}"
            : "api/school/teachers";
        return await GetApiJsonAsync<List<SchoolPortalTeacherListItemDto>>(url, ct) ?? [];
    }

    public async Task<SchoolStaffInviteResultDto?> InviteSchoolTeacherAsync(
        InviteTeacherRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync("api/school/teachers", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await ReadApiJsonAsync<SchoolStaffInviteResultDto>(response.Content, ct);
    }

    public async Task AssignSchoolTeacherClassesAsync(
        Guid teacherUserId,
        IReadOnlyList<Guid> classIds,
        CancellationToken ct = default)
    {
        var response = await PutApiJsonAsync(
            $"api/school/teachers/{teacherUserId}/classes",
            new AssignTeacherClassesRequest(classIds),
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
    }

    public async Task ResendSchoolTeacherInviteAsync(Guid teacherUserId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/school/teachers/{teacherUserId}/resend-invite", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
    }

    public async Task RemoveSchoolTeacherAsync(Guid teacherUserId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/school/teachers/{teacherUserId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
    }
}
