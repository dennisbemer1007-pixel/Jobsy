> **Warnings:** deliver a Release build with 0 new warnings vs the base branch (0 warnings after code-health step 11). Run `dotnet format` and `.github/scripts/count-build-warnings.sh` before opening the PR. No blanket `<NoWarn>`.

# Cursor prompt: Candidate onboarding wizard v2 (`/candidate/start`)

## 0. Ground rules (read first)

- **Base branch:** start from the latest `acceptatie` (`git fetch origin && git checkout -b cursor/onboarding-wizard-v2 origin/acceptatie`).
  - Check the release flow before you open the PR. `render.yaml` deploys `acceptatie` to the acceptance services and `main` to production.
  - At the time of writing, `acceptatie` was 29 commits ahead of `main`. So **target `acceptatie`**.
  - Only if the `acceptatie` branch or its Render services no longer exist, target `main` instead, and say so in the PR description.
- **Exactly ONE pull request.** Do **not** merge it. Do **not** deploy. Do **not** use or trigger shortcut/rule **`123`** (or `456`). Do not push to `acceptatie` or `main` directly.
- **Reference images:** these live on branch `docs/wizard-v2-mockups` at `docs/mockups/wizard-v2/`. Read them from there before you start, e.g. `git fetch origin docs/wizard-v2-mockups && git checkout origin/docs/wizard-v2-mockups -- docs/mockups/wizard-v2`, and include that folder (unchanged) in your PR. Do not generate, edit or download other images. If the branch or folder is missing, stop and report it in the PR description. Expected files:
  - Mobile (390×844): `w2-00-welkom.png`, `w2-01-persoon.png`, `w2-02-beschikbaarheid.png`, `w2-02b-fijnafstemmen.png`, `w2-03-reizen.png`, `w2-04-werkervaring.png`, `w2-05-opleiding.png`, `w2-06-droombaan.png`, `w2-06b-droombaan-zoeken.png`, `w2-07-tests-intro.png`, `w2-08-minitest.png`, `w2-10-klaar.png`
  - Desktop (1280×800): `w2-d1-droombaan-desktop.png`, `w2-d2-beschikbaarheid-desktop.png`
  - `overview.png`
- **Follow `.cursor/rules` and the Lobsy design system** (`design-system.mdc`), summarised below.
  - All UI text goes through `@Culture["Onboarding.*"]` in `Localization/UiStrings*.cs`, for nl, en, pl, ro and ar. Dutch copy is given below; translate the other languages sensibly. No hardcoded Dutch in markup.
- **Redesign only this route** (`/candidate/start`) plus what it needs: catalog, rules, DTO, migration and tests. Don't touch unrelated screens.

## 1. Current implementation (acceptatie), which you will refactor

| Area | File |
|---|---|
| Wizard page (1 494 lines, steps 1–10 + Install phase) | `Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor` (`@page "/candidate/start"`) |
| Step catalog, mini-test IDs, education chips, dream chips | `Jobsy.Core/Rules/OnboardingWizardCatalog.cs` (`StepCount = 10`, `RemainingMinutesByStep`, `CompetencyQuestionIds`, `CareerQuestionIds` (6 items, RIASEC), `CultureQuestionIds`, `ValuesQuestionIds`, `EducationChips`, **`PopularDreamJobChips`**, **`DreamJobChipsByRiasecR/I/A/S/E/C`**, **`DreamChipsForRiasec`**, `FormatEducationLine`) |
| Impression texts | `Jobsy.Core/Rules/OnboardingImpressionLibrary.cs` |
| Step analytics | `Jobsy.Core/Rules/OnboardingStepAnalytics.cs` |
| Entity | `Jobsy.Core/Entities/CandidateOnboarding.cs` (`CurrentStep` 1–10, `StepsJson`) |
| Service / interface / DTOs | `Jobsy.Infrastructure/Services/CandidateOnboardingService.cs`, `Jobsy.Core/Interfaces/ICandidateOnboardingService.cs` (`CandidateOnboardingImpressionDto.DreamJobSuggestions`, `MatchCards`) |
| API | `Jobsy.Api/Controllers/CandidateOnboardingController.cs` (`GET/PUT api/me/onboarding`, `POST complete`, `PUT dream-job` → `ICandidateCareerPlanService.SaveDreamAsync`) |
| Web models | `Jobsy.Web/Models/OnboardingModels.cs`, `Jobsy.Web/Models/VacancyListItem.cs` (`CandidatePreferences`) |
| Resume card on home | `Jobsy.Web/Components/Shared/CandidateOnboardingResumeCard.razor` |
| Day/day-part codes | `Jobsy.Core/Rules/DayPartMatrix.cs` (`DayCodes` Ma…Zo, `DayPartCodes` Ochtend/Middag/Avond/Nacht) |
| Likert UI | `LikertScaleQuestion` component (used by the wizard; a test asserts it stays) |
| PWA install | `window.lobsyPwaInstall` in `wwwroot/js/app-core.js` (keep) |
| Styles | `.ob-*` rules in `wwwroot/css/app.css` from about line 22 506, mirrored in `app.min.css`; version tag in `Components/App.razor` (`?v=…`) |
| Existing tests | `Jobsy.Tests/CandidateOnboardingWizardTests.cs` (asserts on catalog, `LikertScaleQuestion`, `SaveOnboardingDreamJobAsync`, `CompleteMyOnboardingAsync`, `lobsyPwaInstall`, `Onboarding.Later`, resume card, migration names); Playwright pattern in `Jobsy.Tests/MobileSmokePlaywrightTests.cs` and `CandidateTabStabilityPlaywrightTests.cs` (seeded `kandidaat@jobsy.local` / `Jobsy123!`, soft-skip without `JOBSY_E2E_BASE_URL`) |

