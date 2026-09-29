"""Dashboard + Vacatures: ONE shared page per screen, rendered per role (bm / rm / vm)."""
from bmui import *

def kpi(label, v, dl, cls="up", sp=None, icon=None):
    s = spark(sp) if sp else ""
    d = f'<span class="{cls}">{dl}</span>' if cls else dl
    return f'<div class="card kpi"><small>{ic(icon) if icon else ""}{label}</small><div style="display:flex;align-items:flex-end"><div><div class="v">{v}</div><div class="dl">{d}</div></div>{s}</div></div>'

def todo_item(kind, icon, title, meta, action, dis_note=None):
    btn = f'<span class="btn sm">{action}</span>' if action else ""
    return f'<li><span class="ico {kind}">{ic(icon)}</span><div class="t"><b>{title}</b><small>{meta}</small></div>{btn}</li>'

BRANCHES = [  # vestiging, regio, live, nieuw, reactietijd, status
    ("Naaldwijk", "Westland", 7, 14, "1,2 d", "ok"), ("Monster", "Westland", 4, 11, "3,4 d", "wn"),
    ("’s-Gravenzande", "Westland", 3, 6, "1,8 d", "ok"), ("De Lier", "Westland", 2, 3, "—", "wn"),
    ("Honselersdijk", "Westland", 4, 9, "1,5 d", "ok"), ("Delft", "Delfland", 6, 21, "0,9 d", "ok"),
    ("Pijnacker", "Delfland", 3, 8, "2,2 d", "ok"), ("Ridderkerk", "Rotterdam-Zuid", 5, 17, "4,1 d", "bad"),
    ("Barendrecht", "Rotterdam-Zuid", 4, 12, "1,6 d", "ok"),
]

