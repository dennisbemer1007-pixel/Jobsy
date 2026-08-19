# Requirements & Architectuur: Jobsy (MVP)

## 1. Doel van de Applicatie
Jobsy is een hyper-lokale job-matching applicatie gericht op de regionale arbeidsmarkt (startend in Westland en Den Haag), waarbij de nadruk ligt op reistijd en vervoersmiddel in plaats van traditionele zoekfilters. Het platform gebruikt een "Funda-model" (directe visuele controle via een kaart en lijst).

## 2. Tech Stack
- **Backend:** .NET 9 (C#), ASP.NET Core Web API (`Jobsy.Api`)
- **Frontend:** Blazor Web (Interactive Server) (`Jobsy.Web`)
- **Domain / Infra:** `Jobsy.Core` (entities, rules, interfaces) + `Jobsy.Infrastructure` (EF Core, seeders, stub services)
- **Tests:** `Jobsy.Tests`
- **Database:** PostgreSQL met PostGIS (geografische coördinaten / spatial queries)
- **ORM:** Entity Framework Core met NetTopologySuite
- **Routing Engine:** Self-hosted OSRM in Docker
- **Authenticatie:** Microsoft Entra ID + Google + lokale/demo-login (`POST api/auth/local-login`) en registratie-activatie

## 3. Domein: Rollen & Entiteiten

### Rollen
| Rol | Doel |
|-----|------|
| **Candidate** | Zoekt vacatures op reistijd/vervoer; solliciteert, liket, deelt |
| **BranchManager** | Beheert vacatures/tokens voor één vestiging |
| **RegionalManager** | Overzicht over meerdere vestigingen in een regio |
| **EnterpriseManager** | Organisatiebreed: regio’s, gebruikers, tokens, overnames |
| **Intermediary** | Werven voor meerdere externe opdrachtgevers |
| **Admin** | Platformbeheer: bedrijven, finance, WML, settings, logging |

### Kernentiteiten (niet exhaustief)
- **User** — Email, FullName, Role, HomeLocation, OpenForWork, prefs, early-adapter
- **Company** — KVK + `KvkEstablishmentId`, hierarchy (`ParentCompanyId`), `CompanyType` (Employer/Intermediary)
- **Vacancy** — Status (`Draft` / `Active` / `Archived` / `PendingApproval`), media, highlight, extensions, requested publish-opties, salary table, **VacancyCategory** (kleur, tokenprijs, highlight/PushBom-beschikbaarheid, extra aanmaakvelden)
- **VacancyCategory** — Admin-beheerbare categorieën; sturen kaartfilter, legenda, create-dropdown en tokenlogica
- **TokenTransaction** — typed ledger (`Purchase` / `Spend` / `Grant` / `Allocation`) + `TokenSpendReason` (Publish/Highlight/PushBom/Extend)
- **Application** — progressive PII tot Accept; Lobsy-CV PDF én geüpload kandidaat-CV pas na Accept (zie §4d)
- **Engagement** — VacancyClick / Like / Share
- **Region** / **CompanySalaryTable** / **TokenPurchaseCheckout**
- **CompanyRegistration** / **EstablishmentTakeoverRequest** / **LocalAuthCredential**
- **Platform settings** — token packs/costs, PushBom tiers, early-adapter, WML, PlatformLogs, integraties

## 4. Kernfunctionaliteiten (MVP & Demo)
- **Banenkaart (`/`):** Funda split-screen (lijst + OpenStreetMap), reistijd/afstand via PostGIS + OSRM
- **Role dashboards (`/home`):** doorklikbare KPI’s (dag/week/maand) + drilldown voor kandidaat, werkgever en admin
- **Token-producten:** publiceren / highlight / PushBom / verlengen; prepaid “no tokens, no action”
- **Onvoldoende saldo:** blokkeer actie + in-context Mollie-checkout (exact match of bulkapakket); na webhook/return → tokens bijschrijven én pending actie uitvoeren. Vestigingsmanagers zonder kooprecht blijven op `PendingApproval`
- **PushBom:** OpenForWork-kandidaten binnen radius/reistijd; pricing tiers uit settings
- **Tokens:** live Mollie iDEAL + creditcard (Dev-stub zonder API-key); EM koopt in organisatiopot; geen automatische incasso; webhook → instant saldo/pending actie; bedrijfsprofiel: betaalvoorkeur + factuurhistorie; admin grant; uitgifte aan vestigingen
- **Employer suite:** vacature-editor, regio’s, vestigingen (KVK), gebruikers-invite, salaristabellen, sollicitanten
- **Registratie:** KVK-stub (+ SBI) → vestiging → scope → wachtwoord → e-mailverificatie; na activatie dual auth (wachtwoord of Entra, zelfde e-mail); SBI `78*` → Intermediair; anders altijd Bedrijfsmanager — Organization = org-boom, BranchOnly = vestiging-als-bedrijf (kan vestigingsmanagers uitnodigen); conflict → takeover/org-merge
- **Admin suite:** bedrijven, users, vacatures, finance/tokenlog, logging, settings, integratie-pings, WML (incl. halfjaarlijkse update-stub)
- **Mockdata:** rijke seed (engagement, spends, logs, statusmix) zodat dashboards gevuld zijn

## 4b. Matching, dagdelen, uren & Arbeidstijdenwet (specificatie)

Volledige functionele specificatie voor kandidaat- en werkgeverskant:

→ **[`docs/FUNCTIONELE_SPECIFICATIES_MATCHING.md`](docs/FUNCTIONELE_SPECIFICATIES_MATCHING.md)**

## 4c. Intermediair, salesmanager revenue-share & doorlooptijd-KPI

→ **[`docs/FUNCTIONELE_SPECIFICATIES_INTERMEDIAIR_SALES_KPI.md`](docs/FUNCTIONELE_SPECIFICATIES_INTERMEDIAIR_SALES_KPI.md)**

Kernpunten:
- **Intermediair-team:** collega’s uitnodigen op dezelfde organisatie; gedeelde vacatures/saldo
- **KVK verplicht** bij intermediair-vacatures (UI/CSV/API); flexibele adresweergave + “Aangeboden door …”
- **Salesmanager-hiërarchie:** Admin maakt tier-0 aan; tier-0 kan SM-aanbevelingen indienen; Admin keurt goed; tier-1 mag niet verder werven
- **Ambassadeur-rol:** onboarding als Salesmanager; trackingcode `AM-…`; KPI’s (kandidaten + sollicitaties); gelaagde commissie (basis 5%, +1% per 50 kandidaten, Admin-max); kandidaten- + ondernemersflyer (QR); commissie op tokenaankopen referred ondernemers; self-billing uitbetaling
- **Revenue-share / commissie-settlement (realtime via Mollie-webhook):** bij betaalde tokenaankoop van een referred ondernemer → direct SM **15%** + upline SM **3%** van ex-BTW bedrag op `CommissionLedger` (dashboardsaldo), ambassadeur **15%** tokens; strikt ≤ **1 jaar** vanaf `FirstYearStartedAt` (Admin-configureerbaar); idempotent + retry op webhook/complete; `RevenueShareLogs`
- **Admin:** Salesmanager-kolom op bedrijven; KPI gem. doorlooptijd vacatures

## 4d. Lobsy-CV preview, PDF-vrijgave & AI-profielcoach

→ **[`docs/FUNCTIONELE_SPECIFICATIES_CV_PREVIEW_MODERATIE.md`](docs/FUNCTIONELE_SPECIFICATIES_CV_PREVIEW_MODERATIE.md)**

Kernpunten:
- **Voorbeeld-PDF** vóór verzenden: kandidaat inzage/download van automatisch Lobsy-CV (QuestPDF uit profiel + optionele motivatie)
- **Eigen CV-upload:** kandidaat mag PDF/DOCX uploaden; OpenAI vult alleen lege profielvelden die écht duidelijk in het CV staan. Lobsy-CV vermeldt bovenaan dat er een eigen CV is. Werkgever downloadt beide bestanden pas na Accept.
- **Recensies:** kandidaat voegt werkgever + contactpersoon + e-mail + telefoon toe. Vacature kan `MinimumReferences` eisen (standaard geen); apply is dan geblokkeerd tot het profiel genoeg complete recensies heeft.
- **AVG / progressive disclosure:** werkgever (intermediair, bedrijfs-/vestigingsmanager) ziet PDF **pas na Accept** (`Accepted` / `EmployerContacting` / `Hired`); endpoint enforce’t zelfde `PiiRevealed`-regel
- **AI-profielcoach:** lichte moderatie/feedback (heuristics + optionele OpenAI) op AboutMe/motivatie — spelling/taal, te korte velden, beschikbaarheid/match-tips; soft tips blokkeren niet, PII-in-tekst wel

## 4e. Admin lancerings-KPI-dashboard (Westland-campagne)

→ **[`docs/FUNCTIONELE_SPECIFICATIES_ADMIN_LAUNCH_KPI.md`](docs/FUNCTIONELE_SPECIFICATIES_ADMIN_LAUNCH_KPI.md)**

Kernpunten:
- **Teaser-kliks** totaal + uniek op `/westland`, met **UTM/QR-kanalen** (`TeaserEngagementEvents`)
- **Pre-18 nov:** gratis vacatures, Westland-ondernemers, stages & vrijwilligerswerk
- **Tokens:** pakketten, jaardeal €3.000, saldo vs verbruik
- **UI:** `/admin/launch` met MetricTiles, timeseries, UTM-tabel, ~20s refresh

## 4f. Matching — kernpunten (samenvatting)

→ Zie ook §4b / matching-spec.

Kernpunten:
- **Dagdelen-matrix** (vacature + profiel) met vaste blokken Ochtend/Middag/Avond/Nacht en optie **“Tijden in overleg”** (handmatig of auto bij lege API/CSV/ATS-import)
- **Verplichte uren** min/max per week + automatische urencategorie (bijbaan/parttime/fulltime)
- **Geen UI-minimumleeftijd;** achtergrondfiltering via verplichte wettelijke taak-vinkjes + `[ i ]`-tooltips (Arbeidstijdenwet)
- **Matchingspercentage** op banenkaart met breakdown-modal en actie-adviezen
- **Gulden Middenweg** bij solliciteren (&lt; 50%): OTP tegenhouden, profiel aanpassen of vangnet
- **Optioneel motivatieveld** op sollicitatieformulier
- **Werkgeversdashboard:** match-% met kleurcodering, breakdown, wettelijke bevestiging, motivatie, sort hoog→laag
- **CSV/API:** uren + legal flags verplicht; dagdelen optioneel → “Tijden in overleg”

## 5. Navigatie & entry points
- **Anoniem** → banenkaart `/` (BottomNav: Banenkaart, Registreren, Inloggen)
- **Westland-teaser** → `/westland` (alias `/lancering`): livedatum 18 nov, gratis periode, tarieven, WhatsApp Dennis
- **Na login** vanaf `/` of `/banen` → dashboard `/home`
- **Logout** → `/`
- Gedeelde UI: `BottomNav`, `TokenWalletChip`, `MetricTile`, `DrilldownGrid`, `ShareModal`, `PublishOptionsDialog`

## 6. Externe Koppelingen (stubs voor demo)
- **KVK API** — vestigingen/registratie
- **Mollie** — prepaid token-aankoop (live API; Development stub op `/tokens/checkout-stub`)
- **Mail** — activatie/invite/notificaties
- **OpenAI** — vacature-contentmoderatie / mock interview / kandidaat-profielcoach (feature-flagged)
- Feature flags o.a. `JobsyFeatures:*` (activation-link exposure, Authenticator, stubs)
