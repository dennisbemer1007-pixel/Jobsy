#!/usr/bin/env python3
"""R11: narrow desk vignette (no calendar/notebook) — fits right slot with contain."""

from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image

SRC = (
    Path(__file__).resolve().parents[2]
    / "Jobsy.Web/wwwroot/images/candidate-illustrated/beschikbaarheid-profession-wide.jpg"
)
OUT = (
    Path(__file__).resolve().parents[2]
    / "Jobsy.Web/wwwroot/images/candidate-illustrated/mob-figures/tips-moment-desk-scene-mobile.png"
)

# Plant, mug, desk (+ wall clock above) — no calendar or notebook.
SCENE_BOX = (2375, 52, 2548, 702)
FLOOD_TOLERANCE = 18
MARGIN_PX = 8
TARGET_MAX_PX = 560


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

    return rgba


def solidify(rgba: Image.Image) -> Image.Image:
    px = rgba.load()
    w, h = rgba.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a > 0:
                px[x, y] = (r, g, b, 255)
    return rgba


def pad_margin(rgba: Image.Image, margin: int) -> Image.Image:
    w, h = rgba.size
    out = Image.new("RGBA", (w + 2 * margin, h + 2 * margin), (0, 0, 0, 0))
    out.paste(rgba, (margin, margin), rgba)
    return out


def build() -> None:
    src = Image.open(SRC).convert("RGB")
    scene = src.crop(SCENE_BOX)
    long_side = max(scene.size)
    if long_side < TARGET_MAX_PX:
        scale = TARGET_MAX_PX / long_side
        scene = scene.resize(
            (max(1, int(scene.width * scale)), max(1, int(scene.height * scale))),
            Image.Resampling.LANCZOS,
        )

    rgba = solidify(flood_clear_background(scene.convert("RGBA"), FLOOD_TOLERANCE))
    bbox = rgba.getbbox()
    if not bbox:
        raise SystemExit("empty after flood")
    rgba = pad_margin(rgba.crop(bbox), MARGIN_PX)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    rgba.save(OUT, optimize=True)
    print(f"wrote {OUT} size={rgba.size}")


if __name__ == "__main__":
    build()
