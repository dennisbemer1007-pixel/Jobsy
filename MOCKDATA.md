# Mockdata Specificatie: Jobsy (Westland & Haaglanden)

Automatische seed via `JobsyDbSeeder` bij API-start:
`DemoCompaniesSeeder` → `DemoUsersSeeder` → `ApplicationsAndWagesSeeder` → `PlatformSettingsSeeder` → `Sprint0DemoSeeder` → **`Sprint8MetricsSeeder`** → **`WestlandVacanciesSeeder`** → **`HaaglandenVacanciesSeeder`**.

Bestaande DB’s krijgen media/settings/sprint0/sprint8/westland/haaglanden **backfill** (idempotent).

## 1. Bedrijven

| Bedrijf | Type | KVK | Locatie | Seed-grant |
|---------|------|-----|---------|------------|
| Westland Fresh Logistics | Employer | 12345678 | Honselersdijk 51.9812, 4.2235 | 5 |
| Boutique Café De Stad | Employer | 87654321 | Grote Markt DH 52.0735, 4.3120 | 5 |
| Supermarkt De Fred | Employer | 11223344 | Statenkwartier 52.0910, 4.2815 | 5 |
| Demo Intermediair Flex BV | Intermediary | 55667788 | Binckhorstlaan 52.0680, 4.3350 | 20 |

Plus **12 Westland-werkgevers** (Naaldwijk, De Lier, Honselersdijk, Monster, Poeldijk, Wateringen, Maasdijk, Kwintsheul, 's-Gravenzande, Heenweg) via `WestlandVacanciesSeeder` (KVK `71001001`–`71001012`).

Plus **Haaglanden-werkgevers** via `HaaglandenVacanciesSeeder`:
- Den Haag: 12 werkgevers (KVK `72001001`–`72001012`)
- Delft: 10 werkgevers (KVK `73001001`–`73001010`)
- Zoetermeer: 8 werkgevers (KVK `74001001`–`74001008`)

Plus regio “Den Haag Stad”, salaristabel De Fred, token packs/costs/PushBom-tiers (platform settings).

## 2. Vacatures (statusmix)

| Vacature | Bedrijf | Status / flags |
|----------|---------|----------------|
| Allround Orderpicker | Westland | Active + highlighted + extended |
| Ervaren Barista | Café | Active + extended |
| Vakkenvuller / Kassa | De Fred | Active |
| Flex medewerker retail (pool) | Intermediair | Active |
| Seizoenshulp kas (concept) | Westland | Draft |
| Avondploeg orderpicker | Westland | PendingApproval (+ requested options) |
| Zomerhulp (afgelopen) | Café | Archived |

### Banenkaart-testset (Westland)

`WestlandVacanciesSeeder` voegt **~52 Active** vacatures toe, verspreid over het Westland, zodat alle discover-filters te testen zijn:

| Filter | Dekking in seed |
|--------|-----------------|
| **Branche (workType)** | Alle 9: Horeca, Winkel, Logistiek, Tuinbouw, Zorg, Kantoor, Bouw, Schoonmaak, Productie (+ enkele dual flags) |
| **Vervoer** | Exclusief Lopend / Fiets / Auto / OV + gangbare combinaties |
| **Reistijd / radius** | Dichtbij Honselersdijk (~1–2 km), midden (~3–8 km), rand (~8–15 km, o.a. Ter Heijde / Hoek van Holland) |
| **Leeftijd + uurloon** | Enkele retail-vacatures met salaristabel (jeugdschaal); lonen van ~€8,50 tot ~€18,50 |

Guard: platform-log `"Westland banenkaart seed 50"`. Vacature-IDs `a1000000-0000-4000-8000-…`.

### Banenkaart-testset (Haaglanden)

`HaaglandenVacanciesSeeder` voegt **225 Active** vacatures toe:

| Stad | Aantal | Vacature-IDs | Bedrijfs-IDs |
|------|--------|--------------|--------------|
| Den Haag | 100 | `a2000000-…` | `c2000000-…` |
| Delft | 75 | `a3000000-…` | `c3000000-…` |
| Zoetermeer | 50 | `a4000000-…` | `c4000000-…` |

| Kenmerk | Dekking |
|---------|---------|
| **Unieke content** | Iedere vacature heeft een eigen titelvariant, uitgebreide tekst (intro / taken / aanbod / profiel), een lokale SVG-placeholder (`/images/vacancies/…`) en een `VideoUrl` (YouTube) |
| **Media backfill** | `MediaBackfillSeeder` vult ontbrekende/kapotte Unsplash-images, videos en te korte teksten bij bestaande databases |
| **Rijbewijs** | ~30% met `RequiredDrivingLicense` (B, BE, AM, T, Heftruck, C, …) |
| **Branche** | Alle 9 work types + enkele dual flags |
| **Wijken** | Den Haag (o.a. Centrum, Scheveningen, Binckhorst, Laak, Escamp, Ypenburg), Delft (Centrum, TU, Tanthof, Schieoevers), Zoetermeer (Stadshart, Rokkeveen, Seghwaert, Bleizo) |

Guard: platform-log `"Haaglanden banenkaart seed DH100-Delft75-Zoetermeer50"`.

## 3. Demo-accounts (wachtwoord via DemoUsers / local-login)

| E-mail | Rol |
|--------|-----|
| kandidaat@jobsy.local | Candidate (+ OpenForWork, home geo) |
| kandidaat.denhaag@jobsy.local / kandidaat.ver@jobsy.local | Extra PushBom-kandidaten |
| branch@jobsy.local | BranchManager |
| regional@jobsy.local | RegionalManager |
| enterprise@jobsy.local | EnterpriseManager |
| intermediair@jobsy.local | Intermediary |
| admin@jobsy.local | Admin |
| sales@jobsy.local | SalesManager (`SM-DEMO01`, wachtwoord `Jobsy123!`) |

## 3b. Salesmanager-demo (`SalesManagerDemoSeeder` v2)

Login: **sales@jobsy.local** / `Jobsy123!`. Guard: platform-log `"SalesManager dashboard seed v2"`.

| Onderdeel | Seed |
|-----------|------|
| **Referrals (3)** | Westland Fresh (slot 1), Boutique Café (slot 2), Supermarkt De Fred (slot 3) — allen €2500 onboarding betaald |
| **Founder bonuses** | €500 excl. BTW per leverancier (3×) |
| **Tokencommissies** | Café €75, Westland €120, Fred €42,50 (+ €25 kick-off adjustment) |
| **Facturen** | `SB-DEMO-2026-001` **Paid** (Westland founder) · `SB-DEMO-2026-002` **Issued** (Fred founder) |
| **Openstaand** | Café-founder + tokenregels + adjustment blijven uninvoiced → uitbetalen-demo |

## 4. Metrics & logs (Sprint 8)

Zodat `/home` en admin-finance gevuld zijn over **dag / week / maand**:

- **Token ledger:** purchases, spends (Publish/Highlight/PushBom/Extend), allocations
- **Engagement:** clicks/shares over ~25 dagen; likes (uniek per user×vacature) backdated + extra kandidaten
- **Applications:** Pending/Accepted/Rejected + gast-sollicitaties
- **PlatformLogs:** Info/Warning/Error over Auth, Push, Wages, Integration, Mail, Tokens

Guard: platform-log `"Sprint 8 rich metrics seed"` voorkomt dubbele runs.

## 5. Doel
1. Banenkaart direct gevuld (Westland + Den Haag + Delft + Zoetermeer + intermediair).
2. OSRM/reistijd-demo tussen regio’s.
3. Dashboards zonder lege KPI’s tijdens demos/pitches.
