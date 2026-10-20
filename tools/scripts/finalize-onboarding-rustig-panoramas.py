#!/usr/bin/env python3
"""Crop ultra-wide onboarding-rustig panoramas for cover hero bands (no letterbox)."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageEnhance

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama"
_args = [a for a in sys.argv[1:] if a != "--mobile-only"]
RAW = Path(_args[0]) if _args else ROOT / "artifacts/onboarding-rustig-panorama-raw"

# 2× assets: ~1140×170 desktop (6:1), 390×92 mobile (~4.24:1)
DESKTOP = (2280, 340)  # 6:1 at 2× (matches ~1140×170 content column)
MOBILE = (780, 184)  # 2× for CSS band 390×92 (~4.24:1)
CREAM = np.array([247, 244, 240], dtype=np.float32)
DESKTOP_RATIO = 6.0
MOBILE_RATIO = 780 / 184  # 390×92 CSS band at 2×
FADE_BOTTOM = 16
FADE_SIDE = 24
# 16:9 raws only (legacy / step-03): vertical band with headroom (approved step-03 crop).
Y0_FRAC_16X9 = 0.456
Y0_FRAC_SLUG: dict[str, float] = {}
PALETTE_SOFTEN_SLUGS: frozenset[str] = frozenset()
# Desktop raws authored at ~6:1 — scale to DESKTOP, no vertical crop.
BAND_NATIVE_MIN_RATIO = 5.4

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
        canvas = Image.alpha_composite(canvas.convert("RGBA"), layer).convert("RGB")
    return canvas


def is_band_native(im: Image.Image) -> bool:
    w, h = im.size
    return h > 0 and (w / h) >= BAND_NATIVE_MIN_RATIO


def fit_desktop_from_16x9(raw: Image.Image, slug: str = "") -> Image.Image:
    """Legacy: crop vertical slice from 16:9 (same rule as approved step-03)."""
    wide = extend_ultra_wide(raw)
    w, h = wide.size
    scale = DESKTOP[0] / w
    scaled_h = max(1, int(h * scale))
    scaled = wide.resize((DESKTOP[0], scaled_h), Image.Resampling.LANCZOS)
    crop_h = int(round(DESKTOP[0] / DESKTOP_RATIO))
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
            y0 = int(fb[3] - crop_h * 0.96)
    y0 = max(0, min(y0, scaled_h - crop_h))
    band = scaled.crop((0, y0, DESKTOP[0], y0 + crop_h))
    if band.size[1] != DESKTOP[1]:
        band = band.resize(DESKTOP, Image.Resampling.LANCZOS)
    return band


def fit_desktop(raw: Image.Image, slug: str = "") -> Image.Image:
    if is_band_native(raw):
        return raw.resize(DESKTOP, Image.Resampling.LANCZOS)
    return fit_desktop_from_16x9(raw, slug)


def soften_hero_palette(im: Image.Image) -> Image.Image:
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


MOBILE_FULL_WIDTH_SLUGS = frozenset({"step-04", "step-05", "step-06", "step-10"})

SEAMLESS_HERO_SLUGS = frozenset({"step-04", "step-05", "step-06"})


def _load_step04_defuse():
    import importlib.util

    spec = importlib.util.spec_from_file_location(
        "step04_band", ROOT / "tools/scripts/build-onboarding-rustig-step04-band.py"
    )
    mod = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(mod)
    return mod.defuse_right_foliage_red


def step04_defuse_jpeg(path: Path) -> None:
    """Re-run foliage defuse after JPEG save (quantization can reintroduce red specks)."""
    defuse = _load_step04_defuse()
    for _ in range(20):
        im = defuse(Image.open(path))
        im.save(path, quality=98, optimize=True)
        rgb = np.asarray(im.convert("RGB"))
        h, w = rgb.shape[:2]
        r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
        hot = (r > 110) & (r > g + 18) & (r > b + 18) & (g < 145) & (b < 145)
        if not hot[: int(h * 0.58), int(w * 0.90) :].any():
            break


def fit_mobile_band_native(raw: Image.Image, slug: str = "") -> Image.Image:
    """Crop from the same 6:1 band as desktop (one shared scene)."""
    w, h = raw.size
    if slug in MOBILE_FULL_WIDTH_SLUGS:
        band = raw
    else:
        crop_w = min(w, int(round(h * MOBILE_RATIO)))
        x0 = max(0, (w - crop_w) // 2)
        band = raw.crop((x0, 0, x0 + crop_w, h))
    if band.size != MOBILE:
        band = band.resize(MOBILE, Image.Resampling.LANCZOS)
    return band


def fit_mobile(raw: Image.Image, slug: str = "") -> Image.Image:
    """Mobile band from the same scene as desktop (6:1 center crop or aligned 16:9 slice)."""
    if is_band_native(raw):
        return fit_mobile_band_native(raw, slug)
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
        if is_band_native(raw):
            raw_mob = raw
            mob_src = src
        else:
            mob_candidates = [
                RAW / f"{slug}-mobile-raw.jpg",
                RAW / f"{slug}-mobile-raw.png",
                *candidates,
            ]
            mob_src = next((p for p in mob_candidates if p.is_file()), src)
            raw_mob = Image.open(mob_src).convert("RGB")
        mob = apply_cream_fades(fit_mobile(raw_mob, slug))
        if slug in PALETTE_SOFTEN_SLUGS:
            mob = soften_hero_palette(mob)
        if slug in SEAMLESS_HERO_SLUGS:
            mob = _load_step04_defuse()(mob)
        mob_path = OUT / f"{slug}-mobile.jpg"
        jpeg_q = 98 if slug in SEAMLESS_HERO_SLUGS else 92
        mob.save(mob_path, quality=jpeg_q, optimize=True)
        if slug in SEAMLESS_HERO_SLUGS:
            step04_defuse_jpeg(mob_path)
        if mobile_only:
            bb = figure_bbox(mob) or content_bbox(mob)
            print(f"{slug}: mob {mob.size} bbox {bb} <- {mob_src.name}")
            continue
        desk = apply_cream_fades(fit_desktop(raw, slug))
        if slug in PALETTE_SOFTEN_SLUGS:
            desk = soften_hero_palette(desk)
        if slug in SEAMLESS_HERO_SLUGS:
            desk = _load_step04_defuse()(desk)
        desk_path = OUT / f"{slug}-desktop.jpg"
        desk.save(desk_path, quality=jpeg_q, optimize=True)
        if slug in SEAMLESS_HERO_SLUGS:
            step04_defuse_jpeg(desk_path)
        bb = figure_bbox(desk) or content_bbox(desk)
        mode = "band-native" if is_band_native(raw) else "16x9-crop"
        print(
            f"{slug}: desk {desk.size} mob {mob.size} bbox {bb} "
            f"mode={mode} <- desk {src.name} mob {mob_src.name}"
        )


if __name__ == "__main__":
    main()
