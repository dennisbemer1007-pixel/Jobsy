using System.Net;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Help;

/// <summary>
/// Renders one <see cref="HowLobsyRoleGuides.Step"/> body with its deep links inlined.
/// Shared by the signed-in panel and the public <c>/hoe-werkt-lobsy</c> role tab.
/// </summary>
public static class HowLobsyGuideBody
{
    public static MarkupString Render(HowLobsyRoleGuides.Step step, CultureState culture)
    {
        if (step.Links.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(culture[step.BodyKey]));
        }

        var args = new object[step.Links.Length];
        for (var i = 0; i < step.Links.Length; i++)
        {
            var link = step.Links[i];
            var label = WebUtility.HtmlEncode(culture[link.LabelKey]);
            // Candidate step 4 uses Apply.Title as emphasis, not a real href.
            args[i] = link.Href is "#" or ""
                ? $"<strong>{label}</strong>"
                : $"<a href=\"{WebUtility.HtmlEncode(link.Href)}\">{label}</a>";
        }

        return new MarkupString(culture.Format(step.BodyKey, args));
    }
}
