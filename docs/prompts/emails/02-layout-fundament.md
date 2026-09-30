# 02. Foundation: one mail model, one renderer (HTML + text), the new layout, registry, central mailer, guards

Read `00-README.md` first (§0, §M, §B, Dependencies). Branch `cursor/emails-2` from `cursor/emails-hotfix` (or `origin/acceptatie` when PR 01 is merged).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/emails-2`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - No tracking pixels, no link rewriting, no secrets in mails. Every value is HTML-escaped by the renderer.
> - This file changes **structure and look**, not copy. Today's Dutch texts are ported as they are (except where 02.9 says so); the B1 rewrite is 05–07.

| | |
|---|---|
| Branch | `cursor/emails-2` |
| PR title | `feat(email): EmailDocument + one renderer (HTML and plain text), Outlook-proof layout with dark mode and preheader, template registry, central mailer, bare-HTML guard` |
| PR body starts with | `Stacked on #<PR 01> (cursor/emails-hotfix)` (or `Stacked on: none (01 merged)`) + the Dependencies outcome |
| Mockups | `em-d01`/`em-m01` (layout and notes), `em-d02…07` (blocks), `em-d08` (dark), `em-d09` (RTL, renderer support only) |
| Split seam | **02a** = model + renderer + layout + assets + config + tests (02.2–02.6, 02.10); **02b** = `EmailLinks`, registry, mailer, port of all templates, guards (02.7–02.9) |

## Goal
Every mail is built from one small data model and rendered by one component into email-safe HTML **and** a plain-text version. The look matches em-d01, and no code can send a mail that skips the layout.

## 02.1 Today (verify first)
- `Jobsy.Core/Email/EmailLayout.cs` (299 lines): hex constants, `Wrap()` L110–185, `PrimaryButton`/`SecondaryButton` L194–226, `FactCard`, `KpiList`, `OtpBlock` (`data-lobsy-otp`), `MutedNote`, URL helpers (`CandidateApplicationsUrl`, `VacancyUrl`, `EmployerApiKeysUrl`, `LoginUrl`, `WithdrawOthersUrl`, `SetUnavailableUrl`, `PrivacyDataUrl`, …), `Absolute()` defaulting to `https://lobsy.nl`, `FormatEuro`/`FormatKm` (nl-NL).
- `Jobsy.Core/Email/TransactionalEmails.cs` (849 lines):
  - `Templates` (28 `EmailTemplateInfo`)
  - `Compose(key, EmailSampleContext)` for previews
  - one static method per mail returning `ComposedEmail(Key, Category, Subject, Html)`
  - after 01, also `ParentalConsent` and `SupportAccessRequested`
- `EmailLogoEmbedder` (CID for SMTP), `SmtpEmailService` (Resend + SMTP), `IEmailService`/`EmailMessage(To, Subject, BodyHtml, Category)`.
- Senders (`git grep -n "new EmailMessage(" -- '*.cs'`): `ApplicationsController`, `CandidateActionsController`, `CompanyUsersController`, `AuthController` (AccountLockout, bare HTML), `MeController` (consent, after 01 via template), `CompanyRegistrationService`, `SalesManagerInviteService`, `AmbassadeurInviteService`, `CompanyApiKeyService`, `PrivacyDataService`, `VacancyProductService`, `SupportAccessService`, `IntegrationHealthStub`, `EmailCatalogService`, and the jobs `CompanyReengagementHostedService`, `DraftVacancyCleanupHostedService`, `VacancyEngagementReminderHostedService`.
- Tests that pin markup: `TransactionalEmailCatalogTests` L62–65 (`display:inline-block;padding:12px 22px`), L43 (`https://lobsy.nl/images/brand/lobsy-email.png`), L46 (`BrandNavy`); `EmailLayoutTests`; `EmailLogoEmbedderTests`.

