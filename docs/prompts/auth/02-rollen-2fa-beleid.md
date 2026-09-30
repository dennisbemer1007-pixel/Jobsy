# 02. Roles and 2FA policy: SalesManager 2FA, admins never via Google or a personal Microsoft account, no dead-end sessions

Read `00-README.md` first (§0, §IA, Decisions D5, D7, D15, D17, D19, Dependencies E, H, I). Branch `cursor/auth-2` from `cursor/auth-hotfix` (or `origin/acceptatie` when PR 01 is merged). **Stacked.** Run the README **Dependencies** checks first and put the outcome in this PR.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/auth-2`; no force-push.
> - ONE PR into `acceptatie`; the body starts with `Stacked on #<PR 01> (cursor/auth-hotfix)` and lists the dependency cases.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start file 03.
> - Never lock, reset or change a real account on acceptatie or production while testing. Never log passwords, codes, tokens or TOTP secrets.
> - Microsoft and Google logins never get an extra Lobsy 2FA step. The only exception is D7: admins can't use Google (or a personal Microsoft account) at all.

| | |
|---|---|
| Branch | `cursor/auth-2` |
| PR title | `feat(auth): SalesManager 2FA, admin login only via Microsoft work account or password+2FA, no dead-end MFA sessions` |
| Mockups | none (copy only) |
| Migration | `AddDeviceSessionAuthMethod` |

## Goal
- SalesManagers with a password set up 2FA at their next login.
- An admin can't get in with Google or a personal Microsoft account, now or through an old session. They see why and what to do.
- A signed-in session that lacks 2FA gets a clean re-login instead of "Je inlogpoging is verlopen".

## 02.1 Today (verify first)
- `Jobsy.Core/Security/MfaPolicy.cs`: Admin, BranchManager, RegionalManager, EnterpriseManager, Intermediary. Used by `AuthController` L134 (local login), L300 (`provider is null` path of `ensure-external`) and `MfaEnforcementMiddleware` L42.
- `MfaEnforcementMiddleware` (`Jobsy.Web/Security`):
  - skips `external*` auth methods (L33)
  - for a privileged local session without `MfaVerified` it redirects to `/account/mfa?returnUrl=…` (L57–62)
  - that page needs the `Jobsy.MfaChallenge` cookie, which a signed-in session doesn't have → "Je inlogpoging is verlopen" (dead end)
- **External login:**
  - Web `ApplyExternalJobsyProfileAsync` (~L1100–1240) derives `provider` from the scheme ("google" or "entra") and calls `api/auth/ensure-external`.
  - When the API call fails it keeps the user signed in as **Candidate** (`ReplaceRoleClaim(identity, "Candidate")`).
  - Entra authority is `…/{TenantId or "common"}/v2.0` (`EntraOidcOptionsApplier` L21, `AuthServiceCollectionExtensions` L83–88); with `common`, personal Microsoft accounts can sign in. The `tid` claim tells them apart (consumer tenant `9188040d-6c67-4c5b-b112-36a304b66dad`).
- API `AuthController.EnsureExternal` (L190–320) finds or creates the user, binds the external login (`EnsureExternalLoginBoundAsync`) and returns the profile. There is no provider rule per role.
- **Device sessions:**
  - `UserDeviceSession` has `MfaVerifiedUntilUtc` but no auth method.
  - `DeviceSessionsController.Refresh` (L52) → `DeviceSessionService.RotateAsync` rebuilds the principal from the DB role.
- The admin user page shows MFA status via `ResolveMfaStatus` (`AdminController` ~L625: enrolled / external-only / not-enrolled).
- `docs/adr/0005-mfa-local-only.md` documents "MFA only for local passwords".

## 02.2 `MfaPolicy`
- Add `UserRole.SalesManager`. `Ambassadeur` stays out (D5). Add a unit test listing every `UserRole` value with its expected result, so a new role forces a decision.
- Existing SalesManager password sessions: 02.3 sends them through a clean re-login and setup. No migration or forced sign-out job.

