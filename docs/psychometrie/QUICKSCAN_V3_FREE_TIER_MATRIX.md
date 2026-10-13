# Quick-Scan v3 — free tier (25 items per instrument)

**Status:** geïmplementeerd achter `TestsV2Enabled` (`QuickScanVersion = 3`).  
**Bron-itembank:** v2 (`*CatalogV2`); v3 is een **vaste subset** (geen nieuwe stems).  
**Legacy:** `QuickScanVersion = 2` behoudt volledige v2-sets (40/48/48/40).

## Tier-model (per test)

| Laag | Code | Items | Doel |
|------|------|-------|------|
| Onboarding | `TestDepthLevel.First` (5) + wizard-mini | 5 per module in wizard | Indicatief |
| Gratis verdieping | `TestDepthLevel.Full` @ v3 | **25** | Volledige free Quick-Scan |
| Betaalde diepte | `TestDepthLevel.Bottom` | 150–200 (`DeepAnalysisCatalog`) | Paywall + rapport; `*_DEEP_CONSTRUCT_MATRIX.md` |

## Geselecteerde vraag-id’s (v2 bank)

### Competentie (5×5, 3F+2R per schaal)

Ids: `1–5, 9–13, 17–21, 25–29, 33–37` — zie `QuickScanFreeTierV3.CompetenceQuestionIds`.

### RIASEC (4 per type + 1 Artistic)

Ids: `1–4, 9–12, 17–21, 25–28, 33–36, 41–44` — zie `QuickScanFreeTierV3.CareerQuestionIds`.

### Cultuur & persoonlijkheid (2 per dimensie + 1 extra Curiosity)

Ids: `1–12, 25–26, 29–30, 33–34, 37–38, 41–42, 45–47` — zie `QuickScanFreeTierV3.CultureQuestionIds`.  
Onboarding mini/dieper @ v3: `1,3,5,7,9` + `2,4,6,8,10` (alleen ids uit deze set).

### Waarden (5×5, 3F+2R per driver)

Ids: `1–5, 9–13, 17–21, 25–29, 33–37` — zie `QuickScanFreeTierV3.ValuesQuestionIds`.

Constructdekking en reverse-balans: geasserteerd in `QuickScanFreeTierV3Tests`.

## Relatie constructmatrices v2

Volledige v2-mappings blijven leidend in:

- `COMPETENCE_V2_CONSTRUCT_MATRIX.md`
- `RIASEC_V2_CONSTRUCT_MATRIX.md`
- `CULTURE_V2_CONSTRUCT_MATRIX.md`
- `VALUES_V2_CONSTRUCT_MATRIX.md`

v3-rijen zijn de subset-id’s hierboven; geen duplicaten binnen een instrument.
