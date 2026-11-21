using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Passport;

internal static class PassportSsrHeroHtml
{
    internal static void Append(StringBuilder sb, CultureState culture, string? firstName)
    {
        var greeting = BuildGreeting(culture, firstName);

        sb.Append("<header class=\"cand-hero cand-hero--passport-plain\" data-testid=\"passport-greeting\"><div class=\"cand-hero__copy\"><h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(greeting));
        sb.Append("</h1><p class=\"cand-hero__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Greeting.Lead"]));
        sb.Append("</p></div></header>");
    }

    private static string BuildGreeting(CultureState culture, string? firstName)
    {
        var hour = DateTime.Now.Hour;
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return hour switch
            {
                < 12 => culture["Kompas.Greeting.PlainMorning"],
                < 18 => culture["Kompas.Greeting.PlainAfternoon"],
                _ => culture["Kompas.Greeting.PlainEvening"],
            };
        }

        var key = hour switch
        {
            < 12 => "Kompas.Greeting.Morning",
            < 18 => "Kompas.Greeting.Afternoon",
            _ => "Kompas.Greeting.Evening",
        };
        return culture.Format(key, firstName.Trim());
    }
}
