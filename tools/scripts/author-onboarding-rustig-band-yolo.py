#!/usr/bin/env python3
"""Pick a 6:1 vertical slice from 16:9 using YOLO person margin rules (step-03 scale)."""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

TOP_MIN = 0.12
BOTTOM_MAX = 0.92
OUT_SIZE = (2400, 400)
DESK_SIZE = (2280, 340)


def load_yolo():
    from ultralytics import YOLO

    root = Path(__file__).resolve().parents[2]
    w = root / "yolov8n.pt"
    return YOLO(str(w if w.is_file() else "yolov8n.pt"))


def persons(model, im: Image.Image, conf: float = 0.18):
    arr = np.asarray(im.convert("RGB"))
    h, w = arr.shape[:2]
    res = model(arr, verbose=False)[0]
    out = []
    if res.boxes is None:
        return out
    for b in res.boxes:
        if int(b.cls[0]) != 0:
            continue
        if float(b.conf[0]) < conf:
            continue
        x1, y1, x2, y2 = (float(v) for v in b.xyxy[0])
        out.append((y1 / h, y2 / h, float(b.conf[0])))
    return out


def slice_ok(fractions: list[tuple[float, float, float]]) -> bool:
    if not fractions:
        return False
    for y1, y2, _ in fractions:
        if y1 < TOP_MIN or y2 > BOTTOM_MAX:
            return False
    return True


def extract(im: Image.Image, y0: int, crop_h: int) -> Image.Image:
    w, h = im.size
    y0 = max(0, min(y0, h - crop_h))
    band = im.crop((0, y0, w, y0 + crop_h))
    return band.resize(OUT_SIZE, Image.Resampling.LANCZOS)


def main() -> None:
    if len(sys.argv) < 3:
        raise SystemExit("usage: author-onboarding-rustig-band-yolo.py <16x9.jpg> <out-6x1.jpg>")
    src, dst = Path(sys.argv[1]), Path(sys.argv[2])
    im = Image.open(src).convert("RGB")
    w, h = im.size
    crop_h = int(w / 6.0)
    model = load_yolo()
    best = None
    best_any = None
    for y0 in range(0, max(1, h - crop_h), 4):
        band = extract(im, y0, crop_h)
        desk = band.resize(DESK_SIZE, Image.Resampling.LANCZOS)
        fr = persons(model, desk)
        if not fr:
            continue
        margins = []
        ok = True
        for y1, y2, _ in fr:
            margins.append(min(y1 - TOP_MIN, BOTTOM_MAX - y2))
            if y1 < TOP_MIN or y2 > BOTTOM_MAX:
                ok = False
        min_margin = min(margins)
        headroom = sum(y1 for y1, _, _ in fr) / len(fr)
        if best_any is None or min_margin > best_any[0]:
            best_any = (min_margin, y0, fr, band, ok)
        if not ok:
            continue
        if best is None or headroom < best[0]:
            best = (headroom, y0, fr, band)
    if best is None:
        if best_any is None or best_any[0] < -0.08:
            raise SystemExit("FAIL: no vertical slice with detectable persons")
        print(f"WARN: using best-effort slice min_margin={best_any[0]:.3f}")
        _, y0, fr, band, _ = best_any
        best = (0.0, y0, fr, band)
    _, y0, fr, band = best
    dst.parent.mkdir(parents=True, exist_ok=True)
    band.save(dst, quality=95, optimize=True)
    print(f"wrote {dst} y0={y0} crop_h={crop_h} persons={len(fr)}")
    for i, (y1, y2, c) in enumerate(fr):
        print(f"  person[{i}] y1={y1:.3f} y2={y2:.3f} conf={c:.2f}")


if __name__ == "__main__":
    main()
