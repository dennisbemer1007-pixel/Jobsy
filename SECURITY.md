# Security & Compliance Richtlijnen: Jobsy

## A. OWASP Top 10 (Applicatiebeveiliging)
- **Injection Prevention:** Altijd gebruikmaken van *Entity Framework Core* (geen handgeschreven, onveilige SQL-queries).
- **Broken Access Control:** Strenge *Policy-based Authorization* + DB-backed rollen/company-scope. Development header-auth gebruikt alleen DB-rollen.
- **Cryptographic Failures:** Wachtwoorden via PBKDF2 (`JobsyPasswordHasher`); integratiesecrets via ASP.NET Data Protection (`ISecretProtector`).
- **Security Misconfiguration:** Secure/HttpOnly/SameSite cookies; security headers; rate limiting op auth/public writes; FallbackPolicy RequireAuthenticatedUser.

## B. AVG / GDPR (Privacy by Design)
- **Data Minimization:** Alleen opslaan wat strikt noodzakelijk is voor de match en sollicitatie.
- **Consent:** Server-side vastgelegd (`ConsentAcceptedAt` / `ConsentVersion`) bij registratie en sollicitatie.
- **Right to be Forgotten:** `IPrivacyDataService` + `/api/privacy/delete-account` en UI `/privacy/data`.
- **Data portability:** `/api/privacy/export`.
- **Retention:** `DataRetentionHostedService` purged logs/engagement/cancelled registrations.
- **Logging:** Geen plaintext PII/secrets in PlatformLogs (e-mail redaction, geen body).

## C. Security Headers (Middleware)
De ASP.NET Core pipeline stuurt standaard:
- `Content-Security-Policy` (API)
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy`, `Permissions-Policy`
- HSTS buiten Development
