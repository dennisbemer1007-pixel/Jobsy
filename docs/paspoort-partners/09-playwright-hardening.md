# 09: Playwright hardening across the passport and partner flows

**Stacked.**
- Branch: `cursor/paspoort-partners-9` from `cursor/paspoort-partners-8`.
- ONE PR into `acceptatie`, titled **"test(paspoort-partners): end-to-end hardening, privacy and AI-Act guards"**.
- Rules: see README.
- This step adds **tests, seed data and small fixes found by those tests**. No new features. A bug a test reveals is fixed in this PR when small (< ~100 lines). Otherwise stop with a draft PR and report it.

Mockups: all of a–h as visual reference for assertions on texts/sections. Mockup i (teams) is **not** tested or built.

## Goal
Prove end to end, at 390×844 and 1440×900, that the whole stack works and keeps its promises:
- consent first
- only own candidates
- only the passport
- no scores/rankings
- revocation works instantly
- share links expire
- counts respect the threshold
- all 5 languages render (ar rtl web)

## Facts
- Playwright tests live in `Jobsy.Tests/*PlaywrightTests.cs` with `[Collection("PlaywrightSmoke")]`, `PlaywrightCookieConsent` and soft-skip when `JOBSY_E2E_BASE_URL` is unset or unreachable.
- Existing env vars: `JOBSY_E2E_BASE_URL`, `JOBSY_E2E_CANDIDATE_EMAIL/PASSWORD`, `JOBSY_E2E_EMPLOYER_EMAIL/PASSWORD`, `JOBSY_E2E_ALLOW_SIGNUP`, `JOBSY_E2E_ALLOW_FEATURE_TOGGLE`, `JOBSY_E2E_MFA_*`.
- `.github/workflows/acceptatie-smoke.yml` passes the secrets.
- There is no axe-core in the repo. The a11y pattern is `AuthA11yPlaywrightTests.cs`: keyboard path, `dir`, overflow at 390 px and 200% zoom.
- Test accounts: `Jobsy.Infrastructure/Ops/TestAccountsSeedService.cs` (`IsTestAccount`/`IsTestData`). MFA is skipped for test accounts only while the acceptatie test-accounts runtime is active (`MfaPolicy.IsRequiredFor`).

## Scope

### Seed (extend `TestAccountsSeedService`, idempotent, all `IsTestData`)
- Partner company **"Testbureau Westland"** (`Company.Type = Intermediary`) with 2 branches.
  - Active `PassportPartner` with a logo (small generated PNG) and terms accepted.
  - Codes for root + branch 1 (fixed, valid alphabet, e.g. `TWB-7K3` / `TWB-9MQ`; vanity rules from 03).
  - Active pilot subscription.
- Partner company **"Testkweker Maasland"** (`Employer`) with 1 code. Used for the max-3 and "other partner" checks.
- Partner users:
  - EnterpriseManager of Testbureau
  - BranchManager of branch 1
  - EnterpriseManager of Testkweker
- Candidates:
  - **A:** consented to Testbureau via branch 1, share-ready, 4/4 tests, e-mail verified → Lobsy-geverifieerd
    - Also seed **AI output that must never reach partners** (decision 21), written directly to the DB with no LLM call:
      - a WhoAmI story + keywords containing `ZZ-AI-MARKER-WHOAMI`, with `IncludeOnCv = true`
      - a RoleFit result with band "Past goed" for a role whose title contains `ZZ-AI-MARKER-ROLEFIT`
      - competence/culture/values outcomes
      - a CV upload whose `FilledFieldsJson` contains `certificaten`, with one **unconfirmed** certificate named `ZZ-AI-MARKER-CV`
  - **B:** consented, concept (not share-ready)
  - **C:** link without consent ("Nu niet")
  - **D:** consented, `ReconfirmDueAtUtc` in the past → suspended
  - **E:** consented then revoked
  - 4 more unconsented links on Testbureau, so the not-shared count can toggle between 4 and 5 via one extra seeded link (toggle endpoint only in the test runtime)
- New env vars (document them in `docs/ONBOARDING.md` next to the existing `JOBSY_E2E_*` vars, and add them to `acceptatie-smoke.yml` as optional secrets):
  - `JOBSY_E2E_PARTNER_EMAIL` / `JOBSY_E2E_PARTNER_PASSWORD` (Testbureau EnterpriseManager)
  - `JOBSY_E2E_PARTNER_BRANCH_EMAIL` / `_PASSWORD`
  - `JOBSY_E2E_PARTNER_OTHER_EMAIL` / `_PASSWORD`
  - `JOBSY_E2E_PASSPORT_CANDIDATE_EMAIL` / `_PASSWORD` (candidate A)
  - `JOBSY_E2E_PARTNER_CODE`
- Tests soft-skip when the vars they need are missing.

### E2E flows (new files, `[Collection("PlaywrightSmoke")]`, each at 390×844 and 1440×900)
1. **`PassportPartnerOnboardingPlaywrightTests`** (needs `JOBSY_E2E_ALLOW_SIGNUP`):
   - `/p/{code}` → language pl → register → consent (both boxes) → first test
   - partner (branch user) sees the new candidate in Kandidaten
   - decline path: candidate not visible
2. **`PassportPartnerRevokePlaywrightTests`:**
   - candidate A opens Delen & toegang → revoke Testbureau
   - partner refreshes → A gone; direct URL to A's passport → 403 page
   - re-add via code + consent → visible again; restore the seed state at the end
3. **`PassportShareLinkPlaywrightTests`:**
   - candidate creates a 30-day link (default preselected) → anonymous context opens `/v/{id}?s=` → live view with name
   - revoke → private view with the access-request form
   - expired link (seeded past expiry) → private
   - `/v/{id}` without token → authenticity only, no name text present