def dashboard(role="bm"):
    ro = role == "rm"
    if role == "rm":
        kp = [kpi("Actieve vacatures", "20", "+2", sp=[14,15,15,17,18,18,20]),
              kpi("Nieuwe sollicitaties", "43", "+9%", sp=[30,34,33,38,36,40,43]),
              kpi("Gem. eerste reactie", "2,0 dagen", "0,3 d sneller", sp=[3,2.8,2.6,2.5,2.3,2.2,2.0]),
              kpi("Aangenomen", "6", "+1", sp=[3,4,4,5,5,5,6]),
              kpi("Tokenverbruik regio", "31", "van 188 toegewezen", cls="", sp=[20,22,24,25,27,29,31])]
        todo_h = f'<div class="ch"><h2>Signalen in je regio</h2><span class="pill line">{ic("eye")}Ter info</span><span class="sp"></span><a>Alle signalen {ic("chevr")}</a></div>'
        todos = [
            todo_item("bad", "clock", "6 sollicitaties langer dan 48 uur zonder reactie", "Monster (5), Honselersdijk (1) · reageren doet de vestigingsmanager", "Bekijken"),
            todo_item("wn", "flag", "Publicatieaanvraag Naaldwijk wacht op goedkeuring", "Teamleider teelt · 3 tokens · de bedrijfsmanager keurt goed", None),
            todo_item("wn", "cal", "2 vacatures verlopen binnen 7 dagen", "Monster, ’s-Gravenzande", "Bekijken"),
            todo_item("", "user", "De Lier heeft nog geen vestigingsmanager", "Uitnodiging van 22-09 nog niet geaccepteerd", None),
        ]
        rows = [b for b in BRANCHES if b[1] == "Westland"]
        title, sub = "Regio dashboard", "Regio Westland · 5 vestigingen · laatste 30 dagen"
        act = f'<span class="seg"><span>7 d</span><span class="on">30 d</span><span>90 d</span></span><span class="btn">{ic("dl")}Exporteren</span><span style="position:relative;display:inline-flex"><span class="btn pri dis">{ic("lock")}Vacature plaatsen</span><span class="tip" style="bottom:calc(100% + 8px);inset-inline-end:0">Plaatsen doet de vestigings- of bedrijfsmanager</span></span>'
        hint = rohint("Je ziet alle vestigingen in regio Westland. Reageren, publiceren en tokens kopen doen de vestigings- en bedrijfsmanagers.")
    else:
        kp = [kpi("Actieve vacatures", "48", "+4", sp=[38,40,41,43,44,46,48]),
              kpi("Nieuwe sollicitaties", "186", "+12%", sp=[140,150,148,160,170,176,186]),
              kpi("Gem. eerste reactie", "2,1 dagen", "0,4 d sneller", sp=[3.2,3,2.9,2.6,2.4,2.3,2.1]),
              kpi("Aangenomen", "19", "+3", sp=[10,12,13,14,15,17,19]),
              kpi("Tokensaldo", "412", "± 7 weken bij huidig verbruik", cls="", icon="coins")]
        todo_h = f'<div class="ch"><h2>Te doen</h2><span class="cnt" style="margin:0">7</span><span class="sp"></span><a>Alles bekijken {ic("chevr")}</a></div>'
        todos = [
            todo_item("wn", "flag", "3 publicatieaanvragen wachten op jouw goedkeuring", "Naaldwijk, Delft, Barendrecht · samen 9 tokens", "Beoordelen"),
            todo_item("bad", "clock", "17 sollicitaties langer dan 48 uur zonder reactie", "Vooral Ridderkerk (8) en Monster (5)", "Bekijken"),
            todo_item("wn", "cal", "5 vacatures verlopen binnen 7 dagen", "Verlengen kost 1 token per vacature", "Verlengen"),
            todo_item("", "coins", "Pijnacker heeft nog 3 tokens", "Gemiddeld verbruik: 9 tokens per maand", "Tokens verdelen"),
            todo_item("", "user", "De Lier heeft nog geen vestigingsmanager", "Uitnodiging van 22-09 nog niet geaccepteerd", "Opnieuw sturen"),
            todo_item("", "handshake", "Overnameverzoek voor vestiging Rhoon", "Aangevraagd door Voorbeeld Kwekerij Oost BV op 28-09", "Beoordelen"),
        ]
        rows = BRANCHES[:8]
        title, sub = "Dashboard", f"{COMPANY} · 14 vestigingen in 3 regio’s · laatste 30 dagen"
        act = f'<span class="seg"><span>7 d</span><span class="on">30 d</span><span>90 d</span></span><span class="btn">{ic("dl")}Exporteren</span><span class="btn pri">{ic("plus")}Vacature plaatsen</span>'
        hint = ""
    trs = "".join(
        f'<tr><td><b>{n}</b>{"" if ro else f"<div class=\"muted sm\">{r}</div>"}</td><td class="num">{l}</td><td class="num">{nw}</td><td class="num">{rt}</td>'
        f'<td><span class="dotst {"" if s=="ok" else s}">{ {"ok":"Op schema","wn":"Aandacht","bad":"Achterstand"}[s] }</span></td></tr>' for n, r, l, nw, rt, s in rows)
    fun = [("Sollicitaties", 100, "186" if not ro else "43"), ("Geaccepteerd", 38, "71" if not ro else "17"),
           ("Uitgenodigd", 22, "38" if not ro else "9"), ("Aangenomen", 11, "19" if not ro else "6")]
    funnel = "".join(f'<div><span class="muted">{a}</span><span class="bar"><i style="width:{w}%"></i></span><b class="num" style="text-align:end">{v}</b></div>' for a, w, v in fun)
    left = f'<div class="card" style="overflow:hidden">{todo_h}<ul class="todo">{"".join(todos)}</ul></div>'
    if True:
        left += f'<div class="card"><div class="ch"><h2>Wervingstrechter</h2><span class="muted sm">{"regio Westland" if ro else "alle vestigingen"} · 30 dagen</span></div><div class="funnel">{funnel}</div></div>'
    right = f'''<div class="card" style="overflow:hidden"><div class="ch"><h2>{"Vestigingen in je regio" if ro else "Vestigingen"}</h2><span class="sp"></span><a>{"Naar vestigingen" if ro else "Alle 14 vestigingen"} {ic("chevr")}</a></div>
<table><thead><tr><th>Vestiging</th><th class="num">Live vac.</th><th class="num">Nieuw</th><th class="num">1e reactie</th><th>Status</th></tr></thead><tbody>{trs}</tbody></table></div>'''
    body = f'''<div class="ph"><div><h1>{title}</h1><p>{sub}</p></div><div class="act">{act}</div></div>{hint}
<div class="kpis">{"".join(kp)}</div>
<div class="row" style="grid-template-columns:minmax(0,1.1fr) minmax(0,1fr);align-items:start"><div class="row" style="gap:16px">{left}</div>{right}</div>'''
    crumbs = ["Regio Westland", "Dashboard"] if ro else [COMPANY, "Dashboard"]
    return page(role, "dash", crumbs, body)

