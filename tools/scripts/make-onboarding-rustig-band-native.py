#!/usr/bin/env python3
"""Extract a 6:1 band-native JPEG from a 16:9 raw (same vertical slice as approved step-03)."""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image

Y0_FRAC = 0.456
OUT_SIZE = (2400, 400)


def band_from_16x9(im: Image.Image, y0_frac: float = Y0_FRAC) -> Image.Image:
    w, h = im.size
    crop_h = int(w / 6.0)
    y0 = max(0, min(int(h * y0_frac), h - crop_h))
    band = im.crop((0, y0, w, y0 + crop_h))
    return band.resize(OUT_SIZE, Image.Resampling.LANCZOS)


def main() -> None:
    if len(sys.argv) < 3:
        raise SystemExit(
            "usage: make-onboarding-rustig-band-native.py <16x9-in.jpg> <6x1-out.jpg> [y0_frac]"
        )
    src, dst = Path(sys.argv[1]), Path(sys.argv[2])
    y0_frac = float(sys.argv[3]) if len(sys.argv) > 3 else Y0_FRAC
    im = Image.open(src).convert("RGB")
    out = band_from_16x9(im, y0_frac=y0_frac)
    dst.parent.mkdir(parents=True, exist_ok=True)
    out.save(dst, quality=95, optimize=True)
    print(f"wrote {dst} ({out.size[0]}x{out.size[1]}) from {src}")


if __name__ == "__main__":
    main()
