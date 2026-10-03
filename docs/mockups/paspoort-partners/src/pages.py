# Generates A4 passport mockups (a, b, c, f). Fictional data only.
L = {
 'nl': dict(sub="DNA-paspoort · recruiterpagina", ver="Lobsy-geverifieerd", ver2="4/4 DNA-tests afgerond · 28 sep 2026",
   tagline="Praktische doorzetter — werkt nauwkeurig én in tempo", region="Westland · Naaldwijk e.o.", open="Open voor werk",
   avail="Beschikbaar per", availv="Direct", hours="32–40 uur per week", shifts="Diensten", sh=["Ocht.","Midd.","Avond","Nacht"], shnote="avond in overleg · geen nachten",
   transport="Vervoer", transv="Rijbewijs B · fiets", transs="max. 30 min reizen", car="eigen auto: nee",
   contact="Contact", wa="WhatsApp mag", langs="Talen", pl="Pools", en="Engels", nlL="Nederlands", native="moedertaal", nlv="Basis (± A2)", nlx="volgt korte werkinstructies; leert nu richting B1",
   prefs="Werkvoorkeuren", indoor="Binnen (kas / hal)", indoorv="voorkeur", outdoor="Buiten werken", outdoorv="prima, niet bij vorst", phys="Fysiek werk", physv="ja · staand werk, tillen tot ± 15 kg", pace="Tempo / productienorm", pacev="prima", team="Werkomgeving", teamv="vast team, duidelijke taken",
   strengths="Sterke punten", s=["Zorgvuldig werken","Tempo houden","Samenwerken"], kw=["Betrouwbaar","Nauwkeurig","Altijd op tijd","Doorzetter"],
   exp="Ervaring & papieren", e1="Oogstmedewerker", e1b="Kwekerij De Voorbeeldtuin (fictief) · mrt 2024 – nu", e2="Magazijnmedewerker (orderpicken)", e2b="Magazyn Przykład, Polen (fictief) · 2021 – 2023",
   certs="Certificaten", edu="Opleiding", eduv="MBO 1 / Entree (PL-equivalent)",
   scan="Scan voor de live, geverifieerde versie", link="lobsy.nl/v/7KQ-M2P · gedeeld door de kandidaat · geldig t/m 2 nov 2026",
   priv="Bewust níet op dit paspoort: geboortedatum, foto, nationaliteit, BSN, gezondheid. De kandidaat bepaalt wie de live-versie ziet.",
   page="Gemaakt 3 okt 2026 · pagina 1/2"),
}
# Bilingual PL (candidate) + NL (agency): "PL / NL"
def bi(pl, nl): return f'{pl}<span class="lbl2"> · {nl}</span>'
def biv(pl, nl): return f'{pl}<div class="sub" style="font-style:italic">{nl}</div>'

PLMAP=[("avond in overleg · geen nachten","wieczory do uzgodnienia · bez nocy"),
("volgt korte werkinstructies; leert nu richting B1","rozumie krótkie instrukcje; uczy się do B1"),
("voorkeur","preferuję"),("prima, niet bij vorst","ok, nie przy mrozie"),("ja · staand werk, tillen tot ± 15 kg","tak · praca na stojąco, do ± 15 kg"),
("prima","ok"),("vast team, duidelijke taken","stały zespół, jasne zadania"),
("Zorgvuldig werken","Dokładna praca"),("Tempo houden","Utrzymanie tempa"),("Samenwerken","Współpraca"),
("Betrouwbaar","Rzetelna"),("Nauwkeurig","Precyzyjna"),("Altijd op tijd","Zawsze punktualna"),("Doorzetter","Wytrwała"),
("Oogstmedewerker","Pracownik zbiorów"),("Magazijnmedewerker (orderpicken)","Magazynier (kompletacja)"),
("Doe-profiel, zorgvuldig, 2,5 jr oogstervaring","profil praktyczny, 2,5 roku przy zbiorach"),("Orderpicken, heftruckcertificaat, houdt tempo","kompletacja, wózek widłowy, tempo"),("Groeikans: hands-on, wil machines leren","szansa rozwoju: chce obsługiwać maszyny"),
("max. 30 min reizen","maks. 30 min dojazdu")]

