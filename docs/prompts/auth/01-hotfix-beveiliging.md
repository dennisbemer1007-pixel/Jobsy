# 01. Security hotfix: per-visitor rate limits, honest login errors, visible lockout, 2FA attempt limits, CSP fix for 2FA pages

Read `00-README.md` first ("How to run", §0, §IA, §B, Decisions D2, D3, D8–D12). Branch `cursor/auth-hotfix` from `origin/acceptatie`. **Standalone: not stacked on anything.**

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/auth-hotfix`; no force-push.
> - ONE PR into `acceptatie`; the body starts with `Standalone hotfix (not stacked)`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start file 02.
> - Never lock, reset or change a real account on acceptatie or production while testing; test users in the test host only. Never log passwords, codes, tokens, recovery codes or TOTP secrets.
> - Microsoft and Google logins never get an extra Lobsy 2FA step.
> - **Don't redesign the pages here.** Keep today's `login-card` markup and CSS; only add the states and fixes this file names. The redesign is 03/04.

| | |
|---|---|
| Branch | `cursor/auth-hotfix` |
| PR title | `fix(security): per-visitor auth rate limits, honest login/2FA errors, visible lockout, 2FA attempt + replay limits, CSP-safe 2FA scripts` |
| PR body starts with | `Standalone hotfix (not stacked)` |
| Mockups | `au-d02`/`au-m02` (error copy), `au-d03`/`au-m03` (pause copy; without "Nieuw wachtwoord kiezen" until 05) — only for copy, not layout |
| Migration | `AddAuthHardening` (the only one in this file) |
| Split seam | if > ~1.500 lines: **01a** = 01.2 (client IP + limits) + 01.3 (typed failures + mapping) + 01.4 (lockout) + 01.5 (timing); **01b** = 01.6 (2FA limits/replay/recovery mail) + 01.7 (CSP) + 01.8 (small fixes) on `cursor/auth-hotfix-b`, stacked on 01a, body "Stacked on #<01a> (standalone hotfix part 2)" |

## Goal
- One busy minute can no longer lock everyone out of logging in.
- Nobody is told "wrong password" when the real problem is a pause, a rate limit or a server error.
- A locked account says so, with a time, for known and unknown e-mails alike.
- A stolen password alone can't brute-force or replay a 2FA code.
- The recovery-codes page works under the CSP.

## 01.1 Today (verify first)
- **Web → API auth calls** use `new HttpClient { BaseAddress = ApiBaseUrl }` without any client-IP header:
  - `TryLocalApiLoginProfileAsync` (`Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs` ~L797–829) → `api/auth/local-login`
  - `TryMfaVerifyAsync` (~L831–854) → `api/auth/mfa/verify`
  - `MfaPrompt.razor` (~L100–110) and `MfaSetup.razor` (~L95–140) → `api/auth/mfa/state`, `api/auth/mfa/enroll`
  - `TryAttachProvisionedDeviceSessionAsync`, the `ensure-external` call (~L1140, via `IHttpClientFactory`) and `/account/complete-login` (~L574–653): check each.
- The authenticated app client already forwards the visitor IP: `JobsyApiAuthHandler.ApplyTrustedClientIp` (~L139–163) adds `X-Jobsy-Client-Ip` + `X-Jobsy-Internal-Secret` (`InternalClientIpHeaders`, config `JobsyAuth:InternalClientIpSecret`). The API reads it in `RateLimitPartitioning.TryReadTrustedClientIp` (FixedTimeEquals + `IPAddress.TryParse`).
- **API limits:**
  - The `auth` policy (`Jobsy.Api/Program.cs` L99–109) partitions by the `client_ip` claim or `RemoteIpAddress`, **not** by `ResolvePartitionKey`.
  - `Jobsy.Api/Security/LoginProtectionMiddleware.cs` (L19) uses the same IP source.
  - So anonymous auth traffic from Web is one bucket.
- **Web limits:**
  - `Jobsy.Web/Program.cs` L136–149 `auth` policy on `RemoteIpAddress`, and `Jobsy.Web/Security/LoginProtectionMiddleware.cs` answers a bare text 429.
  - `RemoteIpAddress` is the visitor only after `UseForwardedHeaders()` + `CloudflareOriginMiddleware` (L154–155; the middleware replaces it with `CF-Connecting-IP` only when the origin secret is enforced).
  - `VacancyMapApiForwarder.ResolveVisitorIp` (`Hosting/VacancyMapProxyEndpoints.cs` L204–219) reads `CF-Connecting-IP` directly, which can be spoofed when enforcement is off.
- **Login failures:**
  - `AuthController.LocalLogin` (L57–160) returns `Unauthorized(genericError)` for unknown e-mail (L75–78, no hash), for lockout (L80–82) and for a wrong password (L85–114).
  - `LoginLockoutRules` (`Jobsy.Core/Security/LoginLockoutRules.cs`): 5 failures → 15 min, doubling to 240; `FailedLoginCount` resets only on success (L117).
  - The lockout mail is inline HTML (L98–107, category `AccountLockout`) and promises a reset that doesn't exist.
- **Web mapping:** `TryLocalApiLoginProfileAsync` returns `null` for every non-success → `/login?error=invalid` (L313).
- **2FA:**
  - `MfaChallengeService` (in-memory, 10 min, `MfaChallenge(UserId, RememberDevice, LocalPassword)`).
  - `MfaController.Verify` (L105–201) has no attempt counter.
  - `TotpAuthenticator.VerifyCode` accepts steps −1…+1 and keeps no last-used step.
  - `TryUseRecoveryCode` (L219–240) removes the hash silently.
  - `AdminController` L595 also verifies a TOTP (admin reset confirm).
- **CSP:** `JobsyContentSecurityPolicy.ForWeb` = `script-src 'self' 'nonce-…'` + `script-src-attr 'none'`.
  - `MfaRecoveryCodes.razor` has an inline `<script>` (L55) without a nonce and a `disabled` Continue button (L52).
  - `MfaSetup.razor` L47 has an `onclick`.
  - `CspNonce.GetOrCreate(HttpContext)` gives the request nonce.
- `MfaRecoveryCodes.razor` renders for anyone (no auth check).
- `MfaSetup` posts to `/account/mfa/verify`; a failure redirects to `/account/mfa?error=invalid` (L374), which redirects a not-enrolled user to setup without the error.
- `Login.razor` L118: `rememberDevice` `checked`.
- `/register/activate`:
  - `RegisterActivate.razor` (InteractiveServer, prerender off).
  - The mails `TransactionalEmails.RegistrationActivation` (L530) and `TakeoverEmailVerification` (L583) have a "Code invoeren" button → `EmailLayout.RegisterActivateUrl` (L76, no token).
  - Tests: `TransactionalEmailCatalogTests` L117, `EmailLayoutTests` L70.
- Tests that exist and must stay green (extend them where it fits): `LoginProtectionTests`, `RateLimitPartitioningTests`, `MfaForcedEnrollmentTests`, `LoginAntiforgeryTests`, `LoginIdentityTests`, `CspSmokePlaywrightTests`.

## 01.2 The real visitor IP, safely, and per-visitor limits
- **One trusted source (Web):** add `Jobsy.Web/Security/TrustedClientIp.cs` with `static string? Resolve(HttpContext? http)`, which returns `http.Connection.RemoteIpAddress` **after** the forwarded-headers + `CloudflareOriginMiddleware` pipeline (that middleware already swaps in `CF-Connecting-IP` only for requests that passed the origin secret).
  - Never read `CF-Connecting-IP` or `X-Forwarded-For` directly anywhere else.
  - `VacancyMapApiForwarder.ResolveVisitorIp` delegates to it. Say in the PR that map-proxy partitions now use the trusted value.
  - IPv6: normalize to the /64 prefix for rate-limit keys only (`TrustedClientIp.PartitionKey(ip)`); logs keep nothing.
- **One handler for anonymous auth calls (Web):** `Jobsy.Web/Auth/AuthApiClient.cs`.
  - A named `HttpClient` "JobsyAuthApi" (`IHttpClientFactory`, base `ApiBaseUrl`, timeout 8 s) with a `DelegatingHandler` `TrustedClientIpHandler`. It removes any incoming `X-Jobsy-Client-Ip`/`X-Jobsy-Internal-Secret` and, when the secret is configured, adds both with `TrustedClientIp.Resolve(IHttpContextAccessor.HttpContext)`.
  - Move the shared code out of `JobsyApiAuthHandler.ApplyTrustedClientIp` so both use one method.
  - Every call in 01.1 (login, verify, state, enroll, ensure-external, device attach, complete-login) goes through `AuthApiClient`. No `new HttpClient` remains in `Jobsy.Web/Auth` or `Pages/Account` (guard test).
- **API:**
  - The `auth` policy uses `RateLimitPartitioning.ResolvePartitionKey(httpContext, internalClientIpSecret)` (anonymous calls then partition by `cip:` + visitor IP).
  - `Api/Security/LoginProtectionMiddleware` uses the same resolved IP (`TryReadTrustedClientIp` → claim → RemoteIp).
  - Numbers unchanged (D10).
- **Startup warning:** when `JobsyAuth:InternalClientIpSecret` is empty in Web or API outside Development, log one `LogWarning` at startup ("auth rate limits fall back to the Web→API hop"). Don't fail startup.
- **Web 429s:**
  - `LoginProtectionMiddleware` (Web) and a Web `auth` limiter rejection on a form POST (`/account/login`, `/account/mfa/verify`) redirect (303) to `/login?error=too-many&until=<unix>` or `/account/mfa?error=too-many&until=<unix>`, using `Retry-After` / the window end.
  - Other paths keep 429 JSON.
  - `until` is display-only.

## 01.3 Typed failures and honest mapping
- **API `local-login` responses** (record `AuthFailure(string Code, DateTime? RetryAtUtc)` in `Jobsy.Api/Models`):
  - wrong e-mail/password → **401** `{ code: "invalid_credentials" }`
  - paused (known **or** unknown e-mail, 01.4) → **403** `{ code: "locked_out", retryAtUtc }`
  - rate limited → **429** (existing `OnRejectedAsync` body + `Retry-After`); add `code: "rate_limited"` to that JSON
  - inactive user → 401 `invalid_credentials` (unchanged: no oracle)
- **Web:** `TryLocalApiLoginProfileAsync` returns a result type `LocalLoginOutcome { Profile?, Failure (Invalid | Locked | TooMany | Unavailable), RetryAtUtc? }`:
  - timeout, 5xx, network error or unparseable body → `Unavailable`
  - `POST /account/login` maps them to `/login?error=invalid|locked|too-many|unavailable` (+ `&until=` for locked/too-many) with the safe `returnUrl`
- **`Login.razor` texts (today's card; keys in `UiStrings` `Login.*`, 5 languages, pl/ro/ar B1 drafts):**
  - `invalid` → **"Dat klopt niet helemaal. Het e-mailadres of wachtwoord is niet goed. Probeer het nog eens."**
  - `locked` → title **"Even pauze"**, text "Er is te vaak een verkeerd wachtwoord ingevuld. Daarom kun je tot {HH:mm} niet inloggen met je wachtwoord. Zo houden we je account veilig." Then "Inloggen met Microsoft of Google kan wel." and "Hulp nodig? Mail {support}" (`mailto:`, the configured support address, default `support@lobsy.nl`). The time is Europe/Amsterdam; use `AmsterdamTime` from emails 01 if it exists on the branch, else a private helper in `Jobsy.Web/Auth/AuthTime.cs` with the existing lookup pattern (`"Europe/Amsterdam"`, then `"W. Europe Standard Time"`, as `FreePublishRules` ~L45). Missing or past `until` → "Probeer het over een kwartier opnieuw."
  - `too-many` → "Te veel pogingen vanaf dit apparaat. Probeer het om {HH:mm} opnieuw."
  - `unavailable` → "Inloggen lukt nu even niet. Probeer het over een paar minuten opnieuw."
  - The error block gets `role="alert"`; `session-expired` gets `role="status"` and the neutral style (it's not an error).
- **2FA verify responses** (API `MfaController.Verify`):
  - 401 `invalid_code`
  - 401 `challenge_expired` (unknown/used challenge, or 5 wrong codes on it, 01.6)
  - 403 `mfa_locked` + `retryAtUtc`
  - 429
  - 200 adds `recoveryCodesLeft` (int?) and `usedRecoveryCode` (bool)
  - Web `TryMfaVerifyAsync` returns a typed outcome like login.
- **`POST /account/mfa/verify`** reads a hidden form field `source` (`setup` or `prompt`, default `prompt`) and redirects failures to `/account/mfa/setup?error=…` or `/account/mfa?error=…` with `invalid`, `expired`, `locked` (+ `until`) or `too-many` (+ `until`).
  - `expired` clears the challenge cookies and shows "Je inlogpoging is verlopen. Log opnieuw in." with the link.
  - `locked` → "Even pauze. Er is te vaak een verkeerde code ingevuld. Probeer het om {HH:mm} opnieuw."

## 01.4 Lockout: visible, reset after the pause, escalation per 24 h, one mail per 24 h, the same for unknown e-mails
- **Migration `AddAuthHardening`**:
  - `LocalAuthCredential` gets `LockoutCount` (int, default 0), `LastLockoutAtUtc` (DateTime?) and `LastLockoutMailAtUtc` (DateTime?).
  - `User` gets `MfaFailedCount` (int, default 0), `MfaLockoutUntilUtc` (DateTime?), `LastTotpTimeStep` (long?) and `LastMfaLockoutMailAtUtc` (DateTime?).
  - Snapshot updated.
- **`LoginLockoutRules`** (D8):
  - `FailedAttemptsBeforeLockout = 5`
  - `LockoutDuration(int lockoutsInWindow)` → 15, 30, 60, 120, 240 min (1st…5th+)
  - `LockoutWindow = 24 h`
  - `MailCooldown = 24 h`
  - Unit tests for every step.
- **`AuthController.LocalLogin` order**:
  1. Normalize the e-mail. Load the credential.
  2. **Paused?** `LockoutUntil > now` → verify the password against the dummy hash (01.5) and return 403 `locked_out` with `LockoutUntil`. Nothing increments while paused.
  3. **Pause over?** If `LockoutUntil` is set and `≤ now`, set `FailedLoginCount = 0` and `LockoutUntil = null` (the counter resets after the pause). If `LastLockoutAtUtc < now − 24 h`, also set `LockoutCount = 0`.
  4. Verify the password. Wrong:
     - `FailedLoginCount++`
     - at 5: `LockoutCount++`, `LastLockoutAtUtc = now`, `LockoutUntil = now + LockoutDuration(LockoutCount)`, and return **403 `locked_out`** at once (the 5th wrong try already shows the pause)
     - send the lockout mail only if `LastLockoutMailAtUtc` is null or older than 24 h, then set it
     - below 5: 401 `invalid_credentials`
  5. Right: reset `FailedLoginCount`, `LockoutUntil`, `LockoutCount` (not `LastLockoutMailAtUtc`), then as today.
- **Unknown e-mails** (D9): `Jobsy.Core/Security/UnknownAccountLockoutTracker.cs`, a singleton, process-local like `LoginProtectionRateLimiter`.
  - Key = HMAC-SHA256(normalized e-mail) with a key from `JobsyAuth:LocalSessionSigningKey` (or the dev secret fallback that `JobsyLocalSessionToken.ResolveSigningKey` uses).
  - Same thresholds, durations and 24 h window, entries expire after 24 h.
  - Unknown e-mail → dummy hash (01.5) → record the failure → 401 or 403 exactly as for a known account. No mail.
  - Cap the dictionary at 100.000 keys (evict the oldest).
- **Lockout mail** (`TransactionalEmails.AccountLockout(baseUrl, untilUtc)`, today's layout, key/category `AccountLockout`; §0 Mails):
  - subject "Je Lobsy-account is even op pauze"
  - body "Er is 5 keer een verkeerd wachtwoord ingevuld. Daarom kun je tot {d MMMM, HH:mm} niet inloggen met je wachtwoord. Was jij dit? Dan kun je het daarna gewoon weer proberen. Was jij dit niet? Neem dan contact op met {support}."
  - no reset promise (05 adds the reset button)
  - The send stays inside a try/catch (the pause never depends on mail). Move the send into one private method `SendLockoutMailAsync` so emails 07 can swap the template.

## 01.5 Timing: always one password hash
- `JobsyPasswordHasher`: add `static bool VerifyAgainstDummy(string password)`. It verifies against a lazily created hash of a random 32-byte value with `DefaultIterations` and always returns false.
- Call it for an unknown e-mail, for a paused account and for an inactive user, so every `local-login` does exactly one PBKDF2 run.
- Test: `LocalLoginTimingTests` asserts the hasher's verify path runs once for unknown, paused and known accounts. Use a counting test seam (e.g. an internal `Action` hook or an `IPasswordHasher` wrapper); don't measure wall-clock time.

## 01.6 2FA: attempt limits, replay block, recovery-code mail and "x codes left"
- **One verifier:** `Jobsy.Infrastructure/Security/TotpVerifier.cs` (`ITotpVerifier` in Core): `VerifyAsync(User user, string? code, DateTime utcNow)` → `TotpResult { Ok, Step }`.
  - `TotpAuthenticator` gets `TryVerifyCode(secret, code, utcNow, out long matchedStep)` (keep `VerifyCode` as a wrapper for existing callers).
  - Reject when `matchedStep <= user.LastTotpTimeStep` (**replay block**). On success set `LastTotpTimeStep = matchedStep`, with an optimistic check (`ExecuteUpdateAsync … WHERE LastTotpTimeStep IS NULL OR LastTotpTimeStep < @step`, 0 rows = replay).
  - `MfaController.Verify` (enrolment + login) and `AdminController` L595 use it (grep `TotpAuthenticator.VerifyCode` and move every call site).
- **Per challenge:** `MfaChallenge` gets `int FailedAttempts`. `MfaChallengeService` gets `RegisterFailure(token)`, which increments and, at 5, removes the challenge. `Verify` then answers 401 `challenge_expired`.
- **Per user** (D11):
  - every wrong TOTP or recovery code: `MfaFailedCount++`
  - at 10: `MfaLockoutUntilUtc = now + 15 min`, `MfaFailedCount = 0`, and the mail `MfaLockout` (max 1 per 24 h via `LastMfaLockoutMailAtUtc`): subject "Iemand probeerde in te loggen op je Lobsy-account", body "Iemand vulde je wachtwoord goed in, maar de code uit je app 10 keer niet. Was jij dit niet? Neem dan contact op met {support}. Je wachtwoord veranderen kan binnenkort ook zelf." (05 replaces the last sentence with a reset button)
  - while paused: 403 `mfa_locked` without checking the code
  - success resets `MfaFailedCount` and `MfaLockoutUntilUtc`
  - `state` and `enroll` also answer 403 `mfa_locked` while paused (the pages show the pause)
- **Recovery codes:**
  - `TryUseRecoveryCode` normalizes the input (trim, upper-case, strip spaces and `-`) before hashing; stored hashes stay as they are.
  - A used code: `Verify` returns `usedRecoveryCode = true` and `recoveryCodesLeft = hashes.Count`, and sends `RecoveryCodeUsed` (every time, no cooldown): subject "Je hebt een herstelcode gebruikt", body "Je bent net ingelogd met een herstelcode. Je hebt er nog {n}. Was jij dit niet? Neem dan meteen contact op met {support}." (today's layout, escaped, category `RecoveryCodeUsed`, no button)
  - Web: after a recovery-code login, set a short-lived cookie `Jobsy.MfaRecoveryUsed` (data-protected payload `{ left }`, 5 min, HttpOnly, like `MfaRecoveryCodesCookie`) and redirect to `/account/mfa/recovery-codes?used=1&returnUrl=…`.
  - That page (signed in) shows: "Je bent ingelogd met een herstelcode. Je hebt er nog {n}." With n ≤ 3 add a warning: "Bijna op. Vraag een beheerder om je 2FA opnieuw in te stellen, of maak nieuwe codes." (the regeneration link comes in 04). Button "Verder naar Lobsy" → `returnUrl`. The cookie is deleted after reading.
- Never log codes, secrets or the challenge token; log `mfa.verify.failed` / `mfa.locked` / `mfa.recovery.used` with the user id.

## 01.7 CSP: no inline scripts on the 2FA pages
- New `wwwroot/js/features/account-mfa.js` (versioned `?v=`, in the asset guard), loaded on the three MFA pages with `<script src="js/features/account-mfa.js?v=…" nonce="@nonce" defer>`. The nonce comes from `CspNonce.GetOrCreate(HttpContext)` (or a small `NonceScript` component if one exists).
  - **Copy buttons:** any `[data-copy-target="#id"]` copies the target's text without whitespace and then sets a sibling `[data-copy-status]` (`role="status"`, `aria-live="polite"`) to "Gekopieerd" / "Sleutel gekopieerd" for 3 s. The text comes from a `data-copied-label` attribute so it is localized.
  - **Download:** `[data-download-codes]` builds the `.txt` from the `#mfa-recovery-list code` texts, with a header ("Lobsy herstelcodes voor {masked e-mail}", "Gemaakt op {date}", "Elke code werkt één keer."). The header text comes from `data-` attributes rendered by the page. File name `lobsy-herstelcodes.txt`.
  - **Continue:** the button renders **enabled**; the checkbox has `required` (so without JS the browser blocks submit). With JS the button is disabled until the box is ticked.
