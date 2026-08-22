# Jobsy demo-materiaal

Demo-scripts, werkbeschrijvingen met screenshots en een PowerPoint-presentatie van de app.

## Inhoud

| Bestand | Beschrijving |
|---------|--------------|
| [Jobsy-Presentatie.pptx](Jobsy-Presentatie.pptx) | Visuele overview van de hele app (enkele slides) |
| [rollen/00-publiek.md](rollen/00-publiek.md) | Publieke banenkaart & inloggen |
| [rollen/01-kandidaat.md](rollen/01-kandidaat.md) | Rol Kandidaat |
| [rollen/02-filiaalmanager.md](rollen/02-filiaalmanager.md) | Rol Filiaalmanager |
| [rollen/03-regiomanager.md](rollen/03-regiomanager.md) | Rol Regiomanager |
| [rollen/04-enterprise.md](rollen/04-enterprise.md) | Rol Enterprise / Bedrijfsmanager |
| [rollen/05-intermediair.md](rollen/05-intermediair.md) | Rol Intermediair |
| [rollen/06-admin.md](rollen/06-admin.md) | Rol Admin |
| [screenshots/](screenshots/) | Alle printscreens |
| [Testscenario’s per rol](../TESTSCENARIOS_PER_ROL.md) | UAT-grid (happy + unhappy) per rol; CSV: [testscenarios-per-rol.csv](../testscenarios-per-rol.csv) |

## Lokaal starten voor een live demo

```powershell
# Terminal 1
dotnet run --project Jobsy.Api --launch-profile http

# Terminal 2
dotnet run --project Jobsy.Web --launch-profile http
```

- Frontend: http://localhost:5201  
- API/Swagger: http://localhost:5200/swagger  

## Demo-accounts

Wachtwoord voor alle accounts: `Jobsy123!`

| E-mail | Rol |
|--------|-----|
| `kandidaat@jobsy.local` | Kandidaat |
| `ondernemer@jobsy.local` | Filiaalmanager |
| `regio@jobsy.local` | Regiomanager |
| `enterprise@jobsy.local` | Enterprise / Bedrijfsmanager |
| `intermediair@jobsy.local` | Intermediair |
| `admin@jobsy.local` | Admin |
| `sales@jobsy.local` | Salesmanager |

## Screenshots opnieuw maken

Met API + Web draaiend:

```powershell
node docs/demo/capture-screenshots.cjs
```