4. **`PassportPdfV2Playwright`:**
   - candidate downloads the PDF (pl+nl, p2, no contact) → `application/pdf`, file name pattern
   - partner downloads the co-branded PDF
   - download a third PDF and extract its text with PdfPig in-test; assert there is no "%" / "score" / date-of-birth pattern / "Huisvesting"
5. **`PartnerPortalPrivacyPlaywrightTests`:**
   - the table shows A, B, D (paused, not openable) and never C or E
   - no search input, no export button, only status/branch filter, only date sort
   - not-shared KPI shows "minder dan 5" at 4 and "5" after the toggle
   - no total that allows subtraction
   - other partner user cannot open A (403)
   - branch user does not see candidates of other branches
6. **`PassportReconfirmPlaywrightTests`:**
   - candidate D sees the "Gepauzeerd" banner → reconfirm → partner can open D again; reset the seed
7. **`PartnerPricingPlaywrightTests`:**
   - `/voor-partners` type switch keeps plans identical
   - amounts match admin settings (read via an admin-free public DTO)
   - pilot banner remaining slots
   - lead form submit in the test env
8. **`PassportPartnersRtlA11yPlaywrightTests`:**
   - for `/p/{code}`, `/v/{id}`, `/candidate/paspoort/delen`, `/partnerportaal`, `/voor-partners` in `ar`: `dir="rtl"`, no horizontal overflow at 390 and 1440, 200% zoom without overflow, keyboard-reachable primary CTA, visible focus
   - the same pages in nl/en/pl/ro render translated titles (no raw keys like `Partners.`/`Passport.`)
9. **`PassportPartnersSecurityHeadersPlaywrightTests`:**
   - `/v/*` and `/p/*` send `X-Robots-Tag: noindex`, `Cache-Control: no-store` (for `/v`), `Referrer-Policy: no-referrer` (for `/v`) and the strict CSP
   - the token is removed from the URL after load
   - with the flag OFF (only if `JOBSY_E2E_ALLOW_FEATURE_TOGGLE`): `/p`, `/partnerportaal`, `/voor-partners` → 404
10. **MFA (`JOBSY_E2E_MFA_*` pattern):** a non-test partner user without MFA is redirected to login with `mfa-required` when opening `/partnerportaal`. Skip when the vars are absent.
11. **`PartnerViewNoAiOutputPlaywrightTests`** (decision 21; rationale `ai-act-beoordeling.md` §4.4). Covered partner views, for candidate A:
    - the share-link live view `/v/{id}?s=`
    - the partner portal table + passport view
    - the co-branded PDF from the portal
    - the candidate's own PDF v2 download (PDF text via PdfPig)

    In all of them, assert:
    - **no** marker `ZZ-AI-MARKER-*`
    - none of the texts "Past goed", "Past redelijk", "Wie ben ik", "Stressbestendigheid", "Holland", "%", "match", "score"
    - no element whose class or `data-testid` contains `dna-`, `rolefit`, `whoami`, `tips`, `strength`
    - the test block shows only done/not done

    Then have candidate A confirm the certificate in the Data tab. `ZZ-AI-MARKER-CV` then **does** appear in the live view and PDF, which proves the confirmation path works. Reset the seed at the end.
12. **Employers OFF (hotfix 01b):** the whole run happens with `EmployersEnabled` = false (the acceptatie default after 01b). Assert once that:
    - `GET api/settings/feature-flags` reports `employersEnabled: false`
    - the partner flows above still pass
    - the partner EnterpriseManager lands on `/partnerportaal`, not on `employers-off`


### Unit/integration guards (collect and complete; add any missing)
- **Privacy guard:** `PassportDocumentModel` whitelist + "never" list (04); portal DTO reflection guard (07); share-link live model respects Show* (05).
- **No-AI guard (decision 21):** a source scan fails if the passport model factory, the PDF renderer, the `/v` live-view components or the portal components/controllers reference any of these:
  - `IWhoAmIService`, `CandidateWhoAmIProfile`, `WhoAmISnapshot`
  - `RoleFit*`, `CultureFit*`, `ProfileVacancyMatchCalculator`
  - `ITranslationService` (allowed **only** in the translation-approval service)
  - outcome properties of the four test entities (only `CompletedAtUtc` is allowed)
  - CV-extracted fields without the confirmation filter
- **AI-Act guard:** a source scan fails if files under `Jobsy.Web/Components/PartnerPortal/**`, the portal API controllers and partner DTOs contain `OrderBy` on anything other than shared/available dates or the identifiers `Score|Percent|Fit|Rank|Match`. Allow-list comments are not allowed.
- **Threshold guard:** a single `PassportPartnerCounts.Disclose(int)` helper is the only way the portal renders not-shared counts (source scan).
- **i18n:** `LocalizationParityReportTests` green for all new keys in nl/en/pl/ro/ar; `PassportPdfStrings` parity.
- **CSP:** routes registered in the CSP test list.

### CI
- Add the new Playwright files to the existing smoke selection (same trait/collection), so `acceptatie-smoke.yml` runs them when secrets exist.
- Artifacts (screenshots on failure) go to `artifacts/paspoort-partners-playwright/`.

## Success criteria
- All new E2E tests pass against acceptatie with the seed (or soft-skip without env). Guards are in place.
- Any bug found is fixed (small) or reported (large) with a draft PR.
- Full `dotnet test -c Release` green; Release build with 0 warnings.
- The report lists which E2E tests ran vs skipped.

## Out of scope
- New features.
- Visual regression tooling.
- axe-core introduction (separate decision).
- Load tests.
- Lobsy voor teams.
- Talent pool.
- Pro matching.
