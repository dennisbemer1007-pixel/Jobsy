> **Reference snapshot** (2 Oct 2026, `acceptatie` @ `3a15b0d7`, .NET 9 SDK 9.0.318). Line numbers and counts drift, so re-measure on your base before acting.

# Compiler / analyzer warnings on `acceptatie` (`3a15b0d7`)

Read-only analysis, 2 Oct 2026 (CEST). Fresh worktree of `origin/acceptatie`, built with:

`dotnet build Jobsy.sln -c Release --no-incremental` (.NET 9 SDK, analyzer settings from `Directory.Build.props` and `.editorconfig`)

## 1. Headline numbers

| What | Count |
|---|---:|
| Unique warnings (file, line, column, code), all analyzers | **470** |
| Same, but counted the way CI counts them (`.github/scripts/count-build-warnings.sh` only matches `CS`/`CA`/`IDE`/`SYSLIB`) | **411** |
| Baseline the code-quality gate accepts (`.github/quality/warning-baseline.txt`) | 377 |
| NuGet warnings (NU1xxx) during restore or build | **0** |
| Warnings in EF migrations or other generated code | 1 (CS0162, from the Razor generator for `WieZijnWij.razor`) |

Findings:
- **The code-quality gate fails on acceptatie right now:** 411 is more than the 377 baseline.
- **CI misses 59 warnings:** its counter does not match `RZ`, `ASP` or `BL` codes, so it never sees `RZ10012` (35), `ASP0006` (23) or `BL0007` (1).
- **`RZ10012` is a real bug, not noise.** See §4.

Warnings per project:

| Project | Unique warnings |
|---|---:|
| Jobsy.Tests | 163 |
| Jobsy.Infrastructure | 126 |
| Jobsy.Web | 87 |
| Jobsy.Core | 62 |
| Jobsy.Api | 32 |

## 2. Counts by code and project

| Code | Total | Core | Infra | Api | Web | Tests |
|---|---:|---:|---:|---:|---:|---:|
| CA1859 | 86 | 23 | 31 | 4 | 4 | 24 |
| CA1862 | 61 |  | 50 | 9 |  | 2 |
| IDE0060 | 35 | 14 | 8 | 1 | 8 | 4 |
| RZ10012 | 35 |  |  |  | 35 |  |
| CS0618 | 30 |  | 3 |  |  | 27 |
| CA1861 | 24 |  |  |  | 3 | 21 |
| ASP0006 | 23 |  |  |  |  | 23 |
| CA1822 | 20 | 1 | 3 |  |  | 16 |
| CA1826 | 18 | 1 | 5 | 3 | 2 | 7 |
| IDE0052 | 13 |  | 2 | 7 |  | 4 |
| CA1863 | 10 | 3 | 5 |  |  | 2 |
| CS1998 | 9 |  |  |  | 1 | 8 |
| CA1716 | 8 | 3 |  | 1 | 4 |  |
| IDE0059 | 8 | 2 | 2 |  | 1 | 3 |
| CA1051 | 7 |  |  |  |  | 7 |
| CA1068 | 6 | 4 | 1 |  | 1 |  |
| CA1512 | 6 | 1 | 5 |  |  |  |
| CA1865 | 6 |  |  |  |  | 6 |
| CS0414 | 6 |  |  |  | 6 |  |
| CS0168 | 5 |  |  |  | 5 |  |
| IDE0051 | 5 | 3 | 1 |  |  | 1 |
| CA1869 | 4 |  |  |  | 2 | 2 |
| CA1711 | 3 | 1 | 1 |  |  | 1 |
| CA1828 | 3 |  | 1 |  |  | 2 |
| CA1854 | 3 | 3 |  |  |  |  |
| CA2215 | 3 |  |  | 2 | 1 |  |
| CA1000 | 2 |  |  |  | 2 |  |
| CA1001 | 2 |  | 2 |  |  |  |
| CA1707 | 2 |  |  |  | 2 |  |
| CA1710 | 2 |  |  |  | 2 |  |
| CA1720 | 2 |  |  | 1 | 1 |  |
| CA1847 | 2 |  |  | 2 |  |  |
| CA1860 | 2 |  |  |  |  | 2 |
| CA2014 | 2 |  | 2 |  |  |  |
| CA2016 | 2 |  |  | 2 |  |  |
| CS0162 | 2 |  |  |  | 1 | 1 |
| BL0007 | 1 |  |  |  | 1 |  |
| CA1827 | 1 |  | 1 |  |  |  |
| CA1834 | 1 | 1 |  |  |  |  |
| CA2244 | 1 |  |  |  | 1 |  |
| CA2249 | 1 | 1 |  |  |  |  |
| CA2263 | 1 | 1 |  |  |  |  |
| CS0105 | 1 |  |  |  | 1 |  |
| CS0169 | 1 |  |  |  | 1 |  |
| CS0649 | 1 |  |  |  | 1 |  |
| CS8602 | 1 |  |  |  | 1 |  |
| CS8604 | 1 |  | 1 |  |  |  |
| CS8619 | 1 |  | 1 |  |  |  |
| CS8620 | 1 |  | 1 |  |  |  |
| **Total** | **470** | **62** | **126** | **32** | **87** | **163** |

