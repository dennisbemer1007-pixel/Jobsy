# Intermediair (uitzendbureau): Cursor run book

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

**What this stack builds:** the Intermediair role (`UserRole.Intermediary`, an uitzendbureau) done right. It gets the same werkgever shell and page set as the bedrijfsmanager, with a role chip "Intermediair", and it posts vacancies **for opdrachtgevers** (clients).
- **Opdrachtgevers come only from KvK.** The bureau types a KvK number, picks the KvK vestiging that is the werklocatie and confirms a declaration. Name, address and coordinates come from KvK plus a server-side PDOK geocode. **There is no free location field anywhere** (UI, API, CSV, external API).
- **One per-vacancy choice:** the pin stands at **onze vestiging** (default, the opdrachtgever stays hidden) or at the **echte werklocatie** (the opdrachtgever's KvK address, name shown "via {bureau}").
- **Hidden really means hidden:**
  - pin, travel ring, travel-time filter, travel time and matching all use the bureau's vestiging
  - the opdrachtgever's name, KvK number, address and company page never reach a candidate until the bureau invites them ("Uitgenodigd")
- **Full data separation:** the opdrachtgever link is a data record, **not** a company membership.
  - Opdrachtgevers have no login.
  - An opdrachtgever that is also a Lobsy werkgever never sees the bureau's vacancies or applicants.
  - Two bureaus never see each other's opdrachtgevers, vacancies or applicants.
- **The bureau pays everything** from its own wallet; invoices and the agency subscription are on the bureau.
- **Uitleenregistratie check** (Waadi today, Wtta/NAU later) at registration, behind one isolated service. Plus a declaration per opdrachtgever; more than 20 new opdrachtgevers per month goes to admin review.
- **KvK stays the source of truth:**
  - weekly refresh
  - address changes flow through automatically
  - a deregistered vestiging puts its vacancies on hold
- **Kandidaatinzichten** for the bureau with the bedrijfsmanager rules, paid from bureau tokens, for the area around the bureau's vestiging(en).

It also fixes every bug from the 29-09 review. Each bug is listed below with the file that fixes it.

| # | Bug at `a611db40` | Fixed in |
|---|---|---|
| B1 | Hidden mode only swaps name/address: pin, travel time, circles and filter use the client's KvK coordinates; `IntermediaryVacancyRulesTests.ResolvePublicDisplay_masked_uses_vacancy_coords_over_intermediary_hq` locks the leak in | 02 (location model), 03 (resolver + test rewrite) |
| B2 | Public DTOs carry the client's `CompanyId`, `KvkNumber`, `Vestigingsnummer`; `VacancyDetail.razor` links the bureau name to the client's company page; the client's company page lists hidden vacancies; the discovery record carries the client's KvK | 02 (ownership), 03 (DTO, links, canary test) |
| B3 | Adding a client adds a `UserCompany` membership, also on an **existing** (real) employer, without `IsInUse` or consent. The bureau can then manage that employer's vacancies, applications and tokens; two bureaus share a client row; the client sees the bureau's applicants; shell companies push the real employer's later registration into a takeover request to the bureau | 01 (link entity), 02 (migration, memberships removed, shells) |
| B4 | Publish/uitlichten/pushbericht/verlengen spend from the **client's** wallet; checkout can target a client; the invoice is issued in the client's name; the agency subscription is checked on the client | 02 |
| B5 | Client performance counts all vacancies of the client company (own + other bureaus) | 02 |
| B6 | KvK non-hoofdvestigingen get coordinates (0,0); nothing rejects 0,0; registration during a KvK outage defaults to the NL centroid (52.1326, 5.2913) | 01 |
| B7 | Masking uses the bureau organisation, not a chosen bureau vestiging; no multi-vestiging support | 02 (owner vestiging), 06 (UI) |
| B8 | Misleading checkbox copy, no KvK re-check, no declaration, no uitleenregistratie check | 02, 04 |

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-kvk-geo-opdrachtgever-koppeling.md`: server-side PDOK geocoder, no more (0,0) anywhere (KvK mapping, registration fallback, repair of existing rows), `IntermediaryClient` link entity + `api/intermediary/clients` (KvK-only add, declaration, monthly review threshold), `IIntermediaryContext`, admin review queue, "Opdrachtgever toevoegen" drawer component, rights-matrix foundation | `cursor/intermediair-1` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-vacature-eigendom-scheiding.md`: vacancy ownership moves to the bureau vestiging (`Vacancy.CompanyId`) with `IntermediaryClientId`; `Vacancy.Location` = the public pin; 25 km rule; vacancy form step "Opdrachtgever & locatie"; CSV + external API via links only; one-shot data migration (memberships removed, shells, report); wallet/checkout/subscription/invoice; client performance; data separation | `cursor/intermediair-2` | `cursor/intermediair-1` | `acceptatie` |
| 03 | `03-publieke-weergave-privacy.md`: one public-identity resolver, hidden/real display everywhere a candidate looks (map, list, detail, search, JSON-LD, company page, share, assistant, Lobsy-cv, e-mail/push), public DTO stripping, reveal at "Uitgenodigd", the leak-locking test rewritten, canary leak tests | `cursor/intermediair-3` | `cursor/intermediair-2` | `acceptatie` |
| 04 | `04-kvk-verversing-uitleenregistratie.md`: weekly KvK refresh job, address change / deregistration / beyond-25-km holds, signals for Te doen, `ILenderRegistrationCheck` (Waadi / Wtta, isolated), publish gate, admin tab "Uitleenregistratie" | `cursor/intermediair-4` | `cursor/intermediair-3` | `acceptatie` |
| 05 | `05-kandidaatinzichten-intermediair.md`: Kandidaatinzichten for the Intermediary with the BM rules, bureau wallet, area = bureau vestiging(en) | `cursor/intermediair-5` | `cursor/intermediair-4` | `acceptatie` |
| 06 | `06-intermediair-in-werkgever-shell.md`: Intermediary items in the werkgever nav, role chip + "Alle opdrachtgevers ▾" scope chip, dashboard, Opdrachtgevers list + detail, Te doen items, bureau vestigingen, Team, Tokens & facturen, 301s from `/intermediary*`, mobile | `cursor/intermediair-6` | `cursor/intermediair-5` | `acceptatie` |
| 07 | `07-opruimen-docs-guards.md`: terminology, leftover strings, dead code (old endpoint, old checkbox), docs (ROUTES, roles matrix, SECURITY, functional spec, test scenarios, CHANGELOG), final guards, Playwright smoke | `cursor/intermediair-7` | `cursor/intermediair-6` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/intermediair-2b`).

## Pointer prompt (the only prompt needed; it runs 01 … 07)
```
Run the Intermediair stack. First: git fetch origin && git show origin/docs/intermediair:docs/prompts/intermediair/00-README.md — read it completely.
Then read and execute each file in docs/prompts/intermediair/ on that branch strictly in the order the README's table lists (01 … 07; a/b splits where a file allows it), one file = one PR.
File 01 branches from origin/acceptatie; every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 01, run the dependency checks in the README's "Dependencies" section and follow the fallback it prescribes for each one; say in PR 01 which case applied (re-check A and B before 05 and 06).
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes.
At the end report: file → branch → PR number → status, plus anything deferred.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/security/roles-matrix.md`, `docs/adr/0004-roles-and-scope.md`, `docs/release-flow.md`, `SECURITY.md` (intermediary section) and `docs/FUNCTIONELE_SPECIFICATIES_INTERMEDIAIR_SALES_KPI.md`.
2. Run the **Dependencies** checks below and note the outcome (it goes into PR 01). Re-run checks A and B before starting 05 and 06; the werkgever stack may have landed in between.
3. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column. File 01: `git checkout -b cursor/intermediair-1 origin/acceptatie`. Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, and the Playwright suites the file names if you can. Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. Body starts with `Stacked on #<prev PR> (<prev branch>)`, then the PR body items from §0.
   6. Note the PR number, go on to the next file.
4. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely. Push what you have, open that PR as **draft** with the failure described, don't continue.
5. At the end, report the table file → branch → PR number → status (green or draft/red), plus anything deferred.
6. **Never** merge, deploy, or use rule `123` (`.cursor/rules/shortcut-123.mdc`). **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: each file adds its own migration on top of the previous one. Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you, `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches stack** (see above). **ONE PR per file, always into `acceptatie`.** Body starts with `Stacked on #<prev PR> (<prev branch>)`; file 01 says `Stacked on: none (first in the stack)`. The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
- **Stop on red.** `dotnet build` + `dotnet test` after each file. If red and not fixable in scope: stop, push, draft PR with the failure, report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST). Re-check line numbers before editing. Key places:
  - `Jobsy.Core/Rules/IntermediaryVacancyRules.cs` (`ValidateEndClientKvk`, `ResolvePublicDisplay`, `ResolveCategoryId`)
  - `Jobsy.Core/Entities/Vacancy.cs` (`IntermediaryCompanyId`, `ShowClientAddressOnMap`)
  - `Jobsy.Api/Controllers/CompaniesController.cs` (`RegisterIntermediaryClientFromKvk` ~L171, `EnsureActorMembershipAsync` ~L627)
  - `Jobsy.Api/Controllers/VacanciesController.cs` (`SaveDraftAsync` ~L1047, intermediary block ~L886, list DTO ~L2069–2172, `ResolveIntermediaryOrganizationIdAsync` ~L2359; the same helper is duplicated in `VacancyCsvImportController` ~L447)
  - `Jobsy.Infrastructure/Services/VacancyProductService.cs` (spends ~L248/433/506/607/662, `HasActiveAgencySubscriptionAsync` ~L115/385/1310)
  - `Jobsy.Api/Controllers/TokensController.cs` (checkout ~L161–200), `Jobsy.Infrastructure/Services/TokenPurchaseInvoiceService.cs` (~L42)
  - `Jobsy.Infrastructure/Services/MetricsQueryService.cs` (`GetClientPerformanceAsync` ~L320)
  - `Jobsy.Infrastructure/Services/KvkHandelsregisterService.cs` (`MapVestigingen` ~L319)
  - `Jobsy.Infrastructure/Services/CompanyRegistrationService.cs` (intermediary branches ~L686/748/910/1013, manual-address centroid ~L1770)
  - `Jobsy.Api/Controllers/CandidateInsightsController.cs` (`IsInsightsRole` ~L88)
  - `Jobsy.Web/Components/Pages/Branch/CreateVacancy.razor` (intermediary fieldset ~L39–120, `RegisterIntermediaryClientFromKvkAsync` ~L1000)
  - `Jobsy.Web/Components/Pages/VacancyDetail.razor` (company link ~L144, JSON-LD ~L759)
  - `Jobsy.Web/Components/Pages/CompanyPublicPage.razor`, `Jobsy.Web/Components/Pages/Intermediary/{IntermediaryDashboard,Team}.razor`, `Jobsy.Web/Navigation/RoleNavCatalog.cs` (`Intermediary` ~L97)
