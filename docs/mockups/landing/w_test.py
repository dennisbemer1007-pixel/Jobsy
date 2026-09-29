"""Warm mini-test screens + mobile hero/result, both variants (zw = Werkgevers actief OFF)."""
from ui import ic, LOGO, LOBSTER, MASCOT, GOOGLE, MSFT, vb, page
from w_common import em, blob, mascot, ghost, wave
from w_hero import hero_scene_m
CK = ic("check")

def thdr():
    return f'''<header class="thdr"><div class="wrap"><a class="brand"><img src="{LOGO}" alt="">Lobsy</a><span class="pill" style="margin-inline-start:8px"><i class="e">🧪</i>Gratis test</span>
<div class="r"><span class="lang">{ic("globe")}NL{ic("chev")}</span><a class="btn sm">Inloggen</a></div></div></header>'''

BLOCKS = [("🤝", "peach", "Zo werk jij", "Samenwerken, doorzetten, rust als het druk is"), ("💛", "sun", "Dit vind je leuk", "Waar je energie van krijgt"),
          ("🏡", "mint", "Hier voel je je thuis", "Wat voor team en plek bij je past"), ("🧭", "sky", "Dit vind je belangrijk", "Wat jou drijft")]
FACES = ["😟", "😕", "😐", "🙂", "😄"]

def d2():
    bl = "".join(f'<div class="blk">{em(e, t)}<div><h4>{h}</h4><p>{p} · 5 vragen</p></div></div>' for e, t, h, p in BLOCKS)
    scene = (blob("var(--sun)", 30, 40, 360, 300, 0) + "".join(f'<span class="bb" style="left:{x}px;top:{y}px;width:{s}px;height:{s}px"></span>' for x, y, s in [(290, 130, 16), (318, 100, 10), (300, 70, 8), (60, 380, 12)])
             + mascot(100, 60, 220, "", "hm"))
    body = f'''{thdr()}<main class="tmain"><div class="wrap tgrid2"><div class="card tcard">
<span class="pill"><i class="e">👋</i>Gratis · geen account nodig</span><h1>Ontdek je werk-DNA</h1>
<p class="lead" style="font-size:var(--text-md)">20 korte vragen over jou. Er is geen goed of fout: kies gewoon wat je voelt. Na 3 minuutjes zie je: dit ben jij.</p>
<div class="facts"><span class="pill line"><i class="e">📝</i>20 vragen</span><span class="pill line"><i class="e">⏱️</i>± 3 minuten</span><span class="pill line"><i class="e">📱</i>Blijft op dit apparaat</span></div>
<div class="blocks">{bl}</div><div class="hr"></div>
<div class="chk"><span class="b on">{CK}</span><span>Ik ben 16 jaar of ouder.</span></div>
<div class="chk"><span class="b on">{CK}</span><span>Ik snap dat mijn antwoorden 7 dagen op dit apparaat blijven. Werkgevers zien ze nooit. <a style="color:var(--brand);font-weight:600">Meer over privacy</a></span></div>
<div style="display:flex;align-items:center;gap:16px;margin-top:24px"><a class="btn lg pri">Start de test {ic("arrow")}</a><span class="sm muted">Al een account? <a style="color:var(--brand);font-weight:600">Inloggen</a></span></div></div>
<aside class="side-scene"><div class="bubble" style="position:relative;white-space:normal;max-width:280px">Duik maar diep! <i class="e">🌊</i> Je kunt altijd terug.</div>
<div class="ss-art">{scene}</div>
<div class="after"><h4><i class="e">🎁</i> Wat krijg je na de test?</h4><p>Een eerste “Dit ben jij”: hoe je werkt, wat je leuk vindt, waar je je thuis voelt en wat je belangrijk vindt.</p></div></aside>
</div></main>'''
    return page(body)

def _lik():
    return "".join(f'<span class="{"sel" if n==4 else ""}"><i class="e">{FACES[n-1]}</i>{n}</span>' for n in range(1, 6))

def d3():
    prog = "".join(f'<div class="{"on" if n==1 else ""}"><b style="--w:{w}%"></b>{e} {h}</div>'.replace(f'{e} ', f'<i class="e">{e}</i> ') for n, (w, (e, _t, h, _p)) in enumerate(zip([100, 60, 0, 0], BLOCKS)))
    body = f'''{thdr()}<main class="tmain"><div class="wrap"><div class="qwrap">
<div class="qtop"><a>{ic("chevl")}Vorige</a><div class="sp"></div><span>Vraag 8 van 20 · goed bezig! <i class="e">💪</i></span><div class="sp"></div><a>Later verder</a></div>
<div class="prog">{prog}</div>
<div class="card qcard"><div class="blab">{em("💛", "sun")}<div><b>Dit vind je leuk</b><div class="xs muted">Vraag 3 van 5</div></div></div>
<h1>Ik help graag mensen of werk graag in een team met klanten.</h1>
<div class="lik">{_lik()}</div><div class="likl"><span>Past niet</span><span>Past wel</span></div>
<div class="tip"><img src="{MASCOT}" alt=""><span><b>Twijfel je?</b> Kies wat je het eerst voelt. Je antennes weten het vaak al.</span></div></div>
<div class="qfoot"><i class="e">🔒</i>Je antwoorden blijven op dit apparaat. Werkgevers zien ze nooit.</div>
</div></div></main>'''
    return page(body)

