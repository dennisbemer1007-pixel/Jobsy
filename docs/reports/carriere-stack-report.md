# Carrière stack end report (01–06)

Stack: candidate career redesign (`/tmp/carriere-01` … `06`). Base: `origin/acceptatie`, every file stacked on the previous branch. No merges, no deploys, nothing pushed to `main` or `acceptatie`. The candidate nav (`RoleNavCatalog`) was never touched.

This report is meant to be the last thing you need to read about this stack.

## File → branch → PR → status

| File | Branch | Tip SHA | PR | Status | Ids closed |
|---|---|---|---|---|---|
| 01 API & regels | `cursor/carriere-1` | `6cf67aaf` | [#478](https://github.com/dennisbemer1007-pixel/Jobsy/pull/478) | Done | B1 (archive), B3 (server idempotency), B4, B5, B6 (band), B7, B10 (suggestions), B12 (error codes) |
| 02 Reis-overzicht & droombaan | `cursor/carriere-2` | `047a7200` | [#479](https://github.com/dennisbemer1007-pixel/Jobsy/pull/479) | Done | B1 (dialog), B2, B3 (UI), B6/B10/B11/B13 (UI + copy) |
| 03 Stapdetail & groeimoment | `cursor/carriere-3` | `fc6ba1cb` | [#480](https://github.com/dennisbemer1007-pixel/Jobsy/pull/480) | Done | B6 (step), B7, B8, B9, B13 (step copy), B15 |
| 04 Contactverzoeken | `cursor/carriere-4` | `0b325502` | [#481](https://github.com/dennisbemer1007-pixel/Jobsy/pull/481) | Done | T1–T5 |
| 05 Hoe werkt Lobsy | `cursor/carriere-5` | `5bccdab9` | [#482](https://github.com/dennisbemer1007-pixel/Jobsy/pull/482) | Done | H1–H3, B14 (this page) |
| 06 E2E, cleanup, docs | `cursor/carriere-6` | this branch | no PR (as instructed) | Done | — (proves 01–05) |

## Dependencies A–G

Checked before 01 and re-checked before 02, 03 and 04 against `origin/acceptatie`. **No case ever flipped**, so no fallback was needed.

| Dep | Case | Outcome / seam |
|---|---|---|
| A Ontdekkingsreis scene + lobster + tokens | **Present** | `JourneyLobster` and the sea tokens from `features/ontdekkingsreis.css` reused. `CareerClimbScene` is new (02) and uses only those tokens. **No** `journey-tokens.css` fallback was created. |
| A2 `LobsyBubble` | **Present** | Reused with `Variant="Scene"`. |
| B Courses (paspoort 03) | **Present** | 03 uses the course block with the step context. One seam remains: `TODO(paspoort-03)` in `CareerStepCourses` for the slot rules paspoort 03 will own. |
| C Paspoort carrière tab + `CareerPlanViewBuilder` | **Present** | `CareerPlanViewBuilder` extended with archive / band / action fields; the paspoort carrière tab and its tests stayed green. `GrowingShellsStepper` was built in 02 because the paspoort stepper was not there yet — paspoort 04.2 should reuse it instead of building a second one. `CareerFitBandRules` was **not** needed; the existing band rules were reused. |
| D `IFeatureFlags` / `RequiresFeature` | **Present** | Every gate goes through `IFeatureFlags.IsEnabled(PlatformFeature.Employers)`. The `ICareerEmployerGate` fallback seam was **not** created. |
| E Kandidaat-banen fit | **Present** | Step vacancy link carries the count only when the fit gate is open; career band stays about the occupation, never mixed with vacancy bands. |
| F Bewijzen (paspoort 05) | **Present** | "Voeg bewijs toe" opens the paspoort Bewijzen tab prefilled (`?tab=proof&add=certificate&name=…`). |
| G Nav add-on | never edited | The five stones read their own order (`CandidateHowStones`), not the nav. |

## E2E: what ran, what soft-skipped

`Jobsy.Tests/CarrierePlaywrightTests.cs`, `[Collection("PlaywrightSmoke")]`, Chromium, desktop 1440×900 and mobile 390×844. Without `JOBSY_E2E_BASE_URL` every flow returns early (same contract as `Acc2709PlaywrightTests`), so CI stays green without a server.

Run against a local Development stack (API `:5200`, Web `:5201`, freshly migrated database): **18 of 18 cases passed**.

| Flow | Test | Ran |
|---|---|---|
| 1 Empty → choose → overview | `F1_empty_state_choose_dream_and_land_on_the_overview` | Yes on a fresh database; soft-skips once the e2e candidate already has a plan (there is no "delete plan" in the UI). |
| 2 Step → complete → moment → undo | `F2_step_detail_completes_into_the_growth_moment_and_undoes` | Yes |
| 3 Order rule (no complete button on a later step) | `F3_a_later_step_has_no_complete_button` | Yes, for the UI half. The `409 complete_previous_first` half cannot run from the browser (the API only accepts server-minted tokens via `JobsyApiAuthHandler`) and is covered by `CandidateCareerPlanApiTests`. |
| 4 Change dream + restore | `F4_change_dream_keeps_progress_and_the_old_plan_can_be_restored` | Yes. Closing uses the dialog's close button, not Escape, and the flow soft-skips when the candidate has used up the five generations of the day — see *Findings*. |
| 5 No self-claim | `F5_the_removed_course_claim_endpoint_is_gone_from_the_ui` | UI half yes; the `410` + untouched certificates half is an API assertion in `CandidateCareerPlanApiTests`. |
| 6 Talent contacts (mobile + desktop) | `F6_talent_contacts_asks_before_sharing_on_mobile_and_desktop` | Partly: the page, the share question and the decline state run. Creating a Pending request needs an employer seed account; set `JOBSY_E2E_EMPLOYER_EMAIL` to include it, otherwise that part soft-skips and `TalentContacts04BunitTests` covers it. |
| 7 Hoe werkt Lobsy + employer never reaches it | `F7_how_it_works_shows_the_stones_in_order_and_stays_on_the_page`, `F7b_an_employer_never_reaches_the_candidate_how_to_guide` | Yes |
| 8 Five languages incl. `ar` RTL | `F8_every_language_renders_all_three_pages_without_gaps` (theory × 5) | Yes — no missing-key markers, no horizontal overflow at 390, `dir=rtl` and a mirrored scene in `ar`. |
| 9 Reduced motion | `F9_reduced_motion_shows_the_new_shell_without_animation` | Yes |
| 10 Werkgevers OFF | `F10_werkgevers_off_hides_every_vacancy_link` | Soft-skipped: toggling a platform feature needs `JOBSY_E2E_ALLOW_FEATURE_TOGGLE=1` on localhost plus an admin account. The gated behaviour is covered by bUnit with the feature fake (02/03/04/05 suites). |
| 11 Focus (mouse vs Tab) | `F11_mouse_navigation_never_draws_a_focus_box_but_tab_does` (theory × 3 pages) | Yes, see *Findings* for how mouse focus is reproduced. |

Screenshots are written to `artifacts/e2e/carriere/` (git-ignored): `f1-*`, `f2-*`, `f4-*`, `f6-*` (390 + 1440), `f7-*`, `f8-*` for every page × language at 390 plus `nl` and `ar` at 1440, `f9-*`. Compare them with the `cr-*` mockups.

## Findings from the E2E work

1. **The shared dialog has no Escape handler.** `LobsyFriendlyDialog` closes on the × button and on the backdrop, but not on Escape, so flow 4 uses the close button. Adding Escape (and a focus trap) touches every dialog in the app and belongs in its own change — **deferred**.
2. **Chromium treats the h1 focus after navigation as keyboard focus.** `.journey-page h1:focus:not(:focus-visible)` is correct CSS, but a programmatic `focus()` matches `:focus-visible` in Chromium, so the ring can still appear when you land on the page from a full reload. Real mouse navigation inside the app does not focus the h1 at all, and a mouse click on the h1 gives `:focus` without `:focus-visible` and no ring — that is what flow 11 asserts.
3. **Pre-existing migration bug, fixed here.** `20260930130923_AddOneTimeLinks` repeated `Users.SchoolId` and `PersonalDataAccessLogs.SubjectPupilCodeId`, which the earlier `AddScholenFoundation` already creates, so **every fresh database failed to migrate**. Fixed on this branch (not a carrière change, but nothing could be tested without it).
4. **The language selector writes the profile language.** Walking five languages left the shared seed candidate in Polish for the next test, so the career E2E helper normalises the account back to Dutch on login and flow 8 restores Dutch in a `finally`.
5. **The archive sits under the bottom nav.** On the overview the *Eerdere plannen* disclosure is the last element, and the fixed bottom nav keeps covering it however far the page scrolls, so a mouse click on it can be intercepted. Flow 4 falls back to a scripted click; the keyboard path works. Worth a small padding fix on the career page — **deferred**.
6. **The generation limit is real and the E2E respects it.** After five plans in 24 hours the dream change answers `429 generation_limit` and the page says "Je kunt morgen weer een nieuw plan maken.". Flow 4 recognises that message and soft-skips instead of failing, so a repeated run on the same account stays honest.
7. **Pre-existing UAT gap:** `UAT-0137` (`/register/activate`) fails on the baseline too — that route has no Blazor page. Out of scope for this stack.
8. **Pre-existing red tests outside this stack.** The non-Playwright suite has **55 failures on this branch and exactly the same 55 on the parent commit** (`5bccdab9`), in e-mail copy, nav feature flags and werkgever APIs. This stack added 14 passing tests and broke none. The required gate — `dotnet build Jobsy.sln` plus `--filter "FullyQualifiedName~Career|CarrierePlaywright|LocalizationParity|RoutesDoc|AssetVersion|CareerDesign"` — is **215/215 green** with a live server.

## Cleanup

| Removed | Why |
|---|---|
| The whole `/* Career dashboard: Horizon layout */` block in `app.css` (`.career-dash*`, `.horizon-*`, `.lobsy-toast*`, `horizon-shimmer`/`horizon-fade-in` keyframes, ~690 lines) | The Horizon layout, its skeleton and the toast are gone since 02/03. `.career-dash-link-row` stays: `TestDetail.razor` still uses it. |
| `Jobsy.Web/Components/LobsyToast.razor` | Orphaned after 03 replaced the completion toast with the done card. |
| `.career-dash` / `.horizon-card` / `.horizon-stepper__node` / `.horizon-card__cta` in the PWA motion and overscroll blocks | Those selectors no longer exist. |
| `CareerDash.StepperBasis`, `CareerDash.StepperNow` (all five languages) | The only `CareerDash.*` keys with no remaining reader. The other 72 `CareerDash.*` / `Talent.*` keys are still used (paspoort carrière tab, SEO catalog, profile mini-section) and were kept. |

Regenerated the way the repo does: `tools/css/build.sh` → `app.min.css` (303 263 bytes), `App.razor` bumped to `?v=20260930-carriere6`, `Jobsy.Tests/update-asset-versions.py` → `asset-versions.json`.

**Kept on purpose:** `POST api/me/career-path/courses/claim` still answers `410 use_passport_proof` so old clients get a clear error, with a `// remove after 2026-10-30` comment above it. Removing it is a later task.

No dead `HorizonArt`, `horizon-*` or `career-dash` references remain outside the guard tests that assert their absence. There is no old JS confirm function left (02 removed it).

## Strings for native review

`docs/i18n/carriere-review.csv` — 250 keys with the Dutch source and the en/pl/ro/ar text:

| Source file | Keys |
|---|---|
| `UiStringsCareer.cs` (`Career.*`, `CareerStep.*`, `CareerDream.*`, `CareerErr.*`) | 153 |
| `UiStringsTalentCandidate.cs` (`TalentC.*`) | 47 |
| `UiStringsHowLobsyCandidate.cs` (`HowC.*`) | 50 |

Please have **pl / ro / ar** checked by native speakers. Placeholders (`{0}`) and the word *Lobsy* stay untranslated; Dutch job titles stay Dutch (decision D12).

`LocalizationParityReportTests` is green and `docs/i18n/untranslated-baseline.txt` did not grow (still 9 lines).

## Decisions to confirm

| Id | Decision | Where it lives now |
|---|---|---|
| **D8** | No feature flag for this redesign — one big switch, the old `/carriere` is gone in one go. | The old page and its CSS are deleted; there is no toggle back. |
| **D10** | At most **5 plan generations per 24 hours** per candidate. | `CareerGenerationGuard.DailyLimit = 5` (constant, flagged for an admin setting later). Plus a 3-minute in-flight lock and a 10-minute idempotency window so a double click is one generation. |
| **D12** | Job titles stay **Dutch in every language**; catalog size as is. | The dream catalog and the plan titles; the review CSV keeps them Dutch on purpose. |
| **D14** | **No revoke after sharing**; the employer label for "al voorzien" is "Al voorzien". | `TalentContactDeclineReasons`; the employer talent-pool section. |

## Deferred

- A global h1 focus rule (and an Escape handler plus focus trap for `LobsyFriendlyDialog`); see *Findings* 1 and 2.
- Removing the `courses/claim` 410 stub (after 2026-10-30) and the `MatchPercent` column.
- A translated gloss for Dutch job titles (D12).
- A revoke flow for contact details that were already shared (D14).
- An admin setting for the generation limit (D10) instead of the constant.
- E2E that soft-skipped: Werkgevers OFF (needs an admin toggle on localhost), the employer-created Pending contact request (needs an employer seed account), and the direct API assertions (409 / 410) that the browser cannot make.

## Measured

| Thing | Value |
|---|---|
| AI generations during the E2E run | **2** — one per explicit click ("Maak mijn plan" in flow 1, "Maak nieuw plan" in flow 4), confirmed in `CandidateCareerGenerations`. Both flows assert the button disables on the first click, and the server-side 3-minute in-flight lock plus the 10-minute idempotency window make a repeat click a no-op. Restoring an archived plan costs no generation. |
| `carriere.css` unminified | **26 959 bytes** — just over the 25 kB soft target from the plan. |
| `carriere.css` gzipped | 5 238 bytes (what the browser actually downloads). |
| Budget guard | `CareerDesignGuardTests.Career_stylesheet_stays_within_its_page_weight_budget` fails above 28 kB, so the file cannot creep further without a deliberate split. |
| `app.min.css` after cleanup | 303 263 bytes (the Horizon block is gone). |
