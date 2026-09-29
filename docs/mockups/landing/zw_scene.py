"""Hero visual for the 'zonder werkgevers' variant: the lobster steps out of its old shell into its passport (DNA wheel)."""
import math
from ui import ic, LOBSTER, LOGO, vb

SEG = [("Zo werk jij", 86, "var(--coral)"), ("Dit vind je leuk", 78, "var(--gold)"), ("Hier voel je je thuis", 64, "var(--success)"), ("Dit vind je belangrijk", 71, "var(--brand)")]

def wheel(size=150, stroke=16, center=True):
    stroke = min(stroke, max(10, round(size * .13)))
    r = (size - stroke) / 2; c = size / 2; C = 2 * math.pi * r; q = C / 4; gap = 6
    out = []
    for i, (_, v, mix) in enumerate(SEG):
        col = mix
        rot = -90 + i * 90
        out.append(f'<circle cx="{c}" cy="{c}" r="{r}" fill="none" stroke="var(--pearl-mid)" stroke-width="{stroke}" stroke-dasharray="{q-gap:.1f} {C:.1f}" transform="rotate({rot} {c} {c})"/>')
        out.append(f'<circle cx="{c}" cy="{c}" r="{r}" fill="none" stroke="{col}" stroke-width="{stroke}" stroke-linecap="round" stroke-dasharray="{(q-gap)*v/100:.1f} {C:.1f}" transform="rotate({rot} {c} {c})"/>')
    mid = (f'<text x="{c}" y="{c-2}" text-anchor="middle" font-size="{max(12, size*.1):.0f}" font-weight="600" fill="var(--text)">Dit ben jij</text>'
           f'<text x="{c}" y="{c+size*.1:.0f}" text-anchor="middle" font-size="{max(12, size*.08):.0f}" fill="var(--muted)">4 kanten</text>') if center else ""
    return f'<svg class="wheel" width="{size}" height="{size}" viewBox="0 0 {size} {size}" aria-hidden="true">{"".join(out)}{mid}</svg>'

def legend(compact=False):
    rows = "".join(f'<div class="lg-r"><i style="background:{m}"></i><span>{n}</span><b>{v}%</b></div>' for n, v, m in (SEG[:2] if compact else SEG))
    return f'<div class="dlg">{rows}</div>'

def passport(w=360, wheel_size=140, compact=False):
    if compact:
        chips = "".join(f'<span class="pill">{t}</span>' for t in ["Samenwerker", "Mensen helpen", "Warm team"])
        return f'''<div class="ppc" style="width:{w}px"><div class="ppc-h"><img src="{LOGO}" alt=""><b>Mijn Paspoort</b></div>
<div class="ppc-b"><div class="ppc-g">{wheel(wheel_size, center=wheel_size >= 90)}<div class="ppc-cc">{chips}</div></div></div></div>'''
    chips = "".join(f'<span class="pill">{t}</span>' for t in ["Samenwerker", "Mensen helpen", "Warm team"])
    return f'''<div class="ppc" style="width:{w}px"><div class="ppc-h"><img src="{LOGO}" alt=""><b>Mijn Paspoort</b><span class="sp"></span>{vb()}</div>
<div class="ppc-b"><div class="ppc-g">{wheel(wheel_size)}{legend(compact)}</div><div class="ppc-c">{chips}</div></div></div>'''

def fragments(pts):
    return "".join(f'<path d="M{x} {y} q 10 -14 24 -4" fill="none" stroke="color-mix(in srgb, var(--surface) 22%, transparent)" stroke-width="3" stroke-linecap="round" transform="rotate({a} {x} {y})"/>' for x, y, a in pts)

def scene_desktop():
    return f'''<div class="zscene"><svg class="zbg" viewBox="0 0 580 540" aria-hidden="true">
<path d="M150 190 C 150 260, 120 300, 150 350" fill="none" stroke="color-mix(in srgb, var(--surface) 35%, transparent)" stroke-width="1.5" stroke-dasharray="4 6"/>{fragments([(60, 260, 20), (210, 60, -30), (40, 420, 60), (520, 480, 10), (230, 470, -15)])}</svg>
<img class="zold" src="{LOBSTER}" alt=""><span class="zold-l">je oude schild</span>
<div class="zpp">{passport()}</div>
<img class="znew" src="{LOBSTER}" alt="Lobsy, de kreeft">
<div class="chip" style="right:-6px;top:6px">{ic("check")}<div>Past dit beroep bij mij?<small>Verpleegkundige · 82% fit</small></div></div>
<div class="chip" style="right:18px;bottom:30px">{ic("compass")}<div>Ontdekkingsreis<small>Stap 3 van 6 · Hier voel je je thuis</small></div></div>
</div>'''

def scene_mobile():
    return f'''<div class="zscene m-z"><svg class="zbg" viewBox="0 0 390 330" aria-hidden="true">{fragments([(30, 200, 20), (150, 30, -30), (360, 300, 10)])}</svg>
<img class="zold" src="{LOBSTER}" alt=""><div class="zpp">{passport(w=250, wheel_size=96, compact=True)}</div><img class="znew" src="{LOBSTER}" alt="Lobsy, de kreeft">
<div class="chip" style="right:10px;bottom:14px">{ic("check")}<div>Verpleegkundige<small>82% fit met jou</small></div></div></div>'''
