#!/usr/bin/env python3
"""Extract opaque mobile hero figures (people + foreground) for learn-grow overlay."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

PEARL = (247, 244, 240)
SRC = Path(__file__).resolve().parents[2] / "Jobsy.Web/wwwroot/images/candidate-illustrated/leren-groeien-hero.jpg"
OUT = SRC.parent / "leren-groeien-hero-figures-mobile.png"

# Scene layout in leren-groeien-hero.jpg (1280×720): people + desk cluster, no skyline band.
CROP_BOX = (268, 392, 1278, 718)


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
    """Dashed path + building strokes (keep saturated character art)."""
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    mx, mn = max(r, g, b), min(r, g, b)
    sat = (mx - mn) / mx if mx else 0.0
    return lum >= 150 and sat <= 0.12


def main() -> None:
    src = Image.open(SRC).convert("RGBA")
    region = src.crop(CROP_BOX)
    rw, rh = region.size
    px = region.load()
    for y in range(rh):
        for x in range(rw):
            r, g, b, _a = px[x, y]
            if is_background(r, g, b) or is_line_art(r, g, b):
                px[x, y] = (r, g, b, 0)
            else:
                px[x, y] = (r, g, b, 255)

    alpha = region.getchannel("A")
    bbox = alpha.getbbox()
    if not bbox:
        raise SystemExit("no foreground pixels in figure crop")

    pad = 2
    l = max(0, bbox[0] - pad)
    t = max(0, bbox[1] - pad)
    r = min(rw, bbox[2] + pad)
    b = min(rh, bbox[3] + pad)
    out = region.crop((l, t, r, b))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    out.save(OUT, optimize=True)
    print(f"wrote {OUT} size={out.size}")


if __name__ == "__main__":
    main()
