#!/usr/bin/env python3
"""Paste a tiny watercolor lobster onto approved onboarding-rustig hero JPEGs."""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
PAN = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama"

DESK_H_FRAC = 0.045
MOB_H_FRAC = 0.045

PLACEMENTS: dict[str, tuple[tuple[float, float], tuple[float, float]]] = {
    "start": ((0.11, 0.86), (0.18, 0.84)),
    "step-01": ((0.34, 0.88), (0.38, 0.86)),
    "step-02": ((0.56, 0.84), (0.54, 0.82)),
    "step-03": ((0.07, 0.74), (0.12, 0.72)),
    "step-04": ((0.26, 0.86), (0.32, 0.84)),
    "step-05": ((0.52, 0.86), (0.50, 0.84)),
}


def make_micro_lobster(height: int) -> Image.Image:
    """Soft red watercolor micro-mascot (~4–8% of hero band when placed)."""
    w = max(8, int(height * 1.15))
    h = max(10, height)
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    body = (198, 58, 52, 210)
    claw = (185, 48, 44, 190)
    # Body
    draw.ellipse((w * 0.22, h * 0.18, w * 0.78, h * 0.88), fill=body)
    # Tail
    draw.polygon(
        [(w * 0.5, h * 0.88), (w * 0.38, h * 0.98), (w * 0.62, h * 0.98)],
        fill=body,
    )
    # Claws
    draw.ellipse((w * 0.02, h * 0.42, w * 0.28, h * 0.62), fill=claw)
    draw.ellipse((w * 0.72, h * 0.42, w * 0.98, h * 0.62), fill=claw)
    # Eyes
    draw.ellipse((w * 0.38, h * 0.28, w * 0.46, h * 0.38), fill=(40, 30, 28, 200))
    draw.ellipse((w * 0.54, h * 0.28, w * 0.62, h * 0.38), fill=(40, 30, 28, 200))
    im = im.filter(ImageFilter.GaussianBlur(radius=0.35))
    return im


def paste_sprite(base: Image.Image, sprite: Image.Image, cx_frac: float, cy_frac: float) -> None:
    w, h = base.size
    cx, cy = int(w * cx_frac), int(h * cy_frac)
    x0 = cx - sprite.size[0] // 2
    y0 = cy - sprite.size[1] // 2
    base.paste(sprite, (x0, y0), sprite)


def main() -> None:
    slugs = sys.argv[1:] or list(PLACEMENTS.keys())
    for slug in slugs:
        desk_xy, mob_xy = PLACEMENTS[slug]
        for variant, xy, hf in (("desktop", desk_xy, DESK_H_FRAC), ("mobile", mob_xy, MOB_H_FRAC)):
            path = PAN / f"{slug}-{variant}.jpg"
            if not path.is_file():
                raise SystemExit(f"missing {path}")
            im = Image.open(path).convert("RGB")
            target_h = max(10, int(im.size[1] * hf))
            sprite = make_micro_lobster(target_h)
            paste_sprite(im, sprite, xy[0], xy[1])
            im.save(path, quality=92, optimize=True)
            print(f"{path.name}: lobster ~{target_h}px tall @ {xy}")


if __name__ == "__main__":
    main()
