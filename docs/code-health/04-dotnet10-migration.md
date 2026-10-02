# 04: .NET 10 LTS migration (one coordinated step)

- **Deadline: merged into `acceptatie` and smoke-tested well before 10 Nov 2026.** On that date .NET 9 (STS) gets its last patch; it has been in security-only maintenance since May 2026. .NET 10 is LTS (10.0.12 on 8 Sep 2026, supported until 14 Nov 2028).
- **Branch:** `cursor/code-health-04-net10`, from `cursor/code-health-03-packages-minor`. If 01–03 are not merged by about 20 Oct, branch from `origin/acceptatie` directly and say so in the PR.
- **PR:** into its base.
- This step is one PR, because the 10.x Microsoft/EF/Npgsql packages require `net10.0`.

## Files to change

| File | Change |
|---|---|
| `Directory.Build.props` | `<TargetFramework>net10.0</TargetFramework>`. **Temporarily** pin `<AnalysisLevel>9.0-recommended</AnalysisLevel>` (instead of `latest-recommended`), so this PR only shows SDK/obsoletion warnings and not the new analyzer rules. Step 05 raises it to `10.0-recommended`. Add a comment that says so. |
| `tools/i18n-export/i18n-export.csproj`, `tools/generate-icons/GenerateIcons.csproj` | `net10.0` |
| `global.json` | `"version": "10.0.100"`, `"rollForward": "latestFeature"` |
| `Jobsy.Api/Dockerfile`, `Jobsy.Web/Dockerfile` | `mcr.microsoft.com/dotnet/sdk:10.0` and `aspnet:10.0`. Keep `ENV`, `EXPOSE` and `CMD` as they are. Note: `aspnet:10.0` images are Ubuntu-based ("noble") by default, not Debian. Check that `apt`/`apk` lines (if any) and the `sh -c` CMD still work, and that ICU/globalization is fine for nl/pl/ro/ar formatting. |
| `.github/workflows/pr-tests.yml`, `code-quality.yml`, `acceptatie-smoke.yml` | `dotnet-version: "10.0.x"`. Change `Jobsy.Tests/bin/Release/net9.0/playwright.ps1` to `net10.0`. **Needs `workflow` scope**, see global rule 7. |
| `Jobsy.Tests/README.md` | Playwright paths `net9.0` → `net10.0` |
| `.config/dotnet-tools.json` | `dotnet-ef` 9.0.0 → the 10.0.x version that matches EF |
| `Directory.Packages.props` | All `9.0.20` Microsoft.AspNetCore.* / Microsoft.EntityFrameworkCore.* / Microsoft.Extensions.* → the latest **10.0.x**. `Npgsql` → 10.0.x, `Npgsql.EntityFrameworkCore.PostgreSQL` (+ `.NetTopologySuite`) → 10.0.x. Microsoft.IdentityModel.* / System.IdentityModel.Tokens.Jwt: the latest 8.x that OpenIdConnect 10 needs. Update the comments in the file ("aligned 9.0.x patch" etc.). |
| `docs/deploy-render.md`, `ARCHITECTURE.md`, `README.md`, `CONTRIBUTING.md` | Mention .NET 10. |

**render.yaml / Render impact:**
- `render.yaml` uses `runtime: docker` with `dockerfilePath: ./Jobsy.Api/Dockerfile` / `./Jobsy.Web/Dockerfile`, so **no `render.yaml` change is needed**.
- The Blueprint syncs from `main` (see `docs/deploy-render.md`). The acceptatie services rebuild from their branch when Dennis merges, and production only changes when Dennis merges to `main`.
- **Do not touch Render settings.**
- Point out in the PR that the first deploy downloads new base images (a longer build), and that the DataProtection key ring stays compatible across 9 → 10.

## Things that commonly break (check each one and note the result in the PR)

1. **EF Core 10 / Npgsql 10:**
   - Run `dotnet ef migrations add Net10Probe --project Jobsy.Infrastructure --startup-project Jobsy.Api`. It **must produce an empty migration** except for the snapshot `ProductVersion` annotation.
   - If it is empty: delete the probe migration and keep the regenerated snapshot only if the `ProductVersion` change is the sole diff. `PendingModelChangesTests` must pass.
   - If it is not empty, analyse the diff (e.g. changed default mappings for `DateTime`/`timestamptz`, enums, JSON or PostGIS types). Fix it in the model configuration so the schema stays identical. **Never ship a migration that changes production schema just because of the upgrade**, unless it is explained and Dennis agrees (draft PR + question).
   - Run `MigrationsApplyOnFreshDatabaseTests`: all migrations on an empty Postgres 17 + PostGIS database, plus the duplicate-scan.
   - Also replay the migrations on a **copy of an older schema**: create a DB at migration `20260930130923_AddOneTimeLinks`, then apply the rest. Use `dotnet ef database update <name>` locally.
   - Watch for EF 10 query translation changes: run the full suite and look closely at the Sales, Applications and Discovery queries.
2. **ASP.NET Core 10:**
   - Cookie authentication now returns 401/403 for API endpoints instead of redirecting (known 10.0 change). Check `Jobsy.Web` endpoints that rely on login redirects, and the `LoginProtectionTests`.
   - Check `WithOpenApi`/Swashbuckle deprecation warnings (ASPDEPR*).
   - Check the Blazor static-SSR + interactive render modes, and the `blazor.web.js` reference in `App.razor` (bump `?v=`).
   - Check `UseStatusCodePages`/`UseHtmlStatusCodePages` and the error-page behaviour (errors stack tests).
3. **New SDK obsoletions** (`SYSLIB*`, `ASPDEPR*`, `CS0618` on framework APIs): fix them in this PR when they are mechanical. The **only allowed exception to "no new warnings"** is a warning caused purely by the TFM/package bump that needs a behaviour change. List each one in the PR with the step (05–07) that will fix it, and raise `warning-baseline.txt` by exactly that number.
4. **Globalization:** the date/number formatting tests for nl/en/pl/ro/ar (ICU on the new images). Run the email snapshot tests.
5. **Docker:**
   - `docker build -f Jobsy.Api/Dockerfile .` and `-f Jobsy.Web/Dockerfile .` locally.
   - Run the API container against local Postgres: `/health` returns 200 and the migrations apply.
   - Run the Web container: `/healthz` returns 200 and the login page renders.

## Verification

- Release build; the warning report includes a new/removed per-code delta.
- The full test suite plus all Playwright suites; compare with the base and Appendix A.
- Fresh-DB migration test, PendingModelChanges, the older-schema replay, and the API smoke test (health 200, demo login).
- Docker builds and container smoke tests for Api and Web.
- `dotnet list package --outdated`: the Microsoft/EF/Npgsql packages are at the latest 10.0.x.

## Acceptance

- Everything above is green, or a draft PR with the precise blocker.
- There is no schema-changing migration.
- The Dockerfiles, CI and global.json are consistent (`10.0`).
- The PR description has a "Render impact" paragraph and a rollback note: revert the PR, and the images go back to `:9.0`.
