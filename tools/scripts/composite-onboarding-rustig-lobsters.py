#!/usr/bin/env python3
"""Composite the shared Lobsy watercolor lobster sprite onto approved hero JPEGs."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
PAN = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama"
DEFAULT_SPRITE = ROOT / "Jobsy.Web/wwwroot/images/onboarding/lobsy-mascot-sprite.png"

# Rendered band height (CSS): desktop 170px, mobile 130px @2x JPEG doubles.
DESK_BAND_PX = 170
MOB_BAND_PX = 130
DESK_LOBSTER_PX = 30  # ~18% of 170px band (24–32 target)
MOB_LOBSTER_PX = 17  # ~13% of 130px band (14–18 target)

# Safe vertical band on JPEG: below top 10%, above bottom fade (~22%).
Y_MIN = 0.12
Y_MAX = 0.78

# (x_center_frac, y_center_frac) — quiet props, not faces.
PLACEMENTS: dict[str, tuple[tuple[float, float], tuple[float, float]]] = {
    "start": ((0.23, 0.58), (0.26, 0.55)),
    "step-01": ((0.21, 0.62), (0.24, 0.60)),
    "step-02": ((0.52, 0.60), (0.50, 0.58)),
    "step-03": ((0.09, 0.50), (0.14, 0.48)),
    "step-04": ((0.19, 0.56), (0.22, 0.54)),
    "step-05": ((0.28, 0.52), (0.30, 0.50)),
}


def load_sprite(path: Path) -> Image.Image:
    im = Image.open(path).convert("RGBA")
    arr = np.asarray(im).copy()
    r, g, b, a = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2], arr[:, :, 3]
    lum = (r.astype(np.int16) + g.astype(np.int16) + b.astype(np.int16)) // 3
    neutral = (np.abs(r.astype(np.int16) - g.astype(np.int16)) < 18) & (
        np.abs(g.astype(np.int16) - b.astype(np.int16)) < 18
    )
    bg = neutral & (lum > 195)
    a = np.where(bg, 0, np.maximum(a, 255 - bg.astype(np.uint8) * 255))
    arr[:, :, 3] = a
    im = Image.fromarray(arr)
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    return im


def scaled_sprite(base: Image.Image, band_px: int, lobster_px: int, sprite: Image.Image) -> Image.Image:
    jpeg_h = base.size[1]
    target_h = max(12, int(lobster_px * (jpeg_h / band_px)))
    w = int(sprite.size[0] * (target_h / sprite.size[1]))
    return sprite.resize((w, target_h), Image.Resampling.LANCZOS)


def clamp_y(cy_frac: float) -> float:
    return max(Y_MIN, min(Y_MAX, cy_frac))


def paste_sprite(base: Image.Image, piece: Image.Image, cx_frac: float, cy_frac: float) -> None:
    w, h = base.size
    cy_frac = clamp_y(cy_frac)
    cx, cy = int(w * cx_frac), int(h * cy_frac)
    x0 = cx - piece.size[0] // 2
    y0 = cy - piece.size[1] // 2
    rgb = base.convert("RGBA")
    rgb.paste(piece, (x0, y0), piece)
    base.paste(rgb.convert("RGB"))


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SPRITE
    if not src.is_file():
        raise SystemExit(f"missing sprite: {src}")
    sprite = load_sprite(src)
    sprite.save(DEFAULT_SPRITE)
    slugs = sys.argv[2:] if len(sys.argv) > 2 else list(PLACEMENTS.keys())
    for slug in slugs:
        desk_xy, mob_xy = PLACEMENTS[slug]
        for variant, xy, band, lob in (
            ("desktop", desk_xy, DESK_BAND_PX, DESK_LOBSTER_PX),
            ("mobile", mob_xy, MOB_BAND_PX, MOB_LOBSTER_PX),
        ):
            path = PAN / f"{slug}-{variant}.jpg"
            im = Image.open(path).convert("RGB")
            piece = scaled_sprite(im, band, lob, sprite)
            paste_sprite(im, piece, xy[0], xy[1])
            im.save(path, quality=92, optimize=True)
            print(f"{path.name}: ~{piece.size[1]}px on JPEG, center {xy}")


if __name__ == "__main__":
    main()
