# ⚖️ Lawyer review needed

The legal texts of the public-pages stack are **drafts by the dev team, not legal advice**.
They may ship to `acceptatie`; a lawyer reviews them before they go to `main` / lobsy.nl.

Every PR that changes a legal text adds its sections here: document · section id · version ·
what changed · open question.

## Privacy statement (`/privacy`, `PrivacyNl.razor`) — version 2026-10-06

| Section | Id | What changed | Open question for the lawyer |
|---|---|---|---|
| 4 | `delen` | The AI row follows `Ai:Provider`. Default stays OpenAI (United States, DPF). When the provider is Mistral and the API key is set, the table lists Mistral AI (Paris, data in the EU, no transfer basis outside the EEA) and drops OpenAI. A missing Mistral key keeps the OpenAI row, because calls stay on OpenAI. Other American companies in the table are unchanged, so the “Buiten de EU” paragraph stays. | Is “Frankrijk (Parijs); gegevens in de EU” enough, or must the statement name Mistral’s EU contractual option (La Plateforme, EU workspace) as a condition? |
| 7 | `ai` | The CV sentences name Mistral AI and the EU when that provider is active, and OpenAI when it is not. The CV upload hint in nl, en, pl, ro and ar does the same. | When Mistral is on, is “gegevens blijven in de EU” accurate for a CV that may contain a name and phone number, given Mistral only keeps data in the EU if the workspace was created with the EU option? |

## Privacy statement (`/privacy`, `PrivacyNl.razor`) — version 2026-10-04

| Section | Id | What changed | Open question for the lawyer |
|---|---|---|---|
| 4 | `delen` / `buiten-de-eu` | Replaced the line that the app and database are “in the EU, so data stays in the EU”. The text now says the app and database are in Frankfurt (EU), that Render is an American company, and that a few services belong to American companies. Those parties stay in the generated processor table (place + transfer basis). | Is “Frankfurt (EU) + American parent / American subprocessors, named in the table” specific enough, or must each transfer basis be repeated in the paragraph? |
| 7 | `ai` | New B1 paragraph “AI en jouw gegevens”: no scores or rankings for employers, AI only helps the candidate understand themselves, no AI on pupil data, AI answers are labelled, a person can explain. | Does this match the AI Act role Lobsy wants to claim (not high-risk employment AI)? Is “no AI on pupil data” still true if a later feature sends pupil answers to a model? |

## Privacy statement (`/privacy`, `PrivacyNl.razor`) — version 2026-10

Rewritten in public-pages 03 on the `LegalDocument` foundation of 02: B1 Dutch, "je", a summary
per section in 5 languages, and every number read from a constant.

