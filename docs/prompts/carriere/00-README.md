# Carrière in the ontdekkingsreis style (+ Contactverzoeken, Hoe werkt Lobsy): Cursor run book

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

> **Rules (repeated in every file):**
> - Branch from `origin/acceptatie` (file 01) or from the previous file's branch (stacked). ONE PR per file, always into `acceptatie`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`). Never push to `main` or `acceptatie`. No force-pushes. Push only `cursor/carriere-*` branches.
> - Red build/tests or an unmet success criterion: push, open that PR as **draft**, stop and report. Don't start the next file.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

**What this stack builds:**
- **`/carriere` redesigned in the style of De ontdekkingsreis** (`docs/mijn-paspoort` 06–08): the underwater scene, light rays, bubbles, the lobster, the rail, eyebrow + h1 and the bubble.
  - **The lobster is the protagonist.** It **climbs stone by stone from the deep (where you are now) to the light (your dream job, the golden stone)**.
  - On every stone it passed, its old shell stays behind. Per completed step it grows and gets a gold new shell.
  - Claws = what you already have / still grow. Antennas = what fits you. The right stone = a job that fits your size.
- **The career plan made honest.**
  - The dream job is chosen from suggestions and a job list, not free text straight to the AI.
  - Steps are completed by you, not by a self-claim that writes certificates.
  - Changing the dream job keeps what you achieved.
  - Band text instead of a hero %.
  - Real courses: free first, Partnerlink labelled.
- **`/candidate/talent-contacts`** (employer contact requests) and **`/candidate/hoe-werkt-lobsy`** restyled in the same language, with their bugs fixed.
- Every bug from the 30-09 review, and **5 languages (nl/en/pl/ro/ar, `ar` RTL)** on every touched string.

## Bugs fixed (review at `a611db40`, live check on acceptatie 30-09 ~07:50 CEST)

