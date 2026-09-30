# Kandidaat banen (banenkaart, lijst, vacature, sollicitaties, bewaard, Match): Cursor run book

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

> **Rules (repeated in every file):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/kandidaat-banen-*` branch; no force-push.
> - ONE PR per file into `acceptatie` (01 standalone, 02+ stacked).
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Employer-side match scores never change; candidate-only data (fit, dislikes, "Staat lager", home address) never leaves candidate-own views.

**What this stack builds:** a new look & feel and a set of honest fixes for everything a **candidate** does with jobs. Dennis approved the mockups and all defaults on 30-09 ("Akkoord").
- **01 is a standalone hotfix** that can run first and on its own, before any other stack. It fixes three bugs: the real travel-time rings (today `/api/travel/isochrones` always returns 404), the desktop banenkaart crash for a candidate with a complete profile, and the "Maximum call stack size exceeded" page error from the `unload → pagehide` shim.
- **Banenkaart:**
  - A logged-in candidate starts at their **profile home address with 20 min by bike**; an anonymous visitor gets a location prompt. The address can be changed in the filter bar.
  - The address field stops dropping characters, and autocomplete prefers addresses over POIs.
  - The rings are stronger, the popups are consistent, the filter bar has chips and a visible keyword search, and the filter badge is 0 by default.
- **Fit % is honest.** It is shown **only** when the candidate has done at least the culture **or** the values test; otherwise the card says "Maak je paspoort af".
  - Candidate-side scores are recalibrated to spread over ~55–90 %, and "strong fit" starts at 75 %. Every card shows one "why" line.
  - **Employer-side scores do not change.**
- **Dislikes** from De ontdekkingsreis rank a job **lower, never hide it**. The candidate's own card shows "Staat lager: …".
- **Employer attributes** (kernwaarden, branche, engagement badges) come from the werkgever-aanmelding stack. This stack only **displays** them.
- **Uitzendbureau hidden mode:** the pin, mini map and travel time use the bureau's vestiging. There is no Route/Street View to the real workplace.
- **Sollicitaties:** a new **status history** table records each change with its date from now on. The candidate sees a dated timeline. **Bewaard is a tab inside Sollicitaties**, not a nav item.
- **Match:** the desktop dialog and mobile swipe are refreshed with "Waarom jij past" per DNA dimension. "Laten schieten" never hides a job.
- Smaller UX fixes: the focus box, the blank strip on mobile, hardcoded strings, a full-width desktop list, and duplicate seed photos.
- Everything respects the **Werkgevers actief** switch (paspoort 01) and the `/banenkaart` route (landing 04), each through a dependency check with a fallback.

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-hotfix-isochronen-kaartcrash.md`: **standalone hotfix** (not stacked). (a) `ValhallaIsochroneService.ReadMinutes` parses decimals (`30.0`) and rounds, so real rings show. (b) Desktop banenkaart crash for a complete-profile candidate: reproduce, root cause, regression test. (c) Idempotent `unload→pagehide` shim in `App.razor` (no "Maximum call stack size exceeded") | `cursor/kandidaat-banen-hotfix` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-fundament.md`: dependency cases + fallbacks (Dependencies A–G), strings module `UiStringsKandidaatBanen.cs` (5 languages), `features/kandidaat-banen.css`, label catalog `KbLabels`, shared card parts (`KbFitPill`, `KbWhyLine`, `KbBadgeRow`, `KbTravelTime`), feature gating of candidate job pages/APIs, small UX fixes (focus box, hardcoded strings, inline `style=` colours, duplicate seed photos in the seed only) | `cursor/kandidaat-banen-2` | `cursor/kandidaat-banen-hotfix` (or `origin/acceptatie` when PR 01 is merged) | `acceptatie` |
| 03 | `03-banenkaart.md`: map start from the profile address (20 min bike) / location prompt for anonymous visitors, address field fix, address-first autocomplete, filter bar (chips, visible search, badge 0 by default, short mobile sheet), stronger rings + legend, one docked popup for pins and clusters, side list, bottom sheet, blank strip, breakpoints, glyph 404s | `cursor/kandidaat-banen-3` | `cursor/kandidaat-banen-2` | `acceptatie` |
| 04 | `04-fit-dislikes.md`: fit % gate (culture or values test), candidate display calibration (55–90, strong ≥ 75), why line, 4 DNA bars from one source, dislike down-rank + "Staat lager: …" | `cursor/kandidaat-banen-4` | `cursor/kandidaat-banen-3` | `acceptatie` |
| 05 | `05-werkgever-uitzendbureau.md`: employer kernwaarden / branche / engagement badges on cards + detail (display only, from werkgever-aanmelding 08/09), uitzendbureau hidden mode (bureau vestiging pin/travel, no Route/Street View, honest label) | `cursor/kandidaat-banen-5` | `cursor/kandidaat-banen-4` | `acceptatie` |
| 06 | `06-lijst-vacature.md`: full-width desktop list mode, mobile list with "Kaart" button, vacancy detail redesign (fit panel, employer tiles, travel card with mini isochrone + transport switch, one primary action, sticky mobile apply bar) | `cursor/kandidaat-banen-6` | `cursor/kandidaat-banen-5` | `acceptatie` |
| 07 | `07-sollicitaties-bewaard.md`: `ApplicationStatusHistory` table + one recorder, dated timeline, "Wat nu?" per status, kind rejection with similar jobs, Bewaard tab redesign (status per saved job, fit, remove, apply / similar jobs) | `cursor/kandidaat-banen-7` | `cursor/kandidaat-banen-6` | `acceptatie` |
| 08 | `08-match.md`: Match desktop dialog + mobile swipe refresh, "Waarom jij past" per DNA dimension, the "Hierna" column, "Laten schieten" = to the end of the deck (never hidden, D12), keyboard hints, uses the calibrated fit | `cursor/kandidaat-banen-8` | `cursor/kandidaat-banen-7` | `acceptatie` |
| 09 | `09-e2e-rapport.md`: Playwright E2E scenarios for all candidate flows (desktop 1440 + mobile 390, Werkgevers ON/OFF), docs, stack-end report | `cursor/kandidaat-banen-9` | `cursor/kandidaat-banen-8` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations/resources), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/kandidaat-banen-3b`).

