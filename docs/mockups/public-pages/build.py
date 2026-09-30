"""Lobsy public/legal pages (pb-*), phase 1 mockups. Run: python3 build.py [filter]."""
import pathlib, sys
here = pathlib.Path(__file__).parent
sys.path.insert(0, str(here))
from pub_ui import page, dhdr, mhdr, footer, ic, em, blob, wave, vb, anno, MASCOT, LOGO
from render_lib import render

MF = dict(links=("Privacy", "Voorwaarden", "Wie zijn wij"))
def hero(pill, h1, lead, extra="", art=""):
    return (f'<section class="ph">{blob("var(--sun)", 1100, -80, 380, 320, 1)}{blob("var(--peach)", -140, 180, 300, 260, 2)}<div class="wrap" style="position:relative;display:flex;gap:30px;align-items:center">'
            f'<div style="flex:1"><span class="code-pill">{pill}</span><h1>{h1}</h1><p class="lead">{lead}</p>{extra}</div>{art}</div></section>{wave("var(--cream)", "var(--bg)")}')
def art(w=190):
    return f'<img src="{MASCOT}" alt="" style="width:{w}px;position:relative">'
CK = ic("check")

# ---------- hoe-werkt-lobsy ----------
STEPS = [("🗺️", "sky", "Kijk op de kaart", "Je ziet vacatures bij jou in de buurt. Op fiets-, OV- of autotijd.", "Open de banenkaart"),
         ("✨", "peach", "Doe de gratis test", "20 korte vragen. Je ziet meteen wat bij je past. Geen account nodig.", "Doe de test"),
         ("🪪", "mint", "Maak een account", "Pas als je wilt solliciteren. Je testuitslag gaat mee.", "Account maken"),
         ("🤝", "sun", "Solliciteer op jouw tempo", "Werkgevers zien je gegevens pas als jij dat goed vindt.", "Hoe solliciteren werkt")]
def how(m=False):
    st = "".join(f'<div class="stp"><span class="n">{i+1}</span>{em(e, t)}<h3>{h}</h3><p>{p}</p><span class="go">{g}{ic("arrow")}</span></div>' for i, (e, t, h, p, g) in enumerate(STEPS))
    seg = '<div class="seg2" style="margin-top:20px"><span class="on">🙋 Voor jou</span><span>🏢 Voor werkgevers</span><span>🎓 Voor scholen</span></div>'
    promise = ("".join(f'<li>{CK}<span>{t}</span></li>' for t in ["Gratis voor werkzoekenden. Altijd.", "Werkgevers zien je testantwoorden nooit.",
                                                               "Je naam en telefoon deel je pas na jouw ja.", "Je kunt je account en gegevens zelf verwijderen."]))
    faq = "".join(f'<details {"open" if i == 0 else ""}><summary>{q}<span>{ic("chev")}</span></summary><p>{a}</p></details>' for i, (q, a) in enumerate([
        ("Kost Lobsy geld?", "Nee. Voor werkzoekenden is Lobsy gratis. Alleen de uitgebreide testanalyse kost € 2,99, en die is niet nodig."),
        ("Heb ik een cv nodig?", "Nee. Je kunt solliciteren met je profiel. Een cv toevoegen mag, maar hoeft niet."),
        ("Ik ben jonger dan 16. Kan ik meedoen?", "Ja, vanaf 13 jaar. Je ouder of verzorger krijgt dan eerst een mail om toestemming te geven.")]))
    notes = "" if m else anno("Notities (alleen in de mockup)", [
        "Nu: de pagina laadt pas na het opstarten van de live verbinding (prerender uit). Zoekmachines en trage telefoons zien alleen 'Laden…'. Wordt een statische pagina.",
        "Nu: een ingelogde kandidaat wordt doorgestuurd naar /home. Wordt: iedereen kan de pagina lezen; ingelogd opent het juiste tabblad (Voor jou / Voor werkgevers / beheer).",
        "Nu: pl/ro/ar tonen Nederlandse tekst (bewust gekopieerd). Wordt: 5 talen, ar van rechts naar links.",
        "Werkgevers-schakelaar uit: stap 1 wordt 'Maak je paspoort', geen tab Voor werkgevers."])
    body = (dhdr("Hoe het werkt") if not m else mhdr()) + hero(f'{em("🧭","mint","s")}Hoe werkt Lobsy?', 'Zo werkt <em>Lobsy</em>. In 4 stappen.',
            'Eerst kijk je wie je bent en wat je kunt. Daarna vind je werk dat past, dichtbij huis.', seg, "" if m else art())
    body += (f'<section class="sec"><div class="wrap"><div class="steps">{st}</div></div></section>'
             f'<section class="sec" style="padding-top:0"><div class="wrap two"><div class="box">{em("🔒","mint")}<h3 style="margin-top:12px">Dit beloven we je</h3><ul class="chkl">{promise}</ul></div>'
             f'<div class="faq"><h2 style="font-size:1.5rem;margin-bottom:12px">Vragen</h2>{faq}</div></div>'
             f'{"" if m else f"<div style=\'margin-top:22px\'>{notes}</div>"}</div></section>')
    return page(body + footer(**(MF if m else {})), cls="m" if m else "")

