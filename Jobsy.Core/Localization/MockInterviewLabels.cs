using Jobsy.Core.Localization;

namespace Jobsy.Core.Localization;

/// <summary>
/// Localized coach reply labels for the mock interview bot.
/// </summary>
public static class MockInterviewLabels
{
    public sealed record Pack(
        string LanguageCode,
        string LanguageName,
        string Caution,
        string Strong,
        string Tip,
        string Rewrite,
        string Question);

    public static Pack For(string? language)
    {
        var code = JobsyLanguages.Normalize(language);
        return code switch
        {
            "en" => new Pack("en", "English", "Note:", "Strong:", "Tip:", "Try this:", "Question:"),
            "pl" => new Pack("pl", "Polish", "Uwaga:", "Mocne:", "Wskazówka:", "Spróbuj tak:", "Pytanie:"),
            "ro" => new Pack("ro", "Romanian", "Atenție:", "Puternic:", "Sfat:", "Încearcă așa:", "Întrebare:"),
            "ar" => new Pack("ar", "Arabic", "ملاحظة:", "قوي:", "نصيحة:", "جرّب هكذا:", "سؤال:"),
            _ => new Pack("nl", "Dutch", "Let op:", "Sterk:", "Tip:", "Probeer zo:", "Vraag:")
        };
    }

    /// <summary>
    /// All known coach-block prefixes across languages (for UI parsing).
    /// </summary>
    public static IReadOnlyList<(string Prefix, string Kind, string DisplayKey)> AllBlockPrefixes { get; } =
    [
        ("Let op:", "caution", "MockInterview.LabelCaution"),
        ("Note:", "caution", "MockInterview.LabelCaution"),
        ("Uwaga:", "caution", "MockInterview.LabelCaution"),
        ("Atenție:", "caution", "MockInterview.LabelCaution"),
        ("ملاحظة:", "caution", "MockInterview.LabelCaution"),
        ("Sterk:", "strong", "MockInterview.LabelStrong"),
        ("Strong:", "strong", "MockInterview.LabelStrong"),
        ("Mocne:", "strong", "MockInterview.LabelStrong"),
        ("Puternic:", "strong", "MockInterview.LabelStrong"),
        ("قوي:", "strong", "MockInterview.LabelStrong"),
        ("Tip:", "tip", "MockInterview.LabelTip"),
        ("Wskazówka:", "tip", "MockInterview.LabelTip"),
        ("Sfat:", "tip", "MockInterview.LabelTip"),
        ("نصيحة:", "tip", "MockInterview.LabelTip"),
        ("Probeer zo:", "rewrite", "MockInterview.LabelRewrite"),
        ("Try this:", "rewrite", "MockInterview.LabelRewrite"),
        ("Spróbuj tak:", "rewrite", "MockInterview.LabelRewrite"),
        ("Încearcă așa:", "rewrite", "MockInterview.LabelRewrite"),
        ("جرّب هكذا:", "rewrite", "MockInterview.LabelRewrite"),
        ("Vraag:", "question", "MockInterview.LabelQuestion"),
        ("Question:", "question", "MockInterview.LabelQuestion"),
        ("Pytanie:", "question", "MockInterview.LabelQuestion"),
        ("Întrebare:", "question", "MockInterview.LabelQuestion"),
        ("سؤال:", "question", "MockInterview.LabelQuestion"),
    ];
}
