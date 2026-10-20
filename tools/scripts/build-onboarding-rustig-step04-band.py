#!/usr/bin/env python3
"""Build step-04 band-native raw from a seamless wide source (no side patching)."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "artifacts/onboarding-rustig-panorama-raw"
DEFAULT_SRC = RAW / "step-04-source-16x9-u6x-seamless.jpg"
OUT = RAW / "step-04-desktop-raw.jpg"
OUT_SIZE = (2400, 400)
HEAD_MIN_FRAC = 0.16
FADE_BOTTOM = 36


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


def defuse_right_foliage_red(im: Image.Image) -> Image.Image:
    """Pull sunset-red out of the top-right sky band (report check); skip figures/vests below."""
    rgb = np.asarray(im.convert("RGB"))
    h, w = rgb.shape[:2]
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    roi = np.zeros((h, w), dtype=bool)
    roi[: int(h * 0.58), int(w * 0.90) :] = True
    warm = roi & (r > g + 10)
    if not warm.any():
        return im
    out = rgb.copy()
    gfix = out[warm, 1].astype(np.int16)
    out[warm, 0] = np.minimum(out[warm, 0], (gfix + 6).astype(np.uint8))
    return Image.fromarray(out)


def apply_strong_bottom_fade(im: Image.Image, fin) -> Image.Image:
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


def pick_slice(im: Image.Image, fin) -> Image.Image:
    """Fixed vertical window: figures sit in the middle band (YOLO bbox skips heads)."""
    w, h = im.size
    y0 = int(h * 0.11)
    y_end = int(h * 0.79)
    return im.crop((0, y0, w, y_end))


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    if not src.is_file():
        raise SystemExit(f"missing source: {src}")
    fin = load_fin()
    im = Image.open(src).convert("RGB")
    slice_im = pick_slice(im, fin)
    band = slice_im.resize(OUT_SIZE, Image.Resampling.LANCZOS)
    band = fin.apply_cream_fades(band)
    band = defuse_right_foliage_red(band)
    band = apply_strong_bottom_fade(band, fin)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    band.save(OUT, quality=95, optimize=True)
    bb = fin.content_bbox(band)
    print(f"wrote {OUT} content_bbox={bb} from {src.name}")


if __name__ == "__main__":
    main()
