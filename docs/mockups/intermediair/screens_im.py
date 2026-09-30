"""Intermediair (uitzendbureau) screens: same /werkgever shell, role chip 'Intermediair', opdrachtgevers via KvK,
per-vacancy map choice (own vestiging = default vs. client's KvK address). All data is Voorbeelddata."""
from imui import *

MODE = {"verborgen": ("line", "eyeoff", "Via onze vestiging"), "echt": ("info", "pin", "Echte werklocatie"), "gemengd": ("", "layers", "Gemengd")}

def mode_pill(m):
    cls, i, t = MODE[m]
    return f'<span class="pill {cls}">{ic(i)}{t}</span>'

def kpi(label, v, dl, cls="up", icon=None):
    d = f'<span class="{cls}">{dl}</span>' if cls else dl
    return f'<div class="card kpi"><small>{ic(icon) if icon else ""}{label}</small><div class="v">{v}</div><div class="dl">{d}</div></div>'

def todo(kind, icon, title, meta, action):
    btn = f'<span class="btn sm">{action}</span>' if action else ""
    return f'<li><span class="ico {kind}">{ic(icon)}</span><div class="t"><b>{title}</b><small>{meta}</small></div>{btn}</li>'

def ini(name):
    w = [x for x in name.replace("Voorbeeld ", "").split() if x[0].isupper()]
    return (w[0][0] + (w[1][0] if len(w) > 1 else "")).upper()

# ---------------------------------------------------------------- d1 dashboard
def d1():
    kp = "".join([kpi("Actieve vacatures", "18", "+3"), kpi("Nieuwe sollicitaties", "86", "+14%"),
                  kpi("Gem. eerste reactie", "1,4 dagen", "0,2 d sneller"), kpi("Aangenomen", "9", "+2"),
                  kpi("Tokensaldo", "236", "± 9 weken bij huidig verbruik", cls="", icon="coins")])
    todos = "".join([
        todo("bad", "clock", "9 sollicitaties langer dan 48 uur zonder reactie", "Vooral Voorbeeld Logistiek Zuid (6)", "Bekijken"),
        todo("wn", "building", "KvK-adres van Voorbeeld Bloemenexport is gewijzigd", "Werklocatie bijgewerkt uit KvK · controleer 2 vacatures", "Controleren"),
        todo("wn", "pin", "1 vacature staat op 24 km van je vestiging", "Orderpicker nachtdienst · kaartweergave ‘via vestiging’", "Bekijken"),
        todo("", "brief", "Voorbeeld Metaalbewerking heeft nog geen live vacature", "Opdrachtgever toegevoegd op 24-09", "Vacature plaatsen"),
        todo("", "coins", "Tokens raken over ± 9 weken op", "Gemiddeld verbruik: 26 tokens per week", "Tokens kopen"),
    ])
    fun = [("Sollicitaties", 100, "86"), ("Geaccepteerd", 41, "35"), ("Uitgenodigd", 24, "21"), ("Aangenomen", 10, "9")]
    funnel = "".join(f'<div><span class="muted">{a}</span><span class="bar"><i style="width:{w}%"></i></span><b class="num" style="text-align:end">{v}</b></div>' for a, w, v in fun)
    rows = "".join(
        f'<tr><td><div class="who"><span class="av">{ini(n)}</span><div><b>{n.replace("Voorbeeld ", "Vb. ")}</b><small>{c} · KvK {k}</small></div></div></td>'
        f'<td class="num">{lv}</td><td class="num">{ap}</td><td>{mode_pill(m)}</td></tr>'
        for n, k, _v, c, _a, lv, ap, _km, m in CLIENTS[:5])
    body = f'''<div class="ph"><div><h1>Dashboard</h1><p>{AGENCY} · {AGENCY_VEST} · 12 opdrachtgevers · laatste 30 dagen</p></div>
<div class="act"><span class="seg"><span>7 d</span><span class="on">30 d</span><span>90 d</span></span><span class="btn">{ic("dl")}Exporteren</span><span class="btn pri">{ic("plus")}Vacature plaatsen</span></div></div>
<div class="kpis">{kp}</div>
<div class="row" style="grid-template-columns:minmax(0,1fr) minmax(0,1.1fr);align-items:start">
<div class="row" style="gap:16px"><div class="card" style="overflow:hidden"><div class="ch"><h2>Te doen</h2><span class="cnt" style="margin:0">5</span><span class="sp"></span><a>Alles bekijken {ic("chevr")}</a></div><ul class="todo">{todos}</ul></div>
<div class="card"><div class="ch"><h2>Wervingstrechter</h2><span class="muted sm">alle opdrachtgevers · 30 dagen</span></div><div class="funnel">{funnel}</div></div></div>
<div class="card" style="overflow:hidden"><div class="ch"><h2>Opdrachtgevers</h2><span class="sp"></span><a>Alle 12 opdrachtgevers {ic("chevr")}</a></div>
<table><thead><tr><th>Opdrachtgever</th><th class="num">Live vac.</th><th class="num">Sollicitaties</th><th>Op de kaart</th></tr></thead><tbody>{rows}</tbody></table>
<div class="note" style="border-bottom:0;border-top:1px solid var(--border)">{ic("info")}<span>Naam en adres van opdrachtgevers komen uit KvK. ‘Via onze vestiging’: kandidaten zien alleen {AGENCY_SHORT}.</span></div></div></div>'''
    return page_im("dash", [AGENCY_SHORT, "Dashboard"], body)

