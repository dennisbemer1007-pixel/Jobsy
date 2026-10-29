exec(open('_work/build_vo.py').read().split('NOTES=')[0])
def top():
    return f'<header class="top"><div class="brand"><img src="img/vo-merk.png" alt="">Lobsy<small>DNA-paspoort · middelbare school</small></div><div class="sp"></div><div class="stat"><div><b>Alle 4 werelden afgerond</b>Lobsy voor scholen · 14 okt 2026</div><span class="ck">{CHK}</span></div></header>'
FOOT=f'<footer class="foot">{SH}<div class="sp">Geen cijfer en geen schooladvies. Dit paspoort is om te ontdekken.<br>Lobsy bewaart geen namen. Je antwoorden worden na dit schooljaar gewist.</div><div class="r"><b>lobsy.nl</b>14 okt 2026</div></footer>'
def build(d,fn):
    QUOTE=(f'<div class="qbox"><div class="sub">In mijn eigen woorden</div><p>“{d["quote"]}”</p><small>Geschreven door de leerling, niet aangepast door Lobsy</small></div>' if d['quote'] else '<div class="qbox"><div class="sub">In mijn eigen woorden</div><small>Nog niets geschreven. Wil je het zelf invullen?</small><div class="wl" style="height:5mm"></div><div class="wl" style="height:5mm"></div></div>')
    chips='<div class="chips"><span class="chip">Havo 4</span><span class="chip">Profiel Natuur &amp; Gezondheid</span><span class="chip">Amersfoort (school)</span><span class="chip">Code K7Q-M2P</span></div>'
    if d['name']:
        idb=f'<div class="id"><div class="mono">{d["ini"]}</div><div style="flex:1"><div class="kick">Mijn DNA-paspoort</div><div class="nm"><h1>{d["name"]}</h1></div><div class="tag">{d["tag"]}</div>{chips}</div>{QUOTE}</div>'
    else:
        idb=f'<div class="id"><div class="mono empty">jouw<br>naam</div><div style="flex:1"><div class="kick">Mijn DNA-paspoort</div><div class="nm"><span class="nameln"></span><span style="color:var(--soft);font-size:8pt">Naam (optioneel, alleen op de PDF)</span></div><div class="tag" style="margin-top:.8mm">{d["tag"]}</div>{chips}</div>{QUOTE}</div>'
    facts='<div class="facts">'+''.join(f'<div class="card"><div class="k">{k}</div><b>{v}</b>{f"<span>{s}</span>" if s else ""}</div>' for k,v,s in d['facts'])+'</div>'
    dna='<h2>Mijn DNA in 4 lagen</h2><div class="dna">'+''.join(f'<div class="l"><div class="lh"><img src="img/vo-th-{i}.jpg" alt=""><div class="kick">{k}</div></div><h3>{v}</h3><span>{s}</span></div>' for i,k,v,s in LAYERS)+'</div>'
    dis=''.join(f'<span class="chip cor">{x}</span>' for x in d['dis']) or '<span style="color:var(--soft)">Nog niets gekozen.</span>'
    inter='<div class="card"><h3 style="font-size:10pt;margin-bottom:1mm">Mijn interesses</h3><div class="sub">Dit vind ik leuk</div><div class="chips" style="margin:0 0 1.2mm">'+''.join(f'<span class="chip sea">{x}</span>' for x in d['likes'])+f'</div><div class="sub c">Liever niet</div><div class="chips" style="margin:0">{dis}</div></div>'
    stre='<div class="card str"><h3 style="font-size:10pt;margin-bottom:1.2mm">Sterke punten</h3><ul>'+''.join(f'<li><b style="display:inline">{t}.</b> <span>{s}</span></li>' for t,s in d['strengths'])+'</ul>'+('' if len(d['strengths'])==3 else '<div style="color:var(--soft);margin-top:1.6mm">Nog een sterk punt? Schrijf het op:</div><div class="wl"></div>')+'</div>'
    quote=(f'<div class="card quote"><h3 style="font-size:10pt;margin-bottom:1mm">In mijn eigen woorden</h3><p>“{d["quote"]}”</p><small>Geschreven door de leerling, niet aangepast door Lobsy</small></div>' if d['quote'] else
      '<div class="card"><h3 style="font-size:10pt;margin-bottom:1mm">In mijn eigen woorden</h3><div style="color:var(--soft)">Nog niets geschreven. Wil je het zelf invullen?</div><div class="wl"></div><div class="wl"></div><div class="wl"></div></div>')
    learnc='<div class="card"><h3 style="font-size:10pt;margin-bottom:1.2mm">Wat ik wil leren</h3><ul class="ul">'+''.join(f'<li style="margin-bottom:.8mm">{x}</li>' for x in d['learn'])+'</ul>'+('' if len(d['learn'])>1 else '<div class="wl" style="height:5.4mm"></div>')+'</div>'
    row=f'<div class="g3" style="margin-top:2.4mm;grid-template-columns:1.3fr 1.45fr .85fr">{inter}{stre}{learnc}</div>'
    sec='<h2>Sectoren die bij mij passen</h2><div class="sec">'+''.join(f'<div class="card"><h3>{t}</h3><ul>'+''.join(f'<li>{x}</li>' for x in li)+'</ul></div>' for t,li in d['sectors'])
    if len(d['sectors'])<3: sec+=f'<div class="empty-note" style="grid-column:span {3-len(d["sectors"])}">Meer sectoren verschijnen hier als je meer van je profiel invult in Lobsy.</div>'
    sec+='</div>'
    if d['jobs']:
        jobs='<h2>Bijbanen en stages die bij mij passen <small>types werk, geen vacatures</small></h2><div class="sec jobs">'+''.join(f'<div class="card"><h3>{t}</h3><div class="meta">{m}</div><p><b>Waarom:</b> {w} <span class="t">zaterdag · vanaf 15 jaar</span></p></div>' for t,m,w in d['jobs'])+'</div>'
    else:
        jobs='<h2>Bijbanen en stages die bij mij passen <small>types werk, geen vacatures</small></h2><div class="empty-note">Nog geen passende bijbaan of stage binnen jouw leeftijd en regels. Vul je profiel aan of kom later terug.</div>'
    learn='<div><div class="sub">Wat ik wil leren</div><ul class="ul">'+''.join(f'<li>{x}</li>' for x in d['learn'])+'</ul></div>'
    if d['dream']:
        st=''.join(f'<li class="{"goal" if i==3 else ""}"><i>{i+1}</i><b>{t}</b><span>{s}</span></li>' for i,(t,s) in enumerate(d['steps']))
        dream=f'<h2>Droombaan en studierichting</h2><div class="card dr"><div class="head"><span class="kick">Mijn droombaan</span><h3 class="job">{d["dream"]}</h3><div class="guide"><span>Bespreek je route met je mentor of decaan. Je kunt je droombaan altijd aanpassen.</span><img src="img/vo-gids.png" alt=""></div></div><ul class="steps">{st}</ul></div>'
    else:
        dream=f'<h2>Droombaan en studierichting</h2><div class="card dr"><div class="head"><span class="kick">Mijn droombaan</span><span style="color:var(--soft)">Nog geen droombaan gekozen. Kies er een in Lobsy, dan zie je hier je studieroute.</span></div><div class="wl"></div><div class="guide" style="margin:1.6mm 0 0;width:auto;text-align:left"><img src="img/vo-gids.png" alt=""><span>Je paspoort groeit mee. Vul je profiel aan in Lobsy, dan zie je hier meer richtingen en je studieroute.</span></div></div>'
        dream+='<h2>Om te bespreken met je mentor</h2><div class="card"><div style="color:var(--soft)">Wat wil je nog uitzoeken? Schrijf het hier op en neem het mee naar je gesprek.</div>'+'<div class="wl"></div>'*NL+'</div>'
    body=f'<section class="page">{top()}<div class="strip"></div><div class="wrap">{idb}{facts}{dna}{row}{sec}{jobs}{dream}</div>{FOOT}</section>'
    open('src/'+fn,'w').write('<!doctype html><html lang="nl"><head><meta charset="utf-8"><title>DNA-paspoort middelbare school</title><link rel="stylesheet" href="vo.css"></head><body>'+body+'</body></html>')
NL=3
build(FULL,'vo-sara.html'); build(SPARSE,'vo-sparse.html')
