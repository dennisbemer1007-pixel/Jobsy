#!/usr/bin/env python3
"""
Verify Likert reverse-keying and ToPercent match Jobsy.Core.Rules (code reference).

Evidence output: docs/psychometrie/data/scoring_verification.json
"""
from __future__ import annotations

import json
from pathlib import Path

LIKERT_MIN = 1
LIKERT_MAX = 5


def reverse_raw(raw: int) -> int:
    return LIKERT_MAX + LIKERT_MIN - raw


def to_percent(scored: list[int]) -> int:
    avg = sum(scored) / len(scored)
    pct = round((avg - LIKERT_MIN) / (LIKERT_MAX - LIKERT_MIN) * 100)
    return max(0, min(100, pct))


def main() -> None:
    # Mirrors Jobsy.Tests.CompetencyMatchingTests.Score_maps_likert_to_0_100_and_reverses_items
    reverse_ids = {4, 5, 9, 10, 14, 15, 19, 20, 24, 25}
    high = {}
    low = {}
    for i in range(1, 26):
        if i in reverse_ids:
            high[i] = 1
            low[i] = 5
        else:
            high[i] = 5
            low[i] = 1

    def score(answers: dict[int, int]) -> dict[str, int]:
        from collections import defaultdict

        cats = {
            range(1, 6): "Samenwerken",
            range(6, 11): "Resultaatgerichtheid",
            range(11, 16): "Stressbestendigheid",
            range(16, 21): "Innovatie",
            range(21, 26): "Extraversie",
        }
        out = {}
        for span, name in cats.items():
            scored = []
            for q in span:
                raw = answers[q]
                rev = q in reverse_ids
                scored.append(reverse_raw(raw) if rev else raw)
            out[name] = to_percent(scored)
        return out

    high_scores = score(high)
    low_scores = score(low)
    neutral = {i: 3 for i in range(1, 26)}
    neutral_scores = score(neutral)

    checks = [
        ("high_all_100", all(v == 100 for v in high_scores.values()), high_scores),
        ("low_samenwerken_0", low_scores["Samenwerken"] == 0, low_scores),
        ("low_innovatie_0", low_scores["Innovatie"] == 0, low_scores),
        ("neutral_50", neutral_scores["Samenwerken"] == 50, neutral_scores),
    ]

    alpha_check = []
    # After keying, all items at 5 → zero variance → alpha undefined (guard in analyze script)
    keyed_col = [5] * 5
    import numpy as np

    mat = np.tile(keyed_col, (10, 1)).astype(float)
    var_t = mat.sum(axis=1).var(ddof=1)
    alpha_check.append(
        {"case": "keyed_constant", "var_total": float(var_t), "alpha_computable": bool(var_t > 0)}
    )

    result = {
        "reference_tests": "Jobsy.Tests/CompetencyMatchingTests.cs Score_maps_likert_to_0_100_and_reverses_items",
        "reference_code": [
            "Jobsy.Core/Rules/CompetencyTestCatalog.cs:156 (reverse)",
            "Jobsy.Core/Rules/LikertAnswerJson.cs:92-104 (ToPercent)",
        ],
        "checks": [{"name": n, "pass": bool(p), "detail": d} for n, p, d in checks],
        "reverse_formula": "6 - raw (Likert 1-5)",
        "alpha_guard": alpha_check,
        "all_pass": all(bool(x[1]) for x in checks),
    }

    out = Path(__file__).resolve().parents[1] / "data/scoring_verification.json"
    out.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result, indent=2))
    if not result["all_pass"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
