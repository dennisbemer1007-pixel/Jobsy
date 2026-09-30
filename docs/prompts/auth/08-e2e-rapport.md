# 08. Playwright E2E for every auth flow, CSP smoke, docs and the stack-end report

Read `00-README.md` first (all of it). Branch `cursor/auth-8` from `cursor/auth-7`. **Stacked. Last file.**

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/auth-8`; no force-push.
> - ONE PR into `acceptatie`; the body starts with `Stacked on #<PR 07> (cursor/auth-7)` and contains the stack-end report (08.5).
> - Red tests or an unmet success criterion: push, open the PR as **draft**, and still write the report with the red items at the top.
> - E2E runs **only** against a local test host or acceptatie with seeded test accounts, **never production** (`lobsy.nl`). Never lock, reset or change a real account. Never log passwords, codes, tokens, links or TOTP secrets, and never put them in screenshots.
> - Microsoft and Google logins never get an extra Lobsy 2FA step (admins can't use Google, 02).

| | |
|---|---|
| Branch | `cursor/auth-8` |
| PR title | `test(auth): Playwright E2E for login, pause, 2FA, trust, recovery, reset, admin provider block, RTL and mobile + stack-end report` |
| Mockups | all `au-*` (screenshot comparison by eye, attached to the PR) |
| Migration | none |

## Goal
Prove the whole stack works end to end in a real browser on desktop and mobile, with no CSP violations. Leave a report Dennis can read in five minutes.

## 08.1 Today (verify first)
- **Playwright classes** live in `Jobsy.Tests` (`…PlaywrightTests.cs`); CI runs them in the "Playwright mobile smoke" step of `.github/workflows/pr-tests.yml` (L64–68) and excludes them in the unit step (L52).
- `CspSmokePlaywrightTests` exists but is in neither filter list today. Find out how it runs (soft-skip? a separate workflow? `rg -n "CspSmoke" .github`) and write it down in the PR.
- `MfaEnrollmentPlaywrightTests` (in `MfaForcedEnrollmentTests.cs`) only asserts the base URL is never production. Keep that guard for every new class.
- The auth Playwright classes from 03–07 (`LoginPlaywrightTests`, `MfaPlaywrightTests`, `PasswordResetPlaywrightTests`, `AuthA11yPlaywrightTests`) exist when those files ran.

## 08.2 Test host and data
- **Host:** use the existing test host the Playwright classes use (in-process WebApplicationFactory + Kestrel, or `JOBSY_E2E_BASE_URL`). Reuse its guard: the base URL must not contain `lobsy.nl`.
- **Seeded test accounts** (created by the test fixture, not by `DemoUsersSeeder` in production code; if a seeder is needed, only in the `Testing` environment):
  - `e2e-candidate@test.lobsy.local` (password, no 2FA)
  - `e2e-branch@test.lobsy.local` (BranchManager, password, 2FA enrolled with a secret known to the test)
  - `e2e-sales@test.lobsy.local` (SalesManager, password, **not** enrolled, so setup is forced)
  - `e2e-admin@test.lobsy.local` (Admin, password + 2FA)
  - `e2e-admin-google@test.lobsy.local` (Admin, Google-only external login)
  - `e2e-ambassadeur@test.lobsy.local` (Ambassadeur, password, no 2FA)
- **TOTP in tests:** compute codes with `TotpAuthenticator` from the test's secret. Wait for the next time step when a test needs two codes (01's replay block).
- **External providers:** don't call real Microsoft/Google. Use the test authentication handler or a stubbed `ensure-external` path that the earlier files' Web tests use. If none exists, add a `Testing`-only fake OIDC scheme that issues the chosen claims, and never register it outside `Testing` (a unit test asserts that).
- **Mail:** the dev/test mail sink (fake `IEmailSender` or the renderer's test sink). Read reset links from the sink; never print them.
- **Clock:** where a flow needs time to pass (pause end, 30-minute link), use the fake clock the unit tests use, exposed only in `Testing`. If that's not possible in the browser tests, cover the time part in the API tests and say so.

## 08.3 Flows (desktop 1440 × 900 and mobile 390 × 844, nl; ar where marked)
1. **Login ok:** candidate logs in → lands where `AuthRedirects` says; "Blijf ingelogd" unchecked by default; no 2FA step.
2. **Wrong password:** error block (`role=alert`), e-mail kept, focus on password, URL without e-mail.
3. **Pause:** 5 wrong passwords → the "Even pauze" card with a time; the form is gone; Microsoft/Google buttons shown if configured; the reset button (05 present). The same for an unknown e-mail (uniform). One lockout mail in the sink, not two after a second pause within 24 h.
4. **Too many requests:** a burst over the per-IP limit (from one test client with the trusted-IP header set correctly by the Web) → the "Te veel pogingen" block, never a bare text page.
5. **2FA setup (SalesManager):**
   - password → forced setup
   - desktop: QR + key + copy toast
   - mobile: the "Open in authenticator-app" link first (assert `href` starts with `otpauth://`, don't follow it)
   - a wrong code shows the error on the setup page
   - a right code → the recovery codes page with 10 `XXXX-XXXX` codes, download works, "Verder" needs the checkbox
6. **2FA prompt + trust (BranchManager):**
   - prompt shows the masked e-mail
   - wrong code → error
   - a used code again → refused (replay)
   - a right code with "Vertrouw dit apparaat" → in
   - log out, log in again → no code asked
   - change password (or admin reset) → the code is asked again
7. **Recovery:** prompt → "Geen toegang tot je app?" → a recovery code → in, the "je hebt er nog 9" banner, the `RecoveryCodeUsed` mail in the sink. The same code again → refused. Regeneration (D16) makes old codes fail.
8. **2FA attempt limit:** 5 wrong codes on one challenge → "Log opnieuw in"; 10 in a row across challenges → the 2FA pause and one `MfaLockout` mail.
9. **Wachtwoord vergeten** (05 present; else assert the link is hidden):
   - request for a known and an unknown e-mail → the same screen
   - the link from the sink → new password → `/login?setup=done` → login + 2FA still asked (2FA user)
   - the link a second time → "Deze link werkt niet meer"
10. **Admin provider block:**
    - `e2e-admin-google` via the fake Google scheme → `/login?error=admin-provider` with the message, and **no auth cookie** in the browser context
    - admin via the fake Entra scheme with a work tenant → in, no Lobsy 2FA
    - with the consumer tenant → blocked
    - `/login?returnUrl=/admin/users` → no Google button
    - an admin cookie minted with `external:google` (test helper) → signed out on the next request
11. **External non-admin:** BranchManager via fake Google → in, no Lobsy 2FA (the kept rule).
12. **Stale privileged session:** a SalesManager password cookie without `MfaVerified` (test helper) → `/login?error=mfa-required`, not "Je inlogpoging is verlopen".
13. **`/register/activate`:** without a token → `/register` (302 from 01, or 301 from 06).
14. **RTL (ar):** `/login`, the pause card, the prompt, setup and the reset page are mirrored, with LTR fields (assert `dir` on the root and the fields) + screenshots.
15. **Keyboard only:** login and the 2FA prompt completed with Tab/Enter/Space only; focus visible in screenshots.

Group them into `AuthE2EPlaywrightTests` (flows 1–4, 9, 13), `AuthMfaE2EPlaywrightTests` (5–8, 12), `AuthProviderE2EPlaywrightTests` (10–11) and `AuthRtlKeyboardPlaywrightTests` (14–15). Every class keeps the not-production guard.

## 08.4 CSP smoke, CI and docs
- **CSP smoke** on every auth page: `/login` (each state), `/account/mfa`, `/account/mfa/setup`, `/account/mfa/recovery-codes`, `/account/mfa/herstelcodes-vernieuwen`, `/wachtwoord-vergeten`, `/account/wachtwoord-instellen` (reset variant). No `securitypolicyviolation` and no console errors. Extend `CspSmokePlaywrightTests` or add `AuthCspSmokePlaywrightTests`.
- **CI:** add every new Playwright class to **both** filter lists in `.github/workflows/pr-tests.yml` (excluded from the unit step, included in the smoke step). Add `CspSmokePlaywrightTests` too if 08.1 shows it's meant to run there. Screenshots upload with the existing artifact step.
- **Docs:**
  - `SECURITY.md`: one "Inloggen en 2FA" section that sums up 01–05: limits, lockout, 2FA rules per role, admin providers, trusted devices, reset
  - `docs/adr/0005-mfa-local-only.md` checked (02)
  - `docs/ROUTES.md` (all auth routes and redirects)
  - `TESTING.md` (how to run the auth E2E locally, the test accounts, never production)
  - `docs/TESTSCENARIOS_PER_ROL.md` (auth rows per role: Candidate, Ambassadeur, SalesManager, BranchManager, Admin)
  - `CHANGELOG.md`
- `docs/auth-followups.md`: tidy it, one line per follow-up with the owner stack (landing, emails, werkgever-aanmelding, paspoort, admin-redesign) and the file that would do it.

## 08.5 Stack-end report (in the PR body and in `docs/auth-stack-report.md`)
1. **Table:** file → branch → PR number → status (merged / open / draft / skipped) → one-line outcome.
2. **Dependency cases** A–I as they were at each file (present / absent / wait) and the fallback used.
3. **Decisions as built** D1–D19. Mark any deviation, and put the **Needs Dennis** points at the top:
   - D11 mail on a 2FA pause
   - D15 admin Microsoft tenants (`JobsyAuth:AdminAllowedEntraTenants`)
   - D16 regeneration page
   - admin trusted devices (04.7)
   - the pl/ar native check (`docs/i18n/auth-review.md`)
   - the config check: `JobsyAuth:InternalClientIpSecret` and `CLOUDFLARE_ORIGIN_SECRET` set on acceptatie and production (names only, never values)
4. **Test summary:** counts per class, the Playwright screenshots (links to the artifact), CSP result.
5. **Deferred / follow-ups:** from `docs/auth-followups.md`.
6. **Manual checks for Dennis on acceptatie** (≤ 10 bullets), for example:
   - log in with a wrong password 5 times on a test account and see "Even pauze"
   - admin with Google → message
   - SalesManager test account → 2FA setup
   - reset link flow

## Success criteria
- All flows in 08.3 pass on desktop and mobile (or are marked skipped with the dependency reason). No CSP violations on any auth page. CI runs the new classes in the smoke step.
- The docs are updated, and `docs/auth-stack-report.md` + the PR body contain the report with the Needs Dennis points first.

Done → stack complete. Report to Dennis with the stack-end report.
