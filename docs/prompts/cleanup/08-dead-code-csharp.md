Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 08: Remove dead C# and Razor code (certain candidates only)

**Goal:** delete code that is provably unused. No behaviour change.

**Evidence (docs/review/code-review.md §1; data in `docs/review/data/analyzer-unused-and-bugs.txt` and `unused-members-heuristic.txt`):**
- **Unused private members (IDE0051):**
  - `ApplicationsController` Html helper
  - `VacanciesController` resolver helper
  - `DemoUsersSeeder` helper
  - `CandidateInsightsService.ReadValues`
  - `PrivacyDataController` Html helper
  - `TrainingUpskill` `CombineUrl`
  - `AuthServiceCollectionExtensions.TryLocalApiLoginAsync` (:782)
- **Write-only fields (IDE0052):** `DeviceSessionsController._db` (:19), `LobsyCvPdfService.CellOff` (:26).

  Re-run the analyzer to get exact lines:
  ```
  dotnet build -p:AnalysisMode=Recommended 2>&1 | grep -E "IDE0051|IDE0052" | grep -v Jobsy.Tests
  ```
- **Unused components** (no `@page`, no tag usage, no `typeof`/`DynamicComponent`): `MatchMobileLayout.razor`, `KompasTabBar.razor`, `ValuesScorePanel.razor`, `WhoAmIPanel.razor`.
  - Tests read the source of `WhoAmIPanel` (`rg -n "WhoAmIPanel" Jobsy.Tests tools`, incl. `UatScriptRunner`); update or remove those asserts.
  - `.cursor/rules/design-system.mdc` mentions MatchMobileLayout; update that text.
- **Unused public helpers** (0 production callers): see `unused-members-heuristic.txt`, e.g. `JobsyRoles.CanCreateVacancies`, `DreamJobCatalog.FindByKey/FindByTitle`, `DashboardMemoryCache.ScopePrefix`.
  - Only delete a helper when `rg -w <Name>` over the whole repo (incl. `.razor`, tests, tools) shows no callers.
  - If only tests call it, delete the helper **and** the test only when the test exists solely for that helper; otherwise keep it.
- **Unused `JobsyApiClient` methods** (e.g. `GetMyKompasDnaAsync` :1362 and others in §1.3): **wait until the SalesWalletChip/API-client PR is merged** (it edits JobsyApiClient.cs), then re-verify and delete.
- `VacancyDiscovery.razor` `IsActiveOpenManagedVacancy`: **wait until the VacancyDiscovery VacancyCard PR is merged**, then re-verify.
- Legacy redirect-only pages (`/admin/cockpit` etc., §1.9): **keep them** (bookmarks). Only add a comment.

**Do:** delete the items above in small commits (1 commit per group); re-run the analyzer; update the warning baseline from prompt 01 downwards.

**Do not touch:** anything reachable via reflection, DI registration, EF (entities/properties, even if seemingly unused), JSON contracts/DTO properties (serialized), `[JSInvokable]` methods, SignalR hubs, UiStrings keys (prompt 14), CSS/JS (prompts 09/10), migrations.

**Verify:**
- Build green with lower IDE0051/52 counts (target 0 in production code).
- All tests green.
- Playwright smoke green: `/`, `/kompas` (KompasTabBar area), match page, and WhoAmI/Mijn DNA page render.
- `git grep` for each deleted symbol returns nothing.

**Dependency:** after 01 (baseline). The JobsyApiClient part waits for the SalesWalletChip PR; the VacancyDiscovery part waits for the VacancyCard PR.
