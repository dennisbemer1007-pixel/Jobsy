import math
RIA=[('Maken','var(--r)','#9c7a45'),('Onderzoeken','var(--i)','#33505f'),('Creatief','var(--a)','#b4533e'),('Helpen','var(--s)','#4f7d64'),('Leiden','var(--e)','#d1a14a'),('Ordenen','var(--c)','#8796a3')]
def I(n):
    P={'home':'<path d="M3 11.5 12 4l9 7.5"/><path d="M5 10v10h14V10"/>','todo':'<path d="M9 11l2 2 4-4"/><rect x="4" y="4" width="16" height="16" rx="3"/>',
    'cmp':'<path d="M4 20V10M10 20V4M16 20v-7M22 20H2"/>','star':'<path d="m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z"/>',
    'cls':'<rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 10h18M8 5v14"/>','bulb':'<path d="M9 18h6M10 21h4"/><path d="M12 3a6 6 0 0 0-3.5 10.9V16h7v-2.1A6 6 0 0 0 12 3z"/>',
    'team':'<circle cx="9" cy="8" r="3.2"/><path d="M3 20a6 6 0 0 1 12 0"/><circle cx="17" cy="9" r="2.5"/><path d="M15.5 14.2A5 5 0 0 1 21 19"/>','lock':'<rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/>',
    'book':'<path d="M4 5a2 2 0 0 1 2-2h13v16H6a2 2 0 0 0-2 2z"/><path d="M4 19V5"/>','school':'<path d="M3 10 12 5l9 5-9 5z"/><path d="M7 12.5V17c3 2 7 2 10 0v-4.5"/>',
    'cal':'<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/>','user':'<circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/>',
    'dl':'<path d="M12 4v11M7 10l5 5 5-5M5 20h14"/>','print':'<path d="M7 9V3h10v6"/><rect x="3" y="9" width="18" height="8" rx="2"/><path d="M7 14h10v7H7z"/>',
    'plus':'<path d="M12 5v14M5 12h14"/>','chev':'<path d="m6 9 6 6 6-6"/>','info':'<circle cx="12" cy="12" r="9"/><path d="M12 11v6M12 7.5v.5"/>',
    'arrow':'<path d="M5 12h14M13 6l6 6-6 6"/>','up':'<path d="M7 17 17 7M9 7h8v8"/>','menu':'<path d="M4 7h16M4 12h16M4 17h16"/>','key':'<circle cx="8" cy="15" r="4"/><path d="m11 12 9-9M17 6l3 3"/>',
    'chat':'<path d="M4 5h16v11H9l-5 4z"/>','copy':'<rect x="8" y="8" width="12" height="12" rx="2"/><path d="M16 8V5a1 1 0 0 0-1-1H5a1 1 0 0 0-1 1v10a1 1 0 0 0 1 1h3"/>','eye':'<path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/>',
    'clock':'<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>','map':'<path d="M9 4 3 6v14l6-2 6 2 6-2V4l-6 2z"/><path d="M9 4v14M15 6v14"/>'}
    return f'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">{P[n]}</svg>'
VV=f'<a class="vv" href="#">{I("up")}Veel vraag in jouw regio</a>'
DEMO='<span class="demo">Voorbeelddata</span>'
def top(role):
    if role=='school': r=('DS','Schoolbeheerder','Demo Schoolbeheerder')
    else: r=('SV','Leraar','Demo Leraar S. de Vries')
    return f'''<header class="top"><a class="brand" href="#"><img src="img/merk.png" alt="">Lobsy<small>voor scholen</small></a>
<div class="tr"><div class="schoolsel">Demo College Lobsy · 2026-2027{I("chev")}</div><div class="role"><i>{r[0]}</i><span>{r[2]}<small>{r[1]}</small></span></div><a class="lo" href="#">Uitloggen</a><button class="menu-b" aria-label="Menu">{I("menu")}</button></div></header>'''
SCHOOL_NAV=[('Overzicht',[('home','Dashboard','01'),('todo','Te doen','x',3)]),('Inzicht',[('cmp','Vergelijking klassen','02'),('star','Beroepen en droombanen','03'),('bulb','Inzichten voor gesprek','05')]),
 ('Leerlingen',[('cls','Klassen en codes','04')]),('School',[('team','Leraren','x'),('lock','Privacy en ouders','x'),('book','Lesbrief en materiaal','x')])]
