"""School portal (Schoolbeheerder) + Leraar portal. Desktop 1440x900.
Reuses the enterprise visual language 1:1: tokens, icons, sidebar, topbar and CSS from
../admin-redesign (ui.py, ui_css.py) and ../bedrijfsmanager-redesign (bmui.py)."""
import pathlib, sys
HERE = pathlib.Path(__file__).resolve().parent
M = HERE.parent
# docs/mockups/scholen ships its own copy in ./base; the design workspace uses the sibling folders.
sys.path[:0] = [str(HERE / "base")] if (HERE / "base").exists() else [str(M / "admin-redesign"), str(M / "bedrijfsmanager-redesign")]
from ui import P, ic, LOGO, cb          # noqa: E402
from bmui import CSS as BM_CSS          # noqa: E402

P.setdefault("school", '<path d="M3 10 12 5l9 5-9 5z"/><path d="M7 12v5c3 2 7 2 10 0v-5M21 10v6"/>')
P.setdefault("hash", '<path d="M5 9h14M5 15h14M10 4 8 20M16 4l-2 16"/>')
P.setdefault("print", '<path d="M7 9V3h10v6"/><rect x="3" y="9" width="18" height="8" rx="2"/><path d="M7 14h10v7H7z"/>')
P.setdefault("pie", '<path d="M12 3v9h9A9 9 0 1 1 12 3z"/><path d="M15 3.5A9 9 0 0 1 20.5 9H15z"/>')
P.setdefault("file", '<path d="M14 3H6a1 1 0 0 0-1 1v16a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V8z"/><path d="M14 3v5h5M9 13h6M9 17h6"/>')
P.setdefault("target", '<circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1.5"/>')
P.setdefault("door", '<path d="M5 21V4a1 1 0 0 1 1-1h9l4 2v16"/><path d="M3 21h18M12 12h.01"/>')
P.setdefault("minus", '<path d="M5 12h14"/>')
P.setdefault("pen", '<path d="M4 20h4L19 9l-4-4L4 16z"/>')

