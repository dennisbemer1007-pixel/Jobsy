# 05. Employer kernwaarden / branche / engagement on cards + detail, uitzendbureau hidden mode

Read `00-README.md` first (§0, §F, D3, D7, Dependencies A/B). Branch `cursor/kandidaat-banen-5` from `cursor/kandidaat-banen-4`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/kandidaat-banen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - **Display only.** No new employer entry, no new moderation, no new labels for things werkgever-aanmelding 08/09 or intermediair 03 already name. In hidden mode, **no** opdrachtgever identity or coordinates reach the client.

| | |
|---|---|
| Branch | `cursor/kandidaat-banen-5` |
| PR title | `feat(vacatures): show employer values, branche and engagement (from werkgever profile), honest uitzendbureau hidden mode (bureau pin, no route)` |
| PR body starts with | `Stacked on #<PR 04> (cursor/kandidaat-banen-4)` + the Dependencies A and B cases (which blocks are shown/hidden, which hidden-mode path) |
| Mockups | `kd-d3-vacature.png` (kernwaarden tiles, branche pill, engagement tiles), `kd-m3-vacature.png` (hidden mode: "via uitzendbureau FlexPlus", pin on the bureau vestiging, no Route/Street View), `kd-d2` (card chips) |
| Split seam | **05a** = 05.2–05.3 (employer attributes). **05b** = 05.4–05.6 (hidden mode) |

## Goal
Candidates see what an employer stands for (3 kernwaarden, branche, engagement) with honest "who says so" labels, using exactly what employers entered in their profile. A vacancy offered by an uitzendbureau in hidden mode never gives away where the real workplace is: the pin, mini map and travel time point at the bureau's vestiging, and there is no route to the workplace.

## 05.1 Today (verify first)
- **Employer attributes:** none of `CompanyValuesProfile`, `EngagementCatalog` / `CompanyEngagementClaim`, or `Company.WorkTypeLabels` exist on `a611db40`. They come from werkgever-aanmelding 08/09 (Dependencies B).
- **Intermediary:**
  - `Vacancy.CompanyId` = the end client; `Vacancy.IntermediaryCompanyId` = the bureau; `Vacancy.ShowClientAddressOnMap` (false = hidden mode).
  - `Jobsy.Core/Rules/IntermediaryVacancyRules.ResolvePublicDisplay` (~L43–82) returns the bureau's name/address/logo in hidden mode, **but the pin uses the vacancy workplace coordinates** ("Pin always follows the vacancy workplace"). It also builds the hardcoded "Aangeboden door {bureau}" label.
  - Callers: `VacanciesController` ~L2069, `VacancyDiscoveryIndex` ~L145, `VacancyProductService` ~L1024, `VacancyTextSearch` ~L23, `CandidateApplicationLocation` ~L35, `LobsyCvModelFactory` ~L216.
  - Test: `IntermediaryVacancyRulesTests.ResolvePublicDisplay_masked_uses_vacancy_coords_over_intermediary_hq`.
- **Detail:** `VacancyDetail.razor` ~L243–247 shows "Route" (`OpenRouteAsync`) and "Street View" (`OpenStreetViewAsync` ~L1784) for every vacancy.
- **Travel:** travel minutes and the ring filter use the vacancy coordinates, so in hidden mode a candidate can triangulate the workplace.

## 05.2 Employer attributes (Dependencies B present)
Use werkgever-aanmelding's own types, keys and rules. For each part that is present:
- **Branche:** `Company.WorkTypeLabels` (max 4) → detail: a pill row "Branche: …" under the company line (`kd-d3`). Card: not shown (D7 badge budget), except as a list filter already existing.
- **Kernwaarden:**
  - `CompanyValuesProfile` of the **root organisation** (3 of the 10 `CompanyValueCards`) → detail: a block "Waar {bedrijf} voor staat" with 3 tiles (icon + title + short text from `WaProfile.Card.*`).
  - Where the candidate's values test overlaps a card, the tile gets a small "Past bij jou" marker, computed from the existing values fit only (no new scoring). The marker is shown only when the fit gate is open (04).
- **Engagement:**
  - The non-`Removed` `CompanyEngagementClaim`s (the 6 `EngagementCatalog` items) with that stack's honest labels: "Door werkgever opgegeven" (SelfDeclared), "Gecontroleerd door Lobsy" / "Gecontroleerd bij SBB" (Checked).
  - Detail: all of them, as tiles.
  - Card: through `KbBadgeRow` with the D7 priority ("Staat lager" > checked > self-declared), max 2 including the fit pill, then "+n".
