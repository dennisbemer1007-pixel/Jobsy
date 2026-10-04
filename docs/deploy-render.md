# Lobsy op Render (always-on)

Blueprint: [`render.yaml`](../render.yaml). Project **Lobsy**, twee omgevingen.
**Release-pad (Acceptatie eerst):** zie [`release-flow.md`](release-flow.md).

| Environment | Git-branch | Services |
|-------------|------------|----------|
| **Production** | `main` | `jobsy-api`, `jobsy-web` |
| **Acceptatie** | `acceptatie` | `lobsy-acc-api`, `lobsy-acc-web` |

| Environment | Resources | Publieke URL |
|-------------|-----------|----------------|
| **Production** | `jobsy-api`, `jobsy-web`, `jobsy-db` | `https://lobsy.nl` |
| **Acceptatie** | `lobsy-acc-api`, `lobsy-acc-web`, `lobsy-acc-db` | `https://lobsy-acc-web.onrender.com` (Render-subdomein) |

Acceptatie heeft **eigen** Postgres en **eigen** secrets. Die omgeving mag nooit de productiedatabase gebruiken.

## Zoekmachines op Acceptatie

Acceptatie en productie draaien allebei met `ASPNETCORE_ENVIRONMENT=Production`. Daarom staat noindex **niet** op die vlag.

Op **alleen** `lobsy-acc-web` staat `Seo__NoIndex=true` (`Seo:NoIndex` in de app). Dan krijgt elke response `X-Robots-Tag: noindex, nofollow`, elke pagina een meta-robots `noindex, nofollow`, `robots.txt` zegt `Disallow: /` en er is geen sitemap-link. De productieservice `jobsy-web` heeft deze variabele niet en blijft indexeerbaar. Zet hem niet op productie.

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
   - Login via **e-mail + wachtwoord** (Acceptatie seedt lokale demo-accounts; `POST /account/demo-login` is 404 omdat `ASPNETCORE_ENVIRONMENT=Production`)
6. Optioneel: Acceptatie → **•••** → **Block cross-environment connections** (acc kan dan niet via het private netwerk bij Production).

Mail op Acceptatie blijft leeg tot je `Mail__ResendApiKey` / `Mail__FromAddress` in het Dashboard zet. Laat dat zo als je geen echte mails vanuit acc wilt.

## Security (production)

De Blueprint zet `JobsyAuth__AllowDevelopmentAuth=false` op **alle** services (Production én Acceptatie). Daarnaast:

- `POST /account/demo-login` geeft **altijd 404** in Production (harde environment-check), ongeacht config.
- DemoUsers staan alleen in `appsettings.Development.json` (niet in `appsettings.json`).
- `Seed__PurgeDemoData` / `DemoDataPurge` bestaan niet meer — geen startup-wipe van users/bedrijven.
- Mollie `test_…` keys worden buiten Development geweigerd (fail closed); Production vereist `live_…`.
- OAuth client-secrets vereisen een aparte `JobsyAuth__ExternalProvisionSecret`.
- Production custom domain: `PublicWebBaseUrl=https://lobsy.nl` + CORS voor `lobsy.nl` / `www.lobsy.nl`.
- Acceptatie gebruikt `acceptatie.lobsy.nl` / het `onrender.com`-subdomein (geen apex `lobsy.nl` in CORS).
- De admin-badge (Lokaal / Acceptatie / Productie) leest op de **web**-service `Lobsy__DeploymentEnvironment` (`Acceptatie` of `Production`). `PublicWebBaseUrl` staat op de API; zonder die web-variabele viel de badge terug op Lokaal. Optioneel blijft `Deployment__Label` een override.

- `JobsyAuth__DevelopmentAuthSecret` wordt per environment gegenereerd op de API en gedeeld met de web-service van **diezelfde** environment.
- `JobsyAuth__LocalSessionSigningKey` wordt apart gegenereerd en gedeeld voor HMAC-sessietokens.
- `JobsyAuth__ExternalProvisionSecret` wordt apart gegenereerd en gedeeld met web voor OAuth credential-provisioning.
- **ES256 JWT PEMs** (`JobsyAuth__Jwt__PublicKeyPem` op API, `JobsyAuth__Jwt__PrivateKeyPem` op Web) staan in de Blueprint als `sync: false`. Zonder Dashboard-waarde start de app met een gelogde **Development bootstrap-pair** (Critical in logs) zodat Acceptatie/Production niet crashen. Zet zo snel mogelijk een **eigen** ES256-paar per environment (openssl / `JobsyAccessToken.GenerateDevelopmentKeyPair`), zelfde private op Web en public op API. Production-PEMs mogen niet gelijk zijn aan Acceptatie.
- `CLOUDFLARE_ORIGIN_SECRET` is ook `sync: false`. Leeg = geen origin-header-handhaving (Critical-log); gezet = Transform Rule verplicht (behalve `/health` op de API).