EXTRA = r'''
.sidefoot .it{color:var(--text)}.sidefoot .it .i{color:var(--muted)}
.privnote{display:flex;gap:8px;align-items:flex-start;font-size:var(--text-xs);color:var(--muted);padding:6px 10px 0;line-height:1.4}.privnote .i{width:14px;height:14px;color:var(--success);margin-top:1px}
.fa{display:inline-flex;align-items:center;gap:5px;height:22px;padding:0 8px;border-radius:var(--radius-pill);font-size:var(--text-xs);font-weight:600;background:var(--brand);color:var(--surface);white-space:nowrap}.fa .i{width:12px;height:12px}
.fa.soft{background:var(--accent-soft);color:var(--brand)}
td .bar{display:inline-block;width:110px;vertical-align:middle}.pct{display:inline-block;min-width:38px;text-align:end;font-variant-numeric:tabular-nums;color:var(--muted);margin-inline-start:8px}
.hol{font-family:ui-monospace,Menlo,monospace;font-weight:600;letter-spacing:.08em;color:var(--brand)}
.code{font-family:ui-monospace,Menlo,monospace;font-weight:600;letter-spacing:.06em}
.codes{display:grid;grid-template-columns:repeat(4,1fr);gap:6px}
.codes span{font-family:ui-monospace,Menlo,monospace;font-size:var(--text-sm);font-weight:600;letter-spacing:.06em;border:1px dashed var(--border);border-radius:6px;padding:6px 0;text-align:center;background:var(--pearl)}
.codes span small{display:block;font-family:var(--font);font-weight:400;letter-spacing:0;color:var(--muted);font-size:var(--text-xs)}
.codes span.more{border-style:solid;background:var(--surface);color:var(--muted);font-family:var(--font);letter-spacing:0;display:grid;place-items:center}
.stepper{display:inline-flex;align-items:center;border:1px solid var(--border);border-radius:var(--radius-sm);overflow:hidden;height:34px}
.stepper span{width:34px;height:34px;display:grid;place-items:center;color:var(--brand)}.stepper b{min-width:48px;text-align:center;font-size:var(--text-md);border-inline:1px solid var(--border);height:34px;display:grid;place-items:center}
.fgrid{display:grid;grid-template-columns:1fr 1fr;gap:10px 12px}
.fgrid .lbl{margin:0 0 5px}.fgrid .field{width:100%}.fgrid .full{grid-column:1/-1}
.field.dd2{justify-content:space-between}.field.dd2 .i{color:var(--muted);width:14px;height:14px}
.okbox{display:flex;gap:10px;align-items:flex-start;background:var(--success-soft);border-radius:var(--radius-sm);padding:10px 12px;font-size:var(--text-xs);color:var(--text)}.okbox .i{color:var(--success);margin-top:1px}
.infobox{display:flex;gap:10px;align-items:flex-start;background:var(--accent-soft);border-radius:var(--radius-sm);padding:10px 12px;font-size:var(--text-xs);color:var(--text)}.infobox .i{color:var(--brand);margin-top:1px}
.lockset{border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 12px;display:grid;grid-template-columns:auto 1fr auto;gap:2px 10px;align-items:center}
.lockset p{grid-column:2/-1;color:var(--muted);font-size:var(--text-xs)}
.lockset .sw{opacity:.9}
.rbar{display:grid;grid-template-columns:22px 118px 1fr 34px;gap:8px;align-items:center;padding:5px 16px;font-size:var(--text-xs)}
.rbar .bar{height:9px}.rbar em{font-style:normal;font-weight:600;color:var(--brand);font-family:ui-monospace,Menlo,monospace}.rbar .n{text-align:end;color:var(--muted);font-variant-numeric:tabular-nums}
.rank li{list-style:none;display:grid;grid-template-columns:20px 1fr auto;gap:8px;align-items:center;padding:6px 16px;font-size:var(--text-sm)}
.rank li span.k{width:20px;height:20px;border-radius:50%;background:var(--accent-soft);color:var(--brand);font-size:var(--text-xs);font-weight:600;display:grid;place-items:center}
.rank li small{color:var(--muted);font-size:var(--text-xs)}
.kchip{display:inline-flex;align-items:center;gap:5px;height:26px;padding:0 10px;border-radius:var(--radius-pill);border:1px solid var(--border);background:var(--surface);font-size:var(--text-xs);white-space:nowrap}
.kchip b{color:var(--brand)}
.minn{font-size:var(--text-xs);color:var(--muted);padding:0 16px 12px;display:flex;gap:6px;align-items:center}.minn .i{width:13px;height:13px}
.story{font-size:var(--text-md);line-height:1.6;color:var(--text);padding:0 16px 14px}
.mtiles{display:grid;grid-template-columns:repeat(4,1fr);gap:10px;padding:0 16px 16px}
.mtile{background:var(--pearl);border-radius:var(--radius-sm);padding:10px 12px}
.mtile small{display:block;color:var(--muted);font-size:var(--text-xs)}.mtile b{display:block;font-size:var(--text-sm);color:var(--brand);margin-top:2px}.mtile p{font-size:var(--text-xs);color:var(--muted);margin-top:2px}
.route{padding:4px 16px 12px;display:flex;flex-direction:column;gap:0}
.route li{list-style:none;display:grid;grid-template-columns:22px 1fr;gap:10px;position:relative;padding-bottom:10px}
.route li::before{content:"";position:absolute;inset-inline-start:10px;top:20px;bottom:-2px;width:2px;background:var(--border)}.route li:last-child::before{display:none}
.route li i{width:22px;height:22px;border-radius:50%;background:var(--accent-soft);color:var(--brand);font-style:normal;font-size:var(--text-xs);font-weight:600;display:grid;place-items:center;position:relative}
.route li.done i{background:var(--success-soft);color:var(--success)}.route li.done i .i{width:13px;height:13px;stroke-width:2.6}
.route li b{display:block}.route li small{color:var(--muted);font-size:var(--text-xs)}
.checks li{list-style:none;display:flex;gap:8px;align-items:flex-start;padding:3px 16px;font-size:var(--text-sm)}.checks li .i{margin-top:2px;color:var(--success)}.checks li.lr .i{color:var(--brand)}
.subh{font-size:var(--text-xs);font-weight:600;color:var(--muted);text-transform:uppercase;letter-spacing:.03em;padding:8px 16px 4px}
.cpill{display:inline-flex;align-items:center;gap:6px;height:26px;padding:0 10px;border-radius:var(--radius-pill);background:var(--pearl-mid);font-size:var(--text-xs);white-space:nowrap}
.stbar{display:flex;height:10px;border-radius:5px;overflow:hidden;background:var(--pearl-mid)}.stbar i{display:block;height:100%}
.leg2{display:flex;gap:14px;font-size:var(--text-xs);color:var(--muted);margin-top:8px}.leg2 span{display:inline-flex;align-items:center;gap:6px}.leg2 span i{width:10px;height:10px;border-radius:3px;display:inline-block}
.clist{border:1px solid var(--border);border-radius:var(--radius-sm);overflow:hidden}
.clist td{height:34px}.clist th{background:var(--pearl)}
.nameline{display:block;height:1px;border-bottom:1.5px dotted var(--muted);opacity:.6;margin-top:10px;width:100%}
.clmore{font-size:var(--text-xs);color:var(--muted);padding:7px 10px;border-top:1px solid var(--border);background:var(--pearl)}
.nonames{display:flex;gap:10px;align-items:flex-start;background:var(--success-soft);border-radius:var(--radius-sm);padding:10px 12px;font-size:var(--text-xs)}.nonames .i{color:var(--success);margin-top:1px}
'''
CSS = BM_CSS + EXTRA

SCHOOL = "Voorbeeld College Westland"

# ------------------------------------------------------------------ shells
def nav(role):
    if role == "school":
        return [
            ("Overzicht", [("dash", "grid", "Dashboard", ""), ("todo", "check", "Te doen", "3")]),
            ("Leerlingen", [("klas", "users", "Klassen & codes", "8"), ("res", "bar", "Resultaten", "")]),
            ("Team", [("ler", "shield2", "Leraren", "7")]),
            ("School", [("prof", "school", "Schoolgegevens", ""), ("priv", "lock", "Privacy & ouders", "1"), ("mat", "print", "Lesbrief & materiaal", "")]),
        ]
    return [
        ("Mijn klas", [("dash", "grid", "Klasoverzicht", ""), ("codes", "hash", "Leerlingcodes", "28"), ("groep", "pie", "Groepsresultaten", ""), ("droom", "target", "Droombanen", "")]),
        ("In de les", [("venster", "door", "Testvenster", ""), ("mat", "print", "Codelijst & lesbrief", "")]),
        ("Mijn klassen", [("k2b", "users", "Klas 2B", ""), ("k3a", "users", "Klas 3A", "")]),
    ]

