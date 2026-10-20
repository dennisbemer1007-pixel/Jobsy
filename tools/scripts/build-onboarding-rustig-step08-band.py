#!/usr/bin/env python3
"""Build step-08 band-native raw from a seamless wide source (no side patching)."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "artifacts/onboarding-rustig-panorama-raw"
DEFAULT_SRC = RAW / "step-08-source-16x9-u8c-seamless.jpg"
OUT = RAW / "step-08-desktop-raw.jpg"
OUT_SIZE = (2400, 400)


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


def defuse_figure_red(im: Image.Image) -> Image.Image:
    """Neutralize red clothing (e.g. kite runner shirt) without touching sunset sky."""
    rgb = np.asarray(im.convert("RGB")).copy()
    h, w = rgb.shape[:2]
    for _ in range(16):
        r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
        hot = (r > 105) & (r > g + 14) & (r > b + 14) & (g < 150) & (b < 150)
        zone = np.zeros((h, w), dtype=bool)
        zone[int(h * 0.18) :, :] = True
        fix = hot & zone
        if not fix.any():
            break
        rgb[fix, 0] = np.minimum(rgb[fix, 0], (rgb[fix, 1].astype(np.int16) + 10).astype(np.uint8))
    return Image.fromarray(rgb)


def defuse_report_band_red(im: Image.Image) -> Image.Image:
    """Match report-onboarding-rustig-step08 JPEG gate (right 10%, top 58%)."""
    rgb = np.asarray(im.convert("RGB")).copy()
    h, w = rgb.shape[:2]
    report = np.zeros((h, w), dtype=bool)
    report[: int(h * 0.58), int(w * 0.90) :] = True
    for _ in range(32):
        r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
        hot = (r > 110) & (r > g + 18) & (r > b + 18) & (g < 145) & (b < 145)
        sunset_haze = (g > 118) & (r.astype(np.int16) - g.astype(np.int16) < 30)
        fix = hot & report & ~sunset_haze
        if not fix.any():
            break
        cap = np.maximum(rgb[fix, 1], rgb[fix, 2]).astype(np.int16) + 4
        rgb[fix, 0] = np.minimum(rgb[fix, 0], cap.astype(np.uint8))
    return Image.fromarray(rgb)


def pick_slice(im: Image.Image) -> Image.Image:
    w, h = im.size
    y0 = int(h * 0.11)
    y_end = int(h * 0.79)
    return im.crop((0, y0, w, y_end))


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    if not src.is_file():
        raise SystemExit(f"missing source: {src}")
    fin = load_fin()
    s4 = load_step04_helpers()
    im = s4.heal_topright_sky(Image.open(src).convert("RGB"))
    band = pick_slice(im).resize(OUT_SIZE, Image.Resampling.LANCZOS)
    band = defuse_figure_red(band)
    band = defuse_report_band_red(band)
    band = fin.apply_cream_fades(band)
    band = s4.defuse_right_foliage_red(band)
    band = defuse_report_band_red(band)
    band = s4.apply_strong_bottom_fade(band, fin)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    band.save(OUT, quality=98, optimize=True)
    band = defuse_report_band_red(s4.defuse_right_foliage_red(Image.open(OUT)))
    band.save(OUT, quality=98, optimize=True)
    bb = fin.content_bbox(band)
    print(f"wrote {OUT} content_bbox={bb} from {src.name}")


if __name__ == "__main__":
    main()
