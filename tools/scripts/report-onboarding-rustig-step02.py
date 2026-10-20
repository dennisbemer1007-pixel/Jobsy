#!/usr/bin/env python3
"""Capture step-02 report PNGs + lobster 4× (desktop & mobile) and run YOLO gate."""

from __future__ import annotations

import importlib.util
import io
import subprocess
import sys
import time
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
ZOOM = Path("/opt/cursor/artifacts/onboarding-rustig-lobster-zoom")
HERO = Path("/opt/cursor/artifacts/onboarding-rustig-hero-crops")
COMPARE = Path("/opt/cursor/artifacts/onboarding-rustig-style-compare")
CHECK = ROOT / "tools/scripts/check-onboarding-rustig-hero-persons.py"

_spec = importlib.util.spec_from_file_location(
    "lob", ROOT / "tools/scripts/onboarding_rustig_lobster_layout.py"
)
lob = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(lob)


def hero_crop(page) -> Image.Image:
    box = page.locator("[data-testid=ob-rustig-hero]").bounding_box()
    im = Image.open(io.BytesIO(page.screenshot(full_page=False)))
    x0, y0 = int(box["x"]), int(box["y"])
    w, h = int(box["width"]), int(box["height"])
    return im.crop((x0, y0, x0 + w, y0 + h))


def lobster_4x(band: Image.Image, slug: str, variant: str) -> Image.Image:
    w, h = band.size
    cx_frac, cy_frac = lob.placement(slug, variant)
    cx, cy = int(w * cx_frac), int(h * cy_frac)
    crop = band.crop((max(0, cx - 18), max(0, cy - 20), min(w, cx + 18), min(h, cy + 20)))
    zoom = crop.resize((crop.size[0] * 4, crop.size[1] * 4), Image.Resampling.LANCZOS)
    return zoom.filter(ImageFilter.UnsharpMask(radius=1, percent=80, threshold=2))


def main() -> None:
    for d in (OUT, ZOOM, HERO, COMPARE):
        d.mkdir(parents=True, exist_ok=True)
    subprocess.run(["pkill", "-f", "127.0.0.1:8765"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    proc = subprocess.Popen(
        ["dotnet", "run", "--project", str(ROOT / "Jobsy.Web/Jobsy.Web.csproj"), "--urls", "http://127.0.0.1:8765"],
        cwd=ROOT,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    base = "http://127.0.0.1:8765"
    desk_hero = HERO / "step-02-desktop-hero-1440.png"
    mob_hero = HERO / "step-02-mobile-hero.png"
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
            page.goto(f"{base}/mockups/onboarding-rustig/step-02", wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(600)
            page.screenshot(path=str(OUT / "step-02-desktop-1440.png"))
            dh = hero_crop(page)
            dh.save(desk_hero)
            lob_desk = lobster_4x(dh, "step-02", "desktop")
            lob_desk.save(ZOOM / "step-02-desktop-lobster-4x.png")
            lob_desk.save(OUT / "step-02-lobster-4x.png")
            page.close()

            page = browser.new_page(viewport={"width": 390, "height": 844})
            page.goto(f"{base}/mockups/onboarding-rustig/step-02", wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(600)
            page.screenshot(path=str(OUT / "step-02-mobile.png"))
            mh = hero_crop(page)
            mh.save(mob_hero)
            lobster_4x(mh, "step-02", "mobile").save(ZOOM / "step-02-mobile-lobster-4x.png")
            page.close()

            page = browser.new_page(viewport={"width": 1440, "height": 900})
            page.goto(f"{base}/mockups/onboarding-rustig/step-03", wait_until="networkidle")
            page.wait_for_timeout(400)
            ref = hero_crop(page)
            page.close()
            browser.close()

        w = ref.size[0] + dh.size[0] + 8
        canvas = Image.new("RGB", (w, max(ref.size[1], dh.size[1]) + 28), (255, 255, 255))
        canvas.paste(ref, (0, 28))
        canvas.paste(dh, (ref.size[0] + 8, 28))
        d = ImageDraw.Draw(canvas)
        d.text((4, 4), "step-03 (ref)", fill=(40, 40, 40))
        d.text((ref.size[0] + 12, 4), "step-02", fill=(40, 40, 40))
        canvas.save(COMPARE / "step-02-vs-step03-hero-1440.png")
    finally:
        proc.terminate()
        proc.wait(timeout=30)

    for path, variant in ((desk_hero, "desktop"), (mob_hero, "mobile")):
        r = subprocess.run(
            [
                sys.executable,
                str(CHECK),
                str(path),
                "--min-persons",
                "2",
                "--slug",
                "step-02",
                "--variant",
                variant,
            ],
        )
        if r.returncode != 0:
            raise SystemExit(r.returncode)
    print("report + gate OK")


if __name__ == "__main__":
    main()
