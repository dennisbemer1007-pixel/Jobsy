# 03: Step detail (claws, band, courses, proof) and the growth moment

> **Rules (repeated in every file):**
> - Branch from `origin/acceptatie` (file 01) or from the previous file's branch (stacked). ONE PR per file, always into `acceptatie`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`). Never push to `main` or `acceptatie`. No force-pushes. Push only `cursor/carriere-*` branches.
> - Red build/tests or an unmet success criterion: push, open that PR as **draft**, stop and report. Don't start the next file.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

| | |
|---|---|
| Branch | `cursor/carriere-3` from `cursor/carriere-2` |
| PR title | `Carrière 03: step detail with claws, fit band, courses and proof; growth moment and undo` |
| Body starts with | `Stacked on #<PR 02> (cursor/carriere-2)` + re-check outcome B, D, E, F |
| Mockups | `cr-d3-stap-detail-opleidingen`, `cr-d4-stap-klaar`, `cr-m3`, `cr-m4` |
| Split if too big | `03a` = §1–§3 (detail card, gaps, band, vacancies, courses), `03b` = §4–§6 (completion, moment, undo) |

**Goal:** one step, explained in plain words. It says what you still miss (claws to grow), how well this stone fits you (a band, not a number), which course helps (free first; partner labelled) and how to add proof. Finishing a step is a small, warm moment in the scene: the old shell falls, the lobster grows a gold new shell and moves up a stone. Undo is right there.

Closes: B6 (step), B7 (UI), B8, B9, B13 (step copy), B15.

## 1. Detail card (cr-d3 / cr-m3)
- Route: stays on `/carriere` with `?stap={order}` (deep-linkable, back button works; `NavigationManager` query, no reload). Invalid/foreign step ⇒ overview.
- Top: eyebrow "De klim · stap {n} van {total} · groeit nu" (done step: "· gehaald"; todo: "· later") + back link "Mijn groeireis" (mobile: back link above the eyebrow).
- h1 = step title (without the level in brackets if the catalog gives the level separately). Lead = the step summary from the plan (B1; the AI/local text), max 2 sentences (the builder cuts at a sentence end).
- Desktop layout: two columns "Wat je nog mist" | "Match op deze stap", then the courses. **Mobile: courses first**, then the two blocks stacked (as in `cr-m3`).
- Todo steps (later) are viewable read-only: no complete button, a quiet line "Eerst stap {current} afmaken."

## 2. Gaps and band
- **"Wat je nog mist"** + small "Welke klauwen je al hebt, en welke je nog laat groeien."
  - Missing = the step's `SkillsGap` + `MinRequirements` not covered, each with the claw icon.
  - Present = items matched by the candidate's certificates/tests/experience (builder), with the check and "· heb je al".
  - Max 6 lines + "Toon alles ({n})".
- `YearsText` only when > 0: "{n} jaar ervaring helpt" (B8: never "0 jaar").
- **"Match op deze stap"** with eyebrow "Groei eerst. Match daarna.":
  - Pill = `CareerFit.*` band (D4); `Unknown` ⇒ no pill and the line "Doe de Beroepen-test in je paspoort, dan zie je hoe goed dit past." (link to the test).
  - Sentence per band: Good "Deze steen past goed bij jouw formaat." · Fair "Deze steen past al redelijk bij jouw formaat. Met {first gap} worden je matches sterker." · NotYet "Deze steen is nog wat groot. Dat is normaal: je groeit ernaartoe."
  - **Vacancies (D6, Dependency D/E):** only when the employer gate is on, the link "Vacatures voor {step title short}" (→ href from `CareerStepActionLinks`). With Dependency E present and `CandidateFitGate.IsOpen`: the count line "{n} vacatures passen goed" (only when n > 0). Gate closed / E absent ⇒ link only. Gate off ⇒ nothing, including no count.

