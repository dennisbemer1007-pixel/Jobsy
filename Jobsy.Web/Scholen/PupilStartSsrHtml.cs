using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Scholen;

public static class PupilStartSsrHtml
{
    public static string BuildBody(
        PupilQuestionSetDef? def,
        CultureState culture,
        bool groep78,
        string? themeKey,
        IPupilQuestionSetRegistry _)
    {
        if (groep78 && def is not null)
        {
            return PupilGroep78RouteSsrHtml.BuildBody(def, culture, themeKey);
        }

        var sb = new StringBuilder();
        sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Coach.Start"]));
        sb.Append("""<div class="ll-start"><section class="ll-card">""");
        sb.Append("<h1>");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Start.Title"]));
        sb.Append("</h1>");
        sb.Append(PupilLeerlingSsrParts.SpeakButton(
            SpokenText.Join(
                culture["Leerling.Start.Title"],
                culture["Leerling.Start.Lead"],
                culture[def?.StartTimeKey ?? "Leerling.Start.NoteTime"],
                culture["Leerling.Start.NoteNoWrong"],
                culture["Leerling.Start.NotePause"]),
            "ll-start", culture));
        sb.Append("<p class=\"muted\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Start.Lead"]));
        sb.Append("</p><ol class=\"ll-worlds\">");
        foreach (var plan in def?.Worlds ?? Array.Empty<PupilWorldPlan>())
        {
            var w = PupilWorldCatalog.Find(plan.WorldKey);
            if (w is null) continue;
            var n = PupilWorldCatalog.TestWorldNumber(w.Key);
            sb.Append("<li><span class=\"ll-worlds__n\">").Append(n).Append("</span><div><strong>");
            sb.Append(PupilLeerlingSsrParts.E(culture[w.TitleKey])).Append("</strong><small>");
            sb.Append(PupilLeerlingSsrParts.E(culture[w.SubtitleKey])).Append("</small></div></li>");
        }

        sb.Append("</ol><ul class=\"ll-start__notes\"><li>");
        sb.Append(PupilLeerlingSsrParts.E(culture[def?.StartTimeKey ?? "Leerling.Start.NoteTime"]));
        sb.Append("</li><li>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Start.NoteNoWrong"]));
        sb.Append("</li><li>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Start.NotePause"]));
        sb.Append("</li></ul><a class=\"btn-primary\" href=\"/leerling/reis\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Start.Begin"]));
        sb.Append("</a></section></div>");
        return sb.ToString();
    }
}
