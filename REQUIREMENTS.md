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
- **Vacancy** — Status (`Draft` / `Active` / `Archived` / `PendingApproval`), media, highlight, extensions, requested publish-opties, salary table
- **TokenTransaction** — typed ledger (`Purchase` / `Spend` / `Grant` / `Allocation`) + `TokenSpendReason` (Publish/Highlight/PushBom/Extend)
- **Application** — progressive PII tot Accept
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
- **Tokens:** live Mollie (Dev-stub zonder API-key); EM koopt in organisatiopot; geen automatische incasso; admin grant; uitgifte aan vestigingen
- **Employer suite:** vacature-editor, regio’s, vestigingen (KVK), gebruikers-invite, salaristabellen, sollicitanten
- **Registratie:** KVK-stub (+ SBI) → vestiging → wachtwoord → e-mailverificatie; na activatie dual auth (wachtwoord of Entra, zelfde e-mail); SBI `78*` → Intermediair, anders Bedrijfsmanager bij organisatiescope; conflict → takeover/org-merge
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
- **Revenue-share (defaults):** ambassadeur 15% tokens + direct SM 15% + indirect referring SM 3% (≤ 1 jaar, Admin-configureerbaar) + `RevenueShareLogs`
- **Admin:** Salesmanager-kolom op bedrijven; KPI gem. doorlooptijd vacatures

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
- **Na login** vanaf `/` of `/banen` → dashboard `/home`
- **Logout** → `/`
- Gedeelde UI: `BottomNav`, `TokenWalletChip`, `MetricTile`, `DrilldownGrid`, `ShareModal`, `PublishOptionsDialog`

## 6. Externe Koppelingen (stubs voor demo)
- **KVK API** — vestigingen/registratie
- **Mollie** — prepaid token-aankoop (live API; Development stub op `/tokens/checkout-stub`)
- **Mail** — activatie/invite/notificaties
- **OpenAI** — content-moderatie / mock interview (feature-flagged)
- Feature flags o.a. `JobsyFeatures:*` (activation-link exposure, Authenticator, stubs)