Files with the most warnings:


| File | Warnings | Main codes |
|---|---:|---|
| `Jobsy.Web/Components/Pages/Werkgever/CandidateInsights.razor` | 33 | RZ10012 33 |
| `Jobsy.Tests/AdminOrganisationsBunitTests.cs` | 24 | ASP0006 23, CA1826 1 |
| `Jobsy.Infrastructure/Services/AssistantChatService.cs` | 21 | CA1862 21 |
| `Jobsy.Tests/RoleFunctionalRegressionTests.cs` | 14 | CA1822 8, CA1861 5, CA1862 1 |
| `Jobsy.Infrastructure/Services/CompanyRegistrationService.cs` | 8 | CA1862 6, CA1859 1, IDE0051 1 |
| `Jobsy.Tests/CoreFunctionalFlowE2ETests.cs` | 7 | CS0618 4, CA1859 3 |
| `Jobsy.Tests/CareerPathServiceTests.cs` | 6 | CS0618 6 |
| `Jobsy.Infrastructure/Services/AtsVacancyModerationService.cs` | 5 | CA1862 5 |
| `Jobsy.Web/Components/Pages/Candidate/TestDetail.razor` | 5 | CS0168 5 |
| `Jobsy.Api/Controllers/AuthController.cs` | 5 | CA1862 5 |
| `Jobsy.Tests/PassportFitCareerPhase3Tests.cs` | 5 | CS0618 5 |
| `Jobsy.Tests/VacancyCultureFitTranslationApiTests.cs` | 5 | CA1051 3, CA1861 1, CA1822 1 |
| `Jobsy.Core/Email/TransactionalEmails.Templates3.cs` | 4 | IDE0059 2, IDE0060 2 |
| `Jobsy.Infrastructure/Sales/SalesEmployerReadService.cs` | 4 | IDE0060 3, CA1859 1 |
| `Jobsy.Infrastructure/Services/DeepAnalysisService.cs` | 4 | IDE0052 2, IDE0060 2 |

## 3. Each code: meaning, fix, risk, and fix or suppress

Risk levels:
- **M**: mechanical, no behaviour change.
- **L**: low-risk behaviour change (exception type, perf, allocation).
- **B**: changes behaviour or public signatures, so it needs a review and tests.

