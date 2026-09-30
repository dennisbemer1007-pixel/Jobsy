# 02. Company verification status + one public-visibility rule (unverified = invisible)

Read `00-README.md` first. Branch `cursor/werkgever-aanmelding-2` from `cursor/werkgever-aanmelding-1`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/werkgever-aanmelding-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - The migration marks **every existing company Verified** (D6). Nothing that is public today may disappear because of this PR.

| | |
|---|---|
| Branch | `cursor/werkgever-aanmelding-2` |
| PR title | `feat(companies): company verification status (backfilled) + one server-side public-visibility rule for map, search, Match, company pages, sitemap and JSON-LD` |
| PR body starts with | `Stacked on #<PR 01> (cursor/werkgever-aanmelding-1)` |
| Mockups | none directly; wr-d11 "Zichtbaarheid" panel shows the effect |
| Split seam | **02a** = entity + enum + migration/backfill + creation paths (02.2, 02.3). **02b** = `PublicVisibility` + all query sites + index + tests (02.4–02.6) |

## Goal
A company that hasn't proven it's real is invisible to the public in every channel, enforced in one place on the server. Everything that exists today stays visible.

## 02.1 Today (verify first)
- `Company.KvkVerificationStatus` (`Verified` default, `Pending`, `Failed`) only means "the KvK record was checked". `KvkVerificationRules.CanPublishOrSpend` blocks publishing (`VacancyProductService` ~L89) and token spending (`TokensController` ~L190) for Pending/Failed. **No public query looks at any verification.**
- Public read paths:
  - `VacancyDiscoveryIndex` (in-memory index built by `VacancyDiscoveryIndexHostedService`; filters `Status == Active` + dates and `VacancyVisibilityRules.IsPubliclyVisible`, ~L122 and ~L232). It feeds `VacanciesController` `map-view`, `pins`, `cards`, `{id}/card`, `discover` and the candidate match (`CandidateMatchSnapshotService`, `ProfileVacancyMatchService`, `RoleFitCheckService`).
  - `VacanciesController` `{id}` (detail), `{id}/image`, `{id}/travel`, `{id}/culture-fit` (anonymous).
  - `PublicCompaniesController` (`api/public/companies`: by KvK / vestigingsnummer) → `Pages/CompanyPublicPage.razor` (`/{kvk}` and `/{kvk}/{vestigingsnummer}`).
  - `EmployerFlyersController` `public/branches/{companyId}/route` → `Pages/VestigingLanding.razor` (`/vestiging/{id}`).
  - `SiteController` `crawl-index` → `Seo/SeoEndpoints.cs` sitemap (vacancies + `CompanyPaths`). `Seo/StructuredData.cs` `JobPosting` (~L145) on the vacancy detail; Organization data on the company page.
  - `ApplicationsController` and `VacancyEngagementController` use `VacancyVisibilityRules`.
- Company creation paths (`git grep -n "new Company" -- '*.cs'`): `CompanyRegistrationService` (~L709 takeover org, ~L915/932/947/971 provision, ~L1058 siblings), `CompaniesController` ~L136 (bedrijfsmanager adds a vestiging from KvK) and ~L201 (intermediary client from KvK), `AdminController` ~L201, `AtsVacancyModerationService` ~L354, and the seeders (`DemoCompaniesSeeder`, `*VacanciesSeeder`). `CandidateApplicationLocation` builds non-persisted objects (ignore).

## 02.2 Model (D2)
- `enum CompanyVerificationStatus { Unverified = 0, Pending = 1, Verified = 2, Rejected = 3 }` (`Pending` = a method is running: letter sent, manual check requested).
- `enum CompanyVerificationMethod { None = 0, BusinessEmail, Letter, Manual, Backfill, AdminCreated, InheritedFromOrganization, IntermediaryClient }`.
- `Company`: `VerificationStatus`, `VerificationMethod`, `VerifiedAtUtc?`, `VerificationUpdatedAtUtc?`. Index on `VerificationStatus`. **The C# property has no initializer** (every creation path sets it explicitly, 02.3).
- Migration `AddCompanyVerificationStatus`: add the columns with a temporary default `Verified`/`Backfill` so **every existing row is backfilled** (D6), set `VerifiedAtUtc` = migration time, then drop the column defaults in the same migration. `Down` drops the columns. Log the backfilled count as a `PlatformLog` row the first time the app starts after it (not in the migration itself).

## 02.3 Every creation path sets the status explicitly
| Path | Status / method |
|---|---|
| Registration provision (org, branch, siblings) | `Unverified` / `None` (06 starts methods) |
| Takeover org shell (`ApproveTakeoverAsync` ~L709) | copy the target's status and method |
| Bedrijfsmanager adds a vestiging (`CompaniesController` ~L136), and 03's suggested new vestigingen | copy the organisation's status; method `InheritedFromOrganization` |
| Intermediary client from KvK (~L201) | the intermediary's status; method `IntermediaryClient` (flips with it, 03.5). Skip this row if README Dependencies **G** is Present (links aren't companies) |
| Admin creates (`AdminController`), ATS moderation after admin approval, seeders | `Verified` / `AdminCreated` |
- Guard test `CompanyCreationSetsVerificationTests`: a source scan of `Jobsy.Api` + `Jobsy.Infrastructure` (excluding Migrations/Tests) finds every `new Company` initializer and fails when it doesn't assign `VerificationStatus`. There's an allow-list for `CandidateApplicationLocation`.

