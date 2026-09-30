# 06. Meldknop (DSA): report a vacancy or company, admin list with reasoned decisions, notifier and employer mails ⚖️

Read `00-README.md` first (§IA, D8, Dependencies B and D) and `04-voorwaarden.md` §04.5. Branch `cursor/public-pages-6` from `cursor/public-pages-5`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/public-pages-6` from `cursor/public-pages-5` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/public-pages-5)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/public-pages-6`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - Migration `AddContentReports` is the only migration in this stack. Reporter data is minimal: an optional e-mail, never an IP in the table.

| | |
|---|---|
| Branch | `cursor/public-pages-6` |
| PR title | `feat(moderation): report button for vacancies and company pages (DSA notice and action), admin report list with reasoned decisions, notifier and employer mails` |
| Mockups | `pb-d07-bedrijfspagina` ("Klopt er iets niet op deze pagina? Meld het."); the form and admin list follow the public and admin design systems (no mockup) |
| Migration | `AddContentReports` |
| Split seam | **06a** = entity + API + public form; **06b** = admin list + decisions + mails |

## Goal
Anyone can report a vacancy or a company page in two clicks. An admin decides with a reason, and the people involved hear what happened (DSA art. 16/17 basics).

## 06.1 Re-check Dependencies B and D
- **B present:** the 3 mails are registry entries.
- **B absent:** they are `TransactionalEmails` in today's layout.
- **D present:** the list is a tab "Meldingen" on `/admin/vacatures/moderatie`.
- **D absent:** the list replaces the placeholder of today's `/admin/moderation` (`ModerationAdmin.razor`: "Nog niet beschikbaar — moderatiewachtrij komt later."), keeping its route and nav item.

## 06.2 Data (`AddContentReports`)
- `ContentReport`:
  - `Id`, `TargetType` (enum `Vacancy = 1, Company = 2`), `TargetId` (Guid of the vacancy or company, server-resolved), `TargetKvk?` (for company pages)
  - `Reason` (enum: `Fake`, `Discriminating`, `Illegal`, `WrongInfo`, `Unsafe`, `Other`), `Details` (max 1000, trimmed, plain text)
  - `ReporterEmail?` (normalized, max 254), `ReporterUserId?` (when signed in)
  - `CreatedAtUtc`, `Status` (`Open`, `NoAction`, `Restricted`, `Removed`), `DecisionReason?` (max 1000), `DecidedAtUtc?`, `DecidedByUserId?`
  - Index (`Status`, `CreatedAtUtc`), index (`TargetType`, `TargetId`).
- `Company.PublicPageBlockedAtUtc?` (DateTime): set by a "Removed" company decision. 01's `PublicCompanyQuery` adds `PublicPageBlockedAtUtc == null`.
- Retention: `PrivacyConstants.ContentReportRetentionDays = 365` after the decision (open reports are kept until decided), purged by the existing daily cleanup job. `ReporterEmail` is cleared at decision time + 30 days (`ContentReportEmailRetentionDays = 30`). Both appear automatically in the privacy retention table (02.7 reflection test).

