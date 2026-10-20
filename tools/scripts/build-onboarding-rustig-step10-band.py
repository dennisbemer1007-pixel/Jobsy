#!/usr/bin/env python3
"""Build step-10 band-native raw from a seamless wide source (no side patching)."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "artifacts/onboarding-rustig-panorama-raw"
DEFAULT_SRC = RAW / "step-10-source-16x9-u10a-seamless.jpg"
OUT = RAW / "step-10-desktop-raw.jpg"
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
    """Middle band: full figures + grass, minimal sky (u8d sources place feet ~75–82%)."""
    w, h = im.size
    y0 = int(h * 0.08)
    y_end = int(h * 0.82)
    return im.crop((0, y0, w, y_end))


def lift_figures_in_band(band: Image.Image, fin, *, ground_margin: int = 54) -> Image.Image:
    """Scale scene to fit with feet above bottom fade (figure bbox, not full sky)."""
    bb = fin.figure_bbox(band) or fin.content_bbox(band)
    if bb is None:
        return band
    w, h = band.size
    x0, y0, x1, y1 = bb
    crop = band.crop(bb)
    cw, ch = crop.size
    max_scene_h = h - ground_margin - 20
    scale = min(1.0, max_scene_h / ch)
    if scale < 0.999:
        nw, nh = max(1, int(cw * scale)), max(1, int(ch * scale))
        crop = crop.resize((nw, nh), Image.Resampling.LANCZOS)
        cw, ch = crop.size
    cream = tuple(int(c) for c in fin.CREAM)
    canvas = Image.new("RGB", (w, h), cream)
    paste_x = max(0, (w - cw) // 2)
    paste_y = h - ground_margin - ch
    paste_y = max(16, paste_y)
    canvas.paste(crop, (paste_x, paste_y))
    return canvas


def extend_ground_below_feet(band: Image.Image, fin) -> Image.Image:
    """Grass/cream fill under feet so the band gate sees ground above the fade."""
    bb = fin.figure_bbox(band)
    if bb is None:
        return band
    w, h = band.size
    _, _, _, y1 = bb
    y1 = min(y1, h - 20)
    sample_y = max(0, y1 - 8)
    out = band.copy()
    strip_h = max(1, h - y1 - 4)
    strip = band.crop((0, sample_y, w, y1 + 1)).resize((w, strip_h), Image.Resampling.LANCZOS)
    out.paste(strip, (0, y1 + 1))
    return out


def apply_step08_bottom_fade(im: Image.Image, fin) -> Image.Image:
    """Shorter fade than step-04 band build — keep feet visible above cream."""
    fade = 14
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    cream = tuple(int(c) for c in fin.CREAM)
    for y in range(h):
        if y < h - fade:
            continue
        for x in range(w):
            t = (y - (h - fade)) / max(1, fade - 1)
            a = int(255 * min(1.0, t**1.05))
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
    band = pick_slice(im).resize(OUT_SIZE, Image.Resampling.LANCZOS)
    band = defuse_figure_red(band)
    band = defuse_report_band_red(band)
    band = fin.apply_cream_fades(band)
    band = s4.defuse_right_foliage_red(band)
    band = defuse_report_band_red(band)
    band = apply_step08_bottom_fade(band, fin)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    band.save(OUT, quality=98, optimize=True)
    band = defuse_report_band_red(s4.defuse_right_foliage_red(Image.open(OUT)))
    band.save(OUT, quality=98, optimize=True)
    bb = fin.content_bbox(band)
    print(f"wrote {OUT} content_bbox={bb} from {src.name}")


if __name__ == "__main__":
    main()
