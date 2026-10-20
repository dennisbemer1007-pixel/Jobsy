#!/usr/bin/env python3
"""Capture kompas mockups (1440 + 390, top/mid/bottom) and measure mobile hero."""

from __future__ import annotations

import importlib.util
import json
import subprocess
import time
from pathlib import Path

from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
CACHE = "20261020-kompas-c"
PREFIX = "kompas-c"


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(m)
    return m


def person_bbox(im) -> tuple[int, int, int, int] | None:
    """Tighter bbox for standing figure (excludes full-width landscape)."""
    import numpy as np

    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    cream = np.array([247.0, 244.0, 240.0], dtype=np.float32)
    h, w = rgb.shape[:2]
    dist = np.linalg.norm(rgb - cream, axis=2)
    mask = dist > 28.0
    mask[: int(h * 0.18), :] = False
    col = mask.sum(axis=0)
    if col.max() < h * 0.08:
        return None
    thr = max(6, int(col.max() * 0.35))
    xs = np.where(col >= thr)[0]
    if xs.size == 0:
        return None
    x0, x1 = int(xs.min()), int(xs.max()) + 1
    sub = mask[:, x0:x1]
    row = sub.sum(axis=1)
    yr = np.where(row >= max(4, int(row.max() * 0.12)))[0]
    if yr.size == 0:
        return None
    y0, y1 = int(yr.min()), int(yr.max()) + 1
    return x0, y0, x1, y1


def measure_mobile_hero(page) -> dict:
    fin = load_fin()
    hero = page.locator("[data-testid=ob-rustig-hero].ob-rustig__illustrated-hero")
    frame = hero.locator(".ob-rustig__illustrated-frame")
    box = frame.bounding_box()
    if not box:
        raise SystemExit("no hero frame box")
    shot = OUT / f"{PREFIX}-mobile-hero-crop.png"
    frame.screenshot(path=str(shot))
    from PIL import Image

    im = Image.open(shot)
    scene = fin.figure_bbox(im)
    person = person_bbox(im)
    report = {
        "hero_box_css_px": {
            "x": round(box["x"], 1),
            "y": round(box["y"], 1),
            "width": round(box["width"], 1),
            "height": round(box["height"], 1),
        },
        "screenshot_px": list(im.size),
        "scene_bbox_px": list(scene) if scene else None,
        "figure_bbox_px": list(person) if person else None,
    }
    if person and im.size[0]:
        report["figure_left_pct"] = round(person[0] / im.size[0], 4)
        report["figure_height_pct"] = round((person[3] - person[1]) / im.size[1], 4)
    return report


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    subprocess.run(["pkill", "-f", "127.0.0.1:8765"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    proc = subprocess.Popen(
        ["dotnet", "run", "--project", str(ROOT / "Jobsy.Web/Jobsy.Web.csproj"), "--urls", "http://127.0.0.1:8765"],
        cwd=ROOT,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    base = "http://127.0.0.1:8765"
    url = f"{base}/mockups/onboarding-rustig/kompas?cb={CACHE}"
    metrics: dict | None = None
    try:
        with sync_playwright() as p:
            browser = p.chromium.launch(headless=True)
            for _ in range(120):
                try:
                    pg = browser.new_page()
                    pg.goto(f"{base}/mockups/onboarding-rustig/start", timeout=5000)
                    pg.close()
                    break
                except Exception:
                    time.sleep(1)
            else:
                raise SystemExit("server not up")

            page = browser.new_page(viewport={"width": 1440, "height": 900})
            page.goto(url, wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(500)
            page.screenshot(path=str(OUT / f"{PREFIX}-desktop-1440-top.png"))
            page.evaluate("window.scrollTo(0, Math.min(document.body.scrollHeight, 920))")
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / f"{PREFIX}-desktop-1440-mid.png"))
            page.evaluate("window.scrollTo(0, document.body.scrollHeight)")
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / f"{PREFIX}-desktop-1440-bottom.png"))
            page.close()

            page = browser.new_page(viewport={"width": 390, "height": 844})
            page.goto(url, wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(500)
            metrics = measure_mobile_hero(page)
            page.screenshot(path=str(OUT / f"{PREFIX}-mobile-390-top.png"))
            page.evaluate("window.scrollTo(0, Math.min(document.body.scrollHeight, 900))")
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / f"{PREFIX}-mobile-390-mid.png"))
            page.evaluate("window.scrollTo(0, document.body.scrollHeight)")
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / f"{PREFIX}-mobile-390-bottom.png"))
            page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)
    sha = subprocess.check_output(["git", "rev-parse", "--short", "HEAD"], cwd=ROOT, text=True).strip()
    report_path = OUT / f"{PREFIX}-mobile-hero-metrics.json"
    payload = {"commit": sha, "cache": CACHE, **(metrics or {})}
    report_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print(f"kompas mockups OK ({PREFIX} @ {sha})")
    print(json.dumps(payload, indent=2))


if __name__ == "__main__":
    main()
