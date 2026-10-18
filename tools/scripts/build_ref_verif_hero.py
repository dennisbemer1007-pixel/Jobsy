#!/usr/bin/env python3
"""Build ref-verif-hero.jpg: wide cream left, soft blend, full figures on the right."""

from __future__ import annotations

import statistics
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SPOTS = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated/spots"
OUT = SPOTS / "ref-verif-hero.jpg"

CREAM = (247, 244, 240)
PEACH = (231, 205, 182)
CANVAS_W, CANVAS_H = 2560, 720
BLEND_FRAC = 0.38
ART_START_FRAC = 0.20
ART_FEATHER_PX = 360
DECOR_STRIP_PX = 140
JUMP_CHECK_FRAC = 0.27
MAX_COLUMN_JUMP = 6.0


def _column_means(im: Image.Image) -> list[tuple[float, float, float]]:
    w, h = im.size
    px = im.load()
    cols: list[tuple[float, float, float]] = []
    for x in range(w):
        rs = [px[x, y][0] for y in range(h)]
        gs = [px[x, y][1] for y in range(h)]
        bs = [px[x, y][2] for y in range(h)]
        cols.append((statistics.mean(rs), statistics.mean(gs), statistics.mean(bs)))
    return cols


def _max_column_jump(cols: list[tuple[float, float, float]], until_x: int) -> float:
    best = 0.0
    until_x = min(until_x, len(cols))
    for i in range(1, until_x):
        a = cols[i - 1]
        b = cols[i]
        jump = abs(a[0] - b[0]) + abs(a[1] - b[1]) + abs(a[2] - b[2])
        best = max(best, jump)
    return best


def _decor_strip(width: int, height: int) -> Image.Image:
    strip = Image.new("RGB", (width, height))
    px = strip.load()
    for x in range(width):
        t = (x / max(1, width - 1)) ** 0.95
        for y in range(height):
            r = int(CREAM[0] + (PEACH[0] - CREAM[0]) * t)
            g = int(CREAM[1] + (PEACH[1] - CREAM[1]) * t)
            b = int(CREAM[2] + (PEACH[2] - CREAM[2]) * t)
            px[x, y] = (r, g, b)
    return strip


def _feather_art_alpha(art: Image.Image, feather: int) -> Image.Image:
    rgba = art.convert("RGBA")
    w, h = rgba.size
    feather = min(feather, w)
    px = rgba.load()
    for x in range(feather):
        t = (x / max(1, feather - 1)) ** 1.15
        alpha = int(255 * t)
        for y in range(h):
            r, g, b, _ = px[x, y]
            px[x, y] = (r, g, b, alpha)
    return rgba


def build() -> None:
    links = Image.open(SPOTS / "login-links.jpg").convert("RGB")
    rechts = Image.open(SPOTS / "login-rechts.jpg").convert("RGB")
    trim_links = int(links.width * 0.40)
    links = links.crop((trim_links, 0, links.width, links.height))
    trim_rechts = int(rechts.width * 0.06)
    rechts = rechts.crop((trim_rechts, 0, rechts.width, rechts.height))

    combo = Image.new("RGB", (links.width + rechts.width, links.height))
    combo.paste(links, (0, 0))
    combo.paste(rechts, (links.width, 0))

    paste_x = int(CANVAS_W * ART_START_FRAC)
    target_art_w = CANVAS_W - paste_x
    overlap = 90
    core = combo.resize((target_art_w - DECOR_STRIP_PX + overlap, CANVAS_H), Image.Resampling.LANCZOS)
    decor = _decor_strip(DECOR_STRIP_PX, CANVAS_H)
    art = Image.new("RGB", (target_art_w, CANVAS_H))
    art.paste(decor, (0, 0))
    core_rgba = core.convert("RGBA")
    cpx = core_rgba.load()
    for x in range(min(overlap, core.width)):
        t = (x / max(1, overlap - 1)) ** 1.25
        alpha = int(255 * t)
        for y in range(CANVAS_H):
            r, g, b, _ = cpx[x, y]
            cpx[x, y] = (r, g, b, alpha)
    art.paste(core_rgba, (DECOR_STRIP_PX - overlap, 0), core_rgba)

    art_rgba = _feather_art_alpha(art, ART_FEATHER_PX)
    canvas = Image.new("RGB", (CANVAS_W, CANVAS_H), CREAM)
    canvas.paste(art_rgba, (paste_x, 0), art_rgba)

    blend_w = max(2, int(CANVAS_W * BLEND_FRAC))
    px = canvas.load()
    for x in range(blend_w):
        keep = (x / (blend_w - 1)) ** 1.02
        for y in range(CANVAS_H):
            r, g, b = px[x, y]
            px[x, y] = (
                int(CREAM[0] + (r - CREAM[0]) * keep),
                int(CREAM[1] + (g - CREAM[1]) * keep),
                int(CREAM[2] + (b - CREAM[2]) * keep),
            )

    cols = _column_means(canvas)
    check_until = int(CANVAS_W * JUMP_CHECK_FRAC)
    jump = _max_column_jump(cols, check_until)
    if jump > MAX_COLUMN_JUMP:
        raise SystemExit(
            f"Background column jump {jump:.2f} exceeds {MAX_COLUMN_JUMP} in 0–{check_until}px."
        )

    OUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUT, quality=92, optimize=True)
    print(
        f"Wrote {OUT} ({CANVAS_W}x{CANVAS_H}), bg jump {jump:.2f} (0–{check_until}px), art@{paste_x}"
    )


if __name__ == "__main__":
    build()
