SPK='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M11 5 6 9H3v6h3l5 4z"/><path d="M15.5 8.5a5 5 0 0 1 0 7"/><path d="M18.5 5.5a9 9 0 0 1 0 13"/></svg>'
def lees(on=False): return f'<button class="lees{" on" if on else ""}">{SPK}{"Wordt voorgelezen" if on else "Voorlezen"}</button>'
def top(code=True):
    r='<div class="r"><span class="lbl">Hbo Verpleegkunde · jaar 3</span><span class="chip">K7R-M2D</span><button class="btn ghost sm">Pauze</button></div>' if code else '<div class="r"><span>Voor studenten mbo, hbo en wo</span></div>'
    return f'<div class="top"><a class="brand" href="#"><img src="img/merk.png" alt="">Lobsy<small>hoger onderwijs</small></a>{r}</div>'
def hdr(n): return f'<img class="hdr" src="img/hdr-{n}-desk.jpg" alt=""><img class="hdr-m" src="img/hdr-{n}-mob.jpg" alt="">'
def tip(t): return f'<div class="tip"><img src="img/merk.png" alt=""><div>{t}</div></div>'
FOOT='<div class="foot"><a href="#">Privacyverklaring</a><a href="#">Toegankelijkheid</a></div>'
STOPS=[('duin','Werkstijl','Hoe werk jij?'),('gracht','Drijfveren','Wat geeft je energie?'),('park','Pauze','Jouw interesses'),('haven','Waarden','Wat vind je belangrijk?'),('rivier','Werkomgeving','Waar kom je tot je recht?')]
def route(states,subs):
    return '<div class="route">'+''.join(f'<div class="st {s}"><img src="img/th-{k}.jpg" alt=""><div><b>{t}</b><span>{sub or q}</span></div></div>' for (k,t,q),s,sub in zip(STOPS,states,subs))+'</div>'
def hbar(states):
    return '<div class="hbar">'+''.join(f'<div class="hs {s}"><img src="img/th-{k}.jpg" alt=""><span>{t}</span></div>' for (k,t,q),s in zip(STOPS,states))+'</div>'
