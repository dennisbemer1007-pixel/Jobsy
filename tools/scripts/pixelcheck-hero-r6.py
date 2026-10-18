#!/usr/bin/env python3
"""Seam check: in hero sky band, adjacent column mean delta must be <= max_delta."""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image


def max_seam_delta(path: Path, y0: int = 48, y1: int = 130, width_frac: float = 0.62) -> float:
    im = np.array(Image.open(path).convert("RGB"))
    band = im[y0:y1].astype(np.float32)
    col_mean = band.mean(axis=(0, 2))
    w = len(col_mean)
    left = col_mean[: max(2, int(w * width_frac))]
    deltas = np.abs(np.diff(left))
    return float(deltas.max()) if deltas.size else 0.0


def main() -> int:
    max_delta = 6.0
    paths = sys.argv[1:] or [
        "/opt/cursor/artifacts/opleidingen-r6/leren-groeien-desktop-viewport-1440.png",
    ]
    failed = False
    for p in paths:
        path = Path(p)
        if not path.is_file():
            print(f"MISSING {path}")
            failed = True
            continue
        m = max_seam_delta(path)
        ok = m <= max_delta
        print(f"{'OK' if ok else 'FAIL'} {path.name} seam_max_delta={m:.2f}")
        if not ok:
            failed = True
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
