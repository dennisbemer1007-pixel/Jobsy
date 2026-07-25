# Jobsy op Render (gratis demo)

Tijdelijke publieke demo zonder laptop. Free tier: services slapen na ~15 min idle (~1 min cold start). Free Postgres vervalt na **30 dagen**.

## Eenmalig: code + Blueprint

1. Zorg dat deze repo op GitHub staat: `dennisbemer1007-pixel/Jobsy` (main/master met `render.yaml`).
2. Maak een gratis account op [https://render.com/register](https://render.com/register) (GitHub-login mag).
3. In Render Dashboard: **New** → **Blueprint**.
4. Selecteer de GitHub-repo `Jobsy` en bevestig de Blueprint (`render.yaml`).
5. Kies region **Frankfurt** als gevraagd (staat al in de Blueprint).
6. Wacht tot `jobsy-db`, `jobsy-api` en `jobsy-web` groen zijn (eerste build kan 5–10 min duren).

## Gebruiken

- Open de URL van **jobsy-web** (Dashboard → service → `.onrender.com`).
- Demo-login: `kandidaat@jobsy.local` / `Jobsy123!` (zelfde accounts als lokaal).
- API/Swagger: URL van **jobsy-api** + `/swagger`.

Na idle even geduld bij de eerste hit (cold start). Soms moet je de pagina twee keer laden als Web al wakker is maar API nog opstart.

## Wat de Blueprint zet

| Service | Plan | Rol |
|---------|------|-----|
| `jobsy-db` | Free Postgres 16 + PostGIS (via EF) | Database |
| `jobsy-api` | Free web (Docker) | API + seed |
| `jobsy-web` | Free web (Docker) | Blazor UI |

Env o.a.: `JobsyAuth__AllowDevelopmentAuth=true` (demo header-auth, **niet** voor echte productie).

## Lokaal Docker (optioneel)

```powershell
docker build -f Jobsy.Api/Dockerfile -t jobsy-api .
docker build -f Jobsy.Web/Dockerfile -t jobsy-web .
```
