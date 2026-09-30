"""Mail content (B1 Dutch + one AR variant). All names, companies, codes and addresses are sample data (Voorbeelddata)."""
from em_layout import eyebrow, h1, h2, p, kv, steps, button, otp, note, hero_with_mascot, wrap

B = "https://lobsy.nl"
FROM = ("Lobsy", "hallo@mail.lobsy.nl")

def M(key, subject, pre, inner, reason, **kw):
    return dict(key=key, subject=subject, pre=pre, inner=inner, reason=reason, kw=kw)

def mk(n, cls="in"):  # mockup-only numbered badge, positioned in the left gutter
    return f'<span class="mkb {cls}">{n}</span>'

def basis(annot=False):
    a = (lambda n: mk(n)) if annot else (lambda n: "")
    inner = (a(3) + eyebrow("Account", "sky") + h1("Je Lobsy-account is klaar", a(4)) +
             p("Hallo Sanne,") +
             p("Je account voor <strong>Bakkerij De Gouden Korrel</strong> is actief. Je kunt nu je eerste vacature plaatsen.") +
             kv([("Bedrijf", "Bakkerij De Gouden Korrel"), ("Vestiging", "Markt 12, Delft"), ("Inloggen met", "sanne@goudenkorrel.nl")], mk=a(5)) +
             button(f"{B}/login", "Naar je dashboard", mk=a(6)) +
             note("Tip: log in met Google of Microsoft. Dan hoef je geen wachtwoord te onthouden."))
    return M("RegistrationCredentials", "Je Lobsy-account is klaar", "Je kunt nu je eerste vacature plaatsen voor Bakkerij De Gouden Korrel.",
             inner, "Je krijgt deze mail omdat je Bakkerij De Gouden Korrel hebt aangemeld bij Lobsy.",
             prefs=False, mk=dict(logo=mk(2, 'lg'), sig="", foot=mk(7, 'lg')) if annot else None)

def code():
    inner = (eyebrow("Sollicitatie", "peach") + h1("Je code") +
             p("Hoi Alex,") +
             p("Vul deze code in om je sollicitatie voor <strong>Weekendhulp verkoop</strong> af te maken.") +
             otp("123456") +
             p("De code werkt <strong>10 minuten</strong>.", muted=True, size=14) +
             note("Heb jij dit niet gedaan? Dan hoef je niets te doen. Zonder deze code kan niemand solliciteren met jouw e-mailadres."))
    return M("ApplicationVerificationCode", "Je code om te solliciteren", "Je code is 123456. De code werkt 10 minuten.", inner,
             "Je krijgt deze mail omdat iemand met dit e-mailadres wil solliciteren via Lobsy.", prefs=False)

def verstuurd():
    inner = (eyebrow("Sollicitatie", "peach") + h1("Je sollicitatie is verstuurd") +
             p("Hoi Alex,") +
             p("Goed gedaan! <strong>Bakkerij De Gouden Korrel</strong> heeft je sollicitatie ontvangen.") +
             kv([("Vacature", "Weekendhulp verkoop"), ("Werkgever", "Bakkerij De Gouden Korrel"), ("Plaats", "Delft · 2,4 km van je huis"), ("Verstuurd", "30 september 2026")]) +
             h2("Wat gebeurt er nu?") +
             steps(["De werkgever bekijkt je sollicitatie.", "Is er nieuws? Dan krijg je een mail.", "Je ziet de status altijd bij <strong>Mijn sollicitaties</strong>."]) +
             button(f"{B}/sollicitaties", "Bekijk je sollicitatie"))
    return M("ApplicationConfirmation", "Je sollicitatie is verstuurd: Weekendhulp verkoop",
             "Bakkerij De Gouden Korrel heeft je sollicitatie ontvangen. Dit gebeurt er nu.", inner,
             "Je krijgt deze mail omdat je hebt gesolliciteerd via Lobsy.")

def nieuw_werkgever():
    inner = (eyebrow("Nieuwe kandidaat", "mint") + h1("Er is een nieuwe sollicitatie") +
             p("Hallo Sanne,") +
             p("Iemand heeft gesolliciteerd op je vacature <strong>Weekendhulp verkoop</strong>.") +
             kv([("Vacature", "Weekendhulp verkoop"), ("Vestiging", "Bakkerij De Gouden Korrel, Delft"), ("Match", "86% past bij je vacature"),
                 ("Afstand", "2,4 km van je vestiging"), ("Binnengekomen", "30 september 2026, 09:12")], tone="mint") +
             p("<strong>Tip:</strong> reageer binnen 2 werkdagen. Dan blijft de kandidaat enthousiast.") +
             button(f"{B}/werkgever/kandidaten", "Bekijk de kandidaat"))
    return M("EmployerNewApplication", "Nieuwe sollicitatie voor Weekendhulp verkoop",
             "Een kandidaat uit de buurt past voor 86% bij je vacature. Reageer snel.", inner,
             "Je krijgt deze mail omdat je vacatures beheert voor Bakkerij De Gouden Korrel.")