| Section | Id | What changed | Open question for the lawyer |
|---|---|---|---|
| 1 | `wie` | Identity comes from `ILegalIdentity` (D1) instead of literal copy. New line: "We hebben geen functionaris gegevensbescherming; dat is voor ons niet verplicht." | Is the no-DPO statement correct for Lobsy's scale and the kinds of data we process (test results of minors, talent pool)? Should we appoint one voluntarily? |
| 2 | `gegevens` | Split per group (account, werkzoekende, werkgever, salesmanager, berichten) in B1. Technical/cookie data moved to §6. The outdated line "API-credentials kunnen éénmalig per e-mail worden verstuurd" is replaced by "Een API-sleutel mailen we nooit. Je krijgt een link waarmee je de sleutel één keer ziet." (true: `CompanyApiKeyRevealController` / `OneTimeLinkPurpose`). | Is the per-group description specific enough for AVG art. 13, or does it need the categories named verbatim? |
| 3 | `waarom` | One line per legal ground with a plain example (contract, consent, legitimate interest, legal duty). | Are the examples assigned to the right grounds? In particular: is "vacatureteksten controleren" a legitimate interest, and is re-engagement mail to inactive organisations still defensible? |
| 4 | `delen` | Employers only after acceptance; the full processor table is generated from `LegalProcessors` (14 rows, new: Pingen, OSRM/Transitous/Valhalla, OpenStreetMap tiles, web-push services). Render now reads **EU (Frankfurt)** instead of "EU/VS". New "Buiten de EU" paragraph. | Per party: is the transfer basis right (DPF vs SCC)? Cloudflare, Resend, Sentry, OpenAI, Cursor, Google/Microsoft, push and video rows now say "DPF or SCC"; Render says SCC (EU region, US parent); Pingen says EU adequacy decision for Switzerland. |
| 4 | `delen` | Sentry region reads "EU of Verenigde Staten, afhankelijk van onze instelling". | Must the statement name the actual DSN region (`*.de.sentry.io` = EU) instead of both options? |
| 4 | `delen` | The Cursor row (feedback text + optional screenshot) is kept. | Is Cursor still used for feedback handling? If the feature is off, the row must go (03.3). |
| 5 | `bewaren` | The table is generated from `PrivacyConstants` (12 rows). Added the periods the review found missing: access log 2 jaar, notifications 365 dagen, feedback screenshots 90 dagen, action tokens 30 dagen, unconfirmed registration 10 minuten. Non-constant rules (7 years fiscal, withdrawn applications, unsubscribe hash) are listed as text. | Is keeping a hash of the e-mail address after account deletion (to honour an unsubscribe) the right balance against the right to be forgotten? Is 2 years for the access log defensible? |
| 6 | `cookies` | New section with a stable `#cookies` anchor and a table (name · why · how long · needed?) for `Jobsy.Auth`, `Jobsy.LastActivity`, `Jobsy.Culture`, `Jobsy.CookieConsent`, `lobsy_sales_ref`, `jobsy.gratisDna.v1` (7 days) and the engagement key after consent. One line: "Statistieken alleen na ‘Accepteer cookies’." The sales-referral text of the old §6a moved in here. | Is `lobsy_sales_ref` really "functional" (commission attribution) or does it need consent? Is the 1-year life of `Jobsy.Culture` / `Jobsy.CookieConsent` acceptable? |
| 7 | `ai` | Old §5 (location, travel time, matching), §5a (young workers) and §7 (AI) merged. States explicitly that no decision with legal effect is automated and that a human can review a score. | Is the "no automated decision-making" wording strong enough for AVG art. 22, given the Top-10 ranking and the Cultuur Fit? |
| 8 | `tests` | Old §5b in B1. The talent-pool age reads `CandidateConsentRules.TalentPoolMinimumAge`. | Is separate consent per purpose (test · profile analysis · AI · talent pool) described clearly enough, and is the group minimum of 10 candidates enough anonymisation for the regional insights? |
| 9 | `jonger` | New `Components/Legal/AgeRulesText.razor` renders one sentence from `MinimumCandidateAge` (13), `ParentalConsentAge` (16) and `TalentPoolMinimumAge` (18). The same component is used in gebruiksvoorwaarden §2 (04). | Is 13 with parental consent under 16 acceptable for the Dutch implementation of AVG art. 8, and is e-mail to a parent a sufficient "reasonable effort" to verify consent? |
| 10 | `rechten` | Rights in B1 with a "Mijn gegevens" button to `/privacy/data` and a link to the Autoriteit Persoonsgegevens. | Do we need to name the response term (one month) and the identity check explicitly? |
| 11 | `beveiliging` | Old §8 in B1 (role-based access, encrypted sessions, hashed keys and codes, rate limits, redacted logs). | Does the statement need to mention breach notification to the visitor? |
| 12 | `wijzigingen` | How we announce changes, plus the `LegalDocumentVersions` change log (D16). | Does a material change require a new consent round (`PrivacyConstants.CurrentConsentVersion` is deliberately not bumped by this stack)? |

### Open points that are not about one section

- `Legal:*` values (name, address, KvK, btw-id, privacy e-mail) come from config. Empty values hide
  their line, so the reviewed text can look incomplete until Dennis fills them on Render (D1, D14).
- `privacy@lobsy.nl` must be a real, monitored inbox before go-live (D14). Without it the page shows
  the support address.
- The pl / ro / ar "In het kort" blocks are dev-team drafts awaiting a native review
  (`docs/i18n/public-pages-review.md`). The Dutch text is the official version (D3).

## Algemene voorwaarden (`/algemene-voorwaarden`, `AlgemeneVoorwaardenNl.razor`) — version 2026-10

Rewritten in public-pages 04 on the `LegalDocument` foundation of 02. Both terms documents now render
through one `TermsPage` component with an audience switch; they share one version and one date
(`LegalDocumentVersions.Terms`). B1 Dutch, "je", a summary per section in 5 languages.

Section ids changed. The old ids render as empty anchors inside the new section, so links shared
before the rewrite still land in the right place (`wie-is-lobsy` → `wie`, `toepasselijkheid` →
`toepassing`, `account-kvk` → `account`, `betalen-btw` → `betalen`, `matching` → `vacatures`,
`kandidaatgegevens` → `sollicitaties`, `beschikbaarheid` → `beschikbaar`, `beeindiging` → `einde`).

