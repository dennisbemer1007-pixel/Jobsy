"""Shared UI for the Lobsy werkgever (bedrijfsmanager / regiomanager / vestigingsmanager) mockups.
Reuses tokens, icons and base CSS from the admin-redesign mockups (copied into ./base) so both redesigns speak one visual language."""
import pathlib, sys
BASE = pathlib.Path(__file__).resolve().parent / "base"  # copy of admin-redesign ui.py / ui_css.py (same visual language)
sys.path.insert(0, str(BASE))
from ui import P, ic, LOGO, spark, cb  # noqa: E402
from ui_css import CSS as BASE_CSS      # noqa: E402

P.setdefault("pin", '<path d="M12 21s-7-6.2-7-11.5A7 7 0 0 1 19 9.5C19 14.8 12 21 12 21z"/><circle cx="12" cy="9.5" r="2.5"/>')
P.setdefault("arrowl", '<path d="M19 12H5M11 18l-6-6 6-6"/>')
P.setdefault("home", '<path d="M3 11l9-7 9 7"/><path d="M5 10v10h14V10"/>')
P.setdefault("bar", '<path d="M4 20V10M10 20V4M16 20v-7M22 20H2"/>')
P.setdefault("star", '<path d="M12 3l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z"/>')
P.setdefault("zap", '<path d="M13 2L4 14h7l-1 8 9-12h-7z"/>')
P.setdefault("cal", '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/>')

