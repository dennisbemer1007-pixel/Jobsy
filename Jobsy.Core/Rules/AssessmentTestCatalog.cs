using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Per-test metadata for the candidate Tests tab: copy keys, free/deep lengths,
/// and scientific model links (stored here — not hard-coded in markup).
/// </summary>
public static class AssessmentTestCatalog
{
    public static readonly AssessmentTestDefinition[] All =
    [
        new(
            AssessmentKind.Competence,
            TitleKey: "Test.Competence.Title",
            LeadKey: "Competency.Lead",
            ScienceNoteKey: "Competency.ScienceNote",
            FreeQuestionCount: 25,
            FreeMinutesApprox: 3,
            DeepQuestionCount: 150,
            ModelName: "Big Five (Mini-IPIP)",
            ModelUrl: "https://ipip.ori.org/MiniIPIPKey.htm",
            ModelSource:
            "Donnellan et al. (2006), The Mini-IPIP Scales, Psychological Assessment 18(2), https://doi.org/10.1037/1040-3590.18.2.192",
            FreeStartHref: "/candidate/competencies",
            DeepStartHref: "/candidate/deep-analysis/competence"),
        new(
            AssessmentKind.Career,
            TitleKey: "Test.Career.Title",
            LeadKey: "Career.Lead",
            ScienceNoteKey: "Career.ScienceNote",
            FreeQuestionCount: 25,
            FreeMinutesApprox: 3,
            DeepQuestionCount: 200,
            ModelName: "RIASEC (Holland)",
            ModelUrl: "https://www.onetcenter.org/IP.html",
            ModelSource:
            "Holland (1959), A theory of vocational choice, Journal of Counseling Psychology 6(1), https://doi.org/10.1037/h0040767",
            FreeStartHref: "/candidate/career",
            DeepStartHref: "/candidate/deep-analysis/career"),
        new(
            AssessmentKind.Culture,
            TitleKey: "Test.Culture.Title",
            LeadKey: "CultureScan.Lead",
            ScienceNoteKey: "CultureScan.ScienceNote",
            FreeQuestionCount: 18,
            FreeMinutesApprox: 3,
            DeepQuestionCount: 150,
            ModelName: "Organizational Culture Profile + IPIP-facetten",
            ModelUrl: "https://doi.org/10.2307/256404",
            ModelSource:
            "O'Reilly, Chatman & Caldwell (1991), Academy of Management Journal 34(3). Facets: https://ipip.ori.org/",
            FreeStartHref: "/candidate/culture",
            DeepStartHref: "/candidate/deep-analysis/culture"),
        new(
            AssessmentKind.Values,
            TitleKey: "Test.Values.Title",
            LeadKey: "ValuesScan.Lead",
            ScienceNoteKey: "ValuesScan.ScienceNote",
            FreeQuestionCount: 25,
            FreeMinutesApprox: 4,
            DeepQuestionCount: 150,
            ModelName: "Schwartz Value Model",
            ModelUrl: "https://doi.org/10.9707/2307-0919.1116",
            ModelSource:
            "Schwartz (2012), An overview of the Schwartz theory of basic values, Online Readings in Psychology and Culture 2(1)",
            FreeStartHref: "/candidate/values",
            DeepStartHref: "/candidate/deep-analysis/values")
    ];

    public static AssessmentTestDefinition? TryGet(AssessmentKind kind)
        => All.FirstOrDefault(t => t.Kind == kind);

    public static AssessmentTestDefinition? TryGetByKey(string? key)
    {
        if (!AssessmentKindLabels.TryParse(key, out var kind))
        {
            return null;
        }

        return TryGet(kind);
    }

    public static string DetailHref(AssessmentKind kind)
        => $"/profiel/tests/{AssessmentKindLabels.ToSlug(kind)}";
}

public sealed record AssessmentTestDefinition(
    AssessmentKind Kind,
    string TitleKey,
    string LeadKey,
    string ScienceNoteKey,
    int FreeQuestionCount,
    int FreeMinutesApprox,
    int DeepQuestionCount,
    string ModelName,
    string ModelUrl,
    string ModelSource,
    string FreeStartHref,
    string DeepStartHref)
{
    public string Key => AssessmentKindLabels.ToSlug(Kind);
    public string DetailHref => AssessmentTestCatalog.DetailHref(Kind);
}
