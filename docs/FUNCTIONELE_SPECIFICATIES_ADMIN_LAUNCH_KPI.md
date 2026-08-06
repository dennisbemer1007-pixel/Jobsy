# Functionele & technische specificatie: Admin lancerings-KPI-dashboard

**Status:** ontwerp (implementatieklaar)  
**Doelgroep:** Admin  
**Campagne-anker:** teaser `/westland` · `/lancering` · livedatum **18 november 2026** (`FreePublishRules.DefaultUntil`)  
**Bestaande bouwstenen:** `SiteVisit` + `POST /api/analytics/site-visits`, `MetricsQueryService` / `MetricTile` / sparklines, `FreePublishUntil`, `VacancyKind` (Internship/Volunteer), `TokenPurchaseCheckout`, Admin `/home` Bento-dashboard

---

## 0. Doelen

In **één oogopslag** inzicht in:

1. **Verkeer** op de teaser (totaal + uniek), uitgesplitst per **UTM/kanaal/QR**
2. **Groei vóór 18 nov** — gratis vacatures, Westland-ondernemers, stages & vrijwilligerswerk
3. **Tokens & omzet** — pakketten, jaardeals, saldo vs verbruik (klaar voor post-launch)
4. **Conversie** — klik → registratie → gratis vacature
5. **Near-real-time** vernieuwing zonder zware full-page reloads

---

## 1. KPI-catalogus (canonieke keys)

| Key | Label (NL) | Definitie | Fase |
|-----|------------|-----------|------|
| `teaser_clicks_total` | Teaser-kliks (totaal) | Alle geregistreerde pageviews/CTA-kliks op teaser-paden | Altijd |
| `teaser_clicks_unique` | Teaser-kliks (uniek) | Distinct `VisitorKey` (= `UserId` of `AnonymousKey`) | Altijd |
| `teaser_clicks_by_utm` | Per kanaal | Groep op genormaliseerde UTM (zie §3) | Altijd |
| `free_vacancies_prelaunch` | Gratis vacatures t/m live | Vacatures gepubliceerd terwijl `FreePublishRules.IsActive` **of** met `PublishedUnderFreePromo=true` snapshot | Pre |
| `westland_employers` | Westland-ondernemers | Bedrijven in Westland-scope (zie §4.2) met ≥1 geactiveerde employer-rol | Pre |
| `vacancies_internship` | Stages | `VacancyKind.Internship`, status ≠ Draft (of Active-only toggle) | Altijd |
| `vacancies_volunteer` | Vrijwilligerswerk | `VacancyKind.Volunteer` | Altijd |
| `token_packs_sold` | Tokenpakketten verkocht | Credited checkouts met `ProductSku` ∈ pack-SKU’s | Post |
| `year_deals_sold` | Jaardeals (€3.000) | Credited checkouts met `ProductSku = year_deal_3000` | Post |
| `tokens_active_balance` | Actieve tokens (saldo) | Som company wallet balances (niet-expired grants) | Post |
| `tokens_spent_total` | Verbruikte tokens | Som `TokenTransaction` Spend sinds launch of periode | Post |
| `conv_click_to_register` | Conversie klik→registratie | Unieke teaser-bezoekers die later company-registratie activeren (attribution window) | Altijd |
| `conv_click_to_free_vacancy` | Conversie klik→gratis vacature | Unieke bezoekers → bedrijf met ≥1 free-promo publish | Pre |

Periode-filters (hergebruik admin-patroon): `day` / `week` / `month` / `quarter` / `campaign`  
**`campaign`** = vaste range `2026-01-01` → `FreePublishUntil` (of “sindsdien” na live).

---

## 2. Architectuur (lagen)

```
Jobsy.Web  /admin/launch  (of tab op AdminHome)
    ↓ JobsyApiClient
Jobsy.Api  LaunchKpisController  [Authorize Admin]
    ↓
Jobsy.Infrastructure  LaunchKpiQueryService
                       TeaserAnalyticsService (write path)
Jobsy.Core            Entities + LaunchKpiKeys + UtmNormalization
```

