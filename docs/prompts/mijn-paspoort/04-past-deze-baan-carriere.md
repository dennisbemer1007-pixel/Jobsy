# 04 · Mijn Paspoort, Phase 3: tabs Past deze baan? + Carrière

> Read `00-README.md` first: §0 rules apply, plus §F (`<FeatureVisible Feature="Employers">` from 01) and `CourseSuggestionBlock` from 03. Execute this file only after the previous file's PR is open and green.

| | |
|---|---|
| Branch | `cursor/mijn-paspoort-3`, created from `cursor/mijn-paspoort-2` |
| PR | ONE PR into `acceptatie`, title `feat(paspoort): Past deze baan? and Carrière tabs`. Body starts with `Stacked on #<PR of 03> (cursor/mijn-paspoort-2)` |
| Mockups | `pp-d3-past-deze-baan.png`, `pp-m3-past-deze-baan.png`, `pp-d4-carriere.png` |

## 04.1 "Past deze baan?"
- **Logic:** reuse `RoleFitCheckPanel` (state `GetMyRoleFitAsync`, check `EvaluateRoleFitAsync`, `RoleFitCheckResult`).
  - Extract the state handling into a small Web-side state class `RoleFitCheckSession` so both the classic panel and `PassportFitTab` use it.
  - Keep the unlock rules: Quick-Scans needed, `LockMessage`.
- **Left card "Past deze baan bij mij?":**
  - Subtitle with antenna icon: "Vind de omgeving waar jouw antennes tot rust komen."
  - Input + "Check" (primary), and "Hele uitslag ›" in the header, which goes to the existing full result view.
  - "Laatste check: {JobTitle}" + "Vergelijkbare functies ›" (`SimilarRoles`).
  - **4 results (2×2), no percentage as the hero:**
    1. "Wat je antennes zeggen": a band label from `MatchPercent`. Add `RoleFitBandRules` (≥75 "Past goed", ≥50 "Past redelijk", else "Past nog niet") if no band helper exists. The % is only in the full result.
    2. "Klauwen die je al hebt": `Strengths`, top 2.
    3. "Klauw die nog groeit": `Gaps`, first, in the `--warn` icon colour.
    4. "Wat je kunt doen": `ActionSteps`, first.
  - **Course block** "Laat je klauw groeien" (`CourseSuggestionBlock`, context = the first gap + the job title's `SearchKeys`).
- **Right column:**
  - **Culture card** (antenna icon). Show it **only** when `CultureFitLabel` exists: title "Cultuur: {CultureFitLabel}", subline "Hier komen jouw antennes tot rust", and a status pill with the existing band text (never invented). No culture scan → a quiet link "Doe de cultuurscan".
  - **"Vacatures die bij jou passen"**, subtitle with stone icon: "Niet de grootste steen, maar die bij jouw formaat past."
    - Top 3 from `CandidateKompasState.TopMatches` (the same data as `CompetencyMatchPanel`), each with a logo initial, title, company · travel time and a band pill, plus "Top 10 ›" (the existing matches view).
    - Footer row: "Werkgevers die je willen spreken" with the pill "{n} nieuw" (existing talent-contacts count) and "Bekijk" → `/candidate/talent-contacts`.
    - The whole card is hidden when **Werkgevers actief is OFF**, and the server also returns nothing (01.2d).

## 04.2 "Carrière"
- **Data:** reuse `CareerPathService` / `GetCareerPathAsync` and the `CareerDashboard.razor` mapping. Extract the plan → steps / gaps / courses mapping (`CareerDashboard.razor` ~L390–650) into `CareerPlanViewBuilder` for both views. No new endpoints.
- **Card "Mijn droombaan":**
  - Title + "Open mijn hele plan ›" → `/carriere`.
  - Header action: "Droombaan wijzigen" (secondary) → the existing dream-job flow on `/carriere`.
- **Stepper as growing shells:** the shells get bigger from "Nu" to "Doel".
  - done = solid `--brand` + check
  - current = `--accent-soft` fill + dashed `--brand`, label "Groeit nu"
  - future = dotted `--border`
  - goal = `--gold-soft` + `--gold`
  - Labels come from the plan steps. Completing and uncompleting steps stays on `/carriere`.
- **3 cards:**
  - "Wat je nog mist", subtitle with claw icon: "Welke klauwen je al hebt, en welke je nog laat groeien." Gaps have a claw icon in `--warn`; met items have a check + "heb je al".
  - "Opleiding die past": `CourseSuggestionBlock` in the same style as the mockup, with the context = the next step's course names/keys. If the plan already lists a course, reuse `CareerCourseMatcher` to mark courses that are already claimed.
  - "Match op deze stap": eyebrow "Groei eerst. Match daarna.", stone line "Deze steen past al bij jouw formaat.", a text with the band for the next step's occupation, and "Vacatures voor deze stap ›".
    - When **Werkgevers actief is OFF**, this card shows only the growth text, without the vacancy link.
- **No plan yet:** an empty state "Kies je droombaan" + CTA to `/carriere`.

## Tests
- `RoleFitCheckSession` parity with the old panel; `RoleFitBandRules` bands.
- The 4 results come from the DTO fields, with no % in the tab.
- The culture card is hidden without `CultureFitLabel`.
- The vacancies card and the "Match op deze stap" link are hidden and absent in the DOM when employers are OFF.
- `CareerPlanViewBuilder` parity with `CareerDashboard`; the shell stepper states; the no-plan empty state.
- Course blocks via the context (free first, hidden when empty).
- Playwright: both tabs fit 1440×900; the fit tab on 390×844.

## Success criteria (all must hold before you open the PR)
- Both tabs replace the Phase 1 placeholders.
- **Shared logic:** `RoleFitCheckPanel` and `CareerDashboard` use the same extracted `RoleFitCheckSession` / `CareerPlanViewBuilder`, and their markup is unchanged.
- **No invented data:**
  - The fit tab shows band text, not a hero %.
  - The culture card appears only with real `CultureFitLabel` data.
- **Werkgevers actief OFF:** the vacancies card and the "Vacatures voor deze stap" link are absent from the DOM, and the server returns no vacancy data (01).
- **Course blocks:** they follow `CourseSlotRules` and are hidden without curated free data.
- **Layout:** both desktop tabs fit 1440×900, and the fit tab works at 390×844.
- `dotnet build` and `dotnet test` are green. Any existing test you changed has its reason in the PR body.
- The PR body has: stacked-on line, what and why, screenshots (where there is UI), test list, "Out of scope / deferred".

## Done → next
Push, open the PR, note its number. Then continue with **`05-bewijzen-mijn-gegevens.md`**. If anything above is red, stop and report (see 00-README "How to run" step 3).
