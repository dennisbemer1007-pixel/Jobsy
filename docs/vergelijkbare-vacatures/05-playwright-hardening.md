> **Warnings:** deliver a Release build with **0 warnings** (`TreatWarningsAsErrors=true`). Run `dotnet format` and `.github/scripts/count-build-warnings.sh` before opening the PR. No blanket `<NoWarn>`.

# Step 05: Playwright, hardening, docs, runbook

**Branch:** `git fetch origin && git checkout -b cursor/vv-05-hardening origin/cursor/vv-04-analytics`. **PR base:** `cursor/vv-04-analytics`. Title: `test(vv-05): Meer rotsen Playwright + runbook + docs`.

Read [00-README.md](00-README.md). Unit and bUnit tests live in steps 01–04. This step adds the browser tests, closes the gaps found while testing, and writes the docs.

## 0. Rules (read first, they apply to this step)
- **Stacked PRs.** One PR for this step only. Branch and base are given in the header above. Each step's PR targets the previous step's branch (01 targets `acceptatie`), so the chain is `acceptatie ← 01 ← 02 ← 03 ← 04 ← 05`. If the parent branch moves, **merge** the parent into your branch (no rebase of pushed commits).
- **Never merge and never deploy.** Do not merge any PR, do not trigger a Render deploy, and do not use the shortcuts `123`, `456` or `999` from `.cursor/rules/`. Dennis merges.
- **Never push to `main` or `acceptatie`. No force-push** of any kind (`--force` and `--force-with-lease` included).
- **Red tests: stop with a draft PR.** If any test is red and you cannot fix it inside the scope of this step, open the PR as **draft**, list the failing tests and your findings in the PR body, and stop.
- **Warnings.** Release build with 0 warnings: `dotnet build Jobsy.sln -c Release` with `TreatWarningsAsErrors=true` (already set in `Directory.Build.props:10`). Run `dotnet format` and `.github/scripts/count-build-warnings.sh` before opening the PR. Fix any warning you hit **in the same PR**, even if it is old. No blanket `<NoWarn>`, no `#pragma warning disable` without a one-line reason.
- **Serious bugs elsewhere go in a standalone hotfix.** If you find a serious bug outside this feature (security, data loss, crash, wrong money), do not fix it in this PR. Open a separate branch `cursor/hotfix-<topic>` from `origin/acceptatie`, a separate PR into `acceptatie`, and link it from this PR body.
- **Code references** below were checked on `origin/acceptatie` @ `32f47798` (2026-10-02 20:50 CEST). Re-check line numbers before editing, since they move.
- **Mockups** live on branch `docs/vergelijkbare-vacatures`, folder `docs/mockups/vergelijkbare-vacatures/`. The mockup CSS uses raw hex and inline styles for speed. Do **not** copy that; rebuild with design tokens (`.cursor/rules/design-system.mdc`).
- **Privacy.** Never put names, e-mail addresses, phone numbers, street addresses, dates of birth or user ids into embedding input, logs or analytics.

