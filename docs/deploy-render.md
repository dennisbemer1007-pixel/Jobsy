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

Mail op Acceptatie blijft leeg tot je `Lettermint__ApiKey` (of als terugval `Mail__ResendApiKey`) en `Mail__FromAddress` in het Dashboard zet. Het patroon `Mail__AllowedRecipientPattern` staat al in de blueprint: alleen `test-*@lobsy.nl` krijgt mail, plus het extra adres dat je zelf zet. Laat de sleutels leeg als je geen echte mails vanuit acc wilt.

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
- `CLOUDFLARE_ORIGIN_SECRET` is ook `sync: false` (geen waarde in de repo). Leeg = geen origin-header-handhaving (Critical-log in Production). Gezet = zie [Cloudflare origin-secret aanzetten](#cloudflare-origin-secret-aanzetten).

Na Blueprint sync: controleer per environment dat API en web dezelfde `JobsyAuth__DevelopmentAuthSecret`, `JobsyAuth__LocalSessionSigningKey` én `JobsyAuth__ExternalProvisionSecret` hebben. Production-secrets mogen **niet** gelijk zijn aan Acceptatie.

## Cloudflare origin-secret aanzetten

`CLOUDFLARE_ORIGIN_SECRET` staat in `render.yaml` als `sync: false` op API én web (Production `jobsy-api` / `jobsy-web` en Acceptatie `lobsy-acc-api` / `lobsy-acc-web`). Er hoort **geen** waarde in git.

Zolang de variabele leeg is, verandert er niets: verkeer zonder header wordt doorgelaten en Production logt een Critical-regel dat handhaving uit staat.

Staat de variabele wél gezet, dan weigert de service elk verzoek zonder header `X-Jobsy-Origin-Secret` (403), behalve:

| Verkeer | Hoe het binnenkomt |
|---------|-------------------|
| Browser → web (en eventueel een geproxiede API-host) | Cloudflare Transform Rule zet de header |
| Web → API (`ApiBaseUrl`, het `onrender.com`-adres) | De web-service stuurt de header zelf mee op server-side calls. Het geheim gaat niet naar de browser en niet naar Nominatim/PDOK of een externe redirect (bijv. vacaturefoto) |
| Render health check | API `GET /health` en web `GET /healthz` blijven open |
| Mollie `POST /api/webhooks/mollie` en Cursor `POST /api/feedback/cursor-webhook` | Blijven open op het Render-adres (`PublicApiBaseUrl` = `RENDER_EXTERNAL_URL`). Die routes verifiëren zelf (Mollie-pull / HMAC) |

Direct `*.onrender.com` in de browser geeft daarna 403. Dat is de bedoeling.

**Externe vacature-API** (`/api/external/vacancies`) op `*.onrender.com` geeft ook 403. Partners krijgen het geheim niet. Zet eerst een Cloudflare-proxied hostnaam (bijv. `api.lobsy.nl` / een acc-host) met dezelfde Transform Rule, zet `PublicApiBaseUrl` daarop en mail die URL. Doe dat vóór je de externe koppeling nodig hebt nádat het geheim aan staat.

### Volgorde (per environment, nooit Production-waarde = Acceptatie)

1. Deploy deze code terwijl het geheim nog **leeg** is. Gedrag blijft het oude.
2. Genereer een lange random waarde (bijv. `openssl rand -base64 32`). Eén waarde voor API én web van **dezelfde** environment. Andere waarde voor de andere environment. Niet committen.
3. Cloudflare → Rules → Transform Rules → **Modify Request Header**, op alle verzoeken naar de geproxiede hostnamen (ook `/_blazor`, dat is het WebSocket-upgrade-verzoek):
   - Header: `X-Jobsy-Origin-Secret`
   - Waarde: de geheime string (statisch)
   - Production: `lobsy.nl` en `www.lobsy.nl`
   - Acceptatie: `acceptatie.lobsy.nl` (oranje wolk), anders blijft het acc-subdomein `onrender.com` 403 geven zodra het geheim aan staat
4. Zet `CLOUDFLARE_ORIGIN_SECRET` eerst op de **web**-service en wacht tot die deploy groen is (`/healthz`). De web-service stuurt de header daarna naar de API; de API eist hem nog niet.
5. Zet **daarna** dezelfde waarde op de **API**-service en wacht tot `/health` groen is.

Zet je de API eerder dan de web, dan weigert de API alle web→API-calls. Zet je de web eerder dan de Transform Rule, dan krijgen bezoekers 403.

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
   - `CLOUDFLARE_ORIGIN_SECRET` alleen ná de Cloudflare Transform Rule, eerst op web, daarna op API (zelfde waarde per environment) — zie hieronder
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

## Transactionele e-mail (Lettermint of Resend) + SPF/DKIM

See also [email-deliverability.md](email-deliverability.md) for the Dennis checklist (DNS, DMARC, tracking off, support inbox, legal footer).

`Mail__Provider` kiest de verzender.

- **Lettermint** (`POST https://api.lettermint.co/v1/send`, header `x-lettermint-token`) is een Nederlands bedrijf. De mail blijft in de EU. Dit pad is actief alleen als `Mail__Provider=Lettermint` én `Lettermint__ApiKey` gezet is.
- Zonder die sleutel gaat er geen mail via Resend. Lobsy logt één fout: Mail: niet ingesteld. Resend (`POST https://api.resend.com/emails`) geldt alleen als `Mail__Provider=Resend`.
- SMTP is alleen fallback. Open- en klikmeting sturen we niet mee.

Acceptatie zet `Mail__AllowedRecipientPattern` op `^test-[^@]+@lobsy\.nl$`. Andere adressen worden overgeslagen. Het log toont alleen een afgeschermd adres. `Mail__AllowedRecipientAddresses__0` is het extra adres van de beheerder. Productie laat het patroon leeg: daar gaat elke mail eruit.

Zet `Mail__Provider` en `Lettermint__ApiKey` ook op de **web**-service. De privacyzin leest dezelfde config. De web-service verstuurt geen mail; hij kijkt alleen of de sleutel er is.

### Configureren (kies één)

**A. Render / omgeving (aanbevolen voor productie)**

Zet op `jobsy-api` (en provider + Lettermint-sleutel ook op `jobsy-web`):

| Env var | Voorbeeld |
|---------|-----------|
| `Mail__Provider` | `Resend` of `Lettermint` |
| `Lettermint__ApiKey` | project-token (of `LETTERMINT_API_KEY`). Niet in git. |
| `Lettermint__BaseUrl` | leeg laten, of `https://api.lettermint.co/v1/` |
| `Mail__ResendApiKey` | `re_…` (of `RESEND_API_KEY`) |
| `Mail__FromAddress` | `Lobsy <hallo@mail.lobsy.nl>` (or `RESEND_FROM`) |
| `Mail__ReplyTo` | `support@lobsy.nl` |
| `Mail__SupportAddress` | `support@lobsy.nl` |
| `Mail__LegalName` / `Mail__LegalAddress` / `Mail__KvkNumber` | Footer legal line (address + KvK still pending from Dennis) |
| `Mail__AllowedRecipientPattern` | leeg in productie. Acceptatie: `^test-[^@]+@lobsy\.nl$` |
| `Mail__AllowedRecipientAddresses__0` | leeg, of het eigen adres van de beheerder |

**B. Admin UI**

Admin → Integraties → **Mail (Lettermint)** of **Mail (Resend)** → plak de Resend API-key + From als Resend de verzender is → Opslaan → **Stuur testmail**. De tegel toont de verzender die echt aan staat.

De Lettermint-sleutel staat niet in dit scherm. Die zet je alleen als env var. DB-credentials voor Resend hebben voorrang; env vult lege velden.

**Secrets wissen (Admin):** wist DB-keys én schakelt env-fill uit, zodat mail echt stopt (ook als Render-env nog gezet is). Herstel met nieuwe Admin-keys, of knop **Omgeving opnieuw gebruiken**. Alleen env wissen op Render zonder die knop laat mail uitgeschakeld tot je env opnieuw activeert of keys plakt.

Resend is pas operationeel als **API-key én From** beide gezet zijn (DB of env). Lettermint is operationeel als provider én sleutel gezet zijn.

### DNS

1. Voeg het verzenddomein toe bij de actieve verzender (Lettermint of Resend) en verifieer DNS.
2. Zet de **SPF**, **DKIM** en **DMARC** records die de verzender toont. Start DMARC met `p=none` en verhoog later.
3. Gebruik From op het geverifieerde domein (bij Resend niet langdurig `onboarding@resend.dev`).
4. Mislukte sends landen in PlatformLogs (e-mail afgeschermd). Er is geen Resend-webhook in deze code, dus ook geen Lettermint-bounce-webhook.

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

Do not set `Mistral__BaseUrl`. The default is the EU endpoint `https://api.eu.mistral.ai/v1/` (inference in the EU/EFTA, about 1.1× list price). The privacy page treats Mistral as in the EU only when the host of `Mistral__BaseUrl` is `api.eu.mistral.ai`. Any other host, including `https://api.mistral.ai`, does not promise where inference runs, and the privacy page then drops the EU claim. The page reads that host from config. Default model is `mistral-small-latest`. Optional per-feature overrides, still on that model when empty, are read by the API only: `Mistral__Models__Story` (Jouw verhaal), `Mistral__Models__CareerReport` (loopbaanrapport), `Mistral__Models__Compass` (beroepenkompas) and `Mistral__Models__Chat` (coach). The compass call also writes the career-report sentences: a set compass model wins, otherwise the career-report model, otherwise `Mistral:Model`. Leave the four unset to keep `mistral-small-latest`. Without a key, calls stay on OpenAI and the privacy page keeps the OpenAI row. Account, billing and API-key metadata at Mistral can still be handled outside the EU.

Quality calls (verhaal, coach, loopbaanplan, kompas, fit, oefengesprek) use `Mistral__Model`. Set that to `mistral-medium-latest` when you want the stronger model. Cheap calls (vertaling, CV uitlezen, vacaturecontrole) use `Mistral__SmallModel` when it is set, for example `mistral-small-latest`. Leave `Mistral__SmallModel` empty and those calls keep using `Mistral__Model`. If `Mistral__SmallModel` is empty, `Ai__SmallModel` is the next fallback, then `OpenAI__SmallModel`. On the OpenAI path the same split is `OpenAI__Model` plus `OpenAI__SmallModel` (or `Ai__SmallModel`). Admin → Integraties → OpenAI also has Model and Klein model; the tile’s klein model wins over the env small model, and a set small model still wins when the database model is the quality model. The OpenAI tile is hidden while `Ai__Provider=Mistral`, so on Acceptatie set `Mistral__Model` and `Mistral__SmallModel` on the API service.

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

## CV-virusscan (ClamAV, optioneel)

Standaard uit. Magic-byte checks op `POST /api/me/cv` draaien altijd. Knoppen en een EU-pad (zelf-gehoste ClamAV, geen Amerikaans scan-SaaS): [`cv-upload-malware-scan.md`](cv-upload-malware-scan.md). Zet de scan pas aan als clamd bereikbaar is; anders weigert Acceptatie/productie elke CV-upload (fail-closed).
