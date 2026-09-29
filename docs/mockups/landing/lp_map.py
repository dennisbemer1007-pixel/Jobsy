from ui import ic, LOGO

def mapsvg(w=860, h=460, cx=400, cy=235, scale=1.0, home=True):
    s = scale
    water = f'<path d="M0 {h*.78} C {w*.2} {h*.7}, {w*.35} {h*.9}, {w*.55} {h*.84} S {w*.85} {h*.7}, {w} {h*.76} V {h} H 0 Z" fill="var(--accent-soft)"/>'
    parks = "".join(f'<rect x="{x}" y="{y}" width="{a}" height="{b}" rx="18" fill="var(--success-soft)"/>' for x, y, a, b in [(w*.08, h*.12, 130*s, 80*s), (w*.66, h*.18, 110*s, 70*s), (w*.28, h*.56, 90*s, 60*s)])
    greens = "".join(f'<rect x="{w*x}" y="{h*y}" width="{w*a}" height="{h*b}" fill="color-mix(in srgb, var(--pearl-mid) 70%, transparent)"/>' for x, y, a, b in [(.0,.35,.2,.18),(.6,.5,.25,.15),(.42,.05,.18,.14)])
    roads = "".join(f'<path d="{d}" fill="none" stroke="var(--surface)" stroke-width="{sw}" stroke-linecap="round"/>' for d, sw in [
        (f"M0 {h*.3} C {w*.3} {h*.25}, {w*.6} {h*.45}, {w} {h*.38}", 9*s), (f"M{w*.3} 0 C {w*.38} {h*.4}, {w*.5} {h*.6}, {w*.52} {h}", 9*s),
        (f"M{w*.75} 0 L {w*.62} {h*.9}", 6*s), (f"M0 {h*.6} L {w} {h*.6}", 5*s), (f"M{w*.12} 0 L {w*.2} {h*.8}", 5*s)])
    rings = "".join(f'<circle cx="{cx}" cy="{cy}" r="{r*s}" fill="color-mix(in srgb, var(--coral) {f}%, transparent)" stroke="var(--coral)" stroke-opacity=".6" stroke-width="1.5" stroke-dasharray="{da}"/>'
                    for r, f, da in [(215, 3, "3 7"), (145, 4, "3 7"), (78, 8, "0")])
    pins = "".join(f'<g transform="translate({x},{y}) scale({s})"><path d="M0 0c-7-8-11-13-11-18a11 11 0 0 1 22 0c0 5-4 10-11 18z" fill="var(--map-pin)" stroke="var(--surface)" stroke-width="2"/><circle cx="0" cy="-18" r="4" fill="var(--surface)"/></g>'
                   for x, y in [(cx-60*s, cy-40*s), (cx+105*s, cy-70*s), (cx+40*s, cy+110*s), (cx-170*s, cy+30*s), (cx+190*s, cy+40*s), (cx-110*s, cy-150*s), (cx+250*s, cy-140*s), (cx-270*s, cy-90*s)])
    hm = f'<circle cx="{cx}" cy="{cy}" r="{16*s}" fill="var(--brand)" stroke="var(--surface)" stroke-width="3"/><path transform="translate({cx-8*s},{cy-8*s}) scale({.66*s})" d="M4 11 12 4l8 7v9H4z" fill="none" stroke="var(--surface)" stroke-width="2.2" stroke-linejoin="round"/>'
    return f'<svg viewBox="0 0 {w} {h}" preserveAspectRatio="xMidYMid slice" aria-hidden="true">{greens}{parks}{water}{roads}{rings}{pins}{hm if home else ""}</svg>'