def geaccepteerd(src="../src"):
    inner = (hero_with_mascot(eyebrow("Goed nieuws", "mint"), h1("De werkgever wil je leren kennen"), f"{src}/mascot-128.png") +
             p("Hoi Alex,") +
             p("<strong>Bakkerij De Gouden Korrel</strong> vindt je sollicitatie voor <strong>Weekendhulp verkoop</strong> interessant.") +
             h2("Wat nu?") +
             steps(["De werkgever belt of mailt je binnenkort.", "Houd je telefoon en mail in de gaten. Kijk ook in je spam.", "Lees de vacature nog een keer. Dan ben je goed voorbereid."]) +
             button(f"{B}/sollicitaties", "Bekijk je sollicitatie"))
    return M("EmployerReactionAccepted", "Goed nieuws over je sollicitatie bij Bakkerij De Gouden Korrel",
             "De werkgever wil je graag leren kennen. Houd je telefoon en mail in de gaten.", inner,
             "Je krijgt deze mail omdat je hebt gesolliciteerd via Lobsy.")

def uitnodiging():
    inner = (eyebrow("Uitnodiging", "sky") + h1("Je bent uitgenodigd") +
             p("Hallo Joris,") +
             p("<strong>Sanne van Dijk</strong> vraagt of je wilt meewerken aan het Lobsy-account van <strong>Bakkerij De Gouden Korrel</strong>.") +
             kv([("Bedrijf", "Bakkerij De Gouden Korrel"), ("Jouw rol", "Manager: je beheert vacatures en kandidaten"),
                 ("Uitgenodigd door", "Sanne van Dijk"), ("Geldig tot", "7 oktober 2026")]) +
             button(f"{B}/uitnodiging/accepteren?t=voorbeeld", "Uitnodiging accepteren") +
             note("Daarna kies je zelf een wachtwoord. Of je logt in met Google of Microsoft.<br>Ken je Sanne niet? Dan kun je deze mail negeren."))
    return M("UserInvite", "Sanne nodigt je uit voor Bakkerij De Gouden Korrel",
             "Accepteer de uitnodiging en help mee met vacatures en kandidaten.", inner,
             "Je krijgt deze mail omdat Sanne van Dijk je heeft uitgenodigd.", prefs=False)

def overname():
    inner = (eyebrow("Actie nodig", "sun") + h1("Iemand wil je vestiging beheren") +
             p("Hallo Sanne,") +
             p("<strong>Joris Bakker</strong> vraagt toegang tot jouw vestiging op Lobsy.") +
             kv([("Aanvrager", "Joris Bakker"), ("E-mail", "joris@goudenkorrel.nl (bevestigd)"), ("Vestiging", "Bakkerij De Gouden Korrel, Markt 12, Delft"),
                 ("Aangevraagd", "30 september 2026, 09:12")], tone="sun") +
             p("<strong>Jij beslist.</strong> Zonder jouw akkoord krijgt Joris geen toegang.") +
             button(f"{B}/werkgever/overnames", "Bekijk het verzoek") +
             note("Ken je deze persoon niet? Kies dan <strong>Afwijzen</strong>. Twijfel je? Mail ons via hulp@lobsy.nl."))
    return M("TakeoverRequest", "Iemand wil Bakkerij De Gouden Korrel beheren op Lobsy",
             "Bekijk het verzoek van Joris Bakker. Jij beslist of dit mag.", inner,
             "Je krijgt deze mail omdat jij de beheerder bent van deze vestiging op Lobsy.", prefs=False)

AR_LABELS = dict(help="مساعدة", privacy="الخصوصية", prefs="إعدادات البريد", sig="فريق Lobsy",
                 legal="Lobsy B.V. · Voorbeeldstraat 1, 2611 AA Delft · KvK 00000000")

def bdi(t):
    return f'<bdi dir="ltr">{t}</bdi>'

def verstuurd_ar():
    inner = (eyebrow("طلب توظيف", "peach") + h1("تم إرسال طلبك") +
             p(f"مرحبًا {bdi('Alex')}،") +
             p(f"أحسنت! استلمت <strong>{bdi('Bakkerij De Gouden Korrel')}</strong> طلبك.") +
             kv([("الوظيفة", bdi("Weekendhulp verkoop")), ("صاحب العمل", bdi("Bakkerij De Gouden Korrel")),
                 ("المكان", f"{bdi('Delft')} · 2,4 كم من منزلك"), ("تاريخ الإرسال", "30 سبتمبر 2026")]) +
             h2("ماذا يحدث الآن؟") +
             steps(["يراجع صاحب العمل طلبك.", "إذا كان هناك جديد، ستصلك رسالة.", "يمكنك دائمًا رؤية الحالة في <strong>طلباتي</strong>."]) +
             button(f"{B}/sollicitaties", "عرض طلبك"))
    return M("ApplicationConfirmation", "تم إرسال طلبك: Weekendhulp verkoop",
             "استلمت Bakkerij De Gouden Korrel طلبك. إليك ما يحدث الآن.", inner,
             "تصلك هذه الرسالة لأنك تقدمت بطلب عبر Lobsy.", lang="ar", rtl=True, labels=AR_LABELS)

def render(m, src="../src"):
    kw = dict(m["kw"])
    return wrap(m["inner"], subject=m["subject"], pre=m["pre"], reason=m["reason"], src=src, **kw)
