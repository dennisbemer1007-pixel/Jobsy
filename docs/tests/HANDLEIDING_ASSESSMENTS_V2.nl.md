# Handleiding Quick-Scan assessments (v2)

**Status:** concept handleiding voor productie-ondersteuning. Geen COTAN-registratie; geen normtabellen tot de validatiestudie is afgerond.

**Feature flag:** `TestsV2Enabled` (platform). Standaard uit. Nieuwe kandidaten krijgen v2 alleen als de flag aan staat; bestaande v1-resultaten blijven op `QuickScanVersion = 1`.

## Doel en doelgroep

| Instrument | Construct (indicatief) | Doelgroep |
|------------|------------------------|-----------|
| Competentie Quick-Scan v2 | Samenwerken, resultaatgerichtheid, stressbestendigheid, innovatie (+ extraversie in rapport) | Werkzoekenden 16+, NL/EN/PL/RO/AR |
| Carrière (RIASEC) v2 | Realistic, Investigative, Artistic, Social, Enterprising, Conventional | Idem |
| Cultuur & persoonlijkheid v2 | Werkcultuur (6) + Big Five korte schalen (Openness i.p.v. losse “Curiosity”) | Idem |
| Waarden (Schwartz-achtig) v2 | Autonomie, verbinding, prestatie, stabiliteit, impact | Idem |

Gebruik: **oriëntatie en matching-hulp**, geen selectiebesluit zonder menselijke beoordeling (AI Act: menselijke oversight).

## Itemsets en diepte

- **v1:** 18–25 items per instrument (historisch); scores blijven op v1-formules.
- **v2:** uitgebreidere sets (competentie 40, carrière 48, cultuur 24, waarden 40) met B1-itemteksten en volledige vertalingen nl/en/pl/ro/ar (`AssessmentV2.*` keys).
- **Diepteniveaus:** Eerste blik / Dieper / Volledig — korte vormen zijn **indicatief** (`IsIndicativeDepth`); match-% krijgt lagere zekerheid (zie `AssessmentMatchEvidence`).

## Administratie

1. Toestemming test + AI (`TestAiConsent`, versie `PrivacyConstants.CandidateProfilingConsentVersion`).
2. Kandidaat kiest diepte; antwoorden 1–5 Likert, autosave.
3. `QuickScanVersion` wordt gezet bij eerste save (beleid: `QuickScanVersionPolicy`).

## Scoring (transparant)

- Likert: `AssessmentScoringDocumentation.LikertRange`
- Reverse items: `AssessmentScoringDocumentation.ReverseFormula` (6 − ruwe score)
- Schaalpercent: `AssessmentScoringDocumentation.PercentFormula`
- Implementatie per catalogus: `CompetencyTestCatalogV2`, `CareerTestCatalogV2`, `CulturePersonalityCatalogV2`, `SchwartzValuesCatalogV2` via `AssessmentQuickScanCatalog`.

Match-percentage: gewogen overlap met vacature-eisen; bij indicatieve diepte: **uncertainty band** en transparency string `AssessmentV2.Transparency.MatchIndicative`.

## Interpretatie

- Percentages 0–100: relatieve sterkte binnen het instrument, **geen normvergelijking** tot normstudie.
- RIASEC: Holland-code uit top-3; culture/values: top-dimensies voor gesprek met recruiter.
- Geen medische/psychologische diagnose.

## Beperkingen

- Geen gepubliceerde normen of COTAN-beoordeling.
- v2-itemvalidatie (factorstructuur, α) vereist echte responsdata — zie `docs/psychometrie/VALIDATIE_STUDIEPLAN.md` (aparte docs-PR).
- Mini-wizard en onboarding-mini blijven korte proxies; volledige Quick-Scan aanbevolen voor stabieler beeld.

## Normen (plan, geen data)

Fase 1: N ≥ 300 voltooide v2-profielen per instrument, stratificatie leeftijd/geslacht/taal.  
Fase 2: percentielen en betrouwbaarheidsbanden; alleen publiceren na interne review.  
Tot die tijd: UI toont ruwe percentages + indicatieve match, geen “u scoort boven gemiddeld”-claims.

## Technische referenties

- Versie-enum: `AssessmentTestVersions`
- Migratie: `20261013120000_AddQuickScanVersionAndTestsV2Enabled`
- Documentatie in code: `AssessmentScoringDocumentation`, `AssessmentMatchEvidenceRules`
