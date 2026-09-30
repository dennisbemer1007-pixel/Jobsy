"""Builds the au-* auth mockups (HTML) for Lobsy. Render with render.js. All data is Voorbeelddata."""
import base64, pathlib, io
import segno
from au_css import CSS
d = pathlib.Path(__file__).parent
def b64(n): return "data:image/png;base64," + base64.b64encode((d / "src" / n).read_bytes()).decode()
LOGO, MASCOT = b64("lobsy-128.png"), b64("mascot-256.png")
_q = io.BytesIO(); segno.make("otpauth://totp/Lobsy:voorbeeld%40bedrijf.nl?secret=JBSWY3DPEHPK3PXPVOORBEELD&issuer=Lobsy", error="m").save(_q, kind="png", scale=6, border=2, dark="#122033", light="#fffcfa")
QR = "data:image/png;base64," + base64.b64encode(_q.getvalue()).decode()

P = dict(
 mail='<rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 7 9 6 9-6"/>',
 eye='<path d="M2.5 12S6 5 12 5s9.5 7 9.5 7-3.5 7-9.5 7-9.5-7-9.5-7z"/><circle cx="12" cy="12" r="3"/>',
 lock='<rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/>',
 shield='<path d="M12 3 4 6v6c0 5 3.5 8 8 9 4.5-1 8-4 8-9V6z"/><path d="m9 12 2 2 4-4"/>',
 check='<path d="m5 12 5 5 9-10"/>', chev='<path d="m6 9 6 6 6-6"/>', chevl='<path d="m15 6-6 6 6 6"/>', chevr='<path d="m9 6 6 6-6 6"/>',
 copy='<rect x="9" y="9" width="12" height="12" rx="2"/><path d="M5 15H4a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v1"/>',
 dl='<path d="M12 4v11M7 10l5 5 5-5M5 20h14"/>', print='<path d="M7 9V3h10v6"/><rect x="3" y="9" width="18" height="8" rx="2"/><path d="M7 14h10v7H7z"/>',
 globe='<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3c3 3.5 3 14.5 0 18M12 3c-3 3.5-3 14.5 0 18"/>',
 alert='<circle cx="12" cy="12" r="9"/><path d="M12 7v6M12 16.5v.5"/>', clock='<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
 key='<circle cx="8" cy="15" r="4"/><path d="m11 12 9-9M16 7l3 3M14 9l2 2"/>', phone='<rect x="7" y="2.5" width="10" height="19" rx="2"/><path d="M11 18.5h2"/>',
 ext='<path d="M14 4h6v6M20 4l-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5"/>', building='<rect x="4" y="3" width="11" height="18" rx="1"/><path d="M15 9h4a1 1 0 0 1 1 1v11H15M8 7h3M8 11h3M8 15h3"/>',
)
def ic(n): return f'<svg class="i" viewBox="0 0 24 24" aria-hidden="true">{P[n]}</svg>'
GOOGLE = ('<svg class="i" viewBox="0 0 24 24" aria-hidden="true" style="stroke:none"><path fill="#4285F4" d="M21.6 12.2c0-.7-.1-1.4-.2-2H12v3.8h5.4a4.6 4.6 0 0 1-2 3v2.5h3.2c1.9-1.7 3-4.3 3-7.3z"/><path fill="#34A853" d="M12 22c2.7 0 5-.9 6.6-2.4l-3.2-2.5c-.9.6-2 1-3.4 1-2.6 0-4.8-1.8-5.6-4.1H3.1v2.6A10 10 0 0 0 12 22z"/><path fill="#FBBC05" d="M6.4 14c-.2-.6-.3-1.3-.3-2s.1-1.4.3-2V7.4H3.1a10 10 0 0 0 0 9.2z"/><path fill="#EA4335" d="M12 5.9c1.5 0 2.8.5 3.8 1.5l2.9-2.9A10 10 0 0 0 3.1 7.4L6.4 10c.8-2.4 3-4.1 5.6-4.1z"/></svg>')
MSFT = ('<svg class="i" viewBox="0 0 24 24" aria-hidden="true" style="stroke:none"><path fill="#F25022" d="M3 3h8.5v8.5H3z"/><path fill="#7FBA00" d="M12.5 3H21v8.5h-8.5z"/><path fill="#00A4EF" d="M3 12.5h8.5V21H3z"/><path fill="#FFB900" d="M12.5 12.5H21V21h-8.5z"/></svg>')

