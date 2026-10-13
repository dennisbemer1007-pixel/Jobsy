# Psychometrische review Lobsy kandidaatstests (COTAN-geïnspireerd)

**Datum:** 13 oktober 2026  
**Scope:** Read-only review op basis van code, seed/metadata en scripts in `docs/psychometrie/`. **Geen wijzigingen aan productcode.**  
**Status:** Dit is **geen officiële COTAN-beoordeling** en vervangt geen registratie, certificering of juridisch advies. Het is een interne kwaliteits- en risicoscan voor product, privacy en wetenschappelijke vervolgstappen.

---

## 1. Samenvatting in gewone taal

Lobsy biedt kandidaten vier gratis **Quick-Scans** (competenties, beroepsinteresses/RIASEC, cultuur & persoonlijkheid, waarden) plus optionele **diepteanalyses** (150–200 items). Antwoorden zijn 5-punts Likert-stellingen; schalen worden gemiddeld en omgerekend naar **0–100%**. Resultaten voeden **Mijn DNA** (vaste B1-zinnen), **tags** (drempel ≥70%) en het **vacature-matchpercentage** (multidimensionaal model, drempel Top-10 vaak 60%).

**Sterk:** transparante opslag in code, reverse items, duidelijke scheiding free/deep, geen ruwe antwoorden naar werkgevers (privacy-copy), wetenschappelijke bronvermelding in UI, diepe versies met veel items per domein.

**Zwak voor COTAN-niveau:** beperkte items per schaal in Quick-Scans (4–5), **geen normpopulatie**, **geen gepubliceerde betrouwbaarheid/validiteit** op Lobsy-data, competentieschaal is **werkplek-label** (niet 1:1 Mini-IPIP), vacature-profielen worden **afgeleid** uit tekst/sector, onboarding-**mini** (21 vragen) ≠ volledige Quick-Scan, **pl/ro/ar tonen Nederlandse itemteksten**, AI-rapporten na diepteanalyse.

| COTAN-criterium | Oordeel | Kern |
|-----------------|---------|------|
| 1. Uitgangspunten testconstructie | **Onvoldoende** | Constructen benoemd maar niet volledig operationeel gelijk aan standaardinstrumenten |
| 2. Kwaliteit testmateriaal | **Onvoldoende** | Korte schalen, enkele dubbelzinnige items; vertalingen deels incompleet |
| 3. Handleiding | **Onvoldoende** | Geen aparte testhandleiding voor professionals |
| 4. Normen | **Niet te beoordelen / onvoldoende** | Geen normtabellen; ruwe % zonder referentiegroep |
| 5. Betrouwbaarheid | **Niet te beoordelen** | Geen echte responsdata in deze run; N≥300 nodig |
| 6. Begripsvaliditeit | **Onvoldoende** | Geen factor- of convergentiestudies gepubliceerd |
| 7. Criteriumvaliditeit | **Onvoldoende** | Match% niet gekoppeld aan arbeidsuitkomsten gevalideerd |
| Fairness / bias | **Onvoldoende** | Taal-fallback, geen DIF; inferentie vacatures |
| AI Act (high-risk readiness) | **Gedeeltelijk** | Consent aanwezig; uitleg menselijke regie kan scherper |

---

## 2. Inventaris (code & seed)

Machine-leesbare lijst: [`data/inventory.json`](./data/inventory.json) ( gegenereerd met [`scripts/extract_inventory.py`](./scripts/extract_inventory.py) ).

### 2.1 Testoverzicht

| Test | Free (Quick-Scan) | Deep (betaald) | Model (claim in product) | Bron in code |
|------|-------------------|----------------|--------------------------|--------------|
| Competentie | 25 items, 5×5 schalen | 150 items, 30×5 Big Five domeinen | Big Five / Mini-IPIP | `AssessmentTestCatalog.cs` L14–26, `CompetencyTestCatalog.cs` |
| Beroepen (RIASEC) | 25 (5R + 4×I/A/S/E/C) | 200 items | Holland RIASEC | `CareerTestCatalog.cs`, `AssessmentTestCatalog.cs` L27–40 |
| Cultuur & persoonlijkheid | 18 items | 150 items | OCP + IPIP-facetten | `CulturePersonalityCatalog.cs`, `AssessmentTestCatalog.cs` L41–54 |
| Waarden | 25 items, 5×5 | 150 items | Schwartz (werkplek-drivers) | `SchwartzValuesCatalog.cs`, `AssessmentTestCatalog.cs` L55–68 |

