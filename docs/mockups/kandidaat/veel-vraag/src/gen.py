import re
CSS=open('base.css').read()
I={
'trend':'<path d="M4 16l5-5 4 3 7-7"/><path d="M15 7h5v5"/>',
'check':'<path d="m5 12 4 4 10-10"/>',
'bike':'<circle cx="6" cy="16" r="3.5"/><circle cx="18" cy="16" r="3.5"/><path d="M6 16l4-7h5l3 7M10 9 8.5 6H7M12.5 16 15 9"/>',
'clock':'<circle cx="12" cy="12" r="8"/><path d="M12 8v4.5l3 2"/>',
'euro':'<path d="M17 6.5A6.5 6.5 0 1 0 17 17.5M4 10h9M4 14h9"/>',
'x':'<path d="M6 6l12 12M18 6 6 18"/>',
'info':'<circle cx="12" cy="12" r="9"/><path d="M12 11v5M12 8h.01"/>',
'heart':'<path d="M12 20s-7.5-4.6-7.5-10.2A4.3 4.3 0 0 1 12 7.4a4.3 4.3 0 0 1 7.5 2.4C19.5 15.4 12 20 12 20z"/>',
'box':'<path d="M4 8l8-4 8 4v8l-8 4-8-4z"/><path d="M4 8l8 4 8-4M12 12v8"/>',
'truck':'<path d="M3 6.5h11v10H3zM14 10h4l3 3.5v3h-7"/><circle cx="7" cy="17.5" r="1.8"/><circle cx="17.5" cy="17.5" r="1.8"/>',
'wrench':'<path d="M14.5 5.5a4 4 0 0 0 4.9 5.2l-8.6 8.6a2 2 0 0 1-2.8-2.8l8.6-8.6a4 4 0 0 1-2.1-2.4z"/>',
'book':'<path d="M4 5.5A1.5 1.5 0 0 1 5.5 4H11v15H5.5A1.5 1.5 0 0 0 4 20.5zM20 5.5A1.5 1.5 0 0 0 18.5 4H13v15h5.5a1.5 1.5 0 0 1 1.5 1.5z"/>',
'cap':'<path d="M2.5 9 12 4.5 21.5 9 12 13.5z"/><path d="M6.5 11v4.5c1.5 1.5 3.5 2 5.5 2s4-.5 5.5-2V11"/>',
'arrow':'<path d="M9.5 6l6 6-6 6"/>',
'pin':'<path d="M12 21s-6.5-6.2-6.5-11a6.5 6.5 0 0 1 13 0c0 4.8-6.5 11-6.5 11z"/><circle cx="12" cy="10" r="2.3"/>',
'chat':'<path d="M5 18.5V7a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H8z"/>',
'map':'<path d="M9 4 3.5 6v14L9 18l6 2 5.5-2V4L15 6z"/><path d="M9 4v14M15 6v14"/>',
'doc':'<path d="M7 3.5h7l4 4V20H7z"/><path d="M14 3.5V8h4M10 12h5M10 15.5h5"/>',
'tool':'<path d="M4 20l6-6M14 4l6 6-4 4-6-6z"/>',
}
def ic(n,sw=1.8): return f'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="{sw}" stroke-linecap="round" stroke-linejoin="round">{I[n]}</svg>'
VV=f'<span class="vvl">{ic("trend",2.2)}Veel vraag in jouw regio</span>'
TABS=[('Reis','<circle cx="12" cy="12" r="9"/><path d="m15.5 8.5-2 5-5 2 2-5 5-2z"/>'),('Paspoort','<circle cx="12" cy="8" r="4"/><path d="M4 21c1.5-4 4.5-6 8-6s6.5 2 8 6"/>'),('Zoeken','<circle cx="11" cy="11" r="6"/><path d="m20 20-4.5-4.5"/>'),('Sollicitaties','<rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 4V3h6v1M9 11l2 2 4-4"/>'),('Carrière','<circle cx="12" cy="12" r="3.5"/><path d="M12 3v2M12 19v2M3 12h2M19 12h2M5.6 5.6 7 7M17 17l1.4 1.4M5.6 18.4 7 17M17 7l1.4-1.4"/>')]
EXTRA='''
.vvl{display:inline-flex;align-items:center;gap:5px;height:22px;padding:0 8px 0 6px;border-radius:5px;background:#e6f2ea;color:#1f6b43;font-size:.78rem;font-weight:700;white-space:nowrap;font-family:"Nunito Sans",sans-serif;letter-spacing:0;line-height:1}
.vvl svg{width:14px;height:14px}
.vvl.sm{height:20px;font-size:.74rem}
.ic16 svg,svg.ic16{width:16px;height:16px}
.facts{display:flex;flex-wrap:wrap;gap:6px 16px;font-size:.9rem;font-weight:600;color:var(--text)}.facts span{display:inline-flex;align-items:center;gap:6px}.facts svg{width:16px;height:16px;color:var(--muted)}
/* match */
.mwrap{max-width:1120px;margin:0 auto;padding:22px 28px 10px}
.mh{display:flex;align-items:center;gap:14px;margin-bottom:10px}.mh img{width:40px;height:40px}.mh h1{font-size:1.7rem}.mh .sp{flex:1}
.prog{height:4px;border-radius:2px;background:#e4d9ca;margin:0 0 20px}.prog i{display:block;width:25%;height:100%;border-radius:2px;background:var(--brand)}
.mg{display:grid;grid-template-columns:440px minmax(0,1fr);gap:22px;align-items:start}
.mc{background:var(--surf);border:1px solid var(--border);border-radius:var(--rL);overflow:hidden;box-shadow:0 14px 34px -18px rgba(15,45,92,.35)}
.mc .ph{position:relative;height:230px}.mc .ph img{width:100%;height:100%;object-fit:cover;display:block}
.mc .fit{position:absolute;left:12px;top:12px;background:#fffcfa;border-radius:99px;padding:4px 12px 4px 8px;font-weight:800;font-size:.82rem;color:var(--ok);display:flex;gap:6px;align-items:center}.mc .fit svg{width:15px;height:15px}
.mc .bd{padding:16px 20px 18px}
.mc h2{font-size:1.4rem;margin:8px 0 2px}
.mc .org{color:var(--muted);font-size:.95rem;margin-bottom:10px}
.tags{display:flex;gap:6px;margin-top:12px}.tags span{background:#eef2f7;color:#2c4a73;font-weight:700;font-size:.8rem;border-radius:var(--rS);padding:3px 9px}
.acts{display:grid;grid-template-columns:repeat(3,1fr);margin-top:16px;text-align:center;font-weight:700;font-size:.88rem;color:var(--brand)}
.acts i{display:grid;place-items:center;width:52px;height:52px;border-radius:50%;margin:0 auto 6px;border:1px solid var(--border);background:#fff}.acts i svg{width:20px;height:20px}.acts .hot i{background:var(--coral);border-color:var(--coral);color:#fff}
.why>div{display:flex;gap:10px;align-items:flex-start;padding:10px 0;border-top:1px solid var(--border)}.why>div:first-of-type{border-top:0}
.why .ok{width:24px;height:24px;border-radius:50%;background:#e6f2ea;color:var(--ok);display:grid;place-items:center;flex-shrink:0}.why .ok svg{width:14px;height:14px}
.why b{color:var(--brand);display:block}
.kvg{display:grid;grid-template-columns:1fr 1fr;gap:12px 18px}.kvg small{display:block;font-size:.72rem;font-weight:800;letter-spacing:.04em;color:var(--muted);text-transform:uppercase}.kvg b{color:var(--brand)}
.vvnote{display:flex;gap:10px;align-items:flex-start;background:#f3f8f4;border:1px solid #d7eadc;border-radius:var(--rM);padding:12px 14px;font-size:.88rem;color:#2e4a3a}.vvnote svg{width:18px;height:18px;color:var(--ok);flex-shrink:0;margin-top:2px}
.list2>.vi{display:flex;gap:12px;align-items:center;padding:12px 0;border-top:1px solid var(--border)}.list2>.vi:first-child{border-top:0}
.vi img{width:64px;height:64px;border-radius:var(--rM);object-fit:cover;flex-shrink:0}.vi b{color:var(--brand)}
/* beroepen */
.ber{display:grid;gap:14px}
.bk{display:grid;grid-template-columns:52px minmax(0,1fr) auto;gap:16px;align-items:start;background:var(--surf);border:1px solid var(--border);border-radius:var(--rL);padding:18px 20px}
.bk .bi{width:52px;height:52px;border-radius:var(--rM);display:grid;place-items:center;background:#f6ece0;color:var(--coral)}.bk .bi svg{width:24px;height:24px}
.bk h3{font-size:1.2rem;margin:6px 0 4px}
.bk p{color:#3d4e63;font-size:.93rem}
.bk .meta{display:flex;flex-wrap:wrap;gap:6px 16px;margin-top:10px;font-size:.88rem;font-weight:600}.bk .meta span{display:inline-flex;gap:6px;align-items:center}.bk .meta svg{width:16px;height:16px;color:var(--muted)}
.bk .side2{display:flex;flex-direction:column;align-items:flex-end;gap:8px}
.fitp{display:inline-flex;align-items:center;gap:6px;font-size:.78rem;font-weight:700;border-radius:var(--rS);padding:3px 9px;background:#eef2f7;color:#2c4a73;white-space:nowrap}.fitp svg{width:13px;height:13px}
.oml{display:inline-flex;align-items:center;gap:6px;margin-top:10px;font-weight:700;font-size:.88rem;color:var(--brand);text-decoration:underline;text-underline-offset:3px}.oml svg{width:15px;height:15px;color:var(--ok)}
.src{font-size:.8rem;color:var(--muted);margin-top:10px}
/* omscholen */
.oc{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px}
.ok2{background:var(--surf);border:1px solid var(--border);border-radius:var(--rL);padding:18px}
.ok2 .top{display:flex;gap:12px;align-items:flex-start}
.ok2 h3{font-size:1.08rem;margin:4px 0 2px}
.ok2 .sub{font-size:.88rem;color:var(--muted)}
.ok2 .facts{margin-top:12px;font-size:.86rem}
.ok2 .ft{display:flex;justify-content:space-between;align-items:center;gap:10px;margin-top:14px;padding-top:12px;border-top:1px solid var(--border)}
.bbl{display:inline-flex;align-items:center;gap:5px;font-size:.76rem;font-weight:800;border-radius:var(--rS);padding:3px 8px;background:#fdeee0;color:#9c4318}
.bbl svg{width:13px;height:13px}
.bblph{width:64px;height:64px;border-radius:var(--rM);object-fit:cover;flex-shrink:0}
.uitleg{display:grid;gap:10px}
.uitleg>div{display:flex;gap:12px;align-items:flex-start;background:#fbf6ef;border-radius:var(--rM);padding:14px}
.uitleg b{color:var(--brand);display:block;margin-bottom:2px}
.sh{display:flex;align-items:baseline;justify-content:space-between;gap:10px;margin:22px 0 12px}.sh h2{font-size:1.3rem}
@media(max-width:600px){
 .mwrap{padding:12px 12px 8px}.mh h1{font-size:1.3rem}.mh img{width:34px;height:34px}
 .mg{grid-template-columns:minmax(0,1fr);gap:14px}.mc .ph{height:190px}.mc h2{font-size:1.25rem}
 .bk{grid-template-columns:44px minmax(0,1fr);gap:12px;padding:16px}.bk .bi{width:44px;height:44px}.bk .side2{grid-column:1/-1;flex-direction:row;align-items:center;justify-content:space-between;width:100%}
 .bk h3{font-size:1.1rem}
 .oc{grid-template-columns:minmax(0,1fr)}
 .sh{margin:18px 0 10px;flex-direction:column;align-items:flex-start;gap:2px}.sh h2{font-size:1.15rem}
 .kvg{gap:10px 12px}
}
'''
def page(title,active,body,hero=None):
    nav=''.join(f'<span class="{"on" if t==active else ""}">{t}</span>' for t,_ in TABS)
    tab=''.join(f'<span class="{"on" if t==active else ""}"><i><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">{p}</svg></i>{t}</span>' for t,p in TABS)
    h=f'<div class="rustig-hero" aria-hidden="true"><img class="desk" src="img/hero-{hero}-desk.jpg" alt=""><img class="mob" src="img/hero-{hero}-mob.jpg" alt=""></div>' if hero else ''
    return f'''<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{title}</title><style>{CSS}{EXTRA}</style></head><body>
<header class="ah"><img src="lobsy-logo-horizontaal.svg" alt="Lobsy" style="height:34px"><span class="tag hide-m">Dichtbij genoeg om het pantser te laten vallen</span><nav>{nav}</nav><div class="ar"><span class="cb"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M6 16V11a6 6 0 0 1 12 0v5l2 2H4l2-2z"/><path d="M10 21h4"/></svg><i></i></span><span class="lng"><i class="flag"></i>NL</span><span class="who"><b>Sam Voorbeeld</b><span class="av">SV</span></span></div></header>{h}
{body}
<div class="chatdock"><span class="askl"><img src="lobsy-merkteken.svg" alt="">Vraag het Lobsy</span></div><footer class="lf"><div class="ll"><span>Privacyverklaring</span><span>Algemene voorwaarden</span><span>Toegankelijkheid</span><span>Bronnen</span></div><span>© 2026 Lobsy</span></footer><nav class="tabbar">{tab}</nav></body></html>'''