## 02.2 Model (`Jobsy.Core/Email/Model/`)
- `EmailDocument` (immutable record):
  - `TemplateKey`, `Kind` (`EmailKind { Essential, Optional, Security }`), `Culture` (`EmailCulture`: `Language` code + `IsRightToLeft`; nl-only until 04)
  - `Subject`, `Preheader`, `Eyebrow?` (`EmailEyebrow(Text, EmailTone { Peach, Sun, Sky, Mint })`)
  - `Heading`, `Greeting?`, `Blocks` (`IReadOnlyList<EmailBlock>`), `Cta?` (`EmailCta(Label, AbsoluteUrl)`)
  - `ShowMascot` (bool), `ReasonText`, `SignOff`
- `EmailBlock` (sealed hierarchy):
  - `Paragraph(EmailText)`
  - `Facts(IReadOnlyList<(string Label, EmailText Value)>, EmailTone Tone = Sky)`
  - `Steps(string Title, IReadOnlyList<EmailText>)`
  - `Code(string Digits, string ValidityText)`
  - `Note(EmailText, EmailLink? Link = null)`
- `EmailText`: a list of segments (`Plain(string)`, `Bold(string)`, `Link(label, url)`). Built with a tiny formatter: `EmailText.Format("Je sollicitatie bij {0} is ontvangen.", EmailArg.Bold(companyName))`. Templates never write HTML. The resources (04) contain only `{n}` placeholders.
- `ComposedEmail` becomes `(Key, Category, Kind, Language, Subject, Preheader, Html, Text)`. Keep a constructor overload or a factory so the few existing call sites compile during the port.
- Sanity rules enforced by the renderer (throw in Development/Testing, log an error and still send in Production):
  - at most one `Cta`
  - `Kind == Security` ⇒ `Cta == null` and exactly one `Code` block
  - `Preheader != Subject`
  - `ShowMascot` only for the registry's good-news keys

## 02.3 Renderer (`Jobsy.Core/Email/EmailRenderer.cs`)
`EmailRenderResult Render(EmailDocument doc, EmailBrand brand, EmailRenderMode mode = Normal)` → `(Html, Text)`. `EmailRenderMode.PreviewDark` is used by the admin preview only (08). It forces the dark palette without relying on `prefers-color-scheme`.

**HTML (match `em_layout.py` / `html/em-*.mail.html`):**
- `<!DOCTYPE html>`, `<html lang="{lang}" dir="{ltr|rtl}" xmlns:v="urn:schemas-microsoft-com:vml" xmlns:o="urn:schemas-microsoft-com:office:office">`.
- Head: `charset utf-8`, `viewport`, `x-apple-disable-message-reformatting`, `format-detection` (`telephone=no,date=no,address=no,email=no,url=no`), `color-scheme` + `supported-color-schemes` = `light dark`, `<title>` = subject, the MSO `OfficeDocumentSettings` block (PixelsPerInch 96).
- One `<style>` block (the only non-inline CSS):
  - reset
  - `@media (max-width:620px)`: outer padding 18/0/24; card without side borders/radius; 20 px side padding; h1 24/30; button `display:block` full width; facts stack label over value; code 34 px
  - `@media (prefers-color-scheme:dark)`: the dark palette from §0, applied by class
  - `[data-ogsc]`/`[data-ogsb]` overrides
- Body: preheader (`display:none; max-height:0; overflow:hidden; mso-hide:all`) + a second hidden div with ~60× `&#8199;&#65279;&#847; ` filler.
- Layout:
  - an outer full-width table (`class="bg"`)
  - an MSO ghost table `width="600"`, then a `div` `max-width:600px`
  - the logo row: 36 px mark `alt="Lobsy"` + the word "Lobsy" as **live text**, 20 px bold, brand colour
  - the card table: `border-collapse:separate`, radius 18, 1 px border, padding 30/36/20
  - the footer table outside the card
- Blocks as tables (no flexbox/grid, no `position`, no background images).
  - Eyebrow: a pill `td` with the tone background, 13/18 600.
  - Facts: tone-background table, radius 14, rows with a 1 px border-top, label 14 muted / value 16 600.
  - Steps: 26 px number circle (sky) + text 16/22.
  - Code: sky box, monospace 40 px, letter-spacing 10 px, `data-lobsy-otp="{digits}"`.
  - Note: 1 px top border, 14/21 muted.
  - Mascot: a 64 px image cell at the inline-end of the heading row, `alt=""`.