BLOBS = ["M60,-60C78,-42,92,-21,92,0C92,21,78,42,60,58C42,74,21,84,-2,86C-25,88,-50,82,-66,66C-82,50,-90,25,-88,2C-86,-21,-74,-42,-58,-60C-42,-78,-21,-92,0,-92C21,-92,42,-78,60,-60Z",
         "M55,-68C70,-54,80,-35,84,-15C88,5,86,26,75,43C64,60,44,73,22,80C0,87,-24,88,-43,78C-62,68,-76,47,-83,24C-90,1,-90,-24,-79,-43C-68,-62,-46,-75,-24,-80C-2,-85,40,-82,55,-68Z"]
def blob(fill, x, y, w, h, v=0):
    return (f'<svg class="blob" style="{x};top:{y}px;width:{w}px;height:{h}px" viewBox="-100 -100 200 200" preserveAspectRatio="none" aria-hidden="true"><path d="{BLOBS[v%2]}" fill="{fill}"/></svg>')
def deco(m):
    if m: return blob("var(--peach)", "right:-90px", -40, 220, 200) + blob("var(--sky)", "left:-80px", 560, 200, 190, 1)
    return (blob("var(--peach)", "left:6%", 70, 300, 280) + blob("var(--sky)", "right:5%", 420, 340, 300, 1)
            + blob("var(--sun)", "right:18%", 40, 120, 110, 1) + blob("var(--mint)", "left:14%", 560, 150, 140))

LANGS = {"nl": "NL", "ar": "AR"}
def header(m, right="", lang="NL"):
    brand = f'<a class="brand" href="/"><img src="{LOGO}" alt=""><span>Lobsy</span></a>'
    return f'<header class="pub-header">{brand}<span class="sp"></span><button class="pub-lang">{ic("globe")}{lang}{ic("chev")}</button>{right}</header>'

def page(body, m, right="", lang="NL", rtl=False, notes=""):
    dirattr = ' dir="rtl" lang="ar"' if rtl else ' lang="nl"'
    return (f'<!doctype html><html{dirattr}><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><style>{CSS}</style></head>'
            f'<body class="{"m" if m else "d"}">{header(m, right, lang)}<main class="stage">{deco(m)}{body}</main>{notes}</body></html>')

def peek(cls=""): return f'<img class="peek {cls}" src="{MASCOT}" alt="">'
def topline(eyebrow=""):
    e = f'<span class="pub-eyebrow">{eyebrow}</span>' if eyebrow else ""
    return f'<div class="topline">{e}<span class="vb">Voorbeelddata</span></div>'
def inp(label, value="", ph="", kind="", extra="", aside=""):
    v = f'<span>{value}</span>' if value else f'<span class="ph">{ph}</span>'
    lab = f'<div class="lblrow"><label>{label}</label>{aside}</div>'
    return f'<div class="field">{lab}<div class="inp {kind}">{v}{extra}</div></div>'
def chk(txt, on=False, small=""):
    s = f'<small>{small}</small>' if small else ""
    return f'<div class="chk"><span class="b {"on" if on else ""}">{ic("check") if on else ""}</span><span>{txt}{s}</span></div>'
EYE = f'<span class="eye">{ic("eye")}Toon</span>'

