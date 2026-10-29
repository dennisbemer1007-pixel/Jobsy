using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<PupilG78PassportWebDto?> GetParentG78PassportAsync(
        string? language = null,
        CancellationToken ct = default)
    {
        var query = string.IsNullOrWhiteSpace(language)
            ? ""
            : $"?lang={Uri.EscapeDataString(language)}";
        return await GetApiJsonAsync<PupilG78PassportWebDto>($"api/parent/passport/g78{query}", ct);
    }

    public async Task<(byte[]? Bytes, string? FileName, int StatusCode)> GetParentPassportPdfAsync(
        string? query,
        CancellationToken ct = default)
    {
        var path = string.IsNullOrEmpty(query) ? "/ouder/pdf" : $"/ouder/pdf{query}";
        using var response = await _http.GetAsync(path, ct);
        if (!response.IsSuccessStatusCode)
        {
            return (null, null, (int)response.StatusCode);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? "lobsy-dna-paspoort.pdf";
        return (bytes, fileName, (int)response.StatusCode);
    }
}
