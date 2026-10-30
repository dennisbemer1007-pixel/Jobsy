#!/usr/bin/env python3
"""Write trial-20-cost.json from catalog estimates + Acceptatie export coverage."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "docs/mockups/leerling/werelden/data"

# Mirror OccupationDayBatchPlan (gpt-4o list prices, March 2026 reference).
TYPICAL_CALLS_PER_OCC = 1.25 + 4  # nl + en/pl/ro/ar
TYPICAL_INPUT = 1400
TYPICAL_OUTPUT = 480
INPUT_USD_PER_M = 2.50
OUTPUT_USD_PER_M = 10.00


def estimate_usd(occupations: int) -> dict:
    calls = int(occupations * TYPICAL_CALLS_PER_OCC + 0.999)
    inp = calls * TYPICAL_INPUT
    out = calls * TYPICAL_OUTPUT
    usd = inp / 1_000_000 * INPUT_USD_PER_M + out / 1_000_000 * OUTPUT_USD_PER_M
    return {
        "model": "gpt-4o",
        "occupations": occupations,
        "openAiCalls": calls,
        "promptTokens": inp,
        "completionTokens": out,
        "estimatedUsd": round(usd, 4),
        "note": "Token counts are plan averages (OccupationDayBatchPlan); Acceptatie does not return usage headers.",
    }


def main() -> None:
    manifest = json.loads((DATA / "trial-20-manifest.json").read_text(encoding="utf-8"))
    export = json.loads((DATA / "occupation-day-in-life.nl.json").read_text(encoding="utf-8"))
    ids = {o["escoId"].lower() for o in manifest["occupations"]}
    rows = [r for r in export.get("rows") or [] if r["escoId"].lower() in ids]
    gpt4o = [r for r in rows if (r.get("sourceModel") or "").startswith("gpt-4o")]
    blocked = [o for o in manifest["occupations"] if o["escoId"].lower() not in {r["escoId"].lower() for r in rows}]
    est = estimate_usd(len(gpt4o))
    payload = {
        "trialOccupations": 20,
        "generatedOnAcceptatieGpt4o": len(gpt4o),
        "blockedEscoIds": [o["escoId"] for o in blocked],
        "blockedTitlesNl": [o["titleNl"] for o in blocked],
        "blockedReason": "OpenAI output failed highlights validation (fixed in OccupationDayDraftRepair on this branch).",
        "costForGeneratedRows": est,
        "costIfAll20Succeeded": estimate_usd(20),
    }
    (DATA / "trial-20-cost.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    print(json.dumps(payload, indent=2))


if __name__ == "__main__":
    main()
