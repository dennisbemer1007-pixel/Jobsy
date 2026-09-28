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
                ct);
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
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? response.ReasonPhrase ?? "Export mislukt.");
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
}