| # | Bug | Evidence | Fixed in |
|---|---|---|---|
| B1 | Changing the dream job **deletes the plan and all step progress** | `CandidateCareerPlanService.GenerateAndSaveAsync` ~L148–156 removes `StepProgress` + plan | 01 (archive), 02 (dialog) |
| B2 | Regenerate confirm is native `window.confirm` via JS and **returns `true` when JS fails** → costly AI regeneration auto-confirms | `CareerDashboard.razor` ~L506–516 | 02 |
| B3 | The dream input commits on **blur and on Enter** → likely a double confirm/generation; `_busy` is set too late | `CareerDashboard.razor` L122, ~L424, ~L477 | 01 (server idempotency), 02 (UI) |
| B4 | "+ Heb ik al" writes an **unverified certificate (AI course name, current year) into the profile** and can auto-complete a step. Undo doesn't remove it. After an undo, auto-complete stays blocked forever | `ClaimCourseAsync` L224–255, `CareerStepStatusResolver` | 01 |
| B5 | Steps can be completed in any order; "Active" = first open step, so the stepper lies | `CareerStepStatusResolver.Resolve` | 01 |
| B6 | Hero %: "70% match met je profiel" (Piloot) next to "Match op deze stap 0%" | live kandidaat@jobsy.local; `MatchWithProfile`, `StepMatchPercent` L103/L312–320 | 01 (band), 02/03 (UI) |
| B7 | CTA "Schrijf je in voor een basisopleiding" opens `/candidate/profile`: hrefs are hardcoded **by step index** | `CareerPathPlanGenerationService` L158–162 | 01, 03 |
| B8 | "0 jaar richtinggevend" is rendered | live; L304–305 | 03 |
| B9 | Course hints are AI names only ("Luchtvaarttechniek cursus"), no provider/link. `TrainingOffersBlock` isn't on `/carriere` and has no free/partner notion, no Partnerlink disclosure, no `rel="sponsored"`, and shows the raw `ex.Message` | L274, `TrainingOffersBlock.razor` | 03 |
| B10 | Empty state: "Stip op de horizon" 3×, **the placeholder "Typ je droombaan…" is rendered as the h2**, and there are 6 hardcoded management suggestions unrelated to the profile | live valentine@jobsy.local; `CareerPathService` L14–22 | 01, 02 |
| B11 | `HorizonArt` hardcodes 11 hex colours + gradients (sunset, off-metaphor), plus an inline `style="width:…%"` | L763–792, L320 | 02 |
| B12 | Raw `ex.Message` shown to candidates (7×) | L412, 538, 562, 585, 626, 657 | 01 (codes), 02/03 |
| B13 | Copy isn't B1 ("stip op de horizon", "DNA", "gap-analyse", "skills gap", "AI bouwt je diepe stappenplan"). "✓" is baked into strings. `CareerDash.*` exists **only in nl/en** | `UiStringsCompetencies.cs` L421–478 | 02, 03 |
| B14 | Harsh black focus box around the h1 after navigation | live screenshots `/carriere`, `/candidate/hoe-werkt-lobsy` | 02, 05 |
| B15 | Completion is only a toast; no lobster, scene or bubble | — | 03 |
| T1 | `/candidate/talent-contacts`: **h1, lead and back link are never visible.** `.profile-page__header` is hidden ≥ 769 px and `.profile-page--candidate .profile-page__header` ≤ 768 px | live: the page shows only "Je hebt nog geen contactverzoeken."; `app.css` ~L1494, ~L15251 | 04 |
| T2 | **Wrong privacy copy:** "Reageer binnen 48 uur; daarna worden contactgegevens gedeeld". In reality nothing is shared after 48 h (the status goes to `RefundEligible`) | `Talent.CandidateLead`; `TalentPoolService.MarkExpiredAsRefundEligibleAsync` | 04 |
| T3 | "Ja, ik wil contact" **shares name, e-mail and phone immediately without a confirm** or showing what is shared | `CandidateRespondAsync` L319–349, `ToDtoAsync` | 04 |
| T4 | Raw enum status ("Pending"), `RespondByUtc.ToLocalTime()` = **server** time zone, `"g"` format, raw `ex.Message`, token/refund jargon in the candidate message ("De werkgever kan het token terugkrijgen") | `CandidateTalentContacts.razor` L37–39, 73, 99; `Talent.Declined` | 04 |
| T5 | "Ik ben al voorzien" is stored exactly like "Geen interesse"; `Talent.*` candidate keys exist only in nl/en | `CandidateRespondAsync`; `UiStringsCompetencies.cs` L231–247 | 04 |
| H1 | `/candidate/hoe-werkt-lobsy` renders the literal text **"_message"**: `Message="_message"` passes a string literal | live; `Candidate/HowLobsyWorks.razor` | 05 |
| H2 | The error is set and then the page navigates away immediately, so it's never seen. The page allows employer roles but calls the **candidate** API | same file | 05 |
| H3 | Outdated and inconsistent copy: nl says 25 + 25 questions, pl/ro/ar say "20 questions"; "Lik of bewaar"; no Ontdekkingsreis/Paspoort/Carrière. Inline `style` in `HowLobsyGuidePanel` | `UiStrings.cs` `HowLobsy.*`; `HowLobsyGuidePanel.razor` | 05 |

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-plan-data-fixes.md`: plan archive + restore, carry-over of achievements, dream options (suggestions + job catalog + safe free text), completion rules, proof instead of self-claim, step fit band, deterministic step actions, error codes, generation guard, `CareerPlanViewBuilder` | `cursor/carriere-1` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-reis-overzicht-droombaan.md`: page shell in the journey style (`CareerClimbScene`, lobster, rail, growing-shells stepper), empty state, overview, dream-change dialog, removal of `HorizonArt`/native confirm, B1 copy in 5 languages | `cursor/carriere-2` | `cursor/carriere-1` | `acceptatie` |
| 03 | `03-stap-detail-groei.md`: step detail (claws, band, courses via `CourseSuggestionBlock`, proof), completion moment (shell falls, gold new shell), undo, Werkgevers-OFF gating | `cursor/carriere-3` | `cursor/carriere-2` | `acceptatie` |
| 04 | `04-contactverzoeken.md`: `/candidate/talent-contacts` restyle + T1–T5 (confirm dialog, correct copy, Amsterdam time, decline reason, gating) | `cursor/carriere-4` | `cursor/carriere-3` | `acceptatie` |
| 05 | `05-hoe-werkt-lobsy.md`: `/candidate/hoe-werkt-lobsy` restyle as "five stones" + H1–H3 | `cursor/carriere-5` | `cursor/carriere-4` | `acceptatie` |
| 06 | `06-e2e-rapport.md`: Playwright E2E (desktop + mobile, 5 languages incl. RTL, reduced motion, Werkgevers OFF), docs, stack-end report | `cursor/carriere-6` | `cursor/carriere-5` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations/strings), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch.