| Section | Id | What changed | Open question for the lawyer |
|---|---|---|---|
| 1 | `wie` | **New.** Identity comes from `ILegalIdentity` (D1): name, address, KvK, btw-nummer and the support e-mail. An empty `Legal:*` value hides its row, never a placeholder. | Is the btw-nummer on a B2B terms page enough identification, or does the page also need the legal form and the registered seat spelled out? |
| 2 | `toepassing` | Old §1. Adds that a job seeker falls under the gebruiksvoorwaarden instead. | Is "door te registreren of het platform te gebruiken ga je akkoord" enough acceptance for B2B terms, or do we need a recorded tick at registration (as the consumer side has)? |
| 3 | `dienst` | Old §2 in B1. Keeps "Lobsy is geen partij bij de arbeidsovereenkomst" and adds "geen uitzendbureau". | Does the platform description need the Waadi / payrolling disclaimer spelled out, given that matching steers which candidate sees which vacancy? |
| 4 | `account` | Old §3 plus the rule of file 01: "We controleren je KvK-nummer. Zolang dat niet gelukt is, is je bedrijfspagina niet openbaar." | May we keep a company page private until verification without calling that a service limitation, and is the takeover procedure for an occupied vestiging described well enough? |
| 5 | `tokens` | Old §4 and §5 merged. Typos fixed ("bulkpakket", "early-adopterkorting"). **New:** "Prijzen op de tarievenpagina staan exclusief btw. Bij het afrekenen zie je ook het bedrag inclusief btw." (D5). No amount is typed in the text any more; prices come from the tarievenpagina and the checkout. | Is "exclusief btw op de tarievenpagina, inclusief btw in de checkout" correct for a B2B audience that may include non-VAT-registered buyers, and is "tokens zijn geen e-money" defensible as written given the prepaid balance? |
| 6 | `betalen` | Old §5 payment rules only. The refund rule is unchanged ("alleen als de wet dat verplicht of Lobsy dat uitdrukkelijk toezegt"); we deliberately invented no new refund right. | Is a blanket "no refund of unused tokens" enforceable against a small business, and does the prepaid balance need an expiry rule? |
| 7 | `vacatures` | Old §6 and §6a merged. Keeps the ban on a minimum-age filter and the Arbeidstijdenwet caveat. | Does the clause carry enough of the employer's own responsibility under the Wet gelijke behandeling, now that the text is shorter? |
| 8 | `melden` | **New** (DSA notice and action + statement of reasons). Confirmation on report, "meestal binnen 5 werkdagen", a reasoned decision to the employer, objection within 6 months, and a contact point for authorities and users in Dutch or English. | Is Lobsy a "hostingdienst" / online platform under the DSA at this scale, and if so: are the 5-working-day indication, the 6-month objection window and a single support mailbox as the point of contact sufficient (art. 11, 12, 16, 17, 20)? |
| 9 | `sollicitaties` | Old §7 in B1. Keeps progressive disclosure of candidate PII. | Is the employer an independent controller for the application data as stated, or do we need a processor agreement for the part Lobsy stores on their behalf? |
| 10 | `beschikbaar` | Old §8 in B1. | Is a unilateral right to change terms and rates acceptable without a notice period and a right to terminate? |
| 11 | `aansprakelijkheid` | **D6.** The old cap was "wat je in 12 maanden betaalde, of € 250 als dat lager is", which is € 0 when nothing was paid. It now reads "nooit meer dan wat je in de 12 maanden vóór de gebeurtenis betaalde, met een minimum van € 250", with the usual exceptions for intent and gross negligence and for liability the law does not allow to limit. | Is a € 250 floor with a 12-month ceiling proportionate for a B2B platform, and does the exclusion list need indirect / consequential damage named explicitly? |
| 12 | `einde` | Old §10 in B1. | Does suspension for "misbruik" need a notice and a cure period before we may block tokens? |
| 13 | `recht` | Old §11 in B1. | Is a Dutch forum choice enforceable against an employer established elsewhere in the EU? |

## Gebruiksvoorwaarden (`/gebruiksvoorwaarden`, `GebruiksvoorwaardenNl.razor`) — version 2026-10

Consumer-facing counterpart, same version and date, same audience switch.
Old anchors kept as aliases (`wie-is-lobsy` → `wie`, `dienst` → `wat`, `matchscores` →
`solliciteren`, `beschikbaarheid` → `beschikbaar`, `beeindiging` → `einde`).

