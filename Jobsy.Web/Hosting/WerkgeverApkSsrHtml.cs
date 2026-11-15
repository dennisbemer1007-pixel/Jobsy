using System.Net;
using System.Text;

namespace Jobsy.Web.Hosting;

public static class WerkgeverApkSsrHtml
{
    public static string RenderLoginPage(string? error = null, string antiforgery = "")
    {
        var err = string.IsNullOrWhiteSpace(error)
            ? ""
            : $"<p class=\"notice warn\">{WebUtility.HtmlEncode(error)}</p>";
        return $"""
<!doctype html>
<html lang="nl">
<head>
<meta charset="utf-8">
<title>Loopbaan-APK inloggen — Lobsy</title>
<link rel="stylesheet" href="/werkgever-apk/css/shared.css">
<link rel="stylesheet" href="/werkgever-apk/css/extra.css">
</head>
<body class="login">
<div class="app">
  <main class="main">
    <h1>Loopbaan-APK</h1>
    <p class="lead">Vul je bedrijf, team en persoonlijke code in.</p>
    {err}
    <form method="post" action="/loopbaan-apk/login" class="card">
      {antiforgery}
      <label>Bedrijf-id<input name="companyId" required></label>
      <label>Team-id<input name="teamId" required></label>
      <label>Code<input name="code" required autocomplete="off"></label>
      <button type="submit">Start</button>
    </form>
  </main>
</div>
</body>
</html>
""";
    }

    public static string RenderTemplate(string templateFileName, IReadOnlyDictionary<string, string> tokens)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "wwwroot",
            "werkgever-apk",
            "templates",
            templateFileName);
        if (!File.Exists(path))
        {
            var fallback = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "werkgever-apk", "templates", templateFileName);
            path = File.Exists(fallback) ? fallback : path;
        }

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