"""Shared UI for the Lobsy Intermediair (uitzendbureau) mockups.
Same shell and visual language as the werkgever redesign (bmui.py, copied here) — the intermediair
uses the /werkgever page set with a scope/role chip."""
from bmui import *  # noqa: F401,F403  (P, ic, LOGO, CSS, crumb, rohint, ...)
from bmui import CSS as BM_CSS

P.setdefault("bike", '<circle cx="6" cy="17" r="3.2"/><circle cx="18" cy="17" r="3.2"/><path d="M6 17l4-8h5l3 8M10 9l-1.5-3H6M13 9l-2 8"/>')
P.setdefault("layers", '<path d="M12 3l9 5-9 5-9-5 9-5z"/><path d="M3 13l9 5 9-5"/>')

IM_CSS = r'''
.scope .rolechip{display:inline-flex;align-items:center;gap:5px;height:22px;padding:0 8px;border-radius:var(--radius-pill);background:var(--gold-soft);color:var(--gold-ink);font-size:var(--text-xs);font-weight:600}
.scope .rolechip .i{width:12px;height:12px;color:var(--gold-ink)}
.scope .sep{width:1px;height:18px;background:color-mix(in srgb,var(--surface) 25%,transparent)}
.kvk{display:inline-flex;align-items:center;gap:5px;font-size:var(--text-xs);color:var(--muted);white-space:nowrap}
.kvk .i{width:12px;height:12px}
.ro-field{border:1px dashed var(--border);border-radius:var(--radius-sm);background:var(--pearl);padding:10px 12px;display:grid;grid-template-columns:auto 1fr auto;gap:2px 10px;align-items:center}
.ro-field .i{color:var(--muted)}
.ro-field b{font-size:var(--text-sm)}.ro-field small{grid-column:2;color:var(--muted);font-size:var(--text-xs)}
.ro-field .src{grid-row:1/span 2;grid-column:3;display:inline-flex;align-items:center;gap:4px;font-size:var(--text-xs);font-weight:600;color:var(--muted)}
.ro-field .src .i{width:12px;height:12px}
.locc{border:1px solid var(--border);border-radius:var(--radius);padding:12px 14px;display:grid;grid-template-columns:18px 1fr;gap:3px 10px;background:var(--surface)}
.locc .rd{width:16px;height:16px;border-radius:50%;border:1.5px solid var(--border);margin-top:2px}
.locc.on{border-color:var(--brand);background:color-mix(in srgb,var(--accent-soft) 55%,var(--surface))}.locc.on .rd{border:5px solid var(--brand)}
.locc b{font-size:var(--text-sm)}.locc p{grid-column:2;color:var(--muted);font-size:var(--text-xs)}
.locc .see{grid-column:2;margin-top:4px;display:block;font-size:var(--text-xs);color:var(--text)}
.locc .see .i{width:13px;height:13px;color:var(--brand);vertical-align:-2px;margin-inline-end:5px}
.form{display:flex;flex-direction:column;gap:14px}
.form .lbl{margin:0 0 6px}
.step{display:flex;gap:6px;align-items:center;font-size:var(--text-xs);color:var(--muted);margin-bottom:14px}
.step span{display:inline-flex;align-items:center;gap:6px;white-space:nowrap}
.step span b{width:22px;height:22px;border-radius:50%;display:grid;place-items:center;background:var(--pearl-mid);color:var(--muted);font-size:var(--text-xs)}
.step span.on{color:var(--brand);font-weight:600}.step span.on b{background:var(--brand);color:var(--surface)}
.step span.done b{background:var(--success-soft);color:var(--success)}
.step em{flex:1;height:1px;background:var(--border);min-width:18px}
.pick{border:1px solid var(--brand);box-shadow:0 0 0 3px var(--accent-soft);border-radius:var(--radius-sm);background:var(--surface);height:44px;display:flex;align-items:center;gap:10px;padding:0 12px}
.pick .av{width:28px;height:28px}.pick div{flex:1;min-width:0}.pick b{display:block;font-size:var(--text-sm)}.pick small{color:var(--muted);font-size:var(--text-xs)}
.jc{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius);overflow:hidden;display:grid;grid-template-columns:96px 1fr}
.jc .ph2{background:linear-gradient(135deg,color-mix(in srgb,var(--brand) 22%,var(--pearl)),var(--pearl-mid));display:grid;place-items:center;color:var(--brand)}
.jc .ph2 .i{width:28px;height:28px}
.jc .bd{padding:10px 12px;display:flex;flex-direction:column;gap:4px;min-width:0}
.jc h4{font-size:var(--text-md);font-weight:600}
.jc .co{display:flex;align-items:center;gap:6px;font-size:var(--text-sm)}
.jc .meta2{display:flex;flex-wrap:wrap;gap:4px 12px;font-size:var(--text-xs);color:var(--muted)}
.jc .meta2 span{display:inline-flex;align-items:center;gap:4px}.jc .meta2 .i{width:12px;height:12px}
.via{display:inline-flex;align-items:center;gap:5px;height:22px;padding:0 8px;border-radius:var(--radius-pill);background:var(--gold-soft);color:var(--gold-ink);font-size:var(--text-xs);font-weight:600;white-space:nowrap}
.via .i{width:12px;height:12px}
.cmp{display:grid;grid-template-columns:1fr 1fr;gap:20px}
.cmp h2{font-size:var(--text-lg);font-weight:600;display:flex;align-items:center;gap:8px}
.cmp .panel{display:flex;flex-direction:column;gap:12px}
.mapbox{position:relative;border-radius:var(--radius);overflow:hidden;border:1px solid var(--border);background:var(--pearl)}
.maplbl{position:absolute;inset:auto auto 12px 12px;background:var(--surface);border:1px solid var(--border);border-radius:var(--radius-sm);padding:6px 10px;font-size:var(--text-xs);display:flex;align-items:center;gap:6px;box-shadow:var(--shadow)}
.maplbl .i{width:13px;height:13px;color:var(--brand)}
.hidden-list{display:flex;flex-direction:column;gap:6px;font-size:var(--text-xs)}
.hidden-list div{display:flex;align-items:center;gap:8px}.hidden-list .i{width:14px;height:14px}
.hidden-list .y .i{color:var(--success)}.hidden-list .n{color:var(--muted)}.hidden-list .n .i{color:var(--muted)}
.clist a{display:grid;grid-template-columns:32px 1fr auto;gap:2px 10px;align-items:center;padding:10px 14px;border-top:1px solid color-mix(in srgb,var(--border) 60%,transparent)}
.clist a:first-child{border-top:0}
.clist a .av{grid-row:1/span 2}
.clist a b{font-size:var(--text-sm);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.clist a small{color:var(--muted);font-size:var(--text-xs);grid-column:2}
.clist a .cnt{grid-row:1/span 2;grid-column:3}
.clist a.on{background:var(--accent-soft)}.clist a.on b{color:var(--brand)}
.est{border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 12px;display:grid;grid-template-columns:18px 1fr auto;gap:2px 10px}
.est .rd{width:16px;height:16px;border-radius:50%;border:1.5px solid var(--border);margin-top:2px}
.est.on{border-color:var(--brand);background:color-mix(in srgb,var(--accent-soft) 55%,var(--surface))}.est.on .rd{border:5px solid var(--brand)}
.est small{grid-column:2;color:var(--muted);font-size:var(--text-xs)}
.est .pill{grid-row:1/span 2;grid-column:3;align-self:center}
.est.dis{opacity:.6}
body.m .step{margin-bottom:10px}
body.m .locc{padding:12px}
'''
CSS = BM_CSS + IM_CSS

