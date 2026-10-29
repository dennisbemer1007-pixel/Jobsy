from common import *
S1,S2,S3='var(--s1)','var(--s2)','var(--s3)'
CL=[('7A','Groep 7','g','A. Bakker',24,0,0,'plan','ma 2 nov','vr 20 nov','Nog niet bevestigd'),
('8A','Groep 8','g','R. Visser',26,24,22,'dicht','ma 5 okt','vr 16 okt','Bevestigd'),
('8B','Groep 8','g','S. de Vries',25,23,20,'open','ma 12 okt','vr 30 okt','Bevestigd'),
('Plusklas','Groep 8','g','S. de Vries',4,4,4,'dicht','ma 5 okt','vr 16 okt','Bevestigd'),
('2A','Vmbo-t 2','v','K. Smit',27,11,4,'open','ma 19 okt','vr 13 nov','Bevestigd'),
('H4','Havo 4','v','L. Peters',28,27,26,'dicht','ma 28 sep','vr 9 okt','Bevestigd'),
('V4','Vwo 4','v','J. Mulder',25,18,14,'open','ma 19 okt','vr 6 nov','Bevestigd')]
def kn(k): return k if len(k)>3 else 'Klas '+k
ST={'open':('c-open','Open'),'dicht':('c-dicht','Gesloten'),'plan':('c-plan','Gepland')}
def deel_rows(cls=CL):
    h=''
    for k,n,t,l,c,g,a,*_ in cls:
        if c<5: h+=stackrow(kn(k),n,[],1,f'{c} codes',masked='Te weinig leerlingen om te tonen'); continue
        h+=stackrow(kn(k),n,[(a,S1),(g-a,S2),(c-g,S3)],c,f'<b>{a}</b> van {c} afgerond' if g else 'Nog niet gestart')
    return h
