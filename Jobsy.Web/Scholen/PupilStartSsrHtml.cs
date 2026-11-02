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
        IPupilQuestionSetRegistry registry)
    {
        var sb = new StringBuilder();
        if (groep78)
        {
            sb.Append(PupilLeerlingSsrParts.Groep78SceneHeader(
                culture, registry, themeKey, "koraalrif", "koraalrif",
                culture["Leerling.Start.Title"], showJourney: true));
            sb.Append("""<div class="wrap ll-g78-overlap"><section class="card ll-start ll-start--g78">""");
        }
        else
        {
            sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Coach.Start"]));
            sb.Append("""<div class="ll-start"><section class="ll-card">""");
        }

        sb.Append("<h1");
        if (groep78) sb.Append(" class=\"serif\"");
        sb.Append('>');
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
            var titleKey = groep78
                ? PupilJourneyTheme.WorldTitleKey(themeKey, w.Key)
                : w.TitleKey;
            var subKey = groep78
                ? PupilJourneyTheme.WorldSubtitleKey(themeKey, w.Key)
                : w.SubtitleKey;
            sb.Append("<li><span class=\"ll-worlds__n\">").Append(n).Append("</span><div><strong>");
            sb.Append(PupilLeerlingSsrParts.E(culture[titleKey])).Append("</strong><small>");
            sb.Append(PupilLeerlingSsrParts.E(culture[subKey])).Append("</small></div></li>");
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
