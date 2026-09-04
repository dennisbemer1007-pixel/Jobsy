# Jobsy
*Hyper-lokale job-matching voor de regio Westland en Den Haag.*

Jobsy lost het knelpunt van traditionele vacaturebanken op door direct te matchen op reistijd en vervoersmiddel (zoals e-bike, auto en OV) in plaats van traditionele zoekfilters. Het platform gebruikt een intuïtieve Funda-achtige kaartinterface.

## Tech Stack
- **Backend:** .NET 9 (C#), ASP.NET Core Web API
- **Frontend:** Blazor Web
- **Database:** PostgreSQL + PostGIS (NetTopologySuite)
- **Routing:** Self-hosted OSRM (Docker)
- **Beveiliging:** Microsoft Entra ID

## Solution-structuur
```
Jobsy/
├── Jobsy.sln
├── Jobsy.Core/            # Domain: entities, enums, interfaces
├── Jobsy.Infrastructure/  # EF Core, seeder, OSRM/salary services
├── Jobsy.Api/             # ASP.NET Core Web API
└── Jobsy.Web/             # Blazor Funda-layout (lijst + Leaflet-kaart)
```

## Cloud-demo (Render, always-on)

Publiek zonder laptop: zie [`docs/deploy-render.md`](docs/deploy-render.md).
Blueprint: [`render.yaml`](render.yaml) → project **Lobsy**, omgevingen **Production** (`jobsy-api` / `jobsy-web` / `jobsy-db`) en **Acceptatie** (`lobsy-acc-*`). Starter web + Basic Postgres (geen idle spin-down).
Render **New → Blueprint** op GitHub-repo `dennisbemer1007-pixel/Jobsy` (of Manual sync op bestaande Blueprint).

## Lokaal starten

### 1. Vereisten
- [.NET 9 SDK](https://dotnet.microsoft.com/)
- [PostgreSQL](https://www.postgresql.org/) met **PostGIS**
- EF Core tools via lokale manifest: `dotnet tool restore`

### 2. Database
```sql
CREATE DATABASE "JobsyDb";
\c JobsyDb
CREATE EXTENSION IF NOT EXISTS postgis;
```

Pas eventueel de connection string aan in `Jobsy.Api/appsettings.json`:
```
Host=localhost;Port=5432;Database=JobsyDb;Username=postgres;Password=postgres
```

### 3. Migratie + starten
```powershell
# Vanuit de repo-root
dotnet restore Jobsy.sln
dotnet tool restore

# Migratie toepassen (InitialCreate staat al in de repo)
dotnet tool run dotnet-ef database update -p Jobsy.Infrastructure -s Jobsy.Api

# Terminal 1 – API (seeder vult Westland/Den Haag mockdata als DB leeg is)
dotnet run --project Jobsy.Api --launch-profile http

# Terminal 2 – Blazor frontend
dotnet run --project Jobsy.Web --launch-profile http
```

- API / Swagger: http://localhost:5200/swagger
- Frontend: http://localhost:5201

### 4. Inloggen (demo)
Rechtsboven: **Inloggen**. Ondersteund:
- **Microsoft Entra** en **Google** (zet `ClientId` / `ClientSecret` in `Jobsy.Web/appsettings.json`)
- **E-mail / wachtwoord** met demo-accounts:
  - `kandidaat@jobsy.local` (Kandidaat)
  - `ondernemer@jobsy.local` (Filiaalmanager)
  - `regio@jobsy.local` (Regiomanager)
  - `enterprise@jobsy.local` (Enterprise)
  - `intermediair@jobsy.local` (Intermediair)
  - `admin@jobsy.local` (Admin)
  - `sales@jobsy.local` (Salesmanager)
  - Wachtwoord: `Jobsy123!`

## Demo-materiaal
Per rol: demo-script, werkbeschrijving en printscreens, plus een PowerPoint-overzicht:

- [`docs/demo/README.md`](docs/demo/README.md) — index
- [`docs/demo/Jobsy-Presentatie.pptx`](docs/demo/Jobsy-Presentatie.pptx) — presentatie (8 slides)
- [`docs/demo/rollen/`](docs/demo/rollen/) — werkbeschrijving + demo per rol
- [`docs/TESTSCENARIOS_PER_ROL.md`](docs/TESTSCENARIOS_PER_ROL.md) — UAT-testgrid per rol (happy + unhappy); CSV: [`docs/testscenarios-per-rol.csv`](docs/testscenarios-per-rol.csv). Uitvoeren: `dotnet test --filter Suite=Uat999` of shortcut **`999`**.
