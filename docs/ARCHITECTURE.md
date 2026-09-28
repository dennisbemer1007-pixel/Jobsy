# Architecture — Lobsy / Jobsy

User-facing product name: **Lobsy**. Code, DB, packages, and repo: **Jobsy** ([ADR 0001](adr/0001-keep-jobsy-code-name.md)).

## Projects and dependencies

```
Jobsy.Core          entities, enums, interfaces, JobsyRoles / JobsyPolicies
       ↑
Jobsy.Infrastructure   EF Core (JobsyDbContext), seeders, external clients, background jobs
       ↑
Jobsy.Api           ASP.NET Core controllers + JWT / API-key auth

Jobsy.Web  → Jobsy.Core only (project reference)
           → talks to Jobsy.Api over HTTP via JobsyApiClient (+ cookie session on Web)
```

| Project | Responsibility |
|---------|----------------|
| `Jobsy.Core` | Domain model, authorization constants, contracts/interfaces — no EF, no HTTP |
| `Jobsy.Infrastructure` | Persistence, Mollie/KVK/OpenAI/mail/push, `CompanyAuthorizationService`, hosted workers |
| `Jobsy.Api` | Thin HTTP surface; policies; rate limits; Swagger (non-prod) |
| `Jobsy.Web` | Blazor Server UI, cookie auth, MFA UX, MapLibre banenkaart |
| `Jobsy.Tests` | xUnit + WebApplicationFactory + Playwright + source guards |

New business logic belongs in **services**, not fat controllers ([ADR 0002](adr/0002-thin-controllers.md)). DTOs are currently mirrored between Api and Web ([ADR 0003](adr/0003-api-contracts.md)).

## Auth flow

```mermaid
sequenceDiagram
    participant Browser
    participant Web as Jobsy.Web
    participant Api as Jobsy.Api
    participant Db as PostgreSQL

    Browser->>Web: Login (local / Entra / Google)
    Web->>Api: Credential / external provision
    Api->>Db: Validate user + roles
    alt MFA required
        Api-->>Web: MfaChallengeToken
        Web->>Browser: /account/mfa
        Browser->>Web: MFA code
        Web->>Api: MFA verify
    end
    Api-->>Web: Access profile + JWT claims material
    Web->>Browser: Auth cookie (Jobsy cookie scheme)
    Browser->>Web: Page / interactive circuit
    Web->>Api: JobsyApiClient + Bearer JWT (and internal headers)
    Api-->>Web: JSON (scoped by role + company)
```

- **Web:** cookie authentication (`CookieAuthenticationDefaults`), optional demo-login only in Development, MFA cookies `Jobsy.MfaChallenge` / `Jobsy.MfaReturnUrl`, enforcement middleware.
- **API:** JWT Bearer (`JobsyJwtScheme`), API-key policy for selected integrations, FallbackPolicy deny-by-default on API.
- **Roles:** claim values from `JobsyRoles` / `UserRole` ([ADR 0004](adr/0004-roles-and-scope.md)).

## Authorization

- Policy names: `JobsyPolicies` (`RequireAdmin`, `RequireEmployer`, `RequireCandidate`, …) registered in `Jobsy.Api/Authorization/AuthorizationExtensions.cs`.
- Role helpers: `JobsyRoles` — notably `EmployerMutateRoles` **excludes** `RegionalManager` (read-only employer UI/API).
- Company / branch / org scope: `CompanyAuthorizationService` (`GetAccessibleCompanyIdsAsync`, `CanAccessCompanyAsync`). Scope is membership-driven today; see [`docs/security/roles-matrix.md`](security/roles-matrix.md).
- Blazor pages use `@attribute [Authorize(Roles=…)]` / `[AllowAnonymous]`; Web has **no** FallbackPolicy — unannotated pages are reachable (document carefully when adding pages).

## Static assets and bundles

