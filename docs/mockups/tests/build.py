"""Kandidaat-TESTS redesign (fase 1): volledige testpagina's in de stijl van De ontdekkingsreis.
Hergebruikt tokens, scene, kreeft (JourneyLobster) en bubbel uit ../ontdekkingsreis/build.py.
Metafoor: duiken. Elke test heeft dieptes: Eerste indruk (5) · Iets dieper (10) · Heel diep (25, Cultuur 18)
en daaronder, betaald, 'De bodem' = de uitgebreide test (150/200). Scene wordt donkerder naarmate je dieper gaat.
Run: python3 ts_build.py  (playwright + /usr/bin/google-chrome). Alle data = Voorbeelddata."""
import pathlib, importlib.util, random
from playwright.sync_api import sync_playwright
d = pathlib.Path(__file__).parent
_spec = importlib.util.spec_from_file_location("ob", d / "base" / "ontdekkingsreis_build.py")
ob = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(ob)
P = ob.P
P.update(dict(
 antenna='<path d="M7 20a5 5 0 0 1 10 0z"/><path d="M10 15.4C9.4 10.5 7.3 6.4 3.5 3.5M14 15.4c.6-4.9 2.7-9 6.5-11.9"/>',
 claw='<path d="M4 21l5.5-5.5"/><path d="M9.5 15.5C8 10.5 11 5 17 3.5c.6 3-1.2 5.8-4.2 6.8"/><path d="M9.5 15.5c4.6 1.3 9.6-.8 11-5.2-3-.9-6 .1-7.7 2.3"/>',
 star='<path d="m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z"/>',
 file='<path d="M14 3H6a1 1 0 0 0-1 1v16a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V8z"/><path d="M14 3v5h5M9 13h6M9 17h4"/>',
 x='<path d="M6 6l12 12M18 6 6 18"/>',
 down='<path d="M12 4v15M6 13l6 6 6-6"/>',
 bars='<path d="M5 20V11M12 20V5M19 20v-7"/>',
 users='<path d="M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM2 21a7 7 0 0 1 14 0M17 3.5a4 4 0 0 1 0 7.5M22 21a7 7 0 0 0-4-6.3"/>',
 card='<rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 10h18M7 15h4"/>',
 alert='<path d="M12 9v4M12 17h.01"/><path d="M10.3 3.9 2.4 18a2 2 0 0 0 1.7 3h15.8a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z"/>',
 pause='<rect x="6" y="5" width="4" height="14" rx="1"/><rect x="14" y="5" width="4" height="14" rx="1"/>',
 retry='<path d="M20 11a8 8 0 1 0-2.3 5.7"/><path d="M20 4v7h-7"/>',
))
ic = ob.ic; lobster = ob.lobster

# ---------------------------------------------------------------- voorbeelddata
TEST = "Beroepen"; TQ = "Wat vind je leuk?"; TOTAL = 25; DEEP_N = 200
LEVELS = [(5, "Eerste indruk", "Gedaan in de ontdekkingsreis"), (10, "Iets dieper", "+ 5 vragen · ± 1 min"), (25, "Heel diep", "De hele test · ± 4 min")]
TOPICS = [("Nieuwe dingen proberen", 30), ("Afmaken en netjes werken", 30), ("Energie van mensen", 30), ("Samen en aardig", 30), ("Kalm blijven", 30)]