- `MfaSetup.razor`: remove `onclick`, use `data-copy-target="#mfa-manual-key"`.
- `MfaRecoveryCodes.razor`: remove the inline `<script>`. The codes appear only in the `<li><code>` elements (no JSON copy in the HTML).
- **Tests:**
  - `AuthPagesCspGuardTests` (source scan of `Components/Pages/Account/**/*.razor`, `Login.razor`, `RegisterActivate.razor` and any new auth page): no `<script` without both `src=` and `nonce=`, no ` on[a-z]+=` attribute, no `javascript:`.
  - `MfaRecoveryCodesRenderTests` (WebApplicationFactory):
    - mint the recovery cookie with the host's `IDataProtectionProvider` (`MfaRecoveryCodesCookie.ProtectorPurpose`) for a signed-in test user
    - GET the page: HTML contains the 10 codes, one `<script src="…account-mfa.js` whose `nonce` equals the nonce in the response's CSP header, no inline script, and an enabled submit button
    - a second GET shows "al getoond" and no codes
  - Extend `CspSmokePlaywrightTests` with `/account/mfa/setup` and `/account/mfa/recovery-codes`: no `securitypolicyviolation` event. If the Playwright host can't mint the cookies, cover the signed-out redirect only and say so.

## 01.8 Small fixes
- **Recovery-codes page when signed out:** `!User.Identity.IsAuthenticated` → 302 `/login` (static SSR `Response.Redirect`, as `MfaPrompt` does). Signed in with no codes and no `used=1` → keep today's "al getoond" + Continue.
- **Setup error kept:**
  - `MfaSetup` posts `source=setup` and renders `?error=` messages (`role="alert"`) above the code field, with the same QR/key (the secret is already kept by `Enroll`).
  - `MfaPrompt` keeps the `error` query when it redirects a not-enrolled user to setup.
