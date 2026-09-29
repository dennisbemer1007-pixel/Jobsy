# 10. Measure: cookieless KPI funnel + admin view, performance budgets, Lighthouse CI, Playwright LCP

Read `00-README.md` first. Branch `cursor/landing-10` from `cursor/landing-9` (or `-9b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/landing-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Counters store **no identifiers**: no user id, IP, anonymous key, cookie or UA string. No third-party analytics, and no Lighthouse upload to public storage.

| | |
|---|---|
| Branch | `cursor/landing-10` |
| PR title | `feat(landing): cookieless KPI funnel (+ /admin/funnel), performance budgets, Lighthouse CI on / , /ontdek, /banenkaart` |
| PR body starts with | `Stacked on #<PR 09> (cursor/landing-9)` |
| Mockups | none (the admin view uses today's admin page style) |
| Split seam | **10a** = funnel counters + endpoints + admin view (10.2–10.4). **10b** = budgets, Lighthouse CI, Playwright LCP, docs (10.5–10.7) |

## Goal
Dennis can see, per day, variant and language, how many visitors go from the landing page to the test, finish it, create an account and get value from Lobsy, without any tracking consent, because nothing identifies a person. Every PR shows whether "/" still meets the speed budget.

## 10.1 Today (verify first)
- `AnalyticsController` `impressions` + `site-visits` (consent-gated via `AnalyticsConsent.IsGranted`, `anonymousKey` + `AnonymousKeyRules`), sent server-side from Web (`JobsyApiClient.Vacancies.cs` ~L464). `SiteVisit` entity. No third-party analytics. **Leave all of that unchanged.**
- `data-kpi` / `data-kpi-target` attributes exist on the CTAs since 05–08. `landing.js` has the beacon stub.
- CI: `.github/workflows/pr-tests.yml` job `test` (ubuntu-latest), steps Build → unit tests (with a long `FullyQualifiedName!~…` exclusion list for Playwright classes) → Install Playwright Chromium → "Start API + Web (seeded Development)" via `.github/scripts/start-ci-stack.sh` (`ASPNETCORE_ENVIRONMENT=Development`, Web on `127.0.0.1:5201`) → Playwright smoke (inclusion list) → artifact uploads. No Lighthouse config exists.
- Perf tests: `PersistLcpPlaywrightTests`, `MainLayoutTtfbPlaywrightTests`, `BanenkaartMapPerfPlaywrightTests`, `ClientPerformanceMetricsTests`, `VacancyPerformanceMetricsTests`; `docs/performance.md`.

## 10.2 Funnel counters (§K)
- Entity `FunnelDailyCounter` (own migration): `Day` (`DateOnly`, Europe/Amsterdam), `Step` (enum `FunnelStep`, stored as string ≤ 32), `Variant` (`on|zw`), `Culture` (`nl|en|pl|ro|ar`), `Device` (`mobile|desktop`), `Dimension` (string ≤ 32, from an allow-list per step: method `google|microsoft|email`, target `employer|school|partner|employer-register|…`, `voor` `werk|nieuw|school|vast`, `lcp` bucket `0-1000|1000-1500|1500-2000|2000-2500|2500-3000|3000-4000|4000+`, or empty), `Count` (long). Unique index over all key columns.
- `FunnelCounterService.IncrementAsync(key)`: one atomic upsert (`INSERT … ON CONFLICT (…) DO UPDATE SET "Count" = "Count" + 1`). Never read-modify-write.
- **Server-side increments** (most reliable, no JS):
  - `LandingView`: in `LandingRedirectMiddleware` when "/" is rendered to an anonymous visitor. Skip `HEAD`, crawlers (`CrawlerUserAgent`), prefetch/prerender requests (`Sec-Purpose: prefetch`, `Purpose: prefetch`) and requests with `?lang=` from bots. Device from the `Sec-CH-UA-Mobile` hint when present, else the `mobile|desktop` guess the client sends later; never store the UA.
  - `TestStart`, `TestComplete`, `ResultCtaSignup`: from `GratisDna.razor` (interactive) via `JobsyApiClient`, once per page session.
  - `SignupStart{method}`: the `/account/external/*` challenge with `van` or `signup=1`, and `/account/email-code/start`.
  - `SignupComplete{method}`: where a **new** candidate is created (`ensure-external` new user; email-code verify `SignUp`).
  - `FirstValue`: the first view of `/banenkaart` or `/candidate/match` (ON) / the passport or `/candidate/profile` (OFF) by a candidate whose account is ≤ 30 days old. Use a nullable `User.FirstValueAtUtc` column set once (the counter increments only when it flips from null).
- **Client beacons** (static pages): `landing.js` sends `navigator.sendBeacon("/funnel", {step, dim, device})` on clicks of `[data-kpi]` (`LandingCtaTest`, `LandingCtaLogin`, `LandingCtaMap`, `LandingCtaAudience`) and once per page load the LCP bucket (`PerformanceObserver` `largest-contentful-paint`, sent on `visibilitychange: hidden`). Variant + culture are added **server-side** from the request, never trusted from the client.
  - Web endpoint `POST /funnel` (Minimal API): body ≤ 512 bytes, allow-listed values only (anything else → 204 and ignored), rate limit per client (in-memory, IP never stored), same-origin check (`Sec-Fetch-Site`). It forwards server-to-server to `POST api/public/funnel` (provision secret, like `ensure-external`).
- **No consent needed (D12):** nothing is stored on the device and nothing identifies a person. Add one sentence to `/privacy`: "We tellen per dag hoeveel mensen een stap zetten (bijvoorbeeld de test starten). Die tellingen zijn anoniem: er zit geen cookie, IP-adres of account aan vast."
- Retention: delete rows older than 25 months in an existing daily cleanup job (or a small new hosted service if none fits).

## 10.3 Admin view `/admin/funnel` (Dependencies F)
- `[Authorize(Roles = "Admin")]`, InteractiveServer like the other admin pages. The nav item is placed per Dependencies F.
- Filters: period (7 / 30 / 90 days, custom from–to), variant (all/on/zw), language, device.
- **Funnel table:** LandingView → LandingCtaTest → TestStart → TestComplete → ResultCtaSignup → SignupStart → SignupComplete → FirstValue, with counts and "% of previous step" (show "—" when the previous count < 20).
- **CTA split:** hero test vs login vs map vs audience targets.
- **Sign-up methods:** google / microsoft / email.
- **LCP:** bucket histogram + an estimated p75 (from buckets), with the 2.5 s budget line.
- **Daily line** for LandingView and SignupComplete (the existing admin chart primitive, if any; else a simple table per day).
- CSV export of the filtered rows.
- Strings in `UiStringsFunnel.cs` (`AdminFunnel.*`, 5 languages like other admin modules or nl/en if admin is nl/en only; follow what the other admin modules do and keep parity green).
- `docs/ROUTES.md`, `PageHelpDocs`, `BlazorPageRoleAttributesTests` (Admin only), `PageSeoCatalog` (private).

## 10.4 KPI definitions (write them into `docs/landing-kpi.md`)
| KPI | Formula | Target (initial, Dennis can change) |
|---|---|---|
| Landing → test start | TestStart / LandingView | ≥ 25 % |
| Test completion | TestComplete / TestStart | ≥ 70 % |
| Result → account | SignupComplete / TestComplete | ≥ 20 % |
| Account → first value | FirstValue / SignupComplete (7-day window by day sums) | ≥ 60 % |
| CTA split | each `LandingCta*` / sum | (observe) |
| LCP p75 mobile | from `lcp` buckets, Device = mobile | < 2.5 s |

Note in the doc: counts are per event, not per person (a person may count twice). That's the price of no identifiers, and it's fine for trends.

## 10.5 Performance budgets (D13)
Write them into `docs/performance.md` ("Landing en publieke pagina's") and enforce them:
- "/" (both variants), `/werkgevers`, `/scholen`: LCP ≤ **2.5 s**, TTI (`interactive`) ≤ **3.5 s**, TBT ≤ **200 ms**, CLS ≤ **0.1**, on Lighthouse mobile (default simulated throttling, Moto G-class, slow 4G). HTML ≤ 60 KB gz, CSS for the page ≤ 25 KB gz (plus `app.min.css` only if 05 kept it blocking), JS ≤ 15 KB gz (`app-core.js` + `landing.js`), **no** requests to MapLibre/jobMap/tiles/`_blazor`.
- `/ontdek`: LCP ≤ 2.5 s (error), TTI ≤ 3.5 s (warn; it's an interactive Blazor page), CLS ≤ 0.1.
- `/banenkaart`: record the current values as a baseline; warn-only (the map's own perf work is out of scope).

## 10.6 Lighthouse CI
- `.lighthouserc.json` in the repo root:
  - `collect`: `url` = `http://127.0.0.1:5201/`, `http://127.0.0.1:5201/?_variant=zw` (the Development-only override from 01.8), `http://127.0.0.1:5201/ontdek`, `http://127.0.0.1:5201/banenkaart`; `numberOfRuns: 3`; Chrome from the runner (`chromePath` from `CHROME_PATH` or the Playwright Chromium); `settings.skipAudits: ["uses-http2"]` (local http).
  - `assert.assertMatrix` per URL pattern with the budgets from 10.5 (`largest-contentful-paint`, `interactive`, `total-blocking-time`, `cumulative-layout-shift` as `maxNumericValue`, `aggregationMethod: "median"`), plus for "/": `resource-summary:script:size` ≤ 15360 (**error**) and `network-requests` must not match the forbidden URL patterns (a small custom check in the Playwright test is simpler; keep Lighthouse to metrics).
  - `upload.target: "filesystem"`, `outputDir: ".lighthouseci"`. **No** `temporary-public-storage`, no LHCI server.
- `pr-tests.yml`: after "Start API + Web" and **before** the Playwright smoke (a cold, quiet stack), a step "Lighthouse CI" running `npx --yes @lhci/cli@0.14 autorun --config=.lighthouserc.json` (pin the minor version), then upload `.lighthouseci/` as an artifact (`lighthouse-reports`). Failing assertions fail the job.
  - First warm up the stack with one `curl` of each URL so .NET JIT/first-request cost doesn't count.
  - If the GitHub runner turns out too noisy for "error" on TTI, keep LCP/TBT/CLS as error and TTI as warn, and say so in the PR with 3 runs of data. Don't loosen LCP.
- Add every new Playwright test class of this stack (05–10) to **both** filter lists in `pr-tests.yml` (excluded from the unit step, included in the smoke step), as the existing classes are.

## 10.7 Playwright LCP + no-map test
- `LandingLcpPlaywrightTests` (CI stack): Chromium with CDP network throttling (≈ Fast 4G: 150 ms RTT, 1.6 Mbps down) + CPU 4× slowdown, mobile 390×844. Load "/" and "/?_variant=zw" 3 times each, read LCP via `PerformanceObserver`, and assert the median ≤ **2500 ms** (log all values). Assert zero requests matching `maplibre|jobMap|openfreemap|/_blazor|api/vacancies`.
- Keep `PersistLcpPlaywrightTests` for the map (retargeted to `/banenkaart` in 04).

## Tests
- `FunnelCounterServiceTests` (atomic upsert under parallel increments; key allow-lists; unknown values ignored).
- `/funnel` endpoint: size limit, allow-list, cross-site rejected, rate limit, variant/culture set server-side (a client-sent variant is ignored).
- Server increments: LandingView skips HEAD/crawler/prefetch; SignupComplete only for new users; FirstValue once per user.
- A reflection/schema test: `FunnelDailyCounter` has no column named like `*Id` (except its PK), `*Ip*`, `*Key*`, `*Agent*`, `*Email*`.
- `/admin/funnel`: admin only, math (percentages, "—" below 20), CSV.
- `.lighthouserc.json` parses and contains the 4 URLs and the budget numbers from `docs/performance.md` (a test reads both so they can't drift).
- `LandingLcpPlaywrightTests`.
- Existing analytics tests (`site-visits` consent gating) unchanged and green.

## Success criteria
- A full run (landing → test → result → e-mail sign-up → `/banenkaart`) increments every funnel step once, visible in `/admin/funnel` for the right variant and language.
- Nothing in the funnel storage can identify a person (schema test).
- The PR workflow runs Lighthouse on "/", "/?_variant=zw", `/ontdek` and `/banenkaart`, uploads the reports as an artifact, and "/" passes LCP ≤ 2.5 s, TTI ≤ 3.5 s, TBT ≤ 200 ms and CLS ≤ 0.1.

Done → end of stack. Report the table file → branch → PR number → status (green or draft/red), the Dependencies outcome (A–F, and whether A flipped at 07), the Lighthouse numbers for "/" (ON and OFF), and anything deferred (e.g. native review of PL/RO/AR, the mascot art drop, the real `SchoolsEmail`).
