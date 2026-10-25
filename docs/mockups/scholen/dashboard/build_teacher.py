from common import *
S1,S2,S3='var(--s1)','var(--s2)','var(--s3)'
LEG3=legend([('Afgerond',S1),('Bezig',S2),('Nog niet gestart','#ddd8cf')])
KL='<a href="#">Klas 8B</a>'
# ---------- 06 klasoverzicht
notyet=[('TXT-N86','Bezig','bij vraag 18 van 60','c-info','vr 23 okt 14:10'),('Q2M-7RD','Bezig','bij vraag 41 van 60','c-info','vr 23 okt'),('LK4-P9S','Bezig','op het Pauze-eiland','c-info','do 22 okt'),('WPB-S6A','Nog niet gestart','nog niet ingelogd','c-warn',''),('M4K-T2Q','Nog niet gestart','nog niet ingelogd','c-warn','')]
nr=''.join(f'<div class="todo"><div class="t"><b style="font-family:IBM Plex Mono;letter-spacing:.02em">{c}</b><small>{d}{" · laatst actief "+la if la else ""}</small></div><span class="chip {cl}">{s}</span></div>' for c,s,d,cl,la in notyet)
interest=[('Maken',6,RIA[0][1]),('Helpen',5,RIA[3][1]),('Creatief',4,RIA[2][1]),('Onderzoeken',2,RIA[1][1]),('Leiden',2,RIA[4][1]),('Ordenen',1,RIA[5][1])]
ih=''.join(f'<div class="hb" style="grid-template-columns:110px minmax(0,1fr) 92px"><div class="nm" style="font-size:13px">{n}</div><div class="tr2" style="grid-column:auto;height:12px"><i style="width:{v/6*100}%;background:{c}"></i></div><div class="vl">{v} <small>leerlingen</small></div></div>' for n,v,c in interest)
dj=[('Dierenarts',4,''),('Politieagent',3,''),('Kok',2,VV),('Profvoetballer',2,''),('Kapper',2,''),('Game-ontwikkelaar',2,'')]
djh=hbars(dj,4)+'<div class="hb"><div class="rk"></div><div class="nm" style="color:var(--soft)">Overig (komen 1 keer voor)</div><div class="vl">2</div></div><div class="hb"><div class="rk"></div><div class="nm" style="color:var(--soft)">Weet ik nog niet</div><div class="vl">3</div></div>'
DL=[('Afgerond','#33505f',[0,6,11,15,18,20],False,True)]
daily=svgw(line_chart(['ma 12 okt','wo 14 okt','vr 16 okt','di 20 okt','do 22 okt','vr 23 okt'],DL,25,5,w=560,h=230),line_chart(['12/10','14/10','16/10','20/10','22/10','23/10'],DL,25,5,w=360,h=220))
body=ph(f'Mijn klassen › <b>Klas 8B</b>','Klas 8B','Groep 8 · 25 leerlingcodes · testvenster open tot vr 30 okt. Je ziet codes, geen namen. De namenlijst houd je zelf bij.',btn('Codelijst printen','print',g=True)+btn('Download klasverslag','dl'))
body+='<div class="grid g4 kp">'+kpi('Afgerond','20','<span class="up">+2</span> op vr 23 okt','#33505f',' <small>van 25</small>')+kpi('Bezig','3','gaan verder waar ze waren','var(--s2)')+kpi('Nog niet gestart','2','plan een moment in de klas','#ddd8cf')+kpi('Gemiddelde tijd','24 min','voor 60 vragen')+'</div>'
body+='<div class="grid g32">'+card('Voortgang van de klas','<div class="bar" style="height:22px;border-radius:6px"><i style="width:80%;background:var(--s1)"></i><i style="width:12%;background:var(--s2)"></i><i style="width:8%;background:#ddd8cf"></i></div><div style="margin:8px 0 14px">'+LEG3+'</div>'+daily,'Afgeronde leerlingen sinds de start op ma 12 okt')
body+=card('Nog niet klaar',nr+'<div style="margin-top:10px">'+btn('Alle leerlingcodes','arrow',g=True,sm=True)+'</div>','5 codes · zoek de naam op je eigen lijst')+'</div>'
body+='<div class="grid g2">'+card('Interesses van de klas',ih+'<div class="note" style="margin-top:12px">'+I('bulb')+'<span>Jouw klas is een doe-klas: Maken en Helpen staan bovenaan. Bekijk de lesideeën die daarbij passen.</span></div>','Aantal leerlingen met deze interesse bovenaan · 20 leerlingen',btn('Lesideeën','arrow',g=True,sm=True))
body+=card('Top droombanen van de klas',djh,'Zelf gekozen door leerlingen. Een droombaan die maar 1 keer voorkomt, tellen we bij Overig. Zo blijft het anoniem.')+'</div>'
page('06-leraar-klasoverzicht','leraar','06','klas',body,'Klasoverzicht 8B')
# ---------- 07 vergelijking
axes=['Maken','Onderzoeken','Creatief','Helpen','Leiden','Ordenen']
rd=radar(axes,[('Regio','#9c7a45',[24,15,17,22,12,10],True),('Jaarlaag','#9db8c3',[26,17,17,22,11,7],False),('8B','#33505f',[30,10,20,25,10,5],False)],440,32)
leg=legend([('Klas 8B','#33505f'),('Jaarlaag groep 8 (46 leerlingen)','#9db8c3'),('Regio groep 8','#9c7a45','dash')])
rows=[('Maken',30,26,24),('Onderzoeken',10,17,15),('Creatief',20,17,17),('Helpen',25,22,22),('Leiden',10,11,12),('Ordenen',5,7,10)]
def diff(a,b):
    d=a-b; c='var(--green)' if d>2 else ('var(--coral)' if d<-2 else 'var(--soft)'); t='hoger' if d>2 else ('lager' if d<-2 else 'gelijk')
    return f'<span style="color:{c};font-weight:600">{t}</span>'
