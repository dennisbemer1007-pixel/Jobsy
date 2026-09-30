"""De ontdekkingsreis: mockups (desktop 1440x900, mobile 390x844).
Style, header, tokens and cards match ../dna-paspoort/build.py.
Scene colours are only color-mix() of existing tokens (no new hex values).
Run: python3 build.py   (needs playwright + /usr/bin/google-chrome)"""
import pathlib, base64, math, random
from playwright.sync_api import sync_playwright
d = pathlib.Path(__file__).parent; s = d / "src"
def b64(p, mt): return f"data:{mt};base64," + base64.b64encode(p.read_bytes()).decode()
MASCOT = b64(s / "mascot-256.webp", "image/webp"); LOGO = b64(s / "lobsy-128.png", "image/png")

P = dict(  # same line icons as dna-paspoort
 search='<circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/>',
 heart='<path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.6l-1-1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8z"/>',
 clip='<rect x="8" y="2" width="8" height="4" rx="1"/><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/><path d="m9 14 2 2 4-4"/>',
 sun='<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/>',
 user='<circle cx="12" cy="8" r="4"/><path d="M4 21v-1a6 6 0 0 1 6-6h4a6 6 0 0 1 6 6v1"/>',
 bell='<path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9"/><path d="M10 21a2 2 0 0 0 4 0"/>',
 chevd='<path d="m6 9 6 6 6-6"/>', right='<path d="m9 6 6 6-6 6"/>', left='<path d="m15 6-6 6 6 6"/>', check='<path d="m5 12 5 5 9-10"/>',
 pin='<path d="M12 21s-7-6.2-7-11.5A7 7 0 0 1 19 9.5C19 14.8 12 21 12 21z"/><circle cx="12" cy="9.5" r="2.5"/>',
 clock='<circle cx="12" cy="12" r="8"/><path d="M12 8v4l3 2"/>', edit='<path d="M4 20h4L19 9l-4-4L4 16z"/>', plus='<path d="M12 5v14M5 12h14"/>',
 brief='<rect x="3" y="7" width="18" height="13" rx="2"/><path d="M9 7V5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2M3 13h18"/>',
 lock='<rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/>',
 wave='<path d="M3 8c3-2 6 2 9 0s6 2 9 0M3 13c3-2 6 2 9 0s6 2 9 0M3 18c3-2 6 2 9 0s6 2 9 0"/>',
 book='<path d="M5 4h11a3 3 0 0 1 3 3v13H8a3 3 0 0 1-3-3z"/><path d="M5 17a3 3 0 0 1 3-3h11"/><circle cx="12" cy="8.5" r="2"/>',
 compass='<circle cx="12" cy="12" r="9"/><path d="m15.5 8.5-2 5-5 2 2-5z"/>',
 shell='<path d="M12 3c5 0 8.5 3 8.5 7.8 0 5.6-4 9.4-8.5 10.7-4.5-1.3-8.5-5.1-8.5-10.7C3.5 6 7 3 12 3z"/><path d="M6.5 8.5c3.5 1.8 7.5 1.8 11 0M5.5 13c4 2.2 9 2.2 13 0"/>',
 shield='<path d="M12 3 4 6v6c0 5 3.5 8 8 9 4.5-1 8-4 8-9V6z"/>', eyeoff='<path d="M3 3l18 18M10.6 5.1A9.8 9.8 0 0 1 12 5c5 0 8.5 4.5 9.5 7-.4 1-1.2 2.3-2.4 3.6M6.4 6.4C4.5 7.8 3.2 9.8 2.5 12c1 2.5 4.5 7 9.5 7 1.6 0 3-.4 4.3-1.1"/>',
)
def ic(n, c="i", sw=1.9): return f'<svg class="{c}" viewBox="0 0 24 24" style="stroke-width:{sw}" aria-hidden="true">{P[n]}</svg>'

# ---------------------------------------------------------------- journey data
STEPS = [  # (nr, title, sub, zone)
 (1, "Over jou", "Persoonlijke gegevens", "kust"),
 (2, "Wanneer en hoe", "Beschikbaarheid en reizen", "kust"),
 (3, "Werk", "Waar ik werkte en wat ik zoek", "rots"),
 (4, "Leren", "Wat ik deed en nog wil leren", "rots"),
 (5, "Wat ik leuk vind", "Interesses en hobby’s", "rots"),
 (6, "Waar houd ik niet van", "Overslaan mag", "rots"),
 (7, "Competenties", "Test · 5 vragen", "diep"),
 (8, "Beroepen", "Test · 5 vragen", "diep"),
 (9, "Cultuur", "Test · 5 vragen", "diep"),
 (10, "Waarden", "Test · 5 vragen", "diep"),
]
N = len(STEPS)
ZONES = {"kust": "Aan de kust", "rots": "Tussen de rotsen", "diep": "In de diepte"}
SAY = {  # one short, warm line per step (0 = start, 11 = end)
 0: "Mijn oude schaal zit wat krap. Zullen we samen kijken wie eronder zit?",
 1: "Eerst even kennismaken. Je voeten staan nog in het water.",
 2: "Kies wat nu past. Later aanpassen mag altijd.",
 3: "Alles wat je deed telt. Ook vrijwilligerswerk, stage of zorgen voor familie.",
 4: "Leren kan op elke leeftijd. Wat wil jij nog leren?",
 5: "Waar word je blij van? Daar zit vaak je kracht.",
 6: "Hier is het rustig tussen de stenen. Je hoeft niets uit te leggen.",
 7: "Er zijn geen foute antwoorden. Kies wat het eerst in je opkomt.",
 8: "Wat doe je graag? Zwem maar rustig mee.",
 9: "Waar voel je je thuis? Nog twee lagen.",
 10: "Nog één laag, dan zie je wie je bent.",
 11: "Kijk eens hoe je gegroeid bent. Dit is je nieuwe schaal.",
}

# ---------------------------------------------------------------- lobster + shell plates
# Plates in the mascot's 256px coordinate space, in the order they fall off (step 1..10).
BODY = "M95 150C93 190 112 214 128 241C144 214 165 190 165 150Z"
PLATES = [
 '<path d="M40 70C36 46 52 32 66 36L70 54 60 60 64 84C54 90 42 84 40 70Z"/>',            # 1 left claw
 '<path d="M182 132C198 124 216 136 216 158 216 176 204 190 190 190L186 172 196 160 180 150Z"/>',  # 2 right claw
 '<rect x="80" y="210" width="96" height="34" mask="url(#MID)"/>',                           # 3 tip
 '<rect x="80" y="192" width="96" height="16.5" mask="url(#MID)"/>',                         # 4
 '<rect x="80" y="171" width="96" height="19.5" mask="url(#MID)"/>',                         # 5
 '<rect x="80" y="150" width="96" height="19.5" mask="url(#MID)"/>',                         # 6
 '<rect x="93" y="136" width="74" height="13" rx="6.5"/>',                                   # 7 chest low
 '<rect x="97" y="121" width="66" height="13" rx="6.5"/>',                                   # 8 chest high
 '<path d="M99 74C98 58 112 50 134 50L134 64C120 64 108 68 99 74Z"/>',                        # 9 helmet left
 '<path d="M136 50C158 50 172 58 171 74 162 68 150 64 136 64Z"/>',                           # 10 helmet right
]
CRACKS = '<path d="M108 158l6 8-3 7M150 196l-5 6 3 5M120 222l5 6M104 128l5 4M160 142l-4 3M118 56l4 6" />'
_uid = [0]
def lobster(size, gone, new_shell=False, falling=None, label=""):
    """gone = number of plates already shed (0..10). falling = index of plate drawn falling off."""
    _uid[0] += 1; mid = f"m{_uid[0]}"
    plates = "".join(p.replace("#MID", "#" + mid) for p in PLATES[gone:])
    cracks = f'<g class="crk">{CRACKS}</g>' if gone < 6 else ""
    fall = ""
    if falling is not None:
        fall = ('<g class="fall"><path class="trail" d="M150 118C170 112 186 120 194 140"/><path class="trail" d="M232 150l8 -6M238 170l10 -3"/>'
                '<g><g transform="translate(196 170) rotate(40) scale(1.2)">' + SHARD + '</g></g>'
                '<circle cx="246" cy="262" r="3"/><circle cx="262" cy="250" r="2.2"/></g>')
    ns = ""
    if new_shell:
        ns = (f'<g class="nshell"><path d="{BODY}"/><path d="M101 172C120 178 140 178 159 172M106 200C121 205 135 205 150 200"/>'
              '<rect x="93" y="136" width="74" height="13" rx="6.5"/><rect x="97" y="121" width="66" height="13" rx="6.5"/></g>')
    aria = f'role="img" aria-label="{label}"' if label else 'aria-hidden="true"'
    return (f'<svg class="lob" width="{size}" height="{size}" viewBox="0 0 256 256" {aria} style="overflow:visible">'
            f'<defs><mask id="{mid}" maskUnits="userSpaceOnUse" x="0" y="0" width="256" height="256"><path d="{BODY}" fill="#fff"/><circle cx="130" cy="178" r="16" fill="#000"/></mask></defs>'
            f'<use href="#masc"/>{ns}<g class="old">{plates}</g>{cracks}{fall}</svg>')
