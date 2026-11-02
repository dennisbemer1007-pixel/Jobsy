using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilStopSsrHtml
{
    public static string BuildBody(CultureState culture, string? done, string antiforgeryField)
    {
        var paused = string.Equals(done, "1", StringComparison.Ordinal)
            || string.Equals(done, "deel1", StringComparison.Ordinal);
        var sb = new System.Text.StringBuilder();
        sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Coach.Stop"]));
        sb.Append("""<section class="ll-card ll-stop">""");
        if (paused)
        {
            sb.Append("<h1>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.Title"])).Append("</h1>");
            var body = string.Equals(done, "deel1", StringComparison.Ordinal)
                ? culture["Leerling.Vo.Stop.Part1Body"]
                : culture["Leerling.Stop.Body"];
            sb.Append(PupilLeerlingSsrParts.SpeakButton(
                SpokenText.Join(culture["Leerling.Stop.Title"], body), "ll-stop", culture));
            sb.Append("<p>").Append(PupilLeerlingSsrParts.E(body)).Append("</p>");
            sb.Append("""<a class="btn-primary" href="/leerling">""");
            sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.Again"])).Append("</a>");
        }
        else
        {
            sb.Append("<h1>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.AskTitle"])).Append("</h1>");
            sb.Append(PupilLeerlingSsrParts.SpeakButton(
                SpokenText.Join(culture["Leerling.Stop.AskTitle"], culture["Leerling.Stop.AskBody"]),
                "ll-stop-ask", culture));
            sb.Append("<p>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.AskBody"])).Append("</p>");
            sb.Append(PupilLeerlingSsrFrame.PauseForm(antiforgeryField, culture["Leerling.Pause"]));
        }
        sb.Append("</section>");
        return sb.ToString();
    }
}
