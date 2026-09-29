"""Sollicitaties pipeline, Vestigingen & team, Tokens & facturen, Kandidaatinzichten, mobile."""
from bmui import *

def anon_card(nr, match, facts, age, late=False, sel=False):
    f = "".join(f"<span>{x}</span>" for x in facts)
    t = f'<span class="ft" style="{"color:var(--danger)" if late else ""}">{ic("clock")}{age}</span>'
    return f'<div class="kc{" sel" if sel else ""}"><div class="hd"><span class="av anon">{ic("user")}</span><b>Kandidaat {nr}</b></div><div class="facts">{f}</div><div style="display:flex;align-items:center">{t}<span class="mt" style="margin-inline-start:auto">{match}%</span></div></div>'

def named_card(ini, name, match, facts, foot, sel=False, icon="clock"):
    f = "".join(f"<span>{x}</span>" for x in facts)
    return f'<div class="kc{" sel" if sel else ""}"><div class="hd"><span class="av">{ini}</span><b>{name}</b></div><div class="facts">{f}</div><div style="display:flex;align-items:center;gap:6px"><span class="ft">{ic(icon)}{foot}</span><span class="mt" style="margin-inline-start:auto">{match}%</span></div></div>'

def d3():
    c1 = "".join([anon_card("#4821", 92, ["12 min fiets", "Direct", "32 u/w"], "3 dagen · te laat", late=True),
                  anon_card("#5107", 88, ["18 min auto", "Per 15-10", "24 u/w"], "2 dagen"),
                  anon_card("#5230", 81, ["9 min fiets", "Direct", "16 u/w"], "vandaag"),
                  anon_card("#5244", 74, ["25 min OV", "Per 01-11", "40 u/w"], "vandaag")])
    c2 = "".join([named_card("PS", "Priya Sanders", 90, ["14 min fiets", "Direct"], "Sinds 28-09", sel=True, icon="check"),
                  named_card("TM", "Tom Meijer", 84, ["20 min auto", "Per 07-10"], "Sinds 27-09", icon="check"),
                  named_card("AK", "Aylin Kaya", 79, ["11 min fiets", "Direct"], "Sinds 25-09", icon="check")])
    c3 = "".join([named_card("RB", "Ruben de Bruin", 86, ["16 min fiets", "Direct"], "Gesprek do 01-10", icon="cal"),
                  named_card("LJ", "Lotte Jansen", 77, ["22 min OV", "Per 15-10"], "Gesprek vr 02-10", icon="cal")])
    c4 = named_card("MO", "Mehmet Öztürk", 91, ["8 min fiets", "Gestart 21-09"], "Contact zichtbaar", icon="check")
    c5 = f'<div class="kc" style="color:var(--muted);font-size:var(--text-xs)">7 afgewezen · 2 elders aan de slag<br>1 teruggetrokken</div>'
    cols = [("Nieuw", "4", "anoniem", c1), ("Geaccepteerd", "3", "naam + cv", c2), ("Uitgenodigd", "2", "", c3), ("Aangenomen", "1", "contact", c4), ("Afgewezen", "10", "", c5)]
    kb = '<div class="kb">' + "".join(f'<div class="kcol"><h3>{t}<span class="cnt" style="margin:0">{n}</span><small>{s}</small></h3>{c}</div>' for t, n, s, c in cols) + '</div>'
    body = f'''<div class="ph"><div><h1>Sollicitaties</h1><p>Per vacature, van nieuw tot aangenomen.</p></div></div>
<div style="display:flex;gap:8px;align-items:center;margin-bottom:14px"><span class="dd on" style="height:36px">Vacature <em style="color:var(--brand)">Oogstmedewerker kas · Naaldwijk</em>{ic("chevd")}</span><span class="dd">Vestiging <em>Alle</em>{ic("chevd")}</span><span class="dd">{ic("clock")}Alleen &gt; 48 uur</span><span class="seg" style="margin-inline-start:8px"><span class="on">Pijplijn</span><span>Lijst</span></span></div>
{kb}'''
    drawer = f'''<div class="scrim"></div><aside class="drawer">
<div class="dh"><span class="av">PS</span><div><h2>Priya Sanders</h2><div class="muted sm">Oogstmedewerker kas · Naaldwijk · gesolliciteerd 26-09</div></div><span class="x">{ic("x")}</span></div>
<div class="steps"><span class="done">{ic("check")}Nieuw</span><em></em><span class="cur">{ic("check")}Geaccepteerd</span><em></em><span>Uitgenodigd</span><em></em><span>Aangenomen</span></div>
<div class="dtabs"><span class="on">Profiel</span><span>Motivatie</span><span>Tijdlijn</span></div>
<div class="db">
<div class="sec"><h3>Match 90% <span class="muted sm" style="font-weight:400">met deze vacature</span></h3>
<dl class="kv"><dt>Reistijd</dt><dd>14 min fiets · 3,2 km</dd><dt>Beschikbaar</dt><dd>Direct · 32 uur per week</dd><dt>Rijbewijs</dt><dd>B</dd><dt>Opleiding</dt><dd>MBO 2 Teelt en groene technologie</dd><dt>Ervaring</dt><dd>2 jaar oogst en sorteren</dd></dl></div>
<div class="sec"><h3>Wat je ziet</h3><div class="box" style="padding:6px 12px">
<div class="lockrow on">{ic("check")}Anoniem profiel, match en motivatie</div>
<div class="lockrow on">{ic("check")}Naam en Lobsy-cv <span class="muted">(na accepteren)</span></div>
<div class="lockrow off">{ic("lock")}E-mail en telefoon: zichtbaar na aanname</div></div></div>
<div class="sec"><h3>Motivatie</h3><p class="muted">“Ik werk graag buiten en met planten. Afgelopen twee seizoenen heb ik bij een tomatenkweker geoogst en gesorteerd…”</p></div>
<div class="sec"><h3>Interne notitie <span class="muted sm" style="font-weight:400">alleen zichtbaar voor je team</span></h3><div class="ta muted">Belt liever niet tijdens schooltijden; voorkeur voor ochtenddiensten.</div></div>
</div>
<div class="df"><span class="btn pri">{ic("cal")}Uitnodigen</span><span class="btn">{ic("dl")}Lobsy-cv</span><span style="flex:1"></span><span class="btn dng">Afwijzen</span><span class="ra">{ic("more")}</span></div></aside>'''
    return page("bm", "sol", [COMPANY, "Sollicitaties"], body, drawer, collapsed=("Meer", "Organisatie", "Tokens & facturen"), demo="l")

