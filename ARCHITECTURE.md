# Technische Architectuur: Jobsy

## 1. Clean Architecture Principes
De applicatie is opgesplitst in strikte lagen om afhankelijkheden te isoleren:
- **Jobsy.Core (Domain):** Bevat alle entiteiten, enums, business logica en interfaces (`IRoutingService`, `ISalaryService`). Deze laag heeft geen enkele externe afhankelijkheid.
- **Jobsy.Infrastructure (Data & Services):** Bevat de `JobsyDbContext`, EF Core configuraties, migraties, de database seeder en externe API-clients (zoals OSRM en mocks voor KVK/Mollie).
- **Jobsy.Api (Backend / Web API):** De ASP.NET Core Web API die endpoints exposed voor de frontend, beveiligd met Microsoft Entra ID.
- **Jobsy.Web (Frontend):** Blazor Web applicatie voor de gebruikersinterface (Funda-stijl dashboard en kaartweergave).

## 2. Geografische Data & Routing
- **PostGIS:** PostgreSQL wordt gebruikt met de PostGIS extensie en NetTopologySuite. Coördinaten worden opgeslagen als `Point` met een spatial index voor snelle straal-filters (`ST_Distance`).
- **OSRM (Open Source Routing Machine):** Self-hosted routing-engine in een Docker-container voor het berekenen van exacte reistijden en afstanden per vervoersmiddel (fiets, auto, OV), ter voorkoming van externe API-kosten.

## 3. Cloud & Deployment (T/A/P Strategie)
- **Infrastructuur:** Azure App Service (of AWS App Runner) met ondersteuning voor Docker-containers.
- **Omgevingen (Deployment Slots):**
  - **Dev / Test:** Automatische deployment vanuit de development-branch voor directe validatie.
  - **Staging / Acceptatie:** Exacte kopie van productie voor eindvalidatie van features.
  - **Production:** De stabiele, live omgeving voor demo's en gebruikers.