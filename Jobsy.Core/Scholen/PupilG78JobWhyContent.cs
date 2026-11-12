using System.Text;

namespace Jobsy.Core.Scholen;

public sealed record PupilG78JobCompetencyLine(
    string JobAsksLabel,
    string? PupilReflection)
{
    public string FormattedBullet => string.IsNullOrWhiteSpace(PupilReflection)
        ? JobAsksLabel
        : $"{JobAsksLabel} — {PupilReflection}";
}

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
            sb.Append("<section class=\"g78-job-why-panel__section\">");
            sb.Append("<h4 class=\"g78-job-why-panel__section-title\">")
                .Append(HeadingWork)
                .Append("</h4><div class=\"g78-job-why-panel__section-body\">");
            foreach (var line in WorkDescriptionSentences)
            {
                sb.Append("<p>")
                    .Append(System.Net.WebUtility.HtmlEncode(line))
                    .Append("</p>");
            }

            sb.Append("</div></section>");
        }

        if (Competencies.Count > 0)
        {
            sb.Append("<section class=\"g78-job-why-panel__section\">");
            sb.Append("<h4 class=\"g78-job-why-panel__section-title\">")
                .Append(HeadingCompetencies)
                .Append("</h4><ul class=\"g78-job-why-panel__comp-list\">");
            foreach (var comp in Competencies)
            {
                sb.Append("<li class=\"g78-job-why-panel__comp-row\">")
                    .Append(System.Net.WebUtility.HtmlEncode(comp.FormattedBullet))
                    .Append("</li>");
            }

            sb.Append("</ul></section>");
        }

        if (AnswerSentences.Count > 0)
        {
            sb.Append("<section class=\"g78-job-why-panel__section g78-job-why-panel__section--last\">");
            sb.Append("<h4 class=\"g78-job-why-panel__section-title\">")
                .Append(HeadingAnswers)
                .Append("</h4><div class=\"g78-job-why-panel__section-body\">");
            foreach (var line in AnswerSentences)
            {
                sb.Append("<p>")
                    .Append(System.Net.WebUtility.HtmlEncode(line))
                    .Append("</p>");
            }

            sb.Append("</div></section>");
        }

        return sb.ToString();
    }

    public string FormatCompactPlain() => FormatPdfPlain();

    public string FormatPdfPlain()
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
                sb.Append("• ").Append(c.FormattedBullet).Append('\n');
            }
        }

        AppendPlainSection(sb, HeadingAnswers, AnswerSentences);
        return sb.ToString().Trim();
    }

    public string FormatFullPlain() => FormatPdfPlain();

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
