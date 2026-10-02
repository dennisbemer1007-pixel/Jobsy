> **Warnings:** deliver a Release build with **0 warnings** (`TreatWarningsAsErrors=true`). Run `dotnet format` and `.github/scripts/count-build-warnings.sh` before opening the PR. No blanket `<NoWarn>`.

# Step 01: pgvector, embedding service, background job, backfill

**Branch:** `git fetch origin && git checkout -b cursor/vv-01-embeddings origin/acceptatie`. **PR base:** `acceptatie`. Title: `feat(vv-01): pgvector + embeddings foundation`.

Read [00-README.md](00-README.md) first (decisions D1–D6, D14, D15).

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
| EF / Npgsql | `Directory.Packages.props:17-20,36-39` | EF Core 10.0.12, Npgsql + EF provider + NTS 10.0.3. Central package management. |
| DbContext setup | `Jobsy.Infrastructure/DependencyInjection.cs:169-177` | `UseNpgsql(cs, npgsql => { npgsql.UseNetTopologySuite(); npgsql.MigrationsAssembly(...) })`. Connection string, no custom `NpgsqlDataSourceBuilder`. |
| Extensions | `Jobsy.Infrastructure/Data/JobsyDbContext.cs:167` | `modelBuilder.HasPostgresExtension("postgis")`. |
| Migrations | `Jobsy.Infrastructure/Data/Migrations/` (latest `20261002121134_DropDeepAnalysisPriceEuro`) | Applied by the API on start (CI comment in `pr-tests.yml:14`). |
| Tests DB | many `UseInMemoryDatabase(...)` in `Jobsy.Tests` | InMemory provider; raw SQL paths need an InMemory fallback (pattern: `VacancyProductService.cs:1166` `if (_db.Database.IsNpgsql())`). |
| CI Postgres | `.github/workflows/pr-tests.yml:12-26` | `postgis/postgis:16-3.4`. **Has no pgvector.** |
| Render Postgres | `render.yaml:30,206` | PostgreSQL 16. Render supports `CREATE EXTENSION vector;` (docs: render.com/docs/postgresql-extensions). |
| OpenAI resolver | `Jobsy.Core/Interfaces/IOpenAiEndpointResolver.cs`, `Jobsy.Infrastructure/Services/OpenAi/OpenAiEndpointResolver.cs` (`Features` dictionary) | DB credentials → `OpenAiOptions` → defaults. **`ResolveModelAsync` returns the shared chat model (`gpt-4o-mini` or the DB model)**. Do not send that model to `/embeddings`. |
| OpenAI features | `Jobsy.Core/Enums/OpenAiFeature.cs` | 11 features; no embeddings yet. |
| OpenAI HttpClient | `DependencyInjection.cs:179-187` | Named client `"OpenAI"`, 30 s timeout, no redirects. |
| Queue + worker pattern | `Jobsy.Core/Interfaces/ICultureFitRefineQueue.cs`, `Jobsy.Infrastructure/Services/CultureFitRefineQueue.cs`, `Jobsy.Infrastructure/Jobs/CultureFitRefineWorker.cs` (registered `DependencyInjection.cs:95`) | Channel-backed in-memory queue, `BackgroundService`, `EmployersJobGate`, scoped service per item. In-memory, so it is lost on restart (hence the sweep). |
| Content fingerprint pattern | `Jobsy.Core/Entities/CandidateWhoAmIProfile.cs:14` `InputFingerprint` | Reuse the idea: hash of the input text. |
| CLI pattern | `Jobsy.Api/Program.cs:16-20` (`args[0] == "test-accounts"`), `Jobsy.Api/Ops/TestAccountsCommand.cs` | Static `RunAsync(string[] args, TextWriter? output)` with exit codes. |
| Platform log | `Jobsy.Core/Entities/PlatformLog.cs` (`Level`, `Category`, `Message`, `DetailsJson`), admin page `/admin/beveiliging/systeemlogs` | Use for per-call usage rows. |
| Vacancy content | `Jobsy.Core/Entities/Vacancy.cs`: `Title:9`, `Description:10`, `WorkTypes:57`, `WorkTypeLabels:60`, `Kind:63`, `CategoryId:66`, `CulturePillarsJson:186`, `BarrierRequirementsJson:192`, `IntermediaryCompanyId:22` | No separate "tasks" field: tasks are in `Description`. |
| Employer culture | `ICompanyCultureLookup` (`Jobsy.Core/Interfaces/ICompanyCultureLookup.cs`), `CompanyValuesProfile.CardIdsJson`, `CompanyEngagementClaim` (status ≠ `Removed`) | Vestiging → organisation fallback is already in the lookup. Labels: `CulturePillarCatalog.All` (NL text inline), `CompanyValueCards.All` (`WaProfile.Card.*`), `EngagementCatalog.All` (`WaEngage.Item.*`). |
| Candidate passport data | `CandidateValuesProfile` (Schwartz %), `CandidateCareerInterest` (RIASEC %, `HollandCode`, `MatchTagsJson`), `CandidateCompetency` (5 competencies %), `CandidateCareerPlan.DreamTitle`, `User.PreferencesJson` → `CandidatePreferencesDto` (`Roles`, `Employers[].Role/.Description`, `Educations`, `EducationDirection`, `NoWorkExperience`) parsed by `MatchingProfileMapper.DeserializePrefs`; completion via `ProvisionalAssessmentScores` / `CandidateCompetencyStatuses.IsCompleted` | **PII fields to exclude:** `User.FullName/FirstName/LastName/Email/PhoneNumber/DateOfBirth/HomeLocation`, prefs `HomeAddress`, `AboutMe`, `DefaultMotivation`, `Employers[].EmployerName`, `CandidateReference.*`, `CandidateWhoAmIProfile.StoryText`. |
| Consent | `Jobsy.Core/Privacy/CandidateConsentRules.cs` `HasCurrentTestAiConsent`, `CanUseCandidateFeatures`; withdrawal sets `TestAiConsentAt = null` at `Jobsy.Api/Controllers/MeController.cs:1014` and `PrivacyDataService.cs:1245` | |
| Account deletion | `Jobsy.Infrastructure/Services/PrivacyDataService.cs:1301-1306` (removes `CandidateWhoAmIProfiles`) | Add the new table next to it. |