## 02.3 No dead-end for signed-in sessions without 2FA
- In `MfaEnforcementMiddleware`, replace the redirect to `/account/mfa` for an **authenticated** privileged local session without `MfaVerified`:
  - HTML requests: sign out the cookie scheme (and delete the device-session cookie via `DeviceSessionCookie`), then 302 to `/login?error=mfa-required&returnUrl=<safe path+query>`.
  - `/api/*`: 401 as today.
- `Login.razor` (today's card; 03 restyles) shows for `mfa-required` (`role="status"`, neutral): "Log opnieuw in. Daarna stel je een extra beveiliging in met een app op je telefoon." (`Login.MfaRequired`, 5 languages).
- The normal password login then produces the challenge and goes to setup or prompt (01 behaviour).
- Keep the allowlist (`/account`, `/login`, `/logout`, static files) unchanged.

## 02.4 Admins: never Google, never a personal Microsoft account
- **Rule in Core:** `Jobsy.Core/Security/AdminLoginProviderPolicy.cs`, `static bool IsAllowed(UserRole role, string? provider, string? entraTenantId, IReadOnlyCollection<string> allowedAdminTenants)`:
  - non-admin → true
  - admin + `google` → false
  - admin + `entra` + tenant = the consumer tenant → false
  - admin + `entra` + `allowedAdminTenants` non-empty + tenant not in it → false
  - otherwise true
  - Config `JobsyAuth:AdminAllowedEntraTenants` (comma-separated, empty = any work/school tenant; D15).
  - Unit tests for every branch.
- **Web → API:** `ApplyExternalJobsyProfileAsync` sends `providerTenantId` (the `tid` claim, or `http://schemas.microsoft.com/identity/claims/tenantid`; null for Google) in the `ensure-external` body (add it to `EnsureExternalUserRequest`).
- **API `EnsureExternal`:**
  - after finding the user and **before** binding a new external login or issuing anything: if `!AdminLoginProviderPolicy.IsAllowed(user.Role, provider, tenant, allowed)` → **403** `{ code: "provider_not_allowed" }`
  - log `auth.external.blocked` with the user id and provider (no e-mail)
  - a new user created by `ensure-external` is never an admin, so creation is unaffected
- **Web on 403 `provider_not_allowed`** (D19):
  - **don't sign in**: in the OIDC/OAuth ticket events, reject the principal (`context.Fail(…)` + `HandleResponse()` or the equivalent for the Google handler)
  - delete any cookie that was set, and redirect to `/login?error=admin-provider&returnUrl=<safe>`
  - other `ensure-external` failures keep today's Candidate fallback (D19), **except**:
    - any 403 from the API → never sign in
    - an error after the API has resolved the e-mail to an Admin → the API answers 403 `{ code: "admin_external_failed" }` (not 500), and the Web redirects to `/login?error=unavailable` without signing in
  - Say in the PR how you verified this for both handlers.
- **Message** (`Login.ErrorAdminProvider`, `role="alert"`, 5 languages): "Beheerders loggen in met een Microsoft-werkaccount of met e-mail, wachtwoord en een code uit de app. Inloggen met Google of een privé-Microsoft-account kan niet voor beheerders."
- **Existing sessions** (`Jobsy.Web/Security/AdminProviderGuardMiddleware.cs`, right after authentication, before `MfaEnforcementMiddleware`): an authenticated principal with role Admin and `auth_method` `external:google` (or `external:entra` with a stored consumer tenant claim, see below) → sign out + delete the device-session cookie → `/login?error=admin-provider`. `/api/*` → 401.
  - `ApplyProfileClaims` adds the claim `idp_tid` (tenant id) for Entra logins so the guard can check it.
- **Device-session refresh:**
  - Migration `AddDeviceSessionAuthMethod`: `UserDeviceSession.AuthMethod` (string 40, null for old rows). `DeviceSessionService.CreateAsync` stores the principal's `auth_method` (`local-registration`, `password+totp`, `external:google`, `external:entra`) and, for Entra, the tenant (`AuthTenantId`, string 64) in the same migration.
  - `RotateAsync`: for a user whose **current** DB role is Admin, refuse (return null → 401, the Web signs out) when `AuthMethod` is `external:google`, is Entra with a disallowed tenant, or is null **and** `MfaVerifiedUntilUtc` is null or in the past. Old admin device sessions without MFA proof therefore need one fresh login.
- **Hidden in admin context:** on `/login` when the requested `returnUrl` starts with `/admin`, the Google button isn't rendered (Microsoft and password stay), and a line explains it: "Beheerder? Log in met Microsoft (werkaccount) of met e-mail en wachtwoord." On `/account/wachtwoord-instellen` (only when Dependencies E is present), an invite for an Admin shows no Google option.
- **D17 hint:** the admin user detail page (Dependencies H: admin-redesign page if present, else today's) shows, for a user with role Admin whose only login is Google (no `LocalAuthCredential`, no Entra binding): "Deze persoon logt in met Google. Als beheerder kan dat niet. Stuur een link om een wachtwoord te kiezen, of laat hem met Microsoft inloggen." The same hint appears as a confirm step when an admin changes someone's role to Admin. `ResolveMfaStatus` gets a fourth value `google-blocked` for that case, with a label in the status column.
- **ADR:** amend `docs/adr/0005-mfa-local-only.md`: "External IdP logins skip Lobsy 2FA, except that admins may only use Microsoft work/school accounts (and password + 2FA); Google and personal Microsoft accounts are refused for the Admin role (Dennis, 30-09)."

## 02.5 Tests
- `MfaPolicyTests`: SalesManager required, Ambassadeur and Candidate not; every enum value covered.
- `MfaEnforcementStaleSessionTests`: a signed-in SalesManager password principal without `MfaVerified` on `/sales/...` (or `/home`) → signed out + 302 `/login?error=mfa-required&returnUrl=…`; an external SalesManager → passes.
- `AdminLoginProviderPolicyTests` (unit).
- `EnsureExternalAdminBlockTests` (API):
  - Admin + google → 403 `provider_not_allowed`, and no new `UserExternalLogins` row
  - Admin + entra consumer tenant → 403
  - Admin + entra work tenant → 200, no MFA challenge
  - BranchManager + google → 200, no MFA challenge (the rule "no extra 2FA for external" holds)
  - allowed-tenants config respected
- `ExternalAdminBlockWebTests`: a stubbed 403 from `ensure-external` → no auth cookie in the response, redirect to `/login?error=admin-provider`.
- `AdminProviderGuardTests`: an Admin principal with `external:google` → signed out; with `external:entra` + work tenant → passes.
- `DeviceSessionAdminRefreshTests`: refresh refused for Admin + `external:google`; refused for Admin with null `AuthMethod` and no MFA; allowed for Admin `password+totp` with valid `MfaVerifiedUntilUtc`; non-admins unchanged.
- `LoginAdminContextTests`: `/login?returnUrl=/admin/users` renders no Google button and the admin line.
- Existing `ExternalAuthAndInvitePromotionTests`, `MfaForcedEnrollmentTests`, `AuthorizationMatrixReflectionTests` stay green.

## Success criteria
- No code path signs in an Admin via Google or a personal Microsoft account (fresh login, old cookie, device refresh). Each is covered by a test.
- SalesManager password users go through 2FA setup; external SalesManagers don't.
- No authenticated request ends on "Je inlogpoging is verlopen" because of the middleware.
- `SECURITY.md`, `docs/adr/0005-mfa-local-only.md` and `CHANGELOG.md` updated. The PR lists the dependency cases, the new error codes and the config key `JobsyAuth:AdminAllowedEntraTenants`, and flags D15 for Dennis.

Done → next: `03-login-redesign.md`.
