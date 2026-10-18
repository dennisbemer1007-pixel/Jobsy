#!/usr/bin/env python3
"""Build ref-verif hero assets: desktop wide band + mobile figure crop (opaque, no figure alpha)."""

from __future__ import annotations

import statistics
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SPOTS = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated/spots"
OUT_DESKTOP = SPOTS / "ref-verif-hero.jpg"
OUT_MOBILE = SPOTS / "ref-verif-hero-mobile.jpg"

CREAM = (247, 244, 240)
PEACH = (231, 205, 182)
CANVAS_W, CANVAS_H = 2560, 720
MOBILE_W, MOBILE_H = 1200, 720
SOLID_CREAM_FRAC = 0.25
ART_START_FRAC = 0.30
DECOR_STRIP_PX = 160
MAX_COLUMN_JUMP = 6.0
JUMP_CHECK_PX = 180


def _dist(c: tuple[int, int, int], ref: tuple[int, int, int] = CREAM) -> int:
    return abs(c[0] - ref[0]) + abs(c[1] - ref[1]) + abs(c[2] - ref[2])


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


def _max_column_jump(cols: list[tuple[float, float, float]], start: int, until_x: int) -> float:
    best = 0.0
    start = max(0, start)
    until_x = min(until_x, len(cols))
    for i in range(start + 1, until_x):
        a = cols[i - 1]
        b = cols[i]
        jump = abs(a[0] - b[0]) + abs(a[1] - b[1]) + abs(a[2] - b[2])
        best = max(best, jump)
    return best


def _decor_strip(width: int, height: int) -> Image.Image:
    strip = Image.new("RGB", (width, height))
    px = strip.load()
    for x in range(width):
        t = (x / max(1, width - 1)) ** 0.92
        for y in range(height):
            r = int(CREAM[0] + (PEACH[0] - CREAM[0]) * t)
            g = int(CREAM[1] + (PEACH[1] - CREAM[1]) * t)
            b = int(CREAM[2] + (PEACH[2] - CREAM[2]) * t)
            px[x, y] = (r, g, b)
    return strip


def _load_panorama() -> Image.Image:
    links = Image.open(SPOTS / "login-links.jpg").convert("RGB")
    rechts = Image.open(SPOTS / "login-rechts.jpg").convert("RGB")
    combo = Image.new("RGB", (links.width + rechts.width, links.height))
    combo.paste(links, (0, 0))
    combo.paste(rechts, (links.width, 0))
    return combo


def _figure_crop_box(combo: Image.Image, x_min: int) -> tuple[int, int, int, int]:
    """Crop window with full-height figures, starting at or after x_min (skip left partial figure)."""
    w, h = combo.size
    px = combo.load()
    y0 = int(h * 0.12)
    minx, miny, maxx, maxy = w, h, y0, 0
    for y in range(y0, h):
        for x in range(x_min, w):
            if _dist(px[x, y]) > 58:
                minx = min(minx, x)
                maxx = max(maxx, x)
                miny = min(miny, y)
                maxy = max(maxy, y)
    pad_x = 28
    pad_y = 12
    minx = max(x_min, minx - pad_x)
    maxx = min(w - 1, maxx + pad_x)
    miny = max(0, miny - pad_y)
    maxy = min(h - 1, maxy + pad_y)
    return minx, miny, maxx + 1, maxy + 1


def _paste_opaque_figures(
    canvas: Image.Image,
    figures: Image.Image,
    paste_x: int,
    decor_px: int,
) -> int:
    """Paste decor + opaque figures; returns x where decor starts on canvas."""
    target_h = canvas.height
    fig_h = figures.height
    scale = target_h / fig_h
    fig_w = max(1, int(figures.width * scale))
    scaled = figures.resize((fig_w, target_h), Image.Resampling.LANCZOS)
    decor = _decor_strip(decor_px, target_h)
    block = Image.new("RGB", (decor_px + fig_w, target_h), CREAM)
    block.paste(decor, (0, 0))
    block.paste(scaled, (decor_px, 0))
    canvas.paste(block, (paste_x, 0))
    return paste_x


