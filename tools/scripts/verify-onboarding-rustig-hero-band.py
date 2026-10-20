#!/usr/bin/env python3
"""Verify hero band shows figures (lower half) and red lobster in Playwright captures."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

CREAM = np.array([247, 244, 240], dtype=np.float32)
ARTIFACTS = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
SLUGS = ["start", "step-01", "step-02", "step-03", "step-04", "step-05"]
DESKTOP_WIDTHS = [1024, 1280]
SIDEBAR = 300


def hero_box(width: int) -> tuple[int, int, int, int]:
    hero_h = 170 if width >= 1024 else 130
    y0 = 100 if width >= 1024 else 118
    return (SIDEBAR, y0, width, y0 + hero_h)


def analyze_band(band: Image.Image) -> tuple[bool, str]:
    rgb = np.asarray(band.convert("RGB"), dtype=np.float32)
    h, w = rgb.shape[:2]
    dist = np.linalg.norm(rgb - CREAM, axis=2)
    mask = dist > 28
    upper = mask[: h // 2, :].mean()
    lower = mask[h // 2 :, :].mean()
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    red = (r > 145) & (g < 125) & (b < 125) & (r - g > 30)
    red_count = int(red.sum())
    ok_fig = lower >= 0.45 and lower >= upper * 0.85
    ok_lob = red_count >= 40
    msg = f"lower={lower:.2f} upper={upper:.2f} red_px={red_count}"
    return ok_fig and ok_lob, msg


def main() -> None:
    failures: list[str] = []
    for slug in SLUGS:
        for width in DESKTOP_WIDTHS:
            png = ARTIFACTS / f"{slug}-desktop-{width}.png"
            if not png.is_file():
                failures.append(f"missing {png}")
                continue
            im = Image.open(png)
            band = im.crop(hero_box(width))
            ok, msg = analyze_band(band)
            line = f"{'PASS' if ok else 'FAIL'} {png.name}: {msg}"
            print(line)
            if not ok:
                failures.append(line)
        png = ARTIFACTS / f"{slug}-mobile.png"
        if not png.is_file():
            failures.append(f"missing {png}")
            continue
        im = Image.open(png)
        band = im.crop(hero_box(390))
        ok, msg = analyze_band(band)
        line = f"{'PASS' if ok else 'FAIL'} {png.name}: {msg}"
        print(line)
        if not ok:
            failures.append(line)
    if failures:
        print("\nHero band verification FAILED")
        for f in failures:
            print(" -", f)
        raise SystemExit(1)
    print("\nAll hero band checks passed.")


if __name__ == "__main__":
    main()
