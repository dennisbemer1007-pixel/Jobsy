using System.Net;
using System.Text;
using Jobsy.Core.Careers;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Scholen;

public static class PupilOccupationDaySsrHtml
{
    public static string Build(
        CultureState culture,
        string? title,
        OccupationDayResponse? day,
        bool unknown,
        bool error)
    {
        var sb = new StringBuilder();
        sb.Append("""<div class="ll-reis day-life" data-testid="leerling-occupation-day">""");
        sb.Append("""<section class="day-life__card ll-card" aria-labelledby="day-title">""");
        sb.Append("""<h1 id="day-title">""");
        sb.Append(PupilLeerlingSsrParts.E(
            string.IsNullOrWhiteSpace(title) ? culture["Day.Eyebrow"] : culture.Format("Day.Title", title)));
        sb.Append("</h1><p class=\"day-life__disclaimer\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["Day.Disclaimer"]));
        sb.Append("</p>");

        if (error)
        {
            sb.Append("<p class=\"state-message\">").Append(PupilLeerlingSsrParts.E(culture["Common.Error"])).Append("</p>");
        }
        else if (unknown)
        {
            sb.Append("<p class=\"state-message\">").Append(PupilLeerlingSsrParts.E(culture["Day.Unknown"])).Append("</p>");
        }
        else if (day is { Enabled: false })
        {
            sb.Append("<p class=\"state-message\">").Append(PupilLeerlingSsrParts.E(culture["Day.Off"])).Append("</p>");
        }
        else if (day is not { Found: true })
        {
            sb.Append("<p class=\"state-message\">").Append(PupilLeerlingSsrParts.E(culture["Day.Empty"])).Append("</p>");
        }
        else
        {
            OccupationDaySsrTimeline.Append(sb, culture, day);
        }

        sb.Append("""<p class="day-life__back"><a class="btn btn-secondary" href="/leerling/paspoort">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingPassport.Page.Back"]));
        sb.Append("</a></p></section></div>");
        return sb.ToString();
    }
}

internal static class OccupationDaySsrTimeline
{
    internal static void Append(StringBuilder sb, CultureState culture, OccupationDayResponse day)
    {
        if (day.ThinSource)
        {
            sb.Append("<p class=\"day-life__lead\">").Append(WebUtility.HtmlEncode(culture["Day.Thin"])).Append("</p>");
        }

        var blocks = day.Blocks is { Count: > 0 }
            ? day.Blocks
            : BuildLegacy(day, culture);
        sb.Append("<ol class=\"day-life__timeline");
        if (blocks.Count >= 9)
        {
            sb.Append(" day-life__timeline--long");
        }

        sb.Append("\">");
        foreach (var block in blocks)
        {
            sb.Append("<li class=\"day-life__block\">");
            if (TrySplitClockLabel(block.Label, out var clock, out var moment))
            {
                sb.Append("<h2 class=\"day-life__chip\"><span class=\"day-life__time\">")
                    .Append(WebUtility.HtmlEncode(clock))
                    .Append("</span><span class=\"day-life__moment\">")
                    .Append(WebUtility.HtmlEncode(moment))
                    .Append("</span></h2>");
            }
            else
            {
                sb.Append("<h2 class=\"day-life__chip\">").Append(WebUtility.HtmlEncode(block.Label)).Append("</h2>");
            }

            sb.Append("<p>").Append(WebUtility.HtmlEncode(block.Text)).Append("</p></li>");
        }

        sb.Append("</ol>");
        if ((day.Tasks ?? []).Count > 0)
        {
            sb.Append("""<section class="day-life__section"><h2>""").Append(WebUtility.HtmlEncode(culture["Day.Tasks"]));
            sb.Append("""</h2><ul class="day-life__cloud">""");
            foreach (var task in day.Tasks!)
            {
                sb.Append("<li class=\"day-life__pill\">").Append(WebUtility.HtmlEncode(task)).Append("</li>");
            }

            sb.Append("</ul></section>");
        }
    }

    private static List<OccupationDayBlock> BuildLegacy(OccupationDayResponse day, CultureState culture)
    {
        var rows = new List<OccupationDayBlock>();
        Add("morning", culture["Day.Morning"], day.Morning);
        Add("midday", culture["Day.Midday"], day.Midday);
        Add("afternoon", culture["Day.Afternoon"], day.Afternoon);
        Add("close", culture["Day.Closing"], day.Closing);
        return rows;

        void Add(string key, string label, string? text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                rows.Add(new OccupationDayBlock(key, label, text));
            }
        }
    }

    private static bool TrySplitClockLabel(string? label, out string clock, out string moment)
    {
        clock = "";
        moment = "";
        var text = (label ?? "").Trim();
        var space = text.IndexOf(' ');
        if (space <= 0 || space >= text.Length - 1)
        {
            return false;
        }

        var head = text[..space];
        if (!OccupationDayTimeline.TryParseLabelMinutes(head, out _))
        {
            return false;
        }

        clock = head;
        moment = text[(space + 1)..].Trim();
        return moment.Length > 0;
    }
}
