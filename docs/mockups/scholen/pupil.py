"""Leerling-flow (12–16 jaar): login met school + klas + code, speelse wizard (60 vragen, 4 werelden),
hobby's/niet leuk, 'Dit ben jij', droombaan-checker, PDF. Mobiel 390x844 @2x + Chromebook 1366x768 + A4-PDF.
Reuses the lobster (10 shell plates), ocean scene, tokens and CSS from ../ontdekkingsreis/build.py (kid-friendlier copy,
lighter water, no BottomNav, no vacancies/partners)."""
import pathlib, importlib.util
HERE = pathlib.Path(__file__).resolve().parent
_local = HERE / "base" / "ontdekkingsreis_build.py"
spec = importlib.util.spec_from_file_location("orb", _local if _local.exists() else HERE.parent / "ontdekkingsreis" / "build.py")
orb = importlib.util.module_from_spec(spec); spec.loader.exec_module(orb)
P, ic, lobster, scene, SPRITE, LOGO = orb.P, orb.ic, orb.lobster, orb.scene, orb.SPRITE, orb.LOGO

P.update(
 info='<circle cx="12" cy="12" r="9"/><path d="M12 11v5M12 8h.01"/>',
 x='<path d="M6 6l12 12M18 6 6 18"/>',
 school='<path d="M3 10 12 5l9 5-9 5z"/><path d="M7 12v5c3 2 7 2 10 0v-5M21 10v6"/>',
 users='<circle cx="9" cy="8" r="3.5"/><path d="M2.5 20a6.5 6.5 0 0 1 13 0"/><path d="M16 4.5a3.5 3.5 0 0 1 0 7M18 14a6 6 0 0 1 3.5 6"/>',
 key='<circle cx="8" cy="15" r="4"/><path d="m11 12 9-9M17 6l3 3M15 8l2 2"/>',
 dl='<path d="M12 4v11M7 10l5 5 5-5M5 20h14"/>',
 target='<circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1.5"/>',
 star='<path d="M12 3l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z"/>',
 pause='<rect x="6" y="5" width="4" height="14" rx="1"/><rect x="14" y="5" width="4" height="14" rx="1"/>',
 alert='<path d="M12 3 2 20h20z"/><path d="M12 10v4M12 17h.01"/>',
 flag='<path d="M5 21V4M5 4h11l-2 4 2 4H5"/>',
)

WORLDS = [  # nr, place, question-title, model (for the design note only), depth
 (1, "Het koraalrif", "Hoe ben jij?", "persoonlijkheid · Big Five", 3),
 (2, "De schatgrot", "Wat doe je graag?", "interesses · RIASEC", 5),
 (3, "De vuurtoren", "Wat vind je belangrijk?", "drijfveren · Schwartz", 7),
 (4, "De lagune", "Waar voel je je thuis?", "sfeer · cultuur (OCP)", 9),
]

