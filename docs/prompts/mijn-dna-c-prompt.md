# Cursor prompt: Mijn DNA variant C, Kompas load speed, and the mobile test-screen fix

## 0. Rules (read first)
- **Branch:** `git fetch origin && git checkout -b cursor/mijn-dna-c origin/acceptatie`. The code references below are from `origin/acceptatie` @ `a31aba9` (2026-09-27 14:12 CEST); re-check the line numbers, since they may have moved.
- **Exactly ONE pull request, into `acceptatie`.** Do **not** merge it, do **not** deploy, and do **not** use or trigger shortcut/rule **`123`** (or `456`). Never push to `acceptatie` or `main`. See `docs/release-flow.md`.
- **Reference images** are on branch `docs/mijn-dna-c`, folder `docs/mockups/mijn-dna/`. Get them with `git fetch origin docs/mijn-dna-c && git checkout origin/docs/mijn-dna-c -- docs/mockups/mijn-dna docs/prompts/mijn-dna-c-prompt.md`, and include that folder in the PR.
  - `dna2-c-hoogtepunten.png`: the first screen of Mijn DNA, variant C (the chosen one).
  - `dna2-c-hoogtepunten-lang.png`: the full page of variant C.
  - `dna2-overview.png`: the current page next to variants A, B and C, for context only.
  - `test-mobiel.png`: the fixed mobile test screen.
  - All values in the mockups are **examples**. Use the real score fields.
- **Design system:**
  - Use tokens only (`--brand`, `--bg`, `--surface`, `--muted`, `--border`, `--accent-soft`, `--success`, etc.).
  - Font weights are 400/600, with 700 only for the page `h1`/`h2` title.
  - Tap targets are at least 44px. Use logical properties so RTL works.
  - No inline `style=""`. The only exception is CSS custom properties for chart values, such as `style="--w:62%"`.
  - All text goes through `Culture[...]` in nl/en/pl/ro/ar.
  - New CSS goes in `wwwroot/css/features/<feature>.css`, linked in `Components/App.razor` next to `onboarding-wizard.css` and `applications.css`, with both the `<link>` and the `<noscript>` copy. Bump `?v=`.
- **Keep all existing routes and behaviour:**
  - `/candidate/profile?tab=dna|profile|tests|fit`
  - `/home` (Kompas header)
  - the test routes `/candidate/competencies`, `/candidate/career`, `/candidate/culture` and `/candidate/values`
  - the deep-analysis pages and the onboarding wizard
- **Work in the order of the steps below.** Commit after each step with a clear message (`step 1: …`) so it can be reviewed.

---

