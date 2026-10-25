import html
CHK='<svg viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l5 5 9-10"/></svg>'
I={
 'team':'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="8" cy="8" r="3"/><circle cx="16.5" cy="9" r="2.5"/><path d="M2.5 19c.6-3.2 2.8-5 5.5-5s4.9 1.8 5.5 5M13.5 15.2c.9-.8 1.9-1.2 3-1.2 2.3 0 4 1.6 4.5 4.5"/></svg>',
 'help':'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20s-7-4.4-7-10a4 4 0 017-2.6A4 4 0 0119 10c0 5.6-7 10-7 10z"/></svg>',
 'trust':'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3l8 3v6c0 4.5-3.4 7.8-8 9-4.6-1.2-8-4.5-8-9V6z"/><path d="M8.5 12l2.5 2.5 4.5-5"/></svg>',
 'paw':'<svg viewBox="0 0 24 24" fill="currentColor"><ellipse cx="7" cy="9" rx="1.8" ry="2.4"/><ellipse cx="11" cy="6.5" rx="1.8" ry="2.4"/><ellipse cx="15.5" cy="7" rx="1.8" ry="2.4"/><ellipse cx="18.5" cy="11" rx="1.6" ry="2.1"/><path d="M12.5 11.5c-3 0-5.5 3.5-5.5 6 0 1.6 1.4 2.4 3 2 1.2-.3 1.8-.6 2.5-.6s1.3.3 2.5.6c1.6.4 3-.4 3-2 0-2.5-2.5-6-5.5-6z"/></svg>',
 'steth':'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M6 3v6a4 4 0 008 0V3M10 13v2a5 5 0 0010 0v-2"/><circle cx="20" cy="11" r="2"/></svg>',
 'leaf':'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 19c0-8 5-14 15-14 0 10-6 15-14 15"/><path d="M5 19l8-8"/></svg>',
 'okc':'<svg viewBox="0 0 24 24" fill="none" stroke="#2f7d5b" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l5 5 9-10"/></svg>',
 'arrow':'<svg viewBox="0 0 24 24" fill="none" stroke="#d4532f" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12h14M13 6l6 6-6 6"/></svg>',
 'shield':'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3l8 3v6c0 4.5-3.4 7.8-8 9-4.6-1.2-8-4.5-8-9V6z"/><path d="M8.5 12l2.5 2.5 4.5-5"/></svg>',
}
STAR='<svg viewBox="0 0 24 24" width="12" height="12" fill="white"><path d="M12 2.5l2.9 6.1 6.6.8-4.9 4.6 1.3 6.6L12 17.3l-5.9 3.3 1.3-6.6-4.9-4.6 6.6-.8z"/></svg>'
WORLDS=[('rif','Koraalrif','Zo ben jij','In balans'),('grot','Schatgrot','Dit doe je graag','Onderzoeken en helpen'),('vuurtoren','Vuurtoren','Dit vind je belangrijk','Zelf kiezen'),('lagune','Lagune','Hier voel je je thuis','Zelfstandig, vrij')]
FULL=dict(
 name='Noor',
 story='Jij bent in evenwicht: soms stil, soms actief. Je zoekt uit hoe iets werkt en helpt anderen graag. Belangrijk voor jou: zelf kiezen wat je doet. Je voelt je thuis op een plek waar je zelf mag kiezen. Je houdt van dieren en tekenen.',
 likes=['Dieren','Tekenen','Natuur'],like_own='Paardrijden',dis=['Lang stilzitten','Druk en lawaai'],dis_own='Spinnen',
 strengths=[('team','Samenwerken','Ik luister en denk mee.'),('help','Anderen helpen','Ik zie het als iemand vastzit.'),('trust','Betrouwbaar','Op mijn afspraken kan je rekenen.')],
 dream='Dierenarts',
 route=[('Nu','Groep 8','Kies straks op de middelbare school biologie en scheikunde.'),('Straks','Havo of vwo','Diergeneeskunde vraagt vwo met biologie en scheikunde. Of eerst havo en dan een jaar hbo.'),('Daarna','Diergeneeskunde studeren','Aan de universiteit, ongeveer 6 jaar.'),('Doel','Dierenarts','In een praktijk, een dierentuin of op de boerderij.')],
 have=['Je houdt van dieren'],need=['Oefen met zorgen voor een dier of plant','Kies straks vakken waarin je mag uitzoeken'],
 alt='Zit je straks op het vmbo? Via mbo Dierenartsassistent (niveau 4) kan je ook verder.',
 jobs=[('paw','Zorg &amp; dieren','Dierenverzorger','Je zorgt elke dag voor dieren: eten geven, verzorgen en kijken of ze gezond zijn.'),('steth','Onderzoek','Dierenarts','Je helpt dieren beter te worden. Je onderzoekt wat er mis is en werkt met mensen die zorgen.'),('leaf','Onderzoek','Bioloog','Je onderzoekt hoe planten en dieren leven, buiten in de natuur of in een lab.')],
 fit='dieren, tekenen',
 discover=['Hoe ziet een dag bij de dierenarts eruit?','Welke vakken horen bij biologie op de middelbare school?','Ben ik ook goed in iets maken met mijn handen?'],
)
SPARSE=dict(FULL,name='',likes=['Dieren'],like_own='',dis=[],dis_own='',strengths=FULL['strengths'][:1],dream='',jobs=FULL['jobs'][:1],fit='dieren',discover=[],
 story='Jij bent in evenwicht: soms stil, soms actief. Je zoekt graag uit hoe iets werkt.')
