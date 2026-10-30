using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerMessagesSsrData
{
    internal sealed record ThreadRow(EmployerApplicationItem Application, AfterMatchThreadDto Thread);

    internal sealed record PageData(
        Guid ContextApplicationId,
        Guid SelectedThreadId,
        string MessagesBasePath,
        string ArchiveHref,
        IReadOnlyList<ThreadRow> Threads);

    internal static async Task<PageData?> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        Guid contextApplicationId,
        Guid? threadId,
        CancellationToken cancellationToken = default)
    {
        var flags = await featureFlags.GetAsync(cancellationToken);
        if (!flags.AfterMatchEnabled || !flags.EmployersEnabled)
        {
            return null;
        }

        var byId = (await LoadThreadRowsAsync(api, cancellationToken))
            .ToDictionary(r => r.Thread.ApplicationId);
        foreach (var row in await LoadContextOnlyAsync(api, contextApplicationId, cancellationToken))
        {
            byId[row.Thread.ApplicationId] = row;
        }

        var threads = byId.Values.ToList();

        if (flags.TestDepthValueMockupEnabled
            && AfterMatchEmployerMessagesDemoHub.IsDemoApplication(contextApplicationId))
        {
            threads = AfterMatchEmployerMessagesDemoHub.Augment(threads, contextApplicationId).ToList();
        }

        if (threads.Count == 0)
        {
            return null;
        }

        var selected = threadId ?? contextApplicationId;
        if (threads.All(r => r.Thread.ApplicationId != selected))
        {
            selected = contextApplicationId;
        }

        var messagesBasePath = AfterMatchEmployerSollicitatiesUrls.Messages(contextApplicationId);
        var archiveHref = "/werkgever/sollicitaties";

        return new PageData(
            contextApplicationId,
            selected,
            messagesBasePath,
            archiveHref,
            threads);
    }

    private static async Task<List<ThreadRow>> LoadThreadRowsAsync(
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<EmployerApplicationItem> applications;
        try
        {
            applications = await LoadAllApplicationsAsync(api, cancellationToken);
        }
        catch
        {
            return [];
        }

        var eligible = applications
            .Where(a => AfterMatchEligible(a.Status))
            .ToList();
        if (eligible.Count == 0)
        {
            return [];
        }

        var loadTasks = eligible.Select(async item =>
        {
            try
            {
                var thread = await api.GetAfterMatchForEmployerAsync(item.Id, cancellationToken);
                return thread is not null && AfterMatchEmployerBerichtenPresentation.ThreadEligible(thread)
                    ? new ThreadRow(item, thread)
                    : null;
            }
            catch
            {
                return null;
            }
        });

        return (await Task.WhenAll(loadTasks))
            .Where(r => r is not null)
            .Cast<ThreadRow>()
            .ToList();
    }

    private static async Task<List<ThreadRow>> LoadContextOnlyAsync(
        JobsyApiClient api,
        Guid contextApplicationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var thread = await api.GetAfterMatchForEmployerAsync(contextApplicationId, cancellationToken);
            if (thread is null || !AfterMatchEmployerBerichtenPresentation.ThreadEligible(thread))
            {
                return [];
            }

            var item = new EmployerApplicationItem
            {
                Id = thread.ApplicationId,
                VacancyTitle = thread.VacancyTitle,
                CompanyName = thread.CompanyName,
                CandidateName = thread.CandidateName,
                Status = "Accepted",
                PiiRevealed = !string.IsNullOrWhiteSpace(thread.CandidateName),
                CreatedAt = thread.AcceptedAtUtc ?? DateTime.UtcNow,
            };
            return [new ThreadRow(item, thread)];
        }
        catch
        {
            return [];
        }
    }

    private static async Task<IReadOnlyList<EmployerApplicationItem>> LoadAllApplicationsAsync(
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        var all = new List<EmployerApplicationItem>();
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

    private static bool AfterMatchEligible(string status)
        => status is "Accepted" or "EmployerContacting" or "Hired";
}