## Step 1: Test-screen mobile overflow fix (do this first; it's a bug on acceptatie)
**Symptom** (Dennis's phone, `/candidate/competencies`): the question text, the privacy note and the 1–5 scale are cut off on the right. Option 5 and "Past wel" are invisible.

I measured it at 390px: `fieldset.q-likert` is **526px** wide. It shrinks to 363px after the fix below.

**Cause:** commit `3c1b8dd` ("Implement onboarding wizard v2…") added `Jobsy.Web/wwwroot/css/features/onboarding-wizard.css`, which `App.razor` loads on **every** page. Its **line 10** contains **unscoped** `.q-likert*` rules, even though the file header says "All rules are scoped to the route":
- `.q-likert__text{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}` was meant for the collapsed answered row. It now stops every question in every test from wrapping.
- A `<fieldset>` has `min-inline-size:min-content` by default. With a no-wrap legend, the card grows to the full width of the text, and the whole column overflows.
- The same line also overrides the shared questionnaire styling in `wwwroot/css/app.css`: `.q-likert` at ~17318, `.q-likert__legend` at ~17341 (the column layout becomes a row) and `.q-likert__scale` at ~17376.
- `Jobsy.Web/Components/Shared/Questionnaire/LikertScaleQuestion.razor:15-43` was rewritten in the same commit, from radio circles to buttons with `.q-likert__labels`, and gained the `Collapsed`, `IsCurrent` and `Dimmed` states.

**Fix:**
1. In `onboarding-wizard.css`, prefix **all** `.q-likert*` rules with `.ob-wizard ` so they only apply inside the wizard.
2. Apply `white-space:nowrap` / `ellipsis` **only** to `.q-likert--collapsed .q-likert__text`.
3. In the shared questionnaire CSS (move the `.questionnaire*` / `.q-likert*` block from `app.css` ~17174–17600 into a new `features/questionnaire.css`, and remove it from **both** `app.css` and `app.min.css`), add:
   - `fieldset.q-likert{min-inline-size:0}`
   - `.q-likert__text{white-space:normal;overflow-wrap:anywhere}`
   - `.q-likert__legend{float:inline-start;inline-size:100%}` followed by `clear:both` on the next element, so the legend no longer sits on the fieldset border.
   - `.q-likert__scale{grid-template-columns:repeat(5,minmax(0,1fr))}`
   - Make the options `min-block-size:48px` with the number centred.
4. **Privacy space bug:** `Jobsy.Web/Components/Shared/Questionnaire/QuestionnaireShell.razor:81-83` renders "…Meer in de**Privacyverklaring**". The `<text> </text>` there is whitespace-only, so the Razor compiler strips it. Use `@(" ")` or restructure the markup.
5. **Implement the `test-mobiel.png` layout** in `QuestionnaireShell` + `LikertScaleQuestion`, for all 4 tests (and the deep analysis if it uses the same components):
   - **Header:** 44px close button (existing `BackHref`), the test title, and the subtitle "{categorie} · vraag x van n". The "✓ Bewaard" status (existing `SaveStatus`, `aria-live="polite"`) goes at the end. Under it, a progress bar with "{answered} van {total}" (existing `AnsweredCount` / `TotalCount`), with `role="progressbar"` and ARIA values.
   - **Privacy note** collapsed to one line: a lock icon, "Werkgevers zien je antwoorden niet." and a "Meer" link. The link opens the existing full `PrivacyNote` text (`<details>` or `LobsyFriendlyDialog`) plus the privacy link. Add new keys `Questionnaire.PrivacyShort` and `Questionnaire.PrivacyMore`.
   - **Answered questions** become collapsed one-line rows: a check in `--success`, the truncated text, and the chosen value in 600 `--brand`. Tapping a row re-expands it (the existing `Collapsed` / `Expanded` parameters).
   - **The current question** (first unanswered) is expanded with a 2px brand ring. The text wraps fully (`--text-md`, 600), with a muted "i/n" prefix.
   - **5 equal buttons** (`aria-pressed`, `aria-label="{n} van 5"`) with "Past niet" / "Past wel" under the ends. `Onboarding.Likert.High` currently says "Past heel goed"; the questionnaire uses "Past wel", so add a key or parameter for it.
   - **The next question** is dimmed (opacity .5). Later questions stay visible, but the page auto-scrolls to the current one (existing `ScrollToQuestionId`; respect `prefers-reduced-motion`).
   - **Sticky footer:** a full-width 52px primary button. It reads "Volgende" and scrolls to the next unanswered question or category. On the last question, or when everything is answered, it reads "Afronden" (existing `OnComplete` / `Completing`). Under it goes a muted hint "Nog {n} vragen".
6. **Check the wizard (`/candidate/start`) still looks right**, including collapsed rows and the 4 test tabs. Update `Jobsy.Tests/QuestionnaireCompactTests.cs` and `OnboardingWizardV2PlaywrightTests.cs` where their assertions change.

## Step 2: Real test data for the first-impression state
- **The charts are built but never shown.** `Jobsy.Web/Components/Candidate/DnaPanel.razor:100-105`: when a test is provisional (`!card.Completed && card.IsProvisional`), only the "Volledige tests doen" button renders.
  - The bars, chips and poles that `BuildCompetence` / `BuildCareer` / `BuildCulture` / `BuildValues` (`DnaPanel.razor:314-486`) compute for provisional cards are thrown away. That's why Dennis sees 4 empty blocks.
  - Render the charts for provisional cards too, marked as a first impression.
- **Culture and Values have no preview scores:**
  - `CandidateCulturePersonalityService.SaveAsync` calls `ClearScores(row)` for drafts (`Jobsy.Infrastructure/Services/CandidateCulturePersonalityService.cs:93`), and so does `CandidateValuesService.cs:94`.
  - Their DTOs (`Jobsy.Core/Interfaces/ICandidateCulturePersonalityService.cs:20`, `ICandidateValuesService.cs:20`) and the web models `CandidateCultureState` / `CandidateValuesState` (`Jobsy.Web/Models/CompetencyModels.cs`) have no `PreviewScores`.
  - Add `PreviewScores`, computed **on read** in `ToDto` from `AnswersJson` via `CulturePersonalityCatalog.Score(answers)` / `SchwartzValuesCatalog.Score(answers)`. This mirrors `CandidateCompetencyService.ToDto` (`CandidateCompetencyService.cs:147-170`).
  - **Keep `Scores` null for drafts.** Matching and completeness treat non-null score columns as completed, so "stop wiping" means *expose a preview*, not *store partial scores as final*.
  - In `DnaPanel.ApplySnapshot` (`DnaPanel.razor:218-240`), use `Scores ?? PreviewScores` for Culture and Values too, as already done for Competencies and Career.
  - Add unit tests: a draft with N answers returns `PreviewScores` for the answered dimensions only, and `Scores` stays null.
- **Available fields per test** (use these for the charts):
  - **Competenties** (`CompetencyScoreSet`): Samenwerken, Resultaatgerichtheid, Stressbestendigheid, Innovatie, Extraversie. The labels come from `DeepAnalysisQuestionHelp.DomainLabel` / `DimensionLabels`.
  - **Beroepen** (`RiasecScoreSet`): R, I, A, S, E, C. The labels come from `CareerCompassBuilder.TypeLabel`, plus `HollandCode` and `Compass.SuperMatches[].Percent`.
  - **Cultuur** (`CulturePersonalityScoreSet`):
    - The culture dimensions are Autonomy, Informal, Collaboration, Flexibility, Innovation and PeopleFirst, with the pole labels `Dna.CulturePole.*.Low/High` (`DnaSummarySentences.CulturePoles`).
    - The set also holds the Big Five scores (Openness, Conscientiousness, Extraversion, Agreeableness, EmotionalStability). These are optional; they're not shown in variant C.
  - **Waarden** (`SchwartzValuesScoreSet`): Autonomy, Connection, Achievement, Stability, Impact, with labels from `DimensionLabels`.
  - **Status** comes from `Status`, `AnsweredCount` / `QuestionCount`, and the deep-test `IsCompleted`.

## Step 3: Mijn DNA layout, variant C (`dna2-c-hoogtepunten*.png`)
Refactor `DnaPanel.razor`, which is rendered from `CandidateKompas.razor:111`. Move its CSS from `app.css` ~21128 ("Mijn DNA: story + test summary cards") into `features/mijn-dna.css`, and remove the old rules from both `app.css` and `app.min.css`. Keep the Kompas header (title, completeness bar, tab bar) as it is. From top to bottom:

1. **Jouw verhaal** (tighter than today):
   - A card with the `h3`, then the story clamped to **2 lines** plus a "Lees verder" toggle (existing).
   - Keywords go on **one row** of pill chips (`--accent-soft`/`--brand`, `--text-xs` 600, 28px high) that fades at the end instead of wrapping. Max 6, from the existing `_keywords`.
   - Remove "Bijgewerkt op …" from the default view; show it only in the expanded state.
   - Keep the "Updating" notice and the empty state (`Dna.StoryEmpty` + CTA).
2. **"Dit valt op"** is a 3-column grid of highlight cards (surface, shadow, radius, padding 10px). Each card has a label (`--text-xs`, muted) and a value (`--text-sm`, 600, brand):
   - **Sterkste punt:** the highest competency label.
   - **Werk dat past:** the top RIASEC `TypeLabel`.
   - **Belangrijk:** the top value label.
   - Use full scores if available, else preview scores. Hide a card when it has no data. Hide the whole row when all 3 are empty.
3. **Swipeable chart card** (a horizontal scroll-snap carousel, one card per test that has data, with dots under it; `role="region"` and `aria-roledescription="carousel"`, every card reachable by keyboard):
   - **Beroepen:** a RIASEC hexagon radar (inline SVG, 6 axes, brand stroke; dashed when it's a first impression) next to the top 3 as lettered bars ("S Mensen helpen 82%").
   - **Competenties:** a 5-axis radar.
   - **Cultuur:** 6 pole sliders.
   - **Waarden:** 5 horizontal bars, with the top 3 in `--brand` and the rest in a muted brand tint.
   - The header row has the title and the status pill.
   - Build the charts as small, dependency-free Razor components (e.g. `Components/Shared/Charts/RadarChart.razor`, `BarList.razor`, `PoleSliders.razor`) with pure static geometry helpers and unit tests.
   - Each chart gets an accessible text alternative: a visually hidden list of "label: value%".
   - No JS chart library.
4. **"Je 4 tests"** is a 2×2 grid of tiles (gap 10px, min-height ~150px). Each tile shows the title (`--text-sm` 600), a status pill, a **64px progress ring** (SVG) showing `AnsweredCount/QuestionCount` in the centre, a one-line summary, and a CTA row with a chevron at the bottom.
   - **Eerste indruk:** `--gold-soft` / `--gold-ink` pill if those tokens exist, otherwise the existing `dna-card__badge--provisional` colours. Brand ring. CTA "Verder · {n} vr." → the test's `StartHref`.
   - **Volledig:** `--success-soft` / `--success` pill with a check, and a success-coloured full ring. CTA "Bekijk uitslag" → `DetailHref`. Show "Extended" when the deep test is completed (existing `Test.Status.Extended`).
   - **Niet gedaan:** transparent pill with a border, an empty ring "0/n", and a tile background of `#fbf8f5` with a border (use tokens). CTA "Start · ± {min} min".
   - The whole tile is one link, with an accessible name like "Competentietest, eerste indruk, 6 van 25 beantwoord".
5. **Detail sections** below the tiles:
   - "Waar je je thuis voelt" (culture pole sliders, full width) when culture data exists.
   - "Waarden op werk" (bars) when values data exists.
6. **Teaser for tests not done:**
   - In the carousel and the detail sections, show a **ghost chart** (dashed, grey `--border` tints, no values) with the muted line "Doe de {test} en zie …" and a secondary 48px button "Start {test} · {n} vragen".
   - Never show fake numbers.
- **Desktop (≥1024px):** keep the existing two-column Kompas layout with the Top-10 side panel (`CandidateKompas.razor`). Inside, the highlights stay 3 columns, the carousel becomes a 2-column grid of chart cards (no swiping), and the tiles become 4 in a row.
- **i18n:** add keys for the section titles, highlight labels, tile CTAs, teaser lines and the chart alt texts, in all 5 languages.

## Step 4: Load speed (`/candidate/profile?tab=dna`)
**Measured on acceptatie at 390px:**
- Cold: server response starts after 1.35s, the page is loaded at 2.0s, and the network is quiet at 2.9s.
- Warm: 0.72–0.80s / 0.83–0.98s / ~1.9s.
- The **HTML is 415 KB, of which 379 KB (91%) is `Blazor-Server-Component-State`.**

**Targets:** HTML **< 60 KB**, and the server response starts in **< 300 ms** (warm, acceptatie).

1. **Add a small endpoint.** `CandidateKompasController` (`Jobsy.Api/Controllers/CandidateKompasController.cs`, `api/me/kompas`) gets `GET api/me/kompas/dna`, returning a new `CandidateDnaSummaryDto` with:
   - the story, keywords, generated date and status (`BuildWhoAmIStory`, `CandidateKompasService.cs:185`)
   - for each of the 4 tests: status, answered and total counts, `Scores`, `PreviewScores` and deep-completed flags
   - the completeness percent
   - **No** question lists, answers, profile/preferences or matches.
   - Build it with **one or two queries**: project only the score columns from the 4 test tables and the deep-analysis table. Add `JobsyApiClient.GetMyKompasDnaAsync` with the same `_meCache` pattern as `GetMyKompasResultAsync` (`JobsyApiClient.cs:1348`).
   - `DnaPanel` and the Kompas header use it. Keep `GET api/me/kompas` for the Profiel tab.
2. **Stop persisting the full `CandidateKompasState`** into the page:
   - `Profile.razor:738-766` (`PersistKey = "candidateProfileKompas"`, `PersistAsJson` at :763) serialises everything into the HTML. That covers the profile, all answers and question lists (`CandidateCompetencyService.ToDto` returns all 25 `Questions`, as does career), unlocked deep-test questions (`DeepAnalysisService.cs:~531`), and the matches twice.
   - Persist **only** the `CandidateDnaSummaryDto` (a few KB) when the DNA tab is active, and load the Profiel-tab data lazily when that tab opens.
   - Mirror the approach and test of `BanenkaartPersistSizePlaywrightTests.cs` ("never persist the full … catalog").
3. **Duplicate and heavy match data:**
   - `CandidateKompasService.cs:107` sets `career with { TopVacancies = matches }`, so the same list also ships as `TopMatches`. Drop `TopVacancies` from the Kompas response, or send an empty list, and check its consumers with `git grep`.
   - `CandidateMatchSnapshotService.cs:282-283` passes raw `vacancy.ImageUrl` / `CompanyLogoUrl`, which can be inline data URIs. Pass them through `VacancyImageUrls.ForCard(imageUrl, logoUrl, id, workType)` (`Jobsy.Core/Media/VacancyImageUrls.cs:93`), as `VacanciesController` does, so no base64 ends up in JSON or persisted state.
   - Existing snapshots in the database keep raw values until they're recomputed, so map them on read too.
4. **Batch the deep-test states:** `CandidateKompasService.cs:82-85` makes 4 sequential `_deep.GetStateAsync` calls, each 2–3 queries (`DeepAnalysisService.cs:74-84`). Add `GetStatesAsync(userId, kinds)` that loads all 4 rows in one query. In general, collapse the ~25 sequential queries in `CandidateKompasService.GetAsync` (`:51-85`) where possible.
5. **Freshness check:** `CandidateMatchSnapshotService.GetAsync` (`:36-60`) runs `ComputeInputFingerprintCheapAsync` (`:46`, 5 queries over the test and preference rows) on **every** read.
   - Store the fingerprint inputs' `UpdatedAtUtc`, or mark the snapshot dirty when tests or preferences are saved (a flag set in the save paths), so the GET is one query.
   - Also check the 3-second re-fetch loop in `Profile.razor:901` / `:993` (`RefreshInsightsLaterAsync`): it should refresh only the matches, not the whole Kompas.
6. **Reverse-geocode after the first render:**
   - `Profile.razor:939` awaits `Geocoder.ReverseAsync` (5-second timeout) during `OnInitializedAsync` / prerender when `HomeAddress` is empty.
   - Show the coordinate fallback first, then resolve in the background after the first render (`OnAfterRenderAsync`, fire and forget with cancellation) and **store** the result in the preferences so it happens once.
7. **Measure before and after** (the same Playwright script as the tests in step 5) and put the numbers in the PR description.

## Step 5: Tests
**Unit / structure**
- Culture and Values `PreviewScores` (step 2).
- The chart geometry helpers.
- The DNA summary DTO has no questions or answers.
- `Profile.razor` no longer persists the full `CandidateKompasState`.
- `onboarding-wizard.css` has no unscoped `.q-likert` selector (assert that every `.q-likert` occurrence is preceded by `.ob-wizard `).
- `fieldset.q-likert{min-inline-size:0}` exists.
- The privacy link has a space before it.
- Update `KompasDnaLandingTests.cs`, `KompasTabsLayoutTests.cs` and `QuestionnaireCompactTests.cs` where their assertions change.

**Playwright:** a new `Jobsy.Tests/MijnDnaAndTestsMobilePlaywrightTests.cs`, following the `MobileSmokePlaywrightTests` pattern (`JOBSY_E2E_BASE_URL`, soft-skip when unreachable, seeded candidate `kandidaat@jobsy.local` / `Jobsy123!`, or an e2e candidate with first-impression answers seeded for all 4 tests plus one full test). Run it at **360×780, 390×844 and 430×932**, and save screenshots to `artifacts/playwright-dna/`.
1. **No horizontal overflow:** `document.documentElement.scrollWidth <= innerWidth`, and no visible element's right edge is past `innerWidth + 1` on:
   - `/candidate/profile?tab=dna`
   - `/candidate/competencies`, `/candidate/career`, `/candidate/culture` and `/candidate/values`

   On each test page, also assert:
   - all 5 scale buttons of the current question are fully in the viewport
   - both end labels ("Past niet" / "Past wel") are visible
   - the question text wraps (the legend is taller than one line for a long question)
   - the sticky button is visible
2. **Charts in the first-impression state:** for a candidate with only first-impression answers, the DNA page shows ≥1 chart (`svg.radar` or `.bar-list li`) in the carousel, and the tiles show the "Eerste indruk" pill and a ring with "x/25". A test not done shows the teaser (ghost chart plus a Start button) and no numbers.
3. **HTML size and speed:** fetch `/candidate/profile?tab=dna` as the logged-in candidate and assert the HTML is **< 60_000 bytes**. Log the time to first byte and assert < 300 ms when `JOBSY_E2E_STRICT_PERF=1`; otherwise just log it, because CI network timing varies.
4. **No page errors:** collect `pageerror` and console errors during every test and assert there are none.
5. **Wizard regression:** `/candidate/start` mini-test at 390px still shows the collapsed rows and has no overflow.

Run `dotnet build` and `dotnet test` (unit tests) locally. Run Playwright where the stack is available, and report the results plus before/after timings in the PR.

## Definition of done
- Test pages don't overflow at 360, 390 or 430px and match `test-mobiel.png`.
- Mijn DNA matches `dna2-c-hoogtepunten*.png` with real data, and first-impression charts are visible.
- HTML < 60 KB and warm server response < 300 ms on acceptatie, or explain the remaining gap with measurements.
- **One PR into `acceptatie`**, titled "Mijn DNA variant C + Kompas snelheid + mobiele testschermen". It lists the steps, the file changes, the i18n keys, test results, screenshots and the performance numbers, and includes `docs/mockups/mijn-dna/`.
- Not merged, not deployed, no shortcut `123`.
