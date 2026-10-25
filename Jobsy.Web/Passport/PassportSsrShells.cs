using System.Net;
using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Dna;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Navigation;
using Jobsy.Web.Onboarding;

namespace Jobsy.Web.Passport;

internal static class PassportSsrShells
{
    internal static string BuildBody(
        CultureState culture,
        PassportSsrData.PageData data,
        string antiforgeryToken,
        string? openDataSection = null)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"passport-page-wrap\">");
        KreeftHeaderStripHtml.Append(sb, KreeftHeaderStripAssets.LichtPaspoortDesk, KreeftHeaderStripAssets.LichtPaspoortMob, "licht-paspoort");
        sb.Append("<div class=\"panel-page passport-page passport-page--illustrated\" data-testid=\"passport-page-ssr\">");
        sb.Append(BuildGreeting(culture, data.FirstName));
        sb.Append("<div class=\"passport-layout\">");
        AppendSidebarCard(sb, culture, data, compact: false);
        sb.Append("<div class=\"passport-layout__main passport-layout__main--reiskaart\">");
        sb.Append("<div class=\"passport-layout__reiskaart\">");
        KreeftReiskaartHtml.Append(sb, culture, KreeftReiskaartCatalog.ForScreen(16));
        sb.Append("</div><div class=\"passport-layout__main-flow\">");
        AppendSidebarCard(sb, culture, data, compact: true);
        AppendOverview(sb, culture, data);
        AppendTabs(sb, culture, data);
        AppendActivePanel(sb, culture, data, antiforgeryToken, openDataSection);
        AppendFooterNav(sb, culture);
        sb.Append("</div></div></div></div></div></div>");
        AppendBanenkaartPrefetchBoot(sb, data);
        return sb.ToString();
    }

    private static void AppendBanenkaartPrefetchBoot(StringBuilder sb, PassportSsrData.PageData data)
    {
        if (data.Profile.HomeLatitude is not double lat || data.Profile.HomeLongitude is not double lng)
        {
            return;
        }

        var transport = string.IsNullOrWhiteSpace(data.Profile.Preferences.PreferredTransport)
            ? "Fiets"
            : data.Profile.Preferences.PreferredTransport.Trim();
        var maxMinutes = data.Profile.Preferences.MaxTravelMinutes is int m and > 0 ? m : 20;
        sb.Append("<script type=\"application/json\" id=\"jobsy-bk-prefetch\">{\"lat\":")
            .Append(lat.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Append(",\"lng\":")
            .Append(lng.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Append(",\"transport\":\"")
            .Append(WebUtility.HtmlEncode(transport))
            .Append("\",\"maxMinutes\":")
            .Append(maxMinutes)
            .Append("}</script>");
    }

    private static string BuildGreeting(CultureState culture, string? firstName)
    {
        var hour = DateTime.Now.Hour;
        string greeting;
        if (string.IsNullOrWhiteSpace(firstName))
        {
            greeting = hour switch
            {
                < 12 => culture["Kompas.Greeting.PlainMorning"],
                < 18 => culture["Kompas.Greeting.PlainAfternoon"],
                _ => culture["Kompas.Greeting.PlainEvening"],
            };
        }
        else
        {
            var key = hour switch
            {
                < 12 => "Kompas.Greeting.Morning",
                < 18 => "Kompas.Greeting.Afternoon",
                _ => "Kompas.Greeting.Evening",
            };
            greeting = culture.Format(key, firstName.Trim());
        }

        var sb = new StringBuilder();
        sb.Append("<header class=\"passport-ssr-greeting\" data-testid=\"passport-greeting\"><h2 class=\"passport-ssr-greeting__title\">");
        sb.Append(WebUtility.HtmlEncode(greeting));
        sb.Append("</h2><p class=\"passport-ssr-greeting__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Greeting.Lead"]));
        sb.Append("</p></header>");
        return sb.ToString();
    }

    private static void AppendFooterNav(StringBuilder sb, CultureState culture)
    {
        sb.Append("<nav class=\"passport-ssr-footer-nav\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Title"]));
        sb.Append("\" data-testid=\"passport-footer-nav\"><a href=\"");
        sb.Append(OnboardingRoutes.KompasHubPath);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Title"]));
        sb.Append("</a><a href=\"/carriere\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.CareerPath"]));
        sb.Append("</a><a href=\"/candidate/applications\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.Applications"]));
        sb.Append("</a></nav>");
    }

    private static void AppendSidebarCard(StringBuilder sb, CultureState culture, PassportSsrData.PageData data, bool compact)
    {
        var card = data.Card;
        var wrapperClass = compact
            ? "passport-layout__card passport-layout__card--mobile"
            : "passport-layout__sidebar passport-layout__card passport-layout__card--desktop";
        sb.Append("<div class=\"");
        sb.Append(wrapperClass);
        sb.Append("\"><aside class=\"passport-card");
        if (compact)
        {
            sb.Append(" passport-card--compact");
        }

        sb.Append("\" aria-labelledby=\"passport-card-name\">");
        sb.Append("<div class=\"passport-card__banner\"><div class=\"passport-card__banner-text\">");
        sb.Append("<p class=\"passport-card__banner-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Banner"]));
        sb.Append("</p><p class=\"passport-card__member\">");
        sb.Append(WebUtility.HtmlEncode(card.MemberNumber));
        sb.Append("</p></div></div>");
        sb.Append("<div class=\"passport-card__identity\"><div class=\"passport-card__avatar\" aria-hidden=\"true\">");
        sb.Append(WebUtility.HtmlEncode(card.Initials));
        sb.Append("</div><div class=\"passport-card__identity-main\"><h2 id=\"passport-card-name\" class=\"passport-card__name\">");
        sb.Append(WebUtility.HtmlEncode(card.DisplayName));
        sb.Append("</h2>");
        if (compact && !string.IsNullOrWhiteSpace(card.OneLiner))
        {
            sb.Append("<p class=\"passport-card__one-liner\">");
            sb.Append(WebUtility.HtmlEncode(card.OneLiner));
            sb.Append("</p>");
        }

        if (card.ShowOpenForWork)
        {
            sb.Append("<span class=\"status-pill status-pill--success\">");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.OpenForWork"]));
            sb.Append("</span>");
        }

        sb.Append("</div></div>");
        sb.Append("<div class=\"passport-card__layers\" role=\"progressbar\" aria-valuemin=\"0\" aria-valuemax=\"100\" aria-valuenow=\"");
        sb.Append(card.CompletenessPercent);
        sb.Append("\"><p class=\"passport-card__layers-text\">");
        sb.Append(WebUtility.HtmlEncode(culture.Format("Passport.Layers", Math.Min(4, card.CompletenessPercent / 25), 4)));
        sb.Append("</p></div>");
        sb.Append("<div class=\"passport-card__actions");
        if (compact)
        {
            sb.Append(" passport-card__actions--compact");
        }

        sb.Append("\"><a class=\"btn btn-secondary passport-card__pdf-btn\" href=\"/api/me/paspoort.pdf\" data-testid=\"");
        sb.Append(compact ? "passport-download-pdf-mobile" : "passport-download-pdf");
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(data.Flags.PassportPdfV2Enabled
            ? culture["Passport.DownloadDna"]
            : culture["Passport.LobsyCv"]));
        sb.Append("</a><a class=\"btn btn-primary\" href=\"");
        sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Data));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Edit"]));
        sb.Append("</a></div></aside></div>");
    }

    private static void AppendOverview(StringBuilder sb, CultureState culture, PassportSsrData.PageData data)
    {
        var o = data.Overview;
        sb.Append("<section class=\"passport-overview\" aria-labelledby=\"passport-overview-title\">");
        sb.Append("<h2 id=\"passport-overview-title\" class=\"passport-overview__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Overview.Title"]));
        sb.Append("</h2><p class=\"passport-overview__stage\">");
        sb.Append(WebUtility.HtmlEncode(o.CompletedTests == 0
            ? culture["Passport.Overview.Egg"]
            : culture.Format("Passport.Overview.Stage", o.CompletedTests)));
        sb.Append("</p><div class=\"passport-overview__stats\">");
        AppendStat(sb, culture, "Passport.Stat.Strongest", o.Strongest);
        AppendStat(sb, culture, "Passport.Stat.Work", o.WorkFit);
        AppendStat(sb, culture, "Passport.Stat.Home", o.CultureHome);
        AppendStat(sb, culture, "Passport.Stat.Important", o.Important);
        sb.Append("</div></section>");
    }

    private static void AppendStat(StringBuilder sb, CultureState culture, string labelKey, string? value)
    {
        sb.Append("<article class=\"passport-stat\"><p class=\"passport-stat__label\">");
        sb.Append(WebUtility.HtmlEncode(culture[labelKey]));
        sb.Append("</p>");
        if (string.IsNullOrWhiteSpace(value))
        {
            sb.Append("<p class=\"passport-stat__value\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Stat.NotYet"]));
            sb.Append("</p><a class=\"passport-stat__link\" href=\"");
            sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Tests));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Stat.DoTest"]));
            sb.Append("</a>");
        }
        else
        {
            sb.Append("<p class=\"passport-stat__value\">");
            sb.Append(WebUtility.HtmlEncode(value));
            sb.Append("</p>");
        }

        sb.Append("</article>");
    }

    private static void AppendTabs(StringBuilder sb, CultureState culture, PassportSsrData.PageData data)
    {
        var tabs = PassportTabs.Visible(
            data.Flags.EmployersEnabled,
            data.Flags.FutureJobsForYouEnabled,
            data.Flags.ReferenceVerificationEnabled);
        sb.Append("<div class=\"passport-tabs-shell\"><div class=\"passport-tabs\" role=\"tablist\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Title"]));
        sb.Append("\">");
        foreach (var tab in tabs)
        {
            var selected = string.Equals(tab, data.ActiveTab, StringComparison.OrdinalIgnoreCase);
            sb.Append("<a id=\"passport-tab-");
            sb.Append(tab);
            sb.Append("\" class=\"passport-tab");
            if (selected)
            {
                sb.Append(" passport-tab--active");
            }

            sb.Append("\" role=\"tab\" href=\"");
            sb.Append(PassportRedirects.BuildPassportUrl(tab));
            sb.Append("\" data-enhance-nav=\"false\" aria-selected=\"");
            sb.Append(selected ? "true" : "false");
            sb.Append("\"><span>");
            sb.Append(WebUtility.HtmlEncode(TabLabel(culture, tab)));
            sb.Append("</span>");
            if (tab == PassportTabs.Tests && data.TestsNextStepCount > 0)
            {
                sb.Append("<span class=\"passport-tabs__pill\">");
                sb.Append(data.TestsNextStepCount);
                sb.Append("</span>");
            }

            sb.Append("</a>");
        }

        sb.Append("</div></div>");
    }

    private static void AppendActivePanel(
        StringBuilder sb,
        CultureState culture,
        PassportSsrData.PageData data,
        string antiforgeryToken,
        string? openDataSection)
    {
        sb.Append("<div class=\"passport-panels\"><article class=\"passport-panel\" role=\"tabpanel\">");
        if (!string.IsNullOrWhiteSpace(data.ErrorMessage))
        {
            sb.Append("<p class=\"state-message error\" role=\"alert\">");
            sb.Append(WebUtility.HtmlEncode(data.ErrorMessage));
            sb.Append("</p>");
        }
        else if (data.Saved)
        {
            sb.Append("<p class=\"state-message\" role=\"status\">");
            sb.Append(WebUtility.HtmlEncode(culture["Common.Saved"]));
            sb.Append("</p>");
        }

        switch (data.ActiveTab)
        {
            case PassportTabs.Data:
                AppendDataPanel(sb, culture, data, antiforgeryToken, openDataSection);
                break;
            case PassportTabs.Tests:
                AppendTestsPanel(sb, culture, data);
                break;
            case PassportTabs.Fit:
                AppendFitPanel(sb, culture, data);
                break;
            case PassportTabs.Career:
                AppendCareerPanel(sb, culture, data);
                break;
            case PassportTabs.Proof:
                AppendProofPanel(sb, culture);
                break;
            case PassportTabs.References:
                AppendReferencesPanel(sb, culture);
                break;
            default:
                AppendDnaPanel(sb, culture, data);
                break;
        }

        sb.Append("</article></div>");
    }

    private static void AppendDnaPanel(StringBuilder sb, CultureState culture, PassportSsrData.PageData data)
    {
        sb.Append("<p class=\"login-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Bubble.DnaEmpty"]));
        sb.Append("</p><p><a href=\"");
        sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Tests));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Tab.Tests"]));
        sb.Append("</a></p>");
        if (data.Card.Keywords.Count > 0)
        {
            sb.Append("<ul class=\"passport-dna-keywords\">");
            foreach (var kw in data.Card.Keywords)
            {
                sb.Append("<li>");
                sb.Append(WebUtility.HtmlEncode(kw));
                sb.Append("</li>");
            }

            sb.Append("</ul>");
        }
    }

    private static void AppendTestsPanel(StringBuilder sb, CultureState culture, PassportSsrData.PageData data)
    {
        if (data.FromDiscovery)
        {
            sb.Append("<p class=\"passport-ssr-back\"><a class=\"passport-ssr-back-link btn btn-secondary\" href=\"/candidate/ontdekkingsreis?stap=overview\" data-enhance-nav=\"false\" data-testid=\"passport-back-to-discovery\"><span class=\"passport-ssr-back-link__icon\" aria-hidden=\"true\">←</span>");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.Passport.BackToJourney"]));
            sb.Append("</a></p>");
        }

        sb.Append("<p class=\"login-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Bubble.Tests"]));
        sb.Append("</p><ul class=\"passport-ssr-links\">");
        foreach (var (label, href) in TestLinks(culture))
        {
            sb.Append("<li><a href=\"");
            sb.Append(WebUtility.HtmlEncode(href));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(label));
            sb.Append("</a></li>");
        }

        sb.Append("</ul>");
    }

    private static IEnumerable<(string Label, string Href)> TestLinks(CultureState culture)
    {
        yield return (culture["Test.Competence.Title"], "/profiel/tests/competence");
        yield return (culture["Test.Career.Title"], "/profiel/tests/career");
        yield return (culture["Test.Culture.Title"], "/profiel/tests/culture");
        yield return (culture["Test.Values.Title"], "/profiel/tests/values");
    }

    private static void AppendFitPanel(StringBuilder sb, CultureState culture, PassportSsrData.PageData data)
    {
        if (!data.Flags.EmployersEnabled && !data.Flags.FutureJobsForYouEnabled)
        {
            sb.Append("<p class=\"login-lead\" data-testid=\"passport-fit-soon\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Soon"]));
            sb.Append("</p>");
            return;
        }

        sb.Append("<p class=\"login-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Bubble.Fit"]));
        sb.Append("</p><p><a href=\"/candidate/vacancies\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.Search"]));
        sb.Append("</a></p>");
    }

    private static void AppendCareerPanel(StringBuilder sb, CultureState culture, PassportSsrData.PageData data)
    {
        sb.Append("<p class=\"login-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Bubble.Career"]));
        sb.Append("</p>");
        if (!string.IsNullOrWhiteSpace(data.DreamTitle))
        {
            sb.Append("<p><strong>");
            sb.Append(WebUtility.HtmlEncode(data.DreamTitle));
            sb.Append("</strong></p>");
        }

        sb.Append("<p><a href=\"/carriere\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Tab.Career"]));
        sb.Append("</a></p>");
    }

    private static void AppendProofPanel(StringBuilder sb, CultureState culture)
    {
        sb.Append("<p class=\"login-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Bubble.Proof"]));
        sb.Append("</p><p><a href=\"");
        sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Proof));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Tab.Proof"]));
        sb.Append("</a></p>");
    }

    private static void AppendReferencesPanel(StringBuilder sb, CultureState culture)
    {
        sb.Append("<p class=\"login-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Bubble.References"]));
        sb.Append("</p>");
    }

    private static void AppendDataPanel(
        StringBuilder sb,
        CultureState culture,
        PassportSsrData.PageData data,
        string antiforgeryToken,
        string? openDataSection)
    {
        var profile = data.Profile;
        var prefs = profile.Preferences ?? new Jobsy.Web.Models.CandidatePreferences();
        var openAvailability = string.Equals(openDataSection, "availability", StringComparison.OrdinalIgnoreCase);
        sb.Append("<div class=\"passport-data\"><p class=\"passport-data__intro\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Bubble.Data"]));
        sb.Append("</p><form method=\"post\" action=\"/api/passport-client/save-data\" data-enhance=\"false\" data-enhance-nav=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" /><input type=\"hidden\" name=\"returnTab\" value=\"data\" />");
        sb.Append("<div class=\"profile-form competency-profile-card\"><div class=\"passport-data__body\">");
        sb.Append("<label><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.FirstName"]));
        sb.Append("</span><input name=\"firstName\" type=\"text\" autocomplete=\"given-name\" value=\"");
        sb.Append(WebUtility.HtmlEncode(profile.FirstName ?? ""));
        sb.Append("\" data-testid=\"passport-data-first-name\" data-passport-static-field=\"1\" autocomplete=\"given-name\" /></label>");
        sb.Append("<label><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.LastName"]));
        sb.Append("</span><input name=\"lastName\" type=\"text\" autocomplete=\"family-name\" value=\"");
        sb.Append(WebUtility.HtmlEncode(profile.LastName ?? ""));
        sb.Append("\" data-testid=\"passport-data-last-name\" data-passport-static-field=\"1\" autocomplete=\"family-name\" /></label>");
        sb.Append("<label><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Phone"]));
        sb.Append("</span><input name=\"phoneNumber\" type=\"tel\" autocomplete=\"tel\" value=\"");
        sb.Append(WebUtility.HtmlEncode(profile.PhoneNumber ?? ""));
        sb.Append("\" data-testid=\"passport-data-phone\" data-passport-static-field=\"1\" autocomplete=\"tel\" inputmode=\"tel\" /></label>");
        sb.Append("<label><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.AboutMe"]));
        sb.Append("</span><textarea name=\"aboutMe\" rows=\"4\" data-testid=\"passport-data-about\">");
        sb.Append(WebUtility.HtmlEncode(profile.Preferences?.AboutMe ?? ""));
        sb.Append("</textarea></label></div>");
        sb.Append("<div class=\"profile-save-bar\"><button type=\"submit\" class=\"btn btn-primary login-submit\" data-testid=\"passport-data-save\">");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Save"]));
        sb.Append("</button></div></div></form>");

        sb.Append("<div class=\"detail-card passport-data__item");
        if (openAvailability)
        {
            sb.Append(" is-open");
        }

        sb.Append("\" id=\"passport-data-availability\" data-testid=\"passport-data-availability\"><h3 class=\"passport-data__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Section.Availability"]));
        sb.Append("</h3><p class=\"passport-data__sub\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Data.AvailabilitySub"]));
        sb.Append("</p><form method=\"post\" action=\"/api/passport-client/save-availability\" data-enhance=\"false\" data-enhance-nav=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" /><input type=\"hidden\" name=\"returnTab\" value=\"data\" />");
        sb.Append("<input type=\"hidden\" name=\"openSection\" value=\"availability\" />");
        sb.Append("<fieldset class=\"profile-availability\"><legend class=\"visually-hidden\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Availability"]));
        sb.Append("</legend><label class=\"detail-card passport-data__switch\"><span class=\"passport-data__switch-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.PresetFlexible"]));
        sb.Append("</span><input type=\"checkbox\" name=\"flexibleTimes\" value=\"true\"");
        if (prefs.FlexibleTimes == true)
        {
            sb.Append(" checked");
        }

        sb.Append(" data-testid=\"passport-data-flexible\" /></label></fieldset>");
        sb.Append("<div class=\"profile-save-bar\"><button type=\"submit\" name=\"availabilityPreset\" value=\"immediate\" class=\"btn btn-secondary\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.PresetImmediate"]));
        sb.Append("</button><button type=\"submit\" class=\"btn btn-primary\" data-testid=\"passport-data-availability-save\">");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Save"]));
        sb.Append("</button></div></form></div>");

        sb.Append("<p class=\"passport-ssr-nav-links\"><a href=\"");
        sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Proof));
        sb.Append("&item=experience\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apply.Experience"]));
        sb.Append("</a> · <a href=\"");
        sb.Append(OnboardingRoutes.KompasHubPath);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Title"]));
        sb.Append("</a> · <a href=\"/carriere\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.CareerPath"]));
        sb.Append("</a> · <a href=\"/candidate/applications\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.Applications"]));
        sb.Append("</a></p></div>");
    }

    private static string TabLabel(CultureState culture, string tab)
        => culture[tab switch
        {
            PassportTabs.Tests => "Passport.Tab.Tests",
            PassportTabs.Fit => "Passport.Tab.Fit.Compact",
            PassportTabs.Career => "Passport.Tab.Career",
            PassportTabs.Proof => "Passport.Tab.Proof",
            PassportTabs.References => "Passport.Tab.References",
            PassportTabs.Data => "Passport.Tab.Data.Compact",
            _ => "Passport.Tab.Dna",
        }];
}