EXTRA = r'''
body.m .scene{bottom:0}body.c .scene{bottom:0}
.hd .me{display:flex;align-items:center;gap:8px}
.idp{height:36px;display:inline-flex;align-items:center;gap:6px;padding:0 12px;border-radius:var(--radius-pill);background:var(--bg);font-size:var(--text-sm);color:var(--brand);font-weight:600;white-space:nowrap}
.idp .i{width:15px;height:15px}.idp .code{font-family:ui-monospace,Menlo,monospace;letter-spacing:.05em}
body.m .mfoot{bottom:0;padding-bottom:18px}
body.m .mstage{padding-bottom:86px}
.demob{position:fixed;inset-inline-end:10px;bottom:84px;z-index:70;font-size:var(--text-xs);font-weight:600;color:var(--warn);background:var(--warn-soft);border:1px solid color-mix(in srgb,var(--warn) 30%,transparent);border-radius:var(--radius-pill);padding:2px 10px}
body.c .demob{bottom:14px}body.a4 .demob{top:104px;bottom:auto}
/* login */
.sel{height:52px;border:1px solid var(--border);border-radius:var(--radius-sm);display:flex;align-items:center;gap:10px;padding:0 12px;font-size:var(--text-md);background:var(--surface)}
.sel .i{color:var(--muted)}.sel span{flex:1;min-width:0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.sel small{color:var(--muted);font-size:var(--text-sm)}
.sel.on{border-color:var(--brand);box-shadow:0 0 0 2px var(--accent-soft)}
.codebox{display:flex;gap:6px;align-items:center}
.codebox span{width:44px;height:54px;border:1.5px solid var(--border);border-radius:var(--radius-sm);display:grid;place-items:center;font-family:ui-monospace,Menlo,monospace;font-size:var(--text-xl);font-weight:600;color:var(--brand);background:var(--surface)}
.codebox span.f{border-color:var(--brand);box-shadow:0 0 0 3px var(--accent-soft)}.codebox em{width:10px;height:2px;background:var(--muted);opacity:.5}
.safe{display:flex;gap:10px;align-items:flex-start;font-size:var(--text-sm);color:var(--text);background:var(--success-soft);border-radius:var(--radius-sm);padding:10px 12px}.safe .i{color:var(--success);margin-top:2px}
/* question */
body.m .mlob{min-height:0;padding:8px 4px}
body.m .step .world{padding-inline-end:96px}
.world{display:flex;align-items:center;gap:8px;font-size:var(--text-xs);font-weight:600;letter-spacing:.06em;text-transform:uppercase;color:var(--muted)}
.qrow{display:flex;align-items:flex-start;gap:10px;margin-top:14px}
.qrow .qtext{margin-top:0;flex:1}
.ib{width:44px;height:44px;border-radius:50%;flex:none;display:grid;place-items:center;border:1.5px solid var(--brand);color:var(--brand);background:var(--surface)}
.ib.on{background:var(--brand);color:#fff}.ib .i{width:22px;height:22px}
.ex{position:relative;margin-top:12px;background:var(--accent-soft);border-radius:var(--radius);padding:12px 44px 12px 14px;font-size:var(--text-sm);line-height:1.5}
.ex::before{content:"";position:absolute;top:-8px;inset-inline-end:16px;border:8px solid transparent;border-top:0;border-bottom-color:var(--accent-soft)}
.ex b{display:block;color:var(--brand);font-size:var(--text-sm);margin-bottom:2px}.ex .x{position:absolute;top:6px;inset-inline-end:6px;width:32px;height:32px;display:grid;place-items:center;color:var(--brand)}
.kik{display:grid;grid-template-columns:repeat(5,1fr);gap:6px;margin-top:16px}
.kik button{min-height:64px;border-radius:var(--radius);border:1.5px solid var(--border);background:var(--surface);font:inherit;font-size:var(--text-sm);font-weight:600;color:var(--brand);display:flex;flex-direction:column;align-items:center;justify-content:center;gap:5px;padding:6px 2px;line-height:1.15}
.kik button i{display:block;border-radius:50%;background:var(--sea-2)}
.kik button:nth-child(1) i{width:10px;height:10px}.kik button:nth-child(2) i{width:13px;height:13px}.kik button:nth-child(3) i{width:16px;height:16px}.kik button:nth-child(4) i{width:19px;height:19px}.kik button:nth-child(5) i{width:22px;height:22px}
.kik button.on{background:var(--brand);border-color:var(--brand);color:#fff}.kik button.on i{background:var(--gold-light)}
.puz{margin-top:16px;border-radius:var(--radius-sm);background:var(--bg);padding:10px 12px}
.puz p{font-size:var(--text-xs);font-weight:600;color:var(--muted);display:flex;justify-content:space-between}
.pcs{display:grid;grid-template-columns:repeat(4,1fr);gap:6px;margin-top:6px}
.pc{border-radius:var(--radius-sm);min-height:44px;display:flex;flex-direction:column;justify-content:center;align-items:center;text-align:center;padding:4px;font-size:var(--text-xs);color:var(--muted);border:1.5px dashed var(--border);background:var(--surface);line-height:1.2}
.pc b{color:var(--brand);font-size:var(--text-xs)}.pc.on{border:1.5px solid var(--gold-light);background:var(--gold-soft)}.pc.on b{color:var(--gold-ink)}
.pc.half{border-style:solid;border-color:var(--sea-2)}.pc.half b{color:var(--muted);filter:blur(.6px)}
.qprog{position:sticky;top:56px;z-index:20;background:var(--surface);padding:10px 14px;box-shadow:var(--shadow)}
.qprog .t{display:flex;align-items:center;gap:6px;font-size:var(--text-sm)}.qprog .t b{color:var(--brand)}.qprog .t .sp{flex:1}
.qprog .ok{display:flex;align-items:center;gap:4px;font-size:var(--text-xs);color:var(--success)}.qprog .ok .i{width:13px;height:13px}
.w4{display:grid;grid-template-columns:repeat(4,1fr);gap:4px;margin-top:22px;position:relative}
.w4 i{height:8px;border-radius:8px 8px 3px 3px;background:var(--pearl);position:relative;overflow:hidden}.w4 i s{position:absolute;inset:0 auto 0 0;background:var(--sea-5)}
.w4 .mk{position:absolute;top:-27px;width:28px;height:28px;transform:translateX(-50%)}
.w4l{display:grid;grid-template-columns:repeat(4,1fr);gap:4px;margin-top:4px;font-size:var(--text-xs);color:var(--muted)}.w4l span.on{color:var(--brand);font-weight:600}
/* hobbies */
.warnl{display:flex;gap:8px;align-items:flex-start;font-size:var(--text-xs);color:var(--warn);margin-top:6px}.warnl .i{width:14px;height:14px;margin-top:1px}
.chip.no.on{background:var(--warn-soft);border-color:var(--warn);color:var(--warn)}
/* result */
.storyp{font-size:var(--text-md);line-height:1.6;margin-top:10px}
.mt4{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-top:14px}
.mt{border-radius:var(--radius-sm);background:var(--bg);padding:10px 12px}
.mt small{display:block;font-size:var(--text-xs);color:var(--muted)}.mt b{display:block;font-size:var(--text-sm);color:var(--brand);margin-top:2px}
.sect{font-size:var(--text-md);font-weight:600;color:var(--brand);margin-top:18px}
.jobc{display:flex;flex-wrap:wrap;gap:6px;margin-top:8px}.jobc span{min-height:36px;border-radius:var(--radius-pill);background:var(--accent-soft);color:var(--brand);font-size:var(--text-sm);font-weight:600;padding:0 12px;display:inline-flex;align-items:center;gap:6px}
.jobc span .i{width:14px;height:14px}
.seen{display:flex;gap:8px;align-items:flex-start;font-size:var(--text-xs);color:var(--muted);margin-top:14px}.seen .i{width:14px;height:14px;margin-top:1px}
/* droombaan */
.dj{height:52px;border:1.5px solid var(--brand);border-radius:var(--radius-sm);display:flex;align-items:center;gap:10px;padding:0 12px;font-size:var(--text-md);font-weight:600;box-shadow:0 0 0 3px var(--accent-soft)}
.dj .i{color:var(--brand)}
.fitm{display:flex;align-items:center;gap:12px;margin-top:16px}
.meter{flex:1;display:flex;gap:4px}.meter i{flex:1;height:12px;border-radius:6px;background:var(--pearl)}.meter i.on{background:var(--success)}
.fitm b{font-size:var(--text-sm);color:var(--success);white-space:nowrap}
.hl{display:flex;flex-direction:column;gap:2px;margin-top:6px}.hl li{display:flex;gap:10px;align-items:flex-start;font-size:var(--text-sm);min-height:30px;padding-top:4px}
.hl li .i{width:18px;height:18px;color:var(--success)}.hl.lr li .i{color:var(--brand)}
.steps4{margin-top:8px;display:flex;flex-direction:column}
.steps4 li{display:grid;grid-template-columns:28px 1fr;gap:10px;position:relative;padding-bottom:12px}
.steps4 li::before{content:"";position:absolute;inset-inline-start:13px;top:26px;bottom:0;width:2px;background:var(--pearl)}.steps4 li:last-child::before{display:none}
.steps4 li>span{width:28px;height:28px;border-radius:50%;display:grid;place-items:center;background:var(--accent-soft);color:var(--brand);font-size:var(--text-sm);font-weight:600;position:relative}
.steps4 li.now>span{background:var(--brand);color:#fff}.steps4 li.goal>span{background:var(--gold-soft);color:var(--gold-ink)}
.steps4 li b{display:block;font-size:var(--text-sm)}.steps4 li small{display:block;font-size:var(--text-xs);color:var(--muted)}
.cheer{margin-top:14px;border-radius:var(--radius);background:var(--gold-soft);padding:12px 14px;display:flex;gap:10px;align-items:flex-start;font-size:var(--text-sm)}
.cheer .i{color:var(--gold-ink);width:20px;height:20px}
.alt{font-size:var(--text-xs);color:var(--muted);margin-top:4px}
/* chromebook */
body.c .stage{grid-template-columns:260px 620px 1fr;gap:20px;padding:18px 20px 20px;min-height:auto}
body.c .rail{padding:14px 16px}
body.c .st{min-height:30px}body.c .st.now{min-height:40px}
body.c .step{padding:18px 26px 16px}
body.c .lz{min-height:560px;padding-bottom:10px}
body.c .qtext{font-size:var(--text-xl)}
/* pdf */
body.a4{background:var(--pearl)}
.a4p{width:794px;min-height:1123px;background:var(--surface);padding:0 0 32px;position:relative}
.a4h{background:var(--brand);color:#fff;padding:22px 40px;display:flex;align-items:center;gap:14px}
.a4h img{width:40px;height:40px;background:#fff;border-radius:10px;padding:3px}.a4h b{font-size:var(--text-xl);font-weight:700}.a4h small{display:block;opacity:.8;font-size:var(--text-sm)}
.a4h .r{margin-inline-start:auto;text-align:end;font-size:var(--text-sm)}.a4h .r b{font-family:ui-monospace,Menlo,monospace;font-size:var(--text-lg);letter-spacing:.06em;font-weight:600}
.a4b{padding:22px 40px 0;display:grid;grid-template-columns:1fr 250px;gap:24px}
.a4b h2{font-size:var(--text-lg);font-weight:600;color:var(--brand);margin-top:16px}
.nm{display:flex;align-items:flex-end;gap:10px;font-size:var(--text-sm);color:var(--muted);margin:18px 40px 0}.nm span{flex:1;border-bottom:1.5px dotted var(--muted);height:18px}
.a4f{position:absolute;inset:auto 40px 22px 40px;border-top:1px solid var(--pearl);padding-top:10px;font-size:var(--text-xs);color:var(--muted);display:flex;justify-content:space-between}
'''
CSS = orb.CSS + EXTRA