def _blend_canvas_left_background(canvas: Image.Image, art_start_x: int) -> None:
    """Pull left ~25% of canvas toward cream (background columns only — top/bottom bands)."""
    w, h = canvas.size
    px = canvas.load()
    blend_until = int(w * SOLID_CREAM_FRAC)
    for x in range(blend_until):
        t = 1.0 if x < blend_until - 24 else (blend_until - x) / 24.0
        for y in range(h):
            if y < int(h * 0.08) or y > int(h * 0.92):
                r, g, b = px[x, y]
                px[x, y] = (
                    int(CREAM[0] + (r - CREAM[0]) * (1.0 - t)),
                    int(CREAM[1] + (g - CREAM[1]) * (1.0 - t)),
                    int(CREAM[2] + (b - CREAM[2]) * (1.0 - t)),
                )
    for x in range(min(art_start_x, blend_until), art_start_x):
        for y in range(h):
            px[x, y] = CREAM


def build_desktop(combo: Image.Image) -> tuple[int, float]:
    # Right panorama half: both men, no left-door / ghost column from links half.
    x_min = combo.width // 2
    crop = combo.crop(_figure_crop_box(combo, x_min))

    paste_x = int(CANVAS_W * ART_START_FRAC)
    canvas = Image.new("RGB", (CANVAS_W, CANVAS_H), CREAM)
    decor_start = _paste_opaque_figures(canvas, crop, paste_x, DECOR_STRIP_PX)
    _blend_canvas_left_background(canvas, decor_start)

    cols = _column_means(canvas)
    decor_end = decor_start + DECOR_STRIP_PX
    jump = _max_column_jump(cols, decor_start, decor_end)
    if jump > MAX_COLUMN_JUMP:
        raise SystemExit(
            f"Desktop decor jump {jump:.2f} exceeds {MAX_COLUMN_JUMP} "
            f"(cols {decor_start}–{decor_end})."
        )

    OUT_DESKTOP.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUT_DESKTOP, quality=92, optimize=True)
    return decor_start, jump


def build_mobile(combo: Image.Image) -> tuple[int, float]:
    x_min = int(combo.width * 0.46)
    crop = combo.crop(_figure_crop_box(combo, x_min))

    cream_px = int(MOBILE_W * 0.55)
    canvas = Image.new("RGB", (MOBILE_W, MOBILE_H), CREAM)
    target_h = MOBILE_H
    scale = target_h / crop.height
    fig_w = max(1, int(crop.width * scale))
    scaled = crop.resize((fig_w, target_h), Image.Resampling.LANCZOS)

    fade_w = 120
    fade = _decor_strip(fade_w, target_h)
    paste_x = cream_px - fade_w
    canvas.paste(fade, (paste_x, 0))
    canvas.paste(scaled, (cream_px, 0))

    cols = _column_means(canvas)
    art_left = paste_x
    jump = _max_column_jump(cols, paste_x, paste_x + fade_w)
    if jump > MAX_COLUMN_JUMP:
        raise SystemExit(
            f"Mobile art left-edge jump {jump:.2f} exceeds {MAX_COLUMN_JUMP}."
        )

    canvas.save(OUT_MOBILE, quality=92, optimize=True)
    return art_left, jump


def build() -> None:
    combo = _load_panorama()
    d_start, d_jump = build_desktop(combo)
    m_start, m_jump = build_mobile(combo)
    print(
        f"Wrote {OUT_DESKTOP} ({CANVAS_W}x{CANVAS_H}), art@{d_start}, jump {d_jump:.2f}; "
        f"{OUT_MOBILE} ({MOBILE_W}x{MOBILE_H}), art@{m_start}, jump {m_jump:.2f}"
    )


if __name__ == "__main__":
    build()
