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

Publiek (naast banenkaart): `/vacancies/{id}`, `/{kvknummer}` (ondernemer), `/{kvknummer}/{vestigingsnummer}` (vestiging), `/vestiging/{companyId}` (QR-landing → publieke vestigings-URL), `/register`, `/register/activate`, `/login`, `/partner` (+ `/partner/{trackingCode}`), legal pages.

---

## 1. Candidate
*Doel: snel een baan vinden op reistijd/vervoer.*

**BottomNav:** Zoeken · Bewaard · Sollicitaties · Profiel · Hoe werkt Lobsy

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

**BottomNav:** Home · Banenkaart · Vacatures · Mijn tokens · Bedrijfsgegevens · Overnames · Hoe werkt Lobsy

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

**BottomNav:** Home · Banenkaart · Vacatures · Mijn vestigingen · Hoe werkt Lobsy

| Route | Inhoud |
|-------|--------|
| `/home` | Regio-KPI’s |
| `/employer/vacancies` · `/employer/tokens` | Vacatures / tokens |
| `/regional/branches` | Vestigingen |
| `/regional` | Redirect → `/home` |

---

## 4. EnterpriseManager (Bedrijfsmanager)
*Doel: organisatiebreed beheer met strikte mobile/desktop-scheiding.*

**Mobile/PWA BottomNav (operationeel):** Home · Banenkaart · Vacatures · Tokens · Gebruikers · Hoe werkt Lobsy  

**Desktop BottomNav:** bovenstaande (met **Organisatie** desktop-only vóór Hoe werkt Lobsy)

| Route | Inhoud | Scherm |
|-------|--------|--------|
| `/home` | Bedrijfs-KPI’s | Mobiel + desktop |
| `/employer/vacancies` | Vacaturebeheer + approve-publish | Mobiel + desktop |
| `/employer/tokens` | Pot-aankoop, uitgifte, logs | Mobiel + desktop |
| `/employer/users` | Basis gebruikerslijst / invites | Mobiel + desktop |
| `/employer/organization` | Hub voor zwaar org-beheer | Desktop (melding op mobiel) |
| `/employer/salary-tables` | CAO/schalen voor vacatures | Desktop-preferred |
| `/employer/branches` · `/employer/regions` | Vestigingen / regio’s | Desktop-preferred |
| `/employer/company` | Bedrijfsgegevens | Desktop-preferred |
| `/employer/csv-import` | Bulk CSV-import | Desktop-preferred |
| `/employer/takeovers` | Goedkeuren/afwijzen → org-merge | Desktop-preferred |

---

## 5. Intermediary
*Doel: werven voor externe opdrachtgevers.*

**BottomNav:** Home · Banenkaart · Vacatures · Bedrijvenoverzicht · Team · Tokens · Hoe werkt Lobsy

| Route | Inhoud |
|-------|--------|
| `/home` | KPI-dashboard (zelfde metric-tiles als employers) |
| `/intermediary` | Bedrijvenoverzicht: prestaties per gekoppelde opdrachtgever |
| `/intermediary/team` | Collega’s uitnodigen (zelfde organisatie) |
| `/employer/vacancies` | Vacatures per opdrachtgever |
| `/branch/vacancies/new` | Vacature met verplichte KVK-vestiging + open/afgeschermde kaart |
| `/employer/tokens` | Token-saldo |

## 5b. SalesManager
*Doel: acquisitie via trackingcodes + commissie / revenue-share.*

**BottomNav:** Home · Sales-toolkit · Referrals · Onboarding · Facturen · Hoe werkt Lobsy

| Route | Inhoud |
|-------|--------|
| `/home` / `/salesmanager` | Dashboard, trackingcode, commissiesaldo |
| `/salesmanager/referrals` | Aanbevelingen voor nieuwe salesmanagers (alleen tier-0) |
| `/salesmanager/onboarding` | Bedrijfsgegevens + overeenkomst → trackingcode |
| `/salesmanager/invoices` | Self-billing / uitbetaling |

Gekoppelde ondernemers (via trackingcode) krijgen nav **“Mijn Saldo & Tracking”** → `/employer/tokens` of `/branch/tokens`.

## 5c. Ambassadeur
*Doel: kandidaten werven via trackingcode + gelaagde commissie; ondernemersflyer met gratis start-highlight.*

**BottomNav:** Home · Toolkit · Financieel · Onboarding · Hoe werkt Lobsy

| Route | Inhoud |
|-------|--------|
| `/home` / `/ambassadeur` | Dashboard: KPI kandidaten + sollicitaties, commissie%, trackinglink |
| `/ambassadeur/toolkit` | Deelbare link, kandidaten-flyer + ondernemers-flyer (QR) |
| `/ambassadeur/finance` | Commissies per transactie / uitbetalen |
| `/ambassadeur/onboarding` | KvK/BTW/NAW + overeenkomst → trackingcode `AM-…` |
| `/werven/{code}` | Publieke landing → login met Ambassadeur-referral cookie |

Admin: `/admin/ambassadeurs` — uitnodigen, drempels (50 / +1% / max), commissie-override.

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
| `/admin/sales` | Sales beheer: tokenwaarde, commissie-% (direct/indirect), duur, tarieven, pakketten, highlights |
| `/admin/sales-managers` | Salesmanagers, aanbevelingen (approve/reject), trackingcodes, suppliers |
| `/admin/ambassadeurs` | Ambassadeurs, commissiedrempels, overrides, trackingcodes |
| `/admin/logging` · `/admin/settings` · `/admin/integrations` | Logs, pricing/PushBom/early-adapter, integratie-pings |
| `/admin/cnames` | CNAME / regio-hosts (hostname, branding, adres-autocomplete) + checklist-hulp (?) |
| `/admin/masterdata` · `/admin/vacancy-categories` · `/admin/exclusivity` | Keuzelijsten, vacaturecategorieën (kleur/tokens/extra velden), stage-exclusiviteit |
| `/admin/wages` | WML + semi-annual update-stub |
| `/admin` · `/admin/cockpit` | Redirect → `/home` |
| `/admin/moderation` · `/masterdata` · `/notifications` | Placeholders (“later”) |

---

## 7. SalesManager
*Doel: veldverkoop met trackingcode, commissies en partnerflyer.*

**BottomNav:** Home · Sales-toolkit · Referrals · Onboarding · Facturen · Hoe werkt Lobsy

| Route | Inhoud |
|-------|--------|
| `/home` | Dashboard: trackingcode, saldo, suppliers, commissies |
| `/salesmanager/toolkit` | Partnerlink, PDF-flyer, WhatsApp/mail delen, actuele tarieven |
| `/salesmanager/referrals` | SM-aanbevelingen indienen / status (tier-0 only) |
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
