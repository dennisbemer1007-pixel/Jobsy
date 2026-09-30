# Role × capability matrix

Source of truth for authorization intent (Dennis, 28-09-2026) plus how Jobsy implements scope today.
API `[Authorize]` attributes remain authoritative; this document is the human-readable map.

## Roles

| Role (NL) | Claim / `UserRole` | Scope (intended) |
|---|---|---|
| Kandidaat | `Candidate` | Own profile, applications, DNA/tests |
| Bedrijfsmanager | `EnterpriseManager` | All branches + regions of **own company**; posts vacancies; buys / allocates tokens |
| Regiomanager | `RegionalManager` | **Read-only**, only branches in its own region |
| Filiaalmanager | `BranchManager` | Enterprise-like rights, but **own branch only** |
| Salesmanager | `SalesManager` | Wallet payout, sales toolkit, tracking code + % of referred employers |
| Ambassadeur | `Ambassadeur` | Like salesmanager (product unfinished) |
| Decaan | — | Not built |
| Admin | `Admin` | Everything |
| Intermediary | `Intermediary` | **Exists in code, not in Dennis’s list** — see open question below |

## Capability matrix

Legend for **Scope**: `branch` = primary company only · `memberships` = `UserCompany` rows · `org+children` = enterprise expand · `self` = own user · `all` = platform.

| Capability | Candidate | BranchManager | RegionalManager | EnterpriseManager | Intermediary | SalesManager | Ambassadeur | Admin |
|---|---|---|---|---|---|---|---|---|
| Read vacancies (public map) | yes | yes | yes | yes | yes | yes | yes | yes |
| Read managed vacancies | — | branch | memberships (RO) | org+children | clients | — | — | all |
| Create / publish vacancy | — | branch | **no** | org+children | clients | — | — | all |
| React to applications (accept/reject) | — | branch | **no** | org+children | clients | — | — | all |
| Buy tokens | — | branch* | **no** | org pot | yes | — | — | yes |
| Allocate tokens | — | no | **no** | org → branches | no | — | — | yes |
| Unlock talent / withdraw | — | branch | **no** | org+children | clients | — | — | —† |
| Edit company culture | — | branch | **no** | org+children | clients | — | — | yes‡ |
| Manage branches / invites | — | limited | **no** (read) | org | limited | — | — | all |
| Salary tables (manage) | — | read/use | read | org | clients | — | — | all |
| Sales wallet / payout | — | — | — | — | — | self | self | all |
| Tracking code / toolkit | — | — | — | partner affiliate | partner affiliate | self | self | all |
| Admin screens | — | — | — | — | — | — | — | all |
| Own profile / sessions / feedback | self | self | self | self | self | self | self | self |

\* Branch purchase only when the vestiging is not under enterprise token management (`CanPurchaseTokens` + company flags).  
† Talent unlock is `RequireEmployer` + `EmployerMutateRoles` (no Admin on those actions).  
‡ Culture PUT uses `EmployerMutateRoles` (no Admin); Admin may use other admin tools.

## How scope is resolved today

`CompanyAuthorizationService` (`Jobsy.Infrastructure/Services/CompanyAuthorizationService.cs`):

- **BranchManager** → strictly `User.CompanyId` (primary company).
- **EnterpriseManager** → memberships + recursive child companies.
- **RegionalManager** → companies from `CompanyMembership` / primary company — **not** from a `Region` entity.
- So “own region” in product language currently means **whatever memberships were granted** at invite/registration time.

Membership creation: demo seeder + invite / company registration flows attach `UserCompany` rows. If a branch is later moved to another region without updating memberships, access can **drift** (stale memberships or missing ones).

## Open questions for Dennis

1. **Intermediary** — present in `JobsyRoles.EmployerRoles` and production flows (multi-client hiring). Keep as a first-class role, fold into EnterpriseManager, or retire?
2. **Region vs membership** — should RegionalManager scope be driven by a `Region` entity (and auto-update when branches move), or is explicit `UserCompany` membership the lasting model?

## Related code

- Role constants / mutate helpers: `Jobsy.Core/Authorization/JobsyRoles.cs`
- Policies: `Jobsy.Core/Authorization/JobsyPolicies.cs` + `Jobsy.Api/Authorization/AuthorizationExtensions.cs`
- Guards: `Jobsy.Tests/AuthorizationMatrixReflectionTests.cs`, `CrossTenantAuthorizationTests.cs`, `BlazorPageRoleAttributesTests.cs`, `RegionalManagerReadOnlyAuthorizationTests.cs` (prompt 03)

## Werkgever redesign (BM / RM / VM)

Generated from `Jobsy.Tests/Werkgever/WerkgeverRightsMatrix.cs`. Do not hand-edit — regenerate via the matrix completeness / freshness tests.

Legend: ● full · ◐ read-only · ◯ own scope · — hidden/403.

