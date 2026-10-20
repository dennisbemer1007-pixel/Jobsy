#!/usr/bin/env python3
"""Full-viewport PNG of an onboarding-rustig mockup page (#762 branch HTML)."""
from __future__ import annotations

import argparse
import subprocess
import sys
import time
from pathlib import Path

try:
    from playwright.sync_api import sync_playwright
except ImportError:
    print("playwright required (pip install playwright && playwright install chromium)", file=sys.stderr)
    sys.exit(1)

ROOT = Path(__file__).resolve().parents[2]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("slug", help="e.g. step-06")
    parser.add_argument("out_png")
    parser.add_argument("--width", type=int, default=1440)
    parser.add_argument("--height", type=int, default=900)
    parser.add_argument("--base-url", default="http://127.0.0.1:8765")
    parser.add_argument("--cache-bust", default="20261020")
    args = parser.parse_args()

    out = Path(args.out_png)
    out.parent.mkdir(parents=True, exist_ok=True)
    url = f"{args.base_url.rstrip('/')}/mockups/onboarding-rustig/{args.slug}?cb={args.cache_bust}"

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": args.width, "height": args.height})
        page.goto(url, wait_until="networkidle", timeout=120_000)
        page.wait_for_selector("[data-ob-rustig-screen]", timeout=60_000)
        page.wait_for_timeout(600)
        page.screenshot(path=str(out), full_page=False)
        browser.close()
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