CSS = ob.CSS + '''
body.f .scene{bottom:0}body.f .stage{padding-bottom:32px}body.f .mfoot{bottom:0}body.f .mstage{padding-bottom:96px}
.stage{grid-template-columns:272px 640px 1fr}
.rail .route.dv2::before{background:linear-gradient(180deg,var(--sea-1),var(--sea-4) 45%,var(--sea-7) 80%,var(--sea-9))}
.st.lock .dot{background:var(--gold-soft);border-color:var(--gold);color:var(--gold-ink)}.st.lock b{color:var(--gold-ink)}
.st.goal small{color:var(--brand);font-weight:600}
.cnt2{font-size:var(--text-xs);color:var(--muted);margin-top:10px;display:flex;align-items:center;gap:6px}.cnt2 .i{width:14px;height:14px}
/* depth ruler in the scene */
.dz{position:relative;align-self:stretch;min-height:700px}
.ruler{position:absolute;inset-inline-end:36px;top:24px;bottom:36px;width:2px;background:var(--surface);opacity:.35;border-radius:2px}
.tick{position:absolute;inset-inline-end:28px;display:flex;align-items:center;gap:8px;transform:translateY(-50%);font-size:var(--text-xs);font-weight:600;color:var(--surface)}
.tick i{width:18px;height:2px;background:var(--surface);opacity:.7;display:block}
.tick.on{color:var(--gold-light)}.tick.on i{background:var(--gold-light);opacity:1}
.tick.lock{color:var(--gold-light);opacity:.8}
.dz .lobw{position:absolute;inset-inline-end:150px;display:flex;flex-direction:column;align-items:flex-end;gap:10px}
.dz .bubble{max-width:280px;font-size:var(--text-md)}
.dz .lob{animation:bob 6s ease-in-out infinite}
.dz .line{position:absolute;inset-inline-end:240px;top:0;width:2px;border-inline-start:2px dotted var(--surface);opacity:.35}
@media (prefers-reduced-motion: reduce){.dz .lob{animation:none}}
/* question */
.qn .bar{position:relative}.qn .bar i.lv{background:var(--accent-soft)}.qn .bar i.goal{box-shadow:inset 0 0 0 1px var(--border)}
.lvl{display:flex;gap:6px;flex-wrap:wrap;margin-top:8px}
.lvl span{font-size:var(--text-xs);color:var(--muted);display:inline-flex;align-items:center;gap:4px}.lvl span b{color:var(--brand)}.lvl .i{width:12px;height:12px}
.prevh{display:flex;align-items:center;justify-content:space-between;margin-top:18px;font-size:var(--text-sm);font-weight:600;color:var(--text)}
.prevh .lnk{min-height:32px}
.prev{margin-top:6px}
.ex{margin-top:12px;border-radius:var(--radius-sm);background:var(--bg);padding:10px 14px;font-size:var(--text-sm)}
.ex summary{font-weight:600;color:var(--brand);display:flex;align-items:center;gap:6px;list-style:none;min-height:28px}.ex summary .i{width:16px;height:16px}
.ex p{margin-top:4px;color:var(--text)}
.tt span.todo{background:var(--bg)}
.tt small{font-size:inherit;font-weight:400;opacity:.85}
/* intro + offer */
.facts2{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.f2{display:flex;gap:10px;align-items:flex-start;border-radius:var(--radius-sm);background:var(--bg);padding:10px 12px;font-size:var(--text-sm);line-height:1.35}
.f2 .i{width:18px;height:18px;color:var(--brand);margin-top:1px}.f2 b{display:block;font-weight:600}.f2 small{display:block;color:var(--muted);font-size:var(--text-xs)}
.dv.done{background:var(--success-soft);border-color:var(--success)}.dv.done b{color:var(--success)}.dv.done .rd{border:0;display:flex;align-items:center;justify-content:center;color:var(--success)}
.dv.done .rd .i{width:16px;height:16px;stroke-width:2.6}
.dv.gold{border-color:var(--gold);background:var(--gold-soft)}.dv.gold b{color:var(--gold-ink)}
.q2{font-size:var(--text-lg);font-weight:600;color:var(--brand)}
.offer{display:flex;flex-direction:column;gap:6px}
.offer li{display:flex;gap:10px;align-items:flex-start;font-size:var(--text-sm);line-height:1.4;padding:4px 0}.offer li .i{width:18px;height:18px;color:var(--gold-deep);margin-top:1px}
.price{display:flex;align-items:center;gap:14px;border:1px solid var(--gold);background:var(--gold-soft);border-radius:var(--radius);padding:12px 16px}
.price .amt{font-size:var(--text-2xl);font-weight:700;color:var(--gold-ink);line-height:1}
.price small{display:block;font-size:var(--text-xs);color:var(--gold-ink)}
.price .sp{flex:1}
.pm{display:flex;gap:6px;flex-wrap:wrap}.pm span{font-size:var(--text-xs);font-weight:600;border:1px solid var(--border);background:var(--surface);border-radius:6px;padding:2px 8px;color:var(--text)}
.check{display:flex;gap:10px;align-items:flex-start;font-size:var(--text-sm);line-height:1.4}.check .box{margin-top:2px}
.btn.gold{background:var(--gold-light);border-color:var(--gold);color:var(--gold-ink)}
.tease{display:flex;gap:12px;align-items:center;border:1px dashed var(--gold);border-radius:var(--radius);padding:12px 14px;background:var(--surface)}
.tease .ico{width:40px;height:40px;border-radius:10px;background:var(--gold-soft);color:var(--gold-ink);display:flex;align-items:center;justify-content:center;flex:none}
.tease b{display:block;font-size:var(--text-sm);font-weight:600}.tease small{display:block;font-size:var(--text-xs);color:var(--muted)}.tease .sp{flex:1}
.next2{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.nx{border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 12px;display:flex;gap:10px;align-items:center;min-height:56px}
.nx .i{color:var(--brand)}.nx b{display:block;font-size:var(--text-sm);font-weight:600}.nx small{display:block;font-size:var(--text-xs);color:var(--muted)}
/* checkout */
.stat{width:56px;height:56px;border-radius:50%;display:flex;align-items:center;justify-content:center;flex:none}
.stat .i{width:28px;height:28px;stroke-width:2.4}
.stat.ok{background:var(--success-soft);color:var(--success)}.stat.bad{background:var(--warn-soft);color:var(--warn)}.stat.wait{background:var(--accent-soft);color:var(--brand)}
.sthead{display:flex;gap:16px;align-items:center}
.rcpt{display:grid;grid-template-columns:auto 1fr;gap:4px 16px;font-size:var(--text-sm);background:var(--bg);border-radius:var(--radius-sm);padding:12px 14px}.rcpt dt{color:var(--muted)}.rcpt dd{margin:0;font-weight:600}
.spin{width:28px;height:28px;border-radius:50%;border:3px solid var(--accent-soft);border-top-color:var(--brand)}
/* mobile */
.mdive{position:sticky;top:56px;z-index:35;background:var(--surface);padding:8px 14px 10px;border-bottom:1px solid var(--pearl)}
.mdive .t{display:flex;align-items:center;gap:6px;font-size:var(--text-sm)}.mdive .t .sp{flex:1}.mdive .ok{display:inline-flex;align-items:center;gap:4px;color:var(--success);font-size:var(--text-xs);font-weight:600}.mdive .ok .i{width:13px;height:13px}
.mdive .trk{display:flex;gap:3px;margin-top:8px}.mdive .trk i{height:6px;border-radius:6px;background:var(--pearl);display:block}.mdive .trk i.on{background:var(--brand)}.mdive .trk i.lv{background:var(--accent-soft)}
.mdive .ticks{display:flex;justify-content:space-between;font-size:var(--text-xs);color:var(--muted);margin-top:4px}
body.m .facts2,body.m .next2{grid-template-columns:1fr}
body.m .dive{grid-template-columns:1fr}
body.m .price{flex-wrap:wrap}
body.m .qtext{font-size:var(--text-lg)}
body.m .cdemo+.eb{padding-inline-end:104px}
body.m .tease{flex-wrap:wrap}body.m .tease .sp{display:none}body.m .tease>div{flex:1}body.m .tease .lnk{margin-inline-start:52px}
body.m .bn{grid-template-columns:repeat(5,1fr)}body.d .bn{grid-template-columns:repeat(5,1fr)}
'''

