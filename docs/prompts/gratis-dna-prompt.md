# Cursor prompt: Gratis werk-DNA, an anonymous entry point (20 questions, no account)

## 0. Rules (read first)
- **Branch:** `git fetch origin && git checkout -b cursor/gratis-dna origin/acceptatie`. The code references below are from `origin/acceptatie` @ `964f954` (2026-09-27 21:44 CEST). Re-check the line numbers, since they may have moved.
- **Exactly ONE pull request, into `acceptatie`.**
  - Do **not** merge it and do **not** deploy.
  - Do **not** use or trigger shortcut/rule **`123`**.
  - Never push to `acceptatie` or `main`. See `docs/release-flow.md`.
- **Reference images** are on branch `docs/gratis-dna`, folder `docs/mockups/gratis-dna/`. Get them with
  `git fetch origin docs/gratis-dna && git checkout origin/docs/gratis-dna -- docs/mockups/gratis-dna docs/prompts/gratis-dna-prompt.md`, and include that folder in the PR.
  - `g1-landing.png`: landing / hero "Ontdek je werk-DNA" (no account, ± 3 minutes, 20 vragen).
  - `g2-vraag.png`: question screen (test pills, "vraag 7 van 20", test name, one Likert statement at a time, answered items collapsed).
  - `g3-resultaat.png` (viewport) and `g3-resultaat-lang.png` (full page): result with 4 plain-language tiles, the locked jobs teaser, share box, "Wil je het preciezer?" list, "Wis mijn antwoorden van dit apparaat", sticky CTA "Bewaar je DNA – maak gratis account" + "Niet nu".
  - `g4-aanmelden.png`: sign-up with the green "Je 20 antwoorden worden meegenomen" box.
  - `g5-deelkaart-1080x1920.png` (real size) and `g5-deelkaart-preview.png`: the generated story share card.
  - The mockups do **not** show the age question (§4) or the "Nog 5 vragen voor een scherper beeld" link (§3.3). Add both in the same visual language (pill/choice buttons as in `g2`, a quiet text link as in `g3`).
  - All texts in the mockups are examples. The real tile texts come from `OnboardingImpressionLibrary` (§3.3).
- **Design system** (`.cursor/rules/design-system.mdc`):
  - Use tokens (`--brand`, `--surface`, `--muted`, `--border`, `--pearl`, `--success`, `--accent-soft`, …), no new hex colours.
  - Font weights are 400/600 (700 only where the design system already allows it for h1).
  - Tap targets are at least **44×44 px**; Likert buttons 48 px high as in `g2`.
  - Use logical properties so RTL (`ar`) works.
  - Mobile first (360–430 px), and no horizontal overflow.
- **No fake statistics.** No "x% van de mensen…", no hard percentages on the anonymous result or the share card.
- **Out of scope** (do **not** build these in this PR):
  - real Mollie payment for the deep analyses, and the €9 (2 tests) / €15 (all tests) bundles
  - extending the culture test from 18 to 25 questions (`AssessmentTestCatalog.cs:46` stays `FreeQuestionCount: 18`)
  - any job-matching / scoring-for-matching changes
  - redesigning the onboarding wizard (only the changes in §2 and §6.4)

