# Genereert alle kreeft-kandidaatschermen in reisvolgorde + manifest screens.json
import json, os, re, shutil, html
H=os.path.dirname(os.path.abspath(__file__)); os.chdir(H)
META=json.load(open('strips/meta.json'))
IL='../illustraties/'
SCREENS=[]
COPY=[]  # (scherm, sleutel, nl, en)
def T(nl,en,key='',scr=''):
    COPY.append((scr,key,nl,en)); return nl

def strip(name, rel=''):
    m=META[name]
    return f'<div class="kstrip" style="--dh:{m["dh"]}px;--mh:{m["mh"]}px" aria-hidden="true"><img class="desk" src="{rel}strips/{name}-desk.jpg" alt=""><img class="mob" src="{rel}strips/{name}-mob.jpg" alt=""></div>'

def ask(rel=''):
    return f'<a class="kask" href="#hulp"><img src="{rel}lobsy-merkteken.svg" alt="">Lobsy-hulp</a>'

def coach(nl,en,scr,rel=''):
    T(nl,en,'notitie',scr)
    return f'<div class="kcoach"><div><b>Goed om te weten</b>{nl}</div></div>'

NAV=[('Reis','/candidate/ontdekkingsreis'),('Paspoort','/candidate/paspoort'),('Zoeken','/banenkaart'),('Sollicitaties','/candidate/applications'),('Carrière','/carriere')]
def header(active='', loggedin=True):
    if not loggedin:
        return '''<header class="app-header"><a class="brand" href="/"><img src="lobsy-logo-horizontaal.svg" alt="Lobsy"></a>
<nav class="nav"><a href="#">Hoe werkt Lobsy</a><a href="#">Voor werkgevers</a><a href="#">Over ons</a></nav>
<div class="utils"><span class="lang"><img src="nl.svg" alt="">NL</span><a class="btn btn--sm" href="#">Inloggen</a><a class="btn btn--sm btn--primary only-desk" href="#">Account maken</a></div></header>'''
    nav=''.join(f'<a href="{u}"{" class=\"is-active\"" if n==active else ""}>{n}</a>' for n,u in NAV)
    return f'''<header class="app-header"><a class="brand" href="/home"><img src="lobsy-logo-horizontaal.svg" alt="Lobsy"></a>
<nav class="nav">{nav}</nav>
<div class="utils"><a class="util" href="#">i</a><a class="util" href="#"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M6 16V11a6 6 0 0 1 12 0v5l1.5 2h-15z"/><path d="M10 20a2 2 0 0 0 4 0"/></svg></a><span class="lang"><img src="nl.svg" alt="">NL</span><span class="account"><span class="name">Roland van der Velde</span><span class="av">RV</span></span></div></header>'''

FOOT='<footer class="footer"><nav><a href="#">Privacyverklaring</a><a href="#">Algemene voorwaarden</a><a href="#">Toegankelijkheid</a><a href="#">Contact</a></nav><span>© 2026 Lobsy</span></footer>'

def page(slug,title,body,extra_css='',askbtn=True):
    return f'''<!DOCTYPE html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Lobsy · {title}</title>
<link rel="stylesheet" href="base.css"><link rel="stylesheet" href="kreeft.css"><style>{extra_css}</style></head><body><div class="page-shell">{body}{FOOT}</div>{ask() if askbtn else ''}</body></html>'''

def add(num,slug,chapter,title,caption,body=None,file=None,illus='',why='',kind='eigen',css='',askbtn=True,wait=0,src=''):
    fn=file or f'{num:02d}-{slug}.html'
    if body is not None:
        open(fn,'w').write(page(slug,title,body,css,askbtn))
    SCREENS.append(dict(num=num,slug=slug,chapter=chapter,title=title,caption=caption,file=fn,illus=illus,why=why,kind=kind,wait=wait,src=src))

# ---------------- onboarding helpers ----------------
ZONES=[('Strand','Start','Welkom en toestemming'),('Kust','Over jou','3 stappen'),('Rotsen','Achtergrond en Droom','3 stappen'),('Diepte','Wie ben jij','4 korte tests'),('Licht','Jouw Kompas','je resultaat')]
def rail(zi, stepname, shed):
    lis=''
    for i,(z,s_,sub) in enumerate(ZONES):
        cls='done' if i<zi else ('on' if i==zi else '')
        small=f'<small>{stepname}</small>' if i==zi else f'<small>{sub}</small>'
        lis+=f'<li class="{cls}"><i></i><div><b>{z} · {s_}</b>{small}</div></li>'
    return f'<aside class="rail"><ol>{lis}</ol></aside>'
def lagentekst(shed,new=False,nu=True):
    if new: return 'Je bent uit je schild. Alle tien lagen zijn los en je nieuwe schild zit goed.','You are out of your shell. All ten layers are off and your new shell fits well.'
    if shed>0 and not nu: return f'Nog {10-shed} lagen schild. Er zijn er al {shed} los.',f'Still {10-shed} layers of shell. {shed} have already come off.'
    if shed==0 and not nu: return 'Nog 10 lagen schild. Bij elke stap laat er een los. In het Licht ben je uit je schild.','Still 10 layers of shell. With every step one comes off. In the Light you are out of your shell.'
    left=10-shed
    nl=f'Nog {left} {"laag" if left==1 else "lagen"} schild. Na deze stap laat laag {shed+1} los.'
    en=f'Still {left} {"layer" if left==1 else "layers"} of shell. After this step layer {shed+1} comes off.'
    return nl,en
def reiskaart(zi,shed,kop,sub,scr,tip=None,new=False,nu=True):
    """Reiskaart: waar je bent (vijf zones), je schild in tien lagen en een rustige zin in je-vorm."""
    route=''.join(f'<div class="{"done" if i<zi else ("nu" if i==zi else "")}"><i></i>{z}</div>' for i,(z,_,_) in enumerate(ZONES))
    lag=''.join('<span class="los"></span>' if (i<shed and not new) else '<span></span>' for i in range(10))
    nl,en=lagentekst(shed,new,nu); T(nl,en,'reiskaart',scr)
    tp=''
    if tip: T(tip[0],tip[1],'tip',scr); tp=f'<p class="tip">{tip[0]}</p>'
    return f'<div class="reis" aria-label="Waar je bent op je reis"><div class="kop"><b>{kop}</b><span>{sub}</span></div><div class="route">{route}</div><div class="lagen{" nieuw" if new else ""}" aria-label="{nl}">{lag}</div><p>{nl}</p>{tp}</div>'

def ob_page(num,slug,zi,stepname,stepinfo,shed,pose,stripname,eyebrow,title_nl,title_en,lead_nl,lead_en,coach_nl,coach_en,content,illus,why,back=True,next_label='Verder'):
    scr=f'{num:02d}'
    T(title_nl,title_en,'titel',scr); T(lead_nl,lead_en,'intro',scr)
    z,s,_=ZONES[zi]
    body=header('Reis')+strip(stripname)+f'''<main class="wrap"><div class="ob">{rail(zi,stepname,shed)}
<div>
<section class="qcard"><p class="zonetag"><b>{z}</b>{s} · {stepinfo}</p><h1 class="k">{title_nl}</h1><p class="lead">{lead_nl}</p>{content}
<div class="foot">{'<a class="btn" href="#">Terug</a>' if back else '<span></span>'}<span class="muted">Je antwoorden worden bewaard. Pauzeren kan altijd.</span><a class="btn btn--primary" href="#">{next_label}</a></div></section></div>
<div class="gside">{reiskaart(zi,shed,f'{z} · {s}',stepinfo,scr,(coach_nl,coach_en))}</div></div></main>'''
    add(num,slug,'Onboarding' if zi<3 else ('Tests' if zi==3 else 'Resultaat'),f'{z} · {stepname}',f'{z} · {s}: {title_nl}',body,illus=illus,why=why)

def opt(t,on=False,sub='',sq=False):
    return f'<div class="opt{" sq" if sq else ""}{" on" if on else ""}"><i></i><div>{t}{f"<small>{sub}</small>" if sub else ""}</div></div>'
def field(label,val):
    return f'<div class="field"><label>{label}</label><div class="input">{val}</div></div>'
def scaleq(n,q,on):
    lab=['Helemaal niet','Niet echt','Soms','Vaak','Helemaal wel']
    return f'<p class="qnum">Vraag {n}</p><p class="qlabel">{q}</p><div class="scale">'+''.join(f'<span class="{"on" if i==on else ""}">{l}</span>' for i,l in enumerate(lab))+'</div>'

