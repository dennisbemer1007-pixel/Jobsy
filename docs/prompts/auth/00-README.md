# Lobsy auth pages: security hotfix + redesign of login, 2FA and password reset (Cursor run book)

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

> **Rules (repeated in every file):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/auth-*` branch; no force-push.
> - ONE PR per file into `acceptatie` (01 standalone, 02+ stacked).
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never lock, reset or change a real account on acceptatie or production while testing; use test users in the test host only. Never log passwords, codes, tokens, recovery codes or TOTP secrets.
> - Microsoft and Google logins never get an extra Lobsy 2FA step. The only exception is D7: admins can't use Google at all.

**What this stack builds.** Dennis approved the phase-1 review, the au-* mockups and all six defaults on 30-09 ("Akkoord met alles"), plus two rules: admins log in only with Microsoft (work account) or password + 2FA, never Google; SalesManager gets mandatory 2FA, Ambassadeur doesn't.
- **01 is a standalone security hotfix** that can run first and on its own:
  - per-visitor rate limits (the real client IP goes from Web to API safely; only trusted proxy/Cloudflare data)
  - honest error mapping (a 429, a lockout or an API error is never shown as "wrong password")
  - a visible lockout, "Even pauze", with the retry time
  - a failure counter that resets after the lockout; max 1 lockout mail per 24 h; the same lockout for unknown e-mails; a dummy-hash timing fix
  - 2FA: an attempt limit per user and per challenge, a replay block for used codes, a mail when a recovery code is used, plus "x codes left"
  - the CSP nonce fix for the recovery-codes and setup scripts (with a test)
  - a redirect on the recovery-codes page when you're not signed in; the 2FA setup error message is kept
  - "Blijf ingelogd" off by default
  - `/register/activate` without a token redirects to `/register`, and the broken "Code invoeren" mail buttons go away
- **Roles:** `MfaPolicy` adds SalesManager. Admins are blocked from Google (and from personal Microsoft accounts) with a clear message. Stale sessions don't dead-end.
- **Login redesign** (au-d01…d03, d10): static SSR on the public theme, providers hidden when not configured, "Bedrijf registreren" behind Werkgevers actief, "Account maken" for candidates.
- **2FA redesign** (au-d04…d06): prompt, setup and recovery codes on the public theme; "Vertrouw dit apparaat" (30 days, off by default); on mobile the otpauth link comes first; grouped recovery codes.
- **Wachtwoord vergeten** (au-d07…d09): a 30-minute link to the reset variant of `/account/wachtwoord-instellen`, with the same wording whether the account exists or not.
- **5 languages** (nl, en, pl, ro, ar) for every auth string, including today's English-only `Mfa.*` in pl/ro/ar, RTL with `dir=ltr` fields, B1 copy and accessibility.
- **Cleanup:** `/register/activate` is deleted once werkgever-aanmelding's wizard owns the code step.
- **Playwright E2E** and a stack-end report.

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-hotfix-beveiliging.md`: **standalone hotfix** (not stacked). Trusted client IP Web → API + per-visitor/per-account limits; typed login/2FA failures and honest mapping; "Even pauze" lockout with retry time; counter reset after lockout, escalation per 24 h, max 1 lockout mail/24 h; uniform lockout for unknown e-mails; dummy-hash timing; 2FA attempt limits (challenge 5, user 10), TOTP replay block, recovery-code-used mail + "x codes left"; CSP nonce fix (recovery codes + setup) with guard/render tests; recovery-codes redirect when signed out; setup error kept; "Blijf ingelogd" off; `/register/activate` no-token redirect + mail buttons removed. Migration `AddAuthHardening` | `cursor/auth-hotfix` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-rollen-2fa-beleid.md`: `MfaPolicy` + SalesManager (Ambassadeur not); admin Google block + personal Microsoft block (ensure-external 403, Web never signs in, clear message); guard for existing admin Google sessions and device refresh; stale privileged sessions without `MfaVerified` → re-login instead of a dead end; admin UI hints. Migration `AddDeviceSessionAuthMethod` | `cursor/auth-2` | `cursor/auth-hotfix` (or `origin/acceptatie` when PR 01 is merged) | `acceptatie` |
| 03 | `03-login-redesign.md`: `/login` static SSR on the public theme (Dependencies A/B, fallback `AuthPublicLayout`), au-d01/d02/d03/d10; unconfigured providers hidden; "Bedrijf registreren" behind Werkgevers actief (D); "Account maken" (C); e-mail kept after an error; Google hidden in admin context; all login strings in 5 languages | `cursor/auth-3` | `cursor/auth-2` | `acceptatie` |
| 04 | `04-2fa-redesign.md`: prompt, setup and recovery codes redesigned (au-d04…d06); "Vertrouw dit apparaat" 30 days (off by default, revoked on password/2FA reset); mobile otpauth link first; new grouped recovery codes, hash strips `-`/spaces (old codes keep working); recovery-code regeneration; register-wizard context (G). Migration `AddMfaTrustedDevices` | `cursor/auth-4` | `cursor/auth-3` | `acceptatie` |
| 05 | `05-wachtwoord-vergeten.md`: `/wachtwoord-vergeten` + reset variant of `/account/wachtwoord-instellen` (au-d07/m07/m08, au-d09/m09), 30-minute single-use link through docs/emails 01 `IOneTimeLinkService` (Dependencies E, with a fallback), uniform wording, rate limits, sessions and trusted devices revoked, 2FA still asked, lockout screen/mail link to it | `cursor/auth-5` | `cursor/auth-4` | `acceptatie` |
| 06 | `06-register-activate-opruimen.md`: delete `RegisterActivate.razor` and the activation-link code once werkgever-aanmelding 05 owns the code step (Dependencies G); keep a permanent redirect. Fallback: keep 01's redirect, record a follow-up | `cursor/auth-6` | `cursor/auth-5` | `acceptatie` |
| 07 | `07-talen-rtl-toegankelijkheid.md`: parity for all `Login.*`, `Mfa.*`, `Auth.*`, `PasswordReset.*` keys in nl/en/pl/ro/ar (pl/ro/ar B1 drafts + review list), RTL (`dir=ltr` fields, mirrored layout), B1 copy sweep, accessibility sweep (labels, `aria-live`, focus, 44 px, contrast), guard tests | `cursor/auth-7` | `cursor/auth-6` | `acceptatie` |
| 08 | `08-e2e-rapport.md`: Playwright E2E (login, error, pause, 2FA setup + codes, prompt + trust, recovery, reset, admin Google block, RTL, mobile), CSP smoke on every auth page, docs, stack-end report | `cursor/auth-8` | `cursor/auth-7` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations/resources/snapshots), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/auth-hotfix-b`, `cursor/auth-4b`).

