using System.Text;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilGroep78ResultSceneSsrHtml
{
    public static string Render(
        CultureState culture,
        IPupilQuestionSetRegistry registry,
        string? themeKey,
        PupilGroep78JourneySnapshot snapshot)
    {
        if (!snapshot.ShowStrip || string.IsNullOrWhiteSpace(snapshot.NodeKey))
        {
            return "";
        }

        var (bandStem, bandPos) = PupilGroep78JourneySnapshot.SceneBand(themeKey, snapshot);
        var sb = new StringBuilder();
        sb.Append("""<section class="ll-g78-scene ll-g78-result-scene" data-testid="leerling-g78-scene">""");
        sb.Append("<section class=\"band ll-g78-world-band ll-g78-result-band\" style=\"background-image:url('/leerling/werelden/img/");
        sb.Append(bandStem).Append(".jpg');background-position:").Append(bandPos).Append("\" aria-hidden=\"true\"></section>");
        sb.Append("""<div class="ll-g78-scene__journey ll-g78-result-band__journey">""");
        sb.Append(PupilLeerlingSsrParts.Groep78JourneyStrip(
            culture,
            registry,
            themeKey,
            snapshot.NodeKey!,
            snapshot.MetaLine,
            0,
            0,
            sceneMetaIsHtml: snapshot.SceneMetaIsHtml));
        sb.Append("</div></section>");
        return sb.ToString();
    }
}