# ================= 01 landing (moment 0) =================
c='01'
T('Werk dat bij je past. Rustig gevonden.','Work that suits you. Found calmly.','titel',c)
T('Dit is jouw reis. Stap voor stap kom je uit je schild.','This is your journey. Step by step you come out of your shell.','moment 0',c)
T('Je begint op het strand, nog helemaal in je schild. Je vertelt eerst wie je bent en wat je kunt, daarna pas kijk je naar vacatures. Voor iedereen, met of zonder diploma, jong of wat ouder, net begonnen of al jaren aan het werk.','You start on the beach, still fully in your shell. First you tell who you are and what you can do, only then do you look at vacancies. For everyone, with or without a diploma, young or a bit older, just starting or working for years.','intro',c)
T('Stap op het strand','Step onto the beach','knop',c)
steps=[('Strand','Je begint','Je maakt een account en stapt op het strand. Je leest wat er met je gegevens gebeurt.','Nog alle 10 lagen','You create an account and step onto the beach. You read what happens with your data.','All 10 layers still on'),
('Kust','Over jou','Wanneer en waar je kunt werken, en hoe je er komt.','Lagen 1 tot en met 3','When and where you can work, and how you get there.','Layers 1 to 3'),
('Rotsen','Achtergrond en droom','Wat je al deed, wat je leerde en waar je heen wilt.','Lagen 4 tot en met 6','What you did, what you learned and where you want to go.','Layers 4 to 6'),
('Diepte','Wie ben jij','Vier korte tests, zonder goed of fout. Je kijkt naar binnen.','Lagen 7 tot en met 10','Four short tests, no right or wrong. You look inward.','Layers 7 to 10'),
('Licht','Jouw Kompas','Je ziet wat bij je past. Je bent uit je schild en je paspoort is klaar.','Nieuw schild','You see what suits you. You are out of your shell and your passport is ready.','New shell')]
for z,s_,d,l,de,le in steps: T(f'{z} · {s_}: {d} {l}.',f'{z}: {de} {le}.','zone',c)
st=''.join(f'<div class="card zcard"><p class="zonetag"><b>{z}</b>{s_}</p><p class="muted small" style="margin:0">{d}</p><p class="laag">{l}</p></div>' for z,s_,d,l,de,le in steps)
body=header(loggedin=False)+strip('landing')+f'''<main class="wrap"><div class="land">
<div><p class="eyebrow">Voor werkzoekenden</p><h1 class="k big">Werk dat bij je past. Rustig gevonden.</h1>
<p class="moment0">Dit is jouw reis. Stap voor stap kom je uit je schild.</p>
<p class="lead">Je begint op het strand, nog helemaal in je schild. Je vertelt eerst wie je bent en wat je kunt, daarna pas kijk je naar vacatures. Voor iedereen, met of zonder diploma, jong of wat ouder, net begonnen of al jaren aan het werk.</p>
<div class="row"><a class="btn btn--primary" href="#">Stap op het strand</a><a class="btn" href="#">Ik heb al een account</a></div>
<p class="muted small" style="margin-top:12px">Gratis. Ongeveer 20 minuten. Je kunt altijd pauzeren en later verder.</p></div>
{reiskaart(0,0,'Moment 0','Je staat aan het begin',c,nu=False)}</div>
<h2 class="sec-t">Zo loopt je reis</h2><div class="g5">{st}</div>
<div class="g3" style="margin:28px 0 10px"><div class="card"><h3>Eerst jij, dan het werk</h3><p class="muted small" style="margin:0">Je vertelt wat je kunt en wat je belangrijk vindt. Werkgevers zien meer dan een cv.</p></div><div class="card"><h3>Geen goed of fout</h3><p class="muted small" style="margin:0">De tests zijn kort en vriendelijk. Je ziet woorden die bij je passen, geen cijfers.</p></div><div class="card"><h3>Jij houdt de regie</h3><p class="muted small" style="margin:0">Jij bepaalt wat werkgevers zien. Je gegevens blijven van jou.</p></div></div>
</main>'''
add(1,'landing','Start en inloggen','Landingspagina (uitgelogd), moment 0','Moment 0: je loopt bij zonsopgang over het strand, nog helemaal in je schild, het pad op naar de vuurtoren.',body,illus='landing.jpg',
 css='.land{display:grid;grid-template-columns:1fr 340px;gap:44px;align-items:center;padding:18px 0 26px}.big{font-size:2.7rem}.moment0{font-family:Fraunces,Georgia,serif;font-size:1.25rem;color:var(--text);margin:2px 0 14px}.g5{display:grid;grid-template-columns:repeat(5,1fr);gap:12px}.zcard .laag{font-size:.74rem;color:var(--muted);margin:6px 0 0}@media(max-width:900px){.land{grid-template-columns:1fr;gap:14px;padding:12px 0 18px}.big{font-size:1.9rem}.g5{grid-template-columns:1fr}.land .row .btn{flex:1}}',askbtn=False)

# ================= 02 inloggen =================
c='02'
T('Fijn dat je er bent','Good to have you here','titel',c)
T('Log in met je e-mailadres. Nog geen account? Dat maak je in een minuut.','Log in with your email address. No account yet? You create one in a minute.','intro',c)
body=header(loggedin=False)+strip('inloggen')+f'''<main class="wrap"><div class="login">
<section class="qcard"><div class="tabs"><a class="is-active" href="#">Inloggen</a><a href="#">Account maken</a></div>
<h1 class="k" style="margin-top:18px">Fijn dat je er bent</h1><p class="lead">Log in met je e-mailadres. Nog geen account? Dat maak je in een minuut.</p>
{field('E-mailadres','roland@voorbeeld.nl')}{field('Wachtwoord','••••••••••')}
<div class="between" style="margin:6px 0 18px"><span class="small"><span class="sw on" style="display:inline-block;vertical-align:middle;margin-right:8px;transform:scale(.8)"></span>Ingelogd blijven</span><a class="link" href="#">Wachtwoord vergeten?</a></div>
<a class="btn btn--primary" style="width:100%" href="#">Inloggen</a>
<p class="muted small" style="margin:16px 0 0;text-align:center">Lukt het niet? Je adviseur of de helpdesk helpt je graag.</p></section>
{reiskaart(0,0,'Moment 0','Je stapt het strand op',c,('Na het inloggen begint je reis op het strand.','After logging in your journey starts on the beach.'),nu=False)}</div></main>'''
add(2,'inloggen','Start en inloggen','Inloggen en account maken','Inloggen of een account maken. Je staat bij het strandpaviljoen, klaar om het strand op te stappen.',body,illus='inloggen.jpg',
 css='.login{display:grid;grid-template-columns:minmax(0,480px) 320px;justify-content:center;gap:32px;align-items:start;padding:22px 0 36px}@media(max-width:900px){.login{grid-template-columns:1fr;gap:14px;padding:10px 0 24px}.login .reis{order:-1}}',askbtn=False)

# ================= 03 start strand =================
c='03'
T('Welkom bij je reis','Welcome to your journey','titel',c)
T('Je loopt in vijf rustige stukken van het strand naar het licht. Onderweg leer je jezelf beter kennen.','You walk in five calm stretches from the beach to the light. Along the way you get to know yourself better.','intro',c)
zl=[('Strand · Start','Hier begin je. Je leest wat er met je gegevens gebeurt.','This is where you start. You read what happens with your data.'),
('Kust · Over jou','Een paar basisvragen: wanneer en waar je kunt werken.','A few basic questions: when and where you can work.'),
('Rotsen · Achtergrond en Droom','Wat je al hebt gedaan en waar je naartoe wilt.','What you have done so far and where you want to go.'),
('Diepte · Wie ben jij','Vier korte tests over hoe je werkt en wat je belangrijk vindt.','Four short tests about how you work and what matters to you.'),
('Licht · Jouw Kompas','Je ziet wat bij je past en je paspoort is klaar.','You see what suits you and your passport is ready.')]
for a,b,e in zl: T(b,e,'zone '+a,c)
zh=''.join(f'<li><b>{a}</b><span>{b}</span></li>' for a,b,e in zl)
body=header('Reis')+strip('route-kaart')+f'''<main class="wrap"><div class="startg"><div>
<p class="zonetag"><b>Strand</b>Start · welkom en toestemming</p><h1 class="k">Welkom bij je reis, Roland</h1>
<p class="lead">Je loopt in vijf rustige stukken van het strand naar het licht. Onderweg leer je jezelf beter kennen.</p>
<ol class="zl">{zh}</ol>
<div class="card" style="margin-top:18px"><h3>Voor je begint</h3>
<div class="opt sq on" style="margin-bottom:8px"><i></i><div>Ik snap dat Lobsy mijn antwoorden gebruikt om werk te vinden dat bij me past.<small>Werkgevers zien pas iets als jij dat goed vindt.</small></div></div>
<div class="opt sq"><i></i><div>Lobsy mag me af en toe een tip sturen per e-mail.<small>Niet verplicht. Je kunt dit later aanpassen.</small></div></div>
<div class="between" style="margin-top:16px"><a class="link" href="#">Lees wat er met je gegevens gebeurt</a><a class="btn btn--primary" href="#">Begin aan de kust</a></div></div></div>
<div class="gside">{reiskaart(0,0,'Strand · Start','welkom en toestemming',c,('Je oude schild zit wat krap. Onderweg ontdek je wie eronder zit.','Your old shell is a bit tight. Along the way you find out who is underneath.'),nu=False)}</div></div></main>'''
add(3,'start-strand','Onboarding','Strand · Start','Na het inloggen: de route in vijf zones. Op de kaart sta je klein op het strand, aan het begin van het pad.',body,illus='route-kaart.jpg',
 css='.startg{display:grid;grid-template-columns:1fr 300px;gap:40px;padding:14px 0 30px}.zl{list-style:none;padding:0;margin:0;display:grid;gap:8px}.zl li{display:grid;grid-template-columns:240px 1fr;gap:14px;padding:10px 14px;background:#fff;border:1px solid var(--line);border-radius:12px}.zl b{font-family:Fraunces,Georgia,serif;color:var(--terra);font-weight:600}.zl span{color:var(--muted);font-size:.92rem}@media(max-width:900px){.startg{grid-template-columns:1fr;gap:12px}.gside{order:-1}.zl li{grid-template-columns:1fr;gap:2px}}')

# ================= 04-14 onboarding & tests =================
ob_page(4,'kust-kennismaken',1,'Stap 1 · Even kennismaken','stap 1 van 3',0,'meelopen','kust-kennismaken','',
 'Even kennismaken',"Let's get acquainted",'Eerst wat basisgegevens.','First some basic details.',
 'Je begint rustig: alleen je naam en waar je woont.','You start calmly: just your name and where you live.',
 '<div class="g2">'+field('Voornaam','Roland')+field('Achternaam','van der Velde')+'</div><div class="g2">'+field('Postcode','2611 AB')+field('Woonplaats','Delft')+'</div>'+field('Hoe wil je aangesproken worden?','Roland')+'<p class="qlabel">Welke taal spreek je het liefst?</p><div class="chips"><span class="chip is-active">Nederlands</span><span class="chip">English</span><span class="chip">Beide</span></div>',
 'kust-kennismaken.jpg','Een dorpsstraat met verschillende voordeuren: je stelt je voor bij de buren. Kennismaken, waar je woont.',back=False)
