# 04. Algemene voorwaarden + gebruiksvoorwaarden: audience switch, Wie is Lobsy, excl. btw, liability cap, minors, Iets melden (DSA), Betaalde extra's en bedenktijd ⚖️

Read `00-README.md` first (§0 "Legal texts", D1, D5–D8, D11, Dependency F). Branch `cursor/public-pages-4` from `cursor/public-pages-3`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/public-pages-4` from `cursor/public-pages-3` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/public-pages-3)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/public-pages-4`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - ⚖️ Legal text: add every changed section to `docs/legal/review-needed.md` and start the PR body with "⚖️ Lawyer review needed".

| | |
|---|---|
| Branch | `cursor/public-pages-4` |
| PR title | `feat(terms): algemene voorwaarden and gebruiksvoorwaarden in B1 with In het kort, audience switch, identity, excl. btw, liability cap, minors, DSA reporting, paid extras and bedenktijd` |
| Mockups | `pb-d05-algemene-voorwaarden`, `pb-m05-gebruiksvoorwaarden` |
| Migration | none |

## Goal
Terms that say who Lobsy is, what things cost and how, what happens when something is reported, and what a consumer gives up when a paid analysis starts right away.

## 04.1 Today (verify first)
- **`AlgemeneVoorwaarden.razor`** (employers, 172 lines). Sections at L21–165:
  - 1 Toepasselijkheid · 2 Dienstverlening · 3 Account en KVK · 4 Tokenbeleid · 5 Betalingen · 6 Vacatures en content · 6a Matching en jeugdige arbeid · 7 Sollicitaties · 8 Beschikbaarheid · 9 Aansprakelijkheid · 10 Misbruik en beëindiging · 11 Toepasselijk recht
  - Bugs:
    - "bulkapakket" (L80) and "early-adapterkortingen" (L87)
    - "exclusief of inclusief btw zoals in de checkout vermeld" (L98)
    - liability "… of € 250 als dat lager is" (L147–151): with € 0 paid the cap is € 0
    - no identity, no reporting/DSA, date by hand (L11)
- **`Gebruiksvoorwaarden.razor`** (candidates, 144 lines). Sections at L21–137:
  - 1 Toepasselijkheid · 2 Wat Lobsy wel en niet is · 3 Account · 4 Solliciteren · 4a Matchingspercentages · 5 Chatbot en AI · 6 Acceptabel gebruik · 7 Beschikbaarheid · 8 Aansprakelijkheid · 9 Beëindiging · 10 Wijzigingen en recht
  - Missing:
    - identity and minimum age / minors
    - reporting
    - **paid extras**: the uitgebreide test € 2,99 (`FlexCommercialSettings.DeepAnalysisPriceEuro`, per test type after `docs/tests` 01) has no consumer information and no bedenktijd text (`git grep -n -i "herroep\|bedenktijd"` is empty)

## 04.2 Shape
- Both routes render **one** component `Components/Legal/TermsPage.razor` with `Audience = Employer|Candidate`:
  - `/algemene-voorwaarden` → Employer
  - `/gebruiksvoorwaarden` → Candidate
- The **switch** at the top (pb-d05): two pill links "🏢 Voor werkgevers" · "🙋 Voor kandidaten", `aria-current="page"` on the active one. Both are real links to the two URLs (no JS).
- Werkgevers actief OFF (Dependency F present): the switch still shows both. Nav/footer links to the employer terms follow landing's OFF rules.
- `LegalDocumentVersions.Terms` covers both pages (one version, one date).

## 04.3 Outline: algemene voorwaarden (employers)
| # | id | Title (nl) | Content / change |
|---|---|---|---|
| 1 | `wie` | Wie is Lobsy? | **new**: `LegalIdentityCard` incl. btw-nummer + support e-mail |
| 2 | `toepassing` | Wanneer gelden deze voorwaarden? | today's 1 |
| 3 | `dienst` | Wat Lobsy doet | today's 2 |
| 4 | `account` | Account en KvK-controle | today's 3 + "We controleren je KvK-nummer. Zolang dat niet gelukt is, is je bedrijfspagina niet openbaar." (matches 01) |
| 5 | `tokens` | Tokens en prijzen | today's 4 + 5 merged; "Prijzen op de tarievenpagina staan **exclusief btw**. Bij het afrekenen zie je ook het bedrag inclusief btw." (D5); typos fixed ("bulkpakket", "early-adopterkorting"); prices come from the tarievenpagina/checkout, not from this text |
| 6 | `betalen` | Betalen | Mollie, invoice, when tokens are added; refunds of unused tokens as today's text says (don't invent new rules) |
| 7 | `vacatures` | Vacatures en inhoud | today's 6 + 6a |
| 8 | `melden` | Iets melden en wat wij dan doen | **new** (04.5) |
| 9 | `sollicitaties` | Sollicitaties en gegevens van kandidaten | today's 7 |
| 10 | `beschikbaar` | Beschikbaarheid en wijzigingen | today's 8 |
| 11 | `aansprakelijkheid` | Aansprakelijkheid | **D6** (04.6) |
| 12 | `einde` | Misbruik en stoppen | today's 10 |
| 13 | `recht` | Welk recht geldt? | today's 11 |

