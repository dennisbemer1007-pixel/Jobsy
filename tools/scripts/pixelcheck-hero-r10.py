#!/usr/bin/env python3
"""R10: right 45% of mobile learn-grow hero must be >= min_non_pearl fraction."""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

PEARL = np.array([247, 244, 240], dtype=np.float32)
MIN_RATIO = 0.35
RIGHT_FRAC = 0.45


def is_pearl(rgb: np.ndarray) -> np.ndarray:
    diff = np.abs(rgb.astype(np.float32) - PEARL)
    return (diff <= np.array([14, 14, 16])).all(axis=-1)


def non_pearl_ratio(path: Path) -> float:
    im = np.array(Image.open(path).convert("RGB"))
    h, w = im.shape[:2]
    x0 = int(w * (1.0 - RIGHT_FRAC))
    band = im[:, x0:, :]
    pearl = is_pearl(band)
    return float((~pearl).mean())


def main() -> int:
    path = Path(sys.argv[1] if len(sys.argv) > 1 else "/opt/cursor/artifacts/opleidingen-r10/leren-groeien-mobiel-390.png")
    if not path.is_file():
        print(f"MISSING {path}")
        return 1
    ratio = non_pearl_ratio(path)
    ok = ratio >= MIN_RATIO
    print(f"{'OK' if ok else 'FAIL'} {path.name} right45_non_pearl={ratio:.3f} (min {MIN_RATIO})")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