- Versioned assets under `Jobsy.Web/wwwroot` use `?v=` query tags (immutable long-cache).
- Guard: `Jobsy.Tests/AssetVersionGuardTests` + manifest `Jobsy.Tests/asset-versions.json`.
- After changing a versioned file: bump `?v=` in `App.razor` / loaders (`maps-loader.js`, `app-core.js`, …), then `python3 Jobsy.Tests/update-asset-versions.py`.
- CSS: editable source `wwwroot/css/app.css` (and `css/features/*`); served `app.min.css` is a hand-maintained mirror **until** cleanup prompt 10 lands a real minifier — do not edit `app.min.css` alone out of sync (see `CONTRIBUTING.md`).
- Map JS: `jobMap.js` → served `jobMap.min.js`; MapLibre CSP build under `wwwroot/lib/maplibre/`.

## Background jobs

Hosted services (API / Infrastructure) include (non-exhaustive):

- `DatabaseSeedHostedService` — migrate + seed off the request thread
- `DataRetentionHostedService` — AVG retention
- `TokenCheckoutReconcileHostedService`, `VatBufferTransferHostedService`
- `AtsScrapeHostedService`, `AtsVacancyHealthHostedService`
- `VacancyDiscoveryIndexHostedService`, `DashboardCacheRefreshHostedService`
- `CultureFitRefineWorker`, `CandidateInsightsWorker` (OpenAI-backed refine/insights)
- `VacancyEngagementReminderHostedService`, `CompanyReengagementHostedService`
- `DraftVacancyCleanupHostedService`, `UnconfirmedRegistrationCleanupHostedService`
- `TalentContactRefundHostedService`, `KvkVerificationRetryHostedService`
- `FeedbackAutomationPollHostedService`, `IbanEncryptionMigrationHostedService`
- `MinimumWageUpdateHostedService`, `WebPushVapidKeyProvider`

## OpenAI, push, payments

| Concern | Where |
|---------|--------|
| OpenAI | Named HttpClients + integration credentials (`IntegrationKey.OpenAI`); moderation, WhoAmI, assistant, culture-fit, deep analysis |
| Web Push | `WebPush` package + VAPID keys (`WebPush__*`); subscription endpoints under `api/push` |
| Payments | Mollie (live keys outside Development); stub payments only when `JobsyAuth:AllowStubPayments` |

## Diagram (solution)

```mermaid
flowchart TB
    subgraph clients [Clients]
        Browser
    end

    subgraph web [Jobsy.Web]
        Blazor[Blazor Server]
        ApiClient[JobsyApiClient]
        Map[MapLibre / jobMap]
    end

    subgraph api [Jobsy.Api]
        Controllers
        Policies[Auth policies]
        Jobs[Hosted services]
    end

    subgraph infra [Jobsy.Infrastructure]
        EF[JobsyDbContext]
        Scope[CompanyAuthorizationService]
        Ext[Mollie / KVK / OpenAI / Mail / Push]
    end

    subgraph core [Jobsy.Core]
        Domain[Entities / Roles / Interfaces]
    end

    DB[(PostgreSQL + PostGIS)]

    Browser --> Blazor
    Blazor --> Map
    Blazor --> ApiClient
    ApiClient --> Controllers
    Controllers --> Policies
    Controllers --> EF
    Controllers --> Scope
    Jobs --> EF
    Jobs --> Ext
    EF --> DB
    Ext --> DB
    infra --> Domain
    api --> Domain
    web --> Domain
```

## Related docs

- Onboarding / secrets: [`ONBOARDING.md`](ONBOARDING.md)
- Routes catalog: [`ROUTES.md`](ROUTES.md)
- Roles: [`security/roles-matrix.md`](security/roles-matrix.md), [`../ROLES_AND_VIEWS.md`](../ROLES_AND_VIEWS.md)
- Security / AVG: [`../SECURITY.md`](../SECURITY.md)
- Deploy: [`deploy-render.md`](deploy-render.md), [`release-flow.md`](release-flow.md)
