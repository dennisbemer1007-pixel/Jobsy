#!/usr/bin/env python3
"""Remove red lobster glyphs from start hero rasters (raw or band JPEGs)."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]


def lobster_red_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (r > 105) & (r > g + 16) & (r > b + 16) & (g < 140) & (b < 140)


def glyph_mask(rgb: np.ndarray, *, max_cx_frac: float = 0.42) -> np.ndarray:
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
            if area < 10 or area > 8000:
                continue
            x0, x1 = min(xs), max(xs)
            y0, y1 = min(ys), max(ys)
            cxm = (x0 + x1) / 2 / w
            if cxm > max_cx_frac:
                continue
            bh, bw = y1 - y0 + 1, x1 - x0 + 1
            if bh > h * 0.45 or bw > w * 0.22:
                continue
            glyph[y0 : y1 + 1, x0 : x1 + 1] |= mask[y0 : y1 + 1, x0 : x1 + 1]
    return glyph


def inpaint(im: Image.Image, todo: np.ndarray) -> Image.Image:
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    h, w = rgb.shape[:2]
    if not todo.any():
        return im
    out = rgb.copy()
    ys, xs = np.where(todo)
    for y, x in zip(ys, xs, strict=True):
        acc = np.zeros(3, dtype=np.float64)
        n = 0
        for dy in range(-8, 9):
            for dx in range(-8, 9):
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


def clean(path: Path) -> None:
    im = Image.open(path).convert("RGB")
    rgb = np.asarray(im)
    todo = glyph_mask(rgb)
    fixed = inpaint(im, todo)
    fixed.save(path, quality=95 if path.suffix.lower() in (".jpg", ".jpeg") else None)
    left_red = int(lobster_red_mask(np.asarray(fixed))[:, : int(fixed.size[0] * 0.45)].sum())
    print(f"{path.name}: removed {int(todo.sum())} px; left red x<45% = {left_red}")


def main() -> None:
    paths = [Path(p) for p in sys.argv[1:]]
    if not paths:
        paths = [
            ROOT / "artifacts/onboarding-rustig-panorama-raw/start-desktop-raw.jpg",
            ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/start-desktop.jpg",
            ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/start-mobile.jpg",
        ]
    for path in paths:
        if not path.is_file():
            raise SystemExit(f"missing {path}")
        clean(path)


if __name__ == "__main__":
    main()
