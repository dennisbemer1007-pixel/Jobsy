from wr_ui import ic, vb, wizard, acts, cb, CK, em
from data import CO

BR = [("🍽️", "Horeca"), ("🛍️", "Winkel"), ("📦", "Logistiek"), ("🌷", "Tuinbouw"), ("🩺", "Zorg"), ("💼", "Kantoor"), ("🏗️", "Bouw"), ("🧽", "Schoonmaak"), ("🏭", "Productie")]
BR_ON = {"Zorg": "Uit KvK", "Schoonmaak": "Uit KvK"}

def chips(extra_on=()):
    out = []
    for e, n in BR:
        on = n in BR_ON or n in extra_on
        t = f'<span class="t">{BR_ON[n]}</span>' if n in BR_ON else ""
        ck = f'<span class="ck">{CK}</span>' if on else ""
        out.append(f'<span class="{"on" if on else ""}"><i class="e">{e}</i>{n}{t}{ck}</span>')
    return f'<div class="chs">{"".join(out)}</div>'

def sbi():
    return ('<div class="sbi"><i class="e">🏛️</i><span>Uit het Handelsregister: <code>88101</code> Maatschappelijke dienstverlening voor ouderen → <b>Zorg</b> · '
            '<code>81210</code> Algemene reiniging van gebouwen → <b>Schoonmaak</b></span></div>')

def d5():
    card = f'''<div class="toprow"><span class="pill">Stap 4 · Over je bedrijf · 1 van 3</span><span class="pill line">optioneel</span>{vb()}</div>
<h1>In welke branche werken jullie?</h1><p class="lead">Kies alles wat past. We hebben alvast ingevuld wat bij KVK staat. Kandidaten vinden je zo op de banenkaart en in Match.</p>
{sbi()}<div class="sec2"><h3>Branches <span class="cnt">2 gekozen · max. 4</span></h3>{chips()}</div>
<div class="nb" style="margin-top:18px"><i class="e">💡</i><div><b>Per vacature kies je er straks 1 of 2</b><p>Het bedrijf mag meerdere branches hebben. Een vacature krijgt automatisch de eerste; je kunt dat per vacature aanpassen.</p></div></div>
{acts("Verder", skip="Later invullen")}'''
    return wizard(4, "Wat doen jullie?", "Dit heb ik bij KVK gevonden. Klopt het? Tik gerust meer aan.", card, wave=False)

DIMS = [("📏", "Duidelijke kaders", "Zelfstandig werken", 3, "Zelfstandig werken"), ("👔", "Formeel", "Informeel", 3, "Informele sfeer"),
        ("🙋", "Ieder z’n eigen taken", "Veel samenwerken", 4, "Samenwerken"), ("🗓️", "Vaste structuur", "Flexibel meebewegen", 1, "Flexibel meebewegen"),
        ("🧱", "Beproefd en stabiel", "Nieuwe dingen proberen", 2, "Nieuwe dingen proberen"), ("🎯", "Resultaat voorop", "Mensen voorop", 4, "Mensen voorop")]

def sliders(n=6):
    out = []
    for e, l, r, v, name in DIMS[:n]:
        dots = "".join(f'<i class="{"on" if k == v else ""}"></i>' for k in range(5))
        out.append(f'<div class="slr"><span class="l">{l}</span><span class="trk">{dots}</span><span class="r2">{r}</span><span class="dim"><span>{l}</span><span>{r}</span></span></div>')
    return f'<div class="sl">{"".join(out)}</div>'

VALS = [("🕊️", "Vrijheid en eigen regie", "Eigen regie &amp; uitdaging", False), ("🔍", "Nieuwsgierig en leergierig", "Eigen regie &amp; uitdaging", False),
        ("🤗", "Zorgen voor elkaar", "Verbinding &amp; zorg", True), ("🤝", "Echt een team", "Verbinding &amp; zorg", False),
        ("🏆", "Samen resultaat halen", "Prestatie &amp; groei", False), ("🛠️", "Vakmanschap", "Prestatie &amp; groei", True),
        ("⚓", "Betrouwbaar en zeker", "Zekerheid &amp; traditie", False), ("📋", "Duidelijke afspraken", "Zekerheid &amp; traditie", False),
        ("🌍", "Iets betekenen voor anderen", "Impact &amp; rechtvaardigheid", True), ("🌱", "Eerlijk en duurzaam", "Impact &amp; rechtvaardigheid", False)]

def vcards(items=VALS):
    return '<div class="vcards">' + "".join(
        f'<div class="vc {"on" if on else ""}">{f"<span class=ck>{CK}</span>" if on else ""}<i class="e">{e}</i><b>{t}</b><small>{d}</small></div>' for e, t, d, on in items) + "</div>"

def profile():
    cells = "".join(f'<div><b>{v}%</b>{n}</div>' for v, n in [(55, "Zelfstandig"), (60, "Informeel"), (80, "Samenwerken"), (30, "Flexibel"), (45, "Vernieuwend"), (82, "Mensen voorop")])
    return f'''<div class="sec2"><h3><i class="e">🧬</i>Jullie cultuurprofiel <span class="pill line">live</span></h3><p>Dezelfde 6 dimensies als de Cultuurscan van kandidaten, plus je top-3 waarden.</p><div class="prof">{cells}</div>
<div class="toprow" style="margin-top:10px"><span class="pill">Verbinding &amp; zorg</span><span class="pill">Prestatie &amp; groei</span><span class="pill">Impact &amp; rechtvaardigheid</span><span class="hint">De volledige Cultuurscan (12 vragen) staat later in je dashboard.</span></div></div>'''

