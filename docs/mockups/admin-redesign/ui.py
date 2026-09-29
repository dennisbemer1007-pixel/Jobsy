"""Shared parts for the Lobsy Beheer mockups: icons, IA (sidebar), CSS tokens, shell helpers.
Tokens are copied 1:1 from Jobsy.Web/wwwroot/css/app.css :root + the design-system.mdc scale. No new colours."""
import pathlib, base64
d = pathlib.Path(__file__).parent
LOGO = "data:image/png;base64," + base64.b64encode((d / "src/lobsy-128.png").read_bytes()).decode()

P = dict(
 grid='<rect x="4" y="4" width="7" height="7" rx="1.5"/><rect x="13" y="4" width="7" height="7" rx="1.5"/><rect x="4" y="13" width="7" height="7" rx="1.5"/><rect x="13" y="13" width="7" height="7" rx="1.5"/>',
 inbox='<path d="M4 13h4l1.5 3h5L16 13h4"/><path d="M5.5 6h13L20 13v5a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-5z"/>',
 users='<circle cx="9" cy="8" r="3.5"/><path d="M2.5 20a6.5 6.5 0 0 1 13 0"/><path d="M16 4.5a3.5 3.5 0 0 1 0 7M18 14a6 6 0 0 1 3.5 6"/>',
 user='<circle cx="12" cy="8" r="4"/><path d="M4 21v-1a6 6 0 0 1 6-6h4a6 6 0 0 1 6 6v1"/>',
 key='<circle cx="8" cy="15" r="4"/><path d="m11 12 9-9M17 6l3 3M15 8l2 2"/>',
 handshake='<path d="M3 11l4-4 4 2 3-2 7 5"/><path d="M7 7 3 11l7 7 2-2M14 7l4 4-5 5"/>',
 building='<rect x="4" y="3" width="11" height="18" rx="1"/><path d="M15 9h4a1 1 0 0 1 1 1v11H15M8 7h3M8 11h3M8 15h3"/>',
 map='<path d="M9 4 3 6v14l6-2 6 2 6-2V4l-6 2z"/><path d="M9 4v14M15 6v14"/>',
 flag='<path d="M5 21V4M5 4h11l-2 4 2 4H5"/>',
 test='<path d="M9 3h6M10 3v6L5 18a2 2 0 0 0 1.8 3h10.4A2 2 0 0 0 19 18l-5-9V3"/><path d="M7.5 14h9"/>',
 brief='<rect x="3" y="7" width="18" height="13" rx="2"/><path d="M9 7V5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2M3 13h18"/>',
 dl='<path d="M12 4v11M7 10l5 5 5-5M5 20h14"/>',
 shield2='<path d="M12 3 4 6v6c0 5 3.5 8 8 9 4.5-1 8-4 8-9V6z"/><path d="m9 12 2 2 4-4"/>',
 euro='<path d="M17 6.5A6.5 6.5 0 1 0 17 17.5"/><path d="M4 10.5h9M4 13.5h9"/>',
 tag='<path d="M3 12V4h8l10 10-8 8z"/><circle cx="7.5" cy="8" r="1.5"/>',
 gift='<rect x="3" y="8" width="18" height="4" rx="1"/><path d="M5 12v9h14v-9M12 8v13M12 8S10 3 7.5 4 8 8 12 8zM12 8s2-5 4.5-4S16 8 12 8z"/>',
 send='<path d="M21 3 3 10l7 3 3 7z"/><path d="m10 13 4-4"/>',
 receipt='<path d="M6 3h12v18l-3-2-3 2-3-2-3 2z"/><path d="M9 8h6M9 12h6"/>',
 book='<path d="M5 4h11a3 3 0 0 1 3 3v13H8a3 3 0 0 1-3-3z"/><path d="M5 17a3 3 0 0 1 3-3h11"/>',
 grad='<path d="M2 9 12 4l10 5-10 5z"/><path d="M6 11v5c3 2 9 2 12 0v-5"/>',
 db='<ellipse cx="12" cy="6" rx="7" ry="3"/><path d="M5 6v12c0 1.7 3 3 7 3s7-1.3 7-3V6M5 12c0 1.7 3 3 7 3s7-1.3 7-3"/>',
 mail='<rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 7 9 6 9-6"/>',
 toggle='<rect x="2" y="7" width="20" height="10" rx="5"/><circle cx="16" cy="12" r="3"/>',
 sliders='<path d="M4 6h10M18 6h2M4 12h4M12 12h8M4 18h12"/><circle cx="16" cy="6" r="2"/><circle cx="10" cy="12" r="2"/><circle cx="18" cy="18" r="2"/>',
 plug='<path d="M9 3v5M15 3v5M6 8h12v3a6 6 0 0 1-12 0zM12 17v4"/>',
 list='<path d="M8 6h12M8 12h12M8 18h12M4 6h.01M4 12h.01M4 18h.01"/>',
 eye='<path d="M2.5 12S6 5 12 5s9.5 7 9.5 7-3.5 7-9.5 7-9.5-7-9.5-7z"/><circle cx="12" cy="12" r="3"/>',
 eyeoff='<path d="M3 3l18 18M10.6 5.1A9.8 9.8 0 0 1 12 5c5 0 8.5 4.5 9.5 7-.4 1-1.2 2.3-2.4 3.6M6.4 6.4C4.5 7.8 3.2 9.8 2.5 12c1 2.5 4.5 7 9.5 7 1.6 0 3-.4 4.3-1.1"/>',
 lock='<rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/>',
 phone='<rect x="7" y="2.5" width="10" height="19" rx="2"/><path d="M11 18.5h2"/>',
 terminal='<rect x="3" y="4" width="18" height="16" rx="2"/><path d="m7 9 3 3-3 3M13 15h4"/>',
 search='<circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/>',
 bell='<path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9"/><path d="M10 21a2 2 0 0 0 4 0"/>',
 help='<circle cx="12" cy="12" r="9"/><path d="M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.7.3-1 1-1 1.7M12 17h.01"/>',
 chevd='<path d="m6 9 6 6 6-6"/>', chevr='<path d="m9 6 6 6-6 6"/>', chevl='<path d="m15 6-6 6 6 6"/>',
 check='<path d="m5 12 5 5 9-10"/>', x='<path d="M6 6l12 12M18 6 6 18"/>', plus='<path d="M12 5v14M5 12h14"/>',
 more='<circle cx="5" cy="12" r="1.3"/><circle cx="12" cy="12" r="1.3"/><circle cx="19" cy="12" r="1.3"/>',
 filter='<path d="M4 5h16l-6 8v5l-4 2v-7z"/>', cols='<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M9 4v16M15 4v16"/>',
 alert='<path d="M12 3 2 20h20z"/><path d="M12 10v4M12 17h.01"/>', info='<circle cx="12" cy="12" r="9"/><path d="M12 11v5M12 8h.01"/>',
 clock='<circle cx="12" cy="12" r="8"/><path d="M12 8v4l3 2"/>', refresh='<path d="M20 11a8 8 0 0 0-14.5-4.5L4 8M4 4v4h4M4 13a8 8 0 0 0 14.5 4.5L20 16M20 20v-4h-4"/>',
 logout='<path d="M15 4h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-3M10 17l-5-5 5-5M5 12h11"/>', menu='<path d="M4 7h16M4 12h16M4 17h16"/>',
 ext='<path d="M14 4h6v6M20 4l-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5"/>',
 ban='<circle cx="12" cy="12" r="9"/><path d="m5.6 5.6 12.8 12.8"/>',
 coins='<ellipse cx="9" cy="7" rx="6" ry="3"/><path d="M3 7v5c0 1.7 2.7 3 6 3s6-1.3 6-3V7M9 15v2c0 1.7 2.7 3 6 3s6-1.3 6-3v-5c0-1.7-2.7-3-6-3"/>',
)
def ic(n, c="i"): return f'<svg class="{c}" viewBox="0 0 24 24" aria-hidden="true">{P[n]}</svg>'

