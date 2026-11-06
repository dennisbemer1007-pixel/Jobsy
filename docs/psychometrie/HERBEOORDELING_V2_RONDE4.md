# Onafhankelijke herbbeoordeling Quick-Scan v2 — ronde 4 (PR #647, commit 1333e078)

**Datum:** 13 oktober 2026  
**Reviewer:** onafhankelijke, sceptische psychometrische herbeoordeling (COTAN-beoordelaarsstijl)  
**Scope:** PR **#647**, branch `cursor/tests-v2-cotan-alignment-7848`, commit **`1333e078`** (reverse-items waarden, Q04, guards, constructmatrix, opruiming main `UiStrings`)  
**Baseline:** ronde 3 — `docs/psychometrie/HERBEOORDELING_V2_RONDE3.md`  
**Referentiekader:** COTAN 2010 + addenda (unproctored, fairness, tabellen). **Geen officiële COTAN-uitspraak.**

---

## Bewijstypen

| Label | Betekenis |
|-------|-----------|
| **(a)** | Geverifieerd in code, tests of handleiding op commit 1333e078 |
| **(b)** | Handmatig itemlezen / screenshots in deze review |
| **(c)** | Niet te beoordelen zonder responsdata of externe studies |

---

## Executive summary — twee verdicts

### 1. PR #647 merge (code, regressies, v1)

**Ja — mergebaar** op technische gronden **(a)(b)**.

- Release-build geslaagd; gerichte tests (v2 guards, Playwright, v1-isolatie) **groen**.
- Ronde-3 waarden-reverse-fixes **bevestigd**; nieuwe guard `Values_v2_reverse_items_use_contrary_nl_wording` slaagt **(a)**.
- v1-resultaten en versiebeleid niet aangetast door deze PR **(a)**.
- **Voorbehoud (product, geen merge-blocker):** RIASEC v2 heeft **zes** reverse-keyingfouten; cultuur v2 **één** (Q20). Zolang Quick-Scan v2 **niet** als gevalideerd instrument wordt aangeboden of v2-scores leidend zijn voor matching, is dit **schuld** voor een vervolgfix op #647 of direct daarna — geen reden om engineering-merge te blokkeren.

### 2. COTAN concept-indiening (materiaal + handleiding, **zonder** normen)

**Nee — nog niet.** Waarden v2 is na 1333e078 **indieningswaardig qua keying** **(b)**; **RIASEC v2** en **cultuur Q20** ondermijnen scoring en moeten vóór indiening worden gecorrigeerd **(must)**. Empirische normen, betrouwbaarheid en validiteit blijven **(c)**.

---

## Verificatie ronde-3 must-fixes (commit 1333e078)

| Must ronde 3 | Status ronde 4 | Bewijs |
|--------------|----------------|--------|
| Q21/Q31/Q39 waarden: echte reverse-stems + keying | **Opgelost** | Bank + `VALUES_V2_CONSTRUCT_MATRIX.md`; guard **(a)(b)** |
| Q04 autonomie verduidelijken | **Opgelost** | “Ik wil zelf bepalen met welke taak ik begin.” **(a)(b)** |
| Keying-guard waarden | **Toegevoegd** | `Values_v2_reverse_items_use_contrary_nl_wording` **(a)** |
| Dead strings waarden/RIASEC in main `UiStringsAssessmentV2.cs` | **Opgelost** in main file | Bank is laatste merge-laag **(a)** |
| Construct→item-matrix waarden | **Toegevoegd** | `docs/psychometrie/VALUES_V2_CONSTRUCT_MATRIX.md` **(a)** |

**Should (open):** `UiStringsAssessmentV2Supplement.cs` bevat nog **~120** verouderde `AssessmentV2.Values.*`-entries (PL/RO/AR o.a. verkeerde Q04/Q21/Q31/Q39) **(a)** — **overschreven** door bank at runtime, maar **verwarrend voor onderhoud**.

---

## Technische verificatie (1333e078)