def d6():
    card = f'''<div class="toprow"><span class="pill">Stap 4 · Over je bedrijf · 2 van 3</span><span class="pill line">optioneel · 1 minuut</span>{vb()}</div>
<h1>Zo werken wij</h1><p class="lead">Zet de bolletjes waar jullie écht staan, niet waar je graag zou willen staan. Kandidaten met dezelfde voorkeuren komen dan hoger in je lijst.</p>
<div class="sec2"><h3><i class="e">🧭</i>Hoe gaat het bij jullie?</h3>{sliders()}</div>
<div class="sec2"><h3><i class="e">💛</i>Kies 3 kernwaarden <span class="cnt">3 van 3</span></h3><p>Wat vinden jullie het belangrijkst? Elke kaart hoort bij een van de 5 werkwaarden uit de Waardentest.</p>{vcards()}</div>
{profile()}
{acts("Verder", skip="Later invullen")}'''
    return wizard(4, "Wie zijn jullie?", "Kandidaten doen dezelfde test. Hoe eerlijker jij kiest, hoe beter de match. 🎯", card, wave=False)

ENG = [("🌱", "Duurzaam en CO2-bewust", "Bijv. CO2-Prestatieladder, B Corp of een eigen duurzaamheidsplan", None, None),
       ("🤝", "Werk voor iedereen", "Banen voor mensen met afstand tot de arbeidsmarkt: banenafspraak, Participatiewet, SROI", "https://groenenzorg.nl/werk-voor-iedereen", "Link toegevoegd · door werkgever opgegeven"),
       ("🎓", "Erkend leerbedrijf (SBB)", "Leerlingen en stagiairs van het mbo zijn welkom", "SBB-erkenning gevonden: 3 leerbedrijf-erkenningen", "Gecontroleerd bij SBB"),
       ("🏘️", "Lokaal betrokken", "Vrijwilligerswerk, buurtprojecten of een lokale club steunen", None, None),
       ("🌈", "Diversiteit en inclusie", "Bijv. ondertekenaar Charter Diversiteit", None, None),
       ("⚖️", "Eerlijk loon en cao", "Iedereen betaald volgens een cao of een eerlijk loongebouw", "Cao VVT", "Door werkgever opgegeven")]

def eng_rows(items=ENG, proof=True):
    out = []
    for e, t, d, pv, st in items:
        on = pv is not None
        pill = ""
        if on:
            pill = f'<span class="pill {"chkd" if "SBB" in st else "self"}">{("✓ " if "SBB" in st else "")}{st}</span>'
        pr = ""
        if on and proof:
            if "SBB" in st:
                pr = f'<div class="proof"><div class="inp ok">{ic("shield")}{pv}</div><a class="btn sm ghost">Details</a></div>'
            else:
                pr = f'<div class="proof"><div class="inp">{ic("globe") if "http" in pv else ic("report")}{pv}</div><a class="btn sm">Keurmerk toevoegen</a></div>'
        out.append(f'<div class="er {"on" if on else ""}"><div class="top">{cb(on)}<span class="e">{e}</span><div><h4>{t}</h4><p>{d}</p></div>{pill}</div>{pr}</div>')
    return f'<div class="eng">{"".join(out)}</div>'

def badges():
    return ('<div class="toprow bdgs"><span class="bw"><span class="bdg"><i class="e">🎓</i>Erkend leerbedrijf</span><span class="bsrc">✓ gecontroleerd bij SBB</span></span>'
            '<span class="bw"><span class="bdg" style="background:var(--peach)"><i class="e">🤝</i>Werk voor iedereen</span><span class="bsrc">door werkgever opgegeven</span></span>'
            '<span class="bw"><span class="bdg" style="background:var(--sky)"><i class="e">⚖️</i>Eerlijk loon en cao</span><span class="bsrc">door werkgever opgegeven</span></span></div>')

def d7():
    card = f'''<div class="toprow"><span class="pill">Stap 4 · Over je bedrijf · 3 van 3</span><span class="pill line">optioneel</span>{vb()}</div>
<h1>Waar staan jullie voor?</h1><p class="lead">Veel kandidaten vinden maatschappelijke betrokkenheid belangrijk. Kies wat echt bij jullie past. Een bewijs is niet verplicht, maar maakt je badge sterker.</p>
{eng_rows()}
<div class="sec2"><h3><i class="e">👀</i>Zo zien kandidaten het</h3><p>Op je bedrijfspagina en bij elke vacature. We zijn eerlijk over wat we hebben gecontroleerd.</p><div style="margin-top:10px">{badges()}</div></div>
<div class="nb s" style="margin-top:16px"><i class="e">🎯</i><div><b>Telt mee in de match</b><p>Kandidaten voor wie “Impact &amp; rechtvaardigheid” of “Verbinding &amp; zorg” zwaar weegt in de Waardentest, zien jullie vacatures iets hoger.</p></div></div>
{acts("Verder naar verifiëren", skip="Later invullen")}'''
    return wizard(4, "Iets goeds doen telt!", "Maak niets mooier dan het is. Wat wij niet kunnen controleren, noemen we ‘door werkgever opgegeven’.", card, wave=False)
