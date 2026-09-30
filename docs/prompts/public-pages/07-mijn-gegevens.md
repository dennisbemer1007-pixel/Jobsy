# 07. Mijn gegevens (/privacy/data): export as a file download, Dutch time, no raw errors, POST logout, 5 languages

Read `00-README.md` first (§IA, D12, Dependency E). Branch `cursor/public-pages-7` from `cursor/public-pages-6`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/public-pages-7` from `cursor/public-pages-6` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/public-pages-6)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/public-pages-7`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - Don't change the API export content, the deletion rules (reason + e-mail code) or the `/account/logout` endpoint itself (auth stack).

| | |
|---|---|
| Branch | `cursor/public-pages-7` |
| PR title | `feat(privacy): Mijn gegevens in the public style: export as file download, Dutch time for support access, no raw errors, 5 languages` |
| Mockups | `pb-d04-mijn-gegevens`, `pb-m04-mijn-gegevens` |
| Migration | none |

## Goal
You download your data as a file (never on screen), you see in plain words when support looked at your data, and deleting your account is clear and calm.

## 07.1 Today (verify first)
- `Pages/Legal/PrivacyData.razor` (98 lines):
  - `[Authorize]`, InteractiveServer `prerender: false` (shows nothing until the circuit starts), hard-coded Dutch, inline `style=""`
  - Export: `Api.ExportPrivacyDataAsync()` (`JobsyApiClient.Me.cs` L310 → `PrivacyController` L33 `GET export`) is put into `_exportJson` and rendered in a `<pre>` (L43), with the message "Export geladen. Bewaar dit bestand veilig." (L87)
  - Errors: `_message = ex.Message` (L91)
  - Support notes `note.ToString("yyyy-MM-dd HH:mm")` (L21) from `GetSupportAccessNotesAsync` (UTC `DateTime`s)
  - Delete via `UnsubscribeDialog`, then a GET navigation to `/account/logout`
- `/account/logout` accepts GET and POST (`AuthServiceCollectionExtensions.cs` L527).

## 07.2 Page (pb-d04)
- `@layout PublicLayout` (or `LegalPublicLayout`), InteractiveServer **prerender: true**. The content is in the first HTML; the circuit is only for the dialog.
- **Hero:** eyebrow "🔒 Privacy", h1 "Mijn gegevens", lead "Hier zie en download je wat Lobsy van je bewaart. Je kunt ook je account verwijderen."
- **Card 1 "Download je gegevens":**
  - text "Je krijgt één bestand (JSON) met alles wat we van je bewaren. Bewaar het op een veilige plek."
  - button **"Download mijn gegevens"** = `<a class="pub-btn pub-btn--primary" href="/privacy/data/export" download>` (works without JS)
- **Card 2 "Wie heeft je gegevens bekeken?":**
  - rows "Support bekeek je gegevens op {d MMMM yyyy} om {HH:mm}" in **Europe/Amsterdam** (reuse `AmsterdamTime` from `docs/emails` 01 if present, else add `Jobsy.Core/Time/AmsterdamTime.cs` with the same API: `ToLocal`, `FormatDate`, `FormatDateTime(utc, culture)`)
  - empty state "Niemand van Lobsy heeft je gegevens bekeken."
  - the row text comes from `Privacy.Data.SupportViewed`, not `Admin.SupportAccessNotifySubjectNote`
- **Card 3 "Account verwijderen" (danger tint):**
  - text "We vragen een reden en sturen een code naar je e-mail. Daarna blokkeren we je account en wissen we je gegevens, behalve wat we volgens de wet moeten bewaren."
  - link "Wat bewaren we?" → `/privacy#bewaren`
  - button "Account verwijderen" opens the existing `UnsubscribeDialog` (restyle only inside `.pub-theme` via `pp-` classes; no logic change)
  - after completion: a hidden form `method="post" action="/account/logout"` with antiforgery is submitted (JS interop `form.submit()`), instead of a GET navigation
- **Links:** "Terug naar de privacyverklaring" → `/privacy`.
- **Strings:** `Privacy.Data.*` in `UiStringsLegal` (5 languages).

## 07.3 Export endpoint (Web)
- `GET /privacy/data/export` (minimal API in the Web project, `RequireAuthorization()`):
  - calls the existing API export with the user's token (same handler chain as `JobsyApiClient`)
  - returns `application/json; charset=utf-8` with `Content-Disposition: attachment; filename="lobsy-mijn-gegevens-{yyyy-MM-dd}.json"` (Amsterdam date) and `Cache-Control: no-store`
  - on API failure: 302 back to `/privacy/data?export=failed`, where the page shows `Privacy.Data.ExportFailed` "Downloaden lukte niet. Probeer het zo nog eens."
  - rate limit: reuse the API's existing limit; add Web `export` 5/hour per user if none exists
  - `PlatformLog` `privacy.export` (user id), as the API may already do. Don't duplicate if the API logs it.
- No JSON ever goes into component state or the circuit.

## 07.4 Errors
- No `ex.Message`. Load failure of the support notes → hide card 2 and log. Dialog errors keep the dialog's own B1 messages (check it has no `ex.Message`; if it does, map to `Common.Error.TryAgain`).

## 07.5 Tests
- `PrivacyDataExportTests`: anonymous → 302 login; signed in → attachment header, `no-store`, JSON body from the API stub; the page HTML contains no `<pre>` and no export JSON.
- `PrivacyDataPageTests` (bUnit): prerendered content present; the support-note time for `2026-07-01T12:00Z` renders "14:00" and for `2026-12-01T12:00Z` "13:00"; empty state text; no `style=` attributes.
- `PrivacyDataLogoutTests`: after a completed deletion the page renders a POST form to `/account/logout` (no GET navigation).
- `NoRawErrorTests` for this page (source scan: no `ex.Message`).

## Success criteria
- The export arrives as a file with a dated filename; nothing of it renders on screen.
- Times are Dutch time. The page is readable without JS except the delete dialog.
- Screenshots nl desktop/mobile + ar mobile in the PR.

Done → next: `08-hoe-werkt-wie-zijn-wij.md`.
