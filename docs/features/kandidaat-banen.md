# Kandidaat banen

Shared foundation for the candidate jobs stack (banenkaart, lijst, vacature, sollicitaties, bewaard, Match). Later files update this doc.

## Dependencies (checked on `origin/acceptatie`, 2026-09-30)

| Dep | Check | Outcome | What this stack does |
|---|---|---|---|
| **A** Intermediair 02/03 (`IntermediaryPublicIdentity`, `IntermediaryClientId`) | ABSENT | ABSENT | File **05** adds `KbHiddenIntermediaryMask` (`// KB-FALLBACK(A)`). |
| **B** Werkgever-aanmelding 08/09 (`CompanyValuesProfile`, `EngagementCatalog`, `Company.WorkTypeLabels`) | ABSENT (all three) | ABSENT | File **05** hides branche / kernwaarden / engagement blocks. No placeholders. |
| **B′** Werkgever-aanmelding 01 (`ICompanyCultureLookup`) | ABSENT | ABSENT | File **04** notes calibration anchors without employer culture lookup. |
| **C** Paspoort 01/02 (`RequiresFeatureAttribute`, `PlatformFeature.Employers`, `CandidateJobListTabs`) | ABSENT | ABSENT | No gating code. New endpoints get `// KB-FALLBACK(C)` comments. File **07** redesigns `/candidate/liked` in place (no tabs, no nav change). |
| **D** Paspoort 06 (`CandidatePrivatePreferences`, `DislikeMatchRules`) | ABSENT | ABSENT | `IKbDislikeSource` returns none (`// KB-FALLBACK(D)`). |
| **E** Landing 04 (`PublicRoutes.Banenkaart`) | ABSENT | ABSENT | `KbRoutes.Map = "/"` (`// KB-FALLBACK(E)`). |
| **F** Nav order add-on | ABSENT (still Zoeken / Bewaard / Sollicitaties / Carrière / Profiel) | ABSENT | **Do not change nav.** Report only in file 09. |
| **G** Hotfix 01 (`TryGetDouble` in Valhalla) | NOT MERGED on acceptatie | NOT MERGED | Branch `cursor/kandidaat-banen-2` from `cursor/kandidaat-banen-hotfix`. |

## Routes (`KbRoutes`)

| Constant | Value | Notes |
|---|---|---|
| `Map` | `/` | KB-FALLBACK(E) until landing 04 |
| `Applications` | `/candidate/applications` | |
| `Saved` | `/candidate/liked` | Bewaard |
| `Shared` | `/candidate/shared` | |
| `PaspoortTests` | `/profiel` | Culture `/candidate/culture`, values `/candidate/values` until paspoort 02 |

## Shared parts (file 02)

- Strings: `Localization/UiStringsKandidaatBanen.cs` (`Kb.*`, 5 languages)
- Labels: `KandidaatBanen/KbLabels.cs` (candidate-facing enums)
- CSS: `wwwroot/css/features/kandidaat-banen.css` (BEM `kb-`)
- Card parts: `KbFitPill`, `KbWhyLine`, `KbBadgeRow`, `KbTravelTime`
- Category colour: `KbCategoryColor.Style(hex)` — only allowed `style=` helper on candidate job surfaces

## Fit & dislikes (file 04)

- Gate (D2): `CandidateFitGate` — culture **or** values test complete; else "Maak je paspoort af" (no %).
- Display layer: `CandidateFitDisplay` (55–90, strong ≥ 75) — employer `TotalPercent` / `Application.MatchPercent` / snapshot unchanged.
- Why line + 4 DNA bars from `FitDimensions` (one source).
- Dislikes (D8): `IKbDislikeSource` + `KbRanking.DislikePenalty` (15). **Dep D ABSENT** → `KbNoDislikeSource` returns none (`// KB-FALLBACK(D)`).
- **Dep B′** `ICompanyCultureLookup` ABSENT — calibration anchors derived without employer culture lookup; re-run `CandidateFitDistributionReportTests` when werkgever-aanmelding 01 lands.

## Banenkaart (file 03)

- Map start: `KbMapStart.Resolve` — URL/state → session (`jobsy.kb.origin`) → profile home (Fiets · 20) → stored → location prompt
- Address field: `KbAddressField` (bind oninput; focused value wins over stale suggestions)
- Geocoder: `PdokGeocodingClient` first, Nominatim `layer=address` fallback via `CompositeGeocodingClient`
- Filter badge: `KbFilterDefaults` / `KbFilterBadge` — fresh page is 0 (D13)
- Split view from **900 px**; docked popup for pin + cluster; stronger rings + `data-iso-mode`; Open Sans → Noto Sans glyph rewrite
- `KbRoutes.Map = "/"` (KB-FALLBACK(E))

## Employer attributes + uitzendbureau hidden mode (file 05)

