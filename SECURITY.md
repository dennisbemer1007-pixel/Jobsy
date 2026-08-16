# Security & Compliance Richtlijnen: Jobsy

## A. OWASP Top 10 (Applicatiebeveiliging)
- **Injection Prevention:** Altijd gebruikmaken van *Entity Framework Core* (geen handgeschreven, onveilige SQL-queries).
- **Broken Access Control:** Strenge *Policy-based Authorization* + DB-backed rollen/company-scope. Development header-auth gebruikt alleen DB-rollen. Opt-in EF tenant filters (`CompanyTenantScope` / `EnforceCompanyScopeIds`) op Vacancy/TokenTransaction; vacaturebeheer checkt ook `IntermediaryCompanyId`.
- **KVK pending:** Registratie tijdens KVK-storing mag concepten klaarzetten, maar publiceren/tokenaankopen vereisen `KvkVerificationStatus.Verified`. Client mag geen Intermediair-rol claimen tijdens storing; SBI 78* volgt pas na retry. Establishment-id wordt server-side afgeleid.
- **Cryptographic Failures:** Wachtwoorden via PBKDF2 (`JobsyPasswordHasher`); integratiesecrets via ASP.NET Data Protection (`ISecretProtector`); provision-secrets via constant-time compare.
- **Security Misconfiguration:** Secure/HttpOnly/SameSite cookies; security headers; rate limiting op auth/public writes; FallbackPolicy RequireAuthenticatedUser.
- **Session inactivity:** Admin-configurable `SessionInactivityTimeoutMinutes` (default 30, clamp 5–480) on platform features. Web `SessionInactivityMiddleware` + browser idle-timer read it dynamically; `Jobsy.LastActivity` is Data Protection–sealed and subject-bound (forged/future/plaintext values expire the session). A *missing* activity cookie is re-stamped (login race / recovery); idle or tampered cookies still force re-auth. Auth endpoints (`/account/login`, `/account/demo-login`) are skipped so a stale auth cookie cannot block sign-in. Idle sessions redirect to `/login?error=session-expired`. Form draft restore is opt-in (`data-session-draft`) only, excludes IBAN/secrets, and is keyed by user subject.
- **External provision:** `/api/auth/ensure-external` vereist `X-Jobsy-Provision-Secret` buiten Development (fail-closed zonder secret).
- **OIDC:** `SaveTokens=false`; Google options mutations zijn geserialiseerd; `email_verified=false` wordt geweigerd. Externe accounts worden gebonden via `UserExternalLogins` (Entra OID / Google `sub`); match op subject gaat vóór e-mail zodat IdP e-mailwijzigingen geen orphan Candidate maken.