## 06.3 Public form
- **Entry points:**
  - vacancy detail: a small link "Meld deze vacature" at the bottom of the card
  - `/{kvk}` (09 places it; here add it to today's page bottom): "Klopt er iets niet op deze pagina? Meld het."
  - both → `/melden?type=vacancy|company&id={vacancyId|kvk}`
- **`/melden`** (static SSR form + antiforgery, noindex, public layout):
  - title "Iets melden", the target name (vacancy title + company, or company name)
  - reason radios (6, B1 labels), details textarea (optional, 1000), e-mail (optional: "Als je wilt dat we je laten weten wat we doen")
  - button "Melding versturen", and a line about false reports: "Meld alleen iets als je denkt dat het echt niet klopt."
  - Success: "Dank je. We kijken ernaar." (+ "Je krijgt een bevestiging per e-mail." when an e-mail was given).
- **`POST api/reports`:**
  - anonymous, rate limit `report` (5/hour and 20/day per IP; partition by IP, not stored)
  - validates the target exists and is public (a non-public or unknown target → the same success response, stored with `Status = NoAction`, `DecisionReason = "target not public"`; no oracle)
  - de-duplicates the same target + e-mail within 24 h
  - `PlatformLog` `report.created` (report id, type; e-mail redacted)
- Strings `Report.*` in `UiStringsPublicInfo` (5 languages).

## 06.4 Admin list + decisions
- **List:**
  - Open first, then newest
  - columns: Wat (type + title/name link), Reden, Wanneer (Europe/Amsterdam), Meldingen (count per target), Status
  - filter Open/Afgehandeld
  - the reporter e-mail is masked (`PersonalDataMasker`)
- **Detail drawer:**
  - the report(s) for that target, a link to the public page and to the admin vacancy/company page
  - decision buttons:
    - "Geen actie"
    - "Beperken" (vacancy only: `POST api/admin/vacancies/{id}/inactive`, the existing endpoint L765)
    - "Verwijderen" (vacancy: inactive + status `Archived` via the existing admin path; company: set `PublicPageBlockedAtUtc`)
  - A **reason is required** for Beperken/Verwijderen (textarea, B1 hint "Schrijf kort wat er mis is en welke regel het breekt."). All open reports of that target get the same decision.
- **Audit:** `IAdminAuditLog` (D present) or `PlatformLog` `report.decided` (report ids, decision; no free text in logs).
- **API:** `GET api/admin/reports?status=`, `GET api/admin/reports/{targetType}/{targetId}`, `POST api/admin/reports/decide` `{ targetType, targetId, decision, reason }` (Admin role).

## 06.5 Mails
- **ReportReceived** (to the reporter, if an e-mail was given): "We hebben je melding ontvangen" + what they reported + "We laten je weten wat we besluiten."
- **ReportDecided** (to the reporter, if an e-mail was given): decision in B1 (+ reason summary for Beperken/Verwijderen, no employer data).
- **ContentRemoved** (to the company's managers when Beperken/Verwijderen):
  - what (vacancy title or company page), the decision, the reason (escaped), the date
  - "Wil je bezwaar maken? Mail {SupportEmail} binnen 6 maanden."
  - the button goes to the employer's vacancy list
- Every value HTML-escaped; categories `Report*`. B present → registry kind E, reasons `Reported`/`ManagesVacancies`, `EmailStrings` 5 languages; absent → today's layout, nl + en.

## 06.6 Tests
- `ContentReportApiTests`: rate limit → 429; unknown target → same 200 shape; dedup; e-mail optional; no IP column; details trimmed to 1000.
- `ContentReportDecisionTests`: reason required for Beperken/Verwijderen; vacancy becomes non-public; company page answers 404 after Verwijderen (01 query); all open reports of the target are closed; audit row written.
- `ContentReportMailTests`: ReportReceived only with an e-mail; ContentRemoved escapes `<script>` in the reason; the employer mail never contains the reporter's e-mail.
- `ContentReportRetentionTests`: purge after 365 days; e-mail cleared after 30 days post-decision.
- bUnit: the form works without JS (plain POST), antiforgery token present.
- `EfModelSnapshotTests`, `PendingModelChangesTests`, `EfMigrationDiscoveryTests`, `RoutesDocFreshnessTests`, `PageSeoTests` (`/melden` noindex) green.

## Success criteria
- A report can be filed anonymously from a vacancy and a company page, lands in the admin list, and a reasoned decision notifies the employer (and the reporter when known).
- No reporter e-mail reaches the employer. No IP stored.
- 04's "Iets melden" section now links to `/melden` (update the text to name the button).
- PR body: dependency cases B/D, "⚖️ Lawyer review needed" (DSA wording, retention).

Done → next: `07-mijn-gegevens.md`.