def header(m=True, login=False):
    right = ('<div class="lang" aria-label="Taal: Nederlands"><i class="flag"></i>NL ' + ic("chevd") + '</div>') if login else \
            (f'<span class="idp">{ic("users")}2B · <span class="code">K7Q-M2P</span></span>'
             + ('' if m else f'<a class="btn">{ic("pause")}Pauze</a>'))
    return (f'<header class="hd"><div class="wm"><img src="{LOGO}" alt=""><b>Lobsy</b><span>Ontdek wie jij bent</span></div>'
            f'<div class="hr">{right}</div></header>')

def page(title, body, depth, kind="m", extra="", lob_x=60):
    W, H = {"m": (390, 170), "c": (1366, 704)}[kind]
    sc = scene(depth, W, H, lob_x if kind == "m" else 1150)
    cls = {"m": "m", "c": "c d"}[kind]
    return (f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
            f'<title>{title} · Lobsy voor scholen</title><style>{CSS}</style></head><body class="{cls}">{SPRITE}'
            f'{header(kind == "m", title == "Inloggen")}{sc}{body}{extra}{'' if kind == "m" else '<span class="demob">Voorbeelddata</span>'}</body></html>')

def qprog(q, world, gone, label=None):
    w4 = ""
    for k in range(4):
        fill = max(0, min(15, q - k*15)) / 15 * 100
        w4 += f'<i><s style="width:{fill:.0f}%"></s></i>'
    pos = (q - .5) / 60 * 100
    lab = "".join(f'<span class="{"on" if k+1==world else ""}">{WORLDS[k][1].split(" ",1)[1].capitalize()}</span>' for k in range(4))
    return (f'<div class="qprog" role="group" aria-label="Vraag {q} van 60"><p class="t"><b>{label or f"Vraag {q} van 60"}</b><span class="muted">· wereld {world} van 4</span><span class="sp"></span>'
            f'<span class="ok">{ic("check")}Bewaard</span></p>'
            f'<div class="w4">{w4}<span class="mk" style="left:{pos:.1f}%">{lobster(28, gone)}</span></div><div class="w4l">{lab}</div></div>')

