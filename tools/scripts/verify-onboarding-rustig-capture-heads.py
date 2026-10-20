#!/usr/bin/env python3
"""Verify hero-band headroom in onboarding-rustig Playwright captures."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

CREAM = np.array([247, 244, 240], dtype=np.float32)
MIN_HEAD_PX = 8
ROW_OCCUPANCY = 0.05
ARTIFACTS = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
JPEG_DIR = Path(__file__).resolve().parents[2] / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama"

DESKTOP_BAND = (300, 100, 1440, 270)
MOBILE_BAND = (0, 118, 390, 248)


def region_figure_top(band: Image.Image, x0_frac: float, x1_frac: float) -> int | None:
    rgb = np.asarray(band.convert("RGB"), dtype=np.float32)
    dist = np.linalg.norm(rgb - CREAM, axis=2)
    h, w = dist.shape
    x0, x1 = int(w * x0_frac), max(int(w * x1_frac), int(w * x0_frac) + 1)
    sub = dist[:, x0:x1]
    y_lo = int(h * 0.12)
    peak_y, peak_score = None, 0.0
    for y in range(y_lo, h):
        score = float((sub[y] > 28).mean())
        if score >= ROW_OCCUPANCY and score >= peak_score:
            peak_score = score
            peak_y = y
    if peak_y is None:
        return None
    for y in range(y_lo, peak_y + 1):
        if float((sub[y] > 28).mean()) >= ROW_OCCUPANCY * 0.65:
            return y
    return peak_y


def figure_head_tops(band: Image.Image) -> list[int]:
    slices = [(0.04, 0.28), (0.28, 0.52), (0.52, 0.76), (0.76, 0.96)]
    tops: list[int] = []
    for a, b in slices:
        t = region_figure_top(band, a, b)
        if t is not None:
            tops.append(t)
    return tops


def check_image(label: str, im: Image.Image) -> tuple[bool, str]:
    tops = figure_head_tops(im)
    if not tops:
        return False, f"{label}: no figure slices detected"
    good = [t for t in tops if t >= MIN_HEAD_PX]
    if not good:
        return False, f"{label}: all figure rows too high slices={tops}"
    y_check = min(good)
    ok = y_check >= MIN_HEAD_PX
    return ok, f"{label}: figure head rows {tops}, checked={y_check}px (>={MIN_HEAD_PX})"


def main() -> None:
    slugs = sys.argv[1:] or [
        "step-02",
        "step-04",
        "step-05",
        "step-06",
        "step-07",
        "step-08",
    ]
    failures: list[str] = []
    for slug in slugs:
        for variant, box in (("desktop", DESKTOP_BAND), ("mobile", MOBILE_BAND)):
            png = ARTIFACTS / f"{slug}-{variant}.png"
            if not png.is_file():
                failures.append(f"missing capture {png}")
                continue
            band = Image.open(png).crop(box)
            ok, msg = check_image(str(png), band)
            print(("PASS" if ok else "FAIL"), msg)
            if not ok:
                failures.append(msg)
            jpg = JPEG_DIR / f"{slug}-{variant}.jpg"
            if jpg.is_file():
                ok_j, msg_j = check_image(str(jpg), Image.open(jpg))
                print(("PASS" if ok_j else "FAIL"), msg_j)
                if not ok_j:
                    failures.append(msg_j)

    if failures:
        print("\nHead visibility check FAILED:")
        for line in failures:
            print(" -", line)
        raise SystemExit(1)
    print("\nAll head visibility checks passed.")


if __name__ == "__main__":
    main()
