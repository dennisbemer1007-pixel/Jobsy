# 01. Hotfix (standalone): real travel-time rings, desktop banenkaart crash, idempotent pagehide shim

Read `00-README.md` first ("How to run", §0, D1). Branch `cursor/kandidaat-banen-hotfix` from `origin/acceptatie`. This file is **not stacked** on anything and may run on its own, before any other stack.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/kandidaat-banen-hotfix`; no force-push.
> - ONE PR into `acceptatie`; its body starts with `Standalone hotfix (not stacked)`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Hotfix scope only: root-cause fixes + regression tests. No redesign, no refactors, no speculative rewrites, no new strings beyond what a fix needs.

| | |
|---|---|
| Branch | `cursor/kandidaat-banen-hotfix` (from `origin/acceptatie`) |
| PR title | `fix(banenkaart): real isochrone rings (decimal contour), desktop map crash for complete profiles, idempotent pagehide shim` |
| PR body starts with | `Standalone hotfix (not stacked)` + the crash root cause in one paragraph + before/after screenshots of the rings (desktop 1440) |
| Mockups | none. `kd-d1-banenkaart.png` shows what real rings look like; the styling itself is file 03 |
| Split seam | none (small). If (b) turns out to be large, ship (a)+(c) as this PR and (b) as `cursor/kandidaat-banen-hotfix-b` from `origin/acceptatie`, also standalone |

## Goal
Three bugs that hurt every candidate today are fixed at the root, each with a test that fails before the fix:
1. The travel-time rings are real road isochrones again instead of the circle fallback.
2. A candidate with a complete profile can use the desktop banenkaart without "Even iets misgegaan".
3. No "Maximum call stack size exceeded" page error after enhanced navigation.

## 01.1 Today (verify first)
- **(a) Isochrones:**
  - `Jobsy.Infrastructure/Services/ValhallaIsochroneService.cs` `ReadMinutes` (~L212–229) reads the `contour` / `minutes` / `time` property with `TryGetInt32`. Valhalla returns `"contour": 30.0` (a JSON **decimal**), so `TryGetInt32` fails and every feature is skipped.
  - `NormalizeFeatureCollection` then returns null, and `Jobsy.Api/Controllers/TravelController.cs` `GetIsochrones` (~L31–75) returns **404**. The client (`fetchIsochrones` in `jobMap.js`) falls back to circles ("isochrone fallback").
  - Caching:
    - the service caches successes for 7 days (key prefix `iso:`)
    - the controller caches only non-empty results
    - the Web proxy `Jobsy.Web/Hosting/VacancyMapProxyEndpoints.cs` caches only 200/304 for anonymous users (~L163–171)

    So the 404s were never cached, and nothing needs purging.
  - Config: `Routing:IsochroneBaseUrl`, default `https://valhalla1.openstreetmap.de`. OV has no transit costing and uses the circle fallback on purpose; keep that.
  - Existing tests: `Jobsy.Tests/IsochroneServiceTests.cs` (mock service + a string guard on `jobMap.js`: "fetchIsochrones", "isochrone fallback", colours `#2563eb` / `#1d4ed8`, opacities 0.16/0.11/0.07). Keep them green; don't change the ring styling here.
- **(b) Desktop crash:**
  - `kandidaat@jobsy.local` (complete profile: culture, values, competencies, interests) opens the banenkaart at **1440** or **1280** wide. The cards render, and about **4 s later** the page shows the circuit error ("Even iets misgegaan", `Circuit.ErrorTitle`).
  - It does **not** happen on mobile (390), and not for a candidate with an incomplete profile (e.g. valentine, 0/3 tests). That points at the **desktop-only, complete-profile-only** top-match path in `Jobsy.Web/Components/VacancyDiscovery.razor`:
    - `EnsureDesktopMatchDeckAsync` (~L1208), started fire-and-forget (`_ = EnsureDesktopMatchDeckAsync();` ~L1942 and ~L2247). Only `LoadAsync` is inside a try/catch; the rest (state changes, `StateHasChanged`, JS interop) is not.
    - `ShowTopMatchTile` (~L981–989) and `TopMatchTileVisibility.cs`
    - `TopMatchLeadingFragment` (~L1255): captures a component reference, formats the travel via `FormatTopMatchTravel` (~L1195)
    - `HighlightVacancyCarousel` `LeadingItem` (~L527–535)
    - `Components/Match/MatchDeckDialog.razor` (~L537 in the page; 246 lines), `Components/Match/TopMatchTile.razor` (55 lines, `Culture.Format("Match.TopMatchAria", pct, …)`), `Services/MatchDeck.cs`
  - These are **suspects, not a diagnosis**.
- **(c) Page error:**
  - `Jobsy.Web/Components/App.razor` ~L114–141 has an inline script that patches `EventTarget.prototype.addEventListener` / `removeEventListener` to map `unload` → `pagehide` (wrapping listeners in `mapped`).
  - The script runs again on enhanced navigation. The second run wraps the already-patched function, so `add` points at the previous patch. Each extra run adds a layer until a call recurses: "Maximum call stack size exceeded" (a `pageerror` in Playwright).

