using System.Net.Http.Json;
using Jobsy.Core.Contracts;

namespace Jobsy.Web.Services;

public partial class JobsyApiClient
{
    public async Task<IReadOnlyList<CompanyMapPinDto>> GetCompanyMapPinsAsync(
        double? lat,
        double? lng,
        int radiusKm = 25,
        CancellationToken ct = default)
    {
        var url = $"api/company-map-initiative/pins?radiusKm={radiusKm}"
                  + (lat is double la ? $"&lat={la.ToString(System.Globalization.CultureInfo.InvariantCulture)}" : "")
                  + (lng is double lo ? $"&lng={lo.ToString(System.Globalization.CultureInfo.InvariantCulture)}" : "");
        return await _http.GetFromJsonAsync<List<CompanyMapPinDto>>(url, ct) ?? [];
    }

    public Task<CandidateInitiativeWeekDto?> GetInitiativeWeekAsync(CancellationToken ct = default)
        => _http.GetFromJsonAsync<CandidateInitiativeWeekDto>("api/company-map-initiative/candidate/week", ct);

    public async Task<SendWorkInitiativeResult?> SendWorkInitiativeAsync(
        SendWorkInitiativeRequest request,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/company-map-initiative/candidate/send", request, ct);
        return await response.Content.ReadFromJsonAsync<SendWorkInitiativeResult>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<WorkInitiativeListItemDto>> GetCandidateInitiativesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<WorkInitiativeListItemDto>>("api/company-map-initiative/candidate/list", ct) ?? [];

    public Task<CompanyInitiativeSettingsDto?> GetInitiativeSettingsAsync(Guid rootCompanyId, CancellationToken ct = default)
        => _http.GetFromJsonAsync<CompanyInitiativeSettingsDto>($"api/company-map-initiative/employer/{rootCompanyId:D}/settings", ct);

    public async Task<CompanyInitiativeSettingsDto?> SaveInitiativeSettingsAsync(
        Guid rootCompanyId,
        CompanyInitiativeSettingsDto dto,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/company-map-initiative/employer/{rootCompanyId:D}/settings", dto, ct);
        return await response.Content.ReadFromJsonAsync<CompanyInitiativeSettingsDto>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<EmployerInitiativeInboxItemDto>> GetEmployerInitiativeInboxAsync(
        Guid companyId,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<EmployerInitiativeInboxItemDto>>(
               $"api/company-map-initiative/employer/{companyId:D}/inbox", ct) ?? [];
}
