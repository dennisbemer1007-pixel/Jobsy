# Lobsy test accounts on acceptatie: seed + cleanup command (Cursor run book)

Cursor: **read this file completely**, then execute `01-seed-cleanup-testaccounts.md`. It is **one file = one PR**, standalone.

> **Rules (repeated in every file):**
> - Branch `cursor/test-accounts` from `origin/acceptatie`. **Standalone** (not stacked). ONE PR into `acceptatie`; the body starts with `Standalone (not stacked)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/test-accounts`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report.
> - Never run the seed or cleanup command against acceptatie or production yourself. Dennis runs it (see "How Dennis runs it"). Tests use the test host only.
> - Never generate, print, log or commit a password. Passwords come only from Render secret env vars. Never put a real password in tests (use obviously fake values in the test config only).
> - Never change or delete a real (non-test) account, company or vacancy. The command refuses when anything looks like production.

## What this builds
- One CLI command inside the API binary: `dotnet Jobsy.Api.dll test-accounts <seed|cleanup> [options]`. **No HTTP endpoint** and no admin button.
- It runs **only on acceptatie**. A hard guard refuses unless:
  - the deployment marker says `Acceptatie`
  - `TestAccounts:Enabled=true`
  - nothing looks like production (host, database, service name, payments)
- **seed** creates one test account per role that exists in code, plus a minimal sample data set so every role has something to test. It is idempotent.
- **cleanup** lists everything that belongs to test accounts (dry-run by default), and deletes it only with `--execute` and a matching count.
- Test data is marked, shown with a **"Testaccount"** badge in admin, and kept away from real users: no matching, stats, invoices or mails, and never visible to real users.
- **Only** test accounts skip 2FA (a `MfaPolicy` exemption that only works while the guard passes). The admin Google block (docs/auth 02) still applies to the test admin.

## How to run (Cursor)
1. `git fetch origin`. Read this file, then `01-seed-cleanup-testaccounts.md`, `SECURITY.md`, `docs/adr/0005-mfa-local-only.md`, `docs/deploy-render.md`, `docs/release-flow.md` and `ROLES_AND_VIEWS.md`.
2. Run the **Dependency checks** below and put the output in the PR body.
3. `git checkout -b cursor/test-accounts origin/acceptatie`.
4. Implement file 01. Run `dotnet build` and `dotnet test`; everything must be green and the success criteria must hold.
5. Small, clear commits. `git push -u origin cursor/test-accounts`. Open ONE PR into `acceptatie` with the title from file 01. The body contains:
   - `Standalone (not stacked)`
   - the dependency cases
   - the role table
   - the migration name
   - the env vars Dennis must set
   - the test list
   - anything deferred
6. **Stop and report** when tests fail and you can't fix them inside the file's scope, or when the code contradicts this spec in a way you can't resolve safely. Push, open the PR as **draft** with the failure described, and stop.
7. If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you, `git merge origin/acceptatie` (a normal merge commit) and say so in the PR.

## Roles (checked in code on `origin/acceptatie` `a611db40`)
`Jobsy.Core/Enums/UserRole.cs`: `Candidate`, `BranchManager`, `RegionalManager`, `EnterpriseManager`, `Intermediary`, `Admin`, `SalesManager`, `Ambassadeur`.

