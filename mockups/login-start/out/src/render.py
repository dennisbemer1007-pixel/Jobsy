# Render alle mockups op 1440 (desktop) en 390 (mobiel, 2x) + overflow/overlap-check
import os,sys,json
from playwright.sync_api import sync_playwright
D=os.path.dirname(os.path.abspath(__file__));only=sys.argv[1:]
files=sorted(f[:-5] for f in os.listdir(D) if f.endswith('.html') and (not only or f[:-5] in only))
CHECK="""()=>{const o=[];const W=document.documentElement.clientWidth;
if(document.documentElement.scrollWidth>W)o.push('scrollWidth '+document.documentElement.scrollWidth);const leaves=[];
for(const el of document.querySelectorAll('body *')){const r=el.getBoundingClientRect();if(!r.width||!r.height)continue;
 if(getComputedStyle(el).position==='fixed'||el.closest('svg.map'))continue;
 if(r.right>W+.5||r.left<-.5)o.push('beyond '+el.tagName+'.'+el.className);
 if(el.scrollWidth>el.clientWidth+1&&getComputedStyle(el).overflow!='visible')o.push('clipped '+el.className);
 const own=[...el.childNodes].some(n=>n.nodeType==3&&n.textContent.trim());
 if(own||el.classList.contains('in')||el.classList.contains('btn'))leaves.push([el,r]);
 if(own){const p=el.parentElement.getBoundingClientRect();if(r.right>p.right+1.5)o.push('text out '+el.textContent.trim().slice(0,30));}}
for(let i=0;i<leaves.length;i++)for(let j=i+1;j<leaves.length;j++){const [a,ra]=leaves[i],[c,rc]=leaves[j];if(a.contains(c)||c.contains(a))continue;
 const ox=Math.min(ra.right,rc.right)-Math.max(ra.left,rc.left),oy=Math.min(ra.bottom,rc.bottom)-Math.max(ra.top,rc.top);
 if(ox>2&&oy>2)o.push('overlap "'+a.textContent.trim().slice(0,20)+'" / "'+c.textContent.trim().slice(0,20)+'"');}
return [...new Set(o)].slice(0,15)}"""
with sync_playwright() as p:
  b=p.chromium.launch(executable_path='/usr/bin/google-chrome')
  for f in files:
    for w,h,s,suf in [(1440,900,1,'desktop'),(390,844,2,'mobiel')]:
      pg=b.new_page(viewport={'width':w,'height':h},device_scale_factor=s)
      pg.goto(f'file://{D}/{f}.html',wait_until='networkidle')
      iss=pg.evaluate(CHECK)
      pg.screenshot(path=f'{D}/../{f}-{suf}.png',full_page=True)
      print(f,suf,pg.evaluate('document.documentElement.scrollHeight'),json.dumps(iss) if iss else 'ok');pg.close()
  b.close()
