"""Lobsy error pages (er-*), phase 1 mockups. Run: python3 build.py [filter]. Warm landing style (../landing) via ../public-pages/pub_ui.py."""
import pathlib, sys
here = pathlib.Path(__file__).parent
sys.path.insert(0, str(here.parent / "public-pages"))
from pub_ui import page, dhdr, mhdr, footer, ic, em, blob, wave, vb, mascot_img, anno, MASCOT, LOGO
from render_lib import render

def scene(big, bubble, tags, rot=0):
    t = "".join(f'<div class="tag" style="{pos}">{em(e, tone, "s")}<span>{a}<small>{b}</small></span></div>' for e, tone, a, b, pos in tags)
    return (f'<div class="scene"><div class="big">{big}</div><div class="disc"></div>'
            f'{mascot_img(f"transform:translate(-50%,-50%) rotate({rot}deg)")}<div class="bubble">{bubble}</div>{t}</div>')

def hero(pill, h1, lead, ctas, sc, extra=""):
    b = "".join(f'<a class="btn lg {c}">{t}</a>' for t, c in ctas)
    return (f'<section class="eh">{blob("var(--sun)", -120, 260, 360, 300, 1)}{blob("var(--peach)", 1180, -60, 320, 280, 2)}'
            f'<div class="wrap"><div><span class="code-pill">{pill}</span><h1>{h1}</h1><p class="lead">{lead}</p><div class="cta">{b}</div>{extra}</div>{sc}</div></section>'
            f'{wave("var(--cream)", "var(--bg)")}')

QL = [("🗺️", "sky", "Banenkaart", "Vacatures bij jou in de buurt, op reistijd.", "Open de kaart"),
      ("✨", "peach", "Gratis test", "20 vragen. Daarna weet je wat bij je past.", "Doe de test"),
      ("🧭", "mint", "Hoe werkt Lobsy?", "In 4 stappen uitgelegd, zonder moeilijke woorden.", "Lees het"),]
def quick(title="Hier kun je wel verder", items=QL, note=""):
    c = "".join(f'<div class="qc">{em(e, t)}<div><h3>{h}</h3><p>{p}</p><span class="go">{g}{ic("arrow")}</span></div></div>' for e, t, h, p, g in items)
    return f'<section class="ql"><div class="wrap"><h2>{title}</h2><div class="qg">{c}</div>{note}</div></section>'

SUP = '<p class="note">Klopt er iets niet? Mail ons op <a>support@lobsy.nl</a>.</p>'

# ---- 404 ----
H404 = dict(pill=f'{em("🔍","sky","s")}Foutcode <b>404</b>', h1='Deze pagina is <em>zoek</em>.',
            lead='Misschien is de link oud, of zit er een typfout in. Geen zorgen: met de knoppen hieronder kom je weer verder.',
            ctas=[("Naar de startpagina", "onpri"), ("Naar de banenkaart", "ondark")])
def d01():
    sc = scene("404", "Hmm, waar is die pagina? 🤔", [("🔗", "peach", "lobsy.nl/vacatures-delft", "bestaat niet (meer)", "right:0;bottom:70px"),
                                                    ("🗺️", "mint", "Wel 214 vacatures", "binnen 30 min fietsen", "left:0;bottom:24px")], rot=-6)
    notes = anno("Notities (alleen in de mockup)", [
        "Echte <b>404-status</b> (nu: lege pagina met 404). Ook voor onbekende vacature-, bedrijfs- en KvK-links: geen 200 met een foutmelding.",
        "Statische pagina (geen live verbinding nodig), <b>noindex</b>, in de taal van de bezoeker (nl/en/pl/ro/ar). Ingelogd? Dan gaat 'Naar de startpagina' naar je eigen start.",
        "Werkgevers-schakelaar uit: geen banenkaart-knop en -kaart; dan Gratis test + Hoe werkt Lobsy.",
        "We loggen alleen het pad zonder query (geen tokens of e-mailadressen in de logs)."])
    return page(dhdr() + hero(sc=sc, **H404) + quick(note=SUP) + f'<section class="ql" style="padding-top:0"><div class="wrap">{notes}</div></section>' + footer())
