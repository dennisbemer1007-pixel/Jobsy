# Lobsy op Render (always-on)

Blueprint: [`render.yaml`](../render.yaml). Project **Lobsy**, twee omgevingen:

| Environment | Resources | Publieke URL |
|-------------|-----------|----------------|
| **Production** | `jobsy-api`, `jobsy-web`, `jobsy-db` | `https://lobsy.nl` |
| **Acceptatie** | `lobsy-acc-api`, `lobsy-acc-web`, `lobsy-acc-db` | `https://lobsy-acc-web.onrender.com` (Render-subdomein) |

Acceptatie heeft **eigen** Postgres en **eigen** secrets. Die omgeving mag nooit de productiedatabase gebruiken.

Namen verschillen per environment omdat Render servicenamen workspace-breed uniek houdt. `fromDatabase` / `fromService` blijven binnen dezelfde environment.

**Niet hernoemen in `render.yaml`:** een andere Production-naam maakt een *nieuwe* API/web/DB aan en laat de bestaande `jobsy-*` (met de echte data) staan. Projectnaam **Lobsy** en Acceptatie-prefix `lobsy-acc-*` zijn genoeg voor de Lobsy-branding.

## Dubbele Production-stack opruimen

Als Production zowel `jobsy-*` als `lobsy-api` / `lobsy-web` / `lobsy-db` toont:

1. Laat **`jobsy-db`** met rust (de database van ~1 maand is de echte productiedata).
2. Laat **`jobsy-api`** en **`jobsy-web`** met rust (`lobsy.nl` hangt hieraan).
3. Laat Acceptatie (`lobsy-acc-*`) met rust.
4. Verwijder **alleen** de nieuwe Production-kopie: `lobsy-api`, `lobsy-web`, `lobsy-db` (aangemaakt bij de hernoem-sync; lege DB van een paar minuten).
5. Sync de Blueprint pas nádat Production in `render.yaml` weer `jobsy-*` heet — anders maakt Render de lege `lobsy-*` stack opnieuw.

## Plannen (kosten)

Blueprint gebruikt **betaalde instance types**:

| Resource | Plan | Effect |
|----------|------|--------|
| `jobsy-api` / `jobsy-web` (en `lobsy-acc-*`) | **Starter** (~$7/mo elk) | Geen spin-down na idle |
| `jobsy-db` / `lobsy-acc-db` | **Basic-256mb** | Geen 30-dagen free-expiry |

Een workspace-betaalplan of creditcard alleen is **niet** genoeg: Free instances blijven slapen. Het instance-type per service telt.

