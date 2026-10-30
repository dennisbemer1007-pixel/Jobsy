CHK='<svg viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l5 5 9-10"/></svg>'
SH='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3l8 3v6c0 4.5-3.4 7.8-8 9-4.6-1.2-8-4.5-8-9V6z"/><path d="M8.5 12l2.5 2.5 4.5-5"/></svg>'
LAYERS=[('rif','Zo ben jij','In balans','Soms stil, soms actief.'),('grot','Dit doe je graag','Onderzoeken en helpen','Je wilt weten hoe het zit.'),('toren','Dit vind je belangrijk','Zelf kiezen','Je bepaalt graag zelf je aanpak.'),('lagune','Hier voel je je thuis','Zelfstandig, vrij','Ruimte om je eigen werk te doen.')]
FULL=dict(name='Sara Bakker',ini='SB',tag='Zorgzame onderzoeker. Wil weten hoe het zit en helpt graag.',
 facts=[('Niveau','Havo 4','examen in 2028, daarna hbo'),('Leukste vakken','Biologie · Tekenen · Engels',''),('Bijbaan','Za + wo-middag','max. 8 uur per week, overdag'),('Vervoer','Fiets','max. 20 min vanaf huis')],
 likes=['Dieren','Tekenen','Natuur'],dis=['Lang stilzitten','Druk en lawaai'],
 strengths=[('Samenwerken','Je luistert en denkt mee.'),('Anderen helpen','Je ziet het als iemand vastzit.'),('Betrouwbaar','Op jouw afspraken kunnen anderen rekenen.')],
 quote='Ik wil graag ontdekken of zorg echt bij me past.',
 sectors=[('Dieren &amp; natuur',['Houdt van dieren en buiten zijn','Zorgzaam én nieuwsgierig','Stages in dierentuin, boerderij of natuur']),('Zorg &amp; welzijn',['Helpt graag, blijft rustig','Werkt fijn in een vast team','Verpleging, begeleiding of welzijn verkennen']),('Onderzoek &amp; lab',['Wil weten hoe iets werkt','Precies en zorgvuldig','Biologie, lab of data verkennen'])],
 jobs=[('Snuffelstage dierenkliniek','kort kijken, geen vacature','Om te ontdekken hoe een dierenarts werkt. Vaak vanaf 15 jaar met begeleiding.'),('Hulp op een kinderboerderij','dieren en buiten','Past bij zorgzaam zijn en dieren: rustig meelopen en vragen stellen.'),('Assistent bij een sportclub','team en buiten','Leuk als je van sport houdt: meehelpen met training of op de wedstrijddag.')],
 dream='Dierenarts',steps=[('Nu: klas 4 havo','Kies straks biologie en scheikunde.'),('Vwo of hbo-propedeuse','Een havo-diploma geeft geen directe toegang. Kies vwo met biologie en scheikunde, of ga via een hbo-propedeuse.'),('Diergeneeskunde studeren','Na vwo of na een hbo-propedeuse. Ongeveer 6 jaar.'),('Doel: Dierenarts!','In een praktijk, dierentuin of op de boerderij.')],
 learn=['Meer biologie-praktijk','Rustig blijven in de klas'])
SPARSE=dict(FULL,name='',ini='',facts=[('Niveau','Havo 4','examen in 2028, daarna hbo'),('Leukste vakken','Biologie',''),('Bijbaan','Nog niet ingevuld',''),('Vervoer','Nog niet ingevuld','')],
 likes=['Dieren'],dis=[],strengths=FULL['strengths'][:1],quote='',sectors=FULL['sectors'][:1],jobs=[],dream='',learn=['Meer biologie-praktijk'])
NOTES='<h2>Om te bespreken met je mentor</h2><div class="card"><div style="font-size:7.6pt;color:var(--soft)">Wat wil je nog uitzoeken? Schrijf het hier op en neem het mee naar je gesprek.</div><div class="wl"></div><div class="wl"></div><div class="wl"></div><div class="wl"></div></div><div class="card guide" style="margin-top:2.6mm"><img src="img/vo-gids.png" alt=""><span>Je paspoort groeit mee. Vul je profiel aan in Lobsy, dan zie je hier meer richtingen en je studieroute.</span></div>'
def top():
    return f'<header class="top"><div class="brand"><img src="img/vo-merk.png" alt="">Lobsy<small>DNA-paspoort · middelbare school</small></div><div class="sp"></div><div class="stat"><div><b>Alle 4 werelden afgerond</b>Lobsy voor scholen · 14 okt 2026</div><span class="ck">{CHK}</span></div></header>'
def foot(n,t):
    return f'<footer class="foot">{SH}<div class="sp">Geen cijfer en geen schooladvies. Dit paspoort is om te ontdekken.<br>Lobsy bewaart geen namen. Je antwoorden worden na dit schooljaar gewist.</div><div class="r"><b>lobsy.nl</b>14 okt 2026 · pagina {n}/{t}</div></footer>'