## 3. Courses (D5) — Dependency B
- **B present:** `CareerStepCourses.razor` wraps `CourseSuggestionBlock` with the step context (the step's course names and gaps as the match input), h2 "Opleiding die past":
  - Free first ("Gratis" success pill), max 1 partner ("Partnerlink" outline pill, affiliate code, `rel="sponsored noopener noreferrer"`, `target="_blank"`) — all per paspoort 03.
  - Each card: title, type · duration · form · provider, a "why" line with the claw "Laat je klauw ‘{gap}’ groeien".
  - Disclosure under the block: "Gratis staat altijd bovenaan. Partnerlink: Lobsy kan een vergoeding krijgen."
  - No curated free match and no partner ⇒ **hide the block**; only the claw text stays.
  - Never call anything that seeds data (`EnsureDefaultsAsync`) from this page.
- **B absent:** no course block. Under the gaps a plain line "Wat kan helpen: {course names, comma separated}" (not clickable, no provider). `TODO(paspoort-03)` at the seam in `CareerStepCourses.razor`. `TrainingOffersBlock` is **not** used here.
- AI course names are never links (both cases).

## 4. Footer actions
- Desktop footer (active step): text link "Heb je dit al? Voeg bewijs toe" (file icon; href per Dependency F with the first missing diploma/course name prefilled) · primary **"Deze stap is klaar"** (check icon).
- Mobile sticky footer: icon button "Bewijs toevoegen" (`aria-label`) + primary "Deze stap is klaar".
- "Deze stap is klaar" completes directly (D2: no proof needed). No confirm dialog; undo is the safety net (§6).
- Done step detail: secondary "Toch nog niet klaar" only when it is the last completed step (D9); otherwise nothing.

## 5. Growth moment (cr-d4 / cr-m4) — replaces the toast
- After a successful complete, the card switches to `CareerStepDoneCard` (no toast, remove the `LobsyToast` use at L370):
  - eyebrow "De klim · stap {n} van {total} klaar"; the fallen old-shell shard with "Oude schaal" (desktop only)
  - h1 **"Je nieuwe schaal past"**, lead "{step} is gehaald. Je oude schaal was te krap. Je bent weer gegroeid."
  - "Gained" list (max 3, only true items):
    - "{proof name} staat in je paspoort" (only when a matching certificate exists)
    - "Je klauw ‘{gap}’ is gegroeid" (gaps now covered)
    - "Nieuw: {n} vacatures als {short title} passen bij je" (only gate on + E present + fit gate open + n > 0)
    - Nothing true ⇒ just "Stap {n} staat als gehaald in je plan."
  - The stepper, advanced.
  - "Je volgende steen": "Stap {n+1}: {title}" + "Nog {gaps} klauwen · {courses} opleidingen" (courses only with B).
  - Footer: text "Toch nog niet klaar" (undo icon) · primary "Op naar stap {n+1}" (→ the next step detail). Last step: primary "Bekijk je droombaan" (→ overview goal reached, 02 §5).
- **Scene:** `Celebrate = true`. The lobster is 170/86 px with `newShell` (gold rim) and one `fallingPlate` (1.2 s, once), then moves to the next stone on the next render. Brighter light rays. Bubble "Voel je dat? Mijn oude schaal was te krap. Deze nieuwe past precies." / mobile "Voel je dat? Deze nieuwe schaal past precies."
- **Reduced motion:** no fall, no move animation; the new shell and the new position are simply there.
- **Screen readers:** one `aria-live="polite"` region announces "Stap {n} is klaar. Je nieuwe schaal past." Focus moves to the done card's h1 (no black box, §0).
- The moment shows once per completion (reload ⇒ normal detail/overview).

## 6. Undo
- "Toch nog niet klaar" calls uncomplete (01 §3). Success ⇒ back to the step detail as active, live region "Stap {n} staat weer open.", the lobster back on the previous stone (no fall animation).
- 409 `undo_last_first` ⇒ `CareerErr.UndoLastFirst` in the card.
- Undo never touches the paspoort (proofs stay).

## Tests
- bUnit:
  - detail for active/done/todo; gaps present/missing and "Toon alles"; `YearsText` hidden for 0
  - band pill for Good/Fair/NotYet; none for Unknown + test link
  - vacancy link hidden with the gate off; count only with E + fit gate open + n > 0 (use a fake)
  - B present (fake `CourseSuggestionBlock` input: free first, max 1 partner, `rel="sponsored noopener noreferrer"`, disclosure) and B absent (plain text, no `<a>` around course names)
  - completion: done card shown, no toast, live region text, focus on h1; gained list only true items; last step ⇒ "Bekijk je droombaan"
  - undo only on the last completed; reduced-motion class disables the fall (CSS contract: `.career-scene--celebrate .journey-plate--falling` has `animation: none` under `prefers-reduced-motion`)
- API: completing a non-active step from the UI path returns 409 and the UI shows the key text.
- Guard tests from 02 cover the new components.

## Success criteria
- Matches `cr-d3`, `cr-d4`, `cr-m3`, `cr-m4` (nav excepted; §0 differences: vacancy lines only with Werkgevers ON).
- No percentage anywhere on the step; no "0 jaar"; no clickable AI course names.
- Completion is the scene moment, not a toast; undo works and is announced.
- With Werkgevers OFF: no vacancy link, count or "Nieuw: … vacatures" line.
