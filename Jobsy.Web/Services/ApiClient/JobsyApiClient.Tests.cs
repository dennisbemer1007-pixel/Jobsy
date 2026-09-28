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
    public async Task DownloadDeepAnalysisReportAsync(IJSRuntime js, string kind, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/me/deep-analysis/report?kind={Uri.EscapeDataString(kind)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Rapport downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? $"Lobsy-{kind}-rapport.pdf";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, "application/pdf");
    }

    public async Task<CandidateCompetencyState?> GetMyCompetenciesAsync(CancellationToken ct = default)
        => (await GetMeJsonAsync<CandidateCompetencyState>("api/me/competencies", ct)).Value;

    public async Task<WhoAmIState?> GetMyWhoAmIAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<WhoAmIState>("api/me/who-am-i", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            // Circuit budget exceeded (e.g. slow OpenAI path) — treat as soft miss for retry UI.
            return null;
        }
    }

    public async Task<WhoAmIState> SaveMyWhoAmIAsync(bool includeOnCv, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/me/who-am-i", new { includeOnCv }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Persoonsprofiel opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<WhoAmIState>(cancellationToken: ct)
               ?? new WhoAmIState();
    }

    public async Task<CandidateCompetencyState> SaveMyCompetenciesAsync(
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken ct = default)
    {
        var payload = new
        {
            answers = answers.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            complete
        };
        var response = await _http.PutAsJsonAsync("api/me/competencies", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Competentietest opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<CandidateCompetencyState>(cancellationToken: ct)
               ?? new CandidateCompetencyState();
    }

    public async Task<CandidateCultureState?> GetMyCultureAsync(CancellationToken ct = default)
        => (await GetMeJsonAsync<CandidateCultureState>("api/me/culture", ct)).Value;

    public async Task<CandidateCultureState> SaveMyCultureAsync(
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken ct = default)
    {
        var payload = new
        {
            answers = answers.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            complete
        };
        var response = await _http.PutAsJsonAsync("api/me/culture", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Cultuurscan opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<CandidateCultureState>(cancellationToken: ct)
               ?? new CandidateCultureState();
    }

    public async Task<CandidateValuesState?> GetMyValuesAsync(CancellationToken ct = default)
        => (await GetMeJsonAsync<CandidateValuesState>("api/me/values", ct)).Value;

    public async Task<CandidateValuesState> SaveMyValuesAsync(
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken ct = default)
    {
        var payload = new
        {
            answers = answers.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            complete
        };
        var response = await _http.PutAsJsonAsync("api/me/values", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Waardenscan opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<CandidateValuesState>(cancellationToken: ct)
               ?? new CandidateValuesState();
    }

    public async Task<CareerPathPlanApiModel?> GetCareerPathAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("api/me/career-path", ct);
            if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(body) || body.Trim() == "null")
            {
                return null;
            }

            return JsonSerializer.Deserialize<CareerPathPlanApiModel>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<CareerPathPlanApiModel?> GenerateCareerPathAsync(string dreamTitle, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/me/career-path", new { dreamTitle }, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CareerPathPlanApiModel>(cancellationToken: ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<CareerPathPlanApiModel?> CompleteCareerStepAsync(string stepKey, CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/me/career-path/steps/{Uri.EscapeDataString(stepKey)}/complete",
            content: null,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Stap voltooien mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<CareerPathPlanApiModel>(cancellationToken: ct);
    }

    public async Task<CareerPathPlanApiModel?> UncompleteCareerStepAsync(string stepKey, CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/me/career-path/steps/{Uri.EscapeDataString(stepKey)}/uncomplete",
            content: null,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Stap openzetten mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<CareerPathPlanApiModel>(cancellationToken: ct);
    }

    public async Task<CareerPathPlanApiModel?> ClaimCareerCourseAsync(string courseName, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/me/career-path/courses/claim", new { courseName }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Cursus toevoegen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<CareerPathPlanApiModel>(cancellationToken: ct);
    }

    public async Task<CandidateCareerInterestState?> GetMyCareerInterestsAsync(CancellationToken ct = default)
        => (await GetMeJsonAsync<CandidateCareerInterestState>("api/me/career-interests", ct)).Value;

    public async Task<CandidateCareerInterestState> SaveMyCareerInterestsAsync(
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken ct = default)
    {
        var payload = new
        {
            answers = answers.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            complete
        };
        var response = await _http.PutAsJsonAsync("api/me/career-interests", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Beroepentest opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<CandidateCareerInterestState>(cancellationToken: ct)
               ?? new CandidateCareerInterestState();
    }

    public async Task<RoleFitCheckState?> GetMyRoleFitAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<RoleFitCheckState>("api/me/role-fit", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<CandidateKompasState?> GetMyKompasAsync(CancellationToken ct = default)
    {
        var result = await GetMyKompasResultAsync(ct);
        return result.Value;
    }

    public Task<MeGetResult<CandidateKompasState>> GetMyKompasResultAsync(CancellationToken ct = default)
    {
        if (_meCache is null)
        {
            return GetMeJsonAsync<CandidateKompasState>("api/me/kompas", ct);
        }

        return _meCache.GetOrCreateAsync(
            KompasCacheKey,
            token => GetMeJsonAsync<CandidateKompasState>("api/me/kompas", token),
            ct);
    }

    public Task<MeGetResult<CandidateDnaSummary>> GetMyKompasDnaResultAsync(CancellationToken ct = default)
    {
        if (_meCache is null)
        {
            return GetMeJsonAsync<CandidateDnaSummary>("api/me/kompas/dna", ct);
        }

        return _meCache.GetOrCreateAsync(
            KompasDnaCacheKey,
            token => GetMeJsonAsync<CandidateDnaSummary>("api/me/kompas/dna", token),
            ct);
    }

    public async Task<RoleFitCheckState> EvaluateRoleFitAsync(
        string jobTitle,
        Guid? vacancyId = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/me/role-fit", new { jobTitle, vacancyId }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Functie-fit toetsen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<RoleFitCheckState>(cancellationToken: ct)
               ?? new RoleFitCheckState();
    }

    public async Task<List<TrainingOfferCard>> GetTrainingOffersAsync(
        string? jobTitle,
        string campaign,
        CancellationToken ct = default)
    {
        try
        {
            var qs = $"api/me/training-offers?campaign={Uri.EscapeDataString(campaign)}";
            if (!string.IsNullOrWhiteSpace(jobTitle))
            {
                qs += "&jobTitle=" + Uri.EscapeDataString(jobTitle);
            }

            return await _http.GetFromJsonAsync<List<TrainingOfferCard>>(qs, ct) ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<TrainingTrackedLink> TrackTrainingOfferAsync(
        Guid offerId,
        string campaign,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/me/training-offers/{offerId:D}/track",
            new { campaign },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Opleiding openen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<TrainingTrackedLink>(cancellationToken: ct)
               ?? new TrainingTrackedLink();
    }

    public async Task<DeepAnalysisState?> GetDeepAnalysisAsync(string kind, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<DeepAnalysisState>(
                $"api/me/deep-analysis?kind={Uri.EscapeDataString(kind)}",
                ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<AssessmentAdjustmentState?> GetAssessmentAdjustmentsAsync(
        string kind,
        string variant = "quick",
        CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<AssessmentAdjustmentState>(
                $"api/assessments/{Uri.EscapeDataString(kind)}/adjustments?variant={Uri.EscapeDataString(variant)}",
                ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<AssessmentRetakeStartResult> StartAssessmentRetakeAsync(
        string kind,
        string variant = "quick",
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/assessments/{Uri.EscapeDataString(kind)}/retake?variant={Uri.EscapeDataString(variant)}",
            null,
            ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new AssessmentAdjustmentLimitClientException(body);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(ExtractMessage(body) ?? "Opnieuw doen starten mislukt.");
        }

        return System.Text.Json.JsonSerializer.Deserialize<AssessmentRetakeStartResult>(
                   body,
                   new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new AssessmentRetakeStartResult();
    }

    public async Task<IReadOnlyList<AssessmentHistoryItem>> GetAssessmentHistoryAsync(
        string kind,
        string variant = "quick",
        CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<AssessmentHistoryItem>>(
                       $"api/assessments/{Uri.EscapeDataString(kind)}/history?variant={Uri.EscapeDataString(variant)}",
                       ct)
                   ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<DeepAnalysisCheckout> StartDeepAnalysisCheckoutAsync(string kind, CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/me/deep-analysis/checkout?kind={Uri.EscapeDataString(kind)}",
            null,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Checkout starten mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<DeepAnalysisCheckout>(cancellationToken: ct)
               ?? new DeepAnalysisCheckout();
    }

    public async Task<DeepAnalysisState> CompleteDeepAnalysisCheckoutAsync(string paymentId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/me/deep-analysis/checkout/{Uri.EscapeDataString(paymentId)}/complete",
            null,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Betaling afronden mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<DeepAnalysisState>(cancellationToken: ct)
               ?? new DeepAnalysisState();
    }

    public async Task<DeepAnalysisState> SaveDeepAnalysisAsync(
        string kind,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken ct = default)
    {
        var payload = new
        {
            answers = answers.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            complete
        };
        var response = await _http.PutAsJsonAsync(
            $"api/me/deep-analysis?kind={Uri.EscapeDataString(kind)}",
            payload,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Diepte-analyse opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<DeepAnalysisState>(cancellationToken: ct)
               ?? new DeepAnalysisState();
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
}