## Pointer prompt (the only prompt needed; it runs 01 … 06)
```
Run the Carrière stack. First: git fetch origin && git show origin/docs/carriere:docs/prompts/carriere/00-README.md — read it completely.
Then read and execute each file in docs/prompts/carriere/ on that branch strictly in the order the README's table lists (01 … 06; a/b splits where a file allows it), one file = one PR.
File 01 branches from origin/acceptatie; every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 01, run the dependency checks in the README's "Dependencies" section and follow the fallback it prescribes for each one; say in PR 01 which case applied (re-check A–E before 02, 03 and 04).
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes. Don't change the candidate nav.
At the end report: file → branch → PR number → status, plus anything deferred (06 writes the stack-end report).
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/release-flow.md`, and on `origin/docs/mijn-paspoort`: `docs/prompts/mijn-paspoort/00-README.md` (§0, §F, §N, D12), `03-mijn-tests.md` (courses), `04-past-deze-baan-carriere.md` (Carrière tab), `07-ontdekkingsreis-wizard-stappen-1-6.md` (scene + lobster), `08-…` (shed moment), and `docs/mockups/ontdekkingsreis/README.md`.
2. Run the **Dependencies** checks below and note the outcome (it goes into PR 01). Re-run A–E before 02, 03 and 04; other stacks may have landed in between.
3. For each file in order:
   1. Read the whole file.
   2. Create its branch: file 01 `git checkout -b cursor/carriere-1 origin/acceptatie`; later files `git checkout -b <branch> <previous branch>` (previous pushed).
   3. Implement **only** that file's scope plus §0.
   4. `dotnet build` + `dotnet test` (unit + bUnit), and the Playwright suites the file names if you can. Everything green and the file's **success criteria** hold.
   5. Small, clear commits. Push `git push -u origin <branch>` (only `cursor/*`). Open **ONE PR into `acceptatie`** with the file's title. Body starts with `Stacked on #<prev PR> (<prev branch>)` (01: `Stacked on: none (first in the stack)`), then the §0 PR body items.
   6. Note the PR number; go on.
4. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely: push, open the PR as **draft** with the failure described, don't continue.
5. **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: each file adds its own migration on top of the previous one; never edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you, `git merge origin/acceptatie` into the current branch (normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Stacking, PRs, stop-on-red:** as above. The diff includes lower PRs until they merge; say which commits are this file's own.
- **Code references** are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST). Re-check line numbers before editing. Key places:
  - `Jobsy.Web/Components/Pages/Candidate/CareerDashboard.razor` (793 lines; `@page "/carriere"`, `Authorize(Roles="Candidate")`, InteractiveServer, prerender off)
  - `Jobsy.Web/Services/CareerPathService.cs`, `Jobsy.Web/Models/CareerDashboardModels.cs`
  - `Jobsy.Api/Controllers/CandidateCareerPathController.cs` (GET, POST generate, `steps/{key}/complete|uncomplete`, `courses/claim`)
  - `Jobsy.Infrastructure/Services/CandidateCareerPlanService.cs`, `CareerPathPlanGenerationService.cs` (`OpenAiFeature`)
  - `Jobsy.Core/Rules/CareerStepStatusResolver.cs`, `CareerCourseMatcher`, `CareerPlanJson.cs`, `HorizonCareerPathBuilder.cs`, `CareerPathPlanner.cs`, `CareerCompassJson.cs` (`SuperMatches`, `StrongChoices`)
  - `Jobsy.Infrastructure/Services/RoleFitCheckService.cs` (`RoleFitCheckBuilder.Build` is local; `EvaluateAsync` **persists** `CandidateRoleFitChecks` and may call OpenAI)
  - `Jobsy.Web/Components/Candidate/TrainingOffersBlock.razor`, `Jobsy.Infrastructure/Services/TrainingUpskillService.cs`
  - `Jobsy.Web/Components/Pages/Candidate/CandidateTalentContacts.razor`, `Jobsy.Infrastructure/Services/TalentPoolService.cs` (~L240–560), `Jobsy.Core/Rules/TalentContactRules.cs`
  - `Jobsy.Web/Components/Pages/Candidate/HowLobsyWorks.razor`, `Components/Shared/HowLobsyGuidePanel.razor`, `Help/HowLobsyRoleGuides.cs`, `Pages/HowLobsyWorks.razor`
  - `Jobsy.Web/Components/LobsyFriendlyDialog.razor`, `LobsyToast.razor`, `Localization/CultureState.cs` (`IsRightToLeft`), `Components/Layout/MainLayout.razor` (`dir`)