- **Mockups:** branch `docs/intermediair`, folder `docs/mockups/intermediair/`. Read with `git fetch origin docs/intermediair && git show origin/docs/intermediair:docs/mockups/intermediair/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - Desktop 1440×900: `im-d1-dashboard.png`, `im-d2-opdrachtgevers.png`, `im-d3-vacature-opdrachtgever-locatie.png`, `im-d5-opdrachtgever-toevoegen-kvk.png`; `im-d4-kandidaatweergave.png` (candidate view, both modes, long).
  - Mobile 390 wide: `im-m1-dashboard.png`, `im-m2-vacature-locatie.png`.
  - The mockups are a **layout and copy reference**. Bureaus, opdrachtgevers, KvK numbers, addresses and numbers are **Voorbeelddata**. The "Voorbeelddata" pill is mockup-only.
  - **Where the mockup and this spec differ, this spec wins.** Known differences:
    - **Search** in the top bar is built only under the werkgever rule (werkgever README 01.5): not rendered unless a scoped global search exists.
    - **d4, real mode, "Bedrijfspagina: Pagina van {opdrachtgever}":** not built. Real mode shows the opdrachtgever's name as **plain text** plus a "via {bureau}" link to the **bureau's** public page (D22). The opdrachtgever never gets a Lobsy company page from this stack.
    - **d4, hidden mode, "Werklocatie: Regio Westland":** the region is the opdrachtgever's **gemeente** from PDOK (`IntermediaryClient.Municipality`), rendered "Regio {gemeente}" (D21).
    - **d1 Te doen "1 vacature staat op 24 km van je vestiging":** the real signal fires only when the distance is **> 25 km** (then the vacancy is on hold, 04). Show a softer info line between 20 and 25 km ("Bijna 25 km: verborgen blijft mogelijk"), no Te doen item.
    - **d1 "Gemengd"** pill = the opdrachtgever has live vacancies in both modes.
    - **d1 sidebar:** 06 adds **Vestigingen** under Organisatie (bureau vestigingen, D17), missing in the mockup.
    - **d2 "Tokenverbruik" tab:** a report of the bureau's spend on this opdrachtgever's vacancies, read-only. Tokens always come from the bureau wallet (D7); there is no per-opdrachtgever wallet.
    - **d5 declaration text** is the versioned text of D20; the mockup text is a shortened version.
- **Design system.** Follow `.cursor/rules/design-system.mdc` and the werkgever README §0 design rules (same shell, density, tokens-only colours, weights 400/600, breakpoints 640/900/1024, logical properties, calm UI, icons only where allowed). Extra rules for this stack:
  - **"Uit KvK" fields** are rendered with the shared read-only field pattern: value + small lock icon + "Uit KvK" label, never an `<input>` (not even disabled). A guard test asserts that no intermediary page/component renders an `<input>`, `<textarea>` or `contenteditable` bound to an address, street, postcode, city, latitude or longitude.
  - **Map previews** use the existing map component/JS (`jobMap`) or a static preview from tokens. Don't add a second map library. The grey "werklocatie" dot on bureau-only previews (d2, d3) is shown to the bureau only.
  - **The "Uitzendbureau" pill** (hidden mode) and the **"via {bureau}" pill** (real mode) share one component `IntermediaryBadge` (03).
- **Strings:** all new UI text through `@Culture["…"]` in **nl/en/pl/ro/ar**, in a new `Localization/UiStringsIntermediair.cs` (prefixes `Im.`, `ImNav.`, `ImClient.`, `ImVac.`, `ImPublic.`, `AdminIm.`), registered in `UiStrings.cs` like the other modules. Dutch copy in this stack is final (zakelijk, kort, "je"). `LocalizationParityReportTests` and `LocalizationTests` stay green. Hardcoded Dutch on the intermediary parts you touch gets localized on the way (the `CreateVacancy.razor` intermediary fieldset, `IntermediaryDashboard.razor`, `IntermediaryVacancyRules` messages → message keys).
- **Terminology (nl final):**

  | Use | Instead of |
  |---|---|
  | Intermediair (role chip, roles matrix) | Intermediary, bemiddelaar |
  | Bureau / uitzendbureau (in candidate-facing copy) | intermediair |
  | Opdrachtgever | Klant, inhuurend bedrijf, eindklant, end-client |
  | Werklocatie | Adres opdrachtgever, locatie |
  | Onze vestiging / Via onze vestiging | Verborgen, gemaskeerd, masked |
  | Echte werklocatie | Open, klantadres |
  | Uit KvK | Geverifieerd adres |
  | Uitleenregistratie | Waadi-check, Wtta-toelating (except in admin detail text) |

  Code identifiers (`Intermediary`, `ShowClientAddressOnMap`, API routes) **stay**. Only UI text changes. 07 adds the guard test.
- **Location only from KvK (D2), enforced server-side:**
  - no request DTO of any intermediary, vacancy, CSV or external-API write path carries an address, street, postcode, city, latitude, longitude or location field for the werklocatie
  - `Vacancy.Location` is **always computed** by `IntermediaryVacancyLocation.Resolve` (02) for intermediary vacancies, never taken from a request
  - the reflection guard (01, extended in 02) fails the build when such a property appears
- **Identity firewall (D4, §P):** the opdrachtgever's identity (`IntermediaryClient` name, KvK number, vestigingsnummer, address, municipality, coordinates, SBI) leaves the server **only** through `IntermediaryPublicIdentity` (03) and bureau-only endpoints (`api/intermediary/*`, employer vacancy/applicant endpoints under bureau authorization). The canary test (03) seeds a hidden vacancy with unique marker strings and asserts they appear in **no** candidate-facing or anonymous response.
- **Authorization: server first (§R).**
  - Every intermediary API resolves the bureau from the **signed-in user** via `IIntermediaryContext` (01), never from a route or body value alone.
  - Every link id is checked against the bureau (`404` for a foreign link, not `403`, so ids don't leak).
  - The UI hiding a button is **never** the only guard.
  - The matrix test (01) enforces it. Never loosen an existing attribute; list any tightening in the PR.
- **Werkgevers actief (Dependencies, check E):** when `RequiresFeatureAttribute` + `PlatformFeature.Employers` exist, every intermediary page and new endpoint carries `[RequiresFeature(PlatformFeature.Employers)]` like the employer pages.
- **Privacy (AVG):** candidate data follows the **existing** `LobsyCvAccessRules` stages for the bureau exactly as for the bedrijfsmanager (werkgever README §0 Privacy). Kandidaatinzichten stay aggregated with `KAnonymityThreshold = 10`. `PersonalDataAccessLog` as today. The opdrachtgever's KvK data is company data (not personal data), but sole-trader KvK records can contain a person's name: the link stores only what the KvK establishment returns (handelsnaam, address) and shows it only to the bureau and admin.
- **Audit:** admin review actions (approve/reject link, verify/reject uitleenregistratie, threshold setting) go through `IAdminAuditLog` (admin redesign 07) when it exists, else an interim structured `PlatformLog` row (Dependencies, check D). Link creation, archive and KvK refresh changes write a structured `PlatformLog` row (`intermediary.client.*`).
- **Docs and guards to update when routes change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (all new pages private, non-indexable; `PageSeoTests`), `Help/PageHelpDocs.cs` (`PageHelpDocsTests`), `BlazorPageRoleAttributesTests`, `docs/security/roles-matrix.md`, `CHANGELOG.md`. Transactional e-mails that mention a vacancy's company go through the resolver (03).
- **CSS:** new `wwwroot/css/features/intermediair.css` (BEM block `im-…`) only for intermediary-specific pieces (read-only KvK fields, map-mode radio cards, candidate preview). Everything else reuses the werkgever/enterprise styles. Link it in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-intermediair`, add it to `Jobsy.Tests/asset-versions.json` (`AssetVersionGuardTests`). Don't append to `app.css`.
- **Migrations:** `dotnet ef migrations add …` in `Jobsy.Infrastructure`, snapshot updated. `EfModelSnapshotTests`, `PendingModelChangesTests`, `EfMigrationDiscoveryTests` stay green. Data moves happen in the one-shot migration service (02), **not** in EF migration SQL.
- **Discovery index:** every change that alters a vacancy's public fields (owner, location, mode, hold) triggers the existing discovery-index refresh for that vacancy (`VacancyDiscoveryIndex`); the 02 migration triggers a full rebuild at the end.
- **Must NOT touch:**
  - candidate navigation and pages, beyond the candidate-facing display changes in 03
  - token **price values** and **pricing logic**; Mollie/checkout flows and return URLs (only the checkout **target** check in 02 changes)
  - the MFA pages and `MfaEnforcementMiddleware`
  - `features/questionnaire.css`, banenkaart CSS/JS beyond the pin/label data it already reads, `app-core.js`, the cookie banner
  - the employer (BM/RM/VM) rights: the werkgever matrix stays byte-identical, except where the shared KvK geo fix (01) changes how coordinates are filled
  - Ambassadeur (parked)
- **PR description:**
  - what changed and why
  - screenshots desktop 1440 (bureau) and mobile 390 of each new or changed screen; for 03 also the candidate views (hidden and real)
  - the new/old URL list
  - the §R rows this PR added or changed
  - test list
  - "Out of scope / deferred"
- **Run** `dotnet build` and `dotnet test` (unit + bUnit). Run the Playwright suites you touched if you can; say so if you couldn't.

---

## §IA. Information architecture (the contract for all files)

The Intermediary uses the werkgever shell (`WerkgeverLayout`, `WerkgeverNav.cs`; Dependencies, check A). **Label = page `h1` = breadcrumb.** Role chip **"Intermediair"** left of the scope chip. Scope chip **"Alle opdrachtgevers ▾"** (switcher: all / one opdrachtgever; a second section "Vestiging" only when the bureau has > 1 vestiging). Items whose page arrives in a later file are present with `IsAvailable = false` and **not rendered** until that file flips them.

| Group | Item (nl) | URL | Built in | Replaces (old → 301) |
|---|---|---|---|---|
| Overzicht | Dashboard | `/werkgever` | 06 | `/intermediary` (only the dashboard part; see Opdrachtgevers) |
| | Te doen | `/werkgever/te-doen` | 04 data, 06 UI | — |
| Werving | Vacatures | `/werkgever/vacatures` | werkgever 03 (list), 02 (form step) | as werkgever |
| | (no nav item) Nieuwe vacature | `/werkgever/vacatures/nieuw` | 02 (step "Opdrachtgever & locatie") | `/branch/vacancies/new` |
| | Sollicitaties | `/werkgever/sollicitaties` | werkgever 04 | as werkgever |
| | Talentpool | `/werkgever/talentpool` | werkgever | as werkgever |
| | Kandidaatinzichten | `/werkgever/kandidaatinzichten` | 05 | — |
| Opdrachtgevers | Opdrachtgevers | `/werkgever/opdrachtgevers` (+ `/{clientId}` with tabs Overzicht · Vacatures · Sollicitaties · Tokenverbruik) | 06 (drawer from 01) | `/intermediary` → `/werkgever/opdrachtgevers` |
| Organisatie | Vestigingen (bureau vestigingen) | `/werkgever/organisatie/vestigingen` | 06 | — |
| | Team & rechten | `/werkgever/organisatie/team` | 06 (moves) | `/intermediary/team` |
| | Bureauprofiel | `/werkgever/organisatie/profiel` (label override for the Intermediary) | werkgever 05 | as werkgever |
| | Salaristabellen | `/werkgever/organisatie/salaristabellen` | werkgever | as werkgever |
| Tokens & facturen | Saldo & kopen | `/werkgever/tokens` (+ agency-subscription status line) | werkgever 06, 02 (target check) | as werkgever |
| | Mutaties · Facturen | `/werkgever/tokens/mutaties`, `/werkgever/tokens/facturen` | werkgever 06 | as werkgever |
| Meer | Koppelingen · Wervingsmateriaal | as werkgever | werkgever 05 | as werkgever |

**Mobile (< 1024) bottom nav, 5 items:** Overzicht · Vacatures · Sollicitaties · Opdrachtgevers · Meer (`im-m1`).

**Lobsy admin:** `/admin/intermediairs` with tabs **Opdrachtgevers ter controle** (01), **Migratie** (02), **Uitleenregistratie** (04). If admin redesign 01 landed, "Intermediairs" goes into its sidebar group **Organisaties** (after Bedrijven & vestigingen); otherwise add one item to today's admin nav.

**API prefixes:** `api/intermediary/*` (Intermediary, plus Admin where §R says) · `api/admin/intermediary/*` (`RequireAdmin`). The old `POST api/companies/intermediary-clients/from-kvk` is removed in 02 (answers `410 Gone` with `{ code: "use_api_intermediary_clients" }` for one release).

---

## §R. Rights matrix (server-side contract; 01 encodes it, every file keeps it true)

● = allowed · ◯ = own bureau only · — = 403/404. "Own bureau" = the organisation `IIntermediaryContext` resolves for the signed-in Intermediary user (its Type-Intermediary companies). "Foreign link" answers **404**.

| Page / action | Intermediair (own bureau) | Other bureau | Werkgever users of a company with the same KvK (BM/RM/VM) | Candidate / anonymous | Lobsy admin |
|---|---|---|---|---|---|
| KvK lookup for a new opdrachtgever | ◯ (rate-limited) | — | — | — | ● |
| Add opdrachtgever (KvK + vestiging + declaration) | ◯ | — | — | — | ● (support, on behalf of a bureau) |
| View opdrachtgever list/detail incl. KvK snapshot | ◯ | — | — | — | ● |
| "Opnieuw ophalen" (KvK refresh of one link) | ◯ (1× per 10 min per link) | — | — | — | ● |
| Default map mode per opdrachtgever; archive opdrachtgever (no live vacancies) | ◯ | — | — | — | ● |
| Approve / reject a link in review; review threshold setting | — | — | — | — | ● |
| Create/edit/publish/verlengen/uitlichten/pushbericht a vacancy for a link | ◯ (link Active, uitleenregistratie Verified, owner vestiging location from KvK/PDOK) | — | — | — | ● (support) |
| Switch map mode of a vacancy (hidden ↔ real) | ◯ (hidden only ≤ 25 km) | — | — | — | ● |
| See applications of bureau vacancies (privacy stages as BM) | ◯ | — | — | — | ● (as today) |
| Candidate sees opdrachtgever name + werklocatie | — | — | — | real mode: always; hidden mode: **only the candidate's own application from "Uitgenodigd"** (and while Aangenomen) | ● |
| Tokens: saldo, kopen, spend, facturen, agency subscription | ◯ (bureau wallet; checkout target ∈ own bureau companies) | — | — | — | ● (as today) |
| Kandidaatinzichten (free / unlock) | ◯ as BM, bureau vestiging(en) area | — | — | — | as today |
| Client performance per opdrachtgever | ◯ own links, own vacancies only | — | — | — | ● (platform view) |
| Team & rechten (invite Intermediair colleagues) | ◯ (as today, `EmployerInviteRules`) | — | — | — | ● |
| Uitleenregistratie: view own status | ◯ | — | — | — | ● verify / reject |

All Intermediary users of one bureau have the same rights (no BM/VM split inside a bureau, as today).

The matrix test is data-driven from **one** table in `Jobsy.Tests/Intermediair/IntermediairRightsMatrix.cs` (endpoint × actor → expected status; page route × role → allow/deny), including the **other bureau**, **same-KvK werkgever**, **candidate** and **anonymous** cases. Each later file adds its rows.

---

## §D. Data model (01 and 02 build it; later files only add what they name)

All ids `Guid`, times UTC.

| Entity | Fields | Notes |
|---|---|---|
| `IntermediaryClient` (01) | `Id`, `IntermediaryOrganizationId` (bureau org company id), `KvkNumber`, `Vestigingsnummer`, `KvkEstablishmentId`, `Name` (KvK handelsnaam), `Address` (KvK bezoekadres, formatted), `Postcode`, `HouseNumber`, `Municipality` (PDOK gemeentenaam), `Location` (GeoPoint, **never 0,0**), `GeoSource` (`Kvk`/`Pdok`), `SbiCode?`, `SbiDescription?`, `IsHoofdvestiging`, `Status` (`Active`, `PendingReview`, `Rejected`, `Archived`), `KvkStatus` (`Active`, `Deregistered`, `NotFound`), `DefaultShowClientLocation` (bool, default false), `DeclarationAcceptedAtUtc?`, `DeclarationAcceptedByUserId?`, `DeclarationTextVersion?`, `LastKvkCheckAtUtc`, `LastKvkChangeAtUtc?`, `PreviousAddress?`, `ReviewedAtUtc?`, `ReviewedByUserId?`, `ReviewNote?`, `LegacyCompanyId?` (no FK; 02 migration), `CreatedAtUtc`, `CreatedByUserId` | Unique `(IntermediaryOrganizationId, KvkEstablishmentId)`; re-adding an archived one reactivates it with a new declaration. **No FK to any `Company` of the opdrachtgever and no membership.** |
| `Vacancy` (existing, 02) | `CompanyId` = **owning bureau vestiging** (was: the client); + `IntermediaryClientId?` (FK `Restrict`); `IntermediaryCompanyId` = bureau org (unchanged meaning); `ShowClientAddressOnMap` (unchanged; false = onze vestiging); `Location` = **the public pin** (bureau vestiging when hidden, link location when real); + `HoldReason?` (`VacancyHoldReason`: `KvkDeregistered = 1`, `OutsideMaskRadius = 2`, `WerklocatieUnknown = 3`), + `HoldSinceUtc?` | Invariant: `IntermediaryClientId != null` ⇔ `IntermediaryCompanyId != null`. A vacancy with a hold is excluded from discovery/search/matching and shows "Gepauzeerd" to the bureau; `Status` stays as it is (no new status value) |
| `Company` (existing) | + `LocationSource` (`Unknown = 0`, `Kvk = 1`, `Pdok = 2`) (01); + `IsLegacyIntermediaryShell` (bool, 02) | `LocationSource = Unknown` for rows that still have (0,0) or the NL centroid after the 01 repair |
| `IntermediaryMigrationRun` (02) | `Id`, `RanAtUtc`, `Outcome`, `SummaryJson` (counts + lists, see 02.8) | One row per run; the service is idempotent |
| `LenderRegistration` (04) | `Id`, `CompanyId` (bureau org), `Status` (`NotChecked`, `Pending`, `Verified`, `Rejected`), `Source` (`WaadiKvk`, `WttaNau`, `AdminManual`), `Reference?`, `CheckedAtUtc?`, `DecidedByUserId?`, `Note?`, `ValidUntil?`, `CreatedAtUtc` | History rows; the latest decides |
| Settings (01) | `IntermediaryMonthlyClientReviewThreshold` (int, default **20**, 1–500) | Admin, audit-logged |
| Constants (02) | `IntermediaryMaskRules.MaxMaskDistanceKm = 25` | Not a setting (D5) |

---

## §P. Privacy and leak rules (every file)
- **The opdrachtgever is invisible in hidden mode** to everyone except the bureau and Lobsy admin. That covers the map pin and label, travel ring, travel-time filter and value, list card, vacancy page, JSON-LD `hiringOrganization` / `jobLocation`, OG/share data, sitemap, text search (a search for the opdrachtgever's name or KvK finds **nothing**), company pages, assistant chat, Lobsy-cv, matching explanations, the candidate's applications list, and e-mail and push texts.
- **Reveal (D4):** in hidden mode the candidate sees the opdrachtgever's name and werklocatie **only** in their **own** application detail, from status `EmployerContacting` ("Uitgenodigd") and while `Hired`. Not while `Pending`/`Accepted`, and not after `Rejected`/`Withdrawn`. Always with "Solliciteren en contact lopen via {bureau}".
- **Real mode:** name + KvK address + pin at the werklocatie, labelled "via {bureau}". No link to an opdrachtgever company page (D22).
- **No triangulation:** in hidden mode no value derived from the werklocatie reaches the candidate. That includes distance, travel minutes, the order of results by distance, and the radius filter.
- **The link is not a membership (D8):** creating, refreshing or archiving a link never reads or writes `UserCompany`, `Company` rows of the opdrachtgever, their wallet or their users.

---

## Decisions (defaults applied; Dennis can override any of them)
- **D1. Role and shell.** `UserRole.Intermediary` stays (nl "Intermediair"). Same werkgever shell and page set as the bedrijfsmanager, with a role chip "Intermediair" and the opdrachtgevers scope chip. Same token, privacy and Kandidaatinzichten rules as employers. *(Dennis, 29-09)*
- **D2. Location only from KvK.** The werklocatie comes only from the opdrachtgever's KvK vestiging; the bureau can't enter or edit a location anywhere. *(Dennis, 29-09; verified at `a611db40`: no free field today, keep it that way and guard it)*
- **D3. One per-vacancy choice:** "Toon op locatie van onze vestiging (standaard)" or "Toon op echte werklocatie". Default per opdrachtgever via `DefaultShowClientLocation` (false). *(Dennis, 29-09)*
- **D4. Name reveal.** Hidden mode: opdrachtgever name + werklocatie only from "Uitgenodigd", in the candidate's own application. Real mode: name always, "via {bureau}". Solliciteren and contact always via the bureau. *(Dennis, 30-09)*
- **D5. Hidden-mode geo.** Pin, travel ring, filter and travel time all use the bureau vestiging, with the honest label "Reistijd tot de vestiging van het bureau · werklocatie in de regio {gemeente}". Hidden mode is allowed only when the werklocatie is **≤ 25 km** (straight line) from the owning bureau vestiging. *(Dennis, 30-09)*
- **D6. KvK geo.** Server-side **PDOK Locatieserver** geocoding of the KvK address (postcode + huisnummer exact), never typed. A vestiging whose address can't be geocoded **can't be added**. Weekly KvK refresh; a deregistered vestiging puts its vacancies on hold. *(Dennis, 30-09)*
- **D7. Money.** The bureau's wallet pays everything; invoices and the agency subscription are on the bureau. *(Dennis, 30-09)*
- **D8. No opdrachtgever login;** the separate `IntermediaryClient` link replaces membership, with full data separation. *(Dennis, 30-09)*
- **D9. Checks.** Uitleenregistratie check at registration + declaration per opdrachtgever; the 21st and later new opdrachtgevers in a calendar month (Europe/Amsterdam) per bureau go to admin review. *(Dennis, 30-09)*
- **D10. Kandidaatinzichten** follow the bedrijfsmanager rules, paid from bureau tokens, for the area around the bureau's vestiging(en). *(Dennis, 30-09)*
- **D11. Vacancy ownership.** `Vacancy.CompanyId` = the owning **bureau vestiging**; the opdrachtgever is `IntermediaryClientId`. This is what makes D7/D8 hold everywhere: wallet, invoices, applicant visibility, company pages and metrics key on `CompanyId` today.
- **D12. `Vacancy.Location` is the public pin** (bureau vestiging when hidden, werklocatie when real), so every existing consumer (discovery, travel, matching, talent pool, assistant) is consistent without per-consumer masking. The real werklocatie lives only on the link.
- **D13. Holds, not a new status.** `Vacancy.HoldReason` keeps `Status` untouched (no enum churn); a held vacancy is off the map and out of search/matching, and the bureau sees "Gepauzeerd" + the reason. A hold does **not** extend the end date and does **not** refund tokens.
- **D14. Existing data** is converted once by an idempotent migration service (02): links from the client companies, vacancies re-owned, memberships removed, shell companies deleted or retired, balances moved where the ledger allows it, and a report for admin. **Issued invoices are never altered**; the report lists those in a client's name for manual correction.
- **D15. Mode change after publishing** is free. Hidden → real always works. Real → hidden only ≤ 25 km, with the note "Kandidaten die de vacature al zagen, kennen de werklocatie al."
- **D16. Beyond 25 km** the hidden option is disabled with the reason "De werklocatie ligt {n} km van je vestiging (max. 25 km voor 'onze vestiging')". Nothing is preselected, so the bureau chooses real mode explicitly or picks a nearer bureau vestiging. The server answers 409 `mask_radius_exceeded`.
- **D17. Several bureau vestigingen.**
  - The vacancy form has a "Vestiging" select when the bureau has > 1 vestiging. Default: the nearest one within 25 km of the werklocatie, else the primary vestiging.
  - Bureau vestigingen are added only via KvK under Organisatie › Vestigingen (06), with the same flow as the bedrijfsmanager (Type `Intermediary`).
- **D18. Same opdrachtgever at several bureaus** = independent links that never see each other. A link and a Lobsy werkgever with the same KvK vestiging are not connected in any way. The werkgever registers normally, with no takeover flow.
- **D19. Review threshold** = setting `IntermediaryMonthlyClientReviewThreshold` (default 20). Links over it are `PendingReview` and can't be used for publishing until admin approves. The bureau sees "Lobsy controleert deze opdrachtgever eerst (meestal binnen 1 werkdag)." Admin gets an in-app notification.
- **D20. Declaration** (versioned, `ImClient.Declaration.v1`): "Ik verklaar dat {bureau} een opdracht of raamovereenkomst heeft met deze opdrachtgever om voor deze vestiging personeel te werven of uit te lenen." Stored per link with user + time + version. Links migrated from before this stack get "Bevestig je opdracht" in Te doen; new vacancies for them need the declaration first (409 `declaration_required`). Live vacancies keep running.
- **D21. Hidden-mode region** = the opdrachtgever's gemeente (PDOK `gemeentenaam`), rendered "Regio {gemeente}". Never the street, postcode or place name.
- **D22. No opdrachtgever company-page link,** in either mode. The "via {bureau}" / "Uitzendbureau" badge links to the bureau's public company page.
- **D23. Uitleenregistratie** is one isolated service `ILenderRegistrationCheck` (04):
  - **Registration is not blocked.** The bureau can register, add opdrachtgevers and save drafts. Publishing waits until `Verified` (409 `lender_registration_pending`).
  - **Sources:**
    - `WaadiKvk`: KvK has no public Waadi API, so this is admin verification against the KvK Waadi check, with the reference recorded.
    - `WttaNau`: the public NAU register, expected from 1 July 2027. Provider slot only, off by default.
    - `AdminManual`.
  - **Timeline to keep in mind:** Waadi via KvK applies until 1 Jan 2028. The Wtta applies from 1 Jan 2027.
  - A later werkgever-aanmelding spec (not yet approved) may call the same service from the registration wizard.
- **D24. Bureau vestiging location** must come from KvK or PDOK (`Company.LocationSource`). Registration during a KvK outage geocodes the typed registration address server-side via PDOK instead of the NL centroid. If that fails, the bureau can't publish until `KvkVerificationRetryHostedService` fills it (409 `vestiging_location_unverified`). For employer roles the same PDOK step replaces the centroid; when it fails they keep today's behaviour with `LocationSource = Unknown`. The pin at 52.1326, 5.2913 is never shown again: the map skips `Unknown` rows.
- **D25. Opdrachtgever archive** only when it has no live vacancies (409 `client_has_live_vacancies`). History stays.

## Dependencies (check before 01; re-check A and B before 05 and 06; say in the PR which case applied)
- **A. Werkgever shell** (`docs/werkgever-redesign` 01, and 02/04/06 for Te doen, Sollicitaties and Tokens pages). Check: `git grep -n "class WerkgeverNav" origin/acceptatie -- Jobsy.Web` and `git grep -n "WerkgeverLayout" origin/acceptatie -- Jobsy.Web/Components`.
  - **Present:** 06 adds the Intermediary items to `WerkgeverNav.cs` (role chip, scope chip, §IA) and renders the pages in `WerkgeverLayout`. Reuse werkgever 02 Te doen (add intermediary kinds), 04 Sollicitaties and 06 Tokens where they exist.
  - **Absent:** 06 builds the pages as sections (`Components/Werkgever/Sections/Intermediair/*.razor`) hosted at **today's** routes in `MainLayout`:
    - `/intermediary` = dashboard + Te doen card
    - `/intermediary/opdrachtgevers` (+ `/{id}`)
    - `/intermediary/team` as today
    - `/intermediary/vestigingen`

    Update `RoleNavCatalog.Intermediary` accordingly. There are **no `/werkgever` URLs and no 301s**. Say in PR 06 that werkgever 01 must move these sections (its D1 already covers the Intermediary). Sollicitaties and Tokens stay on today's pages.
- **B. Werkgever 07 (Kandidaatinzichten paid unlock).** Check: `git grep -n "InsightsUnlock" origin/acceptatie -- Jobsy.Core/Enums`.
  - **Present:** 05 adds the Intermediary to its role set with exactly the BM semantics (organisation scope = bureau org, wallet = bureau wallet, area = bureau vestigingen).
  - **Absent:** 05 adds the Intermediary to `IsInsightsRole` / `CandidateInsightsAccess` with **the BM rule of that moment** (today: any wallet of the scope with balance > 0), scope = bureau vestigingen. It adds the test `Intermediary_insights_rule_equals_BM_rule`, so werkgever 07 carries the Intermediary along when it replaces the rule. Say so in PR 05.
- **C. Shared enterprise UI primitives** (admin redesign 01 / werkgever 01). Check: `git ls-tree -r --name-only origin/acceptatie -- Jobsy.Web/Components/Ui/Enterprise Jobsy.Web/Components/Admin/Ui`.
  - **`Components/Ui/Enterprise/` exists:** reuse `EntDataTable`, `EntFilterBar`, `EntTabs`, `EntKpiCard`, `EntDrawer`, `EntScopeChip`.
  - **Only `Admin*` exists:** move them exactly as werkgever README Dependencies B prescribes (own first commit, `git mv`, no behaviour change).
  - **Neither exists:** build the minimal set the file needs under `Components/Ui/Enterprise/` as werkgever 01.2 describes, and say in the PR that the admin/werkgever stacks must consume it.
  - Either way: **one** set. Never a second drawer/table/tabs variant.
- **D. Admin redesign 05 (settings catalog) + 07 (audit log).**
  - Check: `git grep -n "class PlatformSettingsCatalog" origin/acceptatie -- Jobsy.Web` and `git grep -n "interface IAdminAuditLog" origin/acceptatie -- Jobsy.Core`.
  - **Catalog present:** add the threshold setting to group **"Werkgevers"** (or "Intermediairs" if that group exists).
  - **Catalog absent:** add it to today's `/admin/settings` (`SettingsAdmin.razor`) next to the agency price (`AgencyAnnualPriceEuro`), backed by the existing settings endpoint, and mark it `// moves into PlatformSettingsCatalog`.
  - **Audit present:** `[AdminAudit("intermediary.client.review")]`, `[AdminAudit("intermediary.lender.decide")]`, `settings.platform.update`. **Absent:** an interim structured `PlatformLog` row.
- **E. §F feature flags / Werkgevers actief** (`docs/mijn-paspoort` 01). Check: `git grep -n "class RequiresFeatureAttribute" origin/acceptatie -- Jobsy.Core`.
  - **Present:** gate intermediary pages and new `api/intermediary/*` endpoints with `[RequiresFeature(PlatformFeature.Employers)]`.
  - **Absent:** build without it, plus the werkgever-style reflection guard test that passes vacuously until the attribute exists.
- **F. Werkgever-aanmelding** (separate spec, **not yet approved**). Check: `git ls-remote --heads origin 'docs/werkgever-aanmelding*' 'cursor/werkgever-aanmelding*'` and `git grep -n "ILenderRegistrationCheck" origin/acceptatie -- Jobsy.Core`.
  - **Absent (expected):** 04 builds `ILenderRegistrationCheck` in `Jobsy.Core/Interfaces` + `Jobsy.Infrastructure/Services/LenderRegistration/`. The only registration touch point is one call in `CompanyRegistrationService` after an SBI-78 activation (`StartForNewBureauAsync`), so the aanmelding spec can move that call into its wizard without changing the service.
  - **Present:** reuse its service/interface; don't build a second one.
- **G. PDOK Locatieserver** (public, no key): `https://api.pdok.nl/bzk/locatieserver/search/v3_1/free`. 01 adds `AddressGeocoderStub` for tests/dev, selected like `KvkServiceStub` in `Jobsy.Infrastructure/DependencyInjection.cs` (config `Geo:Pdok:Enabled`, default true outside Development/Testing). No other geocoder is added on the server. The existing web-side `NominatimGeocodingClient` stays for candidate address suggestions and is **not** used for KvK addresses.
- **Recommended landing order:** werkgever redesign 01–07 → this stack. Not a hard requirement: every case above has a fallback, and 01–04 don't depend on the shell. Never branch from an unmerged branch of another stack.