def mob(card, depth, title, foot, say, lsize=74, gone=0, prog="", new_shell=False, login=False):
    lob = f'<div class="mlob">{lobster(lsize, gone, new_shell, None, "Lobsy de kreeft")}<p class="bubble">{say}</p></div>'
    style = '<style>body.m .scene{top:56px}</style>' if not prog else ''
    body = f'{style}{prog}<main class="mstage">{lob}<section class="card step"><p class="demo cdemo">Voorbeelddata</p>{card}</section></main><div class="mfoot">{foot}</div>'
    return page(title, body, depth, "m")

def kik(sel=None):
    labels = ["Nee", "Niet echt", "Soms", "Best wel", "Ja!"]
    b = "".join(f'<button class="{"on" if k==sel else ""}" role="radio" aria-checked="{"true" if k==sel else "false"}"><i></i>{t}</button>' for k, t in enumerate(labels, 1))
    return f'<div class="kik" role="radiogroup" aria-label="Hoe goed past dit bij jou?">{b}</div>'

def puzzle(pieces, n_done):
    pc = ""
    for state, lab, what in pieces:
        cls = {"on": "pc on", "half": "pc half", "": "pc"}[state]
        pc += f'<div class="{cls}"><b>{lab}</b>{what}</div>'
    return f'<div class="puz"><p><span>Dit ziet Lobsy al over jou</span><span>{n_done} van 4</span></p><div class="pcs">{pc}</div></div>'

