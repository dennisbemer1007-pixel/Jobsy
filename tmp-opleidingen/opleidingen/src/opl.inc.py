# Gedeelde stijl 'Workshops en opleidingen' (zelfde tokens als kandidaatpagina's)
AI.update({"ext":ic('<path d="M14 4h6v6M20 4l-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5"/>'),
"book":ic('<path d="M4 5a2 2 0 0 1 2-2h13v16H6a2 2 0 0 0-2 2z"/><path d="M4 19V5M8 7h7"/>'),
"bookmark":ic('<path d="M6 4h12v17l-6-4-6 4z"/>'),"bmon":'<svg viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"><path d="M6 4h12v17l-6-4-6 4z"/></svg>',
"school":ic('<path d="M3 9 12 4l9 5-9 5-9-5z"/><path d="M7 11v5c3 2 7 2 10 0v-5"/>'),"monitor":ic('<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4"/>'),
"users":ic('<circle cx="9" cy="8" r="3.5"/><path d="M2.5 20c1-3.5 3.5-5.5 6.5-5.5s5.5 2 6.5 5.5"/><path d="M16 4.5a3.5 3.5 0 0 1 0 7M18 14.5c2 .6 3.2 2.6 3.7 5.5"/>'),
"award":ic('<circle cx="12" cy="9" r="5.5"/><path d="m8.5 13.5-1.5 7 5-2.5 5 2.5-1.5-7"/>'),"search":ic('<circle cx="11" cy="11" r="6"/><path d="m20 20-4.5-4.5"/>'),
"sprout":ic('<path d="M12 21v-9"/><path d="M12 12c0-4-3-6-7-6 0 4 3 6 7 6zM12 10c0-3.5 2.5-6 7-6 0 4-2.5 6-7 6z"/>'),
"play":ic('<circle cx="12" cy="12" r="9"/><path d="m10 8.5 5 3.5-5 3.5z"/>'),"build":ic('<path d="M4 21V8l8-4 8 4v13"/><path d="M9 21v-6h6v6"/>')})
OCSS=PCSS+"""
.hero{position:relative;background-color:#f8efe2;background-repeat:no-repeat;overflow:hidden;border-bottom:1px solid #efe3d1}
.hero .hin{position:relative;z-index:1;max-width:1180px;margin:0 auto;padding:40px 28px}
.hero h1{font-size:2.05rem;letter-spacing:-.01em}.hero p.lead{color:#34465c;font-size:1.02rem;margin-top:8px;max-width:520px}
.hero .hact{display:flex;gap:10px;margin-top:18px;flex-wrap:wrap}
.subnav{display:flex;justify-content:space-between;align-items:center;gap:16px;margin-bottom:18px}
.subnav .seg2{display:inline-flex;gap:2px;background:var(--surf);border:1px solid var(--border);border-radius:14px;padding:4px}
.subnav .seg2 span{display:inline-flex;gap:7px;align-items:center;padding:8px 16px;border-radius:10px;font-weight:650;font-size:.92rem;color:var(--brand)}.subnav .seg2 span.on{background:var(--brand);color:#fff}.subnav .seg2 svg{width:16px;height:16px}
.subnav .seg2 i{font-style:normal;font-size:.72rem;background:#f8ecd9;color:#8f6232;border-radius:99px;padding:1px 7px}.subnav .seg2 span.on i{background:rgba(255,255,255,.18);color:#fff}
.subnav .note{font-size:.86rem;color:var(--muted)}
/* typepil */
.tp{display:inline-flex;gap:6px;align-items:center;font-size:.74rem;font-weight:700;border-radius:99px;padding:3px 9px;white-space:nowrap;letter-spacing:.01em}.tp svg{width:13px;height:13px}
.tp.ws{background:#fdf0e3;color:#a2501d}.tp.cu{background:#e9eff8;color:#2c4a73}.tp.op{background:#0f2d5c;color:#fff}
.free{display:inline-flex;gap:5px;align-items:center;font-size:.74rem;font-weight:700;color:#1f5135;background:#e8f3ec;border-radius:99px;padding:3px 9px;white-space:nowrap}.free svg{width:12px;height:12px}
.cost{font-size:.8rem;font-weight:650;color:#34465c;white-space:nowrap}
.pl{font-size:.72rem;font-weight:650;color:#6b6259;background:#f1ede8;border-radius:99px;padding:2px 8px;white-space:nowrap}
/* cursuskaart */
.cc{position:relative;display:flex;flex-direction:column;gap:9px;background:#fffdfb;border:1px solid #ece2d5;border-radius:16px;padding:16px 18px;min-width:0}
.cc.sel{border-color:var(--brand);box-shadow:0 0 0 3px rgba(15,45,92,.08)}
.cc .top{display:flex;gap:6px;align-items:center;flex-wrap:wrap}.cc .top .bm{margin-left:auto;color:#8a7a68;width:20px;height:20px}.cc .top .bm.on{color:var(--coral)}
.cc h4{font-size:1.02rem;color:var(--brand);line-height:1.3}
.meta{display:flex;flex-wrap:wrap;gap:4px 14px;font-size:.84rem;color:#4a5a6e}.meta span{display:inline-flex;gap:6px;align-items:center;white-space:nowrap}.meta svg{width:15px;height:15px;color:#8a7a68}
.why{display:flex;gap:9px;align-items:flex-start;background:#fbf5ec;border-radius:11px;padding:9px 11px;font-size:.86rem;color:#34465c;line-height:1.4}.why svg{width:16px;height:16px;color:#2f7a4f;flex-shrink:0;margin-top:1px}.why b{color:var(--brand);font-weight:650}
.cc .ft{display:flex;justify-content:space-between;align-items:center;gap:10px;margin-top:auto;padding-top:2px}
.cc .ft .lk2{display:inline-flex;gap:6px;align-items:center;font-weight:650;color:var(--brand);font-size:.88rem}.cc .ft .lk2 svg{width:15px;height:15px}
.cc .ft small{font-size:.78rem;color:var(--muted)}
.cc.sel .ft .lk2{color:#a2501d}
.mob{display:none}
.cc .ft .mob,.cc .inb{display:none}
@media(max-width:600px){.cc .ft .mob{display:inline-flex}.cc .inb{display:block;margin:4px -15px -14px;border-top:1px dashed #d8cdbf}.cc .ft .desk{display:none}}
/* zijpaneel */
.panel{background:var(--surf);border:1px solid var(--border);border-radius:18px;overflow:hidden;box-shadow:0 10px 30px rgba(15,45,92,.06)}
.panel .ph{position:relative;padding:18px 22px 16px;background:#fbf2e3;border-bottom:1px solid #efe3d1}
.panel .ph .x{position:absolute;top:14px;right:14px;width:32px;height:32px;border-radius:10px;background:#fff;border:1px solid var(--border);display:grid;place-items:center;color:var(--brand)}.panel .ph .x svg{width:16px;height:16px}
.panel .ph h3{font-size:1.25rem;margin-top:8px;padding-right:36px;line-height:1.25}.panel .ph .by{font-size:.86rem;color:var(--muted);margin-top:4px}
.panel .pb,.inb .pb{padding:16px 22px 20px;display:grid;gap:16px}.inb .pb{padding:14px 15px 4px}.pb{grid-template-columns:minmax(0,1fr)}.pact .btn{padding:13px 12px}
.facts2{display:grid;grid-template-columns:1fr 1fr;gap:8px}.facts2 div{background:#fbf7f1;border:1px solid #f0e7dc;border-radius:12px;padding:9px 11px}.facts2 small{display:flex;gap:6px;align-items:center;font-size:.74rem;font-weight:700;color:#8a7a68;text-transform:uppercase;letter-spacing:.04em}.facts2 small svg{width:13px;height:13px}.facts2 b{display:block;font-size:.9rem;color:var(--brand);margin-top:2px;font-weight:650}
.pb h5{margin:0 0 6px;font-size:.92rem;color:var(--brand)}.pb ul.ls{margin:0;padding:0;list-style:none;display:grid;gap:6px;font-size:.9rem;color:#34465c}.pb ul.ls li{display:flex;gap:9px;align-items:flex-start}.pb ul.ls svg{width:16px;height:16px;color:#2f7a4f;flex-shrink:0;margin-top:2px}
.pb p.tx{font-size:.9rem;color:#34465c}
.gain{display:flex;gap:12px;align-items:center;background:#f4f7fb;border:1px solid #dfe7f1;border-radius:12px;padding:11px 13px;font-size:.88rem;color:#34465c}.gain .gi{width:34px;height:34px;border-radius:10px;background:#fff;display:grid;place-items:center;color:var(--brand);flex-shrink:0}.gain svg{width:18px;height:18px}.gain b{color:var(--brand)}
.pact{display:grid;grid-template-columns:1fr auto;gap:8px}.pact .btn{width:100%}
.prog{display:flex;justify-content:space-between;align-items:center;gap:12px;border-top:1px solid var(--border);padding-top:14px;font-size:.88rem;color:#34465c}.prog b{display:block;color:var(--brand);font-size:.9rem}
.disc{font-size:.78rem;color:var(--muted)}
.ttl{display:flex;flex-wrap:wrap;justify-content:space-between;align-items:baseline;gap:14px;margin:22px 0 12px}.ttl h3{font-size:1.1rem}.ttl small{font-size:.82rem;color:var(--muted)}
.spot{border-radius:50%;object-fit:cover;flex-shrink:0;box-shadow:0 0 0 4px #fff,0 0 0 5px #efe3d1}
@media(max-width:600px){
 .hero .hin{padding:22px 16px 20px}.hero h1{font-size:1.55rem}.hero p.lead{font-size:.95rem}
 .subnav{margin-bottom:14px}.subnav .seg2{display:flex;width:100%}.subnav .seg2 span{flex:1;justify-content:center;padding:8px 8px}.subnav .note{display:none}
 .cc{padding:14px 15px}.panel .ph{padding:16px}.panel .pb{padding:14px 16px 16px}
}"""
def ttag(t):
  return {"Workshop":f'<span class="tp ws">{AI["users"]}Workshop</span>',"Cursus":f'<span class="tp cu">{AI["monitor"]}Cursus</span>',"Opleiding":f'<span class="tp op">{AI["school"]}Opleiding</span>'}[t]