## Pointer prompt (the only prompt needed; it runs 01 … 09)
```
Run the Kandidaat banen stack. First: git fetch origin && git show origin/docs/kandidaat-banen:docs/prompts/kandidaat-banen/00-README.md — read it completely.
Then read and execute each file in docs/prompts/kandidaat-banen/ on that branch strictly in the order the README's table lists (01 … 09; a/b splits where a file allows it), one file = one PR.
File 01 is a standalone hotfix: it branches from origin/acceptatie, is not stacked, and its PR body starts with "Standalone hotfix (not stacked)". If PR 01 already exists (the hotfix was run on its own), don't redo it: reuse its branch.
File 02 branches from cursor/kandidaat-banen-hotfix (or from origin/acceptatie if PR 01 is already merged); every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 02, run the dependency checks in the README's "Dependencies" section and follow the fallback it prescribes for each one; say in PR 02 which case applied.
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes.
At the end report: file → branch → PR number → status, plus the dependency cases and anything deferred.
```

### Pointer prompt: hotfix 01 only
```
Run only the hotfix from the Kandidaat banen stack. First: git fetch origin && git show origin/docs/kandidaat-banen:docs/prompts/kandidaat-banen/00-README.md (read "How to run" and §0) and git show origin/docs/kandidaat-banen:docs/prompts/kandidaat-banen/01-hotfix-isochronen-kaartcrash.md — read it completely.
Execute only file 01: branch cursor/kandidaat-banen-hotfix from origin/acceptatie (not stacked on anything), ONE PR into acceptatie whose body starts with "Standalone hotfix (not stacked)".
Fix (a) the isochrone decimal parse, (b) the desktop banenkaart crash for a complete-profile candidate (reproduce first, root cause, regression test, no speculative rewrites), (c) the idempotent unload→pagehide shim. Build and test; if red or a success criterion can't be met, push, open the PR as draft, stop and report.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes. Don't start file 02.
Report: branch → PR number → status, the crash root cause in one paragraph, and the test list.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md` and `docs/release-flow.md`.
2. **File 01** stands alone. Run it as described in the file, whether or not the rest of the stack runs.
3. Before 02, run the **Dependencies** checks below and note the outcome (it goes into PR 02).
4. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column:
      - File 01: `git checkout -b cursor/kandidaat-banen-hotfix origin/acceptatie`.
      - File 02: `git checkout -b cursor/kandidaat-banen-2 cursor/kandidaat-banen-hotfix` (or `origin/acceptatie` if PR 01 is merged).
      - Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, and the Playwright suites the file names if you can (they soft-skip without `JOBSY_E2E_BASE_URL`). Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. The body starts with `Standalone hotfix (not stacked)` (01) or `Stacked on #<prev PR> (<prev branch>)` (02+), then the PR body items from §0.
   6. Note the PR number and go on to the next file.
