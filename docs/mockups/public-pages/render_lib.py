import os
from playwright.sync_api import sync_playwright
CHECK = """() => { const r=[]; const W=document.documentElement.clientWidth;
 document.querySelectorAll('body *').forEach(e=>{const b=e.getBoundingClientRect(); const fs=parseFloat(getComputedStyle(e).fontSize);
  if(b.width&&b.right>W+1&&!e.closest('.scene,.wave-wrap,.eh,.ph,svg')) r.push('overflowX '+e.tagName+'.'+e.className+' '+Math.round(b.right));
  if(e.childNodes.length&&[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim())&&fs<11.5) r.push('small '+fs+' '+e.textContent.slice(0,20));});
 return {issues:[...new Set(r)].slice(0,12), h:document.documentElement.scrollHeight}; }"""
def render(out, screens, only):
    (out / "html").mkdir(exist_ok=True)
    with sync_playwright() as p:
        chrome = "/usr/bin/google-chrome" if os.path.exists("/usr/bin/google-chrome") else None
        br = p.chromium.launch(executable_path=chrome) if chrome else p.chromium.launch()
        for name, fn, kind in screens:
            if only and not any(o in name for o in only): continue
            html = fn(); (out / "html" / f"{name}.html").write_text(html)
            vp = dict(width=1440, height=900) if kind == "d" else dict(width=390, height=844)
            ctx = br.new_context(viewport=vp, device_scale_factor=1 if kind == "d" else 2, reduced_motion="reduce")
            pg = ctx.new_page(); pg.set_content(html); pg.wait_for_timeout(250)
            res = pg.evaluate(CHECK)
            pg.screenshot(path=str(out / f"{name}.png"), full_page=True)
            print(name, res["h"], res["issues"] or "ok"); ctx.close()
        br.close()