- **Button** (the only CTA):
  - `<!--[if mso]>` `v:roundrect` (height 50, `arcsize="50%"`, `fillcolor` brand, `w:anchorlock`, centered 16 px bold Arial)
  - `<!--[if !mso]><!-->` an `<a class="btn-a" data-lobsy-cta href="…">` (inline-block, pill, padding 15/30, 16/20 700, `mso-hide:all`) `<!--<![endif]-->`
- Footer (13/20 muted, never smaller):
  - the reason line
  - the links line: `Hulp` (`mailto:{SupportAddress}`) · `Privacy` (`{base}/privacy`) · `Mail-instellingen` (`{base}/account/mail-instellingen`, **only** when `Kind == Optional`)
  - the legal line: `{LegalName} · {LegalAddress} · KvK {KvkNumber}`, empty parts omitted (README D14)
- Markers for tests (harmless in clients): `data-lobsy-layout="2"` on the outer table, `data-lobsy-cta` on the button link, `data-lobsy-block="facts|steps|code|note"`, `data-lobsy-otp`.
- No `<script>`, no forms, no external CSS/fonts, no 1×1 images, no query parameters added to links except those the template passes (no `utm_`).

**Text part:**
- The subject is not repeated.
- Order: greeting, heading (underlined with `=` when LTR; no ASCII art for RTL), paragraphs, facts as `Label: value`, numbered steps, the code on its own line with blank lines around it, and the CTA as `{Label}: {url}`.
- Then notes, the sign-off, and the footer (reason, `Hulp: mailto…`, `Privacy: url`, the preferences URL when optional, the legal line).
- Wrapped at 76 characters, never breaking inside URLs. Bold/links are flattened (`label (url)`).

## 02.4 Brand config (`Jobsy.Core/Options/MailOptions.cs`, section `Mail`)
- Add:
  - `ReplyTo` (default `support@lobsy.nl`)
  - `SupportAddress` (default `support@lobsy.nl`)
  - `LegalName` (default `Lobsy`)
  - `LegalAddress` (default empty)
  - `KvkNumber` (default empty)
  - `AssetVersion` (default `YYYYMMDD-mail2`)
- Change the documented default of `FromAddress` to `Lobsy <hallo@mail.lobsy.nl>` (03 wires it).
- `EmailBrand` (record built from options + `PublicWebBaseUrl`): absolute logo/mascot URLs, support address, legal line.
- **Health warning:** in Production, when `LegalAddress` or `KvkNumber` is empty, log a warning at startup and show one line on the admin Integraties/Mail card: "E-mailfooter mist adres en/of KvK-nummer (Mail:LegalAddress, Mail:KvkNumber)". **Dennis has to provide these** (README D14). The PR body says so.