- **"Blijf ingelogd" off:** remove `checked` in `Login.razor` L118. Add the hint "Niet aanvinken op een gedeelde computer." (`Login.RememberDeviceHint`, 5 languages). The demo login is unchanged.
- **`/register/activate`:**
  - A Web middleware `LegacyAuthRouteRedirects` (before endpoints): `GET /register/activate` with no or an empty `token` → **302** `/register`. With a token the page works as today.
  - In `TransactionalEmails.RegistrationActivation` and `TakeoverEmailVerification`, remove the "Code invoeren" button and add the line "Vul deze code in op het scherm waar je je bedrijf registreert."
  - Delete `EmailLayout.RegisterActivateUrl`.
  - Update `TransactionalEmailCatalogTests` (L117: assert **no** `/register/activate` link) and `EmailLayoutTests` L70 (remove).
- **`MfaPrompt` recovery field:** `autocomplete="off"` (not `one-time-code`).
- **`Mfa.AdminHelp`:** change the link target from `/login` to `mailto:{support}`. Text "Hulp nodig? Mail support. Een beheerder kan je 2FA opnieuw instellen."

## 01.9 Tests (beyond the ones above)
- `AuthRateLimitPartitionTests`:
  - two anonymous `local-login` calls with different trusted `X-Jobsy-Client-Ip` + a valid secret land in different partitions
  - a wrong secret or no secret → the hop IP
  - a forged `X-Jobsy-Client-Ip` without the secret is ignored
  - `TrustedClientIp.Resolve` ignores a raw `CF-Connecting-IP` when origin enforcement is off