**Scheiding:**
- **Write path** (anoniem, rate-limited): teaser clicks / UTM — lichtgewicht
- **Read path** (Admin only): aggregaties + timeseries — cached 15–30s

---

## 3. Databasestructuur — kliklogs & UTM

### 3.1 Keuze: uitbreiden `SiteVisits` + dedicated teaser-events

Bestaande `SiteVisit` heeft: `UserId`, `AnonymousKey`, `Path`, `CreatedAt`.  
**Tekort** voor campagne: geen UTM, geen event-type (pageview vs CTA), sessie-dedupe blokkeert *totaal-kliks* (`tryClaimSiteVisit` = 1× per tab).

**Aanbevolen model**

#### A. Uitbreiding `SiteVisits` (optioneel, backward compatible)

| Kolom | Type | Toelichting |
|-------|------|-------------|
| `UtmSource` | `varchar(64)?` | genormaliseerd lowercase |
| `UtmMedium` | `varchar(64)?` | |
| `UtmCampaign` | `varchar(128)?` | |
| `UtmContent` | `varchar(128)?` | bijv. QR-variant |
| `UtmTerm` | `varchar(128)?` | |
| `ReferrerHost` | `varchar(128)?` | alleen host, geen full URL met tokens |
| `LandingPath` | `varchar(128)?` | canonieke `/westland` |

Indexes: `(CreatedAt)`, `(LandingPath, CreatedAt)`, `(UtmSource, CreatedAt)`.

#### B. Nieuwe tabel `TeaserEngagementEvents` (aanbevolen voor total vs unique)

```
TeaserEngagementEvents
  Id                uuid PK
  CreatedAt         timestamptz NOT NULL
  EventType         smallint NOT NULL   -- 1=PageView, 2=CtaRegister, 3=CtaWhatsApp
  Path              varchar(128) NOT NULL  -- /westland | /lancering
  VisitorKey        varchar(128) NOT NULL  -- "u:{guid}" | "a:{anonymousKey}"
  UserId            uuid NULL FK Users
  AnonymousKey      varchar(128) NULL
  SessionId         varchar(64) NULL     -- tab-session; unique pageview per session optioneel
  UtmSource         varchar(64) NULL
  UtmMedium         varchar(64) NULL
  UtmCampaign       varchar(128) NULL
  UtmContent        varchar(128) NULL
  UtmTerm           varchar(128) NULL
  ReferrerHost      varchar(128) NULL
  IsUniqueVisitorDay bool NOT NULL DEFAULT false  -- denormalized helper (optional)
```

**Indexes**
- `(CreatedAt)`
- `(EventType, CreatedAt)`
- `(UtmSource, CreatedAt)` INCLUDE (`VisitorKey`)
- `(VisitorKey, CreatedAt)`
- Partial unique optioneel: `(SessionId, EventType, Path)` WHERE `EventType=PageView` — voorkomt spam; **niet** gebruiken als “totaal kliks” alle pageviews moet tellen over dagen (dan wél meerdere dagen, uniek per session/day).

**Aanbevolen telregels**
- **Totaal kliks/pageviews:** count rows `EventType=PageView` (1 per tab-sessie per kalenderdag, of elke load — kies productregel; default: **1 pageview per VisitorKey per UTC-dag** + aparte CTA-events altijd tellen).
- **Uniek:** `COUNT(DISTINCT VisitorKey)` in periode.
- **CTA-kliks:** aparte `EventType` zodat registratie-knop vs WhatsApp zichtbaar is.

### 3.2 UTM-normalisatie (`UtmNormalization`)

```
Input: ?utm_source=Flyer_Bakker&utm_medium=qr&utm_campaign=westland_nov
→ Source = "flyer_bakker" (trim, lower, max 64, strip control chars)
→ Medium = "qr"
→ Campaign = "westland_nov"
ChannelKey voor UI = Source (fallback Medium, anders "(direct)")
```

Whitelist-suggestie voor demo/copy: `flyer_bakker`, `whatsapp`, `qr_naaldwijk`, `linkedin`, `partner_sm`, `(direct)`.

**Privacy:** geen IP opslaan; geen raw querystring; cookie/analytics consent blijft verplicht (bestaande `jobsyCookieConsent.allowsAnalytics`).

