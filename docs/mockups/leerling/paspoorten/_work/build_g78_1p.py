exec(open('_work/build_g78.py').read().split('NOTES=')[0])
def top():
    return '''<header class="top"><div class="brand"><img src="img/g78-embleem.png" alt="">Lobsy<small>Expeditie · DNA-paspoort</small></div><div class="sp"></div>
<div class="meta"><b>Groep 8 · 8A</b> · OBS De Kompasroos (fictief)<br>14 okt 2026<span class="code">K7Q-M2P</span></div></header>'''
FOOT=f'''<footer class="foot"><span class="shi">{I['shield']}</span><div class="sp">Geen cijfer en geen schooladvies. Dit paspoort is om te ontdekken, voor jou, je ouders en je leraar.<br>Lobsy bewaart geen namen. Je antwoorden worden na dit schooljaar gewist.</div><div class="r"><b>lobsy.nl</b>14 okt 2026</div></footer>'''
LINE='<span style="display:inline-block;width:22mm;border-bottom:1px solid #cbbd9f"></span>'
NOTES1='<div class="card" style="margin-top:2.2mm;border-style:dashed;background:transparent"><h3 class="ct">Ruimte voor jou <small class="muted" style="font-family:Lexend;font-weight:400;font-size:8.6pt;margin-left:1.6mm">Schrijf of teken hier wat je onderweg over jezelf ontdekte.</small></h3><div class="empty" style="height:20mm"></div></div>'
def build(d,fn):
    worlds=''.join(f'<div class="w"><span class="ok">{CHK}</span><div class="im" style="background-image:url(img/g78-{k}.jpg)"><span>{t}</span></div><div class="t"><div class="k">{sub}</div><h3>{val}</h3></div></div>' for k,t,sub,val in WORLDS)
    likes=''.join(f'<span class="chip on">{x}</span>' for x in d['likes'])
    dis=''.join(f'<span class="chip no">{x}</span>' for x in d['dis']) or '<span class="muted">Nog niets gekozen. Dat mag ook.</span>'
    lo=f'<div class="own">Mijn eigen woord:<b>{d["like_own"]}</b></div>' if d['like_own'] else f'<div class="own">Mijn eigen woord: {LINE}</div>'
    do=f'<div class="own">Mijn eigen woord:<b>{d["dis_own"]}</b></div>' if d['dis_own'] else f'<div class="own">Mijn eigen woord: {LINE}</div>'
    st=''.join(f'<div class="s"><span class="ic">{I[i]}</span><div><b>{t}.</b> <span>{s}</span></div></div>' for i,t,s in d['strengths'])
    if len(d['strengths'])<3:
        st+='<div class="s"><span class="ic" style="background:transparent;border:1px dashed var(--line)"></span><div style="flex:1"><span class="muted">Hier ben ik ook goed in:</span><div class="wl" style="height:5mm"></div></div></div>'
    if d['dream']:
        route=''.join(f'<div class="st{" goal" if i==3 else ""}"><div class="hd"><i>{STAR if i==3 else i+1}</i><span class="k">{k}</span></div><b>{t}{"!" if i==3 else ""}</b><span>{s}</span></div>' for i,(k,t,s) in enumerate(d['route']))
        have=''.join(f'<li>{x}</li>' for x in d['have']); need=''.join(f'<li>{x}</li>' for x in d['need'])
        dream=f'''<div class="card dream"><div class="head"><span class="hand">Hier droom ik van...</span><h3 class="job">{d['dream']}</h3><span class="bub">Dierenarts? Sterke keuze. Dit is je route.</span><img class="lob" src="img/g78-kreeft-kaart.png" alt=""></div>
<div class="route">{route}</div>
<div class="have"><div><h4>{I['okc']}Dit heb je al</h4><ul>{have}</ul></div><div><h4>{I['arrow']}Wat heb je nodig?</h4><ul>{need}</ul></div><div class="alt">{d['alt']}</div></div></div>'''
    else:
        dream='''<div class="card dream"><div class="head"><span class="hand">Hier droom ik van...</span><b class="muted" style="font-size:10pt">Nog geen droombaan gekozen. Dat is helemaal goed.</b><span class="bub">Weet je het nog niet? Dan ontdek je het onderweg.</span><img class="lob" src="img/g78-kreeft-kaart.png" alt=""></div>
<p>Kies er een in de Droombaan-checker, of schrijf hier zelf op wat je later misschien wilt worden. Praat erover met je ouders of je leraar.</p><div class="empty"></div></div>'''
    jobs=''.join(f'<div class="card job"><span class="ico">{I[i]}</span><div class="k">{k}</div><h3>{t}</h3><p>{s}</p><div class="fit"><b>Past bij:</b> {d["fit"]}</div></div>' for i,k,t,s in d['jobs'])
    if len(d['jobs'])<3:
        jobs+=f'<div class="card job" style="border-style:dashed;background:transparent;grid-column:span {3-len(d["jobs"])}"><div class="k">Nog meer ontdekken</div><p class="muted" style="margin-top:.8mm">Op de Droombaan-pagina vind je meer beroepen. Schrijf er hier een op die je wilt bekijken:</p><div class="wl" style="margin-top:2.4mm"></div><div class="wl"></div></div>'
    disc=''.join(f'<li><span class="box"></span>{x}</li>' for x in d['discover']) if d['discover'] else ''.join('<li><span class="box"></span><span class="wl"></span></li>' for _ in range(3))
    body=f'''<section class="page">{top()}<div class="strip"></div>
<div class="wrap"><div class="title"><div class="t1"><div class="kicker">Expeditie klaar · alle 60 vragen</div><h1>Mijn DNA-paspoort</h1>
</div><div class="t2"><div class="nameline">Dit paspoort is van<span class="ln">{d['name']}</span></div><div class="note">Je naam schrijf je zelf op. Lobsy bewaart geen namen.</div></div>
<img class="kijk" src="img/g78-kreeft-kijker.png" alt=""><div class="badge"><div class="sh">{CHK}</div><div><b>Schild compleet</b><span>Alle 4 werelden ontdekt</span></div></div></div>
<div class="card story" style="margin-top:2.2mm"><div><h3 class="ct">Dit ben ik! <small class="who" style="margin-left:1.6mm">Wat Lobsy onderweg leerde</small></h3><p>{d['story']}</p></div></div>
<div class="worlds">{worlds}</div>
<div class="g3" style="margin-top:2.2mm"><div class="card like"><h3 class="ct">Jaaa, leuk!</h3><div class="chips">{likes}</div>{lo}</div>
<div class="card dis"><h3 class="ct">Liever niet</h3><div class="chips">{dis}</div>{do}</div>
<div class="card strc"><h3 class="ct">Hier ben ik goed in</h3>{st}</div></div>
<div style="height:2.2mm"></div>{dream}
<h2>Mijn beroepenkaarten <small>om te ontdekken, geen vacatures</small></h2><div class="jobs">{jobs}</div>
<div class="g2" style="margin-top:2.4mm;grid-template-columns:1.2fr 1fr"><div class="card disc"><h3 class="ct">Dit wil ik nog ontdekken</h3><ul>{disc}</ul></div>
<div class="card tip"><h3 class="ct">Tip voor thuis en in de klas</h3><p>Ga samen naar een kinderboerderij of dierenasiel en vraag wat de verzorgers doen.</p><p>Vraag wat het leukst was. Er is geen goed of fout!</p></div></div>
{NOTES1 if not d['dream'] else ''}</div>{FOOT}</section>'''
    open('src/'+fn,'w').write(f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><title>DNA-paspoort groep 7/8</title><link rel="stylesheet" href="g78.css"></head><body>{body}</body></html>')
build(FULL,'groep78-noor.html'); build(SPARSE,'groep78-sparse.html')
