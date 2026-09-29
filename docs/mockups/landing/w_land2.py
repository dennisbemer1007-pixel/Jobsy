from ui import ic, LOGO, LOBSTER, MASCOT, vb
from w_common import em, blob, wave, mascot
from lp_map import mapsvg
from zw_scene import wheel, legend
CK = ic("check")

def _map_show():
    jobs = [("🏥", "Zorgmedewerker thuiszorg", "Kasteelhof Zorg · Naaldwijk", "12 min", "Past goed"),
            ("🔧", "Monteur installatietechniek", "Van Dijk Installatie · Delft", "24 min", "Past goed"),
            ("🌱", "Teeltmedewerker kas", "Groene Kas · Westland", "9 min", "Past redelijk")]
    jl = "".join(f'<div class="job">{em(a, "cream", "s")}<div><h5>{t}</h5><small>{c}</small><span class="pill ok" style="margin-top:6px;height:22px">{m}</span></div><div class="tt"><b>{mm}</b>fietsen</div></div>' for a, t, c, mm, m in jobs)
    return f'''<div class="card showmap"><div class="ml"><h3><i class="e">🗺️</i>De banenkaart</h3><p class="sm muted" style="margin-top:4px">Zie meteen welke banen je echt kunt halen.</p>
<div class="seg"><span class="on"><i class="e">🚲</i>Fiets</span><span><i class="e">🚌</i>OV</span><span><i class="e">🚗</i>Auto</span></div>
<p class="xs muted" style="margin-top:12px">Binnen <b style="color:var(--text)">30 minuten</b> van huis: <b style="color:var(--text)">38 banen</b></p>
<div style="margin-top:6px">{jl}</div></div>
<div class="mapc">{mapsvg(home=False)}<img class="homelob" src="{MASCOT}" alt="Jij"><div class="maplbl">{vb()}<span class="pill">Reistijd-cirkels</span></div>
<span class="ringl" style="left:374px;top:146px">15 min</span><span class="ringl" style="left:374px;top:76px">30 min</span><span class="ringl" style="left:374px;top:8px">45 min</span></div></div>'''

def _pp_show():
    nav = [("🧬", "Mijn DNA", True), ("🧪", "Mijn tests", False), ("✅", "Past deze baan?", False), ("🪜", "Carrière", False), ("📜", "Bewijzen", False)]
    n = "".join(f'<a class="{"on" if on else ""}"><i class="e">{e}</i>{t}</a>' for e, t, on in nav)
    rows = [("🤝", "Zo werk jij", "Samenwerker"), ("💛", "Dit vind je leuk", "Mensen helpen"), ("🏡", "Hier voel je je thuis", "Een warm team"), ("🧭", "Dit vind je belangrijk", "Zekerheid en zorg")]
    r = "".join(f'<div class="rw"><small><i class="e">{e}</i>{s}</small><b>{b}</b></div>' for e, s, b in rows)
    return f'''<div class="card showpp"><div class="spnav"><h3><i class="e">🪪</i>Mijn Paspoort</h3>{n}<p class="spnote">Van jou, voor jou. Jij kiest wat je deelt.</p></div>
<div class="spmain"><div>{wheel(200, 22)}</div><div class="sum"><div style="display:flex;gap:8px;margin-bottom:10px"><span class="pill wn">Binnenkort</span>{vb()}</div><h4>Dit ben jij</h4>
<p>Je werkt graag samen en je maakt af wat je belooft. Je krijgt energie van mensen helpen.</p><div class="rows">{r}</div></div></div></div>'''

def _card(e, tone, title, text, demo, badge=""):
    return f'''<div class="card fc"><div class="top">{em(e, tone)}<div class="sp"></div>{badge}</div><h4>{title}</h4><p>{text}</p><div class="demo">{demo}</div></div>'''

SOON = '<span class="pill wn">Binnenkort</span>'