def nav(m):
    it = [("compass", "De ontdekkingsreis", "Reis"), ("book", "Mijn Paspoort", "Paspoort"), ("sun", "Carrière", "Carrière"),
          ("pin", "Banenkaart", "Banenkaart"), ("clip", "Sollicitaties", "Sollicitaties")]  # Dennis' volgorde (losse add-on)
    return ('<nav class="bn" aria-label="Hoofdmenu">' + "".join(
        f'<a class="{"on" if t=="Mijn Paspoort" else ""}"{" aria-current=page" if t=="Mijn Paspoort" else ""}>{ic(i)}{(ms if m else t)}</a>' for i, t, ms in it) + '</nav>')

def page(title, body, depth, mobile, focus=False, extra=""):
    sc = ob.scene(depth, 390, 170, 60) if mobile else ob.scene(depth, 1440, 836 if focus else 772, 1180)
    cls = ("m" if mobile else "d") + (" f" if focus else "")
    return (f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
            f'<title>{title} · Mijn tests · Lobsy</title><style>{CSS}</style></head><body class="{cls}">{ob.SPRITE}'
            f'{ob.header(mobile)}{sc}{body}{extra}{"" if focus else nav(mobile)}</body></html>')

# ---------------------------------------------------------------- dive rail (tekst-versie van de diepte)
def dive_rail(answered, target=25, deep=None, done_all=False, bodem="lock"):
    h = '<ol class="route dv2" aria-label="Jouw duik in de Beroepentest, van de oppervlakte naar de bodem">'
    h += f'<li class="st done"><span class="dot">{ic("check")}</span><div><b>Aan de oppervlakte</b><small>Start</small></div></li>'
    h += '<li class="zone" aria-hidden="true">Gratis · de hele test</li>'
    prev = 0
    for n, t, sub in LEVELS:
        if done_all or answered >= n:
            h += f'<li class="st done"><span class="dot">{ic("check")}</span><div><b>{t}</b><small>{n} vragen · gedaan</small></div></li>'
        elif prev <= answered < n:
            h += (f'<li class="st now" aria-current="step"><span class="mk">{lobster(30, 10, True)}</span><div><b>{t}</b>'
                  f'<small>Vraag {answered+1} van {n}</small></div></li>')
        else:
            goal = n == target
            h += f'<li class="st todo{" goal" if goal else ""}"><span class="dot">{n}</span><div><b>{t}</b><small>{"Jouw doel · " if goal else ""}{n} vragen</small></div></li>'
        prev = n
    h += '<li class="zone" aria-hidden="true">De bodem · uitgebreid</li>'
    if deep:
        for k, (t, n) in enumerate(TOPICS):
            st = deep[k]
            if st == "done": h += f'<li class="st done"><span class="dot">{ic("check")}</span><div><b>{t}</b><small>{n} vragen · gedaan</small></div></li>'
            elif st == "now": h += f'<li class="st now" aria-current="step"><span class="mk">{lobster(30, 10, True)}</span><div><b>{t}</b><small>Vraag 22 van {n}</small></div></li>'
            else: h += f'<li class="st todo"><span class="dot">{k+1}</span><div><b>{t}</b><small>{n} vragen</small></div></li>'
    else:
        sub = {"lock": f"{DEEP_N} vragen · rapport · € 2,99", "open": "Betaald · klaar om te duiken", "offer": f"{DEEP_N} vragen · rapport"}[bodem]
        h += f'<li class="st todo lock"><span class="dot">{ic("lock" if bodem != "open" else "check")}</span><div><b>Uitgebreide test</b><small>{sub}</small></div></li>'
    h += '</ol>'
    if deep:
        cnt = "Uitgebreide test Competenties"; foot = "51 van 150 vragen · ± 17 min nog"
    else:
        cnt = f"Beroepen · {min(answered, TOTAL)} van {TOTAL} vragen"; foot = "Heel diep gedaan · 25 van 25" if done_all else f"Nog {target - answered} vragen · ± {max(1, (target-answered)//5)} min"
    return (f'<aside class="card rail"><h2>Jouw duik</h2><p class="cnt">{cnt}</p>{h}'
            f'<p class="cnt2">{ic("clock")}{foot}</p><p class="saved">{ic("check")}Alles is bewaard. Stoppen mag altijd.</p></aside>')