NOTES='<div class="card" style="margin-top:5mm;border-style:dashed;background:transparent"><h3 style="font-size:11pt;color:var(--navy)">Ruimte voor jou</h3><p class="muted" style="font-size:7.8pt">Schrijf of teken hier wat je onderweg over jezelf ontdekte.</p><div class="empty" style="height:30mm"></div></div>'
def top():
    return '''<header class="top"><div class="brand"><img src="img/g78-embleem.png" alt="">Lobsy<small>Expeditie · DNA-paspoort</small></div><div class="sp"></div>
<div class="meta"><b>Groep 8 · 8A</b> · OBS De Kompasroos (fictief)<br>14 okt 2026<span class="code">K7Q-M2P</span></div></header>'''
def foot(n):
    return f'''<footer class="foot"><span class="shi">{I['shield']}</span><div class="sp">Geen cijfer en geen schooladvies. Dit paspoort is om te ontdekken, voor jou, je ouders en je leraar.<br>Lobsy bewaart geen namen. Je antwoorden worden na dit schooljaar gewist.</div><div class="r"><b>lobsy.nl</b>pagina {n}/2</div></footer>'''
def build(d,fn):
    worlds=''.join(f'<div class="w"><span class="ok">{CHK}</span><img src="img/g78-{k}.jpg" alt=""><div class="t"><div class="k">{sub}</div><h3>{val}</h3><div class="n">{t}</div></div></div>' for k,t,sub,val in WORLDS)
    likes=''.join(f'<span class="chip on">{x}</span>' for x in d['likes'])
    dis=''.join(f'<span class="chip no">{x}</span>' for x in d['dis']) or '<span class="muted" style="font-size:7.8pt">Nog niets gekozen. Dat mag ook.</span>'
    lo=f'<div class="own">Mijn eigen woord:<b>{d["like_own"]}</b></div>' if d['like_own'] else '<div class="own">Mijn eigen woord: <span style="display:inline-block;width:40mm;border-bottom:1px solid #cbbd9f"></span></div>'
    do=f'<div class="own">Mijn eigen woord:<b>{d["dis_own"]}</b></div>' if d['dis_own'] else '<div class="own">Mijn eigen woord: <span style="display:inline-block;width:40mm;border-bottom:1px solid #cbbd9f"></span></div>'
    st=''.join(f'<div class="card s"><span class="ic">{I[i]}</span><div><b>{t}</b><span>{s}</span></div></div>' for i,t,s in d['strengths'])
    if len(d['strengths'])<3:
        st+='<div class="card s" style="border-style:dashed;background:transparent"><div style="width:100%"><b class="muted" style="font-weight:500">Hier ben ik ook goed in:</b><div class="wl" style="margin-top:2mm"></div></div></div>'
    p1=f'''<section class="page">{top()}
<div class="hero"><div class="badge"><div class="sh">{CHK}</div><b>Schild compleet</b><span>Alle 4 werelden ontdekt</span></div></div>
<div class="title"><div class="kicker">Expeditie klaar · alle 60 vragen</div><h1>Mijn DNA-paspoort</h1>
<div class="nameline">Dit paspoort is van<span class="ln">{d['name']}</span></div><div class="nameline-note">Je naam schrijf je zelf op. Lobsy bewaart geen namen.</div></div>
<div class="wrap">
<h2>Dit ben ik!</h2>
<div class="card story"><div><div class="who">Wat Lobsy onderweg leerde</div><p>{d['story']}</p></div><img src="img/g78-kreeft-kijker.png" alt=""></div>
<div class="worlds">{worlds}</div>
<div class="two" style="margin-top:5mm"><div class="card like"><h3>Jaaa, leuk!</h3><div class="chips">{likes}</div>{lo}</div>
<div class="card dis"><h3>Liever niet</h3><div class="chips">{dis}</div>{do}</div></div>
<h2>Hier ben ik goed in</h2><div class="str">{st}</div>
</div>{foot(1)}</section>'''
    if d['dream']:
        route=''.join(f'<div class="st{" goal" if i==3 else ""}"><i>{STAR if i==3 else i+1}</i><div class="k">{k}</div><b>{t}{"!" if i==3 else ""}</b><span>{s}</span></div>' for i,(k,t,s) in enumerate(d['route']))
        have=''.join(f'<li>{x}</li>' for x in d['have']); need=''.join(f'<li>{x}</li>' for x in d['need'])
        dream=f'''<div class="card dream"><div><div class="hand">Hier droom ik van...</div><h3 class="job">{d['dream']}</h3>
<div class="route">{route}</div>
<div class="have"><div><h4>{I['okc']}Dit heb je al</h4><ul>{have}</ul></div><div><h4>{I['arrow']}Wat heb je nodig?</h4><ul>{need}</ul></div></div>
<div class="alt">{d['alt']}</div></div>
<div class="lob"><div class="bub">Dierenarts? Sterke keuze. Dit is je route.</div><img src="img/g78-kreeft-kaart.png" alt=""></div></div>'''
    else:
        dream='''<div class="card dream"><div><div class="hand">Hier droom ik van...</div><h3 class="job muted" style="font-size:12pt;font-family:Lexend;font-weight:500;margin-top:2mm">Nog geen droombaan gekozen. Dat is helemaal goed.</h3>
<p style="font-size:8.2pt;line-height:1.5">Kies er een in de Droombaan-checker, of schrijf hier zelf op wat je later misschien wilt worden. Praat erover met je ouders of je leraar.</p>
<div class="empty"></div></div>
<div class="lob"><div class="bub">Weet je het nog niet? Dan ontdek je het onderweg.</div><img src="img/g78-kreeft-kaart.png" alt=""></div></div>'''
    jobs=''.join(f'<div class="card job"><span class="ico">{I[i]}</span><div class="k">{k}</div><h3>{t}</h3><p>{s}</p><div class="fit"><b>Past bij:</b> {d["fit"]}</div></div>' for i,k,t,s in d['jobs'])
    if len(d['jobs'])<3:
        jobs+=f'<div class="card job" style="border-style:dashed;background:transparent;grid-column:span {3-len(d["jobs"])}"><div class="k">Nog meer ontdekken</div><p class="muted" style="margin-top:1.4mm">Op de Droombaan-pagina vind je meer beroepen. Schrijf er hier een op die je wilt bekijken:</p><div class="wl" style="margin-top:3mm"></div></div>'
    if d['discover']:
        disc=''.join(f'<li><span class="box"></span>{x}</li>' for x in d['discover'])
    else:
        disc=''.join('<li><span class="box"></span><span class="wl"></span></li>' for _ in range(3))
    p2=f'''<section class="page">{top()}
<div class="band"></div>
<div class="p2title"><div class="card"><div class="kicker">Mijn volgende stap</div><h1>Droombaan en ontdekken</h1></div></div>
<div class="wrap">
<h2>Mijn droombaan</h2>{dream}
<h2>Mijn beroepenkaarten <small>om te ontdekken, geen vacatures</small></h2><div class="jobs">{jobs}</div>
<div class="two" style="margin-top:5mm"><div class="card disc"><h3>Dit wil ik nog ontdekken</h3><ul>{disc}</ul></div>
<div class="card tip"><h3>Tip voor thuis en in de klas</h3><p>Ga samen naar een kinderboerderij of dierenasiel en vraag wat de verzorgers doen.</p><p>Vraag wat het leukst was. Er is geen goed of fout!</p></div></div>
{NOTES if not d['dream'] else ''}</div>{foot(2)}</section>'''
    doc=f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><title>DNA-paspoort groep 7/8</title><link rel="stylesheet" href="g78.css"></head><body>{p1}{p2}</body></html>'
    open('src/'+fn,'w').write(doc)
build(FULL,'groep78-noor.html'); build(SPARSE,'groep78-sparse.html')
