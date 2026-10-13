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
| Employer teamcultuur **6+4 optioneel** (T1–T10) + vacature-accordeon V1–V10 | `EmployerCultureScan` / `VacancyMatchOptional` | Invuldrift | Completion %; N ≥ 30 vestigingen |

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