5. **Stop and report** in any of these cases:
   - tests fail and you can't fix them inside the file's scope
   - a success criterion can't be met
   - the code contradicts this spec in a way you can't resolve safely

   Push what you have, open that PR as **draft** with the failure described, and don't continue.
6. At the end, report the table file → branch → PR number → status (green or draft/red), plus the dependency cases and anything deferred.
7. **Never** merge, deploy, or use rule `123` (`.cursor/rules/shortcut-123.mdc`). **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: each file adds its own migration on top of the previous one. Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run, don't rebase. Only when a conflict blocks you, `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.
   - Never branch from an unmerged branch of **another** stack. Dependencies are detected on `origin/acceptatie` only.

---

## §0. Shared rules (every file)
- **Branches stack** (see above). **ONE PR per file, always into `acceptatie`.**
  - 01's body starts with `Standalone hotfix (not stacked)`.
  - 02's body starts with `Stacked on #<PR 01> (cursor/kandidaat-banen-hotfix)`, or `Stacked on: none (PR 01 already merged)`.
  - Later bodies start with `Stacked on #<prev PR> (<prev branch>)`.
  - The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
- **Stop on red.** Run `dotnet build` + `dotnet test` after each file. If it is red and not fixable in scope: stop, push, open a draft PR with the failure, and report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST); live acceptatie ran the same commit when this spec was written. **Re-check line numbers before editing**; `VacancyDiscovery.razor` (3.575 lines), `VacancyDetail.razor` (2.015) and `jobMap.js` (3.937) move often.
- **Mockups:** branch `docs/kandidaat-banen`, folder `docs/mockups/kandidaat-banen/`. Read them with `git fetch origin docs/kandidaat-banen && git show origin/docs/kandidaat-banen:docs/mockups/kandidaat-banen/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - Desktop 1440×900: `kd-d1-banenkaart.png`, `kd-d2-lijst.png`, `kd-d3-vacature.png`, `kd-d4-sollicitaties.png`, `kd-d5-bewaard.png` (Sollicitaties › tab Bewaard), `kd-d6-match.png`.
  - Mobile 390 wide @2x: `kd-m1-kaart.png`, `kd-m2-lijst.png`, `kd-m3-vacature.png` (uitzendbureau hidden mode), `kd-m4-sollicitaties.png`, `kd-m5-match.png`.
  - The mockups are a **layout and copy reference**. The persona (Samira El Amrani, Herenstraat 20 Wateringen), employers, fit percentages and dates are **Voorbeelddata**. The "Voorbeelddata" pill is mockup-only. The rings in the mockups are real Valhalla bike isochrones; the photos are the app's own fallback WebP images.
  - **Where the mockup and this spec differ, this spec wins.** Known differences:
    - **Nav.** The mockups show Dennis's latest order: De ontdekkingsreis · Mijn Paspoort · Carrière · Banenkaart · Sollicitaties. **This stack does not change the nav**; it only depends on it (Dependencies F). Bewaard is never a nav item (paspoort D8).
    - **"Gesprek ma 5 okt · 10:00 · Groenhof, Wateringen"** on `kd-d4`/`kd-m4`: interview times and places are **not** stored anywhere. The step shows the **date of the status change** only (07). No interview data model is added.
    - **"Gezien door werkgever"** is a real, recorded event from 07 onwards (the employer opens the application). Older applications don't show it (D4).
    - **Engagement tiles** on `kd-d3` ("door werkgever opgegeven") are only shown when werkgever-aanmelding 09 has landed (Dependencies B). The labels come from that stack, not from the mockup.
    - **"82% past bij jou"** on every card assumes the candidate did the culture or values test. Without one, the card shows "Maak je paspoort af" (D2).
    - **Legend "echte wegen, geen cirkel"** is shown only when the rings are real isochrones. With the circle fallback (OV, or Valhalla down), the legend says "ongeveer" (03).
    - **Transport switch** on `kd-d3` shows Fiets/Auto/Lopen/OV. OV has no Valhalla transit costing and keeps the honest circle fallback with the label "ongeveer".
    - The **"Nieuw voor jou"** block on `kd-d3` = the existing `MatchGaps` of the match, not a new feature.
    - The **Lobsy tip** cards are optional content, and only use existing tip/assistant copy. Don't invent a tip engine.
- **Design system.** Follow `.cursor/rules/design-system.mdc` plus these rules, which win when they conflict:
  - **Candidate visual language** (as paspoort / ontdekkingsreis): warm `--surface` cards on the pearl page, rounded 16 px cards, the existing type scale, `--brand` headings, one accent per screen.
  - **Colours:** tokens only (`app.css :root`); no new hex values.
  - **Inline styles:** no inline `style=""` in the `.razor` files this stack touches, with **one exception**. A DB category colour (`ColorHex`) may be set only as a single custom property through one helper, `KbCategoryColor.Style(hex)` (02.6). The helper validates `^#[0-9A-Fa-f]{6}$` and otherwise falls back to a token. The CSP allows `style-src-attr` but requires a nonce for `<style>` elements, so a custom property is the sanctioned route. A guard test (02) checks this.
  - **Type:** weights 400/600 (700 only for the page `h1`); only the type scale; spacing from `--space-*`.
  - **Layout:** breakpoints **640/900/1024 only**. The map-only split `_wideViewport` (769 px today) moves to 900 (03). Use logical properties; chevrons flip under `[dir="rtl"]` (Arabic is a candidate language).
  - **Calm UI:** one primary action per card/sheet/screen, no decorative emoji, and status pills pair colour **and** a label. A card shows at most **2 badges** + "+n" (D7).
  - **Focus:** visible `:focus-visible` rings on interactive elements. The page `h1` that receives programmatic focus after navigation gets **no** visible box (02.5).
