# Changelog: Jobsy

## Vacaturecategorieën (flexibel)

- Admin-beheerbare **VacancyCategory** (naam, kleur, tokenprijs, highlight/PushBom, extra velden)
- Standaardcategorieën: Uitzendbureau, Regulier, Highlight, Inclusief, Vrijwilligerswerk, Stageplekken, 65+
- Dynamische create-dropdown + extra velden; kaartfilter, legenda en pin-kleuren; tokenlogica per categorie
- **Geschikt voor 65+**: checkbox bij reguliere vacatures, label (donkerpaars), kaartfilter (65+-categorie + gevlagde regulier), popup/lijst/detail — géén legenda-item



Alle noemenswaardige wijzigingen aan dit project worden in dit bestand bijgehouden.

## [Unreleased]

### Added
- Kandidaat kan een eigen CV (PDF/DOCX) uploaden; OpenAI vult alleen lege profielvelden als ze duidelijk in het CV staan. Lobsy-CV toont bovenaan dat er een eigen CV is. Recensies (werkgever, contactpersoon, e-mail, telefoon) in het profiel; vacature kan een hard minimum aantal recensies eisen. Na Accept ziet de werkgever Lobsy-CV én het geüploade CV.

### Changed
- Homepage-kaart: echte vacaturemarkers weer zichtbaar. Leaflet start op catalogus-coördinaten (verplicht center/zoom + tegels); geen nep-pins, geen NL-overzicht, geen lege placeholder die de kaart bedekt.
- Role dashboards (`/home`): geen foutflits meer na login (admin en andere rollen). GET-calls retrien kort terwijl auth settelt; 401 alleen zonder credentials; fouttekst alleen als er echt geen data is.
- Homepage-kaart: first paint toont al pins op de NL-preview; Leaflet warmt meteen na paint (geen lege kaart meer tot de circuit klaar is).
- Transactionele mails: Lobsy-logo als klein PNG + inline CID (niet meer de 568&nbsp;KB remote `lobsy.png` die in clients als gebroken plaatje verscheen).
- Homepage weer snel: Leaflet pas na first paint, picsum-foto’s pas ná de live kaart (lazy, 400×267 op kaarten). Originele unieke foto’s en markers blijven. Zie `docs/performance.md`.
- Vacaturefoto’s: picsum-seeds terug (unieke foto per vacature); SVG-stand-ins worden teruggezet. Kaart-init weer in de werkende volgorde (tegels → clusters). Zie `docs/performance.md`.
- Homepage-kaart: markers weer zichtbaar (cluster niet droppen bij 0×0 bounds; preview-overlay weg na init). Zie `docs/performance.md`.
- Homepage-kaart: prerender van echte Carto-NL-tegels (lokaal webp); Leaflet start op heel NL, niet eerst Den Haag/Null Island. Zie `docs/performance.md`.
- Banenkaart-performance: lokale SVG-placeholders i.p.v. picsum, lazy `<img>` op job cards, WebP-logo, Leaflet pas laden op kaartpagina’s, gebundelde `app-core.js`, Brotli + cache-headers. Zie `docs/performance.md`.
- PageSpeed-vervolg: geen 278 job cards meer in de eerste HTML/mobile-kaart (venster van 12), compacte cookiebanner in first paint, map-loader split discovery/detail.
- Quality gate 456: www-canonical alleen voor bekende hosts (geen Host-header open redirect); Cloudflare image-resize alleen same-origin; `app-core.js` synchroon vóór Blazor; map-loader herstelt na een mislukte load.
- Banenkaart opent uit een warme in-memory vacature-index (refresh elke 15s + direct na publiceren/wijzigen) in plaats van een zware DB-query + OpenAI-vertaling per page-open. Kaart en lijst tonen meteen; locatiefilter volgt daarna.
- Quality gate 456 (feedback-pipeline): pagina-URL zonder query/fragment; RTBF wist ook beschrijving/prompt/rol; screenshots max. 90 dagen ongeacht status; geen dubbele Cursor-launch; opgeslagen prompt blijft behouden bij heropenen.

