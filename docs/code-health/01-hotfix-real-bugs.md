# 01: Hotfix: real bugs found through warnings (standalone)

- **Branch:** `cursor/code-health-01-hotfix`, from `origin/acceptatie`.
- **PR:** into `acceptatie`, title "Code health 01: real-bug hotfix (standalone)".
- **Not stacked.** It must merge on its own.
- One commit per item (§1–§7). Follow the global rules in [00-README](00-README.md).

## §1 RZ10012: components rendered as raw HTML tags

**Finding:**
- Razor reports 35× `RZ10012 Found markup element with unexpected name '<X>'`. These components **exist**, but their namespaces are not imported anywhere, so the browser receives literal `<InsightsStoryCard …>` tags and the sections stay empty.
- Affected markup:
  - `Jobsy.Web/Components/Pages/Werkgever/CandidateInsights.razor` (33 places):
    - from `Components/Employer/`: `EmployerTalentTabs`, `InsightsStoryCard`, `InsightsRankedList`, `InsightsDistributionBars`, `InsightsTipsBlock`, `InsightsVacanciesList`
    - from `Components/Werkgever/Insights/`: `WgInsightsKpiLocked`, `WgLockedCard`, `WgInsightsPremium`
  - `Jobsy.Web/Components/Employer/InsightsLockedBlock.razor:4`: `WgLockedCard`
  - `Jobsy.Web/Components/Admin/Sections/CompanyDetailsSection.razor:168`: `RaamflyerTools` (from `Components/Employer/`)

**Change:**
1. Add these to `Jobsy.Web/Components/_Imports.razor`:
   - `@using Jobsy.Web.Components.Employer`
   - `@using Jobsy.Web.Components.Werkgever.Insights`
   - While there, remove the duplicate `@using Jobsy.Web.Features` on line 41 (CS0105).
2. Check for name clashes: the build must stay free of `RZ9985`/ambiguity errors. If a clash appears, use a fully qualified tag in that one place instead.
3. Make RZ10012 a hard error right away, because it is always a bug: in `Jobsy.Web/Jobsy.Web.csproj` add `<WarningsAsErrors>$(WarningsAsErrors);RZ10012</WarningsAsErrors>`.

**Verify the pages, because the components will now actually render:**
- bUnit tests:
  - Render `CandidateInsights` (locked and unlocked DTOs) and `InsightsLockedBlock`. Assert the markup contains no `<insightsstorycard`, `<wglockedcard`, `<insightsdistributionbars` (case-insensitive) and that the expected CSS hooks exist (`insights-story-card`, `wg-locked-card`, …).
  - Render `CompanyDetailsSection` and assert `RaamflyerTools` output is present, not the raw tag.
- Playwright (desktop 1366×900 and mobile 390×844) on a local stack with the Employers feature ON:
  - Log in as a demo BranchManager, open `/werkgever/kandidaatinzichten`, then admin → company details.
  - Take screenshots. Check there are no console errors and no CSP violations.
  - Attach the screenshots to the PR.
- Check for new runtime exceptions (missing `[Parameter]`s or injected services) now that the components really render. Fix them in this PR.

## §2 Lockout email: duration not shown, banned copy

**Finding:**
- In `Jobsy.Core/Email/TransactionalEmails.Templates3.cs` `AccountLockout(...)` (~l.197–222), `durationLabel` is computed but never used (IDE0059). `brand` is unused too.
- P1 hard-codes "5 keer" instead of using `failedAttempts`.
- The NL CTA "Nieuw wachtwoord kiezen" and the EN CTA "Choose a new password" break the copy rule in `AccountEmailCopyTests.No_entra_sbi_or_lobsy_subject_suffix_or_nieuw_wachtwoord`.
- These pre-existing tests fail because of it:
  - `AccountEmailCopyTests.AccountLockout_duration_matches_login_lockout_rules` (6 cases)
  - `AccountEmailCopyTests.No_entra_sbi_or_lobsy_subject_suffix_or_nieuw_wachtwoord`
  - `EmailSnapshotTests.Structural_snapshots_for_every_registry_key` (AccountLockout.nl/en/ar drift; PasswordChanged.en drift, check separately)

