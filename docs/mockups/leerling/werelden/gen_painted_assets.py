#!/usr/bin/env python3
"""Generate watercolor-style painted JPG assets for screens 12 & 13 (mockups only)."""
from __future__ import annotations

import os
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageOps

ROOT = Path(__file__).resolve().parent
SRC = ROOT / "src" / "img"
OUT = SRC / "painted"

WORLDS = ("zee", "tuin", "ster")
JOBS = (
    "dierenverzorger",
    "bioloog",
    "grafisch-ontwerper",
    "hovenier",
    "brandweer",
)
DAY_MOMENTS = ("start", "morning", "pause", "afternoon", "close")

# Source crops per subject (file, center x 0-1, center y 0-1, scale)
JOB_SOURCES = {
    "dierenverzorger": ("rif.jpg", 0.55, 0.45, 1.0),
    "bioloog": ("oceaan.jpg", 0.4, 0.35, 1.05),
    "grafisch-ontwerper": ("lagune.jpg", 0.6, 0.5, 1.0),
    "hovenier": ("dot-tuin-5.jpg", 0.5, 0.45, 1.0),
    "brandweer": ("vuurtoren.jpg", 0.5, 0.55, 1.0),
}

DAY_HERO = ("eiland.jpg", 0.5, 0.42, 1.0)

DAY_SCENE_SOURCES = {
    "start": ("rif.jpg", 0.35, 0.4),
    "morning": ("tuin-baai.jpg", 0.55, 0.35),
    "pause": ("dot-tuin-3.jpg", 0.5, 0.5),
    "afternoon": ("haven.jpg", 0.45, 0.5),
    "close": ("vuurtoren.jpg", 0.5, 0.45),
}

WORLD_WASH = {
    "zee": ((120, 200, 195), 0.22, (15, 91, 102), 0.12),
    "tuin": ((232, 198, 160), 0.2, (120, 168, 110), 0.16),
    "ster": ((120, 140, 180), 0.2, (232, 200, 140), 0.14),
}


def _open_crop(name: str, cx: float, cy: float, scale: float, out_w: int, out_h: int) -> Image.Image:
    path = SRC / name
    im = Image.open(path).convert("RGB")
    w, h = im.size
    # cover crop
    target_ratio = out_w / out_h
    src_ratio = w / h
    if src_ratio > target_ratio:
        nh = int(h / scale)
        nw = int(nh * target_ratio)
    else:
        nw = int(w / scale)
        nh = int(nw / target_ratio)
    left = int(cx * w - nw / 2)
    top = int(cy * h - nh / 2)
    left = max(0, min(w - nw, left))
    top = max(0, min(h - nh, top))
    box = (left, top, left + nw, top + nh)
    im = im.crop(box).resize((out_w, out_h), Image.Resampling.LANCZOS)
    return im


def _paper_grain(w: int, h: int, seed: int) -> Image.Image:
    rnd = random.Random(seed)
    layer = Image.new("L", (w, h))
    px = layer.load()
    for y in range(h):
        for x in range(w):
            px[x, y] = rnd.randint(218, 255)
    return layer.filter(ImageFilter.GaussianBlur(1.2))


def _watercolor(im: Image.Image, world: str, seed: int) -> Image.Image:
    w, h = im.size
    base = im.copy()
    base = base.filter(ImageFilter.GaussianBlur(radius=1.8))
    base = ImageEnhance.Color(base).enhance(1.28)
    base = ImageEnhance.Sharpness(base).enhance(0.85)
    # soft paint blocks
    painted = base.quantize(colors=36, method=Image.Quantize.MEDIANCUT).convert("RGB")
    painted = painted.filter(ImageFilter.GaussianBlur(radius=0.7))
    base = Image.blend(base, painted, 0.55)

    c1, a1, c2, a2 = WORLD_WASH[world]
    base = Image.blend(base, Image.new("RGB", (w, h), c1), a1)
    base = Image.blend(base, Image.new("RGB", (w, h), c2), a2)

    grain = _paper_grain(w, h, seed)
    paper = Image.new("RGB", (w, h), (252, 248, 242))
    grain_mask = grain.point(lambda v: max(0, min(36, int((235 - v) * 0.3))))
    base = Image.composite(paper, base, grain_mask)

    vig = Image.new("L", (w, h), 255)
    draw = ImageDraw.Draw(vig)
    draw.ellipse((-w * 0.06, -h * 0.1, w * 1.06, h * 1.12), fill=210)
    vig = vig.filter(ImageFilter.GaussianBlur(radius=min(w, h) * 0.11))
    darker = ImageEnhance.Brightness(base).enhance(0.9)
    base = Image.composite(base, darker, vig.point(lambda v: max(0, min(48, int((255 - v) * 0.22)))))

    return base


def _save(im: Image.Image, path: Path, quality: int = 88) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    im.save(path, "JPEG", quality=quality, optimize=True)


def generate() -> None:
    seed = 9127
    for world in WORLDS:
        for job in JOBS:
            src, cx, cy, sc = JOB_SOURCES[job]
            im = _open_crop(src, cx, cy, sc, 520, 360)
            painted = _watercolor(im, world, seed)
            seed += 1
            _save(painted, OUT / "jobs" / world / f"{job}.jpg")

        # Day-in-life hero (dierenverzorger only in mockups)
        src, cx, cy, sc = DAY_HERO
        hero = _open_crop(src, cx, cy, sc, 960, 380)
        painted = _watercolor(hero, world, seed)
        seed += 1
        _save(painted, OUT / "day-hero" / world / "dierenverzorger.jpg", quality=90)

        for moment in DAY_MOMENTS:
            src, cx, cy = DAY_SCENE_SOURCES[moment]
            im = _open_crop(src, cx, cy, 1.0, 280, 280)
            painted = _watercolor(im, world, seed)
            seed += 1
            _save(painted, OUT / "day-scenes" / world / f"{moment}.jpg")

    print(f"Wrote painted assets under {OUT.relative_to(ROOT)}")


if __name__ == "__main__":
    generate()