PIECES = [("on", "Zorgzaam", "zo ben jij"), ("half", "Maker?", "dit doe je graag"), ("", "?", "belangrijk"), ("", "?", "thuis")]

# ------------------------------------------------------------------ screens
def p1():
    card = (f'<h1>Log in met je code</h1><p class="lead">Kies je school en je klas. Typ dan de code van jouw kaartje.</p>'
            f'<div class="body">'
            f'<label class="fld">Jouw school<span class="sel">{ic("school")}<span>Voorbeeld College Westland</span><small>Naaldwijk</small>{ic("chevd")}</span></label>'
            f'<label class="fld">Jouw klas<span class="sel">{ic("users")}<span>2B</span>{ic("chevd")}</span></label>'
            f'<div class="fld">Jouw code<div class="codebox" aria-label="Code, 6 tekens"><span>K</span><span>7</span><span>Q</span><em></em><span>M</span><span>2</span><span class="f"></span></div>'
            f'<span class="hint">{ic("key")}Je code staat op het kaartje van je leraar.</span></div>'
            f'<p class="safe">{ic("shield")}<span>Je naam hoef je nergens in te vullen. Lobsy kent alleen jouw code.</span></p>'
            f'</div>')
    foot = f'<a class="btn pri">Start de reis{ic("right")}</a>'
    return mob(card, 0, "Inloggen", foot, "Hoi, ik ben Lobsy! Zullen we samen ontdekken wie jij bent?", lsize=84, gone=0, login=True)

def p2():
    card = (f'<p class="world">Wereld 2 · De schatgrot</p><h1>Wat doe je graag?</h1>'
            f'<div class="qrow"><p class="qtext">Ik wil graag weten hoe dingen werken, zoals een vulkaan of je telefoon.</p>'
            f'<span class="ib on" role="button" aria-expanded="true" aria-label="Voorbeeld bij deze vraag">{ic("info")}</span></div>'
            f'<div class="ex" role="note"><b>Stel je voor…</b>Je ziet een filmpje over een vulkaan die uitbarst. Daarna zoek je nog drie filmpjes op, omdat je precies wilt weten hoe het zit.'
            f'<span class="x" aria-label="Sluiten">{ic("x")}</span></div>'
            f'{kik(4)}{puzzle(PIECES, 1)}')
    foot = f'<a class="btn" aria-label="Vorige vraag">{ic("left")}</a><span class="sp" style="flex:1"></span><a class="btn pri">Volgende{ic("right")}</a>'
    return mob(card, 5, "Vraag", foot, "Goed bezig! Nog 1 vraag, dan valt er weer een schaaltje af.", lsize=70, gone=3, prog=qprog(23, 2, 3))

def chip(t, on=False, no=False):
    return f'<span class="chip{" on" if on else ""}{" no" if no else ""}" role="checkbox" aria-checked="{"true" if on else "false"}">{ic("check") if on else ""}{t}</span>'

