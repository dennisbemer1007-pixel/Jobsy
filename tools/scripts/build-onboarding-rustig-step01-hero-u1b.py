#!/usr/bin/env python3
"""Step-01 u1b: native aquarel source → desktop/mobile bands (start-g pipeline)."""

from __future__ import annotations

import importlib.util
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SRC = (
    ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/step-01-u1b-aquarel-source.jpg"
)
FALLBACK = Path("/opt/cursor/artifacts/assets/step-01-u1b-aquarel-clean.jpg")
DESK_OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/step-01-desktop.jpg"
MOB_OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/step-01-mobile.jpg"
VERIFY = ROOT / "artifacts/onboarding-rustig-panorama-raw/step-01-u1b-band-verify.png"
METRICS = ROOT / "artifacts/onboarding-rustig-panorama-raw/step-01-u1b-band-metrics.json"
DESK = (2280, 340)
MOB = (780, 184)
FADE_BOTTOM = 14
DESK_SCALE_W = 4000
MOB_SCALE_W = 1560
TOP_MARGIN = 0.05
BOTTOM_MARGIN = 0.93
SEAM_GRAD_MAX = 2.4


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(m)
    return m


def scale_to_width(im: Image.Image, target_w: int) -> Image.Image:
    w, h = im.size
    if w >= target_w:
        return im
    nw = target_w
    nh = max(1, int(round(h * nw / w)))
    return im.resize((nw, nh), Image.Resampling.LANCZOS)


def yolo_persons(im: Image.Image) -> list[tuple[float, float, float, float]]:
    from ultralytics import YOLO

    wpath = ROOT / "yolov8n.pt"
    model = YOLO(str(wpath if wpath.is_file() else "yolov8n.pt"))
    res = model(np.asarray(im), verbose=False)[0]
    if res.boxes is None:
        return []
    w, h = im.size
    out: list[tuple[float, float, float, float]] = []
    for b in res.boxes:
        if int(b.cls[0]) != 0 or float(b.conf[0]) < 0.25:
            continue
        x0, y0, x1, y1 = (float(v) for v in b.xyxy[0].tolist())
        if (y1 - y0) < h * 0.04:
            continue
        out.append((x0, y0, x1, y1))
    return out


def pick_crop_y0(im: Image.Image, crop_h: int, persons: list[tuple[float, float, float, float]]) -> int:
    h = im.size[1]
    if not persons:
        return max(0, (h - crop_h) // 2)
    max_feet = max(p[3] for p in persons)
    min_hat = min(p[1] for p in persons)
    lo = int(round(max_feet - crop_h * BOTTOM_MARGIN))
    hi = int(round(min_hat - crop_h * TOP_MARGIN))
    if lo > hi:
        union_h = max_feet - min_hat
        cy = (min_hat + max_feet) / 2
        y0 = int(round(cy - crop_h / 2))
    else:
        y0 = (lo + hi) // 2
    return max(0, min(y0, h - crop_h))


def seam_grad_ratio(im: Image.Image) -> float:
    gray = np.asarray(im.convert("L"), dtype=np.float32)
    gx = np.abs(np.diff(gray, axis=1))
    h, w = gx.shape
    center = gx[:, int(w * 0.4) : int(w * 0.6)].mean()
    wings = max(gx[:, : int(w * 0.12)].mean(), gx[:, int(w * 0.88) :].mean())
    return float(wings / max(1.0, center))


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


def build_band(
    src: Image.Image,
    *,
    out_size: tuple[int, int],
    scale_w: int,
    fin,
) -> tuple[Image.Image, dict]:
    ratio = out_size[0] / out_size[1]
    scaled = scale_to_width(src, scale_w)
    w, h = scaled.size
    crop_h = int(round(w / ratio))
    crop_h = max(60, min(crop_h, h))
    persons = yolo_persons(scaled)
    y0 = pick_crop_y0(scaled, crop_h, persons)
    crop = scaled.crop((0, y0, w, y0 + crop_h))
    if abs(crop.size[0] / crop.size[1] - ratio) > 0.02:
        raise RuntimeError(f"crop ratio {crop.size[0]/crop.size[1]:.4f} != {ratio:.4f}")
    pre = crop.resize(out_size, Image.Resampling.LANCZOS)
    band = apply_bottom_fade(pre, fin)
    fade_y = out_size[1] - FADE_BOTTOM
    feet_y = None
    yolo_band = yolo_persons(pre)
    if yolo_band:
        feet_y = int(max(p[3] for p in yolo_band))
    metrics = {
        "out_px": list(out_size),
        "source_scaled_px": list(scaled.size),
        "crop_y0_px": y0,
        "crop_h_px": crop_h,
        "crop_ratio": round(crop.size[0] / crop.size[1], 4),
        "person_count_yolo": len(yolo_band),
        "fade_start_y_px": fade_y,
        "seam_wing_grad_ratio": round(seam_grad_ratio(pre), 3),
        "yolo_feet_max_y_px": feet_y,
        "feet_above_fade_px": (fade_y - feet_y) if feet_y is not None else None,
    }
    return band, metrics, pre, yolo_band


def save_verify(pre: Image.Image, yolo_boxes: list, path: Path) -> None:
    vis = pre.copy().convert("RGBA")
    draw = ImageDraw.Draw(vis)
    for bb in yolo_boxes:
        draw.rectangle([int(v) for v in bb], outline=(255, 140, 0, 255), width=2)
    path.parent.mkdir(parents=True, exist_ok=True)
    vis.convert("RGB").save(path, optimize=True)


def gate_metrics(metrics: dict, label: str) -> None:
    if metrics.get("person_count_yolo", 0) < 3:
        raise SystemExit(f"FAIL {label}: expected ≥3 people in band, got {metrics.get('person_count_yolo')}")
    feet_ok = metrics.get("feet_above_fade_px")
    if feet_ok is None or feet_ok < 4:
        raise SystemExit(f"FAIL {label}: feet in fade (feet_above_fade_px={feet_ok})")
    if metrics.get("seam_wing_grad_ratio", 99) > SEAM_GRAD_MAX:
        raise SystemExit(f"FAIL {label}: seam score {metrics['seam_wing_grad_ratio']}")


def main() -> None:
    src_path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    if not src_path.is_file():
        src_path = FALLBACK
    if not src_path.is_file():
        raise SystemExit(f"missing step-01 source: {src_path}")
    fin = load_fin()
    src = Image.open(src_path).convert("RGB")
    desk, desk_m, desk_pre, desk_yolo = build_band(src, out_size=DESK, scale_w=DESK_SCALE_W, fin=fin)
    mob, mob_m, _, _ = build_band(src, out_size=MOB, scale_w=MOB_SCALE_W, fin=fin)
    DESK_OUT.parent.mkdir(parents=True, exist_ok=True)
    desk.save(DESK_OUT, quality=92, optimize=True)
    mob.save(MOB_OUT, quality=92, optimize=True)
    save_verify(desk_pre, desk_yolo, VERIFY)
    payload = {"source": src_path.name, "desktop": desk_m, "mobile": mob_m}
    METRICS.parent.mkdir(parents=True, exist_ok=True)
    METRICS.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(payload, indent=2))
    gate_metrics(desk_m, "desktop")
    gate_metrics(mob_m, "mobile")


if __name__ == "__main__":
    main()
