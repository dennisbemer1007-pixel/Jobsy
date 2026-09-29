# 07 · De ontdekkingsreis, part 2: journey shell, zones, lobster, progress, state migration, Start + steps 1–6 + consent

> Read `00-README.md` first: the §0 rules apply, plus §F (the paspoort flag gates the journey, D11). Only start this file once the previous file's PR is open and green.

| | |
|---|---|
| Branch | `cursor/ontdekkingsreis-2`, created from `cursor/ontdekkingsreis-1` |
| PR | ONE PR into `acceptatie`, titled `feat(ontdekkingsreis): journey shell and steps 1–6`. The body starts with `Stacked on #<PR of 06> (cursor/ontdekkingsreis-1)` |
| Mockups | `docs/mockups/ontdekkingsreis/`: `or-d1-start`, `or-d2-over-jou`, `or-d3-werk`, `or-d4-waar-houd-ik-niet-van`, `or-m1-start`, `or-m2-wanneer-en-hoe`. **Read `build.py` there too**: it has the plate SVG paths, the scene per depth, the derived sea tokens and the copy. |

> Size note: if the diff grows past ~1,500 lines, split it into **07a** (`cursor/ontdekkingsreis-2a`: the shell, state migration, scene/lobster/progress, Start, steps 1–2) and **07b** (`cursor/ontdekkingsreis-2b`, stacked on 07a: steps 3–6, consent, the transitional test steps). Each is ONE PR into `acceptatie` with a stacked-on line.

## Goal
"De ontdekkingsreis" **replaces** the onboarding wizard (`/candidate/start`, `OnboardingWizard.razor`, v2) when the paspoort flag is ON:
- The candidate walks from the beach into the deep, and the lobster sheds one shell plate per step (10 in total).
- Everything is saved as they go, and they can stop at any step.
- With the flag OFF, the old wizard is untouched (D11).

## 07.1 Routes, gating, reuse of the old wizard
- **New page** `Components/Pages/Candidate/DiscoveryJourney.razor`:
  - `@page "/candidate/ontdekkingsreis"`, `[Authorize(Roles = "Candidate")]`
  - `[RequiresFeature(CandidatePassport, FallbackPath = "/candidate/start")]`
  - Step via `?stap=start|1…10|toestemming|klaar`. This file builds up to 10 (see 07.6); `klaar` comes in 08.
- **Flag OFF:** `/candidate/ontdekkingsreis` redirects to `/candidate/start`, and the old wizard runs exactly as today.
- **Flag ON:** `/candidate/start` redirects to `/candidate/ontdekkingsreis` (query kept). Do this at the top of `OnboardingWizard.OnInitializedAsync`, the same pattern as 02.1.
  - That way every existing link keeps working: `AuthRedirects.CandidateHowToPath`, `CandidateOnboardingResumeCard`, `MatchUnlockPanel`, `GratisDna.razor` after signup, and the `DeviceSessionService` first-login landing.
  - `MainLayout` (~L299) treats `candidate/start` as a focus page. Add `candidate/ontdekkingsreis` there too: no bottom nav, no footer during the journey, the same as today's wizard.
- **Extract, don't copy.** The old wizard's steps are inline `case` blocks in `OnboardingWizard.razor` (~L188–531: 1 persoonlijk, 2 beschikbaarheid, 3 vervoer, 4 werk, 5 opleiding, 6 droombaan, consent at 7). Move each into a component under `Components/Candidate/Onboarding/`:
  - `PersonalStep`, `AvailabilityStep`, `TransportStep`, `WorkHistoryStep`, `EducationStep`, `DreamJobStep`, `TestConsentStep`
  - They share one `OnboardingProfileDraft` state and the existing save calls: `Api.UpdateMyProfileAsync` via `SaveProfileAsync`, `SaveOnboardingDreamJobAsync`, `AcceptTestAiConsentAsync`.
  - The old wizard renders the same components with **identical markup and classes**, so the existing onboarding tests and Playwright flows stay green.
  - The journey composes them into its new steps and adds the new parts from 06.
  - Keep the mini-test logic (`LoadMiniStepAsync` ~L1200–1281: locked IDs from already-answered questions, the GratisDna merge, `QuestionnaireAutosave`) in one extracted `OnboardingMiniTest` component. **The skip logic is reused as is**: questions already answered, including those merged from the free no-account DNA scan by `GratisDnaMergeService.TryMergeAsync()`, are skipped. A test step with nothing left auto-advances, like `TryAutoAdvanceEmptyMiniStepAsync`.
- **Don't touch** `QuestionnaireShell.razor` or `questionnaire.css` (§0). Reuse is fine.