def depth_zone(frac, bubble, marks=None, gone=10, new_shell=True, size=190, falling=None, label="Lobsy de kreeft duikt"):
    """Scene column: depth ruler with ticks (5 / 10 / 25 / bodem) + lobster at depth fraction."""
    H = 700
    marks = marks or [("Eerste indruk", .16, "on"), ("Iets dieper", .30, ""), ("Heel diep", .56, ""), ("De bodem", .92, "lock")]
    t = "".join(f'<span class="tick {c}" style="top:{24 + f*(H-60):.0f}px">{n}<i></i></span>' for n, f, c in marks)
    y = 24 + frac * (H - 60)
    lob = lobster(size, gone, new_shell, falling, label)
    return (f'<div class="dz" aria-hidden="true"><span class="ruler"></span>{t}<span class="line" style="height:{max(0, y-size*.9):.0f}px"></span>'
            f'<div class="lobw" style="top:{max(0, y - size*.55 - 70):.0f}px"><p class="bubble">{bubble}</p>{lob}</div></div>')

def likert(sel=None):
    return ob.likert(sel)

def seg(answered, total=25, marks=(5, 10)):
    out = ""
    for k in range(total):
        c = "on" if k < answered else ("lv" if k < 10 else "")
        out += f'<i class="{c}"></i>'
    return out

# ---------------------------------------------------------------- cards
def intro_card(m=False):
    dv = [("Eerste indruk", "5 vragen · gedaan", "done", 1), ("Iets dieper", "10 vragen · + 1 min", "", 2), ("Heel diep", "25 vragen · + 4 min", "on", 5)]
    d = "".join(f'<label class="dv {c}" role="radio" aria-checked="{"true" if c=="on" else "false"}"{" aria-disabled=true" if c=="done" else ""}>'
                f'<b><span class="rd">{ic("check") if c=="done" else ""}</span>{a}</b><small>{b}</small>'
                f'<span class="dd" aria-hidden="true">{"".join("<i class=on></i>" if k<n else "<i></i>" for k in range(5))}</span></label>' for a, b, c, n in dv)
    facts = [("check", "Je deed er al 5", "In de ontdekkingsreis. Die tellen mee."), ("clock", "Nog 20 vragen", "± 4 minuten. Stoppen mag altijd."),
             ("wave", "Geen foute antwoorden", "Kies wat het eerst in je opkomt."), ("eyeoff", "Alleen voor jou", "Werkgevers zien je antwoorden niet.")]
    f = "".join(f'<div class="f2">{ic(i)}<div><b>{a}</b><small>{b}</small></div></div>' for i, a, b in facts)
    return (f'<p class="eb">Mijn tests · Beroepen</p><h1>{TQ}</h1>'
            f'<p class="lead">25 korte zinnen over werk dat je leuk vindt. Zo vinden we beroepen die bij je passen.</p>'
            f'<div class="body"><div class="facts2">{f}</div>'
            f'<fieldset><legend class="q2">Hoe diep wil je duiken?</legend><p class="qs">Dieper geeft een scherper beeld. Je kunt ook later verder.</p><div class="dive">{d}</div></fieldset>'
            + ('' if m else f'<p class="hint">{ic("book")}Wat meet deze test? <a class="lnk" style="min-height:0">Lees de uitleg</a></p>') + '</div>'
            + ('' if m else f'<div class="foot"><a class="btn">{ic("left")}Mijn tests</a><span class="sp"></span><a class="btn pri">Ga verder bij vraag 6{ic("right")}</a></div>'))

