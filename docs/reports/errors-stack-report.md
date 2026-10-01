# Error pages stack — end report (errors 01 … 06)

What the stack built, what it proved, and what is still waiting for Dennis. This is §06.5 of
`docs/prompts/errors/06-e2e-rapport.md` and is repeated in the PR body of `cursor/errors-6`.

**In one paragraph:** every failure a visitor can run into now has a calm Lobsy page in their own
language with a real HTTP status code. A page that does not exist says so and offers three ways on;
a job that closed shows up to three similar ones nearby; something breaking on our side gives a
short code to quote to support; planned maintenance says when we expect to be back. None of these
pages call the API or the database to render, so they still work when the thing that broke is the
API or the database. Nothing in Cloudflare, Render or `render.yaml` was changed — those are
Dennis's decisions and they are listed at the bottom.

## 1. The six pull requests

| # | Branch | PR | State | Own commits |
|---|---|---|---|---|
| 01 | `cursor/errors-1` | [#493](https://github.com/dennisbemer1007-pixel/Jobsy/pull/493) | open | `bb7791ff` `e218fe61` `2e5d293b` `d7022a46` |
| 02 | `cursor/errors-2` | [#494](https://github.com/dennisbemer1007-pixel/Jobsy/pull/494) | open | `bf408328` |
| 03 | `cursor/errors-3` | [#495](https://github.com/dennisbemer1007-pixel/Jobsy/pull/495) | open | `ab804c05` |
| 04 | `cursor/errors-4` | [#496](https://github.com/dennisbemer1007-pixel/Jobsy/pull/496) | open | `a7700235` `99e65bff` `c55c5179` |
| 05 | `cursor/errors-5` | [#497](https://github.com/dennisbemer1007-pixel/Jobsy/pull/497) | open | `7e3942ae` `3592c16d` `c4b73fbb` `760825df` |
| 06 | `cursor/errors-6` | this PR | open | `1fee8b1b` |

The branches are stacked, so each diff contains the ones below it until they merge. Merge them in
order 01 → 06; `acceptatie` is the base for all six and none of them touches `main`.

One migration in the whole stack: `AddMaintenanceMode` (05).

## 2. The contract, as it now answers

| Case | Status | Headers | Page |
|---|---|---|---|
| Unknown page (HTML) | 404 | `X-Robots-Tag: noindex`, `no-store` | 404 with three actions |
| Unknown or never-public vacancy | 404 | idem | the same 404 content, inside the vacancy page |
| Closed vacancy (archived, fulfilled, expired) | 410 | idem | title + city + max 3 similar nearby |
| Unhandled exception | 500 | idem | 500 with support code `LB-XXXX` |
| Signed in, missing role | 403 **at the requested URL** | idem | Geen toegang + account + two actions |
| Rate limited (HTML) | 429 | `Retry-After` + idem | Even rustig aan, with the wait |
| Rate limited (API) | 429 | `Retry-After` | ProblemDetails `rate_limited` |
| Maintenance (non-admin HTML) | 503 | `Retry-After` + idem | Onderhoud, in the visitor's language |
| Maintenance (non-admin API) | 503 | `Retry-After` | ProblemDetails `maintenance` |
| Closed vacancy (API) | 410 | noindex | minimal public fields + `code: "vacancy_closed"` |
| `/api/*`, `/_blazor`, `/_framework`, statics | unchanged | — | never an HTML page |
| `/healthz`, API `/health` | 200 | — | unchanged, **including during maintenance** |

## 3. Dependency outcomes (A–G), at every re-check point

| | Dependency | 01 | Re-check | Outcome |
|---|---|---|---|---|
| A | Public theme (landing 01/02/04) | present | 03, 05: present | `ErrorLayout` uses `.pub-theme`, `pub-*`, `LobsyMascot`, `PublicRoutes` |
| B | Public-pages hotfix | **absent** | — | 01 created `StatusPage.razor`, `UiStringsStatus` and the re-execute filter; public-pages must reuse them |
| C | Emails (`AmsterdamTime`) | present | — | the maintenance end time uses it |
| D | Admin redesign | present | 05: present | the switch is an `AdminToggleRow` with a danger impact note on `/admin/instellingen` |
| E | Auth 03 (`/account/switch`) | **absent** | 02: absent | 02 added `reason=switch` to `/account/logout`, redirecting to `/login?returnUrl=` through `AuthRedirects.SafeLocalUrl` |
| F | Werkgevers actief | present | 02, 03: present | 404 follows E3 OFF, 403 handles `reason=employers-off`, 410 is unreachable with employers off |
| G | Tests stack | n/a | — | the 04 ratchet baseline picked up their progress automatically |

Both absent cases have a follow-up line in `docs/errors-followups.md`; neither blocked anything.

## 4. What the tests prove

### Browser matrix — `StatusPagesPlaywrightTests`

The matrix is **6 status pages × 5 languages × 2 widths = 60 page checks**, plus **8 no-JS checks**
(404/500/410/503 in nl and ar). Per page: the status on the document response, `noindex`, exactly
one `<h1>`, `lang` equal to the culture, `dir="rtl"` only for Arabic, no horizontal overflow at
390 px, body text ≥ 16 px, primary buttons ≥ 44 px high, a support code on 500 and 429, and no
"Exception", "at Jobsy.", "System.", GUID-shaped id or `[PLACEHOLDER]` anywhere in the HTML.
Screenshots land in `artifacts/playwright-errors/` as `{code}-{variant}-{lang}-{width}.png`.

Flows: 403 at the requested URL with a switch-account control, the employers-off reason, the 410
page's alternatives (each opening a real 200) versus the search CTA when nothing is nearby, the
maintenance switch, the reconnect toast in nl and ar, an intercepted API call turning one card into
an inline error while the rest of the page lives on, and the copy-code button.

**This suite soft-skips without `JOBSY_E2E_BASE_URL`**, like every other browser suite in the repo,
and rows whose precondition is missing skip themselves and append the reason to
`artifacts/playwright-errors/skipped.txt`. In this environment there is no running stack, so the
whole class soft-skipped; it is registered in **both** filter lists of
`.github/workflows/pr-tests.yml` so CI runs it against the seeded stack on `127.0.0.1:5201`.

Rows that skip even with a stack, and what covers them instead:

| Row | Needs | Covered in process by |
|---|---|---|
| 500 | `Errors__EnableTestThrow` + `Errors__ForceHandler` (Development only; set by `start-ci-stack.sh`, never on Render) | `ErrorPageTests`, `StatusPagesHttpTests` |
| 410 | `JOBSY_E2E_CLOSED_VACANCY_ID` — the seed has no guaranteed-closed vacancy | `ClosedVacancyPageTests`, `ClosedVacancyApiTests` |
| 403 | a candidate session | `ForbiddenStatusTests`, `ForbiddenViewTests` |
| Maintenance | an **admin** session — admins need MFA since auth 02, so a scripted login cannot get in | `MaintenanceMiddlewareTests`, `MaintenanceApiTests` |
| Reconnect toast | a circuit that actually drops | `ReconnectToastTests` |
| Inline error | a card that already uses `InlineErrorBlock` | `InlineErrorBlockTests` |
| 429 | uses the direct `/status/429` route; exhausting the live limiter would need a CI-only permit value that does not exist yet | `RateLimitPageTests` (exhausts the real limiter) |

### HTTP guards — `StatusPagesHttpTests`

Table-driven over `path × Accept (html/json) × signed-in (no/yes)`: **30 tests, all green.** They
assert the real status code, `Content-Type`, `Cache-Control: no-store`, `X-Robots-Tag` plus the
meta noindex, and `Retry-After` on 429/503. Also: unknown vacancy 404 versus closed 410 in HTML,
the API's 410 `code: "vacancy_closed"`, JSON 404 for `/api/...` with never any HTML,
`/_framework/x.js` and `/css/missing.css` staying plain 404s, `/status/999` falling back to the 404
page, `/healthz` answering 200 during maintenance, the sitemap excluding `/status/`,
`/access-denied` and closed vacancies, no `forceLoad` navigation to `/access-denied` left in the
source, and no hard-coded Dutch in `App.razor`'s reconnect markup.

### Regression

`dotnet build Jobsy.sln` clean. The suites named in 06's verification step —
`StatusPages`, `NoRawException`, `Maintenance`, `LocalizationParity`, `RoutesDoc`, `AssetVersion`,
`PageSeo` — are **150 tests, all green**, and the full `Jobsy.Tests` suite is green.

### `ex.Message`

`docs/errors/ex-message-baseline.txt`: **377 uses at `acceptatie`** (measured by 04) → **322 uses
over 109 files** at the end of the stack. The error pages, the public pages, the layouts and the
pages 04 swept are at 0 and the ratchet only lets counts shrink. The baseline file doubles as the
to-do list for whoever touches one of those pages next.

### Screenshot artifact

CI uploads **`playwright-screenshots`**, which now includes `artifacts/playwright-errors/**`
alongside the existing smoke, DNA and e-mail folders.

## 5. Product fixes 06 made (the tests found them)

1. **An unknown vacancy showed a bare line, not the 404 page.** `/vacancies/{unknown}` answered a
   correct 404 but rendered "Vacature niet gevonden." inside the normal app shell — no heading, no
   way on, nothing like the page §IA promises. The 404 hero moved into
   `Components/Errors/NotFoundView.razor` (the same pattern 03 used for 410) and is now shared by
   `/status/404` and `VacancyDetail`.
2. **The API's 410 had no machine-readable reason.** `ClosedVacancyDto` now carries
   `code: "vacancy_closed"` next to the minimal public fields.
3. **There was no way to see the real 500 page in a browser.**
   `ErrorPagesExtensions.UseTestThrowPath` adds `/__test/throw`, gated on both
   `Errors:EnableTestThrow` and a Development host — off by default, never set on Render.

## 6. What Dennis still has to decide or do

1. **Cloudflare plan.** Is `lobsy.nl` on Pro or higher, so Custom Error Rules are available, or on
   Free? The checked facts and the three Free fallbacks (Worker / Render only / nothing) are in
   `docs/onderhoud.md` §2, with a checklist. The important catch: **Cloudflare Error Pages do not
   apply to 500/501/503/505**, so the classic "5XX Errors" page would never fire on Lobsy's own
   503 — a Custom Error Rule is needed, and that is paid-plan only.
2. **Render maintenance URI.** Host `ops/maintenance/index.html` **outside** the web service (a
   Render static site or Cloudflare Pages) and set that absolute URL as the maintenance `uri`, in
   the dashboard or in the blueprint. `render.yaml` was deliberately left untouched;
   `docs/onderhoud.md` §3 has the click path and the YAML snippet.
3. **Cloudflare error page upload** — only if the plan is paid. Upload
   `ops/maintenance/cloudflare-500.html` via its public URL and match on
   `http.response.code in {500 502 503 504}`. The mandatory `::CLOUDFLARE_ERROR_500S_BOX::` token
   is already in the file, exactly once.
4. **`support@lobsy.nl` must exist and be read.** Every status page points there with the support
   code pre-filled in the subject line. If the address should be different, set `Support:Email` —
   the pages pick it up from configuration.
5. **Native review of pl, ro and ar.** `docs/i18n/errors-review.md` lists every `Status.*` key with
   its Dutch source; nl and en are final, the other three are B1 drafts. The five-language
   maintenance page in `ops/maintenance/` holds the same text as literal HTML, so a correction has
   to be made in both places.

## 7. Out of scope / deferred

Full list in `docs/errors-followups.md`. The ones worth knowing:

- **public-pages 01** must reuse `StatusPage.razor` and `UiStringsStatus` instead of recreating
  them, and switch `ErrorChromeProvider` to `LegalIdentityProvider.TryGetCached()` once that
  provider exists — the error pages may never fetch it.
- **auth 03** should point `AccessDeniedView`'s switch-account form at its own path if it adds one,
  keeping the `returnUrl` round trip local-only.
- **The 404 search field is intentionally missing.** The banenkaart has no free-text query
  parameter yet; when it gains one, add the GET form.
- **Panels still use `PanelErrorBoundary`.** `InlineErrorBlock` is the intended replacement and can
  be adopted per stack.
- **`JobsyApiClient` still throws `InvalidOperationException(ExtractMessage(body))`** on most
  methods, which keeps the API body in reach of a page. Migrating a method means switching its
  callers to `UserFacingError` in the same change.
- **A seeded closed vacancy and a CI-only limiter permit value** would let the last two browser
  rows run without environment variables.
