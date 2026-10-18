#!/usr/bin/env python3
"""R15: full-bleed mobile hero plates (one continuous scene, cover crop, no cut-out figures)."""
from __future__ import annotations

import math
import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "Jobsy.Web/wwwroot/images/onboarding"
DIV_DIR = ROOT / "Jobsy.Web/wwwroot/images/diversiteit"
SLUGS = ("kennismaken", "beschikbaarheid", "vervoer", "werkervaring", "opleiding", "droombaan")

MOBILE_W = 780
MOBILE_H = 520
PAGE_BG = (245, 242, 238)

# Source crop (fraction L,T,R,B) before cover-scale to MOBILE_W×MOBILE_H.
# Tuned so figures sit bottom-right with headroom; left keeps sky/path for text gradient.
CROP: dict[str, tuple[float, float, float, float]] = {
    "kennismaken": (0.0, 0.0, 0.95, 1.0),
    "beschikbaarheid": (0.0, 0.0, 0.78, 1.0),
    "vervoer": (0.22, 0.0, 1.0, 1.0),
    "werkervaring": (0.0, 0.0, 0.72, 1.0),
    "opleiding": (0.0, 0.0, 0.80, 1.0),
    "droombaan": (0.0, 0.0, 0.72, 1.0),
}

# Flip so figures that live on the left in desktop art read on the right on mobile.
FLIP_H = frozenset({"beschikbaarheid", "opleiding", "droombaan", "kennismaken"})


def rgb_dist(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def crop_frac(img: Image.Image, box: tuple[float, float, float, float]) -> Image.Image:
    w, h = img.size
    l, t, r, b = box
    return img.crop((int(w * l), int(h * t), int(w * r), int(h * b)))


def cover_to(img: Image.Image, tw: int, th: int, *, anchor_x: float = 0.72, anchor_y: float = 0.55) -> Image.Image:
    w, h = img.size
    scale = max(tw / w, th / h)
    nw, nh = int(w * scale + 0.5), int(h * scale + 0.5)
    scaled = img.resize((nw, nh), Image.Resampling.LANCZOS)
    x0 = int((nw - tw) * anchor_x)
    y0 = int((nh - th) * anchor_y)
    x0 = max(0, min(nw - tw, x0))
    y0 = max(0, min(nh - th, y0))
    return scaled.crop((x0, y0, x0 + tw, y0 + th))


def figure_alpha_from_jpg(path: Path) -> Image.Image:
    """Rough cream-key alpha for diversiteit portrait plates."""
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    px = im.load()
    alpha = Image.new("L", (w, h), 0)
    apx = alpha.load()
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            if r > 210 and g > 200 and b > 185 and (r - b) < 55:
                continue
            if r + g + b > 620:
                continue
            apx[x, y] = 255
    alpha = alpha.filter(ImageFilter.MaxFilter(3))
    alpha = alpha.filter(ImageFilter.GaussianBlur(1.2))
    im.putalpha(alpha)
    return im


def build_werkervaring_scene() -> Image.Image:
    base = Image.open(OUT_DIR / "werkervaring.jpg").convert("RGB")
    w, h = base.size
    left_scene = crop_frac(base, (0.0, 0.0, 0.72, 1.0))
    scene = cover_to(left_scene, w, h, anchor_x=0.0, anchor_y=0.5)

    log = figure_alpha_from_jpg(DIV_DIR / "figuur-logistiek.jpg")
    zorg = figure_alpha_from_jpg(DIV_DIR / "figuur-schoonmaak.jpg")

    def paste_figure(fig: Image.Image, x_frac: float, target_h_frac: float) -> None:
        tw = int(w * target_h_frac * (fig.width / fig.height))
        th = int(h * target_h_frac)
        fig_r = fig.resize((tw, th), Image.Resampling.LANCZOS)
        x = int(w * x_frac) - tw // 2
        y = h - th - int(h * 0.06)
        scene.paste(fig_r, (x, y), fig_r)

    paste_figure(log, 0.66, 0.56)
    paste_figure(zorg, 0.84, 0.52)
    return scene


def build_mobile_hero(slug: str) -> Image.Image:
    if slug == "werkervaring":
        src = build_werkervaring_scene()
    else:
        src = Image.open(OUT_DIR / f"{slug}.jpg").convert("RGB")
    patch = crop_frac(src, CROP[slug])
    if slug in FLIP_H:
        patch = patch.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    return cover_to(patch, MOBILE_W, MOBILE_H, anchor_x=0.78 if slug in FLIP_H else 0.68, anchor_y=0.52)


def write_webp(jpg: Path) -> None:
    webp = jpg.with_suffix(".webp")
    subprocess.run(
        ["cwebp", "-q", "86", str(jpg), "-o", str(webp)],
        check=True,
        capture_output=True,
    )


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    manifest: dict[str, dict[str, int]] = {}
    for slug in SLUGS:
        hero = build_mobile_hero(slug)
        jpg = OUT_DIR / f"{slug}-mobile-hero.jpg"
        hero.save(jpg, quality=90, optimize=True)
        if shutil.which("cwebp"):
            write_webp(jpg)
        manifest[slug] = {"width": MOBILE_W, "height": MOBILE_H}
        print("wrote", jpg)
    meta = OUT_DIR / "mobile-hero-r15-manifest.json"
    meta.write_text(__import__("json").dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
