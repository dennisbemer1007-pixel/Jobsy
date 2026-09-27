# Cursor prompt: Acc 27-09 fixes (popup photo, floating Kompas tabs, Top 10 on mobile, reconnect toast)

> Paste everything below the line into Cursor (repo `dennisbemer1007-pixel/Jobsy`).

---

## Rules
- **ONE PR only.** Do **not** merge, do **not** deploy, do **not** use rule/shortcut 123, and do not trigger Render deploys or retrigger commits.
- **Target branch: `main`.**
  - acceptatie.lobsy.nl currently serves `main`: `<meta name="lobsy-commit" content="e781767…">` = `main` HEAD, and the assets are `?v=20260926-mapfix9`.
  - The `acceptatie` branch is 29 commits ahead of `main`: PR #326 (map 429/proxy-cache/early-boot), the release-flow render.yaml that points the Acc services at `acceptatie`, and the smoke CI. The Render Blueprint still syncs from `main`, so none of that is live.
  - Branch from `origin/main` and do **not** pull `acceptatie` commits into this PR.
  - **Before starting**, re-check `curl -s https://acceptatie.lobsy.nl/ | grep lobsy-commit`. If it now equals `origin/acceptatie` HEAD (the release flow went live), branch from and target `acceptatie` instead, and say so in the PR description.
- Keep the current look (colours, pins, carousel, header, bottom nav, Match button). Make only the changes below.
- One commit per item (§1-§4) so each can be reverted on its own. Bump `app.min.css?v=` / `jobMap.min.js?v=` / `app-core.js?v=` wherever you touch those files.

## §1 Map popup photo = the vacancy's own photo
**Findings (Acc, 27-09 09:05 CEST):**
- The popup `<img class="map-popup__photo">` src comes from the card API: `GET /api/vacancies/{id}/card` → `thumbnailUrl` (`jobMap.js` `mapCardToPopup`, `thumb = card.thumbnailUrl || card.imageUrl`, ~l.1060; rendered in `buildPopupHtml` ~l.459-466).
- The API builds `thumbnailUrl` in `VacanciesController.MapCard` (`Jobsy.Api/Controllers/VacanciesController.cs:1852-1853`) with `VacancyImageUrls.ForPublicList(record.ImageUrl, id, workType) ?? Placeholder(...)`.
- `ForPublicList` (`Jobsy.Core/Media/VacancyImageUrls.cs:62-76`) replaces **inline `data:image/…` uploads, picsum and broken-Unsplash URLs with the work-type SVG** (`/images/vacancies/{type}-{0|1}.svg`). The company logo is **never** used as the fallback there.
- The vacancy **detail** page uses `VacancyPhoto` → `VacancyImageUrls.ForDisplay`/`Resolve`, i.e. the raw photo → logo → SVG. So any vacancy whose photo is stored inline shows its real photo on the detail page but the generic SVG in the popup, the list and the carousel.
- `VacancyImageUrls.PublicImagePath(id)` = `/api/vacancies/{id}/image` already exists. It decodes inline photos (`VacanciesController.cs:533-559`) but is **used nowhere**.
- In the current Acc mock data, all 328 vacancies store an SVG work-type path as `ImageUrl`, so there the popup and detail page match. Dennis's "different image" case needs a vacancy with a real (inline or http) photo. Create or seed one in tests.

**Change:**
1. Add `VacancyImageUrls.ForCard(string? imageUrl, string? logoUrl, Guid id, string? workType)`:
   - inline data URI → `PublicImagePath(id)`
   - usable http(s) or `/images/…` photo → itself
   - else the usable company logo (`Normalize(logoUrl)`)
   - else `Placeholder(id, workType)`
   - Keep picsum/broken-Unsplash → logo → placeholder.
2. Use `ForCard` in `MapCard` (card + cards) and in the discover/list mapper (`VacanciesController.cs:~1992`, `ForPublicList(r.ImageUrl, …)`), so popup, carousel and list all show the same image as the detail page.
3. Web proxy: add `GET /api/vacancies/{id:guid}/image` to `Jobsy.Web/Hosting/VacancyMapProxyEndpoints.cs`, streaming bytes and passing `Content-Type`/`Cache-Control` through. Follow 302 redirects server-side, or return the redirect as-is (same-origin `/images/...`). CSP `img-src 'self'` then covers it.
4. `jobMap.js` `buildPopupHtml`: if the photo is a work-type placeholder (`/images/vacancies/`) **and** `logoUrl` exists, show the logo variant (`map-popup__media-logo`). This is the existing markup, so there is no new style.
5. **CSP check:** the web CSP `img-src 'self' data: blob: https://tiles.openfreemap.org https://i.ytimg.com` blocks external https photo URLs. For http(s) photos not on our origin, route them through a same-origin resize/proxy or keep the logo fallback (`data-fallback-src`). Document the choice. Do **not** widen the CSP to `https:`.