- **Data path:** extend the candidate discovery/detail DTOs from the sources werkgever-aanmelding 09 names (`VacancyDiscoveryRecord.EngagementItems`, the extended `ICompanyCultureLookup` / profile lookups). Read them in the same query, so there is no N+1 (assert the query count in a test).
- **Hidden mode** (05.4): the end client's branche, kernwaarden and engagement are **not** shown (they could identify the client). None of those blocks render.

## 05.3 Employer attributes (Dependencies B absent)
- Blocks that have no source are **not rendered**: no placeholder, no "binnenkort", no mock data. The card badge row still works with fit + "Staat lager".
- Say in the PR which of the three were absent.

## 05.4 Hidden mode, intermediair 03 present (Dependencies A)
- Use `IntermediaryPublicIdentity` for the name, logo, pin coordinates, label and (in the candidate's own application only) the reveal. Use `IntermediaryBadge` for the pill. Use intermediair D5's travel label: "Reistijd tot de vestiging van het bureau · werklocatie in de regio {gemeente}".
- This stack only makes sure its **new** surfaces (card parts, docked popup, side list, detail travel card from 06, Match from 08, saved/applications from 07) go through that resolver. There is no parallel logic. Add a test per surface that a hidden vacancy renders the bureau identity.

## 05.5 Hidden mode, intermediair 03 absent (fallback, Dependencies A)
- `Jobsy.Core/Rules/KbHiddenIntermediaryMask.cs` (pure), marked `// KB-FALLBACK(A): superseded by intermediair 03`, used by the candidate-facing DTO builders only. A vacancy is hidden when `IntermediaryCompanyId != null && !ShowClientAddressOnMap`. For those:
  - **Pin / mini map / travel origin-destination / ring filter:** the **bureau organisation's** `Location` (root org when the bureau has vestigingen). If the bureau has no location, the vacancy gets no pin and no travel time (it stays in the list, with the travel shown as "Reistijd onbekend").
  - **Name/logo:** the bureau. **Label:** "via uitzendbureau {bureau}" (`Kb.Via.Bureau`), replacing the hardcoded "Aangeboden door …".
  - **Hidden fields:** the end client's name, address, gemeente, KvK, logo, photos that are the client's, branche/values/engagement. Route and Street View buttons are not rendered.
  - **Travel label:** "Reistijd tot het bureau".
  - **Reveal:** none in this fallback (D3). The candidate sees the client only through the bureau, outside the platform.
- Change `ResolvePublicDisplay` so the masked case returns the **bureau's** coordinates (the one leak). Update `ResolvePublicDisplay_masked_uses_vacancy_coords_over_intermediary_hq` to the new expectation, with a comment pointing at D3. The admin/travel/SROI uses of `Vacancy.CompanyId` stay (they don't go through the public display).
- Employer and bureau views are unchanged.

## 05.6 Hidden-mode detail + tests (both cases)
- **Detail** (`kd-m3`):
  - no Route / Street View
  - the mini map shows the bureau pin with a small note "Kaart toont de vestiging van het bureau"
  - the company block shows the bureau with "via uitzendbureau"
  - the sticky apply bar is unchanged
- **Privacy test (API level):** for a seeded hidden vacancy, none of the following contain the end client's name, KvK, address, gemeente-level address or workplace coordinates (±0.001°), a Route URL or a Street View URL:
  - the candidate list DTO
  - the detail DTO
  - the map proxy payload
  - the docked popup data
  - the Match deck item
  - the saved-job item
  - OG/SEO meta
- bUnit: detail in hidden mode has no Route/Street View buttons; the card shows "via uitzendbureau {bureau}".

## Tests
- 05.2/05.3 bUnit (present/absent per block), the query-count test, the badge priority
- 05.4 or 05.5 resolver tests + the privacy test (05.6)
- `IntermediaryVacancyRulesTests` updated (fallback case only), `dotnet build`, `dotnet test` green

## Success criteria
- With werkgever-aanmelding 08/09 present, the detail shows the branche, 3 kernwaarden and engagement with honest labels; cards show at most 2 badges + "+n". Absent → those blocks don't render.
- A hidden-mode vacancy's pin, mini map and travel time use the bureau's vestiging; there is no Route/Street View; no candidate payload contains the client's identity or coordinates.
- The reveal follows intermediair D4 (present) or doesn't exist (fallback).

Done → next: `06-lijst-vacature.md`.
