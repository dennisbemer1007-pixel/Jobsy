# 01b: Hotfix: employer part OFF by default until phase 2 (`EmployersEnabled` = false)

**Standalone hotfix, not stacked.**
- Branch: `cursor/paspoort-partners-hotfix-werkgevers` from **`origin/acceptatie`**.
- ONE PR into `acceptatie`, titled **"fix(werkgevers): EmployersEnabled default OFF until phase 2"**.
- The PR body starts with "Standalone hotfix (not stacked)".
- Rules: see README.
- Independent of 01 and of 02–09. Either can merge first. If 01b merges before 02, nothing changes for the stack.
- Decision 20 in `beslissingen-nl.md`. Rationale: `ai-act-beoordeling.md` §0 ("Opvallend punt"), §5.2–5.4.

## Goal
Today on `acceptatie` the flag `EmployersEnabled` defaults to **ON** (found at `7aacb6c8`, still so at `3f8f239c`). Employers therefore see:
- match percentages with breakdown on applications (`MatchPercent`, `MatchBreakdownJson`)
- a talent pool with competence/personality scores, e.g. "Stressbestendigheid", plus RIASEC/Holland
- the AI "Wie ben ik" story snapshot sent with applications (`SnapshotWhoAmIJson`, opt-in)

This hotfix turns the whole employer part **OFF by default until phase 2**:
- all data is kept
- employer entry points and routes are hidden when OFF
- candidate flows keep working
- admins can still turn it ON deliberately

**Not in this PR:** the phase-2 employer view will be **redesigned later**, without percentages or personality scores. Matching will be candidate-driven, and employers will only see applicants, in chronological order. That is out of scope for this stack, so do not redesign anything here.

## Facts (checked on `origin/acceptatie` `3f8f239c`)
**Code defaults that are `true` today:**
- `PlatformFeatureSettings.EmployersEnabled = true` (`Jobsy.Core/Entities/PlatformFeatureSettings.cs` ~L85)
- `FeatureFlagSnapshot.Defaults` (`Jobsy.Core/Features/IFeatureFlags.cs` ~L16)
- the `PlatformFeatureSnapshot` record default (`Jobsy.Core/Interfaces/IPlatformFeatureService.cs` ~L33)
- `row?.EmployersEnabled ?? true` (`Jobsy.Infrastructure/Services/PlatformFeatureService.cs` ~L290)
- the API DTO default (`Jobsy.Api/Models/Sprint6Dtos.cs` ~L282)
- `docs/feature-flags.md` (table says **true**)
- the DB column default `true` (migration `20260929120000_AddEmployersAndPassportFeatureFlags`). The existing singleton row on acceptatie is therefore `true`.

**OFF behaviour already exists** (`docs/feature-flags.md`):
- `[RequiresFeature(PlatformFeature.Employers)]` on about 47 Blazor pages (via `FeatureRouteGate` in `Routes.razor`, which redirects to `FeatureRoutes.HomeFor`) and about 30 API controllers (404 `feature_disabled`)
- `<FeatureVisible>` sections
- mixed responses stripped
- employer jobs paused
- `RequiresEmployers` e-mails suppressed
- nav catalog per flag
- `/access-denied?reason=employers-off` view with copy in 5 languages

**Known gaps:**
1. **`IEmployersSwitch` is still `AlwaysOnEmployersSwitch`** (`Jobsy.Web/Program.cs` ~L117). The middlewares `LandingRedirectMiddleware`, `BanenkaartGateMiddleware` and `BanenRedirectMiddleware`, plus `NotFoundView`, `LandingVariantResolver` and `PartnerFlyerEndpoints`, therefore ignore the flag. See `docs/feature-flags-landing-followup.md` items 1–4.
2. **Pages without the gate whose APIs are gated** (they would render broken forms when OFF):
   - `/sales`, `/sales/*` and the legacy `/salesmanager/*` redirects (`Pages/Sales/*`, role SalesManager)
   - `/register/bedrijf`, `/register/koppelen`, `/register/toegang`, `/register/verifieren`, `/register/verifieren/brief`
   - `/home/metrics/{Key}` (`MetricDrilldownPage`, employer roles + Admin)
   - Verify each one; the list may not be complete.
