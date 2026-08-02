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
| Gedeelde UI | `MetricTile`, `MetricsCategoryBoard` (Bento), `VacancyPerformancePanel`, `DrilldownGrid`, `ShareModal`, `PublishOptionsDialog`, `BottomNav` |
| Dashboard layout | Compacte periode-scroller · categorie-tabs · Bento-grid met featured KPI, sparklines/ringen · Top/Flop 3 vacatures (employer/admin) |

Publiek (naast banenkaart): `/vacancies/{id}`, `/register`, `/register/activate`, `/login`, `/partner` (+ `/partner/{trackingCode}`), legal pages.

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
| `/home` | Vestiging-KPI’s (Bento) + Top/Flop vacatures + drilldown |
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
| `/employer/tokens` | Pot-aankoop (radio pakketten), vestiging-opt-in, uitgifte, logs |
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
| `/home` | Platform-KPI’s (Bento, sparklines/ringen, Top/Flop vacatures) + drilldown |
| `/admin/companies` · `/admin/users` · `/admin/vacancies` | Beheer |
| `/admin/finance` · `/admin/tokens` | Finance KPI + tokenlog / grant |
| `/admin/sales` | Sales beheer: tokenwaarde, tarieven per vacaturetype, pakketten, highlight-toeslagen |
| `/admin/sales-managers` | Salesmanagers, trackingcodes, suppliers |
| `/admin/logging` · `/admin/settings` · `/admin/integrations` | Logs, pricing/PushBom/early-adapter, integratie-pings |
| `/admin/wages` | WML + semi-annual update-stub |
| `/admin` · `/admin/cockpit` | Redirect → `/home` |
| `/admin/moderation` · `/masterdata` · `/notifications` | Placeholders (“later”) |

---

## 7. SalesManager
*Doel: veldverkoop met trackingcode, commissies en partnerflyer.*

**BottomNav:** Home · Sales-toolkit · Onboarding · Facturen

| Route | Inhoud |
|-------|--------|
| `/home` | Dashboard: trackingcode, saldo, suppliers, commissies |
| `/salesmanager/toolkit` | Partnerlink, PDF-flyer, WhatsApp/mail delen, actuele tarieven |
| `/salesmanager/onboarding` | KvK/BTW/NAW + overeenkomst → trackingcode |
| `/salesmanager/invoices` | Self-billing / uitbetalen |
| `/partner/{code}` | Publieke partnerpagina met ingebedde salescode |

---

## Aliassen (bewust behouden)
| Alias | Doel |
|-------|------|
| `/banen` | → `/` |
| `/admin`, `/admin/cockpit` | → `/home` |
| `/branch`, `/regional` | → `/home` |
