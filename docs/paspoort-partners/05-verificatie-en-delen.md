# 05: Live verification page `/v/{id}`, share links (30 days) and the candidate sharing screen

**Stacked.**
- Branch: `cursor/paspoort-partners-5` from `cursor/paspoort-partners-4`.
- ONE PR into `acceptatie`, titled **"feat(paspoort): live verification page, share links and sharing screen"**.
- Rules: see README.
- Gated by `PassportPdfV2Enabled`.
- The partner rows on the sharing screen need `PassportPartnersEnabled` as well.

Mockup: `docs/mockups/paspoort-partners/d-verificatiepagina-en-delen.png` (src `src/d.html`). It has three panels:
1. `/v/…` with access (live view)
2. `/v/…` without access (authenticity + access request)
3. the candidate's "Delen & toegang" screen

## Goal
Decisions 7–9:
- The QR on the PDF leads to an **always-truthful** page.
- Anyone can see that the passport is real and when it was generated.
- Only people the candidate approved see the **live** content:
  - a share link (default 30 days)
  - a consenting partner (03)
  - an approved access request
- The candidate has one screen to see and revoke everything.

## Facts
- `OneTimeLink` + `OneTimeLinkService`: opaque token, `VerificationCodes.Hash` (peppered) stored, never the plaintext. Copy this pattern.
- Security headers/CSP: `Jobsy.Web/Security/JobsyContentSecurityPolicy.cs`, `SecurityHeadersMiddleware.cs`. Noindex helpers: `ErrorResponse.NoIndex`, `X-Robots-Tag` usage in `LanguageEndpoints.cs`.
- Rate limits `public-read`, `access-request` exist.
- E-mail: `IEmailService` + `TransactionalEmails*.cs` + `EmailTemplateRegistry` (localized).

## Data (migration `AddPassportSharing`)
**`PassportShareLink`:**
- `Id`, `CandidateUserId`
- `TokenHash` (peppered, unique)
- `Label` (max 60, e.g. "Bureau X, Westland")
- `CreatedAtUtc`, `ExpiresAtUtc` (7/30/90 days; **default 30**), `RevokedAtUtc`
- `ShowContact` (default false), `ShowPage2` (default true), `ShowExperience` (default true)
- `ViewCount`, `LastViewedAtUtc`
- Optional `PassportAccessRequestId`
- Max 10 active links per candidate.

**`PassportAccessRequest`:**
- `Id`, `PassportDocumentId`
- requester `Name` (60), `Organisation` (80), `Email` (254), `Message` (300)
- `CreatedAtUtc`, `Status` (Pending/Approved/Declined/Expired), `DecidedAtUtc`
- Rules:
  - Pending expires after 30 days.
  - Requester data is deleted 90 days after the decision or expiry (`PassportSharingRetentionJob`, also cleans expired links > 90 days).
- Add the FKs `PassportDocument.ShareLinkId` (column from 04) and `PassportAccessLog.ShareLinkId` (column from 03) → `PassportShareLink` (on delete: set null).

**Logging:** every live view writes `PassportAccessLog` (03) with Kind `ShareLinkView` / `PartnerPortalView` (03 enum; `VerificationView` stays unused for anonymous hits). There is no IP. Anonymous authenticity-only hits are not logged per view: keep only a daily aggregate counter per document.

## `/v/{publicId}` (Web, anonymous, `public-read`)
Three states:
1. **Authenticity, always:**
   - "Echt Lobsy DNA-paspoort" + generated date + languages + whether co-branded (partner name only if the link was co-branded)
   - Lobsy-geverifieerd status **now** (04 rules)
   - "gewijzigd sinds deze PDF" when the current model `ContentHash` ≠ the document hash
   - No name, nothing personal beyond the **initials**.
   - Unknown id → generic 404 (no enumeration hints; constant-time-ish lookup on the unique index).
2. **Live view:**
   - **When:** `?s={token}` matches an active, non-expired link of this document's candidate, **or** the signed-in user passes `IPassportPartnerService.CanPartnerViewAsync` for this candidate.
   - **What:** a web version of p1 (+ p2 if allowed) built from the same `PassportDocumentModel` (current data, not the snapshot), with the share link's Show* toggles applied.
   - Partner views get the "Gedeeld met {partner}" ribbon.
