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
