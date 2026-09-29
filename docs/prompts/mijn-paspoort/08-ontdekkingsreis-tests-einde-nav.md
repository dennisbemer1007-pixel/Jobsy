# 08 · De ontdekkingsreis, part 3: test steps 7–10, "Weer een laag eraf" + going deeper, end screen, nav slot, "Verder ontdekken", old onboarding redirect

> Read `00-README.md` first: the §0 rules apply, plus §F, and **§N (you fill the reserved Discovery slot)**. Only start this file once the previous file's PR is open and green.

| | |
|---|---|
| Branch | `cursor/ontdekkingsreis-3`, created from `cursor/ontdekkingsreis-2` (`cursor/ontdekkingsreis-2b` if 07 was split) |
| PR | ONE PR into `acceptatie`, titled `feat(ontdekkingsreis): tests, going deeper, end screen and nav`. The body starts with `Stacked on #<PR of 07> (cursor/ontdekkingsreis-2)` |
| Mockups | `docs/mockups/ontdekkingsreis/`: `or-d5-test-vraag`, `or-d6-laag-eraf`, `or-d7-einde-paspoort`, `or-m3-test-vraag`, `or-m4-einde` (+ `build.py`) |

## Goal
This file finishes the journey:
- The 4 tests go deeper step by step, and after each test there is a calm "Weer een laag eraf" moment where the candidate chooses how deep to go.
- The journey rises "naar het licht" with a first impression and "Bekijk je paspoort".
- It adds the **De ontdekkingsreis** nav item (§N slot 1). After the journey it stays as the place to look back and go deeper.

## 08.1 Test steps 7–10 (`or-d5`, `or-m3`)
This replaces the transitional code from 07.6.
- **Header:** eyebrow "In de diepte · stap {n} van 10" (mobile: "test {k} van 4"), then the test tab strip (done = check), then the h1 per test:
  - "Hoe werk jij?"
  - "Wat vind je leuk?"
  - "Waar voel je je thuis?"
  - "Wat vind je belangrijk?"
- Lead: "Hoe goed past deze zin bij jou? Er zijn geen foute antwoorden."
- **Question:** "Vraag {i} van {total}" + segment bar, the statement in quotes, and the 5-point Likert.
  - Reuse `OnboardingMiniTest` (07) and its existing radiogroup/labels ("Past niet" … "Past heel goed").
  - Answered questions of this test collapse into a small "Beantwoord" list (the existing `Collapsed` behaviour).
- **Hint** under the first test only: "Na 5 vragen kun je kiezen: klaar, of dieper met 10 of 25 vragen." For Cultuur the text says "10 of alle 18".
- Saving stays as it is: the existing save calls, with already-answered questions skipped and GratisDna answers counted.

## 08.2 Going deeper: question sets
In `OnboardingWizardCatalog`, add per test a **second set** of 5 (`…DeeperQuestionIds`) and "the rest" (= every catalog id):

| Test | Levels | Full count |
|---|---|---|
| Competenties | 5 / 10 / 25 | `CompetencyTestCatalog.QuestionCount` = 25 |
| Beroepen | 5 / 10 / 25 | 25 |
| Cultuur | 5 / 10 / **alle 18** | `CulturePersonalityCatalog.QuestionCount` = 18 |
| Waarden | 5 / 10 / 25 | 25 |

- **Choosing the second set:** take the next item per dimension, the same rule as the mini set (for Big Five, RIASEC, the culture dimensions and Schwartz, the lowest id per dimension not in the mini set). **Beroepen's second set must include Artistic.** Document the ids in XML comments and add a test that each set has no duplicates and only valid ids.
- **Levels are counted in answered questions,** not in sets. If the candidate already answered 12 competency items (for example from the full test page or GratisDna), "Iets dieper" is already reached: show it as done, and only offer "Heel diep".
- **Reaching the full count** saves with `completed: true`, exactly as finishing the test on its own page does. Mijn tests (03) then shows it as done.
- **Stopping at 10** saves with `completed: false`. Mijn tests shows "Nog {x} van {y} vragen".
- **The step number doesn't change** when going deeper ("Stap 7 van 10" stays). Only the question counter grows.

