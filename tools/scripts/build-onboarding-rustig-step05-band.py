#!/usr/bin/env python3
"""Build step-05 band-native raw from a seamless wide source (no side patching)."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "artifacts/onboarding-rustig-panorama-raw"
DEFAULT_SRC = RAW / "step-05-source-16x9-u6y-seamless.jpg"
OUT = RAW / "step-05-desktop-raw.jpg"
OUT_SIZE = (2400, 400)
# Keep graduate feet + grass above the cream fade (step-05 u6y1).
SLICE_Y0 = 0.06
SLICE_Y1 = 0.89
BAND_SCALE_H = 0.74
FADE_BOTTOM = 10


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


def load_step04_helpers():
    spec = importlib.util.spec_from_file_location(
        "step04_band", ROOT / "tools/scripts/build-onboarding-rustig-step04-band.py"
    )
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


def pick_slice(im: Image.Image) -> Image.Image:
    w, h = im.size
    y0 = int(h * SLICE_Y0)
    y_end = int(h * SLICE_Y1)
    return im.crop((0, y0, w, y_end))


def fit_band_scaled(slice_im: Image.Image, fin) -> Image.Image:
    tw, th = OUT_SIZE
    scaled_h = max(1, int(th * BAND_SCALE_H))
    scaled = slice_im.resize((tw, scaled_h), Image.Resampling.LANCZOS)
    cream = tuple(int(c) for c in fin.CREAM)
    canvas = Image.new("RGB", OUT_SIZE, cream)
    canvas.paste(scaled, (0, 0))
    return canvas


def apply_bottom_fade(im: Image.Image, fin) -> Image.Image:
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    cream = tuple(int(c) for c in fin.CREAM)
    for y in range(h):
        if y < h - FADE_BOTTOM:
            continue
        for x in range(w):
            t = (y - (h - FADE_BOTTOM)) / max(1, FADE_BOTTOM - 1)
            a = int(255 * min(1.0, t**0.85))
            if a:
                px[x, y] = (*cream, a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    if not src.is_file():
        raise SystemExit(f"missing source: {src}")
    fin = load_fin()
    s4 = load_step04_helpers()
    im = s4.heal_topright_sky(Image.open(src).convert("RGB"))
    slice_im = pick_slice(im)
    band = fit_band_scaled(slice_im, fin)
    band = fin.apply_cream_fades(band)
    band = s4.defuse_right_foliage_red(band)
    band = apply_bottom_fade(band, fin)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    band.save(OUT, quality=98, optimize=True)
    band = s4.defuse_right_foliage_red(Image.open(OUT))
    band.save(OUT, quality=98, optimize=True)
    bb = fin.content_bbox(band)
    print(f"wrote {OUT} content_bbox={bb} from {src.name}")


if __name__ == "__main__":
    main()
