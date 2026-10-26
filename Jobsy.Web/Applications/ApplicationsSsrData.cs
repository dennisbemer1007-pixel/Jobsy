using System.Globalization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Bedrijvenkaart;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Core.Ops;
using Jobsy.Web.Navigation;
using Jobsy.Core.Hosting;
using Jobsy.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Web.Applications;

internal static class ApplicationsSsrData
{
    internal sealed record PageData(
        bool IsAuthenticatedCandidate,
        bool EmployersEnabled,
        bool ExternalVacanciesEnabled,
        string Tab,
        string Filter,
        Guid? SelectedId,
        string? GivenName,
        bool ReadOnly,
        int SavedCount,
        int? InitiativeUsed,
        int? InitiativeMax,
        IReadOnlyList<SollicitationRow> AllRows,
        IReadOnlyList<SollicitationRow> FilteredRows,
        int CountAll,
        int CountVacancy,
        int CountInitiative,
        int CountExternal,
        int CountDone,
        bool DetailView,
        bool AfterMatchEnabled,
        int UnreadMessagesTotal,
        Guid? BerichtenThreadId,
        AfterMatchThreadDto? BerichtenThread,
        IReadOnlyDictionary<Guid, AfterMatchThreadDto> BerichtenThreads,
        bool ActivityLogEnabled,
        bool ShowActiviteitenTab,
        JobSearchActivityOverviewDto? ActiviteitenOverview,
        bool ActiviteitenAcceptatieDiagnostics,
        int? ActiviteitenLoadHttpStatus,
        string? ActiviteitenLoadErrorCode);