tb='<div class="tw"><table class="tbl"><thead><tr><th>Interesse</th><th>8B</th><th>Jaarlaag</th><th>Regio <span class="demo">Voorbeeld</span></th><th>8B t.o.v. jaarlaag</th></tr></thead><tbody>'+''.join(f'<tr><td class="k">{n}</td><td><b>{a}%</b></td><td>{b}%</td><td>{c}%</td><td>{diff(a,b)}</td></tr>' for n,a,b,c in rows)+'</tbody></table></div>'
tb+='<div class="mcards">'+''.join(f'<div class="mc"><div class="hd"><b>{n}</b>{diff(a,b)}</div><dl><dt>Klas 8B</dt><dd>{a}%</dd><dt>Jaarlaag groep 8</dt><dd>{b}%</dd><dt>Regio (voorbeelddata)</dt><dd>{c}%</dd></dl></div>' for n,a,b,c in rows)+'</div>'
drv=[('Samen met anderen',11,25),('Zelf kiezen',7,19),('Rust en duidelijkheid',9,17),('Iets betekenen voor anderen',6,14),('Uitdaging en nieuwe dingen',4,12)]
dh=''.join(f'<div style="padding:8px 0;border-bottom:1px solid var(--line2)"><div style="font-weight:500;margin-bottom:4px">{n}</div><div style="display:grid;grid-template-columns:minmax(0,1fr) 120px;gap:3px 10px;align-items:center;font-size:12.5px"><div class="tr2" style="height:7px;background:var(--line2);border-radius:3px;overflow:hidden"><i style="display:block;height:100%;width:{a/20*100}%;background:var(--acc)"></i></div><span><b style="color:var(--acc)">{a}</b> van 20 in 8B</span><div class="tr2" style="height:7px;background:var(--line2);border-radius:3px;overflow:hidden"><i style="display:block;height:100%;width:{b/46*100}%;background:var(--s2)"></i></div><span><b>{b}</b> van 46 jaarlaag</span></div></div>' for n,a,b in drv)
dl=''.join(stackrow(k,s,[(a,S1),(g-a,S2),(c-g,S3)],c,f'<b>{a}</b> van {c} afgerond') for k,s,c,g,a in [('Klas 8B','jouw klas',25,23,20),('Klas 8A','R. Visser',26,24,22)])+stackrow('Plusklas','S. de Vries',[],1,'4 codes',masked='Te weinig leerlingen om te tonen')
body=ph(f'Mijn klassen › {KL} › <b>Vergelijking</b>','Klas 8B vergeleken','Hoe verhoudt jouw klas zich tot de rest van groep 8 op school en tot de regio? Alleen groepscijfers.','<div class="tabs"><a class="on" href="#">Jaarlaag groep 8</a><a href="#">Hele school groep 7/8</a></div>')
body+='<div class="grid g2">'+card('Interesseprofiel',leg+'<div style="max-width:440px;margin:6px auto 0">'+svgw(rd)+'</div>','Deel van de leerlingen met deze interesse bovenaan, in procenten')+card('Verschillen op een rij',tb+'<p style="font-size:12.5px;color:var(--soft);margin:10px 0 0">Hoger of lager betekent meer dan 2 procentpunt verschil. Regio is voorbeelddata tot er genoeg scholen meedoen.</p>','8B naast jaarlaag en regio')+'</div>'
body+='<div class="grid g3">'+card('Drijfveren',legend([('Klas 8B','var(--acc)'),('Jaarlaag','var(--s2)')])+dh,'Aantal leerlingen met deze drijfveer in hun top 3')
body+=card('Deelname in de jaarlaag',LEG3+'<div style="height:10px"></div><div class="srows c">'+dl+'</div>','Groep 8 op Demo College Lobsy')
ins=[('Meer doeners dan gemiddeld','Maken, Creatief en Helpen liggen hoger dan in de jaarlaag.'),('Minder onderzoekers','Onderzoeken is kleiner dan in de jaarlaag. Een proefje of excursie kan nieuwsgierig maken.'),('Samen werkt goed','Samen met anderen is de grootste drijfveer. Werk vaker in groepjes.')]
body+=card('Wat valt op',''.join(f'<div class="insight"><div class="no">{k+1}</div><div><b>{t}</b><p>{d}</p></div></div>' for k,(t,d) in enumerate(ins)))+'</div>'
page('07-leraar-vergelijking','leraar','07','pieren',body,'Vergelijking 8B')
# ---------- 08 leerling-detail
steps=[('Nu','Groep 8','Oefen samenwerken en rustig blijven als het spannend wordt.'),('Straks','Middelbare school','Vmbo-kader of hoger. Gymnastiek en maatschappijleer helpen.'),('Opleiding','Politieopleiding','Op de Politieacademie, een paar jaar leren en stage lopen.'),('Droom','Politieagent','Op straat, op het bureau of in een team.')]
st='<div class="steps">'+''.join(f'<div class="step"><div class="sn">{k+1}</div><div class="sk">{a}</div><b>{b}</b><p>{c}</p></div>' for k,(a,b,c) in enumerate(steps))+'</div>'
css='''.steps{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;position:relative}
.step{position:relative;background:#faf8f4;border:1px solid var(--line);border-radius:10px;padding:14px 14px 12px}
.step:not(:last-child):after{content:"";position:absolute;right:-12px;top:27px;width:12px;height:2px;background:var(--ochre)}
.step .sn{width:26px;height:26px;border-radius:50%;background:var(--acc);color:#fff;display:grid;place-items:center;font-weight:600;font-size:13px;margin-bottom:8px}
.step:last-child .sn{background:var(--ochre)}
.step .sk{font-size:11px;letter-spacing:.1em;text-transform:uppercase;color:var(--ochre);font-weight:600}
.step b{display:block;font-size:14.5px;margin:1px 0 3px}.step p{margin:0;font-size:12.5px;color:var(--soft)}
.tiles{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin-top:14px}.tile{border:1px solid var(--line);border-radius:10px;padding:10px 12px;background:#faf8f4}.tile small{display:block;font-size:11px;letter-spacing:.1em;text-transform:uppercase;color:var(--soft);font-weight:600}.tile b{font-size:14.5px}
.pager{display:flex;gap:8px}
@media(max-width:760px){.steps{grid-template-columns:1fr;gap:10px}.step{display:grid;grid-template-columns:34px 1fr;column-gap:10px;padding:12px}.step .sn{grid-row:1/4;margin:0}.step:not(:last-child):after{right:auto;left:24px;top:auto;bottom:-11px;width:2px;height:10px}.tiles{grid-template-columns:1fr}}'''
story='<p style="margin:0;font-size:15px;line-height:1.6">Jij blijft rustig als het even spannend wordt. Je helpt graag en bedenkt iets creatiefs. Belangrijk voor jou: samen met anderen. Je voelt je thuis op een plek waar het gezellig en open is. Je houdt van muziek en van koken of bakken.</p>'
tiles='<div class="tiles">'+''.join(f'<div class="tile"><small>{a}</small><b>{b}</b></div>' for a,b in [('Zo ben jij','Rustig en sterk'),('Dit doe je graag','Creatief en helpen'),('Dit vind je belangrijk','Samen met anderen'),('Hier voel je je thuis','Gezellig en open')])+'</div>'
ln='<div style="font-size:12px;color:var(--soft);font-weight:600;margin-bottom:6px">VINDT HET LEUK</div>'+''.join(f'<span class="tag p">{t}</span>' for t in ['Muziek','Koken en bakken','Bouwen en knutselen','Buiten zijn'])+'<div style="font-size:12px;color:var(--soft);font-weight:600;margin:10px 0 6px">VINDT HET NIET LEUK</div>'+''.join(f'<span class="tag n">{t}</span>' for t in ['Dieren verzorgen','Lang stilzitten'])+'<div style="font-size:12px;color:var(--soft);font-weight:600;margin:14px 0 6px">BEROEPEN DIE BIJ DE UITKOMST PASSEN</div>'+''.join(f'<span class="tag">{t}</span>' for t in ['Kok','Verpleegkundige','Pedagogisch medewerker','Sportleraar'])+'<p style="font-size:12.5px;color:var(--soft);margin:8px 0 0">Dit ziet de leerling ook in de app.</p>'
gs=['Je helpt graag. Wanneer heb je laatst iemand geholpen, en hoe voelde dat?','Wat zou jij als politieagent het liefst doen: op straat, op het bureau of met een team?','Je blijft rustig als het spannend wordt. Waar heb je dat gemerkt?','Koken en muziek vind je leuk. Past dat ook bij een droombaan?']
al='<div style="font-size:12px;color:var(--soft);font-weight:600;margin-bottom:6px">PAST AL BIJ DEZE DROOM</div>'+''.join(f'<span class="tag p">{t}</span>' for t in ['Nieuwsgierig','Rustig bij spanning','Helpt graag'])+'<div style="font-size:12px;color:var(--soft);font-weight:600;margin:8px 0 6px">KAN NOG GROEIEN</div>'+''.join(f'<span class="tag">{t}</span>' for t in ['Plannen','Een groep op gang helpen','Zeggen wat je eerlijk vindt'])
body=f'<style>{css}</style>'+ph(f'Mijn klassen › {KL} › <a href="#">Leerlingcodes</a> › <b>S7H-JJ8</b>','<span style="font-family:IBM Plex Mono;font-weight:500;letter-spacing:.02em">S7H-JJ8</span>','Klas 8B · klaar op do 22 okt · 60 vragen. Weet je wie dit is? Kijk op je eigen codelijst.','<div class="pager">'+btn('Vorige code',g=True)+btn('Volgende code',g=True)+'</div>'+btn('Download verslag (PDF)','dl'))
body+='<div class="note" style="margin-bottom:16px">'+I('lock')+'<span>Je ziet een code, geen naam. Losse antwoorden zie je niet, alleen de uitkomst. Er staan geen scores in.</span></div>'
body+='<div class="grid g32">'+card('Dit ben jij',story+tiles,'Zo ziet de leerling het zelf ook')+card('Leuk en niet leuk',ln,'Gekozen op het Pauze-eiland')+'</div>'
body+=card('Droombaan: Politieagent',st+'<div style="display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1fr);gap:16px;margin-top:16px" class="g2x">'+f'<div>{al}</div><div class="note o">{I("info")}<span>Ook interessant om samen te bekijken: boa, brandweer, jongerenwerker. Deze beroepen passen bij helpen en rustig blijven.</span></div></div>','Ingevuld door de leerling · 4 stappen naar de droom',btn('Bespreek in gesprek','chat',g=True,sm=True))
body+='<div style="height:16px"></div><div class="grid g2">'+card('Gespreksstarters','<ul class="qs">'+''.join(f'<li>{x}</li>' for x in gs)+'</ul>','Voor een kort gesprek van 5 minuten')+card('Tips voor jou als leraar','<ul class="qs"><li>Geef deze leerling een rol waarin hij of zij anderen helpt, zoals maatje van een nieuwe leerling.</li><li>Laat koken of muziek terugkomen in een presentatie over beroepen.</li><li>Bespreek de overstap: welke middelbare school past bij deze droom?</li></ul>')+'</div>'
body=body.replace('class="g2x"','class="g2x"')+'<style>@media(max-width:760px){.g2x{grid-template-columns:1fr!important}}</style>'
page('08-leraar-leerling-detail','leraar','08','leerling',body,'Leerling S7H-JJ8')
# ---------- 09 lesideeën
ideas=[('Maken','var(--r)','Bouw je eigen beroep','45 min · Techniek / Wereldoriëntatie','Groepjes bouwen met karton en tape een werkplek van een beroep uit de top van de klas. Daarna vertellen ze wat je daar doet.','Karton, tape, stiften'),
('Helpen','var(--s)','Helden van de buurt','60 min · Burgerschap','Leerlingen interviewen iemand die anderen helpt, zoals een verpleegkundige of buurtagent. Ze maken er een poster van.','Interviewkaart (in lesbrief)'),
('Creatief','var(--a)','Mijn droombaan als stripverhaal','45 min · Taal / Tekenen','Teken in 4 vakjes hoe jouw droombaan eruitziet: nu, straks, opleiding en de droom. Sluit aan bij de droombaanroute.','Stripblad (in lesbrief)'),
('Samen','var(--i)','Kwaliteitenkwartet','30 min · Kring','Leerlingen geven elkaar kaartjes met kwaliteiten. Wie past bij "rustig bij spanning"? Fijn voor een klas die graag samenwerkt.','Kwartetkaarten (printbaar)')]
ih=''.join(f'<section class="card" style="border-top:3px solid {c}"><div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:6px"><span class="tag" style="background:#f4f1eb">Past bij: {t}</span><span style="font-size:12px;color:var(--soft)">{I("clock").replace("<svg","<svg style=\'width:13px;height:13px;vertical-align:-2px\'")} {d.split(" · ")[0]}</span></div><h2 style="font-size:16px;margin:4px 0 2px">{n}</h2><div style="font-size:12.5px;color:var(--ochre);font-weight:600;margin-bottom:6px">{d.split(" · ")[1]}</div><p style="margin:0 0 10px;font-size:13.5px;color:var(--soft)">{x}</p><div style="font-size:12.5px;margin-bottom:12px"><b>Nodig:</b> {m}</div><div style="display:flex;gap:8px">{btn("Open lesbrief","book",sm=True)}{btn("Zet in planning","cal",g=True,sm=True)}</div></section>' for t,c,n,d,x,m in ideas)
prof='<div style="display:flex;flex-wrap:wrap;gap:8px;align-items:center">'+''.join(f'<span class="tag" style="background:{c};color:#fff">{n} · {v}</span>' for n,v,c in [('Maken',6,'#9c7a45'),('Helpen',5,'#4f7d64'),('Creatief',4,'#b4533e')])+'<span style="font-size:13px;color:var(--soft)">en drijfveer <b style="color:var(--ink)">Samen met anderen</b></span></div>'
gq=['Welk beroep zou je een dag willen proberen, en waarom?','Wat kun jij goed dat niemand op een rapport ziet?','Wie in jouw omgeving heeft een beroep dat je interessant vindt?','Wat vind je spannend aan de middelbare school?','Welk nieuw beroep bestaat er over 20 jaar misschien?']
gast=hbars([('Dierenarts',4),('Politieagent',3),('Kok',2,VV)],4)
body=ph(f'Mijn klassen › {KL} › <b>Lesideeën</b>','Lesideeën voor klas 8B','Ideeën die passen bij het profiel van jouw klas. Gebaseerd op groepscijfers, dus geschikt voor de hele klas.',btn('Alle lesbrieven','book',g=True))
body+=card('Het profiel van jouw klas',prof,'20 leerlingen klaar · aantal leerlingen met deze interesse bovenaan')
body+='<div style="height:16px"></div><div class="grid g2">'+ih+'</div>'
body+='<div class="grid g2">'+card('Gesprek in de klas','<ul class="qs">'+''.join(f'<li>{x}</li>' for x in gq)+'</ul>','Vragen voor een kringgesprek van 15 minuten',btn('Print als kaartjes','print',g=True,sm=True))+card('Gastles uitnodigen',gast+'<p style="font-size:13px;color:var(--soft);margin:10px 0">De meest gekozen droombanen. Vraag ouders of iemand uit de buurt om erover te komen vertellen.</p>'+btn('Download uitnodiging voor ouders','dl',g=True,sm=True),'Op basis van de droombanen van de klas')+'</div>'
page('09-leraar-lesideeen','leraar','09','les',body,'Lesideeën 8B')
# ---------- 10 testvenster
days=['5','6','7','8','9','12','13','14','15','16','19','20','21','22','23','26','27','28','29','30','2','3','4','5','6']
tl='<div class="tl">'
for k,d in enumerate(days):
    inw=5<=k<=19; today=k==15
    cls='w' if inw else ''
    tl+=f'<div class="d {cls}{" today" if k==15 else ""}"><span>{d}</span></div>'