Keep the existing save paths:
- Profile: `Api.UpdateMyProfileAsync` (first/last name, date of birth, phone, home lat/lng, `availableFromDate`/`clearAvailableFromDate`, `CandidatePreferences`).
- Mini-tests: `SaveMyCompetenciesAsync` / `SaveMyCareerInterestsAsync` / `SaveMyCultureAsync` / `SaveMyValuesAsync` with `complete: false` and merged answers.
- Consent: `AcceptTestAiConsentAsync`.
- Dream job: `SaveOnboardingDreamJobAsync`.
- Progress: `SaveMyOnboardingProgressAsync`. Finish: `CompleteMyOnboardingAsync`.
- Address: `IGeocodingClient.SuggestAsync`.

## 2. Step structure and counting (decision)

There are **10 counted steps** plus 3 uncounted screens:

| # | Phase | Screen | Mockup |
|---|---|---|---|
| – | – | Welcome (only on a fresh start) | w2-00 |
| 1 | Over jou | Persoonsgegevens | w2-01 |
| 2 | Over jou | Beschikbaarheid (presets + fine-tune) | w2-02, w2-02b |
| 3 | Over jou | **Reizen** (new: split from old step 2) | w2-03 |
| 4 | Achtergrond | Werkervaring (optional) | w2-04 |
| 5 | Achtergrond | Opleiding (optional) | w2-05 |
| 6 | Droom | Droombaan | w2-06, w2-06b |
| 7 | Wie ben jij | Competentie mini-test. **The test intro + consent (w2-07) is merged into step 7** as its first sub-view. It stays "stap 7 van 10" and is shown only while consent is missing or no answer exists yet | w2-07, w2-08 |
| 8 | Wie ben jij | Beroepen mini-test (keep the 6 RIASEC questions) | like w2-08 |
| 9 | Wie ben jij | Cultuur mini-test | like w2-08 |
| 10 | Wie ben jij | Waarden mini-test | like w2-08 |
| – | – | Finish "Je Kompas staat klaar" (replaces the old step-10 result) | w2-10 |
| – | – | Existing Install/push screen (**keep as is**, after the finish screen) | – |

- Old step 10 (result) becomes the uncounted finish screen. `StepCount` stays 10 and `RemainingMinutesByStep` stays `6,5,4,4,3,3,2,2,1,1`.
- **Migrating in-progress users:**
  - Add `int WizardVersion` to `CandidateOnboarding`. The migration sets default `1` for existing rows; new rows get `2`.
  - When a v1 row is read with `CompletedAtUtc == null`, map `CurrentStep` as follows, then set `WizardVersion = 2`:
    - old 1 → 1, old 2 → 2, old 3 → 4, old 4 → 5, old 5 → 6
    - old 6 → 7, old 7 → 8, old 8 → 9, old 9 → 10
    - old 10 → finish screen (store 10 plus a flag, or set a `ResultReached` step event)
  - Use a pure static helper `OnboardingWizardCatalog.MapV1Step(int)` with unit tests. Leave `StepsJson` history untouched, but new events are v2 numbering.
- **Phases** (for the progress bar), in `OnboardingWizardCatalog`:
  - `Over jou` = 1–3
  - `Achtergrond` = 4–5
  - `Droom` = 6
  - `Wie ben jij` = 7–10

## 3. Global layout (mobile first)

Use design-system tokens only (`--bg #f5f2ee`, `--surface #fffcfa`, `--text #122033`, `--muted #5a6a7d`, `--border #ddd5cc`, `--brand #0f2d5c`, `--accent-soft #e7eef7`, `--success #15803d`/`--success-soft`, `--coral` not used in this flow).
- Font: `var(--font)`. Weights: 400/600, and 700 only for the page `h1`.
- Type scale: `--text-xs … --text-2xl`. Radius: `--radius-sm` for inputs and buttons, `--radius` for cards and tiles, `--radius-pill` for chips.
- Shadows: `--shadow` for cards, `--shadow-lg` only for the suggestion dropdown.
- Don't add new hex values. If `--pearl-mid #efe9e3` exists, use it for tracks; otherwise use `--border` at low opacity via an existing token.

