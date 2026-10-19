#!/usr/bin/env python3
"""Extend door crop left with pearl→wall blend so desktop hero has no hard art edge."""
from __future__ import annotations

import math
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated/bedrijvenkaart-hero-door-crop.jpg"
OUT = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated/bedrijvenkaart-hero-door-desktop.jpg"

PEARL = (247, 244, 240)  # --pearl #f7f4f0
WALL = (239, 224, 212)  # sampled wall in crop


def rgb_dist(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def is_wallish(r: int, g: int, b: int) -> bool:
    if rgb_dist((r, g, b), WALL) <= 38:
        return True
    if r >= 210 and g >= 195 and b >= 175 and (r - b) <= 45:
        return True
    return False


def main() -> None:
    src = Image.open(SRC).convert("RGB")
    w, h = src.size
    pad = int(w * 0.42)
    fade_cols = max(12, int(w * 0.14))
    out_w = w + pad
    out = Image.new("RGB", (out_w, h), PEARL)
    px = src.load()
    out_px = out.load()

    for x in range(pad):
        t = x / max(1, pad - 1)
        c = tuple(int(PEARL[i] + (WALL[i] - PEARL[i]) * t) for i in range(3))
        for y in range(h):
            out_px[x, y] = c

    out.paste(src, (pad, 0))

    for x in range(pad, pad + fade_cols):
        t = (x - pad) / max(1, fade_cols - 1)
        for y in range(h):
            r, g, b = px[x - pad, y]
            if not is_wallish(r, g, b):
                continue
            target = tuple(int(PEARL[i] + (WALL[i] - PEARL[i]) * min(1.0, t + 0.35)) for i in range(3))
            out_px[x, y] = tuple(int(r * (1 - t) + target[i] * t) for i in range(3))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    out.save(OUT, quality=93, optimize=True)
    print(f"Wrote {OUT} ({out_w}x{h})")


if __name__ == "__main__":
    main()
