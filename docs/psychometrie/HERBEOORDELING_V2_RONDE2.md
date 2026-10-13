# Onafhankelijke herbbeoordeling Quick-Scan v2 — ronde 2 (PR #647)

**Datum:** 13 oktober 2026  
**Reviewer:** onafhankelijke, sceptische psychometrische herbeoordeling (COTAN-beoordelaarsstijl)  
**Scope:** branch `cursor/tests-v2-cotan-alignment-7848` (PR **#647**), commit `e5d49fc1e0b8863cc8b6613dee2911a71fda0093`  
**Baseline ronde 1:** `docs/psychometrie/ONAFHANKELIJKE_HERBEOORDELING_V2.md` (PR #648)  
**Referentiekader:** COTAN Beoordelingssysteem 2010 + addenda unproctored (2018), actualisatie (2018), fairness (2021), tabellen (2021).  
**Status:** geen officiële COTAN-uitspraak — dit document vervangt geen registratie.

---

## Bewijstypen

| Label | Betekenis |
|-------|-----------|
| **(a)** | Geverifieerd in code, handleiding of tests op PR #647 |
| **(b)** | Berekend of handmatig itemlezen in deze review (scripts, screenshots) |
| **(c)** | Niet te beoordelen zonder responsdata of externe studies |

---

## Executive summary

### Klaar om in te dienen bij COTAN?

**Nee.**

Ronde 1 **must-fixes** rond placeholders, competentie-schaalteksten, cultuur-itemcount, vertaal-parity guards, handleiding-uitbreiding en `IsIndicativeDepth` zijn grotendeels **technisch afgehandeld** op PR #647. Dat is **niet** hetzelfde als indieningsrijp: de **waarden-scan v2** heeft een **systematische inhoud–construct-fout** (19 van 40 stems zijn letterlijke duplicaten van andere schalen), RIASEC v2 heeft **hergebruikte stems**, en **alle empirische criteria** (normen, α, validiteit, DIF) ontbreken nog. COTAN verwacht aantoonbare kwaliteit op **alle** criteria; een dekkende handleiding en groene unit tests rechtvaardigen geen indiening zolang het itemmateriaal de schaalstructuur ondergraaft.

### Korte tabel per criterium (nu vs na pilot N≈300)

| COTAN | Onderdeel | Nu (indiening) | Na pilot N≈300 + fixes* |
|-------|-----------|----------------|-------------------------|
| 1 | Uitgangspunten testconstructie | **Onvoldoende** | **Voldoende** (mits waarden-/RIASEC-itembank herzien + expert review) |
| 2 | Kwaliteit testmateriaal | **Onvoldoende** | **Voldoende** (mits itemfixes + vertaal-QA in UI) |
| 3 | Kwaliteit handleiding | **Voldoende** (concept) | **Voldoende** → **Goed** (met scorer-manual, EN, voorbeeldrapporten) |
| 4 | Normen | **Onvoldoende** / **(c)** | **Voldoende** (mits normering N≥500 na betrouwbaarheid) |
| 5 | Betrouwbaarheid | **(c)** / inhoud **onvoldoende** | **Voldoende** (mits α/ω per schaal ≥ drempel op v2-data) |
| 6a | Begripsvaliditeit | **Onvoldoende** | **Voldoende** (mits EFA/CFA + itemfix) |
| 6b | Criteriumvaliditeit | **(c)** | **Voldoende** (mits prospectieve match-/uitkomststudie) |

\*Pilot N≈300 is **noodzakelijk maar niet voldoende** zolang de waarden-itembank niet inhoudelijk is herzien; anders meet de pilot vooral artefacten.

---

## Verificatie must-bevindingen ronde 1

| # | Must (ronde 1) | Status ronde 2 | Bewijs |
|---|----------------|----------------|--------|
| 1 | Geen literal `AssessmentV2.*` in UI | **Opgelost** | `AssessmentV2TranslationGuardTests`; Playwright assert; screenshots tonen natuurlijke zinnen **(a)(b)** |
| 2 | Competentie-items onder juiste schaal | **Opgelost** (NL) | Alle 40 NL-stems handmatig gelezen vs `CompetencyTestCatalogV2` **(b)** — zie bijlage A |
| 3 | Cultuur ≥4 items per facet | **Opgelost** | 48 items, 12×4; guard test **(a)** `CulturePersonalityCatalogV2.cs` |
| 4 | Waarden PL/RO/AR echte vertalingen | **Deels opgelost** | 0/40 PL-waarden identical aan NL in supplement **(b)**; **maar** vertaalde stems volgen dezelfde foute NL-itemlogica **(b)** |
| 5 | Parity/guard tests v2 | **Opgelost** | 10 tests in `AssessmentV2TranslationGuardTests` **(a)** |
| 6 | `IsIndicativeDepth` in UI + match | **Opgelost** | `ToDto`: `depthCount < fullCount`; banner `data-testid=assessment-indicative-depth`; match evidence **(a)** |
| 7 | Handleiding COTAN-structuur + unproctored | **Deels opgelost** | Hoofdstukken 1–11 **(a)** `HANDLEIDING_ASSESSMENTS_V2.nl.md`; geen aparte scorer-manual / EN **(a)** |
| 8 | Geen verzonnen normtabellen | **Opgelost** | Handleiding §7.1–7.2; ruwe 0–100% in UI **(a)** |
| — | Empirische normen/α/validiteit | **Open (c)** | Gepland, geen data — verwacht |

---

## Technische verificatie (build & tests)

Uitgevoerd op commit `e5d49fc1` **(a)(b)**:

| Stap | Resultaat |
|------|-----------|
| `dotnet build Jobsy.sln` (Release) | **Geslaagd** |
| `AssessmentV2TranslationGuardTests` + `AssessmentQuickScanV2Tests` + `TestDepthRulesTests` | **42/42 geslaagd** |
| `TestsV2PlaywrightTests` (apart gefilterd) | **5/5 geslaagd** |
| `TestsV2PlaywrightTests` in gecombineerde batch met andere tests | **1 flaky failure** (TestHost niet gestart — race); **geen** productdefect |

Screenshots (visuele controle **(b)**):

- NL desktop intro + vraag: echte itemtekst, v2-transparantieblok, indicatieve-diepte-banner.
- PL/AR Playwright-locale wijzigt **niet** automatisch de app-taal (items blijven NL in stub-test); product-i18n moet via taalkiezer/profiel — **niet** opnieuw end-to-end getest op live Acceptatie.

---

## Per COTAN-criterium (uitgebreid)

### 1 — Uitgangspunten testconstructie

| Aspect | Oordeel | Toelichting |
|--------|---------|-------------|
| **Nu** | **Onvoldoende** | Handleiding v2 beschrijft meetpretentie, verboden gebruik, unproctored, AI Act **(a)**. Versiebeleid `QuickScanVersion` + `TestsV2Enabled` **(a)**. **Maar:** waarden-instrument claimt vijf Schwartz-drivers terwijl 19 items inhoudelijk bij andere drivers horen **(b)** — operationalisatie klopt niet. |
| **Na N≈300** | **Voldoende** | Mogelijk na itembank 2.1, gedocumenteerde construct→item-matrix, externe arbeidspsycholoog review. |

### 2 — Kwaliteit testmateriaal

| Instrument | Items | Reverse-balans | Vertalingen | Inhoud reviewer |
|------------|-------|----------------|-------------|-----------------|
| **Competentie v2** | 40, 8×5 | 4F+4R per schaal **(a)** | nl/en/pl/ro/ar, geen key==value **(a)(b)** | **Voldoende** — 40/40 NL stems passen bij schaal **(b)** bijlage A |
| **Cultuur v2** | 48, 12×4 | 2F+2R per dimensie **(a)** | Volledig in 5 talen **(a)** | **Voldoende** — 48/48 items thematisch passend **(b)** bijlage B; `Curiosity` wordt gescoord als `Openness` **(a)** — in handleiding expliciteren |
| **Carrière v2** | 48, 8×RIASEC | ≥3F/≥3R per type **(a)** | 5 talen **(a)** | **Onvoldoende** — 10 hergebruikte NL-stems over types **(b)** |
| **Waarden v2** | 40, 8×5 drivers | 4F+4R **(a)** | pl/ro/ar ≠ nl **(b)** | **Onvoldoende** — 19 duplicate stems over schalen **(b)** bijlage C |

**Voorbeeld waarden-fout (NL) (b):** `Connection` Q09–Q13 zijn tekstelijk identiek aan `Autonomy` Q01–Q05; `Achievement`, `Stability` en `Impact` herhalen dezelfde blokken. De respondent scoort dus **verbinding/prestatie/stabiliteit/impact** op **autonomie-inhoud**.

**UI-detail:** introtekst noemt nog «25 korte zinnen» terwijl v2 **40** items heeft **(b)** (screenshots) — verwarrend voor respondenten, geen psychometrische kernfout maar wel kwaliteit testadministratie.

| | Nu | Na N≈300 |
|---|-----|----------|
| Oordeel | **Onvoldoende** | **Voldoende** mits itembank fixes + eventueel RIASEC-stems vervangen |

### 3 — Kwaliteit handleiding

| | Nu | Na N≈300 |
|---|-----|----------|
| Oordeel | **Voldoende** (intern concept) | **Voldoende**–**Goed** |

**Aanwezig (a):** doel, populatie, materiaal, unproctored §4, scoring, interpretatie zonder normen, beperkingen, privacy, technische refs — hoofdstukken 1–11.

**Ontbreekt voor COTAN-indiening:** aparte gebruikers- vs scorer-handleiding, contraindicaties uitgewerkt, voorbeeldinterpretaties **met** normen (na studie), literatuurlijst per construct, Engelse parallel, technische fiche per subtest, documentatie `Curiosity`→`Openness`-scoring.

### 4 — Normen

| | Nu | Na N≈300 |
|---|-----|----------|
| Oordeel | **Onvoldoende** / **(c)** | **Voldoende** (met aparte normeringssteekproef N≥500) |

Eerlijk normenplan zonder fake tabellen **(a)**. Geen percentielen/SE in product **(a)**.

### 5 — Betrouwbaarheid

| | Nu | Na N≈300 |
|---|-----|----------|
| Oordeel | **(c)** | **Voldoende** (indien α/ω per schaal rapporteren met CI) |

8 items/schaal is **theoretisch** beter dan v1 **(b)**; waarden-duplicaten **inflateren** schaalcorrelaties kunstmatig — betrouwbaarheid rapporteren vóór itemfix is misleidend.

### 6a — Begripsvaliditeit

| | Nu | Na N≈300 |
|---|-----|----------|
| Oordeel | **Onvoldoende** | **Voldoende** (mits CFA/EFA op gecorrigeerde bank) |

Competentie/cultuur: geen ernstige mismatch meer **(b)**. Waarden + RIASEC: **materiaalrisico hoog** **(b)**. Geen factorstudies **(c)**.

### 6b — Criteriumvaliditeit

| | Nu | Na N≈300 |
|---|-----|----------|
| Oordeel | **(c)** | **Voldoende** (mits prospectieve uitkomsten) |

Match-transparantie (`AssessmentMatchEvidence`, uncertainty) **(a)** is **product-ethisch voldoende**, geen COTAN-criteriumvaliditeit.

---

## Fairness & unproctored (addenda)

| Thema | Nu | Opmerking |
|-------|-----|-----------|
| **Fairness vertalingen** | **Voldoende** (strings) / **Onvoldoende** (inhoud waarden) | Geen NL-fallback meer voor v2-waarden in pl/ro/ar **(b)**; inhoudelijke duplicaten gelden in **alle** talen **(b)** |
| **DIF** | **(c)** | Geen programma |
| **Unproctored** | **Voldoende** (basis) | Handleiding §4 + consent **(a)**; geen fraude-detectie |

---

## Geprioriteerde restlijst

### Must (blokkerend vóór COTAN-indiening)

1. **Waarden v2:** volledige itembank herschrijven — unieke stems per driver, expert review Schwartz-workplace mapping **(b)**.  
2. **Vertalingen waarden:** na NL-fix opnieuw professioneel vertalen pl/ro/ar/en (niet alleen «≠ nl» testen).  
3. **RIASEC v2:** duplicate stems vervangen; documenteer balance per type **(b)**.  
4. **Constructdocumentatie:** matrix item→schaal in handleiding; `Curiosity`/Openness-scoring **(a)**.  
5. **Empirische studie** op **gecorrigeerde** v2-bank: N≥300/instrument (α), daarna normen/criterium **(c)**.  
6. **Geen COTAN-marketing** zolang v2 achter `TestsV2Enabled` (default uit) **(a)**.

### Should

7. Handleiding: scorer-manual, EN, literatuur, voorbeeldcases.  
8. UI-copy: «25 zinnen» → versie-aware (40 bij v2).  
9. E2E-screenshots per taal via echte taalkiezer, niet alleen Playwright-locale.  
10. DIF-plan + uitvoering na N.  
11. v1 NL-fallback blokkeren wanneer v2 actief is.

### Could

12. CFA-publicatie; convergentie met externe instrumenten.  
13. Externe audit GZ-/ registratiepsycholoog.

---

## Bijlage A — Competentie v2 (NL, alle 40 items)

| Id | Schaal | Stem (kern) |
|----|--------|-------------|
| 1–8 | Samenwerken | samenwerken, helpen, luisteren, afzijdig (R), gevoelens (R), overleg, solo (R), overleg waste (R) |
| 9–16 | Resultaatgerichtheid | beloftes, planning, details, uitstel (R), rommel (R), overzicht, deadlines (R), slordig (R) |
| 17–24 | Stressbestendigheid | kalm, herstel, multitask, piekeren (R), onverwacht (R), tijdsdruk, frustratie (R), kritiek (R) |
| 25–32 | Innovatie | nieuwe oplossingen, leren, verbanden, vasthouden oud (R), verandering lastig (R), verbeteren, experimenteren (R), vermijden nieuw (R) |
| 33–40 | Extraversie | energie contact, sociale werkomgeving, woord nemen, alleen (R), drukte (R), gesprek onbekenden, uitputting (R), afwachten (R) |

Reviewer: **inhoud–schaal consistent** **(b)**.

---

## Bijlage B — Cultuur v2 (NL, 48 items)

Alle 48 catalogusregels gelezen **(b)**. Cultuur- en persoonlijkheidsdimensies (inclusief Q25–Q48 supplement) zijn **thematisch coherent** per `CulturePersonalityCatalogV2`. Enige nuance: facet **Curiosity** (Q23–24, Q47–48) wordt in scoring samengevoegd met **Openness** **(a)** — rapportage moet dat niet als apart Big-Five-facet presenteren zonder disclaimer.

---

## Bijlage C — Waarden v2 duplicate stems (NL)

Script **(b):** 19 itemparen delen **exact dezelfde** stem terwijl `SchwartzValuesCatalogV2` een andere `Category` toekent.

| Keys (voorbeeld) | Toegewezen schalen | Probleem |
|------------------|-------------------|----------|
| Q09–Q13 | Connection | = Q01–Q05 Autonomy |
| Q17–Q19, Q21 | Achievement | herhaling Autonomy/Connection-stems |
| Q25–Q29 | Stability | herhaling eerdere blokken |
| Q33–Q37 | Impact | herhaling eerdere blokken |

Unieke inhoud per driver: vooral Q14–Q16 (Connection), Q20–Q24 (Achievement deels), Q30–Q32 (Stability deels), Q38–Q40 (Impact).

---

## Methodiek ronde 2

- Checkout PR #647 branch; **geen** wijzigingen aan testcode.  
- `dotnet build` + gefilterde tests **(a)**.  
- Playwright: visuele inspectie screenshots in `artifacts/tests-v2-playwright/` **(b)**.  
- Python-extractie NL/PL strings + catalogi **(b)**.  
- Handmatig alle competentie- en cultuuritems gelezen **(b)**.  
- Baseline must-lijst uit PR #648 document.

---

## Conclusie

PR #647 levert **serieuze vooruitgang** op ronde 1 (competentie, cultuur, guards, handleiding, transparantie). Voor **indiening bij COTAN** is het pakket **nog niet gereed**: de **waarden-scan v2** is inhoudelijk **niet valide** door massale stemduplicatie over schalen; **RIASEC v2** heeft duplicate content; empirische normen en validiteit ontbreken. **Aanbeveling:** PR #647 **wel** merge-voorbereidend voor product/transparantie, maar **niet** als «COTAN-indieningsklaar» communiceren tot waarden/RIASEC-itembank en validatiestudie zijn afgerond.

---

*Einde herbbeoordeling v2 ronde 2 — geen COTAN-uitspraak.*
