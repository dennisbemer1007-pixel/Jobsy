# Bouwt walkthrough-desktop.html, walkthrough-mobiel.html en de print-HTML voor de pdf's (renders -> jpg).
import json, os, html
from PIL import Image
R='/workspace/kreeft-kandidaat-mockup/'; os.chdir(R)
S=json.load(open('src/screens.json'))
def rp(s,mob):
    nn=f"{s['num']:02d}"; return f"renders/{'mobile' if mob else 'desktop'}/{nn}-{s['slug']}-{'mobiel-390' if mob else 'desktop-1440'}.png"
items=[{'num':s['num'],'chapter':s['chapter'],'title':s['title'],'caption':s['caption'],'d':rp(s,0),'m':rp(s,1)} for s in S]
CSS='''@font-face{font-family:F;src:url(src/Fraunces.ttf)}@font-face{font-family:N;src:url(src/NunitoSans.ttf)}
*{box-sizing:border-box}body{margin:0;font-family:N,system-ui,sans-serif;color:#0f2d5c;background:#efe9df}
.bar{position:sticky;top:0;z-index:5;display:flex;gap:14px;align-items:center;padding:10px 18px;background:#fcf8f2;border-bottom:1px solid #e3ddd4}
.bar h1{font-family:F,serif;font-size:1.1rem;margin:0;white-space:nowrap}.meta{flex:1;min-width:0}.meta b{font-family:F,serif;font-size:1.05rem}
.meta .ch{color:#df5530;font-weight:800;font-size:.8rem;text-transform:uppercase;letter-spacing:.05em}.meta p{margin:2px 0 0;font-size:.9rem;color:#4a5670}
button,select{font:inherit;border:1px solid #c9c1b4;background:#fff;color:#0f2d5c;border-radius:999px;padding:8px 16px;cursor:pointer;font-weight:700}
button.p{background:#0f2d5c;color:#fff;border-color:#0f2d5c}button:disabled{opacity:.35;cursor:default}select{max-width:260px;font-weight:600}
.hint{font-size:.78rem;color:#7a8296;white-space:nowrap}'''
JS='''const I=%s;let i=0;const $=q=>document.querySelector(q);
const sel=$('#sel');I.forEach((s,k)=>{const o=document.createElement('option');o.value=k;o.textContent=String(s.num).padStart(2,'0')+'  '+s.title;sel.appendChild(o)});
function show(k){i=Math.max(0,Math.min(I.length-1,k));const s=I[i];$('#img').src=s.%s;$('#ch').textContent=String(s.num).padStart(2,'0')+' / '+I.length+' · '+s.chapter;$('#t').textContent=s.title;$('#c').textContent=s.caption;
sel.value=i;$('#prev').disabled=i==0;$('#next').disabled=i==I.length-1;$('#scroll').scrollTop=0;history.replaceState(null,'','#'+s.num);if(I[i+1]){new Image().src=I[i+1].%s}}
$('#prev').onclick=()=>show(i-1);$('#next').onclick=()=>show(i+1);sel.onchange=()=>show(+sel.value);
addEventListener('keydown',e=>{if(e.key=='ArrowRight'){show(i+1);e.preventDefault()}if(e.key=='ArrowLeft'){show(i-1);e.preventDefault()}});
const h=+location.hash.slice(1);show(h?I.findIndex(s=>s.num==h):0);'''
BAR='''<div class="bar"><h1>Lobsy · kreeft · %s</h1><div class="meta"><div class="ch" id="ch"></div><b id="t"></b><p id="c"></p></div>
<select id="sel" aria-label="Kies scherm"></select><button id="prev">← Vorige</button><button id="next" class="p">Volgende →</button><span class="hint">pijltjestoetsen</span></div>'''
d=f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><title>Lobsy kreeft · walkthrough desktop</title><style>{CSS}
#scroll{{height:calc(100vh - 74px);overflow:auto;padding:22px}}#img{{display:block;width:100%;max-width:1440px;margin:0 auto;box-shadow:0 10px 40px rgba(15,45,92,.14);border-radius:6px;background:#fff}}</style></head>
<body>{BAR%'desktop 1440'}<div id="scroll"><img id="img" alt=""></div><script>{JS%(json.dumps(items,ensure_ascii=False),'d','d')}</script></body></html>'''
open('walkthrough-desktop.html','w').write(d)
m=f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><title>Lobsy kreeft · walkthrough mobiel</title><style>{CSS}
.stage{{display:flex;justify-content:center;padding:22px 0;height:calc(100vh - 74px)}}
.phone{{width:414px;height:100%;max-height:880px;background:#1b2233;border-radius:46px;padding:12px;box-shadow:0 20px 60px rgba(15,45,92,.25);position:relative}}
.phone::before{{content:"";position:absolute;top:20px;left:50%;transform:translateX(-50%);width:110px;height:26px;background:#1b2233;border-radius:0 0 16px 16px;z-index:2}}
#scroll{{width:390px;height:100%;overflow-y:auto;overflow-x:hidden;border-radius:36px;background:#fff;scrollbar-width:thin}}#img{{display:block;width:390px}}</style></head>
<body>{BAR%'mobiel 390'}<div class="stage"><div class="phone"><div id="scroll"><img id="img" alt=""></div></div></div><script>{JS%(json.dumps(items,ensure_ascii=False),'m','m')}</script></body></html>'''
open('walkthrough-mobiel.html','w').write(m)

