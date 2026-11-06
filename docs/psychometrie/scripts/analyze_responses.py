#!/usr/bin/env python3
"""
Psychometric analysis for Lobsy assessment CSV exports (long format).

CSV columns:
  respondent_hash, language, gender, age_band, test_key, item_id, response_raw, status, completed_at

Also supports --synthetic for indicative demo statistics (NOT empirical validation).
"""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parents[3]
INVENTORY = ROOT / "docs/psychometrie/data/inventory.json"
ONBOARDING_MINI = {
    "competence_quickscan": [1, 6, 11, 16, 21],
    "career_quickscan": [1, 6, 10, 14, 18, 22],
    "culture_quickscan": [1, 3, 5, 7, 11],
    "values_quickscan": [1, 6, 11, 16, 21],
}


def load_scale_map(test_key: str) -> dict[int, tuple[str, bool]]:
    inv = json.loads(INVENTORY.read_text(encoding="utf-8"))
    key_map = {
        "competence_quickscan": "competence_quickscan",
        "career_quickscan": "career_quickscan",
        "culture_quickscan": "culture_quickscan",
        "values_quickscan": "values_quickscan",
    }
    section = inv["tests"][key_map[test_key]]
    return {it["id"]: (it["scale"], it["reverse"]) for it in section["items"]}


def score_item(raw: int, reverse: bool) -> int:
    if reverse:
        return 6 - raw
    return raw


def cronbach_alpha_variance(matrix: np.ndarray) -> float | None:
    """Standard formula (COTAN: internal consistency). Items must be reverse-keyed first."""
    k = matrix.shape[1]
    if k < 2 or matrix.shape[0] < 2:
        return None
    variances = matrix.var(axis=0, ddof=1)
    total = matrix.sum(axis=1)
    var_total = total.var(ddof=1)
    if var_total <= 0:
        return None
    return float(k / (k - 1) * (1 - variances.sum() / var_total))


def cronbach_alpha_from_correlation(matrix: np.ndarray) -> float | None:
    """Spearman-Brown from average inter-item correlation (second method for verification)."""
    if matrix.shape[0] < 2 or matrix.shape[1] < 2:
        return None
    corr = np.corrcoef(matrix, rowvar=False)
    if corr.shape[0] < 2:
        return None
    upper = corr[np.triu_indices(corr.shape[0], k=1)]
    if upper.size == 0:
        return None
    r_bar = float(np.nanmean(upper))
    k = matrix.shape[1]
    denom = 1 + (k - 1) * r_bar
    if abs(denom) < 1e-12:
        return None
    return float(k * r_bar / denom)


def cronbach_alpha(matrix: np.ndarray) -> dict[str, float | None]:
    a1 = cronbach_alpha_variance(matrix)
    a2 = cronbach_alpha_from_correlation(matrix)
    return {
        "alpha_variance_formula": a1,
        "alpha_avg_interitem_correlation": a2,
        "methods_agree_within_0.01": (
            None
            if a1 is None or a2 is None
            else abs(a1 - a2) <= 0.01
        ),
    }


def mcdonald_omega(matrix: np.ndarray) -> float | None:
    """One-factor omega via PCA on correlation matrix (approximation)."""
    if matrix.shape[0] < 3 or matrix.shape[1] < 2:
        return None
    corr = np.corrcoef(matrix, rowvar=False)
    if np.any(np.isnan(corr)):
        return None
    eigvals, eigvecs = np.linalg.eigh(corr)
    idx = int(np.argmax(eigvals))
    loadings = eigvecs[:, idx] * math.sqrt(max(eigvals[idx], 0))
    sum_load = loadings.sum()
    err = corr.shape[0] - sum_load**2
    denom = sum_load**2 + err
    if denom <= 0:
        return None
    return float(sum_load**2 / denom)


def item_rest_correlations(matrix: np.ndarray) -> list[float]:
    out = []
    for j in range(matrix.shape[1]):
        rest = np.delete(matrix, j, axis=1)
        if rest.shape[1] == 0:
            out.append(float("nan"))
            continue
        total_rest = rest.sum(axis=1)
        col = matrix[:, j]
        if col.std() == 0 or total_rest.std() == 0:
            out.append(float("nan"))
        else:
            out.append(float(np.corrcoef(col, total_rest)[0, 1]))
    return out