AGENCY = "Voorbeeld Flexwerk Westland BV"
AGENCY_SHORT = "Voorbeeld Flexwerk Westland"
AGENCY_VEST = "Vestiging Naaldwijk"
AGENCY_ADDR = "Havenstraat 12, 2671 AB Naaldwijk"

CLIENTS = [  # name, kvk, vestigingsnr, city, address, live vacs, apps, km from agency vestiging, map mode default share
    ("Voorbeeld Kwekerij De Kas BV", "90123456", "000045123456", "Honselersdijk", "Kassenweg 4, 2675 LK Honselersdijk", 6, 31, 3, "verborgen"),
    ("Voorbeeld Logistiek Zuid BV", "90234567", "000045234567", "Barendrecht", "Distributieweg 20, 2991 LZ Barendrecht", 4, 22, 24, "echt"),
    ("Voorbeeld Groentepakhuis BV", "90345678", "000045345678", "Poeldijk", "Veilingweg 7, 2685 SB Poeldijk", 3, 14, 4, "verborgen"),
    ("Voorbeeld Bloemenexport BV", "90456789", "000045456789", "De Lier", "Hoofdstraat 88, 2678 CK De Lier", 2, 9, 5, "gemengd"),
    ("Voorbeeld Metaalbewerking BV", "90567890", "000045567890", "Maassluis", "Industrieweg 3, 3144 CL Maassluis", 2, 6, 11, "verborgen"),
    ("Voorbeeld Zorggroep Kust", "90678901", "000045678901", "Monster", "Duinweg 15, 2681 NB Monster", 1, 4, 6, "echt"),
]

