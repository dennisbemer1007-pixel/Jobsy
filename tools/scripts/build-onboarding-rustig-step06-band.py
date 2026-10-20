#!/usr/bin/env python3
"""Build step-06 band-native raw from a seamless wide source (no side patching)."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "artifacts/onboarding-rustig-panorama-raw"
DEFAULT_SRC = RAW / "step-06-source-16x9-u6z2-seamless.jpg"
OUT = RAW / "step-06-desktop-raw.jpg"
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


def pick_slice(im: Image.Image) -> Image.Image:
    """Band window: figures drawn small in mid 16:9 — wide slice like step-04 (u6z2)."""
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
    band = fin.apply_cream_fades(band)
    band = s4.defuse_right_foliage_red(band)
    band = s4.apply_strong_bottom_fade(band, fin)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    band.save(OUT, quality=98, optimize=True)
    band = s4.defuse_right_foliage_red(Image.open(OUT))
    band.save(OUT, quality=98, optimize=True)
    bb = fin.content_bbox(band)
    print(f"wrote {OUT} content_bbox={bb} from {src.name}")


if __name__ == "__main__":
    main()
