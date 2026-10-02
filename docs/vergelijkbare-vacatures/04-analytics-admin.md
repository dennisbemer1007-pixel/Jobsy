> **Warnings:** deliver a Release build with **0 warnings** (`TreatWarningsAsErrors=true`). Run `dotnet format` and `.github/scripts/count-build-warnings.sh` before opening the PR. No blanket `<NoWarn>`.

# Step 04: analytics counters, admin switch + weight setting, usage panel

**Branch:** `git fetch origin && git checkout -b cursor/vv-04-analytics origin/cursor/vv-03-ui`. **PR base:** `cursor/vv-03-ui`. Title: `feat(vv-04): similar-vacancies analytics + admin weight`.

Read [00-README.md](00-README.md) (D7, D13, D14).

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
| Analytics API | `Jobsy.Api/Controllers/AnalyticsController.cs` (`[Route("api/analytics")]`, `impressions` at `:29-31`) | `[AllowAnonymous]`, `[EnableRateLimiting("public-write")]`, **returns `{ recorded = 0 }` when `AnalyticsConsent.IsGranted(Request)` is false** (`Jobsy.Api/Security/AnalyticsConsent.cs`). Follow the same consent gate. |
| Daily aggregate pattern | `Jobsy.Core/Entities/KvkUsageDaily.cs` + `Jobsy.Infrastructure/Services/KvkUsageCounter.cs:45-70` | `Date` (UTC) + type + `Count`, upsert per bucket, no query text stored. |
| Platform settings | `Jobsy.Core/Entities/PlatformFeatureSettings.cs`, `PlatformFeatureSnapshot`, `PlatformFeatureUpdate` (`Jobsy.Core/Interfaces/IPlatformFeatureService.cs:44`), `IPlatformFeatureService.GetAsync/UpdateAsync` | One settings row. Snapshot is what the API and Web read. |
| Admin catalog | `Jobsy.Web/Admin/PlatformSettingsCatalog.cs` (`GroupVacancies` `:55`; Int example `CandidateInsightsUnlockDays` `:195-205` with `Min`/`Max`/`UnitKey`) | Rendered by `PlatformSettingsEditor.razor` on `/admin/instellingen` (`SettingsAdmin.razor`). `FieldExists` guards by reflection. `PlatformSettingsCatalogTests` covers it. |
| PlatformLogs | step 01 writes `openai.embeddings` usage rows | Source for cost/coverage in the panel. |

## 2. Counters (no PII)
- Entity `Jobsy.Core/Entities/SimilarVacancyUsageDaily.cs`:
  - `Id`, `Date` (DateOnly, UTC)
  - `Event` (`open` | `result-click` | `cta-click`)
  - `Mode` (`personal` | `vacancy` | `fallback`)
  - `ReasonCode` (nullable, only for result-click)
  - `Position` (nullable 1–6)
  - `Count`
- Unique index on `(Date, Event, Mode, ReasonCode, Position)`. Migration `AddSimilarVacancyUsageDaily`.
- `ISimilarVacancyUsageCounter` (Infrastructure), modelled on `KvkUsageCounter`. Upsert with `INSERT … ON CONFLICT … DO UPDATE SET "Count" = "Count" + 1` on Npgsql, and a read-modify-write on InMemory.
- **No** vacancy id, user id, IP, user agent or session id. The vacancy id is left out on purpose: per-vacancy counts on a small dataset can re-identify a viewer. Revisit only with Dennis.
- Endpoint on `AnalyticsController`:
  ```
  POST api/analytics/similar-vacancies   [AllowAnonymous] [EnableRateLimiting("public-write")]
  body: { "event": "open|result-click|cta-click", "mode": "personal|vacancy|fallback", "reasonCode": "closer|…|same-work"?, "position": 1..6? }
  → 200 { recorded: 0|1 }   (0 when analytics consent is not granted)
  → 400 on any value outside the whitelists
  ```
  Whitelists are constants in Core (`SimilarVacancyAnalytics.Events/Modes`; reason codes from step 02 `SimilarReasonRules.Codes`). Any unknown JSON property is rejected (`JsonUnmappedMemberHandling.Disallow` on this DTO).
- UI (step 03 components) fires fire-and-forget through `JobsyApiClient`:
  - `open`, when the sheet opens and the result has loaded (mode known)
  - `result-click`, before navigating (do not await longer than 300 ms; navigation must not wait on analytics)
  - `cta-click`
  - Never throws to the UI.
