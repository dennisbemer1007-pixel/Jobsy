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
