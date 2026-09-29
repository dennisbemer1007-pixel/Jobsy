"""Lobsy Partnerportaal (Salesmanager) mockups. Desktop 1440x900 + mobile 390x844 @2x.
Same visual language as admin-redesign / werkgever-redesign / scholen: tokens, icons and base CSS from ./base."""
import pathlib, sys
HERE = pathlib.Path(__file__).resolve().parent
sys.path[:0] = [str(HERE / "base")]
from ui import P, ic, LOGO, cb          # noqa: E402
from bmui import CSS as BM_CSS          # noqa: E402
try:
    import segno
except ImportError:  # deterministic fallback pattern
    segno = None

P.setdefault("link", '<path d="M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1"/><path d="M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1"/>')
P.setdefault("copy", '<rect x="8" y="8" width="12" height="12" rx="2"/><path d="M16 8V5a1 1 0 0 0-1-1H5a1 1 0 0 0-1 1v10a1 1 0 0 0 1 1h3"/>')
P.setdefault("qr", '<rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><path d="M14 14h3v3h-3zM20 14v.01M14 20h.01M17 20h4v-3"/>')
P.setdefault("wallet", '<path d="M4 7h14a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a1 1 0 0 1-1-1V6a2 2 0 0 1 2-2h11"/><path d="M16 13h.01"/>')
P.setdefault("file", '<path d="M14 3H6a1 1 0 0 0-1 1v16a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V8z"/><path d="M14 3v5h5M9 13h6M9 17h6"/>')
P.setdefault("mic", '<rect x="9" y="3" width="6" height="11" rx="3"/><path d="M5 11a7 7 0 0 0 14 0M12 18v3"/>')
P.setdefault("trend", '<path d="M3 17l6-6 4 4 8-8"/><path d="M14 7h7v7"/>')
P.setdefault("msg", '<path d="M21 12a8 8 0 0 1-11.6 7.1L4 20l1.1-4.6A8 8 0 1 1 21 12z"/>')
P.setdefault("card", '<rect x="3" y="6" width="18" height="12" rx="2"/><path d="M7 10h5M7 14h3"/>')
P.setdefault("pres", '<rect x="3" y="4" width="18" height="12" rx="1"/><path d="M12 16v4M8 20h8"/>')
P.setdefault("settings", '<circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z"/>')
P.setdefault("pen", '<path d="M4 20h4L19 9l-4-4L4 16z"/>')
P.setdefault("home", '<path d="M3 11l9-7 9 7"/><path d="M5 10v10h14V10"/>')
P.setdefault("share", '<circle cx="6" cy="12" r="2.5"/><circle cx="18" cy="6" r="2.5"/><circle cx="18" cy="18" r="2.5"/><path d="M8.2 10.8l7.6-3.6M8.2 13.2l7.6 3.6"/>')