# ---------- pdf print-html ----------
T='/tmp/kreeft-pdf/'; os.makedirs(T,exist_ok=True)
PC='''@font-face{font-family:F;src:url(file://@R@src/Fraunces.ttf)}@font-face{font-family:N;src:url(file://@R@src/NunitoSans.ttf)}
@page{size:@SZ@;margin:0}*{box-sizing:border-box}body{margin:0;font-family:N,sans-serif;color:#0f2d5c}
.pg{width:@W@;height:@H@;page-break-after:always;overflow:hidden;padding:@P@;display:flex;flex-direction:column;background:#fcf8f2}
.hd{display:flex;gap:12px;align-items:baseline;margin-bottom:3mm}.hd .n{font-family:F,serif;font-size:15pt;color:#df5530}.hd b{font-family:F,serif;font-size:13pt}
.hd .ch{font-size:8pt;font-weight:800;text-transform:uppercase;letter-spacing:.05em;color:#df5530}.hd p{margin:0;font-size:8.5pt;color:#4a5670;flex:1;text-align:right}
.bd{flex:1;min-height:0;display:flex;justify-content:center;align-items:flex-start;gap:6mm}
.bd img{max-width:100%;max-height:100%;object-fit:contain;box-shadow:0 0 0 .3mm #e3ddd4;background:#fff}
.col{height:100%;display:flex;flex-direction:column;align-items:center}.col img{width:86mm;max-height:none}.col small{font-size:7pt;color:#7a8296;margin-top:1.5mm}
.cover{justify-content:center;align-items:center;text-align:center}.cover h1{font-family:F,serif;font-size:30pt;margin:0 0 4mm}.cover p{font-size:11pt;color:#4a5670;max-width:200mm}'''.replace('@R@',R)
def head(s): return f'''<div class="hd"><span class="n">{s['num']:02d}</span><div><div class="ch">{html.escape(s['chapter'])}</div><b>{html.escape(s['title'])}</b></div><p>{html.escape(s['caption'])}</p></div>'''
chapters=[]
for s in S:
    if not chapters or chapters[-1][0]!=s['chapter']: chapters.append((s['chapter'],[]))
    chapters[-1][1].append(s)
json.dump([[c,[x['num'] for x in l]] for c,l in chapters],open(T+'chapters.json','w'))
def jpg(src,dst,w=None,q=78,box=None):
    im=Image.open(src).convert('RGB')
    if box: im=im.crop(box)
    if w and im.width>w: im=im.resize((w,round(im.height*w/im.width)),Image.LANCZOS)
    im.save(dst,quality=q,optimize=True,progressive=True); return dst
def cover(title,sub): return f'<div class="pg cover"><h1>{title}</h1><p>{sub}</p></div>'
def build(kind,nums,part=''):
    sel=[s for s in S if s['num'] in nums]
    if kind=='d':
        css=PC.replace('@SZ@','A4 landscape').replace('@W@','297mm').replace('@H@','210mm').replace('@P@','9mm 11mm')
        body=cover('Lobsy · de kreeft loopt met je mee'+part,'Kandidaatreis desktop (1440 breed), één scherm per pagina, in de volgorde van de reis. Alle gegevens zijn fictief.')
        for s in sel:
            f=jpg(rp(s,0),T+f"d{s['num']:02d}.jpg",q=80)
            body+=f'<div class="pg">{head(s)}<div class="bd"><img src="file://{f}"></div></div>'
    else:
        css=PC.replace('@SZ@','A4 portrait').replace('@W@','210mm').replace('@H@','297mm').replace('@P@','9mm 10mm')
        body=cover('Lobsy · de kreeft loopt met je mee'+part,'Kandidaatreis mobiel (390 breed). Lange schermen lopen door in kolommen van links naar rechts; elk scherm begint op een nieuwe pagina. Alle gegevens zijn fictief.')
        SEG=2290; OV=60  # bronpixels (2x) per kolom, met kleine overlap
        for s in sel:
            im=Image.open(rp(s,1)); H=im.height; segs=[]; y=0
            while True:
                segs.append((y,min(H,y+SEG)))
                if y+SEG>=H: break
                y+=SEG-OV
            # laatste stukje te klein: samenvoegen
            if len(segs)>1 and segs[-1][1]-segs[-1][0]<300: a=segs.pop(); segs[-1]=(segs[-1][0],a[1])
            pages=[segs[k:k+2] for k in range(0,len(segs),2)]
            for pi,pg in enumerate(pages):
                cols=''
                for ci,(a,b) in enumerate(pg):
                    k=pi*2+ci; f=jpg(rp(s,1),T+f"m{s['num']:02d}-{k}.jpg",w=600,q=74,box=(0,a,im.width,b))
                    lab=f'deel {k+1} van {len(segs)}' if len(segs)>1 else ''
                    cols+=f'<div class="col"><img src="file://{f}"><small>{lab}</small></div>'
                hd=head(s) if pi==0 else f'<div class="hd"><span class="n">{s["num"]:02d}</span><div><b>{html.escape(s["title"])}</b></div><p>vervolg</p></div>'
                body+=f'<div class="pg">{hd}<div class="bd">{cols}</div></div>'
    out=T+f'{kind}{part.strip().replace(" ","")}.html'
    open(out,'w').write(f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><style>{css}</style></head><body>{body}</body></html>'); return out
import sys
if len(sys.argv)>2:
    kind=sys.argv[1]; groups=json.loads(sys.argv[2])
    for gi,g in enumerate(groups): print(build(kind,g,f' · deel {gi+1}' if len(groups)>1 else ''))
else:
    allnums=[s['num'] for s in S]; print(build('d',allnums)); print(build('m',allnums))