- `AuthApiClientGuardTests`: no `new HttpClient(` in `Jobsy.Web/Auth/**` or `Jobsy.Web/Components/Pages/Account/**`.
- `LocalLoginFailureMappingTests` (Web endpoint with a stub API handler): 401 → `error=invalid`, 403 locked → `error=locked&until=`, 429 → `error=too-many&until=`, 500/timeout → `error=unavailable`; `returnUrl` stays safe.
- `LoginLockoutFlowTests` (API):
  - 5 wrong → the 5th returns 403 with `retryAtUtc` ≈ +15 min
  - the correct password while paused → 403
  - after the pause (clock seam) one wrong → 401, `FailedLoginCount == 1`
  - a second lockout within 24 h → 30 min
  - only one lockout mail in 24 h (stub mail count)
  - success resets
- `UnknownAccountLockoutTests`: an unknown e-mail gets 401 ×4 then 403 with the same body shape and fields as a known account; no mail.
- `MfaAttemptLimitTests`:
  - 5 wrong codes on one challenge → `challenge_expired`
  - 10 wrong over two challenges → `mfa_locked` 15 min + one mail
  - the same valid code twice → the second is rejected (replay)
  - a recovery code with `-`/spaces/lower-case works once; the response has `recoveryCodesLeft = 9`; `RecoveryCodeUsed` is mailed
