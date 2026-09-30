# 01. Test accounts per role on acceptatie: guard, flags, seed, cleanup

Read `00-README.md` first (the role table, Decisions D1–D10, the Dependency checks and "How Dennis runs it").

> **Rules (same as the README, repeated on purpose):**
> - Branch `cursor/test-accounts` from `origin/acceptatie`. **Standalone** (not stacked). ONE PR into `acceptatie`; the body starts with `Standalone (not stacked)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/test-accounts`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report.
> - Never run the seed or cleanup command against acceptatie or production yourself. Tests use the test host only.
> - Never generate, print, log or commit a password. Passwords come only from Render secret env vars. Tests use obviously fake values in their own config.
> - Never change or delete a real (non-test) account, company or vacancy. The command refuses when anything looks like production.

| | |
|---|---|
| Branch | `cursor/test-accounts` |
| PR title | `feat(ops): acceptatie-only test accounts per role (CLI seed + cleanup, hard guard, IsTestAccount, 2FA exemption, test↔real boundary)` |
| Migration | `AddTestAccountFlags` |
| Split seam | if > ~1.500 lines: **01a** = guard + flags + migration + CLI seed/cleanup + MfaPolicy exemption + admin badge; **01b** = exclusions and the test↔real boundary (01.6) on `cursor/test-accounts-b`, stacked on 01a (PR body "Stacked on #<01a>") |

## Goal
Dennis can create a complete set of test logins on acceptatie with one command, test every role without 2FA friction, and remove everything with one command. None of it can ever run on production, touch real people or show up in their data.

## 01.1 Today (verify first; line numbers from `origin/acceptatie` a611db40)
- **Roles:** `Jobsy.Core/Enums/UserRole.cs` (8 roles, no Teacher; see the README role table).
- **2FA:**
  - `Jobsy.Core/Security/MfaPolicy.cs` `IsRequired(UserRole)`: Admin, BranchManager, RegionalManager, EnterpriseManager, Intermediary
  - callers: `Jobsy.Api/Controllers/AuthController.cs` L134 (local login: `user.AuthenticatorEnabled || MfaPolicy.IsRequired(user.Role)`) and L300 (`ensure-external`, `provider is null` path); `Jobsy.Web/Security/MfaEnforcementMiddleware.cs` L42
- **Environments** (`render.yaml`):
  - acceptatie services `lobsy-acc-api`/`lobsy-acc-web` run `ASPNETCORE_ENVIRONMENT=Production`, DB `lobsy-acc-db` (database name `lobsy`), `PublicWebBaseUrl=https://acceptatie.lobsy.nl`, `JobsyAuth__AllowStubPayments=true`, `Seed__Enabled=true`
  - production services `jobsy-api`/`jobsy-web`, DB `jobsy-db` (database name `jobsy`), `PublicWebBaseUrl=https://lobsy.nl`, stub payments `false`, seed `false`
  - Render sets `RENDER_SERVICE_NAME` itself (already read in `DatabaseSeedHostedService`)
- **Startup seed:** `Jobsy.Api/Jobs/DatabaseSeedHostedService.cs` → `JobsyDbSeeder.SeedDataAsync` → `DemoUsersSeeder` (`@jobsy.local`, password `Jobsy123!`, `EnsureUserAsync`/`EnsurePasswordAsync` using `JobsyPasswordHasher` + `LocalAuthCredential`). Reuse the password/credential helpers' **approach**, not the demo password. Don't change the demo seed (README D10).
- **CLI:** none exists. `Jobsy.Api/Program.cs` starts with `WebApplication.CreateBuilder(args)` (L14). The Docker `CMD` is `dotnet Jobsy.Api.dll --urls …` in `/app`.
- **Existing test-data exclusions:** only `AssessmentNormService` L68 (`!u.Email.Contains("jobsy.local") && !u.Email.Contains("demo")`). There is no general flag.
- **Mail:** `Jobsy.Core/Interfaces/IEmailService` → `SmtpEmailService` (Resend → SMTP → stub). Credentials can come from admin "Integraties" (`IntegrationCredential`), so acc may send real mail.
- **Admin:** `Components/Pages/Admin/UsersAdmin.razor`, `CompaniesAdmin.razor`, `VacanciesAdmin.razor`.
- **Tests** mostly use the EF InMemory provider (87 files); `PendingModelChangesTests` guards migrations.

