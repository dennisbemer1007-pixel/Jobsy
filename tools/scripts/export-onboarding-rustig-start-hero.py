#!/usr/bin/env python3
"""Export start hero bands from clean 16:9 source (no inpaint)."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "artifacts/onboarding-rustig-panorama-raw/start-desktop-raw.jpg"
FALLBACK = Path("/opt/cursor/artifacts/assets/start-desktop-raw-v3.jpg")
DESK_OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/start-desktop.jpg"
MOB_OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/start-mobile.jpg"
CREAM = np.array([247.0, 244.0, 240.0], dtype=np.float32)


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(m)
    return m


def apply_top_sky_fade(im: Image.Image, top_frac: float = 0.12) -> Image.Image:
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    cream = tuple(int(c) for c in CREAM)
    top = max(8, int(h * top_frac))
    for y in range(h):
        for x in range(w):
            a = 0
            if y < top:
                t = 1.0 - y / max(1, top - 1)
                a = max(a, int(255 * t**0.75))
            if a:
                px[x, y] = (*cream, a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def export_start(src: Path, *, include_mobile: bool = False) -> None:
    import subprocess

    subprocess.check_call(
        [sys.executable, str(ROOT / "tools/scripts/build-onboarding-rustig-start-desktop-band.py"), str(src)],
        cwd=ROOT,
    )
    if include_mobile:
        fin = load_fin()
        raw = Image.open(src).convert("RGB")
        mob = fin.apply_cream_fades(fin.fit_mobile(raw, "start"))
        mob = apply_top_sky_fade(mob, 0.10)
        MOB_OUT.parent.mkdir(parents=True, exist_ok=True)
        mob.save(MOB_OUT, quality=92, optimize=True)
        print(f"wrote {MOB_OUT} size={mob.size}")


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else RAW
    if not src.is_file():
        src = FALLBACK
    if not src.is_file():
        raise SystemExit(f"missing start source: {src}")
    export_start(src)


if __name__ == "__main__":
    main()
