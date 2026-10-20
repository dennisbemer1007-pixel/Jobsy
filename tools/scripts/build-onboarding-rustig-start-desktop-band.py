#!/usr/bin/env python3
"""Start desktop hero: native aquarel source, 6.7:1 crop + uniform resize (no wing stitch)."""

from __future__ import annotations

import importlib.util
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SRC = (
    ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/start-g-aquarel-source.jpg"
)
FALLBACK = Path("/opt/cursor/artifacts/assets/start-g-wide-aquarel-source.jpg")
OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/start-desktop.jpg"
VERIFY = ROOT / "artifacts/onboarding-rustig-panorama-raw/start-desktop-band-verify.png"
DESK = (2280, 340)
FADE_BOTTOM = 14
BAND_RATIO = DESK[0] / DESK[1]
TARGET_SOURCE_W = 4000
TOP_MARGIN_FRAC = 0.06
BOTTOM_MARGIN_FRAC = 0.07
SEAM_GRAD_MAX_RATIO = 2.4


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(m)
    return m


def scale_source(im: Image.Image) -> Image.Image:
    w, h = im.size
    if w >= TARGET_SOURCE_W:
        return im
    nw = TARGET_SOURCE_W
    nh = max(1, int(round(h * nw / w)))
    return im.resize((nw, nh), Image.Resampling.LANCZOS)


def yolo_persons(im: Image.Image) -> list[tuple[float, float, float, float]]:
    try:
        from ultralytics import YOLO

        wpath = ROOT / "yolov8n.pt"
        model = YOLO(str(wpath if wpath.is_file() else "yolov8n.pt"))
        res = model(np.asarray(im), verbose=False)[0]
        if res.boxes is None:
            return []
        w, h = im.size
        out = []
        for b in res.boxes:
            if int(b.cls[0]) != 0 or float(b.conf[0]) < 0.30:
                continue
            x0, y0, x1, y1 = (float(v) for v in b.xyxy[0].tolist())
            cx = (x0 + x1) / 2
            if cx < w * 0.30 or cx > w * 0.92:
                continue
            out.append((x0, y0, x1, y1))
        return out
    except Exception:
        return []


def pick_band_crop(im: Image.Image) -> tuple[Image.Image, dict]:
    w, h = im.size
    crop_h = int(round(w / BAND_RATIO))
    crop_h = max(80, min(crop_h, h))
    persons = yolo_persons(im)
    meta: dict = {"crop_h_px": crop_h}
    if persons:
        x0, py0, x1, py1 = max(persons, key=lambda b: (b[3] - b[1]) * (b[2] - b[0]))
        y0_feet = int(round(py1 - crop_h * (1.0 - BOTTOM_MARGIN_FRAC)))
        y0_hat = int(round(py0 - crop_h * TOP_MARGIN_FRAC))
        best_y0 = max(0, min(y0_feet, y0_hat, h - crop_h))
        meta["person_src_bbox_px"] = [int(x0), int(py0), int(x1), int(py1)]
    else:
        best_y0 = max(0, (h - crop_h) // 2)
    crop = im.crop((0, best_y0, w, best_y0 + crop_h))
    meta["crop_y0_px"] = best_y0
    meta["crop_size_px"] = list(crop.size)
    meta["crop_ratio"] = round(crop.size[0] / crop.size[1], 4)
    return crop, meta


def seam_grad_ratio(im: Image.Image) -> float:
    gray = np.asarray(im.convert("L"), dtype=np.float32)
    gx = np.abs(np.diff(gray, axis=1))
    h, w = gx.shape
    center = gx[:, int(w * 0.4) : int(w * 0.6)].mean()
    wings = max(
        gx[:, : int(w * 0.12)].mean(),
        gx[:, int(w * 0.88) :].mean(),
    )
    if center < 1.0:
        return 1.0
    return float(wings / center)


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


def save_verify_overlay(band_pre: Image.Image, person: list[int] | None, yolo: list[int] | None) -> None:
    vis = band_pre.copy().convert("RGBA")
    draw = ImageDraw.Draw(vis)
    if person:
        draw.rectangle(person, outline=(0, 180, 0, 255), width=3)
    if yolo:
        draw.rectangle(yolo, outline=(255, 140, 0, 255), width=2)
    VERIFY.parent.mkdir(parents=True, exist_ok=True)
    vis.convert("RGB").save(VERIFY, optimize=True)


def compose_band(src: Image.Image, fin) -> tuple[Image.Image, dict]:
    scaled = scale_source(src)
    crop, crop_meta = pick_band_crop(scaled)
    ratio_delta = abs(crop_meta["crop_ratio"] - BAND_RATIO)
    if ratio_delta > 0.02:
        raise RuntimeError(f"crop ratio {crop_meta['crop_ratio']} != {BAND_RATIO:.4f}")
    band_pre = crop.resize(DESK, Image.Resampling.LANCZOS)
    band = apply_bottom_fade(band_pre, fin)
    persons = yolo_persons(band_pre)
    yolo = None
    if persons:
        x0, y0, x1, y1 = max(persons, key=lambda b: (b[3] - b[1]) * (b[2] - b[0]))
        yolo = [int(x0), int(y0), int(x1), int(y1)]
    person = yolo
    fade_start_y = DESK[1] - FADE_BOTTOM
    metrics = {
        "band_px": list(DESK),
        "fade_start_y_px": fade_start_y,
        "source_scaled_px": list(scaled.size),
        **crop_meta,
        "person_bbox_px": person,
        "yolo_person_bbox_px": yolo,
        "seam_wing_grad_ratio": round(seam_grad_ratio(band_pre), 3),
    }
    if person:
        metrics["person_feet_y_px"] = person[3]
        metrics["person_feet_above_fade_px"] = fade_start_y - person[3]
        cy = (person[1] + person[3]) / 2
        metrics["person_cy_pct"] = round(cy / DESK[1], 4)
    save_verify_overlay(band_pre, person, yolo)
    metrics["verify_overlay"] = str(VERIFY.relative_to(ROOT))
    return band, metrics


def main() -> None:
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    if not src.is_file():
        src = FALLBACK
    if not src.is_file():
        raise SystemExit(f"missing start-g source: {src}")
    fin = load_fin()
    band, metrics = compose_band(Image.open(src).convert("RGB"), fin)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    band.save(OUT, quality=92, optimize=True)
    metrics_path = ROOT / "artifacts/onboarding-rustig-panorama-raw/start-desktop-band-metrics.json"
    metrics_path.parent.mkdir(parents=True, exist_ok=True)
    metrics_path.write_text(json.dumps(metrics, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {OUT}")
    print(json.dumps(metrics, indent=2))
    if not metrics.get("yolo_person_bbox_px"):
        raise SystemExit("FAIL: YOLO did not detect walker in band (visual gate)")
    if metrics.get("person_feet_above_fade_px", 0) < 6:
        raise SystemExit("FAIL: walker feet in asset fade zone")
    if metrics.get("seam_wing_grad_ratio", 99) > SEAM_GRAD_MAX_RATIO:
        raise SystemExit(
            f"FAIL: wing seam score {metrics['seam_wing_grad_ratio']} > {SEAM_GRAD_MAX_RATIO}"
        )


if __name__ == "__main__":
    main()