def krijgt(zw=False):
    pp = _card("🪪", "peach", "Mijn Paspoort", "Alles over jou op één plek. Jij kiest wat je deelt.",
               '<div class="tabs"><span class="on">Mijn DNA</span><span>Mijn tests</span><span>Past deze baan?</span><span>Carrière</span><span>Bewijzen</span></div>'
               '<div class="bar" style="grid-template-columns:78px 1fr">Sociaal<b style="--w:82%"></b></div><div class="bar t2" style="grid-template-columns:78px 1fr">Praktisch<b style="--w:70%"></b></div><div class="bar t3" style="grid-template-columns:78px 1fr">Zorgvuldig<b style="--w:58%"></b></div>', SOON)
    oj = _card("🧭", "sky", "Ontdekkingsreis", "Stap voor stap ontdekken wie je bent. Pauzeren mag altijd.",
               f'<div class="jr"><div class="done"><i>{CK}</i>Zo werk jij</div><div class="done"><i>{CK}</i>Dit vind je leuk</div><div class="cur"><i>3</i>Hier voel je je thuis</div><div><i>4</i>Dit vind je belangrijk</div></div>', SOON)
    rp = _card("📘", "sun", "Jouw rapport", "De diepteanalyse: meer vragen en een persoonlijk rapport om te bewaren.",
               '<div class="rep"><h6>Dit ben jij · rapport</h6><div class="ln" style="width:92%"></div><div class="ln" style="width:76%"></div><div class="ln" style="width:84%"></div></div>'
               '<div class="price" style="margin-top:12px"><b>€ 2,99</b><span class="xs muted">eenmalig · na je gratis tests</span></div>')
    if zw:
        occ = [("Verpleegkundige", 82), ("Doktersassistent", 74), ("Sociaal werker", 69)]
        fit = _card("✅", "mint", "Past dit beroep bij mij?", "Kijk hoe goed een beroep bij je DNA past. En wat je nog kunt leren.",
                    '<div class="fit">' + "".join(f'<div class="occ">{o}<span class="m" style="--w:{v}%"></span><b>{v}%</b></div>' for o, v in occ) + '</div>')
        car = _card("🪜", "peach", "Droombaan-check", "Wat is je droombaan? Lobsy laat zien welke stappen je kunt zetten.",
                    f'<div class="path"><div class="now"><i></i><span><b>Nu</b><small>Zorghulp</small></span></div><div><i></i><span><b>Volgende stap</b><small>Mbo Verzorgende IG</small></span></div>'
                    f'<div class="goal"><i>{ic("star")}</i><span><b>Droombaan</b><small>Verpleegkundige</small></span></div></div>')
        show, cards, head = _pp_show(), fit + car + oj + rp, "Van “wie ben ik?” naar “dit past bij mij”."
    else:
        mt = _card("💘", "peach", "Match", "Swipe door banen die bij je DNA passen.",
                   f'<div class="swipe"><div class="sc"></div><div class="sc front"><span class="pill" style="height:22px"><i class="e">⭐</i>Jouw top-match</span><div class="pc" style="margin-top:8px">91%</div><div class="sm"><b>Teamleider logistiek</b></div><div class="xs muted">Hoeve Transport · 18 min</div></div></div>'
                   f'<div class="sw-act" style="margin-top:6px"><span>{ic("x")}</span><span class="y">{ic("heart")}</span></div>')
        show, cards, head = _map_show(), pp + oj + mt + rp, "Van “wie ben ik?” naar “hier wil ik werken”."
    return f'''<section style="padding-top:40px"><div class="wrap"><div class="shead"><div><div class="eyebrow">Wat je krijgt als je inlogt</div><h2 class="sec">{head}</h2></div><div class="sp"></div>{vb("Alle voorbeelden: Voorbeelddata")}</div>
{show}<div class="four">{cards}</div></div></section>'''

