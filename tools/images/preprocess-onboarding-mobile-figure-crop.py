#!/usr/bin/env python3
"""R11: transparent mobile figure crops for onboarding hero (≤1023px only).

Builds {slug}-mobile.webp + {slug}-mobile.png from R10 desktop JPGs: tight crop around
figures + immediate ground/shadow, background alpha removed.
"""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "Jobsy.Web/wwwroot/images/onboarding"
SEAM = Path(__file__).resolve().parent / "preprocess-onboarding-hero-seam.py"
SLUGS = ("kennismaken", "beschikbaarheid", "vervoer", "werkervaring", "opleiding", "droombaan")

# Optional manual figure region (left, top, right, bottom as fractions) when auto mask under-detects.
MOBILE_FIGURE_BOX_FRAC: dict[str, tuple[float, float, float, float]] = {
    "droombaan": (0.46, 0.06, 0.99, 0.98),
}

PAGE_BG = (245, 242, 238)
NEAR_FIGURE_PX = 16
BBOX_PAD = 8
MIN_CROP_W = 120
MIN_CROP_H = 160
MIN_FIGURE_X_FRAC = 0.40


def load_seam():
    spec = importlib.util.spec_from_file_location("seam", SEAM)
    mod = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(mod)
    return mod


def build_content_mask(img: Image.Image, seam) -> list[list[bool]]:
    """Figure pixels plus scenery/ground within NEAR_FIGURE_PX (BFS) of a figure."""
    w, h = img.size
    palette = seam.sample_edge_palette(img)
    px = img.load()
    figure = seam.build_figure_protect_mask(img, palette)
    content = [[False] * w for _ in range(h)]
    queue: list[tuple[int, int, int]] = []

    for y in range(h):
        for x in range(w):
            if figure[y][x]:
                content[y][x] = True
                queue.append((x, y, 0))

    if not queue:
        for y in range(h):
            for x in range(int(w * 0.38), w):
                r, g, b = px[x, y][:3]
                if not seam.looks_like_soft_background(r, g, b, palette):
                    content[y][x] = True
        return content

    head = 0
    while head < len(queue):
        x, y, dist = queue[head]
        head += 1
        if dist >= NEAR_FIGURE_PX:
            continue
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if nx < 0 or ny < 0 or nx >= w or ny >= h or content[ny][nx]:
                continue
            r, g, b = px[nx, ny][:3]
            if seam.looks_like_soft_background(r, g, b, palette):
                continue
            content[ny][nx] = True
            queue.append((nx, ny, dist + 1))

    return content


def bbox_of_mask(mask: list[list[bool]]) -> tuple[int, int, int, int] | None:
    h = len(mask)
    w = len(mask[0])
    xs: list[int] = []
    ys: list[int] = []
    for y in range(h):
        for x in range(w):
            if mask[y][x]:
                xs.append(x)
                ys.append(y)
    if not xs:
        return None
    return min(xs), min(ys), max(xs), max(ys)


def alpha_for_pixel(r: int, g: int, b: int, seam, palette, near_content: bool) -> int:
    if near_content:
        return 255
    if seam.looks_like_soft_background(r, g, b, palette):
        return 0
    if abs(r - PAGE_BG[0]) <= 8 and abs(g - PAGE_BG[1]) <= 8 and abs(b - PAGE_BG[2]) <= 8:
        return 0
    # Leftover decor far from figures — drop (less decor > ghosts).
    return 0


