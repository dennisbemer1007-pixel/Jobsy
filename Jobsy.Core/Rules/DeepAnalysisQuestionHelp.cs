namespace Jobsy.Core.Rules;

/// <summary>
/// Concrete praktijkvoorbeelden per diepte-analysevraag (Jip-en-Janneke, geen vaktermen).
/// </summary>
public static class DeepAnalysisQuestionHelp
{
    public static string ExampleFor(DeepAnalysisQuestion question)
        => AssessmentItemExamples.ExampleFor(question);

    public static string DomainLabel(string domain) => DimensionLabels.For(domain);
}
