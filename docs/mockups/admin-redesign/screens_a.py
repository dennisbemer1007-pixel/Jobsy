from ui import *

def kpi(label, icon, v, delta, cls, sp):
    return f'<div class="card kpi"><small>{ic(icon)}{label}</small><div class="v">{v}</div><div class="dl"><span class="{cls}">{delta}</span>{spark(sp)}</div></div>'

def d1():
    k = "".join([
        kpi("Actieve kandidaten", "user", "4.812", "+6,2% <span class=muted>vs aug</span>", "up", [3,4,4,5,5,6,6,7,8]),
        kpi("Actieve werkgevers", "building", "312", "+14 <span class=muted>deze maand</span>", "up", [5,5,6,6,6,7,7,8,8]),
        kpi("Vacatures live", "brief", "1.146", "−1,8% <span class=muted>vs aug</span>", "down", [8,8,7,8,7,7,6,7,6]),
        kpi("Sollicitaties (7 d)", "send", "2.390", "+9,4% <span class=muted>vs vorige</span>", "up", [4,5,4,6,6,7,6,8,9]),
        kpi("Omzet september", "euro", "€ 18.420", "+11% <span class=muted>vs aug</span>", "up", [3,4,5,5,6,6,7,8,9]),
    ])
    todo = [
        ("bad", "alert", "KvK-controle mislukt", "Voorbeeld Bouw Delfland · nummer niet gevonden", "Organisaties", "2 u", "Controleren"),
        ("wn", "flag", "Overnameverzoek vestiging", "Voorbeeld FlexWerk BV → vestiging Poeldijk", "Organisaties", "5 u", "Beoordelen"),
        ("wn", "shield2", "2 vacatures gemarkeerd door moderatie", "AI: mogelijk discriminerende tekst (leeftijd)", "Vacatures", "Vandaag", "Bekijken"),
        ("", "receipt", "Uitbetaling ter goedkeuring", "5 ambassadeurs · € 2.910,00", "Financiën", "Gisteren", "Goedkeuren"),
        ("bad", "shield2", "AVG-verwijderverzoek", "Kandidaat #48213 · wettelijke termijn nog 27 dagen", "Privacy & AVG", "3 d", "Afhandelen"),
        ("", "send", "6 nieuwe feedbackmeldingen", "Waarvan 2 met label ‘bug’", "Overzicht", "Deze week", "Openen"),
    ]
    rows = "".join(f'<tr><td style="width:44px"><span class="ico {c}">{ic(i)}</span></td><td><b>{t}</b><div class="muted sm">{s}</div></td><td><span class="pill line">{g}</span></td><td class="muted">{w}</td><td class="num"><span class="btn sm">{a}</span></td></tr>' for c, i, t, s, g, w, a in todo)
    status = [("", "Mollie betalingen", "Operationeel", ""), ("wn", "KvK-API", "Traag · 2,8 s gem.", "wn"), ("", "E-mail (Postmark)", "Operationeel", ""),
              ("", "AI-moderatie", "Operationeel", ""), ("wn", "Achtergrondtaken", "11 / 12 · ATS-import gepauzeerd", "wn")]
    st = "".join(f'<li><span class="dotst {c}"></span><span>{n}</span><span class="sp"></span><span class="muted sm">{v}</span></li>' for c, n, v, _ in status)
    modes = [("Werkgevers actief", "ok", "Aan"), ("Mijn Paspoort", "line", "Uit"), ("AI-vacaturemoderatie", "ok", "Aan"), ("Tweestapsverificatie", "ok", "Aan")]
    md = "".join(f'<li><span>{n}</span><span class="sp"></span><span class="pill {c}">{v}</span></li>' for n, c, v in modes)
    acts = [("DB", "Dennis B.", "Instelling gewijzigd: Gratis publiceren t/m 31-10-2026", "09:42"),
            ("SK", "Sanne K.", "2FA gereset voor M. de V•••• (reden: nieuwe telefoon)", "09:15"),
]
    ac = "".join(f'<li><span class="av" style="width:26px;height:26px">{a}</span><span><b>{n}</b> <span class="muted">{t}</span></span><span class="sp"></span><span class="muted sm">{w}</span></li>' for a, n, t, w in acts)
    body = f'''<div class="ph"><div><h1>Goedemorgen, Dennis</h1><p>Dinsdag 29 september 2026 · alles wat vandaag aandacht vraagt op één plek.</p></div>
<div class="act"><div class="seg"><span>Vandaag</span><span class="on">7 dagen</span><span>30 dagen</span><span>Kwartaal</span></div><span class="btn">{ic("dl")}Exporteren</span></div></div>
<div class="kpis">{k}</div>
<style>.list li{{padding:7px 16px}}</style><div class="row" style="grid-template-columns:minmax(0,1fr) 340px">
<div class="card" style="overflow:hidden"><div class="ch"><h2>Te doen</h2><span class="pill">7 open</span><span class="sp"></span><a>Alles bekijken{ic("chevr")}</a></div>
<table><thead><tr><th></th><th>Onderwerp</th><th>Onderdeel</th><th>Sinds</th><th class="num">Actie</th></tr></thead><tbody>{rows}</tbody></table></div>
<div style="display:flex;flex-direction:column;gap:var(--space-4)">
<div class="card"><div class="ch"><h2>Systeemstatus</h2><span class="sp"></span><a>Systeemlogs{ic("chevr")}</a></div><ul class="list">{st}</ul></div>
<div class="card"><div class="ch"><h2>Platform-modus</h2><span class="sp"></span><a>Functies{ic("chevr")}</a></div><ul class="list">{md}</ul></div>
</div></div>
<div class="card" style="margin-top:var(--space-4)"><div class="ch"><h2>Recente beheeracties</h2><span class="sp"></span><a>Auditlog{ic("chevr")}</a></div><ul class="list" style="display:grid;grid-template-columns:1fr 1fr">{ac}</ul></div>'''
    return page("dash", [], ["Beheer", "Overzicht", "Dashboard"], body)

