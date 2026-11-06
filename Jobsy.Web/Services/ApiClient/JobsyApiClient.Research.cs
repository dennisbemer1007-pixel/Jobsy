using System.Net.Http.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<ResearchFeatureStatusClient?> GetResearchFeatureStatusAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<ResearchFeatureStatusClient>("api/public/research/status", ct);

    public async Task<ResearchConsentStateClient?> GetResearchConsentAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<ResearchConsentStateClient>("api/me/research/consent", ct);

    public async Task<ResearchConsentStateClient?> AcceptResearchConsentAsync(
        Components.Shared.ResearchConsentPanel.ResearchDemographicsSubmit demographics,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            "api/me/research/consent",
            new
            {
                demographics = new
                {
                    demographics.AgeBand,
                    demographics.Gender,
                    demographics.EducationLevel,
                    demographics.NativeLanguage,
                    demographics.TestLanguage
                }
            },
            ct);
        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<ResearchConsentStateClient>(response.Content, ct);
    }

    public async Task<ResearchConsentStateClient?> WithdrawResearchConsentAsync(CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync("api/me/research/consent", ct);
        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<ResearchConsentStateClient>(response.Content, ct);
    }

    public async Task<ResearchAdminSettingsClient?> GetResearchAdminSettingsAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<ResearchAdminSettingsClient>("api/admin/research/settings", ct);

    public async Task SaveResearchAdminSettingsAsync(bool enabled, CancellationToken ct = default)
    {
        var response = await PutApiJsonAsync(
            "api/admin/research/settings",
            new ResearchAdminSettingsClient(enabled),
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ResearchDashboardClient?> GetResearchDashboardAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<ResearchDashboardClient>("api/admin/research/dashboard", ct);

    public async Task<IReadOnlyList<ResearchPilotCampaignClient>?> GetResearchPilotCampaignsAsync(CancellationToken ct = default)
        => await GetApiJsonAsync<List<ResearchPilotCampaignClient>>("api/admin/research/pilots", ct);

    public async Task<ResearchPilotCampaignClient?> CreateResearchPilotCampaignAsync(string title, CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync("api/admin/research/pilots", new { title }, ct);
        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<ResearchPilotCampaignClient>(response.Content, ct);
    }

    public async Task<ResearchPilotStartClient?> StartResearchPilotAsync(string code, CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/public/research/pilot/{Uri.EscapeDataString(code)}/start",
            new { testLanguage = (string?)null },
            ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<ResearchPilotStartClient>(response.Content, ct);
    }

    public async Task SubmitResearchPilotAttemptAsync(
        Guid sessionId,
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyDictionary<int, int> itemDurationMs,
        IReadOnlyDictionary<int, bool> itemUnclear,
        string? endComment,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/public/research/sessions/{sessionId}/attempts",
            new
            {
                testKey = AssessmentKindLabels.ToSlug(kind),
                testVersion = 1,
                answers,
                itemDurationMs,
                itemUnclear,
                totalDurationMs = itemDurationMs.Values.Sum(),
                endComment
            },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DownloadResearchExportCsvAsync(IJSRuntime js, CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/admin/research/export.csv", ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, "lobsy_research_export.csv", base64, "text/csv");
    }
}

public sealed class ResearchFeatureStatusClient
{
    public bool Enabled { get; set; }
    public string ConsentTextVersion { get; set; } = "";
    public int TargetRespondents { get; set; }
}

public sealed class ResearchConsentStateClient
{
    public bool HasConsent { get; set; }
    public DateTime? ResearchConsentAt { get; set; }
    public string? ResearchConsentVersion { get; set; }
    public string? AgeBand { get; set; }
    public string? Gender { get; set; }
    public string? EducationLevel { get; set; }
    public string? NativeLanguage { get; set; }
    public string? TestLanguage { get; set; }
}

public sealed record ResearchAdminSettingsClient(bool ResearchConsentEnabled);

public sealed class ResearchDashboardClient
{
    public List<ResearchDashboardRowClient> Rows { get; set; } = [];
    public int TargetPerCell { get; set; }
}

public sealed class ResearchDashboardRowClient
{
    public string TestKey { get; set; } = "";
    public int TestVersion { get; set; }
    public string Language { get; set; } = "";
    public int ConsentedCount { get; set; }
    public int TargetCount { get; set; }
    public bool Suppressed { get; set; }
}

public sealed class ResearchPilotStartClient
{
    public Guid SessionId { get; set; }
    public string CampaignTitle { get; set; } = "";
    public string RespondentHash { get; set; } = "";
}

public sealed class ResearchPilotCampaignClient
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string PilotUrl { get; set; } = "";
}