EXTRA = r'''
.checks li{list-style:none;display:flex;gap:8px;align-items:flex-start;padding:3px 16px;font-size:var(--text-sm)}.checks li .i{margin-top:2px;color:var(--success)}
.rank li{list-style:none;display:grid;grid-template-columns:22px 1fr auto;gap:10px;align-items:center;padding:7px 16px;border-top:1px solid color-mix(in srgb,var(--border) 60%,transparent)}
.rank li:first-child{border-top:0}.rank li span.k{width:22px;height:22px;border-radius:50%;background:var(--accent-soft);color:var(--brand);font-size:var(--text-xs);font-weight:600;display:grid;place-items:center}
.rank li small{display:block;color:var(--muted);font-size:var(--text-xs)}.rank .amt2{font-variant-numeric:tabular-nums;white-space:nowrap}
.demo.l{inset-inline-start:264px}
.wchip{display:inline-flex;align-items:center;gap:8px;height:34px;padding:0 12px;border-radius:var(--radius-pill);background:color-mix(in srgb,var(--surface) 12%,transparent);color:var(--surface);white-space:nowrap;font-size:var(--text-xs)}
.wchip b{font-size:var(--text-sm);font-variant-numeric:tabular-nums}.wchip .i{color:color-mix(in srgb,var(--surface) 80%,transparent)}
.rolec2{display:inline-flex;align-items:center;gap:8px;height:36px;padding:0 10px;border-radius:var(--radius-sm);background:color-mix(in srgb,var(--surface) 12%,transparent);color:var(--surface);white-space:nowrap}
.rolec2 small{font-size:var(--text-xs);color:color-mix(in srgb,var(--surface) 70%,transparent)}
.code{font-family:ui-monospace,Menlo,monospace;font-weight:600;letter-spacing:.06em}
.hero{display:grid;grid-template-columns:1.25fr 1fr 1fr 1fr;gap:var(--space-3);margin-bottom:var(--space-4)}
.hero .main1{background:var(--brand);color:var(--surface);border-color:var(--brand);position:relative;overflow:hidden}
.hero .main1::after{content:"";position:absolute;inset:auto -40px -60px auto;width:200px;height:200px;border-radius:50%;background:color-mix(in srgb,var(--surface) 7%,transparent)}
.hero .main1 small{color:color-mix(in srgb,var(--surface) 78%,transparent)}.hero .main1 .dl{color:color-mix(in srgb,var(--surface) 80%,transparent)}
.hero .main1 .v{font-size:var(--text-2xl)}
.hero .main1 .btn{background:var(--surface);color:var(--brand);border-color:var(--surface);margin-top:10px;position:relative;z-index:1}
.chart{display:grid;grid-template-columns:repeat(12,1fr);gap:8px;align-items:end;height:150px;padding:8px 16px 0}
.chart div{display:flex;flex-direction:column;align-items:center;gap:4px;height:100%;justify-content:flex-end}
.chart i{display:block;width:100%;max-width:30px;border-radius:5px 5px 2px 2px;background:color-mix(in srgb,var(--brand) 22%,var(--surface))}
.chart i.now{background:var(--brand)}.chart i.pend{background:repeating-linear-gradient(135deg,color-mix(in srgb,var(--brand) 35%,var(--surface)) 0 4px,color-mix(in srgb,var(--brand) 15%,var(--surface)) 4px 8px)}
.chart span{font-size:var(--text-xs);color:var(--muted)}
.leg2{display:flex;gap:14px;font-size:var(--text-xs);color:var(--muted);padding:8px 16px 14px}.leg2 span{display:inline-flex;align-items:center;gap:6px}.leg2 i{width:10px;height:10px;border-radius:3px;display:inline-block}
.funnel div b{font-variant-numeric:tabular-nums;text-align:end}
.fstep{display:grid;grid-template-columns:repeat(4,1fr);gap:0;padding:4px 16px 14px}
.fstep div{position:relative;padding:10px 12px;background:var(--pearl);border-radius:var(--radius-sm);margin-inline-end:14px}
.fstep div:not(:last-child)::after{content:"";position:absolute;inset-inline-end:-12px;top:50%;width:10px;height:10px;border-top:2px solid var(--border);border-inline-end:2px solid var(--border);transform:translateY(-50%) rotate(45deg)}
.fstep div:last-child{margin-inline-end:0}
.fstep small{display:block;color:var(--muted);font-size:var(--text-xs)}.fstep b{display:block;font-size:var(--text-xl);font-variant-numeric:tabular-nums;margin-top:2px}
.fstep em{font-style:normal;font-size:var(--text-xs);color:var(--success);font-weight:600}
.staf{display:grid;grid-template-columns:repeat(3,1fr);gap:8px;padding:4px 16px 12px}
.staf div{border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 12px}
.staf div.on{border-color:var(--brand);background:color-mix(in srgb,var(--accent-soft) 60%,var(--surface))}
.staf small{display:block;color:var(--muted);font-size:var(--text-xs)}.staf b{display:block;font-size:var(--text-xl);color:var(--brand)}
.newtag{display:inline-flex;align-items:center;height:18px;padding:0 6px;border-radius:var(--radius-pill);font-size:var(--text-xs);font-weight:600;background:var(--accent-soft);color:var(--brand)}
.linkbox{display:flex;align-items:center;gap:10px;border:1px solid var(--border);border-radius:var(--radius-sm);padding:0 6px 0 12px;height:48px;background:var(--pearl)}
.linkbox .u{flex:1;font-family:ui-monospace,Menlo,monospace;font-size:var(--text-md);font-weight:600;color:var(--brand);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.qrw{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius);padding:14px;display:grid;place-items:center;gap:6px}
.qrw svg{display:block}
.share{display:flex;gap:8px;flex-wrap:wrap}
.mats{display:grid;grid-template-columns:repeat(3,1fr);gap:var(--space-3)}
.mat{padding:14px 16px;display:flex;flex-direction:column;gap:6px}
.mat .ico2{width:36px;height:36px;border-radius:10px;display:grid;place-items:center;background:var(--accent-soft);color:var(--brand)}
.mat b{font-size:var(--text-md)}.mat p{color:var(--muted);font-size:var(--text-xs);flex:1}
.mat .ft{display:flex;align-items:center;gap:8px;margin-top:4px}.mat .ft .sp{flex:1}
.pitch li{list-style:none;display:grid;grid-template-columns:26px 1fr;gap:10px;padding:8px 16px}
.pitch li i{width:26px;height:26px;border-radius:50%;background:var(--brand);color:var(--surface);font-style:normal;font-weight:600;font-size:var(--text-xs);display:grid;place-items:center}
.pitch li b{display:block}.pitch li span{color:var(--muted);font-size:var(--text-sm)}
.quote{margin:4px 16px 14px;padding:10px 12px;border-inline-start:3px solid var(--brand);background:var(--pearl);border-radius:0 var(--radius-sm) var(--radius-sm) 0;font-size:var(--text-sm)}
.infobox{display:flex;gap:10px;align-items:flex-start;background:var(--accent-soft);border-radius:var(--radius-sm);padding:10px 12px;font-size:var(--text-xs);color:var(--text)}.infobox .i{color:var(--brand);margin-top:1px}
.okbox{display:flex;gap:10px;align-items:flex-start;background:var(--success-soft);border-radius:var(--radius-sm);padding:10px 12px;font-size:var(--text-xs);color:var(--text)}.okbox .i{color:var(--success);margin-top:1px}
.privnote{display:flex;gap:8px;align-items:flex-start;font-size:var(--text-xs);color:var(--muted);padding:6px 10px 0;line-height:1.4}.privnote .i{width:14px;height:14px;color:var(--success);margin-top:1px}
.sidefoot .it{color:var(--text)}
.tl li{list-style:none;display:grid;grid-template-columns:22px 1fr auto;gap:10px;position:relative;padding:0 0 12px}
.tl li::before{content:"";position:absolute;inset-inline-start:10px;top:20px;bottom:0;width:2px;background:var(--border)}.tl li:last-child::before{display:none}
.tl li i{width:22px;height:22px;border-radius:50%;background:var(--success-soft);color:var(--success);display:grid;place-items:center;position:relative}
.tl li i .i{width:12px;height:12px;stroke-width:2.6}.tl li.todo i{background:var(--pearl-mid);color:var(--muted)}
.tl li small{display:block;color:var(--muted);font-size:var(--text-xs)}.tl li .d{color:var(--muted);font-size:var(--text-xs);white-space:nowrap}
.yr{display:grid;grid-template-columns:repeat(3,1fr);gap:4px;margin-top:6px}.yr div{height:8px;border-radius:4px;background:var(--pearl-mid);position:relative;overflow:hidden}
.yr div i{position:absolute;inset:0 auto 0 0;background:var(--brand);border-radius:4px}
.yrl{display:grid;grid-template-columns:repeat(3,1fr);gap:4px;font-size:var(--text-xs);color:var(--muted);margin-top:4px}
.steps3{display:flex;align-items:center;gap:6px;font-size:var(--text-xs)}
.steps3 span{display:inline-flex;align-items:center;gap:5px;color:var(--muted)}.steps3 span.on{color:var(--success);font-weight:600}.steps3 span.cur{color:var(--brand);font-weight:600}
.steps3 em{width:18px;height:1.5px;background:var(--border);display:inline-block}
.steps3 .i{width:13px;height:13px}
.amt{display:flex;align-items:center;height:48px;border:1px solid var(--brand);border-radius:var(--radius-sm);padding:0 12px;gap:8px;font-size:var(--text-xl);font-weight:600;font-variant-numeric:tabular-nums;box-shadow:0 0 0 3px var(--accent-soft)}
.amt small{margin-inline-start:auto;font-size:var(--text-xs);font-weight:600;color:var(--brand)}
.sumt{width:100%}.sumt td{height:30px;border:0;padding:0}.sumt td:last-child{text-align:end;font-variant-numeric:tabular-nums}
.sumt tr.tot td{border-top:1px solid var(--border);font-weight:600;font-size:var(--text-md);height:38px}
.invprev{border:1px solid var(--border);border-radius:var(--radius-sm);padding:12px 14px;background:var(--surface);font-size:var(--text-xs)}
.invprev .h{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:8px}.invprev .h b{font-size:var(--text-sm)}
.invprev .lg{display:inline-flex;align-items:center;gap:6px;font-weight:600;color:var(--brand)}.invprev .lg img{width:18px;height:18px}
.fgrid{display:grid;grid-template-columns:1fr 1fr;gap:10px 12px}
.fgrid .lbl{margin:0 0 5px}.fgrid .field{width:100%}.fgrid .full{grid-column:1/-1}
.radio{display:grid;grid-template-columns:18px 1fr;gap:2px 10px;border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 12px}
.radio .rd{width:16px;height:16px;border-radius:50%;border:1.5px solid var(--border);margin-top:2px}
.radio.on{border-color:var(--brand);background:color-mix(in srgb,var(--accent-soft) 55%,var(--surface))}.radio.on .rd{border:5px solid var(--brand)}
.radio p{grid-column:2;color:var(--muted);font-size:var(--text-xs)}
.setl{display:grid;grid-template-columns:220px 1fr;gap:var(--space-5);padding:16px;border-top:1px solid color-mix(in srgb,var(--border) 60%,transparent)}
.setl:first-of-type{border-top:0}.setl h3{font-size:var(--text-sm);font-weight:600}.setl .d{color:var(--muted);font-size:var(--text-xs);margin-top:2px}
.kvl{display:grid;grid-template-columns:150px 1fr auto;gap:8px 12px;align-items:center}
.kvl span{color:var(--muted);font-size:var(--text-xs)}
.mhero{background:var(--brand);color:var(--surface);border-radius:var(--radius);padding:14px 16px;position:relative;overflow:hidden}
.mhero::after{content:"";position:absolute;inset:auto -50px -70px auto;width:190px;height:190px;border-radius:50%;background:color-mix(in srgb,var(--surface) 7%,transparent)}
.mhero small{color:color-mix(in srgb,var(--surface) 78%,transparent);font-size:var(--text-xs);font-weight:600}
.mhero .v{font-size:var(--text-2xl);font-weight:700;font-variant-numeric:tabular-nums;margin-top:2px}
.mhero .row2{display:flex;gap:16px;margin-top:8px;font-size:var(--text-xs);color:color-mix(in srgb,var(--surface) 82%,transparent)}
.mhero .row2 b{display:block;color:var(--surface);font-size:var(--text-sm)}
.mhero .btn{margin-top:12px;background:var(--surface);color:var(--brand);border-color:var(--surface);height:44px;width:100%;justify-content:center;position:relative;z-index:1}
.mchart{display:grid;grid-template-columns:repeat(6,1fr);gap:8px;align-items:end;height:92px;padding:10px 14px 0}
.mchart div{display:flex;flex-direction:column;align-items:center;gap:4px;height:100%;justify-content:flex-end}
.mchart i{display:block;width:100%;border-radius:4px 4px 2px 2px;background:color-mix(in srgb,var(--brand) 22%,var(--surface))}.mchart i.now{background:var(--brand)}
.mchart span{font-size:var(--text-xs);color:var(--muted)}
.mshare{display:grid;grid-template-columns:repeat(3,1fr);gap:8px}
.mshare .btn{height:52px;flex-direction:column;gap:3px;justify-content:center;font-size:var(--text-xs)}
.bnav{grid-template-columns:repeat(5,1fr)}
td.num b{font-variant-numeric:tabular-nums}
.st{display:inline-flex;align-items:center;gap:6px;font-size:var(--text-sm)}.st i{width:8px;height:8px;border-radius:50%;background:var(--muted)}
.st.ok i{background:var(--success)}.st.info i{background:var(--brand)}.st.wn i{background:var(--warn)}
'''
CSS = BM_CSS + EXTRA

