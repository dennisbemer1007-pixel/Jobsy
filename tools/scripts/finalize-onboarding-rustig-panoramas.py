#!/usr/bin/env python3
"""Resize generated onboarding-rustig panoramas, apply cream fades, export mobile crops."""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama"
RAW = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "artifacts/onboarding-rustig-panorama-raw"

DESKTOP = (2080, 320)
MOBILE = (780, 222)  # 3.5:1 at 2x for ~110px CSS height
CREAM = (247, 244, 240)

SLUGS = [
    "start",
    "step-01",
    "step-02",
    "step-03",
    "step-04",
    "step-05",
    "step-06",
    "step-07",
    "step-08",
    "step-09",
    "step-10",
    "step-11",
    "kompas",
]


def apply_cream_fades(im: Image.Image) -> Image.Image:
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    for y in range(h):
        for x in range(w):
            a = 0
            if y > h * 0.82:
                t = (y - h * 0.82) / (h * 0.18)
                a = max(a, int(220 * min(1.0, t**1.15)))
            if x < w * 0.05:
                t = 1.0 - x / (w * 0.05)
                a = max(a, int(200 * t))
            elif x > w * 0.95:
                t = (x - w * 0.95) / (w * 0.05)
                a = max(a, int(200 * t))
            if a:
                px[x, y] = (CREAM[0], CREAM[1], CREAM[2], a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def fit_band(raw: Image.Image, target_w: int, target_h: int) -> Image.Image:
    """Contain full source in band: no head/feet crop; ground aligned to bottom."""
    sw, sh = raw.size
    # Leave a little top sky in the band (figures ~55–70% of height).
    max_scene_h = int(target_h * 0.84)
    scale = min(target_w / sw, max_scene_h / sh)
    nw, nh = max(1, int(sw * scale)), max(1, int(sh * scale))
    scaled = raw.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (target_w, target_h), CREAM)
    x = (target_w - nw) // 2
    y = max(0, target_h - nh - 6)
    canvas.paste(scaled, (x, y))
    return canvas


def fit_desktop(raw: Image.Image) -> Image.Image:
    return fit_band(raw, DESKTOP[0], DESKTOP[1])


def fit_mobile(raw: Image.Image) -> Image.Image:
    return fit_band(raw, MOBILE[0], MOBILE[1])


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for slug in SLUGS:
        candidates = [
            RAW / f"{slug}-desktop-raw.jpg",
            RAW / f"{slug}-raw.jpg",
            RAW / f"onboarding-rustig-{slug}-desktop-raw.jpg",
        ]
        src = next((p for p in candidates if p.is_file()), None)
        if src is None:
            raise SystemExit(f"missing raw for {slug} in {RAW}")
        raw = Image.open(src).convert("RGB")
        desk = apply_cream_fades(fit_desktop(raw))
        mob = apply_cream_fades(fit_mobile(raw))
        desk.save(OUT / f"{slug}-desktop.jpg", quality=92, optimize=True)
        mob.save(OUT / f"{slug}-mobile.jpg", quality=92, optimize=True)
        print(f"{slug}: {desk.size} / {mob.size} <- {src.name}")


if __name__ == "__main__":
    main()
