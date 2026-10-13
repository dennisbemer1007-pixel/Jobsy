# Psychometrie — Lobsy kandidaatstests

Deze map bevat een **read-only** COTAN-geïnspireerde review van de Lobsy-assessments (geen productcode). Het is **geen officiële COTAN-beoordeling**.

## Bestanden

| Bestand | Doel |
|---------|------|
| [COTAN_BEOORDELING_LOBSY_TESTS.md](./COTAN_BEOORDELING_LOBSY_TESTS.md) | Hoofdrapport (NL): COTAN 2010 + addenda, bewijslabels (a/b/c), conservatieve oordelen |
| [ITEMteksten_BEOORDELING.md](./ITEMteksten_BEOORDELING.md) | Alle 93 Quick-Scan items per taal (handmatige review) |
| [VALIDATIE_STUDIEPLAN.md](./VALIDATIE_STUDIEPLAN.md) | Studieplan met steekproefgroottes voor universiteit/COTAN-partner |
| [data/scoring_verification.json](./data/scoring_verification.json) | Reverse + ToPercent verificatie (a) |
| [scripts/extract_inventory.py](./scripts/extract_inventory.py) | Haalt schalen, items en vertalingen uit de repo |
| [scripts/analyze_responses.py](./scripts/analyze_responses.py) | Cronbach α, McDonald ω, item-rest, mini vs lang, DIF-hints |
| [scripts/export_anonymized_responses.sql](./scripts/export_anonymized_responses.sql) | SQL voor geanonimiseerde CSV-export (PostgreSQL) |
| [data/inventory.json](./data/inventory.json) | Machine-leesbare inventaris (gegenereerd) |
| [data/synthetic_demo_report.json](./data/synthetic_demo_report.json) | Indicatieve statistieken op synthetische data |

## Snel starten (Dennis / universiteit)

```bash
# 1. Inventaris verversen vanuit code
python3 docs/psychometrie/scripts/verify_scoring.py
python3 docs/psychometrie/scripts/review_item_wording.py
python3 docs/psychometrie/scripts/extract_inventory.py

# 2. Export uit acceptatie/prod-DB (alleen geanonimiseerd; geen PII)
psql "$DATABASE_URL" -f docs/psychometrie/scripts/export_anonymized_responses.sql -o responses.csv

# 3. Psychometrie op echte data (minimaal ~300 afgeronde respondenten per schaal aanbevolen)
python3 docs/psychometrie/scripts/analyze_responses.py --csv responses.csv --out report.json

# 4. Synthetische demo (geen DB nodig)
python3 docs/psychometrie/scripts/analyze_responses.py --synthetic --n 500 --out docs/psychometrie/data/synthetic_demo_report.json
```

Vereisten Python: `numpy`, `pandas`, `scipy` (zie `requirements-psychometrie.txt`).
