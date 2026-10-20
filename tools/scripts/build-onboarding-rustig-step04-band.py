#!/usr/bin/env python3
"""Build step-04 band-native raw: full figures (feet + ground) scaled into 6:1 band."""

from __future__ import annotations

import importlib.util
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "artifacts/onboarding-rustig-panorama-raw"
SRC = RAW / "step-04-source-16x9-u6o-v3.jpg"
OUT = RAW / "step-04-desktop-raw.jpg"
OUT_SIZE = (2400, 400)
FADE_BOTTOM = 36


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


def apply_strong_bottom_fade(im: Image.Image, fin) -> Image.Image:
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    cream = tuple(int(c) for c in fin.CREAM)
    for y in range(h):
        for x in range(w):
            a = 0
            if y >= h - FADE_BOTTOM:
                t = (y - (h - FADE_BOTTOM)) / max(1, FADE_BOTTOM - 1)
                a = max(a, int(255 * min(1.0, t**0.85)))
            if a:
                px[x, y] = (*cream, a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def main() -> None:
    fin = load_fin()
    im = Image.open(SRC).convert("RGB")
    w, h = im.size
    cb = fin.content_bbox(im) or (0, int(h * 0.2), w, h)
    slice_h = int(w / 4.85)
    y0 = max(0, min(cb[3] - slice_h, h - slice_h))
    tall = im.crop((0, y0, w, y0 + slice_h))
    target_h = 372
    scale = target_h / tall.size[1]
    nw = min(OUT_SIZE[0] - 48, int(tall.size[0] * scale))
    nh = int(tall.size[1] * (nw / tall.size[0]))
    tall = tall.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", OUT_SIZE, tuple(fin.CREAM.astype(int)))
    x0 = max(12, (OUT_SIZE[0] - nw) // 2)
    y0c = OUT_SIZE[1] - nh - 10
    y0c = max(8, y0c)
    canvas.paste(tall, (x0, y0c))
    canvas = fin.apply_cream_fades(canvas)
    canvas = apply_strong_bottom_fade(canvas, fin)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUT, quality=95, optimize=True)
    print(f"wrote {OUT} from {SRC.name} paste y={y0c} scene {nw}x{nh}")


if __name__ == "__main__":
    main()
