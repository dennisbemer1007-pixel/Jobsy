#!/usr/bin/env python3
"""Capture onboarding-rustig mockups at 1024/1280 desktop and 390 mobile."""

from __future__ import annotations

import subprocess
import sys
import time
from pathlib import Path

from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
SLUGS = ["start", "step-01", "step-02", "step-03", "step-04", "step-05"]
DESKTOP_WIDTHS = [1024, 1280]
MOBILE = (390, 844)


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
            for w in range(60):
                try:
                    page = browser.new_page()
                    page.goto(f"{base}/mockups/onboarding-rustig/start", timeout=3000)
                    page.close()
                    break
                except Exception:
                    time.sleep(1)
            else:
                raise SystemExit("web server did not start")
            for slug in SLUGS:
                path = f"/mockups/onboarding-rustig/{slug}"
                for width in DESKTOP_WIDTHS:
                    page = browser.new_page(viewport={"width": width, "height": 900})
                    page.goto(f"{base}{path}", wait_until="networkidle")
                    page.wait_for_selector("[data-ob-rustig-screen]", timeout=30_000)
                    page.wait_for_timeout(400)
                    out = OUT / f"{slug}-desktop-{width}.png"
                    page.screenshot(path=str(out), full_page=False)
                    page.close()
                    print("wrote", out)
                mw, mh = MOBILE
                page = browser.new_page(viewport={"width": mw, "height": mh})
                page.goto(f"{base}{path}", wait_until="networkidle")
                page.wait_for_selector("[data-ob-rustig-screen]", timeout=30_000)
                page.wait_for_timeout(400)
                out = OUT / f"{slug}-mobile.png"
                page.screenshot(path=str(out), full_page=False)
                page.close()
                print("wrote", out)
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)


if __name__ == "__main__":
    main()
