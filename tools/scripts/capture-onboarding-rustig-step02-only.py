#!/usr/bin/env python3
import io
import subprocess
import time
from pathlib import Path

from PIL import Image, ImageFilter
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
ZOOM = Path("/opt/cursor/artifacts/onboarding-rustig-lobster-zoom")


def main() -> None:
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
            for w, h, name in [(1280, 900, "step-02-desktop-1280.png"), (390, 844, "step-02-mobile.png")]:
                page = browser.new_page(viewport={"width": w, "height": h})
                page.goto(f"{base}/mockups/onboarding-rustig/step-02", wait_until="networkidle")
                page.wait_for_selector("[data-ob-rustig-screen]", timeout=30_000)
                page.wait_for_timeout(400)
                page.screenshot(path=str(OUT / name))
                print("wrote", OUT / name)
                if name.endswith("mobile.png"):
                    hero = page.locator("[data-testid=ob-rustig-hero]").bounding_box()
                    im = Image.open(io.BytesIO(page.screenshot(full_page=False)))
                    x0, y0 = int(hero["x"]), int(hero["y"])
                    bw, bh = int(hero["width"]), int(hero["height"])
                    band = im.crop((x0, y0, x0 + bw, y0 + bh))
                    cx, cy = int(bw * 0.76), int(bh * 0.55)
                    crop = band.crop((max(0, cx - 14), max(0, cy - 16), min(bw, cx + 14), min(bh, cy + 16)))
                    zoom = crop.resize((crop.size[0] * 4, crop.size[1] * 4), Image.Resampling.LANCZOS)
                    zoom = zoom.filter(ImageFilter.UnsharpMask(radius=1, percent=80, threshold=2))
                    zout = ZOOM / "step-02-mobile-lobster-4x.png"
                    zoom.save(zout)
                    print("wrote", zout)
                page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)


if __name__ == "__main__":
    main()
