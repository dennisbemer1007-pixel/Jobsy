# E-mails follow-ups

Dependency cases noted while implementing emails stack:

- **H. Intermediair — ABSENT (re-checked at 05):** candidate mails keep today's `CompanyName`. When intermediair 03 lands (`IntermediaryPublicIdentity`), switch employer display names in candidate mails and support hidden-mode bureau naming (`Email.EmployerContacting.ViaIntermediary` / `Email.ApplicationHired.ViaIntermediary` keys are ready).
- **A/C — PRESENT (re-checked at 05):** `EmailLinks.Map` = `/banenkaart` (`PublicRoutes.Banenkaart` / `KbRoutes.Map`).
- **B — PRESENT (re-checked at 06 and 07):** `WerkgeverNav` / `EmployerLinks` / `WerkgeverLegacyHrefTests` on acceptatie; `EmailLinks.Employer*` uses `/werkgever/...`.
- **E — PRESENT (re-checked at 06 and 07):** werkgever-aanmelding mails in registry; TakeoverRequest → `/werkgever/overnames`; TakeoverSubmitted uses HowLobsyWorks (no signed withdraw URL wired in this file).
- **D — PRESENT (re-checked at 07):** `SalesLegacyRoutes` → `/sales/start`; `AmbassadorsEnabled` gate; AmbassadeurInvite suppressed by mailer when off.
- **G — PRESENT (re-checked at 07 for mail targets; full re-check at 08):** `AdminNav`, `IAdminAuditLog` (`email.test-send`), `MfaResetByAdmin` in registry; `EmailLinks.AdminEmails` → `/admin/content/emails`.
- **I — 01 pattern:** set-password / reveal-once / parental-consent website POST (no secrets in mail).

## Deferred / skipped

- Signed TakeoverSubmitted withdraw URL (needs one-time link wiring).
- Native review pl/ar (`docs/i18n/emails-review.md`); ro spot check.
- Password-reset flow (D9) — not in this stack.
- Optional Playwright through `/account/wachtwoord-instellen` on the smoke stack (no mail sink on CI stack yet).
- Hosted PNGs polish beyond current `images/email/*` assets.
- Legal footer address + KvK until Dennis provides values (`Mail:LegalAddress`, `Mail:KvkNumber`).
- Pre-existing Acc suite failures (werkgever nav, RoleNav, banenkaart, SalesAttributedAtUtc, etc.) out of scope for emails.
