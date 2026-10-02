# 09: Test stack: xunit.v3, runner, bunit 2

- **Branch:** `cursor/code-health-09-test-stack`, from `cursor/code-health-08c-identityweb4`, or from `origin/acceptatie` if 01–08c are merged.
- **PR:** into that branch.
- **Test-only changes**, plus `Directory.Packages.props`.
- Large but mechanical.
- **Precondition (Decision 5): run only after the Werkgever and Scholen stacks have been merged into `acceptatie`.** Before starting, check `gh pr list --base acceptatie --state open` and the merge history of `origin/acceptatie`.
  - If any Werkgever (`cursor/werkgever-*`) or Scholen (`cursor/scholen-*`) PR is still open, **stop and report which ones**. Don't open a PR.
  - Every other open test file will still conflict, so list the open stacks in the PR description.

## Packages

| From | To |
|---|---|
| `xunit` 2.9.3 (deprecated, "Legacy") | `xunit.v3` (latest 3.x) |
| `xunit.runner.visualstudio` 2.8.2 | latest 3.x/4.x (supports v3) |
| `Microsoft.NET.Test.Sdk` 17.14.x | latest 18.x |
| `coverlet.collector` 6.0.4 | latest |
| `bunit` 1.40.x | latest 2.x |

## Work

1. **xunit.v3** (follow the official "Migrating from v2 to v3" guide):
   - `Jobsy.Tests.csproj` becomes `<OutputType>Exe</OutputType>`.
   - `IAsyncLifetime` now returns `ValueTask` (`InitializeAsync`/`DisposeAsync`); update all fixtures, including the Playwright ones.
   - Replace `ITestOutputHelper` usings and `Xunit.Abstractions` with `Xunit`.
   - `[Theory]` data and `MemberData` need type checks; `SkipException` and `Assert.Skip` replace custom skip helpers.
   - Check collection fixtures and parallelism settings (`xunit.runner.json`).
2. **bunit 2**:
   - `TestContext` → `BunitContext`, `RenderComponent<T>` → `Render<T>`, `SetParametersAndRender` → `Render` with parameters.
   - Services/JSInterop setup API changes; `FakeNavigationManager` → `BunitNavigationManager`.
   - Do it with a scripted rename first, then fix by hand.
   - The shared helpers in `Jobsy.Tests/TestSupport/` (e.g. `StubTestAccountsRuntime`, bUnit base classes) change first, then the tests.
3. **CI**: the test filter syntax in `pr-tests.yml` / `acceptatie-smoke.yml` must still select the same tests.
   - With xunit.v3 + `dotnet test` (VSTest bridge) `--filter "FullyQualifiedName~…"` still works. Check the counts: **same number of discovered tests before and after** (print `--list-tests` counts for both).
   - Needs `workflow` scope only if the files change.
4. Keep the `[Trait]` / category names the filters use.

## Acceptance

- `dotnet test` discovers the **same number of tests** as before (±0, or a listed reason).
- The same pass/fail set as the base, with Appendix A failures unchanged unless fixed.
- The Playwright suites run.
- 0 new warnings (the xunit v3 analyzers, `xUnit1xxx`, can flag new things: fix them or justify).
- `--deprecated` lists nothing.
