#!/usr/bin/env python3
"""R12: purpose-built square tips vignette — clock, plant, mug, soft desk (no wide crop)."""

from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SRC_BAND = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated/beschikbaarheid.jpg"
SRC_WIDE = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated/beschikbaarheid-profession-wide.jpg"
OUT = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated/mob-figures/tips-moment-vignette-mobile.png"

CANVAS = 520
MARGIN = 32
FLOOD_TOL = 18

# Object crops (separate boxes — never a wide desk strip).
REGIONS_BAND: dict[str, tuple[int, int, int, int]] = {
    "mug": (1075, 555, 1190, 680),
    "plant": (1170, 465, 1278, 620),
}
REGIONS_WIDE: dict[str, tuple[int, int, int, int]] = {
    "clock": (2100, 110, 2250, 260),
}


def color_dist(a: tuple[int, ...], b: tuple[int, ...]) -> int:
    return abs(a[0] - b[0]) + abs(a[1] - b[1]) + abs(a[2] - b[2])


def sample_edge_background(rgb: Image.Image) -> tuple[int, int, int]:
    w, h = rgb.size
    px = rgb.load()
    samples: list[tuple[int, int, int]] = []
    for x in range(w):
        samples.append(px[x, 0][:3])
        samples.append(px[x, h - 1][:3])
    for y in range(h):
        samples.append(px[0, y][:3])
        samples.append(px[w - 1, y][:3])
    rs = sorted(s[0] for s in samples)
    gs = sorted(s[1] for s in samples)
    bs = sorted(s[2] for s in samples)
    mid = len(samples) // 2
    return rs[mid], gs[mid], bs[mid]


def flood_clear_background(rgba: Image.Image, tol: int) -> Image.Image:
    rgb = rgba.convert("RGB")
    bg = sample_edge_background(rgb)
    w, h = rgba.size
    px = rgba.load()
    seen = bytearray(w * h)
    q: deque[tuple[int, int]] = deque()

    def push(x: int, y: int) -> None:
        i = y * w + x
        if seen[i]:
            return
        seen[i] = 1
        if color_dist(px[x, y][:3], bg) <= tol:
            q.append((x, y))

    for x in range(w):
        push(x, 0)
        push(x, h - 1)
    for y in range(h):
        push(0, y)
        push(w - 1, y)

    while q:
        x, y = q.popleft()
        px[x, y] = (px[x, y][0], px[x, y][1], px[x, y][2], 0)
        if x > 0:
            push(x - 1, y)
        if x + 1 < w:
            push(x + 1, y)
        if y > 0:
            push(x, y - 1)
        if y + 1 < h:
            push(x, y + 1)

    for y in range(h):
        for x in range(w):
            if px[x, y][3] > 0:
                r, g, b, _ = px[x, y]
                px[x, y] = (r, g, b, 255)
    return rgba


def cut(src: Image.Image, box: tuple[int, int, int, int]) -> Image.Image:
    return flood_clear_background(src.crop(box).convert("RGBA"), FLOOD_TOL)


def soft_desk_layer(w: int, h: int) -> Image.Image:
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.rounded_rectangle((6, h // 3, w - 6, h - 4), radius=30, fill=(196, 150, 112, 255))
    draw.rounded_rectangle((16, h // 3 + 10, w - 16, h - 12), radius=24, fill=(212, 168, 124, 255))
    return layer


def scale_to(img: Image.Image, max_w: int, max_h: int) -> Image.Image:
    w, h = img.size
    s = min(max_w / w, max_h / h, 1.0)
    if s >= 0.999:
        return img
    return img.resize((max(1, int(w * s)), max(1, int(h * s))), Image.Resampling.LANCZOS)


def paste_centered(canvas: Image.Image, sprite: Image.Image, cx: int, cy: int) -> None:
    w, h = sprite.size
    canvas.paste(sprite, (cx - w // 2, cy - h // 2), sprite)


def build() -> None:
    band = Image.open(SRC_BAND).convert("RGB")
    wide = Image.open(SRC_WIDE).convert("RGB")

    clock = cut(wide, REGIONS_WIDE["clock"])
    plant = cut(band, REGIONS_BAND["plant"])
    mug = cut(band, REGIONS_BAND["mug"])

    canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))

    desk_w = CANVAS - 2 * MARGIN
    desk_h = int(CANVAS * 0.24)
    desk_cy = CANVAS - MARGIN - int(desk_h * 0.35)
    desk_layer = soft_desk_layer(desk_w, desk_h)
    canvas.paste(desk_layer, (MARGIN, desk_cy - desk_h // 2), desk_layer)

    plant_s = scale_to(plant, 210, 250)
    paste_centered(canvas, plant_s, CANVAS - MARGIN - 24, desk_cy - plant_s.height // 2 - 22)

    mug_s = scale_to(mug, 135, 145)
    paste_centered(canvas, mug_s, CANVAS - MARGIN - 78, desk_cy - 6)

    clock_s = scale_to(clock, 165, 165)
    paste_centered(canvas, clock_s, CANVAS - MARGIN - 36, MARGIN + clock_s.height // 2 + 8)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUT, optimize=True)
    print(f"wrote {OUT} size={canvas.size}")


if __name__ == "__main__":
    build()