Metadata lengte/doorlooptijd: `Jobsy.Core/Rules/AssessmentTestCatalog.cs` (regels 11–68).

### 2.2 Schalen en items per schaal (Quick-Scan)

**Competentie** (`CompetencyTestCatalog.cs` L19–88): Samenwerken, Resultaatgerichtheid, Stressbestendigheid, Innovatie, Extraversie — elk **5 items**, waarvan **2 reverse** per schaal (behalve Extraversie: 3 forward, 2 reverse). Matching gebruikt alleen de eerste vier (`CategoryCodes` L33–39); Extraversie is tag/DNA (`L141–142`).

**Beroepen:** zes RIASEC-types; Realistic **5**, overige **4** (`CareerTestCatalog.cs` L34–64, comment L35–36).

**Cultuur:** 6 cultuurdimensies × 2 items + 5 persoonlijkheidsfacetten (Openness **2**, overige **1**) = 18 (`CulturePersonalityCatalog.cs` L47–77). Persoonlijkheidsfacetten hebben **geen reverse items** in de free scan (L71–76).

**Waarden:** Autonomy, Connection, Achievement, Stability, Impact — elk **5 items**, 2 reverse (`SchwartzValuesCatalog.cs` L29–56).

**Diepteanalyse:** 150/200/150/150 unieke stems (`DeepAnalysisCatalog.cs` L5–11); competentie-items in NL hardcoded in `DeepAnalysisCompetenceItems.cs` (geen vertaalsleutels).

### 2.3 Onboarding-mini (≠ volledige Quick-Scan)

Wizard **21 vragen** totaal (`OnboardingImpressionLibrary` / tests: `CandidateOnboardingWizardTests.cs` L37):  
- Competentie: item-id’s **1, 6, 11, 16, 21** — één per schaal (`OnboardingWizardCatalog.cs` L22–26)  
- Beroepen: **1, 6, 10, 14, 18, 22** (`L28–32`)  
- Cultuur: **1, 3, 5, 7, 11** — Innovation dimension **weggelaten** (`L34–38`)  
- Waarden: **1, 6, 11, 16, 21** (`L50–54`)

Provisional scoring: partial JSON → `Score()` + bij cultuur **55% fill** op ontbrekende dimensies (`ProvisionalAssessmentScores.cs` L99–101, L151–155). Dit **verhoogt match/DNA vóór voltooide tests** (`CandidateOnboardingService.cs` via `Resolve*`).

### 2.4 Antwoordformaat & scoring

- Likert **1–5**, labels via `Competency.Likert.*` (`LikertAnswerJson.cs` L8–9, `CompetencyTestCatalog.cs` L103–104).  
- Reverse: `LikertMax + LikertMin - raw` → 6−raw (`CompetencyTestCatalog.cs` L156).  
- Schaalscore %: gemiddelde gescoorde items → `(avg−1)/4×100`, afgerond (`LikertAnswerJson.cs` L92–104).  
- **Geen weging** per item; **geen normtransformatie**.  
- Tags: schalen ≥ **70%** (`CompetencyTestCatalog.cs` L191–200; RIASEC top-3 `CareerTestCatalog.cs` L167–173).  
- DNA zinnen: drempels **70 / 45** (`DnaSummarySentences.cs` L10–11); cultuur DNA binaire drempel **60** (`L80`).

### 2.5 Koppeling DNA ↔ scores ↔ match%

**Mijn DNA:** dominante schaal per test → vaste NL-zin (`DnaSummarySentences.cs`; gebruik o.a. in Kompas-services). Geen LLM voor DNA-samenvattingen.

**Match%:** `ProfileVacancyMatchCalculator.Calculate` (`ProfileVacancyMatchCalculator.cs` L25–167):