| Section | Id | What changed | Open question for the lawyer |
|---|---|---|---|
| 1 | `wie` | **New.** Same `ILegalIdentity` card as the employer page, with the support e-mail as contact. | Does a consumer page need the complaints route (and the ODR / ADR reference) in the identity block rather than only in `melden`? |
| 2 | `voor-wie` | **New.** Renders the shared `AgeRulesText` component (D11), so 13+ / under 16 with parental consent / talentpool 18+ read from `CandidateConsentRules` and are identical to privacy §9 (`#jonger`). | Is 13 as a minimum age with parental consent under 16 acceptable under the Dutch implementation of AVG art. 8, and can a minor validly accept these terms at all? |
| 3 | `wat` | Old §2 in B1. | Is "Lobsy is geen werkgever en geen uitzendbureau" enough, given that we show estimated salaries? |
| 4 | `account` | Old §3 in B1. | — |
| 5 | `solliciteren` | Old §4 and §4a merged; the match percentage is explicitly an estimate and a low score never blocks applying. | Does showing a match percentage need an AVG art. 22 note here as well, or is the privacy statement enough? |
| 6 | `bedenktijd` | **New (D7).** Says Lobsy is free, that the price of a paid extra is shown **inclusief btw** before paying, and that the analysis starts immediately so the 14-day withdrawal right is waived with a required tick. The tick sentence is one string key (`Terms.Waiver.Checkbox`) that the checkout reuses word for word; the € 2,99 amount is not typed here. It also says that under `ParentalConsentAge` a parent must consent first. | Does this wording satisfy art. 6:230m/6:230v BW (information duty + express request + acknowledgement of losing the withdrawal right) for a digital service that starts immediately? And how do we square an immediate start with parental consent for a minor — must the waiver be given by the parent? |
| 7 | `ai` | Old §5 in B1; the retention line now links `/privacy#bewaren` instead of repeating a period. | Is the disclaimer enough for an AI feature that produces a profile-like analysis the user pays for? |
| 8 | `melden` | **New,** candidate wording of the same DSA text. | Same DSA question as the employer page; additionally: does the notifier need the explicit right to escalate to an out-of-court dispute body (art. 21)? |
| 9 | `gebruik` | Old §6 in B1. | — |
| 10 | `beschikbaar` | Old §7 in B1. | — |
| 11 | `aansprakelijkheid` | Old §8, **consumer-safe**: no cap at all. Reads "Lobsy is gratis voor werkzoekenden. We beloven geen baan of match. Je wettelijke rechten als consument blijven altijd gelden." Exclusions only for employer decisions, third-party vacancy data and reliance on AI/match/travel-time output, and never for intent or gross negligence. | Are the remaining exclusions still unfair terms under the Richtlijn oneerlijke handelsbedingen now that a paid extra exists, i.e. does a paying consumer need a different clause from a free user? |
| 12 | `einde` | Old §9 in B1; deletion runs through the privacy statement. | — |
| 13 | `wijzigingen` | Old §10 in B1 plus the change log (D16). | May we change consumer terms unilaterally, or does a material change need acceptance / a right to walk away? |

### Open points of 04 that are not about one section

- The reporting text names the button "Meld deze vacature" / "Meld dit bedrijf" before the button
  exists. **Resolved in public-pages 06**: the buttons exist on the vacancy detail and the company
  page, the text links to `/melden` and names the "Melding versturen" button.
- `Terms.Waiver.Checkbox` must stay identical in the terms and in the checkout. **Resolved in
  public-pages 05**: the checkout checkbox (`DeepAnalysis.razor`) now renders `Terms.Waiver.Checkbox`
  itself (the old, differently worded `DeepPay.Waiver` key was removed) and
  `DeepAnalysisPricing.WaiverTextVersion` now reads `LegalDocumentVersions.Terms.Version` instead of
  the literal `"2026-09"`, so a bump of the terms version automatically bumps the version stored on
  new checkouts and the receipt mail's small print.
- Both documents share one version and one date. A change to only one of them still bumps both.
- The pl / ro / ar "In het kort" blocks and the waiver sentence are dev-team drafts awaiting a
  native review (`docs/i18n/public-pages-review.md`). The Dutch text is the official version (D3).

## Bedenktijd checkout guard (public-pages 05) — version 2026-10

Dependency C (`docs/tests` 01) had already landed `WaiverAcceptedAtUtc` / `WaiverTextVersion` /
`waiver_required` on `main`/`acceptatie` before this file ran (05.2a "Present" path). This file only
aligns the wording and adds a guard test; it does not touch the Mollie/checkout internals or
`FlexCommercialSettings`.

