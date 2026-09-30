from wr_ui import ic, vb, wizard, acts, cb, CK, GOOGLE, MSFT, em, MASCOT
from data import CO, KVK, DOMAIN, VEST, RESULTS

def _search(val, place, focus=True):
    v = val or '<span class="ph">90123456 of bedrijfsnaam</span>'
    return f'''<div style="display:grid;grid-template-columns:minmax(0,1fr) 240px auto;gap:12px;align-items:end;margin-top:22px">
<div class="fld" style="margin:0"><label class="lbl">KvK-nummer of bedrijfsnaam</label><div class="inp big {"focus" if focus else ""}">{ic("building")}{v}<span class="sp"></span></div></div>
<div class="fld" style="margin:0"><label class="lbl">Plaats <em>(optioneel)</em></label><div class="inp big">{ic("pin")}{place or '<span class="ph">Bijv. Utrecht</span>'}</div></div>
<a class="btn pri lg" style="height:60px">Zoeken{ic("arrow")}</a></div>'''

def d1():
    need = "".join(f'<div class="blk">{em(e, t, "s")}<div><h4>{h}</h4><p>{p}</p></div></div>' for e, t, h, p in [
        ("🔎", "sky", "Je KvK-nummer of bedrijfsnaam", "Wij halen de rest op uit het Handelsregister"),
        ("📧", "mint", "Een zakelijk e-mailadres", "Daarmee verifieer je het snelst dat je bij het bedrijf hoort"),
        ("⏱️", "sun", "Zo’n 5 minuten", "Over je bedrijf kun je ook later invullen"),
        ("🛡️", "peach", "Veilig en eerlijk", "Kandidaten zien je pas na verificatie")])
    card = f'''<div class="toprow"><span class="pill"><i class="e">👋</i>Welkom bij Lobsy voor werkgevers</span></div>
<h1>Laten we je bedrijf vinden</h1><p class="lead">Typ je KvK-nummer of zoek op naam. Staat je bedrijf erbij? Dan zie je meteen al je vestigingen.</p>
{_search("", "")}
<p class="hint" style="margin-top:10px"><i class="e">💡</i> 8 cijfers? Dan zoeken we direct op KvK-nummer. Anders zoeken we op naam (vanaf 3 letters).</p>
<div class="sec2"><h3>Wat heb je nodig?</h3><div class="blocks" style="margin-top:12px">{need}</div></div>
<div class="nb c" style="margin-top:22px"><i class="e">🏛️</i><div><b>Gegevens uit het Handelsregister</b><p>Naam, adres en vestigingen komen rechtstreeks van KVK. Je hoeft niets over te typen.</p></div></div>'''
    return wizard(1, "Hoi! Ik ben Lobsy 🦞", "Ik loop even met je mee. In een paar stappen staat je bedrijf klaar.", card)

def rows(res):
    out = []
    for e, t, name, kvk, city, nv, tags, st in res:
        tg = "".join(f'<span class="pill {"wn" if x == "Al op Lobsy" else "line"}">{x}</span>' for x in tags)
        out.append(f'''<div class="rrow {st}">{em(e, t)}<div><h4><span>{name}</span></h4><div class="meta"><span>KvK {kvk}</span><span>{ic("pin")}{city}</span><span>{ic("layers")}{nv}</span></div></div><div class="tags">{tg}</div>{ic("chevr")}</div>''')
    return "".join(out)

def d2():
    card = f'''<div class="toprow"><span class="pill">Stap 1 · Bedrijf zoeken</span>{vb()}</div>
<h1>Welke is van jou?</h1><p class="lead">We vonden 4 bedrijven voor “groen en zorg” in de buurt van Utrecht.</p>
{_search("groen en zorg", "Utrecht")}
<div style="display:flex;align-items:center;gap:10px;margin-top:22px"><b>4 resultaten</b><span class="hint">uit het KVK Handelsregister</span><div style="flex:1"></div><span class="hint">Alleen actieve inschrijvingen</span></div>
<div class="rlist">{rows(RESULTS)}</div>
<div class="nb" style="margin-top:16px"><i class="e">🤔</i><div><b>Staat je bedrijf er niet bij?</b><p>Probeer je KvK-nummer, of zoek zonder plaats. Nog niets? <a class="lnk">Registreer handmatig</a>, dan controleren wij je gegevens.</p></div></div>'''
    return wizard(1, "Goed gezocht!", "Kies je bedrijf. Een geel label betekent dat er al iemand van dat bedrijf op Lobsy zit.", card)

