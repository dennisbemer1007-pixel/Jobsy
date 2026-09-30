"""Shared shell for the werkgever-registratie mockups (wr-*). Reuses ../landing (tokens, pub- warm theme, icons, mascot)."""
import pathlib, sys
LAND = pathlib.Path(__file__).resolve().parent / "base"  # copy of the landing mockup base (branch docs/landing, docs/mockups/landing)
sys.path.insert(0, str(LAND))
from ui import ic, LOGO, MASCOT, GOOGLE, MSFT, vb  # noqa: E402
from ui import page as _page  # noqa: E402
from w_common import blob, em  # noqa: E402
from css_wr import R  # noqa: E402

CK = ic("check")
STEPS = [("Bedrijf zoeken", "KvK-nummer of naam"), ("Vestiging kiezen", "Heel bedrijf of één vestiging"),
         ("Jouw account", "Gegevens en inloggen"), ("Over je bedrijf", "Branche, cultuur, betrokkenheid"), ("Verifiëren", "Zakelijk e-mailadres of brief")]

def page(body, cls=""):
    return _page(body, R, cls)

def header(ctx="Bedrijf registreren"):
    return f'''<header class="wh"><div class="wrap"><a class="brand"><img src="{LOGO}" alt="">Lobsy</a><span class="ctx">{ctx}</span>
<div class="r"><span class="lang">{ic("globe")}NL{ic("chev")}</span><span class="sm muted">Al een account?</span><a class="btn sm">Inloggen</a></div></div></header>'''

def stepper(cur):
    out = []
    for n, (t, s) in enumerate(STEPS, 1):
        st = "done" if n < cur else ("on" if n == cur else "")
        num = CK if n < cur else str(n)
        opt = '<span class="opt">optioneel</span>' if n == 4 else ""
        out.append(f'<li class="{st}"><span class="n">{num}</span><span>{t}<small>{s}</small></span>{opt}</li>')
    return f'<ol class="stp" aria-label="Stappen">{"".join(out)}</ol>'

def guide(cur, title, text, wave=True):
    art = (blob("var(--sun-2)", 30, 30, 240, 190, 1) + f'<img class="lob" src="{MASCOT}" alt="">'
           + ('<span class="wv">👋</span>' if wave else ""))
    return f'''<aside class="guide"><div class="gart">{art}</div><div class="gb"><b>{title}</b>{text}</div>{stepper(cur)}</aside>'''

def wizard(cur, gtitle, gtext, card, wave=True):
    return page(f'''{header()}<main class="wrap wz">{guide(cur, gtitle, gtext, wave)}<section class="wcard">{card}</section></main>''')

def mob(cur, gtext, card, bar, title="Bedrijf registreren"):
    segs = "".join(f'<i class="{"d" if n < cur else ("on" if n == cur else "")}"></i>' for n in range(1, 6))
    body = f'''<header class="mhd"><a class="brand"><img src="{LOGO}" alt="">Lobsy</a><div class="r"><span class="ib" aria-label="Sluiten">{ic("x")}</span></div></header>
<div class="mpr">{segs}</div><div class="mst">Stap {cur} van 5 · {STEPS[cur-1][0]}</div>
<main class="mw"><div class="mg"><img src="{MASCOT}" alt=""><div class="gb">{gtext}</div></div><section class="wcard">{card}</section></main>
<div class="mbar">{bar}</div>'''
    return page(body, "m")

def acts(primary, back=True, skip=None, note=None):
    b = f'<a class="btn ghost">{ic("chevl")}Terug</a>' if back else ""
    s = f'<a class="skip">{skip}</a>' if skip else ""
    n = f'<span class="hint">{note}</span>' if note else ""
    return f'<div class="acts">{b}{n}<div class="sp"></div>{s}<a class="btn pri lg">{primary}{ic("arrow")}</a></div>'

def cb(on=False, inc=False):
    return f'<span class="cb {"on" if on else ("inc" if inc else "")}">{CK if on or inc else ""}</span>'
