# 09. Render matrix, Playwright render checks, E2E flows, docs, stack-end report

Read `00-README.md` first (§0, §M, §IA, Decisions, Dependencies). Branch `cursor/emails-9` from `cursor/emails-8`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/emails-9`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report.
> - This file adds tests, docs and the report. Product code changes only to fix what these tests find (each fix named in the PR).
> - Every new Playwright class goes into **both** filter lists in `.github/workflows/pr-tests.yml`.

| | |
|---|---|
| Branch | `cursor/emails-9` |
| PR title | `test(email): render matrix for every mail × 5 languages, Playwright render checks (600/375, light/dark, nl/ar), E2E link flows, docs + stack report` |
| PR body starts with | `Stacked on #<PR 08> (cursor/emails-8)` + the stack-end report (09.5) |
| Mockups | all em-* (the Playwright screenshots are compared by eye in the PR, not pixel-diffed) |
| Split seam | **09a** = render matrix + Playwright (09.1–09.2); **09b** = E2E flows + docs + report (09.3–09.5) |

## 09.1 Render matrix (`EmailRenderMatrixTests`, unit, AngleSharp)
Theory over **every registry key × nl/en/pl/ro/ar** with `EmailSampleContext.ForPreview` (08) and the fixed clock. For each, parse the HTML and assert:
- `<html lang="{code}" dir="ltr|rtl">` (rtl only for ar); `<meta charset>`, viewport, `color-scheme`, `supported-color-schemes`, `format-detection` meta present.
- Exactly one `<h1>`.
- Exactly one `[data-lobsy-cta]` for kind E/O, zero for kind S; the CTA has a VML twin inside `<!--[if mso]>`.
- The preheader element exists, is hidden, isn't equal to the subject, and is followed by the filler (`&#847;&zwnj;&nbsp;` run or whatever 02 chose).
- The logo `<img>` in every mail with `alt="Lobsy"`, width/height set; the mascot only on the §0 good-news keys, `alt=""`.
- Every `href` is absolute on the configured `PublicWebBaseUrl` or a `mailto:` to the support address; none contain `utm_`, a click-tracking host, or a redirect parameter. No `<img>` of 1×1 or with a tracking-looking query; no remote CSS; no `<script>`, `<form>`, `<iframe>`.
- No unreplaced `{`/`}` placeholder in HTML, text or subject; no `&amp;amp;`.
- The footer has the reason sentence for the key, "Hulp" (mailto) and "Privacy", plus "Mail-instellingen" and "Afmelden" only for kind O; the legal line shows `LegalName`, and address/KvK only when configured (two cases).
- Text part: non-empty, contains the CTA URL (E/O) or the code (S), the reason, and no HTML tags.
- Subject ≤ 60 chars for nl/en with sample data (warn only for pl/ro/ar, 04.7).
- Headers from the mailer's dry-run (03): Reply-To support; List-Unsubscribe + List-Unsubscribe-Post only for kind O; no tracking headers.
- Output: a table in the test log (key × language → pass) so the PR can paste it.

## 09.2 Playwright render checks (`EmailRenderPlaywrightTests`)
- Load each rendered HTML with `page.SetContentAsync` (no running stack needed) for: every key in **nl** and **ar**, at viewport **600** and **375**, **light** and **dark** (`ColorScheme.Dark` emulation + the `PreviewDark` mode for the forced case).
- Assert per render:
  - no horizontal overflow (`document.documentElement.scrollWidth <= innerWidth`)
  - footer text computed font-size ≥ 13 px
  - the CTA's box height ≥ 44 px (and full width at 375)
  - body text ≥ 16 px; h1 24–26 px
  - in dark: the card background isn't `#ffffff`/`#fffcfa` and text contrast ≥ 4.5:1 for body and CTA (compute from computed colours)
  - ar: the text direction is rtl, and the logo row and facts table are mirrored (the first cell's x position > the second's)
