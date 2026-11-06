# Trial-20 model compare (gpt-4o vs gpt-4o-mini)

## Run (does not touch Acceptatie DB)

```bash
export OPENAI_API_KEY='…'   # same key as Acceptatie Integrations → OpenAI
./scripts/run-trial-20-cheap-models.sh
python3 scripts/write-trial-20-model-compare-report.py
```

Outputs:

| File | Purpose |
|------|---------|
| `occupation-day-in-life.nl.json` | gpt-4o baseline (DB export / Acc regen) — **unchanged by mini script** |
| `occupation-day-in-life.mini.nl.json` | gpt-4o-mini trial-20 only (in-memory generator) |
| `trial-20-cost-mini.json` | Actual token usage + USD for mini run |
| `trial-20-model-compare.json` | Merged cost + compare render paths |
| `renders/desktop/compare-zee-*.png` | Side-by-side screen 13 (Zee, 2×1440) |

Verify mini export:

```bash
OCCUPATION_DAY_EXPORT=docs/mockups/leerling/werelden/data/occupation-day-in-life.mini.nl.json \
  python3 scripts/verify-trial-timeline.py
OCCUPATION_DAY_EXPORT=docs/mockups/leerling/werelden/data/occupation-day-in-life.mini.nl.json \
  python3 scripts/verify-trial-quality.py
```

## Grok / xAI

Jobsy `IntegrationKey` has **OpenAI only** (no xAI tile on Acceptatie). Grok is **skipped** until product adds an integration.

## Cost projections (~3 039 occupations)

From `OccupationDayBatchPlan` (typical calls/tokens per occupation, list prices in `OccupationDayBatchPlan.cs`):

| Model | Projected USD (catalog) | Trial 20 (same formula) |
|-------|-------------------------|-------------------------|
| gpt-4o | ~132.43 | ~0.87 |
| gpt-4o-mini | ~7.95 | ~0.05 |

Actual trial-20 USD comes from `trial-20-cost-mini.json` after a real OpenAI run.

## Quality (honest expectations)

After you have both exports, compare the same `escoId` rows:

- **B1 / readability**: mini often uses shorter sentences; watch for abstract words the quality script flags.
- **Timeline**: same five blocks (`start` → `close`); validation should match gpt-4o.
- **Facts only**: both use the same writer + repair pipeline; mini may omit niche detail.
- **Skill chips**: same count rules; mini labels can be vaguer (“samenwerken”) vs specific gpt-4o chips.

Side-by-side PNGs: bioloog, accumonteur, advertentiemanager, docent-alfabetisering (Zee).
