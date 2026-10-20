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
MOB_OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-mockup/kompas-c-hero-mobile.jpg"
MOB_SRC_DEFAULT = ROOT / "artifacts/onboarding-rustig-panorama-raw/kompas-c-hero-mobile-source.jpg"
DIR_OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-mockup"
TARGET_W = 3500
DESK_RATIO = 3.35  # ~400px at 1440 col
MOB_W = 780  # 2× for 390px CSS width
MOB_H = 480  # 2× for 240px — aspect 390:240 (~1.625:1)
MOB_RATIO = 390 / 240
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


def export_mobile_compose(src: Path, out: Path) -> None:
    """Mobile hero: 390:240 cover crop (2× asset), figure head-to-toe, soft sky fade."""
    fin = load_fin()
    im = Image.open(src).convert("RGB")
    w, h = im.size
    target_w, target_h = MOB_W, MOB_H
    aspect = target_w / target_h
    # Scale so we can cover-crop to target aspect.
    if w / h >= aspect:
        scaled_h = target_h
        scaled_w = max(target_w, int(round(w * scaled_h / h)))
    else:
        scaled_w = target_w
        scaled_h = max(target_h, int(round(h * scaled_w / w)))
    scaled = im.resize((scaled_w, scaled_h), Image.Resampling.LANCZOS)
    fb = fin.figure_bbox(scaled)
    margin_x = int(target_w * 0.06)
    if fb:
        fh = max(1, fb[3] - fb[1])
        target_fh = int(target_h * 0.76)
        if fh < target_fh * 0.92:
            zoom = min(1.35, target_fh / fh)
            scaled_w = int(round(scaled_w * zoom))
            scaled_h = int(round(scaled_h * zoom))
            scaled = im.resize((scaled_w, scaled_h), Image.Resampling.LANCZOS)
            fb = fin.figure_bbox(scaled)
            fh = max(1, fb[3] - fb[1]) if fb else fh
        cy = (fb[1] + fb[3]) // 2 if fb else scaled_h // 2
        y0 = int(cy - target_h * 0.54)
        y0 = max(0, min(y0, scaled_h - target_h))
        x0 = (fb[0] - margin_x) if fb else (scaled_w - target_w) // 2
        x0 = max(0, min(x0, scaled_w - target_w))
    else:
        y0 = max(0, (scaled_h - target_h) // 2)
        x0 = max(0, (scaled_w - target_w) // 2)
    band = scaled.crop((x0, y0, x0 + target_w, y0 + target_h))
    fb = fin.figure_bbox(band)
    if fb and fb[0] < margin_x:
        shift = margin_x - fb[0]
        pad = Image.new("RGB", (target_w + shift, target_h), tuple(int(c) for c in fin.CREAM))
        pad.paste(band, (shift, 0))
        band = pad.crop((0, 0, target_w, target_h))
        fb = fin.figure_bbox(band)
    # Soft sky → cream (no hard top edge).
    w, h = band.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    cream = tuple(int(c) for c in fin.CREAM)
    top = int(h * 0.14)
    bottom = int(h * 0.05)
    for y in range(h):
        for x in range(w):
            a = 0
            if y < top:
                t = 1.0 - y / max(1, top - 1)
                a = max(a, int(255 * t**0.75))
            elif y >= h - bottom:
                t = (y - (h - bottom)) / max(1, bottom - 1)
                a = max(a, int(255 * min(1.0, t**1.15)))
            if a:
                px[x, y] = (*cream, a)
    band = Image.alpha_composite(band.convert("RGBA"), overlay).convert("RGB")
    out.parent.mkdir(parents=True, exist_ok=True)
    band.save(out, quality=92, optimize=True)
    fb2 = fin.figure_bbox(band)
    left = fb2[0] / target_w if fb2 else 0
    fh_pct = (fb2[3] - fb2[1]) / target_h if fb2 else 0
    print(
        f"wrote {out} size={band.size} figure_bbox={fb2} "
        f"left={left:.1%} figure_h={fh_pct:.1%} (mobile 390:240)"
    )


def main() -> None:
    mode = sys.argv[1] if len(sys.argv) > 1 and sys.argv[1] in ("desktop", "mobile") else "all"
    args = [a for a in sys.argv[1:] if a not in ("desktop", "mobile")]
    src = Path(args[0]) if args else DEFAULT_SRC
    if mode in ("all", "desktop"):
        if not src.is_file():
            alt = Path("/opt/cursor/artifacts/assets/kompas-a-source-16x9-seamless.jpg")
            if alt.is_file():
                src = alt
            else:
                raise SystemExit(f"missing source: {src}")
        export_band(src, DESK_OUT, TARGET_W, DESK_RATIO, "desktop")
    if mode in ("all", "mobile"):
        mob_src = MOB_SRC_DEFAULT
        if not mob_src.is_file():
            mob_src = Path("/opt/cursor/artifacts/assets/kompas-c-hero-mobile-source.jpg")
        if not mob_src.is_file():
            mob_src = Path("/opt/cursor/artifacts/assets/kompas-b-hero-mobile-source.jpg")
        if not mob_src.is_file():
            raise SystemExit(f"missing mobile source: {mob_src}")
        export_mobile_compose(mob_src, MOB_OUT)


if __name__ == "__main__":
    main()
