exec(open('_work/build_ho.py').read().split('def top')[0])
def top(d):
    v=f'<div class="ver"><div><b>Lobsy-geverifieerd</b>{d["tests"]}</div><span class="ck">{CHK}</span></div>' if d['verified'] else f'<div class="ver pend"><div><b>Nog niet compleet</b>{d["tests"]}</div><span class="ck">{CHK}</span></div>'
    return f'<header class="top"><div class="brand"><img src="img/ho-merk.png" alt="">Lobsy<small>DNA-paspoort · student</small></div><div class="sp"></div>{v}</header>'
FOOT='''<footer class="foot"><div class="qr"><img src="img/ho-qr.svg" alt=""></div><div class="sp"><b>Scan voor de live, geverifieerde versie</b> · Geldig t/m 14 okt 2026 · Gemaakt 14 okt 2026 · <b>lobsy.nl</b><br>Gespreksinput, geen beoordeling: Lobsy geeft bureaus en werkgevers geen score, ranking of automatische selectie van kandidaten.<br>Bewust niet op dit paspoort: geboortedatum, foto, BSN, gezondheid, scores of ranking.</div></footer>'''
def build(d,fn):
    chips='<div class="chips">'+''.join(f'<span class="chip {c}">{t}</span>' for c,t in d['chips'])+'</div>'
    q=(f'<div class="qbox"><div class="lab" style="color:var(--ochre)">In mijn eigen woorden</div><p>“{d["quote"]}”</p><small>Geschreven door de student, niet aangepast door Lobsy</small></div>' if d['quote'] else
       '<div class="qbox"><div class="lab" style="color:var(--ochre)">In mijn eigen woorden</div><small>Nog niets geschreven.</small><div class="wl" style="height:5mm"></div><div class="wl" style="height:5mm"></div></div>')
    if d['name']: idb=f'<div class="id"><div class="mono">{d["ini"]}</div><div style="flex:1"><div class="kick">DNA-paspoort · hoger onderwijs</div><h1>{d["name"]}</h1><div class="tag">{d["tag"]}</div>{chips}</div>{q}</div>'
    else: idb=f'<div class="id"><div class="mono empty">geen<br>naam</div><div style="flex:1"><div class="kick">DNA-paspoort · hoger onderwijs</div><h1>Student LB-54470</h1><div class="tag">{d["tag"]}</div>{chips}</div>{q}</div>'
    f=[]
    if d['avail']: f.append(f'<div class="card"><div class="lab">Beschikbaar per</div><b>{d["avail"][0]}</b><span>{d["avail"][1]}</span></div>')
    if d['shifts']: f.append('<div class="card"><div class="lab">Diensten</div><div class="shifts">'+''.join(f'<div class="{"y" if y else ""}"><b>{a}</b>{b}</div>' for a,b,y in d['shifts'])+'</div></div>')
    t=d['transport']; f.append(f'<div class="card"><div class="lab">Vervoer</div>{f"<b>{t[0]}</b>" if t[0] else ""}<span>{t[1]}</span>{f"<span>{t[2]}</span>" if t[2] else ""}</div>')
    if d['lang']: f.append('<div class="card"><div class="lab">Talen</div><b>Nederlands</b><div class="sq"><i></i><i></i><i></i><i></i><i class="e"></i><em>C1</em></div><span>Bijna als een moedertaal.</span></div>')
    else: f.append('<div class="empty-note" style="grid-column:span 3;display:flex;align-items:center">Beschikbaarheid, diensten en talen nog niet ingevuld.</div>')
    facts='<div class="g4 facts">'+''.join(f)+'</div>'
    stre=f'<div class="card"><h3 class="ct">Sterke punten <small>Zelfinzicht uit een afgeronde test · {d["strdate"]}</small></h3>'+''.join(f'<div class="li"><b>{a}.</b> <span>{b}</span></div>' for a,b in d['strengths'])+'</div>'
    jobs='<div class="card"><h3 class="ct">Stages en bijbanen die bij mij passen</h3>'+(''.join(f'<div class="li o"><b>{a}.</b> <span>{b}</span></div>' for a,b in d['jobs']) if d['jobs'] else '<div style="color:var(--soft)">Nog geen passende stage of bijbaan binnen jouw leeftijd en regels. Vul je profiel aan of kom later terug.</div>')+'</div>'
    exp=''.join(f'<div class="exp"><div><b>{a}</b><span>{c}</span><span>{e}</span></div><em>{b}</em></div>' for a,b,c,e in d['exp'])
    edu='<div class="lab">Opleiding</div><ul class="b">'+''.join(f'<li>{x}</li>' for x in d['edu'])+'</ul>'
    if d['certs']: edu+='<div style="margin-top:.6mm"><span class="lab">Certificaten</span> '+' · '.join(d['certs'])+'</div>'
    expc=f'<div class="card"><h3 class="ct">Ervaring en papieren</h3>{exp or "<div style=color:var(--soft)>Nog geen werkervaring of certificaten toegevoegd.</div>"}<div style="border-top:1px solid var(--line);margin-top:.6mm;padding-top:1mm">{edu}</div></div>'
    educ=''
    s=d['sector']
    sec=f'<div class="card"><h3 class="ct">Sector die bij mij past <small style="color:var(--coral)">Mijn eerste keus</small></h3><div style="display:flex;align-items:baseline;gap:2mm"><h3 style="font-size:10.5pt">{s[0]}</h3><span style="color:var(--soft)">{s[1]}</span></div><div class="lab" style="margin-top:.8mm">Waarom het bij mij past</div><ul class="b">'+''.join(f'<li>{x}</li>' for x in s[2])+f'</ul><div class="lab" style="margin-top:.8mm">Voorbeeldfuncties</div><div>{s[3]}</div></div>'
    if d['route']:
        route='<div class="card"><h3 class="ct">Mijn richting</h3><div class="vsteps">'+''.join(f'<div class="{"goal" if i==3 else ""}"><i>{i+1}</i><span>{b}</span><b>{a}</b></div>' for i,(a,b) in enumerate(d['route']))+'</div></div>'
    else: route=''
    layers='<h2 class="lh2">Mijn DNA in 4 lagen</h2><div class="g4">'+''.join(f'<div class="card layer{"" if dt else " todo"}"><div class="lh"><img src="img/ho-l-{i}.jpg" alt=""><div><h3>{a}</h3><div class="sub">{b}</div></div></div><p>{c or "Deze test is nog niet gedaan."}</p><div class="done">{"✓ afgerond "+dt if dt else "Nog open"}</div></div>' for i,a,b,c,dt in d['layers'])+'</div>'
    how=('<div class="card"><h3 class="ct">Hoe ik graag werk</h3>'+''.join(f'<div class="li"><b>{a}:</b> <span>{b}.</span></div>' for a,b in d['how'])+'</div>') if d['how'] else ''
    best=('<div class="card"><h3 class="ct">Zo haal je het beste uit Yara</h3><ul class="b">'+''.join(f'<li>{x}.</li>' for x in d['best'])+'</ul></div>') if d['best'] else ''
    learnc=('<div class="card"><h3 class="ct">Wat ik wil leren</h3><ul class="b">'+''.join(f'<li>{x}</li>' for x in d['learn'])+'</ul></div>') if d['learn'] else ''
    ver='<div class="card ver2"><h3 class="ct">Wat betekent “Lobsy-geverifieerd”?</h3><div class="vrow"><div class="lab" style="color:var(--ok)">Gecontroleerd door Lobsy</div><div class="vchk">'+' '.join(f'<span>{OK}{x}</span>' for x in d['checked'])+'</div></div><div class="vrow"><div class="lab" style="color:var(--coral)">Niet gecontroleerd</div><div class="vchk">'+' '.join(f'<span>{NO}{x}</span>' for x in NOTC)+'</div></div></div>'
    notes=''
    if not d['name']:
        notes='<div class="g2" style="margin-top:2.4mm"><div class="card"><h3 class="ct">Om te bespreken met je stagebegeleider</h3>'+'<div class="wl"></div>'*NLH+'</div><div class="card"><h3 class="ct">Volgende stap</h3><ul class="b"><li>Rond de andere 3 DNA-tests af. Dan vult dit paspoort zich verder.</li><li>Vul je beschikbaarheid, diensten en talen in.</li><li>Voeg stages, bijbanen of certificaten toe.</li><li>Kies zelf of je naam op het paspoort komt.</li></ul></div></div>'
    body=(f'<section class="page">{top(d)}<div class="strip"></div><div class="wrap">{idb}{facts}'
      f'<div class="cols"><div class="col">{stre}{jobs}{expc}{best}</div><div class="col">{route}{sec}{how}{learnc}{ver}</div></div>'
      f'{layers}{notes}</div>{FOOT}</section>')
    open('src/'+fn,'w').write('<!doctype html><html lang="nl"><head><meta charset="utf-8"><title>DNA-paspoort hoger onderwijs</title><link rel="stylesheet" href="ho.css"></head><body>'+body+'</body></html>')
NLH=14
build(FULL,'ho-yara.html'); build(SPARSE,'ho-sparse.html')
