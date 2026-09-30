"""Render werkgever-registratie mockups (wr-*) to PNG. Run: python3 build.py [filter]"""
import os, pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from playwright.sync_api import sync_playwright
from s_flow import d1, d2, d3, d4, d10
from s_profile import d5, d6, d7
from s_verify import d8, d9, d11, d12
from s_mob import m1, m2, m3, m4, m5, m6, m7
out = pathlib.Path(__file__).parent
(out / "html").mkdir(exist_ok=True)
SCREENS = [("wr-d1-start-zoeken", d1, "d"), ("wr-d2-zoekresultaten", d2, "d"), ("wr-d3-bedrijf-vestigingen", d3, "d"), ("wr-d4-gegevens-account", d4, "d"),
           ("wr-d5-branche", d5, "d"), ("wr-d6-kernwaarden", d6, "d"), ("wr-d7-betrokkenheid", d7, "d"),
           ("wr-d8-verificatie-keuze", d8, "d"), ("wr-d9-brief-onderweg", d9, "d"), ("wr-d10-al-geregistreerd", d10, "d"),
           ("wr-d11-welkom-dashboard", d11, "d"), ("wr-d12-vacature-cultuur", d12, "d"),
           ("wr-m1-zoeken", m1, "m"), ("wr-m2-vestiging-keuze", m2, "m"), ("wr-m3-branche", m3, "m"), ("wr-m4-kernwaarden", m4, "m"),
           ("wr-m5-betrokkenheid", m5, "m"), ("wr-m6-verificatie-keuze", m6, "m"), ("wr-m7-brief-code", m7, "m")]
CHECK = """() => { const r=[]; const W=document.documentElement.clientWidth;
 document.querySelectorAll('body *').forEach(e=>{const b=e.getBoundingClientRect(); const fs=parseFloat(getComputedStyle(e).fontSize);
  if(b.width&&b.right>W+1&&!e.closest('.gart,.env,.welc')) r.push('overflowX '+e.className+' '+Math.round(b.right));
  if(e.scrollWidth>e.clientWidth+1&&['hidden','clip','auto'].includes(getComputedStyle(e).overflowX)&&!e.closest('svg')&&e.children.length===0) r.push('clipped '+e.tagName+'.'+e.className+' '+e.textContent.slice(0,30));
  if(e.childNodes.length&&[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim())&&fs<11.5) r.push('small '+fs+' '+e.textContent.slice(0,20));
  if(e.children.length===0&&e.textContent.trim()&&b.width>0){const p=e.parentElement.getBoundingClientRect(); if(b.right>p.right+2&&getComputedStyle(e.parentElement).overflow==='visible'&&!e.closest('.gart,.env,.welc,.trk')) r.push('spill '+e.tagName+'.'+e.className+' '+e.textContent.slice(0,24));}});
 return {issues:r.slice(0,15), h:document.documentElement.scrollHeight}; }"""
only = sys.argv[1:]
with sync_playwright() as p:
    chrome = os.environ.get("CHROME_PATH") or ("/usr/bin/google-chrome" if os.path.exists("/usr/bin/google-chrome") else None)
    br = p.chromium.launch(executable_path=chrome) if chrome else p.chromium.launch()
    for name, fn, kind in SCREENS:
        if only and not any(o in name for o in only): continue
        html = fn(); (out / "html" / f"{name}.html").write_text(html)
        vp = dict(width=1440, height=900) if kind == "d" else dict(width=390, height=844)
        ctx = br.new_context(viewport=vp, device_scale_factor=1 if kind == "d" else 2, reduced_motion="reduce")
        pg = ctx.new_page(); pg.set_content(html); pg.wait_for_timeout(300)
        res = pg.evaluate(CHECK)
        if kind == "m":
            pg.set_viewport_size({"width": 390, "height": max(844, res["h"])}); pg.wait_for_timeout(100)
        pg.screenshot(path=str(out / f"{name}.png"), full_page=True)
        print(name, "height", res["h"], res["issues"] or "ok")
        ctx.close()
    br.close()