def d4():
    tree = f'''<div class="tree">
<a>{ic("building")}<b>{COMPANY}</b><span class="cnt">14</span></a>
<a class="l1 on">{ic("chevd")}Regio Westland<span class="cnt">5</span></a>
<a class="l2">{ic("pin")}Naaldwijk</a><a class="l2">{ic("pin")}Monster</a><a class="l2">{ic("pin")}’s-Gravenzande</a>
<a class="l2">{ic("pin")}De Lier <span class="pill wn" style="margin-inline-start:auto;height:18px">Geen manager</span></a><a class="l2">{ic("pin")}Honselersdijk</a>
<a class="l1">{ic("chevr")}Regio Delfland<span class="cnt">4</span></a>
<a class="l1">{ic("chevr")}Regio Rotterdam-Zuid<span class="cnt">5</span></a>
<a class="l1 add">{ic("plus")}Regio toevoegen</a><a class="l1 add">{ic("plus")}Vestiging toevoegen via KvK</a></div>'''
    people = [("MV", "Marieke de Vries", "marieke@voorbeeld-tuinbouw.nl", "Bedrijfsmanager", "Alle vestigingen", "ok", "Actief", "vandaag"),
              ("JV", "Joost Verbeek", "joost@voorbeeld-tuinbouw.nl", "Regiomanager", "Regio Westland", "ok", "Actief", "gisteren"),
              ("SB", "Sanne Bakker", "sanne@voorbeeld-tuinbouw.nl", "Vestigingsmanager", "Naaldwijk", "ok", "Actief", "vandaag"),
              ("KD", "Kees Dekker", "kees@voorbeeld-tuinbouw.nl", "Vestigingsmanager", "Monster", "ok", "Actief", "3 d geleden"),
              ("NH", "Noor Hendriks", "noor@voorbeeld-tuinbouw.nl", "Vestigingsmanager", "’s-Gravenzande", "ok", "Actief", "vandaag"),
              ("??", "f.vos@voorbeeld-tuinbouw.nl", "Uitgenodigd 22-09", "Vestigingsmanager", "De Lier", "wn", "Uitgenodigd", "—"),
              ("EB", "Emre Bulut", "emre@voorbeeld-tuinbouw.nl", "Vestigingsmanager", "Honselersdijk", "ok", "Actief", "1 w geleden")]
    rc = {"Bedrijfsmanager": "info", "Regiomanager": "line", "Vestigingsmanager": ""}
    trs = "".join(f'<tr><td><div class="who"><span class="av">{a if a!="??" else ic("mail")}</span><div><b>{n}</b><small>{e}</small></div></div></td><td><span class="pill {rc[r]}">{r}</span></td><td>{sc}</td><td><span class="dotst {"" if k=="ok" else k}">{st}</span></td><td class="muted">{la}</td><td style="text-align:end"><span class="ra">{ic("more")}</span></td></tr>' for a, n, e, r, sc, k, st, la in people)
    main = f'''<div class="card" style="overflow:hidden"><div class="ch" style="padding-bottom:4px"><span class="ico">{ic("map")}</span><div><h2>Regio Westland</h2><div class="muted sm">5 vestigingen · 20 live vacatures</div></div><span class="sp"></span><span class="btn sm">Bewerken</span></div>
<div class="tabs" style="margin:8px 0 0;padding:0 16px"><span>Vestigingen</span><span class="on">Team <span class="cnt">7</span></span><span>Tokens</span></div>
<table><thead><tr><th>Naam</th><th>Rol</th><th>Bereik</th><th>Status</th><th>Actief</th><th></th></tr></thead><tbody>{trs}</tbody></table></div>'''
    body = f'''<div class="ph"><div><h1>Vestigingen &amp; regio’s</h1><p>Je organisatie, regio’s, vestigingen en wie waar bij kan.</p></div><div class="act"><span class="btn">{ic("dl")}Exporteren</span><span class="btn pri">{ic("plus")}Iemand uitnodigen</span></div></div>
<div class="row" style="grid-template-columns:300px minmax(0,1fr);align-items:start"><div class="card">{tree}</div>{main}</div>'''
    roles = [("Bedrijfsmanager", "Alles voor alle vestigingen: vacatures goedkeuren, tokens kopen en verdelen, facturen, team en bedrijfsprofiel.", False),
             ("Regiomanager", "Kijkt mee in de vestigingen van één regio. Alleen lezen: geen wijzigingen, geen tokens kopen.", False),
             ("Vestigingsmanager", "Alle rechten, maar alleen voor de eigen vestiging: vacatures, sollicitaties, tokens en profiel.", True)]
    rhtml = "".join(f'<div class="rolec{" on" if on else ""}"><span class="rd"></span><b>{t}</b><p>{d}</p></div>' for t, d, on in roles)
    drawer = f'''<div class="scrim"></div><aside class="drawer" style="width:392px">
<div class="dh"><span class="ico">{ic("send")}</span><div><h2>Iemand uitnodigen</h2><div class="muted sm">Eén plek voor alle uitnodigingen in je organisatie</div></div><span class="x">{ic("x")}</span></div>
<div class="db" style="margin-top:6px;gap:12px">
<div><span class="lbl" style="margin-top:0">E-mailadres</span><div class="inp" style="width:100%;color:var(--text)">f.vos@voorbeeld-tuinbouw.nl</div></div>
<div><span class="lbl" style="margin-top:0">Rol</span><div style="display:flex;flex-direction:column;gap:8px">{rhtml}</div></div>
<div><span class="lbl" style="margin-top:0">Vestiging</span><div class="inp" style="width:100%;color:var(--text);justify-content:space-between">De Lier · Regio Westland{ic("chevd")}</div></div>
<div class="warnbox">{ic("info")}<div>Kandidaatgegevens volgen de privacyregels: naam na accepteren, contactgegevens pas na aanname.</div></div>
</div>
<div class="df"><span class="btn pri">{ic("send")}Uitnodiging versturen</span><span class="btn ghost">Annuleren</span></div></aside>'''
    return page("bm", "vest", [COMPANY, "Organisatie", "Vestigingen & regio’s"], body, drawer, collapsed=("Meer",), demo="l")