SPRITE = ('<svg width="0" height="0" style="position:absolute" aria-hidden="true"><defs>'
          f'<image id="masc" href="{MASCOT}" width="256" height="256"/>'
          '<linearGradient id="og" x1="0" y1="0" x2="0" y2="1"><stop offset="0" style="stop-color:color-mix(in srgb,var(--shell-old) 70%,var(--surface))"/>'
          '<stop offset="1" style="stop-color:color-mix(in srgb,var(--shell-old) 80%,var(--brand-deep))"/></linearGradient>'
          '<radialGradient id="calm"><stop offset="0" style="stop-color:var(--sea-0);stop-opacity:.55"/><stop offset="1" style="stop-color:var(--sea-0);stop-opacity:0"/></radialGradient>'
          '</defs></svg>')
SHARD = ('<path d="M5 34C6 16 22 4 38 4 54 4 66 14 66 28L58 24 52 31 44 25 36 32 28 26 20 33 13 28Z"/>'
         '<path class="rg" d="M14 22C22 14 32 11 40 11 50 11 58 16 61 22M24 16C30 12 36 11 42 12"/>')
def shard(size, rot=0):
    return (f'<svg class="shard" width="{size}" height="{size*0.6:.0f}" viewBox="0 0 70 40" aria-hidden="true" style="transform:rotate({rot}deg)">{SHARD}</svg>')
def lsize(step, mobile=False):
    return (60 + step * 4) if mobile else (120 + step * 10)

# ---------------------------------------------------------------- scene (background per depth)
GRAD = {  # top -> bottom, all derived from tokens in CSS
 0: "var(--sky) 0%,var(--sea-0) 44%,var(--sea-1) 60%,var(--sand) 100%",
 1: "var(--sky) 0%,var(--sky) 16%,var(--sea-1) 16.3%,var(--sea-3) 100%",
 2: "var(--sky) 0%,var(--sky) 5%,var(--sea-1) 5.3%,var(--sea-3) 80%,var(--sea-4) 100%",
 3: "var(--sea-1) 0%,var(--sea-3) 100%",
 4: "var(--sea-2) 0%,var(--sea-4) 100%",
 5: "var(--sea-2) 0%,var(--sea-5) 100%",
 6: "var(--sea-3) 0%,var(--sea-5) 100%",
 7: "var(--sea-6) 0%,var(--sea-7) 100%",
 8: "var(--sea-7) 0%,var(--sea-8) 100%",
 9: "var(--sea-7) 0%,var(--sea-9) 100%",
 10: "var(--sea-8) 0%,var(--sea-9) 100%",
 11: "var(--sky) 0%,var(--sea-0) 30%,var(--sea-2) 70%,var(--sea-4) 100%",
}
_Z = [1.0]
def weed(x, base, h, sway, w=7, op=.55):
    h *= _Z[0]; sway *= _Z[0]; w = max(3, w * _Z[0])
    return (f'<path d="M{x} {base}C{x+sway} {base-h*.35} {x-sway} {base-h*.65} {x+sway*.6} {base-h}" '
            f'style="stroke:var(--weed);opacity:{op}" stroke-width="{w}" fill="none" stroke-linecap="round"/>')
def rock(cx, cy, rx, ry, op=.5):
    rx *= _Z[0]; ry *= _Z[0]
    return (f'<path d="M{cx-rx} {cy}C{cx-rx} {cy-ry*1.1} {cx-rx*.2} {cy-ry*1.35} {cx+rx*.3} {cy-ry*1.1}C{cx+rx} {cy-ry*.8} {cx+rx*1.05} {cy-ry*.2} {cx+rx} {cy}Z" '
            f'style="fill:var(--rock);opacity:{op}"/>')
def scene(depth, W, H, lob_x):
    r = random.Random(depth * 7 + W); e = []; _Z[0] = 1.0 if W > 600 else .42
    if depth == 0:  # sunny beach, light shallow water
        sx, sy0, sr = (W*.9, H*.13, 44) if W > 600 else (W*.9, H*.2, 16)
        e.append(f'<circle cx="{sx}" cy="{sy0}" r="{sr*1.7}" style="fill:var(--sun);opacity:.18"/>')
        e.append(f'<circle cx="{sx}" cy="{sy0}" r="{sr}" style="fill:var(--sun);opacity:.8"/>')
        wy = H * .42
        e.append(f'<path d="M0 {wy}Q{W*.25} {wy-10} {W*.5} {wy}T{W} {wy}V{H}H0Z" style="fill:var(--sea-2)"/>')
        e.append(f'<path d="M0 {wy+30}Q{W*.25} {wy+22} {W*.5} {wy+30}T{W} {wy+30}V{H}H0Z" style="fill:var(--sea-3);opacity:.45"/>')
        for k in range(8 if W > 600 else 3):
            x = r.uniform(.05, .85) * W; y = wy + 22 + (k % 3) * 30 + r.uniform(-6, 6)
            e.append(f'<path d="M{x} {y}q18 -7 36 0t36 0" style="stroke:var(--surface);opacity:.7" stroke-width="2" fill="none" stroke-linecap="round"/>')
        sy = H * .70
        e.append(f'<path d="M0 {sy}C{W*.3} {sy-30} {W*.62} {sy+10} {W} {sy-20}V{H}H0Z" style="fill:var(--sand)"/>')
        e.append(f'<path d="M0 {sy+2}C{W*.3} {sy-28} {W*.62} {sy+12} {W} {sy-18}" style="stroke:var(--surface);opacity:.8" stroke-width="3" fill="none"/>')
        for k in range(7):
            e.append(f'<ellipse cx="{r.uniform(.05,.95)*W}" cy="{r.uniform(sy+30,H-10)}" rx="{r.uniform(3,7)}" ry="{r.uniform(2,4)}" style="fill:var(--rock);opacity:.25"/>')
    if depth in (1, 2):  # wading in: surface line near the top, sandy bottom
        sl = H * (.16 if depth == 1 else .05)
        e.append(f'<path d="M0 {sl}Q{W*.25} {sl+8} {W*.5} {sl}T{W} {sl}" style="stroke:var(--surface);opacity:.9" stroke-width="3" fill="none"/>')
        for k in range(6):
            x = r.uniform(0, W); y = r.uniform(sl + 40, H * .6)
            e.append(f'<path d="M{x} {y}q14 -6 28 0t28 0" style="stroke:var(--surface);opacity:.5" stroke-width="2" fill="none" stroke-linecap="round"/>')
        by = H * (.80 if depth == 1 else .86)
        e.append(f'<path d="M0 {by}C{W*.35} {by-24} {W*.7} {by+12} {W} {by-8}V{H}H0Z" style="fill:var(--sand);opacity:.9"/>')
        for k in range(4):
            y = by + 18 + k * 14
            e.append(f'<path d="M{W*.1} {y}q{W*.1} -6 {W*.2} 0t{W*.2} 0t{W*.2} 0" style="stroke:var(--rock);opacity:.12" stroke-width="2" fill="none"/>')
        if depth == 2:
            e.append(rock(W * .9, H, 80, 50, .35)); e.append(weed(W * .93, H - 20, 90, 10, 6, .4))
    if 3 <= depth <= 6:  # rocks and seaweed, deeper each step
        for k in range(3):  # faint light rays
            x = W * (.15 + .3 * k)
            e.append(f'<path d="M{x} 0L{x+W*.08} 0L{x+W*.18} {H*.8}L{x+W*.02} {H*.8}Z" style="fill:var(--surface);opacity:{.10-.012*depth:.3f}"/>')
        n = 3 + depth
        for k in range(n):
            x = r.uniform(0, W); e.append(weed(x, H + 5, r.uniform(90, 200 + depth * 20), r.uniform(-18, 18), r.uniform(5, 9), .45 + depth * .03))
        e.append(rock(W * .08, H, 140, 90, .55)); e.append(rock(W * .95, H, 170, 120, .55))
        e.append(rock(W * .55, H, 90, 40, .45))
        if depth >= 4: e.append(rock(-20, H * .55, 90, 160, .35))
        if depth == 6:  # calm, safe spot between stones around the lobster
            e.append(f'<ellipse cx="{lob_x}" cy="{H*.66}" rx="{min(W*.24,300)}" ry="{H*.34}" style="fill:url(#calm)"/>')
            for (dx, cy, rx, ry, op) in [(-150, H - 30, 90, 60, .7), (140, H - 40, 110, 80, .7), (-40, H + 10, 120, 50, .8), (60, H - 110, 50, 34, .6)]:
                e.append(rock(lob_x + dx, cy, rx, ry, op))
    if 7 <= depth <= 10:  # deep blue with light specks and bubbles
        k = depth - 7
        for _ in range(45 + 15 * k if W > 600 else 26 + 6 * k):
            e.append(f'<circle cx="{r.uniform(0,W):.0f}" cy="{r.uniform(0,H):.0f}" r="{r.uniform(.8,2.2):.1f}" style="fill:var(--surface);opacity:{r.uniform(.12,.5):.2f}"/>')
        e.append(rock(W * .1, H + 20, 180, 70, .25)); e.append(rock(W * .9, H + 20, 220, 90, .25))
    if depth == 11:  # rising to the light
        for k in range(5):
            x = W * (.1 + .2 * k)
            e.append(f'<path d="M{x} 0L{x+W*.07} 0L{x+W*.16} {H}L{x-W*.02} {H}Z" style="fill:var(--sun);opacity:.07"/>')
        e.append(f'<ellipse cx="{W*.5}" cy="0" rx="{W*.6}" ry="{H*.35}" style="fill:var(--sun);opacity:.18"/>')
    if depth == 11:
        for j in range(6):
            e.append(f'<circle class="bub b{j%3}" cx="{lob_x - 20 + (j%3)*22}" cy="{H*.62 + j*28}" r="{3+(j%3)*2}" style="stroke:var(--surface);opacity:.7" stroke-width="1.5" fill="none"/>')
    if 3 <= depth <= 10:  # bubbles near the lobster
        by = H * .55
        for j in range(5 if depth >= 7 else 3):
            rr = 3 + (j % 3) * 2.5
            e.append(f'<circle class="bub b{j%3}" cx="{lob_x + 50 + (j%2)*18 - j*4}" cy="{by - j*46}" r="{rr}" style="stroke:var(--surface);opacity:.55" stroke-width="1.5" fill="none"/>')
    return (f'<div class="scene" style="background:linear-gradient(180deg,{GRAD[depth]})" aria-hidden="true">'
            f'<svg viewBox="0 0 {W} {H}" preserveAspectRatio="none" width="100%" height="{"100%" if W>600 else str(H)+"px"}">{"".join(e)}</svg></div>')