def p3():
    likes = [("Gamen", 0), ("Tekenen", 1), ("Sporten", 0), ("Dansen", 0), ("Muziek", 0), ("Koken of bakken", 1), ("Dieren", 1), ("Bouwen of knutselen", 0),
             ("Lezen", 0), ("Filmpjes maken", 0), ("Fietsen repareren", 1), ("Programmeren", 0), ("Buiten zijn", 0)]
    dis = [("Voor de klas praten", 1), ("Lang stilzitten", 1), ("Vies worden", 0), ("Veel lezen", 0), ("Rekenen", 0), ("Drukte en lawaai", 0), ("Alleen werken", 0)]
    card = (f'<p class="world">Pauze-eiland</p><h1>Wat vind jij leuk?</h1><p class="lead">Tik alles aan wat bij je past. Het mag ook niks zijn.</p>'
            f'<div class="body"><section><h2 class="q">Dit doe ik graag</h2><div class="chips">{"".join(chip(t, o) for t, o in likes)}<span class="chip add">{ic("plus")}Iets anders</span></div></section>'
            f'<section><h2 class="q">Dit vind ik niet leuk</h2><div class="chips">{"".join(chip(t, o, True) for t, o in dis)}<span class="chip add">{ic("plus")}Iets anders</span></div>'
            f'<p class="warnl">{ic("alert")}<span>Bij ‘Iets anders’: typ één woord, zoals ‘paardrijden’. Typ geen namen (ook niet je eigen naam), adres of telefoonnummer.</span></p></section></div>')
    foot = f'<a class="btn" aria-label="Vorige">{ic("left")}</a><span class="sp" style="flex:1"></span><a class="btn pri">Door naar wereld 3{ic("right")}</a>'
    return mob(card, 2, "Hobby’s", foot, "Even uitrusten op het eiland. Waar word jij blij van?", lsize=70, gone=5, prog=qprog(30, 2, 5, "Pauze · 30 van 60 klaar"))

STORY = ("Jij bent een rustige maker met een groot hart. Je ziet snel als iemand zich niet fijn voelt, en dan help je. "
         "Je bent graag met je handen bezig en je wilt precies weten hoe iets werkt. "
         "Het liefst doe je iets waar anderen, of dieren, blij van worden. "
         "Je voelt je thuis in een klein groepje waar het rustig en eerlijk is.")
TILES = [("Zo ben jij", "Zorgzaam en precies"), ("Dit doe je graag", "Maken en uitzoeken"), ("Dit vind je belangrijk", "Anderen helpen"), ("Hier voel je je thuis", "Klein groepje, rustig")]

def p4():
    mt = "".join(f'<div class="mt"><small>{a}</small><b>{b}</b></div>' for a, b in TILES)
    jobs = "".join(f'<span>{ic("star")}{t}</span>' for t in ["Dierenartsassistent", "Fietsenmaker", "Laborant", "Kok"])
    card = (f'<p class="world">Klaar · 60 van 60</p><h1>Dit ben jij!</h1>'
            f'<p class="storyp">{STORY}</p><div class="mt4">{mt}</div>'
            f'<p class="sect">Je houdt van</p><div class="chips">{chip("Tekenen")}{chip("Dieren")}{chip("Fietsen repareren")}{chip("Koken of bakken")}</div>'
            f'<p class="sect">Beroepen om eens te bekijken</p><div class="jobc">{jobs}</div>'
            f'<p class="seen">{ic("users")}<span>Je leraar kan dit verhaal ook zien, met jouw code. Niemand anders.</span></p>')
    foot = f'<a class="btn" aria-label="Opslaan als PDF">{ic("dl")}PDF</a><a class="btn pri">Check je droombaan{ic("right")}</a>'
    return mob(card, 11, "Dit ben jij", foot, "Kijk, mijn nieuwe schaal glimt! En dit ben jij.", lsize=96, gone=10, new_shell=True)