NAME, INI, CODE = "Tom Hendriks", "TH", "SM-K7Q2MP"
LINK = "lobsy.nl/p/SM-K7Q2MP"

def qr_svg(size=148):
    if segno:
        q = segno.make("https://" + LINK, error="m")
        return q.svg_inline(scale=max(1, size // 33), border=2, dark="#0a2044", light="#fffcfa")
    cells = "".join(f'<rect x="{x}" y="{y}" width="1" height="1"/>' for x in range(25) for y in range(25) if (x * 7 + y * 13 + x * y) % 5 < 2)
    return f'<svg width="{size}" height="{size}" viewBox="0 0 25 25" fill="#0a2044">{cells}</svg>'

# ------------------------------------------------------------------ shell
def nav():
    return [
        ("Overzicht", [("dash", "grid", "Dashboard", "")]),
        ("Verkopen", [("link", "link", "Mijn link & materiaal", ""), ("emp", "building", "Mijn werkgevers", "12"), ("team", "users", "Salesmanager aanbevelen", "")]),
        ("Geld", [("wallet", "wallet", "Wallet & uitbetalingen", "")]),
        ("Account", [("prof", "user", "Profiel & gegevens", ""), ("help", "help", "Hulp & afspraken", "")]),
    ]

def sidebar(active):
    out = []
    for g, items in nav():
        li = "".join(f'<a class="it{" on" if k==active else ""}">{ic(i)}<span>{t}</span>{f"<span class=cnt>{c}</span>" if c else ""}</a>' for k, i, t, c in items)
        out.append(f'<div class="grp"><h4>{g}</h4>{li}</div>')
    foot = f'<p class="privnote">{ic("shield2")}<span>Je ziet bedrijfsnamen en je eigen commissie. Geen contactpersonen, kandidaten of vacaturedetails.</span></p>'
    return '<nav class="side">' + "".join(out) + f'<div class="sidefoot">{foot}</div></nav>'

def topbar():
    return f'''<header class="top"><div class="brandm"><img src="{LOGO}" alt=""><b>Lobsy</b><span>Partner</span></div>
<span class="rolec2">{ic("tag")}<b>Salesmanager</b><small class="code">{CODE}</small></span>
<div class="gs bm">{ic("search")}<span>Zoek een werkgever…</span><span class="kbd">Ctrl K</span></div>
<div class="tr"><span class="wchip">{ic("wallet")}Beschikbaar <b>€ 1.284,50</b></span><span class="tb">{ic("help")}</span><span class="tb">{ic("bell")}<span class="dot"></span></span>
<div class="me"><span class="av">{INI}</span><div><b>{NAME}</b><small>Salesmanager · 2FA aan</small></div>{ic("chevd")}</div></div></header>'''

def crumb(parts):
    return '<div class="crumb">' + f' {ic("chevr")} '.join(parts[:-1] + [f"<b>{parts[-1]}</b>"]) + '</div>'

def page(active, crumbs, body, extra=""):
    return f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><style>{CSS}</style></head><body>
{topbar()}{sidebar(active)}<main class="main">{crumb(crumbs)}{body}</main>{extra}<span class="demo l">Voorbeelddata</span></body></html>'''

BNAV = [("dash", "home", "Overzicht"), ("link", "qr", "Mijn link"), ("emp", "building", "Werkgevers"), ("wallet", "wallet", "Wallet"), ("more", "menu", "Meer")]
def mpage(body, active="dash", extra=""):
    top = f'''<header class="mtop" style="padding-inline-start:12px"><img src="{LOGO}" alt="" style="width:26px;height:26px;border-radius:6px;background:var(--surface);padding:2px"><b style="font-size:var(--text-md)">Lobsy</b>
<span class="wchip" style="margin-inline-start:auto;height:30px">{ic("wallet")}<b>€ 1.284,50</b></span><span class="tb">{ic("bell")}<span class="dot"></span></span><span class="av">{INI}</span></header>'''
    nav_html = '<nav class="bnav">' + "".join(f'<a class="{"on" if k==active else ""}">{ic(i)}<span>{t}</span></a>' for k, i, t in BNAV) + '</nav>'
    return f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><style>{CSS}</style></head><body class="m">
{top}{body}{nav_html}{extra}<span class="demo" style="bottom:76px">Voorbeelddata</span></body></html>'''

def kpi(label, icon, v, sub, cls=""):
    return f'<div class="card kpi {cls}"><small>{ic(icon)}{label}</small><div class="v">{v}</div><div class="dl">{sub}</div></div>'
def bar(p): return f'<span class="bar"><i style="width:{p}%"></i></span>'

MONTHS = ["okt", "nov", "dec", "jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep"]
EARN = [120, 180, 210, 95, 240, 910, 380, 520, 610, 290, 470, 1182]  # jan–sep 2026 = 4.697

# employer rows: name, place, since, status(cls,label), year idx(0-2), yr progress %, purchases, commission this year, last purchase
EMP = [
    ("Kwekerij De Zonnehoek", "Naaldwijk", "12-03-2026", ("ok", "Actief"), 0, 55, 4, "€ 943,75", "18-09-2026"),
    ("Bakkerij Van Leeuwen", "Delft", "02-05-2026", ("ok", "Actief"), 0, 41, 2, "€ 262,50", "03-09-2026"),
    ("Transport Westland BV", "Poeldijk", "14-01-2025", ("ok", "Actief"), 1, 71, 3, "€ 96,00", "11-08-2026"),
    ("Installatiebedrijf Groen", "Monster", "20-06-2026", ("info", "Eerste aankoop"), 0, 28, 1, "€ 43,75", "22-06-2026"),
    ("Hotel Duinzicht", "Noordwijk", "08-11-2024", ("ok", "Actief"), 1, 89, 5, "€ 187,50", "26-09-2026"),
    ("Horeca De Haven", "Scheveningen", "03-09-2026", ("wn", "Nog geen aankoop"), 0, 7, 0, "€ 0,00", "—"),
    ("Schoonmaakbedrijf Helder", "Rijswijk", "22-02-2026", ("wn", "Stil · 94 dagen"), 0, 60, 1, "€ 50,00", "28-06-2026"),
    ("Garage Vermeulen", "'s-Gravenzande", "30-10-2023", ("line", "Jaar 3 · 5%"), 2, 95, 2, "€ 17,50", "12-07-2026"),
]
YR = ["Jaar 1 · 25%", "Jaar 2 · 10%", "Jaar 3 · 5%"]

# ------------------------------------------------------------------ d1 dashboard
def d1():
    hero = (f'<div class="card kpi main1"><small>{ic("wallet")}Beschikbaar om uit te betalen</small><div class="v">€ 1.284,50</div>'
            f'<div class="dl">excl. btw · volgende uitbetaalronde 1 oktober</div><span class="btn">{ic("send")}Uitbetaling aanvragen</span></div>'
            + kpi("Verdiend dit jaar", "trend", "€ 4.697,00", '<span class="up">+38%</span><span class="muted">t.o.v. 2025</span>')
            + kpi("In behandeling", "clock", "€ 262,50", '<span class="muted">vrij na 14 dagen</span>')
            + kpi("Actieve werkgevers", "building", "9 <span class='muted' style='font-size:var(--text-md);font-weight:400'>van 12</span>", '<span class="up">+2</span><span class="muted">deze maand</span>'))
    mx = max(EARN)
    ch = "".join(f'<div><i class="{"now" if i==11 else ("pend" if False else "")}" style="height:{round(v/mx*100)}%"></i><span>{m}</span></div>' for i, (m, v) in enumerate(zip(MONTHS, EARN)))
    fun = "".join(f'<div><small>{a}</small><b>{b}</b><em>{c}</em></div>' for a, b, c in [("Bezoeken via je link", "214", "sinds start"), ("Aangemeld", "12", "6% van bezoeken"), ("Eerste aankoop", "11", "92% van aanmeldingen"), ("Nog actief", "9", "aankoop < 90 dagen")])
    todo = [("wn", "building", "Horeca De Haven heeft nog niets gekocht", "Aangemeld op 03-09 · tip: bel over de gratis start-highlight", "Bekijken"),
            ("", "clock", "Hotel Duinzicht gaat naar jaar 3", "Vanaf 08-11-2026 krijg je 5% in plaats van 10%", "Bekijken"),
            ("", "receipt", "Factuur LOB-SB-2026-0042 is betaald", "€ 1.089,00 incl. btw op NL•• •••• 4821", "Download")]
    td = "".join(f'<li><span class="ico {c}">{ic(i)}</span><div class="t"><b>{t}</b><small>{s}</small></div><span class="btn sm">{a}</span></li>' for c, i, t, s, a in todo)
    top = "".join(f'<li><span class="k">{k+1}</span><div><b>{n}</b><small>{p} · <span class="st {st[0]}" style="font-size:var(--text-xs)"><i></i>{st[1]}</span></small></div><b class="amt2">{c}</b></li>' for k, (n, p, _, st, _, _, _, c, _) in enumerate(sorted(EMP, key=lambda e: -float(e[7].replace('€','').replace('.','').replace(',','.').strip() or 0))[:5]))
    body = f'''<div class="ph"><div><h1>Goedemiddag, Tom</h1><p>Zo gaat het met je verkoop. Bedragen zijn excl. btw.</p></div>
<div class="act"><div class="seg"><span>Maand</span><span class="on">Dit jaar</span><span>Alles</span></div><span class="btn">{ic("copy")}Kopieer mijn link</span></div></div>
<div class="hero">{hero}</div>
<div class="row" style="grid-template-columns:minmax(0,1fr) 400px">
<div style="display:flex;flex-direction:column;gap:var(--space-4);min-width:0">
<div class="card"><div class="ch"><h2>Je commissie per maand</h2><span class="pill">€ 5.207 in 12 maanden</span><span class="sp"></span><a>Alle mutaties{ic("chevr")}</a></div>
<div class="chart">{ch}</div><div class="leg2"><span><i style="background:color-mix(in srgb,var(--brand) 22%,var(--surface))"></i>Uitbetaald of beschikbaar</span><span><i style="background:var(--brand)"></i>Deze maand (deels in behandeling)</span></div></div>
<div class="card"><div class="ch"><h2>Van link naar klant</h2><span class="newtag">Nieuw</span><span class="sp"></span><span class="muted sm">sinds start</span></div><div class="fstep">{fun}</div></div>
</div>
<div style="display:flex;flex-direction:column;gap:var(--space-4)">
<div class="card"><div class="ch"><h2>Te doen</h2><span class="pill">3</span></div><ul class="todo">{td}</ul></div>
<div class="card" style="overflow:hidden"><div class="ch"><h2>Beste werkgevers</h2><span class="sp"></span><a>Alle 12{ic("chevr")}</a></div>
<p class="muted sm" style="padding:0 16px 4px">Jouw commissie in 2026</p><ul class="rank">{top}</ul><div style="height:8px"></div></div>
</div></div>'''
    return page("dash", ["Partner", "Dashboard"], body)

# ------------------------------------------------------------------ d2 link & toolkit
def d2():
    mats = [("file", "Flyer A4 met QR", "Jouw code en QR staan erop. Met de actuele prijzen.", "PDF", "Download", False),
            ("card", "Visitekaartje met QR", "Voor op tafel of in je tas. 10 per A4.", "PDF", "Download", True),
            ("pres", "Presentatie voor een klant", "7 dia's: wat Lobsy doet, prijzen, zo start je.", "PDF", "Download", True),
            ("mail", "E-mail om te sturen", "Korte tekst met jouw link. Pas hem aan en kopieer.", "Tekst", "Kopieer", True),
            ("coins", "Prijskaart", "Tokenpakketten en wat een vacature kost.", "PDF", "Download", False),
            ("msg", "WhatsApp-bericht", "Kant-en-klaar bericht met jouw link.", "Tekst", "Delen", False)]
    mt = "".join(f'<div class="card mat"><span class="ico2">{ic(i)}</span><b>{t}{" <span class=newtag>Nieuw</span>" if n else ""}</b><p>{d}</p><div class="ft"><span class="pill line">{k}</span><span class="sp"></span><span class="btn sm">{ic("dl" if a=="Download" else ("copy" if a=="Kopieer" else "share"))}{a}</span></div></div>' for i, t, d, k, a, n in mats)
    body = f'''<div class="ph"><div><h1>Mijn link & materiaal</h1><p>Deel je link. Meldt een werkgever zich aan? Dan hoort hij bij jou en krijg je commissie op zijn aankopen.</p></div></div>
<div class="row" style="grid-template-columns:minmax(0,1fr) 380px;align-items:start">
<div style="display:flex;flex-direction:column;gap:var(--space-4);min-width:0">
<div class="card" style="padding:16px 18px;display:grid;grid-template-columns:minmax(0,1fr) auto;gap:18px;align-items:center">
<div style="display:flex;flex-direction:column;gap:10px;min-width:0">
<span class="lbl" style="margin:0">Jouw persoonlijke link</span>
<div class="linkbox"><span class="u">{LINK}</span><span class="btn pri">{ic("copy")}Kopieer</span></div>
<div style="display:flex;gap:16px;align-items:center;flex-wrap:wrap"><span class="muted sm">Of geef je code:</span><span class="code" style="font-size:var(--text-lg);color:var(--brand)">{CODE}</span><span class="pill ok">{ic("check")}Actief</span></div>
<div class="share"><span class="btn">{ic("msg")}WhatsApp</span><span class="btn">{ic("mail")}Mail</span><span class="btn">{ic("qr")}QR downloaden</span><span class="btn">{ic("eye")}Bekijk wat de werkgever ziet</span></div>
<div class="infobox">{ic("info")}<div><b>Zo tellen we een aanmelding voor jou</b><br>Klikt iemand op je link? Dan onthouden we dat 30 dagen. Of de werkgever vult je code in bij het aanmelden. <span class="newtag">Nieuw</span></div></div>
</div>
<div class="qrw">{qr_svg(150)}<span class="muted sm">Scan voor {LINK}</span></div></div>
<div><div class="ch" style="padding:0 0 10px"><h2>Materiaal</h2><span class="sp"></span><span class="muted sm">Alles staat al klaar met jouw code</span></div><div class="mats">{mt}</div></div>
</div>
<div style="display:flex;flex-direction:column;gap:var(--space-4)">
<div class="card"><div class="ch"><h2>Pitch in 60 seconden</h2><span class="newtag">Nieuw</span></div>
<ul class="pitch"><li><i>1</i><div><b>Het probleem</b><span>“Vindt u moeilijk mensen? Kandidaten reageren vaak niet op een lange vacature.”</span></div></li>
<li><i>2</i><div><b>Wat Lobsy doet</b><span>“Lobsy toont uw vacature op een kaart dichtbij huis. Kandidaten solliciteren in een paar tikken.”</span></div></li>
<li><i>3</i><div><b>Wat het kost</b><span>“U betaalt alleen per vacature, met tokens. Geen abonnement.”</span></div></li>
<li><i>4</i><div><b>Zo start u</b><span>“Meld u aan via mijn link. Uw eerste vacature wordt gratis uitgelicht.”</span></div></li></ul>
<p class="quote">Tip: laat op je telefoon de banenkaart zien van hun eigen dorp.</p></div>
<div class="card"><div class="ch"><h2>Wat krijgt de werkgever?</h2></div>
<ul class="checks" style="padding-bottom:12px"><li>{ic("check")}<span>Gratis start-highlight op de eerste vacature (2 tokens)</span></li>
<li>{ic("check")}<span>Eén vast aanspreekpunt: jij</span></li>
<li>{ic("check")}<span>Geen abonnement, geen korting nodig</span></li></ul></div>
<div class="card"><div class="ch"><h2>Jouw commissie</h2></div>
<div class="staf"><div class="on"><small>Jaar 1</small><b>25%</b></div><div><small>Jaar 2</small><b>10%</b></div><div><small>Jaar 3</small><b>5%</b></div></div>
<p class="muted sm" style="padding:0 16px 14px">Over elke tokenaankoop van jouw werkgevers, excl. btw. Jaar 1 start op de dag van aanmelding.</p></div>
</div></div>'''
    return page("link", ["Partner", "Verkopen", "Mijn link & materiaal"], body)

# ------------------------------------------------------------------ d3 werkgevers + detail drawer
def d3():
    rows = ""
    for n, p, since, s, y, yp, cnt, com, last in EMP:
        rows += (f'<tr class="{"sel" if n.startswith("Kwekerij") else ""}"><td><b>{n}</b><div class="muted sm">{p}</div></td><td class="muted">{since}</td>'
                 f'<td><span class="st {s[0]}"><i></i>{s[1]}</span></td><td>{YR[y]}<div class="yr" style="width:120px">{"".join(f"<div><i style=width:{100 if k<y else (yp if k==y else 0)}%></i></div>" for k in range(3))}</div></td>'
                 f'<td class="num">{cnt}</td><td class="muted">{last}</td><td class="num"><b>{com}</b></td></tr>')
    lines = [("18-09-2026", "Pakket Groei · 50 tokens", "€ 875,00", "€ 218,75", ("wn", "In behandeling")),
             ("02-08-2026", "100 tokens", "€ 300,00", "€ 75,00", ("ok", "Beschikbaar")),
             ("14-05-2026", "Pakket Gold · 100 tokens", "€ 1.800,00", "€ 450,00", ("line", "Uitbetaald")),
             ("15-03-2026", "Pakket Silver · 40 tokens", "€ 800,00", "€ 200,00", ("line", "Uitbetaald"))]
    lr = "".join(f'<tr><td>{w}<div class="muted sm">{d} · aankoop {a}</div></td><td class="num"><b>{c}</b><div><span class="pill {s[0]}">{s[1]}</span></div></td></tr>' for d, w, a, c, s in lines)
    drawer = f'''<div class="scrim"></div><aside class="drawer" style="width:540px"><div class="dh"><div><h2>Kwekerij De Zonnehoek</h2><p class="muted sm">Naaldwijk · via jouw link · sinds 12-03-2026</p></div><span class="x">{ic("x")}</span></div>
<div class="db" style="gap:14px">
<div class="fact" style="grid-template-columns:repeat(3,1fr)"><div><small>Jouw commissie totaal</small><b>€ 943,75</b></div><div><small>Aankopen</small><b>4</b></div><div><small>Nu</small><b>Jaar 1 · 25%</b></div></div>
<div><span class="lbl" style="margin-top:0">Commissiejaren</span>
<div class="yr"><div><i style="width:55%"></i></div><div></div><div></div></div>
<div class="yrl"><span>Jaar 1 · 25% · t/m 11-03-2027</span><span>Jaar 2 · 10%</span><span>Jaar 3 · 5% · t/m 11-03-2029</span></div></div>
<div><span class="lbl">Zo ging het</span><ul class="tl">
<li><i>{ic("check")}</i><div><b>Aangemeld via jouw link</b><small>Start-highlight gekregen</small></div><span class="d">12-03-2026</span></li>
<li><i>{ic("check")}</i><div><b>Eerste aankoop</b><small>Pakket Silver</small></div><span class="d">15-03-2026</span></li>
<li class="todo"><i>{ic("clock")}</i><div><b>Jaar 2 begint</b><small>Commissie wordt 10%</small></div><span class="d">12-03-2027</span></li></ul></div>
<div><span class="lbl" style="margin-top:0">Commissie per aankoop</span>
<div class="card" style="overflow:hidden;border-radius:var(--radius-sm)"><table><thead><tr><th>Aankoop (excl. btw)</th><th class="num">Jij krijgt</th></tr></thead><tbody>{lr}</tbody></table></div></div>
<div class="okbox">{ic("shield2")}<div>Je ziet bedrijfsnaam, plaats en je eigen commissie. Geen contactpersonen, kandidaten of vacatures.</div></div>
</div>
<div class="df"><span class="btn">{ic("msg")}Vraag Lobsy om hulp</span><span class="sp" style="flex:1"></span><span class="btn pri">{ic("ext")}Bekijk openbare vacatures</span></div></aside>'''
    body = f'''<div class="ph"><div><h1>Mijn werkgevers</h1><p>12 werkgevers via jouw link of code · 9 actief</p></div>
<div class="act"><span class="btn">{ic("dl")}Export (CSV)</span></div></div>
<div class="kpis" style="grid-template-columns:repeat(4,minmax(0,1fr))">
{kpi("Aangemeld", "building", "12", "<span class=muted>sinds start</span>")}{kpi("Eerste aankoop", "check", "11", "<span class=muted>92% van aanmeldingen</span>")}
{kpi("Stil (> 90 dagen)", "clock", "1", "<span class=muted>even bellen?</span>")}{kpi("Gem. per werkgever", "euro", "€ 391", "<span class=muted>commissie 2026</span>")}</div>
<div class="card" style="overflow:hidden"><div class="fbar"><span class="inp w">{ic("search")}Zoek werkgever of plaats…</span><span class="dd">Status <em>Alle</em>{ic("chevd")}</span><span class="dd">Commissiejaar <em>Alle</em>{ic("chevd")}</span></div>
<table><thead><tr><th>Werkgever</th><th>Aangemeld</th><th>Status</th><th>Commissiejaar</th><th class="num">Aankopen</th><th>Laatste aankoop</th><th class="num">Commissie 2026</th></tr></thead><tbody>{rows}</tbody></table>
<div class="pag"><span>1–8 van 12</span><span class="sp"></span><span class="btn sm">{ic("chevl")}</span><span class="btn sm">{ic("chevr")}</span></div></div>'''
    return page("emp", ["Partner", "Verkopen", "Mijn werkgevers"], body, extra=drawer)

# ------------------------------------------------------------------ d4 wallet
LEDGER = [
    ("26-09-2026", "Commissie · Hotel Duinzicht", "Jaar 2 · 10% over € 437,50", "+ € 43,75", ("wn", "In behandeling"), "vrij op 10-10"),
    ("18-09-2026", "Commissie · Kwekerij De Zonnehoek", "Jaar 1 · 25% over € 875,00", "+ € 218,75", ("wn", "In behandeling"), "vrij op 02-10"),
    ("03-09-2026", "Commissie · Bakkerij Van Leeuwen", "Jaar 1 · 25% over € 175,00", "+ € 43,75", ("ok", "Beschikbaar"), ""),
    ("01-09-2026", "Uitbetaling · factuur LOB-SB-2026-0042", "Naar NL•• •••• 4821", "− € 900,00", ("line", "Betaald"), ""),
    ("11-08-2026", "Commissie · Transport Westland BV", "Jaar 2 · 10% over € 300,00", "+ € 30,00", ("ok", "Beschikbaar"), ""),
    ("02-08-2026", "Commissie · Kwekerij De Zonnehoek", "Jaar 1 · 25% over € 300,00", "+ € 75,00", ("ok", "Beschikbaar"), ""),
    ("28-06-2026", "Correctie · Schoonmaakbedrijf Helder", "Aankoop terugbetaald", "− € 43,75", ("line", "Verrekend"), ""),
    ("22-06-2026", "Founder-bonus · Installatiebedrijf Groen", "20% van startpakket € 2.500", "+ € 500,00", ("ok", "Beschikbaar"), ""),
]
def d4(drawer=False):
    hero = (f'<div class="card kpi main1"><small>{ic("wallet")}Beschikbaar</small><div class="v">€ 1.284,50</div><div class="dl">excl. btw · + € 269,75 btw</div><span class="btn">{ic("send")}Uitbetaling aanvragen</span></div>'
            + kpi("In behandeling", "clock", "€ 262,50", '<span class="muted">vrij na 14 dagen (bedenktijd)</span>')
            + kpi("Aangevraagd", "send", "€ 0,00", '<span class="muted">geen open aanvraag</span>')
            + kpi("Uitbetaald in 2026", "check", "€ 3.150,00", '<span class="muted">4 facturen</span>'))
    rows = "".join(f'<tr><td class="muted">{d}</td><td><b>{t}</b><div class="muted sm">{s}</div></td><td><span class="pill {st[0]}">{st[1]}</span>{f"<div class=\'muted sm\' style=\'margin-top:2px\'>{n}</div>" if n else ""}</td><td class="num"><b style="color:{"var(--success)" if a.startswith("+") else "var(--text)"}">{a}</b></td></tr>' for d, t, s, a, st, n in LEDGER)
    inv = "".join(f'<li><span class="ico">{ic("receipt")}</span><div class="t"><b class="code" style="font-size:var(--text-sm)">{n}</b><small>{d} · {a} incl. btw</small></div><span class="pill {c}">{s}</span><span class="btn sm">{ic("dl")}PDF</span></li>' for n, d, a, s, c in [
        ("LOB-SB-2026-0042", "01-09-2026", "€ 1.089,00", "Betaald", "ok"), ("LOB-SB-2026-0031", "01-07-2026", "€ 726,00", "Betaald", "ok"), ("LOB-SB-2026-0019", "01-05-2026", "€ 1.028,50", "Betaald", "ok")])
    body = f'''<div class="ph"><div><h1>Wallet & uitbetalingen</h1><p>Je commissie, je uitbetalingen en je facturen. Lobsy maakt de factuur voor je (self-billing).</p></div>
<div class="act"><span class="btn">{ic("dl")}Jaaroverzicht 2026 (PDF)</span></div></div>
<div class="hero">{hero}</div>
<div class="row" style="grid-template-columns:minmax(0,1fr) 380px;align-items:start">
<div class="card" style="overflow:hidden;min-width:0"><div class="tabs" style="padding:0 16px;margin:0"><span class="on">Mutaties</span><span>Uitbetalingen</span><span>Facturen</span></div>
<div class="fbar"><span class="dd">Periode <em>2026</em>{ic("chevd")}</span><span class="dd">Soort <em>Alle</em>{ic("chevd")}</span><span class="dd">Status <em>Alle</em>{ic("chevd")}</span></div>
<table><thead><tr><th>Datum</th><th>Omschrijving</th><th>Status</th><th class="num">Bedrag excl. btw</th></tr></thead><tbody>{rows}</tbody></table>
<div class="pag"><span>1–8 van 46</span><span class="sp"></span><span class="btn sm">{ic("chevl")}</span><span class="btn sm">{ic("chevr")}</span></div></div>
<div style="display:flex;flex-direction:column;gap:var(--space-4)">
<div class="card"><div class="ch"><h2>Zo werkt uitbetalen</h2></div>
<ul class="tl" style="padding:4px 16px 4px"><li><i>{ic("check")}</i><div><b>Commissie komt binnen</b><small>14 dagen in behandeling. Dan kan een klant nog geld terugvragen.</small></div><span></span></li>
<li><i>{ic("check")}</i><div><b>Jij vraagt uitbetaling aan</b><small>Vanaf € 50. Lobsy maakt de factuur namens jou.</small></div><span></span></li>
<li><i>{ic("check")}</i><div><b>Lobsy keurt goed</b><small>Uitbetaling op de 1e werkdag van de maand.</small></div><span></span></li>
<li class="todo"><i>{ic("euro")}</i><div><b>Geld op je rekening</b><small>Binnen 3 werkdagen op NL•• •••• 4821.</small></div><span></span></li></ul></div>
<div class="card"><div class="ch"><h2>Laatste facturen</h2><span class="sp"></span><a>Alle facturen{ic("chevr")}</a></div><ul class="todo">{inv}</ul></div>
<div class="card" style="padding:12px 16px"><div class="kvl" style="grid-template-columns:120px 1fr auto"><span>Uitbetaalrekening</span><b style="white-space:nowrap">NL•• 4821</b><a class="btn sm ghost">Wijzigen</a><span>Btw</span><b>21%</b><a class="btn sm ghost">Wijzigen</a></div></div>
</div></div>'''
    extra = ""
    if drawer:
        extra = f'''<div class="scrim"></div><aside class="drawer" style="width:480px"><div class="dh"><div><h2>Uitbetaling aanvragen</h2><p class="muted sm">Lobsy maakt de factuur namens jou (self-billing).</p></div><span class="x">{ic("x")}</span></div>
<div class="db" style="gap:14px">
<div><span class="lbl" style="margin-top:0">Bedrag excl. btw</span><div class="amt">€ 1.284,50<small>Alles</small></div><p class="muted sm" style="margin-top:5px">Beschikbaar € 1.284,50 · minimaal € 50,00</p></div>
<table class="sumt"><tbody><tr><td class="muted">Commissie excl. btw</td><td>€ 1.284,50</td></tr><tr><td class="muted">Btw 21%</td><td>€ 269,75</td></tr><tr class="tot"><td>Jij ontvangt</td><td>€ 1.554,25</td></tr></tbody></table>
<div class="kvl" style="grid-template-columns:120px 1fr"><span>Naar rekening</span><b>NL•• •••• •••• 4821 · T. Hendriks</b><span>Uitbetaling</span><b>1 oktober · daarna binnen 3 werkdagen</b></div>
<div><span class="lbl" style="margin-top:0">Voorbeeld factuur</span>
<div class="invprev"><div class="h"><div><b>Hendriks Sales & Advies</b><div class="muted">KvK 81234567 · btw NL123456789B01</div></div><span class="lg"><img src="{LOGO}" alt="">Lobsy</span></div>
<div class="muted">Factuur LOB-SB-2026-0051 · 01-10-2026 · aan Lobsy B.V.</div>
<div style="margin:6px 0">7 commissieregels · september 2026</div>
<div><b>Factuur uitgereikt door afnemer</b> <span class="muted">(self-billing, volgens onze afspraak van 12-01-2026)</span></div></div></div>
<div class="infobox">{ic("info")}<div>Klopt je btw niet? Gebruik je bijvoorbeeld de kleineondernemersregeling (KOR)? Pas dit eerst aan bij Profiel & gegevens.</div></div>
</div>
<div class="df"><span class="sp" style="flex:1"></span><span class="btn ghost">Annuleren</span><span class="btn pri">{ic("send")}Aanvragen · € 1.554,25</span></div></aside>'''
    return page("wallet", ["Partner", "Geld", "Wallet & uitbetalingen"], body, extra=extra)

def d5(): return d4(drawer=True)

# ------------------------------------------------------------------ d6 profiel
def d6():
    body = f'''<div class="ph"><div><h1>Profiel & gegevens</h1><p>Deze gegevens staan op je facturen. Houd ze goed bij.</p></div></div>
<div class="card" style="max-width:980px">
<div class="setl"><div><h3>Bedrijf</h3><p class="d">Staat op elke factuur die Lobsy voor je maakt.</p></div>
<div class="fgrid"><div class="full"><span class="lbl">Bedrijfsnaam</span><span class="field">Hendriks Sales & Advies</span></div>
<div><span class="lbl">KvK-nummer</span><span class="field">81234567</span></div><div><span class="lbl">Land</span><span class="field">Nederland</span></div>
<div class="full"><span class="lbl">Adres</span><span class="field">Havenstraat 12, 2681 AB Monster</span></div></div></div>
<div class="setl"><div><h3>Btw</h3><p class="d">Bepaalt of er btw op je factuur komt.</p></div>
<div style="display:grid;grid-template-columns:1fr 1fr;gap:10px"><div class="radio on"><span class="rd"></span><b>Ik ben btw-plichtig</b><p>21% btw op de factuur. Btw-nummer NL123456789B01.</p></div>
<div class="radio"><span class="rd"></span><b>Ik gebruik de KOR <span class="newtag">Nieuw</span></b><p>Kleineondernemersregeling: geen btw op de factuur.</p></div></div></div>
<div class="setl"><div><h3>Uitbetaalrekening</h3><p class="d">Hier maakt Lobsy je geld naartoe over.</p></div>
<div><div class="kvl"><span>IBAN</span><b>NL•• •••• •••• 4821</b><span class="btn sm">{ic("pen")}Wijzigen</span><span>Op naam van</span><b>T. Hendriks</b><span></span></div>
<div class="warnbox" style="margin-top:10px;display:flex;gap:10px;align-items:flex-start">{ic("lock")}<div><b>Veilig wijzigen</b> <span class="newtag">Nieuw</span><br><span class="muted sm">Voor een nieuwe IBAN vragen we je 2FA-code. Je krijgt een mail. De eerste uitbetaling naar een nieuwe rekening wacht 3 dagen.</span></div></div></div></div>
<div class="setl"><div><h3>Afspraken</h3><p class="d">Wat je met Lobsy hebt afgesproken.</p></div>
<div class="kvl"><span>Samenwerking</span><b>Bemiddelingsovereenkomst · versie 28-08-2026</b><span class="btn sm">{ic("dl")}PDF</span>
<span>Self-billing</span><b>Akkoord op 12-01-2026: Lobsy maakt je facturen</b><span class="btn sm">{ic("dl")}PDF</span>
<span>Commissie</span><b>25% · 10% · 5% over 3 jaar per werkgever</b><span></span></div></div>
<div class="setl"><div><h3>Beveiliging</h3><p class="d">Je ziet geld en een rekeningnummer. Daarom is 2FA verplicht.</p></div>
<div class="kvl"><span>Tweestapsverificatie</span><b><span class="pill ok">{ic("lock")}Aan · authenticator-app</span></b><span class="btn sm">Beheren</span>
<span>Meldingen per mail</span><b>Nieuwe werkgever · commissie beschikbaar · uitbetaling</b><span class="btn sm">Wijzigen</span></div></div>
</div>'''
    return page("prof", ["Partner", "Account", "Profiel & gegevens"], body)

# ------------------------------------------------------------------ mobile
def m1():
    mx = max(EARN[-6:])
    ch = "".join(f'<div><i class="{"now" if i==5 else ""}" style="height:{round(v/mx*100)}%"></i><span>{m}</span></div>' for i, (m, v) in enumerate(zip(MONTHS[-6:], EARN[-6:])))
    li = "".join(f'<li><span class="ico {c}">{ic(i)}</span><div class="sp"><b>{t}</b><small>{s}</small></div>{ic("chevr")}</li>' for c, i, t, s in [
        ("wn", "building", "Horeca De Haven kocht nog niets", "Aangemeld 03-09 · even bellen?"), ("", "clock", "Hotel Duinzicht naar jaar 3", "Vanaf 08-11: 5% in plaats van 10%")])
    body = f'''<main class="mmain" style="padding-bottom:90px"><h1>Hoi Tom</h1><div class="muted sm">Salesmanager · <span class="code">{CODE}</span></div>
<div class="mhero" style="margin-top:12px"><small>Beschikbaar om uit te betalen</small><div class="v">€ 1.284,50</div>
<div class="row2"><div>In behandeling<b>€ 262,50</b></div><div>Verdiend 2026<b>€ 4.697,00</b></div></div><span class="btn">{ic("send")}Uitbetaling aanvragen</span></div>
<div class="mkpis">{kpi("Actieve werkgevers", "building", "9", "<span class=muted>van 12</span>")}{kpi("Aanmeldingen", "trend", "12", "<span class=up>+1</span><span class=muted>deze maand</span>")}</div>
<div class="mh">Commissie per maand</div><div class="mcard"><div class="mchart">{ch}</div><div style="height:10px"></div></div>
<div class="mh" style="margin-top:16px">Te doen <span class="cnt" style="margin:0">2</span><a>Alles</a></div>
<div class="mcard"><ul class="mlist">{li}</ul></div>
<div class="mcard" style="margin-top:12px;padding:12px 14px;display:flex;align-items:center;gap:10px"><span class="ico2" style="width:36px;height:36px;border-radius:10px;display:grid;place-items:center;background:var(--accent-soft);color:var(--brand)">{ic("qr")}</span><div style="flex:1"><b>Deel je link</b><div class="muted sm">QR laten scannen bij de klant</div></div><span class="btn pri sm">{ic("share")}Delen</span></div>
</main>'''
    return mpage(body, "dash")

def m2():
    rows = "".join(f'<li><div class="sp"><b>{t.split(" · ")[1] if " · " in t else t}</b><small>{d} · {t.split(" · ")[0]}</small></div><div style="text-align:end"><b style="color:{"var(--success)" if a.startswith("+") else "var(--text)"};font-variant-numeric:tabular-nums">{a}</b><div><span class="pill {st[0]}">{st[1]}</span></div></div></li>' for d, t, s, a, st, n in LEDGER[:5])
    body = f'''<main class="mmain" style="padding-bottom:90px"><h1>Wallet</h1><div class="muted sm">Bedragen excl. btw</div>
<div class="mhero" style="margin-top:12px"><small>Beschikbaar</small><div class="v">€ 1.284,50</div>
<div class="row2"><div>In behandeling<b>€ 262,50</b></div><div>Uitbetaald 2026<b>€ 3.150,00</b></div></div><span class="btn">{ic("send")}Uitbetaling aanvragen</span></div>
<div class="mcard" style="margin-top:12px;padding:10px 14px"><b class="sm">Zo werkt het</b><div class="steps3" style="margin-top:6px"><span class="cur">{ic("send")}Aanvraag</span><em></em><span class="cur">{ic("check")}Akkoord</span><em></em><span class="cur">{ic("euro")}Betaald</span></div>
<p class="muted sm" style="margin-top:6px">Vanaf € 50 · uitbetaling op de 1e werkdag · naar NL•• 4821</p></div>
<div class="seg" style="margin-top:14px;width:100%;display:grid;grid-template-columns:repeat(3,1fr)"><span class="on">Mutaties</span><span>Uitbetalingen</span><span>Facturen</span></div>
<div class="mcard" style="margin-top:10px"><ul class="mlist">{rows}</ul></div>
</main>'''
    return mpage(body, "wallet")

def m3():
    body = f'''<main class="mmain" style="padding-bottom:90px"><h1>Mijn link</h1><div class="muted sm">Laat de klant scannen of stuur je link</div>
<div class="mcard" style="margin-top:12px;padding:18px 16px;display:grid;place-items:center;gap:10px">{qr_svg(216)}
<span class="code" style="font-size:var(--text-xl);color:var(--brand)">{CODE}</span><span class="muted sm">{LINK}</span></div>
<div class="mshare" style="margin-top:12px"><span class="btn">{ic("msg")}WhatsApp</span><span class="btn">{ic("mail")}Mail</span><span class="btn pri">{ic("copy")}Kopieer</span></div>
<div class="infobox" style="margin-top:12px">{ic("gift")}<div><b>Voor de werkgever:</b> de eerste vacature wordt gratis uitgelicht.</div></div>
<div class="mh" style="margin-top:16px">Materiaal<a>Alles</a></div>
<div class="mcard"><ul class="mlist"><li><span class="ico">{ic("file")}</span><div class="sp"><b>Flyer A4 met QR</b><small>PDF · met jouw code</small></div>{ic("dl")}</li>
<li><span class="ico">{ic("mic")}</span><div class="sp"><b>Pitch in 60 seconden</b><small>4 zinnen om te oefenen</small></div>{ic("chevr")}</li></ul></div>
</main>'''
    return mpage(body, "link")

SCREENS = [("sm-d1-dashboard", d1, "d"), ("sm-d2-link-materiaal", d2, "d"), ("sm-d3-werkgevers-detail", d3, "d"),
           ("sm-d4-wallet", d4, "d"), ("sm-d5-uitbetaling-aanvragen", d5, "d"), ("sm-d6-profiel-gegevens", d6, "d"),
           ("sm-m1-dashboard", m1, "m"), ("sm-m2-wallet", m2, "m"), ("sm-m3-link-qr", m3, "m")]
