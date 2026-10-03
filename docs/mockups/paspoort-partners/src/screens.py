def wrap(body,w,h,extra=""):
    return f'<!doctype html><html><head><meta charset="utf-8"><link rel="stylesheet" href="style.css"><style>body{{width:{w}px;height:{h}px;overflow:hidden}}{extra}</style></head><body>{body}</body></html>'
SB='<div class="sbar"><span>9:41</span><span>▮▮▮ ◔</span></div>'
def phone(inner, url=None):
    u = f'<div style="margin:0 12px 6px;background:#f3f0f8;border-radius:10px;padding:5px 10px;font-size:10.5px;color:var(--muted)">🔒 {url}</div>' if url else ''
    return f'<div class="phone"><div class="screen">{SB}{u}{inner}</div></div>'
def col(cap, sub, ph): return f'<div><div class="caption">{cap}<small>{sub}</small></div>{ph}</div>'
LOGOBAR = '<div style="display:flex;align-items:center;gap:8px;padding:6px 16px 10px;border-bottom:1px solid var(--line)"><img src="lobsy.png" style="width:26px"><b class="disp" style="color:var(--purple);font-size:17px;font-weight:900">Lobsy</b>{x}</div>'
TITLE = lambda t, s: f'<div style="padding:8px 4px 18px 4px"><div class="disp" style="font-size:24px;font-weight:900;color:var(--ink)">{t}</div><div style="color:var(--muted);font-size:13px;margin-top:3px">{s}</div></div>'
NOTE = '<div style="position:absolute;bottom:10px;left:36px;font-size:10px;color:#a49cb5">MOCKUP · fictieve data · alle namen, bedrijven en codes zijn verzonnen</div>'

