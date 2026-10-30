import os
OUT=os.path.dirname(os.path.abspath(__file__))+'/src/'
SPK='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 5L6 9H2v6h4l5 4V5z"/><path d="M15.5 8.5a5 5 0 010 7"/><path d="M19 5a10 10 0 010 14"/></svg>'
PAU='<svg viewBox="0 0 24 24" fill="currentColor"><rect x="6" y="5" width="4" height="14" rx="1"/><rect x="14" y="5" width="4" height="14" rx="1"/></svg>'
BACK='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M15 18l-6-6 6-6"/></svg>'
NEXT='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12h14M13 6l6 6-6 6"/></svg>'
CHK='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M7 12.5l3.5 3.5L17 9"/></svg>'
PZ='<svg viewBox="0 0 24 24" fill="currentColor"><path d="M10 3a2 2 0 014 0v2h4a1 1 0 011 1v4h-2a2 2 0 000 4h2v4a1 1 0 01-1 1h-4v-2a2 2 0 00-4 0v2H6a1 1 0 01-1-1v-4h2a2 2 0 000-4H5V6a1 1 0 011-1h4V3z"/></svg>'
def page(name,body,title='Lobsy expeditie'):
    open(OUT+name+'.html','w').write(f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{title}</title><link rel="stylesheet" href="style.css"></head><body>{body}</body></html>''')
def top(code=True):
    r='<header class="top"><div class="brand"><img src="img/embleem.png" alt="">Lobsy<small>Expeditie</small></div><div class="sp"></div>'
    if code: r+=f'<span class="chip-code">8B · K7R-2QM</span><button class="btn-pauze">{PAU}<span>Pauze</span></button>'
    return r+'</header>'
FOOT='<footer><a>Privacyverklaring</a><a>Toegankelijkheid</a></footer>'
NODES=[('w','Koraalrif','rif'),('p','Schelpenrij',''),('w','Schatgrot','grot'),('p','Schatkaart',''),('w','Pauze-eiland','eiland'),('w','Vuurtoren','vuurtoren'),('p','Lampen',''),('w','Lagune','lagune'),('w','Dit ben jij','haven')]
def journey(cur,meta=None,seg=None):
    n=len(NODES); h=''
    for i,(t,lab,img) in enumerate(NODES):
        st='done' if i<cur else ('cur' if i==cur else '')
        cls='jn'+(' p' if t=='p' else '')+(' '+st if st else '')
        here='<span class="here">Hier ben je</span>' if i==cur else ''
        if t=='p': dot=f'<span class="dot">{PZ}</span>'
        elif lab=='Dit ben jij': dot='<span class="dot" style="background:#7a5a1e url(img/zeekaart.jpg) 84% 22%/900% no-repeat"></span>'
        else: dot=f'<span class="dot" style="background-image:url(img/{img}.jpg)"></span>'
        h+=f'<div class="{cls}">{here}{dot}<b>{lab}</b></div>'
    pct=f'calc((100% - 52px) * {cur/(n-1):.4f})'
    m=''
    if meta:
        s=''
        if seg:
            done,tot=seg; s='<div class="seg">'+''.join('<i class="f"></i>' if k<done else ('<i class="c"></i>' if k==done else '<i></i>') for k in range(tot))+'</div>'
        m=f'<div class="jmeta"><span>{meta}</span>{s}</div>'
    return f'<div class="journey"><div class="jrow"><span class="jdone" style="width:{pct}"></span>{h}</div>{m}</div>'
def band(img,h,pos='center',hm=None,dark=False,inner=''):
    return f'<section class="band{" dark" if dark else ""}" style="background-image:url(img/{img}.jpg);background-position:{pos};height:{h}px">{inner}</section>'
def bandcss(cls,img,hd,hm,posd,posm):
    return f'<style>.{cls}{{background-image:url(img/{img}.jpg);background-position:{posd};min-height:{hd}px}}@media(max-width:600px){{.{cls}{{min-height:{hm}px;background-position:{posm}}}}}</style>'
def ans(sel=None):
    L=[('Nee',0),('Niet echt',25),('Soms',50),('Best wel',75),('Ja!',100)]
    return '<div class="answers">'+''.join(f'<button class="ans{" sel" if a==sel else ""}"><span class="lvl"><i style="height:{p}%"></i></span>{a}</button>' for a,p in L)+'</div>'
def bubble(txt,tail='b',who='Lobsy',btn=True,on=False):
    b=f'<div class="bt"><button class="speak{" on" if on else ""}" aria-label="Voorlezen">{SPK}</button><span class="speak-label">{"Leest voor..." if on else "Lees voor"}</span></div>' if btn else ''
    return f'<div class="bubble tail-{tail}"><span class="who">{who}</span>{txt}{b}</div>'

def question(name,cur,world,wnum,qn,qtitle,sel=None,img='rif',pos='center 30%',encour=None,lob='kreeft-welkom',gtxt='Kies wat het beste bij je past. Er zijn geen foute antwoorden.',on=False,saved=False):
    wq=qn-(wnum-1)*15
    meta=f'<b>{world}</b> · wereld {wnum} van 4 · vraag {wq} van 15'
    css=bandcss('qb',img,330,210,pos,pos)
    enc=''
    if encour:
        enc=f'<div class="encour"><img src="img/kreeft-schild.png" alt=""><div>{encour}</div><span class="shield" style="margin-left:auto">{''.join('<i class="f"></i>' if k<wnum-1 else '<i></i>' for k in range(4))}</span></div>'
    sv=f'<span class="saved">{CHK[:0]}✓ Bewaard</span>' if saved else ''
    body=f'''{top()}{css}<section class="band qb" style="padding-top:150px"><div class="d-only">{journey(cur,meta,(wq-1,15))}</div><div class="m-only" style="padding-top:0">{journey(cur,meta,(wq-1,15))}</div></section>
<div class="wrap" style="margin-top:-30px;position:relative;z-index:4"><div class="q-layout"><div class="card">{enc}
<div class="qmeta"><span class="kicker">{world}</span><span>·</span><span>Vraag {qn} van 60</span></div>
<h1 class="qtitle">{qtitle}</h1>
<div class="qrow"><button class="speak{" on" if on else ""}">{SPK}</button><span class="speak-label">{"Lobsy leest de vraag voor" if on else "Laat de vraag voorlezen"}</span></div>
{ans(sel)}
<div class="qfoot"><button class="btn btn-ghost">{BACK}Vorige</button>{sv}</div></div>
<aside class="d-only" style="padding-top:10px">{bubble(gtxt,'b',on=False,btn=True)}<img class="lob" src="img/{lob}.png" alt="Lobsy" style="margin-top:22px"></aside>
<div class="m-only"><div class="guide"><div class="ring"></div>{bubble(gtxt,'l')}</div></div>
</div></div>{FOOT}'''
    page(name,body)

# 01 inloggen
page('01-inloggen',f'''{top(False)}{bandcss('lb','oceaan',780,330,'center 60%','38% 50%')}
<section class="band lb dark" style="position:relative"><div class="wrap" style="display:grid;grid-template-columns:1fr 520px;gap:40px;align-items:center;padding-top:50px;padding-bottom:60px" >
<div class="d-only" style="color:#fff;text-shadow:0 2px 14px rgba(0,0,0,.45)"><p class="kicker" style="color:#ffd98a">Groep 7 en 8</p><h1 style="font-size:62px;margin:8px 0 14px">Welkom aan boord.</h1><p style="font-size:22px;max-width:480px">Log in met het kaartje van je leraar. Dan kan de expeditie beginnen.</p>
<div style="display:flex;align-items:flex-end;gap:10px;margin-top:20px"><img src="img/kreeft-welkom.png" style="width:340px;filter:drop-shadow(0 10px 20px rgba(0,0,0,.35))" alt="Lobsy"><div style="color:var(--ink);text-shadow:none;margin-bottom:250px">{bubble('Hoi. Ik ben Lobsy, je gids op deze reis.','l',btn=False)}</div></div></div>
<div class="m-only" style="color:#fff;text-shadow:0 2px 12px rgba(0,0,0,.55)"><p class="kicker" style="color:#ffd98a">Groep 7 en 8</p><h1 style="font-size:38px;margin:6px 0 8px">Welkom aan boord.</h1><p>Log in met het kaartje van je leraar.</p>
<div style="display:flex;align-items:center;gap:8px;margin-top:10px"><img src="img/kreeft-welkom.png" style="width:175px;flex:none" alt="Lobsy"><div style="color:var(--ink);text-shadow:none">{bubble('Hoi. Ik ben Lobsy, je gids.','l',btn=False)}</div></div></div>
<div class="card" style="padding:34px 36px"><div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:18px"><h2 style="font-size:32px">Inloggen</h2><button class="speak">{SPK}</button></div>
<label class="field"><span>Jouw school</span><div class="inp">Basisschool De Golfslag · Demodorp <span>▾</span></div></label>
<label class="field"><span>Jouw klas</span><div class="inp">8B · groep 8 <span>▾</span></div></label>
<label class="field"><span>Jouw code</span><div class="inp" style="border:2px dashed var(--brass);background:#fffaf0;font-family:'Space Grotesk',monospace;font-size:28px;letter-spacing:.18em;justify-content:center;padding:16px">K7R-2QM</div><small style="color:var(--muted);font-size:15px">Staat op je kaartje. Hoofdletters maken niet uit.</small></label>
<p class="note" style="margin:6px 0 22px">Je naam hoeft niet. Lobsy kent alleen je code.</p>
<button class="btn btn-main btn-big" style="width:100%">Aan boord {NEXT}</button></div></div>
<style>@media(max-width:600px){{.lb .wrap{{grid-template-columns:1fr!important;padding-top:24px!important;padding-bottom:30px!important;gap:20px!important}}}}</style></section>{FOOT}''')

# 02 start
page('02-start',f'''{top()}{bandcss('hb','haven',800,860,'center 40%','64% 50%')}
<section class="band hb dark" style="position:relative;overflow:hidden">
<div style="position:absolute;inset:0;background:linear-gradient(90deg,rgba(16,40,74,.72) 0%,rgba(16,40,74,.35) 38%,rgba(16,40,74,0) 60%)" class="d-only"></div>
<div style="position:absolute;inset:0;background:linear-gradient(180deg,rgba(16,40,74,.75) 0%,rgba(16,40,74,.25) 55%,rgba(16,40,74,0) 75%)" class="m-only"></div>
<div class="wrap" style="position:relative;padding-top:70px;color:#fff;text-shadow:0 2px 16px rgba(0,0,0,.4)">
<p class="kicker" style="color:#ffd98a">Expeditie Lobsy · groep 8</p>
<h1 class="st-h" style="font-size:72px;max-width:640px;margin:10px 0 18px">Jouw expeditie begint hier.</h1>
<p class="st-p" style="font-size:23px;max-width:560px">4 werelden, 3 puzzels en 1 eiland om bij te komen. Onderweg ontdek je wie je bent en wat bij je past.</p>
<div class="st-b" style="display:flex;align-items:center;gap:24px;margin-top:34px;flex-wrap:wrap"><button class="btn btn-main btn-big">Start de expeditie {NEXT}</button>
<label style="display:flex;align-items:center;gap:12px;background:rgba(16,40,74,.75);border:1px solid rgba(226,194,122,.6);border-radius:14px;padding:10px 16px;text-shadow:none;font-size:17px"><span style="width:54px;height:30px;border-radius:15px;background:var(--brass);position:relative;display:inline-block"><span style="position:absolute;right:3px;top:3px;width:24px;height:24px;border-radius:50%;background:#fff"></span></span>Voorlezen staat aan</label></div></div>
<img class="d-only" src="img/kreeft-kaart.png" alt="Lobsy" style="position:absolute;left:600px;bottom:20px;width:400px;filter:drop-shadow(0 12px 22px rgba(0,0,0,.45))">
<div class="m-only" style="position:absolute;left:8px;right:10px;bottom:14px"><div class="guide" style="align-items:flex-end"><img src="img/kreeft-kaart.png" style="width:215px;flex:none;margin-right:-30px;filter:drop-shadow(0 8px 16px rgba(0,0,0,.5))" alt="Lobsy"><div style="flex:1;margin-bottom:120px">{bubble('Ik ben je gids. Geen foute antwoorden. Pauzeren mag altijd.','l',on=True)}</div></div></div>
<div class="d-only" style="position:absolute;left:960px;bottom:380px;width:300px">{bubble('Ik ben je gids. Er zijn geen foute antwoorden en je kunt altijd pauzeren.','l',on=True)}</div>
</section>

<div class="wrap" style="padding-top:36px"><div class="card st-route" style="display:grid;grid-template-columns:1.25fr 1fr;gap:34px;align-items:center">
<div style="border-radius:16px;overflow:hidden;border:3px solid var(--sand)"><img src="img/zeekaart.jpg" style="width:100%" alt="Kaart van de reis"></div>
<div><p class="kicker">Zo ziet je reis eruit</p><h2 style="font-size:34px;margin:6px 0 14px">Van de haven naar jouw eiland</h2>
<ul style="list-style:none;display:grid;gap:10px;font-size:18px"><li><span style="width:10px;height:10px;border-radius:50%;background:var(--brass);display:inline-block;margin-right:10px"></span>Ongeveer 30 minuten</li><li><span style="width:10px;height:10px;border-radius:50%;background:var(--brass);display:inline-block;margin-right:10px"></span>Geen foute antwoorden</li><li><span style="width:10px;height:10px;border-radius:50%;background:var(--brass);display:inline-block;margin-right:10px"></span>Stoppen mag, later ga je verder</li></ul>
<button class="btn btn-ghost" style="margin-top:20px">Bekijk de hele route</button></div></div></div>
<style>@media(max-width:600px){{.st-h{{font-size:40px!important}}.st-p{{font-size:19px!important}}.hb .wrap{{padding-top:30px!important}}.st-b{{margin-top:22px!important}}.st-b .btn{{width:100%}}.st-route{{grid-template-columns:1fr!important;gap:18px!important}}}}</style>{FOOT}''')

# 03 reiskaart
PINS=[(160,430,'Haven','Start','s'),(300,345,'Koraalrif','Vraag 1 tot 15','w'),(352,262,'Schelpenrij','Puzzel 1','p'),(425,222,'Schatgrot','Vraag 16 tot 30','w'),(505,300,'Schatkaart','Puzzel 2','p'),(578,262,'Pauze-eiland','Wat vind jij leuk?','i'),(718,192,'Vuurtoren','Vraag 31 tot 45','w'),(770,262,'Vuurtorenlampen','Puzzel 3','p'),(812,345,'Lagune','Vraag 46 tot 60','w'),(855,126,'Dit ben jij','Jouw eiland','f')]
path='M'+' '.join(f'{x},{y}' for x,y,*_ in PINS)
def pts():
    d='M160,430 C220,400 260,370 300,345 S330,280 352,262 S400,225 425,222 S480,270 505,300 S560,270 578,262 S680,200 718,192 S760,235 770,262 S800,320 812,345 S900,300 905,230 S880,150 855,126'
    s=f'<path d="{d}" fill="none" stroke="#fffdf8" stroke-width="9" stroke-linecap="round" opacity=".85"/><path d="{d}" fill="none" stroke="#d4532f" stroke-width="4.5" stroke-dasharray="2 11" stroke-linecap="round"/>'
    for i,(x,y,l,sub,t) in enumerate(PINS):
        col={'w':'#10284a','p':'#b98f3e','i':'#0f5b66','s':'#2f7d5b','f':'#d4532f'}[t]
        if t=='p': s+=f'<rect x="{x-14}" y="{y-14}" width="28" height="28" rx="5" transform="rotate(45 {x} {y})" fill="{col}" stroke="#fffdf8" stroke-width="3"/>'
        else: s+=f'<circle cx="{x}" cy="{y}" r="17" fill="{col}" stroke="#fffdf8" stroke-width="3"/>'
        s+=f'<text x="{x}" y="{y+6}" text-anchor="middle" font-family="Lexend" font-weight="700" font-size="17" fill="#fff">{i+1}</text>'
    s+='<g transform="translate(160,430)"><circle r="27" fill="none" stroke="#ffd98a" stroke-width="4"/></g>'
    return f'<svg viewBox="0 0 1024 576" style="position:absolute;inset:0;width:100%;height:100%">{s}</svg>'
li=''
for i,(x,y,l,sub,t) in enumerate(PINS):
    col={'w':'var(--navy)','p':'var(--brass)','i':'var(--teal)','s':'var(--ok)','f':'var(--coral)'}[t]
    shape='border-radius:7px;transform:rotate(45deg) scale(.82)' if t=='p' else 'border-radius:50%'
    num=f'<span style="display:inline-block;transform:rotate(-45deg)">{i+1}</span>' if t=='p' else str(i+1)
    here=' <span style="background:var(--coral);color:#fff;font-size:12px;border-radius:6px;padding:1px 7px;margin-left:6px">Hier ben je</span>' if i==0 else ''
    li+=f'<li style="display:flex;align-items:center;gap:12px"><span style="width:32px;height:32px;{shape};background:{col};color:#fff;font-weight:700;font-size:15px;display:flex;align-items:center;justify-content:center;flex:none">{num}</span><span><b>{l}</b>{here}<br><span style="color:var(--muted);font-size:15px">{sub}</span></span></li>'
page('03-reiskaart',f'''{top()}{bandcss('kb','oceaan',230,150,'center 45%','center 45%')}
<section class="band kb"><div class="wrap" style="padding-top:44px"><h1 style="font-size:46px;color:#fff;text-shadow:0 2px 14px rgba(0,0,0,.5)" class="rk-h">De route</h1><p style="color:#fff;text-shadow:0 2px 10px rgba(0,0,0,.6);font-size:20px">Zo vaar je van de haven naar jouw eiland.</p></div></section>
<div class="wrap" style="margin-top:-20px;position:relative;z-index:3"><div class="card rk" style="display:grid;grid-template-columns:1fr 300px;gap:30px;padding:26px">
<div><div style="position:relative;border-radius:16px;overflow:hidden;aspect-ratio:16/9"><img src="img/zeekaart.jpg" style="width:100%;height:100%" alt="Zeekaart">{pts()}</div>
<div class="d-only" style="display:flex;align-items:center;gap:16px;margin-top:20px"><div class="ring"></div>{bubble('Elke 15 vragen wacht er een korte puzzel. Halverwege rust je uit op het eiland.','l')}</div></div>
<div><ol style="list-style:none;display:grid;gap:12px;font-size:17px">{li}</ol><button class="btn btn-main" style="width:100%;margin-top:22px">Naar het Koraalrif {NEXT}</button></div></div>
<div class="m-only" style="margin-top:18px"><div class="guide"><div class="ring"></div>{bubble('Elke 15 vragen wacht een korte puzzel. Halverwege rust je uit op het eiland.','l')}</div></div></div>
<style>@media(max-width:600px){{.rk{{grid-template-columns:1fr!important;padding:12px!important;gap:18px!important}}.rk-h{{font-size:34px!important}}.kb .wrap{{padding-top:24px!important}}}}</style>{FOOT}''')

question('04-vraag-1-koraalrif',0,'Het Koraalrif',1,1,'Als een vriend verdrietig is, merk ik dat snel.',on=True,gtxt='Ik lees elke vraag voor. Tik op de luidspreker om het nog eens te horen.')
question('05-vraag-14-aanmoediging',0,'Het Koraalrif',1,14,'Ik maak een plan voordat ik aan een klus begin.',sel='Best wel',saved=True,lob='kreeft-schild',encour='<b>Sterk bezig.</b> Nog 1 vraag, dan is het Koraalrif klaar en heb je het eerste deel van het schild.',gtxt='Bijna bij de eerste puzzel. Kijk goed, dan zie je het patroon.')

# puzzles
S=lambda k:f'<img src="img/schelp-{k}.png" alt="">'
seq=[1,2,2,3,1,2,2,3,1]
def schelp(name,solved):
    slots=''.join(f'<div class="slot">{S(k)}</div>' for k in seq)
    slots+=(f'<div class="slot solved">{S(2)}</div>' if solved else '<div class="slot q">?</div>')
    names=['Sint-jakobsschelp','Stekelhoorn','Kauri','Streepschelp']
    opts=''.join(f'<button class="opt{" sel" if solved and k==2 else (" dim" if solved else "")}"><span class="l">{"ABCD"[k-1]}</span>{S(k)}{names[k-1]}</button>' for k in [1,2,3,4])
    css=bandcss('pb','rif',300,190,'center 75%','center 75%')
    expl=f'<div class="explain">{CHK}<div><b>Goed gezien.</b> Het patroon is: rood, 2 keer blauw, goud. Daarna begint het opnieuw. Dus komt er een blauwe stekelhoorn.</div></div>' if solved else ''
    btns=f'<button class="btn btn-main btn-big">Door naar de Schatgrot {NEXT}</button>' if solved else f'<button class="btn btn-quiet">Weet ik niet, ga verder</button><button class="btn btn-main" style="margin-left:auto">Kies een schelp</button>'
    g=('Netjes. Puzzel 1 is klaar. Op naar de Schatgrot.','kreeft-schild') if solved else ('Neem je tijd. Er loopt geen klok.','kreeft-kaart')
    page(name,f'''{top()}{css}<section class="band pb" style="padding-top:130px">{journey(1,'<b>Puzzel 1 van 3</b> · Schelpenrij')}</section>
<div class="wrap" style="margin-top:-24px;position:relative;z-index:4"><div class="q-layout"><div class="card">
<div class="qmeta"><span class="kicker">Puzzel · Schelpenrij</span></div><h1 class="qtitle">Welke schelp hoort op de plek van het vraagteken?</h1>
<div class="qrow"><button class="speak">{SPK}</button><span class="speak-label">Laat de opdracht voorlezen</span></div>
<div class="pz-row">{slots}</div><div class="opts">{opts}</div>{expl}
<div class="qfoot" style="flex-wrap:wrap">{btns}</div></div>
<aside class="d-only">{bubble(g[0],'b')}<img class="lob" src="img/{g[1]}.png" style="margin-top:22px" alt=""></aside>
<div class="m-only"><div class="guide"><div class="ring"></div>{bubble(g[0],'l')}</div></div></div></div>{FOOT}''')
schelp('06-puzzel-schelpenrij',False)
schelp('07-puzzel-schelpenrij-opgelost',True)

# 08 nieuwe wereld schatgrot
page('08-nieuwe-wereld-schatgrot',f'''{top()}{bandcss('gb','grot',820,640,'center 40%','45% 50%')}
<section class="band gb dark" style="position:relative;overflow:hidden;padding-top:36px">{journey(2,'<b>De Schatgrot</b> · wereld 2 van 4 · vraag 16 tot 30')}
<div class="wrap nw" style="display:grid;grid-template-columns:1fr 360px;gap:20px;align-items:end;margin-top:60px">
<div style="background:rgba(16,40,74,.82);border:1px solid rgba(226,194,122,.55);border-radius:22px;padding:36px 40px;color:#fff;max-width:640px;margin-bottom:60px">
<p class="kicker" style="color:#ffd98a">Wereld 2 van 4</p><h1 style="font-size:60px;margin:6px 0 12px" class="nw-h">De Schatgrot</h1>
<p style="font-size:21px">Hier gaat het over wat jij graag doet. 15 vragen, daarna wacht de schatkaart.</p>
<div style="display:flex;align-items:center;gap:16px;margin:22px 0 26px;background:rgba(255,255,255,.08);border-radius:14px;padding:12px 16px"><span class="shield"><i class="f"></i><i></i><i></i><i></i></span><span>Je hebt het <b style="color:#ffd98a">eerste deel van het schild</b>. Nog 3 te gaan.</span></div>
<div style="display:flex;gap:14px;align-items:center;flex-wrap:wrap"><button class="btn btn-main btn-big">Ga de grot in {NEXT}</button><button class="speak" style="border-color:#ffd98a;color:#ffd98a;background:transparent">{SPK}</button></div></div>
<img class="d-only" src="img/kreeft-kijker.png" style="width:340px;margin-bottom:30px;filter:drop-shadow(0 10px 24px rgba(0,0,0,.6))" alt="Lobsy"></div></section>
<style>@media(max-width:600px){{.nw{{grid-template-columns:1fr!important;margin-top:150px!important}}.nw>div{{padding:24px 20px!important;margin-bottom:24px!important}}.nw-h{{font-size:40px!important}}.nw .btn{{width:auto}}}}</style>
<div class="m-only" style="padding:20px 14px 0"><div class="guide" style="align-items:center"><img src="img/kreeft-kijker.png" style="width:130px;flex:none" alt="">{bubble('Kijk eens wat hier ligt. Benieuwd wat jij graag doet.','l')}</div></div>{FOOT}''')

# 09 schatkaart
cols='ABCDEF'
grid=''
for r in range(1,7):
    for c in range(6):
        cell=f'{cols[c]}{r}'; inner=''
        if cell=='A6': inner='<span style="font-size:30px">⚓</span>'
        if cell in ('C2','D2','C3','B2'): inner=f'<span style="font-family:Fraunces;font-weight:700;font-size:26px;color:#7a3b2a">✕</span><small style="position:absolute;bottom:2px;right:5px;font-size:12px;color:#6b5a3a">{cell}</small>'
        grid+=f'<div style="position:relative;border:1px solid rgba(107,90,58,.35);display:flex;align-items:center;justify-content:center">{inner}</div>'
labels_top=''.join(f'<div style="text-align:center;font-weight:600;color:#6b5a3a">{c}</div>' for c in cols)
labels_side=''.join(f'<div style="display:flex;align-items:center;font-weight:600;color:#6b5a3a">{r}</div>' for r in range(1,7))
steps=[('➡','3 vakjes naar rechts'),('⬆','2 vakjes omhoog'),('⬅','1 vakje naar links'),('⬆','2 vakjes omhoog')]
stl=''.join(f'<li style="display:flex;gap:12px;align-items:center;background:#fff;border:2px solid #d4c6aa;border-radius:12px;padding:10px 14px"><span style="width:30px;height:30px;border-radius:50%;background:var(--navy);color:#fff;display:flex;align-items:center;justify-content:center;font-size:14px;font-weight:700">{i+1}</span><span style="font-size:22px">{a}</span>{t}</li>' for i,(a,t) in enumerate(steps))
optsk=''.join(f'<button class="opt" style="font-size:22px;padding:16px"><span class="l">{"ABCD"[i]}</span>Vak {v}</button>' for i,v in enumerate(['C2','D2','C3','B2']))
page('09-puzzel-schatkaart',f'''{top()}{bandcss('sb','grot',300,190,'center 85%','center 85%')}<section class="band sb" style="padding-top:130px">{journey(3,'<b>Puzzel 2 van 3</b> · Schatkaart')}</section>
<div class="wrap" style="margin-top:-24px;position:relative;z-index:4"><div class="q-layout"><div class="card">
<div class="qmeta"><span class="kicker">Puzzel · Schatkaart</span></div><h1 class="qtitle">Je start bij het anker. Volg de stappen. Op welk vak ligt de schat?</h1>
<div class="qrow"><button class="speak">{SPK}</button><span class="speak-label">Laat de opdracht voorlezen</span></div>
<div class="sk" style="display:grid;grid-template-columns:1fr 1fr;gap:26px;align-items:start">
<div style="background:url(img/zeekaart.jpg) 50% 50%/cover;border-radius:16px;padding:14px;border:3px solid #c8b48a"><div style="background:rgba(244,232,206,.88);border-radius:10px;padding:10px;display:grid;grid-template-columns:24px 1fr;grid-template-rows:24px 1fr;gap:4px">
<div></div><div style="display:grid;grid-template-columns:repeat(6,1fr)">{labels_top}</div><div style="display:grid;grid-template-rows:repeat(6,1fr)">{labels_side}</div>
<div style="display:grid;grid-template-columns:repeat(6,1fr);grid-template-rows:repeat(6,1fr);aspect-ratio:1">{grid}</div></div></div>
<div><p style="font-weight:600;margin-bottom:10px">De stappen</p><ol style="list-style:none;display:grid;gap:10px">{stl}</ol></div></div>
<div class="opts">{optsk}</div>
<div class="qfoot"><button class="btn btn-quiet">Weet ik niet, ga verder</button><button class="btn btn-main" style="margin-left:auto">Kies een vak</button></div></div>
<aside class="d-only">{bubble('Tip: zet je vinger op het anker en loop mee.','b')}<img class="lob" src="img/kreeft-kaart.png" style="margin-top:22px" alt=""></aside>
<div class="m-only"><div class="guide"><div class="ring"></div>{bubble('Tip: zet je vinger op het anker en loop mee.','l')}</div></div></div></div>
<style>@media(max-width:600px){{.sk{{grid-template-columns:1fr!important}}}}</style>{FOOT}''')

# 10 pauze-eiland
likes=['Sport','Buiten zijn','Dieren','Gamen','Tekenen','Muziek','Koken of bakken','Fietsen repareren','Bouwen en knutselen','Techniek','Lezen','Dansen','Theater','Filmpjes maken','Mode','Kleine kinderen','Natuur','Auto\'s en motoren','Computers','Puzzels','Rekenen','Talen','Reizen','Programmeren']
dis=likes+['Voor de klas praten','Lang stilzitten','Hard werken in de kou','Veel lezen','Alleen werken','Druk en lawaai','Vies worden']
L=lambda arr,on,cls:''.join(f'<span class="chip{(" "+cls) if x in on else ""}">{x}</span>' for x in arr)
page('10-pauze-eiland',f'''{top()}{bandcss('eb','eiland',420,230,'center 65%','28% 60%')}<section class="band eb" style="padding-top:36px">{journey(4,'<b>Pauze-eiland</b> · 30 van 60 vragen klaar · pauze tussen wereld 2 en 3')}</section>
<div class="wrap" style="margin-top:-40px;position:relative;z-index:4"><div class="q-layout"><div class="card">
<div class="qmeta"><span class="kicker">Pauze-eiland</span></div><h1 class="qtitle">Even uitrusten. Waar word jij blij van?</h1>
<div class="qrow"><button class="speak">{SPK}</button><span class="speak-label">Laat voorlezen</span></div>
<h2 style="font-family:Lexend;font-size:21px;margin:6px 0 12px">Wat vind jij leuk? <span style="font-weight:400;color:var(--muted);font-size:16px">Kies er zoveel je wilt</span></h2>
<div class="chips">{L(likes,['Dieren','Lezen','Buiten zijn','Natuur'],'on')}<span class="chip on">Duiken</span><span class="chip add">+ Iets anders</span></div>
<h2 style="font-family:Lexend;font-size:21px;margin:28px 0 12px">Wat vind je niet leuk?</h2>
<div class="chips">{L(dis,['Druk en lawaai','Lang stilzitten'],'no')}</div>
<label class="field" style="margin-top:16px"><span>Iets anders dat je niet leuk vindt</span><div class="inp" style="color:#8a8f99">Typ één woord</div></label>
<p class="warn">Typ geen namen, ook niet je eigen naam. En geen adres of telefoonnummer.</p>
<div class="qfoot"><button class="btn btn-ghost">{BACK}Vorige</button><button class="btn btn-main" style="margin-left:auto">Klaar, verder {NEXT}</button></div></div>
<aside class="d-only">{bubble('Halverwege. Dit is jouw moment om even bij te komen.','b')}<img class="lob" src="img/kreeft-welkom.png" style="margin-top:22px" alt=""></aside>
<div class="m-only"><div class="guide"><div class="ring"></div>{bubble('Halverwege. Even bijkomen.','l')}</div></div></div></div>{FOOT}''')

question('11-vraag-31-vuurtoren',5,'De Vuurtoren',3,31,'Ik vind het belangrijk dat iedereen eerlijk behandeld wordt.',sel='Ja!',saved=True,img='vuurtoren',pos='center 40%',lob='kreeft-kijker',gtxt='Wereld 3. Hier gaat het over wat jij belangrijk vindt.')

# 12 lampen
def lamps(pat,small=False):
    s=''.join(f'<span style="width:{30 if small else 46}px;height:{30 if small else 46}px;border-radius:50%;border:3px solid #b98f3e;background:{"radial-gradient(circle,#fff6c8 0%,#ffcf5a 45%,#e28a1d 100%);box-shadow:0 0 18px #ffcf5a" if p else "#1b3a63"}"></span>' for p in pat)
    return f'<div style="display:flex;gap:{8 if small else 14}px">{s}</div>'
rows=[[1,1,0,0],[0,1,1,0],[0,0,1,1]]
rh=''.join(f'<div style="display:flex;align-items:center;gap:18px"><span style="color:#ffd98a;font-weight:600;width:80px">Nacht {i+1}</span>{lamps(r)}</div>' for i,r in enumerate(rows))
rh+='<div style="display:flex;align-items:center;gap:18px"><span style="color:#ffd98a;font-weight:600;width:80px">Nacht 4</span><div style="display:flex;gap:14px">'+''.join('<span style="width:46px;height:46px;border-radius:50%;border:3px dashed #ffd98a;display:flex;align-items:center;justify-content:center;color:#ffd98a;font-family:Fraunces;font-weight:700;font-size:24px">?</span>' for _ in range(4))+'</div></div>'
lo=[[1,0,0,1],[1,1,0,0],[0,0,1,1],[0,1,0,1]]
lopts=''.join(f'<button class="opt" style="background:#10284a;border-color:#10284a;color:#fff"><span class="l" style="background:#b98f3e">{"ABCD"[i]}</span>{lamps(p,True)}</button>' for i,p in enumerate(lo))
page('12-puzzel-vuurtorenlampen',f'''{top()}{bandcss('vb','vuurtoren',300,190,'center 30%','70% 30%')}<section class="band vb" style="padding-top:130px">{journey(6,'<b>Puzzel 3 van 3</b> · Vuurtorenlampen')}</section>
<div class="wrap" style="margin-top:-24px;position:relative;z-index:4"><div class="q-layout"><div class="card">
<div class="qmeta"><span class="kicker">Puzzel · Vuurtorenlampen</span></div><h1 class="qtitle">Elke nacht gaan er andere lampen aan. Welke lampen branden in nacht 4?</h1>
<div class="qrow"><button class="speak">{SPK}</button><span class="speak-label">Laat de opdracht voorlezen</span></div>
<div class="lp" style="background:linear-gradient(#0d1f3a,#1b3a63);border-radius:18px;padding:24px 26px;display:grid;gap:16px">{rh}</div>
<div class="opts">{lopts}</div>
<div class="qfoot"><button class="btn btn-quiet">Weet ik niet, ga verder</button><button class="btn btn-main" style="margin-left:auto">Kies een rij</button></div></div>
<aside class="d-only">{bubble('Let op wat er elke nacht verschuift.','b')}<img class="lob" src="img/kreeft-kijker.png" style="margin-top:22px" alt=""></aside>
<div class="m-only"><div class="guide"><div class="ring"></div>{bubble('Let op wat er elke nacht verschuift.','l')}</div></div></div></div>
<style>@media(max-width:600px){{.lp{{padding:16px 12px!important}}.lp span[style*="46px"]{{width:36px!important;height:36px!important}}.lp>div{{gap:10px!important}}.lp>div>span:first-child{{width:66px!important;font-size:15px}}}}</style>{FOOT}''')

question('13-vraag-60-lagune',7,'De Lagune',4,60,'Ik voel me thuis op een plek waar ik zelf mag kiezen.',img='lagune',pos='center 45%',lob='kreeft-schild',encour='<b>Laatste vraag.</b> Daarna is je schild compleet en zie je wie jij bent.',gtxt='Laatste vraag van de reis. Mooi gedaan tot nu toe.')

# 14 dit ben jij
K=[('rif','Zo ben jij','Afmaker','Je maakt graag af waar je aan begint.'),('grot','Dit doe je graag','Onderzoeken en maken','Je wilt weten hoe iets werkt.'),('vuurtoren','Dit vind je belangrijk','Rust en duidelijkheid','Je houdt van een helder plan.'),('lagune','Hier voel je je thuis','Zelfstandig en vrij','Je kiest graag zelf hoe je iets doet.')]
kc=''.join(f'<div style="background:#fff;border:1px solid #e4d7bf;border-radius:18px;overflow:hidden;box-shadow:0 4px 14px rgba(16,40,74,.08)"><div style="height:120px;background:url(img/{i}.jpg) center/cover"></div><div style="padding:16px 18px"><p class="kicker" style="font-size:12px">{a}</p><h3 style="font-size:24px;margin:4px 0 6px">{b}</h3><p style="font-size:16px;color:var(--muted)">{c}</p><button class="speak" style="width:40px;height:40px;margin-top:10px">{SPK}</button></div></div>' for i,a,b,c in K)
page('14-resultaat-dit-ben-jij',f'''{top()}{bandcss('rb','lagune',460,300,'center 40%','center 40%')}<section class="band rb" style="padding-top:36px">{journey(8)}
<div class="wrap rs" style="display:flex;align-items:flex-end;justify-content:space-between;margin-top:40px"><div style="background:rgba(16,40,74,.85);border:1px solid rgba(226,194,122,.55);border-radius:20px;padding:20px 30px;color:#fff"><p class="kicker" style="color:#ffd98a">Expeditie klaar · alle 60 vragen</p><h1 style="font-size:62px" class="rs-h">Dit ben jij.</h1></div><span class="d-only" style="background:rgba(16,40,74,.85);border-radius:16px;padding:12px 16px;color:#ffd98a;font-weight:600;font-size:15px;text-align:center"><span class="shield" style="display:grid;margin:0 auto 6px;width:84px;height:96px;clip-path:path('M42 0 L84 14 L84 50 C84 76 64 90 42 96 C20 90 0 76 0 50 L0 14 Z')"><i class="f"></i><i class="f"></i><i class="f"></i><i class="f"></i></span>Schild compleet</span></div></section>
<div class="wrap" style="position:relative;z-index:4"><div class="q-layout"><div>
<div class="card" style="margin-bottom:24px"><div class="qrow" style="margin-bottom:12px"><button class="speak">{SPK}</button><span class="speak-label">Laat jouw verhaal voorlezen</span></div>
<p style="font-size:21px;line-height:1.6">Jij maakt graag af waar je aan begint. Je wilt weten hoe iets werkt en maakt het graag zelf. Rust en duidelijkheid vind je belangrijk. Je voelt je thuis op een plek waar je zelf mag kiezen. En je houdt van lezen en dieren.</p></div>
<div class="kc" style="display:grid;grid-template-columns:repeat(4,1fr);gap:16px">{kc}</div>
<div class="card" style="margin-top:24px"><h2 style="font-family:Lexend;font-size:21px;margin-bottom:10px">Je houdt van</h2><div class="chips"><span class="chip on">Lezen</span><span class="chip on">Dieren</span><span class="chip on">Natuur</span><span class="chip on">Duiken</span></div>
<h2 style="font-family:Lexend;font-size:21px;margin:22px 0 10px">Beroepen om eens te bekijken</h2><div class="chips"><span class="chip">Dierenverzorger</span><span class="chip">Dierenarts</span><span class="chip">Bioloog</span><span class="chip">Boswachter</span><span class="chip">Programmeur</span></div>
<p class="note" style="margin-top:22px">Je leraar kan dit verhaal ook zien, met jouw code. Niemand anders.</p>
<div class="qfoot" style="flex-wrap:wrap"><button class="btn btn-ghost">Bewaar als PDF</button><button class="btn btn-main" style="margin-left:auto">Check je droombaan {NEXT}</button></div></div></div>
<aside class="d-only">{bubble('Dit heb ik onderweg over jou geleerd. Klopt het een beetje?','b')}<img class="lob" src="img/kreeft-kijker.png" style="margin-top:22px" alt=""></aside>
<div class="m-only" style="order:-1"><div class="guide"><div class="ring"></div>{bubble('Dit heb ik onderweg over jou geleerd.','l')}</div></div></div></div>
<style>@media(max-width:600px){{.kc{{grid-template-columns:1fr 1fr!important;gap:10px!important}}.kc h3{{font-size:19px!important}}.kc>div>div:last-child{{padding:12px!important}}.kc>div>div:first-child{{height:80px!important}}.rs-h{{font-size:44px!important}}.rs{{margin-top:24px!important}}}}</style>{FOOT}''')

# 15 droombaan
stops=[('Nu','Groep 8','Kies straks op de middelbare school biologie en scheikunde.','haven'),('Straks','Havo of vwo','Diergeneeskunde vraagt vwo met biologie en scheikunde. Of eerst havo en dan een jaar hbo.','vuurtoren'),('Daarna','Diergeneeskunde studeren','Aan de universiteit, ongeveer 6 jaar.','grot'),('Doel','Dierenarts','In een praktijk, een dierentuin of op de boerderij.','lagune')]
st=''
for i,(a,b,c,img) in enumerate(stops):
    last=i==len(stops)-1
    st+=f'''<li style="display:grid;grid-template-columns:70px 1fr;gap:16px;position:relative;padding-bottom:{0 if last else 26}px">{"" if last else '<span style="position:absolute;left:34px;top:68px;bottom:0;border-left:4px dashed var(--coral)"></span>'}
<span style="width:70px;height:70px;border-radius:50%;border:4px solid {"var(--coral)" if last else "var(--brass)"};background:url(img/{img}.jpg) center/cover;box-shadow:0 3px 10px rgba(0,0,0,.2)"></span>
<div style="background:rgba(255,253,248,.94);border-radius:14px;padding:12px 16px;border:1px solid #e4d7bf"><p class="kicker" style="font-size:12px;color:{"var(--coral)" if last else "var(--teal)"}">{a}</p><h3 style="font-size:23px;margin:2px 0 4px">{b}{" ★" if last else ""}</h3><p style="font-size:16px;color:var(--muted)">{c}</p></div></li>'''
page('15-droombaan-route',f'''{top()}{bandcss('db','zeekaart',280,190,'center 30%','center 30%')}<section class="band db"><div class="wrap" style="padding-top:40px"><div style="display:inline-block;background:rgba(16,40,74,.88);border:1px solid rgba(226,194,122,.55);border-radius:20px;padding:18px 28px;color:#fff"><p class="kicker" style="color:#ffd98a">Droombaan-checker</p><h1 style="font-size:48px" class="db-h">Wat wil jij later worden?</h1></div></div></section>
<div class="wrap" style="position:relative;z-index:4"><div class="q-layout"><div>
<div class="card" style="margin-bottom:24px"><div class="dbf" style="display:flex;gap:12px;align-items:center;flex-wrap:wrap"><div class="inp" style="flex:1;min-width:220px;font-weight:600">Dierenarts <span style="color:var(--muted);font-weight:400;font-size:15px">Wijzig</span></div><button class="speak">{SPK}</button><button class="btn btn-ghost">Weet ik nog niet</button></div></div>
<div class="dg" style="display:grid;grid-template-columns:1.2fr 1fr;gap:24px;align-items:start">
<div style="background:url(img/zeekaart.jpg) center/cover;border-radius:22px;padding:24px;border:3px solid #c8b48a"><h2 style="font-size:28px;background:rgba(255,253,248,.94);display:inline-block;padding:6px 14px;border-radius:10px;margin-bottom:18px">Jouw route</h2><ol style="list-style:none">{st}</ol></div>
<div class="card" style="padding:26px"><h2 style="font-family:Lexend;font-size:21px;margin-bottom:10px">Dit heb je al</h2><ul style="list-style:none;display:grid;gap:8px"><li>✓&nbsp; Je wilt weten hoe iets werkt</li><li>✓&nbsp; Je houdt van dieren</li></ul>
<h2 style="font-family:Lexend;font-size:21px;margin:22px 0 10px">Hier kun je aan werken</h2><ul style="list-style:none;display:grid;gap:8px"><li><span style="width:10px;height:10px;border-radius:50%;background:var(--brass);display:inline-block;margin-right:10px"></span>Zorgen voor een dier of plant</li><li><span style="width:10px;height:10px;border-radius:50%;background:var(--brass);display:inline-block;margin-right:10px"></span>Schoolwerk netjes afmaken</li><li><span style="width:10px;height:10px;border-radius:50%;background:var(--brass);display:inline-block;margin-right:10px"></span>Rustig blijven als iets spannend voelt</li></ul>
<p class="warn" style="margin-top:20px">Mooi. Je hebt al een paar dingen in je rugzak. Praat met je leraar over de volgende stap.</p>
<div style="display:grid;gap:10px;margin-top:20px"><button class="btn btn-main">Bewaar als PDF</button><button class="btn btn-ghost">Kies een andere baan</button></div></div></div></div>
<aside class="d-only">{bubble('Dierenarts? Sterke keuze. Hier is je route.','b')}<img class="lob" src="img/kreeft-kaart.png" style="margin-top:22px" alt=""></aside>
<div class="m-only" style="order:-1"><div class="guide"><div class="ring"></div>{bubble('Dierenarts? Sterke keuze. Hier is je route.','l')}</div></div></div></div>
<style>@media(max-width:600px){{.dg{{grid-template-columns:1fr!important}}.db-h{{font-size:34px!important}}.db .wrap{{padding-top:30px!important}}.dg ol li{{grid-template-columns:52px 1fr!important;gap:10px!important}}.dg ol li>span:nth-child(2),.dg ol li>span:first-child[style*="70px"]{{width:52px!important;height:52px!important}}.dg ol li>span[style*="dashed"]{{left:25px!important;top:52px!important}}.dg>div:first-child{{padding:14px!important}}}}</style>{FOOT}''')

# 16 pauze
page('16-pauze-stoppen',f'''{top()}<div style="position:relative">{bandcss('zb','grot',330,210,'center 50%','center 50%')}<section class="band zb" style="padding-top:150px">{journey(2,'<b>De Schatgrot</b> · wereld 2 van 4 · vraag 7 van 15',(6,15))}</section>
<div class="wrap" style="margin-top:-30px;position:relative;z-index:4;padding-bottom:120px"><div class="card" style="max-width:900px"><div class="qmeta"><span class="kicker">De Schatgrot</span><span>·</span><span>Vraag 22 van 60</span></div><h1 class="qtitle">Ik vind het leuk om iets te maken of te repareren.</h1>{ans()}</div></div>
<div style="position:absolute;inset:0;background:rgba(10,24,44,.72);z-index:10;display:flex;align-items:flex-start;justify-content:center;padding:120px 14px 0">
<div class="card pz" style="max-width:560px;width:100%;text-align:center;padding:36px"><img src="img/kreeft-portret.png" style="width:150px;margin:0 auto 10px" alt="">
<h2 style="font-size:36px;margin-bottom:10px">Even stoppen?</h2><p style="font-size:19px;color:var(--muted)">Je antwoorden zijn bewaard. Met je code ga je later verder waar je was.</p>
<div style="display:grid;gap:12px;margin-top:26px"><button class="btn btn-main btn-big">Doorgaan</button><button class="btn btn-ghost">Stoppen voor nu</button></div></div></div></div>{FOOT}''')
print('ok')