**Change:**
1. Show the duration and the number of attempts:
   - `Email.AccountLockout.P1` becomes e.g. NL "Er is {1} keer een verkeerd wachtwoord ingevuld. Daarom kun je {2} niet inloggen met je wachtwoord, tot {0}."
   - Pass `EmailArg.Plain(durationLabel)` and the attempt count.
   - Do the same in all 5 languages (`EmailStrings.{Nl,En,Pl,Ro,Ar}.cs`).
   - Keep "B1" plain language. **No literal minute or day counts in the strings**; `CandidateEmailCopyTests` and `EmployerEmailCopyTests` scan for `\d+ (minuten|dagen)`.
2. CTA without "nieuw wachtwoord" / "new password": NL "Wachtwoord vergeten?", EN "Forgot your password?", with equivalents in pl/ro/ar. The link stays `/wachtwoord-vergeten`.
3. Remove the unused `brand` local.
4. Regenerate the snapshots with `JOBSY_UPDATE_EMAIL_SNAPSHOTS=1` **only** for `AccountLockout.*`, and review the diff. Investigate `PasswordChanged.en` separately: if its drift is unrelated, leave it and say so in the PR.
5. Run `git clean -fdq Jobsy.Tests/EmailSnapshots` before committing so that no stray `.received` files get committed.

**Acceptance:**
- The 7 AccountEmailCopy cases above pass.
- The AccountLockout snapshot is updated and reviewed.
- A new test asserts the NL lockout text contains the duration label for 15 min and for 4 h (`EmailFormat.Duration`).

## §3 `/melden`: chosen reason lost after a validation or server error

**Finding:**
- `Jobsy.Web/Components/Pages/Public/Melden.razor` is a static SSR form.
- `Jobsy.Web/Hosting/ContentReportEndpoints.cs` redirects back with `&fout=opnieuw|reden|teveel`, and `Program.cs` ~l.299–308 adds the rate-limit `&fout=teveel`.
- `_selectedReason` is never set (CS0649), so after an error the visitor has to choose the reason again.

**Change:**
- On the error redirects, add `&reden=<enum name>` when a valid reason was posted. Do **not** put the free-text details in the URL (privacy).
- In `OnInitializedAsync`, parse `reden` with `Enum.TryParse<ContentReportReason>(…, ignoreCase: true)` into `_selectedReason`.

**Tests:**
- Endpoint test: POST with a valid reason that hits "opnieuw", then assert the `Location` contains `reden=`.
- bUnit test: `/melden?type=vacancy&id=…&fout=opnieuw&reden=Scam` renders that radio as `checked`.
- An invalid `reden` value is ignored.

## §4 Payout export: possible null `CreditorName`

**Finding:**
- `Jobsy.Infrastructure/Sales/SalesPayoutRunService.cs` ~l.745–755 (CS8604). `holder` falls back to `profile.CompanyName`, which can be null, and is then passed as `CreditorName` into the SEPA export line.

**Change:**
- If both `PayoutAccountHolderName` and `CompanyName` are blank, fail that line the same way as a missing IBAN (`InvalidOperationException` with a Dutch message like the IBAN one). Better still, add the line to the run's "skipped/needs attention" list if the service has one; check the existing pattern.
- Never emit an empty creditor name.

**Test:** a unit test with a profile that has an IBAN but no holder and no company name → it is not exported and the reason is visible. The normal case is unchanged.

## §5 `stackalloc` inside a loop (CA2014)

**Finding:** `Jobsy.Infrastructure/Scholen/PupilCodeService.cs` ~l.84 and ~l.162 `stackalloc char[PupilCodeFormat.Length]` sit inside `do … while` retry loops of up to 100 attempts, which grows the stack.

**Change:** declare the `Span<char>` buffer once, before the loop, and reuse it.

**Tests:** the existing PupilCode tests stay green. Add one test that forces collisions (a stub `used` set) for 50+ iterations without throwing a stack-related error.

## §6 OpenStreetMap attribution restored (ODbL)

