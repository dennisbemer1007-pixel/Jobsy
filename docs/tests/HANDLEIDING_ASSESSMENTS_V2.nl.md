# Testhandleiding Quick-Scan assessments v2 (Lobsy)

**Versie:** v2 itembank + **v3 tier** (`QuickScanVersion` 1 / 2 / 3)  
**Status:** interne handleiding — **geen COTAN-registratie**, geen normtabellen tot validatiestudie is afgerond.  
**Feature flag:** `TestsV2Enabled` (standaard uit). Nieuwe kandidaten krijgen bij flag **aan** `QuickScanVersion = 3` (25 free items). Legacy v2 (`= 2`) en v1 blijven geldig.

---

## Hoofdstuk 1 — Doel en meetpretentie

### 1.1 Doel
De Quick-Scan v2 ondersteunt **oriëntatie op werk**: competenties, RIASEC-interesses, werkcultuurvoorkeur en waarden. Output is bedoeld voor **gesprek, matching-hulp en paspoort**, niet voor automatische selectie.

### 1.2 Wat wordt níet gemeten
- Geen medische of psychologische diagnose.
- Geen IQ, pathologie of klinische schalen.
- Geen arbeidsongeschiktheids- of veiligheidskeuring.

### 1.3 Toegestaan gebruik
- Kandidaat zelfinzicht en vacature-exploratie.
- Recruiter-ondersteuning **met menselijke beoordeling** (AI Act: geen louter geautomatiseerde afwijzing).
- Interne matching-scores **indicatief** bij onvolledige scan (zie hoofdstuk 6).

### 1.4 Verboden / voorzichtig gebruik
- Enige selectiebeslissing zonder menselijke review.
- Sterke claims (“boven gemiddeld”, “objectief beste kandidaat”) zonder normen.
- Hergebruik van v1-scores alsof het v2-scores zijn.

---

## Hoofdstuk 2 — Populatie en taal

| Instrument | Construct (indicatief) | Doelgroep | Talen v2 |
|------------|------------------------|-----------|----------|
| Competentie | Samenwerken, resultaat, stress, innovatie, extraversie | Werkzoekenden 16+ | nl, en, pl, ro, ar |
| Carrière (RIASEC) | R-I-A-S-E-C | Idem | nl, en, pl, ro, ar |
| Cultuur & persoonlijkheid | 6 cultuur + 6 persoonlijkheidsfacetten | Idem | nl, en, pl, ro, ar |
| Waarden | Autonomie, verbinding, prestatie, stabiliteit, impact | Idem | nl, en, pl, ro, ar |

Minderjarigen: extra ouder/voogd-consent volgens platformregels.

---

## Hoofdstuk 3 — Testmateriaal (v2 bank, v3 tier)

### 3.1 Drie tiers per instrument (`TestsV2Enabled`)

| Laag | Diepte-token | Items (v3) | Betrouwbaarheid UI |
|------|--------------|------------|--------------------|
| Onboarding / mini | First (5) | 5 | Indicatief |
| Gratis verdieping | Deeper (10) → Full | **25** | Volledige **free** Quick-Scan @ v3 |
| Betaalde diepteanalyse | Bottom | 150–200 (`DeepAnalysisCatalog`) | Deep / paywall; matrices `docs/psychometrie/*_DEEP_CONSTRUCT_MATRIX.md` |

### 3.2 Betaalde diepteanalyse (Bottom)

| Instrument | Items | Theoretische basis | Norm / α |
|------------|-------|-------------------|----------|
| Competentie | 150 | Big Five / IPIP-NEO facetten (werk-B1) | **Pilot n≈300** |
| RIASEC | 200 | Holland | **Pilot n≈300** |
| Waarden | 150 | Schwartz workplace drivers | **Pilot n≈300** |
| Cultuur & persoonlijkheid | 150 | OCP-achtig + Big Five werkfacetten | **Pilot n≈300** |

- NL-stems: `DeepAnalysis*Items.cs`; en/pl/ro/ar: embedded JSON (`DeepItemLocalizations`).
- UI: `DeepAnalysis.razor` — `TestQuestionFlow`, voortgang via `TestPageShell` / `TestDiveScene`, **pauze** tussen parts (`Deep.Pause.*`).
- Geen wijziging betaalflow; alleen itemkoppeling via bestaande `DeepAnalysisCatalog`.

