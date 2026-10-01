# ⚖️ Lawyer review needed

The legal texts of the public-pages stack are **drafts by the dev team, not legal advice**.
They may ship to `acceptatie`; a lawyer reviews them before they go to `main` / lobsy.nl.

Every PR that changes a legal text adds its sections here: document · section id · version ·
what changed · open question.

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
  exists. Public-pages 06 builds the form at `/melden`; until then the text offers only the support
  e-mail. If 06 slips, the sentence promises a route that is not there yet.
- `Terms.Waiver.Checkbox` must stay identical in the terms and in the checkout. Public-pages 05
  aligns `DeepPay.Waiver` and `DeepAnalysisPricing.WaiverTextVersion` with
  `LegalDocumentVersions.Terms`; today the checkout still carries the older sentence and version
  `2026-09`.
- Both documents share one version and one date. A change to only one of them still bumps both.
- The pl / ro / ar "In het kort" blocks and the waiver sentence are dev-team drafts awaiting a
  native review (`docs/i18n/public-pages-review.md`). The Dutch text is the official version (D3).
