from wr_ui import ic, vb, wizard, acts, cb, CK, em, MASCOT, LOGO, page
from data import CO, DOMAIN
from s_profile import badges

CAN = [(True, "Vacatures klaarzetten als concept"), (True, "Bedrijfsprofiel, cultuur en branche invullen"), (True, "Collega’s uitnodigen"),
       (False, "Zichtbaar zijn voor kandidaten"), (False, "Kandidaten en sollicitaties bekijken"), (False, "Tokens kopen")]

def canlist():
    return '<div class="canlist">' + "".join(f'<div class="{"" if ok else "no"}">{ic("check") if ok else ic("lock")}{t}</div>' for ok, t in CAN) + "</div>"

def methods(on="mail", mobile=False):
    a, b = ("on", "") if on == "mail" else ("", "on")
    mail = f'''<div class="mcard {a}"><span class="rd"></span><div class="toprow">{em("📧", "mint", "s")}<span class="pill ok">Snelst · ± 1 minuut</span></div>
<h3>Met je zakelijke e-mailadres</h3><p>Het domein moet passen bij de website die bij KVK staat. We sturen een code naar dat adres.</p>
<div class="facts2"><span class="dm"><i class="e">🌐</i>Website bij KVK: {DOMAIN}</span><span class="dm"><i class="e">✅</i>j.devries@{DOMAIN}</span></div>
<div class="how"><div><span class="n">1</span>We mailen een code van 6 cijfers</div><div><span class="n">2</span>Vul de code in (10 minuten geldig)</div><div><span class="n">3</span>Klaar: je bedrijf is meteen zichtbaar</div></div>
<p class="hint">Gmail, Outlook.com, Hotmail en andere gratis adressen tellen niet mee.</p></div>'''
    post = f'''<div class="mcard {b}"><span class="rd"></span><div class="toprow">{em("✉️", "sun", "s")}<span class="pill line">1 tot 3 werkdagen</span></div>
<h3>Met een brief op het KvK-adres</h3><p>Geen zakelijk adres, of past je domein niet? We sturen een brief met een code naar het adres dat bij KVK staat.</p>
<div class="facts2"><span class="dm"><i class="e">📍</i>Vondellaan 12, 3521 GE Utrecht</span></div>
<div class="how"><div><span class="n">1</span>Wij versturen de brief via PostNL</div><div><span class="n">2</span>Vul de code in via je dashboard</div><div><span class="n">3</span>De code is 30 dagen geldig</div></div>
<p class="hint">Staat het adres niet goed bij KVK? Pas het eerst aan bij KVK.</p></div>'''
    return f'<div class="mc">{mail}{post}</div>'

def d8():
    card = f'''<div class="toprow"><span class="pill">Stap 5 · Verifiëren</span>{vb()}</div>
<h1>Laat zien dat je bij {CO} hoort</h1><p class="lead">Zo weten kandidaten dat je vacatures echt zijn. Tot je geverifieerd bent, is je bedrijf <b>onzichtbaar</b> voor kandidaten. Kies hoe je het wilt doen:</p>
{methods("mail")}
<div class="sec2"><h3><i class="e">⏳</i>Wat kan al, en wat na verificatie?</h3>{canlist()}
<p class="hint" style="margin-top:10px">Vacatures die je klaarzet, gaan automatisch live zodra je bent geverifieerd.</p></div>
<div class="nb c" style="margin-top:18px"><i class="e">🙋</i><div><b>Lukt het allebei niet?</b><p>Vraag een handmatige controle aan. Lobsy-support neemt binnen 2 werkdagen contact met je op. <a class="lnk">Handmatige controle aanvragen</a></p></div></div>
{acts("Stuur de code naar j.devries@" + DOMAIN, back=True)}'''
    return wizard(5, "Laatste stap!", "Dit houdt Lobsy eerlijk: zo kan niemand zomaar jullie bedrijf registreren. 🛡️", card, wave=False)