# ---------- wie-zijn-wij ----------
def about(m=False):
    idc = ('<dl class="idc"><dt>Naam</dt><dd>Lobsy <span class="cfg">uit config</span></dd><dt>Adres</dt><dd>[adres] <span class="cfg">Dennis levert aan</span></dd>'
           '<dt>KvK</dt><dd>[KvK-nummer] <span class="cfg">Dennis levert aan</span></dd><dt>E-mail</dt><dd>support@lobsy.nl</dd></dl>')
    story = "".join(f'<div class="box">{em(e, t)}<h3 style="margin-top:12px">{h}</h3><p>{p}</p></div>' for e, t, h, p in [
        ("🦞", "peach", "Waarom een kreeft?", "Een kreeft groeit door zijn oude schild af te werpen. Zo zien wij werk zoeken ook: je groeit, stap voor stap."),
        ("🌷", "mint", "Begonnen in het Westland", "Lobsy begon met één vraag: hoe vinden mensen dichtbij werk dat echt past? Van daaruit bouwen we verder."),
        ("🤝", "sky", "Voor twee kanten", "Voor werkzoekenden: eerlijk en duidelijk. Voor werkgevers: snel en zonder gedoe.")])
    me = (f'<div class="box" style="display:flex;gap:18px;align-items:center"><div class="logo-sq" style="width:88px;height:88px;border-radius:50%">🙂</div>'
          f'<div><h3>Dennis, oprichter</h3><p>Ik bouw software die logisch voelt. Met Lobsy wil ik dat iedereen dichtbij werk kan vinden, ook zonder cv of perfect Nederlands.</p><p class="note">Foto: Dennis levert aan (optioneel)</p></div></div>')
    notes = "" if m else anno("Notities (alleen in de mockup)", ["Tekst komt nu uit de database (beheer) en is alleen Nederlands. Voorstel: vaste tekst in 5 talen; beheer kan alleen de 'persoonlijke' alinea aanpassen.",
        "Nu: 'Via chat weet je sneller…' (er is geen publieke chat) en 'stoffige uitzendbureau-vibes' → weg. Contact wordt support@ (niet privacy@).",
        "Bedrijfsgegevens (naam, adres, KvK) komen uit dezelfde instelling als de mailfooter. Lege velden worden niet getoond, nooit [placeholder]."])
    body = (dhdr() if not m else mhdr()) + hero(f'{em("👋","peach","s")}Wie zijn wij', 'Hoi! Wij zijn <em>Lobsy</em>.', 'Een klein team uit het Westland. We helpen mensen werk vinden dat past, dichtbij huis.', "", "" if m else art(200))
    body += (f'<section class="sec"><div class="wrap"><div class="steps" style="grid-template-columns:repeat({1 if m else 3},1fr)">{story}</div>'
             f'<div class="two" style="margin-top:22px">{me}<div class="box"><h3>Contact en bedrijfsgegevens</h3><p style="margin-bottom:12px">Vraag of idee? Mail ons, we reageren binnen 2 werkdagen.</p>{idc}</div></div>'
             f'{"" if m else f"<div style=\'margin-top:22px\'>{notes}</div>"}</div></section>')
    return page(body + footer(**(MF if m else {})), cls="m" if m else "")

