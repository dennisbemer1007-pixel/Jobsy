Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 06 (HIGH): Time-limited, reasoned support access ("break-glass") for admins

**Goal:** an admin sees unmasked personal data only for **one subject**, for a **limited time** (default 60 min, max 4 h), with a **mandatory reason** (ticket/feedback reference). Every such grant is logged and visible to all admins. This builds on prompt 05.

**Evidence and context:** after prompt 05, admin endpoints return masked data. Support still sometimes needs real contact details (e.g. a candidate who cannot log in, or payout issues). GDPR (data minimisation, art. 5(1)(c); accountability, art. 5(2); security, art. 32) asks for need-to-know access with an audit trail.

**Do:**
1. Add a `SupportAccessGrant` entity:
   - `Id`, `AdminUserId`;
   - `SubjectUserId?` / `SubjectCompanyId?`;
   - `Scope` (flags: Contact, Cv, Iban, Applications, Feedback);
   - `Reason` (min 15 chars), `TicketReference?`;
   - `CreatedAt`, `ExpiresAt`, `RevokedAt?`, `RevokedByUserId?`.

   Add the migration **last**, after rebasing.
2. Add `ISupportAccessService`:
   - `RequestAsync`: requires a recent MFA step-up if the existing MFA infrastructure supports it (check `AuthServiceCollectionExtensions` and the MFA flow; otherwise just require an active MFA session);
   - `HasActiveGrant(adminId, subjectId, scope)`;
   - `RevokeAsync`.
3. Admin endpoints (`AdminController` and the others from prompt 05) return unmasked fields only when `HasActiveGrant` is true. Every unmasked response writes `PersonalDataAccessLog` with `SupportAccessGrantId` and `Action=reveal`.
4. UI:
   - a "Request temporary access" dialog on the admin user, company, sales manager and application detail views (reason, scope, duration select 15/60/240 min);
   - a banner showing "Temporary access to <masked subject> until HH:mm — reason: …" with a revoke button;
   - a list of active and recent grants on the `PersonalDataAccessLogAdmin` page (visible to all admins, the four-eyes principle);
   - Optional (behind a setting, off by default): email notification to other admins on each grant.
5. Optional (setting, off by default): note in the subject's own privacy/data page (`PrivacyData`) that support accessed their data on a given date. Document it; Dennis decides.
6. Put all texts in UiStrings nl + en.

**Do not touch:** the masking rules themselves (prompt 05); non-admin roles; CSS files with pending PRs.

**Verify:**
- Unit tests: grant expiry (use `TimeProvider` if the codebase uses it, otherwise an injectable clock), scope checks, revoke.
- Integration tests: without a grant the admin gets masked values; with a grant for subject A, A is unmasked and B stays masked; after expiry, masked again; each reveal is logged.
- Build and tests green.
- Manual check on the admin UI with the demo admin.

**Dependency:** **after prompt 05 is merged.**
