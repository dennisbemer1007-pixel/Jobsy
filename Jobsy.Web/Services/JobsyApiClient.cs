using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed class JobsyApiClient : IAsyncDisposable
{
    private readonly HttpClient _http;

    public JobsyApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<MasterdataOptionItem>> GetMasterdataAsync(
        string? category = null,
        string? audience = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(category))
        {
            qs.Add($"category={Uri.EscapeDataString(category)}");
        }

        if (!string.IsNullOrWhiteSpace(audience))
        {
            qs.Add($"audience={Uri.EscapeDataString(audience)}");
        }

        var url = qs.Count == 0 ? "api/masterdata" : "api/masterdata?" + string.Join("&", qs);
        return await _http.GetFromJsonAsync<List<MasterdataOptionItem>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<MasterdataOptionItem>> GetMasterdataAdminAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<MasterdataOptionItem>>("api/masterdata/admin", ct) ?? [];

    public async Task<MasterdataOptionItem?> CreateMasterdataAsync(MasterdataOptionForm form, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/masterdata", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<MasterdataOptionItem>(cancellationToken: ct);
    }

    public async Task<MasterdataOptionItem?> UpdateMasterdataAsync(Guid id, MasterdataOptionForm form, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/masterdata/{id}", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<MasterdataOptionItem>(cancellationToken: ct);
    }

    public async Task DeleteMasterdataAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/masterdata/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
    }

    public async Task<IReadOnlyList<VacancyListItem>> GetActiveVacanciesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<VacancyListItem>>("api/vacancies", ct) ?? [];

    public async Task<IReadOnlyList<VacancyListItem>> DiscoverVacanciesAsync(
        double? originLat,
        double? originLng,
        string transport,
        int maxMinutes,
        double? radiusKm,
        int? ageYears = null,
        decimal? minHourlyWage = null,
        decimal? maxHourlyWage = null,
        IEnumerable<string>? workTypes = null,
        string? searchQuery = null,
        int? minHoursPerWeek = null,
        int? maxHoursPerWeek = null,
        CancellationToken ct = default)
    {
        var qs = $"transport={Uri.EscapeDataString(transport)}&maxMinutes={maxMinutes}";
        if (originLat is not null && originLng is not null)
        {
            qs += $"&originLat={originLat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                + $"&originLng={originLng.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }

        if (radiusKm is not null)
        {
            qs += $"&radiusKm={radiusKm.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }

        if (ageYears is not null)
        {
            qs += $"&ageYears={ageYears.Value}";
        }

        if (minHourlyWage is not null)
        {
            qs += $"&minHourlyWage={minHourlyWage.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }

        if (maxHourlyWage is not null)
        {
            qs += $"&maxHourlyWage={maxHourlyWage.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }

        if (minHoursPerWeek is not null)
        {
            qs += $"&minHoursPerWeek={minHoursPerWeek.Value}";
        }

        if (maxHoursPerWeek is not null)
        {
            qs += $"&maxHoursPerWeek={maxHoursPerWeek.Value}";
        }

        if (workTypes is not null)
        {
            foreach (var workType in WorkTypeLabels.NormalizeFilterLabels(workTypes))
            {
                qs += $"&workType={Uri.EscapeDataString(workType)}";
            }
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            qs += $"&q={Uri.EscapeDataString(searchQuery.Trim())}";
        }

        return await _http.GetFromJsonAsync<List<VacancyListItem>>($"api/vacancies/discover?{qs}", ct) ?? [];
    }

    public async Task<VacancyListItem?> GetVacancyAsync(
        Guid id,
        double? originLat = null,
        double? originLng = null,
        string? transport = null,
        int? ageYears = null,
        CancellationToken ct = default)
    {
        var url = $"api/vacancies/{id}";
        var parts = new List<string>();
        if (originLat is not null && originLng is not null)
        {
            parts.Add($"originLat={originLat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            parts.Add($"originLng={originLng.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (!string.IsNullOrWhiteSpace(transport))
        {
            parts.Add($"transport={Uri.EscapeDataString(transport)}");
        }

        if (ageYears is not null)
        {
            parts.Add($"ageYears={ageYears.Value}");
        }

        if (parts.Count > 0)
        {
            url += "?" + string.Join("&", parts);
        }

        return await _http.GetFromJsonAsync<VacancyListItem>(url, ct);
    }

    public async Task<IReadOnlyList<VacancyListItem>> GetManagedVacanciesAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/vacancies/manage", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                ExtractMessage(body)
                ?? $"Vacatures laden mislukt ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<List<VacancyListItem>>(cancellationToken: ct) ?? [];
    }

    public async Task<VacancyListItem?> CreateVacancyAsync(CreateVacancyForm form, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/vacancies", form, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            var feedback = await response.Content.ReadFromJsonAsync<VacancyModerationFeedback>(cancellationToken: ct);
            if (feedback is not null &&
                string.Equals(feedback.Code, Jobsy.Core.Interfaces.VacancyModerationCodes.ContentModeration, StringComparison.Ordinal))
            {
                throw new VacancyModerationException(
                    feedback.Message ?? "De vacaturetekst vraagt om een aanpassing.",
                    feedback.Suggestion ?? "Pas de tekst aan en probeer opnieuw.");
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<VacancyListItem>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<VacancyListItem>> CreateBatchAsync(BatchVacancyForm form, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/vacancies/batch", form, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            var feedback = await response.Content.ReadFromJsonAsync<VacancyModerationFeedback>(cancellationToken: ct);
            if (feedback is not null &&
                string.Equals(feedback.Code, Jobsy.Core.Interfaces.VacancyModerationCodes.ContentModeration, StringComparison.Ordinal))
            {
                throw new VacancyModerationException(
                    feedback.Message ?? "De vacaturetekst vraagt om een aanpassing.",
                    feedback.Suggestion ?? "Pas de tekst aan en probeer opnieuw.");
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<List<VacancyListItem>>(cancellationToken: ct) ?? [];
    }

    public async Task<VacancyProductActionResult?> PublishVacancyAsync(
        Guid vacancyId,
        bool highlight = false,
        bool pushBom = false,
        bool extend = false,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/vacancies/publish",
            new { vacancyId, highlight, pushBom, extend },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(body);
        }

        return await response.Content.ReadFromJsonAsync<VacancyProductActionResult>(cancellationToken: ct);
    }

    public async Task<VacancyProductActionResult?> ApprovePublishAsync(Guid vacancyId, CancellationToken ct = default)
        => await PostVacancyProductAsync($"api/vacancies/{vacancyId}/approve-publish", ct);

    public async Task<VacancyProductActionResult?> HighlightVacancyAsync(Guid vacancyId, CancellationToken ct = default)
        => await PostVacancyProductAsync($"api/vacancies/{vacancyId}/highlight", ct);

    public async Task<PushBomPreview?> PreviewPushBomAsync(Guid vacancyId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<PushBomPreview>($"api/vacancies/{vacancyId}/pushbom/preview", ct);

    public async Task<VacancyProductActionResult?> PushBomVacancyAsync(Guid vacancyId, CancellationToken ct = default)
        => await PostVacancyProductAsync($"api/vacancies/{vacancyId}/pushbom", ct);

    public async Task<VacancyProductActionResult?> ExtendVacancyAsync(Guid vacancyId, CancellationToken ct = default)
        => await PostVacancyProductAsync($"api/vacancies/{vacancyId}/extend", ct);

    public async Task<VacancyProductActionResult?> DeactivateVacancyAsync(Guid vacancyId, CancellationToken ct = default)
        => await PostVacancyProductAsync($"api/vacancies/{vacancyId}/inactive", ct);

    private async Task<VacancyProductActionResult?> PostVacancyProductAsync(string url, CancellationToken ct)
    {
        var response = await _http.PostAsync(url, null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(body);
        }

        return await response.Content.ReadFromJsonAsync<VacancyProductActionResult>(cancellationToken: ct);
    }

    public async Task RecordClickAsync(Guid vacancyId, string? anonymousKey = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/vacancies/{vacancyId}/clicks",
            new { anonymousKey },
            ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Records a click once per vacancy per browser tab (sessionStorage dedupe).
    /// Always includes a stable anonymousKey; the API uses it only when no DB user is resolved.
    /// </summary>
    public async Task RecordClickOnceAsync(
        IJSRuntime js,
        Guid vacancyId,
        CancellationToken ct = default)
    {
        var claimed = await js.InvokeAsync<bool>("jobsyGeo.tryClaimClick", vacancyId.ToString());
        if (!claimed)
        {
            return;
        }

        var anonKey = await js.InvokeAsync<string>("jobsyGeo.getOrCreateAnonymousKey");
        await RecordClickAsync(vacancyId, anonKey, ct);
    }

    public async Task RecordImpressionsAsync(
        IJSRuntime js,
        IEnumerable<Guid> vacancyIds,
        CancellationToken ct = default)
    {
        var ids = vacancyIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var anonKey = await js.InvokeAsync<string>("jobsyGeo.getOrCreateAnonymousKey");
        var response = await _http.PostAsJsonAsync(
            "api/analytics/impressions",
            new { vacancyIds = ids, anonymousKey = anonKey },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RecordSiteVisitOnceAsync(
        IJSRuntime js,
        string? path = null,
        CancellationToken ct = default)
    {
        var claimed = await js.InvokeAsync<bool>("jobsyGeo.tryClaimSiteVisit");
        if (!claimed)
        {
            return;
        }

        var anonKey = await js.InvokeAsync<string>("jobsyGeo.getOrCreateAnonymousKey");
        var response = await _http.PostAsJsonAsync(
            "api/analytics/site-visits",
            new { anonymousKey = anonKey, path },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> GetLikedAsync(Guid vacancyId, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<LikeStatus>($"api/vacancies/{vacancyId}/like", ct);
        return result?.Liked == true;
    }

    public async Task<bool> SetLikedAsync(Guid vacancyId, bool liked, CancellationToken ct = default)
    {
        HttpResponseMessage response;
        if (liked)
        {
            response = await _http.PostAsync($"api/vacancies/{vacancyId}/like", null, ct);
        }
        else
        {
            response = await _http.DeleteAsync($"api/vacancies/{vacancyId}/like", ct);
        }

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LikeStatus>(cancellationToken: ct);
        return result?.Liked == true;
    }

    public async Task ShareVacancyAsync(Guid vacancyId, ShareChannel channel, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/vacancies/{vacancyId}/shares", new { channel }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<MeProfile?> GetMyProfileAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<MeProfile>("api/me/profile", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized || ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.InternalServerError)
        {
            throw new InvalidOperationException(
                "Profiel-API gaf een serverfout (500). Herstart de API zodat de laatste fix actief is.", ex);
        }
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
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/profile", new
        {
            openForWork,
            dateOfBirth,
            preferences,
            homeLatitude,
            homeLongitude,
            clearHomeLocation
        }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MeProfile>(cancellationToken: ct);
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

    public async Task<IReadOnlyList<ApplicationItem>> GetMyApplicationsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ApplicationItem>>("api/me/applications", ct) ?? [];

    public async Task CompleteCandidateHowToAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/me/candidate-how-to-completed", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task WithdrawApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/applications/{applicationId}/withdraw", null, ct);
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

    public async Task DeactivateActiveCompanyApiKeyAsync(Guid companyId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/companies/{companyId}/api-keys/deactivate-active",
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

    public async Task<IReadOnlyList<AdminApiKeyItem>> GetAdminApiKeysAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AdminApiKeyItem>>("api/admin/api-keys", ct) ?? [];

    public async Task DeactivateAdminApiKeyAsync(Guid apiKeyId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/admin/api-keys/{apiKeyId}/deactivate", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
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

    public async Task<IReadOnlyList<KvkEstablishmentItem>> GetRegistrationEstablishmentsAsync(
        string kvkNumber,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<KvkEstablishmentItem>>(
            $"api/registration/kvk/{Uri.EscapeDataString(kvkNumber)}/establishments", ct) ?? [];

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
            salesManagerTrackingCode
        }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? response.ReasonPhrase ?? "Registratie mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<RegistrationSubmitResult>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Lege registratierespons.");
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

    private static string? ExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg))
            {
                return msg.GetString();
            }
        }
        catch
        {
            // fall through
        }

        return body.Length > 400 ? body[..400] : body;
    }

    public async Task<IReadOnlyList<TokenBalance>> GetTokenBalancesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<TokenBalance>>("api/tokens/balance", ct) ?? [];

    public async Task<IReadOnlyList<TokenPackItem>> GetTokenPacksAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<TokenPackItem>>("api/tokens/packs", ct) ?? [];

    public async Task<IReadOnlyList<TokenSpendCostItem>> GetTokenCostsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<TokenSpendCostItem>>("api/tokens/costs", ct) ?? [];

    public async Task<IReadOnlyList<TokenLogItem>> GetTokenLogsAsync(string? companyName = null, CancellationToken ct = default)
    {
        var url = "api/tokens/logs";
        if (!string.IsNullOrWhiteSpace(companyName))
        {
            url += $"?companyName={Uri.EscapeDataString(companyName)}";
        }

        return await _http.GetFromJsonAsync<List<TokenLogItem>>(url, ct) ?? [];
    }

    public async Task<CheckoutResult?> CreateTokenCheckoutAsync(Guid companyId, int packSize, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/tokens/checkout", new { companyId, packSize }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<CheckoutResult>(cancellationToken: ct);
    }

    public async Task CompleteTokenCheckoutAsync(
        string paymentId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/tokens/checkout/complete",
            new { paymentId },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task CompleteTokenCheckoutBySessionAsync(
        Guid checkoutId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/tokens/checkout/complete",
            new { checkoutId },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task AllocateTokensAsync(
        Guid fromCompanyId,
        Guid toCompanyId,
        decimal amount,
        string? note = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/tokens/allocate",
            new { fromCompanyId, toCompanyId, amount, note },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
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

    public async Task<VacancyContactPreferenceItem?> GetVacancyContactPreferenceAsync(
        Guid vacancyId,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/vacancies/{vacancyId}/contact-preference", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<VacancyContactPreferenceItem>(cancellationToken: ct);
    }

    public async Task<VacancyContactPreferenceItem?> UpdateVacancyContactPreferenceAsync(
        Guid vacancyId,
        bool overrideContactPreference,
        bool directContactEnabled,
        bool contactPreferMail,
        bool contactPreferPhone,
        bool contactPreferWhatsApp,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/vacancies/{vacancyId}/contact-preference",
            new
            {
                overrideContactPreference,
                directContactEnabled,
                contactPreferMail,
                contactPreferPhone,
                contactPreferWhatsApp
            },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<VacancyContactPreferenceItem>(cancellationToken: ct);
    }

    public async Task<CsvImportResult?> ImportVacanciesCsvAsync(
        Guid companyId,
        IReadOnlyList<CsvImportRowForm> rows,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/vacancies/csv-import",
            new { companyId, rows },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<CsvImportResult>(cancellationToken: ct);
    }

    public async Task<CsvImportRowResult?> RetryVacancyCsvRowAsync(
        Guid companyId,
        CsvImportRowForm row,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/vacancies/csv-import/row",
            new { companyId, row },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<CsvImportRowResult>(cancellationToken: ct);
    }

    public async Task GrantTokensAsync(
        Guid companyId,
        decimal amount,
        string note,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/tokens/goodwill",
            new { companyId, amount, note },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task<IReadOnlyList<TokenPurchaseFinanceItem>> GetTokenPurchasesAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/tokens/finance/purchases", year, quarter);
        return await _http.GetFromJsonAsync<List<TokenPurchaseFinanceItem>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<TokenGoodwillFinanceItem>> GetTokenGoodwillAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/tokens/finance/goodwill", year, quarter);
        return await _http.GetFromJsonAsync<List<TokenGoodwillFinanceItem>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<VatBufferTransferItem>> GetVatBufferTransfersAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/tokens/finance/vat-transfers", year, quarter);
        return await _http.GetFromJsonAsync<List<VatBufferTransferItem>>(url, ct) ?? [];
    }

    public string GetTokenPurchasesExportUrl(int? year = null, int? quarter = null)
        => BuildTokenFinanceUrl("api/tokens/finance/purchases/export", year, quarter);

    public string GetTokenGoodwillExportUrl(int? year = null, int? quarter = null)
        => BuildTokenFinanceUrl("api/tokens/finance/goodwill/export", year, quarter);

    public async Task DownloadTokenPurchasesCsvAsync(
        Microsoft.JSInterop.IJSRuntime js,
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/tokens/finance/purchases/export", year, quarter);
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = $"token-aankopen-{(year?.ToString() ?? "all")}-Q{(quarter?.ToString() ?? "all")}.csv";
        var base64 = Convert.ToBase64String(bytes);
        await js.InvokeVoidAsync("jobsyDownload.bytes", fileName, base64, "text/csv;charset=utf-8");
    }

    public async Task DownloadTokenGoodwillCsvAsync(
        Microsoft.JSInterop.IJSRuntime js,
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/tokens/finance/goodwill/export", year, quarter);
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = $"token-goodwill-{(year?.ToString() ?? "all")}-Q{(quarter?.ToString() ?? "all")}.csv";
        var base64 = Convert.ToBase64String(bytes);
        await js.InvokeVoidAsync("jobsyDownload.bytes", fileName, base64, "text/csv;charset=utf-8");
    }

    public async Task DownloadTokenInvoicePdfAsync(
        Microsoft.JSInterop.IJSRuntime js,
        Guid invoiceId,
        string invoiceNumber,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/tokens/invoices/{invoiceId}/pdf", ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = $"{invoiceNumber}.pdf";
        var base64 = Convert.ToBase64String(bytes);
        await js.InvokeVoidAsync("jobsyDownload.bytes", fileName, base64, "application/pdf");
    }

    public async Task<PartnerSalesCatalog?> GetPartnerSalesCatalogAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<PartnerSalesCatalog>("api/sales-commercial/catalog", ct);

    public async Task<SalesCommercialAdminModel?> GetSalesCommercialAdminModelAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<SalesCommercialAdminModel>("api/sales-commercial/admin", ct);

    public async Task UpdateSalesCommercialSettingsAsync(
        decimal baseTokenValueEuro,
        decimal highlightCarouselTokens,
        decimal highlightPulseTokens,
        int highlightCarouselDays,
        decimal startHighlightBonusTokens,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/sales-commercial/admin/settings",
            new
            {
                baseTokenValueEuro,
                highlightCarouselTokens,
                highlightPulseTokens,
                highlightCarouselDays,
                startHighlightBonusTokens
            },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateVacancyTypeCostAsync(
        string kind,
        decimal costTokens,
        bool isActive,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/sales-commercial/admin/vacancy-type-costs",
            new { kind, costTokens, isActive },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SalesPackageItem?> UpsertSalesPackageAsync(SalesPackageItem package, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/sales-commercial/admin/packages",
            new
            {
                id = package.Id == Guid.Empty ? (Guid?)null : package.Id,
                name = package.Name,
                code = package.Code,
                category = package.Category,
                tokenAmount = package.TokenAmount,
                priceEuro = package.PriceEuro,
                description = package.Description,
                isActive = package.IsActive,
                sortOrder = package.SortOrder
            },
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SalesPackageItem>(cancellationToken: ct);
    }

    public async Task DeleteSalesPackageAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/sales-commercial/admin/packages/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DownloadPartnerFlyerPdfAsync(
        Microsoft.JSInterop.IJSRuntime js,
        string? trackingCode,
        CancellationToken ct = default)
    {
        var qs = string.IsNullOrWhiteSpace(trackingCode)
            ? "api/sales-commercial/flyer.pdf"
            : $"api/sales-commercial/flyer.pdf?trackingCode={Uri.EscapeDataString(trackingCode.Trim())}";
        var response = await _http.GetAsync(qs, ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = string.IsNullOrWhiteSpace(trackingCode)
            ? "lobsy-partner-flyer.pdf"
            : $"lobsy-partner-flyer-{trackingCode.Trim().ToUpperInvariant()}.pdf";
        var base64 = Convert.ToBase64String(bytes);
        await js.InvokeVoidAsync("jobsyDownload.bytes", fileName, base64, "application/pdf");
    }

    public async Task<IReadOnlyList<VatOpenPeriodItem>> GetVatOpenPeriodsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<VatOpenPeriodItem>>("api/vat/open-periods", ct) ?? [];

    public async Task<VatDeclarationPreviewItem?> PreviewVatDeclarationAsync(
        int year,
        int quarter,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<VatDeclarationPreviewItem>(
            $"api/vat/preview?year={year}&quarter={quarter}", ct);

    public async Task<VatDeclarationListItem?> GenerateVatDeclarationAsync(
        int year,
        int quarter,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/vat/generate", new { year, quarter }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<VatDeclarationListItem>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<VatDeclarationListItem>> GetVatDeclarationsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<VatDeclarationListItem>>("api/vat/declarations", ct) ?? [];

    public async Task DownloadVatDeclarationPdfAsync(
        IJSRuntime js,
        Guid declarationId,
        string periodLabel,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/vat/declarations/{declarationId}/pdf", ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = $"BTW-aangifte-{periodLabel}.pdf";
        var base64 = Convert.ToBase64String(bytes);
        await js.InvokeVoidAsync("jobsyDownload.bytes", fileName, base64, "application/pdf");
    }

    public async Task<IReadOnlyList<SalesManagerCostFinanceItem>> GetSalesManagerCostsAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/vat/sales-manager-costs", year, quarter);
        return await _http.GetFromJsonAsync<List<SalesManagerCostFinanceItem>>(url, ct) ?? [];
    }

    private static string BuildTokenFinanceUrl(string path, int? year, int? quarter)
    {
        var qs = new List<string>();
        if (year is int y)
        {
            qs.Add($"year={y}");
        }

        if (quarter is int q)
        {
            qs.Add($"quarter={q}");
        }

        return qs.Count == 0 ? path : $"{path}?{string.Join('&', qs)}";
    }

    public async Task<IReadOnlyList<EmployerApplicationItem>> GetApplicationsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<EmployerApplicationItem>>("api/applications", ct) ?? [];

    public async Task<IReadOnlyList<RegionItem>> GetRegionsAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/regions", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                ExtractMessage(body)
                ?? $"Regio’s laden mislukt ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<List<RegionItem>>(cancellationToken: ct) ?? [];
    }

    public async Task<RegionItem?> CreateRegionAsync(
        string name,
        Guid organizationCompanyId,
        Guid[]? companyIds = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/regions", new
        {
            name,
            organizationCompanyId,
            companyIds
        }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<RegionItem>(cancellationToken: ct);
    }

    public async Task<RegionItem?> UpdateRegionAsync(
        Guid id,
        string name,
        Guid[] companyIds,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/regions/{id}", new { name, companyIds }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<RegionItem>(cancellationToken: ct);
    }

    public async Task DeleteRegionAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/regions/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task<IReadOnlyList<SalaryTableItem>> GetSalaryTablesAsync(Guid? companyId = null, CancellationToken ct = default)
    {
        var url = "api/salary-tables";
        if (companyId is not null)
        {
            url += $"?companyId={companyId}";
        }

        return await _http.GetFromJsonAsync<List<SalaryTableItem>>(url, ct) ?? [];
    }

    public async Task<SalaryTableItem?> GetSalaryTableAsync(Guid id, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<SalaryTableItem>($"api/salary-tables/{id}", ct);

    public async Task<IReadOnlyList<SalaryTableVacancyItem>> GetSalaryTableVacanciesAsync(Guid id, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SalaryTableVacancyItem>>($"api/salary-tables/{id}/vacancies", ct) ?? [];

    public async Task<SalaryTableItem?> UpsertSalaryTableAsync(UpsertSalaryTableForm form, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/salary-tables", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<SalaryTableItem>(cancellationToken: ct);
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
            confirmLowMatchSafetyNet
        }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<ApplyResultItem>(cancellationToken: ct);
    }

    public async Task<EmployerDirectContactItem?> GetDirectContactForVacancyAsync(
        Guid vacancyId,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/applications/by-vacancy/{vacancyId}/direct-contact", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<EmployerDirectContactItem>(cancellationToken: ct);
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

    public async Task DeleteAccountAsync(string verificationCode, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/privacy/delete-account",
            new { verificationCode },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? response.ReasonPhrase ?? "Verwijderen mislukt.");
        }
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

    public async Task<MockInterviewReply> ContinueMockInterviewAsync(
        Guid vacancyId,
        IReadOnlyList<MockInterviewChatMessage> messages,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/mock-interview", new
        {
            vacancyId,
            messages = messages.Select(m => new { role = m.Role, content = m.Content })
        }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? (string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body));
        }

        return await response.Content.ReadFromJsonAsync<MockInterviewReply>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Geen antwoord van de oefenchat.");
    }

    public async Task<AssistantChatReply> SendAssistantChatAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/assistant/chat", new
        {
            messages = messages.Select(m => new { role = m.Role, content = m.Content })
        }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? (string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body));
        }

        return await response.Content.ReadFromJsonAsync<AssistantChatReply>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Geen antwoord van de assistant.");
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

    public async Task FulfillVacancyAsync(Guid vacancyId, Guid applicationId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/applications/vacancies/{vacancyId}/fulfill/{applicationId}", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task<IReadOnlyList<AdminCompanyItem>> GetAdminCompaniesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AdminCompanyItem>>("api/admin/companies", ct) ?? [];

    public async Task<AdminCompanyItem?> RegisterAdminCompanyFromKvkAsync(
        string kvkNumber,
        string kvkEstablishmentId,
        string type = "Employer",
        Guid? parentCompanyId = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/admin/companies/from-kvk", new
        {
            kvkNumber,
            kvkEstablishmentId,
            type,
            parentCompanyId
        }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<AdminCompanyItem>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<AdminUserItem>> GetAdminUsersAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AdminUserItem>>("api/admin/users", ct) ?? [];

    public async Task<IReadOnlyList<AdminVacancyItem>> GetAdminVacanciesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AdminVacancyItem>>("api/admin/vacancies", ct) ?? [];

    public async Task<VacancyProductActionResult?> AdminExtendVacancyAsync(Guid vacancyId, CancellationToken ct = default)
        => await PostVacancyProductAsync($"api/admin/vacancies/{vacancyId}/extend", ct);

    public async Task<VacancyProductActionResult?> AdminDeactivateVacancyAsync(Guid vacancyId, CancellationToken ct = default)
        => await PostVacancyProductAsync($"api/admin/vacancies/{vacancyId}/inactive", ct);

    public async Task<IReadOnlyList<PlatformLogItem>> GetPlatformLogsAsync(
        string? category = null,
        string? level = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(category))
        {
            qs.Add($"category={Uri.EscapeDataString(category)}");
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            qs.Add($"level={Uri.EscapeDataString(level)}");
        }

        if (from is not null)
        {
            qs.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        }

        if (to is not null)
        {
            qs.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        }

        var url = qs.Count == 0 ? "api/platform-logs" : $"api/platform-logs?{string.Join("&", qs)}";
        return await _http.GetFromJsonAsync<List<PlatformLogItem>>(url, ct) ?? [];
    }

    public async Task<TokenPricingSettings?> GetTokenPricingSettingsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<TokenPricingSettings>("api/settings/token-pricing", ct);

    public async Task UpdateTokenPackAsync(Guid id, decimal priceEuro, bool isActive, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/settings/token-pricing/packs/{id}",
            new { id, priceEuro, isActive },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateTokenCostAsync(Guid id, decimal costTokens, bool isActive, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/settings/token-pricing/costs/{id}",
            new { id, costTokens, isActive },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PushBomSettingsItem?> UpdatePushBomSettingsAsync(
        double radiusKm,
        int maxTravelMinutes,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/settings/token-pricing/pushbom-settings",
            new { radiusKm, maxTravelMinutes },
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PushBomSettingsItem>(cancellationToken: ct);
    }

    public async Task<PushBomPricingTierItem?> UpsertPushBomPricingTierAsync(
        PushBomPricingTierItem tier,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/settings/token-pricing/pushbom-tiers", new
        {
            id = tier.Id == Guid.Empty ? (Guid?)null : tier.Id,
            minCandidates = tier.MinCandidates,
            maxCandidates = tier.MaxCandidates,
            costTokens = tier.CostTokens,
            isActive = tier.IsActive
        }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PushBomPricingTierItem>(cancellationToken: ct);
    }

    public async Task DeletePushBomPricingTierAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/settings/token-pricing/pushbom-tiers/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<EarlyAdapterRuleItem?> UpsertEarlyAdapterRuleAsync(EarlyAdapterRuleItem rule, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/settings/early-adapter-rules", new
        {
            id = rule.Id == Guid.Empty ? (Guid?)null : rule.Id,
            name = rule.Name,
            monthlyGrantTokens = rule.MonthlyGrantTokens,
            purchaseDiscountPercent = rule.PurchaseDiscountPercent,
            isActive = rule.IsActive
        }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EarlyAdapterRuleItem>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<WageRateItem>> GetWageRatesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<WageRateItem>>("api/wages", ct) ?? [];

    public async Task UpsertWageRateAsync(WageRateItem item, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/wages", item, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SemiAnnualWageUpdateResult?> RunSemiAnnualWageUpdateAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/wages/semi-annual-update", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<SemiAnnualWageUpdateResult>(cancellationToken: ct);
    }

    public async Task<WageCheckResult?> CheckWageAsync(decimal hourlyWage, int ageYears = 21, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<WageCheckResult>($"api/wages/check?hourlyWage={hourlyWage}&ageYears={ageYears}", ct);

    public async Task<IReadOnlyList<IntegrationHealthItem>> GetIntegrationHealthAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<IntegrationHealthItem>>("api/integrations/health", ct) ?? [];

    public async Task<IntegrationHealthItem?> TestIntegrationAsync(string key, CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/integrations/health/{Uri.EscapeDataString(key)}/test",
            null,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<IntegrationHealthItem>(cancellationToken: ct);
    }

    public async Task<SendTestMailResultItem?> SendTestMailAsync(string to, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/integrations/health/Mail/send-test",
            new { to },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<SendTestMailResultItem>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<IntegrationCredentialItem>> GetIntegrationCredentialsAsync(
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<IntegrationCredentialItem>>(
            "api/settings/integration-credentials", ct) ?? [];

    public async Task<IntegrationCredentialItem?> SaveIntegrationCredentialAsync(
        string key,
        IntegrationCredentialSaveForm form,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/settings/integration-credentials/{Uri.EscapeDataString(key)}",
            form,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<IntegrationCredentialItem>(cancellationToken: ct);
    }

    public async Task<PlatformFeatureItem?> GetPlatformFeaturesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<PlatformFeatureItem>("api/settings/platform-features", ct);

    public async Task<PlatformFeatureItem?> SavePlatformFeaturesAsync(
        PlatformFeatureItem features,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/settings/platform-features", features, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<PlatformFeatureItem>(cancellationToken: ct);
    }

    public async Task<PlatformCompanyItem?> GetPlatformCompanyAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<PlatformCompanyItem>("api/settings/company", ct);

    public async Task<PlatformCompanyItem?> SavePlatformCompanyAsync(
        PlatformCompanyItem company,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/settings/company", company, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<PlatformCompanyItem>(cancellationToken: ct);
    }

    public async Task<AboutPageItem?> GetPublicAboutPageAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<AboutPageItem>("api/site/about", ct);

    public async Task<AboutPageItem?> GetAboutPageAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<AboutPageItem>("api/settings/about", ct);

    public async Task<AboutPageItem?> SaveAboutPageAsync(
        AboutPageItem about,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/settings/about", about, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<AboutPageItem>(cancellationToken: ct);
    }

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }

    private static string? TryExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return null;
    }

    public async Task<SalesManagerInviteResult?> InviteSalesManagerAsync(
        string email,
        string fullName,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/sales-managers/invite", new { email, fullName }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Uitnodigen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerInviteResult>(cancellationToken: ct);
    }

    public async Task<List<SalesManagerListItem>> GetSalesManagersAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SalesManagerListItem>>("api/sales-managers", ct) ?? [];

    public async Task<SalesManagerDashboard?> GetMySalesManagerDashboardAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales-managers/me/dashboard", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                ExtractMessage(body)
                ?? $"Salesmanager-dashboard mislukt ({(int)response.StatusCode}). Is de API (poort 5200) gestart en gemigreerd?");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerDashboard>(cancellationToken: ct);
    }

    public async Task<SalesManagerDashboard?> GetSalesManagerDashboardAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/sales-managers/{userId}/dashboard", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? $"Dashboard ophalen mislukt ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerDashboard>(cancellationToken: ct);
    }

    public async Task<SalesManagerProfile?> GetMySalesManagerProfileAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales-managers/me/profile", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                ExtractMessage(body)
                ?? $"Salesmanager-profiel mislukt ({(int)response.StatusCode}). Is de API (poort 5200) gestart?");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerProfile>(cancellationToken: ct);
    }

    public async Task<SalesManagerProfile?> UpdateMySalesManagerProfileAsync(
        SalesManagerProfileForm form,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/sales-managers/me/profile", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Profiel opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerProfile>(cancellationToken: ct);
    }

    public async Task<SalesManagerProfile?> SignSalesManagerAgreementAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/sales-managers/me/sign-agreement", new { }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Ondertekenen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerProfile>(cancellationToken: ct);
    }

    public async Task<List<SelfBillingInvoiceItem>> GetMySelfBillingInvoicesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SelfBillingInvoiceItem>>("api/sales-managers/me/invoices", ct) ?? [];

    public async Task<SelfBillingInvoiceItem?> CreateMySelfBillingInvoiceAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/sales-managers/me/invoices", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Factuur aanmaken mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SelfBillingInvoiceItem>(cancellationToken: ct);
    }

    public async Task<SalesManagerPayoutPreview?> GetMyPayoutPreviewAsync(
        decimal? amountExVat = null,
        CancellationToken ct = default)
    {
        var url = amountExVat is null
            ? "api/sales-managers/me/payouts/preview"
            : $"api/sales-managers/me/payouts/preview?amountExVat={amountExVat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return await _http.GetFromJsonAsync<SalesManagerPayoutPreview>(url, ct);
    }

    public async Task<SalesManagerPayoutCheckoutResult?> CreateMyPayoutCheckoutAsync(
        decimal amountExVat,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/sales-managers/me/payouts/checkout",
            new { amountExVat },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Uitbetaling starten mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerPayoutCheckoutResult>(cancellationToken: ct);
    }

    public async Task<SalesManagerPayoutCompleteResult?> CompleteMyPayoutCheckoutAsync(
        string paymentId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/sales-managers/me/payouts/complete",
            new { paymentId },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Uitbetaling afronden mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerPayoutCompleteResult>(cancellationToken: ct);
    }

    public async Task DownloadMySelfBillingInvoiceAsync(
        Guid invoiceId,
        string invoiceNumber,
        IJSRuntime js,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/sales-managers/me/invoices/{invoiceId}/download", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Download mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = string.IsNullOrWhiteSpace(invoiceNumber) ? $"{invoiceId:N}.pdf" : $"{invoiceNumber}.pdf";
        var base64 = Convert.ToBase64String(bytes);
        await js.InvokeVoidAsync("jobsyDownload.bytes", fileName, base64, "application/pdf");
    }

    public async Task<SelfBillingInvoiceItem?> MarkSelfBillingInvoicePaidAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/sales-managers/invoices/{invoiceId}/mark-paid", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Markeren als betaald mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SelfBillingInvoiceItem>(cancellationToken: ct);
    }

    public async Task<OnboardingCheckoutResult?> CreateOnboardingCheckoutAsync(Guid companyId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/companies/{companyId}/onboarding/checkout", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Onboarding-checkout mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<OnboardingCheckoutResult>(cancellationToken: ct);
    }

    public async Task<OnboardingCompleteResult?> CompleteOnboardingCheckoutAsync(
        Guid companyId,
        string paymentId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/companies/{companyId}/onboarding/complete",
            new { paymentId },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Onboarding-betaling afronden mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<OnboardingCompleteResult>(cancellationToken: ct);
    }
}

public sealed class VacancyModerationException : Exception
{
    public VacancyModerationException(string warning, string suggestion)
        : base(warning)
    {
        Warning = warning;
        Suggestion = suggestion;
    }

    public string Warning { get; }
    public string Suggestion { get; }
}

public sealed class VacancyModerationFeedback
{
    public string? Code { get; set; }
    public string? Message { get; set; }
    public string? Suggestion { get; set; }
}

public record CreateVacancyForm(
    Guid CompanyId,
    string Title,
    string Description,
    decimal HourlyWage,
    DateOnly StartDate,
    DateOnly EndDate,
    TransportMode RequiredTransport,
    string[] WorkTypes,
    string? ImageUrl = null,
    string? VideoUrl = null,
    Guid? SalaryTableId = null,
    string? RequiredDrivingLicense = null,
    string? RequiredEducation = null,
    int? MinimumEmployers = null,
    bool OverrideContactPreference = false,
    bool DirectContactEnabled = false,
    bool ContactPreferMail = false,
    bool ContactPreferPhone = false,
    bool ContactPreferWhatsApp = false,
    decimal? MinHoursPerWeek = null,
    decimal? MaxHoursPerWeek = null,
    bool? FlexibleTimes = null,
    Dictionary<string, string[]>? ScheduleSlots = null,
    bool? LegalWorksAfter19 = null,
    bool? LegalNightShift23To06 = null,
    bool? LegalAdultSupervisorPresent = null,
    bool? LegalHandlesMoneyOrClosing = null,
    bool? LegalHeavyOrHazardousWork = null,
    bool ShowClientAddressOnMap = false,
    string Kind = "Regular");

public record BatchVacancyForm(
    string Title,
    string Description,
    decimal HourlyWage,
    DateOnly StartDate,
    DateOnly EndDate,
    TransportMode RequiredTransport,
    string[] WorkTypes,
    Guid[] CompanyIds,
    bool ShowClientAddressOnMap = false);

public sealed class CsvImportRowForm
{
    public int RowNumber { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Branches { get; set; }
    public string? SalaryTableId { get; set; }
    public string? CompanyId { get; set; }
    public string? HourlyWage { get; set; }
    public string? Image { get; set; }
    public string? Video { get; set; }
    public string? Transport { get; set; }
    public string? DrivingLicense { get; set; }
    public string? Education { get; set; }
    public string? MinimumEmployers { get; set; }
    public string? KvkNumber { get; set; }
    public string? KvkEstablishmentId { get; set; }
    public string? ShowClientAddressOnMap { get; set; }
}

public sealed class CsvImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<CsvImportRowResult> Rows { get; set; } = [];
    public string PublishHint { get; set; } = string.Empty;
}

public sealed class CsvImportRowResult
{
    public int RowNumber { get; set; }
    public bool Success { get; set; }
    public Guid? VacancyId { get; set; }
    public string? ErrorMessage { get; set; }
    public CsvImportRowForm Data { get; set; } = new();
}

public sealed class MasterdataOptionItem
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowOnCandidate { get; set; } = true;
    public bool ShowOnVacancy { get; set; } = true;
}

public sealed class MasterdataOptionForm
{
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public bool? ShowOnCandidate { get; set; }
    public bool? ShowOnVacancy { get; set; }
}

public sealed class IntegrationHealthItem
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public DateTime CheckedAtUtc { get; set; }
    public bool? LastPingOk { get; set; }
}

public sealed class SendTestMailResultItem
{
    public bool Ok { get; set; }
    public bool SentViaSmtp { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class IntegrationCredentialItem
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
    public string? ApiKeyMasked { get; set; }
    public bool HasClientSecret { get; set; }
    public string? ClientSecretMasked { get; set; }
    public string? ClientId { get; set; }
    public string? TenantId { get; set; }
    public string? Model { get; set; }
    public string? BaseUrl { get; set; }
    public string? FromAddress { get; set; }
    public bool SupportsApiKey { get; set; }
    public bool SupportsModel { get; set; }
    public bool SupportsOAuth { get; set; }
    public bool SupportsTenantId { get; set; }
    public bool SupportsBaseUrl { get; set; }
    public bool SupportsFromAddress { get; set; }
    public bool? LastPingOk { get; set; }
    public string? LastPingMessage { get; set; }
    public DateTime? LastPingAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class IntegrationCredentialSaveForm
{
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }
    public string? BaseUrl { get; set; }
    public string? FromAddress { get; set; }
    public bool ClearApiKey { get; set; }
    public bool ClearClientSecret { get; set; }
}

public sealed class PlatformFeatureItem
{
    public bool VacancyContentModerationEnabled { get; set; } = true;
    public bool AuthenticatorEnabled { get; set; }
    public bool ExposeRegistrationActivationLinks { get; set; }
    public string PublicWebBaseUrl { get; set; } = "http://localhost:5201";
    public DateTime? UpdatedAtUtc { get; set; }
    public int InactiveCompanyDays { get; set; } = 120;
}

public sealed class PlatformCompanyItem
{
    public string CompanyName { get; set; } = "Lobsy";
    public string Slogan { get; set; } = "Dichtbij genoeg om het pantser te laten vallen";
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; } = "NL";
    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? VatBufferIban { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class AboutPageItem
{
    public string Title { get; set; } = "Wie zijn wij";
    public string Lead { get; set; } = "Over Lobsy — en de mens achter de knop";
    public string BodyHtml { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class SalesManagerInviteResult
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? TemporaryPassword { get; set; }
    public bool CreatedNewUser { get; set; }
}

public sealed class SalesManagerListItem
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? TrackingCode { get; set; }
    public bool IsOnboardingComplete { get; set; }
    public decimal BalanceExVat { get; set; }
    public int SupplierCount { get; set; }
}

public sealed class SalesManagerDashboard
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? TrackingCode { get; set; }
    public bool IsOnboardingComplete { get; set; }
    public decimal BalanceExVat { get; set; }
    public decimal BalanceInclVat { get; set; }
    public decimal UninvoicedExVat { get; set; }
    public decimal OutstandingIssuedExVat { get; set; }
    public List<ReferredSupplierItem> Suppliers { get; set; } = [];
    public List<CommissionEntryItem> RecentLedger { get; set; } = [];
    public List<SelfBillingInvoiceItem> Invoices { get; set; } = [];
}

public sealed class ReferredSupplierItem
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KvkNumber { get; set; } = string.Empty;
    public int? FirstYearSupplierSlot { get; set; }
    public DateTime? FirstYearStartedAt { get; set; }
    public bool HasPaidOnboarding { get; set; }
}

public sealed class CommissionEntryItem
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public decimal AmountExVat { get; set; }
    public decimal VatAmount { get; set; }
    public string? Note { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? InvoiceId { get; set; }
}

public sealed class SelfBillingInvoiceItem
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal SubtotalExVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public sealed class SalesManagerPayoutPreview
{
    public decimal AvailableExVat { get; set; }
    public decimal AmountExVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal AmountInclVat { get; set; }
    public string? Iban { get; set; }
    public string MaskedIban { get; set; } = "—";
    public bool CanPayout { get; set; }
    public string? BlockReason { get; set; }
}

public sealed class SalesManagerPayoutCheckoutResult
{
    public string PaymentId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public decimal AmountEuro { get; set; }
    public string MaskedIban { get; set; } = string.Empty;
    public bool IsStub { get; set; }
}

public sealed class SalesManagerPayoutCompleteResult
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalInclVat { get; set; }
    public string MaskedIban { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class SalesManagerProfile
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Iban { get; set; }
    public string? TrackingCode { get; set; }
    public DateTime? AgreementSignedAt { get; set; }
    public string? AgreementVersion { get; set; }
    public DateTime? OnboardingCompletedAt { get; set; }
    public bool IsOnboardingComplete { get; set; }
}

public sealed class SalesManagerProfileForm
{
    public string CompanyName { get; set; } = string.Empty;
    public string KvkNumber { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Country { get; set; } = "NL";
    public string? Iban { get; set; }
}

public sealed class OnboardingCheckoutResult
{
    public string PaymentId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public decimal AmountEuro { get; set; }
    public bool IsStub { get; set; }
}

public sealed class OnboardingCompleteResult
{
    public Guid CompanyId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool CommissionCredited { get; set; }
    public int? FirstYearSupplierSlot { get; set; }
}

public sealed class TokenPurchaseFinanceItem
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CheckoutId { get; set; }
    public string MolliePaymentId { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int PackSize { get; set; }
    public int AmountExVatCents { get; set; }
    public int VatAmountCents { get; set; }
    public int TotalAmountCents { get; set; }
    public decimal AmountExVatEuro { get; set; }
    public decimal VatAmountEuro { get; set; }
    public decimal TotalAmountEuro { get; set; }
    public DateTime IssuedAt { get; set; }
    public string InvoicePdfUrl { get; set; } = string.Empty;
    public string? VatDeclarationStatusLabel { get; set; }
}

public sealed class TokenGoodwillFinanceItem
{
    public Guid TransactionId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public decimal TokenAmount { get; set; }
    public int AmountExVatCents { get; set; }
    public int VatAmountCents { get; set; }
    public int TotalAmountCents { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? IssuedByUserId { get; set; }
    public string? IssuedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class VatBufferTransferItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string DestinationIbanMasked { get; set; } = string.Empty;
    public int AmountCents { get; set; }
    public decimal AmountEuro { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Note { get; set; }
}

public sealed class VatOpenPeriodItem
{
    public int Year { get; set; }
    public int Quarter { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int OpenTokenInvoiceCount { get; set; }
    public int OpenSalesManagerInvoiceCount { get; set; }
    public bool HasOpenItems { get; set; }
}

public sealed class VatDeclarationPreviewItem
{
    public int Year { get; set; }
    public int Quarter { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int Rubriek1OmzetExVatCents { get; set; }
    public int Rubriek1VatCents { get; set; }
    public int TokenInvoiceCount { get; set; }
    public int GoodwillCount { get; set; }
    public int Rubriek5VoorbelastingCents { get; set; }
    public int Rubriek5CostExVatCents { get; set; }
    public int SalesManagerInvoiceCount { get; set; }
    public int AmountDueCents { get; set; }
    public bool AlreadyDeclared { get; set; }
}

public sealed class VatDeclarationListItem
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Rubriek1OmzetExVatCents { get; set; }
    public int Rubriek1VatCents { get; set; }
    public int Rubriek5VoorbelastingCents { get; set; }
    public int AmountDueCents { get; set; }
    public int TokenInvoiceCount { get; set; }
    public int GoodwillCount { get; set; }
    public int SalesManagerInvoiceCount { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedByName { get; set; }
    public string PlatformCompanyName { get; set; } = string.Empty;
    public bool HasPdf { get; set; }
}

public sealed class SalesManagerCostFinanceItem
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid SalesManagerUserId { get; set; }
    public string SalesManagerCompanyName { get; set; } = string.Empty;
    public decimal SubtotalExVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }
    public string VatTreatment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string? VatDeclarationStatusLabel { get; set; }
}

public sealed class PartnerSalesCatalog
{
    public decimal BaseTokenValueEuro { get; set; }
    public decimal HighlightCarouselTokens { get; set; }
    public decimal HighlightPulseTokens { get; set; }
    public int HighlightCarouselDays { get; set; }
    public decimal StartHighlightBonusTokens { get; set; }
    public List<VacancyTypeCostItem> VacancyTypeCosts { get; set; } = [];
    public List<SalesPackageItem> Packages { get; set; } = [];
}

public sealed class SalesCommercialAdminModel
{
    public Guid SettingsId { get; set; }
    public decimal BaseTokenValueEuro { get; set; }
    public decimal HighlightCarouselTokens { get; set; }
    public decimal HighlightPulseTokens { get; set; }
    public int HighlightCarouselDays { get; set; }
    public decimal StartHighlightBonusTokens { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<VacancyTypeCostItem> VacancyTypeCosts { get; set; } = [];
    public List<SalesPackageItem> Packages { get; set; } = [];
}

public sealed class VacancyTypeCostItem
{
    public string Kind { get; set; } = "Regular";
    public string Label { get; set; } = string.Empty;
    public decimal CostTokens { get; set; }
    public decimal PriceEuro { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SalesPackageItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Category { get; set; } = "Standard";
    public int TokenAmount { get; set; }
    public decimal PriceEuro { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

