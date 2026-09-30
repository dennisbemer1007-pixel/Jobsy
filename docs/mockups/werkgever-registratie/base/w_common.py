"""Warm-style helpers: emoji circles, organic blobs, waves, lobster mini-scenes and the two hero scenes."""
from ui import ic, MASCOT, LOBSTER, LOGO, vb

def em(e, tone="peach", size=""):
    return f'<span class="emo t-{tone} {size}" aria-hidden="true">{e}</span>'

def blob(fill, x, y, w, h, variant=0, rot=0):
    paths = ["M60,-60C78,-42,92,-21,92,0C92,21,78,42,60,58C42,74,21,84,-2,86C-25,88,-50,82,-66,66C-82,50,-90,25,-88,2C-86,-21,-74,-42,-58,-60C-42,-78,-21,-92,0,-92C21,-92,42,-78,60,-60Z",
             "M55,-68C70,-54,80,-35,84,-15C88,5,86,26,75,43C64,60,44,73,22,80C0,87,-24,88,-43,78C-62,68,-76,47,-83,24C-90,1,-90,-24,-79,-43C-68,-62,-46,-75,-24,-80C-2,-85,40,-82,55,-68Z",
             "M48,-58C62,-47,73,-31,79,-12C85,7,86,28,76,44C66,60,45,71,23,78C1,85,-22,88,-41,79C-60,70,-75,49,-82,26C-89,3,-88,-22,-76,-40C-64,-58,-41,-69,-19,-74C3,-79,34,-69,48,-58Z"]
    return (f'<svg class="blob" style="left:{x}px;top:{y}px;width:{w}px;height:{h}px" viewBox="-100 -100 200 200" preserveAspectRatio="none" aria-hidden="true">'
            f'<path d="{paths[variant % 3]}" fill="{fill}" transform="rotate({rot})"/></svg>')

def wave(top, bottom, flip=False):
    d = "M0,40 C180,90 360,0 540,30 C720,60 900,95 1080,55 C1260,15 1350,30 1440,45 L1440,100 L0,100 Z"
    return (f'<div class="wave-wrap" style="background:{top}"><svg class="wave" viewBox="0 0 1440 100" preserveAspectRatio="none" aria-hidden="true" style="{"transform:scaleY(-1)" if flip else ""}">'
            f'<path d="{d}" fill="{bottom}"/></svg></div>')

def mascot(x, y, w, extra="", cls=""):
    left = x if isinstance(x, str) else f"{x}px"
    return f'<img class="{cls}" src="{MASCOT}" alt="" style="left:{left};top:{y}px;width:{w}px;{extra}">'

def ghost(x, y, w, rot=-18, op=.09):
    return f'<img src="{MASCOT}" alt="" style="left:{x}px;top:{y}px;width:{w}px;transform:rotate({rot}deg);filter:brightness(0);opacity:{op}">'

SCENES = {
 "dive": ('var(--sky)', '<path d="M0 34 C 40 20, 80 48, 120 34 S 200 20, 400 34" fill="none" stroke="var(--surface)" stroke-width="3" opacity=".8"/>'
          + "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="none" stroke="var(--surface)" stroke-width="2"/>' for x, y, r in [(150, 46, 5), (162, 30, 3.5), (140, 22, 3), (230, 60, 4)]),
          [(-20, 34, 70, "transform:rotate(128deg)")]),
 "antenna": ('var(--sun)', "".join(f'<path d="M{190-r} 58 A {r} {r} 0 0 1 {190+r} 58" fill="none" stroke="var(--gold)" stroke-width="2.5" stroke-linecap="round" opacity="{o}"/>' for r, o in [(24, .9), (38, .6), (52, .35)]),
          [(0, 40, 64, "")]),
 "rock": ('var(--mint)', '<path d="M110 118 C 120 84, 150 70, 190 72 C 236 74, 262 92, 272 118 Z" fill="color-mix(in srgb, var(--muted) 22%, var(--surface))"/>', [(0, 14, 62, "")]),
 "claw": ('var(--peach)', "".join(f'<text x="{x}" y="{y}" font-size="{s}" font-family="Noto Color Emoji">✨</text>' for x, y, s in [(120, 40, 20), (236, 34, 16), (222, 96, 14)]),
          [(0, 22, 76, "transform:scaleX(-1)")]),
 "grow": ('var(--cream)', '<path d="M60 108 L 320 108" stroke="var(--sun-2)" stroke-width="3" stroke-linecap="round"/>', [(-94+19, 70, 38, ""), (-40+28, 50, 56, ""), (24+41, 24, 82, "")]),
 "path": ('var(--sky)', '<path d="M30 100 C 90 40, 150 110, 210 60 S 300 30, 330 44" fill="none" stroke="var(--brand)" stroke-width="2.5" stroke-dasharray="3 7" stroke-linecap="round" opacity=".5"/>'
          '<text x="318" y="46" font-size="22" font-family="Noto Color Emoji">🚩</text>', [(-72+29, 40, 58, "transform:rotate(-8deg)")]),
}

def mini_scene(kind):
    bg, svg, ms = SCENES[kind]
    imgs = "".join(mascot(f"calc(50% + {dx - w/2:.0f}px)", y, w, extra) for dx, y, w, extra in ms)
    return f'<div class="msc" style="background:{bg}"><svg viewBox="0 0 380 118" preserveAspectRatio="xMidYMid slice" aria-hidden="true">{svg}</svg>{imgs}</div>'
