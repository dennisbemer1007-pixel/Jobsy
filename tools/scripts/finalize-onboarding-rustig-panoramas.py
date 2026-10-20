#!/usr/bin/env python3
"""Crop ultra-wide onboarding-rustig panoramas for cover hero bands (no letterbox)."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama"
_args = [a for a in sys.argv[1:] if a != "--mobile-only"]
RAW = Path(_args[0]) if _args else ROOT / "artifacts/onboarding-rustig-panorama-raw"

# 2× assets: ~1140×170 desktop (6:1), 390×92 mobile (~4:1)
DESKTOP = (2280, 340)  # 6:1 at 2× (matches ~1140×170 content column)
MOBILE = (780, 184)  # 2× for CSS band 390×92 (~4.24:1)
CREAM = np.array([247, 244, 240], dtype=np.float32)
DESKTOP_RATIO = 6.0
MOBILE_RATIO = 780 / 184  # 390×92 CSS band at 2×
HEAD_MARGIN = 14
MIN_HEAD_ROW_FRAC = 0.165  # every head below top ~16.5% of hero band
FEET_ABOVE_FADE = 25
FADE_BOTTOM = 16
FADE_SIDE = 24
# 16:9 raws: vertical band with headroom (tuned vs approved step-03 crop).
Y0_FRAC_16X9 = 0.456
Y0_FRAC_SLUG: dict[str, float] = {
    "step-02": 0.488,
    "step-04": 0.478,
    "step-05": 0.488,
}
PALETTE_SOFTEN_SLUGS = frozenset({"step-02", "step-04", "step-05"})

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


def figure_head_tops_band(band: Image.Image) -> list[int]:
    """Same slice head-row logic as verify-onboarding-rustig-capture-heads.py."""
    rgb = np.asarray(band.convert("RGB"), dtype=np.float32)
    dist = np.linalg.norm(rgb - CREAM, axis=2)
    h, w = dist.shape
    y_lo = max(0, int(h * 0.02))
    row_occ = 0.05

    def region_top(x0_frac: float, x1_frac: float) -> int | None:
        x0, x1 = int(w * x0_frac), max(int(w * x1_frac), int(w * x0_frac) + 1)
        sub = dist[:, x0:x1]
        peak_y, peak_score = None, 0.0
        for y in range(y_lo, h):
            score = float((sub[y] > 28).mean())
            if score >= row_occ and score >= peak_score:
                peak_score = score
                peak_y = y
        if peak_y is None:
            return None
        for y in range(y_lo, peak_y + 1):
            if float((sub[y] > 28).mean()) >= row_occ * 0.65:
                return y
        return peak_y

    tops: list[int] = []
    for a, b in [(0.04, 0.28), (0.28, 0.52), (0.52, 0.76), (0.76, 0.96)]:
        t = region_top(a, b)
        if t is not None:
            tops.append(t)
    return tops


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
    slug: str = "",
) -> Image.Image:
    """Scale to cover width, crop vertical band keeping figure bbox inside."""
    wide = extend_ultra_wide(im)
    w, h = wide.size
    scale = target_w / w
    scaled_h = max(1, int(h * scale))
    scaled = wide.resize((target_w, scaled_h), Image.Resampling.LANCZOS)
    crop_h = int(round(target_w / ratio))
    crop_h = min(crop_h, scaled_h)
    wide_ratio = w / h
    if 1.70 <= wide_ratio <= 1.85:
        frac = Y0_FRAC_SLUG.get(slug, Y0_FRAC_16X9)
        y0 = int(scaled_h * frac)
    else:
        fb = figure_bbox(scaled)
        if fb is None:
            y0 = max(0, int(scaled_h * 0.42) - crop_h // 2)
        else:
            _fx1, _fy1, _fx2, fy2 = fb
            y0 = int(fy2 - crop_h * 0.96)
    y0 = max(0, min(y0, scaled_h - crop_h))
    band = scaled.crop((0, y0, target_w, y0 + crop_h))
    if band.size[1] != target_h:
        band = band.resize((target_w, target_h), Image.Resampling.LANCZOS)
    return band


def soften_hero_palette(im: Image.Image) -> Image.Image:
    """Nudge saturated raws toward approved step-03 pale watercolor."""
    im = ImageEnhance.Color(im).enhance(0.72)
    im = ImageEnhance.Brightness(im).enhance(1.06)
    im = ImageEnhance.Contrast(im).enhance(0.92)
    return im


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
                a = max(a, int(150 * t))
            elif x >= w - FADE_SIDE:
                t = (x - (w - FADE_SIDE)) / max(1, FADE_SIDE - 1)
                a = max(a, int(150 * t))
            if a:
                px[x, y] = (*cream, a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def fit_desktop(raw: Image.Image, slug: str = "") -> Image.Image:
    return crop_to_ratio(raw, DESKTOP[0], DESKTOP[1], DESKTOP_RATIO, slug=slug)


def fit_mobile(raw: Image.Image, slug: str = "") -> Image.Image:
    """Same vertical slice of the raw as desktop (aligned 92px mobile band)."""
    wide = extend_ultra_wide(raw)
    w, h = wide.size
    scale_d = DESKTOP[0] / w
    scaled_h_d = max(1, int(h * scale_d))
    crop_h_d = min(int(round(DESKTOP[0] / DESKTOP_RATIO)), scaled_h_d)
    wide_ratio = w / h
    if 1.70 <= wide_ratio <= 1.85:
        frac = Y0_FRAC_SLUG.get(slug, Y0_FRAC_16X9)
        y0_d = int(scaled_h_d * frac)
    else:
        scaled_d = wide.resize((DESKTOP[0], scaled_h_d), Image.Resampling.LANCZOS)
        fb = figure_bbox(scaled_d)
        if fb is None:
            y0_d = max(0, int(scaled_h_d * 0.42) - crop_h_d // 2)
        else:
            y0_d = int(fb[3] - crop_h_d * 0.96)
    scale_m = MOBILE[0] / w
    scaled_h_m = max(1, int(h * scale_m))
    crop_h_m = min(int(round(MOBILE[0] / MOBILE_RATIO)), scaled_h_m)
    y0_m = int(y0_d * (MOBILE[0] / DESKTOP[0]))
    y0_m = max(0, min(y0_m, scaled_h_m - crop_h_m))
    scaled_m = wide.resize((MOBILE[0], scaled_h_m), Image.Resampling.LANCZOS)
    band = scaled_m.crop((0, y0_m, MOBILE[0], y0_m + crop_h_m))
    if band.size[1] != MOBILE[1]:
        band = band.resize(MOBILE, Image.Resampling.LANCZOS)
    return band


def main() -> None:
    mobile_only = "--mobile-only" in sys.argv
    slug_filter = [a for a in _args[1:] if not a.startswith("-")]
    targets = slug_filter if slug_filter else SLUGS
    OUT.mkdir(parents=True, exist_ok=True)
    for slug in targets:
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
        mob = apply_cream_fades(fit_mobile(raw, slug))
        if slug in PALETTE_SOFTEN_SLUGS:
            mob = soften_hero_palette(mob)
        mob.save(OUT / f"{slug}-mobile.jpg", quality=92, optimize=True)
        if mobile_only:
            bb = figure_bbox(mob) or content_bbox(mob)
            print(f"{slug}: mob {mob.size} bbox {bb} <- {src.name}")
            continue
        desk = apply_cream_fades(fit_desktop(raw, slug))
        if slug in PALETTE_SOFTEN_SLUGS:
            desk = soften_hero_palette(desk)
        desk.save(OUT / f"{slug}-desktop.jpg", quality=92, optimize=True)
        bb = figure_bbox(desk) or content_bbox(desk)
        print(f"{slug}: desk {desk.size} mob {mob.size} bbox {bb} <- {src.name}")


if __name__ == "__main__":
    main()
