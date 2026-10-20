#!/usr/bin/env python3
"""Export Kompas full-width illustrated hero (desktop + mobile) from 16:9 source."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
CREAM = np.array([247.0, 244.0, 240.0], dtype=np.float32)
DEFAULT_SRC = ROOT / "artifacts/onboarding-rustig-panorama-raw/kompas-a-source-16x9-seamless.jpg"
DESK_OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-mockup/kompas-a-hero-desktop.jpg"
MOB_OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-mockup/kompas-a-hero-mobile.jpg"
DIR_OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-mockup"
TARGET_W = 3500
DESK_RATIO = 3.35  # ~400px at 1440 col
MOB_W = 1170
MOB_RATIO = 4.55  # ~257px at 390
EDGE_VIGNETTE = 0.035


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(m)
    return m


def apply_edge_vignette(im: Image.Image, fin) -> Image.Image:
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    cream = tuple(int(c) for c in fin.CREAM)
    top = int(h * EDGE_VIGNETTE)
    bottom = int(h * EDGE_VIGNETTE)
    side = int(w * EDGE_VIGNETTE)
    for y in range(h):
        for x in range(w):
            a = 0
            if y < top:
                t = 1.0 - y / max(1, top - 1)
                a = max(a, int(255 * t**0.9))
            elif y >= h - bottom:
                t = (y - (h - bottom)) / max(1, bottom - 1)
                a = max(a, int(255 * min(1.0, t**0.95)))
            if x < side:
                t = 1.0 - x / max(1, side - 1)
                a = max(a, int(220 * t**0.9))
            elif x >= w - side:
                t = (x - (w - side)) / max(1, side - 1)
                a = max(a, int(220 * t**0.9))
            if a:
                px[x, y] = (*cream, a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def export_band(src: Path, out: Path, target_w: int, ratio: float, label: str) -> None:
    fin = load_fin()
    im = Image.open(src).convert("RGB")
    w, h = im.size
    scaled = im.resize((target_w, max(1, int(h * target_w / w))), Image.Resampling.LANCZOS)
    bb = fin.figure_bbox(scaled) or fin.content_bbox(scaled)
    if bb is None:
        raise SystemExit(f"no bbox in {label}")
    x0, y0, x1, y1 = bb
    fh = max(1, y1 - y0 + 1)
    sky_px = max(16, int(fh * 0.22))
    grass_px = max(14, int(fh * 0.10))
    cy0 = max(0, int(y0 - sky_px))
    cy1 = min(scaled.size[1], int(y1 + grass_px))
    band = scaled.crop((0, cy0, target_w, cy1))
    target_h = max(1, int(round(target_w / ratio)))
    band = band.resize((target_w, target_h), Image.Resampling.LANCZOS)
    band = apply_edge_vignette(band, fin)
    out.parent.mkdir(parents=True, exist_ok=True)
    band.save(out, quality=92, optimize=True)
    fb = fin.figure_bbox(band)
    print(f"wrote {out} size={band.size} figure_bbox={fb} ({label})")


def export_direction_thumbs(src: Path) -> None:
    fin = load_fin()
    im = Image.open(src).convert("RGB")
    w, h = im.size
    boxes = [
        ("kompas-a-direction-zorg.jpg", 0.52, 0.38, 0.68, 0.58),
        ("kompas-a-direction-kas.jpg", 0.62, 0.36, 0.78, 0.56),
        ("kompas-a-direction-kantoor.jpg", 0.72, 0.34, 0.88, 0.54),
    ]
    for name, x0f, y0f, x1f, y1f in boxes:
        crop = im.crop((int(w * x0f), int(h * y0f), int(w * x1f), int(h * y1f)))
        crop = crop.resize((240, 180), Image.Resampling.LANCZOS)
        crop = apply_edge_vignette(crop, fin)
        crop.save(DIR_OUT / name, quality=90, optimize=True)
        print(f"wrote {DIR_OUT / name}")


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    if not src.is_file():
        alt = Path("/opt/cursor/artifacts/assets/kompas-a-source-16x9-seamless.jpg")
        if alt.is_file():
            src = alt
        else:
            raise SystemExit(f"missing source: {src}")
    export_band(src, DESK_OUT, TARGET_W, DESK_RATIO, "desktop")
    export_band(src, MOB_OUT, MOB_W, MOB_RATIO, "mobile")
    export_direction_thumbs(src)


if __name__ == "__main__":
    main()