# ---------------------------------------------------------------- d2 opdrachtgevers list + detail
def clients_list(active=0):
    items = "".join(
        f'<a class="{"on" if i == active else ""}"><span class="av">{ini(n)}</span><b>{n}</b><small>{c} · {lv} live</small><span class="cnt">{ap}</span></a>'
        for i, (n, k, _v, c, _a, lv, ap, _km, _m) in enumerate(CLIENTS))
    return (f'<div class="card" style="overflow:hidden"><div style="padding:12px 14px 10px;display:flex;flex-direction:column;gap:8px">'
            f'<div class="inp">{ic("search")}Zoek op naam, KvK of plaats</div><div style="display:flex;gap:6px"><span class="dd">Status <em>Actief</em>{ic("chevd")}</span><span class="dd">Op de kaart <em>Alle</em>{ic("chevd")}</span></div></div>'
            f'<div class="clist" style="border-top:1px solid var(--border)">{items}</div>'
            f'<div class="pag"><span>6 van 12 opdrachtgevers</span><span class="sp"></span><span class="btn sm">{ic("chevr")}</span></div></div>')

def d2(with_drawer=False):
    n, k, v, c, a, lv, ap, km, _m = CLIENTS[0]
    vacs = [("Oogstmedewerker kas", "ok", "Actief", "verborgen", 12), ("Inpakker bloemen (ochtend)", "ok", "Actief", "verborgen", 8),
            ("Teamleider teelt", "ok", "Actief", "echt", 6)]
    vr = "".join(f'<tr><td><b>{t}</b></td><td><span class="pill {s}">{sl}</span></td><td>{mode_pill(m)}</td><td class="num">{x or "—"}</td></tr>' for t, s, sl, m, x in vacs)
    mp = mapsvg(370, 170, pins=[(95, 125, "Onze vestiging", "agency"), (265, 72, "Opdrachtgever", "client")], line=((95, 101), (265, 48)))
    detail = f'''<div class="card" style="overflow:hidden">
<div class="ch" style="padding-bottom:6px;align-items:flex-start"><span class="av" style="width:44px;height:44px">{ini(n)}</span><div style="min-width:0"><h2 style="font-size:var(--text-lg)">{n}</h2>
<div class="kvk">{ic("building")}KvK {k} · vestiging {v} · {c}</div></div><span class="sp"></span><span class="pill ok">{ic("check")}Gecontroleerd in KvK · 28-09-2026</span><span class="btn sm pri">{ic("plus")}Vacature plaatsen</span><span class="ra">{ic("more")}</span></div>
<div class="tabs" style="margin:6px 0 0;padding:0 16px"><span class="on">Overzicht</span><span>Vacatures <span class="cnt">6</span></span><span>Sollicitaties <span class="cnt">31</span></span><span>Tokenverbruik</span></div>
<div class="row" style="grid-template-columns:minmax(0,1fr) minmax(0,1fr);padding:14px 16px;gap:16px">
<div style="display:flex;flex-direction:column;gap:10px"><h3 style="font-size:var(--text-sm)">Gegevens uit KvK</h3>
<div class="ro-field">{ic("tag")}<b>{n}</b><small>Handelsnaam</small><span class="src">{ic("lock")}Uit KvK</span></div>
<div class="ro-field">{ic("pin")}<b>{a}</b><small>Werklocatie · bezoekadres vestiging {v}</small><span class="src">{ic("lock")}Uit KvK</span></div>
<div class="ro-field">{ic("list")}<b>0113 · Teelt van groenten</b><small>Hoofdactiviteit (SBI)</small><span class="src">{ic("lock")}Uit KvK</span></div>
<p class="muted sm">Je kunt deze gegevens niet wijzigen. Verandert iets in KvK, dan werken we het hier automatisch bij. <a style="color:var(--brand);font-weight:600">Opnieuw ophalen</a></p></div>
<div style="display:flex;flex-direction:column;gap:10px"><h3 style="font-size:var(--text-sm)">Op de kaart</h3>
<div class="mapbox">{mp}<span class="maplbl">{ic("pin")}{km} km tussen je vestiging en de werklocatie</span></div>
<div class="set" style="border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 12px"><h3>Standaard voor nieuwe vacatures</h3><p>Op locatie van onze vestiging. Per vacature aan te passen.</p><span class="ctl"><span class="dd">Via onze vestiging{ic("chevd")}</span></span></div>
<div class="hidden-list"><div class="n">{ic("eyeoff")}Kandidaten zien de naam alleen bij ‘Echte werklocatie’</div><div class="n">{ic("eyeoff")}Andere bureaus en de opdrachtgever zelf zien jouw vacatures en sollicitanten niet</div></div></div></div>
<table style="border-top:1px solid var(--border)"><thead><tr><th>Vacature</th><th>Status</th><th>Op de kaart</th><th class="num">Sollicitaties</th></tr></thead><tbody>{vr}</tbody></table></div>'''
    body = f'''<div class="ph"><div><h1>Opdrachtgevers</h1><p>Bedrijven waarvoor je vacatures plaatst. Naam en adres komen altijd uit KvK.</p></div>
<div class="act"><span class="btn">{ic("dl")}Exporteren</span><span class="btn pri">{ic("plus")}Opdrachtgever toevoegen via KvK</span></div></div>
<div class="row" style="grid-template-columns:320px minmax(0,1fr);align-items:start">{clients_list()}{detail}</div>'''
    extra = add_client_drawer() if with_drawer else ""
    return page_im("cli", [AGENCY_SHORT, "Opdrachtgevers", n] if not with_drawer else [AGENCY_SHORT, "Opdrachtgevers"], body, extra, collapsed=("Meer", "Organisatie", "Tokens & facturen"), demo="l" if with_drawer else "")