def d2():
    users = [
        ("MV", "M. de V••••", "m.d•••@k•••.nl", "Vestigingsmanager", "Voorbeeld Kwekerij Westgaarde", "ok", "Aan", "Vandaag 08:51", "ok", "Actief", True),
        ("JB", "J. B••••", "j.b•••@z•••.nl", "Regiomanager", "Zorggroep Voorbeeld-Maasland", "ok", "Aan", "Vandaag 08:12", "ok", "Actief", True),
        ("AK", "A. K••••", "a.k•••@g•••.com", "Kandidaat", "—", "line", "Uit", "Gisteren", "ok", "Actief", False),
        ("RS", "R. S••••", "r.s•••@v•••.nl", "Intermediair", "Voorbeeld FlexWerk BV", "ok", "Aan", "Gisteren", "ok", "Actief", False),
        ("LH", "L. H••••", "l.h•••@o•••.nl", "Kandidaat", "—", "line", "Uit", "27-09-2026", "wn", "Uitgenodigd", False),
        ("TD", "T. D••••", "t.d•••@b•••.nl", "Enterprisemanager", "Voorbeeld Bouw Delfland", "wn", "Niet ingesteld", "26-09-2026", "ok", "Actief", False),
        ("NP", "N. P••••", "n.p•••@h•••.nl", "Vestigingsmanager", "Horeca Voorbeeldplein", "ok", "Aan", "25-09-2026", "ok", "Actief", False),
        ("FO", "F. O••••", "f.o•••@g•••.com", "Kandidaat", "—", "line", "Uit", "24-09-2026", "bad", "Geblokkeerd", False),
        ("EW", "E. W••••", "e.w•••@l•••.nl", "Salesmanager", "Lobsy", "ok", "Aan", "24-09-2026", "ok", "Actief", False),
        ("IY", "I. Y••••", "i.y•••@t•••.nl", "Vestigingsmanager", "Voorbeeld Techniek Westland", "ok", "Aan", "22-09-2026", "ok", "Actief", False),
        ("GM", "G. M••••", "g.m•••@g•••.com", "Ambassadeur", "Lobsy", "wn", "Niet ingesteld", "19-09-2026", "ok", "Actief", False),
    ]
    rows = "".join(f'<tr class="{"sel" if s else ""}"><td style="width:36px">{cb(s)}</td><td><div class="who"><span class="av">{a}</span><div><b>{n}</b><small>{e}</small></div></div></td><td>{r}</td><td class="muted">{o}</td><td><span class="pill {fc}">{ic("lock")}{f}</span></td><td class="muted">{l}</td><td><span class="dotst {"" if sc=="ok" else sc}">{st}</span></td><td class="num"><span class="ra">{ic("more")}</span></td></tr>' for a, n, e, r, o, fc, f, l, sc, st, s in users)
    body = f'''<div class="ph"><div><h1>Alle gebruikers</h1><p>Persoonsgegevens zijn standaard gemaskeerd (AVG). Inzien kan alleen via support-toegang.</p></div>
<div class="act"><span class="btn">{ic("dl")}Export (geanonimiseerd)</span><span class="btn pri">{ic("plus")}Gebruiker uitnodigen</span></div></div>
<div class="tabs"><span class="on">Alle <span class="cnt">5.325</span></span><span>Kandidaten <span class="cnt">4.812</span></span><span>Werkgevers <span class="cnt">486</span></span><span>Sales &amp; partners <span class="cnt">23</span></span><span>Beheerders <span class="cnt">4</span></span></div>
<div class="card" style="overflow:hidden">
<div class="fbar"><div class="inp w">{ic("search")}Zoek op naam, e-mail of ID</div><span class="dd">Rol <em>Alle</em>{ic("chevd")}</span><span class="dd on">2FA <em style="color:var(--brand)">Alle</em>{ic("chevd")}</span><span class="dd">Status <em>Alle</em>{ic("chevd")}</span><span class="dd">Organisatie <em>Alle</em>{ic("chevd")}</span><span class="sp" style="flex:1"></span><span class="dd">{ic("cols")}Kolommen</span></div>
<div class="bulk"><span class="cb mid"></span><b>2 geselecteerd</b><span style="flex:1"></span><span class="btn">{ic("logout")}Sessies beëindigen</span><span class="btn">{ic("mail")}Uitnodiging opnieuw</span><span class="btn">{ic("ban")}Blokkeren</span></div>
<table><thead><tr><th></th><th>Gebruiker</th><th>Rol</th><th>Organisatie</th><th>2FA</th><th>Laatst ingelogd</th><th>Status</th><th></th></tr></thead><tbody>{rows}</tbody></table>
<div class="pag"><span>1–11 van 5.325</span><span class="sp"></span><span class="btn sm">{ic("chevl")}</span><span class="btn sm" style="background:var(--accent-soft);color:var(--brand)">1</span><span class="btn sm">2</span><span class="btn sm">3</span><span>…</span><span class="btn sm">485</span><span class="btn sm">{ic("chevr")}</span></div></div>'''
    drawer = f'''<div class="scrim"></div><aside class="drawer">
<div class="dh"><span class="av">MV</span><div><h2>M. de V••••</h2><div class="muted sm">Vestigingsmanager · Voorbeeld Kwekerij Westgaarde BV · Naaldwijk</div>
<div style="display:flex;gap:6px;margin-top:6px"><span class="pill ok">Actief</span><span class="pill info">{ic("lock")}2FA aan</span><span class="pill line">ID 10482</span></div></div><span class="x">{ic("x")}</span></div>
<div class="dtabs"><span>Overzicht</span><span>Rollen</span><span class="on">Beveiliging</span><span>Activiteit</span></div>
<div class="db">
<div class="sec"><h3>{ic("phone")}Tweestapsverificatie</h3><div class="box" style="display:flex;align-items:center;gap:12px"><div style="flex:1"><b>Aan · authenticator-app</b><div class="muted sm">Ingesteld op 14-03-2026 · laatst gebruikt vandaag 08:51</div></div><span class="btn dng">{ic("refresh")}2FA resetten</span></div>
<p class="muted sm" style="margin-top:6px">Na een reset stelt de gebruiker bij de volgende login opnieuw 2FA in. Je geeft een reden op en bevestigt met je eigen 2FA-code. De actie komt in het auditlog en de gebruiker krijgt een e-mail.</p></div>
<div class="sec"><h3>{ic("clock")}Actieve sessies <span class="cnt" style="margin:0">2</span><span style="flex:1"></span><a class="btn ghost sm">Alle sessies beëindigen</a></h3>
<div class="box"><ul class="sess"><li>{ic("terminal")}<div><b>Chrome · Windows</b><div class="muted sm">84.•••.•••.12 · Naaldwijk · nu actief</div></div><span class="sp"></span><span class="pill ok">Huidig</span></li>
<li>{ic("phone")}<div><b>Safari · iPhone</b><div class="muted sm">62.•••.•••.201 · 27-09-2026 19:04</div></div><span class="sp"></span><span class="btn sm">Beëindigen</span></li></ul></div></div>
<div class="sec"><h3>{ic("eyeoff")}Persoonsgegevens</h3><div class="box"><dl class="kv"><dt>E-mail</dt><dd>m.d•••@k•••.nl</dd><dt>Telefoon</dt><dd>06 •• •• •• 47</dd><dt>Geboortedatum</dt><dd>•• - •• - 19••</dd></dl>
<div class="warnbox" style="margin-top:10px">{ic("info")}<div><b>Gemaskeerd volgens AVG.</b> Volledige gegevens nodig voor support? Vraag tijdelijke toegang aan (15 min, met reden). Dit wordt gelogd en de gebruiker krijgt bericht.</div></div>
<span class="btn" style="margin-top:10px">{ic("eye")}Support-toegang aanvragen</span></div></div>
</div>
<div class="df"><span class="btn dng">{ic("ban")}Blokkeren</span><span class="btn">{ic("mail")}Wachtwoordlink sturen</span><span style="flex:1"></span><span class="btn">Sluiten</span></div></aside>'''
    return page("users", [], ["Beheer", "Gebruikers & rollen", "Alle gebruikers"], body, drawer, demo="l")