Na Blueprint sync: controleer per environment dat API en web dezelfde `JobsyAuth__DevelopmentAuthSecret`, `JobsyAuth__LocalSessionSigningKey` én `JobsyAuth__ExternalProvisionSecret` hebben. Production-secrets mogen **niet** gelijk zijn aan Acceptatie.

### Named admin aanmaken (Production)

Er is geen `admin@jobsy.local` keep-list meer. Maak een echte admin zo:

1. Registreer of log in via Entra/Google (of local-login) met je eigen e-mailadres tot er een `Users`-rij bestaat.
2. Open Render → `jobsy-db` → **Shell** (of `psql` met de connection string).
3. Promoteer die user (Admin = enum `5`):

```sql
UPDATE "Users"
SET "Role" = 5
WHERE lower("Email") = lower('jij@jouwdomein.nl');
```

4. Log opnieuw in. Verwijder of deactiveer daarna eventuele oude `admin@jobsy.local` / `@jobsy.local` demo-accounts.

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
   - `JobsyAuth__AllowDevelopmentAuth=false`, `JobsyAuth__AllowStubPayments=false`, geen `Seed__PurgeDemoData`
   - Idealiter: `JobsyAuth__Jwt__PublicKeyPem` (API) + `JobsyAuth__Jwt__PrivateKeyPem` (Web) gezet; anders Critical bootstrap-log en Development-PEMs
   - Idealiter: `CLOUDFLARE_ORIGIN_SECRET` gezet op API én Web (zelfde waarde) + Transform Rule
   - Production API-logs: **geen** “Operational wipe” / purge; alleen migrate (+ geen seed tenzij `Seed__Enabled`)
   - Acceptatie API-logs: `Seed completed` / `Seeding Jobsy mock data` (geen wipe)
   - `jobsy-api` URL + `/health` → OK
5. Production: login met je named admin (zie hierboven). Acceptatie: local-login met geseede accounts.

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

- Production: `https://lobsy.nl` of klik **`jobsy-web`** → link bovenaan. Login met Entra/Google of je named admin (geen demo one-click; geen gedeeld demo-wachtwoord in deze docs).
- Acceptatie: klik **`lobsy-acc-web`** → `https://acceptatie.lobsy.nl` of `https://lobsy-acc-web.onrender.com`. Gebruik e-mail + wachtwoord van geseede accounts (one-click demo-login is uit in Production).
- API check: **`jobsy-api`** of **`lobsy-acc-api`** URL + `/health`

Services blijven draaien; geen cold start na idle.

Eerst Acceptatie, daarna Production: zet op `jobsy-api` en `jobsy-web` auto-deploy **uit** (alleen Manual Deploy). Laat `lobsy-acc-*` auto-deployen vanaf branch **`acceptatie`** (niet `main` — zie `render.yaml`).

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

## Runtime (.NET 10)

API en Web bouwen via Docker (`Jobsy.Api/Dockerfile`, `Jobsy.Web/Dockerfile`) op
`mcr.microsoft.com/dotnet/sdk:10.0` / `aspnet:10.0` (Ubuntu “noble”). `render.yaml`
blijft `runtime: docker` — geen Blueprint-wijziging nodig voor de LTS-bump.
Eerste deploy na merge haalt nieuwe base images (langere build). De Data Protection
key ring blijft compatibel over 9 → 10.

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
| `JobsyAuth__AllowDevelopmentAuth` | Altijd `false` op Render (demo-login is sowieso 404 in Production) |
| `lobsy-acc-*` namen | Render vereist unieke servicenamen over de hele workspace |

## Database backups (productie)

Render **Basic Postgres** (`jobsy-db`) maakt dagelijkse automatische backups (zie Dashboard → `jobsy-db` → **Backups**). Voor echte productie:

1. Bevestig in het Dashboard dat daily backups aan staan en noteer de retentie.
2. Plan minstens één restore-drill (nieuwe DB vanuit backup → connection string tijdelijk op Acceptatie).
3. Voor strengere RPO: upgrade naar een plan met Point-in-Time Recovery (PITR) en/of periodieke `pg_dump` naar offsite storage.
4. Documenteer RPO/RTO en wie restore mag uitvoeren in jullie ops-runbook.

## Transactionele e-mail (Resend) + SPF/DKIM

See also [email-deliverability.md](email-deliverability.md) for the Dennis checklist (DNS, DMARC, tracking off, support inbox, legal footer).

