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

DESK_BAND_PX = 170
MOB_BAND_PX = 92
DESK_LOBSTER_PX = 30
MOB_LOBSTER_PX = 15

Y_MIN = 0.12
Y_MAX = 0.78

PLACEMENTS: dict[str, tuple[tuple[float, float], tuple[float, float]]] = {
    "start": ((0.23, 0.58), (0.26, 0.55)),
    "step-01": ((0.21, 0.62), (0.24, 0.60)),
    "step-02": ((0.70, 0.76), (0.88, 0.54)),
    "step-03": ((0.09, 0.50), (0.16, 0.52)),
    "step-04": ((0.78, 0.66), (0.85, 0.66)),
    "step-05": ((0.17, 0.74), (0.17, 0.74)),
    "step-06": ((0.72, 0.58), (0.68, 0.55)),
    "step-07": ((0.38, 0.60), (0.35, 0.57)),
    "step-08": ((0.14, 0.68), (0.15, 0.64)),
    "step-09": ((0.74, 0.56), (0.70, 0.54)),
    "step-10": ((0.22, 0.58), (0.24, 0.55)),
    "step-11": ((0.66, 0.57), (0.62, 0.54)),
    "kompas": ((0.48, 0.60), (0.50, 0.57)),
}


def load_sprite(path: Path) -> Image.Image:
    im = Image.open(path).convert("RGBA")
    arr = np.asarray(im).copy()
    r, g, b, a = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2], arr[:, :, 3]
    lum = (r.astype(np.int16) + g.astype(np.int16) + b.astype(np.int16)) // 3
    neutral = (np.abs(r.astype(np.int16) - g.astype(np.int16)) < 20) & (
        np.abs(g.astype(np.int16) - b.astype(np.int16)) < 20
    )
    # Remove white/light and black/dark mattes; keep existing alpha when already clear.
    matte = (neutral & (lum > 235)) | (neutral & (lum < 22))
    a = np.where(matte, 0, a)
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


def paste_sprite(
    base: Image.Image, piece: Image.Image, cx_frac: float, cy_frac: float, *, mobile: bool = False
) -> None:
    w, h = base.size
    cy_frac = clamp_y(cy_frac)
    cx, cy = int(w * cx_frac), int(h * cy_frac)
    x0 = cx - piece.size[0] // 2
    y0 = cy - piece.size[1] // 2
    canvas = base.convert("RGBA")
    overlay = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    overlay.paste(piece, (x0, y0), piece.split()[3])
    merged = Image.alpha_composite(canvas, overlay)
    base.paste(merged.convert("RGB"))


def main() -> None:
    argv = sys.argv[1:]
    mobile_only = "--mobile-only" in argv
    desktop_only = "--desktop-only" in argv
    argv = [a for a in argv if a not in ("--mobile-only", "--desktop-only")]
    src = DEFAULT_SPRITE
    slug_args: list[str] = []
    for a in argv:
        if a.endswith(".png") or a.endswith(".jpg"):
            src = Path(a)
        else:
            slug_args.append(a)
    if not src.is_file():
        raise SystemExit(f"missing sprite: {src}")
    sprite = load_sprite(src)
    slugs = slug_args if slug_args else list(PLACEMENTS.keys())
    variants = (
        [("mobile", MOB_BAND_PX, MOB_LOBSTER_PX)]
        if mobile_only
        else [("desktop", DESK_BAND_PX, DESK_LOBSTER_PX)]
        if desktop_only
        else [("desktop", DESK_BAND_PX, DESK_LOBSTER_PX), ("mobile", MOB_BAND_PX, MOB_LOBSTER_PX)]
    )
    for slug in slugs:
        desk_xy, mob_xy = PLACEMENTS[slug]
        for variant, band, lob in variants:
            xy = mob_xy if variant == "mobile" else desk_xy
            path = PAN / f"{slug}-{variant}.jpg"
            im = Image.open(path).convert("RGB")
            piece = scaled_sprite(im, band, lob, sprite)
            paste_sprite(im, piece, xy[0], xy[1], mobile=(variant == "mobile"))
            im.save(path, quality=92, optimize=True)
            print(f"{path.name}: ~{piece.size[1]}px on JPEG, center {xy}")


if __name__ == "__main__":
    main()