def add_client_drawer():
    ests = [("Hoofdvestiging · Wateringen", "Zijweg 10, 2291 PH Wateringen", "000046789012", True, ""),
            ("Vestiging ’s-Gravenzande", "Nieuwe Weg 2, 2691 JW ’s-Gravenzande", "000046789013", False, ""),
            ("Vestiging Rijswijk", "Kantoorlaan 5, 2288 EA Rijswijk", "000046789014", False, "Al je opdrachtgever")]
    eh = "".join(f'<div class="est{" on" if on else ""}{" dis" if tag else ""}"><span class="rd"></span><b>{t}</b><small>{ad} · vestigingsnr {vn}</small>{f"<span class=pill line>{tag}</span>" if tag else ""}</div>' for t, ad, vn, on, tag in ests)
    return f'''<div class="scrim"></div><aside class="drawer" style="width:440px">
<div class="dh"><span class="ico">{ic("building")}</span><div><h2>Opdrachtgever toevoegen</h2><div class="muted sm">Via het KvK-nummer. Naam en adres komen uit KvK.</div></div><span class="x">{ic("x")}</span></div>
<div class="db" style="gap:12px">
<div><span class="lbl" style="margin-top:0">KvK-nummer van de opdrachtgever</span><div style="display:flex;gap:8px"><div class="inp" style="flex:1;color:var(--text)">90789012</div><span class="btn">{ic("search")}Zoeken</span></div></div>
<div class="box" style="padding:10px 12px"><b>Voorbeeld Fruitteelt Groep BV</b><div class="muted sm">3 vestigingen in KvK · SBI 0124 Teelt van fruit</div></div>
<div><span class="lbl" style="margin-top:0">Welke vestiging is de werklocatie?</span><div style="display:flex;flex-direction:column;gap:8px">{eh}</div></div>
<label style="display:flex;gap:10px;align-items:flex-start;font-size:var(--text-xs)">{cb(True)}<span>Ik verklaar dat {AGENCY_SHORT} voor deze opdrachtgever werft of uitzendt (opdracht of raamovereenkomst).</span></label>
<div class="warnbox">{ic("info")}<div>Lukt het ophalen uit KvK niet? Probeer het later opnieuw. Een adres met de hand invoeren kan niet, zodat de werklocatie altijd klopt.</div></div>
</div>
<div class="df"><span class="btn pri">{ic("plus")}Opdrachtgever toevoegen</span><span class="btn ghost">Annuleren</span></div></aside>'''

