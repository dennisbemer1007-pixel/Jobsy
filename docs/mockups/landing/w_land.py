"""Warm landing page, both variants. zw=True = Werkgevers actief OFF: no jobs/vacancies/employers/map/match anywhere."""
from ui import ic, LOGO, LOBSTER, MASCOT, vb
from w_common import em, blob, wave, mini_scene, mascot, ghost
from w_hero import hero_scene

def header(zw=False):
    items = ["Hoe het werkt", "Mijn Paspoort", "Ontdekkingsreis", "Scholen"] if zw else ["Hoe het werkt", "Banenkaart", "Werkgevers", "Scholen", "Partners"]
    nav = "".join(f'<a>{n}</a>' for n in items)
    return f'''<header class="hdr"><div class="wrap"><a class="brand"><img src="{LOGO}" alt="">Lobsy</a><nav class="nav">{nav}</nav>
<div class="r"><span class="lang">{ic("globe")}NL{ic("chev")}</span><a class="btn sm ondark">Inloggen</a><a class="btn sm onpri">Doe de gratis test</a></div></div></header>'''

def hero(zw=False):
    lead = ("Lobsy laat zien wie jij bent, wat je kunt en welk werk bij je past. Eerst jij, dan je richting." if zw else
            "Lobsy laat zien wie jij bent, wat je kunt en welk werk bij je past. Eerst jij, dan de baan.")
    third = '<span><i class="e">🪪</i>Gratis paspoort</span>' if zw else '<span><i class="e">🗺️</i><u>Of kijk eerst op de banenkaart</u></span>'
    priv = "Alleen jij ziet je antwoorden" if zw else "Werkgevers zien je antwoorden niet"
    who = [("🔍", "sky", "je richting zoekt" if zw else "werk zoekt"), ("✈️", "sun", "net in Nederland bent"), ("🎒", "mint", "op school zit"), ("🧗", "peach", "even vastzit")]
    chips = "".join(f'<span>{em(e, t)}{x}</span>' for e, t, x in who)
    return f'''<section class="hero"><div class="wrap">
<div>
 <span class="pill dark"><i class="e">👋</i>Hoi! Gratis en zonder account</span>
 <h1>Soms moet je uit je schild <em>groeien</em>.</h1>
 <p class="lead">{lead} Doe de korte test en zie: <b>dit ben jij</b>.</p>
 <div class="ctas"><a class="btn lg onpri">Doe de gratis test {ic("arrow")}</a><a class="btn lg ondark">Inloggen</a></div>
 <div class="micro"><span><i class="e">⏱️</i>20 vragen · 3 minuutjes</span><span><i class="e">🔒</i>{priv}</span>{third}</div>
 <div class="hi-for"><b>Lobsy is er voor jou als je…</b>{chips}</div>
</div>
{hero_scene(zw)}</div></section>{wave("var(--cream)", "var(--bg)")}'''

def wat_is(zw=False):
    s2 = (("✅", "mint", "Zie wat bij je past", "Welke beroepen passen bij jou? En wat heb je nog nodig voor je droombaan?", ["Functiefit", "Droombaan-check"]) if zw else
          ("🗺️", "mint", "Zie wat bij je past", "Banen bij jou in de buurt op de kaart. Met je reistijd op de fiets, met de bus of met de auto.", ["Banenkaart", "Reistijd"]))
    steps = [("🔍", "peach", "Ontdek jezelf", "Korte, leuke vragen over hoe je werkt, wat je leuk vindt en wat je belangrijk vindt. Er is geen goed of fout.", ["Werk-DNA", "Gratis"]), s2,
             ("🪪", "sun", "Laat zien wat je kunt", "Je eigen paspoort met je DNA, je tests en je bewijzen. " + ("Van jou, voor jou." if zw else "Jij kiest wat een werkgever ziet."), ["Paspoort", "Bewijzen" if zw else "Jij beslist"])]
    cards = "".join(f'<div class="card step"><div class="n">{em(e, t, "l")}<small>Stap {n}</small></div><h3>{h}</h3><p>{p}</p><div class="tag">{"".join(f"<span class=pill>{x}</span>" for x in tags)}</div></div>' for n, (e, t, h, p, tags) in enumerate(steps, 1))
    return f'''<section style="padding-top:40px"><div class="wrap"><div class="eyebrow">Wat is Lobsy?</div><h2 class="sec">Geen cv-site. Lobsy kijkt eerst naar jóu.</h2>
<p class="lead">Weet je niet goed wat je kunt of wilt? Telt je diploma hier niet? Dat is heel normaal. Lobsy helpt je ontdekken wie je bent, en wat bij je past.</p>
<div class="steps">{cards}</div></div></section>'''

def kreeft(zw=False):
    tiles = [("dive", "Diep duiken", "Je doet tests die verder gaan dan je cv. Zo zie je wat er echt in je zit.", "Mijn tests"),
             ("antenna", "Antennes", "Je voelt snel of een team of plek bij je past. Lobsy maakt dat gevoel zichtbaar.", "Hier voel je je thuis"),
             ("rock", "De juiste rots", "Een kreeft zoekt een plek waar hij veilig kan groeien. " + ("Welk beroep is jouw rots?" if zw else "Jij ook."), "Past dit beroep?" if zw else "Banenkaart"),
             ("claw", "Je scharen", "Je sterke punten. Met bewijzen erbij: diploma's, certificaten, ervaring.", "Bewijzen"),
             ("grow", "Van zacht naar sterk", "Na elke groeistap word je sterker. Lobsy laat zien welke stap past.", "Carrière"),
             ("path", "Je eigen weg", "Een kreeft loopt niet in een rij. Jij kiest je eigen richting.", "Ontdekkingsreis")]
    t = "".join(f'<div class="card mt">{mini_scene(k)}<div class="mtb"><h4>{h}</h4><p>{p}</p><span class="lnk">{ic("arrow")}In Lobsy: {l}</span></div></div>' for k, h, p, l in tiles)
    story_scene = (blob("var(--peach-2)", 140, 360, 340, 300, 1) + ghost(70, 400, 170, -20, .1) + mascot(230, 400, 200, "", "hm")
                   + '<div class="bubble r" style="left:170px;top:338px">Te krap? Tijd om te groeien! <i class="e">🌱</i></div>')
    return f'''{wave("var(--bg)", "var(--sky)")}<section class="kreeft"><div class="wrap"><div class="eyebrow">De kreeft-visie</div><h2 class="sec">Jij bent de kreeft.</h2>
<p class="lead">Een kreeft stopt nooit met groeien. Maar dat kan alleen als hij zijn oude schild durft los te laten. Net als jij.</p>
<div class="kgrid"><div class="story"><h3>Soms moet je uit je schild groeien.</h3>
<p>Wordt je schild te krap? Dan laat een kreeft het los. Even is hij zacht en kwetsbaar. Hij zoekt een veilige rots. En daarna is hij sterker dan ooit.</p>
<p>Zo gaat het met werk ook. Een nieuw land, een nieuwe school, of gewoon het gevoel dat je vastzit. Lobsy is je veilige rots.</p>
<div class="sscene">{story_scene}</div></div>
<div class="meta">{t}</div></div></div></section>{wave("var(--sky)", "var(--bg)")}'''