| Stap | Resultaat |
|------|-----------|
| `dotnet build Jobsy.sln` (Release) | **Geslaagd** **(a)** |
| `AssessmentV2TranslationGuardTests` (16 tests) | **16/16 geslaagd** **(a)** |
| `TestsV2PlaywrightTests` | **5/5 geslaagd** **(a)** |
| v1-isolatie (`QuickScanVersionPolicy`, `AssessmentQuickScanV2Tests`, …) | **29/29** in gefilterde set **(a)** |
| Screenshots | `artifacts/tests-v2-playwright/` — o.a. intro **“Heel diep · 40 vragen”**, v2-transparantieblok **(b)** |

---

## Reverse-keying — volledige review vier v2-instrumenten

**Scoringregel:** reverse → `6 − ruwe score` vóór middeling per schaal/type **(a)**.  
**Reviewcriterium:** bij **R** moet **hoge agreement** de **doelconstruct-score verlagen**; stem moet **tegen** de construct geformuleerd zijn (of expliciet lage trait). Bij **F** moet hoge agreement de score **verhogen**.

### Waarden v2 (20 reverse-items, 5 drivers × 4R)

| Id | Driver | R? | NL stem (kern) | Keying OK? |
|----|--------|----|----------------|------------|
| Q03 | Autonomie | R | Precies instructies | **Ja** |
| Q05 | Autonomie | R | Vaste routines vs vrijheid | **Ja** |
| Q07 | Autonomie | R | Vrijheid zonder doelen onrustig | **Ja** |
| Q08 | Autonomie | R | Leiding bepaalt organisatie | **Ja** |
| Q11 | Verbinding | R | Geen hulp bij planning | **Ja** |
| Q13 | Verbinding | R | Informele gesprekken afleiding | **Ja** |
| Q15 | Verbinding | R | Alleen koppelen indien nodig | **Ja** |
| Q16 | Verbinding | R | Liever weinig contact | **Ja** |
| Q19 | Prestatie | R | Niet beste hoeven | **Ja** |
| Q21 | Prestatie | R | Zicht resultaten maakt weinig uit | **Ja** *(ronde-3 fix)* |
| Q23 | Prestatie | R | Titels/rangorde onbelangrijk | **Ja** |
| Q24 | Prestatie | R | Competitie interesseert weinig | **Ja** |
| Q27 | Stabiliteit | R | Planningwijzigingen verdragen | **Ja** *(tolerantie = lage stabiliteit; R correct)* |
| Q29 | Stabiliteit | R | Prima als prioriteiten wisselen | **Ja** |
| Q31 | Stabiliteit | R | Last-minute wijzigingen plaatsen | **Ja** *(ronde-3 fix)* |
| Q32 | Stabiliteit | R | Losse afspraken passen goed | **Ja** |
| Q35 | Impact | R | Salaris/status zwaarder dan MVO | **Ja** |
| Q37 | Impact | R | Geen verschil buiten team | **Ja** |
| Q39 | Impact | R | Maatschappelijk effect boeit weinig | **Ja** *(ronde-3 fix)* |
| Q40 | Impact | R | Geen werk beyond eigen taken | **Ja** |

**Vertaling gewijzigde items (Q04, Q21, Q31, Q39):** actieve bank `UiStringsAssessmentV2ValuesCareerBank.cs` — EN/PL/RO/AR **betekenis-equivalent** met NL **(b)** (geen pro-trait frases meer op R-items). Supplement-strings voor PL/RO/AR op dezelfde keys zijn **stale** **(a)**.

---

### Competentie v2 (20 reverse-items, 5 schalen × 4R)

Alle reverse-keys Q04–Q40 (oneven blokken per schaal) handmatig gelezen in NL **(b)** — negatieve trait-formuleringen (afzijdig, teamoverleg tijdverspilling, uitstel, piekeren, vasthouden aan oud, vermijden experimenteren, alleen werken, …). **Geen pro-competentie-stem met `Reverse: true` gevonden.**

---

### Cultuur & persoonlijkheid v2 (24 reverse-items, 12 dimensies × 2R)