def process_slug(slug: str, seam) -> None:
    src = OUT_DIR / f"{slug}.jpg"
    if not src.exists():
        raise FileNotFoundError(src)
    img = Image.open(src).convert("RGB")
    w, h = img.size
    palette = seam.sample_edge_palette(img)
    px = img.load()
    figure = seam.build_figure_protect_mask(img, palette)
    content = build_content_mask(img, seam)
    min_x = int(w * MIN_FIGURE_X_FRAC)
    if slug in MOBILE_FIGURE_BOX_FRAC:
        lf, tf, rf, bf = MOBILE_FIGURE_BOX_FRAC[slug]
        rx0, ry0 = int(w * lf), int(h * tf)
        rx1, ry1 = min(w - 1, int(w * rf)), min(h - 1, int(h * bf))
        manual = [[False] * w for _ in range(h)]
        for y in range(ry0, ry1 + 1):
            for x in range(rx0, rx1 + 1):
                r, g, b = px[x, y][:3]
                if not seam.looks_like_soft_background(r, g, b, palette):
                    manual[y][x] = True
        fig_only = manual
    else:
        fig_only = [[figure[y][x] and x >= min_x for x in range(w)] for y in range(h)]
    box = bbox_of_mask(fig_only) or bbox_of_mask(
        [[content[y][x] and x >= min_x for x in range(w)] for y in range(h)]
    )
    if box is None:
        box = (int(w * 0.45), int(h * 0.08), w - 4, h - 4)
    x0, y0, x1, y1 = box
    x0 = max(x0, min_x)
    trimmed = [[content[y][x] and x >= min_x for x in range(w)] for y in range(h)]
    x0 = max(0, x0 - BBOX_PAD)
    y0 = max(0, y0 - BBOX_PAD)
    x1 = min(w - 1, x1 + BBOX_PAD)
    y1 = min(h - 1, y1 + BBOX_PAD)
    if x1 - x0 < MIN_CROP_W:
        x0 = max(0, x1 - MIN_CROP_W)
    if y1 - y0 < MIN_CROP_H:
        y0 = max(0, y1 - MIN_CROP_H)

    crop_w = x1 - x0 + 1
    crop_h = y1 - y0 + 1
    out = Image.new("RGBA", (crop_w, crop_h), (0, 0, 0, 0))
    out_px = out.load()

    fig_box = bbox_of_mask(fig_only)
    foot_y1 = min(h - 1, (fig_box[3] if fig_box else y1) + 18)
    foot_x0 = max(0, (fig_box[0] if fig_box else x0) - 6)
    foot_x1 = min(w - 1, (fig_box[2] if fig_box else x1) + 6)

    for cy in range(crop_h):
        for cx in range(crop_w):
            sx, sy = x0 + cx, y0 + cy
            r, g, b = px[sx, sy][:3]
            if fig_only[sy][sx]:
                a = 255
            elif (
                foot_x0 <= sx <= foot_x1
                and (fig_box[1] if fig_box else y0) - 8 <= sy <= foot_y1
                and trimmed[sy][sx]
            ):
                a = 255
            else:
                a = alpha_for_pixel(r, g, b, seam, palette, False)
            out_px[cx, cy] = (r, g, b, a)

    alpha_mask = [[out_px[cx, cy][3] >= 128 for cx in range(crop_w)] for cy in range(crop_h)]
    alpha_box = bbox_of_mask(alpha_mask)
    if alpha_box is not None:
        ax0, ay0, ax1, ay1 = alpha_box
        out = out.crop((ax0, ay0, ax1 + 1, ay1 + 1))
        crop_w, crop_h = out.size

    png_path = OUT_DIR / f"{slug}-mobile.png"
    webp_path = OUT_DIR / f"{slug}-mobile.webp"
    out.save(png_path, optimize=True)
    out.save(webp_path, format="WEBP", quality=90, method=6, lossless=False)
    opaque = sum(1 for _r, _g, _b, a in out.getdata() if a > 0)
    crop_w, crop_h = out.size
    print(
        f"{slug}-mobile: crop {crop_w}x{crop_h} from ({x0},{y0}) "
        f"opaque_px={opaque} ratio={opaque/(crop_w*crop_h):.2f}"
    )


def main() -> int:
    seam = load_seam()
    for slug in SLUGS:
        process_slug(slug, seam)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