def d5():
    use = [("Naaldwijk", "Westland", 60, 22, 38), ("Monster", "Westland", 40, 26, 14), ("Delft", "Delfland", 60, 31, 29),
           ("Pijnacker", "Delfland", 20, 17, 3), ("Ridderkerk", "Rotterdam-Zuid", 50, 28, 22), ("Overige 9 vestigingen", "", 158, 63, 95)]
    trs = "".join(f'<tr><td><b>{n}</b>{f"<div class=muted style=font-size:var(--text-xs)>{r}</div>" if r else ""}</td><td class="num">{a}</td><td class="num">{u}</td><td class="num"><b>{o}</b></td><td style="width:120px"><span class="bar{" wn" if o<=5 else ""}"><i style="width:{int(u/a*100)}%"></i></span></td><td style="text-align:end"><span class="btn sm">Verdelen</span></td></tr>' for n, r, a, u, o in use)
    packs = [("10 tokens", "€ 95", "€ 9,50 per token", False), ("50 tokens", "€ 425", "€ 8,50 per token", True), ("100 tokens", "€ 750", "€ 7,50 per token", False)]
    ph = "".join(f'<div class="pk{" on" if on else ""}"><small>{a}</small><b>{p}</b><small>{d}</small></div>' for a, p, d, on in packs)
    costs = [("Vacature publiceren", "3"), ("Verlengen (30 dagen)", "1"), ("Uitlichten", "2"), ("Pushbericht naar kandidaten", "5"), ("Contact via talentpool", "1")]
    ch = "".join(f'<div style="display:flex;justify-content:space-between;padding:4px 0"><span class="muted">{a}</span><b>{b} {"token" if b=="1" else "tokens"}</b></div>' for a, b in costs)
    inv = [("F-2026-0931", "24-09-2026", "50 tokens", "€ 514,25"), ("F-2026-0812", "02-09-2026", "100 tokens", "€ 907,50")]
    ih = "".join(f'<tr><td><b>{a}</b><div class="muted sm">{d}</div></td><td>{p}</td><td class="num">{b}</td><td><span class="pill ok">Betaald</span></td><td style="text-align:end"><span class="ra">{ic("dl")}</span></td></tr>' for a, d, p, b in inv)
    kp = "".join(f'<div class="card kpi"><small>{ic(i) if i else ""}{a}</small><div class="v">{v}</div><div class="dl">{d}</div></div>' for i, a, v, d in [
        ("coins", "Centraal saldo", "412", "niet toegewezen: 224"), ("", "Toegewezen aan vestigingen", "188", "over 14 vestigingen"),
        ("", "Verbruikt (30 dagen)", "86", "vorige 30 dagen: 71"), ("", "Gereserveerd voor aanvragen", "9", "3 publicatieaanvragen")])
    body = f'''<div class="ph"><div><h1>Tokens &amp; facturen</h1><p>Saldo, verdeling over vestigingen, aankopen en facturen op één plek.</p></div><div class="act"><span class="btn">{ic("list")}Alle mutaties</span><span class="btn pri">{ic("plus")}Tokens kopen</span></div></div>
<div class="tabs"><span class="on">Overzicht</span><span>Verbruik per vestiging</span><span>Mutaties</span><span>Facturen</span></div>
<div class="kpis" style="grid-template-columns:repeat(4,minmax(0,1fr))">{kp}</div>
<div class="row" style="grid-template-columns:minmax(0,1fr) 400px;align-items:start">
<div class="card" style="overflow:hidden"><div class="ch"><h2>Verbruik per vestiging</h2><span class="muted sm">30 dagen</span><span class="sp"></span><a>Automatisch aanvullen instellen {ic("chevr")}</a></div>
<table><thead><tr><th>Vestiging</th><th class="num">Toegewezen</th><th class="num">Verbruikt</th><th class="num">Over</th><th></th><th></th></tr></thead><tbody>{trs}</tbody></table></div>
<div class="row" style="gap:16px">
<div class="card"><div class="ch"><h2>Tokens kopen</h2><span class="sp"></span><a>Wat kost wat? {ic("chevd")}</a></div><div style="padding:0 16px 14px;display:flex;flex-direction:column;gap:10px">
<div style="display:grid;grid-template-columns:repeat(3,1fr);gap:8px">{ph}</div>
<div class="box" style="padding:6px 12px;font-size:var(--text-xs)">{ch}</div>
<span class="btn pri" style="justify-content:center">50 tokens kopen · € 425 excl. btw</span></div></div>
</div></div>
<div class="card" style="overflow:hidden;margin-top:16px;width:calc(100% - 416px)"><div class="ch"><h2>Recente facturen</h2><span class="sp"></span><a>Alle facturen {ic("chevr")}</a></div>
<table><thead><tr><th>Factuur</th><th>Omschrijving</th><th class="num">Bedrag incl. btw</th><th>Status</th><th></th></tr></thead><tbody>{ih}</tbody></table></div>'''
    return page("bm", "tok", [COMPANY, "Tokens & facturen"], body, collapsed=("Meer", "Werving"))