## 01.2 (a) Isochrone decimal parse
- `ReadMinutes`: accept a JSON number of either kind. Use `TryGetDouble` and round with `Math.Round(value, MidpointRounding.AwayFromZero)` to an int. Reject non-finite values and values ≤ 0 or > 240 (skip that feature, as today). Keep `TryGetInt32` behaviour identical for integer payloads.
- Check the other readers in the same file for the same int-only assumption (e.g. any `costing`/`time` parsing) and fix only those that read Valhalla numbers.
- Unit tests in `IsochroneServiceTests.cs` (or a new `ValhallaIsochroneParsingTests.cs`):
  - a **real Valhalla-shaped** payload (FeatureCollection, 3 polygon features, `"contour": 10.0`, `20.0`, `30.0`, `"metric": "time"`) normalises to 3 features with minutes 10/20/30
  - `"contour": 20` (int) still works
  - `"contour": 19.6` → 20; `"contour": 0.0`, `-5`, `NaN`-like strings → skipped
  - `TravelController.GetIsochrones` returns **200** with the normalised collection for that payload (use the existing controller test pattern, or a fake `HttpMessageHandler`)
- Manual check (say in the PR): on a local run, `GET /api/travel/isochrones?lat=52.0205&lng=4.2476&mode=Fiets&minutes=10,20,30` returns 200 with 3 features, and the desktop banenkaart shows real (non-circular) rings.

## 01.3 (b) Desktop banenkaart crash: reproduce → root cause → fix → regression test
1. **Reproduce first**, with the Playwright approach used by `Jobsy.Tests/CandidateApplicationsPlaywrightTests.cs` (`TryLoginAsync` with `JOBSY_E2E_CANDIDATE_EMAIL` / `_PASSWORD`, default `kandidaat@jobsy.local`) against a local run or acceptatie:
   - viewport 1440×900 (and 1280×800)
   - log in, open the banenkaart, wait for the cards, then wait 8 s
   - capture `console` + `pageerror` and whether `#blazor-error-ui` / the circuit error title becomes visible
2. **Get the server stack trace** from the Web log (the unhandled exception that kills the circuit). Put the exception type + top frames in the PR body.
3. **Fix the root cause** where the trace points. Examples of what the trace may show (don't pre-apply any of them):
   - an exception inside the fire-and-forget `EnsureDesktopMatchDeckAsync` after `LoadAsync` (state change/`StateHasChanged` off the renderer's sync context → wrap in `InvokeAsync`, handle exceptions and log instead of letting them escape)
   - a `FormatException` from a resource with the wrong placeholders for the arguments given
   - a null `VacancyId` / travel value in `FormatTopMatchTravel`
   - an `ElementReference`/component ref used before render
   - a JS interop call to a function that doesn't exist on desktop
4. Also make the fire-and-forget calls **safe by construction**:
   - one private helper `RunDetachedAsync(Func<Task>, string what)` that awaits the task, catches and logs (`ILogger`, no PII), and never rethrows into the circuit
   - both `_ = EnsureDesktopMatchDeckAsync()` call sites use it

   This is a guard, **in addition** to the root-cause fix, not instead of it.
5. **Regression tests:**
   - a bUnit test that renders the part the root cause lives in (e.g. `TopMatchTile` / the leading fragment / `MatchDeckDialog`) with the data shape that crashed (complete profile: match percentage, travel values, tags as in the seed). It fails before the fix.
   - `Jobsy.Tests/BanenkaartDesktopTopMatchPlaywrightTests.cs` (soft-skips without `JOBSY_E2E_BASE_URL`): the step-1 scenario at 1440 and 1280. It asserts no circuit error, no `pageerror`, and that the top-match tile is visible when the deck has items.
   - if the root cause is a resource format mismatch: a test that runs `Culture.Format` for every `Match.*` key with the argument counts used in code, in all 5 languages.

## 01.4 (c) Idempotent unload → pagehide shim
- Make the inline script in `App.razor` **idempotent**:
  - guard with a flag on `window` (e.g. `if (window.__jobsyPagehideShim) return; window.__jobsyPagehideShim = true;` inside an IIFE)
  - keep the **original native** `addEventListener` / `removeEventListener` once, on the first run
  - never wrap an already wrapped function
- Keep the `WeakMap` so `removeEventListener("unload", fn)` still removes the mapped `pagehide` listener.
- If the script is duplicated elsewhere (e.g. a JS module that re-applies it), apply the same guard there; there must be one shim.
- If the inline script's hash is listed in a CSP (`CspSmokePlaywrightTests`, a CSP header builder), update the hash in the same PR.
- **Tests:**
  - a static guard test: `App.razor` contains the window guard flag, and the patch references the saved native function (not `EventTarget.prototype.addEventListener` read at call time)
  - Playwright `Jobsy.Tests/PagehideShimPlaywrightTests.cs` (soft-skip): open `/`, do **two enhanced navigations** (e.g. to the vacancy detail and back, using links, not `GotoAsync`), and add + remove an `unload` listener after each. It asserts no `pageerror` containing "Maximum call stack size exceeded" and that `EventTarget.prototype.addEventListener.toString()` is unchanged between the first and the last page.

## Tests
- `IsochroneServiceTests` (existing, green) + the new parsing tests (01.2)
- the bUnit regression test + `BanenkaartDesktopTopMatchPlaywrightTests` (01.3)
- the static shim guard + `PagehideShimPlaywrightTests` (01.4)
- `dotnet build`, `dotnet test` green. Run the three Playwright tests against a local run if you can; say so if you couldn't.

## Success criteria
- A real Valhalla payload with decimal contours yields 200 + 3 features; the desktop banenkaart shows real road rings for Fiets/Auto/Lopen (OV stays circle fallback).
- `kandidaat@jobsy.local` at 1440 and 1280: no circuit error within 10 s of the cards rendering; the root cause is named in the PR body and covered by a test that failed before.
- No "Maximum call stack size exceeded" after two enhanced navigations.
- No changes outside the three fixes, their guard helper and their tests.

Done → this hotfix is complete on its own. When running the whole stack, next: `02-fundament.md`.