- **Mockups:** branch `docs/carriere`, folder `docs/mockups/carriere/`. Read with `git show origin/docs/carriere:docs/mockups/carriere/<file> > /tmp/<file>`. Don't commit mockups to code branches.
  - Desktop 1440×900: `cr-d1-geen-droombaan`, `cr-d2-reis-overzicht`, `cr-d3-stap-detail-opleidingen`, `cr-d4-stap-klaar`, `cr-d5-droombaan-wijzigen`, `cr-d6-contactverzoeken`, `cr-d7-contact-delen-bevestigen`, `cr-d8-hoe-werkt-lobsy` (`.png`).
  - Mobile 390×844: `cr-m1` … `cr-m7` (same names; m6 = contactverzoeken, m7 = hoe werkt Lobsy).
  - All data is **Voorbeelddata**; the pill is mockup-only. **Where mockup and spec differ, the spec wins.** Known differences:
    - **Nav:** the mockups show Dennis' order (Ontdekkingsreis · Paspoort · Carrière · Banenkaart · Sollicitaties). **Don't change the nav**; render whatever `BottomNav` gives today.
    - **"Nieuw: 14 vacatures als helpende passen bij je"** (d4) and **"Vacatures voor helpende ›"** (d3) exist only with Werkgevers ON (D6); the count only when check E is present and the fit gate is open.
    - **"Past redelijk" pill** (d3) = `CareerFitBand` text (D4), never a number.
    - **"Plan gemaakt met hulp van AI"** (d2) shows only when the plan came from OpenAI (`FromAi`). The local-builder plan says "Plan gemaakt op basis van je paspoort".
    - **Header** (tagline, language selector, name) is the existing `MainLayout`; don't rebuild it.
- **Design system:** `.cursor/rules/design-system.mdc` + paspoort §0.
  - Tokens only; weights 400/600 (700 only h1); max 2 badges and ONE primary action per card; no decorative emoji; icons only from `NavIcons`/the line set (antenna, claw, stone, shell, wave, star, check, clock, shield).
  - Tap targets ≥ 44 px; breakpoints 640/900/1024; logical properties only (`margin-inline-start`, `inset-inline-*`).
  - **Scene exception (paspoort D12):** only the scene layer may use `linear-gradient`/`color-mix()` of the derived scene tokens. Cards, buttons and text stay flat. **No hex colours** in markup or CSS of this stack (guard test in 02).
  - **No inline `style=` attributes** in the components of this stack, except CSS custom properties for geometry (`style="--x:…;--y:…;--size:…"`). Guard test in 02.
- **Motion:** gentle only. Bubbles rise, the lobster bobs (6 s), the shell plate falls **once** (1.2 s). Under `prefers-reduced-motion: reduce` all animation is off and the **still state is complete** (the new shell is simply there). No autoplaying loops besides those; nothing flashes.
- **Accessibility:**
  - The scene is `aria-hidden="true"`. Every state it shows is also in text: the rail (`<ol>`, `aria-current="step"`), the stepper and the card.
  - The lobster SVG has `role="img"` + a localized `aria-label` only where it carries meaning (completion moment); elsewhere `aria-hidden`.
  - The completion moment is announced via one `aria-live="polite"` region.
  - Focus lands on the h1 after navigation **without** the black box: `.journey-page h1:focus:not(:focus-visible){outline:none}` in the stack's CSS; keyboard focus keeps the brand outline. Don't change the global rule (deferred, listed in 06).
