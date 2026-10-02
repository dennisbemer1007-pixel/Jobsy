# Vergelijkbare vacatures: "Meer rotsen zoals deze" (Cursor spec)

Approved by Dennis on 2026-10-02. This folder is a stacked set of Cursor prompts. Run them **in order**, one PR per step.

On the vacancy detail page (`/vacancies/{id}`), a new quiet button **"Meer rotsen zoals deze"** (subtext *"Lijkt hierop en past bij jou"*) opens a sheet with up to 6 vacancies. These are similar to the viewed vacancy **and** fit the candidate's passport. The ranking uses OpenAI embeddings stored in PostgreSQL with pgvector. Without OpenAI it falls back to plain "same kind of work nearby" results and never shows an error.

Base: `origin/acceptatie` @ `32f47798` (2026-10-02 20:50 CEST). Stack: .NET 10, Blazor Server (`Jobsy.Web`) + API (`Jobsy.Api`) + PostgreSQL 16 with PostGIS on Render.

## Steps

| # | File | Branch (base) | What it delivers |
|---|---|---|---|
| 01 | [01-pgvector-embeddings.md](01-pgvector-embeddings.md) | `cursor/vv-01-embeddings` (`acceptatie`) | pgvector extension + side tables, embedding text builders (no PII), OpenAI embedding client with usage/cost logging, background queue + sweep, `embeddings backfill` CLI, CI image fix. No UI. |
| 02 | [02-api-ranking-terugval.md](02-api-ranking-terugval.md) | `cursor/vv-02-api` (`cursor/vv-01-embeddings`) | `GET api/vacancies/{id}/more-like-this`: hard filters, 0.5/0.5 ranking, deterministic reason codes, fallback that never errors, cache. |
| 03 | [03-ui-knop-sheet.md](03-ui-knop-sheet.md) | `cursor/vv-03-ui` (`cursor/vv-02-api`) | Button in the detail rail, mobile bottom sheet / desktop side panel, reason chips, discovery CTA, NL/EN/PL/RO/AR strings, new CSS file. |
| 04 | [04-analytics-admin.md](04-analytics-admin.md) | `cursor/vv-04-analytics` (`cursor/vv-03-ui`) | PII-free usage counters (open, result click, CTA click), admin weight setting + on/off switch, usage panel. |
| 05 | [05-playwright-hardening.md](05-playwright-hardening.md) | `cursor/vv-05-hardening` (`cursor/vv-04-analytics`) | Playwright mobile 390 + desktop 1440 (no horizontal overflow, keyboard, focus), CI allowlist, docs, runbook. |

Unit tests are part of every step. Step 05 only adds browser tests and final hardening.

## Working rules (restated as §0 in every step file)
One stacked PR per step (`acceptatie ← 01 ← 02 ← 03 ← 04 ← 05`). Never merge or deploy. Never push to `main` or `acceptatie`, and never force-push. Red tests: stop with a draft PR. The Release build must have 0 warnings under `TreatWarningsAsErrors`; fix warnings in the same PR. Serious bugs found along the way go in a standalone `cursor/hotfix-<topic>` PR into `acceptatie`.

## Mockups (`docs/mockups/vergelijkbare-vacatures/`)

These were rendered from the **live acceptatie page** (`/vacancies/a1000000-…-049`, "Allround Westland Fresh") with the proposed parts injected. The real header, travel card, sticky "Solliciteer" bar and tokens are used. All result data is **Voorbeelddata**. In the logged-in shots the header icons and bottom nav are approximated.

| File | Shows |
|---|---|
| `vv-mobiel-1-knop.png` (390×844 @2x) | Button right under "Hoe kom je er?" (logged-in candidate, bottom nav + sticky apply visible). |
| `vv-mobiel-2-sheet-paspoort.png` | Bottom sheet, personal mode: 6 results, travel minutes, one reason chip each. |
| `vv-mobiel-3-sheet-uitgelogd.png` | Logged out: vacancy-only results, distance in km, CTA "Start de ontdekkingsreis". |
| `vv-mobiel-4-sheet-terugval.png` | Fallback (OpenAI/pgvector unavailable): same kind of work nearby, no error. |
| `vv-desktop-1-knop.png` (1440×900) | Button in the right rail under Solliciteer / Bewaar / Delen. |
| `vv-desktop-2-paneel.png` | Right side panel (460 px) with 6 results; the vacancy stays visible. |
| `vv-sheet-*.html` | Clean component HTML (no live scripts) for the three sheet states. |
| `mock.py` | Generator: `python3 mock.py` (Playwright + Chrome, reads the public acceptatie page). |

Checks on the rendered mockups: `scrollWidth == viewport` at 390 and 1440 (no horizontal overflow), and every button/link in the sheet is at least 44 px tall.

## Decisions (defaults; Dennis can change any of them)

