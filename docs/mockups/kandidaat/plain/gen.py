import json,os
CSS="""
.tex{margin:10px 0 24px;font-size:17px;line-height:1.5;color:#4B5563;max-width:560px}.lkrow{display:grid;grid-template-columns:repeat(5,1fr);gap:10px;max-width:520px}.lk{display:flex;align-items:center;justify-content:center;min-height:60px;border:1.5px solid #9CA3AF;border-radius:10px;background:#fff;font-size:22px;font-weight:600;color:#1F2937}.lk.on{border-color:#A8481F;background:#A8481F;color:#fff}.lklab{display:flex;justify-content:space-between;max-width:520px;margin-top:10px;font-size:16px;color:#374151}

.ans{display:flex;align-items:center;min-height:60px;padding:0 20px;border:1.5px solid #9CA3AF;border-radius:10px;background:#fff;font-size:19px;font-weight:500;color:#1F2937;cursor:pointer}.ans.on{border-color:#A8481F;background:#FBEDE5;color:#8A3A17;font-weight:700;box-shadow:inset 4px 0 0 #A8481F}

.mgw{display:none}
@media(max-width:700px){.grid .guide{display:none!important}.mgw{display:block;position:relative;background:#fff;border:1px solid #E5E7EB;border-radius:10px;margin:0 0 14px}
.mg summary{list-style:none;display:flex;align-items:center;gap:10px;min-height:52px;padding:0 56px 0 10px;font-size:16px;font-weight:600;color:#374151;cursor:pointer}.mg summary::-webkit-details-marker{display:none}
.mg summary img{width:32px;height:32px;object-fit:contain;background:#FDF5F0;border-radius:50%;padding:2px;flex:none}.mg summary i{font-style:normal;color:#6B7280;margin-left:auto}.mg[open] summary i{transform:rotate(180deg)}
.mg p{margin:0;padding:0 14px 14px 52px;font-size:17px;line-height:1.5;color:#1F2937}
.mspk{position:absolute;top:4px;right:4px;width:44px;height:44px;border:0;background:transparent;color:#8A3A17;border-radius:8px;display:flex;align-items:center;justify-content:center;cursor:pointer}.mspk svg{width:22px;height:22px}.mspk.on{background:#FBEDE5}}
.guide img{width:64px!important;height:64px!important}

.guide{position:sticky;top:20px;text-align:left}.guide .gh{display:flex;gap:14px;align-items:center;margin-bottom:12px}.guide img{width:84px;height:84px;object-fit:contain;background:#FDF5F0;border-radius:50%;padding:6px}.guide .gt{font-weight:700;color:#111827;font-size:18px}.guide .gs{font-size:15px;color:#4B5563}.guide p{margin:0 0 16px;color:#1F2937;font-size:18px}.speak{min-height:48px;padding:0 16px;border-radius:8px;border:1.5px solid #A8481F;background:#fff;color:#8A3A17;font-weight:600;font-size:17px;display:inline-flex;gap:8px;align-items:center;cursor:pointer;font-family:inherit}.speak svg{width:22px;height:22px}.speak.on{background:#A8481F;color:#fff}

*{box-sizing:border-box}body{margin:0;font-family:Inter,system-ui,sans-serif;color:#111827;background:#F7F8FA;font-size:18px;line-height:1.6;-webkit-font-smoothing:antialiased}
.top{height:72px;background:#fff;border-bottom:1px solid #E5E7EB;display:flex;align-items:center;justify-content:space-between;padding:0 32px}
.logo{font-weight:700;font-size:20px;letter-spacing:-.02em;color:#111827}.logo span{color:#A8481F}
.tr{display:flex;gap:20px;align-items:center;color:#4B5563;font-size:16px}.av{width:32px;height:32px;border-radius:50%;background:#FBEDE5;color:#A8481F;font-weight:600;display:flex;align-items:center;justify-content:center;font-size:15px}
.wrap{max-width:1120px;margin:0 auto;padding:36px 32px 56px}
.step{display:flex;gap:0;margin:0 0 32px;list-style:none;padding:0}
.step li{flex:1;display:flex;flex-direction:column;gap:8px;font-size:16px;color:#4B5563;padding-right:12px}
.step li i{height:4px;border-radius:2px;background:#E5E7EB;display:block}
.step li.done i{background:#A8481F}.step li.cur i{background:linear-gradient(90deg,#A8481F var(--p,50%),#E5E7EB var(--p,50%))}
.step li.cur{color:#111827;font-weight:600}.step li.done{color:#374151}
.grid{display:grid;grid-template-columns:1fr 300px;gap:28px;align-items:start}
.card{background:#fff;border:1px solid #E5E7EB;border-radius:12px;padding:32px}
.side{background:#fff;border:1px solid #E5E7EB;border-radius:12px;padding:22px;font-size:17px;color:#4B5563}
.side h4{margin:0 0 8px;font-size:17px;color:#111827}
.eyebrow{font-size:16px;font-weight:600;color:#A8481F;margin:0 0 6px}
h1{font-size:32px;line-height:1.25;letter-spacing:-.02em;margin:0 0 8px}h2{font-size:20px;margin:28px 0 12px}
.lead{color:#4B5563;margin:0 0 24px;max-width:620px}
label.f{display:block;font-size:17px;font-weight:600;margin:0 0 6px;color:#374151}
.in{min-height:52px;font-size:18px;border:1.5px solid #6B7280;border-radius:8px;padding:0 14px;display:flex;align-items:center;background:#fff;color:#111827}
.fields{display:grid;grid-template-columns:1fr 1fr;gap:18px}
.chips{display:flex;flex-wrap:wrap;gap:10px}
.chip{border:1px solid #D1D5DB;border-radius:8px;padding:0 18px;min-height:48px;display:inline-flex;align-items:center;background:#fff;font-size:17px;color:#374151}
.chip.on{border-color:#A8481F;background:#FBEDE5;color:#8A3A17;font-weight:600}
.opts{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.opt{border:1.5px solid #9CA3AF;border-radius:10px;padding:14px 16px;min-height:56px;display:flex;gap:12px;align-items:flex-start;background:#fff}
.opt b{display:block;font-weight:600}.opt small{color:#4B5563;font-size:15px}
.opt .bx{flex:none;width:18px;height:18px;border:1.5px solid #9CA3AF;border-radius:5px;margin-top:2px}
.opt.on{border-color:#A8481F;background:#FDF5F0}.opt.on .bx{background:#A8481F;border-color:#A8481F;box-shadow:inset 0 0 0 3px #FDF5F0}
.row{display:flex;justify-content:space-between;align-items:center;margin-top:32px;padding-top:22px;border-top:1px solid #F0F1F3;gap:16px}
.btn{min-height:52px;padding:0 26px;border-radius:8px;background:#A8481F;color:#fff;font-weight:600;display:inline-flex;align-items:center;border:0;font-size:18px;white-space:nowrap}
.btn2{min-height:52px;padding:0 20px;border-radius:8px;border:1px solid #D1D5DB;background:#fff;color:#374151;font-weight:500;display:inline-flex;align-items:center;white-space:nowrap}
.note{font-size:16px;color:#4B5563}
.q{border:1px solid #E5E7EB;border-radius:10px;padding:18px;margin-bottom:14px}.q .n{font-size:15px;color:#4B5563;font-weight:600}.q p{margin:4px 0 12px;font-weight:600}
.scale{display:grid;grid-template-columns:repeat(5,1fr);gap:8px}.scale span{border:1px solid #D1D5DB;border-radius:8px;text-align:center;padding:12px 4px;min-height:48px;font-size:15px;color:#374151}.scale span.on{border-color:#A8481F;background:#FBEDE5;color:#8A3A17;font-weight:600}
.list{border:1px solid #E5E7EB;border-radius:10px;margin-bottom:10px;padding:14px 16px;display:flex;justify-content:space-between;align-items:center}.list small{color:#4B5563;display:block}
.add{color:#A8481F;font-weight:600;font-size:16px}
.tags{display:flex;flex-wrap:wrap;gap:6px;margin-top:10px}.tag{font-size:14px;padding:4px 10px;border-radius:999px;background:#F3F4F6;color:#374151}
.tag.t{background:#FBEDE5;color:#8A3A17}
.k4{display:grid;grid-template-columns:1fr 1fr;gap:14px}.k4 .q p{margin:4px 0 0;font-weight:400;color:#4B5563}
.badge{display:inline-block;font-size:14px;font-weight:600;color:#92400E;background:#FEF3C7;border-radius:999px;padding:3px 10px}
.helpm{display:none}.foot{border-top:1px solid #E5E7EB;background:#fff;padding:20px 32px;font-size:15px;color:#4B5563;display:flex;gap:22px}
.hero{display:grid;grid-template-columns:1.1fr .9fr;gap:48px;align-items:center;padding:40px 0}
.hero h1{font-size:44px;line-height:1.12}
.how{display:grid;grid-template-columns:repeat(5,1fr);gap:12px}
.how div{background:#fff;border:1px solid #E5E7EB;border-radius:10px;padding:16px;font-size:13.5px;color:#4B5563}.how b{display:block;color:#111827;font-size:14.5px;margin:6px 0 4px}.how em{font-style:normal;font-size:14px;color:#A8481F;font-weight:700}
.three{display:grid;grid-template-columns:repeat(3,1fr);gap:16px;margin-top:20px}
.pdf{background:#fff;border:1px solid #D1D5DB;box-shadow:0 1px 3px rgba(0,0,0,.06);max-width:640px;margin:0 auto;padding:44px;aspect-ratio:1/1.414}
.kv{display:grid;grid-template-columns:150px 1fr;gap:8px 16px;font-size:16px}.kv dt{color:#4B5563}.kv dd{margin:0}
@media(max-width:700px){.grid .guide{order:-1;position:static;padding:14px;display:grid;grid-template-columns:56px 1fr;gap:4px 12px;align-items:start}.guide .gh{display:contents}.guide img{width:56px;height:56px;grid-row:1/3;padding:3px}.guide .gs{display:none}.guide .gt{font-size:16px}.guide p{font-size:17px;margin:0 0 10px;line-height:1.45}.guide .speak{grid-column:2;justify-self:start;min-height:48px}body{font-size:17px}.scale{grid-template-columns:1fr!important}.scale span{font-size:16px!important;padding:12px!important;text-align:left!important}.top{padding:0 18px;height:56px}.tr .hide{display:none}.wrap{padding:20px 16px 40px}.grid,.hero,.fields,.opts,.k4,.three{grid-template-columns:1fr}.card{padding:22px 18px}h1{font-size:23px}.hero{padding:8px 0;gap:20px}.hero h1{font-size:32px}.how{grid-template-columns:1fr}.step li span{display:none}.lang span{padding:8px 9px!important}.helpt{display:none}.helpm{display:inline!important}.step{margin-bottom:6px}.stepl{margin:0 0 18px!important}.row{flex-direction:column-reverse;align-items:stretch}.row .btn,.row .btn2{justify-content:center}.row .note{text-align:center}.scale{grid-template-columns:repeat(5,1fr);gap:4px}.scale span{font-size:11px;padding:8px 2px}.foot{flex-wrap:wrap;gap:12px;padding:18px 16px}.pdf{padding:22px;aspect-ratio:auto}.kv{grid-template-columns:1fr;gap:2px}.kv dd{margin-bottom:8px}}
"""
STEPS=["Start","Over jou","Werk en leren","Tests","Resultaat"]
def stepper(cur,p):
    h='<ol class="step">'
    for i,s in enumerate(STEPS):
        c='done' if i<cur else ('cur' if i==cur else '')
        h+=f'<li class="{c}" style="--p:{p}%"><i></i><span>{i+1}. {s}</span></li>'
    return h+f'</ol><div class="stepl" style="font-size:17px;color:#111827;margin:-14px 0 22px"><b>Stap {cur+1} van 5</b> · {STEPS[cur]}</div>'
