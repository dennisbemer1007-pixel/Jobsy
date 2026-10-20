#!/usr/bin/env python3
"""Export step-06 desktop illustrated hero (3.5:1 full column width, v5-scale cast)."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
CREAM = np.array([247.0, 244.0, 240.0], dtype=np.float32)
DEFAULT_SRC = ROOT / "artifacts/onboarding-rustig-panorama-raw/step-06-droom-u6-aquarel-v7-source.jpg"
OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-mockup/step-06-droom-professions-u6-aquarel.jpg"
TARGET_W = 3500
MIN_RATIO = 3.5
SKY_FRAC = 0.15
FIGURE_SKY_FRAC = 0.12
GRASS_FRAC = 0.10
EDGE_VIGNETTE = 0.04


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(m)
    return m


def cast_row_bbox(im: Image.Image) -> tuple[int, int, int, int] | None:
    """Horizontal extent of the figure row (exclude wide sky-hill bleed)."""
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    h, w = rgb.shape[:2]
    dist = np.linalg.norm(rgb - CREAM, axis=2)
    y0, y1 = int(h * 0.32), int(h * 0.92)
    sub = dist[y0:y1, :]
    mask = sub > 28.0
    col_counts = mask.sum(axis=0)
    if col_counts.max() < 5:
        return None
    threshold = max(6, float(col_counts.max()) * 0.07)
    cols = np.where(col_counts >= threshold)[0]
    if cols.size == 0:
        return None
    rows = np.where(mask[:, cols.min() : cols.max() + 1].any(axis=1))[0]
    if rows.size == 0:
        return None
    return (
        int(cols.min()),
        int(y0 + rows.min()),
        int(cols.max()) + 1,
        int(y0 + rows.max()) + 1,
    )


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
                a = max(a, int(230 * t**0.9))
            elif x >= w - side:
                t = (x - (w - side)) / max(1, side - 1)
                a = max(a, int(230 * t**0.9))
            if a:
                px[x, y] = (*cream, a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def export_desktop(src: Path, out: Path) -> None:
    fin = load_fin()
    im = Image.open(src).convert("RGB")
    w, h = im.size
    scaled = im.resize((TARGET_W, max(1, int(h * TARGET_W / w))), Image.Resampling.LANCZOS)
    bb = fin.figure_bbox(scaled) or fin.content_bbox(scaled)
    if bb is None:
        raise SystemExit("no figure bbox in source")
    x0, y0, x1, y1 = bb
    fh = max(1, y1 - y0 + 1)
    fw = max(1, x1 - x0 + 1)
    sky_px = max(12, int(fh * 0.18))
    grass_px = max(12, int(fh * 0.08))
    cy0 = max(0, int(y0 - sky_px))
    cy1 = min(scaled.size[1], int(y1 + grass_px))
    band = scaled.crop((0, cy0, TARGET_W, cy1))
    target_h = max(1, int(TARGET_W / MIN_RATIO))
    pre_h = max(1, cy1 - cy0)
    band = band.resize((TARGET_W, target_h), Image.Resampling.LANCZOS)
    scale_y = target_h / pre_h
    fy0e = (y0 - cy0) * scale_y
    fy1e = (y1 - cy0) * scale_y
    tw, th = band.size
    cast_h_frac = (fy1e - fy0e) / th
    sky_frac = fy0e / th
    if cast_h_frac < 0.65:
        raise SystemExit(f"cast height {cast_h_frac:.2%} < 65% (y={fy0e:.0f}-{fy1e:.0f})")
    if sky_frac < SKY_FRAC * 0.85 or sky_frac > SKY_FRAC * 1.35:
        raise SystemExit(f"sky fraction {sky_frac:.2%} outside ~{SKY_FRAC:.0%} target")
    bb_check = fin.figure_bbox(band)
    cast_w_frac = (bb_check[2] - bb_check[0] + 1) / tw if bb_check else 1.0
    band = apply_edge_vignette(band, fin)
    out.parent.mkdir(parents=True, exist_ok=True)
    band.save(out, quality=92, optimize=True)
    print(
        f"wrote {out} size={band.size} cast_w={cast_w_frac:.1%} cast_h={cast_h_frac:.1%} "
        f"sky={sky_frac:.1%} figure_bbox={bb_check} from {src.name}"
    )


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    if not src.is_file():
        for alt in (
            Path("/opt/cursor/artifacts/assets/step-06-droom-u6-aquarel-v7-source.jpg"),
            Path("/opt/cursor/artifacts/assets/step-06-droom-u6-aquarel-v5-source.jpg"),
        ):
            if alt.is_file():
                src = alt
                break
        else:
            raise SystemExit(f"missing source: {src}")
    export_desktop(src, OUT)


if __name__ == "__main__":
    main()
