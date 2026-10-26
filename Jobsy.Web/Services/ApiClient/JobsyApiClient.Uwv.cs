using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<UwvToolkitStateDto?> GetUwvToolkitAsync(CancellationToken ct = default)
        => (await GetMeJsonAsync<UwvToolkitStateDto>("api/me/uwv/toolkit", ct)).Value;

    public async Task<UwvToolkitStateDto?> SaveUwvOndernemerAsync(int[] answers, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/uwv/ondernemer", new { answers }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Ondernemerstest opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<UwvToolkitStateDto>(cancellationToken: ct);
    }

    public async Task<UwvToolkitStateDto?> SaveUwvDrijfverenAsync(string[] driverKeys, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/uwv/drijfveren", new { driverKeys }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Drijfveren opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<UwvToolkitStateDto>(cancellationToken: ct);
    }

    public async Task<UwvAdviseurShareCreatedDto?> CreateUwvAdviseurShareAsync(
        string[] sections,
        string language,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/me/uwv/adviseur-share",
            new { sections, language },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Deellink aanmaken mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<UwvAdviseurShareCreatedDto>(cancellationToken: ct);
    }

    public async Task<UwvAdviseurShareViewDto?> GetUwvAdviseurSharePublicAsync(string token, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(
            $"api/uwv/adviseur-share/{Uri.EscapeDataString(token)}",
            HttpCompletionOption.ResponseHeadersRead,
            ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UwvAdviseurShareViewDto>(cancellationToken: ct);
    }
}

public sealed record UwvToolkitStateDto(
    string? OndernemerOutcomeKey,
    DateTime? OndernemerCompletedAtUtc,
    IReadOnlyList<string> DrijfverenDriverKeys,
    DateTime? DrijfverenCompletedAtUtc);

public sealed record UwvAdviseurShareCreatedDto(
    string Token,
    DateTime ExpiresAtUtc);

public sealed record UwvAdviseurShareViewDto(
    string Language,
    IReadOnlyList<string> Sections,
    string CandidateDisplayName,
    string? OndernemerOutcomeKey,
    IReadOnlyList<string> DrijfverenDriverKeys,
    IReadOnlyList<UwvAdviseurEndorsedCompetenceDto> EndorsedCompetences,
    DateTime ExpiresAtUtc);

public sealed record UwvAdviseurEndorsedCompetenceDto(
    string Label,
    string? RecognitionLine);
