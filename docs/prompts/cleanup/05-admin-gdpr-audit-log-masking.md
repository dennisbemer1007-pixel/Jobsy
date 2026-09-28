Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 05 (HIGH): Admin GDPR: personal-data access log, masked/aggregated admin views by default, pagination, IBAN encryption at rest

**Goal:** the admin role no longer sees raw personal data by default. Every access to personal data by staff is logged (who, what, whose data, when, why). This is an intentional behaviour change for **admin screens only**; candidate, employer and sales flows stay the same.

**Inventory: what admin can see today (acceptatie @ ccf6976; see code-review.md §0.5 for the full list):**

| Surface | Personal data exposed |
|---|---|
| `GET /api/admin/users` (`Jobsy.Api/Controllers/AdminController.cs:214-232`, DTO `AdminUserDetailDto` in `Jobsy.Api/Models/Sprint6Dtos.cs:21`) → `Admin/UsersAdmin.razor` | email, first/last name, role, company, created/last login for **all users, unpaginated** |
| `GET /api/applications` (`ApplicationsController.cs:67-125`), admin branch **without scope filter or Take** | candidate name, email, address, city, snapshot phone number, age, motivation, status |
| CV download endpoints (`ApplicationsController.cs` ~:188 and ~:263), admin allowed | full CV file (all CV PII) |
| `SalesManagersController` / `AmbassadeursController` → `SalesManagersAdmin.razor`, `AmbassadeursAdmin.razor` | name, email, phone, **IBAN (stored in plain text: `JobsyDbContext.cs` ~:1402/1430/1455)**, payouts, referred companies |
| `FeedbackController` → `FeedbackAdmin.razor` | user email/id, free text, **screenshots** (may contain someone else's PII) |
| `PlatformLogsController` → `LoggingAdmin.razor` | `DetailsJson` may contain emails, IDs, IPs (check the log call sites) |
| `TokenFinanceController`, `VatDeclarationsController` → `TokenFinanceAdmin`, `FinanceAdmin` | company contact names/emails, invoice addresses, exports (CSV) |
| `EmailCatalogController` / `MailTestAdmin.razor` | sending to arbitrary addresses, template previews with real data? (verify) |
| `CompaniesAdmin.razor`, `ModerationAdmin.razor`, `VacanciesAdmin.razor` | contact persons, vacancy contacts, moderation of candidate texts |
| Admin impersonation or "view as"? | verify: `rg -n "Impersonat" Jobsy.*` |

`PlatformLog` (`Jobsy.Core/Entities/PlatformLog.cs`) has no actor, subject or purpose column. **There is no access log for personal data.**

**Do:**
1. **Entity + service:** add `Jobsy.Core/Entities/PersonalDataAccessLog.cs` with these columns:
   - `Id`, `OccurredAt` (UTC), `ActorUserId`, `ActorRole`;
   - `SubjectUserId?`, `SubjectCompanyId?`;
   - `Resource` (e.g. `admin.users.list`, `application.cv.download`, `salesmanager.iban.view`);
   - `Action` (list/view/export/download/reveal);
   - `Reason?`, `SupportAccessGrantId?` (used in prompt 06);
   - `CorrelationId`, `IpHash` (SHA-256 + salt, no raw IP).

   Add `IPersonalDataAccessLogger` (Core) and an implementation (Infrastructure). Writes are fire-and-forget safe but must not be lost silently: log a failure to ILogger. Retention: 2 years, configurable, via an existing cleanup/background job pattern if there is one, otherwise document it.
2. **Masking by default (API):** add `Jobsy.Core/Privacy/PersonalDataMasker` with `MaskEmail("jan.jansen@x.nl") → "j***@x.nl"`, `MaskName → "Jan J."`, `MaskPhone → "••• ••• 12"`, `MaskIban → "NL•• •••• •••• 1234"`, address → city only, age → age band.
   - Admin endpoints listed above return masked values by default.
   - Unmasked values are only returned via prompt 06 (support access), or for the admin's own record.
   - **Candidate/employer/sales endpoints for their own data are unchanged.**
3. **Aggregated views:** the admin users overview shows counts per role, company, active/inactive and registration week by default, plus a searchable, **paginated** (`page`, `pageSize` ≤ 100) masked list. `GET /api/applications` for admin: require `companyId` or `vacancyId` filters, or return aggregate counts only. Add `Take` + pagination. Update `UsersAdmin.razor` and the other admin pages to show masked values with a "why do I need to see this?" hint that links to the support access flow (prompt 06; until then, show a disabled button).
4. **Logging:** call the logger for:
   - admin list endpoints (one row per request, subject null, with filter summary);
   - admin detail views (subject set);
   - every CV download (all roles: employer too, since it is also personal data; subject = candidate);
   - talent-pool unlock (employer sees contact details);
   - finance CSV exports;
   - feedback screenshot views.
5. **IBAN at rest:** add an EF value converter using the existing DataProtection key ring (`IDataProtectionProvider`, purpose `Jobsy.Iban.v1`) for the IBAN columns (`JobsyDbContext.cs` ~:1402/1430/1455), plus a data migration that encrypts existing rows (idempotent: detect already-protected values). Check that payout flows still read the IBAN (tests). **Keys must be persisted** (they are in the DB/Render disk; check `DataProtection` config in Api Program.cs). Document the key-loss risk.
6. **Admin page for the log:** `Admin/PersonalDataAccessLogAdmin.razor` (read-only, paginated, filter by actor/subject/date). Accessing it is itself logged.
7. **Migration:** create the EF migration **last**, after rebasing on the latest acceptatie (`dotnet ef migrations add AddPersonalDataAccessLog -p Jobsy.Infrastructure -s Jobsy.Api`), because other PRs may add migrations. Ensure the model snapshot is clean.

**Do not touch:** candidate/employer/sales self-service endpoints (other than adding logging to CV download and unlock); authentication/MFA; CSS files that pending PRs change (use existing admin table classes); the OpenAI services (a separate DPIA question; list it in the PR).

**Verify:**
- Build green; all tests green.
- New tests:
  - masker unit tests;
  - admin `GET /api/admin/users` returns masked email/name and pagination;
  - admin `GET /api/applications` without a filter returns aggregates or 400;
  - a CV download writes exactly one `PersonalDataAccessLog` row;
  - IBAN round-trips through the converter and the DB column no longer contains the plain IBAN (raw SQL assert);
  - SalesManager payout still sees their own full IBAN.
- Playwright/manual: the admin users page shows masked data and loads fast with pagination.
- `dotnet ef migrations script` succeeds.

**Dependency:** after 03/04 is preferred (independent code, but the same test infrastructure). Coordinate with pending PRs: **none touch admin pages**, but rebase before creating the migration.
