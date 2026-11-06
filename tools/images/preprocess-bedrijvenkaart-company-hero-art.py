#!/usr/bin/env python3
"""R21: company public header art — PNG with wall/sky alpha fade (finale-hero crop)."""
from __future__ import annotations

import math
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Jobsy.Web/wwwroot/images/onboarding/finale-hero.jpg"
OUT = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated/company-public-hero-art.png"

CROP_X = 432
CROP_Y = 0
CROP_W = 1280 - CROP_X
CROP_H = 720
FADE_FRAC = 0.25


def rgb_dist(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def is_soft_background(r: int, g: int, b: int) -> bool:
    if r >= 218 and g >= 210 and b >= 198 and max(r, g, b) - min(r, g, b) <= 42:
        return True
    if g >= r - 12 and g >= b - 8 and max(r, g, b) - min(r, g, b) <= 55:
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
    fade_end = max(10, int(w * FADE_FRAC))
    px = crop.load()

    for y in range(h):
        for x in range(fade_end):
            r, g, b, a = px[x, y]
            if not is_soft_background(r, g, b):
                continue
            wgt = fade_weight(x, fade_end)
            px[x, y] = (r, g, b, int(a * (1.0 - wgt)))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    crop.save(OUT, optimize=True)
    print(f"Wrote {OUT} ({w}x{h})")


if __name__ == "__main__":
    main()
