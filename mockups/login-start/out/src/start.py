def ic(p): return f'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">{p}</svg>'
IC={"map":ic('<path d="M9 4 3 6v14l6-2 6 2 6-2V4l-6 2-6-2z"/><path d="M9 4v14M15 6v14"/>'),
"pass":ic('<rect x="5" y="3" width="14" height="18" rx="2"/><circle cx="12" cy="10" r="3"/><path d="M8.5 17h7"/>'),
"compass":ic('<circle cx="12" cy="12" r="9"/><path d="m15.5 8.5-2 5-5 2 2-5 5-2z"/>'),
"match":ic('<path d="M12 21s-7-4.5-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 11c0 5.5-7 10-7 10z"/>'),
"book":ic('<path d="M4 5a2 2 0 0 1 2-2h13v16H6a2 2 0 0 0-2 2V5z"/><path d="M8 7h7"/>'),
"device":ic('<rect x="7" y="2.5" width="10" height="19" rx="2"/><path d="M11 18h2"/>'),
"eye":ic('<path d="M3 3l18 18"/><path d="M10.6 6.1A9.8 9.8 0 0 1 12 6c5 0 9 6 9 6a15 15 0 0 1-2.6 3.2M6.6 7.6C4.4 9.1 3 12 3 12s4 6 9 6a8.6 8.6 0 0 0 3.9-.9"/>'),
"trash":ic('<path d="M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13"/>'),
"shield":ic('<path d="M12 3 5 6v5c0 4.5 3 8 7 10 4-2 7-5.5 7-10V6l-7-3z"/><path d="m9 12 2 2 4-4"/>'),
"plus":ic('<path d="M12 5v14M5 12h14"/>'),"minus":ic('<path d="M5 12h14"/>')}
STEPS=[("vragen","1","20 korte vragen","Geen goed of fout. In 3 minuten klaar, zonder account."),
("dna","2","Jouw werk-DNA","Hoe jij werkt en wat je belangrijk vindt, in gewone woorden."),
("banen","3","Banen die passen","Op de kaart, met je reistijd per fiets, OV of auto.")]
ZONES=[("Het strand","Start met wie je bent vandaag."),("Aan de kust","Ontdek wat je kunt en wat je leuk vindt."),("Tussen de rotsen","Kies welke richting bij je past."),("In de diepte","Ga dieper met je werk-DNA."),("Naar het licht","Zie welke banen echt bij je passen.")]
GET=[("map","De banenkaart","Zie welke banen je echt kunt halen, met reistijd per fiets, OV of auto.","Gratis"),
("pass","Mijn Paspoort","Alles over jou op één plek. Jij kiest wat je deelt.","Gratis"),
("compass","Ontdekkingsreis","Stap voor stap ontdekken wie je bent. Pauzeren mag altijd.","Gratis"),
("match","Match","Swipe door banen die bij je werk-DNA passen.","Gratis"),
("book","Jouw rapport","De diepteanalyse na je gratis tests: meer vragen en een persoonlijk rapport om te bewaren.","€ 2,99 eenmalig")]
TRUST=[("device","Eerst op jouw apparaat","Je testantwoorden blijven op je eigen telefoon of computer tot je een account maakt."),
("eye","Werkgevers zien niets zonder jou","Een werkgever ziet je antwoorden nooit. In je paspoort kies jij wat je deelt."),
("trash","Wissen kan altijd","Met één knop wis je je antwoorden. Je account verwijderen kan ook, zelf."),
("shield","Veilig en eerlijk","Vanaf 13 jaar. Jonger dan 16? Dan vraagt je ouder of voogd eerst toestemming. Geen verborgen kosten.")]
FAQ=[("Is Lobsy echt gratis?","Ja. De test, je account, je paspoort en de banenkaart zijn gratis. Alleen het uitgebreide rapport (de diepteanalyse) kost eenmalig € 2,99."),
("Heb ik een account nodig voor de test?","Nee. De test doe je zonder account. Je antwoorden blijven op je eigen apparaat tot je er zelf een maakt."),
("Wat gebeurt er met mijn antwoorden?","Ze blijven op je eigen apparaat tot je een account maakt. Een werkgever ziet ze nooit; in je paspoort kies jij wat je deelt."),
("Ik spreek nog niet goed Nederlands. Kan ik Lobsy gebruiken?","")]
steps="".join(f'<div class="st"><div class="spot" style="background-image:url(spots/{k}.jpg)"></div><div class="stt"><span class="num">{n}</span><h3>{h}</h3><p>{p}</p></div></div>' for k,n,h,p in STEPS)
zones="".join(f'<li><span class="dot">{i+1}</span><b>{h}</b><span>{p}</span></li>' for i,(h,p) in enumerate(ZONES))
get="".join(f'<div class="gc"><span class="gi">{IC[k]}</span><h4>{h}</h4><p>{p}</p><em class="{"" if t=="Gratis" else "paid"}">{t}</em></div>' for k,h,p,t in GET)
trust="".join(f'<div class="tr"><span class="ti">{IC[k]}</span><div><h4>{h}</h4><p>{p}</p></div></div>' for k,h,p in TRUST)
faq="".join(f'<div class="fq{" open" if a else ""}"><div class="fqh">{q}<span>{IC["minus" if a else "plus"]}</span></div>{f"<p>{a}</p>" if a else ""}</div>' for q,a in FAQ)
S=f"""
.nav .short{{display:none}}
.hero{{position:relative;height:690px;background:#fbf2e3 url({A}finale-hero.jpg) no-repeat center top/1440px auto}}
.hero:after{{content:"";position:absolute;left:0;right:0;bottom:0;height:90px;background:linear-gradient(transparent,var(--pearl))}}
.hin{{position:relative;z-index:2;max-width:1200px;margin:0 auto;padding:48px 32px 0}}
.hin .txt{{max-width:660px}}
.hero h1{{font-size:3.3rem;line-height:1.05;letter-spacing:-.015em;margin:16px 0 14px}}.hero h1 em{{font-style:normal;color:#2f7a4f}}
.lead{{font-size:1.15rem;color:#34465c;line-height:1.5;max-width:540px}}
.ctas{{display:flex;gap:22px;align-items:center;margin-top:26px}}
.meta{{display:flex;gap:18px;flex-wrap:wrap;margin-top:16px;color:#46576c;font-size:.88rem}}.meta span{{display:inline-flex;gap:6px;align-items:center}}.meta span:before{{content:"";width:5px;height:5px;border-radius:50%;background:#c9a27a}}
.mband{{display:none}}
.band2{{padding:44px 0}}.band2.a{{background:#f7f1e8}}.band2.b{{background:#fbf2e3;border-top:1px solid #efe3d1;border-bottom:1px solid #efe3d1}}.band2.c{{background:#f1f2ea;border-top:1px solid #e3e6d8;border-bottom:1px solid #e3e6d8}}
.sec{{max-width:1200px;margin:0 auto;padding:0 32px}}
.eb{{color:#b4461f;font-weight:650;font-size:.85rem;margin-bottom:6px}}.sec h2{{font-size:2rem}}.sec .h2s{{color:var(--muted);margin-top:6px;font-size:1.02rem}}
.steps{{display:grid;grid-template-columns:repeat(3,1fr);gap:20px;margin-top:22px}}
.st{{background:var(--surf);border:1px solid var(--border);border-radius:18px;overflow:hidden}}
.spot{{height:170px;background-size:cover;background-position:center 60%;background-color:var(--cream)}}
.stt{{padding:18px 22px 22px}}.num{{display:inline-grid;place-items:center;width:26px;height:26px;border-radius:50%;background:var(--soft);color:var(--brand);font-weight:700;font-size:.85rem}}
.st h3{{font-size:1.15rem;margin:10px 0 4px}}.st p{{color:#3d4e63}}
.jh{{display:flex;justify-content:space-between;align-items:flex-end;gap:24px}}
.zones{{list-style:none;margin:30px 0 0;padding:0;display:grid;grid-template-columns:repeat(5,1fr);position:relative}}
.zones:before{{content:"";position:absolute;left:14px;right:14px;top:14px;border-top:2px dashed #d9c3a6}}
.zones li{{position:relative;padding-right:18px}}.dot{{position:relative;display:grid;place-items:center;width:30px;height:30px;border-radius:50%;background:#fff;border:2px solid var(--coral);color:var(--coral);font-weight:700;font-size:.85rem}}
.zones li:last-child .dot{{background:var(--coral);color:#fff}}
.zones b{{display:block;color:var(--brand);margin:12px 0 2px}}.zones span:not(.dot){{color:#3d4e63;font-size:.93rem}}
.kaart{{display:grid;grid-template-columns:1fr 1.25fr;align-items:center;gap:8px;background:#fef1e1;border:1px solid var(--border);border-radius:20px;overflow:hidden}}
.kaart .kt{{padding:36px 40px}}.kaart h2{{font-size:1.7rem;margin:4px 0 8px}}.kaart p{{color:#3d4e63}}
.modes{{display:flex;gap:8px;margin:18px 0 22px}}.modes span{{padding:6px 14px;border-radius:99px;border:1px solid #d6cec4;background:#fff;font-weight:600;font-size:.88rem;color:var(--brand)}}.modes span:first-child{{background:var(--brand);color:#fff;border-color:var(--brand)}}
.kart{{padding:24px 28px 24px 0}}.kart img{{display:block;width:100%;height:auto;}}
.stats{{display:grid;grid-template-columns:repeat(4,1fr);gap:16px;margin-top:18px}}
.stat{{padding:16px 20px;border-radius:14px;background:rgba(255,252,250,.6);border:1px solid var(--border)}}.stat b{{display:block;color:var(--brand);font-size:1.5rem}}.stat span{{color:var(--muted);font-size:.9rem}}
.gets{{display:grid;grid-template-columns:repeat(5,1fr);gap:14px;margin-top:22px}}
.gc{{background:var(--surf);border:1px solid var(--border);border-radius:16px;padding:20px;display:flex;flex-direction:column}}
.gi,.ti{{width:42px;height:42px;border-radius:12px;background:var(--soft);color:var(--brand);display:grid;place-items:center;flex-shrink:0}}.gi svg,.ti svg{{width:21px;height:21px}}
.gc h4{{margin:14px 0 6px;font-size:1.05rem}}.gc p{{color:#3d4e63;font-size:.92rem;flex:1}}
.gc em{{font-style:normal;margin-top:14px;align-self:flex-start;font-size:.78rem;font-weight:700;color:#1f5135;background:#e8f3ec;border-radius:99px;padding:3px 10px}}.gc em.paid{{color:#8f6232;background:#f8ecd9}}
.trust{{display:grid;grid-template-columns:repeat(4,1fr);gap:14px;margin-top:22px}}
.tr{{display:flex;flex-direction:column;gap:12px;align-items:flex-start;padding:18px 20px;border-radius:16px;background:var(--surf);border:1px solid var(--border)}}.tr h4{{font-size:1.02rem;margin-bottom:3px}}.tr p{{color:#3d4e63;font-size:.93rem}}
.duo{{display:grid;grid-template-columns:1fr 1fr;gap:22px;align-items:stretch}}.emp{{display:flex;flex-direction:column}}
.emp{{background:var(--surf);border:1px solid var(--border);border-radius:20px;overflow:hidden}}
.emp .eart{{flex:1;min-height:170px;background:var(--cream) url(spots/werkgever.jpg) no-repeat center 35%/cover}}
.emp .et{{padding:24px 28px 28px}}.emp h2,.fqs h2{{font-size:1.5rem;margin:2px 0 4px}}.emp ul{{margin:14px 0 20px;padding:0;list-style:none;display:grid;gap:6px;color:#3d4e63}}
.emp li:before{{content:"";display:inline-block;width:6px;height:6px;border-radius:50%;background:var(--brand);margin-right:10px;vertical-align:2px}}
.erow{{display:flex;gap:16px;align-items:center;flex-wrap:wrap}}.soon{{font-size:.75rem;font-weight:700;color:#8f6232;background:#f8ecd9;border-radius:99px;padding:3px 10px}}
.fqs{{background:var(--surf);border:1px solid var(--border);border-radius:20px;padding:28px}}
.fq{{border-top:1px solid var(--border);padding:14px 0}}.fq:first-of-type{{margin-top:14px}}.fqh{{display:flex;justify-content:space-between;gap:14px;font-weight:650;color:var(--brand)}}.fqh span{{width:20px;height:20px;flex-shrink:0;color:var(--muted)}}.fq p{{margin-top:8px;color:#3d4e63;font-size:.95rem}}
.end{{max-width:1200px;margin:0 auto;padding:0 32px}}
.endin{{display:grid;grid-template-columns:1fr 360px;align-items:stretch;background:#fbf2e3;border:1px solid #efe3d1;border-radius:22px;overflow:hidden}}
.endin .tx{{padding:36px 40px}}.endin h2{{font-size:1.7rem;margin-bottom:6px}}.endin p{{color:#3d4e63;margin-bottom:20px}}
.endart{{background:url(spots/banen.jpg) no-repeat center 30%/cover}}
@media(max-width:600px){{
 .nav .lang{{display:none}}.nav .right{{gap:14px}}.nav .btn.sm{{padding:8px 12px}}.nav .long{{display:none}}.nav .short{{display:inline}}
 .hero{{height:auto;background:none}}.hero:after{{display:none}}
 .mband{{display:block;height:230px;margin:4px 16px 0;border-radius:16px;background:#fbf2e3 url({A}finale-hero.jpg) no-repeat;background-size:auto 420px;background-position:74% 78%}}
 .hin{{padding:22px 16px 0}}.hero h1{{font-size:2.15rem}}.lead{{font-size:1.02rem}}
 .ctas{{flex-direction:column;align-items:stretch;gap:14px}}.ctas .lnk{{text-align:center}}
 .band2{{padding:32px 0}}.band2.a[style]{{padding-top:32px!important}}.meta{{flex-direction:column;gap:4px}}.sec{{padding:0 16px}}.tr{{flex-direction:row}}.sec h2{{font-size:1.5rem}}
 .steps,.gets,.trust,.duo{{grid-template-columns:1fr}}.steps{{gap:14px}}
 .st{{display:grid;grid-template-columns:110px 1fr}}.spot{{height:auto;min-height:130px}}.stt{{padding:14px 16px}}
 .jh{{display:block}}.jh .btn{{display:none}}
 .zones{{grid-template-columns:1fr;gap:16px}}.zones:before{{left:14px;right:auto;top:14px;bottom:14px;border-top:0;border-left:2px dashed #d9c3a6}}
 .zones li{{display:grid;grid-template-columns:30px 1fr;column-gap:14px;padding:0}}.zones li .dot{{grid-row:span 2}}.zones b{{margin:2px 0 0}}
 .jcta{{display:flex!important;margin-top:22px}}
 .kaart{{grid-template-columns:1fr}}.kaart{{gap:0}}.kart{{order:-1;padding:18px 16px 0}}.kaart .kt{{padding:14px 20px 24px}}
 .stats{{grid-template-columns:1fr 1fr;gap:10px}}
 .gets{{gap:12px}}.gc{{display:grid;grid-template-columns:42px 1fr;column-gap:14px;padding:16px}}.gc .gi{{grid-row:span 3}}.gc h4{{margin:0 0 4px}}.gc em{{margin-top:10px;justify-self:start}}
 .trust{{gap:12px}}.fqs{{padding:22px 20px}}.emp .eart{{flex:none;height:180px}}.emp .et{{padding:20px}}
 .end{{padding:0 16px}}.endin{{grid-template-columns:1fr}}.endart{{height:170px;order:-1}}.endin .tx{{padding:22px 20px}}.endin .btn{{width:100%}}
}}"""
B=f"""<section class="hero"><nav class="nav"><div class="brand"><img src="lobsy-256.png" alt="">Lobsy</div><div class="links"><span>Hoe het werkt</span><span>Banenkaart</span><span>Partners</span></div>
<div class="right"><span class="lang">NL ▾</span><span class="nolnk">Inloggen</span><span class="btn sm coral"><span class="long">Doe de gratis test</span><span class="short">Gratis test</span></span></div></nav>
<div class="mband"></div>
<div class="hin"><div class="txt"><span class="badge"><i></i>Gratis en zonder account</span>
<h1>Soms moet je uit je schild <em>groeien.</em></h1>
<p class="lead">Lobsy laat zien wie jij bent, wat je kunt en welk werk bij je past. Eerst jij, dan de baan. Doe de korte test en zie: dit ben jij.</p>
<div class="ctas"><span class="btn coral">Doe de gratis test →</span><span class="lnk">Inloggen</span></div>
<div class="meta"><span>20 vragen · 3 minuten</span><span>Werkgevers zien je antwoorden niet</span></div></div></div></section>
<div class="band2 a" style="padding-top:8px"><section class="sec"><div class="eb">Zo werkt het</div><h2>In drie stappen van jou naar werk</h2><p class="h2s">Eerst jij, dan de baan.</p><div class="steps">{steps}</div></section></div>
<section class="band2 b"><div class="sec"><div class="jh"><div><div class="eb">Ontdekkingsreis</div><h2>Van het strand naar het licht</h2><p class="h2s">Vijf zones, dezelfde reis als in de app.</p></div><span class="btn">Start de ontdekkingsreis →</span></div>
<ol class="zones">{zones}</ol><div class="jcta" style="display:none"><span class="btn" style="width:100%">Start de ontdekkingsreis →</span></div></div></section>
<div class="band2 a"><section class="sec"><div class="kaart"><div class="kt"><div class="eb">De banenkaart</div><h2>Ruim 320 vacatures, met jouw reistijd</h2><p>Zie welke banen je echt kunt halen vanaf huis, en hoe lang je onderweg bent.</p><div class="modes"><span>Fiets</span><span>OV</span><span>Auto</span></div><span class="btn line">Bekijk de banenkaart →</span></div><div class="kart"><img src="spots/vervoer-blok.jpg" alt=""></div></div>
<div class="stats"><div class="stat"><b>320+</b><span>vacatures op de banenkaart</span></div><div class="stat"><b>20 vragen</b><span>en 3 minuten voor de gratis test</span></div><div class="stat"><b>NL en EN</b><span>Lobsy in het Nederlands en Engels</span></div><div class="stat"><b>Gratis</b><span>test, account, paspoort en banenkaart</span></div></div></section></div>
<div class="band2 c"><section class="sec"><div class="eb">Wat je krijgt</div><h2>Van “wie ben ik?” naar “hier wil ik werken”</h2><p class="h2s">Alles op één plek, zodra je inlogt.</p><div class="gets">{get}</div></section></div>
<div class="band2 a"><section class="sec"><div class="eb">Privacy en vertrouwen</div><h2>Jouw verhaal is van jou</h2><div class="trust">{trust}</div></section></div>
<div class="band2 b"><section class="sec"><div class="duo"><div class="emp"><div class="eart"></div><div class="et"><div class="eb">Voor werkgevers</div><h2>Vind mensen die écht bij je team passen</h2><p style="color:var(--muted)">Niet alleen op papier.</p>
<ul><li>Vacatures plaatsen</li><li>Werken met tokens</li><li>Matches op werk-DNA en reistijd</li></ul><div class="erow"><span class="btn line">Voor werkgevers →</span><span class="soon">Binnenkort</span><span class="lnk">Werkgever registreren</span></div></div></div>
<div class="fqs"><div class="eb">Veelgestelde vragen</div><h2>Kort en eerlijk antwoord</h2>{faq}</div></div></section></div>
<div class="band2 a"><section class="end"><div class="endin"><div class="tx"><h2>Benieuwd wat bij jou past?</h2><p>Doe de gratis test. Geen account nodig, en je antwoorden blijven van jou.</p><div class="ctas" style="margin-top:0"><span class="btn coral">Doe de gratis test →</span><span class="lnk">Account maken</span></div></div><div class="endart"></div></div></section></div>"""
page("start",S,B)
