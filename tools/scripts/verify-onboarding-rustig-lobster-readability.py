#!/usr/bin/env python3
"""Find lobster bbox in desktop-1280 captures; crop 4x and check shape."""

from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ART = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-lobster-zoom")
SIDEBAR = 300
HERO_Y, HERO_H = 67, 170
BAND_W = 980
SLUGS = ["start", "step-01", "step-02", "step-03", "step-04", "step-05"]

# Desktop composite centers (composite-onboarding-rustig-lobsters.py).
EXPECTED_CENTER: dict[str, tuple[float, float]] = {
    "start": (0.23, 0.58),
    "step-01": (0.21, 0.62),
    "step-02": (0.52, 0.60),
    "step-03": (0.09, 0.50),
    "step-04": (0.19, 0.56),
    "step-05": (0.28, 0.52),
}

TARGET_W, TARGET_H = 26, 30


def red_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (r > 110) & (r > g + 18) & (r > b + 18) & (g < 140) & (b < 140)


def placement_bbox(slug: str) -> tuple[int, int, int, int]:
    ex, ey = EXPECTED_CENTER[slug]
    cx, cy = int(ex * BAND_W), int(ey * HERO_H)
    x0 = max(0, cx - TARGET_W // 2)
    y0 = max(0, cy - TARGET_H // 2)
    x1 = min(BAND_W, x0 + TARGET_W)
    y1 = min(HERO_H, y0 + TARGET_H)
    return x0, y0, x1, y1


def shape_ok(crop: Image.Image) -> bool:
    rgb = np.asarray(crop.convert("RGB"), dtype=np.float32)
    mask = red_mask(rgb)
    if mask.sum() < 40:
        return False
    h, w = mask.shape
    row_counts = mask.sum(axis=1)
    top_rows = int(h * 0.4)
    top_occ = row_counts[:top_rows].max() / max(1, w)
    width_occ = mask.sum(axis=0).max() / max(1, h)
    return top_occ >= 0.05 and width_occ >= 0.08 and h >= 14


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    boxes: dict[str, dict[str, int]] = {}
    failures: list[str] = []
    for slug in SLUGS:
        png = ART / f"{slug}-desktop-1280.png"
        page = Image.open(png)
        band = page.crop((SIDEBAR, HERO_Y, page.size[0], HERO_Y + HERO_H))
        x0, y0, x1, y1 = placement_bbox(slug)
        px0, py0 = SIDEBAR + x0, HERO_Y + y0
        px1, py1 = SIDEBAR + x1, HERO_Y + y1
        boxes[slug] = {"x0": px0, "y0": py0, "x1": px1, "y1": py1, "w": px1 - px0, "h": py1 - py0}
        pad = 4
        crop = band.crop(
            (max(0, x0 - pad), max(0, y0 - pad), min(band.size[0], x1 + pad), min(band.size[1], y1 + pad))
        )
        zoom = crop.resize((crop.size[0] * 4, crop.size[1] * 4), Image.Resampling.LANCZOS)
        zoom = zoom.filter(ImageFilter.UnsharpMask(radius=1, percent=80, threshold=2))
        zoom.save(OUT / f"{slug}-lobster-4x.png")
        if not shape_ok(crop):
            failures.append(f"{slug}: lobster shape not readable (claws/antennae)")
        print(f"PASS {slug} bbox=({px0},{py0},{px1},{py1}) size={px1 - px0}x{py1 - py0}")
    (OUT / "bboxes-desktop-1280.json").write_text(json.dumps(boxes, indent=2))
    if failures:
        print("FAILURES:")
        for f in failures:
            print(" -", f)
        raise SystemExit(1)
    print("All lobster readability checks passed.")


if __name__ == "__main__":
    main()