## 07.2 State: wizard version 3, safe migration
`CandidateOnboarding` already has `WizardVersion` (1 → 2 mapped on read via `OnboardingWizardCatalog.MapV1Step`). Add version **3**:
- In `OnboardingWizardCatalog`:
  - `WizardVersionV3 = 3`
  - `MapV2ToV3(int)`: 1→1, 2→2, 3→2, 4→3, 5→4, 6→4, 7→7, 8→8, 9→9, 10→10
  - `MapV3ToV2(int)`: 1→1, 2→2, 3→4, 4→5, 5→6, 6→6, 7→7, 8→8, 9→9, 10→10
  - v1 goes through `MapV1Step` first
  - `V3StepCount = 10`, `V3RemainingMinutes` (total ≈ 12 min, per the mockup "10 stappen · ± 12 minuten")
- **`CandidateOnboardingService`:**
  - Flag ON: an **incomplete** v1/v2 row is mapped to v3 on read and saved as v3 on the next progress save.
  - Flag OFF: a v3 row is mapped back with `MapV3ToV2` on read, so switching the flag off mid-journey never breaks the old wizard.
  - `FinishReached` from v2 maps to "at the end screen" (08).
  - Completed rows (`CompletedAtUtc` set) are never reopened.
- **Answers are never touched by the migration.** They live in the profile and test entities; only the step pointer moves.
- **`StepEvent` analytics:** add an optional `int? WizardVersion` to each event, so dropout analysis can tell versions apart. Old events without it read as their row's version.
- **Save per step:** use the existing `SaveMyOnboardingProgressAsync(step, stepCompleted/stepSkipped)` API. No new endpoint. The API accepts step 1–10 for v3; validate against the row's version.

## 07.3 The scene: zones, background, lobster
- **Zones (from `build.py` `STEPS`/`ZONES`):**

  | Screen | Zone (eyebrow) | Depth |
  |---|---|---|
  | Start | "Het strand" | 0 |
  | Steps 1–2 | "Aan de kust" | 1–2 |
  | Steps 3–6 | "Tussen de rotsen" | 3–6; step 6 adds the "calm spot" between stones around the lobster |
  | Steps 7–10 | "In de diepte" | 7–10 |
  | End (08) | "Naar het licht" | 11 |

- **Background:**
  - The component is `JourneyScene.razor` (depth, desktop/mobile): a CSS background + one inline SVG layer (sun and waves, sand, rocks, seaweed, light rays, specks and bubbles), `aria-hidden="true"`, built from `build.py` `scene()`.
  - **Colours:** only the derived scene tokens `--sea-0…9`, `--sky`, `--sand`, `--sun`, `--rock`, `--weed`, `--shell-old`. They are defined **in `features/ontdekkingsreis.css`** (not in `app.css` `:root`) as `color-mix()` of existing tokens, exactly as in `build.py`.
  - **Design exception (D12):** the scene layer may use `linear-gradient` (per depth, `GRAD` in `build.py`), because it is illustration, not UI. Cards, buttons and text stay flat and follow the design system.
  - Mobile: the scene is a 170 px band under the header. Desktop: the full stage behind the cards.
- **Lobster:**
  - The component is `JourneyLobster.razor` (`plates shed` 0–10, `newShell`, `fallingPlate?`, `size`, `label`).
  - It uses the **current mascot** (`mascot-256.webp` as an SVG `<image>`) with the **10 SVG shell plates** layered on top, in `build.py` `PLATES` order (left claw, right claw, tail tip, 3 tail segments, 2 chest, 2 helmet halves), plus the cracks while fewer than 6 are shed.
  - Plates shed = completed counted steps. Size grows per step: desktop 120 + 10 px per step, mobile 60 + 4 px.
  - Each step has one mascot bubble, from `build.py` `SAY` (the NL text is final; keys `Discovery.Say.0…11`).
  - `role="img"`, `aria-label` "Lobsy de kreeft".
  - Real artwork comes later. Keep the plate layer behind a clean component API so it can be swapped.
- **Motion:**
  - bubbles rise slowly
  - the lobster bobs gently
  - a plate falls once, in 1.2 s (used in 08)
  - `@media (prefers-reduced-motion: reduce)` stops all of it; the static state is complete.
  - No motion on the form cards.

## 07.4 Progress, saving, "Later verder"
- **Desktop rail** (left card "Jouw reis"): an `<ol>` route, `aria-label` "Jouw reis, van het strand naar de diepte".
  - It lists: Het strand, then the zone labels (`aria-hidden`), then steps 1–10 with title + sub, then "Naar het licht".
  - States: done = check + "Laag {n} eraf"; current = a small lobster marker + `aria-current="step"`; todo = number.
  - Under it: "**Stap {x} van 10**" (at the start: "10 stappen · ± 12 minuten"), a 10-segment shell bar "**{n} van 10 lagen eraf**", and "**Alles is bewaard. Stoppen mag altijd.**"
