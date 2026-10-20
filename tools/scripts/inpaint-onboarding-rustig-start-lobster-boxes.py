#!/usr/bin/env python3
"""Inpaint fence + rock lobster regions on start hero assets (start-b)."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DESK = (2280, 340)
MOB = (780, 184)

# Composite fence sprite on hero bands (desktop coords).
DESK_FENCE_BOX = (18, 72, 310, 290)
# Painted rock mascot visible in mobile band (780×184 coords).
MOB_ROCK_BOX = (42, 38, 148, 158)
# Painted rock mascot on 16:9 raw (1280×720 coords).
RAW_ROCK_BOX = (80, 320, 520, 720)


def scale_boxes(
    boxes: tuple[tuple[int, int, int, int], ...], ref: tuple[int, int], target: tuple[int, int]
) -> list[tuple[int, int, int, int]]:
    rw, rh = ref
    tw, th = target
    sx, sy = tw / rw, th / rh
    out: list[tuple[int, int, int, int]] = []
    for x0, y0, x1, y1 in boxes:
        out.append(
            (
                max(0, int(x0 * sx) - 2),
                max(0, int(y0 * sy) - 2),
                min(tw - 1, int(x1 * sx) + 2),
                min(th - 1, int(y1 * sy) + 2),
            )
        )
    return out


def box_mask(h: int, w: int, boxes: list[tuple[int, int, int, int]]) -> np.ndarray:
    m = np.zeros((h, w), dtype=bool)
    for x0, y0, x1, y1 in boxes:
        m[y0 : y1 + 1, x0 : x1 + 1] = True
    return m


def inpaint(im: Image.Image, todo: np.ndarray) -> Image.Image:
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    h, w = rgb.shape[:2]
    out = rgb.copy()
    ys, xs = np.where(todo)
    for y, x in zip(ys, xs, strict=True):
        acc = np.zeros(3, dtype=np.float64)
        n = 0
        for dy in range(-10, 11):
            for dx in range(-10, 11):
                ny, nx = y + dy, x + dx
                if ny < 0 or ny >= h or nx < 0 or nx >= w or todo[ny, nx]:
                    continue
                acc += out[ny, nx]
                n += 1
        if n:
            out[y, x] = acc / n
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def patch(path: Path, boxes: list[tuple[int, int, int, int]]) -> None:
    im = Image.open(path).convert("RGB")
    w, h = im.size
    todo = box_mask(h, w, boxes)
    fixed = inpaint(im, todo)
    fixed.save(path, quality=95 if path.suffix.lower() in (".jpg", ".jpeg") else None)
    print(f"{path.name}: inpainted {int(todo.sum())} px in {len(boxes)} box(es)")


def main() -> None:
    desk_path = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/start-desktop.jpg"
    mob_path = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/start-mobile.jpg"
    raw_path = ROOT / "artifacts/onboarding-rustig-panorama-raw/start-desktop-raw.jpg"
    patch(desk_path, [DESK_FENCE_BOX])
    patch(mob_path, [*scale_boxes((DESK_FENCE_BOX,), DESK, MOB), MOB_ROCK_BOX])
    if raw_path.is_file():
        rw, rh = Image.open(raw_path).size
        patch(raw_path, scale_boxes((RAW_ROCK_BOX,), (1280, 720), (rw, rh)))


if __name__ == "__main__":
    main()
