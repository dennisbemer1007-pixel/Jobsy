using System.Net;
using System.Text;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Media;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingKompasSsrShells
{
    internal sealed record ReadyModel(
        string FirstName,
        OnboardingImpression? Impression,
        int MatchCount,
        int MaxTravelMinutes,
        string? HomeLabel,
        OnboardingFinaleCardsInput CardsInput,
        string HubUrl,
        string FullTestsUrl);

    internal sealed record HubModel(
        string FirstName,
        OnboardingImpression? Impression,
        int MatchCount,
        int MaxTravelMinutes,
        string? HomeLabel,
        OnboardingFinaleCardsInput CardsInput,
        bool PassportEnabled,
        string ProfileUrl,
        string PassportUrl);

    public static string BuildReadyBody(CultureState culture, ReadyModel model)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-rustig__canvas ob-rustig__kompas ob-rustig__kompas--ready\">");
        sb.Append("<div class=\"ob-rustig__kompas-finale\" data-testid=\"onboarding-rustig-kompas\">");
        sb.Append("<section class=\"ob-finale ob-finale--no-hero ob-finale--kompas-ready\" data-testid=\"onboarding-finale-v2\">");
        sb.Append("<div class=\"ob-finale__wrap\">");
        AppendKompasContent(sb, culture, model.Impression, model.MatchCount, model.MaxTravelMinutes, model.HomeLabel, model.CardsInput, includeFullTestsSection: false);
        AppendPrimaryCtas(sb, culture, model.HubUrl, model.FullTestsUrl, readyScreen: true);
        sb.Append("</div></section></div></div>");
        return sb.ToString();
    }

    public static string BuildHubBody(CultureState culture, HubModel model)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"kompas-page-wrap\">");
        KreeftHeaderStripHtml.Append(sb, KreeftHeaderStripAssets.LichtKompasDesk, KreeftHeaderStripAssets.LichtKompasMob, "licht-kompas");
        sb.Append("<section class=\"kompas kompas--ssr\" aria-labelledby=\"kompas-title\" data-testid=\"candidate-kompas-ssr\">");
        sb.Append("<header class=\"kompas__intro\">");
        sb.Append("<h2 id=\"kompas-title\" class=\"kompas__heading\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Title"]));
        sb.Append("</h2>");
        sb.Append("<p class=\"ob-wizard__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Ssr.Lead"]));
        sb.Append("</p></header>");

        sb.Append("<nav class=\"kompas-tabs admin-sublinks\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.TabsLabel"]));
        sb.Append("\">");
        var hubPath = OnboardingRoutes.KompasHubPath;
        var profileTabUrl = model.PassportEnabled ? model.PassportUrl : model.ProfileUrl;
        AppendTabLink(sb, culture["Kompas.TabDna"], hubPath + "#kompas-tests", samePageAnchor: true);
        AppendTabLink(sb, culture["Kompas.TabProfile"], profileTabUrl);
        AppendTabLink(sb, culture["Kompas.TabTests"], hubPath + "#kompas-full-tests", samePageAnchor: true);
        AppendTabLink(sb, culture["Kompas.TabFit"], hubPath + "#kompas-directions", samePageAnchor: true);
        sb.Append("</nav>");

        sb.Append("<div class=\"kompas-hub-layout kompas-hub-layout--reiskaart\">");
        sb.Append("<div class=\"kompas-hub-layout__reiskaart\">");
        KreeftReiskaartHtml.Append(sb, culture, KreeftReiskaartCatalog.ForScreen(15));
        sb.Append("</div><div class=\"kompas-hub-layout__main\">");
        sb.Append("<div class=\"ob-rustig__canvas ob-rustig__kompas kompas--ssr-body\">");
        sb.Append("<section class=\"ob-finale ob-finale--no-hero ob-finale--hub\" data-testid=\"onboarding-finale-v2\">");
        sb.Append("<div class=\"ob-finale__wrap\">");
        AppendKompasContent(sb, culture, model.Impression, model.MatchCount, model.MaxTravelMinutes, model.HomeLabel, model.CardsInput, includeFullTestsSection: true);
        sb.Append("</div></section></div>");
        sb.Append("</div></div>");

        sb.Append("<div class=\"kompas__ssr-actions\">");
        var openProfileUrl = model.PassportEnabled ? model.PassportUrl : model.ProfileUrl;
        sb.Append("<a class=\"ob-rustig__btn ob-rustig__btn--primary\" href=\"");
        sb.Append(WebUtility.HtmlEncode(openProfileUrl));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Ssr.OpenProfile"]));
        sb.Append("</a>");

        if (model.PassportEnabled)
        {
            sb.Append("<a class=\"ob-rustig__btn\" href=\"");
            sb.Append(WebUtility.HtmlEncode(model.PassportUrl));
            sb.Append("\" data-testid=\"kompas-go-passport\">");
            sb.Append(WebUtility.HtmlEncode(culture["Onboarding.FinaleV2.Do.Passport.Title"]));
            sb.Append("</a>");
        }

        sb.Append("</div></section></div>");
        return sb.ToString();
    }

    private static void AppendKompasContent(
        StringBuilder sb,
        CultureState culture,
        OnboardingImpression? impression,
        int matchCount,
        int maxTravelMinutes,
        string? homeLabel,
        OnboardingFinaleCardsInput cardsInput,
        bool includeFullTestsSection)
    {
        AppendTestSummaries(sb, culture, impression);
        AppendDirections(sb, culture);
        AppendMatchBar(sb, culture, matchCount, maxTravelMinutes, homeLabel);
        AppendDoCards(sb, culture, cardsInput, homeLabel);
        if (includeFullTestsSection)
        {
            AppendFullTestsSection(sb, culture);
        }
    }

    private static void AppendPrimaryCtas(StringBuilder sb, CultureState culture, string hubUrl, string fullTestsUrl, bool readyScreen)
    {
        sb.Append("<div class=\"ob-finale__ctas ob-finale__ctas--heroless\" data-testid=\"onboarding-finale-heroless-ctas\">");
        if (readyScreen)
        {
            sb.Append("<a class=\"ob-finale__btn ob-finale__btn--primary\" href=\"");
            sb.Append(WebUtility.HtmlEncode(PassportRedirects.PassportPath));
            sb.Append("\" data-enhance-nav=\"false\" data-testid=\"onboarding-kompas-go-passport\">");
            sb.Append(WebUtility.HtmlEncode(culture["HowLobsy.SignedIn.PassportCta"]));
            sb.Append("</a>");
            sb.Append("<a class=\"ob-finale__btn\" href=\"");
            sb.Append(WebUtility.HtmlEncode(hubUrl));
            sb.Append("\" data-testid=\"onboarding-kompas-complete\">");
            sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Finish.Cta"]));
            sb.Append("</a>");
        }

        sb.Append("<a class=\"ob-finale__link\" href=\"");
        sb.Append(WebUtility.HtmlEncode(fullTestsUrl));
        sb.Append("\" data-testid=\"onboarding-kompas-full-tests\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.FinaleV2.FullTests"]));
        sb.Append("</a></div>");
    }

    private static void AppendFullTestsSection(StringBuilder sb, CultureState culture)
    {
        sb.Append("<section class=\"ob-rustig__kompas-full-tests\" id=\"kompas-full-tests\" aria-labelledby=\"kompas-full-tests-h\">");
        sb.Append("<h2 id=\"kompas-full-tests-h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.FinaleV2.FullTests"]));
        sb.Append("</h2><p class=\"ob-rustig__kompas-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Ssr.FullTestsLead"]));
        sb.Append("</p><a class=\"ob-rustig__btn ob-rustig__btn--primary\" href=\"/profiel/tests/competence\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Ssr.FullTestsCta"]));
        sb.Append("</a></section>");
    }

    private static void AppendMatchBar(StringBuilder sb, CultureState culture, int matchCount, int maxTravelMinutes, string? homeLabel)
    {
        if (matchCount <= 0)
        {
            return;
        }

        string? travelLine = maxTravelMinutes > 0 && !string.IsNullOrWhiteSpace(homeLabel)
            ? culture.Format("Onboarding.FinaleV2.Highlight.Sub", maxTravelMinutes, homeLabel)
            : null;

        sb.Append("<a class=\"ob-finale__hl\" href=\"");
        sb.Append(WebUtility.HtmlEncode(KbRoutes.Map));
        sb.Append("\" data-testid=\"onboarding-finale-match-bar\">");
        sb.Append("<span class=\"ob-finale__hl-n\">");
        sb.Append(matchCount);
        sb.Append("</span><div class=\"ob-finale__hl-copy\"><b>");
        sb.Append(WebUtility.HtmlEncode(culture.Format("Onboarding.FinaleV2.Highlight.Title", matchCount)));
        sb.Append("</b>");
        if (!string.IsNullOrWhiteSpace(travelLine))
        {
            sb.Append("<small>");
            sb.Append(WebUtility.HtmlEncode(travelLine));
            sb.Append("</small>");
        }

        sb.Append("</div><span class=\"ob-finale__hl-go\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.FinaleV2.Highlight.Link"]));
        sb.Append("</span></a>");
    }

    private static void AppendDoCards(StringBuilder sb, CultureState culture, OnboardingFinaleCardsInput cardsInput, string? homeLabel)
    {
        var cards = PickNowCards(cardsInput);
        if (cards.Count == 0)
        {
            return;
        }

        sb.Append("<h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.FinaleV2.Do.Title"]));
        sb.Append("</h2><p class=\"ob-finale__h2s\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.FinaleV2.Do.Lead"]));
        sb.Append("</p><div class=\"ob-finale__dog\" data-testid=\"onboarding-finale-now-cards\">");
        foreach (var card in cards)
        {
            sb.Append("<article class=\"ob-finale__do\"><span class=\"ob-finale__di\" aria-hidden=\"true\">");
            sb.Append(CardIconMarkup(card.Id));
            sb.Append("</span><h4>");
            sb.Append(WebUtility.HtmlEncode(culture[card.TitleKey]));
            sb.Append("</h4><p>");
            sb.Append(WebUtility.HtmlEncode(FormatCardBody(culture, card, homeLabel)));
            sb.Append("</p></article>");
        }

        sb.Append("</div>");
    }

    private static IReadOnlyList<OnboardingFinaleCard> PickNowCards(OnboardingFinaleCardsInput input)
    {
        var available = OnboardingFinaleCards.BuildNow(input);
        var matching = available.First(c => c.Id == OnboardingFinaleCardId.Matching);
        var banenkaart = available.First(c => c.Id == OnboardingFinaleCardId.Banenkaart);
        var employers = new OnboardingFinaleCard(
            OnboardingFinaleCardId.EmployerDiscovery,
            "Onboarding.FinaleV2.Do.Employers.Title",
            "Onboarding.FinaleV2.Do.Employers.Body",
            ComingSoon: false);

        return [matching, banenkaart, employers];
    }

    private static string FormatCardBody(CultureState culture, OnboardingFinaleCard card, string? homeLabel)
        => card.Id switch
        {
            OnboardingFinaleCardId.Banenkaart when !string.IsNullOrWhiteSpace(homeLabel)
                => culture.Format(card.BodyKey, homeLabel),
            _ => culture[card.BodyKey],
        };

    private static string CardIconMarkup(OnboardingFinaleCardId id) => id switch
    {
        OnboardingFinaleCardId.Matching => OnboardingFinaleIcons.Match,
        OnboardingFinaleCardId.Banenkaart => OnboardingFinaleIcons.Map,
        OnboardingFinaleCardId.EmployerDiscovery => OnboardingFinaleIcons.Employers,
        OnboardingFinaleCardId.Passport => OnboardingFinaleIcons.Passport,
        OnboardingFinaleCardId.Deeper => OnboardingFinaleIcons.Deeper,
        OnboardingFinaleCardId.References => OnboardingFinaleIcons.References,
        OnboardingFinaleCardId.MatchNotifications => OnboardingFinaleIcons.Bell,
        _ => OnboardingFinaleIcons.Match,
    };

    private static void AppendTabLink(StringBuilder sb, string label, string href, bool samePageAnchor = false)
    {
        sb.Append("<a class=\"admin-sublink\" href=\"");
        sb.Append(WebUtility.HtmlEncode(href));
        sb.Append('"');
        if (samePageAnchor)
        {
            sb.Append(" data-enhance-nav=\"false\"");
        }

        sb.Append('>');
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</a>");
    }

    private static void AppendTestSummaries(StringBuilder sb, CultureState culture, OnboardingImpression? impression)
    {
        var summaries = OnboardingRustigKompasContent.TestSummaries(impression);
        if (summaries.Count == 0)
        {
            return;
        }

        sb.Append("<section class=\"ob-rustig__kompas-tests\" id=\"kompas-tests\" aria-labelledby=\"kompas-tests-h\">");
        sb.Append("<h2 id=\"kompas-tests-h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.Kompas.TestsTitle"]));
        sb.Append("</h2><p class=\"ob-rustig__kompas-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.Kompas.TestsLead"]));
        sb.Append("</p><div class=\"ob-rustig__kompas-test-grid\">");
        foreach (var card in summaries)
        {
            sb.Append("<article class=\"ob-rustig__kompas-test-card\"><small>");
            sb.Append(WebUtility.HtmlEncode(culture[card.TabKey]));
            sb.Append("</small>");
            if (!string.IsNullOrWhiteSpace(card.Title))
            {
                sb.Append("<h3>");
                sb.Append(WebUtility.HtmlEncode(card.Title));
                sb.Append("</h3>");
            }

            if (!string.IsNullOrWhiteSpace(card.Summary))
            {
                sb.Append("<p>");
                sb.Append(WebUtility.HtmlEncode(card.Summary));
                sb.Append("</p>");
            }

            sb.Append("</article>");
        }

        sb.Append("</div></section>");
    }

    private static void AppendDirections(StringBuilder sb, CultureState culture)
    {
        sb.Append("<section class=\"ob-rustig__kompas-directions\" id=\"kompas-directions\" aria-labelledby=\"kompas-dir-h\">");
        sb.Append("<h2 id=\"kompas-dir-h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.Kompas.DirectionsTitle"]));
        sb.Append("</h2><p class=\"ob-rustig__kompas-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.Kompas.DirectionsLead"]));
        sb.Append("</p><div class=\"ob-rustig__kompas-dir-grid\" style=\"display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:0.75rem;width:100%;max-width:100%;box-sizing:border-box\">");
        foreach (var card in OnboardingRustigKompasContent.Directions)
        {
            sb.Append("<article class=\"ob-rustig__kompas-dir-card\" style=\"display:flex;flex-direction:column;gap:0.45rem;width:100%;min-width:0;box-sizing:border-box;padding:0.65rem 0.75rem;border:1px solid color-mix(in srgb,var(--border) 75%,var(--brand) 25%);border-radius:var(--radius-sm);background:var(--surface)\"><img src=\"");
            sb.Append(WebUtility.HtmlEncode(card.ImageUrl));
            sb.Append("\" alt=\"\" width=\"320\" height=\"240\" loading=\"lazy\" decoding=\"async\" style=\"width:100%;max-width:100%;aspect-ratio:4/3;max-height:180px;object-fit:cover;border-radius:6px;display:block\" /><div style=\"min-width:0\"><h3 style=\"margin:0 0 0.25rem;font-size:0.95rem;line-height:1.25;color:var(--brand)\">");
            sb.Append(WebUtility.HtmlEncode(culture[card.TitleKey]));
            sb.Append("</h3><p style=\"margin:0;font-size:0.8125rem;line-height:1.35;color:var(--muted)\">");
            sb.Append(WebUtility.HtmlEncode(culture[card.BodyKey]));
            sb.Append("</p></div></article>");
        }

        sb.Append("</div></section>");
    }
}