Lobsy stuurt alle platformmails via **Resend** (`POST https://api.resend.com/emails`). SMTP is alleen fallback.

### Configureren (kies één)

**A. Render / omgeving (aanbevolen voor productie)**

Zet op `jobsy-api`:

| Env var | Voorbeeld |
|---------|-----------|
| `Mail__ResendApiKey` | `re_…` (of `RESEND_API_KEY`) |
| `Mail__FromAddress` | `Lobsy <hallo@mail.lobsy.nl>` (or `RESEND_FROM`) |
| `Mail__ReplyTo` | `support@lobsy.nl` |
| `Mail__SupportAddress` | `support@lobsy.nl` |
| `Mail__LegalName` / `Mail__LegalAddress` / `Mail__KvkNumber` | Footer legal line (address + KvK still pending from Dennis) |

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

## AI provider (OpenAI or Mistral)

`Ai:Provider` is `OpenAI` or `Mistral`. The code default is OpenAI, so production stays on OpenAI until you set the variables. The key is not stored in the admin screen.

| Env var | Where | Notes |
|---------|--------|--------|
| `Ai__Provider` | Acceptatie API **and** web | `Mistral` or leave unset (`OpenAI`). `sync: false` in the blueprint. |
| `Mistral__ApiKey` | Acceptatie API **and** web | API sends it. Web only checks that it is set, so `/privacy` matches the calls. Alias: `MISTRAL_API_KEY`. |

Do not set `Mistral__BaseUrl`. The default is the EU endpoint `https://api.eu.mistral.ai/v1/` (inference in the EU/EFTA, about 1.1× list price). The global host `https://api.mistral.ai` does not promise where inference runs, and the privacy page then drops the EU claim. Default model is `mistral-small-latest`. Without a key, calls stay on OpenAI and the privacy page keeps the OpenAI row. Account, billing and API-key metadata at Mistral can still be handled outside the EU.

Production (`jobsy-api` / `jobsy-web`) does not list these keys. Do not set `Ai__Provider=Mistral` there until you choose to switch live.

## Sentry & webhook-ops

1. Maak een Sentry project en zet `Sentry__Dsn` op API én web (Production en eventueel Acceptatie).
2. Mollie webhook-fouten geven **503** (Mollie retries) en schrijven PlatformLog categorie `MollieWebhook`.
3. `TokenCheckoutReconcileHostedService` herstelt betaalde checkouts zonder credit/factuur (idempotent).
4. Optioneel: zet `VerificationCodes__Pepper` op een lange random string per omgeving.

## Echte productie vs Acceptatie

| Flag | Acceptatie (`lobsy-acc-*`) | Production (`jobsy-*`) |
|------|----------------------------|------------------------|
| `JobsyAuth__AllowDevelopmentAuth` | `false` | `false` + Entra/Google |
| `JobsyAuth__AllowStubPayments` | `true` (alleen acc) | `false` + live Mollie (`live_…` key) |
| `Swagger__Enabled` | `false` | `false` (of tijdelijk `true` voor partners) |
| `Seed:Enabled` | `true` | `false` |
| `Lobsy__DeploymentEnvironment` | `Acceptatie` | `Production` |
| `TestAccounts__Enabled` | Dashboard (`sync: false`) | `false` |
| DemoDataPurge / `Seed:PurgeDemoData` | verwijderd | verwijderd |

## Testaccounts (alleen acceptatie)

CLI inside the API binary (no HTTP endpoint):

```bash
cd /app && dotnet Jobsy.Api.dll test-accounts seed --dry-run
cd /app && dotnet Jobsy.Api.dll test-accounts seed
cd /app && dotnet Jobsy.Api.dll test-accounts cleanup
cd /app && dotnet Jobsy.Api.dll test-accounts cleanup --execute --expect-users <n>
cd /app && dotnet Jobsy.Api.dll test-accounts status
```

**Dashboard (Acceptatie → `lobsy-acc-api`):** set `TestAccounts__Enabled=true` and one secret per role (`TestAccounts__Password__Candidate`, `CandidateNew`, `BranchManager`, `EnterpriseManager`, `RegionalManager`, `Intermediary`, `SalesManager`, `Admin`, `Ambassadeur`, and when needed `Teacher` / `SchoolAdmin`). Passwords never go in git. On `lobsy-acc-web` set `Lobsy__DeploymentEnvironment=Acceptatie` and `TestAccounts__Enabled=true` (no passwords). Production keeps `Lobsy__DeploymentEnvironment=Production` and `TestAccounts__Enabled=false`.

Hard guard: Acceptatie marker + switch + `lobsy-acc-*` service name + allowlisted public host + database name `lobsy` + stub payments / no `live_` Mollie + non-Development host. Never run this CLI against production.
