using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<AfterMatchThreadDto?> GetAfterMatchForCandidateAsync(
        Guid applicationId,
        CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(
            $"api/me/applications/{applicationId:D}/after-match",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<AfterMatchThreadDto>(response.Content, ct);
    }

    public async Task<AfterMatchThreadDto?> GetAfterMatchForEmployerAsync(
        Guid applicationId,
        CancellationToken ct = default)
    {
        return await GetApiJsonAsync<AfterMatchThreadDto>(
            $"api/werkgever/applications/{applicationId:D}/after-match",
            ct);
    }

    public async Task<AfterMatchThreadDto> ProposeEmployerAfterMatchSlotsAsync(
        Guid applicationId,
        ProposeEmployerSlotsRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/werkgever/applications/{applicationId:D}/after-match/slots",
            request,
            ct);
        response.EnsureSuccessStatusCode();
        return (await ReadApiJsonAsync<AfterMatchThreadDto>(response.Content, ct))!;
    }

    public async Task<AfterMatchThreadDto> ProposeCandidateAfterMatchSlotAsync(
        Guid applicationId,
        ProposeCandidateSlotRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/me/applications/{applicationId:D}/after-match/slots/propose",
            request,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorException.FromResponseAsync(response, ct);
        }

        return (await ReadApiJsonAsync<AfterMatchThreadDto>(response.Content, ct))!;
    }

    public async Task<AfterMatchThreadDto> SelectAfterMatchSlotAsync(
        Guid applicationId,
        SelectSlotRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/applications/{applicationId:D}/after-match/select-slot",
            request,
            ct);
        response.EnsureSuccessStatusCode();
        return (await ReadApiJsonAsync<AfterMatchThreadDto>(response.Content, ct))!;
    }

    public async Task<AfterMatchThreadDto> RescheduleAfterMatchAsync(
        Guid applicationId,
        RescheduleRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/applications/{applicationId:D}/after-match/reschedule",
            request,
            ct);
        response.EnsureSuccessStatusCode();
        return (await ReadApiJsonAsync<AfterMatchThreadDto>(response.Content, ct))!;
    }

    public async Task<AfterMatchThreadDto> CancelAfterMatchInterviewAsync(
        Guid applicationId,
        RescheduleRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/applications/{applicationId:D}/after-match/cancel",
            request,
            ct);
        response.EnsureSuccessStatusCode();
        return (await ReadApiJsonAsync<AfterMatchThreadDto>(response.Content, ct))!;
    }

    public async Task<SensitiveMessagePreview?> PreviewAfterMatchMessageAsync(
        Guid applicationId,
        SendMessageRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/applications/{applicationId:D}/after-match/messages/preview",
            request,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await ReadApiJsonAsync<SensitiveMessagePreview>(response.Content, ct);
    }

    public async Task<AfterMatchThreadDto> SendAfterMatchMessageAsync(
        Guid applicationId,
        SendMessageRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/applications/{applicationId:D}/after-match/messages",
            request,
            ct);
        response.EnsureSuccessStatusCode();
        return (await ReadApiJsonAsync<AfterMatchThreadDto>(response.Content, ct))!;
    }

    public async Task<bool> TryMarkCandidateAfterMatchMessagesReadAsync(
        Guid applicationId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/me/applications/{applicationId:D}/after-match/messages/read",
            content: null,
            ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> TryMarkEmployerAfterMatchMessagesReadAsync(
        Guid applicationId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/werkgever/applications/{applicationId:D}/after-match/messages/read",
            content: null,
            ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<AfterMatchThreadDto> RecordAfterMatchOutcomeAsync(
        Guid applicationId,
        RecordOutcomeRequest request,
        CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            $"api/werkgever/applications/{applicationId:D}/after-match/outcome",
            request,
            ct);
        response.EnsureSuccessStatusCode();
        return (await ReadApiJsonAsync<AfterMatchThreadDto>(response.Content, ct))!;
    }

    public async Task<AfterMatchReminderPreferenceDto> UpdateAfterMatchReminderPreferencesAsync(
        UpdateReminderPreferenceRequest request,
        CancellationToken ct = default)
    {
        var response = await PutApiJsonAsync("api/me/after-match/reminder-preferences", request, ct);
        response.EnsureSuccessStatusCode();
        return (await ReadApiJsonAsync<AfterMatchReminderPreferenceDto>(response.Content, ct))!;
    }

    public async Task DownloadAfterMatchCalendarAsync(
        Guid applicationId,
        IJSRuntime js,
        CancellationToken ct = default)
    {
        await DownloadNamedFileAsync(
            $"api/applications/{applicationId:D}/after-match/calendar.ics",
            js,
            "lobsy-gesprek.ics",
            ct);
    }

    public async Task<byte[]?> TryDownloadAfterMatchCalendarAsync(
        Guid applicationId,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/applications/{applicationId:D}/after-match/calendar.ics",
            ct);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadAsByteArrayAsync(ct)
            : null;
    }
}
