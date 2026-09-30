"""Render Lobsy Intermediair (uitzendbureau) mockups to PNG. Run: python3 build.py [filter]"""
import os, pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from playwright.sync_api import sync_playwright
from screens_im import d1, d2, d3, d4, d5, m1, m2
out = pathlib.Path(__file__).parent
(out / "html").mkdir(exist_ok=True)
SCREENS = [
    ("im-d1-dashboard", d1, "d"),
    ("im-d2-opdrachtgevers", d2, "d"),
    ("im-d3-vacature-opdrachtgever-locatie", d3, "d"),
    ("im-d4-kandidaatweergave", d4, "d"),
    ("im-d5-opdrachtgever-toevoegen-kvk", d5, "d"),
    ("im-m1-dashboard", m1, "m"),
    ("im-m2-vacature-locatie", m2, "m"),
]
CHECK = """() => { const r=[]; const W=document.documentElement.clientWidth;
 document.querySelectorAll('body *').forEach(e=>{const b=e.getBoundingClientRect(); const fs=parseFloat(getComputedStyle(e).fontSize);
  if(b.width&&b.right>W+1&&getComputedStyle(e).position!=='fixed') r.push('overflowX '+e.className+' '+Math.round(b.right));
  if(e.scrollWidth>e.clientWidth+1&&['hidden','clip'].includes(getComputedStyle(e).overflowX)&&e.tagName!=='svg'&&!e.closest('svg')&&e.children.length===0) r.push('clipped '+e.tagName+'.'+e.className+' '+e.textContent.slice(0,30));
  if(e.childNodes.length&&[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim())&&fs<11.5) r.push('small '+fs+' '+e.textContent.slice(0,20));});
 document.querySelectorAll('table').forEach(t=>{if(t.scrollWidth>t.parentElement.clientWidth+1) r.push('TABLE too wide '+t.scrollWidth+'>'+t.parentElement.clientWidth);});
 return {issues:r.slice(0,15), h:document.documentElement.scrollHeight}; }"""
only = sys.argv[1:]
with sync_playwright() as p:
    exe = os.environ.get("CHROME_PATH")  # optional; default = Playwright chromium
    br = p.chromium.launch(**({"executable_path": exe} if exe else {}))
    for name, fn, kind in SCREENS:
        if only and not any(o in name for o in only): continue
        html = fn(); (out / "html" / f"{name}.html").write_text(html)
        vp = dict(width=1440, height=900) if kind == "d" else dict(width=390, height=844)
        ctx = br.new_context(viewport=vp, device_scale_factor=1 if kind == "d" else 2, reduced_motion="reduce")
        pg = ctx.new_page(); pg.set_content(html); pg.wait_for_timeout(250)
        res = pg.evaluate(CHECK)
        pg.screenshot(path=str(out / f"{name}.png"), full_page=(name == "im-d4-kandidaatweergave"))
        print(name, "height", res["h"], res["issues"] or "ok")
        ctx.close()
    br.close()
