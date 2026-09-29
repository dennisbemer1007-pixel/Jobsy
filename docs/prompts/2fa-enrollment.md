# Prompt: forced 2FA enrollment for local password logins (fix/2fa-enrollment)

Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123 (`.cursor/rules/shortcut-123.mdc`). Never push to `main` or `acceptatie`, and never force-push. Branch name: `fix/2fa-enrollment`.

## Problem (verified 29-09-2026 on `origin/acceptatie` 20dc8d5 and `origin/main` 9a2c0d4)

Privileged users (Admin, BranchManager, RegionalManager, EnterpriseManager, Intermediary; see `Jobsy.Core/Security/MfaPolicy.cs`) who never set up an authenticator are stuck after entering their password. They land on `/account/mfa`, which only says "Vul de code uit je authenticator-app in…" and shows code and recovery-code fields. They have no code, so they cannot log in. This affects production (lobsy.nl) and acceptatie. It was reproduced on acceptatie with `ondernemer@jobsy.local`.

Root cause chain:
1. **Mandatory MFA reuses an old feature flag.** PR #315 (commit 2288aec, merged into `main` 26-09-2026 19:27 CEST, "H2: Login protection — lockout, MFA…") made MFA mandatory per role (`AuthController.cs:134` for password login, `:299` for external login). Enrollment then goes through `MfaController.Enroll` (`Jobsy.Api/Controllers/MfaController.cs:46-81`). That method returns **400 when `features.AuthenticatorEnabled` is false** (`:58-62`). That flag is the old **"Authenticator stub aan"** flag for candidate applications (`SettingsAdmin.razor:69-72`, `ApplicationsController.cs:707-715`). Its config default is **`false`** in `Jobsy.Api/appsettings.json:6` (`JobsyFeatures:AuthenticatorEnabled`, since 25-07). A `PlatformFeatureSettings` row overrides it (`PlatformFeatureService.cs:160`). So enrollment is disabled by default.
2. **The error is swallowed.** `Jobsy.Web/Components/Pages/Mfa.razor:73-83` only shows the secret when the enroll call succeeds. On any non-success response (400, 401, 429) it silently falls back to the "enter your code" text. `MfaController.Verify` then always fails, because there is no secret. The user is deadlocked.
3. **Recovery codes are discarded.** They are generated in `MfaController.Verify` (`:118-121`) and returned, but the Web handler `/account/mfa/verify` (`Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs:349-392`) throws them away. They are never shown.
4. **The page is not a proper page:**
   - raw inline-styled markup (`<main class="container" style=…>`, bare `<label>`/`<input>`/`<button>`, no design-system classes; that is why it looks unstyled);
   - no QR code;
   - hardcoded Dutch strings;
   - `?error=invalid` is ignored;
   - it runs under the global `InteractiveServer` render mode (`App.razor:225-230`), so `OnInitializedAsync` runs twice (prerender and circuit) and calls `enroll` twice through the shared Web IP rate-limit bucket (`Jobsy.Api/Program.cs:99-108`, `auth` policy 20/min).
5. **External logins are forced into Lobsy MFA too.** Microsoft (Entra, `AddOpenIdConnect(EntraScheme)`) and Google logins go through `api/auth/ensure-external` → `AuthController.cs:299`, which also forces Lobsy MFA and redirects to `/account/mfa` (`AuthServiceCollectionExtensions.cs:1156-1166`). Those providers already enforce MFA.
6. **Route-level enforcement is narrow.** `Jobsy.Web/Security/MfaEnforcementMiddleware.cs` only covers `/admin` for the Admin role.

## Goal

1. Normal e-mail (or short name) plus password login, as before.
2. **Local password login + role requires MFA + no authenticator yet** → forced **enrollment**:
   - show a QR code and the manual key;
   - verify one 6-digit code;
   - show 10 recovery codes **once**, with "Ik heb ze bewaard" before continuing;
   - only then create the session and redirect to the original `returnUrl`.
3. **Local password login + authenticator already set up** → code prompt (TOTP or recovery code), as today but styled.
4. **Any external identity provider login** (Microsoft/Entra, Google, any future OIDC scheme) **never** gets Lobsy's own enrollment or code prompt. The IdP handles MFA.
   - Rule for accounts that have both a local password and an external login: **the Lobsy 2FA requirement applies only to the sign-in method used.** Signing in with the password requires Lobsy TOTP (enrolling if needed). Signing in with Microsoft/Google skips it.
   - Document this rule in `docs/ARCHITECTURE.md` (auth section) and in an ADR `docs/adr/NNNN-mfa-local-only.md`.