ob_page(5,'kust-wanneer',1,'Stap 2 · Wanneer kun je werken?','stap 2 van 3',1,'meelopen','kust-wanneer','',
 'Wanneer kun je werken?','When can you work?','Kies wat nu bij je leven past. Je kunt het later altijd aanpassen.','Choose what fits your life now. You can always change it later.',
 'Er is geen goed of fout. Kies wat nu bij je leven past.','There is no right or wrong. Choose what fits your life now.',
 '<p class="qlabel">Hoeveel uur per week?</p><div class="chips"><span class="chip">Tot 16 uur</span><span class="chip is-active">16 tot 32 uur</span><span class="chip">32 uur of meer</span><span class="chip">Weet ik nog niet</span></div><p class="qlabel">Op welke momenten?</p><div class="opts two">'+opt('Doordeweeks overdag',True,sq=True)+opt('Avonden',sq=True)+opt('Weekenden',True,sq=True)+opt('Wisselende diensten',sq=True)+'</div><p class="qlabel">Vanaf wanneer?</p><div class="chips"><span class="chip is-active">Zo snel mogelijk</span><span class="chip">Binnen een maand</span><span class="chip">Later</span></div>',
 'kust-wanneer.jpg','Een dagboog met getijdenpaal en strandhuisjes: ochtend, middag, avond en eb en vloed. Over tijd en ritme.')
ob_page(6,'kust-vervoer',1,'Stap 3 · Hoe kom je op je werk?','stap 3 van 3',2,'meelopen','kust-vervoer','',
 'Hoe kom je op je werk?','How do you get to work?','Dan zie je werk binnen een afstand die voor jou haalbaar is.','Then you see work within a distance that works for you.',
 'Kies hoe ver je wilt reizen. Je ziet dan alleen werk binnen die afstand.','Choose how far you want to travel. You then only see work within that distance.',
 '<div class="opts two">'+opt('Fiets of e-bike',True,sq=True)+opt('Openbaar vervoer',True,sq=True)+opt('Auto',sq=True)+opt('Lopend',sq=True)+'</div><p class="qlabel">Hoe lang wil je maximaal reizen, enkele reis?</p><div class="chips"><span class="chip">15 min</span><span class="chip is-active">30 min</span><span class="chip">45 min</span><span class="chip">1 uur</span></div>',
 'kust-vervoer.jpg','Duinen met fiets, bus, auto, veerboot en trein: alle manieren om ergens te komen.')
ob_page(7,'rotsen-werk',2,'Stap 4 · Heb je al gewerkt?','achtergrond · optioneel',3,'wijzen','rotsen-werk','',
 'Heb je al gewerkt?','Have you worked before?','Bijbaan, vrijwilligerswerk of stage telt ook mee.','A side job, volunteering or an internship also counts.',
 'Alles telt mee. Ook vrijwilligerswerk en zorgen voor anderen.','Everything counts. Volunteering and caring for others too.',
 '<div class="opts">'+opt('Magazijnmedewerker bij Distributie Delfland',True,'2019 tot nu · 24 uur per week')+opt('Vrijwilliger bij de voedselbank',True,'2021 tot nu · zaterdag')+'</div><a class="btn btn--sm" style="margin-top:12px" href="#">+ Werkervaring toevoegen</a><p class="qlabel">Waar ben je goed in geworden?</p><div class="chips"><span class="chip is-active">Plannen</span><span class="chip is-active">Samenwerken</span><span class="chip">Klanten helpen</span><span class="chip is-active">Netjes werken</span><span class="chip">Techniek</span></div>',
 'rotsen-werk.jpg','Een schuurtje met net, boot, kruiwagen en terrastafel tussen de rotsen: allerlei soorten werk, ook klein en informeel.',next_label='Verder of overslaan')
ob_page(8,'rotsen-leren',2,'Stap 5 · Wat is je opleiding?','achtergrond · optioneel',4,'wijzen','rotsen-leren','',
 'Wat is je opleiding?','What is your education?','Ook cursussen en dingen die je zelf hebt geleerd tellen mee.','Courses and things you taught yourself count too.',
 'Een diploma is fijn, maar wat je kunt telt net zo zwaar.','A diploma is nice, but what you can do counts just as much.',
 '<p class="qlabel">Hoogst afgeronde opleiding</p><div class="chips"><span class="chip">Basisschool</span><span class="chip is-active">Vmbo of mbo 1</span><span class="chip">Mbo 2 tot 4</span><span class="chip">Havo of vwo</span><span class="chip">Hbo of wo</span><span class="chip">Anders</span></div><p class="qlabel">Cursussen en certificaten</p><div class="opts">'+opt('Heftruckcertificaat',True,'2020',sq=True)+opt('EHBO',True,'2023',sq=True)+'</div>',
 'rotsen-leren.jpg','Een stenen schooltje in de rotsen met boeken, lei, sextant en lantaarn: leren op school en leren onderweg.')
ob_page(9,'rotsen-droom',2,'Stap 6 · Wat is je droombaan?','droom · 1 stap',5,'wijzen','rotsen-droom','',
 'Wat is je droombaan?','What is your dream job?','Denk groot. In Carrière zie je later een route ernaartoe.','Think big. In Career you will later see a route to it.',
 'Denk gerust groot. De weg ernaartoe zoek je later stap voor stap uit.','Feel free to dream big. You work out the way there later, step by step.',
 field('Mijn droombaan','Coördinator in de zorg')+'<p class="qlabel">Past bij jou</p><div class="chips"><span class="chip">Planner zorgroutes</span><span class="chip">Teamleider logistiek</span><span class="chip">Begeleider dagbesteding</span></div><p class="qlabel">Populair in de buurt <span class="vv" style="margin-left:6px">Veel vraag in jouw regio</span></p><div class="chips"><span class="chip">Medewerker thuiszorg</span><span class="chip">Monteur zonnepanelen</span><span class="chip">Doktersassistent</span></div>',
 'rotsen-droom.jpg','Een bankje met verrekijker op een klif boven zee: vooruitkijken, ver weg dromen.')
ob_page(10,'diepte-hoe-werk-jij',3,'Test 1 · Hoe werk jij?','test 1 van 4',6,'rusten','diepte-werken','',
 'Hoe werk jij?','How do you work?','Kies bij elke zin wat het meest op je lijkt.','For each sentence, choose what is most like you.',
 'Kies wat het meest op je lijkt. Je eerste gevoel is meestal goed.','Choose what is most like you. Your first feeling is usually right.',
 scaleq(3,'Ik maak graag eerst een plan voordat ik begin.',3)+'<div class="scale-l"><span>Helemaal niet</span><span>Helemaal wel</span></div><div style="height:14px"></div>'+scaleq(4,'Ik werk het liefst samen met anderen.',4)+'<div style="height:14px"></div>'+scaleq(5,'Ik blijf rustig als het druk wordt.',None),
 'diepte-werken.jpg','Een school vissen, een paar vissen samen en één vis alleen: zo zie je verschillende manieren van werken, samen of zelfstandig.',next_label='Volgende vraag')
ob_page(11,'diepte-leuk',3,'Test 2 · Wat vind je leuk?','test 2 van 4',7,'rusten','diepte-leuk','',
 'Wat vind je leuk?','What do you like?','Kies de dingen die je graag doet. Meerdere mag.','Choose the things you enjoy doing. More than one is fine.',
 'Denk aan dagen waarop de tijd voorbij vloog.','Think of days when time flew by.',
 '<div class="opts two">'+opt('Met mijn handen iets maken of repareren',True,sq=True)+opt('Mensen helpen of verzorgen',True,sq=True)+opt('Dingen regelen en ordenen',True,sq=True)+opt('Buiten zijn, in de natuur',sq=True)+opt('Iets bedenken of ontwerpen',sq=True)+opt('Uitleggen en lesgeven',sq=True)+opt('Met cijfers en computers werken',sq=True)+opt('Verkopen en klanten spreken',sq=True)+'</div>',
 'diepte-leuk.jpg','Een rif waar elk dier iets anders doet: een krab bouwt, een zeepaardje verzorgt, een octopus ordent. Interesses zijn verschillend en allemaal waardevol.')
# pauze
c='12'
T('Even rusten','A short rest','titel',c)
body=header('Reis')+strip('diepte-pauze')+f'''<main class="wrap"><div class="ob">{rail(3,'Even rusten tussen de tests',8)}
<div><section class="qcard pause"><p class="zonetag"><b>Diepte</b>Wie ben jij · halverwege</p><h1 class="k">Even rusten</h1>
<p class="lead">Je hebt twee van de vier tests gedaan. Neem gerust even pauze. Je antwoorden zijn bewaard en je gaat verder waar je was.</p>
<div class="g2"><div class="card"><h3>Gedaan</h3><p class="muted small" style="margin:0">Hoe werk jij?<br>Wat vind je leuk?</p></div><div class="card"><h3>Nog te gaan</h3><p class="muted small" style="margin:0">Waar voel je je thuis?<br>Wat vind je belangrijk?</p></div></div>
<div class="foot"><a class="btn" href="#">Later verder</a><span class="muted">Ongeveer 6 minuten</span><a class="btn btn--primary" href="#">Verder met test 3</a></div></section></div>
<div class="gside">{reiskaart(3,8,'Diepte · Wie ben jij','halverwege',c,('Je bent halverwege de diepte. Ga verder wanneer je wilt.','You are halfway through the deep. Continue whenever you like.'),nu=False)}</div></div></main>'''
add(12,'diepte-pauze','Tests','Diepte · Even rusten','Pauzescherm halverwege de tests. Je zit even op een steen in de grot.',body,illus='diepte-pauze.jpg',why='Een stille grot met een rustplek op de bodem: halverwege de diepte even op adem komen.')
ob_page(13,'diepte-thuis',3,'Test 3 · Waar voel je je thuis?','test 3 van 4',8,'rusten','diepte-thuis','',
 'Waar voel je je thuis?','Where do you feel at home?','Kies de werkplek die het best bij je past.','Choose the workplace that suits you best.',
 'Sommige mensen houden van drukte, anderen van rust. Beide is goed.','Some people like a busy place, others like quiet. Both are fine.',
 '<div class="opts">'+opt('Een rustige plek waar ik geconcentreerd kan werken',False,'Weinig prikkels, vaste taken')+opt('Een levendig team waar veel gebeurt',True,'Veel contact, afwisseling')+opt('Een open plek met ruimte en vrijheid',False,'Zelf indelen, vaak buiten of onderweg')+'</div><p class="qlabel">Wat voor leidinggevende past bij jou?</p><div class="chips"><span class="chip">Die me vrij laat</span><span class="chip is-active">Die meedenkt</span><span class="chip">Die duidelijk zegt wat er moet</span></div>',
 'diepte-thuis.jpg','Drie leefplekken naast elkaar: een beschutte grot, een druk rif en een open zeegrasveld. Elke plek is een ander soort werkplek.')
