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
- **Right to be Forgotten:** `IPrivacyDataService` + `/api/privacy/delete-account` en UI `/privacy/data` — wist o.a. snapshots, verificatiecodes, site visits, registratiecontact, IBAN/MaskedIban.
- **Data portability:** `/api/privacy/export` (applications+snapshots, engagement, registraties, sales payouts/ledger/invoices).
- **IBAN:** Volledige IBAN alleen server-side; API/UI tonen gemaskeerde vorm.
- **Retention:** `DataRetentionHostedService` purged logs/engagement/cancelled registrations/site visits.
- **Logging:** Geen plaintext e-mailadressen in PlatformLogs (redaction via `EmailServiceStub.RedactEmail`).
- **Demo:** `JobsyAuth:AllowDevelopmentAuth` + DemoUsers zijn bewust voor de publieke demo; seed draait alleen bij Development / `Seed:Enabled` / AllowDevelopmentAuth. Demo one-click login zet geen wachtwoorden in HTML.

## C. Security Headers (Middleware)
De ASP.NET Core pipeline stuurt standaard:
- `Content-Security-Policy` (API)
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy`, `Permissions-Policy`
- HSTS buiten Development
