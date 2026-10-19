#!/usr/bin/env python3
"""R7: tips-only mobile panel (780×520) — cream left 45%, full desk scene right with margins."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

PEARL = (247, 244, 240)
SRC = (
    Path(__file__).resolve().parents[2]
    / "Jobsy.Web/wwwroot/images/candidate-illustrated/beschikbaarheid.jpg"
)
OUT = SRC.parent / "tips-moment-hero-mobile-panel.jpg"

MOB_W, MOB_H = 780, 520
LEFT_FRAC = 0.45
MARGIN = 26
SCENE_BOX = (560, 35, 1280, 720)


def build() -> None:
    src = Image.open(SRC).convert("RGB")
    scene = src.crop(SCENE_BOX)

    left_w = int(MOB_W * LEFT_FRAC)
    canvas = Image.new("RGB", (MOB_W, MOB_H), PEARL)

    avail_w = MOB_W - left_w - MARGIN
    avail_h = MOB_H - 2 * MARGIN
    scale = min(avail_w / scene.width, avail_h / scene.height)
    tw = max(1, int(scene.width * scale))
    th = max(1, int(scene.height * scale))
    scene = scene.resize((tw, th), Image.Resampling.LANCZOS)

    paste_x = left_w + (avail_w - tw) // 2
    paste_y = MOB_H - th - MARGIN
    canvas.paste(scene, (paste_x, paste_y))

    # Feather blend at cream/scene boundary (in-image, not CSS over objects)
    overlay = Image.new("RGBA", (MOB_W, MOB_H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    fade_w = 36
    x0 = left_w - fade_w // 2
    for x in range(max(0, x0), min(MOB_W, left_w + fade_w)):
        t = max(0.0, min(1.0, (x - x0) / fade_w))
        alpha = int(255 * (1.0 - t) * 0.35)
        draw.line([(x, 0), (x, MOB_H)], fill=(PEARL[0], PEARL[1], PEARL[2], alpha))
    canvas = Image.alpha_composite(canvas.convert("RGBA"), overlay).convert("RGB")

    OUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUT, quality=93, optimize=True)
    print(f"wrote {OUT} scene={tw}x{th} at ({paste_x},{paste_y})")


if __name__ == "__main__":
    build()
