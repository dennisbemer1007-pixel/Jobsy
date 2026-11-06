#!/usr/bin/env python3
"""R12: mobile panel without graduation gown (stays off fade / no ghost)."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

PEARL = (247, 244, 240)
SRC = Path(__file__).resolve().parents[2] / "Jobsy.Web/wwwroot/images/candidate-illustrated/leren-groeien-hero.jpg"
OUT_DIR = SRC.parent
OUT_FIGURES = OUT_DIR / "leren-groeien-hero-figures-mobile.png"
OUT_ATMO = OUT_DIR / "leren-groeien-hero-mobile-atmosphere.png"

# Source layout: nurse ~168–288, gown ~288–412, student+woman ~412+.
NURSE_BOX = (168, 288, 240, 718)
DUO_BOX = (400, 286, 715, 718)  # student + laptop woman (after gown)
BOOKS_BOX = (998, 430, 1274, 718)
PANEL_W = 820
PANEL_H = 460
FEATHER = 110
FIGURES_PASTE_X = 138


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
                    px[x, y] = (r, g, b, 200)
                else:
                    px[x, y] = (r, g, b, 0)
    return rgba


def mask_band(rgba: Image.Image, start_frac: float, end_frac: float) -> None:
    w, h = rgba.size
    x0 = int(w * start_frac)
    x1 = int(w * end_frac)
    px = rgba.load()
    for x in range(x0, min(x1, w)):
        for y in range(h):
            px[x, y] = (0, 0, 0, 0)


def feather_left(rgba: Image.Image) -> Image.Image:
    w, h = rgba.size
    px = rgba.load()
    for x in range(min(FEATHER, w)):
        f = x / FEATHER
        for y in range(h):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, int(a * f))
    return rgba


def matte_crop_loose(src: Image.Image, box: tuple[int, int, int, int]) -> Image.Image:
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


def matte_crop(src: Image.Image, box: tuple[int, int, int, int]) -> Image.Image:
    return matte_crop_loose(src, box)


def prepare_people_strip(src: Image.Image) -> Image.Image:
    nurse = matte_crop_loose(src, NURSE_BOX)
    duo = matte_crop_loose(src, DUO_BOX)
    gap = 12
    w = nurse.width + gap + duo.width
    h = max(nurse.height, duo.height)
    strip = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    strip.paste(nurse, (0, h - nurse.height), nurse)
    strip.paste(duo, (nurse.width + gap, h - duo.height), duo)
    bbox = strip.getchannel("A").getbbox()
    return strip.crop(bbox) if bbox else strip


def build_people_layer(src: Image.Image) -> Image.Image:
    people = prepare_people_strip(src)
    books = src.crop(BOOKS_BOX).convert("RGBA")
    books = matting(books, keep_background=False)
    books = books.resize((int(books.width * 0.55), int(books.height * 0.55)), Image.Resampling.LANCZOS)

    canvas = Image.new("RGBA", (PANEL_W, PANEL_H), (0, 0, 0, 0))
    people_h = PANEL_H - 8
    people_w = int(people.width * (people_h / people.height))
    people = people.resize((people_w, people_h), Image.Resampling.LANCZOS)
    paste_x = FIGURES_PASTE_X
    canvas.paste(people, (paste_x, PANEL_H - people_h), people)
    bx = min(paste_x + people_w - 24, PANEL_W - books.width - 4)
    by = PANEL_H - books.height
    canvas.paste(books, (bx, by), books)
    return canvas


def build_atmosphere_layer(src: Image.Image) -> Image.Image:
    strip = prepare_people_strip(src)
    people = strip.resize(
        (int(strip.width * (PANEL_H / strip.height)), PANEL_H),
        Image.Resampling.LANCZOS,
    )
    panel = Image.new("RGBA", (PANEL_W, PANEL_H), (PEARL[0], PEARL[1], PEARL[2], 0))
    panel.paste(people, (FIGURES_PASTE_X, 0), people)
    people = matting(panel, keep_background=True)
    px = people.load()
    w, h = people.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a and is_line_art(r, g, b):
                px[x, y] = (PEARL[0], PEARL[1], PEARL[2], 0)
    return feather_left(people)


def main() -> None:
    src = Image.open(SRC).convert("RGB")
    fig = build_people_layer(src)
    atmo = build_atmosphere_layer(src)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    fig.save(OUT_FIGURES, optimize=True)
    atmo.save(OUT_ATMO, optimize=True)
    print(f"wrote {OUT_FIGURES} size={fig.size}")
    print(f"wrote {OUT_ATMO} size={atmo.size}")


if __name__ == "__main__":
    main()
