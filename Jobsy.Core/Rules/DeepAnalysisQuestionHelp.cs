namespace Jobsy.Core.Rules;

/// <summary>
/// Concrete praktijkvoorbeelden per diepte-analysevraag (Jip-en-Janneke, geen vaktermen).
/// </summary>
public static class DeepAnalysisQuestionHelp
{
    public static string ExampleFor(DeepAnalysisQuestion question, string lang = "nl")
        => AssessmentItemExamples.ExampleFor(question, lang);

    public static string DomainLabel(string domain) => DimensionLabels.For(domain);
}
