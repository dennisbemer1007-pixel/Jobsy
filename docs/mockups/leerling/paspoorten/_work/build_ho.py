CHK='<svg viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l5 5 9-10"/></svg>'
OK='<svg viewBox="0 0 24 24" fill="none" stroke="#3d6b55" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l5 5 9-10"/></svg>'
NO='<svg viewBox="0 0 24 24" fill="none" stroke="#b4533e" stroke-width="3" stroke-linecap="round"><path d="M6 6l12 12M18 6L6 18"/></svg>'
FULL=dict(name='Yara de Vries',ini='YdV',tag='Zorgzaam en rustig. Wil weten hoe het zit en legt rustig uit.',
 chips=[('','Hbo Verpleegkunde · jaar 3'),('o','Stage sep–dec 2026'),('','Utrecht · 25 km'),('','Beschikbaar'),('o','Open voor werk'),('l','Paspoortnr. LB-54470')],
 verified=True,tests='Alle 4 DNA-tests afgerond · 11 okt 2026',
 avail=('1 sep 2026','16–24 uur per week'),shifts=[('Ocht.','In overleg',0),('Midd.','Ja',1),('Avond','In overleg',0),('Nacht','In overleg',0)],
 transport=('Rijbewijs B · ov','max. 25 min reizen','Eigen auto: nee'),
 lang=True,
 strdate='11 okt 2026',strengths=[('Zorgzaam en betrouwbaar','Checkt afspraken en volgt het zorgplan.'),('Rustig onder druk','Blijft kalm als het druk is op de afdeling.'),('Zorgvuldig in de praktijk','Werkt stap voor stap volgens protocol.')],
 jobs=[('Stage verpleegafdeling','Past bij zorgzaam en zorgvuldig · Meelopen met verpleegkundigen · Binnen 25 km'),('Bijbaan zorginstelling (16+)','Rustige uren na school · Helpen bij basiszorg · Avond of middag'),('Vrijwilligerswerk buurtcentrum','Contact met mensen · Korte shifts · Om te ontdekken')],
 exp=[('Stage SEH (snuffel)','1 mnd','Ziekenhuis (fictief) · mrt 2025','Observatie, begeleid meelopen'),('BHV-helper','10 mnd','OBS De Kompasroos · sep 2024 – jun 2025','Eerste hulp team op school')],
 edu=['Hbo Verpleegkunde, richting verpleegkundige in het ziekenhuis','Jaar 3 · stageperiode sep–dec 2026'],certs=['BHV','EHBO'],
 route=[('Hbo Verpleegkunde · jaar 3','Nu'),('Stage verpleegafdeling sep–dec 2026','Stageperiode'),('Afronden hbo · BIG-register','Daarna'),('Verpleegkundige ziekenhuis','Doel')],
 learn=['Meer ervaring met acute zorg','Medicatie toedienen onder begeleiding oefenen','Communicatie met patiënten en familie'],
 sector=('Zorg &amp; welzijn','Mensgericht en zorgzaam',['Past bij functie: Stage verpleegafdeling','Ervaring: Stage SEH (snuffel)'],'Stage verpleegafdeling · Bijbaan zorginstelling · Assistent zorg'),
 quote='Ik wil mensen echt helpen als ze bang of onzeker zijn. Rustig blijven en uitleggen wat er gebeurt.',
 layers=[('rivier','Mijn scharen','Competenties','Zorgzaam en betrouwbaar · Rustig onder druk · Zorgvuldig in de praktijk','23 sep 2026'),('gracht','Mijn antennes','Loopbaan','Helpen en uitleggen · Werken met mensen in de zorg','24 sep 2026'),('park','Mijn rots','Werkcultuur','Vast team op de afdeling · Duidelijke afspraken met de begeleider','4 okt 2026'),('haven','Mijn kompas','Waarden','Veiligheid voor de patiënt · Respectvol contact','11 okt 2026')],
 how=[('Veiligheid voor de patiënt','controleert naam en polsbandje'),('Respectvol contact','legt rustig uit wat er gebeurt')],
 best=['Laat haar eerst observeren en stel daarna korte, concrete vragen','Geef een vaste begeleider op de afdeling tijdens de stage','Plan medicatiemomenten stap voor stap, met dubbelcheck'],
 checked=['DNA-tests (alle 4)','E-mail bevestigd','Paspoort-inhoud = live-versie op de QR-datum'])