def top(logged=True):
    LG='<span class="lang" style="display:inline-flex;border:1.5px solid #9CA3AF;border-radius:8px;overflow:hidden;font-weight:600"><span style="padding:10px 12px;background:#111827;color:#fff">NL</span><span style="padding:10px 12px">EN</span></span>'
    HP='<span class="btn2" style="min-height:44px"><span class="helpt">Hulp nodig?</span><span class="helpm">Hulp</span></span>'
    r=(LG+HP+'<div class="av">SW</div>') if logged else (LG+'<span class="hide">Voor werkgevers</span><span class="btn2" style="min-height:44px">Inloggen</span>')
    return f'<header class="top"><div class="logo">Lobsy<span>.</span></div><div class="tr">{r}</div></header>'
FOOT='<footer class="foot"><span>Privacyverklaring</span><span>Algemene voorwaarden</span><span>Toegankelijkheid</span><span>Contact</span><span>© 2026 Lobsy</span></footer>'
def page(title,body,logged=True):
    mg=MG.pop() if MG else '';body=body.replace('<div class="grid">',mg+'<div class="grid">',1) if mg else body
    return f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{title}</title><style>{CSS}</style></head><body>{top(logged)}<main class="wrap">{body}</main>{FOOT}{JS}</body></html>'
def chips(items,on=()):
    return '<div class="chips">'+''.join(f'<span class="chip{" on" if x in on else ""}">{x}</span>' for x in items)+'</div>'
