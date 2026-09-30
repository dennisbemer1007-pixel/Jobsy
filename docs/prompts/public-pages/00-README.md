# Lobsy public and legal pages: standalone hotfix + warm redesign of hoe-werkt-lobsy, wie-zijn-wij, privacy, mijn gegevens, voorwaarden, partner and /{kvk} (Cursor run book)

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

> **Rules (repeated in every file):**
> - Branch from `origin/acceptatie` (01, and 02 when PR 01 is merged) or from the previous file's branch (stacked). ONE PR per file into `acceptatie` (01 standalone, 02+ stacked).
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/public-*` branch; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders like `[ADRES]` or internal ids to visitors.

**What this stack builds.** Dennis approved the phase-1 review (`pb-*` mockups) and all 14 proposals on 30-09 ("Akkoord").
- **01 is a standalone hotfix** that can run first and on its own:
  - (a) Lobsy's legal identity (name, address, KvK, btw-id, e-mails) comes from **one `Legal:*` config**. An empty value hides its line; never a placeholder. The mail footer shows it too.
  - (b) **`/{kvk}`** only exists for companies with a **verified KvK** and **≥ 1 public vacancy**, otherwise 404. It shows the city only, with no internal ids or address coordinates. The sitemap uses the same filter.
  - (c) `/partner/{code}` becomes noindex with a canonical to `/partner`.
  - (d) The literal "%0A" in 3 share mails is fixed.
  - (e) A simple real 404 page, and a 404 status for unknown vacancies.
- **Legal pages in the warm public style** (`PublicLayout` from `docs/landing`). One `LegalDocument` component with a table of contents, **"In het kort" blocks in 5 languages** (nl, en, pl, ro, ar; ar right-to-left), and the **full Dutch text as the official version**. Version and date come from one constant per document.
- **Privacy:**
  - the full processor list (new: **Pingen, OpenStreetMap/FOSSGIS, web-push services**; Render region **Frankfurt**)
  - retention periods straight from `PrivacyConstants`
  - a `#cookies` anchor
  - age rules consistent everywhere: **13+**, **under 16 with parental consent**, **talentpool 18+**
- **Voorwaarden:**
  - one switch "Voor werkgevers / Voor kandidaten"
  - "Wie is Lobsy" (identity + btw-id)
  - B2B prices **excl. btw**
  - liability **"what you paid in the last 12 months, at least € 250"**
  - "Iets melden" (DSA)
  - "Betaalde extra's en bedenktijd"
- **Bedenktijd:** the € 2,99 uitgebreide test can't start without the waiver checkbox (owned by `docs/tests` 01; this stack aligns the text and guards it).
- **Meldknop (DSA):** "Meld deze vacature / dit bedrijf" on vacancy and company pages, with an admin list and a reasoned decision.
- **Mijn gegevens:** the export is a **file download**, times in Dutch time, no raw errors.
- **Hoe werkt Lobsy:** static SSR (no "Laden…"), in the sitemap, real ar/pl/ro text.
- **Wie zijn wij:** a static page in 5 languages with **support@lobsy.nl**. The admin text editor is removed.
- **Partner:** B1 copy, prices **excl. btw**, "Gratis" instead of € 0,00, gated by the werkgevers-actief switch.
- **`/{kvk}`:** in the public layout, with a "Meld het" link.

