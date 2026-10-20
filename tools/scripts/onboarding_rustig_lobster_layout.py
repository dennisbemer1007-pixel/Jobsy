"""Shared lobster placement geometry for onboarding rustig heroes."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
PAN = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama"
SPRITE = ROOT / "Jobsy.Web/wwwroot/images/onboarding/lobsy-mascot-sprite.png"

DESK_BAND_PX = 170
MOB_BAND_PX = 92
DESK_LOBSTER_PX = 30
MOB_LOBSTER_PX = 15
LOBSTER_PX_SLUG: dict[str, tuple[int, int]] = {
    "step-02": (24, 12),
}

Y_MIN = 0.12
Y_MAX = 0.78
MOBILE_RATIO = 780 / 184

PLACEMENTS: dict[str, tuple[tuple[float, float], tuple[float, float]]] = {
    "start": ((0.23, 0.58), (0.26, 0.55)),
    "step-01": ((0.21, 0.62), (0.24, 0.60)),
    "step-02": ((0.955, 0.480), (0.955, 0.480)),
    "step-03": ((0.09, 0.50), (0.16, 0.52)),
    "step-04": ((0.980, 0.420), (0.820, 0.420)),
    "step-05": ((0.060, 0.420), (0.060, 0.420)),
    "step-06": ((0.650, 0.420), (0.770, 0.420)),
    "step-07": ((0.540, 0.600), (0.540, 0.600)),
    "step-08": ((0.080, 0.420), (0.080, 0.420)),
    "step-09": ((0.910, 0.420), (0.910, 0.420)),
    "step-10": ((0.150, 0.420), (0.150, 0.420)),
    "step-11": ((0.66, 0.57), (0.62, 0.54)),
    "kompas": ((0.600, 0.420), (0.600, 0.420)),
}


def clamp_y(cy_frac: float) -> float:
    return max(Y_MIN, min(Y_MAX, cy_frac))


def lobster_px(slug: str, variant: str) -> int:
    desk, mob = LOBSTER_PX_SLUG.get(slug, (DESK_LOBSTER_PX, MOB_LOBSTER_PX))
    return desk if variant == "desktop" else mob


def band_px(variant: str) -> int:
    return DESK_BAND_PX if variant == "desktop" else MOB_BAND_PX


def placement(slug: str, variant: str) -> tuple[float, float]:
    desk, mob = PLACEMENTS[slug]
    return desk if variant == "desktop" else mob


def _sprite_trimmed():
    im = Image.open(SPRITE).convert("RGBA")
    bbox = im.getbbox()
    return im.crop(bbox) if bbox else im


def lobster_bbox_pixels(
    im: Image.Image, slug: str, variant: str
) -> tuple[int, int, int, int]:
    w, h = im.size
    cx_frac, cy_frac = placement(slug, variant)
    cy_frac = clamp_y(cy_frac)
    cx, cy = int(w * cx_frac), int(h * cy_frac)
    sprite = _sprite_trimmed()
    lob = lobster_px(slug, variant)
    band = band_px(variant)
    piece_h = max(10, int(lob * (h / band)))
    piece_w = max(8, int(sprite.size[0] * (piece_h / sprite.size[1])))
    x0 = cx - piece_w // 2
    y0 = cy - piece_h // 2
    return x0, y0, x0 + piece_w, y0 + piece_h


def min_box_gap(
    a: tuple[int, int, int, int], b: tuple[int, int, int, int]
) -> float:
    ax1, ay1, ax2, ay2 = a
    bx1, by1, bx2, by2 = b
    dx = max(0, bx1 - ax2, ax1 - bx2)
    dy = max(0, by1 - ay2, ay1 - by2)
    if dx == 0:
        return float(dy)
    if dy == 0:
        return float(dx)
    return float((dx * dx + dy * dy) ** 0.5)


def band_frac_to_mobile_frac(fx: float, band_w: int = 2400, band_h: int = 400) -> float:
    crop_w = min(band_w, int(round(band_h * MOBILE_RATIO)))
    x0 = max(0, (band_w - crop_w) // 2)
    px = fx * band_w
    if px < x0 or px > x0 + crop_w:
        raise ValueError(f"band x={fx:.3f} outside mobile crop")
    return (px - x0) / crop_w
