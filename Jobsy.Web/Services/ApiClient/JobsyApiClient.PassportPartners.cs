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
        response.EnsureSuccessStatusCode();
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
