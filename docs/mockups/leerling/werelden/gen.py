import os,json
from painted_svgs import (
    chip_icon,
    job_icon,
    job_none_icon,
    day_icon,
    world_deco,
    category_art_img,
    day_hero_art_img,
    day_scene_art,
    PLUS as PLUS_SVG,
)

JOB_INTRO_B1='Dit zijn 5 beroepen die goed bij jou passen. Dat zien we aan jouw antwoorden!'
JOB_COACH=(
    'Dit zijn 5 beroepen die goed bij jou passen. Dat zien we aan jouw antwoorden! '
    'Kies er één. Of tik: Ik vind er geen leuk. Druk daarna op Bevestigen.'
)
JOB_READ_ALOUD=JOB_COACH
JOB_NONE_LABEL='Ik vind er geen leuk'
JOB_NONE_NEXT=(
    'Dan ga je naar <b>Check je droombaan</b>. Daar zoek je zelf een beroep. '
    'Dat mag — je hoeft nu niet te kiezen.'
)
JOB_JOB_NEXT='Als je bevestigt, ga je straks verder met het beroep dat jij koos.'
OUT=os.path.dirname(os.path.abspath(__file__))+'/src/'
SPK='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 5L6 9H2v6h4l5 4V5z"/><path d="M15.5 8.5a5 5 0 010 7"/><path d="M19 5a10 10 0 010 14"/></svg>'
PAU='<svg viewBox="0 0 24 24" fill="currentColor"><rect x="6" y="5" width="4" height="14" rx="1"/><rect x="14" y="5" width="4" height="14" rx="1"/></svg>'
BACK='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M15 18l-6-6 6-6"/></svg>'
NEXT='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12h14M13 6l6 6-6 6"/></svg>'
CHK='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M7 12.5l3.5 3.5L17 9"/></svg>'
PZ='<svg viewBox="0 0 24 24" fill="currentColor"><path d="M10 3a2 2 0 014 0v2h4a1 1 0 011 1v4h-2a2 2 0 000 4h2v4a1 1 0 01-1 1h-4v-2a2 2 0 00-4 0v2H6a1 1 0 01-1-1v-4h2a2 2 0 000-4H5V6a1 1 0 011-1h4V3z"/></svg>'
SWAP='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 8h13l-3-3M20 16H7l3 3"/></svg>'
FOOT='<footer><a>Privacyverklaring</a><a>Toegankelijkheid</a></footer>'

W={
 'zee':dict(cls='t-zee',name='Zee-expeditie',small='Expeditie',
   nodes=[('w','Koraalrif','img/rif.jpg'),('p','Schelpenrij',''),('w','Schatgrot','img/grot.jpg'),('p','Schatkaart',''),('w','Pauze-eiland','img/eiland.jpg'),('w','Vuurtoren','img/vuurtoren.jpg'),('p','Lampen',''),('w','Lagune','img/lagune.jpg'),('w','Dit ben jij','FINALZEE')],
   start='Haven',art='het',stations=['Koraalrif','Schatgrot','Vuurtoren','Lagune'],
   map='zeekaart.jpg',routeband=('oceaan',230,150,'center 45%','center 45%'),qband=('rif','center 30%'),iband=('rif','center 75%'),
   lob_q='kreeft-welkom',lob_i='kreeft-kaart',route_sub='Zo vaar je van de haven naar jouw eiland.',
   route_bubble='Elke 15 vragen wacht er een korte puzzel. Halverwege rust je uit op het eiland.',
   intro_sub='Vind het patroon in de rij schelpen op het rif.',intro_bubble='Eerste deel klaar. Even je hoofd gebruiken.'),
 'tuin':dict(cls='t-tuin',name='Eilandtuin',small='Eilandtuin',
   nodes=[('w','Bloemenbaai','img/dot-tuin-1.jpg'),('p','Schelpenrij',''),('w','Koraaltuin','img/dot-tuin-2.jpg'),('p','Schatkaart',''),('w','Pauze-eiland','img/dot-tuin-3.jpg'),('w','Waterval','img/dot-tuin-4.jpg'),('p','Lampen',''),('w','Zonnelagune','img/dot-tuin-6.jpg'),('w','Dit ben jij','img/dot-tuin-5.jpg')],
   start='Steiger',art='de',stations=['Bloemenbaai','Koraaltuin','Waterval','Zonnelagune'],
   map='tuin-kaart.jpg',routeband=('tuin-baai',230,150,'center 22%','center 22%'),qband=('tuin-baai','center 78%'),iband=('tuin-koraal','center 82%'),
   lob_q='kreeft-welkom',lob_i='kreeft-kaart',route_sub='Zo ga je van de steiger naar jouw eiland.',
   route_bubble='Elke 15 vragen wacht er een korte puzzel. Halverwege rust je uit op het eiland.',
   intro_sub='Vind het patroon in de rij schelpen in de koraaltuin.',intro_bubble='Eerste deel klaar. Even je hoofd gebruiken.'),
 'ster':dict(cls='t-ster',name='Sterrenkust',small='Sterrenkust',
   nodes=[('w','Duinpad','img/dot-ster-1.jpg'),('p','Schelpenrij',''),('w','Maangrot','img/dot-ster-2.jpg'),('p','Schatkaart',''),('w','Pauze-eiland','img/dot-ster-3.jpg'),('w','Vuurtoren','img/dot-ster-4.jpg'),('p','Lampen',''),('w','Sterrenbaai','img/dot-ster-5.jpg'),('w','Dit ben jij','img/dot-ster-6.jpg')],
   start='Strandpaal',art='het',stations=['Duinpad','Maangrot','Vuurtoren','Sterrenbaai'],
   map='sterren-kaart.jpg',routeband=('sterren-strand',230,150,'center 12%','70% 12%'),qband=('sterren-kaart','center 82%'),iband=('sterren-strand','center 78%'),
   lob_q='kreeft-welkom',lob_i='kreeft-kijker',route_sub='Je route is een sterrenbeeld. Elke ster is een stop.',
   route_bubble='Elke 15 vragen wacht er een korte puzzel. Halverwege rust je uit op het eiland.',
   intro_sub='Vind het patroon in de rij schelpen op het maanstrand.',intro_bubble='Eerste deel klaar. Even je hoofd gebruiken.'),
}
Q='Als een vriend verdrietig is, merk ik dat snel.'