def m01():
    sc = scene("404", "Waar is die pagina? 🤔", [("🔗", "peach", "bestaat niet (meer)", "lobsy.nl/vacatures-delft", "right:4px;bottom:6px")], rot=-6)
    return page(mhdr() + hero(sc=sc, **H404) + quick(note=SUP) + footer(links=("Privacy", "Voorwaarden", "Wie zijn wij")), cls="m")

# ---- 500 ----
REF = ('<div class="ref">' + em("🧾", "sun", "s") + '<span>Foutcode voor support: <code>7F3K-9Q2M</code></span><a class="btn ondark">' + ic("report") + 'Kopieer</a></div>'
       '<div class="meta"><span>' + ic("shield") + 'Je gegevens zijn veilig</span><span>' + ic("mail") + 'Wij krijgen automatisch een melding</span></div>')
H500 = dict(pill=f'{em("🔧","sun","s")}Foutcode <b>500</b>', h1='Oeps. Er ging iets mis <em>bij ons</em>.',
            lead='Het ligt niet aan jou. Probeer het over een minuutje opnieuw. Lukt het dan nog steeds niet? Stuur ons de foutcode hieronder.',
            ctas=[("Probeer opnieuw", "onpri"), ("Naar de startpagina", "ondark")], extra=REF)
def d02():
    sc = scene("500", "Even sleutelen… 🔧", [("⏱️", "sun", "Meestal snel opgelost", "probeer het zo opnieuw", "right:0;bottom:60px")], rot=8)
    notes = anno("Notities (alleen in de mockup)", [
        "Foutcode = korte code (8 tekens) die naar de request-id wijst in de logs/Sentry. Nooit technische details, stacktraces of uitzonderingsteksten tonen.",
        "Werkt zonder live verbinding en zonder API: eigen kale layout, geen menu dat zelf data ophaalt (anders kan de foutpagina ook falen).",
        "'Probeer opnieuw' laadt de vorige pagina opnieuw (alleen bij GET). Na een formulier (POST): 'Terug' in plaats van opnieuw versturen.",
        "Status blijft 500, noindex. Ook fouten in een actieve pagina ('Even iets misgegaan') krijgen deze stijl, in het venster zelf."])
    return page(dhdr() + hero(sc=sc, **H500) + f'<section class="ql"><div class="wrap">{notes}</div></section>' + footer())
def m02():
    sc = scene("500", "Even sleutelen… 🔧", [], rot=8)
    return page(mhdr() + hero(sc=sc, **H500) + footer(links=("Privacy", "Voorwaarden", "Wie zijn wij")), cls="m")

# ---- 403 ----
H403 = dict(pill=f'{em("🔒","peach","s")}Geen toegang', h1='Deze pagina is niet voor <em>jouw account</em>.',
            lead='Je bent ingelogd als <b>Sanne</b> (kandidaat). Deze pagina is alleen voor werkgevers.',
            ctas=[("Naar mijn start", "onpri"), ("Inloggen met een ander account", "ondark")],
            extra='<p class="note">Moet je hier wel kunnen komen? Vraag het aan de beheerder van je bedrijf, of mail <a>support@lobsy.nl</a>.</p>')
def d03():
    sc = scene("", "Deze deur is dicht 🚪", [("🙂", "peach", "Sanne", "kandidaat " + vb(), "left:0;bottom:60px"),
                                             ("🏢", "sky", "Voor werkgevers", "/werkgever/sollicitaties", "right:0;bottom:120px")])
    notes = anno("Notities (alleen in de mockup)", [
        "Status <b>403</b> op de pagina zelf (nu: redirect naar /access-denied met 200). De URL blijft staan, zodat 'Inloggen met een ander account' kan terugkeren (returnUrl).",
        "We noemen alleen voor wie de pagina is (kandidaat, werkgever, beheer), nooit welke rechten of data er achter zitten.",
        "'Inloggen met een ander account' = uitloggen via POST en dan inloggen met returnUrl."])
    return page(dhdr(logged="Sanne") + hero(sc=sc, **H403) + f'<section class="ql"><div class="wrap">{notes}</div></section>' + footer())