# ---------- (d) verification + share/access ----------
v1 = LOGOBAR.format(x='<span class="sp"></span><span class="chip">PL · NL · EN</span>') + '''
<div style="padding:12px 16px;overflow:hidden">
<div style="background:#e9f6f2;border:1.5px solid #9fd8c4;border-radius:14px;padding:10px 12px;display:flex;gap:10px;align-items:center">
<div class="seal" style="width:34px;height:34px">✓</div><div><b style="color:#0b5c46">Echt Lobsy-paspoort · live</b><div class="sub">LB-48213 · gecontroleerd 3 okt 2026, 09:12</div></div></div>
<div style="display:flex;gap:10px;align-items:center;margin-top:14px"><div class="avatar" style="width:48px;height:48px;font-size:18px;border-radius:14px">MK</div>
<div><div class="disp" style="font-size:19px;font-weight:900;color:var(--ink)">Marta Kowalska</div><div class="sub">Westland · beschikbaar: direct · 32–40 u</div></div></div>
<div class="card p" style="margin-top:12px"><div class="lbl">Lobsy-geverifieerd</div>
<div class="row" style="margin-top:4px"><span>🦞 Competenties</span><span class="sub">✓ 21 sep 2026</span></div>
<div class="row"><span>📡 Loopbaaninteresse</span><span class="sub">✓ 22 sep 2026</span></div>
<div class="row"><span>🪨 Werkcultuur</span><span class="sub">✓ 25 sep 2026</span></div>
<div class="row"><span>🧭 Waarden</span><span class="sub">✓ 28 sep 2026</span></div>
<div class="row" style="margin-top:3px"><span>E-mail & telefoon</span><span class="sub">✓ bevestigd</span></div>
<div class="sub" style="margin-top:4px">Identiteit en diploma's niet door Lobsy gecontroleerd.</div></div>
<div class="card" style="margin-top:8px"><div class="row"><b>Paspoort (2 pagina's)</b><span class="chip">PDF ↓</span></div><div class="sub">Gewijzigd sinds je PDF? <b style="color:var(--coral)">Ja</b> · beschikbaarheid bijgewerkt 2 okt</div></div>
<div class="card c" style="margin-top:8px;font-size:11px">Gedeeld door Marta met <b>Kasflex Uitzendbureau</b> · toegang t/m 2 nov 2026. Alleen het paspoort; testantwoorden blijven privé. Geen score of ranking: gespreksinput.</div>
<div style="margin-top:10px" class="btn pri">Contact opnemen met Marta</div>
</div>'''
v2 = LOGOBAR.format(x='') + '''
<div style="padding:16px">
<div style="background:#e9f6f2;border:1.5px solid #9fd8c4;border-radius:14px;padding:12px;display:flex;gap:10px;align-items:center">
<div class="seal" style="width:34px;height:34px">✓</div><div><b style="color:#0b5c46">Dit paspoort is echt</b><div class="sub">LB-48213 · PDF gemaakt op 3 okt 2026</div></div></div>
<div style="text-align:center;margin-top:26px"><div style="font-size:44px">🔒</div>
<div class="disp" style="font-size:18px;font-weight:900;color:var(--ink);margin-top:6px">De inhoud is privé</div>
<div class="sub" style="font-size:12px;margin:6px 10px 0">De kandidaat bepaalt wie de live-versie mag zien. Jouw link is verlopen of je hebt (nog) geen toegang.</div></div>
<div class="card" style="margin-top:20px"><div class="lbl">Toegang vragen</div>
<div style="margin-top:6px;border:1.5px solid var(--line);border-radius:10px;padding:8px;color:var(--muted)">Jouw naam en organisatie</div>
<div style="margin-top:6px;border:1.5px solid var(--line);border-radius:10px;padding:8px;color:var(--muted)">Waarom wil je meekijken? (optioneel)</div>
<div class="btn pri" style="margin-top:10px">Vraag toegang aan Marta</div>
<div class="sub" style="margin-top:6px">Marta krijgt een melding en kiest zelf: ja, nee of negeren. Jij ziet nooit haar contactgegevens zonder haar ja.</div></div>
<div class="sub" style="text-align:center;margin-top:12px">Geen naam, geen gegevens zonder toestemming — alleen de echtheid.</div></div>'''
row = lambda name, kind, until, on, extra='': f'''<div class="card" style="margin-top:8px;padding:10px"><div class="row" style="align-items:center"><div><b style="color:var(--ink)">{name}</b> <span class="chip o" style="font-size:9px;padding:1px 6px">{kind}</span><div class="sub">{until}</div>{extra}</div><div class="toggle {'on' if on else ''}"></div></div></div>'''
v3 = LOGOBAR.format(x='<span class="sp"></span><span class="sub">Mijn paspoort</span>') + f'''
<div style="padding:12px 16px">
<div class="disp" style="font-size:19px;font-weight:900;color:var(--ink)">Delen & toegang</div>
<div class="sub" style="font-size:11.5px">Jij bepaalt wie je paspoort ziet. Intrekken kan altijd.</div>
{row("Kasflex Uitzendbureau","uitzendbureau","via partnercode · t/m 2 nov 2026",True,'<div class="sub" style="color:var(--purple)">2× bekeken · laatst 2 okt, 14:05</div>')}
{row("Van der Voorbeeld Kwekerijen","werkgever","deellink · t/m 17 okt 2026",True,'<div class="sub" style="color:var(--purple)">1× bekeken · 1 okt</div>')}
{row("Toegangsverzoek: J. de Recruiter","onbekend","wacht op jouw antwoord",False,'<div style="display:flex;gap:6px;margin-top:5px"><span class="chip g">Toestaan</span><span class="chip c">Weigeren</span></div>')}
<div class="card p" style="margin-top:10px"><div class="lbl">Wat de ander ziet</div>
<div class="row" style="margin-top:6px"><span>Paspoort pagina 1 (recruiter)</span><span class="sub">altijd</span></div>
<div class="row" style="margin-top:4px;align-items:center"><span>Telefoon & e-mail</span><div class="toggle on"></div></div>
<div class="row" style="margin-top:4px;align-items:center"><span>Pagina 2 (verdieping)</span><div class="toggle on"></div></div>
<div class="row" style="margin-top:4px;align-items:center"><span>Werkervaring</span><div class="toggle on"></div></div>
<div class="sub" style="margin-top:5px">Testantwoorden en ruwe scores deel je nooit — ook niet als je dat zou willen.</div></div>
<div style="display:flex;gap:8px;margin-top:10px"><div class="btn sec" style="flex:1">+ Nieuwe deellink</div><div class="btn ghost" style="flex:1">Alles intrekken</div></div>
<div class="sub" style="margin-top:6px">Deellinks verlopen standaard na 30 dagen.</div></div>'''
d = f'''<div class="canvas" style="height:1000px;position:relative"><div style="display:flex;gap:44px;justify-content:center">
{col("1 · Recruiter scant de QR (met toegang)","lobsy.nl/v/… — live, met echtheidscontrole",phone(v1,"lobsy.nl/v/7KQ-M2P"))}
{col("2 · Iemand zonder toegang scant","Alleen echtheid, geen inhoud",phone(v2,"lobsy.nl/v/7KQ-M2P"))}
{col("3 · Kandidaat: Delen & toegang","Wie ziet wat, hoe lang, intrekken",phone(v3,"lobsy.nl/candidate/paspoort/delen"))}
</div>{NOTE}</div>'''
open('d.html','w').write(wrap(d,1300,1000))

