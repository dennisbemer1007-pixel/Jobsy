# Mockdata Specificatie: Jobsy (Westland & Den Haag)

Automatische seed via `JobsyDbSeeder` bij API-start:
`DemoCompaniesSeeder` → `DemoUsersSeeder` → `ApplicationsAndWagesSeeder` → `PlatformSettingsSeeder` → `Sprint0DemoSeeder` → **`Sprint8MetricsSeeder`**.

Bestaande DB’s krijgen media/settings/sprint0/sprint8 **backfill** (idempotent).

## 1. Bedrijven

| Bedrijf | Type | KVK | Locatie | Seed-grant |
|---------|------|-----|---------|------------|
| Westland Fresh Logistics | Employer | 12345678 | Honselersdijk 51.9812, 4.2235 | 5 |
| Boutique Café De Stad | Employer | 87654321 | Grote Markt DH 52.0735, 4.3120 | 5 |
| Supermarkt De Fred | Employer | 11223344 | Statenkwartier 52.0910, 4.2815 | 5 |
| Demo Intermediair Flex BV | Intermediary | 55667788 | Binckhorstlaan 52.0680, 4.3350 | 20 |

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

## 4. Metrics & logs (Sprint 8)

Zodat `/home` en admin-finance gevuld zijn over **dag / week / maand**:

- **Token ledger:** purchases, spends (Publish/Highlight/PushBom/Extend), allocations
- **Engagement:** clicks/shares over ~25 dagen; likes (uniek per user×vacature) backdated + extra kandidaten
- **Applications:** Pending/Accepted/Rejected + gast-sollicitaties
- **PlatformLogs:** Info/Warning/Error over Auth, Push, Wages, Integration, Mail, Tokens

Guard: platform-log `"Sprint 8 rich metrics seed"` voorkomt dubbele runs.

## 5. Doel
1. Banenkaart direct gevuld (Westland + Den Haag + intermediair).
2. OSRM/reistijd-demo tussen regio’s.
3. Dashboards zonder lege KPI’s tijdens demos/pitches.
