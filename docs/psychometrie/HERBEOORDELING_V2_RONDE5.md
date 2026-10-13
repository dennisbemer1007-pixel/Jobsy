# Onafhankelijke herbbeoordeling Quick-Scan v2 — ronde 5 (PR #647, commit 03db9f5c)

**Datum:** 13 oktober 2026  
**Reviewer:** onafhankelijke, sceptische psychometrische herbeoordeling (COTAN-beoordelaarsstijl)  
**Scope:** PR **#647**, branch `cursor/tests-v2-cotan-alignment-7848`, commit **`03db9f5c`** (cultuur Q20, RIASEC Q07/14/22/30/38/46, keying-guard alle instrumenten, constructmatrices)  
**Baseline:** ronde 4 — `docs/psychometrie/HERBEOORDELING_V2_RONDE4.md`  
**Referentiekader:** COTAN 2010 + addenda. **Geen officiële COTAN-uitspraak.**

---

## Bewijstypen

| Label | Betekenis |
|-------|-----------|
| **(a)** | Geverifieerd in code, tests of handleiding op commit 03db9f5c |
| **(b)** | Handmatig itemlezen / screenshots in deze review |
| **(c)** | Niet te beoordelen zonder responsdata of externe studies |

---

## Executive summary — twee verdicts

### (1) PR #647 merge?

**Ja.** Build en gerichte tests groen **(a)**; ronde-4 psychometrische blockers (RIASEC ×6, cultuur Q20) **opgelost** in runtime-itembank + afgedwongen door `V2_all_instruments_reverse_items_use_contrary_nl_wording` over **alle 82** reverse-items **(a)(b)**; v1-isolatie intact **(a)**. Restpunten zijn **documentatie/onderhoud** (zie restlijst), geen merge-blockers voor engineering.

### (2) Materiaal klaar voor COTAN **concept-indiening** (zonder normen)?

**Ja — voorwaardelijk.** De **itembank, keying en scoring** zijn op dit commit-niveau **indieningswaardig als conceptinstrument** **(b)**. **Vóór** upload van het dossier naar COTAN: **must** de foutieve NL-kolom in drie constructmatrices herstellen **(a)**. Normen, betrouwbaarheid, validiteit en DIF blijven expliciet **(c)**.

---

## Verificatie ronde-4 must-fixes (03db9f5c)

| Must ronde 4 | Status ronde 5 | Bewijs |
|--------------|----------------|--------|
| Cultuur Q20 agreeableness reverse | **Opgelost** | NL: eigen gemak boven last collega's; bank overschrijft base **(a)(b)** |
| RIASEC Q07, Q14, Q22, Q30, Q38, Q46 | **Opgelost** | Anti-type stems 5 talen; forbidden-phrases in guard **(a)(b)** |
| Keying-guard alle v2-instrumenten | **Toegevoegd** | `V2_all_instruments_reverse_items_use_contrary_nl_wording` — competence, career, culture, values **(a)** |
| Constructmatrices alle tests | **Deels** | Vier bestanden aanwezig; **waarden** NL correct; **competentie/cultuur/RIASEC** kolom “NL stem” bevat **Arabisch** (generatiefout) **(a)** |

---

## Technische verificatie

| Stap | Resultaat |
|------|-----------|
| `dotnet build Jobsy.sln` (Release) | **Geslaagd** **(a)** |
| `AssessmentV2TranslationGuardTests` | **16/16** **(a)** |
| `TestsV2PlaywrightTests` | **5/5** **(a)** |
| v1 / v2-isolatie (gefilterde set) | **29/29** **(a)** |
| Screenshots | `artifacts/tests-v2-playwright/` — intro **40 vragen**, v2-transparantie **(b)** |

---

## Reverse-keying — volledige NL-review (82 items)

**Methode:** catalogus-flags **(a)** + handmatige NL-lezing steekproef en **alle** gewijzigde items **(b)** + guard `V2_all_instruments_reverse_items_use_contrary_nl_wording` op **alle** reverse-keys **(a)**.

| Instrument | # reverse | NL keying (ronde 5) |
|------------|-----------|---------------------|
| Waarden v2 | 20 | **OK** — ongewijzigd t.o.v. 1333e078; Q21/Q31/Q39/Q27 e.d. consistent |
| Competentie v2 | 20 | **OK** — negatieve trait-stems (afzijdig, uitstel, piekeren, vermijden innovatie, …) |
| Cultuur v2 | 24 | **OK** — incl. **Q20** (zie below) |
| RIASEC v2 | 18 | **OK** — incl. **Q07/14/22/30/38/46** (zie below) |

Geen resterende pro-trait + `Reverse: true` combinaties in actieve `UiStrings.Get`-teksten **(a)(b)**.

### Gewijzigde stems (ronde 4→5) — NL + vertaling