def envelope():
    return f'''<div class="env"><div class="paper"><b>Lobsy · verificatiecode</b>Voor: Jasmijn de Vries, {CO}<div class="code">K7Q2-M9PX</div></div><div class="flap"></div><img src="{MASCOT}" alt=""></div>'''

def code_boxes(filled="K7Q2M"):
    s = []
    for i in range(8):
        if i == 4:
            s.append('<span class="dash">-</span>')
        ch = filled[i] if i < len(filled) else ""
        s.append(f'<span class="{"f" if i == len(filled) else ""}">{ch}</span>')
    return f'<div class="otp w8">{"".join(s)}</div>'

def timeline():
    rows = [("ok", CK, "Brief aangevraagd", "di 29 sep, 23:40", ""), ("ok", CK, "Gedrukt en aan PostNL gegeven", "wo 30 sep", ""),
            ("now", "3", "Onderweg naar Vondellaan 12, Utrecht", "verwacht do 1 of vr 2 okt", ""), ("", "4", "Code invullen", "geldig t/m do 29 okt", "")]
    return '<div class="tl">' + "".join(f'<div><span class="d {c}">{n}</span><span><b>{t}</b><br><small>{s}</small></span><span></span></div>' for c, n, t, s, _ in rows) + "</div>"

def d9():
    card = f'''<div class="toprow"><span class="pill wn">Brief onderweg</span>{vb()}</div>
<h1>Je brief is onderweg ✉️</h1><p class="lead">We hebben een brief met een code gestuurd naar het KvK-adres van {CO} Vul de code hieronder in zodra je hem hebt.</p>
<div style="display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1fr);gap:24px;margin-top:20px;align-items:start">
<div>{envelope()}{timeline()}</div>
<div><div class="fld" style="margin-top:0"><label class="lbl">Code uit de brief</label>{code_boxes()}<span class="hint">8 tekens, staat onder “Uw verificatiecode”. Hoofdletters maken niet uit.</span></div>
<a class="btn pri lg full" style="margin-top:16px">Bedrijf verifiëren{ic("arrow")}</a>
<div class="nb c" style="margin-top:16px"><i class="e">📭</i><div><b>Niet ontvangen?</b><p>Opnieuw versturen kan vanaf <b>ma 5 okt</b> (nog 2 keer). Een nieuwe brief maakt de oude code ongeldig.</p></div></div>
<div class="nb" style="margin-top:10px"><i class="e">📧</i><div><b>Toch sneller?</b><p>Heb je nu wel een zakelijk e-mailadres op {DOMAIN}? <a class="lnk">Verifieer met e-mail</a></p></div></div>
<p class="hint" style="margin-top:10px">Na 5 foute pogingen blokkeren we de code en vragen we support om mee te kijken.</p></div></div>
<div class="sec2"><h3><i class="e">⏳</i>Intussen kun je al</h3>{canlist()}</div>
{acts("Naar mijn dashboard", back=False, note="Je kunt de code ook later in je dashboard invullen.")}'''
    return wizard(5, "Even geduld!", "De postbode komt eraan. 📮 Zet intussen alvast je eerste vacature klaar: die gaat vanzelf live.", card, wave=False)

def side(active="Dashboard"):
    items = [("Dashboard", "compass", False), ("Vacatures", "report", False), ("Kandidaten", "user", True), ("Sollicitaties", "mail", True), ("Bedrijfsprofiel", "building", False),
             ("Vestigingen", "pin", False), ("Team", "handshake", False), ("Tokens", "coin", True)]
    li = "".join(f'<a class="{"on" if n == active else ""}">{ic(i)}{n}{ic("lock", "i lk") if lk else ""}</a>' for n, i, lk in items)
    return f'''<aside class="side"><a class="brand"><img src="{LOGO}" alt="">Lobsy</a><div class="co">{em("🏡", "mint", "s")}<div><b>Groen &amp; Zorg</b><small>Hele bedrijf · 2 vestigingen</small></div></div>{li}
<h5>Account</h5><a>{ic("shield")}Beveiliging</a></aside>'''