def price(c): return f'<span class="free">{AI["check"]}Gratis</span>' if c=="Gratis" else f'<span class="cost">{c}</span>'
def card(c,sel=False,bm=False,partner=False,foot=None,extra="",after=""):
  t,title,dur,vorm,cost,by,why=c
  vi=AI["monitor"] if vorm.startswith("Online") else AI["pin"]
  pt='<span class="pl">Partnerlink</span>' if partner else ""
  ft=foot if foot is not None else f'<div class="ft"><span class="lk2">{"Details staan rechts" if sel else "Bekijk details"} {AI["chev"]}</span><small>{by}</small></div>'
  return f'<article class="cc{" sel" if sel else ""}"><div class="top">{ttag(t)}{price(cost)}{pt}<span class="bm{" on" if bm else ""}">{AI["bmon"] if bm else AI["bookmark"]}</span></div><h4>{title}</h4><div class="meta"><span>{AI["clock"]}{dur}</span><span>{vi}{vorm}</span></div><div class="why">{AI["sprout"]}<span>{why}</span></div>{extra}{ft}{after}</article>'
# Nepdata (generieke aanbieders, geen echte namen)
C={
"rust":("Workshop","Rustig blijven als het druk wordt","2 dagdelen","Klassikaal · Zaandam","Gratis","Leerwerkloket (regio)","<b>Groeipunt uit je test:</b> rustig blijven onder druk. Handig aan een drukke balie."),
"mbo":("Opleiding","Mbo 4 Dierenartsassistent paraveterinair (BBL)","3 jaar · 1 dag school per week","Klassikaal · ROC","Lesgeld per jaar","ROC (regio)","<b>Dit is je volgende stap</b> naar dierenarts. Leren en werken tegelijk."),
"basis":("Cursus","Basis dierverzorging en hygiëne","6 weken · 2 uur per week","Online","€ 49","Online cursus","<b>Met certificaat</b> voor je Bewijzen. Sterk bij je aanmelding voor de BBL."),
"balie":("Cursus","Lastige gesprekken aan de balie","4 weken · 1 uur per week","Online","Gratis","Open leerplatform","<b>Past bij stap 3:</b> werken in een kliniek, waar je baasjes te woord staat."),
"plan":("Workshop","Plannen en afmaken wat je begint","1 dag","Klassikaal · Zaandam","Gratis","Bibliotheek (regio)","<b>Groeipunt uit je test:</b> plannen en doorzetten tot iets af is."),
"ehbd":("Workshop","Eerste hulp bij dieren","1 dag","Klassikaal · Amsterdam","€ 95","Opleider (regio)","<b>Vaak gevraagd</b> in vacatures voor dierenartsassistent."),
"digi":("Cursus","Digitaal werken: mail, agenda en roosters","3 dagdelen","Online of klassikaal","Gratis","Taal- en digitaalhuis (regio)","<b>Voor iedereen:</b> handig in bijna elke baan, ook in de kliniek."),
"taal":("Cursus","Nederlands op de werkvloer","10 weken · 1 avond per week","Klassikaal · Zaandam","Gratis","Taalhuis (regio)","<b>Voor iedereen:</b> zekerder praten met collega's en klanten."),
"samen":("Workshop","Feedback geven en vragen","1 dagdeel","Online","Gratis","Open leerplatform","<b>Bouwt voort op je sterke punt</b> samenwerken."),
}
def panel(close=True):
  x=f'<span class="x">{AI["x"]}</span>' if close else ""
  return f"""<aside class="panel"><div class="ph">{x}<div class="top" style="display:flex;gap:6px;flex-wrap:wrap">{ttag("Workshop")}{price("Gratis")}</div><h3>Rustig blijven als het druk wordt</h3><p class="by">Leerwerkloket (regio) · in groepjes van 8 tot 12</p></div>
"""+panel_body()+"""</aside>"""
def panel_body():
  return f"""<div class="pb"><div class="facts2"><div><small>{AI["clock"]}Duur</small><b>2 dagdelen</b></div><div><small>{AI["pin"]}Vorm</small><b>Klassikaal, Zaandam</b></div><div><small>{AI["euro"]}Kosten</small><b>Gratis</b></div><div><small>{AI["cal"]}Start</small><b>12 november</b></div></div>
<div><h5>Wat je leert</h5><ul class="ls"><li>{AI["check"]}Overzicht houden als alles tegelijk komt</li><li>{AI["check"]}Rustig blijven bij een boze of verdrietige klant</li><li>{AI["check"]}Na een drukke dag weer opladen</li></ul></div>
<div><h5>Waarom dit voor jou</h5><p class="tx">In je test kwam naar voren dat drukte je snel raakt. In een kliniek is het aan de balie soms vol. Deze workshop maakt zulke dagen lichter, en past bij stap 2 en 3 van je route.</p></div>
<div class="gain"><span class="gi">{AI["award"]}</span><span><b>Wat het je oplevert:</b> een bewijs van deelname. Dat zet je met één klik in je paspoort.</span></div>
<div class="pact"><span class="btn coral">Aanmelden bij aanbieder {AI["ext"]}</span><span class="ibtn2" style="width:48px;height:48px">{AI["bookmark"]}</span></div>
<div class="prog"><span><b>Al begonnen?</b>Zet hem in je voortgang, dan denken we mee.</span><span class="sw"></span></div>
<p class="disc">Je meldt je aan bij de aanbieder zelf. Lobsy deelt niets zonder jouw toestemming.</p></div>"""

