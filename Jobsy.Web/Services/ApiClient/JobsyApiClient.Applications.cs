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
    public async Task DownloadApplicationLobsyCvPdfAsync(
        Guid applicationId,
        IJSRuntime js,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/applications/{applicationId:D}/lobsy-cv.pdf", ct);
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

    public async Task DownloadApplicationUploadedCvAsync(
        Guid applicationId,
        IJSRuntime js,
        CancellationToken ct = default)
    {
        await DownloadNamedFileAsync($"api/applications/{applicationId:D}/uploaded-cv", js, "CV.pdf", ct);
    }

    public async Task<MeProfile?> AcceptTalentPoolConsentAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/me/talent-pool-consent", null, ct);
        await EnsureConsentResponseAsync(response);
        return await response.Content.ReadFromJsonAsync<MeProfile>(cancellationToken: ct);
    }

    public async Task<MeProfile?> WithdrawTalentPoolConsentAsync(CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync("api/me/talent-pool-consent", ct);
        await EnsureConsentResponseAsync(response);
        return await response.Content.ReadFromJsonAsync<MeProfile>(cancellationToken: ct);
    }

    public async Task<List<AnonymousTalentCard>?> SearchTalentPoolAsync(
        string? tags,
        int? maxTravelMinutes,
        string? drivingLicense,
        string? availability = null,
        string? transport = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(tags))
        {
            qs.Add($"tags={Uri.EscapeDataString(tags)}");
        }

        if (maxTravelMinutes is int m)
        {
            qs.Add($"maxTravelMinutes={m}");
        }

        if (!string.IsNullOrWhiteSpace(drivingLicense))
        {
            qs.Add($"drivingLicense={Uri.EscapeDataString(drivingLicense)}");
        }

        if (!string.IsNullOrWhiteSpace(availability))
        {
            qs.Add($"availability={Uri.EscapeDataString(availability)}");
        }

        if (!string.IsNullOrWhiteSpace(transport))
        {
            qs.Add($"transport={Uri.EscapeDataString(transport)}");
        }

        var url = "api/employer/talent/search" + (qs.Count == 0 ? "" : "?" + string.Join('&', qs));
        try
        {
            return await _http.GetFromJsonAsync<List<AnonymousTalentCard>>(url, ct);
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task UnlockTalentContactAsync(Guid candidateUserId, string message, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/employer/talent/unlock",
            new { candidateUserId, message },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Contact ontgrendelen mislukt.");
        }
    }

    public async Task<List<TalentContactRequestModel>?> ListEmployerTalentContactsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<TalentContactRequestModel>>("api/employer/talent/requests", ct);
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task WithdrawTalentContactAsync(Guid requestId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/employer/talent/{requestId}/withdraw", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Intrekken mislukt.");
        }
    }

    public async Task<List<TalentContactRequestModel>?> ListCandidateTalentContactsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<TalentContactRequestModel>>("api/me/talent-contacts", ct);
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task RespondToTalentContactAsync(Guid requestId, bool accept, bool alreadyPlaced = false, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/me/talent-contacts/{requestId}/respond",
            new { accept, alreadyPlaced },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Reageren op contactverzoek mislukt.");
        }
    }

    public async Task<IReadOnlyList<ApplicationItem>> GetMyApplicationsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ApplicationItem>>("api/me/applications", ct) ?? [];

    public async Task WithdrawApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/applications/{applicationId}/withdraw", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task<CandidateActionResultItem?> SetUnavailableViaTokenAsync(
        string token,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/candidate-actions/set-unavailable",
            new { token },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<CandidateActionResultItem>(cancellationToken: ct);
    }

    public async Task<CandidateActionResultItem?> SetUnavailableAuthenticatedAsync(
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/candidate-actions/set-unavailable/me", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<CandidateActionResultItem>(cancellationToken: ct);
    }

    public async Task<CandidateActionResultItem?> WithdrawOtherApplicationsViaTokenAsync(
        string token,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/candidate-actions/withdraw-others",
            new { token },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<CandidateActionResultItem>(cancellationToken: ct);
    }

    public async Task<CandidateActionResultItem?> WithdrawOtherApplicationsAuthenticatedAsync(
        Guid hiredApplicationId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/candidate-actions/withdraw-others/me",
            new { hiredApplicationId },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<CandidateActionResultItem>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<EmployerApplicationItem>> GetApplicationsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<EmployerApplicationItem>>("api/applications", ct) ?? [];

    public async Task<ApplyResultItem?> ApplyAsync(
        Guid vacancyId,
        string preferredTransport,
        int estimatedTravelMinutes,
        bool useAuthenticator = false,
        bool acceptedTerms = false,
        bool workPermitConfirmed = false,
        string? verificationCode = null,
        string? consentVersion = null,
        string? motivation = null,
        bool confirmLowMatchSafetyNet = false,
        string? studentNumber = null,
        string? schoolEmail = null,
        string? studyProgram = null,
        string? studyYear = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/applications", new
        {
            vacancyId,
            preferredTransport,
            estimatedTravelMinutes,
            useAuthenticator,
            acceptedTerms,
            workPermitConfirmed,
            verificationCode,
            consentVersion = consentVersion ?? Jobsy.Core.Privacy.PrivacyConstants.CurrentConsentVersion,
            motivation,
            confirmLowMatchSafetyNet,
            studentNumber,
            schoolEmail,
            studyProgram,
            studyYear
        }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<ApplyResultItem>(cancellationToken: ct);
    }

    public async Task ReactToApplicationAsync(Guid applicationId, string status, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/applications/{applicationId}/react", new { status }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task MarkEmployerContactAsync(Guid applicationId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/applications/{applicationId}/contact", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }
}