- Screenshots to `artifacts/playwright-email/{key}-{lang}-{width}-{theme}.png`. Add `artifacts/playwright-email/**` to the "Upload Playwright screenshots" paths.
- Add `EmailRenderPlaywrightTests` (and 08's `AdminEmailPreviewSmokeTests` if not already) to **both** filter lists in `.github/workflows/pr-tests.yml`.

## 09.3 E2E flows (integration tests on the API with a capturing mail sink)
Use the existing `WebApplicationFactory` test setup (`JobsyTestAuth`, like `SupportAccessGrantApiTests`) with a capturing `IEmailService` (pattern of `Sprint7RegistrationTests`). Parse the link from the captured mail (HTML and text must carry the same URL).
- **Invite → set password → login:** a manager invites a new user → one mail with a set-password link and no password; GET preview shows the masked e-mail; POST sets the password; login works; the second POST with the same token fails; a second invite invalidates the first link (D7).
- **Parental consent:** request consent → mail names the child's first name, link on `PublicWebBaseUrl/toestemming`; `GET api/parental-consent/preview` and the legacy `GET …/confirm` do **not** set `ParentalConsentAt`; the POST does; a second POST is a no-op success.
- **API key reveal:** request credentials → mail has the reveal link and no key; before reveal, the old key still works; reveal returns the key once and deactivates older keys; the second reveal fails; `Cache-Control: no-store` on the response.
- **One-click unsubscribe:** trigger PushBom → the mail has List-Unsubscribe + List-Unsubscribe-Post; `POST /mail/afmelden?t=` with `List-Unsubscribe=One-Click` returns 200 without auth or antiforgery; the next PushBom for that address is suppressed (logged "suppressed: opt-out") and a kind E mail (ApplicationConfirmation) still goes.
- **Language:** an anonymous application with `X-Jobsy-Language: ar` → the OTP mail is ar/rtl; a user with `ro` gets ApplicationConfirmation in ro.
- **Support access:** a grant request → the mail to the other admins is escaped (a reason with `<b>` shows as text) and shows the Amsterdam expiry.
- Optional (only if the smoke stack exposes a mail sink already): one Playwright flow through `/account/wachtwoord-instellen`. Otherwise skip and write it into followups.

## 09.4 Docs
- `docs/emails.md` ("How to add or change a mail"): registry entry, kind/reason/mascot, `EmailDocument` blocks, strings in 5 languages + review list, `EmailLinks`, snapshots (`JOBSY_UPDATE_EMAIL_SNAPSHOTS=1`), preview page, the guards and what they catch, the deliverability doc (03).
- `docs/emails-followups.md`: every "absent" dependency line from 02–08, the password-reset flow (D9), native review pl/ar, the legal footer values, and anything skipped in 09.3.
- `CHANGELOG.md`: one entry for the stack (what users will notice: new look, one button, their language, unsubscribe for optional mails, safer links).
- `docs/ROUTES.md` is already updated by 01/03/08 (check `RoutesDocFreshnessTests`).

## 09.5 Stack-end report (in the PR body and as the last message)
- PR list 01–09 with state (open/draft), and which PRs are this stack's own commits.
- Dependency outcomes A–I (present/absent per file where re-checked).
- Test summary: counts for the render matrix, Playwright renders, E2E flows; any skipped with the reason.
- Screenshot index (the artifact name).
- **What Dennis still has to provide or do:**
  1. Registered legal name, address and KvK number → `Mail:LegalName`, `Mail:LegalAddress`, `Mail:KvkNumber` (until then the footer shows only "Lobsy").
  2. Resend: add and verify the domain `mail.lobsy.nl` (the DKIM/SPF/MX records Resend shows), DMARC for lobsy.nl, set `FromAddress` to `Lobsy <hallo@mail.lobsy.nl>` in Integraties, send a test from the preview page (`docs/email-deliverability.md`).
  3. Who reads `support@lobsy.nl` (it's Reply-To and the Hulp link for every mail): make sure the mailbox exists and someone reads it.
  4. Native review of the pl and ar strings (`docs/i18n/emails-review.md`); ro spot check.
  5. Optional: `Mail:TestRecipientAllowList` for colleagues who should get test mails.
- Out of scope / deferred (from followups).

## Success criteria
- Render matrix green for every key × 5 languages; Playwright render checks green for nl + ar at 600/375 light/dark, screenshots uploaded.
- The 4 link flows + language + support-access E2E tests green.
- The report lists everything Dennis must provide.

Done → end of stack. Report (09.5) and stop.
