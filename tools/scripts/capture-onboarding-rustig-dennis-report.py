#!/usr/bin/env python3
"""Capture Dennis report set: 6 mobile, 4 desktop 1280, start mobile lobster 4x."""

from __future__ import annotations

import subprocess
import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
ZOOM = Path("/opt/cursor/artifacts/onboarding-rustig-lobster-zoom")
MOB_SLUGS = ["start", "step-01", "step-02", "step-03", "step-04", "step-05"]
DESK_SLUGS = ["step-01", "step-02", "step-04", "step-05"]
MOB = (390, 844)
HERO_H = 92
# Mobile composite centers (match composite-onboarding-rustig-lobsters.py).
MOB_CENTER = {
    "start": (0.26, 0.55),
    "step-01": (0.24, 0.60),
    "step-02": (0.50, 0.58),
    "step-03": (0.14, 0.48),
    "step-04": (0.22, 0.54),
    "step-05": (0.30, 0.50),
}


def start_mobile_lobster_4x(page) -> None:
    ZOOM.mkdir(parents=True, exist_ok=True)
    hero = page.locator("[data-testid=ob-rustig-hero]").bounding_box()
    if not hero:
        raise SystemExit("hero band not found")
    shot = page.screenshot(full_page=False)
    im = Image.open(__import__("io").BytesIO(shot))
    x0, y0, w, h = int(hero["x"]), int(hero["y"]), int(hero["width"]), int(hero["height"])
    band = im.crop((x0, y0, x0 + w, y0 + h))
    ex, ey = MOB_CENTER["start"]
    cx, cy = int(w * ex), int(h * ey)
    tw, th = 24, 28
    crop = band.crop((max(0, cx - tw // 2), max(0, cy - th // 2), min(w, cx + tw // 2), min(h, cy + th // 2)))
    zoom = crop.resize((crop.size[0] * 4, crop.size[1] * 4), Image.Resampling.LANCZOS)
    zoom = zoom.filter(ImageFilter.UnsharpMask(radius=1, percent=80, threshold=2))
    out = ZOOM / "start-mobile-lobster-4x.png"
    zoom.save(out)
    print("wrote", out)


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
    try:
        with sync_playwright() as p:
            browser = p.chromium.launch(headless=True)
            for _ in range(60):
                try:
                    pg = browser.new_page()
                    pg.goto(f"{base}/mockups/onboarding-rustig/start", timeout=3000)
                    pg.close()
                    break
                except Exception:
                    time.sleep(1)
            else:
                raise SystemExit("web server did not start")

            mw, mh = MOB
            for slug in MOB_SLUGS:
                page = browser.new_page(viewport={"width": mw, "height": mh})
                page.goto(f"{base}/mockups/onboarding-rustig/{slug}", wait_until="networkidle")
                page.wait_for_selector("[data-ob-rustig-screen]", timeout=30_000)
                page.wait_for_timeout(400)
                out = OUT / f"{slug}-mobile.png"
                page.screenshot(path=str(out), full_page=False)
                h1 = page.locator(".ob-rustig__page-head h1").bounding_box()
                hero = page.locator("[data-testid=ob-rustig-hero]").bounding_box()
                print(f"mobile {slug}: hero_h={hero['height'] if hero else '?'} h1_y={h1['y'] if h1 else '?'}")
                if slug == "start":
                    start_mobile_lobster_4x(page)
                page.close()

            for slug in DESK_SLUGS:
                page = browser.new_page(viewport={"width": 1280, "height": 900})
                page.goto(f"{base}/mockups/onboarding-rustig/{slug}", wait_until="networkidle")
                page.wait_for_selector("[data-ob-rustig-screen]", timeout=30_000)
                page.wait_for_timeout(400)
                out = OUT / f"{slug}-desktop-1280.png"
                page.screenshot(path=str(out), full_page=False)
                print("wrote", out)
                page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)


if __name__ == "__main__":
    main()
