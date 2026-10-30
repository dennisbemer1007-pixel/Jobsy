#!/usr/bin/env python3
"""Static fixture page using live Jobsy CSS/assets (fake copy, real styling)."""
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
CSS = (ROOT / "Jobsy.Web/wwwroot/css/features/ontdekkingsreis.css").read_text()
HERO = (ROOT / "Jobsy.Web/wwwroot/css/features/candidate-hero-compact-preview.css").read_text()
BASE = "http://127.0.0.1:5201"
V = "20261021-01"

HTML = f"""<!DOCTYPE html><html lang="nl"><head><meta charset="utf-8"/><style>{HERO}{CSS}</style></head><body>
<div class="journey journey--compact-preview-c">
<header class="cand-hero cand-compact-hero cand-hero--compact-preview-c cand-hero--discovery-overview-v8">
<div class="cand-compact-hero__art"><img src="{BASE}/images/candidate-illustrated/discovery-overview-strip.webp?v={V}"/></div></header>
<div class="journey-step"><div class="journey-overview" data-testid="discovery-overview-route">
<h1>Verder ontdekken</h1>
<div class="journey-route"><div class="journey-route__canvas" id="discovery-route-canvas">
<svg class="journey-route__svg"><path id="discovery-route-path" d=""/></svg>
<div class="journey-route__stops">
<div class="journey-route__stop"><button type="button" class="journey-route__stop-btn"><div class="journey-route__vignette is-done" data-discovery-vignette><img src="{BASE}/images/discovery/haltes/strand.webp?v={V}"/></div><div class="journey-route__label"><p class="journey-route__title">Het strand</p><span class="journey-route__status is-done">Klaar</span></div></button></div>
<div class="journey-route__stop"><button type="button" class="journey-route__stop-btn"><div class="journey-route__vignette" data-discovery-vignette><img src="{BASE}/images/discovery/haltes/kust.webp?v={V}"/></div><div class="journey-route__label"><p class="journey-route__title">Aan de kust</p><span class="journey-route__status is-progress">Nog 1 stap</span></div></button></div>
<div class="journey-route__stop"><button type="button" class="journey-route__stop-btn"><div class="journey-route__vignette is-done" data-discovery-vignette><img src="{BASE}/images/discovery/haltes/rotsen.webp?v={V}"/></div><div class="journey-route__label"><p class="journey-route__title">Tussen de rotsen</p><span class="journey-route__status is-done">Klaar</span></div></button></div>
<div class="journey-route__stop"><button type="button" class="journey-route__stop-btn"><div class="journey-route__vignette is-done" data-discovery-vignette><img src="{BASE}/images/discovery/haltes/diepte.webp?v={V}"/></div><div class="journey-route__label"><p class="journey-route__title">In de diepte</p><span class="journey-route__status is-done">Klaar</span></div></button></div>
<div class="journey-route__stop is-current"><button type="button" class="journey-route__stop-btn"><div class="journey-route__vignette is-current" data-discovery-vignette><img src="{BASE}/images/discovery/haltes/licht.webp?v={V}"/></div><div class="journey-route__label"><p class="journey-route__title">Naar het licht</p><span class="journey-route__status is-open">Open</span><span class="btn btn-primary journey-route__cta">Ga verder</span></div></button></div>
</div></div>
<div class="journey-route__panel-wrap"><div class="journey-route__connector"></div><article class="journey-route__panel"><header class="journey-route__panel-head"><img class="journey-route__panel-vig" src="{BASE}/images/discovery/haltes/licht.webp?v={V}"/><div><h3>Naar het licht</h3><p>Afronden en je paspoort bekijken.</p></div></header><ul class="journey-route__sections"><li><div><h4>Sterkte</h4><p>Nieuwe dingen uitproberen.</p></div><button type="button" class="journey-route__adjust">Aanpassen</button></li></ul></article></div>
<h2 class="journey-q">Je tests</h2>
<div class="journey-route__tests"><div class="journey-route__test"><img class="journey-route__test-art" src="{BASE}/images/discovery/test-accents/competency.webp?v={V}"/><p class="journey-route__test-title">Wat ik leuk vind</p><span class="status-pill">Klaar</span></div><div class="journey-route__test is-todo"><img class="journey-route__test-art" src="{BASE}/images/discovery/test-accents/values.webp?v={V}"/><p class="journey-route__test-title">Waarden</p><span class="status-pill journey-route__test-pill-todo">Nog te doen</span><button type="button" class="btn btn-primary journey-route__test-start">Start</button></div></div>
</div></div></div></body></html>"""

OUT = Path("/opt/cursor/artifacts")

def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    with sync_playwright() as p:
        browser = p.chromium.launch()
        for width, height, tag in ((1440, 900, "1440"), (390, 844, "390")):
            page = browser.new_page(viewport={"width": width, "height": height})
            page.set_content(HTML, wait_until="networkidle")
            page.wait_for_timeout(300)
            page.locator(".journey-route__adjust").click()
            page.locator(".journey-route__test-start").click()
            path = OUT / f"ontdekkingsreis-route-product-{tag}.png"
            page.screenshot(path=str(path), full_page=True)
            print(path)
        browser.close()

if __name__ == "__main__":
    main()