# ---------------------------------------------------------------- CSS
CSS = '''
:root{--bg:#f5f2ee;--surface:#fffcfa;--text:#122033;--muted:#5a6a7d;--border:#ddd5cc;--pearl:#efe9e3;--brand:#0f2d5c;--brand-deep:#0a2044;--accent-hover:#163a6b;--accent-soft:#e7eef7;--coral:#f54a1b;
--success:#15803d;--success-soft:#ecfdf3;--warn:#a65b00;--warn-soft:#fff8e7;
--gold:#c9a227;--gold-light:#e4c65a;--gold-deep:#a6851c;--gold-ink:#5c4a0f;--gold-soft:#faf6e8;
--font:"Inter","Segoe UI","Helvetica Neue",Arial,sans-serif;--shadow:0 1px 2px rgba(15,45,92,.06);--shadow-lg:0 12px 32px rgba(15,45,92,.12);--radius-sm:8px;--radius:12px;--radius-pill:999px;
--text-xs:.75rem;--text-sm:.875rem;--text-md:1rem;--text-lg:1.125rem;--text-xl:1.375rem;--text-2xl:1.75rem;
--space-1:.25rem;--space-2:.5rem;--space-3:.75rem;--space-4:1rem;--space-5:1.5rem;--space-6:2rem;
/* scene: derived from existing tokens only */
--sky:color-mix(in srgb,var(--accent-soft) 45%,var(--surface));
--sea-0:color-mix(in srgb,var(--accent-soft) 75%,var(--surface));--sea-1:var(--accent-soft);
--sea-2:color-mix(in srgb,var(--brand) 14%,var(--accent-soft));--sea-3:color-mix(in srgb,var(--brand) 28%,var(--accent-soft));
--sea-4:color-mix(in srgb,var(--brand) 42%,var(--accent-soft));--sea-5:color-mix(in srgb,var(--brand) 58%,var(--accent-soft));
--sea-6:color-mix(in srgb,var(--brand) 80%,var(--accent-soft));--sea-7:var(--brand);
--sea-8:color-mix(in srgb,var(--brand-deep) 55%,var(--brand));--sea-9:var(--brand-deep);
--sand:color-mix(in srgb,var(--gold-light) 28%,var(--bg));--sun:var(--gold-light);
--rock:color-mix(in srgb,var(--muted) 60%,var(--brand));--weed:color-mix(in srgb,var(--success) 55%,var(--brand));
--shell-old:color-mix(in srgb,var(--muted) 62%,var(--gold-deep))}
*{box-sizing:border-box}html,body{margin:0}
body{font-family:var(--font);background:var(--bg);color:var(--text);line-height:1.5;font-size:16px}
p,h1,h2,h3,ul,ol,fieldset{margin:0}b,strong,summary,legend{font-weight:600}ul,ol{padding:0;list-style:none}fieldset{border:0;padding:0}
svg.i{width:18px;height:18px;fill:none;stroke:currentColor;stroke-linecap:round;stroke-linejoin:round;flex-shrink:0}
.vh{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0 0 0 0)}
.muted{color:var(--muted)}.sm{font-size:var(--text-sm)}.xs{font-size:var(--text-xs)}.b6{font-weight:600}
.demo{font-size:var(--text-xs);color:var(--muted);border:1px dashed var(--border);border-radius:6px;padding:0 8px;background:var(--surface)}
/* header + nav: identical to dna-paspoort */
.hd{position:sticky;top:0;z-index:40;height:64px;background:var(--surface);display:flex;align-items:center;justify-content:space-between;padding:0 24px;border-bottom:1px solid var(--pearl)}
.wm{display:flex;align-items:center;gap:10px}.wm img{width:40px;height:40px}.wm b{font-size:22px;font-weight:700;color:var(--brand)}.wm span{font-size:14px;font-weight:600;color:var(--brand);margin-inline-start:14px}
.hr{display:flex;align-items:center;gap:12px}
.cb{width:40px;height:40px;border-radius:50%;border:1px solid var(--border);background:var(--surface);display:flex;align-items:center;justify-content:center;color:var(--brand)}
.lang{height:40px;padding:0 12px;border:1px solid var(--border);border-radius:var(--radius-sm);display:flex;align-items:center;gap:8px;font-size:14px;font-weight:600;color:var(--brand);background:var(--surface)}
.flag{width:20px;height:14px;border-radius:3px;background:linear-gradient(#ae1c28 33%,#fff 33% 66%,#21468b 66%);box-shadow:0 0 0 1px rgba(0,0,0,.08)}
.who{text-align:end;margin-inline-start:4px}.who b{display:block;font-size:14px;font-weight:600;color:var(--brand)}.who small{font-size:12px;color:var(--muted)}
.bn{position:fixed;left:0;right:0;bottom:0;height:64px;background:var(--surface);border-top:1px solid var(--pearl);display:grid;grid-template-columns:repeat(6,1fr);padding:4px 8px;z-index:50}
.bn a{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:2px;font-size:12px;font-weight:600;color:var(--muted);border-radius:var(--radius-sm);white-space:nowrap}
.bn a.on{background:var(--accent-soft);color:var(--brand)}
.card{background:var(--surface);border-radius:var(--radius);box-shadow:var(--shadow-lg)}
.btn{height:44px;border-radius:var(--radius-sm);display:inline-flex;align-items:center;justify-content:center;gap:8px;font-size:var(--text-sm);font-weight:600;border:1px solid var(--border);color:var(--brand);background:var(--surface);padding:0 16px;white-space:nowrap;font-family:inherit}
.btn.pri{background:var(--brand);color:#fff;border-color:var(--brand)}
.lnk{white-space:nowrap;font-size:var(--text-sm);font-weight:600;color:var(--brand);display:inline-flex;align-items:center;gap:4px;min-height:44px}.lnk .i{width:16px;height:16px}
.pill{font-size:12px;font-weight:600;border-radius:var(--radius-pill);padding:2px 10px;background:var(--accent-soft);color:var(--brand);display:inline-flex;align-items:center;gap:5px;white-space:nowrap}
.pill .i{width:13px;height:13px}.pill.ok{background:var(--success-soft);color:var(--success)}.pill.gold{background:var(--gold-soft);color:var(--gold-ink)}
.bubble{background:var(--surface);border-radius:14px 14px 14px 4px;padding:10px 14px;font-size:var(--text-sm);color:var(--text);box-shadow:var(--shadow)}
:focus-visible{outline:2px solid var(--brand);outline-offset:2px}
/* ---------------- stage */
.scene{position:fixed;left:0;right:0;top:64px;bottom:64px;z-index:0;overflow:hidden}
.scene svg{display:block}
.stage{position:relative;z-index:1;display:grid;grid-template-columns:272px 640px 1fr;gap:24px;padding:24px 24px 88px;min-height:calc(100vh - 64px);align-items:start}
/* rail = dive-depth progress */
.rail{padding:18px 18px 16px}
.rail h2{font-size:var(--text-md);font-weight:600;color:var(--brand)}
.rail .cnt{font-size:var(--text-sm);color:var(--muted)}
.route{position:relative;margin-top:12px}
.route::before{content:"";position:absolute;inset-inline-start:13px;top:10px;bottom:10px;width:4px;border-radius:4px;background:linear-gradient(180deg,var(--sand),var(--sea-1) 12%,var(--sea-3) 45%,var(--sea-7) 75%,var(--sea-9))}
.zone{font-size:var(--text-xs);font-weight:600;color:var(--muted);letter-spacing:.06em;text-transform:uppercase;padding:8px 0 2px 40px}
.st{position:relative;display:flex;align-items:center;gap:12px;min-height:34px;padding-inline-start:4px}
.st .dot{width:22px;height:22px;border-radius:50%;display:flex;align-items:center;justify-content:center;background:var(--surface);border:2px solid var(--border);color:var(--muted);font-size:var(--text-xs);font-weight:600;flex:none;position:relative;z-index:1}
.st .dot .i{width:13px;height:13px;stroke-width:2.6}
.st.done .dot{background:var(--brand);border-color:var(--brand);color:#fff}
.st b{display:block;font-size:var(--text-sm);font-weight:600;line-height:1.25}.st small{display:block;font-size:var(--text-xs);color:var(--muted);line-height:1.3}
.st.todo b{font-weight:400;color:var(--muted)}
.st.now{background:var(--accent-soft);border-radius:var(--radius-sm);margin-inline-start:-4px;padding-inline-start:0;min-height:44px}
.st.now .mk{width:30px;height:30px;flex:none;position:relative;z-index:1;display:flex;align-items:center;justify-content:center}
.st.now b{color:var(--brand)}
.st.end .dot{border-style:dashed}
.layers{margin-top:14px;display:flex;align-items:center;gap:10px}
.layers .segs{display:flex;gap:3px;flex:1}
.layers .segs i{flex:1;height:10px;border-radius:9px 9px 3px 3px;background:var(--shell-old)}
.layers .segs i.new{background:var(--gold-light)}.layers .segs i.off{background:transparent;box-shadow:inset 0 0 0 1.5px var(--border)}
.layers span{font-size:var(--text-xs);color:var(--muted);white-space:nowrap}
.saved{display:flex;align-items:center;gap:6px;font-size:var(--text-xs);color:var(--success);margin-top:10px}.saved .i{width:14px;height:14px}
/* step card */
.step{padding:24px 32px 18px;display:flex;flex-direction:column}
.eb{display:flex;align-items:center;gap:8px;font-size:var(--text-xs);font-weight:600;letter-spacing:.06em;text-transform:uppercase;color:var(--muted)}
.skipb{font-size:var(--text-xs);font-weight:600;letter-spacing:0;text-transform:none;color:var(--success);background:var(--success-soft);border-radius:var(--radius-pill);padding:2px 10px}
h1{font-size:var(--text-2xl);font-weight:700;color:var(--brand);line-height:1.25;margin-top:6px}
.lead{font-size:var(--text-md);color:var(--muted);margin-top:6px;max-width:34em}
.body{display:flex;flex-direction:column;gap:16px;margin-top:18px}
.row{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.fld{display:flex;flex-direction:column;gap:6px;font-size:var(--text-sm);font-weight:600;color:var(--text)}
.fld .opt{font-weight:400;color:var(--muted)}
.inp{height:44px;border:1px solid var(--border);border-radius:var(--radius-sm);display:flex;align-items:center;padding:0 12px;font-size:var(--text-md);font-weight:400;color:var(--text);background:var(--surface)}
.inp.ph{color:var(--muted)}
.hint{font-size:var(--text-xs);font-weight:400;color:var(--muted);display:flex;align-items:center;gap:6px}.hint .i{width:14px;height:14px}
.hint.ok{color:var(--success)}
.birth{display:grid;grid-template-columns:72px 72px 96px;gap:8px}
.check{display:flex;align-items:center;gap:10px;font-size:var(--text-sm);min-height:44px}
.box{width:22px;height:22px;border-radius:6px;border:2px solid var(--border);display:flex;align-items:center;justify-content:center;flex:none;color:#fff}
.box.on{background:var(--brand);border-color:var(--brand)}.box .i{width:14px;height:14px;stroke-width:2.8}
.foot{display:flex;align-items:center;gap:12px;margin-top:20px}
.foot .sp{flex:1}
h2.q{font-size:var(--text-lg);font-weight:600;color:var(--brand)}
.qs{font-size:var(--text-sm);color:var(--muted);margin-top:2px}
/* chips (toggle) */
.chips{display:flex;flex-wrap:wrap;gap:8px;margin-top:10px}
.chip{min-height:40px;border-radius:var(--radius-pill);border:1px solid var(--border);background:var(--surface);padding:0 14px;display:inline-flex;align-items:center;gap:6px;font-size:var(--text-sm);color:var(--text)}
.chip .i{width:15px;height:15px;stroke-width:2.6}
.chip.on{background:var(--accent-soft);border-color:var(--brand);color:var(--brand);font-weight:600}
.chip.add{border-style:dashed;color:var(--brand);font-weight:600}
/* job rows */
.jobs{display:flex;flex-direction:column;gap:8px;margin-top:8px}
.job{display:flex;align-items:center;gap:12px;border:1px solid var(--border);border-radius:var(--radius-sm);padding:6px 6px 6px 12px;min-height:56px}.job>small{white-space:nowrap;padding-inline-end:6px}
.job .ico{width:36px;height:36px;border-radius:10px;background:var(--bg);color:var(--brand);display:flex;align-items:center;justify-content:center;flex:none}
.job b{display:block;font-size:var(--text-sm);font-weight:600}.job small{font-size:var(--text-xs);color:var(--muted)}
.job .sp{flex:1}.job .ed{width:44px;height:44px;display:flex;align-items:center;justify-content:center;color:var(--brand)}
details.more{border-radius:var(--radius-sm);background:var(--bg);padding:0 14px}
details.more summary{min-height:44px;display:flex;align-items:center;gap:8px;font-size:var(--text-sm);font-weight:600;color:var(--brand);list-style:none}
details.more summary .sp{flex:1}details.more summary small{font-size:var(--text-sm);font-weight:400;color:var(--muted)}
/* lobster zone */
.lz{position:relative;align-self:stretch;display:flex;flex-direction:column;align-items:center;justify-content:flex-end;padding-bottom:40px;min-height:620px}
.lz .bubble{max-width:300px;margin-bottom:14px;font-size:var(--text-md)}
.lob .old{fill:url(#og);stroke:var(--brand-deep);stroke-opacity:.35;stroke-width:1.4;opacity:.94}
.lob .crk{fill:none;stroke:var(--brand-deep);stroke-opacity:.4;stroke-width:1.4;stroke-linecap:round}
.lob .fall{fill:var(--shell-old);stroke:var(--brand-deep);stroke-opacity:.35;stroke-width:1.4}
.lob .fall .rg,.shard .rg{fill:none;stroke:var(--brand-deep);stroke-opacity:.35;stroke-width:1.6;stroke-linecap:round}
.shard{fill:url(#og);stroke:var(--brand-deep);stroke-opacity:.35;stroke-width:1.4;overflow:visible}
.lob .fall{fill:url(#og)}
.lob .fall .trail{fill:none;stroke:var(--surface);stroke-width:3;stroke-dasharray:2 9;stroke-linecap:round;opacity:.9}
.lob .nshell{fill:none;stroke:var(--gold-light);stroke-width:3;stroke-linecap:round;opacity:.95}
.shadow{width:60%;height:14px;border-radius:50%;background:var(--brand-deep);opacity:.12;margin-top:-10px}
/* likert */
.tt{display:flex;gap:6px;flex-wrap:wrap}
.tt span{font-size:var(--text-xs);font-weight:600;border-radius:var(--radius-pill);padding:4px 10px;background:var(--bg);color:var(--muted);display:inline-flex;align-items:center;gap:4px}
.tt span.now{background:var(--brand);color:#fff}.tt span.done{background:var(--success-soft);color:var(--success)}.tt .i{width:12px;height:12px;stroke-width:2.8}
.qn{display:flex;align-items:center;gap:10px;font-size:var(--text-sm);color:var(--muted);margin-top:16px}
.qn .bar{flex:1;display:flex;gap:4px}.qn .bar i{flex:1;height:6px;border-radius:6px;background:var(--pearl)}.qn .bar i.on{background:var(--brand)}
.qtext{font-size:var(--text-xl);font-weight:600;color:var(--text);line-height:1.35;margin-top:14px}
.lik{display:grid;grid-template-columns:repeat(5,1fr);gap:8px;margin-top:18px}
.lik button{height:56px;border-radius:var(--radius-sm);border:1px solid var(--border);background:var(--surface);font:inherit;font-size:var(--text-lg);font-weight:600;color:var(--brand)}
.lik button.on{background:var(--brand);border-color:var(--brand);color:#fff}
.likl{display:flex;justify-content:space-between;font-size:var(--text-sm);color:var(--muted);margin-top:8px}
.prev{display:flex;flex-direction:column;gap:6px;margin-top:18px}
.prev li{display:flex;align-items:center;gap:10px;font-size:var(--text-sm);color:var(--muted);min-height:32px}
.prev li .i{width:15px;height:15px;color:var(--success)}.prev li span{flex:1;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.prev li b{font-weight:600;color:var(--brand)}
.deeper{display:flex;align-items:center;gap:10px;padding:10px 14px;border-radius:var(--radius-sm);background:var(--bg);font-size:var(--text-sm);color:var(--muted);margin-top:18px}.deeper .i{color:var(--brand)}
/* shed moment */
.shed{display:flex;align-items:center;gap:20px}
.shed .pic{width:150px;height:150px;flex:none;position:relative;border-radius:var(--radius);background:var(--bg);display:flex;align-items:center;justify-content:center}
.fallen{display:flex;flex-direction:column;align-items:center;gap:10px}.fallen span{font-size:var(--text-xs);font-weight:600;color:var(--muted)}
.lz.rising{justify-content:center;padding-bottom:120px}
.gained{display:flex;flex-direction:column;gap:2px;margin-top:4px}
.gained li{display:flex;gap:10px;align-items:center;font-size:var(--text-sm);min-height:32px}.gained li .i{width:16px;height:16px;color:var(--success)}
.dive{display:grid;grid-template-columns:repeat(3,1fr);gap:8px;margin-top:10px}
.dv{border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 12px;display:flex;flex-direction:column;gap:2px;min-height:44px}
.dv b{font-size:var(--text-sm);font-weight:600;display:flex;align-items:center;gap:8px}.dv small{font-size:var(--text-xs);color:var(--muted)}
.dv .rd{width:18px;height:18px;border-radius:50%;border:2px solid var(--border);flex:none}
.dv.on{border-color:var(--brand);background:var(--accent-soft)}.dv.on .rd{border:5px solid var(--brand)}
.dv .dd{display:flex;gap:2px;margin-top:4px}.dv .dd i{width:14px;height:4px;border-radius:4px;background:var(--pearl)}.dv .dd i.on{background:var(--brand)}
.note{position:fixed;inset-inline-start:24px;bottom:76px;z-index:60;max-width:272px;font-size:var(--text-xs);color:var(--muted);background:var(--surface);border:1px dashed var(--border);border-radius:var(--radius-sm);padding:8px 10px;line-height:1.45}
.note.tr{inset-inline-start:auto;inset-inline-end:24px;bottom:auto;top:88px;max-width:320px}
.note b{color:var(--text);font-weight:600}
/* end + passport reveal */
.ppc{border-radius:var(--radius);overflow:hidden;border:1px solid var(--pearl);margin-top:18px}
.ppc .banner{height:64px;position:relative;background:linear-gradient(135deg,var(--brand) 0%,var(--accent-hover) 100%);color:#fff;padding:12px 16px}
.ppc .banner .t{font-size:12px;font-weight:600;letter-spacing:.14em}.ppc .banner .no{font-size:12px;opacity:.8;letter-spacing:0;font-weight:400;margin-inline-start:6px}
.ppc .stamp{position:absolute;inset-inline-end:14px;top:8px;width:50px;height:54px;color:var(--gold-light);font-size:8px;font-weight:600;letter-spacing:.06em;line-height:1.15;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center;transform:rotate(-10deg)}
.ppc .stamp svg{position:absolute;inset:0;width:100%;height:100%}.ppc .stamp .i{width:18px;height:18px;position:relative;stroke-width:2.4}
.ppc .who2{display:flex;align-items:flex-end;gap:12px;padding:0 16px;margin-top:-26px;position:relative}
.ppc .av{width:64px;height:64px;border-radius:50%;border:3px solid var(--surface);position:relative;background:linear-gradient(160deg,#f3d9c4,#d9a987);overflow:hidden;flex:none;box-shadow:var(--shadow)}
.ppc .av .hair{position:absolute;left:8px;top:5px;width:46px;height:44px;border-radius:26px 26px 12px 12px;background:#3a2418}
.ppc .av .face{position:absolute;left:17px;top:15px;width:28px;height:34px;border-radius:15px;background:#e8b896}
.ppc .av .bd{position:absolute;left:5px;top:47px;width:52px;height:26px;border-radius:26px 26px 0 0;background:var(--brand)}
.ppc .who2 h3{font-size:var(--text-lg);font-weight:600;color:var(--brand);padding-bottom:2px}
.facts{display:grid;grid-template-columns:repeat(3,1fr);gap:8px;padding:14px 16px 16px}
.fact{border-radius:var(--radius-sm);background:var(--bg);padding:8px 10px}
.fact small{display:block;font-size:var(--text-xs);color:var(--muted)}.fact b{display:block;font-size:var(--text-sm);font-weight:600;color:var(--brand)}
.fact .pv{font-size:var(--text-xs);color:var(--muted)}
/* motion: gentle, and off with reduced motion */
@keyframes rise{from{transform:translateY(0);opacity:.55}to{transform:translateY(-120px);opacity:0}}
@keyframes drop{0%{transform:translate(-60px,-60px) rotate(-20deg);opacity:0}25%{opacity:1}100%{transform:none;opacity:1}}
@keyframes bob{50%{transform:translateY(-6px)}}
.bub{animation:rise 9s linear infinite}.bub.b1{animation-duration:12s;animation-delay:-4s}.bub.b2{animation-duration:15s;animation-delay:-8s}
.lob .fall>g{animation:drop 1.2s ease-out 1 both}
.lz .lob{animation:bob 6s ease-in-out infinite}
@media (prefers-reduced-motion: reduce){.bub,.lob .fall>g,.lz .lob{animation:none}}
/* ================= MOBILE (390) ================= */
body.m .hd{padding:0 12px;height:56px}body.m .wm span{display:none}body.m .wm b{font-size:20px}body.m .wm img{width:34px;height:34px}body.m .cb,body.m .lang{height:36px}body.m .cb{width:36px}
body.m .scene{top:128px}
body.m .bn{padding:4px 2px}body.m .bn a{font-size:12px;letter-spacing:-.01em}
.mprog{position:sticky;top:56px;z-index:20;background:var(--surface);padding:10px 14px 10px;box-shadow:var(--shadow)}
.mprog .t{display:flex;align-items:center;gap:6px;font-size:var(--text-sm)}.mprog .t b{font-weight:600;color:var(--brand)}.mprog .t .sp{flex:1}
.mprog .t .ok{display:flex;align-items:center;gap:4px;font-size:var(--text-xs);color:var(--success)}.mprog .t .ok .i{width:13px;height:13px}
.mtrack{position:relative;display:flex;gap:3px;margin-top:22px}
.mtrack i{flex:1;height:8px;border-radius:8px 8px 3px 3px;background:var(--pearl)}
.mtrack .mk{position:absolute;top:-26px;width:28px;height:28px;transform:translateX(-50%)}
.mtrack .mk svg{display:block}
.mstage{position:relative;z-index:1;padding:0 12px 150px}
.mlob{display:flex;align-items:flex-end;gap:8px;padding:12px 4px 12px;min-height:128px}
.mlob .bubble{font-size:var(--text-sm);margin-bottom:10px}
body.m .step{padding:18px 16px 18px}
body.m h1{font-size:var(--text-xl)}
body.m .lead{font-size:var(--text-sm)}
body.m .body{margin-top:16px;gap:16px}
body.m .chip{min-height:44px}
.mfoot{position:fixed;left:0;right:0;bottom:64px;z-index:30;background:var(--surface);padding:10px 12px;display:flex;align-items:center;gap:10px;border-top:1px solid var(--pearl)}
.mfoot .btn.pri{flex:1}
body.m .lik{gap:6px}body.m .lik button{height:52px}
body.m .qtext{font-size:var(--text-lg)}
body.m .facts{grid-template-columns:1fr}
body.m .step{position:relative}.only-m{display:none}body.m .only-m{display:inline}body.m .only-d{display:none}.cdemo{position:absolute;inset-inline-end:12px;top:12px}
body.m .tt{display:none}body.m .job .ico{display:none}body.m .jobs .job{min-height:52px}
body.m .ppc .facts{grid-template-columns:1fr;gap:6px;padding:12px}body.m .fact{display:flex;align-items:baseline;gap:8px;padding:6px 10px}body.m .fact small{flex:1}body.m .fact .pv{display:none}
body.m .hint{font-size:var(--text-xs)}
.presets{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.pre{border:1px solid var(--border);border-radius:var(--radius-sm);padding:8px 10px;min-height:56px;background:var(--surface)}
.pre b{display:flex;align-items:center;gap:6px;font-size:var(--text-sm);font-weight:600}.pre small{display:block;font-size:var(--text-xs);color:var(--muted)}
.pre.on{border-color:var(--brand);background:var(--accent-soft)}.pre.on b{color:var(--brand)}.pre .i{width:14px;height:14px;stroke-width:2.8}
.sumr{display:flex;flex-direction:column;gap:2px;border-radius:var(--radius-sm);background:var(--bg);padding:10px 12px}
.sumr .h{display:flex;justify-content:space-between;align-items:center;font-size:var(--text-sm);font-weight:600}
.sumr dl{margin:4px 0 0;display:grid;grid-template-columns:auto 1fr;gap:2px 12px;font-size:var(--text-sm)}.sumr dt{color:var(--muted)}.sumr dd{margin:0;font-weight:600}
'''

