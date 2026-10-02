# Code health: warnings, packages, .NET 10 (Cursor spec)

> Paste a pointer prompt into Cursor, e.g. *"Read `docs/code-health/00-README.md` and implement step 01."*
> Repo: `dennisbemer1007-pixel/Jobsy`.
> Reference data: [`warnings-analysis.md`](warnings-analysis.md). It was measured on `acceptatie` @ `3a15b0d7` on 2 Oct 2026 and has 470 unique warnings (411 as the current CI script counts them), the NuGet/npm/vendored-library status and the .NET support dates.

## Dennis' decision (2 Oct 2026)

- **Warnings fail the build.** `TreatWarningsAsErrors=true` for every project. Exception: `NU1901;NU1902;NU1903;NU1904` (NuGet audit) stay warnings, and the separate "Vulnerable package gate" in CI keeps failing on High/Critical.
- **Warnings are fixed straight away, in the same PR/build, so work doesn't stall:**
  - **CI auto-fix job** (built in step 11).
    - On PR branches only, it runs `dotnet format` (whitespace + style + analyzers, **safe fixers only**, i.e. an allow-list of diagnostic IDs) and pushes one fix commit to the same PR branch. The build then reruns.
    - It never runs on `main` or `acceptatie`, never on fork PRs, and never loops: it skips when the last commit was made by the bot, or carries the `[code-health-autofix]` marker.
  - **Standing rule for every future Cursor prompt** (see below): deliver 0 warnings in Release before opening a PR.
- **The CI warning counter must include `RZ`, `ASP` and `BL` codes** (Razor/Blazor/ASP.NET analyzers). These hid a real rendering bug (RZ10012).

## Global rules (apply to every step)

1. **Never merge, never deploy.** Do not trigger Render deploys, do not change Render settings or secrets, and do not use rule/shortcut **123**.
2. **Never push to `main` or `acceptatie`.** No force-push, no history rewrites on pushed branches. Push only the step's own branch (`cursor/code-health-NN-<slug>`).
3. **One PR per step** (step 08 is one PR per package). The base is `acceptatie` for 01 (standalone); for 02 onwards it is the previous step's branch (stacked). When the previous step has been merged into `acceptatie` by Dennis, branch from and target `acceptatie` instead, and say so in the PR description.
4. **Every step, before opening the PR:**
   - `dotnet build Jobsy.sln -c Release --no-incremental` (warnings are counted from this log, see "Counting warnings")
   - the full test suite: `dotnet test Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build` against local Postgres 17 + PostGIS, with the same env vars as `.github/workflows/pr-tests.yml`. Run the Playwright suites too when the step touches UI, maps, auth or the test stack (install browsers with `playwright.ps1 install --with-deps chromium`).
   - the **fresh-DB migration guard** `MigrationsApplyOnFreshDatabaseTests` and `PendingModelChangesTests`, always
   - the **API smoke test**: start `Jobsy.Api` against an empty database and check that all migrations apply, `GET /health` returns 200 and a demo login works
5. **No new warnings.** The unique warning count (counter including RZ/ASP/BL) must be **≤ the base branch count**, and every file you touch must be warning-free. Steps 02–07 must lower the count. The only exception is step 04, see its file.
6. **Red tests mean stop.** If anything fails that does not fail on the base branch (compare against Appendix A in [11](11-gate-warnings-as-errors.md)), or the build fails, open the PR as a **draft**, describe the failure and stop. Don't "fix" tests by weakening their assertions unless the step file says so.
7. **Pushes that touch `.github/workflows/*`** need a token with `workflow` scope. If the push is rejected for that reason, do not work around it. Put the workflow diff in the PR description as a fenced patch, mark the PR draft and report it.
8. Commit as Dennis: `git -c user.name="Dennis Bemer" -c user.email="dennisbemer1007@users.noreply.github.com" commit …`. Make small commits, one per logical item, so each can be reverted on its own.
9. Don't touch real accounts, production data, `render.yaml` secrets or `TestAccounts__*` / `Lobsy__DeploymentEnvironment`.
10. Bump cache-busting `?v=` query strings (`App.razor`) for any CSS/JS you change, and regenerate `app.min.css` with `tools/css/build.sh` when `app.css` changes.

## Standing rule for all future Cursor prompts (not only this spec)

Every Cursor prompt or stack must deliver a **Release build with 0 warnings** before opening a PR. Until step 11 has landed, the rule is **0 new warnings** compared with the base branch, and the files you touch must be warning-free.
- Run `dotnet format` (whitespace + style + analyzers) locally first.
- Fix what's left by hand. Suppressing a warning is only allowed via `.editorconfig` or an inline `#pragma warning disable <ID> // reason`, and only with a written reason.
- Never use a blanket `<NoWarn>`.

Add this rule to `CONTRIBUTING.md` (step 02) and to the template that new prompts are made from (e.g. `docs/prompts/`), so it is copied into every prompt.

## Counting warnings

```bash
dotnet build Jobsy.sln -c Release --no-incremental -v minimal 2>&1 | tee /tmp/build.log
.github/scripts/count-build-warnings.sh /tmp/build.log    # after step 02 this counts CS|CA|IDE|SYSLIB|RZ|ASP|BL|NU
```

Until step 02 lands, also report the full count. Key = `path(line,col):CODE` over `: warning [A-Z]+[0-9]+:`. The baseline full count on `3a15b0d7` is 470.