- **Mobile:** a top bar "Stap {x} van 10 · {titel}" with a "Bewaard" check, and a 10-segment track (stepped `--sea-*` fills) with the lobster marker at the current position.
- **"Alles is bewaard" is honest:**
  - It shows only after the last autosave/PUT succeeded.
  - While saving: "Bewaren…".
  - On error: `--warn` "Niet bewaard. We proberen het opnieuw." (retry with backoff) and the primary button stays enabled.
- **Footer on every step:** "Terug" (not on step 1), then "Overslaan" where a step is skippable (6, and the optional parts), then **"Later verder"**, then the primary "Volgende".
  - "Later verder" saves and goes to `/candidate/paspoort`, where the existing resume card (`CandidateOnboardingResumeCard`, now pointing to `/candidate/ontdekkingsreis`) invites them back.
- **Focus:** on each step change, focus moves to the step `h1` (the existing `tabindex="-1"` pattern). The step change is announced via `aria-live="polite"` ("Stap 3 van 10: Werk").

## 07.5 Screens (NL copy final; the mockups show the layout)
- **Start** (`or-d1`, `or-m1`):
  - Eyebrow "De ontdekkingsreis", h1 "Hoi {voornaam}, fijn dat je er bent".
  - Lead: "Samen ontdekken we wie je bent en welk werk bij je past. Laag voor laag. Aan het eind staat alles in je paspoort."
  - 3 zone rows (Aan de kust 2 min, Tussen de rotsen 4 min, In de diepte 5 min).
  - 3 hints: "Er zijn geen foute antwoorden…", "Alles wordt meteen bewaard…", "Liever een andere taal? Kies je taal rechtsboven."
  - CTA "Begin de reis".
  - With Werkgevers actief OFF, the lead drops "en welk werk bij je past" (key variant).
- **1 · Over jou** (`or-d2`): `PersonalStep`, which covers first/last name (prefilled from the account), postcode (hint "alleen voor reistijd, werkgevers zien je adres niet"), birth date, phone (optional) and WhatsApp.
  - The WhatsApp line and the employer wording are **hidden when Werkgevers actief is OFF**.
  - The birth-date hint, OFF variant: "We gebruiken je leeftijd alleen om te kijken of je toestemming van een ouder nodig hebt."
  - Keep today's under-16 behaviour exactly.
- **2 · Wanneer en hoe** (`or-m2`): `AvailabilityStep` (the presets + "Zo hebben we het ingevuld · Aanpassen" summary), `TransportStep` (transport chips + max travel chips) and **Rijbewijs** (the existing `DrivingLicenses` chips, which move here from the profile-only form).
- **3 · Werk** (`or-d3`): `WorkHistoryStep` (employers, "Ik heb nog niet gewerkt"), then the **new "Wat voor werkgever zoek ik?"** (`EmployerPreferences` chips, "Meer kiezen mag"), then a collapsed `<details>` "In welk werkveld wil ik werken?" (existing `Roles`).
  - With Werkgevers actief OFF, "Wat voor werkgever zoek ik?" stays. It is self-knowledge, and it becomes useful when employers return.
- **4 · Leren:**
  - `EducationStep` (level + direction)
  - **Certificaten** (the existing `Certificates`, name + year)
  - `DreamJobStep` retitled "**Waar wil ik naartoe?**"
  - **new** "Wat wil ik nog leren?" (`LearningGoals`, chips + free text)
  - **new** "Talen die ik spreek" (`SpokenLanguages`)
  - **new** "Niveau Nederlands" (`DutchLevel`, 5 plain rows)
  - It's long, so the dream-job and learning parts are open by default, and certificates and languages are `<details>` with a summary line of what is filled.
- **5 · Wat ik leuk vind:** **new** `Hobbies` chips + "Iets anders", then an optional "Over mij" (the existing `AboutMe`, `textarea`, with the counter the profile uses). Bubble: "Waar word je blij van? Daar zit vaak je kracht."
- **6 · Waar houd ik niet van** (`or-d4`):
  - The whole step is **skippable**, with a "Overslaan mag" badge in the eyebrow.
  - Chips + "Iets anders", saved via `api/me/private-preferences` (06).
  - Always-visible note with an eye-off icon: "**Alleen voor jou en je matches. Werkgevers zien dit niet.**"
  - The scene shows the calm spot between the stones.
