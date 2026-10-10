using System.Net.Http.Json;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public sealed record Golf2PassportNextStepResponse(string? NextStepTitle);

    public sealed record Golf2ConversationSheetDto(
        string StrengthsText,
        string MotivationText,
        string CustomText,
        string CustomTextLabel);

    public sealed record Golf2OutsideWorkDto(
        Guid Id,
        string ActivityTitle,
        string Description,
        int? HoursPerWeek,
        int SortOrder);

    public sealed record Golf2FourTestsFeedbackDto(
        int HelpfulnessRating,
        string OpenAnswer,
        bool ShareWithPilot);

    public sealed record Golf2WestlandTaskDto(
        Guid Id,
        Guid OccupationId,
        string OccupationTitle,
        string TitleNl,
        bool Gecontroleerd);

    public sealed record WestlandPilotStatsResponse(
        string EnrollmentsStatus,
        int? Enrollments,
        string FeedbackResponsesStatus,
        int? FeedbackResponses,
        string AvgHelpfulnessStatus,
        int? AvgHelpfulnessRating);

    public async Task<Golf2PassportNextStepResponse?> GetGolf2PassportNextStepAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/me/golf2/westland/passport-next-step", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Golf2PassportNextStepResponse>(CaseInsensitiveJson, ct);
    }

    public async Task<Golf2ConversationSheetDto?> GetGolf2ConversationSheetAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/me/golf2/westland/conversation-sheet", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Golf2ConversationSheetDto>(CaseInsensitiveJson, ct);
    }

    public async Task<Golf2ConversationSheetDto> SaveGolf2ConversationSheetAsync(
        string strengthsText,
        string motivationText,
        string customText,
        Dictionary<string, string?>? extraFields,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/me/golf2/westland/conversation-sheet",
            new { strengthsText, motivationText, customText, extraFields },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorException.FromResponseAsync(response, ct);
        }

        return (await response.Content.ReadFromJsonAsync<Golf2ConversationSheetDto>(CaseInsensitiveJson, ct))!;
    }

    public async Task<IReadOnlyList<Golf2OutsideWorkDto>> GetGolf2OutsideWorkAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/me/golf2/westland/outside-work", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Golf2OutsideWorkDto>>(CaseInsensitiveJson, ct) ?? [];
    }

    public async Task<Golf2OutsideWorkDto> UpsertGolf2OutsideWorkAsync(
        Guid? id,
        string activityTitle,
        string description,
        int? hoursPerWeek,
        int sortOrder,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/me/golf2/westland/outside-work",
            new { id, activityTitle, description, hoursPerWeek, sortOrder },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorException.FromResponseAsync(response, ct);
        }

        return (await response.Content.ReadFromJsonAsync<Golf2OutsideWorkDto>(CaseInsensitiveJson, ct))!;
    }

    public async Task DeleteGolf2OutsideWorkAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/me/golf2/westland/outside-work/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorException.FromResponseAsync(response, ct);
        }
    }

    public async Task<Golf2FourTestsFeedbackDto?> GetGolf2FourTestsFeedbackAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/me/golf2/westland/four-tests-feedback", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Golf2FourTestsFeedbackDto>(CaseInsensitiveJson, ct);
    }

    public async Task<Golf2FourTestsFeedbackDto> SaveGolf2FourTestsFeedbackAsync(
        int helpfulnessRating,
        string openAnswer,
        bool shareWithPilot,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/me/golf2/westland/four-tests-feedback",
            new { helpfulnessRating, openAnswer, shareWithPilot },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorException.FromResponseAsync(response, ct);
        }

        return (await response.Content.ReadFromJsonAsync<Golf2FourTestsFeedbackDto>(CaseInsensitiveJson, ct))!;
    }

    public async Task<IReadOnlyList<Golf2WestlandTaskDto>> GetGolf2WestlandTasksAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/me/golf2/westland/tasks", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Golf2WestlandTaskDto>>(CaseInsensitiveJson, ct) ?? [];
    }

    public async Task<IReadOnlyList<Guid>> GetGolf2TaskChoicesAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/me/golf2/westland/task-choices", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Guid>>(CaseInsensitiveJson, ct) ?? [];
    }

    public async Task SetGolf2TaskChoicesAsync(IReadOnlyList<Guid> taskIds, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/me/golf2/westland/task-choices",
            new { taskIds },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorException.FromResponseAsync(response, ct);
        }
    }

    public async Task<WestlandPilotStatsResponse?> GetWestlandPilotStatsAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/admin/westland-pilot/stats", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WestlandPilotStatsResponse>(CaseInsensitiveJson, ct);
    }

    public async Task<byte[]?> GetWestlandPilotExportCsvAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/admin/westland-pilot/export.csv", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