def d5():
    return d2(with_drawer=True)

# ---------------------------------------------------------------- d3 vacancy form
def jobcard(hidden=True, travel="12 min"):
    n, _k, _v, c, *_ = CLIENTS[0]
    if hidden:
        co = f'<div class="co"><b>{AGENCY_SHORT}</b><span class="via">{ic("handshake")}Uitzendbureau</span></div>'
        loc = f'<span>{ic("pin")}Naaldwijk (vestiging bureau)</span><span>{ic("bike")}{travel} tot de vestiging</span>'
    else:
        co = f'<div class="co"><b>{n}</b><span class="via">{ic("handshake")}via {AGENCY_SHORT}</span></div>'
        loc = f'<span>{ic("pin")}{c}</span><span>{ic("bike")}{travel} fietsen</span>'
    return (f'<div class="jc"><div class="ph2">{ic("brief")}</div><div class="bd"><h4>Oogstmedewerker kas</h4>{co}'
            f'<div class="meta2">{loc}<span>{ic("euro")}€ 14,40 per uur</span><span>{ic("clock")}24–32 uur</span></div></div></div>')

def d3():
    n, k, v, c, a, *_rest = CLIENTS[0]
    steps = ('<div class="step"><span class="on"><b>1</b>Opdrachtgever &amp; locatie</span><em></em><span><b>2</b>Functie</span><em></em>'
             '<span><b>3</b>Salaris &amp; uren</span><em></em><span><b>4</b>Voorwaarden</span><em></em><span><b>5</b>Zichtbaarheid &amp; kosten</span></div>')
    form = f'''<div class="card" style="padding:18px 20px"><div class="form">
<div><span class="lbl">Opdrachtgever</span><div class="pick"><span class="av">{ini(n)}</span><div><b>{n}</b><small>KvK {k} · vestiging {v} · {c}</small></div>{ic("chevd")}</div>
<div style="display:flex;gap:14px;margin-top:6px;font-size:var(--text-xs)"><a style="color:var(--brand);font-weight:600">{ic("plus")} Nieuwe opdrachtgever via KvK</a><span class="muted">Alleen opdrachtgevers die je via KvK hebt toegevoegd</span></div></div>
<div><span class="lbl">Werklocatie</span><div class="ro-field">{ic("pin")}<b>{a}</b><small>Bezoekadres uit KvK · opgehaald op 28-09-2026</small><span class="src">{ic("lock")}Uit KvK · niet te wijzigen</span></div>
<p class="muted sm" style="margin-top:6px">Klopt dit adres niet? Kies een andere KvK-vestiging van deze opdrachtgever.</p></div>
<div><span class="lbl">Waar staat de vacature op de kaart?</span><div style="display:flex;flex-direction:column;gap:8px">
<div class="locc on"><span class="rd"></span><b>Toon op locatie van onze vestiging (standaard)</b><p>Pin bij {AGENCY_SHORT}, {AGENCY_ADDR}. De naam en het adres van de opdrachtgever blijven verborgen.</p>
<div class="see">{ic("eye")}Kandidaten zien: <b>{AGENCY_SHORT}</b> · Naaldwijk · label ‘Uitzendbureau’</div></div>
<div class="locc"><span class="rd"></span><b>Toon op echte werklocatie</b><p>Pin op het KvK-adres van de opdrachtgever in {c} (3 km van je vestiging). Kandidaten zien de naam van de opdrachtgever.</p>
<div class="see">{ic("eye")}Kandidaten zien: <b>{n}</b> · {c} · ‘via {AGENCY_SHORT}’</div></div></div></div>
<div class="warnbox" style="background:var(--accent-soft)">{ic("bike")}<div><b>Reistijd</b> rekenen we vanaf de plek op de kaart. Bij ‘onze vestiging’ staat erbij: ‘reistijd tot de vestiging van het bureau, de werklocatie ligt in de regio Westland’.</div></div>
</div></div>
<div style="display:flex;gap:8px;margin-top:14px"><span class="btn">Opslaan als concept</span><span class="sp" style="flex:1"></span><span class="btn pri">Volgende: functie {ic("chevr")}</span></div>'''
    mp = mapsvg(470, 250, pins=[(250, 150, AGENCY_SHORT, "agency"), (345, 100, "", "muted")], home=(150, 190), ring=(150, 190, 120, ""))
    preview = f'''<div class="card" style="overflow:hidden"><div class="ch"><h2>Zo zien kandidaten het</h2><span class="sp"></span><span class="seg"><span class="on">Kaart</span><span>Detail</span></span></div>
<div class="mapbox" style="margin:0 16px;border-radius:var(--radius-sm)">{mp}<span class="maplbl">{ic("bike")}Cirkel = 15 min fietsen vanaf jouw adres</span></div>
<div style="padding:12px 16px;display:flex;flex-direction:column;gap:10px">{jobcard(True, "9 min")}
<div class="hidden-list"><div class="y">{ic("check")}Pin, naam en reistijd: vestiging van {AGENCY_SHORT}</div>
<div class="n">{ic("eyeoff")}Niet zichtbaar: naam, KvK-nummer, adres en bedrijfspagina van de opdrachtgever</div>
<div class="n">{ic("pin")}Grijze stip = echte werklocatie, alleen zichtbaar voor jou</div></div></div></div>'''
    body = f'''<div class="ph"><div><h1>Nieuwe vacature</h1><p>Voor een opdrachtgever. Stap 1 van 5.</p></div></div>{steps}
<div class="row" style="grid-template-columns:minmax(0,1.1fr) minmax(0,1fr);align-items:start"><div>{form}</div>{preview}</div>'''
    return page_im("vac", [AGENCY_SHORT, "Vacatures", "Nieuwe vacature"], body, collapsed=("Meer", "Organisatie", "Tokens & facturen", "Opdrachtgevers"), scope=("Vb. Kwekerij De Kas", ""))

