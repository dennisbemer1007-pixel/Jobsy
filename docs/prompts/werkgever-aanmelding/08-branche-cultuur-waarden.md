# 08. Over je bedrijf, part 1–2: branches, "Zo werken wij" sliders and 3 kernwaarden (+ vacancy inherit/override)

Read `00-README.md` first. Branch `cursor/werkgever-aanmelding-8` from `cursor/werkgever-aanmelding-7` (or `-7b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/werkgever-aanmelding-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Don't change the candidate Cultuurscan or Waardentest (questions, scoring, reports). Only the employer side and how the match reads it.

| | |
|---|---|
| Branch | `cursor/werkgever-aanmelding-8` |
| PR title | `feat(companies): company branches (max 4, SBI prefill), quick culture sliders and 3 kernwaarden feeding the match; vacancies inherit with a per-team override` |
| PR body starts with | `Stacked on #<PR 07> (cursor/werkgever-aanmelding-7)` |
| Mockups | `wr-d5` (branche), `wr-d6` (Zo werken wij + kernwaarden), `wr-m3`, `wr-m4`, `wr-d12` (vacancy editor section) |
| Split seam | **08a** = branches + sliders + the step-4 shell (08.2–08.4). **08b** = kernwaarden + match + vacancy override (08.5–08.7) |

## Goal
In about a minute (optional, skippable), a new employer tells candidates what kind of work they offer and how it feels to work there, in the **same language** as the candidate tests, so the match actually uses it.

## 08.1 Today (verify first)
- `WorkTypeLabels` (9 labels, `MaxPerVacancy = 2`, `CombineStored`, `ResolveLabels`), `Vacancy.WorkTypes` + `Vacancy.WorkTypeLabels`. **Company has no branches.** `SbiWorkTypeMap` (04).
- `CompanyCultureProfile` (in `Entities/CandidateCulturePersonalityProfile.cs`): `AnswersJson`, `Status`, 6 culture percents (`AutonomyPercent` … `PeopleFirstPercent`) + optional personality percents. The employer scan at `/employer/culture` uses `CulturePersonalityCatalog` items 1–12: two per culture dimension, the second **reversed** (Q2, Q4 … Q12; Likert 1–5). Labels via `DimensionLabels` (Zelfstandig werken, Informele sfeer, Samenwerken, Flexibel meebewegen, Nieuwe dingen proberen, Mensen voorop).
- 01 made `ICompanyCultureLookup` feed `ProfileVacancyMatchInput.CompanyCultureScores` (vestiging → org fallback, 10-min cache).
- Values: `ProfileVacancyMatchCalculator` (~L51–58) and `BroadMatchRationaleBuilder` (~L104) call `SchwartzValuesFitRules.Fit01(candidate, title, description)`, which uses `InferVacancyDrivers` (keyword guess). Drivers: `SchwartzValuesCatalog` Autonomy, Connection, Achievement, Stability, Impact.
- Per-vacancy pillars: `Vacancy.CulturePillarsJson` (`CulturePillarCatalog`, 3–5), `CreateVacancyRequest.CulturePillars`, `VacanciesController` ~L1060, `CultureFitBuilder.Evaluate(input.CulturePillars, …)` (~L126).

## 08.2 Company branches (D12, wr-d5)
- `Company.WorkTypeLabels` (string, stored like the vacancy's via `CombineStored`) + `WorkTypeLabels.MaxPerCompany = 4` (migration `AddCompanyProfileExtras`, shared with 08.3/08.5).
- Stored on the **root organisation**; a vestiging uses the root's (a vestiging override is out of scope).
- Prefill: on activation (05) set it from `SbiWorkTypeMap.Map(company SBI codes)` if empty, marked "voorgesteld op basis van je KvK-inschrijving" in the UI until the user saves.
- Vacancy editor: a **new** vacancy of this company starts with the company's first branche (still max 2, still required as today). Existing vacancies don't change.

## 08.3 "Zo werken wij" sliders (D11, wr-d6 left)
- 6 sliders (1–5, labelled ends per dimension, texts `WaProfile.Slider.{Code}.Low|High`), one per `CultureDimensionCodes` entry, in catalog order.
- Save: for dimension k (1–6) with value v, write answers item `2k−1 = v` and item `2k = 6 − v` into `CompanyCultureProfile.AnswersJson`, recompute the percents with the **existing** employer scoring code, set `Status = Completed`, and a new `Source` column (`Quick`|`Full`, default `Full` for existing rows) = `Quick`.
- Doing the full 12-item scan later (`/employer/culture` or the redesign's Cultuur tab) overwrites it and sets `Source = Full`. The full scan page shows "Ingevuld via snelle schuifjes, verfijn met de volledige scan" when `Source = Quick`, and the sliders open prefilled from the answers (v = item 2k−1).
- Evict the 01 cache on save.

## 08.4 Step 4 shell (`/register/bedrijf`, wr-d5/d6, wr-m3/m4)
- `/register/bedrijf?stap=branche|cultuur|betrokkenheid` (public theme, noindex, signed-in bedrijfsmanager; §IA). A 3-step mini stepper "Branche · Zo werken wij · Betrokkenheid" ("≈ 3 minuten, alles optioneel") with **Overslaan** on every step and **Opslaan en verder**. `stap=betrokkenheid` shows a placeholder "Volgt" until 09 lands.
- After the last step (or Overslaan): to `/register/verifieren` (06) if unverified, else the dashboard.
- API: `GET/PUT api/companies/{id}/profile-extras` `{ workTypeLabels[], cultureSliders{code: 1–5}?, valueCardIds[]? }` (company managers; allowed while unverified per 03). Validation: max 4 branches from `WorkTypeLabels.All`, slider values 1–5, exactly 3 cards or none.
- The same three editors appear in the employer app's organisation profile (dependency C: `/werkgever/organisatie/profiel` tabs; else today's company settings page) as app-design-system components sharing one `CompanyProfileExtrasClient`.

## 08.5 Kernwaarden (D11, wr-d6 right)
- 10 cards, 2 per driver (`Jobsy.Core/Rules/CompanyValueCards.cs`, ids stable):

  | Driver | Card A | Card B |
  |---|---|---|
  | Autonomy (Eigen regie & uitdaging) | `vrijheid` "Vrijheid in hoe je je werk doet" | `uitdaging` "Elke dag iets nieuws leren" |
  | Connection (Verbinding & zorg) | `teamgevoel` "Een hecht team" | `zorg` "We zorgen voor elkaar en onze klanten" |
  | Achievement (Prestatie & groei) | `groei` "Doorgroeien en beter worden" | `resultaat` "Samen resultaat halen" |
  | Stability (Zekerheid & traditie) | `zekerheid` "Vaste afspraken en zekerheid" | `vakmanschap` "Degelijk vakmanschap" |
  | Impact (Impact & rechtvaardigheid) | `betekenis` "Werk dat ertoe doet" | `eerlijk` "Eerlijk en duurzaam ondernemen" |

  Texts in `WaProfile.Card.*` (5 languages). The user picks **exactly 3**.
- New entity `CompanyValuesProfile` (company id, `CardIdsJson`, 5 driver percents, updated at) on the root organisation (vestiging → org fallback). Scores (D11): a driver with 2 chosen cards = 90, 1 card = 75, 0 cards = 40.

## 08.6 Values in the match
- Extend `ICompanyCultureLookup` (01) to return `(CulturePersonalityScores? Culture, SchwartzValuesScores? Values)` per company in the same batched, cached call.
- `ProfileVacancyMatchInput.CompanyValuesScores`. In `ProfileVacancyMatchCalculator` and `BroadMatchRationaleBuilder`: `Fit01(candidate, input.CompanyValuesScores ?? InferVacancyDrivers(title, description))`, so the keyword guess stays the fallback. The rationale line mentions the kernwaarden when used ("Past bij wat {bedrijf} belangrijk vindt: Een hecht team").
- Bump the match snapshot version (as 01 did) so cached match scores refresh.

## 08.7 Vacancy editor: inherit or override (wr-d12)
- New "Cultuur van dit team" section in the vacancy editor (`CreateVacancy.razor`): default **"Zoals bij {bedrijf}"** (read-only summary of the 6 sliders + 3 kernwaarden, with an "Aanpassen" link to the organisation profile) and a toggle **"Dit team werkt anders"** that reveals the existing `CulturePillarCatalog` picker (3–5 pillars).
- Rule (no new column): a vacancy **inherits** when `CulturePillarsJson` is empty; it **overrides** when pillars are set. Existing vacancies with pillars are therefore overrides (unchanged). Switching the toggle off clears the pillars.
- Match: an overriding vacancy uses its pillars (`CultureFitBuilder`, as today) and does **not** use the company culture scores (`CompanyCultureScores = null` for that vacancy); the kernwaarden (values) always come from the company.
- When the company has no culture profile, the section says "Nog geen cultuurprofiel. Vul 'Zo werken wij' in (1 minuut)" with a link.

## Tests
- `CompanyWorkTypesTests`: max 4, only known labels, SBI prefill on activation, a new vacancy defaults to the company's first branche, existing vacancies untouched.
- `QuickCultureSlidersTests`: v → answers (2k−1 = v, 2k = 6 − v) → percents equal the full-scan scoring of the same answers; `Source` Quick → Full after a full scan; the cache is evicted.
- `CompanyValueCardsTests`: exactly 3 cards; 2 cards on one driver → 90, 1 → 75, 0 → 40; vestiging falls back to the org.
- Match: with company values → `Fit01` gets them (not the inferred ones); without → the inferred fallback; an overriding vacancy ignores the company culture but keeps the values; snapshot version bumped. A before/after score fixture shows the expected change for one candidate.
- API validation + the step-4 flow (Playwright: branche → sliders → cards → verifieren; Overslaan on each step).

## Success criteria
- A company that filled in the sliders and 3 cards gets a different, explainable match for a candidate with a matching Cultuurscan/Waardentest than one that didn't. A vacancy with "Dit team werkt anders" uses its own pillars.

Done → next: `09-maatschappelijke-betrokkenheid.md`.