def sidebar(role, active):
    out = []
    for g, items in nav(role):
        li = "".join(f'<a class="it{" on" if k==active else ""}">{ic(i)}<span>{t}</span>{f"<span class=cnt>{c}</span>" if c else ""}</a>' for k, i, t, c in items)
        out.append(f'<div class="grp"><h4>{g}</h4>{li}</div>')
    foot = (f'<a class="it">{ic("help")}<span>Hulp voor scholen</span>{ic("ext")}</a>'
            f'<p class="privnote">{ic("shield2")}<span>Lobsy kent geen leerlingnamen. Alleen codes. De namenlijst houdt de school zelf.</span></p>')
    return '<nav class="side">' + "".join(out) + f'<div class="sidefoot">{foot}</div></nav>'

def topbar(role):
    if role == "school":
        scope = f'<span class="scope fixed">{ic("school")}<b>{SCHOOL}</b><small>Naaldwijk</small></span>'
        who = ('JV', 'J. Visser', 'Schoolbeheerder · 2FA actief'); search = "Zoek klas, code of leraar…"
    else:
        scope = f'<span class="scope">{ic("users")}<b>Klas 2B</b><small>mavo/havo 2</small>{ic("chevd")}</span>'
        who = ('RJ', 'R. Jansen', 'Leraar · 2FA actief'); search = "Zoek een code, bijv. K7Q…"
    return f'''<header class="top"><div class="brandm"><img src="{LOGO}" alt=""><b>Lobsy</b><span>School</span></div>
{scope}
<div class="gs bm">{ic("search")}<span>{search}</span><span class="kbd">Ctrl K</span></div>
<div class="tr"><span class="tb">{ic("help")}</span><span class="tb">{ic("bell")}</span>
<div class="me"><span class="av">{who[0]}</span><div><b>{who[1]}</b><small>{who[2]}</small></div>{ic("chevd")}</div></div></header>'''

def crumb(parts):
    return '<div class="crumb">' + f' {ic("chevr")} '.join(parts[:-1] + [f"<b>{parts[-1]}</b>"]) + '</div>'

def page(role, active, crumbs, body, extra="", demo=""):
    return f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><style>{CSS}</style></head><body>
{topbar(role)}{sidebar(role, active)}<main class="main">{crumb(crumbs)}{body}</main>{extra}<span class="demo {demo}">Voorbeelddata</span></body></html>'''

def kpi(label, icon, v, sub):
    return f'<div class="card kpi"><small>{ic(icon)}{label}</small><div class="v">{v}</div><div class="dl">{sub}</div></div>'

def bar(p, wn=False): return f'<span class="bar{" wn" if wn else ""}"><i style="width:{p}%"></i></span>'

CLASSES = [  # klas, niveau, leraar, codes, gestart, klaar, venster
    ("1A", "mavo/havo 1", "S. de Boer", 27, 0, 0, "Nog niet open"),
    ("1B", "vmbo-k 1", "M. Yilmaz", 24, 9, 2, "Open t/m 9 okt"),
    ("2A", "havo/vwo 2", "P. Smit", 29, 29, 26, "Gesloten"),
    ("2B", "mavo/havo 2", "R. Jansen", 28, 25, 19, "Open t/m 9 okt"),
    ("2C", "vmbo-t 2", "L. Bakker", 26, 18, 11, "Open t/m 16 okt"),
    ("3A", "havo 3", "R. Jansen", 24, 24, 23, "Gesloten"),
    ("3B", "vmbo-b/k 3", "K. Mulder", 22, 14, 6, "Open t/m 16 okt"),
    ("3C", "vwo 3", "A. Kok", 32, 30, 28, "Gesloten"),
]
RIASEC = [("R", "Doen & maken"), ("I", "Onderzoeken"), ("A", "Creatief"), ("S", "Helpen"), ("E", "Leiden & regelen"), ("C", "Ordenen")]

def venster_pill(v):
    if v.startswith("Open"): return f'<span class="pill ok">{v}</span>'
    if v == "Gesloten": return f'<span class="pill line">{v}</span>'
    return f'<span class="pill wn">{v}</span>'

# ------------------------------------------------------------------ SCHOOL
def s1():
    tot = sum(c[3] for c in CLASSES); klaar = sum(c[5] for c in CLASSES); bezig = sum(c[4]-c[5] for c in CLASSES)
    k = "".join([
        kpi("Klassen", "users", "8", "<span class=muted>leerjaar 1 t/m 3</span>"),
        kpi("Leerlingcodes", "hash", f"{tot}", "<span class=muted>geen namen bij Lobsy</span>"),
        kpi("Tests klaar", "check", f"{klaar}", f"<span class=up>{round(klaar/tot*100)}%</span><span class=muted>van alle codes</span>"),
        kpi("Nu bezig", "clock", f"{bezig}", "<span class=muted>gem. 24 min per test</span>"),
        kpi("Leraren", "shield2", "7", "<span class=muted>6 met 2FA ·</span><span class=down>1 wacht</span>"),
    ])
    rows = ""
    for kl, niv, men, n, st, kl_, v in CLASSES:
        p = round(kl_/n*100)
        rows += (f'<tr class="{"sel" if kl=="2B" else ""}"><td><b>Klas {kl}</b><div class="muted sm">{niv}</div></td><td>{men}</td><td class="num">{n}</td><td class="num">{st}</td><td class="num"><b>{kl_}</b></td>'
                 f'<td>{bar(p, p<40)}<span class="pct">{p}%</span></td><td>{venster_pill(v)}</td><td class="num"><span class="ra">{ic("chevr")}</span></td></tr>')
    todo = [("wn", "shield2", "K. Mulder (3B) heeft 2FA nog niet ingesteld", "Uitnodiging 25-09 · kan pas inloggen na 2FA", "Herinneren"),
            ("", "print", "Klas 1A: codelijst nog niet geprint", "27 codes klaar om uit te delen", "Printen"),
            ("wn", "mail", "Ouderbrief klas 1B nog niet bevestigd", "Nodig vóór het testvenster opent (onder 16)", "Bevestigen")]
    td = "".join(f'<li><span class="ico {c}">{ic(i)}</span><div class="t"><b>{t}</b><small>{s}</small></div><span class="btn sm">{a}</span></li>' for c, i, t, s, a in todo)
    ri = [34, 18, 22, 41, 15, 20]
    mx = max(ri)
    rb = "".join(f'<div class="rbar"><em>{l}</em><span>{n}</span>{bar(round(v/mx*100))}<span class="n">{v}</span></div>' for (l, n), v in zip(RIASEC, ri))
    body = f'''<div class="ph"><div><h1>Dashboard</h1><p>{SCHOOL} · schooljaar 2026–2027 · 8 klassen</p></div>