# 01 matchkaart
b1=f'''<main class="mwrap"><div class="mh"><img src="lobsy-merkteken.svg" alt=""><div><h1>Jouw top-matches</h1><p class="small muted">Vacature 3 van 12 · je kunt niets fout doen</p></div><span class="sp"></span><span class="btn line sm hide-m">{ic("map")}Op de kaart</span></div><div class="prog"><i></i></div>
<div class="mg"><div><div class="mc"><div class="ph"><img src="img/foto-zorg.jpg" alt=""><span class="fit">{ic("check",2.4)}Past goed bij jou</span></div><div class="bd">{VV}<h2>Verzorgende IG</h2><div class="org">Zorgcentrum Het Voorbeeld · Naaldwijk</div><div class="facts"><span>{ic("bike")}9 min fietsen</span><span>{ic("clock")}24 tot 32 uur</span><span>{ic("doc")}Vast contract</span></div><div class="tags"><span>Zorg</span><span>Overdag</span></div></div></div>
<div class="acts"><div><i>{ic("x")}</i>Laten schieten</div><div><i>{ic("info")}</i>Meer info</div><div class="hot"><i>{ic("heart")}</i>Snel kennismaken</div></div></div>
<div class="stack"><div class="card"><h2 style="font-size:1.2rem;margin-bottom:8px">Waarom jij past</h2><div class="why">
<div><span class="ok">{ic("check",2.4)}</span><div><b>Cultuur</b><span class="small muted">Een klein team dat rustig en warm met elkaar omgaat.</span></div></div>
<div><span class="ok">{ic("check",2.4)}</span><div><b>Interesses</b><span class="small muted">Je helpt graag mensen en werkt graag met je handen.</span></div></div>
<div><span class="ok">{ic("check",2.4)}</span><div><b>Reistijd en uren</b><span class="small muted">9 minuten fietsen en de uren passen bij jouw week.</span></div></div></div></div>
<div class="card"><h2 style="font-size:1.2rem;margin-bottom:12px">Meer info</h2><div class="kvg"><div><small>Reistijd</small><b>9 min fietsen</b></div><div><small>Uren</small><b>24 tot 32 uur</b></div><div><small>Begin</small><b>In overleg</b></div><div><small>Contract</small><b>Vast contract</b></div></div>
<div class="vvnote" style="margin-top:16px">{ic("trend",2.2)}<div><b style="color:var(--ok)">Veel vraag in jouw regio</b><br>Werkgevers in Haaglanden zoeken veel mensen voor dit beroep. Dat vergroot je kans op werk dichtbij.</div></div>
<div class="hr"></div><div class="row wrapflex" style="gap:16px"><span class="lk small">Bekijk de vacature</span><span class="lk small">Een dag als verzorgende</span></div></div></div></div></main>'''
open('01-matchkaart.html','w').write(page('Match','Zoeken',b1))

