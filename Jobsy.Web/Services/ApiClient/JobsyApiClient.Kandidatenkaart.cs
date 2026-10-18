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
            return await _http.GetFromJsonAsync<KandidatenkaartMapDto>($"api/employer/kandidatenkaart/map?{qs}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<KandidatenkaartVacancyOptionDto>> GetKandidatenkaartVacanciesAsync(
        Guid? companyId,
        CancellationToken ct = default)
    {
        var qs = companyId is Guid c ? $"?companyId={c:D}" : "";
        return await _http.GetFromJsonAsync<List<KandidatenkaartVacancyOptionDto>>(
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
        return await _http.GetFromJsonAsync<KandidatenkaartInvitePreviewDto>(
            $"api/employer/kandidatenkaart/invite/preview?{qs}", ct);
    }

    public async Task<KandidatenkaartInviteResultDto?> StartKandidatenkaartInviteAsync(
        KandidatenkaartInviteClientRequest body,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/employer/kandidatenkaart/invite", body, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KandidatenkaartInviteResultDto>(cancellationToken: ct);
    }

    public async Task<CandidateKandidatenkaartSettingsDto?> GetCandidateKandidatenkaartSettingsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<CandidateKandidatenkaartSettingsDto>("api/me/kandidatenkaart/settings", ct);

    public async Task<CandidateKandidatenkaartSettingsDto?> UpdateCandidateKandidatenkaartSettingsAsync(
        CandidateKandidatenkaartSettingsUpdate update,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/kandidatenkaart/settings", update, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CandidateKandidatenkaartSettingsDto>(cancellationToken: ct);
    }

    public async Task AckKandidatenkaartReAvailabilityAsync(bool becomeAvailable, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/me/kandidatenkaart/re-availability",
            new KandidatenkaartReAvailabilityAck(becomeAvailable),
            ct);
        response.EnsureSuccessStatusCode();
    }
}

public sealed record KandidatenkaartInviteClientRequest(
    Guid VacancyId,
    string AreaKey,
    Guid? CompanyId,
    int MinMatchPercent = 70,
    int TravelKm = 25);