### 3.3 Attribution → conversie

Nieuwe lichte tabel of kolommen:

```
TeaserAttributions
  VisitorKey        varchar(128) PK/unique
  FirstTouchAt      timestamptz
  UtmSource         varchar(64)?
  CompanyId         uuid?          -- gezet bij registratie-activatie
  RegisteredAt      timestamptz?
  FirstFreePublishAt timestamptz?
```

Flow:
1. Eerste teaser-event upsert first-touch UTM.
2. Bij company activate: match `AnonymousKey` uit localStorage (zelfde key als analytics) → zet `CompanyId`.
3. KPI conversie = attributed companies / unique teaser visitors.

---

## 4. Domeinregels voor lanceringsmetriek

### 4.1 Gratis vacatures tot 18 november

Bronnen (combineer voor betrouwbaarheid):

1. **Snapshot bij publish:** nieuwe kolom `Vacancy.PublishedUnderFreePromo` (bool) + `PublishedAtUtc`  
   → immuun voor latere wijziging van `FreePublishUntil`.
2. **Fallback:** `Status` werd Active terwijl `FreePublishRules.IsActive(settings.FreePublishUntil, PublishedAt)`.

Filter: `Kind == Regular` (stages/vrijwilligers apart — die blijven sowieso €0).

KPI `free_vacancies_prelaunch` = count waar `PublishedUnderFreePromo && Kind==Regular`.

### 4.2 Westland-ondernemers

Definieer **Westland-scope** centraal (`WestlandGeoRules` of Region-seed):

| Strategie | Implementatie |
|-----------|----------------|
| **A (MVP)** | Bedrijven waarvan vestigingsadres/postcode in Westland-lijst (Naaldwijk, De Lier, …) of `Company.RegionId` = Westland-region |
| **B** | Bounding box + PostGIS op company location |
| **C** | Handmatige tag `Company.LaunchCohort = Westland2026` bij registratie vanaf teaser |

Aanbevolen: **A + C** — registratie vanaf `/westland` zet `LaunchCohort`; geo-lijst als aanvulling.

Telling: distinct `CompanyId` met role ∈ {BranchManager, RegionalManager, EnterpriseManager, Intermediary}, `EmailVerified`/activated, cohort of geo match.

### 4.3 Stages & vrijwilligerswerk

```
vacancies_internship = Vacancies.Count(Kind == Internship && Status in Active|…)
vacancies_volunteer  = Vacancies.Count(Kind == Volunteer && …)
```

Toon ook “gratis-jaren resterend” als info-chip (3 jaar vanaf launch — platform setting `SocialFreeUntil = 2029-11-18`).

### 4.4 Tokens & jaardeals

Introduceer `TokenPurchaseCheckout.ProductSku` (varchar) of enum:

| Sku | PackSize / betekenis | Prijs (ex. marketingcopy) |
|-----|----------------------|---------------------------|
| `token_single` | 1 | €25 |
| `pack_10` | 10 (+1 bonus via fulfillment rule) | €250 |
| `pack_25` | 25 (+3 bonus) | €625 |
| `year_deal_3000` | maandelijkse grant-job + 50% kortingsvlag op company | €3.000 |

KPI’s:
- `token_packs_sold` = credited where sku in pack_*
- `year_deals_sold` = credited where sku = year_deal_3000
- `tokens_active_balance` = bestaand wallet-aggregate (FinanceAdmin)
- `tokens_spent_total` = sum Spend in periode

Jaardeal fulfillment (apart ticket): `Company.YearDealActiveUntil`, monthly `TokenGrant` hosted service — dashboard toont verkochte deals + actieve deals.

---

## 5. API-contracten

### 5.1 Write — teaser analytics

```
POST /api/analytics/teaser-events
[AllowAnonymous] + rate limit public-write
Body: {
  "anonymousKey": "anon-…",
  "eventType": "page_view" | "cta_register" | "cta_whatsapp",
  "path": "/westland",
  "sessionId": "…",
  "utmSource": "flyer_bakker",
  "utmMedium": "qr",
  "utmCampaign": "westland_nov",
  "utmContent": null,
  "utmTerm": null,
  "referrerHost": "lobsy.nl"
}
→ 204 / { recorded: true }
```

