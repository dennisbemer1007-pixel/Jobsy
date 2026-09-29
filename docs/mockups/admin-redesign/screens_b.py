from ui import *

def d3():
    orgs = [
        ("open", "Voorbeeld Kwekerij Westgaarde BV", "9•••••12", "Werkgever", "Westland", "4", "38", "12", "1.240", "ok", "Actief", True),
        ("child", "Vestiging Naaldwijk", "", "Vestiging", "Westland", "", "14", "5", "", "ok", "Actief", False),
        ("child", "Vestiging Honselersdijk", "", "Vestiging", "Westland", "", "11", "4", "", "ok", "Actief", False),
        ("child", "Vestiging Poeldijk", "", "Vestiging", "Westland", "", "9", "3", "", "wn", "Overname", False),
        ("child", "Vestiging De Lier (nieuw)", "", "Vestiging", "Westland", "", "4", "0", "", "off", "Niet live", False),
        ("closed", "Zorggroep Voorbeeld-Maasland", "8•••••07", "Enterprise", "Maasland", "7", "64", "21", "3.100", "ok", "Actief", False),
        ("closed", "Voorbeeld FlexWerk BV", "7•••••55", "Intermediair", "Zuid-Holland", "2", "17", "33", "860", "ok", "Actief", False),
        ("closed", "Voorbeeld Bouw Delfland", "—", "Werkgever", "Delfland", "1", "3", "0", "0", "bad", "KvK mislukt", False),
        ("closed", "Tuincentrum De Voorbeeldhof", "6•••••31", "Werkgever", "Westland", "1", "5", "2", "80", "ok", "Actief", False),
        ("closed", "Voorbeeld Logistiek Oost BV", "5•••••90", "Werkgever", "Oost-Nederland", "3", "22", "9", "410", "wn", "Inactief 96 d", False),
        ("closed", "Horeca Voorbeeldplein", "4•••••18", "Werkgever", "Den Haag", "2", "8", "6", "150", "ok", "Actief", False),
        ("closed", "Voorbeeld Techniek Westland", "3•••••64", "Werkgever", "Westland", "1", "6", "4", "320", "ok", "Actief", False),
    ]
    rows = []
    for kind, n, kvk, t, r, v, u, vac, tok, sc, st, sel in orgs:
        if kind == "child":
            name = f'<span class="indent"></span>{n}'
        else:
            name = f'<div style="display:flex;align-items:center"><span class="caret">{ic("chevd" if kind=="open" else "chevr")}</span><div><b>{n}</b><div class="muted sm">{t} · {v} vest.</div></div></div>'
        rows.append(f'<tr class="{"sel" if sel else ("hl" if kind=="child" else "")}"><td>{name}</td><td class="mono">{kvk}</td><td class="muted">{r}</td><td class="num">{u}</td><td class="num">{vac}</td><td class="num">{tok}</td><td><span class="dotst {"" if sc=="ok" else sc}">{st}</span></td></tr>')
    body = f'''<div class="ph"><div><h1>Bedrijven &amp; vestigingen</h1><p>Klanten en intermediairs met hun vestigingen. Lobsy’s eigen gegevens staan onder Platforminstellingen.</p></div>
<div class="act"><span class="btn">{ic("dl")}Exporteren</span><span class="btn pri">{ic("plus")}Organisatie toevoegen</span></div></div>
<div class="row" style="grid-template-columns:minmax(0,1fr) 320px;align-items:start">
<div class="card" style="overflow:hidden">
<div class="fbar"><div class="inp w" style="width:220px">{ic("search")}Zoek op naam of KvK</div><span class="dd">Type <em>Alle</em>{ic("chevd")}</span><span class="dd">Regio <em>Alle</em>{ic("chevd")}</span><span class="dd">Status <em>Alle</em>{ic("chevd")}</span><span style="flex:1"></span><span class="seg"><span class="on">Boom</span><span>Plat</span></span></div>
<div class="note">{ic("info")}3 registraties wachten op KvK-controle of overname. <a>Aanvragen bekijken</a></div>
<table><thead><tr><th>Organisatie</th><th>KvK</th><th>Regio</th><th class="num">Gebr.</th><th class="num">Vac.</th><th class="num">Tokens</th><th>Status</th></tr></thead><tbody>{"".join(rows)}</tbody></table>
<div class="pag"><span>8 van 312 organisaties</span><span class="sp"></span><span class="btn sm">{ic("chevl")}</span><span class="btn sm">{ic("chevr")}</span></div></div>
<div class="card"><div class="ch" style="padding-bottom:4px"><span class="ico">{ic("building")}</span><div><h2>Voorbeeld Kwekerij Westgaarde BV</h2><div class="muted sm">Werkgever · klant sinds 02-2025</div></div></div>
<div style="padding:8px 16px 14px;display:flex;flex-direction:column;gap:12px">
<dl class="kv"><dt>KvK</dt><dd class="mono" style="color:var(--text)">9•••••12 <span class="pill ok" style="height:18px">{ic("check")}Geverifieerd</span></dd><dt>Regio</dt><dd>Westland</dd><dt>Domein</dt><dd>westland.lobsy.nl</dd><dt>Enterprisemanager</dt><dd>P. v••• D••••</dd><dt>Tokensaldo</dt><dd><b>1.240</b> <span class="muted">· 80 goodwill</span></dd><dt>Pakket</dt><dd>Groei · € 149 / mnd</dd></dl>
<div class="warnbox">{ic("alert")}<div><b>Overname aangevraagd</b><br>Voorbeeld FlexWerk BV wil vestiging Poeldijk overnemen (9 gebruikers, 3 vacatures). Aangevraagd door R. S•••• op 29-09-2026.</div></div>
<span class="btn pri" style="justify-content:center">{ic("flag")}Overname beoordelen</span>
<div style="display:grid;grid-template-columns:1fr 1fr;gap:8px"><span class="btn" style="justify-content:center;padding:0 6px">{ic("gift")}Tokens geven</span><span class="btn" style="justify-content:center;padding:0 6px">{ic("users")}38 gebruikers</span></div>
<span class="btn ghost" style="justify-content:center">{ic("ext")}Volledig openen</span>
</div></div></div>'''
    return page("orgs", [], ["Beheer", "Organisaties", "Bedrijven & vestigingen"], body)