PREV = [("Ik help graag mensen die het even moeilijk hebben.", "5"), ("Ik ben graag buiten aan het werk.", "2"), ("Ik bedenk graag hoe iets er mooier uit kan zien.", "4")]
def question_card(m=False):
    pv = "".join(f'<li>{ic("check")}<span>{t}</span><b>{a}</b></li>' for t, a in PREV[: (1 if m else 3)])
    return (f'<p class="eb">In de diepte · Beroepen · Iets dieper</p>'
            f'<h1 style="margin-top:6px">{TQ}</h1><p class="lead">Hoe goed past deze zin bij jou? Er zijn geen foute antwoorden.</p>'
            f'<p class="qn">Vraag 7 van 25<span class="bar">{seg(6)}</span></p>'
            f'<p class="lvl"><span>{ic("check")}<b>Eerste indruk</b> gedaan</span><span>· nog 4 tot <b>Iets dieper</b></span></p>'
            f'<p class="qtext">“Ik vind het leuk om uit te zoeken hoe een apparaat werkt.”</p>{likert(None)}'
            f'<p class="prevh">Beantwoord (6)<a class="lnk">Aanpassen</a></p><ul class="prev" aria-label="Beantwoorde vragen">{pv}</ul>'
            + ('' if m else f'<div class="foot"><a class="btn" aria-label="Vorige vraag">{ic("left")}Terug</a><span class="sp"></span><a class="lnk">Later verder</a><a class="btn pri">Volgende{ic("right")}</a></div>'))

def deep_card(m=False):
    tabs = '<div class="tt" aria-label="Onderwerpen">' + "".join(
        f'<span class="{c}">{ic("check") if c=="done" else ""}{t}{"" if m else f" <small>{s}</small>"}</span>' for t, c, s in
        [("Nieuwe dingen", "done", "30/30"), ("Afmaken", "now", "21/30"), ("Energie", "todo", "0/30"), ("Samen", "todo", "0/30"), ("Kalm", "todo", "0/30")]) + '</div>'
    bar = "".join(f'<i class="{"on" if k<51 else ""}" style="flex:1"></i>' for k in range(0, 150, 6))
    return (f'<p class="eb">De bodem · Competenties · onderwerp 2 van 5</p>{tabs}'
            f'<h1 style="margin-top:10px">Afmaken en netjes werken</h1><p class="lead">Hoe goed past deze zin bij jou? Kies wat het eerst in je opkomt.</p>'
            f'<p class="qn">Vraag 52 van 150<span class="bar">{bar}</span></p>'
            f'<p class="qtext">“Ik raak het overzicht kwijt als er veel taken tegelijk openstaan.”</p>'
            f'<details class="ex" open><summary>{ic("book")}Voorbeeld uit de praktijk</summary><p>Je hebt drie klussen tegelijk open. Een klant belt. Weet je daarna nog waar je was?</p></details>'
            f'{likert(None)}'
            f'<p class="deeper">{ic("pause")}Na dit onderwerp komt een rustpunt. Stoppen mag: alles is bewaard.</p>'
            + ('' if m else f'<div class="foot"><a class="btn" aria-label="Vorige vraag">{ic("left")}Terug</a><span class="sp"></span><a class="lnk">Later verder</a><a class="btn pri">Volgende{ic("right")}</a></div>'))

def done_card(m=False):
    g = "".join(f'<li>{ic("check")}{t}</li>' for t in ["Werk dat bij je past: helpen en zorgen", "Je houdt van: mensen verder helpen", "Minder jouw ding: veel alleen met machines"])
    pic = f'<div class="pic"><div class="fallen">{ob.shard(110 if not m else 80,-8)}<span>Heel diep · 25 van 25</span></div></div>'
    nx = (f'<div class="next2"><div class="nx">{ic("edit")}<div><b>Iets aanpassen?</b><small>Dat mag nog 3 keer</small></div></div>'
          f'<div class="nx">{ic("wave")}<div><b>Volgende test: Cultuur</b><small>Nog 13 vragen · ± 3 min</small></div></div></div>')
    tease = (f'<div class="tease"><span class="ico">{ic("down")}</span><div><b>Nog dieper? Tot de bodem.</b><small>De uitgebreide test: 200 vragen en een rapport van 9 pagina’s.</small></div>'
             f'<span class="sp"></span><a class="lnk">Bekijk wat je krijgt{ic("right")}</a></div>')
    return (f'<p class="eb">In de diepte · Beroepen · heel diep klaar</p>'
            f'<div class="shed" style="margin-top:14px">{pic if not m else ""}<div><h1 style="margin-top:0">Weer een laag eraf</h1>'
            f'<p class="lead">Beroepen is helemaal klaar. Dit staat nu in je paspoort:</p><ul class="gained">{g}</ul></div></div>'
            f'<div class="body" style="margin-top:16px">{nx}{tease}</div>'
            + ('' if m else f'<div class="foot"><a class="lnk">Terug naar Mijn tests</a><span class="sp"></span><a class="btn pri">Bekijk je uitslag{ic("right")}</a></div>'))