PROGCSS="""
.pg{padding:18px 20px}.pg h3{font-size:1.02rem}.pg .pi{display:flex;gap:12px;align-items:flex-start;padding:11px 0;border-top:1px solid #efe6db}.pg .pi:first-of-type{border-top:0;padding-top:10px}
.pg .pic{width:30px;height:30px;border-radius:50%;display:grid;place-items:center;flex-shrink:0}.pg .pic svg{width:15px;height:15px}.pg .pic.ok{background:#e8f3ec;color:#1f5135}.pg .pic.go{background:#fdf0e3;color:#a2501d}.pg .pic.bm{background:#eef2f7;color:#2c4a73}
.pg .pi b{display:block;font-size:.9rem;color:var(--brand);line-height:1.3}.pg .pi small{font-size:.8rem;color:var(--muted)}.pg .pi small.g{color:#1f5135;font-weight:650}
.wk{display:flex;gap:3px;margin-top:6px}.wk i{width:22px;height:5px;border-radius:99px;background:#e3dbd0}.wk i.on{background:#a2501d}
"""
def prog_mini():
  return f"""<section class="card pg"><h3>Jouw voortgang op deze route</h3>
<div class="pi"><span class="pic ok">{AI["check"]}</span><div><b>Eerste hulp bij dieren</b><small class="g">Afgerond · staat in je Bewijzen</small></div></div>
<div class="pi"><span class="pic go">{AI["play"]}</span><div><b>Lastige gesprekken aan de balie</b><small>Bezig · week 2 van 4</small><div class="wk"><i class="on"></i><i class="on"></i><i></i><i></i></div></div></div>
<p class="disc" style="margin-top:8px">Wat je afrondt, zien werkgevers in je paspoort. Vacatures die erom vragen, komen hoger in je matches.</p></section>"""
