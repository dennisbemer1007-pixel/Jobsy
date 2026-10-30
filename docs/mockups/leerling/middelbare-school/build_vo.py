SPK='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 5 6 9H3v6h3l5 4z"/><path d="M15.5 8.5a5 5 0 0 1 0 7"/><path d="M18.5 5.5a9 9 0 0 1 0 13"/></svg>'
def lees(on=False,label='Lees voor'): return f'<button class="lees{" on" if on else ""}">{SPK}{"Leest voor" if on else label}</button>'
def top(code=True):
    r='<div class="r"><span class="chip">H4 · P6S-TBU</span><button class="btn ghost sm">Pauze</button></div>' if code else '<div class="r"><span class="muted" style="font-size:14px">Voor leerlingen</span></div>'
    return f'<div class="top"><a class="brand" href="#"><img src="img/merk.png" alt="">Lobsy<small>middelbare school</small></a>{r}</div>'
def hdr(n): return f'<img class="hdr" src="img/hdr-{n}-desk.jpg" alt=""><img class="hdr-m" src="img/hdr-{n}-mob.jpg" alt="">'
def gids(text,extra=''): return f'<div class="gids"><img src="img/gids.png" alt="Lobsy"><div class="bubble">{text}<div class="row">{lees(label="Lees voor")}{extra}</div></div></div>'
FOOT='<div class="foot"><a href="#">Privacyverklaring</a><a href="#">Toegankelijkheid</a></div>'
STOPS=[('rif','Het koraalrif','Hoe ben jij?'),('grot','De schatgrot','Wat doe je graag?'),('eiland','Pauze-eiland','Waar word jij blij van?'),('toren','De vuurtoren','Wat vind je belangrijk?'),('lagune','De lagune','Waar voel je je thuis?')]
def rail(states,subs):
    o='<div class="rail">'
    for (k,t,q),s,sub in zip(STOPS,states,subs):
        o+=f'<div class="stop {s}"><img src="img/th-{k}.jpg" alt=""><div><b>{t}</b><span>{sub or q}</span></div></div>'
    return o+'</div>'
def shield(n): return '<div class="shield">'+''.join(f'<i class="{"off" if i<n else ""}"></i>' for i in range(10))+'</div>'
def mrail(states):
    o='<div class="mrail">'
    for (k,t,q),s in zip(STOPS,states): o+=f'<div class="ms {s}"><img src="img/th-{k}.jpg" alt=""><span>{t.replace("Het ","").replace("De ","")}</span></div>'
    return o+'</div>'