**Finding:**
- The maps use OpenFreeMap tiles (OpenStreetMap data, ODbL), but the attribution is removed everywhere:
  - `Jobsy.Web/wwwroot/js/jobsyMapLibre.js` ~l.127: removes `.maplibregl-ctrl-attrib` etc.
  - ~l.319: `attributionControl: false`
  - ~l.336: empty `"AttributionControl.ToggleAttribution"`
  - `Jobsy.Web/Components/App.razor:51`: inline CSS `display:none !important`
  - `Jobsy.Web/wwwroot/css/app.css` ~l.3633–3640: `.job-map` / `.vacancy-detail-map .maplibregl-ctrl-attrib`
- The OSM licence requires visible attribution: "© OpenStreetMap contributors", linking to https://www.openstreetmap.org/copyright. OpenFreeMap asks for a credit too.

**Change:**
1. Enable a **compact** attribution on every MapLibre map (Banenkaart / job map, vacancy detail map, Kandidaatinzichten map `wwwroot/js/features/kandidaatinzichten-map.js`): `attributionControl: { compact: true, customAttribution: '<a href="https://openfreemap.org" target="_blank" rel="noopener">OpenFreeMap</a> © <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>' }`.
   - It may be styled small and low-contrast to match the brand, but it must be visible without interaction on desktop. On mobile the compact "i" toggle is acceptable.
   - Keep the MapLibre logo hidden; that is allowed.
2. Remove the attribution-hiding rules (keep the logo-hiding ones). Give the toggle a localized `aria-label` via the existing locale object, instead of the empty string.
3. Make sure the attribution does not overlap the bottom nav, the carousel or the Match button on 390×844. Adjust the `bottom` offset with a CSS variable if needed.
4. Rebuild `jobMap.min.js` / `jobsyMapLibre.min.js` / `vacancyDetailMap.min.js`. No minifier is documented yet (`git log` the `.min.js` files for hints). If none is found, use a pinned `npx terser@5 --compress --mangle` and document the exact command in `docs/performance.md`. Bump the `?v=` strings.

**Tests:**
- Playwright on `/banenkaart` and on a vacancy detail page, desktop and mobile: `.maplibregl-ctrl-attrib` is visible (or the compact button is), and contains "OpenStreetMap". Include screenshots.
- Existing map Playwright suites (Banenkaart*, JobMapPinsClusters*) are still green.

## §7 npm: `image-size` vulnerability via `pptxgenjs` (root `package.json`)

**Finding:**
- The root `package.json` has only `pptxgenjs ^4.0.1` and **no lockfile**. It is used only by `docs/demo/build-pptx.cjs`, a local demo deck that isn't part of the app, the Docker images or CI.
- `npm audit` reports 2× **high** through `image-size` 1.2.1 (GHSA-5p2g-fcmc-qvqq, GHSA-w3rx-r6r6-pgpr: denial of service through infinite loops; fixed in ≥ 2.0.3, latest 2.0.4).

**Change:**
1. Add `"overrides": { "image-size": "^2.0.4" }`, run `npm install --package-lock-only`, and commit the new `package-lock.json`.
2. Run `node docs/demo/build-pptx.cjs` (after `npm ci`) and check that the deck still builds **with its images**. image-size 2 changed its API (`imageSize` named export).
3. If it breaks, use the fallback: move the script and its `package.json` to `tools/demo-deck/` with its own lockfile, pin a pptxgenjs version whose audit is clean, and document that in `docs/demo/README.md`.
4. Target: `npm audit` shows 0 vulnerabilities at the root and in `tools/css`.

## Acceptance (whole PR)

- Release build OK, with **no RZ10012 left** (now an error).
- Unique warnings: full count ≤ 470 − 35 (RZ10012) − 1 (CS0105) − the IDE0059/CS0649/CS8604/CA2014 hits fixed here. Report the exact number.
- Full test suite: the previously failing AccountLockout and copy tests (7) and the AccountLockout snapshot now pass. **No new failures** compared with Appendix A in [11](11-gate-warnings-as-errors.md).
- Fresh-DB migration test, PendingModelChanges and the API smoke test pass. This step adds no migration.
- Screenshots attached for §1 and §6.
