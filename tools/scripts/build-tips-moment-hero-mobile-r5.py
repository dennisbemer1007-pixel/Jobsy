#!/usr/bin/env python3
"""R5: single continuous mobile tips hero (780×520) — crop from beschikbaarheid.jpg."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

PEARL = (247, 244, 240)
SRC = (
    Path(__file__).resolve().parents[2]
    / "Jobsy.Web/wwwroot/images/candidate-illustrated/beschikbaarheid.jpg"
)
OUT = SRC.parent / "tips-moment-hero-mobile.jpg"

MOB_W, MOB_H = 780, 520
# Older man + desk; excludes baby carrier and second parent on the far left.
SOURCE_CROP = (380, 0, 1280, 720)


def build() -> None:
    src = Image.open(SRC).convert("RGB")
    scene = src.crop(SOURCE_CROP)
    scale = MOB_H / scene.height
    target_w = int(scene.width * scale)
    target_h = MOB_H
    scene = scene.resize((target_w, target_h), Image.Resampling.LANCZOS)

    canvas = Image.new("RGB", (MOB_W, MOB_H), PEARL)
    paste_x = MOB_W - target_w
    canvas.paste(scene, (paste_x, 0))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUT, quality=92, optimize=True)
    print(f"wrote {OUT} ({MOB_W}x{MOB_H}) paste_x={paste_x}")


if __name__ == "__main__":
    build()