| Key | NL (bank, runtime) | EN / PL / RO / AR | Keying | Stemkwaliteit |
|-----|-------------------|-------------------|--------|---------------|
| Culture Q20 | Ik kies soms bewust voor mijn eigen gemak, ook als collega's daar last van hebben. | Comfort boven overlast collega's — equivalent **(b)** | **R OK** | Enkel agreeableness-contrast; **sociaal geladen** maar niet extreem; geen double-barrel |
| Career Q07 | Zwaar tillen of vies werk vermijd ik liever. | avoid heavy/dirty — equivalent **(b)** | **R OK** | Overlap met Q05/Q08 (meerdere anti-R reverses) — **should**, geen keyingfout |
| Career Q14 | Ik geloof cijfers of berichten liever zonder extra bronnen na te trekken. | believe without checking sources **(b)** | **R OK** | “Cijfers **of** berichten” licht double-barrel; acceptabel B1 |
| Career Q22 | Creatief bezig zijn met vorm, kleur of stijl spreekt me weinig aan. | appeals little **(b)** | **R OK** | Eén construct (artistiek); **OK** |
| Career Q30 | Ik neem liever geen tijd om collega's of klanten te coachen. | prefer not coaching **(b)** | **R OK** | “Collega's **of** klanten” licht double-barrel; **OK** |
| Career Q38 | Ik pitch of verkoop mijn plannen aan leiding liever niet. | prefer not pitch/sell **(b)** | **R OK** | Pitch **of** verkoop — parallel formulering; **OK** |
| Career Q46 | Nauwkeurig administratie of gegevens ordenen vind ik vooral saai. | admin/sorting boring **(b)** | **R OK** | Twee voorbeelden, één anti-C-thema; **OK** |

**Runtime-merge:** `UiStringsAssessmentV2ValuesCareerBank.MergeAll` **laatste** laag **(a)** — cultuur Q20 + RIASEC-fixes actief. **Stale** pro-trait Q20 staat nog in `UiStringsAssessmentV2.cs` **(a)** — geen runtime-effect, **wel onderhoudsrisico**.

---

## Constructmatrices (documentatie)

| Bestand | NL-kolom | Inhoud Q20 / gewijzigde RIASEC |
|---------|----------|----------------------------------|
| `VALUES_V2_CONSTRUCT_MATRIX.md` | **Correct NL** **(a)** | Q21/Q31/Q39 up-to-date |
| `COMPETENCE_V2_CONSTRUCT_MATRIX.md` | **Fout: Arabisch** **(a)** | Niet bruikbaar als NL-referentie |
| `CULTURE_V2_CONSTRUCT_MATRIX.md` | **Fout: Arabisch** **(a)** | Q20-regel nog **oude** pro-agreeableness-tekst |
| `RIASEC_V2_CONSTRUCT_MATRIX.md` | **Fout: Arabisch** **(a)** | Kolom wél overeenkomstig **AR** bank, niet NL |

**Impact:** geen scoring-bug **(a)**; **wel** must vóór COTAN-dossier (reviewers lezen matrices).

---

## Per COTAN-criterium (eindstand ronde 5)

| # | Criterium | Stand op 03db9f5c |
|---|-----------|-------------------|
| 1 | Uitgangspunten testconstructie | **Voldoende** |
| 2 | Kwaliteit testmateriaal (items/keying) | **Voldoende** |
| 2b | Kwaliteit **indieningsdocumentatie** (matrices) | **Onvoldoende** tot NL-kolom gefixed |
| 3 | Handleiding | **Voldoende** (§5.1 scorer; waarden-matrix OK) → **Goed** na EN-manual + expert sign-off **(should)** |
| 4 | Normen | **Onvoldoende** / **(c)** |
| 5 | Betrouwbaarheid | **(c)** |
| 6a | Begripsvaliditeit | **(c)**; materiaal **voldoende** voor concept |
| 6b | Criteriumvaliditeit | **(c)** |

---

## Restlijst (precies)

### Must vóór COTAN-dossier (geen normen)

1. **`COMPETENCE_V2_CONSTRUCT_MATRIX.md`**, **`CULTURE_V2_CONSTRUCT_MATRIX.md`**, **`RIASEC_V2_CONSTRUCT_MATRIX.md`**: kolom “NL stem (referentie)” vullen met **Nederlandse** bankteksten; cultuur Q20 + RIASEC Q07/14/22/30/38/46 alignen met bank.

### Should (product / onderhoud, geen scoring)

2. Verwijder of align **stale** `AssessmentV2.Culture.Q20` in `UiStringsAssessmentV2.cs` (5 talen).  
3. Opruimen **stale** `AssessmentV2.Values.*` in `UiStringsAssessmentV2Supplement.cs` (ronde 2/3).  
4. RIASEC: **redundantie** Realistic reverse Q05/Q07/Q08 (drie vermijd-fysiek-stems) — optionele herformulering.  
5. Engelse **eindgebruikers**-handleiding; externe arbeidspsycholoog **sign-off** itembank 2.0; Curiosity→Openness expliciet in §5; E2E per app-taal.

### Alleen empirisch **(c)**

6. N≥300 per instrument: α/ω, item-rest, EFA/CFA waar passend.  
7. Normen (N≥500) + SE.  
8. Criteriumvaliditeit (match/uitkomsten).  
9. DIF gender/taal/leeftijd; test–retest subsample.

---

## Conclusie ronde 5

Commit **`03db9f5c`** sluit de **psychometrische keying-review** af voor alle vier v2-instrumenten **(a)(b)**. PR **#647** is **mergebaar**. **Concept-indiening bij COTAN zonder normen** is **verantwoord** zodra de **drie constructmatrices** NL-correct zijn en het studieplan **(c)** eerlijk blijft; **registratie/kwalificatie** vereist nog empirische stappen.

| Vraag | Verdict |
|-------|---------|
| **(1) #647 merge?** | **Ja** |
| **(2) COTAN concept (zonder normen)?** | **Ja, voorwaardelijk** — items/keying **ja**; dossier **must:** matrix-NL fix (#1 hierboven) |

---

*Einde herbbeoordeling v2 ronde 5 — geen COTAN-uitspraak.*