LEG3=legend([('Afgerond',S1),('Bezig',S2),('Nog niet gestart','#ddd8cf')])
# ---------- 01 dashboard
labels=['21 sep','28 sep','5 okt','12 okt','19 okt','25 okt']
tr=line_chart(labels,[('Gestart','var(--s2)',[0,12,40,66,94,107],False,False),('Afgerond','#33505f',[0,9,31,52,77,90],False,True)],120,30,w=420,h=250)
trm=line_chart(['21/9','28/9','5/10','12/10','19/10','25/10'],[('Gestart','var(--s2)',[0,12,40,66,94,107],False,False),('Afgerond','#33505f',[0,9,31,52,77,90],False,True)],120,30,w=360,h=220)
todo=[('c-warn',I('lock'),'Ouders van klas 7A informeren','Testvenster opent ma 2 nov. Bevestig eerst dat ouders de brief hebben gehad.','Bevestigen'),
('c-plan',I('clock'),'Klas 2A loopt achter','4 van 27 afgerond, venster sluit vr 13 nov. Plan een lesuur met de mentor.','Herinner mentor'),
('c-info',I('team'),'Leraar L. Peters heeft geen tweestapsverificatie','Dit is nodig om leerlingverhalen te bekijken.','Stuur herinnering')]
tod=''.join(f'<div class="todo"><div class="ic {c}">{i}</div><div class="t"><b>{t}</b><small>{d}</small></div>{btn(a,g=True,sm=True)}</div>' for c,i,t,d,a in todo)
wk=''.join(f'<div class="todo"><div class="ic {ST[s][0]}">{I("cal")}</div><div class="t"><b>{kn(k)} <span class="chip {ST[s][0]}" style="margin-left:6px">{ST[s][1]}</span></b><small>{o} t/m {cl}</small></div></div>' for k,n,t,l,c,g,a,s,o,cl,p in [CL[2],CL[6],CL[4],CL[0]])
body=ph('Schoolbeheer','Dashboard','Hoe staat de loopbaantest ervoor op Demo College Lobsy? Alles in groepscijfers, zonder namen.',btn('Download overzicht','dl',g=True)+btn('Nieuwe klas','plus'))
body+='<div class="grid g4 kp">'+kpi('Klassen','7','4 groep 7/8 · 3 middelbare school')+kpi('Leerlingcodes','159','uitgedeeld aan leerlingen')+kpi('Afgerond','90',f'<span class="up">+13</span> sinds vorige week','#33505f',' <small>van 159</small>')+kpi('Testvensters open','3','8B, 2A en V4 · 1 gepland')+'</div>'
body+='<div class="grid g32">'+card('Deelname per klas',LEG3+'<div style="height:12px"></div><div class="srows">'+deel_rows()+'</div>','Stand zondag 25 oktober',btn('Alle klassen','arrow',g=True,sm=True))
body+=card('Voortgang over tijd',legend([('Gestart','var(--s2)','ln'),('Afgerond','#33505f','ln')])+svgw(tr,trm)+'<div class="note" style="margin-top:10px">'+I('info')+'<span>Deze week 13 leerlingen klaar, vooral uit 8B en V4.</span></div>','Aantal leerlingen, per week')+'</div>'
top3=hbars([('Dierenarts',12),('Politieagent',9),('Verpleegkundige',8,VV)],12)
body+='<div class="grid g3">'+card('Te doen',tod,'3 punten',btn('Alles','arrow',g=True,sm=True))+card('Testvensters',wk,'Open en gepland')+card('Meest gekozen droombanen',top3+'<div style="margin-top:10px">'+btn('Bekijk top 10','arrow',g=True,sm=True)+'</div>','Hele school, 76 leerlingen met een droombaan')+'</div>'
page('01-school-dashboard','school','01','school',body,'Dashboard')
# ---------- 02 vergelijking
LEGR=legend([(n,c) for n,c,_ in RIA])
g78=pctrow('Klas 8A','22 leerlingen',[23,18,18,23,9,9])+pctrow('Klas 8B','20 leerlingen',[30,10,20,25,10,5])+pctrow('Plusklas','4 leerlingen',[],masked='Te weinig leerlingen om te tonen')+pctrow('Klas 7A','nog niet gestart',[],masked='Nog geen resultaten')+'<hr class="sep">'+pctrow('Groep 7/8 totaal','46 leerlingen',[26,17,17,22,11,7])+pctrow('Regio groep 8 '+DEMO,'regio Utrecht',[24,15,17,22,12,10])
vo=pctrow('Klas 2A','4 leerlingen klaar',[],masked='Te weinig leerlingen om te tonen')+pctrow('Klas H4','26 leerlingen',[12,19,15,27,15,12])+pctrow('Klas V4','14 leerlingen',[7,36,14,14,21,8])+'<hr class="sep">'+pctrow('Middelbare school totaal','44 leerlingen',[10,25,15,23,17,10])+pctrow('Landelijk havo/vwo '+DEMO,'',[14,20,16,21,16,13])
dr=[('Samen met anderen',54,38),('Zelf kiezen',41,52),('Rust en duidelijkheid',37,22),('Iets betekenen voor anderen',30,35),('Uitdaging en nieuwe dingen',26,40),('Zekerheid',12,13)]
drh=''.join(f'<div style="padding:8px 0;border-bottom:1px solid var(--line2)"><div style="font-weight:500;margin-bottom:4px">{n}</div><div style="display:grid;grid-template-columns:minmax(0,1fr) 40px;gap:3px 10px;align-items:center;font-size:12.5px"><div class="tr2" style="height:7px;background:var(--line2);border-radius:3px;overflow:hidden"><i style="display:block;height:100%;width:{a}%;background:var(--ochre)"></i></div><b style="text-align:right;color:var(--ochre)">{a}%</b><div class="tr2" style="height:7px;background:var(--line2);border-radius:3px;overflow:hidden"><i style="display:block;height:100%;width:{b}%;background:var(--acc)"></i></div><b style="text-align:right;color:var(--acc)">{b}%</b></div></div>' for n,a,b in dr)
body=ph('Schoolbeheer › Inzicht','Vergelijking klassen','Zie per klas waar de interesses liggen en wat leerlingen drijft. Groep 7/8 en de middelbare school maken een andere vragenlijst, daarom staan ze apart.',
 '<div class="tabs"><a class="on" href="#">Interesses</a><a href="#">Drijfveren</a><a href="#">Deelname</a></div>'+btn('Download','dl',g=True))