**New styles** go in a new file `Jobsy.Web/wwwroot/css/features/onboarding-wizard.css`, linked in `Components/App.razor` after `app.min.css`.
- Remove the obsolete `.ob-*` rules from **both** `app.css` and `app.min.css` in sync, and bump the `?v=` tag.
- Use BEM under `.ob-wizard`. No inline `style=""` except CSS custom properties for progress widths (`style="--w:40%"`).
- Use logical properties (RTL).

**Header (every counted step)**, sticky at the top, background `--bg`:
1. **Row 1 (52px):**
   - Start: back `icon-btn` (44×44, chevron, `aria-label="Vorige stap"`, disabled on step 1).
   - Centre: **4 phase segments** (flex 1 each, 6px high, gap 4px, track colour pearl, fill `--brand`). A completed phase is 100% filled; the current phase is filled by (index in phase + 1)/steps in phase; future phases are 0%.
   - End: text button "Later verder" (600, `--brand`, 44px high).
   - The whole segment group has `role="progressbar"`, `aria-valuemin=1`, `aria-valuemax=10`, `aria-valuenow=step` and `aria-label="Stap x van 10"`.
2. **Row 2 (20px, `--text-xs`, `--muted`):**
   - Start: "**{Phase}** · stap {x} van 10 · nog ± {min} min" (phase name in 600 `--brand`).
   - End: save status with `aria-live="polite"`:
     - "✓ Bewaard" (`--success`, 600) for 2.5s after each successful save
     - "Bezig met bewaren…" (`--muted`) while saving
     - "Niet bewaard · Opnieuw" (button, `--danger`) on failure
3. **Content** (padding 16px 24px, bottom padding clears the footer):
   - Optional `Optioneel` pill (xs, 600, muted on pearl), then `h1` (`--text-2xl`, 700, `--brand`, line-height 1.2), then the lead (`--text-md`, `--muted`), then controls.

**Footer**, sticky at the bottom, with a gradient fade from transparent to `--bg`, padding 12px 24px + `env(safe-area-inset-bottom)`:
- Primary button: full width, 52px high, `--brand`, white, 600, radius-sm. Label "Volgende", or a step-specific one.
- Optional ghost link under it (44px): "Overslaan" / "Weet ik nog niet" / "Volledige tests doen voor meer precisie".
- Use `btn-compact btn-compact--primary` styling tokens. Don't invent new `*-btn` classes; if you need a size modifier, add `ob-wizard__cta`.

**Desktop (≥1024px):**
- Two-column grid: `300px | 1fr`, full height.
- **Sidebar** (`--surface`, inline-end border in pearl, padding 28px 24px):
  - Lobsy logo + wordmark.
  - A vertical phase list of 5 items: Over jou (3 stappen), Je achtergrond (optioneel), Je droombaan (1 stap), Wie ben jij (4 korte tests), Jouw Kompas.
  - Each item: 28px circle with the number; done = `--success` fill with a check; current = `--accent-soft` row background, number circle `--brand`, 600.
  - At the bottom: "✓ Alles is bewaard" (`--success`) and the link "Later verder".
- **Main** (padding 28px 56px):
  - Top line: "Stap x van 10", a thin bar (max 420px, 6px), and "nog ± y min".
  - Content `max-width: 760px`, centred.
  - A footer row in the same width: "‹ Terug" link at the start, optional ghost, primary button at the end (auto width, padding 0 28px).
- **Between 640 and 1023px:** the mobile layout, centred, max-width 560px.

## 4. Per-screen specs

### w2-00 Welkom (uncounted; only when there is no progress yet)
- 56px Lobsy logo, `h1` "Hoi {voornaam}, fijn dat je er bent".
- Lead: "In ± 6 minuten maken we samen je startprofiel. Daarna zie je in je Kompas wie je bent en welk werk bij je past."
- 4 phase rows as cards (`--surface`, `--shadow`, 14px 16px). Each row: 36px number circle (`--accent-soft`/`--brand`), title 600 + muted sub, and the time at the end (xs, muted):
  - Over jou · "Waar je woont en wanneer je kunt" · 2 min
  - Je achtergrond · "Werk en opleiding, mag je overslaan" · 1 min
  - Je droombaan · "Waar wil je naartoe?" · ½ min
  - Wie ben jij · "4 korte tests" · 2 min
- Hint: "Alles wordt meteen bewaard. Stoppen mag, je gaat later verder waar je was."
- Primary "Beginnen".

### w2-01 Persoonsgegevens (step 1)
- `h1` "Even kennismaken". Lead "Klopt dit? We hebben je naam overgenomen van {Google|Microsoft}." (use the auth method; generic "van je account" as fallback).
- Voornaam | Achternaam side by side (52px inputs, labels above, `autocomplete` given-name/family-name), prefilled.
- **Postcode** (single field, `inputmode="text"`, `autocomplete="postal-code"`, pin icon):
  - When the input matches `^[1-9][0-9]{3}\s?[A-Za-z]{2}$`, debounce 350ms, call `IGeocodingClient.SuggestAsync(postcode)`, take the first result whose postcode matches, and set city + lat/lng.
  - Show a check at the end of the field and the hint "{Woonplaats} · alleen voor reistijd, werkgevers zien je adres niet".
  - If there's no result: show the inline error "We vinden deze postcode niet" plus a link "Vul je woonplaats in", which reveals the existing city field with the current suggestions list.
  - Normalise the postcode to `1234 AB`. Keep `ComposedHomeAddress()` semantics.
