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

- Buiten Development accepteert header-auth `@jobsy.local` demo-accounts met de gedeelde secret; echte registratie-/OAuth-gebruikers sturen ook `X-Jobsy-Local-Session` (HMAC met `LocalSessionSigningKey`, vernieuwd bij session-activity).
- OAuth client-secrets vereisen een aparte `JobsyAuth__ExternalProvisionSecret` (niet dezelfde DevelopmentAuthSecret; Web gebruikt geen DevelopmentAuthSecret-fallback meer).
- Custom domain: `PublicWebBaseUrl=https://lobsy.nl` + CORS voor `lobsy.nl` / `www.lobsy.nl`.

- `JobsyAuth__DevelopmentAuthSecret` wordt gegenereerd op `jobsy-api` en gedeeld met `jobsy-web`. Alleen requests met die secret-header worden geaccepteerd — spoofing van `X-Jobsy-Email` vanaf het internet werkt niet meer.
- `JobsyAuth__LocalSessionSigningKey` wordt apart gegenereerd en gedeeld voor HMAC-sessietokens van niet-demo gebruikers.
- `JobsyAuth__ExternalProvisionSecret` wordt apart gegenereerd en gedeeld met `jobsy-web` voor OAuth credential-provisioning.
- `JobsyFeatures__ExposeRegistrationActivationLinks=false` (geen activatie-URL in API-responses).

Na Blueprint sync: controleer dat beide services dezelfde `JobsyAuth__DevelopmentAuthSecret`, `JobsyAuth__LocalSessionSigningKey` én `JobsyAuth__ExternalProvisionSecret` hebben.

## Acceptatie (environment in project **Lobsy**)

Productie staat in **Lobsy → Production** (`lobsy-api` / `lobsy-web` / `lobsy-db`, branch `main`).
**Lobsy → Acceptatie** is dezelfde stack (Docker, Starter, Basic-256mb Postgres, Frankfurt) maar **nieuwe** resources. Klik **niet** op **Move existing services** — dat haalt productie uit Production.

Render-servicenamen zijn uniek in de hele workspace. De prod-Blueprint (`render.yaml`) niet een tweede keer toepassen. Gebruik `render.acceptatie.yaml` of maak de drie services in Acceptatie (stappen hieronder).

### Wat “identiek” wél en niet is

| Wel hetzelfde | Nooit delen met prod |
|---------------|----------------------|
| Dockerfiles, plans, regio, health checks, feature-flags | Database (`lobsy-db`) |
| Branch `Acceptatie` (nu gelijk aan `main`) | Signing keys / peppers |
| Demo-login gedrag | Mollie live-keys + echte betalingen |
| | Resend + testdata naar echte gebruikers |

Kosten: nog eens ~$14/mo web + Postgres (naast productie).

### Aanbevolen: tweede Blueprint

1. Zorg dat `render.acceptatie.yaml` op branch **`Acceptatie`** staat (al in deze repo).
2. In Render: **New → Blueprint**.
3. Repo **Jobsy**, branch **`Acceptatie`**.
4. **Blueprint Path:** `render.acceptatie.yaml` (niet het default `render.yaml`).
5. Deploy. Render maakt in **Lobsy → Acceptatie**:
   - `lobsy-db-acceptatie`
   - `lobsy-api-acceptatie`
   - `lobsy-web-acceptatie`
6. Wacht tot alle drie groen zijn. Open `lobsy-web-acceptatie` → `https://….onrender.com`.
7. API-check: `lobsy-api-acceptatie` URL + `/health`.
8. Kopieer **alleen** dashboard-secrets die je op acceptatie nodig hebt (zie tabel hieronder). Nieuwe waarden, geen copy-paste van prod-signing-keys.

Landen services in Ungrouped: **••• → Move** naar **Lobsy / Acceptatie**. Verplaats nooit `lobsy-api` / `lobsy-web` / `lobsy-db`.

### Dashboard: leeg Acceptatie-scherm

Op **Lobsy → Acceptatie** (“Acceptatie is empty”):

