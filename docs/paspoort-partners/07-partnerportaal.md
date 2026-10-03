# 07: Partner portal `/partnerportaal`

**Stacked.**
- Branch: `cursor/paspoort-partners-7` from `cursor/paspoort-partners-6`.
- ONE PR into `acceptatie`, titled **"feat(partners): partner portal (own candidates, passport only, 2FA)"**.
- Rules: see README.
- Gated by `PassportPartnersEnabled`. Flag OFF → 404, and the nav item is hidden.

Mockup: `docs/mockups/paspoort-partners/g-partneroverzicht.png` (src `src/g.html`).
- Deviations (the spec wins):
  - no percentages, completeness bars or "fit" anywhere
  - the table has no score/"match" column
  - sorting is by date only; filtering by status only
  - housing is not shown

## Goal
Decisions 14–16: agencies and employers that became a passport partner get a small, privacy-first workspace:
- see **only their own candidates** (who came via their code and gave consent)
- see **only the passport** (HTML + co-branded PDF)
- manage their code, flyers, logo and branches

No search in the Lobsy pool. No scores, rankings, filters on traits or automatic selection (AI Act). 2FA is required.

## Facts
- Roles: `UserRole.BranchManager/RegionalManager/EnterpriseManager/Intermediary`.
- Membership via `UserCompany` (`User.CompanyMemberships`) + `Company.ParentCompanyId` tree.
- MFA:
  - `MfaPolicy.IsRequired` already covers these roles for local passwords
  - `MfaEnforcementMiddleware` redirects non-MFA sessions
  - `AdminSessionClaims.IsMfaSatisfiedInSession` = `mfa_verified` or an external IdP (open point: external IdP skips Lobsy 2FA, same as today)
- Existing affiliate flyer: `PartnerFlyerPdfService` (IM/BM tracking codes) + `PartnerFlyerEndpoints` + rate limit `partner-flyer`. **Do not change it.** Build a separate passport-partner flyer.
- Renderer and model from 04. `CanPartnerViewAsync` / `LogAccessAsync` from 03.

## Scope

### Authorization
- New policy `JobsyPolicies.RequirePassportPartner`. It requires:
  - role ∈ {BranchManager, RegionalManager, EnterpriseManager, Intermediary}
  - membership in a company that has an **active** `PassportPartner` (root or descendant)
  - MFA satisfied
- **Scope per role:**
  - EnterpriseManager / Intermediary (root): all branches of the partner
  - RegionalManager: branches in their regions (existing `RegionCompany`)
  - BranchManager: links whose `PartnerCode.BranchCompanyId` is their branch
  - Implement it as one `PassportPartnerScope` query helper, covered by unit tests.
- **Every** candidate read goes through `CanPartnerViewAsync` + scope. Every passport view/PDF writes `PassportAccessLog` (`PartnerPortalView` / `PartnerPdfDownload`).
- Partner terms (03): the first visit requires accepting the current `PassportPartnerTerms` version (root manager only). Until then, only the terms page is shown.

### Pages (Blazor, new layout section "Partnerportaal" in the employer nav)
1. **Overzicht:**
   - consented candidates (exact)
   - new consents this month (exact)
   - reconfirm due within 30 days (exact)
   - share-ready passports (exact, among consented)
   - "Via je code gestart, (nog) niet gedeeld" → exact only if ≥ 5, else "minder dan 5" (`CountDisclosureThreshold`)
   - **Anti-subtraction:**
     - never show a "total started" number next to the consented count when the not-shared count is < 5
     - per-branch and per-month breakdowns of not-shared counts follow the same rule per cell
   - Short explainer "Waarom zien we niet iedereen?".
2. **Kandidaten:**
   - Table of **consented, not revoked** links in scope. Suspended links are listed as "Gepauzeerd" and cannot be opened.
   - Columns:
     - first name + last-name initial
     - passport status: share-ready / concept / Lobsy-geverifieerd, as text badges, not scores
     - shared since
     - available from
     - languages (codes)
     - candidate-chosen sectors (labels, in candidate order)
     - branch
     - contact allowed (yes/no)
   - **Sort:** shared date (default, newest first) or available-from. **Filter:** status (all/share-ready/verified/paused) and branch.
   - **No** free-text search, no filtering on sectors, languages, traits or tests, no CSV/Excel export, no bulk actions. Paging 25.