- **Strings:** candidate pages are multilingual. All new text goes through `@Culture["…"]` in a new module `Localization/UiStringsKandidaatBanen.cs` with prefix `Kb.`, merged like `UiStringsMatch.MergeAll(nl, en, pl, ro, ar)`, in **all 5 languages** (nl, en, pl, ro, ar).
  - `LocalizationParityReportTests` stays green without exemptions.
  - Existing keys are reused where they already say the right thing (`Match.*`, `Apps.Status.*`, `Discovery.*`).
  - **No hardcoded Dutch** in markup, C# or JS that this stack touches. JS strings come in through a `data-*` attribute or an init options object.
- **No raw enum names in the UI.** `ApplicationStatus`, the new `ApplicationStatusEventKind`, the timeline step, the saved-job state and the transport mode all go through `KbLabels` (02.3).
  - Reflection test: every value has a non-empty label in all 5 languages.
  - bUnit test: no rendered candidate job page contains an enum member name.
- **Terminology (nl, final):**

  | Use | Instead of |
  |---|---|
  | past bij jou (82% past bij jou) | Match, matchscore (on cards) |
  | Sterke match (≥ 75 %) | Topmatch (as a band label; "Jouw top-match" stays for the tile) |
  | Maak je paspoort af | Vul je profiel aan, 0 % |
  | Staat lager: {reden} | Verborgen, gefilterd |
  | Reistijd · {n} min fietsen | Afstand (when a travel time exists) |
  | via uitzendbureau {bureau} | Aangeboden door {bureau} |
  | Verstuurd · Gezien door werkgever · Gesprek · Uitslag | Pending, Accepted |
  | Niet gekozen | Afgewezen (candidate-facing) |
  | Bewaard | Geliked, favorieten |

- **Gating (Werkgevers actief, Dependencies C):** every candidate job surface this stack touches is gated when Werkgevers is OFF. That covers banenkaart, lijst, vacature detail, sollicitaties, bewaard, Match and every new endpoint. The gate is server-side first; hiding the UI is **never** the only guard.
- **Privacy:**
  - A candidate's dislikes, fit score and "Staat lager" note are **candidate-own only**. They never appear in employer, shared (`/candidate/shared`), public, SEO or OG views, and are never logged.
  - Hidden-mode vacancies never send the opdrachtgever's identity or coordinates to the client (D5).
- **Docs and guards to update when routes/pages change:**
  - `docs/ROUTES.md` (`RoutesDocFreshnessTests`)
  - `Seo/PageSeoCatalog.cs` (`PageSeoTests`)
  - `Help/PageHelpDocs.cs` (`PageHelpDocsTests`)
  - `BlazorPageRoleAttributesTests`
  - `CHANGELOG.md`
- **CSS:** new `wwwroot/css/features/kandidaat-banen.css` (BEM block `kb-…`) for the shared card parts, timeline and tabs.
  - Page-specific rules go into the **existing** feature files: `banenkaart.css`, `applications.css`, `match-desktop.css`.
  - Every changed stylesheet is linked in `Components/App.razor` (normal list **and** `<noscript>`) with a bumped `?v=YYYYMMDD-kb`, and added to or updated in `Jobsy.Tests/asset-versions.json` (`AssetVersionGuardTests`). The same goes for changed JS (`jobMap.js`, `jobsyMapLibre.js`, `app-core.js` for `jobsyGeo`). If a `.min.js` twin exists (e.g. `jobsyMapLibre.min.js`), regenerate it the way the repo produced it before (check `git log -- <file>`), and keep both in sync.
  - Don't append to `app.css`.
