# Cleanup prompts: order and dependencies

Source: `docs/review/code-review.md` (review of `acceptatie` @ `ccf6976`, 28-09-2026). Every prompt starts with:
"Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123."
Run them **one at a time**, merge, then start the next one. Performance work is in `/workspace/jobsy-design/review/performance-audit.md` (PERF-1..10) and is not repeated here.

| # | Prompt | Priority | Depends on / wait for |
|---|---|---|---|
| 01 | `01-guardrails-editorconfig-analyzers.md` | medium (base) | none |
| 02 | `02-packages-central-management.md` | medium (security bump) | 01 preferred; **wait for SalesWalletChip/API-client PR** |
| 03 | `03-authz-regiomanager-readonly.md` | **HIGH** | none (can go first) |
| 04 | `04-authz-role-scope-matrix-tests.md` | **HIGH** | **03** |
| 05 | `05-admin-gdpr-audit-log-masking.md` | **HIGH** | 03/04 preferred; migration last after rebase |
| 06 | `06-admin-gdpr-support-access.md` | **HIGH** | **05** |
| 07 | `07-small-bugfixes.md` | medium | items 6a/6b wait for VacancyDetail-hero and bottom-nav-feedback PRs |
| 08 | `08-dead-code-csharp.md` | low | 01; ApiClient part waits for SalesWalletChip PR; VacancyDiscovery part waits for VacancyCard PR |
| 09 | `09-dead-assets-and-js-sources.md` | low | **wait for** jobMap pins-once, popup-height/?v, asset-versions guard, bottom-nav feedback, cookie-banner PRs |
| 10 | `10-dead-css.md` | low | **wait for all CSS PRs**; 01; preferably 09 |
| 11 | `11-dedup-openai-settings.md` | low | none |
| 12 | `12-dedup-razor.md` | low | **wait for** TestDetail/MatchPage and SalesWalletChip PRs; 08 |
| 13 | `13-structure-apiclient-split-and-component-folders.md` | low | **wait for all razor PRs + SalesWalletChip PR**; 08, 12 |
| 14 | `14-localization-guard.md` | low | 01 |
| 15 | `15-docs-onboarding-adr.md` | medium | 01, 03/04 preferred |

**Recommended order:** 03 → 04 → 01 → 05 → 06 → 07 → 11 → 14 → 02 → 08 → 15 → 09 → 10 → 12 → 13.
(The numbering is from safest to most invasive. Because of Dennis's priority, the authorization/GDPR prompts 03–06 go first. 01 is harmless and can run in parallel.)

## Uitleg per prompt (Nederlands)

- **01 Vangrails:** `.editorconfig`, gedeelde build-instellingen en een extra CI-check (warnings mogen niet stijgen, geen secrets, geen kwetsbare packages). Er verandert geen code.
- **02 Packages:** alle NuGet-versies op één plek, AngleSharp-lek dichten en twee overbodige packages eruit.
- **03 Regiomanager alleen-lezen (HOOG):** een regiomanager kan nu nog talent ontgrendelen (tokens + contactgegevens), cultuur opslaan en onboarding afrekenen. Dat wordt dichtgezet in de API, de knoppen worden verborgen, en er komen tests bij.
- **04 Rollenmatrix + tests (HOOG):** een automatische test die faalt als een nieuw endpoint de regiomanager laat wijzigen, plus tests dat niemand bij data van een ander bedrijf, andere vestiging of andere regio kan. Ook een rollen-document.
- **05 Admin & AVG (HOOG):** admin ziet standaard gemaskeerde en samengevatte gegevens, lijsten krijgen paginering, elke inzage in persoonsgegevens wordt gelogd (wie, wat, van wie, wanneer) en IBAN's worden versleuteld opgeslagen.
- **06 Tijdelijke support-toegang (HOOG):** een admin vraagt met reden toegang tot één persoon aan, voor maximaal een paar uur. Alles wordt gelogd en is zichtbaar voor andere admins.
- **07 Kleine bugfixes:** `eval` weg (veiliger CSP), N+1-query in de talentpool, ontbrekende annulering, crashes in `async void` voorkomen.
- **08 Dode C#-code:** ongebruikte methodes, velden en 4 ongebruikte componenten weg.
- **09 Dode bestanden:** ongebruikte afbeeldingen (o.a. mascot.png van 567 KB) weg, en duidelijk maken welk JS-bestand de bron is (tests lezen nu soms een oude kopie).
- **10 Dode CSS:** ongeveer 1.600 regels ongebruikte CSS weg, en een echte minifier in plaats van `app.min.css` met de hand bijwerken. Wacht tot alle CSS-PR's gemerged zijn.
- **11 OpenAI-dubbeling:** 11 gekopieerde stukjes "welke key/model" samenvoegen tot één.
- **12 Razor-dubbeling:** vragenlijst-pagina's, payout-stubs en de taalwissel-code één keer schrijven in plaats van vaak.
- **13 Structuur:** het bestand van 5.000 regels (JobsyApiClient) opsplitsen en panelen uit de map Pages verplaatsen. Alleen verplaatsen, niets aan de logica veranderen.
- **14 Vertalingen:** test die laat zien hoeveel teksten niet vertaald zijn, en de zichtbare "Jobsy"-teksten worden "Lobsy".
- **15 Documentatie:** nieuwe README, architectuur, beslissingen (ADR's), onboarding, rollen en routes.
