#!/usr/bin/env python3
"""YOLO person margins + optional lobster–person gap check on hero crops."""

from __future__ import annotations

import argparse
import importlib.util
import sys
from pathlib import Path

from PIL import Image

TOP_MIN = 0.10
BOTTOM_MAX = 0.93
PERSON_CLS = 0
MIN_LOBSTER_GAP_PX = 10
NO_LOBSTER_SLUGS: frozenset[str] = frozenset({"step-02"})

ROOT = Path(__file__).resolve().parents[2]
_layout_path = ROOT / "tools/scripts/onboarding_rustig_lobster_layout.py"
_spec = importlib.util.spec_from_file_location("lob_layout", _layout_path)
lob = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(lob)


def load_yolo():
    from ultralytics import YOLO

    weights = ROOT / "yolov8n.pt"
    if not weights.is_file():
        weights = Path("yolov8n.pt")
    return YOLO(str(weights))


def detect_persons_pixels(model, im: Image.Image, conf: float = 0.25):
    import numpy as np

    arr = np.asarray(im.convert("RGB"))
    results = model(arr, verbose=False)[0]
    boxes = []
    if results.boxes is None:
        return boxes
    for b in results.boxes:
        if int(b.cls[0]) != PERSON_CLS:
            continue
        if float(b.conf[0]) < conf:
            continue
        x1, y1, x2, y2 = (int(v) for v in b.xyxy[0])
        boxes.append({"px": (x1, y1, x2, y2), "conf": float(b.conf[0])})
    return boxes


MARGIN_MIN_CONF = 0.45


def validate_persons(boxes: list[dict], w: int, h: int, label: str) -> bool:
    ok = True
    for i, b in enumerate(boxes):
        if b["conf"] < MARGIN_MIN_CONF:
            print(f"{label} person[{i}] conf={b['conf']:.2f} (skip margin, below {MARGIN_MIN_CONF})")
            continue
        x1, y1, x2, y2 = b["px"]
        y1f, y2f = y1 / h, y2 / h
        top_ok = y1f >= TOP_MIN
        bot_ok = y2f <= BOTTOM_MAX
        status = "OK" if (top_ok and bot_ok) else "FAIL"
        if not (top_ok and bot_ok):
            ok = False
        print(
            f"{label} person[{i}] conf={b['conf']:.2f} "
            f"y1={y1f:.3f} y2={y2f:.3f} px=({x1},{y1},{x2},{y2}) "
            f"(need y1>={TOP_MIN:.2f} y2<={BOTTOM_MAX:.2f}) {status}"
        )
    return ok


def validate_lobster(im: Image.Image, slug: str, variant: str, persons: list[dict], label: str) -> bool:
    lob_box = lob.lobster_bbox_pixels(im, slug, variant)
    print(f"{label} lobster bbox px={lob_box} center={lob.placement(slug, variant)}")
    ok = True
    for i, p in enumerate(persons):
        gap = lob.min_box_gap(lob_box, p["px"])
        status = "OK" if gap >= MIN_LOBSTER_GAP_PX else "FAIL"
        if gap < MIN_LOBSTER_GAP_PX:
            ok = False
        print(f"{label} lobster↔person[{i}] gap={gap:.1f}px (need >={MIN_LOBSTER_GAP_PX}) {status}")
    return ok


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("images", nargs="+", help="hero PNG paths")
    p.add_argument("--min-persons", type=int, default=1)
    p.add_argument("--slug", default="", help="enables lobster gap check")
    p.add_argument("--variant", default="", help="desktop|mobile (required with --slug)")
    args = p.parse_args()
    model = load_yolo()
    all_ok = True
    for path_str in args.images:
        path = Path(path_str)
        im = Image.open(path)
        w, h = im.size
        persons = detect_persons_pixels(model, im)
        print(f"\n== {path} ({w}x{h}) persons={len(persons)} ==")
        strong = [b for b in persons if b["conf"] >= 0.24]
        if len(strong) < args.min_persons:
            print(f"FAIL: expected at least {args.min_persons} person(s) with conf>=0.24 (got {len(strong)})")
            all_ok = False
        if not validate_persons(persons, w, h, path.name):
            all_ok = False
        if args.slug and args.variant and args.slug not in NO_LOBSTER_SLUGS:
            if not validate_lobster(im, args.slug, args.variant, persons, path.name):
                all_ok = False
    if not all_ok:
        raise SystemExit(1)
    print("\nPASS: person margins" + (" and lobster gaps" if args.slug else ""))


if __name__ == "__main__":
    main()