## 01.2 The guard (`Jobsy.Core/Ops/TestAccountEnvironmentGuard.cs`)
- `static TestAccountGuardResult Evaluate(TestAccountGuardInput input)`: pure, unit-tested. It returns `Allowed` or a list of failed checks, each with a stable code and a message **without values**.
- **Input,** built by `TestAccountGuardInput.FromConfiguration(IConfiguration, IHostEnvironment)`:
  - `Lobsy:DeploymentEnvironment`
  - `TestAccounts:Enabled`
  - `RENDER_SERVICE_NAME`
  - the `PublicWebBaseUrl` host
  - the database name and host parsed from `ConnectionStrings:JobsyDb` with `NpgsqlConnectionStringBuilder` (URL form too, as Render gives it; reuse the existing parser if one exists)
  - `JobsyAuth:AllowStubPayments`
  - whether a Mollie API key from config **or** the `IntegrationCredential` store starts with `live_` (read the prefix only; never log the key)
  - `IHostEnvironment.EnvironmentName`
- **Allowed only if all of these hold:**
  1. `deployment_marker`: `Lobsy:DeploymentEnvironment` equals `Acceptatie` (ordinal ignore-case).
  2. `switch_off`: `TestAccounts:Enabled` is `true`.
  3. `service_name`: `RENDER_SERVICE_NAME` is present and starts with `lobsy-acc-`. `jobsy-api`, `jobsy-web` or anything else → refuse.
  4. `public_host`: the `PublicWebBaseUrl` host is in `TestAccounts:AllowedPublicHosts` (default `acceptatie.lobsy.nl`). `lobsy.nl`, `www.lobsy.nl` → always refuse, even if listed.
  5. `database`: the database name equals `TestAccounts:ExpectedDatabaseName` (default `lobsy`) and is never `jobsy`. Refuse when the connection string can't be parsed.
  6. `payments`: `JobsyAuth:AllowStubPayments` is `true` and no `live_` Mollie key is configured.
  7. `host_environment`: `EnvironmentName` is not `Development` (the command is acceptatie-only; local runs are refused). The unit tests call `Evaluate` directly with crafted input; the command tests use the same input type.
- **Two consumers:**
  - the CLI (01.4), which checks the full list; failure → exit 2
  - the runtime exemption `ITestAccountsRuntime.IsActive` (01.5), registered in **API and Web**, evaluated once at startup
    - the Web has `ConnectionStrings__JobsyDb` but no `PublicWebBaseUrl` or payment keys in `render.yaml`, so it checks 1, 2, 3, 5 and 7 plus its own `ApiBaseUrl` host (refuse when it contains `jobsy-api`, the production API service name)
    - log one line at startup: `Test accounts runtime: active` or `inactive (<codes>)`
