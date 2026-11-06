#!/usr/bin/env python3
"""R11 mobile learn-grow: people-forward panel + soft atmosphere (no hard seam)."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

PEARL = (247, 244, 240)
SRC = Path(__file__).resolve().parents[2] / "Jobsy.Web/wwwroot/images/candidate-illustrated/leren-groeien-hero.jpg"
OUT_DIR = SRC.parent
OUT_FIGURES = OUT_DIR / "leren-groeien-hero-figures-mobile.png"
OUT_ATMO = OUT_DIR / "leren-groeien-hero-mobile-atmosphere.png"

# Nurse → laptop woman (+ small book stack on the right).
PEOPLE_BOX = (248, 286, 1010, 718)
BOOKS_BOX = (998, 430, 1274, 718)
PANEL_W = 820
PANEL_H = 460
FEATHER = 110


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


def feather_left(rgba: Image.Image) -> Image.Image:
    w, h = rgba.size
    px = rgba.load()
    for x in range(min(FEATHER, w)):
        f = x / FEATHER
        for y in range(h):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, int(a * f))
    return rgba


def build_people_layer(src: Image.Image) -> Image.Image:
    people = src.crop(PEOPLE_BOX).convert("RGBA")
    people = matting(people, keep_background=False)
    alpha = people.getchannel("A")
    bbox = alpha.getbbox()
    if not bbox:
        raise SystemExit("no people pixels")
    people = people.crop(bbox)

    # Mask worker on far left of people strip.
    pw, ph = people.size
    mask_w = int(pw * 0.08)
    px = people.load()
    for x in range(mask_w):
        for y in range(ph):
            px[x, y] = (0, 0, 0, 0)

    books = src.crop(BOOKS_BOX).convert("RGBA")
    books = matting(books, keep_background=False)
    books = books.resize((int(books.width * 0.55), int(books.height * 0.55)), Image.Resampling.LANCZOS)

    canvas = Image.new("RGBA", (PANEL_W, PANEL_H), (0, 0, 0, 0))
    people_h = PANEL_H - 8
    people_w = int(people.width * (people_h / people.height))
    people = people.resize((people_w, people_h), Image.Resampling.LANCZOS)
    canvas.paste(people, (0, PANEL_H - people_h), people)
    bx = min(people_w - 24, PANEL_W - books.width - 4)
    by = PANEL_H - books.height
    canvas.paste(books, (bx, by), books)
    return canvas


def build_atmosphere_layer(src: Image.Image) -> Image.Image:
    panel = src.crop(PEOPLE_BOX).convert("RGBA")
    panel = matting(panel, keep_background=True)
    # Drop skyline strokes entirely.
    px = panel.load()
    w, h = panel.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a and is_line_art(r, g, b):
                px[x, y] = (PEARL[0], PEARL[1], PEARL[2], 0)
    panel = panel.resize((PANEL_W, PANEL_H), Image.Resampling.LANCZOS)
    return feather_left(panel)


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