def p5():
    have = ["Je bent zorgzaam, voor mensen én dieren", "Je wilt weten hoe iets werkt", "Je werkt precies en netjes"]
    learn = ["Biologie en scheikunde goed leren", "Rustig blijven als een dier pijn heeft"]
    hv = "".join(f'<li>{ic("check")}<span>{t}</span></li>' for t in have)
    lr = "".join(f'<li>{ic("book")}<span>{t}</span></li>' for t in learn)
    card = (f'<p class="world">Droombaan-checker</p><h1>Wat wil jij later worden?</h1>'
            f'<div class="body" style="gap:10px"><div class="dj">{ic("search")}Dierenarts<span class="sp" style="flex:1"></span>{ic("x")}</div>'
            f'<div class="fitm"><span class="meter">{"".join("<i class=on></i>" if k<3 else "<i></i>" for k in range(5))}</span><b>3 van 5 heb je al!</b></div>'
            f'<section><h2 class="q">Dit heb je al</h2><ul class="hl">{hv}</ul></section>'
            f'<section><h2 class="q">Wat heb je nodig?</h2><ul class="hl lr">{lr}</ul>'
            f'<ol class="steps4" style="margin-top:12px">'
            f'<li class="now"><span>1</span><div><b>Nu: klas 2</b><small>Kies straks biologie en scheikunde</small></div></li>'
            f'<li><span>2</span><div><b>Havo of vwo afmaken</b><small>Zit je op vmbo? Via mbo Dierenartsassistent kan je ook verder.</small></div></li>'
            f'<li><span>3</span><div><b>Diergeneeskunde studeren</b><small>Universiteit Utrecht · 6 jaar</small></div></li>'
            f'<li class="goal"><span>{ic("star")}</span><div><b>Dierenarts!</b><small>In een praktijk, dierentuin of op de boerderij</small></div></li></ol></section>'
            f'<p class="cheer">{ic("star")}<span>Het is een lange reis, maar jij hebt al een goede start. Een kreeft groeit ook stap voor stap. Vraag je leraar wat je nu al kunt doen.</span></p></div>')
    foot = f'<a class="btn">Andere baan</a><a class="btn pri">{ic("dl")}Bewaar als PDF</a>'
    return mob(card, 0, "Droombaan", foot, "Dierenarts? Wauw! Jij hebt al een zorgzaam hart.", lsize=84, gone=10, new_shell=True)

def rail_c(q):
    items = [("done", "Start", "Code ingevoerd"),
             ("done", "Het koraalrif", "Hoe ben jij? · 15 vragen"),
             ("done", "De schatgrot", "Wat doe je graag? · 15 vragen"),
             ("done", "Pauze-eiland", "Wat vind je leuk?"),
             ("now", "De vuurtoren", f"Vraag {q} van 60"),
             ("todo", "De lagune", "Waar voel je je thuis? · 15"),
             ("end", "Dit ben jij", "Jouw verhaal + droombaan")]
    h = '<ol class="route">'
    for k, (s, t, sub) in enumerate(items):
        if s == "done": h += f'<li class="st done"><span class="dot">{ic("check")}</span><div><b>{t}</b><small>{sub}</small></div></li>'
        elif s == "now": h += f'<li class="st now" aria-current="step"><span class="mk">{lobster(30, 6)}</span><div><b>{t}</b><small>{sub}</small></div></li>'
        elif s == "todo": h += f'<li class="st todo"><span class="dot">4</span><div><b>{t}</b><small>{sub}</small></div></li>'
        else: h += f'<li class="st end todo"><span class="dot">{ic("sun")}</span><div><b>{t}</b><small>{sub}</small></div></li>'
    h += '</ol>'
    segs = "".join(f'<i class="{"off" if k < 6 else ""}"></i>' for k in range(10))
    return (f'<aside class="card rail"><h2>Jouw reis</h2><p class="cnt">Wereld 3 van 4 · ± 8 min</p>{h}'
            f'<div class="layers"><span class="segs">{segs}</span><span>6 van 10 eraf</span></div>'
            f'<p class="saved">{ic("check")}Alles is bewaard. Pauze mag altijd.</p></aside>')