def prog(done,half=0): return '<div class="prog">'+''.join(f'<i class="{"d" if i<done else ("h" if i<done+half else "")}"></i>' for i in range(4))+'</div>'
def page(fn,title,body,css=''):
    open(f'src/{fn}.html','w').write(f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{title}</title><link rel="stylesheet" href="ho.css"><style>{css}</style></head><body>{body}{FOOT}</body></html>')

# 01
TROW=''.join(f'<div><img src="img/th-{k}.jpg" alt=""><span>{t}</span></div>' for k,t,q in STOPS)
page('01-inloggen','Inloggen',top(False)+f'''
<div class="big hero"><img src="img/big-welkom.jpg" alt="Rivier met stenen brug en oude huizen in de ochtendmist"></div>
<div class="wrap lgrid"><div class="lleft"><div class="eyebrow">Lobsy voor studenten</div><h2>Een route in vier onderdelen</h2>
<p class="muted">Je beantwoordt stellingen over hoe je werkt, wat je energie geeft, wat je belangrijk vindt en waar je tot je recht komt. Daarna zie je welke beroepen en stappen bij je passen.</p>
<div class="trow">{TROW}</div><p class="facts">80 stellingen · ongeveer 30 minuten · geen goede of foute antwoorden</p>
{tip('Je kunt altijd pauzeren. Je gaat later verder waar je was.')}</div>
<div class="card login">
 <div class="eyebrow">Welkom</div>
 <h1>Goed dat je er bent</h1>
 <p class="muted">Ontdek wat bij je past, in je opleiding en daarna. Kies je instelling en opleiding en vul de code in die je van je docent of loopbaanbegeleider hebt gekregen.</p>
 {lees()}
 <label>Instelling</label><div class="sel">Demo Hogeschool Lobsy · Utrecht</div>
 <label>Opleiding</label><div class="sel">Hbo Verpleegkunde · jaar 3</div>
 <label>Persoonlijke code</label><div class="inp">K7R-M2D</div>
 <div class="note">Je naam is niet nodig. Je antwoorden zijn alleen gekoppeld aan je code.</div>
 <button class="btn full">Begin je route</button>
</div></div>''',
'''.hero>img{height:520px;object-position:50% 60%}
.lgrid{display:grid;grid-template-columns:1fr 480px;gap:56px;align-items:start}
.lleft{padding-top:36px}.lleft h2{font-size:30px;margin:8px 0 10px}.lleft p{margin:0 0 20px;max-width:540px}
.trow{display:flex;gap:16px;margin-bottom:14px}.trow div{display:flex;flex-direction:column;align-items:center;gap:6px;width:88px;text-align:center;font-size:12.5px;color:var(--soft)}
.trow img{width:68px;height:68px;border-radius:50%;object-fit:cover;box-shadow:0 0 0 1px var(--line)}
.lleft .facts{font-weight:500;color:var(--acc)}
.login{margin-top:-200px;position:relative}
.login h1{font-size:34px;margin:8px 0 10px}.login p{margin:0 0 14px}
label{display:block;font-weight:500;font-size:14px;margin:16px 0 6px}
.sel,.inp{border:1px solid var(--line);border-radius:8px;padding:12px 14px;background:#fff;position:relative}
.sel::after{content:"";position:absolute;right:16px;top:18px;width:7px;height:7px;border-right:1.5px solid var(--soft);border-bottom:1.5px solid var(--soft);transform:rotate(45deg)}
.inp{border-color:var(--acc);letter-spacing:.12em;font-weight:500}
.note{background:var(--acc2);border-radius:8px;padding:10px 14px;font-size:14px;margin:18px 0}
.full{width:100%;padding:15px;font-size:16px}
@media(max-width:600px){.hero>img{height:280px;object-position:55% 60%}.lgrid{grid-template-columns:1fr;gap:0}.lleft{order:2;padding-top:26px}.lleft h2{font-size:24px}
.trow{gap:4px;justify-content:space-between}.trow div{width:62px;font-size:11px}.trow img{width:52px;height:52px}.login{margin:-36px 0 0}.login h1{font-size:28px}}''')

# 02
pins=[(105,'1','Werkstijl','Hoe werk jij?'),(330,'2','Drijfveren','Wat geeft je energie?'),(512,'P','Pauze','Jouw interesses'),(700,'3','Waarden','Wat vind je belangrijk?'),(905,'4','Werkomgeving','Waar kom je tot je recht?')]
pinh=''.join(f'<div class="pin{" p" if n=="P" else ""}" style="left:{x/10.24:.2f}%"><i>{n}</i><b>{t}</b><span>{q}</span></div>' for x,n,t,q in pins)
page('02-start-routeoverzicht','Jouw route',top()+hdr('horizon')+f'''
<div class="wrap intro"><div class="eyebrow">Jouw route</div><h1>Van wie je bent naar waar je heen wilt</h1><p class="muted">Vier onderdelen, met halverwege een korte pauze. Onderweg zie je steeds waar je bent.</p></div>
<div class="big map"><img src="img/big-reiskaart.jpg" alt="Route langs duinen, gracht, park, haven en rivier met skyline"><div class="pins">{pinh}</div></div>
<div class="wrap"><div class="grid2">
<div class="card"><div class="hrow"><h2>Zo werkt het</h2>{lees()}</div>
<ul class="how"><li><b>80 stellingen</b>, 20 per onderdeel. Kies steeds wat het best bij je past.</li><li>Na onderdeel 2 volgt een <b>korte pauze</b>. Daar geef je aan waar je interesse naar uitgaat.</li><li>Het duurt ongeveer <b>30 minuten</b>. Je kunt het in één keer doen of in twee delen.</li><li>Je antwoorden worden steeds bewaard. Stoppen kan op elk moment.</li><li>Twijfel je over een stelling? Open het <b>voorbeeld</b> voor een situatie uit de praktijk.</li></ul>
<button class="btn" style="padding:14px 28px">Begin met onderdeel 1</button></div>
<div class="side"><div class="card get"><h3>Wat je aan het eind krijgt</h3><ul><li>Een profiel in vier lagen: hoe je werkt, wat je energie geeft, wat je belangrijk vindt en waar je tot je recht komt.</li><li>Beroepen die bij je passen, met de stappen ernaartoe.</li><li>Stages en bijbanen om te verkennen.</li></ul></div><ol class="mlist">{''.join(f'<li><img src="img/th-{k}.jpg" alt=""><div><b>{t}</b><span>{q}</span></div></li>' for k,t,q in STOPS)}</ol>
{tip('Er zijn geen goede of foute antwoorden. Het gaat om hoe jij het zelf ervaart.')}</div>
</div></div>''',
'''.intro{padding-top:30px;padding-bottom:10px}.intro h1{font-size:40px;margin:6px 0 8px}.intro p{margin:0}
.map>img{height:640px;object-position:50% 50%}
.pins{position:absolute;left:0;right:0;top:66%}
.pin{position:absolute;transform:translate(-50%,-15px);display:flex;flex-direction:column;align-items:center;text-align:center;width:190px}
.pin b,.pin span{background:rgba(253,252,250,.93);padding:0 10px}.pin b{border-radius:6px 6px 0 0;padding-top:4px}.pin span{border-radius:0 0 6px 6px;padding-bottom:5px}
.pin i{width:30px;height:30px;border-radius:50%;background:var(--acc);color:#fff;font:600 13px "IBM Plex Sans";display:flex;align-items:center;justify-content:center;font-style:normal;margin-bottom:6px;box-shadow:0 0 0 4px rgba(253,252,250,.9)}
.pin.p i{background:var(--ochre)}
.pin b{font-family:"Source Serif Pro",serif;font-size:19px;font-weight:600}.pin span{font-size:13.5px;color:var(--soft)}
.grid2{display:grid;grid-template-columns:1.4fr 1fr;gap:28px;margin-top:8px}
.hrow{display:flex;justify-content:space-between;align-items:center;gap:12px}
.how{padding-left:20px;margin:14px 0 22px}.how li{margin:6px 0}
.mlist{list-style:none;margin:0 0 18px;padding:0;display:none}
.mlist li{display:flex;gap:12px;align-items:center;padding:8px 0;border-bottom:1px solid var(--line)}.mlist img{width:44px;height:44px;border-radius:50%;object-fit:cover}
.mlist b{display:block;font-size:15px}.mlist span{font-size:13px;color:var(--soft)}
.side{padding-top:6px;display:flex;flex-direction:column;gap:16px}.get{padding:22px}.get h3{font-size:19px}.get ul{margin:10px 0 0;padding-left:18px;font-size:14.5px}.get li{margin:5px 0}
@media(max-width:600px){.intro h1{font-size:28px}.map>img{height:auto}.pins{display:none}.grid2{grid-template-columns:1fr;gap:18px}.mlist{display:block;order:1;margin:0}.side{display:contents}.grid2>.card{order:2}.get{order:3}.side .tip{order:4}}''')

# 03
page('03-vraag-werkstijl','Stelling',top()+hdr('duin')+f'''
<div class="wrap qgrid">
<aside class="card rc"><div class="eyebrow">Jouw route</div><div class="rmeta"><b>Onderdeel 1 van 4</b><span>Stelling 12 van 20</span></div>{prog(0,1)}
{route(['now','next','next','next','next'],['Bezig','','','',''])}
<div class="enc">Goed tempo. Je antwoorden worden steeds bewaard.</div></aside>
<main><div class="card q">
<div class="qtop"><div class="eyebrow">Werkstijl · Hoe werk jij?</div>{lees(True)}</div>
<div class="mobmeta">{hbar(['now','next','next','next','next'])}<div style="margin-top:10px">Onderdeel 1 van 4 · stelling 12 van 20</div>{prog(0,1)}</div>
<h1>Als ik een deadline heb, maak ik een planning en houd ik me eraan.</h1>
<details open><summary>Voorbeeld uit de praktijk</summary><p>Over drie weken lever je je stageverslag in. Je verdeelt het schrijven vooraf over de weken en houdt je aan die verdeling, ook als je stagediensten wisselen.</p></details>
<div class="answers"><div class="ans"><i></i>Klopt niet</div><div class="ans"><i></i>Klopt meestal niet</div><div class="ans"><i></i>Klopt deels</div><div class="ans sel"><i></i>Klopt meestal</div><div class="ans"><i></i>Klopt helemaal</div></div>
<div class="qfoot"><button class="btn ghost">Vorige</button><span class="saved">✓ Bewaard</span><button class="btn ghost">Pauze</button></div>
</div>
{tip('Twijfel je? Kies wat meestal klopt. Denk aan hoe je echt werkt, niet aan hoe het hoort.')}
</main></div>''',
'''.qgrid{display:grid;grid-template-columns:300px 1fr;gap:28px;margin-top:-30px;position:relative;align-items:start}
.rc{padding:22px}.rmeta{display:flex;justify-content:space-between;align-items:baseline;margin-top:8px;font-size:14px}.rmeta span{color:var(--soft);font-size:13px}
.rc .route{margin-top:12px}.enc{font-size:13.5px;color:var(--soft);margin-top:14px;border-top:1px solid var(--line);padding-top:12px}
.q{padding:36px}.qtop{display:flex;justify-content:space-between;align-items:center;gap:12px}
.q h1{font-size:31px;margin:18px 0 18px;max-width:720px;font-weight:600}
details{background:var(--bg);border-radius:8px;padding:12px 16px;margin-bottom:24px}summary{font-weight:500;color:var(--acc);cursor:pointer}details p{margin:8px 0 0}
.qfoot{display:flex;justify-content:space-between;align-items:center;margin-top:26px}.saved{color:var(--acc);font-size:14px}
main .tip{margin-top:18px}
.mobmeta{display:none}
@media(max-width:600px){.qgrid{grid-template-columns:1fr;margin-top:-16px;gap:16px}.rc{display:none}.q{padding:22px 20px}.q h1{font-size:23px;margin:12px 0 14px}
.mobmeta{display:block;font-size:13px;color:var(--soft);margin-top:12px}.qtop{flex-wrap:wrap}.qfoot .btn{padding:10px 14px}}''')

# 04
page('04-nieuw-onderdeel-drijfveren','Onderdeel 2',top()+f'''
<div class="big tr"><img src="img/big-gracht.jpg" alt="Stille gracht met bomen en een stenen brug"></div>
<div class="wrap"><div class="card tc">
<div class="tcl"><div class="eyebrow">Onderdeel 2 van 4</div><h1>Drijfveren</h1><h2>Wat geeft je energie?</h2>
<p>Werkstijl is afgerond. In dit onderdeel gaat het over wat je motiveert: welke taken je energie geven en waar je juist moe van wordt.</p>
<div class="row"><button class="btn">Ga verder met onderdeel 2</button>{lees()}</div></div>
<div class="tcr"><div class="eyebrow" style="color:var(--soft)">Waar je bent</div>{hbar(['done','now','next','next','next'])}<p class="muted sm">Stelling 21 van 80 · 20 stellingen in dit onderdeel</p>
{tip('Goed bezig. Je eerste onderdeel staat.')}</div>
</div></div>''',
'''.tr>img{height:600px;object-position:50% 45%}
.tc{margin-top:-70px;position:relative;display:grid;grid-template-columns:1.1fr 1fr;gap:40px;padding:36px}
.tc h1{font-size:44px;margin:6px 0 2px}.tc h2{font-size:22px;font-weight:400;font-style:italic;color:var(--acc)}.tc p{margin:14px 0 20px}
.row{display:flex;gap:12px;align-items:center;flex-wrap:wrap}.tcr .hbar{margin:14px 0 10px}.sm{font-size:13.5px;margin:0 0 16px}
@media(max-width:600px){.tr>img{height:300px}.tc{grid-template-columns:1fr;gap:22px;margin-top:-30px;padding:22px 20px}.tc h1{font-size:34px}}''')

# 05
CH=['Zorg','Mensen begeleiden','Onderzoek','Gezondheid','Lesgeven','Organiseren','Techniek','Sport en bewegen','Creatief werk','Recht','ICT','Natuur','Communicatie']
Y={'Zorg','Mensen begeleiden','Onderzoek','Gezondheid'}
page('05-pauze-interesses','Pauze',top()+f'''
<div class="big pk"><img src="img/big-park.jpg" alt="Bankje onder een grote boom in een stil park"></div>
<div class="wrap"><div class="card pz">
<div class="ph"><div><div class="eyebrow">Halverwege · 40 van 80</div><h1>Even pauze. Waar gaat je interesse naar?</h1></div>{lees()}</div>
{hbar(['done','done','now','next','next'])}
<div class="cols"><div><h3>Dit spreekt me aan</h3><div class="chips">{''.join(f'<span class="c{" y" if c in Y else ""}">{c}</span>' for c in CH)}<span class="c add">+ Zelf toevoegen</span></div></div>
<div><h3>Liever niet</h3><div class="chips"><span class="c n">Verkoop</span><span class="c n">Lang achter een scherm</span><span class="c">Nachtdiensten</span><span class="c">Veel reizen</span><span class="c add">+ Zelf toevoegen</span></div>
<div class="warn">Noem geen namen of andere persoonlijke gegevens als je zelf iets toevoegt.</div></div></div>
<div class="pf"><button class="btn ghost">Vorige</button><button class="btn">Opslaan en verder</button></div>
</div></div>''',
'''.pk>img{height:690px;object-position:50% 0%}
.pz{margin-top:-24px;position:relative;padding:34px}.ph{display:flex;justify-content:space-between;align-items:flex-start;gap:12px}.pz h1{font-size:32px;margin:6px 0 18px}
.pz .hbar{max-width:560px;margin-bottom:24px}.cols{display:grid;grid-template-columns:1.3fr 1fr;gap:36px}.cols h3{font-size:19px;margin-bottom:10px}
.warn{font-size:13.5px;color:var(--soft);margin-top:16px;border-left:2px solid var(--line);padding-left:12px}.pf{display:flex;justify-content:space-between;margin-top:28px}
@media(max-width:600px){.pk>img{height:280px}.pz{margin-top:-28px;padding:22px 20px}.ph{flex-direction:column;gap:6px;margin-bottom:18px}.pz h1{margin-bottom:0}.pz h1{font-size:25px}.cols{grid-template-columns:1fr;gap:22px}}''')

# 06
page('06-eerste-helft-afgerond','Eerste helft afgerond',top()+f'''
<div class="big hv"><img src="img/big-haven.jpg" alt="Haven bij schemering met twee kranen en een schip">
<div class="ov card"><div style="display:flex;justify-content:space-between;align-items:center"><div class="eyebrow">Eerste helft afgerond</div>{lees()}</div><h1>Twee van de vier onderdelen staan</h1>
<p>Je hebt werkstijl en drijfveren afgerond en je interesses aangegeven. Ga nu door met waarden, of stop hier en ga later verder. Je antwoorden zijn bewaard.</p>
{prog(2)}<p class="muted sm">40 van 80 stellingen</p>
<div class="row"><button class="btn">Verder met onderdeel 3</button><button class="btn ghost">Later verder</button></div></div></div>
<div class="wrap"><div class="card rb">{hbar(['done','done','done','next','next'])}<p class="muted sm">Onderdeel 3 · Waarden begint bij de haven</p></div>
<div class="tw">{tip('Neem gerust even afstand. Een frisse blik helpt bij de volgende vragen.')}</div></div>''',
'''.hv>img{height:660px;object-position:50% 0%}
.ov{position:absolute;left:max(32px,calc(50% - 558px));top:70px;width:470px;padding:30px;background:rgba(253,252,250,.95)}
.ov h1{font-size:32px;margin:6px 0 10px}.ov p{margin:0 0 12px}.sm{font-size:13.5px}.row{display:flex;gap:12px;flex-wrap:wrap;margin-top:16px}
.rb{margin-top:-40px;position:relative;padding:22px 30px}.rb .hbar{max-width:640px;margin:0 auto}.rb p{text-align:center;margin:10px 0 0}
.tw{margin-top:18px}
@media(max-width:600px){.hv>img{height:300px;object-position:100% 0%}.ov{position:static;width:auto;margin:16px 16px 0;padding:22px 20px}.ov h1{font-size:26px}.rb{margin-top:16px;padding:18px 14px}}''')

# 07
L=[('Werkstijl','Rustig en zorgvuldig','Je werkt stap voor stap en houdt overzicht, ook als het druk is.'),('Drijfveren','Zorgen en uitleggen','Je krijgt energie van mensen helpen en dingen helder uitleggen.'),('Waarden','Betekenisvol werk','Je wilt weten dat je werk verschil maakt voor iemand.'),('Werkomgeving','Een vast team','Je komt tot je recht met duidelijke afspraken en collega\'s om je heen.')]
page('07-profiel','Jouw profiel',top()+f'''
<div class="big rv"><img src="img/big-rivier.jpg" alt="Rivier bij gouden licht met een fiets langs het pad en een skyline">
<div class="ttl"><span class="chip">Afgerond · 80 van 80</span><h1>Jouw profiel</h1><p>Zorgzaam en rustig. Wil weten hoe het zit en legt rustig uit.</p></div></div>
<div class="wrap g7">
<div class="card st7"><div class="hrow"><h2>Zo kom je naar voren</h2>{lees()}</div>
<p>Je werkt rustig en zorgvuldig en houdt het overzicht, ook als het op de afdeling druk wordt. Je krijgt energie van mensen helpen en van dingen helder uitleggen. Werk moet voor jou betekenis hebben. Je voelt je het best in een vast team met duidelijke afspraken.</p>
<div class="lay">{''.join(f'<div class="ly"><div class="eyebrow">{a}</div><h3>{b}</h3><p>{c}</p></div>' for a,b,c in L)}</div>
<div class="done7"><div class="eyebrow" style="color:var(--soft)">Jouw route is afgerond</div>{hbar(['done','done','done','done','done'])}</div></div>
<div class="sd">
<div class="card"><h3>Sterke punten</h3><dl><dt>Zorgzaam en betrouwbaar</dt><dd>Checkt afspraken en volgt het zorgplan.</dd><dt>Rustig onder druk</dt><dd>Blijft kalm als het druk is.</dd><dt>Zorgvuldig in de praktijk</dt><dd>Werkt stap voor stap volgens protocol.</dd></dl></div>
<div class="card"><h3>Interesses</h3><div class="chips"><span class="c y">Zorg</span><span class="c y">Mensen begeleiden</span><span class="c y">Onderzoek</span><span class="c y">Gezondheid</span></div><h3 style="margin-top:16px">Liever niet</h3><div class="chips"><span class="c n">Verkoop</span><span class="c n">Lang achter een scherm</span></div></div>
<div class="card"><h3>Wat je wilt leren</h3><ul class="ll"><li>Meer ervaring met acute zorg</li><li>Medicatie toedienen onder begeleiding</li><li>Communicatie met patiënten en familie</li></ul></div>
</div></div>
<div class="wrap"><div class="card act"><span class="muted sm">Je loopbaanbegeleider ziet dit profiel via je code. Verder niemand.</span><div class="row"><button class="btn ghost">Bewaar als PDF</button><button class="btn">Bekijk passende beroepen</button></div></div></div>''',
'''.rv>img{height:460px;object-position:50% 45%}
.ttl{position:absolute;left:max(32px,calc(50% - 558px));top:50px;max-width:540px;background:rgba(253,252,250,.84);padding:20px 26px;border-radius:12px}.ttl h1{font-size:52px;margin:12px 0 6px}.ttl p{margin:0;font-size:18px;font-family:"Source Serif Pro",serif;font-style:italic}
.g7{display:grid;grid-template-columns:1.35fr 1fr;gap:24px;margin-top:-60px;position:relative;align-items:start}
.hrow{display:flex;justify-content:space-between;align-items:center;gap:12px}.st7 p{margin:12px 0 18px}
.lay{display:grid;grid-template-columns:1fr 1fr;gap:12px}.ly{background:var(--bg);border-radius:8px;padding:16px}.ly h3{font-size:19px;margin:4px 0 4px}.ly p{margin:0;font-size:14px;color:var(--soft)}
.sd{display:flex;flex-direction:column;gap:16px}.sd .card{padding:22px}.sd h3{font-size:19px;margin-bottom:10px}
dl{margin:0}dt{font-weight:600;font-size:14.5px;margin-top:10px}dd{margin:0;font-size:14px;color:var(--soft)}
.done7{margin-top:22px;border-top:1px solid var(--line);padding-top:16px}.done7 .hbar{margin-top:12px;max-width:560px}
.ll{margin:0;padding-left:18px;font-size:14.5px}.ll li{margin:4px 0}
.act{margin-top:20px;display:flex;justify-content:space-between;align-items:center;gap:16px;padding:18px 24px}.row{display:flex;gap:12px}.sm{font-size:13.5px}
@media(max-width:600px){.rv>img{height:300px;object-position:70% 60%}.big.rv{display:flex;flex-direction:column-reverse}.ttl{position:static;background:none;padding:22px 16px 18px}.ttl h1{font-size:34px}.ttl p{font-size:15.5px}.g7{grid-template-columns:1fr;margin-top:-28px;gap:16px}.lay{grid-template-columns:1fr}
.act{flex-direction:column;align-items:stretch}.row{flex-direction:column}}''')

# 08
page('08-beroepen-en-carriere','Beroepen en carrière',top()+f'''
<div class="big wg"><img src="img/big-wegen.jpg" alt="Uitzicht over een stad met ziekenhuis, station en wegen die alle kanten op gaan">
<div class="ttl"><div class="eyebrow">Na je route</div><h1>Welke kant ga je op?</h1><p>Beroepen die passen bij je profiel, met de stappen ernaartoe. Je kiest zelf, samen met je loopbaanbegeleider.</p></div></div>
<div class="wrap g8">
<div class="card"><div class="eyebrow">Jouw richting</div><div class="hrow"><h2>Verpleegkundige in het ziekenhuis</h2>{lees()}</div>
<span class="vv">Veel vraag in jouw regio</span>
<ol class="steps"><li><i>1</i><div><b>Hbo Verpleegkunde · jaar 3</b><span>Nu</span></div></li><li><i>2</i><div><b>Stage verpleegafdeling · sep tot dec 2026</b><span>Stageperiode</span></div></li><li><i>3</i><div><b>Afronden hbo en inschrijven in het BIG-register</b><span>Daarna</span></div></li><li class="goal"><i>4</i><div><b>Verpleegkundige ziekenhuis</b><span>Doel</span></div></li></ol>
<div class="tip2">Past bij je werkstijl: rustig en zorgvuldig, ook als het druk is op de afdeling.</div></div>
<div class="sd">
<div class="card"><h3>Andere beroepen om te verkennen</h3><div class="jb"><b>Wijkverpleegkundige</b><span>Zelfstandig bij mensen thuis, vaste wijk</span></div><div class="jb"><b>Praktijkondersteuner huisarts</b><span>Begeleiden en uitleggen, vaste werktijden</span></div><div class="jb"><b>Verpleegkundig onderzoeker</b><span>Na een master · onderzoek in de zorg</span></div></div>
<div class="card"><h3>Stages en bijbanen</h3><div class="jb"><b>Stage verpleegafdeling</b><span>Meelopen met verpleegkundigen · binnen 25 km</span></div><div class="jb"><b>Bijbaan zorginstelling</b><span>Avond of weekend · helpen bij basiszorg</span></div></div>
</div></div>
<div class="wrap"><div class="card act"><span class="muted sm">Dit komt ook in je DNA-paspoort voor stages en werk.</span><div class="row"><button class="btn ghost">Bewaar als PDF</button><button class="btn">Plan gesprek met je loopbaanbegeleider</button></div></div>
<div style="margin-top:18px">{tip('Een richting is geen eindpunt. Je kunt je route later altijd bijstellen.')}</div></div>''',
'''.wg>img{height:500px;object-position:50% 55%}
.ttl{position:absolute;left:max(32px,calc(50% - 558px));top:44px;max-width:560px}.ttl h1{font-size:46px;margin:8px 0 8px}.ttl p{margin:0}
.g8{display:grid;grid-template-columns:1.3fr 1fr;gap:24px;margin-top:-50px;position:relative;align-items:start}
.hrow{display:flex;justify-content:space-between;align-items:center;gap:12px;margin-top:6px}.hrow h2{font-size:27px}
.vv{display:inline-block;margin-top:12px;background:var(--acc2);color:var(--acc);border-radius:6px;padding:4px 10px;font-size:13px;font-weight:500}
.steps{list-style:none;padding:0;margin:20px 0}.steps li{display:flex;gap:14px;align-items:flex-start;position:relative;padding-bottom:18px}
.steps li:not(:last-child)::before{content:"";position:absolute;left:15px;top:34px;bottom:2px;border-left:1px dashed #9aa8ae}
.steps i{flex:none;width:31px;height:31px;border:1px solid var(--acc);color:var(--acc);border-radius:6px;display:flex;align-items:center;justify-content:center;font-style:normal;font-weight:600;font-size:14px;background:#fff}
.steps .goal i{background:var(--coral);border-color:var(--coral);color:#fff}.steps b{display:block}.steps span{font-size:13.5px;color:var(--soft)}
.tip2{background:var(--bg);border-radius:8px;padding:12px 16px;font-size:14.5px}
.sd{display:flex;flex-direction:column;gap:16px}.sd .card{padding:22px}.sd h3{font-size:19px;margin-bottom:6px}
.jb{padding:10px 0;border-bottom:1px solid var(--line)}.jb:last-child{border:0}.jb b{display:block;font-size:15px;font-weight:600}.jb span{font-size:13.5px;color:var(--soft)}
.act{margin-top:20px;display:flex;justify-content:space-between;align-items:center;gap:16px;padding:18px 24px}.row{display:flex;gap:12px}.sm{font-size:13.5px}
@media(max-width:600px){.wg>img{height:280px}.big.wg{display:flex;flex-direction:column-reverse}.ttl{position:static;padding:22px 16px 18px}.ttl h1{font-size:30px}.g8{grid-template-columns:1fr;margin-top:-24px;gap:16px}.hrow{flex-wrap:wrap}.hrow h2{font-size:22px}
.act{flex-direction:column;align-items:stretch}.row{flex-direction:column}}''')
print('ok')