- **Languages (§L):** see below. Every new or touched string in **nl/en/pl/ro/ar**.
- **Strings:** new `Localization/UiStringsCareer.cs` (prefixes `Career.`, `CareerStep.`, `CareerDream.`, `CareerErr.`), `UiStringsTalentCandidate.cs` (`TalentC.`) and `UiStringsHowLobsyCandidate.cs` (`HowC.`). Follow the `UiStringsMatch.MergeAll` pattern and register them in `UiStrings.cs`. Old `CareerDash.*` keys that become unused are removed in 06 (not earlier; the paspoort tab may read them). `LocalizationParityReportTests` and `LocalizationTests` stay green.
- **Server errors:** API endpoints of this stack answer with `{ code: "<snake_case>" }` (ProblemDetails extension) and the UI maps codes to `CareerErr.*` / `TalentC.Err.*` keys. The UI never shows `ex.Message`; unknown errors show `Common.Error` and log the exception.
- **Authorization:** `/carriere`, `/candidate/talent-contacts` and `/candidate/hoe-werkt-lobsy` are **Candidate** only (05 tightens hoe-werkt-lobsy, which today also allows employer roles and admin: they are redirected to `/hoe-werkt-lobsy`). Every career endpoint resolves the user from the claims; a foreign plan/step id answers 404. `BlazorPageRoleAttributesTests` updated.
- **Feature flags:** only through `IFeatureFlags` (Dependency D). Werkgevers OFF ⇒ no vacancy data, links or counts on these pages, and the server returns none.
- **Privacy (AVG):** the career plan is personal data. It's included in the existing data export and deletion (`PrivacyDataService`) incl. archived plans (01); archived plans are hard-deleted 30 days after archiving. Talent-contact sharing is shown before it happens (04). No new data leaves Lobsy; AI generation keeps today's provider path (`OpenAiFeature`), and the dream title goes to it only as a quoted data field (01.3).
- **CSS:** new `wwwroot/css/features/carriere.css` (BEM `career-…`, plus `journey-page` scoping). Link it in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-carriere`, add it to `Jobsy.Tests/asset-versions.json` (`AssetVersionGuardTests`). Don't append to `app.css`. Remove the `career-dash`/`horizon-*` rules from `app.css` in 06 once unused (+ `app.min.css`).
- **Docs/guards when routes or pages change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (all three pages private; `PageSeoTests`), `Help/PageHelpDocs.cs` (`/carriere`, `/candidate/talent-contacts`, `/candidate/hoe-werkt-lobsy`; `PageHelpDocsTests`), `CHANGELOG.md`.
- **Must NOT touch:**
  - the candidate nav (`RoleNavCatalog`, `BottomNav`, `MainLayout` nav slots), the header, the cookie banner, the MFA pages
  - `features/questionnaire.css`, banenkaart CSS/JS, `app-core.js`
  - the employer talent-pool pages (except the decline-reason label in 04)
  - token prices and logic
  - the paspoort stack's files beyond the reuse named in Dependencies
- **PR description:** what changed and why · the B/T/H bug ids closed · screenshots desktop 1440 + mobile 390 of each changed screen (nl, plus one `ar` RTL screenshot from 02 on) · test list · "Out of scope / deferred".

## §S. Scene and lobster contract (02 builds, 03–05 reuse)
- **`CareerClimbScene.razor`** (`Components/Candidate/Journey/`): the page background layer.
  - Parameters: `Variant` (`Climb`, `Listen`, `Stones`), `Mobile`, `Stones` (list of `ClimbStone(Label, State: Done|Now|Todo|Dream, …)`), `CurrentIndex`, `Celebrate`.
  - The gradient is light at the top and deep at the bottom: `--sky 0%, --sea-1 16%, --sea-3 45%, --sea-6 78%, --sea-8 100%`.
  - Light rays come from the top inline-end corner towards the dream stone (`--sun`, opacity .07; .10 when `Celebrate`), specks in the deep, seaweed at the bottom (desktop only) and bubbles near the lobster.
  - Geometry = `build.py` `DPTS`/`MPTS` (desktop zone 404×748, five stones zig-zagging up; mobile band 390×150, stones rising to the end side). Stones use the `stone()` shape; the dream stone gets a `--sun` glow + a `--gold-light` rim + a gold shell outline above it.
  - Every passed stone carries the old-shell shard (`SHARD`). The trail is solid `--gold-light` up to the lobster and dotted `--surface` beyond.
  - **RTL:** the SVG layer is mirrored with `transform: scaleX(-1)` under `[dir="rtl"]`; labels are HTML and are not mirrored; their position uses `inset-inline-start`.
- **Lobster:** `JourneyLobster` (Dependency A) with `plates shed`, `newShell`, `fallingPlate`, `size`, `label`.
  - Career sizing: desktop `118 + 14 × completedSteps` px (max 190), mobile `62 + 8 × completedSteps` (max 100). During the completion moment it's 170 / 86 with `newShell = true` and `fallingPlate` set.
  - The plate count shown is **not** the ontdekkingsreis count: career uses `platesShed = min(10, 4 + 2 × completed)` so the lobster visibly loses old shell as it climbs (D7).
  - Positioned on the current stone via CSS custom properties (`--x`, `--y`, `--size`).
- **Bubble:** `LobsyBubble` (Dependency A2) with the white "scene" variant (`.bubble`, radius 14/14/14/4, tail flips in RTL). One bubble per screen; text keys `Career.Say.*`.
- **Rail:** reuses the ontdekkingsreis rail markup/CSS (`journey-rail`) with zones "In de diepte · waar je nu bent", "De klim", "Naar het licht". Rows: Nu (done), steps (done "Nieuwe schaal" / now lobster marker "Groeit nu · {level}" / todo number), goal (gold star, "Jouw droombaan").
  - Footer: gold segments = completed steps, a brand outline segment = current, pearl = future, with "{n} van {total} nieuwe schalen" and "Alles is bewaard. Wisselen mag altijd.".
  - Mobile has no rail; the band + stepper carry it.
- **Growing-shells stepper:** `GrowingShellsStepper.razor`, the paspoort 04.2 rules (done `--brand` + check; current `--accent-soft` + dashed `--brand` "Groeit nu"; future dotted `--border`; goal `--gold-soft`/`--gold` star). Sizes 30 → 48 px from Nu to Doel (mobile −4 px). Labels = plan step short titles. If paspoort 04 already built a stepper, reuse it and add only what's missing (Dependency C).

## §L. Languages (every file)
- nl is the source and is **final as written in these files** (B1: short sentences, "je", no jargon, no "AI" in headings).
- en/pl/ro/ar are written by you in the same plain B1 register. Keep placeholders (`{0}`) and the word **Lobsy** untranslated; job titles from the Dutch catalog stay Dutch (D12).
- Each file lists its new keys in the PR body under "Strings for native review" (06 compiles them into one list for Dennis). **pl/ro/ar texts are machine-quality until a native speaker checks them** (flag for Dennis).
- **RTL (`ar`):**
  - `MainLayout` already sets `dir="rtl"`. Use logical properties only; chevrons/arrows flip (`[dir="rtl"] .i-chevron{transform:scaleX(-1)}`); the scene SVG mirrors (§S).
  - Numbers stay Western digits, as elsewhere in the app. Dates and times use the current culture with the Europe/Amsterdam zone (04).
  - Test: a bUnit/Playwright check per page that `ar` renders `dir="rtl"`, has no horizontal overflow at 390 px, and the primary button is at the inline end.
- The AI plan text is generated in the UI language active at generation time and stored with `PlanLanguage` (D13).

## Decisions (defaults applied; Dennis can override any of them)
- **D1. Dream-job choice.** Up to 3 suggestions from the paspoort (Beroepen-test `SuperMatches`/`StrongChoices`, then werkveld wishes) + search in a job list (`CareerDreamCatalog` + vacancy category names). Free text only via "Ik vind mijn beroep niet", sanitized, and never sent to the AI as instructions. *(Dennis, 30-09)*
- **D2. Steps are completed by the candidate.** "Deze stap is klaar" is allowed without proof. "+ Heb ik al" is removed and **nothing is written to the profile** from `/carriere`. Proof is optional via "Voeg bewijs toe" (Bewijzen, Dependency F). *(Dennis, 30-09)*
- **D3. Changing the dream job keeps what you achieved.** Completed proofs stay in the paspoort and the new plan counts them. The old plan is archived and can be restored for 30 days. `LobsyFriendlyDialog`, never an automatic yes. *(Dennis, 30-09)*
- **D4. Band text everywhere, no %** on candidate career surfaces: `CareerFitBand` Good "Past goed" (≥ 75) · Fair "Past redelijk" (≥ 50) · NotYet "Past nog niet" (< 50) · Unknown (no band shown). "0 jaar" is never rendered. *(Dennis, 30-09)*
- **D5. Courses = paspoort 03 rules.** `CourseSlotRules`: best free first, at most 1 partner (with affiliate code, `rel="sponsored"`, disclosure). AI course names are never clickable. No curated free match ⇒ no course block, only the claw text. *(Dennis, 30-09)*
- **D6. `/carriere` is the full plan; the paspoort Carrière tab is the compact summary from the same `CareerPlanViewBuilder`.** The lobster's growth counts career steps (not the 10 ontdekkingsreis plates). Werkgevers OFF ⇒ no vacancy links or counts. *(Dennis, 30-09)*
- **D7. Metaphor = the climb** from the deep to the light, the old shell left on every passed stone, a gold new shell per step (§S). *(default, from the approved mockups)*
- **D8. No new feature flag.** The new `/carriere`, talent-contacts and hoe-werkt-lobsy replace the old pages for all candidates in one go. When the paspoort flag is OFF, "In je paspoort" reads "In je profiel" and links go to `/candidate/profile`. *(default, **flag for Dennis**)*
- **D9. Completion order.** Only the **current** step can be completed and only the **last** completed step can be undone (server 409 `complete_previous_first` / `undo_last_first`). Proof auto-completion (from Bewijzen) still works for any step, but only fills from the start (step n completes automatically only when 1…n−1 are complete). *(default)*
- **D10. Generation guard.** One generation in flight per candidate; the same dream within 10 minutes returns the existing plan (idempotent); **max 5 generations per rolling 24 h** (429 `generation_limit` → friendly text "Je kunt morgen weer een nieuw plan maken"). *(default, **flag for Dennis**: the number 5)*
- **D11. Archive retention** 30 days, then hard-delete by a hosted job. Max 3 archived plans per candidate (the oldest goes first). *(default)*
- **D12. Job catalog.** `CareerDreamCatalog` (Core, curated, ≥ 150 Dutch occupation titles incl. entry-level jobs relevant for labour migrants and school leavers: orderpicker, productiemedewerker, schoonmaker, zorghulp, kok, chauffeur …, each with MBO/HBO level and werkveld) + active `VacancyCategory` names. **Titles stay Dutch in every language** (they are Dutch job names; a translated gloss is deferred). *(default, **flag for Dennis**)*
- **D13. Plan language.** The AI plan text is generated in the UI language at generation time (`PlanLanguage` stored). If the UI language differs later, a quiet line offers "Maak je plan opnieuw in {taal}" (counts towards D10). *(default)*
- **D14. Talent contacts.**
  - "Ja" shows a confirm dialog with the exact name/e-mail/phone that will be shared.
  - The decline reason is stored (`NotInterested` / `AlreadyPlaced`) and shown to the employer as "Geen interesse" / "Al voorzien".
  - Sharing can't be undone (said in the dialog; a revoke flow is deferred).
  - Candidates can still answer after 48 h while the request is open. *(default, **flag for Dennis**: the revoke flow and the employer label)*
- **D15. Hoe werkt Lobsy (candidate)** = "five stones" in Dennis' nav order (Ontdekkingsreis · Paspoort · Carrière · Banenkaart · Sollicitaties), each with its own done state and a link. The stones follow the flags (Werkgevers OFF ⇒ 3 stones; paspoort OFF ⇒ "Profiel"; ontdekkingsreis absent ⇒ "Profiel invullen"). The guest/employer guides are unchanged apart from shared bug fixes. *(default)*
- **D16. Step fit band source.** Local `RoleFitCheckBuilder.Build` for the step's occupation title, **read-only** (never `EvaluateAsync`, which persists and may call OpenAI). No test data ⇒ `Unknown` (no band). *(default)*
- **D17. Deterministic step actions** replace the AI `ActionHref`/`ActionLabel`: "Bekijk opleidingen" (scroll to courses) · "Voeg bewijs toe" · "Vacatures voor deze stap" (Werkgevers ON) · "Deze stap is klaar". *(default)*
- **D18. Werkgevers OFF on contact requests.** `/candidate/talent-contacts` and its API are unreachable for candidates (standard gate page), the profile/paspoort link is hidden and no new candidate notifications are created. Nothing else in the talent pool changes. *(default)*

## Dependencies (check before 01; re-check A–E before 02, 03 and 04; say in the PR which case applied)
- **A. Ontdekkingsreis scene + lobster + tokens** (`docs/mijn-paspoort` 07). Check: `git grep -n "JourneyLobster\|JourneyScene" origin/acceptatie -- Jobsy.Web` and `git ls-tree origin/acceptatie Jobsy.Web/wwwroot/css/features/ontdekkingsreis.css`.
  - **Present:** reuse `JourneyLobster` and the sea tokens from `features/ontdekkingsreis.css`. `CareerClimbScene` is new (02) and uses only those tokens. Don't copy tokens.
  - **Absent:** 02 builds `Components/Candidate/Journey/JourneyLobster.razor` exactly per paspoort 07 (same name, parameters and plate paths from `build.py` `PLATES`/`BODY`/`CRACKS`/`SHARD`, mascot `mascot-256.webp`). It also builds the scene tokens in a **separate** `wwwroot/css/features/journey-tokens.css` (the `--sea-*`, `--sky`, `--sand`, `--sun`, `--rock`, `--weed`, `--shell-old` definitions, verbatim). Say in PR 02 that paspoort 07 must reuse both instead of redefining them. Never branch from the paspoort branches.
- **A2. `LobsyBubble`** (paspoort 02). Check: `git grep -n "LobsyBubble" origin/acceptatie -- Jobsy.Web`.
  - **Present:** reuse it; add the `Variant="Scene"` style if missing.
  - **Absent:** 02 builds `Components/Shared/LobsyBubble.razor` per paspoort 02 (mascot 40 px + `accent-soft` bubble, `Variant` = `Inline | Scene`) and says so in the PR.
- **B. Courses** (paspoort 03). Check: `git grep -n "class CourseSlotRules\|CourseSuggestionBlock" origin/acceptatie -- Jobsy.Core Jobsy.Web` and `git grep -n "IsFree\|IsPartner" origin/acceptatie -- Jobsy.Core/Entities/TrainingEntities.cs`.
  - **Present:** 03 uses `CourseSuggestionBlock` with the step context (`CourseSlotRules.Pick`), as paspoort 04.2 does.
  - **Absent:** **don't** build a second course system. 03 renders no course block; the step shows the claw lines and the plan's course names as **plain text** "Wat kan helpen: …" (not clickable, no provider). `TrainingOffersBlock` is **not** added to `/carriere`. Say in PR 03 that paspoort 03 will switch it on (a `TODO(paspoort-03)` at one seam: `CareerStepCourses.razor`).
- **C. Paspoort Carrière tab + `CareerPlanViewBuilder` + `RoleFitBandRules`** (paspoort 04). Check: `git grep -n "class CareerPlanViewBuilder\|class RoleFitBandRules\|GrowingShells\|PassportCareer" origin/acceptatie -- Jobsy.Core Jobsy.Web`.
  - **Present:** 01 extends `CareerPlanViewBuilder` with the new fields (archive, band, actions) and keeps the paspoort tab working (its tests stay green). The band uses `RoleFitBandRules`. 02 reuses its shell stepper if it exists.
  - **Absent:** 01 extracts `CareerPlanViewBuilder` (Web, pure) from `CareerDashboard.razor` ~L390–650 with the view model paspoort 04.2 needs, and adds `CareerFitBandRules` in Core (≥ 75 / ≥ 50 / else, same numbers as `RoleFitBandRules`). Its doc comment says that `RoleFitBandRules` must delegate to it. Say in PR 01 that paspoort 04 must consume both.
- **D. Werkgevers actief / `IFeatureFlags` / `RequiresFeature`** (paspoort 01). Check: `git grep -n "interface IFeatureFlags\|class RequiresFeatureAttribute\|FeatureRouteGate" origin/acceptatie -- Jobsy.Core Jobsy.Web`.
  - **Present:** use `IFeatureFlags.IsEnabled(PlatformFeature.Employers)` for every gate here; `/candidate/talent-contacts` + `api/me/talent-contacts*` get `[RequiresFeature(PlatformFeature.Employers)]`; `FeatureRouteGate` shows its standard page.
  - **Absent:** add a one-method seam `ICareerEmployerGate` (Web + Api) that returns **true** today (employers always on), with the `// replace with IFeatureFlags (paspoort 01)` comment. Every gate in this stack goes through it, so paspoort 01 changes one class. Tests cover both values.