def c1():
    pieces = [("on", "Zorgzaam", "zo ben jij"), ("on", "Maker", "dit doe je graag"), ("half", "Helpen?", "belangrijk"), ("", "?", "thuis")]
    bar = "".join(f'<i class="{"on" if k<8 else ""}"></i>' for k in range(15))
    card = (f'<p class="world">Wereld 3 · De vuurtoren</p><h1 style="margin-top:4px">Wat vind je belangrijk?</h1>'
            f'<p class="qn">Vraag 8 van 15<span class="bar">{bar}</span></p>'
            f'<div class="qrow"><p class="qtext">Ik wil later iets doen waar de wereld beter van wordt.</p>'
            f'<span class="ib on" aria-expanded="true" aria-label="Voorbeeld bij deze vraag">{ic("info")}</span></div>'
            f'<div class="ex"><b>Stel je voor…</b>Je school houdt een actie voor de voedselbank. Jij wilt meteen meedoen, want je vindt het fijn als iedereen genoeg te eten heeft.<span class="x">{ic("x")}</span></div>'
            f'{kik(5)}{puzzle(pieces, 2)}'
            f'<div class="foot"><a class="btn">{ic("left")}Terug</a><span class="sp"></span><a class="btn pri">Volgende{ic("right")}</a></div>')
    lz = (f'<div class="lz"><p class="bubble">Ik zie je steeds beter. Jij bent een echte helper!</p>{lobster(200, 6, False, None, "Lobsy de kreeft")}<span class="shadow"></span></div>')
    body = f'<main class="stage">{rail_c(38)}<section class="card step">{card}</section>{lz}</main>'
    return page("Vraag", body, 7, "c")

def a4():
    mt = "".join(f'<div class="mt"><small>{a}</small><b>{b}</b></div>' for a, b in TILES)
    return (f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><style>{CSS}</style></head><body class="a4"><div class="a4p">'
            f'<div class="a4h"><img src="{LOGO}" alt=""><div><b>Mijn ontdekkingsreis</b><small>Lobsy voor scholen · 29-09-2026</small></div>'
            f'<div class="r">Klas 2B · Voorbeeld College Westland<br><b>K7Q-M2P</b></div></div>'
            f'<p class="nm">Naam (vul zelf in)<span></span></p>'
            f'<div class="a4b"><div><h2 style="margin-top:0">Dit ben jij</h2><p class="storyp">{STORY}</p><div class="mt4">{mt}</div>'
            f'<h2>Je houdt van</h2><div class="chips">{chip("Tekenen")}{chip("Dieren")}{chip("Fietsen repareren")}{chip("Koken of bakken")}</div>'
            f'<h2>Niet zo leuk vind je</h2><div class="chips">{chip("Voor de klas praten")}{chip("Lang stilzitten")}</div>'
            f'<h2>Beroepen om eens te bekijken</h2><div class="jobc">{"".join(f"<span>{t}</span>" for t in ["Dierenartsassistent", "Fietsenmaker", "Laborant", "Kok"])}</div></div>'
            f'<div><div style="display:flex;justify-content:center">{lobster(150, 10, True, None, "")}</div>'
            f'<h2>Mijn droombaan: dierenarts</h2><ol class="steps4">'
            f'<li class="now"><span>1</span><div><b>Nu: klas 2</b><small>Biologie + scheikunde kiezen</small></div></li>'
            f'<li><span>2</span><div><b>Havo/vwo</b><small>of vmbo → mbo 4 Dierenartsassistent</small></div></li>'
            f'<li><span>3</span><div><b>Diergeneeskunde</b><small>Universiteit Utrecht · 6 jaar</small></div></li>'
            f'<li class="goal"><span>{ic("star")}</span><div><b>Dierenarts!</b></div></li></ol>'
            f'<p class="cheer">{ic("star")}<span>Jij hebt al een goede start. Stap voor stap kom je er.</span></p></div></div>'
            f'<div class="a4f"><span>Lobsy bewaart geen namen. Deze PDF is voor jou en je leraar.</span><span>Pagina 1 van 1</span></div>'
            f'</div>{SPRITE}<span class="demob">Voorbeelddata</span></body></html>')

SCREENS = [  # name, fn, viewport kind, full_page
    ("sc-p1-leerling-inloggen", p1, "m", False),
    ("sc-p2-leerling-vraag-info", p2, "m", False),
    ("sc-p3-leerling-hobbys-niet-leuk", p3, "m", True),
    ("sc-p4-leerling-dit-ben-jij", p4, "m", True),
    ("sc-p5-leerling-droombaan-checker", p5, "m", True),
    ("sc-p6-leerling-pdf", a4, "a4", True),
    ("sc-p7-chromebook-vraag", c1, "c", False),
]