def page1(mode='nl', cobrand=False):
    t = L['nl']
    P = mode == 'plnl'
    def lab(nl, pl): return bi(pl, nl) if P else nl
    head_brand = f'''<img class="logo" src="lobsy.png"><div class="brand">Lobsy<small>{"Paszport DNA · strona dla rekrutera" if P else t['sub']}{" · DNA-paspoort" if P else ""}</small></div>'''
    if cobrand:
        head_brand = f'''<img class="logo" src="lobsy.png"><div class="brand">Lobsy<small>DNA-paspoort · recruiterpagina</small></div>
        <div style="font:300 26px Nunito;color:#c9bfdc;margin:0 4px">×</div>
        <div><img src="kasflex.svg" style="height:40px;display:block"><div style="font-size:8.5px;color:var(--muted);margin-top:1px">in samenwerking met Kasflex Uitzendbureau <i>(fictief)</i></div></div>'''
    langswitch = '<div style="display:flex;gap:4px;margin-right:10px"><span class="chip" style="background:var(--purple);color:#fff">PL</span><span class="chip">NL</span><span class="chip o">EN</span></div>' if P else ''
    ribbon = ''
    if cobrand:
        ribbon = '''<div style="margin-top:8px;background:#e9f6f2;border:1px solid #c6e8dc;border-radius:10px;padding:6px 10px;font-size:9.5px;color:#0b3d33;display:flex;gap:8px;align-items:center">
        <b>✓ Gedeeld met Kasflex</b> <span class="chip o" style="font-size:8px;padding:1px 6px">partnertype: uitzendbureau</span> op 1 okt 2026 met uitdrukkelijke toestemming van Marta · intrekbaar · Kasflex ziet alleen dit paspoort, geen testantwoorden</div>'''
    tagline = biv("Praktyczna i wytrwała — pracuje dokładnie i w dobrym tempie", t['tagline']) if P else t['tagline']
    html = f'''<div class="page{' bi' if P else ''}"><div class="mock-tag">Mockup · fictieve kandidaat</div>
<div class="hdr">{head_brand}<div class="sp"></div>{langswitch}
<div class="badge-ver"><div class="seal">✓</div><div><b>{"Zweryfikowane przez Lobsy" if P else t['ver']}</b><span>{"4/4 testy DNA · 28 wrz 2026" if P else t['ver2']}</span>{'<div class="lbl2">Lobsy-geverifieerd · 4/4 tests</div>' if P else ''}</div></div></div>
{ribbon}
<div class="hero"><div class="avatar">MK</div><div style="flex:1">
<h1>Marta Kowalska</h1><div class="tagline">{tagline}<span class="der">AFGELEID</span></div>
<div class="meta"><span class="chip">📍 {t['region']}<span class="der">AFGELEID</span></span><span class="chip g">● {"Otwarta na pracę" if P else t['open']}</span><span class="chip o">Paspoortnr. LB-48213</span></div></div></div>

<div class="grid g4 sec" style="margin-top:14px">
<div class="card p"><div class="lbl">{lab(t['avail'],"Dostępna od")}</div><div class="val">{"Od zaraz" if P else t['availv']}{'<span class="lbl2"> · Direct</span>' if P else ''}</div><div class="sub">{"32–40 godz./tydz." if P else t['hours']}</div></div>
<div class="card p"><div class="lbl">{lab(t['shifts'],"Zmiany")}<span class="der">AFGELEID</span></div>
<div class="shift"><div class="y">{"Rano" if P else t['sh'][0]}</div><div class="y">{"Popoł." if P else t['sh'][1]}</div><div class="mby">{"Wiecz." if P else t['sh'][2]}</div><div class="n">{"Noc" if P else t['sh'][3]}</div></div>
<div class="sub" style="margin-top:4px">{t['shnote']}</div></div>
<div class="card c"><div class="lbl">{lab(t['transport'],"Dojazd")}</div><div class="val">{t['transv'] if not P else "Prawo jazdy B · rower"}</div><div class="sub">{t['transs']} · {t['car']}<span class="new">NIEUW</span></div></div>
<div class="card c"><div class="lbl">{lab(t['contact'],"Kontakt")}</div><div class="val" style="font-size:11.5px">+31 6 0000 0000</div><div class="sub">✓ {t['wa']} · marta@example.com</div></div>
</div>

<div class="grid g2 sec" style="grid-template-columns:1fr 1.25fr">
<div class="card"><h2 style="font-size:13px;margin-bottom:4px">{lab(t['langs'],"Języki")}</h2>
<div class="lang"><span><b>{"Polski" if P else t['pl']}</b>{'<span class="lbl2"> · Pools</span>' if P else ''}</span><span class="sub">{"ojczysty · moedertaal" if P else t['native']}</span><div class="dots"><i class="on"></i><i class="on"></i><i class="on"></i><i class="on"></i><i class="on"></i></div></div>
<div class="lang"><span><b>{"Angielski" if P else t['en']}</b>{'<span class="lbl2"> · Engels</span>' if P else ''}</span><span class="sub">B1</span><div class="dots"><i class="on"></i><i class="on"></i><i class="on"></i><i></i><i></i></div></div>
<div class="lang"><span><b>{"Niderlandzki" if P else t['nlL']}</b>{'<span class="lbl2"> · Nederlands</span>' if P else ''}</span><span class="sub">{t['nlv']}</span><div class="dots"><i class="on"></i><i class="on"></i><i></i><i></i><i></i></div></div>
<div class="sub" style="margin-top:4px">{t['nlx']}</div></div>
<div class="card"><h2 style="font-size:13px;margin-bottom:4px">{lab(t['prefs'],"Preferencje pracy")}<span class="new">NIEUW</span></h2>
{''.join(f'<div class="lang"><span>{lab(a,b) if P else a}</span><b style="color:var(--purple);text-align:right">{v}</b></div>' for a,b,v in [
 (t['indoor'],"W środku (szklarnia / hala)",t['indoorv']),(t['outdoor'],"Na zewnątrz",t['outdoorv']),(t['phys'],"Praca fizyczna",t['physv']),(t['pace'],"Tempo / norma",t['pacev']),(t['team'],"Środowisko",t['teamv'])])}
</div></div>

<div class="sec"><h2><span class="ic">✦</span>{lab(t['strengths'],"Mocne strony")}<span class="sub" style="font-weight:500;font-family:Inter;font-size:9px">zelfinzicht uit Lobsy-competentietest · 21 sep 2026 · door Marta gekozen</span></h2>
<div class="grid g3">{''.join(f'<div class="card p"><b style="color:var(--ink)">{s}</b><div class="sub">{d}</div></div>' for s,d in zip(t['s'],["werkt netjes, controleert eigen werk","houdt een vast tempo vol","helpt collega's, werkt goed in ploeg"]))}</div>
<div style="margin-top:7px;display:flex;gap:5px;flex-wrap:wrap">{''.join(f'<span class="chip c">{k}</span>' for k in t['kw'])}</div></div>

<div class="sec"><h2><span class="ic">➜</span>{lab("Sectoren die bij mij passen","Branże, które do mnie pasują")}<span class="der">AFGELEID</span><span class="sub" style="font-weight:500;font-family:Inter;font-size:9px">volgens mijn eigen tests · door mij gekozen · uitleg op p. 2</span></h2>
<div class="grid g3">{''.join(f'<div class="card"><div class="row"><b class="disp" style="font-size:13px;color:var(--ink)">{i}. {n}</b></div><div class="sub" style="margin-top:3px;color:var(--text)">{w}</div></div>' for i,(n,b,c,w) in enumerate([
 ("Szklarnie" if P else "Glastuinbouw","88%","g","Doe-profiel, zorgvuldig, 2,5 jr oogstervaring"),
 ("Logistyka" if P else "Logistiek","79%","g","Orderpicken, heftruckcertificaat, houdt tempo"),
 ("Produkcja" if P else "Techniek & productie","64%","c","Groeikans: hands-on, wil machines leren")],1))}</div>
<div style="margin-top:7px" class="sub"><b style="color:var(--ink)">{lab("Zoekt","Szuka")}:</b> orderpicker · medewerker tuinbouw &nbsp;·&nbsp; <b style="color:var(--ink)">{lab("Contract","Umowa")}:</b> uitzend → vast <span class="new">NIEUW</span> &nbsp;·&nbsp; <b style="color:var(--ink)">{lab("Huisvesting","Zakwaterowanie")}:</b> eigen woning in Naaldwijk <span class="new">NIEUW</span></div></div>

<div class="sec"><h2><span class="ic">◆</span>{lab(t['exp'],"Doświadczenie i dokumenty")}</h2>
<div class="grid g2" style="grid-template-columns:1.3fr 1fr">
<div class="card"><div class="row"><b style="color:var(--ink)">{t['e1']}</b><span class="sub">2 jr 7 mnd</span></div><div class="sub">{t['e1b']}</div>
<div class="row" style="margin-top:6px"><b style="color:var(--ink)">{t['e2']}</b><span class="sub">2 jr 7 mnd</span></div><div class="sub">{t['e2b']}</div></div>
<div class="card"><div class="lbl">{lab(t['certs'],"Certyfikaty")}</div><div style="margin-top:3px">VCA Basis <span class="sub">2024</span> · Heftruck <span class="sub">2022</span></div>
<div class="lbl" style="margin-top:6px">{lab(t['edu'],"Wykształcenie")}</div><div style="margin-top:2px">{t['eduv']}</div></div></div></div>

<div class="foot"><div class="qr"><img src="qr-verify.svg"></div><div class="txt">
<b>{biv("Zeskanuj, aby zobaczyć aktualną, zweryfikowaną wersję", t['scan']) if P else t['scan']}</b><br>{t['link']}
<div class="privacy">{t['priv']}</div><div class="privacy" style="color:var(--purple)"><b>Gespreksinput, geen beoordeling:</b> Lobsy geeft bureaus en werkgevers geen score, ranking of automatische selectie van kandidaten.</div></div>
<div style="text-align:right;font-size:8.5px;color:var(--muted)">{t['page']}<br><span style="color:var(--purple);font-weight:700">lobsy.nl</span></div></div>
<div class="legend">MOCKUP · fictieve data · <span style="color:var(--coral)">NIEUW</span> = veld bestaat nog niet in datamodel · <span style="color:var(--purple)">AFGELEID</span> = berekend uit bestaande data/tests</div>
</div>'''
    if P:
        for nl,pl in PLMAP:
            html=html.replace('>'+nl+'<', '>'+pl+'<span class="lbl2" style="display:block;font-weight:500"> '+nl+'</span><',1) if ('>'+nl+'<') in html else html.replace(nl, pl+' <span class="lbl2">('+nl+')</span>',1)
    return html

