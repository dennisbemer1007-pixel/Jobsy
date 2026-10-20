#!/usr/bin/env python3
"""Capture step-02 heroes and block unless YOLO person margins pass (desktop + mobile)."""

from __future__ import annotations

import io
import subprocess
import sys
import time
from pathlib import Path

from PIL import Image
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
HERO = Path("/opt/cursor/artifacts/onboarding-rustig-hero-crops")
CHECK = ROOT / "tools/scripts/check-onboarding-rustig-hero-persons.py"


def main() -> None:
    HERO.mkdir(parents=True, exist_ok=True)
    subprocess.run(["pkill", "-f", "127.0.0.1:8765"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    proc = subprocess.Popen(
        ["dotnet", "run", "--project", str(ROOT / "Jobsy.Web/Jobsy.Web.csproj"), "--urls", "http://127.0.0.1:8765"],
        cwd=ROOT,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    base = "http://127.0.0.1:8765"
    desk_out = HERO / "step-02-desktop-hero-1280.png"
    mob_out = HERO / "step-02-mobile-hero.png"
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

            def save_hero(slug: str, viewport: dict, path: Path) -> None:
                page = browser.new_page(viewport=viewport)
                page.goto(f"{base}/mockups/onboarding-rustig/{slug}", wait_until="networkidle")
                page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
                page.wait_for_timeout(500)
                box = page.locator("[data-testid=ob-rustig-hero]").bounding_box()
                im = Image.open(io.BytesIO(page.screenshot(full_page=False)))
                x0, y0 = int(box["x"]), int(box["y"])
                w, h = int(box["width"]), int(box["height"])
                im.crop((x0, y0, x0 + w, y0 + h)).save(path)
                page.close()

            save_hero("step-02", {"width": 1280, "height": 900}, desk_out)
            save_hero("step-02", {"width": 390, "height": 844}, mob_out)
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)

    r = subprocess.run(
        [sys.executable, str(CHECK), str(desk_out), str(mob_out), "--min-persons", "2"],
    )
    raise SystemExit(r.returncode)


if __name__ == "__main__":
    main()