TEACH_NAV=[('Klas 8B',[('home','Klasoverzicht','06'),('cmp','Vergelijking','07'),('key','Leerlingcodes','08'),('bulb','Lesideeën','09')]),('In de les',[('cal','Testvenster','10'),('print','Codelijst en lesbrief','x')]),('Mijn klassen',[('cls','Klas 8A','x'),('cls','Klas 8B','k'),('cls','Plusklas','x')])]
def side(nav,act,help):
    h='<nav class="side">'
    for g,items in nav:
        h+=f'<div class="g">{g}</div>'
        for it in items:
            ic,lb,k=it[:3]; on=' class="on"' if k==act else (' class="sel"' if k=='k' else '')
            n=f'<span class="n">{it[3]}</span>' if len(it)>3 else ''
            h+=f'<a href="#"{on}>{I(ic)}{lb}{n}</a>'
    return h+f'<div class="help"><b>Privacy voorop</b>{help}</div></nav>'
def mnav(nav,act,ctx):
    h=f'<nav class="mnav">'+(f'<div class="ctx">{ctx}</div>' if ctx else '')
    for g,items in nav[:2]:
        for it in items:
            if it[2]=='x' and len(it)<4: continue
            on=' class="on"' if it[2]==act else ''
            h+=f'<a href="#"{on}>{it[1]}</a>'
    return h+'<a href="#">Meer</a></nav>'
def page(fn,role,act,hdr,body,title):
    nav=SCHOOL_NAV if role=='school' else TEACH_NAV
    help='Lobsy kent geen namen, alleen codes. Groepen kleiner dan 5 leerlingen tonen we niet.'
    ctx='' if role=='school' else 'Klas 8B · groep 8'
    html=f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{title}</title><link rel="stylesheet" href="dash.css">
