# 03. Sending: plain-text part, From/Reply-To via config, List-Unsubscribe One-Click for optional mails, mail settings

Read `00-README.md` first (§0, §M kinds, §IA, D1, D5, D6, D12). Branch `cursor/emails-3` from `cursor/emails-2`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/emails-3`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - No open/click tracking, no link rewriting, no tracking pixels. Unsubscribe applies **only** to the 3 optional mails (§M kind O); essential and security mails never get `List-Unsubscribe`.
> - Don't change how Resend/SMTP credentials are stored (`IIntegrationCredentialService`).

| | |
|---|---|
| Branch | `cursor/emails-3` |
| PR title | `feat(email): plain-text part, From/Reply-To via config, RFC 8058 one-click unsubscribe for optional mails, /account/mail-instellingen` |
| PR body starts with | `Stacked on #<PR 02> (cursor/emails-2)` |
| Mockups | `em-d01` footer (Mail-instellingen appears only on optional mails, e.g. `em-d03`'s position) |
| Migration | `AddEmailOptOuts` |
| Split seam | **03a** = transport (text, headers, From/Reply-To, idempotency, tags) 03.2–03.3; **03b** = opt-outs + endpoints + pages 03.4–03.6 |

## Goal
Mails are multipart (HTML + text) and come from `Lobsy <hallo@mail.lobsy.nl>` with replies going to `support@lobsy.nl`. Optional notifications can be switched off with one click, from Gmail/Yahoo's unsubscribe button too.

## 03.1 Today (verify first)
- `SmtpEmailService`:
  - The Resend request (`CreateResendRequest` L375) sends `from`, `to`, `subject`, `html` only.
  - SMTP (L161–170) sets `From` from `settings.FromAddress` and only `HtmlBody`.
  - `TryResolveResend`/`TryResolveSmtp` read `FromAddress` from the integration secrets. `MailOptions.FromAddress` (env `Mail__FromAddress`) fills empty DB secrets (`MailTestSendTests.Mail_env_resend_credentials_fill_empty_db_secrets`).
- `EmailMessage(To, Subject, BodyHtml, Category)`; after 02 the mailer builds it.
- No user mail preferences exist (`git grep -n "OptOut\|EmailPreference" -- 'Jobsy.Core/Entities'` is empty). PushBom already has a tokenized "set unavailable" action (`/candidate/actions/set-unavailable`), which is a status, not a mail preference; it stays.

## 03.2 Message + transport
- `EmailMessage` gets:
  - `string? BodyText`
  - `string? ReplyTo`
  - `IReadOnlyDictionary<string,string>? Headers`
  - `IReadOnlyList<(string Name, string Value)>? Tags`
  - `string? IdempotencyKey`
  - The existing positional constructor keeps working.
- The mailer fills `BodyText` (renderer text part), `ReplyTo = MailOptions.ReplyTo`, the tags `category={Category}`, `lang={Language}`, `kind={Kind}` (Resend tag rules: ASCII letters, digits, `_`, `-`), and the headers from 03.3.
  - `IdempotencyKey`: the caller may pass one (e.g. `apply-confirm:{applicationId}`); hosted jobs **must** pass `{key}:{entityId}:{yyyyMMdd}` so a job retry can't double-send.
- **Resend:** add `text`, `reply_to`, `headers`, `tags` to `ResendSendRequest`, plus the `Idempotency-Key` request header when set. Never send `tracking`/click options; add a comment that open/click tracking stays **off** at the domain level too (03.7).
- **SMTP:** `BodyBuilder { HtmlBody, TextBody }` → multipart/alternative; `ReplyTo`; custom headers via `mime.Headers.Add`.
- **From resolution:**
  1. the DB integration `FromAddress` if set
  2. else `MailOptions.FromAddress`
  3. else `Lobsy <hallo@mail.lobsy.nl>`
  - Parse it with `MailboxAddress.Parse`. If the display name is missing, add "Lobsy".
  - The admin Integraties/Mail card shows the effective From and Reply-To. In Production it shows a warning when the From domain isn't `mail.lobsy.nl` ("Afzender wijkt af van het afgesproken adres").
- `MailOptions` doc comment: replace the `noreply@lobsy.nl` example with `Lobsy <hallo@mail.lobsy.nl>`.

## 03.3 Headers
- Every mail: `X-Entity-Ref-ID: {guid}` (prevents Gmail threading of unrelated notifications) and `Auto-Submitted: auto-generated`. Nothing else custom.
- **Optional mails only** (§M kind O: PushBom, VacancyEngagementReminder, CompanyReEngagement):
  - `List-Unsubscribe: <{PublicWebBaseUrl}/mail/afmelden?t={token}>`
  - `List-Unsubscribe-Post: List-Unsubscribe=One-Click`
  - no `mailto:` variant (it would need an inbox that processes it)
  - The same URL is behind the footer link "Afmelden voor deze mails" (a Note-style line the renderer adds for kind O above the footer links) and "Mail-instellingen".
- **Token:** `IDataProtector` purpose `Lobsy.Mail.Unsubscribe.v1`, payload `{ emailHash, category, issuedUtc }`, base64url. It is valid for 1 year (unsubscribe links must keep working for old mails) and carries no PII in plaintext.
  - `emailHash` = SHA-256 of the normalized address + the `VerificationCodes` pepper (reuse its hashing helper if it takes arbitrary input; else add `EmailAddressHasher` next to it).

## 03.4 Opt-out storage
- Entity `EmailOptOut`, table `EmailOptOuts`, migration `AddEmailOptOuts`: `Id`, `EmailHash` (64), `Category` (string 64 = the template key), `CreatedAtUtc`, `Source` (`OneClick`, `Page`, `Settings`), unique (`EmailHash`, `Category`).
- `IEmailPreferenceService`: `IsOptedOutAsync(email, key)`, `OptOutAsync(email, key, source)`, `OptInAsync(email, key)`, `GetForUserAsync(userId)`.
- The mailer checks `IsOptedOutAsync` for kind O before sending. Suppressed → `email.suppressed` log with reason `opted-out` (redacted).
- Privacy: account deletion (`PrivacyDataService`) keeps the opt-out rows (they hold only a hash and stop future mail). Document this in the privacy page's retention list if that list names tables. The data export includes "Afgemelde mails: {namen}".

## 03.5 Endpoints
- `POST /mail/afmelden` (**Web** minimal API endpoint, anonymous, antiforgery disabled for this endpoint only, rate limit 30/min per IP). It accepts the RFC 8058 body `List-Unsubscribe=One-Click` or a form post from the page.
  - It validates the token, calls the API `POST api/email-preferences/unsubscribe` `{ token }` (API validates again and writes the opt-out), and returns `200` with a plain HTML page (or a 302 to the page for browser form posts).
  - Invalid token → 400, generic message. It never reveals whether an address exists.
- `GET /mail/afmelden?t=` (Web page `Pages/Public/MailUnsubscribe.razor`, static SSR, noindex): "Wil je geen {categorienaam} meer ontvangen?" with a button "Afmelden" (POST). It **never** mutates on GET.
  - After POST: "Je krijgt deze mails niet meer." + "Toch weer aanzetten" (POST, same token → opt-in) + a link to `/account/mail-instellingen`.
- `GET/PUT api/me/email-preferences` (authenticated): the optional categories the user's roles can receive (candidate: PushBom; employer roles: VacancyEngagementReminder, CompanyReEngagement), each `{ key, label, enabled }`.
- **`/account/mail-instellingen`** (authenticated, static SSR form, noindex):
  - Heading "Mail-instellingen".
  - Toggles for the optional mails, with a short line for each: "Tips over vacatures bij jou in de buurt", "Herinnering als je vacature 14 dagen openstaat", "Bericht als je een tijd niet actief was". Labels via `UiStrings` `MailSettings.*`, 5 languages.
  - A read-only list "Deze mails krijg je altijd", with its reason: codes, sollicitaties, uitnodigingen, beveiliging.
  - Link it from the account/profile menu where "Privacy & gegevens" sits today.

## 03.6 Strings
The new UI strings (unsubscribe page, settings page) go through `UiStrings` in 5 languages (pl/ro/ar drafts listed in `docs/i18n/emails-review.md`, created here). The mail's "Afmelden voor deze mails" line goes through `EmailStrings` from 04 (nl literal in the renderer until then, with a `// 04` marker the parity test in 04 removes).

## 03.7 Deliverability doc for Dennis
Add `docs/email-deliverability.md`. It is short and is a checklist for Dennis, not for code:
1. **Resend domain `mail.lobsy.nl`:** in Resend, add the domain, then create exactly the records Resend shows at the DNS host of lobsy.nl: the DKIM TXT record, plus the MX and SPF TXT records on its bounce subdomain (typically `send.mail.lobsy.nl`). Wait for "Verified".
2. **DMARC** on `lobsy.nl` (applies to the subdomain): start `v=DMARC1; p=none; rua=mailto:dmarc@lobsy.nl; adkim=r; aspf=r`, then move to `p=quarantine` after 2–4 clean weeks.
3. **Resend settings:** click and open tracking **off** for the domain (D6).
4. **`support@lobsy.nl`** must be a real, read mailbox (Reply-To for every mail). Decide who reads it (Dennis, as the owner).
5. `hallo@mail.lobsy.nl` needs no inbox (replies go to Reply-To). Optionally forward it to support.
6. Set `Mail__FromAddress`, `Mail__ReplyTo`, `Mail__SupportAddress`, `Mail__LegalName`, `Mail__LegalAddress`, `Mail__KvkNumber` in Render for acceptatie and production (the address and KvK still have to be provided).
7. Google Postmaster Tools for `mail.lobsy.nl` (optional).
- Link it from `docs/deploy-render.md`.

## Tests
- `ResendRequestTests`: `text`, `reply_to`, `tags`, headers and `Idempotency-Key` are present; kind O has both List-Unsubscribe headers; kind E/S have neither; no tracking fields.
- `SmtpMimeTests`: multipart/alternative with text and html parts; the Reply-To header; List-Unsubscribe only for kind O; no `cid:` part.
- `FromResolutionTests`: DB wins, then config, then the default; a missing display name becomes "Lobsy"; the Production warning.
- `UnsubscribeFlowTests`:
  - a POST with the RFC 8058 body opts out
  - GET doesn't
  - a tampered or expired token → 400
  - an opt-out suppresses the next PushBom for that address; opt-in restores it
  - essential mails are still sent to an opted-out address
- `EmailPreferencesApiTests`: role-based categories; PUT round-trip; anonymous → 401.
- bUnit/SSR tests for both pages (form method POST, noindex, no token in links other than the form).
- `MailTestSendTests` stay green (update for the new request shape).
- `RoutesDocFreshnessTests`, `PageSeoTests`, `BlazorPageRoleAttributesTests`, `EfModelSnapshotTests`, `PendingModelChangesTests` stay green.

## Success criteria
- A captured Resend payload for PushBom contains `text`, `reply_to: support@lobsy.nl`, `List-Unsubscribe` + `List-Unsubscribe-Post`, and no tracking options. The ApplicationConfirmation payload has no List-Unsubscribe.
- The one-click POST works without cookies or antiforgery; the page GET never writes.
- `docs/email-deliverability.md` exists and the PR body repeats its "Dennis to do" list.

Done → next: `04-talen-rtl.md`.