- **Consent:** follow `AnalyticsConsent.IsGranted`. The admin panel shows the note "Telt alleen bezoekers die analytics-cookies accepteerden". Counts are a lower bound.

## 3. Admin settings
- `PlatformFeatureSettings` + snapshot + update:
  - `SimilarVacanciesEnabled` (bool, default **true**)
  - `SimilarVacancyWeightPercent` (int, default **50**, range 0–100). The share for "similar to this vacancy"; the passport share = 100 − value.
  - Migration `AddSimilarVacancySettings` with defaults so the existing row gets 50/true.
- `PlatformSettingsCatalog`, group `GroupVacancies`:
  - `SimilarVacanciesEnabled`: Bool, `ImpactLevel = Warn`, impact text "De knop 'Meer rotsen zoals deze' verdwijnt voor iedereen".
  - `SimilarVacancyWeightPercent`: Int, `Min 0`, `Max 100`, unit `%`. Description: "Hoeveel telt 'lijkt op deze vacature' mee. De rest (100 − dit getal) is 'past bij het paspoort'. 50 = evenveel."
  - Title/description keys in the admin localization file, all 5 languages via the same `Add` pattern.
- Wiring:
  - Step 02's ranker reads the weight from `IPlatformFeatureService.GetAsync()` (already cached), replacing the step 02 constant.
  - The cache key gets the weight, so a change is picked up at once; also bump the settings version used in the key on `UpdateAsync`.
  - Switch off: the API returns 404 for `more-like-this`, and the Web entry button is not rendered.
- Note in the PR: weight 100 means "vacancy similarity only" (same as logged-out behaviour). Weight 0 still keeps the `MinVacancySimilarity` floor from step 02, so results always stay somewhat similar.

## 4. Usage panel (admin)
- `GET api/admin/similar-vacancies/usage?days=30` (`[Authorize(Roles = "Admin")]`, days clamp 1–90). Returns:
  - totals and per day: opens, result clicks, CTR (= result clicks / opens), CTA clicks
  - mode mix (% personal / vacancy / fallback)
  - clicks by reason code and by position
  - embedding coverage: % of public vacancies with a current vector, % of consenting candidates with one (from the step 01 `status` query)
  - embedding cost in the period: sum of the step 01 PlatformLogs `openai.embeddings` usage → tokens and USD estimate
- Web: a section "Meer rotsen zoals deze" on the existing admin analytics/usage page (put it next to the KVK usage block if that is where usage lives; otherwise a card on `/admin/instellingen` under the vacancies group). Plain table + numbers, no new chart library.
- A fallback share above 20% over 7 days shows a hint: "Veel terugval: controleer OpenAI-sleutel en embeddings-status". Link to the runbook (step 05).

## 5. Tests (this PR)
- `SimilarVacancyUsageCounterTests`: increments per bucket, separate buckets per mode/reason/position, concurrent increments on Npgsql (Testcontainers, if the suite has them; otherwise InMemory + one PG test in CI).
- `SimilarVacancyAnalyticsEndpointTests`:
  - 200 `recorded:1` with consent, 0 without consent.
  - 400 for an unknown event/mode/reason, a position of 0 or 7, or an unknown extra property (e.g. `email`).
  - Rate-limit attribute present.
  - A reflection test on the row/DTO that no property is named like `*UserId|*Email|*Ip|*Session|VacancyId`.
- `SimilarVacancySettingsTests`:
  - Defaults 50/true.
  - Update clamps outside 0–100 (or the API rejects them; match the existing Int behaviour).
  - Weight 100 → ranking equals vacancy mode; weight 0 → passport similarity dominates, and the floor is respected.
  - Switch off → `more-like-this` 404 and the entry is not rendered (bUnit).
- `PlatformSettingsCatalogTests`: new rows present, in group vacancies, Min/Max set.
- `AdminSimilarUsageEndpointTests`: 403 for non-admin; aggregation maths (CTR, mode mix); cost sum from seeded PlatformLogs rows.

## 6. Acceptance
- CI green; Release build 0 warnings.
- Locally: open the sheet, click a result, then see the counts on the admin page (with analytics cookies accepted).
- Changing the weight in `/admin/instellingen` changes the order on the next open (no restart).
- PR body: screenshot of the admin panel and of the two new settings rows.