- `MfaVerifySourceTests`: a wrong code with `source=setup` redirects to `/account/mfa/setup?error=invalid`.
- `RegisterActivateRedirectTests`: no token → 302 `/register`; with a token → 200.
- `LoginRememberDefaultTests` (bUnit or HTML): the checkbox has no `checked`.
- All existing auth tests stay green; `EfModelSnapshotTests`, `PendingModelChangesTests`, `EfMigrationDiscoveryTests`, `RoutesDocFreshnessTests`, `PageSeoTests`, `AssetVersionGuardTests`, `LocalizationParityReportTests`.

## Success criteria
- No `new HttpClient` in Web auth code. Anonymous auth calls carry the trusted client IP when the secret is configured. The API `auth` policy and login protection partition per visitor.
- `/login?error=invalid` is produced **only** by a 401 `invalid_credentials`.
- A paused account (known or unknown e-mail) shows "Even pauze" with a time. The counter resets after the pause. At most one lockout mail per 24 h.
- One PBKDF2 verify per `local-login` call on every path.
- 5 wrong codes end a challenge; 10 pause 2FA; a TOTP step can't be reused; a recovery-code use mails and shows the count.
- The recovery-codes page works with the CSP (render test), and no auth page has an inline script or `on*=` attribute (guard).
- `docs/ROUTES.md` (the `/register/activate` redirect note), `SECURITY.md` (rate limits, lockout, 2FA limits) and `CHANGELOG.md` updated.
- The PR body lists the error codes (§IA), the migration, and the config check for Dennis (`JobsyAuth:InternalClientIpSecret` on Web + API, `CLOUDFLARE_ORIGIN_SECRET`).

Done → next (only when running the full stack): `02-rollen-2fa-beleid.md`.
