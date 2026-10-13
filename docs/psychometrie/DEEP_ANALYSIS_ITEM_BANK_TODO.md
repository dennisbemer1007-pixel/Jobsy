# Betaalde diepteanalyse — itembank status

**Status (oktober 2026):** itembanks **150 / 200 / 150 / 150** geïmplementeerd in `DeepAnalysisCatalog` + embedded NL + `Data/DeepItems/*-{en,pl,ro,ar}.json`.  
**Normen / α:** nog te bepalen met pilotdata (**n≈300** per instrument) — zie `VALIDATIE_STUDIEPLAN.md`.

## Instrumenten

| Instrument | Items | Construct | Constructmatrix |
|------------|-------|-----------|-----------------|
| Competentie (Big Five / facetten) | 150 | IPIP-NEO-achtig, werkcontext B1 | `COMPETENCE_DEEP_CONSTRUCT_MATRIX.md` |
| Carrière RIASEC | 200 | Holland R–I–A–S–E–C | `RIASEC_DEEP_CONSTRUCT_MATRIX.md` |
| Waarden | 150 | Schwartz workplace drivers | `VALUES_DEEP_CONSTRUCT_MATRIX.md` |
| Cultuur & persoonlijkheid | 150 | 6 cultuur + 6 persoonlijkheid | `CULTURE_DEEP_CONSTRUCT_MATRIX.md` |

## Kwaliteitscontroles (CI)

- `DeepAnalysisItemBankQualityTests` — unieke stems, reverse-balans per domein, near-duplicate vs free Quick-Scan v3, taal-heuristieken.
- `TestItemTranslationParityTests` — volledige id-dekking en geen NL-fallback in shipped talen.
- Regeneratie vertalingen: `docs/psychometrie/scripts/regenerate_deep_item_locales.py` + `validate_deep_locales.py`.

## Product

- Free tier v3: 25 items (`QuickScanFreeTierV3`); **anker op v2-id’s**, geen overlap met deep-stems (score-vergelijkbaarheid via domeinen).
- Paywall / flow: `DeepAnalysis.razor`, `TestDepthLevel.Bottom`, autosave + pauze per `TestDepthRules.Parts`.
- Feature flag: `TestsV2Enabled` (Quick-Scan v3); deep catalog ongewijzigd qua versioning t.o.v. Quick-Scan.

## Open (post-pilot)

- [ ] Native review vertalingen (Argos-batch → menselijke B1-pass).
- [ ] Factorstructuur / item-trash analyse op pilotdata.
- [ ] Module-merge architectuur (Persoon+Werkstijl) — **geen** nieuwe items zonder nieuwe constructmatrix.