- Basis: reistijd, uren, dagdelen, ervaring/opleiding (`L86–121`).  
- Competenties: `VacancyCompetencyProfile.Fit01` — ratio kandidaat/target per schaal, gemiddeld (`VacancyCompetencyProfile.cs` L65–80); targets **geïnferd** uit worktype + keyword-boost (`L21–62`).  
- Cultuur/persoonlijkheid: blend in competency01 (`ProfileVacancyMatchCalculator.cs` L35–48).  
- Waarden: `SchwartzValuesFitRules.Fit01` (`L51–57`).  
- RIASEC + beroepen-kompas: gewichten o.a. `InterestWeightQuickScan` 0.25 (`L15–18, L87–96`).  
- Weergave: kleur **≥70** groen (`MatchScoreCalculator.cs` L10); Top-10 filter **≥60** (`ProfileVacancyMatchCalculator.cs` L13, L188).  
- Cultuurfit kan totaalscore **herwegen** (`CultureFitBuilder.TotalScoreWeight`, L134–136).

**Belangrijk constructiepunt:** match% is een **holistisch productmodel**, geen pure testscore; vacature-eisen zijn deels **heuristisch**, niet empirisch genormeerd.

### 2.6 Korte vs lange versie — meten ze hetzelfde?

| Vergelijking | Zelfde items? | Zelfde construct? |
|--------------|---------------|-------------------|
| Mini (wizard) vs Quick-Scan | Deelset (1 item/schaal) | **Nee** — mini is te kort voor betrouwbare schaal; cultuur mist Innovation |
| Quick-Scan vs Deep | Overlappend thema, andere stems/lengte | **Hypothese**, niet empirisch aangetoond in Lobsy |
| Competentie Q21–25 | Was legacy RIASEC; nu Extraversie | Migratie gedocumenteerd (`CompetencyTestCatalog.cs` L175–178) |

Empirische mini–full vergelijking: script [`analyze_responses.py`](./scripts/analyze_responses.py) (sectie `mini_vs_full`); vereist echte data.

---

## 3. Kwaliteit itemteksten (steekproef)

Beoordelingskader: B1 NL, geen dubbele barreling, geen leading, geen jargon, consistentie nl/en/pl/ro/ar.

### 3.1 Positief

- Werkplekcontext Den Haag/Westland in diepe competentie-items (`DeepAnalysisCompetenceItems.cs` L5–6).  
- UI vermeldt reverse items (`UiStringsCompetencies.cs` — `Competency.ScienceNote`).  
- EN vertaling aanwezig voor Quick-Scan items (`UiStringsCompetencies.cs` `En()` blok, o.a. L535+).

### 3.2 Aandachtspunten (met voorbeelden)

| Issue | Voorbeeld | Locatie |
|-------|-----------|---------|
| **Dubbel/conceptueel breed** | “Ik kan meerdere dingen tegelijk aan zonder in paniek te raken.” (taken vs emotie) | `Competency.Q13` → `UiStringsCompetencies.cs` L86 |
| **Absoluut taalgebruik** | “Ik luister **eerst** naar anderen **voordat** ik mijn mening geef.” | `Competency.Q03` |
| **Construct shift** | Quick-Scan “Innovatie” vs deep “Openheid” — gerelateerd maar niet identiek | `CompetencyTestCatalog` vs `DeepAnalysisCompetenceItems` |
| **Cultuur: 1 item per facet** | Extraversion/Agreeableness/… slechts 1 item in free scan | `CulturePersonalityCatalog.cs` L71–76 |
| **Vertaling pl/ro/ar** | Items = **Nederlands** (fallback) | `UiStringsCompetencies.cs` L16–18 `Merge(pl/ro/ar, Nl())` |
| **RIASEC ongelijke lengte** | Realistic 5, Social 4 — scoringsgemiddelde ok, maar precision verschilt | `CareerTestCatalog.cs` |

Aanbevolen: professionele vertaling + back-translation; itemreview door arbeidspsycholoog; B1-check op diepe 150/200 stems (nu NL-only strings).

---

## 4. Empirische analyse

### 4.1 Data in deze run

