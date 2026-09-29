"""Render Lobsy Beheer redesign mockups to PNG. Run: python3 build.py"""
import pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from playwright.sync_api import sync_playwright
from screens_a import d1, d2
from screens_b import d3, d4
from screens_c import d5, d6, m1, m2
out = pathlib.Path(__file__).parent
(out / "html").mkdir(exist_ok=True)
SCREENS = [("ad-d1-dashboard", d1, "d"), ("ad-d2-gebruikers-2fa", d2, "d"), ("ad-d3-organisaties", d3, "d"),
           ("ad-d4-platforminstellingen", d4, "d"), ("ad-d5-beveiliging-audit", d5, "d"), ("ad-d6-financien", d6, "d"),
           ("ad-m1-dashboard", m1, "m"), ("ad-m2-2fa-resetten", m2, "m")]
CHECK = """() => { const r=[]; const W=document.documentElement.clientWidth;
 document.querySelectorAll('body *').forEach(e=>{const b=e.getBoundingClientRect(); const fs=parseFloat(getComputedStyle(e).fontSize);
  if(b.width&&b.right>W+1&&getComputedStyle(e).position!=='fixed') r.push('overflowX '+e.className+' '+Math.round(b.right));
  if(e.scrollWidth>e.clientWidth+1&&['hidden','clip'].includes(getComputedStyle(e).overflowX)&&e.tagName!=='svg'&&!e.closest('svg')&&e.children.length===0) r.push('clipped '+e.tagName+'.'+e.className+' '+e.textContent.slice(0,30));
  if(e.childNodes.length&&[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim())&&fs<11.5) r.push('small '+fs+' '+e.textContent.slice(0,20));});
 document.querySelectorAll('table').forEach(t=>{if(t.scrollWidth>t.parentElement.clientWidth+1) r.push('TABLE too wide '+t.scrollWidth+'>'+t.parentElement.clientWidth);});
 return {issues:r.slice(0,15), h:document.documentElement.scrollHeight}; }"""
only = sys.argv[1:]
with sync_playwright() as p:
    br = p.chromium.launch(executable_path="/usr/bin/google-chrome")
    for name, fn, kind in SCREENS:
        if only and not any(o in name for o in only): continue
        html = fn(); (out / "html" / f"{name}.html").write_text(html)
        vp = dict(width=1440, height=1330 if "d4" in name else 900) if kind == "d" else dict(width=390, height=844)
        ctx = br.new_context(viewport=vp, device_scale_factor=1 if kind == "d" else 2, reduced_motion="reduce")
        pg = ctx.new_page(); pg.set_content(html); pg.wait_for_timeout(250)
        res = pg.evaluate(CHECK)
        pg.screenshot(path=str(out / f"{name}.png"))
        print(name, "height", res["h"], res["issues"] or "ok")
        ctx.close()
    br.close()
