Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 03 (HIGH): Enforce read-only access for RegionalManager in the API and hide mutating UI actions

**Goal:** a regiomanager is **read-only** for the branches in its region, per Dennis's role definition. Today some API endpoints still let RegionalManager mutate data. This is an intentional authorization fix; all other roles keep their current behaviour.

**Evidence (acceptatie @ ccf6976):**
- `Jobsy.Core/Authorization/JobsyRoles.cs` `EmployerRoles` includes `RegionalManager`. The policies `RequireEmployer` and `RequireAdminOrEmployer` (`Jobsy.Api/Authorization/AuthorizationExtensions.cs:157-163`) therefore admit it.
- Mutating endpoints that are protected only by those class-level policies:
  - `Jobsy.Api/Controllers/TalentPoolController.cs:70` `POST unlock`: spends tokens and returns candidate contact PII after consent. Class policy at `:13`.
  - `TalentPoolController.cs:97` `POST {requestId}/withdraw`.
  - `Jobsy.Api/Controllers/CompanyCultureController.cs:43` `PUT` company culture. Class policy at `:12`.
  - `Jobsy.Api/Controllers/SupplierOnboardingController.cs:27` `POST checkout` and `:56` `POST complete`. Class policy at `:10`.
- The existing pattern for correct handling is already there: `JobsyRoles.VacancyLifecycleRoles`, `ApplicationReactRoles`, `TokenPurchaseRoles` and `TokenAllocateRoles` (without RegionalManager), used as `[Authorize(Roles = ...)]` per action.
- `Jobsy.Api/Controllers/SalaryTablesController.cs:130-170` `GET {id}` calls `FillEmptySalaryTablesAsync` (a DB write) **before** the company authorization check (~:163). Any employer can therefore trigger writes on another company's salary table ID (no data leak, but an unauthorized side effect).
- Blazor pages that admit RegionalManager and show mutating actions:
  - `Components/Pages/Employer/TalentPool.razor:3` (unlock button)
  - `Employer/TalentContacts.razor:3` (withdraw)
  - `Employer/CultureScan.razor:4` (save culture)
  - `Employer/OnboardingCheckout.razor:3`
  - `Employer/Tokens.razor:4` (buy button; the API already rejects via TokenPurchaseRoles, but the UI shows it)
  - `Regional/TokenControl.razor:3` (allocate; the API rejects)
  - `Employer/Branches.razor:4`: check the invite and edit actions outside the `<AuthorizeView Roles="EnterpriseManager">` at :15.
- No test covers RegionalManager on these endpoints (`rg -l "RegionalManager" Jobsy.Tests`, then check).

**Do:**
1. In `JobsyRoles`, add `public const string EmployerMutateRoles = "{BranchManager},{EnterpriseManager},{Intermediary}"` (and optionally `...WithAdmin`) with an XML doc: "RegionalManager is read-only".
2. Add `[Authorize(Roles = JobsyRoles.EmployerMutateRoles)]` to TalentPool `unlock` and `withdraw` and to CompanyCulture `PUT`. For SupplierOnboarding `checkout` and `complete`, use the variant including Admin (admin currently allowed there). Keep the class-level policies (reads stay allowed).
3. SalaryTables `GET {id}`: move the company authorization check **before** `FillEmptySalaryTablesAsync`. Also skip the fill for RegionalManager (read-only), or better, move the fill to the create or migration path if that is trivial. Otherwise only reorder.
4. UI: remove `RegionalManager` from the `@attribute [Authorize]` of `OnboardingCheckout.razor`. In TalentPool, TalentContacts, CultureScan, Tokens, TokenControl and Branches, keep read access but hide or disable mutating buttons for RegionalManager. Use `JobsyRoles.CanPurchaseTokens` / `CanAllocateTokens` / a new `CanMutateEmployerData(role)`, and add a short read-only notice via a new UiStrings key (nl + en; other languages fall back).
5. Tests (new file `Jobsy.Tests/RegionalManagerReadOnlyAuthorizationTests.cs`), using the existing API test host and auth helpers (see how `*AuthorizationTests.cs` / `WebApplicationFactory` tests log in as a role):
   - RegionalManager gets 403 on each of the 5 mutating endpoints;
   - BranchManager and EnterpriseManager still get non-403 (200/400 as today);
   - RegionalManager GETs (talent search, requests, culture GET, salary tables GET) still get 200;
   - SalaryTables GET for another company returns 403/404 **and does not write** (assert row count/updatedAt unchanged).

**Do not touch:** `CompanyAuthorizationService` scope logic (correct per the review); vacancy/application controllers (already correct); the RegionalManager membership model (see prompt 04 for the Region-vs-membership question); CSS files (pending CSS PRs).

**Verify:**
- Build green; all existing tests pass; the new tests pass.
- Manual/Playwright check with the demo RegionalManager: `/employer/talent` shows no unlock button; `/employer/culture` is read-only; `/employer/onboarding-checkout` redirects to access-denied.
- BranchManager demo: unlock and culture save still work.

**Dependency:** none. Can go right after 01 (or before it; it is independent).
