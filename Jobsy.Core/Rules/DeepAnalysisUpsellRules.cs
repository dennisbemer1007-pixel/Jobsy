using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Candidate-facing deep-test upsell lines (API + mobile clients).</summary>
public static class DeepAnalysisUpsellRules
{
    public static string CopyNl(AssessmentKind kind, int questionCount)
        => kind switch
        {
            AssessmentKind.Career =>
                $"Ontgrendel de uitgebreide beroepentest — {questionCount} vragen",
            AssessmentKind.Competence =>
                $"Ontgrendel de uitgebreide competentietest — {questionCount} vragen",
            AssessmentKind.Culture =>
                $"Ontgrendel de uitgebreide cultuurtest — {questionCount} vragen",
            _ =>
                $"Ontgrendel de uitgebreide waardentest — {questionCount} vragen"
        };
}