### 3.3 Itembudget per versie (Quick-Scan)

| Instrument | v1 Full | v2 legacy Full | **v3 free Full** |
|------------|---------|----------------|------------------|
| Competentie | 25 | 40 | **25** (v2-subset) |
| Carrière | 25 | 48 | **25** |
| Cultuur | 18 | 48 | **25** |
| Waarden | 25 | 40 | **25** |

v3-id’s: `docs/psychometrie/QUICKSCAN_V3_FREE_TIER_MATRIX.md`.

- **Likert 1–5** (oneens → eens). Reverse items: gescoorde waarde = 6 − ruwe waarde.
- **Schaling:** percent = round((gemiddelde − 1) / 4 × 100), 0–100 (zie `AssessmentScoringDocumentation`).
- Itemteksten: waarden + RIASEC + cultuur Q20-fix in `UiStringsAssessmentV2ValuesCareerBank.cs`; overige v2 in supplement; parity gecontroleerd door `AssessmentV2TranslationGuardTests`. Construct→item matrices: `docs/psychometrie/VALUES_V2_CONSTRUCT_MATRIX.md`, `COMPETENCE_V2_CONSTRUCT_MATRIX.md`, `CULTURE_V2_CONSTRUCT_MATRIX.md`, `RIASEC_V2_CONSTRUCT_MATRIX.md`.

Korte vormen (5/10 items): **indicatief** — `IsIndicativeDepth` en UI-banner `AssessmentV2.Transparency.ShortDepth`.

---

## Hoofdstuk 4 — Administratie (unproctored)

### 4.1 Setting
Thuis of eigen device; geen toezicht (unproctored). Kandidaat beantwoordt zelfstandig.

### 4.2 Integriteit en fraude
- Geen proctoring; geen biometrische identiteitscontrole tijdens de scan.
- Platform: account + consent; rate limits op opslaan; geen “perfect score” beloning.
- **Beleid:** bij vermoeden van oneerlijk invullen (extreme snelheid, inconsistent patroon) mag recruiter **geen** scan als enige bewijs gebruiken; her-test of diepere procedure mogelijk.

### 4.3 Technische administratie
1. Toestemming test + AI (`TestAiConsent`).
2. Keuze diepteniveau; autosave per antwoord.
3. `QuickScanVersion` vastgelegd bij eerste save (`QuickScanVersionPolicy`).

### 4.4 Unproctored-addendum (COTAN 2018)
- Instructie: eerlijk antwoorden, geen “goede” antwoorden kiezen.
- Omgeving: rust, eigen tempo; pauzeren toegestaan.
- Geen tijdslimiet per item (soft UX only).

---

## Hoofdstuk 5 — Scoring en match

### 5.1 Scorer-handleiding (technisch)

Alle v2-instrumenten gebruiken **Likert 1–5** per item. Opgeslagen antwoorden blijven gekoppeld aan **vraagnummer** (`1…N`); wijzigingen in itemtekst (itembank 2.x) raken **niet** de betekenis van reeds opgeslagen ruwe scores zolang `QuickScanVersion` gelijk blijft.

| Stap | Regel | Code-referentie |
|------|--------|-----------------|
| 1 | Parse JSON-antwoorden; valideer bereik 1–5 | `LikertAnswerJson`, `*CatalogV2.ValidateAnswers` |
| 2 | Per schaal: gemiddelde van items; **reverse** items: gebruik `6 − ruwe waarde` vóór middeling | `SchwartzValuesCatalog.Score`, competentie/cultuur/RIASEC-scorers |
| 3 | Schaalpercent = `round((gemiddelde − 1) / 4 × 100)`, clamp 0–100 | `AssessmentScoringDocumentation` |
| 4 | **Indicatieve diepte:** als `QuestionCount` &lt; volledige bank → markeer `IsIndicativeDepth`; geen normvergelijking | `Candidate*Service.ToDto`, `TestDepthRules` |
| 5 | Match: gewogen overlap profiel–vacature; bij indicatieve diepte factor **0,72** + **10 pp** onzekerheid | `AssessmentMatchEvidenceRules`, `ProfileVacancyMatchCalculator` |

