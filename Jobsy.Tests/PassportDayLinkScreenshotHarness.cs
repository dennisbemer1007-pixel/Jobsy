using System.Text;
using Jobsy.Core.Careers;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Passport;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

internal static class PassportDayLinkScreenshotHarness
{
    public const string CookId = "90f75f67-495d-49fa-ab57-2f320e251d7e";

    public static CultureState CreateCulture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AuthenticationStateProvider>(new EmptyAuth());
        services.AddSingleton<IJSRuntime, NoJsRuntime>();
        services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        var culture = services.BuildServiceProvider().GetRequiredService<CultureState>();
        culture.SetLanguageAsync("nl").GetAwaiter().GetResult();
        return culture;
    }

    public static string InlineStyles(string repoRoot, params string[] relativePaths)
    {
        var wwwroot = Path.Combine(repoRoot, "Jobsy.Web/wwwroot");
        var blobs = new List<string>();
        foreach (var rel in relativePaths)
        {
            var path = Path.Combine(wwwroot, rel.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path))
            {
                blobs.Add(File.ReadAllText(path));
            }
        }

        foreach (var href in PageAssetCatalog.StylesheetsForPupilSsr("/leerling/paspoort"))
        {
            var rel = href.TrimStart('/').Split('?')[0];
            var path = Path.Combine(wwwroot, rel.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path))
            {
                blobs.Add(File.ReadAllText(path));
            }
        }

        return string.Join('\n', blobs);
    }

    public static string Wrap(string css, string body)
        => $$"""
            <!DOCTYPE html><html lang="nl"><head><meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <link href="https://fonts.googleapis.com/css2?family=Fraunces:wght@700&family=Lexend:wght@400;600&display=swap" rel="stylesheet" />
            <style>{{css}}</style></head>
            <body style="margin:0;background:#f5f2ee;padding:20px;font-family:Lexend,sans-serif">{{body}}</body></html>
            """;

    public static string G78PaspoortJobCard(CultureState culture, bool withDay)
    {
        var model = new PupilG78PassportWebDto(
            Enabled: true,
            OccupationCards:
            [
                new PupilG78PassportCardDto(
                    "Natuur en buiten",
                    "Kok",
                    "Je werkt met eten en mensen.",
                    ["Samen"],
                    82,
                    WhyFit: null,
                    DayEscoId: withDay ? CookId : null),
            ]);
        return PupilG78PassportSsrHtml.Render(model, culture);
    }

    public static string DitBenJijJobs(CultureState culture, bool withDay)
    {
        var sb = new StringBuilder();
        sb.Append("<h2 class=\"rsub\">Beroepen om eens te bekijken</h2>");
        sb.Append("<div class=\"chips ll-g78-result-jobs\" data-testid=\"ll-g78-result-jobs\">");
        AppendChipRow(sb, culture, "Kok", withDay ? CookId : null);
        AppendChipRow(sb, culture, "Hovenier", null);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendChipRow(StringBuilder sb, CultureState culture, string label, string? dayEscoId)
    {
        sb.Append("<div class=\"ll-g78-result-job-row\">");
        sb.Append("<span class=\"chip\">").Append(System.Net.WebUtility.HtmlEncode(label)).Append("</span>");
        if (!string.IsNullOrWhiteSpace(dayEscoId))
        {
            sb.Append("<span class=\"ll-g78-result-job-day\">");
            OccupationDayPassportSsr.AppendLeerlingButton(sb, culture, dayEscoId!, label);
            sb.Append("</span>");
        }

        sb.Append("</div>");
    }

    public static string CandidateFitPastDit(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <div class="passport-ssr passport-fit-shell" style="max-width:720px">
            <nav class="passport-tabs" aria-label="Paspoort"><a class="passport-tabs__tab is-active" href="#">Past dit beroep?</a></nav>
            <div class="passport-fit__layout"><section class="detail-card passport-fit__card">
            """);
        sb.Append("<h3 class=\"passport-fit__h\">");
        sb.Append(System.Net.WebUtility.HtmlEncode(culture["Passport.Fit.Title"]));
        sb.Append("</h3><p class=\"passport-fit__last-line\">");
        sb.Append(System.Net.WebUtility.HtmlEncode(culture.Format("Passport.Fit.LastCheck", "kok")));
        sb.Append("</p><p class=\"passport-fit__day\">");
        OccupationDayPassportSsr.AppendCandidateButton(sb, culture, CookId, "kok");
        sb.Append("</p></section></div></div>");
        return sb.ToString();
    }

    public static string CandidateFutureJobs(CultureState culture)
    {
        var list = new FutureJobsList(
            false,
            [new FutureJobRow(CookId, "kok", 80, 3, "groot", 12, null, [])],
            OccupationOutlook.Shared.Peildatum);
        var sb = new StringBuilder();
        var stored = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { CookId };
        PassportSsrFutureJobsHtml.Append(sb, culture, list, "Nederland", "", showCheck: true, stored);
        return sb.ToString();
    }

    public static string PupilDayPage(CultureState culture)
    {
        var day = new OccupationDayResponse
        {
            Enabled = true,
            Found = true,
            EscoId = CookId,
            TitleNl = "kok",
            Morning = "Je start je dag met de mise en place.",
            Midday = "Rond het middaguur is het druk in de keuken.",
            Afternoon = "In de middag serveer je de gerechten.",
            Closing = "Je ruimt op en sluit af.",
            Highlights = ["Samen koken"],
            VariesNote = "Elke keuken werkt anders.",
            ThinSource = false,
            Language = "nl",
            Blocks =
            [
                new OccupationDayBlock("07:00", "07:00 Start", "Je kijkt wat er op het menu staat."),
                new OccupationDayBlock("12:00", "12:00 Lunch", "Het is druk. Je helpt mee met serveren."),
            ],
            Tasks = ["Porties klaarmaken"],
            Skills = ["Nauwkeurig werken"],
        };
        return PupilOccupationDaySsrHtml.Build(culture, "kok", day, unknown: false, error: false);
    }

    private sealed class EmptyAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class NoJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => default;
    }
}
