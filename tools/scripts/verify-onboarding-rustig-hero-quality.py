#!/usr/bin/env python3
"""Verify hero band: full figures, tiny lobster, match approved framing heuristics."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

CREAM = np.array([247, 244, 240], dtype=np.float32)
ART = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
SIDEBAR = 300
SLUGS = ["start", "step-01", "step-02", "step-03", "step-04", "step-05"]
APPROVED_REF = Path(__file__).resolve().parents[2] / "artifacts/onboarding-rustig-approved-u5f"


def hero_box(width: int) -> tuple[int, int, int, int]:
    h = 170 if width >= 1024 else 130
    y = 100 if width >= 1024 else 118
    return (SIDEBAR, y, width, y + h)


def red_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (r > 150) & (g < 115) & (b < 115) & (r - g > 35)


def figure_top_bottom(band: Image.Image) -> tuple[int | None, int | None]:
    rgb = np.asarray(band.convert("RGB"), dtype=np.float32)
    dist = np.linalg.norm(rgb - CREAM, axis=2)
    h, w = dist.shape
    row_occ = (dist > 28).mean(axis=1)
    thr = max(0.04, float(row_occ.max()) * 0.15)
    active = np.where(row_occ >= thr)[0]
    if active.size == 0:
        return None, None
    return int(active.min()), int(active.max())


def lobster_blob(band: Image.Image) -> tuple[int, int, int, int] | None:
    rgb = np.asarray(band.convert("RGB"), dtype=np.float32)
    mask = red_mask(rgb)
    if not mask.any():
        return None
    ys, xs = np.where(mask)
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def check(path: Path, width: int, ref_band: Image.Image | None) -> list[str]:
    errs: list[str] = []
    im = Image.open(path)
    band = im.crop(hero_box(width))
    h, w = band.size[1], band.size[0]
    y_top, y_bot = figure_top_bottom(band)
    if y_top is not None and y_top < 6:
        errs.append(f"figure top too high y={y_top} (<6px headroom)")
    if y_bot is not None and y_bot > h - 4:
        errs.append(f"figure bottom clipped y={y_bot} band_h={h}")
    lb = lobster_blob(band)
    if lb is None:
        errs.append("no red lobster detected in band")
    else:
        _x1, y1, _x2, y2 = lb
        lh = y2 - y1
        if lh > 16:
            errs.append(f"lobster too tall {lh}px (max ~14px in band)")
        if lh < 5:
            errs.append(f"lobster too small {lh}px (min ~6px in band)")
        if y1 < 2 or y2 > h - 2:
            errs.append(f"lobster clipped y={y1}-{y2} band_h={h}")
    if ref_band is not None:
        cur = np.asarray(band.convert("RGB"), dtype=np.float32)
        ref = np.asarray(ref_band.convert("RGB").resize(band.size), dtype=np.float32)
        # Ignore red lobster pixels when comparing to approved framing.
        ignore = red_mask(cur) | red_mask(ref)
        diff = np.abs(cur - ref)
        diff[ignore] = 0
        if float(diff.mean()) > 6.0:
            errs.append(f"framing drift vs approved mean_diff={diff.mean():.1f}")
    return errs


def main() -> None:
    failures: list[str] = []
    for slug in SLUGS:
        for label, width in (("1280", 1280), ("390", 390)):
            png = ART / f"{slug}-desktop-{width}.png" if width >= 1024 else ART / f"{slug}-mobile.png"
            ref = None
            ref_path = APPROVED_REF / f"{slug}-{'desktop' if width >= 1024 else 'mobile'}-band.png"
            if ref_path.is_file():
                ref = Image.open(ref_path)
            if not png.is_file():
                failures.append(f"missing {png}")
                continue
            errs = check(png, width, ref)
            status = "PASS" if not errs else "FAIL"
            print(status, png.name, "; ".join(errs) if errs else "ok")
            failures.extend(f"{png.name}: {e}" for e in errs)
    if failures:
        print("\nQuality gate FAILED")
        for f in failures:
            print(" -", f)
        raise SystemExit(1)
    print("\nAll hero quality checks passed.")


if __name__ == "__main__":
    main()
