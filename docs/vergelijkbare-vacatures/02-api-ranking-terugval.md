> **Warnings:** deliver a Release build with **0 warnings** (`TreatWarningsAsErrors=true`). Run `dotnet format` and `.github/scripts/count-build-warnings.sh` before opening the PR. No blanket `<NoWarn>`.

# Step 02: similar-vacancies API, ranking, reason codes, fallback

**Branch:** `git fetch origin && git checkout -b cursor/vv-02-api origin/cursor/vv-01-embeddings`. **PR base:** `cursor/vv-01-embeddings`. Title: `feat(vv-02): more-like-this API + ranking + fallback`.

Read [00-README.md](00-README.md) (D7–D10). This step needs the tables and clients from step 01.

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
| Existing `/similar` | `Jobsy.Api/Controllers/VacanciesController.cs:700-770` | Only for **closed** vacancies (410 page). Max 3, same category ≤ 25 km else ≤ 10 km, nearest first. **Leave it unchanged.** Its logic is the template for our fallback. |
| Discover filters | `VacanciesController.cs:418-600` | `VacancyDiscoveryQuery.Filter`, `MatchesTransport`, `TravelReach.MaxCrowFliesKm` + `TravelReach.Estimate` (no routing calls), legal-age filter via `match.Core.LegalAgeKnown && !match.Core.LegalEligible` (`:518`), night-shift down-rank `DislikeMatchRules.DownRankNightShifts` (`:590`). `maxMinutes` default 30, clamp 5–90. |
| Card mapping | `VacanciesController.cs:2293` `MapCard(record, showWage, travelMinutes, matchPercent, matchBand, …)`, `:2114` `CanViewerSeeWageAsync`, `:2107` `ApplyPublicListCacheHeaders` | `MapCard` already applies the intermediary display rules. |
| Visibility | `Jobsy.Core/Rules/PublicVisibility.cs:12-40` | `IsVacancyPublic(record, today)` = Active + date window + `PublisherVerified`. "The only place that decides public visibility." Add the new read path to `PublicVisibilityEndpointTests`. |
| Index | `IVacancyDiscoveryIndex.GetActiveAsync` | Public, non-test records (`VacancyDiscoveryRecord`). |
| Candidate context | `ProfileVacancyMatchService.TryLoadForPrincipalAsync` / `ScoreAsync` (`Jobsy.Infrastructure/Services/ProfileVacancyMatchService.cs:31-100`) | Gives prefs, age, competency/values/RIASEC scores and the legal flags per record. |
| Company culture | `ICompanyCultureLookup.GetForCompaniesAsync` | Values (Schwartz), culture, engagement per end-client `CompanyId`. |
| Applications | `Application.CandidateUserId`, `Application.VacancyId` (`Jobsy.Core/Entities/Application.cs:8-10`) | |
| Rate limits | `Jobsy.Api/Program.cs:161` `public-write`, `:217` `public-read` | |

## 2. Endpoint
In `VacanciesController` (it reuses `MapCard`, `CanViewerSeeWageAsync` and the cache headers). Keep the action thin and put all logic in a service.

```
GET api/vacancies/{id:guid}/more-like-this
    ?originLat=&originLng=&transport=Fiets&maxMinutes=30
[AllowAnonymous] [EnableRateLimiting("public-read")]
→ 200 MoreLikeThisResponseDto
→ 404 when the viewed vacancy is not public (PublicVisibility.IsVacancyPublic)
```
```csharp
public sealed record MoreLikeThisResponseDto(
    string Mode,                 // "personal" | "vacancy" | "fallback"
    bool ShowDiscoveryCta,       // logged out, or candidate without passport vector
    IReadOnlyList<MoreLikeThisItemDto> Items);   // 0..6
public sealed record MoreLikeThisItemDto(
    VacancyCardDto Card,         // from MapCard (travelMinutes or distanceKm filled)
    string ReasonCode,           // see §5
    string? ReasonArg);          // stable id (value card id, competency code, engagement id, branche label), never display text
```
- **Never 5xx because of embeddings.** Wrap the embedding path in try/catch, log a warning with `{VacancyId} {Mode} {FailureCode}`, and return the fallback. Only a bug in the fallback itself may surface (an empty list is acceptable).
- Cache headers: logged-out uses `ApplyPublicListCacheHeaders()`. Logged-in uses `Cache-Control: private, no-store` because the result is personal. Add `X-Robots-Tag: noindex`.
- Add the route to `docs/ROUTES.md` and to `PublicVisibilityEndpointTests`.