### Added
- End-to-end feedback-pipeline: globale Feedback-knop (screenshot + metadata), `POST /api/feedback`, admin-datagrid `/admin/feedback`, functionele prompt en Cursor Cloud Agent-koppeling die een PR opent; PR-URL via webhook/poll terug in het grid.
- Prepaid token checkout (“no tokens, no action”): bij onvoldoende saldo blokkeert publish/highlight/PushBom/extend met in-context Mollie-top-up (exact match + bulkapakketten); na webhook/return worden tokens bijgeschreven en de pending actie automatisch uitgevoerd (`PendingTokenAction`).
- Salesmanager multi-level referral (één laag): Admin maakt tier-0 aan; tier-0 dient aanbevelingen in; Admin keurt goed vóór provisioning; tier-1 kan niet verder werven.
- Configureerbare commissies (defaults 15% direct / 3% indirect, max. 1 jaar per ondernemer) op `/admin/sales`; ledger + `RevenueShareLogs` voor indirecte bonus.
- Bedrijfsregistratie: gekozen wachtwoord bij submit, e-mailverificatie, daarna dual login (e-mail/wachtwoord of Microsoft Entra met hetzelfde adres).
- Automatische KVK/SBI-roltoekenning: SBI `78*` → Intermediair; overig Organization-scope → Bedrijfsmanager (EnterpriseManager).
- Quality gate 456: takeover e-mailverificatie vóór inbox/approve; pending `PasswordHash` gewist bij cancel/reject/expiry/anonymize; intermediair-takeover detacht van employer-org.
- Functionele specificatie matching / dagdelen / uren / Arbeidstijdenwet voor kandidaat- en werkgeverskant (`docs/FUNCTIONELE_SPECIFICATIES_MATCHING.md`), inclusief verwijzing vanuit `REQUIREMENTS.md`.
- Admin Sales beheer (`/admin/sales`): basis tokenwaarde (€25), tokenkosten per vacaturetype (Regulier / Stageplek / Vrijwilligerswerk), standaard + First Year / Enterprise pakketten (Silver/Gold/Platinum), highlight-toeslagen.
- Vacaturetype `VacancyKind` op create/publish; publiceerkosten volgen type (incl. nul-tarief voor vrijwilligerswerk).
- Publieke Partner Sales-pagina (`/partner`, `/partner/{code}`) met live tarieven/pakketten, PDF-flyer (QuestPDF) en WhatsApp/mail-deelknoppen.
- Salesmanager toolkit (`/salesmanager/toolkit`) met persoonlijke trackinglink/flyer; registratie via `?ref=` activeert gratis start-highlight op eerste vacature.
- Cursor shortcut `456` (quality gate): full regression + AVG audit + code review.
- Quality-gate fixes: start-highlight only on vestiging + atomic consume (incl. approve path); anonymous catalog read-only; flyer rate-limit + `SM-XXXXXX` validation; type pricing ignores catalog `IsActive`; i18n key parity (nl/en/pl/ro/ar).

## [0.8.0] - 2026-07-25 (Sprint 8: Polish, seed, docs, cleanup)
### Toegevoegd
- Rijke idempotente `Sprint8MetricsSeeder`: spends/pushboms/extensions, time-spread engagement, statusmix-vacatures (Draft/Pending/Archived + intermediair), platform logs, allocations.
- Gedeelde UI: `MetricTile`, `DrilldownGrid`; `ShareVacancyModal` → `ShareModal`.
- Candidate BottomNav: Home → `/home`; Admin-nav gestript (modules blijven op `/home`); Intermediary `/home` gebruikt EmployerHomePanel.

### Opgeruimd
- Dode componenten: `AdminHub`, `ComingSoonPage`.
- Legacy dashboards `/branch` en `/regional` redirecten naar `/home` (aliases `/banen`, `/admin`, `/admin/cockpit` blijven).

### Docs
- `ROLES_AND_VIEWS.md` en `REQUIREMENTS.md` bijgewerkt naar Sprints 4–7 + entry `/` ↔ `/home`.
- `MOCKDATA.md` beschrijft Sprint 8 metrics/logs seed.

### Review-fixes
- Metric drilldowns voor stock-KPI’s (active vacancies / users / companies); applications redacteren PII tot Accept (employers).
- `MetricsKeys.PlatformOnly` naar Core (Api niet meer afhankelijk van Infrastructure type).
- Seeder: geen early-exit op bestaande Spend/Allocation; intermediair-spend gebruikt bestaande vacancy-id; Café-guard voor archived vacancy.
- EmployerHomePanel toont alle metrics (geen `Take(8)`); Intermediary applications-link + `/branch/applicants` authorize.

