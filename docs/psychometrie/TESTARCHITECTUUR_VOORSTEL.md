# Voorstel: compacte testarchitectuur Lobsy (psychometrie & matching)

**Datum:** 13 oktober 2026  
**Status:** Architectuurvoorstel (documentatie only)  
**Doel:** Minder overlap tussen kandidaat-assessments, duidelijke constructen voor matching, COTAN-conservatieve lijn (geen normclaims zonder data).  
**Relatie andere PR’s:** psychometrische review [PR #645](https://github.com/dennisbemer1007-pixel/Jobsy/pull/645) (`docs/psychometrie/*`); implementatie Quick-Scan v2 [PR #647](https://github.com/dennisbemer1007-pixel/Jobsy/pull/647) (`TestsV2Enabled`, itembanks 40/48/24/40). Dit document **vult #645 aan met een merge-/productarchitectuur** en sluit aan op bestaande code (`ProfileVacancyMatchCalculator`, `VacancyCompetencyProfile`, paspoort-voorkeuren).

**Disclaimer:** Geen officiële COTAN-beoordeling. Matchpercentages zijn **oriënterend** tot criteriumvalidatie (uitkomsten op het werk) is uitgevoerd (`VALIDATIE_STUDIEPLAN.md` in PR #645).

---

## 1. Dekkingsmatrix (construct → huidige tests → overlap)

Legenda kolommen: **Primair** = hoofdmeting; **Secundair** = afgeleid/heuristisch; **Gap** = niet of nauwelijks gemeten.

| Construct | Wat we meten (operationeel) | Huidige bron (v1) | Diepte / items | Overlap & doublures |
|-----------|----------------------------|-------------------|----------------|---------------------|
| **Persoonlijkheid / Big Five** | OCEAN-facetten in werktaal | Competentie Quick-Scan (25): 4 “werkcompetenties” + Extraversie (`CompetencyTestCatalog`) | Deep 150 IPIP-achtig (`DeepAnalysisCatalog`) | **Zwaar:** Cultuur-scan 18 items bevat 5 IPIP-facetten (`CulturePersonalityCatalog` Q13–18). Extraversie dubbel (competentie + cultuur). Competentie-labels ≠ pure Mini-IPIP-schalen maar wel Big Five-claim in UI. |
| **Interesses / beroepsrichting (RIASEC)** | Holland-types, tags, kompas | Beroepentest 25 (`CareerTestCatalog`) | Deep 200 | Weinig overlap met persoonlijkheid; **wel** kruislink via `CulturePersonalityFitRules.NeededFacets(occupation)` en beroepen-kompas → vacaturetitel-heuristiek. |
| **Waarden (Schwartz)** | 5 workplace drivers | Waardenscan 25 (`SchwartzValuesCatalog`) | Deep 150 | **Conceptueel** overlap **Autonomie** (waarden) vs **Autonomy** (cultuur). Achievement ↔ Resultaatgerichtheid (competentie). |
| **Cultuurfit (voorkeur organisatie)** | 6 cultuurdimensies (OCP-achtig) | Cultuur & persoonlijkheid 18: 6×2 cultuuritems | Deep 150 (6×15 + facetten) | **Dubbel met Big Five** in dezelfde test. Werkgever-kant: 3–5 **culture pillars** op vacature (`CulturePillarCatalog`) → gemapt op **competentiescores**, niet op cultuurscan (`CultureFitBuilder`). |
| **Competenties (person–job)** | Samenwerken, Resultaat, Stress, Innovatie (0–100) | Zelfde competentietest | Vacature: `VacancyCompetencyProfile.Infer` (sector + tekst) | Competenties zijn **Big Five in andere labels** (A/C/ES/O mapping in catalog-comment). Matching blend: 75% competency fit + 25% personality/culture als cultuurscan compleet is (`ProfileVacancyMatchCalculator` L29–48). |
| **Cognitief / leervermogen** | — | **Gap** | — | Geen apart instrument; deep PDF noemt IPIP-normen, geen capaciteitentest. |
| **Werkcontext-voorkeuren** (tempo, fysiek, binnen/buiten, structuur, team/solo, klantcontact, ploegen) | Deels profiel, deels inferentie | Paspoort **SharedWorkPreferences** (`WorkPreferenceCatalogs`: indoor/outdoor/physical/pace) + beschikbaarheidsmatrix + reistijd | Geen test | **Niet in match%** op banenkaart (alleen reis/uren/dagdelen in `MatchScoreCalculator`). Team/solo/klant/ploeg vooral via vacaturetekst → `VacancyCompetencyProfile` / RIASEC-infer. Cultuur **Collaboration** raakt team/solo. |
| **Motivatie / drijfveren** | Schwartz + UI “drijfveren” | Waardenscan (+ marketing copy) | Deep waarden | **Merge-kandidaat:** motivatiestellingen overlappen waarden; aparte “motivatietest” niet nodig. |
| **Situationeel oordeel (SJT)** | — | **Gap** | — | Geen SJT; wel barrier checks (`VacancyBarrierCatalog`) bij sollicitatie. |
| **Praktische constraints / deal-breakers** | Rijbewijs, opleiding, uren, reistijd, legal tasks, certificaten | Profiel + vacaturevelden; `MatchScoreCalculator` + `VacancyBarrierCatalog` | Hard filter deels | Geen Likert-test; **filter** in matching en sollicitatie. |
| **Taalsniveau** | — | **Gap** (wel meertalige UI v2 in #647) | — | Geen CEFR-veld gekoppeld aan match. |

### 1.1 Itembudget vandaag (kandidaat)

| Laag | Competentie | Beroepen | Cultuur | Waarden | **Totaal** |
|------|-------------|----------|---------|---------|------------|
| Onboarding-mini | 5 | 6 | 5 | 5 | **21** (`OnboardingWizardCatalog`) |
| Quick-Scan “Full” | 25 | 25 | 18 | 25 | **93** |
| Betaalde diepte | 150 | 200 | 150 | 150 | **650** (niet allemaal verplicht) |

**v2 (PR #647, flag uit):** 40 + 48 + 24 + 40 = **152** Quick-Scan-items (+ diepte ongewijzigd versioning).

### 1.2 Waar matching nu op leunt (`ProfileVacancyMatchCalculator`)

| Signaal | Bron kandidaat | Bron vacature | Rol in score |
|---------|----------------|---------------|--------------|
| Reis / uren / dagdelen | `MatchScoreInput` | Vacature schema | **Gewogen basis** (~25–40% van “rest” na interesse) |
| Ervaring / opleiding / rijbewijs | Profiel | Vacature-eisen | Experience-blok (~15–20%) |
| Competenties | Competentietest | `VacancyCompetencyProfile` infer (+ optioneel expliciet) | ~20% van rest; **geblend** met cultuurpersoonlijkheid + waarden |
| Interesse | RIASEC + beroepen-kompas | `VacancyRiasecProfile` + occupation fit | 25–38% (hoger na deep career) |
| Cultuurfit (pillars) | Competenties + cultuurscan | `CulturePillarsJson` | Extra **12%** op totaal na hard gates (`CultureFitBuilder.TotalScoreWeight`) |

**Observatie:** vier aparte tests voeden één “competency01”-kanaal; kandidaat ziet vier menu’s maar de engine **stapelt** persoonlijkheid, cultuur, waarden en werk-competenties.

---

## 2. Voorstel: lean architectuur (merge & scheiding test vs profiel)

### 2.1 Ontwerpprincipes

1. **Eén construct = één schaalbank** — geen Big Five in twee tests.
2. **Werkcontext = voorkeurprofiel**, geen Likert-battery (iconen/korte keuzes, B1, vertaalbaar).
3. **Diepte = betrouwbaarheid**, niet vier losse upsells op hetzelfde construct — max. **twee betaalde dieptes** (Persoon+Werkstijl gecombineerd; Carrière apart).
4. **COTAN-conservatief:** behoud **≥8 items per schaal** in de “volledige” free scan waar mogelijk (Mini-IPIP-norm); kortere lagen expliciet **indicatief** (`TestDepthRules` / `AssessmentMatchEvidence` in #647).
5. **Fairness:** B1 NL + volledige vertaling itemteksten (lessen uit #645); cognitieve screener **non-verbaal**, nooit sollicitatie-gate.
6. **Geen stille migratie** van scores — `QuickScanVersion` / assessmentversie (zoals #647).

### 2.2 Voorgestelde modules (4 metingen + 2 profielblokken)

| Module | Vervangt | Quick items | Full free | Diepte (betaald) | Tijd quick (indicatief) |
|--------|----------|-------------|-----------|------------------|-------------------------|
| **A. Persoon & werkstijl** | Competentie + Big Five-deel cultuurscan | 30 (6× O,C,E,A,ES) | 30 | 120 (24×5) | ~4 min |
| **B. Beroepen-kompas (RIASEC)** | Beroepentest | 24 (4×6 types) | 24 | 120 | ~4 min |
| **C. Team- & werkcultuur** | Cultuurscan **zonder** facetten | 12 (2×6 OCP-dims) | 12 | 72 (12×6) | ~3 min |
| **D. Waarden & motivatie** | Waardenscan (+ motivatiecopy) | 20 (4×5 drivers) | 20 | 100 (20×5) | ~4 min |
| **E. Werkcontext-profiel** | Deel onboarding + paspoort | **0 test** | 8–12 **keuzevelden** | — | ~2 min |
| **F. Praktische fit** | Profiel/wizard | **0 test** | bestaande velden | — | (deels al ingevuld) |

**Totaal free Likert:** 30+24+12+20 = **86** (vs. 93 v1 / 152 v2) — **minder vragen**, **minder overlap**.

**Onboarding-mini (voorstel):** **14 items** + werkcontext-profiel: 3 persoon (top facetten), 6 RIASEC (1 per type), 2 cultuur (Autonomy + Collaboration), 3 waarden (Autonomy, Connection, Achievement) — Innovation/cultuur Innovation expliciet in mini opnemen (fix t.o.v. huidige omit in wizard).

#### Mapping module A → huidige matching

- Publiceer **zowel** ruwe OCEAN-percentages **als** afgeleide werklabels (Samenwerken, Resultaat, Stress, Innovatie) via vaste regressie/gewichten uit deep IPIP-bank — **één bron**, geen dubbele vragenlijst.
- `VacancyCompetencyProfile` blijft werken op de vier werkdimensies; employer kan targets overschrijven.

#### Module E — werkcontext (geen test)

Vaste velden (uitbreiding `WorkPreferenceCatalogs` + vacature-spiegel):

| Veld | Kandidaat | Vacature / team |
|------|-----------|-----------------|
| Tempo | `PaceCodes` | “Rustig / normaal / piek” (enum) |
| Fysiek | bestaand | zwaarte tilwerk / staan / lopen |
| Binnen/buiten | bestaand | % binnen (slider 0–100) |
| Team vs solo | afgeleid uit Collaboration + shift type | “Team / zelfstandig / gemengd” |
| Structuur | Flexibility-dimension | “Vaste procedures / mix / veel vrijheid” |
| Klantcontact | enum (veel/weinig/geen) | uit functie + pillar “samen” |
| Ploegen | dagdelen-matrix | `LegalTaskFlags` + rooster |

Match: **filter** bij harde mismatch (bijv. kandidaat `rather-not` outdoor + vacature 100% buiten); **licht gewicht** (5–8% totaal) na validatie — start in pilot **alleen uitleg**, geen %.

#### Cognitief (optioneel, fase 3)

- **8–12 non-verbale items** (patroon/serie), totaal **indicatief**, label “oriëntatie, geen IQ-test”.
- Geen ranking-gate; alleen coaching + employer filter “liefst met begeleiding” optioneel.

#### Taal (fase 3)

- Zelfrapport CEFR of “Nederlands werktaal: makkelijk / met hulp / liever andere taal” — **filter**, geen psychometrische schaal tot vertaalde itembanken klaar zijn.

### 2.3 Overlap opgelost (samenvatting)

| Was dubbel | Wordt |
|------------|--------|
| Big Five in Competentie + Cultuur | Alleen module A |
| Innovatie (competentie) + Innovation (cultuur) | Innovatie in A; cultuur Innovation alleen organisatie-voorkeur in C |
| Extraversie dubbel | Eén schaal in A |
| Waarden Autonomie vs cultuur Autonomy | C = organisatie; D = persoonlijke waarden; match via **aparte** fit-formules (niet optellen tot één %) |
| Motivatie vs waarden | Eén module D |

### 2.4 Relatie Quick-Scan v2 (#647)

| Aspect | #647 (v2) | Dit voorstel |
|--------|-----------|--------------|
| Itemaantallen | 40/48/24/40 | 30/24/12/20 (+ profiel) |
| Construct-split | Nog 4 tests, cultuur bevat nog facetten | **Merge** persoon + competentie-labels |
| Versioning / flag | `TestsV2Enabled` | Behouden; **v3** = module-architectuur na pilot |
| Transparantie | UI evidence | Overnemen + per module “indicatief / volledig” |

Aanbeveling: **#647 als tussenstap** (betere items & fairness); **module-merge als v3** na psychometrische check op v2-data.

---

## 3. Werkgever- & vacaturekant (zelfde dimensies)

### 3.1 Korte vragenlijst (10–15 min, éénmalig per vestiging + per vacature-aanvulling)

| Blok | Vragen | Spiegelt module |
|------|--------|-----------------|
| **Teamcultuur** | 12 Likert (zelfde 6 dimensies als kandidaat C) | C |
| **Waarden nadruk** | 10 Likert (5 drivers, “wat is hier normaal belangrijk”) | D |
| **Werkcontext** | 8 enkelkeuze (tempo, fysiek, klant, ploeg, binnen/buiten) | E |
| **Competentie-profiel** | Optioneel: 4 sliders 0–100 **of** laat infer + 1 bevestigingsvraag | A (targets) |
| **Pillars (bestaand)** | Blijft 3–5 chips voor UX/copy | Koppeling naar C via `CulturePersonalityFitRules.PillarFit01` i.p.v. alleen competency mapping |

**Opslag:** `CompanyCultureProfile` uitbreiden met dezelfde dimensiecodes als kandidaat; vacature erft vestiging + overrides (JSON).

### 3.2 Gebruik in `ProfileVacancyMatchCalculator` (filter vs gewicht)

| Dimensie | Mechanisme | Default |
|----------|------------|---------|
| Legal / leeftijd / taak-vinkjes | **Hard filter** (`LegalEligible`) | Geen % als false |
| Rijbewijs / opleiding / barriers | **Hard filter** sollicitatie; zacht in profiel-match | Gap in uitleg |
| Max reistijd | **Hard** onder voorkeur; anders score 0 op travel-component | 40% in core-only match |
| Uren / dagdelen | **Filter** bij 0 overlap; anders gewogen | Core |
| RIASEC + occupation | **Gewicht** 25–38% | — |
| Werkcompetenties (4D) | **Gewicht** ~20% | `VacancyCompetencyProfile.Fit01` |
| OCEAN / persoon–rol | **Gewicht** 8–12% (apart tonen, niet in competency01 stoppen) | Nu verstopt in blend |
| Cultuur dimensies (C) | **Gewicht** 10%; **filter** als verschil >40 pp op PeopleFirst/Autonomy (instelbaar) | Nu deels via pillars |
| Waarden (D) | **Gewicht** 8–10%; **filter** alleen bij expliciete employer “must-have” (max 2 drivers) | Nu heuristic tekst |
| Werkcontext (E) | **Filter** hard mismatches; **gewicht** laag na pilot | Nog niet |
| Cultuurfit AI/pillars | **Gewicht** 12% totaal (bestaand) | `CultureFitBuilder` |
| Cognitief / taal | **Filter** optioneel employer; **geen gewicht** in % | — |

**Productregel:** max. **2 hard filters** uit psychometrie (rest = uitleg + ranking). Voorkomt “zwarte lijst”-gevoel en AVG-risico.

### 3.3 `VacancyCompetencyProfile` (verbetering)

- **Vandaag:** infer uit sector + keywords (`VacancyCompetencyProfile.cs`).
- **Voorstel:** volgorde **(1)** expliciete employer sliders → **(2)** teamcultuur → **(3)** infer fallback.
- Documenteer in vacature-UI: “Typisch voor deze functie — pas aan als je team afwijkt.”

---

## 4. Gefaseerde roadmap

### Fase 0 — Baseline (nu → acceptatie)

| Actie | Feature flag | Risico | Pilotdata |
|-------|--------------|--------|-----------|
| Merge docs #645 + dit voorstel | — | Geen | — |
| #647 review: v2 itembanks, `TestsV2Enabled` default **uit** | `TestsV2Enabled` | Meertaligheid / lengte | A/B: completion rate, tijd, drop-off |

### Fase 1 — Transparantie & scheiding scores (Q4 2026)

| Actie | Flag | Risico | Data |
|-------|------|--------|------|
| Match-uitleg splitst competency / cultuur / waarden / interesse | `MatchExplainV2` | UX-complexiteit | Klikgedrag “waarom match” |
| Indicatieve lagen (< Full count) verlagen match-evidence (#647 patroon) | `TestsV2Enabled` | Kandidaten verwachten hogere % | Vergelijk klachten / support |
| Employer teamcultuur 12-item (optioneel) | `EmployerCultureScan` | Invuldrift | N ≥ 30 vestigingen |

### Fase 2 — Module-architectuur v3 (2027)

| Actie | Flag | Risico | Data |
|-------|------|--------|------|
| Module A live; redirect oude competentie-URL | `AssessmentModulesV3` | Historische scores incomparable | Parallel run 3 maanden |
| Module C zonder facetten; migratie mapping tabel v1→v3 | idem | Tijdelijke dual write | Cronbach α per module (≥300 N) |
| Werkcontext in match (filter-only) | `WorkContextMatch` | False negatives | Employer feedback |

### Fase 3 — Validatie & optionele instrumenten (2027+)

| Actie | Flag | Risico | Data |
|-------|------|--------|------|
| Criteriumvalidatie match% vs 90-dagen behoud | — | Juridisch/selectie | Gestratificeerde steekproef |
| Non-verbale cognitieve screener | `CognitiveScreener` | Fairness / bias | DIF-analyse (#645 scripts) |
| Taalniveau filter | `LanguageFit` | Discriminatie | Alleen zelfrapport + vacature-eis |

### Risico’s (top 5)

1. **Construct drift** bij merge — mitigatie: vaste mappingstabel + geen herberekening oude scans.
2. **Te korte schalen** na merge — mitigatie: Full free ≥24–30 items per module; diepte voor rapporten.
3. **Match% verlies vertrouwen** — mitigatie: bands + “indicatief” + employer erkenning in UI (#647).
4. **Employer invullast** — mitigatie: infer defaults, 12-item max, hergebruik vestiging.
5. **AI-cultuurfit** (`CultureFitRefineWorker`) — mitigatie: lokale engine blijft leidend; AI alleen copy within clamp.

### Wat moet uit pilotdata komen vóór “v3 default aan”

| Vraag | Minimale N | Methode |
|-------|------------|---------|
| α / ω per module Full free | 300 afgerond per module | `analyze_responses.py` |
| Convergentie A vs oude competentie + cultuur facetten | 200 met beide versies | Correlaties + CFA (extern) |
| Match% vs sollicitatie / hire proxy | 500 koppels | Logistische regressie |
| DIF taal / opleiding | 300 per groep | Mantel-Haenszel |
| Invultijd & completion | Alle | Funnel analytics |

---

## 5. Beslispunten voor product & juridisch

1. **Eén “DNA”-dashboard** met 4 modules + werkcontext i.p.v. 4 losse testmerken.
2. **Pricing diepte:** bundel Persoon+Werkstijl i.p.v. apart competentie + cultuur deep.
3. **Positionering:** blijft **matching & loopbaanoriëntatie**, geen gecertificeerde selectietest — expliciet in consent & employer contract.
4. **Volgorde merge:** eerst data v2 (#647), dan v3 module-split — niet tegelijk.

---

## 6. Referenties (code)

| Onderdeel | Pad |
|-----------|-----|
| Match-calculator | `Jobsy.Core/Rules/ProfileVacancyMatchCalculator.cs` |
| Vacature-competenties infer | `Jobsy.Core/Rules/VacancyCompetencyProfile.cs` |
| Core reis/uren/dagdelen | `Jobsy.Core/Rules/MatchScoreCalculator.cs` |
| Test-metadata | `Jobsy.Core/Rules/AssessmentTestCatalog.cs` |
| Diepte-aantallen | `Jobsy.Core/Rules/DeepAnalysisCatalog.cs` |
| Onboarding-mini | `Jobsy.Core/Rules/OnboardingWizardCatalog.cs` |
| Werkcontext paspoort | `Jobsy.Core/Rules/WorkPreferenceCatalogs.cs` |
| Cultuur pillars werkgever | `Jobsy.Core/Rules/CulturePillarCatalog.cs` |

---

*Einde voorstel — alleen documentatie; geen wijziging productcode in deze PR.*
