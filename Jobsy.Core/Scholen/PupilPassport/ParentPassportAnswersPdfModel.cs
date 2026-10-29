using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Core.Scholen.PupilPassport;

public sealed record ParentPassportAnswersPdfItem(string Question, string Answer);

public sealed record ParentPassportAnswersPdfSection(
    string Title,
    string Subtitle,
    IReadOnlyList<ParentPassportAnswersPdfItem> Items);

public sealed record ParentPassportAnswersPdfModel(
    bool Enabled,
    string Language,
    string HeaderTitle,
    string Intro,
    string AnswerLabel,
    string Footer,
    string ClassLabel,
    string SchoolName,
    string DisplayCode,
    DateTime Date,
    IReadOnlyList<ParentPassportAnswersPdfSection> Sections);

public static class ParentPassportAnswersPdfModelBuilder
{
    public static ParentPassportAnswersPdfModel Build(
        PupilQuestionSetDef def,
        string? answersJson,
        string? language,
        string classLabel,
        string schoolName,
        string displayCode,
        DateTime dateUtc)
    {
        var lang = string.IsNullOrWhiteSpace(language) ? "nl" : language.Trim();
        var keys = ParentPassportAnswersBuilder.Build(def, answersJson);
        if (!keys.Enabled)
        {
            return new ParentPassportAnswersPdfModel(
                false,
                lang,
                "",
                "",
                "",
                "",
                classLabel,
                schoolName,
                displayCode,
                dateUtc,
                []);
        }

        var sections = keys.Sections.Select(s => new ParentPassportAnswersPdfSection(
            PupilLeerlingScholenLocalized.Get(s.TitleKey, lang),
            PupilLeerlingScholenLocalized.Get(s.SubtitleKey, lang),
            s.Items.Select(i => new ParentPassportAnswersPdfItem(
                PupilLeerlingScholenLocalized.Get(i.QuestionTextKey, lang),
                PupilLeerlingScholenLocalized.Get(i.AnswerLabelKey, lang))).ToList())).ToList();

        return new ParentPassportAnswersPdfModel(
            true,
            lang,
            ParentPassportAnswersPdfCopy.Get("Ouder.AnswersPdf.Header", lang),
            ParentPassportAnswersPdfCopy.Get("Ouder.AnswersPdf.Intro", lang),
            ParentPassportAnswersPdfCopy.Get("Ouder.AnswersPdf.AnswerLabel", lang),
            ParentPassportAnswersPdfCopy.Get("Ouder.AnswersPdf.Footer", lang),
            classLabel,
            schoolName,
            displayCode,
            dateUtc,
            sections);
    }
}
