using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public Task<JobSearchActivitiesClientResult> GetJobSearchActivitiesOverviewAsync(
        ILogger logger,
        CancellationToken ct = default)
        => FetchJobSearchActivitiesOverviewAsync(logger, ct);

    public async Task<JobSearchActivitiesClientResult> FetchJobSearchActivitiesOverviewAsync(
        ILogger logger,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("api/me/job-search-activities", ct);
            var status = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                var overview = await response.Content.ReadFromJsonAsync<JobSearchActivityOverviewDto>(
                    ApiJson,
                    cancellationToken: ct);
                return new JobSearchActivitiesClientResult(overview, status, null, null);
            }

            var code = await ActivityLogClientDiagnostics.ReadErrorCodeAsync(response, ct);
            ActivityLogClientDiagnostics.LogOverviewFailure(logger, response.StatusCode, code);
            return new JobSearchActivitiesClientResult(null, status, code, null);
        }
        catch (Exception ex)
        {
            ActivityLogClientDiagnostics.LogOverviewFailure(logger, HttpStatusCode.ServiceUnavailable, "client_exception", ex);
            return new JobSearchActivitiesClientResult(
                null,
                (int)HttpStatusCode.ServiceUnavailable,
                "client_exception",
                ex.GetType().Name);
        }
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

    public async Task<(byte[]? Bytes, int HttpStatus, string? ErrorCode)> TryDownloadJobSearchActivitiesPdfAsync(
        ILogger logger,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("api/me/job-search-activities/period.pdf", ct);
            var status = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                return (bytes, status, null);
            }

            var code = await ActivityLogClientDiagnostics.ReadErrorCodeAsync(response, ct);
            ActivityLogClientDiagnostics.LogPdfFailure(logger, response.StatusCode, code);
            return (null, status, code);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "job_search_activities_pdf status={Status} code={Code} exception_type={ExceptionType}",
                (int)HttpStatusCode.ServiceUnavailable,
                "client_exception",
                ex.GetType().Name);
            return (null, (int)HttpStatusCode.ServiceUnavailable, "client_exception");
        }
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
