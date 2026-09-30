# Werkgever redesign — server-side tightenings (01–08)

Stricter never looser. Documented for release notes / Dennis.

| # | Tightening | Where |
|---|---|---|
| D5 | `BranchManager` removed from `JobsyRoles.TokenPurchaseRoles` / `CanPurchaseTokens` — VM cannot buy tokens | Core auth + Tokens checkout |
| D4 | RegionalManager rejected on all employer mutating endpoints (react, publish options, allocate, invite, unlock, …) | Controllers + matrix tests |
| D6 | Only EnterpriseManager (BM) / Intermediary / Admin invite via `CompanyUsersController` — VM/RM invite UI removed | API already strict; UI single `WgInviteDrawer` |
| D7 | `approve-publish` remains BM-only | VacanciesController |
| 07 | Insights unlock: RM cannot unlock; VM only when `CandidateInsightsUnlockPerBranch` and own wallet; else request path | CandidateInsightsController |
| 07 | Locked insights fields omitted server-side (never sent to browser) | CandidateInsightsService |
| D3 | Scope chip only narrows — endpoints re-check `GetAccessibleCompanyIdsAsync` | EmployerScope + company auth |
| 01+ | Every `/werkgever` page has explicit `[Authorize(Roles=…)]` matching the matrix | Blazor pages + WerkgeverRightsMatrix |

No intentional loosening versus pre-redesign attributes.