# ---------- (e) start with partner code + consent ----------
def welcome():
    return f'''<div style="background:linear-gradient(160deg,#f4effb,#fff1ec);padding:14px 16px 18px">
<div style="display:flex;align-items:center;gap:10px"><img src="lobsy.png" style="width:34px"><span style="color:#c9bfdc;font-size:20px">×</span><img src="kasflex.svg" style="height:34px"></div>
<div class="chip o" style="margin-top:10px;font-size:10px">Partner: uitzendbureau · Westland</div>
<div class="disp" style="font-size:23px;font-weight:900;color:var(--ink);margin-top:10px;line-height:1.1">Cześć! Zrób swój darmowy paszport DNA</div>
<div class="sub" style="font-size:11.5px;margin-top:4px">Hoi! Maak gratis je DNA-paspoort · Kasflex nodigt je uit</div></div>
<div style="padding:14px 16px">
<div class="lbl">Twój język · Jouw taal</div>
<div style="display:flex;gap:6px;margin-top:6px;flex-wrap:wrap"><span class="chip" style="background:var(--purple);color:#fff">Polski</span><span class="chip o">Română</span><span class="chip o">English</span><span class="chip o">Nederlands</span><span class="chip o">العربية</span></div>
<div class="lbl" style="margin-top:14px">Kod partnera · Partnercode</div>
<div style="margin-top:6px;border:2px solid var(--purple);border-radius:12px;padding:10px 12px;display:flex;justify-content:space-between;font:800 18px Nunito;color:var(--ink);letter-spacing:.12em">KAS-FLX<span style="color:var(--green);font-size:13px;letter-spacing:0">✓ Kasflex</span></div>
<div class="sub" style="margin-top:4px">Automatisch ingevuld via de link of QR op de flyer.</div>
<div class="card p" style="margin-top:14px"><b style="color:var(--ink)">🦞 Wat krijg je?</b><ul class="why"><li>4 korte tests: ontdek je sterke kanten</li><li>Een paspoort voor bij je CV, in 2 talen</li><li>Altijd gratis voor jou</li></ul></div>
<div class="btn pri" style="margin-top:14px">Zaczynam · Ik begin</div>
<div class="sub" style="text-align:center;margin-top:8px">Zonder code? Lobsy werkt ook gewoon zonder partner.</div></div>'''
def consent(partner, kind, logo):
    return f'''<div style="padding:10px 16px">
<div style="display:flex;align-items:center;gap:8px">{logo}<span class="chip o" style="font-size:10px">{kind}</span></div>
<div class="disp" style="font-size:20px;font-weight:900;color:var(--ink);margin-top:10px;line-height:1.15">Mag {partner} je paspoort zien?</div>
<div class="sub" style="font-size:11.5px;margin-top:3px">Jouw keuze. Lobsy blijft ook gratis als je “nee” zegt.</div>
<div class="card m" style="margin-top:10px;padding:9px 11px"><b style="color:#0b5c46">✓ Wat {partner} ziet</b><div style="font-size:11px">Alleen je DNA-paspoort — precies wat op de PDF staat — en of het af is.</div></div>
<div class="card c" style="margin-top:6px;padding:9px 11px"><b style="color:#c2381f">✕ Wat {partner} nooit ziet</b><div style="font-size:11px">Je testantwoorden, ruwe scores en privé-voorkeuren (zoals wat je niet leuk vindt).</div></div>
<div class="card p" style="margin-top:6px;padding:9px 11px"><b style="color:var(--purple)">⚖ Geen score, geen ranking</b><div style="font-size:11px">Lobsy beoordeelt, rangschikt of filtert je niet voor {partner}. Je paspoort is alleen input voor een gesprek met een mens.</div></div>
<div class="check"><div class="box"></div><div>Ik geef <b>{partner}</b> toestemming om mijn DNA-paspoort te bekijken. Ik kan dit altijd intrekken via <i>Delen & toegang</i>.</div></div>
<div class="check" style="border-style:dashed"><div class="box"></div><div style="color:var(--muted)">Optioneel: {partner} mag mij bellen of appen over werk.</div></div>
<div class="btn pri" style="margin-top:10px;opacity:.55">Ja, deel mijn paspoort</div>
<div class="btn ghost" style="margin-top:6px">Nu niet — wel gratis verder</div>
<div class="sub" style="margin-top:6px;font-size:9.5px">Vakjes staan standaard uit. Toestemming v1.0 · vastgelegd met datum en tijd · <u>privacyverklaring</u></div></div>'''
EMP = '<div style="display:flex;align-items:center;gap:6px"><div style="width:34px;height:34px;border-radius:9px;background:#8a3b12;color:#fff;display:grid;place-items:center;font:900 15px Nunito">VdV</div><div><b style="color:var(--ink);font-size:12px">Van der Voorbeeld</b><div class="sub" style="font-size:9px">Kwekerijen · HR (fictief)</div></div></div>'
e = f'''<div class="canvas" style="height:1000px;position:relative"><div style="display:flex;gap:44px;justify-content:center">
{col("1 · Start via partnerlink of flyer-QR","lobsy.nl/p/KAS-FLX · co-branded, in eigen taal",phone(welcome(),"lobsy.nl/p/KAS-FLX"))}
{col("2 · Expliciete toestemming (uitzendbureau)","Na account aanmaken · hier in NL getoond, kandidaat ziet eigen taal",phone(consent("Kasflex","partner: uitzendbureau",'<img src="kasflex.svg" style="height:30px">')))}
{col("3 · Zelfde flow, partnertype werkgever","HR van een grotere werkgever, eigen code VDV-HR",phone(consent("Van der Voorbeeld","partner: werkgever (HR)",EMP)))}
</div>{NOTE}</div>'''
open('e.html','w').write(wrap(e,1300,1000))

