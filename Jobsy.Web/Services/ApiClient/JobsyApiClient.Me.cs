using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<MeProfile?> GetMyProfileAsync(CancellationToken ct = default)
    {
        var result = await GetMyProfileResultAsync(ct);
        return result.Value;
    }

    public Task<MeGetResult<MeProfile>> GetMyProfileResultAsync(CancellationToken ct = default)
        => GetMeJsonAsync<MeProfile>("api/me/profile", ct);

    public async Task DownloadMyLobsyCvPdfAsync(IJSRuntime js, CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/me/lobsy-cv.pdf", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Lobsy-CV downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? $"Lobsy-CV-{DateTime.UtcNow:yyyyMMdd}.pdf";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, "application/pdf");
    }

    public async Task<MeProfile?> UploadMyCvAsync(IBrowserFile file, CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream(CandidateCvFileRules.MaxBytes, ct);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.Name);
        var response = await _http.PostAsync("api/me/cv", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "CV uploaden mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<MeProfile>(cancellationToken: ct);
    }

    public async Task DownloadMyUploadedCvAsync(IJSRuntime js, CancellationToken ct = default)
    {
        await DownloadNamedFileAsync("api/me/cv", js, "CV.pdf", ct);
    }

    public async Task<MeProfile?> DeleteMyCvAsync(CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync("api/me/cv", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "CV verwijderen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<MeProfile>(cancellationToken: ct);
    }

    public async Task<DiplomaEvaluationItem> SaveDiplomaEvaluationAsync(
        DiplomaEvaluationDraft draft,
        CancellationToken ct = default)
    {
        var payload = new
        {
            diplomaTitle = draft.DiplomaTitle,
            issuingBody = draft.IssuingBody,
            issuingBodyOther = draft.IssuingBodyOther,
            equivalentLevelText = draft.EquivalentLevelText,
            equivalentLevelCode = string.IsNullOrWhiteSpace(draft.EquivalentLevelCode) ? null : draft.EquivalentLevelCode,
            evaluationDate = draft.DateInput,
            referenceNumber = draft.ReferenceNumber
        };
        var response = draft.Id is Guid id
            ? await _http.PutAsJsonAsync($"api/me/diploma-evaluations/{id:D}", payload, ct)
            : await _http.PostAsJsonAsync("api/me/diploma-evaluations", payload, ct);
        return await ReadDiplomaEvaluationAsync(response, ct);
    }

    public async Task<DiplomaEvaluationItem> UploadDiplomaEvaluationDocumentAsync(
        Guid id,
        string fileName,
        string contentType,
        byte[] bytes,
        CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        content.Add(fileContent, "file", string.IsNullOrWhiteSpace(fileName) ? "waardering.pdf" : fileName);
        var response = await _http.PostAsync($"api/me/diploma-evaluations/{id:D}/document", content, ct);
        return await ReadDiplomaEvaluationAsync(response, ct);
    }

    public async Task<DiplomaEvaluationItem> DeleteDiplomaEvaluationDocumentAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/me/diploma-evaluations/{id:D}/document", ct);
        return await ReadDiplomaEvaluationAsync(response, ct);
    }

    public async Task DeleteDiplomaEvaluationAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/me/diploma-evaluations/{id:D}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw DiplomaEvaluationFailed(body, (int)response.StatusCode);
        }
    }

    public Task DownloadDiplomaEvaluationDocumentAsync(IJSRuntime js, Guid id, CancellationToken ct = default)
        => DownloadNamedFileAsync($"api/me/diploma-evaluations/{id:D}/document", js, "waardering.pdf", ct);

    private static async Task<DiplomaEvaluationItem> ReadDiplomaEvaluationAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw DiplomaEvaluationFailed(body, (int)response.StatusCode);
        }

        return JsonSerializer.Deserialize<DiplomaEvaluationItem>(body, DiplomaJson)
               ?? throw new InvalidOperationException("diploma_eval_failed");
    }

    private static ApiErrorException DiplomaEvaluationFailed(string body, int statusCode)
    {
        var code = ExtractDiplomaErrorCode(body);
        return new ApiErrorException(code ?? ApiErrorException.Unknown, statusCode);
    }

    private static string? ExtractDiplomaErrorCode(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("code", out var code))
            {
                var value = code.GetString();
                if (DiplomaEvaluationRules.IsErrorCode(value))
                {
                    return value;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static readonly JsonSerializerOptions DiplomaJson = new(JsonSerializerDefaults.Web);

    public async Task<MeProfile?> AcceptConsentAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/me/accept-consent", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MeProfile>(cancellationToken: ct);
    }

    public async Task<MeProfile?> AcceptTestAiConsentAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/me/test-ai-consent", null, ct);
        await EnsureConsentResponseAsync(response);
        return await response.Content.ReadFromJsonAsync<MeProfile>(cancellationToken: ct);
    }

    public async Task<MeProfile?> WithdrawTestAiConsentAsync(bool deleteResults, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/me/test-ai-consent?deleteResults={deleteResults.ToString().ToLowerInvariant()}", ct);
        await EnsureConsentResponseAsync(response);
        return await response.Content.ReadFromJsonAsync<MeProfile>(cancellationToken: ct);
    }

    public async Task RequestParentalConsentAsync(string parentEmail, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/me/parental-consent-request", new { parentEmail }, ct);
        await EnsureConsentResponseAsync(response);
    }

    public async Task<MeProfile?> UpdateDateOfBirthAsync(DateOnly dateOfBirth, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/date-of-birth", new { dateOfBirth }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MeProfile>(cancellationToken: ct);
    }

    public async Task<MeProfile?> UpdateMyLanguageAsync(string language, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/language", new { language }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MeProfile>(cancellationToken: ct);
    }

    public async Task<MeProfile?> UpdateMyProfileAsync(
        bool? openForWork = null,
        DateOnly? dateOfBirth = null,
        CandidatePreferences? preferences = null,
        double? homeLatitude = null,
        double? homeLongitude = null,
        bool clearHomeLocation = false,
        string? firstName = null,
        string? lastName = null,
        string? phoneNumber = null,
        bool? whatsAppContactAllowed = null,
        IReadOnlyList<CandidateReferenceItem>? references = null,
        DateOnly? availableFromDate = null,
        bool clearAvailableFromDate = false,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/profile", new
        {
            openForWork,
            dateOfBirth,
            preferences,
            homeLatitude,
            homeLongitude,
            clearHomeLocation,
            firstName,
            lastName,
            phoneNumber,
            whatsAppContactAllowed,
            references,
            availableFromDate,
            clearAvailableFromDate
        }, ct);
        response.EnsureSuccessStatusCode();
        InvalidateMeCache(KompasCacheKey);
        InvalidateMeCache(KompasDnaCacheKey);
        return await response.Content.ReadFromJsonAsync<MeProfile>(cancellationToken: ct);
    }

    public async Task<CandidatePrivatePreferences?> GetMyPrivatePreferencesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<CandidatePrivatePreferences>("api/me/private-preferences", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<CandidatePrivatePreferences?> UpdateMyPrivatePreferencesAsync(
        IReadOnlyList<string>? dislikes = null,
        IReadOnlyList<string>? customDislikes = null,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/private-preferences", new
        {
            dislikes,
            customDislikes
        }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CandidatePrivatePreferences>(cancellationToken: ct);
    }

    public async Task<OnboardingState?> GetMyOnboardingAsync(CancellationToken ct = default)
    {
        if (_meCache is not null)
        {
            var cached = await _meCache.GetOrCreateAsync(
                OnboardingCacheKey,
                async token =>
                {
                    var value = await FetchOnboardingAsync(token);
                    return MeGetResult<OnboardingState>.Ok(value);
                },
                ct: ct);
            return cached.Value;
        }

        return await FetchOnboardingAsync(ct);
    }

    private async Task<OnboardingState?> FetchOnboardingAsync(CancellationToken ct)
    {
        try
        {
            return await _http.GetFromJsonAsync<OnboardingState>("api/me/onboarding", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<OnboardingState> SaveMyOnboardingProgressAsync(
        int currentStep,
        bool? stepCompleted = null,
        bool? stepSkipped = null,
        string? source = null,
        bool? finishReached = null,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/onboarding", new
        {
            currentStep,
            stepCompleted,
            stepSkipped,
            source,
            finishReached
        }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Voortgang opslaan mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<OnboardingState>(cancellationToken: ct))!;
    }

    public async Task<OnboardingState> CompleteMyOnboardingAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/me/onboarding/complete", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Startprofiel afronden mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<OnboardingState>(cancellationToken: ct))!;
    }

    public async Task SaveOnboardingDreamJobAsync(string? dreamTitle, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/onboarding/dream-job", new { dreamTitle }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Droombaan opslaan mislukt.");
        }
    }

    public void InvalidateMeCache(string? key = null) => _meCache?.Invalidate(key);

    public async Task<IReadOnlyList<CandidateMatchedVacancy>> GetMyMatchedVacanciesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<CandidateMatchedVacancy>>("api/me/matched-vacancies", ct)
                   ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return [];
        }
    }

    /// <summary>Read-only stone state for the candidate how-to guide (05 §2).</summary>
    public Task<CandidateJourneySummaryApiModel?> GetMyJourneySummaryAsync(CancellationToken ct = default)
        => _http.GetFromJsonAsync<CandidateJourneySummaryApiModel>("api/me/journey-summary", ct);

    public async Task CompleteCandidateHowToAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/me/candidate-how-to-completed", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task<IReadOnlyList<CandidateEngagementItem>> GetMyLikesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<CandidateEngagementItem>>("api/me/likes", ct) ?? [];

    public async Task<IReadOnlyList<CandidateEngagementItem>> GetMySharesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<CandidateEngagementItem>>("api/me/shares", ct) ?? [];

    private static async Task EnsureConsentResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(ExtractMessage(body) ?? "Toestemming aanpassen mislukt.");
    }

    public async Task<IReadOnlyList<UserNotificationItem>> GetNotificationsAsync(
        int take = 50,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<UserNotificationItem>>($"api/notifications?take={take}", ct) ?? [];

    public async Task<int> GetUnreadNotificationCountAsync(CancellationToken ct = default)
    {
        var dto = await _http.GetFromJsonAsync<UnreadNotificationCountWire>("api/notifications/unread-count", ct);
        return dto?.Count ?? 0;
    }

    public async Task<UserNotificationItem?> MarkNotificationReadAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/notifications/{id}/read", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserNotificationItem>(cancellationToken: ct);
    }

    public async Task MarkAllNotificationsReadAsync(CancellationToken ct = default)
    {
        await _http.PostAsync("api/notifications/read-all", null, ct);
    }

    private sealed class UnreadNotificationCountWire
    {
        public int Count { get; set; }
    }

    public async Task<string> ExportPrivacyDataAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/privacy/export", ct);
        if (!response.IsSuccessStatusCode)
        {
            // errors 04 §04.5: a typed error (code + supportCode + retryAfterSeconds) instead of
            // the raw body, which used to end up on screen through ex.Message.
            throw await ApiErrorException.FromResponseAsync(response, ct);
        }

        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<IReadOnlyList<UnsubscribeReasonOption>> GetUnsubscribeReasonsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/privacy/unsubscribe-reasons", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? response.ReasonPhrase ?? "Redenen laden mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<List<UnsubscribeReasonOption>>(cancellationToken: ct) ?? [];
    }

    public async Task RequestUnsubscribeAsync(string reasonCode, string? reasonOther = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/privacy/request-unsubscribe",
            new { reasonCode, reasonOther },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? response.ReasonPhrase ?? "Aanvraag mislukt.");
        }
    }

    public async Task ConfirmUnsubscribeAsync(string verificationCode, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/privacy/confirm-unsubscribe",
            new { verificationCode },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? response.ReasonPhrase ?? "Bevestigen mislukt.");
        }
    }

    public async Task<EmailPreferencesDto?> GetMyEmailPreferencesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<EmailPreferencesDto>("api/me/email-preferences", ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<EmailPreferencesDto?> UpdateMyEmailPreferencesAsync(
        IReadOnlyList<EmailPreferenceItemDto> items,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/email-preferences", new { items }, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<EmailPreferencesDto>(cancellationToken: ct);
    }

    public async Task<WhatsAppReminderPreferenceDto?> GetReminderWhatsAppAsync(CancellationToken ct = default)
    {
        try
        {
            return await GetApiJsonAsync<WhatsAppReminderPreferenceDto>("api/me/reminder-whatsapp", ct);
        }
        catch
        {
            return null;
        }
    }

    public sealed class WhatsAppReminderPreferenceDto
    {
        public bool Available { get; set; }
        public bool OptedIn { get; set; }
        public string? Phone { get; set; }
    }

    public async Task<IReadOnlyList<ReferenceConfirmationItem>> GetReferenceConfirmationsAsync(CancellationToken ct = default)
    {
        try
        {
            return await GetApiJsonAsync<List<ReferenceConfirmationItem>>("api/me/reference-confirmations", ct) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<(bool Ok, string? Message)> RequestReferenceConfirmationAsync(
        Guid referenceId,
        string roleTitle,
        bool consentAccepted,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/me/references/{referenceId}/confirmation",
            new { roleTitle, consentAccepted },
            ct);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        return (false, ExtractMessage(body) ?? "De mail kon niet weg. Probeer het later opnieuw.");
    }

    public async Task<bool> SetReferenceShareAsync(
        Guid referenceId,
        bool showOnPartnerPassport,
        bool shareWorkedHere,
        bool sharePeriod,
        bool shareDidWell,
        bool shareWorkAgain,
        bool shareExtra,
        CancellationToken ct = default)
    {
        var response = await PutApiJsonAsync(
            $"api/me/references/{referenceId}/confirmation/share",
            new
            {
                showOnPartnerPassport,
                shareWorkedHere,
                sharePeriod,
                shareDidWell,
                shareWorkAgain,
                shareExtra
            },
            ct);
        return response.IsSuccessStatusCode;
    }

    public sealed record ReferenceConfirmationItem(
        Guid ReferenceId,
        string EmployerName,
        string ContactName,
        string Email,
        string Status,
        string? RoleTitle,
        DateTime? ConfirmedAtUtc,
        DateTime? DeclinedAtUtc,
        bool? WorkedHere,
        string? Period,
        string? DidWell,
        string? WorkAgain,
        string? Extra,
        bool ShowOnPartnerPassport,
        bool ShareWorkedHere,
        bool SharePeriod,
        bool ShareDidWell,
        bool ShareWorkAgain,
        bool ShareExtra,
        int RequestsUsed);

    public sealed record EmailPreferenceItemDto(string Key, string Label, bool Enabled);

    public sealed record EmailPreferencesDto(
        IReadOnlyList<EmailPreferenceItemDto>? Optional,
        IReadOnlyList<string>? Always);
}
