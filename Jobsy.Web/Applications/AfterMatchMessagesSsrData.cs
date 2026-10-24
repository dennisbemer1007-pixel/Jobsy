using Jobsy.Core.Features;
using Jobsy.Web.Hosting;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Web.Applications;

internal static class AfterMatchMessagesSsrData
{
    internal sealed record PageData(
        ApplicationsSsrData.PageData Apps,
        Guid ContextApplicationId,
        string BackHref,
        string MessagesBasePath,
        Guid SelectedThreadId,
        string ArchiveHref);

    internal static async Task<PageData?> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        Guid contextApplicationId,
        string? returnUrl,
        Guid? threadId,
        string? candidateEmail,
        CancellationToken cancellationToken = default)
    {
        var flags = await featureFlags.GetAsync(cancellationToken);
        if (!flags.AfterMatchEnabled || !flags.EmployersEnabled)
        {
            return null;
        }

        var apps = await ApplicationsSsrData.LoadAsync(
            api,
            featureFlags,
            culture,
            configuration,
            hostEnvironment,
            isCandidate: true,
            tabRaw: ApplicationsSsrQuery.TabBerichten,
            filterRaw: ApplicationsSsrQuery.FilterAll,
            selectedId: null,
            givenName: null,
            readOnly: false,
            candidateEmail: candidateEmail,
            berichtenThreadId: threadId ?? contextApplicationId,
            cancellationToken: cancellationToken);

        if (!apps.IsAuthenticatedCandidate)
        {
            return null;
        }

        var threads = apps.AllRows
            .Where(SollicitatiesBerichtenPresentation.RowHasMessageThread)
            .ToList();
        if (threads.Count == 0)
        {
            return null;
        }

        var selected = threadId ?? contextApplicationId;
        if (threads.All(r => r.ApplicationId != selected))
        {
            selected = threads[0].ApplicationId!.Value;
        }

        var backHref = ApplicationsSsrQuery.TryGetSafeApplicationsReturn(returnUrl, out var safeReturn)
            ? safeReturn
            : AfterMatchSollicitatiesUrls.AfterMatchBase(contextApplicationId);

        var messagesBasePath = AfterMatchSollicitatiesUrls.Messages(contextApplicationId);

        var archiveHref = ApplicationsSsrQuery.BuildUrl(
            ApplicationsSsrQuery.TabSollicitaties,
            ApplicationsSsrQuery.FilterDone,
            null);

        return new PageData(
            apps,
            contextApplicationId,
            backHref,
            messagesBasePath,
            selected,
            archiveHref);
    }

    internal static string BuildThreadHref(Guid applicationId)
        => AfterMatchSollicitatiesUrls.Messages(applicationId);

    internal static string BuildReportHref(SollicitationRow row)
    {
        if (row.VacancyId is Guid vacancyId)
        {
            return ContentReportEndpoints.BuildFormUrl("vacancy", vacancyId.ToString("D"));
        }

        return ContentReportEndpoints.BuildFormUrl("company", row.RowId.ToString("D"));
    }
}
