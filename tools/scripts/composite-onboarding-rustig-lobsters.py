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

import importlib.util

_layout_path = Path(__file__).resolve().parent / "onboarding_rustig_lobster_layout.py"
_spec = importlib.util.spec_from_file_location("lob_layout", _layout_path)
lob_layout = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(lob_layout)

DESK_BAND_PX = lob_layout.DESK_BAND_PX
MOB_BAND_PX = lob_layout.MOB_BAND_PX
DESK_LOBSTER_PX = lob_layout.DESK_LOBSTER_PX
MOB_LOBSTER_PX = lob_layout.MOB_LOBSTER_PX
Y_MIN = lob_layout.Y_MIN
Y_MAX = lob_layout.Y_MAX
PLACEMENTS = lob_layout.PLACEMENTS
LOBSTER_PX_SLUG = lob_layout.LOBSTER_PX_SLUG
# Dennis: no mascot composite on these slugs (illustration only).
NO_LOBSTER_SLUGS: frozenset[str] = frozenset({"step-02", "step-04", "step-05", "step-06", "step-07"})


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


def scaled_sprite(
    base: Image.Image, band_px: int, lobster_px: int, sprite: Image.Image, *, slug: str = "", variant: str = "desktop"
) -> Image.Image:
    if slug and slug in LOBSTER_PX_SLUG:
        lobster_px = LOBSTER_PX_SLUG[slug][0 if variant == "desktop" else 1]
    jpeg_h = base.size[1]
    target_h = max(10, int(lobster_px * (jpeg_h / band_px)))
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
        if slug in NO_LOBSTER_SLUGS:
            print(f"skip {slug}: NO_LOBSTER_SLUGS")
            continue
        desk_xy, mob_xy = PLACEMENTS[slug]
        for variant, band, lob in variants:
            xy = mob_xy if variant == "mobile" else desk_xy
            path = PAN / f"{slug}-{variant}.jpg"
            im = Image.open(path).convert("RGB")
            piece = scaled_sprite(im, band, lob, sprite, slug=slug, variant=variant)
            paste_sprite(im, piece, xy[0], xy[1], mobile=(variant == "mobile"))
            im.save(path, quality=92, optimize=True)
            print(f"{path.name}: ~{piece.size[1]}px on JPEG, center {xy}")


if __name__ == "__main__":
    main()