- **Geboortedatum:** 3 numeric inputs, Dag | Maand | Jaar (flex 1/1/1.6, `inputmode="numeric"`, `maxlength` 2/2/4), auto-advance, validated into `DateOnly`.
  - Hint "Zo tonen we alleen banen die bij je leeftijd mogen". One `<fieldset>` with `<legend>`.
- Telefoonnummer, label with "optioneel" at the end (muted), `type="tel"`.
- "Volgende" is enabled when first name, postcode (resolved to lat/lng or a manual city) and a valid date of birth are filled.

### w2-02 Beschikbaarheid (step 2), presets
- `h1` "Wanneer kun je werken?" Lead "Kies wat bij je past, meerdere mag. We vullen de details voor je in."
- 2-column grid (gap 10px) of **preset cards**: `<button aria-pressed>`, min-height 56px, radius, border `--border`, surface.
  - Each card: a 22px checkbox square at the start, title 600 `--text-sm`, sub `--text-xs` muted.
  - Selected: 2px `--brand` border, `--accent-soft` background, brand text, filled check.
  - Order and copy:
    1. Per direct · "Ik kan meteen beginnen"
    2. Bijbaan naast school · "Na school en in het weekend"
    3. Weekenden · "Za en zo"
    4. Avonden · "Ma–vr na 18:00"
    5. Kantoordagen · "Ma–vr, 9–17 uur"
    6. Vakantiewerk · "In de schoolvakanties"
    7. Parttime · "12–32 uur per week"
    8. Fulltime · "36–40 uur per week"
- Below: a summary card "Zo hebben we het ingevuld" with the toggle "Aanpassen ▾" (`aria-expanded`). Rows: Uren per week (e.g. "8 – 16 uur"), Dagen (a compact sentence built from the matrix, e.g. "Ma–vr middag · za/zo"), Beginnen ("Per direct" or a date).
- **If "Per direct" is not selected:** show a "Beschikbaar vanaf" date field under the summary. This maps to `availableFromDate`; "Per direct" maps to `clearAvailableFromDate`.

