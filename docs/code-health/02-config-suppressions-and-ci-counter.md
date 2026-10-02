# 02: Config: justified suppressions, CI counter incl. RZ/ASP/BL, baseline reset

- **Branch:** `cursor/code-health-02-config`, from `cursor/code-health-01-hotfix`, or from `origin/acceptatie` if 01 is already merged.
- **PR:** into the base above.
- **No production code changes in this step.** It only touches `.editorconfig`, CI scripts/workflows, the baseline and docs.

## §1 `.editorconfig`: suppressions, each with a written reason

Add the blocks below to the existing `.editorconfig`, after the existing `CA1848/CA1305…` block. Keep a comment line per rule: **no suppression without a reason.**

```ini
[*.cs]
# CA1862: every hit (61) is inside an EF Core IQueryable predicate (email / tracking-code lookups,
# ATS + assistant search). EF translates ToLower()/ToUpper() to SQL lower()/upper(), but it cannot
# translate string.Equals(..., StringComparison), so the suggested fix throws at runtime.
# Follow-up: normalised/citext columns (see warnings-analysis.md §3).
dotnet_diagnostic.CA1862.severity = none
# CA1859: internal/private API shape. Read-only interfaces express intent; the perf gain is negligible here.
dotnet_diagnostic.CA1859.severity = suggestion
# CA1863: format strings are localised at runtime (UiStrings/EmailStrings), so CompositeFormat caching does not apply.
dotnet_diagnostic.CA1863.severity = suggestion
# CA1716/CA1711/CA1720/CA1000: VB/C++ interop and library-design naming rules. We ship no public library.
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
# ASP0006: bUnit RenderTreeBuilder helpers in tests use seq++. Rendering diff stability is not under test here.
dotnet_diagnostic.ASP0006.severity = none
```

Also mirror the `[**/Jobsy.Tests/**/*.cs]` glob, the way the existing CA1707 rule does.

Before suppressing CA1862 globally, **check again that all its hits are EF predicates.** Grep each reported line: it must sit inside a `.Where/.Any/.First…` on a `DbSet`/`IQueryable`. If any hit is in-memory LINQ, fix that one with `StringComparison.OrdinalIgnoreCase` and keep a scoped rule instead (`[Jobsy.Infrastructure/**.cs]` + `[Jobsy.Api/**.cs]`).

## §2 CI counter includes RZ / ASP / BL (and NU)

- `.github/scripts/count-build-warnings.sh`:
  - Change the code group to `(?:CS|CA|IDE|SYSLIB|RZ|ASP|BL)\d+`.
  - Make the path match `.razor` and `.cshtml` as well as `.cs`/`.csproj`.
  - Normalise absolute paths generically: strip everything up to the repo root (`$GITHUB_WORKSPACE`/`git rev-parse --show-toplevel`), not only `/workspace/`.
  - Keep the `NU` package key.
- Add a tiny self-test, `.github/scripts/count-build-warnings.selftest.sh`. It runs the counter on a fixture log that contains one each of CS, CA, IDE, RZ, ASP, BL and NU, plus a duplicate, and expects **7**. Run it in `code-quality.yml` before the build.
- Also print a per-code summary (`code count`, sorted) in the job log, so PR authors see what to fix.

## §3 Baseline reset

- After §1 and §2, build with `dotnet build Jobsy.sln -c Release --no-incremental` and set `.github/quality/warning-baseline.txt` to the **new real count**, counted with the new counter (expected ≈ 470 − 35 RZ10012 already fixed in 01 − ≈226 suppressed ≈ **200–210**). Put the per-code table in the PR description.
- `code-quality.yml` keeps failing when the count rises above the baseline. Additionally: if the count drops below the baseline, **fail with "please lower warning-baseline.txt to N"**, so the baseline is always exact and every PR that fixes warnings lowers it. That replaces the current `::notice::`.

## §4 Restore the PublicPages Playwright lines in `pr-tests.yml`

These were lost when the errors stack was merged without `workflow` scope:
- Re-add `&FullyQualifiedName!~PublicPagesPlaywrightTests` to the **unit** test filter.
- Re-add `|FullyQualifiedName~PublicPagesPlaywrightTests` to the **smoke/Playwright** filter.
- Re-add `artifacts/playwright-public/**` to the artifact upload list.

Compare with `origin/cursor/public-pages-10:.github/workflows/pr-tests.yml` to get the exact lines.

## §5 Standing rule in CONTRIBUTING

Add a "Warnings" section to `CONTRIBUTING.md` with the standing rule from [00-README](00-README.md) (0 new warnings now, 0 warnings once step 11 is in). Include the local commands:
- `dotnet format whitespace|style|analyzers --verify-no-changes`
- the counter script

Add the same three lines to the top of every new prompt under `docs/prompts/` (create `docs/prompts/_TEMPLATE.md` if there is none).

## Acceptance

- `code-quality` workflow: the self-test passes, and the count equals the new baseline exactly.
- Same test results as 01 (no new failures); fresh-DB migration and PendingModelChanges pass.
- The PR description has the warning table before and after per code, plus the justification list (copy the comments above).
- Workflow push rejected for lack of `workflow` scope → follow global rule 7 (draft + patch in the description).