def banner():
    return f'''<div class="ban"><i class="e">🙈</i><div><div class="toprow"><b>Nog niet zichtbaar voor kandidaten</b><span class="pill wn">Niet geverifieerd</span></div>
<p>Je bedrijfspagina, vacatures, banenkaart, Match en Google tonen nog niets van {CO} Zodra je code is ingevuld, gaan je klaargezette vacatures automatisch live.</p></div><div class="sp"></div>
<a class="btn pri">Code uit de brief invullen</a><a class="btn">Via e-mail</a></div>'''

def d11():
    cl = [(True, "Account aangemaakt", "Ingelogd met Microsoft · telt als tweestapsverificatie"), (True, "Bedrijf en vestigingen gekozen", "Utrecht en Amersfoort"),
          (True, "Branche en cultuurprofiel", "Zorg, Schoonmaak · 3 kernwaarden"), (True, "Maatschappelijke betrokkenheid", "3 badges, 1 gecontroleerd"),
          (False, "Bedrijf verifiëren", "Brief onderweg · verwacht do 1 okt"), (False, "Eerste vacature klaarzetten", "Gaat live na verificatie"),
          (False, "Collega’s uitnodigen", "Bijv. de vestigingsmanager in Amersfoort"), (False, "Volledige Cultuurscan (12 vragen)", "Maakt je match nog scherper")]
    cli = "".join(f'<div class="cli {"done" if d else ""}"><span class="d">{CK if d else ""}</span><div><b>{t}</b><small>{s}</small></div>{"" if d else ic("chevr")}</div>' for d, t, s in cl)
    vis = "".join(f'<div class="lockrow">{ic("eyeoff")}<span><b>{t}</b> · {s}</span></div>' for t, s in [
        ("Bedrijfspagina", "verborgen, geeft 404 aan bezoekers"), ("Banenkaart, zoeken en Match", "geen vacatures zichtbaar"), ("Google en sitemap", "niet vermeld, geen structured data")])
    vac = "".join(f'<tr><td><b>{t}</b><div class="hint">{w}</div></td><td><span class="pill {c}">{s}</span></td></tr>' for t, w, s, c in [
        ("Verzorgende IG, wijkteam Utrecht-Oost", "Utrecht · 24–32 uur", "Klaar · gaat live na verificatie", "ok"),
        ("Huishoudelijk medewerker", "Amersfoort · 12–20 uur", "Concept", "line")])
    body = f'''<div class="app">{side()}<main class="amain"><div class="atop"><h1>Welkom, Jasmijn</h1>{vb()}<div class="r"><span class="lang">{ic("globe")}NL</span><a class="btn sm">{ic("user")}Jasmijn de Vries</a></div></div>
{banner()}
<div class="welc"><img src="{MASCOT}" alt=""><div><h2>Je bedrijf staat klaar! 🎉</h2><p>Nog één ding: de code uit de brief. Intussen kun je alvast vacatures klaarzetten en je team uitnodigen.</p></div></div>
<div class="dgrid"><div class="pan"><h3><i class="e">✅</i>Aan de slag<span class="sp"></span><span class="sm muted">4 van 8</span></h3><div class="prog2"><i style="width:50%"></i></div><div class="cl">{cli}</div></div>
<div style="display:flex;flex-direction:column;gap:20px"><div class="pan"><h3><i class="e">📝</i>Vacatures<span class="sp"></span><a class="btn sm pri">{ic("plus")}Nieuwe vacature</a></h3><table class="vt">{vac}</table></div>
<div class="pan"><h3><i class="e">🙈</i>Zichtbaarheid</h3>{vis}<p class="hint" style="margin-top:10px">Kandidaten, sollicitaties en tokens kopen gaan open na verificatie.</p></div></div></div></main></div>'''
    return page(body)