body+='<div class="note o" style="margin-bottom:16px">'+I('info')+'<span>Percentages zijn het deel van de klas met deze interesse bovenaan. Groepen onder de 5 leerlingen tonen we niet. Regio en landelijk zijn voorbeelddata tot er genoeg scholen meedoen.</span></div>'
body+='<div class="grid g2">'+card('Groep 7/8: interesses per klas',LEGR+'<div style="height:12px"></div>'+g78,'Vragenlijst groep 7/8 · 60 vragen')+card('Middelbare school: interesses per klas',LEGR+'<div style="height:12px"></div>'+vo,'Vragenlijst middelbare school · 100 vragen')+'</div>'
body+='<div class="grid g12">'+card('Drijfveren',legend([('Groep 7/8','var(--ochre)'),('Middelbare school','var(--acc)')])+'<div style="height:6px"></div>'+drh,'Deel van de leerlingen met deze drijfveer in hun top 3')
ins=[('Helpen en Maken zijn sterk in groep 8','Bijna de helft van groep 7/8 heeft Helpen of Maken bovenaan. Dat is iets meer dan in de regio.'),
('V4 is een onderzoeksklas','Ruim een derde van V4 heeft Onderzoeken bovenaan. Een bezoek aan een lab of hogeschool past goed.'),
('Ordenen blijft klein','Weinig leerlingen kiezen Ordenen. Administratie en logistiek zoeken in de regio juist veel mensen.'),
('Oudere leerlingen willen meer zelf kiezen','In de middelbare school wordt Zelf kiezen en Uitdaging belangrijker, Rust en duidelijkheid minder.')]
body+=card('Wat valt op',''.join(f'<div class="insight"><div class="no">{k+1}</div><div><b>{t}</b><p>{d}</p></div></div>' for k,(t,d) in enumerate(ins)),'Automatisch samengevat uit de groepscijfers',btn('Gebruik in ouderavond','arrow',g=True,sm=True))+'</div>'
page('02-school-vergelijking-klassen','school','02','boten',body,'Vergelijking klassen')
# ---------- 03 top 10
t10=[('Dierenarts',12,'','12'),('Politieagent',9,'','9'),('Verpleegkundige',8,VV,'8'),('Kok',7,VV,'7'),('Game-ontwikkelaar',7,'','7'),('Profvoetballer',6,'','6'),('Leraar basisonderwijs',6,VV,'6'),('Elektricien',5,VV,'5'),('Advocaat',5,'','5'),('Kapper',4,'','4')]
sec=[('Zorg en welzijn',16,VV),('Dieren en natuur',12,''),('Techniek en installatie',11,VV),('Veiligheid',9,''),('ICT en media',8,''),('Horeca en voeding',8,VV),('Sport en bewegen',6,''),('Onderwijs',6,VV)]
gap=[('Techniek en installatie','11 leerlingen','Veel vraag','c-open'),('Logistiek en transport','2 leerlingen','Veel vraag','c-open'),('Administratie','3 leerlingen','Veel vraag','c-open'),('Bouw','4 leerlingen','Veel vraag','c-open')]
gp=''.join(f'<div class="todo"><div class="t"><b>{a}</b><small>Gekozen door {b}</small></div><span class="chip {c}">{t}</span></div>' for a,b,t,c in gap)
body=ph('Schoolbeheer › Inzicht','Beroepen en droombanen','De droombanen die leerlingen zelf kozen, opgeteld voor de hele school. Zo zie je welke beroepen leven en waar kansen liggen.',
 '<div class="tabs"><a class="on" href="#">Hele school</a><a href="#">Groep 7/8</a><a href="#">Middelbare school</a></div>'+btn('Download','dl',g=True))
