using System.Net.Http;
using System.Net.Http.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<ExternalVacancyDetailDto?> ImportExternalVacancyAsync(string url, CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync("api/candidate/external-vacancies/import", new ExternalVacancyImportRequest(url), ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Vacature toevoegen mislukt.");
        }

        return await ReadApiJsonAsync<ExternalVacancyDetailDto>(response.Content, ct);
    }

    public async Task<IReadOnlyList<ExternalVacancyListItemDto>> ListExternalVacanciesAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<List<ExternalVacancyListItemDto>>("api/candidate/external-vacancies", ct) ?? [];

    public async Task<ExternalVacancyDetailDto?> GetExternalVacancyAsync(Guid id, CancellationToken ct = default)
        => await GetApiJsonAsync<ExternalVacancyDetailDto>($"api/candidate/external-vacancies/{id:D}", ct);

    public async Task<ExternalVacancyApplyResultDto?> ApplyExternalVacancyAsync(
        Guid id,
        ExternalVacancyApplyRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync($"api/candidate/external-vacancies/{id:D}/apply", request, ct);
        return await ReadApiJsonAsync<ExternalVacancyApplyResultDto>(response.Content, ct);
    }

    public async Task DownloadExternalVacancyApplicationLetterAsync(
        Guid id,
        IJSRuntime js,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/candidate/external-vacancies/{id:D}/application-letter.pdf", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "PDF downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "sollicitatiebrief.pdf";
        await SendBrowserDownloadAsync(js, fileName, Convert.ToBase64String(bytes), "application/pdf");
    }

    public async Task<ExternalVacancyEmployerInviteDto?> GetExternalVacancyEmployerInviteAsync(
        string token,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var url = "api/public/external-vacancy/employer-invite?token=" + Uri.EscapeDataString(token.Trim());
        try
        {
            return await GetApiJsonAsync<ExternalVacancyEmployerInviteDto>(url, ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ExternalVacancyAdminMetricsDto?> GetExternalVacancyAdminMetricsAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<ExternalVacancyAdminMetricsDto>("api/admin/external-vacancy-metrics", ct);
}
