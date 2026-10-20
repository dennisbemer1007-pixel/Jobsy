#!/usr/bin/env python3
"""Capture step-06 mockups (1440 desktop + 390 mobile) with framing checks."""

from __future__ import annotations

import subprocess
import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
DESK_JPEG = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/step-06-desktop.jpg"
MOB_JPEG = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/step-06-mobile.jpg"
CACHE = "20261020-u6z1"


def lobster_red_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (r > 110) & (r > g + 18) & (r > b + 18) & (g < 145) & (b < 145)


def small_red_glyphs(rgb: np.ndarray, *, min_x_frac: float = 0.90) -> int:
    h, w = rgb.shape[:2]
    mask = lobster_red_mask(rgb)
    vis = np.zeros((h, w), dtype=bool)
    n = 0
    for y in range(h):
        for x in range(w):
            if not mask[y, x] or vis[y, x]:
                continue
            stack = [(y, x)]
            xs: list[int] = []
            ys: list[int] = []
            while stack:
                cy, cx = stack.pop()
                if cy < 0 or cy >= h or cx < 0 or cx >= w or vis[cy, cx] or not mask[cy, cx]:
                    continue
                vis[cy, cx] = True
                xs.append(cx)
                ys.append(cy)
                stack.extend([(cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)])
            if len(xs) < 12 or len(xs) > 900:
                continue
            x0, x1 = min(xs), max(xs)
            y0, y1 = min(ys), max(ys)
            cxm, cym = (x0 + x1) / 2, (y0 + y1) / 2
            if cxm < w * min_x_frac or cym > h * 0.58:
                continue
            if (y1 - y0 + 1) > 40 or (x1 - x0 + 1) > 40:
                continue
            n += 1
    return n


def assert_hero_jpeg(path: Path, label: str) -> None:
    rgb = np.asarray(Image.open(path).convert("RGB"))
    h, w = rgb.shape[:2]
    cream = np.array([247.0, 244.0, 240.0])
    dist = np.linalg.norm(rgb.astype(np.float32) - cream, axis=2)
    rows = np.where(dist.max(axis=1) > 30)[0]
    min_bottom = 0.86 if "mobile" in label else 0.90
    if rows.size == 0 or rows.max() < h * min_bottom:
        raise SystemExit(f"FAIL {label}: bottom clipped (max y={rows.max() if rows.size else 0})")
    band = rgb[: int(h * 0.58), int(w * 0.90) :]
    red = lobster_red_mask(band)
    r, g, _b = band[:, :, 0], band[:, :, 1], band[:, :, 2]
    sunset_haze = (g > 118) & (r.astype(np.int16) - g.astype(np.int16) < 30)
    foliage_red = int((red & ~sunset_haze).sum())
    if foliage_red > 8:
        raise SystemExit(f"FAIL {label}: {foliage_red} red px in right foliage band")
    if small_red_glyphs(rgb) > 0:
        raise SystemExit(f"FAIL {label}: small red glyph(s) detected on right")
    sub = rgb[: int(h * 0.42), int(w * 0.72) :].astype(np.float32)
    sh, sw = sub.shape[:2]
    col_std = sub.std(axis=(0, 2))
    low = col_std < 9.0
    if low.any():
        run = 0
        max_run = 0
        for v in low:
            if v:
                run += 1
                max_run = max(max_run, run)
            else:
                run = 0
        if max_run > sw * 0.22:
            raise SystemExit(f"FAIL {label}: flat sky patch (run {max_run}px)")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    assert_hero_jpeg(DESK_JPEG, "desktop")
    assert_hero_jpeg(MOB_JPEG, "mobile")
    subprocess.run(["pkill", "-f", "127.0.0.1:8765"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    proc = subprocess.Popen(
        ["dotnet", "run", "--project", str(ROOT / "Jobsy.Web/Jobsy.Web.csproj"), "--urls", "http://127.0.0.1:8765"],
        cwd=ROOT,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    base = "http://127.0.0.1:8765"
    url = f"{base}/mockups/onboarding-rustig/step-06?cb={CACHE}"
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
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "step-06-desktop-1440.png"))
            page.close()

            page = browser.new_page(viewport={"width": 390, "height": 844})
            page.goto(url, wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "step-06-mobile.png"))
            page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)
    print("step-06 mockups OK")


if __name__ == "__main__":
    main()
