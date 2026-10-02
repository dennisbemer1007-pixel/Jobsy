"""Puzzelpauzes in de ontdekkingsreis (groep 7/8): 3 visuele puzzels na vraag 15, 30 en 45.
Mockups only (sample data). Reuses the leerling look from ../../jobsy-design/mockup/scholen/pupil.py
(tokens, lobster with shell plates, ocean scene, header, progress). Every puzzle instance is GENERATED
from the pupil code (seeded) by the generators below, so these mockups show real generator output."""
import sys, hashlib, random, pathlib
sys.path.insert(0, "/workspace/jobsy-design/mockup/scholen")
import pupil
from pupil import ic, lobster, P

TEMPLATE_VERSION = "v1"

def rng(code, key):
    """Deterministic per pupil + puzzle: same code -> same puzzle (reload safe); other code -> other instance."""
    h = hashlib.sha256(f"{TEMPLATE_VERSION}:{key}:{code}".encode()).digest()
    return random.Random(int.from_bytes(h[:8], "big"))

# ---------------------------------------------------------------- tokens (shape carries meaning, colour is extra)
COLS = [("#E69F00", "oranje"), ("#56B4E9", "lichtblauw"), ("#009E73", "groen"), ("#F0E442", "geel"),
        ("#0072B2", "blauw"), ("#D55E00", "rood"), ("#CC79A7", "roze")]  # Okabe-Ito, colour-blind safe
OUT = "#0f2d5c"
def _star(cx, cy, R, r, n=5, rot=-90):
    import math
    pts = []
    for k in range(n * 2):
        a = math.radians(rot + k * 180 / n); rr = R if k % 2 == 0 else r
        pts.append(f"{cx + rr * math.cos(a):.1f},{cy + rr * math.sin(a):.1f}")
    return " ".join(pts)
SHAPES = {
 "schelp": lambda c: (f'<path d="M24 41C16 40 8 32 7 22C7 14 15 7 24 7C33 7 41 14 41 22C40 32 32 40 24 41Z" fill="{c}" stroke="{OUT}" stroke-width="2.4" stroke-linejoin="round"/>'
                      f'<path d="M24 40 13 13M24 40 18 9.5M24 40V8M24 40 30 9.5M24 40 35 13" stroke="{OUT}" stroke-width="1.6" stroke-linecap="round" opacity=".7"/>'
                      f'<path d="M19 40h10v3.5H19z" fill="{c}" stroke="{OUT}" stroke-width="2" stroke-linejoin="round"/>'),
 "zeester": lambda c: f'<polygon points="{_star(24, 25, 19, 8)}" fill="{c}" stroke="{OUT}" stroke-width="2.4" stroke-linejoin="round"/><circle cx="24" cy="25" r="2.2" fill="{OUT}" opacity=".5"/>',
 "parel": lambda c: f'<circle cx="24" cy="24" r="15" fill="{c}" stroke="{OUT}" stroke-width="2.4"/><circle cx="18.5" cy="18.5" r="4.5" fill="#fff" opacity=".85"/>',
 "kristal": lambda c: (f'<path d="M24 5 41 20 24 43 7 20Z" fill="{c}" stroke="{OUT}" stroke-width="2.4" stroke-linejoin="round"/>'
                       f'<path d="M7 20h34M16 20l8-15 8 15-8 23Z" fill="none" stroke="{OUT}" stroke-width="1.5" stroke-linejoin="round" opacity=".6"/>'),
 "maan": lambda c: f'<path d="M30 6A18 18 0 1 0 41 33 14 14 0 1 1 30 6Z" fill="{c}" stroke="{OUT}" stroke-width="2.4" stroke-linejoin="round"/>',
 "vis": lambda c: (f'<path d="M5 24C11 13 26 11 35 19L43 12V36L35 29C26 37 11 35 5 24Z" fill="{c}" stroke="{OUT}" stroke-width="2.4" stroke-linejoin="round"/>'
                   f'<circle cx="14" cy="22" r="2.6" fill="{OUT}"/><path d="M22 18c2 4 2 8 0 12" stroke="{OUT}" stroke-width="1.6" fill="none" opacity=".6"/>'),
}
SHAPE_NL = {"schelp": "schelp", "zeester": "zeester", "parel": "parel", "kristal": "kristal", "maan": "maan", "vis": "vis"}
ADJ = {"blauw": "blauwe", "lichtblauw": "lichtblauwe", "groen": "groene", "geel": "gele", "rood": "rode"}
def tname(t):
    """Dutch name with correct adjective form: 'lichtblauwe parel', 'blauw kristal' (het-woord)."""
    shape, (_, coln) = t
    adj = coln if shape == "kristal" else ADJ.get(coln, coln)
    return f"{adj} {SHAPE_NL[shape]}"