VACS = [  # titel, vestiging, type, status(kind,label), nieuw, totaal, weerg, tot, extras, sel
    ("Teamleider teelt", "Naaldwijk", "Fulltime", ("wn", "Wacht op goedkeuring"), 0, 0, "—", "—", "Aanvraag S. Bakker · 3 tokens", False),
    ("Chauffeur C/E", "Delft", "Fulltime", ("wn", "Wacht op goedkeuring"), 0, 0, "—", "—", "Aanvraag R. Aydın · 3 tokens", False),
    ("Oogstmedewerker kas", "Naaldwijk", "Parttime", ("ok", "Actief"), 6, 14, "1.204", "18-10-2026", "Uitgelicht", True),
    ("Productiemedewerker inpak", "Ridderkerk", "Flex", ("ok", "Actief"), 8, 23, "2.310", "03-10-2026", "Pushbericht", True),
    ("Heftruckchauffeur", "Monster", "Fulltime", ("ok", "Actief"), 3, 9, "860", "05-10-2026", "", False),
    ("Medewerker orderpicking", "Barendrecht", "Parttime", ("ok", "Actief"), 2, 12, "1.022", "27-10-2026", "Uitgelicht", False),
    ("Monteur klimaatinstallaties", "Delft", "Fulltime", ("ok", "Actief"), 1, 4, "642", "14-11-2026", "", False),
    ("Planner logistiek", "Pijnacker", "Fulltime", ("off", "Concept"), 0, 0, "—", "—", "Nog 2 velden invullen", False),
    ("Bijbaan weekend kas", "’s-Gravenzande", "Bijbaan", ("off", "Vervuld"), 0, 7, "1.540", "gesloten 21-09", "", False),
]