def m03():
    sc = scene("", "Deze deur is dicht 🚪", [], rot=0)
    return page(mhdr(logged="Sanne") + hero(sc=sc, **H403) + footer(links=("Privacy", "Voorwaarden", "Wie zijn wij")), cls="m")

# ---- 503 maintenance ----
OTHER = [("English", "We are doing some maintenance. Lobsy is back around 14:30 (Dutch time)."),
         ("Polski", "Trwają prace konserwacyjne. Lobsy wróci około 14:30 (czasu holenderskiego)."),
         ("Română", "Facem lucrări de întreținere. Lobsy revine în jurul orei 14:30 (ora Olandei)."),
         ("العربية", "نقوم ببعض أعمال الصيانة. سيعود Lobsy حوالي الساعة 14:30 (بتوقيت هولندا).")]
def maint(m=False):
    langs = "".join(f'<div class="qc" {"dir=rtl" if n=="العربية" else ""}>{em("🌍","sky","s")}<div><h3 style="font-size:var(--text-md)">{n}</h3><p>{t}</p></div></div>' for n, t in OTHER)
    hdr = (f'<header class="{"mh" if m else "hdr"}"><div class="{"" if m else "wrap"}" style="display:flex;align-items:center;height:100%"><a class="brand"><img src="{LOGO}" alt="">Lobsy</a></div></header>')
    sc = scene("", "Even opknappen 🛠️", [("⏰", "sun", "Terug rond 14:30", "Nederlandse tijd", "right:0;bottom:70px")], rot=-4)
    h = hero(pill=f'{em("🛠️","sun","s")}Onderhoud', h1='We zijn even aan het <em>opknappen</em>.',
             lead='Lobsy is zo weer terug: rond <b>14:30</b> (Nederlandse tijd). Je account en je gegevens blijven gewoon bewaard.',
             ctas=[("Pagina vernieuwen", "onpri")], sc=sc,
             extra='<div class="meta"><span>' + ic("clock") + 'Vernieuw over een paar minuten</span><span>' + ic("mail") + 'Vragen? support@lobsy.nl</span></div>')
    notes = "" if m else f'<div style="margin-top:22px">{anno("Notities (alleen in de mockup)", ["Twee varianten, zelfde look: (a) onderhoudsstand in de app (beheerder zet hem aan, status 503 + Retry-After, beheerders kunnen er wel in); (b) een losse HTML-pagina voor als de app helemaal niet reageert (Cloudflare/Render-foutpagina).", "Variant (b) heeft geen server-taal: daarom kort in alle 5 talen onder elkaar. Geen scripts, geen externe fonts; logo inline.", "De eindtijd komt uit de onderhoudsinstelling; zonder tijd: \'Lobsy is zo weer terug.\'"])}</div>'
    return page(hdr + h + f'<section class="ql"><div class="wrap"><h2>In andere talen</h2><div class="qg" style="grid-template-columns:repeat({1 if m else 4},1fr)">{langs}</div>{notes}</div></section>'
                + f'<footer class="sft"><div class="wrap"><span>© 2026 Lobsy</span></div></footer>', cls="m" if m else "")

# ---- vacancy gone (410) ----
def jobs(m=False):
    data = [("🥐", "peach", "Medewerker bakkerij", "Bakkerij De Gouden Korrel · 12 min fietsen"), ("🌷", "mint", "Kwekerij-medewerker", "Kwekerij Van Dijk · 9 min fietsen"),
            ("🛒", "sky", "Vakkenvuller", "Supermarkt Markt 12 · 6 min lopen")]
    c = "".join(f'<div class="qc">{em(e, t)}<div><h3>{h}</h3><p>{p}</p><span class="go">Bekijk{ic("arrow")}</span></div></div>' for e, t, h, p in data)
    return f'<section class="ql"><div class="wrap"><h2>Vergelijkbare vacatures in Naaldwijk {vb()}</h2><div class="qg">{c}</div></div></section>'
