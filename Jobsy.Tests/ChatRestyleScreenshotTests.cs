using Microsoft.Playwright;

namespace Jobsy.Tests;

public class ChatRestyleScreenshotTests
{
    private const string OutDir = "/opt/cursor/artifacts/chat-r1";

    [Fact]
    public async Task Render_chat_restyle_preview_screenshots()
    {
        Directory.CreateDirectory(OutDir);
        var root = FindRoot();
        var www = Path.Combine(root, "Jobsy.Web/wwwroot");
        var css = await File.ReadAllTextAsync(Path.Combine(www, "css/app.min.css"));
        var mark = "file://" + Path.Combine(www, "brand/svg/lobsy-merkteken.svg");
        var sil = "file://" + Path.Combine(www, "brand/svg/lobsy-monochroom-wit.svg");

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var shots = new (string Name, string Body, int W, int H)[]
        {
            ("candidate-sollicitaties-1440-closed", WarmBody(mark, false), 1440, 900),
            ("candidate-sollicitaties-1440-open", WarmBody(mark, true), 1440, 900),
            ("candidate-sollicitaties-390-closed", WarmBody(mark, false), 390, 844),
            ("candidate-sollicitaties-390-open", WarmBody(mark, true), 390, 844),
            ("candidate-vacancy-390-open", WarmBody(mark, true, vacancy: true), 390, 844),
            ("employer-1440-closed", BusinessBody(sil, false), 1440, 900),
            ("employer-1440-open", BusinessBody(sil, true), 1440, 900),
            ("admin-1440-closed", BusinessBody(sil, false), 1440, 900),
        };

        foreach (var (name, body, w, h) in shots)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h },
            });
            var page = await context.NewPageAsync();
            var html = $"<!DOCTYPE html><html><head><meta charset=utf-8><style>{css}</style>"
                       + $"<base href=\"file://{www}/\"></head><body>{body}</body></html>";
            await page.SetContentAsync(html);
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(OutDir, name + ".png"),
            });
        }
    }

    private static string WarmBody(string mark, bool open, bool vacancy = false)
    {
        var pageName = vacancy ? "deze vacature" : "Sollicitaties";
        var greeting = vacancy
            ? "Hoi Sanne. Je bekijkt Zorgmedewerker dagbesteding. Waar kan ik je mee helpen?"
            : "Hoi Sanne. Je hebt dinsdag een gesprek bij Bakkerij De Korenaar, mooi zeg. Zal ik je helpen het voor te bereiden?";
        var openPanel = open
            ? $"""
<div class="lobsy-assistant is-open lobsy-assistant--skin-warm lobsy-assistant--coach has-bottom-nav"><div id="lobsy-assistant-panel" class="lobsy-assistant__panel"><header class="lobsy-assistant__header"><div class="lobsy-assistant__brand"><img class="lobsy-assistant__avatar" src="{mark}" width="40" height="40"/><div><h2>Lobsy</h2><p class="lobsy-assistant__lead">Vraag het Lobsy</p></div></div><div class="lobsy-assistant__header-actions"><button class="lobsy-assistant__icon-btn">+</button><button class="lobsy-assistant__icon-btn">×</button></div></header><p class="lobsy-assistant__context"><span class="lobsy-assistant__context-dot"></span>Je bekijkt nu {pageName}, ik denk met je mee</p><div class="lobsy-assistant__chat"><div class="lobsy-assistant__bubble lobsy-assistant__bubble--assistant"><div class="lobsy-assistant__bubble-head"><img class="lobsy-assistant__bubble-logo" src="{mark}" width="32" height="32"/><span>Lobsy</span></div><p>{greeting}</p></div></div><div class="lobsy-assistant__chips"><button class="lobsy-assistant__chip">Ja, help me voorbereiden</button><button class="lobsy-assistant__chip">Wat betekent 'Bekeken'?</button><button class="lobsy-assistant__chip">Hoe stuur ik een initiatief?</button></div><form class="lobsy-assistant__compose"><div class="lobsy-assistant__compose-row"><textarea placeholder="Typ je vraag"></textarea><button type="button" class="lobsy-assistant__send">→</button></div></form><p class="lobsy-assistant__disclaimer">Lobsy is een AI-assistent en geeft geen persoonlijk advies. Deel geen BSN, bankgegevens of wachtwoorden. <a href="#">Meer over Lobsy</a></p><p class="lobsy-assistant__human">Liever een mens spreken? <button class="auth-link">Stuur ons een bericht</button></p></div></div>
"""
            : "";
        return $"""
<div class="app-shell has-bottom-nav" style="--bottom-nav-h:64px;min-height:900px;background:#f5f2ee">
<main style="padding:1rem"><div class="card" style="max-width:720px;margin:2rem auto;padding:1rem;background:#fff;border-radius:12px"><h1>Sollicitaties</h1><button class="btn btn-primary" style="margin-top:1rem">Bekijk</button></div></main>
<nav class="bottom-nav" style="position:fixed;bottom:0;left:0;right:0;height:64px;background:#fff;border-top:1px solid #ddd;display:flex;justify-content:space-around;align-items:center;z-index:50"><span>Home</span><span>Sollicitaties</span><span>Paspoort</span></nav>
{openPanel}
<div class="lobsy-coach-dock" data-lobsy-coach><button id="lobsy-coach-btn" class="lobsy-coach-dock__btn lobsy-coach-dock__btn--warm"><span class="lobsy-coach-dock__medallion"><img src="{mark}" width="56" height="56"/></span><span class="lobsy-coach-dock__label">Vraag het Lobsy</span></button></div>
</div>
""";
    }

    private static string BusinessBody(string sil, bool open)
    {
        var launcher = open
            ? ""
            : $"""<div class="lobsy-assistant-launcher lobsy-assistant-launcher--business has-bottom-nav"><button class="lobsy-assistant-launcher__btn"><img src="{sil}" width="28" height="28"/></button></div>""";
        var panel = open
            ? $"""
<div class="lobsy-assistant is-open lobsy-assistant--skin-business lobsy-assistant--business-panel has-bottom-nav"><div class="lobsy-assistant__panel"><header class="lobsy-assistant__header"><div class="lobsy-assistant__brand"><span class="lobsy-assistant__avatar lobsy-assistant__avatar--business"><img src="{sil}" width="24" height="24"/></span><div><h2>Lobsy-assistent</h2><p class="lobsy-assistant__lead">Antwoord op je vragen over Lobsy</p></div></div><div class="lobsy-assistant__header-actions"><button class="lobsy-assistant__icon-btn">+</button><button class="lobsy-assistant__icon-btn">×</button></div></header><div class="lobsy-assistant__chat"><div class="lobsy-assistant__bubble lobsy-assistant__bubble--assistant"><div class="lobsy-assistant__bubble-head"><span class="lobsy-assistant__avatar lobsy-assistant__avatar--business"><img src="{sil}" width="20" height="20"/></span><span>Lobsy</span></div><p>Hoi Sanne. Ik help je met vragen over vacatures en sollicitaties. Waar kan ik je mee helpen?</p></div></div><form class="lobsy-assistant__compose"><div class="lobsy-assistant__compose-row"><textarea placeholder="Typ je vraag"></textarea><button type="button" class="lobsy-assistant__send">→</button></div></form><p class="lobsy-assistant__disclaimer">Lobsy is een AI-assistent en geeft geen persoonlijk advies. Deel geen BSN, bankgegevens of wachtwoorden. <a href="#">Meer over Lobsy</a></p></div></div>
"""
            : "";
        return $"""
<div class="wg-shell wg-has-bottom-nav" style="--bottom-nav-h:64px;min-height:900px;background:#eef1f5">
<main class="wg-main" style="padding:1rem"><h1>Vacatures</h1><button class="btn btn-primary">Nieuwe vacature</button></main>
{launcher}
{panel}
</div>
""";
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
