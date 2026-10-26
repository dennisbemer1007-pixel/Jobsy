using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Services;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerWeekAsideLoader
{
    internal static async Task<IReadOnlyList<AfterMatchThreadDto>> LoadScopeThreadsAsync(
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        var applications = await LoadApplicationsAsync(api, cancellationToken);
        var threads = new List<AfterMatchThreadDto>();
        foreach (var app in applications)
        {
            if (!IsAfterMatchEligible(app.Status))
            {
                continue;
            }

            try
            {
                var thread = await api.GetAfterMatchForEmployerAsync(app.Id, cancellationToken);
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

        return threads;
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
