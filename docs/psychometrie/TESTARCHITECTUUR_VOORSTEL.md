# Voorstel: compacte testarchitectuur Lobsy (psychometrie & matching)

**Datum:** 13 oktober 2026  
**Status:** Goedgekeurd tier-model — **v3 free tier geïmplementeerd** achter `TestsV2Enabled` (aparte PR); module-merge v3 architectuur (§2.2) blijft roadmap.  
**Doel:** Minder overlap tussen kandidaat-assessments, duidelijke constructen voor matching, COTAN-conservatieve lijn (geen normclaims zonder data).  
**Relatie andere PR’s:** psychometrische review [PR #645](https://github.com/dennisbemer1007-pixel/Jobsy/pull/645) (`docs/psychometrie/*`); implementatie Quick-Scan v2 [PR #647](https://github.com/dennisbemer1007-pixel/Jobsy/pull/647) (`TestsV2Enabled`, itembanks 40/48/24/40). Dit document **vult #645 aan met een merge-/productarchitectuur** en sluit aan op bestaande code (`ProfileVacancyMatchCalculator`, `VacancyCompetencyProfile`, paspoort-voorkeuren).

**Disclaimer:** Geen officiële COTAN-beoordeling. Matchpercentages zijn **oriënterend** tot criteriumvalidatie (uitkomsten op het werk) is uitgevoerd (`VALIDATIE_STUDIEPLAN.md` in PR #645).

**Eigenaar-prioriteit:** het matchpercentage moet **realistisch** voelen — geen “standaard 80%”, wel eerlijke spreiding, aparte **betrouwbaarheid/compleetheid**, en harde filters voor deal-breakers. Zie **§4**.

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

#### Eigenaar-tier (per test / module)

| Laag | Doel | Itembudget (richtlijn) | Betrouwbaarheid |
|------|------|------------------------|-----------------|
| **Onboarding** | Snel beeld + eerste match-hint | Mini per module (wizard; doel ~14–21 items totaal) | Expliciet **indicatief** |
| **FREE verdieping** | Volledige Quick-Scan per construct | **Max. 25 vragen per test** (niet cumulatief over modules) | Volledige free-laag waar ≥8 items/schaal haalbaar; anders indicatief |
| **PAID diepte** | Rapport + hogere match-evidence | **150–200 vragen** per dieptest (bundel Persoon+Werkstijl i.p.v. dubbele upsells) | Hogere confidence in UI; nog steeds geen normclaim zonder data |

**Spanning v2 vs tier:** huidige v2 Quick-Scans (**40 / 48 / 24 / 40**) overschrijden de **25 free**-grens per test. **Voorstel zonder v2-itembank te wijzigen in deze PR:**

1. **Productcopy + `AssessmentMatchEvidence`:** alles boven 25 beantwoorde items in de free-laag blijft **indicatief** voor match-confidence (cap zoals `MatchCalibrationV1` / `IndicativeFitCap`).
2. **v3 module-architectuur (§2.2):** free-Likert **30+24+12+20** herverdeeld zodat **geen module** de 25-grens overschrijdt (Persoon 30 → split onboarding 14 + free 25 via versioning).
3. **v2 als tussenstap (#647):** flag `TestsV2Enabled` uit default; pilot meet completion vs v1; marketing noemt v2 “uitgebreide scan”, match toont **indicatief** tot tier-fit is afgerond.
4. **COTAN-conservatief:** free-tier reliability **niet** gelijkstellen aan 40–48 item-schalen; diepte-rapporten blijven leidend voor interpretatie.

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

## 3. Werkgever- & vacaturekant (optioneel, zelfde dimensies als kandidaat)

**Eigenaar-requirement:** twee **optionele** invulmomenten — (a) cultuurfit op **bedrijf/team**-niveau (5–10 vragen), (b) **korte** aanvulling bij **vacature plaatsen** (werkcontext, gevraagde competenties, afwijkingen t.o.v. teamcultuur). Beide mappen op dezelfde dimensies als de kandidaat-modules (C, D, E, A). **Niet invullen = geldige keuze**; matching valt terug op infer (`VacancyCompetencyProfile`, tekst-heuristiek, pillars).

**Aansluiting huidige product (acceptatie):** vestiging heeft al optionele **cultuurscan** (12 Likert, 6 cultuurdimensies × 2) via `CultureScanSection.razor` → `CompanyCultureProfile`; registratie kent **Quick** (6 sliders, `CompanyCultureSources.Quick`). Vacature-flow heeft al **optionele team-override** met culture pillars (`CreateVacancy.razor`, `_teamOverride`). Dit voorstel **versmallt** de verplichte mentale load en maakt de koppeling naar `ProfileVacancyMatchCalculator` expliciet.

### 3.1 UX-principes (snel & optioneel)

| Principe | Gedrag |
|----------|--------|
| **Standaard = doorgaan** | Vacature publiceren zonder extra blok; geen modal-gate. |
| **Progressieve diepte** | Team: **6 vragen (~2 min)** volstaat voor match; link “+4 vragen voor scherper beeld” naar 10. |
| **Eén scherm per context** | Teamcultuur onder **Organisatie → Cultuur** (bestaand); vacature-aanvulling als **ingeklapte** accordeon “Match scherper stellen (optioneel)”. |
| **Herbruik** | Vacature toont samenvatting teamprofiel + “Wijkt deze functie af?” → alleen deltas invullen. |
| **Transparantie kandidaat** | Geen ruwe employer-antwoorden; wel “past bij teamcultuur” in match-uitleg. |
| **Read-only rollen** | Regio/lezen: profiel zien, niet wijzigen (bestaand patroon). |

**Vacature-wizard copy (NL):**

- Accordeon-titel: **“Match scherper stellen (optioneel, ±1 min)”**
- Subtekst: *“Lobsy schat dit al uit functie en branche. Vul alleen in wat afwijkt van jullie team — dan worden matches en uitleg preciezer.”*
- CTA skip: **“Overslaan — standaard inschatting gebruiken”** (primary path blijft publiceren).

**Teamcultuur copy:**

- *“Help kandidaten begrijpen hoe het bij jullie werkt. Zes korte stellingen zijn genoeg; uitgebreider kan later.”*

### 3.2 (a) Optionele vragenlijst bedrijf / team — **6 + 4 vragen**

**Formaat:** Likert 1–5 (*helemaal oneens* … *helemaal eens*), zelfde scoring als kandidaat (`CulturePersonalityCatalog` / `SchwartzValuesCatalog` → 0–100% per dimensie).

#### Kern (6 vragen, ~2 min) — `CompanyCultureSources.Quick` / team-default

| # | Dimensie (module) | Vraag (NL) | Code |
|---|-------------------|------------|------|
| T1 | Autonomy (C) | *“Bij ons mag je veel zelf beslissen hoe je je werk doet.”* | `Autonomy` |
| T2 | Informal (C) | *“We doen het informeel: voornaam, korte lijntjes, weinig protocol.”* | `Informal` |
| T3 | Collaboration (C) | *“We werken het liefst in een team en overleggen vaak.”* | `Collaboration` |
| T4 | Flexibility (C) | *“Taken en tijden wisselen regelmatig — flexibiliteit hoort erbij.”* | `Flexibility` |
| T5 | Innovation (C) | *“We proberen graag nieuwe manieren; verbeteren hoort bij de baan.”* | `Innovation` |
| T6 | PeopleFirst (C) | *“De mens staat voor het resultaat: we nemen tijd voor elkaar en voor klanten.”* | `PeopleFirst` |

#### Uitbreiding optioneel (+4 vragen, ~2 min) — wordt `Full` (max. 10 teamvragen)

| # | Dimensie | Vraag (NL) | Code |
|---|----------|------------|------|
| T7 | Achievement (D) | *“Presteren en doelen halen worden hier zichtbaar beloond.”* | `Achievement` |
| T8 | Stability (D) | *“Vaste afspraken en duidelijke regels geven ons rust op de werkvloer.”* | `Stability` |
| T9 | Connection (D) | *“Collegialiteit en erbij horen zijn minstens zo belangrijk als de taak zelf.”* | `Connection` |
| T10 | Impact (D) | *“We willen merken dat ons werk ertoe doet voor anderen of de omgeving.”* | `Impact` |

> **Nota:** T7–T10 meten **organisatienorm** (wat hier geldt), niet de persoonlijkheid van de invuller. Geen Big Five-vragen aan werkgever — persoon–rol fit blijft kandidaat-side (module A) + tekst-heuristiek.

**Opslag:** bestaande `CompanyCultureProfile` (percentvelden per dimensie); T1–T6 → cultuurpercentages; T7–T10 → optioneel `CompanyValuesProfile` (zelfde 5 drivers als kandidaat D) of JSON `EmployerValuesEmphasis`.

**Bestaande 12-item scan:** blijft beschikbaar als “Uitgebreid teamprofiel” voor werkgevers die meer willen; inhoudelijk overlap met T1–T6 (1 vraag per dimensie i.p.v. 2).

### 3.3 (b) Optionele vragenlijst bij vacature — **max. 8 korte items**

Alles in één ingeklapte accordeon; defaults = **erfenis teamprofiel** + **infer**. Alleen gewijzigde velden worden opgeslagen op de vacature (`VacancyCultureOverridesJson`, `VacancyWorkContextJson`, `VacancyCompetencyTargets` — namen voorstel).

#### Blok B1 — Werkcontext (module E, enkelkeuze, ~30 s)

| # | Veld | Vraag (NL) | Antwoordopties → codes |
|---|------|------------|-------------------------|
| V1 | Tempo | *“Hoe is het tempo in deze functie?”* | Rustig (`calm`) · Normaal (`norm-ok`) · Vaak piek/druk (`piek`) |
| V2 | Fysiek | *“Hoe fysiek is het werk?”* | Zittend/licht (`light`) · Veel staan/lopen (`standing`) · Tillen tot ~15 kg (`lifting-15`) · Zwaarder (`lifting-25`) |
| V3 | Binnen/buiten | *“Waar werk je vooral?”* | Binnen (`prefer-indoor`) · Mix (`mix`) · Buiten (`prefer-outdoor`) |
| V4 | Klantcontact | *“Hoeveel contact met klanten/gasten?”* | Veel · Af en toe · Weinig/geen |

Mapping kandidaat: `SharedWorkPreferences` + paspoort; **match:** hard filter bij sterke mismatch (bijv. kandidaat `rather-not` outdoor + vacature `prefer-outdoor`), anders geen score-impact in fase 1.

#### Blok B2 — Gevraagde competenties (module A → 4 werkdimensies, ~45 s)

| # | Vraag (NL) | UI |
|---|------------|-----|
| V5 | *“Hoe belangrijk is **samenwerken** in deze functie?”* | Slider 0–100, default = infer |
| V6 | *“Hoe belangrijk is **resultaat halen**?”* | idem |
| V7 | *“Hoe belangrijk is **rust onder druk**?”* | idem |
| V8 | *“Hoe belangrijk is **nieuwe oplossingen bedenken**?”* | idem |

**Snel-variant (1 vraag):** checkbox *“Gebruik Lobsy-inschatting uit functietitel en branche”* (aan = geen V5–V8 opslaan, infer only).

#### Blok B3 — Teamcultuur afwijking (module C, ~30 s)

| # | Vraag (NL) | UI |
|---|------------|-----|
| V9 | *“Wijkt de **sfeer in dit team** af van jullie algemene bedrijfscultuur?”* | Nee → erf `CompanyCultureScores` · Ja → toon 3–6 **delta-sliders** (zelfde T1–T6 dimensies, alleen afwijkende) |
| V10 | (alleen bij Ja) *“Welke 3 woorden passen het best bij **dit** team?”* | Hergebruik **culture pillars** (3 chips max) — bestaand UX |

Pillars blijven **marketing + `CultureFitBuilder`**; numerieke fit loopt via `CulturePersonalityScores` (team + delta).

### 3.4 Datastroom naar `ProfileVacancyMatchCalculator`

Logische resolutie (implementatie-voorstel; geen code in deze PR):

```
EffectiveVacancyCulture =
  ApplyVacancyDelta(CompanyCultureScores, VacancyCultureDelta)
  ?? InferFromPillars(CulturePillars)           // bestaand
  ?? null                                        // geen cultuurfit op C

EffectiveVacancyCompetencies =
  VacancyCompetencyTargets (V5–V8 ingevuld)
  ?? MapCultureToCompetencies(EffectiveVacancyCulture)  // optionele brug
  ?? VacancyCompetencyProfile.Infer(workTypes, title, description)  // FALLBACK

EffectiveVacancyValues =
  VacancyValuesOverride ?? CompanyValuesEmphasis (T7–T10)
  ?? SchwartzValuesFitRules.InferVacancyDrivers(title, description)   // FALLBACK

EffectiveWorkContext =
  VacancyWorkContextJson (V1–V4) ?? InferWorkContext(title, description, schedule)
```

**Invoer `ProfileVacancyMatchInput` (bestaand + uitbreiding):**

| Veld | Bron |
|------|------|
| `VacancyCompetencies` | `EffectiveVacancyCompetencies` |
| `CompanyCultureScores` | `EffectiveVacancyCulture` (niet alleen company-wide als vacature-delta) |
| `CulturePillars` | Vacature (override) of vestiging |
| `CandidateCultureScores` | Kandidaat module C |
| `CandidateCompetencies` | Kandidaat module A (werkdimensies) |
| `CandidateValuesScores` | Kandidaat module D |
| `Core` (reis/uren/dagdelen) | Profiel + vacature (bestaand) |

**Bestaande blend in calculator (v1):**

1. `competency01` = `VacancyCompetencyProfile.Fit01(kandidaat, EffectiveVacancyCompetencies)` — **fallback infer** als employer niets invulde.
2. Indien kandidaat-cultuur compleet: personality + `CultureFit01(kandidaat, EffectiveVacancyCulture)` → 75/25 mix in competency-kanaal (L29–48).
3. Waarden: `SchwartzValuesFitRules.Fit01(kandidaat, EffectiveVacancyValues)` → 78/22 blend (L51–57).
4. `CultureFitBuilder.Evaluate(pillars, kandidaat-competenties, kandidaat-cultuur)` — **12%** op totaal na hard gates; pillars komen van vacature (V10) of default.

**Werkcontext (V1–V4):** fase 1 **pre-filter** vóór `Calculate` (kandidaat ziet vacature wel/niet in top-10); fase 2 optioneel 5–8% gewicht.

**Hard gates (ongewijzigd):** reis, legal, rijbewijs, opleiding — employer-vragenlijsten vervangen deze niet.

### 3.5 Gebruik in `ProfileVacancyMatchCalculator` (filter vs gewicht)

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

### 3.6 `VacancyCompetencyProfile` (fallback-keten)

- **Vandaag:** infer uit sector + keywords (`VacancyCompetencyProfile.cs`).
- **Voorstel:** volgorde **(1)** vacature-sliders V5–V8 → **(2)** afgeleid uit teamcultuur T1–T6 → **(3)** infer fallback (titel, branche, omschrijving).
- UI: standaard checkbox “Lobsy-inschatting gebruiken”; alleen bij uitzetten sliders tonen.
- Match-uitleg tag: *“Gebaseerd op jullie ingevulde functieprofiel”* vs *“Typisch voor deze branche (inschatting)”* zodat werkgever en kandidaat weten welke bron geldt.

---

## 4. Audit match-realism (`ProfileVacancyMatchCalculator`)

De huidige **TotalPercent** is een gewogen gemiddelde (0–1) × 100, afgerond (`ProfileVacancyMatchCalculator.cs` L106–143), met optionele **+12% cultuur-pillars** (`CultureFitBuilder.TotalScoreWeight`, L154–157) en **+0–5 engagement** (`EngagementCatalog.MaxBonus`, L166–172). Drempels in product:

| Constante | Waarde | Betekenis in UI |
|-----------|--------|-----------------|
| `DisplayThreshold` | **60** | Top-10 banenkaart / profiel-widget: vacatures **onder 60%** verdwijnen uit ranking (`RankScored`, L215–224). |
| `MatchScoreWeights.StrongMatchThreshold` | **70** | “Groene” band — voelt als **sterke** match, terwijl 70% statistisch “C-voldoende” is. |
| `MatchScoreWeights.GuldenMiddenwegThreshold` | **50** | Sollicitatie-safety-net: onder 50% extra bevestiging (`GuldenMiddenwegRules`). |

### 4.1 Waar scores **omhoog** drukken (inflatie)

| # | Mechanisme | Code / gedrag | Effect |
|---|------------|---------------|--------|
| 1 | **Ontbrekende reis = volle score** | `MatchScoreCalculator.ScoreTravel`: geen minuten of max → **40/40 punten** (“neutral travel”, L124–127) | Kandidaat zonder locatie/reis krijgt **100%** reis-component. |
| 2 | **Ontbrekende uren = halve tot volle score** | Geen kandidaat-uren → **15/30** (L153–155); geen vacature-uren → **30/30** (L148–150) | Onvolledig profiel telt alsnog mee als “redelijke” fit. |
| 3 | **Flexibele vacature-roosters** | `VacancySchedule.FlexibleTimes` → dagdelen **30/30** neutraal (L179–181) | Veel vacatures scoren **max** dagdelen zonder overlap-check. |
| 4 | **Experience-baseline** | `ScoreExperience`: geen opleidingseis → **0,55–0,85** per subcomponent (L316–339); zelfde sector → **exact01 = 1** | Alleen sector-label “Horeca” vs “Horeca” geeft al hoge ervaringsscore. |
| 5 | **Competentie-fit eenzijdig** | `VacancyCompetencyProfile.Fit01`: `min(c/target, 1)` (L78–79) — **boven target geen bonus, onder target zacht** | Uniform hoge testscores (~80) lijken op elke vacature “geschikt”. |
| 6 | **Infer fallback overal** | Geen employer-input → infer targets + RIASEC-tags + waarden uit vacaturetekst (`SchwartzValuesFitRules.InferVacancyDrivers`) | Tekst-heuristiek **vermindert variatie** tussen vacatures. |
| 7 | **Stacking cultuur + waarden in competency-kanaal** | 75/25 personality, 78/22 waarden op `competency01` (L51–77) | Eén hoog kanaal tilt **20%** van rest-gewicht mee; kandidaat ziet niet wat wat doet. |
| 8 | **Cultuurfit-bonus** | Na voldoen hard gates: **12%** totaal opnieuw gemiddeld met pillar-fit (L145–157) | Kan **brede** matches over de 60-drempel tillen. |
| 9 | **Ranking cutoff 60** | Alleen ≥60 in Top-10 | UI toont **alleen** “oranje/groen” band; gebruiker ziet geen spreiding 30–55%. |
| 10 | **Geen aparte confidence** | `TotalPercent` is altijd “vol” | Ontbrekende tests worden **niet** als “onbekend” gelabeld. |

**Clustering:** unit tests verwachten bewust brede profielen **≥ 60%** (`MultidimensionalMatchingTests`, `CompetencyMatchingTests.Rank`) — product en tests zijn zo afgestemd dat “goede verhalen” zichtbaar blijven, wat **hoge baselines** bevestigt.

### 4.2 Voorstel: eerlijke kalibratie

**Scheiding weergave (verplicht in UI):**

| Veld | Betekenis |
|------|-----------|
| **Match % (fit)** | Geschatte fit **alleen over bekende dimensies**, geen neutrale vullers. |
| **Betrouwbaarheid % (confidence)** | Gewichtsfraction van dimensies die **daadwerkelijk** gemeten zijn (tests af, profielvelden ingevuld, employer-optioneel ingevuld). |
| **Label** | Bij confidence &lt; 50: *“Indicatief — vul X in voor scherper beeld”*; geen groene band. |

**Rekenregels (voorstel `MatchCalibrationV1`):**

1. **Geen neutrale 100%** — ontbrekende reis/uren/dagdelen: component **exclus** uit teller (hernormaliseer weights), confidence −Δ.
2. **Indicatieve testdiepte** (#647): `AssessmentMatchEvidence.Indicative` → cap component (bijv. max 0,65 × raw fit) + lagere confidence.
3. **Competentie-fit symmetrisch:** `1 − |c − t| / 100` per dimensie (straf voor over- én onderfit); target uit employer/vacature, anders infer.
4. **Gewichten (onderzoeksgedreven prior):** meta-analyses person–job fit (Conscientiousness sterkste Big Five predictor; interests matig; culture modest — o.a. Barrick & Mount; NIST/Schmidt voor g). Voor Lobsy **tot er geen cognitieve screener is**:

   | Laag | Dimensies | Gewicht fit | Rol |
   |------|-----------|-------------|-----|
   | 0 | Deal-breakers (legal, rijbewijs, harde barrier, reis **over** max, uren **0 overlap**) | — | **Filter** (vacature verborgen of rood) |
   | 1 | Conscientiousness/resultaat + rol-eisen (module A **C**, ervaring, opleiding) | **30%** | Taakprestatie-proxy |
   | 2 | Praktisch (reis, uren, dagdelen, werkcontext E) | **25%** | Arbeidsvoorwaarden |
   | 3 | Interesses RIASEC + beroepen-kompas | **20%** | Retention / motivatie |
   | 4 | Cultuur C + pillars | **15%** | Team-fit |
   | 5 | Waarden D | **10%** | Langer-termijn fit |
   | (later) | Cognitief non-verbaal (indicatief) | **≤10%** binnen laag 1, nooit filter | Alleen na validatie |

5. **Geen additieve bonuses** op totaal — engagement (+5) en pillar-fit **binnen** laag 4, max +3 punten op fit%, niet stapelen boven 100.
6. **Bands herkalibreren:** sterk ≥ **78**; goed **62–77**; twijfel **45–61**; laag &lt; 45. Top-10 drempel **55** i.p.v. 60, of dynamisch op percentiel in regio.
7. **Shrinkage bij lage confidence:**  
   `fit_display = round(100 × fit_raw × (0,55 + 0,45 × confidence))`  
   (confidence 0,22 → max ~64% van raw naar display; voorkomt 85% bij dun profiel).

**Hard filters (deal-breakers)** — blijven **vóór** fit-berekening:

- `LegalEligible == false`
- `TravelWithinPreference == false` (kandidaat-max bekend)
- `HoursOverlapHours == 0` (beide kanten ingevuld)
- `VacancyBarrierCheck` dealbreaker (diploma/certificaat)
- Optioneel werkgever: taal/rijbewijs “must”

### 4.3 Voorbeelden: huidige code vs voorstel (zelfde inputs)

Scores **huidige code** zijn nagebouwd met de **zelfde formules** als `ProfileVacancyMatchCalculator` + `MatchScoreCalculator` (Python-replica op acceptatie-branch, maart 2026; ter controle: `dotnet test --filter MultidimensionalMatchingTests` / `CompetencyMatchingTests` op CI). **Voorstel** = `MatchCalibrationV1` (§4.2) met confidence-gewichten per scenario.

| ID | Profiel (samenvatting) | **Huidig** | **Voorstel fit** | **Confidence** | Interpretatie |
|----|------------------------|------------|------------------|----------------|---------------|
| **S1** | Geen tests; reis & uren **onbekend**; zelfde sector (logistiek) | **85%** | **55%** | **22%** | Inflatie: neutrale reis (40/40) + flex dagdelen + sector-match → “groen terwijl DNA leeg”. |
| **S2** | Kandidaat **Horeca**, vacature **Zorg**; testscores overal ~80% | **76%** | **63%** | **62%** | Tests “goed genoeg” ondanks verkeerde sector; asymmetrische Fit01 + hoge experience-transferable. |
| **S3** | Retail × retail; sterke competenties; RIASEC enterprising; career deep | **89%** | **85%** | **92%** | Echte fit **mag** hoog; kleine correctie ( geen bonus-stacking ). |
| **S4** | Reis **90 min** vs max **20**; uren **8–12** vs vacature **32–40**; geen tests | **41%** | **30%** | **38%** | Rood blijft rood; confidence laag → geen valse hoop. |
| **S5** | “Broad admin” profiel (≈ `MultidimensionalMatchingTests`) — overdraagbare titel, hoge C, deep career | **87%** | **82%** | **88%** | Blijft **boven** drempel maar niet “bijna perfect”; uitleg “brede match” blijft, % iets bescheidener. |

**Takeaway:** onder huidige regels scoort **S1 (weinig data) hoger dan S2 (verkeerde sector)** — dat ondermijnt vertrouwen. Na kalibratie daalt vooral **dun bewijs** (S1), blijft **echte fit** (S3/S5) hoog maar niet op 90+.

### 4.4 Validatieplan (scores ↔ uitkomsten)

| Fase | Data | Metriek | Doel |
|------|------|---------|------|
| **A — Telemetrie** (direct) | Alle `ProfileVacancyMatch` + confidence | Distributie fit%; % per band; correlatie confidence ↔ ingevulde modules | Detecteer clustering &gt; 70% |
| **B — Proxies** (3–6 mnd) | Sollicitatie → uitnodiging → **ContactShared** | Lift top-deciel vs bottom; calibration plot | Fit% voorspelt **gesprek**, niet alleen klik |
| **C — Werkgever** | 1–5 ster “klopt deze match?” na unlock | Mean rating vs fit% | Face validity |
| **D — Uitkomst** (12+ mnd, AVG-safe) | **90-dagen behoud** / contract verlengd (flex) / “niet doorgestart” reden | HR logistic; Brier score | Criteriumvaliditeit |
| **E — A/B** | Oude vs `MatchCalibrationV1` | Sollicatie-rate, geen toename mis-hires (werkgever) | Productacceptatie |

Export: bestaande `docs/psychometrie/scripts/export_anonymized_responses.sql` uitbreiden met match-snapshots (geen PII). Rapportage kwartaal in `VALIDATIE_STUDIEPLAN.md`.

**Feature flags:** `MatchCalibrationV1` (rekenengine), `MatchShowConfidence` (UI), `MatchBands2027` (drempels).

---

## 5. Gefaseerde roadmap

### Fase 0 — Baseline (nu → acceptatie)

| Actie | Feature flag | Risico | Pilotdata |
|-------|--------------|--------|-----------|
| Merge docs #645 + dit voorstel | — | Geen | — |
| #647 review: v2 itembanks, `TestsV2Enabled` default **uit** | `TestsV2Enabled` | Meertaligheid / lengte | A/B: completion rate, tijd, drop-off |

### Fase 1 — Transparantie, confidence & kalibratie (Q4 2026) — **prioriteit eigenaar**

| Actie | Flag | Risico | Data |
|-------|------|--------|------|
| **MatchCalibrationV1**: geen neutrale 100% bij missende reis/uren; confidence % in UI | `MatchCalibrationV1` / `MatchShowConfidence` | Lagere Top-10 counts | Funnel + support tickets |
| Match-uitleg splitst competency / cultuur / waarden / interesse | `MatchExplainV2` | UX-complexiteit | Klikgedrag “waarom match” |
| Indicatieve lagen (< Full count) verlagen match-evidence (#647 patroon) | `TestsV2Enabled` | Kandidaten verwachten hogere % | Vergelijk klachten / support |
| Employer teamcultuur **6+4 optioneel** (T1–T10) + vacature-accordeon V1–V10 | `EmployerCultureScan` / `VacancyMatchOptional` | Invuldrift | Completion %; N ≥ 30 vestigingen |
| Telemetrie fit-distributie + calibration plot (§4.4 fase A) | — | — | Dashboard admin |

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
4. **Employer invullast** — mitigatie: infer defaults, **6 teamvragen default**, vacature-accordeon ingeklapt, hergebruik vestiging.
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

## 6. Beslispunten voor product & juridisch

1. **Eén “DNA”-dashboard** met 4 modules + werkcontext i.p.v. 4 losse testmerken.
2. **Pricing diepte:** bundel Persoon+Werkstijl i.p.v. apart competentie + cultuur deep.
3. **Positionering:** blijft **matching & loopbaanoriëntatie**, geen gecertificeerde selectietest — expliciet in consent & employer contract.
4. **Volgorde merge:** eerst data v2 (#647), dan v3 module-split — niet tegelijk.
5. **Match-realism vóór marketing “80% match”:** kalibratie + confidence live vóór brede campagne.

---

## 7. Referenties (code)

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
