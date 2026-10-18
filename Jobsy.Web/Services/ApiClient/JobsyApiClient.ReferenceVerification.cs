using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<bool> GetReferenceVerificationEligibleAsync(CancellationToken ct = default)
    {
        try
        {
            var dto = await GetApiJsonAsync<EligibleDto>("api/me/reference-verification/eligible", ct);
            return dto?.Eligible == true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<ReferenceVerificationListItem>> GetReferenceVerificationListAsync(
        CancellationToken ct = default)
    {
        try
        {
            return await GetApiJsonAsync<List<ReferenceVerificationListItem>>("api/me/reference-verification", ct) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<VerifiedCompetenceItem>> GetVerifiedCompetencesAsync(CancellationToken ct = default)
    {
        try
        {
            return await GetApiJsonAsync<List<VerifiedCompetenceItem>>(
                "api/me/reference-verification/verified-competences", ct) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<(bool Ok, string? Error)> RequestWorkExperienceReferenceAsync(
        Guid entryId,
        string referentName,
        string relation,
        string email,
        bool consentAccepted,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/me/work-experience/{entryId:D}/reference-verification",
            new
            {
                referentName,
                relation,
                email,
                consentAccepted
            },
            ct);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var error = ExtractJsonString(body, "error");
        return (false, error);
    }

    public async Task<bool> RevokeReferenceVerificationAsync(Guid requestId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/me/reference-verification/{requestId:D}", ct);
        return response.IsSuccessStatusCode;
    }

    private static string? ExtractJsonString(string body, string property)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty(property, out var value))
            {
                return value.GetString();
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private sealed class EligibleDto
    {
        [JsonPropertyName("eligible")]
        public bool Eligible { get; set; }
    }

    public sealed record ReferenceVerificationListItem(
        Guid Id,
        Guid WorkExperienceEntryId,
        string EmployerName,
        string RoleTitle,
        string DisplayStatus,
        string ReferentName,
        string Relation,
        DateTime? CompletedAtUtc,
        DateTime? SentAtUtc,
        DateTime? ExpiresAtUtc,
        int? DaysRemaining,
        bool Active,
        int EndorsedCount);

    public sealed record VerifiedCompetenceItem(
        string Code,
        string Label,
        string RecognitionLine,
        DateTime CompletedAtUtc);

    public sealed record ReferenceVerificationPublicView(
        string State,
        string CandidateName,
        string EmployerName,
        string RoleTitle,
        string? StartMonth,
        string? EndMonth,
        bool IsCurrent,
        string? DutiesText,
        IReadOnlyList<ReferenceVerificationCompetenceOffer> Competences,
        bool AlreadyConfirmed);

    public sealed record ReferenceVerificationCompetenceOffer(string Code, string Label, string? Description);
}
