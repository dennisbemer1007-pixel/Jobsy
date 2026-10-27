using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Contracts;

namespace Jobsy.Web.Services;

public partial class JobsyApiClient
{
    public async Task<KandidatenkaartMapDto?> GetKandidatenkaartMapAsync(
        Guid? companyId,
        Guid? vacancyId,
        int minMatch,
        int travelKm,
        string? availability,
        string? hours,
        CancellationToken ct = default)
    {
        var qs = $"minMatch={minMatch}&travelKm={travelKm}";
        if (companyId is Guid c) qs += $"&companyId={c:D}";
        if (vacancyId is Guid v) qs += $"&vacancyId={v:D}";
        if (!string.IsNullOrWhiteSpace(availability)) qs += $"&availability={Uri.EscapeDataString(availability)}";
        if (!string.IsNullOrWhiteSpace(hours)) qs += $"&hours={Uri.EscapeDataString(hours)}";
        try
        {
            var response = await _http.GetAsync($"api/employer/kandidatenkaart/map?{qs}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var err = await response.Content.ReadFromJsonAsync<KandidatenkaartApiError>(ApiJson, ct);
                if (string.Equals(err?.Code, "vacancy_required", StringComparison.Ordinal))
                {
                    return null;
                }
            }

            response.EnsureSuccessStatusCode();
            return await ReadApiJsonAsync<KandidatenkaartMapDto>(response.Content, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<KandidatenkaartVacancyOptionDto>> GetKandidatenkaartVacanciesAsync(
        Guid? companyId,
        CancellationToken ct = default)
    {
        var qs = companyId is Guid c ? $"?companyId={c:D}" : "";
        return await GetApiJsonAsync<List<KandidatenkaartVacancyOptionDto>>(
                   $"api/employer/kandidatenkaart/vacancies{qs}", ct)
               ?? [];
    }

    public async Task<KandidatenkaartInvitePreviewDto?> PreviewKandidatenkaartInviteAsync(
        Guid vacancyId,
        string areaKey,
        Guid? companyId,
        int minMatch,
        int travelKm,
        CancellationToken ct = default)
    {
        var qs = $"vacancyId={vacancyId:D}&areaKey={Uri.EscapeDataString(areaKey)}&minMatch={minMatch}&travelKm={travelKm}";
        if (companyId is Guid c) qs += $"&companyId={c:D}";
        return await GetApiJsonAsync<KandidatenkaartInvitePreviewDto>(
            $"api/employer/kandidatenkaart/invite/preview?{qs}", ct);
    }

    public async Task<KandidatenkaartInviteResultDto?> StartKandidatenkaartInviteAsync(
        KandidatenkaartInviteClientRequest body,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync("api/employer/kandidatenkaart/invite", body, ct);
        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<KandidatenkaartInviteResultDto>(response.Content, ct);
    }

    public async Task<CandidateKandidatenkaartSettingsDto?> GetCandidateKandidatenkaartSettingsAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<CandidateKandidatenkaartSettingsDto>("api/me/kandidatenkaart/settings", ct);

    public async Task<CandidateKandidatenkaartSettingsDto?> UpdateCandidateKandidatenkaartSettingsAsync(
        CandidateKandidatenkaartSettingsUpdate update,
        CancellationToken ct = default)
    {
        var response = await PutApiJsonAsync("api/me/kandidatenkaart/settings", update, ct);
        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<CandidateKandidatenkaartSettingsDto>(response.Content, ct);
    }

    public async Task AckKandidatenkaartReAvailabilityAsync(bool becomeAvailable, CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            "api/me/kandidatenkaart/re-availability",
            new KandidatenkaartReAvailabilityAck(becomeAvailable),
            ct);
        response.EnsureSuccessStatusCode();
    }

    private sealed record KandidatenkaartApiError(string? Code, string? Message);
}

public sealed record KandidatenkaartInviteClientRequest(
    Guid VacancyId,
    string AreaKey,
    Guid? CompanyId,
    int MinMatchPercent = 70,
    int TravelKm = 25);
