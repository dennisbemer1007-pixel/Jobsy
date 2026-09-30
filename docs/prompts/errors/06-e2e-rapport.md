# 06. Playwright E2E for every status page (desktop/mobile, 5 languages, RTL, no-JS), status/header guards, flows, docs, stack-end report

Read `00-README.md` first (§0, §IA, Decisions, Dependencies). Branch `cursor/errors-6` from `cursor/errors-5`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/errors-6` from `cursor/errors-5` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/errors-5)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/errors-6`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - This file adds tests, docs and the report. Product code changes only to fix what these tests find (each fix named in the PR). Every new Playwright class goes into **both** filter lists in `.github/workflows/pr-tests.yml`.

| | |
|---|---|
| Branch | `cursor/errors-6` |
| PR title | `test(errors): Playwright E2E for all status pages (desktop/mobile, 5 languages, RTL, no-JS), status/header guards, maintenance/403/410 flows, docs + stack report` |
| PR body starts with | `Stacked on #<PR 05> (cursor/errors-5)` + the stack-end report (06.5) |
| Mockups | all `er-*` (screenshots are compared by eye in the PR, not pixel-diffed) |
| Split seam | **06a** = Playwright (06.1–06.2); **06b** = HTTP guards + docs + report (06.3–06.5) |

## 06.1 Playwright page matrix (`StatusPagesPlaywrightTests`)
Run against the CI stack (`.github/scripts/start-ci-stack.sh`, `http://127.0.0.1:5201`). The seed must include:
- one Active public vacancy
- one closed vacancy (`ClosedAtUtc` set) with ≥ 3 similar public vacancies nearby
- one closed vacancy with none nearby
- a candidate and an employer account
- an admin

For **404** (`/bestaat-niet`), **500** (the test-only throwing endpoint from 01, enabled in the CI stack via an env flag that is **off** by default and never set on Render), **403** (candidate opens `/admin`), **410** (both closed vacancies), **429** (a lowered limiter via a CI-only env value), and **503** (maintenance on), in **nl, en, pl, ro, ar**, at **1440×900** and **390×844**, assert:
- the expected HTTP status on the document response (Playwright `response.status()`)
- `<meta name="robots" content="noindex">`
- one `<h1>`; `<html lang>` = culture; `dir="rtl"` only for ar
- no horizontal overflow at 390; body text ≥ 16 px; primary buttons ≥ 44 px high
- no "Exception", "at Jobsy.", "System.", `TraceIdentifier`-like GUIDs, or `[PLACEHOLDER]` text in the HTML
- 500 and 429: a support code matching `^LB-[2-9A-HJKMNP-TV-Z]{4}$`
- **no-JS:** a second context with `javaScriptEnabled: false` for 404/500/410/503 shows the full page (it's static SSR)
- screenshots to `artifacts/playwright-errors/{code}-{variant}-{lang}-{width}.png` (full page); add the path to the "Upload Playwright screenshots" step

## 06.2 Flows
- **403 switch account:**
  - a candidate opens `/admin/settings` → 403 page, URL unchanged
  - "Ander account" → logout → login as the admin → lands back on `/admin/settings`
- **403 employers-off** (paspoort dependency; else skip with reason): `/access-denied?reason=employers-off` shows the specific text.
- **410:**
  - the closed vacancy shows at most 3 similar cards, each opening a 200 vacancy
  - the "none nearby" one shows the search CTA instead
  - the intermediary-hidden case shows no company name
- **Maintenance:**
  - an admin switches on (05.5) → within 20 s an anonymous context gets 503 on `/` and `/vacatures`
  - the admin context still browses and sees the banner
  - `/healthz` stays 200
  - switch off → anonymous 200 within 20 s
- **Reconnect toast:** kill the circuit (`page.context().setOffline(true)` on an interactive page) → the toast shows the "trying" text in the page language (nl and ar); back online → it disappears.
- **Inline block error:** route-intercept one API call on a dashboard card to return 500 with a `supportCode` → the card shows "Dit stukje laadt nu niet." + the code, and the rest of the page works; "Opnieuw" with the intercept removed loads the card.
- **Copy button:** on 500, "Kopieer" puts the code on the clipboard (grant clipboard permission); without JS the code is selectable text.

## 06.3 HTTP guards (`StatusPagesHttpTests`, no browser)
Table-driven test over the matrix `path × Accept (html/json) × signed-in (no/yes)`. Assert status, `Content-Type`, `Cache-Control: no-store` on status pages, `X-Robots-Tag` or meta noindex, and `Retry-After` on 429/503. Include:
- `/vacancies/{random-guid}` 404; `/vacancies/{closed}` 410 (HTML) and `api/vacancies/{closed}` 410 ProblemDetails `code: "vacancy_closed"`
- `/api/...` unknown → JSON 404, never HTML
- `/_framework/x.js`, `/css/missing.css` → plain 404, no HTML page
- `/status/999` → 404 page
- `/healthz` and API `/health` → 200, including during maintenance
- the sitemap doesn't contain `/status/`, `/access-denied` or closed vacancies
- sources:
  - `NoRawExceptionMessageTests` from 04 green
  - no `forceLoad` navigation to `/access-denied` left
  - no hard-coded Dutch in `App.razor`'s reconnect markup

## 06.4 Docs
- `docs/support-codes.md` (from 01) completed:
  - where codes appear (500, 503, inline block, API ProblemDetails)
  - how to search Sentry/Render logs
  - retention of logs
- `docs/errors-followups.md`:
  - every "absent" dependency line from 01–05
  - the remaining `ex.Message` files (from `docs/errors/ex-message-baseline.txt`, with counts)
  - native review pl/ro/ar (`docs/i18n/errors-review.md`)
  - anything skipped in 06.2
- `docs/onderhoud.md` (from 05) checked: runbook steps match what the E2E did.
- `docs/ROUTES.md` checked (`RoutesDocFreshnessTests`), `CHANGELOG.md` one entry: what visitors notice (friendly pages in your language, real status codes, a code to quote to support, maintenance page, closed jobs show alternatives).

## 06.5 Stack-end report (in the PR body and as the last message)
- PR list 01–06 with state (open/draft) and own commits.
- Dependency outcomes A–G per re-check point.
- Test summary: page matrix count (codes × variants × 5 languages × 2 widths + no-JS), flows, HTTP guards; skipped items with reason.
- The `ex.Message` count at `acceptatie` vs. at the end of the stack.
- Screenshot artifact name.
- **What Dennis still has to decide or do:**
  1. **Cloudflare plan:** is Custom Error Pages available (paid) or use the Free fallback (Worker / Render only)? See `docs/onderhoud.md`.
  2. **Render maintenance URI:** host `ops/maintenance/index.html` outside the web service (Render static site or Cloudflare Pages) and set it as the maintenance `uri` (dashboard or blueprint). Cursor did not change `render.yaml`.
  3. **Cloudflare error page upload** (only if paid): upload `ops/maintenance/cloudflare-500.html` via its public URL.
  4. **support@lobsy.nl:** make sure it exists and is read; the status pages point there with the support code.
  5. Native review pl/ro/ar.
- Out of scope / deferred (from followups).

## Success criteria
- The matrix is green for every status page × 5 languages × 2 widths, and no-JS works for the static ones, with screenshots uploaded.
- The HTTP guards prove real status codes, noindex/no-store, Retry-After, JSON for API routes, and untouched health checks.
- The report lists everything Dennis must decide or configure.

Done → end of stack. Report (06.5) and stop.
