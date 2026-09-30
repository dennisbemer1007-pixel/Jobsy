# 06: Playwright E2E, cleanup, docs and the stack-end report

> **Rules (repeated in every file):**
> - Branch from `origin/acceptatie` (file 01) or from the previous file's branch (stacked). ONE PR per file, always into `acceptatie`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`). Never push to `main` or `acceptatie`. No force-pushes. Push only `cursor/carriere-*` branches.
> - Red build/tests or an unmet success criterion: push, open that PR as **draft**, stop and report. Don't start the next file.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

| | |
|---|---|
| Branch | `cursor/carriere-6` from `cursor/carriere-5` |
| PR title | `Carrière 06: E2E (desktop + mobile, 5 languages, RTL, reduced motion, Werkgevers OFF), cleanup, docs, stack report` |
| Body starts with | `Stacked on #<PR 05> (cursor/carriere-5)` |
| Mockups | all `cr-*` (visual comparison in the report) |
| Split if too big | `06a` = §1 E2E, `06b` = §2–§4 cleanup + docs + report |

**Goal:** prove the whole stack works end to end on desktop 1440 and mobile 390, in all 5 languages including `ar` RTL, with reduced motion and with Werkgevers OFF. Remove what the stack made unused. Update the docs and hand Dennis one clear report.

## 1. Playwright E2E
- New `Jobsy.Tests/CarrierePlaywrightTests.cs`, same conventions as `Acc2709PlaywrightTests`:
  - `[Collection("PlaywrightSmoke")]`, soft-skip without `JOBSY_E2E_BASE_URL`
  - seed users `kandidaat@jobsy.local` / `valentine@jobsy.local` with the seed password from `DemoUsersSeeder`, never hardcoded new secrets
  - viewports 1440×900 and 390×844; `ReducedMotion = Reduce` in one context
- Test data: a candidate without a plan must exist for the empty-state flow. Prefer a dedicated E2E seed user created by the existing test seeding (`DemoUsersSeeder` pattern, **Development/E2E only**). If none can be added safely, reset via the public API (archive the active plan via a new dream + restore at the end) and say which path you used.
- **Flows** (each as its own test; screenshots saved under `artifacts/e2e/carriere/` and attached to the PR):
  1. **Empty → choose → overview:** `/carriere` shows one h1 "Waar wil jij naartoe groeien?" and no "Stip op de horizon"/placeholder h2. Pick a suggestion (or search "kok" → select). "Maak mijn groeiplan" (double click ⇒ one request; count network calls). The overview shows the stepper with real titles and **no `%`** anywhere in the main content.
  2. **Step → complete → moment → undo:** open the current step; no "0 jaar"; no `<a>` around AI course names; "Deze stap is klaar" ⇒ the done card "Je nieuwe schaal past", the live region text, no toast; "Toch nog niet klaar" ⇒ the step is open again.
  3. **Order rule:** a later step has no complete button; a direct API call to complete it returns 409 `complete_previous_first`.
  4. **Change dream + restore:** dialog open → Esc ⇒ the plan is unchanged. Pick another dream → "Maak nieuw plan" ⇒ the carried-over line appears and the earlier completed step still counts where it applies. "Eerdere plannen" → "Zet terug" ⇒ the old plan and its progress are back.
  5. **No self-claim:** `POST api/me/career-path/courses/claim` ⇒ 410; the profile certificates are unchanged (read before/after).
  6. **Talent contacts** (needs a Pending request for the E2E candidate: create it via the employer API with an E2E employer seed user if available; else soft-skip with a clear message and cover it in bUnit):
     - the h1 is visible at 390 and 1440
     - "Ja, deel mijn gegevens" opens the dialog listing name/e-mail/phone; "Nog niet" sends nothing (no POST observed)
     - confirm ⇒ "Je zei ja"; a second request declined with "Ik heb al werk" ⇒ "Je zei nee"
     - the employer view shows "Al voorzien"
  7. **Hoe werkt Lobsy:** no text "_message"; the stones in D15 order; "Ik snap het" stays on the page; the primary goes to the `now` stone. An employer user opening `/candidate/hoe-werkt-lobsy` gets no candidate API call and ends on the right page.
  8. **Languages:** for each of `nl`, `en`, `pl`, `ro`, `ar` (switch with the existing language selector or culture cookie):
     - `/carriere`, `/candidate/talent-contacts`, `/candidate/hoe-werkt-lobsy` render without missing-key markers (whatever `CultureState` shows for missing keys)
     - no horizontal overflow at 390 (`scrollWidth <= clientWidth + 1`)
     - `ar`: `html[dir=rtl]`, the scene has the mirrored class, the primary button's `getBoundingClientRect().left` < the secondary's (inline end in RTL)
  9. **Reduced motion:** with `ReducedMotion = Reduce`, completing a step shows the new shell at once. No running CSS animations on `.journey-lobster`/`.journey-plate` (`getAnimations().length === 0`).
  10. **Werkgevers OFF** (only when Dependency D is present and the E2E admin can toggle it; restore the value in `finally`):
      - `/carriere` has no vacancy link or count, not even in the done card
      - `/candidate/talent-contacts` shows the gate page; the hoe-werkt page shows 3 stones
      - D absent ⇒ covered by bUnit with the gate fake; say so.
  11. **Focus:** after client navigation to each page, the h1 is focused and its computed `outline-style` is `none` for mouse navigation. After a Tab, the focused element has a visible outline.
