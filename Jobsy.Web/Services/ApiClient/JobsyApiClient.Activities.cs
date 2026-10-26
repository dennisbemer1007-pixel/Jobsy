using System.Net.Http.Json;
using Jobsy.Core.Contracts;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<JobSearchActivityOverviewDto?> GetJobSearchActivitiesOverviewAsync(
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/me/job-search-activities", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound
            || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JobSearchActivityOverviewDto>(cancellationToken: ct);
    }

    public async Task<bool> TryAddManualJobSearchActivityAsync(
        MultipartFormDataContent content,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/me/job-search-activities", content, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> TryUpdateJobSearchActivityAgreementAsync(
        int activitiesPerPeriod,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/me/job-search-activities/agreement",
            new UpdateActivityAgreementRequest(activitiesPerPeriod),
            ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<byte[]?> TryDownloadJobSearchActivitiesPdfAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/me/job-search-activities/period.pdf", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<(byte[] Content, string ContentType, string FileName)?> TryDownloadJobSearchActivityAttachmentAsync(
        Guid activityId,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/me/job-search-activities/{activityId:D}/attachment", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "bijlage";
        var type = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return (bytes, type, fileName);
    }
}
