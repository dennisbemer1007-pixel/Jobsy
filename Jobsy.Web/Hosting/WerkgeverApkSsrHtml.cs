using System.Net;
using System.Text;
using Jobsy.Core.Contracts.WerkgeverApk;

namespace Jobsy.Web.Hosting;

public static class WerkgeverApkSsrHtml
{
    public static string RenderLoginPage(
        IReadOnlyList<WerkgeverApkEmployeeCompanyOptionDto> companies,
        Guid? selectedCompanyId,
        Guid? selectedTeamId,
        string? error = null,
        string antiforgery = "",
        string? prefillCode = null)
    {
        var err = string.IsNullOrWhiteSpace(error)
            ? ""
            : $"<p class=\"notice warn\">{WebUtility.HtmlEncode(error)}</p>";

        var codeOnly = selectedCompanyId is Guid co
                       && selectedTeamId is Guid te
                       && co != Guid.Empty
                       && te != Guid.Empty;
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"nl\"><head>");
        sb.Append("<meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append("<title>Loopbaan-APK inloggen — Lobsy</title>");
        sb.Append("<link rel=\"stylesheet\" href=\"/werkgever-apk/css/shared.css\">");
        sb.Append("<link rel=\"stylesheet\" href=\"/werkgever-apk/css/extra.css\"></head>");
        sb.Append("<body class=\"login\"><div class=\"app\"><main class=\"main\">");
        sb.Append("<h1>Loopbaan-APK</h1><p class=\"lead\">Kies je bedrijf en team, of log in met alleen je code.</p>");
        sb.Append(err);
        sb.Append("<form method=\"post\" action=\"/loopbaan-apk/login\" class=\"card\">");
        sb.Append(antiforgery);

        if (codeOnly && selectedCompanyId is Guid companyId && selectedTeamId is Guid teamId)
        {
            sb.Append("<input type=\"hidden\" name=\"companyId\" value=\"");
            sb.Append(WebUtility.HtmlEncode(companyId.ToString("D")));
            sb.Append("\" /><input type=\"hidden\" name=\"teamId\" value=\"");
            sb.Append(WebUtility.HtmlEncode(teamId.ToString("D")));
            sb.Append("\" />");
        }
        else
        {
            sb.Append("<label>Bedrijf<select name=\"companyId\" id=\"wa-company\"><option value=\"\">Kies bedrijf</option>");
            foreach (var c in companies)
            {
                sb.Append("<option value=\"").Append(WebUtility.HtmlEncode(c.Id.ToString("D"))).Append('"');
                if (selectedCompanyId == c.Id)
                {
                    sb.Append(" selected");
                }

                sb.Append('>').Append(WebUtility.HtmlEncode(c.Name)).Append("</option>");
            }

            sb.Append("</select></label><label>Team<select name=\"teamId\" id=\"wa-team\"><option value=\"\">Kies team</option>");
            var teams = companies
                .Where(c => selectedCompanyId is null || c.Id == selectedCompanyId)
                .SelectMany(c => c.Teams);
            foreach (var t in teams)
            {
                sb.Append("<option value=\"").Append(WebUtility.HtmlEncode(t.Id.ToString("D"))).Append('"');
                if (selectedTeamId == t.Id)
                {
                    sb.Append(" selected");
                }

                sb.Append(" data-company=\"").Append(WebUtility.HtmlEncode(
                    companies.First(c => c.Teams.Any(x => x.Id == t.Id)).Id.ToString("D")));
                sb.Append("\">").Append(WebUtility.HtmlEncode(t.Name)).Append("</option>");
            }

            sb.Append("</select></label>");
        }

        sb.Append("<label>Jouw code<input name=\"code\" required autocomplete=\"off\" maxlength=\"12\" value=\"");
        sb.Append(WebUtility.HtmlEncode(prefillCode ?? ""));
        sb.Append("\" placeholder=\"Bijv. K7Q-M2P\" /></label>");
        sb.Append("<p class=\"muted\">We slaan geen naam op. Alleen je code telt.</p>");
        sb.Append("<button type=\"submit\" class=\"btn primary\">Start</button></form>");
        sb.Append("</main></div>");
        sb.Append("""
<script>
(function(){
  var companies = document.getElementById('wa-company');
  var teams = document.getElementById('wa-team');
  if (!companies || !teams) return;
  var all = Array.from(teams.querySelectorAll('option[data-company]'));
  companies.addEventListener('change', function () {
    var cid = companies.value;
    teams.innerHTML = '<option value="">Kies team</option>';
    all.filter(function (o) { return o.getAttribute('data-company') === cid; })
      .forEach(function (o) { teams.appendChild(o.cloneNode(true)); });
  });
})();
</script>
""");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    public static string RenderTemplate(string templateFileName, IReadOnlyDictionary<string, string> tokens)
    {
        var path = WerkgeverApkTemplateLocator.PathFor(templateFileName);
        var html = File.Exists(path) ? File.ReadAllText(path) : "<html><body><p>Pagina niet beschikbaar.</p></body></html>";
        html = html
            .Replace("shared.css", "/werkgever-apk/css/shared.css", StringComparison.Ordinal)
            .Replace("extra.css", "/werkgever-apk/css/extra.css", StringComparison.Ordinal)
            .Replace("img/", "/werkgever-apk/img/", StringComparison.Ordinal);

        var sb = new StringBuilder(html);
        foreach (var (key, value) in tokens)
        {
            sb.Replace($"{{{{{key}}}}}", WebUtility.HtmlEncode(value));
        }

        return sb.ToString();
    }
}