| What | Where | What changed | Open question for the lawyer |
|---|---|---|---|
| Waiver checkbox sentence | `DeepAnalysis.razor` (checkout offer) | Reuses `Terms.Waiver.Checkbox` word for word instead of the separate, differently worded `DeepPay.Waiver` key. One source of truth across the terms and the checkout. | None beyond the 04.7 `bedenktijd` question (art. 6:230m/6:230v BW) — the sentence itself did not change here. |
| Waiver text version | `DeepAnalysisPricing.WaiverTextVersion` → `DeepAnalysisCheckout.WaiverTextVersion` | Now reads `LegalDocumentVersions.Terms.Version` ("2026-10") instead of the literal "2026-09", so the stored version always matches the terms version the consumer actually saw. Existing rows (version "2026-09" / "legacy") are left as they were paid under. | If the terms text changes again without changing the waiver sentence itself, should the stored `WaiverTextVersion` still bump (current behaviour: yes, since it is tied to the whole terms document, not only §`bedenktijd`)? |
| Order summary link | `DeepAnalysis.razor` | Added "Lees meer over bedenktijd" linking to `/gebruiksvoorwaarden#bedenktijd`, next to the checkbox. | — |
| Receipt mail small print | `Email.DeepTestReceipt` (`deep_test_receipt`) | Added a "Voorwaarden versie" / "Terms version" fact row with the checkout's stored `WaiverTextVersion`, next to amount/date/invoice number. | — |

## Meldknop / notice and action (public-pages 06) — DSA art. 16/17 + retention

