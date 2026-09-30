"""Kandidaat banen (banenkaart, lijst, detail, sollicitaties, bewaard, Match) mockups. Desktop 1440x900, mobile 390x844 @2x.
Run: python3 basemap.py (once), then python3 build.py [filter]   (needs playwright + /usr/bin/google-chrome)"""
import pathlib, sys
d = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(d))
from playwright.sync_api import sync_playwright
import kd as sm
CHECK = """() => { const r=[]; const W=document.documentElement.clientWidth;
 document.querySelectorAll('body *').forEach(e=>{ if(e.closest('svg')) return; const b=e.getBoundingClientRect(); const cs=getComputedStyle(e); const fs=parseFloat(cs.fontSize);
  if(b.width&&b.right>W+1&&cs.position!=='fixed') r.push('overflowX '+e.className+' '+Math.round(b.right));
  const txt=e.childNodes.length&&[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim());
  if(txt&&e.scrollWidth>e.clientWidth+1&&cs.whiteSpace==='nowrap'&&cs.display!=='inline') r.push('nowrap-overflow '+e.tagName+'.'+e.className+' '+e.textContent.trim().slice(0,30));
  if(txt&&fs<11.5) r.push('small '+fs+' '+e.textContent.slice(0,20)); });
 document.querySelectorAll('table').forEach(t=>{if(t.scrollWidth>t.parentElement.clientWidth+1) r.push('TABLE too wide '+t.scrollWidth+'>'+t.parentElement.clientWidth);});
 return {issues:r.slice(0,12), h:document.documentElement.scrollHeight}; }"""
VP = {"d": (1440, 900, 1), "m": (390, 844, 2)}
only = sys.argv[1:]
(d / "html").mkdir(exist_ok=True)
with sync_playwright() as p:
    br = p.chromium.launch(executable_path="/usr/bin/google-chrome")
    for name, fn, kind in sm.SCREENS:
        if only and not any(o in name for o in only): continue
        html = fn(); f = d / "html" / f"{name}.html"; f.write_text(html)
        w, h, s = VP[kind]
        ctx = br.new_context(viewport=dict(width=w, height=h), device_scale_factor=s, reduced_motion="reduce")
        pg = ctx.new_page(); pg.goto(f.as_uri()); pg.wait_for_timeout(300)
        res = pg.evaluate(CHECK)
        if res["h"] > h:
            pg.set_viewport_size(dict(width=w, height=res["h"])); pg.wait_for_timeout(200)
        pg.screenshot(path=str(d / f"{name}.png"))
        print(name, "h", res["h"], res["issues"] or "ok")
        ctx.close()
    br.close()