Indicatie: Production ~$14/mo web + Postgres. Acceptatie is **nog eens** hetzelfde. Zie [Render pricing](https://render.com/pricing).

## Acceptatie aanzetten

Acceptatie in het dashboard is alleen een lege map totdat de Blueprint de drie `lobsy-acc-*` resources aanmaakt.

1. Merge deze `render.yaml` naar `main` (of wacht tot de PR gemerged is).
2. Render Dashboard → Blueprint van deze repo → **Manual sync**.
3. Controleer het sync-plan:
   - **Aanmaken:** `lobsy-acc-db`, `lobsy-acc-api`, `lobsy-acc-web` in environment **Acceptatie**
   - **Niet verwijderen:** `jobsy-api`, `jobsy-web`, `jobsy-db`
   - Klik **niet** op **Move existing services** in het lege Acceptatie-blok (dat verhuist Production).
4. Bevestig. Wacht tot de drie acc-resources groen zijn (eerste API-start seedt de lege acc-DB).
5. Check:
   - `https://lobsy-acc-api.onrender.com/health` → OK
   - `https://lobsy-acc-web.onrender.com` opent de site
   - Login: `kandidaat@jobsy.local` / `Jobsy123!`
6. Optioneel: Acceptatie → **•••** → **Block cross-environment connections** (acc kan dan niet via het private netwerk bij Production).

Mail op Acceptatie blijft leeg tot je `Mail__ResendApiKey` / `Mail__FromAddress` in het Dashboard zet. Laat dat zo als je geen echte mails vanuit acc wilt.

## Security (demo)

De Blueprint houdt `JobsyAuth__AllowDevelopmentAuth=true` zodat demo-login via de Web UI werkt, maar:

- Buiten Development accepteert header-auth `@jobsy.local` demo-accounts met de gedeelde secret; echte registratie-/OAuth-gebruikers sturen ook `X-Jobsy-Local-Session` (HMAC met `LocalSessionSigningKey`, vernieuwd bij session-activity).
- OAuth client-secrets vereisen een aparte `JobsyAuth__ExternalProvisionSecret` (niet dezelfde DevelopmentAuthSecret; Web gebruikt geen DevelopmentAuthSecret-fallback meer).
- Production custom domain: `PublicWebBaseUrl=https://lobsy.nl` + CORS voor `lobsy.nl` / `www.lobsy.nl`.
- Acceptatie gebruikt het `onrender.com`-subdomein van `lobsy-acc-web` (geen `lobsy.nl` in CORS).

- `JobsyAuth__DevelopmentAuthSecret` wordt per environment gegenereerd op de API en gedeeld met de web-service van **diezelfde** environment.
- `JobsyAuth__LocalSessionSigningKey` wordt apart gegenereerd en gedeeld voor HMAC-sessietokens van niet-demo gebruikers.
- `JobsyAuth__ExternalProvisionSecret` wordt apart gegenereerd en gedeeld met web voor OAuth credential-provisioning.
- `JobsyFeatures__ExposeRegistrationActivationLinks=false` (geen activatie-URL in API-responses).

Na Blueprint sync: controleer per environment dat API en web dezelfde `JobsyAuth__DevelopmentAuthSecret`, `JobsyAuth__LocalSessionSigningKey` én `JobsyAuth__ExternalProvisionSecret` hebben. Production-secrets mogen **niet** gelijk zijn aan Acceptatie.

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

## Bestaande deploy upgraden / her-syncen

1. Push deze `render.yaml` naar `main`.
2. Blueprint-pagina → **Manual sync** (of wacht op auto-sync).
3. Bevestig instance types (Starter / Basic-256mb) in het Dashboard.
4. Controleer na sync:
   - `jobsy-api` → **Environment**: `ConnectionStrings__JobsyDb` is een echte `postgres://` / `postgresql://` URL
   - Production API-logs: `Operational wipe finished` of `nothing to delete` (geen nieuwe Westland-seed). Daarna: `already marked`.
   - Acceptatie API-logs: `Seed completed` / `Seeding Jobsy mock data` (geen wipe)
   - `jobsy-api` URL + `/health` → OK
5. `https://lobsy.nl` toont geen bedrijven/vacatures meer (alleen `admin@jobsy.local`); `https://acceptatie.lobsy.nl` blijft geseeded.

Als de connection string leeg is of corrupt (vaak na DB-upgrade), zie hieronder.

## Als sync faalt of API “Failed” is (regio-mismatch)

Eerdere deploys hadden DB in **Oregon** en web in **Frankfurt**. Regio’s zijn **niet** te wijzigen.

1. Verwijder in het Dashboard (Allow/confirm alles) **alleen** de kapotte resources van **die** environment, bijvoorbeeld Production:
   - `jobsy-api`
   - `jobsy-web`
   - `jobsy-db`
2. Blueprint-pagina → **Manual sync**
3. Wacht tot alle drie opnieuw groen zijn (zelfde regio: **Frankfurt**)
4. API herseedt mockdata bij eerste start op een lege DB

Verwijder Acceptatie-resources niet samen met Production.

## Gebruiken

- Production: `https://lobsy.nl` of klik **`jobsy-web`** → link bovenaan. Na de operational wipe: login `admin@jobsy.local` / `Jobsy123!` (geen vacatures/bedrijven tot je ze opnieuw aanmaakt).
- Acceptatie: klik **`lobsy-acc-web`** → `https://acceptatie.lobsy.nl` of `https://lobsy-acc-web.onrender.com`. Demo-login: `kandidaat@jobsy.local` / `Jobsy123!`
- API check: **`jobsy-api`** of **`lobsy-acc-api`** URL + `/health`

Services blijven draaien; geen cold start na idle.

Eerst Acceptatie, daarna Production: zet op `jobsy-api` en `jobsy-web` auto-deploy **uit** (alleen Manual Deploy). Laat `lobsy-acc-*` auto-deployen vanaf `main`.

## Antiforgery / “key was not found in the key ring”

Na een redeploy kan Render kort dit loggen als je browser nog oude cookies heeft:

`The antiforgery token could not be decrypted` / `The key {...} was not found in the key ring`

**Nu meteen:** site-cookies voor `*.onrender.com` wissen (of privévenster) en opnieuw laden.

**Structureel:** web bewaart Data Protection-keys in Postgres (`ConnectionStrings__JobsyDb`). Zorg dat die env-var gezet is (Blueprint zet dit via de DB van dezelfde environment). Zonder DB-keys blijven cookies na elke deploy ongeldig.

## API deploy Failed: `PendingModelChangesWarning`

EF Core 9 faalt `MigrateAsync` als `JobsyDbContext` afwijkt van `JobsyDbContextModelSnapshot` (hand-geschreven migratie zonder snapshot-update). De API-host stopt dan (`BackgroundServiceExceptionBehavior=StopHost`) en Render markeert de deploy als Failed — vaak met korte duur (~25–90s) terwijl `jobsy-web` wél live blijft.

**Check in logs:** `The model for context 'JobsyDbContext' has pending changes`.

**Fix:** snapshot bijwerken (`dotnet ef migrations add …` of entity in snapshot zetten) en `dotnet ef migrations has-pending-model-changes` groen houden. Regressietest: `EfModelSnapshotTests`.

## API deploy “Timed Out” terwijl logs “Now listening” tonen

Render markeert de deploy pas live als `healthCheckPath` (`/health`) herhaaldelijk **2xx/3xx** teruggeeft (max. ~15 min). Als de API wél start maar de check faalt (vaak door `AllowedHosts` 400, of `UseHttpsRedirection` die interne probes naar `https://lobsy.nl/health` stuurt), zie je:

- `Application started` / `Now listening on: http://0.0.0.0:10000`
- daarna `==> Timed Out` en `Detected service running on port 10000`

Mitigatie in repo: API `AllowedHosts=*`, geen HTTPS-redirect in Production, seed via background hosted service (luistert meteen), `/health` anonymous.

## Crash: inotify / FileSystemWatcher limit

Als de API crasht met:
`The configured user limit (128) on the number of inotify instances has been reached`

dan heeft .NET te veel file-watchers (config reload). De Dockerfiles en Blueprint zetten
`DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false`. Na push: Manual Deploy van de API (en eventueel web).

## Connection string fout (na DB-upgrade)

Als de API crasht met:
`Format of the initialization string does not conform to specification starting at index 0`

dan is `ConnectionStrings__JobsyDb` leeg of geen echte Postgres-string — mockdata en de site blijven dan leeg/kapot.

1. Open de Postgres van **die** environment (`jobsy-db` of `lobsy-acc-db`) → **Info** → kopieer **Internal Database URL**  
   (begint met `postgres://` of `postgresql://`)
2. Open de API én web van dezelfde environment → **Environment**
3. Zet / herstel key **`ConnectionStrings__JobsyDb`** op die volledige URL (geen aanhalingstekens)
4. **Save** → Manual Deploy van de API (web daarna desnoods ook)
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
| `lobsy-acc-*` namen | Render vereist unieke servicenamen over de hele workspace |

## Database backups (productie)

Render **Basic Postgres** (`jobsy-db`) maakt dagelijkse automatische backups (zie Dashboard → `jobsy-db` → **Backups**). Voor echte productie:

1. Bevestig in het Dashboard dat daily backups aan staan en noteer de retentie.
2. Plan minstens één restore-drill (nieuwe DB vanuit backup → connection string tijdelijk op Acceptatie).
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

## KVK Handelsregister

Zonder API-key blijft de **demo-stub** (vaste testnummers zoals `11223344`). Met key gaat registratie live naar KVK.

**A. Admin UI (aanbevolen)**

Admin → Integraties → **KVK** → plak API-key → Base URL:

| Omgeving | Base URL |
|----------|----------|
| Productie (echte bedrijven) | leeg laten, of `https://api.kvk.nl/api/` |
| KVK-testomgeving | `https://api.kvk.nl/test/api/` |

Niet `https://developers.kvk.nl/` of de Zoeken-URL (`.../v2/zoeken`) plakken. **Opslaan** → **Test verbinding**. 401/403 = key past niet bij die Base URL (test-key vs productie).

**B. Render / omgeving**

Zet op de **API**-service (`jobsy-api` / `lobsy-acc-api`):

| Env var | Voorbeeld |
|---------|-----------|
| `Kvk__ApiKey` | key uit Mijn API-keys (of `KVK_API_KEY`) |
| `Kvk__BaseUrl` | leeg of `https://api.kvk.nl/api/` |

Keys uit Integraties gaan voor; env vult lege velden. Na deploy: Integraties → Test verbinding.

Als KVK IP-whitelisting aan heeft staan in het Developer Portal, voeg de uitgaande IP’s van Render toe of zet die restrictie uit — anders weigert KVK de calls (dat is geen stub meer).

## Sentry & webhook-ops

1. Maak een Sentry project en zet `Sentry__Dsn` op API én web (Production en eventueel Acceptatie).
2. Mollie webhook-fouten geven **503** (Mollie retries) en schrijven PlatformLog categorie `MollieWebhook`.
3. `TokenCheckoutReconcileHostedService` herstelt betaalde checkouts zonder credit/factuur (idempotent).
4. Optioneel: zet `VerificationCodes__Pepper` op een lange random string per omgeving.

## Echte productie vs publieke demo

| Flag | Demo (Render blueprint) | Echte productie |
|------|-------------------------|-----------------|
| `JobsyAuth__AllowDevelopmentAuth` | `true` | `false` + Entra/Google |
| `JobsyAuth__AllowStubPayments` | `true` | `false` + live Mollie |
| `Swagger__Enabled` | `false` | `false` (of tijdelijk `true` voor partners) |
| `Seed:Enabled` | `true` op Acceptatie | `false` |
| `Seed:PurgeDemoData` | uit | `true` (eenmalige wipe: alle bedrijven/vacatures/kandidaten; houdt `admin@jobsy.local`). Draait ook als `PublicWebBaseUrl` `https://lobsy.nl` is, zodat Blueprint-env-sync niet verplicht is. |
