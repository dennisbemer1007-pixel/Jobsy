#!/usr/bin/env python3
"""Widen start-desktop-raw-v3 for 6.7:1 band slice (outpaint wings, single center scene)."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "artifacts/onboarding-rustig-panorama-raw"
DEFAULT_IN = RAW / "start-desktop-raw.jpg"
FALLBACK = Path("/opt/cursor/artifacts/assets/start-desktop-raw-v3.jpg")
OUT = RAW / "start-source-16x9-startf-seamless.jpg"
SLICE_Y0 = 0.42
SLICE_Y1 = 0.92
BAND_RATIO = 2280 / 340
EDGE_SAMPLE_PX = 160


def load_cream() -> tuple[int, int, int]:
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(m)
    return tuple(int(c) for c in m.CREAM)


def target_width(h: int = 720) -> int:
    slice_h = (SLICE_Y1 - SLICE_Y0) * h
    need = int(round(slice_h * BAND_RATIO))
    return max(2100, need)


def extend_horizontal(im: Image.Image, target_w: int) -> Image.Image:
    w, h = im.size
    if w >= target_w:
        return im
    pad_l = (target_w - w) // 2
    pad_r = target_w - w - pad_l
    cream = np.array(load_cream(), dtype=np.uint8)
    arr = np.asarray(im.convert("RGB"))
    canvas = np.tile(cream, (h, target_w, 1)).copy()
    canvas[:, pad_l : pad_l + w] = arr
    ew = min(EDGE_SAMPLE_PX, w // 3)
    left_edge = arr[:, :ew]
    right_edge = arr[:, w - ew :]
    canvas[:, :pad_l] = cv2.resize(left_edge, (pad_l, h), interpolation=cv2.INTER_LANCZOS4)
    canvas[:, pad_l + w :] = cv2.resize(right_edge, (pad_r, h), interpolation=cv2.INTER_LANCZOS4)
    mask = np.zeros((h, target_w), np.uint8)
    seam = max(24, ew // 4)
    for cx in (pad_l, pad_l + w):
        x0, x1 = max(0, cx - seam), min(target_w, cx + seam)
        mask[:, x0:x1] = 255
    bgr = cv2.cvtColor(canvas, cv2.COLOR_RGB2BGR)
    healed = cv2.inpaint(bgr, mask, 12, cv2.INPAINT_NS)
    return Image.fromarray(cv2.cvtColor(healed, cv2.COLOR_BGR2RGB))


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_IN
    if not src.is_file():
        src = FALLBACK
    if not src.is_file():
        raise SystemExit(f"missing source: {src}")
    im = Image.open(src).convert("RGB")
    tw = target_width(im.size[1])
    wide = extend_horizontal(im, tw)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    wide.save(OUT, quality=95, optimize=True)
    print(f"wrote {OUT} size={wide.size} target_w={tw}")


if __name__ == "__main__":
    main()
