from ui import *

def d5():
    rows = [
        ("29-09 09:15:04", "SK", "Sanne K.", "bad", "2FA gereset", "M. de V•••• (#10482)", "Nieuwe telefoon · ticket #2231", "ok", "c7f3-91ab"),
        ("29-09 09:12:40", "SK", "Sanne K.", "wn", "Support-toegang", "M. de V•••• · 15 min", "Controle vóór 2FA-reset", "ok", "c7f3-90e2"),
        ("29-09 07:58:11", "—", "Onbekend", "bad", "Beheerderslogin mislukt", "admin@l•••.nl · 3×", "—", "bad", "a102-44f0"),
        ("29-09 09:42:18", "DB", "Dennis B.", "info", "Instelling gewijzigd", "Gratis publiceren t/m", "Actie oktober verlengd", "ok", "b88e-1c03"),
        ("28-09 16:20:55", "SK", "Sanne K.", "info", "Tokens toegekend", "Tuincentrum De Voorbeeldhof · 80", "Goodwill: betaalstoring", "ok", "b61d-7a9c"),
        ("28-09 11:03:27", "DB", "Dennis B.", "info", "Rol toegevoegd", "J. B•••• · Regiomanager", "Nieuwe regio Maasland", "ok", "b5f0-2e11"),
        ("28-09 10:47:02", "DB", "Dennis B.", "line", "Export gemaakt", "Gebruikers · geanonimiseerd", "Maandrapport september", "ok", "b5e9-0d7a"),
        ("28-09 03:00:00", "⚙", "Systeem", "line", "Bewaartermijn toegepast", "38 accounts geanonimiseerd", "Geen activiteit > 24 mnd", "ok", "sys-0928"),
        ("27-09 14:31:19", "SK", "Sanne K.", "wn", "Sessies beëindigd", "2 gebruikers · 5 sessies", "Verdachte login gemeld", "ok", "b2a4-6f18"),
        ("26-09 12:05:44", "DB", "Dennis B.", "bad", "Verwijderverzoek uitgevoerd", "Kandidaat #47109", "AVG art. 17 · verzoek 12-09", "ok", "af91-3b27"),
    ]
    tr = "".join(f'<tr><td class="mono" style="color:var(--text)">{t[:11]}</td><td>{n}</td><td><span class="pill {c}">{act}</span></td><td style="max-width:236px;overflow:hidden;text-overflow:ellipsis">{o}<div class="muted sm" style="overflow:hidden;text-overflow:ellipsis">{r}</div></td><td><span class="dotst {"" if res=="ok" else "bad"}">{"Gelukt" if res=="ok" else "Geblokkeerd"}</span></td><td class="mono">{cid}</td></tr>' for t, a, n, c, act, o, r, res, cid in rows)
    body = f'''<div class="ph"><div><h1>Beveiliging &amp; audit</h1><p>Wie deed wat, wanneer en waarom. Het auditlog is onveranderbaar en wordt 7 jaar bewaard.</p></div>
<div class="act"><span class="btn">{ic("dl")}Exporteren (CSV)</span></div></div>
<div class="tabs"><span class="on">{ic("list")}Auditlog</span><span>{ic("eye")}Gegevensinzage</span><span>{ic("lock")}2FA &amp; sessies</span><span>{ic("shield2")}Privacy &amp; AVG <span class="cnt">1</span></span><span>{ic("terminal")}Systeemlogs</span></div>
<div class="row" style="grid-template-columns:minmax(0,1fr) 280px;align-items:start">
<div class="card" style="overflow:hidden">
<div class="fbar"><div class="inp" style="width:210px">{ic("search")}Zoek object of correlatie-id</div><span class="dd">Actie <em>Alle</em>{ic("chevd")}</span><span class="dd">Door <em>Iedereen</em>{ic("chevd")}</span><span class="dd on">Periode <em style="color:var(--brand)">7 dagen</em>{ic("chevd")}</span><span class="dd">Resultaat <em>Alle</em>{ic("chevd")}</span></div>
<table><thead><tr><th>Tijd</th><th>Door</th><th>Actie</th><th>Object · reden</th><th>Resultaat</th><th>Correlatie</th></tr></thead><tbody>{tr}</tbody></table>
<div class="pag"><span>10 van 1.284 gebeurtenissen</span><span class="sp"></span><span class="btn sm">{ic("chevl")}</span><span class="btn sm">{ic("chevr")}</span></div></div>
<div style="display:flex;flex-direction:column;gap:var(--space-4)">
<div class="card"><div class="ch"><h2>Actieve support-toegang</h2><span class="sp"></span><span class="pill wn">1</span></div>
<div style="padding:0 16px 14px"><div class="box"><b>Sanne K. → M. de V••••</b><div class="muted sm">Nog 6 min · reden: controle telefoonnummer</div><span class="btn dng sm" style="margin-top:8px">{ic("x")}Nu intrekken</span></div></div></div>
<div class="card"><div class="ch"><h2>Privacy &amp; AVG</h2></div><ul class="list">
<li>{ic("eyeoff")}<span>Gegevens standaard gemaskeerd</span><span class="sp"></span><span class="pill ok">Aan</span></li>
<li>{ic("clock")}<span>Bewaartermijn-taak</span><span class="sp"></span><span class="muted sm">Nacht 03:00</span></li>
<li>{ic("alert")}<span>Open verzoeken</span><span class="sp"></span><span class="pill bad">1 · 27 d</span></li></ul></div>
<div class="card"><div class="ch"><h2>Beheerders</h2></div><ul class="list">
<li>{ic("lock")}<span>Met 2FA</span><span class="sp"></span><span class="pill ok">4 / 4</span></li>
<li>{ic("alert")}<span>Mislukte logins (24 u)</span><span class="sp"></span><span class="pill bad">3</span></li>
<li>{ic("key")}<span>Laatst gewijzigde rol</span><span class="sp"></span><span class="muted sm">28-09</span></li></ul></div>
</div></div>'''
    return page("audit", [], ["Beheer", "Beveiliging & audit", "Auditlog"], body)

