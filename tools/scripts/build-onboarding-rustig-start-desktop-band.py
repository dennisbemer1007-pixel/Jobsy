#!/usr/bin/env python3
"""Start desktop hero band (u8d-style): clean v3 source, figures above fade."""

from __future__ import annotations

import importlib.util
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SRC = ROOT / "artifacts/onboarding-rustig-panorama-raw/start-desktop-raw.jpg"
FALLBACK = Path("/opt/cursor/artifacts/assets/start-desktop-raw-v3.jpg")
OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/start-desktop.jpg"
DESK = (2280, 340)
FADE_BOTTOM = 14
FEET_Y_TARGET = 172


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(m)
    return m


def pick_slice(im: Image.Image) -> Image.Image:
    w, h = im.size
    return im.crop((0, int(h * 0.08), w, int(h * 0.82)))


def person_bbox_yolo(im: Image.Image) -> tuple[int, int, int, int] | None:
    try:
        from ultralytics import YOLO

        wpath = ROOT / "yolov8n.pt"
        model = YOLO(str(wpath if wpath.is_file() else "yolov8n.pt"))
        res = model(np.asarray(im), verbose=False)[0]
        if res.boxes is None:
            return None
        w = im.size[0]
        best = None
        for b in res.boxes:
            if int(b.cls[0]) != 0:
                continue
            conf = float(b.conf[0])
            if conf < 0.35:
                continue
            x0, y0, x1, y1 = (int(v) for v in b.xyxy[0].tolist())
            cx = (x0 + x1) / 2
            if cx < w * 0.25 or cx > w * 0.92:
                continue
            area = (x1 - x0) * (y1 - y0)
            if best is None or area > best[0]:
                best = (area, x0, y0, x1, y1)
        if best is None:
            return None
        return best[1], best[2], best[3], best[4]
    except Exception:
        return None


def estimate_region_bbox(
    im: Image.Image, fin, x0_frac: float, x1_frac: float
) -> tuple[int, int, int, int] | None:
    w, h = im.size
    x0, x1 = int(w * x0_frac), int(w * x1_frac)
    sub = im.crop((x0, 0, x1, h))
    bb = fin.figure_bbox(sub) or fin.content_bbox(sub)
    if bb is None:
        return None
    return x0 + bb[0], bb[1], x0 + bb[2], bb[3]


def apply_bottom_fade(im: Image.Image, fin) -> Image.Image:
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    cream = tuple(int(c) for c in fin.CREAM)
    fade = FADE_BOTTOM
    for y in range(h):
        if y < h - fade:
            continue
        for x in range(w):
            t = (y - (h - fade)) / max(1, fade - 1)
            a = int(255 * min(1.0, t**1.05))
            if a:
                px[x, y] = (*cream, a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def compose_band(src: Image.Image, fin) -> tuple[Image.Image, dict]:
    band = pick_slice(src).resize(DESK, Image.Resampling.LANCZOS)
    fb0 = fin.figure_bbox(band) or fin.content_bbox(band)
    feet_y = fb0[3] if fb0 else DESK[1]
    shift = max(0, feet_y - FEET_Y_TARGET)
    if shift > 0:
        cream = tuple(int(c) for c in fin.CREAM)
        arr = np.asarray(band, dtype=np.uint8)
        out = np.empty_like(arr)
        out[:] = cream
        keep = DESK[1] - shift
        out[:keep] = arr[shift : shift + keep]
        band = Image.fromarray(out)
    band_pre = band
    band = apply_bottom_fade(band_pre, fin)
    fb = fin.figure_bbox(band_pre) or fin.content_bbox(band_pre)
    person = estimate_region_bbox(band_pre, fin, 0.52, 0.80)
    sign = estimate_region_bbox(band_pre, fin, 0.38, 0.50)
    fade_start_y = DESK[1] - FADE_BOTTOM
    metrics = {
        "band_px": list(DESK),
        "fade_start_y_px": fade_start_y,
        "content_bbox_px": list(fb) if fb else None,
        "person_bbox_px": list(person) if person else None,
        "sign_bbox_px": list(sign) if sign else None,
        "vertical_shift_px": shift,
    }
    if person:
        cy = (person[1] + person[3]) / 2
        metrics["person_cy_pct"] = round(cy / DESK[1], 4)
        metrics["person_feet_y_px"] = person[3]
        metrics["person_feet_above_fade_px"] = fade_start_y - person[3]
    if sign:
        metrics["sign_bottom_y_px"] = sign[3]
        metrics["sign_above_fade_px"] = fade_start_y - sign[3]
    return band, metrics


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    if not src.is_file():
        src = FALLBACK
    if not src.is_file():
        raise SystemExit(f"missing source: {src}")
    fin = load_fin()
    band, metrics = compose_band(Image.open(src).convert("RGB"), fin)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    band.save(OUT, quality=92, optimize=True)
    metrics_path = ROOT / "artifacts/onboarding-rustig-panorama-raw/start-desktop-band-metrics.json"
    metrics_path.parent.mkdir(parents=True, exist_ok=True)
    metrics_path.write_text(json.dumps(metrics, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {OUT}")
    print(json.dumps(metrics, indent=2))
    if not metrics.get("person_bbox_px"):
        raise SystemExit("FAIL: no walker bbox estimate")
    if metrics.get("person_feet_above_fade_px", 0) < 10:
        raise SystemExit("FAIL: walker feet in asset fade zone")
    if metrics.get("sign_above_fade_px", 0) < 6:
        raise SystemExit("FAIL: sign in asset fade zone")
    cy = metrics.get("person_cy_pct", 0)
    if not (0.35 <= cy <= 0.45):
        print(f"WARN person_cy_pct={cy} (target 35-45%)")


if __name__ == "__main__":
    main()
