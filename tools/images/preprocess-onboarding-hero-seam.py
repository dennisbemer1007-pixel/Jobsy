#!/usr/bin/env python3
"""R10: soft horizontal fade of left ~10%% of onboarding hero art to page --bg (#f5f2ee).

All non-figure pixels in the fade zone blend to PAGE_BG (ground, path, plants included).
If a figure sits inside the fade zone, pad the canvas left with cream and apply the gradient
in the padded strip so figures stay intact. Writes JPG + WebP under wwwroot/images/onboarding/.
"""
from __future__ import annotations

import math
import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "Jobsy.Web/wwwroot/images/onboarding"
SLUGS = ("kennismaken", "beschikbaarheid", "vervoer", "werkervaring", "opleiding", "droombaan")
GIT_ORIGINALS_COMMIT = "26a660c84"

PAGE_BG = (245, 242, 238)  # --bg #f5f2ee
FADE_FRACTION = 0.10
EDGE_SAMPLE_FRACTION = 0.06
BG_DISTANCE_MAX = 42.0
FIGURE_BAND_Y0 = 0.10
FIGURE_BAND_Y1 = 0.80
FIGURE_COL_FRAC = 0.055
FIGURE_PAD_MARGIN_PX = 12


def rgb_dist(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def sample_edge_palette(img: Image.Image) -> list[tuple[int, int, int]]:
    w, _h = img.size
    edge_w = max(8, int(w * EDGE_SAMPLE_FRACTION))
    pixels = img.load()
    samples: list[tuple[int, int, int]] = []
    for y in range(0, img.size[1], 4):
        for x in range(0, edge_w, 4):
            samples.append(pixels[x, y][:3])
    samples.sort(key=lambda c: (c[0] + c[1] + c[2]))
    if not samples:
        return [PAGE_BG]
    q1 = samples[len(samples) // 4]
    med = samples[len(samples) // 2]
    q3 = samples[(3 * len(samples)) // 4]
    return list({q1, med, q3, PAGE_BG})


def looks_like_soft_background(r: int, g: int, b: int, palette: list[tuple[int, int, int]]) -> bool:
    if min(rgb_dist((r, g, b), p) for p in palette) <= BG_DISTANCE_MAX:
        return True
    if r >= 190 and g >= 170 and b >= 150 and (r - b) <= 55 and abs(r - g) <= 45:
        max_c = max(r, g, b)
        min_c = min(r, g, b)
        if max_c - min_c <= 55:
            return True
    if abs(r - PAGE_BG[0]) <= 10 and abs(g - PAGE_BG[1]) <= 10 and abs(b - PAGE_BG[2]) <= 10:
        return True
    return False


def looks_like_scenery(r: int, g: int, b: int) -> bool:
    """Ground, path, plants, peach wash — fadeable non-figure foreground."""
    spread = max(r, g, b) - min(r, g, b)
    if g >= r + 10 and g >= b + 6 and g > 70:
        return True
    # Olive foliage / low-contrast greens (left-edge plant strips).
    if g >= r - 10 and g >= b - 8 and spread <= 48 and 70 <= g <= 200:
        return True
    if r >= g >= b and (r - b) <= 95 and r >= 95:
        return True
    if spread <= 22 and 35 <= max(r, g, b) <= 210:
        return True
    if r >= 190 and g >= 170 and b >= 150 and spread <= 60:
        return True
    return False


def is_figure_pixel(r: int, g: int, b: int, palette: list[tuple[int, int, int]]) -> bool:
    if looks_like_soft_background(r, g, b, palette):
        return False
    if looks_like_scenery(r, g, b):
        return False
    return True


def fade_weight(x: int, fade_end: int) -> float:
    if fade_end <= 0 or x >= fade_end:
        return 0.0
    t = 1.0 - (x / fade_end)
    return t * t * (3.0 - 2.0 * t)


def build_figure_protect_mask(img: Image.Image, palette: list[tuple[int, int, int]]) -> list[list[bool]]:
    w, h = img.size
    y0 = int(h * FIGURE_BAND_Y0)
    y1 = int(h * FIGURE_BAND_Y1)
    px = img.load()
    col_hits = [0] * w
    protect = [[False] * w for _ in range(h)]

    for y in range(y0, y1):
        for x in range(w):
            r, g, b = px[x, y][:3]
            if is_figure_pixel(r, g, b, palette):
                protect[y][x] = True
                col_hits[x] += 1

    col_thresh = max(4, int((y1 - y0) * FIGURE_COL_FRAC))
    figure_columns = {x for x in range(w) if col_hits[x] >= col_thresh}

    for y in range(h):
        for x in range(w):
            if protect[y][x]:
                continue
            if x not in figure_columns:
                continue
            r, g, b = px[x, y][:3]
            if is_figure_pixel(r, g, b, palette):
                protect[y][x] = True

    return protect


def leftmost_figure_column(_img: Image.Image, protect: list[list[bool]], y0: int, y1: int) -> int | None:
    """Left edge of the main figure mass (ignore sparse edge foliage)."""
    w = len(protect[0])
    col_hits = [sum(1 for y in range(y0, y1) if protect[y][x]) for x in range(w)]
    col_thresh = max(4, int((y1 - y0) * FIGURE_COL_FRAC))
    strong = [x for x in range(w) if col_hits[x] >= col_thresh]
    if not strong:
        return None
    max_hit = max(col_hits)
    cluster_thresh = max(col_thresh, int(max_hit * 0.35))
    cluster_cols = [x for x in strong if col_hits[x] >= cluster_thresh]
    if not cluster_cols:
        cluster_cols = strong
    return min(cluster_cols)


def pad_left_if_needed(img: Image.Image, fade_width: int, protect: list[list[bool]]) -> tuple[Image.Image, list[list[bool]], int]:
    w, h = img.size
    y0 = int(h * FIGURE_BAND_Y0)
    y1 = int(h * FIGURE_BAND_Y1)
    fig_left = leftmost_figure_column(img, protect, y0, y1)
    pad = 0
    if fig_left is not None and fig_left < fade_width + FIGURE_PAD_MARGIN_PX:
        pad = fade_width + FIGURE_PAD_MARGIN_PX - fig_left

    if pad <= 0:
        return img, protect, 0

    out = Image.new("RGB", (w + pad, h), PAGE_BG)
    out.paste(img, (pad, 0))
    px_old = img.load()
    px_new = out.load()
    for y in range(h):
        for x in range(w):
            px_new[x + pad, y] = px_old[x, y][:3]

    new_protect = [[False] * (w + pad) for _ in range(h)]
    for y in range(h):
        for x in range(w):
            if protect[y][x]:
                new_protect[y][x + pad] = True
    return out, new_protect, pad


def apply_left_fade(img: Image.Image, protect: list[list[bool]], fade_width: int) -> None:
    w, h = img.size
    px = img.load()
    fade_end = fade_width
    for y in range(h):
        for x in range(fade_end):
            if protect[y][x]:
                continue
            r, g, b = px[x, y][:3]
            wgt = fade_weight(x, fade_end)
            if wgt <= 0:
                continue
            px[x, y] = (
                int(r + (PAGE_BG[0] - r) * wgt),
                int(g + (PAGE_BG[1] - g) * wgt),
                int(b + (PAGE_BG[2] - b) * wgt),
            )


def cap_fade_zone_column_jumps(
    img: Image.Image, protect: list[list[bool]], fade_end: int, max_jump: int = 6, passes: int = 4
) -> None:
    """Cap horizontal RGB steps in the fade strip (never rewrite protected figure pixels)."""
    w, h = img.size
    px = img.load()
    x_limit = min(fade_end - 1, w - 2)
    for _ in range(passes):
        for y in range(h):
            for x in range(x_limit):
                if protect[y][x] or protect[y][x + 1]:
                    continue
                left = px[x, y][:3]
                right = px[x + 1, y][:3]
                jump = max(abs(left[i] - right[i]) for i in range(3))
                if jump <= max_jump:
                    continue
                t = max_jump / jump
                px[x, y] = tuple(int(left[i] + (right[i] - left[i]) * t) for i in range(3))


def max_left_edge_jump(img: Image.Image, ys: tuple[int, ...]) -> int:
    w, h = img.size
    px = img.load()
    worst = 0
    for y in ys:
        yy = min(max(0, y), h - 1)
        for x in range(min(w - 2, 24)):
            c1 = px[x, yy]
            c2 = px[x + 1, yy]
            jump = max(abs(c1[i] - c2[i]) for i in range(3))
            worst = max(worst, jump)
    return worst


def restore_originals_from_git() -> None:
    for slug in SLUGS:
        dest = OUT_DIR / f"{slug}.jpg"
        blob = subprocess.check_output(
            ["git", "show", f"{GIT_ORIGINALS_COMMIT}:Jobsy.Web/wwwroot/images/onboarding/{slug}.jpg"],
            cwd=ROOT,
        )
        dest.write_bytes(blob)


def process_image(path: Path) -> None:
    img = Image.open(path).convert("RGB")
    w, h = img.size
    fade_width = max(16, int(w * FADE_FRACTION))
    palette = sample_edge_palette(img)
    protect = build_figure_protect_mask(img, palette)
    img, protect, pad = pad_left_if_needed(img, fade_width, protect)
    fade_end = pad + fade_width
    apply_left_fade(img, protect, fade_end)
    cap_fade_zone_column_jumps(img, protect, fade_end)

    mid = h // 2
    bottom = h - 6
    jump = max_left_edge_jump(img, (mid, bottom))
    print(
        f"processed {path.name}: pad={pad}px fade_end={fade_end}px "
        f"left_jump(mid/bottom)={jump} size={img.size[0]}x{img.size[1]}"
    )
    if jump > 6:
        print(f"  WARN: left-edge column jump {jump} > 6 at y=mid/bottom", file=sys.stderr)

    img.save(path, quality=92, optimize=True)
    webp_path = path.with_suffix(".webp")
    img.save(webp_path, format="WEBP", quality=88, method=6)
    print(f"  wrote {webp_path.name}")


def main() -> int:
    if "--restore-git" in sys.argv:
        restore_originals_from_git()
        print(f"restored originals from {GIT_ORIGINALS_COMMIT}")
    for slug in SLUGS:
        jpg = OUT_DIR / f"{slug}.jpg"
        if not jpg.exists():
            print(f"missing {jpg}", file=sys.stderr)
            return 1
        process_image(jpg)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