- **Migrations** (07 only, plus 02 if a fallback needs one): `dotnet ef migrations add …` in `Jobsy.Infrastructure`, with the snapshot updated. `EfModelSnapshotTests`, `PendingModelChangesTests` and `EfMigrationDiscoveryTests` stay green.
- **Must NOT touch:**
  - **employer-side match scores:**
    - `Application.MatchPercent` (set at apply, `ApplicationsController` ~L793)
    - `CandidateMatchSnapshotService` and its snapshot version
    - `MatchScoreWeights.StrongMatchThreshold` (70) as used by employer views
    - `ProfileVacancyMatchCalculator` weights
  - The candidate calibration is a **display layer** on top (04, D2).
  - the nav order and `RoleNavCatalog` (Dependencies F), the paspoort pages, the ontdekkingsreis wizard
  - the employer-side entry of kernwaarden / branche / engagement (werkgever-aanmelding 08/09) and the intermediair ownership model (intermediair 02/03); this stack only **reads** them
  - `ICompanyCultureLookup` / `CultureFitBuilder` scoring, and the Cultuurscan / values test questions or storage
  - the apply flow (consent, e-mail verification, snapshot fields) except the one status-history hook (07)
  - `MatchDeck` ordering rules beyond "Laten schieten = lower" (08)
  - the cookie banner and `app-core.js`, except the `jobsyGeo` functions (03 changes the launch origin)
- **PR description:**
  - what changed and why
  - the dependency cases (02) or "n/a"
  - screenshots at desktop 1440 and mobile 390 of each new or changed screen, both for a complete-profile candidate (`kandidaat@jobsy.local`) and an incomplete one
  - test list
  - "Out of scope / deferred"
- **Run** `dotnet build` and `dotnet test` (unit + bUnit). Run the Playwright suites you touched if you can (`JOBSY_E2E_BASE_URL`, `JOBSY_E2E_CANDIDATE_EMAIL` / `_PASSWORD`); say so if you couldn't.

---

## §S. Screens and URLs (the contract for all files)
No new public URLs, and none are renamed. The table maps each mockup to the page that implements it.

| Mockup | Page / component | URL | Files |
|---|---|---|---|
| `kd-d1`, `kd-m1` | `VacancyDiscovery.razor` in map mode | `/banenkaart` when landing 04 has landed, otherwise `/` (Dependencies E, one constant `KbRoutes.Map`) | 03, 04, 05 |
| `kd-d2`, `kd-m2` | `VacancyDiscovery.razor` in list mode: desktop gets a new "Kaart · Lijst" toggle (query `?weergave=lijst`); mobile keeps today's `ToggleMobileView` | same | 06 |
| `kd-d3`, `kd-m3` | `Pages/VacancyDetail.razor` | existing detail route | 05, 06 |
| `kd-d4`, `kd-m4` | `Pages/Candidate/Applications.razor` | `/candidate/applications` | 07 |
| `kd-d5` | `Pages/Candidate/Liked.razor`, shown as the **Bewaard tab of Sollicitaties** (`CandidateJobListTabs`, paspoort D8) | `/candidate/liked` (unchanged; the Sollicitaties nav item is active) | 07 |
| `kd-d6`, `kd-m5` | `Components/Match/MatchDeckDialog.razor` (desktop) / `Pages/Candidate/MatchPage.razor` (mobile) | existing | 08 |

`/candidate/shared` keeps its current content and gets the same tab bar when paspoort D8 is present. It never shows fit, dislikes or "Staat lager".

## §F. Fit display rules (04 builds them; every file uses them)
- **One source:** `Jobsy.Core/Rules/CandidateFitDisplay.cs` (pure). Input: the existing `ProfileVacancyMatch` + a `CandidateFitGate` (has culture test, has values test). Output: `CandidateFit?`, which is null when gated, and otherwise has these fields:
  - `Percent` (int, 55–90 by construction)
  - `Band` (`Strong` ≥ 75, `Good` 65–74, `Some` < 65)
  - `WhyLine` (one short sentence)
  - `Dimensions` (Cultuur / Waarden / Competenties / Interesses, each 0–100 or null)