1. **+ Create new service** → **Postgres** — naam `lobsy-db-acceptatie`, plan **Basic 256 MB**, regio **Frankfurt**, Postgres 16. Create.
2. **+ Create new service** → **Web Service** — repo Jobsy, Docker, branch **`Acceptatie`**, `./Jobsy.Api/Dockerfile`, context `.`, plan **Starter**, Frankfurt, health `/health`, naam `lobsy-api-acceptatie`.
3. **+ Create new service** → **Web Service** — zelfde repo/branch, `./Jobsy.Web/Dockerfile`, health `/`, naam `lobsy-web-acceptatie`.
4. Env-vars (tweede tab: Production `lobsy-api` / `lobsy-web` als voorbeeld van de key-namen):

| Key | Acceptatie-waarde |
|-----|-------------------|
| `ConnectionStrings__JobsyDb` | Internal URL van **`lobsy-db-acceptatie`** (api én web) |
| `ApiBaseUrl` (web) | URL van **`lobsy-api-acceptatie`** |
| `PublicApiBaseUrl` (api) | idem |
| `PublicWebBaseUrl` (api) | URL van **`lobsy-web-acceptatie`** |
| `Cors__AllowedOrigins__0` | diezelfde web-URL |
| `JobsyAuth__DevelopmentAuthSecret` e.d. | **nieuw** genereren; web = zelfde waarde als api |

Zet **geen** `https://lobsy.nl` in CORS of `PublicWebBaseUrl` op acceptatie — anders wijzen mails/links naar productie.

Snel overzicht van prod-env: Production `lobsy-api` + `lobsy-web` selecteren → **Generate Blueprint** (waarden komen er niet in, wel de key-namen).

### Secrets en integraties overzetten

| Integratie | Acceptatie |
|------------|------------|
| Resend (`Mail__ResendApiKey` / `FromAddress`) | Alleen zetten als je écht mail wilt testen. Bij een testdatabase: test-inbox of geen key. **Nooit** prod-data + prod-Resend (dan mailen echte gebruikers). |
| Sentry `Sentry__Dsn` | Mag dezelfde Dsn; filter op environment/host. |
| Entra / Google | Nieuwe redirect URI’s: `{acceptatie-web}/signin-entra` en `/signin-google`. |
| Mollie | **Test-keys**, tenzij je bewust live wilt. `JobsyAuth__AllowStubPayments` staat in de blueprint op `true` (zelfde als huidige prod-demo). |
| Signing keys / `VerificationCodes__Pepper` | Altijd nieuw. Gedeelde keys maken sessies/OTP tussen prod en acceptatie inwisselbaar. |

### Testdata: leeg (seed) of kopie van prod

**Lege DB (standaard):** api seedt mockdata bij eerste start (AllowDevelopmentAuth), net als de publieke demo.

**Kopie van productie** (alleen als je UAT met echte inhoud nodig hebt):

1. Prod: `lobsy-db` → **Recovery / Backups** of `pg_dump` met de **External Database URL**.
2. Restore in `lobsy-db-acceptatie` (`pg_restore --no-owner --no-acl`).
3. Schakel Resend uit of gebruik een veilige From, tot je zeker weet dat er geen mails naar echte adressen gaan.
4. Deel nooit de prod connection string met acceptatie-services.

```bash
pg_dump -Fc -d "$PROD_EXTERNAL_URL" -f lobsy-prod.dump
pg_restore -d "$ACCEPTATIE_EXTERNAL_URL" -v --no-owner --no-acl lobsy-prod.dump
```

### Gelijk houden met productie

- **Code:** merge `main` → `Acceptatie` (of cherry-pick de release). Push naar `Acceptatie` deployst automatisch `lobsy-*-acceptatie`.
- **Infra:** wijzigingen in `render.yaml` (prod) spiegelen in `render.acceptatie.yaml` (andere namen).
- Na validatie: merge `Acceptatie` → `main` zodat de productie-Blueprint opnieuw deployt.

### Custom domain (optioneel)

1. DNS: `acceptatie.lobsy.nl` CNAME naar de `onrender.com` van `lobsy-web-acceptatie`.
2. Custom domain koppelen op die web-service.
3. `PublicWebBaseUrl` + CORS op die origin zetten.

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

## API deploy “Timed Out” terwijl logs “Now listening” tonen

Render markeert de deploy pas live als `healthCheckPath` (`/health`) herhaaldelijk **2xx/3xx** teruggeeft (max. ~15 min). Als de API wél start maar de check faalt (vaak door `AllowedHosts` 400, of `UseHttpsRedirection` die interne probes naar `https://lobsy.nl/health` stuurt), zie je:

