#!/usr/bin/env python3
"""Remove small saturated red glyphs (e.g. lobster sprites) via local green inpaint."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

# u6p composite lobsters on desktop 2280×340 → optional manual boxes on any band size.
U6P_DESK_LOBSTER_BOXES = ((2158, 143, 2195, 185), (2215, 122, 2253, 164))
REF_DESK = (2280, 340)


def lobster_red_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (r > 110) & (r > g + 18) & (r > b + 18) & (g < 145) & (b < 145)


def scale_boxes(boxes: tuple[tuple[int, int, int, int], ...], w: int, h: int) -> list[tuple[int, int, int, int]]:
    rw, rh = REF_DESK
    sx, sy = w / rw, h / rh
    out: list[tuple[int, int, int, int]] = []
    for x0, y0, x1, y1 in boxes:
        out.append(
            (
                max(0, int(x0 * sx) - 2),
                max(0, int(y0 * sy) - 2),
                min(w - 1, int(x1 * sx) + 2),
                min(h - 1, int(y1 * sy) + 2),
            )
        )
    return out


def auto_glyph_mask(rgb: np.ndarray) -> np.ndarray:
    h, w = rgb.shape[:2]
    mask = lobster_red_mask(rgb)
    vis = np.zeros((h, w), dtype=bool)
    glyph = np.zeros((h, w), dtype=bool)
    for y in range(h):
        for x in range(w):
            if not mask[y, x] or vis[y, x]:
                continue
            stack = [(y, x)]
            xs: list[int] = []
            ys: list[int] = []
            while stack:
                cy, cx = stack.pop()
                if cy < 0 or cy >= h or cx < 0 or cx >= w or vis[cy, cx] or not mask[cy, cx]:
                    continue
                vis[cy, cx] = True
                xs.append(cx)
                ys.append(cy)
                stack.extend([(cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)])
            area = len(xs)
            if area < 12 or area > 1200:
                continue
            x0, x1 = min(xs), max(xs)
            y0, y1 = min(ys), max(ys)
            cxm = (x0 + x1) / 2
            if cxm < w * 0.82 or y1 > h * 0.62:
                continue
            bh, bw = y1 - y0 + 1, x1 - x0 + 1
            if bh > 55 or bw > 55:
                continue
            glyph[y0 : y1 + 1, x0 : x1 + 1] |= mask[y0 : y1 + 1, x0 : x1 + 1]
    return glyph


def box_mask(h: int, w: int, boxes: list[tuple[int, int, int, int]]) -> np.ndarray:
    m = np.zeros((h, w), dtype=bool)
    for x0, y0, x1, y1 in boxes:
        m[y0 : y1 + 1, x0 : x1 + 1] = True
    return m


def inpaint_glyphs(im: Image.Image) -> Image.Image:
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    h, w = rgb.shape[:2]
    boxes = scale_boxes(U6P_DESK_LOBSTER_BOXES, w, h)
    todo = auto_glyph_mask(rgb.astype(np.uint8)) | box_mask(h, w, boxes)
    if not todo.any():
        return im
    out = rgb.copy()
    ys, xs = np.where(todo)
    for y, x in zip(ys, xs, strict=True):
        acc = np.zeros(3, dtype=np.float64)
        n = 0
        for dy in range(-6, 7):
            for dx in range(-6, 7):
                if dx == 0 and dy == 0:
                    continue
                ny, nx = y + dy, x + dx
                if ny < 0 or ny >= h or nx < 0 or nx >= w or todo[ny, nx]:
                    continue
                acc += out[ny, nx]
                n += 1
        if n:
            out[y, x] = acc / n
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def main() -> None:
    for path_str in sys.argv[1:]:
        path = Path(path_str)
        fixed = inpaint_glyphs(Image.open(path))
        fixed.save(path, quality=95 if path.suffix.lower() in (".jpg", ".jpeg") else None)
        rgb = np.asarray(fixed.convert("RGB"))
        h, w = rgb.shape[:2]
        red = int(lobster_red_mask(rgb)[:, int(w * 0.88) :].sum())
        print(f"{path.name}: retouched; red px x>=0.88 = {red}")


if __name__ == "__main__":
    main()