# ---------- privacy ----------
PROC = [("Render", "EU (Frankfurt)", "Hosting van de app en database", "Alle gegevens van je account", False),
        ("Cloudflare", "EU/VS", "Beveiliging en snelle levering", "IP-adres, technische gegevens", False),
        ("Resend", "VS", "E-mails versturen", "E-mailadres, naam, inhoud van de mail", False),
        ("Mollie", "Nederland", "Betalingen", "Bedrag, betaalstatus (geen kaartgegevens bij ons)", False),
        ("Pingen", "Zwitserland", "Brieven per post (bedrijfscontrole)", "Bedrijfsnaam, adres, code in de brief", True),
        ("Sentry", "EU/VS", "Foutmeldingen", "Technische gegevens, geen formulierinhoud", False),
        ("OpenAI", "VS", "AI-functies die jij kiest", "Tekst die je invoert, cv-tekst bij cv uitlezen", False),
        ("KvK", "Nederland", "Bedrijven controleren", "KvK- en vestigingsnummer", False),
        ("OpenStreetMap / FOSSGIS", "EU", "Kaarten en reistijd", "Coördinaten, geen naam", True),
        ("Push-diensten (Google, Apple, Mozilla)", "EU/VS", "Meldingen op je telefoon (als jij dat aanzet)", "Een apparaatcode, de tekst van de melding", True)]
def ptable(m=False):
    rows = "".join(f'<tr class="{"new" if n else ""}"><td>{a}{" <span class=flag>nieuw</span>" if n else ""}</td><td>{b}</td><td>{c}</td><td>{d}</td></tr>' for a, b, c, d, n in PROC)
    return f'<table class="ptab"><thead><tr><th>Partij</th><th>Waar</th><th>Waarvoor</th><th>Welke gegevens</th></tr></thead><tbody>{rows}</tbody></table>'
TOC = ["Wie is verantwoordelijk?", "Welke gegevens?", "Waarom mogen we dat?", "Met wie delen we het?", "Hoe lang bewaren we het?", "Cookies", "AI en matching", "Jonger dan 16", "Jouw rechten", "Wijzigingen"]
def privacy(m=False):
    toc = "".join(f'<a class="{"on" if i == 3 else ""}">{i+1}. {t}</a>' for i, t in enumerate(TOC))
    tocb = (f'<div class="mtoc">{ic("menu")}<span style="flex:1;margin-inline-start:10px">Inhoud (10 onderdelen)</span>{ic("chev")}</div>' if m else
            f'<aside class="toc"><h4>Op deze pagina</h4>{toc}<div class="sep"></div><a>{ic("user")} Mijn gegevens</a><a>{ic("report")} Download als pdf</a></aside>')
    idc = ('<dl class="idc"><dt>Naam</dt><dd>Lobsy <span class="cfg">Legal:Name</span></dd><dt>Adres</dt><dd>[wordt ingevuld] <span class="cfg">Legal:Address</span></dd>'
           '<dt>KvK</dt><dd>[wordt ingevuld] <span class="cfg">Legal:KvkNumber</span></dd><dt>Privacyvragen</dt><dd>privacy@lobsy.nl <span class="cfg">Legal:PrivacyEmail</span></dd></dl>')
    tl = lambda t: f'<div class="tldr">{em("💡","mint","s")}<div><b>In het kort</b><p>{t}</p></div></div>'
    ret = [("Onbevestigde sollicitaties", "48 uur"), ("Registratie zonder bevestigde code", "10 minuten"), ("Geannuleerde registraties", "30 dagen"),
           ("Platformlogs en feedback-screenshots", "90 dagen"), ("Klik- en bezoekstatistieken, meldingen", "365 dagen"), ("Log van wie jouw gegevens inzag", "730 dagen"),
           ("Account, tests en uitslagen", "tot je je account verwijdert")]
    rt = "".join(f'<tr><td>{a}</td><td>{b}</td></tr>' for a, b in ret)
    doc = (f'<div class="doc"><div class="docnote">{em("🌍","sky","s")}<span><b>De Nederlandse tekst is de officiële versie.</b> De blokken "In het kort" staan in jouw taal (nl, en, pl, ro, ar).</span></div>'
           f'<div class="dsec"><h2>1. Wie is verantwoordelijk?</h2>{tl("Lobsy is verantwoordelijk voor je gegevens. Vragen? Mail ons.")}{idc}</div>'
           f'<div class="dsec"><h2>4. Met wie delen we het?</h2>{tl("We delen alleen wat nodig is. Werkgevers zien je naam en telefoon pas als jij dat goed vindt. We verkopen niets.")}'
           f'<p><b>Werkgevers</b> zien je sollicitatie. Contactgegevens pas na acceptatie. Testantwoorden nooit.</p><p style="margin-top:10px"><b>Partijen die voor ons werken (verwerkers):</b></p>{ptable(m)}'
           f'<p class="note">Selectie voor de mockup; de volledige lijst (ook Google/Microsoft-login, Transitous, YouTube/Vimeo, Cursor) staat in de review. Buiten de EU: we gebruiken het EU-VS Data Privacy Framework of de standaardcontractbepalingen van de EU. <a>Lijst als pdf</a></p></div>'
           f'<div class="dsec"><h2>5. Hoe lang bewaren we het?</h2>{tl("We bewaren gegevens niet langer dan nodig. Je account blijft tot je hem verwijdert.")}'
           f'<table class="ptab"><tbody>{rt}</tbody></table><p class="note">Termijnen komen rechtstreeks uit de code (PrivacyConstants), zodat pagina en systeem altijd gelijk zijn.</p></div></div>')
    notes = "" if m else f'<div style="margin-top:0;padding-bottom:100px">{anno("Notities (alleen in de mockup)", ["Nu staat er live op lobsy.nl: [BEDRIJFSNAAM], KVK [KVK-NUMMER], [ADRES] en een mailto naar [CONTACT E-MAIL PRIVACY]. Wordt: waarden uit config; leeg = regel weg.", "Nieuw in de lijst: Pingen (post), OpenStreetMap/FOSSGIS (reistijd, kaartje in pdf), push-diensten. Render draait in Frankfurt (render.yaml); Render zelf is een VS-bedrijf.", "Bewaartermijnen: nu mist o.a. het inzage-log (730 d), meldingen (365 d) en de 10 minuten voor registratie. Wordt uit PrivacyConstants getoond.", "Versie + datum komen uit PrivacyConstants.CurrentConsentVersion, niet met de hand."])}</div>'
    body = (dhdr() if not m else mhdr()) + hero(f'{em("🔒","mint","s")}Privacy', 'Privacyverklaring', 'Welke gegevens we gebruiken, waarom, en wat jouw rechten zijn. In gewone taal.',
            '<div class="upd"><span>' + ic("clock") + 'Versie 2026-09-26</span><span>' + ic("globe") + 'Samenvatting in 5 talen</span></div>', "")
    body += f'<div class="wrap"><div class="lgl">{tocb}{doc}</div>{notes}</div>'
    return page(body + footer(**(MF if m else {})), cls="m" if m else "")

