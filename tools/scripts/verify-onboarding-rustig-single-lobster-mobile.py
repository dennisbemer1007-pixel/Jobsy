#!/usr/bin/env python3
"""Each mobile JPEG: one small sprite at placement; no painted-in lobster mascot."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
PAN = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama"
REPORT_SLUGS = ["start", "step-01", "step-02", "step-03", "step-04", "step-05"]
ALL_SLUGS = REPORT_SLUGS + [
    "step-06",
    "step-07",
    "step-08",
    "step-09",
    "step-10",
    "step-11",
    "kompas",
]
MOB_CENTER: dict[str, tuple[float, float]] = {
    "start": (0.26, 0.55),
    "step-01": (0.24, 0.60),
    "step-02": (0.48, 0.55),
    "step-03": (0.16, 0.52),
    "step-04": (0.22, 0.54),
    "step-05": (0.32, 0.50),
    "step-06": (0.68, 0.55),
    "step-07": (0.35, 0.57),
    "step-08": (0.20, 0.58),
    "step-09": (0.70, 0.54),
    "step-10": (0.24, 0.55),
    "step-11": (0.62, 0.54),
    "kompas": (0.50, 0.57),
}
SPRITE_MAX_H_JPEG = 36
SPRITE_PICK_DIST = 38


def red_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (r > 110) & (r > g + 18) & (r > b + 18) & (g < 145) & (b < 145)


def components(mask: np.ndarray) -> list[tuple[float, float, int, int, int]]:
    h, w = mask.shape
    vis = np.zeros_like(mask, dtype=bool)
    out: list[tuple[float, float, int, int, int]] = []
    for y in range(h):
        for x in range(w):
            if not mask[y, x] or vis[y, x]:
                continue
            stack = [(y, x)]
            ys, xs = [], []
            while stack:
                cy, cx = stack.pop()
                if cy < 0 or cy >= h or cx < 0 or cx >= w or vis[cy, cx] or not mask[cy, cx]:
                    continue
                vis[cy, cx] = True
                ys.append(cy)
                xs.append(cx)
                stack.extend([(cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)])
            area = len(ys)
            if area < 45:
                continue
            y0, y1 = min(ys), max(ys)
            x0, x1 = min(xs), max(xs)
            bh, bw = y1 - y0 + 1, x1 - x0 + 1
            cxm, cym = (x0 + x1) / 2, (y0 + y1) / 2
            out.append((cxm, cym, area, bh, bw))
    return out


def painted_lobster(c: tuple[float, float, int, int, int], img_h: int) -> bool:
    _, cym, area, bh, bw = c
    if cym > img_h * 0.58:
        return False
    if bh < 50 or bh > 86:
        return False
    if bw < 44 or bw > 92:
        return False
    if area < 520 or area > 1800:
        return False
    ratio = bw / max(1, bh)
    return 0.76 <= ratio <= 1.32


def verify_slug(slug: str) -> tuple[bool, str]:
    path = PAN / f"{slug}-mobile.jpg"
    band = np.asarray(Image.open(path).convert("RGB"), dtype=np.float32)
    w, h = band.shape[1], band.shape[0]
    ex, ey = MOB_CENTER[slug]
    tcx, tcy = ex * w, ey * h
    comps = components(red_mask(band))

    def dist(cxm: float, cym: float) -> float:
        return ((cxm - tcx) ** 2 + (cym - tcy) ** 2) ** 0.5

    painted = [c for c in comps if painted_lobster(c, h)]
    if painted:
        return False, f"painted-in lobster-like shape (n={len(painted)})"

    small = [c for c in comps if c[3] <= SPRITE_MAX_H_JPEG]
    if not small:
        return False, "no small red blob"
    best = min(small, key=lambda c: dist(c[0], c[1]))
    if dist(best[0], best[1]) > SPRITE_PICK_DIST:
        return False, f"sprite too far from placement (d={dist(best[0], best[1]):.0f}px)"
    extra = [c for c in comps if c is not best and painted_lobster(c, h)]
    if extra:
        return False, f"extra painted lobster candidate (n={len(extra)})"
    bh = best[3]
    return True, f"1 sprite h={bh}px JPEG (~{bh // 2}px @390)"


def main() -> None:
    slugs = REPORT_SLUGS if "--report-only" in sys.argv else ALL_SLUGS
    failures: list[str] = []
    for slug in slugs:
        ok, msg = verify_slug(slug)
        if ok:
            print(f"OK {slug}: {msg}")
        else:
            failures.append(f"{slug}: {msg}")
    if failures:
        print("FAILURES:")
        for f in failures:
            print(" -", f)
        raise SystemExit(1)
    print("All mobile JPEGs: single small Lobsy sprite.")


if __name__ == "__main__":
    main()
