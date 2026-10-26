using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Web.Banenkaart;
using Jobsy.Web.Hosting;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Services;

namespace Jobsy.Web.Match;

internal static class MatchSsrShells
{
    internal static string BuildBody(
        CultureState culture,
        MatchDeck deck,
        string antiforgeryToken,
        string onboardingStartPath,
        bool occupationDayInLifeEnabled)
    {
        var sb = new StringBuilder();
        var locked = !(deck.Gate.IsProfileComplete && deck.Gate.FitGateOpen);
        sb.Append("<div class=\"match-page-wrap\">");
        KreeftHeaderStripHtml.Append(sb, KreeftHeaderStripAssets.MatchDesk, KreeftHeaderStripAssets.MatchMob, "match");
        sb.Append("<div class=\"match-page match-page--shell match-page--illustrated");
        if (locked)
        {
            sb.Append(" match-page--locked");
        }

        sb.Append("\" data-testid=\"match-page-ssr\" data-match-ssr=\"1\" tabindex=\"-1\" data-progress-format=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Match.DialogProgress"]));
        sb.Append("\" data-toast-liked=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Match.ToastLiked"]));
        sb.Append("\" data-toast-skipped=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Match.ToastSkipped"]));
        sb.Append("\" data-toast-error=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Error.TryAgain"]));
        sb.Append("\">");
        AppendHeader(sb, culture, deck);
        sb.Append("<section class=\"match-page__body");
        if (deck.Gate.IsProfileComplete && deck.Gate.FitGateOpen)
        {
            sb.Append(" match-page__panel--swipe");
        }
        else
        {
            sb.Append(" match-page__panel--gate");
        }

        sb.Append("\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Match.DialogTitle"]));
        sb.Append("\">");
        AppendBody(sb, culture, deck, onboardingStartPath, occupationDayInLifeEnabled);
        sb.Append("</section></div></div>");
        sb.Append("<input type=\"hidden\" id=\"match-ssr-token\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" />");
        AppendBootJson(sb, deck);
        sb.Append("<script src=\"");
        sb.Append(WwwrootAssetCatalog.Url("js/match-ssr.js"));
        sb.Append("\" defer></script>");
        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb, CultureState culture, MatchDeck deck)
    {
        sb.Append("<header class=\"match-page__top\"><div class=\"match-page__brand\"><div><h1 class=\"match-page__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.DialogTitle"]));
        sb.Append("</h1>");
        if (deck.Gate.IsProfileComplete && deck.Gate.FitGateOpen && deck.Count > 0 && !deck.IsFinished)
        {
            sb.Append("<p class=\"match-page__progress-text\" data-match-progress-text aria-live=\"polite\">");
            sb.Append(WebUtility.HtmlEncode(culture.Format("Match.DialogProgress", deck.Position, deck.Count)));
            sb.Append("<span> · ");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.NoWrongChoice"]));
            sb.Append("</span></p>");
        }

        sb.Append("</div></div><div class=\"match-page__top-actions\"><a class=\"match-page__icon-btn\" href=\"");
        sb.Append(KbRoutes.Map);
        sb.Append("\" title=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.BackToSearch"]));
        sb.Append("\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.BackToSearch"]));
        sb.Append("\" data-enhance-nav=\"false\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><circle cx=\"11\" cy=\"11\" r=\"7\"/><path d=\"m20 20-3.5-3.5\"/></svg></a></div></header>");
        if (deck.Gate.IsProfileComplete && deck.Gate.FitGateOpen && deck.Count > 0 && !deck.IsFinished)
        {
            var fill = deck.Count == 0 ? 0 : (int)Math.Round(100.0 * deck.Position / deck.Count);
            sb.Append("<div class=\"match-page__bar\" data-match-progress-bar role=\"progressbar\" aria-valuemin=\"1\" aria-valuemax=\"");
            sb.Append(deck.Count);
            sb.Append("\" aria-valuenow=\"");
            sb.Append(deck.Position);
            sb.Append("\"><svg class=\"match-page__bar-svg\" viewBox=\"0 0 100 4\" preserveAspectRatio=\"none\" aria-hidden=\"true\"><rect class=\"match-page__bar-track\" x=\"0\" y=\"0\" width=\"100\" height=\"4\" rx=\"2\" /><rect class=\"match-page__bar-fill\" x=\"0\" y=\"0\" width=\"");
            sb.Append(fill);
            sb.Append("\" height=\"4\" rx=\"2\" /></svg></div>");
        }
    }

    private static void AppendBody(
        StringBuilder sb,
        CultureState culture,
        MatchDeck deck,
        string onboardingStartPath,
        bool occupationDayInLifeEnabled)
    {
        if (!deck.Gate.IsProfileComplete)
        {
            MatchSsrGateHtml.Append(sb, culture, deck.Gate, onboardingStartPath, fitGateOnly: false);
            return;
        }

        if (!deck.Gate.FitGateOpen)
        {
            MatchSsrGateHtml.Append(sb, culture, deck.Gate, onboardingStartPath, fitGateOnly: true);
            return;
        }

        if (deck.IsFinished)
        {
            sb.Append("<div class=\"match-page__empty\" data-testid=\"match-deck-done\"><h2>");
            sb.Append(WebUtility.HtmlEncode(culture["Match.DeckDone"]));
            sb.Append("</h2><div class=\"match-page__empty-actions\"><a class=\"match-page__cta\" href=\"");
            sb.Append(KbRoutes.Saved);
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.ViewSaved"]));
            sb.Append("</a><a class=\"match-page__cta match-page__cta--ghost\" href=\"");
            sb.Append(KbRoutes.Map);
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.BackToMap"]));
            sb.Append("</a></div></div>");
            return;
        }

        if (deck.Current is null)
        {
            sb.Append("<div class=\"match-page__empty\"><h2>");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.EmptyTitle"]));
            sb.Append("</h2><p>");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.EmptyLead"]));
            sb.Append("</p><div class=\"match-page__empty-actions\"><a class=\"match-page__cta\" href=\"");
            sb.Append(KbRoutes.Map);
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.BackToSearch"]));
            sb.Append("</a><a class=\"match-page__cta match-page__cta--ghost\" href=\"/candidate/match\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.Reload"]));
            sb.Append("</a></div></div>");
            return;
        }

        var startIndex = deck.Index;
        sb.Append("<div class=\"match-page__grid\" data-match-deck>");
        MatchSsrSwipeHtml.AppendStack(sb, culture, deck.Items, startIndex);
        MatchSsrSideHtml.AppendPanels(sb, culture, deck.Items, startIndex, occupationDayInLifeEnabled);
        sb.Append("</div>");
        sb.Append("<p class=\"match-page__toast\" data-match-toast role=\"status\" hidden></p>");
    }

    private static void AppendBootJson(StringBuilder sb, MatchDeck deck)
    {
        var ids = deck.Items
            .Select(v => v.VacancyId)
            .Where(id => id is Guid)
            .Select(id => id!.Value)
            .ToList();
        var payload = new
        {
            index = deck.Index,
            count = deck.Count,
            vacancyIds = ids,
            savePath = BanenkaartSsrQuery.SavePath
        };
        sb.Append("<script type=\"application/json\" id=\"match-ssr-boot\">");
        sb.Append(WebUtility.HtmlEncode(JsonSerializer.Serialize(payload)));
        sb.Append("</script>");
    }
}
