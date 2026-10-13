# Gemaakte keuzes — Tests V2 (COTAN-alignment)

## Versiebeleid

- **Aparte versie `QuickScanVersion = 2`** op alle vier kandidaat-assessmenttabellen; v1 blijft default voor bestaande rijen.
- Nieuwe assessments → v2 **alleen** als `TestsV2Enabled` aan staat (`QuickScanVersionPolicy`).
- Scoring en itembanks via **`AssessmentQuickScanCatalog`**; geen stille herberekening van v1-opgeslagen percentages.

## Items & taal

- v2 itembanks in dedicated catalogi (40/48/24/40 items) met **B1 NL** en parallelle keys voor en/pl/ro/ar.
- Reverse items expliciet in catalogus; scoring documenteert 6 − x.
- Culture v2: **Openness** i.p.v. aparte “Curiosity”-schaal in scoring.
- v1 typo waarden Q14 (“overtreff”) gecorrigeerd zonder versiewijziging.

## Diepte & match

- `TestDepthRules` versie-aware; korte vormen blijven maar **`IsIndicativeDepth`** + lagere match-evidence (`AssessmentMatchEvidence.Indicative`, uncertainty +10 pp).
- Geen sterke match-claims bij mini/deeper zonder volledige scan.

## Transparantie

- UI: `TestConsentGate` + `data-testid="assessment-v2-transparency"` bij v2.
- Consent-scherm vermeldt geen diagnose + menselijke oversight (AI Act).
- Strings: `AssessmentV2.Transparency.*` (5 talen).

## Feature flag & rollout

- `PlatformFeature.TestsV2` / admin toggle; default **uit**.
- Geen merge naar productie-main via agent; acceptatie-PR voor review.

## Wat bewust niet is gedaan

- **Geen COTAN-claim** en geen normtabellen of validity-cijfers in product.
- Geen verplichte migratie v1 → v2 (kandidaat kan op v1 blijven tot nieuwe scan met flag).
- Deep analysis / betaalde rapporten ongewijzigd qua versioning (Quick-Scan v2 scope).

## Documentatie & tests

- Handleiding: `docs/tests/HANDLEIDING_ASSESSMENTS_V2.nl.md`
- Psychometrische review: aparte PR `docs/psychometrie/*`
- Automatische tests: `AssessmentQuickScanV2Tests`, `TestsV2FeatureFlagTests`, Playwright `TestsV2PlaywrightTests`