# ---------- (g) partner overview ----------
cands = [("Marta K.","Paspoort af","g","1 okt","Direct","PL · EN · NL basis","Glastuinbouw, logistiek"),
 ("Andrei P.","Paspoort af","g","30 sep","13 okt","RO · EN","Techniek, logistiek"),
 ("Tomasz W.","Bezig · 3/4 tests","c","29 sep","Direct","PL · NL basis","Logistiek"),
 ("Elena D.","Paspoort af","g","27 sep","1 nov","RO · IT · EN","Glastuinbouw"),
 ("Kees de V.","Bezig · 1/4 tests","c","26 sep","Direct","NL · EN","Techniek"),
 ("Oksana M.","Paspoort af","g","24 sep","20 okt","UK · PL · EN","Glastuinbouw, productie")]
rows=''.join(f'<tr><td><b style="color:var(--ink)">{n}</b></td><td><span class="pill" style="background:{"var(--mint);color:var(--green)" if c=="g" else "var(--soft-c);color:#c2381f"}">{s}</span></td><td>{d}</td><td>{a}</td><td>{l}</td><td class="sub" style="font-size:12px">{sec}<div style="font-size:10px;color:#a49cb5">gekozen door kandidaat</div></td><td><span class="chip">Paspoort bekijken →</span></td></tr>' for n,s,c,d,a,l,sec in cands)
kpi = lambda n,l,s,cl='p': f'<div class="card {cl}" style="padding:14px 16px"><div class="disp" style="font-size:30px;font-weight:900;color:var(--ink)">{n}</div><b style="color:var(--ink)">{l}</b><div class="sub">{s}</div></div>'
g = f'''<div style="display:flex;height:740px;background:#f7f5fb;position:relative">
<div style="width:230px;background:#fff;border-right:1px solid var(--line);padding:18px 14px">
<div style="display:flex;align-items:center;gap:8px"><img src="lobsy.png" style="width:30px"><b class="disp" style="color:var(--purple);font-size:19px;font-weight:900">Lobsy</b><span class="chip o" style="font-size:9px">Partner</span></div>
<div style="margin-top:20px;display:flex;flex-direction:column;gap:4px;font-weight:600">
<div style="background:var(--soft-p);color:var(--purple);border-radius:10px;padding:9px 10px">▦ Overzicht</div><div style="padding:9px 10px;color:var(--muted)">🦞 Mijn kandidaten</div><div style="padding:9px 10px;color:var(--muted)">🔗 Partnercode & flyers</div><div style="padding:9px 10px;color:var(--muted)">🎨 Logo & co-branding</div><div style="padding:9px 10px;color:var(--muted)">🏢 Vestigingen</div><div style="padding:9px 10px;color:var(--muted)">📄 Abonnement</div></div>
<div class="card p" style="margin-top:24px;font-size:11px"><b style="color:var(--purple)">Partnertype</b><div style="margin-top:4px;display:flex;gap:4px"><span class="pill" style="background:var(--purple);color:#fff">Uitzendbureau</span><span class="pill" style="background:#fff;color:var(--muted);border:1px solid var(--line)">Werkgever</span></div><div class="sub" style="margin-top:5px">Werkgevers (HR) krijgen exact hetzelfde portaal.</div></div></div>
<div style="flex:1;padding:22px 28px">
<div class="row" style="align-items:center"><div style="display:flex;align-items:center;gap:12px"><img src="kasflex.svg" style="height:42px"><div><div class="disp" style="font-size:22px;font-weight:900;color:var(--ink)">Kasflex Uitzendbureau <span style="font-size:12px;color:var(--muted);font-weight:600">(fictief)</span></div><div class="sub">Vestiging Naaldwijk · abonnement Partner (voorbeeld)</div></div></div><span class="chip o">Week 40 · 2026</span></div>
<div style="margin-top:14px;background:linear-gradient(90deg,#f4effb,#fff1ec);border:1.5px solid #dccdf2;border-radius:14px;padding:12px 16px;display:flex;gap:22px;font-size:12px">
<div><b style="color:var(--purple)">🔒 Alleen jouw eigen kandidaten</b><div class="sub" style="font-size:11px">Alleen wie met jouw code startte én toestemming gaf. Geen zoeken in de Lobsy-pool.</div></div>
<div><b style="color:var(--purple)">📄 Alleen het paspoort</b><div class="sub" style="font-size:11px">Geen testantwoorden, ruwe scores of privé-voorkeuren.</div></div>
<div><b style="color:var(--purple)">⚖ Geen scores of ranking</b><div class="sub" style="font-size:11px">Lobsy sorteert of filtert niet op geschiktheid. Jij voert het gesprek.</div></div></div>
<div class="grid g4" style="margin-top:14px">{kpi(42,"Gestart via jouw code","sinds 1 sep 2026")}{kpi(31,"Toestemming gegeven","alleen deze zie je bij naam","c")}{kpi(19,"Paspoort af","4/4 tests, deelbaar","m")}{kpi(2,"Toestemming ingetrokken","toegang direct gestopt","")}</div>
<div class="grid" style="grid-template-columns:1fr 300px;gap:14px;margin-top:14px">
<div class="card" style="padding:0"><div class="row" style="padding:12px 14px;align-items:center"><b class="disp" style="font-size:16px;color:var(--ink)">Kandidaten met toestemming (31)</b>
<div style="display:flex;gap:6px"><span class="chip">Status: alle ▾</span><span class="chip o">Sortering: laatst gedeeld ▾</span></div></div>
<table><tr><th>Kandidaat</th><th>Paspoort</th><th>Gedeeld op</th><th>Beschikbaar</th><th>Talen</th><th>Sectoren</th><th></th></tr>{rows}</table>
<div class="sub" style="padding:10px 14px">+ 11 kandidaten gestart zonder toestemming: alleen als aantal zichtbaar, nooit bij naam. Sortering alleen op datum/status — nooit op “geschiktheid”.</div></div>
<div><div class="card p" style="text-align:center"><div class="lbl">Jouw partnercode</div><div class="disp" style="font-size:28px;font-weight:900;color:var(--purple);letter-spacing:.1em;margin-top:4px">KAS-FLX</div>
<div style="width:120px;height:120px;margin:8px auto;background:#fff;border-radius:10px;padding:8px"><img src="qr-partner.svg" style="width:100%"></div><div class="sub">lobsy.nl/p/KAS-FLX</div>
<div style="display:flex;gap:6px;justify-content:center;margin-top:8px"><span class="chip">Link kopiëren</span><span class="chip">Flyer PL/RO/EN/NL ↓</span></div></div>
<div class="card" style="margin-top:12px"><b style="color:var(--ink)">Co-branding</b><div class="sub" style="margin-top:3px">Jouw logo staat op het paspoort van je eigen kandidaten: “in samenwerking met Kasflex”.</div><div style="margin-top:8px;display:flex;align-items:center;gap:8px"><img src="lobsy.png" style="width:22px">×<img src="kasflex.svg" style="height:24px"></div></div></div></div>
</div>{NOTE}</div>'''
open('g.html','w').write(wrap(g,1440,740))