## B. AVG / GDPR (Privacy by Design)
- **Data Minimization:** Alleen opslaan wat strikt noodzakelijk is voor de match en sollicitatie.
- **Consent:** Server-side vastgelegd (`ConsentAcceptedAt` / `ConsentVersion` = `PrivacyConstants.CurrentConsentVersion`); clientversies worden genegeerd. Bij bump van `CurrentConsentVersion` moeten niet-kandidaat-accounts opnieuw akkoord gaan (`NeedsConsentReaccept` + `POST /api/me/accept-consent` + blocking UI). Kandidaten herbevestigen per sollicitatie.
- **Progressive disclosure:** Werkgevers zien kandidaat-PII (naam, e-mail, telefoon, adres, stad, work-permit, CV-snapshots) pas na acceptatie (`Accepted` / `EmployerContacting` / `Hired`). Pre-accept wél zichtbaar voor screening: motivatie, match-%, crow-flies afstand (km, zonder adres), leeftijd en beschikbaarheidssamenvatting. Het automatisch gegenereerde **Lobsy-CV (PDF)** volgt dezelfde vrijgave: download/preview voor werkgevers alleen post-Accept; kandidaat mag eigen preview altijd. Autorisatie op het PDF-endpoint (niet alleen UI). Spec: `docs/FUNCTIONELE_SPECIFICATIES_CV_PREVIEW_MODERATIE.md`.
- **Right to be Forgotten:** `IPrivacyDataService` + geverifieerde uitschrijving (`request-unsubscribe` / `confirm-unsubscribe`) via UI `/privacy/data` en `/candidate/profile` — reden + e-mailverificatiecode, daarna blokkeren en anonimiseren (snapshots, verificatiecodes, site visits, registratiecontact, IBAN/MaskedIban). Admin ziet de reden-code in platform logs (categorie `Unsubscribe`); free-text `ReasonOther` wordt niet gelogd. `POST /api/privacy/delete-account` vereist dezelfde verificatiecode.
- **Data portability:** `/api/privacy/export` (applications+snapshots, engagement, registraties, sales payouts/ledger/invoices, eigen feedback zonder screenshot-bytes).
- **In-app feedback:** `POST /api/feedback` slaat beschrijving + optionele screenshot op. Geen e-mail in het record (alleen user-id/rol/weergavenaam). Pagina-URL wordt zonder query/fragment bewaard. Screenshots zijn admin-only (`GET /api/feedback/{id}/screenshot`), staan niet in de lijst-API, en worden gewist bij right-to-be-forgotten (inclusief vrije tekst/prompt/rol) plus retentie (90 dagen na aanmaak, ongeacht status). Cursor-webhook vereist HMAC (`CursorCloud:WebhookSecret`) buiten Development.
- **IBAN:** Volledige IBAN alleen server-side; API/UI tonen gemaskeerde vorm.
- **Retention:** `DataRetentionHostedService` purged logs/engagement/cancelled registrations/site visits. Teaser-campagne-events (`TeaserEngagementEvents`) volgen dezelfde retentielijn; geen IP-opslag; UTM alleen genormaliseerde dimensies.
- **Logging:** Geen plaintext e-mailadressen in PlatformLogs (redaction via `EmailServiceStub.RedactEmail`).
- **Registration occupancy:** `GET /api/registration/kvk/{kvk}/establishments` returns boolean `IsInUse` (no owner/contact PII) so the wizard can mark claimed vestigingen as unavailable and offer a claim path. Generic `GET /api/kvk/.../establishments` still hides occupancy for anonymous callers.
- **Demo:** `JobsyAuth:AllowDevelopmentAuth` + DemoUsers zijn bewust voor de publieke demo; seed draait alleen bij Development / `Seed:Enabled` / AllowDevelopmentAuth. Demo one-click login zet geen wachtwoorden in HTML. Buiten Development werkt header-auth voor `@jobsy.local` demo-accounts met alleen de gedeelde `DevelopmentAuthSecret`. Niet-demo e-mails (echte registraties / OAuth) vereisen daarnaast een HMAC `X-Jobsy-Local-Session` ondertekend met `JobsyAuth:LocalSessionSigningKey` (fallback: DevelopmentAuthSecret voor lokale DX), uitgegeven bij `local-login` / `ensure-external` en vernieuwd via `/account/session-activity`. Activatie-URL’s in registratie-API-responses alleen bij `ExposeRegistrationActivationLinks`. OAuth client-secrets vereisen een aparte `JobsyAuth:ExternalProvisionSecret` (nooit DevelopmentAuthSecret als fallback).
- **Verification OTPs:** Sollicitatie- en unsubscribe-codes via `RandomNumberGenerator` + HMAC-SHA256 met application pepper (`VerificationCodes.Hash`); legacy unsalted SHA-256 blijft verifieerbaar tijdens rollout. Max 5 foute pogingen per code + rate limit policy `otp-verify` (10/min, keyed op user+IP).
- **Verified applications only:** Werkgeversmetrics/counts/drilldowns en kandidaat-sollicitatielijsten tellen alleen `EmailVerifiedAt != null`.
- **Registration activate:** Gekozen wachtwoord (PBKDF2) bij submit; activatie bevestigt e-mail. Takeover vereist e-mailverificatie vóór inbox/approve. Tijdelijk wachtwoord (legacy) alleen per e-mail; API/UI echo’t hem buiten Development niet. Pending `PasswordHash` wordt gewist bij activate/reject/cancel/expiry/anonymize.

## C. Security Headers & Error Handling (Middleware)
De ASP.NET Core pipeline stuurt standaard:
- `Content-Security-Policy` (API + Web; Blazor-compatible op Web)
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy`, `Permissions-Policy`
- HSTS buiten Development
- `ExceptionHandlingMiddleware` — generieke ProblemDetails naar clients; stacktraces/PII blijven server-side
- **Swagger:** standaard aan in Development; buiten Development uit tenzij `Swagger:Enabled=true`. “Try it out” blijft uit buiten Development.
- **Sentry:** optioneel via `Sentry:Dsn` / `Sentry__Dsn` (API + Web); geen PII (`SendDefaultPii=false`).
- **OTP pepper:** optioneel deploy-secret `VerificationCodes:Pepper` (fallback ingebouwde pepper blijft verifieerbaar tijdens rollout).
- **Stub payouts:** alleen Development of expliciet `JobsyAuth:AllowStubPayments=true` (niet meer gekoppeld aan AllowDevelopmentAuth).
- **Cookie consent:** banner + `Jobsy.CookieConsent` in localStorage; anonieme site-analytics pas na “Accepteer analytics”.
