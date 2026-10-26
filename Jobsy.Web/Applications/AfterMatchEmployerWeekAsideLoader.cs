using Jobsy.Core.Contracts;
using Jobsy.Web.Services;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerWeekAsideLoader
{
    internal static async Task<IReadOnlyList<AfterMatchThreadDto>> LoadScopeThreadsAsync(
        JobsyApiClient api,
        Guid contextApplicationId,
        CancellationToken cancellationToken)
    {
        var threads = new List<AfterMatchThreadDto>();
        var seen = new HashSet<Guid>();

        foreach (var app in await LoadApplicationsAsync(api, cancellationToken))
        {
            if (!IsAfterMatchEligible(app.Status))
            {
                continue;
            }

            await TryAddThreadAsync(api, app.Id, threads, seen, cancellationToken);
        }

        if (AfterMatchEmployerMessagesDemoHub.IsDemoApplication(contextApplicationId))
        {
            foreach (var demoId in AfterMatchEmployerMessagesDemoHub.AllDemoApplicationIds)
            {
                await TryAddThreadAsync(api, demoId, threads, seen, cancellationToken);
            }
        }

        return threads;
    }

    private static async Task TryAddThreadAsync(
        JobsyApiClient api,
        Guid applicationId,
        List<AfterMatchThreadDto> threads,
        HashSet<Guid> seen,
        CancellationToken cancellationToken)
    {
        if (!seen.Add(applicationId))
        {
            return;
        }

        try
        {
            var thread = await api.GetAfterMatchForEmployerAsync(applicationId, cancellationToken);
            if (thread is not null)
            {
                threads.Add(thread);
            }
        }
        catch
        {
            // optional peer row
        }
    }

    private static async Task<IReadOnlyList<Jobsy.Web.Models.EmployerApplicationItem>> LoadApplicationsAsync(
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        var all = new List<Jobsy.Web.Models.EmployerApplicationItem>();
        for (var page = 1; page <= 5; page++)
        {
            var batch = await api.GetApplicationsAsync(page: page, pageSize: 100, ct: cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            all.AddRange(batch);
            if (batch.Count < 100)
            {
                break;
            }
        }

        return all;
    }

    private static bool IsAfterMatchEligible(string status)
        => status is "Accepted" or "EmployerContacting" or "Hired";
}