EXTRA = r'''
.scope{height:36px;display:inline-flex;align-items:center;gap:9px;padding:0 10px 0 9px;border-radius:var(--radius-sm);background:color-mix(in srgb,var(--surface) 12%,transparent);color:var(--surface);white-space:nowrap}
.scope .i{color:color-mix(in srgb,var(--surface) 80%,transparent)}
.scope b{font-size:var(--text-sm)}.scope small{font-size:var(--text-xs);color:color-mix(in srgb,var(--surface) 70%,transparent)}
.scope.fixed{background:transparent;border:1px solid color-mix(in srgb,var(--surface) 25%,transparent)}
.ro{display:inline-flex;align-items:center;gap:6px;height:24px;padding:0 10px;border-radius:var(--radius-pill);background:var(--accent-soft);color:var(--brand);font-size:var(--text-xs);font-weight:600;white-space:nowrap}
.ro .i{width:13px;height:13px}
.tok{display:inline-flex;align-items:center;gap:7px;height:32px;padding:0 10px;border-radius:var(--radius-sm);color:var(--surface);white-space:nowrap}
.tok .i{color:var(--gold)}.tok small{color:color-mix(in srgb,var(--surface) 70%,transparent);font-size:var(--text-xs)}
.gs.bm{max-width:420px}.gs.bm span{white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.sidefoot{position:absolute;inset:auto 12px 14px 12px;border-top:1px solid var(--border);padding-top:10px;display:flex;flex-direction:column;gap:2px}
.sidefoot .it{color:var(--brand)}.sidefoot .it .i{color:var(--brand)}
.sidefoot p{font-size:var(--text-xs);color:var(--muted);padding:4px 10px 0;line-height:1.4}
.rohint{display:flex;align-items:center;gap:10px;background:var(--accent-soft);color:var(--brand);border-radius:var(--radius-sm);padding:9px 14px;font-size:var(--text-sm);margin-bottom:var(--space-4)}
.rohint .i{flex:none}.rohint span{color:var(--text)}.rohint .sp{flex:1}
.btn.dis{opacity:.5;border-style:dashed}.btn.dis .i{width:13px;height:13px}
.tip{position:absolute;z-index:5;background:var(--brand-deep);color:var(--surface);font-size:var(--text-xs);border-radius:6px;padding:6px 9px;white-space:nowrap;box-shadow:var(--shadow-lg)}
.tip::after{content:"";position:absolute;top:100%;inset-inline-end:22px;border:6px solid transparent;border-top-color:var(--brand-deep)}
.todo li{list-style:none;display:flex;align-items:center;gap:12px;padding:10px 16px;border-top:1px solid color-mix(in srgb,var(--border) 60%,transparent)}
.todo li .t{flex:1;min-width:0}.todo li .t b{display:block}.todo li .t small{color:var(--muted);font-size:var(--text-xs)}
.bar{display:block;height:6px;border-radius:3px;background:var(--pearl-mid);overflow:hidden;min-width:60px}.bar i{display:block;height:100%;background:var(--brand);border-radius:3px}
.bar.wn i{background:var(--warn)}
.funnel{display:flex;flex-direction:column;gap:8px;padding:4px 16px 14px}
.funnel div{display:grid;grid-template-columns:110px 1fr 56px;gap:10px;align-items:center;font-size:var(--text-xs)}
.funnel .bar{height:10px;border-radius:5px}.funnel .bar i{border-radius:5px}
.kb{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:12px;align-items:start}
.kcol{background:var(--pearl-mid);border-radius:var(--radius);padding:8px;display:flex;flex-direction:column;gap:8px}
.kcol h3{font-size:var(--text-sm);font-weight:600;display:flex;align-items:center;gap:8px;padding:4px 4px 2px}
.kcol h3 small{font-weight:400;color:var(--muted);font-size:var(--text-xs)}
.kc{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 11px;display:flex;flex-direction:column;gap:6px}
.kc .hd{display:flex;align-items:center;gap:8px}.kc .hd b{flex:1;min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.kc .av{width:26px;height:26px;font-size:var(--text-xs)}.kc .av.anon{background:var(--pearl-mid);color:var(--muted)}.kc .av .i{width:14px;height:14px}
.kc .facts{color:var(--muted);font-size:var(--text-xs);display:flex;flex-wrap:wrap;gap:3px 10px}
.kc .ft{display:flex;align-items:center;gap:6px;font-size:var(--text-xs);color:var(--muted)}.kc .ft .i{width:12px;height:12px}
.kc.sel{border-color:var(--brand);box-shadow:0 0 0 2px var(--accent-soft)}
.mt{font-weight:600;color:var(--brand);font-size:var(--text-xs);background:var(--accent-soft);border-radius:var(--radius-pill);padding:1px 7px}
.steps{display:flex;align-items:center;gap:6px;padding:12px 20px 0;font-size:var(--text-xs);color:var(--muted)}
.steps span{display:inline-flex;align-items:center;gap:5px;white-space:nowrap}.steps span .i{width:13px;height:13px}
.steps span.done{color:var(--success)}.steps span.cur{color:var(--brand);font-weight:600}
.steps em{flex:1;height:1px;background:var(--border)}
.lockrow{display:flex;align-items:center;gap:8px;font-size:var(--text-xs);padding:6px 0}.lockrow .i{width:14px;height:14px}
.lockrow.on .i{color:var(--success)}.lockrow.off{color:var(--muted)}
.tree{padding:6px 8px 10px}
.tree a{display:flex;align-items:center;gap:8px;height:32px;padding:0 8px;border-radius:var(--radius-sm);white-space:nowrap}
.tree a .i{width:15px;height:15px;color:var(--muted)}.tree a .cnt{margin-inline-start:auto}
.tree a.on{background:var(--accent-soft);color:var(--brand);font-weight:600}.tree a.on .i{color:var(--brand)}
.tree .l1{padding-inline-start:22px}.tree .l2{padding-inline-start:46px}
.tree .add{color:var(--brand);font-weight:600}.tree .add .i{color:var(--brand)}
.rolec{border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 12px;display:grid;grid-template-columns:18px 1fr;gap:2px 10px}
.rolec .rd{width:16px;height:16px;border-radius:50%;border:1.5px solid var(--border);margin-top:2px}
.rolec.on{border-color:var(--brand);background:color-mix(in srgb,var(--accent-soft) 55%,var(--surface))}.rolec.on .rd{border:5px solid var(--brand)}
.rolec p{grid-column:2;color:var(--muted);font-size:var(--text-xs)}
.pk{border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 12px;display:flex;flex-direction:column;gap:2px}
.pk.on{border-color:var(--brand);background:color-mix(in srgb,var(--accent-soft) 55%,var(--surface))}
.pk b{font-size:var(--text-md)}.pk small{color:var(--muted);font-size:var(--text-xs)}
.hbar{display:grid;grid-template-columns:150px 1fr 40px;gap:10px;align-items:center;padding:5px 16px;font-size:var(--text-xs)}
.hbar .bar{height:8px}
.chips{display:flex;flex-wrap:wrap;gap:6px;padding:4px 16px 14px}
.map{position:relative;height:100%;min-height:360px;border-radius:0 0 var(--radius) var(--radius);overflow:hidden;background:var(--pearl)}
.leg{position:absolute;inset:auto auto 12px 12px;background:var(--surface);border:1px solid var(--border);border-radius:var(--radius-sm);padding:8px 10px;font-size:var(--text-xs);display:flex;flex-direction:column;gap:5px}
.leg .sw2{display:inline-flex;gap:2px}.leg .sw2 i{width:18px;height:10px;border-radius:2px;background:var(--brand)}

.demo.l{inset-inline-start:264px}
body.m .mtop .scope{height:32px;padding:0 8px}
.bnav{position:fixed;inset:auto 0 0 0;height:64px;background:var(--surface);border-top:1px solid var(--border);display:grid;grid-template-columns:repeat(5,1fr);z-index:45}
.bnav a{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:3px;font-size:var(--text-xs);color:var(--muted);position:relative}
.bnav a .i{width:20px;height:20px}.bnav a.on{color:var(--brand);font-weight:600}
.bnav a .cnt{position:absolute;top:6px;inset-inline-start:calc(50% + 4px);margin:0;background:var(--brand);color:var(--surface)}
.mcard{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius);overflow:hidden}
.mh{font-size:var(--text-md);font-weight:600;margin:4px 0 8px;display:flex;align-items:center;gap:8px}.mh a{margin-inline-start:auto;font-size:var(--text-xs);color:var(--brand)}
.sticky{position:fixed;inset:auto 0 0 0;background:var(--surface);border-top:1px solid var(--border);padding:12px 16px 20px;display:grid;grid-template-columns:1fr 1fr;gap:10px;z-index:45}
.sticky .btn{height:44px;justify-content:center}
.fact{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.fact div{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius-sm);padding:9px 11px}
.fact small{display:block;color:var(--muted);font-size:var(--text-xs)}
/* Premium / locked pattern — same as paid extended test results (testresultaten.css + mockup/testresultaten) */
:root{--gold-light:#e4c65a;--gold-deep:#a6851c;--gold-ink:#5c4a0f}
.ptag{margin-inline-start:auto;display:inline-grid;place-items:center;color:var(--gold-ink);background:var(--gold-soft);border:1px solid #e9dcae;border-radius:50%;width:20px;height:20px;flex:none}
.ptag .i{width:11px;height:11px}
.it.on .ptag{background:var(--gold-soft)}
.lk{display:inline-flex;align-items:center;gap:4px;font-size:var(--text-xs);font-weight:600;color:var(--gold-ink);background:var(--gold-soft);border:1px solid #e9dcae;border-radius:var(--radius-pill);padding:1px 8px;white-space:nowrap}
.lk .i{width:12px;height:12px}
.free{display:inline-flex;align-items:center;gap:4px;font-size:var(--text-xs);font-weight:600;color:var(--success);background:var(--success-soft);border-radius:var(--radius-pill);padding:1px 8px;white-space:nowrap}
.free .i{width:12px;height:12px}
.lc{position:relative;overflow:hidden}
.bl{filter:blur(3.2px);opacity:.9;user-select:none;pointer-events:none}
.shine{position:absolute;inset:auto 0 0 0;height:46px;background:linear-gradient(transparent,var(--surface))}
.stamp{position:absolute;inset-inline-start:50%;top:58%;transform:translate(-50%,-50%) rotate(-12deg);font-size:var(--text-xs);font-weight:600;letter-spacing:.12em;text-transform:uppercase;color:color-mix(in srgb,var(--warn) 55%,transparent);border:1.5px dashed color-mix(in srgb,var(--warn) 40%,transparent);border-radius:6px;padding:2px 10px;background:color-mix(in srgb,var(--warn-soft) 75%,transparent);white-space:nowrap;z-index:2}
.lockov{position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px;text-align:center;padding:16px;z-index:3}
.lockov .lci{width:40px;height:40px;border-radius:50%;background:var(--gold-soft);border:1px solid #e9dcae;color:var(--gold-ink);display:grid;place-items:center}
.lockov.pn{inset:auto 50% 14px auto;transform:translateX(50%);background:var(--surface);border:1px solid var(--border);border-radius:var(--radius-sm);box-shadow:var(--shadow-lg);padding:10px 14px;flex-direction:row;gap:10px;text-align:start;width:max-content;max-width:92%}
.lockov.pn .lci{width:32px;height:32px;flex:none}.lockov.pn span{font-size:var(--text-xs)}
.lockov b{font-size:var(--text-md)}.lockov span{color:var(--muted);font-size:var(--text-sm);max-width:340px}
.prem{border-radius:14px;background:var(--gold-soft);border:2px solid var(--gold);box-shadow:0 10px 28px color-mix(in srgb,var(--gold-deep) 18%,transparent);padding:18px 22px;display:grid;grid-template-columns:minmax(0,1.15fr) minmax(0,1fr);gap:22px;align-items:center;position:relative;overflow:hidden}
.prem::before{content:"";position:absolute;inset:0 0 auto 0;height:5px;background:linear-gradient(90deg,var(--gold-light),var(--gold),var(--gold-deep))}
.prem .tag{display:inline-flex;align-items:center;gap:6px;background:var(--gold-ink);color:var(--surface);font-size:var(--text-xs);font-weight:600;border-radius:var(--radius-pill);padding:2px 10px}
.prem .tag .i{width:13px;height:13px;fill:var(--gold-light);stroke:var(--gold-light)}
.prem h3{font-size:var(--text-xl);font-weight:700;color:var(--brand);margin-top:8px;line-height:1.25}
.prem p.l{margin-top:4px}
.prem ul{margin-top:8px;display:grid;grid-template-columns:1fr 1fr;gap:5px 16px}.prem li{list-style:none;display:flex;gap:7px;align-items:flex-start}.prem li .i{color:var(--success);margin-top:2px}
.cta{display:flex;flex-direction:column;gap:9px}
.btn-gold{display:flex;align-items:center;justify-content:space-between;gap:12px;min-height:58px;padding:8px 16px;border-radius:10px;background:linear-gradient(135deg,var(--gold-light) 0%,var(--gold) 100%);color:var(--brand);border:1px solid var(--gold-deep);box-shadow:0 2px 0 var(--gold-deep),0 8px 18px color-mix(in srgb,var(--gold-deep) 28%,transparent);font-weight:700;font-size:var(--text-md)}
.btn-gold .lab{white-space:nowrap;display:flex;align-items:center;gap:8px}.btn-gold .lab .i{width:19px;height:19px}
.btn-gold .meta{display:flex;flex-direction:column;align-items:flex-end;line-height:1.2}
.btn-gold .price{font-size:var(--text-lg);font-weight:700;background:var(--brand);color:var(--surface);border-radius:8px;padding:2px 10px;white-space:nowrap}
.btn-gold .meta small{white-space:nowrap;font-size:var(--text-xs);font-weight:600;opacity:.85}
.cta .fine{font-size:var(--text-xs);color:var(--gold-ink);font-weight:600;text-align:center;display:flex;align-items:center;justify-content:center;gap:5px}
.cta .fine .i{width:14px;height:14px}
.kpi.lc .v{filter:blur(5px);user-select:none}
'''
CSS = BASE_CSS + EXTRA