- **Toestemming** (uncounted, no plate): `TestConsentStep`, exactly today's consent (texts, the "Meer uitleg" details, private note). It shows only when `TestAiConsentAt` is null, between step 6 and 7. Declining keeps them on the journey: they can do 1–6, stop there, and come back to it via "Verder ontdekken" (08).

## 07.6 Steps 7–10 in this file (transitional until 08)
Render the extracted `OnboardingMiniTest` plainly for Competenties, Beroepen, Cultuur and Waarden:
- 5 questions each, using the existing `OnboardingWizardCatalog.*QuestionIds`
- already-answered questions are skipped
- answers go to the existing test entities through the existing save calls (`SaveMyCompetenciesAsync`, etc., `completed: false`)
- the journey layout, and a test tab strip "Competenties · Beroepen · Cultuur · Waarden"

After step 10 the journey calls the existing `CompleteMyOnboardingAsync` and goes to `/candidate/paspoort`. **08 replaces** this with the deeper choice, the shed moment and the end screen. Keep the transitional code in one method so 08 can swap it cleanly.

## 07.7 Strings and CSS
- **Strings:** `Discovery.*` for the journey copy, `Discovery.Say.*`, `Discovery.Zone.*`, `Discovery.Rail.*`, `Discovery.Saved*`, `Discovery.Later`, `Discovery.Step{n}.*`. In nl/en/pl/ro/ar, in `UiStringsDiscovery.cs`.
- **CSS:**
  - `wwwroot/css/features/ontdekkingsreis.css`, BEM `journey-…`
  - linked in `App.razor` (+ `<noscript>`) with `?v=`
  - added to `asset-versions.json`
  - Layout: 3 columns on desktop (rail 300 px · step card · lobster zone); a single column on mobile with the scene band.
  - The step card uses the existing card and field classes. Tap targets ≥ 44 px, RTL-safe (the route and the chevrons flip).

## Tests
- **Catalog:** `MapV2ToV3`/`MapV3ToV2` table tests (every step, round trip where defined), v1 → v2 → v3, and the remaining minutes.
- **Service:**
  - incomplete v1/v2 rows become v3 with the flag ON and the answers untouched
  - v3 rows read as v2 with the flag OFF
  - completed rows are unchanged
  - `StepEvent` carries the version
- **Routes:**
  - Flag ON: `/candidate/start?x=1` → `/candidate/ontdekkingsreis?x=1`.
  - Flag OFF: `/candidate/ontdekkingsreis` → `/candidate/start`, and the old wizard renders.
  - `MainLayout` hides the bottom nav on the journey.
- **Extraction parity:** bUnit renders the old wizard steps with the same markup/classes before and after (selector snapshots); the existing onboarding tests stay green.
- **Skip logic:** with GratisDna answers merged for 3 of the 5 competency IDs, step 7 asks only 2; with all 5, it auto-advances.
- **bUnit (new):**
  - `JourneyLobster`: plate count per step, the cracks shown below 6, aria
  - `JourneyScene`: the depth class per step, `aria-hidden`
  - the rail states and texts
  - "Alles is bewaard" only after a successful save; the error state
  - "Later verder" saves, then navigates
  - step 6 skip + the private note
  - the WhatsApp line hidden when Werkgevers OFF
- **Playwright** (`OntdekkingsreisPlaywrightTests.cs`), flag ON:
  - walk Start → 6 → consent → 7 at 1440×900 and 390×844 with reduced motion
  - stop with "Later verder" at 3, re-login, and you resume at 3
  - no horizontal overflow on mobile

## Success criteria (all must hold before you open the PR)
- **Flag OFF:** `/candidate/start` and the old wizard behave exactly as before (all existing onboarding tests are green), and the journey route isn't reachable.
- **Flag ON:**
  - every old link lands in the journey
  - in-progress v1/v2 candidates resume at the mapped step with all answers kept
  - Start and steps 1–6 + consent match the mockups
  - every step saves, shows an honest "Alles is bewaard", and offers "Later verder"
  - steps 7–10 work transitionally and skip already-answered questions
- **Honest employer copy:** dislikes are private, and the employer wording is hidden when Werkgevers actief is OFF.
- **Reduced motion** gives a complete, still screen.
- **No duplicate step logic:** the old wizard and the journey share the extracted step components.
- `dotnet build` and `dotnet test` are green. Any existing test you changed has its reason in the PR body.
- The PR body has the stacked-on line, what and why, screenshots (Start, 1, 3, 6 at desktop + mobile; the old wizard with the flag OFF), the test list, and "Out of scope / deferred".

## Done → next
Push, open the PR and note its number. Then continue with **`08-ontdekkingsreis-tests-einde-nav.md`**. If anything above is red, stop and report (see 00-README "How to run" step 3).