NAV = [
 ("Overzicht", [("dash", "grid", "Dashboard", ""), ("todo", "inbox", "Te doen", "7"), ("feedback", "send", "Feedback", "")]),
 ("Gebruikers & rollen", [("users", "users", "Alle gebruikers", ""), ("roles", "key", "Rollen & rechten", ""), ("partners", "handshake", "Sales & ambassadeurs", "")]),
 ("Organisaties", [("orgs", "building", "Bedrijven & vestigingen", ""), ("regions", "map", "Regio’s & domeinen", ""), ("reg", "flag", "Aanvragen", "3")]),
 ("Kandidaten & tests", [("cands", "user", "Kandidaten", ""), ("tests", "test", "Tests & normen", "")]),
 ("Vacatures & matching", [("vac", "brief", "Vacatures", ""), ("ats", "dl", "ATS-import", ""), ("mod", "shield2", "Moderatie", "2"), ("cat", "tag", "Categorieën & salaris", "")]),
 ("Financiën", [("fin", "euro", "Omzet & transacties", ""), ("prices", "coins", "Prijzen & pakketten", ""), ("goodwill", "gift", "Goodwill & tokens", ""), ("payout", "receipt", "Uitbetalingen & btw", "")]),
 ("Content & opleidingen", [("pages", "book", "Pagina’s & flyer", ""), ("training", "grad", "Opleidingen", ""), ("master", "db", "Stamgegevens", ""), ("mail", "mail", "E-mails & meldingen", "")]),
 ("Platforminstellingen", [("features", "toggle", "Functies", ""), ("general", "sliders", "Algemeen", ""), ("integr", "plug", "Integraties & API", "")]),
 ("Beveiliging & audit", [("audit", "list", "Auditlog", ""), ("pii", "eye", "Gegevensinzage", ""), ("sec", "lock", "2FA & sessies", ""), ("gdpr", "shield2", "Privacy & AVG", "1"), ("logs", "terminal", "Systeemlogs", "")]),
]