3. The Polish `Status.Forbidden.EmployersOffLead` has a typo ("Dam y znać").

**Production** (`origin/main` `9a2c0d49`, which `render.yaml` deploys to production, autoDeploy on commit):
- There is **no** `EmployersEnabled` flag and no `IFeatureFlags`. The employer part is always on and cannot be switched off.
- `/employer/talent` shows competency % incl. Stressbestendigheid, and `/branch/applicants` shows match %. There is no WhoAmI snapshot on main.
- When acceptatie is promoted to main, the migration in this hotfix makes production OFF as well. Without this hotfix it would stay ON (column default `true`).

## Scope

### 1. Defaults → OFF
- Set every code default listed above to `false`. Keep `CandidatePassportEnabled` untouched.
- New migration **`SetEmployersDefaultOff`**, following the pattern of `20261002234904_SetCandidatePassportDefaultOn`:
  - `AlterColumn` default `false`
  - **one-shot** `UPDATE "PlatformFeatureSettings" SET "EmployersEnabled" = FALSE;`, with a comment saying this is Dennis's decision 20 (03-10-2026)
  - `Down` restores only the column default and never flips row data
- **No data is deleted or modified:** vacancies, applications, talent pool, tokens, companies and WhoAmI snapshots stay as they are.
- Update `docs/feature-flags.md`: default **false**, plus one line saying phase 2 is redesigned and out of scope.
- **Admin toggle** (`PlatformSettingsCatalog` / `PlatformSettingsEditor`): turning it ON shows a confirm dialog in 5 languages. The NL copy is: "Fase 2 wordt opnieuw ontworpen. Als je dit aanzet, zien werkgevers weer matchpercentages, persoonlijkheidsscores in de talentpool en het AI-verhaal. Weet je het zeker?" Log the change in `AdminAuditLog`.

### 2. Make the switch real
- Replace the `AlwaysOnEmployersSwitch` registration with an adapter over `IFeatureFlags` (follow-up doc item 1). Keep `AlwaysOnEmployersSwitch` and `FixedEmployersSwitch` for tests only, or delete the former if unused.
- Apply follow-up doc items 2–3:
  - anonymous `/` while OFF renders the landing **-zw** variant; canonical stays `/`
  - `FeatureRoutes` home amendments
- Item 4 (replace `EmployersGate` with `RequiresFeature`) only if it is small. Otherwise leave it and note it in the report.

### 3. Close the gaps (hidden entry points/routes when OFF)
- Add `[RequiresFeature(PlatformFeature.Employers)]` to every page in gap 2, and to any other page whose backing API controller or action is Employers-gated.
- Add a guard test (below) so this cannot regress.
- **Friendly page instead of a broken page.** Add a new page `/werkgevers/binnenkort` (anonymous, `noindex`, 5 languages) with this copy:
  - title "Voor werkgevers: binnenkort"
  - lead "Lobsy is nu eerst voor kandidaten. De omgeving voor werkgevers komt terug in een volgende fase."
  - CTA to `/` and, for signed-in candidates, to their home
- Use it as `FallbackPath` for employer-only pages:
  - `/employer/*`, `/werkgever/*`, `/branch*`, `/regional*`, `/intermediary*`, `/register/bedrijf*`, `/register/*` company flows
  - `/sales*`, `/ambassadeur*`, `/partner*` (affiliate), `/tokens/checkout-*`
  - Employer-side-only **signed-in** users keep going to `/access-denied?reason=employers-off` via `HomeFor` (existing). Align that copy with "binnenkort", and fix the PL typo.
- Candidate-facing vacancy pages (`/candidate/match|vacancies|liked|shared|applications|talent-contacts`, `/banenkaart`, `/vacancies/{id}`) keep the existing redirect to the candidate home. Candidates are not sent to a "werkgevers" page.
- Server-side, an unknown employer path → normal 404.
- **API:** unchanged (404 `feature_disabled`).
- The nav and footer contain no employer links when OFF (existing `FeatureVisible`; verify `MainLayout`, `AppFooter` and the landing CTAs).