body+='<div class="grid g4 kp">'+kpi('Droombaan ingevuld','76','van 90 afgeronde leerlingen')+kpi('Weet ik nog niet','14','normaal op deze leeftijd')+kpi('Verschillende beroepen','41','genoemd door leerlingen')+kpi('Met veel vraag in de regio','26','leerlingen kozen zo een beroep')+'</div>'
body+='<div class="grid g21">'+card('Top 10 droombanen',hbars(t10,12)+'<p style="font-size:12.5px;color:var(--soft);margin:10px 0 0">Aantal leerlingen. Beroepen die minder dan 3 keer voorkomen staan niet in de lijst.</p>','Hele school · 76 leerlingen met een droombaan',btn('Groep 7/8 en VO naast elkaar','cmp',g=True,sm=True))
body+='<div>'+card('Top sectoren',hbars(sec,16,'var(--ochre)'),'Waar de droombanen bij horen')+'</div></div>'
body+='<div class="grid g2">'+card('Kansen om te laten zien',gp+'<div style="margin-top:10px">'+btn('Bekijk Veel vraag in jouw regio','map',g=True,sm=True)+'</div>','Sectoren met veel vraag in de regio, vergeleken met de keuzes van leerlingen. '+DEMO)
cmp=[('Dierenarts',9,3),('Politieagent',6,3),('Verpleegkundige',3,5),('Kok',5,2),('Game-ontwikkelaar',4,3),('Profvoetballer',5,1)]
ch=''.join(f'<div class="hb" style="grid-template-columns:minmax(0,1fr) 90px 60px"><div class="nm">{n}</div><div class="vl" style="color:var(--ochre)">{a}</div><div class="vl">{b}</div></div>' for n,a,b in cmp)
body+=card('Groep 7/8 en middelbare school',f'<div class="hb" style="grid-template-columns:minmax(0,1fr) 90px 60px;font-size:11.5px;color:var(--mute);font-weight:600;letter-spacing:.05em;text-transform:uppercase"><div>Beroep</div><div>Groep 7/8</div><div>VO</div></div>'+ch,'Aantal leerlingen per droombaan')+'</div>'
page('03-school-top10-beroepen','school','03','beroepen',body,'Beroepen en droombanen')
# ---------- 04 klassen en codes
rows='';mc=''
for k,n,t,l,c,g,a,s,o,cl,p in CL:
    small=c<5
    prog=f'<div class="mini"><i style="width:{a/c*100}%;background:{S1}"></i><i style="width:{(g-a)/c*100}%;background:{S2}"></i></div><small>{a} van {c} afgerond{", "+str(g-a)+" bezig" if g-a else ""}</small>'
    pc='c-open' if p=='Bevestigd' else 'c-warn'
    rows+=f'<tr><td class="k">{kn(k)}<small>{n}</small></td><td>{l}</td><td>{c}</td><td>{prog}</td><td><span class="chip {ST[s][0]}">{ST[s][1]}</span><small style="margin-top:3px">{o} t/m {cl}</small></td><td><span class="chip {pc}">{p}</span></td><td style="text-align:right">{btn("Beheren",g=True,sm=True)}</td></tr>'
    mc+=f'<div class="mc"><div class="hd"><b>{kn(k)} <span style="font-weight:400;color:var(--soft);font-size:13px">· {n}</span></b><span class="chip {ST[s][0]}">{ST[s][1]}</span></div><dl><dt>Leraar</dt><dd>{l}</dd><dt>Codes</dt><dd>{c}</dd><dt>Afgerond</dt><dd>{a} van {c}</dd><dt>Bezig</dt><dd>{g-a}</dd><dt>Testvenster</dt><dd>{o} t/m {cl}</dd><dt>Ouders</dt><dd>{p}</dd></dl><div class="ft">{btn("Beheren",g=True,sm=True)}{btn("Codelijst","print",g=True,sm=True)}</div></div>'
