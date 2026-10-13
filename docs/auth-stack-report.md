# Auth stack end report (files 01–08)

## Needs Dennis (read first)

1. **D11** — Confirm lockout / MFA lockout mails on a 2FA pause (AccountLockout / MfaLockout CTAs → `/wachtwoord-vergeten`).
2. **D15** — Confirm `JobsyAuth:AdminAllowedEntraTenants` on acceptatie (work tenants only for admin Entra).
3. **D16** — Confirm regeneration page `/account/mfa/herstelcodes-vernieuwen` in production use.
4. **Admin trusted devices (04.7)** — confirm admin sees trusted-device count / MFA reset clears trusts.
5. **pl / ar native check** — `docs/i18n/auth-review.md` before go-live.
6. **Config names only** — verify `JobsyAuth:InternalClientIpSecret` and `CLOUDFLARE_ORIGIN_SECRET` are set on acceptatie and production (never paste values).
7. **ar screenshots** — attach desktop/mobile of login, pause, MFA, forgot, set-password when reviewing Acc.
8. **Drop column** — later migration for `PlatformFeatureSettings.ExposeRegistrationActivationLinks`.

## Manual checks on acceptatie (≤ 10)

1. Wrong password 5× on a **test** account → “Even pauze”.
2. Admin via Google → admin-provider message, no cookie.
3. SalesManager test account → forced 2FA setup.
4. BranchManager → prompt + “Vertrouw dit apparaat” then skip on next login.
5. Recovery code once → banner + mail; same code refused.
6. Wachtwoord vergeten → uniform screen; link works once; 2FA still asked after reset.
7. `/login?returnUrl=/admin/users` → no Google button.
8. `/register/activate?token=…` → 301 `/register` (token not shown).
9. `/login?lang=ar` → RTL layout, LTR e-mail/password.
10. Microsoft/Google login for non-admin → **no** extra Lobsy 2FA.

## What shipped (01→08)

| File | Branch | Tip (approx) | Summary |
|---|---|---|---|
| 01 | `cursor/auth-hotfix` | `46cb0511` | TrustedClientIp, typed failures, lockout, TOTP replay, CSP guards |
| 02 | `cursor/auth-2` | `88527c8f` | Roles/2FA policy, admin provider block, AuthMethod on sessions |
| 03 | `cursor/auth-3` | `229e28d5` | Login redesign on PublicLayout / au-* |
| 04 | `cursor/auth-4` | `e3d2892c` | MFA redesign, trusted devices, recovery XXXX-XXXX |
| 05 | `cursor/auth-5` | `6ff9a164` | Wachtwoord vergeten (PasswordReset purpose, 30 min) |
| 06 | `cursor/auth-6` | `7522a244` | Remove `/register/activate` + activation links; 301 |
| 07 | `cursor/auth-7` | `6ace57f4` | pl/ro/ar MFA+login, a11y/CSS/i18n guards |
| 08 | `cursor/auth-8` | *72edbdea* | Playwright E2E classes + this report |

## Dependency cases

| Dep | Case | Notes |
|---|---|---|
| A Public theme | **PRESENT** | PublicLayout + public-theme.css |
| B LobsyMascot | **PRESENT** (re-check at 03) | Used; no AuMascot fallback |
| C account-maken | **PRESENT** | |
| D Employers | **PRESENT** | IEmployersSwitch / PlatformFeature.Employers |
| E OneTimeLinks | **PRESENT** | PasswordReset purpose added in 05 (no migration) |
| F Mailer | **PRESENT** | ITransactionalMailer + EmailStrings |
| G WA wizard | **PRESENT** | 04 wizard variant; 06 removed activate page |
| H AdminAudit | **PRESENT** | |
| I Sales | **PRESENT** | SalesManager MFA required |

Re-checks: 03 A/B/C/D; 04 A/G/H; 05 A/E/F; 06 G — all PRESENT.

## Decisions as built (highlights)

- Microsoft/Google: **no** extra Lobsy 2FA; admins cannot use Google.
- Password reset: 30 min single-use; sessions + trusts revoked; 2FA still required after.
- Activation: wizard code only; old links 301 `/register`.
- MFA recovery codes: grouped `XXXX-XXXX`; trusted device cookie 30d.

## Test summary

- Unit/API auth suites (01–07): green for file criteria.
- New Playwright: `AuthE2EPlaywrightTests`, `AuthMfaE2EPlaywrightTests`, `AuthProviderE2EPlaywrightTests`, `AuthRtlKeyboardPlaywrightTests`, plus `AuthA11yPlaywrightTests` and `CspSmokePlaywrightTests` wired into CI unit-exclude + smoke-include.
- Soft-skip without `JOBSY_E2E_BASE_URL`; MFA deep flows need `JOBSY_E2E_MFA_*` seeds.
- **CspSmokePlaywrightTests:** soft-skip via `JOBSY_CSP_SMOKE_BASE_URL` or `JOBSY_E2E_BASE_URL`; was in neither CI filter before 08 — now in both (exclude unit / include smoke). Auth paths include `/login` and `/wachtwoord-vergeten`.

## Pre-existing Acc red (outside auth criteria)

- `PlatformSettingsCatalogTests.Keys_unique…` — `FieldExists("EmployersEnabled")` true vs expected false.
- `CandidateRegisterLinkGuardTests` — WA/Werkgever `/register` hrefs.

## Deferred / follow-ups

See `docs/auth-followups.md`. Also: seeded e2e MFA accounts in Testing, fake OIDC for admin Google E2E in browser, axe-core, ar screenshot gallery for PR review.