def nav_im():
    return [
        ("Overzicht", [("dash", "grid", "Dashboard", ""), ("todo", "check", "Te doen", "5")]),
        ("Werving", [("vac", "brief", "Vacatures", ""), ("sol", "inbox", "Sollicitaties", "18"), ("talent", "users", "Talentpool", ""), ("ins", "bar", "Kandidaatinzichten", "")]),
        ("Opdrachtgevers", [("cli", "handshake", "Opdrachtgevers", "12")]),
        ("Organisatie", [("team", "shield2", "Team & rechten", ""), ("prof", "tag", "Bureauprofiel", ""), ("sal", "euro", "Salaristabellen", "")]),
        ("Tokens & facturen", [("tok", "coins", "Saldo & kopen", ""), ("fac", "receipt", "Facturen", "")]),
        ("Meer", [("api", "plug", "Koppelingen", ""), ("mat", "dl", "Wervingsmateriaal", "")]),
    ]

def sidebar_im(active, collapsed=()):
    out = []
    for g, items in nav_im():
        keys = [k for k, *_ in items]
        if g in collapsed and active not in keys:
            badge = sum(int(c) for *_, c in items if c)
            b = f'<span class="cnt" style="margin-inline-start:auto;margin-inline-end:6px">{badge}</span>' if badge else ""
            out.append(f'<div class="grp col"><h4><span>{g}</span>{b}{ic("chevr")}</h4></div>')
            continue
        li = "".join(f'<a class="it{" on" if k==active else ""}">{ic(i)}<span>{t}</span>{f"<span class=cnt>{c}</span>" if c else ""}{PTAG if k=="ins" else ""}</a>' for k, i, t, c in items)
        out.append(f'<div class="grp"><h4>{g}</h4>{li}</div>')
    foot = f'<a class="it">{ic("map")}<span>Banenkaart bekijken</span>{ic("ext")}</a>'
    return '<nav class="side">' + "".join(out) + f'<div class="sidefoot">{foot}</div></nav>'

def topbar_im(scope_label="Alle opdrachtgevers", scope_sub="12"):
    scope = (f'<span class="scope"><span class="rolechip">{ic("handshake")}Intermediair</span><span class="sep"></span>'
             f'{ic("users")}<b>{scope_label}</b><small>{scope_sub}</small>{ic("chevd")}</span>')
    return f'''<header class="top"><div class="brandm"><img src="{LOGO}" alt=""><b>Lobsy</b><span>Werkgever</span></div>
{scope}
<div class="gs bm">{ic("search")}<span>Zoek vacature, kandidaat of opdrachtgever…</span><span class="kbd">Ctrl K</span></div>
<div class="tr"><span class="tok">{ic("coins")}<b>236</b><small>tokens</small></span><span class="tb">{ic("help")}</span><span class="tb">{ic("bell")}<span class="dot"></span></span>
<div class="me"><span class="av">LB</span><div><b>Lotte Bos</b><small>Intermediair</small></div>{ic("chevd")}</div></div></header>'''

def page_im(active, crumbs, body, extra="", collapsed=(), demo="", scope=("Alle opdrachtgevers", "12")):
    return f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><style>{CSS}</style></head><body>
{topbar_im(*scope)}{sidebar_im(active, collapsed)}<main class="main">{crumb(crumbs)}{body}</main>{extra}<span class="demo {demo}">Voorbeelddata</span></body></html>'''

BNAV_IM = [("dash", "home", "Overzicht", ""), ("vac", "brief", "Vacatures", ""), ("sol", "inbox", "Sollicitaties", "18"), ("cli", "handshake", "Opdrachtgevers", ""), ("more", "menu", "Meer", "")]

def mpage_im(body, extra="", top=None, active="dash", bnav=True):
    top = top or f'''<header class="mtop" style="padding-inline-start:12px"><img src="{LOGO}" alt=""><span class="scope"><span class="rolechip">{ic("handshake")}Intermediair</span>{ic("chevd")}</span>