Consent-gate client-side (bestaand). Geen PII in body.

### 5.2 Read — admin launch KPI’s

```
GET /api/admin/launch-kpis?period=campaign|day|week|month
[Authorize Admin]

Response: {
  "asOfUtc": "…",
  "period": { "key": "campaign", "fromUtc": "…", "toUtc": "…" },
  "liveDate": "2026-11-18",
  "phase": "prelaunch" | "live",
  "kpis": [
    { "key": "teaser_clicks_total", "label": "…", "value": 1240, "deltaPct": 12.5, "sparkline": [..] },
    …
  ],
  "utmBreakdown": [
    { "channel": "flyer_bakker", "total": 420, "unique": 310, "sharePct": 33.8 }
  ],
  "timeseries": {
    "teaserClicks": [ { "date": "2026-08-01", "total": 40, "unique": 32 } ],
    "freeVacancies": [ { "date": "…", "count": 5 } ],
    "conversion": [ { "date": "…", "clickToVacancyPct": 4.2 } ]
  },
  "finance": {
    "packsSold": 0,
    "yearDealsSold": 0,
    "tokensBalance": 12500.5,
    "tokensSpent": 320
  }
}
```

```
GET /api/admin/launch-kpis/utm/{source}/drilldown?period=…
→ lijst recente events / companies (zonder e-mail plaintext; company name + id)
```

Cache: `IMemoryCache` key `launch-kpis:{period}` TTL **20 seconden** (near-real-time).

---

## 6. Frontend — UI/UX

### 6.1 Route & navigatie

- Pagina: **`/admin/launch`** — “Lanceringsdashboard”
- Link in Admin-nav / AdminHome header-chip “Campagne Westland”
- Alleen `Admin`-rol

### 6.2 Layout (widgets)

```
┌─────────────────────────────────────────────────────────┐
│ Lanceringsdashboard          [Dag|Week|Maand|Campagne] ⏱│
│ Live over 104 dagen · data ~20s ververst                 │
├──────────────┬──────────────┬──────────────┬────────────┤
│ Teaser totaal│ Uniek        │ Conversie %  │ Fase-chip  │
│ + sparkline  │ + sparkline  │ klik→vacature│ Pre-launch │
├──────────────┴──────────────┴──────────────┴────────────┤
│ 📈 Kliks over tijd (totaal vs uniek)      [line chart]  │
├─────────────────────────────┬───────────────────────────┤
│ Kanalen (UTM)               │ Groei Westland            │
│ horizontal bar / table      │ gratis vacatures          │
│ flyer_bakker ████ 420       │ ondernemers               │
│ whatsapp     ███  310       │ stages | vrijwilligers    │
├─────────────────────────────┴───────────────────────────┤
│ Tokens & deals (prep / post)                            │
│ pakketten | jaardeals | saldo | verbruik                │
└─────────────────────────────────────────────────────────┘
```

**Componenten (hergebruik)**
- `MetricTile` + `MetricSparkline` / `MetricRing` — KPI-kaartjes
- Nieuw: `LaunchKpiBoard.razor` — samenstelling
- Nieuw: `UtmChannelTable.razor` — sorteerbare kanalen
- Nieuw: `LaunchTimeseriesChart.razor` — SVG/CSS chart of lichte chart-lib al in repo; voorkeur **pure SVG** zoals sparklines (geen zware dependency)
- Polling: `PeriodicTimer` / `Timer` elke **20s** zolang tab zichtbaar (`visibilitychange`)

### 6.3 Teaser-instrumentatie (Web)

Op `WestlandTeaser.razor` / `TeaserLayout`:
1. Na analytics-consent: `page_view` event met UTM uit `NavigationManager.Uri` / `QueryHelpers`
2. Op Register-CTA click: `cta_register` (vóór navigatie)
3. Op WhatsApp click: `cta_whatsapp`
4. Persist first-touch UTM in `localStorage` key `Jobsy.TeaserAttribution` voor latere registratie-koppeling

