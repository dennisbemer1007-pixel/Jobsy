# 01. Standalone hotfix: legal identity from config (no placeholders), /{kvk} only for verified companies with vacancies, partner noindex, %0A share mails, real 404

Read `00-README.md` first ("How to run", §0, §IA, D1/D2/D4). Branch `cursor/public-hotfix` from `origin/acceptatie`. **Standalone: not stacked on anything.**

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/public-hotfix` from `origin/acceptatie`. ONE PR into `acceptatie`; the body starts with `Standalone hotfix (not stacked)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/public-hotfix`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start file 02.
> - Never show `ex.Message`, placeholders like `[ADRES]` or internal ids to visitors.
> - Don't redesign pages here. Keep today's layouts and markup. Change only what this file names; the redesign is 02+ (and `docs/errors`).

| | |
|---|---|
| Branch | `cursor/public-hotfix` |
| PR title | `fix(legal,privacy,seo): legal identity from Legal:* config (no placeholders, also mail footer); /{kvk} only for verified companies with public vacancies, no ids/coords; partner code pages noindex; %0A share mails; real 404 page + 404 for unknown vacancies` |
| PR body starts with | `Standalone hotfix (not stacked)` |
| Mockups | none needed (today's layout). `pb-d07` notes 1–3 describe the `/{kvk}` data rules |
| Migration | none |
| Split seam | if > ~1.500 lines: **01a** = (a) legal identity + (c) + (d) + (e); **01b** = (b) `/{kvk}` on `cursor/public-hotfix-b`, stacked on 01a, body "Stacked on #<01a> (standalone hotfix part 2)" |

## Goal
- lobsy.nl never shows `[BEDRIJFSNAAM]`, `[KVK-NUMMER]`, `[ADRES]` or `[CONTACT E-MAIL PRIVACY]` again, and Lobsy's identity lives in one place.
- Nobody can get a public branded page by registering under someone else's KvK number, and the company page leaks no home addresses, coordinates or internal ids.
- Crawlers stop indexing duplicate partner pages, soft-404 vacancies and blank 404s.

## 01.1 Today (verify first)
- `Jobsy.Core/Privacy/PlatformLegalIdentity.cs` L9–12: four placeholder constants. Used in `Pages/Legal/Privacy.razor` L29–31 and L109 (text + `mailto:`), and in the default HTML of `Infrastructure/Services/AboutPageSettingsService.cs` L52. `Jobsy.Tests/CandidateConsentRulesTests.cs` L52–58 (`Platform_legal_identity_uses_placeholders`) **asserts** the placeholders. Live on lobsy.nl/privacy on 30-09.
- An admin "Bedrijfsgegevens" singleton already exists: `PlatformCompanySettings` (`CompanyName`, `Address`, `PostalCode`, `City`, `Country`, `KvkNumber`, `VatNumber`, `Email`, …) via `IPlatformCompanySettingsService`, page `/admin/company` (`CompanySettingsAdmin.razor`), used on invoices/PDFs.
- Mail footer: `Jobsy.Core/Email/EmailLayout.cs` `Wrap` (~L110–185) ends with "Je ontvangt deze e-mail omdat…" and has no legal line. Sending: `SmtpEmailService` (Resend + SMTP; `DependencyInjection.cs` L260) and `EmailServiceStub`.
- `/{kvk}`:
  - `Jobsy.Api/Controllers/PublicCompaniesController.cs`: `QueryPublicRows` (L138) filters **only** `c.KvkNumber == kvk`. No `KvkVerificationStatus` (`Jobsy.Core/Enums/KvkVerificationStatus.cs`: Verified/Pending/Failed) and no vacancy check.
  - The DTO returns `Address`, `Latitude`, `Longitude` per branch and `CompanyIds` (GUIDs).
  - `Pages/CompanyPublicPage.razor` (448 lines) uses `_page.CompanyIds` for `Api.DiscoverVacanciesAsync(companyIds:)` (L264–270) and `jobMap.focusCompany(guid)` (L327–329), shows `_page.Address` (L54–56) and `branch.Address` (L112). `MarkNotFound()` L295–302 already sets 404 in prerender.
  - JSON-LD `StructuredData.Organization(origin, name, address, …)` with `Navigation.BaseUri` (L199–212).
  - Sitemap: `SiteController` crawl-index L59–80 builds company paths from every publicly visible vacancy's KvK, without the verification check; `Seo/SeoEndpoints.cs` L95–120 adds them.
  - Pending companies come from registration while the KvK API was down, and from ATS auto-creation (`AtsVacancyModerationService` ~L364).
- `/partner/{code}`: `Seo/PageSeoCatalog.cs` L220 `("/partner/", Public(...))` → indexable. `Pages/Partner/PartnerSales.razor` `@page "/partner"` + `"/partner/{TrackingCode?}"`.
- `%0A`: `Uri.EscapeDataString($"Hoi,%0A%0A…")` in `Pages/Partner/PartnerSales.razor` L187–190, `Pages/Employer/Tokens.razor` L645–650, `Pages/SalesManager/SalesToolkit.razor` L148–153 → the mail body shows a literal "%0A".
- 404: no `UseStatusCodePages*` (`Jobsy.Web/Program.cs` L150–200), Router without `NotFound` (`Components/Routes.razor`). Live `/bestaat-niet` = 404 with an empty body.
- Unknown vacancy: API `VacanciesController.GetById` L562+ returns 404 (or the non-public branch for owners). `Pages/VacancyDetail.razor` shows `Vacancy.NotFound` (L45) with **status 200** and the default `PageSeo` (indexable).

## 01.2 (a) Legal identity from config
### 01.2.1 Options and service (API side)
- `Jobsy.Core/Options/LegalOptions.cs`, section `Legal`:
  - `Name` (registered name, e.g. "… B.V." or the eenmanszaak name), `TradeName` (default "Lobsy")
  - `Street`, `PostalCode`, `City`, `Country` (default "Nederland")
  - `KvkNumber`, `VatNumber`
  - `PrivacyEmail` (default empty), `SupportEmail` (default `support@lobsy.nl`), `SchoolsEmail` (default empty)
  - All strings trimmed; empty = not set.
- `Jobsy.Core/Interfaces/ILegalIdentity.cs` → `Task<LegalIdentitySnapshot> GetAsync(CancellationToken)`, implemented in `Jobsy.Infrastructure/Services/LegalIdentityService.cs`.
  - Per field: `Legal:*` value, else the matching `PlatformCompanySettings` value (`CompanyName` → `Name` only when it isn't the default "Lobsy"; `Address` → `Street`; `Email` → neither: it's the invoice mailbox), else empty.
  - Cached 5 minutes (`IMemoryCache`). `IPlatformCompanySettingsService.UpdateAsync` evicts the cache.
  - `LegalIdentitySnapshot` has helpers:
    - `DisplayName` = `Name` if set, else `TradeName`
    - `AddressLine` = "Street, PostalCode City" from the non-empty parts
    - `PrivacyContact` = `PrivacyEmail` if set, else `SupportEmail`
    - `FooterLine` = "{DisplayName} · {AddressLine} · KvK {KvkNumber} · btw {VatNumber}", each part only when set
  - At startup (hosted `IStartupFilter` or the first `GetAsync`): if a `Legal:*` value and the Bedrijfsgegevens value are both set and differ, log **one** warning `legal.identity.mismatch` with the field names (no values).
- `appsettings.json` (API): add the `Legal` section with the defaults above (empty strings). No real values in the repo.
- **Don't edit `render.yaml`.** In the PR body, list the env vars Dennis sets on the Render API service: `Legal__Name`, `Legal__Street`, `Legal__PostalCode`, `Legal__City`, `Legal__KvkNumber`, `Legal__VatNumber`, `Legal__PrivacyEmail` (optional).

### 01.2.2 Endpoint + Web client
- `GET api/site/legal` in `SiteController` (`[AllowAnonymous]`, `public-read`) → `LegalIdentityDto` (§IA). Omit null/empty properties (`JsonIgnoreCondition.WhenWritingNull`). `Cache-Control: public, max-age=300`.
- Web: `JobsyApiClient.GetLegalIdentityAsync()` plus `Jobsy.Web/Services/LegalIdentityProvider.cs` (singleton, 5-minute memory cache, last good value on API failure, else an "empty" DTO with only `TradeName` "Lobsy" and `SupportEmail`). Pages inject `LegalIdentityProvider`.

### 01.2.3 Pages (today's markup, values only)
- `Privacy.razor` L29–31: render "**Verwerkingsverantwoordelijke:** {DisplayName}{, KvK x}{, AddressLine}." with each part only when set. Contact: "Vragen over privacy: {PrivacyContact}" as a `mailto:` link. Same at L109. Never render `mailto:` with an empty address.
- `AboutPageSettingsService` default HTML L52: replace the `PlatformLegalIdentity.PrivacyEmail` interpolation with the literal `support@lobsy.nl` (the default text is only used when the DB row is empty; 08 removes it).
- Delete `PlatformLegalIdentity.cs`. `git grep -n "PlatformLegalIdentity"` must be empty afterwards. If `docs/landing` added `PlatformLegalIdentity.SchoolsEmail` in the meantime, map it to `LegalOptions.SchoolsEmail`.

### 01.2.4 Mail footer
- `EmailLayout.Wrap` emits one marker `<!--lobsy:legal-footer-->` as the last element inside the footer cell (after the "Je ontvangt…" line).
- `SmtpEmailService` (both the Resend and the SMTP path) and `EmailServiceStub` replace the marker right before sending with `<p style="margin:8px 0 0;font-size:11px;color:{Muted};">{Escape(FooterLine)}</p>`, or with an empty string when `FooterLine` is only the trade name. `ILegalIdentity` is injected. The plain-text alternative (if any) gets the same line.
- If `docs/emails` 02 already landed (README Dependency B present), skip the marker: fill `MailOptions.LegalName/LegalAddress/KvkNumber` from `ILegalIdentity` instead and say so in the PR.

## 01.3 (b) `/{kvk}`: verified + public vacancy, city only, no ids
### 01.3.1 One query
- `Jobsy.Infrastructure/Services/PublicCompanyQuery.cs` (or a private helper in the controller if that's the codebase pattern), used by the controller **and** the crawl index:
  - companies with `KvkNumber == kvk` **and** `KvkVerificationStatus == Verified` **and** not deleted/inactive (reuse whatever flag the admin "Bedrijven" list uses for inactive)
  - that have ≥ 1 vacancy where `VacancyVisibilityRules.IsPubliclyVisible(record, today)` is true (same rule as discovery; use `VacancyDiscoveryRecord` if it's cheaper)
  - per company: `Name`, `City` (from the structured address if it exists, else parse the last token after the postcode with the existing address helper; if no reliable city, null), `LogoUrl`, `KvkEstablishmentId`, `ParentCompanyId`, public vacancy count
- Unverified or failed companies, or companies without public vacancies → the API answers **404** with `{ code: "not_found" }` (no Dutch message, no hint why).

### 01.3.2 DTOs (breaking change inside our own Web only)
- `PublicCompanyPageDto { Kvk, Vestigingsnummer?, Name, City?, LogoUrl?, Branches? }`. **No** `Address`, `Latitude`, `Longitude`, `CompanyIds`.
- `PublicCompanyBranchDto { Name, City?, Vestigingsnummer?, Path, VacancyCount }`. **No** `CompanyId`, address or coordinates.
- New `GET api/public/companies/{kvk}/vacancies` and `…/{kvk}/{vestiging}/vacancies` → the same item shape the discovery endpoint returns for the banenkaart (`VacancyListItemDto` or its list variant), filtered on those companies. It reuses the discovery projection (no new projection) and caps at 100. Vacancy coordinates are already public on the banenkaart; that's fine.

### 01.3.3 Web page (today's markup)
- `CompanyPublicPage.razor` loads the page DTO, then the vacancies from the new endpoint (no `companyIds`).
- It shows `City` instead of `Address` (L54–56, L112).
- Map focus: replace `jobMap.focusCompany(guid)` with the existing "fit to pins" call used after loading pins (check `jobMap*.js` for the function name; don't change `jobMap*.js`). If no such call exists, skip focusing and leave a TODO for 09.
- JSON-LD: `StructuredData.Organization(origin, name, address: City, logo, path)`. `origin` comes from `PageSeoResolver.Origin`/config (the same origin the sitemap uses), not `Navigation.BaseUri`.
- API 404 → `MarkNotFound()` (status 404, `PageSeo Index=false`).

### 01.3.4 Sitemap
- `SiteController` crawl-index L59–80 builds `CompanyPaths` from `PublicCompanyQuery` (verified + public vacancy), and adds vestiging paths only for branches with a public vacancy.

## 01.4 (c) Partner code pages
- `PageSeoCatalog` L220: `/partner/` prefix → `Public(..., index: false)` (use the existing overload or add `NoIndex(...)`; don't change other entries).
- `PartnerSales.razor`: `<PageSeo CanonicalPath="/partner" Index="@(string.IsNullOrEmpty(TrackingCode))" />`.

## 01.5 (d) `%0A` in share mails
- In the 3 places, build the body with real newlines (`"Hoi,\n\n…\n{url}\n\n"`) and escape **once** with `Uri.EscapeDataString`. Keep the texts otherwise unchanged (09 rewrites the partner copy).
- Add one helper `Jobsy.Web/Services/MailtoLink.cs` `Build(subject, body)` and use it in all 3 places.

## 01.6 (e) Real 404
- `Program.cs` (Web), after `UseExceptionHandler`: `app.UseStatusCodePagesWithReExecute("/status/{0}")`, **only** for requests that accept `text/html` and whose path doesn't start with `/api`, `/_blazor`, `/_framework`, `/_content`, `/healthz`, or have a file extension. Use `UseWhen` or a small `StatusCodePagesOptions` handler; `IStatusCodePagesFeature` stays enabled for the rest.
- `Pages/Status/StatusPage.razor`:
  - `@page "/status/{Code:int}"`, `[AllowAnonymous]`, `[ExcludeFromInteractiveRouting]`, today's simple `login-page`/`login-card` markup, `PageSeo Index=false`
  - 404: h1 `Status.NotFound.Title` "Deze pagina bestaat niet", lead `Status.NotFound.Lead` "Misschien is de link oud of zit er een typfout in.", links "Naar de banenkaart" (`/`) and "Hoe werkt Lobsy?" (`/hoe-werkt-lobsy`)
  - any other code: a generic "Er ging iets mis" + `/` link
  - The response keeps the original status (re-execute does that; don't set 200). A direct GET `/status/404` also answers 404.
  - Keys in a new module `Localization/UiStringsStatus.cs` (prefix `Status.`, registered in `UiStrings.cs`; 5 languages; pl/ro/ar drafts listed in `docs/i18n/public-pages-review.md`). `docs/errors` extends the same module.
- `VacancyDetail.razor`: when the API returns 404 (unknown or not public for this viewer), set `Response.StatusCode = 404` during prerender (same pattern as `CompanyPublicPage.MarkNotFound`) and render `PageSeo Index=false`. The visible text stays `Vacancy.NotFound`. `docs/errors` 03 adds the 410 page for closed vacancies; this file doesn't distinguish closed vs unknown.
- `docs/errors` 01 later replaces the page design and adds the support code. Keep the route and class names so that file can restyle in place.

## 01.7 Tests
- `LegalIdentityServiceTests`: config wins; empty config falls back to Bedrijfsgegevens; both empty → `FooterLine` = trade name only; `PrivacyContact` falls back to `SupportEmail`; the mismatch warning is logged once without values.
- `SiteLegalEndpointTests`: anonymous 200, empty fields omitted, cache header.
- `PrivacyPageLegalTests` (bUnit or HTML render): with empty config the page contains no `[`…`]` placeholder, no `mailto:"` with an empty address, and no "KvK" label; with values set it shows them.
- **Replace** `CandidateConsentRulesTests.Platform_legal_identity_uses_placeholders` with `No_legal_placeholders_in_source`: a source scan of `Jobsy.Web`, `Jobsy.Core`, `Jobsy.Infrastructure` finds none of `[BEDRIJFSNAAM]`, `[KVK-NUMMER]`, `[ADRES]`, `[CONTACT E-MAIL PRIVACY]`, and no `PlatformLegalIdentity`.
- `EmailLegalFooterTests`: the marker is replaced with the escaped line (`<b>` in the name renders as text); with an empty identity the marker is removed; no marker ever reaches the provider.
- `PublicCompaniesVisibilityTests` (API, in-memory/Testcontainers as the suite does):
  - Verified + 1 public vacancy → 200 (no `companyIds`, `address`, `latitude`/`longitude` properties in the JSON)
  - Pending → 404; Failed → 404
  - Verified without public vacancies → 404; only an expired/draft vacancy → 404
  - the vestiging route follows the same rules
  - `/vacancies` returns only public vacancies of that KvK
