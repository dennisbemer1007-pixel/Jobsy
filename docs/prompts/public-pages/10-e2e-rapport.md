# 10. Playwright E2E (desktop + mobile, 5 languages incl. RTL), status codes, SEO and privacy guards, docs, stack-end report

Read `00-README.md` first (§0, §IA, Decisions, Dependencies). Branch `cursor/public-pages-10` from `cursor/public-pages-9`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/public-pages-10` from `cursor/public-pages-9` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/public-pages-9)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/public-pages-10`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - This file adds tests, docs and the report. Product code changes only to fix what these tests find (each fix named in the PR). Every new Playwright class goes into **both** filter lists in `.github/workflows/pr-tests.yml`.

| | |
|---|---|
| Branch | `cursor/public-pages-10` |
| PR title | `test(public): Playwright E2E for the public and legal pages (desktop/mobile, 5 languages, RTL), status/SEO/privacy guards, docs + stack report` |
| PR body starts with | `Stacked on #<PR 09> (cursor/public-pages-9)` + the stack-end report (10.5) |
| Mockups | all `pb-*` (screenshots are compared by eye in the PR, not pixel-diffed) |
| Split seam | **10a** = Playwright (10.1–10.2); **10b** = guards + docs + report (10.3–10.5) |

## 10.1 Playwright page matrix (`PublicPagesPlaywrightTests`)
Run against the CI stack (`.github/scripts/start-ci-stack.sh`, `http://127.0.0.1:5201`) with seeded data. The seed must include:
- one **verified** company with 2 branches and 3 public vacancies
- one **Pending** and one **Failed** company, each with a public vacancy
- one verified company with only a draft vacancy

For `/hoe-werkt-lobsy`, `/wie-zijn-wij`, `/privacy`, `/algemene-voorwaarden`, `/gebruiksvoorwaarden`, `/partner`, `/{kvk-verified}`, `/privacy/data` (signed in as a candidate), `/melden?type=company&id={kvk}`, in **nl, en, pl, ro, ar** (via `/taal/{lang}` or `?lang=`, whichever 02 uses), at **1440×900** and **390×844**, assert:
- status 200; one `<h1>`; `<html lang>` = culture; `dir="rtl"` only for ar
- no horizontal overflow at 390 (`scrollWidth <= innerWidth`)
- body text ≥ 16 px; tap targets of buttons/links in the main content ≥ 44 px high (sample the primary CTAs)
- no text matching `/\[[A-Z][A-Z \-]+\]/` (placeholders), no "Exception", "at Jobsy.", "System." in the HTML
- legal pages:
  - the TOC links scroll to existing ids
  - non-nl shows the official-text note and "In het kort" in that language
  - the Dutch body carries `lang="nl"`
- the footer legal line shows only configured parts (run once with `Legal__KvkNumber` empty: no "KvK" label)
- the privacy processor table contains Pingen, OpenStreetMap and a push-services row; Render says Frankfurt
- `/partner`: no "€ 0,00"; "Gratis" present; "exclusief btw" present
- screenshots to `artifacts/playwright-public/{page}-{lang}-{width}.png` (full page); add the path to the "Upload Playwright screenshots" step

## 10.2 Flows
- **Report:**
  - anonymous `/melden` for the verified company → success text
  - an admin opens the list, decides "Verwijderen" with a reason
  - `/{kvk}` now answers 404
  - the employer mail was captured (capturing sink if the smoke stack has one; else assert via the API test in 06 and note it)
- **Mijn gegevens:** click "Download mijn gegevens" → a download event with filename `lobsy-mijn-gegevens-*.json`; the page shows no JSON.
- **Bedenktijd:** as a candidate on acceptatie-like config (stub payments on), the pay button is disabled until the waiver box is ticked; submitting without it via a crafted POST → 400 `waiver_required`.
- **Werkgevers actief OFF** (Dependency F present; else skip with reason): `/partner` and `/{kvk}` → 302 `/`; `/hoe-werkt-lobsy` has no employer pill.
- **Signed-in how-it-works:** a candidate and a SalesManager both get 200 on `/hoe-werkt-lobsy` (no redirect).