# ---------------------------------------------------------------- d4 candidate view: hidden vs real
def d4():
    n, _k, _v, c, a, *_ = CLIENTS[0]
    m1 = mapsvg(620, 300, pins=[(270, 190, AGENCY_SHORT, "agency")], home=(200, 200), ring=(200, 200, 140, ""))
    m2 = mapsvg(620, 300, pins=[(310, 135, n.replace("Voorbeeld ", "Vb. "), "client")], home=(200, 200), ring=(200, 200, 140, ""))
    def detail(hidden):
        if hidden:
            rows = [("Werkgever", f"{AGENCY_SHORT} <span class='via'>{ic('handshake')}Uitzendbureau</span>"), ("Werklocatie", "Regio Westland · adres volgt als het bureau je uitnodigt"),
                    ("Reistijd", "9 min fietsen tot de vestiging van het bureau"), ("Bedrijfspagina", f"Pagina van {AGENCY_SHORT}")]
        else:
            rows = [("Werkgever", f"{n} <span class='via'>{ic('handshake')}via {AGENCY_SHORT}</span>"), ("Werklocatie", f"{c} (adres uit KvK)"),
                    ("Reistijd", "14 min fietsen naar de werklocatie"), ("Bedrijfspagina", f"Pagina van {n.replace('Voorbeeld ', 'Vb. ')}")]
        return '<dl class="kv" style="grid-template-columns:110px 1fr">' + "".join(f"<dt>{x}</dt><dd>{y}</dd>" for x, y in rows) + "</dl>"
    see_h = f'''<div class="hidden-list"><div class="y">{ic("check")}Naam, logo en vestiging van het bureau</div><div class="y">{ic("check")}Uurloon, uren, functie en reistijd tot de vestiging</div>
<div class="n">{ic("eyeoff")}Naam, adres, KvK en bedrijfspagina van de opdrachtgever</div><div class="n">{ic("eyeoff")}Exacte werklocatie (pin en reistijd lopen via de vestiging)</div></div>'''
    see_r = f'''<div class="hidden-list"><div class="y">{ic("check")}Naam van de opdrachtgever + ‘via {AGENCY_SHORT}’</div><div class="y">{ic("check")}Pin op het KvK-adres, reistijd naar de werklocatie</div>
<div class="y">{ic("check")}Bedrijfspagina van de opdrachtgever</div><div class="n">{ic("info")}Solliciteren en contact lopen altijd via het bureau</div></div>'''
    panel = lambda title, pill, mp, lbl, hidden, see: f'''<div class="panel"><h2>{title} {pill}</h2>
<div class="mapbox">{mp}<span class="maplbl">{ic("pin")}{lbl}</span></div>{jobcard(hidden, "9 min" if hidden else "14 min")}
<div class="card" style="padding:12px 14px"><h3 style="font-size:var(--text-sm);margin-bottom:8px">Vacaturepagina</h3>{detail(hidden)}</div>
<div class="card" style="padding:12px 14px"><h3 style="font-size:var(--text-sm);margin-bottom:8px">Wat de kandidaat ziet</h3>{see}</div></div>'''
    body = f'''<div class="ph"><div><h1>Zo ziet de kandidaat het</h1><p>Dezelfde vacature, twee keuzes voor de kaart. De kandidaat ziet altijd dat het via een uitzendbureau gaat.</p></div></div>
<div class="cmp">{panel("Via onze vestiging", '<span class="pill line">standaard</span>', m1, "Pin op de vestiging van het bureau · cirkel = 15 min fietsen", True, see_h)}
{panel("Op echte werklocatie", '<span class="pill info">keuze per vacature</span>', m2, "Pin op het KvK-adres van de opdrachtgever · cirkel = 15 min fietsen", False, see_r)}</div>'''
    return f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><style>{CSS}</style></head><body>
