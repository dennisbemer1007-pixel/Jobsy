#!/usr/bin/env python3
"""Build desktop learn-grow hero: full mobile scene + pearl extension, soft background seam."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

PEARL = (247, 244, 240)
SRC = Path(__file__).resolve().parents[2] / "Jobsy.Web/wwwroot/images/candidate-illustrated/leren-groeien-hero.jpg"
OUT = SRC.parent / "leren-groeien-hero-wide.jpg"

SCENE_W = 1280
SCENE_H = 720
OUT_W = 1920
SEAM_BLEND = 180  # px at scene left edge: feather background only


def is_background(r: int, g: int, b: int) -> bool:
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    mx, mn = max(r, g, b), min(r, g, b)
    sat = (mx - mn) / mx if mx else 0.0
    return lum >= 188 and sat <= 0.22


def main() -> None:
    scene = Image.open(SRC).convert("RGB")
    if scene.size != (SCENE_W, SCENE_H):
        scene = scene.resize((SCENE_W, SCENE_H), Image.Resampling.LANCZOS)

    canvas = Image.new("RGB", (OUT_W, SCENE_H), PEARL)
    scene_x = OUT_W - SCENE_W
    canvas.paste(scene, (scene_x, 0))

    pix = canvas.load()
    pr, pg, pb = PEARL
    for x in range(scene_x, scene_x + SEAM_BLEND):
        f = 1.0 - (x - scene_x) / SEAM_BLEND  # pearl at seam, scene at end
        for y in range(SCENE_H):
            r, g, b = pix[x, y]
            if not is_background(r, g, b):
                continue
            pix[x, y] = (
                int(pr * f + r * (1 - f)),
                int(pg * f + g * (1 - f)),
                int(pb * f + b * (1 - f)),
            )

    OUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUT, quality=92, optimize=True)
    print(f"wrote {OUT} size={canvas.size}")


if __name__ == "__main__":
    main()