- **Geen productie-DB** en **geen lokale demo-DB met echte responsen** in de cloud agent-omgeving.  
- **`data/synthetic_demo_report.json`**: alleen om het analyse-script te valideren (`--synthetic`, N=500). **Gebruik deze alfa’s/ correlaties niet** voor kwaliteitsclaims — de generator is niet gekalibreerd op Lobsy-respondenten.

### 4.2 Minimum N (richtlijn)

| Doel | Minimaal N (richtlijn) |
|------|------------------------|
| Cronbach α per schaal (5 items) | **≥300** afgeronde Quick-Scans |
| Factoranalyse / CFA | **≥500** |
| DIF per taal (nl/en/pl/ro/ar) | **≥200 per taalgroep** |
| Test–retest (2–4 weken) | **≥100** |
| Criteriumvaliditeit (hired / 90-dagen behoud) | **≥100–300** gekoppelde cases |
| Convergentie vs COTAN-instrument (bijv. IPIP-20, SDS) | **≥150** |

### 4.3 Uitvoering op echte (geanonimiseerde) data

1. SQL-export: [`scripts/export_anonymized_responses.sql`](./scripts/export_anonymized_responses.sql)  
2. Analyse: `python3 docs/psychometrie/scripts/analyze_responses.py --csv responses.csv --out report.json`  
3. Rapporteer α, ω, item-rest, plafond-effecten, inter-scale correlaties, mini–full r.

---

## 5. COTAN-criteria (detail)

### Criterium 1 — Uitgangspunten testconstructie: **Onvoldoende**

**Evidence:** Theoretische labels (Big Five, RIASEC, Schwartz, OCP) staan in `AssessmentTestCatalog.cs` L21–66, maar operationele definities zijn **werkplek-herschrijvingen** (bijv. Samenwerken ≈ Agreeableness-werk, `CompetencyTestCatalog.cs` L7–8). Geen document met expliciete constructdefinitie, geen item-writing protocol, geen blue-print per facet.

**Fixes:** Constructdocument (1 pagina per test); mappingtabel construct → item → vacature-inferentie; peer review door GZ-/ arbeidspsycholoog.

### Criterium 2 — Kwaliteit testmateriaal: **Onvoldoende**

**Evidence:** 4–5 items/schaal (Quick-Scan); cultuur-personality ongelijk; diepe items niet via vertaalpipeline.

**Fixes:** Uitbreiden naar ≥8–10 items/schaal of officieel verkorte versie valideren; itemanalyse (p-values, rest-correlaties) na N≥300; itemrewrite lijst uit sectie 3.

### Criterium 3 — Handleiding: **Onvoldoende**

**Evidence:** Gebruikerscopy in UI (`Competency.ScienceNote`, privacy) — geen handleiding voor afnemer/beoordelaar met norminterpretatie, beperkingen, forbidden uses.

**Fixes:** Testhandleiding PDF: doel, doelgroep, admin, scoring, foutmarges, “geen selectiebesluit alleen op test”, FAQ.

### Criterium 4 — Normen: **Onvoldoende / niet te beoordelen**

**Evidence:** Scores zijn ruwe percentiles zonder referentiepopulatie (`LikertAnswerJson.ToPercent`). Vacature-targets zijn defaults 60 + heuristiek (`VacancyCompetencyProfile.cs` L36–41).

**Fixes:** Normeringstudie (NL werkzoekenden, stratificatie leeftijd/opleiding/regio); publicatie normtabellen; scheiding “ruwe score” vs “normscore” in UI.

### Criterium 5 — Betrouwbaarheid: **Niet te beoordelen** (in deze run)

**Evidence:** Geen echte data; 5-item schalen verwacht α ≈ .65–.75 **als** items homogeen zijn — moet gemeten worden.

**Fixes:** Interne consistentie + test–retest; rapporteer per schaal en per taalversie.

### Criterium 6 — Begripsvaliditeit: **Onvoldoende**

**Evidence:** Geen CFA/EFA op Lobsy-data; geen convergentie met bestaande instrumenten; RIASEC Quick-Scan ≠ SDS lengte.