### w2-02b Fijn afstemmen (step 2, expanded)
- Selected presets are shown as small chips on top (+ meer).
- A card with:
  - **Uren per week**: reuse `HoursRangeSlider` (two thumbs, 0–40, step 1, value label "8 – 16 uur" at the end in 600 brand).
  - **Dagen en dagdelen** matrix: 7 rows (Ma…Zo) × 3 columns (Ochtend/Middag/Avond; don't show Nacht). Cells are toggle buttons ≥44×30px (the visible tile may be 30px high, but the hit area is ≥44px via padding), with `aria-pressed` and `aria-label="{dag} {dagdeel}"`. On = `--brand` fill.
  - Hint "Tik op een vakje om het aan of uit te zetten."
- The primary becomes "Klaar" (collapses the panel); after that it's "Volgende" again.
- **Desktop (w2-d2):** presets grid on the left, and the fine-tune card on the right, always visible (2 columns, gap 24px).

### Availability preset rules (deterministic, combinable)
Implement in a new pure class `Jobsy.Core/Rules/AvailabilityPresetRules.cs`, with unit tests.

| Code | Label | Hours (min–max) | Day-parts | Other |
|---|---|---|---|---|
| `direct` | Per direct | – | – | `availableImmediate = true` (deselecting shows the date field) |
| `school` | Bijbaan naast school | 6–16 | Ma–Vr Middag; Za Ochtend+Middag | |
| `weekend` | Weekenden | 8–16 | Za, Zo Ochtend+Middag | |
| `evening` | Avonden | 8–20 | Ma–Vr Avond | |
| `office` | Kantoordagen (ma–vr) | 32–40 | Ma–Vr Ochtend+Middag | |
| `holiday` | Vakantiewerk | 16–40 | Ma–Vr Ochtend+Middag | stored as a preset code (seasonal intent) |
| `parttime` | Parttime | 12–32 | *only if no day-specific preset is selected:* Ma–Vr Ochtend+Middag | |
| `fulltime` | Fulltime | 36–40 | *only if no day-specific preset is selected:* Ma–Vr Ochtend+Middag | |

- **Combine** (`Compute(IReadOnlySet<string> presets)` → `(decimal? Min, decimal? Max, ISet<string> DayParts, bool Immediate)`):
  - Day-parts are the **union** of the day-parts of the selected presets. Day-specific presets are `school`, `weekend`, `evening`, `office` and `holiday`.
  - Hours: min = **minimum** of the selected presets' mins, max = **maximum** of their maxes, clamped to 1–60. Presets without hours are ignored. If no preset has hours, keep the current values.
  - Immediate = `direct` is selected.
  - Order doesn't matter: the same set always gives the same result.
  - Example: school + weekend + direct → 6–16 h, Ma–Vr Middag + Za/Zo Ochtend+Middag, per direct.
- **Manual override:** as soon as the user changes the slider or the matrix, set `presetsOverridden = true`.
  - Presets then no longer overwrite fields. Show "Je hebt het zelf aangepast · Opnieuw invullen" (link → recompute from presets and clear the flag).
  - Deselecting a preset without an override recomputes from the remaining presets.
- **Persist** the selected preset codes and the override flag in `PreferencesJson` as `availabilityPresets: string[]` and `availabilityPresetsOverridden: bool`.
  - Add them to `CandidatePreferencesDto` (Core contracts) and `CandidatePreferences` (Web model). JSON only, so no DB migration.
  - Restore the chips from these on resume. Existing readers (e.g. `WhoAmICompleteness.TryReadSignals`) must ignore unknown fields; verify this.
- **Validation for "Volgende":** hours min ≤ max and at least 1 day-part. Otherwise show the inline hint "Kies minstens één moment waarop je kunt".

### w2-03 Reizen (step 3, new)
- `h1` "Hoe kom je op je werk?" Lead "Dan laten we alleen banen zien die goed te bereiken zijn."
- `h2` "Vervoer": a 3-column grid of 48px segmented buttons (`aria-pressed`) with the **existing values only**: Lopend ("Lopen"), Fiets, E-bike, OV, Auto.
  - Don't add Scooter; the mockup shows it, but there is no backend value.
  - Default: keep the existing value, else none; required.
- `h2` "Maximaal reizen" with the value at the end ("20 min"): 4 segmented buttons, 10 / 20 / 30 / 45 min, plus a small "Anders" link that reveals a number input (1–180). Default: the existing `_maxTravel` (30).
- **Optional live line:** "Binnen {n} min {fietsen|met OV|…} vanaf {woonplaats} zijn nu {count} vacatures."
  - Only implement this if an existing API gives a count cheaply (e.g. the vacancy search or pins endpoint with a travel filter). Don't add a new heavy endpoint; if there's none, leave the line out.

### w2-04 Werkervaring (step 4, optional)
- `Optioneel` pill, `h1` "Heb je al gewerkt?", lead "Bijbaan, vrijwilligerswerk of stage telt ook mee."
- Saved jobs as summary cards (surface, shadow): 40px initials square (`--accent-soft`/brand), role 600, "{werkgever} · {periode}" muted, and an end `icon-btn` × (`aria-label="Verwijder {functie}"`).
- A dashed "＋ Nog een baan toevoegen" button (52px) opens an inline form card with the existing fields: Functie, Werkgever, Van (month), Tot (month, empty means now), and the buttons Opslaan / Annuleren. Max 3 jobs.
- A divider "of", then the checkbox card "Ik heb nog geen werkervaring" (disables and hides the list).
- Footer: "Volgende" (always enabled) and ghost "Overslaan" (existing `SkipStep3Async` logic, renamed for step 4).

### w2-05 Opleiding (step 5, optional)
- `Optioneel` pill, `h1` "Wat is je opleiding?", lead "Waar je nu mee bezig bent, of je hoogst afgeronde."
- A radio list (real `<input type="radio">` inside 44–52px rows, radius-sm, border). Selected: 2px brand border, `--accent-soft`, 600.
  - Rows: Geen (`EducationLevelLabels.None`) · Basisschool · VMBO · HAVO · VWO · MBO · HBO · WO.
  - When MBO is selected, show an inline segmented sub-choice "Niveau 1 2 3 4" under it, mapping to the existing `MBO 1…4` chips.
- `h2` "Richting" + "optioneel": suggestion chips that depend on the level, plus "Anders…" (text input, max 128):
  - HAVO/VWO: Natuur & Techniek, Natuur & Gezondheid, Economie & Maatschappij, Cultuur & Maatschappij
  - VMBO/MBO: Zorg & welzijn, Techniek, Economie & handel, ICT, Horeca & bakkerij, Groen
  - HBO/WO: free text only
- Save via `FormatEducationLine(level, direction)`. Footer "Volgende" plus ghost "Overslaan".

### w2-06 Droombaan (step 6)
- `h1` "Wat is je droombaan?" Lead "Denk groot. We maken er in Carrière een route naartoe."
- A search input (52px, search icon, placeholder "Zoek of typ je droombaan"), then a **3-column tile grid** (desktop: 6 columns). The first 12 tiles show without scrolling on mobile; the rest follow on scroll, with the hint "Scroll voor meer beroepen".
- **Tile:** `<button aria-pressed>`, 84–92px high, radius, surface, border. 40px icon circle (`--accent-soft`, brand icon) with the label under it (xs, 600, centred, 2 lines max).
  - Selected: 2px brand border, accent background, icon circle brand with a white icon, and a 20px brand check badge at the top end.
  - Single select.
- **Remove entirely** from the wizard and the catalog:
  - `PopularDreamJobChips`, `DreamJobChipsByRiasec*`, `DreamChipsForRiasec`
  - the result "Past je droombaan nog?" chip block
  - `DreamJobSuggestions` in `OnboardingImpression`/`CandidateOnboardingImpressionDto`, and its producer in `CandidateOnboardingService`
  - Update the tests. No vacancy titles or vacancy examples anywhere on this step.
- Footer: "Volgende" (enabled when a tile is chosen or there's free text) plus ghost "Weet ik nog niet" (existing `PickDreamUnknown`: saves null and marks the step skipped).
- Save via the existing `SaveOnboardingDreamJobAsync(title)` → `CareerPlans.SaveDreamAsync` (Carrière picks it up). Store the Dutch display title.

**Dream-job catalog:** a new `Jobsy.Core/Rules/DreamJobCatalog.cs` with `record DreamJob(string Key, string TitleNl, string IconKey, string[] Synonyms)`. Put the Dutch titles in the catalog and the per-language titles in `UiStrings` as `DreamJob.{Key}`. Icons are inline SVG strings in a new `Jobsy.Web/Navigation/DreamJobIcons.cs`, in the style of `NavIcons.cs`: stroke `currentColor`, 1.75, 24×24 viewBox, `aria-hidden`. Copy the path data from **Lucide** (ISC licence; add attribution in a comment).

Order (the first 12 show on mobile) → Lucide icon:
1. dierenarts → `paw-print`
2. piloot → `plane`
3. advocaat → `scale`
4. leraar → `presentation`
5. arts → `stethoscope`
6. architect → `drafting-compass`
7. kok → `chef-hat`
8. brandweer (Brandweerman/-vrouw) → `flame`
9. game-developer → `gamepad-2`
10. astronaut → `rocket`
11. politie (Politieagent) → `shield`
12. verpleegkundige → `heart-pulse`
13. ondernemer → `briefcase`
14. journalist → `mic`
15. fotograaf → `camera`
16. kapper → `scissors`
17. programmeur → `code`
18. bouwkundige → `hard-hat`
19. tandarts → `smile`
20. psycholoog → `brain`
21. fysiotherapeut → `activity`
22. verloskundige → `baby`
23. apotheker → `pill`
24. rechter → `gavel`
25. notaris → `stamp`
26. ingenieur → `cog`
27. elektricien → `plug-zap`
28. automonteur → `wrench`
29. timmerman → `hammer`
30. hovenier → `trees`
31. bioloog → `leaf`
32. wetenschapper → `flask-conical`
33. grafisch-ontwerper → `pen-tool`
34. muzikant → `music`
35. acteur → `drama`
36. profsporter → `trophy`
37. personal-trainer → `dumbbell`
38. contentmaker → `video`
39. marketeer → `megaphone`
40. accountant → `calculator`
41. makelaar → `house`
42. cabinepersoneel → `luggage`
43. militair → `medal`
44. pedagogisch-medewerker → `blocks`
45. dierenverzorger → `rabbit`
46. evenementenorganisator → `party-popper`
47. data-scientist → `chart-line`
48. bakker → `croissant`

If an icon name doesn't exist in Lucide, pick the closest one and note it.

**Search (w2-06b):**
- Filter the catalog client-side: case- and diacritics-insensitive, prefix match on the title first, then contains on the title and synonyms. For example, "dier" → Dierenarts, Dierenverzorger (+ synonyms such as "dierenartsassistent" if you add them).
- Show at most 6 suggestion rows under the input in a dropdown card (surface, `--shadow-lg`, radius). Each row is 56px: 34px icon circle, the title with the matched part in 600 brand, optionally a muted tag at the end.
- The last row is always "＋ Gebruik "{tekst}" als eigen droombaan" (brand, 600), which saves free text (trimmed, max 128).
- Keyboard: ↑/↓/Enter/Esc. Use `role="combobox"` + `listbox`, with `aria-activedescendant`.
- Hint under the dropdown: "Staat je droombaan er niet bij? Typ hem gewoon in."
- Choosing a suggestion selects the matching tile (scroll it into view) or the free-text value.

### w2-07 Tests intro + consent (step 7, first sub-view)
- `h1` "Nu even over jou". Lead "4 korte tests met een paar vragen per test. Er zijn geen foute antwoorden: kies wat het eerst in je opkomt."
- A numbered list (32px circles):
  1. Hoe je werkt · Competenties, zoals samenwerken
  2. Wat je leuk vindt · Beroepen die bij je passen
  3. Waar je je thuis voelt · Werksfeer en cultuur
  4. Wat je belangrijk vindt · Je waarden
- A consent card with a switch (`role="switch"`, `aria-checked`): "Ik geef toestemming om mijn antwoorden te analyseren voor mijn Kompas." The link "Meer uitleg" opens the existing consent text in a `LobsyFriendlyDialog`/`<details>`.
  - This replaces the current "Toestemming geven en verder" block and keeps `AcceptTestAiConsentAsync`.
- A lock hint: "Werkgevers zien je antwoorden niet."
- The primary "Start de eerste test" is enabled only when the switch is on. It calls `AcceptTestAiConsentAsync`, then shows the questions of step 7.
- Skip this sub-view if consent already exists.

### w2-08 Mini-test (steps 7–10)
- **Test tabs** at the top: 4 equal pills (xs, 600): "Hoe je werkt" · "Leuk" · "Sfeer" · "Waarden". Current = brand/white, done = `--success-soft`/success, future = pearl/muted. Not clickable forward.
- `h1` (`--text-xl` here), e.g. "Hoe werk jij?" / "Wat vind je leuk?" / "Waar voel je je thuis?" / "Wat vind je belangrijk?". Lead "Hoe goed past elke zin bij jou?"
- **Collapsing list** (extend `LikertScaleQuestion` with parameters such as `Collapsed`, `IsCurrent`, `ScaleLabels`; keep the component name):
  - **Answered** questions collapse into a one-line card: check icon (success), truncated text (ellipsis), the chosen value in 600 brand at the end. Tapping it re-expands the question to change the answer.
  - The **current** question (first unanswered) is expanded: a 2px brand ring, "{i}/{n}" muted prefix plus text (`--text-md`), 5 equal buttons 1–5 (44px high, radius-sm, `aria-pressed`, `aria-label="{i} van 5"`), and the end labels "Past niet" / "Past heel goed" (xs, muted).
  - The **next** question shows dimmed (opacity .55, not interactive); later ones are hidden until reached.
  - After answering, auto-advance and `scrollIntoView({block:"center"})` the new current question. Use `behavior:"auto"` under `prefers-reduced-motion`.
  - Save each tap immediately (existing `OnMiniAnswerAsync`, merged answers, `complete:false`) and flash "Bewaard" in the header.
- The footer primary is "Volgende test" (step 10: "Bekijk mijn Kompas"), disabled until all questions of this test are answered. Hint under it: "Nog {n} vragen · telt mee voor de volledige {Competentie|Beroepen|Cultuur|Waarden}test".
- Keep the question IDs from `OnboardingWizardCatalog` (Beroepen keeps its 6 RIASEC items; the copy says "een paar vragen", not "5"). Keep the "already answered" handling (`MiniAllDone`).

### w2-10 Klaar (finish, uncounted)
- No header bar, just the status area.
- A centred 112px ring (8px `--brand` stroke) with a 52px compass icon (`NavIcons` style).
- Badge "Eerste indruk" (xs, 600, brand on accent-soft).
- `h1` "Je Kompas staat klaar, {voornaam}". Lead "Dit valt ons nu al op:"
- 3 highlight cards (surface, shadow, 12px 14px). Each: 36px icon circle, a muted xs label, and a 600 value:
  - "Je sterkste punt" ← `Strengths[0]`, short sentence
  - "Werk dat bij je past" ← top 2 RIASEC labels joined by " en "
  - "Belangrijk voor jou" ← `TopValue`
  - Hide a card if its data is missing.
- Key figure: "{n} vacatures passen al bij je" (number in `--text-2xl` 600 `--success`). Hide when 0.
- **No match cards and no dream chips** here (remove the old `ob-match-cards` block).
- Primary "Naar mijn Kompas" → `CompleteMyOnboardingAsync()` → the **existing Install/push phase** (unchanged; `lobsyPwaInstall`, "Later"). After install or "Later", navigate to the Kompas at **`/candidate/profile`** (currently `/candidate/match`; change both `NavigateTo("/candidate/match")` calls).
- Ghost link "Volledige tests doen voor meer precisie" → `/home?tab=tests` (existing).
- Calm: no confetti, no animation beyond an optional 300ms ring fade-in (off under reduced motion).

### "Later verder", resume and back
- "Later verder" saves pending changes (flush the debounce), calls `SaveMyOnboardingProgressAsync(step)`, and navigates to `/home`. There, `CandidateOnboardingResumeCard` shows. Update its copy: "Maak je startprofiel af" / "Nog {n} stappen, daarna staat je Kompas klaar." / "Verder waar je was".
- **Resume** opens `/candidate/start` at `CurrentStep`, with every field reloaded from the profile, preferences (incl. `availabilityPresets`), dream job and test drafts.
- **Back** goes to the previous step without losing anything: data is already persisted, and in-memory state is kept.

## 5. Data model / migration notes
- Migration `AddOnboardingWizardVersion`: `CandidateOnboardings.WizardVersion int NOT NULL DEFAULT 1`. The service creates new rows with 2 and applies the v1→v2 mapping on read (section 2).
- `PreferencesJson`: new optional `availabilityPresets: string[]` and `availabilityPresetsOverridden: bool`. JSON only, so no column. Update `CandidatePreferencesDto`, `CandidatePreferences` and the mapping in the profile API.
- The dream job still goes through `SaveDreamAsync` (`CandidateCareerPlan.DreamTitle`); no schema change.
- Remove `DreamJobSuggestions` from the DTOs; check no other consumer uses it (`git grep`).
- Keep `AvailableFromDate` (already migrated) and the education line format.

## 6. Accessibility
- All tap targets are ≥44×44px (tiles, chips, matrix cells via hit-area padding, scale buttons, back, "Later verder", remove ×).
- Every input has a visible `<label>`. Grouped inputs use `<fieldset><legend>` (date of birth, presets, transport, travel, education, matrix). Toggles use `<button aria-pressed>`; radios are real radios; the switch uses `role="switch"`.
- The progress bar has ARIA values (section 3). The save status uses `aria-live="polite"`. Errors are linked with `aria-describedby`.
- Visible `:focus-visible` outline in `--brand`. Contrast is AA. Status is never shown by colour alone: a check icon plus the text "Bewaard".
- Focus moves to the `h1` on each step change. Back/next are reachable by keyboard; the combobox supports keyboard navigation.
- RTL: logical properties; chevrons flip under `[dir="rtl"]`.

## 7. Tests

**Unit** (`Jobsy.Tests/CandidateOnboardingWizardTests.cs` + a new `AvailabilityPresetRulesTests.cs`, `DreamJobCatalogTests.cs`):
- Presets: each preset maps to its exact hours and day-parts. Combinations are order-independent (property test over permutations). Examples from section 4. `parttime`/`fulltime` only add days without a day-specific preset. The override flag stops recomputation.
- `DreamJobCatalog`: ≥40 entries, unique keys and titles, every `IconKey` exists in `DreamJobIcons`. Search "dier" returns Dierenarts first; the search ignores diacritics and case.
- `OnboardingWizardCatalog`: `PopularDreamJobChips`/`DreamChipsForRiasec` are gone, `StepCount == 10`, phases cover 1–10 without gaps, `MapV1Step` covers 1–10.
- Update the existing asserts (keep `LikertScaleQuestion`, `SaveOnboardingDreamJobAsync`, `CompleteMyOnboardingAsync`, `lobsyPwaInstall`, `Onboarding.Later`, and the resume card).

**Playwright** (new `Jobsy.Tests/OnboardingWizardV2PlaywrightTests.cs`, in `[Collection("PlaywrightSmoke")]`):
- Same soft-skip pattern and `JOBSY_E2E_BASE_URL` as `MobileSmokePlaywrightTests`.
- Use a dedicated seeded candidate (add `onboarding.e2e@jobsy.local` / `Jobsy123!` to the existing dev/CI seed, with onboarding reset per test through the same seeding/reset mechanism the e2e stack already uses; if none exists, add a dev-only reset in the seeder, never a public endpoint).
- Run each scenario on **mobile 390×844** and **desktop 1280×800**, and save screenshots to `artifacts/playwright-onboarding/…`:
  1. **Full walkthrough:** welcome → steps 1–10 → finish shows "Je Kompas staat klaar" → "Naar mijn Kompas" → install screen → "Later" → URL `/candidate/profile`. Assert the progress text "stap x van 10" on every step and a visible "Bewaard" after edits.
  2. **Presets fill fields:** select "Bijbaan naast school" + "Weekenden" → the summary shows "6 – 16 uur" and the matrix has exactly the expected cells on. Deselect "Weekenden" → only the Zo Ochtend and Zo Middag cells turn off (Za stays on because of "Bijbaan naast school"). Change the slider → the override hint appears and presets no longer overwrite.
  3. **Skip optional steps:** "Overslaan" on steps 4 and 5 and "Weet ik nog niet" on step 6 → reaches step 7; the `StepsJson` skipped events are recorded (via `GET api/me/onboarding`).
  4. **Resume via "Later verder":** at step 3 → lands on `/home` with the resume card → click it → back at step 3 with transport and travel still selected.
  5. **Back keeps data:** fill step 1 → Volgende → change a preset on step 2 → back → step 1 fields unchanged → forward → preset still selected.
  6. **Dream-job search:** type "dier" → the suggestion "Dierenarts" is visible → pick it → the tile is selected. Type "zeiler" → "Gebruik "zeiler" als eigen droombaan" saves it.
  7. **Layout guard:** on each step at 390×844, no horizontal overflow (`document.documentElement.scrollWidth <= 390`) and the primary button is fully visible.
- Run `dotnet build` and `dotnet test` (unit) locally. Playwright runs where the stack is available.

## 8. Definition of done
- All 12 mobile screens and 2 desktop layouts match the reference images (allowed deviation: no Scooter option; test copy "een paar vragen").
- One PR against `acceptatie` titled "Onboarding wizard v2 (/candidate/start)", including the `docs/mockups/wizard-v2/` folder from branch `docs/wizard-v2-mockups`, with a description listing:
  - the reference image filenames
  - the step-count decision
  - the preset rules table
  - the migration
  - the removed items (`PopularDreamJobChips`, RIASEC dream chips, `DreamJobSuggestions`, result match cards)
  - test results
- Not merged, not deployed, no shortcut `123`.
