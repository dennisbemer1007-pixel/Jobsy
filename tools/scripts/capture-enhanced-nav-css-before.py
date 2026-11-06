#!/usr/bin/env python3
"""Capture unstyled enhanced-nav screenshots on Acceptatie (pre-fix)."""
from __future__ import annotations

import asyncio
from pathlib import Path

from playwright.async_api import async_playwright

BASE = "https://acceptatie.lobsy.nl"
EMAIL = "onboarding.e2e@jobsy.local"
PASSWORD = "Jobsy123!"
OUT = Path("/workspace/artifacts/enhanced-nav-css/before")


async def accept_cookies(page) -> None:
    for sel in (
        "button[data-consent='analytics']",
        "button[data-consent='necessary']",
        "button:has-text('Alles accepteren')",
        "button:has-text('Accept all')",
    ):
        btn = page.locator(sel).first
        if await btn.count() > 0:
            try:
                await btn.click(timeout=3000)
                return
            except Exception:
                pass


async def login(page) -> None:
    await page.goto(f"{BASE}/login", wait_until="domcontentloaded", timeout=90000)
    await accept_cookies(page)
    await page.fill("input[name='email']", EMAIL)
    await page.fill("input[name='password']", PASSWORD)
    await page.locator("button[type='submit']").click()
    await page.wait_for_timeout(2000)


async def capture(viewport: tuple[int, int], tag: str) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True)
        context = await browser.new_context(
            viewport={"width": viewport[0], "height": viewport[1]},
            locale="nl-NL",
            is_mobile=viewport[0] < 500,
            has_touch=viewport[0] < 500,
        )
        page = await context.new_page()
        await login(page)
        await page.goto(f"{BASE}/banenkaart", wait_until="domcontentloaded", timeout=90000)
        await page.wait_for_timeout(1500)

        for href, name in (
            ("/candidate/ontdekkingsreis", "ontdekkingsreis"),
            ("/candidate/paspoort", "paspoort"),
        ):
            if viewport[0] < 500:
                link = page.locator(f"nav.bottom-nav a[href='{href}']").first
            else:
                link = page.locator(f"a[href='{href}']").first
            await link.click()
            await page.wait_for_timeout(1200)
            await page.screenshot(path=str(OUT / f"before-{tag}-{name}.png"))

        await browser.close()


async def main() -> None:
    await capture((1440, 900), "1440")
    await capture((390, 844), "390")


if __name__ == "__main__":
    asyncio.run(main())