- **E. Kandidaat-banen fit** (`docs/kandidaat-banen` 04/08). Check: `git grep -n "class CandidateFitDisplay\|class CandidateFitGate\|FitBand" origin/acceptatie -- Jobsy.Core Jobsy.Api`.
  - **Present:** the "Vacatures voor deze stap" link may carry a count "{n} vacatures passen goed" = vacancies for the step's occupation keys with `FitBand ≥ Good` for this candidate, **only when** `CandidateFitGate.IsOpen`. The link opens the banenkaart/list with the step query. Vacancy band labels are the kandidaat-banen ones.
  - **Absent:** link only (`/?q={keys}`), no count, no vacancy bands.
  - Either way the career band (D4) is about the occupation, not about vacancies; the two never mix in one pill.
- **F. Bewijzen** (paspoort 05). Check: `git grep -n "CertificatesSection\|ProofStrengthRules" origin/acceptatie -- Jobsy.Web Jobsy.Core`.
  - **Present:** "Voeg bewijs toe" opens the paspoort Bewijzen tab at "Certificaat toevoegen", prefilled with the course/diploma name (`?tab=proof&add=certificate&name=…`).
  - **Absent:** it opens today's profile certificates section (`/candidate/profile#certificates`, prefill via query) using the existing certificate form. The candidate enters the year; nothing is saved without their submit.
- **G. Nav add-on** (Dennis' order, separate). Nothing to check: this stack **never** edits the nav. If the add-on has landed, the Carrière item is already active on `/carriere`; the hoe-werkt-lobsy stones read their order from D15, not from the nav.
- **Landing order:** any. Every case above has a fallback. Recommended after paspoort 01–07 if they are close to merging. Never branch from an unmerged branch of another stack.