def opts(items,on=()):
    return '<div class="opts">'+''.join(f'<div class="opt{" on" if (a in on) else ""}"><span class="bx"></span><div><b>{a}</b>{f"<small>{b}</small>" if b else ""}</div></div>' for a,b in items)+'</div>'
SAVE='<span class="note">Je antwoorden worden bewaard. Pauzeren kan altijd.</span>'
def nav(primary,back=True):
    return f'<div class="row">{"<span class=btn2>Terug</span>" if back else "<span></span>"}{SAVE}<span class="btn">{primary}</span></div>'
def flow(cur,p,eyebrow,h,lead,inner,tip,primary="Verder",back=True):
    return stepper(cur,p)+f'<div class="grid"><section class="card"><p class="eyebrow">{eyebrow}</p><h1>{h}</h1>{f'<p class="lead">{lead}</p>' if lead else '<div style="height:14px"></div>'}{inner}{nav(primary,back)}</section>'+guide(tip)+'</div>'
SP='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><path d="M4 9v6h4l5 4V5L8 9H4z"/><path d="M16.5 8.5a5 5 0 0 1 0 7M19 6a8.5 8.5 0 0 1 0 12"/></svg>'
JS="<script>function lees(b){var t=b.dataset.t||b.parentNode.querySelector('p').textContent;if(speechSynthesis.speaking){speechSynthesis.cancel();b.classList.remove('on');return}var u=new SpeechSynthesisUtterance(t);u.lang='nl-NL';u.rate=0.92;var v=speechSynthesis.getVoices().find(function(x){return x.lang&&x.lang.toLowerCase().indexOf('nl')==0});if(v)u.voice=v;u.onend=function(){b.classList.remove('on')};b.classList.add('on');speechSynthesis.speak(u)}</script>"
GI=[0];MG=[]
def guide(t):
    k=GI[0];GI[0]+=1;img=f"assets/kreeft-{k:02d}.webp"
    m=f'<details class="mg"{{OPEN}}><summary><img src="{img}" alt=""><span>Waarom vragen we dit?</span><i aria-hidden="true">⌄</i></summary><p>{t}</p></details><button class="mspk" type="button" onclick="lees(this)" aria-label="Voorlezen" data-t="{t}">{SP}</button>'
    MG.append('<div class="mgw">'+m+'</div>')
    return f'<aside class="side guide"><div class="gh"><img src="{img}" alt="Kreeft, je gids"><div><div class="gt">Je gids</div><div class="gs">Zo gebruiken we dit</div></div></div><p>{t}</p><button class="speak" type="button" onclick="lees(this)" aria-label="Voorlezen">{SP}Voorlezen</button></aside>'