def scale_percent(row: pd.Series, item_ids: list[int], reverse_map: dict[int, bool]) -> float:
    vals = []
    for iid in item_ids:
        v = row.get(iid)
        if pd.isna(v):
            continue
        vals.append(score_item(int(v), reverse_map[iid]))
    if not vals:
        return float("nan")
    avg = sum(vals) / len(vals)
    return (avg - 1) / 4 * 100


def analyze_test(df: pd.DataFrame, test_key: str) -> dict:
    meta = load_scale_map(test_key)
    sub = df[df["test_key"] == test_key].copy()
    if sub.empty:
        return {"n_respondents": 0, "note": "no data"}

    sub = sub[sub["status"].str.lower() == "completed"] if "status" in sub.columns else sub
    pivot = sub.pivot_table(
        index="respondent_hash", columns="item_id", values="response_raw", aggfunc="first"
    )
    n = pivot.shape[0]
    reverse_map = {i: meta[i][1] for i in meta if i in pivot.columns}
    keyed = pivot.copy()
    for iid in keyed.columns:
        if int(iid) in reverse_map and reverse_map[int(iid)]:
            keyed[iid] = 6 - keyed[iid]

    scales: dict[str, list[int]] = {}
    for iid, (scale, _) in meta.items():
        scales.setdefault(scale, []).append(iid)

    scale_stats = {}
    scale_scores = {}
    for scale, ids in scales.items():
        cols = [c for c in pivot.columns if int(c) in ids]
        if len(cols) < 2:
            continue
        mat = keyed[cols].dropna(how="any").to_numpy(dtype=float)
        if mat.shape[0] < 2:
            continue
        irr = item_rest_correlations(mat)
        percents = keyed[cols].mean(axis=1) / 5 * 100  # rough; proper uses ToPercent on subset
        # Proper percent per person
        rev = {int(i): meta[int(i)][1] for i in cols}
        pct_list = []
        for _, row in pivot[cols].iterrows():
            vals = []
            for c in cols:
                if pd.isna(row[c]):
                    continue
                vals.append(score_item(int(row[c]), rev[int(c)]))
            if vals:
                avg = sum(vals) / len(vals)
                pct_list.append((avg - 1) / 4 * 100)
        scale_scores[scale] = np.array(pct_list)
        alpha = cronbach_alpha(mat)
        scale_stats[scale] = {
            "n_complete": int(mat.shape[0]),
            "k_items": len(cols),
            "reverse_key_applied_before_alpha": True,
            "cronbach_alpha": alpha,
            "mcdonald_omega": mcdonald_omega(mat),
            "item_rest_min": float(np.nanmin(irr)),
            "item_rest_mean": float(np.nanmean(irr)),
            "mean_percent": float(np.mean(pct_list)) if pct_list else None,
            "sd_percent": float(np.std(pct_list, ddof=1)) if len(pct_list) > 1 else None,
            "pct_at_ceiling_100": float(np.mean(np.array(pct_list) >= 95)) if pct_list else None,
        }

    # Inter-scale correlation
    inter = {}
    names = list(scale_scores.keys())
    for i, a in enumerate(names):
        for b in names[i + 1 :]:
            sa, sb = scale_scores[a], scale_scores[b]
            m = min(len(sa), len(sb))
            if m < 3:
                continue
            inter[f"{a}|{b}"] = float(np.corrcoef(sa[:m], sb[:m])[0, 1])

    # Mini vs full agreement
    mini_ids = ONBOARDING_MINI.get(test_key, [])
    mini_vs_full = None
    if mini_ids and len(mini_ids) >= 1:
        rev_all = {int(i): meta[int(i)][1] for i in meta}
        full_pct = {}
        mini_pct = {}
        for resp, row in pivot.iterrows():
            full_vals, mini_vals = [], []
            for iid in meta:
                if iid not in row or pd.isna(row[iid]):
                    continue
                sc = score_item(int(row[iid]), rev_all[iid])
                full_vals.append((meta[iid][0], sc))
            for iid in mini_ids:
                if iid not in row or pd.isna(row[iid]):
                    continue
                mini_vals.append((meta[iid][0], score_item(int(row[iid]), rev_all[iid])))
            if not full_vals or not mini_vals:
                continue
            for scale in {s for s, _ in full_vals}:
                f = [v for s, v in full_vals if s == scale]
                m = [v for s, v in mini_vals if s == scale]
                if f and m:
                    full_pct.setdefault(scale, []).append((sum(f) / len(f) - 1) / 4 * 100)
                    mini_pct.setdefault(scale, []).append((m[0] - 1) / 4 * 100)
        per_scale = {}
        for scale in full_pct:
            if scale in mini_pct and len(full_pct[scale]) >= 3:
                a = np.array(full_pct[scale])
                b = np.array(mini_pct[scale][: len(a)])
                per_scale[scale] = {
                    "pearson_r": float(np.corrcoef(a, b)[0, 1]),
                    "mean_abs_diff": float(np.mean(np.abs(a - b))),
                    "n": int(len(a)),
                }
        mini_vs_full = per_scale

    return {
        "n_respondents": int(n),
        "scales": scale_stats,
        "inter_scale_correlations": inter,
        "mini_vs_full": mini_vs_full,
    }


