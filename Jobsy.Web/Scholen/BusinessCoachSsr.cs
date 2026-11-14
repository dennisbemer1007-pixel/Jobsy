using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Scholen;

/// <summary>Variant-B business coach dock for SSR pages (school, teacher, werkgever shell).</summary>
public static class BusinessCoachSsr
{
    public static void AppendDock(StringBuilder sb, CultureState culture, bool hasBottomNav)
    {
        sb.Append("<div id=\"lobsy-assistant-ssr-config\" hidden data-chat-url=\"/api/assistant-client/chat\" data-business=\"1\" data-coach-avatar-url=\"")
            .Append(WebUtility.HtmlEncode(BrandImages.AbsoluteCoachBAvatar))
            .Append("\" data-coach-figure-url=\"")
            .Append(WebUtility.HtmlEncode(BrandImages.AbsoluteCoachB))
            .Append("\" data-title=\"")
            .Append(WebUtility.HtmlEncode(culture["Assistant.Title"]))
            .Append("\" data-subtitle=\"")
            .Append(WebUtility.HtmlEncode(culture["Assistant.SubtitleBusiness"]))
            .Append("\" data-greeting=\"")
            .Append(WebUtility.HtmlEncode(culture["Assistant.Greeting"]))
            .Append("\" data-close=\"")
            .Append(WebUtility.HtmlEncode(culture["Assistant.Close"]))
            .Append("\" data-send=\"")
            .Append(WebUtility.HtmlEncode(culture["Assistant.Send"]))
            .Append("\" data-placeholder=\"")
            .Append(WebUtility.HtmlEncode(culture["Assistant.Placeholder"]))
            .Append("\" data-bubble-text=\"")
            .Append(WebUtility.HtmlEncode(culture["Coach.OnboardingBubble"]))
            .Append("\" data-lang=\"")
            .Append(WebUtility.HtmlEncode(culture.Language))
            .Append("\"></div>");

        var navClass = hasBottomNav ? " lobsy-coach-dock--has-nav" : "";
        sb.Append("<div class=\"lobsy-coach-dock lobsy-coach-dock--variant-b")
            .Append(navClass)
            .Append("\" data-lobsy-coach data-ssr-coach=\"business\"><p class=\"lobsy-coach-dock__teaser\" data-ob-coach-bubble role=\"status\">")
            .Append(WebUtility.HtmlEncode(culture["Coach.OnboardingBubble"]))
            .Append("</p><button type=\"button\" id=\"lobsy-assistant-launcher-btn\" class=\"lobsy-coach-dock__btn lobsy-coach-dock__btn--lobster-b\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Coach.OpenOnboarding"]))
            .Append("\" aria-haspopup=\"dialog\" aria-expanded=\"false\" aria-controls=\"lobsy-assistant-panel\" data-ssr-coach-open><img class=\"lobsy-coach-dock__lobster\" src=\"")
            .Append(WebUtility.HtmlEncode(BrandImages.AbsoluteCoachB))
            .Append("\" alt=\"")
            .Append(WebUtility.HtmlEncode(culture["Coach.LobsterAlt"]))
            .Append("\" width=\"140\" height=\"200\" decoding=\"async\" /></button></div>");
    }
}