def field(l,v): return f'<div><label class="f">{l}</label><div class="in">{v}</div></div>'
S=[]
def add(slug,title,html): S.append((slug,title,html))
add("start","Startpagina",page("Lobsy",f'''<div class="hero"><div><p class="eyebrow">Voor werkzoekenden</p><h1>Werk dat bij je past. Rustig gevonden.</h1><p class="lead" style="font-size:17px">Je vertelt eerst wie je bent en wat je kunt, daarna pas kijk je naar vacatures. Voor iedereen, met of zonder diploma, jong of wat ouder.</p><div style="display:flex;gap:12px;flex-wrap:wrap"><span class="btn">Account maken</span><span class="btn2">Ik heb al een account</span></div><p class="note" style="margin-top:14px">Gratis. Ongeveer 20 minuten. Je kunt altijd pauzeren en later verder.</p></div>
<div class="card"><p class="eyebrow">Zo werkt het</p><div style="display:grid;gap:14px">{"".join(f'<div style="display:flex;gap:14px"><span class="av" style="flex:none">{i+1}</span><div><b>{a}</b><div class="note">{b}</div></div></div>' for i,(a,b) in enumerate([("Start","Account en toestemming"),("Over jou","Wanneer en waar je kunt werken"),("Werk en leren","Ervaring, opleiding en droombaan"),("Tests","Vier korte tests, zonder goed of fout"),("Resultaat","Jouw Kompas en je paspoort")]))}</div></div></div>
<div class="three">{"".join(f'<div class="side"><h4>{a}</h4>{b}</div>' for a,b in [("Eerst jij, dan het werk","Werkgevers zien meer dan een cv."),("Geen goed of fout","Je ziet woorden die bij je passen, geen cijfers."),("Jij houdt de regie","Jij bepaalt wat werkgevers zien.")])}</div>''',False))
add("inloggen","Inloggen",page("Inloggen",f'''<div style="max-width:440px;margin:24px auto"><div class="card"><div style="display:flex;border-bottom:1px solid #E5E7EB;margin:-8px 0 24px"><span style="padding:10px 0;margin-right:22px;border-bottom:2px solid #A8481F;font-weight:600">Inloggen</span><span style="padding:10px 0;color:#4B5563">Account maken</span></div><h1>Fijn dat je er bent</h1><p class="lead">Log in met je e-mailadres. Nog geen account? Dat maak je in een minuut.</p><div style="display:grid;gap:16px">{field("E-mailadres","sam@voorbeeld.nl")}{field("Wachtwoord","••••••••••")}</div><div style="display:flex;justify-content:space-between;margin:16px 0 22px;font-size:16px"><span>&#9744; Ingelogd blijven</span><span class="add">Wachtwoord vergeten?</span></div><span class="btn" style="width:100%;justify-content:center">Inloggen</span><p class="note" style="margin-top:16px">Lukt het niet? Je adviseur of de helpdesk helpt je graag.</p></div></div>''',False))
add("start-toestemming","Start: welkom en toestemming",page("Start",flow(0,60,"Start","Welkom, Sam","In vijf korte stappen kom je tot je Kompas en paspoort. Onderweg leer je jezelf beter kennen.",
 '<h2>Voor je begint</h2>'+opts([("Ik snap dat Lobsy mijn antwoorden gebruikt om werk te vinden dat bij me past.",""),("Werkgevers zien pas iets als jij dat goed vindt.",""),("Lobsy mag me af en toe een tip sturen per e-mail.","Niet verplicht. Je kunt dit later aanpassen.")],on=("Ik snap dat Lobsy mijn antwoorden gebruikt om werk te vinden dat bij me past.","Werkgevers zien pas iets als jij dat goed vindt.")).replace('class="opts"','class="opts" style="grid-template-columns:1fr"')+'<p style="margin-top:16px"><span class="add">Lees wat er met je gegevens gebeurt</span></p>',
 "Hoi Sam, ik loop met je mee. Je toestemming bepaalt wat we met je antwoorden doen. Werkgevers zien niets zonder jouw ja.","Beginnen",False)))
