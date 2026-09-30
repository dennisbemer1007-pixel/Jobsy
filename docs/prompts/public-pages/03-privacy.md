# 03. Privacy statement rewritten: identity, full processor list (Pingen, OSM, web push), Render Frankfurt, retention from code, cookies, age 13/16/18 ⚖️

Read `00-README.md` first (§0 "Legal texts", D1, D3, D11, D14, Dependency G). Branch `cursor/public-pages-3` from `cursor/public-pages-2`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/public-pages-3` from `cursor/public-pages-2` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/public-pages-2)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/public-pages-3`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - ⚖️ Legal text: add every changed section to `docs/legal/review-needed.md` and start the PR body with "⚖️ Lawyer review needed".

| | |
|---|---|
| Branch | `cursor/public-pages-3` |
| PR title | `feat(privacy): privacy statement in B1 with In het kort in 5 languages, full processor list, retention from code, cookies section, consistent age rules` |
| Mockups | `pb-d03-privacy`, `pb-m03-privacy`, `pb-m08-privacy-arabisch-rtl` |
| Migration | none |

## Goal
A privacy statement that is complete (AVG art. 13/14), correct against the code, readable at B1, and summarised in the reader's language.

## 03.1 Today (verify first)
- `Pages/Legal/Privacy.razor` sections (h2 at L22 … L234):
  - 1 Verwerkingsverantwoordelijke · 2 Welke gegevens · 3 Doeleinden en grondslagen · 4 Verwerkers en derden · 5 Locatie, reistijd en matching · 5a Jeugdige arbeid · 5b Tests en talentpool · 6 Bewaartermijnen · 7 AI-functies · 8 Beveiliging · 9 Jouw rechten · 10 Minderjarigen · 11 Wijzigingen
- **Processors today (L64–81):** Cloudflare, Render **"(EU/VS)"**, Resend, Sentry, Cursor, OpenAI, Google/Microsoft login, Mollie, KVK, OSRM/Transitous, OpenFreeMap, YouTube/Vimeo, WhatsApp/telefoon.
  - **Missing:**
    - **Pingen** (letter verification, `docs/werkgever-aanmelding` 06)
    - **valhalla1.openstreetmap.de** (`ValhallaIsochroneService.cs` L38, travel-time zones from coordinates)
    - **tile.openstreetmap.org** (`OsmTileMapImageService.cs` L112, static map images for PDFs)
    - **web-push services** (`WebPushSubscriptions`, VAPID: the browser's push service: Google FCM, Mozilla, Apple, Microsoft)
  - `render.yaml` L13/29/39/130/199: every Render resource runs in **frankfurt**.
- **Retention today (L160–168)** lists 7 items. Missing: `PersonalDataAccessLogRetentionDays` 730, `UserNotificationRetentionDays` 365, `FeedbackScreenshotRetentionDays` 90, `CandidateActionTokenRetentionDays` 30, `UnconfirmedRegistrationRetentionMinutes` 10.
- L44 says "API-credentials kunnen éénmalig per e-mail worden verstuurd". That's outdated once `docs/emails` 01 (reveal-once link) lands, and wrong in spirit today.
- L227 "vanaf 13 jaar". The ages are constants in `CandidateConsentRules` L8–10 (`MinimumCandidateAge` 13, `ParentalConsentAge` 16, `TalentPoolMinimumAge` 18). The texts don't always read the constants.
- Cookies are one long list item inside §2 (L43); there's no `#cookies` section.

## 03.2 New outline (section ids are stable URLs)
| # | id | Title (nl) | Content |
|---|---|---|---|
| 1 | `wie` | Wie is verantwoordelijk? | `LegalIdentityCard` (01/02) + "Vragen? Mail {PrivacyContact}." No DPO (say so: "We hebben geen functionaris gegevensbescherming; dat is voor ons niet verplicht.") ⚖️ |
| 2 | `gegevens` | Welke gegevens gebruiken we? | today's §2 per group (account, kandidaat, werkgever, salesmanager, communicatie), B1 bullets; technical data moves to §6 |
| 3 | `waarom` | Waarom mogen we dat? | today's §3 grounds, one line per ground with an example |
| 4 | `delen` | Met wie delen we gegevens? | employers (after acceptance only), then `ProcessorTable` (03.3), then "Buiten de EU" (03.4) |
| 5 | `bewaren` | Hoe lang bewaren we gegevens? | `RetentionTable` (02.7) + the non-constant rules (self-billing fiscal 7 years, withdrawn applications) |
| 6 | `cookies` | Cookies en opslag op je apparaat | table: name · why · how long · needed? (`Jobsy.Auth`, `Jobsy.LastActivity`, `Jobsy.Culture`, `Jobsy.CookieConsent`, `jobsy.gratisDna.v1` 7 days, engagement key after consent). One line: "Statistieken alleen na 'Accepteer cookies'." |
| 7 | `ai` | AI, matching en reistijd | today's §5 + §7 merged: what the AI does, OpenAI, no automated decisions with legal effect, reistijd via OSRM/Transitous/Valhalla, jeugdige arbeid checks (today's 5a) |
| 8 | `tests` | Tests en talentpool | today's 5b in B1; talentpool from `TalentPoolMinimumAge` |
| 9 | `jonger` | Jonger dan 16? | 03.5 |
| 10 | `rechten` | Jouw rechten | inzage, correctie, verwijderen, bezwaar, overdraagbaarheid; button "Mijn gegevens" → `/privacy/data`; complaint at the Autoriteit Persoonsgegevens with link |
| 11 | `beveiliging` | Hoe beveiligen we je gegevens? | today's §8 in B1 |
| 12 | `wijzigingen` | Wijzigingen | how we announce changes + `LegalDocument` change log |

