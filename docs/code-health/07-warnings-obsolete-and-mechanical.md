# 07: Warnings cleanup 3: obsolete members, parameter order, mechanical rest

- **Branch:** `cursor/code-health-07-obsolete-mechanical`, from `cursor/code-health-06-unused`.
- **PR:** into that branch.
- Re-measure first. Also include any net10 analyzer codes that 05 deferred to "07".

## §1 CS0618: finish the deprecations (30 hits on 2 Oct: 27 in tests, 3 in Infrastructure)

| Obsolete member | Message | Plan |
|---|---|---|
| `PlatformFeatureSettings.ExposeRegistrationActivationLinks` / `JobsyFeatureOptions.ExposeRegistrationActivationLinks` | "Unused since auth 06; drop in a later migration" | **Drop it (Decision 8, approved).** Remove the property from the entity and the options, and add a **migration that drops the column**. Make sure `MigrationsApplyOnFreshDatabaseTests` and `PendingModelChangesTests` pass and the duplicate-scan stays clean. Remove the test usages (`SalesManagerCommissionTests`, `PlatformRobustnessTests`, `Sprint7RegistrationTests`, `LenderRegistration10Tests`, `FreePublishRulesTests`, `CoreFunctionalFlowE2ETests`, `CompanyVerificationServiceTests`, …). Delete any config key in `appsettings*.json` / `render.yaml` that only fed this option; **do not edit Render itself**, list the key for Dennis. |
| `FlexCommercialSettings.DeepAnalysisPriceEuro` | "Use DeepTestPrice*Euro per kind" | **Drop it (Decision 8, approved).** Remove it from the seeder (`PlatformSettingsSeeder.cs` ~l.277), the DbContext mapping (`JobsyDbContext.cs` ~l.1073) and the entity, and add a migration that drops the column. Before writing the migration, check that every reader uses the per-kind `DeepTestPrice*Euro` settings and that they are seeded with defaults. Note in the PR that the old value is not migrated, because the per-kind prices replace it. |
| `CareerPathStepApiModel.StepMatchPercent / ActionHref / ActionLabel` | "Replaced by StepFitBand / ActionKinds" | If the API no longer emits them, remove them from the model and update `CareerPathServiceTests` / `PassportFitCareerPhase3Tests` to assert the new fields. If they are still sent for old clients (check the Web client), keep them, and the tests that cover legacy compatibility get `#pragma warning disable CS0618 // legacy wire-compat assertion`. |

**Migrations rule:** one migration per dropped column with a clear name (`DropExposeRegistrationActivationLinks`, …). Run the fresh-DB guard, PendingModelChanges and the older-schema replay (as in 04), plus the API smoke test. **Never edit an existing migration.**

## §2 Signatures

- **CA1068** (6): `CancellationToken` must be the last parameter in `Jobsy.Core/Interfaces/IVacancyProductService.PublishAsync`, `ITranslationService.TranslateVacancyAsync`, `IDeviceSessionService.CreateAsync`, `ICandidateCareerInterestService.GetAsync`, ….
  - Reorder the parameters and update all implementations and call sites, using named arguments where order is ambiguous.
  - Optional `bool`/`string?` parameters come **before** the token.
- **CA1710** (2): `Jobsy.Web/Services/ApiError.cs`, `Services/Careers/CareerApiError.cs`. They derive from `Exception`, so rename them to `ApiErrorException` / `CareerApiErrorException`. Or, if they are not really exceptions, stop deriving from `Exception`. Pick whichever is the smaller diff and explain the choice.
- **CA1707** (2, Web): `EntraOidcOptionsApplier.ValidateMicrosoftIssuer` parameter names → camelCase.

## §3 Mechanical rest (production code)

Fix all of these; they are behaviour-neutral or tiny:
- **CA1826** (18): `First()` / `Last()` / `Count()` on lists → indexers or `.Count`. Check each one for empty collections; `First()` throws `InvalidOperationException`, `[0]` throws `ArgumentOutOfRangeException`. Keep the semantics, and add guards where an empty list is possible.
- **CA1512** (6): use the `ArgumentOutOfRangeException.ThrowIf*` helpers.
- **CA1854** (3): `TryGetValue`.
- **CA1828** / **CA1827**: `AnyAsync()` / `Any()`.
- **CA1847** / **CA1834** / **CA1865**: `char` overloads.
- **CA2249**: `Contains`. **CA2263**: generic `Enum.IsDefined<T>`.
- **CA1861** / **CA1869** (production code): `static readonly` arrays and a cached `JsonSerializerOptions` (`PupilAuthEndpoints.cs` ~l.73, `JobsyApiClient.Sales.cs` ~l.1012).
- **CS1998** (9): the Playwright `InitializeAsync` methods and `StatusPage.razor` ~l.189: drop `async` and return `Task.CompletedTask`.
- **ASP0006** in tests: suppressed in 02. Optionally replace `seq++` with literals in `AdminOrganisationsBunitTests`.

Try `dotnet format analyzers --diagnostics CA1826 CA1512 CA1854 CA1828 CA1827 CA1847 CA1834 CA1865 CA2249 CA2263 --severity info` first, then review the diff by hand.

## Acceptance

- `dotnet build -c Release` shows **0 warnings**, or only the ones explicitly deferred to 08–10 (package-specific), each listed. Set `warning-baseline.txt` to that exact number (target 0).
- Full tests: no new failures. Fresh-DB migration test, PendingModelChanges and the older-schema replay pass. API smoke test OK.
- The PR lists the dropped columns and migrations, and any config keys Dennis must remove in Render (don't touch Render yourself).
