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
| Salesmanager | `SalesManager` | Lobsy Partner portal (`/sales/*`): dashboard, link & materiaal, werkgevers (handelsnaam/plaats/eigen commissie), aanbevelen, wallet. Payouts via aanvraag (≥ € 50) + admin-goedkeuring van maandelijkse ronde. **2FA verplicht.** |
| Ambassadeur | `Ambassadeur` | **Geparkeerd** (`AmbassadorsEnabled` uit). Geen toegang tot ambassadeur-functies; gegevens blijven bewaard. 2FA blijft verplicht als de rol weer aan gaat. |
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
| Sales payout approve / SEPA export / mark paid | — | — | — | — | — | — | — | Admin + MFA session |
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
