Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 02: Central package management, AngleSharp security bump, redundant package references

**Goal:** one place for NuGet versions, remove a known vulnerability and remove 2 redundant references. Behaviour must stay the same.

**Evidence:**
- NU1902: `Jobsy.Infrastructure.csproj` references `AngleSharp 1.3.0`,, which has a known moderate vulnerability (see the NU1902 message for the advisory link).
- EF Core, ASP.NET and `Microsoft.Extensions.*` are pinned to `9.0.0` in 4 csproj files, while the SDK is 9.0.318.
- `Microsoft.AspNetCore.DataProtection.Abstractions` in `Jobsy.Infrastructure.csproj` is transitive via `Microsoft.AspNetCore.DataProtection` (certain).
- `System.IdentityModel.Tokens.Jwt` in `Jobsy.Web.csproj` has no direct usage in Jobsy.Web (`rg "System.IdentityModel|JwtSecurityToken" Jobsy.Web` finds 0 matches). It still comes in transitively via Jobsy.Core (likely safe; verify the build).

**Do:**
1. Add `Directory.Packages.props` with `ManagePackageVersionsCentrally=true`. Move every `Version=` from the 5 csproj files into it; the csproj files keep `<PackageReference Include=... />` without versions. Keep `PrivateAssets` and `IncludeAssets` as they are.
2. Bump AngleSharp to the first non-vulnerable version (check the advisory). Bump all `Microsoft.*` / `System.*` 9.0.0 packages to the **same latest 9.0.x patch** (no major upgrades). Keep Npgsql and EF Core aligned (both 9.0.x).
3. Remove `Microsoft.AspNetCore.DataProtection.Abstractions` (Infrastructure) and `System.IdentityModel.Tokens.Jwt` (Web) if the build and tests stay green. If Web needs the JWT package after all, keep it and note why in the PR.
4. Add `<NuGetAudit>true</NuGetAudit>` and `<NuGetAuditLevel>moderate</NuGetAuditLevel>` (in `Directory.Build.props` if prompt 01 is merged, otherwise in `Directory.Packages.props`).

**Do not touch:** application code; `.github/dependabot.yml` (it works once it reaches `main`); `tools/generate-icons` (separate tool; only include it if it builds with CPM, otherwise opt it out with `ManagePackageVersionsCentrally=false`).

**Verify:**
- `dotnet restore && dotnet build Jobsy.sln -c Release`: 0 errors and **no NU1902**.
- `dotnet test` (unit filter) passes with the same count.
- The CI Playwright smoke (`pr-tests.yml`) is green.
- `dotnet list package --vulnerable --include-transitive` shows no vulnerable packages.
- Start API + Web locally (`.github/scripts/start-ci-stack.sh`), then check login and `/` banenkaart.

**Dependency:** **wait until the SalesWalletChip/API-client PR (JobsyApiClientFactory, retry handler) is merged**, because it may add `Microsoft.Extensions.Http` or other packages to `Jobsy.Web.csproj`. It can come after or together with prompt 01.