| # | Decision | Default |
|---|---|---|
| D1 | Vector store | pgvector on the existing Render Postgres (`CREATE EXTENSION vector` via EF migration). Packages `Pgvector.EntityFrameworkCore` **0.3.0** (EF Core 9/10, needs Npgsql EF ≥ 9.0.1; we use 10.0.3) + `Pgvector` 0.3.2. |
| D2 | Where vectors live | **1:1 side tables** `VacancyEmbeddings` (PK `VacancyId`) and `CandidateProfileEmbeddings` (PK `UserId`) with `vector(1536)`, not extra columns on `Vacancies`/`Users`. The discovery index loads every vacancy row (`VacancyDiscoveryIndex.RefreshAsync`), and 6 KB per row would slow that down for nothing. Model, hash and version metadata stay next to the vector. See open question 4. |
| D3 | Model | `text-embedding-3-small`, 1536 dims, configurable (`Embeddings:Model`, `Embeddings:Dimensions`). Changing the dimension needs a migration; a mismatch disables the feature with a warning. |
| D4 | Embedding input | Content only. Vacancy: title, description (= tasks, scrubbed), culture pillars, kernwaarden cards, branche/category, engagement labels. Candidate: values, interests (RIASEC), strengths (competencies), experience (roles, functions, education, dream job). **Never** names (person, company, employer, contact), e-mail, phone, address, date of birth or age. |
| D5 | Consent | A candidate vector is only made with current test/AI consent (`CandidateConsentRules.HasCurrentTestAiConsent`) and `CanUseCandidateFeatures`. Otherwise vacancy-only ranking. The vector is deleted on account deletion and on consent withdrawal. |
| D6 | Freshness | SHA-256 content hash. Unchanged text means no OpenAI call. Changes are queued on save, and a sweep every 10 min catches every other write path (CSV, ATS, API, admin). Backfill CLI for the first run. |
| D7 | Ranking | Hard filters first (public + verified publisher, not the same vacancy, not applied, within the candidate's travel time, legal age), then `score = 0.5 × sim(viewed) + 0.5 × sim(passport)`. Weight in admin settings. Logged out or no passport: `sim(viewed)` only + CTA. Max 2 per company, near-duplicates removed, minimum similarity 0.25 to the viewed vacancy. |
| D8 | Travel for logged-out users | Banenkaart origin when the page has one (same as the 410 page), otherwise 25 km around the viewed vacancy. |
| D9 | Reason chip | Exactly one per result, deterministic, no LLM per view. Order: Dichter bij huis → Past bij je waarde → Past bij je sterkte → Ook buiten werken → Ook flexibele tijden → Geen ervaring nodig → Zelfde inzet → Zelfde branche → Zelfde soort werk. Max 2 equal chips per list when another applies. |
| D10 | Fallback | OpenAI down, no key, no vector yet or pgvector missing: the same hard filters, then same category/branche, nearest first. The API never returns 5xx for this; the UI shows results or a friendly empty state. |
| D11 | UI | Quiet secondary tile in the detail rail (visible on mobile and desktop; the rail actions are hidden below 1024 px, so it sits **after** `.kb-detail__rail-actions`). Mobile: bottom sheet (88 vh). ≥ 900 px: right side panel (460 px). Solliciteer stays the only primary action. |
| D12 | Copy | Button "Meer rotsen zoals deze" / "Lijkt hierop en past bij jou". Logged-out / no passport subtext "Lijkt op deze baan" (open question 1). All 5 UI languages (nl, en, pl, ro, ar; the `Add` helper requires all five). PL is real Polish. |
| D13 | Analytics | Daily aggregate counters only (date, event, mode, reason code, position). No user id, anonymous key, IP or vacancy id of the viewer. Admin sees opens, result clicks, CTR, CTA clicks and mode mix for the last 30 days. |
| D14 | Cost | Every embeddings call logs model, input count, tokens, estimated USD (`Embeddings:UsdPerMillionTokens` = 0.02) and duration, both to `ILogger` and to `PlatformLogs` (category `openai.embeddings`). Expected cost is about $0.01 for a full backfill of 1,000 vacancies. |
| D15 | CI | `pr-tests.yml` uses `postgis/postgis:16-3.4`, which has **no pgvector**. Step 01 installs `postgresql-16-pgvector` into the service container. CI/dev use a deterministic fake embedding provider (`Embeddings:Provider=fake`), so the whole path runs without OpenAI. The fake is refused in Production. |

## Open questions for Dennis (with recommendation)

1. **Subtext for logged-out users and users without a passport.** "Past bij jou" is not true without a passport. *Recommendation:* show "Lijkt op deze baan" there and keep "Lijkt hierop en past bij jou" for candidates with a passport (as mocked).
2. **Privacy and consent.** The passport summary goes to OpenAI to compute a vector. *Recommendation:* reuse the existing test/AI consent (no new consent version), mention "matching vectors" in the OpenAI line of the privacy statement, and delete the vector on withdrawal. Dennis/legal to confirm the wording.
3. **Desktop: side panel or inline section?** *Recommendation:* the right side panel (as mocked). The vacancy stays visible next to the results, and the panel is the same component as the mobile sheet, so there is less code.
4. **Side tables instead of a column on `Vacancies`.** The brief says "vector column on vacancies". *Recommendation:* side tables (D2), for speed and cleaner metadata. Functionally it is the same, and it can be inlined later if wanted.

## Out of scope
- No change to the existing `GET api/vacancies/{id}/similar` (closed-vacancy 410 page, `VacanciesController.cs:705`). The new endpoint is separate.
- No change to the match score (`ProfileVacancyMatchCalculator`) or the banenkaart ranking.
- No production rollout. Production gets pgvector only when Dennis releases `acceptatie → main`.
