#!/usr/bin/env python3
"""Mobile tips hero: atmosphere (desk/lamp) + figures from beschikbaarheid-profession-wide."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

PEARL = (247, 244, 240)
SRC = (
    Path(__file__).resolve().parents[2]
    / "Jobsy.Web/wwwroot/images/candidate-illustrated/beschikbaarheid-profession-wide.jpg"
)
OUT_DIR = SRC.parent
OUT_FIGURES = OUT_DIR / "tips-moment-hero-figures-mobile.png"
OUT_ATMO = OUT_DIR / "tips-moment-hero-mobile-atmosphere.png"

PANEL_W = 820
PANEL_H = 460
FEATHER = 120
FIGURES_PASTE_X = 96

# People cluster (left of wide art); props on the right.
FIGURES_BOX = (24, 96, 640, 718)
PROPS_BOX = (1180, 0, 2560, 718)


def is_background(r: int, g: int, b: int) -> bool:
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    mx, mn = max(r, g, b), min(r, g, b)
    sat = (mx - mn) / mx if mx else 0.0
    if lum >= 188 and sat <= 0.22:
        return True
    if abs(r - PEARL[0]) <= 12 and abs(g - PEARL[1]) <= 12 and abs(b - PEARL[2]) <= 14:
        return True
    return False


def is_line_art(r: int, g: int, b: int) -> bool:
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    mx, mn = max(r, g, b), min(r, g, b)
    sat = (mx - mn) / mx if mx else 0.0
    return lum >= 145 and sat <= 0.11


def matting(rgba: Image.Image, *, keep_background: bool) -> Image.Image:
    px = rgba.load()
    w, h = rgba.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if is_line_art(r, g, b):
                px[x, y] = (PEARL[0], PEARL[1], PEARL[2], 0)
            elif is_background(r, g, b):
                if keep_background:
                    px[x, y] = (r, g, b, 210)
                else:
                    px[x, y] = (r, g, b, 0)
    return rgba


def feather_left(rgba: Image.Image) -> Image.Image:
    w, h = rgba.size
    px = rgba.load()
    for x in range(min(FEATHER, w)):
        f = x / FEATHER
        for y in range(h):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, int(a * f))
    return rgba


def matte_crop(src: Image.Image, box: tuple[int, int, int, int]) -> Image.Image:
    layer = src.crop(box).convert("RGBA")
    px = layer.load()
    w, h = layer.size
    for y in range(h):
        for x in range(w):
            r, g, b, _a = px[x, y]
            if is_background(r, g, b) or is_line_art(r, g, b):
                px[x, y] = (0, 0, 0, 0)
    bbox = layer.getchannel("A").getbbox()
    if not bbox:
        raise SystemExit(f"empty crop {box}")
    return layer.crop(bbox)


def build_figures_layer(src: Image.Image) -> Image.Image:
    people = matte_crop(src, FIGURES_BOX)
    canvas = Image.new("RGBA", (PANEL_W, PANEL_H), (0, 0, 0, 0))
    target_h = PANEL_H - 12
    target_w = int(people.width * (target_h / people.height))
    people = people.resize((target_w, target_h), Image.Resampling.LANCZOS)
    canvas.paste(people, (FIGURES_PASTE_X, PANEL_H - target_h), people)
    return canvas


def build_atmosphere_layer(src: Image.Image) -> Image.Image:
    props = src.crop(PROPS_BOX).convert("RGBA")
    props = matting(props, keep_background=True)
    canvas = Image.new("RGBA", (PANEL_W, PANEL_H), (PEARL[0], PEARL[1], PEARL[2], 0))
    scale = min(PANEL_W / props.width, PANEL_H / props.height)
    pw = max(1, int(props.width * scale))
    ph = max(1, int(props.height * scale))
    props = props.resize((pw, ph), Image.Resampling.LANCZOS)
    canvas.paste(props, (PANEL_W - pw, PANEL_H - ph), props)
    return feather_left(canvas)


def main() -> None:
    src = Image.open(SRC).convert("RGB")
    fig = build_figures_layer(src)
    atmo = build_atmosphere_layer(src)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    fig.save(OUT_FIGURES, optimize=True)
    atmo.save(OUT_ATMO, optimize=True)
    print(f"wrote {OUT_FIGURES} size={fig.size}")
    print(f"wrote {OUT_ATMO} size={atmo.size}")


if __name__ == "__main__":
    main()