add("over-jou-kennismaken","Over jou 1: kennismaken",page("Over jou",flow(1,33,"Over jou · stap 1 van 3","Even kennismaken","Eerst wat basisgegevens.",
 '<div class="fields">'+field("Voornaam","Sam")+field("Achternaam","de Wit")+field("Postcode","2611 AB")+field("Woonplaats","Delft")+field("Hoe wil je aangesproken worden?","Sam")+'</div><h2>Welke taal spreek je het liefst?</h2>'+chips(["Nederlands","English","Beide"],["Nederlands"]),
 "Met je postcode zoeken we werk dat goed te bereizen is. Je naam zien werkgevers pas na een match.")))
add("over-jou-wanneer","Over jou 2: wanneer",page("Over jou",flow(1,66,"Over jou · stap 2 van 3","Wanneer kun je werken?","Kies wat nu bij je leven past. Je kunt het later altijd aanpassen.",
 '<h2 style="margin-top:0">Hoeveel uur per week?</h2>'+chips(["Tot 16 uur","16 tot 32 uur","32 uur of meer","Weet ik nog niet"],["16 tot 32 uur"])+'<h2>Op welke momenten?</h2>'+chips(["Doordeweeks overdag","Avonden","Weekenden","Wisselende diensten"],["Doordeweeks overdag","Weekenden"])+'<h2>Vanaf wanneer?</h2>'+chips(["Zo snel mogelijk","Binnen een maand","Later"],["Zo snel mogelijk"]),
 "Met je uren en momenten laten we alleen werk zien dat in je week past. Je kunt dit altijd aanpassen.")))
add("over-jou-vervoer","Over jou 3: vervoer",page("Over jou",flow(1,100,"Over jou · stap 3 van 3","Hoe kom je op je werk?","Dan zie je werk binnen een afstand die voor jou haalbaar is.",
 chips(["Fiets of e-bike","Openbaar vervoer","Auto","Lopend"],["Fiets of e-bike","Openbaar vervoer"])+'<h2>Hoe lang wil je maximaal reizen, enkele reis?</h2>'+chips(["15 min","30 min","45 min","1 uur"],["30 min"]),
 "We rekenen met je vervoer en reistijd. Zo zie je geen werk dat te ver weg is.")))
add("werk","Werk en leren 1: werkervaring",page("Werk en leren",flow(2,33,"Werk en leren · stap 1 van 3 · optioneel","Heb je al gewerkt?","Bijbaan, vrijwilligerswerk of stage telt ook mee.",
 '<div class="list"><div><b>Magazijnmedewerker bij Distributie Delfland</b><small>2019 tot nu · 24 uur per week</small></div><span class="add">Wijzig</span></div><div class="list"><div><b>Vrijwilliger bij de voedselbank</b><small>2021 tot nu · zaterdag</small></div><span class="add">Wijzig</span></div><p><span class="add">+ Werkervaring toevoegen</span></p><h2>Waar ben je goed in geworden?</h2>'+chips(["Plannen","Samenwerken","Klanten helpen","Netjes werken","Techniek"],["Plannen","Samenwerken","Netjes werken"]),
 "Je ervaring zetten we in je paspoort. We zoeken er werk bij waarin je dit al kunt.","Verder of overslaan")))