def vlist(mode):
    out = []
    for i, (n, a, nr, st, hq) in enumerate(VEST):
        used = st == "used"
        if mode == "heel":
            mark = cb(inc=not used) if not used else '<span class="cb"></span>'
        else:
            mark = f'<span class="rb {"on" if i == 0 else ""}"></span>'
        tags = ('<span class="pill line">Hoofdvestiging</span>' if hq else "") + (
            '<span class="pill wn">Heeft al een beheerder</span><a class="btn sm">Vraag toegang aan</a>' if used else '<span class="pill ok">Beschikbaar</span>')
        cls = "used" if used else ("on" if (mode == "heel" or i == 0) else "")
        out.append(f'<div class="vr {cls}">{mark}<div><b>{n}</b><div class="a">{a} · vestigingsnr. {nr}</div></div><div class="toprow">{tags}</div></div>')
    return f'<div class="vl">{"".join(out)}</div>'

def cobox():
    return f'''<div class="cobox">{em("🏡", "mint", "l")}<div><h3>{CO}</h3><div class="meta"><span>KvK {KVK}</span><span>Besloten vennootschap</span><span>{ic("globe")} {DOMAIN}</span><span>3 vestigingen</span></div></div><div class="sp"></div><a class="btn sm ghost">Ander bedrijf</a></div>'''

def scope(on):
    a = "on" if on == "heel" else ""
    b = "on" if on == "een" else ""
    return f'''<div class="optc"><div class="oc {a}"><span class="rd"></span>{em("🏢", "sun", "s")}<h4>Het hele bedrijf</h4><p>Jij wordt bedrijfsmanager. Alle vrije vestigingen komen er meteen bij.</p>
<ul><li>{CK}2 vestigingen direct inbegrepen</li><li>{CK}Tokens en vacatures per vestiging of centraal</li><li>{CK}Later collega’s per vestiging uitnodigen</li></ul></div>
<div class="oc {b}"><span class="rd"></span>{em("📍", "sky", "s")}<h4>Alleen één vestiging</h4><p>Handig als je alleen voor jouw locatie werft. Uitbreiden kan later altijd.</p>
<ul><li>{CK}Jij beheert alleen deze vestiging</li><li>{CK}Andere vestigingen blijven vrij</li><li class="no">{ic("clock")}Het hoofdkantoor kan later aansluiten</li></ul></div></div>'''

def d3():
    card = f'''<div class="toprow"><span class="pill">Stap 2 · Vestiging kiezen</span>{vb()}</div>
<h1>Gevonden! Wat wil je aanmelden?</h1>
<div style="margin-top:18px">{cobox()}</div>
<div class="sec2"><h3>Wat meld je aan?</h3>{scope("heel")}</div>
<div class="sec2"><h3>Vestigingen van dit bedrijf <span class="pill line">3</span></h3><p>Uit het Handelsregister. Groen = komt er meteen bij.</p>{vlist("heel")}</div>
<div class="nb w" style="margin-top:14px"><i class="e">🔐</i><div><b>Zeist heeft al een beheerder op Lobsy</b><p>Die vestiging nemen we niet mee. Werk je daar? Vraag toegang aan, dan beslist de huidige beheerder.</p></div></div>
{acts("Verder naar je account")}'''
    return wizard(2, "Mooi bedrijf!", "Meld je het hele bedrijf aan? Dan komen alle vrije vestigingen er meteen bij. Je kunt ook met één vestiging beginnen.", card)

