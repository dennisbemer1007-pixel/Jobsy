Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 01: Guardrails: .editorconfig, Directory.Build.props, analyzer and quality workflow (no code changes)

**Goal:** add guardrails that stop new mess without touching existing application code. Behaviour must stay the same.

**Context and evidence (from docs/review/code-review.md §6):**
- There is no `.editorconfig` and no `Directory.Build.props`, and no analyzers are configured. The default build shows 1 CS warning (`Jobsy.Api/Program.cs(146,18)` CS8602) and NU1902 (AngleSharp).
- With `AnalysisMode=Recommended` there are 2,193 warnings. 1,292 of them are CA1707 in tests (underscore test names, intended) and 193 are IDE0005.
- CI (`.github/workflows/pr-tests.yml`) has no format, analyzer, vulnerability or secret check.

**Do:**
1. Add a root `.editorconfig` that **matches the current style**: 4 spaces, file-scoped namespaces preferred, `var` allowed, `_camelCase` private fields, PascalCase types/members, `Async` suffix, CRLF/LF as currently used (check with `git ls-files --eol`), UTF-8. Set these severities:
   - `suggestion` for style rules;
   - `warning` for IDE0051, IDE0052, IDE0060, IDE0059, CA2016, CA2208, CA1001, CA1068;
   - `none` for CA1707 inside `Jobsy.Tests/**`, and for CA1848 / CA1305 / CA1304 / CA1311 for now (culture rules will be a later pass);
   - for `Jobsy.Infrastructure/Data/Migrations/**`: `generated_code = true`.
2. Add a root `Directory.Build.props` that centralises `TargetFramework net9.0`, `Nullable enable`, `ImplicitUsings enable`, `AnalysisLevel latest-recommended` and `EnforceCodeStyleInBuild true`. Remove only the duplicated properties from the 5 csproj files. Keep `IsPackable` in the tests project. **Do not** set `TreatWarningsAsErrors` globally.
3. Add a NEW workflow `.github/workflows/code-quality.yml` (do not edit `pr-tests.yml`, because pending PRs edit its test filter lists). It runs on `pull_request` and has these steps:
   - `dotnet build Jobsy.sln -c Release`, then count unique warnings and **fail only if the count is higher than `.github/quality/warning-baseline.txt`** (commit the baseline number you measure);
   - `dotnet format whitespace --verify-no-changes --include <changed .cs files in the PR>` (changed files only, so no mass reformat);
   - `dotnet list package --vulnerable --include-transitive`, which fails on High/Critical and warns on Moderate;
   - `gitleaks/gitleaks-action@v2` (or `gitleaks detect --no-git` on the diff). Allow-list the documented dev placeholders in `appsettings.Development.json` (`local-dev-*`, demo passwords) with a `.gitleaks.toml`.
4. Do NOT fix warnings in this PR (no code edits). The only exception: if `Directory.Build.props` makes the build fail, fix the build config, not the code.

**Do not touch:** any `.cs`, `.razor`, `.css` or `.js` file; `pr-tests.yml`; `acceptatie-smoke.yml`; `App.razor`.

**Verify:**
- `dotnet build Jobsy.sln -c Release` succeeds with 0 errors. Report the warning count before and after in the PR description.
- `dotnet test Jobsy.Tests` (same filter as `pr-tests.yml` unit step) passes, with the same number of tests as before (2,383 without Playwright on 28-09).
- The new workflow runs green on the PR. Deliberately confirm locally that adding an unused private field increases the count and would fail.
- `git diff --stat` shows only new files, csproj property removals and the workflow.

**Pending-PR conflicts:** none (new files; csproj property lines only). If the SalesWalletChip/API-client PR changes `Jobsy.Web.csproj`, rebase and keep its package refs.