def privacy_ar():
    tl = lambda t: f'<div class="tldr">{em("💡","mint","s")}<div><b>باختصار</b><p>{t}</p></div></div>'
    hdr = f'<header class="mh"><a class="brand"><img src="{LOGO}" alt=""><span dir="ltr">Lobsy</span></a><div class="r"><a class="t">تسجيل الدخول</a><span class="ib">{ic("menu")}</span></div></header>'
    body = hdr + hero(f'{em("🔒","mint","s")}الخصوصية', 'بيان الخصوصية', 'ما البيانات التي نستخدمها، ولماذا، وما هي حقوقك.', "", "")
    body += (f'<div class="wrap"><div class="lgl"><div class="mtoc">{ic("menu")}<span style="flex:1;margin-inline-start:10px">المحتوى (10 أقسام)</span>{ic("chev")}</div><div class="doc">'
             f'<div class="docnote">{em("🌍","sky","s")}<span><b>النص الهولندي هو النسخة الرسمية.</b> فقرات «باختصار» بلغتك.</span></div>'
             f'<div class="dsec"><h2>4. مع من نشارك بياناتك؟</h2>{tl("نشارك فقط ما هو ضروري. لا يرى صاحب العمل اسمك أو هاتفك إلا بموافقتك. لا نبيع أي شيء.")}'
             f'<div dir="ltr" lang="nl" style="text-align:left"><p class="note" style="margin:0 0 6px">Nederlandse tekst (officieel):</p><p>Werkgevers zien je sollicitatie. Contactgegevens pas na acceptatie. Testantwoorden nooit.</p></div></div>'
             f'<div class="dsec"><h2>9. حقوقك</h2>{tl("يمكنك رؤية بياناتك وتنزيلها وحذفها. يمكنك أيضًا تقديم شكوى إلى هيئة حماية البيانات الهولندية.")}<a class="btn onpri" style="margin-top:6px">بياناتي</a></div></div></div></div>')
    return page(body + footer(links=("الخصوصية", "الشروط", "من نحن"), legal="Lobsy · [العنوان من الإعدادات]", lang="العربية"), cls="m", lang="ar", rtl=True)

