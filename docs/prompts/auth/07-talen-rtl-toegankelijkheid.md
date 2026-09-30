# 07. Languages, RTL, B1 copy and accessibility for every auth page

Read `00-README.md` first (§0 Copy/Strings, §IA, Decisions, and the "Needs Dennis" note on pl/ar review). Branch `cursor/auth-7` from the last branch that exists (`cursor/auth-6`, else `cursor/auth-5`, else `cursor/auth-4`). **Stacked.**

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/auth-7`; no force-push.
> - ONE PR into `acceptatie`; the body starts with `Stacked on #<previous PR> (<previous branch>)`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start file 08.
> - Never lock, reset or change a real account on acceptatie or production while testing. Never log passwords, codes, tokens or TOTP secrets.
> - Microsoft and Google logins never get an extra Lobsy 2FA step (admins can't use Google, 02).

| | |
|---|---|
| Branch | `cursor/auth-7` |
| PR title | `feat(auth): all auth copy in nl/en/pl/ro/ar at B1, RTL with LTR fields, accessibility sweep and guards` |
| Mockups | `au-d10`/`au-m10` (ar RTL); all others for the accessibility checks |
| Migration | none |
| Split seam | if > ~1.500 lines: **07a** = strings + review file + guards; **07b** = RTL + accessibility fixes on `cursor/auth-7b` |

## Goal
Every auth screen and auth mail reads naturally in all 5 languages at B1 level. Arabic is fully mirrored while e-mail, codes and keys stay left-to-right. Keyboard and screen-reader users get through every flow.

## 07.1 Today (verify first)
- **`UiStringsMfa.cs`:** its `Add` helper copies the **English** text into pl/ro/ar (L11–18). So `LocalizationParityReportTests` (which checks key parity and "identical to nl" debt against `docs/i18n/untranslated-baseline.txt`: en=230, pl=845, ro=862, ar=837) doesn't notice untranslated 2FA copy.
- **Other keys:** `Login.*` in `UiStrings.cs`/`UiStringsExtras.cs`; `Auth.*` and `PasswordReset.*` in `UiStringsAuth.cs` (added by 01–05); mail copy in `TransactionalEmails` or `EmailStrings` (Dependencies F).
- **RTL:**
  - `MainLayout.razor` L16 sets `dir="rtl"` on its wrapper via `Culture.IsRightToLeft`
  - the auth layout (`PublicLayout`, or 03's `AuthPublicLayout`) must do the same on its root and on `<html>` if the layout owns it; check what 03 did
- **Guards that exist:** `LocalizationParityReportTests`, `AccessibilityGuardTests`, `AuthPagesCspGuardTests` (01).

## 07.2 Strings: parity and real translations
- **Scope:** every key used by `Login.razor`, `LoginStatusBlock`, `MfaPrompt/Setup/RecoveryCodes`, the regeneration page, `ForgotPassword.razor`, the reset variant of `SetPassword.razor`, the auth layout, and the auth mails:
  - `AccountLockout`, `MfaLockout`, `RecoveryCodeUsed`, `RecoveryCodesRegenerated`, `PasswordReset`, `PasswordResetExternalOnly`, `PasswordChanged`
  - Collect the list with a script (`tools/` or a test helper) that scans those files for `Culture["…"]` and the mail keys; paste the count in the PR.
- **`UiStringsMfa.cs`:** replace the `Add(key, nl, en)` helper with `Add(key, nl, en, pl, ro, ar)` and give every key real pl/ro/ar text (B1, "je/jij"-level informal where the language has it, in the Lobsy tone of the rest of the file set).
- **nl + en are final.** pl/ro/ar are careful B1 drafts; list them all in **`docs/i18n/auth-review.md`**: key, nl, the draft, and a note column. pl and ar get the header "Needs a native check before production (Dennis)".
- **New guard** `AuthLocalizationGuardTests`:
  - for the collected auth keys, the pl/ro/ar values are not equal to the en value (allow-list: brand names, "Microsoft", "Google", "Lobsy", format-only strings like `{0}/{1}`)
  - placeholders (`{0}`, `{1}`…) match across the 5 languages
  - no value is empty
- **Baseline:** lower `untranslated-baseline.txt` to the new counts (they must drop or stay equal; the PR states the before and after).
- **Unused keys:** remove auth keys that no page or mail uses any more (the old modal keys, `Mfa.EnterCode` if 04 didn't) and update `docs/i18n/candidate-unused-keys.md`.

## 07.3 B1 copy sweep (nl first, then the others)
- Short sentences (≤ 15 words where possible), active voice, "je". One idea per sentence.
- Say what happened, then what to do: "Dat klopt niet helemaal. Probeer het nog eens."
- No jargon:
  - "authenticator-app" stays (the store name) with one explanation on setup
  - "2FA" only in admin UI; on user pages use "extra beveiliging" and "code uit je app"
  - "herstelcode" stays
- Times as `HH:mm` in Europe/Amsterdam with the word "om": "Probeer het weer om 14:35".
- Dates in mails: `d MMMM yyyy` in the mail's language.
- The same words for the same thing everywhere: "Inloggen", "Nieuw wachtwoord kiezen", "Code uit je app", "Herstelcode", "Vertrouw dit apparaat", "Even pauze". Add a small glossary table at the top of `docs/i18n/auth-review.md`.
- Keep the copy that 01–05 defined; change it only for B1 or consistency, and list each change in the PR (old → new).

## 07.4 RTL (au-d10 / au-m10)
- The auth layout root gets `dir="rtl"` and `lang="ar"` for ar (`lang` for every language).
- `auth.css` uses logical properties only (`margin-inline-start`, `inset-inline-end`, `text-align: start`). Add a grep guard in `AuthCssGuardTests`: no `left`/`right` in `auth.css` except inside `[dir="ltr"]` rules or in comments.
- **Mirror:**
  - the card layout, the mascot corner (it peeks over the top-**left** edge in RTL)
  - step numbers, the chevron of `<details>`
  - the "Toon" toggle position, and the icon side of status blocks
- **Keep LTR** (`dir="ltr"` on the element, with `text-align: start` still working):
  - e-mail, password, TOTP code, recovery code, the manual key and the recovery codes list
  - the masked e-mail
  - URLs, and times like `14:35`
- Provider buttons: the icon stays before the text in reading order (so on the right in RTL).
- ar screenshots (desktop + mobile) of `/login`, the pause card, the prompt, setup, recovery codes, forgot password and the new password page go into the PR.

## 07.5 Accessibility sweep (all auth pages)
- **Landmarks:** one `<main>`, one `h1`, headings in order, header/footer landmarks from the layout.
- **Labels:** every field has a visible `<label for>`; hints linked via `aria-describedby`; required fields `required` (and no `*`-only signals).
- **Errors:**
  - the `LoginStatusBlock` of kind error has `role="alert"`; info has `role="status"`
  - invalid fields get `aria-invalid="true"` + `aria-describedby` pointing at the message
  - focus goes to the first invalid field after a failed POST (server-rendered `autofocus`)
- **Live regions:** the copy toast and the rules checklist on the new password page are `aria-live="polite"`.
- **Buttons vs links:** actions are `<button>`, navigation is `<a>`. The "Toon" toggle has `aria-pressed` and an accessible name ("Wachtwoord tonen" / "Wachtwoord verbergen").
- **Focus:**
  - a visible focus ring on every interactive element (token-based)
  - no focus traps (the old modal is gone)
  - the skip link from the layout works
- **Size and zoom:** tap targets ≥ 44 × 44 px; 200 % zoom and 320 px width without horizontal scroll; text resize doesn't clip the codes grid.
- **Contrast:** AA for text, focus rings and the tint blocks (check each tint with the token pair it uses; list the ratios in the PR).
- **Motion:** blobs and toasts respect `prefers-reduced-motion`.
- **Images:** the mascot and blobs are decorative (`alt=""`/`aria-hidden`); the QR has a meaningful `alt` and the key is always available as text.
- **Autocomplete:** `username`, `current-password`, `new-password`, `one-time-code`, and recovery `off` (01).

## 07.6 Tests
- `AuthLocalizationGuardTests` (07.2); `LocalizationParityReportTests` green with the lower baseline.
- `AuthCssGuardTests` (logical properties; no hex/rgb literals; no inline `style=` in the auth `.razor` files).
- `AuthAccessibilityGuardTests` (static HTML via WebApplicationFactory, for each auth page in nl and ar):
  - one `h1`, `main`, and a label for every input
  - `aria-invalid` + `aria-describedby` in the error states
  - `dir="ltr"` on the listed fields
  - `dir="rtl"` on the root for ar
- `AuthA11yPlaywrightTests` (both CI filter lists): axe-core if the repo already has it (`rg -n "axe" Jobsy.Tests package.json`), otherwise the keyboard path and focus-ring screenshot checks per page; 390 px and 200 % zoom without horizontal scroll; ar screenshots.
- All earlier auth tests stay green.

## Success criteria
- Every auth key has real text in 5 languages, and `docs/i18n/auth-review.md` lists the pl/ro/ar drafts with the native-check note for Dennis.
- The ar pages mirror correctly, with LTR fields.
- The a11y guards and Playwright checks pass. The PR contains the before/after baseline, the copy changes (old → new), the contrast ratios and the screenshots.

Done → next: `08-e2e-rapport.md`.
