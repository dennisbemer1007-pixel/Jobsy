# 05: Warnings cleanup 1: behaviour-adjacent fixes + .NET 10 analyzer level

- **Branch:** `cursor/code-health-05-behaviour`, from `cursor/code-health-04-net10`.
- **PR:** into that branch.
- The codes and counts come from [warnings-analysis.md](warnings-analysis.md) §3 and are re-measured on the base. **Re-measure first**: after 04 some lines and counts have moved. Paste the per-code table of the base in the PR.

## §0 Raise the analyzer level

- In `Directory.Build.props`, change `AnalysisLevel` from `9.0-recommended` (the pin from 04) to `10.0-recommended`.
- Build and list the **new** codes it introduces. For each one, decide **fix or justified suppress** using the same criteria as 02:
  - Generated code, test-only micro-perf and naming rules → suppress with a reason.
  - Anything about correctness, dispose, async or security → fix.
- New hits of codes that belong in 06 or 07 can wait for those steps; list them in the PR.

## §1 Fix (each may change behaviour, so each needs a test or a reasoned "no behaviour change")

| Code | Where (at analysis time) | Fix |
|---|---|---|
| CA2215 | `Jobsy.Api/Security/DualWindowRateLimiter.cs` (~l.79 `DisposeAsyncCore`, ~l.96 `PairLease.Dispose(bool)`) | Call `base.DisposeAsyncCore()` / `base.Dispose(disposing)`. Unit-test that the rate limiter can be disposed twice and that leases release correctly. |
| CA2215 | `Jobsy.Web/Services/JobsyApiClientFactory.cs` ~l.59 `NonDisposingHandler.Dispose(bool)` | **Intentionally** skips the base call: the handler must not dispose the shared inner handler. Use an inline `#pragma warning disable CA2215 // shared inner handler must outlive this wrapper` with a test that proves the inner handler survives. |
| CA1001 | `Jobsy.Infrastructure/Services/VacancyDiscoveryIndex.cs` (`_refreshLock`), `Jobsy.Infrastructure/Services/Letters/PingenLetterService.cs` (`_tokenGate`) | Implement `IDisposable` (dispose the `SemaphoreSlim`). Both are singletons, so check the DI lifetime and that nothing uses them after disposal. |
| CA2016 | `Jobsy.Api/Controllers/PasswordResetController.cs` ~l.106 / ~l.129 (`Task.Run`) | The mail is sent fire-and-forget **after** the response on purpose. Pass `CancellationToken.None` explicitly with a comment. Do **not** forward the request token (it would cancel the mail). |
| CS8602 / CS8604 / CS8619 / CS8620 | `Jobsy.Web/Components/Pages/VacancyDetail.razor` ~l.904; `Jobsy.Infrastructure/Sales/SalesWalletPortalService.cs` ~l.126/224 (tuple `Address` nullability); anything left in `SalesPayoutRunService` after 01 | Fix the annotations or add null guards. In `SalesWalletPortalService` decide whether `Address` is nullable end-to-end (DTO and UI show "—" when it is missing). |
| BL0007 | `Jobsy.Web/Components/Registration/WaStepCode.razor` ~l.59 (`Expired` parameter with setter logic) | Make it an auto-property and move the logic to `OnParametersSet`. A bUnit test checks that an expired code shows the expired state and that a re-render with `Expired=false` resets it. |
| CS0414 / CS0169 / CS0168 | `Pages/Werkgever/CreateVacancy.razor` ~l.807/809 (`_clientKvkNumber`, `_kvkBusy`); `CareerTest.razor` / `ValuesScan.razor` ~l.133 (`_lastAnnounced`); `VacancyDiscovery.razor` ~l.1118 (`_listRailMapReady`); `Pages/Candidate/TestDetail.razor` (5× unused `ex`) | **Check each one for a missing feature before deleting it:** `_kvkBusy` probably meant a spinner or disabled button during the KvK lookup, and `_lastAnnounced` an aria-live dedupe. Wire it up if that was clearly the intent, otherwise delete it. For `TestDetail` `catch (… ex)`: log through the existing `ILogger`/`UserFacingError` pattern, or use `catch (…)` without a variable. |
| CS0162 | `Components/Pages/Legal/WieZijnWij.razor` (unreachable branch in generated code: a constant `if`) and `Jobsy.Tests/AboutPageTests.cs` ~l.117 | Remove the dead branch or the constant condition. |
| CA2244 | `Jobsy.Web/Seo/PageSeoCatalog.cs` ~l.163–164 (duplicate `/register/toegang`) | Delete the duplicate line. |
| IDE0059 | The rest after 01 (`HaaglandenVacanciesSeeder.cs` ~l.197 `role`, `IntegrationHealthStub.cs` ~l.62 `view`, tests) | Remove the assignment, or use the value if it was clearly meant to be used (explain which). |

## Acceptance

- Each code in §1 is gone from the build, or has a justified inline suppression with a test.
- The new `10.0-recommended` analyzer codes are triaged (fixed or suppressed in `.editorconfig`, with reasons), and the list is in the PR.
- The warning count is lower than the base. `warning-baseline.txt` is lowered to the exact new count, as the 02 rule requires.
- Full tests and Playwright suites (VacancyDetail, registration, Kompas/tests pages): no new failures. Fresh-DB migration test and PendingModelChanges pass.