### 6.4 UX-copy (admin)

- Lege staat pre-traffic: “Nog geen teaser-kliks — deel je UTM-links of QR’s.”
- Fase Pre: finance-widgets gedimd met hint “Tarieven actief ná 18 november”
- Fase Live: groei-widgets blijven historisch zichtbaar (campaign totals)

---

## 7. Near-real-time strategie

| Laag | Mechanisme |
|------|------------|
| Client | Poll 20s + manual refresh-knop |
| API | Memory cache 20s; `asOfUtc` in response |
| DB | Indexes op `CreatedAt`; aggregaties in SQL (`GROUP BY date_trunc`) |
| Optioneel later | SignalR hub `LaunchKpiHub` push bij threshold (niet MVP) |

Geen continuous websocket in MVP — 20s voelt “live” voor campagne-monitoring.

---

## 8. Implementatiestappen (engineering)

### Fase A — Tracking foundation
1. Migratie `TeaserEngagementEvents` (+ optioneel SiteVisit UTM-kolommen)
2. `POST /api/analytics/teaser-events` + tests (rate limit, validation, consent niet server-side enforced maar client)
3. Wire teaser pagina + UTM parse helper
4. Seed demo events in Development voor dashboard-demo

### Fase B — Query service & API
1. `ILaunchKpiQueryService` met KPI’s §1
2. `GET /api/admin/launch-kpis`
3. Snapshot `PublishedUnderFreePromo` bij publish
4. Westland cohort/geo helper
5. Unit tests op aggregaties + FreePublish window

### Fase C — Admin UI
1. `/admin/launch` + nav-link
2. Widgets, UTM-tabel, timeseries SVG
3. Polling + periode-tabs
4. Localization NL (Admin-only; EN later)

### Fase D — Finance SKUs
1. `ProductSku` op checkout + year deal stub
2. Finance widgets koppelen
3. Documentatie tarieven synchroon met teaser-copy

### Fase E — Docs & AVG
1. `SECURITY.md`: teaser events = geen IP/e-mail; retention gelijk SiteVisits
2. Retention job: purge `TeaserEngagementEvents` ouder dan N dagen (align `PrivacyConstants`)

---

## 9. AVG / security checklist

- Admin-only read endpoints (`JobsyPolicies.RequireAdmin`)
- Anonymous write: rate limit + anonymousKey validatie (bestaand patroon)
- Geen plaintext e-mail in drilldowns/logs
- UTM/content max-length + sanitize
- Consent vóór write (ePrivacy)
- Retention + right-to-be-forgotten: anon key events blijven aggregaat-OK; user-linked rows scrubben bij delete-account

---

## 10. Testplan (minimaal)

1. Pageview met `?utm_source=flyer_bakker` → row + breakdown channel  
2. Zelfde VisitorKey zelfde dag → unique=1, total volgens dag-regel  
3. CTA events verhogen niet unique visitor dubbel voor page_view unique  
4. Free vacancy publish vóór 18 nov → `PublishedUnderFreePromo`  
5. Admin KPI endpoint 403 voor BranchManager  
6. Cache: twee calls binnen TTL → zelfde `asOfUtc`  
7. Conversie: attribution visitor → activated company  

---

## 11. Relatie tot bestaande admin-dashboard

| Bestaand | Nieuw |
|----------|-------|
| `/admin` / AdminHome platform KPI’s | Campagne-specifieke `/admin/launch` |
| `site_visits` / `site_visits_unique` | Teaser-scoped + UTM (nauwkeuriger) |
| FinanceAdmin token metrics | Launch finance strip (packs/year deals) |
| MetricTile Bento | Zelfde design language, vaste campagne-layout |

Geen breaking change aan bestaande metrics; launch-dashboard is **add-on**.

---

## 12. Samenvatting in één zin

**Breid anonieme teaser-events uit met UTM en visitor-keys, aggregeer die samen met free-promo vacatures, Westland-cohorten en token-SKU’s in een Admin-only `/admin/launch` dashboard met MetricTiles, kanaaltabel en timeseries die elke ~20 seconden verversen.**