# ---------- screens ----------
def login(m, state="ok"):
    right = '' if m else '<a class="pub-btn pub-btn--secondary pub-btn--sm" href="/account-maken">Account maken</a>'
    alert = ""
    email_kind, pw_kind, email_val = "", "", ""
    pw_val = ""
    if state == "err":
        alert = f'<div class="alert err" role="alert">{ic("alert")}<div><b>Dat klopt niet helemaal</b>Het e-mailadres of wachtwoord is niet goed. Probeer het nog eens.</div></div>'
        email_val, pw_kind = "d.devries@bakkerijzon.nl", "err focus"
    elif state == "retry":
        alert = f'<div class="alert info" role="status">{ic("clock")}<div><b>Je sessie is verlopen</b>Log opnieuw in. Je komt terug waar je was.</div></div>'
    email = inp("E-mailadres", email_val, "naam@voorbeeld.nl", email_val and "" or "")
    pw = inp("Wachtwoord", "•••••••" if state=="err" else "", "Je wachtwoord", pw_kind, EYE, '<a href="/wachtwoord-vergeten">Wachtwoord vergeten?</a>')
    ferr = '<p class="ferr" style="margin:-6px 0 12px">Nog 2 pogingen, daarna pauzeren we even.</p>' if state=="err" and False else ""
    body = f'''<div class="col"><div class="pub-card">{peek()}
      {topline("Welkom terug")}
      <h1>Inloggen bij Lobsy</h1><p class="lead">Kies hoe je wilt inloggen.</p>
      {alert}
      <div class="stack"><a class="pub-btn pub-btn--secondary prov">{MSFT}Inloggen met Microsoft</a><a class="pub-btn pub-btn--secondary prov">{GOOGLE}Inloggen met Google</a></div>
      <div class="divider">of met je e-mailadres</div>
      {email}{pw}{ferr}
      {chk("Blijf ingelogd op dit apparaat", False, "Niet aanvinken op een gedeelde computer.")}
      <button class="pub-btn pub-btn--primary pub-btn--block">Inloggen</button>
      <div class="foot"><span>Nieuw bij Lobsy? <a href="/account-maken">Maak gratis een account</a></span>
      <span>Voor werkgevers: <a href="/register">Bedrijf registreren (KvK)</a></span></div>
    </div><p class="below"><a href="/">Terug naar de banenkaart</a></p></div>'''
    return page(body, m, right)

def locked(m):
    right = '' if m else '<a class="pub-btn pub-btn--secondary pub-btn--sm" href="/account-maken">Account maken</a>'
    body = f'''<div class="col"><div class="pub-card">{peek()}
      {topline()}
      <span class="emo-c sun" aria-hidden="true">⏸️</span>
      <h1>Even pauze</h1>
      <p class="lead">Er is te vaak een verkeerd wachtwoord ingevuld. Daarom kun je <b>15 minuten</b> niet inloggen met je wachtwoord. Zo houden we je account veilig.</p>
      <div class="alert warn" role="status">{ic("clock")}<div><b>Probeer het weer om 10:45</b>Of kies nu meteen een nieuw wachtwoord. Dan kun je direct weer inloggen.</div></div>
      <div class="stack"><a class="pub-btn pub-btn--primary pub-btn--block">Nieuw wachtwoord kiezen</a>
      <a class="pub-btn pub-btn--secondary prov" style="justify-content:center">{MSFT}Inloggen met Microsoft</a>
      <a class="pub-btn pub-btn--secondary prov" style="justify-content:center">{GOOGLE}Inloggen met Google</a></div>
      <div class="foot"><span>Was jij dit niet? Kies een nieuw wachtwoord. We sturen je ook een mail.</span><span><a href="mailto:support@lobsy.nl">Hulp nodig? Mail support</a></span></div>
    </div><p class="below"><a href="/login">Terug naar inloggen</a></p></div>'''
    return page(body, m, right)

def mfa_prompt(m, recovery=False):
    code = ('<div class="field"><label>Code uit je app</label><div class="otp" aria-label="6 cijfers">'
            '<span>4</span><span>8</span><span>2</span><span class="cur">0</span><span class="e">·</span><span class="e">·</span></div>'
            '<span class="help">6 cijfers · de code verandert elke 30 seconden</span></div>')
    rec = (f'<div class="disc">{ic("key")}Geen telefoon bij de hand? Gebruik een herstelcode{ic("chev")}</div>')
    body = f'''<div class="col"><div class="pub-card">{peek()}
      {topline("Stap 2 van 2")}
      <span class="emo-c sky" aria-hidden="true">🛡️</span>
      <h1>Nog één stap</h1>
      <p class="lead">Open je authenticator-app (bijvoorbeeld Microsoft Authenticator) en vul de code in die bij <b>Lobsy</b> staat.</p>
      <div class="who"><span class="av">DV</span>d•••@bakkerijzon.nl<a href="/login">Ander account</a></div>
      {code}
      {chk("Vertrouw dit apparaat 30 dagen", False, "Dan vragen we hier niet elke keer om een code.")}
      <button class="pub-btn pub-btn--primary pub-btn--block">Inloggen</button>
      <div style="margin-top:14px">{rec}</div>
      <div class="foot"><span>App kwijt en geen herstelcodes? <a href="mailto:support@lobsy.nl">Mail support</a>. We zetten 2FA dan opnieuw voor je klaar.</span></div>
    </div><p class="below">Deze stap is verplicht voor werkgevers en beheerders. <a href="/hoe-werkt-lobsy#2fa">Waarom?</a></p></div>'''
    return page(body, m)