# ---------------------------------------------------------------- shared parts
def header(m):
    who = '' if m else '<div class="who"><b>Samira El Amrani</b><small>Kandidaat</small></div>'
    demo = '' if m else '<span class="demo">Voorbeelddata</span>'
    return (f'<header class="hd"><div class="wm"><img src="{LOGO}" alt=""><b>Lobsy</b><span>Dichtbij genoeg om het pantser te laten vallen</span></div>{demo}'
            f'<div class="hr"><div class="cb" aria-label="Meldingen">{ic("bell")}</div><div class="lang" aria-label="Taal: Nederlands"><i class="flag"></i>NL {ic("chevd")}</div>{who}<div class="cb">{ic("user")}</div></div></header>')
def nav(m):
    it = [("compass", "De ontdekkingsreis", "Reis", 1), ("book", "Mijn Paspoort", "Paspoort", 0), ("search", "Zoeken", "Zoeken", 0),
          ("heart", "Bewaard", "Bewaard", 0), ("clip", "Sollicitaties", "Sollicitaties", 0), ("sun", "Carrière", "Carrière", 0)]
    return ('<nav class="bn" aria-label="Hoofdmenu">' + "".join(
        f'<a class="{"on" if o else ""}"{" aria-current=page" if o else ""}>{ic(i)}{(ms if m else t)}</a>' for i, t, ms, o in it) + '</nav>')

