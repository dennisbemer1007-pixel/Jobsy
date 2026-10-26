using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.KandidaatBanen;

internal static class ExternalVacancyAddSsrShells
{
    internal static string BuildBody(CultureState culture, string? prefillUrl)
    {
        var urlValue = string.IsNullOrWhiteSpace(prefillUrl) ? "" : prefillUrl.Trim();
        var sb = new StringBuilder();
        sb.Append("<div class=\"kb-ext kb-ext--kreeft\" data-testid=\"kb-ext-add\" data-kb-ext-add-ssr>");
        sb.Append("<div class=\"test-kreeft-strip kb-ext__strip\" aria-hidden=\"true\" data-testid=\"kreeft-header-strip\">");
        sb.Append("<img class=\"test-kreeft-strip__desk\" src=\"");
        sb.Append(WebUtility.HtmlEncode(KreeftHeaderStripAssets.ToevoegenDesk));
        sb.Append("\" width=\"1440\" height=\"225\" alt=\"\" loading=\"lazy\" decoding=\"async\" />");
        sb.Append("<img class=\"test-kreeft-strip__mob\" src=\"");
        sb.Append(WebUtility.HtmlEncode(KreeftHeaderStripAssets.ToevoegenMob));
        sb.Append("\" width=\"780\" height=\"105\" alt=\"\" loading=\"lazy\" decoding=\"async\" />");
        sb.Append("</div><header><h1 class=\"kb-ext__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.Title"]));
        sb.Append("</h1><p class=\"kb-ext__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.Lead"]));
        sb.Append("</p></header><section class=\"kb-ext__card\"><h2 class=\"kb-ext__method-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.Method1Title"]));
        sb.Append("</h2><p class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.Method1Body"]));
        sb.Append("</p></section><section class=\"kb-ext__card\"><h2 class=\"kb-ext__method-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.Method2Title"]));
        sb.Append("</h2><div class=\"kb-ext__url-row\"><label class=\"visually-hidden\" for=\"ext-vac-url\">");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.Method2Title"]));
        sb.Append("</label><input id=\"ext-vac-url\" type=\"url\" inputmode=\"url\" autocomplete=\"off\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.UrlPlaceholder"]));
        sb.Append("\" data-testid=\"kb-ext-url-input\" value=\"");
        sb.Append(WebUtility.HtmlEncode(urlValue));
        sb.Append("\" /><button type=\"button\" class=\"kb-ext__btn\" data-testid=\"kb-ext-add-submit\" data-kb-ext-submit data-kb-ext-idle=\"");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.Submit"]));
        sb.Append("\" data-kb-ext-working=\"");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.Working"]));
        sb.Append("\" data-kb-ext-fail=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Error.TryAgain"]));
        sb.Append("\" data-kb-ext-disabled=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Panel.ErrorInline"]));
        sb.Append("\" data-kb-ext-empty=\"");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.UrlPlaceholder"]));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.Submit"]));
        sb.Append("</button></div><p class=\"state-message error\" role=\"alert\" data-kb-ext-error hidden></p></section>");
        sb.Append("<p class=\"muted kb-ext__coach-tip\">");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Add.Coach"]));
        sb.Append("</p></div>");
        return sb.ToString();
    }
}