def vacatures(role="bm"):
    vm = role == "vm"
    data = [v for v in VACS if v[1] == "Naaldwijk"] + ([
        ("Inpakker bloemen (ochtend)", "Naaldwijk", "Parttime", ("ok", "Actief"), 2, 6, "488", "30-10-2026", "", False),
        ("Kasmedewerker bijbaan", "Naaldwijk", "Bijbaan", ("ok", "Actief"), 4, 11, "930", "02-10-2026", "", False),
        ("Magazijnmedewerker", "Naaldwijk", "Fulltime", ("off", "Concept"), 0, 0, "—", "—", "Klaar om te publiceren", False),
        ("Seizoenskracht oogst", "Naaldwijk", "Flex", ("off", "Inactief"), 0, 18, "2.020", "gedeactiveerd 12-09", "", False)] if vm else []) if vm else VACS
    rows = []
    for t, b, ty, (sk, sl), nw, tot, wg, until, ex, sel in data:
        pend = sl == "Wacht op goedkeuring"
        vis = "".join(f'<span class="pill info" style="margin-inline-end:4px">{ic("star" if e=="Uitgelicht" else "zap")}{e}</span>' for e in [ex] if ex in ("Uitgelicht", "Pushbericht"))
        info = ex if ex not in ("Uitgelicht", "Pushbericht") else ""
        sub = f'{"" if vm else b + " · "}{ty}' + (f' · <span class="{"" if not pend else ""}">{info}</span>' if info else "")
        nwc = f'<span class="cnt" style="margin:0 6px 0 0;background:var(--brand);color:var(--surface)">{nw}</span>' if nw else ""
        if pend and not vm:
            acts = f'<span class="btn sm pri">{ic("check")}Goedkeuren</span><span class="ra">{ic("more")}</span>'
        elif pend and vm:
            acts = f'<span class="btn sm">Aanvraag intrekken</span>'
        elif sl == "Concept":
            acts = f'<span class="btn sm">Afmaken</span><span class="ra">{ic("more")}</span>'
        elif sl == "Actief":
            acts = f'<span class="ra">{ic("inbox")}</span><span class="ra">{ic("more")}</span>'
        else:
            acts = f'<span class="btn sm">{"Heropenen" if sl=="Inactief" else "Dupliceren"}</span><span class="ra">{ic("more")}</span>'
        pill = "wn" if sk == "wn" else ("ok" if sk == "ok" else "")
        stl = "Wacht op bedrijfsmanager" if (pend and vm) else sl
        rows.append(f'<tr class="{"sel" if sel else ("hl" if pend else "")}"><td style="width:36px">{cb(sel)}</td><td><b>{t}</b><div class="muted sm">{sub}</div></td>'
                    f'<td><span class="pill {pill}">{stl}</span></td><td class="num">{nwc}{tot if tot else "—"}</td><td class="num">{wg}</td><td>{until}</td><td>{vis or "<span class=muted>—</span>"}</td>'
                    f'<td style="text-align:end"><div style="display:inline-flex;gap:6px;align-items:center">{acts}</div></td></tr>')
    if vm:
        tabs = ["Alle <span class=cnt>12</span>", "Actief <span class=cnt>7</span>", "Wacht op goedkeuring <span class=cnt>1</span>", "Concept <span class=cnt>2</span>", "Verloopt binnenkort <span class=cnt>1</span>", "Gesloten <span class=cnt>1</span>"]
        filt = f'<div class="inp w">{ic("search")}Zoek op titel of vacaturenummer</div><span class="dd">Type <em>Alle</em>{ic("chevd")}</span><span class="dd">Salaristabel <em>Alle</em>{ic("chevd")}</span>'
        note = f'<div class="note">{ic("coins")}<span>Vestiging Naaldwijk heeft <b style="color:var(--text)">38 tokens</b>, toegewezen door de bedrijfsmanager. Publiceer je boven dit saldo, dan gaat de vacature als aanvraag naar de bedrijfsmanager.</span><a>Tokens bekijken</a></div>'
        head = f'<div class="ph"><div><h1>Vacatures</h1><p>Vacatures van vestiging Naaldwijk.</p></div><div class="act"><span class="btn">{ic("dl")}Exporteren</span><span class="btn pri">{ic("plus")}Vacature plaatsen</span></div></div>'
        crumbs = ["Vestiging Naaldwijk", "Vacatures"]
        bulk = f'<div class="bulk"><b>1 geselecteerd</b><span class="btn">{ic("refresh")}Verlengen · 1 token</span><span class="btn">{ic("star")}Uitlichten · 2 tokens</span><span class="btn">Deactiveren</span><span style="flex:1"></span><span class="btn" style="border:0">{ic("x")}</span></div>'
        pag = "7 van 12 vacatures"
    else:
        tabs = ["Alle <span class=cnt>64</span>", "Actief <span class=cnt>48</span>", "Wacht op goedkeuring <span class=cnt>3</span>", "Concept <span class=cnt>6</span>", "Verloopt binnenkort <span class=cnt>5</span>", "Gesloten <span class=cnt>7</span>"]
        filt = f'<div class="inp" style="width:220px">{ic("search")}Zoek op titel of nummer</div><span class="dd on">Vestiging <em style="color:var(--brand)">Alle 14</em>{ic("chevd")}</span><span class="dd">Regio <em>Alle</em>{ic("chevd")}</span><span class="dd">Type <em>Alle</em>{ic("chevd")}</span><span class="dd">Geplaatst door <em>Iedereen</em>{ic("chevd")}</span>'
        note = f'<div class="note">{ic("info")}3 vestigingen vragen tokens aan om te publiceren. Goedkeuren boekt de tokens af van het centrale saldo (412). <a>Alleen aanvragen tonen</a></div>'
        head = f'<div class="ph"><div><h1>Vacatures</h1><p>Alle vacatures van je 14 vestigingen.</p></div><div class="act"><span class="btn">{ic("dl")}Exporteren</span><span class="btn pri">{ic("plus")}Vacature plaatsen</span></div></div>'
        crumbs = [COMPANY, "Vacatures"]
        bulk = f'<div class="bulk"><b>2 geselecteerd</b><span class="btn">{ic("refresh")}Verlengen · 2 tokens</span><span class="btn">{ic("star")}Uitlichten · 4 tokens</span><span class="btn">Dupliceren</span><span class="btn">Deactiveren</span><span style="flex:1"></span><span class="btn" style="border:0">{ic("x")}</span></div>'
        pag = "9 van 64 vacatures"
    tabs_html = '<div class="tabs" style="margin:0;padding:12px 16px 0">' + "".join(f'<span class="{"on" if i==0 else ""}">{t}</span>' for i, t in enumerate(tabs)) + '</div>'
    body = f'''{head}<div class="card" style="overflow:hidden">{tabs_html}<div class="fbar">{filt}<span style="flex:1"></span><span class="btn sm">{ic("cols")}Kolommen</span></div>{note}{bulk}
<table><thead><tr><th></th><th>Vacature</th><th>Status</th><th class="num">Sollicitaties</th><th class="num">Weergaven</th><th>Online tot</th><th>Zichtbaarheid</th><th></th></tr></thead><tbody>{"".join(rows)}</tbody></table>
<div class="pag"><span>{pag}</span><span class="sp"></span><span class="btn sm">{ic("chevl")}</span><span class="btn sm">{ic("chevr")}</span></div></div>'''
    return page(role, "vac", crumbs, body, collapsed=("Meer",))
