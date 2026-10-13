# Onafhankelijke herbbeoordeling Quick-Scan v2 — ronde 3 (PR #647, commit 16f9892a)

**Datum:** 13 oktober 2026  
**Reviewer:** onafhankelijke, sceptische psychometrische herbeoordeling (COTAN-beoordelaarsstijl)  
**Scope:** PR **#647**, branch `cursor/tests-v2-cotan-alignment-7848`, commit **`16f9892a`** (fix duplicate stems waarden/RIASEC)  
**Baseline:** ronde 2 — `docs/psychometrie/HERBEOORDELING_V2_RONDE2.md`  
**Referentiekader:** COTAN 2010 + addenda (unproctored, fairness, tabellen). **Geen officiële COTAN-uitspraak.**

---

## Bewijstypen

| Label | Betekenis |
|-------|-----------|
| **(a)** | Geverifieerd in code, tests of handleiding op commit 16f9892a |
| **(b)** | Handmatig itemlezen / script / screenshots in deze review |
| **(c)** | Niet te beoordelen zonder responsdata of externe studies |

---

## Executive summary

### Klaar om in te dienen bij COTAN?

**Nee — nog niet.** Ronde-2 **blokkers** (duplicate stems waarden/RIASEC) zijn **inhoudelijk opgelost** in de actieve itembank (`UiStringsAssessmentV2ValuesCareerBank.cs`) en afgedwongen met `V2_item_stems_are_unique_within_each_language` **(a)(b)**. Intro-copy toont **40** items bij v2 **(b)**. Scorer-sectie in handleiding is toegevoegd **(a)**.

**Nieuwe must-bevinding ronde 3:** **drie reverse-keying fouten** in `SchwartzValuesCatalogV2` — pro-stems staan als `Reverse: true`, waardoor hoge agreement **lager** op de schaal scoort (Q21 Prestatie, Q31 Stabiliteit, Q39 Impact) **(a)(b)**. Dat is een **scoringdefect**, niet alleen tekst.

**Na correctie reverse-flags + resterende documentatie/expert-stappen:** **voorlopige indiening** bij COTAN (materiaal + handleiding + studieplan, **zonder** normen) is **realistisch**; **registratie/“goed”** blijft afhankelijk van **empirische** normen, betrouwbaarheid en validiteit **(c)**.

---

## Verificatie ronde-2 blockers (commit 16f9892a)

| Blocker ronde 2 | Status ronde 3 | Bewijs |
|-----------------|----------------|--------|
| Waarden: duplicate stems over schalen | **Opgelost** (runtime) | 40 unieke NL-stems; guard per taal **(a)**; handmatig alle 40 NL gelezen **(b)** |
| RIASEC: duplicate stems | **Opgelost** | 6×8 unieke stems per type **(b)** |
| PL/RO/AR waarden ≠ NL-copy | **Opgelost** in bank | Spot-check Q09/Q21/Q39 en steekproef per schaal **(b)** |
| Intro “25 zinnen” bij v2 | **Opgelost** | `TestFlow.Lead.Competence.V2` + `_fullCount` in razor **(a)(b)** screenshot |
| Scorer-handleiding | **Deels opgelost** | §5.1 technische scorer-tabel **(a)** — nog geen aparte eindgebruikers-EN-manual |
| Duplicate-stem guard | **Toegevoegd** | 5 talen in `AssessmentV2TranslationGuardTests` **(a)** |

**Technische kanttekening (should):** oude waarden/RIASEC-strings staan nog in `UiStringsAssessmentV2.cs` `Nl()`/`En()`/… **(a)**; merge-volgorde is supplement → **bank last**, dus **runtime = bank** **(a)**. Dead code verhoogt risico bij toekomstige merge-wijziging.

---

## Technische verificatie

| Stap | Resultaat |
|------|-----------|
| `dotnet build Jobsy.sln` (Release) | **Geslaagd** **(b)** |
| `AssessmentV2TranslationGuardTests` + QuickScan + `TestDepthRules` | **47/47 geslaagd** **(b)** |
| `TestsV2PlaywrightTests` | **5/5 geslaagd** **(b)** |
| Visuele controle screenshots | v2-transparantie, indicatieve banner, item “Ik maak af wat ik beloof…”, intro **40 korte zinnen** **(b)** |