H410 = dict(pill=f'{em("📭","sky","s")}Vacature gesloten', h1='Deze vacature is <em>niet meer online</em>.',
            lead='De werkgever heeft de vacature gesloten, of hij is verlopen. Er zijn nog genoeg andere banen bij jou in de buurt.',
            ctas=[("Bekijk vacatures in de buurt", "onpri"), ("Naar de startpagina", "ondark")])
def d05():
    sc = scene("", "Deze is al vergeven 🎉", [("🥐", "peach", "Medewerker bakkerij", "gesloten op 28 september", "right:0;bottom:70px")], rot=-8)
    notes = anno("Notities (alleen in de mockup)", ["Bestaande maar gesloten of verlopen vacature: status <b>410</b> + noindex. Onbekende id: 404 met de gewone 404-pagina.",
                                                     "Zelfde patroon voor een bedrijfspagina (/{kvk}) die niet (meer) openbaar is: 404, geen hint of het bedrijf bij Lobsy bekend is.",
                                                     "Vergelijkbare vacatures alleen als de werkgevers-schakelaar aan staat; anders de 404-snelkoppelingen."])
    return page(dhdr() + hero(sc=sc, **H410) + jobs() + f'<section class="ql" style="padding-top:0"><div class="wrap">{notes}</div></section>' + footer())

# ---- Arabic RTL 404 (mobile) ----
def m05():
    sc = scene("404", "أين هذه الصفحة؟ 🤔", [], rot=6)
    h = hero(pill=f'{em("🔍","sky","s")}رمز الخطأ <b>404</b>', h1='هذه الصفحة <em>غير موجودة</em>.',
             lead='ربما الرابط قديم أو فيه خطأ في الكتابة. لا تقلق: استخدم الأزرار أدناه للمتابعة.',
             ctas=[("إلى الصفحة الرئيسية", "onpri"), ("إلى خريطة الوظائف", "ondark")], sc=sc)
    q = quick("يمكنك المتابعة من هنا", [("🗺️", "sky", "خريطة الوظائف", "وظائف قريبة منك حسب وقت السفر.", "افتح الخريطة"),
                                         ("✨", "peach", "اختبار مجاني", "20 سؤالًا. بعدها تعرف ما يناسبك.", "ابدأ الاختبار")],
              note='<p class="note">هل هناك خطأ؟ راسلنا على <a dir="ltr">support@lobsy.nl</a>.</p>')
    hdr = f'<header class="mh"><a class="brand"><img src="{LOGO}" alt=""><span dir="ltr">Lobsy</span></a><div class="r"><a class="t">تسجيل الدخول</a><span class="ib">{ic("menu")}</span></div></header>'
    return page(hdr + h + q + footer(links=("الخصوصية", "الشروط", "من نحن"), legal="Lobsy · [العنوان من الإعدادات]", lang="العربية"), cls="m", lang="ar", rtl=True)

# ---- small states ----
def d06():
    toast = (f'<div class="box" style="display:flex;gap:14px;align-items:center">{em("📶","sky")}<div><h3 style="font-size:var(--text-lg)">Verbinding herstellen…</h3><p>Even geduld. Je blijft op deze pagina.</p></div></div>'
             f'<div class="box" style="display:flex;gap:14px;align-items:center">{em("🔌","peach")}<div style="flex:1"><h3 style="font-size:var(--text-lg)">Verbinding verbroken</h3><p>Laad de pagina opnieuw. Wat je hebt opgeslagen, blijft bewaard.</p></div><a class="btn onpri">Opnieuw laden</a></div>')
    rl = (f'<div class="box">{em("🐢","sun","l")}<h3 style="margin-top:12px">Even rustig aan</h3><p>Je hebt dit heel vaak achter elkaar geprobeerd. Wacht een minuutje en probeer het dan opnieuw.</p>'
          f'<p class="note">Foutcode 429 · Probeer het weer na 14:02</p></div>')
    inline = (f'<div class="box">{em("😕","peach","l")}<h3 style="margin-top:12px">Dit deel laadt nu niet</h3><p>De rest van de pagina werkt wel. Probeer het zo opnieuw.</p>'
              f'<div class="cta" style="display:flex;gap:10px;margin-top:14px"><a class="btn onpri">Probeer opnieuw</a><a class="btn ghost">Foutcode kopiëren</a></div></div>')
    notes = anno("Notities (alleen in de mockup)", ["Verbindingsmelding: nu altijd Nederlands in App.razor; wordt 5 talen + RTL.",
                                                     "429 (te veel verzoeken): nu een lege pagina; krijgt deze kaart met de tijd waarop het weer kan (Nederlandse tijd).",
                                                     "Fout in één blok (bijv. vacatures laden mislukt): geen technische foutmelding (nu soms ex.Message), wel 'Probeer opnieuw'."])
    body = (dhdr() + f'<section class="ph"><div class="wrap"><span class="code-pill">{em("🧩","sky","s")}Kleine meldingen</span><h1>Meldingen <em>binnen</em> een pagina</h1>'
            f'<p class="lead">Voor momenten waarop niet de hele pagina stuk is.</p></div></section>{wave("var(--cream)", "var(--bg)")}'
            f'<section class="sec"><div class="wrap"><div class="two">{toast}</div><div class="two" style="margin-top:22px">{rl}{inline}</div><div style="margin-top:22px">{notes}</div></div></section>' + footer())
    return page(body)