- **Dep B ABSENT** (all three: `CompanyValuesProfile`, `EngagementCatalog`, `Company.WorkTypeLabels`): **do not render** branche pills, kernwaarden tiles, or engagement badges/tiles. No placeholders. Cards keep fit + "Staat lager" via `KbBadgeRow` (D7).
- **Dep A ABSENT:** `KbHiddenIntermediaryMask` (`// KB-FALLBACK(A): superseded by intermediair 03`).
  - Hidden when `IntermediaryCompanyId != null && !ShowClientAddressOnMap`.
  - Pin / mini map / travel → bureau organisation location (root parent when loaded); no pin when bureau has no coords ("Reistijd onbekend").
  - Label: `Kb.Via.Bureau` ("via uitzendbureau {bureau}"); travel: `Kb.Travel.ToBureauSimple`.
  - No Route / Street View; KvK/vestiging of the end client redacted on discovery/detail DTOs.
  - Reveal: none in this fallback (intermediair D4 absent).
  - `ResolvePublicDisplay` pins the bureau (D3); intermediair 03 must delete the fallback.

## List + vacancy detail (file 06)

- Desktop ≥900: Kaart/Lijst radio toggle (`?weergave=lijst` + `sessionStorage jobsy.kb.weergave`). List mode hides the map canvas, full-width `KbListRow` + right rail (≥1024: mini map + how we sort).
- Mobile list: compact `KbListRow` + floating **Kaart** FAB above bottom nav (`ToggleMobileView`).
- Vacancy detail: fit panel, key facts grid, travel card (transport switch Fiets/Auto/Lopen/OV with “ongeveer” for OV), one `.btn--primary` Solliciteer (rail ≥1024 / sticky apply bar &lt;1024). Dep B blocks still absent. Route/Street View secondary links only when not hidden mode.


## Sollicitaties + Bewaard (file 07)

- `ApplicationStatusHistory` + migration `AddApplicationStatusHistory`; single writer `IApplicationStatusRecorder` / `ApplicationStatusTransitions`.
- Candidate timeline via `ApplicationTimelineBuilder` (D4: no invented history for older apps).
- "Gezien door werkgever" = first `EmployerViewed` (detail open / CV download / react / contact); list does not count; support access never records.
- **Dep C ABSENT:** `/candidate/liked` redesigned in place (`h1` Bewaard), **no** `CandidateJobListTabs`, **no** nav change.
- Bewaard cards: `KbSavedJobState` (Open / Sluit over n / Fulfilled / Closed), unsave+undo, apply / view / similar.

## Match (file 08)

- Desktop `MatchDeckDialog` + mobile `MatchPage` / `SwipeCard`: calibrated `KbFitPill`, "Waarom jij past" DNA rows (`MatchDnaRows`), Hierna with fit + travel, keyboard hints (← → Enter Esc).
- D12: `MatchDeck.Defer` moves a skip to the end of the session once; second skip advances. Toast `Kb.Match.Skipped`. Never hides; map/list/top-match unaffected.
- Gate closed → `MatchUnlockPanel` (Maak je paspoort af). Deck done → Bewaard + kaart links.
- Hidden-mode company line via `Kb.Via.Bureau`. "Staat lager" chip when ranked lower.

## E2E + stack report (file 09)

### How to run Playwright (kandidaat banen)

```bash
export JOBSY_E2E_BASE_URL=https://localhost:7xxx   # or Acc URL
export JOBSY_E2E_CANDIDATE_EMAIL=kandidaat@jobsy.local
export JOBSY_E2E_CANDIDATE_PASSWORD=Jobsy123!
export JOBSY_E2E_INCOMPLETE_EMAIL=valentine@jobsy.local   # optional; gate closed
# Optional local-only Werkgevers OFF (never on Acc):
# export JOBSY_E2E_ALLOW_FEATURE_TOGGLE=1

dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter "FullyQualifiedName~KandidaatBanenE2ePlaywrightTests"
```

Without `JOBSY_E2E_BASE_URL` the suite **soft-skips** (returns green). Screenshots land in `artifacts/e2e/kandidaat-banen/` (gitignored).

Shared helper: `Jobsy.Tests/E2e/KbE2e.cs` (login, viewports 1440×900 / 390×844, pageerror/console, circuit check, `E2eRoutes.Banenkaart` → `KbRoutes.Map`).

Scenarios S1–S13 cover banenkaart, anonymous address, Valentine gate, list mode, detail, hidden mode, sollicitaties, bewaard, Match desktop/mobile, Werkgevers OFF (local+Dep C only), and pagehide/h1 focus.

### Fit rules (§F, short)

- Gate open = culture **or** values test done → show calibrated % (55–90, strong ≥ 75) + why line + DNA bars.
- Gate closed / anonymous → "Maak je paspoort af", no number.
- Dislikes: sort penalty 15, never hide, "Staat lager: …" (Dep D; currently fallback returns none).
- Employer `Application.MatchPercent` / snapshot unchanged.

