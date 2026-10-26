using System.Globalization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Applications;

internal static class AfterMatchOutcomeSsrData
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");

    internal sealed record SimilarJobRow(
        Guid? VacancyId,
        string Initials,
        string Title,
        string Subtitle,
        string? Hint);

    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string SollicitatiesHref,
        string ActivitiesHref,
        string SimilarJobsHref,
        string LearnHref,
        string AssistantHref,
        string OutcomeReason,
        string YesDateLabel,
        string PlanDateLabel,
        string InterviewDateLabel,
        string OutcomeDateLabel,
        IReadOnlyList<SimilarJobRow> SimilarJobs);

    internal static async Task<PageData?> LoadAsync(
        JobsyApiClient api,
        CultureState culture,
        Guid applicationId,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        var thread = await api.GetAfterMatchForCandidateAsync(applicationId, cancellationToken);
        if (thread is null || !AfterMatchSsrRouting.ShouldUseOutcomeScreen(thread))
        {
            return null;
        }

        var backHref = ApplicationsSsrQuery.TryGetSafeApplicationsReturn(returnUrl, out var safeReturn)
            ? safeReturn
            : KbRoutes.Applications;

        var activitiesHref = ApplicationsSsrQuery.BuildUrl(
            ApplicationsSsrQuery.TabActiviteiten,
            ApplicationsSsrQuery.FilterAll,
            null,
            null);

        const string learnHref = "/carriere";
        const string assistantHref = "/candidate/hoe-werkt-lobsy";
        var reason = ResolveOutcomeReason(culture, thread);

        var yesDate = thread.AcceptedAtUtc is DateTime acceptedAt
            ? AfterMatchMailSsrTime.ToAmsterdam(acceptedAt).ToString("d MMM", Nl)
            : "13 okt";
        var interviewStart = thread.ScheduledStartUtc is DateTime sched
            ? AfterMatchMailSsrTime.ToAmsterdam(sched)
            : (DateTime?)null;
        var planDate = interviewStart?.AddDays(-7).ToString("d MMM", Nl) ?? "—";
        var interviewDate = AfterMatchTimelineLabels.InterviewStep(thread);
        var outcomeDate = AfterMatchMailSsrTime.ToAmsterdam(DateTime.UtcNow).ToString("d MMM", Nl);

        return new PageData(
            thread,
            backHref,
            activitiesHref,
            KbRoutes.Map,
            learnHref,
            assistantHref,
            reason,
            yesDate,
            planDate,
            interviewDate,
            outcomeDate,
            ResolveSimilarJobs(culture, thread));
    }

    private static string ResolveOutcomeReason(CultureState culture, AfterMatchThreadDto thread)
    {
        if (!string.IsNullOrWhiteSpace(thread.OutcomeReason))
        {
            return thread.OutcomeReason.Trim();
        }

        return thread.Outcome switch
        {
            AfterMatchOutcomeKind.Offer => culture["AfterMatch.Screen13.FallbackReason"],
            AfterMatchOutcomeKind.NextRound => culture["AfterMatch.Screen13.FallbackReason"],
            _ => culture["AfterMatch.Screen13.FallbackReason"]
        };
    }

    internal static PageData BuildFallback(
        CultureState culture,
        Guid applicationId,
        string? returnUrl)
    {
        var backHref = ApplicationsSsrQuery.TryGetSafeApplicationsReturn(returnUrl, out var safeReturn)
            ? safeReturn
            : KbRoutes.Applications;

        var activitiesHref = ApplicationsSsrQuery.BuildUrl(
            ApplicationsSsrQuery.TabActiviteiten,
            ApplicationsSsrQuery.FilterAll,
            null,
            null);

        const string learnHref = "/carriere";
        const string assistantHref = "/candidate/hoe-werkt-lobsy";
        var placeholder = new AfterMatchThreadDto(
            applicationId,
            Guid.Empty,
            InterviewStatus.Done,
            AfterMatchContactShareState.From(InterviewStatus.Done, AfterMatchOutcomeKind.NotNow),
            AfterMatchOutcomeKind.NotNow,
            culture["AfterMatch.Screen13.FallbackVacancyTitle"],
            culture["AfterMatch.Screen13.FallbackCompany"],
            null,
            InterviewFormat.OnSite,
            null,
            null,
            null,
            null,
            null,
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "",
            null,
            null,
            null);

        return new PageData(
            placeholder,
            backHref,
            activitiesHref,
            KbRoutes.Map,
            learnHref,
            assistantHref,
            culture["AfterMatch.Screen13.FallbackReason"],
            "—",
            "—",
            "—",
            AfterMatchMailSsrTime.ToAmsterdam(DateTime.UtcNow).ToString("d MMM", Nl),
            ResolveSimilarJobs(culture, placeholder));
    }

    private static IReadOnlyList<SimilarJobRow> ResolveSimilarJobs(CultureState culture, AfterMatchThreadDto thread)
    {
        if (thread.SimilarVacancies.Count > 0)
        {
            return thread.SimilarVacancies
                .Select(v => new SimilarJobRow(
                    v.VacancyId,
                    CompanyInitials(v.CompanyName),
                    v.Title,
                    JoinSubtitle(v.CompanyName, v.TravelLabel),
                    null))
                .ToList();
        }

        return
        [
            new SimilarJobRow(
                null,
                culture["AfterMatch.Screen13.DemoJob1Initials"],
                culture["AfterMatch.Screen13.DemoJob1Title"],
                culture["AfterMatch.Screen13.DemoJob1Sub"],
                culture["AfterMatch.Screen13.DemoJob1Hint"]),
            new SimilarJobRow(
                null,
                culture["AfterMatch.Screen13.DemoJob2Initials"],
                culture["AfterMatch.Screen13.DemoJob2Title"],
                culture["AfterMatch.Screen13.DemoJob2Sub"],
                culture["AfterMatch.Screen13.DemoJob2Hint"]),
        ];
    }

    private static string JoinSubtitle(string company, string? travel)
    {
        if (string.IsNullOrWhiteSpace(travel))
        {
            return company;
        }

        return $"{company} · {travel}";
    }

    private static string CompanyInitials(string company)
    {
        var parts = company.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
            : company.Length >= 2 ? company[..2].ToUpperInvariant() : company.ToUpperInvariant();
    }
}