def offer_card(m=False):
    get = [("bars", "Je sterke kanten en groeipunten, per onderdeel"), ("users", "Wat elke score voor jou betekent, in gewone woorden"),
           ("compass", "Beroepen die bij je passen, met uitleg waarom"), ("file", "Een PDF van 9 pagina’s. Jij kiest met wie je hem deelt.")]
    gl = "".join(f'<li>{ic(i)}<span>{t}</span></li>' for i, t in get)
    return (f'<p class="eb">De bodem · uitgebreide test</p><h1>Duik tot de bodem</h1>'
            f'<p class="lead">200 vragen over werk dat je leuk vindt. Daarna krijg je een rapport, helemaal over jou.</p>'
            f'<div class="body"><section><h2 class="q">Wat krijg je?</h2><ul class="offer" style="margin-top:6px">{gl}</ul>'
            f'<a class="lnk">{ic("file")}Bekijk eerst een voorbeeld</a></section>'
            f'<div class="facts2"><div class="f2">{ic("clock")}<div><b>± 35 minuten</b><small>In stukjes. Alles wordt bewaard.</small></div></div>'
            f'<div class="f2">{ic("check")}<div><b>Je gratis uitslag blijft</b><small>Die 25 vragen blijven altijd van jou.</small></div></div></div>'
            f'<div class="price"><div><span class="amt">€ 2,99</span><small>Eenmalig · inclusief btw</small></div><span class="sp"></span>'
            f'<div><div class="pm"><span>iDEAL</span><span>Bancontact</span><span>Creditcard</span></div><small class="muted" style="color:var(--muted)">Veilig betalen via Mollie</small></div></div>'
            f'<label class="check"><span class="box"></span><span>Ik wil meteen beginnen. Ik weet dat ik dan niet meer binnen 14 dagen kan annuleren.</span></label></div>'
            + ('' if m else f'<div class="foot"><a class="lnk">Nu niet</a><span class="sp"></span><a class="btn pri">Naar betalen · € 2,99{ic("right")}</a></div>'))

def ok_card(m=False):
    return (f'<p class="eb">De bodem · betaling</p><div class="sthead" style="margin-top:12px"><span class="stat ok">{ic("check")}</span>'
            f'<div><h1 style="margin:0">Betaald. Je kunt beginnen</h1><p class="lead" style="margin-top:4px">Fijn! De uitgebreide Beroepentest staat voor je klaar.</p></div></div>'
            f'<div class="body"><dl class="rcpt"><dt>Wat</dt><dd>Uitgebreide test Beroepen</dd><dt>Bedrag</dt><dd>€ 2,99 (incl. btw)</dd><dt>Datum</dt><dd>wo 30 sep 2026, 08:41</dd><dt>Kenmerk</dt><dd>LB-DA-7F3K2</dd></dl>'
            f'<p class="hint">{ic("file")}Je krijgt de factuur ook per e-mail.</p>'
            f'<div class="facts2"><div class="f2">{ic("clock")}<div><b>200 vragen · ± 35 min</b><small>In 5 onderwerpen, met rustpunten.</small></div></div>'
            f'<div class="f2">{ic("check")}<div><b>Stoppen mag</b><small>Je gaat later verder waar je was.</small></div></div></div></div>'
            + ('' if m else f'<div class="foot"><a class="lnk">Later beginnen</a><span class="sp"></span><a class="btn pri">Begin met vraag 1{ic("right")}</a></div>'))

def fail_card(m=False):
    return (f'<p class="eb">De bodem · betaling</p><div class="sthead" style="margin-top:12px"><span class="stat bad">{ic("alert")}</span>'
            f'<div><h1 style="margin:0">De betaling is niet gelukt</h1><p class="lead" style="margin-top:4px">Er is niets afgeschreven. Dat kan gebeuren, bijvoorbeeld als je het scherm van je bank sloot.</p></div></div>'
            f'<div class="body"><ul class="offer"><li>{ic("retry")}<span>Probeer het nog een keer. Je kunt ook een andere manier kiezen.</span></li>'
            f'<li>{ic("check")}<span>Je gratis uitslag en je antwoorden zijn gewoon bewaard.</span></li>'
            f'<li>{ic("antenna")}<span>Is er toch geld afgeschreven? Tik op <b>Assistent</b>. Dan zoeken we het uit.</span></li></ul></div>'
            + ('' if m else f'<div class="foot"><a class="lnk">Terug naar de test</a><span class="sp"></span><a class="btn pri">Opnieuw betalen{ic("retry")}</a></div>'))

def wait_card():
    return (f'<p class="eb">De bodem · betaling</p><div class="sthead" style="margin-top:12px"><span class="stat wait"><span class="spin" aria-hidden="true"></span></span>'
            f'<div><h1 style="margin:0">We checken je betaling</h1><p class="lead" style="margin-top:4px" role="status">Dit duurt meestal een paar seconden.</p></div></div>'
            f'<div class="body"><p class="hint">{ic("clock")}Duurt het langer dan een minuut? Je mag deze pagina sluiten. We sturen je een mail als het klaar is.</p>'
            f'<p class="hint">{ic("check")}Je betaalt nooit twee keer voor dezelfde test.</p></div>')

