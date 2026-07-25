# Jobsy op Render (gratis demo)

Tijdelijke publieke demo zonder laptop. Free tier: services slapen na ~15 min idle (~1 min cold start). Free Postgres vervalt na **30 dagen**.

## Eenmalig: code + Blueprint

1. Repo op GitHub: `dennisbemer1007-pixel/Jobsy` (branch `main` met `render.yaml`).
2. Account op [https://render.com/register](https://render.com/register) (GitHub-login).
3. Render Dashboard: **New** → **Blueprint** → repo **Jobsy** → Deploy.

## Als sync faalt of API “Failed” is (regio-mismatch)

Eerdere deploys hadden DB in **Oregon** en web in **Frankfurt**. Regio’s zijn **niet** te wijzigen.

1. Verwijder in het Dashboard (Allow/confirm alles):
   - `jobsy-api`
   - `jobsy-web`
   - `jobsy-db`
2. Blueprint-pagina → **Manual sync**
3. Wacht tot alle drie opnieuw groen zijn (zelfde regio: **Frankfurt**)

## Gebruiken

- URL: klik **`jobsy-web`** → link bovenaan (`https://….onrender.com`)
- Login: `kandidaat@jobsy.local` / `Jobsy123!`
- API check: **`jobsy-api`** URL + `/health`

Na idle: eerste hit ~1 min cold start. Soms 2× laden (Web wakker, API nog niet).

## Waarom zo geconfigureerd

| Keuze | Reden |
|-------|--------|
| Alles `frankfurt` | Zelfde private network voor Postgres |
| `RENDER_EXTERNAL_URL` | Free web services mogen geen privé-HTTP van elkaar ontvangen |
| `JobsyAuth__AllowDevelopmentAuth` | Demo-logins zonder Entra (niet voor echte productie) |