def rail(cur, end=False):
    """Vertical dive-depth progress. cur = current step (0 = start)."""
    h = '<ol class="route" aria-label="Jouw reis, van het strand naar de diepte">'
    h += f'<li class="st {"done" if cur>0 or end else "now"}">' + (f'<span class="mk">{lobster(30, 0)}</span>' if cur == 0 and not end else f'<span class="dot">{ic("check")}</span>') + '<div><b>Het strand</b><small>Start</small></div></li>'
    zone = None
    for nr, t, sub, z in STEPS:
        if z != zone: h += f'<li class="zone" aria-hidden="true">{ZONES[z]}</li>'; zone = z
        if end or nr < cur:
            h += f'<li class="st done"><span class="dot">{ic("check")}</span><div><b>{t}</b><small>Laag {nr} eraf</small></div></li>'
        elif nr == cur:
            h += f'<li class="st now" aria-current="step"><span class="mk">{lobster(30, nr-1)}</span><div><b>{t}</b><small>{sub}</small></div></li>'
        else:
            h += f'<li class="st todo"><span class="dot">{nr}</span><div><b>{t}</b><small>{sub}</small></div></li>'
    h += (f'<li class="st now" aria-current="step"><span class="mk">{lobster(30, 10, True)}</span><div><b>Naar het licht</b><small>Je nieuwe schaal</small></div></li>' if end
          else '<li class="st end todo"><span class="dot">' + ic("sun") + '</span><div><b>Naar het licht</b><small>Je nieuwe schaal</small></div></li>')
    h += '</ol>'
    gone = N if end else max(cur - 1, 0)
    segs = "".join(f'<i class="{"new" if end else ("off" if k < gone else "")}"></i>' for k in range(N))
    cnt = "Klaar · 10 van 10 · nieuwe schaal" if end else ("10 stappen · ± 12 minuten" if cur == 0 else f"Stap {cur} van {N}")
    saved = f'<p class="saved">{ic("check")}Alles is bewaard. Stoppen mag altijd.</p>' if cur > 0 or end else ''
    return (f'<aside class="card rail"><h2>Jouw reis</h2><p class="cnt">{cnt}</p>{h}'
            f'<div class="layers" aria-label="Oude schaal: {gone} van {N} lagen eraf"><span class="segs">{segs}</span><span>{gone} van {N} lagen eraf</span></div>{saved}</aside>')

