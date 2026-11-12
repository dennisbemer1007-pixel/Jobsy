using System.Text;

namespace Jobsy.Core.Scholen;

public sealed record PupilG78JobCompetencyLine(
    string JobAsksLabel,
    string? PupilReflection);

/// <summary>Structured G78 job "why" panel (web + PDF).</summary>
public sealed record PupilG78JobWhyContent(
    IReadOnlyList<string> WorkDescriptionSentences,
    IReadOnlyList<PupilG78JobCompetencyLine> Competencies,
    IReadOnlyList<string> AnswerSentences)
{
    public const string HeadingWork = "Wat doe je in dit werk?";
    public const string HeadingCompetencies = "Wat vraagt dit werk?";
    public const string HeadingAnswers = "Wat zeggen jouw antwoorden?";

    public bool HasContent =>
        WorkDescriptionSentences.Count > 0
        || Competencies.Count > 0
        || AnswerSentences.Count > 0;

    public string FormatHtml()
    {
        if (!HasContent)
        {
            return "";
        }

        var sb = new StringBuilder();
        if (WorkDescriptionSentences.Count > 0)
        {
            sb.Append("<p class=\"g78-job-why-panel__section-title\"><b>")
                .Append(HeadingWork)
                .Append("</b></p>");
            foreach (var line in WorkDescriptionSentences)
            {
                sb.Append("<p class=\"g78-job-why-panel__section-body\">")
                    .Append(System.Net.WebUtility.HtmlEncode(line))
                    .Append("</p>");
            }
        }

        if (Competencies.Count > 0)
        {
            sb.Append("<p class=\"g78-job-why-panel__section-title\"><b>")
                .Append(HeadingCompetencies)
                .Append("</b></p>");
            foreach (var comp in Competencies)
            {
                sb.Append("<p class=\"g78-job-why-panel__section-body\">• ")
                    .Append(System.Net.WebUtility.HtmlEncode(comp.JobAsksLabel));
                if (!string.IsNullOrWhiteSpace(comp.PupilReflection))
                {
                    sb.Append(' ')
                        .Append(System.Net.WebUtility.HtmlEncode(comp.PupilReflection));
                }

                sb.Append("</p>");
            }
        }

        if (AnswerSentences.Count > 0)
        {
            sb.Append("<p class=\"g78-job-why-panel__section-title\"><b>")
                .Append(HeadingAnswers)
                .Append("</b></p>");
            foreach (var line in AnswerSentences)
            {
                sb.Append("<p class=\"g78-job-why-panel__section-body\">")
                    .Append(System.Net.WebUtility.HtmlEncode(line))
                    .Append("</p>");
            }
        }

        return sb.ToString();
    }

    public string FormatCompactPlain()
    {
        if (!HasContent)
        {
            return "";
        }

        var parts = new List<string>();
        if (WorkDescriptionSentences.Count > 0)
        {
            parts.Add(string.Join(' ', WorkDescriptionSentences));
        }

        if (Competencies.Count > 0)
        {
            var comp = string.Join(' ', Competencies.Select(c =>
                c.JobAsksLabel
                + (string.IsNullOrWhiteSpace(c.PupilReflection) ? "" : " " + c.PupilReflection)));
            parts.Add(comp);
        }

        if (AnswerSentences.Count > 0)
        {
            parts.Add(string.Join(' ', AnswerSentences));
        }

        return string.Join(' ', parts);
    }

    public string FormatFullPlain()
    {
        if (!HasContent)
        {
            return "";
        }

        var sb = new StringBuilder();
        AppendPlainSection(sb, HeadingWork, WorkDescriptionSentences);
        if (Competencies.Count > 0)
        {
            sb.Append(HeadingCompetencies).Append('\n');
            foreach (var c in Competencies)
            {
                sb.Append("• ").Append(c.JobAsksLabel).Append('\n');
                if (!string.IsNullOrWhiteSpace(c.PupilReflection))
                {
                    sb.Append("  ").Append(c.PupilReflection).Append('\n');
                }
            }
        }

        AppendPlainSection(sb, HeadingAnswers, AnswerSentences);
        return sb.ToString().Trim();
    }

    private static void AppendPlainSection(StringBuilder sb, string heading, IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        sb.Append(heading).Append('\n');
        foreach (var line in lines)
        {
            sb.Append(line).Append('\n');
        }
    }
}