## 10.3 HTTP guards (`PublicPagesHttpTests`, no browser)
- Status:
  - `/bestaat-niet` 404 HTML + `noindex`
  - `/vacancies/{random}` 404 + `noindex`
  - `/{pending-kvk}`, `/{failed-kvk}`, `/{draft-only-kvk}`, `/00000000` → 404
  - `/api/public/companies/{pending}` → 404
- API privacy: the verified company JSON has no `companyIds`, `address`, `latitude`, `longitude` or GUID-shaped values (regex over the body).
- SEO:
  - `/partner/ABC` noindex + canonical `/partner`
  - sitemap contains `/hoe-werkt-lobsy`, the legal pages and the verified company, and **not** the Pending/Failed/draft-only ones
- Render modes: `/privacy`, `/hoe-werkt-lobsy`, `/wie-zijn-wij`, `/partner` HTML contains the full content and no interactive component marker (`<!--Blazor:` with `"type":"server"`).
- Sources:
  - `git grep`-style test: no `ex.Message` in the pages this stack touched (list them)
  - no `PlatformLegalIdentity`
  - no `AboutPageSettingsService`

## 10.4 Docs
- `docs/legal/review-needed.md` complete: every section of privacy/terms/bedenktijd/DSA, with version, what changed and the open questions (DPO line, transfer bases per US party, Sentry region, liability wording, waiver wording, DSA timelines).
- `docs/legal/README.md` (new, short): where the texts live (`Components/Legal/Docs/`), how to bump a version (`LegalDocumentVersions` + History + change log), the "In het kort" keys, the processor and retention catalogs, and the rule "no literal identity in markup".
- `docs/public-pages-followups.md`: every "absent" dependency line from 02–09, the `AboutPageSettings` table drop, native review pl/ro/ar (`docs/i18n/public-pages-review.md`), and anything skipped in 10.2.
- `docs/ROUTES.md` checked (`RoutesDocFreshnessTests`), `CHANGELOG.md` one entry: what visitors notice (warm pages, summaries in your language, real company data rules, report button, data download).

## 10.5 Stack-end report (in the PR body and as the last message)
- PR list 01–10 with state (open/draft) and own commits.
- Dependency outcomes A–H per re-check point.
- Test summary: page matrix count (pages × 5 languages × 2 widths), flows, HTTP guards; skipped items with reason.
- Screenshot artifact name.
- **⚖️ Lawyer review list** (from `docs/legal/review-needed.md`): must be done before these texts go to `main`.
- **What Dennis still has to provide or do:**
  1. On Render (API service): `Legal__Name`, `Legal__Street`, `Legal__PostalCode`, `Legal__City`, `Legal__KvkNumber`, `Legal__VatNumber`, or fill `/admin/company` (fallback). Until then the pages show only "Lobsy" and support@.
  2. Confirm whether `privacy@lobsy.nl` is a real, read inbox → `Legal__PrivacyEmail`; otherwise privacy questions go to support@.
  3. Make sure `support@lobsy.nl` exists and is read (contact on Wie zijn wij, DSA contact point, bezwaar).
  4. Founder paragraph and optional photo for Wie zijn wij (`About.Founder.Text`, `wwwroot/images/about/founder.webp`).
  5. Native review pl/ro/ar.
  6. Check the VAT finding from 09.2 (displayed excl. amounts) against the invoices.
- Out of scope / deferred (from followups).

## Success criteria
- The page matrix is green for all pages × 5 languages × 2 widths, with screenshots uploaded.
- The HTTP guards prove: no placeholders, real 404s, no unverified company pages, no ids/coordinates in public company JSON, correct noindex/sitemap.
- The report lists the lawyer items and everything Dennis must provide.

Done → end of stack. Report (10.5) and stop.
