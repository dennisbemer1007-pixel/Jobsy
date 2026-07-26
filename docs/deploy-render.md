# Jobsy op Render (always-on demo)

Publieke demo zonder laptop. Blueprint gebruikt **betaalde instance types**:

| Resource | Plan | Effect |
|----------|------|--------|
| `jobsy-api` / `jobsy-web` | **Starter** (~$7/mo elk) | Geen spin-down na idle |
| `jobsy-db` | **Basic-256mb** | Geen 30-dagen free-expiry |

Een workspace-betaalplan of creditcard alleen is **niet** genoeg: Free instances blijven slapen. Het instance-type per service telt.

Indicatie kosten: ~$14/mo web + Postgres-compute/storage (prorata per seconde). Zie [Render pricing](https://render.com/pricing).

## Security (demo)

De Blueprint houdt `JobsyAuth__AllowDevelopmentAuth=true` zodat demo-login via de Web UI werkt, maar:

- `JobsyAuth__DevelopmentAuthSecret` wordt gegenereerd op `jobsy-api` en gedeeld met `jobsy-web`. Alleen requests met die secret-header worden geaccepteerd — spoofing van `X-Jobsy-Email` vanaf het internet werkt niet meer.
- `JobsyFeatures__ExposeRegistrationActivationLinks=false` (geen activatie-URL in API-responses).
- Custom domain: `PublicWebBaseUrl=https://lobsy.nl` + CORS voor `lobsy.nl` / `www.lobsy.nl`.

Na Blueprint sync: controleer dat beide services dezelfde `JobsyAuth__DevelopmentAuthSecret` hebben.

## Eenmalig: code + Blueprint

1. Repo op GitHub: `dennisbemer1007-pixel/Jobsy` (branch `main` met `render.yaml`).
2. Account op [https://render.com/register](https://render.com/register) (GitHub-login) + betaalmethode.
3. Render Dashboard: **New** → **Blueprint** → repo **Jobsy** → Deploy.

## Bestaande free-deploy upgraden

1. Push deze `render.yaml` naar `main`.
2. Blueprint-pagina → **Manual sync** (of wacht op auto-sync).
3. Bevestig upgrades naar Starter / Basic-256mb in het Dashboard.
4. Controleer na sync:
   - `jobsy-api` → **Environment**: `ConnectionStrings__JobsyDb` is een echte `postgres://` / `postgresql://` URL
   - `jobsy-api` Logs: `Seeding Jobsy mock data` of `Seed completed`
   - `jobsy-api` URL + `/health` → OK
5. Open `jobsy-web`; mockdata (Westland / Den Haag vacatures) hoort zichtbaar te zijn.

Als de connection string leeg is of corrupt (vaak na DB-upgrade), zie hieronder.

## Als sync faalt of API “Failed” is (regio-mismatch)

Eerdere deploys hadden DB in **Oregon** en web in **Frankfurt**. Regio’s zijn **niet** te wijzigen.

1. Verwijder in het Dashboard (Allow/confirm alles):
   - `jobsy-api`
   - `jobsy-web`
   - `jobsy-db`
2. Blueprint-pagina → **Manual sync**
3. Wacht tot alle drie opnieuw groen zijn (zelfde regio: **Frankfurt**)
4. API herseedt mockdata bij eerste start op een lege DB

## Gebruiken

- URL: klik **`jobsy-web`** → link bovenaan (`https://….onrender.com`)
- Login: `kandidaat@jobsy.local` / `Jobsy123!`
- API check: **`jobsy-api`** URL + `/health`

Services blijven draaien; geen cold start na idle.

## Antiforgery / “key was not found in the key ring”

Na een redeploy kan Render kort dit loggen als je browser nog oude cookies heeft:

`The antiforgery token could not be decrypted` / `The key {...} was not found in the key ring`

**Nu meteen:** site-cookies voor `*.onrender.com` wissen (of privévenster) en opnieuw laden.

**Structureel:** `jobsy-web` bewaart Data Protection-keys in Postgres (`ConnectionStrings__JobsyDb`). Zorg dat die env-var gezet is (Blueprint zet dit via `jobsy-db`). Zonder DB-keys blijven cookies na elke deploy ongeldig.

## Crash: inotify / FileSystemWatcher limit

Als de API crasht met:
`The configured user limit (128) on the number of inotify instances has been reached`

dan heeft .NET te veel file-watchers (config reload). De Dockerfiles en Blueprint zetten
`DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false`. Na push: Manual Deploy van `jobsy-api` (en eventueel `jobsy-web`).

## Connection string fout (na DB-upgrade)

Als `jobsy-api` crasht met:
`Format of the initialization string does not conform to specification starting at index 0`

dan is `ConnectionStrings__JobsyDb` leeg of geen echte Postgres-string — mockdata en de site blijven dan leeg/kapot.

1. Open **`jobsy-db`** → **Info** → kopieer **Internal Database URL**  
   (begint met `postgres://` of `postgresql://`)
2. Open **`jobsy-api`** én **`jobsy-web`** → **Environment**
3. Zet / herstel key **`ConnectionStrings__JobsyDb`** op die volledige URL (geen aanhalingstekens)
4. **Save** → Manual Deploy van `jobsy-api` (web daarna desnoods ook)
5. Optioneel in DB-shell: `CREATE EXTENSION IF NOT EXISTS postgis;`
6. In API-logs bevestigen dat de seeder draait

## Waarom zo geconfigureerd

| Keuze | Reden |
|-------|--------|
| `plan: starter` op api + web | Always-on; geen 15-min spin-down |
| `plan: basic-256mb` op DB | Blijvende Postgres (geen free 30-dagen expiry) |
| Alles `frankfurt` | Zelfde private network voor Postgres |
| `RENDER_EXTERNAL_URL` | Stabiele cross-service HTTP (ook op free bruikbaar) |
| `ConnectionStrings__JobsyDb` op **web én api** | Data Protection-keys in Postgres (antiforgery/auth cookies na redeploy) |
| `JobsyAuth__AllowDevelopmentAuth` | Demo-logins zonder Entra (niet voor echte productie) |