- `Application started` / `Now listening on: http://0.0.0.0:10000`
- daarna `==> Timed Out` en `Detected service running on port 10000`

Mitigatie in repo: API `AllowedHosts=*`, geen HTTPS-redirect in Production, seed via background hosted service (luistert meteen), `/health` anonymous.

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
| `plan: starter` op api + web | Always-on; geen 15-min spin-down. Eén web-instance = geen sticky sessions voor Blazor/SignalR |

| `plan: basic-256mb` op DB | Blijvende Postgres (geen free 30-dagen expiry) |
| Alles `frankfurt` | Zelfde private network voor Postgres |
| `RENDER_EXTERNAL_URL` | Stabiele cross-service HTTP (ook op free bruikbaar) |
| `ConnectionStrings__JobsyDb` op **web én api** | Data Protection-keys in Postgres (antiforgery/auth cookies na redeploy) |
| `JobsyAuth__AllowDevelopmentAuth` | Demo-logins zonder Entra (niet voor echte productie) |

## Database backups (productie)

Render **Basic Postgres** (`jobsy-db`) maakt dagelijkse automatische backups (zie Dashboard → `jobsy-db` → **Backups**). Voor echte productie:

1. Bevestig in het Dashboard dat daily backups aan staan en noteer de retentie.
2. Plan minstens één restore-drill (nieuwe DB vanuit backup → connection string tijdelijk op staging).
3. Voor strengere RPO: upgrade naar een plan met Point-in-Time Recovery (PITR) en/of periodieke `pg_dump` naar offsite storage.
4. Documenteer RPO/RTO en wie restore mag uitvoeren in jullie ops-runbook.

## Transactionele e-mail (Resend) + SPF/DKIM

Lobsy stuurt alle platformmails via **Resend** (`POST https://api.resend.com/emails`). SMTP is alleen fallback.

### Configureren (kies één)

**A. Render / omgeving (aanbevolen voor productie)**

Zet op `jobsy-api`:

| Env var | Voorbeeld |
|---------|-----------|
| `Mail__ResendApiKey` | `re_…` (of `RESEND_API_KEY`) |
| `Mail__FromAddress` | `Lobsy <noreply@lobsy.nl>` (of `RESEND_FROM`) |

**B. Admin UI**

Admin → Integraties → **Mail (Resend)** → plak API-key + From → Opslaan → **Stuur testmail**.

DB-credentials hebben voorrang; env vult lege velden.

**Secrets wissen (Admin):** wist DB-keys én schakelt env-fill uit, zodat mail echt stopt (ook als Render-env nog gezet is). Herstel met nieuwe Admin-keys, of knop **Omgeving opnieuw gebruiken**. Alleen env wissen op Render zonder die knop laat mail uitgeschakeld tot je env opnieuw activeert of keys plakt.

Resend is pas operationeel als **API-key én From** beide gezet zijn (DB of env).

### DNS

1. Voeg het verzenddomein toe in Resend (bijv. `lobsy.nl`) en verifieer DNS.
2. Zet de door Resend aangeleverde **SPF** en **DKIM** records; start **DMARC** met `p=none` en verhoog later.
3. Gebruik From op het geverifieerde domein (niet langdurig `onboarding@resend.dev`).
4. Mislukte sends landen in PlatformLogs (e-mail geredacteerd).

## Sentry & webhook-ops

1. Maak een Sentry project en zet `Sentry__Dsn` op `jobsy-api` én `jobsy-web`.
2. Mollie webhook-fouten geven **503** (Mollie retries) en schrijven PlatformLog categorie `MollieWebhook`.
3. `TokenCheckoutReconcileHostedService` herstelt betaalde checkouts zonder credit/factuur (idempotent).
4. Optioneel: zet `VerificationCodes__Pepper` op een lange random string per omgeving.

## Echte productie vs publieke demo

| Flag | Demo (Render blueprint) | Echte productie |
|------|-------------------------|-----------------|
| `JobsyAuth__AllowDevelopmentAuth` | `true` | `false` + Entra/Google |
| `JobsyAuth__AllowStubPayments` | `true` | `false` + live Mollie |
| `Swagger__Enabled` | `false` | `false` (of tijdelijk `true` voor partners) |
| `Seed:Enabled` | via AllowDevelopmentAuth | `false` |
