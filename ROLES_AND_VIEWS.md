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
| Hoe werkt Lobsy | Account-menu onder de userknop (niet Admin; gasten hebben geen account-menu) |
| Lobsy-assistent | Rechterrand-tab, zelfde patroon als Feedback (niet gast, niet Ambassadeur) |
| Gedeelde UI | `MetricTile`, `MetricsCategoryBoard` (Bento), `VacancyPerformancePanel`, `DrilldownGrid`, `ShareModal`, `PublishOptionsDialog`, `BottomNav` |
| Dashboard layout | Compacte periode-scroller · categorie-tabs · Bento-grid met featured KPI, sparklines/ringen · Top/Flop 3 vacatures (employer/admin) |

Publiek (naast banenkaart): `/vacancies/{id}`, `/{kvknummer}` (ondernemer), `/{kvknummer}/{vestigingsnummer}` (vestiging), `/vestiging/{companyId}` (QR-landing → publieke vestigings-URL), `/westland` · `/lancering` (teaser live 18 nov), `/register`, `/register/activate`, `/login`, `/partner` (+ `/partner/{trackingCode}`), legal pages.

---

## 1. Candidate
*Doel: snel een baan vinden op reistijd/vervoer.*

**BottomNav:** Match · Zoeken · Bewaard · Vacatures · Sollicitaties · Profiel

| Route | Inhoud |
|-------|--------|
| `/candidate/match` | Match & Swipe: ontgrendel-checklist (basis + opleiding incl. **Geen** + tests) tot `IsProfileComplete`; daarna relevante vacatures (opleiding/reistijd). Banenkaart blijft breder zoeken. |
| `/` | Banenkaart (filters, lijst, kaart). Ingelogde kandidaat: live match-% per vacature, **Cultuur Fit**-label na harde criteria, sort/filter op match, uitleg waarom |
| `/home` | **Mijn Lobsy Kompas** in tabbladen: **Wie ben ik?**, **Mijn profiel**, **Mijn competenties**, **DISC-Analyse**, **Mijn beste match**, **Functiefit checker** + eigen metrics |
| `/vacancies/{id}` | Detail, solliciteren, like/share, optioneel mock interview, kandidaat **Past deze vacature bij mij?** |
| `/candidate/applications` | Sollicitatiehistorie |
| `/candidate/vacancies` | Overzicht + **Onlangs bekeken** (localStorage, alleen vacancy-GUIDs) + in-de-buurt op reistijd/vervoer |
| `/candidate/liked` · `/candidate/shared` | Engagement-lijsten |
| `/candidate/profile` | OpenForWork, NAW/CV, recensies, **Mijn Lobsy Kompas**-tabbladen (Wie ben ik? / profiel / competenties: grafiek + accordeon met workshops / DISC-Analyse / beste match / Functiefit checker in 4 stappen) — werkgeverscontact alleen op tab Mijn profiel; Top 10 vacatures (≥60%); PDF als diepte-analyse klaar is |
| `/candidate/competencies` | Competentietest (25); draft tussentijds opslaan; scores herberekend bij afronden |
| `/candidate/disc` | Gedragsanalyse / DISC Quick-Scan (25); accordeon met workshops; optionele diepte-analyse 150 |
| `/candidate/career` | Beroepentest (25); Mijn Beroepen-kompas + top 10 actieve vacatures |
| `/candidate/deep-analysis/{kind}` | Betaalde 150-vragen analyse (competence \| career) + loopbaan-PDF (gekleurd logo, match-banden, *Wat betekent dit voor jou?*) |
| `/candidate/talent-contacts` | Inbox contactverzoeken (48u); geen extra bottom-nav tab |

---

## 2. BranchManager
*Doel: lokaal werven voor één vestiging.*

**BottomNav:** Home · Banenkaart · Vacatures · Sollicitaties · Talentpool · Mijn tokens · Bedrijfsgegevens · Overnames