## 08.3 "Weer een laag eraf" moment (`or-d6`)
- **When:** after the last question of the chosen level of each test (7–10). It is **not a modal**: it replaces the step card, and it only moves on when the candidate clicks.
- **Content:**
  - Eyebrow "In de diepte · stap {n} van 10 klaar", with the tab strip showing the next test.
  - A fallen shell piece (`build.py` `SHARD`) with the label "Laag {n} van 10".
  - h1 "**Weer een laag eraf**", then "{Test} is klaar. Dit staat nu in je paspoort:".
  - Up to 3 lines from the **existing** `OnboardingImpressionComposer` for that test (top competencies / RIASEC / culture highlight / top value), marked "(voorlopig)" while provisional.
- **Fieldset "Wil je dieper in deze test?"** Sub: "Meer vragen geeft een scherper beeld. Het mag, het hoeft niet. Je kunt dit later ook nog doen."
  - It's a radiogroup of 3 cards with a 5-dot depth indicator:
    - "Zo laten": "5 vragen · klaar" (default)
    - "Iets dieper": "10 vragen · + 2 min"
    - "Heel diep": "25 vragen · + 6 min" (Cultuur: "alle 18 · + 4 min")
  - Levels that are already reached show as done and can't be selected. If the test is already complete, the fieldset is replaced by "Deze test heb je helemaal gedaan."
- **Footer:**
  - "Later verder"
  - primary "**Verder naar {volgende test}**" (after Waarden: "**Naar het licht**")
  - when a deeper level is selected, the primary reads "**Duik dieper**" and continues the same test
- **Lobster:**
  - The bubble reads "Voel je dat? Weer een laag eraf. Je wordt groter."
  - The plate for this step falls once (1.2 s) and the lobster grows.
  - With reduced motion there is no fall, only the new state and text.
  - `aria-live="polite"` announces "Laag {n} van 10 eraf".

## 08.4 End screen "Naar het licht" (`or-d7`, `or-m4`)
- **Call** the existing `CompleteMyOnboardingAsync` when the screen opens (idempotent). The route is `?stap=klaar`, and the scene is depth 11 (light rays, bubbles, the lobster rising with its **new shell**).
- **Rail:** everything done. "Klaar · 10 van 10 · nieuwe schaal".
- **Card:**
  - Eyebrow "Klaar · 10 van 10 lagen eraf", h1 "**Dit ben jij, {voornaam}**".
  - Lead: "Je oude schaal is eraf. Alles wat je vertelde staat nu in je paspoort. Werkgevers zien pas iets als jij dat wilt." With Werkgevers actief OFF, the last sentence is dropped.
- **Mini passport:** reuse the passport card from 02 as a compact `Preview` variant (banner with "LOBSY PASPOORT" + member number + dashed stamp, initials avatar, name). It has 3 facts with an "Eerste indruk" pill:
  - "Je sterkste punt"
  - "Werk dat bij je past"
  - "Belangrijk voor jou"
  - They come from the **existing** `CandidateOnboardingImpressionDto`. A missing test shows "Nog niet ontdekt". Never invent a value.
- **Hint:**
  - With Werkgevers ON and `MatchingVacancyCount > 0`: "{n} vacatures passen al bij je. Hoe dieper je duikt, hoe scherper je matches."
  - Otherwise: "Eerste indruk. Hoe dieper je duikt, hoe scherper het beeld."
  - Check that file 01's mixed-response rule (01.2d) also empties `MatchingVacancyCount`/`MatchCards` in the onboarding impression when Werkgevers is OFF. If 01 missed it, add it here, with a test.
