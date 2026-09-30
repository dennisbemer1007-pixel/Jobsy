using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<EmployerOnboardingStatusItem?> GetEmployerOnboardingStatusAsync(
        CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<EmployerOnboardingStatusItem>(
                "api/employer/onboarding/status", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public async Task DismissVestigingSuggestionAsync(string kvkEstablishmentId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/employer/onboarding/vestiging-suggestions/dismiss",
            new { kvkEstablishmentId },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task AcceptVestigingSuggestionAsync(string kvkEstablishmentId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/employer/onboarding/vestiging-suggestions/accept",
            new { kvkEstablishmentId },
            ct);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class EmployerOnboardingStatusItem
{
    public Guid RootCompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public string VerificationStatus { get; set; } = "";
    public string VerificationMethod { get; set; } = "";
    public string? RejectionReason { get; set; }
    public bool ManualPending { get; set; }
    public DateTime? ManualReplyByUtc { get; set; }
    public bool LetterUnderway { get; set; }
    public Guid? ActiveLetterId { get; set; }
    public DateTime? LetterSentAtUtc { get; set; }
    public DateTime? LetterExpiresAtUtc { get; set; }
    public int LetterFailedAttempts { get; set; }
    public int LetterAttemptsRemaining { get; set; }
    public DateTime? LetterResendAvailableAtUtc { get; set; }
    public int LetterResendsRemaining { get; set; }
    public string? LetterAddressMasked { get; set; }
    public bool EmailAvailable { get; set; }
    public bool ShowUnverifiedBanner { get; set; }
    public bool ShowSuccessBanner { get; set; }
    public int LastAutoPublishedVacancyCount { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public bool IsBureau { get; set; }
    public bool LenderRegistrationPending { get; set; }
    public bool HasReadyVacancy { get; set; }
    public List<EmployerChecklistItemModel> Checklist { get; set; } = [];
    public List<EmployerVisibilityRowModel> Visibility { get; set; } = [];
    public List<EmployerVestigingSuggestionModel> SuggestedVestigingen { get; set; } = [];
    public bool ChecklistComplete { get; set; }
}

public sealed class EmployerChecklistItemModel
{
    public string Key { get; set; } = "";
    public bool Done { get; set; }
    public string? Href { get; set; }
    public string? Detail { get; set; }
}

public sealed class EmployerVisibilityRowModel
{
    public string Key { get; set; } = "";
    public bool Visible { get; set; }
}

public sealed class EmployerVestigingSuggestionModel
{
    public string KvkEstablishmentId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
}
