# Review: foutpagina's (stack A): fase 1

*Bron: `origin/acceptatie` @ `a611db40` (read-only worktree). Live check lobsy.nl (= `origin/main` @ `9a2c0d49`, 214 commits achter acc) op 30-09-2026 ±09:30 CEST.*
*Status: fase 1 (review + mockups + vragen). Nog geen spec: wacht op "akkoord" van Dennis.*

## 1. Wat er nu is

| Onderdeel | Bestand | Nu |
|---|---|---|
| Fout (500) | `Jobsy.Web/Components/Pages/Error.razor` (`/Error`) | NL hard-coded, `MainLayout` (volledige nav + API-componenten → kan zelf falen), geen `[ExcludeFromInteractiveRouting]`, toont ruwe `TraceIdentifier`, link "Naar de banenkaart" |
| Handler | `Jobsy.Web/Program.cs:166` `UseExceptionHandler("/Error", createScopeForErrors: true)` | alleen buiten Development; POST-re-execute kan op Blazor-antiforgery stuiten |
| 404 | n.v.t. | **Bestaat niet.** Geen `UseStatusCodePages*`, geen Router `<NotFound>`. Live `/bestaat-niet` → 404 met **0-byte body** (kale browserpagina) |
| Geen toegang | `Pages/AccessDenied.razor` + `Routes.razor` `RedirectToAccessDenied` | forceLoad-redirect → `/access-denied` geeft **200** (hoort 403), oorspronkelijke URL weg, knoppen `/home` + `/login`, NL only |
| 429 | rate limiter | lege body |
| Reconnect | `Components/App.razor:235` | "Verbinding herstellen…" hard-coded NL (test `BlazorCircuitGuardTests.cs:59` checkt op die string) |
| Onderhoud | n.v.t. | Geen onderhoudsmodus; bij down/deploy toont Cloudflare/Render eigen 502/503-pagina |

## 2. Bevindingen

1. **Geen echte 404** (hoog, SEO + UX). Kale lege pagina; bezoekers raken de weg kwijt.
2. **Soft-404's** (middel, SEO):
   - Onbekende vacature live (main): **200 + `index,follow`** met "Even iets misgegaan" uit de `MainLayout`-ErrorBoundary.
   - Op acc toont `VacancyDetail` "Vacature niet gevonden" maar zet geen 404-status.
   - `/{8 cijfers}` onbekend: wel 404, maar binnen de kaart-chrome.
   - Gesloten vacature: hoort 410 + "deze vacature is dicht, hier lijken op".
3. **Error-pagina kan zelf crashen**: `MainLayout` laadt nav/API-componenten. Voorstel: eigen `ErrorLayout` (statische SSR, geen circuit, geen DB/API-calls), `[ExcludeFromInteractiveRouting]`.
4. **Techniek zichtbaar**: ruwe TraceIdentifier op /Error en `ex.Message` op o.a. PartnerSales en PrivacyData. Voorstel: korte supportcode (bijv. `LB-7Q3K`), in logs/Sentry gekoppeld aan request-id; nooit exceptiontekst tonen.
5. **403 als 200**, zonder uitleg welk account is ingelogd en zonder terug-URL.
6. **Niet vertaald**: Error, AccessDenied en reconnect-toast zijn NL only; ar (RTL) ontbreekt.
7. **Geen onderhoudsmodus**: deploys/migraties tonen een Render/Cloudflare-pagina in het Engels.
8. `Circuit:DetailedErrors` staat true op acc en false op prod. Dat is bewust, geen actie.

## 3. Voorstel (voor de spec, na akkoord)