- **Tests** (`TestAccountEnvironmentGuardTests`), one per check:
  - production-like input: `Lobsy:DeploymentEnvironment=Production`, `RENDER_SERVICE_NAME=jobsy-api`, `PublicWebBaseUrl=https://lobsy.nl`, database `jobsy`, stub payments `false`, a `live_` key → every check refuses, and so does each single deviation from a valid acceptatie input
  - a valid acceptatie input → allowed
  - missing values → refused
  - the messages never contain the input values (assert the fake connection string and the fake key don't appear)

## 01.3 Flags and migration `AddTestAccountFlags`
- `User.IsTestAccount` (bool, default false, index).
- `Company.IsTestData` and `Vacancy.IsTestData` (bool, default false, index). Schools and classes too when dependency S is present.
- **Nothing in the app may set these flags except the seed command:**
  - no DTO, API, import (CSV/ATS) or admin form binds them
  - add a reflection test over the API request models and Blazor forms: no property named `IsTestAccount`/`IsTestData`
  - a real user can never become a test account, and the flags can't be cleared through the UI
- The migration only adds the columns (default false): no data changes, and `PendingModelChangesTests` stays green.

## 01.4 The CLI (`Jobsy.Api/Ops/TestAccountsCommand.cs`)
- **Entry:**
  - at the very top of `Program.cs`: `if (args.Length > 0 && args[0] == "test-accounts") return await TestAccountsCommand.RunAsync(args[1..]);`
  - `RunAsync` builds a `Host.CreateApplicationBuilder`: the same configuration sources as the API (appsettings + env vars), `AddInfrastructure(...)` and the DbContext, logging to the console with **no** Sentry PII
  - **no** hosted services (don't register `DatabaseSeedHostedService`, index or scrape jobs), no Kestrel, no controllers
  - If `AddInfrastructure` registers hosted services, remove them from this host's service collection (`services.RemoveAll<IHostedService>()`) and test that no `IHostedService` resolves.
- **Verbs and options:**
  - `seed [--dry-run] [--only <AccountKey,...>]`
  - `cleanup [--execute --expect-users <n>]`
  - `status`: prints the guard result and the list of test accounts (e-mail, role, last login), no passwords
  - `--help`
  - unknown input → exit 1
- **Order in every verb:**
  1. Evaluate the guard; on refusal print `Refused: <codes>` and exit 2.
  2. For `seed` and `cleanup` only: check that the database is migrated (`GetPendingMigrationsAsync` is empty; if not, exit 4 with "Run the API once so migrations apply"). Never migrate from the command.
  3. Run inside one DB transaction; roll back on any exception (exit 4).
- **Output:** a plain table (account key, e-mail, role, action `created|updated|unchanged|skipped`, reason). Never a password, hash, token, pupil code or connection string. Log the same summary to `PlatformLog` (category `TestAccounts`, message `seed`/`cleanup` with counts and the actor `cli`).

## 01.5 Seed
- **Accounts:** the README role table. Account keys map to `TestAccounts:Password:<Key>` (env `TestAccounts__Password__<Key>`). E-mail `test-<slug>@<TestAccounts:EmailDomain>` (default `lobsy.nl`); slugs are fixed in `TestAccountCatalog` (Core, unit-tested): `kandidaat`, `kandidaat-nieuw`, `werkgever`, `bedrijfsmanager`, `regiomanager`, `intermediair`, `salesmanager`, `admin`, `ambassadeur`, and `leraar`/`schoolbeheerder` with dependency S.
  - A role not present in `UserRole` → skipped ("role does not exist in this build").
  - A missing password → skipped with a warning (exit 3 at the end if anything was skipped).
  - A password that fails `RegistrationPasswordRules` → skipped ("password for <Key> doesn't meet the password rules"; no value).
- **Per account** (idempotent; lookup by normalized e-mail):
  - **No user** → create with `IsTestAccount=true`, `IsActive=true`, `FullName` "Test <Rol>" (for example "Test Kandidaat"), and `LocalAuthCredential` hashed with `JobsyPasswordHasher`.
  - **A user with `IsTestAccount=true`** → update the role and data to the catalog. Rehash only when `JobsyPasswordHasher.Verify` fails, and bump `SessionVersion` in that case, so a rotated password logs old sessions out.
  - **A user with that e-mail and `IsTestAccount=false`** → **never touch it**: skip with "a real account uses this e-mail". Refuse the whole run when the `Admin` key hits this case.
  - Always: `AuthenticatorEnabled=false`, `AuthenticatorSecret=null`, `RecoveryCodesHash=null` (D8), no external logins, lockout fields cleared.
- **Sample data** (all flagged; deterministic names; created once, updated when changed):
  - **Test company tree:**
    - root `Testbedrijf Lobsy (test)` with type `Employer`, `IsTestData=true`
    - two vestigingen: `Testvestiging Den Haag (test)` and `Testvestiging Delft (test)`, with real coordinates in those cities
  - **Company details:**
    - KvK numbers that can't collide with real ones and aren't publicly routable: check the KvK validator and the public `/{kvknummer}` route. Prefer a value the route regex rejects; if the column only takes 8 digits, use a documented reserved range and add a guard that the KvK lookup services (`KvkHandelsregisterService`, `KvkVerificationRetryService`) never call out for `IsTestData` companies.
    - verification status `Verified`
    - no welcome-token side effects
  - **Memberships:**
    - `test-werkgever` = BranchManager of Den Haag
    - `test-regiomanager` = RegionalManager over both (use the existing region/`RegionCompany` model)
    - `test-bedrijfsmanager` = EnterpriseManager of the root
  - **Tokens:** a `TokenTransactionKind.Grant` of 25 to the root with reason `test-seed` (no revenue, no invoice).
  - **Vacancies:** 3 in the test company:
    - 2 published in Den Haag, 1 draft in Delft
    - real categories and valid salary data (use the existing rules: minimum wage, salary table)
    - `IsTestData=true`
    - titles end with " (test)"
  - **Intermediary:**
    - `Testbureau Lobsy (test)`, type `Intermediary` with `test-intermediair` as member
    - one client company or client vacancy as the intermediary model allows today (check `IntermediaryCompanyId` usage in `DemoUsersSeeder` and follow the same relation); flagged
  - **Candidates:**
    - `test-kandidaat`: completed onboarding (`CandidateOnboarding.FinishReached=true`, `CompletedAtUtc` set, current wizard version), home location Den Haag, preferences, `OpenForWork=true`, the free tests done with fixed answers through the existing services, so `MatchProfileCompleteness` says complete; one application to a test vacancy (status new) so the werkgever sees an applicant
    - `test-kandidaat-nieuw`: no onboarding row (or step 1), nothing else
  - **SalesManager:**
    - a `SalesManagerProfile` (active, with a tracking code prefixed `TEST-` if the code format allows it)
    - the test company referred by it (`ReferredBySalesManagerUserId`)
    - **no** commission ledger rows
  - **Ambassadeur:** an `AmbassadeurProfile` (active, onboarding done); no attributions or commissions.
  - **Admin:** the account only.
  - **Teacher** (dependency S present): the test school, one class `Testklas 1A` with the default number of pupil codes via the scholen generator (never printed), the teacher assignment, and the `SchoolAdmin` account for that school.
- **`--dry-run`** prints the same table with the planned actions and writes nothing (the transaction is rolled back; test it).
- After a real seed: invalidate the banenkaart index (`IVacancyDiscoveryIndex.Invalidate()`), as `DatabaseSeedHostedService` does. The running API refreshes within its 15 s job; say so in the output.

## 01.6 Keep test data away from real users (the test ↔ real boundary)
- **Central helpers:**
  - `Jobsy.Core/Ops/TestDataRules.cs`: `bool IsTestViewer(ClaimsPrincipal)` (a new claim `lobsy_test_account=1`, issued at login from `User.IsTestAccount`, both local and external) and `bool CanSeeTestData(principal)` (test viewer or Admin)
  - IQueryable extensions in Infrastructure: `.ExcludeTestUsers()`, `.ExcludeTestCompanies()`, `.ExcludeTestVacancies()`, and `.VisibleTo(principal)`, which keeps test rows only when `CanSeeTestData`
  - Use explicit filters, not EF global query filters (those would hide the rows from login, admin and cleanup).
- **Visibility** (real users never see test data):
  - banenkaart, search, the discovery index result (keep an `IsTestData` bit in the index entry and filter per request), vacancy detail `/vacancies/{id}` (404 for others), the public company pages `/{kvk}`, `/{kvk}/{vestiging}`, `/vestiging/{id}`
  - sitemap, JSON-LD/Google Jobs, feeds and exports (always excluded, also for admins)
  - talent pool (`TalentPoolService`): real employers never see test candidates; test employers see **only** test candidates
  - applicants, match (`ProfileVacancyMatchService`, `CandidateMatchSnapshotService`), `CandidateInsights*`, culture fit and role-fit queues: no test ↔ real pairs
- **Stats:**
  - platform/admin metrics (`MetricsQueryService`, `CachingMetricsQueryService`, `DashboardRefreshService`, `Sprint8`-style metric tables), finance (`TokenFinanceQueryService`), sales and ambassadeur dashboards: exclude test users, companies and vacancies. The same goes for `SiteVisit`, `VacancyClick`, `VacancySearchImpression`, `VacancyLike` and `VacancyShare` rows caused by test viewers: don't record them at all.
  - a test company's own dashboard shows its own test data
  - `AssessmentNormService` L68: add `!u.IsTestAccount` next to the existing email rule
- **Money:**
  - `TokenPurchaseInvoiceService.CreateForCheckoutAsync`, `VatDeclarationService`, `VatBufferTransferService`, `SelfBillingInvoiceService`, `CommissionLedgerService`, `RevenueShareService`, `SalesManagerPayoutService`, `AmbassadeurAttributionService` and `PartnerAffiliateService`: no rows for test users or companies
  - a stub checkout by a test company grants tokens as `Grant` with reason `test-purchase`, and the UI says "Testaccount: geen factuur"
- **Interactions:** refuse server-side with 403 `{ code: "test_account_boundary" }` when a test principal or test entity meets a real one:
  - apply, like, share tracking, talent contact/unlock, takeover requests, company membership/invites
  - sales/ambassadeur/partner referral codes (a test code used by a real registration counts as an invalid code; a real code isn't applied to a test company)
  - support access grants, feedback-to-agent (still allowed, but tagged `test`)
  - Find the entry points with `rg` on the services above and list them in the PR.
- **Mail and notifications:**
  - an `IEmailService` decorator `TestAccountMailGuard` in Infrastructure, with an `AsyncLocal` scope `TestAccountScope`
  - the scope is set by an API middleware when the principal is a test account, and by the seed/cleanup command
  - inside a test scope, a message to a recipient who isn't a test account is **dropped**; log `mail.dropped.test_boundary` with the redacted recipient (`EmailServiceStub.RedactEmail`) and the template key
  - mail to test addresses goes through normally (D5)
  - the same rule applies to `UserNotificationService` and `WebPushNotificationService`
  - background jobs that mail about entities (reminders, re-engagement: `ReengagementEmailSentAtUtc`, unverified reminders if present) skip `IsTestData` companies and test users entirely
- **Privacy/AI:** test accounts may use AI features on acceptatie as normal. Nothing about test accounts goes into norms or aggregates (above).

## 01.7 2FA exemption (D8)
- `MfaPolicy.IsRequiredFor(UserRole role, bool isTestAccount, bool testAccountsActive) => !(isTestAccount && testAccountsActive) && IsRequired(role)`. Keep `IsRequired` as is (docs/auth 02 changes it; see the README dependency AU).
- **API `AuthController`:**
  - L134: `if (!exempt && (user.AuthenticatorEnabled || MfaPolicy.IsRequired(user.Role)))`, where `exempt = user.IsTestAccount && runtime.IsActive`; exempt logins get `auth_method=password+test-exempt`
  - L300: the same
  - the login response carries `IsTestAccount` so the Web adds the claim `lobsy_test_account=1`
  - log `auth.login.test_exempt` with the user id
- **Web `MfaEnforcementMiddleware`:** skip when the principal has `lobsy_test_account=1` **and** the Web's `ITestAccountsRuntime.IsActive`. Otherwise unchanged.
- **Device-session refresh** (`DeviceSessionService.RotateAsync`): the rebuilt principal gets the claim from the DB flag.
- **Admin Google block:** if `AdminLoginProviderPolicy` (auth 02) exists, the test admin via Google is still refused (test). If it doesn't exist yet, nothing to do, but add a note in the PR.
- **Not exempt:** lockout, rate limits, the password rules, session version checks.
- **Tests** (`MfaPolicyTestAccountTests`, `TestAccountLoginTests`):
  - test admin + runtime active → no challenge
  - test admin + runtime inactive → challenge
  - a real admin with `IsTestAccount=false` → challenge
  - a real admin row with `IsTestAccount=true` but runtime inactive (the production case) → challenge
  - Web middleware with the claim but runtime inactive → redirect to 2FA as today
  - external login unchanged

## 01.8 Admin badge
- `UsersAdmin.razor` (or the admin-redesign table, dependency AR):
  - a chip "Testaccount" (token-based classes, a `pill`-style existing class; no inline styles) next to the name
  - a filter "Testaccounts: tonen / verbergen" (default: shown with the badge)
  - no toggle to change the flag
- `CompaniesAdmin.razor` and `VacanciesAdmin.razor`: the chip "Testdata" on flagged rows.
- The user detail page shows "Dit is een testaccount. Inloggen zonder 2FA werkt alleen op acceptatie." when flagged.
- Strings in `UiStrings` (`Admin.TestAccount`, `Admin.TestData`, `Admin.TestAccountFilter`, `Admin.TestAccountNote`) in 5 languages.
- Admin totals and KPIs exclude test data (01.6); lists show it with the badge.

## 01.9 Cleanup
- **`cleanup` (dry-run):**
  - collects the test users (`IsTestAccount`) and test companies/vacancies (`IsTestData`, plus schools/classes with S)
  - prints the accounts and a per-table row count of everything that would be deleted
  - ends with `To delete: dotnet Jobsy.Api.dll test-accounts cleanup --execute --expect-users <n>`
- **`--execute --expect-users <n>`:**
  - refuses (exit 1) when `n` differs from the current count
  - deletes in FK-safe order in one transaction: applications, likes, shares, clicks, impressions, talent contacts, onboarding, assessments and all candidate profile tables, CVs (and their stored files via the existing file store), notifications, push subscriptions, device sessions, credentials, external logins, token transactions, pending actions, memberships, sales/ambassadeur profiles, vacancies (+ translations), companies (children first), then the users
  - `PlatformLog` rows stay (they hold no PII)
- **Never delete:**
  - a user without `IsTestAccount`
  - a company that still has a non-test member (report it and exit 3)
  - rows of real entities that happen to reference test data (e.g. a real `PlatformFeedback` tagged `test`: detach instead of delete, and list it)
- **Coverage guard** (`TestDataCleanupCoverageTests`): walk the EF model (`db.Model.GetEntityTypes()`) for every FK pointing at `User`, `Company` or `Vacancy`. Each must be handled by the cleanup or listed in an explicit "kept" list with a reason. A new FK then fails the test until someone decides.
- Invalidate the banenkaart index after a real cleanup.
- Seed after cleanup works again (idempotency test).

## 01.10 render.yaml and docs
- **`render.yaml`:**
  - acc services `lobsy-acc-api`/`lobsy-acc-web`: `Lobsy__DeploymentEnvironment` value `Acceptatie`, `TestAccounts__Enabled` `sync: false` (Dennis sets `true`)
  - `lobsy-acc-api` only: the `TestAccounts__Password__<Key>` entries as `sync: false`
  - production `jobsy-api`/`jobsy-web`: `Lobsy__DeploymentEnvironment` value `Production` and `TestAccounts__Enabled` value `"false"`
  - Keep the file's comments style; don't touch other keys.
- **`docs/deploy-render.md`:** a section "Testaccounts (alleen acceptatie)" with the env vars and the run steps from the README "How Dennis runs it".
- **`SECURITY.md`:** the guard, the 2FA exemption (test accounts only, acceptatie only), the boundary and the mail guard.
- **`docs/adr/0005-mfa-local-only.md`:** a note that test accounts on acceptatie are exempt while the guard passes.
- **`TESTING.md`:** how to use the test accounts on acceptatie (no passwords in the doc).
- **Follow-ups:** `docs/testaccounts-followups.md` (Teacher/SchoolAdmin after scholen 01; D10 demo passwords; auth 02 `IsRequiredFor`).
- **`CHANGELOG.md`.**

## 01.11 Tests (all in the test host; never a real password)
- `TestAccountEnvironmentGuardTests` (01.2), with **production refusal** for every signal and for the full production profile.
- `TestAccountsCommandTests` (InMemory or the existing test DB setup, fake config with obviously fake passwords such as `fake-Password-For-Tests-1`):
  - guard refused → exit 2 and nothing written
  - `seed` creates the catalog; a second `seed` → all `unchanged`
  - a changed password → `updated` + `SessionVersion` bumped
  - a missing password → skipped + exit 3
  - a real user with a test e-mail is untouched
  - `--dry-run` writes nothing
  - an unknown role key is skipped
  - **no password, hash or connection string in the captured console/log output** (assert on the fake values)
- `TestAccountsNoHostedServicesTests`: the CLI host resolves no `IHostedService` and opens no port.
- `TestAccountSeedDataTests`:
  - each role has its sample data: werkgever sees one applicant; regiomanager sees 2 vestigingen; test kandidaat has a complete match profile; the new kandidaat has none
  - tokens are a `Grant`; no invoice, VAT, commission or ledger rows
- `TestAccountFlagBindingTests` (01.3 reflection).
- `TestDataVisibilityTests`: a real candidate/anonymous user can't see test vacancies (banenkaart query, detail 404, company page 404, sitemap, feeds); a real employer's talent pool and applicants have no test candidates; admin sees them with the flag.
- `TestAccountBoundaryTests`: test candidate → apply to a real vacancy → 403 `test_account_boundary`; a real candidate → a test vacancy → 404/403; referral codes; talent contact.
- `TestDataStatsExclusionTests`: metrics, finance, norms, and sales/ambassadeur dashboards unchanged by the seed.
- `TestAccountMailGuardTests`: inside a test scope, mail to a real recipient is dropped and logged redacted; mail to a test address is sent; notifications/push are the same.
- `MfaPolicyTestAccountTests` / `TestAccountLoginTests` (01.7).
- `AdminTestAccountBadgeTests`: the badge and filter render; no control changes the flag.
- `TestDataCleanupCoverageTests` + `TestAccountsCleanupTests`:
  - dry-run lists and writes nothing
  - `--execute` with the wrong count refuses
  - the right count deletes all test data and no real rows (seed some real rows first and assert they survive)
  - a company with a real member blocks
- Existing tests stay green: `MfaForcedEnrollmentTests`, `LoginProtectionTests`, `PendingModelChangesTests`, `AuthorizationMatrixReflectionTests`, `AssessmentNorm*`, and the invoice/VAT tests.

## Success criteria
- The command refuses on anything that isn't acceptatie, and the tests prove it for every production signal. On valid acceptatie config it seeds every existing role idempotently, with sample data, without ever printing or logging a password.
- Test accounts log in without 2FA only while the guard passes (API and Web); real accounts are unchanged; the admin Google block is unchanged.
- Real users never see, match with, get mail from, or get counted with test data; no invoice, VAT, commission or payout row exists for test data.
- Cleanup removes all test data and nothing else; the FK coverage test passes.
- The PR contains:
  - the dependency output (S/AU/AR)
  - the role table (seeded / skipped / missing: Teacher and SchoolAdmin until scholen 01)
  - the env vars for Render
  - the boundary entry-point list
  - the follow-ups
  - D3/D4/D10 flagged for Dennis

Done → report to Dennis.
