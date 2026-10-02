# 06: Passport nav ON by default (everywhere, incl. production)

**Stacked.**
- Branch: `cursor/kandidaat-polish-6` from `cursor/kandidaat-polish-5`.
- ONE PR into `acceptatie`, titled **"feat(nav): passport layout on by default (Ontdekkingsreis · Paspoort · Zoeken · Sollicitaties · Carrière)"**.
- Rules: see README.

## Why
- The passport nav is merged in `Jobsy.Web/Navigation/RoleNavCatalog.cs` (`CandidateItems(FeatureFlagSnapshot)`), but it isn't live: `CandidatePassportEnabled` defaults to **false** (`Jobsy.Core/Features/IFeatureFlags.cs` L16, entity `PlatformFeatureSettings.CandidatePassportEnabled`, migration `20260929120000_AddEmployersAndPassportFeatureFlags` with `defaultValue: false`).
- **Dennis (02-10 17:09): the new profile/passport layout must be ON BY DEFAULT everywhere, including production.** The admin switch stays, so it can be turned off.
- With the flag on, the order is De ontdekkingsreis · Mijn Paspoort · Carrière · Banenkaart · Sollicitaties. Dennis wants **Ontdekkingsreis · Mijn Paspoort · Zoeken · Sollicitaties · Carrière**.
- The admin switch (`Jobsy.Web/Admin/PlatformSettingsCatalog.cs` ~L130–157, group `platform-mode`, `/admin/instellingen`) shows **raw keys**. None of these strings exist: `AdminSettings.Employers.Enabled.Title`, `.Desc`, `.ImpactOff`, `AdminSettings.Passport.Enabled.Title`, `.Desc`.
- History of Match in the nav:
  - added as a bottom-nav tab in `2dda3f8d` (23-09)
  - replaced in `160f012d` (24-09) by the 5-item nav
  - since then it has been a button on the map
- **Match stays a button on the map** (02 makes it visible). It does **not** go in the nav.

## Scope
1. **Nav order.** In `CandidateItems`, with passport ON and employers ON, return:
   `[DiscoveryItem, PassportItem, SearchItem, ApplicationsWithSavedAliases, CareerItem]`
   - "Zoeken" uses the existing `SearchItem` (`Nav.Search`, `/banenkaart`, alias `/`). Keep `BanenkaartItem` only if something else still uses it; otherwise remove it, and its doc comment.
   - Passport ON + employers OFF stays `[Discovery, Passport, Career]`.
   - Passport OFF keeps today's legacy order (D1).
   - Update the doc comments on `CandidateItems` / `ShowsSavedInNav`.
2. **Bewaard is a tab under Sollicitaties** (passport ON):
   - `/candidate/liked` and `/candidate/shared` keep working and mark **Sollicitaties** active (they are already aliases in `ApplicationsWithSavedAliases`).
   - On those pages, show `CandidateJobListTabs` (**Sollicitaties | Bewaard {n}**) at the top. Under it, on the Bewaard pages, show the **Bewaard / Gedeeld** sub-toggle (`CandidateSavedSubnav`) as pills, as in mockup C.
   - The h1 stays "Bewaard".
   - With passport OFF, nothing changes.
3. **Old Bewaard links redirect.** Add redirects (302, query string kept) from legacy or guessed URLs to the tab:
   - `/bewaard` → `/candidate/liked`
   - `/candidate/saved` → `/candidate/liked`
   - `/candidate/bewaard` → `/candidate/liked`
   - add any other old Bewaard route you find in `git log -S` or `docs/ROUTES.md`
   - Links inside the app (Match "Bewaard", toast "Bekijk", e-mails) point to `KbRoutes.Saved`.
   - Update `docs/ROUTES.md`.
4. **Admin strings.** Add these to `Jobsy.Web/Localization/UiStringsAdmin.cs` next to `AdminSettings.Group.PlatformMode` (~L394), in **nl/en/pl/ro/ar**, plain language:
   - `AdminSettings.Employers.Enabled.Title`: "Werkgevers actief"
   - `AdminSettings.Employers.Enabled.Desc`: "Kandidaten zien banen, de banenkaart en sollicitaties. Werkgevers kunnen vacatures plaatsen."
   - `AdminSettings.Employers.Enabled.ImpactOff`: "Uit: kandidaten zien geen banen meer. Werkgevers kunnen niet plaatsen."
   - `AdminSettings.Passport.Enabled.Title`: "Mijn Paspoort en Ontdekkingsreis"
   - `AdminSettings.Passport.Enabled.Desc`: "Nieuwe navigatie voor kandidaten: Ontdekkingsreis, Mijn Paspoort, Zoeken, Sollicitaties, Carrière. Bewaard wordt een tab onder Sollicitaties. Staat standaard aan."
   - Add a guard test: **every** `TitleKey` / `DescriptionKey` / `ImpactKey` in `PlatformSettingsCatalog` resolves to a real string in all 5 languages (no raw key).