## Pointer prompt (the only prompt needed; it runs 01 … 08)
See the report that came with this branch, or copy from here:
```
Run the Lobsy auth stack. First: git fetch origin && git show origin/docs/auth:docs/prompts/auth/00-README.md — read it completely.
Then read and execute each file in docs/prompts/auth/ on that branch strictly in the order the README's table lists (01 … 08; a/b splits where a file allows it), one file = one PR.
File 01 is a standalone security hotfix: it branches from origin/acceptatie, is not stacked, and its PR body starts with "Standalone hotfix (not stacked)". If PR 01 already exists (the hotfix was run on its own), don't redo it: reuse its branch.
File 02 branches from cursor/auth-hotfix (or from origin/acceptatie if PR 01 is already merged); every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 02, run the dependency checks in the README's "Dependencies" section and follow the fallback it prescribes for each one; say in PR 02 which case applied. Re-run the checks the README names at 03, 04, 05 and 06.
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes. Never lock or change real accounts on acceptatie/production; never log passwords, codes, tokens or TOTP secrets. Microsoft/Google logins never get an extra Lobsy 2FA step (admins can't use Google at all).
At the end report: file → branch → PR number → status, plus the dependency cases, what Dennis still has to decide (README "Needs Dennis") and anything deferred.
```

### Pointer prompt: hotfix 01 only
```
Run only the security hotfix from the Lobsy auth stack. First: git fetch origin && git show origin/docs/auth:docs/prompts/auth/00-README.md (read "How to run", §0 and §B) and git show origin/docs/auth:docs/prompts/auth/01-hotfix-beveiliging.md — read it completely.
Execute only file 01: branch cursor/auth-hotfix from origin/acceptatie (not stacked on anything), ONE PR into acceptatie whose body starts with "Standalone hotfix (not stacked)".
Fix: per-visitor rate limits with the real client IP forwarded Web → API behind the internal secret (trusted Cloudflare/proxy data only, never a raw CF-Connecting-IP); typed login and 2FA failures so a 429, a lockout or an API error is never shown as invalid credentials; the visible "Even pauze" lockout with the retry time; the failure counter reset after a lockout, escalation per 24 h and max 1 lockout mail per 24 h; the same lockout for unknown e-mails; the dummy-hash timing fix; 2FA attempt limits per challenge and per user, the TOTP replay block, the recovery-code-used mail and "x codes left"; the CSP nonce fix for the recovery-codes and setup scripts with a guard test and a render test; the recovery-codes redirect when signed out; the kept 2FA setup error; "Blijf ingelogd" off by default; the /register/activate redirect without a token and the removed "Code invoeren" mail buttons. One migration: AddAuthHardening.
Build and test; if red or a success criterion can't be met, push, open the PR as draft, stop and report.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes. Never lock or change real accounts on acceptatie/production; never log passwords, codes, tokens or TOTP secrets. Don't start file 02.
Report: branch → PR number → status, the changed endpoints and error codes, the migration name, the test list, the config Dennis must check (JobsyAuth:InternalClientIpSecret on Web and API, CLOUDFLARE_ORIGIN_SECRET) and anything deferred.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/i18n/README.md`, `docs/release-flow.md`, `SECURITY.md` and `docs/adr/0005-mfa-local-only.md`.
2. **File 01** stands alone. Run it as described in the file, whether or not the rest of the stack runs.
3. Before 02, run the **Dependencies** checks below and note the outcome (it goes into PR 02).
4. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column:
      - File 01: `git checkout -b cursor/auth-hotfix origin/acceptatie`.
      - File 02: `git checkout -b cursor/auth-2 cursor/auth-hotfix` (or `origin/acceptatie` if PR 01 is merged).
      - Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, plus the Playwright suites the file names if you can. Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. The body starts with `Standalone hotfix (not stacked)` (01) or `Stacked on #<prev PR> (<prev branch>)` (02+), then the PR body items from §0.
   6. Note the PR number, go on to the next file.
5. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely. Push what you have, open that PR as **draft** with the failure described, and don't continue. **Exception:** a file whose dependency case says "skip" (05 case E-wait, 06 case G-absent) is not a failure; see the file.
6. At the end, report the table file → branch → PR number → status (green, draft/red or skipped), plus anything deferred.
7. **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: 01 adds `AddAuthHardening`, 02 adds `AddDeviceSessionAuthMethod`, 04 adds `AddMfaTrustedDevices`; 05 adds `AddOneTimeLinks` **only** in its fallback case E-absent. No other file adds a migration. Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you (or a dependency check flips at a re-check point), `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches:** 01 standalone, 02+ stacked (see above). **ONE PR per file, always into `acceptatie`.** The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
- **Stop on red.** Run `dotnet build` + `dotnet test` after each file. If red and not fixable in scope: stop, push, open a draft PR with the failure, report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST). Re-check line numbers before editing.
- **Mockups:** branch `docs/auth`, folder `docs/mockups/auth/`. Read with `git fetch origin docs/auth && git show origin/docs/auth:docs/mockups/auth/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - Desktop (1440 × 900, full page): `au-d01-login.png`, `au-d02-login-fout.png`, `au-d03-login-pauze.png`, `au-d04-2fa-code.png`, `au-d05-2fa-instellen.png`, `au-d06-herstelcodes.png`, `au-d07-wachtwoord-vergeten.png` (form + "Kijk in je mail" side by side), `au-d09-nieuw-wachtwoord.png`, `au-d10-login-ar-rtl.png`.
  - Mobile (390 × 844, 2x): `au-m01` … `au-m06`, `au-m07-wachtwoord-vergeten.png`, `au-m08-wachtwoord-mail-verstuurd.png`, `au-m09`, `au-m10`.
  - The HTML/CSS behind them: `build.py` + `au_css.py` (tokens 1:1 from `app.css :root` + the landing warm tints). Use them for spacing, sizes and colours, **not** as code to paste.
  - "Voorbeelddata": d.devries@bakkerijzon.nl, "DV", voorbeeld@bedrijf.nl, the key `JBSW Y3DP EHPK 3PXP`, the QR, all codes, "10:45", "0:42". The "Voorbeelddata" pill is mockup-only.
  - **Where a mockup and this spec differ, this spec wins.** Known differences:
    - **au-d03 "Nieuw wachtwoord kiezen"** exists only once 05 has landed. Before that (01 and a skipped 05) the pause screen shows "Hulp nodig? Mail support" and the Microsoft/Google buttons.
    - **au-d04 "Stap 2 van 2"** eyebrow: only shown when the prompt follows a password on the same visit (always today). Fine to keep.
    - **au-d05/m05 eyebrow "Bedrijf registreren · account beveiligen"** and **au-d06 "Verder naar stap 4: je bedrijf"** are the **register-wizard variant** (04, Dependencies G). Everyone else sees no eyebrow and "Verder naar Lobsy".
    - **au-d06 recovery codes** show 8 characters in two groups (`7K2F-9QXM`): that is the new format from 04. 01 keeps today's 16-hex codes.
    - **au-d09 "Minstens 10 tekens"** → the real rule text from `RegistrationPasswordRules` (12–128 today). Never hard-code the number.
    - **au-d09 eyebrow `/account/wachtwoord-instellen`** is a label for the reviewer, not UI.
    - **"Waarom?" link** (au-d04) → `/hoe-werkt-lobsy` if that page exists, else omit the link.
    - **Lobster:** the small waving mascot peeks over the card. Use `LobsyMascot` (`Waving`, small) when Dependencies B is present, else the fallback in B.