### Timeline rules (07)

- New `ApplicationStatusHistory` rows from now on; no invented backfill (D4).
- Legacy apps: Verstuurd (`CreatedAt`) + current status (`RespondedAt` when set).
- "Gezien door werkgever" = first `EmployerViewed` (`POST api/applications/{id}/viewed`).

### Hidden-mode rule (05 / Dep A fallback)

- When `IntermediaryCompanyId` set and `ShowClientAddressOnMap == false`: pin/travel = bureau; label "via uitzendbureau {bureau}"; no Route/Street View; no client coords in DTOs. No reveal until intermediair 03.

### Nav check (Dependencies F) — report only, **nav unchanged**

| Expected (Dennis) | Actual on this branch |
|---|---|
| De ontdekkingsreis · Mijn Paspoort · Carrière · Banenkaart · Sollicitaties | **Zoeken · Bewaard · Sollicitaties · Carrière · Profiel** |

Bewaard remains a nav item (paspoort D8 / CandidateJobListTabs ABSENT). Map item is Zoeken → `/`. This stack did not change `RoleNavCatalog`.

### Stack-end table (09.4)

| File | Branch | PR | Status | Notes |
|---|---|---|---|---|
| 01 hotfix | `cursor/kandidaat-banen-hotfix` | #443 | green | Isochrone decimals; TopMatchLeadingFragment attribute order; pagehide shim |
| 02 fundament | `cursor/kandidaat-banen-2` | #444 | green | Dep A–G cases + fallbacks, Kb strings/labels/CSS |
| 03 banenkaart | `cursor/kandidaat-banen-3` | #445 | green | Home+20 fiets, chips, rings, docked popup |
| 04 fit/dislikes | `cursor/kandidaat-banen-4` | #446 | green | Gate + calibration; Dep D fallback |
| 05 werkgever/uitzend | `cursor/kandidaat-banen-5` | #447 | green | Hidden mode fallback A; Dep B blocks hidden |
| 06 lijst/vacature | `cursor/kandidaat-banen-6` | #448 | green | `?weergave=lijst`, detail fit/travel |
| 07 sollicitaties | `cursor/kandidaat-banen-7` | #449 | green | Status history + timeline + Bewaard redesign |
| 08 Match | `cursor/kandidaat-banen-8` | *(expected #450)* | green | DNA why-rows, Hierna, D12 skip-to-end |
| 09 E2E + report | `cursor/kandidaat-banen-9` | *(parent opens; this agent does not open PR)* | green (soft-skip without E2E URL) | Playwright S1–S13 + docs |

### Dependency cases (final)

| Dep | Outcome | Fallback |
|---|---|---|
| A | ABSENT | `KbHiddenIntermediaryMask` (`KB-FALLBACK(A)`) |
| B / B′ | ABSENT | blocks not rendered; calibration without `ICompanyCultureLookup` |
| C | ABSENT | `KB-FALLBACK(C)` comments; no tabs; S12 soft-skips |
| D | ABSENT | `KbNoDislikeSource` (`KB-FALLBACK(D)`) |
| E | ABSENT | `KbRoutes.Map = "/"` (`KB-FALLBACK(E)`) |
| F | ABSENT (order differs) | **nav not changed**; report above |
| G | Hotfix 01 not merged on acceptatie | stack branched from hotfix |

`git grep KB-FALLBACK` hits: Core mask/dislike, Web `KbRoutes`/`Program`, Api Vacancies/Applications, Infrastructure DI/discovery, tests, this doc.

### Fit distribution (04.4)

- After calibration: gate-open scores in **55–90**; strong ≥ 75 share within harness bounds; DNA order ≠ nearest-travel.
- Before (raw employer %): unchanged for employers; candidate UI no longer shows ungated raw %.
- Werkgever-aanmelding 01 (`ICompanyCultureLookup`): **ABSENT** when anchors were derived.

### Crash root cause (01)

`TopMatchLeadingFragment` called `AddComponentReferenceCapture` before `AddAttribute` → `InvalidOperationException` → circuit “Even iets misgegaan” for complete-profile desktop candidates after match deck load.

### Out of scope / deferred

- Interview times/places on timeline (mockup only; not stored).
- Server-side persisted Match skip (D12 session-only).
- Reveal of opdrachtgever when intermediair 03 absent (none).
- Dep B kernwaarden/branche/engagement tiles until werkgever-aanmelding 08/09.
- Nav order add-on (Dependencies F) — separate stack.
- Landing 04 `/banenkaart`, paspoort feature gate, paspoort 06 dislikes — fallbacks in place.
- S7 may soft-skip without a seeded hidden-mode vacancy; S12 skips without Dep C + local toggle.