> **⚖️ Lawyer review needed before go-live.** The texts in 03 (privacy), 04 (algemene voorwaarden + gebruiksvoorwaarden), 05 (bedenktijd wording) and 06 (DSA notice/decision wording) are **drafts by the dev team, not legal advice**. Each PR that changes legal text:
> - adds its changed sections to `docs/legal/review-needed.md` (document, section id, version, what changed, open question)
> - says "⚖️ Lawyer review needed" at the top of the PR body
>
> Merging to `acceptatie` is fine; the stack-end report (10) lists what must be reviewed before `main`.

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-hotfix-juridisch-kvk.md`: **standalone hotfix** (not stacked). `Legal:*` config + `ILegalIdentity` + `GET api/site/legal` (empty → hidden, also mail footer, `CandidateConsentRulesTests` updated); `/{kvk}` only verified + ≥ 1 public vacancy else 404, city only, no ids/coords, sitemap filtered; `/partner/{code}` noindex + canonical; `%0A` in 3 places; simple `/status/{code}` 404 page + 404 for unknown vacancies | `cursor/public-hotfix` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-juridisch-fundament.md`: `LegalDocument` component (TOC, "In het kort", version line, print), `LegalDocumentVersions`, `UiStringsLegal` (5 languages), `LegalProcessors` catalog, `LegalRetention` table from `PrivacyConstants`, static SSR + `PublicLayout` for the legal routes, footer legal line | `cursor/public-pages-2` | `cursor/public-hotfix` (or `origin/acceptatie` when PR 01 is merged) | `acceptatie` |
| 03 | `03-privacy.md`: privacy statement rewritten on 02 (identity, processors incl. Pingen/OSM/web push, Render Frankfurt, retention, `#cookies`, age 13/16/18, AI, rights), outdated API-mail line fixed ⚖️ | `cursor/public-pages-3` | `cursor/public-pages-2` | `acceptatie` |
| 04 | `04-voorwaarden.md`: algemene voorwaarden + gebruiksvoorwaarden on 02, audience switch, Wie is Lobsy, excl. btw, liability cap, typos, minors, Iets melden (DSA), Betaalde extra's en bedenktijd ⚖️ | `cursor/public-pages-4` | `cursor/public-pages-3` | `acceptatie` |
| 05 | `05-bedenktijd.md`: waiver gate for the € 2,99 uitgebreide test: align with `docs/tests` 01 (present) or guard + follow-up (absent); waiver text version = voorwaarden version ⚖️ | `cursor/public-pages-5` | `cursor/public-pages-4` | `acceptatie` |
| 06 | `06-meldknop.md`: `ContentReport` (migration `AddContentReports`), "Meld deze vacature / dit bedrijf" dialog (anonymous, rate-limited), admin list + decision with reasons, notifier + employer mails, retention ⚖️ | `cursor/public-pages-6` | `cursor/public-pages-5` | `acceptatie` |
| 07 | `07-mijn-gegevens.md`: `/privacy/data` in the public style: export as file download, Dutch time, no `ex.Message`, POST logout, 5 languages | `cursor/public-pages-7` | `cursor/public-pages-6` | `acceptatie` |
| 08 | `08-hoe-werkt-wie-zijn-wij.md`: `/hoe-werkt-lobsy` static SSR + sitemap + real pl/ro/ar, no candidate redirect; `/wie-zijn-wij` static in 5 languages with support@, admin text editor removed | `cursor/public-pages-8` | `cursor/public-pages-7` | `acceptatie` |
| 09 | `09-partner-bedrijfspagina.md`: `/partner` B1, excl. btw, "Gratis", jargon out, werkgevers-actief gate; `/{kvk}` in `PublicLayout` with vestiging tabs, JSON-LD from config base URL, "Meld het" | `cursor/public-pages-9` | `cursor/public-pages-8` | `acceptatie` |
| 10 | `10-e2e-rapport.md`: Playwright E2E (desktop + mobile, 5 languages incl. RTL, no placeholders, status codes, noindex, sitemap), docs, stack-end report incl. the lawyer list | `cursor/public-pages-10` | `cursor/public-pages-9` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations/resources/snapshots), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/public-pages-3b`).

## Pointer prompt (the only prompt needed; it runs 01 … 10)
```
Run the Lobsy public/legal pages stack. First: git fetch origin && git show origin/docs/public-pages:docs/prompts/public-pages/00-README.md — read it completely.
Then read and execute each file in docs/prompts/public-pages/ on that branch strictly in the order the README's table lists (01 … 10; a/b splits where a file allows it), one file = one PR.
File 01 is a standalone hotfix: branch cursor/public-hotfix from origin/acceptatie, not stacked, PR body starts with "Standalone hotfix (not stacked)". If PR 01 already exists (the hotfix was run on its own), don't redo it: reuse its branch.
File 02 branches from cursor/public-hotfix (or origin/acceptatie if PR 01 is merged); every later file branches from the previous file's branch (stacked) and opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 02, run the dependency checks in the README's "Dependencies" section and follow the fallback for each; say in PR 02 which case applied. Re-run the checks the README names at 05, 06, 08 and 09.
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Legal texts (03, 04, 05, 06) are drafts: add them to docs/legal/review-needed.md and put "⚖️ Lawyer review needed" at the top of those PR bodies.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes. Never show ex.Message, placeholders or internal ids to visitors.
At the end report: file → branch → PR number → status, the dependency cases, what Dennis still has to provide (Legal:* values on Render, privacy@ inbox, lawyer review) and anything deferred.
```

### Pointer prompt: hotfix 01 only
```
Run only the standalone hotfix from the Lobsy public/legal pages stack. First: git fetch origin && git show origin/docs/public-pages:docs/prompts/public-pages/00-README.md (read "How to run", §0 and §IA) and git show origin/docs/public-pages:docs/prompts/public-pages/01-hotfix-juridisch-kvk.md — read it completely.
Execute only file 01: branch cursor/public-hotfix from origin/acceptatie (not stacked on anything), ONE PR into acceptatie whose body starts with "Standalone hotfix (not stacked)".
Fix (a) the legal identity: a Legal:* config section (Name, TradeName, Street, PostalCode, City, KvkNumber, VatNumber, PrivacyEmail, SupportEmail, SchoolsEmail) read by ILegalIdentity in the API (per-field fallback to the admin Bedrijfsgegevens row), served by GET api/site/legal, used by /privacy, the Wie zijn wij default text and the mail footer; an empty value hides its line, never a placeholder; delete PlatformLegalIdentity and update CandidateConsentRulesTests; (b) /{kvk}: only companies with KvkVerificationStatus Verified and at least one publicly visible vacancy, otherwise 404; the DTO has the city only, no company GUIDs, no street address, no coordinates; vacancies come from a new kvk-scoped endpoint; the sitemap company paths use the same filter; (c) /partner/{code}: noindex + canonical /partner; (d) the %0A double encoding in PartnerSales, Tokens and SalesToolkit; (e) a simple real 404 page via UseStatusCodePagesWithReExecute("/status/{0}") for HTML requests, and a real 404 status for unknown/non-public vacancies on /vacancies/{id}.
Build and test; if red or a success criterion can't be met, push, open the PR as draft, stop and report.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes. Don't start file 02.
Report: branch → PR number → status, the new endpoints and config keys, the test list, what Dennis must set on Render (Legal__* env vars), and anything deferred.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/i18n/README.md`, `docs/release-flow.md` and `SECURITY.md`.
2. **File 01** stands alone. Run it as described in the file, whether or not the rest of the stack runs.
3. Before 02, run the **Dependencies** checks below and note the outcome (it goes into PR 02).
4. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column:
      - File 01: `git checkout -b cursor/public-hotfix origin/acceptatie`.
      - File 02: `git checkout -b cursor/public-pages-2 cursor/public-hotfix` (or `origin/acceptatie` if PR 01 is merged).
      - Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, plus the Playwright suites the file names if you can. Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. The body starts with `Standalone hotfix (not stacked)` (01) or `Stacked on #<prev PR> (<prev branch>)` (02+), then the PR body items from §0.
   6. Note the PR number, go on to the next file.
5. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely. Push what you have, open that PR as **draft** with the failure described, and don't continue.
6. At the end, report the table file → branch → PR number → status (green or draft/red), plus anything deferred.
7. **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: only 06 adds one (`AddContentReports`). No other file adds a migration. Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you (or a dependency check flips at a re-check point), `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches:** 01 standalone, 02+ stacked (see above). **ONE PR per file, always into `acceptatie`.** The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
- **Stop on red.** `dotnet build` + `dotnet test` after each file. If red and not fixable in scope: stop, push, open a draft PR with the failure, report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST). Re-check line numbers before editing. Production (`main` @ `9a2c0d49`) is 214 commits behind; don't fix `main`-only symptoms.
- **Mockups:** branch `docs/public-pages`, folder `docs/mockups/public-pages/`. Read with `git fetch origin docs/public-pages && git show origin/docs/public-pages:docs/mockups/public-pages/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - Desktop 1440 (1x, full page) / mobile 390 (2x, full page):
    - `pb-d01-hoe-werkt-lobsy` / `pb-m01-hoe-werkt-lobsy`
    - `pb-d02-wie-zijn-wij` / `pb-m02-wie-zijn-wij`
    - `pb-d03-privacy` / `pb-m03-privacy`, plus `pb-m08-privacy-arabisch-rtl`
    - `pb-d04-mijn-gegevens` / `pb-m04-mijn-gegevens`
    - `pb-d05-algemene-voorwaarden` / `pb-m05-gebruiksvoorwaarden`
    - `pb-d06-partner` / `pb-m06-partner`
    - `pb-d07-bedrijfspagina` / `pb-m07-bedrijfspagina`
    - (all `.png`)
  - HTML in `html/` (self-contained). Builder: `build.py` + `pub_ui.py` (needs `docs/mockups/landing/` from `docs/landing` next to it). Use them for copy, spacing and colour mixes, **not** as code to paste. The review with all findings is `docs/mockups/public-pages/REVIEW.md`.
  - Grey "Notities (alleen in de mockup)" blocks, "Voorbeelddata" pills and the yellow `Legal:*` config chips are mockup-only.
  - **Where a mockup and this spec differ, this spec wins.** Known differences:
    - **Processor table in pb-d03** shows a selection. The full list is 03.3.
    - **"Download als pdf"** in the privacy TOC = "Afdrukken of opslaan als pdf" (browser print with print CSS, 02.5). There's no server-side PDF.
    - **pb-d05 shows 5 sections** of the algemene voorwaarden. The full outline is 04.2.
    - **pb-d07 map** is illustrative. The map shows only the vacancy pins that the banenkaart already shows.
    - **Footer legal line** "Lobsy · [adres uit config] · KvK [uit config]" is `ILegalIdentity` data (01). Empty parts are omitted.