# ---------- (h) pricing ----------
def tier(name, price, per, sub, feats, hi=False, cta="Start"):
    fs=''.join(f'<li style="margin:6px 0">{f}</li>' for f in feats)
    return f'''<div class="card" style="padding:22px;{'border:2px solid var(--purple);box-shadow:0 14px 34px rgba(91,42,154,.18);' if hi else ''}position:relative">
{'<div style="position:absolute;top:-12px;left:22px" class="pill" ><span class="pill" style="background:var(--grad);color:#fff">Meest gekozen in pilot</span></div>' if hi else ''}
<div class="disp" style="font-size:21px;font-weight:900;color:var(--ink)">{name}</div><div class="sub" style="font-size:12.5px;min-height:34px">{sub}</div>
<div style="margin-top:10px"><span class="disp" style="font-size:34px;font-weight:900;color:var(--purple)">{price}</span> <span class="sub" style="font-size:12.5px">{per}</span></div>
<div class="chip c" style="margin-top:6px;font-size:10px">voorbeeldbedrag · indicatief</div>
<ul class="why" style="margin-top:12px;font-size:12.5px">{fs}</ul>
<div class="btn {'pri' if hi else 'sec'}" style="margin-top:14px">{cta}</div></div>'''
prin = lambda i,t,s: f'<div style="flex:1"><div style="font-size:22px">{i}</div><b class="disp" style="color:var(--ink);font-size:15px">{t}</b><div class="sub" style="font-size:12px">{s}</div></div>'
h = f'''<div style="background:#fff;height:1240px;position:relative;font-size:13px">
<div style="display:flex;align-items:center;gap:10px;padding:16px 60px;border-bottom:1px solid var(--line)"><img src="lobsy.png" style="width:32px"><b class="disp" style="color:var(--purple);font-size:21px;font-weight:900">Lobsy</b><span class="sp"></span>
<span class="sub" style="margin-right:20px;font-size:13px">Kandidaten</span><span class="sub" style="margin-right:20px;font-size:13px">Scholen</span><b style="color:var(--purple);margin-right:20px">Partners</b><span class="btn pri" style="padding:8px 16px;font-size:12px">Plan een demo</span></div>
<div style="text-align:center;padding:40px 0 10px"><div class="chip o">lobsy.nl/partners/prijzen</div>
<div class="disp" style="font-size:40px;font-weight:900;color:var(--ink);margin-top:10px">Lobsy voor uitzendbureaus & werkgevers</div>
<div class="sub" style="font-size:16px;margin-top:6px">Jouw kandidaten maken gratis hun DNA-paspoort. Jij krijgt een sterker gesprek — in hun taal én de jouwe.</div>
<div style="display:inline-flex;margin-top:18px;background:var(--soft-p);border-radius:999px;padding:4px"><span class="pill" style="background:#fff;color:var(--purple);padding:7px 18px;font-size:13px;box-shadow:0 1px 3px rgba(0,0,0,.1)">Uitzendbureau</span><span class="pill" style="color:var(--muted);padding:7px 18px;font-size:13px">Werkgever (HR)</span></div>
<div class="sub" style="margin-top:6px">Zelfde pakketten en prijzen voor beide partnertypes.</div></div>
<div style="margin:18px 60px 0;background:var(--grad);border-radius:16px;padding:16px 22px;color:#fff;display:flex;align-items:center;gap:16px">
<div style="font-size:28px">🦞</div><div style="flex:1"><b class="disp" style="font-size:18px">Pilot Westland 2026: de eerste 5 partners 3 maanden gratis Partner</b><div style="opacity:.9;font-size:12.5px">In ruil voor feedback (2 korte gesprekken) en een quote die we mogen gebruiken. Daarna 50% korting t/m juni 2027 (voorbeeld).</div></div>
<span class="btn" style="background:#fff;color:var(--purple);padding:9px 16px;font-size:13px">Meld je aan voor de pilot</span></div>
<div class="grid g3" style="margin:30px 60px 0;gap:22px">
{tier("Gratis","€0","",
 "De kandidaat deelt zelf zijn paspoort met jou.",
 ["Paspoort bekijken via deellink of QR","Echtheidscontrole (Lobsy-geverifieerd)","Tweetalig: taal kandidaat + NL/EN","Geen account nodig om te bekijken","<span class='sub'>Geen eigen code, logo of overzicht</span>"],cta="Niets te doen")}
{tier("Partner","€99–€249","per vestiging / maand",
 "Eigen partnercode, je logo op het paspoort, overzicht van je kandidaten.",
 ["Eigen partnercode + QR + flyers in 5 talen","Co-branded paspoort: “in samenwerking met …”","Overzicht: gestart / af / gedeeld / ingetrokken","Onbeperkt kandidaten — geen prijs per kandidaat","Staffel op aantal actieve kandidaten per maand (bijv. t/m 50 / 150 / 300)"],hi=True,cta="Word partner")}
{tier("Pro","vanaf €500","per maand",
 "Meerdere vestigingen, koppeling met je ATS, later je eigen vacatures.",
 ["Alles uit Partner, voor al je vestigingen","Koppeling met je ATS / planningssysteem","Later: je eigen vacatures op Lobsy tonen aan je eigen kandidaten","Prijs op aantal vestigingen en volume-staffel","Vaste contactpersoon + kwartaalrapport (geanonimiseerd)"],cta="Plan een gesprek")}
</div>
<div style="margin:34px 60px 0;border:1.5px solid #dccdf2;background:var(--soft-p);border-radius:18px;padding:22px 26px">
<b class="disp" style="font-size:18px;color:var(--ink)">Onze spelregels — in elk pakket</b>
<div style="display:flex;gap:22px;margin-top:12px">
{prin("🔒","Alleen jouw eigen kandidaten","Je ziet alleen wie met jóuw code startte én toestemming gaf. Geen zoeken in de Lobsy-pool.")}
{prin("📄","Alleen het paspoort","Nooit testantwoorden, ruwe scores of privé-voorkeuren van de kandidaat.")}
{prin("⚖","Geen scores of ranking","Lobsy beoordeelt, rangschikt of filtert kandidaten niet voor jou. Het paspoort is gespreksinput; een mens beslist.")}
{prin("🚫","Nooit betalen per kandidaat","Je betaalt voor het platform (vestigingen, staffel), niet per aangeleverde persoon.")}
{prin("🎁","Gratis voor kandidaten","Altijd, ook als ze geen toestemming geven of later intrekken.")}
</div></div>
<div class="grid g2" style="margin:28px 60px 0;gap:22px">
<div><b class="disp" style="font-size:15px;color:var(--ink)">Wat als een kandidaat zijn toestemming intrekt?</b><div class="sub" style="font-size:12.5px;margin-top:3px">Dan stopt je toegang direct. Gedownloade PDF's vallen onder jouw eigen AVG-plicht; de QR-link toont dan alleen nog “echt, maar privé”.</div></div>
<div><b class="disp" style="font-size:15px;color:var(--ink)">Kan ik ook kandidaten uit de rest van Lobsy zien?</b><div class="sub" style="font-size:12.5px;margin-top:3px">Nee. Lobsy is geen cv-database. Je werkt alleen met mensen die via jou zijn binnengekomen.</div></div>
</div>
<div style="position:absolute;bottom:18px;left:60px;right:60px;border-top:1px solid var(--line);padding-top:10px;font-size:11px;color:#a49cb5">MOCKUP · Alle bedragen zijn voorbeeldbedragen / indicatief, excl. btw, en geen aanbod. Bedrijfsnamen fictief.</div>
</div>'''
open('h.html','w').write(wrap(h,1440,1240))
print('ok')