def voor_wie(zw=False):
    if zw:
        w = [("🙋", "peach", "Jij, op zoek naar je richting", "Weet je nog niet wat je wilt? Of wil je iets nieuws? Begin gewoon.", ["Gratis tests en paspoort", "Beroepen die bij je passen", "Jij beslist wat je deelt"], "Doe de gratis test", "pri", True),
             ("✈️", "sun", "Nieuw in Nederland", "Je diploma telt hier (nog) niet? Laat zien wat je wél kunt.", ["In je eigen taal", "Bewijzen van je ervaring", "Stap voor stap, B1-taal"], "Kies je taal", "", False),
             ("🧗", "mint", "Even vastgelopen", "Werk dat niet meer past? Dan is het tijd om uit je schild te groeien.", ["Ontdek wat je energie geeft", "Droombaan-check", "Rustig, in je eigen tempo"], "Zo werkt het", "", False),
             ("🎓", "sky", "School", "Help leerlingen en studenten ontdekken wat bij ze past.", ["Tests voor je klas", "Inzicht per groep", "Hulp bij studiekeuze"], "Voor scholen", "", False)]
    else:
        w = [("🙋", "peach", "Kandidaat", "Voor jou als je werk zoekt, net in Nederland bent of vastzit.", ["Gratis tests en paspoort", "Banen op reistijd", "Jij beslist wat je deelt"], "Doe de gratis test", "pri", True),
             ("🏢", "sun", "Werkgever", "Vind mensen die écht bij je team passen, niet alleen op papier.", ["Vacatures plaatsen", "Werken met tokens", "Matches op DNA en reistijd"], "Voor werkgevers", "", False),
             ("🎓", "sky", "School", "Help leerlingen en studenten ontdekken wat bij ze past.", ["Tests voor je klas", "Stages via je schooldomein", "Inzicht per groep"], "Voor scholen", "", False),
             ("🤝", "mint", "Partner", "Breng Lobsy naar bedrijven en verdien mee als partner.", ["Eigen partnerlink", "Toolkit en materiaal", "Inzicht in je resultaat"], "Word partner", "", False)]
    c = "".join(f'<div class="card wc{" me" if me else ""}"><div class="wtop t-{t}">{em(e, t, "l")}</div><div class="wb"><h3>{h}</h3><p>{p}</p><ul>{"".join(f"<li>{CK}{x}</li>" for x in li)}</ul><a class="btn {b} go">{cta}{ic("arrow")}</a></div></div>' for e, t, h, p, li, cta, b, me in w)
    title = "Lobsy is er voor iedereen die wil groeien." if zw else "Lobsy is er voor iedereen die werk een plek geeft."
    return f'''<section style="padding-top:24px"><div class="wrap"><div class="eyebrow">Voor wie?</div><h2 class="sec">{title}</h2><div class="who">{c}</div></div></section>'''

def trust(zw=False):
    t = [("📱", "peach", "Eerst op jouw apparaat", "Je testantwoorden blijven op je eigen telefoon of computer tot je een account maakt."),
         ("🙈", "sun", "Alleen jij kijkt mee" if zw else "Werkgevers zien niets zonder jou", "Je antwoorden en je paspoort zijn van jou. Jij kiest wat je deelt." if zw else "Een werkgever ziet je antwoorden nooit. In je paspoort kies jij wat je deelt."),
         ("🧹", "mint", "Wissen kan altijd", "Met één knop wis je je antwoorden. Je account verwijderen kan ook, zelf."),
         ("🛡️", "sky", "Veilig en eerlijk", "Vanaf 16 jaar. Privacyregels in gewone taal. Geen verborgen kosten.")]
    c = "".join(f'<div class="tg">{em(e, "surface" if False else tn)}<h4>{h}</h4><p>{p}</p></div>' for e, tn, h, p in t)
    return f'''<section class="trust"><div class="wrap"><div class="eyebrow">Privacy en vertrouwen</div><h2 class="sec">Jouw verhaal is van jou.</h2><div class="tgrid">{c}</div></div></section>'''

