# ADR 0005: MFA is local-password only

**Status:** Accepted  
**Date:** 2026-09-29

## Context

Privileged roles (`MfaPolicy`) must use Lobsy TOTP after a local password login. The same accounts may also sign in with Microsoft (Entra) or Google. Those identity providers already enforce MFA (or the org’s Conditional Access). Forcing a second Lobsy authenticator after an IdP login blocked admins and employers who never enrolled in Lobsy’s TOTP, and duplicated MFA for users who already completed it at the IdP.

## Decision

- **Local password sign-in** for a role in `MfaPolicy` (or any user who voluntarily enrolled) requires Lobsy’s TOTP challenge. If the user is not enrolled yet, they get the forced enrollment flow (QR + verify + one-time recovery codes) before a session cookie is issued.
- **External IdP sign-in** (`entra`, `google`, or any other normalized provider) **never** creates a Lobsy MFA challenge. The session carries `auth_method=external:{provider}` and is treated as MFA-satisfied for enforcement and support-access step-up.
- **Mixed accounts** (password + linked IdP): the rule follows the **sign-in method used**, not the account. Password → Lobsy TOTP (enroll if needed). Microsoft/Google → skip Lobsy MFA.
- Server-side: `MfaEnforcementMiddleware` redirects or returns 401 when a privileged **local** session lacks `MfaVerified`. External sessions are exempt. Allowlist covers `/account/*`, login/logout, static assets, Blazor framework, and health endpoints.
- Enrollment no longer depends on the candidate-application “Authenticator stub” feature flag.

## Consequences

- Demo privileged accounts without a seed TOTP go through enrollment after password login (no shared secret backdoor).
- Admins can reset another user’s 2FA (audited, sessions revoked); self-reset via this admin API is refused.
- Documented in [`docs/ARCHITECTURE.md`](../ARCHITECTURE.md) (auth section) and covered by MFA / login-protection tests.

## Amendment (auth 02, 2026-09)

External IdP logins skip Lobsy 2FA, except that admins may only use Microsoft work/school accounts (and password + 2FA); Google and personal Microsoft accounts are refused for the Admin role (Dennis, 30-09). SalesManager password users require MFA; Ambassadeur does not.

## Amendment (test accounts, acceptatie)

Acceptatie-only CLI test accounts (`User.IsTestAccount`) skip local MFA via `MfaPolicy.IsRequiredFor(role, isTestAccount, testAccountsActive)` **only while** the process-wide acceptatie guard passes (`ITestAccountsRuntime.IsActive` on API and Web). When the guard is inactive (production, switch off, wrong host/DB/payments), the flag alone does not exempt anyone. The admin Google block remains.
