"""Render Lobsy werkgever (bedrijfsmanager) redesign mockups to PNG. Run: python3 build.py [filter]"""
import pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from playwright.sync_api import sync_playwright
from screens_dash import dashboard, vacatures
out = pathlib.Path(__file__).parent
(out / "html").mkdir(exist_ok=True)
SCREENS = [
    ("bm-d1-dashboard", lambda: dashboard("bm"), "d"),
    ("bm-d2-vacatures", lambda: vacatures("bm"), "d"),
    ("bm-d7-dashboard-regiomanager", lambda: dashboard("rm"), "d"),
    ("bm-d8-vacatures-vestigingsmanager", lambda: vacatures("vm"), "d"),
]
try:
    from screens_more import d3, d4, d5, m1, m2
    from screens_insights import d6, m3
    SCREENS[2:2] = [("bm-d3-sollicitaties-pipeline", d3, "d"), ("bm-d4-vestigingen-team", d4, "d"),
                    ("bm-d5-tokens-facturen", d5, "d"), ("bm-d6-kandidaatinzichten", d6, "d")]
    SCREENS += [("bm-m1-dashboard", m1, "m"), ("bm-m2-sollicitatie", m2, "m"), ("bm-m3-kandidaatinzichten-gratis", m3, "m")]
except ImportError as e:
    print("screens_more not ready:", e)
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
        vp = dict(width=1440, height=900) if kind == "d" else dict(width=390, height=844 if "m3" not in name else 1030)
        ctx = br.new_context(viewport=vp, device_scale_factor=1 if kind == "d" else 2, reduced_motion="reduce")
        pg = ctx.new_page(); pg.set_content(html); pg.wait_for_timeout(250)
        res = pg.evaluate(CHECK)
        pg.screenshot(path=str(out / f"{name}.png"))
        print(name, "height", res["h"], res["issues"] or "ok")
        ctx.close()
    br.close()
