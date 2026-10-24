from common import *
css='''
.pill--ext{background:#efeaf6;color:#5b3f8c;border-color:#ddd2ec}
.sol-actions{display:flex;justify-content:space-between;align-items:center;gap:10px;flex-wrap:wrap;margin-bottom:14px}
.sol-actions .chips{margin:0}
@media (max-width:900px){.sol-actions .btn{width:100%;justify-content:center}}
.sol-top{display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;margin-bottom:14px}
.week-left{display:flex;align-items:center;gap:8px;font-size:.84rem;font-weight:700;background:#fff;border:1px solid var(--line);border-radius:999px;padding:6px 12px}
.app{display:grid;grid-template-columns:44px 1fr auto;gap:14px;align-items:start;text-decoration:none;color:var(--text)}
.app.is-open{border-color:#c9b9a3;box-shadow:0 0 0 3px #efe7da}
.app h3{margin:6px 0 2px;font-size:1rem}
.app .c{margin:0;font-size:.86rem;color:var(--muted)}
.app .s{margin:8px 0 0;font-size:.84rem;color:var(--muted)}
.app .chev{color:var(--muted);width:20px;margin-top:12px}
.steps{list-style:none;margin:10px 0 14px;padding:0}
.steps li{display:grid;grid-template-columns:24px 1fr;gap:10px;position:relative;padding-bottom:14px}
.steps li::before{content:"";position:absolute;left:11px;top:22px;bottom:0;width:2px;background:var(--line)}
.steps li:last-child::before{display:none}
.dot{width:24px;height:24px;border-radius:50%;border:2px solid var(--line);background:#fff;display:grid;place-items:center}
.dot.done{background:var(--success);border-color:var(--success);color:#fff}.dot.done svg{width:13px}
.dot.now{border-color:var(--brand);box-shadow:inset 0 0 0 4px #fff;background:var(--brand)}
.steps b{display:block;font-size:.92rem}.steps span{font-size:.82rem;color:var(--muted)}
.acts{display:grid;gap:8px}
.acts .btn{width:100%}
.legend-q{font-size:.86rem}
@media (max-width:900px){.detail{display:none}.app{grid-template-columns:44px 1fr 20px}}
'''
def card(ini,kind,title,comp,status,cls,line,open_=False,href='#'):
    k={'i':'<span class="pill pill--init">Ik wil hier werken</span>','e':'<span class="pill pill--ext">Buiten Lobsy</span>'}.get(kind,'<span class="pill">Op vacature</span>')
    return f'''<a class="card app{' is-open' if open_ else ''}" href="{href}"><span class="logo-sq">{ini}</span><div>{k}<h3>{title}</h3><p class="c">{comp}</p><p class="s"><span class="pill {cls}">{status}</span>&nbsp; {line}</p></div><span class="chev">{I('chev')}</span></a>'''
def tabs(active):
    t=[('Sollicitaties','/candidate/applications'),('Mijn activiteiten','/candidate/applications/activiteiten'),('Bewaard','/candidate/bewaard')]
    return '<nav class="tabs" aria-label="Onderdelen">'+''.join(f'<a href="{h}"{" class=\"is-active\"" if n==active else ""}>{n}{" <span class=\"pill\" style=\"padding:1px 8px;margin-left:4px\">4</span>" if n=="Bewaard" else ""}</a>' for n,h in t)+'</nav>'