<main style="padding:28px 40px">{body}</main><span class="demo">Voorbeelddata</span></body></html>'''

# ---------------------------------------------------------------- mobile
def m1():
    kp = "".join(f'<div class="card kpi"><small>{x}</small><div class="v">{v}</div><div class="dl">{d}</div></div>' for x, v, d in [
        ("Actieve vacatures", "18", '<span class="up">+3</span>'), ("Nieuwe sollicitaties", "86", '<span class="up">+14%</span>'),
        ("Eerste reactie", "1,4 d", '<span class="up">0,2 d sneller</span>'), ("Tokensaldo", "236", "± 9 weken")])
    td = [("bad", "clock", "9 sollicitaties > 48 uur", "Vooral Vb. Logistiek Zuid (6)"), ("wn", "building", "KvK-adres gewijzigd", "Vb. Bloemenexport · 2 vacatures"),
          ("wn", "pin", "Vacature op 24 km van je vestiging", "Orderpicker nachtdienst"), ("", "brief", "Nog geen live vacature", "Vb. Metaalbewerking")]
    li = "".join(f'<li><span class="ico {k}">{ic(i)}</span><div class="sp"><b>{t}</b><small>{m}</small></div>{ic("chevr")}</li>' for k, i, t, m in td)
    cl = "".join(f'<li><span class="av">{ini(n)}</span><div class="sp"><b>{n.replace("Voorbeeld ", "Vb. ")}</b><small>{c} · {lv} live · {ap} sollicitaties</small></div>{ic("chevr")}</li>' for n, _k, _v, c, _a, lv, ap, _km, _m in CLIENTS[:3])
    body = f'''<main class="mmain" style="padding-bottom:90px"><h1>Dashboard</h1><div class="muted sm">{AGENCY_SHORT} · 12 opdrachtgevers · 30 dagen</div>
