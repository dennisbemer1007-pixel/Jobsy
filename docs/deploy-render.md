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

## Antiforgery / “key was not found in the key ring”

Na een redeploy kan Render kort dit loggen als je browser nog oude cookies heeft:

`The antiforgery token could not be decrypted` / `The key {...} was not found in the key ring`

**Nu meteen:** site-cookies voor `*.onrender.com` wissen (of privévenster) en opnieuw laden.

**Structureel:** `jobsy-web` bewaart Data Protection-keys in Postgres (`ConnectionStrings__JobsyDb`). Zorg dat die env-var gezet is (Blueprint zet dit via `jobsy-db`). Zonder DB-keys blijven cookies na elke deploy ongeldig.

## Connection string fout (Starter-upgrade)

Als `jobsy-api` crasht met:
`Format of the initialization string does not conform to specification starting at index 0`

dan is `ConnectionStrings__JobsyDb` leeg of geen echte Postgres-string.

1. Open **`jobsy-db`** → **Info** → kopieer **Internal Database URL**  
   (begint met `postgres://` of `postgresql://`)
2. Open **`jobsy-api`** → **Environment**
3. Zet / herstel key **`ConnectionStrings__JobsyDb`** op die volledige URL (geen aanhalingstekens)
4. **Save** → Manual Deploy van `jobsy-api`
5. Optioneel in DB-shell: `CREATE EXTENSION IF NOT EXISTS postgis;`

## Waarom zo geconfigureerd

| Keuze | Reden |
|-------|--------|
| Alles `frankfurt` | Zelfde private network voor Postgres |
| `RENDER_EXTERNAL_URL` | Free web services mogen geen privé-HTTP van elkaar ontvangen |
| `ConnectionStrings__JobsyDb` op **web én api** | Data Protection-keys in Postgres (antiforgery/auth cookies na redeploy) |
| `JobsyAuth__AllowDevelopmentAuth` | Demo-logins zonder Entra (niet voor echte productie) |