| Route | Inhoud |
|-------|--------|
| `/home` | Vestiging-KPI’s (Bento) + Top/Flop vacatures + drilldown |
| `/employer/vacancies` | Beheer + publiceren (basis/highlight/PushBom/verlengen) |
| `/branch/vacancies/new` | Nieuwe vacature, inclusief 3–5 cultuurpijlers en optionele hoge/lage functie-drempel (diploma/certificaat/ervaring) |
| `/branch/applicants` | Sollicitaties; pre-accept: motivatie/afstand/beschikbaarheid/leeftijd; PII + Lobsy-CV + geüpload CV na Accept; daarna uitnodigen / matchen / weigeren |
| `/employer/talent` | Anonieme talentpool (filters zonder leeftijd); ContactUnlock 1 token |
| `/employer/talent-contacts` | Contactverzoeken + 48u refund-intrekken |
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

## 4. EnterpriseManager (Bedrijfsmanager)
*Doel: organisatiebreed beheer met strikte mobile/desktop-scheiding.*

**Mobile/PWA BottomNav (operationeel):** Home · Banenkaart · Vacatures · Sollicitaties · Tokens · Gebruikers  

**Desktop BottomNav:** bovenstaande (met **Organisatie** desktop-only)

| Route | Inhoud | Scherm |
|-------|--------|--------|
| `/home` | Bedrijfs-KPI’s | Mobiel + desktop |
| `/employer/vacancies` | Vacaturebeheer + approve-publish | Mobiel + desktop |
| `/branch/applicants` | Sollicitaties (zelfde flow als filiaalmanager) | Mobiel + desktop |
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

**BottomNav:** Home · Banenkaart · Vacatures · Bedrijvenoverzicht · Team · Tokens

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

**BottomNav:** Home · Sales-toolkit · Referrals · Onboarding · Facturen

| Route | Inhoud |
|-------|--------|
| `/home` / `/salesmanager` | Dashboard, trackingcode, commissiesaldo |
| `/salesmanager/referrals` | Aanbevelingen voor nieuwe salesmanagers (alleen tier-0) |
| `/salesmanager/onboarding` | Bedrijfsgegevens + overeenkomst → trackingcode |
| `/salesmanager/invoices` | Self-billing / uitbetaling |

Gekoppelde ondernemers (via trackingcode) krijgen nav **“Mijn Saldo & Tracking”** → `/employer/tokens` of `/branch/tokens`.

## 5c. Ambassadeur
*Doel: kandidaten werven via trackingcode + gelaagde commissie; ondernemersflyer met gratis start-highlight.*

**BottomNav:** Home · Toolkit · Financieel · Onboarding

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
| `/admin/companies` · `/admin/users` · `/admin/vacancies` · `/admin/ats-vacancies` | Beheer (ATS = eigen bottom-nav knop + gescrapete directe werkgevers) |
| `/admin/finance` · `/admin/tokens` | Finance KPI + tokenlog / grant |
| `/admin/sales` | Sales beheer: tokenwaarde, commissie-% (direct/indirect), duur, tarieven, pakketten, highlights |
| `/admin/sales-managers` | Salesmanagers, aanbevelingen (approve/reject), trackingcodes, suppliers |
| `/admin/ambassadeurs` | Ambassadeurs, commissiedrempels, overrides, trackingcodes |
| `/admin/logging` · `/admin/settings` · `/admin/integrations` | Logs, pricing/PushBom/early-adapter, integratie-pings |
| `/admin/launch` | Lancerings-KPI’s: teaser-kliks/UTM, gratis vacatures, Westland-groei, tokens/jaardeals (spec) |
| `/admin/cnames` | CNAME / regio-hosts (hostname, branding, adres-autocomplete) + checklist-hulp (?) |
| `/admin/masterdata` · `/admin/vacancy-categories` · `/admin/exclusivity` · `/admin/training` | Keuzelijsten, vacaturecategorieën (kleur/tokens/extra velden), stage-exclusiviteit, opleiders (affiliate + regionale deals, maand-CSV) |
| `/admin/wages` | WML + semi-annual update-stub |
| `/admin` · `/admin/cockpit` | Redirect → `/home` |
| `/admin/moderation` · `/masterdata` · `/notifications` | Placeholders (“later”) |

---

## 7. SalesManager
*Doel: veldverkoop met trackingcode, commissies en partnerflyer.*

**BottomNav:** Home · Sales-toolkit · Referrals · Onboarding · Facturen

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