SPARSE=dict(FULL,nl=13,name='',ini='',tag='Samenwerken in team',chips=[('','Hbo Verpleegkunde · jaar 3'),('o','Stage sep–dec 2026'),('','Utrecht'),('l','Paspoortnr. LB-54470')],
 verified=False,tests='1 van 4 DNA-tests afgerond',avail=None,shifts=None,transport=('','max. 25 min reizen',''),lang=False,
 strdate='30 sep 2026',strengths=[('Samenwerken in team','Uit de afgeronde competentietest.')],jobs=[],exp=[],edu=['Hbo Verpleegkunde','Jaar 3'],certs=[],route=[],learn=[],
 sector=('Zorg &amp; welzijn','Mensgericht en zorgzaam',['Past bij functie: Stage verpleegkunde','Samenwerken in team, uit de competentietest'],'Stage verpleegkunde'),
 quote='',layers=[('rivier','Mijn scharen','Competenties','Samenwerken in team','30 sep 2026'),('gracht','Mijn antennes','Loopbaan','',''),('park','Mijn rots','Werkcultuur','',''),('haven','Mijn kompas','Waarden','','')],
 how=[],best=[],checked=['DNA-tests (1 van 4)','Paspoort-inhoud = live-versie op de QR-datum'])
NOTC=["Identiteit en werkvergunning","Echtheid van diploma's en certificaten","Referenties van vorige werkgevers"]
def top(d):
    v=f'<div class="ver"><div><b>Lobsy-geverifieerd</b>{d["tests"]}</div><span class="ck">{CHK}</span></div>' if d['verified'] else f'<div class="ver pend"><div><b>Nog niet compleet</b>{d["tests"]}</div><span class="ck">{CHK}</span></div>'
    return f'<header class="top"><div class="brand"><img src="img/ho-merk.png" alt="">Lobsy<small>DNA-paspoort · student</small></div><div class="sp"></div>{v}</header>'
def foot(n,t):
    return f'''<footer class="foot"><div class="qr"><img src="img/ho-qr.svg" alt=""></div><div class="sp"><b>Scan voor de live, geverifieerde versie</b> · Geldig t/m 14 okt 2026<br>Gespreksinput, geen beoordeling: Lobsy geeft bureaus en werkgevers geen score, ranking of automatische selectie van kandidaten.<br>Bewust niet op dit paspoort: geboortedatum, foto, BSN, gezondheid, scores of ranking.</div><div class="r"><b>lobsy.nl</b>Gemaakt 14 okt 2026<br>pagina {n}/{t}</div></footer>'''