def d4():
    card = f'''<div class="toprow"><span class="pill">Stap 3 · Jouw account</span>{vb()}</div>
<h1>Nu even over jou</h1><p class="lead">Jij wordt de bedrijfsmanager van <b>{CO}</b> Dat kun je later aan een collega overdragen.</p>
<div class="grid2"><div class="fld"><label class="lbl">Voor- en achternaam</label><div class="inp ok">Jasmijn de Vries</div></div>
<div class="fld"><label class="lbl">Functie <em>(optioneel)</em></label><div class="inp">HR-adviseur</div></div>
<div class="fld"><label class="lbl">Zakelijk e-mailadres</label><div class="inp ok focus">{ic("mail")}j.devries@{DOMAIN}<span class="sp"></span><span class="pill ok">{CK}Zakelijk</span></div>
<span class="hint">Past bij {DOMAIN}. Top: met dit adres verifieer je je bedrijf straks in één minuut.</span></div>
<div class="fld"><label class="lbl">Telefoon <em>(optioneel)</em></label><div class="inp">06 12 34 56 78</div><span class="hint">Alleen voor support. Nooit zichtbaar voor kandidaten.</span></div></div>
<div class="sec2"><h3>Hoe wil je inloggen?</h3><p>Microsoft of Google telt meteen als tweestapsverificatie. Met e-mail stel je die na afloop in (verplicht voor bedrijfsmanagers).</p>
<div class="prov2"><a class="btn on">{MSFT}Microsoft</a><a class="btn">{GOOGLE}Google</a><a class="btn">{ic("mail")}E-mail en wachtwoord</a></div>
<p class="hint" style="margin-top:8px">Log in met hetzelfde e-mailadres: j.devries@{DOMAIN}</p></div>
<div class="sec2"><div class="nb c"><i class="e">🎟️</i><div style="flex:1"><b>Code van een salesmanager of partner</b><p>We onthouden de link waarmee je binnenkwam. Een code die je zelf typt, gaat voor.</p></div><span class="pill ok">{CK}SM-K7Q2MP · via link</span><a class="btn sm ghost">Wijzigen</a></div></div>
<div class="chk"><span class="b on">{CK}</span><span>Ik ga akkoord met de <a class="lnk">voorwaarden voor werkgevers</a> en heb de <a class="lnk">privacyverklaring</a> gelezen.</span></div>
<div class="chk"><span class="b on">{CK}</span><span>Ik mag {CO} vertegenwoordigen op Lobsy. <span class="muted">Dit controleren we in de laatste stap.</span></span></div>
{acts("Account maken")}'''
    return wizard(3, "Bijna een account!", "Gebruik je werkmail. Dan kun je straks meteen laten zien dat je echt bij het bedrijf hoort.", card)

def d10():
    card = f'''<div class="toprow"><span class="pill wn">Al geregistreerd</span>{vb()}</div>
<h1>Dit bedrijf staat al op Lobsy</h1><p class="lead">Er is al een bedrijfsmanager voor <b>{CO}</b> Je kunt dus geen tweede eigenaar worden, maar je kunt wel toegang vragen.</p>
<div style="margin-top:18px">{cobox()}</div>
<div class="sec2"><h3>Vraag toegang aan</h3><p>We sturen je verzoek naar de huidige beheerder. Wie dat is, laten we om privacyredenen niet zien.</p>
<div class="grid2"><div class="fld"><label class="lbl">Waar werk je?</label><div class="inp">{ic("pin")}Amersfoort<span class="sp"></span>{ic("chev")}</div></div>
<div class="fld"><label class="lbl">Welke rol zoek je?</label><div class="inp">Vestigingsmanager<span class="sp"></span>{ic("chev")}</div></div>
<div class="fld"><label class="lbl">Je zakelijke e-mailadres</label><div class="inp ok">{ic("mail")}s.bakker@{DOMAIN}<span class="sp"></span><span class="pill ok">{CK}Zakelijk</span></div><span class="hint">Past bij {DOMAIN}: dan gaat je verzoek sneller.</span></div>
<div class="fld"><label class="lbl">Bericht <em>(optioneel)</em></label><div class="inp">Ik ben de nieuwe teamleider in Amersfoort.</div></div></div></div>
<div class="sec2"><h3>Wat gebeurt er dan?</h3><div class="tl">
<div><span class="d now">1</span><span><b>De bedrijfsmanager krijgt je verzoek</b><br><small>Per e-mail en in Lobsy onder Toegangsverzoeken</small></span><span class="hint">vandaag</span></div>
<div><span class="d">2</span><span><b>Na 3 werkdagen een herinnering</b><br><small>Jij krijgt bericht zodra er een besluit is</small></span><span class="hint">dag 3</span></div>
<div><span class="d">3</span><span><b>Geen reactie? Dan kijkt Lobsy-support mee</b><br><small>Support belt of mailt het bedrijf en beslist samen</small></span><span class="hint">dag 5</span></div></div></div>
<div class="nb" style="margin-top:14px"><i class="e">🗝️</i><div><b>Is de beheerder vertrokken en ben jij nu verantwoordelijk?</b><p>Dan kun je het eigendom overnemen. Daarvoor sturen we een brief met code naar het KvK-adres, en support controleert het verzoek. <a class="lnk">Eigendom overnemen</a></p></div></div>
{acts("Verzoek versturen")}'''
    return wizard(2, "Geen zorgen!", "Je collega’s zijn je voor geweest. Vraag ze om toegang, dan werk je samen in één account.", card, wave=False)