**Tests:**
- xUnit `VacancyImageUrlsTests.ForCard_*`: data URI → `/api/vacancies/{id}/image`; https photo kept; empty photo + logo → logo; empty + no logo → SVG; picsum → logo/SVG.
- `VacancyCardApiTests`: a vacancy with inline photo → `thumbnailUrl == "/api/vacancies/{id}/image"`.
- Playwright (390×844 and 1366×900) against a seeded vacancy with a photo: tap the pin → `.map-popup__photo` `currentSrc` equals the detail page's `.vacancy-media__photo` `currentSrc` (or `/api/vacancies/{id}/image`), `naturalWidth > 0`, no CSP violation in the console.

## §2 Mijn profiel: Kompas tab ribbon "floats"
**Findings:**
- `.kompas-tabs.admin-sublinks` is `position: sticky; top: 0; z-index: 6` with a shadow (`Jobsy.Web/wwwroot/css/app.css:18115-18137`, sticky since `ff734cf` 20-09).
- On mobile the scroll container is **not** `.app-main` but `.panel-page.profile-page`. Measured: `overflow-x: hidden` → `overflow-y: auto`, scrollHeight 1917 / clientHeight 577. That comes from `app.css:16019-16024` (`@media (max-width: 768px) … .panel-page { overflow-x: hidden }`) plus `.panel-page { padding: 1.5rem 1.25rem 2rem }` (`app.css:290`).
- So the bar sticks **24 px below the scroller top**, only **363 px wide** (inset 14 px each side), with the `var(--bg)` background covering just that box. Content (e.g. "Top 10 vacatures" heading, cards) scrolls visibly above and beside it, and it hovers over cards with its shadow.
- Screenshots: `acc-2709/mobile-02-tabs-floating.png`, `mobile-03-profiel-top10.png`, `mobile-03-tests-top10.png`.
- The fixed `.profile-save-bar` on ≤1024 px (`app.css:16463-16476`) is a second floating layer on the Profiel tab.

**Change** (mobile ≤1023 px only; desktop unchanged):
- Make the ribbon a normal in-flow element: `@media (max-width: 1023px) { .kompas-tabs.admin-sublinks { position: static; box-shadow: none; } }`. Keep the horizontal scroll and chip styles.
- Do **not** touch the global `.panel-page` overflow rule (it prevents horizontal overflow on mobile).
- Leave `.profile-save-bar` as is (it is intentional), but make sure it never covers the last field: the existing `padding-bottom: 4.5rem` must still apply.

**Tests (Playwright 390×844, logged in as the demo candidate on local/Acc):**
- Open `/candidate/profile`, tabs Profiel / Tests / Functiefit. Scroll the real scroller (`.panel-page.profile-page`) by 800 px.
- Assert `.kompas-tabs` has `getComputedStyle().position === "static"` and is off-screen (bottom < scroller top).
- At 1366×900, sticky behaviour is unchanged.

## §3 Top 10 vacatures: not on mobile
**Findings:**
- The panel is `CompetencyMatchPanel`, rendered in the Kompas `Side` slot from `Profile.razor:649-658` (`<Side><PanelErrorBoundary Name="matched-vacancies">…<CompetencyMatchPanel …/>`). It is also rendered in `CareerTest.razor:52-56`.
- `CandidateKompas.razor:226-231` renders `Side` when `ShouldShowSide` is true. That is `_tab != Dna || _isWideViewport` (`:263-264`), where `_isWideViewport` defaults to **true** (`:254`) and is set via `jobsyViewport.isKompasWide` (≥1024 px, `app-core.js:405-407`).
- So on mobile it is hidden **only on the Mijn DNA tab**. On Profiel, Tests and Functiefit it is stacked under or above the main panel. It also flashes during prerender on DNA, because the default is `true`.
- No unmerged PR hides it on mobile. Related history: #267 / `dd93cf5` "Show the Top 10 vacancies beside the compass again" (23-09), and `cc64fd3` (26-09) added `ShouldShowSide`.

**Change:**
- `ShouldShowSide => _isWideViewport` (all tabs). Default `_isWideViewport = false` until JS reports it, and re-evaluate on resize/orientation (a `matchMedia` listener → `[JSInvokable] SetWide(bool)`).
- Add CSS as a safety net against prerender flashes: `@media (max-width: 1023px) { .kompas-workspace__side { display: none; } }`.
- On mobile, `Profile.razor` should skip the Top 10 fetch entirely when the side isn't shown (avoid the API call). Keep the kompas-snapshot `TopMatches` path (`Profile.razor:835-843`) for desktop.
- `CareerTest.razor:52-56`: hide the match panel below 1024 px the same way (CSS class `questionnaire-matches` → `display: none` on mobile). A link "Bekijk je matches" → `/candidate/match` may stay.

**Tests:**
- Playwright 390×844: on `/candidate/profile`, for each tab (Mijn DNA, Profiel, Tests, Functiefit), `.competency-matches` count = 0 (also right after first paint, before hydration).
- `/candidate/career` (Beroepentest) result: no `.competency-matches`.
- At 1366×900, `.kompas-workspace__side .competency-matches` is visible on all tabs.
- bUnit/xUnit guard: `ShouldShowSide` depends only on viewport width.