def generate_synthetic(n: int, seed: int = 42) -> pd.DataFrame:
    rng = np.random.default_rng(seed)
    rows = []
    tests = [
        ("competence_quickscan", 25),
        ("career_quickscan", 25),
        ("culture_quickscan", 18),
        ("values_quickscan", 25),
    ]
    for r in range(n):
        rh = f"synth_{r:05d}"
        lang = rng.choice(["nl", "en", "pl"], p=[0.7, 0.2, 0.1])
        for test_key, n_items in tests:
            # latent trait per scale drives responses
            meta = load_scale_map(test_key)
            scale_latent = {s: rng.normal(0, 1) for s in {meta[i][0] for i in meta}}
            for iid in range(1, n_items + 1):
                if iid not in meta:
                    continue
                scale, rev = meta[iid]
                z = scale_latent[scale] + rng.normal(0, 0.35)
                # Higher latent → agreement with construct; reverse items get inverted raw
                z_eff = -z if rev else z
                prob = 1 / (1 + np.exp(-z_eff))
                raw = int(np.clip(np.round(prob * 4 + 1), 1, 5))
                rows.append(
                    {
                        "respondent_hash": rh,
                        "language": lang,
                        "gender": "unknown",
                        "age_band": rng.choice(["18-24", "25-34", "35-44", "45-54"]),
                        "test_key": test_key,
                        "item_id": iid,
                        "response_raw": raw,
                        "status": "Completed",
                        "completed_at": "2026-01-01",
                    }
                )
    return pd.DataFrame(rows)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--csv", type=Path, help="Long-format CSV export")
    ap.add_argument("--synthetic", action="store_true")
    ap.add_argument("--n", type=int, default=500, help="Synthetic N")
    ap.add_argument("--out", type=Path, required=True)
    args = ap.parse_args()

    if not INVENTORY.exists():
        raise SystemExit(f"Run extract_inventory.py first; missing {INVENTORY}")

    if args.synthetic:
        df = generate_synthetic(args.n)
        data_source = f"synthetic_simulation_n={args.n}"
    else:
        if not args.csv or not args.csv.exists():
            raise SystemExit("--csv required unless --synthetic")
        df = pd.read_csv(args.csv)
        data_source = str(args.csv)

    results = {
        "evidence_class": "b_synthetic_indicative_only" if args.synthetic else "b_or_c_depends_on_csv",
        "data_source": data_source,
        "warning": (
            "INDICATIEF: synthetische data; alpha/correlaties zijn geen schatting van Lobsy-kwaliteit. "
            "Gebruik uitsluitend om de analyse-pipeline te controleren."
            if args.synthetic
            else "Empirische resultaten — controleer N, selectiebias en representativiteit vóór COTAN-claims."
        ),
        "minimum_n_guidance": {
            "reliability_alpha": 300,
            "factor_analysis": 500,
            "dif_by_language": 200,
            "criterion_validity_hire": 100,
        },
        "tests": {},
    }

    for test_key in ONBOARDING_MINI:
        results["tests"][test_key] = analyze_test(df, test_key)

    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(results, indent=2), encoding="utf-8")
    print(f"Wrote {args.out}")


if __name__ == "__main__":
    main()