| Q | Dimensie | NL stem (kern) | Keying OK? |
|---|----------|----------------|------------|
| Q02 | Autonomie | Duidelijke instructies / vaste kaders | **Ja** |
| Q04 | Informeel | Formelere toon, hiërarchie | **Ja** |
| Q06 | Samenwerking | Beter alleen afmaken | **Ja** |
| Q08 | Flexibiliteit | Vaste, voorspelbare planning | **Ja** |
| Q10 | Innovatie | Bewezen methodes | **Ja** |
| Q12 | People-first | Resultaat eerst, ook sfeer lastig | **Ja** |
| Q14 | Openness | Bekende werkwijzen vs nieuw | **Ja** |
| Q16 | Conscientiousness | Taken uitstellen | **Ja** |
| Q18 | Extraversie | Drukke ruimtes putten uit | **Ja** |
| **Q20** | **Agreeableness** | **Belangen anderen boven eigen gemak** | **Nee — pro-agreeableness + R** **(must)** |
| Q22 | Emotional stability | Tegenslagen raken lang | **Ja** |
| Q24 | Curiosity | Liever geen nieuwe software | **Ja** |
| Q26 | Autonomie | Leiding keurt elke stap goed | **Ja** |
| Q28 | Informeel | Formele aanhef/titels | **Ja** |
| Q30 | Samenwerking | Best zonder bemoeienis werkvorm | **Ja** |
| Q32 | Flexibiliteit | Onrust bij roosterwissels | **Ja** |
| Q34 | Innovatie | Verandering minimaal | **Ja** |
| Q36 | People-first | Targets belangrijker dan sfeer | **Ja** |
| Q38 | Openness | Hoef niet te weten waarom procedure | **Ja** |
| Q40 | Conscientiousness | Perfectie kost te veel tijd | **Ja** |
| Q42 | Extraversie | Lange dagen gesprekken maken leeg | **Ja** |
| Q44 | Agreeableness | Hard kiezen ook als collega's het niet leuk vinden | **Ja** |
| Q46 | Emotional stability | Spanning 's avonds mee | **Ja** |
| Q48 | Curiosity | Hoef niet te weten achter nieuwe tool | **Ja** |

**Geen geautomatiseerde contrary-wording guard** voor cultuur (alleen balans 2F+2R per dimensie) **(a)**.

---

### RIASEC / carrière v2 (18 reverse-items, 6 types × 3R)

| Q | Type | R? | NL stem (kern) | Keying OK? |
|---|------|----|----------------|------------|
| Q05 | Realistic | R | Vermijd vies/zwaar/buiten | **Ja** |
| **Q07** | Realistic | R | **Zwaar tillen/vies schrikt niet af** | **Nee — pro-R + R** **(must)** |
| Q08 | Realistic | R | Vermijd fysiek werk | **Ja** |
| Q12 | Investigative | R | Onderbuikgevoel vs onderzoek | **Ja** |
| **Q14** | Investigative | R | **Bronnen vergelijken graag** | **Nee — pro-I + R** **(must)** |
| Q16 | Investigative | R | Uitzoekwerk saai | **Ja** |
| Q20 | Artistic | R | Creatief zonder regels lastig | **Ja** |
| **Q22** | Artistic | R | **Muziek/beeld/tekst graag aanpassen** | **Nee — pro-A + R** **(must)** |
| Q24 | Artistic | R | Vast stappenplan vs improviseren | **Ja** |
| Q28 | Social | R | Weinig contact met mensen | **Ja** |
| **Q30** | Social | R | **Praktische tips geven graag** | **Nee — pro-S + R** **(must)** |
| Q32 | Social | R | Vermijd begeleiden/helpen | **Ja** |
| Q36 | Enterprising | R | Anderen voortouw | **Ja** |
| **Q38** | Enterprising | R | **Pitch idee graag** | **Nee — pro-E + R** **(must)** |
| Q40 | Enterprising | R | Vermijd aansturen | **Ja** |
| Q44 | Conventional | R | Losse structuren passen beter | **Ja** |
| **Q46** | Conventional | R | **Gegevens graag ordenen** | **Nee — pro-C + R** **(must)** |
| Q48 | Conventional | R | Regels/formulieren ongeduldig | **Ja** |