## 1. Background: what exists today (audit, verified in code and on acceptatie.lobsy.nl)
| Topic | Where | Fact |
|---|---|---|
| Onboarding wizard | `Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor:1-3` | `@page "/candidate/start"`, `InteractiveServer (prerender: false)`, `[Authorize(Roles = "Candidate")]`. Anonymous visitors are redirected to `/login`. |
| Mini-test IDs | `Jobsy.Core/Rules/OnboardingWizardCatalog.cs:91,97,104,119` | Competency `[1, 6, 11, 16, 21]` (5), **Career `[1, 6, 10, 14, 18, 22]` (6, line 97)**, Culture `[1, 3, 5, 7, 11]` (5), Values `[1, 6, 11, 16, 21]` (5) = **21 today**. |
| Result label | `Jobsy.Core/Rules/OnboardingImpressionLibrary.cs:9` | `ResultLabel = "Eerste indruk · op basis van 21 vragen"`; test at `Jobsy.Tests/CandidateOnboardingWizardTests.cs:15,28,43`. |
| Plain-language sentences | `OnboardingImpressionLibrary.cs:11` `StrengthSentence`, `:27` `RiasecSentence`, `:45` `CultureSentence`, `:63` `ValueSentence` | Static, Dutch only, no DB. |
| Impression composition | `Jobsy.Infrastructure/Services/CandidateOnboardingService.cs:208-345` (`BuildImpressionAsync`) + `:347-368` (`TopCompetencyItems`) | Reads the 4 rows from the DB, resolves provisional scores, picks top-2 strengths, top-2 RIASEC, best culture dimension, best value, then adds live matches. The picking logic is pure but locked inside a DB-bound service. |
| Provisional scores | `Jobsy.Core/Rules/ProvisionalAssessmentScores.cs` (`ResolveCompetency`, `ResolveCareer`, `ResolveCulture`, `ResolveValues`) | Scores from partial draft answers, static. `CareerTestCatalog.Score` (`CareerTestCatalog.cs:121-151`) returns `null` per RIASEC type without answers; `RiasecRanking.Rank` (`RiasecRanking.cs:11-35`) skips nulls. |
| Wizard mini step loading | `OnboardingWizard.razor:1187-1266` (`PrepareMiniAsync`) | Per step 7–10 it loads the test via `Api.GetMy…Async`, puts answered mini IDs in `_lockedIds` (`:1241`) and shows only unanswered ones (`:1245`); autosave merges with server answers and calls `Api.SaveMy…Async(merged, complete: false)` (`:1255-1264`). If all are answered it shows `Onboarding.MiniAllDone` (`:567-569`) but the step itself is **not** skipped. |
| Consent gate | `OnboardingWizard.razor:739`, `:881-884` | Step 7 asks test/AI consent first when `!_hasConsent`. |
| Consent API | `Jobsy.Api/Controllers/MeController.cs:830-851` `POST api/me/test-ai-consent`; client `Jobsy.Web/Services/ApiClient/JobsyApiClient.cs:800` `AcceptTestAiConsentAsync` | Sets `TestAiConsentAt` + `TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion` (`PrivacyConstants.cs:11`). Rejects when `!CanUseCandidateFeatures`. |
| Age / parental consent | `Jobsy.Core/Privacy/CandidateConsentRules.cs:8-9,19-27`; `MeController.cs:920-955` `parental-consent-request`; `ParentalConsentController.cs:20-44` | Min age 13, parental consent below 16. The parental flow needs an **account** (token stored on the `User`, confirmation link mailed to the parent). |
| Save services | `Jobsy.Infrastructure/Services/CandidateCompetencyService.cs:~62-110` (same pattern in `CandidateCareerInterestService.cs:39-117`, `CandidateCulturePersonalityService.cs`, `CandidateValuesService.cs`) | **Warning:** `SaveAsync(…, complete: false)` sets `Status = Draft` and clears the stored percentages. Calling it on a *completed* test silently downgrades it. The merge must never do that (§6.3). |
| Questionnaire UI | `Jobsy.Web/Components/Shared/Questionnaire/QuestionnaireShell.razor:162-184`, `LikertScaleQuestion.razor:49-65` | Reusable shell (progress bar, `AnsweredCount/TotalCount`, `SaveStatus`, `BackHref`, `OnNext/OnBack`) and Likert item (`Index/Total`, `LowLabel/HighLabel`, `Collapsed`, `ValueChanged`). |
| Public layout | `Jobsy.Web/Components/Layout/TeaserLayout.razor`; example page `Jobsy.Web/Components/Pages/WestlandTeaser.razor:1-5` | `@layout TeaserLayout`, `[AllowAnonymous]`, `InteractiveServer (prerender: true)`. |
| Browser storage + cookie consent | `Jobsy.Web/wwwroot/js/app-core.js:5-6` (`jobsy.origin`, `jobsy.anonymousKey`), `:92-126` (engagement key only after analytics consent), `:359-395` (`Jobsy.CookieConsent`, `necessary` vs `analytics`) | Keys use the lower-case `jobsy.*` pattern. Analytics storage is gated; functional storage is not. Documented in `Jobsy.Web/Components/Pages/Legal/Privacy.razor:43`. |
| Share | `Jobsy.Web/Components/ShareModal.razor:49-76` | Vacancy share modal (WhatsApp, e-mail, …, copy link). No `navigator.share` / image share yet. |
| Post-login landing | `Jobsy.Web/Auth/AuthRedirects.cs:8,16` | Candidates land on `/candidate/start` (how-to/onboarding) or the banenkaart. Google login goes via `Login.razor:73-88` → the external callback → these redirects. Register: `Jobsy.Web/Components/Pages/Register.razor:1,5,402` (`/register`, `?ref=`). |
| Translations | `Jobsy.Web/Localization/UiStrings.cs:3376-3382` (`…MergeAll(nl, en, pl, ro, ar)`), pattern file `UiStringsOnboardingV2.cs` | Five languages: nl, en, pl, ro, ar. |
| Tests project | `Jobsy.Tests/Jobsy.Tests.csproj` | xUnit + Playwright 1.49 + Mvc.Testing; references Web. **bUnit is not referenced yet.** Playwright soft-skip pattern: `Jobsy.Tests/Acc2709PlaywrightTests.cs:18` (`JOBSY_E2E_BASE_URL`). |