5. **Passport layout ON by default everywhere, including production** (Dennis, 02-10 17:09).
   - The admin switch stays (`/admin/instellingen`, group `platform-mode`), so an admin can still turn it **off**.
   - **Code defaults to `true`.** Change every place that falls back to `false`:
     - `Jobsy.Core/Features/IFeatureFlags.cs` L16: `FeatureFlagSnapshot.Defaults`, set `CandidatePassportEnabled: true`
     - `Jobsy.Core/Entities/PlatformFeatureSettings.cs` ~L86–90: `public bool CandidatePassportEnabled { get; set; } = true;` and fix the doc comment ("Default true")
     - `Jobsy.Core/Interfaces/IPlatformFeatureService.cs` ~L34: the snapshot record parameter default `CandidatePassportEnabled = true`
     - `Jobsy.Infrastructure/Services/PlatformFeatureService.cs` ~L291: `row?.CandidatePassportEnabled ?? true` (no row means ON)
     - `Jobsy.Infrastructure/Services/DeviceSessionService.cs` ~L581: `?? true`
     - `Jobsy.Infrastructure/Services/CandidateOnboardingService.cs` ~L307: the fallback already reads `FeatureFlagSnapshot.Defaults`, so it follows automatically. Check it.
     - `Jobsy.Web/Admin/PlatformSettingsCatalog.cs` ~L146–157 (descriptor) and ~L391–445 (update builder): the descriptor reads the stored row, so make sure nothing there treats a missing value as `false`. If the catalog has a "default" / "reset to default" notion for this setting, it must be `true`.
     - `Jobsy.Core/Admin/PlatformModeSummary.cs` ~L24: the comment says the passport field stays out of the summary. Add it now that the field exists, showing ON/OFF.
     - Run `rg -n "CandidatePassportEnabled" Jobsy.Core Jobsy.Infrastructure Jobsy.Web --glob '!*Migrations*'` and check every hit.
   - **Stored rows must not keep it OFF by accident.**
     - Migration `20260929120000_AddEmployersAndPassportFeatureFlags` added the column with `defaultValue: false`, so **every existing `PlatformFeatureSettings` row (acceptatie and production) stores `false`**, even though nobody chose it. Until now the admin switch showed a raw key, so no deliberate choice was possible.
     - Add a new EF migration **`SetCandidatePassportDefaultOn`** (timestamp after `20261002121134_DropDeepAnalysisPriceEuro`, or after the newest one on your base) that:
       1. changes the column default: `AlterColumn<bool>("CandidatePassportEnabled", "PlatformFeatureSettings", defaultValue: true, oldDefaultValue: false)`
       2. flips existing rows **once**: `migrationBuilder.Sql("UPDATE \"PlatformFeatureSettings\" SET \"CandidatePassportEnabled\" = TRUE;")`
       3. `Down`: restores `defaultValue: false` only. It does **not** flip data back; write a comment explaining why.
     - Update `JobsyDbContextModelSnapshot.cs` (~L4357) with `.HasDefaultValue(true)` (and the entity config, if the property is configured there) so the model and the migration agree. `dotnet ef migrations has-pending-model-changes` must report none.
     - If startup seeds a `PlatformFeatureSettings` row when none exists, make sure the seed uses the entity default (`true`) and doesn't set `false` explicitly.
     - Because the migration runs only once, an admin who turns it off later keeps it off. Nothing re-enables it at startup.
   - **Side effects to check and report in the PR** (don't change their logic):
     - With passport ON, `FeatureRoutes.CandidateHome` (`Jobsy.Core/Features/FeatureRoutes.cs` ~L74–84) sends a candidate whose `CandidateOnboarding.CompletedAtUtc` is null to `/candidate/ontdekkingsreis`, and a ready one to `/candidate/paspoort`.
     - `DeviceSessionService.ResolveShowCandidateHowToAsync` (~L573–590) follows the same rule.
     - Confirm with a test that an **existing** candidate without an onboarding row can still log in, lands on De ontdekkingsreis and can reach Zoeken, Sollicitaties and Carrière. Nobody gets locked out.
6. **Do not** add Match to the nav. Check that `MainLayout.razor` (~L224, `candidate/match` path handling) still marks the right item (Zoeken) when on `/candidate/match`.

## Files to touch
- `Jobsy.Web/Navigation/RoleNavCatalog.cs` (items ~L36–90, `CandidateItems` ~L116–148, `ShowsSavedInNav` ~L151)
- `Jobsy.Web/Components/Layout/MainLayout.razor` (active item logic ~L224)
- `Jobsy.Web/Components/Candidate/CandidateJobListTabs.razor`, `Jobsy.Web/Components/Candidate/CandidateSavedSubnav.razor`
- `Jobsy.Web/Components/Pages/Candidate/Liked.razor`, `Jobsy.Web/Components/Pages/Candidate/Shared.razor`, `Jobsy.Web/Components/Pages/Candidate/Applications.razor` (the tabs on the Sollicitaties page)
- `Jobsy.Web/KandidaatBanen/KbRoutes.cs`, `Jobsy.Web/Program.cs` (redirects), `docs/ROUTES.md`
- `Jobsy.Web/Localization/UiStringsAdmin.cs` (~L394)
- `Jobsy.Web/Admin/PlatformSettingsCatalog.cs` (only if a key name has to change; prefer adding the strings)
- `Jobsy.Core/Features/IFeatureFlags.cs` (default **true**), `Jobsy.Core/Entities/PlatformFeatureSettings.cs`, `Jobsy.Core/Interfaces/IPlatformFeatureService.cs`, `Jobsy.Core/Admin/PlatformModeSummary.cs`
- `Jobsy.Infrastructure/Services/PlatformFeatureService.cs`, `Jobsy.Infrastructure/Services/DeviceSessionService.cs`, `Jobsy.Infrastructure/Services/CandidateOnboardingService.cs` (check)
- New migration `Jobsy.Infrastructure/Data/Migrations/<timestamp>_SetCandidatePassportDefaultOn.cs` (+ `.Designer.cs`), `JobsyDbContextModelSnapshot.cs`
- Tests (update on purpose and say so in the PR). Tests that build a snapshot assuming passport `false` by default must be updated: `FeatureFlagFoundationTests.cs`, `HowLobsyPageTests.cs`, `PartnerPageTests.cs`, `StatusPageTests.cs` (they pass `CandidatePassportEnabled: false` explicitly; keep that where the test is about the OFF case, and add an ON case where the default matters), `RoleFunctionalRegressionTests.cs`, `Werkgever/RoleNavCatalogEmployerEmptyTests.cs`, `PassportBunitTests.cs`, `CandidateLandingTests.cs`, `PlatformSettingsCatalogTests.cs`, `NavFeedbackPlaywrightTests.cs`

## Tests
- **Unit:**
  - `CandidateItems` returns exactly Discovery · Passport · Search · Applications(+saved aliases) · Career for passport ON + employers ON
  - the other three flag combinations are unchanged
  - Match is never in any candidate nav
- **Unit:** the catalog key guard (every admin setting key resolves in nl/en/pl/ro/ar).
- **Unit:** `FeatureFlagSnapshot.Defaults.CandidatePassportEnabled == true`, `new PlatformFeatureSettings().CandidatePassportEnabled == true`, and `PlatformFeatureService` with **no row** reports passport ON.
- **Migration test** (the repo's migration/integration test pattern, Postgres or the existing test DB):
  - a row with `CandidatePassportEnabled = false` is `true` after `SetCandidatePassportDefaultOn`
  - a newly inserted row without the column set defaults to `true`
  - after the migration, the admin turns it OFF via `PlatformFeatureUpdate(CandidatePassportEnabled: false)` and it stays OFF, because no startup code re-enables it
- **Admin:** the `CandidatePassportEnabled` descriptor in `PlatformSettingsCatalog` reads ON for a default row, and writing OFF works.
- **Existing candidate without onboarding:** logs in, home is `/candidate/ontdekkingsreis`, and the nav shows all 5 items.
- **Redirects:** `/bewaard`, `/candidate/saved` and `/candidate/bewaard` return 302 to `/candidate/liked`, with the query kept.
- **bUnit:** on `/candidate/liked` with passport ON, `CandidateJobListTabs` shows "Sollicitaties | Bewaard" with Bewaard active, and the Bewaard/Gedeeld pills are present.
- **Playwright 390×844** (passport ON):
  - the bottom nav shows 5 items in the agreed order with no overflow
  - tapping Sollicitaties, then the Bewaard tab, shows the compact list from 05
  - the Match button is on the map

## Success criteria
- **By default (fresh database, existing acceptatie data and existing production data after the migration)**, candidates see Ontdekkingsreis · Mijn Paspoort · Zoeken · Sollicitaties · Carrière. Bewaard is a tab under Sollicitaties, and old Bewaard links land there.
- An admin can turn the passport layout OFF in `/admin/instellingen`, and it stays OFF.
- The admin switches show real titles and descriptions in all 5 languages.
- Match is not in the nav.
- No horizontal overflow at 390. Release build with 0 warnings, full tests green.