def faq(zw=False):
    first = ("Ja. De test, je account en je paspoort zijn gratis. Alleen het uitgebreide rapport (de diepteanalyse) kost eenmalig een klein bedrag." if zw else
             "Ja. De test, je account, je paspoort en de banenkaart zijn gratis. Alleen het uitgebreide rapport (de diepteanalyse) kost eenmalig een klein bedrag.")
    q = [("Is Lobsy echt gratis?", first, True), ("Heb ik een account nodig voor de test?", "", False), ("Wat gebeurt er met mijn antwoorden?", "", False),
         ("Ik spreek nog niet goed Nederlands. Kan ik Lobsy gebruiken?", "", False),
         ("Ik werk op een school. Hoe begin ik?" if zw else "Ik ben werkgever of school. Hoe begin ik?", "", False), ("Waarom een kreeft?", "", False)]
    c = "".join(f'<div class="fq{" open" if o else ""}"><h4>{h}{ic("chev")}</h4>{f"<p>{p}</p>" if p else ""}</div>' for h, p, o in q)
    return f'''<section><div class="wrap faqg"><div><div class="eyebrow">Veelgestelde vragen</div><h2 class="sec">Nog vragen?</h2><p class="lead">Kort en eerlijk antwoord.</p>
<div class="card help"><img src="{MASCOT}" alt=""><div><b>Hulp nodig?</b><p class="sm muted">Lees hoe Lobsy werkt of stel je vraag via de chat.</p><a class="btn ghost sm" style="margin-top:4px;padding:0">Hoe werkt Lobsy? {ic("arrow")}</a></div></div></div>
<div class="faq">{c}</div></div></section>'''

def final_footer(zw=False):
    cols = ([("Lobsy", ["Hoe het werkt", "Wie zijn wij", "Gratis test"]), ("Voor", ["Jou", "Nieuw in Nederland", "Scholen"]),
             ("Account", ["Inloggen", "Account maken"]), ("Juridisch", ["Privacy", "Cookies", "Algemene voorwaarden", "Gebruiksvoorwaarden"])] if zw else
            [("Lobsy", ["Hoe het werkt", "Wie zijn wij", "Banenkaart", "Gratis test"]), ("Voor", ["Kandidaten", "Werkgevers", "Scholen", "Partners"]),
             ("Account", ["Inloggen", "Account maken", "Werkgever registreren"]), ("Juridisch", ["Privacy", "Cookies", "Algemene voorwaarden", "Gebruiksvoorwaarden"])])
    fc = "".join(f'<div><h5>{h}</h5>{"".join(f"<a>{a}</a>" for a in l)}</div>' for h, l in cols)
    tag = "Eerst jij, dan je richting. Lobsy helpt je zien wie je bent en wat bij je past." if zw else "Eerst jij, dan de baan. Lobsy helpt je zien wie je bent en welk werk bij je past."
    return f'''<section style="padding-top:8px;padding-bottom:120px"><div class="wrap"><div class="final">{blob("var(--sun-2)", 760, -40, 380, 320, 2)}<img src="{MASCOT}" alt="" style="width:150px;position:relative">
<div style="position:relative"><h2>Klaar om uit je schild te groeien?</h2><p>20 vragen. 3 minuutjes. Daarna weet je meer over jezelf.</p></div><div class="sp"></div>
<a class="btn lg onpri" style="position:relative">Doe de gratis test {ic("arrow")}</a><a class="btn lg ondark" style="position:relative">Inloggen</a></div></div></section>
{wave("var(--bg)", "var(--pearl-mid)")}<footer class="ftr"><div class="wrap"><div class="fg"><div><a class="brand"><img src="{LOGO}" alt="">Lobsy</a><p style="margin-top:12px;max-width:260px">{tag}</p></div>{fc}</div>
<div class="fb"><span>© 2026 Lobsy · gemaakt met <i class="e">🧡</i></span><div class="sp"></div><span class="lang" style="height:auto">{ic("globe")}Nederlands{ic("chev")}</span></div></div></footer>'''