def d6():
    def kpi(l, i, v, d):
        return f'<div class="card kpi"><small>{ic(i)}{l}</small><div class="v">{v}</div><div class="dl">{d}</div></div>'
    k = kpi("Omzet september", "euro", "€ 18.420", '<span class="up">+11%</span> vs aug') + kpi("Tokens verkocht", "coins", "9.860", '<span class="up">+640</span> vs aug') + \
        kpi("Open bij Mollie", "clock", "3 · € 412", "oudste 2 dagen") + kpi("Btw-buffer", "receipt", "€ 3.868", "Q3 aangifte 31-10") + kpi("Uit te betalen", "send", "€ 2.910", "5 ambassadeurs")
    tx = [("29-09 09:31", "LB-2026-0947", "Voorbeeld Kwekerij Westgaarde BV", "Tokenbundel 500", "€ 1.089,00", "ok", "Betaald", "iDEAL"),
          ("29-09 08:02", "LB-2026-0946", "Horeca Voorbeeldplein", "Pakket Start", "€ 59,00", "wn", "Open", "iDEAL"),
          ("28-09 17:44", "LB-2026-0945", "Zorggroep Voorbeeld-Maasland", "Pakket Enterprise", "€ 1.450,00", "ok", "Betaald", "Incasso"),
          ("28-09 15:20", "LB-2026-0944", "Voorbeeld FlexWerk BV", "Tokenbundel 250", "€ 568,00", "ok", "Betaald", "Creditcard"),
          ("28-09 11:05", "LB-2026-0943", "Voorbeeld Logistiek Oost BV", "PushBom 7 dagen", "€ 89,00", "bad", "Mislukt", "iDEAL"),
          ("27-09 16:12", "LB-2026-0942", "Tuincentrum De Voorbeeldhof", "Tokenbundel 100", "€ 242,00", "line", "Terugbetaald", "iDEAL"),
          ("27-09 10:40", "LB-2026-0941", "Voorbeeld Techniek Westland", "Pakket Groei", "€ 149,00", "ok", "Betaald", "Incasso"),
          ("26-09 14:03", "LB-2026-0940", "Voorbeeld Bouw Delfland", "Tokenbundel 100", "€ 242,00", "wn", "Open", "iDEAL"),
          ("26-09 09:18", "LB-2026-0939", "Zorggroep Voorbeeld-Maasland", "Flex & talent", "€ 320,00", "ok", "Betaald", "Incasso")]
    rows = "".join(f'<tr><td class="mono" style="color:var(--text)">{t[:5]}</td><td class="mono">{f}</td><td><b>{o}</b></td><td>{p}<div class="muted sm">{m}</div></td><td><span class="pill {c}">{s}</span></td><td class="num"><b>{a}</b></td><td class="num"><span class="ra">{ic("more")}</span></td></tr>' for t, f, o, p, a, c, s, m in tx)
    pay = [("GM", "G. M••••", "Ambassadeur · 12 aanm.", "€ 840,00"), ("EW", "E. W••••", "Salesmanager · commissie", "€ 1.120,00"), ("KL", "K. L••••", "Ambassadeur · 6 aanm.", "€ 420,00"), ("HB", "H. B••••", "Ambassadeur · 5 aanm.", "€ 350,00"), ("ST", "S. T••••", "Ambassadeur · 3 aanm.", "€ 180,00")]
    pl = "".join(f'<li><span class="av" style="width:26px;height:26px">{a}</span><div style="min-width:0"><b>{n}</b><div class="muted sm" style="white-space:nowrap">{d}</div></div><span class="sp"></span><b style="font-variant-numeric:tabular-nums;white-space:nowrap">{v}</b></li>' for a, n, d, v in pay)
    body = f'''<div class="ph"><div><h1>Omzet &amp; transacties</h1><p>Betalingen via Mollie, facturen en tokenaankopen. Prijzen staan bij Prijzen &amp; pakketten.</p></div>
<div class="act"><div class="seg"><span>Juli</span><span>Augustus</span><span class="on">September</span><span>Q3</span></div><span class="btn">{ic("dl")}Export voor boekhouding</span></div></div>
<div class="kpis">{k}</div>
<div class="row" style="grid-template-columns:minmax(0,1fr) 300px;align-items:start">
<div class="card" style="overflow:hidden">
<div class="fbar"><div class="inp" style="width:220px">{ic("search")}Zoek factuur of organisatie</div><span class="dd">Status <em>Alle</em>{ic("chevd")}</span><span class="dd">Product <em>Alle</em>{ic("chevd")}</span><span class="dd">Methode <em>Alle</em>{ic("chevd")}</span></div>
<table class="fin"><style>.fin td,.fin th{{padding-inline:8px}}</style><thead><tr><th>Datum</th><th>Factuur</th><th>Organisatie</th><th>Product</th><th>Mollie</th><th class="num">Incl. btw</th><th></th></tr></thead><tbody>{rows}</tbody></table>
<div class="pag"><span>9 van 214 transacties</span><span class="sp"></span><span class="btn sm">{ic("chevl")}</span><span class="btn sm">{ic("chevr")}</span></div></div>
<div style="display:flex;flex-direction:column;gap:var(--space-4)">
<div class="card"><div class="ch"><h2>Ter goedkeuring</h2><span class="sp"></span><span class="pill wn">5</span></div><ul class="list">{pl}</ul>
<div style="padding:10px 16px 14px;border-top:1px solid var(--border);display:flex;align-items:center;gap:8px"><span class="muted sm">Totaal <b style="color:var(--text)">€ 2.910,00</b></span><span style="flex:1"></span><span class="btn pri sm">{ic("check")}Alle 5 goedkeuren</span></div></div>
<div class="card"><div class="ch"><h2>Btw Q3 2026</h2><span class="sp"></span><a>Aangifte{ic("chevr")}</a></div><ul class="list">
<li><span>Omzet excl. btw</span><span class="sp"></span><b>€ 42.610</b></li><li><span>Af te dragen btw (21%)</span><span class="sp"></span><b>€ 8.948</b></li><li><span>Gereserveerd in buffer</span><span class="sp"></span><span class="pill ok">€ 3.868 (sep)</span></li></ul></div>
</div></div>'''
    return page("fin", [], ["Beheer", "Financiën", "Omzet & transacties"], body, demo="l")

