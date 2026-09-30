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
    public async Task<CompanyCultureState?> GetCompanyCultureAsync(Guid? companyId = null, CancellationToken ct = default)
    {
        try
        {
            var qs = companyId is Guid id ? $"?companyId={id}" : "";
            return await _http.GetFromJsonAsync<CompanyCultureState>($"api/company/culture{qs}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<CompanyCultureState> SaveCompanyCultureAsync(
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        Guid? companyId = null,
        CancellationToken ct = default)
    {
        var payload = new
        {
            answers = answers.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            complete
        };
        var qs = companyId is Guid id ? $"?companyId={id}" : "";
        var response = await _http.PutAsJsonAsync($"api/company/culture{qs}", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Bedrijfscultuur opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<CompanyCultureState>(cancellationToken: ct)
               ?? new CompanyCultureState();
    }

    public async Task<IReadOnlyList<MetricCount>> GetMyMetricsSummaryAsync(string period = "week", CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<MetricCount>>($"api/me/metrics/summary?period={Uri.EscapeDataString(period)}", ct) ?? [];

    public async Task<IReadOnlyList<MetricDrilldownItem>> GetMyMetricsDrilldownAsync(
        string key,
        string period = "week",
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<MetricDrilldownItem>>(
            $"api/me/metrics/drilldown/{Uri.EscapeDataString(key)}?period={Uri.EscapeDataString(period)}", ct) ?? [];

    public async Task<IReadOnlyList<MetricCount>> GetEmployerMetricsSummaryAsync(
        string period = "week",
        Guid? companyId = null,
        CancellationToken ct = default)
    {
        var qs = $"period={Uri.EscapeDataString(period)}";
        if (companyId is not null)
        {
            qs += $"&companyId={companyId}";
        }

        return await _http.GetFromJsonAsync<List<MetricCount>>($"api/metrics/summary?{qs}", ct) ?? [];
    }

    public async Task<IReadOnlyList<MetricDrilldownItem>> GetEmployerMetricsDrilldownAsync(
        string key,
        string period = "week",
        Guid? companyId = null,
        CancellationToken ct = default)
    {
        var qs = $"period={Uri.EscapeDataString(period)}";
        if (companyId is not null)
        {
            qs += $"&companyId={companyId}";
        }

        return await _http.GetFromJsonAsync<List<MetricDrilldownItem>>(
            $"api/metrics/drilldown/{Uri.EscapeDataString(key)}?{qs}", ct) ?? [];
    }

    public async Task<ClientPerformanceBoard?> GetClientPerformanceAsync(
        string period = "week",
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<ClientPerformanceBoard>(
            $"api/metrics/client-performance?period={Uri.EscapeDataString(period)}", ct);

    public async Task<DashboardRefreshResult> RefreshDashboardAsync(
        string period = "week",
        Guid? companyId = null,
        CancellationToken ct = default)
    {
        var qs = $"period={Uri.EscapeDataString(period)}";
        if (companyId is not null)
        {
            qs += $"&companyId={companyId}";
        }

        var response = await _http.PostAsync($"api/dashboard/refresh?{qs}", content: null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                ExtractMessage(body) ?? $"Dashboard verversen mislukt ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<DashboardRefreshResult>(cancellationToken: ct)
               ?? new DashboardRefreshResult();
    }

    public async Task<IReadOnlyList<CompanySummary>> GetMyCompaniesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<CompanySummary>>("api/companies/mine", ct) ?? [];

    public async Task<IReadOnlyList<CompanyApiKeyItem>> GetCompanyApiKeysAsync(
        Guid companyId,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<CompanyApiKeyItem>>(
            $"api/companies/{companyId}/api-keys", ct) ?? [];

    public async Task<GeneratedApiKeyItem?> GenerateCompanyApiKeyAsync(
        Guid companyId,
        string? name = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/companies/{companyId}/api-keys",
            new { name },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<GeneratedApiKeyItem>(cancellationToken: ct);
    }

    public async Task DeactivateCompanyApiKeyAsync(
        Guid companyId,
        Guid apiKeyId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/companies/{companyId}/api-keys/{apiKeyId}/deactivate",
            null,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
    }

    public async Task<EmailApiKeyResultItem?> EmailCompanyApiKeyCredentialsAsync(
        Guid companyId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/companies/{companyId}/api-keys/email-credentials",
            null,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<EmailApiKeyResultItem>(cancellationToken: ct);
    }

    public async Task<CompanySummary?> RegisterEstablishmentAsync(
        string kvkNumber,
        string kvkEstablishmentId,
        Guid? parentCompanyId = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/companies/from-kvk", new
        {
            kvkNumber,
            kvkEstablishmentId,
            parentCompanyId
        }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<CompanySummary>(cancellationToken: ct);
    }

    public async Task<CompanySummary?> RegisterIntermediaryClientFromKvkAsync(
        string kvkNumber,
        string kvkEstablishmentId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/companies/intermediary-clients/from-kvk", new
        {
            kvkNumber,
            kvkEstablishmentId
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<CompanySummary>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<KvkEstablishmentItem>> GetKvkEstablishmentsAsync(
        string kvkNumber,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<KvkEstablishmentItem>>(
            $"api/kvk/{Uri.EscapeDataString(kvkNumber)}/establishments", ct) ?? [];

    public async Task<KvkSearchResultItem> SearchKvkAsync(
        string query,
        string? place = null,
        int page = 1,
        CancellationToken ct = default)
    {
        var qs = new List<string> { $"q={Uri.EscapeDataString(query)}" };
        if (!string.IsNullOrWhiteSpace(place))
        {
            qs.Add($"plaats={Uri.EscapeDataString(place.Trim())}");
        }

        if (page > 1)
        {
            qs.Add($"pagina={page}");
        }

        return await _http.GetFromJsonAsync<KvkSearchResultItem>(
                   $"api/kvk/search?{string.Join("&", qs)}", ct)
               ?? new KvkSearchResultItem { Status = "NotFound" };
    }

    public async Task<KvkCompanyProfileItem> GetKvkProfileAsync(
        string kvkNumber,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<KvkCompanyProfileItem>(
               $"api/registration/kvk/{Uri.EscapeDataString(kvkNumber)}/profile", ct)
           ?? new KvkCompanyProfileItem { Status = "NotFound", KvkNumber = kvkNumber };

    public async Task<KvkEstablishmentsLookupResult> LookupRegistrationEstablishmentsAsync(
        string kvkNumber,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<KvkEstablishmentsLookupResult>(
               $"api/registration/kvk/{Uri.EscapeDataString(kvkNumber)}/establishments", ct)
           ?? new KvkEstablishmentsLookupResult { Status = "NotFound" };

    public async Task<RegistrationSubmitResult> SubmitRegistrationAsync(
        string kvkNumber,
        string kvkEstablishmentId,
        string scope,
        string contactName,
        string contactEmail,
        string? contactPhone = null,
        bool acceptedTerms = false,
        string? consentVersion = null,
        string? salesManagerTrackingCode = null,
        string? partnerTrackingCode = null,
        string? password = null,
        bool allowPendingKvkVerification = false,
        string? manualEstablishmentName = null,
        string? manualEstablishmentAddress = null,
        string? manualEstablishmentNumber = null,
        double? manualLatitude = null,
        double? manualLongitude = null,
        bool? manualIsIntermediarySbi = null,
        IReadOnlyList<string>? selectedEstablishmentIds = null,
        Guid? salesManagerUserId = null,
        DateTime? representationConsentAtUtc = null,
        string? representationConsentVersion = null,
        string? preferredLoginProvider = null,
        bool locationUnknown = false,
        bool acceptedRepresentation = false,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/registration", new
        {
            kvkNumber,
            kvkEstablishmentId,
            scope,
            contactName,
            contactEmail,
            contactPhone,
            acceptedTerms,
            consentVersion = consentVersion ?? Jobsy.Core.Privacy.PrivacyConstants.CurrentConsentVersion,
            salesManagerTrackingCode,
            partnerTrackingCode,
            password,
            allowPendingKvkVerification,
            manualEstablishmentName,
            manualEstablishmentAddress,
            manualEstablishmentNumber,
            manualLatitude,
            manualLongitude,
            manualIsIntermediarySbi,
            selectedEstablishmentIds,
            salesManagerUserId,
            representationConsentAtUtc,
            representationConsentVersion,
            preferredLoginProvider,
            locationUnknown,
            acceptedRepresentation
        }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? response.ReasonPhrase ?? "Registratie mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<RegistrationSubmitResult>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Lege registratierespons.");
    }

    public async Task<RegistrationReferralItem> ResolveRegistrationReferralAsync(
        string? typedCode,
        string? linkCode,
        CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(typedCode))
        {
            qs.Add($"typed={Uri.EscapeDataString(typedCode)}");
        }

        if (!string.IsNullOrWhiteSpace(linkCode))
        {
            qs.Add($"link={Uri.EscapeDataString(linkCode)}");
        }

        if (qs.Count == 0)
        {
            return new RegistrationReferralItem();
        }

        return await _http.GetFromJsonAsync<RegistrationReferralItem>(
                   $"api/registration/referral?{string.Join("&", qs)}", ct)
               ?? new RegistrationReferralItem();
    }

    public async Task<RegistrationActivationResult> ConfirmRegistrationAsync(
        Guid registrationId,
        string verificationCode,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/registration/{registrationId:D}/confirm",
            new { verificationCode },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? response.ReasonPhrase ?? "Bevestiging mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<RegistrationActivationResult>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Lege bevestigingsrespons.");
    }

    public async Task<RegistrationActivationResult> ActivateRegistrationAsync(
        string token,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/registration/activate?token={Uri.EscapeDataString(token)}",
            null,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? response.ReasonPhrase ?? "Activatie mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<RegistrationActivationResult>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Lege activatierespons.");
    }

    public async Task<IReadOnlyList<TakeoverInboxItem>> GetTakeoverInboxAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<TakeoverInboxItem>>("api/registration/takeovers", ct) ?? [];

    public async Task<TakeoverDecisionResult> ApproveTakeoverAsync(Guid takeoverId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/registration/takeovers/{takeoverId}/approve", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? response.ReasonPhrase ?? "Goedkeuren mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<TakeoverDecisionResult>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Lege takeover-respons.");
    }

    public async Task<TakeoverDecisionResult> RejectTakeoverAsync(
        Guid takeoverId,
        string? note = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/registration/takeovers/{takeoverId}/reject",
            new { note },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? response.ReasonPhrase ?? "Afwijzen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<TakeoverDecisionResult>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Lege takeover-respons.");
    }

    public async Task<CompanySummary?> UpdateTokenManagementAsync(
        Guid companyId,
        bool tokensManagedByEnterprise,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/companies/{companyId}/token-management",
            new { tokensManagedByEnterprise },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<CompanySummary>(cancellationToken: ct);
    }

    public async Task<CompanySummary?> UpdateCsvBatchImportAsync(
        Guid companyId,
        bool csvBatchImportEnabled,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/companies/{companyId}/csv-batch-import",
            new { csvBatchImportEnabled },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<CompanySummary>(cancellationToken: ct);
    }

    public async Task<CompanySummary?> UpdateEmailVerificationPreferenceAsync(
        Guid companyId,
        bool requireEmailVerificationForApplications,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/companies/{companyId}/email-verification",
            new { requireEmailVerificationForApplications },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<CompanySummary>(cancellationToken: ct);
    }

    public async Task<CompanySummary?> UpdateContactPreferenceAsync(
        Guid companyId,
        bool directContactEnabled,
        bool contactPreferMail,
        bool contactPreferPhone,
        bool contactPreferWhatsApp,
        string? contactEmail,
        string? contactPhone,
        string? contactWhatsApp,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/companies/{companyId}/contact-preference",
            new
            {
                directContactEnabled,
                contactPreferMail,
                contactPreferPhone,
                contactPreferWhatsApp,
                contactEmail,
                contactPhone,
                contactWhatsApp
            },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<CompanySummary>(cancellationToken: ct);
    }

    public async Task<CompanySummary?> UpdateBillingPreferenceAsync(
        Guid companyId,
        string? preferredPaymentMethod,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/companies/{companyId}/billing-preference",
            new { preferredPaymentMethod },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<CompanySummary>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<CompanyBillingHistoryItem>> GetCompanyBillingHistoryAsync(
        Guid companyId,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<CompanyBillingHistoryItem>>(
            $"api/companies/{companyId}/billing-history", ct) ?? [];

    public async Task DownloadCompanyBillingInvoicePdfAsync(
        Guid companyId,
        Guid invoiceId,
        string invoiceNumber,
        IJSRuntime js,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/companies/{companyId}/billing/invoices/{invoiceId:D}/pdf", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = string.IsNullOrWhiteSpace(invoiceNumber) ? $"{invoiceId:N}.pdf" : $"{invoiceNumber}.pdf";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, "application/pdf");
    }

    public async Task<IReadOnlyList<CompanyUserItem>> GetCompanyUsersAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<CompanyUserItem>>("api/company-users", ct) ?? [];

    public async Task<CompanyUserItem?> InviteCompanyUserAsync(InviteUserForm form, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/company-users/invite", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<CompanyUserItem>(cancellationToken: ct);
    }

    public async Task<CompanyUserItem?> UpdateCompanyUserAsync(Guid userId, UpdateCompanyUserForm form, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/company-users/{userId}", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<CompanyUserItem>(cancellationToken: ct);
    }

    public async Task<CandidateInsightsDto?> GetCandidateInsightsAsync(
        Guid? branchId,
        int radiusKm = 20,
        int period = 90,
        CancellationToken ct = default)
    {
        try
        {
            var qs = $"radiusKm={radiusKm}&period={period}";
            if (branchId is Guid id)
            {
                qs += $"&branchId={id:D}";
            }

            return await _http.GetFromJsonAsync<CandidateInsightsDto>($"api/employer/candidate-insights?{qs}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<CandidateInsightsBranchDto>> GetCandidateInsightsBranchesAsync(
        CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<CandidateInsightsBranchDto>>(
                       "api/employer/candidate-insights/branches", ct)
                   ?? [];
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return [];
        }
    }
}