## 04.4 Outline: gebruiksvoorwaarden (candidates)
| # | id | Title (nl) | Content / change |
|---|---|---|---|
| 1 | `wie` | Wie is Lobsy? | **new**: identity card |
| 2 | `voor-wie` | Voor wie gelden ze? | **new**: `AgeRulesText` (03.5, D11) |
| 3 | `wat` | Wat Lobsy wel en niet is | today's 2 |
| 4 | `account` | Jouw account | today's 3 |
| 5 | `solliciteren` | Solliciteren | today's 4 + 4a (match % is an estimate) |
| 6 | `bedenktijd` | Betaalde extra's en bedenktijd | **new** (04.7) |
| 7 | `ai` | AI en de chatbot | today's 5 |
| 8 | `melden` | Iets melden | **new** (04.5, candidate wording) |
| 9 | `gebruik` | Wat mag niet | today's 6 |
| 10 | `beschikbaar` | Beschikbaarheid | today's 7 |
| 11 | `aansprakelijkheid` | Aansprakelijkheid | today's 8, consumer-safe wording (no cap that limits statutory consumer rights) ⚖️ |
| 12 | `einde` | Stoppen | today's 9 |
| 13 | `wijzigingen` | Wijzigingen en recht | today's 10 |

## 04.5 "Iets melden" (DSA notice and action + statement of reasons) ⚖️
- **Draft nl:**
  - "Zie je een vacature of bedrijf dat niet klopt of niet mag (bijvoorbeeld nep, discriminerend of illegaal)? Klik op 'Meld deze vacature' of 'Meld dit bedrijf', of mail {SupportEmail}."
  - "We bevestigen je melding als je je e-mailadres geeft. We kijken zo snel mogelijk, meestal binnen 5 werkdagen."
  - "Halen we iets weg of beperken we het, dan krijgt de werkgever een uitleg: wat, waarom, en hoe bezwaar maken. Jij krijgt ook bericht als je je e-mailadres gaf."
  - "Bezwaar? Mail {SupportEmail} binnen 6 maanden."
  - "Contactpunt voor autoriteiten en gebruikers: {SupportEmail} (Nederlands of Engels)."
- The button and flow are built in 06; this text links to `/melden` only after 06 (until then, just the e-mail).

## 04.6 Liability (D6) ⚖️
- **Employers (draft nl):** "Is Lobsy aansprakelijk? Dan betalen we nooit meer dan wat je in de 12 maanden vóór de gebeurtenis aan Lobsy betaalde, met een minimum van € 250. Dit geldt niet bij opzet of grove schuld van Lobsy, en niet voor aansprakelijkheid die de wet niet laat beperken."
- **Candidates:** "Lobsy is gratis voor werkzoekenden. We doen ons best, maar we beloven geen baan of match. Je wettelijke rechten als consument blijven altijd gelden." No cap below statutory consumer rights.

## 04.7 "Betaalde extra's en bedenktijd" (D7) ⚖️
- **Draft nl:**
  - "Lobsy is gratis. Sommige extra's kosten geld, zoals de uitgebreide test met analyse. Je ziet de prijs (inclusief btw) en wat je krijgt vóór je betaalt."
  - "Normaal heb je bij online kopen 14 dagen bedenktijd. Bij de uitgebreide test start de analyse meteen. Daarom vragen we je vooraf met een vinkje: 'Ik wil dat de analyse meteen start. Ik weet dat ik dan geen 14 dagen bedenktijd heb.' Zonder dat vinkje kun je niet betalen."
  - "Je krijgt een bevestiging per e-mail met de prijs en deze keuze."
  - "Ben je jonger dan {ParentalConsentAge}? Dan heb je eerst toestemming van je ouder of voogd nodig voor de tests."
- The vinkje sentence is **one** string key `Terms.Waiver.Checkbox` that 05 (and `docs/tests` 01, if present) must reuse word for word. Add it to `UiStringsLegal` in 5 languages.

## 04.8 Other
- B1 throughout ("je", short sentences). Keep the legal meaning of today's text. Where you simplify a clause, keep the original meaning, and note the clause in `docs/legal/review-needed.md`.
- The acceptance texts elsewhere ("Door verder te gaan ga je akkoord met de voorwaarden…") keep linking to the same URLs. Anchor links used elsewhere (`git grep -n "algemene-voorwaarden#\|gebruiksvoorwaarden#"`) keep working (add empty `<span id>` aliases for old anchors).

## 04.9 Tests
- `TermsPageTests` (bUnit): both routes render the right audience and switch state; the section ids exist (`wie`, `melden`, `aansprakelijkheid`, `bedenktijd` for candidates, `tokens` for employers).
- `TermsCopyTests`: no "bulkapakket", "early-adapter", "exclusief of inclusief btw"; the employer text contains "exclusief btw", "inclusief btw" (checkout sentence) and "minimum van € 250"; the candidate text contains the exact `Terms.Waiver.Checkbox` value.
- `TermsIdentityTests`: identity card rows follow `LegalIdentityProvider` (empty → hidden).
- `AgeRulesText` is used in both privacy and terms (render both, compare the sentence).
- Screenshots nl desktop/mobile + ar mobile.

## Success criteria
- Both pages show who Lobsy is, the reporting route, and (employers) excl. btw + the € 250 minimum cap; (candidates) the bedenktijd section with the shared checkbox sentence.
- `docs/legal/review-needed.md` updated; PR body starts with "⚖️ Lawyer review needed".

Done → next: `05-bedenktijd.md`.
