#!/usr/bin/env python3
"""Rasterize upright Lobsy SVG to watercolor RGBA sprite with true transparency."""

from __future__ import annotations

from io import BytesIO
from pathlib import Path

import cairosvg
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
SVG = ROOT / "Jobsy.Web/wwwroot/brand/svg/lobsy-mascot-sprite-source.svg"
OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/lobsy-mascot-sprite.png"


def watercolorize(im: Image.Image) -> Image.Image:
    im = im.convert("RGBA")
    rgb = im.split()[0:3]
    base = Image.merge("RGB", rgb)
    # Soft watercolor bleed
    soft = base.filter(ImageFilter.GaussianBlur(radius=0.6))
    base = Image.blend(base, soft, 0.35)
    arr = np.asarray(base, dtype=np.float32)
    noise = np.random.default_rng(42).normal(0, 3.5, arr.shape)
    arr = np.clip(arr + noise, 0, 255).astype(np.uint8)
    base = Image.fromarray(arr)
    alpha = im.split()[3]
    alpha = alpha.filter(ImageFilter.GaussianBlur(radius=0.8))
    return Image.merge("RGBA", (*base.split(), alpha))


def assert_alpha_ok(im: Image.Image) -> None:
    assert im.mode == "RGBA", im.mode
    a = np.asarray(im.split()[3])
    corners = [a[0, 0], a[0, -1], a[-1, 0], a[-1, -1]]
    if any(c > 8 for c in corners):
        raise SystemExit(f"sprite corners not transparent: alpha={corners}")
    if (a > 10).sum() < 500:
        raise SystemExit("sprite has too few opaque pixels")


def main() -> None:
    png_bytes = cairosvg.svg2png(url=str(SVG), output_width=640, output_height=640)
    im = Image.open(BytesIO(png_bytes)).convert("RGBA")
    # Drop faint halos on pure white from rasterizer
    arr = np.asarray(im).copy()
    r, g, b, a = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2], arr[:, :, 3]
    lum = (r.astype(np.int16) + g.astype(np.int16) + b.astype(np.int16)) // 3
    near_white = (lum > 248) & (np.abs(r.astype(np.int16) - g.astype(np.int16)) < 6)
    a[near_white] = 0
    arr[:, :, 3] = a
    im = Image.fromarray(arr)
    bbox = im.getbbox()
    if bbox:
        pad = 8
        x0, y0, x1, y1 = bbox
        im = im.crop(
            (
                max(0, x0 - pad),
                max(0, y0 - pad),
                min(im.size[0], x1 + pad),
                min(im.size[1], y1 + pad),
            )
        )
    im = watercolorize(im)
    assert_alpha_ok(im)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    im.save(OUT, optimize=True)
    print(f"wrote {OUT} {im.size} mode={im.mode}")


if __name__ == "__main__":
    main()