COMPANY = "Voorbeeld Tuinbouwgroep BV"
ROLES = {
    "bm": dict(name="Marieke de Vries", ini="MV", label="Bedrijfsmanager", scope=("building", "Alle vestigingen", "14"), switch=True, ro=False, tokens="412"),
    "rm": dict(name="Joost Verbeek", ini="JV", label="Regiomanager", scope=("map", "Regio Westland", "5 vestigingen"), switch=True, ro=True, tokens=None),
    "vm": dict(name="Sanne Bakker", ini="SB", label="Vestigingsmanager", scope=("building", "Vestiging Naaldwijk", ""), switch=False, ro=False, tokens="38"),
}

def nav(role):
    if role == "rm":
        return [
            ("Overzicht", [("dash", "grid", "Dashboard", ""), ("todo", "bell", "Signalen", "4")]),
            ("Werving", [("vac", "brief", "Vacatures", ""), ("sol", "inbox", "Sollicitaties", "9"), ("talent", "users", "Talentpool", ""), ("ins", "bar", "Kandidaatinzichten", "")]),
            ("Mijn regio", [("vest", "building", "Vestigingen", "5"), ("tok", "coins", "Tokenverbruik", "")]),
        ]
    if role == "vm":
        return [
            ("Overzicht", [("dash", "grid", "Dashboard", ""), ("todo", "check", "Te doen", "3")]),
            ("Werving", [("vac", "brief", "Vacatures", ""), ("sol", "inbox", "Sollicitaties", "7"), ("talent", "users", "Talentpool", ""), ("ins", "bar", "Kandidaatinzichten", "")]),
            ("Mijn vestiging", [("prof", "building", "Vestigingsprofiel", ""), ("mat", "dl", "Wervingsmateriaal", "")]),
            ("Tokens & facturen", [("tok", "coins", "Saldo & kopen", ""), ("fac", "receipt", "Facturen", "")]),
        ]
    return [
        ("Overzicht", [("dash", "grid", "Dashboard", ""), ("todo", "check", "Te doen", "7")]),
        ("Werving", [("vac", "brief", "Vacatures", ""), ("sol", "inbox", "Sollicitaties", "23"), ("talent", "users", "Talentpool", ""), ("ins", "bar", "Kandidaatinzichten", "")]),
        ("Organisatie", [("vest", "building", "Vestigingen & regio’s", ""), ("team", "shield2", "Team & rechten", ""), ("prof", "tag", "Bedrijfsprofiel", ""), ("sal", "euro", "Salaristabellen", "")]),
        ("Tokens & facturen", [("tok", "coins", "Saldo & kopen", ""), ("use", "list", "Verbruik per vestiging", ""), ("fac", "receipt", "Facturen", "")]),
        ("Meer", [("api", "plug", "Koppelingen", ""), ("over", "handshake", "Overnames", "1"), ("mat", "dl", "Wervingsmateriaal", ""), ("partner", "gift", "Partnerprogramma", "")]),
    ]