TILES = [("🤝", "peach", "Zo werk jij", "Samenwerker", "Je werkt graag samen en je maakt af wat je belooft.", ["Samenwerken", "Doorzetten"]),
         ("💛", "sun", "Dit vind je leuk", "Mensen helpen", "Je krijgt energie van werk met mensen en van dingen regelen.", ["Sociaal", "Ondernemend"]),
         ("🏡", "mint", "Hier voel je je thuis", "Een warm team", "Een plek waar mensen elkaar helpen en waar je zelf mag kiezen hoe.", ["Samen", "Mensen eerst"]),
         ("🧭", "sky", "Dit vind je belangrijk", "Zekerheid en zorg", "Je wilt iets betekenen voor anderen en weten waar je aan toe bent.", ["Zorgen voor", "Zekerheid"])]

def tiles_html():
    return "".join(f'<div class="card tile"><div class="ttop t-{t}">{em(e, "cream", "s")}<small>{s}</small></div><div class="tb2"><h3>{h}</h3><p>{p}</p><div class="chips">{"".join(f"<span class=pill>{c}</span>" for c in ch)}</div></div></div>' for e, t, s, h, p, ch in TILES)

def locked():
    return f'''<div class="card lockd"><div class="blurred"><div class="tabs"><span class="on">Mijn DNA</span><span>Mijn tests</span><span>Past deze baan?</span><span>Carrière</span><span>Bewijzen</span></div>
<div class="bar" style="grid-template-columns:120px 1fr;margin-top:14px">Samenwerken<b style="--w:86%"></b></div><div class="bar t2" style="grid-template-columns:120px 1fr">Doorzetten<b style="--w:74%"></b></div>
<div class="bar t3" style="grid-template-columns:120px 1fr">Rust onder druk<b style="--w:61%"></b></div></div>
<div class="ov"><span><i class="e">🔐</i>Je volledige paspoort · na gratis account</span></div></div>'''

def unlocks(zw):
    return ([("🧬", "Je volledige paspoort met Mijn DNA"), ("✅", "“Past dit beroep bij mij?” voor elk beroep"), ("🪜", "Je droombaan-check en je volgende stap"), ("🧭", "De Ontdekkingsreis, stap voor stap")] if zw else
            [("🧬", "Je volledige paspoort met Mijn DNA"), ("✅", "“Past deze baan?” bij elke vacature"), ("🗺️", "Banen op je reistijd-kaart"), ("⭐", "Jouw top-match in Match")])

def providers():
    return f'<div class="prov"><a class="btn">{GOOGLE}Doorgaan met Google</a><a class="btn">{MSFT}Doorgaan met Microsoft</a><a class="btn">{ic("mail")}Doorgaan met e-mail</a></div>'

def signup(zw):
    u = "".join(f'<li><i class="e">{e}</i>{t}</li>' for e, t in unlocks(zw))
    return f'''<aside class="card signup"><h2>Bewaar je DNA en zie je hele paspoort <i class="e">🎁</i></h2><p class="sm muted" style="margin-top:6px">Gratis. Je tests in je account zijn dan al klaar.</p>
<ul class="unl">{u}</ul>{providers()}<div class="carry">{CK}Je 20 antwoorden gaan mee</div>
<p class="xs muted" style="margin-top:12px">Al een account? <a style="color:var(--brand);font-weight:600">Inloggen</a></p></aside>'''

def rhead():
    return f'''<div class="rhead"><div style="display:flex;gap:8px"><span class="pill">Eerste indruk · op basis van 20 vragen</span>{vb()}</div>
<h1><span class="rh-art">{blob("var(--sun)", -14, -10, 110, 100, 1)}<img src="{MASCOT}" alt=""></span>Dit ben jij <i class="e" style="font-size:36px">🎉</i></h1>
<p class="lead" style="margin-top:8px">Wauw, wat een mooi begin! Dit is een eerste blik op je werk-DNA. Hoe meer je invult, hoe scherper het beeld.</p></div>'''