def page(name,body,cls):
    open(OUT+name+'.html','w').write(f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Lobsy · Groep 7/8</title><link rel="stylesheet" href="style.css"><link rel="stylesheet" href="themes.css"><link rel="stylesheet" href="world-deco.css"></head><body class="{cls}">{body}</body></html>')
def top(small='Expeditie',code=True):
    r=f'<header class="top"><div class="brand"><img src="img/embleem.png" alt="">Lobsy<small>{small}</small></div><div class="sp"></div>'
    if code: r+=f'<span class="chip-code">8B · K7R-2QM</span><button class="btn-pauze">{PAU}<span>Pauze</span></button>'
    return r+'</header>'
def journey(w,cur,meta=None,seg=None):
    N=w['nodes'];n=len(N);h=''
    for i,(t,lab,img) in enumerate(N):
        st='done' if i<cur else ('cur' if i==cur else '')
        cls='jn'+(' p' if t=='p' else '')+(' '+st if st else '')
        here='<span class="here">Hier ben je</span>' if i==cur else ''
        if t=='p': dot=f'<span class="dot">{PZ}</span>'
        elif img=='FINALZEE': dot='<span class="dot" style="background:#7a5a1e url(img/zeekaart.jpg) 84% 22%/900% no-repeat"></span>'
        else: dot=f'<span class="dot" style="background-image:url({img})"></span>'
        h+=f'<div class="{cls}">{here}{dot}<b>{lab}</b></div>'
    pct=f'calc((100% - 52px) * {cur/(n-1):.4f})'
    m=''
    if meta:
        s=''
        if seg:
            done,tot=seg; s='<div class="seg">'+''.join('<i class="f"></i>' if k<done else ('<i class="c"></i>' if k==done else '<i></i>') for k in range(tot))+'</div>'
        m=f'<div class="jmeta"><span>{meta}</span>{s}</div>'
    return f'<div class="journey"><div class="jrow"><span class="jdone" style="width:{pct}"></span>{h}</div>{m}</div>'
def bandcss(cls,img,hd,hm,posd,posm):
    return f'<style>.{cls}{{background-image:url(img/{img}.jpg);background-position:{posd};min-height:{hd}px}}@media(max-width:600px){{.{cls}{{min-height:{hm}px;background-position:{posm}}}}}</style>'
def ans(sel=None):
    L=[('Nee',0),('Niet echt',25),('Soms',50),('Best wel',75),('Ja!',100)]
    return '<div class="answers">'+''.join(f'<button class="ans{" sel" if a==sel else ""}"><span class="lvl"><i style="height:{p}%"></i></span>{a}</button>' for a,p in L)+'</div>'
def bubble(txt,tail='b',btn=True):
    b=f'<div class="bt"><button class="speak" aria-label="Voorlezen">{SPK}</button><span class="speak-label">Lees voor</span></div>' if btn else ''
    return f'<div class="bubble tail-{tail}"><span class="who">Lobsy</span>{txt}{b}</div>'

# ---------- 00 keuze ----------
cards=[('zee','img/haven.jpg','center 45%','Zee-expeditie','Varen langs riffen, grotten en een vuurtoren. Stoer en avontuurlijk.','#10284a'),
       ('tuin','img/tuin-baai.jpg','center 60%','Eilandtuin','Bloemen, koraal en warme baaien. Kleurrijk en zonnig.','#3a2230'),
       ('ster','img/sterren-kaart.jpg','center 30%','Sterrenkust','Een rustige kust in de nacht. Je route is een sterrenbeeld.','#141c36')]
def keuze(name,selected=None):
    cs=''
    for k,img,pos,t,d,col in cards:
        sel=' sel' if k==selected else ''
        pick='<span class="pick">Gekozen</span>' if k==selected else ''
        cs+=f'<button class="wcard{sel}"><div class="wimg" style="background-image:url({img});background-position:{pos}">{pick}</div><div class="wbody"><h2>{t}</h2><p>{d}</p><div class="wgo"><span>Kies deze wereld</span><i style="background:{col}">{NEXT}</i></div></div></button>'
    body=f'''{top('Groep 7 en 8')}{bandcss('kz','keuze-strip',200,120,'center 55%','30% 55%')}<section class="band kz"></section>
<div class="wrap" style="margin-top:-60px;position:relative;z-index:4"><div class="card kz-card">
<div class="kz-head" style="display:grid;grid-template-columns:1fr auto;gap:24px;align-items:center">
<div><p class="kicker">Voor je begint</p><h1 style="font-size:46px;margin:6px 0 8px" class="kz-h">Kies jouw wereld.</h1><p style="font-size:20px;color:var(--muted)">Kies wat jij mooi vindt. Tik op een wereld.</p></div>
<div class="d-only" style="display:flex;align-items:center;gap:14px;max-width:440px"><div class="ring"></div>{bubble('Ik ga met je mee, in elke wereld.','l',btn=True)}</div></div>
<div class="worlds">{cs}</div>
<div class="same">{CHK.replace('<svg','<svg style="width:30px;height:30px;color:var(--ok);flex:none"')}<div><b>Zelfde vragen en spellen, alleen een andere wereld.</b> Wisselen kan later altijd, via Pauze. Je antwoorden blijven bewaard.</div></div>
</div>
<div class="m-only" style="margin-top:18px"><div class="guide"><div class="ring"></div>{bubble('Ik ga met je mee, in elke wereld.','l')}</div></div>
</div>
<style>@media(max-width:600px){{.kz-head{{grid-template-columns:1fr!important}}.kz-h{{font-size:34px!important}}.same{{font-size:15px}}.kz+.wrap{{margin-top:-30px!important}}}}</style>{FOOT}'''
    page(name,body,'t-zee')

# ---------- route ----------
PINS={
 'zee':[(160,430),(300,345),(352,262),(425,222),(505,300),(578,262),(718,192),(770,262),(812,345),(855,126)],
 'tuin':[(228,452),(350,388),(318,292),(392,196),(482,252),(566,292),(694,178),(772,262),(800,355),(868,140)],
 'ster':[(110,366),(207,345),(282,323),(346,280),(397,323),(477,266),(572,236),(673,217),(749,181),(849,111)],
}
TYPES=['s','w','p','w','p','i','w','p','w','f']
def pts(k):
    P=PINS[k]; s=''
    if k=='ster':
        d='M'+' L'.join(f'{x},{y}' for x,y in P)
        s+=f'<path d="{d}" fill="none" stroke="#f3e2a8" stroke-width="3" opacity=".75"/>'
        cols={'w':'#24305a','p':'#b7a06a','i':'#4a6a8c','s':'#4a7a64','f':'#c7802f'}
    else:
        if k=='zee': d='M160,430 C220,400 260,370 300,345 S330,280 352,262 S400,225 425,222 S480,270 505,300 S560,270 578,262 S680,200 718,192 S760,235 770,262 S800,320 812,345 S900,300 905,230 S880,150 855,126'
        else: d='M228,452 C280,430 320,410 350,388 S320,320 318,292 S360,210 392,196 S460,230 482,252 S540,290 566,292 S660,200 694,178 S760,235 772,262 S790,330 800,355 S910,300 915,230 S890,160 868,140'
        lc='#d4532f' if k=='zee' else '#b5452c'
        s+=f'<path d="{d}" fill="none" stroke="#fffdf8" stroke-width="9" stroke-linecap="round" opacity=".85"/><path d="{d}" fill="none" stroke="{lc}" stroke-width="4.5" stroke-dasharray="2 11" stroke-linecap="round"/>'
        cols={'w':'#10284a','p':'#b98f3e','i':'#0f5b66','s':'#2f7d5b','f':'#d4532f'} if k=='zee' else {'w':'#3a2230','p':'#c08a4a','i':'#4d7a5c','s':'#4d7a5c','f':'#c4553a'}
    st='#fffdf8' if k!='ster' else '#f3e2a8'
    for i,((x,y),t) in enumerate(zip(P,TYPES)):
        col=cols[t]
        if k=='ster' and t!='p': s+=f'<circle cx="{x}" cy="{y}" r="26" fill="#f3e2a8" opacity=".18"/>'
        if t=='p': s+=f'<rect x="{x-14}" y="{y-14}" width="28" height="28" rx="5" transform="rotate(45 {x} {y})" fill="{col}" stroke="{st}" stroke-width="3"/>'
        else: s+=f'<circle cx="{x}" cy="{y}" r="17" fill="{col}" stroke="{st}" stroke-width="3"/>'
        s+=f'<text x="{x}" y="{y+6}" text-anchor="middle" font-family="Lexend" font-weight="700" font-size="17" fill="#fff">{i+1}</text>'
    x0,y0=P[0]; s+=f'<circle cx="{x0}" cy="{y0}" r="27" fill="none" stroke="#ffd98a" stroke-width="4"/>'
    return f'<svg viewBox="0 0 1024 576" style="position:absolute;inset:0;width:100%;height:100%">{s}</svg>'
def route(name,k):
    w=W[k]; N=w['nodes']
    labels=[(w['start'],'Start','s')]
    subs={0:'Vraag 1 tot 15',2:'Vraag 16 tot 30',5:'Vraag 31 tot 45',7:'Vraag 46 tot 60'}
    full={'Lampen':'Vuurtorenlampen'}
    pz=0
    for i,(t,lab,img) in enumerate(N):
        if t=='p': pz+=1; labels.append((full.get(lab,lab),f'Puzzel {pz}','p'))
        elif lab=='Pauze-eiland': labels.append((lab,'Wat vind jij leuk?','i'))
        elif lab=='Dit ben jij': labels.append((lab,'Jouw eiland' if k!='ster' else 'Jouw ster','f'))
        else: labels.append((lab,subs[i],'w'))
    li=''
    for i,(l,sub,t) in enumerate(labels):
        col={'w':'var(--navy)','p':'var(--brass)','i':'var(--teal)','s':'var(--ok)','f':'var(--coral)'}[t]
        shape='border-radius:7px;transform:rotate(45deg) scale(.82)' if t=='p' else 'border-radius:50%'
        num=f'<span style="display:inline-block;transform:rotate(-45deg)">{i+1}</span>' if t=='p' else str(i+1)
        here=' <span style="background:var(--coral);color:#fff;font-size:12px;border-radius:6px;padding:1px 7px;margin-left:6px">Hier ben je</span>' if i==0 else ''
        li+=f'<li style="display:flex;align-items:center;gap:12px"><span style="width:32px;height:32px;{shape};background:{col};color:#fff;font-weight:700;font-size:15px;display:flex;align-items:center;justify-content:center;flex:none">{num}</span><span><b>{l}</b>{here}<br><span style="color:var(--muted);font-size:15px">{sub}</span></span></li>'
    rb=w['routeband']
    body=f'''{top(w['small'])}{bandcss('kb',*rb)}
<section class="band kb"><div class="wrap" style="padding-top:44px"><h1 style="font-size:46px;color:#fff;text-shadow:0 2px 14px rgba(0,0,0,.5)" class="rk-h">De route</h1><p style="color:#fff;text-shadow:0 2px 10px rgba(0,0,0,.6);font-size:20px">{w['route_sub']}</p></div></section>
<div class="wrap" style="margin-top:-20px;position:relative;z-index:3"><div class="card rk" style="display:grid;grid-template-columns:1fr 300px;gap:30px;padding:26px">
<div><div style="position:relative;border-radius:16px;overflow:hidden;aspect-ratio:16/9"><img src="img/{w['map']}" style="width:100%;height:100%" alt="Kaart">{pts(k)}</div>
<div class="d-only" style="display:flex;align-items:center;gap:16px;margin-top:20px"><div class="ring"></div>{bubble(w['route_bubble'],'l')}</div></div>
<div><ol style="list-style:none;display:grid;gap:12px;font-size:17px">{li}</ol><button class="btn btn-main" style="width:100%;margin-top:22px;padding:16px 14px">Naar {w['art']} {w['stations'][0]} {NEXT}</button>
<button class="btn btn-quiet" style="width:100%;margin-top:6px;font-size:16px">{SWAP.replace('<svg','<svg style="width:20px;height:20px"')}Andere wereld kiezen</button></div></div>
<div class="m-only" style="margin-top:18px"><div class="guide"><div class="ring"></div>{bubble(w['route_bubble'],'l')}</div></div></div>
<style>@media(max-width:600px){{.rk{{grid-template-columns:1fr!important;padding:12px!important;gap:18px!important}}.rk-h{{font-size:34px!important}}.kb .wrap{{padding-top:24px!important}}}}</style>{FOOT}'''
    page(name,body,w['cls'])

# ---------- vraag ----------
def question(name,k):
    w=W[k]; st=w['stations'][0]; art=w['art'].capitalize()+' '
    meta=f'<b>{art}{st}</b> · deel 1 van 4 · vraag 1 van 15'
    img,pos=w['qband']
    gtxt='Ik lees elke vraag voor. Tik op de luidspreker om het nog eens te horen.'
    body=f'''{top(w['small'])}{bandcss('qb',img,330,210,pos,pos)}<section class="band qb" style="padding-top:150px">{journey(w,0,meta,(0,15))}</section>
<div class="wrap" style="margin-top:-30px;position:relative;z-index:4"><div class="q-layout"><div class="card">
<div class="qmeta"><span class="kicker">{art}{st}</span><span>·</span><span>Vraag 1 van 60</span></div>
<h1 class="qtitle">{Q}</h1>
<div class="qrow"><button class="speak on">{SPK}</button><span class="speak-label">Lobsy leest de vraag voor</span></div>
{ans()}
<div class="qfoot"><button class="btn btn-ghost">{BACK}Vorige</button></div></div>
<aside class="d-only" style="padding-top:10px">{bubble(gtxt,'b')}<img class="lob" src="img/{w['lob_q']}.png" alt="Lobsy" style="margin-top:22px"></aside>
<div class="m-only"><div class="guide"><div class="ring"></div>{bubble(gtxt,'l')}</div></div>
</div></div>{FOOT}'''
    page(name,body,w['cls'])

# ---------- puzzel-intro ----------
S=lambda n:f'<img src="img/schelp-{n}.png" alt="">'
def intro(name,k):
    w=W[k]; img,pos=w['iband']
    row=''.join(f'<div class="slot">{S(n)}</div>' for n in [1,2,2,3,1])+'<div class="slot q">?</div>'
    body=f'''{top(w['small'])}{bandcss('ib',img,300,190,pos,pos)}<section class="band ib" style="padding-top:130px">{journey(w,1,'<b>Puzzel 1 van 3</b> · Schelpenrij · na vraag 15')}</section>
<div class="wrap" style="margin-top:-24px;position:relative;z-index:4"><div class="q-layout"><div class="card">
<p class="kicker">Puzzel 1 van 3</p><h1 style="font-size:44px;margin:6px 0 6px" class="in-h">Schelpenrij</h1>
<p style="font-size:20px;color:var(--muted)">{w['intro_sub']}</p>
<div class="prev-row">{row}</div>
<ol class="steps"><li><span>1</span>Kijk goed naar de rij.</li><li><span>2</span>Zoek wat er steeds terugkomt.</li><li><span>3</span>Kies welke schelp erna komt.</li></ol>
<div class="facts"><span>Geen klok</span><span>Weet je het niet? Ga gewoon verder</span></div>
<div class="qfoot" style="flex-wrap:wrap"><button class="speak">{SPK}</button><span class="speak-label">Laat de uitleg voorlezen</span><button class="btn btn-main btn-big" style="margin-left:auto">Start de puzzel {NEXT}</button></div></div>
<aside class="d-only">{bubble(w['intro_bubble'],'b')}<img class="lob" src="img/{w['lob_i']}.png" style="margin-top:22px" alt="Lobsy"></aside>
<div class="m-only"><div class="guide"><div class="ring"></div>{bubble(w['intro_bubble'],'l')}</div></div></div></div>
<style>@media(max-width:600px){{.in-h{{font-size:34px!important}}.qfoot .btn-main{{order:0!important}}}}</style>{FOOT}'''
    page(name,body,w['cls'])

# ---------- result (Dit ben jij) ----------
STORY=('Jij maakt graag af waar je aan begint. Je wilt weten hoe iets werkt en maakt het graag zelf. '
       'Rust en duidelijkheid vind je belangrijk. Je voelt je thuis op een plek waar je zelf mag kiezen. '
       'En je houdt van lezen en dieren.')
RESULT_TILES=[
 ('Zo ben jij','Je maakt graag af waar je aan begint.'),
 ('Dit doe je graag','Je wilt weten hoe iets werkt.'),
 ('Dit vind je belangrijk','Je houdt van een helder plan.'),
 ('Hier voel je je thuis','Je kiest graag zelf hoe je iets doet.'),
]
LIKES=['Lezen','Dieren','Natuur','Duiken']
JOBS=['Dierenverzorger','Dierenarts','Bioloog','Boswachter','Programmeur']
RESULT_BAND={'zee':('lagune','center 40%'),'tuin':('dot-tuin-6','center 55%'),'ster':('dot-ster-5','center 45%')}
RESULT_TILE_IMG={
 'zee':['img/rif.jpg','img/grot.jpg','img/vuurtoren.jpg','img/lagune.jpg'],
 'tuin':['img/dot-tuin-1.jpg','img/dot-tuin-2.jpg','img/dot-tuin-4.jpg','img/dot-tuin-6.jpg'],
 'ster':['img/dot-ster-1.jpg','img/dot-ster-2.jpg','img/dot-ster-4.jpg','img/dot-ster-5.jpg'],
}
def result(name,k):
    w=W[k]; img,pos=RESULT_BAND[k]; last=len(w['nodes'])-1
    tiles=''
    for (lab,txt),bg in zip(RESULT_TILES,RESULT_TILE_IMG[k]):
        tiles+=f'''<div class="rtile"><div class="rtile-img" style="background-image:url({bg})"></div><div class="rtile-body"><p class="kicker">{lab}</p><p class="rtile-txt">{txt}</p><button class="speak" aria-label="Voorlezen">{SPK}</button></div></div>'''
    chips_l=''.join(f'<span class="chip on">{c}</span>' for c in LIKES)
    chips_j=''.join(f'<span class="chip">{c}</span>' for c in JOBS)
    coach='Kijk, dit ben jij.'
    body=f'''{top(w['small'])}{bandcss('rb',img,460,300,pos,pos)}
<section class="band rb dark" style="padding-top:36px">{journey(w,last,'<b>Klaar</b> · alle 60 vragen · jouw verhaal')}</section>
<div class="wrap rs-hdr"><div class="rs-title"><span class="pill-done">Klaar</span><h1 class="rs-h">Dit ben jij</h1></div></div>
<div class="wrap" style="position:relative;z-index:4;margin-top:-20px"><div class="q-layout"><div>
<div class="card" style="margin-bottom:24px"><div class="qrow" style="margin-bottom:12px"><button class="speak">{SPK}</button><span class="speak-label">Laat jouw verhaal voorlezen</span></div>
<p class="story-body">{STORY}</p></div>
<div class="rtiles">{tiles}</div>
<div class="card" style="margin-top:24px"><h2 class="rsub">Je houdt van</h2><div class="chips">{chips_l}</div>
<h2 class="rsub">Beroepen om eens te bekijken</h2><div class="qrow" style="margin:10px 0 14px"><button class="speak">{SPK}</button><span class="speak-label">Laat de beroepen voorlezen</span></div><div class="chips">{chips_j}</div>
<p class="note" style="margin-top:22px">Je leraar kan dit verhaal ook zien, met jouw code. Niemand anders.</p></div>
<div class="qfoot res-foot"><button class="btn btn-ghost">Jouw DNA-paspoort</button><button class="btn btn-ghost">PDF</button><button class="btn btn-main" style="margin-left:auto">Check je droombaan {NEXT}</button></div></div>
<aside class="d-only">{bubble(coach,'b')}<img class="lob" src="img/kreeft-kijker.png" style="margin-top:22px" alt="Lobsy"></aside>
<div class="m-only" style="order:-1"><div class="guide"><div class="ring"></div>{bubble(coach,'l')}</div></div></div></div>
<style>
.rs-hdr{{display:flex;align-items:flex-end;margin-top:28px;margin-bottom:8px}}
.rs-title{{background:rgba(16,40,74,.88);border:1px solid rgba(226,194,122,.55);border-radius:20px;padding:18px 28px;color:#fff}}
.pill-done{{display:inline-block;background:var(--ok);color:#fff;font-size:13px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;border-radius:999px;padding:4px 12px;margin-bottom:8px}}
.rs-h{{font-size:52px;color:#ffd98a;margin:0}}
.story-body{{font-size:21px;line-height:1.6;margin:0}}
.rtiles{{display:grid;grid-template-columns:repeat(4,1fr);gap:16px;margin-bottom:4px}}
.rtile{{background:#fff;border:1px solid #e4d7bf;border-radius:18px;overflow:hidden;box-shadow:0 4px 14px rgba(16,40,74,.08)}}
.rtile-img{{height:120px;background-size:cover;background-position:center}}
.rtile-body{{padding:16px 18px}}
.rtile-txt{{font-size:16px;color:var(--muted);margin:6px 0 0;line-height:1.45}}
.rtile .speak{{width:40px;height:40px;margin-top:10px}}
.rsub{{font-family:Lexend;font-size:21px;margin:0 0 10px;font-weight:600}}
.res-foot{{flex-wrap:wrap;margin-top:24px}}
@media(max-width:600px){{.rs-h{{font-size:38px!important}}.rs-hdr{{margin-top:16px}}.rtiles{{grid-template-columns:1fr 1fr!important;gap:10px!important}}.rtile-img{{height:80px!important}}.rtile-body{{padding:12px!important}}.rtile-txt{{font-size:15px}}.res-foot .btn-main{{width:100%;margin-left:0!important;order:-1}}.res-foot .btn-ghost{{flex:1;min-width:calc(50% - 8px)}}}}
</style>{FOOT}'''
    page(name,body,w['cls'])

# ---------- pauze-eiland (likes / dislikes split) ----------
ISLAND_CUR=4
I='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">%s</svg>'
PLUS_I=I%('<path d="M12 5v14M5 12h14"/>')
CHIP_ICO={
 'sport':'<circle cx="12" cy="12" r="9"/><path d="M12 3v18M3 12h18"/>',
 'buiten':'<path d="M12 3v4M12 17v4M4.2 7.5l3 3M16.8 16.5l3 3M3 12h4M17 12h4M4.2 16.5l3-3M16.8 7.5l3-3"/>',
 'dieren':'<circle cx="9" cy="9" r="2"/><circle cx="15" cy="9" r="2"/><path d="M7 16c1.5-2 8.5-2 10 0"/>',
 'gamen':'<rect x="4" y="8" width="16" height="10" rx="3"/><path d="M9 12v4M7 14h4"/>',
 'tekenen':'<path d="M4 20l8-8 4 4 8-8"/>',
 'muziek':'<path d="M9 18V6l10-2v12"/><circle cx="7" cy="18" r="2"/><circle cx="17" cy="16" r="2"/>',
 'koken':'<path d="M6 10h12v8H6z"/><path d="M8 10V6M12 10V5M16 10V6"/>',
 'fietsen-repareren':'<circle cx="7" cy="17" r="3"/><circle cx="17" cy="17" r="3"/><path d="M7 17l4-7h3l3 7"/>',
 'bouwen':'<path d="M4 20h16M7 20V9l5-4 5 4v11"/>',
 'techniek':'<path d="M14 7l3-3 4 4-3 3M5 20l9-9"/>',
 'lezen':'<path d="M5 6c2-1 4-1 7 0 3-1 5-1 7 0v12c-2-1-4-1-7 0-3-1-5-1-7 0z"/>',
 'dansen':'<path d="M8 20l4-8 4 2-3 6"/>',
 'theater':'<path d="M4 8h16M6 8l2 12h8l2-12M9 4h6"/>',
 'filmpjes':'<rect x="4" y="7" width="16" height="12" rx="2"/><path d="M10 11l4 2-4 2z"/>',
 'mode':'<path d="M8 7l4-3 4 3v13H8z"/>',
 'kleine-kinderen':'<circle cx="12" cy="8" r="3"/><path d="M6 20c1-4 10-4 12 0"/>',
 'natuur':'<path d="M12 3c-3 4-6 7-6 11a6 6 0 0012 0c0-4-3-7-6-11z"/>',
 'autos':'<path d="M5 16l2-5h10l2 5"/><circle cx="8" cy="17" r="2"/><circle cx="16" cy="17" r="2"/>',
 'computers':'<rect x="3" y="5" width="18" height="12" rx="2"/><path d="M8 21h8"/>',
 'puzzels':'<path d="M8 8h3v3H8zM13 8h3v3h-3zM8 13h3v3H8zM13 13h3v3h-3z"/>',
 'rekenen':'<path d="M7 7h10v10H7z"/><path d="M9 9l6 6M15 9l-6 6"/>',
 'talen':'<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3a14 14 0 000 18M12 3a14 14 0 010 18"/>',
 'reizen':'<path d="M4 16l8-10 8 10H4z"/><path d="M12 6v10"/>',
 'programmeren':'<path d="M8 8l-4 4 4 4M16 8l4 4-4 4M14 4l-4 16"/>',
 'voor-de-klas':'<path d="M12 4v16M8 8h8M8 16h8"/>',
 'lang-stilzitten':'<rect x="6" y="5" width="12" height="14" rx="2"/><path d="M9 9h6"/>',
 'hard-werken-kou':'<path d="M12 3v18M8 7h8M8 11h8M8 15h8"/>',
 'veel-lezen':'<path d="M6 6h12v14H6z"/><path d="M9 9h6M9 12h6"/>',
 'alleen-werken':'<circle cx="12" cy="8" r="3"/><path d="M6 20c1-3 10-3 12 0"/>',
 'druk-lawaai':'<path d="M5 10h4l4-3v14l-4-3H5z"/><path d="M17 9a4 4 0 010 6"/>',
 'vies-worden':'<path d="M8 8c0 4 2 8 4 8s4-4 4-8"/>',
}
LIKE_LABELS=[
 ('sport','Sport'),('buiten','Buiten zijn'),('dieren','Dieren'),('gamen','Gamen'),('tekenen','Tekenen'),('muziek','Muziek'),
 ('koken','Koken of bakken'),('fietsen-repareren','Fietsen repareren'),('bouwen','Bouwen & knutselen'),('techniek','Techniek'),
 ('lezen','Lezen'),('dansen','Dansen'),('theater','Theater'),('filmpjes','Filmpjes maken'),('mode','Mode'),
 ('kleine-kinderen','Kleine kinderen'),('natuur','Natuur'),('autos',"Auto's & motoren"),('computers','Computers'),('puzzels','Puzzels'),
 ('rekenen','Rekenen'),('talen','Talen'),('reizen','Reizen'),('programmeren','Programmeren'),
]
DISLIKE_EXTRA=[
 ('voor-de-klas','Voor de klas praten'),('lang-stilzitten','Lang stilzitten'),('hard-werken-kou','Hard werken in de kou'),
 ('veel-lezen','Veel lezen'),('alleen-werken','Alleen werken'),('druk-lawaai','Druk en lawaai'),('vies-worden','Vies worden'),
]
def chip_btn(key,label,on=False,kind='like'):
    svg=chip_icon(key)
    cls='pchip on '+kind if on else 'pchip'
    return f'<button type="button" class="{cls}"><span class="ico">{svg}</span>{label}</button>'
def island_band_img(k):
    for t,lab,img in W[k]['nodes']:
        if lab=='Pauze-eiland': return img.replace('img/','').replace('.jpg','')
    return 'eiland'
def island_page(name,k,step):
    w=W[k]; ib=island_band_img(k)
    meta=f'<b>Pauze-eiland</b> · 30 van 60 klaar · even rust · stap {step} van 2'
    pos='center 45%'
    if step==1:
        title='Wat jij leuk vindt'
        lead='Even rust. Waar word jij blij van?'
        coach='Je bent halverwege. Kies wat jij leuk vindt. Je mag ook niks kiezen.'
        on={'dieren','natuur','tekenen'}
        chips=''.join(chip_btn(key,lab,key in on,'like') for key,lab in LIKE_LABELS)
        chips+=f'<button type="button" class="pchip other"><span class="ico">{PLUS_SVG}</span>+ Iets anders</button>'
        extra=''
    else:
        title='Wat jij niet leuk vindt'
        lead='Wat vind jij niet leuk?'
        coach='Kies wat jij niet leuk vindt. Ook leeg mag.'
        on={'lang-stilzitten','druk-lawaai','voor-de-klas'}
        all_chips=LIKE_LABELS+DISLIKE_EXTRA
        chips=''.join(chip_btn(key,lab,key in on,'dislike') for key,lab in all_chips)
        chips+=f'<button type="button" class="pchip other on dislike"><span class="ico">{PLUS_SVG}</span>+ Iets anders</button>'
        extra='''<label class="field" style="margin-top:14px"><span class="island-sub">Iets anders</span>
<input style="width:100%;font:inherit;font-size:19px;padding:14px 16px;border-radius:12px;border:2px solid #cdbf9f;background:#fff" value="" placeholder="bijv. paardrijden" maxlength="24"></label>
<p class="warn" style="margin-top:14px"><b>Let op.</b> Zet hier geen namen, adres of telefoonnummer.</p>'''
    body=f'''{top(w['small'])}{bandcss('ib2',ib,280,200,pos,pos)}<section class="band ib2" style="padding-top:130px">{journey(w,ISLAND_CUR,meta)}</section>
<div class="wrap" style="margin-top:-24px;position:relative;z-index:4"><div class="q-layout"><div class="card card-deco">{world_deco(k)}
<div class="qmeta"><span class="kicker">Pauze-eiland</span><span>·</span><span>30 van 60 klaar</span></div>
<span class="step-pill">Stap {step} van 2</span>
<p class="island-sub">{title}</p>
<h1 class="island-lead">{lead}</h1>
<div class="qrow"><button class="speak on">{SPK}</button><span class="speak-label">Lobsy leest de tekst voor</span></div>
<div class="pick-chips" role="group" aria-label="{title}">{chips}</div>{extra}
<div class="qfoot"><button class="btn btn-ghost">{BACK}Vorige</button><button class="btn btn-main" style="margin-left:auto">Klaar, verder! {NEXT}</button></div></div>
<aside class="d-only" style="padding-top:10px">{bubble(coach,'b')}<img class="lob" src="img/{w['lob_q']}.png" alt="Lobsy" style="margin-top:22px"></aside>
<div class="m-only"><div class="guide"><div class="ring"></div>{bubble(coach,'l')}</div></div>
</div></div>{FOOT}'''
    page(name,body,w['cls'])

# ---------- beroep kiezen (5 RIASEC-richtingen, groep 7/8 pool) ----------
DJ='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">%s</svg>'
JOB_ICONS={
 'dierenverzorger':'<path d="M13 9c2-4 4-6 6-5 2 2-1 6-3 8a6 6 0 1 1-10 0c-2-2-5-6-3-8 2-1 4 1 6 5M9 14h.01M15 14h.01"/>',
 'bioloog':'<path d="M11 20A7 7 0 0 1 4 13C4 7 10 4 20 4c0 10-3 16-9 16ZM4 17c3-3 6-5 11-7"/>',
 'grafisch-ontwerper':'<path d="m12 19 7-7-4-4-7 7-4 1 1-4 7-7 4 4M14 3l7 7M3 21h6"/>',
 'hovenier':'<path d="m10 13-3-3 3-6 3 6-3 3ZM10 13v7M18 16l-3-3 3-6 3 6-3 3ZM18 16v4M3 21h18"/>',
 'brandweer':'<path d="M12 22c4 0 7-2.7 7-6.5 0-3-2-5-4-7-1 3-3 4-4 5-1-2-1-4 .5-6.5C7 9.5 5 13 5 15.5 5 19.3 8 22 12 22Z"/>',
}
# Keys/titles/blurbs/domains align with DreamJobCatalog + PupilPassportCopy (Groep78).
# category_slug = one illustration per beroepsgroep (see img/jobs/{slug}.jpg)
JOB_PICKS=[
 ('dierenverzorger','Dierenverzorger','Zorg & dieren','zorg-dieren',
  'Je zorgt voor dieren: eten geven, verzorgen en kijken of ze gezond zijn.',
  'jij van <b>dieren</b> houdt en graag voor anderen zorgt.',True),
 ('bioloog','Bioloog','Onderzoek','onderzoek',
  'Je ontdekt hoe planten en dieren leven. Soms buiten, soms in een lab.',
  'jij van <b>natuur</b> houdt en wilt weten hoe iets werkt.',False),
 ('grafisch-ontwerper','Grafisch ontwerper','Creatief','creatief',
  'Je bedenkt en maakt plaatjes, logo\'s en andere creatieve dingen.',
  'jij van <b>tekenen</b> en maken houdt.',False),
 ('hovenier','Hovenier','Buiten & groen','buiten-groen',
  'Je maakt tuinen en parken mooi. Je bent bijna de hele dag buiten.',
  'jij van <b>buiten</b> en <b>natuur</b> houdt.',False),
 ('brandweer','Brandweerman/-vrouw','Helpen','helpen',
  'Je helpt mensen als er brand is of als iemand in de knel zit.',
  'jij snel ziet wanneer iemand hulp nodig heeft.',False),
]
def job_pick(name,k,none_selected=False):
    w=W[k]; img,pos=RESULT_BAND[k]; last=len(w['nodes'])-1
    cards=''
    for key,title,domain,cat_slug,blurb,why,default_sel in JOB_PICKS:
        sel=(not none_selected) and default_sel
        art_src=category_art_img(cat_slug)
        day_hint=' · <span style="color:var(--teal);font-weight:600">Tik voor een dag in dit beroep</span>' if key=='dierenverzorger' else ''
        mark='<span class="jmark">✓ Jouw keuze</span>' if sel else ''
        cards+=f'''<button type="button" class="jpick{" sel" if sel else ""}" aria-pressed="{"true" if sel else "false"}">
<figure class="jpick-paint jpick-paint-w-{k}"><img src="{art_src}" alt="" width="400" height="400"></figure><div class="jbody"><p class="jtitle">{title}</p><p class="jdom">{domain}</p><p class="jblurb">{blurb}</p><p class="jwhy">Leuk voor jou omdat {why}{day_hint}</p></div>
<span class="jspeak"><button type="button" class="speak" aria-label="Voorlezen">{SPK}</button></span>
{mark}</button>'''
    none_art=job_none_icon()
    none_mark='<span class="jmark">✓ Jouw keuze</span>' if none_selected else ''
    cards+=f'''<button type="button" class="jpick jpick-none{" sel" if none_selected else ""}" aria-pressed="{"true" if none_selected else "false"}">
<div class="jpick-art jpick-art-none">{none_art}</div><div class="jbody"><p class="jtitle">{JOB_NONE_LABEL}</p>
<p class="jblurb">Geen van deze vijf? Dat mag. Je gaat gewoon verder.</p></div>
<span class="jspeak"><button type="button" class="speak" aria-label="Voorlezen">{SPK}</button></span>
{none_mark}</button>'''
    next_note=f'<p class="note jpick-next{" on" if none_selected else ""}" role="status">{JOB_NONE_NEXT if none_selected else JOB_JOB_NEXT}</p>'
    coach=JOB_COACH
    body=f'''{top(w['small'])}{bandcss('jb',img,360,240,pos,pos)}
<section class="band jb dark" style="padding-top:36px">{journey(w,last,'<b>Klaar!</b> · tijd om een beroep te kiezen')}</section>
<div class="wrap" style="position:relative;z-index:4;margin-top:-20px"><div class="q-layout"><div class="card card-deco">{world_deco(k)}
<div class="qmeta"><span class="kicker">Droombaan</span><span>·</span><span>Kies 1</span></div>
<p class="job-intro-b1">{JOB_INTRO_B1}</p>
<h1 class="island-lead" style="margin-top:4px">Welk beroep past bij jou?</h1>
<p class="jpick-hint">Kies 1 beroep. Of kies: <b>Ik vind er geen leuk</b>.</p>
<p class="sr-read" id="job-read-aloud">{JOB_READ_ALOUD}</p>
<div class="qrow"><button class="speak on" aria-describedby="job-read-aloud">{SPK}</button><span class="speak-label">Lobsy leest de uitleg voor</span></div>
<div class="job-picks" role="radiogroup" aria-label="Kies een beroep of ik vind er geen leuk">{cards}</div>
{next_note}
<div class="qfoot"><button class="btn btn-ghost">{BACK}Vorige</button><button class="btn btn-main">Bevestigen {NEXT}</button></div>
<p class="jpick-hint" style="margin-top:10px;font-size:14px">Druk op Bevestigen als je iets hebt gekozen.</p></div>
<aside class="d-only" style="padding-top:10px">{bubble(coach,'b')}<img class="lob" src="img/kreeft-kijker.png" alt="Lobsy" style="margin-top:22px"></aside>
<div class="m-only"><div class="guide"><div class="ring"></div>{bubble(coach,'l')}</div></div></div></div>{FOOT}'''
    page(name,body,w['cls'])

# ---------- dag in het leven (OccupationDayBlock timeline, zelfde model als OccupationDayPage) ----------
DAY_ICO={
 'start':DJ%('<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/>'),
 'morning':DJ%('<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/>'),
 'pause':DJ%('<path d="M8 6h8v12H8zM10 4h4"/>'),
 'afternoon':DJ%('<path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2"/><circle cx="12" cy="13" r="5"/>'),
 'close':DJ%('<path d="M12 3a7 7 0 1 0 0 14 7 7 0 0 0 0-14Z"/><path d="M12 3v3M5 19h14"/>'),
 'talk':DJ%('<path d="M8 10h8M8 14h5"/>'),
 'plan':DJ%('<path d="M5 6h14M5 12h14M5 18h8"/>'),
 'handover':DJ%('<path d="M5 12h14M13 6l6 6-6 6"/>'),
}
def _title_case_nl(title):
    t=(title or '').strip()
    if not t:
        return 'Beroep'
    return t[0].upper()+t[1:]

def _slug(title):
    import re
    s=re.sub(r'[^a-z0-9]+','-',(title or '').lower().strip())
    return s.strip('-') or 'beroep'

def load_trial_manifest(export_path=None):
    root=os.path.dirname(os.path.abspath(__file__))
    mp=os.path.join(root,'data','trial-20-manifest.json')
    ep=export_path or os.environ.get('OCCUPATION_DAY_EXPORT') or os.path.join(root,'data','occupation-day-in-life.nl.json')
    manifest=json.load(open(mp,encoding='utf-8'))
    export=json.load(open(ep,encoding='utf-8'))
    by_id={}
    for row in export.get('rows') or []:
        eid=(row.get('escoId') or row.get('EscoId') or '').lower()
        if eid:
            by_id[eid]=row
    trials=[]
    for occ in manifest.get('occupations') or []:
        eid=(occ.get('escoId') or '').lower()
        row=by_id.get(eid)
        if not row:
            print('skip missing export', occ.get('titleNl'), eid)
            continue
        blocks=[]
        for b in row.get('blocks') or []:
            blocks.append({
                'key': b.get('key') or b.get('Key',''),
                'label': b.get('label') or b.get('Label',''),
                'text': b.get('text') or b.get('Text',''),
            })
        if not blocks:
            for key,label in [('start','07:00 Start'),('morning','09:00 Ochtend'),('pause','12:00 Pauze'),('afternoon','14:00 Middag'),('close','17:00 Afronden')]:
                field={'start':'morning','morning':'morning','pause':'midday','afternoon':'afternoon','close':'closing'}.get(key,key)
                txt=(row.get(field) or row.get(field.capitalize()) or '').strip()
                if txt:
                    blocks.append({'key':key,'label':label,'text':txt})
        title_nl=row.get('titleNl') or row.get('TitleNl') or occ.get('titleNl') or ''
        trials.append({
            'escoId': eid,
            'slug': _slug(title_nl),
            'dreamTitle': _title_case_nl(title_nl),
            'categorySlug': occ.get('categorySlug') or 'zorg-dieren',
            'blocks': blocks,
            'skills': row.get('skills') or row.get('Skills') or [],
            'variesNote': row.get('variesNote') or row.get('VariesNote') or '',
        })
    return trials

TRIAL_DAYS=load_trial_manifest()

def job_day(name,k,d):
    w=W[k]; img,pos=RESULT_BAND[k]; last=len(w['nodes'])-1
    title=d.get('dreamTitle','Beroep')
    cat=d.get('categorySlug') or 'zorg-dieren'
    scene=day_scene_art(cat)
    hero=day_hero_art_img(cat)
    blocks=''
    for b in d['blocks']:
        key=b['key']; lab=b['label']; txt=b['text']
        blocks+=f'''<li class="day-block" data-moment="{key}"><div class="day-block-inner"><span class="day-scene day-scene-w-{k}"><img class="day-scene-img" src="{scene}" alt="" width="400" height="400"></span><div><h2>{lab}</h2><p>{txt}</p></div>
<span class="dspeak"><button type="button" class="speak" aria-label="Voorlezen">{SPK}</button></span></div></li>'''
    facts=''
    skills=d.get('skills') or []
    if skills:
        pills=''.join(f'<li class="day-pill">{s}</li>' for s in skills[:6])
        facts=f'<section class="day-facts" aria-labelledby="day-skills-title"><h2 id="day-skills-title">Wat je vaak doet</h2><ul class="day-cloud">{pills}</ul></section>'
    varies=(d.get('variesNote') or '').strip()
    if varies:
        varies='Overal is het net even anders. Dat is normaal.'
    varies_html=f'<p class="day-varies">{varies}</p>' if varies else ''
    coach='Ik loop met je mee door een gewone werkdag. Neem je tijd.'
    body=f'''{top(w['small'])}{bandcss('jb',img,320,220,pos,pos)}
<section class="band jb dark" style="padding-top:32px">{journey(w,last,'<b>Droombaan</b> · zo ziet een dag eruit')}</section>
<div class="wrap" style="position:relative;z-index:4;margin-top:-20px"><div class="q-layout"><div class="card card-deco">{world_deco(k)}
<div class="qmeta"><span class="kicker">Een werkdag</span><span>·</span><span>{title}</span></div>
<h1 class="island-lead" style="margin-top:4px">Een dag als {title}</h1>
<p class="day-disclaimer">Zo kan een dag eruitzien. Overal is het een beetje anders.</p>
<figure class="day-job-hero day-job-hero-w-{k}"><img src="{hero}" alt="" width="960" height="380"></figure>
<div class="qrow"><button class="speak on">{SPK}</button><span class="speak-label">Lobsy leest de uitleg voor</span></div>
<ol class="day-timeline">{blocks}</ol>{facts}{varies_html}
<div class="qfoot" style="flex-wrap:wrap"><button class="btn btn-ghost">{BACK}Terug naar de beroepen</button>
<button class="btn btn-main" style="margin-left:auto">Dit beroep kies ik {NEXT}</button></div></div>
<aside class="d-only" style="padding-top:10px">{bubble(coach,'b')}<img class="lob" src="img/kreeft-kijker.png" alt="Lobsy" style="margin-top:22px"></aside>
<div class="m-only"><div class="guide"><div class="ring"></div>{bubble(coach,'l')}</div></div></div></div>{FOOT}'''
    page(name,body,w['cls'])

if __name__ == '__main__':
    keuze('00-kies-je-wereld')
    for i,k in enumerate(['zee','tuin','ster']):
        b=1+i*3; nm={'zee':'zee','tuin':'eilandtuin','ster':'sterrenkust'}[k]
        route(f'{b:02d}-{nm}-route',k); question(f'{b+1:02d}-{nm}-vraag',k); intro(f'{b+2:02d}-{nm}-puzzel-intro',k)
        island_page(f'11a-{nm}-pauze-leuk',k,1); island_page(f'11b-{nm}-pauze-niet-leuk',k,2)
        result(f'10-{nm}-result',k); job_pick(f'12-{nm}-beroep-kiezen',k,none_selected=True)
        for day in TRIAL_DAYS:
            job_day(f'13-{nm}-beroep-dag-{day["slug"]}', k, day)
    print('ok', len(TRIAL_DAYS), 'trial days')
