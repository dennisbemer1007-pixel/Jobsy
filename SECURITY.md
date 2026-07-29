# Security & Compliance Richtlijnen: Jobsy

## A. OWASP Top 10 (Applicatiebeveiliging)
- **Injection Prevention:** Altijd gebruikmaken van *Entity Framework Core* (geen handgeschreven, onveilige SQL-queries).
- **Broken Access Control:** Strenge *Policy-based Authorization* + DB-backed rollen/company-scope. Development header-auth gebruikt alleen DB-rollen.
- **Cryptographic Failures:** Wachtwoorden via PBKDF2 (`JobsyPasswordHasher`); integratiesecrets via ASP.NET Data Protection (`ISecretProtector`); provision-secrets via constant-time compare.
- **Security Misconfiguration:** Secure/HttpOnly/SameSite cookies; security headers; rate limiting op auth/public writes; FallbackPolicy RequireAuthenticatedUser.
- **External provision:** `/api/auth/ensure-external` vereist `X-Jobsy-Provision-Secret` buiten Development (fail-closed zonder secret).
- **OIDC:** `SaveTokens=false`; Google options mutations zijn geserialiseerd; `email_verified=false` wordt geweigerd.

## B. AVG / GDPR (Privacy by Design)
- **Data Minimization:** Alleen opslaan wat strikt noodzakelijk is voor de match en sollicitatie.
- **Consent:** Server-side vastgelegd (`ConsentAcceptedAt` / `ConsentVersion` = `PrivacyConstants.CurrentConsentVersion`); clientversies worden genegeerd.
- **Progressive disclosure:** Werkgevers zien kandidaat-PII en snapshotvelden pas na acceptatie (`Accepted` / `EmployerContacting` / `Hired`).
- **Right to be Forgotten:** `IPrivacyDataService` + geverifieerde uitschrijving (`request-unsubscribe` / `confirm-unsubscribe`) via UI `/privacy/data` en `/candidate/profile` — reden + e-mailverificatiecode, daarna blokkeren en anonimiseren (snapshots, verificatiecodes, site visits, registratiecontact, IBAN/MaskedIban). Admin ziet de reden in platform logs (categorie `Unsubscribe`). `POST /api/privacy/delete-account` vereist dezelfde verificatiecode.
- **Data portability:** `/api/privacy/export` (applications+snapshots, engagement, registraties, sales payouts/ledger/invoices).
- **IBAN:** Volledige IBAN alleen server-side; API/UI tonen gemaskeerde vorm.
- **Retention:** `DataRetentionHostedService` purged logs/engagement/cancelled registrations/site visits.
- **Logging:** Geen plaintext e-mailadressen in PlatformLogs (redaction via `EmailServiceStub.RedactEmail`).
- **Demo:** `JobsyAuth:AllowDevelopmentAuth` + DemoUsers zijn bewust voor de publieke demo; seed draait alleen bij Development / `Seed:Enabled` / AllowDevelopmentAuth. Demo one-click login zet geen wachtwoorden in HTML. Buiten Development mag header-auth alleen `@jobsy.local` demo-accounts (incl. Admin/SalesManager). OAuth client-secrets vereisen een aparte `JobsyAuth:ExternalProvisionSecret` (nooit DevelopmentAuthSecret als fallback).
- **Verification OTPs:** Sollicitatie- en unsubscribe-codes via `RandomNumberGenerator` + constant-time compare (`VerificationCodes`). Max 5 foute pogingen per code + rate limit policy `otp-verify` (10/min).
- **Verified applications only:** Werkgeversmetrics/counts/drilldowns en kandidaat-sollicitatielijsten tellen alleen `EmailVerifiedAt != null`.
- **Registration activate:** Tijdelijk wachtwoord gaat alleen per e-mail; API/UI echo’t hem buiten Development niet.

## C. Security Headers (Middleware)
De ASP.NET Core pipeline stuurt standaard:
- `Content-Security-Policy` (API)
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy`, `Permissions-Policy`
- HSTS buiten Development