tbl=f'<div class="tw"><table class="tbl"><thead><tr><th>Klas</th><th>Leraar</th><th>Codes</th><th>Voortgang</th><th>Testvenster</th><th>Ouders</th><th></th></tr></thead><tbody>{rows}</tbody></table></div><div class="mcards">{mc}</div>'
body=ph('Schoolbeheer › Leerlingen','Klassen en codes','Maak klassen aan, deel codes uit en plan het testvenster. Leerlingen loggen in met hun code, zonder naam.',btn('Codelijsten printen','print',g=True)+btn('Nieuwe klas','plus'))
body+='<div class="grid g4 kp">'+kpi('Klassen','7','6 leraren')+kpi('Codes','159','12 nog nooit gebruikt')+kpi('Testvensters','3 open','1 gepland, 3 gesloten')+kpi('Ouders bevestigd','6 van 7','7A nog niet')+'</div>'
body+=card('Alle klassen',tbl,'Schooljaar 2026-2027','<div class="tabs"><a class="on" href="#">Alle 7</a><a href="#">Groep 7/8</a><a href="#">Middelbare school</a></div>')
body+='<div class="grid g3" style="margin-top:16px">'+card('Zo werkt het','<ol style="margin:0;padding-left:18px;font-size:13.5px;line-height:1.7"><li>Maak een klas aan en kies het niveau.</li><li>Print de codelijst en houd zelf bij welke code bij wie hoort.</li><li>Bevestig dat ouders zijn geïnformeerd.</li><li>Plan het testvenster. Alleen dan kunnen leerlingen inloggen.</li></ol>')+card('Codes vervangen','<p style="margin:0;font-size:13.5px;color:var(--soft)">Code kwijt of verkeerd uitgedeeld? Vervang hem in de klas. De oude code werkt dan niet meer.</p>')+card('Einde schooljaar','<p style="margin:0;font-size:13.5px;color:var(--soft)">Op 31 juli 2027 worden alle codes en antwoorden van dit schooljaar verwijderd. Groepscijfers blijven bewaard.</p>')+'</div>'
page('04-school-klassen-en-codes','school','04','huisjes',body,'Klassen en codes')
# ---------- 05 inzichten
kern=[('Bijna 6 op de 10 leerlingen hebben de test gedaan','90 van de 159 leerlingen zijn klaar. 7A begint op 2 november.'),
('Helpen en Onderzoeken leven het meest','Op de hele school staat Helpen bovenaan. In groep 8 is Maken het grootst: met je handen werken.'),
('Zorg en techniek zijn populair, en hebben veel vraag','26 leerlingen kozen een droombaan met veel vraag in de regio, zoals verpleegkundige en elektricien.'),
('14 leerlingen weten het nog niet','Dat is normaal. Een beroepenmiddag of open dag helpt ze verder.')]
kh=''.join(f'<div class="insight"><div class="no">{k+1}</div><div><b>{t}</b><p>{d}</p></div></div>' for k,(t,d) in enumerate(kern))
q=['Waar wordt uw kind blij van, op school en daarbuiten?','Welk beroep uit de top 10 kent u van dichtbij? Wilt u daarover vertellen in de klas?','Wat vindt uw kind spannend aan de overstap naar een nieuwe school?','Hoe kunnen we samen helpen als uw kind nog niet weet wat het wil?']
mini_t=hbars([('Dierenarts',12),('Politieagent',9),('Verpleegkundige',8),('Kok',7),('Game-ontwikkelaar',7)],12)
prev=f'''<div style="border:1px solid var(--line);border-radius:8px;padding:18px;background:#fdfcfa;box-shadow:0 8px 24px rgba(31,42,51,.07)">
<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:8px"><div class="brand" style="font-size:15px"><img src="img/merk.png" alt="" style="width:12px;height:19px">Lobsy</div><span style="font-size:11px;color:var(--soft)">Demo College Lobsy · oktober 2026</span></div>
<img src="img/hdr-ouderavond-desk.jpg" alt="" style="width:100%;height:48px;object-fit:cover;border-radius:4px">
<div style="font:600 17px 'Source Serif Pro',serif;margin:10px 0 2px">Wat onze leerlingen drijft</div><div style="font-size:11.5px;color:var(--soft);margin-bottom:8px">Groepscijfers van 90 leerlingen. Geen namen, geen losse antwoorden.</div>
<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;font-size:12px"><div><b style="font-size:12px">Interesses</b>{pctrow('Hele school','',[18,21,16,23,14,8]).replace('class="pr"','class="pr" style="grid-template-columns:1fr"')}{legend([(n,c) for n,c,_ in RIA]).replace('font-size:12.5px','')}</div><div style="font-size:12px"><b>Top 5 droombanen</b>{mini_t}</div></div></div>'''
body=ph('Schoolbeheer › Inzicht','Inzichten voor gesprek en ouderavond','Kant-en-klare boodschappen en een printbare samenvatting. Handig voor een ouderavond, teamoverleg of de nieuwsbrief.',btn('Kopieer voor nieuwsbrief','copy',g=True)+btn('Download presentatie','dl',g=True)+btn('Download PDF','dl'))
body+='<div style="display:flex;gap:10px;flex-wrap:wrap;margin-bottom:16px"><div class="tabs"><a class="on" href="#">Hele school</a><a href="#">Groep 7/8</a><a href="#">Middelbare school</a><a href="#">Per klas</a></div><div class="schoolsel" style="font-size:13px">Periode: dit schooljaar'+I('chev')+'</div></div>'
body+='<div class="grid g2">'+card('Kernboodschappen',kh,'Zo kun je het vertellen',btn('Bewerken',g=True,sm=True))+card('Voorbeeld van de samenvatting',prev,'1 pagina A4 · klaar om te printen')+'</div>'
body+='<div class="grid g3">'+card('Gespreksvragen voor ouders','<ul class="qs">'+''.join(f'<li>{x}</li>' for x in q)+'</ul>')
body+=card('Ideeën voor de ouderavond','<ul class="qs"><li>Laat ouders zelf 6 vragen uit de test maken en vergelijk met de klas.</li><li>Nodig 3 ouders uit met een beroep uit de top 10.</li><li>Zet de interesses van de klas op het bord en praat erover in groepjes.</li></ul>')
body+=card('Wat staat er wel en niet in','<div class="todo"><div class="ic c-open">'+I('todo')+'</div><div class="t"><b>Wel</b><small>Aantallen en verdelingen per klas en voor de hele school.</small></div></div><div class="todo"><div class="ic c-warn">'+I('lock')+'</div><div class="t"><b>Niet</b><small>Namen, codes, losse antwoorden of groepen onder de 5 leerlingen.</small></div></div>')+'</div>'
page('05-school-inzichten-ouderavond','school','05','ouderavond',body,'Inzichten voor gesprek')