- **ErrorLayout** = warme PublicLayout-stijl uit `docs/landing` (lichte kop met logo + taal, golf, kreeft-mascotte, simpele footer), statische SSR, `noindex`, 5 talen incl. RTL, taal uit cookie/Accept-Language.
- **Pagina's**:
  - **404**: zoekveld "Wat zoek je?", 3 knoppen: Banenkaart, Gratis test, Hulp.
  - **500**: excuus, supportcode, "Probeer opnieuw", mail support met code vooraf ingevuld.
  - **403 "Geen toegang"**: "Je bent ingelogd als X". Knoppen "Naar mijn start" en "Inloggen met een ander account" (`returnUrl` bewaard). Echte 403-status.
  - **410**: vacature gesloten, met vergelijkbare vacatures.
  - **429**: "Even rustig aan", wacht-seconden.
  - **503 onderhoud**: in-app schakelaar (admin-instelling), `Retry-After`, admins mogen erdoor.
- **Statische fallback** `maintenance.html` (alle 5 talen onder elkaar, geen scripts, inline SVG) voor Cloudflare (custom error page / Worker) en Render. De Cloudflare-plan-mogelijkheden moeten nog gecheckt worden.
- **Techniek**:
  - `UseStatusCodePagesWithReExecute("/status/{0}")` voor niet-API-routes. API blijft ProblemDetails JSON.
  - Router `<NotFound>` rendert dezelfde component.
  - `VacancyDetail`/`CompanyPublicPage`/`/{kvk}` zetten echte 404/410.
- **Kleine meldingen**: reconnect-toast en inline blok-fout ("Dit stukje laadt niet") in 5 talen.

## 4. Afhankelijkheden

- Landing-stack: PublicLayout/`PublicRoutes` (deel 01) levert de kop/footer; ErrorLayout is een afgeslankte variant zonder data-calls.
- Footer-regel juridische naam/KvK: gedeelde `Legal:*`-config (zie public-review; ook e-mail-footer `MailOptions`).
- `BlazorCircuitGuardTests` aanpassen als reconnect-tekst via resources gaat.

## 5. Hotfix-kandidaat (los)

- **Klein**: echte 404-body (minimaal statische NL/EN pagina via `UseStatusCodePagesWithReExecute`) + 404-status voor onbekende vacature (nu 200/index op prod). Kan los van het design; het design volgt in de stack.

## 6. Mockups (`docs/mockups/errors/`, HTML in `html/`)

| PNG | Wat |
|---|---|
| `er-d00-huidig.png` | Huidige Error, AccessDenied, kale 404 |
| `er-d01-404.png` / `er-m01-404.png` | Nieuwe 404 |
| `er-d02-500.png` / `er-m02-500.png` | 500 met supportcode |
| `er-d03-geen-toegang.png` / `er-m03-geen-toegang.png` | 403 met account + wissel |
| `er-d04-onderhoud.png` / `er-m04-onderhoud.png` | Onderhoud: in-app + statische 5-talen-variant |
| `er-d05-vacature-gesloten.png` | 410 gesloten vacature |
| `er-m05-404-arabisch-rtl.png` | 404 in het Arabisch (RTL) |
| `er-d06-kleine-meldingen.png` | Reconnect-toast, 429, inline blokfout |

Opnieuw bouwen: `cd mockup/errors && python3 build.py [filter]`.

## 7. Vragen (B1) met voorstel

1. **Onderhoud**: een schakelaar in admin + een vaste pagina bij Cloudflare? *Voorstel: ja, allebei. Ik check eerst wat ons Cloudflare-plan kan.*
2. **Foutcode**: tonen we een korte code (bijv. LB-7Q3K) die support kan opzoeken? *Voorstel: ja, geen technische tekst.*
3. **404-knoppen**: Banenkaart, Gratis test en Hulp? *Voorstel: ja, plus een zoekveld.*
4. **Gesloten vacature**: tonen we vergelijkbare vacatures in de buurt? *Voorstel: ja, maximaal 3.*
5. **Mini-hotfix**: nu al een simpele echte 404-pagina en 404-status voor onbekende vacatures? *Voorstel: ja, los van deze stack.*
