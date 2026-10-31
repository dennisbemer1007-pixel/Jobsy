#!/usr/bin/env python3
"""Merge cost JSON files into trial-20-model-compare.json."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "docs/mockups/leerling/werelden/data"
OUT = DATA / "trial-20-model-compare.json"


def load(name):
    p = DATA / name
    return json.loads(p.read_text(encoding="utf-8")) if p.exists() else None


def main():
    base = load("trial-20-model-compare.json") or {}
    g4o_cost = load("trial-20-cost.json") or {}
    mini = load("trial-20-cost-mini.json")

    out = {
        "catalogOccupations": base.get("catalogOccupations") or 3039,
        "pricingNote": base.get("pricingNote"),
        "gpt4o": dict(base.get("gpt4o") or {}),
        "gpt4oMini": dict(base.get("gpt4oMini") or {}),
        "grok": base.get("grok")
        or {
            "status": "skipped",
            "reason": "No xAI/Grok IntegrationKey or Acceptatie integrations tile.",
        },
        "compareSlugs": base.get("compareSlugs")
        or ["bioloog", "accumonteur", "advertentiemanager", "docent-alfabetisering"],
        "compareRenders": base.get("compareRenders") or [],
        "compareRendersStatus": base.get("compareRendersStatus"),
        "qualityAssessment": base.get("qualityAssessment") or {},
    }

    actual = (g4o_cost.get("actualOpenAiUsageThisRun") or {}).get("usd")
    if actual is not None:
        out["gpt4o"]["Trial20ActualUsd"] = actual
    est = g4o_cost.get("estimatedUsdIfFreshGpt4oRegenAll20") or {}
    if est.get("estimatedUsd") is not None:
        out["gpt4o"]["Trial20EstimatedUsd"] = est["estimatedUsd"]

    if mini:
        out["gpt4oMini"]["Trial20ActualUsd"] = mini.get("costUsd")
        out["gpt4oMini"]["Trial20ActualNote"] = (
            f"model={mini.get('model')} generated={mini.get('generated')} failed={mini.get('failed')}"
        )
        out["gpt4oMini"]["EstimatedUsdCatalog3039"] = mini.get("projectedCatalog3039Usd")
        out["compareRendersStatus"] = "generated"
        if not out["qualityAssessment"].get("status") or out["qualityAssessment"]["status"] == "pending mini export":
            out["qualityAssessment"] = {
                "status": "review after mini run",
                "note": "Re-read sample slugs in compare PNGs and run verify-trial-quality.py on mini export.",
            }

    OUT.write_text(json.dumps(out, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(out, indent=2))


if __name__ == "__main__":
    main()