# 04 carriere
def bk(icn,name,fit,txt,meta,link=False,btn='Bekijk vacatures'):
    l=f'<div><span class="oml">{ic("cap")}Hier kun je in omscholen{ic("arrow",2)}</span></div>' if link else ''
    return f'''<div class="bk"><span class="bi">{ic(icn)}</span><div>{VV}<h3>{name}</h3><p>{txt}</p><div class="meta">{meta}</div>{l}</div><div class="side2"><span class="fitp">{fit}</span><span class="btn line sm">{ic("map")}{btn}</span></div></div>'''
b4=f'''<main class="wrap"><section class="intro"><nav class="bc" aria-label="Kruimelpad"><a>Carrière</a><i>›</i><span>Veel vraag in jouw regio</span></nav><h1>Veel vraag in Haaglanden en dicht bij jouw werk</h1><p>Beroepen die lijken op wat je nu doet als orderpicker en waar werkgevers in jouw regio veel mensen voor zoeken.</p></section>
<div class="g2"><div class="ber">
{bk("box","Magazijnmedewerker",f'{ic("check",2.4)}Past bij jouw ervaring',"Je kent het werk in een magazijn al. Hier doe je ook ontvangst, voorraad en verzending.",f'<span>{ic("map")}38 vacatures binnen 20 min fietsen</span><span>{ic("clock")}Vaak 32 tot 40 uur</span>')}
{bk("truck","Chauffeur bestelbus",f'{ic("check",2.4)}Past bij jouw ervaring',"Je brengt pakketten rond in de buurt. Je hebt alleen je rijbewijs B nodig.",f'<span>{ic("map")}21 vacatures binnen 20 min fietsen</span><span>{ic("clock")}Vaak 24 tot 40 uur</span>')}
{bk("wrench","Monteur warmtepompen",f'{ic("cap")}Met een opleiding',"Je werkt met je handen en bent veel onderweg. Je leert het vak via een opleiding of via leren en werken.",f'<span>{ic("map")}9 leerwerkplekken in de buurt</span>',True,'Bekijk leerwerkplekken')}
</div>
<aside class="card"><h2 style="font-size:1.15rem">Wat betekent dit?</h2><p class="small muted" style="margin-top:6px">Het groene label laat zien dat werkgevers in jouw regio veel mensen zoeken voor dat beroep. Het is goed nieuws, geen oordeel over jou.</p>
<div class="hr"></div><div class="list"><div><span class="ico green">{ic("trend",2.2)}</span><div class="sp"><b class="small" style="color:var(--brand)">Veel vraag in jouw regio</b><div class="xs muted">Volgens de lijst van UWV voor Haaglanden</div></div></div>
<div><span class="ico navy">{ic("cap")}</span><div class="sp"><b class="small" style="color:var(--brand)">Hier kun je in omscholen</b><div class="xs muted">Opleidingen en leren en werken voor dat beroep</div></div></div></div>
<div class="hr"></div><div class="row"><span class="ico">{ic("chat")}</span><p class="small"><span class="lk">Bespreek het met je adviseur</span><br><span class="muted">Neem deze beroepen mee naar je gesprek.</span></p></div>
<p class="src">Bron: UWV, regio Haaglanden. Voorbeelddata.</p></aside></div></main>'''
open('04-carriere.html','w').write(page('Carrière','Carrière',b4,'carriere'))

