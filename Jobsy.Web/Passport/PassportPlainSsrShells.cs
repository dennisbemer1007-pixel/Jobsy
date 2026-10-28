using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Passport;

/// <summary>Plain paspoort SSR (mockup #878 docs/mockups/kandidaat/plain src/20-paspoort.html).</summary>
internal static class PassportPlainSsrShells
{
    internal static string BuildBody(CultureState culture, PassportSsrData.PageData data)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"passport-page-wrap cp-plain-app\">");
        sb.Append("<div class=\"cp-plain-passport\" data-testid=\"passport-page-ssr\" data-passport-ssr=\"1\" data-plain-app=\"paspoort\">");
        AppendSteps(sb, culture);
        sb.Append("<p class=\"cp-plain-passport__step-line\"><b>");
        sb.Append(WebUtility.HtmlEncode(culture.Format("PlainPublic.Passport.StepLineBold", 5, 5)));
        sb.Append("</b> · ");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Landing.Step5Title"]));
        sb.Append("</p>");
        sb.Append("<div class=\"cp-plain-passport__grid\">");
        AppendMainCard(sb, culture, data);
        AppendSideColumn(sb, culture);
        sb.Append("</div>");
        AppendFooterNav(sb, culture);
        sb.Append("</div></div>");
        AppendBanenkaartPrefetchBoot(sb, data);
        return sb.ToString();
    }

    private static void AppendSteps(StringBuilder sb, CultureState culture)
    {
        sb.Append("<ol class=\"cp-plain-passport__steps\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Landing.HowTitle"]));
        sb.Append("\">");
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
            sb.Append("<li class=\"");
            sb.Append(last ? "cur" : "done");
            sb.Append("\" style=\"--p:100%\"><i aria-hidden=\"true\"></i><span>");
            sb.Append(WebUtility.HtmlEncode($"{i + 1}. {steps[i]}"));
            sb.Append("</span></li>");
        }

        sb.Append("</ol>");
    }

    private static void AppendMainCard(StringBuilder sb, CultureState culture, PassportSsrData.PageData data)
    {
        var card = data.Card;
        sb.Append("<section class=\"cp-plain-passport__card\">");
        sb.Append("<p class=\"cp-plain-passport__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.Eyebrow"]));
        sb.Append("</p><h1 class=\"cp-plain-passport__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.Title"]));
        sb.Append("</h1><p class=\"cp-plain-passport__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.Lead"]));
        sb.Append("</p>");
        sb.Append("<div class=\"cp-plain-passport__profile\">");
        sb.Append("<div class=\"cp-plain-passport__avatar\" aria-hidden=\"true\">");
        sb.Append(WebUtility.HtmlEncode(card.Initials));
        sb.Append("</div><div><b class=\"cp-plain-passport__name\">");
        sb.Append(WebUtility.HtmlEncode(card.DisplayName));
        sb.Append("</b>");
        if (!string.IsNullOrWhiteSpace(card.OneLiner))
        {
            sb.Append("<div class=\"cp-plain-passport__note\">");
            sb.Append(WebUtility.HtmlEncode(card.OneLiner));
            sb.Append("</div>");
        }

        if (card.ShowOpenForWork)
        {
            sb.Append("<div class=\"cp-plain-passport__pill\">");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.OpenForWork"]));
            sb.Append("</div>");
        }

        sb.Append("</div></div>");
        sb.Append("<dl class=\"cp-plain-passport__kv\">");
        AppendKv(sb, culture["PlainPublic.Passport.KvWork"], FormatWork(card));
        AppendKv(sb, culture["PlainPublic.Passport.KvEducation"], card.LicenseLabel);
        AppendKv(sb, culture["PlainPublic.Passport.KvDream"], data.DreamTitle);
        AppendKv(sb, culture["PlainPublic.Passport.KvAvailable"], card.AvailableLabel);
        sb.Append("</dl>");
        AppendTagQuadrants(sb, culture, data);
        if (data.DnaPanel.StoryGeneratedAt is DateTime updated)
        {
            sb.Append("<p class=\"cp-plain-passport__note cp-plain-passport__updated\">");
            var cultureInfo = CultureInfo.GetCultureInfo(string.Equals(culture.Language, "en", StringComparison.OrdinalIgnoreCase) ? "en-GB" : "nl-NL");
            sb.Append(WebUtility.HtmlEncode(culture.Format("PlainPublic.Passport.Updated", updated.ToString("d MMMM yyyy", cultureInfo))));
            sb.Append("</p>");
        }

        sb.Append("<div class=\"cp-plain-passport__actions\">");
        sb.Append("<a class=\"cp-plain-passport__btn2\" href=\"");
        sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Data));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Edit"]));
        sb.Append("</a><a class=\"cp-plain-passport__btn\" href=\"/api/me/paspoort.pdf\" data-testid=\"passport-download-pdf\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(data.Flags.PassportPdfV2Enabled
            ? culture["Passport.DownloadDna"]
            : culture["Passport.LobsyCv"]));
        sb.Append("</a></div></section>");
    }

    private static void AppendSideColumn(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"cp-plain-passport__aside-col\">");
        sb.Append("<aside class=\"cp-plain-passport__side\"><h2 class=\"cp-plain-passport__side-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.GuideTitle"]));
        sb.Append("</h2><p>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.GuideBody"]));
        sb.Append("</p></aside>");
        sb.Append("<aside class=\"cp-plain-passport__side\"><h2 class=\"cp-plain-passport__side-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.VisibilityTitle"]));
        sb.Append("</h2><ul class=\"cp-plain-passport__vis-list\">");
        sb.Append("<li><b>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.VisibilityEmployers"]));
        sb.Append("</b><span>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.VisibilityEmployersHint"]));
        sb.Append("</span></li><li><b>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.VisibilityName"]));
        sb.Append("</b><span>");
        sb.Append(WebUtility.HtmlEncode(culture["PlainPublic.Passport.VisibilityNameHint"]));
        sb.Append("</span></li></ul></aside></div>");
    }

    private static void AppendTagQuadrants(StringBuilder sb, CultureState culture, PassportSsrData.PageData data)
    {
        var o = data.Overview;
        sb.Append("<div class=\"cp-plain-passport__k4\">");
        AppendTagBlock(sb, culture["Passport.Stat.Strongest"], o.Strongest.Value, o.Strongest.Status);
        AppendTagBlock(sb, culture["Passport.Stat.Work"], o.WorkFit.Value, o.WorkFit.Status);
        AppendTagBlock(sb, culture["Passport.Stat.Home"], o.CultureHome.Value, o.CultureHome.Status);
        AppendTagBlock(sb, culture["Passport.Stat.Important"], o.Important.Value, o.Important.Status);
        sb.Append("</div>");
    }

    private static void AppendTagBlock(StringBuilder sb, string label, string? value, string? status)
    {
        sb.Append("<div><div class=\"cp-plain-passport__tag-label\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</div><div class=\"cp-plain-passport__tags\">");
        if (!string.IsNullOrWhiteSpace(value))
        {
            foreach (var part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append("<span class=\"cp-plain-passport__tag\">");
                sb.Append(WebUtility.HtmlEncode(part));
                sb.Append("</span>");
            }
        }
        else if (!string.IsNullOrWhiteSpace(status))
        {
            sb.Append("<span class=\"cp-plain-passport__tag cp-plain-passport__tag--muted\">");
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

    private static void AppendFooterNav(StringBuilder sb, CultureState culture)
    {
        sb.Append("<nav class=\"cp-plain-passport__foot\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Title"]));
        sb.Append("\"><a href=\"");
        sb.Append(OnboardingRoutes.KompasHubPath);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Title"]));
        sb.Append("</a><a href=\"/carriere\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.CareerPath"]));
        sb.Append("</a><a href=\"/candidate/applications\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.Applications"]));
        sb.Append("</a></nav>");
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