| Page | BM | RM | VM | Authorize roles |
|---|---|---|---|---|
| `/werkgever` | ● | ◐ | ◯ | `BranchManager,RegionalManager,EnterpriseManager,Intermediary` |
| `/werkgever/te-doen` | ● | ◐ | ◯ | `BranchManager,RegionalManager,EnterpriseManager` |
| `/werkgever/vacatures` | ● | ◐ | ◯ | `BranchManager,RegionalManager,EnterpriseManager,Intermediary,Admin` |
| `/werkgever/vacatures/nieuw` | ● | — | ◯ | `BranchManager,EnterpriseManager,Intermediary` |
| `/werkgever/sollicitaties` | ● | ◐ | ◯ | `BranchManager,RegionalManager,EnterpriseManager,Intermediary,Admin` |
| `/werkgever/sollicitaties/{ApplicationId:guid}` | ● | ◐ | ◯ | `BranchManager,RegionalManager,EnterpriseManager,Intermediary,Admin` |
| `/werkgever/talentpool` | ● | ◐ | ◯ | `BranchManager,RegionalManager,EnterpriseManager,Intermediary` |
| `/werkgever/kandidaatinzichten` | ● | ◐ | ◯ | `BranchManager,RegionalManager,EnterpriseManager` |
| `/werkgever/organisatie/vestigingen` | ● | ◐ | — | `RegionalManager,EnterpriseManager,Admin` |
| `/werkgever/organisatie/team` | ● | — | — | `EnterpriseManager,Admin` |
| `/werkgever/organisatie/profiel` | ● | — | ◯ | `BranchManager,EnterpriseManager,Admin,Intermediary` |
| `/werkgever/organisatie/salaristabellen` | ● | — | ◯ | `BranchManager,EnterpriseManager,Admin` |
| `/werkgever/organisatie/salaristabellen/{TableId:guid}` | ● | — | ◯ | `BranchManager,EnterpriseManager,Admin` |
| `/werkgever/tokens` | ● | ◐ | ◯ | `BranchManager,RegionalManager,EnterpriseManager,Intermediary` |
| `/werkgever/tokens/verbruik` | ● | ◐ | — | `RegionalManager,EnterpriseManager,Intermediary` |
| `/werkgever/tokens/mutaties` | ● | ◐ | ◯ | `BranchManager,RegionalManager,EnterpriseManager,Intermediary` |
| `/werkgever/tokens/facturen` | ● | — | — | `EnterpriseManager,Intermediary,Admin` |
| `/werkgever/koppelingen` | ● | — | — | `EnterpriseManager,Admin` |
| `/werkgever/overnames` | ● | — | ◯ | `BranchManager,EnterpriseManager,Admin` |
| `/werkgever/wervingsmateriaal` | ● | ◐ | ◯ | `BranchManager,RegionalManager,EnterpriseManager,Admin` |
| `/werkgever/partner` | ● | — | — | `EnterpriseManager,Intermediary` |
| `/werkgever/partner/uitbetalen` | ● | — | — | `EnterpriseManager,Intermediary` |

| Mutating / employer API | BM | RM | VM |
|---|---|---|---|
| `api/werkgever/dashboard` | yes | yes | yes |
| `api/werkgever/te-doen` | yes | yes | yes |
| `api/werkgever/tokens/summary` | yes | yes | yes |
| `api/vacancies/manage` | yes | yes | yes |
| `api/vacancies` | yes | no | yes |
| `api/vacancies/{id}` | yes | no | yes |
| `api/vacancies/publish` | yes | no | yes |
| `api/vacancies/{id}/approve-publish` | yes | no | no |
| `api/vacancies/{id}/highlight` | yes | no | yes |
| `api/vacancies/{id}/pushbom` | yes | no | yes |
| `api/vacancies/{id}/extend` | yes | no | yes |
| `api/vacancies/{id}/inactive` | yes | no | yes |
| `api/vacancies/{id}/contact-preference` | yes | no | yes |
| `api/vacancies/{id}/email-verification` | yes | no | yes |
| `api/applications/{id}/react` | yes | no | yes |
| `api/applications/{id}/contact` | yes | no | yes |
| `api/applications/vacancies/{vacancyId}/fulfill/{applicationId}` | yes | no | yes |
| `api/company-users/invite` | yes | no | no |
| `api/company-users/{id}` | yes | no | no |
| `api/regions` | yes | no | no |
| `api/regions/{id}` | yes | no | no |
| `api/companies/from-kvk` | yes | no | no |
| `api/companies/{companyId}/token-management` | yes | no | no |
| `api/companies/{companyId}/csv-batch-import` | yes | no | no |
| `api/companies/{companyId}/email-verification` | yes | no | yes |
| `api/companies/{companyId}/contact-preference` | yes | no | yes |
| `api/companies/{companyId}/billing-preference` | yes | no | no |
| `api/companies/{id}/billing-history` | yes | no | no |
| `api/tokens/checkout` | yes | no | no |
| `api/tokens/top-up-quote` | yes | no | no |
| `api/tokens/allocate` | yes | no | no |
| `api/werkgever/token-requests` | yes | no | yes |
| `api/werkgever/token-requests/{id}/approve` | yes | no | no |
| `api/werkgever/token-requests/{id}/reject` | yes | no | no |
| `api/werkgever/token-requests/{id}/withdraw` | yes | no | yes |
| `api/employer/candidate-insights/unlock` | yes | no | yes |
| `api/employer/candidate-insights/unlock-request` | no | no | yes |
| `api/employer/candidate-insights/unlock-request/{id}/reject` | yes | no | no |
| `api/employer/candidate-insights/export.csv` | yes | yes | yes |
| `api/employer/talent/unlock` | yes | no | yes |
| `api/employer/talent/{requestId}/withdraw` | yes | no | yes |
| `api/registration/takeovers/{id}/approve` | yes | no | yes |
| `api/registration/takeovers/{id}/reject` | yes | no | yes |
| `api/salary-tables` | yes | no | yes |
| `api/company/culture` | yes | no | yes |
| `api/vacancies/csv-import` | yes | no | no |
| `api/vacancies/csv-import/row` | yes | no | no |
| `api/companies/{companyId}/api-keys` | yes | no | no |
| `api/companies/{companyId}/api-keys/{apiKeyId}/deactivate` | yes | no | no |
| `api/companies/{companyId}/api-keys/email-credentials` | yes | no | no |

