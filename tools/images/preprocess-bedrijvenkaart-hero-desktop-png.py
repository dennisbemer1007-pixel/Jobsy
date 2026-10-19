#!/usr/bin/env python3
"""R20: desktop PNG crop — wall fade in alpha, no sliced flowers, full right edge."""
from __future__ import annotations

import math
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated/bedrijvenkaart-hero-mobile.jpg"
OUT = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated/bedrijvenkaart-hero-desktop.png"

# Plain wall left of window (excludes bottom-left flower sprigs in source).
CROP_X = 728
CROP_Y = 0
CROP_W = 1200 - CROP_X
CROP_H = 720
FADE_FRAC = 0.18


def rgb_dist(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def is_wallish(r: int, g: int, b: int) -> bool:
    if r >= 205 and g >= 185 and b >= 165 and (r - b) <= 50:
        return True
    if g >= r - 8 and g >= b - 6 and max(r, g, b) - min(r, g, b) <= 45:
        return True
    return False


def fade_weight(x: int, fade_end: int) -> float:
    if fade_end <= 0 or x >= fade_end:
        return 0.0
    t = 1.0 - (x / fade_end)
    return t * t * (3.0 - 2.0 * t)


def main() -> None:
    src = Image.open(SRC).convert("RGBA")
    crop = src.crop((CROP_X, CROP_Y, CROP_X + CROP_W, CROP_Y + CROP_H))
    w, h = crop.size
    fade_end = max(8, int(w * FADE_FRAC))
    px = crop.load()

    for y in range(h):
        for x in range(fade_end):
            r, g, b, a = px[x, y]
            if not is_wallish(r, g, b):
                continue
            wgt = fade_weight(x, fade_end)
            px[x, y] = (r, g, b, int(a * (1.0 - wgt)))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    crop.save(OUT, optimize=True)
    print(f"Wrote {OUT} ({w}x{h}) crop from x={CROP_X}")


if __name__ == "__main__":
    main()