ob_page(14,'diepte-waarden',3,'Test 4 · Wat vind je belangrijk?','test 4 van 4',9,'rusten','diepte-waarden','',
 'Wat vind je belangrijk?','What matters to you?','Kies er drie. Wat moet je werk je echt geven?','Choose three. What should your work really give you?',
 'Laatste test. Kies wat je echt belangrijk vindt, niet wat hoort.','Last test. Choose what really matters to you, not what you think you should.',
 '<div class="opts two">'+opt('Zekerheid en een vast inkomen',True,sq=True)+opt('Iets betekenen voor anderen',True,sq=True)+opt('Fijne collega\'s',True,sq=True)+opt('Blijven leren',sq=True)+opt('Vrijheid in mijn tijd',sq=True)+opt('Dicht bij huis',sq=True)+'</div><p class="muted small" style="margin-top:12px">3 van 3 gekozen</p>',
 'diepte-waarden.jpg','Oesters met parels, een anker en een kompas op de zeebodem: wat waardevol is, wat je houvast geeft en waar je op koerst.',next_label='Naar je Kompas')

# ================= 15 kompas =================
c='15'
T('Jouw Kompas','Your Compass','titel',c)
tests=[('Hoe werk jij?','Je plant graag vooruit en werkt fijn samen.',['Planner','Teamspeler','Zorgvuldig']),('Wat vind je leuk?','Je helpt graag en maakt dingen in orde.',['Helpen','Regelen','Met je handen']),('Waar voel je je thuis?','In een levendig team met een leidinggevende die meedenkt.',['Levendig','Samen','Meedenken']),('Wat vind je belangrijk?','Zekerheid, betekenis en fijne collega\'s.',['Zekerheid','Betekenis','Collega\'s'])]
tc=''.join(f'<div class="card"><h3>{a}</h3><p class="muted small">{b}</p>'+''.join(f'<span class="word">{w}</span>' for w in ws)+'</div>' for a,b,ws in tests)
dirs=[('Zorg & welzijn','Mensen helpen en dingen goed regelen komen hier samen.',True),('Groene techniek','Met je handen werken aan iets dat ertoe doet.',True),('Organiseren & samenwerken','Plannen, ordenen en een team draaiend houden.',False)]
dc=''.join(f'<div class="card"><div class="between" style="margin-bottom:6px"><h3 style="margin:0">{a}</h3>{"<span class=\"vv\">Veel vraag in jouw regio</span>" if v else ""}</div><p class="muted small" style="margin:0 0 12px">{b}</p><a class="link" href="#">Bekijk vacatures</a></div>' for a,b,v in dirs)
body=header('Reis')+strip('licht-kompas')+f'''<main class="wrap"><div class="kg"><div>
<p class="zonetag"><b>Licht</b>Jouw Kompas · onboarding en tests afgerond</p><h1 class="k">Jouw Kompas</h1>
<p class="lead">Wat je vier tests samen laten zien. Geen cijfers, wel woorden die bij je passen.</p>
<div class="g2">{tc}</div>
<h2 class="sec-t" style="margin-top:26px">Richtingen die bij je passen</h2><div class="g3">{dc}</div>
<div class="card" style="margin-top:16px"><h3>Je droom: Coördinator in de zorg</h3><p class="muted small" style="margin:0 0 10px">Je Kompas wijst dezelfde kant op. In Carrière zie je welke stappen je kunt zetten.</p><a class="btn btn--sm" href="#">Naar Carrière</a></div>
<div class="row" style="margin-top:22px"><a class="btn btn--primary" href="#">Bekijk je paspoort</a><a class="btn" href="#">Naar vacatures</a></div></div>
<div class="gside">{reiskaart(4,10,'Licht · Jouw Kompas','reis afgerond',c,('Kijk hoe ver je gekomen bent. Dit is je nieuwe schild.','Look how far you have come. This is your new shell.'),new=True)}</div></div></main>'''
add(15,'licht-kompas','Resultaat','Licht · Jouw Kompas','Het resultaat van de reis in woorden, met richtingen en "Veel vraag in jouw regio". Je staat in het gouden licht bij de vuurtoren, in je nieuwe schild.',body,illus='licht-kompas.jpg',why='Een vuurtoren met lichtbundel over het water: aankomen bij het licht, richting zien. Het kompas hoort bij de titel Jouw Kompas.',
 css='.kg{display:grid;grid-template-columns:1fr 250px;gap:36px;padding:14px 0 30px}@media(max-width:900px){.kg{grid-template-columns:1fr;gap:12px}.gside{order:-1}}')

# ================= 16 paspoort =================
c='16'
T('Je paspoort','Your passport','titel',c)
body=header('Paspoort')+strip('licht-paspoort')+f'''<main class="wrap"><div class="kg"><div>
<p class="zonetag"><b>Licht</b>Paspoort · klaar om te delen</p><h1 class="k">Je paspoort</h1><p class="lead">Dit is wie je bent, in je eigen woorden. Jij bepaalt wat werkgevers zien.</p>
<div class="pp"><div class="pph"><span>LOBSY PASPOORT</span><span>Bijgewerkt 25 oktober 2026</span></div>
<div class="ppb"><div class="ppimg"><div class="seal"><img src="lobsy-merkteken.svg" alt=""><b>RV</b><span>Delft · 2026</span></div></div><div>
<h2 class="sec-t" style="margin:0 0 4px">Roland van der Velde</h2><p class="muted small" style="margin:0 0 14px">Delft · fiets en ov, tot 30 minuten · 16 tot 32 uur</p>
<dl class="kv"><dt>Werkervaring</dt><dd>Magazijnmedewerker, vrijwilliger voedselbank</dd><dt>Opleiding</dt><dd>Vmbo, heftruckcertificaat, EHBO</dd><dt>Droom</dt><dd>Coördinator in de zorg</dd><dt>Beschikbaar</dt><dd>Zo snel mogelijk, overdag en weekend</dd></dl>
</div></div>
<div class="ppw"><div><b>Zo werk ik</b><br><span class="word">Planner</span><span class="word">Teamspeler</span><span class="word">Zorgvuldig</span></div><div><b>Dit vind ik leuk</b><br><span class="word">Helpen</span><span class="word">Regelen</span><span class="word">Met je handen</span></div><div><b>Hier voel ik me thuis</b><br><span class="word">Levendig team</span><span class="word">Meedenkende leiding</span></div><div><b>Dit vind ik belangrijk</b><br><span class="word">Zekerheid</span><span class="word">Betekenis</span><span class="word">Collega's</span></div></div></div>
<div class="card" style="margin-top:16px"><div class="toggle"><div><b>Zichtbaar voor werkgevers</b><span class="d">Werkgevers met een passende vacature kunnen je paspoort zien.</span></div><span class="sw on"></span></div><div class="toggle"><div><b>Naam pas tonen na een match</b><span class="d">Tot die tijd zien werkgevers alleen je voornaam.</span></div><span class="sw on"></span></div></div>
<div class="row" style="margin-top:18px"><a class="btn btn--primary" href="#">Paspoort aanpassen</a><a class="btn" href="#">Download als pdf</a></div></div>
<div class="gside">{reiskaart(4,10,'Licht · Paspoort','klaar om te delen',c,('Je paspoort is klaar. Werkgevers zien wie je bent, niet alleen je cv.','Your passport is ready. Employers see who you are, not just your CV.'),new=True)}</div></div></main>'''
add(16,'paspoort','Resultaat','Licht · Paspoort','Het paspoort: wie je bent in woorden, en jij bepaalt de zichtbaarheid.',body,illus='licht-paspoort',why='Een leren document met stempel op een tafel aan de haven: een paspoort voor je volgende reis. Als kaartbeeld in het paspoort, omdat de tafel vooraan staat. De strip blijft het licht van de vuurtoren: je bent nog in de zone Licht.',
 css='.kg{display:grid;grid-template-columns:1fr 250px;gap:36px;padding:14px 0 30px}.pp{background:#fff;border:1px solid var(--line);border-radius:18px;overflow:hidden}.pph{background:var(--brand);color:#fff;font-size:.74rem;letter-spacing:.08em;padding:10px 18px;display:flex;justify-content:space-between}.ppb{display:grid;grid-template-columns:260px 1fr;gap:22px;padding:20px}.seal{aspect-ratio:4/3;border-radius:12px;background:var(--sand);border:1px dashed #e6d7bf;display:grid;place-content:center;justify-items:center;gap:6px}.seal img{width:54px;height:54px}.seal b{font-family:Fraunces,Georgia,serif;font-size:1.6rem;color:var(--text)}.seal span{font-size:.78rem;color:var(--muted)}.ppw{display:grid;grid-template-columns:repeat(4,1fr);gap:14px;padding:16px 20px;border-top:1px solid var(--line);background:var(--sand);font-size:.88rem}.ppw b{display:block;margin-bottom:6px}@media(max-width:900px){.kg{grid-template-columns:1fr;gap:12px}.gside{order:-1}.ppb{grid-template-columns:1fr}.ppw{grid-template-columns:1fr 1fr}.pph span:last-child{display:none}}')


# ================= bestaande pagina's (goedgekeurde structuur) =================
BEST='bestaand'
PROJ={'kmn':('/workspace/kandidaat-mockups-nieuw/_bron-html',None),'ndm':('/workspace/na-de-match-r2/src',None),'vv':('/workspace/veel-vraag-mockup/src',None),'bk':('/workspace/bedrijvenkaart-r21/src',None),'bnk':('/workspace/banenkaart-lagen/src',None)}
IGN=shutil.ignore_patterns('node_modules','*.py','*.mjs','package*.json','__pycache__','*.png')
if os.path.exists(BEST): shutil.rmtree(BEST)
for k,(p,_) in PROJ.items(): shutil.copytree(p,f'{BEST}/{k}',ignore=IGN)
_pj=f'{BEST}/bk/parts.js'; _t=open(_pj).read(); open(_pj,'w').write(_t.replace('src="logo.png"','src="../../lobsy-merkteken.svg"').replace('Sanne de Vries','Roland van der Velde').replace('>SV<','>RV<'))
# kmn: nieuwe illustraties onder de oude bestandsnamen, zodat ook kaartjes in de pagina het nieuwe beeld tonen
for old,new in [('hero-mijn-lobsy','mijn-lobsy'),('hero-profiel','profiel'),('hero-sollicitaties','sollicitaties'),('hero-adviseur','adviseur'),('hero-instellingen','instellingen'),('hero-wat-drijft-jou','wat-drijft'),('hero-ondernemerscheck','ondernemer')]:
    shutil.copy(f'../illustraties/{new}.jpg',f'{BEST}/kmn/img/{old}.jpg')