## 2. Exactly 20 onboarding questions (5 per test)
1. `OnboardingWizardCatalog.cs:97`: change `CareerQuestionIds` to **`[1, 6, 14, 18, 22]`**, i.e. drop **Q10 (Artistic)**. Update the XML comment above it ("five of six RIASEC directions; Artistic is measured in the full test").
   - Rationale: most Westland vacancies are logistics, horticulture, hospitality, care and admin, so R/I/S/E/C carry the most matching signal. An unanswered type is `null` and is skipped by `RiasecRanking.Rank`, so Artistic can never become a "top" in the first impression. If Dennis prefers another direction to drop, only this array and its tests change.
   - Existing users who already answered Q10 keep that answer (it is a valid free-test answer). Do not migrate data.
2. `OnboardingImpressionLibrary.cs:9`: `"Eerste indruk · op basis van 20 vragen"`. Search the repo for any other "21" occurrence (`rg -n "21 (vragen|questions)"`, plus the pl/ro/ar translations) and change them to 20. If the label is rendered to users, give it a translation key (`Onboarding.Result.Label`, 5 languages) instead of the hard-coded Dutch string.
3. Update the tests `CandidateOnboardingWizardTests.cs:15` (`6` → `5`), `:28` (keep `22`, add `Assert.DoesNotContain(10, …)`), `:43` (`"20 vragen"`), and add a test that the four arrays sum to **20** and each has 5 distinct IDs that exist in their catalog.
4. Add `OnboardingWizardCatalog.TotalMiniQuestionCount => 20` (computed from the arrays) and use it everywhere a "20" is shown (landing, progress, sign-up box), never a literal.

## 3. New public route `/ontdek` (alias `/dna`)
### 3.1 Page and architecture
- New page `Jobsy.Web/Components/Pages/Public/GratisDna.razor`:
  - `@page "/ontdek"` and `@page "/dna"` (the share card shows `lobsy.nl/dna`)
  - `@layout TeaserLayout`, `@attribute [AllowAnonymous]`, `@rendermode @(new InteractiveServerRenderMode(prerender: true))`, like `WestlandTeaser.razor:1-5`
  - The landing must prerender for SEO. Read browser storage only in `OnAfterRenderAsync(firstRender)`.
- One page with three internal states: `Landing` → `Questions` → `Result` (plus `Under16`, §4). Use a `?stap=` query or internal state; the browser back button must go back one screen, not leave the flow.
- **Logged-in candidates** who open `/ontdek` are redirected to `/candidate/start` (or the Mijn DNA page when onboarding is finished). Employers/admins see the page but get a note that the test is for candidates, and no storage is written.
- **No authenticated API calls, and no new API endpoints.** Everything runs in the Blazor circuit with the static catalogs (`CompetencyTestCatalog`, `CareerTestCatalog`, `CulturePersonalityCatalog`, `SchwartzValuesCatalog`, `ProvisionalAssessmentScores`, `OnboardingImpressionLibrary`). Nothing is written to the database. Do not log answers or scores (no `ILogger` with answer payloads, no telemetry events with answers).
- Add `/ontdek` to the sitemap / public routes list if one exists, and add a quiet entry link on the public teaser (`WestlandTeaser.razor`) and on `/login` + `/register` ("Eerst je werk-DNA ontdekken? Doe de gratis test"). Keep it small; no redesign of those pages.

