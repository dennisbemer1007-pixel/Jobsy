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
            if y > h * 0.55:
                t = (y - h * 0.55) / (h * 0.45)
                a = max(a, int(245 * min(1.0, t**1.05)))
            if x < w * 0.05:
                t = 1.0 - x / (w * 0.05)
                a = max(a, int(200 * t))
            elif x > w * 0.95:
                t = (x - w * 0.95) / (w * 0.05)
                a = max(a, int(200 * t))
            if a:
                px[x, y] = (CREAM[0], CREAM[1], CREAM[2], a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def fit_desktop(raw: Image.Image) -> Image.Image:
    """Scale to full band width, keep bottom slice (for bottom-weighted 16:9 sources)."""
    target_w, target_h = DESKTOP
    sw, sh = raw.size
    scale = target_w / sw
    nh = int(sh * scale)
    scaled = raw.resize((target_w, nh), Image.Resampling.LANCZOS)
    if nh >= target_h:
        top = nh - target_h
        return scaled.crop((0, top, target_w, nh))
    canvas = Image.new("RGB", DESKTOP, CREAM)
    canvas.paste(scaled, (0, target_h - nh))
    return canvas


def mobile_from_desktop(desk: Image.Image) -> Image.Image:
    w, h = desk.size
    crop_w = int(h * (MOBILE[0] / MOBILE[1]))
    crop_w = min(crop_w, w)
    x0 = (w - crop_w) // 2
    crop = desk.crop((x0, 0, x0 + crop_w, h))
    return crop.resize(MOBILE, Image.Resampling.LANCZOS)


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
        mob = apply_cream_fades(mobile_from_desktop(desk))
        desk.save(OUT / f"{slug}-desktop.jpg", quality=92, optimize=True)
        mob.save(OUT / f"{slug}-mobile.jpg", quality=92, optimize=True)
        print(f"{slug}: {desk.size} / {mob.size} <- {src.name}")


if __name__ == "__main__":
    main()