## §4 Frequent "Verbinding herstellen…" (Blazor reconnect toast)
**Findings:**
- Config: `KeepAliveInterval` 15 s, `ClientTimeoutInterval` 60 s (`Jobsy.Web/Program.cs:44-50`); `DisconnectedCircuitRetentionPeriod` 5 min, max 200 (`:55-60`); WebSocket keepalive 15 s (`:153-156`).
- The custom toast is `#components-reconnect-modal` (`App.razor:221-232`, CSS `app.css:16484-16510`). It shows **immediately** on any drop. On `rejected` it only shows "Sessie verlopen. Herladen", with no auto-reload.
- `blazor.web.js` autostarts (`App.razor:248`) with default reconnection options, and nothing reconnects on `visibilitychange`/`online`.
- **Measured on Acc (27-09 09:05-09:25 CEST):**
  - 6.5 min mobile and 6.5 min desktop of continuous use (tab switching every 12-20 s, 24 page loads): **0 reconnect toasts, 0 WebSocket closes**.
  - A 90 s frozen tab (CDP) also caused no reconnect.
  - Offline 8 s: no toast.
  - Offline 25 s: toast shown for the whole outage, recovered ~1 s after coming back online.
  - Offline 75 s: toast shown for the whole outage. In one run it was still showing 15 s after coming back online (waiting for the backoff); in the other run it recovered after ~1 s.
- **So it is not random server instability in the foreground.** The likely triggers are:
  1. **Mobile backgrounding / screen lock / network switches.** iOS/Android suspend the WebSocket; on return the toast shows until the next backoff retry.
  2. **Deploys:** Acc web is a **single Starter instance** that redeploys on every push to `main` (69 first-parent commits on `main` since 26-09 00:00 CEST). Every deploy drops all circuits, so users see "Verbinding herstellen" → "Sessie verlopen" and must tap Herladen.
  3. Background tabs longer than the 5 min retention → `rejected`.
  - Render health check is `healthCheckPath: /` (render.yaml, acc and prod web). That is the heavy prerendered map page, which calls the API. Slow responses under load risk failed health checks and restarts.
  - Data Protection keys are persisted in Postgres (`Jobsy.Web/Security/DataProtectionSetup.cs`), so deploys don't log users out, only drop circuits.

**Change:**
1. `App.razor`:
   - `blazor.web.js` with `autostart="false"`, then a nonce'd inline script: `Blazor.start({ circuit: { reconnectionOptions: { maxRetries: 60, retryIntervalMilliseconds: (n) => n < 3 ? 500 : n < 10 ? 2000 : 5000 } } })`.
   - On `document.visibilitychange` → visible and on `window.online`: if `#components-reconnect-modal` has `components-reconnect-show`/`-failed`, call `Blazor.reconnect()` immediately.
   - On `components-reconnect-rejected` (circuit gone after a deploy or retention): **auto `location.reload()`** once, guarded by `sessionStorage` to avoid loops. Keep the existing "Herladen" button as a fallback.
2. `app.css` `.reconnect-toast.components-reconnect-show`: show only after **1.5 s** (`transition-delay` / animation delay), so short blips don't flash. Keep the look.
3. `Program.cs`: `DisconnectedCircuitRetentionPeriod` 5 → **15 min** and `DisconnectedCircuitMaxRetained` 200 → 100 (Starter 512 MB). Keep KeepAlive 15 s / ClientTimeout 60 s.
4. Add a lightweight `GET /healthz` (200 "ok", no auth, no prerender, no API call) to Jobsy.Web. In `render.yaml` set `healthCheckPath: /healthz` for `jobsy-web` and `lobsy-acc-web`.
   - **Mention in the PR description that this render.yaml change only takes effect on Blueprint sync. Do not sync or deploy.**
5. Log reconnect telemetry client-side (count per session via the existing `CircuitExceptionLogger` connection down/up logs; no new service), so Dennis can correlate with Render deploy times in the logs.

**Tests:**
- Playwright 390×844, logged in on local:
  - `context.setOffline(true)` for 1 s → toast **never** visible.
  - Offline 25 s → toast visible, then within 3 s of `setOffline(false)` the toast is hidden and the circuit is working (click a bottom-nav tab, page renders).
  - Offline 25 s while `visibilityState` is hidden, then dispatch visible → reconnect is attempted within 1 s (spy on `Blazor.reconnect`).
- Unit/static guard: `App.razor` contains `autostart="false"`, `Blazor.start(` with `reconnectionOptions`, and a `components-reconnect-rejected` → `location.reload` handler.
- `GET /healthz` returns 200 in < 50 ms and never touches `IJobsyApiClient` (WebApplicationFactory test).

## PR description must include
- Target branch and why (Acc serves `main` @ `e781767`; `acceptatie` unreleased).
- Per item: root cause (file:line), change, test.
- Screenshots before/after for §2 and §3 (mobile 390×844) and a popup photo example for §1.
- Note for Dennis: reconnect toasts will still appear briefly after each deploy. Batching merges (the release flow) reduces them. The `/healthz` switch needs a Blueprint sync.