### 3.2 Question screens (`g2`)
- 20 statements: the mini IDs from §2 in wizard order (competency 5 → career 5 → culture 5 → values 5), with the existing translation keys (`CompetencyTestCatalog.Questions[].TextKey` etc., the same as `OnboardingWizard.razor:1206-1228`).
- Reuse `QuestionnaireShell` + `LikertScaleQuestion` (see §1). If `QuestionnaireShell` is too tied to the logged-in tests (e.g. `BackHref` defaults to `/candidate/profile`, "Bewaard" status), add parameters rather than forking it: e.g. `SaveStatusText` ("Opgeslagen op dit apparaat") and `ShowPrivacyNote`. Existing pages must keep their current look (a bUnit/snapshot check on one existing page is welcome).
- Header: the 4 test pills (done = green, current = brand, as in `g2`), "vraag 7 van 20", the test name, and a progress bar over all 20. After each answer, auto-advance to the next statement (like the wizard). Back goes to the previous statement.
- Each answer is written to storage immediately (§5), so a reload or closing the tab resumes at the first unanswered question ("Verder waar je was").

### 3.3 Result screen (`g3`)
- **Extract the pure picking logic** from `CandidateOnboardingService.BuildImpressionAsync` (`:268-321`) and `TopCompetencyItems` (`:347-368`) into a static Core class, e.g. `Jobsy.Core/Rules/OnboardingImpressionComposer.cs`:
  - `Compose(ResolvedCompetency, ResolvedRiasec, ResolvedCulture, ResolvedValues) → ImpressionCore(strengths, riasecTop2, cultureHighlight, topValue)`
  - `BuildImpressionAsync` must call it, with **no behaviour change** (the existing impression tests must pass unchanged). The anonymous page calls the same composer with scores from `ProvisionalAssessmentScores.Resolve…(status: null, answersJson: <from storage>, …)`.
- Four tiles, as in `g3`:
  - "Zo werk jij" → top strength sentence
  - "Dit vind je leuk" → top RIASEC sentence
  - "Hier voel je je thuis" → culture sentence
  - "Dit vind je belangrijk" → value sentence
  - Above them, the label badge "Eerste indruk · op basis van 20 vragen". Below them: "Dit is een eerste indruk. Hoe meer vragen je beantwoordt, hoe preciezer het wordt."
  - **No percentages, no bars, no scores** anywhere on this page.
  - Optional "In het kort: …" line: only if it can be built from translation keys (no free text, no AI).
- **Translations of the sentences:** `OnboardingImpressionLibrary` is Dutch-only. Add translation keys `GratisDna.Tile.{Strength|Riasec|Culture|Value}.{code}` for all codes in 5 languages. NL values must be **identical** to the library, and a unit test asserts that for every code. The page uses the keys; the library stays the source for the wizard.
- **Locked jobs teaser** (blurred placeholder rows, lock icon): "Meld je aan om te zien welke banen bij je passen" / "Vacatures in jouw buurt, gesorteerd op jouw DNA." Tapping it goes to sign-up (§6.1). The blurred rows are static placeholders, **not** real vacancies, and **no** "x banen gevonden" counts.
- **Share box** "Deel je werk-DNA" (WhatsApp, Instagram, generic share icon), see §3.4.
- **"Wil je het preciezer?"** list (as in `g3-resultaat-lang`): "Maak de 4 tests af" (± 25 vragen per test · je 20 antwoorden tellen mee, tag Gratis), "Uitgebreide test met rapport" (150–200 vragen per test, tag Betaald, **no price shown**), "Banen die bij je passen" (tag Gratis). All three go to sign-up.
- A quiet link **"Nog 5 vragen voor een scherper beeld"** → sign-up (same target as the CTA, with `?van=ontdek`). It does not add questions anonymously.
- **"Wis mijn antwoorden van dit apparaat"**: a confirmation step (inline or small dialog, "Weet je het zeker?"), then remove the storage key and return to the landing.
- Sticky footer: primary **"Bewaar je DNA – maak gratis account"** and ghost "Niet nu" (which just closes the sticky footer/scrolls; answers stay stored until expiry).