INSIGHTS_LOCKED = True  # tag disappears once the company has full insights
PTAG = f'<span class="ptag" title="Premium">{ic("lock")}</span>'

def sidebar(role, active, collapsed=()):
    out = []
    for g, items in nav(role):
        keys = [k for k, *_ in items]
        if g in collapsed and active not in keys:
            badge = sum(int(c) for *_, c in items if c)
            b = f'<span class="cnt" style="margin-inline-start:auto;margin-inline-end:6px">{badge}</span>' if badge else ""
            out.append(f'<div class="grp col"><h4><span>{g}</span>{b}{ic("chevr")}</h4></div>')
            continue
        li = "".join(f'<a class="it{" on" if k==active else ""}">{ic(i)}<span>{t}</span>{f"<span class=cnt>{c}</span>" if c else ""}{PTAG if (k=="ins" and INSIGHTS_LOCKED) else ""}</a>' for k, i, t, c in items)
        out.append(f'<div class="grp"><h4>{g}</h4>{li}</div>')
    foot = f'<a class="it">{ic("map")}<span>Banenkaart bekijken</span>{ic("ext")}</a>'
    if role == "rm":
        foot += '<p>Je kijkt mee als regiomanager. Wijzigen doen de vestigings- en bedrijfsmanagers.</p>'
    return '<nav class="side">' + "".join(out) + f'<div class="sidefoot">{foot}</div></nav>'