HEAD='<div class="head"><p class="eyebrow">Sollicitaties</p><h1>Mijn sollicitaties</h1><p class="lead">Op vacatures en bij bedrijven waar je zelf aanklopte. Alles op één plek.</p></div>'
if __name__=='__main__':
    body=HEAD+f'''
<div class="sol-top">{tabs('Sollicitaties')}<span class="week-left">{I('people').replace('<svg','<svg width="16" height="16"')} Nog 2 van de 3 keer ‘Ik wil hier werken’ deze week</span></div>
<div class="sol-actions"><div class="chips"><a class="chip is-active" href="?filter=alles">Alles · 7</a><a class="chip" href="?filter=vacature">Op vacature · 3</a><a class="chip" href="?filter=initiatief">Ik wil hier werken · 3</a><a class="chip" href="?filter=buiten">Buiten Lobsy · 1</a><a class="chip" href="?filter=afgerond">Afgerond · 1</a></div><a class="btn btn--primary" href="/candidate/external/add">{I('plus').replace('<svg','<svg width="16" height="16"')} Sollicitatie toevoegen</a></div>
<div class="grid-side">
<div class="stack">
 {card('GH','i','Team Groen en onderhoud','Groenland Hoveniers · Rijswijk','Gesprek gepland','pill--ok','Kennismaking dinsdag 27 oktober, 10:30',True,'/candidate/applications/1')}
 {card('BK','v','Medewerker bakkerij','Bakkerij De Korenaar · Delft','Gezien','pill--blue','Verstuurd 13 oktober · werkgever heeft je cv bekeken',False,'/candidate/applications/2')}
 {card('WL','v','Magazijnmedewerker','Westhaven Logistiek · Rijswijk','Verstuurd','pill','Verstuurd 20 oktober · reactie uiterlijk 3 november',False,'/candidate/applications/3')}
 {card('TH','e','Medewerker tuincentrum','Tuincentrum De Hoven · Den Hoorn','Verstuurd','pill','Toegevoegd 22 oktober · je solliciteerde via de site van de werkgever',False,'/candidate/applications/7')}
 {card('WH','i','Algemene ingang','Woonzorg Haaglanden · Delft','Verstuurd','pill','Verstuurd vandaag · reactie uiterlijk 30 oktober',False,'/candidate/applications/4')}
 {card('TD','i','Team Service en onderhoud','Techniek Delft · Delft','Nu niet','pill--warn','Ze bewaren je profiel, je hoort het als er plek is',False,'/candidate/applications/5')}
 {card('FS','v','Medewerker fietsenwerkplaats','Fietsdokter Schie · Schiedam','Afgerond','pill','Vacature gesloten op 9 oktober',False,'/candidate/applications/6')}
</div>
<aside class="card detail">
 <span class="pill pill--init">Ik wil hier werken</span>
 <h3 style="margin:8px 0 2px">Team Groen en onderhoud</h3>
 <div class="muted small">Groenland Hoveniers · Rijswijk</div>
 <ol class="steps">
  <li><span class="dot done">{I('check')}</span><div><b>Verstuurd</b><span>woensdag 7 oktober</span></div></li>
  <li><span class="dot done">{I('check')}</span><div><b>Gezien</b><span>donderdag 8 oktober</span></div></li>
  <li><span class="dot now"></span><div><b>Gesprek</b><span>dinsdag 27 oktober, 10:30</span></div></li>
  <li><span class="dot"></span><div><b>Uitkomst</b><span>Je hoort het na het gesprek</span></div></li>
 </ol>
 <div class="note" style="margin-bottom:14px"><span>Wat kun je nu doen? Oefen je gesprek en lees de tips voor een kennismaking.</span></div>
 <div class="acts">
  <a class="btn btn--primary" href="/candidate/na-de-match/gesprek">Bereid je gesprek voor</a>
  <a class="btn" href="/candidate/applications/1/bericht">Je bericht bekijken</a>
  <a class="btn" href="/bedrijf/groenland-hoveniers">Bedrijfspagina</a>
  <a class="btn" href="/candidate/applications/1/cv">Lobsy-cv bekijken</a>
  <a class="btn btn--danger" href="/candidate/applications/1/intrekken">Sollicitatie intrekken</a>
 </div>
</aside>
</div>
<p class="muted small" style="margin-top:16px">Tik op een sollicitatie om alle stappen te zien. Nog niets gevonden? <a href="/banenkaart">Naar de banenkaart</a></p>
'''
    open('/workspace/kmn-src/sollicitaties.html','w').write(page('Sollicitaties','Sollicitaties','hero-sollicitaties.jpg','center 46%','20% 55%',body,'auto 240%',css))