### 3.4 Share (Web Share API + fallback) and the 1080×1920 card (`g5`)
- New JS module `Jobsy.Web/wwwroot/js/gratis-dna.js` (loaded only on this page, respect the existing CSP/bundling setup; add it to the minify/bundle step if the other modules are bundled):
  - `renderCard(model) → Blob (image/png)`: draw on a **1080×1920 canvas**: brand background, Lobsy logo (existing asset), "Mijn werk-DNA", the 4 tile titles + sentences, the "Eerste indruk" label, and the footer CTA "Ontdek jouw werk-DNA · lobsy.nl/dna". **No name, no percentages, no date of birth, nothing personal.** Wait for `document.fonts.ready` before drawing; wrap long lines (pl/ro are longer; `ar` renders RTL).
  - `share(model, channel)`:
    - If `navigator.canShare?.({ files: [file] })` → `navigator.share({ files: [file], text, url })` (this covers Instagram Stories and WhatsApp on mobile).
    - Fallback WhatsApp: open `https://wa.me/?text=<encoded text + https://lobsy.nl/dna>`.
    - Fallback Instagram (no web intent exists): download the PNG and show a hint "Afbeelding bewaard. Open Instagram en kies hem voor je story."
    - Generic icon: `navigator.share({ text, url })`, else copy the link (reuse the copy feedback style of `ShareModal.razor`).
  - The shared URL is always the plain `/dna` landing. **Never** put answers or results in the URL.
- A user who cancels the native share sheet (`AbortError`) sees no error.

## 4. Consent and age (landing / before the first question)
Chosen option, the simplest compliant one:
- **Landing (`g1`) gets two required inputs before "Start de gratis test":**
  1. Age choice (two pill buttons): "16 jaar of ouder" / "Jonger dan 16".
  2. Test consent checkbox, short text + link to `/privacy`: "Ik geef toestemming dat Lobsy mijn antwoorden gebruikt om mijn werk-DNA te berekenen." Store `{ version: PrivacyConstants.CandidateProfilingConsentVersion, at: <ISO UTC> }` in the storage object (§5). The version must come from C# (`PrivacyConstants.cs:11`), never hard-coded in JS.
- **Under 16:** anonymous use is **not** offered, because parental consent needs a verifiable parent e-mail tied to an account (`MeController.cs:920-955`, `ParentalConsentController.cs:20-44`). Show a friendly `Under16` state: "Leuk dat je mee wilt doen! Onder de 16 heb je toestemming van je ouder of voogd nodig. Maak een account, dan vragen we die toestemming. Daarna kun je de test doen." with a button to `/register`. **Store no answers** for this choice (and do not store the age choice either). Explain this choice in the PR description.
- **16+:** store `ageBand: "16plus"` (not a birth date) and start the questions. The real date of birth is still asked in the wizard; the merge re-checks it (§6.3).
- If the stored consent version differs from the current `CandidateProfilingConsentVersion` (the privacy text changed), ask for consent again before showing the result or merging.

## 5. Browser storage
- One key: **`jobsy.gratisDna.v1`** in `localStorage` (fall back to `sessionStorage` if `localStorage` throws, as `app-core.js:110-126` does).
- Shape (validate strictly on read; discard on any mismatch):
  ```json
  { "v": 1, "createdAtUtc": "…", "expiresAtUtc": "… (+7 days)",
    "consent": { "version": "2026-09-26", "atUtc": "…" }, "ageBand": "16plus",
    "answers": { "competency": { "1": 4 }, "career": {}, "culture": {}, "values": {} } }
  ```
  - Only the 20 mini IDs from §2, values 1..5. Unknown IDs/tests are dropped.
  - Expired → delete on read. "Wis mijn antwoorden" → delete.
  - No name, e-mail, date of birth or free text is ever stored.
