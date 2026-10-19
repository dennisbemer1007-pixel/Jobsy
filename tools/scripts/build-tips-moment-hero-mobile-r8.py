#!/usr/bin/env python3
"""R8: tips mobile desk scene PNG (transparent) — Bewaard-style right slot, no hard panel rect."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageFilter

PEARL = (247, 244, 240)
SRC = (
    Path(__file__).resolve().parents[2]
    / "Jobsy.Web/wwwroot/images/candidate-illustrated/beschikbaarheid-profession-wide.jpg"
)
OUT = (
    Path(__file__).resolve().parents[2]
    / "Jobsy.Web/wwwroot/images/candidate-illustrated/mob-figures/tips-moment-desk-scene-mobile.png"
)

# Right-hand desk vignette from wide band (exclude left figures / arc).
SCENE_BOX = (1480, 8, 2520, 710)
OUT_W, OUT_H = 520, 520
KEY_TOLERANCE = 42


def color_dist(a: tuple[int, ...], b: tuple[int, ...]) -> float:
    return abs(a[0] - b[0]) + abs(a[1] - b[1]) + abs(a[2] - b[2])


def key_background(rgba: Image.Image) -> Image.Image:
    w, h = rgba.size
    px = rgba.load()
    refs = [
        px[2, 2][:3],
        px[w - 3, 2][:3],
        px[2, h - 3][:3],
        px[w - 3, h - 3][:3],
        PEARL,
    ]

    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            rgb = (r, g, b)
            nearest = min(color_dist(rgb, ref) for ref in refs)
            if nearest <= KEY_TOLERANCE:
                px[x, y] = (r, g, b, 0)
            elif nearest <= KEY_TOLERANCE + 28:
                fade = (nearest - KEY_TOLERANCE) / 28.0
                alpha = int(a * fade)
                px[x, y] = (r, g, b, alpha)

    return rgba.filter(ImageFilter.GaussianBlur(radius=0.6))


def trim_transparent(rgba: Image.Image, pad: int = 8) -> Image.Image:
    bbox = rgba.getbbox()
    if not bbox:
        return rgba
    x0, y0, x1, y1 = bbox
    x0 = max(0, x0 - pad)
    y0 = max(0, y0 - pad)
    x1 = min(rgba.width, x1 + pad)
    y1 = min(rgba.height, y1 + pad)
    return rgba.crop((x0, y0, x1, y1))


def build() -> None:
    src = Image.open(SRC).convert("RGB")
    scene = src.crop(SCENE_BOX)

    scale = min(OUT_W / scene.width, OUT_H / scene.height)
    tw = max(1, int(scene.width * scale))
    th = max(1, int(scene.height * scale))
    scene = scene.resize((tw, th), Image.Resampling.LANCZOS)

    canvas = Image.new("RGBA", (OUT_W, OUT_H), (0, 0, 0, 0))
    paste_x = OUT_W - tw
    paste_y = OUT_H - th
    canvas.paste(scene.convert("RGBA"), (paste_x, paste_y))

    canvas = key_background(canvas)
    canvas = trim_transparent(canvas, pad=4)

    # Final canvas: anchor content bottom-right like Bewaard mob figures.
    final = Image.new("RGBA", (OUT_W, OUT_H), (0, 0, 0, 0))
    tw, th = canvas.size
    scale2 = min(OUT_W / tw, OUT_H / th) * 0.98
    nw, nh = max(1, int(tw * scale2)), max(1, int(th * scale2))
    canvas = canvas.resize((nw, nh), Image.Resampling.LANCZOS)
    final.paste(canvas, (OUT_W - nw, OUT_H - nh), canvas)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    final.save(OUT, optimize=True)
    print(f"wrote {OUT} size={final.size}")


if __name__ == "__main__":
    build()