for f in ['lobsy-logo-horizontaal.svg']:
    shutil.copy(f,f'{BEST}/kmn/img/logo.svg')
def inject(proj,fname,num,slug,chapter,title,caption,stripname,coachtxt=None,illus='',why='',wait=0,heroname=None,askbtn=True):
    p=f'{BEST}/{proj}/{fname}'; s=open(p).read()
    rel='../../'
    st=strip(stripname,rel) if stripname else ''
    n0=len(s)
    c1=0
    if stripname:
        s,c1=re.subn(r'<div class="hero"[^>]*></div>',st,s,count=1)
        if not c1: s,c1=re.subn(r'<div class="rustig-hero"[^>]*>.*?</div>',st,s,count=1,flags=re.S)
    s=s.replace('</head>',f'<link rel="stylesheet" href="{rel}kreeft-inject.css"></head>',1)
    a=ask(rel)
    s,c2=re.subn(r'<div class="chatdock">.*?</div>','',s,flags=re.S)
    if askbtn: s=s.replace('</body>',a+'</body>',1)
    if coachtxt:
        cn=coach(coachtxt[0],coachtxt[1],f'{num:02d}',rel)
        s,c3=re.subn(r'(</h1>\s*(<p[^>]*>.*?</p>)?)',lambda m:m.group(1)+cn,s,count=1,flags=re.S)
    for a_,b_ in [('Maak je reis af: Naar het licht','Je Kompas en paspoort zijn klaar'),('Nog één test, dan is je paspoort helemaal compleet. Duurt ongeveer 5 minuten.','Kijk rustig wat werkgevers van je zien. Jij bepaalt wat je deelt.'),('Je paspoort is bijna compleet.','Je paspoort is compleet.')]: s=s.replace(a_,b_)
    s=s.replace('Sam Voorbeeld','Roland van der Velde').replace('src="logo.png"','src="../../lobsy-merkteken.svg"')
    # geen scores of percentages; kansberoep heet 'Veel vraag in jouw regio'
    s=re.sub(r'(<td data-l="Match met vacature">)\d+%(</td>)',r'\1Past goed\2',s)
    for a_,b_ in [('Match met vacature','Past bij jou'),('Kansberoepen en omscholingsideeën','Beroepen met veel vraag in jouw regio en omscholingsideeën'),('Kansberoep (Haaglanden)','Veel vraag in jouw regio'),('>Veel kans op werk in de regio<','>Ja<'),('>Geen kansberoep<','>Nee<'),
      ('Past bij jou: hoe goed de vacature past bij je reistijd, uren en dagdelen. Kansberoep: hoeveel vraag er in de regio Haaglanden is naar dit beroep, volgens open cijfers van CBS en de UWV-kansberoepen. ‘Geen kansberoep’: het beroep staat niet op de lijst kansberoepen.','Past bij jou: de vacature past bij je reistijd, uren en dagdelen. Veel vraag in jouw regio: werkgevers in Haaglanden zoeken veel mensen voor dit beroep, volgens open gegevens van CBS en UWV.')]: s=s.replace(a_,b_)
    s=s.replace('Vraag het Lobsy, of','Vraag het via Lobsy-hulp, of')
    s=s.replace('Sanne de Vries','Roland van der Velde').replace('>SV<','>RV<').replace('Sanne','Roland')
    newf=f'{BEST}/{proj}/{num:02d}-{slug}.html'
    s=s.replace('node_modules/maplibre-gl/dist/','../../vendor/maplibre/')
    open(newf,'w').write(s)
    add(num,slug,chapter,title,caption,file=newf,illus=illus,why=why,kind='bestaand',wait=wait,src=fname)
    return c1

# 17 Mijn Lobsy
inject('kmn','mijn-lobsy.html',17,'mijn-lobsy','Mijn Lobsy','Mijn Lobsy','Thuisbasis na de reis. Je loopt in de haven naar je havenkantoortje, de rest is rustig overzicht.','mijn-lobsy',
 ('Welkom terug. Kijk rustig wat er voor je klaarstaat.','Welcome back. Take your time to see what is waiting for you.'),'mijn-lobsy.jpg','Een havenkantoortje aan de promenade met boten: je thuishaven, het vaste punt waar je steeds terugkomt.')

# 18 Match / vacature (eigen opbouw, structuur zoals match-mockup in PR 878)
c='18'
T('Medewerker thuiszorg','Home care worker','titel',c)
body=header('Zoeken')+strip('match')+f'''<main class="wrap" style="padding-bottom:30px">
<nav class="small muted" style="margin:14px 0 6px">Zoeken › Jouw matches › Medewerker thuiszorg</nav>
<div class="grid-side"><div class="stack">
<section class="card"><div class="row" style="margin-bottom:10px"><span class="logo-sq">ZD</span><div><p class="eyebrow" style="margin:0">Zorggroep Duinzicht · Delft</p><h1 class="k" style="margin:0">Medewerker thuiszorg</h1></div></div>
<div class="row" style="margin-bottom:14px"><span class="vv">Veel vraag in jouw regio</span><span class="pill">24 tot 32 uur</span><span class="pill">€ 2.600 tot € 3.100 per maand</span><span class="pill">Fiets 14 min</span></div>
<p style="margin:0">Je bezoekt cliënten thuis in Delft en helpt ze met de dingen die even niet zelf lukken. Je werkt in een vast wijkteam en plant je route samen met je collega's.</p></section>
<section class="card"><h2 class="sec-t">Waarom dit bij je past</h2>
<div class="opts"><div class="opt on sq"><i></i><div>Je helpt graag mensen en regelt dingen goed<small>Uit je test Wat vind je leuk?</small></div></div><div class="opt on sq"><i></i><div>Een levendig team met een leidinggevende die meedenkt<small>Uit je test Waar voel je je thuis?</small></div></div><div class="opt on sq"><i></i><div>Dicht bij je droom: coördinator in de zorg<small>Na een jaar kun je doorgroeien naar planner</small></div></div></div></section>
<section class="card"><h2 class="sec-t">Wat je nodig hebt</h2><p class="muted" style="margin:0">Geen diploma nodig om te starten. Je volgt een betaalde opleiding tot helpende zorg en welzijn tijdens het werk. Een rijbewijs is niet nodig, je werkt op de fiets.</p></section>
</div><aside class="stack">
<div class="card"><a class="btn btn--primary" style="width:100%" href="#">Solliciteer met je paspoort</a><a class="btn" style="width:100%;margin-top:8px" href="#">Bewaren</a><p class="muted small" style="margin:10px 0 0">De werkgever ziet je paspoort. Je naam pas na een match.</p></div>
{coach('Deze vacature past bij wat je in je reis vertelde. Lees rustig verder.','This vacancy fits what you shared on your journey. Read on at your own pace.',c)}
<div class="card"><h3>Over Zorggroep Duinzicht</h3><p class="muted small" style="margin:0">Thuiszorg in Delft en Rijswijk. 140 collega's in zes wijkteams.</p></div></aside></div></main>'''
add(18,'match-vacature','Match en vacatures','Match: vacature','Een passende vacature, met in woorden waarom hij past. Geen scores.',body,illus='match.jpg',why='Een zeilbootje dat precies in een passende ligplaats aan de steiger aanlegt: een match die klopt.',css='.grid-side{margin-top:6px}@media(max-width:900px){.grid-side .row .pill{font-size:.72rem}}')

# 19 Banenkaart
inject('bnk','index.html',19,'banenkaart','Banenkaart','Banenkaart','De kaart met werk om je heen. Functioneel en rustig: boven de kaart lees je zelf de kaart, onderaan de knop Lobsy-hulp.','banenkaart',None,'banenkaart.jpg','De polder met dorp, kassen, loods, zorgcentrum en boerderijen, gezien als een kaart: werk om je heen, op afstand die je kunt fietsen.',wait=1)
# 20 Bedrijvenkaart
pj=open(f'{BEST}/bk/parts.js').read()
pj=re.sub(r'function hero\(m\)\{return `.*?`\}','function hero(m){return `'+strip('bedrijvenkaart','../../').replace('`','')+'`}',pj,flags=re.S)
open(f'{BEST}/bk/parts.js','w').write(pj)
inject('bk','kaart.html',20,'bedrijvenkaart','Bedrijvenkaart','Bedrijvenkaart','Bedrijven bij jou in de buurt. In de strip loop je door de winkelstraat; op de kaart zelf staat geen kreeft.',None,None,'bedrijvenkaart.jpg','Een winkelstraat met bakkerij, fietsenmaker, garage, bloemist, praktijk en werkplaats: bedrijven in je buurt waar je kunt werken.',wait=1)