- A small JS API in `gratis-dna.js` (`load`, `save`, `clear`) plus a C# wrapper service `GratisDnaStorage` (JS interop), used by both the page and the merge (§6).
- **Cookie-consent decision (document it):** this is functional storage that is strictly necessary for a service the user explicitly asks for (ePrivacy art. 5(3) exemption), so it is **not** gated by `Jobsy.CookieConsent` / `analyticsAllowed()` (`app-core.js:92-107`), unlike the engagement key. Add a line to the privacy page list at `Privacy.razor:43` (NL, and EN if that page is translated): "antwoorden van de gratis werk-DNA-test in `localStorage` (`jobsy.gratisDna.v1`), alleen op jouw apparaat, max. 7 dagen, wisbaar via 'Wis mijn antwoorden'; bij het maken van een account worden ze in je profiel opgeslagen." Also mention it in the PR description.

## 6. Sign-up and merge
### 6.1 Sign-up screen (`g4`)
- The CTA goes to `/register?van=ontdek` (keep `?ref=` working, `Register.razor:402`). When valid stored answers exist, `Register.razor` shows the green box from `g4`: **"Je 20 antwoorden worden meegenomen"** (use the real count if fewer than 20, e.g. "Je 12 antwoorden worden meegenomen"), with the 4 tests ticked, and the line "Gratis. De tests in je onboarding zijn dan al klaar." Without stored answers nothing changes on Register.
- Show the same box (compact) on `/login` so the Google path also communicates it.

### 6.2 Where the merge runs
- A scoped service `GratisDnaMergeService` (Web) with one method `TryMergeAsync()` that runs **at most once per circuit** (cache the `Task`).
- Call it:
  1. at the start of `OnboardingWizard` loading, **awaited before** step/mini computation (`PrepareMiniAsync`, `OnboardingWizard.razor:1187`), and
  2. from a tiny `<GratisDnaMerge />` component in `MainLayout` for authenticated **Candidates** (`OnAfterRenderAsync(firstRender)`), so users who land on the banenkaart after Google login (`AuthRedirects.cs:16`) are merged too.
- This covers both the e-mail-code Register path and the Google login path, because both end in an authenticated candidate circuit.

### 6.3 Merge rules (unit-tested)
Put the pure decision logic in Core, e.g. `GratisDnaMerge.Plan(stored, serverAnswersPerTest, serverStatusPerTest, today)` → per test the answers to write.
1. Read and validate storage (§5). Nothing valid → no-op.
2. Load the profile (`GetMeAsync`). If `!CandidateConsentRules.CanUseCandidateFeatures(user)` (under 16 without parental consent) → **do not merge and do not clear**; retry on a later login (expiry still applies).
3. Consent: if the stored consent version equals `CandidateProfilingConsentVersion` and the user has no current test consent (`CandidateConsentRules.cs:25-27`), call the existing `AcceptTestAiConsentAsync()` (`JobsyApiClient.cs:800` → `MeController.cs:830`). If the versions differ, do not auto-consent. Keep the data, and merge after the user accepts the consent in the wizard (step 7 gate, `OnboardingWizard.razor:881`).
4. Per test: `GetMy…Async`.
   - If the test is **Completed** (`CandidateCompetencyStatuses.IsCompleted`, `Jobsy.Core/Entities/CandidateCompetency.cs:41`), **skip it entirely**. Never call `SaveMy…Async(…, complete: false)` on a completed test, because that downgrades it to Draft (`CandidateCompetencyService.cs:98-108`).
   - Otherwise add stored answers **only for question IDs that have no server answer** (server answers always win), and call `SaveMy…Async(merged, complete: false)` only when at least one answer was added.
5. When all four tests are handled successfully, **clear** the storage key. On any API error, keep the storage and retry next time (no partial clear, no error toast; log the failure **without** answers).
6. After a merge, raise an event/state so an open wizard reloads its mini step.

### 6.4 Wizard skips answered test steps
- The existing per-question skip (`_lockedIds`, `OnboardingWizard.razor:1241-1245`) already hides answered mini questions. Add the step-level skip: when a mini step (7–10) has **no unanswered mini questions** after loading, do not show the `Onboarding.MiniAllDone` screen (`:567-569`). Advance automatically (record it with `SaveMyOnboardingProgressAsync(step, stepCompleted: true)`; for step 10 go to the finish/impression as `NextAsync` does at `:889-894`).
- Apply the skip in the forward direction only. Going back into a completed step still shows `MiniAllDone`.
- Result: a user who did all 20 anonymously gets profile steps 1–6, no consent gate (consent was recorded in §6.3), no test steps, and then the finish screen with the impression.