# 05 omscholen
def opl(t,sub,facts,cta):
    return f'<div class="ok2"><div class="top"><span class="ico navy">{ic("cap")}</span><div><h3>{t}</h3><div class="sub">{sub}</div></div></div><div class="facts">{facts}</div><div class="ft"><span class="lk small">Meer over deze opleiding</span><span class="btn line sm">{cta}</span></div></div>'
def bbl(t,org,facts):
    return f'<div class="ok2"><div class="top"><span class="ico" style="width:52px;height:52px">{ic("wrench")}</span><div><span class="bbl">{ic("book",2)}Leren en werken (BBL)</span><h3>{t}</h3><div class="sub">{org}</div></div></div><div class="facts">{facts}</div><div class="ft"><span class="lk small">Bewaren</span><span class="btn sm">Bekijk vacature</span></div></div>'
b5=f'''<main class="wrap"><section class="intro"><nav class="bc" aria-label="Kruimelpad"><a>Carrière</a><i>›</i><a>Workshops en opleidingen</a><i>›</i><span>Omscholen</span></nav>{VV}<h1 style="margin-top:8px">Omscholen tot monteur warmtepompen</h1><p>Je kunt een opleiding volgen, of leren en werken tegelijk. Kies wat bij jou past, in je eigen tempo.</p></section>
<div class="g2"><div>
<div class="sh" style="margin-top:0"><h2>Leren en werken (BBL)</h2><span class="small muted">4 plekken binnen 20 min fietsen</span></div>
<div class="oc">{bbl("Leerling monteur warmtepompen","Installatiebedrijf Voorbeeld · Rijswijk",f'<span>{ic("bike")}14 min</span><span>{ic("clock")}32 tot 38 uur</span><span>{ic("euro")}Met salaris</span>')}
{bbl("Leerling servicemonteur","Klimaattechniek Voorbeeld · Delft",f'<span>{ic("bike")}18 min</span><span>{ic("clock")}36 uur</span><span>{ic("euro")}Met salaris</span>')}</div>
<div class="sh"><h2>Opleidingen</h2><span class="small muted">In de buurt en online</span></div>
<div class="oc">{opl("Monteur duurzame installaties","Mbo niveau 2 · Techniekcollege Voorbeeld, Delft",f'<span>{ic("clock")}Ongeveer 2 jaar</span><span>{ic("pin")}Op locatie</span>',"Aanmelden")}
{opl("Basiscursus warmtepompen","Kort · Leerwerkplaats Voorbeeld, Den Haag",f'<span>{ic("clock")}8 dagdelen</span><span>{ic("pin")}Op locatie en online</span>',"Aanmelden")}</div>
</div>
<aside class="card"><h2 style="font-size:1.15rem">Goed om te weten</h2><div class="uitleg" style="margin-top:12px">
<div><span class="ico navy">{ic("info")}</span><p class="small"><b>Heb je een WW-uitkering?</b>Vraag UWV of je met behoud van uitkering een opleiding voor dit beroep mag volgen.</p></div>
<div><span class="ico">{ic("book")}</span><p class="small"><b>Leren en werken</b>Bij leren en werken (BBL) werk je en volg je een opleiding, met salaris.</p></div></div>
<div class="hr"></div><div class="row"><span class="ico">{ic("chat")}</span><p class="small"><span class="lk">Bespreek het met je adviseur</span><br><span class="muted">Samen kijken jullie wat bij je past.</span></p></div>
<p class="src">Voorbeelddata. Vraag altijd zelf na wat er voor jou geldt.</p></aside></div></main>'''
open('05-omscholen.html','w').write(page('Omscholen','Carrière',b5,'omscholen'))