def mprog(cur, end=False, title=""):
    gone = N if end else max(cur - 1, 0)
    segs = ""
    for k in range(N):
        col = f"var(--sea-{min(9, 1 + k*8//9)})" if (k < cur - 1 or end) else ("var(--sea-3)" if k == cur - 1 else "")
        segs += f'<i style="{"background:"+col if col else ""}"></i>'
    pos = 95 if end else ((cur - .5) / N * 100 if cur else 2)
    lab = "Klaar · 10 van 10" if end else (f"Stap {cur} van {N}" if cur else "10 stappen · ± 12 min")
    ok = f'<span class="ok">{ic("check")}Bewaard</span>' if cur else ''
    return (f'<div class="mprog" role="group" aria-label="Voortgang: {lab}, {gone} lagen eraf"><p class="t"><b>{lab}</b><span class="muted">{("· " + title) if title else ""}</span><span class="sp"></span>{ok}</p>'
            f'<div class="mtrack">{segs}<span class="mk" style="left:{pos:.1f}%">{lobster(28, gone, end)}</span></div></div>')

def footer(primary, back=True, later=True, skip=False):
    b = f'<a class="btn" aria-label="Vorige stap">{ic("left")}Terug</a>' if back else ''
    sk = '<a class="lnk">Overslaan</a>' if skip else ''
    lt = '<a class="lnk">Later verder</a>' if later else ''
    return f'<div class="foot">{b}<span class="sp"></span>{sk}{lt}<a class="btn pri">{primary}{ic("right")}</a></div>'

def lzone(step, txt=None, gone=None, new_shell=False, falling=None):
    g = max(step - 1, 0) if gone is None else gone
    lab = "Lobsy de kreeft" + (": een stukje oude schaal valt eraf" if falling is not None else "")
    size = 270 if step == 11 else lsize(step)
    rising = step == 11
    return (f'<div class="lz{" rising" if rising else ""}"><p class="bubble">{txt or SAY[step]}</p>{lobster(size, g, new_shell, falling, lab)}'
            + ('' if rising else '<span class="shadow"></span>') + '</div>')

def page(title, body, depth, mobile, extra=""):
    sc = scene(depth, 390, 170, 60) if mobile else scene(depth, 1440, 772, 1180)
    demo = ''
    return (f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
            f'<title>{title} · De ontdekkingsreis · Lobsy</title><style>{CSS}</style></head><body class="{"m" if mobile else "d"}">{SPRITE}'
            f'{header(mobile)}{sc}{demo}{body}{extra}{nav(mobile)}</body></html>')

# ---------------------------------------------------------------- step contents
def welcome_body(m=False):
    zones = [("Aan de kust", "Over jou, en wanneer je kunt werken", "2 min"),
             ("Tussen de rotsen", "Je werk, wat je leerde en wat je leuk vindt", "4 min"),
             ("In de diepte", "4 korte tests van 5 vragen", "5 min")]
    li = "".join(f'<li class="job"><span class="ico">{ic(["wave","shell","compass"][k])}</span><div><b>{a}</b><small>{b}</small></div><span class="sp"></span><small class="muted">{c}</small></li>' for k, (a, b, c) in enumerate(zones))
    return (f'<p class="eb">De ontdekkingsreis</p><h1>Hoi Samira, fijn dat je er bent</h1>'
            f'<p class="lead">Samen ontdekken we wie je bent en welk werk bij je past. Laag voor laag. Aan het eind staat alles in je paspoort.</p>'
            f'<div class="body"><ul class="jobs" style="margin:0">{li}</ul>'
            f'<p class="hint">{ic("check")}Er zijn geen foute antwoorden. Het maakt niet uit waar je nu staat.</p>'
            f'<p class="hint">{ic("clock")}Alles wordt meteen bewaard. Stoppen mag, je gaat later verder waar je was.</p>'
            f'<p class="hint">{ic("wave")}Liever een andere taal? Kies je taal rechtsboven.</p></div>')

def personal_body():
    return (f'<p class="eb">Aan de kust · stap 1 van {N}</p><h1>Even kennismaken</h1>'
            f'<p class="lead">Klopt dit? Je naam hebben we van je account overgenomen.</p>'
            f'<div class="body"><div class="row"><label class="fld">Voornaam<span class="inp">Samira</span></label><label class="fld">Achternaam<span class="inp">El Amrani</span></label></div>'
            f'<label class="fld">Postcode<span class="inp" style="max-width:200px">2291 AB</span><span class="hint ok">{ic("check")}Wateringen · alleen voor reistijd, werkgevers zien je adres niet</span></label>'
            f'<fieldset class="fld"><legend style="padding:0;margin-bottom:6px">Geboortedatum</legend><div class="birth"><span class="inp">14</span><span class="inp">03</span><span class="inp">2004</span></div>'
            f'<span class="hint">Zo laten we alleen banen zien die bij je leeftijd mogen.</span></fieldset>'
            f'<label class="fld"><span>Telefoonnummer <span class="opt">(mag je leeg laten)</span></span><span class="inp" style="max-width:280px">06 12345678</span></label>'
            f'<label class="check" style="margin-top:-10px"><span class="box on">{ic("check")}</span>Werkgevers mogen mij ook via WhatsApp benaderen</label></div>')

