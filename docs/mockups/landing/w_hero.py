from ui import ic, MASCOT, LOBSTER, vb
from w_common import blob, mascot, ghost, em
from zw_scene import passport

def _disc(cx, cy, R):
    rings = "".join(f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="none" stroke="var(--coral)" stroke-opacity="{o}" stroke-width="2" stroke-dasharray="{d}"/>' for r, o, d in [(R*.95, .35, "3 8"), (R*.66, .45, "3 8"), (R*.38, .6, "0")])
    roads = "".join(f'<path d="{d}" stroke="var(--surface)" stroke-width="7" fill="none" stroke-linecap="round"/>' for d in [f"M{cx-R} {cy+20} C {cx-R/2} {cy-30}, {cx+R/2} {cy+40}, {cx+R} {cy-10}", f"M{cx-40} {cy-R} C {cx-10} {cy-R/3}, {cx+30} {cy+R/3}, {cx+10} {cy+R}"])
    pins = "".join(f'<circle cx="{cx+dx}" cy="{cy+dy}" r="7" fill="var(--map-pin)" stroke="var(--surface)" stroke-width="3"/>' for dx, dy in [(-120, -60), (95, -110), (130, 60), (-90, 110), (-160, 30), (40, 150), (160, -30)])
    return (f'<svg class="blob" style="left:0;top:0;width:100%;height:100%" viewBox="0 0 580 540" aria-hidden="true"><defs><clipPath id="dc"><circle cx="{cx}" cy="{cy}" r="{R}"/></clipPath></defs>'
            f'<circle cx="{cx}" cy="{cy}" r="{R}" fill="var(--surface)"/><g clip-path="url(#dc)"><rect x="{cx-R}" y="{cy+R*.45}" width="{2*R}" height="{R}" fill="var(--sky)"/>'
            f'<circle cx="{cx-R*.5}" cy="{cy-R*.45}" r="{R*.22}" fill="var(--mint)"/>{roads}</g>{rings}{pins}</svg>')

def hero_scene(zw=False):
    b = blob("var(--sun)", 40, 10, 540, 520, 0) + blob("var(--peach)", 400, 330, 210, 190, 1, 30) + blob("var(--sky)", -10, 330, 170, 150, 2)
    if not zw:
        inner = (_disc(300, 270, 205) + ghost(40, 110, 170) + '<span class="shell-tag" style="left:28px;top:300px">je oude schild</span>'
                 + mascot(190, 150, 230, "", "hm") + '<div class="bubble" style="left:250px;top:84px">Hoi! Zin om te groeien? <i class="e">👋</i></div>'
                 + f'<div class="chip" style="left:0;top:14px"><i class="e" style="font-size:20px">🚲</i><div>Zorgmedewerker<small>12 min fietsen · Naaldwijk</small></div></div>'
                 + f'<div class="chip" style="right:-10px;top:210px"><i class="e" style="font-size:20px">🚌</i><div>Monteur<small>24 min met de bus · Delft</small></div></div>'
                 + f'<div class="dib" style="right:10px;bottom:0"><h4><i class="e">🦞</i>Dit ben jij {vb()}</h4><div class="bar">Samenwerken<b style="--w:86%"></b></div><div class="bar t2">Doorzetten<b style="--w:74%"></b></div><div class="bar t3">Rust onder druk<b style="--w:61%"></b></div></div>')
    else:
        inner = (ghost(10, 40, 180, -24, .08) + '<span class="shell-tag" style="left:0;top:4px">je oude schild</span>'
                 + f'<div class="zpp" style="left:215px;top:92px;transform:rotate(3deg)">{passport()}</div>'
                 + mascot(40, 250, 220, "", "hm") + '<div class="bubble" style="left:40px;top:196px">Kijk, dit ben ik! <i class="e">✨</i></div>'
                 + '<div class="chip" style="right:-10px;top:26px"><i class="e" style="font-size:20px">✅</i><div>Past dit beroep bij mij?<small>Verpleegkundige · 82% fit</small></div></div>'
                 + '<div class="chip" style="right:20px;bottom:24px"><i class="e" style="font-size:20px">🧭</i><div>Ontdekkingsreis<small>Stap 3 van 6 · Hier voel je je thuis</small></div></div>')
    return f'<div class="wscene">{b}{inner}</div>'

def hero_scene_m(zw=False):
    b = blob("var(--sun)", 30, 0, 340, 320, 0) + blob("var(--peach)", 250, 200, 140, 120, 1)
    if not zw:
        inner = (ghost(10, 50, 110) + mascot(120, 70, 160, "", "hm") + '<div class="bubble" style="left:150px;top:14px;font-size:14px;padding:8px 12px">Hoi! <i class="e">👋</i></div>'
                 + '<div class="chip" style="left:12px;top:206px"><i class="e">🚲</i><div>Zorgmedewerker<small>12 min fietsen</small></div></div>'
                 + '<div class="chip" style="right:12px;top:150px"><i class="e">🚌</i><div>Monteur<small>24 min bus</small></div></div>'
                 + f'<span class="vb" style="position:absolute;right:14px;top:14px">Voorbeelddata</span>')
    else:
        inner = (ghost(0, 30, 110, -24, .08) + f'<div class="zpp" style="left:138px;top:44px;transform:rotate(3deg)">{passport(w=230, wheel_size=78, compact=True)}</div><span class="vb" style="position:absolute;right:14px;bottom:10px">Voorbeelddata</span>'
                 + mascot(10, 140, 150, "", "hm") + '<div class="bubble" style="left:18px;top:98px;font-size:14px;padding:8px 12px">Dit ben ik! <i class="e">✨</i></div>')
    return f'<div class="wscene m">{b}{inner}</div>'
