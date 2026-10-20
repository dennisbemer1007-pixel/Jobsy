#!/usr/bin/env python3
"""Build step-04 band-native raw: full-width 6:1 bleed, complete figures + headroom."""

from __future__ import annotations

import importlib.util
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "artifacts/onboarding-rustig-panorama-raw"
SRC = RAW / "step-04-source-16x9-u6o-v3.jpg"
OUT = RAW / "step-04-desktop-raw.jpg"
OUT_SIZE = (2400, 400)
HEAD_MIN_FRAC = 0.14
FADE_BOTTOM = 36


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


def widen_landscape(im: Image.Image, fin, target_ratio: float = 2.35) -> Image.Image:
    """Extend wings from side strips so the band can fill 6:1 without side letterbox."""
    w, h = im.size
    target_w = int(h * target_ratio)
    if target_w <= w:
        return im
    cream = tuple(int(c) for c in fin.CREAM)
    out = Image.new("RGB", (target_w, h), cream)
    ox = (target_w - w) // 2
    wing = ox
    strip = max(96, w // 8)
    left = im.crop((0, 0, strip, h)).resize((wing, h), Image.Resampling.LANCZOS)
    right = im.crop((w - strip, 0, w, h)).resize((wing, h), Image.Resampling.LANCZOS)
    out.paste(left, (0, 0))
    out.paste(right, (wing + w, 0))
    out.paste(im, (ox, 0))
    return out


def apply_strong_bottom_fade(im: Image.Image, fin) -> Image.Image:
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    cream = tuple(int(c) for c in fin.CREAM)
    for y in range(h):
        for x in range(w):
            if y < h - FADE_BOTTOM:
                continue
            t = (y - (h - FADE_BOTTOM)) / max(1, FADE_BOTTOM - 1)
            a = int(255 * min(1.0, t**0.85))
            if a:
                px[x, y] = (*cream, a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def pick_slice(im: Image.Image, fin) -> Image.Image:
    w, h = im.size
    cb = fin.content_bbox(im) or (0, int(h * 0.15), w, h)
    slice_h = int(w / 4.35)
    slice_h = min(slice_h, h - 4)
    y0 = max(0, min(int(cb[3] - slice_h * 0.94), h - slice_h))
    for _ in range(24):
        band = im.crop((0, y0, w, y0 + slice_h))
        fb = fin.figure_bbox(band)
        if fb is None:
            break
        head = fb[1] / band.size[1]
        if head >= HEAD_MIN_FRAC:
            break
        y0 = max(0, y0 - max(4, int(slice_h * 0.03)))
    return im.crop((0, y0, w, min(h, y0 + slice_h)))


def main() -> None:
    fin = load_fin()
    im = widen_landscape(Image.open(SRC).convert("RGB"), fin)
    slice_im = pick_slice(im, fin)
    band = slice_im.resize(OUT_SIZE, Image.Resampling.LANCZOS)
    band = fin.apply_cream_fades(band)
    band = apply_strong_bottom_fade(band, fin)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    band.save(OUT, quality=95, optimize=True)
    bb = fin.content_bbox(band)
    print(f"wrote {OUT} size={OUT_SIZE} content_bbox={bb} from {SRC.name}")


if __name__ == "__main__":
    main()