# ---------------------------------------------------------------- desktop screens
def desk(rail_html, card, zone, depth, title, focus=False, extra=""):
    body = f'<main class="stage">{rail_html}<section class="card step">{card}</section>{zone}</main>'
    return page(title, body, depth, False, focus, extra)

def d1():
    z = depth_zone(.16, "Vijf vragen heb je al gedaan. Zullen we samen wat dieper gaan?")
    note = ('<p class="note"><b>Ontwerpnotitie</b> · Nieuw introscherm. Antwoorden uit de ontdekkingsreis en de gratis DNA-scan tellen mee: '
            'de knop begint bij de eerste open vraag. Niveaus en tellingen komen uit één regel (5 · 10 · 25, Cultuur 18).</p>')
    return desk(dive_rail(5), intro_card() , z, 8, "Beroepentest", extra=note)
def d2():
    z = depth_zone(.25, "Er zijn geen foute antwoorden. Kies wat het eerst in je opkomt.")
    note = ('<p class="note"><b>Ontwerpnotitie</b> · Eén vraag tegelijk, dezelfde component als in de ontdekkingsreis (08.1). '
            'Focusmodus: geen menu onderin. De 5 knoppen zijn één radiogroep (pijltjestoetsen); focus gaat na een antwoord naar de volgende vraag.</p>')
    return desk(dive_rail(6), question_card(), z, 9, "Vraag 7", focus=True, extra=note)
def d3():
    marks = [("Heel diep", .10, "on"), ("Nieuwe dingen", .28, "on"), ("Afmaken", .44, ""), ("Energie", .60, ""), ("Samen", .76, ""), ("Kalm · de bodem", .92, "lock")]
    z = depth_zone(.40, "Hier beneden is het stil. Neem je tijd, we gaan in stukjes.", marks=marks)
    note = ('<p class="note"><b>Ontwerpnotitie</b> · Uitgebreide test: ook één vraag tegelijk (nu 150 vragen tegelijk op de pagina). '
            'Rustpunt na elk onderwerp in de kaart, geen pop-up. “Voorbeeld uit de praktijk” klapt in de kaart open.</p>')
    return desk(dive_rail(25, deep=["done", "now", "", "", ""]), deep_card(), z, 10, "Uitgebreide test", focus=True, extra=note)
def d4():
    z = depth_zone(.56, "Voel je dat? Weer een laag eraf. Nu zie ik nog beter wat je leuk vindt.", marks=[("Eerste indruk", .16, "on"), ("Iets dieper", .30, "on"), ("Heel diep", .56, "on"), ("De bodem", .92, "lock")], falling=6)
    note = ('<p class="note"><b>Ontwerpnotitie</b> · Zelfde moment als ontdekkingsreis 08.3, maar voor de hele test. Het schaalstuk valt één keer (1,2 s); '
            'bij “minder beweging” staat het er meteen. Geen modal. Link naar de uitslag (/profiel/tests/career).</p>')
    return desk(dive_rail(25, done_all=True), done_card(), z, 9, "Beroepen klaar", extra=note)
def d5():
    z = depth_zone(.80, "Daar beneden ligt nog meer van jou. Kijken mag, het hoeft niet.", marks=[("Eerste indruk", .16, "on"), ("Iets dieper", .30, "on"), ("Heel diep", .56, "on"), ("De bodem", .92, "lock")])
    note = ('<p class="note"><b>Ontwerpnotitie</b> · Aanbod in plaats van één knop “Ontgrendel (€ 2,99)”. Prijs, btw, betaalmanieren en het afzien van de '
            'bedenktijd staan vóór de betaalknop. Gratis blijft altijd eerst en blijft gelden.</p>')
    return desk(dive_rail(25, done_all=True, bodem="offer"), offer_card(), z, 10, "Uitgebreide test", extra=note)
def d6():
    z = depth_zone(.86, "Gelukt! Zullen we samen naar de bodem duiken?", marks=[("Eerste indruk", .16, "on"), ("Iets dieper", .30, "on"), ("Heel diep", .56, "on"), ("De bodem", .92, "on")])
    note = ('<p class="note"><b>Ontwerpnotitie</b> · Terugkomst van Mollie. De status komt van de server (webhook of opvragen bij Mollie), '
            'niet van de URL. Drie staten: bezig (m6), gelukt (dit scherm), niet gelukt (d7).</p>')
    return desk(dive_rail(25, done_all=True, bodem="open"), ok_card(), z, 10, "Betaald")
def d7():
    z = depth_zone(.56, "Geen zorgen. Er is niets afgeschreven.", marks=[("Eerste indruk", .16, "on"), ("Iets dieper", .30, "on"), ("Heel diep", .56, "on"), ("De bodem", .92, "lock")])
    return desk(dive_rail(25, done_all=True, bodem="lock"), fail_card(), z, 9, "Betaling niet gelukt")

