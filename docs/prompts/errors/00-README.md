# Lobsy error pages: one calm ErrorLayout for 404/500/403/410/429/503, support codes, maintenance mode, 5 languages incl. RTL (Cursor run book)

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

> **Rules (repeated in every file):**
> - Branch from `origin/acceptatie` (01) or from the previous file's branch (stacked). ONE PR per file into `acceptatie`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/errors-*` branch; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Error pages never show `ex.Message`, stack traces, request ids, paths of internal files, or internal ids. They never call the API or the database to render.
> - Never change Cloudflare or Render settings (dashboard, `render.yaml` deploy settings). Maintenance files are prepared in the repo; Dennis switches them on.

**What this stack builds.** Dennis approved the phase-1 review (`er-*` mockups) and all proposals on 30-09 ("Akkoord").
- **`ErrorLayout`:** a light version of the warm public layout: logo, language, a wave, the mascot, a small footer.
  - Static SSR: no Blazor circuit, **no API or DB calls**, noindex.
  - 5 languages (nl, en, pl, ro, ar; ar right-to-left).
- **404** "Deze pagina bestaat niet": a search field "Wat zoek je?" and three buttons (Banenkaart · Gratis test · Hulp). Every unknown page, vacancy and `/{kvk}` gets a **real 404**.
- **500** "Er ging iets mis": an apology, "Probeer opnieuw", and a **support code `LB-XXXX`** that support can look up (Sentry tag + log scope), plus a mail link to support with the code filled in. Never exception text.
- **403** "Geen toegang":
  - a **real 403 status** on the URL you asked for (no redirect to `/access-denied`), and the return URL is kept
  - "Je bent ingelogd als {naam/e-mail}"
  - buttons "Naar mijn start" and "Inloggen met een ander account" (logs out, then login with `returnUrl`)
- **410** "Deze vacature is gesloten": the job title and the city, plus **max 3 similar vacancies nearby**.
- **429** "Even rustig aan" with the wait time (`Retry-After`). The reconnect toast and the inline "Dit stukje laadt niet" block are translated.
- **No `ex.Message` to users:** a helper plus a ratchet guard over the whole Web project.
- **Maintenance:**
  - an **admin switch** gives 503 + `Retry-After` for everyone except admins, with an admin banner
  - a **static 5-language page** (no scripts) for Render maintenance mode and, depending on the **Cloudflare plan check** (05.1), for Cloudflare's 5xx page or a Worker

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-errorlayout-404-500.md`: `ErrorLayout` (static, noindex, 5 languages, RTL, no data calls), `UiStringsStatus`, `/status/{code}` restyled (or created), friendly 404 with search + 3 buttons, 500 via `/Error` with `SupportCode` (`LB-XXXX`, Sentry tag + log scope), `UseStatusCodePagesWithReExecute`, 404 status for unknown vacancies (if not done by public-pages 01) | `cursor/errors-1` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-geen-toegang-403.md`: real 403 at the requested URL (no forceLoad redirect), account shown, "Naar mijn start" + "Inloggen met een ander account" (logout → login with `returnUrl`), `reason=` variants (employers-off), `/access-denied` kept as a 403 page | `cursor/errors-2` | `cursor/errors-1` | `acceptatie` |
| 03 | `03-vacature-gesloten-410.md`: API tells closed from unknown (410 + minimal public data), `GET api/vacancies/{id}/similar?limit=3`, 410 page with max 3 similar nearby, noindex, sitemap unaffected | `cursor/errors-3` | `cursor/errors-2` | `acceptatie` |
| 04 | `04-429-reconnect-meldingen.md`: 429 HTML page + Retry-After (Web) and ProblemDetails (API), reconnect toast in 5 languages, `InlineErrorBlock` with support code, `UserFacingError` helper, ex.Message ratchet guard (`docs/errors/ex-message-baseline.txt` may only shrink) and the public/error pages at 0 | `cursor/errors-4` | `cursor/errors-3` | `acceptatie` |
| 05 | `05-onderhoud.md`: maintenance switch in admin (migration `AddMaintenanceMode`), Web + API 503 middleware with allow-list and admin bypass, admin banner, `ops/maintenance/` static page (5 languages, no scripts) + Cloudflare variant, `docs/onderhoud.md` with the Cloudflare plan check and the Render maintenance-mode steps | `cursor/errors-5` | `cursor/errors-4` | `acceptatie` |
| 06 | `06-e2e-rapport.md`: Playwright E2E for every status page (desktop + mobile, 5 languages, RTL, no-JS), status-code and header guards, maintenance flow, docs, stack-end report | `cursor/errors-6` | `cursor/errors-5` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations/resources/snapshots), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/errors-4b`).

## Pointer prompt (the only prompt needed; it runs 01 … 06)
```
Run the Lobsy error-pages stack. First: git fetch origin && git show origin/docs/errors:docs/prompts/errors/00-README.md — read it completely.
Then read and execute each file in docs/prompts/errors/ on that branch strictly in the order the README's table lists (01 … 06; a/b splits where a file allows it), one file = one PR.
File 01 branches from origin/acceptatie; every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>" (01: "Stacked on: none (first in the stack)").
Before 01, run the dependency checks in the README's "Dependencies" section and follow the fallback for each; say in PR 01 which case applied. Re-run the checks the README names at 02, 03 and 05.
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Error pages never call the API or DB to render, never show ex.Message, request ids or internal ids, and are noindex. Never change Cloudflare or Render settings: prepare files and docs only.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes.
At the end report: file → branch → PR number → status, the dependency cases, the Cloudflare plan decision Dennis must take, what Dennis must switch on himself (Render maintenance URI, Cloudflare error page or Worker) and anything deferred.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/i18n/README.md`, `docs/release-flow.md` and `SECURITY.md`.
2. Run the **Dependencies** checks below and note the outcome (it goes into PR 01).
3. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column:
      - File 01: `git checkout -b cursor/errors-1 origin/acceptatie`.
      - Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, plus the Playwright suites the file names if you can. Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. The body starts with `Stacked on: none (first in the stack)` (01) or `Stacked on #<prev PR> (<prev branch>)` (02+), then the PR body items from §0.
   6. Note the PR number, go on to the next file.
4. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely. Push what you have, open that PR as **draft** with the failure described, and don't continue.
5. At the end, report the table file → branch → PR number → status (green or draft/red), plus anything deferred.
6. **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: only 05 adds one (`AddMaintenanceMode`). Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you (or a dependency check flips at a re-check point), `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches stack** (see above). **ONE PR per file, always into `acceptatie`.** The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
- **Stop on red.** `dotnet build` + `dotnet test` after each file. If red and not fixable in scope: stop, push, open a draft PR with the failure, report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST). Re-check line numbers before editing.
- **Mockups:** branch `docs/errors`, folder `docs/mockups/errors/`. Read with `git fetch origin docs/errors && git show origin/docs/errors:docs/mockups/errors/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - `er-d00-huidig.png`: today's Error, AccessDenied and the blank 404, for comparison.
  - Desktop 1440 (1x) / mobile 390 (2x):
    - `er-d01-404` / `er-m01-404`
    - `er-d02-500` / `er-m02-500`
    - `er-d03-geen-toegang` / `er-m03-geen-toegang`
    - `er-d04-onderhoud` / `er-m04-onderhoud` (in-app + static 5-language variant)
    - `er-d05-vacature-gesloten` (410)
    - `er-m05-404-arabisch-rtl`
    - `er-d06-kleine-meldingen` (reconnect toast, 429, inline block error)
    - (all `.png`)
  - HTML in `html/` (self-contained). The builder `build.py` needs `docs/mockups/public-pages/` (branch `docs/public-pages`) and `docs/mockups/landing/` (branch `docs/landing`) next to it. The review is `docs/mockups/errors/REVIEW.md`.
  - Grey "Notities" blocks and "Voorbeelddata" pills are mockup-only.
  - **Where a mockup and this spec differ, this spec wins.** Known differences:
    - **Support code format** in the mockups ("LB-7Q3K") is the format; the value is random per error (01.4).
    - **"Naar de banenkaart"** targets `PublicRoutes.Banenkaart` when landing 04 landed, else `/`.
    - **404 search field** submits to the banenkaart search when it has a text query parameter (01.3), else it's left out.
    - **410 similar vacancies** are max 3 and may be fewer or none (03.4).
    - **Maintenance static page** is one HTML file with all 5 languages stacked; the in-app page shows only the current language.
