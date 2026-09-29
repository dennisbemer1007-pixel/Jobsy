# 01. Public theme shell: PublicLayout, public-theme.css, strings, nav, language switch, switch seam, funnel link fixes

Read `00-README.md` first. Branch `cursor/landing-1` from `origin/acceptatie`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/landing-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - The warm style lives only under `.pub-theme`; the logged-in app (`MainLayout`) must look exactly as before.

| | |
|---|---|
| Branch | `cursor/landing-1` |
| PR title | `feat(landing): public theme shell — PublicLayout, pub- theme, landing strings, SSR language switch, employers switch seam` |
| PR body starts with | `Stacked on: none (first in the stack)` + the Dependencies outcome (A–F) |
| Mockups | all `lp-*` for header/footer/colours; `lp-d1-landing.png` + `lp-d1-landing-zw.png` for nav/footer, `lp-m1-hero.png` for the mobile header |
| Split seam | **01a** = theme CSS + `PublicLayout` + nav/footer + strings + TeaserLayout fixes (01.2, 01.3, 01.6, 01.7). **01b** = SSR culture + `/taal` + `IEmployersSwitch` + static cookie-banner mode (01.4, 01.5, 01.8) |

## Goal
Everything the public pages share exists and is tested before any page is built: a layout with the warm theme, one cookie banner and RTL; a nav/footer that knows ON vs OFF; strings in 5 languages; a language switch that works without a Blazor circuit; and one seam (`IEmployersSwitch`) that later files ask "are employers active?". The candidate funnel links in `TeaserLayout` stop pointing at the company registration.

## 01.1 Today (verify first)
- `Components/Layout/TeaserLayout.razor`: brand `href="/westland"`, hard-coded "Registreren" → `/register`, "Inloggen" → `/login`, `aria-label="Snelle links"`, `<CookieConsentBanner />`, `<FeedbackWidget />`, `dir` RTL. Used by `Pages/Public/GratisDna.razor` and `Pages/WestlandTeaser.razor`.
- `MainLayout.razor` also renders `<CookieConsentBanner OnConsentChanged=…>` (~L80). `Components/CookieConsentBanner.razor` uses `@onclick` + `jobsyCookieConsent.set` with a server-minted analytics token (`MintAnalyticsToken`), so it **needs a circuit**. CSS `.cookie-consent` in `app.css` (~L13055); `html.cookie-consent-known` hides it before paint.
- `Components/App.razor`: `PageRenderMode` is `null` for pages with `[ExcludeFromInteractiveRouting]` (the MFA pages use this); `LoadBlazorRuntime` skips `blazor.web.js` only for crawlers (`CrawlerUserAgent`).
- `Localization/CultureState.cs`: `InitializeAsync` reads the profile language (signed in) or `jobsyCulture.get` via **JS interop**. During static SSR, JS isn't available, so today a static page always renders **nl**. There's no server-side read of the `Jobsy.Culture` cookie.
- `Components/Layout/LanguageSelector.razor` is interactive (`@onclick`).
- `Components/Layout/AppFooter.razor` L10 links `/westland` (werkgevers-actief 01 hides it when OFF; leave it).
- Dependencies check A decides the `IEmployersSwitch` implementation.

