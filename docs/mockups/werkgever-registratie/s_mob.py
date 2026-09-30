from wr_ui import ic, vb, mob, cb, CK, em
from data import CO, DOMAIN, RESULTS
from s_flow import rows, scope, vlist
from s_profile import chips, sbi, sliders, vcards, VALS, eng_rows, ENG
from s_verify import methods, envelope, code_boxes

def m1():
    card = f'''<div class="toprow">{vb()}</div><h1 style="margin-top:8px">Vind je bedrijf</h1>
<div class="fld"><label class="lbl">KvK-nummer of bedrijfsnaam</label><div class="inp focus">{ic("building")}groen en zorg</div></div>
<div class="fld"><label class="lbl">Plaats <em>(optioneel)</em></label><div class="inp">{ic("pin")}Utrecht</div></div>
<div style="display:flex;align-items:center;gap:8px;margin-top:18px"><b class="sm">4 resultaten</b><span class="hint">uit KVK</span></div>
<div class="rlist">{rows(RESULTS[:3])}</div>'''
    return mob(1, "Hoi! Typ je KvK-nummer of zoek op naam. 👋", card, f'<a class="btn pri">Zoeken{ic("arrow")}</a>')

def m2():
    card = f'''<div class="toprow"><span class="pill">KvK 90123456</span>{vb()}</div><h1 style="margin-top:8px">{CO}</h1>
<p class="sm muted">Utrecht · 3 vestigingen</p>{scope("heel")}
<div class="sec2"><h3>Vestigingen</h3>{vlist("heel")}</div>
<div class="nb w" style="margin-top:12px"><i class="e">🔐</i><div><b>Zeist heeft al een beheerder</b><p>Werk je daar? Vraag toegang aan.</p></div></div>'''
    return mob(2, "Heel bedrijf of één vestiging? Allebei kan.", card, f'<a class="btn ghost">{ic("chevl")}</a><a class="btn pri">Verder{ic("arrow")}</a>')

def m3():
    card = f'''<div class="toprow"><span class="pill line">optioneel</span>{vb()}</div><h1 style="margin-top:8px">In welke branche werken jullie?</h1>
<p class="sm muted">Kies alles wat past. Twee hebben we al uit KVK gehaald.</p>{sbi()}{chips()}'''
    return mob(4, "Klopt dit? Tik gerust meer aan.", card, f'<a class="btn ghost">Later</a><a class="btn pri">Verder{ic("arrow")}</a>')

def m4():
    card = f'''<div class="toprow"><span class="pill line">optioneel · 1 min</span>{vb()}</div><h1 style="margin-top:8px">Zo werken wij</h1>
<div class="sec2" style="margin-top:14px"><h3>Hoe gaat het bij jullie?</h3>{sliders(4)}<p class="hint" style="margin-top:8px">+ 2 vragen</p></div>
<div class="sec2"><h3>Kies 3 kernwaarden <span class="cnt">3 van 3</span></h3>{vcards([VALS[2], VALS[5], VALS[8], VALS[3]])}</div>'''
    return mob(4, "Kandidaten doen dezelfde test. Wees eerlijk! 🎯", card, f'<a class="btn ghost">Later</a><a class="btn pri">Verder{ic("arrow")}</a>')

def m5():
    card = f'''<div class="toprow"><span class="pill line">optioneel</span>{vb()}</div><h1 style="margin-top:8px">Waar staan jullie voor?</h1>
<p class="sm muted">Wat we niet kunnen controleren, noemen we “door werkgever opgegeven”.</p>{eng_rows(ENG[1:4] + ENG[5:], proof=True)}'''
    return mob(4, "Iets goeds doen telt voor veel kandidaten.", card, f'<a class="btn ghost">Later</a><a class="btn pri">Verder{ic("arrow")}</a>')

def m6():
    card = f'''<div class="toprow">{vb()}</div><h1 style="margin-top:8px">Verifieer je bedrijf</h1>
<p class="sm muted">Tot die tijd ben je onzichtbaar voor kandidaten. Kies hoe:</p>{methods("post")}
<p class="hint" style="margin-top:12px">Lukt het niet? <a class="lnk">Handmatige controle aanvragen</a></p>'''
    return mob(5, "Laatste stap! Zo houden we Lobsy eerlijk. 🛡️", card, f'<a class="btn ghost">{ic("chevl")}</a><a class="btn pri">Stuur de brief{ic("arrow")}</a>')

def m7():
    card = f'''<div class="toprow"><span class="pill wn">Brief onderweg</span>{vb()}</div><h1 style="margin-top:8px">Vul je code in</h1>
<p class="sm muted">Verwacht do 1 of vr 2 okt op Vondellaan 12, Utrecht. Code geldig t/m 29 okt.</p>
<div style="margin-top:14px">{envelope()}</div>
<div class="fld"><label class="lbl">Code uit de brief</label>{code_boxes()}</div>
<div class="nb c" style="margin-top:14px"><i class="e">📭</i><div><b>Niet ontvangen?</b><p>Opnieuw versturen kan vanaf ma 5 okt (nog 2 keer).</p></div></div>'''
    return mob(5, "De postbode komt eraan! 📮", card, f'<a class="btn pri">Bedrijf verifiëren{ic("arrow")}</a>')