<div class="act"><div class="seg"><span class="on">Dit schooljaar</span><span>Deze maand</span></div><span class="btn">{ic("dl")}Exporteren</span><span class="btn pri">{ic("plus")}Nieuwe klas</span></div></div>
<div class="kpis">{k}</div>
<div class="row" style="grid-template-columns:minmax(0,1fr) 380px">
<div class="card" style="overflow:hidden"><div class="ch"><h2>Tests per klas</h2><span class="pill">{klaar} van {tot} klaar</span><span class="sp"></span><a>Alle klassen{ic("chevr")}</a></div>
<table><thead><tr><th>Klas</th><th>Leraar</th><th class="num">Codes</th><th class="num">Gestart</th><th class="num">Klaar</th><th>Voltooiing</th><th>Testvenster</th><th></th></tr></thead><tbody>{rows}</tbody></table></div>
<div style="display:flex;flex-direction:column;gap:var(--space-4)">
<div class="card"><div class="ch"><h2>Te doen</h2><span class="pill">3</span></div><ul class="todo">{td}</ul></div>
<div class="card"><div class="ch"><h2>Interesses · hele school</h2><span class="sp"></span><a>Resultaten{ic("chevr")}</a></div>
<p class="muted sm" style="padding:0 16px 6px">Hoe vaak een richting in de top-3 staat · {klaar} leerlingen klaar</p>{rb}
<p class="minn" style="padding-top:8px">{ic("lock")}Groepscijfers pas vanaf 5 leerlingen per klas.</p></div>
</div></div>'''
    return page("school", "dash", [SCHOOL, "Dashboard"], body, demo="l")

def s2():
    rows = ""
    for kl, niv, men, n, st, kl_, v in CLASSES[:7]:
        rows += (f'<tr class="{"sel" if kl=="2C" else ""}"><td><b>Klas {kl}</b><div class="muted sm">{niv}</div></td><td>{men}</td><td class="num">{n}</td>'
                 f'<td>{venster_pill(v)}</td><td class="num"><span class="ra">{ic("more")}</span></td></tr>')
    rows += (f'<tr class="hl"><td><b>Klas 2D</b><div class="muted sm">vmbo-t 2</div></td><td>L. Bakker</td><td class="num">28</td><td><span class="pill info">{ic("check")}Net gemaakt</span></td><td class="num"><span class="ra">{ic("more")}</span></td></tr>')
    codes = ["K7Q-M2P", "B4X-T9R", "H3N-W6C", "Y8D-F2K", "R5G-P7V", "T2M-X4H"]
    cl = "".join(f'<tr><td class="muted">{i+1}</td><td><span class="code">{c}</span></td><td><span class="nameline"></span></td></tr>' for i, c in enumerate(codes))
    drawer = f'''<div class="scrim"></div><aside class="drawer" style="width:520px"><div class="dh"><div><h2>Nieuwe klas</h2><p class="muted sm">Stap 2 van 2 · codelijst uitdelen</p></div><span class="x">{ic("x")}</span></div>
