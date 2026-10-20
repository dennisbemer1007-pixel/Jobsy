#!/usr/bin/env python3
"""YOLO person check on hero JPEG or PNG. Fails if any person violates vertical margins."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

TOP_MIN = 0.12
BOTTOM_MAX = 0.92
PERSON_CLS = 0


def load_yolo():
    from ultralytics import YOLO

    root = Path(__file__).resolve().parents[2]
    weights = root / "yolov8n.pt"
    if not weights.is_file():
        weights = Path("yolov8n.pt")
    return YOLO(str(weights))


def detect_persons(model, im: Image.Image, conf: float = 0.25):
    import numpy as np

    arr = np.asarray(im.convert("RGB"))
    results = model(arr, verbose=False)[0]
    boxes = []
    if results.boxes is None:
        return boxes
    h, w = arr.shape[:2]
    for b in results.boxes:
        if int(b.cls[0]) != PERSON_CLS:
            continue
        if float(b.conf[0]) < conf:
            continue
        x1, y1, x2, y2 = (float(v) for v in b.xyxy[0])
        boxes.append(
            {
                "x1": x1 / w,
                "y1": y1 / h,
                "x2": x2 / w,
                "y2": y2 / h,
                "conf": float(b.conf[0]),
            }
        )
    return boxes


def validate(boxes: list[dict], label: str) -> bool:
    ok = True
    for i, b in enumerate(boxes):
        top_ok = b["y1"] >= TOP_MIN
        bot_ok = b["y2"] <= BOTTOM_MAX
        status = "OK" if (top_ok and bot_ok) else "FAIL"
        if not (top_ok and bot_ok):
            ok = False
        print(
            f"{label} person[{i}] conf={b['conf']:.2f} "
            f"y1={b['y1']:.3f} y2={b['y2']:.3f} "
            f"(need y1>={TOP_MIN:.2f} y2<={BOTTOM_MAX:.2f}) {status}"
        )
    if not boxes:
        print(f"{label}: WARNING no persons detected")
    return ok


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("images", nargs="+", help="hero PNG/JPEG paths")
    p.add_argument("--min-persons", type=int, default=1)
    args = p.parse_args()
    model = load_yolo()
    all_ok = True
    for path_str in args.images:
        path = Path(path_str)
        im = Image.open(path)
        boxes = detect_persons(model, im)
        print(f"\n== {path} ({im.size[0]}x{im.size[1]}) persons={len(boxes)} ==")
        if len(boxes) < args.min_persons:
            print(f"FAIL: expected at least {args.min_persons} person(s)")
            all_ok = False
        if not validate(boxes, path.name):
            all_ok = False
    if not all_ok:
        raise SystemExit(1)
    print("\nPASS: all person boxes within vertical margins")


if __name__ == "__main__":
    main()
