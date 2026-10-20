#!/usr/bin/env python3
"""Export step-06 desktop illustrated hero (wide ~3.5:1, full cast, no side crop)."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SRC = ROOT / "artifacts/onboarding-rustig-panorama-raw/step-06-droom-u6-aquarel-v5-source.jpg"
OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-mockup/step-06-droom-professions-u6-aquarel.jpg"
TARGET_W = 3500
MIN_RATIO = 3.5  # width : height


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(m)
    return m


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
    cy0 = max(0, int(y0 - fh * 0.38))
    cy1 = min(scaled.size[1], int(y1 + fh * 0.18))
    band = scaled.crop((0, cy0, TARGET_W, cy1))
    target_h = max(1, int(TARGET_W / MIN_RATIO))
    fcy = (y0 + y1) // 2
    ny0 = int(fcy - target_h / 2)
    ny0 = max(0, min(ny0, band.size[1] - target_h))
    band = band.crop((0, ny0, TARGET_W, ny0 + target_h))
    bb_check = fin.figure_bbox(band)
    if bb_check is None:
        raise SystemExit("figures missing after vertical trim")
    _, fy0, _, fy1 = bb_check
    if fy0 < 8 or fy1 > target_h - 12:
        raise SystemExit(f"figures clipped in export bbox={bb_check} target_h={target_h}")
    band = fin.apply_cream_fades(band)
    out.parent.mkdir(parents=True, exist_ok=True)
    band.save(out, quality=92, optimize=True)
    bb2 = fin.figure_bbox(band) or fin.content_bbox(band)
    print(f"wrote {out} size={band.size} figure_bbox={bb2} from {src.name}")


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    if not src.is_file():
        alt = Path("/opt/cursor/artifacts/assets/step-06-droom-u6-aquarel-v5-source.jpg")
        if alt.is_file():
            src = alt
        else:
            raise SystemExit(f"missing source: {src}")
    export_desktop(src, OUT)


if __name__ == "__main__":
    main()