- **Design.** Everything renders inside `PublicLayout` / `.pub-theme` from `docs/landing` 01 (Dependencies A) and follows its approved public-theme deviations (landing README §0 "Design system"). New CSS goes in `wwwroot/css/features/public-pages.css` (BEM prefix `pp-`, scoped under `.pub-theme`), linked in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-x` and added to `Jobsy.Tests/asset-versions.json`. Never append to `app.css`. No inline `style=""` in `.razor`, no `!important`, logical properties only (RTL), breakpoints 640/900/1024, tap targets ≥ 44 px, visible focus, AA contrast, `prefers-reduced-motion`.
- **Render modes.** Public read pages are **static SSR** (`[ExcludeFromInteractiveRouting]`): `/privacy`, `/algemene-voorwaarden`, `/gebruiksvoorwaarden`, `/hoe-werkt-lobsy`, `/wie-zijn-wij`, `/partner`, `/status/*`. `/{kvk}` stays InteractiveServer **with prerender** (map) but its content is in the prerendered HTML. `/privacy/data` is `[Authorize]`, InteractiveServer **with prerender** (the delete dialog), and its export is a plain download link (07). Small enhancements use a deferred module ≤ 3 KB gz (`public-pages.js`: TOC scroll-spy, copy link, share); pages work without JS.
- **Strings.** All new UI text via `@Culture["…"]` in new modules registered in `UiStrings.cs`:
  - `Localization/UiStringsStatus.cs` (prefix `Status.`; 01, shared with `docs/errors`)
  - `Localization/UiStringsLegal.cs` (prefixes `Legal.`, `Privacy.`, `Terms.`; 02–05)
  - `Localization/UiStringsPublicInfo.cs` (prefixes `HowLobsy.`, `About.`, `PartnerPage.`, `CompanyPage.`, `Report.`; 06–09)
  - Every key in **nl, en, pl, ro, ar** from the file that adds it (`LocalizationParityReportTests` green). nl and en are final. pl/ro/ar are B1 drafts listed for native review in `docs/i18n/public-pages-review.md`. **`docs/i18n/untranslated-baseline.txt` may not grow.** Resource values never contain HTML; links are composed in markup.
- **Legal texts (D3).**
  - The **full text is Dutch and official**. It is written as razor markup in one component per document under `Components/Legal/Docs/` (e.g. `PrivacyNl.razor`, sections with stable ids), not as resource strings.
  - Each section has a short **"In het kort"** block from `UiStringsLegal` in the reader's language. When the culture isn't nl, the top note says (in that language): "De Nederlandse tekst is de officiële versie. De blokken 'In het kort' staan in jouw taal."
  - B1 Dutch, "je", short sentences, no jargon. Legal precision beats brevity where they conflict; say so in the lawyer list.
- **Legal identity (D1).** Only via `ILegalIdentity` / `LegalIdentityDto` (01). Never a literal company name, address, KvK or e-mail in markup, except `support@lobsy.nl`/`privacy@lobsy.nl` as **config defaults** in `LegalOptions`.
- **Errors.** No `ex.Message` in any page this stack touches. Map API failures to `Common.Error.TryAgain` ("Dat lukte niet. Probeer het zo nog eens.") or a specific key; log the exception with the request id.
- **Security and privacy (every file):**
  - public endpoints are `[AllowAnonymous]` + `public-read` rate limit, and return only what the page shows
  - no internal GUIDs, coordinates of company addresses, contact data or verification states in public DTOs
  - anonymous POSTs (06) are antiforgery-protected static forms + rate limit `report`
  - PlatformLog rows redact e-mails (`EmailServiceStub.RedactEmail`)
- **Docs and guards to update when routes change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (`PageSeoTests`), `Seo/SeoEndpoints.cs` (sitemap), `Help/PageHelpDocs.cs` (`PageHelpDocsTests`), `BlazorPageRoleAttributesTests`, `CHANGELOG.md`.
- **Must NOT touch:**
  - the design of logged-in `MainLayout` pages (no `pp-`/`pub-` rule may affect them)
  - Mollie/checkout internals and `FlexCommercialSettings` semantics (05 only reads/aligns)
  - `VacancyDiscovery` internals, `jobMap*.js` (09 only calls existing map functions)
  - `LocalAuthCredential`, lockout, MFA pages, `MfaEnforcementMiddleware`
  - the other stacks' in-progress branches (`cursor/landing-*`, `cursor/emails-*`, `cursor/tests-*`, `cursor/admin-redesign-*`, `cursor/auth-*`, `cursor/errors-*`, `cursor/werkgevers-actief`, `cursor/werkgever-aanmelding-*`): never branch from or merge them
- **PR description:**
  - what changed and why
  - screenshots desktop 1440 + mobile 390 of each changed page (nl), plus ar mobile from 02 on
  - the new/changed URL + endpoint list
  - test list
  - "⚖️ Lawyer review needed" first line for 03–06
  - "Out of scope / deferred"
- **Playwright in CI:** every new Playwright test class goes into **both** filter lists in `.github/workflows/pr-tests.yml` (excluded from the unit step, included in the smoke step), like the existing classes.

---

## §IA. Routes and endpoints (the contract for all files)

| Route / endpoint | What | Who | Render | SEO | Built in |
|---|---|---|---|---|---|
| `GET api/site/legal` | `LegalIdentityDto { name, tradeName, street?, postalCode?, city?, kvkNumber?, vatNumber?, privacyEmail, supportEmail, schoolsEmail? }`. Only non-empty values | anonymous, `public-read` | — | — | 01 |
| `/status/{code:int}` | Status page (01: simple; `docs/errors` 01 restyles). Only reached via re-execute; a direct GET renders with that status | anonymous | static SSR | noindex | 01 |
| `GET api/public/companies/{kvk}` (+ `/{vestiging}`) | `PublicCompanyPageDto { kvk, vestigingsnummer?, name, city?, logoUrl?, branches[{ name, city?, vestigingsnummer?, path, vacancyCount }] }`. 404 unless verified + ≥ 1 public vacancy | anonymous | — | — | 01 |
| `GET api/public/companies/{kvk}/vacancies` (+ `/{vestiging}/vacancies`) | Public vacancy cards of that KvK (same shape and filter as discovery) | anonymous | — | — | 01 |
| `/{kvk}` `/{kvk}/{vestiging}` | Company page | anonymous | InteractiveServer prerender, `PublicLayout` (09) | index only when served 200 | 01 (data), 09 (design) |
| `/partner` | Partner page | anonymous; gate werkgevers-actief (09) | static SSR (09) | index (ON) | 09 |
| `/partner/{code}` | Same page with sales code | anonymous | static SSR | **noindex**, canonical `/partner` | 01 |
| `/privacy` (`#cookies`, section ids) | Privacy statement | anonymous | static SSR, `PublicLayout` | index | 02, 03 |
| `/algemene-voorwaarden`, `/gebruiksvoorwaarden` | Terms (one component, audience switch) | anonymous | static SSR | index | 02, 04 |
| `/privacy/data` | Mijn gegevens | `[Authorize]` | InteractiveServer prerender, `PublicLayout` | noindex | 07 |
| `GET /privacy/data/export` | JSON file download (`Content-Disposition: attachment`, `no-store`), Web endpoint proxying the existing API export for the signed-in user | `[Authorize]`, same-user | endpoint | noindex | 07 |
| `/melden?type=vacancy|company&id=…` | Report form (DSA) | anonymous | static SSR form + antiforgery | noindex | 06 |
| `POST api/reports` | Store a report | anonymous, rate limit `report` (5/hour/IP, 20/day/IP) | — | — | 06 |
| `/admin/moderation` (today's placeholder) or the admin-redesign tab "Meldingen" on `/admin/vacatures/moderatie` | Report list + decisions | Admin | InteractiveServer | private | 06 |
| `GET api/site/prices` (only if no public price endpoint exists yet) | `{ deepAnalysisFromEuroInclVat }` for public copy | anonymous, `public-read` | — | — | 08 |
| `/hoe-werkt-lobsy` | How Lobsy works | anonymous + roles | static SSR | index, in sitemap | 08 |
| `/wie-zijn-wij` | About | anonymous | static SSR | index | 08 |
| `/admin/about`, `GET/PUT api/settings/about`, `GET api/site/about` | removed in 08 (`/admin/about` 301 → `/admin`) | — | — | — | 08 |

## Decisions (Dennis "Akkoord" 30-09 on the 14 proposals; extra defaults marked *extra*)
- **D1. One legal identity source.** `Legal:*` config in the API (`LegalOptions`), exposed by `ILegalIdentity` and `GET api/site/legal`. It is used by the legal pages, the footer, the mail footer, `/wie-zijn-wij` and `docs/errors` pages. An empty value hides its line (never a placeholder). *(Dennis, 30-09)*
  - *extra:* each empty `Legal:*` field falls back to the admin Bedrijfsgegevens row (`PlatformCompanySettings`, `/admin/company`), which the invoices already use. When both are set and differ, the API logs one warning at startup and `/admin/company` shows "Let op: de instelling Legal:* op de server wint." So Dennis can fill either place.
- **D2. Hotfix scope:** placeholders, `/{kvk}`, partner noindex, `%0A`, simple 404 + 404 for unknown vacancies, as standalone PR 01. *(Dennis, 30-09)*
- **D3. Languages:** the Dutch text is official, plus "In het kort" in 5 languages. *(Dennis, 30-09)*
- **D4. `/{kvk}`:** only companies with `KvkVerificationStatus.Verified` and ≥ 1 publicly visible vacancy (`VacancyVisibilityRules.IsPubliclyVisible`), otherwise 404. City only. *(Dennis, 30-09)*
  - *extra:* vestiging tabs only for branches with a public vacancy. The logo is shown only when it's an uploaded logo.
- **D5. Partner prices** shown **excl. btw**, with "Alle prijzen zijn exclusief btw." under the table. 0 tokens → "Gratis". *(Dennis, 30-09)*
  - *extra:* `TokenVatPricing` says token packs are **charged incl. 21 % btw**, so today's catalog amounts are probably incl. btw. 09.2 checks this. Excl. btw is the main amount, and the incl. amount is shown small next to it ("€ 10,33 excl. btw · € 12,50 incl."). The amounts Mollie charges don't change.
- **D6. Liability cap:** "at most what you paid Lobsy in the 12 months before the event, and at least € 250"; the usual exceptions (intent, gross negligence). ⚖️ *(Dennis, 30-09)*
- **D7. Bedenktijd € 2,99:** a required checkbox "Ik wil dat de analyse meteen start. Ik weet dat ik dan geen 14 dagen bedenktijd heb." Without it the checkout can't start. *(Dennis, 30-09)*
- **D8. Meldknop (DSA):** "Meld deze vacature" / "Meld dit bedrijf", reports in an admin list, a decision with a reason, a mail to the notifier (when they left an e-mail) and to the employer when content is removed. *(Dennis, 30-09)*
- **D9. Wie zijn wij contact:** `support@lobsy.nl` (`Legal:SupportEmail`); `privacy@` only for privacy questions. *(Dennis, 30-09)*
- **D10. Wie zijn wij** is a static page in 5 languages. The admin text editor is removed. *(Dennis, 30-09)*
- **D11. Age:** 13+ may use Lobsy (`CandidateConsentRules.MinimumCandidateAge`); under 16 needs parental consent (`CandidateConsentRules.ParentalConsentAge`); the talentpool is 18+ (`TalentPoolMinimumAge`). Every text reads these constants; the same sentence appears in privacy §9 (`#jonger`) and gebruiksvoorwaarden §2. *(Dennis, 30-09)*
- **D12. Mijn gegevens export** is a file download, never shown on screen. *(Dennis, 30-09)*
- **D13 (errors stack).** Maintenance, support code, 404 buttons and the closed-vacancy page are in `docs/errors`.
- **D14. Privacy contact default** *extra*: `Legal:PrivacyEmail` default empty → the page shows `SupportEmail` for privacy questions. Dennis confirms whether `privacy@lobsy.nl` is a real inbox, and sets it if so.
- **D15. Version and date** *extra*: one constant per document in `Jobsy.Core/Legal/LegalDocumentVersions.cs` (`Privacy = "2026-10"`, `Terms = "2026-10"`, with the date as `DateOnly`). The version line reads "Versie oktober 2026 · geldig vanaf {datum}". `PrivacyConstants.CurrentConsentVersion` stays the consent version and is **not** changed by this stack (a new consent round is Dennis' call).
- **D16. Change log** *extra*: every legal document ends with "Wat is er veranderd?", listing the last 3 versions from `LegalDocumentVersions.History`.
- **D17. Print** *extra*: "Afdrukken of opslaan als pdf" uses the browser print dialog with a print stylesheet (all sections open, no header/footer chrome). No server PDF.

## Dependencies (check before 02; say in PR 02 which case applied)
- **A. Public theme (`docs/landing` 01).** Check: `git grep -n "class PublicRoutes\|IEmployersSwitch" origin/acceptatie -- Jobsy.Web Jobsy.Core` and `git ls-tree -r --name-only origin/acceptatie -- Jobsy.Web/Components/Layout/PublicLayout.razor Jobsy.Web/wwwroot/css/features/public-theme.css`.
  - **Present:** every page here uses `@layout PublicLayout`, `.pub-theme` primitives (`pub-card`, `pub-btn--primary|secondary`, `pub-chip`, `pub-eyebrow`), `PublicRoutes`, the `/taal/{lang}` switch and `LobsyMascot` (landing 02) where the mockups show the mascot. The footer legal line (02.6) goes into `PublicLayout`'s footer.
  - **Absent:** 02 adds `Components/Layout/LegalPublicLayout.razor` (header: logo → `/`, links Hoe het werkt · Banenkaart · Inloggen, today's `LanguageSelector`; footer: today's `AppFooter` links + the legal line) with root class `pp-theme`. It defines the few warm tints it needs in `public-pages.css` via `color-mix()` on existing tokens (same recipes as `pub_ui.py`). Add **`docs/public-pages-followups.md`**: "landing 01: `LegalPublicLayout` → `PublicLayout`, `.pp-theme` → `.pub-theme`, drop the duplicated tints, use `PublicRoutes`." Never create landing's names (`PublicLayout`, `PublicRoutes`, `pub-*`).
  - Re-check at 08 and 09.
- **B. Emails renderer and footer (`docs/emails` 02).** Check: `git grep -n "class EmailDocument\|interface ITransactionalMailer\|LegalAddress" origin/acceptatie -- Jobsy.Core Jobsy.Infrastructure`.
  - **Present:** `MailOptions.LegalName/LegalAddress/KvkNumber` are **filled from `ILegalIdentity`** (one `IPostConfigureOptions<MailOptions>` or the renderer reads `ILegalIdentity` directly; pick the smaller change and say which). 01's footer marker (01.2.4) is removed where the renderer replaced `EmailLayout`. The notifier/employer mails of 06 are registry entries (kind E, reason `Reported` / `ManagesVacancies`) with `EmailStrings` keys in 5 languages.
  - **Absent:** 01's marker approach stays. 06 adds `TransactionalEmails.ReportReceived/ReportDecided/ContentRemoved` in today's `EmailLayout`. Add a line to `docs/public-pages-followups.md`: "emails 02: read `ILegalIdentity` for the legal footer; register ReportReceived/ReportDecided/ContentRemoved."
  - Re-check at 06.
- **C. Tests payment + waiver (`docs/tests` 01).** Check: `git grep -n "WaiverAcceptedAtUtc\|waiver_required" origin/acceptatie -- Jobsy.Core Jobsy.Api Jobsy.Infrastructure`.
  - **Present:** 05 only aligns: the waiver sentence and `WaiverTextVersion` equal `LegalDocumentVersions.Terms`, the order summary links to `/gebruiksvoorwaarden#bedenktijd`, and guard tests (05.3) prove "no waiver → no checkout".
  - **Absent:** 05 does **not** build a second checkout or migration. Today the uitgebreide test can't be bought in Production at all (`DeepAnalysisService.StartCheckoutAsync` throws when `AllowStubPayments=false`, see `docs/tests` P1), so nobody can skip a waiver there. 05 adds:
    - a server guard in `DeepAnalysisService.StartCheckoutAsync` that throws `waiver_required` unless the caller passes `waiverAccepted: true`
    - the checkbox in today's `TestDetail.razor` offer (stub path on acceptatie), with no new columns: log the acceptance in `PlatformLog` `deep.waiver-accepted` with the version
    - a follow-up line: "tests 01: persist `WaiverAcceptedAtUtc`/`WaiverTextVersion` = `LegalDocumentVersions.Terms`; keep public-pages 05 guard tests green."
  - Re-check at 05.
- **D. Admin redesign (`docs/admin-redesign` 01/02/05).** Check: `git grep -n "class AdminNav\b\|AdminNav.cs\|interface IAdminAuditLog" origin/acceptatie -- Jobsy.Web Jobsy.Core`.
  - **Present:** 06's report list is a tab "Meldingen" on `/admin/vacatures/moderatie` (item label "Moderatie"), decisions write `IAdminAuditLog` rows. 08 removes the "Wie zijn wij" tab from `/admin/content/paginas` (the flyer tab stays; the page title becomes "Werkgeversflyer" if only one tab is left, per admin-redesign's one-naming-source rule in `AdminNav.cs`).
  - **Absent:** 06 fills today's placeholder page `/admin/moderation` (`ModerationAdmin.razor`, "Nog niet beschikbaar") with the report list, keeping its route and nav item, and writes `PlatformLog` rows. 08 removes `/admin/about` and its nav item. Follow-up line for admin-redesign.
  - Re-check at 06 and 08.
- **E. Auth (`docs/auth` 03).** Check: `git grep -n "AuthPublicLayout\|au-theme" origin/acceptatie -- Jobsy.Web`. Nothing here depends on it. When present, `/privacy/data` (07) links "Inloggen" through the same `returnUrl` helper (`AuthRedirects.ResolveRequestedReturnUrl`), and the legal pages keep the same header as the auth pages (both use A's layout). Re-check at 07.
- **F. Werkgevers actief switch (`docs/mijn-paspoort` 01 / landing 01 `IEmployersSwitch`).** Check: `git grep -n "interface IEmployersSwitch\|interface IFeatureFlags" origin/acceptatie -- Jobsy.Core Jobsy.Web`.
  - **Present:** `/partner`, `/partner/{code}` and `/{kvk}` answer **302 → `/`** when OFF. The voorwaarden switch defaults to "Voor kandidaten" and hides employer-only sections' links from the nav (the text stays reachable at `/algemene-voorwaarden`). The sitemap drops `/partner` and company URLs when OFF (landing §S).
  - **Absent:** no gate; follow-up line "werkgevers-actief: gate `/partner*` and `/{kvk}` (302 → /)".
  - Re-check at 09.
- **G. Werkgever-aanmelding (`docs/werkgever-aanmelding` 06: Pingen letter).** Check: `git grep -n "Pingen" origin/acceptatie -- Jobsy.Core Jobsy.Infrastructure`. **Either way** the processor row "Pingen" is listed in 03 (the review found it's planned). **Absent:** the row carries `Status = Planned`, and the page says "(vanaf de start van brief-verificatie)". A catalog test (03.6) flips it when the code lands.
- **H. Errors stack (`docs/errors` 01).** Check: `git grep -n "class ErrorLayout\|SupportCode" origin/acceptatie -- Jobsy.Web Jobsy.Core`.
  - **Present:** 01's simple `/status/{code}` page is already replaced. Don't touch it. `/{kvk}` 404 (09) and report-form errors use `ErrorLayout`'s support-code helper.
  - **Absent:** keep 01's page. Re-check at 09.
- **Recommended landing order:** 01 as soon as possible (live placeholders on lobsy.nl/privacy + company-page exposure), then landing 01 before 02 if it's close. Not a hard requirement: every case above has a fallback. Never branch from an unmerged branch of another stack.