def m1():
    k = "".join(f'<div class="card kpi"><small>{l}</small><div class="v">{v}</div><div class="dl"><span class="{c}">{d}</span></div></div>' for l, v, d, c in
                [("Actieve kandidaten", "4.812", "+6,2%", "up"), ("Actieve werkgevers", "312", "+14", "up"), ("Vacatures live", "1.146", "−1,8%", "down"), ("Omzet sep", "€ 18.420", "+11%", "up")])
    todo = [("bad", "alert", "KvK-controle mislukt", "Voorbeeld Bouw Delfland"), ("wn", "flag", "Overnameverzoek", "FlexWerk → Poeldijk"),
            ("wn", "shield2", "2 vacatures gemarkeerd", "AI-moderatie"), ("", "receipt", "Uitbetaling goedkeuren", "5 personen · € 2.910")]
    tl = "".join(f'<li><span class="ico {c}">{ic(i)}</span><div><b>{t}</b><small>{s}</small></div><span class="sp"></span>{ic("chevr")}</li>' for c, i, t, s in todo)
    st = "".join(f'<li style="min-height:44px"><span class="dotst {c}"></span><span>{n}</span><span class="sp"></span><span class="muted sm">{v}</span></li>' for c, n, v in
                 [("", "Mollie", "Operationeel"), ("wn", "KvK-API", "Traag")])
    body = f'''<main class="mmain"><div class="crumb">Beheer {ic("chevr")} <b>Dashboard</b></div><h1>Goedemorgen, Dennis</h1><p class="muted sm">Di 29 sep · 7 dingen vragen aandacht</p>
<div class="mkpis">{k}</div>
<div class="card" style="overflow:hidden;margin-bottom:14px"><div class="ch"><h2>Te doen</h2><span class="pill">7</span><span class="sp"></span><a>Alles</a></div><ul class="mlist" style="border-top:1px solid var(--border)">{tl}</ul></div>
<div class="card" style="overflow:hidden"><div class="ch"><h2>Systeemstatus</h2></div><ul class="mlist" style="border-top:1px solid var(--border)">{st}</ul></div></main>'''
    return mpage(body)