3. **Kandidaat → paspoort:**
   - the HTML passport view (same component as the `/v` live view from 05, partner variant with ribbon)
   - "Download PDF": 04 renderer with `PassportPartnerId`. The partner picks nl/en as primary; the candidate language becomes secondary when different. Contact is shown only if `ContactConsentAtUtc`.
   - The download writes a `PassportDocument` row.
   - Notice: "Gespreksinput, geen beoordeling. Jij beslist, Lobsy rangschikt niet." plus own-controller notice for downloaded PDFs.
4. **Code & flyers:**
   - per-branch codes (`CodeDisplay`), QR PNG download (QRCoder, `/p/{code}`), copy link
   - **flyer PDF** via the new `PassportPartnerFlyerPdfService`: A4 + A5, languages nl/en/pl/ro, partner logo, QR, 3 benefits, privacy line, "gratis voor kandidaten"
   - rate limit `partner-flyer` (reuse the policy name)
   - codes are created/deactivated by the root manager within `MaxBranches`
5. **Branding:**
   - logo upload (PNG/JPEG ≤ 512 KB, re-encoded server-side to PNG, max 600×200, no SVG; same validator as the 03 admin page)
   - display name (max 80; admin can lock it)
   - preview of the co-branded p1 header
6. **Vestigingen:** list of branches with code + consented count; add branch codes up to `MaxBranches` (more → "neem contact op", 08).

### API (Api project, `[Authorize(Policy = RequirePassportPartner)]`, `[RequiresFeature(PassportPartners)]`)
- `GET api/partner-portal/overview`
- `GET api/partner-portal/candidates?sort=shared|available&status=&branchId=&page=`
- `GET api/partner-portal/candidates/{linkId}/passport` (HTML model)
- `GET api/partner-portal/candidates/{linkId}/passport.pdf?lang=nl|en`
- `GET/POST/DELETE api/partner-portal/codes`
- `GET api/partner-portal/codes/{id}/qr.png`
- `GET api/partner-portal/codes/{id}/flyer.pdf?lang=&format=a4|a5`
- `PUT api/partner-portal/branding`
- `POST api/partner-portal/terms/accept`

Candidates are addressed by `linkId` (never by user id) in all routes.

**DTO guard (reflection test):** no property on any `PartnerPortal*` DTO may be named or contain `Score`, `Percent`, `Fit`, `Rank`, `Match`, `Riasec`, `Holland`, `DateOfBirth`, `Age`, `Nationality`, `Address`, `Postcode`, or be numeric except counts/ids/page fields on whitelisted members.

## Tests
- **Unit:**
  - scope matrix (enterprise / regional / branch / other partner / non-member / candidate role)
  - threshold rendering 0, 4, 5, 12 + anti-subtraction (no total when the not-shared count < 5)
  - suspended rows not openable
  - sort/filter whitelist (unknown sort → default)
  - flyer service renders 4 languages, contains the code + QR
  - logo validator (PNG/JPEG ok, SVG/oversize rejected, re-encode)
- **API:**
  - 401 without MFA
  - 403 for another partner's `linkId`
  - 404 when the flag is OFF
  - revoked link → 403 + no log row
  - PDF includes the co-brand ribbon and no contact block when there is no contact consent
  - access log rows written
- **DTO reflection guard.**
- **bUnit:** overview cards; table has no search input / export button; status filter only.
- **Playwright 390 + 1440** (soft-skip, seeded partner from 09):
  - login with partner test account → overview → candidates → open passport → download PDF
  - flyer download
  - no horizontal overflow
  - `ar` UI renders rtl without breaking

## Success criteria
- A partner user with 2FA sees only consented candidates in their scope, only their passport.
- No scores, rankings or trait filters. Counts that include non-consenting candidates respect the threshold, with no subtraction leak.
- Codes, QR, flyers, logo and branches work. Every view/download is logged.
- Release build with 0 warnings, tests green.

## Out of scope
- Pricing/subscriptions (08); `IsActive` stays admin-set here.
- Messaging candidates from the portal.
- Vacancies/matching for partners (Pro, later; text only in 08).
- Talent-pool access (decided separately).
- Lobsy voor teams.
- Changes to the affiliate partner (`/partner`, IM-/BM- codes).