| Code (n) | What it means | Typical fix | Risk | Fix or suppress |
|---|---|---|---|---|
| **CA1859** (86) | A private or internal member returns or takes an interface (`IReadOnlyList<T>`, `IEnumerable<T>`) where the concrete type is always used. The analyzer suggests the concrete type to avoid interface dispatch. | Change the signature to `List<T>`/`T[]`/`Dictionary<…>`. | M, but lots of churn | **Suppress** (`suggestion`). Read-only interfaces show intent here, and the gain is too small to matter for this app. |
| **CA1862** (61) | `x.ToLower() == y` or `x.ToLower().Contains(t)` is used for a case-insensitive comparison. | In-memory code: `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`. | **B** | **Suppress** for the EF query projects with a written reason. All 61 sit inside EF Core LINQ queries (email lookups, tracking codes, ATS/assistant search). EF translates `ToLower()` to SQL `lower()`, but **cannot translate the `StringComparison` overload, so the suggested fix throws at runtime**. The real fix later is a normalised-email column, `citext` or `EF.Functions.ILike`, which is a separate DB change. |
| **IDE0060** (35) | A parameter is never used. | Remove it, or rename it to `_` if the signature is fixed (interface, delegate, DI, endpoint). | B (signature change, call sites) | **Fix.** Some point at unfinished logic, e.g. `VacancyImageUrls(vacancyId)` and `PrivacyConstants(role)`. |
| **RZ10012** (35) | A Razor element looks like a component, but no component by that name is in scope, so it **renders as a raw HTML tag**. | Add `@using Jobsy.Web.Components.Employer` and `@using Jobsy.Web.Components.Werkgever.Insights`. | **B** (the components will start rendering) | **Fix now, it's a bug.** See §4. |
| **CS0618** (30) | Code uses a member marked `[Obsolete]`: `ExposeRegistrationActivationLinks`, `DeepAnalysisPriceEuro`, `CareerPathStepApiModel.StepMatchPercent/ActionHref/ActionLabel`. 27 are in tests and 3 in Infrastructure (seeder and DbContext mapping). | Finish the deprecation: drop the members, plus a migration for the two DB columns. Tests that deliberately cover legacy fields can use `#pragma warning disable CS0618` with a comment. | B (schema/API) | **Fix**, by finishing the deprecation. The obsolete messages already say "drop in a later migration". |
| **CA1861** (24) | A constant array is passed as an argument and gets allocated on every call. | Move it to a `static readonly` field. | M | Fix the 3 in Web. **Suppress in `Jobsy.Tests`**. |
| **ASP0006** (23) | bUnit/RenderTreeBuilder uses `seq++` as a sequence number, which breaks diffing. | Use literal sequence numbers. | M (tests only) | Fix (one file, `AdminOrganisationsBunitTests`), or suppress in tests. |
| **CA1822** (20) | A member uses no instance data, so it can be `static`. | Add `static`. | M | Fix the 4 in production code. Suppress in tests; xUnit helpers are often instance members on purpose. |
| **CA1826** (18) | LINQ `First()/Last()/Count()` is used on an indexable list. | Use `[0]`, `[^1]` or `.Count`. | L (`First()` throws `InvalidOperationException` on an empty list, `[0]` throws `ArgumentOutOfRangeException`) | Fix, checking each one for empty lists. |
| **IDE0052** (13) | A private field is written but never read, usually an unused injected `_environment`/`_configuration`. | Remove the field and the constructor parameter. | L (DI constructor shape; tests that `new` the class) | Fix. |
| **CA1863** (10) | `string.Format` is called repeatedly with the same format; the analyzer suggests caching a `CompositeFormat`. | Use `static readonly CompositeFormat`. | M | **Suppress.** The formats are mostly localized strings resolved at runtime, so they can't be cached statically. |
| **CS1998** (9) | An `async` method has no `await` (mostly Playwright `InitializeAsync`, plus one in `StatusPage.razor`). | Drop `async` and return `Task.CompletedTask`. | M | Fix. |
| **CA1716** (8) | An identifier is a reserved keyword in another .NET language (`Step`, `Set`, parameter `to`). | Rename. | B (public API rename) | **Suppress.** These are VB interop concerns and we ship no public library. |
| **IDE0059** (8) | A value is assigned but never used. | Remove the assignment, or use the value. | **B (can hide bugs)** | **Fix after review.** Example: `TransactionalEmails.AccountLockout` computes `durationLabel` but never puts it in the email. That is probably why the existing failing `AccountEmailCopyTests.AccountLockout_duration_matches_login_lockout_rules` cases fail (not verified). |
| **CA1051** (7) | A test class exposes public instance fields. | Make them properties or private. | M | Suppress in tests. |
| **CA1068** (6) | `CancellationToken` is not the last parameter (several `Jobsy.Core.Interfaces`). | Reorder parameters and update call sites, using named args. | B (interface signatures) | Fix in its own small PR. `.editorconfig` raises this rule to warning on purpose. |
| **CA1512** (6) | A hand-written `throw new ArgumentOutOfRangeException`. | `ArgumentOutOfRangeException.ThrowIfNegative/ThrowIfLessThan…`. | L (message text changes) | Fix. |
| **CS0414** (6), **CS0168** (5), **CS0169** (1), **CS0649** (1) | Dead fields and variables in Razor pages: `CreateVacancy._kvkBusy/_clientKvkNumber`, `_lastAnnounced` in two scans, unused `ex` in `TestDetail.razor`, and `Melden._selectedReason`, which is never set, so the chosen reason is lost after a validation error. | Remove them, or wire them up as intended (spinner, aria-live announce, keeping the selected reason). | L–B | Fix. Check each: unwired UI state is sometimes a missing feature. |
| **CA1865** (6), **CA1847** (2), **CA1834** (1) | A one-character string is used where a `char` overload exists. | `StartsWith('/')`, `Contains(',')`, `Append(',')`. | M | Fix. |
| **IDE0051** (5) | An unused private member: `TransactionalEmails.Bold/F/Fmt`, `CompanyRegistrationService.ValidateSalesOrAmbassadeurTrackingCodeAsync`. | Delete it. | M (check it isn't reached via reflection) | Fix. |
| **CA1869** (4) | A new `JsonSerializerOptions` is created per call. | Use a cached `static readonly` instance. | M (perf) | Fix. |
| **CA1711** (3), **CA1710** (2), **CA1720** (2), **CA1000** (2), **CA1707** (2, Web) | Naming: types end in `Queue`/`Collection`; `ApiError` should end in `Exception`; enum members `Int`/`Object`; static members on a generic `MeGetResult<T>`; underscores in OIDC callback parameters. | Rename. | B (renames across the solution, JSON/enum names) | **Suppress** with a reason. `CA1710` could optionally be fixed with a rename. |
| **CA1854** (3) | `ContainsKey` followed by the indexer, which looks the key up twice. | Use `TryGetValue`. | M | Fix. |
| **CA1828** (3), **CA1827** (1), **CA1860** (2) | `Count() > 0` / `CountAsync() > 0` / `Any()` on an array. | `Any()` / `AnyAsync()` / `Length > 0`. | M (also faster SQL) | Fix. |
| **CA2215** (3) | A `Dispose(bool)`/`DisposeAsyncCore` override does not call the base (`DualWindowRateLimiter`, `JobsyApiClientFactory.NonDisposingHandler`). | Call `base.Dispose(disposing)`. For `NonDisposingHandler`, skipping the base call **is the whole point**, so suppress that one inline with a comment. | L | Fix 2, suppress 1 inline. |
| **CA1001** (2) | A class owns a `SemaphoreSlim` but isn't `IDisposable` (`VacancyDiscoveryIndex`, `PingenLetterService`). | Implement `IDisposable`. | L | Fix. They're singletons, so the change is harmless. |
| **CA2014** (2) | `stackalloc` inside a loop in `PupilCodeService`, which risks a stack overflow. | Move the `stackalloc` out of the loop. | L (correctness) | **Fix.** |
| **CA2016** (2) | `PasswordResetController` does not forward its `CancellationToken` to `Task.Run`. | Pass `CancellationToken.None` explicitly. The mail runs fire-and-forget after the response on purpose, so do **not** forward the request token. | M | Fix, by stating the intent explicitly. |
| **CS8602/8604/8619/8620** (4) | Nullability mismatches in `SalesWalletPortalService`, `SalesPayoutRunService` (`CreditorName` may be null) and `VacancyDetail.razor`. | Add a null check or fix the annotation. | L–B (possible NRE in payout export) | **Fix.** |
| **CA2244** (1) | A duplicate dictionary key `/register/toegang` in `PageSeoCatalog`. Both values are identical, so it's harmless. | Delete one line. | M | Fix. |
| **CS0105** (1) | A duplicate `@using Jobsy.Web.Features` in `_Imports.razor`. | Delete it. | M | Fix. |
| **CS0162** (2) | Unreachable code: `WieZijnWij.razor` (in the Razor-generated output) and `AboutPageTests`. | Remove the dead branch or the constant condition. | M | Fix. |
| **CA2263** (1), **CA2249** (1) | `Enum.IsDefined(typeof…)` instead of the generic overload; `IndexOf(...) >= 0` instead of `Contains`. | Use the modern overload. | M | Fix. |
| **BL0007** (1) | A component `[Parameter]` (`WaStepCode.Expired`) has logic in its setter. | Make it an auto-property and move the logic to `OnParametersSet`. | B (render timing) | Fix with a bUnit test. |

## 4. Real bugs found through warnings

These are all already present on acceptatie. None of them came from today's merges.

1. **RZ10012: Kandidaatinzichten and the admin company page render empty tags instead of components.**
   - `Pages/Werkgever/CandidateInsights.razor` (33 elements) and `Components/Employer/InsightsLockedBlock.razor` use components that exist but are not in scope:
     - from `Components/Employer`: `InsightsStoryCard`, `InsightsDistributionBars`, `InsightsRankedList`, `InsightsTipsBlock`, `InsightsVacanciesList`, `EmployerTalentTabs`
     - from `Components/Werkgever/Insights`: `WgLockedCard`, `WgInsightsKpiLocked`, `WgInsightsPremium`
   - `Admin/Sections/CompanyDetailsSection.razor` uses `RaamflyerTools` the same way.
   - Neither `Jobsy.Web.Components.Employer` nor `Jobsy.Web.Components.Werkgever.Insights` is imported anywhere, so the browser receives literal `<InsightsStoryCard …>` tags.
   - The page sits behind the Employers feature flag, which probably hid this. It may also explain some of the existing failing `CandidateInsights*` tests.
   - Fix: add both `@using` lines to `Components/_Imports.razor`, then check the page visually and with bUnit.
2. **IDE0059 in `AccountLockout` email:** the lockout duration is computed but never shown. This likely explains the 6 existing failing `AccountLockout_duration_matches_login_lockout_rules` cases.
3. **CS8604 in `SalesPayoutRunService`:** a null `CreditorName` can reach the payout export line.
4. **CA2014 in `PupilCodeService`:** `stackalloc` inside a loop.
5. **CS0649/CS0414 in Razor:** state that was never wired up (the Melden reason after a validation error, the KvK busy spinner, aria-live announcements).

## 5. NuGet

- **No NU1xxx warnings.** Audit is on: `NuGetAudit=true`, `NuGetAuditLevel=moderate`.
- `dotnet list package --vulnerable --include-transitive` lists **no vulnerable packages** in any project. The AngleSharp NU1902 that the workflow comment mentions is gone.
- `--deprecated` (doesn't produce warnings, but worth tracking):
  - `xunit` 2.9.2 and its transitive parts `xunit.core`, `xunit.assert` and `xunit.extensibility.*` are flagged **Legacy**, with xunit.v3 as the successor (Jobsy.Tests).
  - Transitive `Azure.Identity` 1.11.4, `Microsoft.Identity.Client` 4.70.1 and `Microsoft.Identity.Client.Extensions.Msal` 4.61.3 are flagged deprecated ("Other") in Jobsy.Api and Jobsy.Tests. Bump them through their parent package, or pin newer versions in `Directory.Packages.props`.
- Notable `--outdated` packages (all at their latest 9.x patch; the bigger jumps are majors):
  - EF Core/ASP.NET 9.0.20 → 10.x, together with a .NET 10 move
  - Npgsql 9 → 10
  - Swashbuckle 7 → 10
  - Sentry 5 → 6
  - bunit 1.36 → 2.x
  - Microsoft.Identity.Web 3 → 4
  - ImageSharp 3 → 4
  - Playwright 1.49 → 1.63
  - Smaller: MailKit 4.18.1, AngleSharp 1.8.3, QRCoder 1.8, QuestPDF 2026.9
- **With TreatWarningsAsErrors**, add `<WarningsNotAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>`. Otherwise a newly published CVE would break every build overnight. The existing "Vulnerable package gate" step already fails CI on High/Critical.

## 6. Proposed `.editorconfig` suppressions (each with a reason)

```ini
[*.cs]
# EF Core translates ToLower()/ToUpper() to SQL lower()/upper() but cannot translate
# string.Equals(..., StringComparison). All CA1862 hits are inside IQueryable predicates.
# Long-term: normalised/citext email + tracking-code columns (separate DB change).
dotnet_diagnostic.CA1862.severity = none
# Internal API shape: read-only interfaces express intent; perf gain is negligible here.
dotnet_diagnostic.CA1859.severity = suggestion
# Format strings are localised at runtime; CompositeFormat cannot be cached statically.
dotnet_diagnostic.CA1863.severity = suggestion
# VB/C++ interop naming rules; we ship no public library.
dotnet_diagnostic.CA1716.severity = none
dotnet_diagnostic.CA1711.severity = none
dotnet_diagnostic.CA1720.severity = none
dotnet_diagnostic.CA1000.severity = none

[Jobsy.Tests/**.cs]
# Test code: readability over micro-perf and API-design rules.
dotnet_diagnostic.CA1822.severity = none
dotnet_diagnostic.CA1861.severity = none
dotnet_diagnostic.CA1051.severity = none
dotnet_diagnostic.CA1865.severity = none
dotnet_diagnostic.CA1860.severity = none
dotnet_diagnostic.CA1869.severity = none
dotnet_diagnostic.CA1859.severity = none
```

Scoping CA1862 to `Jobsy.Infrastructure/**` and `Jobsy.Api/**` would also work. Every hit is EF either way.

Effect: roughly **226** of the 470 disappear without touching code (CA1859 86, CA1862 61, CA1863 10, CA1716 8, CA1711/CA1720/CA1000 7, and in tests CA1822 16, CA1861 21, CA1051 7, CA1865 6, CA1860 2, CA1869 2; the test-only CA1859 hits are already counted under CA1859).

Also fix the CI counter: add `RZ|ASP|BL` to the regex in `count-build-warnings.sh` so Razor/Blazor warnings are gated too.

## 7. Is 0 warnings + TreatWarningsAsErrors realistic?

**Yes.** About 244 warnings remain after the justified suppressions, and most are mechanical. Five PR-sized steps:

| Step | Content | Approx. warnings removed | Risk |
|---|---|---:|---|
| 1. Config | Justified `.editorconfig` suppressions (§6). Add `RZ|ASP|BL` to the CI counter. Reset `warning-baseline.txt` to the new real count. | ~226 | None (no code) |
| 2. Real bugs | RZ10012 `@using` (35) with a visual/bUnit check of Kandidaatinzichten and admin company details; AccountLockout duration (IDE0059); nullability CS86xx (4); CA2014; CA2215/CA1001; CS0649/CS0414/CS0168/CS0169; CA2244; CS0105; CS0162; BL0007 | ~66 | Behaviour (intended fixes) |
| 3. Unused code | IDE0060 (35), IDE0052 (13), IDE0051 (5), remaining IDE0059 | ~59 | Signatures and DI constructors |
| 4. Obsolete and signatures | CS0618 (30): drop the obsolete members plus a migration for `ExposeRegistrationActivationLinks`/`DeepAnalysisPriceEuro`, or justified pragmas in legacy tests. CA1068 (6) parameter order. CA1710 (2), CA2016 (2) | ~40 | Schema/API, so run the migration tests |
| 5. Mechanical rest and the gate | CA1826, CA1512, CA1854, CA1828/27, CA1847/34/65, CA2249/63, CS1998, ASP0006 (tests), the remaining CA1822/CA1861/CA1869 in production code, CA1707 (2, Web: rename or inline-suppress). Then set `TreatWarningsAsErrors=true` (plus `WarningsNotAsErrors` NU1901–NU1904) and drop the baseline file. | ~79 | Low |

Steps 1 and 5 can be done by a bot in an hour each. Steps 2–4 each need about half a day to a day, including tests.

Caveats:
- `Directory.Build.props` currently says *"Do not set TreatWarningsAsErrors"* (a prompt-01 guardrail), so turning it on is Dennis's call.
- A softer option is to enable `-warnaserror` only in the code-quality CI job, so local builds stay lenient.
- Every open feature branch adds warnings: today's merges took the CI count from 377 to 411. Once the gate is strict, each stack must be warning-free before it merges, so do step 5 when few stacks are open.
- `EnforceCodeStyleInBuild=true` and `AnalysisLevel=latest-recommended` mean an SDK or analyzer upgrade (e.g. .NET 10) can bring new warnings. Pin `AnalysisLevel` to `9.0-recommended` when turning on errors, and raise it on purpose later.

## 8. Outdated / deprecated

Read-only check, 2 Oct 2026 (CEST), on a fresh worktree of `origin/acceptatie` (`3a15b0d7`). Commands:
- `dotnet list package --outdated` (also with `--highest-minor`, `--highest-patch` and `--include-transitive`)
- `npm outdated` / `npm audit`
- version banners of the vendored files in `wwwroot/lib`

### 8.1 Runtime / SDK

| Item | Current | Latest | Note |
|---|---|---|---|
| TargetFramework (`Directory.Build.props`, plus `tools/i18n-export` and `tools/generate-icons`) | `net9.0` (STS) | `net10.0` (LTS, 10.0.12 of 8 Sep 2026, supported until 14 Nov 2028) | **.NET 9 reaches end of support on 10 Nov 2026, 39 days from now.** After that there are no security patches. It has been in the security-fixes-only "maintenance" phase since May 2026. |
| `global.json` SDK | `9.0.100`, `rollForward: latestFeature` | SDK 10.0.x | Any 9.0.x SDK ≥ 9.0.100 works; the box has 9.0.318. |
| Docker images (`Jobsy.Api/Dockerfile`, `Jobsy.Web/Dockerfile`, used by Render) | `mcr.microsoft.com/dotnet/sdk:9.0` / `aspnet:9.0` (floating tag) | `:10.0` | The floating tag picks up 9.0.x patches on each deploy, but nothing after EOL. |
| CI (`setup-dotnet`) | `9.0.x` in 3 workflows | `10.0.x` | Changing it needs a push with `workflow` scope. |
| .NET runtime packages | 9.0.20 everywhere | 9.0.20 is the latest 9.x patch | Fully patched within 9.x. |

**Risk of moving to net10:**
- **Medium.** It is one coordinated PR touching: the TFM, global.json, both Dockerfiles, the 3 workflows, and every `Microsoft.*`/EF/Npgsql package to 10.x at the same time. The 10.x packages require net10, so they can't move separately.
- Expect:
  - new analyzer warnings (`AnalysisLevel=latest-recommended`, see §7)
  - EF Core 10 query/translation changes, so re-run the migration and `PendingModelChanges` tests; a new snapshot may be needed
  - Npgsql 10 type-mapping changes
  - ASP.NET 10 behaviour changes (e.g. cookie-auth redirect vs 401 for API endpoints, `WithOpenApi` deprecations)
- Recommended: do it before 10 Nov 2026 on its own stack, with an acceptatie deploy and smoke test.

### 8.2 NuGet: direct packages (`Directory.Packages.props`)

Columns: current, latest, latest in the same minor/major line, bump type, risk.

| Package | Projects | Current | Latest | Safe step (same major) | Bump | Upgrade risk |
|---|---|---|---|---|---|---|
| Microsoft.AspNetCore.Authentication.Google / .OpenIdConnect | Web | 9.0.20 | 10.0.12 | already latest 9.x | major | Tied to net10 (see 8.1). Medium; recheck the OIDC/Entra login flows. |
| Microsoft.AspNetCore.DataProtection | Infra | 9.0.20 | 10.0.12 | already latest 9.x | major | Tied to net10. Low; the key-ring format is compatible. |
| Microsoft.AspNetCore.Mvc.Testing | Tests | 9.0.20 | 10.0.12 | already latest 9.x | major | Tied to net10. Low. |
| Microsoft.EntityFrameworkCore (+ .Design, .InMemory, .Relational) | Infra/Api/Tests | 9.0.20 | 10.0.12 | already latest 9.x | major | Tied to net10. **Medium–high:** query translation changes, model snapshot/migrations, and possibly InMemory behaviour in tests. Run the migration guard tests. |
| Microsoft.Extensions.* (Configuration, Binder, Hosting.Abstractions, Http, Options.ConfigurationExtensions) | Infra/Tests | 9.0.20 | 10.0.12 | already latest 9.x | major | Tied to net10. Low. |
| Npgsql | Web | 9.0.5 | 10.0.3 | already latest 9.x | major | Tied to EF 10. Medium (date/time and enum mapping changes). |
| Npgsql.EntityFrameworkCore.PostgreSQL (+ .NetTopologySuite) | Infra | 9.0.4 | 10.0.3 | already latest 9.x | major | Tied to EF 10. Medium–high (PostGIS mapping, migrations). |
| Microsoft.Identity.Web | Api | 3.8.3 | 4.16.0 | **3.15.1** (3.8.4 patch) | major | Low risk to 3.15.1, which should also bring newer transitive Azure.Identity/MSAL (not verified; otherwise pin them, see 8.3). Going to 4.x is medium: API and options changes in token acquisition. |
| Sentry.AspNetCore | Api/Web | 5.14.1 | 6.12.0 | **5.16.3** | major | Low to 5.16.3. 6.x is medium: options were renamed/removed, so check the `UseSentry` config and that error reporting still arrives. |
| Swashbuckle.AspNetCore | Api | 7.2.0 | 10.2.3 | **7.3.2** | major | Low to 7.3.2. 8–10 bring Microsoft.OpenApi 2.x, which breaks any custom filter, and on net10 the built-in `Microsoft.AspNetCore.OpenApi` is the better choice. Medium. |
| SixLabors.ImageSharp | Infra, tools | 3.1.12 | 4.1.2 | already latest 3.x | major | Medium: API changes in processing and encoders. Re-check the Six Labors licence terms before a major bump. Keep `ImageSharp.Drawing` (2.1.7) in step. |
| QuestPDF | Infra | 2025.7.1 | 2026.9.1 | **2025.12.4** (2025.7.4 patch) | year-major | Low–medium: rendering/layout differences in generated PDFs (sales PDFs, flyers). Compare output visually. Check the licence (Community vs commercial) on each major. |
| AngleSharp | Infra/Tests | 1.5.0 | 1.8.3 | 1.8.3 (1.5.2 patch) | minor | Low; also used by bunit, so keep both compatible. |
| MailKit | Infra | 4.17.0 | 4.18.1 | 4.18.1 | minor | Low. |
| QRCoder | Infra | 1.6.0 | 1.8.0 | 1.8.0 | minor | Low; some renderer APIs are marked obsolete, so watch for new CS0618 warnings. |
| WebPush | Infra | 1.0.12 | 1.0.13 | 1.0.13 | patch | Low. It still pulls the old, unmaintained `Portable.BouncyCastle` 1.8.1.3. The library itself is barely maintained, so consider an alternative long-term. |
| bunit | Tests | 1.36.0 | 2.11.3 | **1.40.0** | major | Low to 1.40. 2.x is **high churn**: `TestContext` became `BunitContext` and the render APIs were renamed, which touches every bUnit test. |
| xunit | Tests | 2.9.2 | 2.9.3 | 2.9.3 | patch | Low. 2.x is marked **legacy/deprecated** in favour of `xunit.v3`, a large migration (assembly attributes, `IAsyncLifetime` changes, runner). |
| xunit.runner.visualstudio | Tests | 2.8.2 | 4.0.0 | — | major | Low–medium; 3.x+ runs both v2 and v3 tests. Do it together with the xunit.v3 move. |
| Microsoft.NET.Test.Sdk | Tests | 17.12.0 | 18.10.1 | **17.14.1** | major | Low to 17.14.1; 18.x is low–medium. |
| coverlet.collector | Tests | 6.0.2 | 10.1.0 | **6.0.4** | major | Low (only CI coverage). |
| Microsoft.Playwright | Tests | 1.49.0 | 1.63.0 | 1.63.0 | minor | Low–medium: CI must install matching browsers (`playwright install`), and screenshots/selectors can change slightly. |

These are already at the latest version: Microsoft.IdentityModel.Tokens / System.IdentityModel.Tokens.Jwt 8.23.0, Newtonsoft.Json 13.0.4 (pinned against NU1903), and SixLabors.ImageSharp.Drawing 2.1.7.

### 8.3 NuGet: transitive (`--include-transitive`, summarised)

- **125 transitive packages are outdated:** 96 major, 24 minor, 5 patch. About 90 of the majors are the `Microsoft.*`/`System.*` 9.x → 10.x family, which moves with net10.
- **Deprecated (from `--deprecated`):** `Azure.Identity` 1.11.4, `Microsoft.Identity.Client` 4.70.1 and `Microsoft.Identity.Client.Extensions.Msal` 4.61.3, all pulled in by `Microsoft.Identity.Web.Certificate` 3.8.3 (Api and Tests). Fix by bumping Microsoft.Identity.Web to 3.15.1, or pin current versions (Azure.Identity 1.21.0, Microsoft.Identity.Client 4.90.1) in `Directory.Packages.props` (transitive pinning is enabled).
- **Old ASP.NET 9.0.0 packages in Jobsy.Tests**, pulled in by `bunit.web` 1.36: `Microsoft.AspNetCore.Components*`, `Authentication.JwtBearer`. This is test-only and the shared framework (9.0.20) wins at runtime; bunit 1.40 or a pin cleans it up.
- **Design-time only:** `Microsoft.CodeAnalysis.*` 4.8.0 and `Humanizer.Core` 2.14.1, via `Microsoft.EntityFrameworkCore.Design` (Api/Infra). Not a runtime risk.
- **Others:** `Microsoft.OpenApi` 1.6.22 (via Swashbuckle 7; 2.x/3.x come with Swashbuckle ≥ 8), `Portable.BouncyCastle` 1.8.1.3 (via WebPush, unmaintained), `AngleSharp.Css` 1.0.0-beta.144 (via bunit), `System.Text.Encodings.Web` 4.7.2 (Api/Tests).
- **No known vulnerabilities** in any project (`--vulnerable --include-transitive`), and no NU190x warnings.

### 8.4 npm

| package.json | Package | Current | Latest | Status |
|---|---|---|---|---|
| `tools/css/package.json` (has a lockfile) | `lightningcss-cli` | 1.33.0 (pinned) | 1.33.0 | Up to date. `npm audit`: 0 vulnerabilities. |
| `package.json` (root, **no lockfile**) | `pptxgenjs` | ^4.0.1, resolves to 4.0.1 | 4.0.1 | Latest, but `npm audit` reports **2 high** issues through the transitive `image-size` 1.2.1 (GHSA-5p2g-fcmc-qvqq, GHSA-w3rx-r6r6-pgpr: denial of service from infinite loops on crafted JXL/HEIF/ICNS images; fixed in image-size 2.0.3+, latest 2.0.4). |

About the root `package.json`:
- It is only used by `docs/demo/build-pptx.cjs`, a local script that builds a demo slide deck. It is not part of the app, the Docker images or CI, and it only reads our own images, so the **real risk is very low**.
- Options:
  - add an `"overrides": { "image-size": "^2.0.4" }`. Medium risk: image-size 2 changed its API, so test that the deck still builds.
  - or move the script into `tools/` with its own lockfile.
- Note that `npm audit fix` suggests going *down* to pptxgenjs 4.0.0.

### 8.5 Vendored JS/CSS (`Jobsy.Web/wwwroot/lib`)

There are no CDN links; everything is self-hosted. There is no bootstrap, leaflet, chart.js or jQuery. `js/*.js` is all our own code.

| Library | Files | Current | Latest | Bump | Upgrade risk |
|---|---|---|---|---|---|
| MapLibre GL JS | `lib/maplibre/maplibre-gl-csp.js`, `-csp-worker.js`, `.css`, `.map` | 5.24.0 (latest 5.x) | 6.11.2 | major | Medium. v6 has breaking API changes (removed deprecated options, event/projection changes). `jobsyMapLibre.js`, `jobMap.js` and `vacancyDetailMap.js` need retesting on desktop and mobile, along with the map Playwright tests (Banenkaart*, JobMapPinsClusters). Keep the CSP build (`-csp`). |
| html2canvas | `lib/html2canvas/html2canvas.min.js` | 1.4.1 (Jan 2022) | 1.4.1 | none | It is the latest, but **unmaintained since 2022** (no releases, many open issues). A maintained drop-in fork exists (`html2canvas-pro` 2.5.0, supports modern CSS colours). Low–medium risk to swap if screenshot/feedback capture breaks on modern CSS. |

Side note found while checking MapLibre:
- `App.razor` hides `.maplibregl-ctrl-attrib` and `jobsyMapLibre.js` sets `attributionControl: false`.
- The OpenFreeMap tiles use OpenStreetMap data (ODbL), which **requires visible attribution** ("© OpenStreetMap contributors").
- No replacement attribution text was found in `Jobsy.Web` outside the privacy page. Worth a quick check; this is a licence issue, not a version issue.

### 8.6 Suggested order (PR-sized)

1. **Low-risk 9.x bumps, no TFM change:**
   - Microsoft.Identity.Web 3.15.1, plus pins for Azure.Identity/MSAL if still old
   - Sentry 5.16.3, Swashbuckle 7.3.2
   - AngleSharp 1.8.3, MailKit 4.18.1, QRCoder 1.8.0, WebPush 1.0.13
   - QuestPDF 2025.12.4 (check PDFs)
   - bunit 1.40.0, xunit 2.9.3, Test.Sdk 17.14.1, coverlet 6.0.4, Playwright 1.63 (with a browser install)
   - Then run the full test suite plus an acceptatie smoke test.
2. **net10 + all 10.x Microsoft/EF/Npgsql packages** in one stack, **before 10 Nov 2026**:
   - TFM, global.json, Dockerfiles, CI `setup-dotnet` (needs `workflow` scope)
   - pin `AnalysisLevel` first to avoid a flood of new warnings
   - run the fresh-DB migration and `PendingModelChanges` tests, deploy to acceptatie, smoke-test logins (local and Entra/Google)
3. **Majors with API churn, one per PR:** Sentry 6, Swashbuckle → `Microsoft.AspNetCore.OpenApi` (or Swashbuckle 10), ImageSharp 4 (after a licence check), Microsoft.Identity.Web 4, QuestPDF 2026.x.
4. **Test stack:** xunit.v3 + runner 4 + bunit 2. Large but mechanical; do it when few feature stacks are open.
5. **Frontend:** MapLibre 6 (with map Playwright tests), a possible html2canvas → html2canvas-pro swap, and the OSM attribution fix. The root `package.json` image-size override or moving the script is a separate tiny PR.