<style>.side a.sel{{background:transparent;color:var(--acc);font-weight:600}}.side a.sel svg{{color:var(--acc)}}</style></head><body>
{top(role)}<img class="hdr" src="img/hdr-{hdr}-desk.jpg" alt=""><img class="hdr-m" src="img/hdr-{hdr}-mob.jpg" alt="">
<div class="shell">{side(nav,act,help)}<main>{mnav(nav,act,ctx)}{body}</main></div>
<div class="foot"><a href="#">Privacyverklaring</a><a href="#">Toegankelijkheid</a><a href="#">Hulp voor scholen</a></div></body></html>'''
    open(f'src/{fn}.html','w').write(html)
def ph(crumb,h1,lead,acts=''):
    return f'<div class="crumb">{crumb}</div><div class="ph"><div><h1>{h1}</h1><p>{lead}</p></div><div class="acts">{acts}</div></div>'
def card(title,body,sub='',right='',cls=''):
    s=f'<div class="s">{sub}</div>' if sub else ''
    return f'<section class="card {cls}"><div class="ch"><div><h2>{title}</h2>{s}</div>{right}</div>{body}</section>'
def kpi(l,v,d,col=None,small=''):
    i=f'<i style="background:{col}"></i>' if col else ''
    return f'<div class="card kpi"><div class="l">{i}{l}</div><div class="v">{v}{small}</div><div class="d">{d}</div></div>'
def btn(t,ic=None,g=False,sm=False):
    return f'<a class="btn{" g" if g else ""}{" sm" if sm else ""}" href="#">{I(ic) if ic else ""}{t}</a>'
def legend(items):
    return '<div class="legend">'+''.join(f'<span><i class="{c2}" style="background:{c}"></i>{t}</span>' for t,c,*r in items for c2 in [r[0] if r else ''])+'</div>'
def stackrow(lb,sub,segs,total,tx,masked=None):
    if masked: b=f'<div class="masked">{masked}</div>'
    else: b='<div class="bar">'+''.join(f'<i style="width:{v/total*100:.2f}%;background:{c}"></i>' for v,c in segs)+'</div>'
    return f'<div class="sr"><div class="lb"><b>{lb}</b><small>{sub}</small></div>{b}<div class="tx">{tx}</div></div>'
def pctrow(lb,sub,vals,masked=None):
    if masked: b=f'<div class="masked">{masked}</div>'
    else: b='<div class="pbar">'+''.join(f'<i style="width:{v}%;background:{RIA[k][1]}">{str(v) if v>=9 else ""}</i>' for k,v in enumerate(vals) if v>0)+'</div>'
    return f'<div class="pr"><div class="lb">{lb}<small>{sub}</small></div>{b}</div>'
def hbars(items,mx,col='var(--acc)',rank=True):
    h=''
    for k,it in enumerate(items):
        nm,v=it[0],it[1]; ex=it[2] if len(it)>2 else ''; vs=it[3] if len(it)>3 else str(v)
        h+=f'<div class="hb"><div class="rk">{k+1 if rank else ""}</div><div class="nm">{nm}{ex}</div><div class="vl">{vs}</div><div class="tr2"><i style="width:{v/mx*100:.1f}%;background:{col}"></i></div></div>'
    return h
def line_chart(labels,series,ymax,step,w=640,h=230,ylab=''):
    L,R,T,B=40,30,16,30; pw,phh=w-L-R,h-T-B
    def X(i): return L+pw*i/(len(labels)-1)
    def Y(v): return T+phh*(1-v/ymax)
    s=f'<svg viewBox="0 0 {w} {h}" font-family="IBM Plex Sans" font-size="11.5">'
    for v in range(0,ymax+1,step):
        s+=f'<line x1="{L}" x2="{w-R}" y1="{Y(v):.1f}" y2="{Y(v):.1f}" stroke="#ece8e1"/><text x="{L-8}" y="{Y(v)+4:.1f}" text-anchor="end" fill="#8a949b">{v}</text>'
    for i,lb in enumerate(labels): s+=f'<text x="{X(i):.1f}" y="{h-8}" text-anchor="middle" fill="#8a949b">{lb}</text>'
    for name,col,vals,dash,area in series:
        pts=[(X(i),Y(v)) for i,v in enumerate(vals) if v is not None]
        if area: s+=f'<path d="M{pts[0][0]:.1f},{Y(0):.1f} '+' '.join(f'L{x:.1f},{y:.1f}' for x,y in pts)+f' L{pts[-1][0]:.1f},{Y(0):.1f}Z" fill="{col}" opacity=".10"/>'
        s+=f'<polyline points="{" ".join(f"{x:.1f},{y:.1f}" for x,y in pts)}" fill="none" stroke="{col}" stroke-width="2.4" {"stroke-dasharray=\"5 4\"" if dash else ""} stroke-linejoin="round"/>'
        if not dash:
            for x,y in pts: s+=f'<circle cx="{x:.1f}" cy="{y:.1f}" r="3.4" fill="#fff" stroke="{col}" stroke-width="2"/>'
            x,y=pts[-1]; s+=f'<text x="{x-6:.1f}" y="{y-9:.1f}" text-anchor="end" font-weight="600" fill="{col}" font-size="12.5">{vals[len(pts)-1]}</text>'
    return s+'</svg>'
def radar(axes,series,size=320,mx=35):
    c=size/2; r=size/2-92
    s=f'<svg viewBox="0 0 {size} {size}" font-family="IBM Plex Sans" font-size="14">'
    n=len(axes)
    def P(k,v): a=-math.pi/2+2*math.pi*k/n; return c+r*v/mx*math.cos(a), c+r*v/mx*math.sin(a)
    for lv in (mx/3,2*mx/3,mx):
        s+='<polygon points="'+' '.join(f'{P(k,lv)[0]:.1f},{P(k,lv)[1]:.1f}' for k in range(n))+'" fill="none" stroke="#e6e2da"/>'
    for k,ax in enumerate(axes):
        x,y=P(k,mx); s+=f'<line x1="{c}" y1="{c}" x2="{x:.1f}" y2="{y:.1f}" stroke="#e6e2da"/>'
        lx,ly=P(k,mx*1.16); anc='middle' if abs(lx-c)<10 else ('start' if lx>c else 'end')
        s+=f'<text x="{lx:.1f}" y="{ly+4:.1f}" text-anchor="{anc}" fill="#3a464f" font-weight="500">{ax}</text>'
    for name,col,vals,dash in series:
        pts=' '.join(f'{P(k,v)[0]:.1f},{P(k,v)[1]:.1f}' for k,v in enumerate(vals))
        s+=f'<polygon points="{pts}" fill="{col if not dash else "none"}" fill-opacity=".14" stroke="{col}" stroke-width="2.2" {"stroke-dasharray=\"5 4\"" if dash else ""}/>'
    return s+'</svg>'
def svgw(d,m=None): return f'<div class="svgw ch-d">{d}</div><div class="svgw ch-m">{m or d}</div>' if m else f'<div class="svgw">{d}</div>'
