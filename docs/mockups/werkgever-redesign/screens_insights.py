"""Kandidaatinzichten as a PAID feature: free teaser + locked premium parts.
Reuses the locked/premium pattern of the paid extended test results
(Jobsy.Web/wwwroot/css/features/testresultaten.css, mockup/testresultaten): blur + 'Vergrendeld' chip + gold block."""
from bmui import *

PRICE = ("12 tokens", "90 dagen · alle vestigingen")  # VOORBEELD: geen prijs in de code (nu: tokensaldo > 0 = volledig)

def lcard(title, inner):
    return (f'<div class="card lc"><div class="ch"><h2>{title}</h2><span class="sp"></span><span class="lk">{ic("lock")}Vergrendeld</span></div>'
            f'<div class="bl" aria-hidden="true">{inner}</div><div class="shine"></div><span class="stamp">Voorbeelddata</span></div>')

def dummy_bars(items):
    return "".join(f'<div class="hbar"><span>{a}</span><span class="bar"><i style="width:{w}%"></i></span><b class="num" style="text-align:end">{w}%</b></div>' for a, w in items) + '<div style="height:10px"></div>'

def insights_map(h=300, locked=True):
    import random
    random.seed(7)
    cells = []
    for y in range(9):
        for x in range(15):
            d = max(0, 1 - (((x - 6) ** 2) / 40 + ((y - 4) ** 2) / 14)) + random.random() * .25
            if d < .35: continue
            cells.append(f'<rect x="{x*48+4}" y="{y*42+4}" width="44" height="38" rx="4" style="fill:var(--brand);opacity:{min(.75, d*.7):.2f}"/>')
    pins = "".join(f'<g transform="translate({x},{y})"><circle r="10" style="fill:var(--surface);stroke:var(--brand);stroke-width:2"/><circle r="4" style="fill:var(--brand)"/></g><text x="{x+15}" y="{y+4}" style="font:600 13px Inter,sans-serif;fill:var(--text)">{n}</text>' for x, y, n in [(300, 190, "Naaldwijk"), (150, 110, "Monster"), (420, 262, "De Lier"), (470, 120, "Honselersdijk")])
    ring = '<circle cx="330" cy="190" r="175" style="fill:none;stroke:var(--brand);stroke-width:1.5;stroke-dasharray:6 5;opacity:.6"/>'
    roads = '<path d="M0 240 C200 220 380 280 720 250" style="stroke:var(--surface);stroke-width:6;fill:none"/><path d="M360 0 C340 150 400 260 380 380" style="stroke:var(--surface);stroke-width:5;fill:none"/>'
    dens = f'<g style="filter:blur(6px);opacity:.55">{"".join(cells)}</g>' if locked else "".join(cells)
    return f'<svg viewBox="0 0 720 380" width="100%" height="{h}" preserveAspectRatio="xMidYMid slice" style="display:block">{roads}{dens}{ring}{pins}</svg>'

CHECKS = ["Dichtheid per wijk en reistijd", "Match met je vacatures", "Werkvelden, prioriteiten, droombanen", "Trends en export (CSV/pdf)"]

def premium(mobile=False):
    ch = "".join(f'<li>{ic("check")}<span>{c}</span></li>' for c in CHECKS)
    meta = "" if mobile else f"<small>{PRICE[1]}</small>"
    btn = f'<span class="btn-gold"><span class="lab">{ic("star")}{"Ontgrendel" if mobile else "Ontgrendel volledige inzichten"}</span><span class="meta"><span class="price">{PRICE[0]}</span>{meta}</span></span>'
    if mobile:
        return (f'<section class="prem" style="grid-template-columns:1fr;padding:16px;gap:12px"><div><span class="tag">{ic("star")}Volledige inzichten</span>'
                f'<h3 style="font-size:var(--text-lg)">Weet wie er rond je vestiging zoekt</h3><ul style="grid-template-columns:1fr">{ch}</ul></div>'
                f'<div class="cta">{btn}<p class="fine">{ic("shield2")}Voorbeeldprijs · {PRICE[1]}</p></div></section>')
    return (f'<section class="prem"><div><span class="tag">{ic("star")}Volledige inzichten</span><h3>Weet precies wie er rond je vestigingen zoekt</h3>'
            f'<p class="l">Ontgrendel alles hierboven met echte cijfers voor al je 14 vestigingen. Altijd anoniem.</p><ul>{ch}</ul></div>'
            f'<div class="cta">{btn}<span class="btn" style="justify-content:center">{ic("eye")}Bekijk voorbeeldrapport</span>'
            f'<p class="fine">{ic("shield2")}Voorbeeldprijs · afrekenen met je tokensaldo · geen abonnement</p></div></section>')