<div class="db" style="gap:12px">
<div class="fgrid">
<div><span class="lbl">Naam klas</span><span class="field">2D</span></div>
<div><span class="lbl">Leerjaar en niveau</span><span class="field dd2">vmbo-t 2{ic("chevd")}</span></div>
<div><span class="lbl">Aantal leerlingen</span><span class="stepper"><span>{ic("minus")}</span><b>28</b><span>{ic("plus")}</span></span></div>
<div><span class="lbl">Leraar</span><span class="field dd2">L. Bakker{ic("chevd")}</span></div>
</div>
<div class="okbox">{ic("check")}<div><b>28 leerlingcodes gemaakt</b><br>Elke leerling krijgt één code. Een code is geen naam: Lobsy weet niet wie erachter zit.</div></div>
<div><span class="lbl" style="margin-top:0">Voorbeeld codelijst</span>
<div class="clist"><table><thead><tr><th style="width:36px">Nr.</th><th style="width:110px">Code</th><th>Naam (vul zelf in)</th></tr></thead><tbody>{cl}</tbody></table>
<p class="clmore">+ 22 codes · op de print staat de naamkolom leeg</p></div></div>
<div class="nonames">{ic("shield2")}<div><b>Lobsy bewaart geen namen</b><br>Ook geen voornamen. Vul de namen pas in op papier of in jullie eigen leerlingsysteem. Zo blijft de lijst bij de school.</div></div>
<div class="lockrow on">{ic("check")}<span>Ouders zijn geïnformeerd en kunnen bezwaar maken <span class="muted">· bevestigd door J. Visser</span></span></div>
</div>
<div class="df"><span class="btn">{ic("dl")}Download CSV</span><span class="sp" style="flex:1"></span><span class="btn pri">{ic("print")}Codelijst printen / PDF</span></div></aside>'''
    body = f'''<div class="ph"><div><h1>Klassen & codes</h1><p>Maak een klas, kies het aantal leerlingen en deel de codes uit.</p></div>
<div class="act"><span class="btn">{ic("print")}Codelijst printen/downloaden (PDF/CSV)</span><span class="btn pri">{ic("plus")}Nieuwe klas</span></div></div>
<div class="card" style="overflow:hidden;max-width:880px"><div class="fbar"><span class="inp w">{ic("search")}Zoek klas of leraar…</span><span class="dd">Leerjaar <em>Alle</em>{ic("chevd")}</span><span class="dd">Testvenster <em>Alle</em>{ic("chevd")}</span></div>
<table><thead><tr><th>Klas</th><th>Leraar</th><th class="num">Codes</th><th>Testvenster</th><th></th></tr></thead><tbody>{rows}</tbody></table></div>'''
    return page("school", "klas", [SCHOOL, "Leerlingen", "Klassen & codes"], body, extra=drawer, demo="l")

def s3():
    T = [("RJ", "R. Jansen", "r.jansen@voorbeeldcollege.nl", "Leraar", "2B, 3A", "ok", "Actief", "via Microsoft", "Vandaag 08:10"),
         ("PS", "P. Smit", "p.smit@voorbeeldcollege.nl", "Leraar", "2A", "ok", "Actief", "Authenticator-app", "Gisteren"),
         ("LB", "L. Bakker", "l.bakker@voorbeeldcollege.nl", "Leraar", "2C, 2D", "ok", "Actief", "via Microsoft", "Gisteren"),
         ("MY", "M. Yilmaz", "m.yilmaz@voorbeeldcollege.nl", "Leraar", "1B", "ok", "Actief", "Authenticator-app", "28-09-2026"),
         ("SB", "S. de Boer", "s.deboer@voorbeeldcollege.nl", "Leraar", "1A", "ok", "Actief", "via Microsoft", "27-09-2026"),
         ("AK", "A. Kok", "a.kok@voorbeeldcollege.nl", "Leraar", "3C", "ok", "Actief", "Authenticator-app", "25-09-2026"),
         ("KM", "K. Mulder", "k.mulder@voorbeeldcollege.nl", "Leraar", "3B", "wn", "Wacht op 2FA", "Nog niet ingesteld", "—")]
    rows = "".join(f'<tr class="{"sel" if n=="K. Mulder" else ""}"><td><div class="who"><span class="av">{a}</span><div><b>{n}</b><small>{e}</small></div></div></td><td>{k}</td>'
                   f'<td><span class="pill {c}">{ic("lock")}{s}</span><div class="muted sm" style="margin-top:2px">{m}</div></td><td class="muted">{l}</td><td class="num"><span class="ra">{ic("more")}</span></td></tr>' for a, n, e, r, k, c, s, m, l in T)
    kl = "".join(f'<label class="kchip" style="height:32px">{cb(k in ("3B",))}Klas {k}</label>' for k in ["1A", "1B", "2A", "2B", "2C", "2D", "3A", "3B", "3C"])
    drawer = f'''<div class="scrim"></div><aside class="drawer" style="width:470px"><div class="dh"><div><h2>Leraar uitnodigen</h2><p class="muted sm">De leraar krijgt een mail en stelt eerst 2FA in.</p></div><span class="x">{ic("x")}</span></div>