tl+='</div>'
css='''.tl{display:grid;grid-template-columns:repeat(25,minmax(0,1fr));gap:3px;margin:8px 0 4px}
.tl .d{height:34px;border-radius:5px;background:#efece6;display:flex;align-items:flex-end;justify-content:center;font-size:11px;color:var(--soft);padding-bottom:3px}
.tl .d.w{background:var(--green2);color:#2f5a43;font-weight:600}
.tl .d.today{background:var(--green);color:#fff;box-shadow:0 0 0 2px #fff,0 0 0 4px var(--green)}
.tlm{display:flex;justify-content:space-between;font-size:11.5px;color:var(--mute);font-weight:600;letter-spacing:.06em;text-transform:uppercase}
.form{display:grid;grid-template-columns:1fr 1fr;gap:12px 14px}
.form label{display:block;font-size:12.5px;font-weight:600;margin-bottom:4px}
.inp{border:1px solid #cfd6d9;border-radius:8px;padding:9px 12px;background:#fff;font-size:14px;display:flex;justify-content:space-between;align-items:center}
.inp svg{width:16px;height:16px;color:var(--soft)}
.tog{display:flex;align-items:center;gap:10px;font-size:13.5px;grid-column:1/3}.tog i{width:36px;height:20px;border-radius:999px;background:var(--acc);position:relative;flex:none}.tog i:after{content:"";position:absolute;right:3px;top:3px;width:14px;height:14px;border-radius:50%;background:#fff}
.big{display:flex;gap:16px;align-items:center}.big .ic{width:48px;height:48px;border-radius:12px;background:var(--green2);color:var(--green);display:grid;place-items:center;flex:none}.big .ic svg{width:24px;height:24px}
.big b{font-size:19px;display:block}.big span{color:var(--soft);font-size:13.5px}
.msg{border:1px dashed var(--line);border-radius:10px;padding:14px;background:#faf8f4;font-size:13.5px}
@media(max-width:760px){.tl{grid-template-columns:repeat(15,minmax(0,1fr))}.tl .d:nth-child(-n+5),.tl .d:nth-child(n+21){display:none}.form{grid-template-columns:1fr}.tog{grid-column:auto}}'''
body=f'<style>{css}</style>'+ph(f'Mijn klassen › {KL} › <b>Testvenster</b>','Testvenster','Leerlingen kunnen alleen inloggen terwijl het testvenster open is. Zo maken ze de test in de klas, op een moment dat jij kiest.',btn('Nu sluiten',g=True))
body+='<div class="grid g21">'+card('Status',f'<div class="big"><div class="ic">{I("clock")}</div><div><b>Open, nog 5 schooldagen</b><span>Geopend op ma 12 okt om 08:30 · sluit op vr 30 okt om 16:00</span></div></div><div style="height:16px"></div><div class="tlm"><span>Oktober</span><span>November</span></div>{tl}<div class="legend" style="margin-top:8px"><span><i style="background:var(--green2)"></i>Testvenster open</span><span><i style="background:var(--green)"></i>Volgende lesdag, ma 26 okt</span><span><i style="background:#efece6"></i>Dicht</span></div>','Klas 8B · groep 8')
body+=card('Voortgang in dit venster','<div class="srows">'+stackrow('Klas 8B','25 codes',[(20,S1),(3,S2),(2,S3)],25,'<b>20</b> van 25')+'</div><div style="margin-top:12px">'+LEG3+'</div><div class="note o" style="margin-top:14px">'+I('info')+'<span>5 leerlingen zijn nog niet klaar. Plan een inhaalmoment voor vrijdag 30 oktober.</span></div>','Stand zondag 25 oktober')+'</div>'
form=f'''<div class="form"><div><label>Opent op</label><div class="inp">ma 12 okt 2026{I("cal")}</div></div><div><label>Om</label><div class="inp">08:30{I("clock")}</div></div>
<div><label>Sluit op</label><div class="inp">vr 30 okt 2026{I("cal")}</div></div><div><label>Om</label><div class="inp">16:00{I("clock")}</div></div>
<div class="tog"><i></i>Alleen tijdens schooltijd, ma t/m vr van 08:30 tot 16:00</div></div><div style="display:flex;gap:8px;margin-top:16px">{btn("Opslaan")}{btn("Annuleren",g=True)}</div>'''
body+='<div class="grid g2">'+card('Venster aanpassen',form,'Wijzigingen gelden meteen voor alle 25 codes')
expl='<div class="insight"><div class="no">1</div><div><b>Binnen het venster</b><p>Leerlingen loggen in met hun code en maken de test. Ze kunnen pauzeren en later verder gaan.</p></div></div><div class="insight"><div class="no">2</div><div><b>Buiten het venster</b><p>Inloggen kan niet. Antwoorden die al zijn gegeven blijven bewaard.</p></div></div><div class="insight"><div class="no">3</div><div><b>Na het sluiten</b><p>Wie nog bezig was, gaat verder als je het venster opnieuw opent. Resultaten blijven zichtbaar.</p></div></div>'
body+=card('Hoe werkt het testvenster?',expl+'<div style="font-size:12px;color:var(--soft);font-weight:600;margin:10px 0 6px">DIT ZIET EEN LEERLING ALS HET VENSTER DICHT IS</div><div class="msg">Het testvenster van je klas is dicht. Je leraar zet het weer open.</div>')+'</div>'
page('10-leraar-testvenster','leraar','10','veer',body,'Testvenster 8B')