- **Gate (D2):** `CandidateFitGate.IsOpen` = the culture test is complete **or** the values test is complete (the same completion checks the paspoort uses). When it is closed, every candidate surface shows "Maak je paspoort af" + a link to the paspoort test. That covers the card, list, detail, Match, the top-match tile and saved jobs. It shows **no** number, **no** band colour and **no** "Sterke match".
- **Anonymous visitors** never see a fit.
- **Sorting "Past het best"** and the candidate `minMatchPercent` filter use `CandidateFit.Percent`, then the dislike down-rank (§F.dislike). They never use the raw employer percentage.
- **Dislike down-rank:** a vacancy that matches one of the candidate's dislikes (Dependencies D) gets a sort penalty of `KbRanking.DislikePenalty` (15 points), is **never hidden**, keeps its shown percentage, and shows "Staat lager: {reden}" on the candidate's own card and detail.
- **Why line:** the first `MatchWhy` item mapped to a short candidate sentence (`Kb.Why.*`), e.g. "Je helpt graag mensen · rustig team". There is never an empty why line; with no why data the line is omitted.

## §D. Data model (only 07 adds a table)
- `ApplicationStatusHistory`:
  - `Id` (Guid)
  - `ApplicationId` (FK, cascade)
  - `Kind` (`ApplicationStatusEventKind`: `Created`, `StatusChanged`, `EmployerViewed`)
  - `FromStatus` (`ApplicationStatus?`)
  - `ToStatus` (`ApplicationStatus?`)
  - `OccurredAtUtc` (DateTime, UTC)
  - `ActorKind` (`Candidate`, `Employer`, `System`)
  - `ActorUserId` (Guid?, never exposed to the candidate)

  Index: `(ApplicationId, OccurredAtUtc)`. At most one `EmployerViewed` row per application (filtered unique index).
- **No backfill of invented dates (D4).** Existing applications get no history rows. The timeline shows "Verstuurd" (`CreatedAt`) + the current status (dated with `RespondedAt` when that is set, otherwise undated).

## Decisions
- **D1. File 01 is a standalone hotfix** that runs first and on its own, from `origin/acceptatie`, not stacked. *(Dennis, 30-09)*
- **D2. Fit %** is only shown with the culture **or** values test done; otherwise "Maak je paspoort af". Recalibrate so scores spread ~55–90 %; strong fit from 75 %. Show the "why" line on cards. *(Dennis, 30-09)*
  - *Default:* the recalibration is a **candidate-side display layer** (`CandidateFitDisplay`). Employer scores, `Application.MatchPercent` and the snapshot version stay the same.
  - *Default:* the calibration anchors live in one place, with a distribution report test (04.4). This way they can be re-tuned when werkgever-aanmelding 01 (culture reaches the match) lands.
- **D3. Uitzendbureau hidden mode:**
  - The pin, mini map and travel time use the bureau's vestiging, and there is no Route/Street View to the real workplace. *(Dennis, 30-09)*
  - It reuses the intermediair stack (the bureau vestiging owns the vacancy, `IntermediaryPublicIdentity`, `IntermediaryBadge`, D5 label). *(Dennis, 30-09)*
  - *Default (conflict resolved):* Dennis's brief says "real workplace shared after applying". Intermediair **D4** (Dennis, 30-09) is stricter: the name + workplace are revealed **only** in the candidate's own application from status `EmployerContacting` ("Uitgenodigd") and while `Hired`. This stack follows **intermediair D4** as the single rule and implements no reveal of its own. When intermediair 03 is absent, nothing is revealed (Dependencies A). Dennis can override this.
- **D4. Status timeline:** a new status history table records each change with its date from now on. Older applications show only "Verstuurd" (created date) + the current status. *(Dennis, 30-09)*
  - *Default:* "Gezien door werkgever" is a recorded `EmployerViewed` event (the first time an employer user opens the application detail).
  - *Default:* the current status of an old application uses `RespondedAt` as its date when that is set, because that is a real stored date.
- **D5. Map start:**
  - *Default:* the start view is literally "profile home, 20 min, fiets", also when the profile has its own `PreferredTransport` / `MaxTravelMinutes`. Those preferences keep feeding the match score but no longer set the map start (today `VacancyDiscovery` ~L1386–1394 copies them into the filters). Dennis can say if the profile preference should win.
  - A logged-in candidate starts at the profile home address with 20 min by bike. An anonymous visitor gets a location prompt. The address can be changed in the filter bar. *(Dennis, 30-09)*
  - Fix the address field dropping characters and autocomplete preferring POIs. *(Dennis, 30-09)*
  - *Default:* the address suggestions use **PDOK Locatieserver** (official BAG addresses, free, no key) first, with Nominatim (`layer=address`) as the fallback, behind the existing `IGeocodingClient`.
- **D6. Bewaard is not a nav item.** It is a tab inside Sollicitaties (paspoort D8, max 5 nav items). This stack does not change the nav order; it depends on Dennis's order add-on (Dependencies F). *(Dennis, 30-09)*
- **D7. Employer kernwaarden / branche / engagement badges** reuse werkgever-aanmelding 08/09; this stack defines no new entry. *(Dennis, 30-09)*
  - *Default:* a card shows at most **2 badges**: the fit pill + 1 secondary chip, priority "Staat lager" > a checked engagement claim > a self-declared engagement claim. The rest go into "+n". The detail shows all of them (werkgever-aanmelding 09 card rule).