def m1():
    todos = [("wn", "flag", "3 publicatieaanvragen", "Samen 9 tokens"), ("bad", "clock", "17 sollicitaties > 48 uur", "Ridderkerk 8 · Monster 5"),
             ("wn", "cal", "5 vacatures verlopen", "Binnen 7 dagen"), ("", "coins", "Pijnacker: nog 3 tokens", "Tokens verdelen")]
    li = "".join(f'<li><span class="ico {k}">{ic(i)}</span><div class="sp"><b>{t}</b><small>{m}</small></div>{ic("chevr")}</li>' for k, i, t, m in todos)
    kp = "".join(f'<div class="card kpi"><small>{a}</small><div class="v">{v}</div><div class="dl">{d}</div></div>' for a, v, d in [
        ("Actieve vacatures", "48", '<span class="up">+4</span>'), ("Nieuwe sollicitaties", "186", '<span class="up">+12%</span>'), ("Eerste reactie", "2,1 d", '<span class="up">0,4 d sneller</span>'), ("Tokensaldo", "412", "± 7 weken")])
    body = f'''<main class="mmain" style="padding-bottom:90px"><h1>Dashboard</h1><div class="muted sm">14 vestigingen · laatste 30 dagen</div>
<div class="mkpis">{kp}</div>
<div class="mh">Te doen <span class="cnt" style="margin:0">7</span><a>Alles</a></div>
<div class="mcard"><ul class="mlist">{li}</ul></div>
<div class="mh" style="margin-top:16px">Vestigingen met achterstand<a>Alle 14</a></div>
<div class="mcard"><ul class="mlist"><li><span class="dotst bad"></span><div class="sp"><b>Ridderkerk</b><small>17 nieuw · 1e reactie 4,1 d</small></div>{ic("chevr")}</li></ul></div></main>'''
    return mpage(body, active="dash")