## 01.2 Public theme CSS `wwwroot/css/features/public-theme.css`
- Root class `.pub-theme` (on `PublicLayout`'s outer element). Everything is scoped under it; **no bare element selectors** and no selector that can match outside `.pub-theme`.
- Custom properties, all mixed from existing tokens (take the exact mixes from the mockup sources `css_warm.py` and `css_lp.py`):
  - surfaces: `--pub-bg` (cream), `--pub-bg-alt` (sand), `--pub-card`, `--pub-line`
  - accents: `--pub-coral`, `--pub-coral-soft`, `--pub-gold-soft`, `--pub-sky-soft`, `--pub-mint-soft`
  - `--pub-shadow-soft` (coral-tinted), `--pub-radius-card: 24px`, `--pub-radius-scene: 36px`, `--pub-radius-pill: 999px`
  - type: `--pub-text-hero: 3.25rem` (≤ 640: `2.125rem`), `--pub-text-h2: 2.25rem` (≤ 640: `1.75rem`), `--pub-text-result: 3rem`
- BEM blocks (only these in 01; later files add their own `pub-landing__…`, `pub-test__…`, `pub-signup__…`):
  - `pub-header`, `pub-nav`, `pub-menu` (mobile sheet), `pub-footer`, `pub-lang`
  - `pub-btn` (`--primary` navy pill, `--secondary` outline pill, `--ghost`), `pub-chip`, `pub-card`, `pub-eyebrow`, `pub-section` (`--alt` sand background, `--wave-top`/`--wave-bottom` SVG wave edges), `pub-emoji`, `pub-pill-sample` ("Voorbeeld")
- Link it in `App.razor` (normal list **and** `<noscript>`), `?v=YYYYMMDD-public-theme`, add it to `asset-versions.json`.
- Add the **"Public theme (landing, gratis test, public info pages)"** section to `.cursor/rules/design-system.mdc` with the approved deviations list from README §0 (verbatim), the rule "only under `.pub-theme`", and the block list above.

## 01.3 `PublicLayout` (`Components/Layout/PublicLayout.razor`)
- Markup: `<div class="pub-theme" dir="@(rtl ? "rtl" : null)" lang="@lang">` → `<PublicHeader />` → `<main id="main">@Body</main>` → `<PublicFooter />` → **one** `<CookieConsentBanner Mode="…" />` (01.5). A skip link "Naar de inhoud" goes first.
- `<PageSeoHead />` as the TeaserLayout does. There's no `FeedbackWidget` on the landing (performance, D13). `/ontdek` keeps it (06 decides placement).
- Works in **both** render modes: static SSR (landing, 05) and InteractiveServer prerender (`/ontdek`, 06). No `@onclick` in the header/footer; the mobile menu is a `<details>`/`<dialog>` plus `landing.js` enhancement (keyboard: Esc closes, focus returns to the toggle).
- `PublicHeader`: logo (`LobsyLogo`, links "/") · nav items from `PublicNavCatalog` · `pub-lang` language menu (01.4) · "Inloggen" (`/login`) · primary "Doe de gratis test" (`/ontdek`). Mobile ≤ 900: logo + "Inloggen" + menu button (lp-m1-hero).
- `PublicFooter`: columns from `PublicNavCatalog.Footer(variant)`: brand line, "Lobsy", "Voor", "Account", "Juridisch". Bottom row "© {year} Lobsy · gemaakt met 🧡" + current language name.
- **`TeaserLayout`** stays in this file (used by `GratisDna` and `WestlandTeaser`); only its links and culture init change (01.7). 06 moves `GratisDna.razor` to `PublicLayout`; `/westland` keeps `TeaserLayout`. Don't delete it.

## 01.4 Culture on static pages + language switch
- **Server-side culture resolution.** Add `CultureState.InitializeFromRequest(HttpContext)`: `?lang=` (valid `JobsyLanguages` code) > cookie `Jobsy.Culture` > nl. Call it from `PublicLayout` (static SSR and prerender) **before** the JS path, and make `InitializeAsync` keep a value that's already set. Signed-in users never see the landing (D1), so no profile call is needed there.
  - Put `<html lang>` and `dir` into the response for static pages too (today they're set by `SyncDocumentAsync` via JS). Verify in `App.razor` how `lang` is emitted and make it culture-aware for `PublicLayout` pages.
- **`?lang=`** changes only that response (no cookie). It exists for hreflang and crawlers. The canonical never carries `?lang`.
- **`GET /taal/{lang}?returnUrl=/…`**: a Web endpoint (Minimal API, next to the other `/account/*` endpoints in `Auth/AuthServiceCollectionExtensions.cs` or a new `Localization/LanguageEndpoints.cs`). It validates `lang`, sets the `Jobsy.Culture` cookie with the same attributes as `jobsyCulture.set` (check `app-core.js` culture section: name, path `/`, max-age, SameSite), and 302s to `returnUrl` **only if** it's a local path (`Url.IsLocalUrl` semantics, no `//`, no scheme), else "/". `robots` noindex; links carry `rel="nofollow"`.
- `pub-lang` menu: a `<details>` list of 5 links `/taal/{code}?returnUrl={current path+query without lang}` with native names (Nederlands, English, Polski, Română, العربية) and `hreflang`/`lang` attributes. The current language is marked `aria-current="true"`.
- **hreflang helper** `Seo/HreflangLinks.cs`: for a public path, emit `<link rel="alternate" hreflang="nl|en|pl|ro|ar" href="https://…{path}?lang=xx">` + `x-default` (no parameter). Used by `PageSeoHead` for paths flagged `Hreflang = true` in `PageSeoCatalog` (added here, set for "/" in 05, `/ontdek` in 06, `/werkgevers` + `/scholen` in 08).
- No Accept-Language auto-redirect (crawlers and shared links stay predictable). The menu is the way in.

## 01.5 One cookie banner, also on static pages
- `CookieConsentBanner` gets a `Mode` parameter: `Interactive` (today, default) and `Static`.
  - **Static** renders the same markup and classes, with buttons `data-consent="necessary|analytics"` and no `@onclick`. `app-core.js`'s cookieConsent section gets a small delegated click handler: for `necessary` it calls `jobsyCookieConsent.set("necessary")`. For `analytics` it first `POST`s `/account/cookie-consent/analytics-token` (new tiny Web endpoint, antiforgery-exempt but same-origin checked via `Origin`/`Sec-Fetch-Site`, returns the same token `MintAnalyticsToken` makes) and then calls `set(token)`. Then it adds `cookie-consent-known` to `<html>` and removes the banner.
  - Extract `MintAnalyticsToken` into a shared service so both modes produce identical values (test).
- `PublicLayout` picks `Static` when `HttpContext.AcceptsInteractiveRouting()` is false, else `Interactive`.
- **Exactly one banner per page:** `PublicLayout` renders it; `MainLayout` keeps its own; no page may render one itself. Guard test: render every page with `PublicLayout` or `TeaserLayout` and assert one `.cookie-consent` element.
- Public-theme restyle of the banner, scoped `.pub-theme .cookie-consent` (pill buttons, cream card, `--pub-shadow-soft`). Size and paint containment as today (it must not become the LCP element).
- **Mobile collision rule (D15):** every bottom-fixed element in the public theme uses the class `pub-fixed-bottom`, and `public-theme.css` has `html:not(.cookie-consent-known) .pub-theme .pub-fixed-bottom { display: none; }`. Nothing in 01 is fixed-bottom yet; 06 uses it.

## 01.6 Strings + nav catalog
- `Localization/UiStringsLanding.cs` (5 languages) with the shell keys: `PublicNav.*` (Hoe het werkt, Banenkaart, Werkgevers, Scholen, Partners, Mijn Paspoort, Ontdekkingsreis, Inloggen, Doe de gratis test, Menu, Sluiten, Naar de inhoud, Kies je taal), `PublicFooter.*` (column titles, Kandidaten, Jou, Nieuw in Nederland, Account maken, Werkgever registreren, Privacy, Cookies, Algemene voorwaarden, Gebruiksvoorwaarden, Wie zijn wij, the brand line ON/`.Zw`, "gemaakt met"). Register in `UiStrings.cs`.
- `LandingText.For(key, variant)` helper: returns `key + ".Zw"` when the variant is OFF **and** that key exists, else `key`. Unit test.
- `Navigation/PublicNavCatalog.cs` (pure, unit-tested): `Header(LandingVariant)` and `Footer(LandingVariant)` return label keys + hrefs per §V. ON header: `/hoe-werkt-lobsy`, `/banenkaart`, `/werkgevers`, `/scholen`, `/partner`. OFF header: `/hoe-werkt-lobsy`, `/#wat-je-krijgt`, `/#ontdekkingsreis`, `/scholen`. Items whose page doesn't exist yet (`/banenkaart` until 04, `/werkgevers` + `/scholen` until 08) carry `IsAvailable = false` and **aren't rendered**; the file that builds the page flips them.
- `LandingVariant` enum `{ On, Zw }` in `Jobsy.Web/Features/`.

## 01.7 Funnel link fixes in `TeaserLayout` (D16)
- Brand link → "/".
- "Registreren" → label `PublicNav.CreateAccount` ("Account maken"), href `PublicRoutes.CreateAccount` (`/account-maken`). 03 adds that page. Between PR 01 and PR 03 the link 404s on the stack only; say so in the PR.
- Call `CultureState.InitializeFromRequest` (01.4) from `TeaserLayout` too, so `/ontdek` prerenders in the chosen language.
- "Inloggen" and the nav `aria-label` via `@Culture[…]`.
- Add `Jobsy.Web/Navigation/PublicRoutes.cs` with constants: `Landing = "/"`, `Test = "/ontdek"`, `CreateAccount = "/account-maken"`, `CreateAccountFromTest = "/account-maken?van=ontdek"`, `Banenkaart = "/banenkaart"`, `Employers = "/werkgevers"`, `Schools = "/scholen"`, `Partner = "/partner"`, `CompanyRegister = "/register"`, `Login = "/login"`. Later files use only these constants.

## 01.8 `IEmployersSwitch` seam (Dependencies A)
- `Jobsy.Web/Features/IEmployersSwitch.cs`: `ValueTask<bool> IsEnabledAsync(CancellationToken ct = default)` and `ValueTask<LandingVariant> VariantAsync(…)`.
- **A present:** `FeatureFlagsEmployersSwitch` over `IFeatureFlags` (`EmployersEnabled`).
- **A absent:** `AlwaysOnEmployersSwitch` + the follow-up doc `docs/feature-flags-landing-followup.md` (README Dependencies A) + the same 4 lines as an XML comment on the interface.
- **Test/dev override:** config `Landing:ForceVariant` (`on|zw`) and the query `?_variant=on|zw`, both honoured **only** when `IHostEnvironment.IsDevelopment()` (the CI stack runs `ASPNETCORE_ENVIRONMENT=Development`, see `start-ci-stack.sh`). Production and Staging ignore both (unit tests with those host environments), and `_variant` never appears in canonicals, sitemaps or links. This is how Playwright, Lighthouse (10) and screenshots show OFF on one stack, also when A is absent.
- `LandingVariantResolver` (scoped): resolves once per request and caches it in `HttpContext.Items`. Every public component gets the variant from here, never from its own flag read.

## Tests
- `PublicThemeCssGuardTests`: every selector in `public-theme.css` starts with `.pub-theme` or `html…` + `.pub-theme`. No `!important`. No hex/rgb literals (tokens + `color-mix` only). No `pub-` class used in any `.razor` rendered by `MainLayout` (scan `Components/Pages/**` whose layout isn't `PublicLayout`).
- `PublicLayoutBunitTests`: one `.cookie-consent`, skip link first, `dir="rtl"` for ar, `lang` attribute, header items ON vs OFF (`IsAvailable = false` items not rendered).
- `PublicNavCatalogTests` (ON/OFF hrefs per §V; OFF never contains `/banenkaart`, `/werkgevers`, `/register`, `/partner`, `/westland`).
- `LanguageEndpointTests`: valid lang sets the cookie; invalid lang → 400/"/"; `returnUrl=https://evil` / `//evil` / `\\evil` → "/"; `?lang=ar` renders RTL without setting a cookie.
- `CultureStateRequestTests`: `?lang` > cookie > nl.
- `CookieConsentStaticModeTests`: static markup has `data-consent` buttons and no Blazor event attributes; the token endpoint rejects cross-site `Sec-Fetch-Site`; the token is the same format as interactive mode.
- `EmployersSwitchTests`: adapter or AlwaysOn per A; `ForceVariant` ignored in Production.
- `TeaserLayout` bUnit: brand "/", account link `/account-maken`, no `/register`, no `/westland`.
- `LocalizationParityReportTests` green (all 5 languages), `AssetVersionGuardTests`, `RoutesDocFreshnessTests` (the `/taal/{lang}` endpoint in `docs/ROUTES.md`).

## Success criteria
- A throwaway test page (not routed in production; bUnit only) under `PublicLayout` shows the warm header/footer from lp-d1 in nl, en and ar (RTL).
- `MainLayout` pages are pixel-identical in the existing Playwright visual checks (if any). The CSS guard proves `.pub-` can't leak.
- `/taal/en?returnUrl=/ontdek` sets the cookie and `/ontdek` renders in English on first paint (no nl flash).
- `TeaserLayout` no longer links to `/register` or `/westland`.
- PR 01 states which Dependencies case (A–F) applied.

Done → next: `02-mascotte-assets.md`.