- **D8. Dislikes:** "Staat lager: …" ranks lower and never hides. It depends on the ontdekkingsreis dislike data (paspoort 06). *(Dennis, 30-09)*
  - *Default (extension of paspoort 06):* paspoort 06 shows dislikes only in the paspoort accordion and wizard step 6. This stack adds a **candidate-own-only** note "Staat lager: nachtdienst" on the candidate's own cards and detail. It never appears in shared, employer or public views.
- **D9. Smaller UX fixes** from the review are in scope: the focus box, the blank strip on mobile, hardcoded strings, the filter badge default, the filter form, visible keyword search, a full-width desktop list, a consistent popup, and duplicate seed photos (seed only). *(Dennis, 30-09)*
- **D10. Gating:** respect the Werkgevers actief switch and the `/banenkaart` route from landing 04. *(Dennis, 30-09)*
- **D11. Last file:** Playwright E2E scenarios for the candidate flows + a stack-end report. *(Dennis, 30-09)*
- **D12.** *(default)* "Laten schieten" in Match stays **session-only**, as today (`HandleReject` stores nothing). It moves the card to the **end of the deck** ("Je ziet hem later nog"), never hides it, and never affects the map or list. No new table.
- **D13.** *(default)* The default travel filter becomes **20 min by bike** (was 30). The filter badge counts only deviations from the **candidate's own defaults** (profile address, profile transport, 20 min, age from the profile). A fresh page shows badge 0.

## Dependencies (check before 02; say in PR 02 which case applied)
Check on `origin/acceptatie` after `git fetch origin`. Record the case per dependency in PR 02 and in a short `docs/features/kandidaat-banen.md` (created in 02, updated by later files). Fallback code is written against today's types and marked `// KB-FALLBACK(<letter>): replaced when <stack> lands`, so the other stack can find and delete it with one grep.

- **A. Intermediair 02/03 (the bureau vestiging owns the vacancy, `IntermediaryPublicIdentity`, `IntermediaryBadge`).** Check: `git grep -n "IntermediaryPublicIdentity" origin/acceptatie -- Jobsy.Core` and `git grep -n "IntermediaryClientId" origin/acceptatie -- Jobsy.Core/Entities`.
  - **Present:** 05 uses `IntermediaryPublicIdentity` for the name, pin, label and reveal, and `IntermediaryBadge` for the pill. It adds **no** logic of its own; the hidden-mode travel label is intermediair D5's "Reistijd tot de vestiging van het bureau · werklocatie in de regio {gemeente}".
  - **Absent:** 05 adds a minimal candidate-side mask in `Jobsy.Core/Rules/KbHiddenIntermediaryMask.cs`. When `IntermediaryCompanyId` is set and `ShowClientAddressOnMap == false`:
    - the pin, mini map and travel time use the **bureau organisation's** coordinates, and Route/Street View are hidden
    - the label is "via uitzendbureau {bureau}"
    - there is no reveal at all

    It is marked `// KB-FALLBACK(A): superseded by intermediair 03`. It also changes the one leak `IntermediaryVacancyRules.ResolvePublicDisplay` ("Pin always follows the vacancy workplace") for candidate DTOs **only**, and updates the test `ResolvePublicDisplay_masked_uses_vacancy_coords_over_intermediary_hq` with a comment. Say in PR 05 that intermediair 03 must delete the fallback.
  - Either way: no hidden-mode DTO contains the opdrachtgever's name, coordinates, gemeente-level address or Route/Street View URL (test in 05).
- **B. Werkgever-aanmelding 08/09 (branche labels, kernwaarden, engagement claims).** Check: `git grep -n "class CompanyValuesProfile" origin/acceptatie -- Jobsy.Core`, `git grep -n "class EngagementCatalog" origin/acceptatie -- Jobsy.Core` and `git grep -n "WorkTypeLabels" origin/acceptatie -- Jobsy.Core/Entities/Company.cs`.
  - **Present:** 05 reads these into the candidate DTOs:
    - `Company.WorkTypeLabels` (max 4)
    - `CompanyValuesProfile` (3 of the 10 `CompanyValueCards`, root organisation, texts `WaProfile.Card.*`)
    - the non-`Removed` `CompanyEngagementClaim` rows from `VacancyDiscoveryRecord.EngagementItems`, with the honest labels from that stack: "Door werkgever opgegeven", "Gecontroleerd door Lobsy", "Gecontroleerd bij SBB"

    It reuses that stack's label keys; it defines no new ones for the same meaning.
  - **Absent (any of the three):** the matching block (branche pill / kernwaarden tiles / engagement badges) is **not rendered**. No placeholder, no mock data, no new entry UI. Say which blocks were hidden.
  - **Werkgever-aanmelding 01** (`ICompanyCultureLookup`, the employer culture reaches the match) only changes the raw inputs. Check: `git grep -n "interface ICompanyCultureLookup" origin/acceptatie -- Jobsy.Core`. Say in PR 04 whether it was present when the calibration anchors were derived (D2).
