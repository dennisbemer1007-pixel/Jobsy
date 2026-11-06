using System.Net;
using System.Net.Http.Json;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<IReadOnlyList<TrainingPartnerOfferModel>> GetTrainingPartnersCareerStepAsync(
        string? stepTitle,
        IReadOnlyList<string>? gapKeys,
        CancellationToken ct = default)
    {
        var qs = "api/training/partners/career-step";
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(stepTitle))
        {
            parts.Add("stepTitle=" + Uri.EscapeDataString(stepTitle));
        }

        if (gapKeys is { Count: > 0 })
        {
            parts.Add("gaps=" + Uri.EscapeDataString(string.Join(',', gapKeys)));
        }

        if (parts.Count > 0)
        {
            qs += "?" + string.Join('&', parts);
        }

        using var response = await _http.GetAsync(qs, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TrainingPartnerOfferModel>>(ApiJson, ct) ?? [];
    }

    public async Task<TrainingLearnGrowPageModel?> GetTrainingLearnGrowPageAsync(
        string? search,
        string? subject,
        bool freeOnly,
        string? format,
        string? duration,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add("q=" + Uri.EscapeDataString(search));
        }

        if (!string.IsNullOrWhiteSpace(subject) && !subject.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("subject=" + Uri.EscapeDataString(subject));
        }

        if (freeOnly)
        {
            parts.Add("freeOnly=true");
        }

        if (!string.IsNullOrWhiteSpace(format))
        {
            parts.Add("format=" + Uri.EscapeDataString(format));
        }

        if (!string.IsNullOrWhiteSpace(duration))
        {
            parts.Add("duration=" + Uri.EscapeDataString(duration));
        }

        var qs = "api/training/partners/learn-grow";
        if (parts.Count > 0)
        {
            qs += "?" + string.Join('&', parts);
        }

        using var response = await _http.GetAsync(qs, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TrainingLearnGrowPageModel>(ApiJson, ct);
    }

    public async Task<IReadOnlyList<TrainingCompetencyGrowthModel>> GetTrainingCompetencyGrowthAsync(
        CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/training/partners/competency-growth", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TrainingCompetencyGrowthModel>>(ApiJson, ct) ?? [];
    }

    public async Task<TrainingProgressSummaryModel?> GetTrainingProgressAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/training/partners/progress", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TrainingProgressSummaryModel>(ApiJson, ct);
    }

    public async Task ToggleTrainingPartnerOfferSavedAsync(Guid offerId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/training/partners/offers/{offerId:D}/saved", null, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task SetTrainingPartnerOfferProgressAsync(
        Guid offerId,
        int current,
        int total,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/training/partners/offers/{offerId:D}/progress",
            new { current, total },
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<TrainingPartnerAdminModel>> GetTrainingPartnersAdminAsync(
        CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/training/partners/admin", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TrainingPartnerAdminModel>>(ApiJson, ct) ?? [];
    }

    public async Task<TrainingPartnerAdminModel?> UpsertTrainingPartnerAdminAsync(
        TrainingPartnerAdminUpsertModel body,
        CancellationToken ct = default)
    {
        var response = await PutApiJsonAsync("api/training/partners/admin", body, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TrainingPartnerAdminModel>(ApiJson, ct);
    }

    public async Task ImportTrainingPartnerFeedAsync(Guid providerId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/training/partners/admin/{providerId:D}/import", null, ct);
        response.EnsureSuccessStatusCode();
    }
}