3. **Private:**
   - Shown otherwise: "Dit paspoort is privé" + an access-request form (name, organisation, e-mail, message, checkbox "Ik vraag dit aan voor een vacature/werk").
   - Rate limit `access-request`; honeypot.
   - On submit, the candidate gets an e-mail + in-app notification. The requester gets a neutral confirmation that doesn't reveal whether the candidate exists beyond the authenticity block.

**Headers:**
- `X-Robots-Tag: noindex, nofollow`, `Cache-Control: no-store`, `Referrer-Policy: no-referrer` (token in URL)
- the existing strict CSP, no third-party scripts
- the token is stripped from the address bar after first load (`history.replaceState`)

**PDF tie-in:** when a PDF is generated for a share link (from the sharing screen "PDF voor deze link"), the QR encodes `/v/{publicId}?s={token}`. Without a link, the QR stays `/v/{publicId}`.

## Candidate sharing screen `/candidate/paspoort/delen` (mockup d)
Sections:
- **Partners** (03 links): logo, name, type, since, contact consent toggle, reconfirm-due date, "Intrekken". Max-3 hint. "Code toevoegen" arrives in 06; leave a placeholder only when the flag is ON.
- **Deellinks:** create (label, duration 7/30/90 with 30 preselected, toggles), copy link, show QR, "PDF voor deze link", views + last viewed, revoke.
- **Toegangsverzoeken:** approve, which creates a 30-day link and e-mails it to the requester with a template; or decline. The requester's e-mail is only shown to the candidate.
- **Wat is zichtbaar:** read-only list of what partners/links can and can never see (AVG list + "geen score/ranking").
- **Vertalingen:** approve/redo per field (re-uses 04 logic).
- **"Alles intrekken":** confirm dialog. It revokes all links and all partner links (reason `CandidateRevokedAll`) and declines pending requests.
- Entry: button "Delen & toegang" on `PassportCard` and in the Proof tab.

## API
- `GET/POST/DELETE api/me/passport/share-links`
- `GET api/me/passport/access-requests` and `POST …/{id}/approve|decline`
- `POST api/me/passport/revoke-all`
- `GET api/public/passport/{publicId}` (authenticity DTO)
- `POST api/public/passport/{publicId}/access-request`
- Live data is served server-side in Blazor; no public JSON with personal data.
- All endpoints validate ownership. Tokens are compared by hash only.

## Emails (all 5 languages, registry + preview)
- `PassportAccessRequested` (to candidate)
- `PassportAccessApproved` (to requester, contains the link + expiry)
- `PassportShareLinkExpiringSoon`: optional, **not** in v1, only listed

## Tests
- **Unit:**
  - token hashing
  - expiry/revoke
  - max 10 links
  - Show* toggles applied to the model
  - ContentHash change detection
  - retention job (requests 90 days, links)
  - revoke-all
  - `CanView` matrix: share token / partner / none / expired / revoked / suspended partner
- **Integration/API:** ownership (candidate A can't revoke B's link); unknown id → 404 with the same body for unknown and other states.
- **bUnit:** sharing screen sections; 30 days default; revoke-all confirm.
- **Playwright 390 + 1440** (soft-skip):
  - create link → open in an anonymous context → live view
  - revoke → private view
  - access request submit
  - `/v/{id}` without token = authenticity only (no name)
  - response headers contain noindex + no-store
  - no horizontal overflow
- **i18n parity** for new keys.

## Success criteria
- `/v/{id}` never leaks personal data without a valid token or partner consent.
- Share links default to 30 days, can be revoked, are logged, and expire automatically.
- Candidates see and control every link, partner and request on one screen. "Alles intrekken" works instantly.
- Release build with 0 warnings, tests green.

## Out of scope
- The partner portal (07).
- Partner codes/onboarding (06).
- Push notifications.
- Analytics on views beyond `ViewCount`.
- Public search/indexing of passports (never).
