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
        CreateManualJobSearchActivityRequest request,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/me/job-search-activities", request, ct);
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
}
