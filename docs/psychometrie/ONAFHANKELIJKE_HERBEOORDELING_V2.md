# Onafhankelijke herbbeoordeling Lobsy-tests — Quick-Scan v2 (PR #647)

**Datum:** 13 oktober 2026  
**Reviewer:** onafhankelijke, conservatieve psychometrische review (COTAN-beoordelaarsstijl)  
**Scope:** productstand `cursor/tests-v2-cotan-alignment-7848` (PR **#647**), tegen baseline **PR #645** (`docs/psychometrie/*`, branch `cursor/psychometrie-cotan-review-7848`)  
**Referentiekader:** COTAN Beoordelingssysteem **2010** + addenda unproctored (2018), actualisatie instrumenten (2018), fairness (2021), tabellen (2021).  
**Status:** **geen officiële COTAN-uitspraak** — dit document vervangt geen registratie, geen juridisch advies en geen arbeidspsychologisch advies.

---

## Bewijstypen (verplichte leeswijze)

| Label | Betekenis |
|-------|-----------|
| **(a)** | Geverifieerd uit broncode / handleiding op PR #647 (file:line) |
| **(b)** | Berekend of gecontroleerd in deze review (scripts, handmatig itemlezen) — geen kwaliteitsclaim over respondenten |
| **(c)** | Niet te beoordelen zonder echte responsdata of externe studies |

---

## Executive summary

### Klaar voor indiening bij COTAN?

**Nee.**

v2 is een **betekenisvolle voorbereidingsstap** (langere schalen, versiebeleid, transparantie, concept-handleiding, match-evidence), maar er zijn **blokkerende materiaaldefecten** (ontbrekende competentie-itemteksten, inhoud–schaal-mismatch, gedeeltelijke NL-fallback in pl/ro/ar voor waarden) en nog steeds **geen empirische normen, betrouwbaarheid of validiteit**. COTAN verwacht aantoonbare kwaliteit op alle criteria; “plan zonder data” en “feature flag uit” rechtvaardigen geen indiening.

**Deels** geldt alleen voor: interne documentatie, studieplan (PR #645), en productmechanismen die **eerlijke communicatie** ondersteunen (geen fake normen, indicatieve match).

### Belangrijkste v2-winst t.o.v. v1 (PR #645)

| Thema | v1 (645) | v2 (647) — reviewerbevinding |
|-------|----------|------------------------------|
| Items per schaal (Quick-Scan) | 4–5 **(a)** | 8 per hoofdschaal (40/48/24/40) **(a)** — **voldoende voor α-schatting**, nog niet voor COTAN zonder data **(c)** |
| pl/ro/ar Quick-Scan | NL-fallback via `MergeAll` **(a)** | Echte vertalingen voor **Career + Culture** v2-keys; **Values** deels nog NL **(a)(b)** |
| Cultuur/waarden EN (v1 keys) | Ontbraken **(a)** | v2 `AssessmentV2.*` volledig EN voor cultuur/waarden **(a)** |
| Handleiding | Alleen UI **(a)** | Concept `docs/tests/HANDLEIDING_ASSESSMENTS_V2.nl.md` **(a)** — nog geen COTAN-hoofdstukstructuur |
| Match-% | Heuristiek **(a)** | `AssessmentMatchEvidence` + uncertainty −10 pp + transparantietekst **(a)** |
| Diepteanalyse i18n | NL hardcoded **(a)** | Embedded JSON `Jobsy.Core/Data/DeepItems/*-{en,pl,ro,ar}.json` + `DeepItemLocalizations` **(a)** |

### Blokkerende bevindingen v2 (must vóór serieuze COTAN-voorbereiding)

1. **12 competentie-items (Q20–21, Q25–29, Q33–37)** tonen in **alle vijf talen** de placeholder-string `AssessmentV2.Competency.Qxx` **(a)(b)** — onbruikbaar testmateriaal.  
2. **Inhoud–schaal-koppeling competentie v2 is deels fout** (items over stress/extraversie/innovatie gescoord onder verkeerde schaalnaam) **(a)(b)** — ernstige bedreiging begripsvaliditeit.  
3. **Waarden v2:** 24/40 PL-items (en vergelijkbaar RO/AR) **identiek aan NL** **(b)** — fairness-risico.  
4. **Geen normen, α, CFA, criteriumvaliditeit, DIF** op Lobsy-data **(c)**.  
5. **Handleiding v2** is concept, niet compleet volgens COTAN criterium 3 **(a)**.

---

## Per COTAN-criterium: v1 → v2

Schaal: **onvoldoende / voldoende / goed / niet te beoordelen**. Conservatief: bij twijfel **niet te beoordelen** (data) of **onvoldoende** (materiaal/documentatie).

### 1 — Uitgangspunten testconstructie

| | Oordeel | Toelichting | Bewijs |
|---|---------|-------------|--------|
| **v1** | **Onvoldoende** | Theorie in catalog-metadata; geen professionele testhandleiding; unproctored niet uitgewerkt | PR #645 §7.1 **(a)** |
| **v2** | **Onvoldoende** (licht verbeterd) | Concept-handleiding beschrijft construct **indicatief**, doelgroep, geen diagnose, AI Act **(a)**; versiebeleid `QuickScanVersion` + `TestsV2Enabled` **(a)**; **geen** volledige operationalisatie per schaal (competentie-mismatch); unproctored-addendum nog niet systematisch | `docs/tests/HANDLEIDING_ASSESSMENTS_V2.nl.md:7–16` **(a)**; `QuickScanVersionPolicy.cs` **(a)**; competentie mismatch **(a)(b)** |

**Rest vóór indiening:** testhandleiding met expliciete meetpretentie, verboden uses, unproctored-procedure (identiteit, fraude, omgeving), koppeling construct→items **na** itemfix.

---

### 2 — Kwaliteit testmateriaal

| | Oordeel | Toelichting | Bewijs |
|---|---------|-------------|--------|
| **v1** | **Onvoldoende** | 4–5 items/schaal; vertaal-gaten; diep NL-only | PR #645 §7.2 **(a)** |
| **v2** | **Onvoldoende** | **Aantal:** 8 items/schaal competentie/carrière/waarden **(a)** `CompetencyTestCatalogV2.cs:15–16`, `CareerTestCatalogV2.cs:13`, `SchwartzValuesCatalogV2.cs:11`; cultuur **2 items/dimensie** (12 schalen) **(a)** `CulturePersonalityCatalogV2.cs:44–69` — **te kort voor betrouwbaarheid**. **Vertalingen:** Career/Culture v2 volledig per taal in `UiStringsAssessmentV2.cs` **(a)**; Values PL/RO/AR deels NL **(b)**; competentie placeholders **(a)(b)**. **Itemkwaliteit:** RIASEC v2 hergebruikt 23 duplicate stems **(b)**; competentie schaal–inhoud conflict **(b)**. **Diep:** 150–200 items per soort, unieke stems, reverse-check in catalog **(a)** `DeepAnalysisCatalog.cs:5–17`, `109–148`; JSON i18n **(a)** `DeepItemLocalizations.cs:9–88`, `Jobsy.Core/Data/DeepItems/*.json` |

#### Vertalingen pl / ro / ar / en — handmatige verificatie Quick-Scan v2

| Instrument | EN | PL | RO | AR | Reviewer |
|------------|----|----|----|-----|----------|
| Carrière (48) | Echte EN | Echte PL | Echte RO | Echte AR | **Voldoende** voor taal **(a)**; inhoudelijk veel duplicate stems **(b)** |
| Cultuur (24) | Echte EN | Echte PL | Echte RO | Echte AR | **Voldoende** taal **(a)**; 2 items/facet **onvoldoende** psychometrisch **(a)** |
| Waarden (40) | Echte EN | **24× NL** | **24× NL** | **24× NL** | **Onvoldoende** fairness **(b)** `UiStringsAssessmentV2.cs` Pl()/Ro()/Ar() Values Q01–Q05, Q09–Q13, … |
| Competentie (40) | Deels EN | Deels PL | Deels PL | Deels AR | **Onvoldoende** — 12 placeholders **alle** talen **(b)** |

v1-fallback (pl/ro/ar = NL) voor **niet-v2** keys blijft bestaan in legacy `UiStringsCompetencies.cs` e.d. **(a)** PR #645 §5.1 — relevant zolang `TestsV2Enabled` uit staat.

#### Reverse-keying en scoring (recompute) **(b)**

| Catalogus | Forward / reverse per schaal (8-item schalen) | All-5 / all-1 patroon → schaal% |
|-----------|-----------------------------------------------|----------------------------------|
| Competentie v2 | 4F + 4R per schaal **(a)** `CompetencyTestCatalogV2.cs:28–72` | 50 / 50 (symmetrisch bij volledige keyed set) |
| Carrière v2 | 5F + 3R per RIASEC-type **(a)** `CareerTestCatalogV2.cs:27–80` | idem |
| Waarden v2 | 4F + 4R per driver **(a)** `SchwartzValuesCatalogV2.cs:23–67` | idem |
| Cultuur v2 | 1F + 1R per dimensie **(a)** `CulturePersonalityCatalogV2.cs:46–69` | idem |

Formule **(a)** `AssessmentScoringDocumentation.cs:11–13`, `LikertAnswerJson.ToPercent` (via PR #645). Reverse vóór gemiddelde: **(a)** `DeepAnalysisCatalog.cs:264`, competentie-scoring in catalog services.

**Let op:** correcte arithmetic op **verkeerd gelabelde** competentie-items invalidates interpretatie **(b)** (Q9–Q19 vs schaalnaam).

Voorbeeld competentie-mismatch **(a)(b)**:

| Id | Schaal in catalogus | NL-item (kern) |
|----|---------------------|----------------|
| 9–10 | Resultaatgerichtheid | piekeren / onverwacht → **stress** |
| 11–13 | Resultaatgerichtheid | innovatie / leren / verbanden |
| 17–19 | Stressbestendigheid | extraversie / alleen werken / groepen |

---

### 3 — Kwaliteit handleiding

| | Oordeel | Toelichting | Bewijs |
|---|---------|-------------|--------|
| **v1** | **Onvoldoende** | Geen testhandleiding | PR #645 §7.3 **(a)** |
| **v2** | **Onvoldoende** (concept) | Handleiding v2: doel, scoring, beperkingen, normen**plan** **(a)**; **ontbreekt** o.a. aparte gebruikers-/scorer-handleiding, foutmarge, contraindicaties, unproctored, technische fiche per instrument, voorbeeldinterpretaties met normen, literatuurlijst — COTAN hst. 3-structuur niet compleet | `HANDLEIDING_ASSESSMENTS_V2.nl.md:1–61` **(a)** |

**Rest:** handleiding uitbreiden naar volledige COTAN-hoofdstukken (doel, populatie, administratie, scoring, normen, interpretatie, misbruik, beperkingen, contact); Engelse parallel indien meertalig instrument.

---

### 4 — Normen

| | Oordeel | Toelichting | Bewijs |
|---|---------|-------------|--------|
| **v1** | **Niet te beoordelen (c)** / inhoud **onvoldoende** | Ruwe 0–100% | PR #645 §7.4 **(a)** |
| **v2** | **Niet te beoordelen (c)** / inhoud **onvoldoende** | Eerlijk **normenplan** zonder fake data **(a)** `HANDLEIDING_ASSESSMENTS_V2.nl.md:51–55`; UI blijft ruwe percentages **(a)**; geen percentielen | PR #645 `VALIDATIE_STUDIEPLAN.md` **(c)** |

**Rest:** normeringsstudie (zie § Minimale studie).

---

### 5 — Betrouwbaarheid

| | Oordeel | Toelichting | Bewijs |
|---|---------|-------------|--------|
| **v1** | **Niet te beoordelen (c)** | Geen α op Lobsy-data | PR #645 §7.5 **(c)** |
| **v2** | **Niet te beoordelen (c)** | 8 items/schaal **theoretisch** geschikter voor α ≥ .80 **(b)**; cultuur 2 items/facet **onvoldoende** **(a)**; geen empirische α/test–retest **(c)** | — |

**Rest:** export + analyse (PR #645 scripts); rapporteer α/ω + CI per schaal en versie.

---

### 6 — Begripsvaliditeit

| | Oordeel | Toelichting | Bewijs |
|---|---------|-------------|--------|
| **v1** | **Niet te beoordelen (c)** | Geen EFA/CFA | PR #645 §7.6 **(c)** |
| **v2** | **Niet te beoordelen (c)** + **materiaalrisico onvoldoende** | Competentie item–schaal mismatch **(b)**; RIASEC duplicate content **(b)**; geen factorstudies **(c)** | zie §2 |

**Rest:** itemfix + expert review + CFA op v2-data; convergentie met bekende instrumenten (optioneel).

---

### 7 — Criteriumvaliditeit

| | Oordeel | Toelichting | Bewijs |
|---|---------|-------------|--------|
| **v1** | **Niet te beoordelen (c)** | Match% ongevalideerd | PR #645 §7.4, §7.7 **(a)(c)** |
| **v2** | **Niet te beoordelen (c)** (transparantie **voldoende** voor product, niet voor COTAN) | Indicatieve diepte: `CompetencyWeightFactor` 0,72, uncertainty 10 pp **(a)** `AssessmentMatchEvidence.cs:25–63`; transparantienote bij `Indicative` **(a)** `ProfileVacancyMatchCalculator.cs:183–192`; **geen** correlatie met arbeidsuitkomsten **(c)** | — |

**Rest:** prospectieve studie match vs sollicitatie/ plaatsing / retentie; documenteer vacature-inferentie bias.

---

### Fairness (addendum 2021)

| | Oordeel | Toelichting | Bewijs |
|---|---------|-------------|--------|
| **v1** | **Onvoldoende** | pl/ro/ar = NL; geen DIF | PR #645 §8.1 **(a)(c)** |
| **v2** | **Onvoldoende** | v2 Career/Culture vertaald **(a)**; Values + competentie placeholders **(b)**; `TestItemTranslationParityTests` test alleen **v1** keys (`Competency.Q*`), niet `AssessmentV2.*` **(a)** `TestItemTranslationParityTests.cs:15–40`; geen DIF-programma **(c)** | — |

**Rest:** volledige v2-pariteitstests; professionele vertaling + back-translation; DIF op gender/taal/leeftijd na N.

---

### Unproctored (addendum 2018)

| | Oordeel | Toelichting | Bewijs |
|---|---------|-------------|--------|
| **v1** | **Onvoldoende** | Thuis invullen; addendum niet adresserd | PR #645 §7.1 **(a)** |
| **v2** | **Onvoldoende** | Zelfde context; transparantie v2 **(a)** `TestConsentGate.razor:28–51`; geen proctoring, geen fraude-detectie document | `TestConsentGate.razor` **(a)** |

**Rest:** unproctored-beleid in handleiding (integriteit, herkansing, identificatie, data quality flags).

---

### Korte vormen / diepte / matchclaims

| Mechanisme | Beoordeling | Bewijs |
|------------|-------------|--------|
| Eerste blik (5) / dieper (10) vs volledig | **Voldoende** richting transparantie | `TestDepthRules.cs:23–24`, `AssessmentMatchEvidenceRules.FromQuickScan` **(a)** |
| `IsIndicativeDepth` in UI-modellen | **Niet geïmplementeerd** | Property alleen in `CompetencyModels.cs` **(a)** — geen toewijzing in Web/Infrastructure **(a)** |
| v2-transparantieblok | **Voldoende** | `data-testid="assessment-v2-transparency"` **(a)** `TestConsentGate.razor:44–51` |
| Default rollout | **Voldoende** risicobeheersing | `TestsV2Enabled` default uit **(a)** PR #647 body, handleiding regel 5 |

---

### Consent / transparantie

| | v1 | v2 |
|---|----|----|
| Test + AI consent | Deels **(a)** PR #645 §8.2 | Consent gate + v2 transparency strings **(a)** `TestConsentGate.razor:25–51` |
| Geen diagnose | UI | Expliciet `AssessmentV2.Transparency.NotDiagnosis` **(a)** |
| Menselijke oversight | Beleidsclaim | String + handleiding **(a)** — **niet** overal hard afgedwongen in code **(a)** PR #645 |

---

## Diepteanalyse (150–200 items)

| Soort | Items | i18n | Opmerking reviewer |
|-------|-------|------|---------------------|
| Competentie | 150 | en/pl/ro/ar JSON **(a)** | Bron NL `DeepAnalysisCompetenceItems.cs`; vertaling via resources **(a)** |
| Carrière | 200 | idem **(a)** | Unieke stems enforced **(a)** `DeepAnalysisCatalog.cs:122–133` |
| Cultuur | 150 | idem **(a)** | |
| Waarden | 150 | idem **(a)** | Tests eisen 0 NL-fallback **(a)** `TestItemTranslationParityTests.cs:48–65` — **niet** in deze omgeving gedraaid (geen `dotnet` in PATH); code-review **(a)** |

**v2 Quick-Scan scope** wijzigt diep-analyse catalogi niet **(a)** `GEMAAKTE_KEUZES_TESTS_V2.md:36`.

---

## Geprioriteerde gaplijst

### Must (blokkerend vóór COTAN-indiening)

1. **Competentie v2:** placeholder-items invullen in **alle talen** (Q20–21, Q25–29, Q33–37) **(a)(b)**.  
2. **Competentie v2:** itemteksten **heraligneren** met schaaltoewijzing in `CompetencyTestCatalogV2.cs` + psycholoog review **(b)**.  
3. **Waarden v2:** resterende **24 NL-items** vertalen in pl/ro/ar (geen copy-paste NL) **(b)**.  
4. **Automatische tests:** parity voor **`AssessmentV2.*`** keys (niet alleen v1 `Competency.Q*`) **(a)**.  
5. **Handleiding** compleet volgens COTAN criterium 3 + unproctored-addendum **(a)**.  
6. **Empirische studie:** normen + betrouwbaarheid + basis validiteit op **v2** data **(c)** — geen marketingclaims tot afgerond.

### Should

7. Cultuur v2 uitbreiden tot **≥4–8 items** per cultuur-/persoonlijkheidsdimensie of extern korte vorm valideren.  
8. RIASEC v2: duplicate stems vervangen; balance per type documenteren.  
9. DIF/fairness-plan + uitvoering na N.  
10. `IsIndicativeDepth` koppelen aan UI of property verwijderen (consistentie).  
11. v1-fallback pl/ro/ar deprecaten of blokkeren wanneer v2 actief is.

### Could

12. CFA + publicatie; convergent validiteit.  
13. Match-% kalibratie vs uitkomsten (N≥200).  
14. Externe audit door GZ-/ registratiepsycholoog.

---

## Minimale data / studie (voor criterium 4–7)

Gebaseerd op PR #645 `VALIDATIE_STUDIEPLAN.md` en COTAN-praktijk **(c)**:

| Fase | Design | N (richtlijn) | Duur (indicatief) | Deliverables |
|------|--------|---------------|-------------------|--------------|
| **A — Materiaal fix** | Expert review + pilot N≈30/taal | 30 × 5 talen | 4–8 weken | Itembank v2.1, vertaalrapport |
| **B — Betrouwbaarheid** | Cross-sectioneel, unproctored | **≥300** voltooide profielen **per instrument** (v2), stratificatie leeftijd/taal/geslacht | 3–6 maanden na launch v2 | α/ω, item-rest, scale reliability report |
| **C — Structuur** | EFA/CFA per instrument | dezelfde steekproef B | parallel | Factorstructuur, fit indices |
| **D — Normen** | Normering subsample | **≥500** totaal of 300/instrument | na B | Percentielen, SE measurement |
| **E — Criterium** | Prospectief | **≥200** matches met follow-up (sollicitatie/plaatsing/3-maanden) | 6–12 maanden | Validity coefficients, ROC indien selectie |
| **F — Fairness** | DIF | power afhankelijk | na B | Mantel-Haenszel / IRT-DIF rapport |
| **G — Test–retest** | Subsample | **≥100**, interval 2–4 weken | 2–4 maanden | r, agreement |

**Timeline:** earliest realistische COTAN-voorbereiding **≥9–12 maanden** na materiaalfix + v2 in productie (flag aan) met continue dataverzameling — **geen kalendergarantie**, wel volgorde: **A → B → C/D parallel → E/F/G**.

---

## Methodiek deze herbbeoordeling

- Baseline-oordelen overgenomen uit **PR #645** `COTAN_BEOORDELING_LOBSY_TESTS.md`.  
- v2-code en strings gelezen op branch PR **#647** (checkout door reviewer).  
- Script **(b):** extractie `UiStringsAssessmentV2.cs`, telling placeholders, NL-duplicaten pl/ro/ar, reverse-balans per catalogus, schaalpercent recompute bij all-1/all-5.  
- **Geen** productcode gewijzigd door reviewer.  
- **Geen** toegang productie/acceptatie DB **(c)**.  
- `dotnet test` niet uitgevoerd (SDK niet in agent-PATH); bevindingen uit code + script.

---

## Conclusie

Quick-Scan **v2** adresseert deel van de PR #645 must-list (langere schalen, EN/cultuur/waarden v2-keys, concept-handleiding, match-transparantie, diep-i18n infrastructure), maar **is niet indieningsrijp** bij COTAN: testmateriaal is **feitelijk defect** op competentie-v2, vertaling waarden-deel onvolledig, cultuur nog te kort, en **alle empirische criteria** ontbreken. **Aanbeveling:** PR #647 **niet** positioneren als COTAN-aligned voor registratie; eerst must-fixes, dan validatiestudie volgens plan.

---

*Einde onafhankelijke herbbeoordeling v2 — geen COTAN-uitspraak.*