add("leren","Werk en leren 2: opleiding",page("Werk en leren",flow(2,66,"Werk en leren · stap 2 van 3 · optioneel","Wat is je opleiding?","Ook cursussen en dingen die je zelf hebt geleerd tellen mee.",
 '<h2 style="margin-top:0">Hoogst afgeronde opleiding</h2>'+chips(["Basisschool","Vmbo of mbo 1","Mbo 2 tot 4","Havo of vwo","Hbo of wo","Anders"],["Vmbo of mbo 1"])+'<h2>Cursussen en certificaten</h2><div class="list"><div><b>Heftruckcertificaat</b><small>2020</small></div><span class="add">Wijzig</span></div><div class="list"><div><b>EHBO</b><small>2023</small></div><span class="add">Wijzig</span></div><p><span class="add">+ Cursus toevoegen</span></p>',
 "Met je opleiding en cursussen vinden we werk dat past. Geen diploma? Dan kijken we naar wat je kunt.")))
add("droombaan","Werk en leren 3: droombaan",page("Werk en leren",flow(2,100,"Werk en leren · stap 3 van 3","Wat is je droombaan?","Denk groot. In Carrière zie je later een route ernaartoe.",
 field("Mijn droombaan","Coördinator in de zorg")+'<h2>Past bij jou</h2>'+chips(["Planner zorgroutes","Teamleider logistiek","Begeleider dagbesteding"])+'<h2>Populair in de buurt <span class="badge" style="margin-left:6px">Veel vraag in jouw regio</span></h2>'+chips(["Medewerker thuiszorg","Monteur zonnepanelen","Doktersassistent"]),
 "Je droombaan gebruiken we in Carrière. Daar zie je welke stappen je ernaartoe kunt zetten.")))
def q(n,t,on): return f'<div class="q"><div class="n">Vraag {n}</div><p>{t}</p><div class="scale">'+''.join(f'<span class="{"on" if i==on else ""}">{s}</span>' for i,s in enumerate(SC))+'</div></div>'
add("test-intro","Tests: introductie",page("Tests",flow(3,5,"Tests","Vier korte tests","Bij elke zin kies je hoe goed hij bij je past, van 1 tot 5. Er is geen goed of fout.",
 '<div class="k4">'+''.join(f'<div class="q"><div class="n">Test {i+1}</div><p><b style="color:#111827">{a}</b><br>5 zinnen</p></div>' for i,a in enumerate(["Hoe werk jij?","Wat vind je leuk?","Waar voel je je thuis?","Wat vind je belangrijk?"]))+'</div><div class="lklab" style="margin-top:18px"><span>1 = Past niet</span><span>5 = Past heel goed</span></div>',
 "Met je antwoorden maken we je Kompas. Je ziet woorden die bij je passen, geen cijfers.","Begin met test 1")))
EX={'test1-v1': 'Voorbeeld: jullie tillen samen zware dozen tot de klus klaar is.', 'test1-v3': 'Voorbeeld: je ruimt je plek op zodat de volgende ploeg verder kan.', 'test2-v1': 'Voorbeeld: je werkt liever met je handen, bijvoorbeeld monteren, rijden of iets zichtbaars maken.', 'test2-v3': 'Voorbeeld: je experimenteert klein om te zien wat gebeurt.', 'test3-v1': 'Voorbeeld: je mag zelf de volgorde van taken kiezen.', 'test3-v3': 'Voorbeeld: informele afspraken werken voor jou.', 'test4-v1': 'Voorbeeld: je mag zelf de volgorde van taken kiezen.', 'test4-v5': 'Voorbeeld: je streeft naar een persoonlijk record.'}
def tq(slug,title,tn,tname,qn,qt,question,on,tip,last=False):
    pr=int(qn/qt*100)
    btn=''.join(f'<span class="lk{" on" if i==on else ""}" aria-label="{i}{", past niet" if i==1 else ", past heel goed" if i==5 else ""}">{i}</span>' for i in range(1,6))
    ans=f'<div class="lkrow">{btn}</div><div class="lklab"><span>1 = Past niet</span><span>5 = Past heel goed</span></div>'
    inner=f'<div style="margin:-8px 0 22px"><div style="display:flex;justify-content:space-between;font-size:16px;color:#374151;margin-bottom:6px"><b>Vraag {qn} van {qt}</b><span>Test {tn} van 4</span></div><div style="height:6px;background:#E5E7EB;border-radius:3px"><div style="height:6px;width:{pr}%;background:#A8481F;border-radius:3px"></div></div></div><p style="font-size:24px;font-weight:600;line-height:1.35;margin:0;color:#111827">{question}</p><p class="tex">{EX[slug]}</p>'+ans
    if not last: inner+='<p class="note" style="margin-top:16px">Na je keuze ga je vanzelf naar de volgende vraag.</p>'
    add(slug,title,page(tname,flow(3,int((tn-1)*25+qn/qt*25),"Tests",tname,"Hoe goed past elke zin bij jou?",inner,tip,"Volgende test" if last else "Volgende")))
