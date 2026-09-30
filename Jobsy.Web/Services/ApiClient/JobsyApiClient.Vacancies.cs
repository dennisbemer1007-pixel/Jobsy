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
        IEnumerable<Guid>? categoryIds = null,
        bool? suitableFor65Plus = null,
        IEnumerable<Guid>? companyIds = null,
        int? take = null,
        int? minMatchPercent = null,
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

        if (categoryIds is not null)
        {
            foreach (var id in categoryIds.Where(x => x != Guid.Empty).Distinct())
            {
                qs += $"&categoryId={id:D}";
            }
        }

        if (companyIds is not null)
        {
            foreach (var id in companyIds.Where(x => x != Guid.Empty).Distinct())
            {
                qs += $"&companyId={id:D}";
            }
        }

        if (suitableFor65Plus == true)
        {
            qs += "&suitableFor65Plus=true";
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            qs += $"&q={Uri.EscapeDataString(searchQuery.Trim())}";
        }

        if (take is int cap)
        {
            cap = Math.Clamp(cap, 1, 200);
            qs += $"&take={cap}";
        }

        if (minMatchPercent is int floor)
        {
            qs += $"&minMatchPercent={Math.Clamp(floor, 0, 100)}";
        }

        return await _http.GetFromJsonAsync<List<VacancyListItem>>($"api/vacancies/discover?{qs}", ct) ?? [];
    }

    public async Task<VacancyMapViewResponse?> GetVacancyMapViewAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<VacancyMapViewResponse>("api/vacancies/map-view", ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<VacancyPinBootItem>> GetVacancyPinsAsync(
        string transport,
        int maxMinutes,
        double? originLat = null,
        double? originLng = null,
        double? radiusKm = null,
        IEnumerable<string>? workTypes = null,
        string? searchQuery = null,
        IEnumerable<Guid>? categoryIds = null,
        IEnumerable<Guid>? companyIds = null,
        int? minMatchPercent = null,
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

        if (workTypes is not null)
        {
            foreach (var workType in WorkTypeLabels.NormalizeFilterLabels(workTypes))
            {
                qs += $"&workType={Uri.EscapeDataString(workType)}";
            }
        }

        if (categoryIds is not null)
        {
            foreach (var id in categoryIds.Where(x => x != Guid.Empty).Distinct())
            {
                qs += $"&categoryId={id:D}";
            }
        }

        if (companyIds is not null)
        {
            foreach (var id in companyIds.Where(x => x != Guid.Empty).Distinct())
            {
                qs += $"&companyId={id:D}";
            }
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            qs += $"&q={Uri.EscapeDataString(searchQuery.Trim())}";
        }

        if (minMatchPercent is int floor)
        {
            qs += $"&minMatchPercent={Math.Clamp(floor, 0, 100)}";
        }

        try
        {
            return await _http.GetFromJsonAsync<List<VacancyPinBootItem>>($"api/vacancies/pins?{qs}", ct) ?? [];
        }
        catch
        {
            return [];
        }
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

    public async Task<VacancyTravelResult?> GetVacancyTravelAsync(
        Guid id,
        double originLat,
        double originLng,
        string? transport = null,
        CancellationToken ct = default)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var url =
            $"api/vacancies/{id:D}/travel" +
            $"?originLat={originLat.ToString(inv)}" +
            $"&originLng={originLng.ToString(inv)}";
        if (!string.IsNullOrWhiteSpace(transport))
        {
            url += $"&transport={Uri.EscapeDataString(transport)}";
        }

        using var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<VacancyTravelResult>(cancellationToken: ct);
    }

    public async Task<VacancyCultureFitPoll?> GetVacancyCultureFitAsync(
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<VacancyCultureFitPoll>($"api/vacancies/{id:D}/culture-fit", ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<VacancyListItem>> GetManagedVacanciesAsync(
        IReadOnlyList<Guid>? companyIds = null,
        CancellationToken ct = default)
    {
        var url = "api/vacancies/manage";
        if (companyIds is { Count: > 0 })
        {
            var qs = string.Join("&", companyIds.Select(id => $"companyIds={id:D}"));
            url += "?" + qs;
        }

        using var response = await _http.GetAsync(url, ct);
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
        return await ReadVacancySaveResponseAsync(response, ct);
    }

    public async Task<VacancyListItem?> UpdateVacancyAsync(Guid id, CreateVacancyForm form, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/vacancies/{id}", form, ct);
        return await ReadVacancySaveResponseAsync(response, ct);
    }

    private static async Task<VacancyListItem?> ReadVacancySaveResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
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
        await ThrowIfVacancyProductFailedAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<VacancyProductActionResult>(cancellationToken: ct);
    }

    public async Task<VacancyProductActionResult?> ApprovePublishAsync(Guid vacancyId, CancellationToken ct = default)
        => await PostVacancyProductAsync($"api/vacancies/{vacancyId}/approve-publish", ct);

    public async Task<VacancyProductActionResult?> MarkVacancyReadyAsync(Guid vacancyId, CancellationToken ct = default)
        => await PostVacancyProductAsync($"api/vacancies/{vacancyId}/ready", ct);

    public async Task<VacancyProductActionResult?> ClearVacancyReadyAsync(Guid vacancyId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/vacancies/{vacancyId}/ready", ct);
        await ThrowIfVacancyProductFailedAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<VacancyProductActionResult>(cancellationToken: ct);
    }

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
        await ThrowIfVacancyProductFailedAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<VacancyProductActionResult>(cancellationToken: ct);
    }

    private static async Task ThrowIfVacancyProductFailedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        if ((int)response.StatusCode == 402)
        {
            var info = TryDeserialize<InsufficientTokensInfo>(body);
            if (info is not null
                && string.Equals(info.Code, "InsufficientTokens", StringComparison.OrdinalIgnoreCase))
            {
                throw new InsufficientTokensException(info);
            }
        }

        throw new InvalidOperationException(TryExtractMessage(body) ?? body);
    }

    public async Task RecordClickAsync(
        IJSRuntime js,
        Guid vacancyId,
        string? anonymousKey = null,
        CancellationToken ct = default)
    {
        var response = await PostAnalyticsJsonAsync(
            js,
            $"api/vacancies/{vacancyId}/clicks",
            new { anonymousKey },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RecordClickOnceAsync(
        IJSRuntime js,
        Guid vacancyId,
        CancellationToken ct = default)
    {
        if (!await AllowsAnalyticsAsync(js))
        {
            return;
        }

        var claimed = await js.InvokeAsync<bool>("jobsyGeo.tryClaimClick", vacancyId.ToString());
        if (!claimed)
        {
            return;
        }

        var anonKey = await js.InvokeAsync<string>("jobsyGeo.getOrCreateAnonymousKey");
        await RecordClickAsync(js, vacancyId, anonKey, ct);
    }

    public async Task RecordImpressionsAsync(
        IJSRuntime js,
        IEnumerable<Guid> vacancyIds,
        CancellationToken ct = default)
    {
        if (!await AllowsAnalyticsAsync(js))
        {
            return;
        }

        var ids = vacancyIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var anonKey = await js.InvokeAsync<string>("jobsyGeo.getOrCreateAnonymousKey");
        var response = await PostAnalyticsJsonAsync(
            js,
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
        if (!await AllowsAnalyticsAsync(js))
        {
            return;
        }

        var claimed = await js.InvokeAsync<bool>("jobsyGeo.tryClaimSiteVisit");
        if (!claimed)
        {
            return;
        }

        var anonKey = await js.InvokeAsync<string>("jobsyGeo.getOrCreateAnonymousKey");
        var response = await PostAnalyticsJsonAsync(
            js,
            "api/analytics/site-visits",
            new { anonymousKey = anonKey, path },
            ct);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<bool> AllowsAnalyticsAsync(IJSRuntime js)
    {
        try
        {
            return await js.InvokeAsync<bool>("jobsyCookieConsent.allowsAnalytics");
        }
        catch
        {
            return false;
        }
    }

    private async Task<HttpResponseMessage> PostAnalyticsJsonAsync(
        IJSRuntime js,
        string url,
        object body,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        var token = "";
        try
        {
            token = await js.InvokeAsync<string>("jobsyCookieConsent.get");
        }
        catch
        {
            // private mode / prerender
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.TryAddWithoutValidation(CookieConsentNames.HeaderName, token.Trim());
        }

        return await _http.SendAsync(request, ct);
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

    public async Task ShareVacancyAsync(
        IJSRuntime js,
        Guid vacancyId,
        ShareChannel channel,
        CancellationToken ct = default)
    {
        var response = await PostAnalyticsJsonAsync(js, $"api/vacancies/{vacancyId}/shares", new { channel }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<VacancyPerformanceBoard?> GetVacancyPerformanceAsync(
        string period = "week",
        Guid? companyId = null,
        int take = 3,
        CancellationToken ct = default)
    {
        var qs = $"period={Uri.EscapeDataString(period)}&take={take}";
        if (companyId is not null)
        {
            qs += $"&companyId={companyId}";
        }

        return await _http.GetFromJsonAsync<VacancyPerformanceBoard>(
            $"api/metrics/vacancy-performance?{qs}", ct);
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

    public async Task<VacancyEmailVerificationItem?> UpdateVacancyEmailVerificationAsync(
        Guid vacancyId,
        bool requireEmailVerification,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/vacancies/{vacancyId}/email-verification",
            new { requireEmailVerification },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<VacancyEmailVerificationItem>(cancellationToken: ct);
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

    public async Task<IReadOnlyList<VacancyCategoryItem>> GetVacancyCategoriesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<VacancyCategoryItem>>("api/vacancy-categories", ct) ?? [];

    public async Task<IReadOnlyList<VacancyCategoryFieldItem>> GetVacancyCategoryFieldCatalogAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<VacancyCategoryFieldItem>>("api/vacancy-categories/field-catalog", ct) ?? [];

    public async Task<VacancyCategoryItem?> CreateVacancyCategoryAsync(VacancyCategoryForm form, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/vacancy-categories", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<VacancyCategoryItem>(cancellationToken: ct);
    }

    public async Task<VacancyCategoryItem?> UpdateVacancyCategoryAsync(Guid id, VacancyCategoryForm form, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/vacancy-categories/{id}", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<VacancyCategoryItem>(cancellationToken: ct);
    }

    public async Task<bool> DeleteVacancyCategoryAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/vacancy-categories/{id}", ct);
        return response.IsSuccessStatusCode;
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

    public async Task FulfillVacancyAsync(
        Guid vacancyId,
        Guid applicationId,
        bool rejectOtherApplications = true,
        bool closeVacancy = true,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/applications/vacancies/{vacancyId}/fulfill/{applicationId}",
            new { rejectOtherApplications, closeVacancy },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }
}
