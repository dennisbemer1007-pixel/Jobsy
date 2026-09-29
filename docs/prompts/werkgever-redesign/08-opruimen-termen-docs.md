# 08 · Clean-up: plain Dutch, one set of terms, dead code out, docs and full role × page coverage

> Read `00-README.md` first. §0 (terminology, strings, docs/guards), §IA, §R and D11 apply.

| | |
|---|---|
| Branch | `cursor/werkgever-redesign-8` from `cursor/werkgever-redesign-7` (stacked) |
| PR | ONE PR into `acceptatie`: `chore(werkgever): terminology guard, localization, dead code removal and docs`. Stacked on #<PR of 07> (`cursor/werkgever-redesign-7`) |
| Mockups | all (final visual check per role) |
| Split seam (if too big) | 8a = guards + localization; 8b = dead code + docs + Playwright smoke |

**Goal.** The employer side speaks one language and has no dead ends. The rights matrix is complete, and the docs describe what's live.

## 08.1 Terminology guard (D11)
- `WerkgeverTerminologyTests`, a data test over `UiStringsWerkgever` + `UiStringsCandidateInsights` + every string used by `Pages/Werkgever/**`, `Components/Werkgever/**`, `PublishOptionsDialog`, `PushBomConfirmDialog`, `TokenTopUpDialog`, employer e-mails and notifications:
  - the **nl** values must not contain (case-insensitive, word-bounded): `PushBom`, `Push Bom`, `Highlight`, `Gematcht`, `Contact opgenomen`, `filiaalmanager`, `branchmanager`, `branch manager`, `Enterprisemanager`, `Enterprise manager`, `Regional manager`, `Extend`, `tokens uitgeven`
  - the **en** values use the English equivalents consistently (Company manager / Regional manager / Branch manager, Hired, Invited, Feature, Push message). Record the chosen en terms in the test so they stay stable. pl/ro/ar only need to be non-empty and different from the key (existing parity tests).
- `WerkgeverHardcodedDutchTests`: `.razor` files under `Pages/Werkgever` and `Components/Werkgever` contain no Dutch text literals outside `@Culture[...]` (heuristic: text nodes with ≥ 2 Dutch stop words; allowlist for brand names). Fix what it finds.
- Status labels app-wide for employers come from one map, `WerkgeverStatusLabels` (vacancy + application statuses → nl keys). Replace ad-hoc switch statements in the moved pages.

## 08.2 Remove dead code
Delete, and prove each file unreferenced with `rg`:
- `Components/Employer/EnterpriseNavItems.cs`, `EnterpriseOrgSubnav.razor`
- `Pages/Employer/Organization.razor` (if not done in 01)
- `Pages/Regional/TokenControl.razor` (if not done in 06)
- `Pages/Branch/BranchDashboard.razor`, `Pages/Regional/RegionalDashboard.razor` (their routes live in the redirect component)
- `Components/Home/EmployerHomePanel.razor` (if no longer used by admins or intermediaries; otherwise keep it for them and say so)
- the old `RoleNavCatalog` employer helpers, empty folders (`Pages/Employer`, `Pages/Branch`, `Pages/Regional`) once all pages moved
- CSS rules only used by the deleted pages, with the asset versions bumped

Keep the **redirect table** and the in-circuit redirect component: they stay for at least one release (D2). Add `// Remove after {release}` and a CHANGELOG line.

## 08.3 Rights matrix complete
- `WerkgeverRightsMatrix` covers **every** `/werkgever` page and **every** mutating employer endpoint touched by 01–07.
- A reflection test enumerates:
  - all `@page` routes under `Pages/Werkgever`
  - all controller actions with `[HttpPost|Put|Patch|Delete]` in the employer controllers: `Vacancies`, `Applications`, `CompanyUsers`, `Regions`, `Companies`, `Tokens`, `TokenLogs`, `SalaryTables`, `CandidateInsights`, `Werkgever*`, `Registration` takeovers, `EmployerFlyers`, talent contacts
  - Each one must appear in the matrix (**no silent gaps**).
- Update `docs/security/roles-matrix.md` from the matrix. The simplest route is a small test-generated markdown that the test compares, like `RoutesDocFreshnessTests`.

## 08.4 Docs
- `docs/ROUTES.md`: the new `/werkgever` routes plus the old → new redirect table.
- `docs/security/roles-matrix.md` (08.3); `docs/adr/0004-roles-and-scope.md`: add a short "Werkgever redesign" section (RM read-only, VM no purchase, only the BM invites, scope chip only narrows).
- `docs/TESTSCENARIOS_PER_ROL.md`: BM/RM/VM scenarios for dashboard, vacatures, sollicitaties, organisatie, tokens, kandidaatinzichten (free, unlock, renew, request).
- `CHANGELOG.md`: one user-facing entry in Dutch.
- `docs/feature-flags.md` if it exists by now (Dependencies A): the `/werkgever` routes under Werkgevers actief + Kandidaatinzichten on/off.
- `Help/PageHelpDocs.cs`: help text per new page (short, Dutch, "je").

## 08.5 Final UI pass
- Per role × page, check against the mockups at 1440 and 390: label = h1 = crumb, one primary action per card, RM hint present, no English, no emoji, RTL (`ar`) layout of the sidebar, the drawers and the bottom nav.
- Accessibility: axe (or the existing a11y helper) on Dashboard, Vacatures, Sollicitaties, Team & rechten, Tokens and Kandidaatinzichten for BM. No serious or critical issues.
- Performance: no page makes > 4 API calls on load (log it in a Playwright trace and report it in the PR).

## Tests
- `WerkgeverTerminologyTests`, `WerkgeverHardcodedDutchTests`, the matrix completeness test.
- Playwright `WerkgeverSmokePlaywrightTests`, per role (BM, RM, VM) × key pages (Dashboard, Vacatures, Sollicitaties, Tokens, Kandidaatinzichten; plus Organisatie/Team for BM) at 1440 and 390: loads, h1 correct, no console errors, no old-URL navigation.
- All earlier suites green: `LocalizationParityReportTests`, `RoutesDocFreshnessTests`, `PageSeoTests`, `PageHelpDocsTests`, `BlazorPageRoleAttributesTests`, `AssetVersionGuardTests`, `RoleFunctionalRegressionTests`, the Candidate insights suites.

## Success criteria
- No jargon or English in nl on employer pages (guarded).
- No dead employer pages or components.
- The rights matrix covers every page and mutating endpoint, and the docs match it.
- The smoke suite is green for all three roles on desktop and mobile.

## Done → stack complete
Push, open the PR, note its number. Report to Dennis:
- all 8 PR numbers with their branches
- the Dependencies A/B/C cases applied
- the deferred items: vacancy end date, pdf export, voorbeeldrapport, automatisch aanvullen, reminder e-mail, interne notitie, global search
- the list of server-side tightenings

Never merge, never deploy.