- **Actions:** the secondary "Een test verdiepen" goes to `/candidate/ontdekkingsreis` (overview, 08.6). The primary "**Bekijk je paspoort**" goes to `/candidate/paspoort?tab=dna`.
- **Install and notifications:** the existing install/notifications prompt (`WizardPhase.Install` in the old wizard) is extracted into a component and shown as **one quiet secondary card** under the end card, with the same logic and texts. It is not an extra step.
- **Declined consent at 07.5:** the journey ends after step 6 with the variant "**Tot hier: 6 van 10 lagen eraf**" and the text "De tests doe je later, via De ontdekkingsreis." That variant also completes the onboarding. The nav item then offers the tests.

## 08.5 Nav: fill the Discovery slot (§N)
- `CandidateNavSlot.Discovery` becomes `NavItem("Nav.Discovery", "/candidate/ontdekkingsreis", NavIcons.Compass, ["/candidate/start"])`.
  - Add the new line icon `Compass` to `NavIcons`, in the same style.
  - It shows **only when the paspoort flag is ON** (the journey exists only then).
- **Label:** "De ontdekkingsreis". It's long, so add an optional `ShortTitleKey` to `NavItem` for widths below 640 px ("Reis"; also "Paspoort" for Mijn Paspoort). BottomNav uses it when set; other items are unchanged.
- **Resulting nav** (the §N table; max 5 holds):

  | Werkgevers actief | Nav |
  |---|---|
  | ON | De ontdekkingsreis · Mijn Paspoort · Zoeken · Sollicitaties · Carrière |
  | OFF | De ontdekkingsreis · Mijn Paspoort · Carrière |
  | Paspoort OFF | unchanged: today's nav (D1) |

  - The mockup's 6-item nav is **not** used (D8).
- **Focus mode:** `MainLayout` hides the nav only **while a journey is in progress** (steps, consent, the laag-eraf moment, the end). The overview (08.6) shows the normal nav with this item active.

## 08.6 "Verder ontdekken" (the nav target after completion)
`/candidate/ontdekkingsreis` without `?stap`:
- a **completed** onboarding (v1/v2/v3) shows the overview
- an incomplete one resumes the journey at the saved step

