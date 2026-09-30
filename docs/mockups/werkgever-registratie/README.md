# Werkgever-registratie (wr-*) · fase 1 mockups

Build: `python3 build.py [filter]` (Playwright). Desktop 1440 at dpr1, mobile 390 at dpr2. All 19 screens pass the overflow, clip and small-font checks.
Style: the warm public `pub-` theme of the landing mockups. `base/` is a verbatim copy of the needed landing mockup sources (`ui.py`, `css_*.py`, `w_common.py`, `src/`) from branch `docs/landing` (`docs/mockups/landing/`), so this folder builds on its own. All example data is fictional and labelled "Voorbeelddata".
Lobster = `LobsyMascot` (Waving pose on steps 1–3, Default after that; fallback `mascot-256`).

| Screen | What |
|---|---|
| wr-d1-start-zoeken | one smart field (8 digits = KvK number, otherwise a name search from 3 letters) plus an optional place |
| wr-d2-zoekresultaten | KVK Zoeken results; "Al op Lobsy" label |
| wr-d3-bedrijf-vestigingen | company card, choice between heel bedrijf and één vestiging, vestigingen list (a vestiging that already has an owner is excluded, with "Vraag toegang aan") |
| wr-d4-gegevens-account | details, business e-mail domain match, Microsoft/Google/e-mail, sales code from the cookie, terms and a representation checkbox |
| wr-d5-branche | multi-select from the existing WorkType list (9), prefilled from KvK SBI codes |
| wr-d6-kernwaarden | "Zo werken wij": 6 sliders (the 6 CulturePersonalityCatalog culture dimensions) plus 3 of 10 value cards (the 5 Schwartz drivers) plus a live profile |
| wr-d7-betrokkenheid | 6 social-engagement items with optional proof; labels "gecontroleerd bij SBB" and "door werkgever opgegeven" |
| wr-d8-verificatie-keuze | the employer chooses: business e-mail (domain = KvK website) or a letter to the KvK address; manual check as fallback |
| wr-d9-brief-onderweg | letter on its way: timeline, 8-character code valid 30 days, resend limit, switch to e-mail |
| wr-d10-al-geregistreerd | already has an owner, so the registrant sends an access request (reminder on day 3, support on day 5); ownership takeover only by letter plus support |
| wr-d11-welkom-dashboard | "Nog niet zichtbaar voor kandidaten" banner, checklist, vacancy "Klaar · gaat live na verificatie", visibility panel |
| wr-d12-vacature-cultuur | a vacancy inherits the company culture profile and badges; "Dit team werkt anders" = the existing 3–5 CulturePillars as an override; candidate preview |
| wr-m1…m7 | search, vestiging choice, branche, kernwaarden, betrokkenheid, verification choice (letter), letter code |

Proposed and not yet in the design system: the `.wz` wizard layout, `.mcard` method cards, `.bdg` engagement badges, and `.env` letter illustration (landing radii 24/36).
