# Onderhoudsmodus (errors 05)

Drie lagen, van binnen naar buiten:

| Laag | Wie zet het aan | Wat de bezoeker krijgt | Health checks |
|---|---|---|---|
| **1. Schakelaar in Lobsy** | admin, `/admin/instellingen` → Onderhoudsmodus | `/status/503` in eigen taal, echte 503 + `Retry-After` | `/healthz` en `/health` blijven 200 |
| **2. Render maintenance mode** | Dennis, Render dashboard of `render.yaml` | Render's eigen pagina of `ops/maintenance/index.html`, status 503 | service is niet publiek bereikbaar |
| **3. Cloudflare custom error** | Dennis, Cloudflare dashboard (betaald plan) | `ops/maintenance/cloudflare-500.html` bij 5xx van de origin | n.v.t. |

Laag 1 is de normale weg en kost geen cent. Laag 2 en 3 zijn alleen nodig als de app zélf plat
ligt (deploy, crash, migratie die de app niet start). **Cursor raakt Cloudflare, Render-dashboards,
DNS en `render.yaml` nooit aan** — dit document is de klikinstructie voor Dennis.

---

## 1. Geplande onderhoud (de normale weg)

1. Ga naar `/admin/instellingen` → **Onderhoudsmodus**.
2. Vul optioneel **Verwacht klaar om** in (Amsterdamse tijd) en een interne notitie.
   De notitie is alleen voor admins en komt nooit op de publieke pagina.
3. Zet de schakelaar aan en bevestig ("Bezoekers zien vanaf nu de onderhoudspagina. Jij blijft erin.").
4. **Wacht 15 seconden.** De webserver pollt `api/site/status` elke 15 s, dus zo lang kan het duren
   voordat elke instance de knop ziet.
5. Doe de deploy / migratie / ingreep. Jij blijft als admin overal in en ziet een rode balk
   bovenaan: "Onderhoudsmodus staat AAN."
6. Controleer na afloop zelf een paar pagina's (jij ziet de echte site, niet de 503).
7. Zet de schakelaar uit. Dat gaat zonder bevestiging en is binnen 15 s overal weg.

Wat de bezoeker in die periode krijgt:

| Soort verzoek | Status | Headers | Inhoud |
|---|---|---|---|
| HTML-pagina (`/`, `/vacatures`, …) | 503 | `Retry-After`, `X-Robots-Tag: noindex`, `Cache-Control: no-store` | onderhoudspagina in eigen taal |
| API-call | 503 | `Retry-After` | ProblemDetails `{ "code": "maintenance" }` |
| `/healthz`, `/health` | 200 | — | ongewijzigd (Render health checks) |
| `/login`, `/account/*`, `/status/*`, statics, `/robots.txt` | normaal | — | ongewijzigd |
| admin (rol Admin op het auth-cookie) | normaal | — | hele app + rode balk |

`Retry-After` (besluit E8): seconden tot de verwachte eindtijd, geklemd op 60–3600. Geen of
verstreken eindtijd → 300.

---

## 2. Plan check Cloudflare (gecontroleerd 2026-10-01)

Dit is een **check, geen actie**. Lobsy staat achter Cloudflare, dat voor Render zit (zie de
Transform Rule met `X-Jobsy-Origin-Secret` in `render.yaml`). Welke edge-optie mogelijk is, hangt
af van het Cloudflare-plan.

### Wat de documentatie zegt

Gecontroleerd op **2026-10-01**:

- <https://developers.cloudflare.com/rules/custom-errors/> — beschikbaarheid per plan
- <https://developers.cloudflare.com/rules/custom-errors/edit-error-pages/> — pagina uploaden
- <https://developers.cloudflare.com/rules/custom-errors/reference/error-tokens/> — verplichte tokens
- <https://developers.cloudflare.com/fundamentals/reference/error-responses/> — standaardgedrag per plan

| | Free | Pro | Business | Enterprise |
|---|---|---|---|---|
| Custom Errors beschikbaar | nee | ja | ja | ja |
| Error Pages | nee | ja | ja | ja |
| Custom Error Rules | 0 | 25 | 50 | 300 |
| Origin Error Pages | nee | nee | nee | ja |

Twee dingen die je makkelijk mist:

1. **Error Pages werken niet bij status 500, 501, 503 en 505.** Letterlijk: *"Error Pages do not
   apply to responses with an HTTP status code of `500`, `501`, `503`, or `505`. … You can still
   customize responses for these status codes using Custom Error Rules."* Lobsy's onderhoud is juist
   een **503**, dus de klassieke "5XX Errors"-pagina doet voor ons niets. We hebben een
   **Custom Error Rule** nodig, en die is ook alleen op betaalde plannen beschikbaar.
2. De verplichte token voor een 5XX Error Page is `::CLOUDFLARE_ERROR_500S_BOX::`, exact één keer
   in de HTML. Hij staat in `ops/maintenance/cloudflare-500.html` in een visueel verborgen element.
   Zet géén `referrer` meta-tag op die pagina (breekt Cloudflare-challenges). Limiet is ~1,5 MB;
   onze pagina is ongeveer 6 KB.

### Advies