## [0.7.0] - 2026-07-24 (Sprint 7: Registratie KVK + org-merge)
### Toegevoegd
- Publieke registratieflow `/register`: KVK → vestiging → scope (alleen-vestiging / hele organisatie) → contact → activatie-mail (stub link).
- Unique `KvkEstablishmentId` blijft de vestigingssleutel; activatie maakt `User` + `LocalAuthCredential` (uniek stub-wachtwoord).
- Conflictflow: vestiging al in gebruik → `EstablishmentTakeoverRequest`; inbox `/employer/takeovers`; na goedkeuring org-merge (parent-koppeling + token-allocatie naar organisatie).
- Login valt terug op `POST api/auth/local-login` voor geregistreerde accounts (naast DemoUsers).

### Beveiliging / review-fixes
- Activatietoken is one-time (cleared na gebruik); replay lekt geen wachtwoord; TTL 48u.
- `ActivationUrl` alleen in API-response als `JobsyFeatures:ExposeRegistrationActivationLinks` (Development aan).
- `stub-activation` is Admin-only + Development/feature-flag (niet anoniem).
- Organisatie-overname alleen door EnterpriseManager/Admin; prior owners verliezen memberships.
- Geen nieuwe parent bij bestaande `ParentCompanyId`; siblings met openstaande registratie worden niet geclaimd.
- Dubbele pending-registratie op dezelfde vestiging → conflict; EM claims∩DB her-expand children.

### API
- `api/registration` (submit / activate / kvk establishments / takeovers approve|reject)
- `api/auth/local-login`

## [0.6.0] - 2026-07-24 (Sprint 6: Admin suite)
### Toegevoegd
- Admin platformdashboard (`/home`) met doorklikbare KPI’s; platform-only metrics (`errors`, `companies_employers` / `companies_intermediaries`, users) alleen voor Admin.
- Admin modules: bedrijven/intermediairs, gebruikers, vacatures, financieel (KPI + tokenlog), platform logging, token-grant, WML/salaris, systeeminstellingen, integratie-pings.
- Intermediair-seed/backfill (`CompanyType.Intermediary`) voor admin KPI’s.
- Halfjaarlijkse WML-update stub (`POST api/wages/semi-annual-update`) + `MinimumWageUpdateHostedService` reminder-job (Europe/Amsterdam).
- Settings: token-pack pricing, spend-costs en early-adapter rules upsert via `api/settings`.
- Admin UI grids (`.table-scroll` / `.data-table`) en filters; placeholders (moderatie, masterdata, notificaties) als muted “later” onder `AdminPageShell`.

### API
- Uitbreiding admin-metrics drilldowns; `api/wages` upsert + semi-annual; `api/settings/token-pricing` (+ packs/costs/early-adapter); admin company/user/vacancy/log endpoints.

### Review-fixes
- `SalaryService` leest actuele `MinimumWageRates` uit de DB (hardcoded alleen als fallback) — admin WML-edits gelden voor `/check` en vacaturevalidatie.
- Semi-annual `EffectiveFrom` valt op de due-date (1 jan/1 jul) i.p.v. altijd de volgende periode; reminder-job gebruikt NL-kalender.
- Admin `UserCount` + users-filter tellen ook `UserCompanies`-memberships; `tokens_purchased` telt geen grants meer.
- `GET api/wages` toont alleen huidige effectieve tarieven per leeftijd; intermediair krijgt geen Westland-logo-fallback.

## [0.5.0] - 2026-07-24 (Sprint 5: Bedrijf / Regio / Vestiging UI)
### Toegevoegd
- Employer dashboards met doorklikbare metrics (periode-tabs + drilldown via `api/metrics`, scoped door `CompanyAuthorizationService`).
- Vacaturebeheer: rich-text toolbar, image/video-URL, salaristabel-dropdown, live preview-tegel, Publiceren-popup met token-opties (highlight / pushbom / verlengen).
- Tokens: Mollie-stub checkout (`/tokens/checkout-stub`), vestiging-allocatie (`Allocation` ledger), logs met bedrijfsnaam-filter.
- Vestigingen toevoegen via KVK-stub; Regio’s CRUD; Gebruikers invite-by-email + rollen (e-mail stub).
- Salaristabellen CRUD voor employers; beschikbaar bij vacatureplaatsing.
- Sollicitanten: progressive disclosure — eerst woonplaats/afstand/voorkeuren; na Accept volledige PII.

