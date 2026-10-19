#!/usr/bin/env python3
"""R9: desk scene PNG — edge flood-fill bg removal, full opacity/saturation, tight trim."""

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

# Desk vignette only (no left-side arc / figures).
SCENE_BOX = (1540, 28, 2545, 712)
FLOOD_TOLERANCE = 18
TARGET_MAX_PX = 640


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


def is_background(rgb: tuple[int, int, int], bg: tuple[int, int, int], tol: int) -> bool:
    return color_dist(rgb, bg) <= tol


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
        if is_background(px[x, y][:3], bg, tol):
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


def feather_alpha_edges(rgba: Image.Image) -> Image.Image:
    """1px soft edge on fg/bg boundary only — keeps interior fully opaque."""
    w, h = rgba.size
    px = rgba.load()
    alpha = [[px[x, y][3] for x in range(w)] for y in range(h)]
    out = [[alpha[y][x] for x in range(w)] for y in range(h)]
    for y in range(h):
        for x in range(w):
            if alpha[y][x] == 0:
                continue
            edge = False
            for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                nx, ny = x + dx, y + dy
                if nx < 0 or ny < 0 or nx >= w or ny >= h or alpha[ny][nx] == 0:
                    edge = True
                    break
            if edge:
                out[y][x] = min(255, alpha[y][x])
            else:
                out[y][x] = 255
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            px[x, y] = (r, g, b, out[y][x])
    return rgba


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

    rgba = scene.convert("RGBA")
    rgba = flood_clear_background(rgba, FLOOD_TOLERANCE)
    rgba = feather_alpha_edges(rgba)

    bbox = rgba.getbbox()
    if not bbox:
        raise SystemExit("empty scene after flood fill")
    rgba = rgba.crop(bbox)

    # Portrait-ish crop (keep right/desk side) so height — not column width — drives scale.
    tw, th = rgba.size
    target_aspect = 0.92
    if tw / th > target_aspect:
        new_w = max(1, int(th * target_aspect))
        rgba = rgba.crop((tw - new_w, 0, tw, th))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    rgba.save(OUT, optimize=True)
    print(f"wrote {OUT} size={rgba.size} bbox={bbox}")


if __name__ == "__main__":
    build()