def tok(t, size=40, cls="tok"):
    shape, (col, coln) = t
    return (f'<svg class="{cls}" width="{size}" height="{size}" viewBox="0 0 48 48" role="img" aria-label="{tname(t)}">'
            f'{SHAPES[shape](col)}</svg>')

# ---------------------------------------------------------------- generators
P1_FAMILIES = {1: ["AB", "AAB"], 2: ["ABC", "ABB", "AABB"], 3: ["ABCB", "ABCD", "AABC"]}
def gen_p1(code, level=2):
    r = rng(code, "p1-schelpenrij")
    fam = r.choice(P1_FAMILIES[level]); letters = sorted(set(fam))
    shapes = r.sample(list(SHAPES), 4); cols = r.sample(COLS, 4)
    tokens = list(zip(shapes, cols))                       # 4 distinct shapes AND colours
    m = {L: tokens[i] for i, L in enumerate(letters)}       # alphabet; the rest are foils
    off = r.randrange(len(fam))                             # pattern may start mid-cycle
    row = [m[fam[(i + off) % len(fam)]] for i in range(8)]
    opts = tokens[:]; r.shuffle(opts)
    return dict(family=fam, offset=off, shown=row[:7], answer=row[7], options=opts, level=level)

def gen_p2(code, level=2):
    r = rng(code, "p2-schatkaart")
    D = {"up": (-1, 0), "down": (1, 0), "left": (0, -1), "right": (0, 1)}
    PERP = {"up": ["left", "right"], "down": ["left", "right"], "left": ["up", "down"], "right": ["up", "down"]}
    runs_n = {1: 2, 2: 3, 3: 4}[level]; tot = {1: (3, 4), 2: (5, 7), 3: (7, 9)}[level]
    while True:
        start = (r.randrange(5), r.randrange(5)); cur = start; seen = {start}; runs = []; d = r.choice(list(D)); ok = True
        for k in range(runs_n):
            if k: d = r.choice(PERP[d] + (["up", "down", "left", "right"] if level == 3 else []))
            n = r.randint(1, 3)
            for _ in range(n):
                cur = (cur[0] + D[d][0], cur[1] + D[d][1])
                if not (0 <= cur[0] < 5 and 0 <= cur[1] < 5) or cur in seen: ok = False; break
                seen.add(cur)
            if not ok: break
            runs.append((d, n))
        if ok and tot[0] <= sum(n for _, n in runs) <= tot[1] and abs(cur[0] - start[0]) + abs(cur[1] - start[1]) >= 2:
            break
    free = [(i, j) for i in range(5) for j in range(5) if (i, j) not in seen]
    rocks = r.sample(free, 3)
    path = [start]; p = start
    for d, n in runs:
        for _ in range(n):
            p = (p[0] + D[d][0], p[1] + D[d][1]); path.append(p)
    return dict(start=start, runs=runs, end=cur, path=path, rocks=rocks, level=level)