def blocks(d):
    B={}
    chips='<div class="chips">'+''.join(f'<span class="chip {c}">{t}</span>' for c,t in d['chips'])+'</div>'
    if d['name']:
        B['hero']=f'<div class="hero"></div><div class="id"><div class="mono">{d["ini"]}</div><div><div class="kick">DNA-paspoort · hoger onderwijs</div><h1>{d["name"]}</h1><div class="tag">{d["tag"]}</div>{chips}</div></div>'
    else:
        B['hero']=f'<div class="hero"></div><div class="id"><div class="mono empty">geen<br>naam</div><div><div class="kick">DNA-paspoort · hoger onderwijs</div><h1>Student LB-54470</h1><div class="tag">{d["tag"]}</div>{chips}</div></div>'
    f=[]
    if d['avail']: f.append(f'<div class="card"><div class="lab">Beschikbaar per</div><b>{d["avail"][0]}</b><span>{d["avail"][1]}</span></div>')
    if d['shifts']: f.append('<div class="card"><div class="lab">Diensten</div><div class="shifts">'+''.join(f'<div class="{"y" if y else ""}"><b>{a}</b>{b}</div>' for a,b,y in d['shifts'])+'</div></div>')
    t=d['transport']; f.append(f'<div class="card"><div class="lab">Vervoer</div>{f"<b>{t[0]}</b>" if t[0] else ""}<span>{t[1]}</span>{f"<span>{t[2]}</span>" if t[2] else ""}</div>')
    if len(f)<3 and not d['lang']: f.append('<div class="empty-note" style="grid-column:span %d;display:flex;align-items:center">Beschikbaarheid en diensten nog niet ingevuld.</div>'%(3-len(f)))
    if d['lang']: f.append('<div class="card"><div class="lab">Talen</div><b>Nederlands</b><div style="display:flex;align-items:center;gap:2mm;margin:.6mm 0 .4mm"><div style="display:flex;gap:1mm"><i style="display:block;width:3mm;height:3mm;border-radius:.6mm;background:#33505f"></i><i style="display:block;width:3mm;height:3mm;border-radius:.6mm;background:#33505f"></i><i style="display:block;width:3mm;height:3mm;border-radius:.6mm;background:#33505f"></i><i style="display:block;width:3mm;height:3mm;border-radius:.6mm;background:#33505f"></i><i style="display:block;width:3mm;height:3mm;border-radius:.6mm;background:#e4eaec"></i></div><b style="font-size:8pt;color:#33505f">C1</b></div><span>Bijna als een moedertaal.</span></div>')
    B['facts']=f'<div class="grid{4 if len(f)==4 else 3} facts">'+''.join(f)+'</div>'
    B['lang']=''
    B['str']=f'<h2>Sterke punten <small>Zelfinzicht uit een afgeronde test · {d["strdate"]}</small></h2><div class="grid3">'+''.join(f'<div class="card it acc"><b>{a}</b><span>{b}</span></div>' for a,b in d['strengths'])+'</div>'
    B['jobs']='<h2>Stages en bijbanen die bij mij passen</h2>'+('<div class="grid3">'+''.join(f'<div class="card it"><b>{a}</b><span>{b}</span></div>' for a,b in d['jobs'])+'</div>' if d['jobs'] else '<div class="empty-note">Nog geen passende stage of bijbaan binnen jouw leeftijd en regels. Vul je profiel aan of kom later terug.</div>')
    exp=''.join(f'<div class="exp it"><div><b>{a}</b><span>{c}</span><span>{e}</span></div><em>{b}</em></div>' for a,b,c,e in d['exp'])
    edu='<div class="lab">Opleiding</div><ul class="b">'+''.join(f'<li>{x}</li>' for x in d['edu'])+'</ul>'
    if d['certs']: edu+='<div class="lab" style="margin-top:2mm">Certificaten</div><ul class="b">'+''.join(f'<li>{x}</li>' for x in d['certs'])+'</ul>'
    B['exp']='<h2>Ervaring en papieren</h2>'+(f'<div class="grid2"><div class="card">{exp}</div><div class="card">{edu}</div></div>' if exp else f'<div class="grid2"><div class="card">{edu}</div><div class="empty-note" style="display:flex;align-items:center">Nog geen werkervaring of certificaten toegevoegd.</div></div>')
    B['route']=('<h2>Mijn richting</h2><div class="card"><div class="steps">'+''.join(f'<div class="{"goal" if i==3 else ""}"><i>{i+1}</i><span>{b}</span><b>{a}</b></div>' for i,(a,b) in enumerate(d['route']))+'</div></div>') if d['route'] else ''
    B['learn']=('<h2>Wat ik wil leren</h2><div class="learn">'+''.join(f'<span>{x}</span>' for x in d['learn'])+'</div>') if d['learn'] else ''
    s=d['sector']
    B['sector']=f'<h2>Sectoren die bij mij passen</h2><div class="card sector"><div><div class="first">Mijn eerste keus</div><h3>{s[0]}</h3><div style="color:var(--soft);font-size:7.6pt">{s[1]}</div></div><div><div class="lab">Waarom het bij mij past</div><ul class="b">'+''.join(f'<li>{x}</li>' for x in s[2])+f'</ul></div><div><div class="lab">Voorbeeldfuncties</div><div style="font-size:7.6pt">{s[3]}</div></div></div>'
    B['quote']=f'<h2>In mijn eigen woorden</h2><div class="card quote">“{d["quote"]}”<small>Geschreven door de student, niet aangepast door Lobsy</small></div>' if d['quote'] else ''
    B['layers']='<h2>Mijn DNA in 4 lagen</h2><div class="grid4">'+''.join(f'<div class="card layer{"" if dt else " todo"}"><img src="img/ho-l-{i}.jpg" alt=""><h3>{a}</h3><div class="sub">{b}</div><p>{c or "Deze test is nog niet gedaan."}</p><div class="done">{"✓ afgerond "+dt if dt else "Nog open"}</div></div>' for i,a,b,c,dt in d['layers'])+'</div>'
    B['how']=('<div class="grid3"><div><h2>Hoe ik graag werk</h2><div class="card">'+''.join(f'<div class="it" style="margin-bottom:1.6mm"><b>{a}</b><span>{b}</span></div>' for a,b in d['how'])+'</div></div><div><h2>Zo haal je het beste uit Yara</h2><div class="card"><ul class="b">'+''.join(f'<li style="margin-bottom:.8mm">{x}</li>' for x in d['best'])+'</ul></div></div><div><h2>Wat ik wil leren</h2><div class="card"><ul class="b">'+''.join(f'<li style="margin-bottom:.8mm">{x}</li>' for x in d['learn'])+'</ul></div></div></div>') if d['how'] else ''
    B['ver']='<h2>Wat betekent “Lobsy-geverifieerd”?</h2><div class="grid2"><div class="card"><div class="lab" style="color:var(--ok)">Gecontroleerd door Lobsy</div><ul class="vchk">'+''.join(f'<li>{OK}{x}</li>' for x in d['checked'])+'</ul></div><div class="card"><div class="lab" style="color:var(--coral)">Niet gecontroleerd</div><ul class="vchk">'+''.join(f'<li>{NO}{x}</li>' for x in NOTC)+'</ul></div></div>'
    L='<div style="border-bottom:.3mm solid #d9d4c7;height:8.5mm"></div>'
    B['notes']='<div class="grid2"><div><h2>Om te bespreken met je stagebegeleider</h2><div class="card">'+L*(d.get('nl',6))+'</div></div><div><h2>Volgende stap</h2><div class="card"><ul class="b"><li style="margin-bottom:1.4mm">Rond de andere 3 DNA-tests af. Dan vult dit paspoort zich verder.</li><li style="margin-bottom:1.4mm">Vul je beschikbaarheid, diensten en talen in.</li><li style="margin-bottom:1.4mm">Voeg stages, bijbanen of certificaten toe.</li><li>Kies zelf of je naam op het paspoort komt.</li></ul></div></div></div>'
    return B
def build(d,fn,layout):
    B=blocks(d); pages=[]; n=len(layout)
    for i,keys in enumerate(layout):
        head=B['hero'] if i==0 else f'<div class="band"></div><div class="p2t"><div><div class="kick">Verdieping</div><h1>Mijn DNA en richting</h1></div><div class="who">{d["name"] or "Student"} · LB-54470</div></div>'
        pages.append(f'<section class="page">{top(d)}{head}<div class="wrap">'+''.join(B[k] for k in keys)+f'</div>{foot(i+1,n)}</section>')
    open('src/'+fn,'w').write('<!doctype html><html lang="nl"><head><meta charset="utf-8"><title>DNA-paspoort hoger onderwijs</title><link rel="stylesheet" href="ho.css"></head><body>'+''.join(pages)+'</body></html>')
build(FULL,'ho-yara.html',[['facts','str','jobs','exp','quote'],['route','sector','layers','how','ver']])
build(SPARSE,'ho-sparse.html',[['facts','str','jobs','exp','layers'],['sector','ver','notes']])
