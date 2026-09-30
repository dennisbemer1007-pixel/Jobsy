# ADR 0004: Roles and company scope

**Status:** Accepted  
**Date:** 2026-09-28

## Context

Authorization was historically implicit in controllers and Blazor `[Authorize(Roles=…)]` attributes. Product intent (Dennis, 2026-09-28) requires a clear matrix — especially **RegionalManager = read-only** for employer mutations (tokens unlock, culture save, onboarding checkout, vacancy lifecycle, application react).

## Decision

- Human-readable source of intent: [`docs/security/roles-matrix.md`](../security/roles-matrix.md).
- Code sources of truth:
  - `Jobsy.Core/Authorization/JobsyRoles.cs` (`EmployerMutateRoles` excludes RegionalManager)
  - `Jobsy.Core/Authorization/JobsyPolicies.cs` + API `AuthorizationExtensions`
  - `CompanyAuthorizationService` for company/branch/org accessibility
- Guard tests: `AuthorizationMatrixReflectionTests`, `CrossTenantAuthorizationTests`, `RegionalManagerReadOnlyAuthorizationTests`, `BlazorPageRoleAttributesTests`.
- Product page inventory (UX): [`ROLES_AND_VIEWS.md`](../../ROLES_AND_VIEWS.md).
- Blazor route × role table (generated): [`docs/ROUTES.md`](../ROUTES.md).

## Consequences

- New mutating employer endpoints must not authorize `RegionalManager`.
- Scope bugs (cross-tenant) are regressions — fix product code, do not weaken tests.
- Open questions (Intermediary longevity; Region entity vs memberships) stay in the roles-matrix doc until product decides.

## Werkgever redesign

Shared `/werkgever/…` pages for Bedrijfsmanager (BM), Regiomanager (RM) and Vestigingsmanager (VM):

- **RM is read-only** everywhere (D4): UI hides actions; mutating APIs return 403.
- **VM cannot purchase tokens** (D5): removed from `TokenPurchaseRoles`; uses tokenaanvraag + allocate from BM; no invoices.
- **Only BM invites** team members (D6): single `WgInviteDrawer`; `CompanyUsersController` stays EM/Intermediary/Admin.
- **Scope chip only narrows**: every endpoint re-checks `GetAccessibleCompanyIdsAsync`.
- Rights table: `Jobsy.Tests/Werkgever/WerkgeverRightsMatrix.cs` → section in [`docs/security/roles-matrix.md`](../security/roles-matrix.md).