### 4. Candidate flows must not break (flag OFF)
Smoke-test all of these with the flag OFF:
- sign-up (`/account-maken`, code), onboarding (`/candidate/start`)
- ontdekkingsreis, the 4 tests
- `/candidate/paspoort` (all tabs; the Fit tab uses `.Zw` copy where present)
- `/carriere`, deep analysis, WhoAmI (candidate-only)
- own Lobsy-CV download, GratisDna `/ontdek`, the school flows

Check that:
- no page calls a gated API without handling 404 `feature_disabled` (no error toasts, no empty broken cards)
- `CandidateKompas`, `RoleFitCheck` and `CandidateMetrics` return the stripped variants (existing)
- the candidate home follows `FeatureRoutes.HomeFor`

## Tests
- **Update tests that assert ON defaults:**
  - `FeatureFlagFoundationTests` (~L33/L43/L73–78/L105): defaults now false
  - `AdminDashboardTeDoenTests` (~L485)
  - test factories (`ErrorPagesWebFactory`, `ForbiddenWebFactory`) keep an explicit value per test, with no implicit ON
  - any test that relied on the default ON sets `EmployersEnabled: true` explicitly
- **Migration test:** after migrating, an existing row is false, a new row is false, and `Down` does not touch the row value.
- **Guard test (reflection):**
  - every routable component under `Pages/{Employer,Werkgever,Intermediary,Sales,Ambassadeur,Partner}` (branch/regional pages live in these folders) plus the `Register{Bedrijf,Koppelen,Toegang,Verifieren,VerifierenBrief}` pages and `MetricDrilldownPage` carries `RequiresFeature(PlatformFeature.Employers)`
  - every API controller under the same domain list in `docs/feature-flags.md` is gated
- **Switch adapter tests:** OFF → anonymous `/` renders -zw; `/banenkaart` → redirect; `/werkgevers/binnenkort` renders with noindex.
- **bUnit:** admin confirm dialog when switching ON; nav for a candidate has 3 items (passport ON / employers OFF).
- **Playwright 390×844 + 1440×900** (soft-skip):
  - flag OFF (the default on acceptatie after migration): anonymous `/employer/talent`, `/branch/applicants`, `/werkgever/sollicitaties`, `/register/bedrijf` and `/sales` → "binnenkort" page
  - the candidate journey (login → paspoort → each tab → carrière) has no console errors, no `feature_disabled` toasts and no links to employer routes (collect all `a[href]` and assert none point at gated prefixes)
  - no horizontal overflow; `ar` renders rtl
- **Existing employer Playwright suites** (`WerkgeverSmokePlaywrightTests`, `CandidateInsightsPlaywrightTests`, `E2e/CareerE2e` employer parts, …):
  - read `GET api/settings/feature-flags` first
  - when `employersEnabled` is false, **soft-skip** with the reason "Employers OFF (decision 20)"
  - or toggle ON/OFF around the test if `JOBSY_E2E_ALLOW_FEATURE_TOGGLE` is set, restoring OFF afterwards
- Full `dotnet test -c Release`; Release build with 0 warnings.

## Success criteria
- Fresh installs, acceptatie after migration, and production after promotion all run with the employer part **OFF**.
- No employer sees match %, personality scores or the AI story, because employer routes are unreachable. All data is intact.
- Candidates' flows work without errors. Admins can deliberately turn it ON, behind a confirm dialog.
- Release build with 0 warnings, tests green.

## Out of scope
- The phase-2 employer redesign (candidate-driven matching, chronological applicants, no percentages/personality scores).
- Deleting or migrating employer data.
- Changing `ProfileVacancyMatchCalculator` or the talent pool.
- Production deploys or main merges (never).
- The passport-partner portal. It is **not** behind `EmployersEnabled`; see step 07.