Each section gets a `Privacy.Sec.{id}.Summary` "In het kort" (02.5 keys; update them where the outline changed).

## 03.3 Processor list (`LegalProcessors` rows)
| Id | Name | Region | Purpose | Data | Status |
|---|---|---|---|---|---|
| `render` | Render | EU (Frankfurt); Render is a US company | hosting app + database | all account/platform data | Active |
| `cloudflare` | Cloudflare | EU/VS | security, DDoS, fast delivery | IP address, request data | Active |
| `resend` | Resend | VS | sending e-mail | e-mail address, name, mail content | Active |
| `sentry` | Sentry | EU/VS (check the DSN host: `*.de.sentry.io` = EU) | error reports | technical data, no form content | Active |
| `mollie` | Mollie | Nederland | payments | amount, status, method (no card data at Lobsy) | Active |
| `pingen` | Pingen | Zwitserland | sending verification letters by post | company name, address, code in the letter | Planned/Active (Dependency G) |
| `openai` | OpenAI | VS | AI features you choose | text you enter, cv text for cv reading | Active |
| `cursor` | Cursor | VS | handling feedback | feedback text, optional screenshot | Active |
| `google-ms` | Google, Microsoft | VS/EU | signing in | verified e-mail, login id | Active |
| `kvk` | KvK | Nederland | checking companies | KvK and establishment number | Active |
| `routing` | OSRM, Transitous, Valhalla (FOSSGIS / openstreetmap.de) | EU | travel time and travel zones | coordinates, transport mode | Active |
| `maps` | OpenFreeMap, OpenStreetMap tiles | EU | showing maps (also map images in PDFs) | IP address (browser), map area | Active |
| `push` | Push services (Google, Apple, Mozilla, Microsoft) | VS/EU | notifications on your phone, only if you turn them on | a device code, the text of the notification | Active |
| `video` | YouTube, Vimeo | VS/EU | videos, only after your click | IP address, device data | Active |

- The Region column says "Zwitserland" for Pingen and the transfer basis "adequaatheidsbesluit" in §4 "Buiten de EU".
- Check each "VS" row against the code before writing the final text. When a row turns out unused (for example Cursor feedback is off), keep it out and say so in the PR.

## 03.4 Transfers outside the EU
"Sommige partijen zitten (ook) in de VS. Dan gebruiken we het EU-VS Data Privacy Framework of de standaardcontractbepalingen van de EU. Pingen zit in Zwitserland; de EU vindt de bescherming daar gelijkwaardig." ⚖️ (the lawyer confirms per party which basis applies).

## 03.5 Age (D11)
- One component `Components/Legal/AgeRulesText.razor` renders, from the constants: "Je mag Lobsy gebruiken vanaf 13 jaar. Ben je jonger dan {ParentalConsentAge}? Dan vragen we eerst toestemming aan je ouder of voogd voor de tests en de AI-analyse. De talentpool is vanaf {TalentPoolMinimumAge} jaar."
- 13 is `CandidateConsentRules.MinimumCandidateAge` (L8; enforced in `MeController` ~L186). Use the constant. `MeController` ~L283 (`Preferences.AgeYears` 15–67) is for wage tables, not eligibility: don't change or mention it.
- The same component is used in 04 (gebruiksvoorwaarden §2).

## 03.6 Other fixes
- Replace the API-credentials line (L44) with: "Een API-sleutel mailen we nooit. Je krijgt een link waarmee je de sleutel één keer ziet." (true after `docs/emails` 01). If `git grep -n "ApiKeyReveal" origin/acceptatie` is empty, write instead: "Een API-sleutel sturen we alleen naar de technische contactpersoon die de bedrijfsmanager kiest." and add a follow-up line to update it after emails 01.
- `#cookies` is the id of §6. Old anchors (`#verwerkers`, if any) keep working via an empty `<span id>`.
- The whole text is B1 with "je". No "wij verwerken", "ná", "i.p.v.".
- `docs/legal/review-needed.md` (new): one row per section with what changed and open questions (DPO line, transfer bases, Sentry region, Cursor still used?).

## 03.7 Tests
- `PrivacyProcessorsTests`: every `LegalProcessors` row renders; Pingen shows "(vanaf de start van brief-verificatie)" when `Status = Planned`; a source check: when `git grep` finds `Pingen` in `Jobsy.Infrastructure`, the row is `Active` (a test that reads the assembly for a type name containing "Pingen" is enough).
- `PrivacyRetentionTests`: the 12 retention constants appear with their formatted duration; changing a constant changes the page (render with a patched value via the catalog function).
- `PrivacyAgeTextTests`: the text contains `MinimumCandidateAge`, `ParentalConsentAge` and `TalentPoolMinimumAge` (13, 16, 18).
- `PrivacyAnchorsTests`: ids `wie`, `delen`, `bewaren`, `cookies`, `rechten` exist; `/privacy#cookies` is linked from the footer.
- `PrivacyNoPlaceholderTests`: rendered in 5 cultures, no `[` + uppercase placeholder, no "EU/VS" for Render.
- bUnit/Playwright screenshots nl desktop + mobile, ar mobile for the PR.

## Success criteria
- The processor table lists Pingen, Valhalla/OSM tiles and push services. Render says EU (Frankfurt).
- The retention table is generated from `PrivacyConstants` (no duplicated numbers in markup).
- The age sentence is the shared component, used here and in 04.
- `docs/legal/review-needed.md` lists every privacy section; the PR body starts with "⚖️ Lawyer review needed".

Done → next: `04-voorwaarden.md`.
