# 08. Admin preview per mail × language × theme, fake data only, limited test send and "send all"

Read `00-README.md` first (§IA, §B, Dependencies D/G). Branch `cursor/emails-8` from `cursor/emails-7`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/emails-8`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - The preview uses **fake data only**: no database reads for vacancies, users or companies, no real tokens, no real API keys.
> - Test mails only go to the admin's own address or the configured allow-list.

| | |
|---|---|
| Branch | `cursor/emails-8` |
| PR title | `feat(admin): e-mail preview (HTML + text, 5 languages, light/dark, 600/375) with fake data; limited and paced test send` |
| PR body starts with | `Stacked on #<PR 07> (cursor/emails-7)` + the outcome of the dep G re-check |
| Mockups | none for the admin page (use today's admin page style); the previews themselves are the em-* mails |
| Split seam | **08a** = API (08.1–08.3); **08b** = page (08.4) |

## 08.0 Today (verify first)
- `EmailCatalogController` (`api/settings/email-templates`, Admin, rate limit `public-write`): `GET` list, `POST {key}/send`, `POST send-all`.
- `EmailCatalogService.BuildContextAsync` (L146–170) reads a **live active vacancy** from the DB (L151) for the sample and looks up `PublicApiBaseUrl` (L157). `SendAllAsync` (L116–140) loops over every template and sends them back to back to any address.
- Page: `/admin/mail-test` (`MailTestAdmin.razor`, "send all" at ~L158). There is no preview; the admin has to send to see a mail.

## 08.1 Fake data (`EmailSampleContext.ForPreview`)
- One fixed sample set in Core, the README "Voorbeelddata": Alex, Sanne van Dijk, Joris Bakker, Bakkerij De Gouden Korrel, Markt 12 Delft, "Medewerker bakkerij", code `123456`, 86 %, 2,4 km, 12 min, € 14,50, fixed dates relative to a fixed clock (`2026-09-30 10:00 Europe/Amsterdam`) so previews and snapshots are stable.
- Links: real routes on the configured `PublicWebBaseUrl` with token value `voorbeeld` (e.g. `/account/wachtwoord-instellen?t=voorbeeld`); these pages show "Deze link is niet (meer) geldig" for that token. API base: `https://api.voorbeeld.invalid`. No API key, no password anywhere.
- Per-language sample names stay the same (user data is never translated).
- **Delete** `BuildContextAsync`'s vacancy query and the `PublicApiBaseUrl` lookup. `EmailCatalogService` no longer needs `JobsyDbContext` for previews (it still writes the log row for sends).
- The same context feeds the snapshot tests (02), so preview = snapshot.

## 08.2 Preview endpoint
- `GET api/settings/email-templates` returns per key: `key`, `name` (nl label), `kind` (E/O/S), `reason`, `hasMascot`, `parked` (dep D: AmbassadeurInvite while ambassadors are off), `languages`.
- `GET api/settings/email-templates/{key}/preview?lang=nl|en|pl|ro|ar&theme=light|dark` → `{ subject, preheader, html, text, kind, language, dir, headers }`. `headers` lists what the send would add (From, Reply-To, List-Unsubscribe yes/no) as display data. Unknown key/lang → 404/400.
- `theme=dark` renders with `EmailRenderMode.PreviewDark` (02): the dark CSS forced on, so the admin sees dark without a dark-mode client.
- Admin only, `[EnableRateLimiting("public-write")]` like the controller (or a read limiter if one exists), no caching of the HTML in shared caches (`Cache-Control: no-store`).

## 08.3 Test send and "send all", limited
- `POST {key}/send { lang, theme? }`: the recipient is **the signed-in admin's own e-mail**. A `to` field is only accepted when it is in `Mail:TestRecipientAllowList` (config, default empty). Anything else → 400 "Alleen naar je eigen adres of de allow-list."
- Subject gets the prefix `[Test] `; the mail has header `X-Lobsy-Test: 1` and tag `test=true` (03), and no List-Unsubscribe even for kind O.
- `POST send-all { lang }`:
  - same recipient rule
  - max **1 send-all per 15 minutes per admin**
  - paced **≤ 1 message per second** (Resend's default limit is 2 requests/s; stay under it)
  - skips parked keys
  - runs in the background with a progress row (a `Task` with `IServiceScopeFactory`, not the request thread), returns 202 with a run id; `GET send-all/{runId}` for progress
- Daily cap per admin: `Mail:TestDailyCap` (default **100**) for single + send-all together. Over the cap → 429 with the reset time (Amsterdam).
- Every test send writes an audit row: dep G present → `IAdminAuditLog` (`action = "email.test-send"`, key, language, recipient redacted); absent → `PlatformLog` Info with the redacted recipient (as today). Never log the HTML.

## 08.4 Page
- Route: `/admin/content/emails` (dep G present; `/admin/mail-test` 301s there, nav label "E-mails & meldingen") or `/admin/mail-test` (absent; nav label "E-mails"). `InteractiveServer`, Admin role, `PageSeoCatalog` private, `PageHelpDocs` entry.
- Layout: a list of the 31+ keys on the left (grouped Kandidaat / Werkgever / Registratie & account / Intern, badges "Optioneel" for kind O, "Code" for S, "Geparkeerd" for parked), the preview on the right.
- Preview toolbar: language (nl/en/pl/ro/ar, ar shows "RTL"), theme (licht/donker), width (600 / 375), tab HTML / Tekst / Headers.
- HTML preview: `<iframe sandbox="" srcdoc="…">` (empty sandbox: no scripts, no same-origin, no forms, no top navigation). Width fixed to 600 or 375 px, height auto from content via a fixed generous height + scroll (no script in the frame). Links inside the frame don't navigate (sandbox); show "Links werken niet in het voorbeeld" under the frame.
- Text tab: `<pre>` with the text part. Headers tab: the `headers` data.
- Buttons: "Stuur test naar mij" (single) and "Stuur alles naar mij" (send-all, with a confirm dialog showing the count, the pacing and the remaining daily cap). Show the progress of a running send-all.
- The page has no free-text recipient field unless `TestRecipientAllowList` is non-empty; then it's a dropdown of the allow-list.
- Strings via `UiStrings` (nl/en at least; the rest per the usual UI rule), `docs/i18n/untranslated-baseline.txt` may not grow.

## Tests
- `EmailPreviewTests`: every key × 5 languages × light/dark returns 200 with non-empty html/text; the service makes **no** DB query (use a `JobsyDbContext` that throws on `Vacancies` access, or assert the service no longer takes the context for previews); fixed clock → stable output.
- `EmailTestSendLimitsTests`: recipient forced to the admin's own address; allow-list honoured; `[Test] ` prefix; no List-Unsubscribe; second send-all within 15 min → 429; daily cap → 429; pacing (fake clock, ≥ 1 s between sends); parked keys skipped.
- `EmailCatalogAuditTests`: audit row per send with the recipient redacted.
- bUnit (if the project has it) or a Playwright smoke test `AdminEmailPreviewSmokeTests`: page loads, iframe has `sandbox=""` and a `srcdoc`, language switch to ar sets `dir="rtl"` in the srcdoc; add to both filter lists in `.github/workflows/pr-tests.yml`.
- `RoutesDocFreshnessTests`, `PageSeoTests`, `PageHelpDocsTests`, `BlazorPageRoleAttributesTests` green.

## Success criteria
- An admin can see every mail in every language, light and dark, desktop and mobile, without sending anything and without any real data.
- "Send all" can't mail anyone but the admin (or the allow-list), and can't exceed the limits.

Done → next: `09-e2e-render-rapport.md`.