<div class="db" style="gap:14px">
<div class="fgrid"><div class="full"><span class="lbl">Naam</span><span class="field">T. van Dijk</span></div>
<div class="full"><span class="lbl">E-mail van school</span><span class="field">t.vandijk@voorbeeldcollege.nl</span><p class="muted sm" style="margin-top:4px">Alleen adressen op @voorbeeldcollege.nl</p></div></div>
<div class="infobox">{ic("users")}<div><b>Een leraar ziet alleen de klassen die je hier aanvinkt.</b><br>Geen andere klassen, geen schoolbrede cijfers. Aanpassen kan later.</div></div>
<div><span class="lbl" style="margin-top:0">Klassen van deze leraar</span><div style="display:flex;flex-wrap:wrap;gap:6px">{kl}</div></div>
<div class="lockset">{ic("lock")}<b>Tweestapsverificatie</b><span class="fa">{ic("lock")}Verplicht</span><p>Staat altijd aan voor leraren en schoolbeheerders. Inloggen met het Microsoft- of Google-account van school telt ook als 2FA.</p></div>
</div>
<div class="df"><span class="sp" style="flex:1"></span><span class="btn ghost">Annuleren</span><span class="btn pri">{ic("send")}Uitnodiging sturen</span></div></aside>'''
    body = f'''<style>td{{height:54px}}</style><div class="ph"><div><h1>Leraren</h1><p>7 leraren · 2FA is verplicht voor iedereen met toegang tot resultaten.</p></div>
<div class="act"><span class="fa soft">{ic("lock")}2FA verplicht</span><span class="btn pri">{ic("plus")}Leraar uitnodigen</span></div></div>
<div class="card" style="overflow:hidden;max-width:880px"><div class="fbar"><span class="inp w">{ic("search")}Zoek leraar…</span><span class="dd">Klas <em>Alle</em>{ic("chevd")}</span><span class="dd">2FA <em>Alle</em>{ic("chevd")}</span></div>
<table><thead><tr><th>Leraar</th><th>Eigen klassen</th><th>2FA</th><th>Laatst actief</th><th></th></tr></thead><tbody>{rows}</tbody></table></div>'''
    return page("school", "ler", [SCHOOL, "Team", "Leraren"], body, extra=drawer, demo="l")

CODES = [  # code, status, done/60, holland, drijfveer, droombaan
    ("K7Q-M2P", "Klaar", 60, "RIS", "Helpen", "Dierenarts"),
    ("B4X-T9R", "Klaar", 60, "AES", "Vrijheid", "Game developer"),
    ("H3N-W6C", "Bezig", 41, "", "", ""),
    ("Y8D-F2K", "Klaar", 60, "SEA", "Samen", "Kapper"),
    ("R5G-P7V", "Klaar", 60, "RCI", "Zekerheid", "Elektricien"),
    ("T2M-X4H", "Niet gestart", 0, "", "", ""),
    ("W9C-J3N", "Klaar", 60, "ESC", "Presteren", "Ondernemer"),
    ("F6P-R8B", "Bezig", 12, "", "", ""),
    ("N4V-K2Y", "Klaar", 60, "SIA", "Wereld beter", "Verpleegkundige"),
    ("D7J-H5T", "Klaar", 60, "IRC", "Presteren", "Programmeur"),
    ("P3R-C9W", "Klaar", 60, "ASE", "Vrijheid", "—"),
]
def st_pill(s):
    return {"Klaar": '<span class="pill ok">Klaar</span>', "Bezig": '<span class="pill info">Bezig</span>'}.get(s, '<span class="pill line">Niet gestart</span>')

def s4():
    rows = "".join(f'<tr><td><span class="code">{c}</span></td><td>{st_pill(s)}</td><td>{bar(round(n/60*100))}<span class="pct">{n}/60</span></td><td><span class="hol">{h or "—"}</span></td><td>{d or "—"}</td><td>{j or "—"}</td></tr>' for c, s, n, h, d, j in CODES[:10])
    tabs = "".join(f'<span class="{"on" if k=="2B" else ""}">Klas {k}</span>' for k in ["1B", "2A", "2B", "2C", "3A", "3B", "3C"])
    body = f'''<div class="ph"><div><h1>Resultaten</h1><p>Per klas en per code. Namen zie je hier nooit. Die staan alleen op de eigen lijst van de leraar.</p></div>