5. **Server-side enforcement:**
   - No auth cookie is issued before TOTP is verified. Keep the current challenge-only state and do not sign in on password success.
   - Extend `MfaEnforcementMiddleware` so that **every** authenticated request by a user whose role is in `MfaPolicy` and whose `auth_method` is a local password must carry `MfaVerified=1`. Otherwise redirect to `/account/mfa` (HTML) or return 401 (for `/api` proxies).
   - Sessions with `auth_method` external (Entra/Google) are exempt.
   - Allowlist: `/account/*`, `/login`, `/logout`, static assets, `/_blazor`, `/_framework`, `/health`, `/healthz`.
6. **Admin can reset 2FA for another user.** See section C.
7. **Remove the dependency on the "Authenticator stub" flag.** MFA enrollment must never depend on `JobsyFeatures:AuthenticatorEnabled` or `PlatformFeatureSettings.AuthenticatorEnabled`. Keep that flag only for the candidate-application stub (`ApplicationsController`), and relabel it in `SettingsAdmin.razor` as "Authenticator bij sollicitatie (stub)" so nobody confuses it with login 2FA.

## Implementation

### A. API (`Jobsy.Api`)

- **`MfaController.Enroll`:**
  - Delete the `features.AuthenticatorEnabled` gate.
  - If the user is already enrolled (`AuthenticatorEnabled == true`), return **409** `{ enrolled: true }` and never return the secret again.
  - Otherwise create or keep a pending secret. Return `{ secret, provisioningUri, qrSvgDataUri }`:
    - generate the QR server-side with the already referenced **QRCoder 1.6.0** (`Directory.Packages.props:41`, used in `Jobsy.Infrastructure`);
    - SVG, base64 `data:image/svg+xml` URI (CSP `img-src` already allows `data:`);
    - put the QR helper in `Jobsy.Infrastructure/Security/TotpQrCode.cs`.
  - Never log the secret.
