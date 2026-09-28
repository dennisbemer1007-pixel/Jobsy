Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 07: Small correctness and security fixes (eval, N+1, CancellationToken, argument exceptions, async void)

**Goal:** fix concrete small bugs found in the review. Each fix goes in its own commit. There is no functional change, except that the first fix removes `unsafe-eval` from the CSP (security hardening).

**Evidence and what to do:**
1. **`eval` via JS interop:** `Jobsy.Web/Components/Pages/Candidate/Applications.razor:321`, `~:447` and `~:459` call `Js.InvokeVoidAsync("eval", ...)` to focus menu items; the selector is built from Guid ids.
   - Replace this with an `ElementReference` + `FocusAsync()`, or add a small named function in `wwwroot/js/app-core.js` (e.g. `window.lobsyFocus.firstMenuItem(panelId)`).
   - **Prefer `ElementReference`**, so no JS change and no `?v` bump is needed (app-core.js has pending ?v PRs).
   - Then check whether anything else needs `unsafe-eval`: `rg -n "eval\(|new Function|InvokeVoidAsync\(\"eval" Jobsy.Web`, plus MapLibre (worker blob; MapLibre GL ≥ 2 does not need eval but needs `worker-src blob:`) and html2canvas (feedback screenshots).
   - Remove `'unsafe-eval'` from `Jobsy.Web/Security/JobsyContentSecurityPolicy.cs:30` (check the exact path with `rg -n "unsafe-eval" Jobsy.Web`) **only after** a Playwright smoke with a CSP-violation listener (`page.on("console")` + `securitypolicyviolation`) on `/` banenkaart (map pins + popup), vacancy detail map, feedback screenshot, login, and a test page. Update `Jobsy.Tests/ContentSecurityPolicyTests.cs:18` and `ZapFindingsTests.cs:63`, which currently pin `unsafe-eval`.
   - If any violation appears, keep `unsafe-eval`, remove only the eval calls, and document why in the PR.
2. **N+1 in the talent pool:** `Jobsy.Infrastructure/Services/TalentPoolService.cs` `ListForEmployerAsync` (~:418-437) loops over up to 100 ids with `FirstAsync` + `ToDtoAsync` per id. Replace this with one query (`Where(ids.Contains)` + the needed `Include`s or a projection) and map in memory, keeping the order. Add a test asserting the same output (order and fields) for a seeded set.
3. **CA2016:** `Jobsy.Infrastructure/Services/WebPushNotificationService.cs:110`: pass `cancellationToken` to `SendNotificationAsync` (the library overload accepts it; check the WebPush package version).
4. **CA2208:** `FlexCommercialService.cs:49-64`: `ArgumentException`/`ArgumentOutOfRangeException` with the wrong or missing `paramName`. Use `nameof(param)` and keep the message text identical, because tests may assert it.
5. **CA5350** (TOTP HMAC-SHA1): RFC 6238 requires it. Add a targeted `[SuppressMessage]` with a justification; do not change the algorithm.
6. **`async void` without try/catch:**
   - `Jobsy.Web/Components/Pages/VacancyDetail.razor:1133`: **wait until the VacancyDetail hero/YouTube facade PR is merged**, then wrap the body in try/catch (log + ignore `JSDisconnectedException`/`ObjectDisposedException`).
   - `Jobsy.Web/Components/Layout/BottomNav.razor:62` (check the path): **wait until the bottom-nav feedback navFeedback.js PR is merged**, then do the same.
   - If those PRs are not merged yet, leave these two out and note it in the PR.
7. `Jobsy.Api/Program.cs:146` CS8602: add a null-check or null-forgiving operator with a reason (the only default-build warning).

**Do not touch:** App.razor; CSS files; `jobMap.js` / `app-core.js` (pending PRs), unless the ElementReference approach is impossible, in which case wait for the ?v PRs; other catch blocks (see prompt 12).

**Verify:**
- Build green; warning count down by at least 3.
- All tests green, including the updated CSP tests.
- Playwright CSP smoke with no violations (attach the log to the PR).
- Manual: on the `/candidate/applications` (`/sollicitaties`) keyboard menu, focus goes to the first item.
- Talent-pool list output is unchanged (test).

**Dependency:** none for items 1–5 and 7. Items 6a/6b wait for the pending PRs listed above.