**Patroon:** per Holland-type zijn **drie** items `Reverse: true`; **twee** daarvan zijn inhoudelijk correct keyed, **één** is systematisch een **pro-type**-stem (Q7, Q14, Q22, Q30, Q38, Q46). **Geen** career-reverse wording guard **(a)** — alleen minimum aantal reverse per type.

**Correctie-opties (must vóór COTAN):** per item óf `Reverse: false` in `CareerTestCatalogV2.cs` óf stem herschrijven naar anti-type (analoog Q05/Q08). Vertalingen in bank volgen de NL-semantiek **(b)** — fix moet in **alle vijf talen** consistent.

---

## Spot-check vertalingen (gewijzigde waarden + RIASEC-probleemitems)

| Key | NL (bank) | EN (bank) | PL/RO/AR |
|-----|-----------|-----------|----------|
| Values Q04 | Zelf bepalen starttaak | decide which task to start | Equivalent **(b)** |
| Values Q21 | Resultaten zien maakt weinig uit | whether others see results matters little | Equivalent **(b)** |
| Values Q31 | Last-minute wijzigingen plaatsen | can usually accommodate last-minute changes | Equivalent **(b)** |
| Values Q39 | Maatschappelijk effect boeit weinig | societal outcome matters little | Equivalent **(b)** |
| Career Q07 | Vies/zwaar schrikt niet | dirty/heavy does not put off | Pro-R in alle talen **(b)** |
| Career Q38 | Pitch idee graag | like pitching an idea | Pro-E in alle talen **(b)** |

---

## Per COTAN-criterium (eindstand ronde 4)

| # | Criterium | PR #647 / materiaal **nu** | Na career Q7/14/22/30/38/46 + cultuur Q20 + should-opruiming **(c)** ondeelbaar |
|---|-----------|----------------------------|-------------------------------------------------------------------------------------|
| 1 | Uitgangspunten testconstructie | **Voldoende** | **Voldoende** → **Goed** met externe constructreview |
| 2 | Kwaliteit testmateriaal | **Onvoldoende** (RIASEC + 1 cultuur-item) | **Voldoende** (waarden + competentie + overige cultuur) |
| 3 | Handleiding | **Voldoende** (§5.1 scorer; matrix waarden) | **Voldoende** → **Goed** (EN manual, RIASEC-matrix, expert sign-off) |
| 4 | Normen | **Onvoldoende** / **(c)** | **Onvoldoende** tot normering |
| 5 | Betrouwbaarheid | **(c)** | **(c)** |
| 6a | Begripsvaliditeit | **(c)**; materiaal **deels voldoende** | **(c)** na EFA/CFA |
| 6b | Criteriumvaliditeit | **(c)** | **(c)** |

---

## Restlijst (na 1333e078)

| Prioriteit | Actie |
|------------|--------|
| **Must (COTAN)** | RIASEC: keying of stems Q07, Q14, Q22, Q30, Q38, Q46 |
| **Must (COTAN)** | Cultuur Q20: reverse-stem of flag |
| **Should** | Career/culture reverse guards (analogaan waarden) |
| **Should** | Supplement legacy `AssessmentV2.Values.*` verwijderen |
| **Should** | RIASEC constructmatrix; EN eindgebruikershandleiding |
| **Empirisch (c)** | N≥300/instrument, normen, DIF, retest — studieplan §7.2 |

---

## Conclusie ronde 4

Commit **`1333e078`** **lost de ronde-3 waarden-blockers af** en versterkt het pakket (matrix, guards, Q04). **Reverse-review over alle vier v2-tests** toont **competentie en waarden keying consistent**; **RIASEC (6 items) en cultuur Q20** blijven **scoringdefecten** voor elk gebruik waarin v2-scores interpretabel moeten zijn.

| Vraag | Verdict |
|-------|---------|
| **#647 merge?** (build, tests, v1 geldig) | **Ja** — met expliciete follow-up voor RIASEC/cultuur vóór v2 “COTAN-concept”-communicatie |
| **COTAN concept-indiening (zonder normen)?** | **Nee** — eerst must-fix RIASEC + Q20; daarna realistisch **concept** met eerlijk **(c)**-plan |

---

*Einde herbbeoordeling v2 ronde 4 — geen COTAN-uitspraak.*