| Asked for | Role in code | Account key | E-mail (default domain) | Status |
|---|---|---|---|---|
| kandidaat (onboarding done) | `Candidate` | `Candidate` | `test-kandidaat@lobsy.nl` | exists |
| kandidaat (onboarding not done) | `Candidate` | `CandidateNew` | `test-kandidaat-nieuw@lobsy.nl` | exists |
| werkgever | `BranchManager` (UI "Filiaalmanager", one vestiging) | `BranchManager` | `test-werkgever@lobsy.nl` | exists under another name: there is **no** role "werkgever"; the employer role for one location is `BranchManager` |
| bedrijfsmanager | `EnterpriseManager` (UI "Bedrijfsmanager") | `EnterpriseManager` | `test-bedrijfsmanager@lobsy.nl` | exists |
| regiomanager | `RegionalManager` | `RegionalManager` | `test-regiomanager@lobsy.nl` | exists |
| intermediair | `Intermediary` | `Intermediary` | `test-intermediair@lobsy.nl` | exists |
| leraar (with one class) | — | `Teacher` | `test-leraar@lobsy.nl` | **missing**: no `Teacher` role, no school or class entities. Planned in `docs/scholen` 01 (`Teacher`, `SchoolAdmin`, `School`, `SchoolClass`, `TeacherClassAssignment`). Dependency S below |
| salesmanager | `SalesManager` | `SalesManager` | `test-salesmanager@lobsy.nl` | exists |
| admin | `Admin` | `Admin` | `test-admin@lobsy.nl` | exists |
| (not asked) | `Ambassadeur` | `Ambassadeur` | `test-ambassadeur@lobsy.nl` | exists; **included by default** (D3) |
| (not asked, planned) | `SchoolAdmin` | `SchoolAdmin` | `test-schoolbeheerder@lobsy.nl` | only with dependency S present (D3) |

- A role that isn't in `UserRole` at run time is **skipped with a warning** ("role Teacher does not exist in this build; skipped").
- The command prints the role table as seeded/updated/skipped, with no passwords.

## Decisions (defaults; Dennis can change them)
- **D1. CLI, not an endpoint.** Pattern: a `test-accounts` verb handled at the top of `Jobsy.Api/Program.cs` **before** `WebApplication.CreateBuilder(args)` builds the web app. It builds a plain `Host` with `AddInfrastructure` and the DbContext, **without** hosted services, Kestrel or controllers, runs the command, and exits with a code.
  - The repo has no CLI/one-off job today; the startup seeder (`DatabaseSeedHostedService`, `Seed__Enabled`) runs on every boot and is the wrong place, because the seed must run only on demand.
  - Dennis runs the command through the Render **Shell** of `lobsy-acc-api`, or as a Render **one-off job** on that service.
- **D2. Deployment marker.** Acceptatie runs `ASPNETCORE_ENVIRONMENT=Production` (see `render.yaml`), so the environment name can't tell acc from production. New config key `Lobsy:DeploymentEnvironment`:
  - `Lobsy__DeploymentEnvironment=Acceptatie` on `lobsy-acc-api` and `lobsy-acc-web`
  - `Production` on `jobsy-api` and `jobsy-web`, set explicitly in `render.yaml`
  - Missing or any other value → refuse
