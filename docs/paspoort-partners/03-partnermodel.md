# 03: Partner model: PassportPartner, codes, candidate links with consent, access log

**Stacked.**
- Branch: `cursor/paspoort-partners-3` from `cursor/paspoort-partners-2`.
- ONE PR into `acceptatie`, titled **"feat(paspoort-partners): partner model, partner codes, consented candidate links, access log"**.
- Rules: see README.
- Everything is gated by `PassportPartnersEnabled` (default OFF).

Mockups (for the vocabulary; the UI comes later): `e-start-partnercode-toestemming.png`, `g-partneroverzicht.png`.

## Goal
Backend foundation for decisions 7–13:
- A staffing agency (Uitzendbureau) or an employer's HR (Werkgever) becomes a **paspoortpartner** with codes per branch.
- A candidate who starts with a code is linked to that partner.
- The partner can see the passport **only** after explicit, versioned, revocable consent.
- Consent is reconfirmed every 6 months, with at most 3 active partners per candidate.
- Every partner view is logged.

## Facts (checked on `85a43263`)
- `Company.Type` is `CompanyType.Employer | Intermediary` (`ProductEnums.cs` L39–43). That **is** the partner type: Werkgever / Uitzendbureau.
- Branches already exist: `Company.ParentCompanyId` / child companies. Users belong via `User.CompanyId` and `User.CompanyMemberships` (`UserCompany`, for Regional/Enterprise/Intermediary).
- Employer-side roles with **mandatory 2FA**: `MfaPolicy.IsRequired` covers BranchManager, RegionalManager, EnterpriseManager, Intermediary.
- `Company.LogoUrl` exists, but nothing uploads logos and no PDF loads them. Co-branding needs stored bytes.
- **School codes** (`Jobsy.Core/Scholen/PupilCodeFormat.cs`, `Jobsy.Infrastructure/Scholen/PupilCodeService.cs`):
  - 6 characters from `ABCDEFGHJKMNPQRSTUVWXYZ23456789`, displayed `XXX-XXX`
  - normalization strips spaces/dashes
  - lookup via `HMACSHA256` with config key `Scholen:CodeHmacKey` (dev key file fallback)
  - Data Protection for reprint
  - They are **per-pupil anonymous login codes without an account**: same format, different purpose. Re-use the format and lookup approach, **not** the entity.
- Candidate attribution pattern: `AmbassadeurAttributionService.TryAttributeCandidateAsync`, called from `AuthController` ~L455/L476/L789 with `ReferralCode`.
- Affiliate codes `PartnerAffiliateProfile` (`IM-`/`BM-`) are about **company** referrals. Leave them alone (see README naming).
- Consent pattern: `User.TalentPoolConsentAt` + version, and `PrivacyConstants.CandidateProfilingConsentVersion`.
- `CandidateConsentRules.TalentPoolMinimumAge = 18`.
- Account deletion: `PrivacyDataService.AnonymizeUserAsync` (~L868). Data export: `PrivacyDataService.ExportAsync` (~L38). Retention: `DataRetentionHostedService`.

## Scope

### Shared short-code format
- Extract `Jobsy.Core/Rules/ShortCodeFormat.cs` (alphabet, length 6, `Normalize`, `TryNormalize`, `IsWellFormed`, `Display`) from `PupilCodeFormat`.
- `PupilCodeFormat` delegates to it with **identical behaviour** (all school tests stay green; no school-code migration).

### Entities + migration `AddPassportPartners`