- **New `POST api/auth/mfa/state`** (anonymous, challenge token in body, `auth` rate limit). Returns `{ enrolled: bool, email }` for the challenge user. The Web decides the page mode from this, **not** from enroll success.
- **`MfaController.Verify`:**
  - When `!user.AuthenticatorEnabled`, **only accept a TOTP code** (recovery codes don't exist yet).
  - Keep generating 10 recovery codes on first successful verification and return them in `RecoveryCodes`.
  - Store the codes only as SHA-256 hashes (as now).
- **`MfaChallengeService`:** add `bool LocalPassword` to `MfaChallenge` so the API can assert challenges only come from the password flow.
- **`AuthController.EnsureExternal` (`:299`):**
  - When `provider` is not null (entra, google or any other normalized provider), **skip** `RequiresMfa`. Return the normal session response with `AuthMethod = "external:{provider}"`.
  - Never create an MFA challenge for external logins.
  - Keep lines 134-146 (password login) as is, except the challenge carries `LocalPassword = true`.
- **Keep the voluntary case.** "User voluntarily enabled authenticator → prompt on password login" stays, for any role including candidates.
- **`SupportAccessService.cs:70`:** treat a session with an external auth method as MFA-satisfied, just like `mfaVerifiedInSession`.

### B. Web (`Jobsy.Web`)

- **Split `Mfa.razor` into three static-SSR pages**, all `[AllowAnonymous]`, using the `login-modal login-card` layout pattern from `Login.razor` (the same centered card on mobile and desktop):
  - **`/account/mfa/setup`** (enrollment):
    - step indicator "Stap 1 van 2";
    - QR `<img alt="QR-code voor je authenticator-app">`;
    - "Lukt scannen niet? Vul deze sleutel in" as a `<details>` with the key in monospace, grouped in blocks of 4, plus a copy button (`icon-btn` with `aria-label`);
    - short instructions for Microsoft Authenticator and Google Authenticator (B1 Dutch);
    - code field (`inputmode="numeric" autocomplete="one-time-code" pattern="[0-9]{6}"`);
    - primary button "Bevestigen".
  - **`/account/mfa/recovery-codes`** (step 2, show once):
    - 10 codes in a monospace grid;
    - "Download als .txt" (client-side blob) and "Kopieer";
    - required checkbox "Ik heb mijn herstelcodes veilig bewaard";
    - primary button "Verder naar Lobsy".
    - The codes travel from `/account/mfa/verify` to this page in a **Data-Protection-encrypted, HttpOnly, SameSite=Lax cookie with a 5-minute max-age**. The page deletes that cookie when it renders, so a reload does **not** show them again (it shows "Je herstelcodes zijn al getoond").
  - **`/account/mfa`** (code prompt for enrolled users):
    - TOTP field;
    - `<details>` "Geen toegang tot je app? Gebruik een herstelcode" with the recovery field;
    - primary button "Inloggen";
    - a text link "Hulp nodig? Vraag een beheerder je 2FA te resetten".
  - `/account/mfa` must redirect to `/account/mfa/setup` when `state.enrolled == false`.
  - Show real error messages. Map API 400/401/409/429 to a friendly text (expired → "Je inlogpoging is verlopen. Log opnieuw in." plus a link to `/login`; 429 → "Te veel pogingen…").
  - Render `?error=invalid`.
- **Mark all `/account/mfa*` pages `[ExcludeFromInteractiveRouting]`.** In `App.razor`, use `HttpContext.AcceptsInteractiveRouting()` to render `<Routes>`/`<HeadOutlet>` without a render mode for them, so they are plain SSR forms. `OnInitializedAsync` must run once, and antiforgery must work. Keep every other page on `InteractiveServer` exactly as now.
- **`POST /account/mfa/verify`:**
  - On first enrollment (`RecoveryCodes.Length > 0`), sign the user in (MFA verified), set the one-time recovery-code cookie and redirect to `/account/mfa/recovery-codes?returnUrl=…`.
  - Otherwise redirect to the stored return URL.
  - When sessions start, set `auth_method` to `password+totp` or `external:{provider}`.
- **Extend `MfaEnforcementMiddleware`** as described in Goal 5. Add an `MfaPolicy.IsRequired(role)` check based on the role claim.
- **Styles:**
  - New file `wwwroot/css/features/account-mfa.css`, linked in `App.razor` with its own `?v=`.
  - Follow `.cursor/rules/design-system.mdc`: tokens only, `--radius`/`--radius-sm`, max 2 font weights, mobile-first (640/900/1024), logical properties for RTL, focus-visible, tap targets ≥44 px, no inline `style=`, no new colors.
  - Reuse `login-card`, `login-submit` (primary), `btn-compact` (secondary) and `status-pill`.
- **Copy:**
  - All strings via `@Culture["Mfa.*"]` in a new `Localization/UiStringsMfa.cs` with **nl (B1)** and **en**, and the same keys in pl/ro/ar (English fallback text is acceptable).
  - Update the localization parity baseline and `docs/ROUTES.md` (these tests currently fail on `acceptatie` when you forget).
  - Suggested NL copy:
    - "Beveilig je account" / "Voor jouw rol is een extra stap nodig: een code uit een authenticator-app, zoals Microsoft Authenticator."
    - "Scan de QR-code met je app" / "Vul de 6 cijfers uit je app in".
    - "Bewaar je herstelcodes" / "Heb je je telefoon niet bij je? Dan kun je met één van deze codes inloggen. Elke code werkt één keer."
    - "Vul de code uit je authenticator-app in".

### C. Admin reset 2FA (`Jobsy.Api` + `UsersAdmin.razor`)

- **API:** `POST api/admin/users/{userId:guid}/mfa/reset` on the existing `AdminController` (`[Authorize(Policy = JobsyPolicies.RequireAdmin)]`). Body: `{ reason: string (required, 5–500 chars), confirmCode?: string }`.
- **Rules:**
  - The acting admin's session must be MFA-verified (`MfaVerified` claim or an external-provider session). Otherwise return 403.
  - `userId == actingAdminId` → 403 "Reset je eigen 2FA via je profiel" (no silent self-reset).
  - Preferred: when the acting admin uses a local password session, require `confirmCode` (a fresh TOTP from the admin's own authenticator) and verify it. That is the re-auth step.
- **Effect:**
  - `AuthenticatorSecret = null`, `RecoveryCodesHash = null`, `AuthenticatorEnabled = false`, `AuthenticatorEnrolledAtUtc = null`;
  - `SessionVersion++`;
  - `IDeviceSessionService.RevokeAllAsync(userId, "mfa-reset")`, so all sessions of that user are invalidated;
  - the user enrolls again at the next password login.
- **Audit:** log via `IPersonalDataAccessLogger.LogAsync(new PersonalDataAccessEntry(actorId, actorRole, Resource: "user.mfa", Action: "reset", SubjectUserId: userId, Reason: reason, IpAddress: …))`. Also show it in `PersonalDataAccessLogAdmin`. Never include the secret or codes in logs or responses. Response: `204`.
- **Admin users list** (`GET api/admin/users`): add a read-only `MfaStatus` field: `"enrolled" | "not-enrolled" | "external-only"`. It is a status, not the secret.
- **UI (`Jobsy.Web/Components/Pages/Admin/UsersAdmin.razor`):**
  - In the per-user `RowActionsMenu`, add "2FA resetten". It is only visible when `MfaStatus == "enrolled"` and the user is not the current admin.
  - It opens `LobsyFriendlyDialog` with the explanation "Deze gebruiker moet bij de volgende inlog de authenticator opnieuw instellen. Alle sessies worden beëindigd.", a required reason field and, for local-password admins, a "Jouw authenticatorcode" field.
  - Then show a success toast.
  - Show the MFA status as a `status-pill` (max 2 badges per row).

### D. Demo accounts and existing users

- Seeded privileged demo accounts (`DemoUsersSeeder.cs`: `admin@`, `ondernemer@`, `intermediair@`, manager accounts) have no authenticator. They must now get the **enrollment flow** after their password, not a dead end.
- **Do not** add any bypass, backdoor or known shared TOTP secret to seeds or config.
- A pending, unconfirmed secret left by the old flow (`AuthenticatorSecret` set, `AuthenticatorEnabled == false`) must simply be reused or replaced by the setup page.

## Tests (xUnit in `Jobsy.Tests`, all must pass locally; do not weaken existing tests)

1. **Forced enrollment.** Password login as Admin with no secret:
   - the response has `RequiresMfa`, `MfaEnrolled=false`;
   - `mfa/state` → `enrolled=false`;
   - `mfa/enroll` → 200 with secret, `otpauth://` URI and a QR data URI, **even when `JobsyFeatures:AuthenticatorEnabled=false`** and when a `PlatformFeatureSettings` row has it false. This is the regression test for the root cause.
2. **Verify on first enrollment.** A correct TOTP (computed with `TotpAuthenticator`) → `AuthenticatorEnabled=true`, exactly 10 recovery codes returned. A second enroll call → 409 and no secret.
3. **Recovery codes shown once.** The Web `/account/mfa/verify` sets the one-time cookie. The first GET `/account/mfa/recovery-codes` shows 10 codes. A second GET does not show codes.
4. **Enrolled user.** Password login → `/account/mfa` code prompt, and never the setup page or the secret. A wrong code → error shown, no auth cookie. A recovery code works once, and a second use fails.
5. **Server-side enforcement:**
   - with only a challenge cookie (password OK, no TOTP), GET `/admin`, `/branch`, `/employer/vacancies` and a proxied API call → redirect to `/account/mfa` or 401, never 200;
   - a forged auth cookie without `MfaVerified` for a privileged local-password session → redirected;
   - a Candidate without an authenticator is unaffected.
6. **External login skips Lobsy 2FA.** `ensure-external` with provider `entra`, `microsoft`, `google` and an arbitrary `oidc` for an Admin with or without an authenticator → no `RequiresMfa`, no challenge, and the session gets `auth_method` external. The middleware lets that session into `/admin`. The same user via password → TOTP required (mixed-account rule).
7. **Admin reset:**
   - a non-admin (Candidate, BranchManager, SalesManager) calling `POST api/admin/users/{id}/mfa/reset` → 403;
   - anonymous → 401;
   - an admin without an MFA-verified session → 403;
   - self-reset → 403;
   - a valid reset clears the secret, codes and flag, bumps `SessionVersion`, revokes device sessions and writes exactly one `PersonalDataAccessLog` row with actor, subject, reason and time;
   - the response and log contain no secret;
   - the next password login of the target → enrollment.
8. **Static SSR.** `/account/mfa*` pages are excluded from interactive routing (prerendered HTML contains no `<!--Blazor:` component markers for the page). Enroll is called at most once per page view (fake API handler counts calls).
9. **UI guards.** No `style="` in the new `.razor` files. All strings resolve via `Culture["Mfa.*"]` (nl + en present). The parity baseline and `docs/ROUTES.md` are updated.
10. **Optional soft-skip Playwright** (only when `JOBSY_E2E_BASE_URL` is set, mobile 390×844): the demo `ondernemer@jobsy.local` → setup page shows a QR `<img>` and the manual key. Compute a TOTP from the key, submit → recovery-codes page → continue → lands on the branch dashboard with no console errors. Only run against acceptatie, never production.

## Success criteria

- A privileged local user without 2FA can log in on first try by scanning a QR, entering one code and saving recovery codes, with a styled mobile-friendly page (matches `login-card`).
- Enrolled users get a styled code prompt. Microsoft and Google users are never asked for Lobsy 2FA.
- There is no path to a privileged page with a local password session that lacks `MfaVerified`.
- Admins can reset another user's 2FA with a reason, the reset is audited, and sessions are revoked.
- MFA no longer depends on the "Authenticator stub" flag.
- CI `test` is green for the new and touched tests. Also fix any test this PR breaks. Mention in the PR description any pre-existing red jobs you did not touch.

## Scope guard

Do not touch the banenkaart/map files, `app-core.js`, `jobMap.js`, filter-sheet, cookie padding, Match, DNA/gratis-dna or Kandidaatinzichten code. Only change `App.razor` for:
- the render-mode switch for excluded pages;
- the new `account-mfa.css` link.

PR description: root cause (points 1–6 above), screenshots of the setup, recovery-codes and prompt pages (mobile and desktop), the mixed-account rule, and test list.