def mfa_setup(m):
    steps = '<ol class="steps"><li class="done">Account</li><li class="on">Beveiligen</li><li>Herstelcodes</li></ol>'
    s1 = f'<div class="num"><span class="n">1</span><div><b>Installeer een authenticator-app</b><p>Heb je er al een? Dan kun je deze stap overslaan.</p><div class="apps"><span class="pub-chip">Microsoft Authenticator</span><span class="pub-chip">Google Authenticator</span></div></div></div>'
    if m:
        s2 = (f'<div class="num"><span class="n">2</span><div style="flex:1"><b>Voeg Lobsy toe aan je app</b><p>Tik op de knop. Je app opent en zet Lobsy erin.</p>'
              f'<a class="pub-btn pub-btn--primary pub-btn--block" style="margin-top:10px">{ic("ext")}Open in authenticator-app</a>'
              f'<p style="margin-top:10px">Lukt dat niet? Vul deze sleutel in:</p><div class="key"><code>JBSW Y3DP EHPK 3PXP</code><button class="pub-btn pub-btn--ghost pub-btn--sm">{ic("copy")}Kopieer</button></div>'
              f'<div class="disc" style="margin-top:10px">{ic("phone")}App op een andere telefoon? Toon QR-code{ic("chev")}</div></div></div>')
        grid = s1 + s2
    else:
        s2 = (f'<div class="num"><span class="n">2</span><div style="flex:1"><b>Scan de QR-code</b><p>Kies in je app <i>Account toevoegen</i> en scan de code.</p>'
              f'<p style="margin-top:10px">Scannen lukt niet? Vul deze sleutel in:</p><div class="key"><code>JBSW Y3DP EHPK 3PXP</code><button class="pub-btn pub-btn--ghost pub-btn--sm">{ic("copy")}Kopieer</button></div>'
              f'<div style="margin-top:8px"><span class="toast" role="status">{ic("check")}Sleutel gekopieerd</span></div></div></div>')
        grid = f'<div class="setup"><div class="qr"><img src="{QR}" alt="QR-code om Lobsy toe te voegen aan je authenticator-app"><small>Account: voorbeeld@bedrijf.nl</small></div><div>{s1}{s2}</div></div>'
    s3 = ('<div class="num" style="margin-top:6px"><span class="n">3</span><div style="flex:1"><b>Vul de code uit je app in</b><p>Zo weten we dat het gelukt is.</p>'
          '<div class="otp" style="margin-top:10px"><span>1</span><span>9</span><span class="cur">0</span><span class="e">·</span><span class="e">·</span><span class="e">·</span></div></div></div>')
    body = f'''<div class="col {"wide" if not m else ""}"><div class="pub-card">{peek()}
      {topline("Bedrijf registreren · account beveiligen")}
      <h1>Beveilig je account</h1>
      <p class="lead">Werkgevers en beheerders loggen in met een extra code uit een app op je telefoon. Dat duurt 2 minuten.</p>
      {steps}{grid}{s3}
      <button class="pub-btn pub-btn--primary pub-btn--block" style="margin-top:8px">Bevestigen</button>
      <div class="foot"><span>Log je liever in met Microsoft of Google? Dan heb je deze stap niet nodig. <a href="/login">Kies een andere manier</a></span></div>
    </div></div>'''
    return page(body, m)