### API
- `POST api/tokens/checkout`, `POST api/tokens/checkout/complete`, `POST api/tokens/allocate`, `GET api/tokens/packs|costs`
- `api/regions` CRUD, `POST api/companies/from-kvk`, `api/salary-tables`, `api/company-users` (+ invite)
- Employer applications DTO zonder PII tot status Accepted

### Beveiliging / review-fixes
- Checkout-sessies persisted (`TokenPurchaseCheckout`); complete crediteert alleen server-side PackSize; onbekende paymentIds zijn niet betaald.
- Invite blokkeert cross-tenant overname en peer/hogere rollen.
- Salaristabel-upsert controleert ownership (geen IDOR via body CompanyId).
- KVK-vestiging moet matchen op parent-KVK; prefs geredacteerd tot Accept; preview zonder MarkupString XSS.
- `RequireCompanyAccess` faalt closed zonder CompanyId; application react via atomische Pending-update.

## [0.4.0] - 2026-07-24 (Sprint 4: Engagement ledger + token products)
### Toegevoegd
- Publish-opties: base / highlight / pushbom / extend met typed token-debit (`TrySpendMany`) en saldo-check.
- PushBom stub: selecteert `OpenForWork`-kandidaten binnen 10 km (`User.HomeLocation` + PostGIS / Haversine-fallback), schrijft push-logs.
- Verlengen (+14 dagen, `ExtensionCount`) en inactive (`Archived`).
- Onvoldoende tokens bij publish → `PendingApproval` + notificatie naar EnterpriseManager; `POST .../approve-publish` om goed te keuren.
- Employer vacaturebeheer-acties voor productflows; profiel ondersteunt thuislocatie.

### Opgelost (code review)
- PendingApproval alleen via `ApprovePublish` (geen bypass door branch managers).
- Aangevraagde publish-opties blijven bewaard (`RequestedHighlight/PushBom/Extend`) en worden bij goedkeuring afgeschreven.
- Status/flag opnieuw gevalideerd binnen spend-transactie (minder double-spend races).
- Push/e-mail pas ná commit; lege PushBom schrijft geen tokens af.
- UI: Goedkeuren alleen voor EnterpriseManager/Admin.

### Bestond al (bevestigd)
- Like / unlike / share / click engagement-API’s en typed `TokenTransaction` ledger.

## [0.3.0] - 2026-07-24 (Sprint 3: Kandidaat dashboard)
### Toegevoegd
- Kandidaat `/home` met metrics-tegels (sollicitaties / shares / likes / reacties) × dag/week/maand en drilldown-grids.
- Mijn sollicitaties, geliked en gedeeld-pagina's met echte data.
- Profiel: Open for work, voorkeuren (domeinen / reistijd / vervoer) en geboortedatum.
- Sollicitatieflow koppelt `CandidateUserId`, stuurt IEmailService-bevestiging (stub) en optionele Authenticator stub-flag (`JobsyFeatures:AuthenticatorEnabled`).
- Werkgever-reactie (`POST api/applications/{id}/react`) met stub e-mail + push + deeplink.
- `IPushNotificationService` stub en candidate-scoped metrics API (`api/me/metrics`).

### Opgelost
- Duplicate apply checkt nu ook op e-mail; unique indexes op `(VacancyId, CandidateUserId)` en `(VacancyId, CandidateEmail)`.
- Werkgever-react alleen toegestaan bij status Pending (geen spam-notificaties).
- Vacaturedetail toont bestaande sollicitatie na refresh.
- `PublicWebBaseUrl` wijst naar poort 5201; e-mail HTML wordt ge-escaped.

## [0.1.0] - 2026-07-23 (Week 1: Fundament & Demo MVP)
### Toegevoegd
- Initiële mappenstructuur volgens Clean Architecture (.NET 9).
- Database-entiteiten (`User`, `Company`, `Vacancy`, `TokenTransaction`) met Entity Framework Core en PostGIS ondersteuning.
- Automatische Database Seeder met realistische mockdata voor de regio's Westland en Den Haag.
- Basis Web API endpoints voor het opvragen van actieve vacatures.
- Eerste Blazor frontend component met een Funda-achtige split-screen opzet.
- Documentatie bestanden (`REQUIREMENTS.md`, `CONTEXT.md`, `SECURITY.md`, `TESTING.md`, `ARCHITECTURE.md`).