def setting(title, desc, ctl, meta, imp="", chg=False, tag=""):
    return f'<div class="set{" chg" if chg else ""}"><h3>{title}{tag}</h3><div class="ctl">{ctl}</div><p>{desc}</p><div class="meta">{ic("clock")}{meta}</div>{f"<div class=imp>{imp}</div>" if imp else ""}</div>'

def d4():
    sw = lambda on, dis=False: f'<span class="sw{" on" if on else ""}{" dis" if dis else ""}"></span>'
    g1 = setting("Werkgevers actief", "Werkgevers, vestigingen en intermediairs kunnen inloggen, vacatures plaatsen en matchen.", sw(True), "Laatst gewijzigd door Dennis B. · 12-09-2026",
        f'<div class="warnbox">{ic("alert")}<div><b>Impact bij uitzetten:</b> 486 werkgeversaccounts kunnen niet meer inloggen, 1.146 vacatures gaan offline en 6 achtergrondtaken pauzeren. Er wordt niets verwijderd; aanzetten herstelt alles.</div></div>') + \
        setting("Mijn Paspoort (nieuw kandidaatprofiel)", "Kandidaten zien het nieuwe DNA-paspoort en de ontdekkingsreis in plaats van het oude profiel.", sw(True), "Gewijzigd, nog niet opgeslagen · was: Uit", chg=True, tag=' <span class="pill info">Nieuw</span>')
    g2 = setting("AI-vacaturemoderatie", "Nieuwe en gewijzigde vacatureteksten worden automatisch gecontroleerd op discriminatie en misleiding.", sw(True), "Laatst gewijzigd door Sanne K. · 03-09-2026") + \
        setting("Gratis publiceren", "Werkgevers publiceren zonder tokens tot en met de gekozen datum.", f'<span class="field">31-10-2026 {ic("clock")}</span>', "Laatst gewijzigd door Dennis B. · vandaag 09:42")
    g3 = setting("Tweestapsverificatie (authenticator)", "Beheerders en werkgevers loggen in met een code uit een authenticator-app.", sw(True), "Laatst gewijzigd door Dennis B. · 01-06-2026",
        f'<div class="dangerbox">{ic("alert")}<div><b>Niet uitzetten in Productie.</b> Zonder 2FA zijn beheerdersaccounts alleen met een wachtwoord beveiligd. Uitzetten vraagt een tweede beheerder.</div></div>') + \
        setting("Sessie-timeout bij inactiviteit", "Gebruikers worden na deze periode zonder activiteit uitgelogd.", '<span class="field">30 <span class="muted">min</span></span>', "Standaard · nooit gewijzigd") + \
        setting("Beheerders melden bij support-toegang", "Alle beheerders krijgen een e-mail zodra iemand tijdelijke toegang tot persoonsgegevens krijgt.", sw(True), "Laatst gewijzigd door Sanne K. · 20-08-2026")
    g4 = setting("Activatielinks tonen bij registratie", "Voor demo’s: toont de activatielink direct in plaats van via e-mail.", sw(True), "Vergrendeld: kan alleen aan in Acceptatie", tag=' <span class="pill wn">Alleen in Acceptatie</span>')
    def grp(t, d, inner): return f'<div class="card" style="overflow:hidden;margin-bottom:var(--space-3)"><div class="ch" style="padding-bottom:8px"><div><h2>{t}</h2><div class="muted sm">{d}</div></div></div>{inner}</div>'
    hist = [("Mijn Paspoort", "Uit → Aan", "Dennis B. · nu (niet opgeslagen)"), ("Gratis publiceren t/m", "30-09 → 31-10-2026", "Dennis B. · vandaag 09:42"),
            ("Werkgevers actief", "Uit → Aan", "Dennis B. · 12-09-2026"), ("AI-vacaturemoderatie", "Uit → Aan", "Sanne K. · 03-09-2026"), ("Support-toegang melden", "Uit → Aan", "Sanne K. · 20-08-2026")]
    hl = "".join(f'<li><b>{a}</b><p>{b}</p><p>{c}</p></li>' for a, b, c in hist)
    body = f'''<div class="ph"><div><h1>Functies</h1><p>Zet onderdelen van het platform aan of uit. Elke wijziging wordt met reden gelogd in het auditlog.</p></div>
<div class="act"><span class="seg"><span class="on">Acceptatie</span><span>Productie (alleen lezen)</span></span></div></div>
<div class="row" style="grid-template-columns:minmax(0,1fr) 300px;align-items:start">
<div>{grp("Platform-modus", "Grote schakelaars die bepalen wie het platform kan gebruiken.", g1)}
{grp("Vacatures", "Controle en prijsacties rond vacatures.", g2)}
{grp("Beveiliging", "Inloggen, sessies en toegang tot persoonsgegevens.", g3)}
{grp("Demo &amp; test", "Hulpmiddelen die nooit in Productie aan mogen staan.", g4)}</div>
<div class="card" style="position:sticky;top:72px"><div class="ch"><h2>Wijzigingen</h2><span class="sp"></span><a>Auditlog{ic("chevr")}</a></div><ul class="hist">{hl}</ul></div></div>'''
    bar = f'<div class="savebar"><span class="ico" style="background:color-mix(in srgb,var(--surface) 14%,transparent);color:var(--surface)">{ic("toggle")}</span><div class="chgl"><b>1 wijziging:</b> Mijn Paspoort <s>Uit</s> → Aan</div><span style="flex:1"></span><span class="btn ghost">Annuleren</span><span class="btn w">{ic("check")}Opslaan en loggen</span></div>'
    return page("features", [], ["Beheer", "Platforminstellingen", "Functies"], body, bar)