<div class="act"><span class="btn">{ic("dl")}Exporteer (CSV, zonder antwoorden)</span></div></div>
<div class="tabs">{tabs}</div>
<div class="row" style="grid-template-columns:minmax(0,1fr) 360px;align-items:start">
<div class="card" style="overflow:hidden"><div class="ch"><h2>Klas 2B · per code</h2><span class="pill">19 van 28 klaar</span><span class="sp"></span><span class="muted sm">Leraar R. Jansen</span></div>
<table><thead><tr><th>Code</th><th>Status</th><th>Voortgang</th><th>Interessecode</th><th>Top-drijfveer</th><th>Droombaan</th></tr></thead><tbody>{rows}</tbody></table>
<div class="pag"><span>1–10 van 28</span><span class="sp"></span><span class="btn sm">{ic("chevl")}</span><span class="btn sm">{ic("chevr")}</span></div></div>
<div class="card"><div class="ch"><h2>Wat de school ziet</h2><span class="sp"></span><span class="pill ok">Per code: aan</span></div>
<ul class="checks">
<li>{ic("check")}Aantal tests per klas en per code</li>
<li>{ic("check")}Korte uitkomst per code (interessecode, drijfveer, droombaan)</li>
<li class="lr">{ic("eyeoff")}<span>Geen losse antwoorden, hobby’s of het verhaal. Dat ziet alleen de leraar van de klas.</span></li>
<li class="lr">{ic("eyeoff")}<span>Geen werkgevers, vacatures of opleidingslinks. Nooit.</span></li></ul>
<p class="minn" style="padding-top:10px;align-items:flex-start">{ic("info")}<span>Lobsy-beheer kan ‘resultaten per code’ uitzetten. Dan zie je alleen totalen per klas (vanaf 5 leerlingen).</span></p></div>
</div>'''
    return page("school", "res", [SCHOOL, "Leerlingen", "Resultaten"], body)

# ------------------------------------------------------------------ TEACHER
def t1():
    CODES_T = CODES[:10]
    k = "".join([
        kpi("Klaar", "check", "19 <span class=muted style='font-size:var(--text-sm);font-weight:400'>/ 28</span>", "<span class=up>68%</span><span class=muted>van de klas</span>"),
        kpi("Bezig", "clock", "6", "<span class=muted>gem. bij vraag 34</span>"),
        kpi("Nog niet gestart", "hash", "3", "<span class=muted>codes T2M, G8W, V3K</span>"),
        kpi("Gem. tijd", "clock", "26 min", "<span class=muted>past in 1 lesuur</span>"),
    ])
    rows = "".join(f'<tr class="{"sel" if c.startswith("K7Q") else ""}"><td><span class="code">{c}</span></td><td>{st_pill(s)}</td><td>{bar(round(n/60*100))}<span class="pct">{n}/60</span></td><td><span class="hol">{h or "—"}</span></td><td>{d or "—"}</td><td>{j or "—"}</td><td class="num">{"<span class=ra>"+ic("file")+"</span>" if s=="Klaar" else ""}<span class="ra">{ic("chevr")}</span></td></tr>' for c, s, n, h, d, j in CODES_T)
    ri = [9, 5, 7, 11, 4, 6]; mx = max(ri)
    rb = "".join(f'<div class="rbar"><em>{l}</em><span>{n}</span>{bar(round(v/mx*100))}<span class="n">{v}</span></div>' for (l, n), v in zip(RIASEC, ri))
    dv = [("Helpen", "7 leerlingen"), ("Vrijheid", "5"), ("Presteren", "4"), ("Samen", "2"), ("Zekerheid", "1")]
    rk = "".join(f'<li><span class="k">{i+1}</span><span>{a}</span><small>{b}</small></li>' for i, (a, b) in enumerate(dv))
    dj = "".join(f'<span class="kchip">{a} <b>{b}</b></span>' for a, b in [("Dierenarts", 3), ("Game developer", 2), ("Kapper", 2), ("Programmeur", 2), ("Verpleegkundige", 1), ("Weet ik nog niet", 4)])
    body = f'''<div class="ph"><div><h1>Klas 2B</h1><p>mavo/havo 2 · 28 leerlingcodes · testvenster open t/m vrijdag 9 oktober</p></div>
<div class="act"><span class="btn">{ic("door")}Testvenster sluiten</span><span class="btn">{ic("print")}Codelijst</span><span class="btn pri">{ic("dl")}Klasrapport (PDF)</span></div></div>
<div class="kpis" style="grid-template-columns:repeat(4,minmax(0,1fr))">{k}</div>
<style>.tt1 td .bar{{width:72px}}</style><div class="row" style="grid-template-columns:minmax(0,1fr) 372px">
<div class="card tt1" style="overflow:hidden"><div class="fbar"><span class="inp w">{ic("search")}Zoek code…</span><span class="dd">Status <em>Alle</em>{ic("chevd")}</span><span class="sp" style="flex:1"></span><span class="muted sm">Namen staan op jouw eigen lijst</span></div>
<table><thead><tr><th>Code</th><th>Status</th><th>Voortgang</th><th>Interesse</th><th>Drijfveer</th><th>Droombaan</th><th></th></tr></thead><tbody>{rows}</tbody></table>
<div class="pag"><span>1–10 van 28</span><span class="sp"></span><span class="btn sm">{ic("chevl")}</span><span class="btn sm">{ic("chevr")}</span></div></div>
<div style="display:flex;flex-direction:column;gap:var(--space-4)">
<div class="card"><div class="ch"><h2>Interesses van de klas</h2><span class="sp"></span><span class="muted sm">19 klaar</span></div>{rb}<div style="height:8px"></div></div>
<div class="card"><div class="ch"><h2>Top-drijfveren</h2></div><ul class="rank">{rk}</ul><div style="height:4px"></div></div>
<div class="card"><div class="ch"><h2>Droombanen</h2><span class="sp"></span><span class="muted sm">vanaf 5 klaar</span></div><div class="chips">{dj}</div></div>
</div></div>'''
    return page("teacher", "dash", ["Klas 2B", "Klasoverzicht"], body)

def t2():
    tiles = [("Zo ben jij", "Zorgzaam & precies", "persoonlijkheid"), ("Dit doe je graag", "Maken · onderzoeken", "interesses · RIS"),
             ("Dit vind je belangrijk", "Anderen helpen", "drijfveren"), ("Hier voel je je thuis", "Klein team, rustig", "sfeer")]
    mt = "".join(f'<div class="mtile"><small>{a}</small><b>{b}</b><p>{c}</p></div>' for a, b, c in tiles)
    story = ("Jij bent een rustige maker met een groot hart. Je merkt snel als iemand zich niet fijn voelt, en dan help je. "
             "Je bent graag met je handen bezig en je wilt precies weten hoe iets werkt. "
             "Het liefst doe je iets waar anderen, of dieren, blij van worden. Je voelt je thuis in een klein groepje waar het rustig en eerlijk is.")
    body = f'''<div class="ph"><div><h1><span class="code" style="font-size:inherit">K7Q-M2P</span></h1><p>Klas 2B · klaar op 29-09-2026 · 60 van 60 vragen · 27 min</p></div>