# ---------- privacy/data ----------
def mydata(m=False):
    exp = (f'<div class="box">{em("📦","sky")}<h3 style="margin-top:12px">Download je gegevens</h3><p>Je krijgt één bestand (JSON) met alles wat we van je hebben: account, sollicitaties, tests, toestemmingen.</p>'
           f'<div class="dl" style="margin-top:14px"><a class="btn onpri">{ic("report")}Download bestand</a><span class="note" style="margin:0">lobsy-mijn-gegevens-2026-09-30.json</span></div></div>')
    sup = (f'<div class="box">{em("🛟","sun")}<h3 style="margin-top:12px">Wie heeft je gegevens ingezien?</h3><p>Support mag alleen meekijken als jij om hulp vraagt. Dat zie je hier.</p>'
           f'<table class="ptab" style="margin-top:8px"><tbody><tr><td>28 september 2026, 14:05</td><td>Support, 60 minuten, om je te helpen met inloggen {vb()}</td></tr></tbody></table><p class="note">Tijden in Nederlandse tijd.</p></div>')
    cons = (f'<div class="box">{em("✅","mint")}<h3 style="margin-top:12px">Je toestemmingen</h3><ul class="chkl"><li>{CK}<span>Tests en profielanalyse: <b>aan</b> · <a style="color:var(--brand);font-weight:600">wijzig</a></span></li>'
            f'<li>{CK}<span>Anonieme talentpool: <b>uit</b></span></li><li>{CK}<span>Statistieken (cookies): <b>uit</b></span></li></ul></div>')
    dele = (f'<div class="box" style="outline:2px solid var(--peach-2)">{em("🗑️","peach")}<h3 style="margin-top:12px">Account verwijderen</h3><p>We sturen eerst een code naar je e-mail. Daarna verwijderen we je account en je gegevens. Dit kun je niet terugdraaien.</p>'
            f'<a class="btn ondark" style="margin-top:14px;color:var(--coral)">Account verwijderen</a></div>')
    notes = "" if m else anno("Notities (alleen in de mockup)", ["Nu: 'Exporteer JSON' toont alle gegevens als tekst op het scherm (meekijkers, zware pagina) terwijl de melding zegt 'Bewaar dit bestand'. Wordt: echte download.",
        "Nu: tijden van support-inzage in UTC ('(UTC)' in de tekst). Wordt: Nederlandse tijd, met duur en reden.",
        "Nu: foutmelding = technische uitzonderingstekst (ex.Message). Wordt: vriendelijke tekst + foutcode.", "Nu alleen Nederlands; wordt 5 talen."])
    body = (dhdr(logged="Sanne") if not m else mhdr(logged="Sanne")) + hero(f'{em("🗂️","sky","s")}Mijn gegevens', 'Jouw gegevens, <em>jouw keuze</em>.', 'Download alles wat we van je hebben, bekijk je toestemmingen of verwijder je account.', "", "")
    body += f'<section class="sec"><div class="wrap"><div class="two">{exp}{sup}{cons}{dele}</div>{"" if m else f"<div style=\'margin-top:22px\'>{notes}</div>"}</div></section>'
    return page(body + footer(**(MF if m else {})), cls="m" if m else "")