## 1. What exists today (verified)
| Topic | Where | Fact |
|---|---|---|
| CI smoke stack | `.github/workflows/pr-tests.yml:25-31` (`JOBSY_E2E_BASE_URL=http://127.0.0.1:5201`), `.github/scripts/start-ci-stack.sh` (Development + seed, API :5200, Web :5201) | Step 01 adds pgvector to the service container and `Embeddings__Provider=fake`. |
| Allowlist | `pr-tests.yml:70-104` | One `--filter-class '*XxxPlaywrightTests'` per class (OR'd). Screenshots upload from `artifacts/playwright-smoke/**` (`:113-120`). |
| Test contract | `Jobsy.Tests/CarrierePlaywrightTests.cs:11-30`, `Acc2709PlaywrightTests.cs:130-150` | `[Collection("PlaywrightSmoke")]`, soft-skip without `JOBSY_E2E_BASE_URL`, `PlaywrightCookieConsent.AcceptAsync(context)`, candidate login via `JOBSY_E2E_CANDIDATE_EMAIL/PASSWORD`, default seed `kandidaat@jobsy.local`. |
| Docs | `docs/FUNCTIONELE_SPECIFICATIES_MATCHING.md`, `docs/ROUTES.md`, `docs/runbooks/` | |

## 2. Make CI deterministic
- In `start-ci-stack.sh`, after the seed is ready, run `dotnet run --project Jobsy.Api/Jobsy.Api.csproj -c Release --no-build -- embeddings backfill` once (fake provider), so vectors exist before the browser tests. Log the `embeddings status` output.
- Make sure the seeded candidate `kandidaat@jobsy.local` has current test/AI consent and enough passport signal for a candidate vector. Seed it if not (Development seed only, flagged `IsTestData`), so personal mode is exercised.
- Pick the test vacancy by API (`GET api/vacancies/discover` → first public id with ≥ 3 neighbours), never a hard-coded GUID.

## 3. `Jobsy.Tests/MeerRotsenPlaywrightTests.cs`
`[Collection("PlaywrightSmoke")]`, soft-skip without `JOBSY_E2E_BASE_URL` (same contract as `CarrierePlaywrightTests`). Accept cookies with `PlaywrightCookieConsent`. Screenshots go to `artifacts/playwright-smoke/vv/{name}.png`, inside the existing upload path.

Run each flow at **390×844** (mobile, `IsMobile`, `HasTouch`, DPR 2) and **1440×900** (desktop): `[Theory]` with a viewport parameter.

| # | Flow | Asserts |
|---|---|---|
| F1 | Anonymous: open vacancy detail | Entry button visible inside `[data-testid=kb-detail-rail]`; title "Meer rotsen zoals deze"; sub "Lijkt op deze baan"; bounding box height ≥ 44; **no horizontal overflow** (`document.documentElement.scrollWidth <= document.documentElement.clientWidth` and the same for `document.body`). |
| F2 | Anonymous: open sheet | `role=dialog` visible; mobile = bottom sheet (top > 0, bottom == viewport height ± 1); desktop = right panel (left ≥ 1440 − 460 − 1); 1–6 rows; each row exactly 1 `.similar-chip`; viewed vacancy id not among the hrefs; CTA visible and links to `/ontdek`; no horizontal overflow inside the dialog (`scrollWidth <= clientWidth` on the dialog). |
| F3 | Keyboard | Focus the entry, Enter opens, focus is inside the dialog, 20× Tab stays inside, Esc closes, `document.activeElement` is the entry button again. |
| F4 | Candidate login | Sub "Lijkt hierop en past bij jou"; why line is the personal text (or the vacancy text if the candidate has no vector, then fail with a clear message: CI seed is wrong); no CTA when personal; no row links to a vacancy the candidate applied to (apply to one first via the API helper, if one exists, else skip with a message). |
| F5 | Result click | Click row 1, URL becomes `/vacancies/{id}` of that row; back button returns to the detail page (sheet closed). |
| F6 | Closed vacancy | A closed vacancy page (410) has **no** entry button. |
| F7 | Switch off | Only when an admin token for the smoke stack is available: set `SimilarVacanciesEnabled=false`, reload, button gone, set back in `finally`. Otherwise soft-skip with a message. |
| F8 | RTL | `?culture=ar` (or the existing culture switch): the sheet opens, no horizontal overflow, the close button sits on the inline-start side. |
| F9 | Fallback | Only when the test can restart the stack with `Embeddings__Provider=none`: skip otherwise. The unit/endpoint tests from step 02 cover this already. |

Add `--filter-class '*MeerRotsenPlaywrightTests' \` to the allowlist in `pr-tests.yml` (keep the trailing `\` chain valid; the last line has none).

## 4. Hardening checklist (fix in this PR if small, else a hotfix branch per rules §0)
- Long titles and company names (60+ chars, Polish/Romanian chips) ellipsize; no overflow at 320 px.
- Slow API (add a 2 s delay in a dev-only test hook, or throttle in Playwright with `route`): loading state shows, double clicks do not fire 2 requests.
- The sheet closes on navigation (Blazor enhanced navigation must not leave the body scroll-locked).
- `prefers-reduced-motion` honoured (Playwright `ReducedMotion = Reduce` → no transition on the sheet).
- No console errors and no failed requests (other than analytics without consent) during F1–F5: collect `page.Console` / `page.RequestFailed` and assert empty.
- Lighthouse-style check not required; keep CLS stable: the entry button reserves its height in prerender.

## 5. Docs
- `docs/FUNCTIONELE_SPECIFICATIES_MATCHING.md`: new section "Meer rotsen zoals deze (vergelijkbare vacatures)". Cover the modes, hard filters, score formula + admin weight, minimum similarity, reason-code priority, fallback, privacy (embedding input content-only, consent, no PII in analytics), and the difference from the 410 `/similar` list.
- `docs/ROUTES.md`: `GET api/vacancies/{id}/more-like-this`, `POST api/analytics/similar-vacancies`, `GET api/admin/similar-vacancies/usage`.
- New runbook `docs/runbooks/embeddings-backfill.md`:
  1. Check the extension: `SELECT extname, extversion FROM pg_extension WHERE extname = 'vector';` (Render Postgres 16 supports `vector`; the migration from step 01 creates it).
  2. Check the config: the `OpenAI__ApiKey` env var is set on the API service, and `Embeddings__Provider=openai`.
  3. Render one-off job / shell on the API service: `dotnet Jobsy.Api.dll embeddings backfill --dry-run` → check counts and the estimated tokens/USD. Then `dotnet Jobsy.Api.dll embeddings backfill`.
  4. `dotnet Jobsy.Api.dll embeddings status` → coverage. Check the cost in PlatformLogs category `openai.embeddings`, or on the admin usage panel.
  5. Model change: set `Embeddings__Model`/`Dimensions`, deploy (dimension changes need a migration), run backfill. Old rows are ignored by the `Model`/`Dimensions` filter.
  6. Rollback: admin switch `SimilarVacanciesEnabled` off (instant). Embeddings can stay; they are not visible anywhere.
  7. A high fallback share on the admin panel means: check the key, the OpenAI status, and `embeddings status`.
- Do **not** run the runbook against production as part of this PR (acceptatie only, and only when Dennis asks).

## 6. Acceptance
- CI green, including `MeerRotsenPlaywrightTests` (both viewports). Screenshots are in the `playwright-screenshots` artifact under `vv/`.
- Release build 0 warnings.
- PR body:
  - the F1–F9 result table (ran / soft-skipped + reason)
  - the screenshots next to `docs/mockups/vergelijkbare-vacatures/*.png`
  - p95 timing of `more-like-this` from 50 calls on the CI stack (budget < 300 ms)
- When this PR is green, report the chain (01→05 PR links) to Dennis. **Do not merge anything.**