# 21 Bewaard
c='21'
T('Bewaard','Saved','titel',c)
items=[('ZD','Medewerker thuiszorg','Zorggroep Duinzicht · Delft','Vacature',True),('GW','Monteur zonnepanelen','GroenWatt Installaties · Rijswijk','Vacature',True),('BK','Bakkerij De Korenaar','Bedrijf · Delft · 3 vacatures','Bedrijf',False),('MB','Opleiding helpende zorg en welzijn','MBO Haaglanden · deeltijd','Opleiding',False)]
li=''.join(f'<div class="card bw"><span class="logo-sq">{a}</span><div><b>{t}</b><p class="muted small" style="margin:2px 0 0">{s}</p></div><div class="row">{"<span class=\"vv\">Veel vraag in jouw regio</span>" if v else ""}<span class="pill">{k}</span><a class="btn btn--sm" href="#">Bekijk</a></div></div>' for a,t,s,k,v in items)
body=header('Zoeken')+strip('bewaard')+f'''<main class="wrap" style="padding-bottom:30px"><div class="head"><h1 class="k">Bewaard</h1><p class="lead" style="margin:0">Wat je wilde onthouden, op één plek.</p></div>
<div class="tabs" style="margin-bottom:16px"><a class="is-active" href="#">Alles (4)</a><a href="#">Vacatures</a><a href="#">Bedrijven</a><a href="#">Opleidingen</a></div><div class="stack">{li}</div></main>'''
add(21,'bewaard','Bewaard','Bewaard','Je bewaarde vacatures, bedrijven en opleidingen. Klein in de strip, je loopt langs je plank met schelpen.',body,illus='bewaard.jpg',why='Een plank in een strandhuisje met verzamelde schelpen met labeltjes en een pot zeeglas: dingen die je bewaart om later terug te zien.',
 css='.bw{display:grid;grid-template-columns:auto 1fr auto;gap:14px;align-items:center}@media(max-width:900px){.bw{grid-template-columns:auto 1fr}.bw .row{grid-column:1/-1}}')
# 22 Bewaard leeg
c='22'
T('Nog niets bewaard','Nothing saved yet','titel',c)
body=header('Zoeken')+strip('bewaard')+f'''<main class="wrap"><div class="head"><h1 class="k">Bewaard</h1></div>
<div class="empty"><img class="v" src="{IL}leeg-bewaard.jpg" alt=""><div><h2>Nog niets bewaard</h2><p class="muted">Zie je een vacature, bedrijf of opleiding die je wilt onthouden? Tik op Bewaren. Dan vind je hem hier terug.</p>
{coach('Neem je tijd. Wat je mooi vindt, bewaar je hier.','Take your time. What you like, you keep here.',c)}<a class="btn btn--primary" href="#">Bekijk je matches</a></div></div></main>'''
add(22,'bewaard-leeg','Bewaard','Bewaard · leeg','Lege staat: nog niets bewaard.',body,illus='leeg-bewaard.jpg',why='Een leeg bewaardoosje met één schelp: er is nog ruimte, en het eerste dat je vindt krijgt een plek.')

# 23-27 Sollicitaties en UWV
inject('kmn','sollicitaties.html',23,'sollicitaties','Sollicitaties','Sollicitaties','Je sollicitaties in één lijst met status. In de strip loop je met je brieven naar het postkantoor.','sollicitaties',None,'sollicitaties.jpg','Een postkantoortje aan de kade met een postboot vol enveloppen en een pad naar de bedrijven: je brieven zijn onderweg.')
c='24'
T('Nog geen sollicitaties','No applications yet','titel',c)
body=header('Sollicitaties')+strip('sollicitaties')+f'''<main class="wrap"><div class="head"><h1 class="k">Mijn sollicitaties</h1></div>
<div class="tabs" style="margin-bottom:16px"><a class="is-active" href="#">Sollicitaties</a><a href="#">Mijn activiteiten</a></div>
<div class="empty"><img class="v" src="{IL}leeg-sollicitaties.jpg" alt=""><div><h2>Nog geen sollicitaties</h2><p class="muted">Als je solliciteert via Lobsy, zie je hier hoe het ervoor staat. Solliciteerde je ergens anders? Die kun je ook toevoegen.</p>
{coach('Dat komt nog. Begin bij een vacature die je aanspreekt.','That will come. Start with a vacancy that appeals to you.',c)}<div class="row"><a class="btn btn--primary" href="#">Bekijk je matches</a><a class="btn" href="#">Sollicitatie toevoegen</a></div></div></div></main>'''
add(24,'sollicitaties-leeg','Sollicitaties','Sollicitaties · leeg','Lege staat: nog geen sollicitaties.',body,illus='leeg-sollicitaties.jpg',why='Een brievenbus op de steiger zonder post: nog niets verstuurd, maar de bus staat klaar.')
inject('kmn','sollicitaties-toevoegen.html',25,'sollicitatie-toevoegen','Sollicitaties','Sollicitatie toevoegen','Zelf een sollicitatie toevoegen die je buiten Lobsy deed.','toevoegen',None,'toevoegen.jpg','Een stapel brieven op een tafel aan de kade waar net een envelop bovenop is gelegd: een sollicitatie toevoegen aan je stapel.')
inject('kmn','sollicitaties-activiteiten.html',26,'mijn-activiteiten','UWV: Mijn activiteiten','Mijn activiteiten (UWV)','Je activiteiten per week bijhouden voor het UWV.','activiteiten',
 ('Je hoeft niets te onthouden. Je activiteiten staan per week voor je klaar.','You do not have to remember anything. Your activities are ready for you per week.'),'activiteiten.jpg','Een havenmeestersbureau met een open logboek per week, kalender en stempel: netjes bijhouden wat je deed.')
inject('kmn','sollicitaties-pdf.html',27,'uwv-overzicht','UWV: Mijn activiteiten','UWV-overzicht (pdf)','Het overzicht dat je naar het UWV stuurt. Een formeel document, dus zonder illustratie en in de goedgekeurde bewoording.',None,None,'geen (document)','Dit is een formeel pdf-document voor het UWV. Bewust zonder illustratie of kreeft, zodat het zakelijk en leesbaar blijft.',askbtn=False)

# 28-31 Na de match
N='_na-de-match__'
inject('ndm',N+'01-kandidaat-wat-nu.html',28,'na-de-match-wat-nu','Na de match','Na de match: wat nu','De werkgever zei ja. Je loopt naar het tafeltje voor twee en leest rustig wat er nu gebeurt.','na-de-match',
 ('Mooi nieuws. Hieronder lees je stap voor stap wat er nu gebeurt.','Good news. Below you read step by step what happens now.'),'na-de-match.jpg','Een haventerras met een tafeltje voor twee, twee kopjes en een klokje: klaar voor het eerste gesprek.')
inject('ndm',N+'04-kandidaat-moment-kiezen.html',29,'na-de-match-moment','Na de match','Na de match: moment kiezen','Een moment kiezen voor het gesprek.','moment',
 ('Kies een moment dat bij je past. Verzetten kan altijd.','Choose a moment that suits you. You can always reschedule.'),'moment.jpg','Een klokkentorentje aan de kade, een getijdenbord met lege kaartjes en een bankje: een moment kiezen dat past.')
inject('ndm',N+'10-kandidaat-berichten.html',30,'berichten','Na de match','Berichten','Berichten met de werkgever.','berichten',None,'berichten.jpg','Twee vuurtorens aan weerszijden van de haveningang die elkaar zacht lichtsignalen geven: berichten heen en weer.')
inject('ndm',N+'13-kandidaat-nu-niet.html',31,'na-de-match-nu-niet','Na de match','Na de match: nu niet','Het werd dit keer niets. Rustig en zonder oordeel.','nu-niet',
 ('Dit keer niet. Dat zegt niets over wie je bent. Morgen kijk je verder.','Not this time. It says nothing about who you are. Tomorrow you look further.'),'nu-niet.jpg','Een haven bij avondlicht waar bootjes veilig terugkomen en de vuurtoren aangaat: even rusten, morgen weer uitvaren.')

# 32-33 Carrière en omscholen
def vv_inject(fname,num,slug,title,caption,name,coachtxt,why):
    p=f'{BEST}/vv/{fname}'; s=open(p).read()
    s=re.sub(r'<img class="desk" src="img/hero-[a-z]+-desk.jpg" alt="">',f'<img class="desk" src="../../strips/{name}-desk.jpg" alt="">',s)
    s=re.sub(r'<img class="mob" src="img/hero-[a-z]+-mob.jpg" alt="">',f'<img class="mob" src="../../strips/{name}-mob.jpg" alt="">',s)
    open(p,'w').write(s)
    inject('vv',fname,num,slug,'Carrière en omscholen',title,caption,None,coachtxt,name+'.jpg',why)
    # strip-hoogte gelijk trekken
    f=f'{BEST}/vv/{num:02d}-{slug}.html'; s=open(f).read()
    m=META[name]; s=s.replace('</head>',f'<style>:root{{--hero-d:{m["dh"]}px;--hero-m:{m["mh"]}px}}.rustig-hero,.hero{{height:{m["dh"]}px}}@media(max-width:900px){{.rustig-hero,.hero{{height:{m["mh"]}px}}}}</style></head>',1); open(f,'w').write(s)
vv_inject('04-carriere.html',32,'carriere','Carrière','Je route naar je droom, met "Veel vraag in jouw regio".','carriere',
 ('Je droom is een richting. Je zet er kleine stappen naartoe.','Your dream is a direction. You take small steps toward it.'),'Een kustpad dat langs uitkijkpunten omhoog slingert naar de vuurtoren, met wegwijzers bij elke splitsing: je carrièreroute, stap voor stap.')
vv_inject('05-omscholen.html',33,'omscholen','Omscholen','Omscholen naar een beroep met veel vraag.','omscholen',
 ('Wat je al kunt neem je mee. Je begint niet opnieuw.','You take what you can already do with you. You are not starting over.'),'Een botenwerf waar een oude boot een nieuw zeil en verf krijgt en een andere al uitvaart: dezelfde boot, een nieuwe koers.')

# 34-38 Jezelf en account
inject('kmn','wat-drijft-jou.html',34,'wat-drijft-jou','Wat drijft jou','Wat drijft jou','Wat geeft je energie? Een rustige verdieping.','wat-drijft',
 ('Wat geeft je energie? Daar zit vaak je richting.','What gives you energy? That is often where your direction is.'),'wat-drijft.jpg','Een zeilbootje met de wind in het zeil en een windzak op de steiger: wat jou drijft is je eigen wind.')
inject('kmn','ondernemerscheck.html',35,'ondernemerscheck','Ondernemerscheck','Ondernemerscheck','Past ondernemen bij jou? Een korte check.','ondernemer',
 ('Zelf iets beginnen? Met deze check zie je of het bij je past.','Starting something yourself? This check shows you whether it suits you.'),'ondernemer.jpg','Een kraampje en werkplaatsje aan de kade dat voor het eerst opengaat, met een bezorgfiets: je eigen zaak beginnen.')
