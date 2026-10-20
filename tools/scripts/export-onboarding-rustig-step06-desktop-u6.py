#!/usr/bin/env python3
"""Export step-06 desktop illustrated hero (wide ~3.5:1, full cast, soft cream edges)."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SRC = ROOT / "artifacts/onboarding-rustig-panorama-raw/step-06-droom-u6-aquarel-v6-source.jpg"
OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-mockup/step-06-droom-professions-u6-aquarel.jpg"
TARGET_W = 3500
MIN_RATIO = 3.5  # width : height
HEADROOM_FRAC = 0.18
SIDE_FRAC = 0.06
GRASS_FRAC = 0.10


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(m)
    return m


def apply_illustrated_vignette(im: Image.Image, fin) -> Image.Image:
    """Soft cream on all edges (match panorama hero headers)."""
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    cream = tuple(int(c) for c in fin.CREAM)
    top = int(h * 0.14)
    bottom = int(h * 0.12)
    side = int(w * 0.07)
    for y in range(h):
        for x in range(w):
            a = 0
            if y < top:
                t = 1.0 - y / max(1, top - 1)
                a = max(a, int(235 * t**0.85))
            elif y >= h - bottom:
                t = (y - (h - bottom)) / max(1, bottom - 1)
                a = max(a, int(255 * min(1.0, t**1.05)))
            if x < side:
                t = 1.0 - x / max(1, side - 1)
                a = max(a, int(210 * t**0.9))
            elif x >= w - side:
                t = (x - (w - side)) / max(1, side - 1)
                a = max(a, int(210 * t**0.9))
            if a:
                px[x, y] = (*cream, a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def export_desktop(src: Path, out: Path) -> None:
    fin = load_fin()
    im = Image.open(src).convert("RGB")
    target_h = max(1, int(TARGET_W / MIN_RATIO))
    cream = tuple(int(c) for c in fin.CREAM)
    canvas = Image.new("RGB", (TARGET_W, target_h), cream)
    max_inner_w = int(TARGET_W * (1 - 2 * SIDE_FRAC))
    max_inner_h = int(target_h * (1 - HEADROOM_FRAC - GRASS_FRAC))
    scale = min(max_inner_w / im.size[0], max_inner_h / im.size[1])
    nw, nh = max(1, int(im.size[0] * scale)), max(1, int(im.size[1] * scale))
    placed = im.resize((nw, nh), Image.Resampling.LANCZOS)
    px = (TARGET_W - nw) // 2
    py = int(target_h * HEADROOM_FRAC * 0.55)
    canvas.paste(placed, (px, py))
    band = canvas
    bb_check = fin.figure_bbox(band)
    if bb_check is None:
        raise SystemExit("figures missing after export crop")
    fx0, fy0, fx1, fy1 = bb_check
    tw, th = band.size
    if fy0 < th * HEADROOM_FRAC * 0.75:
        raise SystemExit(f"insufficient headroom fy0={fy0} th={th}")
    if fx0 < tw * SIDE_FRAC * 0.65 or fx1 > tw * (1 - SIDE_FRAC * 0.65):
        raise SystemExit(f"insufficient side margin bbox={bb_check}")
    if fy1 > th * (1 - GRASS_FRAC * 0.55):
        raise SystemExit(f"insufficient grass below feet bbox={bb_check}")
    band = fin.apply_cream_fades(band)
    band = apply_illustrated_vignette(band, fin)
    out.parent.mkdir(parents=True, exist_ok=True)
    band.save(out, quality=92, optimize=True)
    bb2 = fin.figure_bbox(band) or fin.content_bbox(band)
    print(f"wrote {out} size={band.size} figure_bbox={bb2} from {src.name}")


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    if not src.is_file():
        for alt in (
            Path("/opt/cursor/artifacts/assets/step-06-droom-u6-aquarel-v6-source.jpg"),
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
