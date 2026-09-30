# 05. Wachtwoord vergeten: 30-minute reset link to `/account/wachtwoord-instellen`

Read `00-README.md` first (§0, §IA, Decisions D1, D2, D13, D14, Dependencies A, E, F). Branch `cursor/auth-5` from `cursor/auth-4`. **Stacked.** **Re-run Dependencies E and F first.** In case **E-wait** this file is skipped (README E) and 06 branches from `cursor/auth-4`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/auth-5`; no force-push.
> - ONE PR into `acceptatie`; the body starts with `Stacked on #<PR 04> (cursor/auth-4)` and names the E and F case.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start file 06.
> - Never lock, reset or change a real account on acceptatie or production while testing. Never send a reset mail to a real person; use test accounts and the dev mail sink. Never log passwords, codes, tokens, links or TOTP secrets.
> - Microsoft and Google logins never get an extra Lobsy 2FA step (admins can't use Google, 02).

| | |
|---|---|
| Branch | `cursor/auth-5` |
| PR title | `feat(auth): wachtwoord vergeten with a 30-minute single-use link, uniform wording, sessions revoked, 2FA still asked` |
| Mockups | `au-d07`/`au-m07` (form), `au-m08` (mail sent; desktop state is the right half of `au-d07`), `au-d09`/`au-m09` (new password) |
| Migration | none (E present), or `AddOneTimeLinks` (E-absent) |
| Split seam | E-absent and > ~1.500 lines: **05a** = emails 01.3.1/01.3.2 shell (one-time links + set-password page); **05b** = the reset flow on `cursor/auth-5b` |

## Goal
Someone who forgot their password gets back in by themselves, safely:
- one form, one neutral answer
- a link that works once for 30 minutes
- a clear new-password page
- then a normal login with 2FA

## 05.1 Today (verify first)
- There is no reset flow. Lockout (01) and the `MfaLockout` mail say "Hulp nodig? Mail support".
- **Password storage:** `LocalAuthCredential` + `JobsyPasswordHasher`; rules in `Jobsy.Core/Rules/RegistrationPasswordRules.cs` (min 12, max 128; reuse them, and don't add new rules).
- **Invites** set passwords through their own flows (`SalesManagerInviteService`, `AmbassadeurInviteService`, `CompanyRegistrationService`). Don't change them here.
- **Dependencies:**
  - E: `IOneTimeLinkService` + `SetPassword.razor`
  - F: mail renderer
  - Write down in the PR what each check printed.

## 05.2 One-time link (E cases)
- **E present:** add `OneTimeLinkPurpose.PasswordReset` (next free value, don't renumber). Lifetime 30 min via `OneTimeLinkRules` (add a per-purpose lifetime if it only has one). Single use. `CreateAsync` for the same user + purpose invalidates older unused links (D14; add that behaviour if it's missing, with a test).
- **E-absent:** implement emails 01.3.1 and the page shell of 01.3.2 **with exactly the names written there** (README E), plus `PasswordReset`. Migration `AddOneTimeLinks`. Tokens: 32 random bytes, base64url in the link, only the SHA-256 hash stored.
- **E-wait:** stop. This file is skipped (README E).

## 05.3 Request: `/wachtwoord-vergeten` (au-d07 / au-m07 / au-m08)
- **Page:**
  - `Components/Pages/Account/ForgotPassword.razor`, `@page "/wachtwoord-vergeten"`, static SSR, the 03 layout + `au-card`, the small mascot
  - h1 "Wachtwoord vergeten?" Lead "Vul je e-mailadres in. Dan sturen we je een link om een nieuw wachtwoord te kiezen."
  - the e-mail field as on `/login` (prefilled from `Jobsy.LoginHint` when present)
  - primary "Stuur de link"
  - link "Terug naar inloggen"
- **Web** `POST /account/wachtwoord-vergeten` (antiforgery) → API `POST api/auth/password-reset/request { email, culture }` via the "JobsyAuthApi" client (trusted IP, 01) → always 303 to `/wachtwoord-vergeten?sent=1`.
- **Sent state** (au-m08 / right half of au-d07):
  - emoji circle 📬, h2 "Kijk in je mail"
  - text: "Staat dit e-mailadres bij ons? Dan krijg je binnen een paar minuten een mail met een link. De link werkt 30 minuten. Geen mail? Kijk ook bij ongewenste mail."
  - secondary "Opnieuw versturen" (a new POST, same limits)
  - "Terug naar inloggen"
  - **The same screen for every case** (D1/D14).
- **API** `PasswordResetController.Request` (`[AllowAnonymous]`, `[EnableRateLimiting("auth")]`):
  - **Always 202** with an empty body, even for invalid input (also 202 for bad e-mail format; the Web validates the format first).
  - Rate limit per e-mail: 3 per hour (keyed by the HMAC of the normalized e-mail as 01's `UnknownAccountLockoutTracker`, process-local). Over the limit → 202 without a mail.
  - Timing: do the lookup and the link work the same way for known and unknown e-mails, and send mail after the response (a queued background send via the existing mail queue if there is one; otherwise fire-and-forget with logging). The response time must not depend on whether the account exists (test with a tolerance, as 01's dummy-hash test).
  - **Who gets which mail (D14):**
    - active user with a `LocalAuthCredential`, or with no login method at all → `PasswordReset` mail with the link
    - active user with only Microsoft/Google (external logins, no local credential) → `PasswordResetExternalOnly` mail: "Je logt bij Lobsy in met {Microsoft/Google}. Je hebt dus geen Lobsy-wachtwoord nodig. Ga naar lobsy.nl/login en kies {provider}." No link.
    - **Admins** follow the same rules (they may use a password + 2FA; D7).
    - unknown or inactive → nothing
  - Log `auth.password_reset.requested` with the user id or `unknown`, never the e-mail or the token.

## 05.4 The mails (F cases)
- **`PasswordReset`:**
  - subject "Kies een nieuw wachtwoord voor Lobsy"
  - body: "Je vroeg om een nieuw wachtwoord. Klik op de knop. De link werkt 30 minuten en één keer."
  - button "Nieuw wachtwoord kiezen" → `{PublicBaseUrl}/account/wachtwoord-instellen?token=…&doel=reset`
  - "Was jij dit niet? Dan hoef je niets te doen. Je wachtwoord blijft hetzelfde."
  - no e-mail address in the URL
- **`PasswordResetExternalOnly`** as above.
- **`PasswordChanged`** (after success):
  - "Je wachtwoord is gewijzigd op {datum} om {HH:mm}. Je bent op alle apparaten uitgelogd."
  - "Was jij dit niet? Mail support." (mailto)
- **F present:** registry entries (README F), `EmailStrings` in 5 languages, culture from the request or the user's saved culture.
- **F absent:** `TransactionalEmails` methods with stable keys (§0 Mails), culture nl/en (the rest falls back to nl), + follow-up line.

## 05.5 New password: reset variant of `/account/wachtwoord-instellen` (au-d09 / au-m09)
- `SetPassword.razor` gets a **reset variant** when the link's purpose is `PasswordReset`:
  - h1 "Kies een nieuw wachtwoord"
  - context line with the masked e-mail (04 `EmailMask`)
  - two fields "Nieuw wachtwoord" / "Herhaal wachtwoord" (`autocomplete="new-password"`, `dir="ltr"`, "Toon" toggle from `auth.js`)
  - a live rules list built from `RegistrationPasswordRules` ("Minstens {0} tekens" with the real number, never hard-coded; checked state via `auth.js`, and the server validates again)
  - primary "Wachtwoord opslaan"
  - Keep the invite/set variant from emails 01 unchanged.
- **The token:**
  - `GET` uses `PeekAsync` (doesn't consume)
  - expired, used or unknown → `LoginStatusBlock` "Deze link werkt niet meer. Vraag een nieuwe aan." + primary "Nieuwe link aanvragen" → `/wachtwoord-vergeten`
  - Put the token in a hidden field, don't echo it anywhere else, and send `Referrer-Policy: no-referrer` on this page.
- **POST** (antiforgery) → API `POST api/account/setup-password` with purpose reset (or `api/auth/password-reset/complete` if emails 01's endpoint can't take a purpose; say which) → `ConsumeAsync` in the same transaction as:
  - set/replace `LocalAuthCredential` (hash via `JobsyPasswordHasher`)
  - **clear lockout** (01 fields: failed count, lockout end, lockouts-in-24h counter) and the 2FA pause from 01
  - `SessionVersion` + 1
  - revoke all device sessions (`RevokeAllAsync`, reason `password-reset`)
  - revoke all trusted devices (04, D13)
  - invalidate other unused `PasswordReset` links
  - the `PasswordChanged` mail
  - log `auth.password_reset.completed` (user id only)
- Then 303 to `/login?setup=done` (03's mint state "Je wachtwoord is opgeslagen. Log nu in.").
- **2FA stays:** at the next login `MfaPolicy` applies as usual. A reset never disables 2FA or clears the TOTP secret or recovery codes. Someone who lost both password and phone mails support (the admin 2FA reset stays the path).
- Admin invites (02): the reset variant shows no Google option for an Admin.

## 05.6 Wire it in
- Set `AuthFeatures.PasswordResetAvailable = true`. This shows:
  - "Wachtwoord vergeten?" on `/login` (03)
  - the "Nieuw wachtwoord kiezen" button on the pause card (03)
- The `AccountLockout` mail (01) and the `MfaLockout` mail (01) get the button "Nieuw wachtwoord kiezen" → `/wachtwoord-vergeten` (a plain link, not a token; the person requests the link themselves).
- **Routes:**
  - add `/wachtwoord-vergeten` to ROUTES.md, `PageSeoCatalog` (noindex), `PageHelpDocs` if pages are listed there
  - the anonymous allowlist for `MfaEnforcementMiddleware` and any auth-required middleware
  - the CSP smoke list
- `SECURITY.md`: a section "Wachtwoord vergeten" with the lifetime, single use, rate limits, uniform response and revocations.

## 05.7 Tests
- `PasswordResetRequestTests`:
  - 202 for known/unknown/inactive/external-only/bad input
  - the right mail per case (fake mail sink); no mail for unknown/inactive
  - 4th request in an hour → no mail
  - a new request invalidates the older link
  - no e-mail or token in logs (log capture)
- `PasswordResetTimingTests`: known vs unknown e-mail response times within the tolerance used in 01.
- `PasswordResetCompleteTests`:
  - valid link → password works, old one doesn't
  - lockout cleared, `SessionVersion` bumped, device sessions and trusted devices revoked, `PasswordChanged` sent
  - a second use of the same link → "Deze link werkt niet meer"
  - after 30 min (fake clock) → expired
  - password shorter than 12 → error, token not consumed
  - 2FA user: the next login still asks for the code
- `SetPasswordResetVariantRenderTests`: title, masked e-mail, `autocomplete="new-password"`, `Referrer-Policy: no-referrer`, and no token in any link on the page.
- `LoginPageRenderTests` updated: "Wachtwoord vergeten?" now present; the pause card shows the reset button.
- E-absent: the emails 01.3.1 tests named there (`OneTimeLinkServiceTests` etc.).
- `PasswordResetPlaywrightTests` (both CI filter lists):
  - the full flow with the dev mail sink: request → sent screen → link → new password → `/login?setup=done` → login (+ 2FA for a 2FA test user)
  - screenshots nl desktop/mobile
  - no CSP violation

## Success criteria
- The screen and the response look the same for every e-mail. Only the right people get a link, and the link works once for 30 minutes.
- After a reset the person is signed out everywhere, trusted devices are gone, the lockout is lifted and 2FA is still asked.
- The pages match au-d07/m07/m08/d09/m09. The PR states the E and F cases, the endpoint choice (05.5) and the follow-ups.

Done → next: `06-register-activate-opruimen.md`.