# ---- today (for comparison) ----
def d00():
    fr = lambda cap, inner: (f'<div><div style="background:#fff;border:1px solid var(--border);border-radius:14px;height:380px;overflow:hidden;position:relative">{inner}</div>'
                             f'<p class="note" style="margin-top:8px"><b>{cap}</b></p></div>')
    blank = '<div style="padding:60px 40px;font-family:Arial;color:#5f6368"><div style="font-size:40px">📄</div><h3 style="color:#202124;font-weight:400;font-size:22px;margin:14px 0">Deze pagina van lobsy.nl kan niet worden gevonden</h3><p style="font-size:14px">HTTP ERROR 404</p></div>'
    card = lambda h, p, a: (f'<div style="background:var(--bg);height:100%;display:grid;place-items:center"><div style="background:var(--surface);border:1px solid var(--border);border-radius:12px;padding:26px;width:80%">'
                            f'<h3 style="font-size:22px">{h}</h3><p class="muted" style="margin:8px 0">{p}</p><a style="color:var(--brand);font-weight:600">{a}</a></div></div>')
    g = (fr("/bestaat-niet → lege pagina (404, 0 bytes)", blank) + fr("/Error → korte kaart, alleen NL, 'Naar de banenkaart'", card("Er ging iets mis", "Deze pagina kon de aanvraag niet afronden. Ga terug naar de banenkaart en probeer het opnieuw.<br><small>Referentie: 0HN7K2QK9M1V4:00000001</small>", "Naar de banenkaart"))
         + fr("Geen toegang → redirect naar /access-denied (200)", card("Geen toegang", "Je hebt geen rechten om deze pagina te bekijken.", "Naar home · Inloggen"))
         + fr("Onbekende vacature (productie) → 200, index, 'Even iets misgegaan'", card("Even iets misgegaan", "De pagina reageerde niet goed. Probeer het opnieuw — je voortgang blijft meestal bewaard.", "Opnieuw proberen")))
    return page(f'<section class="sec"><div class="wrap"><h2>Zo ziet het er nu uit</h2><p class="sub">Gemeten op lobsy.nl en origin/acceptatie (30-09).</p><div style="display:grid;grid-template-columns:repeat(4,1fr);gap:18px">{g}</div></div></section>')

SCREENS = [("er-d00-huidig", d00, "d"), ("er-d01-404", d01, "d"), ("er-m01-404", m01, "m"), ("er-d02-500", d02, "d"), ("er-m02-500", m02, "m"),
           ("er-d03-geen-toegang", d03, "d"), ("er-m03-geen-toegang", m03, "m"), ("er-d04-onderhoud", maint, "d"), ("er-m04-onderhoud", lambda: maint(True), "m"),
           ("er-d05-vacature-gesloten", d05, "d"), ("er-m05-404-arabisch-rtl", m05, "m"), ("er-d06-kleine-meldingen", d06, "d")]
if __name__ == "__main__":
    render(here, SCREENS, sys.argv[1:])