**Versie-scheiding:** v1 → v1-catalogi; v2 (legacy extended) → volledige v2-bank; v3 → `QuickScanFreeTierV3` subset. Geen stille migratie van opgeslagen antwoorden.

**Waarden v2:** vijf workplace-drivers (Autonomie, Verbinding, Prestatie, Stabiliteit, Impact) — 8 unieke stems per driver, 4 forward + 4 reverse (Schwartz/PVQ-stijl, B1).

**RIASEC v2:** 8 unieke stems per Holland-type (R–C); geen hergebruikte stems tussen types.

### 5.2 Match-percentage
- Gewogen overlap kandidaat–vacature; cultuur/waarden/engagement-blends zoals in `ProfileVacancyMatchCalculator`.
- **Indicatieve diepte:** `AssessmentMatchEvidence.Indicative` → gewichtsfactor 0,72, **+10 pp onzekerheid**, UI-key `AssessmentV2.Transparency.MatchIndicative` (+ `MatchUncertainty` tekst).

---

## Hoofdstuk 6 — Interpretatie

### 6.1 Ruwe percentages
0–100 = relatieve sterkte **binnen het instrument**, geen normvergelijking tot normstudie.

### 6.2 Volledige vs korte scan
- **Volledig:** stabieler beeld per schaal; match mag als **volledige Quick-Scan** worden gewogen.
- **Eerste blik / Dieper:** alleen **indicatie**; geen sterke claims in UI of rapportage.

### 6.3 Voorbeeld (zonder normen)
“Resultaatgerichtheid 72%” = relatief hoog binnen eigen antwoordpatroon op v2-items; **niet** “boven populatiegemiddelde”.

---

## Hoofdstuk 7 — Normen en precisie

### 7.1 Huidige status
**Geen** gepubliceerde normen, SE of percentielen. UI toont ruwe scores.

### 7.2 Plan (geen fake data)
Zie `docs/psychometrie/VALIDATIE_STUDIEPLAN.md`: fases A (materiaal) → B (α, N≥300/instrument) → D (normen N≥500) → E (criterium) → F (DIF).

---

## Hoofdstuk 8 — Beperkingen

- Geen COTAN-beoordeling; geen externe validatiestudie op Lobsy-data in v2.
- Cultuur/waarden/competentie zijn ** verkorte vormen**, geen volledige Big Five / Schwartz-batterijen.
- RIASEC v2: itembank gecontroleerd op unieke stems per taal (`AssessmentV2TranslationGuardTests`).
- Vertaling: v2 keys gecontroleerd; v1 legacy-keys kunnen nog NL-fallback hebben wanneer v2 uit staat.

---

## Hoofdstuk 9 — Foutmarge en herkansing

- Onzekerheid match: zie `MatchUncertaintyPoints` (typisch 10 bij indicatieve diepte).
- Kandidaat mag scan verder zetten tot volledig; versie blijft op eerste toegewezen `QuickScanVersion`.
- Geen automatische “reset” van v1 naar v2 zonder expliciete productbeslissing.

---

## Hoofdstuk 10 — Privacy, consent, AI Act

- Test-AI consent verplicht (`TestConsentGate`).
- Transparantieblok v2: geen diagnose, menselijke oversight, match indicatief.
- Gegevens: antwoorden gekoppeld aan account; zie privacyverklaring.

---

## Hoofdstuk 11 — Technische referenties

- Versie: `AssessmentTestVersions`, migratie `QuickScanVersion`
- Scoring: `AssessmentScoringDocumentation`, `LikertAnswerJson`
- Match evidence: `AssessmentMatchEvidenceRules`
- Keuzes product: `docs/tests/GEMAAKTE_KEUZES_TESTS_V2.md`

---

## Contact / onderhoud

Producteigenaar Lobsy; psychometrische review: zie PR-documentatie `docs/psychometrie/` (aparte docs-PR). Wijzigingen v2 via `TestsV2Enabled` en itembank-updates met regressietests (`AssessmentV2TranslationGuardTests`, `AssessmentQuickScanV2Tests`).
