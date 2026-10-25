using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
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

        IReadOnlyList<EmployerApplicationItem> applications;
        try
        {
            applications = await api.GetApplicationsAsync(ct: cancellationToken);
        }
        catch
        {
            return null;
        }

        var eligible = applications
            .Where(a => AfterMatchEligible(a.Status))
            .ToList();
        if (eligible.Count == 0)
        {
            return null;
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

        var threads = (await Task.WhenAll(loadTasks))
            .Where(r => r is not null)
            .Cast<ThreadRow>()
            .OrderByDescending(r =>
            {
                var lastMessage = r.Thread.Messages.Count > 0 ? r.Thread.Messages[^1] : null;
                return lastMessage?.CreatedAtUtc
                       ?? r.Thread.AcceptedAtUtc
                       ?? r.Application.CreatedAt;
            })
            .ToList();

        if (threads.Count == 0)
        {
            return null;
        }

        var selected = threadId ?? contextApplicationId;
        if (threads.All(r => r.Thread.ApplicationId != selected))
        {
            selected = threads[0].Thread.ApplicationId;
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

    private static bool AfterMatchEligible(string status)
        => status is "Accepted" or "EmployerContacting" or "Hired";
}