## 02.4 One rule: `Jobsy.Core/Rules/PublicVisibility.cs`
- `static bool IsCompanyPublic(Company c)` → `c.VerificationStatus == Verified`.
- `static bool IsVacancyPublic(Vacancy v, DateOnly today)` → the existing `VacancyVisibilityRules.IsPubliclyVisible(v, today)` **and** `IsCompanyPublic(v.Company)` **and** (`v.IntermediaryCompanyId == null` or `IsCompanyPublic(v.IntermediaryCompany)`).
- EF expressions for queries: `PublicVisibility.CompanyIsPublic` (`Expression<Func<Company,bool>>`) and `PublicVisibility.VacancyPublisherIsPublic` (`Expression<Func<Vacancy,bool>>`, company + intermediary). They're used in SQL so unverified rows never leave the database for public reads.
- `VacancyVisibilityRules.IsPubliclyVisible(...)` overloads call `PublicVisibility` (so the existing callers in `ApplicationsController`, `MockInterviewController`, `SiteController`, `VacanciesController`, `VacancyEngagementController` and the index get the rule automatically). `VacancyDiscoveryRecord` gets `PublisherVerified` (set at index build), used by the record overload.
- Don't duplicate the condition anywhere else; call these members. XML comment on the class: "The only place that decides public visibility. Add new public read paths here and to `PublicVisibilityEndpointTests`."

## 02.5 Apply it to every public read path
- **Index:** the `VacancyDiscoveryIndex` load query adds `.Where(PublicVisibility.VacancyPublisherIsPublic)` and includes `IntermediaryCompany`. Add `IVacancyDiscoveryIndex.InvalidateCompanyAsync(Guid companyId)` (or reuse the existing refresh hook) so a verification change (03) or a backfill appears within one refresh cycle (target ≤ 60 s). Map, pins, cards, discover and Match follow automatically.
- **Vacancy detail, image, travel, culture-fit, apply:** anonymous/candidate callers get **404** (the same body as a missing vacancy) when not public. The employer's **own** preview stays: an authenticated user with access to the vacancy's company (existing `_companyAuth` check) gets 200 plus the response header `X-Robots-Tag: noindex` and a `IsPreview=true` flag on the DTO. `VacancyDetail.razor` shows a small "Voorbeeld · nog niet zichtbaar voor kandidaten" line for them (`WaBanner.PreviewLine`).
- **Company pages:** `PublicCompaniesController` returns 404 for unverified companies (same as unknown KvK, no distinction). If a verified vestiging belongs to an unverified organisation, the organisation page is 404 and the vestiging page shows only the vestiging. The vacancy list on the page uses the public index. `EmployerFlyersController` public route + `VestigingLanding` → 404/"niet gevonden" for unverified.
- **Sitemap + SEO:** `SiteController.crawl-index` only lists public vacancies and companies (use the rule). `StructuredData.JobPosting` and the company Organization JSON-LD are only emitted for public rows (they come from the same DTOs; add an assertion in `StructuredData` that `IsPreview` suppresses JSON-LD). `PageSeoCatalog` marks preview responses `noindex`.
- **Other anonymous endpoints:** go through `git grep -n "AllowAnonymous" -- Jobsy.Api/Controllers` once. For each GET that returns vacancy or company data, apply the rule or document why it's safe (e.g. `api/kvk/*` returns KvK data, not Lobsy company data; `RegistrationController` establishments returns only `IsInUse`). Put the classification table in the PR.
- The `ExternalVacanciesController` (ATS push, API-key auth) keeps working. A pushed vacancy of an unverified company is stored but not public.

## 02.6 Tests
- `PublicVisibilityTests` (unit): company Unverified/Pending/Rejected → not public; Verified → public; the vacancy needs its company **and** (if any) its intermediary to be Verified; the existing visibility conditions still apply.
- `PublicVisibilityEndpointTests` (API integration): seed an **unverified** company with an Active, in-date vacancy and a **verified** control. Call every anonymous read endpoint (map-view, pins, cards, card, discover, detail, image, travel, culture-fit, public company by KvK and by vestiging, flyer route, crawl-index, apply) → the unverified vacancy/company never appears (404 or absent); the control does. Plus a reflection test: every `[AllowAnonymous]` GET action in `VacanciesController`, `PublicCompaniesController`, `SiteController`, `EmployerFlyersController` and `VacancyEngagementController` must be listed in this test class (either "filtered" or "safe"), so a new public endpoint can't skip the rule.
- Web: sitemap XML has no URL of the unverified company/vacancy. The vacancy detail page for an anonymous visitor → the not-found page, no JSON-LD. The owner's preview → 200, `noindex`, no JSON-LD.
- Match: the unverified company's vacancy never appears in a candidate's match list or snapshot.
- Migration: after migrate, all pre-existing companies are `Verified`/`Backfill`, and a company inserted afterwards without a status fails the guard test (02.3).
- `EfModelSnapshotTests`, `PendingModelChangesTests`, `EfMigrationDiscoveryTests`, `VacancyVisibilityRulesTests`, `CompanyPublicPathsTests` stay green.

## Success criteria
- On a freshly migrated copy of the acceptatie data, the public vacancy count, the sitemap URL count and the map pin count are **identical** before and after (numbers in the PR).
- A new unverified company with an Active vacancy is invisible on every public channel listed in 02.5, and visible within ≤ 60 s after its status becomes Verified (test flips the status directly).

Done → next: `03-beperkingen-niet-geverifieerd.md`.
