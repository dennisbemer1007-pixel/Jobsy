# 03. Login redesign: `/login` as a static SSR page on the public theme

Read `00-README.md` first (§0, §IA, Decisions D3, D4, D7, D18, Dependencies A, B, C, D). Branch `cursor/auth-3` from `cursor/auth-2`. **Stacked.** Re-run Dependencies A, B, C and D first and put the cases in the PR.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/auth-3`; no force-push.
> - ONE PR into `acceptatie`; the body starts with `Stacked on #<PR 02> (cursor/auth-2)`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start file 04.
> - Never lock, reset or change a real account on acceptatie or production while testing. Never log passwords, codes, tokens or TOTP secrets.
> - Microsoft and Google logins never get an extra Lobsy 2FA step (admins can't use Google, 02).

| | |
|---|---|
| Branch | `cursor/auth-3` |
| PR title | `feat(auth): new login page on the public theme (static SSR), honest states, hidden unconfigured providers, Werkgevers-aware links` |
| Mockups | `au-d01`/`au-m01` (login), `au-d02`/`au-m02` (error), `au-d03`/`au-m03` (pause), `au-d10`/`au-m10` (ar RTL) |
| Migration | none |
| Split seam | if > ~1.500 lines: **03a** = layout fallback (Dependencies A/B absent) + `auth.css` + `AuMascot`; **03b** = the page on `cursor/auth-3b` |

## Goal
The login looks and feels like the public site. It works without a Blazor circuit and says clearly what happened. It only offers what works: providers that are configured, "Bedrijf registreren" only when employers are on, "Account maken" when it exists.

## 03.1 Today (verify first)
- `Login.razor`:
  - an interactive component (no `[ExcludeFromInteractiveRouting]`) rendering `login-modal` with `role="dialog" aria-modal="true"`, a backdrop link and × to `/`
  - `GratisDnaRegisterBox`, the providers (disabled buttons when not configured, `ExternalAuth.IsEntraConfiguredAsync`/`IsGoogleConfiguredAsync`), the form, "Nieuw bedrijf op Lobsy? Registreer via KVK", "Terug naar de banenkaart" and the `/ontdek` quiet link
  - 01 and 02 added the states `invalid`, `locked`, `too-many`, `unavailable`, `mfa-required`, `admin-provider`
  - the `?ref=` Ambassadeur cookie and the session-expired draft cleanup (`OnAfterRenderAsync` JS) live here too
- CSS: `login-*` classes in `app.css` (shared with other pages; check with `rg "login-card|login-modal|login-form" Jobsy.Web`).
- Language: today's interactive `LanguageSelector`; static switch only if Dependencies A is present (`/taal/{lang}`).

## 03.2 Page structure (au-d01 / au-m01)
- `Login.razor` becomes **static SSR**: `[ExcludeFromInteractiveRouting]`, `@layout PublicLayout` (A present) or `AuthPublicLayout` (A absent, see README).
  - No `role="dialog"`, no backdrop, no ×. A normal page with `<main>`, one `<h1>`, and focus not forced onto the h1.
  - Anything that needed interactivity moves:
    - `?ref=` cookie: already server-side, keep it.
    - Session-expired draft cleanup: `wwwroot/js/features/auth.js` (nonce, `defer`), triggered by a `data-clear-drafts` attribute on `<main>` when `error=session-expired`. Keep the `lobsySessionIdle.clearDrafts` behaviour; if that global lives in a script that isn't loaded on static pages, load the smallest script that defines it.
    - Antiforgery: `GetAndStoreTokens` in SSR as today.
- **Header** (layout): logo → `/`, language pill, and on desktop the secondary pill "Account maken" (C present) → `PublicRoutes.CreateAccount`. Mobile: logo + language only.
- **Card** (`au-card`, radius 36, `--shadow-soft` recipe, max 460 px; full width on mobile with 14 px gutter): the small waving mascot peeks over the top-right edge (`LobsyMascot` Waving Small, or `AuMascot`, `alt=""`, decorative).
  - Eyebrow "Welkom terug" (coral, 600).
  - h1 "Inloggen bij Lobsy".
  - Lead "Kies hoe je wilt inloggen."
  - Error/status block (03.4).
  - Providers: full-width secondary pills with the brand icons, Microsoft first, then Google.
    - **Only providers that are configured** are rendered (no disabled buttons).
    - Google is hidden in admin context (02).
    - None configured → no provider block and no divider.
  - Divider "of met je e-mailadres".
  - The form (03.3).
  - Card footer: "Nieuw bij Lobsy? **Maak gratis een account**" (C present; C absent → "Nieuw bij Lobsy? Log in met Microsoft of Google. Dan maken we je account."), and "Voor werkgevers: **Bedrijf registreren (KvK)**" → `/register`, only when Dependencies D says employers are on (or always when D is absent).
  - Below the card: "Terug naar de banenkaart" → the banenkaart path (`AuthRedirects.BanenkaartPath` or `PublicRoutes.Banenkaart`) and the quiet `/ontdek` link (keep `GratisDna.QuietLink.*`).
- `GratisDnaRegisterBox` stays when `?van=ontdek` or the test answers exist (as today), placed above the providers. Don't change its logic.
- Organic blobs are decorative background (CSS only, `aria-hidden`, hidden below 640 px except one).

## 03.3 The form
- `<form method="post" action="/account/login" data-enhance="false" novalidate>` with antiforgery + `returnUrl` hidden.
  - **E-mail:**
    - `<label for="login-email">E-mailadres</label>`, `<input id="login-email" name="email" type="email" autocomplete="username" inputmode="email" autocapitalize="none" autocorrect="off" spellcheck="false" required dir="ltr">`
    - Keep bare usernames working (`LoginIdentity.Normalize`, e.g. "twalieb"): use `type="text"` if `type="email"` would block them; check `LoginIdentityTests` and say which you chose.
    - Placeholder "naam@voorbeeld.nl".
  - **Password:**
    - label row with "Wachtwoord" and, right-aligned, "Wachtwoord vergeten?" → `/wachtwoord-vergeten`, **only when** `AuthFeatures.PasswordResetAvailable` is true (a constant in `Jobsy.Web/Auth/AuthFeatures.cs`, false until 05 sets it)
    - `<input type="password" autocomplete="current-password" required dir="ltr">`
    - "Toon" toggle: a `<button type="button" aria-pressed="false" aria-controls="login-password">` with an eye icon, handled by `auth.js`; hidden without JS
  - **"Blijf ingelogd op dit apparaat"** checkbox, unchecked (D3), with the hint "Niet aanvinken op een gedeelde computer."
  - Primary pill "Inloggen" (full width, 52 px). The antiforgery-missing state keeps today's disabled button + retry text.
- **E-mail kept after an error:**
  - On `invalid`, `locked` or `too-many`, the Web endpoint sets a data-protected cookie `Jobsy.LoginHint` (5 min, HttpOnly, Lax, payload = the normalized e-mail).
  - The page prefills the field and deletes the cookie. Never put the e-mail in the URL.
  - Focus goes to the password field when there's an error (`autofocus` on that field only in the error case).

## 03.4 States (au-d02 / au-d03)
- One block above the providers, with an icon + bold title + one sentence:
  - `invalid`: danger tint, `role="alert"`, title "Dat klopt niet helemaal", text "Het e-mailadres of wachtwoord is niet goed. Probeer het nog eens." The password field gets `aria-invalid="true"` and `aria-describedby` pointing at the block.
  - `locked`: the **pause card replaces the form** (au-d03):
    - emoji circle ⏸️ (sun), h1 "Even pauze", the text from 01, and a sun-tinted status box "Probeer het weer om {HH:mm}"
    - then (05 present) primary "Nieuw wachtwoord kiezen" → `/wachtwoord-vergeten`
    - then the configured providers as secondary pills
    - footer "Was jij dit niet? …" + "Hulp nodig? Mail support"
    - link "Terug naar inloggen" → `/login` (keeps `returnUrl`)
  - `too-many`: sun tint, `role="status"`, the text from 01; the form stays.
  - `unavailable`: sky tint, `role="status"`.
  - `session-expired`: sky tint, `role="status"`, "Je sessie is verlopen. Log opnieuw in. Je komt terug waar je was."
  - `mfa-required`, `admin-provider` (02) and the provider errors: the same component, with the tint by severity.
  - After `?setup=done` (emails 01 / 05): mint tint, "Je wachtwoord is opgeslagen. Log nu in."
- `LoginStatusBlock.razor` (`Components/Auth/`) renders all of them from one `LoginState` enum. The 04 and 05 pages reuse it.

## 03.5 Copy and strings
- All keys in `UiStrings` `Login.*` (update existing, add new), **5 languages** (nl/en final; pl/ro/ar B1 drafts, listed for 07's review file).
- Replace:
  - "Registreer via KVK" → "Bedrijf registreren (KvK)"
  - "Kies Microsoft, Google of je Lobsy-account." → "Kies hoe je wilt inloggen."
  - "of met e-mail" → "of met je e-mailadres"
  - "Inloggen met Microsoft/Google" stays
- Remove keys that are no longer used (`Login.Close`, `Login.EntraNotConfigured`, `Login.GoogleNotConfigured`, …) and update `docs/i18n/candidate-unused-keys.md` if the parity report lists them.
- ar (au-d10): the layout mirrors via logical properties; e-mail/password inputs keep `dir="ltr"`; the provider icons stay before the text in reading order.

## 03.6 Tests
- `LoginPageRenderTests` (WebApplicationFactory, static HTML):
  - no `role="dialog"`
  - one `h1`
  - `autocomplete="username"` / `current-password`
  - `rememberDevice` unchecked
  - no disabled provider buttons
  - only configured providers render (toggle the stub `IExternalAuthCredentialSource`)
  - no Google with `returnUrl=/admin/...`
  - "Bedrijf registreren" hidden with employers OFF (D present) or present (D absent)
  - no "Wachtwoord vergeten?" while `PasswordResetAvailable` is false
- `LoginStatesRenderTests`: each `error=` value renders its title, role and tint class. `locked` renders the pause card and no form.
- `LoginHintCookieTests`: after an `invalid` post the next GET prefills the e-mail, the cookie is gone after one render, and the URL has no e-mail.
- `LoginPlaywrightTests` (new class, both CI filter lists): desktop 1440 + mobile 390 screenshots (nl + ar), the keyboard path (Tab order: providers → e-mail → password → toggle → remember → submit), no horizontal scroll at 390, and no CSP violation.
- `AuthPagesCspGuardTests` (01) now also covers `Login.razor`; `LoginAntiforgeryTests` and `LoginIdentityTests` stay green.

## Success criteria
- `/login` renders without a Blazor circuit, matches au-d01/m01/d02/d03/d10 in structure and tokens, and shows no disabled or unavailable options.
- All login copy is in 5 languages; the ar page is RTL with LTR input fields.
- PR: screenshots (nl desktop/mobile, ar desktop), the dependency cases A–D, follow-ups written to `docs/auth-followups.md` for every absent case.

Done → next: `04-2fa-redesign.md`.