<span style="margin-inline-start:auto" class="tb">{ic("bell")}<span class="dot"></span></span><span class="av">LB</span></header>'''
    nav_html = ('<nav class="bnav">' + "".join(f'<a class="{"on" if k==active else ""}">{ic(i)}<span>{t}</span>{f"<span class=cnt>{c}</span>" if c else ""}</a>' for k, i, t, c in BNAV_IM) + '</nav>') if bnav else ""
    return f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><style>{CSS}</style></head><body class="m">
{top}{body}{nav_html}{extra}<span class="demo" style="bottom:{76 if bnav else 92}px">Voorbeelddata</span></body></html>'''

def mapsvg(w=720, h=380, pins=(), home=None, ring=None, line=None):
    """Neutral map: roads, water, optional candidate home + travel ring, pins (x, y, label, kind: agency|client|muted)."""
    roads = ('<rect width="100%" height="100%" style="fill:var(--pearl)"/>'
             f'<path d="M0 {h*0.18} C{w*0.25} {h*0.1} {w*0.45} {h*0.05} {w} {h*0.14} L{w} 0 L0 0 Z" style="fill:color-mix(in srgb,var(--brand) 10%,var(--pearl))"/>'
             f'<path d="M0 {h*0.64} C{w*0.3} {h*0.58} {w*0.55} {h*0.72} {w} {h*0.66}" style="stroke:var(--surface);stroke-width:7;fill:none"/>'
             f'<path d="M{w*0.48} 0 C{w*0.46} {h*0.4} {w*0.56} {h*0.7} {w*0.52} {h}" style="stroke:var(--surface);stroke-width:6;fill:none"/>'
             f'<path d="M{w*0.1} {h} C{w*0.2} {h*0.7} {w*0.3} {h*0.45} {w*0.18} {h*0.2}" style="stroke:var(--surface);stroke-width:4;fill:none"/>'
             f'<path d="M{w*0.7} {h} C{w*0.72} {h*0.8} {w*0.85} {h*0.5} {w} {h*0.42}" style="stroke:var(--surface);stroke-width:4;fill:none"/>')
    g = []
    if ring:
        cx, cy, r, lab = ring
        g.append(f'<circle cx="{cx}" cy="{cy}" r="{r}" style="fill:color-mix(in srgb,var(--brand) 7%,transparent);stroke:var(--brand);stroke-width:1.5;stroke-dasharray:6 5;opacity:.85"/>')
        if lab:
            g.append(f'<text x="{cx}" y="{cy - r - 8}" text-anchor="middle" style="font:600 12px Inter,sans-serif;fill:var(--brand)">{lab}</text>')
    if line:
        (x1, y1), (x2, y2) = line
        g.append(f'<path d="M{x1} {y1} L{x2} {y2}" style="stroke:var(--muted);stroke-width:1.5;stroke-dasharray:3 4"/>')
    if home:
        hx, hy = home
        g.append(f'<g transform="translate({hx},{hy})"><rect x="-11" y="-11" width="22" height="22" rx="6" style="fill:var(--brand-deep)"/><path d="M-6 1 L0 -5 L6 1 M-4 0 V6 H4 V0" style="stroke:var(--surface);stroke-width:1.6;fill:none"/></g>'
                 f'<text x="{hx+16}" y="{hy+4}" style="font:600 12px Inter,sans-serif;fill:var(--text)">Jouw adres</text>')
    for x, y, lab, kind in pins:
        col = {"agency": "var(--gold-deep)", "client": "var(--brand)", "muted": "var(--muted)"}[kind]
        dash = ' stroke-dasharray="3 3"' if kind == "muted" else ""
        g.append(f'<g transform="translate({x},{y})"><path d="M0 0 C-10 -12 -13 -18 -13 -24 A13 13 0 0 1 13 -24 C13 -18 10 -12 0 0Z" style="fill:var(--surface);stroke:{col};stroke-width:2.5"{dash}/><circle cy="-24" r="5" style="fill:{col}"/></g>')
        if lab:
            right = x + 17 + len(lab) * 7.2 > w - 6
            tx, anc = (x - 17, "end") if right else (x + 17, "start")
            g.append(f'<text x="{tx}" y="{y-20}" text-anchor="{anc}" style="font:600 12px Inter,sans-serif;fill:var(--text);paint-order:stroke;stroke:var(--pearl);stroke-width:4px">{lab}</text>')
    return f'<svg viewBox="0 0 {w} {h}" width="100%" height="{h}" preserveAspectRatio="xMidYMid slice" style="display:block">{roads}{"".join(g)}</svg>'
