# Psychometrische review Lobsy kandidaatstests

**Datum:** 13 oktober 2026 (herziening: rigoureuze, conservatieve pass)  
**Status:** **Geen officiële COTAN-beoordeling.** Dit document is een interne kwaliteits- en risicoscan op basis van broncode en beperkte synthetische pipeline-tests. Het vervangt geen registratie bij COTAN, geen juridisch advies en geen arbeidspsychologisch advies.

---

## 0. Referentiekader COTAN

Beoordeling is **geïnspireerd** op:

| Bron | Versie / datum |
|------|----------------|
| *COTAN Beoordelingssysteem voor de kwaliteit van tests* (Evers, Lucassen, Meijer & Sijtsma) | **2010** ([PDF NIP](https://nip.nl/wp-content/uploads/2022/08/COTAN-Beoordelingssysteem-2010.pdf), [cotandocumentatie.nl](https://www.cotandocumentatie.nl/cotan/beoordelingssysteem/)) |
| Addendum unproctored gegevensverzameling | 01-12-2018 |
| Addendum actualisatie instrumenten | 01-12-2018 |
| Fairness addendum | 14-07-2021 |
| Wijzigingen tabellen beoordelingssysteem | 01-06-2021 |

COTAN gebruikt per criterium een **driepuntsschaal: onvoldoende / voldoende / goed** ([NIP procedure](https://nip.nl/cotan/beoordelingsprocedure/)). Wij passen die labels **conservatief** toe: bij twijfel **“niet te beoordelen”** (ontbrekende data) of **“onvoldoende”** (wel code, geen onderbouwing). Wij claimen **nergens “goed”** voor dit instrument in zijn huidige vorm.

Toelichting betrouwbaarheid selectiecontext (extern, COTAN-gerelateerde literatuur): voor beslissingen met grote gevolgen worden in de sector vaak α **> .90** als sterk en **< .80** als zwak geïnterpreteerd ([Fundament 6.2](https://www.fundamentvoor.nl/onderneming-personeel/eerlijke-werving-en-selectie/assessment-beoordelen/62-inhoud-beoordeling-cotan)). Lobsy-tests zijn **niet** als gecertificeerde selectietest gepositioneerd in code; toch beïnvloeden scores **match%** en ranking — dat is een **risico** (zie criterium 7).

---

## 1. Bewijstypen (verplichte leeswijze)

Elke bewering in dit document is gelabeld:

| Label | Betekenis |
|-------|-----------|
| **(a)** | Geverifieerd uit **broncode** of gegenereerde inventaris uit code (file:line of `data/*.json` uit repo) |
| **(b)** | **Berekend op synthetische data** — uitsluitend indicatief voor analyse-scripts, **geen** kwaliteitsclaim over Lobsy-respondenten |
| **(c)** | **Niet te beoordelen** zonder echte, geanonimiseerde responsdata (minimaal N zie §6 en `VALIDATIE_STUDIEPLAN.md`) |

Artifacts in deze PR:

- `data/inventory.json` — (a) via `scripts/extract_inventory.py`
- `data/scoring_verification.json` — (a) replicatie unit-testlogica in Python
- `data/item_wording_review.json`, `ITEMteksten_BEOORDELING.md` — (a) + handmatige leesreview
- `data/synthetic_demo_report.json` — (b) alleen pipeline

---

## 2. Executive summary (conservatief)

Lobsy heeft vier gratis Quick-Scans (18–25 Likert-items) plus betaalde diepteanalyses (150–200 items), onboarding-mini (21 items) en productlogica voor DNA, tags en vacature-match%. **(a)**

| COTAN-criterium | Ons oordeel | Motief (kort) |
|-----------------|-------------|---------------|
| 1 Uitgangspunten testconstructie | **Onvoldoende** | Theorie genoemd, maar meetpretentie / operationele definities niet als testhandleiding uitgewerkt **(a)** |
| 2 Kwaliteit testmateriaal | **Onvoldoende** | Korte schalen; vertaal-gaten; cultuur/waarden EN zonder itemvertaling **(a)** |
| 3 Kwaliteit handleiding | **Onvoldoende** | Alleen UI-teksten, geen professionele testhandleiding **(a)** |
| 4 Normen | **Niet te beoordelen (c)** / inhoudelijk **onvoldoende** | Geen normpopulatie; ruwe 0–100% **(a)** |
| 5 Betrouwbaarheid | **Niet te beoordelen (c)** | Geen echte data in deze review |
| 6 Begripsvaliditeit | **Niet te beoordelen (c)** | Geen CFA/convergentiestudies |
| 7 Criteriumvaliditeit | **Niet te beoordelen (c)** | Match% niet gevalideerd vs arbeidsuitkomsten **(a)** model, **(c)** empirisch |
| Fairness (addendum 2021) | **Onvoldoende** | pl/ro/ar NL-items; geen DIF **(a)** |
| AI / uitleg match + diep rapport | **Onvoldoende** voor high-risk readiness | Consent deels **(a)**; monitoring/onafhankelijkheid **(c)** |

---

## 3. Inventaris tests & schalen **(a)**

### 3.1 Overzicht

| Test | Items free | Items deep | Bron metadata |
|------|------------|------------|---------------|
| Competentie | 25 | 150 | `AssessmentTestCatalog.cs:14–26` |
| Beroepen (RIASEC) | 25 | 200 | `AssessmentTestCatalog.cs:27–40` |
| Cultuur & persoonlijkheid | 18 | 150 | `AssessmentTestCatalog.cs:41–54` |
| Waarden (Schwartz-drivers) | 25 | 150 | `AssessmentTestCatalog.cs:55–68` |

Diepteanalyse lengtes: `DeepAnalysisCatalog.cs:5–17`.

### 3.2 Competentie Quick-Scan **(a)**

- Schalen: Samenwerken, Resultaatgerichtheid, Stressbestendigheid, Innovatie, Extraversie — **5 items** elk (`CompetencyTestCatalog.cs:61–88`, `CategoryQuestionCount=5` L17).
- Reverse items: 2 per schaal (Extraversie: 3 forward, 2 reverse) — tests bevestigen ≥8 reverse (`Jobsy.Tests/CompetencyMatchingTests.cs:24–25`).
- Matching gebruikt **4** schalen (zonder Extraversie): `CategoryCodes` `CompetencyTestCatalog.cs:33–39`; Extraversie apart `L141–142`.
- Constructieclaim UI: Mini-IPIP / Big Five (`CompetencyTestCatalog.cs:6–8`, `UiStringsCompetencies.cs:33–34`) — **operationeel** zijn het **werkplek-benamingen**, geen 1:1 Mini-IPIP schalen **(a)**.

### 3.3 Beroepen Quick-Scan **(a)**

- RIASEC: Realistic **5** items, I/A/S/E/C **4** elk (`CareerTestCatalog.cs:34–64`).
- Holland-code: top-3 letters bij scores >0 (`CareerTestCatalog.cs:176–190`).
- Tags: ≥70% top types, max 3 (`CareerTestCatalog.cs:167–173`).

### 3.4 Cultuur Quick-Scan **(a)**

- 6 cultuurdimensies × **2** items + 5 persoonlijkheidsfacetten (Openness **2**, overige **1**) = 18 (`CulturePersonalityCatalog.cs:47–77`).
- Personality items L71–76: **geen** reverse flags in catalog (alle `Reverse: false` behalve Q18 Openness reverse).

### 3.5 Waarden Quick-Scan **(a)**

- 5 drivers × 5 items (`SchwartzValuesCatalog.cs:29–56`).

### 3.6 Onboarding-mini **(a)**

- 21 items totaal (`OnboardingWizardCatalog.cs:26–54`; test `CandidateOnboardingWizardTests.cs:12–18`).
- Cultuur-mini **laat Innovation-dimensie weg** (`OnboardingWizardCatalog.cs:34–38`).
- Provisional padding cultuur: ontbrekende dimensies → **55%** (`ProvisionalAssessmentScores.cs:11`, `L99–101`, `L151–155`).

### 3.7 Diepteanalyse competentie **(a)**

- 150 NL strings hardcoded (`DeepAnalysisCompetenceItems.cs`), **geen** UiStrings-sleutels — vertaling/arabisering niet via dezelfde pipeline als Quick-Scan.

Volledige itemteksten Quick-Scan: **`ITEMteksten_BEOORDELING.md`** (93 items, handmatig gelezen + heuristiek).

---

## 4. Scoring, DNA, match% **(a)**

### 4.1 Antwoordformaat

- Likert **1–5** (`LikertAnswerJson.cs:8–9`).
- Validatie tekst: `CompetencyTestCatalog.cs:101–104`.

### 4.2 Reverse & percentiles

- Reverse: `LikertMax + LikertMin - raw` → **6 − raw** (`CompetencyTestCatalog.cs:156`).
- Percent: gemiddelde gescoorde waarden → `(avg−1)/4×100`, clamp 0–100 (`LikertAnswerJson.cs:92–104`).

**Verificatie (a):** `data/scoring_verification.json` — Python-replicatie van `CompetencyMatchingTests.Score_maps_likert_to_0_100_and_reverses_items`:

- Alle schalen 100 bij “high” patroon: **pass**
- Samenwerken/Innovatie 0 bij “low” patroon: **pass**
- Neutraal (alle 3) → 50%: **pass**

*(Dotnet-test niet gedraaid in cloud agent: `dotnet` ontbrak in PATH; Python-replicatie gebruikt dezelfde formules.)*

### 4.3 DNA-headlines

- Drempels high/mid: **70 / 45** (`DnaSummarySentences.cs:10–11`).
- Cultuur DNA binair: **60** (`DnaSummarySentences.cs:80`).
- Vaste NL-zinnen, geen LLM (`DnaSummarySentences.cs:3–7`).

### 4.4 Match%

- Calculator: `ProfileVacancyMatchCalculator.Calculate` (`ProfileVacancyMatchCalculator.cs:25–167`).
- Top-10 drempel **60%**: `DisplayThreshold = 60` (`ProfileVacancyMatchCalculator.cs:13`, `L188`).
- Kleur band “sterk” **≥70**: `MatchScoreCalculator.cs:10`, gebruikt in `ProfileVacancyMatchCalculator.cs:162–166`.
- Vacature-competenties **geïnferd** ( geen candidate-only ): `VacancyCompetencyProfile.cs:21–62`.
- Tag cutoff **70%**: `CompetencyTestCatalog.cs:191–200`.

**Conservatieve conclusie (a+c):** match% is een **productheuristiek**, geen gevalideerde criterion score **(c)**.

---

## 5. Itemteksten — review alle talen **(a)**

Methodiek: alle **93** Quick-Scan items gelezen in **nl, en, pl, ro, ar** (zie `ITEMteksten_BEOORDELING.md`). pl/ro/ar zijn **identiek aan nl** door `MergeAll` **(a)**:

- `UiStringsCompetencies.cs:16–18`
- `UiStringsCulture.cs:16–18`
- `UiStringsValues.cs:16–18`

### 5.1 Vertaalstatus **(a)**

| Test | NL | EN items | pl / ro / ar |
|------|----|-----------|--------------|
| Competentie | ja | **ja** (`UiStringsCompetencies.cs:535–559`) | **NL-fallback** |
| Beroepen | ja | **ja** (`Career.Q` in En-blok) | **NL-fallback** |
| Cultuur | ja | **nee** — `En()` overschrijft Q01–Q18 niet (`UiStringsCulture.cs:154–188`) | **NL-fallback** |
| Waarden | ja | **nee** — `En()` overschrijft Q01–Q25 niet (`UiStringsValues.cs:87–114`) | **NL-fallback** |

**Reviewer-oordeel:** Voor gebruikers met UI-taal en/pl/ro/ar is de **meetinhoud feitelijk Nederlands** → fairness- en begripsbias **(a)**; empirische DIF **(c)**.

### 5.2 Geselecteerde itemissues (handmatige review, NL)

| Item | Issue | Ernst |
|------|-------|-------|
| `Competency.Q13` | Twee concepten (multitask + paniek) | should rewrite |
| `Competency.Q03` | Absoluut “eerst … voordat” | could soften |
| `Career.Q05` | Leading “vermijd … vies, zwaar” | could |
| `CultureScan.Q02` | Autonomy **reverse** pole vs Q01 — pole pair ok **(a)** catalog | — |
| `ValuesScan.Q14` | Typo “overtreff” i.p.v. “overtreft” **(a)** `UiStringsValues.cs:60` | must fix |
| Diep NL-only | 150+200+150+150 stems niet in vertaaltabel | must (beleid) |

Volledige lijst per item: **`ITEMteksten_BEOORDELING.md`**.

---

## 6. Empirische psychometrie **(b)** vs **(c)**

### 6.1 Echte data

- **Geen** toegang tot productie- of acceptatie-database in deze review **(c)**.
- Export klaar: `scripts/export_anonymized_responses.sql`.

### 6.2 Synthetische pipeline-check **(b)**

`analyze_responses.py --synthetic --n 800` → `data/synthetic_demo_report.json`:

- **Doel:** controleren dat reverse-keying vóór α gebeurt (`reverse_key_applied_before_alpha: true`) en dat **twee** α-methoden overeenkomen.
- Voorbeeld competentie/Samenwerken **(b)**: `alpha_variance_formula ≈ 0.955`, `alpha_avg_interitem_correlation ≈ 0.955`, `methods_agree_within_0.01: true`.

**Conservatief:** hoge α op synthetische unidimensionele data **bewijst niets** over Lobsy. Eerdere synthetische run **zonder** reverse-aware generator produceerde **negatieve** α — dat illustreert dat verkeerde pipeline misleidende cijfers geeft **(b)**; daarom is dubbele verificatie verplicht vóór echte data.

### 6.3 Wat pas na export kan **(c)**

Cronbach α/ω, item-rest, plafond, inter-scale correlaties, mini–full r, DIF, criteriumvaliditeit — zie **`VALIDATIE_STUDIEPLAN.md`**.

---

## 7. COTAN-criteria (detail, conservatief)

### 7.1 Criterium 1 — Uitgangspunten testconstructie: **Onvoldoende (a)**

COTAN 2010 beoordeelt o.a. meetpretentie, doelgroep, theoretische onderbouwing en operationalisatie (PDF §criterium 1).

| Subvraag | Beoordeling | Bewijs |
|----------|-------------|--------|
| Meetpretentie / gebruiksdoel beschreven | Gedeeltelijk | UI `ScienceNote`-keys **(a)**, geen testhandleiding **(a)** |
| Theoretische basis | Gedeeltelijk | `AssessmentTestCatalog.cs` modelnamen **(a)** |
| Operationalisatie in items | Gedeeltelijk | Catalogi **(a)**, maar werkplek-labels ≠ standaardinstrument **(a)** |
| Doelgroep / context unproctored | Minimaal | Web thuis invullen; unproctored-addendum niet adresserd **(a+c)** |

### 7.2 Criterium 2 — Kwaliteit testmateriaal: **Onvoldoende (a)**

| Aspect | Beoordeling | Bewijs |
|--------|-------------|--------|
| Aantal items per schaal | Zwak | 4–5 Quick-Scan **(a)** |
| Itemformulering | Matig | Review §5.2 **(a)** |
| Vertalingen | Zwak | §5.1 **(a)** |
| Layout/instructies | Basis | Likert labels `UiStringsCompetencies.cs:50–54` **(a)** |
| Diep testmateriaal | NL-only | `DeepAnalysisCompetenceItems.cs` **(a)** |

### 7.3 Criterium 3 — Handleiding: **Onvoldoende (a)**

Geen aparte handleiding met norminterpretatie, foutmarge, contraindicaties, scorer instructies. Alleen kandidaat-UI **(a)**.

### 7.4 Criterium 4 — Normen: **Niet te beoordelen (c)**; inhoud **onvoldoende (a)**

Geen normeringsstudie; percentiles zijn **ruwe transformatie** Likert **(a)** `LikertAnswerJson.ToPercent`. Vacature-defaults 60 **(a)** `VacancyCompetencyProfile.cs:36–41`.

### 7.5 Criterium 5 — Betrouwbaarheid: **Niet te beoordelen (c)**

Geen α/ω/test–retest op Lobsy-data. COTAN verwacht rapportage procedure + steekproef **(c)**.

### 7.6 Criterium 6 — Begripsvaliditeit: **Niet te beoordelen (c)**

Geen EFA/CFA, geen convergentie met COTAN-geregistreerde instrumenten **(c)**.

### 7.7 Criterium 7 — Criteriumvaliditeit: **Niet te beoordelen (c)**

Match% en tags beïnvloeden ranking **(a)** maar correlatie met hire/retention **(c)**.

---

## 8. Fairness & AI Act

### 8.1 Fairness **(a + c)**

- Taal: §5.1 **(a)**.
- Geslacht: geen veld op `User` voor DIF **(a)** `User.cs` (geen Gender property in entity).
- Provisional scores in onboarding beïnvloeden indruk vóór voltooide test **(a)** `ProvisionalAssessmentScores.cs`.
- Fairness addendum 2021: geen documenteerbaar DIF-programma **(c)**.

### 8.2 AI Act / high-risk **(conservatief)**

Matching + AI-rapporten kunnen **toegang tot werk** raken. **(a)** `TestAiConsentAt` op `User.cs:40–42`. **(c)** volledige conformity assessment niet uitgevoerd. Beleid: test **niet** enige automatische afwijzing — **niet** als harde regel in code aangetoond **(a)** (match is ranking, geen hard gate everywhere).

---

## 9. Prioritized fix list

### Must (voor serieuze validatie / fairness)

1. **Vertaal alle itemteksten** cultuur + waarden EN; echte pl/ro/ar vertalingen (niet `Merge(Nl())`) — bewijs issue `UiStringsCulture.cs:154–188`, `UiStringsValues.cs:87–114`.
2. **Fix typo** `ValuesScan.Q14` “overtreff” → “overtreft” (`UiStringsValues.cs:60`).
3. **Testhandleiding** (meetpretentie, verboden uses, geen diagnose, interpretatie 0–100%, beperkingen mini vs full).
4. **Scheid provisional onboarding** visueel/logisch van voltooide scores in match (badge bestaat **(a)** `Onboarding.ProvisionalBadge` — policy documenteren).
5. **Export + analyse op echte data** (N≥300/test) vóór marketing “wetenschappelijk betrouwbaar”.

### Should

6. Quick-Scan uitbreiden tot **≥8–10 items/schaal** of extern verkorte vorm valideren.
7. Cultuur personality: **≥2 items/facet** + reverse keys waar nodig.
8. Documenteer vacature-inferentie (`VacancyCompetencyProfile`) + bias-audit.
9. Diepteanalyse: vertaalstrategie of “alleen NL” expliciet in UI/consent.
10. Test–retest subset (N=100).

### Could

11. Item rewrites (Q13, absolute woorden).
12. Gelijk trekken RIASEC items per schaal (4 vs 5).
13. CFA/publicatie met universiteitspartner.
14. Kalibratie match% vs hire (N=200+).

---

## 10. Validatiestudie

Zie **`VALIDATIE_STUDIEPLAN.md`** (fases, N, methoden, deliverables).

---

## 11. Bijlagen

| Bestand | Type |
|---------|------|
| `ITEMteksten_BEOORDELING.md` | (a) item review |
| `data/inventory.json` | (a) |
| `data/scoring_verification.json` | (a) |
| `data/synthetic_demo_report.json` | (b) |
| `VALIDATIE_STUDIEPLAN.md` | plan |

---

*Einde document — geen COTAN-uitspraak.*