**Overview:**
- h1 "Verder ontdekken", with the lead "Kijk terug op je reis, of duik dieper." The scene is light (depth 11, static), and the lobster has its new shell.
- **Route list** of the 10 steps (+ consent if it's missing). Each row: title, status and an action.
  - Status: "Klaar", "Overgeslagen", or **"Nieuw · 1 min"** for the new parts that are still empty.
  - The new parts are step 3 werkgever voorkeuren, step 4 leren/talen/Nederlands, step 5 hobby's, step 6 niet leuk.
  - Candidates who finished the **old** wizard see those parts as "Nieuw". They are invited, never forced.
  - Action: "Bekijken" opens that step in **review mode**. It uses the same components, the footer reads "Opslaan" + "Terug naar overzicht", there is no plate animation, and the progress isn't changed.
- **Test rows:** the current depth ("10 van 25 vragen") plus the next levels as buttons ("Iets dieper", "Heel diep" / "Alle 18"), which open the test step at that level. When a test is complete, the row shows "Helemaal gedaan" + the link "Uitgebreid rapport" to Mijn tests (03), where the existing deep-analysis offer lives. There are no prices here.
- **Consent missing:** the row "Toestemming voor de tests" with "Nu geven" goes to the consent step.

## 08.7 Old onboarding: redirect, keep the code (D13)
- Make every entry point flag-aware through one helper, `OnboardingRoutes.StartPath(flags)`, which returns `/candidate/ontdekkingsreis` when ON and `/candidate/start` when OFF. That removes the extra hop from the 07 redirect. The entry points are:
  - `AuthRedirects.CandidateHowToPath` usages
  - `CandidateOnboardingResumeCard`
  - `MatchUnlockPanel` (3 links; localize its hardcoded Dutch on the way: "Verder met starten", "Start of hervat")
  - `GratisDna.razor` after signup
  - `DeviceSessionService` first-login landing
- The 07 redirect stays as a safety net.
- **Don't delete** `OnboardingWizard.razor` or its strings. It's still the flag-OFF path. Removing it is a later cleanup, once the paspoort flag is permanently ON. List it under "Out of scope / deferred".
- **Docs:** `docs/ROUTES.md`, `PageSeoCatalog` (private), `PageHelpDocs` (`/candidate/ontdekkingsreis`), `CHANGELOG.md`.

## 08.8 Strings
- `Discovery.Test.*` (titles, lead, hint, counter)
- `Discovery.Shed.*` (title, lead, deeper legend/sub, options, done states, CTAs)
- `Discovery.End.*` (title, lead + OFF variant, facts, hints, CTAs, the "Tot hier" variant)
- `Discovery.Overview.*`
- `Nav.Discovery`, `Nav.Discovery.Short`, `Nav.Passport.Short`
- the localized `MatchUnlockPanel` texts

All in nl/en/pl/ro/ar.

## Tests
- **Catalog:**
  - the deeper sets are valid and duplicate-free
  - Beroepen's second set includes Artistic
  - the levels and full counts per test (Cultuur 18)
  - the "already reached" logic from the answered counts
- **Saving:** the full level saves `completed: true`, and 10 saves `completed: false`. GratisDna-merged answers count toward the levels and are not asked again.
- **Shed moment (bUnit):**
  - the 3 options, with Cultuur "alle 18"
  - the default "Zo laten"
  - the done/disabled states
  - the "Duik dieper" label switch
  - the aria-live text
  - the reduced-motion class removes the fall
- **End (bUnit):**
  - the facts come from the impression DTO, with "Nog niet ontdekt" for a missing test
  - the vacancy hint only when Werkgevers ON and the count > 0
  - the lead variant when OFF
  - `CompleteMyOnboardingAsync` called once
  - the "Tot hier" variant after declined consent
- **Nav:**
  - `CandidateItems` for all 4 flag combinations: exact order, count ≤ 5, Discovery first when the paspoort flag is ON and absent when it's OFF
  - `ShortTitleKey` rendering below 640 px
  - active state on `/candidate/ontdekkingsreis` and `/candidate/start`
- **Overview:**
  - completed v2 users see the new parts as "Nieuw"
  - review mode saves without changing progress
  - the test depth buttons open the right level
  - the missing-consent row
- **Routes:** `OnboardingRoutes.StartPath` in both states; every entry point uses it (grep test: no hardcoded `/candidate/start` outside `OnboardingWizard.razor` and `OnboardingRoutes`).
- **Playwright,** flag ON, at 1440×900 and 390×844 with reduced motion:
  - the full journey Start → 10 → end → "Bekijk je paspoort"
  - "Heel diep" on Cultuur asks until 18, and Mijn tests shows it as done
  - after completion, the nav shows "De ontdekkingsreis" first, and the overview works
  - flag OFF: the old wizard is unchanged

## Success criteria (all must hold before you open the PR)
- **Flag ON:**
  - the journey runs from Start to the end screen
  - each test offers 5 / 10 / 25 (Cultuur 5 / 10 / 18) through the "Weer een laag eraf" moment
  - answers are saved in the existing test entities, and full depth completes the test
  - the end screen shows only real impression data and leads to the paspoort
- **Nav:** "De ontdekkingsreis" sits in the leftmost slot and stays after completion, leading to "Verder ontdekken". The nav never exceeds 5 items.
- **Flag OFF:** everything, including the old onboarding and today's nav, is exactly as before.
- **Werkgevers actief OFF:** no vacancy counts or employer wording on the end screen.
- **Reduced motion** is respected everywhere. There is no horizontal overflow at 390 px.
- `dotnet build` and `dotnet test` are green. Any existing test you changed has its reason in the PR body.
- The PR body has the stacked-on line, what and why, screenshots (a test question, the shed moment, the end, the overview, and the nav at desktop + mobile), the test list, and "Out of scope / deferred" (removing the old wizard, the real lobster artwork).

## Done → next
Push, open the PR and note its number. This is the **last file**: give the final report (00-README "How to run" step 4). If anything above is red, stop and report.
