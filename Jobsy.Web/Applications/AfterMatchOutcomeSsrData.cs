using System.Globalization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
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
        if (thread is null || thread.Outcome != AfterMatchOutcomeKind.NotNow)
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

        const string candidateHow = "/candidate/hoe-werkt-lobsy";
        var reason = string.IsNullOrWhiteSpace(thread.OutcomeReason)
            ? culture["AfterMatch.Screen13.DemoReason"]
            : thread.OutcomeReason.Trim();

        var yesDate = thread.AcceptedAtUtc?.ToLocalTime().ToString("d MMM", Nl) ?? "13 okt";
        var interviewStart = thread.ScheduledStartUtc?.ToLocalTime();
        var planDate = interviewStart?.AddDays(-7).ToString("d MMM", Nl) ?? "15 okt";
        var interviewDate = interviewStart?.ToString("d MMM", Nl) ?? "22 okt";
        var outcomeDate = DateTime.UtcNow.ToLocalTime().ToString("d MMM", Nl);

        return new PageData(
            thread,
            backHref,
            activitiesHref,
            KbRoutes.Map,
            candidateHow,
            candidateHow,
            reason,
            yesDate,
            planDate,
            interviewDate,
            outcomeDate,
            ResolveSimilarJobs(culture, thread));
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
