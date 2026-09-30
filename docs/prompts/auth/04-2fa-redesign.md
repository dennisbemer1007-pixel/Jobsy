# 04. 2FA redesign: prompt, setup, recovery codes, "Vertrouw dit apparaat"

Read `00-README.md` first (§0, §IA, Decisions D3, D11, D12, D13, D16, Dependencies G, H). Branch `cursor/auth-4` from `cursor/auth-3`. **Stacked.** Re-run Dependencies G and H first and put the cases in the PR.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/auth-4`; no force-push.
> - ONE PR into `acceptatie`; the body starts with `Stacked on #<PR 03> (cursor/auth-3)`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start file 05.
> - Never lock, reset or change a real account on acceptatie or production while testing. Never log passwords, codes, tokens or TOTP secrets.
> - Microsoft and Google logins never get an extra Lobsy 2FA step (admins can't use Google, 02).

| | |
|---|---|
| Branch | `cursor/auth-4` |
| PR title | `feat(auth): 2FA prompt, setup and recovery codes redesigned; trusted device 30 days; grouped recovery codes; regeneration` |
| Mockups | `au-d04`/`au-m04` (prompt), `au-d05`/`au-m05` (setup), `au-d06`/`au-m06` (recovery codes) |
| Migration | `AddMfaTrustedDevices` |
| Split seam | if > ~1.500 lines: **04a** = pages + codes format + regeneration; **04b** = trusted device (entity, migration, cookie, revocations) on `cursor/auth-4b` |

## Goal
2FA feels like part of Lobsy, not a bare form:
- the prompt says who you are and lets you trust a device
- setup works on the phone itself
- recovery codes are easy to read, save and replace
Security from 01 stays intact.

## 04.1 Today (verify first)
- `Components/Pages/Account/MfaPrompt.razor`, `MfaSetup.razor`, `MfaRecoveryCodes.razor`: static SSR (`[ExcludeFromInteractiveRouting]`), but styled as `login-modal … role="dialog"`. 01 moved their scripts to `wwwroot/js/features/account-mfa.js` and fixed the signed-out redirect and errors.
- API `MfaController`:
  - `state`, `enroll` (returns secret, `ProvisioningUri` from `TotpAuthenticator.BuildProvisioningUri`, SVG QR data URI from `TotpQrCode`)
  - `verify`: first enrolment → `GenerateRecoveryCodes` (10 × 16 hex), hash via `HashRecoveryCode` with 01's normalization; otherwise TOTP or a recovery code
  - `RememberDevice` from the challenge → `DeviceSessionService.CreateAsync(mfaVerified: true)`
- "Blijf ingelogd" (device session, `MfaVerifiedUntilUtc`) keeps a session alive. It isn't a trusted device: after logout or expiry the code is asked again. That stays true; D13 adds the trust separately.
- Admin 2FA reset: `AdminController` ~L555–620 clears secret + codes, bumps `SessionVersion`, revokes device sessions, logs it. `UsersAdmin.razor` shows the status.

## 04.2 Shared page frame
- All three pages use the 03 layout (`PublicLayout` or `AuthPublicLayout`), the `au-card` and `LoginStatusBlock` for errors. No `role="dialog"`, one `h1`, static SSR, JS only in `account-mfa.js` (nonce).
- Top of the card: the step context line, masked e-mail and "Ander account" link.
  - Masked e-mail from `state`: "d••••@lobsy.nl" (first letter + `••••` + domain; `EmailMask.Mask` in `Jobsy.Core/Security`, unit-tested, never logged).
  - "Ander account" → `POST /account/mfa/cancel`: deletes the challenge cookies, then `/login`.

## 04.3 Prompt (au-d04 / au-m04)
- h1 "Vul je code in". Lead "Open je authenticator-app en typ de 6 cijfers."
- **One field:**
  - `<input id="mfa-code" name="code" inputmode="numeric" autocomplete="one-time-code" pattern="[0-9 ]{6,7}" maxlength="7" dir="ltr" autofocus>`, label "Code uit je app"
  - the server strips spaces
  - no placeholder zeros
- **"Vertrouw dit apparaat 30 dagen"** checkbox, unchecked (D3). Hint "Dan vragen we de code hier niet elke keer. Alleen op je eigen apparaat."
- Primary "Inloggen". Errors via `LoginStatusBlock` (`invalid_code`, `challenge_expired`, `mfa_locked` from 01).
- **Recovery:**
  - `<details>` "Geen toegang tot je app?" contains the recovery field (`autocomplete="off"`, `autocapitalize="characters"`, `dir="ltr"`, label "Herstelcode", hint "Bijvoorbeeld 7KQ2-M9XA") and its own submit button "Inloggen met herstelcode"
  - it opens by itself when the last error came from a recovery code
  - Code and recovery code are **separate submits** (`name="method" value="totp|recovery"`); the API checks only the chosen one.
- Footer: "Nieuwe telefoon en geen herstelcodes? Mail support" (mailto from 01).

## 04.4 Setup (au-d05 / au-m05)
- h1 "Beveilig je account". Lead "Je hebt een app nodig die codes maakt, zoals Microsoft Authenticator of Google Authenticator."
- Three numbered steps (an `<ol>` with big step numbers):
  1. "Installeer een authenticator-app" with store links (plain links, no badges required).
  2. "Koppel de app":
     - **Mobile** (a CSS media query shows/hides, and `account-mfa.js` doesn't sniff UA): primary pill "Open in authenticator-app" = `href` the `ProvisioningUri` (`otpauth://…`), then `<details>` "Werkt dat niet? Toon de QR-code en sleutel".
     - **Desktop:** the QR (`<img alt="QR-code om Lobsy te koppelen">`) + the manual key grouped in 4s (`dir="ltr"`, monospace) + "Kopieer sleutel" (`data-copy-target`, 01's toast "Gekopieerd" in an `aria-live="polite"` region), and a small link "Op je telefoon? Open in authenticator-app".
     - Both variants are in the HTML; only visibility differs.
  3. "Vul de code in": the same field as the prompt, primary "Koppelen".
- No trust checkbox on setup (the first time always ends with the recovery codes).
- The error from 01 stays on the page (`source=setup`).
- **Wizard variant** (G present, `returnUrl` starts with `/register/`): the stepper of the werkgever-aanmelding wizard above the card (reuse its component; step "Beveiligen") and "Stap 3 van 4" in the context line. G absent → no stepper; add a follow-up.

## 04.5 Recovery codes (au-d06 / au-m06)
- **New format (D12):** `GenerateRecoveryCodes` makes 10 codes of 8 chars from `23456789ABCDEFGHJKLMNPQRSTUVWXYZ` (`RandomNumberGenerator.GetItems`), shown as `XXXX-XXXX`. Stored hash = `HashRecoveryCode(Normalize(code))`, where `Normalize` trims, upper-cases and strips `-` and whitespace (01). Old 16-hex codes keep working.
- The page:
  - mint success banner "Je extra beveiliging staat aan"
  - h2 "Bewaar je herstelcodes"
  - the text "Kwijt je telefoon? Met één van deze codes kom je toch binnen. Elke code werkt één keer."
  - the 10 codes in a 2-column grid (1 column below 360 px), monospace, `dir="ltr"`, numbered, `aria-label` per code
  - buttons "Download (.txt)", "Kopieer alles", "Print" (`data-download-codes`, `data-copy-target`, `window.print` via `account-mfa.js`)
  - the checkbox "Ik heb mijn codes veilig bewaard" (required)
  - primary "Verder" (enabled; the server-side required checkbox from 01)
  - Print CSS shows only the codes + date + "Lobsy herstelcodes voor d••••@lobsy.nl".
- **Wizard variant** (G present): "Stap 4 van 4", primary "Verder naar je bedrijf".
- **`?used=1`** (01) shows the "je hebt er nog {x}" banner and the link "Maak nieuwe herstelcodes" (D16).

## 04.6 Recovery-code regeneration (D16)
- `GET/POST /account/mfa/herstelcodes-vernieuwen`: signed in + `MfaVerified` + local password; otherwise it redirects as in 01.
  - Form: the TOTP field + primary "Maak nieuwe codes". Text: "Je oude codes werken daarna niet meer."
  - API `POST api/auth/mfa/recovery-codes/regenerate` (authenticated, rate-limited `auth`, TOTP required with 01's replay block) → new codes, a `RecoveryCodesRegenerated` mail (via the renderer, Dependencies F), and trusted devices revoked (D13).
  - Then the 04.5 page with the new codes (the same short-lived cookie as today's `MfaRecoveryCodesCookie`).
- Linked from `?used=1`, from the account/settings page if one exists (`rg "account/instellingen|/instellingen" Jobsy.Web/Components/Pages` → say what you found), and from the `RecoveryCodeUsed` mail.

## 04.7 Trusted device (D13)
- **Entity `MfaTrustedDevice`** (`Jobsy.Core/Entities`): `Id`, `UserId`, `TokenHash` (SHA-256 hex, unique), `CreatedAtUtc`, `ExpiresAtUtc` (+30 days), `LastUsedAtUtc`, `UserAgentSummary` (browser + OS, max 120), `RevokedAtUtc`, `SessionVersionAtCreate`. Migration `AddMfaTrustedDevices`, index on (`UserId`, `RevokedAtUtc`).
- **Create:** `verify` with `trustDevice=true` and a TOTP or recovery success → `MfaTrustedDeviceService.CreateAsync` returns the raw token. The Web sets `Jobsy.MfaTrust` (HttpOnly, Secure, SameSite=Lax, 30 days, path `/`, data-protected wrapper containing user id + token).
- **Use:**
  - The Web's local-login post sends the cookie's token to `api/auth/local-login` as `mfaTrustToken`.
  - After the password succeeds and `MfaPolicy.IsRequired`, the API checks for a matching row: same user, not revoked, not expired, `SessionVersionAtCreate == user.SessionVersion`, and 2FA still enrolled.
  - Match → no challenge, `MfaVerified: true`, `auth_method=password+trusted-device`, `LastUsedAtUtc` updated.
  - It never skips the password or the lockout (D13).
  - No match → normal challenge, and the Web deletes the cookie.
- **Revoke on:**
  - password change/reset (05)
  - admin 2FA reset (`AdminController.ResetMfa`)
  - recovery-code regeneration
  - "log uit op alle apparaten" (`DeviceSessionsController.RevokeAll` L140, `DeviceSessionRules.RevokeReasonLogoutAll`)
  - every `SessionVersion` bump: the version check covers it; also set `RevokedAtUtc` in the explicit paths
  - Microsoft/Google logins don't touch trusted devices.
- **Admin:** the user page shows "Vertrouwde apparaten: {n}" next to the 2FA status. The 2FA reset confirm text becomes "Hiermee zet je de extra beveiliging uit en vergeet Lobsy alle vertrouwde apparaten. De persoon stelt 2FA opnieuw in bij de volgende login." Audit via `IAdminAuditLog` (H present) or today's logging.
- **Admins** (D13): trust works for them too, with the same 30 days. Mention it in the PR as a point for Dennis to confirm; no change unless he says so.
- **Privacy:** add `MfaTrustedDevice` to the privacy export/erase (`PrivacyDataService`, next to `UserExternalLogins`) and to `docs/privacy` if the data map lists tables.

## 04.8 Clean-up
- Remove unused `Mfa.*` keys (for example `Mfa.EnterCode` if still unused; check with the parity/unused report).
- Remove the modal classes from the three pages and any `account-mfa__*` CSS that's no longer used.
- Add all new keys (`Mfa.*`, `Auth.*`) in 5 languages (07 reviews them).

## 04.9 Tests
- `MfaPagesRenderTests`:
  - each page has one `h1`, no `role="dialog"`, and no inline script
  - the prompt field has `autocomplete="one-time-code"` and `dir="ltr"`
  - trust unchecked
  - the masked e-mail is shown, the full e-mail isn't
  - setup contains the `otpauth://` link and the QR
  - codes page: 10 codes in `XXXX-XXXX`
- `RecoveryCodeFormatTests`: alphabet, length, grouping; hash equality for `7kq2-m9xa`, ` 7KQ2M9XA `, `7KQ2 M9XA`; an old 16-hex code still verifies.
- `RecoveryCodeRegenerationTests`: wrong/replayed TOTP → 401; success → old codes fail, new work, mail sent, trusted devices revoked.
- `MfaTrustedDeviceTests`:
  - trust → the next password login skips the code
  - wrong password still fails and counts for lockout
  - expired, revoked, `SessionVersion`-bumped, other-user and 2FA-reset tokens → challenge
  - password change and admin reset revoke
- `MfaCancelTests`: "Ander account" clears the challenge cookies.
- `AdminMfaResetTests` extended for trusted devices.
- `CspSmokePlaywrightTests` + a new `MfaPlaywrightTests` (both CI filter lists):
  - setup on 1440 and 390 (the mobile link visible first)
  - copy toast
  - codes page print preview has no CSP violation
  - screenshots in the PR

## Success criteria
- The three pages match au-d04/d05/d06 (desktop + mobile) in structure and tokens and work without a circuit.
- Trusted devices skip only the code, expire after 30 days and are revoked on every path in 04.7, with a test for each path.
- New codes are grouped; old codes still work; regeneration works and mails.
- The PR lists the dependency cases G/H, the admin-trust point for Dennis and the follow-ups.

Done → next: `05-wachtwoord-vergeten.md`.
