using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Web.Discovery;

internal static class DiscoverySsrOverviewSummaries
{
    public static string StepOverviewSummary(
        int step,
        CultureState culture,
        OnboardingProfileDraft draft,
        CandidateDiscoveryOverviewPanelDto? panel)
    {
        var text = step switch
        {
            1 => OverviewAboutSummary(culture, draft),
            2 => OverviewWhenSummary(draft),
            3 => OverviewWorkSummary(culture, draft),
            4 => OverviewLearnSummary(culture, draft),
            5 => OverviewLikesSummary(draft),
            6 => OverviewDislikesSummary(draft),
            7 => TestPanelStatus(culture, OnboardingWizardCatalog.OnboardingTestKind.Competency, panel),
            8 => TestPanelStatus(culture, OnboardingWizardCatalog.OnboardingTestKind.Career, panel),
            9 => TestPanelStatus(culture, OnboardingWizardCatalog.OnboardingTestKind.Culture, panel),
            10 => TestPanelStatus(culture, OnboardingWizardCatalog.OnboardingTestKind.Values, panel),
            _ => ""
        };
        return string.IsNullOrWhiteSpace(text) ? culture["Discovery.Overview.SummaryEmpty"] : text;
    }

    public static string TestPanelStatus(
        CultureState culture,
        OnboardingWizardCatalog.OnboardingTestKind kind,
        CandidateDiscoveryOverviewPanelDto? panel)
    {
        var answered = DiscoverySsrJourneyState.AnsweredCount(kind, panel);
        var mini = OnboardingWizardCatalog.MiniLevelCount;
        var full = OnboardingWizardCatalog.FullLevelCount(kind);
        if (answered >= full)
        {
            return culture["Discovery.Overview.Done"];
        }

        if (answered >= mini)
        {
            return culture.Format("Discovery.Overview.TestAnsweredCount", answered, full);
        }

        return culture["Discovery.Overview.TestTodo"];
    }

    public static string OverviewAboutSummary(CultureState culture, OnboardingProfileDraft draft)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(draft.Phone))
        {
            parts.Add(draft.Phone.Trim());
        }

        if (!string.IsNullOrWhiteSpace(draft.City))
        {
            parts.Add(draft.City);
        }
        else if (!string.IsNullOrWhiteSpace(draft.Postcode))
        {
            parts.Add(draft.Postcode);
        }

        if (draft.Immediate)
        {
            parts.Add(culture["Discovery.Overview.Done"]);
        }

        return string.Join(", ", parts);
    }

    public static string OverviewWhenSummary(OnboardingProfileDraft draft)
    {
        var parts = new List<string>();
        if (draft.Presets.Count > 0)
        {
            parts.Add(string.Join(", ", draft.Presets.Take(2)));
        }

        if (!draft.Immediate && draft.AvailableFrom is { } from)
        {
            parts.Add(from.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.GetCultureInfo("nl-NL")));
        }

        if (draft.MaxTravel > 0)
        {
            parts.Add($"{draft.MaxTravel} min");
        }

        return string.Join(", ", parts);
    }

    public static string OverviewWorkSummary(CultureState culture, OnboardingProfileDraft draft)
        => string.Join(", ", new[] { RolesSummary(culture, draft), draft.Dream }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public static string OverviewLearnSummary(CultureState culture, OnboardingProfileDraft draft)
        => string.Join(", ", new[] { draft.Education, CertificatesSummary(draft), LanguagesSummary(culture, draft) }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    public static string OverviewLikesSummary(OnboardingProfileDraft draft)
        => string.Join(", ", draft.Hobbies.Take(3).Concat(draft.HobbyFreeText.Take(1)));

    public static string OverviewDislikesSummary(OnboardingProfileDraft draft)
        => string.Join(", ", draft.Dislikes.Take(3).Concat(draft.CustomDislikes.Where(s => !string.IsNullOrWhiteSpace(s)).Take(1)));

    private static string RolesSummary(CultureState culture, OnboardingProfileDraft draft)
        => draft.Roles.Count == 0
            ? ""
            : string.Join(", ", draft.Roles.Select(r => UiLabels.WorkType(culture, r)).Take(3));

    private static string CertificatesSummary(OnboardingProfileDraft draft)
    {
        var names = draft.Certificates.Where(c => !string.IsNullOrWhiteSpace(c.Name)).Select(c => c.Name!).Take(2).ToList();
        return names.Count == 0 ? "" : string.Join(", ", names);
    }

    private static string LanguagesSummary(CultureState culture, OnboardingProfileDraft draft)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(draft.DutchLevel))
        {
            parts.Add(culture[DiscoveryCatalogs.DutchLabelKey(draft.DutchLevel)]);
        }

        parts.AddRange(draft.SpokenLanguages.Select(l => l.Code).Take(3));
        return string.Join(", ", parts);
    }

    public static string OverviewRouteTestTitleKey(OnboardingWizardCatalog.OnboardingTestKind kind) => kind switch
    {
        OnboardingWizardCatalog.OnboardingTestKind.Competency => "Discovery.Test.Competency.Title",
        OnboardingWizardCatalog.OnboardingTestKind.Career => "Discovery.Test.Career.Title",
        OnboardingWizardCatalog.OnboardingTestKind.Culture => "Discovery.Test.Culture.Title",
        _ => "Discovery.Test.Values.Title"
    };
}