# ---------------------------------------------------------------- mobile screens
def mdive(label, answered, total=25, ticks=("5", "10", "25"), ok=True):
    if total > 50:  # uitgebreide test: 25 blokjes van 6 vragen
        trk = "".join(f'<i class="{"on" if k < answered else ""}" style="flex:1"></i>' for k in range(0, total, 6))
    else:
        trk = "".join(f'<i class="{"on" if k < answered else ("lv" if k < 10 else "")}" style="flex:1"></i>' for k in range(total))
    okh = f'<span class="ok">{ic("check")}Bewaard</span>' if ok else ''
    return (f'<div class="mdive" role="group" aria-label="Voortgang: {label}"><p class="t"><b>{label}</b><span class="sp"></span>{okh}</p>'
            f'<div class="trk">{trk}</div></div>')
def mob(card, foot, depth, title, bubble, top="", focus=False, size=72, new_shell=True, gone=10, falling=None):
    card = '<p class="demo cdemo">Voorbeelddata</p>' + card
    lob = f'<div class="mlob">{lobster(size, gone, new_shell, falling, "Lobsy de kreeft")}<p class="bubble">{bubble}</p></div>'
    body = f'{top}<main class="mstage">{lob}<section class="card step">{card}</section></main>' + (f'<div class="mfoot">{foot}</div>' if foot else '')
    return page(title, body, depth, True, focus)
def m1():
    return mob(intro_card(True), f'<a class="btn" aria-label="Terug naar Mijn tests">{ic("left")}</a><a class="btn pri">Ga verder bij vraag 6{ic("right")}</a>', 8,
               "Beroepentest", "5 vragen heb je al. Samen wat dieper?", top=mdive("Beroepen · 5 van 25", 5))
def m2():
    return mob(question_card(True), f'<a class="btn" aria-label="Vorige vraag">{ic("left")}</a><a class="lnk">Later verder</a><a class="btn pri">Volgende{ic("right")}</a>', 9,
               "Vraag 7", "Geen foute antwoorden. Kies wat eerst opkomt.", top=mdive("Vraag 7 van 25 · Beroepen", 6), focus=True)
def m3():
    return mob(deep_card(True), f'<a class="btn" aria-label="Vorige vraag">{ic("left")}</a><a class="lnk">Later verder</a><a class="btn pri">Volgende{ic("right")}</a>', 10,
               "Uitgebreide test", "Neem je tijd. We gaan in stukjes.", top=mdive("Vraag 52 van 150 · Afmaken", 51, 150), focus=True)
def m4():
    return mob(done_card(True), f'<a class="lnk">Mijn tests</a><a class="btn pri">Bekijk je uitslag{ic("right")}</a>', 9,
               "Beroepen klaar", "Voel je dat? Weer een laag eraf.", top=mdive("Beroepen · 25 van 25", 25), size=80, falling=6)
def m5():
    return mob(offer_card(True), f'<a class="lnk">Nu niet</a><a class="btn pri">Naar betalen · € 2,99</a>', 10,
               "Uitgebreide test", "Kijken mag, het hoeft niet.")
def m6():
    return mob(wait_card(), '', 10, "Betaling checken", "Even geduld. Ik kijk mee.")

D = [("ts-d1-test-intro", d1), ("ts-d2-vraag-likert", d2), ("ts-d3-vraag-uitgebreid", d3), ("ts-d4-test-klaar", d4),
     ("ts-d5-uitgebreid-aanbod", d5), ("ts-d6-checkout-gelukt", d6), ("ts-d7-checkout-mislukt", d7)]
M = [("ts-m1-test-intro", m1), ("ts-m2-vraag-likert", m2), ("ts-m3-vraag-uitgebreid", m3), ("ts-m4-test-klaar", m4),
     ("ts-m5-uitgebreid-aanbod", m5), ("ts-m6-checkout-bezig", m6)]

if __name__ == "__main__":
    with sync_playwright() as pw:
        b = pw.chromium.launch(executable_path="/usr/bin/google-chrome")
        pg = b.new_page(viewport={"width": 1440, "height": 900}, reduced_motion="reduce")
        for n, fn in D:
            f = d / f"{n}.html"; f.write_text(fn()); pg.goto(f.as_uri()); pg.wait_for_timeout(300)
            print(n, pg.evaluate(ob.CHECK), f"{f.stat().st_size//1024}kB"); pg.screenshot(path=str(d / f"{n}.png"))
        pm = b.new_page(viewport={"width": 390, "height": 844}, device_scale_factor=2, reduced_motion="reduce")
        for n, fn in M:
            f = d / f"{n}.html"; f.write_text(fn()); pm.goto(f.as_uri()); pm.wait_for_timeout(300)
            print(n, pm.evaluate(ob.CHECK), f"{f.stat().st_size//1024}kB")
            h = max(844, pm.evaluate("document.documentElement.scrollHeight")); pm.set_viewport_size({"width": 390, "height": h}); pm.wait_for_timeout(150)
            pm.screenshot(path=str(d / f"{n}.png")); pm.set_viewport_size({"width": 390, "height": 844})
        b.close()