The report form lives at `/melden` (static SSR, antiforgery, noindex) and is reachable from the
vacancy detail ("Meld deze vacature") and the company page ("Klopt er iets niet op deze pagina?
Meld het."). An admin decides on `/admin/vacatures/moderatie` → tab "Meldingen" with a required
statement of reasons for "Beperken" and "Verwijderen". Reporter data is deliberately minimal: an
optional e-mail address and never an IP address.

| What | Where | What it says / does | Open question for the lawyer |
|---|---|---|---|
| Reason list | `Report.Reason.*` (`UiStringsPublicInfo`) | Six B1 options: nep, discriminerend, mag niet volgens de wet, verkeerde informatie, onveilig werk, iets anders. | Is a six-option list specific enough for a DSA art. 16 notice, or must the notice form ask for an explanation and a good-faith statement as separate required fields? |
| Optional e-mail | `/melden` form, `ContentReport.ReporterEmail` | "Als je wilt dat we je laten weten wat we doen." Anonymous reports are accepted. | Art. 16(2) wants the notifier's name and e-mail "unless" the notice concerns certain offences. Is accepting fully anonymous notices acceptable, given we then cannot send a statement of reasons? |
| False reports | `Report.FalseWarning` | "Meld alleen iets als je denkt dat het echt niet klopt." We do not (yet) suspend repeat abusers (art. 23). | Do we need an explicit art. 23 misuse policy and a suspension mechanism before launch, or is the rate limit enough for now? |
| Statement of reasons | `ContentReport.DecisionReason`, mail `ContentRemoved` | The employer gets what was reported, the decision, the reason (HTML-escaped) and the date, plus "Wil je bezwaar maken? Mail {SupportEmail} binnen 6 maanden." The notifier gets the decision (`ReportDecided`) only when an e-mail was given. | Art. 17(3) lists mandatory elements (facts, automated means yes/no, legal or contractual ground, redress options incl. out-of-court dispute settlement and judicial redress). Our mail names only the internal objection route. Which elements must we add, and must we publish decisions in the DSA transparency database? |
| No notifier identity to the employer | `ContentReportService.NotifyOwnerAsync` | The employer mail never contains the reporter's e-mail address or user id. | Confirm this is the right balance against the employer's right to contest a notice. |
| Decision scope | `POST api/admin/reports/decide` | One decision closes **all** open reports of the same target with the same reason. "Beperken" exists for vacancies only (the vacancy goes inactive); "Verwijderen" archives the vacancy or sets `Company.PublicPageBlockedAtUtc` so `/{kvk}` answers 404 and the sitemap drops it. | Does a company-page block need its own appeal window or reinstatement rule, separate from a single vacancy? |
| No IP address | `ContentReport` (no IP column); rate limit `report` partitions by IP in memory only | 5 per hour and 20 per day per IP; the key is never persisted. | Would a retained IP be needed as evidence for an abuse policy (art. 23), and if so on what basis and for how long? |
| Retention | `PrivacyConstants.ContentReportRetentionDays = 365`, `ContentReportEmailRetentionDays = 30` | The reporter e-mail is cleared 30 days after the decision; the whole report is purged 365 days after the decision. Open reports are kept until decided. Both periods appear in the privacy retention table (`Legal.Retention.ContentReports`). | Is 365 days after the decision long enough to defend an appeal or an authority request, and is clearing the e-mail after 30 days compatible with a 6-month objection window (we then can no longer reach the notifier)? |
| Log redaction | `PlatformLog` `report.created` | Stores report id, type and reason; the e-mail is redacted (`EmailServiceStub.RedactEmail`). The admin audit row (`report.decided`) holds the decision and the report ids and no free text. | — |

## Go-live checklist (public-pages 10)

Everything below must be reviewed by a lawyer **before these texts go to `main` / lobsy.nl**.
Merging to `acceptatie` is fine. The sections above hold the detail; this is the short list the
stack-end report (`docs/reports/public-pages-stack-report.md`) points at.

| # | Document | Section | The question in one line |
|---|---|---|---|
| 1 | Privacy | §1 `wie` | Is "we have no data protection officer" correct for our scale and the data we process (test results of minors, talent pool)? |
| 2 | Privacy | §4 `delen` | Per US party: is the transfer basis right (DPF vs SCC)? Render says SCC with an EU region and a US parent; Pingen leans on the Swiss adequacy decision. |
| 3 | Privacy | §4 `delen` | Must the Sentry row name the actual DSN region (EU) instead of "EU or United States, depending on our setting"? |
| 4 | Privacy | §5 `bewaren` | Is keeping a hash of a deleted account's e-mail (to honour an unsubscribe) defensible, and is 2 years for the access log proportionate? |
| 5 | Privacy | §6 `cookies` | Is `lobsy_sales_ref` functional or does it need consent? |
| 6 | Privacy | §7 `ai` | Is the "no automated decision-making" wording strong enough for AVG art. 22, given the Top-10 ranking and the Cultuur Fit? |
| 7 | Privacy / gebruiksvoorwaarden | §9 `jonger` / §2 `voor-wie` | Is 13+ with parental consent under 16 acceptable under the Dutch implementation of AVG art. 8, and is mailing a parent enough verification? |
| 8 | Algemene voorwaarden | §11 `aansprakelijkheid` | Is a € 250 floor with a 12-month ceiling proportionate B2B, and must indirect / consequential damage be named explicitly (D6)? |
| 9 | Algemene voorwaarden | §5 `tokens` / §6 `betalen` | Is "excl. btw on the tariff page, incl. btw in the checkout" correct B2B, and is "no refund of unused tokens" enforceable against a small business? |
| 10 | Gebruiksvoorwaarden | §6 `bedenktijd` | Does the waiver sentence satisfy art. 6:230m/6:230v BW, and who gives the waiver for a minor — the parent? (D7) |
| 11 | Gebruiksvoorwaarden | §11 `aansprakelijkheid` | Are the remaining exclusions unfair terms now that a paid extra exists? |
| 12 | Both terms | §8 `melden` | Is Lobsy an online platform under the DSA at this scale, and are 5 working days, a 6-month objection window and one support mailbox enough (art. 11, 12, 16, 17, 20)? |
| 13 | DSA mails | `ContentRemoved` / `ReportDecided` | Art. 17(3) lists mandatory elements (facts, automated means, ground, out-of-court redress). Our mail names only the internal objection route. Which elements must we add, and must decisions go into the DSA transparency database? |
| 14 | DSA retention | `PrivacyConstants` | Is 365 days after the decision long enough, and does clearing the reporter e-mail after 30 days clash with the 6-month objection window? |
| 15 | DSA misuse | `Report.FalseWarning` | Do we need an explicit art. 23 misuse policy and a suspension mechanism before launch? |

### Not a legal question, but it blocks a complete review

- The `Legal:*` values are empty on Render, so the identity card and the footer legal line show only
  "Lobsy" and the support address. A lawyer reading the page today sees an incomplete identity
  block (D1, D14). Dennis fills them (see the stack-end report).
- `privacy@lobsy.nl` must be a real, monitored inbox before go-live, otherwise privacy questions
  keep going to support@ (D14).
- The pl / ro / ar "In het kort" blocks are dev-team drafts awaiting a native review
  (`docs/i18n/public-pages-review.md`). Only the Dutch text is official (D3).
