# Functioneel Ontwerp & Specificatie: Lobsy Uitbreidingen

Intermediair-beheer met flexibele adresweergave, salesmanager-tracking met revenue-share, en beheer-KPI’s.

## 1. Intermediair: team, KVK & banenkaart

### Teambeheer
- Intermediair nodigt collega’s uit op e-mail (`/intermediary/team`).
- Na registratie dezelfde intermediair-organisatie (`CompanyId`) + gedeelde memberships → gedeelde vacatures en tokensaldo.

### Vacature + KVK (achterkant)
- Bij aanmaken (handmatig, batch, CSV, API) is KVK-nummer + vestiging van het **inhurende bedrijf** verplicht voor de rol Intermediair.
- Altijd opgeslagen via `Vacancy.CompanyId` (eindklant) + `Vacancy.IntermediaryCompanyId`.
- Ontbrekende KVK/vestiging → request/import direct afgewezen.

### Flexibele adresweergave (voorkant)
- Standaard / afgeschermd (`ShowClientAddressOnMap = false`): banenkaart toont naam + adres + pin van het uitzendbureau.
- Open kaart: toont eindklant.
- Pop-up label: **“Aangeboden door [Intermediair]”**. Sollicitatie loopt via het uitzendbureau.
- Eindklant-KVK/locatie blijft in DB voor admin, reistijd-matching en SROI.

## 2. Salesmanager: tracking, hiërarchie & revenue-share

### Rol & hiërarchie (één wervingslaag)
- **Admin** maakt initiële (tier-0) salesmanagers aan via `/admin/sales-managers`.
- Actieve tier-0 salesmanagers dienen aanbevelingen in (`/salesmanager/referrals`) met trackingcode + korte motivatie.
- **Admin-goedkeuring is verplicht** vóór accountprovisioning; goedgekeurde kandidaten krijgen een eigen trackingcode na onboarding.
- Doorverwezen (tier-1) salesmanagers **kunnen zelf geen** nieuwe salesmanagers aanbevelen (`CanRecruitSalesManagers = false`).

### Trackingcodes
- Salesmanagers genereren unieke trackingcodes na onboarding/agreement (bestaand).
- Koppeling op bedrijfsrecord: `Company.ReferredBySalesManagerUserId`.
- Gekoppelde ondernemers krijgen nav **“Mijn Saldo & Tracking”** (`HasSalesReferral` claim) → tokens-pagina met statusuitleg.

### Revenue-share bij tokenaankoop (referred companies)
Percentages zijn **Admin-configureerbaar** (`/admin/sales`); defaults hieronder. Directe/indirecte SM-commissie lopen maximaal **1 jaar** vanaf `Company.FirstYearStartedAt`.

| Ontvanger | Default % | Bestemming |
|-----------|-----------|------------|
| Ondernemer (ambassadeur) | 15% | Token-tegoed (`Grant`) |
| Primary salesmanager (direct) | 15% | Commissiesaldo (≤ 1 jaar) |
| Referring salesmanager (indirect) | 3% | Commissiesaldo (alleen bij SM→SM referral, ≤ 1 jaar) |
| Platform (Lobsy) | rest | Impliciet |

Volledige logging in `RevenueShareLogs` (+ commissieledger / tokenledger), idempotent per checkout.

### Admin
- Bedrijvengrid: kolom **Salesmanager**.
- Aanbevelingen reviewen op `/admin/sales-managers`; commissietarieven op `/admin/sales`.

## 3. KPI: gemiddelde doorlooptijd vacatures

\[
\text{Doorlooptijd} = \text{AVG}(\text{ClosedAtUtc} - \text{CreatedAtUtc})
\]

voor status **Archived** of **Fulfilled** (met `ClosedAtUtc`; legacy fallback op `EndDate`).

Weergave: hoofdmeter op Admin-dashboard (`avg_vacancy_lead_time_days`).