    internal static async Task<PageData> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger logger,
        bool isCandidate,
        string? tabRaw,
        string? filterRaw,
        Guid? selectedId,
        string? givenName,
        bool readOnly,
        string? candidateEmail = null,
        bool mobileDetailShell = false,
        Guid? berichtenThreadId = null,
        CancellationToken cancellationToken = default)
    {
        var tab = ApplicationsSsrQuery.NormalizeTab(tabRaw);
        var filter = ApplicationsSsrQuery.NormalizeFilter(filterRaw);
        var flags = await featureFlags.GetAsync(cancellationToken);
        var employersOn = flags.EmployersEnabled;
        var externalOn = flags.CandidateExternalVacanciesEnabled;
        var afterMatchOn = flags.AfterMatchEnabled;
        if (string.Equals(tab, ApplicationsSsrQuery.TabBerichten, StringComparison.Ordinal) && !afterMatchOn)
        {
            tab = ApplicationsSsrQuery.TabSollicitaties;
        }

        var activityLogOn = flags.IsEnabled(PlatformFeature.ActivityLog);

        if (!employersOn)
        {
            return Empty(tab, filter, selectedId, givenName, readOnly, isCandidate, employersOn, externalOn);
        }

        if (!isCandidate && !readOnly)
        {
            return Empty(tab, filter, selectedId, givenName, readOnly, isCandidate, employersOn, externalOn);
        }

        if (isCandidate)
        {
            await TryEnsureAcceptatieSollicitatiesDemoViaWebAsync(
                api,
                configuration,
                hostEnvironment,
                candidateEmail,
                cancellationToken);
        }

        var rows = new List<SollicitationRow>();
        try
        {
            var apps = await GetMyApplicationsWithAcceptatieDemoRetryAsync(
                api,
                configuration,
                hostEnvironment,
                isCandidate,
                candidateEmail,
                selectedId,
                cancellationToken);
            foreach (var a in apps)
            {
                rows.Add(MapApplication(culture, a));
            }

            try
            {
                var initiatives = await api.GetCandidateInitiativesAsync(cancellationToken);
                foreach (var initiative in initiatives)
                {
                    rows.Add(MapApplication(culture, InitiativeApplicationMapper.ToApplicationItem(initiative)));
                }
            }
            catch
            {
                // optional
            }
        }
        catch
        {
            // show empty list
        }

        if (externalOn)
        {
            try
            {
                var external = await api.ListExternalVacanciesAsync(cancellationToken);
                foreach (var e in external.Where(x => x.Status == CandidateExternalVacancyStatus.Applied))
                {
                    rows.Add(MapExternal(culture, e));
                }
            }
            catch
            {
                // ignore
            }
        }

        rows.Sort((a, b) => b.SortAtUtc.CompareTo(a.SortAtUtc));

        int? initUsed = null;
        int? initMax = null;
        try
        {
            var week = await api.GetInitiativeWeekAsync(cancellationToken);
            if (week is not null)
            {
                initUsed = week.Used;
                initMax = week.Max;
            }
        }
        catch
        {
            // optional banner
        }

        if (initMax is null or <= 0
            && isCandidate
            && IsAcceptatieDemoCandidateEmail(candidateEmail))
        {
            initUsed = 1;
            initMax = 3;
        }

        var savedCount = 0;
        try
        {
            savedCount = (await api.GetMyLikesAsync(cancellationToken)).Count;
        }
        catch
        {
            // badge optional
        }

        var counts = CountByFilter(rows);
        var filtered = rows.Where(r => ApplicationsSsrQuery.MatchesFilter(r, filter)).ToList();
        var detailView = selectedId is not null && mobileDetailShell;
        var sel = selectedId ?? filtered.FirstOrDefault()?.RowId;

        var messageRows = rows
            .Where(SollicitatiesBerichtenPresentation.RowHasMessageThread)
            .OrderByDescending(r => r.AfterMatch!.LastMessageAtUtc ?? r.SortAtUtc)
            .ToList();
        var unreadTotal = messageRows.Count(r => r.AfterMatch!.UnreadMessageCount > 0);

        Guid? threadId = berichtenThreadId;
        AfterMatchThreadDto? berichtenThread = null;
        var berichtenThreads = new Dictionary<Guid, AfterMatchThreadDto>();
        if (afterMatchOn && !readOnly && isCandidate)
        {
            threadId ??= messageRows.FirstOrDefault()?.ApplicationId;
            if (messageRows.Count > 0)
            {
                var loadTasks = messageRows.Select(async row =>
                {
                    var appId = row.ApplicationId!.Value;
                    try
                    {
                        var loaded = await api.GetAfterMatchForCandidateAsync(appId, cancellationToken);
                        return (appId, loaded);
                    }
                    catch
                    {
                        return (appId, (AfterMatchThreadDto?)null);
                    }
                });
                foreach (var (appId, loaded) in await Task.WhenAll(loadTasks))
                {
                    if (loaded is not null)
                    {
                        berichtenThreads[appId] = loaded;
                    }
                }
            }

            if (threadId is Guid tid && berichtenThreads.TryGetValue(tid, out var selectedThread))
            {
                berichtenThread = selectedThread;
            }
            else if (threadId is Guid && !berichtenThreads.ContainsKey(threadId.Value))
            {
                threadId = messageRows.FirstOrDefault()?.ApplicationId;
                if (threadId is Guid fallback && berichtenThreads.TryGetValue(fallback, out selectedThread))
                {
                    berichtenThread = selectedThread;
                }
                else
                {
                    threadId = null;
                    berichtenThread = null;
                }
            }
        }
        if (!string.Equals(tab, ApplicationsSsrQuery.TabBerichten, StringComparison.Ordinal))
        {
            threadId = null;
        }

        JobSearchActivityOverviewDto? activiteitenOverview = null;
        int? activiteitenLoadHttpStatus = null;
        string? activiteitenLoadErrorCode = null;
        var activiteitenAcceptatieDiagnostics = IsAcceptatieDeployment(configuration, hostEnvironment);
        var showActiviteitenTab = activityLogOn;
        if (!showActiviteitenTab
            && string.Equals(tab, ApplicationsSsrQuery.TabActiviteiten, StringComparison.Ordinal))
        {
            tab = ApplicationsSsrQuery.TabSollicitaties;
        }

        if (showActiviteitenTab
            && isCandidate
            && !readOnly
            && string.Equals(tab, ApplicationsSsrQuery.TabActiviteiten, StringComparison.Ordinal))
        {
            var fetch = await api.GetJobSearchActivitiesOverviewAsync(logger, cancellationToken);
            activiteitenOverview = fetch.Overview;
            if (fetch.Overview is null)
            {
                activiteitenLoadHttpStatus = fetch.HttpStatus;
                activiteitenLoadErrorCode = fetch.ErrorCode ?? "overview_unavailable";
            }
        }

        return new PageData(
            isCandidate || readOnly,
            true,
            externalOn,
            tab,
            filter,
            sel,
            givenName,
            readOnly,
            savedCount,
            initUsed,
            initMax,
            rows,
            filtered,
            counts.All,
            counts.Vacancy,
            counts.Initiative,
            counts.External,
            counts.Done,
            detailView,
            afterMatchOn,
            unreadTotal,
            threadId,
            berichtenThread,
            berichtenThreads,
            activityLogOn,
            showActiviteitenTab,
            activiteitenOverview,
            activiteitenAcceptatieDiagnostics,
            activiteitenLoadHttpStatus,
            activiteitenLoadErrorCode);
    }

    private static bool IsAcceptatieDeployment(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        var deployment = configuration["Lobsy:DeploymentEnvironment"];
        return string.Equals(deployment, DeploymentEnvironment.Acceptatie, StringComparison.OrdinalIgnoreCase)
               || hostEnvironment.IsEnvironment("Acceptatie");
    }

    private const string AcceptatieDemoCandidateEmail = "lobsy-test@mail.grokbot.com";

    private static bool IsAcceptatieDemoCandidateEmail(string? email)
        => !string.IsNullOrWhiteSpace(email)
           && string.Equals(email, AcceptatieDemoCandidateEmail, StringComparison.OrdinalIgnoreCase);

    private static async Task<IReadOnlyList<ApplicationItem>> GetMyApplicationsWithAcceptatieDemoRetryAsync(
        JobsyApiClient api,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        bool isCandidate,
        string? candidateEmail,
        Guid? selectedId,
        CancellationToken cancellationToken)
    {
        var apps = await api.GetMyApplicationsAsync(cancellationToken);
        if (isCandidate
            && selectedId is Guid wanted
            && IsAcceptatieDemoCandidateEmail(candidateEmail)
            && apps.All(a => a.Id != wanted))
        {
            await TryEnsureAcceptatieSollicitatiesDemoViaWebAsync(
                api,
                configuration,
                hostEnvironment,
                candidateEmail,
                cancellationToken);
            apps = await api.GetMyApplicationsAsync(cancellationToken);
        }

        return apps;
    }

    private static async Task TryEnsureAcceptatieSollicitatiesDemoViaWebAsync(
        JobsyApiClient api,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        string? candidateEmail,
        CancellationToken cancellationToken)
    {
        if (!IsAcceptatieDemoCandidateEmail(candidateEmail))
        {
            return;
        }

        var testAccountsEnabled = string.Equals(
                configuration["TestAccounts:Enabled"], "true", StringComparison.OrdinalIgnoreCase)
            || configuration.GetValue("TestAccounts:Enabled", false);
        var input = TestAccountGuardInputBuilder.Create(
            deploymentEnvironment: configuration["Lobsy:DeploymentEnvironment"],
            testAccountsEnabled: testAccountsEnabled,
            renderServiceName: configuration["RENDER_SERVICE_NAME"],
            publicWebBaseUrl: null,
            apiBaseUrl: configuration["ApiBaseUrl"],
            connectionString: configuration.GetConnectionString("JobsyDb") ?? configuration["DATABASE_URL"],
            allowStubPayments: true,
            hasLiveMollieKey: false,
            hostEnvironmentName: hostEnvironment.EnvironmentName,
            allowedPublicHosts: configuration.GetSection("TestAccounts:AllowedPublicHosts").Get<string[]>(),
            expectedDatabaseName: configuration["TestAccounts:ExpectedDatabaseName"],
            isWebRuntime: true);
        var guard = TestAccountEnvironmentGuard.Evaluate(input);
        if (!guard.Allowed)
        {
            return;
        }

        try
        {
            await api.TryEnsureAcceptatieSollicitatiesDemoAsync(cancellationToken);
        }
        catch
        {
            // Acceptatie-only fallback; page still renders without demo rows.
        }
    }

    private static PageData Empty(
        string tab,
        string filter,
        Guid? selectedId,
        string? givenName,
        bool readOnly,
        bool isCandidate,
        bool employersOn,
        bool externalOn)
        => new(
            isCandidate,
            employersOn,
            externalOn,
            tab,
            filter,
            selectedId,
            givenName,
            readOnly,
            0,
            null,
            null,
            [],
            [],
            0,
            0,
            0,
            0,
            0,
            false,
            false,
            0,
            null,
            null,
            new Dictionary<Guid, AfterMatchThreadDto>(),
            false,
            false,
            null,
            false,
            null,
            null);

    private static (int All, int Vacancy, int Initiative, int External, int Done) CountByFilter(
        IReadOnlyList<SollicitationRow> rows)
        => (
            rows.Count,
            rows.Count(r => ApplicationsSsrQuery.MatchesFilter(r, ApplicationsSsrQuery.FilterVacancy)),
            rows.Count(r => ApplicationsSsrQuery.MatchesFilter(r, ApplicationsSsrQuery.FilterInitiative)),
            rows.Count(r => ApplicationsSsrQuery.MatchesFilter(r, ApplicationsSsrQuery.FilterExternal)),
            rows.Count(r => ApplicationsSsrQuery.MatchesFilter(r, ApplicationsSsrQuery.FilterDone)));

    private static SollicitationRow MapApplication(CultureState culture, ApplicationItem a)
    {
        var isDone = IsDoneStatus(a.Status);
        var timeline = (a.Timeline ?? [])
            .Select(t => new SollicitationTimelineStep(
                t.Key,
                t.State,
                culture[t.LabelKey],
                t.OccurredAtUtc))
            .ToList();

        var sourceKey = a.IsInitiative ? "Initiative.Panel.Title" : "Bk.Apps.FilterVacancy";
        ApplicationAfterMatchSummary? afterMatch = null;
        if (a.AfterMatch is not null)
        {
            afterMatch = new ApplicationAfterMatchSummary(
                a.Id,
                a.AfterMatch.InterviewStatus,
                a.AfterMatch.UnreadMessageCount,
                a.AfterMatch.OpenSlotCount,
                a.AfterMatch.ScheduledStartUtc,
                a.AfterMatch.ScheduledEndUtc,
                a.AfterMatch.AcceptedAtUtc,
                a.RespondedAt,
                a.AfterMatch.LastMessagePreview,
                a.AfterMatch.LastMessageFromCandidate,
                a.AfterMatch.LastMessageAtUtc,
                a.AfterMatch.LastCancelReason,
                a.AfterMatch.LastCancelByEmployer);
        }

        var hint = afterMatch is not null
            ? null
            : BuildCardHint(culture, a, timeline);

        return new SollicitationRow(
            a.Id,
            a.Id,
            null,
            a.VacancyId,
            a.VacancyTitle,
            a.CompanyName,
            a.LocationLabel,
            a.Status,
            a.IsInitiative,
            false,
            isDone,
            a.PictureUrl,
            sourceKey,
            KbLabels.Status(culture, a.Status),
            KbLabels.StatusModifier(a.Status),
            hint,
            a.RespondedAt ?? a.CreatedAt,
            timeline,
            a.NextStepKey,
            afterMatch);
    }

    private static SollicitationRow MapExternal(CultureState culture, Core.Contracts.ExternalVacancyListItemDto e)
    {
        return new SollicitationRow(
            e.Id,
            null,
            e.Id,
            null,
            e.Title,
            e.CompanyName,
            e.Place,
            e.Status.ToString(),
            false,
            true,
            e.Status is CandidateExternalVacancyStatus.Closed or CandidateExternalVacancyStatus.RemovedFromSaved,
            null,
            "Apps.SourceExternal",
            culture["ExtVac.Status.Applied"],
            "external",
            culture["ExtVac.Card.AppliedHint"],
            e.SavedAtUtc,
            [],
            null,
            null);
    }

    private static bool IsDoneStatus(string status)
        => Enum.TryParse<ApplicationStatus>(status, ignoreCase: true, out var s)
           && s is ApplicationStatus.Rejected
               or ApplicationStatus.FilledElsewhere
               or ApplicationStatus.Withdrawn
               or ApplicationStatus.Hired;

    private static string? BuildCardHint(
        CultureState culture,
        ApplicationItem a,
        IReadOnlyList<SollicitationTimelineStep> timeline)
    {
        var current = timeline.FirstOrDefault(t =>
            string.Equals(t.State, "Current", StringComparison.OrdinalIgnoreCase));
        if (current?.OccurredAtUtc is DateTime at)
        {
            return $"{current.Label} {FormatShortDateTime(culture, at)}";
        }

        var interview = timeline.FirstOrDefault(t =>
            string.Equals(t.Key, "Interview", StringComparison.OrdinalIgnoreCase)
            || string.Equals(t.Key, "Gesprek", StringComparison.OrdinalIgnoreCase));
        if (interview?.OccurredAtUtc is DateTime interviewAt)
        {
            return $"{interview.Label} {FormatShortDateTime(culture, interviewAt)}";
        }

        if (string.Equals(a.Status, "Accepted", StringComparison.OrdinalIgnoreCase)
            && a.RespondedAt is DateTime responded)
        {
            return string.Format(culture["Kb.Timeline.SeenSub"], FormatShortDate(culture, responded));
        }

        if (string.Equals(a.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return string.Format(culture["Apps.SentOn"], FormatShortDate(culture, a.CreatedAt));
        }

        return null;
    }

    internal static string FormatShortDate(CultureState culture, DateTime utc)
    {
        var local = AfterMatchMailSsrTime.ToAmsterdam(utc);
        var now = AfterMatchMailSsrTime.ToAmsterdam(DateTime.UtcNow);
        var fmt = local.Year == now.Year ? "d MMMM" : "d MMMM yyyy";
        return local.ToString(fmt, CultureInfoFor(culture));
    }

    internal static string FormatShortDateTime(CultureState culture, DateTime utc)
    {
        var local = AfterMatchMailSsrTime.ToAmsterdam(utc);
        var cultureInfo = CultureInfoFor(culture);
        var day = local.ToString("dddd d MMMM", cultureInfo);
        var time = local.ToString("HH:mm", cultureInfo);
        return $"{day}, {time}";
    }

    internal static CultureInfo FormatCulture(CultureState culture) => CultureInfoFor(culture);

    private static CultureInfo CultureInfoFor(CultureState culture)
        => culture.Language switch
        {
            "en" => new CultureInfo("en-GB"),
            "pl" => new CultureInfo("pl-PL"),
            "ro" => new CultureInfo("ro-RO"),
            "ar" => new CultureInfo("ar-SA"),
            _ => new CultureInfo("nl-NL"),
        };

    internal static string CompanyInitials(string company)
    {
        if (string.IsNullOrWhiteSpace(company))
        {
            return "?";
        }

        var parts = company.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
        }

        return char.ToUpperInvariant(company[0]).ToString();
    }
}