def d6():
    kp = "".join(f'<div class="card kpi"><small>{a}<span class="free" style="margin-inline-start:auto">{ic("check")}Gratis</span></small><div class="v">{v}</div><div class="dl">{d}</div></div>' for a, v, d in [
        ("Kandidaten binnen 15 km", "2.340", "<span class=up>+6%</span> in 90 dagen"), ("Gewenste uren", "27 u", "gemiddeld per week")])
    kp += "".join(f'<div class="card kpi lc"><small>{a}<span class="lk" style="margin-inline-start:auto">{ic("lock")}Premium</span></small><div class="v">{v}</div><div class="dl">{d}</div></div>' for a, v, d in [
        ("Passend bij je vacatures", "486", "Hoeveel passen bij je 48 vacatures?"), ("Direct beschikbaar", "41%", "Wie kan binnen 2 weken starten?")])
    mapov = f'<div class="lockov pn"><span class="lci">{ic("lock")}</span><div><b style="font-size:var(--text-sm)">Waar wonen ze precies?</b><br><span>Dichtheid per wijk en reistijd per vestiging</span></div></div>'
    mapcard = (f'<div class="card lc"><div class="ch"><div><h2>Kaart</h2><div class="muted" style="font-size:var(--text-xs)">Gratis: straal en totaal · &lt; 10 kandidaten blijft leeg</div></div><span class="sp"></span><span class="lk">{ic("lock")}Dichtheid</span></div>'
               f'<div style="background:var(--pearl);border-radius:0 0 var(--radius) var(--radius);overflow:hidden">{insights_map(236)}</div>{mapov}</div>')
    wf = lcard("Gewenste werkvelden", dummy_bars([("Tuinbouw en teelt", 44), ("Logistiek", 27), ("Productie", 18), ("Techniek", 11), ("Horeca", 7)][:4]))
    pr = lcard("Prioriteiten", dummy_bars([("Vaste uren", 58), ("Dichtbij huis", 52), ("Goed salaris", 47), ("Leuk team", 39), ("Doorgroeien", 24)][:4]))
    body = f'''<div class="ph"><div><h1 style="display:flex;align-items:center;gap:10px">Kandidaatinzichten <span class="pill line">Gratis versie</span></h1><p>Wie zoekt werk rond je vestigingen. Anoniem en opgeteld: je ziet nooit individuele kandidaten.</p></div>
<div class="act"><span class="btn">{ic("users")}Naar talentpool</span><span class="btn dis">{ic("lock")}Exporteren</span></div></div>
<div style="display:flex;gap:8px;align-items:center;margin-bottom:14px"><span class="dd on">Vestiging <em style="color:var(--brand)">Regio Westland (5)</em>{ic("chevd")}</span><span class="seg"><span>5 km</span><span>10 km</span><span class="on">15 km</span><span>25 km</span></span><span class="dd">Periode <em>90 dagen</em>{ic("chevd")}</span><span class="dd">{ic("lock")}Werkveld <em>Premium</em></span></div>
<div class="kpis" style="grid-template-columns:repeat(4,minmax(0,1fr))">{kp}</div>
<div class="row" style="grid-template-columns:minmax(0,1.3fr) minmax(0,1fr) minmax(0,1fr);align-items:stretch;margin-bottom:16px">{mapcard}{wf}{pr}</div>
{premium()}'''
    return page("bm", "ins", [COMPANY, "Kandidaatinzichten"], body, collapsed=("Meer", "Tokens & facturen", "Organisatie"))

def m3():
    top = f'<header class="mtop" style="padding-inline-start:4px"><span class="tb">{ic("arrowl")}</span><b>Kandidaatinzichten</b><span style="margin-inline-start:auto" class="tb">{ic("more")}</span></header>'
    kf = "".join(f'<div class="card kpi"><small>{a}</small><div class="v">{v}</div><div class="dl"><span class="free">{ic("check")}Gratis</span></div></div>' for a, v in [("Binnen 15 km", "2.340"), ("Gewenste uren", "27 u")])
    kl = "".join(f'<div class="card kpi lc"><small>{a}</small><div class="v">{v}</div><div class="dl"><span class="lk">{ic("lock")}Premium</span></div></div>' for a, v in [("Passend bij vacatures", "486"), ("Direct beschikbaar", "41%")])
    mapc = (f'<div class="mcard lc" style="position:relative"><div style="background:var(--pearl)">{insights_map(200)}</div>'
            f'<div class="lockov pn"><span class="lci">{ic("lock")}</span><b style="font-size:var(--text-sm)">Dichtheid per wijk</b></div></div>')
    wf = (f'<div class="mcard lc" style="position:relative;margin-top:10px"><div class="bl" style="padding-top:10px">{dummy_bars([("Tuinbouw en teelt", 44), ("Logistiek", 27), ("Productie", 18)])}</div>'
          f'<div class="lockov"><span class="lci">{ic("lock")}</span><b>Werkvelden en prioriteiten</b></div></div>')
    body = f'''<main class="mmain"><div style="display:flex;align-items:center;gap:8px"><span class="pill line">Gratis versie</span><span class="muted sm">Naaldwijk · 15 km · 90 dagen</span></div>
<div class="mkpis">{kf}{kl}</div>{mapc}{wf}
<div style="margin-top:14px">{premium(mobile=True)}</div>
<p class="muted sm" style="margin-top:10px;text-align:center">Altijd anoniem · groepen onder 10 kandidaten tonen we niet</p><div style="height:64px"></div></main>'''
    return mpage(body, top=top, bnav=False)
