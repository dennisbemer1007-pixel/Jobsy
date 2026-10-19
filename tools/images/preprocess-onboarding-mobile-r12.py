#!/usr/bin/env python3
"""R12 mobile hero: full-bleed scenery background (figures inpainted) + opaque figure PNG."""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "Jobsy.Web/wwwroot/images/onboarding"
SEAM = Path(__file__).resolve().parent / "preprocess-onboarding-hero-seam.py"
FIGURE_CROP = Path(__file__).resolve().parent / "preprocess-onboarding-mobile-figure-crop.py"
SLUGS = ("kennismaken", "beschikbaarheid", "vervoer", "werkervaring", "opleiding", "droombaan")

PAGE_BG = (245, 242, 238)
MOBILE_W = 780
MOBILE_H = 480
TEXT_FRAC = 0.52
BRIDGE_FRAC = 0.10
MAX_LUM_JUMP = 6

MOBILE_FIGURE_BOX_FRAC: dict[str, tuple[float, float, float, float]] = {
    "droombaan": (0.46, 0.06, 0.99, 0.98),
}


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(mod)
    return mod


def luminance(r: int, g: int, b: int) -> float:
    return 0.299 * r + 0.587 * g + 0.114 * b


def dilate_mask(mask: list[list[bool]], radius: int = 3) -> list[list[bool]]:
    h = len(mask)
    w = len(mask[0])
    out = [[False] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            if not mask[y][x]:
                continue
            for dy in range(-radius, radius + 1):
                for dx in range(-radius, radius + 1):
                    if dx * dx + dy * dy <= radius * radius:
                        ny, nx = y + dy, x + dx
                        if 0 <= ny < h and 0 <= nx < w:
                            out[ny][nx] = True
    return out


def inpaint_figures(img: Image.Image, figure: list[list[bool]]) -> Image.Image:
    w, h = img.size
    px = img.load()
    out = img.copy()
    opx = out.load()
    for y in range(h):
        for x in range(w):
            if not figure[y][x]:
                continue
            rs = gs = bs = c = 0
            for dy in range(-14, 15):
                for dx in range(-14, 15):
                    if dx * dx + dy * dy > 196:
                        continue
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and not figure[ny][nx]:
                        r, g, b = px[nx, ny][:3]
                        rs += r
                        gs += g
                        bs += b
                        c += 1
            if c > 0:
                opx[x, y] = (rs // c, gs // c, bs // c)
            else:
                opx[x, y] = PAGE_BG
    return out


def cover_resize_right(img: Image.Image, tw: int, th: int) -> Image.Image:
    w, h = img.size
    scale = max(tw / w, th / h)
    nw, nh = int(w * scale + 0.5), int(h * scale + 0.5)
    scaled = img.resize((nw, nh), Image.Resampling.LANCZOS)
    x0 = max(0, nw - tw)
    y0 = max(0, nh - th)
    return scaled.crop((x0, y0, x0 + tw, y0 + th))


def apply_text_and_bridge_fade(img: Image.Image) -> None:
    w, h = img.size
    px = img.load()
    text_end = int(w * TEXT_FRAC)
    bridge_end = min(w, int(w * (TEXT_FRAC + BRIDGE_FRAC)) + 24)
    for y in range(h):
        for x in range(w):
            if x < text_end:
                px[x, y] = PAGE_BG
                continue
            if x >= bridge_end:
                continue
            t = (x - text_end) / max(1, bridge_end - text_end)
            t = t * t * (3.0 - 2.0 * t)
            r, g, b = px[x, y][:3]
            px[x, y] = (
                int(PAGE_BG[0] + (r - PAGE_BG[0]) * t),
                int(PAGE_BG[1] + (g - PAGE_BG[1]) * t),
                int(PAGE_BG[2] + (b - PAGE_BG[2]) * t),
            )


def force_luminance_jumps(img: Image.Image, passes: int = 12) -> None:
    px = img.load()
    w, h = img.size
    for _ in range(passes):
        for y in range(h):
            for x in range(w - 1):
                c1 = px[x, y][:3]
                c2 = px[x + 1, y][:3]
                j = abs(luminance(*c1) - luminance(*c2))
                if j <= MAX_LUM_JUMP:
                    continue
                mid = tuple((c1[i] + c2[i]) // 2 for i in range(3))
                px[x, y] = mid
                px[x + 1, y] = mid
        for y in range(h - 1):
            for x in range(w):
                c1 = px[x, y][:3]
                c2 = px[x, y + 1][:3]
                j = abs(luminance(*c1) - luminance(*c2))
                if j <= MAX_LUM_JUMP:
                    continue
                mid = tuple((c1[i] + c2[i]) // 2 for i in range(3))
                px[x, y] = mid
                px[x, y + 1] = mid


def cap_luminance_jumps(img: Image.Image, passes: int = 3) -> None:
    px = img.load()
    w, h = img.size
    for _ in range(passes):
        for y in range(h):
            for x in range(w - 1):
                c1 = px[x, y][:3]
                c2 = px[x + 1, y][:3]
                j = abs(luminance(*c1) - luminance(*c2))
                if j <= MAX_LUM_JUMP:
                    continue
                t = MAX_LUM_JUMP / j
                l1, l2 = luminance(*c1), luminance(*c2)
                target = l1 + (l2 - l1) * t if l2 > l1 else l1 - (l1 - l2) * t
                delta = target - l1
                px[x, y] = tuple(max(0, min(255, int(c + delta))) for c in c1)
        for y in range(h - 1):
            for x in range(w):
                c1 = px[x, y][:3]
                c2 = px[x, y + 1][:3]
                j = abs(luminance(*c1) - luminance(*c2))
                if j <= MAX_LUM_JUMP:
                    continue
                t = MAX_LUM_JUMP / j
                l1, l2 = luminance(*c1), luminance(*c2)
                target = l1 + (l2 - l1) * t if l2 > l1 else l1 - (l1 - l2) * t
                delta = target - l1
                px[x, y] = tuple(max(0, min(255, int(c + delta))) for c in c1)


def max_luminance_jump(
    img: Image.Image,
    figure_mask: list[list[bool]] | None = None,
    text_only_end: int = 0,
) -> tuple[int, str]:
    px = img.load()
    w, h = img.size
    worst = 0
    where = ""
    bridge_end = int(w * (TEXT_FRAC + BRIDGE_FRAC)) + 24
    for y in range(h):
        for x in range(w - 1):
            if x < bridge_end and x + 1 < bridge_end and text_only_end:
                continue
            if figure_mask and (figure_mask[y][x] or figure_mask[y][x + 1]):
                continue
            j = int(abs(luminance(*px[x, y][:3]) - luminance(*px[x + 1, y][:3])))
            if j > worst:
                worst = j
                where = f"h x={x} y={y}"
    for y in range(h - 1):
        for x in range(w):
            if x < text_only_end:
                continue
            if figure_mask and (figure_mask[y][x] or figure_mask[y + 1][x]):
                continue
            j = int(abs(luminance(*px[x, y][:3]) - luminance(*px[x, y + 1][:3])))
            if j > worst:
                worst = j
                where = f"v x={x} y={y}"
    return worst, where


def build_figure_mask(img: Image.Image, seam, slug: str) -> list[list[bool]]:
    w, h = img.size
    palette = seam.sample_edge_palette(img)
    px = img.load()
    figure = seam.build_figure_protect_mask(img, palette)
    if slug not in MOBILE_FIGURE_BOX_FRAC:
        return figure
    lf, tf, rf, bf = MOBILE_FIGURE_BOX_FRAC[slug]
    rx0, ry0 = int(w * lf), int(h * tf)
    rx1, ry1 = min(w - 1, int(w * rf)), min(h - 1, int(h * bf))
    for y in range(ry0, ry1 + 1):
        for x in range(rx0, rx1 + 1):
            r, g, b = px[x, y][:3]
            if seam.is_figure_pixel(r, g, b, palette) or (
                not seam.looks_like_soft_background(r, g, b, palette) and not seam.looks_like_scenery(r, g, b)
            ):
                figure[y][x] = True
    return figure


def process_mobile_bg(slug: str, seam) -> tuple[int, str]:
    src = OUT_DIR / f"{slug}.jpg"
    img = Image.open(src).convert("RGB")
    figure = dilate_mask(build_figure_mask(img, seam, slug), radius=4)
    if slug == "vervoer":
        # Vervoer: keep figures in the full-bleed bg (scene mask treats van/road as “figure”).
        no_fig = img.copy()
    else:
        no_fig = inpaint_figures(img, figure)
    mobile = cover_resize_right(no_fig, MOBILE_W, MOBILE_H)
    apply_text_and_bridge_fade(mobile)
    force_luminance_jumps(mobile, passes=10)
    mobile = mobile.filter(ImageFilter.GaussianBlur(radius=2.0))
    text_end = int(MOBILE_W * TEXT_FRAC)

    def restore_text_zone() -> None:
        px_local = mobile.load()
        for y in range(MOBILE_H):
            for x in range(text_end):
                px_local[x, y] = PAGE_BG

    restore_text_zone()
    scenic = mobile.crop((text_end, 0, MOBILE_W, MOBILE_H))
    sw, sh = scenic.size
    down = 6 if slug in ("vervoer", "opleiding") else 4
    scenic = scenic.resize((max(1, sw // down), max(1, sh // down)), Image.Resampling.BILINEAR)
    scenic = scenic.resize((sw, sh), Image.Resampling.BILINEAR)
    mobile.paste(scenic, (text_end, 0))
    apply_text_and_bridge_fade(mobile)
    restore_text_zone()
    px = mobile.load()
    for y in range(MOBILE_H - 5, MOBILE_H):
        for x in range(text_end, MOBILE_W):
            px[x, y] = px[x, MOBILE_H - 6][:3]

    loop_count = 35 if slug in ("vervoer", "opleiding") else 20
    for _ in range(loop_count):
        jump, where = max_luminance_jump(mobile, text_only_end=text_end)
        if jump <= MAX_LUM_JUMP:
            break
        force_luminance_jumps(mobile, passes=3)
        scenic = mobile.crop((text_end, 0, MOBILE_W, MOBILE_H))
        scenic = scenic.filter(ImageFilter.GaussianBlur(radius=0.9))
        mobile.paste(scenic, (text_end, 0))
        apply_text_and_bridge_fade(mobile)
        restore_text_zone()
        for y in range(MOBILE_H - 5, MOBILE_H):
            for x in range(text_end, MOBILE_W):
                px[x, y] = px[x, MOBILE_H - 6][:3]
    jump, where = max_luminance_jump(mobile, text_only_end=text_end)
    mobile.save(OUT_DIR / f"{slug}-mobile-bg.png", optimize=True)
    mobile.save(OUT_DIR / f"{slug}-mobile-bg.webp", format="WEBP", quality=88, method=6)
    return jump, where


def main() -> int:
    seam = load_module(SEAM, "seam")
    crop = load_module(FIGURE_CROP, "crop")
    failed = 0
    for slug in SLUGS:
        crop.process_slug(slug, seam)
        jump, where = process_mobile_bg(slug, seam)
        ok = jump <= MAX_LUM_JUMP
        print(f"{slug}-mobile-bg: max_lum_jump={jump} at {where} {'OK' if ok else 'FAIL'}")
        if not ok:
            failed += 1
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
