using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Onboarding;

namespace Jobsy.Web.Media;

internal static class KreeftReiskaartHtml
{
    internal static void Append(StringBuilder sb, CultureState culture, KreeftReiskaartCatalog.ScreenState state)
    {
        var shellLine = ShellLine(culture, state);
        sb.Append("<aside class=\"kreeft-reiskaart-wrap ob-rustig__reiskaart\" data-testid=\"kreeft-reiskaart\" data-reiskaart-screen=\"");
        sb.Append(state.ScreenNumber);
        sb.Append("\"><div class=\"kreeft-reiskaart reis\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Reiskaart.Aria"]));
        sb.Append("\"><div class=\"kop\"><b>");
        sb.Append(WebUtility.HtmlEncode(culture[state.KopKey]));
        sb.Append("</b><span>");
        sb.Append(WebUtility.HtmlEncode(culture[state.SubKey]));
        sb.Append("</span></div><div class=\"route\" aria-hidden=\"true\">");
        for (var zi = 0; zi < KreeftReiskaartCatalog.ZoneNameKeys.Length; zi++)
        {
            var zoneClass = zi < state.ZoneIndex ? "done" : zi == state.ZoneIndex ? "nu" : "";
            sb.Append("<div class=\"");
            sb.Append(zoneClass);
            sb.Append("\"><i></i>");
            sb.Append(WebUtility.HtmlEncode(culture[KreeftReiskaartCatalog.ZoneNameKeys[zi]]));
            sb.Append("</div>");
        }

        sb.Append("</div><div class=\"lagen");
        if (state.NewShell)
        {
            sb.Append(" nieuw");
        }

        sb.Append("\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(shellLine));
        sb.Append("\">");
        for (var i = 0; i < 10; i++)
        {
            var released = !state.NewShell && i < state.Shed;
            sb.Append("<span");
            if (released)
            {
                sb.Append(" class=\"los\"");
            }

            sb.Append("></span>");
        }

        sb.Append("</div><p>");
        sb.Append(WebUtility.HtmlEncode(shellLine));
        sb.Append("</p>");
        if (!string.IsNullOrEmpty(state.TipKey))
        {
            sb.Append("<p class=\"tip\">");
            sb.Append(WebUtility.HtmlEncode(culture[state.TipKey!]));
            sb.Append("</p>");
        }

        sb.Append("</div></aside>");
    }

    internal static string ShellLine(CultureState culture, KreeftReiskaartCatalog.ScreenState state)
    {
        if (state.NewShell)
        {
            return culture["Reiskaart.Shell.AllReleased"];
        }

        if (state.MidDepthPause)
        {
            return culture.Format("Reiskaart.Shell.MidDepth", 10 - state.Shed, state.Shed);
        }

        if (state.Shed == 0 && state.ZoneIndex == 0)
        {
            return culture["Reiskaart.Shell.Intro"];
        }

        var left = 10 - state.Shed;
        var layerToRelease = state.Shed + 1;
        return left == 1
            ? culture.Format("Reiskaart.Shell.AfterStepOneLeft", layerToRelease)
            : culture.Format("Reiskaart.Shell.AfterStepManyLeft", left, layerToRelease);
    }
}