## 3. Service
`Jobsy.Core/Interfaces/ISimilarVacancyService.cs` + `Jobsy.Infrastructure/Services/SimilarVacancyService.cs` (scoped):
```csharp
Task<SimilarVacancyResult> GetAsync(Guid vacancyId, ClaimsPrincipal viewer, SimilarVacancyOrigin? origin, CancellationToken ct);
```
Flow:
1. **Viewed vacancy.** Load the record from `GetActiveAsync()`. If it is missing, return not found.
2. **Viewer context.**
   - **Candidate:** `ProfileVacancyMatchService.TryLoadForPrincipalAsync`. Origin = `User.HomeLocation`. If that is null, use the query origin. Use prefs `MaxTravelMinutes` (default 30) and `PreferredTransport` (default `Fiets`). Load the applied vacancy ids for this user (one query on `Applications`). Load the `CandidateProfileEmbedding` only when `HasCurrentTestAiConsent && CanUseCandidateFeatures`.
   - **Anonymous / other roles:** query origin, `maxMinutes` (clamp 5–90, default 30), transport. Without an origin, use a 25 km radius around the viewed vacancy (D8).
3. **Hard filters** (pure, `Jobsy.Core/Rules/Similar/SimilarVacancyFilters.cs`, in this order):
   1. `PublicVisibility.IsVacancyPublic(r, today)` (Active, date window, verified publisher, verified intermediary).
   2. `r.Id != viewed.Id`.
   3. Not applied (`appliedIds`).
   4. Travel:
      - With an origin: `VacancyDiscoveryQuery.MatchesTransport`, `TravelReach.Estimate(...).TravelMinutes <= maxMinutes` and `DistanceKm <= TravelReach.MaxCrowFliesKm(mode, maxMinutes)`.
      - Without an origin: `HaversineKm(viewed, r) <= 25`.
   5. Legal age: drop the record when `LegalAgeKnown && !LegalEligible`. Use the same `MatchingProfileMapper.BuildInput` → `MatchScoreCalculator` core as Discover, but **only the core**; do not compute the full profile match for 300 records.
   6. Near-duplicate: same end-client `CompanyId` + same normalised title (lowercase, trailing ` (\d+)` and `·` suffixes stripped). Keep the first one by id.
4. **Similarity** (`ISimilarityStore` in Infrastructure):
   - Npgsql, one round-trip:
     ```sql
     SELECT "VacancyId", 1 - ("Vector" <=> @v) AS "SimV", 1 - ("Vector" <=> @c) AS "SimC"
     FROM "VacancyEmbeddings"
     WHERE "VacancyId" = ANY(@ids) AND "Model" = @model AND "Dimensions" = @dims
     ```
     `@c` is null in vacancy mode; select `NULL` then. Use parameters, never string-concatenated ids.
   - InMemory (tests): load the rows and compute the cosine in C# (same pattern as `VacancyProductService.cs:1166`).
   - If the viewed vacancy has no vector: enqueue it (step 01 queue) and go to **fallback**.
   - If the candidate vector is missing or stale: use the existing one if there is one (enqueue a refresh). If there is none, use vacancy mode.
5. **Ranking** (pure, `Jobsy.Core/Rules/Similar/SimilarVacancyRanker.cs`):
   - `wV = settings.SimilarVacancyWeightPercent / 100.0` (step 04 adds the setting; until then a const 50), `wC = 1 - wV`.
   - Personal: `score = wV·SimV + wC·SimC`. Vacancy mode: `score = SimV`.
   - Drop the result when `SimV < MinVacancySimilarity` (0.25). "Lijkt hierop" must stay true even when the passport pulls hard.
   - Night-shift dislike: apply `DislikeMatchRules.DownRankNightShifts` after sorting, as Discover does.
   - Max 2 per end-client `CompanyId`.
   - Order: score desc → travel minutes asc (null last) → title ordinal → id. Take 6.
   - Fewer than 3 left: top up from the fallback list (excluding ids already chosen). Those items get reason `same-work`/`same-branch`, and `Mode` stays what it was.
6. **Fallback** (pure, `SimilarVacancyFallback.cs`): the same hard-filtered set → same `CategoryId` as viewed, else at least one shared WorkType label → nearest first (travel minutes, else distance) → title. Take 6. `Mode="fallback"`.
7. **Reason codes** (§5) per item, then `MapCard`.
8. **Cache:** `IMemoryCache`, key `mlt:{vacancyId}:{viewerKey}:{weights}:{indexRefreshedTicks}`, 10 min. `viewerKey` = `u:{SHA256(userId)[..16]}` or `a:{lat:F2},{lng:F2},{transport},{maxMinutes}`. Never cache across users.