<div class="act"><span class="btn">{ic("chevl")}Vorige code</span><span class="btn">Volgende code{ic("chevr")}</span><span class="btn pri">{ic("dl")}PDF downloaden</span></div></div>
<div class="rohint">{ic("eyeoff")}<b>Je ziet een code, geen naam.</b><span>Zoek de naam op je eigen lijst. Losse antwoorden zie je niet, alleen de uitkomst.</span></div>
<div class="row" style="grid-template-columns:minmax(0,1fr) 420px">
<div style="display:flex;flex-direction:column;gap:var(--space-4)">
<div class="card"><div class="ch"><h2>Dit ben jij</h2><span class="pill info">Verhaal zoals de leerling het zag</span></div><p class="story">{story}</p><div class="mtiles">{mt}</div></div>
<div class="row" style="grid-template-columns:1fr 1fr">
<div class="card"><div class="ch"><h2>Vindt het leuk</h2></div><div class="chips">{"".join(f'<span class="cpill">{t}</span>' for t in ["Tekenen", "Dieren verzorgen", "Fietsen repareren", "Koken"])}</div></div>
<div class="card"><div class="ch"><h2>Vindt het niet leuk</h2></div><div class="chips">{"".join(f'<span class="cpill">{t}</span>' for t in ["Voor de klas praten", "Lang stilzitten"])}</div></div></div>
<div class="card"><div class="ch"><h2>Gespreksstarters voor het gesprek met de leerling</h2></div><ul class="checks" style="padding-bottom:12px">
<li class="lr">{ic("help")}Je helpt graag. Wanneer merkte je laatst dat iemand hulp nodig had?</li>
<li class="lr">{ic("help")}Welk vak past bij ‘weten hoe iets werkt’? Biologie, NaSk?</li>
<li class="lr">{ic("help")}Voor de klas praten vind je niet leuk. Wat zou helpen?</li></ul></div>
</div>
<div class="card"><div class="ch"><h2>Droombaan-checker</h2><span class="sp"></span><span class="muted sm">ingevuld door leerling</span></div>
<div style="padding:0 16px 10px;display:flex;align-items:center;gap:10px"><span class="ico">{ic("target")}</span><div><b style="font-size:var(--text-lg)">Dierenarts</b><div class="muted sm">Past goed bij wat de leerling al heeft</div></div></div>
<div style="padding:0 16px 12px"><div class="stbar"><i style="width:60%;background:var(--success)"></i><i style="width:40%;background:var(--accent-soft)"></i></div>
<div class="leg2"><span><i style="background:var(--success)"></i>3 dingen heb je al</span><span><i style="background:var(--accent-soft)"></i>2 ga je nog leren</span></div></div>
<p class="subh">Heb je al</p><ul class="checks"><li>{ic("check")}Zorgzaam voor mens en dier</li><li>{ic("check")}Nieuwsgierig hoe iets werkt</li><li>{ic("check")}Precies en netjes werken</li></ul>
<p class="subh">Ga je nog leren</p><ul class="checks"><li class="lr">{ic("book")}Biologie en scheikunde goed onder de knie</li><li class="lr">{ic("book")}Rustig blijven als een dier pijn heeft</li></ul>
<p class="subh">Zo kom je er</p><ol class="route">
<li class="done"><i>{ic("check")}</i><div><b>Nu: klas 2</b><small>Kies straks biologie en scheikunde in je pakket</small></div></li>
<li><i>2</i><div><b>Havo/vwo afmaken</b><small>Of via vmbo → mbo Dierenartsassistent (niveau 4)</small></div></li>
<li><i>3</i><div><b>Diergeneeskunde</b><small>Universiteit Utrecht · 6 jaar</small></div></li>
<li><i>4</i><div><b>Dierenarts</b><small>In een praktijk, dierentuin of op een boerderij</small></div></li></ol>
<p class="minn">{ic("shield2")}Geen opleidingslinks, werkgevers of vacatures voor leerlingen.</p></div>
</div>'''
    return page("teacher", "codes", ["Klas 2B", "Leerlingcodes", "K7Q-M2P"], body)

SCREENS = [("sc-s1-school-dashboard", s1), ("sc-s2-klassen-codes", s2), ("sc-s3-leraren-2fa", s3), ("sc-s4-resultaten-per-code", s4),
           ("sc-t1-leraar-klasoverzicht", t1), ("sc-t2-leraar-code-detail", t2)]
