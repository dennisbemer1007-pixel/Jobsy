# Public and legal pages: stack-end report (files 01–10)

The run book is `docs/prompts/public-pages/00-README.md` on branch `docs/public-pages`.
Ten files, ten pull requests, all into `acceptatie`. Nothing was merged, nothing was deployed, and
neither `main` nor `acceptatie` was pushed to.

## 1. Pull requests

| # | File | Branch | PR | Base | State | Own commits |
|---|---|---|---|---|---|---|
| 01 | Hotfix juridisch + `/{kvk}` | `cursor/public-hotfix` | [#476](https://github.com/dennisbemer1007-pixel/Jobsy/pull/476) | `acceptatie` | draft (reused, standalone) | `Legal:*` config + `ILegalIdentity` + `GET api/site/legal`; `/{kvk}` verified-only; `/partner/{code}` noindex; `%0A` fix; real `/status/{code}` 404 |
| 02 | Juridisch fundament | `cursor/public-pages-2` | [#484](https://github.com/dennisbemer1007-pixel/Jobsy/pull/484) | `acceptatie` | open | `LegalDocument`, `LegalDocumentVersions`, `UiStringsLegal` (5 talen), `LegalProcessors`, `LegalRetention`, footer legal line |
| 03 | Privacyverklaring ⚖️ | `cursor/public-pages-3` | [#485](https://github.com/dennisbemer1007-pixel/Jobsy/pull/485) | `acceptatie` | open | privacy rewrite, processor table (14 rows), retention from `PrivacyConstants`, `#cookies`, 13/16/18 |
| 04 | Voorwaarden ⚖️ | `cursor/public-pages-4` | [#486](https://github.com/dennisbemer1007-pixel/Jobsy/pull/486) | `acceptatie` | open | `TermsPage` + audience switch, "Wie is Lobsy", excl. btw, liability cap (D6), "Iets melden" (DSA), bedenktijd |
| 05 | Bedenktijd ⚖️ | `cursor/public-pages-5` | [#487](https://github.com/dennisbemer1007-pixel/Jobsy/pull/487) | `acceptatie` | open | one waiver sentence (`Terms.Waiver.Checkbox`), `WaiverTextVersion` = `LegalDocumentVersions.Terms`, guard tests |
| 06 | Meldknop (DSA) ⚖️ | `cursor/public-pages-6` | [#488](https://github.com/dennisbemer1007-pixel/Jobsy/pull/488) | `acceptatie` | open | `ContentReport` + migration `AddContentReports`, `/melden`, `POST api/reports`, admin tab "Meldingen", 3 mails, retention |
| 07 | Mijn gegevens | `cursor/public-pages-7` | [#489](https://github.com/dennisbemer1007-pixel/Jobsy/pull/489) | `acceptatie` | open | `/privacy/data` on `PublicLayout`, `GET /privacy/data/export` as file download, Dutch time, POST logout |
| 08 | Hoe werkt Lobsy + Wie zijn wij | `cursor/public-pages-8` | [#490](https://github.com/dennisbemer1007-pixel/Jobsy/pull/490) | `acceptatie` | open | both pages static SSR in 5 languages, admin about-editor and `api/site/about` removed |
| 09 | Partner + bedrijfspagina | `cursor/public-pages-9` | [#491](https://github.com/dennisbemer1007-pixel/Jobsy/pull/491) | `acceptatie` | open | `/partner` B1 + excl. btw + "Gratis"; `/{kvk}` in `PublicLayout` with vestiging tabs, JSON-LD from config, "Meld het" |
| 10 | E2E + docs + this report | `cursor/public-pages-10` | [#492](https://github.com/dennisbemer1007-pixel/Jobsy/pull/492) | `acceptatie` | open | `PublicPagesPlaywrightTests`, `PublicPagesHttpTests`, two product fixes, `docs/legal/README.md`, completed review list, follow-ups, this report |

Every PR from 02 on is stacked: its diff contains the lower PRs until they merge, so review the
"own commits" column, not the whole diff.

## 2. Dependencies A–H

Checked before file 02 and re-checked at the points the run book names (05, 06, 08, 09).
No check flipped during the run.

| | Dependency | Outcome | What it meant |
|---|---|---|---|
| A | Public theme (`PublicLayout`, `public-theme.css`, `PublicRoutes`) | **Present** | Every page uses `@layout PublicLayout` and the `pub-*` primitives; no `LegalPublicLayout` / `pp-theme` fallback was built. The footer legal line went into `PublicFooter`. |
| B | Emails renderer + footer (`EmailDocument`, `ITransactionalMailer`) | **Present** | `MailOptions` legal fields are filled from `ILegalIdentity`; the 06 mails are registry entries with `EmailStrings` keys in five languages. |
| C | Tests payment + waiver (`WaiverAcceptedAtUtc`, `waiver_required`) | **Present** | File 05 only aligned wording and version; no second checkout and no migration. |
| D | Admin redesign (`AdminNav`, `IAdminAuditLog`) | **Present** | The report list is a tab on `/admin/vacatures/moderatie` and decisions write audit rows; 08 removed the "Wie zijn wij" tab through `AdminNav`. |
| E | Auth 03 (`AuthPublicLayout`, `au-theme`) | **Absent** | Nothing here depends on it. Follow-up: route "Inloggen" on `/privacy/data` through `AuthRedirects.ResolveRequestedReturnUrl` once it lands. |
| F | Werkgevers-actief switch (`IEmployersSwitch` / `IFeatureFlags`) | **Present** | `/partner`, `/partner/{code}` and `/{kvk}` carry `[RequiresFeature(Employers, FallbackPath = "/")]` → 302 `/` when OFF, and both leave the sitemap. |
| G | Pingen (werkgever-aanmelding 06) | **Present** | The Pingen processor row is `Active` with the Swiss adequacy decision as transfer basis, not `Planned`. |
| H | Errors stack (`ErrorLayout`, `SupportCode`) | **Absent** | File 01's simple `/status/{code}` page stays; `/{kvk}` renders the same body through `StatusPageContent`. Follow-up noted. |

## 3. Test summary

### Page matrix (`PublicPagesPlaywrightTests`, soft-skip without `JOBSY_E2E_BASE_URL`)

9 pages × 5 languages (nl, en, pl, ro, ar) × 2 widths (1440×900, 390×844) = **90 page loads**, each
asserting: HTTP 200 · exactly one `<h1>` · `<html lang>` matches the culture · `dir="rtl"` only for
ar · no horizontal overflow at 390 · body text ≥ 16 px · sampled primary CTAs ≥ 44 px high · no
placeholder matching `[A-Z ]+` in brackets · no `at Jobsy.`, `System.Exception` or "Unhandled
exception" in the HTML · the footer legal line shows no "KvK" label while `Legal__KvkNumber` is
unset. Legal pages additionally: every TOC link points at an existing id, "In het kort" is present,
and a non-Dutch reader sees the official-text note plus a Dutch body marked `lang="nl"`.
`/privacy` names Pingen, OpenStreetMap, a push-services row and Frankfurt; `/partner` never shows
"€ 0,00" and always names btw.

Screenshots: `artifacts/playwright-public/{page}-{lang}-{width}.png` (full page), uploaded by the
workflow artifact **`playwright-screenshots`**. The class is in both filter lists of
`.github/workflows/pr-tests.yml` (excluded from the unit step, included in the smoke step).

### Flows

| Flow | Result |
|---|---|
| Anonymous report of a verified company → success text | covered |
| Candidate "Download mijn gegevens" → download event `lobsy-mijn-gegevens-*.json`, no JSON on screen | covered |
| Signed-in candidate and SalesManager get 200 on `/hoe-werkt-lobsy` (no redirect) | covered |
| Bedenktijd: pay button disabled until the waiver is ticked | covered when the checkout offer is reachable; the server 400 `waiver_required` is covered by `BedenktijdGuardTests` (05) |
| Admin decides "Verwijderen" → `/{kvk}` 404 + employer mail | **skipped in the browser**: the smoke stack has no capturing mail sink. Covered by `ContentReportDecisionTests` and `ContentReportMailTests` (06) |
| Werkgevers actief OFF → `/partner` and `/{kvk}` 302 `/` | **skipped in the browser**: flipping the platform flag would change the shared CI stack. Covered by the `RequiresFeature` reflection tests in `PartnerPageTests` / `CompanyPageLayoutTests` (09) |

### HTTP guards (`PublicPagesHttpTests`, no browser, always run)

- `PublicPagesApiGuardTests` — the verified company JSON has no GUID-shaped value, no `companyId`,
  no `address`, no `latitude`/`longitude`, and the street never leaves the server (only the city
  does). Pending, Failed, draft-only and unknown KvK all answer 404; the crawl index that feeds the
  sitemap lists only the verified one.
- `PublicPagesWebGuardTests` — `/bestaat-niet` is a 404 with HTML and `noindex`; an unknown vacancy
  and an unknown company page are real 404s; `/partner/ABC` is noindex with a canonical to
  `/partner`; the six static public pages render complete HTML with one `<h1>`, no interactive
  component marker (`"type":"server"`) and no placeholder; the sitemap carries the public pages and
  the verified company and never `/privacy/data` or `/melden`.
- `PublicPagesSourceGuardTests` — none of the 13 pages/components this stack owns renders
  `ex.Message`, and `PlatformLegalIdentity` and `AboutPageSettingsService` stay deleted.

Nothing was skipped in the HTTP guards.

## 4. Product fixes this file found

Two bugs, both found by the new guards and fixed here:

1. **A 404 answered with an empty page.** `UseStatusCodePagesWithReExecute("/status/{0}")` rewrote
   the request and cleared the endpoint, but routing had been inserted implicitly at the top of the
   pipeline, so nothing resolved the rewritten path again: visitors got a 404 status with a blank
   body instead of the status page from file 01. `app.UseRouting()` is now explicit, directly after
   the re-execute.
2. **`/partner/{code}` could be indexed.** The page set `Index` from the *normalised* sales code, so
   any code that failed to parse (`/partner/ABC`) announced `robots: index,follow` while the catalog
   said noindex — an indexable duplicate of `/partner`. The noindex now follows the route parameter.

## 5. ⚖️ Lawyer review list

The legal texts of 03, 04, 05 and 06 are **drafts by the dev team, not legal advice**. They may ship
to `acceptatie`; they may not ship to `main` / lobsy.nl before a lawyer has gone through the
15 numbered questions in [`docs/legal/review-needed.md`](../legal/review-needed.md) §"Go-live
checklist". The headlines: the no-DPO statement, the transfer basis per US processor, the Sentry
region, the liability cap (D6), the bedenktijd waiver wording (D7), the DSA timelines and the
mandatory elements of a statement of reasons (art. 17(3)), and the age rules for minors.

## 6. What Dennis still has to provide or do

1. **Set the `Legal:*` values on Render (API service):** `Legal__Name`, `Legal__Street`,
   `Legal__PostalCode`, `Legal__City`, `Legal__KvkNumber`, `Legal__VatNumber` — or fill
   Bedrijfsgegevens on `/admin/company`, which each empty field falls back to. Until then the legal
   pages, the footer and the mail footer show only "Lobsy" and the support address. They never show
   a placeholder, so nothing is broken; it is just incomplete.
2. **Confirm whether `privacy@lobsy.nl` is a real, read inbox.** If yes, set `Legal__PrivacyEmail`.
   If not, privacy questions keep going to support@ (D14).
3. **Make sure `support@lobsy.nl` exists and is read.** It is the contact on Wie zijn wij, the DSA
   contact point and the address for an objection to a moderation decision.
4. **Founder paragraph and optional photo for Wie zijn wij:** the text key `About.Founder.Text` and
   `wwwroot/images/about/founder.webp`. Without the file the page shows an emoji avatar.
5. **Native review of pl, ro and ar.** The list is `docs/i18n/public-pages-review.md`. nl and en are
   final; the Dutch text stays the official version.
6. **Check the VAT finding from 09.2 against the invoices.** The stored catalog and pack amounts are
   the amounts Mollie charges and are therefore incl. 21 % btw; `/partner` now derives and leads with
   the excl.-btw amount. No stored price changed, but the bookkeeping should confirm that the
   catalog really is incl. btw.

## 7. Out of scope / deferred

Full list in [`docs/public-pages-followups.md`](../public-pages-followups.md). The headlines:

- Drop the obsolete `AboutPageSettings` table (cleanup migration; nothing reads it since 08).
- Auth 03 (Dependency E): route "Inloggen" on `/privacy/data` through the shared returnUrl helper.
- Errors stack (Dependency H): replace the `/status/{code}` body with `ErrorLayout` and give the
  company-page 404 and the report form a support code.
- Add a pulse-highlight row to the `/partner` tariff table when pulse becomes buyable.
- Extend `DemoCompaniesSeeder` with a Pending, a Failed and a draft-only company if the 404 cases
  should also be proven in a browser instead of against an in-memory API.
- A capturing mail sink in the smoke stack, so the DSA employer mail can be asserted end to end.