def m2():
    body = f'''<main class="mmain"><div class="crumb">Gebruikers {ic("chevr")} <b>M. de V••••</b></div>
<div style="display:flex;gap:12px;align-items:center;margin-top:6px"><span class="av" style="width:48px;height:48px">MV</span><div><h1 style="font-size:var(--text-lg)">M. de V••••</h1><div class="muted sm">Vestigingsmanager · Westgaarde BV</div></div></div>
<div class="card" style="margin-top:14px;padding:14px"><b>Tweestapsverificatie</b><div class="muted sm">Aan · authenticator-app</div></div></main>'''
    sheet = f'''<div class="mscrim"></div><div class="sheet"><div class="grab"></div>
<div style="display:flex;gap:10px;align-items:flex-start"><span class="ico bad">{ic("refresh")}</span><div><h2 style="font-size:var(--text-lg);font-weight:600;line-height:1.3">2FA resetten voor M. de V••••?</h2>
<p class="muted sm" style="margin-top:4px">De authenticator wordt ontkoppeld en alle 2 sessies worden beëindigd. Bij de volgende login stelt de gebruiker 2FA opnieuw in.</p></div></div>
<label class="lbl">Reden (verplicht)</label><div class="ta">Nieuwe telefoon, identiteit gecontroleerd via support-ticket #2231</div>
<label class="lbl">Jouw 2FA-code</label><div class="otp"><span>4</span><span>8</span><span>1</span><span class="f">|</span><span></span><span></span></div>
<div class="warnbox" style="margin-top:14px">{ic("info")}<div>Komt in het auditlog met jouw naam en reden. M. de V•••• krijgt hierover een e-mail.</div></div>
<div style="display:grid;grid-template-columns:1fr 1fr;gap:10px;margin-top:16px"><span class="btn" style="height:44px;justify-content:center">Annuleren</span><span class="btn dngp" style="height:44px;justify-content:center">{ic("refresh")}2FA resetten</span></div></div>'''
    return mpage(body, sheet)
