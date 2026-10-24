using System.Globalization;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;

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
        int CountDone);

    internal static async Task<PageData> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        bool isCandidate,
        string? tabRaw,
        string? filterRaw,
        Guid? selectedId,
        string? givenName,
        bool readOnly,
        CancellationToken cancellationToken = default)
    {
        var tab = ApplicationsSsrQuery.NormalizeTab(tabRaw);
        var filter = ApplicationsSsrQuery.NormalizeFilter(filterRaw);
        var flags = await featureFlags.GetAsync(cancellationToken);
        var employersOn = flags.EmployersEnabled;
        var externalOn = flags.CandidateExternalVacanciesEnabled;

        if (!employersOn)
        {
            return Empty(tab, filter, selectedId, givenName, readOnly, isCandidate, employersOn, externalOn);
        }

        if (!isCandidate && !readOnly)
        {
            return Empty(tab, filter, selectedId, givenName, readOnly, isCandidate, employersOn, externalOn);
        }

        var rows = new List<SollicitationRow>();
        try
        {
            var apps = await api.GetMyApplicationsAsync(cancellationToken);
            foreach (var a in apps)
            {
                rows.Add(MapApplication(culture, a));
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
        var sel = selectedId ?? filtered.FirstOrDefault()?.RowId;

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
            counts.Done);
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
            0);

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

        var hint = BuildCardHint(culture, a, timeline);
        var sourceKey = a.IsInitiative ? "Initiative.Panel.Title" : "Bk.Apps.FilterVacancy";

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
            a.NextStepKey);
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
        var local = utc.ToLocalTime();
        var now = DateTime.Now;
        var fmt = local.Year == now.Year ? "d MMMM" : "d MMMM yyyy";
        return local.ToString(fmt, CultureInfoFor(culture));
    }

    internal static string FormatShortDateTime(CultureState culture, DateTime utc)
    {
        var local = utc.ToLocalTime();
        var cultureInfo = CultureInfoFor(culture);
        var day = local.ToString("dddd d MMMM", cultureInfo);
        var time = local.ToString("HH:mm", cultureInfo);
        return $"{day}, {time}";
    }

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
