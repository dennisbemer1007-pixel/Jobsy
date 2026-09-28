Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 04 (HIGH): Role × scope authorization matrix, reflection guard and cross-tenant tests

**Goal:** make the role model explicit and guarded by tests, so that new endpoints cannot accidentally let RegionalManager mutate data or leak data across company/branch/region. Tests and docs only; no behaviour change unless a test uncovers a real leak (then fix it in a separate commit with a clear description).

**Roles (Dennis, 28-09):**
- kandidaat = `Candidate`
- bedrijfsmanager = `EnterpriseManager`: all branches and regions of its own company; posts vacancies; buys tokens.
- regiomanager = `RegionalManager`: READ-ONLY, only branches in its own region.
- branchmanager = `BranchManager`: EnterpriseManager rights, but only its own branch.
- salesmanager = `SalesManager`: wallet payout, sales toolkit, tracking code with % of referred employers.
- ambassadeur = `Ambassadeur`: like salesmanager, unfinished.
- decaan: not built.
- admin = `Admin`: everything.
- `Intermediary` exists in code but not in Dennis's list. Document it and ask Dennis in the PR description.

**Evidence:**
- Scope logic is central in `Jobsy.Infrastructure/Services/CompanyAuthorizationService.cs`:
  - BranchManager is limited to its primary company (~:143-147);
  - EnterpriseManager expands to child companies;
  - RegionalManager gets the companies from its `CompanyMembership` rows, **not** from a `Region` entity.
  - So "own region" = whatever memberships were granted. Verify how memberships are created for regiomanagers (`CompanyRegistrationService`, the invite flow) and whether a branch moved to another region loses access (drift risk).
- 66 controllers, 351 actions. Scope checks are called per action (`_companyAuth.*` / `CanAccessCompanyAsync`, pattern varies). No IDOR was found in the manual review of Vacancies, Applications, TalentPool, SalaryTables, Tokens, Branches, Dashboard, CompanyCulture and Me, but there is no systematic test.

**Do:**
1. `docs/security/roles-matrix.md`: a table of role × capability (read vacancies, create/publish vacancy, react to applications, buy tokens, allocate tokens, unlock talent, edit culture, manage branches/invites, salary tables, sales wallet/payout, tracking code, admin screens) with scope (own branch / own region memberships / own company + children / all). Include the Intermediary question and the Region-vs-membership question.
2. `Jobsy.Tests/AuthorizationMatrixReflectionTests.cs`:
   - Enumerate all controller actions via reflection (`Jobsy.Api` assembly, `ControllerBase` subclasses, `HttpPost/Put/Patch/Delete` attributes).
   - Compute the effective allowed roles from class- and action-level `[Authorize(Roles/Policy)]` (map policies via `JobsyPolicies` → `AuthorizationExtensions` role lists; copy the mapping into a test helper with a test that asserts it matches the real `AuthorizationOptions` by resolving `IAuthorizationPolicyProvider` from the test host).
   - Assert that **no mutating action admits RegionalManager**, except the explicit allow-list `RegionalManagerMutationAllowList` (expected: own profile/me/settings, notifications, device sessions, logout, feedback; each with a one-line reason).
   - A second test: every mutating action without `[AllowAnonymous]` has an `[Authorize]` (class or action).
   - Emit a readable failure message listing `Controller.Action` and the roles.
3. `Jobsy.Tests/CrossTenantAuthorizationTests.cs` (integration, existing test host/in-memory or test DB pattern). Seed company A (branch A1, A2, same enterprise) and company B. Then check:
   - BranchManager A1 cannot GET/PUT vacancy, applications, salary table, culture or tokens of A2 or B (403/404);
   - EnterpriseManager A can access A1 and A2 but not B;
   - RegionalManager with membership A1 can read A1 but not A2 or B, and gets 403 on mutations;
   - Candidate X cannot read candidate Y's applications or CV (`ApplicationsController` CV endpoints at ~:188/:263);
   - SalesManager S1 cannot read S2's wallet/payouts/tracking stats;
   - Ambassadeur likewise.
   Cover at least one representative endpoint per controller group; list the ones not covered in the test file header.
4. Blazor: add `Jobsy.Tests/BlazorPageRoleAttributesTests.cs`, which reflects over `Jobsy.Web` components with `@page` + `[Authorize(Roles=...)]` and asserts that pages whose name or route implies mutation (`CreateVacancy`, `EditVacancy`, `OnboardingCheckout`, `Tokens` purchase) do not list RegionalManager, with an allow-list. This is a lightweight guard; the API remains the source of truth.

**Do not touch:** production code (except to fix a real leak, in a separate commit); `pr-tests.yml` filters (the new tests are regular unit/integration tests, run by the existing unit step; check that the filter includes them).

**Verify:** the new tests pass on acceptatie **after prompt 03**; before 03 they must fail on exactly the 5 endpoints from prompt 03 (mention this in the PR). Full test suite green; build green.

**Dependency:** **after prompt 03** (otherwise the reflection test fails by design).