def blocks(d):
    B={}
    chips='<div class="chips"><span class="chip">Havo 4</span><span class="chip">Profiel Natuur &amp; Gezondheid</span><span class="chip">Amersfoort (school)</span><span class="chip">Code K7Q-M2P</span></div>'
    if d['name']:
        idb=f'<div class="id"><div class="mono">{d["ini"]}</div><div><div class="kick">Mijn DNA-paspoort</div><h1>{d["name"]}</h1><div class="tag">{d["tag"]}</div>{chips}</div></div>'
    else:
        idb=f'<div class="id"><div class="mono empty">jouw<br>naam</div><div><div class="kick">Mijn DNA-paspoort</div><div style="font-size:7.6pt;color:var(--soft);margin-top:1mm">Naam op je paspoort (optioneel, alleen op de PDF)</div><span class="nameln"></span><div class="tag" style="margin-top:1.6mm">{d["tag"]}</div>{chips}</div></div>'
    B['hero']=f'<div class="hero"></div>{idb}'
    B['facts']='<div class="facts">'+''.join(f'<div class="card"><div class="k">{k}</div><b>{v}</b>{f"<span>{s}</span>" if s else ""}</div>' for k,v,s in d['facts'])+'</div>'
    B['dna']='<h2>Mijn DNA in 4 lagen</h2><div class="dna">'+''.join(f'<div class="l"><img src="img/vo-th-{i}.jpg" alt=""><div><div class="kick">{k}</div><h3>{v}</h3><span>{s}</span></div></div>' for i,k,v,s in LAYERS)+'</div>'
    dis=''.join(f'<span class="chip cor">{x}</span>' for x in d['dis']) or '<span style="font-size:7.6pt;color:var(--soft)">Nog niets gekozen.</span>'
    B['int']='<h2>Mijn interesses</h2><div class="two"><div class="card"><div class="sub">Dit vind ik leuk</div><div class="chips" style="margin:0">'+''.join(f'<span class="chip sea">{x}</span>' for x in d['likes'])+f'</div></div><div class="card"><div class="sub c">Liever niet</div><div class="chips" style="margin:0">{dis}</div></div></div>'
    B['str']='<h2>Sterke punten</h2><div class="str">'+''.join(f'<div class="card"><b>{t}</b><span>{s}</span></div>' for t,s in d['strengths'])+'</div>'
    B['quote']=(f'<h2>In mijn eigen woorden</h2><div class="card quote"><span class="q">“</span><div><p>{d["quote"]}</p><small>Geschreven door de leerling, niet aangepast door Lobsy</small></div></div>' if d['quote'] else
      '<h2>In mijn eigen woorden</h2><div class="card"><div style="font-size:7.6pt;color:var(--soft)">Nog niets geschreven. Wil je het zelf invullen?</div><div class="wl"></div><div class="wl"></div></div>')
    B['sec']='<h2>Sectoren die bij mij passen</h2><div class="sec">'+''.join(f'<div class="card"><h3>{t}</h3><ul>'+''.join(f'<li>{x}</li>' for x in li)+'</ul></div>' for t,li in d['sectors'])+'</div>'
    if d['jobs']:
        B['jobs']='<h2>Bijbanen en stages die bij mij passen <small>types werk, geen vacatures</small></h2><div class="sec jobs">'+''.join(f'<div class="card"><h3>{t}</h3><div class="meta">{m}</div><div class="tags"><span>zaterdag</span><span>vanaf 15 jaar</span></div><p><b>Waarom:</b> {w}</p></div>' for t,m,w in d['jobs'])+'</div>'
    else:
        B['jobs']='<h2>Bijbanen en stages die bij mij passen <small>types werk, geen vacatures</small></h2><div class="empty-note">Nog geen passende bijbaan of stage binnen jouw leeftijd en regels. Vul je profiel aan of kom later terug.</div>'
    learn='<div class="card"><h3 style="font-size:10.5pt;margin-bottom:1.6mm">Wat ik wil leren</h3><ul class="ul">'+''.join(f'<li>{x}</li>' for x in d['learn'])+'</ul></div>'
    if d['dream']:
        st=''.join(f'<li class="{"goal" if i==3 else ""}"><i>{i+1}</i><div><b>{t}</b><span>{s}</span></div></li>' for i,(t,s) in enumerate(d['steps']))
        B['dream']=f'<h2>Droombaan en studierichting</h2><div class="dr"><div class="card"><div class="kick">Mijn droombaan</div><h3 class="job">{d["dream"]}</h3><ul class="steps">{st}</ul></div><div style="display:flex;flex-direction:column;gap:2.6mm">{learn}<div class="card guide"><img src="img/vo-gids.png" alt=""><span>Bespreek je route met je mentor of decaan. Je kunt je droombaan altijd aanpassen.</span></div></div></div>'
    else:
        B['dream']=f'<h2>Droombaan en studierichting</h2><div class="dr"><div class="card"><div class="kick">Mijn droombaan</div><p style="font-size:8pt;color:var(--soft);margin-top:1.2mm">Nog geen droombaan gekozen. Kies er een in Lobsy, dan zie je hier je studieroute.</p><div class="wl"></div></div>{learn}</div>'
    return B
def page(inner,n,t,first):
    return f'<section class="page">{top()}{inner}{foot(n,t)}</section>'
def build(d,fn,single=False):
    B=blocks(d)
    if single:
        pages=[page(B['hero']+'<div class="wrap">'+B['facts']+B['dna']+B['int']+B['str']+B['sec']+B['jobs']+B['dream']+'</div>',1,1,True)]
    else:
        p2h=f'<div class="band"></div><div class="p2t"><div><div class="kick">Mijn route</div><h1>Richtingen en vervolg</h1></div><div class="who">{d["name"] or "DNA-paspoort"} · Havo 4 · K7Q-M2P</div></div>'
        pages=[page(B['hero']+'<div class="wrap">'+B['facts']+B['dna']+B['int']+B['str']+'</div>',1,2,True),
               page(p2h+'<div class="wrap">'+B['quote']+B['sec']+B['jobs']+B['dream']+(NOTES if not d['dream'] else '')+'</div>',2,2,False)]
    open('src/'+fn,'w').write('<!doctype html><html lang="nl"><head><meta charset="utf-8"><title>DNA-paspoort middelbare school</title><link rel="stylesheet" href="vo.css"></head><body>'+''.join(pages)+'</body></html>')
build(FULL,'vo-sara.html'); build(SPARSE,'vo-sparse.html')
