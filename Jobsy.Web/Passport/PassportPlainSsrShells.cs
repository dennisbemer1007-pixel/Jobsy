using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Passport;

/// <summary>Plain paspoort SSR — markup/classes from mockup #878 src/20-paspoort.html.</summary>
internal static class PassportPlainSsrShells
{
    internal static string BuildBody(CultureState culture, PassportSsrData.PageData data)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"passport-page-wrap cp-plain-app cp-plain878\">");
        sb.Append("<main class=\"wrap\">");
        AppendSteps(sb, culture);
        sb.Append("<div class=\"stepl\" style=\"font-size:17px;color:#111827;margin:-14px 0 22px\"><b>");
        sb.Append(WebUtility.HtmlEncode(culture.Format("PlainPublic.Passport.StepLineBold", 5, 5)));
        sb.Append("</b> · ");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Landing.Step5Title"]));
        sb.Append("</div>");
        AppendMobileGuide(sb, culture);
        sb.Append("<div class=\"grid\">");
        AppendMainCard(sb, culture, data);
        AppendSideColumn(sb, culture);
        sb.Append("</div></main>");
        AppendFooter(sb, culture);
        sb.Append("</div>");
        AppendBanenkaartPrefetchBoot(sb, data);
        return sb.ToString();
    }

    private static void AppendSteps(StringBuilder sb, CultureState culture)
    {
        sb.Append("<ol class=\"step\">");
        var steps = new[]
        {
            culture["PlainPublic.Landing.Step1Title"],
            culture["PlainPublic.Landing.Step2Title"],
            culture["PlainPublic.Landing.Step3Title"],
            culture["PlainPublic.Landing.Step4Title"],
            culture["PlainPublic.Landing.Step5Title"],
        };
        for (var i = 0; i < steps.Length; i++)
        {
            var last = i == steps.Length - 1;
            sb.Append("<li class=\"").Append(last ? "cur" : "done").Append("\" style=\"--p:100%\"><i></i><span>");
            sb.Append(WebUtility.HtmlEncode($"{i + 1}. {steps[i]}"));
            sb.Append("</span></li>");
        }

        sb.Append("</ol>");
    }

    private static void AppendMobileGuide(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"mgw\"><details class=\"mg\"><summary><span>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.GuideTitle"]));
        sb.Append("</span><i aria-hidden=\"true\">⌄</i></summary><p>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.GuideBody"]));
        sb.Append("</p></details></div>");
    }

    private static void AppendMainCard(StringBuilder sb, CultureState culture, PassportSsrData.PageData data)
    {
        var card = data.Card;
        sb.Append("<section class=\"card\"><p class=\"eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.Eyebrow"]));
        sb.Append("</p><h1>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.Title"]));
        sb.Append("</h1><p class=\"lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.Lead"]));
        sb.Append("</p><div style=\"border:1px solid #E5E7EB;border-radius:10px;padding:22px\">");
        sb.Append("<div style=\"display:flex;gap:14px;align-items:center;margin-bottom:18px\"><div class=\"av\" style=\"width:48px;height:48px;font-size:16px\">");
        sb.Append(WebUtility.HtmlEncode(card.Initials));
        sb.Append("</div><div><b style=\"font-size:18px\">");
        sb.Append(WebUtility.HtmlEncode(card.DisplayName));
        sb.Append("</b>");
        if (!string.IsNullOrWhiteSpace(card.OneLiner))
        {
            sb.Append("<div class=\"note\">");
            sb.Append(WebUtility.HtmlEncode(card.OneLiner));
            sb.Append("</div>");
        }

        sb.Append("</div></div>");
        sb.Append("<dl class=\"kv\">");
        AppendKv(sb, culture["PlainPublic.Passport.KvWork"], FormatWork(card));
        AppendKv(sb, culture["PlainPublic.Passport.KvEducation"], card.LicenseLabel);
        AppendKv(sb, culture["PlainPublic.Passport.KvDream"], data.DreamTitle);
        AppendKv(sb, culture["PlainPublic.Passport.KvAvailable"], card.AvailableLabel);
        sb.Append("</dl>");
        AppendTagQuadrants(sb, culture, data);
        if (data.DnaPanel.StoryGeneratedAt is DateTime updated)
        {
            var cultureInfo = CultureInfo.GetCultureInfo(string.Equals(culture.Language, "en", StringComparison.OrdinalIgnoreCase) ? "en-GB" : "nl-NL");
            sb.Append("<p class=\"note\" style=\"margin:16px 0 0\">");
            sb.Append(WebUtility.HtmlEncode(culture.Format("PlainPublic.Passport.Updated", updated.ToString("d MMMM yyyy", cultureInfo))));
            sb.Append("</p>");
        }

        sb.Append("</div><div class=\"row\"><a class=\"btn2\" href=\"");
        sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Data));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Edit"]));
        sb.Append("</a><span></span><a class=\"btn\" href=\"/api/me/paspoort.pdf\" data-testid=\"passport-download-pdf\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(data.Flags.PassportPdfV2Enabled
            ? culture["Passport.DownloadDna"]
            : culture["Passport.LobsyCv"]));
        sb.Append("</a></div></section>");
    }

    private static void AppendSideColumn(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div style=\"display:grid;gap:20px;align-content:start\">");
        sb.Append("<aside class=\"side guide\"><div class=\"gh\"><div><div class=\"gt\">");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.GuideTitle"]));
        sb.Append("</div><div class=\"gs\">");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.GuideSubtitle"]));
        sb.Append("</div></div></div><p>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.GuideBody"]));
        sb.Append("</p></aside><aside class=\"side\"><h4>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.VisibilityTitle"]));
        sb.Append("</h4><div class=\"opts\" style=\"grid-template-columns:1fr\"><div class=\"opt on\"><span class=\"bx\"></span><div><b>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.VisibilityEmployers"]));
        sb.Append("</b><small>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.VisibilityEmployersHint"]));
        sb.Append("</small></div></div><div class=\"opt on\"><span class=\"bx\"></span><div><b>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.VisibilityName"]));
        sb.Append("</b><small>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.VisibilityNameHint"]));
        sb.Append("</small></div></div></div></aside></div>");
    }

    private static void AppendTagQuadrants(StringBuilder sb, CultureState culture, PassportSsrData.PageData data)
    {
        var o = data.Overview;
        sb.Append("<div class=\"k4\" style=\"margin-top:18px\">");
        AppendTagBlock(sb, culture["Passport.Stat.Strongest"], o.Strongest.Value, o.Strongest.Status);
        AppendTagBlock(sb, culture["Passport.Stat.Work"], o.WorkFit.Value, o.WorkFit.Status);
        AppendTagBlock(sb, culture["Passport.Stat.Home"], o.CultureHome.Value, o.CultureHome.Status);
        AppendTagBlock(sb, culture["Passport.Stat.Important"], o.Important.Value, o.Important.Status);
        sb.Append("</div>");
    }

    private static void AppendTagBlock(StringBuilder sb, string label, string? value, string? status)
    {
        sb.Append("<div><div class=\"n note\" style=\"font-weight:600\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</div><div class=\"tags\">");
        if (!string.IsNullOrWhiteSpace(value))
        {
            foreach (var part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append("<span class=\"tag\">");
                sb.Append(WebUtility.HtmlEncode(part));
                sb.Append("</span>");
            }
        }
        else if (!string.IsNullOrWhiteSpace(status))
        {
            sb.Append("<span class=\"tag t\">");
            sb.Append(WebUtility.HtmlEncode(status));
            sb.Append("</span>");
        }

        sb.Append("</div></div>");
    }

    private static void AppendKv(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        sb.Append("<dt>");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</dt><dd>");
        sb.Append(WebUtility.HtmlEncode(value));
        sb.Append("</dd>");
    }

    private static string? FormatWork(PassportSsrDisplay.CardModel card)
    {
        if (!string.IsNullOrWhiteSpace(card.AboutMe))
        {
            var trimmed = card.AboutMe.Trim();
            return trimmed.Length > 120 ? trimmed[..117] + "..." : trimmed;
        }

        if (card.LookingFor.Count > 0)
        {
            return string.Join(", ", card.LookingFor.Take(3));
        }

        return null;
    }

    private static void AppendFooter(StringBuilder sb, CultureState culture)
    {
        sb.Append("<footer class=\"foot\"><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Privacy"]));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Terms"]));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Footer.Accessibility"]));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Footer.Contact"]));
        sb.Append("</span><span>© ");
        sb.Append(DateTime.UtcNow.Year);
        sb.Append(" Lobsy</span></footer>");
    }

    private static void AppendBanenkaartPrefetchBoot(StringBuilder sb, PassportSsrData.PageData data)
    {
        if (data.Profile.HomeLatitude is not double lat || data.Profile.HomeLongitude is not double lng)
        {
            return;
        }

        var transport = string.IsNullOrWhiteSpace(data.Profile.Preferences?.PreferredTransport)
            ? "Fiets"
            : data.Profile.Preferences.PreferredTransport.Trim();
        var maxMinutes = data.Profile.Preferences?.MaxTravelMinutes is int m and > 0 ? m : 20;
        sb.Append("<script type=\"application/json\" id=\"jobsy-bk-prefetch\">{\"lat\":")
            .Append(lat.ToString(CultureInfo.InvariantCulture))
            .Append(",\"lng\":")
            .Append(lng.ToString(CultureInfo.InvariantCulture))
            .Append(",\"transport\":\"")
            .Append(WebUtility.HtmlEncode(transport))
            .Append("\",\"maxMinutes\":")
            .Append(maxMinutes)
            .Append("}</script>");
    }
}