- `CrawlIndexCompanyPathsTests`: Pending/Failed companies are absent, even with a public vacancy.
- `PartnerSeoTests`: `/partner/ABC123` renders `noindex` and `<link rel="canonical" href="…/partner">`; `/partner` stays index.
- `MailtoLinkTests`: the body contains `%0A` exactly where the newlines are and never `%250A`.
- `StatusPageTests`: GET `/bestaat-niet` (Accept text/html) → 404 with the status page HTML and `noindex`; `/api/unknown` → unchanged API 404 (no HTML); `/css/missing.css` → 404 without HTML; unknown vacancy `/vacancies/{newGuid}` → 404 + `noindex`.
- `RoutesDocFreshnessTests`, `PageSeoTests`, `BlazorPageRoleAttributesTests`, `LocalizationParityReportTests` stay green.

## Success criteria
- `git grep -n "PlatformLegalIdentity\|\[BEDRIJFSNAAM\]\|\[ADRES\]\|\[KVK-NUMMER\]\|CONTACT E-MAIL PRIVACY"` is empty.
- `GET api/public/companies/{kvk}` for a Pending or Failed company, or one without public vacancies, is 404. No public company DTO contains a GUID, street address or coordinates.
- `/bestaat-niet` and `/vacancies/{unknown}` return 404 with an HTML page and `noindex`.
- `/partner/{code}` is `noindex` with canonical `/partner`.
- PR body lists: the new endpoints, the `Legal__*` env vars Dennis sets on Render, the behaviour change for existing Pending companies (their page disappears until verified) and the follow-up for `docs/errors` 01 (restyle `/status/{code}`).

Done → next (only when running the full stack): `02-juridisch-fundament.md`.