def gen_p3(code, level=2):
    r = rng(code, "p3-vuurtorenlampen")
    shapes = r.sample(list(SHAPES), 4); cols = r.sample([c for c in COLS if c[1] != "geel"], 4)  # yellow is too weak on a lit window
    syms = list(zip(shapes, cols)); foil = syms[3]; syms = syms[:3]
    base = [[0, 1, 2], [1, 2, 0], [2, 0, 1]]
    rows = [0, 1, 2]; r.shuffle(rows); colsp = [0, 1, 2]; r.shuffle(colsp)
    grid = [[syms[base[i][j]] for j in colsp] for i in rows]
    gap = r.randrange(9); answer = grid[gap // 3][gap % 3]
    opts = syms + [foil]; r.shuffle(opts)
    return dict(grid=grid, gap=gap, answer=answer, options=opts, level=level)

# ---------------------------------------------------------------- shared UI
P.update(
 up='<path d="M12 19V5M5.5 11.5 12 5l6.5 6.5"/>', down='<path d="M12 5v14M5.5 12.5 12 19l6.5-6.5"/>',
 left_a='<path d="M19 12H5M11.5 5.5 5 12l6.5 6.5"/>', right_a='<path d="M5 12h14M12.5 5.5 19 12l-6.5 6.5"/>',
 puzzle='<path d="M9 4h4a2 2 0 1 1 4 0h3v5a2 2 0 1 0 0 4v7h-5a2 2 0 1 0-4 0H4v-5a2 2 0 1 1 0-4V4z"/>',
 sparkle='<path d="M12 3v5M12 16v5M3 12h5M16 12h5M6 6l3 3M15 15l3 3M6 18l3-3M15 9l3-3"/>',
 skip='<path d="M5 6l7 6-7 6zM13 6l7 6-7 6z"/>',
)
ARW = {"up": "up", "down": "down", "left": "left_a", "right": "right_a"}
ARW_NL = {"up": "omhoog", "down": "omlaag", "left": "naar links", "right": "naar rechts"}

PCSS = r'''
.pz-meta{display:flex;align-items:center;gap:6px;margin-top:12px;font-size:var(--text-xs);font-weight:600;color:var(--muted)}
.pz-meta .i{width:14px;height:14px;color:var(--success)}
.pz-say{display:flex;align-items:flex-end;gap:8px;margin-top:10px}
.pz-say .bubble{background:var(--accent-soft);box-shadow:none;font-size:var(--text-md);font-weight:600;color:var(--brand);border-radius:14px 14px 14px 4px;margin-bottom:6px;line-height:1.35}
body.m .pz-m{padding-top:10px}
body.m .res .p2-grid{max-width:250px;margin-inline:auto}
body.m .res .tower{width:180px;padding-top:44px}body.m .res .tower .lamp{height:48px}
body.m .res .res-star{margin-top:12px}
body.m .step{padding:14px 14px 14px}
.pz-q{font-size:var(--text-lg);font-weight:600;color:var(--text);margin-top:12px;line-height:1.35}
.pz-h{display:flex;align-items:center;gap:10px}.pz-h h1{margin:0}
.pz-badge{width:40px;height:40px;border-radius:12px;background:var(--gold-soft);color:var(--gold-ink);display:grid;place-items:center;flex:none;border:1.5px solid var(--gold-light)}
.pz-badge .i{width:22px;height:22px}
/* p1 */
.p1-row{position:relative;display:grid;grid-template-columns:repeat(4,1fr);gap:12px 10px;margin-top:14px;padding:14px 12px;border-radius:16px;background:linear-gradient(180deg,var(--sea-0),var(--sea-1))}
.p1-row .cell{position:relative;z-index:1;aspect-ratio:1;border-radius:50%;background:var(--surface);display:grid;place-items:center;box-shadow:0 2px 6px rgba(15,45,92,.12)}
.p1-row .cell.q{background:var(--surface);border:3px dashed var(--brand);box-shadow:none;font-size:30px;font-weight:700;color:var(--brand)}
.p1-row .cell.q.fill{border:3px solid var(--success);background:var(--success-soft)}
.p1-row .str{position:absolute;inset:0;width:100%;height:100%;z-index:0;pointer-events:none}
.p1-row .num{position:absolute;inset-block-start:-4px;inset-inline-start:-4px;width:20px;height:20px;border-radius:50%;background:var(--brand);color:#fff;font-size:11px;font-weight:700;display:grid;place-items:center}
.opts{display:grid;grid-template-columns:repeat(4,1fr);gap:10px;margin-top:12px}
.opt{position:relative;min-height:68px;border-radius:16px;border:2px solid var(--border);background:var(--surface);display:grid;place-items:center;box-shadow:var(--shadow)}
.opt.on{border:3px solid var(--brand);background:var(--accent-soft);box-shadow:0 0 0 4px color-mix(in srgb,var(--brand) 14%,transparent)}
.opt .ck{position:absolute;inset-block-start:-8px;inset-inline-end:-8px;width:24px;height:24px;border-radius:50%;background:var(--brand);color:#fff;display:grid;place-items:center}
.opt .ck .i{width:14px;height:14px;stroke-width:3}
.olab{font-size:var(--text-sm);font-weight:600;color:var(--muted);margin-top:16px}
/* p2 */
.p2-route{display:flex;flex-wrap:wrap;align-items:center;gap:8px;margin-top:12px;padding:8px 10px;border-radius:14px;background:var(--gold-soft);border:1.5px solid var(--gold-light)}
.p2-route .lb{font-size:var(--text-sm);font-weight:700;color:var(--gold-ink);display:flex;align-items:center;gap:6px}
.p2-route .lb svg{width:26px;height:26px}
.p2-route .run{display:inline-flex;gap:2px}.p2-route .run+.run{margin-inline-start:6px}
.p2-route .ar{width:30px;height:30px;border-radius:9px;background:var(--brand);color:#fff;display:grid;place-items:center}
.p2-route .ar .i{width:20px;height:20px;stroke-width:2.8}
.p2-grid{display:grid;grid-template-columns:repeat(5,1fr);gap:3px;margin-top:10px;padding:6px;border-radius:16px;background:color-mix(in srgb,var(--gold-light) 40%,#f3e2b3);box-shadow:inset 0 0 0 2px color-mix(in srgb,var(--gold-deep) 35%,transparent);position:relative}
.p2-grid .c{aspect-ratio:1;border-radius:10px;background:color-mix(in srgb,#fff 55%,var(--sand));display:grid;place-items:center;position:relative;min-width:44px;min-height:44px}
.p2-grid .c.on{background:var(--accent-soft);box-shadow:0 0 0 3px var(--brand)}
.p2-grid .c.on::after{content:"?";font-size:26px;font-weight:800;color:var(--brand)}
.p2-grid .c.path{background:color-mix(in srgb,var(--gold-light) 55%,#fff)}
.p2-grid .c.end{background:var(--gold-soft);box-shadow:0 0 0 3px var(--gold)}
.p2-grid .c .lob{width:88%;height:88%}
.p2-grid .pth{position:absolute;inset:6px;width:calc(100% - 12px);height:calc(100% - 12px);pointer-events:none;z-index:2}
.p2-grid .start-lab{position:absolute;inset-block-end:-2px;inset-inline:0;text-align:center;font-size:10.5px;font-weight:700;color:var(--brand)}
/* p3 */
.p3-wrap{display:flex;justify-content:center;margin-top:10px}
.tower{position:relative;width:214px;padding-top:48px}
.tower .lamp{position:absolute;top:0;left:50%;transform:translateX(-50%);width:110px;height:54px}
.tower .body{position:relative;border-radius:16px 16px 10px 10px;padding:10px 14px 14px;background:repeating-linear-gradient(180deg,var(--surface) 0 34px,color-mix(in srgb,var(--coral) 82%,#fff) 34px 52px);box-shadow:inset 0 0 0 3px var(--brand),var(--shadow-lg)}
.tower .win{display:grid;grid-template-columns:repeat(3,1fr);gap:6px;padding:8px;border-radius:14px;background:var(--brand)}
.tower .w{aspect-ratio:1;border-radius:10px;background:radial-gradient(circle at 50% 45%,#fff8d6 0 45%,#f6dd7f 100%);display:grid;place-items:center}
.tower .w.gap{background:var(--sea-6);border:3px dashed var(--gold-light);font-size:30px;font-weight:800;color:var(--gold-light)}
.tower .w.gap.fill{background:radial-gradient(circle at 50% 45%,#fff8d6 0 45%,#f6dd7f 100%);border:3px solid var(--success)}
.tower .rowhint{display:flex;justify-content:space-between;margin-top:8px;font-size:var(--text-xs);font-weight:600;color:var(--brand)}
.rule{display:flex;gap:8px;align-items:center;justify-content:center;margin-top:8px;font-size:var(--text-sm);font-weight:600;color:var(--brand)}
.rule .mini{display:inline-grid;grid-template-columns:repeat(3,10px);gap:2px}.rule .mini i{width:10px;height:10px;border-radius:2px;background:var(--sea-2)}.rule .mini i.h{background:var(--gold)}
/* result */
.res-star{margin-top:16px;border-radius:16px;background:var(--gold-soft);border:2px solid var(--gold-light);padding:14px 16px;display:flex;gap:12px;align-items:center}
.res-star .s{width:48px;height:48px;border-radius:50%;background:var(--gold-light);color:var(--gold-ink);display:grid;place-items:center;flex:none}
.res-star .s .i{width:28px;height:28px;fill:currentColor}
.res-star b{display:block;font-size:var(--text-xl);line-height:1.25;color:var(--gold-ink);font-weight:700}
.res-fun{font-size:var(--text-sm);color:var(--text);margin-top:12px;line-height:1.5}
.res-note{display:flex;gap:8px;align-items:center;font-size:var(--text-xs);color:var(--muted);margin-top:12px}
.res-note .i{width:14px;height:14px}
.confetti{position:absolute;inset:0;pointer-events:none;overflow:hidden;border-radius:inherit;z-index:0}
.res>*:not(.confetti),.step>.pz-h,.step>.pz-split{position:relative;z-index:1}
.confetti i{position:absolute;width:8px;height:12px;border-radius:2px;opacity:.85}
.chest{width:100%;height:100%}
.skip{font-size:var(--text-sm);font-weight:600;color:var(--brand);display:inline-flex;align-items:center;gap:6px;min-height:44px;padding:0 10px;border-radius:var(--radius-sm)}
.skip .i{width:16px;height:16px}
body.m .mlob{min-height:0;padding:6px 4px 8px}
body.c .step{padding:20px 26px 18px}
body.c .opts{max-width:460px}
body.c .p2-grid{max-width:340px;margin-inline:auto}
body.c .p1-row{grid-template-columns:repeat(8,1fr);padding:18px 14px}
body.c .p1-row .str{display:none}
body.c .p1-row::before{content:"";position:absolute;left:30px;right:30px;top:50%;border-top:3px dashed #a6851c}
body.c .p1-row .cell{z-index:1}
body.c .pz-split{display:grid;grid-template-columns:1fr 1fr;gap:20px;align-items:start;margin-top:6px}
body.c .tower{width:260px}
body.c .res-star b{font-size:var(--text-lg)}
'''

CHEST = ('<svg class="chest" viewBox="0 0 48 48" aria-hidden="true"><path d="M8 22h32v18a3 3 0 0 1-3 3H11a3 3 0 0 1-3-3z" fill="#a6851c" stroke="#0f2d5c" stroke-width="2"/>'
         '<path d="M8 22c0-8 6-13 16-13s16 5 16 13z" fill="#c9a227" stroke="#0f2d5c" stroke-width="2"/>'
         '<path d="M8 27h32" stroke="#0f2d5c" stroke-width="2"/><rect x="20" y="24" width="8" height="9" rx="2" fill="#e4c65a" stroke="#0f2d5c" stroke-width="2"/>'
         '<path d="M14 9l-2-4M24 6V2M34 9l2-4" stroke="#c9a227" stroke-width="2.4" stroke-linecap="round"/></svg>')
ROCK = '<svg width="34" height="34" viewBox="0 0 48 48" aria-hidden="true"><path d="M8 36c-2-8 4-16 12-18 8-3 18 1 21 9 2 6-2 11-8 11H14c-3 0-5-1-6-2z" fill="#8b97a8" stroke="#0f2d5c" stroke-width="2"/><path d="M18 24c3-2 7-2 10 0" stroke="#fff" stroke-width="2" opacity=".5" fill="none"/></svg>'
WEED = '<svg width="34" height="34" viewBox="0 0 48 48" aria-hidden="true"><path d="M16 44c-4-10 6-14 0-26M26 44c4-10-6-16 2-30M34 44c-2-8 6-10 2-20" stroke="#2f7a5a" stroke-width="3.4" fill="none" stroke-linecap="round"/></svg>'

def header(code, m=True):
    right = f'<span class="idp">{ic("users")}Groep 8 · <span class="code">{code}</span></span>' + ('' if m else f'<a class="btn">{ic("pause")}Pauze</a>')
    return (f'<header class="hd"><div class="wm"><img src="{pupil.LOGO}" alt=""><b>Lobsy</b><span>Ontdek wie jij bent</span></div>'
            f'<div class="hr">{right}</div></header>')

def page(title, body, depth, kind, code, lob_x=300):
    pupil.header = lambda m=True, login=False: header(code, m)
    html = pupil.page(title, body, depth, kind, lob_x=lob_x)
    return html.replace("</style>", PCSS + "</style>", 1)

def prog(after, n):
    import re
    h = pupil.qprog(after, (after - 1) // 15 + 1, after // 6, f"Puzzel {n} van 3")
    return re.sub(r'<span class="muted">[^<]*</span>', f'<span class="muted">· {after} van 60 klaar</span>', h, count=1)

def mob(card, depth, title, foot, code, after, n):
    body = f'{prog(after, n)}<main class="mstage pz-m"><section class="card step"><p class="demo cdemo">Voorbeelddata</p>{card}</section></main><div class="mfoot">{foot}</div>'
    return page(title, body, depth, "m", code)
def say(text, gone, size=52):
    return f'<div class="pz-say">{lobster(size, gone, False, None, "Lobsy de kreeft")}<p class="bubble">{text}</p></div>'

def chips_line():
    return f'<p class="pz-meta">{ic("check")}Geen tijd · geen cijfer · fout is niet erg</p>'

def foot_q(label="Klaar"):
    return f'<a class="skip">{ic("skip")}Sla over</a><span style="flex:1"></span><a class="btn pri">{label}{ic("right")}</a>'
def foot_r(label):
    return f'<a class="btn pri">{label}{ic("right")}</a>'

CONF = "".join(f'<i style="left:{x}%;top:{y}%;background:{c};transform:rotate({rt}deg)"></i>' for x, y, c, rt in
               [(1, 30, "#E69F00", 20), (96.5, 26, "#56B4E9", -30), (1.2, 48, "#009E73", 40), (96.8, 52, "#CC79A7", -15), (45, .6, "#F0E442", 60), (60, .4, "#D55E00", -50), (1, 70, "#0072B2", 25), (96.6, 76, "#E69F00", -35)])

# ---------------------------------------------------------------- puzzle 1: de schelpenrij
P1_STR = ('<svg class="str" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">'
          '<path d="M8 27H92L8 73H92" fill="none" stroke="#a6851c" stroke-dasharray="4 4" vector-effect="non-scaling-stroke" style="stroke-width:3px" stroke-linejoin="round"/></svg>')
def head(n, kicker, title):
    return f'<div class="pz-h"><span class="pz-badge">{ic("puzzle")}</span><div><p class="world">{kicker}</p><h1>{title}</h1></div></div>'
def ask(text, m, gone):
    return say(text, gone) if m else f'<p class="pz-q">{text}</p>'
def p1_row(g, filled=False, size=44):
    cells = "".join(f'<div class="cell">{tok(t, size)}</div>' for t in g["shown"])
    last = f'<div class="cell q fill">{tok(g["answer"], size)}</div>' if filled else '<div class="cell q" aria-label="Lege plek">?</div>'
    return f'<div class="p1-row" role="img" aria-label="Een rij van 8 plekjes, de laatste is leeg">{P1_STR}{cells}{last}</div>'
def opts(g, sel=None, size=46):
    h = ""
    for t in g["options"]:
        on = sel is not None and t == sel
        ck = f'<span class="ck">{ic("check")}</span>' if on else ""
        h += f'<button class="opt{" on" if on else ""}" aria-pressed="{"true" if on else "false"}">{tok(t, size)}{ck}</button>'
    return f'<div class="opts" role="group" aria-label="Kies wat er hierna komt">{h}</div>'
def p1_card(g, m=True, sel=True):
    return (head(1, "Puzzel 1 · Koraalrif", "De schelpenrij")
            + ask("Wat komt er op de plek van het vraagteken?", m, 2) + p1_row(g)
            + opts(g, g["answer"] if sel else None) + chips_line())
def star(text, fun):
    return (f'<div class="res-star"><span class="s">{ic("star")}</span><b>{text}</b></div><p class="res-fun">{fun}</p>'
            f'<p class="res-note">{ic("check")}Dit is geen toets. Je krijgt geen cijfer.</p>')
S1 = ("Jij ziet snel patronen.", "Patronen zitten overal: in muziek, in computercode en in het weer. Daar heb je veel aan!")
S2 = ("Jij houdt een route goed in je hoofd.", "Handig als je later bouwt, bezorgt, vliegt of huizen ontwerpt.")
S3 = ("Jij denkt stap voor stap na.", "Zo werken ook detectives, dokters en monteurs: eerst kijken, dan slim kiezen.")
def p1_res_card(g, m=True):
    return (f'<div class="confetti">{CONF}</div>' + head(1, "Puzzel 1 klaar", "Goed gekeken!")
            + ask("Wauw, jij hebt scherpe ogen!", m, 2) + p1_row(g, True) + star(*S1))

# ---------------------------------------------------------------- puzzle 2: de schatkaart
def route(g):
    runs = "".join(f'<span class="run" aria-label="{n} keer {ARW_NL[d]}">' + "".join(f'<span class="ar">{ic(ARW[d])}</span>' for _ in range(n)) + '</span>' for d, n in g["runs"])
    return f'<div class="p2-route" aria-label="Route"><span class="lb">Route</span>{runs}</div>'
def grid2(g, sel=None, solved=False):
    pts = g["path"]; cells = ""
    for i in range(5):
        for j in range(5):
            c = (i, j); cls = "c"; inner = ""
            if c == g["start"]:
                cls += " st0"; inner = f'<span class="lob">{lobster(40, 5)}</span><span class="start-lab">start</span>'
            elif solved and c == g["end"]:
                cls += " end"; inner = CHEST
            elif solved and c in pts:
                cls += " path"
            elif c in g["rocks"]:
                inner = ROCK if (i + j) % 2 else WEED
            if not solved and sel == c: cls += " on"
            cells += f'<button class="{cls}" aria-label="Rij {i + 1}, vak {j + 1}">{inner}</button>'
    svg = ""
    if solved:
        d = " ".join(f'{"M" if k == 0 else "L"}{(p[1] + .5) * 20:.1f} {(p[0] + .5) * 20:.1f}' for k, p in enumerate(pts))
        svg = (f'<svg class="pth" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true"><path d="{d}" fill="none" stroke="#f54a1b" '
               f'stroke-dasharray="6 5" stroke-linecap="round" stroke-linejoin="round" vector-effect="non-scaling-stroke" style="stroke-width:4px"/></svg>')
    return f'<div class="p2-grid" role="grid" aria-label="Schatkaart, 5 bij 5 vakjes">{cells}{svg}</div>'
def p2_card(g, m=True, sel=True):
    q = "Ik volg de pijlen. Waar ligt de schat? Tik het vakje."
    if m:
        return head(2, "Puzzel 2 · Schatgrot", "De schatkaart") + ask(q, m, 5) + route(g) + grid2(g, g["end"] if sel else None) + chips_line()
    return head(2, "Puzzel 2 · Schatgrot", "De schatkaart") + ask(q, m, 5) + route(g) + grid2(g, g["end"] if sel else None) + chips_line()
def p2_res_card(g, m=True):
    h = f'<div class="confetti">{CONF}</div>' + head(2, "Puzzel 2 klaar", "Schat gevonden!")
    if m:
        return h + ask("Mijn schat! Jij bent een echte ontdekker.", m, 5) + grid2(g, solved=True) + star(*S2)
    return h + f'<div class="pz-split"><div>{route(g)}{star(*S2)}</div>{grid2(g, solved=True)}</div>'

# ---------------------------------------------------------------- puzzle 3: de vuurtorenlampen
LAMP = ('<svg class="lamp" viewBox="0 0 120 64" aria-hidden="true"><path d="M60 36 0 8v56zM60 36l60-28v56z" fill="#f6dd7f" opacity=".45"/>'
        '<rect x="40" y="28" width="40" height="36" rx="6" fill="#0f2d5c"/><rect x="47" y="34" width="26" height="22" rx="4" fill="#fff3b8"/>'
        '<path d="M36 28h48L60 6z" fill="#f54a1b" stroke="#0f2d5c" stroke-width="3" stroke-linejoin="round"/><circle cx="60" cy="5" r="4" fill="#0f2d5c"/></svg>')
def tower(g, filled=False, size=40):
    w = ""
    for k in range(9):
        t = g["grid"][k // 3][k % 3]
        if k == g["gap"]:
            w += f'<div class="w gap fill">{tok(t, size)}</div>' if filled else '<div class="w gap" aria-label="Leeg raam">?</div>'
        else:
            w += f'<div class="w">{tok(t, size)}</div>'
    return f'<div class="p3-wrap"><div class="tower">{LAMP}<div class="body"><div class="win" role="img" aria-label="Vuurtoren met 9 ramen in 3 rijen, 1 raam is leeg">{w}</div></div></div></div>'
RULE = ('<p class="rule"><span class="mini" aria-hidden="true"><i class="h"></i><i class="h"></i><i class="h"></i><i></i><i></i><i></i><i></i><i></i><i></i></span>'
        '<span class="mini" aria-hidden="true"><i class="h"></i><i></i><i></i><i class="h"></i><i></i><i></i><i class="h"></i><i></i><i></i></span>'
        'Elke rij en elke kolom: elke lamp 1 keer.</p>')
def p3_card(g, m=True, sel=True):
    q = "Eén lamp is uit. Welke lamp past in het lege raam?"
    if m:
        return head(3, "Puzzel 3 · Vuurtoren", "De vuurtorenlampen") + ask(q, m, 7) + tower(g) + RULE + opts(g, g["answer"] if sel else None, 42) + chips_line()
    return (head(3, "Puzzel 3 · Vuurtoren", "De vuurtorenlampen")
            + f'<div class="pz-split"><div>{tower(g, size=46)}</div><div>{ask(q, m, 7)}{RULE}{opts(g, g["answer"] if sel else None)}{chips_line()}</div></div>')
def p3_res_card(g, m=True):
    h = f'<div class="confetti">{CONF}</div>' + head(3, "Puzzel 3 klaar", "Alle lampen branden!")
    if m:
        return h + ask("Alles brandt weer. Dankjewel!", m, 7) + tower(g, True, 32) + star(*S3)
    return h + f'<div class="pz-split"><div>{tower(g, True, 46)}</div><div>{star(*S3)}</div></div>'

# ---------------------------------------------------------------- desktop (Chromebook 1366x768)
def rail(n):
    items = [("Het koraalrif", "Hoe ben jij? · 15 vragen"), ("Puzzel 1", "De schelpenrij"), ("De schatgrot", "Wat doe je graag? · 15"),
             ("Puzzel 2", "De schatkaart"), ("Pauze-eiland", "Wat vind je leuk?"), ("De vuurtoren", "Wat vind je belangrijk? · 15"),
             ("Puzzel 3", "De vuurtorenlampen"), ("De lagune", "Waar voel je je thuis? · 15"), ("Dit ben jij", "Jouw verhaal")]
    cur = {1: 1, 2: 3, 3: 6}[n]; gone = {1: 2, 2: 5, 3: 7}[n]
    h = '<ol class="route">'
    for k, (t, sub) in enumerate(items):
        pz = t.startswith("Puzzel")
        if k < cur: h += f'<li class="st done"><span class="dot">{ic("check")}</span><div><b>{t}</b><small>{sub}</small></div></li>'
        elif k == cur: h += f'<li class="st now" aria-current="step"><span class="mk">{lobster(30, gone)}</span><div><b>{t}</b><small>{sub}</small></div></li>'
        else: h += f'<li class="st todo"><span class="dot">{ic("puzzle") if pz else ""}</span><div><b>{t}</b><small>{sub}</small></div></li>'
    h += '</ol>'
    segs = "".join(f'<i class="{"off" if k < gone else ""}"></i>' for k in range(10))
    return (f'<aside class="card rail"><h2>Jouw reis</h2><p class="cnt">Puzzelpauze {n} van 3</p>{h}'
            f'<div class="layers"><span class="segs">{segs}</span><span>{gone} van 10 eraf</span></div>'
            f'<p class="saved">{ic("check")}Alles is bewaard. Pauze mag altijd.</p></aside>')
def desk(n, card, say, depth, code, gone, foot):
    lz = f'<div class="lz"><p class="bubble">{say}</p>{lobster(190, gone, False, None, "Lobsy de kreeft")}<span class="shadow"></span></div>'
    body = f'<main class="stage">{rail(n)}<section class="card step">{card}<div class="foot">{foot}</div></section>{lz}</main>'
    return page(f"Puzzel {n}", body, depth, "c", code)

CODES = ("K7Q-M2P", "B8N-3KD")
SAY = {1: "Puzzeltijd! Geen toets, gewoon voor de lol.", 2: "Ik heb een oude schatkaart gevonden! Zoek je mee?", 3: "Help je mij? Dan zien de schepen ons weer."}
SAY_R = {1: "Wauw, jij hebt scherpe ogen!", 2: "Mijn schat! Jij bent een echte ontdekker.", 3: "Alles brandt weer. Dankjewel!"}
NEXT = {1: "Door naar de schatgrot", 2: "Naar het Pauze-eiland", 3: "Door naar de lagune"}
META = {1: (15, 2, 3), 2: (30, 5, 6), 3: (45, 7, 8)}  # after-question, plates gone, scene depth
GEN = {1: gen_p1, 2: gen_p2, 3: gen_p3}
QC = {1: p1_card, 2: p2_card, 3: p3_card}; RC = {1: p1_res_card, 2: p2_res_card, 3: p3_res_card}

def mq(n, code):
    after, gone, depth = META[n]; g = GEN[n](code)
    return mob(QC[n](g, True), depth, f"Puzzel {n}", foot_q(), code, after, n)
def mr(n, code):
    after, gone, depth = META[n]; g = GEN[n](code)
    return mob('<div class="res">' + RC[n](g, True) + '</div>', depth, f"Puzzel {n} klaar", foot_r(NEXT[n]), code, after, n)
def dq(n, code):
    after, gone, depth = META[n]; g = GEN[n](code)
    foot = f'<a class="skip">{ic("skip")}Sla over</a><span class="sp"></span><a class="btn pri">Klaar{ic("right")}</a>'
    return desk(n, QC[n](g, False), SAY[n], depth, code, gone, foot)

SLUG = {1: "p1-schelpenrij", 2: "p2-schatkaart", 3: "p3-vuurtorenlampen"}
SCREENS = []
for n in (1, 2, 3):
    SCREENS += [(f"{SLUG[n]}-mobiel-vraag", lambda n=n: mq(n, CODES[0]), "m"),
                (f"{SLUG[n]}-mobiel-resultaat", lambda n=n: mr(n, CODES[0]), "m"),
                (f"{SLUG[n]}-desktop", lambda n=n: dq(n, CODES[0]), "c"),
                (f"{SLUG[n]}-variant-b-mobiel-vraag", lambda n=n: mq(n, CODES[1]), "m")]

def describe(n, code):
    g = GEN[n](code)
    nm = tname
    if n == 1:
        return f'patroon {g["family"]} (start op stap {g["offset"] + 1}) · antwoord: {nm(g["answer"])} · antwoord op knop {g["options"].index(g["answer"]) + 1} van 4'
    if n == 2:
        rs = ", ".join(f'{k}× {ARW_NL[d]}' for d, k in g["runs"])
        return f'start rij {g["start"][0] + 1}, vak {g["start"][1] + 1} · route: {rs} · schat: rij {g["end"][0] + 1}, vak {g["end"][1] + 1}'
    return f'leeg raam: rij {g["gap"] // 3 + 1}, kolom {g["gap"] % 3 + 1} · antwoord: {nm(g["answer"])} · antwoord op knop {g["options"].index(g["answer"]) + 1} van 4'
