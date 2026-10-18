#!/usr/bin/env python3
"""Blend left ~20%% of onboarding hero art from peach/cream border tones to page --bg (#f5f2ee).

Only pixels that match soft background tones are recolored; saturated / figure pixels are left intact.
Writes JPG + WebP under Jobsy.Web/wwwroot/images/onboarding/.
"""
from __future__ import annotations

import math
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "Jobsy.Web/wwwroot/images/onboarding"
SLUGS = ("kennismaken", "beschikbaarheid", "vervoer", "werkervaring", "opleiding", "droombaan")

PAGE_BG = (245, 242, 238)  # --bg #f5f2ee
FADE_FRACTION = 0.28
EDGE_SAMPLE_FRACTION = 0.06
BG_DISTANCE_MAX = 42.0


def rgb_dist(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def sample_edge_palette(img: Image.Image) -> list[tuple[int, int, int]]:
    w, h = img.size
    edge_w = max(8, int(w * EDGE_SAMPLE_FRACTION))
    pixels = img.load()
    samples: list[tuple[int, int, int]] = []
    for y in range(0, h, 4):
        for x in range(0, edge_w, 4):
            samples.append(pixels[x, y][:3])
    samples.sort(key=lambda c: (c[0] + c[1] + c[2]))
    if not samples:
        return [PAGE_BG]
    q1 = samples[len(samples) // 4]
    med = samples[len(samples) // 2]
    q3 = samples[(3 * len(samples)) // 4]
    return list({q1, med, q3})


def looks_like_soft_background(r: int, g: int, b: int, palette: list[tuple[int, int, int]]) -> bool:
    if min(rgb_dist((r, g, b), p) for p in palette) <= BG_DISTANCE_MAX:
        return True
    # Peach / cream wash (low contrast, warm)
    if r >= 190 and g >= 170 and b >= 150 and (r - b) <= 55 and abs(r - g) <= 45:
        max_c = max(r, g, b)
        min_c = min(r, g, b)
        if max_c - min_c <= 55:
            return True
    return False


def fade_weight(x: int, width: int) -> float:
    fade_end = int(width * FADE_FRACTION)
    if x >= fade_end:
        return 0.0
    t = 1.0 - (x / fade_end)
    return t * t * (3.0 - 2.0 * t)  # smoothstep


def process_image(path: Path) -> None:
    img = Image.open(path).convert("RGB")
    w, h = img.size
    palette = sample_edge_palette(img)
    pixels = img.load()
    for y in range(h):
        for x in range(w):
            r, g, b = pixels[x, y]
            hard_left = x < int(w * 0.08)
            wgt = fade_weight(x, w)
            if wgt <= 0 and not hard_left:
                continue
            if not looks_like_soft_background(r, g, b, palette):
                continue
            strength = 1.0 if hard_left else wgt
            nr = int(r + (PAGE_BG[0] - r) * strength)
            ng = int(g + (PAGE_BG[1] - g) * strength)
            nb = int(b + (PAGE_BG[2] - b) * strength)
            pixels[x, y] = (nr, ng, nb)
    img.save(path, quality=92, optimize=True)
    webp_path = path.with_suffix(".webp")
    img.save(webp_path, format="WEBP", quality=88, method=6)
    print(f"processed {path.name} + {webp_path.name}")


def main() -> int:
    for slug in SLUGS:
        jpg = OUT_DIR / f"{slug}.jpg"
        if not jpg.exists():
            print(f"missing {jpg}", file=sys.stderr)
            return 1
        process_image(jpg)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