CODES = ["7K2F-9QXM", "B4TN-6WRE", "H8PC-3JVA", "M5DZ-2YLU", "Q9GS-7NKB", "R3WX-8FTH", "T6LE-4CPM", "V2AJ-9DQS", "X7NB-5HGR", "Z4KU-6MWE"]
def recovery(m):
    lis = "".join(f"<li>{c}</li>" for c in CODES)
    steps = '<ol class="steps"><li class="done">Account</li><li class="done">Beveiligen</li><li class="on">Herstelcodes</li></ol>'
    body = f'''<div class="col {"wide" if not m else ""}"><div class="pub-card">{peek()}
      {topline("Laatste stap")}
      {steps}
      <div class="alert ok" role="status">{ic("check")}<div><b>Gelukt! Je account is beveiligd</b>Je logt voortaan in met je wachtwoord én een code uit je app.</div></div>
      <h1>Bewaar je herstelcodes</h1>
      <p class="lead">Telefoon kwijt of kapot? Dan log je in met één van deze codes. Elke code werkt één keer. <b>We laten ze maar één keer zien.</b></p>
      <ul class="codes" aria-label="Herstelcodes">{lis}</ul>
      <div class="row" style="margin-bottom:18px"><button class="pub-btn pub-btn--secondary pub-btn--sm">{ic("dl")}Download (.txt)</button><button class="pub-btn pub-btn--secondary pub-btn--sm">{ic("copy")}Kopieer</button>{"" if m else '<button class="pub-btn pub-btn--secondary pub-btn--sm">'+ic("print")+'Print</button>'}<span class="toast" role="status" style="align-self:center">{ic("check")}Gekopieerd</span></div>
      <p class="help" style="margin:-4px 0 14px">Tip: bewaar ze in je wachtwoordmanager of print ze uit. Niet in je mail.</p>
      {chk("Ik heb mijn herstelcodes veilig bewaard", True)}
      <button class="pub-btn pub-btn--primary pub-btn--block">Verder naar stap 4: je bedrijf</button>
    </div></div>'''
    return page(body, m)

def forgot(m, sent=False):
    if sent:
        inner = f'''{topline()}<span class="emo-c mint" aria-hidden="true">📬</span>
      <h1>Kijk in je mail</h1>
      <p class="lead">Hoort <b>d.devries@bakkerijzon.nl</b> bij een Lobsy-account? Dan krijg je binnen een paar minuten een mail met een link. Die link werkt 30 minuten.</p>
      <div class="alert info">{ic("mail")}<div><b>Geen mail gekregen?</b>Kijk ook bij ongewenste mail. Of log in met Microsoft of Google als je dat eerder deed.</div></div>
      <button class="pub-btn pub-btn--secondary pub-btn--block" disabled>Stuur opnieuw (over 0:42)</button>
      <div class="foot"><a href="/wachtwoord-vergeten">Ander e-mailadres</a><a href="/login">Terug naar inloggen</a></div>'''
    else:
        inner = f'''{topline()}<span class="emo-c" aria-hidden="true">🔑</span>
      <h1>Wachtwoord vergeten?</h1>
      <p class="lead">Geen probleem. Vul je e-mailadres in. We sturen je een link om een nieuw wachtwoord te kiezen.</p>
      {inp("E-mailadres", "d.devries@bakkerijzon.nl", "", "focus")}
      <button class="pub-btn pub-btn--primary pub-btn--block" style="margin-top:4px">Stuur de link</button>
      <div class="foot"><span>Log je in met Microsoft of Google? Dan heb je geen Lobsy-wachtwoord. <a href="/login">Kies die knop</a></span><a href="/login">Terug naar inloggen</a></div>'''
    return f'<div class="pub-card">{peek() if not sent else peek("l")}{inner}</div>'

def forgot_page(m, which="both"):
    if which == "both":
        body = f'<div class="col duo">{forgot(m)}{forgot(m, True)}</div>'
    else:
        body = f'<div class="col">{forgot(m, which == "sent")}</div>'
    return page(body, m)