# ---------- voorwaarden ----------
def terms(kind="werkgevers", m=False):
    emp = kind == "werkgevers"
    seg = f'<div class="seg2" style="margin-top:16px"><span class="{"on" if emp else ""}">🏢 Voor werkgevers</span><span class="{"" if emp else "on"}">🙋 Voor kandidaten</span></div>'
    toc_items = (["Wie is Lobsy?", "Voor wie gelden ze?", "Wat doet Lobsy?", "Account en KvK", "Tokens", "Betalen en btw", "Vacatures en inhoud", "Iets melden", "Kandidaatgegevens", "Aansprakelijkheid", "Recht en geschillen"] if emp else
                 ["Wie is Lobsy?", "Voor wie gelden ze?", "Wat Lobsy wel en niet is", "Je account", "Solliciteren", "Betaalde extra's en bedenktijd", "AI en matching", "Iets melden", "Wat mag niet?", "Aansprakelijkheid", "Wijzigingen"])
    toc = "".join(f'<a class="{"on" if i == 7 else ""}">{i+1}. {t}</a>' for i, t in enumerate(toc_items))
    tocb = (f'<div class="mtoc">{ic("menu")}<span style="flex:1;margin-inline-start:10px">Inhoud ({len(toc_items)} onderdelen)</span>{ic("chev")}</div>' if m else f'<aside class="toc"><h4>Op deze pagina</h4>{toc}</aside>')
    tl = lambda t: f'<div class="tldr">{em("💡","mint","s")}<div><b>In het kort</b><p>{t}</p></div></div>'
    idc = ('<dl class="idc"><dt>Naam</dt><dd>Lobsy <span class="cfg">config</span></dd><dt>Adres</dt><dd>[wordt ingevuld]</dd><dt>KvK</dt><dd>[wordt ingevuld]</dd>'
           + ('<dt>Btw-id</dt><dd>[wordt ingevuld] <span class="cfg">nieuw</span></dd>' if emp else '') + '<dt>Contact</dt><dd>support@lobsy.nl</dd></dl>')
    s1 = f'<div class="dsec"><h2>1. Wie is Lobsy? <span class="flag">nieuw</span></h2>{tl("Dit zijn onze gegevens. Zo weet je met wie je afspraken maakt.")}{idc}</div>'
    s8 = (f'<div class="dsec"><h2>8. Iets melden <span class="flag">nieuw</span></h2>{tl("Zie je een vacature of bedrijf dat niet klopt? Meld het. We kijken ernaar en laten je weten wat we doen.")}'
          f'<p>Meld via de knop "Meld deze vacature" of mail support@lobsy.nl. Als we een vacature weghalen of een account beperken, krijg je een uitleg waarom, en kun je bezwaar maken.</p></div>')
    if emp:
        s6 = (f'<div class="dsec"><h2>6. Betalen en btw</h2>{tl("Je betaalt vooraf met tokens via Mollie. Alle prijzen zijn exclusief btw, tenzij er iets anders staat.")}'
              f'<p>Aansprakelijkheid (art. 10) is beperkt tot wat je in 12 maanden betaalde, met een maximum van € 250. <span class="flag">vraag aan Dennis</span></p></div>')
        doc = s1 + s6 + s8
    else:
        s6 = (f'<div class="dsec"><h2>6. Betaalde extra\'s en bedenktijd <span class="flag">nieuw</span></h2>{tl("Lobsy is gratis. Koop je iets extra, zoals de uitgebreide analyse (€ 2,99)? Dan zie je vooraf de prijs en wat je krijgt.")}'
              f'<p>Omdat je de analyse meteen krijgt, vragen we bij het betalen of je akkoord bent dat de bedenktijd van 14 dagen dan vervalt. Zonder dat vinkje start de analyse niet.</p></div>')
        s0 = f'<div class="dsec"><h2>2. Voor wie gelden ze?</h2>{tl("Voor iedereen die Lobsy gebruikt als werkzoekende, vanaf 13 jaar. Jonger dan 16? Dan vragen we eerst toestemming aan je ouder.")}</div>'
        doc = s1 + s0 + s6 + s8
    note = f'<div class="docnote">{em("🌍","sky","s")}<span><b>De Nederlandse tekst is de officiële versie.</b> "In het kort" staat in jouw taal.</span></div>'
    title = "Algemene voorwaarden" if emp else "Gebruiksvoorwaarden"
    lead = "De afspraken tussen jouw bedrijf en Lobsy." if emp else "De afspraken als je Lobsy gebruikt om werk te vinden."
    notes = "" if (m or not emp) else f'<div style="padding-bottom:100px">{anno("Notities (alleen in de mockup)", ["Beide pagina\'s missen nu wie Lobsy is (naam, adres, KvK, btw) en een manier om iets te melden (DSA: meldknop + uitleg bij verwijderen).", "Gebruiksvoorwaarden: geen minimumleeftijd en geen regels voor betaalde extra\'s (€ 2,99): bedenktijd/afstandsverklaring ontbreekt.", "Algemene voorwaarden: tekst \'exclusief of inclusief btw zoals in de checkout\' en typfouten (bulkapakket, early-adapter). Datum \'2 augustus 2026\' met de hand.", "Eén layout voor beide, met wissel bovenaan. URL\'s blijven /algemene-voorwaarden en /gebruiksvoorwaarden."])}</div>'
    body = (dhdr() if not m else mhdr()) + hero(f'{em("📜","sun","s")}Voorwaarden', title, lead, seg + '<div class="upd"><span>' + ic("clock") + 'Versie 2026-10 (concept)</span></div>', "")
    body += f'<div class="wrap"><div class="lgl">{tocb}<div class="doc">{note}{doc}</div></div>{notes}</div>'
    return page(body + footer(**(MF if m else {})), cls="m" if m else "")