- **Design system.** Follow `.cursor/rules/design-system.mdc`. The public auth pages (`/login`, `/account/mfa*`, `/wachtwoord-vergeten`, `/account/wachtwoord-instellen`) use the **public theme** from `docs/landing` 01 (`.pub-theme`, `pub-` BEM, its approved deviations) or the fallback in Dependencies A. New auth-only blocks use the prefix `au-` in `wwwroot/css/features/auth.css`, scoped under the theme root class. Tokens only (no hex/rgb literals; `color-mix()` on existing tokens), no inline `style=""` in `.razor`, breakpoints 640/900/1024, logical properties, tap targets ≥ 44 px, visible focus rings, AA contrast. CSS files are linked in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-x` and added to the asset-version guard (`AssetVersionGuardTests`).
- **Render mode.** Every auth page is **static SSR** (`[ExcludeFromInteractiveRouting]`, no Blazor circuit) with plain `<form method="post">` + antiforgery (`data-enhance="false"` as today). JS is progressive enhancement only, from `wwwroot/js/features/account-mfa.js` (01) / `wwwroot/js/features/auth.js` (03), loaded with the CSP nonce. **No inline `<script>`, no `on*=` attributes, no `javascript:` URLs** (CSP `script-src 'nonce-…'` + `script-src-attr 'none'`; guard test from 01).
- **Copy:** B1 Dutch, short sentences, "je", no em-dashes, no jargon ("authenticator" gets an explanation the first time: "een app op je telefoon die codes maakt"; no "token", no "KVK" in caps → "KvK", no "Entra"). Terminology: "Account maken" (candidates), "Bedrijf registreren (KvK)" (employers), "Inloggen", "Wachtwoord vergeten?", "Even pauze", "Herstelcodes", "Vertrouw dit apparaat". Error texts say what happened and what to do next.
- **Strings:** all UI text via `UiStrings` / `CultureState` (`Culture["…"]`), in **nl, en, pl, ro, ar** from the file that adds them (no English copies in pl/ro/ar for new keys). New keys live in `Jobsy.Web/Localization/UiStringsAuth.cs` (`Auth.*`, `PasswordReset.*`) and the existing `UiStringsMfa.cs` / `UiStrings.cs` (`Login.*`). nl and en are final; pl/ro/ar are B1 drafts listed in `docs/i18n/auth-review.md` (07). **pl and ar need a native review before go-live** (same rule as the emails stack). `docs/i18n/untranslated-baseline.txt` may not grow; 07 shrinks it by the `Mfa.*` rows.
- **Mails** this stack adds or changes (AccountLockout, MfaLockout, RecoveryCodeUsed, PasswordReset, PasswordChanged): until docs/emails 02 has landed (Dependencies F) they use today's `TransactionalEmails` + `EmailLayout` with every value escaped, one button at most, no secrets, no tracking. Put each in `TransactionalEmails` as its own method with a stable key so the emails stack can move it into its catalog.
- **Security and privacy (every file):**
  - Never log a password, TOTP code, recovery code, reset token, challenge token or secret. Log user id + event only (`PlatformLog` / `ILogger` with redacted e-mail via `EmailServiceStub.RedactEmail`).
  - Uniform responses: login, reset request and lockout never reveal whether an e-mail has an account (text, status code, redirect target, timing).
  - `returnUrl` only through `AuthRedirects.ResolveRequestedReturnUrl` / `SafeLocalUrl` (don't weaken them).
  - Tokens (challenge, trust cookie, reset link) are 256-bit random and stored hashed; cookies are `HttpOnly`, `Secure` (via `JobsyCookie.ShouldMarkSecure`), `SameSite=Lax`, `Path=/`.
  - GET never mutates.
  - Pages that show secrets (setup key/QR, recovery codes) send `Cache-Control: no-store` (they do today; keep it) and `Referrer-Policy: no-referrer`.
- **Docs and guards to update when routes change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (`PageSeoTests`, all auth pages noindex), `Seo/SeoEndpoints.cs` (robots), `Help/PageHelpDocs.cs` (`PageHelpDocsTests`), `BlazorPageRoleAttributesTests`, `CHANGELOG.md`, `SECURITY.md` (01, 02, 05), `docs/adr/0005-mfa-local-only.md` (02: amend for the admin provider rule).
- **Must NOT touch:**
  - the Microsoft/Google OIDC setup beyond what a file names (scopes, callback paths, nonce/correlation cookies)
  - `VerificationCodes` hashing/pepper and OTP lifetimes (only read the constants)
  - the landing, werkgever-aanmelding, emails, mijn-paspoort, admin-redesign and salesmanager stacks' files beyond the call sites a file names; their in-progress branches (`cursor/landing-*`, `cursor/emails-*`, `cursor/werkgever-aanmelding-*`, `cursor/paspoort-*`, `cursor/admin-redesign-*`, `cursor/salesmanager-*`): never branch from or merge them
  - the logged-in app layout (`MainLayout`): no `pub-`/`au-` class may affect it
- **PR description:**
  - what changed and why
  - for UI changes: screenshots nl desktop 1440 + mobile 390 of each changed page (plus ar for 03, 04, 07)
  - the new/changed URL + endpoint + error-code list
  - test list
  - "Out of scope / deferred"
- **Playwright in CI:** every new Playwright test class goes into **both** filter lists in `.github/workflows/pr-tests.yml` (excluded from the unit step, included in the smoke step), like the existing classes (`GratisDnaPlaywrightTests`, `BanenkaartV3PlaywrightTests`). `CspSmokePlaywrightTests` is in neither list today (`a611db40`); 08 checks how it runs and adds it to both if that is the intended pattern.

---

## §IA. Routes and endpoints (the contract for all files)

| Route / endpoint | What | Who | Render | SEO | Built in |
|---|---|---|---|---|---|
| `/login` | Login: Microsoft, Google, e-mail + password, errors, "Even pauze" | anonymous | static SSR form | noindex | 01 (states), 03 (redesign) |
| `POST /account/login` | Web form post → `api/auth/local-login`; maps typed failures to `/login?error=` | anonymous | endpoint | — | 01 |
| `/account/mfa` | 2FA code prompt (+ recovery code, + "Vertrouw dit apparaat" from 04) | challenge cookie | static SSR form | noindex | 01 (errors), 04 |
| `/account/mfa/setup` | 2FA setup: QR, key, otpauth link, verify | challenge cookie | static SSR form | noindex | 01 (errors), 04 |
| `/account/mfa/recovery-codes` | Show new codes once; `?used=1` = "you used a code, x left" | signed in (else 302 `/login`) | static SSR | noindex | 01, 04 |
| `POST /account/mfa/verify` | Web → `api/auth/mfa/verify`; `source=setup\|prompt` decides where an error goes | challenge cookie | endpoint | — | 01 |
| `/account/mfa/herstelcodes-vernieuwen` | New recovery codes after a TOTP confirm (D16) | signed in, `MfaVerified` | static SSR form | noindex | 04 |
| `/wachtwoord-vergeten` (GET + POST) | Reset request, then the neutral "Kijk in je mail" | anonymous | static SSR form | noindex | 05 |
| `/account/wachtwoord-instellen?t=` | docs/emails 01 page; **reset variant** when the link purpose is `PasswordReset` | anonymous (token) | static SSR form | noindex | emails 01 / 05 |
| `POST api/auth/password-reset/request` `{ email }` | Always 202, same body; sends the mail only for a local-password or no-credential account that may reset | anonymous (called by Web), rate-limited | — | — | 05 |
| `api/auth/local-login` | 200 profile / 401 `invalid_credentials` / 403 `locked_out` + `retryAtUtc` / 429 `rate_limited` + `Retry-After` | Web only | — | — | 01 |
| `api/auth/mfa/verify` | 200 profile (+ `recoveryCodesLeft`, `usedRecoveryCode`) / 401 `invalid_code` / 401 `challenge_expired` / 403 `mfa_locked` + `retryAtUtc` / 429 | Web only | — | — | 01 |
| `api/auth/ensure-external` | + 403 `provider_not_allowed` (admin + Google or personal Microsoft) | Web only (provision secret) | — | — | 02 |
| `/register/activate` | no token → 302 `/register` (01); page deleted, permanent 301 `/register` (06) | anonymous | — | noindex | 01, 06 |

`/login?error=` values (Web): `invalid`, `locked` (+ `until=<unix seconds>`), `too-many` (+ `until`), `unavailable`, `retry`, `session-expired`, `mfa-required` (02), `admin-provider` (02), plus today's provider errors. `until` only changes what the page shows; it is never trusted for security.

## §B. Review bugs → where fixed (each gets a regression test)

| Bug (origin/acceptatie @ a611db40) | Fixed in |
|---|---|
| All logins share one API `auth` bucket (20/min): Web calls `local-login`, `mfa/state`, `mfa/enroll`, `mfa/verify` with a bare `HttpClient` and no client IP (`AuthServiceCollectionExtensions.cs` L801–850, `MfaPrompt.razor` ~L105, `MfaSetup.razor` ~L100); the API `auth` policy and `LoginProtectionMiddleware` partition by `client_ip` claim or RemoteIp (`Jobsy.Api/Program.cs` L99–109, `Api/Security/LoginProtectionMiddleware.cs` L19) | 01 |
| `VacancyMapApiForwarder.ResolveVisitorIp` trusts a raw `CF-Connecting-IP` header even when origin enforcement is off (`VacancyMapProxyEndpoints.cs` L204–219) | 01 |
| A 429, lockout or API error shows "Ongeldige e-mail of wachtwoord" (`TryLocalApiLoginProfileAsync` L812–827 → `error=invalid` L313); Web `LoginProtectionMiddleware` answers a bare text 429 page | 01 |
| Lockout invisible (`AuthController` L80–82 returns the generic 401); lockout mail promises a reset that doesn't exist | 01 (visible + honest mail), 05 (reset) |
| `FailedLoginCount` only resets on success → every wrong password after a lockout locks again + another mail (`AuthController` L85–114, `LoginLockoutRules`) | 01 |
| Timing enumeration: unknown e-mail returns before PBKDF2 600k (`AuthController` L75–78) | 01 |
| No TOTP attempt limit per user/challenge (`MfaChallengeService` 10 min, `MfaController.Verify` L105–149); no replay block (`TotpAuthenticator.VerifyCode` ±1 step); recovery-code use silent | 01 |
| CSP blocks the inline `<script>` in `MfaRecoveryCodes.razor` L55 (no nonce) → Download/Copy dead, "Verder" stays `disabled` (L52); the `onclick` copy button in `MfaSetup.razor` L47 is blocked by `script-src-attr 'none'` | 01 |
| `/account/mfa/recovery-codes` for a signed-out visitor shows "Stap 2 van 2" + "Verder naar Lobsy" | 01 |
| Wrong setup code → `/account/mfa?error=invalid` → redirect to setup → error lost | 01 |
| "Blijf ingelogd" checked by default (`Login.razor` L118) | 01 |
| "Code invoeren" buttons in RegistrationActivation/TakeoverEmailVerification go to `/register/activate` without a token (`TransactionalEmails` L530/L583, `EmailLayout.RegisterActivateUrl` L76) → "Geen activatie-token in de URL." | 01 (redirect + buttons), 06 (delete) |
| `MfaPolicy` omits SalesManager; a signed-in privileged session without `MfaVerified` is sent to `/account/mfa` without a challenge → "Je inlogpoging is verlopen" dead end (`MfaEnforcementMiddleware` L57–62) | 02 |
| Admin can sign in with Google; Entra authority is `common` (personal Microsoft accounts accepted); a failed `ensure-external` still signs the user in as Candidate (`ApplyExternalJobsyProfileAsync` fallback) | 02 |
| Login is an old modal (`role=dialog`, no focus trap) on a grey backdrop, interactive Blazor; MFA pages are bare cards; no "Account maken", no "Wachtwoord vergeten?"; "Registreer via KVK" for everyone, also with Werkgevers OFF; unconfigured Google shown as a disabled button; login error without `role=alert`, far from the fields; e-mail lost after an error | 03 |
| Mobile setup can't scan its own screen; `ProvisioningUri` unused; key hidden in `<details>`; recovery codes 16 hex ungrouped; recovery field `autocomplete=one-time-code`; "Vraag een beheerder…" link goes to `/login`; `Mfa.EnterCode` unused | 04 |
| No password reset flow | 05 |
| `Mfa.*` English in pl/ro/ar (`UiStringsMfa.cs` L11–18); RTL untested; "Activatie · Lobsy" as h1; jargon | 07 (+ each file for its own new keys) |

## Decisions (Dennis "Akkoord met alles" 30-09 on the six defaults + two rules; extra defaults marked *extra*)
- **D1. Wachtwoord vergeten: yes.** Link by mail to the reset variant of `/account/wachtwoord-instellen`, valid **30 minutes**, single use, the same wording on screen whether the account exists or not. The lockout screen and mail link to it. *(Dennis, 30-09)*
- **D2. Lockout is visible** as "Even pauze" with the retry time (Europe/Amsterdam, `HH:mm`). It is shown for unknown e-mails too after the same number of failures, so it reveals nothing. The failure counter resets when the lockout ends; at most **one lockout mail per 24 h**. *(Dennis, 30-09)*
- **D3. "Vertrouw dit apparaat" 30 days** on the 2FA prompt, **off by default**. **"Blijf ingelogd" off by default** for everyone. *(Dennis, 30-09)*
- **D4. No e-mail code login on `/login`** in this stack. Candidates without a password use `/account-maken` (landing 03) for the e-mail code; `/login` links there when it exists. *(Dennis, 30-09)*
- **D5. SalesManager gets mandatory 2FA** (password users). **Ambassadeur doesn't.** Microsoft/Google logins stay 2FA-free. *(Dennis, 30-09)*
- **D6. `/register/activate`:** redirect now (01), delete after werkgever-aanmelding (06). Language switch in the header pill as on the landing pages (`/taal/{lang}`). *(Dennis, 30-09)*
- **D7. Admins log in only with Microsoft (work account) or password + 2FA, never Google.** Google is blocked for the Admin role with a clear message and hidden where the page knows it's an admin context. *(Dennis, 30-09)*
- **D8. Lockout thresholds:** 5 wrong passwords → pause. Duration by the number of lockouts in the last 24 h: 15, 30, 60, 120, then 240 min. The count of lockouts resets 24 h after the last one or on a successful login. *extra*
- **D9. Unknown e-mails** get the same counter and pause, kept process-local (like `LoginProtectionRateLimiter`), keyed by an HMAC of the normalized e-mail. No mail. *extra*
- **D10. Rate limits:** API `auth` policy per trusted visitor IP, 20/min (unchanged number); login protection per IP 20/min and per account 8/min (unchanged numbers), now on the real visitor IP. Web shows "Te veel pogingen" with the time, never a bare text page. *extra*
- **D11. 2FA limits:** 5 wrong codes per challenge (then the challenge ends: "Log opnieuw in"), 10 wrong codes in a row per user → a **2FA pause of 15 min** + a mail "Iemand kende je wachtwoord, maar niet je code. Kies een nieuw wachtwoord." (max 1 per 24 h). A used TOTP time step can't be used again. *extra* **(needs Dennis: the mail on a 2FA pause)**
- **D12. Recovery codes:** 10 codes; new format (04) 8 characters from `23456789ABCDEFGHJKLMNPQRSTUVWXYZ` shown as `XXXX-XXXX`; the hash normalizes (trim, upper-case, strip `-` and spaces), so old 16-hex codes keep working. Every use sends a mail and shows "je hebt er nog x". *extra*
- **D13. Trusted device:** a 256-bit random cookie `Jobsy.MfaTrust` (HttpOnly, Secure, Lax, 30 days) matched against a hashed row per user + device. Revoked on password change/reset, 2FA reset by an admin, recovery-code regeneration, "log uit op alle apparaten" and `SessionVersion` bumps. A trusted device skips only the 2FA code, never the password. *extra*
- **D14. Reset details:** request always shows the same "Kijk in je mail". The mail goes only to accounts with a local password or with no login method at all; Microsoft/Google-only accounts get a mail "Je logt in met Microsoft/Google" instead (same screen). A new link invalidates older unused ones. After the reset: lockout cleared, `SessionVersion` +1, all device sessions and trusted devices revoked, a "wachtwoord gewijzigd" mail, then `/login?setup=done`. 2FA stays required at the next login. Rate limit per IP and per e-mail (3 per hour per e-mail). *extra*
- **D15. Admin + Microsoft:** a personal Microsoft account (Entra tenant `9188040d-6c67-4c5b-b112-36a304b66dad`) is blocked for admins like Google. Any work/school tenant is allowed unless `JobsyAuth:AdminAllowedEntraTenants` (comma-separated tenant ids, empty by default) is set. *extra* **(needs Dennis: restrict admins to Lobsy's own tenant?)**
- **D16. Recovery-code regeneration** (04): a signed-in, 2FA-verified user can make 10 new codes after typing a current TOTP code; old codes stop working. Linked from the "x codes left" screen. *extra* **(needs Dennis: OK to add this small page)**
- **D17. Google-only user becomes Admin:** the admin role change stays possible, but the admin UI warns "Deze persoon logt in met Google. Als beheerder kan dat niet: stuur een link om een wachtwoord te kiezen of laat hem met Microsoft inloggen." The person sees the admin-provider message at the next Google login. *extra*
- **D18. Werkgevers actief OFF:** "Bedrijf registreren" is hidden on every auth page; employer-only users still log in and land where docs/mijn-paspoort 01 sends them (`/access-denied?reason=employers-off`); this stack doesn't duplicate that routing. *extra*
- **D19. Failed `ensure-external`:** `provider_not_allowed` never signs anyone in. Other failures keep today's behaviour (Candidate fallback) except for an e-mail that belongs to an Admin, which is rejected (`/login?error=unavailable`). *extra*

### Needs Dennis (flagged in the PRs and the stack-end report)
- D11 mail on a 2FA pause (default: yes).
- D15 admin Microsoft tenants (default: any work/school tenant, personal blocked).
- D16 recovery-code regeneration page (default: yes).
- Admins may use "Vertrouw dit apparaat" too (default: yes, 30 days like everyone; 04.7).
- pl/ar native review of the new strings before go-live.
- Config check: `JobsyAuth:InternalClientIpSecret` set to the same value on Web and API, and `CLOUDFLARE_ORIGIN_SECRET` set on both (01 needs them for per-visitor limits; without them 01 falls back to per-hop limits and logs a startup warning).

## Dependencies (check before 02; say in PR 02 which case applied)
- **A. Public theme (`docs/landing` 01).** Check: `git grep -n "class PublicRoutes\|IEmployersSwitch" origin/acceptatie -- Jobsy.Web` and `git ls-tree -r --name-only origin/acceptatie -- Jobsy.Web/Components/Layout/PublicLayout.razor Jobsy.Web/wwwroot/css/features/public-theme.css`.
  - **Present:** auth pages use `@layout PublicLayout`, `.pub-theme`, `pub-` primitives (`pub-card`, `pub-btn--primary|secondary`, `pub-chip`, `pub-eyebrow`, `pub-lang`), `PublicRoutes` and the `/taal/{lang}` switch. `auth.css` only adds `au-` blocks under `.pub-theme`.
  - **Absent:** 03 adds `Components/Layout/AuthPublicLayout.razor` (header: logo → `/`, language menu with links to today's culture switch, "Account maken" when C is present; one cookie banner as today) with root class `au-theme`, and defines the few warm tints it needs in `auth.css` via `color-mix()` on existing tokens (same recipes as the mockup `au_css.py`), scoped `.au-theme`. Add **`docs/auth-followups.md`**: "landing 01: `AuthPublicLayout` → `PublicLayout`, `.au-theme` → `.pub-theme`, drop the duplicated tints, use `PublicRoutes`." Never create landing's names (`PublicLayout`, `PublicRoutes`, `pub-*`).
  - Re-check at 03, 04 and 05.
- **B. Mascot (`docs/landing` 02).** Check: `git grep -n "class LobsyMascot\|LobsyMascot.razor" origin/acceptatie -- Jobsy.Web`. **Present:** `<LobsyMascot Pose="Waving" Size="Small" />`. **Absent:** `Components/Auth/AuMascot.razor` with today's `BrandImages.MascotWebp*` and a `au-mascot--peek` class; follow-up line. Re-check at 03.
- **C. Candidate account page (`docs/landing` 03).** Check: `git grep -n "account-maken" origin/acceptatie -- Jobsy.Web`.
  - **Present:** keep landing 03's `/login` additions (its "Nieuw bij Lobsy? Maak gratis account" link and any e-mail-code sign-in link) inside the new layout, with `PublicRoutes.CreateAccount`. Header button "Account maken".
  - **Absent:** no "Account maken" link. The footer line becomes "Nieuw bij Lobsy? Log in met Microsoft of Google. Dan maken we je account." (that is today's behaviour of `ensure-external`). Follow-up line. Never create `/account-maken`.
  - Re-check at 03.
- **D. Werkgevers actief (`docs/mijn-paspoort` 01 / landing 01 seam).** Check: `git grep -n "IEmployersSwitch\|PlatformFeature.Employers\|EmployersEnabled" origin/acceptatie -- Jobsy.Web Jobsy.Core Jobsy.Infrastructure`.
  - **Present:** "Bedrijf registreren (KvK)" renders only when the switch says ON (use `IEmployersSwitch` if present, else `IFeatureFlags`/`PlatformFeature.Employers`). Keep paspoort's employer-only routing as is.
  - **Absent:** the link always renders; follow-up line "paspoort 01: gate `Auth.RegisterCompany` on the switch".
  - Re-check at 03.
- **E. One-time links + set-password page (`docs/emails` 01).** Check: `git grep -n "interface IOneTimeLinkService\|OneTimeLinkPurpose" origin/acceptatie -- Jobsy.Core` and `git ls-tree -r --name-only origin/acceptatie -- Jobsy.Web/Components/Pages/Account/SetPassword.razor`.
  - **Present:** 05 adds `OneTimeLinkPurpose.PasswordReset` (next free value) and a reset variant of `SetPassword.razor`; no migration (the enum is stored as int). 02 hides Google on that page for admin invites.
  - **E-wait (absent on acceptatie, but `git ls-remote --heads origin cursor/emails-hotfix` exists):** 05 is **skipped**. Don't copy the open PR. Write `docs/auth-followups.md`: "Run auth 05 after emails 01 merges", keep the "Wachtwoord vergeten?" link and the pause-screen button hidden (`AuthFeatures.PasswordResetAvailable = false`), and continue with 06 branching from `cursor/auth-4`. Report 05 as "skipped (waiting on emails 01)".
  - **E-absent (neither code nor branch):** 05 implements **exactly** emails 01.3.1 (entity `OneTimeLink`, table `OneTimeLinks`, migration `AddOneTimeLinks`, `IOneTimeLinkService` with `CreateAsync`/`PeekAsync`/`ConsumeAsync`, `OneTimeLinkRules`) and the page shell of 01.3.2 (`Pages/Account/SetPassword.razor` at `/account/wachtwoord-instellen`, `GET api/account/setup-link`, `POST api/account/setup-password`) with the names written there, plus `PasswordReset`. Add to `docs/auth-followups.md`: "emails 01: `OneTimeLinks` + `/account/wachtwoord-instellen` exist from auth 05; reuse them, add only `SetPassword`/`ApiKeyReveal` behaviour."
  - Re-check at 05.
- **F. Emails renderer (`docs/emails` 02/04/07).** Check: `git grep -n "interface ITransactionalMailer\|class EmailDocument\|class EmailStrings" origin/acceptatie -- Jobsy.Core Jobsy.Infrastructure`. **Present:** new auth mails are registry entries (kind E, `RecoveryCodeUsed`/`MfaLockout`/`PasswordChanged` reason `Security`; `PasswordReset` kind S-with-button, reason `AccountRequest`) with `EmailStrings` keys in 5 languages; AccountLockout copy follows emails 07 if it landed. **Absent:** today's `TransactionalEmails`/`EmailLayout` (§0 Mails) + follow-up line. Re-check at 05.
- **G. Werkgever-aanmelding wizard (`docs/werkgever-aanmelding` 05, D18).** Check: `git grep -n "register/koppelen\|register/bedrijf\|register/verifieren" origin/acceptatie -- Jobsy.Web` and `git grep -n "BuildActivationUrl\|RegisterActivateUrl" origin/acceptatie -- Jobsy.Core Jobsy.Infrastructure Jobsy.Api`.
  - **Present (05 landed):** 04 shows the wizard variant (eyebrow, stepper "Account · Beveiligen · Herstelcodes", "Verder naar stap 4: je bedrijf") when the MFA `returnUrl` starts with `/register/`. 06 deletes `/register/activate` and the activation-link code.
  - **Absent:** 04 has no wizard variant (generic copy); 06 is **skipped** with a follow-up (01's redirect stays). Report 06 as "skipped (waiting on werkgever-aanmelding 05)".
  - Re-check at 04 and 06.
- **H. Admin redesign (`docs/admin-redesign` 01/07).** Check: `git grep -n "interface IAdminAuditLog\|class AdminNavCatalog" origin/acceptatie -- Jobsy.Core Jobsy.Web`. **Present:** audit rows for admin 2FA reset, trusted-device revoke and the D17 warning go through `IAdminAuditLog`; the D17 hint sits on the admin-redesign user detail page. **Absent:** `PersonalDataAccessLog`/`PlatformLog` as `AdminController.ResetMfa` does today; the hint goes on today's admin user page. Re-check at 02 and 04.
- **I. Salesmanager (`docs/salesmanager` 01).** Check: `git grep -n "SalesLegacyRoutes\|/sales/start" origin/acceptatie -- Jobsy.Web`. **Present:** after 2FA setup a SalesManager lands on `returnUrl` or `/sales/start`. **Absent:** today's post-login route. Either way the SalesManager invite/set-password path (emails 01) ends in the normal login → 2FA setup. Check at 02.
- **Recommended landing order:** 01 as soon as possible (security, and it doesn't wait for anything). Then emails 01 before auth 05, and landing 01–03 before auth 03 if they're close. Not a hard requirement: every case above has a fallback. Never branch from an unmerged branch of another stack.
