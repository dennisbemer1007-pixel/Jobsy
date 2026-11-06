using System.Net.Http.Json;
using Jobsy.Core.Contracts;

namespace Jobsy.Web.Services;

public partial class JobsyApiClient
{
    public async Task<PublicCompanyInitiativeViewDto?> GetPublicInitiativeProfileAsync(string kvk, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/company-map-initiative/public/{Uri.EscapeDataString(kvk)}/profile",
            ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await ReadApiJsonAsync<PublicCompanyInitiativeViewDto>(response.Content, ct);
    }

    public async Task<bool> AcceptWorkInitiativeAsync(Guid id, Guid billingCompanyId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/company-map-initiative/employer/{id:D}/accept?billingCompanyId={billingCompanyId:D}",
            null,
            ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<CompanyMapPinDto>> GetCompanyMapPinsAsync(
        double? lat,
        double? lng,
        int radiusKm = 25,
        CancellationToken ct = default)
    {
        var url = $"api/company-map-initiative/pins?radiusKm={radiusKm}"
                  + (lat is double la ? $"&lat={la.ToString(System.Globalization.CultureInfo.InvariantCulture)}" : "")
                  + (lng is double lo ? $"&lng={lo.ToString(System.Globalization.CultureInfo.InvariantCulture)}" : "");
        return await GetApiJsonAsync<List<CompanyMapPinDto>>(url, ct) ?? [];
    }

    public Task<CandidateInitiativeWeekDto?> GetInitiativeWeekAsync(CancellationToken ct = default)
        => GetApiJsonAsync<CandidateInitiativeWeekDto>("api/company-map-initiative/candidate/week", ct);

    public async Task<SendWorkInitiativeResult?> SendWorkInitiativeAsync(
        SendWorkInitiativeRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync("api/company-map-initiative/candidate/send", request, ct);
        return await ReadApiJsonAsync<SendWorkInitiativeResult>(response.Content, ct);
    }

    public async Task<IReadOnlyList<WorkInitiativeListItemDto>> GetCandidateInitiativesAsync(CancellationToken ct = default)
    {
        try
        {
            return await GetApiJsonAsync<List<WorkInitiativeListItemDto>>("api/company-map-initiative/candidate/list", ct) ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public Task<CompanyInitiativeSettingsDto?> GetInitiativeSettingsAsync(Guid rootCompanyId, CancellationToken ct = default)
        => GetApiJsonAsync<CompanyInitiativeSettingsDto>($"api/company-map-initiative/employer/{rootCompanyId:D}/settings", ct);

    public async Task<CompanyInitiativeSettingsDto?> SaveInitiativeSettingsAsync(
        Guid rootCompanyId,
        CompanyInitiativeSettingsDto dto,
        CancellationToken ct = default)
    {
        var response = await PutApiJsonAsync($"api/company-map-initiative/employer/{rootCompanyId:D}/settings", dto, ct);
        return await ReadApiJsonAsync<CompanyInitiativeSettingsDto>(response.Content, ct);
    }

    public async Task<IReadOnlyList<EmployerInitiativeInboxItemDto>> GetEmployerInitiativeInboxAsync(
        Guid companyId,
        CancellationToken ct = default)
        => await GetApiJsonAsync<List<EmployerInitiativeInboxItemDto>>(
               $"api/company-map-initiative/employer/{companyId:D}/inbox", ct) ?? [];

    public async Task DeclineWorkInitiativeAsync(Guid id, bool keepOpen, CancellationToken ct = default)
    {
        await PostApiJsonAsync(
            $"api/company-map-initiative/employer/{id:D}/decline",
            new { KeepOpen = keepOpen, ReducedMaxPerWeek = (int?)null, CloseTeam = false },
            ct);
    }
}