def topbar(role):
    r = ROLES[role]
    icn, lab, sub = r["scope"]
    sub_html = f'<small>{sub}</small>' if sub else ""
    chev = ic("chevd") if r["switch"] else ""
    scope = f'<span class="scope{"" if r["switch"] else " fixed"}">{ic(icn)}<b>{lab}</b>{sub_html}{chev}</span>'
    ro = f'<span class="ro">{ic("eye")}Alleen lezen</span>' if r["ro"] else ""
    tok = f'<span class="tok">{ic("coins")}<b>{r["tokens"]}</b><small>tokens</small></span>' if r["tokens"] else ""
    return f'''<header class="top"><div class="brandm"><img src="{LOGO}" alt=""><b>Lobsy</b><span>Werkgever</span></div>
{scope}{ro}
<div class="gs bm">{ic("search")}<span>Zoek vacature, kandidaat of vestiging…</span><span class="kbd">Ctrl K</span></div>
<div class="tr">{tok}<span class="tb">{ic("help")}</span><span class="tb">{ic("bell")}<span class="dot"></span></span>
<div class="me"><span class="av">{r["ini"]}</span><div><b>{r["name"]}</b><small>{r["label"]}</small></div>{ic("chevd")}</div></div></header>'''

def crumb(parts):
    s = f' {ic("chevr")} '.join(parts[:-1] + [f"<b>{parts[-1]}</b>"])
    return f'<div class="crumb">{s}</div>'

