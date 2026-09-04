# Lobsy op Render (always-on)

Blueprint: [`render.yaml`](../render.yaml). Project **Lobsy**, twee omgevingen:

| Environment | Resources | Publieke URL |
|-------------|-----------|----------------|
| **Production** | `lobsy-api`, `lobsy-web`, `lobsy-db` | `https://lobsy.nl` |
| **Acceptatie** | `lobsy-acc-api`, `lobsy-acc-web`, `lobsy-acc-db` | `https://lobsy-acc-web.onrender.com` (Render-subdomein) |

Acceptatie heeft **eigen** Postgres en **eigen** secrets. Die omgeving mag nooit de productiedatabase gebruiken.

Namen verschillen per environment omdat Render servicenamen workspace-breed uniek houdt. `fromDatabase` / `fromService` blijven binnen dezelfde environment.

## Plannen (kosten)

Blueprint gebruikt **betaalde instance types**:

| Resource | Plan | Effect |
|----------|------|--------|
| `lobsy-api` / `lobsy-web` (en `lobsy-acc-*`) | **Starter** (~$7/mo elk) | Geen spin-down na idle |
| `lobsy-db` / `lobsy-acc-db` | **Basic-256mb** | Geen 30-dagen free-expiry |

Een workspace-betaalplan of creditcard alleen is **niet** genoeg: Free instances blijven slapen. Het instance-type per service telt.

Indicatie: Production ~$14/mo web + Postgres. Acceptatie is **nog eens** hetzelfde. Zie [Render pricing](https://render.com/pricing).

## Acceptatie aanzetten

Acceptatie in het dashboard is alleen een lege map totdat de Blueprint de drie `lobsy-acc-*` resources aanmaakt.

1. Merge deze `render.yaml` naar `main` (of wacht tot de PR gemerged is).
2. Render Dashboard → Blueprint van deze repo → **Manual sync**.
3. Controleer het sync-plan:
   - **Aanmaken:** `lobsy-acc-db`, `lobsy-acc-api`, `lobsy-acc-web` in environment **Acceptatie**
   - **Niet verwijderen:** `lobsy-api`, `lobsy-web`, `lobsy-db`
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

## Eenmalig: code + Blueprint

1. Repo op GitHub: `dennisbemer1007-pixel/Jobsy` (branch `main` met `render.yaml`).
2. Account op [https://render.com/register](https://render.com/register) (GitHub-login) + betaalmethode.
3. Render Dashboard: **New** → **Blueprint** → repo **Jobsy** → Deploy.

## Bestaande deploy upgraden / her-syncen

1. Push deze `render.yaml` naar `main`.
2. Blueprint-pagina → **Manual sync** (of wacht op auto-sync).
3. Bevestig instance types (Starter / Basic-256mb) in het Dashboard.
4. Controleer na sync:
   - `lobsy-api` → **Environment**: `ConnectionStrings__JobsyDb` is een echte `postgres://` / `postgresql://` URL
   - `lobsy-api` Logs: `Seeding Jobsy mock data` of `Seed completed`
   - `lobsy-api` URL + `/health` → OK
5. Open `lobsy-web`; mockdata (Westland / Den Haag vacatures) hoort zichtbaar te zijn.

Als de connection string leeg is of corrupt (vaak na DB-upgrade), zie hieronder.

## Als sync faalt of API “Failed” is (regio-mismatch)

Eerdere deploys hadden DB in **Oregon** en web in **Frankfurt**. Regio’s zijn **niet** te wijzigen.

1. Verwijder in het Dashboard (Allow/confirm alles) **alleen** de kapotte resources van **die** environment, bijvoorbeeld Production:
   - `lobsy-api`
   - `lobsy-web`
   - `lobsy-db`
2. Blueprint-pagina → **Manual sync**
3. Wacht tot alle drie opnieuw groen zijn (zelfde regio: **Frankfurt**)
4. API herseedt mockdata bij eerste start op een lege DB

Verwijder Acceptatie-resources niet samen met Production.

## Gebruiken

- Production: `https://lobsy.nl` of klik **`lobsy-web`** → link bovenaan
- Acceptatie: klik **`lobsy-acc-web`** → `https://lobsy-acc-web.onrender.com`
- Login: `kandidaat@jobsy.local` / `Jobsy123!`
- API check: **`lobsy-api`** of **`lobsy-acc-api`** URL + `/health`

Services blijven draaien; geen cold start na idle.

Eerst Acceptatie, daarna Production: zet op `lobsy-api` en `lobsy-web` auto-deploy **uit** (alleen Manual Deploy). Laat `lobsy-acc-*` auto-deployen vanaf `main`.

## Antiforgery / “key was not found in the key ring”

Na een redeploy kan Render kort dit loggen als je browser nog oude cookies heeft:

`The antiforgery token could not be decrypted` / `The key {...} was not found in the key ring`

**Nu meteen:** site-cookies voor `*.onrender.com` wissen (of privévenster) en opnieuw laden.

**Structureel:** web bewaart Data Protection-keys in Postgres (`ConnectionStrings__JobsyDb`). Zorg dat die env-var gezet is (Blueprint zet dit via de DB van dezelfde environment). Zonder DB-keys blijven cookies na elke deploy ongeldig.

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

1. Open de Postgres van **die** environment (`lobsy-db` of `lobsy-acc-db`) → **Info** → kopieer **Internal Database URL**  
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

Render **Basic Postgres** (`lobsy-db`) maakt dagelijkse automatische backups (zie Dashboard → `lobsy-db` → **Backups**). Voor echte productie:

1. Bevestig in het Dashboard dat daily backups aan staan en noteer de retentie.
2. Plan minstens één restore-drill (nieuwe DB vanuit backup → connection string tijdelijk op Acceptatie).
3. Voor strengere RPO: upgrade naar een plan met Point-in-Time Recovery (PITR) en/of periodieke `pg_dump` naar offsite storage.
4. Documenteer RPO/RTO en wie restore mag uitvoeren in jullie ops-runbook.

## Transactionele e-mail (Resend) + SPF/DKIM

Lobsy stuurt alle platformmails via **Resend** (`POST https://api.resend.com/emails`). SMTP is alleen fallback.

### Configureren (kies één)

**A. Render / omgeving (aanbevolen voor productie)**

Zet op `lobsy-api`:

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
| `Seed:Enabled` | via AllowDevelopmentAuth | `false` |