def newpw(m):
    body = f'''<div class="col"><div class="pub-card">{peek()}
      {topline("/account/wachtwoord-instellen")}
      <h1>Kies een nieuw wachtwoord</h1><p class="lead">Voor <b>d.devries@bakkerijzon.nl</b></p>
      {inp("Nieuw wachtwoord", "••••••••••••", "", "focus", EYE)}
      <div class="stack" style="gap:6px;margin:-4px 0 16px;font-size:var(--text-sm)"><span class="pub-chip ok">{ic("check")}Minstens 10 tekens</span><span class="pub-chip ok">{ic("check")}Niet een bekend wachtwoord</span></div>
      <button class="pub-btn pub-btn--primary pub-btn--block">Opslaan en inloggen</button>
      <div class="foot"><span>Na het opslaan loggen we je uit op je andere apparaten.</span></div>
    </div></div>'''
    return page(body, m)

def login_ar(m):
    right = '' if m else '<a class="pub-btn pub-btn--secondary pub-btn--sm">إنشاء حساب</a>'
    body = f'''<div class="col"><div class="pub-card">{peek()}
      <div class="topline"><span class="pub-eyebrow">مرحبًا بعودتك</span><span class="vb">Voorbeelddata</span></div>
      <h1>تسجيل الدخول إلى Lobsy</h1><p class="lead">اختر طريقة تسجيل الدخول.</p>
      <div class="stack"><a class="pub-btn pub-btn--secondary prov">{MSFT}تسجيل الدخول عبر Microsoft</a><a class="pub-btn pub-btn--secondary prov">{GOOGLE}تسجيل الدخول عبر Google</a></div>
      <div class="divider">أو بالبريد الإلكتروني</div>
      <div class="field"><div class="lblrow"><label>البريد الإلكتروني</label></div><div class="inp" dir="ltr" style="justify-content:flex-end"><span>d.devries@bakkerijzon.nl</span></div></div>
      <div class="field"><div class="lblrow"><label>كلمة المرور</label><a>نسيت كلمة المرور؟</a></div><div class="inp"><span>•••••••</span><span class="eye">{ic("eye")}إظهار</span></div></div>
      {chk("البقاء مسجّل الدخول على هذا الجهاز", False, "لا تفعّل هذا على جهاز مشترك.")}
      <button class="pub-btn pub-btn--primary pub-btn--block">تسجيل الدخول</button>
      <div class="foot"><span>جديد على Lobsy؟ <a>أنشئ حسابًا مجانيًا</a></span><span>لأصحاب العمل: <a>تسجيل شركة (KvK)</a></span></div>
    </div></div>'''
    return page(body, m, right, "AR", rtl=True)

SCREENS = {
 "au-d01-login": (login, False, {}), "au-m01-login": (login, True, {}),
 "au-d02-login-fout": (login, False, {"state": "err"}), "au-m02-login-fout": (login, True, {"state": "err"}),
 "au-d03-login-pauze": (locked, False, {}), "au-m03-login-pauze": (locked, True, {}),
 "au-d04-2fa-code": (mfa_prompt, False, {}), "au-m04-2fa-code": (mfa_prompt, True, {}),
 "au-d05-2fa-instellen": (mfa_setup, False, {}), "au-m05-2fa-instellen": (mfa_setup, True, {}),
 "au-d06-herstelcodes": (recovery, False, {}), "au-m06-herstelcodes": (recovery, True, {}),
 "au-d07-wachtwoord-vergeten": (forgot_page, False, {"which": "both"}), "au-m07-wachtwoord-vergeten": (forgot_page, True, {"which": "form"}),
 "au-m08-wachtwoord-mail-verstuurd": (forgot_page, True, {"which": "sent"}),
 "au-d09-nieuw-wachtwoord": (newpw, False, {}), "au-m09-nieuw-wachtwoord": (newpw, True, {}),
 "au-d10-login-ar-rtl": (login_ar, False, {}), "au-m10-login-ar-rtl": (login_ar, True, {}),
}
if __name__ == "__main__":
    out = d / "html"; out.mkdir(exist_ok=True)
    for name, (fn, m, kw) in SCREENS.items():
        (out / f"{name}.html").write_text(fn(m, **kw), encoding="utf-8")
    print(len(SCREENS), "html")