def page2():
    sectors = [
     ("Glastuinbouw","Mijn eerste keus",88,["Doe-profiel (Realistisch) uit loopbaantest","Zorgvuldig + tempo: past bij oogsten/sorteren","Werkt graag binnen, in vast team","2,5 jaar oogstervaring"],"Oogst- en teeltmedewerker · sorteren & inpak · kwaliteitscontrole"),
     ("Logistiek","Ook interessant",79,["Orderpick-ervaring met handscanner","Heftruckcertificaat (2022)","Houdt tempo vast, ook bij normwerk"],"Orderpicker · inpakker · heftruckchauffeur"),
     ("Techniek & productie","Wil ik in groeien",64,["Hands-on en nauwkeurig","Wil leren: reachtruck / machinebediening","Nederlands B1 helpt bij doorgroei"],"Productiemedewerker · machineoperator (met inwerken)")]
    cards = ''.join(f'''<div class="card {'p' if i==0 else ''}" style="display:flex;flex-direction:column">
<div class="row"><span class="disp" style="font-weight:900;font-size:15px;color:var(--ink)">{i+1}. {n}</span></div>
<div class="sub" style="margin-top:2px">{b}</div>
<div class="lbl" style="margin-top:7px">Waarom het bij mij past</div><ul class="why">{''.join(f'<li>{w}</li>' for w in why)}</ul>
<div class="lbl" style="margin-top:6px">Voorbeeldfuncties</div><div class="sub" style="color:var(--text)">{roles}</div></div>''' for i,(n,b,p,why,roles) in enumerate(sectors))
    dna = [("🦞","Mijn scharen","Competenties","Zorgvuldig werken · tempo houden","21 sep"),
           ("📡","Mijn antennes","Loopbaaninteresse","Doen & ordenen (R-C)","22 sep"),
           ("🪨","Mijn rots","Werkcultuur","Duidelijke structuur, vast team","25 sep"),
           ("🧭","Mijn kompas","Waarden","Zekerheid · eerlijk loon · collega's","28 sep")]
    dnah = ''.join(f'<div class="card c"><div style="font-size:18px">{e}</div><div class="disp" style="font-weight:900;color:var(--ink);font-size:13px">{a}</div><div class="lbl">{b}</div><div style="margin-top:4px;font-weight:600;color:var(--ink)">{c}</div><div class="sub" style="margin-top:3px">✓ afgerond {d} 2026</div></div>' for e,a,b,c,d in dna)
    return f'''<div class="page"><div class="mock-tag">Mockup · fictieve kandidaat</div>
<div class="hdr"><img class="logo" src="lobsy.png" style="width:30px;height:30px"><div class="brand" style="font-size:17px">Marta Kowalska<small>DNA-paspoort · verdieping · LB-48213</small></div><div class="sp"></div>
<div class="badge-ver"><div class="seal">✓</div><div><b>Lobsy-geverifieerd</b><span>4/4 DNA-tests · 28 sep 2026</span></div></div></div>

<div class="sec"><h2><span class="ic">★</span>Sectoren die bij mij passen<span class="der">AFGELEID</span></h2>
<div class="grid g3">{cards}</div>
<div class="sub" style="margin-top:5px">Voorgesteld door Lobsy op basis van mijn eigen tests, ervaring en voorkeuren; ik heb ze zelf gekozen en op volgorde gezet. Zelfinzicht en gespreksinput, géén score of beoordeling van de kandidaat.</div></div>

<div class="sec"><h2><span class="ic">🧬</span>Mijn DNA in 4 lagen</h2><div class="grid g4">{dnah}</div></div>

<div class="grid g2 sec" style="grid-template-columns:1.1fr 1fr">
<div class="card"><h2 style="font-size:13px">Hoe ik graag werk</h2>
<ul class="why" style="margin-top:4px">{''.join(f'<li><b style="color:var(--ink)">{a}</b> <span class="sub">— {b}</span></li>' for a,b in [("Duidelijke structuur & afspraken","weet graag wat er verwacht wordt"),("Hands-on, met mijn handen bezig","liever doen dan vergaderen"),("Samen in een vast team","vaste ploeg geeft rust"),("Minder: alles zelf beslissen","liever duidelijke taakverdeling")])}</ul>
</div>
<div class="card p"><h2 style="font-size:13px">Zo haal je het beste uit Marta<span class="new">NIEUW</span></h2>
<ul class="why"><li>Leg een nieuwe taak één keer voor-doen-na-doen uit</li><li>Geef een vaste ploeg en vaste begintijd</li><li>Instructies liefst kort, met beeld of in het Pools/Engels</li><li>Waardeert feedback op kwaliteit, niet alleen op tempo</li></ul>
<div class="lbl" style="margin-top:8px">Wil leren</div><div style="margin-top:3px;display:flex;gap:5px;flex-wrap:wrap"><span class="chip">Nederlands naar B1</span><span class="chip">Reachtruck-certificaat</span></div></div></div>

<div class="sec"><h2><span class="ic">✎</span>In mijn eigen woorden</h2>
<div class="card" style="font-size:11px;line-height:1.5">“Ik werk graag met mijn handen en ben altijd op tijd. In de kas vind ik het fijn dat je ziet wat je gedaan hebt. Ik zoek vast werk in de buurt van Naaldwijk, het liefst in een team waar ik ook Nederlands kan leren.”
<div class="sub" style="margin-top:4px">Geschreven door de kandidaat · vertaald uit het Pools <span class="chip o" style="font-size:8px;padding:1px 6px">automatisch vertaald</span></div></div></div>

<div class="sec"><h2><span class="ic">✓</span>Wat betekent “Lobsy-geverifieerd”?</h2>
<div class="card" style="display:grid;grid-template-columns:1fr 1fr;gap:8px">
<div><b style="color:var(--green)">✓ Gecontroleerd door Lobsy</b><ul class="why"><li>4/4 DNA-tests zelf afgerond op dit account (datum per test)</li><li>E-mail en telefoon bevestigd</li><li>Paspoort-inhoud = live-versie op de QR-datum</li></ul></div>
<div><b style="color:var(--coral)">✕ Niet gecontroleerd</b><ul class="why"><li>Identiteit / werkvergunning (doet het bureau zelf)</li><li>Echtheid van diploma's en certificaten</li><li>Referenties van vorige werkgevers</li></ul></div></div></div>

<div class="foot"><div class="qr" style="width:62px;height:62px"><img src="qr-verify.svg"></div><div class="txt"><b>Live-versie & echtheid controleren</b><br>lobsy.nl/v/7KQ-M2P · gedeeld door de kandidaat · geldig t/m 2 nov 2026
<div class="privacy">Testantwoorden en ruwe scores blijven privé bij de kandidaat en staan nooit op het paspoort. Lobsy geeft bureaus en werkgevers geen score, ranking of automatische selectie: dit paspoort is gespreksinput.</div></div>
<div style="text-align:right;font-size:8.5px;color:var(--muted)">pagina 2/2<br><span style="color:var(--purple);font-weight:700">lobsy.nl</span></div></div>
<div class="legend">MOCKUP · fictieve data · <span style="color:var(--coral)">NIEUW</span> = bestaat nog niet · <span style="color:var(--purple)">AFGELEID</span> = berekend uit bestaande data/tests</div></div>'''

def wrap(body, w=794, h=1123):
    return f'<!doctype html><html><head><meta charset="utf-8"><link rel="stylesheet" href="style.css"><style>body{{width:{w}px}}</style></head><body>{body}</body></html>'

open('a.html','w').write(wrap(page1('nl')))
open('b.html','w').write(wrap(page2()))
open('c.html','w').write(wrap(page1('plnl')))
open('f.html','w').write(wrap(page1('nl', cobrand=True)))
print("ok")
