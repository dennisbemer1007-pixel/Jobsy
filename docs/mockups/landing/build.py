"""Render Lobsy landing mockups (lp-*) to PNG. Run: python3 build.py [filter]"""
import os, pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from playwright.sync_api import sync_playwright
from ui import page
from w_land import header, hero, wat_is, kreeft
from w_land2 import krijgt, voor_wie, trust, faq, final_footer
from w_test import d2, d3, d4, m1, m2, m3
out = pathlib.Path(__file__).parent
(out / "html").mkdir(exist_ok=True)
def d1(zw=False):
    return page("".join(f(zw) for f in (header, hero, wat_is, kreeft, krijgt, voor_wie, trust, faq, final_footer)))
SCREENS = [("lp-d1-landing", d1, "full"), ("lp-d2-test-start", d2, "d"), ("lp-d3-test-vraag", d3, "d"), ("lp-d4-test-resultaat", d4, "d"),
           ("lp-m1-hero", m1, "mfull"), ("lp-m2-test-vraag", m2, "m"), ("lp-m3-resultaat", m3, "m"),
           ("lp-d1-landing-zw", lambda: d1(True), "full"), ("lp-m1-hero-zw", lambda: m1(True), "mfull"),
           ("lp-d4-test-resultaat-zw", lambda: d4(True), "d"), ("lp-m3-resultaat-zw", lambda: m3(True), "m")]
CHECK = """() => { const r=[]; const W=document.documentElement.clientWidth;
 document.querySelectorAll('body *').forEach(e=>{const b=e.getBoundingClientRect(); const fs=parseFloat(getComputedStyle(e).fontSize);
  if(b.width&&b.right>W+1&&!e.closest('.hero,.scene,.story,.final,.mscene,.wscene,.wave-wrap,.msc,.side-scene,.rh-art')) r.push('overflowX '+e.className+' '+Math.round(b.right));
  if(e.scrollWidth>e.clientWidth+1&&['hidden','clip'].includes(getComputedStyle(e).overflowX)&&!e.closest('svg')&&e.children.length===0) r.push('clipped '+e.tagName+'.'+e.className+' '+e.textContent.slice(0,30));
  if(e.childNodes.length&&[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim())&&fs<11.5) r.push('small '+fs+' '+e.textContent.slice(0,20));});
 document.querySelectorAll('.ppc .pill, .zpp .pill').forEach(e=>{const c=e.closest('.ppc,.zpp').getBoundingClientRect(); const b=e.getBoundingClientRect();
  if(b.right>c.right+1||b.left<c.left-1||b.right>W-12) r.push('chip outside card '+e.textContent.trim());});
 return {issues:r.slice(0,15), h:document.documentElement.scrollHeight}; }"""
only = sys.argv[1:]
with sync_playwright() as p:
    chrome = os.environ.get("CHROME_PATH") or ("/usr/bin/google-chrome" if os.path.exists("/usr/bin/google-chrome") else None)
    br = p.chromium.launch(executable_path=chrome) if chrome else p.chromium.launch()
    for name, fn, kind in SCREENS:
        if only and not any(o in name for o in only): continue
        html = fn(); (out / "html" / f"{name}.html").write_text(html)
        vp = dict(width=1440, height=900) if kind in ("d", "full") else dict(width=390, height=844)
        ctx = br.new_context(viewport=vp, device_scale_factor=1 if kind in ("d", "full") else 2, reduced_motion="reduce")
        pg = ctx.new_page(); pg.set_content(html); pg.wait_for_timeout(300)
        res = pg.evaluate(CHECK)
        pg.screenshot(path=str(out / f"{name}.png"), full_page=kind in ("full", "mfull", "d"))
        print(name, "height", res["h"], res["issues"] or "ok")
        ctx.close()
    br.close()
