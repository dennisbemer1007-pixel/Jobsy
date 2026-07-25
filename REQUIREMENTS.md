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
| **Intermediary** | Batch-werving voor meerdere externe opdrachtgevers |
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
- **Token-producten:** publiceren / highlight / PushBom / verlengen; onvoldoende saldo → `PendingApproval` (EM/Admin keurt goed)
- **PushBom:** OpenForWork-kandidaten binnen radius/reistijd; pricing tiers uit settings
- **Tokens:** Mollie-stub checkout, admin grant, vestiging-allocatie
- **Employer suite:** vacature-editor, regio’s, vestigingen (KVK), gebruikers-invite, salaristabellen, sollicitanten
- **Registratie:** KVK-stub → vestiging → activatie; conflict → takeover/org-merge
- **Admin suite:** bedrijven, users, vacatures, finance/tokenlog, logging, settings, integratie-pings, WML (incl. halfjaarlijkse update-stub)
- **Mockdata:** rijke seed (engagement, spends, logs, statusmix) zodat dashboards gevuld zijn

## 5. Navigatie & entry points
- **Anoniem** → banenkaart `/` (BottomNav: Banenkaart, Registreren, Inloggen)
- **Na login** vanaf `/` of `/banen` → dashboard `/home`
- **Logout** → `/`
- Gedeelde UI: `BottomNav`, `TokenWalletChip`, `MetricTile`, `DrilldownGrid`, `ShareModal`, `PublishOptionsDialog`

## 6. Externe Koppelingen (stubs voor demo)
- **KVK API** — vestigingen/registratie
- **Mollie** — token-aankoop (`/tokens/checkout-stub`)
- **Mail** — activatie/invite/notificaties
- **OpenAI** — content-moderatie / mock interview (feature-flagged)
- Feature flags o.a. `JobsyFeatures:*` (activation-link exposure, Authenticator, stubs)