- **Design.** `ErrorLayout` uses the public theme (Dependency A): the `pub-` primitives inside `.pub-theme`, or the `pp-`/own tints fallback. New CSS in `wwwroot/css/features/errors.css` (BEM prefix `err-`, scoped under the layout root), linked in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-x`, added to `Jobsy.Tests/asset-versions.json`. Never append to `app.css`. No inline `style=""`, no `!important`, logical properties only, breakpoints 640/900/1024, tap targets ≥ 44 px, visible focus, AA contrast, `prefers-reduced-motion` (the mascot bob stops).
- **Error pages never depend on the thing that broke:**
  - no API or DB calls, no interactive render mode, no `AuthorizeView` needing API data
  - the account name on 403 comes from the auth cookie's claims only
  - the legal footer line uses `LegalIdentityProvider`'s cached value only (never fetches; `TryGetCached`)
  - `ErrorLayout` catches its own rendering exceptions and falls back to a minimal inline HTML string
- **Strings:** all text via `@Culture["…"]` in `Localization/UiStringsStatus.cs` (prefix `Status.`; created by `docs/public-pages` 01 or here in 01, Dependency B). Every key in **nl, en, pl, ro, ar** from the file that adds it (`LocalizationParityReportTests` green). nl and en are final; pl/ro/ar are B1 drafts listed in `docs/i18n/errors-review.md`. `docs/i18n/untranslated-baseline.txt` may not grow. The culture is resolved from the `Jobsy.Culture` cookie, else `Accept-Language`, else nl, with no API call. The static maintenance HTML is the only place with literal text (5 languages).
- **Tone:** B1, "je", short. Calm, no blame ("Er ging iets mis bij ons", never "Je deed iets fout"), no technical words (no "server", "exception", "HTTP", "request").
- **SEO:** every error/status response has `<meta name="robots" content="noindex">` and `X-Robots-Tag: noindex`, and is never in the sitemap. Status codes are real (404, 410, 403, 429, 500, 503); never 200 with an error text.
- **Docs and guards to update when routes change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (`PageSeoTests`), `Help/PageHelpDocs.cs` (`PageHelpDocsTests`), `BlazorPageRoleAttributesTests`, `CHANGELOG.md`.
- **Must NOT touch:**
  - the logged-in `MainLayout` design (only its ErrorBoundary content changes in 04)
  - `LocalAuthCredential`, lockout, MFA pages, `MfaEnforcementMiddleware` (only reuse)
  - `VacancyDiscovery` internals, `jobMap*.js`
  - Cloudflare/Render settings (only files + docs)
  - other stacks' in-progress branches (`cursor/landing-*`, `cursor/public-*`, `cursor/auth-*`, `cursor/admin-redesign-*`, `cursor/emails-*`, `cursor/werkgevers-actief`): never branch from or merge them
- **PR description:**
  - what changed and why
  - screenshots desktop 1440 + mobile 390 (nl) of each changed page, plus ar mobile
  - the status code / header table for the routes the file touches
  - test list
  - "Out of scope / deferred"
- **Playwright in CI:** every new Playwright test class goes into **both** filter lists in `.github/workflows/pr-tests.yml` (excluded from the unit step, included in the smoke step), like the existing classes.

---

## §IA. Status pages and routes (the contract for all files)

| Case | Status | Headers | Page | Built in |
|---|---|---|---|---|
| Unknown page (HTML request) | 404 | `X-Robots-Tag: noindex` | 404 (search + 3 buttons) via re-execute `/status/404` | 01 |
| Unknown / not public vacancy `/vacancies/{id}` | 404 | noindex | 404 | 01 (if not done by public-pages 01) |
| Closed vacancy (was public: archived, fulfilled, expired) | 410 | noindex | 410 + max 3 similar | 03 |
| `/{kvk}` not public | 404 | noindex | 404 | public-pages 01/09 (uses this layout when present) |
| Unhandled exception | 500 | noindex, `Cache-Control: no-store` | 500 with support code via `/Error` | 01 |
| Signed in, missing role | 403 at the requested URL | noindex, `no-store` | Geen toegang (account + 2 actions) | 02 |
| `/access-denied` (direct or legacy, `?reason=`) | 403 | noindex | same page | 02 |
| Not signed in, protected page | 302 → `/login?returnUrl=` (unchanged) | — | — | — |
| Rate limited (HTML) | 429 | `Retry-After`, noindex | 429 "Even rustig aan" | 04 |
| Rate limited (API) | 429 | `Retry-After` | ProblemDetails `{ code: "rate_limited", retryAfterSeconds, supportCode? }` | 04 |
| Maintenance on (non-admin HTML) | 503 | `Retry-After`, noindex, `no-store` | Onderhoud (current language) | 05 |
| Maintenance on (non-admin API) | 503 | `Retry-After` | ProblemDetails `{ code: "maintenance" }` | 05 |
| App down / deploy (Render/Cloudflare) | 502/503 from the edge | — | `ops/maintenance/index.html` (after Dennis switches it on) | 05 |
| `/api/*`, `/_blazor`, `/_framework`, static files | unchanged | — | no HTML status page | 01 |

Routes: `/status/{code:int}` (re-execute target; direct GET answers with that code for 404/410/429/500/503 and 404 otherwise), `/Error` (exception handler), `/access-denied` (403). All `[AllowAnonymous]`, `[ExcludeFromInteractiveRouting]`, `@layout ErrorLayout`, noindex.

## Decisions (Dennis "Akkoord" 30-09; extra defaults marked *extra*)
- **E1. Maintenance:** an admin switch in the app (503 + `Retry-After`, admins pass) **and** a static page for the edge. The Cloudflare plan is checked first (05.1). *(Dennis, 30-09)*
- **E2. Support code** `LB-` + 4 characters (Crockford base32 without I, L, O, U), random per error. Shown on 500, inline errors and 429. It is logged with the request id, the path template (no query) and the user id (if any), and set as Sentry tag `support_code`. *(Dennis, 30-09)*
- **E3. 404 buttons:** Banenkaart · Gratis test · Hulp, plus a search field. *(Dennis, 30-09)*
  - *extra:* "Hulp" → `/hoe-werkt-lobsy`
  - *extra:* when werkgevers actief is OFF, "Banenkaart" becomes "Mijn Paspoort" (`/ontdek` for anonymous)
- **E4. Closed vacancy:** a page with max 3 similar vacancies nearby, status 410. *(Dennis, 30-09)*
- **E5. Languages:** 5 languages, ar RTL, from the cookie/`Accept-Language` without API calls. *(Dennis, 30-09)*
- **E6. 403** keeps the requested URL and status. "Inloggen met een ander account" = logout, then `/login?returnUrl={requested}`. *(Dennis, 30-09)*
- **E7. No `ex.Message` to users** anywhere. A ratchet guard makes the count only go down. The public and error pages are at 0. *(Dennis, 30-09)*
- **E8. Maintenance defaults** *extra*: `Retry-After` = seconds until the expected end (min 60, max 3600), else 300. The admin sets an optional expected end time (Europe/Amsterdam), and the page shows "We verwachten terug te zijn om {HH:mm}" only when it's set. `/healthz` always answers 200 (Render health checks).
- **E9. Support mail** *extra*: "Mail support" is `mailto:{SupportEmail}?subject=Foutcode {code}` (from `LegalIdentityProvider` cache, default `support@lobsy.nl`).

## Dependencies (check before 01; say in PR 01 which case applied)
- **A. Public theme (`docs/landing` 01, 02, 04).** Check: `git grep -n "class PublicRoutes\|class LobsyMascot" origin/acceptatie -- Jobsy.Web` and `git ls-tree -r --name-only origin/acceptatie -- Jobsy.Web/Components/Layout/PublicLayout.razor Jobsy.Web/wwwroot/css/features/public-theme.css`.
  - **Present:**
    - `ErrorLayout` wraps its content in `.pub-theme` and uses `pub-btn`, `pub-card`, `pub-chip` and `LobsyMascot` (pose Waving on 404/403/410, Sitting on 500/503/429 if the poses exist)
    - links come from `PublicRoutes` (`Banenkaart`, `GratisTest`, `HowItWorks`)
    - it does **not** reuse `PublicLayout` itself when that layout makes data calls (check). Reuse only its header/footer pieces that don't.
  - **Absent:**
    - `ErrorLayout` has its own `err-` header (logo → `/`, language links via the existing culture switch) and footer
    - the warm tints come from `color-mix()` on existing tokens (recipes in `docs/mockups/public-pages/pub_ui.py`), the mascot from today's `BrandImages.MascotWebp*`, the links are today's routes (`/`, `/ontdek`, `/hoe-werkt-lobsy`)
    - add **`docs/errors-followups.md`**: "landing 01/02/04: ErrorLayout → `.pub-theme` + `pub-*`, `LobsyMascot`, `PublicRoutes`." Never create landing's names.
  - Re-check at 03 and 05.
- **B. Public-pages hotfix (`docs/public-pages` 01).** Check: `git grep -n "class LegalIdentityProvider\|UseStatusCodePagesWithReExecute\|UiStringsStatus" origin/acceptatie -- Jobsy.Web`.
  - **Present:**
    - `/status/{code}` (`Pages/Status/StatusPage.razor`), the re-execute filter and `UiStringsStatus` exist: restyle **in place** (keep route, file and keys; add keys)
    - the vacancy 404 already exists: don't redo it
    - the footer legal line uses `LegalIdentityProvider.TryGetCached()` (add the method if it only has an async getter)
  - **Absent:**
    - 01 creates `UiStringsStatus.cs`, `StatusPage.razor` and the re-execute filter exactly as public-pages 01 §01.6 describes (same names), and the vacancy 404
    - the footer shows only "© {year} Lobsy"
    - add a follow-up line: "public-pages 01: reuse errors 01's StatusPage/UiStringsStatus; don't recreate."
- **C. Emails (`docs/emails` 01/02).** Check: `git grep -n "class AmsterdamTime\|interface ITransactionalMailer" origin/acceptatie -- Jobsy.Core Jobsy.Infrastructure`. **Present:** the maintenance end time uses `AmsterdamTime`. **Absent:** add `Jobsy.Core/Time/AmsterdamTime.cs` with the same API emails 01 describes (`ToLocal`, `FormatDate`, `FormatDateTime`). No mails in this stack.
- **D. Admin redesign (`docs/admin-redesign` 01/05/07).** Check: `git grep -n "class AdminNav\b\|AdminNav.cs\|AdminToggleRow\|interface IAdminAuditLog" origin/acceptatie -- Jobsy.Web Jobsy.Core`.
  - **Present:** the maintenance switch is an `AdminToggleRow` in "Platforminstellingen → Functies" (`/admin/instellingen`) with an `AdminImpactNote` (danger): "Iedereen behalve admins ziet de onderhoudspagina." Changes write `IAdminAuditLog`.
  - **Absent:** a section "Onderhoud" at the top of today's `/admin/settings` (`SettingsAdmin.razor`) and a `PlatformLog` `maintenance.on|off` row. Follow-up line.
  - Re-check at 05.
- **E. Auth (`docs/auth` 03).** Check: `git grep -n "AuthPublicLayout\|/account/switch" origin/acceptatie -- Jobsy.Web`.
  - **Present:** "Inloggen met een ander account" uses auth's switch/logout path if it has one; the login page it lands on is auth's redesign.
  - **Absent:** 02 adds `reason=switch` handling to `/account/logout` (`AuthServiceCollectionExtensions.cs` ~L527): sign out as today, then redirect to `/login?returnUrl={safe local}` (via `AuthRedirects.SafeLocalUrl`). Follow-up line for auth.
  - Re-check at 02.
- **F. Werkgevers actief (`docs/mijn-paspoort` 01 / landing `IEmployersSwitch`).** Check: `git grep -n "interface IEmployersSwitch\|interface IFeatureFlags\|employers-off" origin/acceptatie -- Jobsy.Core Jobsy.Web`.
  - **Present:** 404 buttons follow E3 OFF; 403 handles `reason=employers-off` with its own copy; 410 is unreachable when OFF (vacancies 302 to `/`).
  - **Absent:** ON copy only; follow-up line.
  - Re-check at 02 and 03.
- **G. Tests stack (`docs/tests` 01).** It removes `ex.Message` from the test pages. The 04 ratchet baseline is computed from `acceptatie` at 04 time, so it includes their progress automatically. Nothing to check.
- **Recommended landing order:** `docs/public-pages` 01 first if it's close (it adds the simple 404 this stack restyles), landing 01/02 before 01 if close. Not required: every case has a fallback. Never branch from an unmerged branch of another stack.
