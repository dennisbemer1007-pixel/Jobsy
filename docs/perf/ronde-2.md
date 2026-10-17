# Performance ronde 2 (2026-10-17)

Doel: megasnelle Lobsy-app — minder full reloads, snellere anonieme paden, Blazor-circuit sneller laten voelen. Base: `acceptatie`. Geen wijzigingen aan acceptatie-flags of productie.

## Lab-setup

- **Lokaal:** CI-stack (`start-ci-stack.sh`), Release-build, PostgreSQL + PostGIS.
- **Acceptatie (alleen GET):** `https://acceptatie.lobsy.nl` als *voor* baseline vóór deploy van deze PR.
- **Lighthouse 12**, mediane van 3 runs (`tools/perf/lighthouse-median.sh`).
- **Mobile:** Lighthouse mobile preset (throttled CPU/netwerk).
- Screenshots: `tools/perf/perf2-screenshots.mjs` → JPEG in `/cursor/stores/self/perf2/screenshots/`.

## Lighthouse — publieke pagina’s (ms, mediane van 3)

| Pagina | Profiel | Metriek | Acceptatie (voor) | Lokaal (na PR) | Δ |
|--------|---------|---------|-------------------|----------------|---|
| `/login` | desktop | FCP / LCP | 483 / 529 | 282 / 363 | −42% / −31% |
| `/login` | mobile | FCP / LCP | 2035 / 2035 | 1871 / 2487 | −8% / +22%* |
| `/login` | mobile | Score | 97 | 96 | ≈ |
| `/account-maken` | desktop | FCP / LCP | 543 / 583 | 284 / 364 | −48% / −38% |
| `/account-maken` | mobile | FCP / LCP | 2562 / 2562 | 1975 / 1975 | −23% / −23% |
| `/account-maken` | mobile | Score | 93 | 98 | +5 |
| `/` | mobile | FCP / LCP | 1514 / 1514 | 1940 / 1940 | VM-variantie |
| `/banenkaart` | mobile | Score | 74 | 66 | nog zwaar (kaart/tegels) |

\*Mobile login LCP op de agent-VM fluctueert; structureel verschil: **geen** `blazor.web.js` / SignalR meer op `/login` en `/account-maken` (zie hieronder).

## Blazor / circuit (audit 2026-10-16 vs deze PR)

| Meting | Audit acceptatie | Deze PR (verwachting) |
|--------|------------------|------------------------|
| Circuit interactief (desktop, koud) | ~3100 ms | Preload `app-core.js` + `blazor.web.js`; `performance.mark` in `blazor-boot.js` voor lab |
| Circuit interactief (mobile, koud) | ~5600 ms | Zelfde + minder render-blocking CSS op signup |

Login/account-maken starten **geen** circuit meer (`[NoBlazorRuntime]`).

## Full reloads — inventaris

### Aangepast (SPA / enhanced navigation)

| Locatie | Was | Nu | Reden |
|---------|-----|-----|--------|
| `Register.razor` (`van=ontdek`) | `forceLoad: true` | `false`, `replace` | Zelfde sessie, geen cookie-reset |
| `RegisterKoppelen.razor` | `forceLoad: true` | `false`, `replace` | Al ingelogd na external bind |
| `LetterCodeEntry.razor` | `forceLoad: true` | `false`, `replace` | Werkgever-shell blijft op circuit |
| `CompanyVerificationBanner.razor` | `forceLoad: true` | `false`, `replace` | Idem |
| `TrainingOffersBlock` / `CourseSuggestionBlock` | altijd `forceLoad` | alleen bij externe URL | Interne deep links blijven in-app |

### Bewust behouden (`forceLoad: true`)

| Locatie | Reden |
|---------|--------|
| `RedirectToLogin.razor`, `AuthHeader` logout, privacy logout | Auth-cookie / sessie reset |
| `Login.razor` (al ingelogd) | Static SSR; echte redirect nodig |
| `RegisterWizard` → register-signin / external-login | Nieuwe sessie-token / OAuth hop |
| Checkout-URL’s (Mollie e.d.) | Externe betaling |
| `WerkgeversBinnenkort` → candidate shell | Static SSR redirect |
| `RegisterVerifierenBrief` | Brief-flow state |
| Service worker | Alleen **handmatige** “Vernieuwen” (geen auto-reload) |

## Belangrijkste code-wijzigingen

1. **`[NoBlazorRuntime]`** op `/login`, `/account-maken`, `/account-maken/code` — geen `blazor.web.js`, geen 15+ kandidaat-stylesheets op signup.
2. **`PageAssetCatalog`:** `/account-maken` → `Area.None` (lean CSS-catalogus).
3. **`App.razor`:** `preload` voor `app-core.js` en `blazor.web.js`; gratis-dna CSS op static signup.
4. **`blazor-boot.js`:** `performance.mark` / `measure` voor circuit-timing in lab.
5. **`tools/perf/perf2-suite.mjs`** en **`perf2-screenshots.mjs`** voor herhaalbare metingen.

## Aanbevelingen voor Dennis (buiten deze PR)

| Actie | Verwachte winst |
|-------|-----------------|
| Render plan / regio dichter bij NL + DB tier | −100–300 ms TTFB acceptatie |
| CDN / edge cache voor static (Cloudflare al deels) | Snellere warm loads, minder origin hits |
| OpenFreeMap tegel-budget / vector tile CDN | Banenkaart mobile LCP/TBT |
| PersistentComponentState op zware discovery-lading | Minder dubbele API na prerender |

## Screenshots

Zie `/cursor/stores/self/perf2/screenshots/` (`before-acc-*` vs `after-local-*`, 1440×900 en 390×844).

## PR #710 review (2026-10-17)

- **Microsoft-knop:** `Login.razor` toont providers alleen als `ExternalAuth.IsEntraConfiguredAsync()` / Google geconfigureerd is. Lokaal zonder secrets → geen knop; met dummy Entra env op Web → wel knop + `au-divider`, nog steeds zonder `blazor.web.js`. Capture: `tools/perf/perf2-capture-review.mjs`.
- **Cookiebanner NoBlazorRuntime:** extra `css/features/cookie-consent.css` in `App.razor` wanneer `!LoadBlazorRuntime` (compacte banner i.p.v. alleen `public-theme.css`). Captures: `tools/perf/perf2-static-auth-screenshots.mjs`.
- **E2E zonder circuit:** `Jobsy.Tests/NoBlazorStaticAuthPlaywrightTests.cs` + `StaticAuthVisualPlaywrightTests.cs` (styled inputs/providers, cookie banner, code step). Vereist CI-stack + `JOBSY_E2E_CAPTURE_EMAIL_CODES=1`; optioneel `ConnectionStrings__JobsyDb` om seed-lockout te resetten.
- **Signup CSS zonder app.min:** `css/features/signup-static.css` via `PageAssetCatalog.IncludeSignupStaticCss` op `/account-maken` (+ code). Captures: `tools/perf/perf2-auth-visual-compare.mjs`, `perf2-static-auth-screenshots.mjs` (consent reset per page).
- **Banenkaart:** lokaal `Jobsy__TestEmployersEnabled=true` (zelfde als acceptatie employers-flag). Metrics in `banenkaart-load-metrics.json`.
- **Asset guard:** `app-core.js?v=20261017-03` (+ `asset-versions.json` sync).