**Performance budget:** p95 < 300 ms on acceptatie with a warm index (one SQL query, no routing API, and **no OpenAI call on the request path**: vacancy and candidate embeddings are always made in the background).

## 4. Logged-out and no-passport behaviour
- `ShowDiscoveryCta = true` when the viewer is anonymous, or is a candidate without a usable candidate vector (no consent, not enough signal, or not computed yet).
- Non-candidate roles (employer, admin) get vacancy mode with `ShowDiscoveryCta = false`.

## 5. Reason codes (pure, `Jobsy.Core/Rules/Similar/SimilarReasonRules.cs`)
Exactly one per item, first match wins, deterministic. Personal codes need personal data.

| Order | Code | Rule | Arg |
|---|---|---|---|
| 1 | `closer` | Origin known, `travel(result) <= travel(viewed) - 5` min | none |
| 2 | `value` | Candidate values completed; their top Schwartz driver (tie: catalog order) is the driver of one of the result company's 3 value cards (`CompanyValueCards`, org fallback) | card id (`teamgevoel`) |
| 3 | `strength` | Candidate competency completed; top competency; a result culture pillar (`CulturePillarCatalog`, vacancy pillars, else company culture) has weight ≥ 85 for it | competency code (`Samenwerken`) |
| 4 | `outdoor` | Viewed **and** result WorkTypes intersect `{Tuinbouw, Bouw}` (const set `SimilarReasonRules.OutdoorWorkTypes`) | none |
| 5 | `flexible` | Viewed and result both `FlexibleTimes` | none |
| 6 | `no-experience` | Result is low barrier (no barrier JSON requirements, no `RequiredEducation`, `MinimumEmployers` null/0) **and** (candidate `NoWorkExperience == true`, or anonymous and viewed is also low barrier) | none |
| 7 | `engagement` | Viewed and result companies share an engagement item (status ≠ Removed) | engagement id (`duurzaam`) |
| 8 | `same-branch` | Shared WorkType label | branche label (`Logistiek`) |
| 9 | `same-work` | Default | none |

- **Diversity:** walk the ranked list. If a code was already used twice and a later rule also matches this item, take the next matching rule. If nothing else matches, the code may repeat.
- The UI turns code + arg into text (step 03). The API never sends display text.

## 6. Tests (this PR)
- `SimilarVacancyFiltersTests`:
  - Inactive, expired and unverified publishers are excluded, as is a verified company with an unverified intermediary.
  - The same id, applied ids, records beyond travel time, records beyond 25 km without an origin, legal-age ineligible records and near-duplicates are all excluded.
  - The order of filters does not change the result.
- `SimilarVacancyRankerTests`:
  - 0.5/0.5 weighting is correct.
  - Weight 100/0 equals vacancy-only.
  - The `MinVacancySimilarity` floor holds even with a high SimC.
  - There are at most 2 per company and the tie-breaks are stable.
  - The top-up from the fallback happens when fewer than 3 remain.
  - The output is the same for the same input (run twice).
- `SimilarVacancyFallbackTests`: category first, then branche, then nearest. Hard filters are still applied.
- `SimilarReasonRulesTests`: every code fires on a minimal fixture, the priority order holds, the diversity rule works, and personal codes never fire in vacancy mode.
- `SimilarVacancyServiceTests` (InMemory + fake embeddings):
  - Personal mode uses both vectors. Anonymous gives vacancy mode + CTA.
  - No consent gives vacancy mode + CTA.
  - A missing viewed vector gives fallback and enqueues the vacancy.
  - An `IEmbeddingClient`/store that throws gives fallback, **no exception**.
  - pgvector missing (simulate `PostgresException 42883/42P01` from the store) gives fallback.
  - The cache key differs per user.
- `MoreLikeThisEndpointTests` (`WebApplicationFactory`, like the existing API tests):
  - 404 for a non-public id, and 200 with `Mode` set.
  - Never 500 when the embedding store throws.
  - The anonymous response is `public` cacheable, the logged-in one is `no-store`.
  - Max 6 items, and the viewed id is never in the items.
- `PublicVisibilityEndpointTests`: add the new route.

## 7. Acceptance
- CI green; Release build 0 warnings.
- With the fake provider in the CI stack, `GET /api/vacancies/{seeded id}/more-like-this` returns `Mode=vacancy` (anonymous) with ≥ 1 item. With `Embeddings__Provider=none` it returns `Mode=fallback`.
- No UI yet. The existing `/similar` is byte-for-byte the same.
- PR body: example JSON responses for the three modes (seed data), and timing from a local run.