inject('kmn','adviseur.html',36,'adviseur','Adviseur','Adviseur','Wat je meeneemt naar je adviseur werk.','adviseur',
 ('Soms helpt een mens om mee te praten. Je adviseur denkt met je mee.','Sometimes it helps to talk to a person. Your advisor thinks along with you.'),'adviseur.jpg','Twee fauteuils naar elkaar toe bij een patrijspoort met thee: een persoonlijk gesprek met je adviseur.')
inject('kmn','profiel.html',37,'profiel','Profiel en instellingen','Profiel','Je persoonlijke gegevens. Functioneel; in de strip loop je door je eigen gang naar de deur.','profiel',None,'profiel.jpg','Een opgeruimde gang met jas, sjaal, laarzen, spiegel en een deur met naambordje: jouw eigen gegevens, netjes op hun plek.')
inject('kmn','instellingen.html',38,'instellingen','Profiel en instellingen','Instellingen','Meldingen, privacy en taal. Functioneel; in de strip stel je zelf de wijzers af.','instellingen',None,'instellingen.jpg','Een werkplaatsje in de vuurtoren met wijzers, hendels en laatjes: alles afstellen zoals jij het wilt.')

# 39 Geen matches
c='39'
T('Nog geen matches','No matches yet','titel',c)
body=header('Zoeken')+strip('match')+f'''<main class="wrap"><div class="head"><h1 class="k">Jouw matches</h1></div>
<div class="empty"><img class="v" src="{IL}leeg-matches.jpg" alt=""><div><h2>Nog geen matches</h2><p class="muted">We vinden nog geen vacatures die goed bij je passen. Dat kan komen doordat je reis nog niet af is, of doordat je reisafstand klein is.</p>
{coach('Maak je reis af, dan zie je meer vacatures. Het licht staat al aan.','Finish your journey and you will see more vacancies. The light is already on.',c)}<div class="row"><a class="btn btn--primary" href="#">Maak je reis af</a><a class="btn" href="#">Reisafstand aanpassen</a></div></div></div></main>'''
add(39,'matches-leeg','Lege staten en hulp','Geen matches','Lege staat: nog geen matches.',body,illus='leeg-matches.jpg',why='Een vuurtoren in de verte en een lantaarnpaal die al brandt: er is nog niets binnen, maar het licht staat aan en er wordt gezocht.')

# 40 Lobsy-hulp (open paneel)
c='40'
T('Lobsy-hulp','Lobsy help','titel',c)
T('Hulp bij de app en je reis','Help with the app and your journey','paneel',c)
T('Hoi Roland. Waar wil je hulp bij?','Hi Roland. What would you like help with?','hulp',c)
T('Dat betekent dat werkgevers bij jou in de buurt veel mensen zoeken voor dit werk. Je maakt daar vaak sneller kans op een gesprek.','It means employers near you are looking for many people for this work. You often get an interview sooner there.','hulp',c)
T('Lobsy-hulp is een digitale hulp. Voor persoonlijk advies kun je altijd je adviseur vragen.','Lobsy help is a digital helper. For personal advice you can always ask your advisor.','paneel',c)
bg=open('18-match-vacature.html').read()
mainpart=re.search(r'<main.*?</main>',bg,re.S).group(0)
body=header('Zoeken')+strip('match')+mainpart+f'''<div class="dim"></div><aside class="kpanel" aria-label="Lobsy-hulp">
<div class="kph"><img src="lobsy-merkteken.svg" alt=""><div><b>Lobsy-hulp</b><span>Hulp bij de app en je reis</span></div><span class="x">×</span></div>
<div class="kpb"><div class="msg l">Hoi Roland. Waar wil je hulp bij?</div>
<div class="chips" style="margin:4px 0 14px"><span class="chip">Hoe werkt solliciteren?</span><span class="chip">Wat ziet een werkgever?</span><span class="chip">Ik wil even pauzeren</span></div>
<div class="msg r">Wat betekent Veel vraag in jouw regio?</div>
<div class="msg l">Dat betekent dat werkgevers bij jou in de buurt veel mensen zoeken voor dit werk. Je maakt daar vaak sneller kans op een gesprek.</div>
<div class="msg l">Wil je de vacatures met veel vraag bij jou in de buurt zien?<div class="row" style="margin-top:8px"><a class="btn btn--sm btn--primary" href="#">Ja, laat zien</a><a class="btn btn--sm" href="#">Nee, dank je</a></div></div></div>
<div class="kpf"><div class="input" style="flex:1;color:var(--muted)">Typ je vraag</div><a class="btn btn--primary btn--sm" href="#">Stuur</a></div>
<p class="kpn">Lobsy-hulp is een digitale hulp. Voor persoonlijk advies kun je altijd je adviseur vragen.</p></aside>'''
add(40,'lobsy-hulp','Lege staten en hulp','Lobsy-hulp (paneel open)','De knop Lobsy-hulp staat op elke pagina. Zo ziet het geopende paneel eruit, hier boven de vacature. Neutraal, met het merkteken: de kreeft ben jij, niet de helper.',body,illus='lobsy-merkteken.svg',askbtn=False,
 css='.dim{position:fixed;inset:0;background:rgba(15,45,92,.18);z-index:50}.kpanel{position:fixed;top:0;right:0;bottom:0;width:420px;background:var(--bg);border-left:1px solid var(--line);z-index:70;display:flex;flex-direction:column;box-shadow:-12px 0 32px rgba(15,45,92,.12)}.kph{display:flex;gap:12px;align-items:center;padding:16px 18px;border-bottom:1px solid var(--line);background:var(--cream)}.kph img{width:44px;height:44px;border-radius:12px;background:#fff;padding:6px;border:1px solid #f0e2cc}.kph b{display:block;font-family:Fraunces,Georgia,serif;font-size:1.15rem}.kph span{font-size:.82rem;color:var(--muted)}.kph .x{margin-left:auto;font-size:1.6rem;color:var(--muted)}.kpb{flex:1;padding:18px;overflow:auto}.msg{max-width:88%;padding:11px 14px;border-radius:16px;margin-bottom:10px;font-size:.93rem;line-height:1.45}.msg.l{background:#fff;border:1px solid var(--line);border-bottom-left-radius:4px}.msg.r{background:var(--brand);color:#fff;margin-left:auto;border-bottom-right-radius:4px}.kpf{display:flex;gap:8px;align-items:center;padding:12px 18px;border-top:1px solid var(--line)}.kpn{margin:0;padding:0 18px 14px;font-size:.76rem;color:var(--muted)}@media(max-width:900px){.kpanel{top:auto;left:0;width:auto;height:82vh;border-left:0;border-top:1px solid var(--line);border-radius:20px 20px 0 0}}')

# 41 404
c='41'
T('Deze pagina bestaat niet (meer)','This page does not exist (anymore)','titel',c)
body=header('')+f'''<main class="wrap"><div class="err"><div class="errimg"><img class="v" src="{IL}fout-404.jpg" alt=""><img class="kr" src="{IL}kreeft-kaart.webp" alt="De kreeft kijkt op de kaart"></div>
<div><p class="eyebrow">Foutcode 404</p><h1 class="k">Deze pagina bestaat niet (meer)</h1><p class="lead">Misschien is de link oud, of is er een typfout in het adres geslopen.</p>
{coach('Deze pagina is er niet. Met de knoppen hieronder vind je de weg terug.','This page is not here. The buttons below help you find your way back.',c)}
<div class="row"><a class="btn btn--primary" href="#">Naar Mijn Lobsy</a><a class="btn" href="#">Naar je reis</a></div></div></div></main>'''
ERRCSS='.err{display:grid;grid-template-columns:520px 1fr;gap:44px;align-items:center;padding:48px 0}.errimg{position:relative}.errimg .v{width:100%;border-radius:20px;display:block}.errimg .kr{position:absolute;right:-30px;bottom:-14px;width:150px;height:auto}@media(max-width:900px){.err{grid-template-columns:1fr;gap:26px;padding:20px 0}.errimg .kr{width:100px;right:-6px;bottom:-16px}}'
add(41,'404','Lege staten en hulp','404: pagina niet gevonden','Een verkeerde link. Je kijkt rustig op de kaart.',body,illus='fout-404.jpg + kreeft-kaart',why='Een duinpad met een lege wegwijzer: even niet weten waar je bent. Lobsy leest de kaart om samen de weg terug te vinden.',css=ERRCSS)
# 42 fout
c='42'
T('Er ging iets mis','Something went wrong','titel',c)
body=header('')+f'''<main class="wrap"><div class="err"><div class="errimg"><img class="v" src="{IL}fout-algemeen.jpg" alt=""><img class="kr" src="{IL}kreeft-wachten.webp" alt="De kreeft wacht rustig op een paal"></div>
<div><p class="eyebrow">Even geduld</p><h1 class="k">Er ging iets mis</h1><p class="lead">Dit ligt aan ons, niet aan jou. Je antwoorden en gegevens zijn veilig bewaard.</p>
{coach('Er wordt aan gewerkt. Probeer het zo nog eens.','It is being worked on. Please try again shortly.',c)}
<div class="row"><a class="btn btn--primary" href="#">Probeer opnieuw</a><a class="btn" href="#">Naar Mijn Lobsy</a></div><p class="muted small" style="margin-top:14px">Blijft het misgaan? Mail naar hulp@lobsy.example</p></div></div></main>'''
add(42,'fout','Lege staten en hulp','Foutpagina','Er ging iets mis aan onze kant. Rustig, met de belofte dat je gegevens veilig zijn.',body,illus='fout-algemeen.jpg + kreeft-wachten',why='Een steiger die wordt gerepareerd met een gereedschapskist: er is iets kapot, er wordt aan gewerkt. Lobsy wacht rustig.',css=ERRCSS)