**Pro is het minimum** als je een eigen edge-pagina wil; Business voegt voor dit doel niets
noodzakelijks toe (alleen meer rules/assets). Enterprise is alleen nodig voor Origin Error Pages,
en dat hebben we niet nodig.

- **Zitten we op Pro of hoger** → maak een **Custom Error Rule** (Rules → Overview) die matcht op
  `http.response.code in {500 502 503 504}` en als content `ops/maintenance/cloudflare-500.html`
  serveert (inline of als Custom Error Asset vanaf een publieke URL). Vul die URL hieronder in.
- **Zitten we op Free** → drie opties:
  - **(a) Cloudflare Worker** op de zone die de origin-respons doorgeeft en bij 5xx de HTML van
    `ops/maintenance/index.html` teruggeeft met status 503 en `Retry-After: 300`. Workers zitten
    ook in het Free-plan.
  - **(b) Alleen Render maintenance mode** (zie §3). Dekt geplande ingrepen, niet een crash.
  - **(c) Niets** — bij een crash ziet de bezoeker Cloudflare's eigen foutpagina. Laag 1 dekt al
    alle geplande onderhoud, dus dit is een verdedigbare keuze.

### Wat Dennis moet beslissen

- [ ] Op welk Cloudflare-plan zit `lobsy.nl` nu? (Dashboard → de zone → Overview, rechterkolom.)
- [ ] Willen we een edge-pagina bij een onverwachte storing, of is laag 1 genoeg?
- [ ] Bij Pro+: Custom Error Rule aanmaken met de HTML uit `ops/maintenance/cloudflare-500.html`.
- [ ] Bij Free: Worker (a), alleen Render (b), of niets (c)?
- [ ] Waar hosten we de publieke URL van de pagina? (Render static site of Cloudflare Pages uit
      `ops/maintenance/`.) Vul hem hier in: `__________`.

---

## 3. Render maintenance mode (gecontroleerd 2026-10-01)

Bron: <https://render.com/docs/maintenance-mode> en <https://render.com/docs/blueprint-spec>.

- Alleen voor **betaalde** web services. De service blijft draaien maar is niet publiek bereikbaar
  (privé netwerk blijft werken).
- Render antwoordt op élk verzoek met **503** plus de ingestelde pagina.
- De `uri` moet een **absolute URL buiten deze service** zijn — dus niet `jobsy-web` zelf. Een
  Render static site uit `ops/maintenance/` is de aanrader. Geeft die URL een fout terug, dan
  stuurt Render die fout door in plaats van de onderhoudspagina.
- Zonder `uri` toont Render zijn eigen standaardpagina.

### Klikpad (Dennis)

1. Render dashboard → service **jobsy-web** → **Settings** → omlaag naar **Maintenance Mode**.
2. Zet de schakelaar aan en bevestig in de dialoog. Dit werkt direct.
3. Uitzetten: dezelfde schakelaar terug.

Wil je het in de blueprint vastleggen in plaats van per hand (Dennis, niet Cursor — `render.yaml`
blijft in deze stack ongewijzigd):

```yaml
services:
  - type: web
    name: jobsy-web
    maintenanceMode:
      enabled: true
      uri: https://onderhoud.lobsy.nl   # static site uit ops/maintenance/, nooit jobsy-web zelf
```

> Let op: Render maintenance mode blokkeert **alles**, inclusief `/healthz` en jouw admin-login.
> Gebruik laag 1 zodra je zelf nog in de app moet kunnen.

---

## 4. Onverwachte storing: wat ziet de bezoeker?

| Situatie | Zonder edge-pagina | Met Render maintenance mode | Met Cloudflare Custom Error Rule (Pro+) |
|---|---|---|---|
| Deploy / herstart | Cloudflare 502/503-pagina | Render-pagina of `ops/maintenance/index.html`, 503 | `cloudflare-500.html`, 503 |
| App crasht | Cloudflare 502-pagina | idem | `cloudflare-500.html` |
| Database plat, app draait | Lobsy's eigen 500-pagina met foutcode `LB-XXXX` | idem | idem (geen 5xx van de edge) |
| Geplande ingreep | onderhoudspagina via laag 1 | idem | idem |

---

## 5. Bestanden in deze repo

| Pad | Waarvoor |
|---|---|
| `ops/maintenance/index.html` | 5 talen, geen scripts. Voor Render `maintenanceMode.uri` of een Worker. |
| `ops/maintenance/cloudflare-500.html` | Zelfde pagina plus de verplichte Cloudflare 5XX-token. |
| `Jobsy.Web/Hosting/MaintenanceMiddleware.cs` | 503 + allow-list + admin bypass. |
| `Jobsy.Web/Hosting/MaintenanceState.cs` | De 15-seconden poller op `api/site/status`. |
| `Jobsy.Api/Security/MaintenanceApiMiddleware.cs` | 503 ProblemDetails voor de API. |
| `Jobsy.Core/Rules/MaintenanceRules.cs` | `Retry-After`-regels (E8) en de notitielengte. |

Beide HTML-bestanden zijn zelfstandig: geen scripts, geen externe fonts of afbeeldingen, inline CSS
en inline SVG. `MaintenanceStaticPageTests` bewaakt de grootte, het ontbreken van `<script`, de vijf
`lang`-waarden en de Cloudflare-token.