## 02.5 Assets
- `Jobsy.Web/wwwroot/images/email/lobsy-mark-72.png` (72×72, from today's `lobsy-email.png`/`lobsy-128.png` resized with ImageSharp or committed resized) and `mascot-celebrating-128.png` (Dependencies A: landing 02's `Celebrating` pose as a PNG if present, else today's `mascot-128.png`).
- The URL is `{PublicWebBaseUrl}/images/email/<file>?v={AssetVersion}`; both files get `Cache-Control: public, max-age=31536000, immutable` like other versioned assets.
- Add the paths to `Jobsy.Tests/asset-versions.json` if `AssetVersionGuardTests` covers `wwwroot/images` (check).
- **Delete** `EmailLogoEmbedder`, its CID rewrite in `SmtpEmailService.SendViaSmtpAsync` (L167–169) and `Jobsy.Infrastructure/Assets/lobsy-email.png` as an embedded resource. Keep `wwwroot/images/brand/lobsy-email.png` (old mails in inboxes still point to it).
- Rewrite `EmailLogoEmbedderTests` → `EmailAssetTests`:
  - both PNGs exist, are valid, and are ≤ 20 KB
  - the renderer's `<img>` tags are absolute https with `width`/`height`/`alt`
  - the SMTP body contains no `cid:`
  - keep `Chatbots_keep_the_illustrated_mascot` if it tests something else

## 02.6 Tokens
`EmailTheme` (static, Core) holds the light and dark hex values from §0. The only other place with colours is the `<style>` block the renderer writes from `EmailTheme`. Delete the old `EmailLayout` colour constants once nothing uses them.

## 02.7 Links (`Jobsy.Core/Email/EmailLinks.cs`)
- Constructed from a **required** base URL (`EmailLinks.For(string publicWebBaseUrl)`); throws on null/empty.
  - This removes every `baseUrl: null`. The call sites listed in README §B get the real `PublicWebBaseUrl` from `IPlatformFeatureService`. The jobs read it the same way.
  - Hosted jobs that run outside a request use the same service. There is no fallback to a hard-coded `https://lobsy.nl` in Production; in Development/Testing the fallback is `http://localhost:5xxx` from config.
- Members:
  - `CandidateApplications`, `Vacancy(id)`, `Map` (Dependencies A/C), `WithdrawOthers(token)`, `SetUnavailable(token)`, `Login`, `PrivacyData`, `Privacy`, `MailSettings`
  - `SetPassword(token)`, `ApiKeyReveal(token)`, `ParentalConsent(token)` (01 routes)
  - `EmployerHome`, `EmployerApplications(applicationId?)`, `EmployerVacancyEdit(id)`, `EmployerTokens`, `EmployerTakeovers`, `EmployerApiSettings`, `EmployerTeam` (Dependencies B), `SalesOnboarding`, `AmbassadeurOnboarding` (Dependencies D), `AdminEmails` (Dependencies G), `AdminPersonalDataAccessLog`, `HowLobsyWorks`, `SupportMailto`
- Every URL is absolute on the base; `mailto:` only for support.
- Delete the URL helpers from `EmailLayout` and move their callers to `EmailLinks`. The existing `Deep_links_point_to_expected_routes` test moves to `EmailLinksTests`, with one test per dependency case (use a fake route source).

## 02.8 Registry (`Jobsy.Core/Email/EmailTemplateRegistry.cs`)
- One `EmailTemplateDefinition` per key in README §M:
  - `Key`, `Category` (today's category string, kept for PlatformLog continuity), `Audience`, `Kind`, `ReasonKey`, `GoodNews` (mascot allowed)
  - `RequiresEmployers` (Dependencies F)
  - `Parked` (AmbassadeurInvite when Dependencies D says so)
  - admin `Title` + `Description` (nl)
- 31 keys, including `ParentalConsent`, `SupportAccessRequested` and `AccountLockout`. Mails from other stacks present on acceptatie (Dependencies A/E/G) are registered too, with the kind the README names.
- `TransactionalEmails.Templates` returns the registry (as `EmailTemplateInfo` for the existing API contract). `Compose(key, sample)` covers every key (the new three included).

## 02.9 Central mailer + port
- `ITransactionalMailer` (Core interface, Infrastructure `TransactionalMailer`): `Task<EmailSendOutcome> SendAsync(ComposedEmail mail, string to, EmailSendOptions? options = null, CancellationToken ct = default)`.
  - It looks up the definition.
  - It suppresses when `RequiresEmployers` and employers are off (Dependencies F), or when `Parked` (log `email.suppressed` with key + reason, redacted recipient). 03 adds opt-outs.
  - It builds the `EmailMessage` and calls `IEmailService`.
  - It is the **only** place that constructs `EmailMessage` (the stub/provider tests excepted).
- **Port every template** to `EmailDocument` + renderer, keeping today's copy, with these mechanical rules:
  - `PrimaryButton` → `Cta`.
  - Each `SecondaryButton`/extra link → a `Note` with a `Link` (05–07 then decide).
  - `FactCard`/`KpiList` → `Facts`.
  - `OtpBlock` → `Code`; code mails lose their button now (§0 rule).
  - `MutedNote` → `Note`.
  - The preheader stays unless it equals the subject; then use the first paragraph's first sentence.
  - `AccountLockout` (from `AuthController` L101) becomes `TransactionalEmails.AccountLockout(links, …)` with today's text; `AuthController` calls the mailer.
- Every sender in 02.1 switches to `ITransactionalMailer`. `EmailCatalogService` too (08 reworks it).
- Remove `EmailLayout.Wrap`, the button/card helpers and `OtpBlock`. Keep `Escape` inside the renderer only. `FormatEuro`/`FormatKm` move to `EmailFormat` (nl for now, culture in 04).

## 02.10 Tests
- **Renderer unit tests (`EmailRendererTests`):**
  - Doctype, `lang`/`dir`, the five meta tags, one `<style>`.
  - The ghost table `<!--[if mso]>` with `width="600"`.
  - The preheader + filler.
  - Exactly one `data-lobsy-cta` with a VML twin (same href) when `Cta` is set.
  - Mascot only when allowed.
  - The footer reason; the Mail-instellingen link only for `Optional`; the legal line omits empty parts.
  - Escaping: a value `<script>` appears as `&lt;script&gt;` in the HTML and literally in the text.
  - An RTL culture gives `dir="rtl"` and `text-align:right` on the card.
  - The text part contains the CTA URL and the code, and no HTML tags.
  - The sanity rules throw in Testing.
- **`EmailLayoutGuardTests` (replaces `Production_senders_use_the_shared_catalog`):**
  - Source scan: `new EmailMessage(` appears only in `TransactionalMailer.cs` (plus the listed test fixtures). No `.cs` outside `EmailRenderer.cs` contains `<html`, `<p style`, `<table` or `EmailLayout.Wrap(`.
  - Runtime: the mailer throws in Testing when `Html` lacks `data-lobsy-layout="2"`. A test sends a bare-HTML `ComposedEmail` and expects the throw.
- **Structural snapshots (`EmailSnapshotTests`):**
  - For every registry key (sample context, nl), store `Jobsy.Tests/EmailSnapshots/{key}.nl.txt`. It holds the **text part** plus a structural outline built with AngleSharp: element path for `h1`, `data-lobsy-*` markers, visible text per block, hrefs, `img` src/alt/size.
  - **No `style` attributes or CSS** in the snapshot.
  - Regenerate with `JOBSY_UPDATE_EMAIL_SNAPSHOTS=1 dotnet test --filter FullyQualifiedName~EmailSnapshot` (same pattern as `RoutesDocFreshnessTests`). A missing snapshot fails.
- **`TransactionalEmailCatalogTests`:**
  - Replace the inline-CSS and hard-coded-URL assertions: every key renders; there is one CTA or (Security) one code; every href is absolute on the test base or `mailto:`; `lobsy-mark-72.png` is present; no `cid:`.
  - The count assertion becomes "contains at least the §M keys".
- `EmailLinksTests` (per dependency case), `EmailAssetTests` (02.5), `TransactionalMailerTests` (suppression: employers off, parked; the message has the category; the recipient is redacted in logs).
- Update `EmailLayoutTests` or delete it where the renderer tests cover it (say which in the PR).

## Success criteria
- `git grep -n "EmailLayout.Wrap\|PrimaryButton(\|SecondaryButton(\|EmailLogoEmbedder\|baseUrl: null" -- '*.cs'` is empty.
- `git grep -n "new EmailMessage(" -- 'Jobsy.Api' 'Jobsy.Infrastructure' 'Jobsy.Core'` shows only `TransactionalMailer`.
- The PNG renders of ApplicationConfirmation (nl) at 600 and 375 in the PR match em-d01's structure (logo row, card, eyebrow if set, one button, footer). Dark render with `prefers-color-scheme: dark` matches em-d08's palette.
- Outlook check: the HTML contains the MSO ghost table and VML for the CTA (test). If a Litmus/Email on Acid account is available, attach Outlook 2019/365 and Gmail app screenshots; if not, say "no client farm available".

Done → next: `03-verzending-headers-afmelden.md`.
