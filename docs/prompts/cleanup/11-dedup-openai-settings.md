Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 11: Deduplicate OpenAI API-key/model/base-URL resolution (11 copies)

**Goal:** one shared resolver instead of about 450 copied lines. The resolved values and fallback order must stay exactly the same.

**Evidence:** `ResolveApiKeyAsync` / `ResolveModelAsync` / `ResolveBaseUrl` (names vary slightly) are copied into these 11 services in `Jobsy.Infrastructure/Services/`:
- `WhoAmIGenerationService.cs` (~:169-216)
- `AssistantChatService.cs`
- `CareerPathPlanGenerationService.cs`
- `CultureFitAiService.cs`
- `VacancyContentModerationService.cs`
- `CvExtractionService.cs`
- `MockInterviewService.cs`
- `OpenAiTranslationService.cs`
- `CareerCompassGenerationService.cs`
- `RoleFitCheckService.cs`
- `OpenAiCompetenceDeepReportAiService.cs`

Each reads platform settings (DB) first, then configuration/env, with a per-service default model.

**Do:**
1. **Diff the 11 copies first**, e.g. `diff <(sed -n ...) <(sed -n ...)`, and write the differences (default model, setting keys, env names, timeouts) into a table in the PR description.
2. Add `Jobsy.Infrastructure/Services/OpenAi/OpenAiEndpointResolver.cs` (+ interface `IOpenAiEndpointResolver`, registered in DI where the services are registered) with `ResolveAsync(OpenAiFeature feature, CancellationToken ct)`, which returns `(ApiKey, Model, BaseUrl)`. The per-feature defaults and setting keys go in one table (`OpenAiFeature` enum → defaults).
3. Replace the copies in the 11 services with calls to the resolver. Keep the constructor signatures compatible for tests where possible, or update the test construction.
4. Unit tests for the resolver: DB setting wins over config; config over default; per-feature default model equals the old value (one test case per feature, **with the literal old values from step 1**).

**Do not touch:** prompts and messages sent to OpenAI; HTTP call code; retry/timeout behaviour; data sent (the privacy review is separate, see code-review.md §0.6).

**Verify:**
- Build and tests green.
- The new resolver tests pass.
- `rg -n "ResolveApiKeyAsync" Jobsy.Infrastructure` shows only the resolver.
- Manual: on Acc/local with a key, the WhoAmI generation and translation still work (or rely on existing integration tests with fake handlers).

**Dependency:** none (no pending PR touches these services).
