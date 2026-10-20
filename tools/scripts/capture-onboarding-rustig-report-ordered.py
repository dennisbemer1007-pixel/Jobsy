#!/usr/bin/env python3
"""Dennis report order: step-02, 04, 05, 06, 07–10, kompas (+ lobster 4× each)."""

from __future__ import annotations

import io
import subprocess
import sys
import time
from pathlib import Path

from PIL import Image, ImageFilter
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
ZOOM = Path("/opt/cursor/artifacts/onboarding-rustig-lobster-zoom")

ORDER = ["step-02", "step-04", "step-05", "step-06", "step-07", "step-08", "step-09", "step-10", "kompas"]

MOB_CENTER: dict[str, tuple[float, float]] = {
    "step-02": (0.76, 0.55),
    "step-04": (0.12, 0.58),
    "step-05": (0.14, 0.66),
    "step-06": (0.68, 0.55),
    "step-07": (0.35, 0.57),
    "step-08": (0.15, 0.64),
    "step-09": (0.70, 0.54),
    "step-10": (0.24, 0.55),
    "kompas": (0.50, 0.57),
}


def lobster_4x(page, slug: str) -> None:
    hero = page.locator("[data-testid=ob-rustig-hero]").bounding_box()
    im = Image.open(io.BytesIO(page.screenshot(full_page=False)))
    x0, y0 = int(hero["x"]), int(hero["y"])
    w, h = int(hero["width"]), int(hero["height"])
    band = im.crop((x0, y0, x0 + w, y0 + h))
    ex, ey = MOB_CENTER[slug]
    cx, cy = int(w * ex), int(h * ey)
    crop = band.crop((max(0, cx - 14), max(0, cy - 16), min(w, cx + 14), min(h, cy + 16)))
    zoom = crop.resize((crop.size[0] * 4, crop.size[1] * 4), Image.Resampling.LANCZOS)
    zoom = zoom.filter(ImageFilter.UnsharpMask(radius=1, percent=80, threshold=2))
    zout = ZOOM / f"{slug}-mobile-lobster-4x.png"
    zoom.save(zout)
    print("wrote", zout)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    ZOOM.mkdir(parents=True, exist_ok=True)
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
            for _ in range(90):
                try:
                    pg = browser.new_page()
                    pg.goto(f"{base}/mockups/onboarding-rustig/start", timeout=5000)
                    pg.close()
                    break
                except Exception:
                    time.sleep(1)
            else:
                raise SystemExit("server not up")

            for slug in ORDER:
                page = browser.new_page(viewport={"width": 1280, "height": 900})
                page.goto(f"{base}/mockups/onboarding-rustig/{slug}", wait_until="networkidle")
                page.wait_for_selector("[data-ob-rustig-screen]", timeout=30_000)
                page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=10_000)
                page.wait_for_timeout(400)
                desk = OUT / f"{slug}-desktop-1280.png"
                page.screenshot(path=str(desk))
                print("wrote", desk)
                page.close()

                page = browser.new_page(viewport={"width": 390, "height": 844})
                page.goto(f"{base}/mockups/onboarding-rustig/{slug}", wait_until="networkidle")
                page.wait_for_selector("[data-ob-rustig-screen]", timeout=30_000)
                page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=10_000)
                page.wait_for_timeout(400)
                mob = OUT / f"{slug}-mobile.png"
                page.screenshot(path=str(mob))
                print("wrote", mob)
                lobster_4x(page, slug)
                page.close()

            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)


if __name__ == "__main__":
    main()