def chip(t, on=False): return f'<span class="chip{" on" if on else ""}" role="checkbox" aria-checked="{"true" if on else "false"}">{ic("check") if on else ""}{t}</span>'

def work_body():
    jobs = [("Zorghulp (vrijwillig)", "Zorghuis Wateringen · 2025 – nu"), ("Bediening", "Restaurant Al Bahr, Casablanca · 2021 – 2024")]
    j = "".join(f'<li class="job"><span class="ico">{ic("brief")}</span><div><b>{a}</b><small>{b}</small></div><span class="sp"></span><a class="ed" aria-label="{a} aanpassen">{ic("edit")}</a></li>' for a, b in jobs)
    want = [("Klein, warm team", 1), ("Groot bedrijf", 0), ("Vaste werkplek", 1), ("Leren op de werkvloer", 1), ("Hulp met Nederlands", 0), ("Kans om door te groeien", 0), ("Dicht bij huis", 1), ("Veel afwisseling", 0)]
    return (f'<p class="eb">Tussen de rotsen · stap 3 van {N}</p><h1>Jouw werk</h1>'
            f'<p class="lead">Waar heb je gewerkt, en wat voor werkgever zoek je?</p>'
            f'<div class="body"><section><h2 class="q">Waar heb ik gewerkt?</h2><p class="qs">Bijbaan, vrijwilligerswerk, stage of zorgen voor familie telt ook mee.</p>'
            f'<ul class="jobs">{j}</ul><div style="display:flex;align-items:center;gap:16px"><a class="lnk">{ic("plus")}Werkgever toevoegen</a>'
            f'<label class="check"><span class="box"></span>Ik heb nog niet gewerkt</label></div></section>'
            f'<section><h2 class="q">Wat voor werkgever zoek ik?</h2><p class="qs">Kies wat voor jou belangrijk is. Meer kiezen mag.</p>'
            f'<div class="chips">{"".join(chip(t,o) for t,o in want)}</div></section>'
            f'<details class="more"><summary>In welk werkveld wil ik werken?<span class="sp"></span><small>Zorg & welzijn, Horeca</small>{ic("chevd")}</summary></details></div>')

def dislike_body(m=False):
    opts = [("Nachtdiensten", 1), ("Zwaar tillen", 0), ("Veel alleen werken", 1), ("Bellen met klanten", 0), ("Lawaai", 0), ("Kou of veel buiten", 0),
            ("Veel computerwerk", 0), ("Steeds andere tijden", 0), ("Drukke plekken", 1), ("Lang reizen", 0)]
    return (f'<p class="eb">Tussen de rotsen · stap 6 van {N}<span class="skipb">Overslaan mag</span></p><h1>Waar houd ik niet van?</h1>'
            f'<p class="lead">Ook dat is goed om te weten. Dan laten we minder werk zien dat niet bij je past.</p>'
            f'<div class="body"><section><h2 class="q">Wat liever niet?</h2><p class="qs">Kies wat je wilt. Niets kiezen is ook goed.</p>'
            f'<div class="chips">{"".join(chip(t,o) for t,o in opts)}<span class="chip add">{ic("plus")}Iets anders</span></div></section>'
            f'<p class="hint">{ic("eyeoff")}Alleen voor jou en je matches. Werkgevers zien dit niet.</p></div>')

def likert(sel=None):
    b = "".join(f'<button class="{"on" if k==sel else ""}" role="radio" aria-checked="{"true" if k==sel else "false"}" aria-label="{k} van 5">{k}</button>' for k in range(1, 6))
    return f'<div class="lik" role="radiogroup" aria-label="Hoe goed past deze zin bij jou?">{b}</div><div class="likl"><span>Past niet</span><span>Past heel goed</span></div>'

def tests_tabs(cur):
    names = ["Competenties", "Beroepen", "Cultuur", "Waarden"]
    return '<div class="tt">' + "".join(
        f'<span class="{"done" if 7+k<cur else ("now" if 7+k==cur else "")}">{ic("check") if 7+k<cur else ""}{n}</span>' for k, n in enumerate(names)) + '</div>'

def test_body(step, qi, qtext, prev, zone_line, sel=None, deeper=True):
    bar = "".join(f'<i class="{"on" if k<qi else ""}"></i>' for k in range(5))
    pv = "".join(f'<li>{ic("check")}<span>{t}</span><b>{a}</b></li>' for t, a in prev)
    dp = f'<p class="deeper">{ic("wave")}Na 5 vragen kun je kiezen: klaar, of dieper met 10 of 25 vragen.</p>' if deeper else ''
    title = {7: "Hoe werk jij?", 8: "Wat vind je leuk?", 9: "Waar voel je je thuis?", 10: "Wat vind je belangrijk?"}[step]
    return (f'<p class="eb">In de diepte · <span class="only-d">stap {step} van {N}</span><span class="only-m">test {step-6} van 4</span></p>{tests_tabs(step)}<h1 style="margin-top:12px">{title}</h1>'
            f'<p class="lead">Hoe goed past deze zin bij jou? Er zijn geen foute antwoorden.</p>'
            f'<p class="qn">Vraag {qi} van 5<span class="bar">{bar}</span></p>'
            f'<p class="qtext">“{qtext}”</p>{likert(sel)}{"<ul class=prev aria-label=Beantwoord>"+pv+"</ul>" if pv else ""}{dp}')

# ---------------------------------------------------------------- desktop screens
def desk(step_for_rail, card, lz, depth, title, end=False, extra=""):
    body = f'<main class="stage">{rail(step_for_rail, end)}<section class="card step">{card}</section>{lz}</main>'
    return page(title, body, depth, False, extra)

def d1():
    card = welcome_body() + f'<div class="foot"><span class="sp"></span><a class="btn pri">Begin de reis{ic("right")}</a></div>'
    note = ('<p class="note"><b>Ontwerpnotitie</b> · Achtergrond = CSS-verloop + inline SVG, geen foto’s. Alle zeekleuren zijn color-mix() van bestaande tokens. '
            'Bubbels stijgen traag; bij “minder beweging” staan ze stil.</p>')
    return desk(0, card, lzone(0, gone=0), 0, "Start", extra=note)
def d2():
    return desk(1, personal_body() + footer("Volgende", back=False), lzone(1), 1, "Over jou")
def d3():
    return desk(3, work_body() + footer("Volgende"), lzone(3), 3, "Werk")
def d4():
    return desk(6, dislike_body() + footer("Volgende", skip=True), lzone(6), 6, "Waar houd ik niet van")
def d5():
    prev = [("Ik werk graag samen met anderen om een klus af te ronden.", "4"), ("Ik maak af wat ik beloof, ook als het tegenzit.", "5"), ("Ik blijf kalm als het druk is of er iets misgaat.", "3")]
    return desk(7, test_body(7, 4, "Ik bedenk graag nieuwe manieren om een probleem op te lossen.", prev, "", sel=None) + footer("Volgende"), lzone(7), 7, "Test Competenties")
def d6():
    gained = ["Je sterkste punt: zorgzaam (voorlopig)", "Je werkt graag samen", "Je blijft rustig als het druk is"]
    g = "".join(f'<li>{ic("check")}{t}</li>' for t in gained)
    dive = [("Zo laten", "5 vragen · klaar", 1, 1), ("Iets dieper", "10 vragen · + 2 min", 0, 2), ("Heel diep", "25 vragen · + 6 min", 0, 5)]
    dv = "".join(f'<label class="dv{" on" if on else ""}" role="radio" aria-checked="{"true" if on else "false"}"><b><span class="rd"></span>{a}</b><small>{b}</small>'
                 f'<span class="dd" aria-hidden="true">{"".join(f"<i class={chr(39)}on{chr(39)}></i>" if k<n else "<i></i>" for k in range(5))}</span></label>' for a, b, on, n in dive)
    card = (f'<p class="eb">In de diepte · stap 7 van {N} klaar</p>{tests_tabs(8)}'
            f'<div class="shed" style="margin-top:14px"><div class="pic"><div class="fallen">{shard(120,-8)}<span>Laag 7 van {N}</span></div></div>'
            f'<div><h1 style="margin-top:0">Weer een laag eraf</h1><p class="lead">Competenties is klaar. Dit staat nu in je paspoort:</p><ul class="gained">{g}</ul></div></div>'
            f'<div class="body" style="margin-top:18px"><fieldset><legend class="q" style="font-size:var(--text-lg);font-weight:600;color:var(--brand);padding:0">Wil je dieper in deze test?</legend>'
            f'<p class="qs">Meer vragen geeft een scherper beeld. Het mag, het hoeft niet. Je kunt dit later ook nog doen.</p><div class="dive">{dv}</div></fieldset></div>'
            f'<div class="foot"><span class="sp"></span><a class="lnk">Later verder</a><a class="btn pri">Verder naar Beroepen{ic("right")}</a></div>')
    note = ('<p class="note tr"><b>Ontwerpnotitie</b> · Het schaalstuk valt één keer (1,2 s). Bij “minder beweging”: geen val, alleen de nieuwe tekst. '
            'Geen pop-up: dit moment vervangt de stapkaart en gaat pas verder als je zelf klikt.</p>')
    return desk(8, card, lzone(8, txt="Voel je dat? Weer een laag eraf. Je wordt groter.", gone=7, falling=6), 7, "Laag eraf", extra=note)