def m2():
    top = f'<header class="mtop" style="padding-inline-start:4px"><span class="tb">{ic("arrowl")}</span><div style="line-height:1.2"><b>Kandidaat #4821</b><div style="font-size:var(--text-xs);opacity:.75">Oogstmedewerker kas · Naaldwijk</div></div><span style="margin-inline-start:auto" class="tb">{ic("more")}</span></header>'
    body = f'''<main class="mmain" style="padding-bottom:100px">
<div style="display:flex;align-items:center;gap:10px;margin-bottom:12px"><span class="av" style="width:44px;height:44px;background:var(--pearl-mid);color:var(--muted)">{ic("user")}</span><div class="sp" style="flex:1"><b style="font-size:var(--text-md)">Nieuwe sollicitatie</b><div class="sm" style="color:var(--danger)">3 dagen zonder reactie</div></div><span class="mt" style="font-size:var(--text-sm);padding:3px 10px">92% match</span></div>
<div class="fact"><div><small>Reistijd</small><b>12 min fiets</b></div><div><small>Beschikbaar</small><b>Direct</b></div><div><small>Uren per week</small><b>32</b></div><div><small>Rijbewijs</small><b>B</b></div></div>
<span class="lbl">Motivatie</span><div class="mcard" style="padding:12px 14px;color:var(--muted)">“Ik zoek werk dichtbij huis met vaste uren. Ik heb twee zomers bij een paprikakweker gewerkt en vind het fijn om buiten bezig te zijn…”</div>
<span class="lbl">Opleiding en ervaring</span><div class="mcard"><ul class="mlist"><li><div class="sp"><b>MBO 2 Teelt</b><small>Afgerond</small></div></li><li><div class="sp"><b>Oogstmedewerker</b><small>2 seizoenen</small></div></li></ul></div>
<div class="note" style="margin-top:14px;border:0;border-radius:var(--radius-sm)">{ic("lock")}<span>Naam en cv zie je na accepteren. Contactgegevens na aanname.</span></div>
</main>'''
    sticky = f'<div class="sticky"><span class="btn dng">Afwijzen</span><span class="btn pri">{ic("check")}Accepteren</span></div>'
    return mpage(body, extra=sticky, top=top, bnav=False).replace('<span class="demo" style="bottom:14px">', '<span class="demo" style="bottom:92px">')
