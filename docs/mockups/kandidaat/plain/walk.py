import json,glob
from PIL import Image,ImageDraw,ImageFont
S=json.load(open('src/screens.json'))
for d,lab,w,h in [('desktop','Desktop 1440',1440,900),('mobiel','Mobiel 390',390,844)]:
  L=[s for s in S if d=='mobiel' or not s.get('mobileOnly')]
  items=json.dumps([{"t":f"{str(s['num']).zfill(2)}. {s['title']}","f":"src/"+s['file']} for s in L],ensure_ascii=False)
  open(f'walkthrough-{d}.html','w').write(f'''<!doctype html><meta charset=utf-8><title>Lobsy onboarding plain · {lab}</title><style>body{{margin:0;font-family:Inter,sans-serif;background:#E5E7EB}}nav{{position:sticky;top:0;background:#111827;color:#fff;display:flex;gap:12px;align-items:center;padding:10px 16px;flex-wrap:wrap}}button,select{{font-size:16px;padding:8px 14px;border-radius:6px;border:0}}iframe{{display:block;margin:20px auto;width:{w}px;height:{h}px;border:0;background:#fff;box-shadow:0 2px 12px rgba(0,0,0,.15){';border-radius:28px;border:10px solid #111827' if d=='mobiel' else ''}}}</style><nav><b>Onboarding plain · {lab}</b><button id=p>Vorige</button><select id=s></select><button id=n>Volgende</button><span id=k></span><span style="opacity:.75">Tip: klik in het scherm op Voorlezen</span></nav><iframe id=c title="Scherm"></iframe><script>const I={items};let i=Math.max(0,(parseInt(location.hash.slice(1))||1)-1);const s=document.getElementById('s');I.forEach((x,j)=>s.add(new Option(x.t,j)));function g(j){{try{{speechSynthesis.cancel()}}catch(e){{}}i=Math.min(I.length-1,Math.max(0,j));c.src=I[i].f;s.value=i;k.textContent=(i+1)+' / '+I.length;location.hash=i+1}}p.onclick=()=>g(i-1);n.onclick=()=>g(i+1);s.onchange=()=>g(+s.value);onkeydown=e=>{{if(e.key=='ArrowRight')g(i+1);if(e.key=='ArrowLeft')g(i-1)}};g(i)</script>''')
for d in ['desktop','mobiel']:
  ims=[Image.open(f).convert('RGB') for f in sorted(glob.glob(f'renders/{d}/*.png'))]
  ims[0].save(f'onboarding-plain-{d}.pdf',save_all=True,append_images=ims[1:],resolution=150);print(d,len(ims))
fp=[f for f in glob.glob('/usr/share/fonts/**/*.ttf',recursive=True) if 'Inter' in f and 'Bold' in f] or glob.glob('/usr/share/fonts/**/*.ttf',recursive=True)
def cmp(a,b,out,l1,l2,gap,fs):
  a=Image.open(a).convert('RGB');b=Image.open(b).convert('RGB')
  H=max(a.height,b.height);o=Image.new('RGB',(a.width+b.width+gap,H+fs*2+10),'white');o.paste(a,(0,fs*2+10));o.paste(b,(a.width+gap,fs*2+10))
  dr=ImageDraw.Draw(o);f=ImageFont.truetype(fp[0],fs);dr.text((10,fs//2),l1,fill='black',font=f);dr.text((a.width+gap+10,fs//2),l2,fill='black',font=f);o.save(out,quality=85)
cmp('/workspace/kreeft-kandidaat-mockup/renders/desktop/04-kust-kennismaken-desktop-1440.png','renders/desktop/04-over-jou-kennismaken-desktop-1440.png','vergelijking-over-jou.jpg',"Nu: geïllustreerd (Kust · stap 1)","Plain met gids (Over jou · stap 1)",60,40)
cmp('/workspace/kreeft-kandidaat-mockup/renders/mobile/04-kust-kennismaken-mobiel-390.png','renders/mobiel/04-over-jou-kennismaken-mobiel-390.png','vergelijking-mobiel.jpg',"Nu: geïllustreerd","Plain, gids ingeklapt",80,44)
a=Image.open('renders/desktop/12-test1-v3-desktop-1440.png').convert('RGB');b=Image.open('renders/mobiel/12-test1-v3-mobiel-390.png').convert('RGB')
b=b.resize((int(b.width*a.height/b.height*1.0),a.height)) if b.height>a.height else b
cmp_a='/tmp/a.png';a.save(cmp_a);b.save('/tmp/b.png');cmp(cmp_a,'/tmp/b.png','vergelijking-testvraag.jpg',"Desktop 1440 · test 1, vraag 3","Mobiel 390",60,40)