**Fixes:** CFA per test; correlatie Mini-IPIP / RIASEC short forms; known-groups (beroepsrichting).

### Criterium 7 — Criteriumvaliditeit: **Onvoldoende**

**Evidence:** Match% (`ProfileVacancyMatchCalculator`) niet gevalideerd tegen instroom, tevredenheid, retention; `Application.MatchPercent` kan null zijn (`ApplicationRules.cs`).

**Fixes:** Prospective study: match% decielen vs hire/90d; calibration plot; eventueel herkalibratie gewichten.

---

## 6. Fairness, bias & taal

- **Taal:** pl/ro/ar krijgen **Nederlandse** testitems (`UiStringsCompetencies.cs` L16–18) → systematische **begripsbias** voor niet-NL sprekers.  
- **Leeftijd:** geen aparte normen; jongeren (<16) hebben parental consent flow (`User.cs` L44–48) — goed, maar geen validatie itembegrip.  
- **Geslacht:** geen optionele DIF-analyse mogelijk zonder veld (niet op `User` entity).  
- **Vacature-inferentie:** keywords kunnen sectorstereotypen versterken (`VacancyCompetencyProfile.cs` L52–56).  
- **Provisional scores:** neutral fill 55% (`ProvisionalAssessmentScores.cs` L11) kan matches **kunstmatig middelmatig** maken vóór voltooide tests.

**Fixes:** Echte vertalingen; DIF (Mantel-Haenszel / IRT-LR) op taal en leeftijd; audit inferentieregels; badge “voorlopige indruk” blijft zichtbaar (`Onboarding.ProvisionalBadge`).

---

## 7. AI Act & high-risk readiness (orientation)

Lobsy gebruikt tests + **AI** (diepteanalyse-rapport, compass, carrièrepad — o.a. `OpenAiCompetenceDeepReportAiService`, `CareerCompassGenerationService`). Werkgeversmatching kan impact hebben op **toegang tot arbeid** → prepare for **high-risk** obligations:

| Thema | Huidige staat | Aanbeveling |
|-------|---------------|-------------|
| Transparantie | Science notes, privacy copy | Uitbreiden: hoe match% tot stand komt (gewichten in begrijpelijke taal) |
| Uitlegbaarheid | Regels + `Why`/`Gaps` teksten (`ProfileVacancyMatchCalculator.cs` L312–564) | Menselijk reviewbare documentatie; geen black-box voor enkel score |
| Menselijk toezicht | Werkgever ziet geen ruwe antwoorden | Beleid: test **niet** enige sollicitatiegrond; HR-training |
| Consent | `TestAiConsentAt` (`User.cs` L40–42) | Versioneer; scheid test-only vs AI-report |
| Data minimization | AVG-richtlijnen in repo | Behouden; log geen ruwe Likert in PlatformLogs |
| Bias monitoring | Ontbreekt empirisch | Periodieke DIF + outcome monitoring |

---

## 8. Concreet validatieplan (voor Dennis / universiteit)

1. **Data-export** (SQL-script) → CSV; alleen gehash respondent_id.  
2. **Descriptief:** N per test/status/taal; itemfrequenties.  
3. **Betrouwbaarheid:** α, ω, item-rest; verwijder/split items < .30 rest.  
4. **Structuur:** EFA/CFA Quick-Scan vs theorie; multi-group op taal.  
5. **Mini vs full:** correlatie per schaal; besluit: mag mini match beïnvloeden?  
6. **Convergent:** parallel admin IPIP-20 + RIASEC short form (subsample).  
7. **Criterium:** match% vs sollicitatie → hire → 90 dagen (met toestemming).  
8. **Fairness:** DIF taal/leeftijd; review vacature-inferentie.  
9. **Documentatie:** testhandleiding + normen + release notes bij itemwijziging.

---

## 9. Bijlagen

- Inventaris JSON: [`data/inventory.json`](./data/inventory.json)  
- Synthetische demo (alleen scripttest): [`data/synthetic_demo_report.json`](./data/synthetic_demo_report.json)  
- Scripts: [`scripts/`](./scripts/)

---

*Einde rapport — geen officiële COTAN-uitspraak.*
