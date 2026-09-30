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

    public async Task<IReadOnlyList<ExclusivitySettingItem>> GetExclusivitySettingsAsync(
        bool admin = false,
        CancellationToken ct = default)
    {
        var url = admin ? "api/exclusivity-settings/admin" : "api/exclusivity-settings";
        return await _http.GetFromJsonAsync<List<ExclusivitySettingItem>>(url, ct) ?? [];
    }

    public async Task<ExclusivitySettingItem?> CreateExclusivitySettingAsync(
        ExclusivitySettingForm form,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/exclusivity-settings", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<ExclusivitySettingItem>(cancellationToken: ct);
    }

    public async Task<ExclusivitySettingItem?> UpdateExclusivitySettingAsync(
        Guid id,
        ExclusivitySettingForm form,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/exclusivity-settings/{id}", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<ExclusivitySettingItem>(cancellationToken: ct);
    }

    public async Task DeleteExclusivitySettingAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/exclusivity-settings/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
    }

    public async Task<List<TrainingProviderAdmin>> GetTrainingProvidersAdminAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<TrainingProviderAdmin>>("api/admin/training/providers", ct) ?? [];

    public async Task<TrainingOfferAdmin> UpsertTrainingOfferAdminAsync(object payload, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/admin/training/offers", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<TrainingOfferAdmin>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Lege opleiding-response.");
    }

    public async Task RecordTrainingConversionAsync(object payload, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/admin/training/conversions", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Conversie matchen mislukt.");
        }
    }

    public async Task<string> ExportTrainingCsvAsync(int year, int month, Guid? providerId, CancellationToken ct = default)
    {
        var url = $"api/admin/training/export?year={year}&month={month}";
        if (providerId is Guid id)
        {
            url += $"&providerId={id:D}";
        }

        return await _http.GetStringAsync(url, ct);
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

    public async Task<SalesCommercialAdminModel?> GetSalesCommercialAdminModelAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<SalesCommercialAdminModel>("api/sales-commercial/admin", ct);

    public async Task<IReadOnlyList<VacancyCategoryItem>> GetVacancyCategoriesAdminAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<VacancyCategoryItem>>("api/vacancy-categories/admin", ct) ?? [];

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

    public async Task<IReadOnlyList<DateTime>> GetSupportAccessNotesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<DateTime>>("api/privacy/support-access-notes", ct)
           ?? [];

    public async Task<IReadOnlyList<AdminCompanyItem>> GetAdminCompaniesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AdminCompanyItem>>("api/admin/companies", ct) ?? [];

    public async Task<AdminCompaniesPage> GetAdminCompaniesPageAsync(
        string? q = null,
        string? type = null,
        string? region = null,
        string? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var qs = new List<string>
        {
            $"page={Math.Max(1, page)}",
            $"pageSize={Math.Clamp(pageSize, 1, 100)}"
        };
        if (!string.IsNullOrWhiteSpace(q)) qs.Add($"q={Uri.EscapeDataString(q)}");
        if (!string.IsNullOrWhiteSpace(type)) qs.Add($"type={Uri.EscapeDataString(type)}");
        if (!string.IsNullOrWhiteSpace(region)) qs.Add($"region={Uri.EscapeDataString(region)}");
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");

        return await _http.GetFromJsonAsync<AdminCompaniesPage>($"api/admin/companies?{string.Join('&', qs)}", ct)
               ?? new AdminCompaniesPage();
    }

    public async Task<IReadOnlyList<AdminKvkIssueItem>> GetAdminKvkIssuesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AdminKvkIssueItem>>("api/admin/companies/kvk-issues", ct) ?? [];

    public async Task RetryCompanyKvkAsync(Guid companyId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/admin/companies/{companyId:D}/kvk-retry", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

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
    {
        var page = await GetAdminUsersPageAsync(page: 1, pageSize: 100, ct: ct);
        return page.Items;
    }

    public async Task<AdminSearchResultDto> AdminSearchAsync(string q, CancellationToken ct = default)
    {
        var url = $"api/admin/search?q={Uri.EscapeDataString(q)}";
        return await _http.GetFromJsonAsync<AdminSearchResultDto>(url, ct)
               ?? new AdminSearchResultDto();
    }

    public async Task<AdminUsersPage> GetAdminUsersPageAsync(
        int page = 1,
        int pageSize = 50,
        string? q = null,
        string? role = null,
        string? companyType = null,
        Guid? companyId = null,
        bool earlyOnly = false,
        string? mfa = null,
        string? active = null,
        string? tab = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>
        {
            $"page={page}",
            $"pageSize={Math.Clamp(pageSize, 1, 100)}"
        };
        if (!string.IsNullOrWhiteSpace(q))
        {
            qs.Add($"q={Uri.EscapeDataString(q)}");
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            qs.Add($"role={Uri.EscapeDataString(role)}");
        }

        if (!string.IsNullOrWhiteSpace(companyType))
        {
            qs.Add($"companyType={Uri.EscapeDataString(companyType)}");
        }

        if (companyId is Guid cid)
        {
            qs.Add($"companyId={cid:D}");
        }

        if (earlyOnly)
        {
            qs.Add("earlyOnly=true");
        }

        if (!string.IsNullOrWhiteSpace(mfa))
        {
            qs.Add($"mfa={Uri.EscapeDataString(mfa)}");
        }

        if (!string.IsNullOrWhiteSpace(active))
        {
            qs.Add($"active={Uri.EscapeDataString(active)}");
        }

        if (!string.IsNullOrWhiteSpace(tab))
        {
            qs.Add($"tab={Uri.EscapeDataString(tab)}");
        }

        return await _http.GetFromJsonAsync<AdminUsersPage>($"api/admin/users?{string.Join('&', qs)}", ct)
               ?? new AdminUsersPage();
    }

    public async Task ResetUserMfaAsync(
        Guid userId,
        string reason,
        string? confirmCode = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/users/{userId:D}/mfa/reset",
            new { reason, confirmCode },
            ct);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(ExtractApiMessage(body) ?? response.ReasonPhrase ?? "MFA reset mislukt.");
    }

    public async Task<IReadOnlyList<AdminUserSessionItem>> GetAdminUserSessionsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<List<AdminUserSessionItem>>(
                   $"api/admin/users/{userId:D}/sessions", ct)
               ?? [];
    }

    public async Task RevokeAdminUserSessionAsync(
        Guid userId,
        Guid sessionId,
        string reason,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/users/{userId:D}/sessions/{sessionId:D}/revoke",
            new { reason },
            ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task RevokeAllAdminUserSessionsAsync(
        Guid userId,
        string reason,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/users/{userId:D}/sessions/revoke-all",
            new { reason },
            ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task BlockAdminUserAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/users/{userId:D}/block",
            new { reason },
            ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task UnblockAdminUserAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/users/{userId:D}/unblock",
            new { reason },
            ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<AdminBulkUsersResponse> BulkAdminUsersAsync(
        string action,
        IReadOnlyList<Guid> userIds,
        string reason,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/users/bulk/{Uri.EscapeDataString(action)}",
            new { userIds, reason },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractApiMessage(body) ?? response.ReasonPhrase ?? "Bulkactie mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<AdminBulkUsersResponse>(cancellationToken: ct)
               ?? new AdminBulkUsersResponse();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(ExtractApiMessage(body) ?? response.ReasonPhrase ?? "Verzoek mislukt.");
    }

    private static string? ExtractApiMessage(string? body)
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
        catch (System.Text.Json.JsonException)
        {
            // fall through
        }

        return body;
    }

    public async Task<SupportAccessGrantItem> RequestSupportAccessAsync(
        Guid? subjectUserId,
        Guid? subjectCompanyId,
        Jobsy.Core.Enums.SupportAccessScope scope,
        string reason,
        string? ticketReference,
        int durationMinutes,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/admin/support-access",
            new
            {
                subjectUserId,
                subjectCompanyId,
                scope,
                reason,
                ticketReference,
                durationMinutes
            },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<SupportAccessGrantItem>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Lege support-access response.");
    }

    public async Task RevokeSupportAccessAsync(Guid grantId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/admin/support-access/{grantId:D}/revoke", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task<IReadOnlyList<SupportAccessGrantItem>> ListSupportAccessAsync(
        bool activeOnly = false,
        int take = 50,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SupportAccessGrantItem>>(
               $"api/admin/support-access?activeOnly={activeOnly}&take={take}", ct)
           ?? [];

    public async Task<PersonalDataAccessLogPage> GetPersonalDataAccessLogAsync(
        int page = 1,
        int pageSize = 50,
        Guid? actorUserId = null,
        Guid? subjectUserId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>
        {
            $"page={page}",
            $"pageSize={Math.Clamp(pageSize, 1, 100)}"
        };
        if (actorUserId is Guid a)
        {
            qs.Add($"actorUserId={a:D}");
        }

        if (subjectUserId is Guid s)
        {
            qs.Add($"subjectUserId={s:D}");
        }

        if (fromUtc is DateTime from)
        {
            qs.Add($"fromUtc={Uri.EscapeDataString(from.ToUniversalTime().ToString("O"))}");
        }

        if (toUtc is DateTime to)
        {
            qs.Add($"toUtc={Uri.EscapeDataString(to.ToUniversalTime().ToString("O"))}");
        }

        return await _http.GetFromJsonAsync<PersonalDataAccessLogPage>(
                   $"api/admin/personal-data-access-log?{string.Join('&', qs)}", ct)
               ?? new PersonalDataAccessLogPage();
    }

    public async Task<IReadOnlyList<AdminVacancyItem>> GetAdminVacanciesAsync(
        string? moderation = null,
        CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(moderation)
            ? "api/admin/vacancies"
            : $"api/admin/vacancies?moderation={Uri.EscapeDataString(moderation)}";
        return await _http.GetFromJsonAsync<List<AdminVacancyItem>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<AtsListingItem>> GetAtsListingsAsync(
        string? status = null,
        string? q = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
        {
            qs.Add($"status={Uri.EscapeDataString(status)}");
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            qs.Add($"q={Uri.EscapeDataString(q)}");
        }

        var url = qs.Count == 0 ? "api/admin/ats/listings" : "api/admin/ats/listings?" + string.Join('&', qs);
        return await _http.GetFromJsonAsync<List<AtsListingItem>>(url, ct) ?? [];
    }

    public async Task<AtsListingItem?> UpdateAtsListingAsync(Guid id, AtsListingUpdateForm form, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/admin/ats/listings/{id}", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<AtsListingItem>(cancellationToken: ct);
    }

    public async Task<AtsApproveResult?> ApproveAtsListingAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/admin/ats/listings/{id}/approve", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<AtsApproveResult>(cancellationToken: ct);
    }

    public async Task RejectAtsListingAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/admin/ats/listings/{id}/reject", new { reason }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task DeleteAtsListingAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/admin/ats/listings/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task<AtsScrapeRunReport> RunAtsScrapeAsync(Guid? sourceId = null, CancellationToken ct = default)
    {
        var url = sourceId is Guid id
            ? $"api/admin/ats/scrape?sourceId={id}"
            : "api/admin/ats/scrape";
        var response = await _http.PostAsync(url, null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<AtsScrapeRunReport>(cancellationToken: ct)
               ?? new AtsScrapeRunReport();
    }

    public async Task<int> RunAtsHealthAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/admin/ats/health", null, ct);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return payload.TryGetProperty("changed", out var c) ? c.GetInt32() : 0;
    }

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

    public async Task<FeedbackListItem> SubmitFeedbackAsync(SubmitFeedbackForm form, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/feedback", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<FeedbackListItem>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Feedback-response ontbreekt.");
    }

    public async Task<IReadOnlyList<FeedbackListItem>> GetFeedbackAsync(
        string? type = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(type))
        {
            qs.Add($"type={Uri.EscapeDataString(type)}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            qs.Add($"status={Uri.EscapeDataString(status)}");
        }

        var url = qs.Count == 0 ? "api/feedback" : "api/feedback?" + string.Join("&", qs);
        return await _http.GetFromJsonAsync<List<FeedbackListItem>>(url, ct) ?? [];
    }

    public Task<FeedbackDetailItem?> GetFeedbackDetailAsync(Guid id, CancellationToken ct = default)
        => _http.GetFromJsonAsync<FeedbackDetailItem>($"api/feedback/{id}", ct);

    public async Task<string?> GetFeedbackScreenshotDataUrlAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/feedback/{id}/screenshot", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length == 0)
        {
            return null;
        }

        var mime = response.Content.Headers.ContentType?.MediaType ?? "image/png";
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    public async Task<FeedbackPromptItem?> SaveFeedbackPromptAsync(
        Guid id,
        string? prompt,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/feedback/{id}/prompt", new { prompt }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<FeedbackPromptItem>(cancellationToken: ct);
    }

    public async Task<FeedbackAutomateItem?> AutomateFeedbackAsync(
        Guid id,
        string? prompt,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/feedback/{id}/automate", new { prompt }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<FeedbackAutomateItem>(cancellationToken: ct);
    }

    public async Task<FeedbackDetailItem?> UpdateFeedbackStatusAsync(
        Guid id,
        string status,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/feedback/{id}/status", new { status }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<FeedbackDetailItem>(cancellationToken: ct);
    }

    public async Task<FeedbackDetailItem?> RefreshFeedbackAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/feedback/{id}/refresh", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<FeedbackDetailItem>(cancellationToken: ct);
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

    public async Task<LobsyCommercialSettingsItem?> UpdateLobsyCommercialAsync(
        LobsyCommercialSettingsItem settings,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/settings/lobsy-commercial",
            new
            {
                settings.MarginPerHourEuro,
                settings.BackofficePartnerName,
                settings.DeepAnalysisPriceEuro,
                settings.AgencyAnnualPriceEuro,
                settings.ContactUnlockCostTokens
            },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Opslaan van Lobsy-bedragen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<LobsyCommercialSettingsItem>(cancellationToken: ct);
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

    public async Task<IReadOnlyList<IntegrationHealthItem>> GetIntegrationHealthAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<IntegrationHealthItem>>("api/integrations/health", ct) ?? [];

    public async Task<AdminTodoResponseItem?> GetAdminTodoAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<AdminTodoResponseItem>("api/admin/todo", ct);

    public async Task<AdminFinanceSummaryItem?> GetAdminFinanceSummaryAsync(
        string period = "week",
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<AdminFinanceSummaryItem>(
            $"api/admin/finance/summary?period={Uri.EscapeDataString(period)}", ct);

    public async Task<KvkUsageItem?> GetKvkUsageAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<KvkUsageItem>("api/integrations/kvk/usage", ct);

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

    public async Task<IReadOnlyList<EmailTemplateItem>> GetEmailTemplatesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<EmailTemplateItem>>("api/settings/email-templates", ct) ?? [];

    public async Task<EmailCatalogSendResultItem?> SendEmailTemplateAsync(
        string key,
        string to,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/settings/email-templates/{Uri.EscapeDataString(key)}/send",
            new { to },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<EmailCatalogSendResultItem>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<EmailCatalogSendResultItem>> SendAllEmailTemplatesAsync(
        string to,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/settings/email-templates/send-all",
            new { to },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<List<EmailCatalogSendResultItem>>(cancellationToken: ct)
               ?? [];
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

    public async Task<PlatformFeatureItem?> PatchPlatformFeaturesAsync(
        PlatformFeaturePatch patch,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/settings/platform-features", patch, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<PlatformFeatureItem>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<SalesParkedBalanceApiItem>> GetSalesParkedBalancesAsync(
        CancellationToken ct = default)
    {
        var items = await _http.GetFromJsonAsync<List<SalesParkedBalanceApiItem>>(
            "api/admin/sales/parked-balances", ct);
        return items ?? [];
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

    public async Task<MarketingFlyerItem?> GetMarketingFlyerAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<MarketingFlyerItem>("api/settings/marketing-flyer", ct);

    public async Task<MarketingFlyerItem?> SaveMarketingFlyerAsync(
        MarketingFlyerItem flyer,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/settings/marketing-flyer", flyer, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<MarketingFlyerItem>(cancellationToken: ct);
    }

    public async Task<MarketingFlyerItem?> ResetMarketingFlyerAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/settings/marketing-flyer/reset", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<MarketingFlyerItem>(cancellationToken: ct);
    }

    public async Task DownloadMarketingFlyerPdfAsync(
        Microsoft.JSInterop.IJSRuntime js,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/settings/marketing-flyer.pdf", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? "Flyer downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, "lobsy-werkgeversflyer.pdf", base64, "application/pdf");
    }

    public async Task<IReadOnlyList<RegionHostItem>> GetRegionHostsAsync(CancellationToken ct = default)
    {
        var rows = await _http.GetFromJsonAsync<List<RegionHostItem>>("api/region-hosts", ct);
        return rows ?? [];
    }

    public async Task<RegionHostItem?> ResolveRegionHostAsync(string? host = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(host)
            ? "api/region-hosts/resolve"
            : $"api/region-hosts/resolve?host={Uri.EscapeDataString(host)}";
        try
        {
            return await _http.GetFromJsonAsync<RegionHostItem>(url, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<RegionHostItem> CreateRegionHostAsync(RegionHostUpsertPayload payload, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/region-hosts", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return (await response.Content.ReadFromJsonAsync<RegionHostItem>(cancellationToken: ct))!;
    }

    public async Task<RegionHostItem> UpdateRegionHostAsync(
        Guid id,
        RegionHostUpsertPayload payload,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/region-hosts/{id}", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return (await response.Content.ReadFromJsonAsync<RegionHostItem>(cancellationToken: ct))!;
    }

    public async Task DeleteRegionHostAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/region-hosts/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
    }

    public async Task<AdminAuditPage> GetAdminAuditAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? action = null,
        Guid? actor = null,
        string? result = null,
        string? targetType = null,
        string? targetId = null,
        string? q = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        var qs = new List<string>
        {
            $"page={page}",
            $"pageSize={Math.Clamp(pageSize, 1, 100)}"
        };
        if (from is DateTime f) qs.Add($"from={Uri.EscapeDataString(f.ToUniversalTime().ToString("O"))}");
        if (to is DateTime t) qs.Add($"to={Uri.EscapeDataString(t.ToUniversalTime().ToString("O"))}");
        if (!string.IsNullOrWhiteSpace(action)) qs.Add($"action={Uri.EscapeDataString(action)}");
        if (actor is Guid a) qs.Add($"actor={a:D}");
        if (!string.IsNullOrWhiteSpace(result)) qs.Add($"result={Uri.EscapeDataString(result)}");
        if (!string.IsNullOrWhiteSpace(targetType)) qs.Add($"targetType={Uri.EscapeDataString(targetType)}");
        if (!string.IsNullOrWhiteSpace(targetId)) qs.Add($"targetId={Uri.EscapeDataString(targetId)}");
        if (!string.IsNullOrWhiteSpace(q)) qs.Add($"q={Uri.EscapeDataString(q)}");

        return await _http.GetFromJsonAsync<AdminAuditPage>($"api/admin/audit?{string.Join('&', qs)}", ct)
               ?? new AdminAuditPage();
    }

    public async Task<byte[]> ExportAdminAuditCsvAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? action = null,
        string? result = null,
        string? q = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (from is DateTime f) qs.Add($"from={Uri.EscapeDataString(f.ToUniversalTime().ToString("O"))}");
        if (to is DateTime t) qs.Add($"to={Uri.EscapeDataString(t.ToUniversalTime().ToString("O"))}");
        if (!string.IsNullOrWhiteSpace(action)) qs.Add($"action={Uri.EscapeDataString(action)}");
        if (!string.IsNullOrWhiteSpace(result)) qs.Add($"result={Uri.EscapeDataString(result)}");
        if (!string.IsNullOrWhiteSpace(q)) qs.Add($"q={Uri.EscapeDataString(q)}");
        var url = qs.Count == 0 ? "api/admin/audit/export" : "api/admin/audit/export?" + string.Join('&', qs);
        return await _http.GetByteArrayAsync(url, ct);
    }

    public async Task<AdminAuditSummary?> GetAdminAuditSummaryAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<AdminAuditSummary>("api/admin/audit/summary", ct);

    public async Task<AdminMfaOverview?> GetAdminMfaOverviewAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<AdminMfaOverview>("api/admin/audit/mfa-overview", ct);

public async Task<IReadOnlyList<Jobsy.Core.Contracts.Scholen.SchoolListItemDto>> GetAdminSchoolsAsync(
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<Jobsy.Core.Contracts.Scholen.SchoolListItemDto>>(
            "api/admin/schools", ct) ?? [];

    public async Task<Jobsy.Core.Contracts.Scholen.SchoolDetailDto?> GetAdminSchoolAsync(
        Guid schoolId,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<Jobsy.Core.Contracts.Scholen.SchoolDetailDto>(
            $"api/admin/schools/{schoolId}", ct);

    public async Task<Jobsy.Core.Contracts.Scholen.SchoolDetailDto?> CreateAdminSchoolAsync(
        Jobsy.Core.Contracts.Scholen.CreateSchoolRequest request,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/admin/schools", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Contracts.Scholen.SchoolDetailDto>(cancellationToken: ct);
    }

    public async Task<Jobsy.Core.Contracts.Scholen.SchoolDetailDto?> UpdateAdminSchoolAsync(
        Guid schoolId,
        Jobsy.Core.Contracts.Scholen.UpdateSchoolRequest request,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/admin/schools/{schoolId}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Contracts.Scholen.SchoolDetailDto>(cancellationToken: ct);
    }

    public async Task<Jobsy.Core.Contracts.Scholen.SchoolDetailDto?> DeactivateAdminSchoolAsync(
        Guid schoolId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/admin/schools/{schoolId}/deactivate", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Contracts.Scholen.SchoolDetailDto>(cancellationToken: ct);
    }

    public async Task<Jobsy.Core.Contracts.Scholen.SchoolDetailDto?> RecordAdminSchoolAgreementAsync(
        Guid schoolId,
        Jobsy.Core.Contracts.Scholen.RecordProcessorAgreementRequest request,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/admin/schools/{schoolId}/processor-agreement", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Contracts.Scholen.SchoolDetailDto>(cancellationToken: ct);
    }

    public async Task<Jobsy.Core.Contracts.Scholen.SchoolStaffInviteResultDto?> InviteAdminSchoolAdminAsync(
        Guid schoolId,
        Jobsy.Core.Contracts.Scholen.InviteSchoolAdminRequest request,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/schools/{schoolId}/invite-admin", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Contracts.Scholen.SchoolStaffInviteResultDto>(cancellationToken: ct);
    }

    public async Task<Jobsy.Core.Contracts.Scholen.SchoolReportViewDto?> GetAdminSchoolReportAsync(
        int? schoolYearStart = null,
        Guid? schoolId = null,
        Jobsy.Core.Enums.SchoolLevel? level = null,
        int? year = null,
        CancellationToken ct = default)
    {
        var q = new List<string>();
        if (schoolYearStart is int sy) q.Add($"schoolYearStart={sy}");
        if (schoolId is Guid sid) q.Add($"schoolId={sid:D}");
        if (level is { } lv) q.Add($"level={lv}");
        if (year is int y) q.Add($"year={y}");
        var url = "api/admin/schools/rapportage" + (q.Count == 0 ? "" : "?" + string.Join('&', q));
        return await _http.GetFromJsonAsync<Jobsy.Core.Contracts.Scholen.SchoolReportViewDto>(url, ct);
    }

    public async Task<IReadOnlyList<int>> GetAdminSchoolReportYearsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<int>>("api/admin/schools/rapportage/years", ct) ?? [];

    public async Task<Jobsy.Core.Contracts.Scholen.SchoolRetentionStatusDto?> GetAdminSchoolRetentionAsync(
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<Jobsy.Core.Contracts.Scholen.SchoolRetentionStatusDto>(
            "api/admin/schools/retention", ct);

    public async Task<Jobsy.Core.Contracts.Scholen.SchoolRetentionDryRunDto?> DryRunAdminSchoolRetentionAsync(
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/admin/schools/retention/dry-run", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Contracts.Scholen.SchoolRetentionDryRunDto>(
            cancellationToken: ct);
    }

    public async Task<Jobsy.Core.Contracts.Scholen.SchoolRetentionImpactDto?> GetAdminSchoolRetentionImpactAsync(
        int month,
        int day,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<Jobsy.Core.Contracts.Scholen.SchoolRetentionImpactDto>(
            $"api/admin/schools/retention/impact?month={month}&day={day}", ct);

    public async Task<Jobsy.Core.Contracts.Scholen.SnapshotTotalsResultDto?> RefreshAdminSchoolAggregatesAsync(
        int? schoolYearStart = null,
        CancellationToken ct = default)
    {
        var url = schoolYearStart is int sy
            ? $"api/admin/schools/aggregates/refresh?schoolYearStart={sy}"
            : "api/admin/schools/aggregates/refresh";
        var response = await _http.PostAsync(url, null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Contracts.Scholen.SnapshotTotalsResultDto>(
            cancellationToken: ct);
    }

    public async Task DeleteAdminSchoolAsync(
        Guid schoolId,
        string confirmName,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/schools/{schoolId}/delete",
            new Jobsy.Core.Contracts.Scholen.ConfirmSchoolNameRequest(confirmName),
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }
    }

    public string BuildAdminSchoolReportCsvUrl(
        int? schoolYearStart = null,
        Guid? schoolId = null,
        Jobsy.Core.Enums.SchoolLevel? level = null,
        int? year = null)
    {
        var q = new List<string>();
        if (schoolYearStart is int sy) q.Add($"schoolYearStart={sy}");
        if (schoolId is Guid sid) q.Add($"schoolId={sid:D}");
        if (level is { } lv) q.Add($"level={lv}");
        if (year is int y) q.Add($"year={y}");
        return "api/admin/schools/rapportage.csv" + (q.Count == 0 ? "" : "?" + string.Join('&', q));
    }
}