- **C. Paspoort 01/02 (Werkgevers actief + nav slots + Bewaard tab D8).** Check: `git grep -n "class RequiresFeatureAttribute" origin/acceptatie -- Jobsy.Core`, `git grep -n "PlatformFeature.Employers" origin/acceptatie -- Jobsy.Core Jobsy.Web Jobsy.Api` and `git grep -n "CandidateJobListTabs" origin/acceptatie -- Jobsy.Web`.
  - **Flags present:**
    - New candidate job APIs get `[RequiresFeature(PlatformFeature.Employers)]` (controllers) or `.RequireFeature(PlatformFeature.Employers)` (minimal endpoints).
    - Pages go through `FeatureRouteGate`.
    - Every new endpoint is added to the reflection expected-list test.
    - OFF → 404 `feature_disabled` (API) and the gate's page (UI).
  - **Flags absent:** no gating code. Add a `// KB-FALLBACK(C): gate with PlatformFeature.Employers when paspoort 01 lands` comment on each new endpoint, and say so in PR 02.
  - **`CandidateJobListTabs` present:** 07 redesigns the Bewaard content **inside** that tab bar and keeps its rule (the tabs render only when Bewaard is not in the nav).
  - **Absent:** 07 redesigns `/candidate/liked` in place and adds **no** tabs and **no** nav change. Bewaard stays wherever the nav has it today.
- **D. Paspoort 06 (private dislikes).** Check: `git grep -n "class CandidatePrivatePreferences" origin/acceptatie -- Jobsy.Core` and `git grep -n "class DislikeMatchRules" origin/acceptatie -- Jobsy.Core`.
  - **Present:** 04 reads `DislikesJson` through `DislikeMatchRules` (e.g. `night-shifts` → `Vacancy.LegalNightShift23To06`) and applies §F's down-rank + note. The dislike codes map to `Kb.Dislike.{code}` short reasons ("nachtdienst").
  - **Absent:** there is no down-rank and no note. The code path exists behind an `IKbDislikeSource` that returns "none" (`// KB-FALLBACK(D)`).
- **E. Landing 04 (`/banenkaart`).** Check: `git grep -n "PublicRoutes.Banenkaart" origin/acceptatie -- Jobsy.Web`.
  - **Present:** every link, redirect and test in this stack uses `PublicRoutes.Banenkaart` (and `AuthRedirects.BanenkaartPath` after login). The Playwright tests use `E2eRoutes.Banenkaart`.
  - **Absent:** use one constant `KbRoutes.Map = "/"` (`Home.razor`), marked `// KB-FALLBACK(E): PublicRoutes.Banenkaart (landing 04)`. No string literals elsewhere.
- **F. Nav order add-on (after Mijn Paspoort 08): De ontdekkingsreis · Mijn Paspoort · Carrière · Banenkaart · Sollicitaties.** Check the candidate items in `Jobsy.Web/Navigation/RoleNavCatalog.cs` on `origin/acceptatie` (`CandidateItems(...)` with paspoort + Werkgevers ON).
  - **This stack never changes the nav** in either case. It only relies on:
    - the Sollicitaties item being active on `/candidate/applications`, `/candidate/liked` and `/candidate/shared`
    - the map item pointing at `KbRoutes.Map` / `PublicRoutes.Banenkaart`
  - If the order differs, say so in PR 09's report; don't fix it here.
- **G. Hotfix 01.** Check: `git grep -n "TryGetDouble" origin/acceptatie -- Jobsy.Infrastructure/Services/ValhallaIsochroneService.cs`. **Merged:** 02 branches from `origin/acceptatie`. **Not merged:** 02 branches from `cursor/kandidaat-banen-hotfix`.
- **Recommended landing order:** hotfix 01 (any time) → paspoort 01–08 + nav add-on → landing 04 → werkgever-aanmelding 08/09 → intermediair 02/03 → this stack 02–09. Not a hard requirement: every case above has a fallback.