# ---------- partner ----------
def partner(m=False):
    usps = "".join(f'<div class="box">{em(e, t)}<h3 style="margin-top:12px">{h}</h3><p>{p}</p></div>' for e, t, h, p in [
        ("📍", "sky", "Kandidaten dichtbij", "We laten je vacature zien aan mensen die er binnen 30 minuten kunnen zijn."),
        ("🪙", "sun", "Betaal per vacature", "Geen abonnement. Je koopt tokens en gebruikt ze als je wilt."),
        ("🎁", "peach", "Gratis start-highlight", "Met een salescode krijg je je eerste highlight gratis (t.w.v. 2 tokens).")])
    rows = [("Vacature (regulier)", "2 tokens", "€ 25,00"), ("Bijbaan", "1 token", "€ 12,50"), ("Stage", "1 token", "€ 12,50"), ("Vrijwilligerswerk", "—", "Gratis"), ("Highlight (7 dagen)", "1 token", "€ 12,50")]
    tr = "".join(f'<tr><td>{a}</td><td>{b}</td><td style="text-align:end;font-weight:700">{c}</td></tr>' for a, b, c in rows)
    tab = (f'<div class="box"><h3>Tarieven {vb()}</h3><p style="margin-bottom:6px">1 token = € 12,50. Alle prijzen zijn <b>exclusief btw</b>.</p><table class="ptab"><tbody>{tr}</tbody></table>'
           f'<p class="note">Pakketten met korting vind je na het aanmelden.</p></div>')
    share = (f'<div class="box">{em("📣","mint")}<h3 style="margin-top:12px">Deel met een collega</h3><p>Stuur deze pagina door. Je salescode gaat automatisch mee.</p>'
             f'<div class="dl" style="margin-top:14px"><a class="btn ondark">WhatsApp</a><a class="btn ondark">{ic("mail")}Mail</a><a class="btn ondark">{ic("report")}Flyer (pdf)</a></div>'
             f'<p class="note">Salescode: <b>SM-7K2Q9D</b> {vb()}</p></div>')
    notes = "" if m else anno("Notities (alleen in de mockup)", ["Nu: 'Mail'-knop zet letterlijk '%0A' in de mail (dubbel gecodeerd). Wordt: echte nieuwe regels.",
        "Nu: '0 tokens ≈ € 0,00' voor gratis soorten. Wordt: 'Gratis'. Nu staat nergens of prijzen incl./excl. btw zijn.",
        "Nu: /partner/SM-XXXXXX is los indexeerbaar (dubbele pagina's met codes in Google). Wordt: canonical /partner, noindex voor code-varianten.",
        "Nu: jargon ('landelijke spill', 'Funda-model', 'Pulse') en foutmelding = ex.Message. Wordt: B1 in 5 talen. Werkgevers-schakelaar uit: pagina 302 naar /."])
    body = (dhdr("Partners") if not m else mhdr()) + hero(f'{em("🤝","sun","s")}Voor werkgevers', 'Vind personeel <em>dichtbij</em>.',
            'Lobsy laat je vacature zien aan mensen in de buurt, op fiets-, OV- of autotijd. Je betaalt alleen als je een vacature plaatst.',
            '<div class="ecta" style="display:flex;gap:12px;margin-top:22px;flex-wrap:wrap"><a class="btn lg onpri">Bedrijf aanmelden ' + ic("arrow") + '</a><a class="btn lg ondark">Bekijk de tarieven</a></div>', "" if m else art(180))
    body += (f'<section class="sec"><div class="wrap"><div class="steps" style="grid-template-columns:repeat({1 if m else 3},1fr)">{usps}</div>'
             f'<div class="two" style="margin-top:22px">{tab}{share}</div>{"" if m else f"<div style=\'margin-top:22px\'>{notes}</div>"}</div></section>')
    return page(body + footer(**(MF if m else {})), cls="m" if m else "")