def ppcard(m=False):
    facts = [("Je sterkste punt", "Zorgzaam"), ("Werk dat bij je past", "Helpen & maken"), ("Belangrijk voor jou", "Verbinding")]
    f = "".join(f'<div class="fact"><small>{a}</small><b>{b}</b><span class="pv">Eerste indruk</span></div>' for a, b in facts)
    stamp = ('<svg viewBox="0 0 64 68" preserveAspectRatio="none" aria-hidden="true"><path d="M32 4C46 4 58 12 58 28 58 46 45 58 32 64 19 58 6 46 6 28 6 12 18 4 32 4Z" '
             'style="fill:none;stroke:var(--gold-light)" stroke-width="1.8" stroke-dasharray="3.5 2.5"/></svg>')
    return (f'<div class="ppc" aria-label="Voorbeeld van je paspoort"><div class="banner"><p class="t">LOBSY PASPOORT<span class="no">LB-04817</span></p>'
            f'<div class="stamp" aria-hidden="true">{stamp}{ic("check")}</div></div>'
            f'<div class="who2"><span class="av"><i class="hair"></i><i class="face"></i><i class="bd"></i></span><div><h3>Samira El Amrani</h3></div></div>'
            f'<div class="facts">{f}</div></div>')
def d7():
    card = (f'<p class="eb">Klaar · 10 van {N} lagen eraf</p><h1>Dit ben jij, Samira</h1>'
            f'<p class="lead">Je oude schaal is eraf. Alles wat je vertelde staat nu in je paspoort. Werkgevers zien pas iets als jij dat wilt.</p>'
            f'{ppcard()}<p class="hint" style="margin-top:14px">{ic("check")}12 vacatures passen al bij je. Hoe dieper je duikt, hoe scherper je matches.</p>'
            f'<div class="foot"><a class="lnk">Een test verdiepen</a><span class="sp"></span><a class="btn pri">Bekijk je paspoort{ic("right")}</a></div>')
    return desk(11, card, lzone(11, gone=10, new_shell=True), 11, "Klaar", end=True)

# ---------------------------------------------------------------- mobile screens
def mob(cur, card, depth, title, foot, end=False, ptitle="", say_step=None, gone=None, new_shell=False):
    card = '<p class="demo cdemo">Voorbeelddata</p>' + card
    st = cur if say_step is None else say_step
    g = max(cur - 1, 0) if gone is None else gone
    lob = f'<div class="mlob">{lobster(lsize(st, True), g, new_shell, None, "Lobsy de kreeft")}<p class="bubble">{SAY[st]}</p></div>'
    body = f'{mprog(cur, end, ptitle)}<main class="mstage">{lob}<section class="card step">{card}</section></main><div class="mfoot">{foot}</div>'
    return page(title, body, depth, True)
def m1():
    return mob(0, welcome_body(True), 0, "Start", f'<a class="btn pri">Begin de reis{ic("right")}</a>')
def m2():
    pre = [("Per direct", "Ik kan meteen beginnen", 1), ("Bijbaan naast school", "Na school en in het weekend", 0), ("Weekenden", "Za en zo", 0), ("Avonden", "Ma–vr na 18:00", 1),
           ("Kantoordagen", "Ma–vr, 9–17 uur", 0), ("Vakantiewerk", "In de schoolvakanties", 0), ("Parttime", "12–32 uur per week", 1), ("Fulltime", "36–40 uur per week", 0)]
    p = "".join(f'<span class="pre{" on" if on else ""}" role="checkbox" aria-checked="{"true" if on else "false"}"><b>{ic("check") if on else ""}{a}</b><small>{b}</small></span>' for a, b, on in pre)
    card = (f'<p class="eb">Aan de kust · stap 2 van {N}</p><h1>Wanneer kun je werken?</h1><p class="lead">Kies wat bij je past, meer mag. Wij vullen de rest voor je in.</p>'
            f'<div class="body"><div class="presets" role="group" aria-label="Wanneer kun je werken">{p}</div>'
            f'<div class="sumr"><p class="h">Zo hebben we het ingevuld<a class="lnk" style="min-height:0">Aanpassen</a></p><dl><dt>Uren per week</dt><dd>12 – 32 uur</dd><dt>Wanneer</dt><dd>Ma–vr avond · za, zo</dd><dt>Vanaf</dt><dd>Per direct</dd></dl></div>'
            f'<section><h2 class="q">Hoe kom je op je werk?</h2><div class="chips">{chip("Fiets",1)}{chip("E-bike")}{chip("Auto")}{chip("OV")}{chip("Lopend")}</div>'
            f'<p class="qs" style="margin-top:12px">Maximaal reizen</p><div class="chips" style="margin-top:6px">{chip("10 min")}{chip("20 min")}{chip("30 min",1)}{chip("45 min")}</div></section></div>')
    foot = f'<a class="btn" aria-label="Vorige stap">{ic("left")}</a><a class="lnk">Later verder</a><a class="btn pri">Volgende{ic("right")}</a>'
    return mob(2, card, 2, "Wanneer en hoe", foot, ptitle="Wanneer en hoe")
def m3():
    prev = [("Ik wil zelf kunnen bepalen hoe ik mijn werk aanpak.", "4")]
    card = test_body(10, 2, "Ik help collega’s graag, ook buiten mijn eigen taken.", prev, "", sel=5, deeper=True)
    foot = f'<a class="btn" aria-label="Vorige vraag">{ic("left")}</a><a class="lnk">Later verder</a><a class="btn pri">Volgende{ic("right")}</a>'
    return mob(10, card, 10, "Test Waarden", foot, ptitle="Waarden")
def m4():
    card = (f'<p class="eb">Klaar · 10 van {N} lagen eraf</p><h1>Dit ben jij, Samira</h1>'
            f'<p class="lead">Je oude schaal is eraf. Alles staat nu in je paspoort.</p>{ppcard(True)}'
            f'<p class="hint" style="margin-top:12px">{ic("check")}Eerste indruk. Hoe dieper je duikt, hoe scherper je matches.</p>')
    return mob(11, card, 11, "Klaar", f'<a class="btn pri">Bekijk je paspoort{ic("right")}</a>', end=True, say_step=11, gone=10, new_shell=True)

D = [("or-d1-start", d1), ("or-d2-over-jou", d2), ("or-d3-werk", d3), ("or-d4-waar-houd-ik-niet-van", d4), ("or-d5-test-vraag", d5), ("or-d6-laag-eraf", d6), ("or-d7-einde-paspoort", d7)]
M = [("or-m1-start", m1), ("or-m2-wanneer-en-hoe", m2), ("or-m3-test-vraag", m3), ("or-m4-einde", m4)]

CHECK = """()=>{const out={over:[],small:[],weights:new Set()};
for(const e of document.querySelectorAll('body *')){const cs=getComputedStyle(e);const r=e.getBoundingClientRect();
 if(e.closest('svg'))continue;
 if(r.width&&(r.right>innerWidth+1))out.over.push(e.className+':'+Math.round(r.right));
 if(e.childNodes.length&&[...e.childNodes].some(n=>n.nodeType==3&&n.textContent.trim())){out.weights.add(cs.fontWeight);if(parseFloat(cs.fontSize)<12)out.small.push(e.className+':'+cs.fontSize);
  if(e.scrollWidth>e.clientWidth+1&&cs.overflow==='visible'&&cs.display!=='inline')out.over.push('scroll:'+e.className+':'+e.textContent.trim().slice(0,20));}}
out.weights=[...out.weights];out.over=out.over.slice(0,6);out.small=out.small.slice(0,6);out.h=document.documentElement.scrollHeight;return out}"""

if __name__ == "__main__":
    with sync_playwright() as pw:
        b = pw.chromium.launch(executable_path="/usr/bin/google-chrome")
        pg = b.new_page(viewport={"width": 1440, "height": 900}, reduced_motion="reduce")
        for n, fn in D:
            f = d / f"{n}.html"; f.write_text(fn()); pg.goto(f.as_uri()); pg.wait_for_timeout(300)
            print(n, pg.evaluate(CHECK), f"{f.stat().st_size//1024}kB"); pg.screenshot(path=str(d / f"{n}.png"))
        pm = b.new_page(viewport={"width": 390, "height": 844}, device_scale_factor=2, reduced_motion="reduce")
        for n, fn in M:
            f = d / f"{n}.html"; f.write_text(fn()); pm.goto(f.as_uri()); pm.wait_for_timeout(300)
            navw = pm.evaluate("[...document.querySelectorAll('.bn a')].map(a=>{const r=document.createRange();r.selectNodeContents(a);return Math.round(r.getBoundingClientRect().width)+'/'+Math.round(a.getBoundingClientRect().width)})")
            print(n, pm.evaluate(CHECK), "nav", navw, f"{f.stat().st_size//1024}kB"); pm.screenshot(path=str(d / f"{n}.png"))
        b.close()
