# 01. ErrorLayout (static, noindex, 5 languages, RTL, no data calls), friendly 404, 500 with support code, real status codes

Read `00-README.md` first ("How to run", §0, §IA, E2, E3, E5, E9, Dependencies A–C, F). Branch `cursor/errors-1` from `origin/acceptatie`. **First in the stack.**

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/errors-1` from `origin/acceptatie`. ONE PR into `acceptatie`; the body starts with `Stacked on: none (first in the stack)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/errors-1`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Error pages never show `ex.Message`, stack traces, request ids or internal ids, and never call the API or DB to render.
> - Run the README Dependencies checks A–G first and write the outcome at the top of the PR body.

| | |
|---|---|
| Branch | `cursor/errors-1` |
| PR title | `feat(errors): ErrorLayout in the public style (static, noindex, 5 languages, RTL), friendly 404, 500 with support code, real status codes for unknown pages and vacancies` |
| Mockups | `er-d00-huidig` (today), `er-d01-404`, `er-m01-404`, `er-m05-404-arabisch-rtl`, `er-d02-500`, `er-m02-500` |
| Migration | none |
| Split seam | **01a** = `ErrorLayout` + strings + 404 + status wiring; **01b** = 500 + `SupportCode` + Sentry/log scope |

## Goal
When something goes wrong, the visitor sees a calm Lobsy page in their language with a way forward, crawlers see the real status code, and support can find the error from a short code.

## 01.1 Today (verify first)
- `Pages/Error.razor` (29 lines):
  - `@page "/Error"`, hard-coded Dutch "Er ging iets mis", link "Naar de banenkaart" `/`
  - shows `Referentie: {HttpContext.TraceIdentifier}`
  - no layout attribute, so `MainLayout` renders (nav, `AuthHeader`, `NotificationBell` and API-backed components can fail again); no `[ExcludeFromInteractiveRouting]`
- `Program.cs` L166: `UseExceptionHandler("/Error", createScopeForErrors: true)` outside Development only. There's no `UseStatusCodePages*` and the Router (`Components/Routes.razor`) has no `NotFound`.
- Live `/bestaat-niet` → 404 with an empty body.
- Unknown vacancy (`VacancyDetail.razor` L45 `Vacancy.NotFound`) → 200 and indexable (unless `docs/public-pages` 01 landed).
- `MainLayout.razor` L42–56: `ErrorBoundary` with `Circuit.ErrorTitle` "Even iets misgegaan" (04 touches it).
- The API `ExceptionHandlingMiddleware.cs` L65 adds `traceId` to ProblemDetails. Sentry is set up in Web (`Program.cs` L16–19) and the API.

## 01.2 `ErrorLayout` (`Components/Layout/ErrorLayout.razor`)
- **Root:** `<div class="err-layout pub-theme" dir="{rtl|ltr}">` (`pub-theme` only when Dependency A is present).
- **Header:** logo → `/`, a language menu of plain links (`/taal/{lang}?returnUrl={current path}` when landing 01 is present, else today's culture endpoint), "Inloggen" (hidden when signed in; then "Naar mijn start").
- **Main:**
  - two columns ≥ 900 px: text left, mascot + soft blob right (er-d01); stacked on mobile with the mascot on top (er-m01)
  - `<main id="main">`, one `h1`, focus on the h1 on load via `autofocus` on a skip target (no JS needed)
- **Footer:** "© {year} {DisplayName}" + Privacy · Voorwaarden · Hulp links. The legal line uses the cached identity only (Dependency B).
- **No** `AuthorizeView` with API data, no `NotificationBell`, no `AppFooter` if it calls the API. Reading `HttpContext.User` claims is fine.
- **Culture:** a small `ErrorCulture.Resolve(HttpContext)` (cookie `Jobsy.Culture` → `Accept-Language` → nl, limited to the 5 supported). It sets `CultureState` for the render.
- **Self-protection:** the layout body is wrapped in a `try` render helper. If rendering throws, the exception handler writes a minimal hard-coded HTML (nl + en, no CSS link) with the status code. Test this path.
- **Headers:** every response through the layout gets `X-Robots-Tag: noindex`, `Cache-Control: no-store` (500/503) or `no-cache` (404/410), and `<meta name="robots" content="noindex">`.
- **CSS:** `errors.css` (`err-` blocks: `err-hero`, `err-code`, `err-actions`, `err-search`, `err-card`, `err-mascot`). The mascot bob respects `prefers-reduced-motion`.

## 01.3 404 (`/status/404`, re-execute target)
- **Dependency B present:** restyle `Pages/Status/StatusPage.razor` in place.
- **Dependency B absent:** create it exactly as public-pages §01.6:
  - `@page "/status/{Code:int}"`
  - the `UseStatusCodePagesWithReExecute("/status/{0}")` filter for HTML requests only (not `/api`, `/_blazor`, `/_framework`, `/_content`, `/healthz`, file extensions)
  - `VacancyDetail` 404 status + noindex when the API returns 404
- **Content** (er-d01):
  - eyebrow chip "404"
  - h1 `Status.NotFound.Title` "Deze pagina bestaat niet"
  - lead `Status.NotFound.Lead` "Misschien is de link oud, of zit er een typfout in. Geen zorgen, we helpen je verder."
  - **Search** "Wat zoek je?" (a GET form): only if the banenkaart accepts a text query (check `Home.razor`/banenkaart for a `[SupplyParameterFromQuery]` text parameter; at `a611db40` there is none). Then submit to it; else leave the field out (§0 known difference).
  - 3 buttons (E3): **Banenkaart** (primary) → `PublicRoutes.Banenkaart` or `/`; **Gratis test** → `/ontdek`; **Hulp** → `/hoe-werkt-lobsy`. With F present and OFF: "Mijn Paspoort" → `/ontdek` replaces Banenkaart.
  - Small line: "Klopt er iets niet? Mail {SupportEmail}."
- The page keeps the original 404 status (re-execute). A direct `GET /status/404` also answers 404. Unknown codes → the 404 page with status 404.

## 01.4 500 (`/Error`) + support code
- **`Pages/Error.razor`:** `@layout ErrorLayout`, `[ExcludeFromInteractiveRouting]`, `[AllowAnonymous]`, response status stays 500.
- **Content** (er-d02):
  - eyebrow "Foutje bij ons"
  - h1 `Status.Error.Title` "Er ging iets mis"
  - lead `Status.Error.Lead` "Het ligt niet aan jou. Probeer het zo nog eens. Lukt het niet? Stuur ons de code hieronder."
  - a code card: label "Foutcode", the code in mono (`LB-7Q3K`), button "Kopieer" (a tiny inline module; without JS the code is selectable text)
  - buttons: **"Probeer opnieuw"** (a link to the original path for GET requests; for POSTs a link to the referring page, or `/`), **"Mail support"** → `mailto:{SupportEmail}?subject=Foutcode%20{code}` (E9, built with a `MailtoLink`-style helper, escaped once)
  - no request id, no path, no exception
- **`Jobsy.Web/Diagnostics/SupportCode.cs`:**
  - `Create()` → `"LB-" + 4 chars` from `23456789ABCDEFGHJKMNPQRSTVWXYZ` (no 0/1/I/L/O/U), `RandomNumberGenerator`
  - `SupportCode.GetOrCreate(HttpContext)` stores the code in `HttpContext.Items`, so one request has one code. The `/Error` page and 04's inline errors call it.
  - the page logs **once** `LogError(exception, "Unhandled error {SupportCode} {RequestId} {PathTemplate} {UserId}")`, where the exception comes from `IExceptionHandlerPathFeature`, `PathTemplate` is the endpoint's route pattern (no query string, no ids when a template is available) and `UserId` is the id claim or null
  - sets the Sentry tag `support_code` and the log scope, using `SentrySdk.ConfigureScope` or the existing Sentry integration
- **API side (small):** `ExceptionHandlingMiddleware` adds `supportCode` (same generator in `Jobsy.Core/Diagnostics/SupportCodeGenerator.cs`, shared) next to `traceId` in ProblemDetails, logs and tags it. 04 shows it in inline errors.
- **POST re-execute:** the handler re-executes as GET for `/Error` (set `ExceptionHandlerOptions.ExceptionHandlingPath` + a small handler that forces `GET` so antiforgery on the Razor endpoint doesn't reject it). Test a POST that throws.
- **Development:** keep the developer exception page (no change), but `/Error` can be opened directly for a visual check (it renders a sample code).
- **Docs:** `docs/support-codes.md`: how support finds a code (Sentry search `support_code:LB-7Q3K`; Render log search), what's logged and what isn't (no query strings, no bodies).

## 01.5 Strings (`UiStringsStatus.cs`)
`Status.NotFound.*`, `Status.Error.*`, `Status.Common.*` (buttons, "Foutcode", "Kopieer", "Gekopieerd", support line), `Status.Nav.*` (header/footer labels), 5 languages. The ar texts in er-m05 are a draft; list pl/ro/ar in `docs/i18n/errors-review.md`.

## 01.6 Tests
- `StatusPagesTests` (WebApplicationFactory):
  - `/bestaat-niet` with `Accept: text/html` → 404, HTML contains `Status.NotFound.Title` and `noindex`, `X-Robots-Tag: noindex`
  - `/api/bestaat-niet` → API 404 without HTML
  - `/img/missing.png` → 404 without HTML
  - `/status/404` direct → 404
  - `/vacancies/{newGuid}` → 404 + noindex
- `ErrorPageTests`:
  - a test endpoint that throws (registered only in the test host) → 500; HTML contains a code matching `^LB-[2-9A-HJKMNP-TV-Z]{4}$`, no exception message, no `TraceIdentifier` value
  - the log contains the same code once
  - a POST that throws also renders `/Error`
- `ErrorLayoutIsolationTests`: rendering `/status/404` and `/Error` with the API client replaced by one that throws on every call → still 404/500 with the full page (proves no data calls).
- `ErrorLayoutFallbackTests`: a forced layout render failure → the minimal HTML, still the right status.
- `ErrorCultureTests`: cookie ar → `dir="rtl"`, ar strings; `Accept-Language: pl` → pl; unknown → nl.
- `SupportCodeTests`: alphabet, length, 10k codes → no forbidden characters.
- bUnit: the 404 has exactly 3 action links with the right hrefs (ON), and OFF with the test double (F).
- `RoutesDocFreshnessTests`, `PageSeoTests`, `BlazorPageRoleAttributesTests`, `LocalizationParityReportTests`, `AssetVersionGuardTests` green.

## Success criteria
- Every unknown HTML page and vacancy answers 404 with the new page; exceptions answer 500 with a support code; neither page makes an API call.
- The pages work without JS, in 5 languages, ar RTL.
- PR body: dependency outcomes A–G, the status/header table, screenshots nl desktop + mobile, ar mobile, and `docs/support-codes.md`.

Done → next: `02-geen-toegang-403.md`.