---

## Handmatige review — Waarden v2 (NL, alle 40 items)

Legend: F = forward, R = reverse in catalogus. **Construct** = reviewer-oordeel inhoud vs driver.

### Autonomie (Q01–Q08)

| Item | R | Stem (samenvatting) | Construct |
|------|---|---------------------|-----------|
| Q01 | F | Zelf beslissen wanneer welke taak | **Passend** |
| Q02 | F | Eigen werkwijze kiezen | **Passend** |
| Q03 | R | Precies instructies (tegen autonomie) | **Passend** |
| Q04 | F | Nieuwe taken/manieren motiveren | **Twijfel** — stimulatie/verandering; zwak autonomie, geen blokker |
| Q05 | R | Vaste routines vs vrijheid | **Passend** |
| Q06 | F | Zelf volgorde taken | **Passend** |
| Q07 | R | Vrijheid zonder doelen onrustig | **Passend** |
| Q08 | R | Leiding bepaalt organisatie | **Passend** |

### Verbinding (Q09–Q16)

| Item | R | Construct |
|------|---|-----------|
| Q09–Q10 | F | Team/support, collega's — **passend** |
| Q11, Q13, Q15–Q16 | R | Geen hulp, afleiding, weinig contact — **passend** |
| Q12, Q14 | F | Contact behoefte, geruststellen — **passend** |

### Prestatie (Q17–Q24)

| Item | R | Construct |
|------|---|-----------|
| Q17–Q18, Q20, Q22 | F | Trots targets, erkenning, hoge eisen, groei — **passend** |
| Q19, Q23–Q24 | R | Niet beste hoeven, titels/competitie onbelangrijk — **passend** |
| **Q21** | **R** | “Resultaten die anderen zien, drijven mij aan” — **inhoud pro-prestatie, reverse flag FOUT** **(must)** |

### Stabiliteit (Q25–Q32)

| Item | R | Construct |
|------|---|-----------|
| Q25–Q26, Q28, Q30 | F | Vast rooster, duidelijke afspraken, voorspelbaar, baanzekerheid — **passend** |
| Q27, Q29, Q32 | R | Tolereren wijzigingen, wisselende prioriteiten, losse regels — **passend** |
| **Q31** | **R** | “Onverwachte wijzigingen … storen me” — **hoge agreement = hoge stabiliteit; reverse flag FOUT** **(must)** |

### Impact (Q33–Q40)

| Item | R | Construct |
|------|---|-----------|
| Q33–Q34, Q36, Q38 | F | MVO, sociale projecten, helpen, eerlijke samenleving — **passend** |
| Q35, Q37, Q40 | R | Salaris zwaarder, geen verschil buiten team, geen werk beyond tasks — **passend** |
| **Q39** | **R** | “Maatschappelijk effect … het belangrijkst” — **pro-impact, reverse flag FOUT** **(must)** |

**Double-barreled:** geen ernstige gevallen; Q35 “salaris **of** status” is bewuste trade-off, acceptabel B1 **(b)**.  
**Taalniveau B1:** korte zinnen, geen jargon — **voldoende** **(b)**.  
**Reverse-balans:** 4F+4R per driver in catalogus **(a)** — **klopt**, behalve **verkeerde** reverse-toewijzing op Q21/Q31/Q39.

---

## Handmatige review — RIASEC v2 (NL, alle 48 items)

Per type **8 unieke stems**, thematisch passend bij Holland-type **(b)**. Voorbeelden:

| Type | Id’s | Reviewer |
|------|------|----------|
| Realistic | Q01–Q08 | Machines, praktijk, fysiek — **passend** |
| Investigative | Q09–Q16 | Manuals, feiten, analyse — **passend** |
| Artistic | Q17–Q24 | Vorm, creatie, brainstorm — **passend** |
| Social | Q25–Q32 | Steun, uitleg, luisteren — **passend** |
| Enterprising | Q33–Q40 | Leiding, onderhandelen, pitch — **passend** |
| Conventional | Q41–Q48 | Checklist, planning, procedures — **passend** |

Reverse-balans: ≥3 forward + ≥3 reverse per type **(a)**.

---