# ---------------- scène per stap: wat je ziet, waar de kreeft (jij) is, waarom het past ----------------
# num: (scène, kreeft, past bij stap omdat, schild, licht)
SC={
1:('Strand bij zonsopgang, het pad loopt over de duinen naar de vuurtoren in de verte.','In profiel, nog met zeepokken op zijn schild, loopt naar rechts het duinpad op richting de vuurtoren.','moment 0: je staat aan het begin en ziet waar de reis heen gaat, nog voor je iets invult.','10 lagen, heel schild','dageraad, roze en koel'),
2:('Strandpaviljoen met open deur, lamp en kapstok.','Driekwart van achteren, op weg naar de open deur.','inloggen is binnenstappen: de deur staat open en je wordt verwacht.','10 lagen','dageraad'),
3:('De routekaart: strand, kust, rotsen, diepte en vuurtoren.','Heel klein op het strand, aan het begin van het pad, van achteren.','je ziet de hele route en jezelf op het startpunt, voordat je toestemming geeft.','10 lagen','dageraad'),
4:('Dorpsstraat aan de kust met verschillende voordeuren.','Profiel, loopt naar rechts langs de deuren.','kennismaken gaat over wie je bent en waar je woont: je loopt door je eigen straat.','laag 1 laat los','vroege ochtend'),
5:('Getijdenpaal en strandhuisjes onder een dagboog.','Profiel, loopt naar rechts langs de getijdenpaal.','de vraag gaat over tijd en ritme: eb en vloed, ochtend en avond.','laag 2 laat los','ochtend'),
6:('Duinen met fiets, bus, auto, veerboot en trein.','Profiel, loopt naar rechts over het duinpad.','de vraag gaat over hoe je ergens komt; je bent zelf onderweg tussen alle vervoer.','laag 3 laat los','ochtend'),
7:('Schuurtje met net, boot en kruiwagen tussen de rotsen.','Profiel, stapt naar rechts over de rotsen langs het schuurtje.','alle soorten werk tellen mee, ook klein en informeel; je loopt langs het gereedschap.','laag 4 laat los','helder daglicht'),
8:('Stenen schooltje in de rotsen met boeken, lei en sextant.','Klimt naar rechtsboven tegen de stapel boeken.','leren is klimmen: op school en onderweg.','laag 5 laat los','helder daglicht'),
9:('Bankje met verrekijker op een klif boven zee; boven zee stijgen zes droomwolkjes op met dezelfde kreeft als piloot in een propellervliegtuigje, schilder bij een ezel, dokter in witte jas, bakker met brood, tuinier met gieter en bouwvakker met kruiwagen.','Van achteren, naast (niet achter) de verrekijker, schaar boven de ogen, kijkt naar rechts over zee; kleine belletjes lopen van zijn hoofd naar de wolkjes.','bij de droombaan stel je je voor wat je zou kunnen worden: veel verschillende banen, van piloot en dokter tot bakker en bouwvakker, allemaal even waardevol, en jij bent het in elk wolkje.','laag 6 laat los','helder, weids'),
10:('School vissen, een paar samen, één alleen.','Zwemt de diepte in, naar rechts, schuin omlaag.','de test vraagt hoe je werkt, samen of alleen; je duikt in jezelf.','laag 7 laat los','blauw onder water'),
11:('Rif waar elk dier iets anders doet.','Zwemt naar rechts langs het rif.','interesses verschillen en zijn allemaal waardevol; je kijkt rond wat jou trekt.','laag 8 laat los','blauw'),
12:('Stille grot met een steen op de bodem.','Zit op de steen, zijaanzicht, rust uit.','halverwege de tests even op adem komen.','8 lagen los (pauze)','blauw, gedempt'),
13:('Grot, druk rif en open zeegrasveld naast elkaar.','Zwemt naar rechts tussen de leefplekken.','je kiest waar je je thuis voelt; je verkent de drie plekken.','laag 9 laat los','blauw'),
14:('Oesters met parels, anker en kompas op de zeebodem.','Duikt omlaag naar de parel, naar rechts.','je kiest wat waardevol is: de parels, het anker als houvast, het kompas als koers.','laag 10 laat los','blauw, iets lichter'),
15:('Vuurtoren met lichtbundel in gouden avondlicht.','Driekwart van achteren bij de vuurtoren, één kompas in de schaar, oude schildstukjes bij zijn poten.','je bent aangekomen bij het licht en ziet je Kompas: je bent uit je schild.','nieuw schild','goud'),
16:('Tafel aan de haven met een leren paspoort en stempel.','Profiel, loopt met zijn post naar rechts, weg van de tafel, de haven in.','je paspoort is klaar voor je volgende reis: werk zoeken in de haven.','nieuw schild','goud, warm'),
17:('Havenkantoortje aan de promenade met boten.','Profiel, loopt naar het havenkantoortje.','Mijn Lobsy is je thuishaven, waar je steeds terugkomt.','nieuw schild','haven, daglicht'),
18:('Zeilbootje dat in een passende ligplaats aanlegt.','Profiel op de steiger, loopt naar de boot.','een match is een boot die past in jouw ligplaats.','nieuw schild','haven'),
19:('Polder met dorp, kassen, loods en zorgcentrum.','Profiel, staat aan de rand en kijkt omlaag op zijn kaart.','op de banenkaart lees je zelf de kaart van werk om je heen.','nieuw schild','haven'),
20:('Winkelstraat met bakkerij, fietsenmaker, garage en bloemist.','Profiel, loopt door de straat langs de winkels.','bedrijven in je buurt: je loopt er zelf langs.','nieuw schild','haven'),
21:('Plank in een strandhuisje met verzamelde schelpen.','Heel klein op de plank, loopt langs de schelpen.','bewaren is verzamelen wat je wilt onthouden.','nieuw schild','haven'),
22:('Zelfde strip als Bewaard; leeg bewaardoosje met één schelp.','In de strip langs de plank; in de lege staat geen kreeft.','de plank staat klaar, er is nog ruimte.','nieuw schild','haven'),
23:('Postkantoortje aan de kade met een postboot vol enveloppen.','Profiel, loopt met zijn brieven naar het postkantoor.','je sollicitaties zijn brieven die onderweg zijn.','nieuw schild','haven'),
24:('Zelfde strip als Sollicitaties; brievenbus op de steiger zonder post.','In de strip op weg naar het postkantoor; in de lege staat geen kreeft.','nog niets verstuurd, maar de bus staat klaar.','nieuw schild','haven'),
25:('Stapel brieven op een tafel aan de kade.','Profiel, loopt over de tafel naar de stapel.','je legt zelf een sollicitatie bij de stapel.','nieuw schild','haven'),
26:('Havenmeestersbureau met open logboek, kalender en stempel.','Zijaanzicht, kijkt omlaag in zijn papier naast het logboek.','je houdt per week bij wat je deed, zoals een logboek.','nieuw schild','haven'),
27:('Geen (formeel document).','Geen.','dit is het formele UWV-document; zakelijk en leesbaar, in de goedgekeurde bewoording.','','geen'),
28:('Haventerras met een tafeltje voor twee en twee kopjes.','Profiel, loopt naar het tafeltje.','na de match volgt het eerste gesprek; de tafel staat klaar.','nieuw schild','haven, middag'),
29:('Klokkentorentje aan de kade en een bord met lege kaartjes.','Van achteren op de kade bij de klokkentoren, kijkt naar het bord.','je kiest een moment dat past.','nieuw schild','haven'),
30:('Twee vuurtorens aan weerszijden van de haveningang die elkaar lichtsignalen geven.','Van achteren bij de vuurtoren, kijkt naar het licht aan de overkant.','berichten gaan heen en weer, zoals lichtsignalen.','nieuw schild','haven, avond'),
31:('Haven bij avondlicht waar bootjes terugkomen.','Zit op een paal en kijkt uit over de haven.','dit keer niet: even rusten, morgen weer uitvaren.','nieuw schild','avond, zacht'),
32:('Kustpad dat langs wegwijzers omhoog slingert naar de vuurtoren.','Profiel, loopt het pad op.','je carrièreroute loop je stap voor stap.','nieuw schild','haven'),
33:('Botenwerf waar een oude boot een nieuw zeil krijgt.','Profiel, kijkt omlaag op zijn plan bij de werf.','omscholen is dezelfde boot met een nieuwe koers.','nieuw schild','haven'),
34:('Zeilbootje met wind in het zeil en een windzak.','Van achteren, kijkt naar de wind en het zeil.','wat jou drijft is je eigen wind.','nieuw schild','haven'),
35:('Kraampje aan de kade dat voor het eerst opengaat.','Profiel, loopt naar het kraampje.','je eigen zaak beginnen: je loopt er zelf naartoe.','nieuw schild','haven'),
36:('Twee fauteuils bij een patrijspoort met thee.','Zit in de fauteuil, zijaanzicht.','een persoonlijk gesprek met je adviseur; je zit er al klaar voor.','nieuw schild','haven, binnen'),
37:('Opgeruimde gang met jas, sjaal, laarzen en een deur met naambordje.','Profiel, loopt naar de deur met je naam.','je eigen gegevens, netjes op hun plek.','nieuw schild','binnen'),
38:('Werkplaatsje in de vuurtoren met wijzers en hendels.','Driekwart, staat bij het bord met wijzers.','instellingen zijn de wijzers die je zelf afstelt.','nieuw schild','binnen'),
39:('Zelfde strip als de match; vuurtoren en brandende lantaarn als lege staat.','In de strip op de steiger; in de lege staat geen kreeft.','er is nog niets binnen, maar het licht staat aan.','nieuw schild','haven'),
40:('Zelfde strip als de match; paneel met het Lobsy-merkteken.','In de strip op de steiger; de hulp heeft geen kreeft.','de kreeft ben jij, niet de helper; de hulp is neutraal.','nieuw schild','haven'),
41:('Duinpad met een lege wegwijzer.','Zijaanzicht, kijkt omlaag op zijn kaart.','even niet weten waar je bent, en zelf de weg terug zoeken.','nieuw schild','haven'),
42:('Steiger die wordt gerepareerd met een gereedschapskist.','Zit op een paal en wacht rustig.','er is iets kapot en er wordt aan gewerkt; jij wacht even.','nieuw schild','haven'),
}
for x in SCREENS:
    sc,kr,pa,la,li=SC[x['num']]
    x.update(scene=sc,kreeft=kr,past=pa,laag=la,licht=li,why=f'{sc} {kr}')
SCREENS.sort(key=lambda s:s['num'])
json.dump(SCREENS,open('screens.json','w'),indent=1,ensure_ascii=False)
json.dump(COPY,open('copy.json','w'),indent=0,ensure_ascii=False)
print(len(SCREENS),'schermen')
