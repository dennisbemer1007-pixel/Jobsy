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
HEAD_MARGIN = 12
MIN_HEAD_PX = 12
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
    mask[: int(h * 0.40), :] = False
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
    crop_h = min(target_h, scaled_h)
    bbox = figure_bbox(scaled) or content_bbox(scaled)
    if bbox is None:
        y0 = max(0, int(scaled_h * 0.42) - crop_h // 2)
    else:
        _x1, y1, _x2, y2 = bbox
        min_headroom = max(HEAD_MARGIN, int(crop_h * 0.28))
        cy = (y1 + y2) // 2
        y0 = cy - crop_h // 2
        y0 = max(0, min(y0, scaled_h - crop_h))
        if y1 < y0 + min_headroom:
            y0 = max(0, y1 - min_headroom)
        if y2 > y0 + crop_h - FEET_ABOVE_FADE:
            y0_feet = min(scaled_h - crop_h, y2 + FEET_ABOVE_FADE - crop_h)
            if y1 >= y0_feet + min_headroom:
                y0 = y0_feet
        if y1 < y0 + min_headroom:
            y0 = max(0, y1 - min_headroom)
        y0 = max(0, min(y0, scaled_h - crop_h))
    band = scaled.crop((0, y0, target_w, y0 + crop_h))
    if band.size[1] != target_h:
        band = band.resize((target_w, target_h), Image.Resampling.LANCZOS)
    # If we landed on empty sky, anchor on the lower scene block.
    check = figure_bbox(band)
    fig_h = (check[3] - check[1]) if check else 0
    if fig_h < int(band.size[1] * 0.18):
        y0_alt = int(scaled_h * 0.56) - crop_h // 2
        y0_alt = max(0, min(y0_alt, scaled_h - crop_h))
        band = scaled.crop((0, y0_alt, target_w, y0_alt + crop_h))
        if band.size[1] != target_h:
            band = band.resize((target_w, target_h), Image.Resampling.LANCZOS)
        if bbox is not None:
            _x1, y1, _x2, y2 = bbox
            mh = max(HEAD_MARGIN, int(crop_h * 0.22))
            y0_fix = max(0, y1 - mh)
            if y2 > y0_fix + crop_h - FEET_ABOVE_FADE:
                y0_fix = min(scaled_h - crop_h, y2 + FEET_ABOVE_FADE - crop_h)
            y0_fix = max(0, min(y0_fix, scaled_h - crop_h))
            band = scaled.crop((0, y0_fix, target_w, y0_fix + crop_h))
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


def figure_head_tops(im: Image.Image) -> list[int]:
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    dist = np.linalg.norm(rgb - CREAM, axis=2)
    h, w = dist.shape
    mask = dist > 28.0
    visited = np.zeros_like(mask, dtype=bool)
    tops: list[int] = []
    min_area = max(80, int(h * w * 0.002))
    for y in range(h):
        for x in range(w):
            if not mask[y, x] or visited[y, x]:
                continue
            stack = [(y, x)]
            ys: list[int] = []
            xs: list[int] = []
            while stack:
                cy, cx = stack.pop()
                if cy < 0 or cy >= h or cx < 0 or cx >= w:
                    continue
                if visited[cy, cx] or not mask[cy, cx]:
                    continue
                visited[cy, cx] = True
                ys.append(cy)
                xs.append(cx)
                stack.extend([(cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)])
            if len(ys) < min_area:
                continue
            y0, y1 = min(ys), max(ys)
            x0, x1 = min(xs), max(xs)
            blob_h, blob_w = y1 - y0 + 1, x1 - x0 + 1
            if blob_h < int(h * 0.12):
                continue
            if blob_h > int(h * 0.72):
                continue
            if blob_w > int(w * 0.55) and blob_h < int(h * 0.35):
                continue
            tops.append(y0)
    return tops


def assert_headroom(im: Image.Image, label: str) -> None:
    tops = figure_head_tops(im)
    if not tops:
        return
    y_top = min(tops)
    if y_top < MIN_HEAD_PX:
        raise SystemExit(f"{label}: figure head too close to top (y={y_top}px, need >={MIN_HEAD_PX})")


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