def page(role, active, crumbs, body, extra="", collapsed=(), demo=""):
    return f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><style>{CSS}</style></head><body>
{topbar(role)}{sidebar(role, active, collapsed)}<main class="main">{crumb(crumbs)}{body}</main>{extra}<span class="demo {demo}">Voorbeelddata</span></body></html>'''

def rohint(text):
    return f'<div class="rohint">{ic("eye")}<b>Alleen lezen</b><span>{text}</span></div>'

BNAV = [("dash", "home", "Overzicht", ""), ("vac", "brief", "Vacatures", ""), ("sol", "inbox", "Sollicitaties", "23"), ("tok", "coins", "Tokens", ""), ("more", "menu", "Meer", "")]

def mpage(body, extra="", top=None, active="dash", bnav=True):
    top = top or f'''<header class="mtop" style="padding-inline-start:12px"><img src="{LOGO}" alt=""><span class="scope">{ic("building")}<b>Alle vestigingen</b>{ic("chevd")}</span>
<span style="margin-inline-start:auto" class="tb">{ic("bell")}<span class="dot"></span></span><span class="av">MV</span></header>'''
    nav_html = ('<nav class="bnav">' + "".join(f'<a class="{"on" if k==active else ""}">{ic(i)}<span>{t}</span>{f"<span class=cnt>{c}</span>" if c else ""}</a>' for k, i, t, c in BNAV) + '</nav>') if bnav else ""
    return f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><style>{CSS}</style></head><body class="m">
{top}{body}{nav_html}{extra}<span class="demo" style="bottom:{76 if bnav else 14}px">Voorbeelddata</span></body></html>'''