<div class="mkpis">{kp}</div>
<div class="mh">Te doen <span class="cnt" style="margin:0">5</span><a>Alles</a></div><div class="mcard"><ul class="mlist">{li}</ul></div>
<div class="mh" style="margin-top:16px">Opdrachtgevers<a>Alle 12</a></div><div class="mcard"><ul class="mlist">{cl}</ul></div></main>'''
    return mpage_im(body, active="dash")

def m2():
    n, k, v, c, a, *_ = CLIENTS[0]
    top = f'<header class="mtop" style="padding-inline-start:4px"><span class="tb">{ic("arrowl")}</span><div style="line-height:1.2"><b>Nieuwe vacature</b><div style="font-size:var(--text-xs);opacity:.75">Stap 1 van 5 · Opdrachtgever &amp; locatie</div></div><span style="margin-inline-start:auto" class="tb">{ic("x")}</span></header>'
    mp = mapsvg(358, 150, pins=[(170, 100, AGENCY_SHORT, "agency"), (250, 62, "", "muted")])
    body = f'''<main class="mmain" style="padding-bottom:110px"><div class="form">
<div><span class="lbl">Opdrachtgever</span><div class="pick"><span class="av">{ini(n)}</span><div><b>Vb. Kwekerij De Kas BV</b><small>KvK {k} · {c}</small></div>{ic("chevd")}</div></div>
<div><span class="lbl">Werklocatie</span><div class="ro-field" style="grid-template-columns:auto 1fr">{ic("pin")}<b>Kassenweg 4, Honselersdijk</b><small>{ic("lock")} Uit KvK · niet te wijzigen</small></div></div>
<div><span class="lbl">Waar staat de vacature op de kaart?</span><div style="display:flex;flex-direction:column;gap:8px">
<div class="locc on"><span class="rd"></span><b>Toon op locatie van onze vestiging (standaard)</b><p>Kandidaten zien alleen {AGENCY_SHORT}, Naaldwijk.</p></div>
<div class="locc"><span class="rd"></span><b>Toon op echte werklocatie</b><p>Pin op het KvK-adres in {c}. Kandidaten zien de naam van de opdrachtgever.</p></div></div></div>
<div class="mcard" style="position:relative">{mp}<span class="maplbl" style="inset:8px auto auto 8px;bottom:auto">{ic("eye")}Zo ziet de kandidaat het</span></div>
<p class="muted sm">Reistijd rekenen we vanaf de plek op de kaart.</p></div></main>'''
    sticky = f'<div class="sticky"><span class="btn">Concept</span><span class="btn pri">Volgende {ic("chevr")}</span></div>'
    return mpage_im(body, extra=sticky, top=top, bnav=False)