def page(fn,title,body,css=''):
    open(f'src/{fn}.html','w').write(f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{title}</title><link rel="stylesheet" href="vo.css"><style>{MR}{css}</style></head><body>{body}{FOOT}</body></html>')
MR='''.mrail{display:flex;justify-content:space-between;gap:4px;margin:0 0 14px;position:relative;max-width:560px}
.mrail::before{content:"";position:absolute;left:24px;right:24px;top:22px;border-top:2px dotted #b9c6c6}
.ms{display:flex;flex-direction:column;align-items:center;gap:4px;width:64px;position:relative;font-size:11.5px;text-align:center;line-height:1.2;color:var(--soft)}
.ms img{width:44px;height:44px;border-radius:50%;object-fit:cover;border:2px solid #fff;box-shadow:0 0 0 1.5px var(--line)}
.ms.done img{box-shadow:0 0 0 2px var(--sea)}.ms.now img{box-shadow:0 0 0 3px var(--coral)}.ms.now{color:var(--ink);font-weight:700}.ms.next{opacity:.6}
@media(min-width:601px){.qgrid .mrail,.routebar .mrail{display:none}.pz .mrail{margin:16px 0 4px}}'''
# 01 inloggen
TROW=''.join(f'<div><img src="img/th-{k}.jpg" alt=""><span>{t}</span></div>' for k,t,q in STOPS)
page('01-inloggen','Inloggen',top(False)+f'''
<div class="big hero"><img src="img/big-welkom.jpg" alt="Duinpad naar het strand met Lobsy bij de vlonder"></div>
<div class="wrap lgrid"><div class="lleft"><div class="eyebrow">Lobsy voor de middelbare school</div><h2>Een reis langs de kust, in 4 werelden</h2>
<p class="muted">Je beantwoordt vragen over wie jij bent, wat je graag doet en wat je belangrijk vindt. Daarna zie je welke routes bij je passen.</p>
<div class="trow">{TROW}</div><p class="facts">100 vragen · 2 lesdelen · geen foute antwoorden</p>
<div class="gidsrow">{gids('Hoi! Ik ben Lobsy. Ik reis met je mee langs de kust.')}</div></div>
<div class="card login">
 <div class="eyebrow">Welkom</div>
 <h1>Fijn dat je er bent</h1>
 <p class="muted">Ontdek wie jij bent en wat bij je past. Kies je school en je klas, en typ de code van je kaartje.</p>
 {lees()}
 <label>Jouw school</label><div class="sel">Demo College Lobsy · Naaldwijk</div>
 <label>Jouw klas</label><div class="sel">H4 · havo 4</div>
 <label>Jouw code</label><div class="inp">P6S-TBU</div>
 <div class="note">Je naam hoeft niet. Lobsy kent alleen je code.</div>
 <button class="btn full">Start je reis</button>
</div></div>''',
'''.hero>img{height:500px;object-position:50% 72%}
.lgrid{display:grid;grid-template-columns:1fr 500px;gap:48px;align-items:start}
.lleft{padding-top:34px}.lleft h2{font-size:30px;margin:6px 0 8px}.lleft p{margin:0 0 18px;max-width:520px}
.trow{display:flex;gap:14px;margin-bottom:12px}.trow div{display:flex;flex-direction:column;align-items:center;gap:6px;width:92px;text-align:center;font-size:12.5px;color:var(--soft);line-height:1.25}
.trow img{width:76px;height:76px;border-radius:50%;object-fit:cover;border:3px solid #fff;box-shadow:0 0 0 1.5px var(--line)}
.lleft .facts{font-weight:600;color:var(--sea)}
.login{margin-top:-250px;position:relative}
.login h1{font-size:34px;margin:6px 0 8px}.login p{margin:0 0 14px}
label{display:block;font-weight:600;font-size:14px;margin:16px 0 6px}
.sel,.inp{border:1.5px solid var(--line);border-radius:12px;padding:12px 14px;background:#fff;position:relative}
.sel::after{content:"";position:absolute;right:16px;top:18px;width:8px;height:8px;border-right:2px solid var(--soft);border-bottom:2px solid var(--soft);transform:rotate(45deg)}
.inp{border-color:var(--sea);letter-spacing:.12em;font-weight:600}
.note{background:var(--sea2);border-radius:12px;padding:10px 14px;font-size:14px;margin:16px 0}
.full{width:100%;padding:15px;font-size:16px}
.gidsrow{margin:8px 0 0}
@media(max-width:600px){.hero>img{height:300px;object-position:38% 70%}.lgrid{grid-template-columns:1fr;gap:0}.lleft{order:2;padding-top:24px}.lleft h2{font-size:24px}
.trow{gap:4px;justify-content:space-between}.trow div{width:62px;font-size:11px}.trow img{width:54px;height:54px}.login{margin:-40px 0 0}.login h1{font-size:28px}}''')
# 02 start / reisoverzicht
pins=[(120,330,'Start','Hier begin je',''),(270,470,'1','Het koraalrif','Hoe ben jij?'),(486,282,'2','De schatgrot','Wat doe je graag?'),(844,350,'Pauze','Pauze-eiland','Even uitrusten'),(1050,175,'3','De vuurtoren','Wat vind je belangrijk?'),(1250,345,'4','De lagune','Waar voel je je thuis?')]
pinh=''.join(f'<div class="pin{" w" if not n.isdigit() else ""}" style="left:{x/14.4:.2f}%;top:{(y)/6.2:.2f}%"><i>{n}</i><div><b>{t}</b><span>{q}</span></div></div>' for x,y,n,t,q in pins)
page('02-start-reisoverzicht','Jouw reis',top()+hdr('kust')+f'''
<div class="wrap intro"><div class="eyebrow">Jouw reis</div><h1>Een reis langs de kust, in 4 werelden</h1><p class="muted">Lobsy leert jou kennen, zonder namen. Onderweg zie je steeds waar je bent.</p></div>
<div class="big map"><img src="img/big-reiskaart.jpg" alt="Kaart van de reis langs koraalrif, schatgrot, pauze-eiland, vuurtoren en lagune">{pinh}</div>
<div class="wrap"><div class="grid2">
<div class="card"><div style="display:flex;justify-content:space-between;align-items:center;gap:12px"><h2>Zo werkt het</h2>{lees()}</div>
<ul class="how"><li><b>100 vragen</b>, 25 in elke wereld. Er zijn geen foute antwoorden.</li><li>Na wereld 2 kom je op het <b>Pauze-eiland</b>. Daar kies je wat je leuk vindt.</li><li>Het duurt <b>2 lesdelen</b> van 40 tot 45 minuten. Je docent zegt wanneer deel 2 begint.</li><li>Stoppen mag altijd. Je gaat later verder waar je was.</li><li>Twijfel je over een vraag? Open <b>Stel je voor</b> voor een voorbeeld.</li></ul>
<button class="btn" style="padding:14px 28px">Beginnen</button></div>
<div class="side">{gids('Klaar voor de reis? Ik ga met je mee.')}
<ol class="mlist">{''.join(f'<li><img src="img/th-{k}.jpg" alt=""><div><b>{t}</b><span>{q}</span></div></li>' for k,t,q in STOPS)}</ol></div>
</div></div>''',
'''.intro{padding-top:26px;padding-bottom:14px}.intro h1{font-size:38px;margin:4px 0 6px}.intro p{margin:0}
.map>img{height:620px;object-position:50% 60%}
.pin{position:absolute;display:flex;gap:8px;align-items:flex-start;transform:translate(-14px,-14px)}
.pin i{flex:none;width:28px;height:28px;border-radius:50%;background:var(--coral);color:#fff;font:700 12px "DM Sans";display:flex;align-items:center;justify-content:center;font-style:normal;box-shadow:0 0 0 4px rgba(255,253,248,.8)}
.pin.w i{background:var(--sea);width:auto;padding:0 9px;border-radius:14px}
.pin div{background:rgba(255,253,248,.92);border-radius:10px;padding:5px 10px;box-shadow:0 3px 10px rgba(30,47,59,.12)}
.pin b{display:block;font-size:14px}.pin span{font-size:12px;color:var(--soft)}
.grid2{display:grid;grid-template-columns:1.35fr 1fr;gap:28px;margin-top:-60px;position:relative}
.how{padding-left:20px;margin:16px 0 22px}.how li{margin:8px 0}
.side{padding-top:80px}.mlist{display:none}
@media(max-width:600px){.intro h1{font-size:27px}.map>img{height:auto;aspect-ratio:16/9}.pin{display:none}
.grid2{grid-template-columns:1fr;margin-top:16px;gap:18px}.side{padding-top:0;order:-1}
.mlist{display:block;list-style:none;padding:0;margin:14px 0 0;position:relative}
.mlist li{display:flex;gap:12px;align-items:center;padding:6px 0;position:relative}
.mlist li:not(:last-child)::after{content:"";position:absolute;left:25px;top:58px;height:12px;border-left:2px dotted #b9c6c6}
.mlist img{width:52px;height:52px;border-radius:50%;object-fit:cover;border:2px solid #fff;box-shadow:0 0 0 1.5px var(--line)}
.mlist b{display:block;font-size:15px}.mlist span{font-size:13px;color:var(--soft)}}''')
# 03 vraag
QCSS='''.qgrid{display:grid;grid-template-columns:270px 1fr;gap:26px;margin-top:-38px;position:relative}
.railcard{padding:20px}.railcard h3{font-size:18px;margin-bottom:10px}
.qmeta{display:flex;justify-content:space-between;align-items:center;gap:10px;flex-wrap:wrap}
.q{font-size:27px;line-height:1.3;margin:10px 0 14px}
.stel{background:#f2efe7;border-radius:14px;padding:14px 16px;margin:16px 0;border-left:4px solid var(--sea)}
.stel b{font-family:Fraunces,serif}.stel p{margin:6px 0 0}
.saved{color:var(--sea);font-size:14px;font-weight:600;margin:12px 0 0}
.qnav{display:flex;justify-content:space-between;margin-top:18px;gap:10px}
.ushield{display:none}.under{margin-top:22px;display:flex;justify-content:space-between;align-items:flex-end;gap:20px}
.prog{font-size:13px;color:var(--soft)}
@media(max-width:600px){.qgrid{grid-template-columns:1fr;margin-top:14px;gap:0}.railcard{display:none}.q{font-size:22px}.under{flex-direction:column;align-items:stretch}.ushield{display:block}}'''
page('03-vraag-koraalrif','Vraag',top()+hdr('rif')+f'''
<div class="wrap"><div class="qgrid">
<div class="card railcard"><h3>Jouw reis</h3><div class="prog">Wereld 1 van 4 · vraag 12 van 25</div>{rail(['now','next','next','next','next'],['Vraag 12 van 25','','','',''])}
<div class="prog" style="margin-top:14px">Het schild van Lobsy</div>{shield(1)}<div class="prog">Elke 10 vragen valt er een plaat af. Nog 8 vragen tot de volgende.</div></div>
<div>{mrail(['now','next','next','next','next'])}
<div class="card"><div class="qmeta"><div><div class="eyebrow">Het koraalrif · wereld 1 van 4</div><div class="prog">Vraag 12 van 100</div></div>{lees(True)}</div>
<h2 class="q">Als ik een deadline heb, maak ik een planning en houd ik me eraan.</h2>
<div class="stel"><div style="display:flex;justify-content:space-between;align-items:center"><b>Stel je voor</b>{lees()}</div><p>Over twee weken moet je praktijkopdracht af. Je verdeelt het werk over de dagen.</p></div>
<div class="answers"><div class="ans"><i></i>Klopt niet</div><div class="ans"><i></i>Klopt meestal niet</div><div class="ans"><i></i>Klopt deels</div><div class="ans sel"><i></i>Klopt meestal</div><div class="ans"><i></i>Klopt helemaal</div></div>
<div class="saved">✓ Bewaard</div>
<div class="qnav"><button class="btn ghost">Vorige</button><button class="btn ghost">Pauze</button></div></div>
<div class="under">{gids('Twijfel je? Kies wat meestal klopt.')}<div class="card ushield" style="padding:14px 18px;min-width:240px"><div class="prog">Het schild van Lobsy</div>{shield(1)}<div class="prog">Nog 8 vragen tot de volgende plaat</div></div></div>
</div></div></div>''',QCSS)
# 04 nieuwe wereld
page('04-nieuwe-wereld-schatgrot','Nieuwe wereld',top()+f'''
<div class="big cave"><img src="img/big-grot-p1.jpg" alt="Schatgrot met open kist en Lobsy">
<div class="ov card"><div class="eyebrow">Wereld 2 van 4</div><h1>De schatgrot</h1><p class="lead">Wat doe je graag?</p>
<p>Het koraalrif is klaar. Je hebt 25 vragen beantwoord en er is al een plaat van Lobsy's schild af. In de schatgrot gaat het over wat je graag doet en wat je later wilt leren.</p>
<div style="display:flex;gap:10px;flex-wrap:wrap;align-items:center"><button class="btn">Op naar de schatgrot</button>{lees()}</div></div></div>
<div class="wrap"><div class="card routebar">{mrail(['done','now','next','next','next'])}<div class="rr">{''.join(f'<div class="rs {s}"><img src="img/th-{k}.jpg" alt=""><b>{t}</b><span>{sub}</span></div>' for (k,t,q),s,sub in zip(STOPS,['done','now','next','next','next'],['Klaar','Nu','Na vraag 50','Deel 2','Laatste wereld']))}</div>
<div class="prog" style="text-align:center;margin-top:10px">Vraag 26 van 100 komt eraan</div></div>
<div style="margin-top:20px">{gids('Mooi! Kies wat bij jou past, niet wat anderen vinden.')}</div></div>''',
'''.cave>img{height:600px;object-position:50% 50%}
.ov{position:absolute;left:max(28px,calc(50% - 572px));bottom:40px;max-width:480px;background:rgba(255,253,248,.95)}
.ov h1{font-size:40px;margin:4px 0}.lead{font-family:Fraunces,serif;font-size:20px;color:var(--sea);margin:0 0 10px}
.routebar{margin-top:24px}
.rr{display:flex;justify-content:space-between;position:relative}
.rr::before{content:"";position:absolute;left:8%;right:8%;top:36px;border-top:2px dotted #b9c6c6}
.rs{display:flex;flex-direction:column;align-items:center;gap:4px;width:19%;text-align:center;position:relative}
.rs img{width:72px;height:72px;border-radius:50%;object-fit:cover;border:3px solid #fff;box-shadow:0 0 0 1.5px var(--line)}
.rs b{font-size:14px}.rs span{font-size:12.5px;color:var(--soft)}
.rs.done img{box-shadow:0 0 0 2.5px var(--sea)}.rs.now img{box-shadow:0 0 0 3.5px var(--coral);width:84px;height:84px;margin-top:-6px}.rs.next{opacity:.6}
.prog{font-size:13px;color:var(--soft)}
@media(max-width:600px){.cave>img{height:300px;object-position:62% 50%}.ov{position:relative;left:auto;bottom:auto;margin:-40px 14px 0;max-width:none}.ov h1{font-size:30px}.rr{display:none}.routebar .mrail{margin:0}}''')
# 05 pauze-eiland
likes=['Sport','Buiten zijn','Dieren','Gamen','Tekenen','Muziek','Koken of bakken','Fietsen repareren','Bouwen & knutselen','Techniek','Lezen','Dansen','Theater','Filmpjes maken','Mode','Kleine kinderen','Natuur','Auto\'s & motoren','Computers','Puzzels','Rekenen','Talen','Reizen','Programmeren']
dis=likes+['Voor de klas praten','Lang stilzitten','Hard werken in de kou','Veel lezen','Alleen werken','Druk en lawaai','Vies worden']
Y={'Dieren','Tekenen','Natuur'};N={'Lang stilzitten','Druk en lawaai'}
page('05-pauze-eiland','Pauze-eiland',top()+f'''
<div class="big isl"><img src="img/big-eiland-p1.jpg" alt="Klein eiland met boom, hangmat en Lobsy op het bankje"></div>
<div class="wrap"><div class="card pz"><div class="qmeta"><div><div class="eyebrow">Pauze-eiland</div><div class="prog">50 van 100 klaar · pauze tussen wereld 2 en 3</div></div>{lees()}</div>
{mrail(['done','done','now','next','next'])}
<h1 style="font-size:32px;margin:10px 0 4px">Even uitrusten. Waar word jij blij van?</h1><p class="muted" style="margin:0 0 18px">Kies zo veel als je wilt. Dit telt niet mee als vraag.</p>
<h3>Wat vind jij leuk?</h3><div class="chips">{''.join(f'<span class="c{" y" if c in Y else ""}">{c}</span>' for c in likes)}<span class="c add">+ Iets anders</span></div>
<h3 style="margin-top:22px">Wat vind je niet leuk?</h3><div class="chips">{''.join(f'<span class="c{" n" if c in N else ""}">{c}</span>' for c in dis)}<span class="c add">+ Iets anders</span></div>
<p class="warn">Typ geen namen (ook niet je eigen naam), adres of telefoonnummer.</p>
<div class="qnav"><button class="btn ghost">Vorige</button><button class="btn">Klaar, verder</button></div></div>
<div style="margin-top:20px">{gids('Even pauze. Je doet het goed.')}</div></div>''',
'''.isl>img{height:600px;object-position:50% 25%}
.pz{margin-top:-50px;position:relative}.pz h3{font-size:18px;margin-bottom:10px}
.warn{background:#f6ecd9;border-radius:12px;padding:10px 14px;font-size:14px;margin:20px 0 0}
.qnav{display:flex;justify-content:space-between;margin-top:18px}.qmeta{display:flex;justify-content:space-between;align-items:center;gap:10px}.prog{font-size:13px;color:var(--soft)}
@media(max-width:600px){.isl>img{height:260px;object-position:50% 30%}.pz{margin-top:-30px}.pz h1{font-size:24px!important}.pz .mrail{margin-top:14px}}''')
# 06 deel 1 klaar
page('06-deel-1-klaar','Deel 1 klaar',top()+f'''
<div class="big tow"><img src="img/big-toren-p1.jpg" alt="Vuurtoren in de schemering met Lobsy op de rots">
<div class="ov card"><div class="eyebrow">Deel 1 is klaar</div><h1>Goed gedaan!</h1>
<p>Je antwoorden zijn bewaard. Je docent zegt of je nu verdergaat of in een volgende les. Na de pauze wacht de vuurtoren.</p>
<div class="prog">Het schild van Lobsy · 5 van 10 platen eraf</div>{shield(5)}
<div style="display:flex;gap:10px;flex-wrap:wrap;margin-top:16px"><button class="btn">Verder met deel 2</button><button class="btn ghost">Stoppen voor nu</button></div>
<div style="margin-top:12px">{lees()}</div></div></div>
<div class="wrap"><div class="card routebar">{mrail(['done','done','done','now','next'])}<div class="rr">{''.join(f'<div class="rs {s}"><img src="img/th-{k}.jpg" alt=""><b>{t}</b><span>{sub}</span></div>' for (k,t,q),s,sub in zip(STOPS,['done','done','done','now','next'],['Klaar','Klaar','Klaar','Deel 2 start hier','Laatste wereld']))}</div></div>
<div style="margin-top:20px">{gids('Goed gedaan! Stoppen mag. Je gaat later verder waar je was.')}</div></div>''',
'''.tow>img{height:600px;object-position:50% 30%}
.ov{position:absolute;right:max(28px,calc(50% - 572px));top:60px;max-width:440px;background:rgba(255,253,248,.95)}
.ov h1{font-size:40px;margin:4px 0 8px}.prog{font-size:13px;color:var(--soft)}
.routebar{margin-top:24px}
.rr{display:flex;justify-content:space-between;position:relative}
.rr::before{content:"";position:absolute;left:8%;right:8%;top:36px;border-top:2px dotted #b9c6c6}
.rs{display:flex;flex-direction:column;align-items:center;gap:4px;width:19%;text-align:center;position:relative}
.rs img{width:72px;height:72px;border-radius:50%;object-fit:cover;border:3px solid #fff;box-shadow:0 0 0 1.5px var(--line)}
.rs b{font-size:14px}.rs span{font-size:12.5px;color:var(--soft)}
.rs.done img{box-shadow:0 0 0 2.5px var(--sea)}.rs.now img{box-shadow:0 0 0 3.5px var(--coral)}.rs.next{opacity:.6}
@media(max-width:600px){.tow>img{height:320px;object-position:30% 30%}.ov{position:relative;right:auto;top:auto;margin:-40px 14px 0;max-width:none}.ov h1{font-size:30px}.rr{display:none}.routebar .mrail{margin:0}}''')
# 07 resultaat
L=[('Zo ben jij','In balans','Soms stil, soms actief.'),('Dit doe je graag','Onderzoeken en helpen','Je wilt weten hoe het zit.'),('Dit vind je belangrijk','Zelf kiezen','Je bepaalt graag zelf je aanpak.'),('Hier voel je je thuis','Zelfstandig, vrij','Ruimte om je eigen werk te doen.')]
page('07-resultaat-dit-ben-jij','Dit ben jij',top()+f'''
<div class="big lag"><img src="img/big-lagune-p1.jpg" alt="Lagune met steiger en Lobsy">
<div class="ttl"><span class="chip">Klaar · 100 van 100</span><h1>Dit ben jij</h1></div></div>
<div class="wrap"><div class="res">
<div class="card story"><div style="display:flex;justify-content:space-between;align-items:center;gap:10px"><h2>Jouw verhaal</h2>{lees()}</div>
<p>Jij bent in evenwicht: soms stil, soms actief. Je wilt graag weten hoe iets zit en je helpt anderen graag. Belangrijk voor jou: zelf kunnen kiezen hoe je iets aanpakt. Je voelt je thuis op een plek waar je zelfstandig kunt werken. Je houdt van dieren, tekenen en de natuur.</p>
<div class="layers">{''.join(f'<div class="ly"><div class="eyebrow">{a}</div><b>{b}</b><span>{c}</span>{lees()}</div>' for a,b,c in L)}</div></div>
<div class="col">
<div class="card"><h3>Je houdt van</h3><div class="chips"><span class="c y">Dieren</span><span class="c y">Tekenen</span><span class="c y">Natuur</span></div>
<h3 style="margin-top:16px">Liever niet</h3><div class="chips"><span class="c n">Lang stilzitten</span><span class="c n">Druk en lawaai</span></div></div>
<div class="card"><h3>Sterke punten</h3><ul class="sp"><li><b>Samenwerken</b> Je luistert en denkt mee.</li><li><b>Anderen helpen</b> Je ziet het als iemand vastzit.</li><li><b>Betrouwbaar</b> Afspraken kun je op je rekenen.</li></ul></div>
<div class="card"><h3>Richtingen om te verkennen</h3><div class="chips"><span class="c">Dieren & natuur</span><span class="c">Zorg & welzijn</span><span class="c">Onderzoek & lab</span></div></div>
</div></div>
<div class="card acts"><div class="note">Je docent kan dit verhaal ook zien, met jouw code. Niemand anders.</div><div style="display:flex;gap:10px;flex-wrap:wrap"><button class="btn ghost">Bewaar als PDF</button><button class="btn">Bekijk jouw route</button></div></div>
<div style="margin-top:20px">{gids('Kijk, dit ben jij. Zullen we kijken welke route bij je past?')}</div></div>''',
'''.lag>img{height:440px;object-position:50% 62%}
.ttl{position:absolute;left:max(28px,calc(50% - 572px));top:36px}.ttl h1{font-size:52px;margin-top:8px;color:var(--ink);text-shadow:0 1px 12px rgba(255,253,248,.8)}
.res{display:grid;grid-template-columns:1.4fr 1fr;gap:24px;margin-top:-70px;position:relative}
.story p{margin:12px 0 18px;font-size:16.5px}
.layers{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.ly{background:#f4f0e6;border-radius:14px;padding:14px 16px;display:flex;flex-direction:column;gap:4px;align-items:flex-start}
.ly b{font-family:Fraunces,serif;font-size:19px}.ly span{font-size:14px;color:var(--soft);margin-bottom:6px}
.col{display:flex;flex-direction:column;gap:16px}.col .card{padding:20px}.col h3{font-size:18px;margin-bottom:10px}
.sp{list-style:none;padding:0;margin:0}.sp li{padding:6px 0;border-bottom:1px solid var(--line);font-size:14.5px}.sp li:last-child{border:0}.sp b{display:block}
.acts{margin-top:24px;display:flex;justify-content:space-between;align-items:center;gap:16px;flex-wrap:wrap;padding:18px 22px}
.note{background:var(--sea2);border-radius:12px;padding:10px 14px;font-size:14px}
@media(max-width:600px){.lag>img{height:260px;object-position:42% 62%}.ttl{top:14px;left:14px}.ttl h1{font-size:34px}
.res{grid-template-columns:1fr;margin-top:-24px;gap:16px}.layers{grid-template-columns:1fr}.acts{flex-direction:column;align-items:stretch}.acts .btn{flex:1}}''')
# 08 vervolgroute
page('08-route-profiel-en-vervolg','Jouw route',top()+f'''
<div class="big rt"><img src="img/big-route-p1.jpg" alt="Haven met zeilboten die elk een andere kant op varen, Lobsy op de meerpaal">
<div class="intro"><div class="eyebrow">Na de reis</div><h1>Welke route past bij jou?</h1><p>Kijk welk profiel en welke vervolgopleiding passen bij wat je wilt worden. Je kiest zelf, samen met je mentor.</p></div></div>
<div class="wrap"><div class="g">
<div class="card"><div class="eyebrow">Droombaan-checker</div><h2 style="margin:4px 0 12px">Wat wil jij later worden?</h2>
<div class="inp">Dierenarts</div><div style="display:flex;gap:10px;margin:12px 0 20px;flex-wrap:wrap"><button class="btn ghost sm">Andere baan</button><button class="btn ghost sm">Weet ik nog niet</button>{lees()}</div>
<ol class="steps"><li><i>1</i><div><b>Nu: havo 4</b><span>Kies biologie en scheikunde in je profiel Natuur & Gezondheid.</span></div></li>
<li><i>2</i><div><b>Vwo of hbo-propedeuse</b><span>Een havo-diploma geeft geen directe toegang. Stroom door naar vwo of haal eerst je hbo-propedeuse.</span></div></li>
<li><i>3</i><div><b>Diergeneeskunde studeren</b><span>Universiteit, 6 jaar.</span></div></li>
<li class="goal"><i>★</i><div><b>Dierenarts</b><span>In een dierenkliniek, op het platteland of in de zorg voor landbouwdieren.</span></div></li></ol>
<p class="tip">Het is een lange route, maar je hebt al een goede start. Vraag je mentor wat je nu al kunt doen.</p></div>
<div class="col">
<div class="card"><h3>Profiel dat past</h3><div class="pf"><b>Natuur & Gezondheid</b><span>Biologie, scheikunde, wiskunde A of B</span></div><p class="muted s">Past bij: onderzoeken en helpen, dieren en natuur.</p></div>
<div class="card"><h3>Vervolgopleidingen om te bekijken</h3><ul class="ol"><li><b>Biologie en medisch laboratoriumonderzoek</b><span>hbo · 4 jaar</span></li><li><b>Dier- en veehouderij</b><span>hbo · 4 jaar</span></li><li><b>Diergeneeskunde</b><span>wo · 6 jaar · via vwo of hbo-propedeuse</span></li></ul></div>
<div class="card"><h3>Wat kun je nu al doen?</h3><ul class="ol"><li><b>Snuffelstage dierenkliniek</b><span>Kort meekijken, vaak vanaf 15 jaar</span></li><li><b>Open dag bezoeken</b><span>Kies er twee uit de lijst hierboven</span></li></ul></div>
</div></div>
<div class="card acts"><span class="muted">Dit komt ook in je DNA-paspoort.</span><div style="display:flex;gap:10px;flex-wrap:wrap"><button class="btn ghost">Bewaar als PDF</button><button class="btn">Bespreek met je mentor</button></div></div>
<div style="margin-top:20px">{gids('Een kreeft groeit ook stap voor stap. Jij kiest je eigen koers.')}</div></div>''',
'''.rt .intro{position:absolute;left:max(28px,calc(50% - 572px));top:40px;max-width:560px}.rt .intro h1{font-size:44px;margin:4px 0 6px}.rt .intro p{margin:0;color:var(--ink)}
.rt>img{height:520px;object-position:50% 45%}
.g{display:grid;grid-template-columns:1.25fr 1fr;gap:24px;margin-top:-24px;position:relative}
.inp{border:1.5px solid var(--sea);border-radius:12px;padding:12px 14px;background:#fff;font-weight:600}
.steps{list-style:none;padding:0;margin:0;position:relative}
.steps li{display:flex;gap:14px;padding:10px 0;position:relative}
.steps li:not(:last-child)::after{content:"";position:absolute;left:17px;top:48px;bottom:-6px;border-left:2px dotted #b9c6c6}
.steps i{flex:none;width:36px;height:36px;border-radius:50%;background:var(--sea);color:#fff;font:700 14px "DM Sans";display:flex;align-items:center;justify-content:center;font-style:normal}
.steps .goal i{background:var(--coral)}.steps b{display:block}.steps span{font-size:14.5px;color:var(--soft)}
.tip{background:#f6ecd9;border-radius:12px;padding:12px 14px;margin:12px 0 0;font-size:14.5px}
.col{display:flex;flex-direction:column;gap:16px}.col .card{padding:20px}.col h3{font-size:18px;margin-bottom:10px}
.pf{background:var(--sea2);border-radius:12px;padding:12px 14px}.pf b{display:block;font-family:Fraunces,serif;font-size:19px}.pf span{font-size:14px}.s{font-size:14px;margin:10px 0 0}
.ol{list-style:none;padding:0;margin:0}.ol li{padding:8px 0;border-bottom:1px solid var(--line)}.ol li:last-child{border:0}.ol b{display:block;font-size:15px}.ol span{font-size:13.5px;color:var(--soft)}
.acts{margin-top:24px;display:flex;justify-content:space-between;align-items:center;gap:16px;flex-wrap:wrap;padding:18px 22px}
@media(max-width:600px){.rt .intro{position:static;padding:20px 14px 0}.rt .intro h1{font-size:28px}.rt>img{height:260px;object-position:22% 60%}.rt{display:flex;flex-direction:column-reverse}.g{grid-template-columns:1fr;margin-top:-20px;gap:16px}.acts{flex-direction:column;align-items:stretch}}''')
print('done')