## 7. Texts / translations
- New file `Jobsy.Web/Localization/UiStringsGratisDna.cs` with `MergeAll(nl, en, pl, ro, ar)`, registered next to the others at `UiStrings.cs:3376-3382`.
- Keys under `GratisDna.*` for every visible string: landing, age, consent, under-16, question header, result, tiles (§3.3), teaser, share, the "preciezer" list, wipe + confirmation, sticky CTA, the register/login box, and the share-card texts.
- **NL and EN complete; pl, ro, ar with every key present** (a best-effort translation is fine; do not leave keys missing). Add a unit test that all 5 dictionaries contain every `GratisDna.*` key.
- Dutch copy from the mockups is leading ("Ontdek je werk-DNA", "Start de gratis test", "Bewaar je DNA – maak gratis account", "Meld je aan om te zien welke banen bij je passen", "Je 20 antwoorden worden meegenomen", "Wis mijn antwoorden van dit apparaat"). Simple Dutch (B1), and address the user as "je".

## 8. Tests (all must pass in CI)
**Unit (xUnit, `Jobsy.Tests`):**
- Catalog: 4 × 5 = **20** mini IDs, career without 10, `TotalMiniQuestionCount == 20`, label "20 vragen" (§2).
- Composer: `BuildImpressionAsync` output unchanged for an existing fixture; composer picks the expected tiles for fixed answer sets; career answers with Artistic missing never yield Artistic.
- Anonymous scoring: 20 answers → 4 tiles; fewer answers → only the tiles with data; invalid values ignored.
- Storage validation: wrong version, expired, unknown IDs, values outside 1..5 → discarded/dropped.
- Merge plan: fills only unanswered IDs; **never overwrites** existing server answers; **skips Completed tests**; no-op + no clear when `CanUseCandidateFeatures` is false; no auto-consent on a version mismatch; clears only after full success.
- Translations: every `GratisDna.*` key exists in nl/en/pl/ro/ar; NL tile keys equal the `OnboardingImpressionLibrary` sentences.

**bUnit:**
- Add the `bunit` package to `Jobsy.Tests.csproj` (it is not referenced yet).
- Result state renders the 4 tiles + "Eerste indruk · op basis van 20 vragen", the locked teaser, the share buttons, the CTA and the wipe link; contains **no `%`**. Under-16 state renders the register CTA and triggers no storage write.

**Playwright (soft-skip when `JOBSY_E2E_BASE_URL` is empty, same pattern as `Acc2709PlaywrightTests.cs:18`):**
- Viewport **390×844**, fresh context:
  1. `/ontdek` → choose 16+, tick consent → answer 20 statements → result visible (4 tiles, no `%`)
  2. reload mid-way resumes at the right question
  3. the CTA leads to `/register?van=ontdek` showing "Je 20 antwoorden worden meegenomen"
- Merge + skip check: if a disposable test candidate can be created on the target (or behind a `JOBSY_E2E_ALLOW_SIGNUP` flag), register/log in, open `/candidate/start`, and assert that steps 7–10 are **not** shown (the wizard goes from step 6 to the finish) and `localStorage["jobsy.gratisDna.v1"]` is gone. Otherwise soft-skip this part with a clear message. Never use or alter `kandidaat@jobsy.local`'s real data for this.
- **No horizontal overflow** (`document.documentElement.scrollWidth <= innerWidth`) on landing, question, result and register at **360, 390 and 430 px**.
- "Wis mijn antwoorden" removes the key and returns to the landing.

## 9. Definition of done
- `dotnet build` and `dotnet test` are green, with no new warnings in the touched files.
- The PR description contains:
  - screenshots at 390 px of landing, question, result, under-16 and register-with-box (and the generated share card)
  - the under-16 choice (§4) and the storage/cookie-consent reasoning (§5)
  - the dropped career question (Q10) and why
  - an explicit "out of scope" list (§0)
- ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.
