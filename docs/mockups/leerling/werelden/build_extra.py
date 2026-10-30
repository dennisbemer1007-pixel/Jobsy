import json,os
from meta import SCREENS
from PIL import Image
for dev,lab,w in [('desktop','Desktop 1440',1440),('mobile','Mobiel 390',390)]:
    items=[dict(f=f'renders/{dev}/{n}.png',ch=c,t=t,c=cap) for n,c,t,cap in SCREENS]
    phone = dev=='mobile'
    stage = ('<div class="phone"><div id="scroll"><img id="img" alt=""></div></div>' if phone else '<div id="scroll" class="desk"><img id="img" alt=""></div>')
    html=f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><title>Lobsy groep 7/8 · drie werelden · {lab}</title><style>
*{{box-sizing:border-box}}body{{margin:0;font-family:Lexend,system-ui,sans-serif;color:#10284a;background:#ece6dc}}
.bar{{position:sticky;top:0;z-index:5;display:flex;gap:14px;align-items:center;padding:10px 18px;background:#fffdf8;border-bottom:1px solid #e3ddd4;flex-wrap:wrap}}
.bar h1{{font-family:Fraunces,serif;font-size:1.1rem;margin:0;white-space:nowrap}}.meta{{flex:1;min-width:260px}}.meta b{{font-family:Fraunces,serif}}
.meta .ch{{color:#d4532f;font-weight:700;font-size:.78rem;text-transform:uppercase;letter-spacing:.06em}}.meta p{{margin:2px 0 0;font-size:.88rem;color:#4c5a70}}
.note{{width:100%;font-size:.85rem;background:#e7f0ec;border-left:4px solid #2f7d5b;padding:6px 12px;border-radius:6px}}
button,select{{font:inherit;border:1px solid #c9c1b4;background:#fff;color:#10284a;border-radius:999px;padding:8px 16px;cursor:pointer;font-weight:600}}
button.p{{background:#10284a;color:#fff;border-color:#10284a}}button:disabled{{opacity:.35;cursor:default}}
.stage{{display:flex;justify-content:center;padding:20px 0}}
.desk{{width:min(1440px,96vw);height:calc(100vh - 150px);overflow:auto;background:#fff;box-shadow:0 10px 40px rgba(16,40,74,.2);border-radius:10px}}.desk img{{width:100%;display:block}}
.phone{{width:414px;height:calc(100vh - 150px);max-height:880px;background:#1b2233;border-radius:46px;padding:12px}}
.phone #scroll{{width:390px;height:100%;overflow-y:auto;overflow-x:hidden;border-radius:36px;background:#fff}}.phone img{{display:block;width:390px}}
</style></head><body><div class="bar"><h1>Lobsy · groep 7/8 · drie werelden · {lab}</h1><div class="meta"><div class="ch" id="ch"></div><b id="t"></b><p id="c"></p></div>
<select id="sel" aria-label="Kies scherm"></select><button id="prev">← Vorige</button><button id="next" class="p">Volgende →</button>
<div class="note"><b>Zelfde vragen en spellen, alleen een andere wereld.</b> Alle drie de werelden hebben dezelfde 60 vragen in dezelfde volgorde, dezelfde denkspellen en regels, hetzelfde Pauze-eiland en dezelfde uitkomst.</div></div>
<div class="stage">{stage}</div><script>const I={json.dumps(items,ensure_ascii=False)};let i=0;const $=x=>document.getElementById(x);
I.forEach((s,k)=>{{const o=document.createElement('option');o.value=k;o.textContent=(k+1)+'. '+s.ch+' · '+s.t;$('sel').appendChild(o)}});
function show(n){{i=Math.max(0,Math.min(I.length-1,n));const s=I[i];$('img').src=s.f;$('ch').textContent=(i+1)+' van '+I.length+' · '+s.ch;$('t').textContent=s.t;$('c').textContent=s.c;$('sel').value=i;$('prev').disabled=i==0;$('next').disabled=i==I.length-1;$('scroll').scrollTop=0}}
$('prev').onclick=()=>show(i-1);$('next').onclick=()=>show(i+1);$('sel').onchange=e=>show(+e.target.value);$('img').onclick=()=>show(i+1);
document.onkeydown=e=>{{if(e.key=='ArrowRight')show(i+1);if(e.key=='ArrowLeft')show(i-1)}};show(0);</script></body></html>'''
    open(f'walkthrough-{"desktop" if dev=="desktop" else "mobiel"}.html','w').write(html)
print('ok')
