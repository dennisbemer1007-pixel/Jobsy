#!/usr/bin/env python3
"""Crop ultra-wide onboarding-rustig panoramas for cover hero bands (no letterbox)."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama"
RAW = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "artifacts/onboarding-rustig-panorama-raw"

# 2× assets: ~1140×170 desktop (6:1), 390×130 mobile (3:1)
DESKTOP = (2280, 340)  # 6:1 at 2× (matches ~1140×170 content column)
MOBILE = (780, 260)
CREAM = np.array([247, 244, 240], dtype=np.float32)
DESKTOP_RATIO = 6.0
MOBILE_RATIO = 3.0
HEAD_MARGIN = 8
FEET_ABOVE_FADE = 25
FADE_BOTTOM = 25
FADE_SIDE = 30

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


def content_bbox(im: Image.Image) -> tuple[int, int, int, int] | None:
    """Bounding box of non-cream illustration pixels."""
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    dist = np.linalg.norm(rgb - CREAM, axis=2)
    mask = dist > 22.0
    if not mask.any():
        return None
    ys, xs = np.where(mask)
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def figure_bbox(im: Image.Image) -> tuple[int, int, int, int] | None:
    """BBox of scene/figures, ignoring empty cream sky in the top of the frame."""
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    dist = np.linalg.norm(rgb - CREAM, axis=2)
    h, w = dist.shape
    mask = dist > 30.0
    mask[: int(h * 0.26), :] = False
    mask[int(h * 0.82) :, :] = False
    if mask.sum() < h * w * 0.004:
        mask = dist > 22.0
        mask[: int(h * 0.26), :] = False
    if not mask.any():
        return None
    row_counts = mask.sum(axis=1)
    threshold = max(8, int(row_counts.max() * 0.12))
    active = np.where(row_counts >= threshold)[0]
    if active.size == 0:
        ys, xs = np.where(mask)
    else:
        y_top, y_bot = int(active.min()), int(active.max()) + 1
        band = mask[y_top:y_bot, :]
        ys, xs = np.where(band)
        ys = ys + y_top
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def extend_ultra_wide(raw: Image.Image, min_ratio: float = 6.5) -> Image.Image:
    """Blend three 16:9 panels into one continuous strip (no mirrored seams)."""
    w, h = raw.size
    bbox = content_bbox(raw)
    if bbox is not None:
        bw = bbox[2] - bbox[0]
        if bw >= w * 0.88:
            return raw
    if w / h >= min_ratio:
        return raw
    panel = raw
    pw, ph = panel.size
    canvas_w = int(pw * 2.55)
    canvas = Image.new("RGB", (canvas_w, ph), tuple(CREAM.astype(int)))
    # Left third, center, right third with soft overlap blends.
    positions = [0, int(pw * 0.78), int(pw * 1.56)]
    for i, x in enumerate(positions):
        layer = Image.new("RGBA", (canvas_w, ph), (0, 0, 0, 0))
        layer.paste(panel.convert("RGBA"), (x, 0))
        if i == 0:
            canvas = Image.alpha_composite(canvas.convert("RGBA"), layer).convert("RGB")
            continue
        blend_w = int(pw * 0.12)
        for bx in range(blend_w):
            t = bx / max(1, blend_w - 1)
            alpha = int(255 * t)
            x0 = positions[i] + bx
            if x0 >= canvas_w:
                break
            for y in range(ph):
                c1 = canvas.getpixel((x0, y))
                c2 = layer.getpixel((x0, y))[:3]
                if layer.getpixel((x0, y))[3] == 0:
                    continue
                canvas.putpixel(
                    (x0, y),
                    tuple(int(c1[j] * (1 - t) + c2[j] * t) for j in range(3)),
                )
        rest = Image.alpha_composite(
            canvas.convert("RGBA"),
            layer,
        ).convert("RGB")
        canvas = rest
    return canvas


def crop_to_ratio(
    im: Image.Image,
    target_w: int,
    target_h: int,
    ratio: float,
) -> Image.Image:
    """Scale to cover width, crop vertical band keeping figure bbox inside."""
    wide = extend_ultra_wide(im)
    w, h = wide.size
    scale = target_w / w
    scaled_h = max(1, int(h * scale))
    scaled = wide.resize((target_w, scaled_h), Image.Resampling.LANCZOS)
    crop_h = int(round(target_w / ratio))
    crop_h = min(crop_h, scaled_h)
    bbox = figure_bbox(scaled) or content_bbox(scaled)
    if bbox is None:
        y0 = max(0, int(scaled_h * 0.42) - crop_h // 2)
    else:
        _x1, y1, _x2, y2 = bbox
        sky = int(crop_h * 0.32)
        y0 = y1 - sky
        ground_y = int(scaled_h * 0.70)
        y0 = min(y0, ground_y - int(crop_h * 0.72))
        y0 = max(0, min(y0, scaled_h - crop_h))
        if y1 < y0 + HEAD_MARGIN:
            y0 = max(0, y1 - HEAD_MARGIN)
        if y2 > y0 + crop_h - FEET_ABOVE_FADE:
            y0 = min(scaled_h - crop_h, y2 + FEET_ABOVE_FADE - crop_h)
        y0 = max(0, min(y0, scaled_h - crop_h))
    band = scaled.crop((0, y0, target_w, y0 + crop_h))
    if band.size[1] != target_h:
        band = band.resize((target_w, target_h), Image.Resampling.LANCZOS)
    return band


def apply_cream_fades(im: Image.Image) -> Image.Image:
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    cream = tuple(int(c) for c in CREAM)
    for y in range(h):
        for x in range(w):
            a = 0
            if y >= h - FADE_BOTTOM:
                t = (y - (h - FADE_BOTTOM)) / max(1, FADE_BOTTOM - 1)
                a = max(a, int(255 * min(1.0, t**1.1)))
            if x < FADE_SIDE:
                t = 1.0 - x / max(1, FADE_SIDE - 1)
                a = max(a, int(220 * t))
            elif x >= w - FADE_SIDE:
                t = (x - (w - FADE_SIDE)) / max(1, FADE_SIDE - 1)
                a = max(a, int(220 * t))
            if a:
                px[x, y] = (*cream, a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def fit_desktop(raw: Image.Image) -> Image.Image:
    return crop_to_ratio(raw, DESKTOP[0], DESKTOP[1], DESKTOP_RATIO)


def fit_mobile(raw: Image.Image) -> Image.Image:
    return crop_to_ratio(raw, MOBILE[0], MOBILE[1], MOBILE_RATIO)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for slug in SLUGS:
        candidates = [
            RAW / f"{slug}-desktop-raw.jpg",
            RAW / f"{slug}-raw.jpg",
            RAW / f"{slug}-desktop-raw.png",
            RAW / f"{slug}-raw.png",
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
        bb = figure_bbox(desk) or content_bbox(desk)
        print(f"{slug}: desk {desk.size} mob {mob.size} bbox {bb} <- {src.name}")


if __name__ == "__main__":
    main()
