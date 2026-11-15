using System.Net;
using System.Text;

namespace Jobsy.Core.Scholen.MboHbo;

public static class PupilMboHboPassportSectionHtml
{
    public static string Build(PupilMboHboBlock block, PupilMboHboScoring.BlockOutcome outcome)
        => block switch
        {
            PupilMboHboBlock.Interesse => BarsBody(outcome, maxBars: 3),
            PupilMboHboBlock.Competentie => PillsBody(outcome),
            PupilMboHboBlock.Vakken => PillsBody(outcome, footnote: "Op basis van hoe leuk je de vakken vindt en hoe goed je erin denkt te zijn."),
            PupilMboHboBlock.Beroepen => RankedBody(outcome),
            PupilMboHboBlock.Werkwaarden => PillsBody(outcome, footnote: "Dit zegt iets over de werkplek waarop jij je prettig kunt voelen."),
            _ => SummaryBody(outcome),
        };

    public static string TodoPlaceholder(PupilMboHboBlock block)
    {
        var title = PupilMboHboMockupCopy.ShortBlockTitle(block);
        var key = PupilMboHboProgressState.BlockKey(block);
        return $"""
            <div class="mhbo-pass-todo">
            <p class="mhbo-pass-todo__label">Nog te doen</p>
            <p class="mhbo-pass-todo__text">{E(PupilMboHboCopy.BlockMeta(block).Description)}</p>
            <a class="mhbo-btn mhbo-btn--ghost mhbo-pass-todo__btn" href="/leerling/mbo-hbo/test/{E(key)}">Start {E(title)}</a>
            </div>
            """;
    }

    public static string VolgendeStapCard()
        => $"<p>{E(PupilMboHboMockupCopy.PassportVolgendeStapBody)}</p>";

    private static string BarsBody(PupilMboHboScoring.BlockOutcome outcome, int maxBars)
    {
        var sb = new StringBuilder("<div class=\"mhbo-pass-bars\">");
        foreach (var bar in outcome.Bars.Take(maxBars))
        {
            var pct = Math.Clamp(bar.Percent, 0, 100);
            sb.Append("<div class=\"mhbo-pass-bar-row\"><span class=\"mhbo-pass-bar-label\">")
                .Append(E(bar.Label))
                .Append("</span><span class=\"mhbo-pass-bar-track\" role=\"presentation\"><i style=\"width:")
                .Append(pct.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append("%\"></i></span><span class=\"mhbo-pass-bar-val\">")
                .Append(pct.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append("</span></div>");
        }

        sb.Append("</div><p class=\"mhbo-pass-footnote\">").Append(E(outcome.Summary)).Append("</p>");
        return sb.ToString();
    }

    private static string PillsBody(PupilMboHboScoring.BlockOutcome outcome, string? footnote = null)
    {
        var sb = new StringBuilder();
        if (outcome.Highlights.Count > 0)
        {
            sb.Append("<ul class=\"mhbo-pass-pills\">");
            foreach (var h in outcome.Highlights.Take(6))
            {
                sb.Append("<li>").Append(E(h)).Append("</li>");
            }

            sb.Append("</ul>");
        }

        var note = footnote ?? outcome.Summary;
        if (!string.IsNullOrWhiteSpace(note))
        {
            sb.Append("<p class=\"mhbo-pass-footnote\">").Append(E(note)).Append("</p>");
        }

        return sb.ToString();
    }

    private static string RankedBody(PupilMboHboScoring.BlockOutcome outcome)
    {
        var items = outcome.Highlights.Count > 0
            ? outcome.Highlights
            : outcome.Bars.Select(b => b.Label).ToList();
        var sb = new StringBuilder("<ol class=\"mhbo-pass-rank\">");
        var n = 1;
        foreach (var item in items.Take(3))
        {
            sb.Append("<li><span class=\"mhbo-pass-rank__no\">")
                .Append(n.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append("</span><span>").Append(E(item)).Append("</span></li>");
            n++;
        }

        sb.Append("</ol><p class=\"mhbo-pass-footnote\">");
        sb.Append(E("Dit zijn ideeën, geen keuze die je nu al moet maken."));
        sb.Append("</p>");
        return sb.ToString();
    }

    private static string SummaryBody(PupilMboHboScoring.BlockOutcome outcome)
        => $"<p>{E(outcome.Summary)}</p>";

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
}