from ui_css import CSS

def spark(vals):
    mx, mn = max(vals), min(vals); n = len(vals) - 1
    pts = " ".join(f"{i*64/n:.1f},{20-(v-mn)/(mx-mn or 1)*18:.1f}" for i, v in enumerate(vals))
    return f'<svg class="spark" viewBox="0 0 64 22"><polyline points="{pts}"/></svg>'

def sidebar(active, open_groups):
    out = []
    for g, items in NAV:
        keys = [k for k, *_ in items]
        if g in open_groups or active in keys:
            li = "".join(f'<a class="it{" on" if k==active else ""}">{ic(i)}<span>{t}</span>{f"<span class=cnt>{c}</span>" if c else ""}</a>' for k, i, t, c in items)
            out.append(f'<div class="grp"><h4>{g}</h4>{li}</div>')
        else:
            badge = sum(int(c) for *_, c in items if c)
            b = f'<span class="cnt" style="margin-inline-start:auto;margin-inline-end:6px">{badge}</span>' if badge else ""
            out.append(f'<div class="grp col"><h4><span>{g}</span>{b}{ic("chevr")}</h4></div>')
    return '<nav class="side">' + "".join(out) + '</nav>'

def topbar(env="Acceptatie"):
    cls = "env prod" if env == "Productie" else "env"
    return f'''<header class="top"><div class="brandm"><img src="{LOGO}" alt=""><b>Lobsy</b><span>Beheer</span></div>
<span class="{cls}"><i></i>{env}</span>
<div class="gs">{ic("search")}<span>Zoek gebruiker, bedrijf, vacature, factuur of correlatie-id…</span><span class="kbd">Ctrl K</span></div>
<div class="tr"><span class="tb">{ic("help")}</span><span class="tb">{ic("bell")}<span class="dot"></span></span>
<div class="me"><span class="av">DB</span><div><b>Dennis B.</b><small>Beheerder · 2FA actief</small></div>{ic("chevd")}</div></div></header>'''

def crumb(parts):
    s = f' {ic("chevr")} '.join(parts[:-1] + [f"<b>{parts[-1]}</b>"])
    return f'<div class="crumb">{s}</div>'

def page(active, open_groups, crumbs, body, extra="", env="Acceptatie", demo=""):
    return f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><style>{CSS}</style></head><body>
{topbar(env)}{sidebar(active, open_groups)}<main class="main">{crumb(crumbs)}{body}</main>{extra}<span class="demo {demo}">Voorbeelddata</span></body></html>'''

def mpage(body, extra=""):
    return f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><style>{CSS}</style></head><body class="m">
<header class="mtop"><span class="tb">{ic("menu")}</span><img src="{LOGO}" alt=""><b>Beheer</b><span class="env" style="margin-inline-start:4px"><i></i>Acceptatie</span>
<span style="margin-inline-start:auto" class="tb">{ic("search")}</span><span class="av">DB</span></header>{body}{extra}</body></html>'''

def cb(on=False): return f'<span class="cb{" on" if on else ""}">{ic("check") if on else ""}</span>'