**`PassportPartner`**
- `Id`, `CompanyId` (unique FK, the partner's root/organisation company)
- `IsActive` (bool; step 08 drives it from subscriptions, here admin-set)
- `DisplayName` (max 80)
- `LogoPng` (bytea, null), `LogoContentType`, `LogoUpdatedAtUtc`
- `MaxBranches` (int, default 1)
- `TermsVersion` + `TermsAcceptedAtUtc` + `TermsAcceptedByUserId` (partner terms, see below)
- `CreatedAtUtc`, `UpdatedAtUtc`
- Type is **derived** from `Company.Type`: `PassportPartnerType.Uitzendbureau` for `Intermediary`, `Werkgever` for `Employer`. Expose a helper. Do not store a second copy.

**`PassportPartnerCode`**
- `Id`, `PassportPartnerId`
- `BranchCompanyId` (the company/branch the code belongs to; must be the root or a descendant)
- `CodeLookupHash` (HMAC-SHA256 hex, unique index)
- `CodeDisplay` (`XXX-XXX`). Codes are **public** (printed on flyers), so plaintext display is fine; lookups always go via the hash, as with school codes.
- `IsActive`, `CreatedAtUtc`, `DeactivatedAtUtc`
- HMAC key config: `PassportPartners:CodeHmacKey`. It is required outside Development when the flag is ON (startup check like `Scholen:CodeHmacKey`). Dev key file: `passport-partners-code-hmac.key`.
- Codes are generated randomly from the alphabet. Admins may set a vanity code **only** if it is well-formed (no I/L/O/0/1).

**`PassportPartnerCandidateLink`**
- `Id`, `CandidateUserId`, `PassportPartnerId`, `PartnerCodeId` (null)
- `Source`: `Code | AddedCode`
- `StartedAtUtc`
- `ConsentGivenAtUtc` (null), `ConsentVersion` (null)
- `ContactConsentAtUtc` (null): the optional "mag mij bellen/appen" box
- `ConsentPromptDismissedAtUtc` (null): the candidate chose "Nu niet" on the consent screen (step 06). The link stays unconsented, is never visible individually to the partner, and only counts in aggregates ≥ 5.
- `ReconfirmDueAtUtc` (null; set to consent + 6 months; reconfirm moves it +6 months)
- `SuspendedAtUtc` (null; set when the reconfirm date passes)
- `RevokedAtUtc` (null), `RevokedReason`: `Candidate | PartnerEnded | AccountDeleted | Admin`
- Unique index on `(CandidateUserId, PassportPartnerId)` (re-linking re-uses the row: clear revoke, require new consent).

**`PassportAccessLog`**
- `Id`, `CandidateUserId`, `PassportPartnerId` (null), `ViewerUserId` (null)
- `ShareLinkId` (null; FK added in step 05)
- `Kind`: `PartnerPortalView | PartnerPdfDownload | ShareLinkView | VerificationView`
- `OccurredAtUtc`
- No IP stored.

### Rules (`Jobsy.Core/Rules/PassportPartnerRules.cs`)
- `MaxActivePartners = 3`. **Active** = consent given, not revoked. Suspended links count as active (the candidate can reconfirm).
- `ReconfirmInterval = 6 months`, `ReconfirmReminderLead = 14 days`.
- `MinimumAge = 18`. Partner links and consent need age ≥ 18 by `User.DateOfBirth`. Candidates without a DOB are asked their age band ("Ik ben 18 of ouder") at consent time (step 06). See open point 2.
- `CountDisclosureThreshold = 5`: any count that includes not-consented links is shown as the exact number only when it is ≥ 5, otherwise as "minder dan 5" (0–4 look the same). Partner surfaces must not let a partner derive a sub-5 count by subtraction (see step 07). Same spirit as `SchoolAnonymity.MinGroupSize`.
- `PrivacyConstants.PartnerShareConsentVersion = "2026-10-03"`.
- `PassportPartnerTerms.CurrentVersion = "2026-10-03"`. The terms text must state:
  - only own candidates (own code + consent), no search in the Lobsy pool
  - only the passport, never test answers or raw scores
  - **Lobsy provides no score, ranking or automatic selection; the passport is conversation input and a human decides**
  - the partner is an independent controller for PDFs it downloads
  - 2FA required

### Service `IPassportPartnerService` (Infrastructure): the single authorization point
- `ResolveCodeAsync(code)` → partner summary (display name, type, branch label, has logo) or null when inactive or the flag is OFF.
- `AttachByCodeAsync(candidateId, code, source)` → creates/reuses the link **without consent**. Idempotent; a revoked link stays revoked until new consent.
- `GiveConsentAsync(candidateId, linkId, consentVersion, contactConsent, confirmAdult)`:
  - enforces max 3 (error key `Partners.Consent.Max`) and minimum age
  - stamps the version and sets the reconfirm date
- `RevokeAsync(candidateId, linkId, reason)`, `ReconfirmAsync(candidateId, linkId)`
- `CanPartnerViewAsync(partnerUserId, candidateId, mfaSatisfied)` → the link or null. **Only** true when all of these hold:
  - the flag is ON and the partner is active
  - the user is a member of the partner's company or a branch
  - the user's role is BranchManager / RegionalManager / EnterpriseManager / Intermediary
  - MFA is satisfied for this session. The caller passes `mfaSatisfied`, computed with the existing `AdminSessionClaims.IsMfaSatisfiedInSession` (`mfa_verified` = "1" or an external-IdP `auth_method`, same as `MfaPolicy`/`MfaEnforcementMiddleware`). The API side uses the equivalent check from `PersonalDataAccessLogExtensions`.
  - consent is given, not revoked, not suspended
  - the candidate is active and not anonymized
- `LogAccessAsync(...)`
- **Every** later partner-facing read (steps 05, 07) must go through `CanPartnerViewAsync`.

### Background job `PassportPartnerConsentJob` (daily)
- Sends the reconfirm reminder e-mail (new template `PartnerConsentReconfirmReminder`, 5 languages, link to `/candidate/paspoort/delen`) 14 days before the due date, once.
- On the due date: sets `SuspendedAtUtc`.
- Purges `PassportAccessLog` rows older than 12 months (or hooks into `DataRetentionHostedService`).

### Privacy hooks
- `PrivacyDataService.AnonymizeUserAsync`: delete the candidate's links and access-log rows (the account is gone, so its counts disappear too).
- `ExportAsync`: include the candidate's links (partner name, type, dates, status) and the access log (partner name, kind, date).

### API (all `[RequiresFeature(PlatformFeature.PassportPartners)]`)
- Public: `GET api/passport-partners/codes/{code}` (rate limit `public-read`) → `{ displayName, type, branchLabel, logoUrl }`, 404 when unknown or inactive.
- Public: `GET api/passport-partners/{id}/logo` (cached, `image/png|jpeg`).
- Candidate (`RequireCandidate`):
  - `GET api/me/passport-partners`
  - `POST api/me/passport-partners` `{ code }` (AddedCode)
  - `POST api/me/passport-partners/{linkId}/consent`
  - `POST …/{linkId}/revoke`
  - `POST …/{linkId}/reconfirm`
- **No** partner-portal endpoints yet (step 07).

### Admin (minimal, `RequireAdmin`)
New page `/admin/paspoortpartners`:
- list partners
- create a `PassportPartner` for an existing `Company` (shows the derived type)
- toggle `IsActive`
- upload a logo:
  - PNG/JPEG only, ≤ 512 KB, ≥ 200 px wide
  - re-encode to PNG server-side and strip metadata
  - no SVG
- generate or deactivate codes per branch (≤ `MaxBranches`, root counts as 1)
- print the code list PDF, re-using `SchoolCodeListPdfService` layout ideas (Lobsy styling)

## Tests
- **Unit:**
  - `ShortCodeFormat` (and `PupilCodeFormat` parity: same outputs for a corpus of inputs)
  - code generation never emits I/L/O/0/1
  - HMAC lookup round-trip
  - vanity code validation
  - `GiveConsentAsync`:
    - the 4th active link is rejected
    - a revoked link no longer counts
    - under 18 is rejected
    - the version is stamped
    - reconfirm moves +6 months
  - the job suspends on the due date and sends the reminder once
  - `CanPartnerViewAsync` truth table: flag off, inactive partner, wrong company, branch user OK, missing MFA claim, candidate role, no consent, suspended, revoked, anonymized candidate
  - anonymize deletes links and logs; export contains them
- **API tests:**
  - public code resolve: 404 when the flag is OFF, rate-limited
  - candidate endpoints scoped to the own user (cannot consent on someone else's link id)
- **bUnit:** admin page renders, and the logo upload rejects SVG and > 512 KB.

## Success criteria
- Partners, codes and links exist with consent, revoke, reconfirm and max-3 rules enforced in one service, and every rule is unit-tested.
- School codes behave exactly as before.
- Flag OFF → all new endpoints 404 `feature_disabled`.
- Release build with 0 warnings, full tests green.

## Out of scope
- The candidate start/consent UI (06).
- The portal (07).
- Subscriptions (08).
- Any talent-pool or affiliate-code change.