def d4(zw=False):
    body = f'''{thdr()}<main class="tmain"><div class="wrap rgrid"><div>{rhead()}
<div class="tiles">{tiles_html()}</div>{locked()}
<div class="rfoot"><a class="btn sm"><i class="e">💬</i>Deel via WhatsApp</a><a class="btn sm ghost">Nog 5 vragen voor een scherper beeld</a><div style="flex:1"></div><a class="btn sm ghost" style="color:var(--muted)"><i class="e">🧹</i>Wis mijn antwoorden</a></div>
</div>{signup(zw)}</div></main>'''
    return page(body)

def m1(zw=False):
    lead = ("Lobsy laat zien wie jij bent en wat bij je past. Doe de korte test en zie: <b>dit ben jij</b>." if zw else
            "Lobsy laat zien wie jij bent en welk werk bij je past. Doe de korte test en zie: <b>dit ben jij</b>.")
    third = '<span><i class="e">🪪</i>Gratis paspoort</span>' if zw else '<span><i class="e">🗺️</i><u>Banenkaart</u></span>'
    priv = "Alleen jij ziet het" if zw else "Werkgevers zien niets"
    body = f'''<header class="mh"><a class="brand"><img src="{LOGO}" alt="">Lobsy</a><div class="r"><a class="t">Inloggen</a><span class="ib" aria-label="Menu">{ic("menu")}</span></div></header>
<section class="mhero"><span class="pill dark"><i class="e">👋</i>Hoi! Gratis en zonder account</span>
<h1>Soms moet je uit je schild <em>groeien</em>.</h1><p class="lead">{lead}</p>
<div class="mcta"><a class="btn onpri">Doe de gratis test {ic("arrow")}</a><a class="btn ondark">Inloggen</a></div>
<div class="mmicro"><span><i class="e">⏱️</i>3 minuutjes</span><span><i class="e">🔒</i>{priv}</span>{third}</div>
{hero_scene_m(zw)}</section>{wave("var(--cream)", "var(--bg)")}
<section class="peek"><div class="eyebrow">Wat is Lobsy?</div><h2>Geen cv-site. Lobsy kijkt eerst naar jóu.</h2></section>'''
    return page(body, cls="m")

def m2():
    prog = "".join(f'<div class="{"on" if n==1 else ""}"><b style="--w:{w}%"></b></div>' for n, w in enumerate([100, 60, 0, 0]))
    body = f'''<header class="mt2"><img src="{LOGO}" alt=""><b>Vraag 8 van 20 · goed bezig! <i class="e">💪</i></b><div class="sp"></div><span class="ib" aria-label="Later verder">{ic("x")}</span></header>
<main class="mq"><div class="prog">{prog}</div>
<div class="card qcard"><div class="blab" style="display:flex;gap:10px;align-items:center">{em("💛", "sun")}<div><b>Dit vind je leuk</b><div class="xs muted">Vraag 3 van 5</div></div></div>
<h1>Ik help graag mensen of werk graag in een team met klanten.</h1>
<div class="lik">{_lik()}</div><div class="likl"><span>Past niet</span><span>Past wel</span></div>
<div class="tip"><img src="{MASCOT}" alt=""><span><b>Twijfel je?</b> Kies wat je het eerst voelt.</span></div></div>
<div class="mnav"><a class="btn">{ic("chevl")}Vorige</a><a class="btn pri">Volgende{ic("chevr")}</a></div>
<div class="qfoot"><i class="e">🔒</i>Blijft op dit apparaat</div></main>'''
    return page(body, cls="m")

def m3(zw=False):
    u = "".join(f'<li><i class="e">{e}</i>{t}</li>' for e, t in unlocks(zw)[:3])
    body = f'''<header class="mt2"><img src="{LOGO}" alt=""><b>Lobsy</b><div class="sp"></div><a class="btn sm ghost">Inloggen</a></header>
<main class="mr"><div style="display:flex;gap:6px;flex-wrap:wrap"><span class="pill">Eerste indruk · 20 vragen</span>{vb()}</div>
<h1><img src="{MASCOT}" alt="">Dit ben jij <i class="e" style="font-size:28px">🎉</i></h1><p class="sm muted" style="margin-top:6px">Wauw, wat een mooi begin!</p>
<div class="tiles">{tiles_html()}</div></main><div class="scrim"></div>
<div class="sheet"><span class="grab"></span><h2>Bewaar je DNA en zie je hele paspoort <i class="e">🎁</i></h2><ul class="unl" style="margin-top:10px">{u}</ul>
{providers()}<div class="carry">{CK}Je 20 antwoorden gaan mee</div>
<p class="xs muted" style="margin-top:12px;text-align:center">Al een account? <a style="color:var(--brand);font-weight:600">Inloggen</a> · <a style="font-weight:600">Later</a></p></div>'''
    return page(body, cls="m")
