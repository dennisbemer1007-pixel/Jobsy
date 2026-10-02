# Jobsy.Tests

xUnit suite for Lobsy/Jobsy: unit, integration (`WebApplicationFactory`), source/grep guards, UAT scripts, and Playwright.

## Quick commands

```bash
# Full suite (unit + integration; Playwright soft-skips without browser/URL as designed)
dotnet test Jobsy.Tests/Jobsy.Tests.csproj

# Release build like CI
dotnet test Jobsy.Tests/Jobsy.Tests.csproj -c Release

# Focused filters
dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter "FullyQualifiedName~CoreFunctionalFlow"
dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter "FullyQualifiedName~Authorization"
dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter "Suite=Uat999"
dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter "FullyQualifiedName~RoutesDocFreshness"
```

CI splits **unit/integration** from **Playwright smoke** (see `.github/workflows/pr-tests.yml`). Locally, install Chromium once:

```bash
pwsh Jobsy.Tests/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
# or after Release build: bin/Release/net10.0/playwright.ps1
```

Live Acc / CI stack: set `JOBSY_E2E_BASE_URL` (and optional candidate credentials). Without a URL, many Playwright tests soft-skip.

## Categories

| Category | Examples | When to use |
|----------|----------|-------------|
| **Unit / domain** | matching, tokens, commissions, privacy helpers | Pure logic, no HTTP |
| **API integration** | `WebApplicationFactory` + EF InMemory or TestServer | Authz, status codes, webhooks |
| **Source-grep / guard** | `AssetVersionGuardTests`, CSS/JS string asserts, `File.ReadAllText` on razor/js/css | Cheap regression on static contracts; ~400+ `File.ReadAllText` call sites across ~80 files |
| **bUnit** | component render tests (e.g. GratisDna) | Isolated Blazor component behaviour |
| **Playwright** | banenkaart, mobile smoke, onboarding, tabs | Real browser layout, navigation, circuits |
| **UAT 999** | `UatScenarioTests`, `UatRoleApiScriptsTests` | Per-role catalog / API happy+unhappy scripts |

## Source-grep tests vs bUnit / Playwright

Prefer **source-grep guards** when:

- The invariant is “this string / `?v=` / attribute must remain in file X”.
- You need a fast CI signal without hosting the app.

Prefer **bUnit** when:

- Component state, markup, or cascading parameters matter and DOM is enough.

Prefer **Playwright** when:

- Layout, MapLibre, Blazor circuit, mobile nav, or multi-page flows must actually work in a browser.

Do not delete a Playwright coverage of a flaky UX bug just because a grep test exists — they catch different failures.

## Coverage

```bash
dotnet test Jobsy.Tests/Jobsy.Tests.csproj \
  --collect:"XPlat Code Coverage"
# Results under TestResults/**/coverage.cobertura.xml (coverlet.collector is referenced)
```

There is no enforced coverage gate in CI today; use locally when changing critical auth/billing paths.

## Routes documentation guard

`RoutesDocFreshnessTests` fails if [`docs/ROUTES.md`](../docs/ROUTES.md) is out of date vs Blazor `@page` directives.

Regenerate:

```bash
JOBSY_UPDATE_ROUTES_DOC=1 dotnet test Jobsy.Tests/Jobsy.Tests.csproj \
  --filter "FullyQualifiedName~RoutesDocFreshness"
```

## Related

- Functional plan: [`../TESTING.md`](../TESTING.md)
- Roles: [`../docs/security/roles-matrix.md`](../docs/security/roles-matrix.md)
- Contributing: [`../CONTRIBUTING.md`](../CONTRIBUTING.md)