## 2. Packages and extension
1. `Directory.Packages.props`: add `Pgvector.EntityFrameworkCore` **0.3.0** (supports EF Core 9 and 10; depends on `Npgsql.EntityFrameworkCore.PostgreSQL >= 9.0.1` and `Pgvector >= 0.3.2`). Reference it from `Jobsy.Infrastructure.csproj` only.
2. `DependencyInjection.cs:171-175`: `npgsql.UseVector();` next to `UseNetTopologySuite()`. There is no custom `NpgsqlDataSourceBuilder`. If one is ever added, it also needs `dataSourceBuilder.UseVector()` (pgvector-dotnet issue #61, otherwise `Writing values of 'Pgvector.Vector' is not supported`).
3. `JobsyDbContext.OnModelCreating`: `modelBuilder.HasPostgresExtension("vector");` under the postgis line.

## 3. Entities and migration
New entities in `Jobsy.Core/Entities/`. Core gets **no** package reference to Pgvector. Keep the CLR vector as `float[]` in Core and convert in Infrastructure.

```csharp
public class VacancyEmbedding            // 1:1 with Vacancy, cascade delete
{
    public Guid VacancyId { get; set; }  // PK + FK
    public Vacancy Vacancy { get; set; } = null!;
    public float[] Vector { get; set; } = [];       // vector(1536) in Postgres
    public string Model { get; set; } = "";         // e.g. text-embedding-3-small
    public int Dimensions { get; set; }
    public int TextVersion { get; set; }            // EmbeddingTextVersions.Vacancy
    public string ContentHash { get; set; } = "";   // 64 hex chars
    public int TokenCount { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CandidateProfileEmbedding    // 1:1 with User (Candidate), cascade delete
{
    public Guid UserId { get; set; }  // PK + FK
    // same fields as above (Vector, Model, Dimensions, TextVersion, ContentHash, TokenCount, UpdatedAtUtc)
}
```

Mapping in `JobsyDbContext` (Npgsql):
- `Property(e => e.Vector).HasConversion(new ValueConverter<float[], Pgvector.Vector>(a => new Vector(a), v => v.ToArray())).HasColumnType("vector(1536)")`, plus a `ValueComparer<float[]>` that compares sequences.
- Under InMemory (`!Database.IsNpgsql()`), skip the column type and keep `float[]` with the same comparer. Check that the model builds in both providers. Add a test that creates the InMemory context and saves one row.
- `Model` max 64, `ContentHash` max 64 (fixed), indexes: none needed now. At ≤ 50k rows the exact distance on a pre-filtered id list is fast. Leave a comment that an HNSW index (`USING hnsw ("Vector" vector_cosine_ops)`) is the next step above that size.

Migration `AddVectorEmbeddings`:
- `migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;")` (EF also emits `AlterDatabase().Annotation("Npgsql:PostgresExtension:vector", ...)`; keep both idempotent).
- Create both tables. `Down` drops the tables but **not** the extension.
- `const int VectorDimensions = 1536` lives in one place (`Jobsy.Core/Rules/EmbeddingDimensions.cs`) and is used by the mapping and the options check.

## 4. CI and local dev (must land in this PR, or CI is red)
`pr-tests.yml`: right after "Setup .NET" add:

```yaml
      - name: Install pgvector into the PostGIS service
        run: |
          docker exec ${{ job.services.postgres.id }} bash -c "apt-get update -qq && apt-get install -y -qq postgresql-16-pgvector"
```

The official postgres/postgis Debian images ship the PGDG apt repo, so this works without a new image. Verify it in the PR run, and paste the `SELECT extversion FROM pg_extension WHERE extname='vector'` result in the PR body.
Also update the local DB instructions in `docs/ONBOARDING.md` / `README.md` (the same apt line, or `CREATE EXTENSION vector` on a pgvector-enabled image).
For `acceptatie`: the migration runs on the next deploy of `lobsy-acc-api` after Dennis merges. Do not run anything against Render yourself.

## 5. Options
`Jobsy.Core/Options/EmbeddingOptions.cs`, section `"Embeddings"`:

| Key | Default | Note |
|---|---|---|
| `Enabled` | `true` | Global kill switch (the admin switch comes in step 04). |
| `Provider` | `"openai"` | `"openai"`, `"fake"` (deterministic, CI/dev only; **refuse in Production** at startup with an error log and treat as `none`), `"none"`. |
| `Model` | `"text-embedding-3-small"` | Never taken from `OpenAiEndpointResolution.Model`. |
| `Dimensions` | `1536` | Must equal `EmbeddingDimensions.Vector`; otherwise log an error and disable. |
| `BatchSize` | `64` | Inputs per API call. |
| `MaxInputChars` | `6000` | Truncate after building the text (about 1,500 tokens). |
| `SweepIntervalMinutes` | `10` | |
| `UsdPerMillionTokens` | `0.02` | For the cost estimate in logs. |
| `TimeoutSeconds` | `20` | Per call (shorter than the shared 30 s client timeout, via a linked CTS). |
| `MinVacancySimilarity` | `0.25` | Used in step 02. |

`appsettings.Development.json` and the CI stack (`.github/scripts/start-ci-stack.sh` env) set `Embeddings__Provider=fake`.

## 6. Embedding text builders (pure, in Core, fully unit-tested)
`Jobsy.Core/Rules/Embeddings/VacancyEmbeddingText.cs` and `CandidateEmbeddingText.cs`, plus `EmbeddingPiiScrubber.cs`. Static, no I/O. Inputs are small records, so tests need no DB.

**Vacancy text** (Dutch canonical labels, fixed order, one line per part, empty parts skipped):
```
Functie: {Title}
Werk: {scrubbed Description}
Branche: {WorkTypeLabels or WorkTypes flags, CategoryName}
Soort: {Regulier | Stage | Vrijwilligerswerk}
Cultuur: {CulturePillarCatalog NL text for the vacancy pillars, else company culture top pillars}
Kernwaarden: {CompanyValueCards NL labels of the 3 cards (org fallback)}
Betrokkenheid: {EngagementCatalog NL labels, status != Removed}
Drempel: {laag | diploma X | ervaring N jaar}   // from BarrierRequirementsJson / RequiredEducation, no free text
```
- **Never included:** `Company.Name`, intermediary name, `OfferedByLabel`, addresses, KvK numbers, contact e-mail/phone/WhatsApp, image/video URLs, wages (wage is not "content" and changes often).
- `EmbeddingPiiScrubber.Scrub(text, extraNames)` removes e-mail addresses, phone numbers (NL/international patterns), URLs, Dutch postcodes (`\b\d{4}\s?[A-Z]{2}\b`), and every occurrence of the company and intermediary names (case-insensitive, whole words). It collapses whitespace and caps the length.
- Use the original vacancy text, not `VacancyTranslation`.

**Candidate text** (only when the consent gate passes):
```
Waarden: {top 2 Schwartz drivers, NL labels, only if CandidateValuesProfile completed}
Interesses: {top 2 RIASEC labels + MatchTags, only if CandidateCareerInterest completed}
Sterktes: {top 2 competencies, NL labels, only if CandidateCompetency completed}
Wil graag: {Prefs.Roles}
Ervaring: {Employers[].Role + scrubbed Employers[].Description}   // NOT EmployerName
Opleiding: {Educations + EducationDirection}
Droombaan: {CandidateCareerPlan.DreamTitle}
```
- `HasEnoughSignal` = at least one completed test **or** (roles + one experience/education line). Otherwise return `null`, which means no candidate vector and vacancy-only mode.
- **Never included:** names, e-mail, phone, `HomeAddress`, `HomeLocation`, `DateOfBirth`/age, `AboutMe`, `DefaultMotivation`, `EmployerName`, references, `WhoAmI.StoryText`, school/student data.

**Hash:** `ContentHash = SHA256($"{TextVersion}|{Model}|{Dimensions}|{text}")` as lowercase hex. Bump `EmbeddingTextVersions.Vacancy/Candidate` when the format changes, which re-embeds everything through the sweep.

## 7. Embedding client
`Jobsy.Core/Interfaces/IEmbeddingClient.cs`:
```csharp
Task<EmbeddingBatchResult> EmbedAsync(IReadOnlyList<string> inputs, EmbeddingPurpose purpose, CancellationToken ct);
// EmbeddingBatchResult(bool Success, IReadOnlyList<float[]> Vectors, string Model, int PromptTokens, string? FailureCode)
```
- `OpenAiEmbeddingClient` (Infrastructure):
  - Add `OpenAiFeature.Embeddings` to the enum **and** to `OpenAiEndpointResolver.Features` (`DefaultModel` there is irrelevant; use `BaseUrlConfigFallbackStyle.NormalizeOrDefault`).
  - Take `ApiKey` + `BaseUrl` from the resolver. Take the model from `EmbeddingOptions.Model`.
  - `POST {BaseUrl}embeddings` with `{ "model", "input": [...], "dimensions": 1536, "encoding_format": "float" }`, using the named `"OpenAI"` HttpClient. Parse `data[i].embedding` by `index` and `usage.prompt_tokens`.
  - No API key → `Success=false, FailureCode="no-key"` without an HTTP call.
  - 429/5xx/timeout → one retry after 2 s, then fail.
  - Circuit breaker: after 3 consecutive failures, skip calls for 5 minutes (`FailureCode="circuit-open"`).
  - Never throw to callers, except for `OperationCanceledException` on shutdown.
- `FakeEmbeddingClient`: deterministic. Tokenise the lowercase words, hash each word into one of 1536 buckets (signed), then L2-normalise. Similar texts give similar vectors, so ranking tests and CI Playwright are meaningful. `PromptTokens` = word count.
- **Usage/cost logging (every call, success or failure):**
  - `ILogger` structured: `"Embeddings call {Purpose} {Provider} {Model} inputs={Inputs} tokens={Tokens} usd={Usd:0.000000} ms={Ms} outcome={Outcome}"`.
  - **plus** one `PlatformLog` row: `Category="openai.embeddings"`, `Level=Info|Warning`, `DetailsJson` with the same fields.
  - **No input text, no user ids.** `Purpose` is `vacancy` / `candidate` / `backfill`.

## 8. Queue, worker, sweep, hooks
- `IEmbeddingQueue` (Channel, bounded 5,000, dedupe by `(kind, id)` like `CultureFitRefineQueue`), with items `EmbeddingWorkItem(EmbeddingKind Kind, Guid Id)`.
- `EmbeddingWorker : BackgroundService`:
  - Drains the queue in batches of up to `BatchSize` per kind.
  - For each item it builds the text and computes the hash. If the hash equals the stored one, it skips the item and counts it as "unchanged" (log at Debug).
  - Otherwise it calls `IEmbeddingClient` and upserts. On failure, the existing row stays as it is (stale is better than nothing).
  - Respect `EmployersJobGate` like `CultureFitRefineWorker`, and `Embeddings.Enabled`.
- `EmbeddingSweepHostedService` (every `SweepIntervalMinutes`, first run 2 min after start):
  - Loads the public vacancy ids, test data included. `VacancyDiscoveryIndex.GetActiveIncludingTestAsync` (`VacancyDiscoveryIndex.cs:71`) exists on the class but not on `IVacancyDiscoveryIndex`, so add it to the interface; test viewers (`TestDataRules.IsTestViewer`) need vectors for the CLI test vacancies too. Enqueues ids with no row, an old `TextVersion`/`Model`, or a changed hash. To keep the sweep cheap, compute the hash only for vacancies whose source rows changed since `UpdatedAtUtc`, or every Nth sweep for all of them; pick the simpler option, which is fine at ~1k vacancies.
  - Candidates: enqueue users with consent where any of the passport source rows (`CandidateValuesProfiles`, `CandidateCareerInterests`, `CandidateCompetencies`, `CandidateCareerPlans`, `User.PreferencesJson` change marker) has `UpdatedAtUtc` > embedding `UpdatedAtUtc`. Batch 200 per sweep.
- Fast-path hooks:
  - Enqueue the vacancy after a successful create/update in `VacanciesController.Create/Update` (`:1110`, `:1120`) and after publish/approve.
  - Enqueue the candidate after a test is completed and after a profile save.
  - Everything else is caught by the sweep.
- Delete `CandidateProfileEmbedding` when consent is withdrawn (`MeController.cs:1014`, `PrivacyDataService.cs:1245`) and on account deletion (`PrivacyDataService.cs:1301` block). The data export lists only `{ model, updatedAtUtc }`, never the vector.

## 9. Backfill CLI
`Jobsy.Api/Ops/EmbeddingsCommand.cs`, wired in `Program.cs` like `test-accounts`:
```
dotnet Jobsy.Api.dll embeddings backfill [--vacancies] [--candidates] [--limit N] [--dry-run]
dotnet Jobsy.Api.dll embeddings status
```
- `--dry-run` prints the counts (missing / stale / unchanged) plus estimated tokens and USD, and makes no API calls.
- Without flags it does both kinds.
- It processes synchronously in batches, prints progress, and exits 0/1. Refuse with exit 2 when the provider is `fake` in Production.
- `status` prints the totals per kind, per model, and the oldest `UpdatedAtUtc`.
- Document the Render one-off command in `docs/runbooks/` (step 05 finishes the runbook). Do **not** run it on Render yourself.

## 10. Tests (this PR)
- `VacancyEmbeddingTextTests`:
  - The fixed order and labels are present.
  - Company name, intermediary name, e-mail, phone, URL and postcode inside the description are **removed**.
  - Wage, address and KvK are never present.
  - The text is the same when only the wage changes, so the hash is unchanged.
- `CandidateEmbeddingTextTests`:
  - Build from a user named "Dennis Bemer", `dennis@example.nl`, `06-12345678`, address "Heulweg 9, Kwintsheul", employer "Albert Heijn Naaldwijk", and a "Bel me op 0612345678" description. Assert that **none** of these strings or digits appear.
  - Roles, functions, values and strengths do appear.
  - `HasEnoughSignal` returns false for an empty passport.
- `EmbeddingHashTests`: the same input gives the same hash. A version, model or dimension bump changes the hash.
- `EmbeddingWorkerTests` (InMemory + fake client):
  - An unchanged hash causes no client call.
  - A changed text causes one call and an upsert.
  - A client failure keeps the old row.
  - No consent means no candidate row, and withdrawal deletes it.
- `OpenAiEmbeddingClientTests` (stub `HttpMessageHandler`):
  - Request shape: the model comes from `EmbeddingOptions`, not from the resolver chat model.
  - Usage parsing, the no-key short-circuit (zero HTTP calls), and retry + circuit breaker.
  - One `PlatformLog` row with no input text.
- `FakeEmbeddingClientTests`: deterministic output, unit length, similar text gives a higher cosine than unrelated text.
- `EmbeddingsCommandTests`: `--dry-run` makes no client calls, and the exit codes are correct.
- Model test: InMemory context saves and reads both entities.

## 11. Acceptance
- CI is green, including the migration on the pgvector-enabled service container.
- On a fresh DB: `embeddings backfill --dry-run` shows counts, and `embeddings backfill` (fake provider) fills `VacancyEmbeddings` for all public vacancies.
- No UI change. No change to existing endpoints.
- PR body: decisions taken, CI pgvector proof, the estimated cost for acceptatie (from `--dry-run` against a local seed DB), and the list of files.