## Steps (stacked PRs into `acceptatie`)

| # | File | Branch | Base | Summary |
|---|---|---|---|---|
| 01 | [01-hotfix-real-bugs.md](01-hotfix-real-bugs.md) | `cursor/code-health-01-hotfix` | `acceptatie` (standalone) | RZ10012 components rendered as raw HTML, lockout email, /melden reason, payout `CreditorName`, `stackalloc` in loop, OSM attribution, npm `image-size` |
| 02 | [02-config-suppressions-and-ci-counter.md](02-config-suppressions-and-ci-counter.md) | `cursor/code-health-02-config` | 01 or `acceptatie` | Justified `.editorconfig` suppressions, CI counter incl. RZ/ASP/BL, baseline reset, CONTRIBUTING rule, restore PublicPages Playwright lines in `pr-tests.yml` |
| 03 | [03-safe-package-bumps.md](03-safe-package-bumps.md) | `cursor/code-health-03-packages-minor` | 02 | Same-major NuGet bumps; Identity.Web 3.15.x (removes deprecated Azure.Identity/MSAL) |
| 04 | [04-dotnet10-migration.md](04-dotnet10-migration.md) | `cursor/code-health-04-net10` | 03 | **net10.0 LTS + all 10.x Microsoft/EF/Npgsql packages. Deadline: before 10 Nov 2026** |
| 05 | [05-warnings-behaviour-fixes.md](05-warnings-behaviour-fixes.md) | `cursor/code-health-05-behaviour` | 04 | Bug-adjacent warnings + switch analyzers to the 10.0 level |
| 06 | [06-warnings-unused-code-signatures.md](06-warnings-unused-code-signatures.md) | `cursor/code-health-06-unused` | 05 | IDE0060/0052/0051/0059 |
| 07 | [07-warnings-obsolete-and-mechanical.md](07-warnings-obsolete-and-mechanical.md) | `cursor/code-health-07-obsolete-mechanical` | 06 | CS0618 + migration, CA1068 order, mechanical rest |
| 08 | [08-major-package-bumps.md](08-major-package-bumps.md) | `cursor/code-health-08a-sentry6`, `-08b-openapi`, `-08c-identityweb4`, `-08d-imagesharp4`, optional `-08e-questpdf2026` | 07 (each on the previous) | One major per sub-PR; ImageSharp and QuestPDF only after Dennis' licence OK |
| 09 | [09-test-stack.md](09-test-stack.md) | `cursor/code-health-09-test-stack` | 08 (last sub-PR) | xunit.v3, runner, bunit 2 |
| 10 | [10-frontend-libs.md](10-frontend-libs.md) | `cursor/code-health-10-frontend` | 09 | MapLibre 6, html2canvas → maintained fork |
| 11 | [11-gate-warnings-as-errors.md](11-gate-warnings-as-errors.md) | `cursor/code-health-11-gate` | 10 | TreatWarningsAsErrors, pin AnalysisLevel, delete the baseline, auto-fix job, fix the 49 pre-existing failing tests (Appendix A) |

**Time pressure:** 04 has a hard deadline (.NET 9 support ends **10 Nov 2026**). If 01–03 are not merged by about 20 Oct, branch 04 directly from `acceptatie` and report that.

## Report per PR (put it in the PR description and in your final message)

```
Step NN — <title>
Branch / base / PR: …
What changed: bullet list (one line per commit)
Files touched: list (group by project)
Warnings: before <n_full> (<n_ci>) → after <n_full> (<n_ci>); per-code delta for codes that changed
Tests: <passed>/<total>; failures: list, each marked "pre-existing (Appendix A)" or "NEW"
Fresh-DB migration test: pass/fail; PendingModelChanges: pass/fail; API smoke: health/login
Playwright (if run): which suites, result
Open points / questions for Dennis
```

## Open questions for Dennis (answer before the step that needs them)

1. **ImageSharp 4 licence (08d)** and **QuestPDF 2026 licence (08e):** both have revenue thresholds for free use. Does Lobsy qualify, or is a commercial licence needed? Until there's an answer, 08d/08e stop at an analysis-only draft PR.
2. **Auto-fix bot token (11):** create a GitHub App or a fine-grained PAT (contents: write, this repo only) as the secret `CODE_HEALTH_BOT_TOKEN`. Without it, bot commits don't retrigger CI and the author must re-run the checks by hand.
3. **`workflow` scope:** steps 02, 04 and 11 change `.github/workflows/*`. The pushing agent or token needs `workflow` scope; otherwise those PRs arrive as drafts with a patch.
4. **OpenAPI (08b):** switch to the built-in `Microsoft.AspNetCore.OpenApi` (preferred) or stay on Swashbuckle 10?
5. **Timing of 09 (test stack):** pick a moment with few open feature stacks.
6. **OSM attribution styling (01 §6):** a compact "i" toggle on mobile is acceptable; on desktop the attribution is visible. OK?
7. **Ambiguous pre-existing tests (11, Appendix A, "Ask Dennis"):** MFA enrolment redirect, Ambassadeur MFA, and BranchManager 403s on the werkgever APIs. Which side (test or code) reflects the intended behaviour?
8. **Dropping obsolete DB columns (07):** OK to drop `ExposeRegistrationActivationLinks` and `DeepAnalysisPriceEuro` once the code no longer uses them?
