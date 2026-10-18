#!/usr/bin/env python3
"""R10 mobile learn-grow: atmosphere strip + opaque 3-person (+ books) figure crop."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

PEARL = (247, 244, 240)
SRC = Path(__file__).resolve().parents[2] / "Jobsy.Web/wwwroot/images/candidate-illustrated/leren-groeien-hero.jpg"
OUT_DIR = SRC.parent
OUT_FIGURES = OUT_DIR / "leren-groeien-hero-figures-mobile.png"
OUT_ATMO = OUT_DIR / "leren-groeien-hero-mobile-atmosphere.png"

# Nurse, graduate, laptop woman + book stack (skip leftmost worker / dashed skyline).
CROP_BOX = (308, 278, 1276, 718)
LEFT_PAD = 56
FEATHER = 56
WORKER_MASK_FRAC = 0.13


def is_background(r: int, g: int, b: int) -> bool:
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    mx, mn = max(r, g, b), min(r, g, b)
    sat = (mx - mn) / mx if mx else 0.0
    if lum >= 188 and sat <= 0.22:
        return True
    if abs(r - PEARL[0]) <= 10 and abs(g - PEARL[1]) <= 10 and abs(b - PEARL[2]) <= 12:
        return True
    return False


def is_line_art(r: int, g: int, b: int) -> bool:
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    mx, mn = max(r, g, b), min(r, g, b)
    sat = (mx - mn) / mx if mx else 0.0
    return lum >= 148 and sat <= 0.11


def feather_left(rgba: Image.Image) -> Image.Image:
    w, h = rgba.size
    px = rgba.load()
    for x in range(min(FEATHER, w)):
        f = x / FEATHER
        for y in range(h):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, int(a * f))
    return rgba


def pad_left(rgba: Image.Image, pad: int) -> Image.Image:
    if pad <= 0:
        return rgba
    out = Image.new("RGBA", (rgba.width + pad, rgba.height), (PEARL[0], PEARL[1], PEARL[2], 0))
    out.paste(rgba, (pad, 0))
    return out


def build_atmosphere(scene: Image.Image) -> Image.Image:
    return feather_left(scene.convert("RGBA"))


def build_figures(scene: Image.Image) -> Image.Image:
    rw, rh = scene.size
    px = scene.convert("RGBA")
    pl = px.load()
    for y in range(rh):
        for x in range(rw):
            r, g, b = scene.getpixel((x, y))[:3]
            if is_background(r, g, b) or is_line_art(r, g, b):
                pl[x, y] = (r, g, b, 0)
            else:
                pl[x, y] = (r, g, b, 255)

    alpha = px.getchannel("A")
    bbox = alpha.getbbox()
    if not bbox:
        raise SystemExit("no foreground in figure crop")
    pad = 4
    l = max(0, bbox[0] - pad)
    t = max(0, bbox[1] - pad)
    r = min(rw, bbox[2] + pad)
    b = min(rh, bbox[3] + pad)
    fig = px.crop((l, t, r, b))
    fw, fh = fig.size
    mask_w = int(fw * WORKER_MASK_FRAC)
    fpx = fig.load()
    for x in range(min(mask_w, fw)):
        for y in range(fh):
            fpx[x, y] = (0, 0, 0, 0)
    fig = pad_left(fig, LEFT_PAD)
    return fig


def main() -> None:
    src = Image.open(SRC).convert("RGB")
    scene = src.crop(CROP_BOX)
    atmo = build_atmosphere(scene)
    fig = build_figures(scene)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    atmo.save(OUT_ATMO, optimize=True)
    fig.save(OUT_FIGURES, optimize=True)
    print(f"wrote {OUT_ATMO} size={atmo.size}")
    print(f"wrote {OUT_FIGURES} size={fig.size}")


if __name__ == "__main__":
    main()
