#!/usr/bin/env python3
"""Simulate R15 desktop hero (ref-full.jpg); assert column jump <= 6 in left 55%."""

from __future__ import annotations

import statistics
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Jobsy.Web/wwwroot/images/candidate/ref-full.jpg"
CREAM = (251, 245, 236)
HERO_W, HERO_H = 1440, 200
ART_INSET_LEFT = 0.34
FADE_PX = 160
MAX_JUMP = 6.0
CHECK_FRAC = 0.55


def _column_means(im: Image.Image) -> list[tuple[float, float, float]]:
    w, h = im.size
    px = im.load()
    return [
        (
            statistics.mean([px[x, y][0] for y in range(h)]),
            statistics.mean([px[x, y][1] for y in range(h)]),
            statistics.mean([px[x, y][2] for y in range(h)]),
        )
        for x in range(w)
    ]


def _max_jump(cols: list[tuple[float, float, float]], until_x: int) -> float:
    best = 0.0
    for i in range(1, min(until_x, len(cols))):
        a, b = cols[i - 1], cols[i]
        best = max(
            best,
            abs(a[0] - b[0]) + abs(a[1] - b[1]) + abs(a[2] - b[2]),
        )
    return best


def compose_desktop() -> Image.Image:
    canvas = Image.new("RGB", (HERO_W, HERO_H), CREAM)
    art = Image.open(SRC).convert("RGBA")
    scale = HERO_H / art.height
    art_w = int(art.width * scale)
    art = art.resize((art_w, HERO_H), Image.Resampling.LANCZOS)
    paste_x = HERO_W - art_w
    art_left = int(HERO_W * ART_INSET_LEFT)
    if paste_x < art_left:
        paste_x = art_left
    fade_px = FADE_PX
    apx = art.load()
    for x in range(art_w):
        vx = paste_x + x
        alpha = 255
        if vx < art_left + fade_px:
            t = max(0.0, min(1.0, (vx - art_left) / fade_px))
            alpha = int(255 * t)
        for y in range(HERO_H):
            r, g, b = apx[x, y][:3]
            apx[x, y] = (r, g, b, alpha)
    canvas.paste(art, (paste_x, 0), art)
    return canvas


def main() -> int:
    hero = compose_desktop()
    cols = _column_means(hero)
    until = int(HERO_W * CHECK_FRAC)
    jump = _max_jump(cols, until)
    out = ROOT / "Jobsy.Web/wwwroot/images/candidate/ref-full-desktop-check.png"
    hero.save(out)
    print(f"Desktop seam check: jump {jump:.2f} (0–{until}px), wrote {out}")
    if jump > MAX_JUMP:
        print(f"FAIL: jump {jump:.2f} > {MAX_JUMP}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