- Run locally against the Development server if you can. Otherwise the tests soft-skip in CI. The report says which ran.

## 2. Cleanup
- Remove, only when `git grep` shows no users:
  - `HorizonArt`, `career-dash`/`horizon-*` rules in `app.css` (+ regenerate `app.min.css` the way the repo does)
  - unused `CareerDash.*` and candidate `Talent.*`/`HowLobsy.Candidate.*` keys (all 5 languages), and the old JS confirm function
  - the obsolete `courses/claim` stub **stays** (410) with a `// remove after <date + 30 days>` comment. Say so in the report; removing it is a later task.
- `docs/i18n/untranslated-baseline.txt` must not grow; `LocalizationParityReportTests` green.
- Update the review lists: `docs/i18n/candidate-unused-keys.md` only if the repo's process expects it.

## 3. Docs
- `docs/ROUTES.md`: `/carriere` (new `?stap=`), `/candidate/talent-contacts` (gate), `/candidate/hoe-werkt-lobsy` (Candidate-only). New API routes: `dream-options`, `archived`, `archived/{id}/restore`, `talent-contacts/{id}/share-preview`, `journey-summary`. `RoutesDocFreshnessTests` green.
- `docs/TESTSCENARIOS_PER_ROL.md` + `docs/testscenarios-per-rol.csv`: candidate scenarios for the flows in §1 (nl), in the file's existing format.
- `CHANGELOG.md`: one entry per PR of this stack (nl, B1, user-facing).
- `docs/FUNCTIONELE_SPECIFICATIES_LOBSY_PLATFORM.md`: update the Carrière / contact requests sections if they exist (archive 30 days, no self-claim, band texts, confirm before sharing).

## 4. Stack-end report (PR body of 06 **and** as `docs/reports/carriere-stack-report.md` on this branch)
- **Table:** file → branch → PR number → status (open / draft + reason) → the B/T/H ids closed.
- **Dependencies:** the A–G outcome per file (present/absent, fallback used, the seams left for paspoort: `TODO(paspoort-03)`, `ICareerEmployerGate`, `journey-tokens.css`, `CareerFitBandRules`).
- **Screenshots:** desktop 1440 + mobile 390 per screen next to the matching `cr-*` mockup (nl), plus `ar` for `/carriere` overview and contacts.
- **Strings for native review:** every new/changed key with the nl source and the en/pl/ro/ar text, as a table (or a CSV at `docs/i18n/carriere-review.csv`), so Dennis can have pl/ro/ar checked by native speakers (README §L).
- **Decisions to confirm (flagged in the README):**
  - D8 (no feature flag, one big switch)
  - D10 (5 plans per 24 h)
  - D12 (Dutch job titles in every language; catalog size)
  - D14 (no revoke after sharing; employer label "Al voorzien")
- **Deferred:**
  - global h1 focus rule
  - removing `courses/claim` (410 stub) and the `MatchPercent` column
  - a translated gloss for job titles
  - a revoke flow for shared contact details
  - admin setting for the generation limit
  - anything the E2E soft-skipped and why
- **Measured:** the number of AI generations in the E2E run (should equal the number of explicit "Maak …" clicks), and page weight of `carriere.css` (keep < 25 kB unminified).

## Tests
- `dotnet test` all green, including the new Playwright file (soft-skip allowed without the env var) and every guard test from 02/04/05.

## Success criteria
- Every flow in §1 passes where it can run, and every soft-skip is explained.
- No unused code or keys from the old pages remain (except the documented 410 stub).
- Docs and the report are complete. The report is the last thing Dennis needs to read.
