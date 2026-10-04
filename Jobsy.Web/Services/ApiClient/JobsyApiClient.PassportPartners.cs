using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<IReadOnlyList<PassportPartnerAdminItem>> GetAdminPassportPartnersAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/admin/passport-partners", ct);
        // Feature off is a 404. An empty list is the right admin state, not a save error.
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<PassportPartnerAdminItem>>(CaseInsensitiveJson, ct) ?? [];
    }

    public async Task CreateAdminPassportPartnerAsync(
        Guid companyId, string? displayName, int? maxBranches, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/admin/passport-partners",
            new { companyId, displayName, maxBranches },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorException.FromResponseAsync(response, ct);
        }
    }

    public async Task<IReadOnlyList<PassportPartnerCodeItem>> GetAdminPassportPartnerCodesAsync(
        Guid partnerId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/admin/passport-partners/{partnerId}/codes", ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<PassportPartnerCodeItem>>(CaseInsensitiveJson, ct) ?? [];
    }

    public async Task CreateAdminPassportPartnerCodeAsync(
        Guid partnerId, Guid branchCompanyId, string? vanityCode, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/passport-partners/{partnerId}/codes",
            new { branchCompanyId, vanityCode },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorException.FromResponseAsync(response, ct);
        }
    }

    public async Task DeactivateAdminPassportPartnerCodeAsync(Guid codeId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/admin/passport-partners/codes/{codeId}/deactivate", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorException.FromResponseAsync(response, ct);
        }
    }

    public async Task<(byte[]? Bytes, int StatusCode)> GetPassportPartnerCodesPdfAsync(
        Guid partnerId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/admin/passport-partners/{partnerId}/codes.pdf", ct);
        if (!response.IsSuccessStatusCode)
        {
            return (null, (int)response.StatusCode);
        }

        return (await response.Content.ReadAsByteArrayAsync(ct), 200);
    }

    public async Task SetAdminPassportPartnerActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/passport-partners/{id}/active",
            new { isActive },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UploadAdminPassportPartnerLogoAsync(
        Guid id, string fileName, string contentType, byte[] bytes, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "image/png" : contentType);
        content.Add(file, "file", fileName);
        var response = await _http.PostAsync($"api/admin/passport-partners/{id}/logo", content, ct);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class PassportPartnerAdminItem
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; }
    public int MaxBranches { get; set; }
    public string? Type { get; set; }
    public bool HasLogo { get; set; }
}

public sealed class PassportPartnerCodeItem
{
    public Guid Id { get; set; }
    public string CodeDisplay { get; set; } = "";
    public bool IsActive { get; set; }
    public Guid BranchCompanyId { get; set; }
    public string BranchName { get; set; } = "";
}