- **D3. Extra accounts.** `Ambassadeur` is seeded too (it's a real role and needs testing). `SchoolAdmin` is seeded only once docs/scholen 01 is in code. **(Needs Dennis: keep Ambassadeur? add SchoolAdmin later?)**
- **D4. E-mail domain.** `TestAccounts:EmailDomain`, default `lobsy.nl`. These addresses only receive mail if a mailbox or catch-all exists for `test-*@lobsy.nl`. **(Needs Dennis: create a catch-all, or set the domain to one you control.)**
- **D5. Mail.** Mail **to** test addresses is allowed (it's your domain). Mail caused by a test account **to** a real user is always dropped and logged (no content, recipient redacted).
- **D6. Test ↔ real boundary.**
  - Test accounts may **read** public data (banenkaart, public vacancy pages).
  - Every **interaction** between test and real data is refused server-side with 403 `test_account_boundary`: apply, like, share tracking, talent contact, takeover, referral codes, invites, company membership.
  - Test vacancies and companies are visible only to test accounts and admins.
- **D7. Money.** Test companies get tokens from the seed (a `TokenTransactionKind.Grant` ledger entry with reason `test-seed`, no revenue). Stub checkouts by test accounts still grant tokens but create **no** invoice number, VAT entry, commission, payout, revenue share or self-billing entry. A live payment (Mollie `live_` key) makes the guard refuse.
- **D8. 2FA exemption.** Only `IsTestAccount` users skip 2FA, and only while the guard passes in the running process (API **and** Web). A test flag on a production row therefore changes nothing. The seed clears any authenticator on test accounts. Microsoft/Google logins stay 2FA-free as today. The **admin Google block stays**: the test admin logs in with a password only.
- **D9. Cleanup** is dry-run by default. It deletes only rows owned by test data, needs `--execute --expect-users <n>` with the exact number from the dry-run, and never deletes a user without `IsTestAccount`.
- **D10. Existing demo seed stays as it is.** `DemoUsersSeeder` (`@jobsy.local`, `Seed__Enabled=true` on acc) isn't touched by this PR. **Finding (needs Dennis):** that seeder gives every demo account, including `admin@jobsy.local` and the other privileged roles, the password `Jobsy123!`, which is written in the code. On acceptatie, anyone who knows it can sign in and, if 2FA isn't set up yet, set up their **own** authenticator. Recommendation for a follow-up: once the test accounts work, stop seeding privileged demo accounts on acc (or give them env-var passwords). Write it to `docs/testaccounts-followups.md`; don't change it here.

## Dependency checks (run before you start; paste the output in the PR)
- **S. Schools (`docs/scholen` 01).** `git grep -n "Teacher\|SchoolAdmin" origin/acceptatie -- Jobsy.Core/Enums/UserRole.cs` and `git grep -n "class SchoolClass\|class TeacherClassAssignment" origin/acceptatie -- Jobsy.Core`.
  - **Present:** seed `Teacher` with one test school, one class (`Testklas 1A`, a few pupil codes via the scholen code generator; codes are never printed) and the assignment. Also `SchoolAdmin` for that school. Test schools and classes get `IsTestData`. The schools feature switch isn't changed.
  - **Absent (today):** no teacher; the command skips it with a warning. Follow-up line: "Add Teacher/SchoolAdmin to the test-account seed after scholen 01."
- **AU. Auth stack (`docs/auth`).** `git grep -n "SalesManager" origin/acceptatie -- Jobsy.Core/Security/MfaPolicy.cs` (auth 02), `git grep -n "class AdminLoginProviderPolicy" origin/acceptatie -- Jobsy.Core` (auth 02), `git grep -n "MfaTrustedDevice" origin/acceptatie -- Jobsy.Core` (auth 04).
  - **MfaPolicy:** today `IsRequired(UserRole)` has 5 roles. Add the exemption as a **separate** method so both specs can land in any order: `MfaPolicy.IsRequiredFor(UserRole role, bool isTestAccount, bool testAccountsActive) => !(isTestAccount && testAccountsActive) && IsRequired(role)`, and switch the three callers to it:
    - `AuthController` L134 and L300
    - `MfaEnforcementMiddleware` L42
    - plus the device-session refresh of auth 02 when present
  - If auth 02 already merged, keep its SalesManager rule and its "every enum value" test, and add the exemption cases to that test. If auth 02 comes later, its spec must keep calling `IsRequiredFor` (write a note in `docs/testaccounts-followups.md` and in the PR).
  - **Admin Google block** (auth 02 `AdminLoginProviderPolicy`): unchanged. Add a test that the test admin via Google is still refused when the policy exists.
  - **Auth 01 lockout/rate limits:** they apply to test accounts too. No exemption.
  - **Trusted devices (auth 04):** nothing special; the exemption means no code is asked anyway.
- **AR. Admin redesign (`docs/admin-redesign`).** `git grep -n "class AdminNavCatalog\|interface IAdminAuditLog" origin/acceptatie -- Jobsy.Web Jobsy.Core`. Present → the badge goes on the redesigned users and companies tables. Absent (today) → on `Components/Pages/Admin/UsersAdmin.razor` and `CompaniesAdmin.razor`.

## How Dennis runs it on Render (acceptatie only)
**Once, in the Render Dashboard** (project Lobsy → environment **Acceptatie**):
1. `lobsy-acc-api` → Environment:
   - `Lobsy__DeploymentEnvironment` = `Acceptatie`
   - `TestAccounts__Enabled` = `true`
   - `TestAccounts__EmailDomain` = `lobsy.nl` (optional)
   - one secret per account (each at least 12 characters, `RegistrationPasswordRules`):
     - `TestAccounts__Password__Candidate`
     - `TestAccounts__Password__CandidateNew`
     - `TestAccounts__Password__BranchManager`
     - `TestAccounts__Password__EnterpriseManager`
     - `TestAccounts__Password__RegionalManager`
     - `TestAccounts__Password__Intermediary`
     - `TestAccounts__Password__SalesManager`
     - `TestAccounts__Password__Admin`
     - `TestAccounts__Password__Ambassadeur`
     - later `TestAccounts__Password__Teacher` and `TestAccounts__Password__SchoolAdmin`
   - A missing password → that account is skipped with a warning.
2. `lobsy-acc-web` → Environment: `Lobsy__DeploymentEnvironment` = `Acceptatie` and `TestAccounts__Enabled` = `true` (the Web needs them for the 2FA exemption; no passwords here).
3. Production (`jobsy-api`, `jobsy-web`): **nothing to add**. After the PR merges, `render.yaml` sets `Lobsy__DeploymentEnvironment=Production` and `TestAccounts__Enabled=false` there. If Blueprint sync doesn't apply, set them in the Dashboard the same way.
4. Save. Render redeploys the services. Wait until acceptatie is live with this PR merged.

**Seed:**
1. `lobsy-acc-api` → **Shell** tab. Run `cd /app && dotnet Jobsy.Api.dll test-accounts seed --dry-run` and read the plan (which accounts are created, updated or skipped, and why).
2. Then `dotnet Jobsy.Api.dll test-accounts seed`. It prints the role table and exits with code 0. Running it again changes nothing (or only rotates passwords you changed in the env vars).
3. No Shell tab, or the Shell run is killed for memory (a second .NET process on a starter instance)? Use a one-off job: `lobsy-acc-api` → **Jobs** (or the Render API `POST /v1/services/<service-id>/jobs` with `{"startCommand":"dotnet Jobsy.Api.dll test-accounts seed"}`), and read the output in the job's logs.

**Log in:** `https://acceptatie.lobsy.nl/login` with `test-<role>@lobsy.nl` and the password from the env var. No 2FA step.

**Cleanup:**
1. `dotnet Jobsy.Api.dll test-accounts cleanup` (a dry-run). It lists every account and the row count per table that would be deleted, and prints the line to run next.
2. `dotnet Jobsy.Api.dll test-accounts cleanup --execute --expect-users <n>` with the number from step 1.
3. Afterwards, set `TestAccounts__Enabled=false` if you want the test logins off completely.

**Exit codes:** `0` ok · `1` usage error · `2` guard refused (the message names the failed check, never a value) · `3` partial (some accounts skipped; see warnings) · `4` failed (nothing written; the transaction was rolled back).

## Needs Dennis
- D3: keep the extra Ambassadeur account (default yes); SchoolAdmin once schools exist.
- D4: a mailbox or catch-all for `test-*@lobsy.nl` (or another domain).
- D10: the public demo password `Jobsy123!` on privileged `@jobsy.local` accounts on acceptatie (follow-up).
- Setting the env vars above on acceptatie.

## Pointer prompt
```
Build the Lobsy test-accounts command. First: git fetch origin && git show origin/docs/testaccounts:docs/prompts/testaccounts/00-README.md — read it completely, then git show origin/docs/testaccounts:docs/prompts/testaccounts/01-seed-cleanup-testaccounts.md and execute it.
Branch cursor/test-accounts from origin/acceptatie (standalone, not stacked); ONE PR into acceptatie whose body starts with "Standalone (not stacked)". Run the README dependency checks (S schools, AU auth stack, AR admin redesign) first and put the output in the PR.
Build a CLI verb in the API binary (dotnet Jobsy.Api.dll test-accounts seed|cleanup), no HTTP endpoint, with the hard guard (Lobsy:DeploymentEnvironment=Acceptatie AND TestAccounts:Enabled=true AND nothing looks like production), IsTestAccount/IsTestData flags (one migration), the Testaccount badge in admin, exclusion from matching/stats/invoices/mails and the test↔real boundary, the MfaPolicy exemption for test accounts only (admin Google block unchanged), passwords only from TestAccounts__Password__<Role> env vars, the idempotent seed with sample data, and cleanup with dry-run first.
Build and test; if red or a success criterion can't be met, push, open the PR as draft, stop and report.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes. Never run the command against acceptatie or production yourself; never print, log or commit a password; never touch a real account.
Report: branch → PR number → status, the role table (seeded / skipped / missing), the migration name, the env vars Dennis must set on Render, the test list and anything deferred.
```