# ---------- company page /{kvk} ----------
def company(m=False):
    jobs = [("🥐", "var(--peach)", "Medewerker bakkerij", "Bijbaan · 12–20 uur", "12 min fietsen"), ("🧁", "var(--sun)", "Banketbakker", "Parttime · 24–32 uur", "12 min fietsen"),
            ("🚚", "var(--sky)", "Bezorger (e-bike)", "Bijbaan · weekend", "8 min fietsen"), ("🧹", "var(--mint)", "Schoonmaker winkel", "Bijbaan · avond", "12 min fietsen")]
    jc = "".join(f'<div class="jc"><div class="ph2" style="background:{bg};display:grid;place-items:center;font-size:44px">{e}</div><div class="b"><h4>{t}</h4><p>{s}</p><p>{ic("bike")} {d}</p></div></div>' for e, bg, t, s, d in jobs)
    pins = "".join(f'<span class="pin" style="left:{x}%;top:{y}%"></span>' for x, y in [(40, 42), (58, 55), (50, 30)])
    mp = f'<div class="mapbox"><svg viewBox="0 0 400 520" style="position:absolute;inset:0;width:100%;height:100%"><path d="M0 300 C 120 260, 200 360, 400 300" stroke="#fff" stroke-width="14" fill="none" opacity=".7"/><path d="M180 0 C 170 200, 260 300, 230 520" stroke="#fff" stroke-width="10" fill="none" opacity=".7"/></svg>{pins}</div>'
    head = (f'<div style="display:flex;gap:16px;align-items:center"><div class="logo-sq">🥐</div><div><h1 style="font-size:{1.6 if m else 2.2}rem;font-weight:700">Bakkerij De Gouden Korrel</h1>'
            f'<p class="muted">Delft · 4 vacatures · <span class="flag ok">KvK gecontroleerd</span></p></div></div>'
            f'<div class="seg2" style="margin-top:16px"><span class="on">Alle vestigingen (2)</span><span>Delft, Markt</span><span>Den Haag</span></div>')
    notes = "" if m else f'<div style="padding-bottom:100px">{anno("Notities (alleen in de mockup)", ["Nu toont /{kvk} élk bedrijf met dat KvK-nummer: ook als de KvK-controle nog loopt (Pending) of is mislukt (Failed), en ook zonder vacatures. Daardoor kan een nep-registratie onder andermans KvK-nummer een openbare pagina krijgen. Voorstel hotfix: alleen gecontroleerde bedrijven met minstens één openbare vacature; anders 404.", "De API geeft nu ook alle vestigingsadressen, exacte coördinaten en interne bedrijfs-id\'s. Voorstel: plaats + straat alleen van vestigingen met een openbare vacature; coördinaten afgerond zoals op de kaart; geen id\'s.", "Niet-bestaand KvK-nummer → de gewone 404 (nu: \'niet gevonden\' binnen de kaartpagina). Werkgevers-schakelaar uit → 404.", "Bovenaan het gewone Lobsy-menu (nu een eigen kaartkop). Kaart laadt pas na de lijst, zodat de pagina snel is."])}</div>'
    body = (dhdr("Banenkaart") if not m else mhdr())
    body += (f'<div class="wrap"><div class="crumb" style="margin-top:18px"><a>Banenkaart</a>›<span>Bakkerij De Gouden Korrel</span>{vb()}</div><div class="cp"><div>{head}'
             f'<div class="jg" style="margin-top:20px">{jc}</div><p class="note">Klopt er iets niet op deze pagina? <a>Meld het</a>.</p></div>{"" if m else mp}</div>{notes}</div>')
    return page(body + footer(**(MF if m else {})), cls="m" if m else "")

SCREENS = [("pb-d01-hoe-werkt-lobsy", how, "d"), ("pb-m01-hoe-werkt-lobsy", lambda: how(True), "m"),
           ("pb-d02-wie-zijn-wij", about, "d"), ("pb-m02-wie-zijn-wij", lambda: about(True), "m"),
           ("pb-d03-privacy", privacy, "d"), ("pb-m03-privacy", lambda: privacy(True), "m"), ("pb-m08-privacy-arabisch-rtl", privacy_ar, "m"),
           ("pb-d04-mijn-gegevens", mydata, "d"), ("pb-m04-mijn-gegevens", lambda: mydata(True), "m"),
           ("pb-d05-algemene-voorwaarden", terms, "d"), ("pb-m05-gebruiksvoorwaarden", lambda: terms("kandidaten", True), "m"),
           ("pb-d06-partner", partner, "d"), ("pb-m06-partner", lambda: partner(True), "m"),
           ("pb-d07-bedrijfspagina", company, "d"), ("pb-m07-bedrijfspagina", lambda: company(True), "m")]
if __name__ == "__main__":
    render(here, SCREENS, sys.argv[1:])
