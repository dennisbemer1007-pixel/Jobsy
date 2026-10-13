# Betaalde diepteanalyse — itembank TODO (150–200 items)

**Doel:** paywall-hook en UI-structuur (`TestDepthLevel.Bottom`, `DeepAnalysisCatalog`) zonder **verzonnen** psychometrische items.

## Huidige code (ongewijzigde counts)

| Instrument | Doel-range (tier) | Huidige `DeepAnalysisCatalog` | Status |
|------------|-------------------|----------------------------------|--------|
| Competentie | 150–200 | 150 (5×30 domeinen) | Bank aanwezig; valideren tegen IPIP/workplace-claim |
| Carrière (RIASEC) | 150–200 | 200 | Bank aanwezig |
| Cultuur | 150–200 | 150 | Bank aanwezig |
| Waarden | 150–200 | 150 | Bank aanwezig |

## TODO (geen nieuwe items zonder onderbouwing)

1. **Psychometrische review** per bank: α, factorstructuur, item-trash (zie `VALIDATIE_STUDIEPLAN.md`).
2. **Tier-copy:** marketing “150–200” alignen met werkelijke `QuestionCountFor(kind)` na review (eventueel splitsen 150 vs 200 per instrument).
3. **Module-merge (v3 architectuur):** gecombineerde Persoon+Werkstijl-diepte — **nieuwe items alleen** na constructmatrix + COTAN-intern review.
4. **Ontbrekende stems:** als een schaal onder minimum items zit na trim, expliciet **TODO** in constructmatrix + geen productie-normclaim.

## Productgedrag (nu)

- Free tier v3: max 25 items (`QuickScanFreeTierV3`).
- Bottom-tier: bestaande deep flow; betaling/consent zoals `DeepAnalysis.razor`.
- Match-evidence: `AssessmentMatchEvidence.DeepAnalysis` na voltooide deep set.