tq("test1-v1","Test 1, vraag 1",1,"Hoe werk jij?",1,5,"Ik werk graag samen met anderen om een klus af te ronden.",0,"Met je antwoorden zien we hoe jij werkt. Dat gebruiken we voor je Kompas en je matches.")
tq("test1-v3","Test 1, vraag 3",1,"Hoe werk jij?",3,5,"Ik blijf kalm als het druk is of er iets misgaat.",4,"Je eerste gevoel is meestal goed. Er is geen goed of fout.")
tq("test2-v1","Test 2, vraag 1",2,"Wat vind je leuk?",1,5,"Ik werk graag met machines, gereedschap of buiten in de praktijk.",0,"Wat je leuk vindt, wijst ons de richting. Zo zoeken we beroepen die bij je passen.")
tq("test2-v3","Test 2, vraag 3",2,"Wat vind je leuk?",3,5,"Ik help graag mensen of werk graag in een team met klanten.",5,"Denk aan dagen waarop de tijd voorbij vloog.")
tq("test3-v1","Test 3, vraag 1",3,"Waar voel je je thuis?",1,5,"Ik werk het liefst zelfstandig, zonder dat iemand steeds meekijkt.",0,"Zo zoeken we een werkplek en een team waar je je prettig voelt.")
tq("test3-v3","Test 3, vraag 3",3,"Waar voel je je thuis?",3,5,"Ik werk het liefst samen met collega’s aan één doel.",4,"Drukte of rust, allebei is goed. Kies wat bij je past.")
tq("test4-v1","Test 4, vraag 1",4,"Wat vind je belangrijk?",1,5,"Ik wil zelf kunnen bepalen hoe ik mijn werk aanpak.",0,"Wat je belangrijk vindt, vergelijken we met wat werkgevers bieden.")
tq("test4-v5","Test 4, vraag 5",4,"Wat vind je belangrijk?",5,5,"Ik wil dat mijn werk iets goeds doet voor mens of milieu.",3,"Laatste vraag. Daarna zie je je Kompas.",last=True)
K=[("Hoe werk jij?","Je plant graag vooruit en werkt fijn samen.",["Planner","Teamspeler","Zorgvuldig"]),("Wat vind je leuk?","Je helpt graag en maakt dingen in orde.",["Helpen","Regelen","Met je handen"]),("Waar voel je je thuis?","In een levendig team met een leidinggevende die meedenkt.",["Levendig","Samen","Meedenken"]),("Wat vind je belangrijk?","Zekerheid, betekenis en fijne collega's.",["Zekerheid","Betekenis","Collega's"])]
kc='<div class="k4">'+''.join(f'<div class="q"><div class="n">{a}</div><p>{b}</p><div class="tags">{"".join(f"<span class=\'tag t\'>{t}</span>" for t in c)}</div></div>' for a,b,c in K)+'</div>'
R=[("Zorg & welzijn",True,"Mensen helpen en dingen goed regelen komen hier samen."),("Groene techniek",True,"Met je handen werken aan iets dat ertoe doet."),("Organiseren & samenwerken",False,"Plannen, ordenen en een team draaiend houden.")]
rc='<div class="three" style="margin-top:0">'+''.join(f'<div class="q" style="margin:0"><p style="margin:0 0 4px">{a}</p>{"<span class=badge>Veel vraag in jouw regio</span>" if v else ""}<div class="note" style="margin:8px 0 12px">{d}</div><span class="add">Bekijk vacatures</span></div>' for a,v,d in R)+'</div>'
add("kompas","Resultaat: Jouw Kompas",page("Kompas",stepper(4,50)+'<div class="grid">'+f'<section class="card"><p class="eyebrow">Resultaat · onboarding en tests afgerond</p><h1>Jouw Kompas</h1><p class="lead">Wat je vier tests samen laten zien. Geen cijfers, wel woorden die bij je passen.</p>{kc}<h2>Richtingen die bij je passen</h2>{rc}<div class="q" style="margin-top:20px;background:#F9FAFB"><div class="n">Je droom</div><p style="margin:4px 0">Coördinator in de zorg</p><div class="note">Je Kompas wijst dezelfde kant op. In Carrière zie je welke stappen je kunt zetten.</div></div><div class="row"><span class="btn2">Naar Carrière</span><span class="btn2">Naar vacatures</span><span class="btn">Bekijk je paspoort</span></div></section>'+guide("Dit is je Kompas. Hiermee zoeken we vacatures die bij je passen. Je ziet ze straks bij Zoeken.")+'</div>'))
add("paspoort","Resultaat: paspoort",page("Paspoort",stepper(4,100)+f'''<div class="grid"><section class="card"><p class="eyebrow">Resultaat · klaar om te delen</p><h1>Je paspoort</h1><p class="lead">Dit is wie je bent, in je eigen woorden. Jij bepaalt wat werkgevers zien.</p>
<div style="border:1px solid #E5E7EB;border-radius:10px;padding:22px"><div style="display:flex;gap:14px;align-items:center;margin-bottom:18px"><div class="av" style="width:48px;height:48px;font-size:16px">SW</div><div><b style="font-size:18px">Sam de Wit</b><div class="note">Delft · fiets en ov, tot 30 minuten · 16 tot 32 uur</div></div></div>
<dl class="kv"><dt>Werkervaring</dt><dd>Magazijnmedewerker, vrijwilliger voedselbank</dd><dt>Opleiding</dt><dd>Vmbo, heftruckcertificaat, EHBO</dd><dt>Droom</dt><dd>Coördinator in de zorg</dd><dt>Beschikbaar</dt><dd>Zo snel mogelijk, overdag en weekend</dd></dl>
<div class="k4" style="margin-top:18px">{"".join(f'<div><div class="n note" style="font-weight:600">{a}</div><div class="tags">{"".join(f"<span class=tag>{t}</span>" for t in c)}</div></div>' for a,c in [("Zo werk ik",K[0][2]),("Dit vind ik leuk",K[1][2]),("Hier voel ik me thuis",["Levendig team","Meedenkende leiding"]),("Dit vind ik belangrijk",K[3][2])])}</div><p class="note" style="margin:16px 0 0">Bijgewerkt 25 oktober 2026</p></div>
<div class="row"><span class="btn2">Paspoort aanpassen</span><span></span><span class="btn">Download als pdf</span></div></section>
<div style="display:grid;gap:20px;align-content:start">'''+guide("Je paspoort laat werkgevers zien wie je bent. Jij kiest wie het ziet. Je naam blijft verborgen tot een match.")+f'''<aside class="side"><h4>Zichtbaarheid</h4>{opts([("Zichtbaar voor werkgevers","Werkgevers met een passende vacature kunnen je paspoort zien."),("Naam pas tonen na een match","Tot die tijd zien werkgevers alleen je voornaam.")],on=("Zichtbaar voor werkgevers","Naam pas tonen na een match")).replace('class="opts"','class="opts" style="grid-template-columns:1fr"')}</aside></div></div>'''))
add("paspoort-pdf","Resultaat: paspoort als pdf",page("Paspoort pdf",f'''<p class="note" style="text-align:center;margin:0 0 16px">Voorbeeld van de pdf · A4</p><div class="pdf"><div style="display:flex;justify-content:space-between;border-bottom:2px solid #A8481F;padding-bottom:12px;margin-bottom:22px"><div class="logo">Lobsy<span>.</span></div><span class="note">Paspoort · 25 oktober 2026</span></div><h1 style="font-size:26px">Sam de Wit</h1><p class="note" style="margin:0 0 22px">Delft · fiets en ov, tot 30 minuten · 16 tot 32 uur</p>
<dl class="kv"><dt>Werkervaring</dt><dd>Magazijnmedewerker bij Distributie Delfland, 2019 tot nu<br>Vrijwilliger bij de voedselbank, 2021 tot nu</dd><dt>Opleiding</dt><dd>Vmbo · heftruckcertificaat (2020) · EHBO (2023)</dd><dt>Droom</dt><dd>Coördinator in de zorg</dd><dt>Beschikbaar</dt><dd>Zo snel mogelijk, overdag en weekend</dd></dl>
<h2 style="font-size:15px;border-top:1px solid #E5E7EB;padding-top:18px">Kompas</h2>{"".join(f'<p style="margin:0 0 10px;font-size:16px"><b>{a}</b> {b}</p>' for a,b,c in K)}
<h2 style="font-size:15px">Richtingen</h2><p style="font-size:16px;margin:0">Zorg & welzijn · Groene techniek · Organiseren & samenwerken</p><p class="note" style="margin-top:28px">Gedeeld door Sam via Lobsy. Sam bepaalt zelf wat in dit paspoort staat.</p></div><div style="text-align:center;margin-top:20px"><span class="btn">Download pdf</span></div>'''))
meta=[]
for i,(slug,title,h) in enumerate(S,1):
    f=f"{i:02d}-{slug}.html";open("src/"+f,"w").write(h.replace("{OPEN}",""));meta.append({"num":i,"slug":slug,"title":title,"file":f})
    if i==4:
        f="04b-over-jou-uitgeklapt.html";open("src/"+f,"w").write(h.replace("{OPEN}"," open"));meta.append({"num":"04b","slug":"over-jou-uitgeklapt","title":"Over jou 1: gids uitgeklapt","file":f,"mobileOnly":True})
json.dump(meta,open("src/screens.json","w"),ensure_ascii=False,indent=1)
print(len(meta))