def d12():
    dims = "".join(f'<div><b>{v}%</b>{n}</div>' for v, n in [(55, "Zelfstandig"), (60, "Informeel"), (80, "Samenwerken"), (30, "Flexibel"), (45, "Vernieuwend"), (82, "Mensen voorop")])
    pil = "".join(f'<span class="{"on" if on else ""}">{t}</span>' for t, on in [("samen &amp; klantgericht", True), ("zorgvuldig &amp; betrouwbaar", True), ("kalm onder druk", True), ("informeel &amp; handen uit de mouwen", False), ("groei &amp; innovatie", False)])
    body = f'''<div class="app">{side("Vacatures")}<main class="amain"><div class="atop"><h1>Vacature bewerken</h1>{vb()}<span class="pill ok">Klaar · gaat live na verificatie</span><div class="r"><a class="btn sm">Voorbeeld</a><a class="btn sm pri">Opslaan</a></div></div>
<div class="toprow" style="margin-bottom:14px"><span class="pill line">1 Basis</span><span class="pill line">2 Uren en loon</span><span class="pill coral">3 Cultuur en waarden</span><span class="pill line">4 Vragen</span></div>
<div class="vgrid"><div class="pan"><h3><i class="e">🧬</i>Cultuur en waarden</h3><p class="sm muted" style="margin-top:4px">Standaard gebruikt elke vacature het cultuurprofiel van het bedrijf. Werkt dit team anders? Pas het dan alleen hier aan.</p>
<div class="inh" style="margin-top:14px"><span class="tgl on"></span><div><b>Cultuurprofiel van {CO} gebruiken</b><div class="hint">Laatst bijgewerkt bij de registratie · 29 sep</div></div></div>
<div class="prof">{dims}</div>
<div class="toprow" style="margin-top:10px"><span class="pill">Verbinding &amp; zorg</span><span class="pill">Prestatie &amp; groei</span><span class="pill">Impact &amp; rechtvaardigheid</span></div>
<div class="sec2"><h3>Dit team werkt anders <span class="pill line">optioneel</span></h3><p>Kies 3 tot 5 kenmerken voor dit team. Die gaan dan vóór het bedrijfsprofiel.</p><div class="pillars off">{pil}</div></div>
<div class="sec2"><h3><i class="e">🌱</i>Betrokkenheid bij deze vacature</h3><p>Overgenomen van het bedrijfsprofiel. Je kunt badges per vacature uitzetten.</p><div style="margin-top:10px">{badges()}</div></div></div>
<div class="pv"><div class="ph"><span class="lg">G&amp;Z</span><div><div class="hint">Zo ziet een kandidaat het</div><h4>Verzorgende IG, wijkteam Utrecht-Oost</h4></div></div>
<div class="pb"><div class="toprow"><span class="bdg"><i class="e">🎓</i>Erkend leerbedrijf</span><span class="bdg" style="background:var(--peach)"><i class="e">🤝</i>Werk voor iedereen</span><span class="bdg" style="background:var(--sky)"><i class="e">⚖️</i>Cao VVT</span></div>
<p class="hint">🎓 gecontroleerd bij SBB · 🤝 en ⚖️ door werkgever opgegeven</p>
<div class="fitr"><span>Cultuur past</span><b class="bar2" style="--w:84%"></b><b>84%</b></div>
<div class="fitr v"><span>Waarden passen</span><b class="bar2" style="--w:78%"></b><b>78%</b></div>
<div class="why"><b>Waarom?</b> Jij werkt graag samen en zet mensen voorop, net als dit team. En “Verbinding &amp; zorg” is voor jou en voor hen het belangrijkst.</div>
<p class="hint">Kandidaten zien dit alleen als ze zelf de Cultuurscan of Waardentest hebben gedaan.</p></div></div></div></main></div>'''
    return page(body)