## Spot-check vertalingen (betekenis-equivalentie)

Steekproef per driver + RIASEC-type; volledige bank parallel in EN/PL/RO/AR **(a)**.

| Key | NL (kern) | EN / PL / RO / AR |
|-----|-----------|-------------------|
| Q09 Connection | Team ondersteuning | Equivalent **(b)** |
| Q21 Achievement | Zichtbare resultaten drijven | Equivalent tekst; **reverse-fout geldt in alle talen** **(b)** |
| Q31 Stability | Wijzigingen storen | Equivalent **(b)** |
| Q39 Impact | Sociaal effect belangrijkst | Equivalent **(b)** |
| Career Q13 | Oorzaak storing | Equivalent **(b)** |
| Career Q38 | Idee pitchen | Equivalent **(b)** |

Geen NL-fallback in actieve bank voor waarden/RIASEC **(b)**.

---

## Per COTAN-criterium

| # | Criterium | Nu (indiening) | Na reverse-fix + N≈300 pilot **(c)** |
|---|-----------|----------------|----------------------------------------|
| 1 | Uitgangspunten testconstructie | **Voldoende** | **Voldoende** → **Goed** met externe constructreview |
| 2 | Kwaliteit testmateriaal | **Onvoldoende** (reverse-keying) | **Voldoende** |
| 3 | Handleiding | **Voldoende** | **Voldoende** → **Goed** (EN + voorbeeldinterpretatie + contraindicaties) |
| 4 | Normen | **Onvoldoende** / **(c)** | **Voldoende** na normering |
| 5 | Betrouwbaarheid | **(c)** | **Voldoende** na α/ω-rapport |
| 6a | Begripsvaliditeit | **(c)** / materiaal **voldoende*** | **Voldoende** na EFA/CFA |
| 6b | Criteriumvaliditeit | **(c)** | **Voldoende** na prospectieve uitkomsten |

\*Na Q21/Q31/Q39 fix; inhoud uniek en schaal-thematisch **voldoende**, geen CFA yet.

---

## Wat blijft **boven** empirische data (expliciet)

### Must vóór indiening

1. **Reverse-keying** corrigeren: Q21 → forward; Q31 → forward; Q39 → forward (catalogus + regressietest op keying vs stempolariteit).  
2. **Opruimen** legacy waarden/RIASEC-entries in `UiStringsAssessmentV2.cs` of merge-documentatie, zodat alleen de bank bron van waarheid is.

### Should (COTAN-intake, geen normen vereist)

3. Construct→item-matrix in handleiding (1 pagina per instrument).  
4. Engelse testhandleiding (parallel NL).  
5. Externe arbeidspsycholoog: korte sign-off itembank 2.0 + unproctored-beleid.  
6. Gebruikershandleiding vs scorer-handleiding scheiden (§5.1 is technisch, nog kort).  
7. `Curiosity`→`Openness` scoring cultuur expliciet in §5.  
8. E2E per **app-taal** (niet alleen Playwright-locale).

### Alleen empirisch **(c)** — verwacht na indiening/parallel

9. N≥300/instrument: α/ω, item-rest, eventueel EFA/CFA.  
10. Normen (N≥500) + SE.  
11. Criteriumvaliditeit match/uitkomsten.  
12. DIF gender/taal/leeftijd.  
13. Test–retest subsample.

---

## Restlijst en PR #647

| Prioriteit | Actie |
|------------|--------|
| **Must** | Fix reverse Q21, Q31, Q39 |
| **Should** | Dead strings verwijderen; EN manual; expert sign-off |
| **Empirisch** | Validatiestudie volgens handleiding §7.2 |

---

## Conclusie ronde 3

Commit **16f9892a** adresseert de **kern** van ronde 2 (unieke waarden- en RIASEC-itembank, guards, intro-aantallen, scorer-sectie). Voor **COTAN-indiening vandaag**: **nee** — reverse-keying op drie waarden-items ondermijnt scoring. **Na die fix** is het pakket **indieningswaardig als conceptinstrument** met eerlijk studieplan; **volledige COTAN-kwalificatie** vereist nog **empirische** normen, betrouwbaarheid en validiteit.

---

*Einde herbbeoordeling v2 ronde 3 — geen COTAN-uitspraak.*
