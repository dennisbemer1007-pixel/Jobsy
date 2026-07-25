# Rollen, Rechten & Pagina-architectuur: Jobsy

Route- en capability-matrix per rol. Entry: **`/` = banenkaart**, **`/home` = role dashboard**. BottomNav via `RoleNavCatalog`.

---

## Cross-cutting
| Onderwerp | Gedrag |
|-----------|--------|
| Anoniem | Banenkaart `/`, Registreren, Inloggen |
| Post-login | `/` of `/banen` → `/home`; overige local returnUrls behouden |
| Logout | → `/` |
| Auth | Entra + Google + lokale/demo + registratie-activatie |
| Tokens (werkgevers) | `TokenWalletChip` in header → role-specifieke tokens-URL |
| Gedeelde UI | `MetricTile`, `DrilldownGrid`, `ShareModal`, `PublishOptionsDialog`, `BottomNav` |

Publiek (naast banenkaart): `/vacancies/{id}`, `/register`, `/register/activate`, `/login`, legal pages.

---

## 1. Candidate
*Doel: snel een baan vinden op reistijd/vervoer.*

**BottomNav:** Home · Banenkaart · Sollicitaties · Gedeeld · Geliked · Profiel

| Route | Inhoud |
|-------|--------|
| `/` | Banenkaart (filters, lijst, kaart) |
| `/home` | Eigen metrics: sollicitaties, shares, likes, reacties (periode + drilldown) |
| `/vacancies/{id}` | Detail, solliciteren, like/share, optioneel mock interview |
| `/candidate/applications` | Sollicitatiehistorie |
| `/candidate/liked` · `/candidate/shared` | Engagement-lijsten |
| `/candidate/profile` | OpenForWork, prefs, DOB, HomeLocation (PushBom) |

---

## 2. BranchManager
*Doel: lokaal werven voor één vestiging.*

**BottomNav:** Home · Banenkaart · Vacatures · Mijn tokens · Overnames

| Route | Inhoud |
|-------|--------|
| `/home` | Vestiging-KPI’s + drilldown |
| `/employer/vacancies` | Beheer + publiceren (basis/highlight/PushBom/verlengen) |
| `/branch/vacancies/new` | Nieuwe vacature |
| `/branch/applicants` | Sollicitanten; PII pas na Accept |
| `/branch/tokens` | Saldo / logs |
| `/employer/takeovers` | Inbox overnames |
| `/branch` | Redirect → `/home` |

Onvoldoende tokens bij publiceren → `PendingApproval` (EM/Admin keurt goed).

---

## 3. RegionalManager
*Doel: overzicht over vestigingen in de regio.*

**BottomNav:** Home · Banenkaart · Vacatures · Mijn vestigingen

| Route | Inhoud |
|-------|--------|
| `/home` | Regio-KPI’s |
| `/employer/vacancies` · `/employer/tokens` | Vacatures / tokens |
| `/regional/branches` | Vestigingen |
| `/regional` | Redirect → `/home` |

---

## 4. EnterpriseManager
*Doel: organisatiebreed beheer.*

**BottomNav:** Home · Banenkaart · Vacatures · Salaristabellen · Tokens · Vestigingen · Regio’s · Gebruikers

| Route | Inhoud |
|-------|--------|
| `/home` | Bedrijfs-KPI’s |
| `/employer/vacancies` | Vacaturebeheer + approve-publish |
| `/employer/tokens` | Aankoop (Mollie-stub), allocatie, logs |
| `/employer/branches` · `/employer/regions` | Vestigingen / regio’s |
| `/employer/users` | Invite-by-email + rollen |
| `/employer/salary-tables` | CAO/schalen voor vacatures |
| `/employer/takeovers` | Goedkeuren/afwijzen → org-merge |

---

## 5. Intermediary
*Doel: batch-hiring voor externe opdrachtgevers.*

**BottomNav:** Home · Banenkaart · Opdrachtgevers · Batch-tool · Tokens

| Route | Inhoud |
|-------|--------|
| `/home` | KPI-dashboard (zelfde metric-tiles als employers) |
| `/intermediary` | Overzicht gekoppelde bedrijven |
| `/intermediary/batch` | Multi-locatie publicatie |
| `/employer/tokens` | Token-saldo |

---

## 6. Admin
*Doel: platformcontrole.*

**BottomNav:** Home · Banenkaart · Vacatures · Financieel · Bedrijven · Settings  
*(Users, logging, wages, integraties via Settings-extra’s of modules op `/home`)*

| Route | Inhoud |
|-------|--------|
| `/home` | Platform-KPI’s (incl. errors, users, company counts) + module-grid |
| `/admin/companies` · `/admin/users` · `/admin/vacancies` | Beheer |
| `/admin/finance` · `/admin/tokens` | Finance KPI + tokenlog / grant |
| `/admin/logging` · `/admin/settings` · `/admin/integrations` | Logs, pricing/PushBom/early-adapter, integratie-pings |
| `/admin/wages` | WML + semi-annual update-stub |
| `/admin` · `/admin/cockpit` | Redirect → `/home` |
| `/admin/moderation` · `/masterdata` · `/notifications` | Placeholders (“later”) |

---

## Aliassen (bewust behouden)
| Alias | Doel |
|-------|------|
| `/banen` | → `/` |
| `/admin`, `/admin/cockpit` | → `/home` |
| `/branch`, `/regional` | → `/home` |
