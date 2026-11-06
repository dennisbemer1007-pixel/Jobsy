using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<ProactiveMatchingSettingsDto?> GetProactiveMatchingSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            return await GetApiJsonAsync<ProactiveMatchingSettingsDto>("api/me/proactive-matching/settings", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProactiveMatchingSettingsDto?> UpdateProactiveMatchingSettingsAsync(
        ProactiveMatchingSettingsUpdate update,
        CancellationToken ct = default)
    {
        var response = await PutApiJsonAsync("api/me/proactive-matching/settings", update, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Instellingen opslaan mislukt.");
        }

        return await ReadApiJsonAsync<ProactiveMatchingSettingsDto>(response.Content, ct);
    }

    public async Task<IReadOnlyList<ProactiveMatchSuggestionDto>?> ListProactiveMatchSuggestionsAsync(
        int take = 50,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        try
        {
            return await GetApiJsonAsync<List<ProactiveMatchSuggestionDto>>(
                $"api/me/proactive-matching/suggestions?take={take}",
                ct);
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<ProactiveMatchSuggestionDto?> SubmitProactiveMatchFeedbackAsync(
        Guid suggestionId,
        ProactiveMatchFeedback feedback,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/me/proactive-matching/suggestions/{suggestionId:D}/feedback",
            new { Feedback = feedback },
            ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Feedback opslaan mislukt.");
        }

        return await ReadApiJsonAsync<ProactiveMatchSuggestionDto>(response.Content, ct);
    }

    public async Task<IReadOnlyList<ProactiveVacancyShortlistEntryDto>?> GetProactiveVacancyShortlistAsync(
        Guid vacancyId,
        CancellationToken ct = default)
    {
        try
        {
            return await GetApiJsonAsync<List<ProactiveVacancyShortlistEntryDto>>(
                $"api/employer/proactive-matching/vacancies/{vacancyId:D}/shortlist",
                ct);
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<TalentContactRequestDto?> IntroduceProactiveCandidateAsync(
        Guid vacancyId,
        Guid candidateUserId,
        string? message,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/employer/proactive-matching/vacancies/{vacancyId:D}/introduce",
            new { CandidateUserId = candidateUserId, Message = message },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Kennismaking starten mislukt.");
        }

        return await ReadApiJsonAsync<TalentContactRequestDto>(response.Content, ct);
    }
}
