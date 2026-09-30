"""Carriere redesign (fase 1): /carriere in de stijl van De ontdekkingsreis.
Hergebruikt tokens, scene-kleuren, kreeft + schaalplaten uit base/ontdekkingsreis_build.py (kopie van docs/mockups/ontdekkingsreis/build.py op docs/mijn-paspoort).
Metafoor: de kreeft klimt steen voor steen uit de diepte naar het licht (je droombaan).
Op elke gehaalde steen laat hij zijn oude schaal achter en groeit hij een nieuwe (goud).
Run: python3 cr_build.py   (playwright + /usr/bin/google-chrome). Alle data = Voorbeelddata."""
import pathlib, importlib.util, random
from playwright.sync_api import sync_playwright
d = pathlib.Path(__file__).parent
_spec = importlib.util.spec_from_file_location("ob", d / "base" / "ontdekkingsreis_build.py")
ob = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(ob)
P = ob.P
P.update(dict(
 antenna='<path d="M7 20a5 5 0 0 1 10 0z"/><path d="M10 15.4C9.4 10.5 7.3 6.4 3.5 3.5M14 15.4c.6-4.9 2.7-9 6.5-11.9"/>',
 claw='<path d="M4 21l5.5-5.5"/><path d="M9.5 15.5C8 10.5 11 5 17 3.5c.6 3-1.2 5.8-4.2 6.8"/><path d="M9.5 15.5c4.6 1.3 9.6-.8 11-5.2-3-.9-6 .1-7.7 2.3"/>',
 stone='<path d="M3.5 16.5c0-4.2 4-7.5 9-7.5 4.6 0 8 2.6 8 6 0 2.9-3.1 4.8-8.3 4.8-5.3 0-8.7-1-8.7-3.3z"/><path d="M8 12.5c1.2-.8 2.6-1.2 4-1.2"/>',
 star='<path d="m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z"/>',
 ext='<path d="M14 4h6v6M20 4l-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5"/>',
 undo='<path d="M9 14 4 9l5-5"/><path d="M4 9h10a6 6 0 0 1 0 12h-3"/>',
 file='<path d="M14 3H6a1 1 0 0 0-1 1v16a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V8z"/><path d="M14 3v5h5M9 13h6M9 17h4"/>',
 x='<path d="M6 6l12 12M18 6 6 18"/>',
))
ic = ob.ic; lobster = ob.lobster; SHARD = ob.SHARD

# ---------------------------------------------------------------- voorbeelddata (sluit aan op pp-d4)
DREAM = "MBO-verpleegkundige"
NOW = ("Zorghulp (vrijwillig)", "Je startsteen")
STEPS = [  # (nr, kort, titel, sub)
 (1, "Kennismaken", "Kennismaken met de zorg", "Korte cursus"),
 (2, "Helpende", "Helpende Zorg & Welzijn", "Niveau 2"),
 (3, "Verzorgende IG", "Verzorgende IG", "Niveau 3"),
]
NS = len(STEPS)

# ---------------------------------------------------------------- extra CSS (tokens van ob.CSS)
CSS = ob.CSS + '''
.stage{grid-template-columns:300px 640px 1fr}
.route.up::before{background:linear-gradient(180deg,var(--sea-7),var(--sea-4) 35%,var(--sea-1) 70%,var(--gold-light))}
.st.goal .dot{background:var(--gold-soft);border-color:var(--gold);color:var(--gold-ink)}
.st.goal b{color:var(--gold-ink)}
.route.up~.layers .segs i{background:var(--pearl)}.route.up~.layers .segs i.gold{background:var(--gold-light)}.layers .segs i.grow{background:transparent;box-shadow:inset 0 0 0 1.5px var(--brand);border:0;background-image:none}
.ebr{display:flex;align-items:center;justify-content:space-between;gap:12px}
/* growing shells stepper (zelfde regels als paspoort 04.2) */
.shells{display:flex;align-items:flex-start;margin-top:18px;position:relative}
.shells li{flex:1;display:flex;flex-direction:column;align-items:center;text-align:center;position:relative;gap:6px;min-width:0}
.shells li::before{content:"";position:absolute;top:var(--mid);left:-50%;right:50%;height:2px;background:var(--border);z-index:0}
.shells li:first-child::before{display:none}
.shells li.done::before,.shells li.now::before{background:var(--brand)}
.shells .sh{position:relative;z-index:1;display:flex;align-items:center;justify-content:center;border-radius:50% 50% 46% 46%/58% 58% 42% 42%;background:var(--surface);border:2px dotted var(--border);color:var(--muted);font-size:var(--text-xs);font-weight:600}
.shells .sh .i{width:15px;height:15px;stroke-width:2.6}
.shells li.done .sh{background:var(--brand);border:2px solid var(--brand);color:#fff}
.shells li.now .sh{background:var(--accent-soft);border:2px dashed var(--brand);color:var(--brand)}
.shells li.goal .sh{background:var(--gold-soft);border:2px solid var(--gold);color:var(--gold-ink)}
.shells b{font-size:var(--text-xs);font-weight:600;color:var(--text);line-height:1.2}.shells small{font-size:var(--text-xs);color:var(--muted);line-height:1.2;display:block}
.shells li.now b{color:var(--brand)}.shells li.goal b{color:var(--gold-ink)}
/* next-step box */
.nxt{border-radius:var(--radius);background:var(--bg);padding:16px 18px;display:flex;flex-direction:column;gap:10px}
.nxt h2{font-size:var(--text-lg);font-weight:600;color:var(--brand);line-height:1.3}
.facts3{display:flex;flex-direction:column;gap:6px}
.facts3 li{display:flex;align-items:center;gap:10px;font-size:var(--text-sm)}
.facts3 .i{width:17px;height:17px;color:var(--brand)}.facts3 .w{color:var(--warn)}.facts3 .s{color:var(--success)}
.nxt .foot{margin-top:4px}
.have li{display:flex;align-items:center;gap:10px;font-size:var(--text-sm);min-height:34px;border-bottom:1px solid var(--pearl)}
.have li:last-child{border-bottom:0}.have li .i{width:16px;height:16px;color:var(--success)}.have li .sp{flex:1}.have li small{font-size:var(--text-xs);color:var(--muted);white-space:nowrap}
.ai{font-size:var(--text-xs);color:var(--muted);display:flex;align-items:center;gap:6px}.ai .i{width:14px;height:14px}
/* step detail */
.back{display:inline-flex;align-items:center;gap:4px;font-size:var(--text-sm);font-weight:600;color:var(--brand);min-height:32px;margin-bottom:4px}.back .i{width:16px;height:16px}
.two{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.blk{border:1px solid var(--pearl);border-radius:var(--radius);padding:12px 14px;display:flex;flex-direction:column;gap:6px}
.blk h2{font-size:var(--text-md);font-weight:600;color:var(--brand)}
.blk .qs{margin-top:-4px}
.gaps li{display:flex;align-items:flex-start;gap:10px;font-size:var(--text-sm);line-height:1.35;padding:3px 0}
.gaps li .i{width:17px;height:17px;margin-top:1px}.gaps li.miss .i{color:var(--warn)}.gaps li.ok .i{color:var(--success)}.gaps li.ok span small{color:var(--muted);font-size:var(--text-xs)}
.eyb2{font-size:var(--text-xs);font-weight:600;letter-spacing:.06em;text-transform:uppercase;color:var(--muted);display:flex;align-items:center;gap:6px}.eyb2 .i{width:14px;height:14px}
.fit{display:flex;align-items:center;gap:10px;font-size:var(--text-sm)}.fit .i{color:var(--brand)}
.pill.out{background:var(--surface);color:var(--text);box-shadow:inset 0 0 0 1px var(--border)}
.crs{display:grid;grid-template-columns:1fr 1fr;gap:10px}
.co{border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 12px;display:flex;flex-direction:column;gap:4px}
.co.free{border-color:var(--success);background:var(--success-soft)}
.co .tp{display:flex;align-items:center;gap:8px;flex-wrap:wrap}.co .tp b{font-size:var(--text-sm);font-weight:600}
.co small{font-size:var(--text-xs);color:var(--muted)}
.co .why{display:flex;align-items:center;gap:6px;font-size:var(--text-xs);color:var(--text)}.co .why .i{width:14px;height:14px;color:var(--warn)}
.co .lnk{min-height:32px}
/* climb zone (desktop) */
.climb{position:relative;height:748px;align-self:stretch}
.climb>svg{position:absolute;inset:0}
.rl{position:absolute;transform:translateX(-50%);font-size:var(--text-xs);font-weight:600;background:var(--surface);color:var(--text);border-radius:var(--radius-pill);padding:2px 10px;white-space:nowrap;box-shadow:var(--shadow);display:inline-flex;align-items:center;gap:5px}
.rl .i{width:12px;height:12px;stroke-width:2.6}.rl.done .i{color:var(--success)}.rl.now{background:var(--brand);color:#fff}
.rl.dream{background:var(--gold-soft);color:var(--gold-ink);box-shadow:inset 0 0 0 1.5px var(--gold),var(--shadow);font-size:var(--text-sm);padding:4px 12px}.rl.dream .i{width:14px;height:14px;color:var(--gold)}
.rl.q{background:var(--surface);color:var(--muted);font-weight:400}
.clob{position:absolute}
.clob .lob{display:block;animation:bob 6s ease-in-out infinite}
.cbub{position:absolute;max-width:210px;font-size:var(--text-sm)}
.climb .shadow{position:absolute;width:90px;margin:0}
@media (prefers-reduced-motion: reduce){.clob .lob{animation:none}}
.tr2{fill:none;stroke-linecap:round}
/* dialog (LobsyFriendlyDialog) */
.scrim{position:fixed;inset:0;background:var(--brand-deep);opacity:.45;z-index:70}
.dlg{position:fixed;left:50%;top:92px;transform:translateX(-50%);width:560px;z-index:80;padding:22px 24px 18px;display:flex;flex-direction:column;gap:14px}
.dlg .hdr{display:flex;align-items:center;gap:12px}.dlg .hdr h2{font-size:var(--text-xl);font-weight:700;color:var(--brand);line-height:1.25}
.dlg .cls{margin-inline-start:auto;width:44px;height:44px;display:flex;align-items:center;justify-content:center;color:var(--muted)}
.what{border-radius:var(--radius-sm);background:var(--bg);padding:12px 14px}
.what h3{font-size:var(--text-sm);font-weight:600;margin-bottom:4px}
.what li{display:flex;gap:10px;align-items:flex-start;font-size:var(--text-sm);padding:3px 0;line-height:1.35}.what li .i{width:16px;height:16px;margin-top:2px;color:var(--success)}.what li .i.m{color:var(--muted)}
.sugg{display:flex;flex-direction:column;gap:8px}
.sg{display:flex;align-items:center;gap:12px;border:1px solid var(--border);border-radius:var(--radius-sm);padding:8px 12px;min-height:56px;background:var(--surface)}
.sg.on{border-color:var(--brand);background:var(--accent-soft)}
.sg .ico{width:36px;height:36px;border-radius:10px;background:var(--bg);color:var(--brand);display:flex;align-items:center;justify-content:center;flex:none}
.sg.on .ico{background:var(--surface)}
.sg b{display:block;font-size:var(--text-sm);font-weight:600}.sg small{display:block;font-size:var(--text-xs);color:var(--muted)}.sg .sp{flex:1}
.sg .rd{width:20px;height:20px;border-radius:50%;border:2px solid var(--border);flex:none}.sg.on .rd{border:6px solid var(--brand)}
.or{display:flex;align-items:center;gap:10px;font-size:var(--text-xs);color:var(--muted)}.or::before,.or::after{content:"";flex:1;height:1px;background:var(--pearl)}
.inp .i{color:var(--muted);margin-inline-end:8px}
/* mobile */
body.m .scene{top:56px}
.mclimb{position:relative;height:150px;margin:0 -12px}
.mclimb>svg{position:absolute;inset:0}
.mbub{position:absolute;left:12px;top:8px;max-width:150px;font-size:var(--text-sm);line-height:1.35;padding:8px 12px}
body.m .two,body.m .crs{grid-template-columns:1fr}
body.m .shells b{font-size:var(--text-xs)}body.m .shells small{display:none}
body.m .nxt{padding:14px}
body.m .bn{grid-template-columns:repeat(5,1fr)}
body.d .bn{grid-template-columns:repeat(5,1fr)}
body.m .dlg{width:auto;left:8px;right:8px;transform:none;top:64px}

/* contactverzoeken + hoe werkt lobsy */
.req{border:1px solid var(--border);border-radius:var(--radius);padding:14px 16px;display:flex;flex-direction:column;gap:8px}
.req.open{border-color:var(--brand);box-shadow:inset 3px 0 0 var(--brand)}
.req .top{display:flex;align-items:center;gap:10px;flex-wrap:wrap}.req .top b{font-size:var(--text-md);font-weight:600;color:var(--brand)}.req .top .sp{flex:1}
.req .ico{width:36px;height:36px;border-radius:10px;background:var(--bg);color:var(--brand);display:flex;align-items:center;justify-content:center;flex:none;font-weight:600}
.req q{display:block;font-size:var(--text-sm);color:var(--text);background:var(--bg);border-radius:var(--radius-sm);padding:8px 12px;quotes:none}
.req .acts{display:flex;align-items:center;gap:10px;flex-wrap:wrap}.req .acts .sp{flex:1}
.req .when{font-size:var(--text-xs);color:var(--muted);display:flex;align-items:center;gap:6px}.req .when .i{width:14px;height:14px}
.pill.wait{background:var(--warn-soft);color:var(--warn)}.pill.neutral{background:var(--pearl);color:var(--muted)}
.how li{display:flex;align-items:flex-start;gap:10px;font-size:var(--text-sm);padding:6px 0;line-height:1.4}.how li .i{width:18px;height:18px;color:var(--brand);margin-top:1px}
.shared{display:grid;grid-template-columns:auto 1fr;gap:4px 14px;font-size:var(--text-sm);background:var(--bg);border-radius:var(--radius-sm);padding:10px 14px}.shared dt{color:var(--muted)}.shared dd{margin:0;font-weight:600}
.map5{display:flex;flex-direction:column;gap:8px}
.m5{display:flex;align-items:center;gap:12px;border:1px solid var(--border);border-radius:var(--radius-sm);padding:8px 12px;min-height:60px}
.m5 .nr{width:32px;height:32px;border-radius:50% 50% 46% 46%/58% 58% 42% 42%;display:flex;align-items:center;justify-content:center;font-size:var(--text-xs);font-weight:600;background:var(--surface);border:2px dotted var(--border);color:var(--muted);flex:none}
.m5.done .nr{background:var(--brand);border:2px solid var(--brand);color:#fff}.m5.now{border-color:var(--brand);background:var(--accent-soft)}.m5.now .nr{border:2px dashed var(--brand);color:var(--brand);background:var(--surface)}
.m5 .nr .i{width:14px;height:14px;stroke-width:2.6}.m5 b{display:block;font-size:var(--text-sm);font-weight:600}.m5 small{display:block;font-size:var(--text-xs);color:var(--muted)}.m5 .sp{flex:1}
.m5 .lnk{min-height:32px}
body.m .req .acts .btn{flex:1}
'''

# ---------------------------------------------------------------- shared parts
ACTIVE = ["Carrière"]
def nav(m):
    it = [("compass", "De ontdekkingsreis", "Reis"), ("book", "Mijn Paspoort", "Paspoort"), ("sun", "Carrière", "Carrière"),
          ("pin", "Banenkaart", "Banenkaart"), ("clip", "Sollicitaties", "Sollicitaties")]  # Dennis' nav order (separate add-on)
    return ('<nav class="bn" aria-label="Hoofdmenu">' + "".join(
        f'<a class="{"on" if t==ACTIVE[0] else ""}"{" aria-current=page" if t==ACTIVE[0] else ""}>{ic(i)}{(ms if m else t)}</a>' for i, t, ms in it) + '</nav>')

def climb_scene(W, H, mode="plan"):
    """Achtergrond: licht boven (droombaan), diep onder (waar je nu zit). Alleen token-kleuren."""
    r = random.Random(W + H); e = []
    big = W > 600
    for k in range(4 if big else 3):  # lichtstralen uit de rechterbovenhoek naar de droomsteen
        x = W * (.62 + .09 * k)
        e.append(f'<path d="M{x} 0L{x+W*.05} 0L{x-W*.12} {H*.75}L{x-W*.2} {H*.75}Z" style="fill:var(--sun);opacity:{.10 if mode=="done" else .07}"/>')
    e.append(f'<ellipse cx="{W*.82}" cy="0" rx="{W*.3}" ry="{H*.22}" style="fill:var(--sun);opacity:{.22 if mode=="done" else .14}"/>')
    for _ in range(40 if big else 14):  # zwevende lichtjes in de diepte
        e.append(f'<circle cx="{r.uniform(0,W):.0f}" cy="{r.uniform(H*.45,H):.0f}" r="{r.uniform(.8,2):.1f}" style="fill:var(--surface);opacity:{r.uniform(.12,.4):.2f}"/>')
    if big:
        for k in range(9):
            x = r.uniform(0, W * .55); e.append(ob.weed(x, H + 5, r.uniform(80, 170), r.uniform(-16, 16), r.uniform(5, 8), .4))
        e.append(ob.rock(W * .05, H, 160, 80, .45)); e.append(ob.rock(W * .5, H, 120, 40, .35))
    return (f'<div class="scene" style="background:linear-gradient(180deg,var(--sky) 0%,var(--sea-1) 16%,var(--sea-3) 45%,var(--sea-6) 78%,var(--sea-8) 100%)" aria-hidden="true">'
            f'<svg viewBox="0 0 {W} {H}" preserveAspectRatio="none" width="100%" height="100%">{"".join(e)}</svg></div>')

def page(title, body, mobile, extra=""):
    sc = climb_scene(390, 800) if mobile else climb_scene(1440, 772, extra and "done" or "plan")
    return (f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
            f'<title>{title} · Mijn carrière · Lobsy</title><style>{CSS}</style></head><body class="{"m" if mobile else "d"}">{ob.SPRITE}'
            f'{ob.header(mobile)}{sc}{body}{extra if extra!="done" else ""}{nav(mobile)}</body></html>')

def stone(cx, cy, rx, h=48, gold=False, op=.92):
    """Platte, onregelmatige steen; bovenkant op y=cy (daar staat de kreeft)."""
    p = (f'M{cx-rx} {cy+h}C{cx-rx*1.02} {cy+h*.45} {cx-rx*.85} {cy+2} {cx-rx*.5} {cy}'
         f'C{cx-rx*.25} {cy-3} {cx-rx*.05} {cy+1} {cx+rx*.15} {cy-1}'
         f'C{cx+rx*.45} {cy-4} {cx+rx*.8} {cy+1} {cx+rx*.92} {cy+h*.3}'
         f'C{cx+rx*1.0} {cy+h*.55} {cx+rx*1.03} {cy+h*.8} {cx+rx*1.05} {cy+h}Z')
    s = f'<path d="{p}" style="fill:var(--rock);opacity:{op}"/>'
    s += f'<path d="M{cx-rx*.92} {cy+h}C{cx-rx*.95} {cy+h*.4} {cx-rx*.7} {cy+4} {cx-rx*.35} {cy+3}L{cx-rx*.2} {cy+h*.55}L{cx-rx*.45} {cy+h}Z" style="fill:var(--surface);opacity:.08"/>'
    s += f'<path d="M{cx+rx*.22} {cy+2}L{cx+rx*.36} {cy+h*.45}L{cx+rx*.12} {cy+h}" style="stroke:var(--brand-deep);opacity:.25" stroke-width="1.5" fill="none" stroke-linecap="round"/>'
    s += f'<path d="M{cx-rx*.6} {cy+3}C{cx-rx*.3} {cy-1} {cx+rx*.3} {cy-2} {cx+rx*.7} {cy+3}" style="stroke:var(--surface);opacity:.3" stroke-width="2.5" fill="none" stroke-linecap="round"/>'
    s += f'<ellipse cx="{cx+rx*1.2}" cy="{cy+h-4}" rx="{max(4,rx*.14)}" ry="{max(3,rx*.09)}" style="fill:var(--rock);opacity:{op*.8}"/>'
    if gold:
        s = (f'<circle cx="{cx}" cy="{cy-10}" r="{rx*1.35}" style="fill:var(--sun);opacity:.25"/><circle cx="{cx}" cy="{cy-10}" r="{rx*.9}" style="fill:var(--sun);opacity:.2"/>' + s +
             f'<path d="{p}" style="fill:none;stroke:var(--gold-light)" stroke-width="3"/>')
    return s

def old_shell(x, y, sc=.6, rot=-10):
    return f'<g class="shard" transform="translate({x} {y}) rotate({rot}) scale({sc})">{SHARD}</g>'

def trail(pts, done_upto, sw=3):
    """pts = rock tops bottom->top; segments < done_upto are solid gold, rest dashed."""
    s = ""
    for i in range(len(pts) - 1):
        (x1, y1), (x2, y2) = pts[i], pts[i + 1]
        mx, my = (x1 + x2) / 2, min(y1, y2) - 26
        dpath = f'M{x1} {y1-6}Q{mx} {my} {x2} {y2-6}'
        if i < done_upto:
            s += f'<path class="tr2" d="{dpath}" style="stroke:var(--gold-light);opacity:.95" stroke-width="{sw}"/>'
        else:
            s += f'<path class="tr2" d="{dpath}" style="stroke:var(--surface);opacity:.75" stroke-width="{sw}" stroke-dasharray="2 9"/>'
    return s

# Desktop climb geometry (zone 404 x 748). Order: Nu, stap1, stap2, stap3, droom.
DPTS = [(92, 690), (272, 574), (120, 452), (292, 326), (186, 172)]
DRX = [72, 62, 64, 56, 86]

def climb_desk(cur, bubble, done=None, celebrate=False, mode="plan"):
    """cur = index van de steen waar de kreeft staat (0 = Nu). done = aantal gehaalde stappen."""
    done = (cur - 1 if cur > 0 else 0) if done is None else done
    W, H = 404, 748
    svg = [f'<svg viewBox="0 0 {W} {H}" width="{W}" height="{H}" aria-hidden="true" style="overflow:visible">']
    if mode == "empty":
        cands = [(96, 360, 54), (300, 290, 54), (196, 170, 64)]
        for (x, y, rx) in cands:
            svg.append(stone(x, y, rx, gold=False, op=.55))
            svg.append(f'<circle cx="{x}" cy="{y-12}" r="{rx*1.1}" style="fill:var(--sun);opacity:.12"/>')
        svg.append(stone(*DPTS[0], DRX[0]))
        for (x, y, rx) in cands:
            svg.append(f'<path class="tr2" d="M{DPTS[0][0]} {DPTS[0][1]-10}Q{(DPTS[0][0]+x)/2+30} {(DPTS[0][1]+y)/2} {x} {y-8}" style="stroke:var(--surface);opacity:.55" stroke-width="2.5" stroke-dasharray="2 9"/>')
    else:
        svg.append(trail(DPTS, cur if celebrate else done + 1 if cur > 0 else 0))
        for i, ((x, y), rx) in enumerate(zip(DPTS, DRX)):
            svg.append(stone(x, y, rx, gold=(i == 4)))
        for i in range(1, cur + (1 if not celebrate else 0)):
            pass
        # oude schaal ligt achter op elke steen die je al gehaald hebt
        for i in range(0, cur):
            x, y = DPTS[i]; svg.append(old_shell(x - 44, y - 20, .62, -8))
        if celebrate:
            x0, y0 = DPTS[cur]
            svg.insert(1, f'<circle cx="{x0}" cy="{y0-70}" r="120" style="fill:var(--sun);opacity:.22"/><circle cx="{x0}" cy="{y0-70}" r="78" style="fill:var(--sun);opacity:.2"/>')
            rr = random.Random(3)
            for k in range(14):
                import math
                a = k / 14 * 6.283; rad = 96 + rr.uniform(-8, 14)
                svg.append(f'<circle cx="{x0+rad*math.cos(a):.0f}" cy="{y0-70+rad*math.sin(a)*.9:.0f}" r="{rr.uniform(2,4.5):.1f}" style="fill:var(--gold-light);opacity:.9"/>')
        gx, gy = DPTS[4]
        svg.append(f'<g transform="translate({gx-22} {gy-78})" style="color:var(--gold-light)"><svg width="44" height="44" viewBox="0 0 24 24" style="fill:none;stroke:var(--gold-light);stroke-width:1.6;stroke-linecap:round;stroke-linejoin:round">{P["shell"]}</svg></g>')
    svg.append('</svg>')
    out = "".join(svg)
    labels = ""
    if mode != "empty":
        names = [("Nu · Zorghulp", "done")] + [(f"{n} · {k}", "done" if n <= done else ("now" if n == cur else "")) for n, k, t, s in STEPS]
        for i, (t, cls) in enumerate(names):
            x, y = DPTS[i]
            icn = ic("check") if cls == "done" else ""
            labels += f'<span class="rl {cls}" style="left:{x}px;top:{y+22}px">{icn}{t}</span>'
        gx, gy = DPTS[4]
        labels += f'<span class="rl dream" style="left:{gx}px;top:{gy+18}px">{ic("star")}Droombaan · {DREAM}</span>'
    else:
        for (x, y), t in zip([(96, 360), (300, 290), (196, 170)], ["?", "?", "?"]):
            labels += f'<span class="rl q" style="left:{x}px;top:{y+20}px">Welke steen?</span>'
        labels += f'<span class="rl done" style="left:{DPTS[0][0]}px;top:{DPTS[0][1]+22}px">Nu · Zorghulp</span>'
    # kreeft
    x, y = DPTS[cur]
    size = (170 if celebrate else 118 + 14 * done) if mode != "empty" else 124
    lab = "Lobsy de kreeft" + (" krijgt een nieuwe schaal" if celebrate else f" op steen {cur}" if cur else " op de startsteen")
    lob = lobster(size, 10 if celebrate else 4 + done * 2 if mode != "empty" else 2, celebrate, 6 if celebrate else None, lab)
    lobh = f'<div class="clob" style="left:{x - size/2:.0f}px;top:{y - size + 8:.0f}px">{lob}</div>'
    bx, by, bw = bubble[1]
    bub = f'<p class="bubble cbub" style="left:{bx}px;top:{by}px;max-width:{bw}px">{bubble[0]}</p>'
    extra = ""
    if mode == "empty":
        extra = (f'<svg style="position:absolute;left:{x-40}px;top:{y-size-24}px" width="80" height="34" viewBox="0 0 80 34" aria-hidden="true">'
                 '<path d="M14 26q-8-12 2-22M24 28q-6-9 0-17M66 26q8-12-2-22M56 28q6-9 0-17" style="fill:none;stroke:var(--surface);opacity:.85" stroke-width="2.4" stroke-linecap="round"/></svg>')
    return f'<div class="climb">{out}{labels}{extra}{lobh}{bub}</div>'

# Mobile climb geometry (band 390 x 150): left -> right rising.
MPTS = [(36, 132), (112, 116), (190, 98), (268, 80), (346, 56)]
MRX = [30, 30, 31, 30, 36]
def climb_mob(cur, done=None, celebrate=False, mode="plan"):
    done = (cur - 1 if cur > 0 else 0) if done is None else done
    W, H = 390, 150
    s = [f'<svg viewBox="0 0 {W} {H}" width="100%" height="{H}" aria-hidden="true" style="overflow:visible">']
    if mode == "empty":
        for (x, y, rx) in [(170, 92, 26), (262, 70, 26), (346, 52, 32)]:
            s.append(f'<circle cx="{x}" cy="{y-6}" r="{rx*1.2}" style="fill:var(--sun);opacity:.14"/>'); s.append(stone(x, y, rx, 28, op=.55))
            s.append(f'<text x="{x}" y="{y+22}" text-anchor="middle" style="fill:var(--surface);font:600 13px var(--font)">?</text>')
        s.append(stone(*MPTS[0], MRX[0], 28))
    else:
        s.append(trail(MPTS, cur if celebrate else done + 1 if cur > 0 else 0, 2.4))
        for i, ((x, y), rx) in enumerate(zip(MPTS, MRX)):
            s.append(stone(x, y, rx, 28, gold=(i == 4)))
            if 0 < i < 4:
                s.append(f'<text x="{x}" y="{y+19}" text-anchor="middle" style="fill:var(--surface);font:600 12px var(--font)">{i}</text>')
        for i in range(0, cur):
            x, y = MPTS[i]; s.append(old_shell(x - 22, y - 10, .32, -8))
        if celebrate:
            x0, y0 = MPTS[cur]
            s.insert(1, f'<circle cx="{x0}" cy="{y0-34}" r="54" style="fill:var(--sun);opacity:.28"/>')
            for k in range(10):
                import math
                a = k / 10 * 6.283
                s.append(f'<circle cx="{x0+50*math.cos(a):.0f}" cy="{y0-34+44*math.sin(a):.0f}" r="2.6" style="fill:var(--gold-light)"/>')
        gx, gy = MPTS[4]
        s.append(f'<g transform="translate({gx-12} {gy+8})"><svg width="24" height="24" viewBox="0 0 24 24" style="fill:none;stroke:var(--gold-ink);stroke-width:2;stroke-linecap:round;stroke-linejoin:round">{P["star"]}</svg></g>')
    s.append('</svg>')
    x, y = MPTS[cur]
    size = (86 if celebrate else 62 + 8 * done) if mode != "empty" else 66
    lob = lobster(size, 10 if celebrate else 4 + done * 2 if mode != "empty" else 2, celebrate, None, "Lobsy de kreeft")
    return f'<div class="mclimb">{"".join(s)}<div class="clob" style="left:{x - size/2 + 12:.0f}px;top:{y - size + 6:.0f}px">{lob}</div></div>'

def rail(cur, done):
    """Groeireis-rail: Nu -> stappen -> droombaan (van de diepte naar het licht)."""
    h = '<ol class="route up" aria-label="Jouw groeireis, van waar je nu bent naar je droombaan">'
    h += '<li class="zone" aria-hidden="true">In de diepte · waar je nu bent</li>'
    h += f'<li class="st done"><span class="dot">{ic("check")}</span><div><b>{NOW[0]}</b><small>{NOW[1]}</small></div></li>'
    h += '<li class="zone" aria-hidden="true">De klim</li>'
    for nr, k, t, sub in STEPS:
        if nr <= done:
            h += f'<li class="st done"><span class="dot">{ic("check")}</span><div><b>{t}</b><small>Nieuwe schaal</small></div></li>'
        elif nr == cur:
            h += f'<li class="st now" aria-current="step"><span class="mk">{lobster(30, 4 + done*2)}</span><div><b>{t}</b><small>Groeit nu · {sub}</small></div></li>'
        else:
            h += f'<li class="st todo"><span class="dot">{nr}</span><div><b>{t}</b><small>{sub}</small></div></li>'
    h += '<li class="zone" aria-hidden="true">Naar het licht</li>'
    h += f'<li class="st goal"><span class="dot">{ic("star")}</span><div><b>{DREAM}</b><small>Jouw droombaan · niveau 4</small></div></li></ol>'
    segs = "".join(f'<i class="{"gold" if k < done else ("grow" if k == done else "")}"></i>' for k in range(NS))
    return (f'<aside class="card rail"><h2>Jouw groeireis</h2><p class="cnt">Naar {DREAM}</p>{h}'
            f'<div class="layers" aria-label="{done} van {NS} stappen gehaald"><span class="segs">{segs}</span><span>{done} van {NS} nieuwe schalen</span></div>'
            f'<p class="saved">{ic("check")}Alles is bewaard. Wisselen mag altijd.</p></aside>')

def shells(done, cur, compact=False):
    nodes = [("Nu", "Zorghulp", "done", ic("check"))]
    for nr, k, t, sub in STEPS:
        st = "done" if nr <= done else ("now" if nr == cur else "")
        lab = "Groeit nu" if st == "now" else f"Stap {nr}"
        nodes.append((lab, k if not compact else k.split()[0], st, ic("check") if st == "done" else str(nr)))
    nodes.append(("Doel", "Verpleegkundige", "goal", ic("star")))
    sizes = [30, 34, 38, 42, 48]
    li = ""
    for i, (a, b, st, inner) in enumerate(nodes):
        sz = sizes[i] - (4 if compact else 0)
        cur_attr = ' aria-current="step"' if st == "now" else ''
        li += (f'<li class="{st}" style="--mid:{sizes[-1]//2 - (2 if compact else 0)}px"{cur_attr}><span style="height:{sizes[-1] - (4 if compact else 0)}px;display:flex;align-items:center">'
               f'<span class="sh" style="width:{sz}px;height:{sz}px">{inner}</span></span><b>{a}</b><small>{b}</small></li>')
    return f'<ol class="shells" aria-label="Je stappen: van Nu naar je droombaan">{li}</ol>'

def desk(cur, done, card, climb, title, extra=""):
    body = f'<main class="stage">{rail(cur, done)}<section class="card step">{card}</section>{climb}</main>'
    return page(title, body, False, extra)

# ---------------------------------------------------------------- screens: content
def empty_card(m=False):
    sug = [("Verpleegkundige (MBO)", "Past goed bij je · zorgzaam, helpt graag", 1, "shell"),
           ("Begeleider gehandicaptenzorg", "Past goed bij je · werkt graag met mensen", 0, "heart"),
           ("Doktersassistent", "Past redelijk · rustig en precies", 0, "clip")]
    s = "".join(f'<li class="sg{" on" if on else ""}" role="radio" aria-checked="{"true" if on else "false"}"><span class="ico">{ic(i)}</span><div><b>{a}</b><small>{b}</small></div><span class="sp"></span><span class="rd"></span></li>' for a, b, on, i in sug)
    return (f'<p class="eb">Mijn carrière</p><h1>Waar wil jij naartoe groeien?</h1>'
            f'<p class="lead">Kies je droombaan. Lobsy maakt een plan met kleine stappen, van waar je nu bent tot daar. Je kunt altijd wisselen.</p>'
            f'<div class="body"><section><h2 class="q">Past bij jou</h2><p class="qs">Uit je paspoort: wat je goed kunt en leuk vindt.</p>'
            f'<ul class="sugg" role="radiogroup" aria-label="Droombaan kiezen" style="margin-top:10px">{s}</ul></section>'
            f'<p class="or">of</p>'
            f'<label class="fld">Zelf een beroep zoeken<span class="inp ph">{ic("search")}Bijvoorbeeld kok, monteur of leraar</span>'
            f'<span class="hint">We zoeken in een lijst met echte beroepen. Zo klopt je plan beter.</span></label>'
            + ('' if m else f'<p class="hint">{ic("compass")}Weet je het nog niet? <a class="lnk" style="min-height:0">Maak eerst de ontdekkingsreis af</a></p>')
            + '</div>')

def overview_card(m=False):
    facts = (f'<ul class="facts3"><li>{ic("claw","i w")}Nog 2 klauwen laten groeien</li>'
             f'<li>{ic("book")}2 opleidingen · 1 is gratis</li>'
             f'<li>{ic("stone")}Deze steen past al redelijk bij jou</li></ul>')
    have = (f'<ul class="have"><li>{ic("check")}<span>Zorghulp (vrijwillig) · 1 jaar</span><span class="sp"></span><small>In je paspoort</small></li>'
            f'<li>{ic("check")}<span>Kennismaken met de zorg · cursus</span><span class="sp"></span><small>In je paspoort</small></li></ul>')
    edit = '' if m else f'<a class="lnk">{ic("edit")}Droombaan wijzigen</a>'
    return (f'<div class="ebr"><p class="eb">Mijn carrière · stap 2 van {NS}</p>{edit}</div>'
            f'<h1>{"Naar " + DREAM if m else "Op weg naar " + DREAM}</h1>'
            f'<p class="lead">Een kreeft groeit alleen als hij zijn oude schaal loslaat. Zo groei jij ook: steen voor steen.</p>'
            f'{shells(1, 2, m)}'
            f'<div class="body"><section class="nxt" aria-labelledby="nx"><p class="eyb2">{ic("claw")}Nu aan de beurt · groeit nu</p><h2 id="nx">Helpende Zorg &amp; Welzijn (niveau 2)</h2>{facts}'
            + ('' if m else f'<div class="foot" style="margin-top:2px"><span class="sp"></span><a class="btn pri">Bekijk deze stap{ic("right")}</a></div>') +
            f'</section>'
            f'<section><h2 class="q">Wat je al hebt</h2>{have}</section>'
            f'<p class="ai">{ic("compass")}Plan gemaakt met hulp van AI, op basis van je paspoort. Klopt iets niet? Zeg het ons.</p></div>')

def gaps_block():
    return (f'<section class="blk"><h2>Wat je nog mist</h2><p class="qs">Welke klauwen je al hebt, en welke je nog laat groeien.</p>'
            f'<ul class="gaps"><li class="miss">{ic("claw")}<span>Diploma Helpende (niveau 2)</span></li>'
            f'<li class="miss">{ic("claw")}<span>Nederlands lezen en schrijven (B1)</span></li>'
            f'<li class="ok">{ic("check")}<span>Zorgzaam en geduldig <small>· heb je al</small></span></li>'
            f'<li class="ok">{ic("check")}<span>Ervaring met ouderen <small>· heb je al</small></span></li></ul></section>')
def match_block():
    return (f'<section class="blk"><p class="eyb2">{ic("stone")}Groei eerst. Match daarna.</p><h2>Match op deze stap</h2>'
            f'<p class="fit"><span class="pill">Past redelijk</span></p>'
            f'<p class="sm">Deze steen past al redelijk bij jouw formaat. Met het diploma worden je matches sterker.</p>'
            f'<a class="lnk" style="min-height:32px">Vacatures voor helpende{ic("right")}</a></section>')
def course_block():
    return (f'<section><h2 class="q">Opleiding die past</h2><div class="crs" style="margin-top:8px">'
            f'<article class="co free"><p class="tp"><span class="pill ok">{ic("check")}Gratis</span><b>Nederlands voor de zorg (B1)</b></p>'
            f'<small>Cursus · 10 weken · online · Voorbeeld-Taalhuis</small><p class="why">{ic("claw")}Laat je klauw ‘Nederlands B1’ groeien</p>'
            f'<a class="lnk">Bekijk cursus{ic("ext")}</a></article>'
            f'<article class="co"><p class="tp"><span class="pill out">Partnerlink</span><b>Helpende Zorg &amp; Welzijn (BBL)</b></p>'
            f'<small>Opleiding · 1 jaar · leren en werken · Voorbeeld-Opleidingen</small><p class="why">{ic("claw")}Laat je klauw ‘Diploma Helpende’ groeien</p>'
            f'<a class="lnk" rel="sponsored">Bekijk opleiding{ic("ext")}</a></article></div>'
            f'<p class="xs muted" style="margin-top:8px">Gratis staat altijd bovenaan. Partnerlink: Lobsy kan een vergoeding krijgen.</p></section>')

def detail_card(m=False):
    top = (f'<a class="back">{ic("left")}Mijn groeireis</a><p class="eb">Stap 2 van {NS} · groeit nu</p>' if m else
           f'<div class="ebr"><p class="eb">De klim · stap 2 van {NS} · groeit nu</p><a class="back">{ic("left")}Mijn groeireis</a></div>')
    return (top +
            f'<h1>Helpende Zorg &amp; Welzijn</h1>'
            f'<p class="lead">Je helpt mensen met wassen, eten en bewegen. Met dit diploma (niveau 2) mag je betaald in de zorg werken.</p>'
            + (f'<div class="body" style="gap:14px">{course_block()}<div class="two">{gaps_block()}{match_block()}</div></div>' if m else f'<div class="body" style="gap:14px"><div class="two">{gaps_block()}{match_block()}</div>{course_block()}</div>')
            + ('' if m else f'<div class="foot" style="margin-top:14px"><a class="lnk">{ic("file")}Heb je dit al? Voeg bewijs toe</a><span class="sp"></span><a class="btn pri">Deze stap is klaar{ic("check")}</a></div>'))

def done_card(m=False):
    g = "".join(f'<li>{ic("check")}{t}</li>' for t in [
        "Diploma Helpende (niveau 2) staat in je paspoort",
        "Je klauw ‘Nederlands B1’ is gegroeid",
        "Nieuw: 14 vacatures als helpende passen bij je"])
    pic = f'<div class="pic"><div class="fallen">{ob.shard(110 if not m else 80,-8)}<span>Oude schaal</span></div></div>'
    nxt = (f'<div class="nxt" style="flex-direction:row;align-items:center;gap:12px;padding:12px 14px"><span class="sg" style="border:0;padding:0;min-height:0;background:none"><span class="ico">{ic("stone")}</span></span>'
           f'<div style="flex:1"><p class="eyb2" style="margin-bottom:2px">Je volgende steen</p><b class="sm b6">Stap 3: Verzorgende IG (niveau 3)</b><p class="xs muted">Nog 3 klauwen · 2 opleidingen</p></div></div>')
    return (f'<p class="eb">De klim · stap 2 van {NS} klaar</p>'
            f'<div class="shed" style="margin-top:14px">{pic if not m else ""}<div><h1 style="margin-top:0">Je nieuwe schaal past</h1>'
            f'<p class="lead">Helpende Zorg &amp; Welzijn is gehaald. Je oude schaal was te krap. Je bent weer gegroeid.</p></div></div>'
            f'<ul class="gained" style="margin-top:12px">{g}</ul>'
            f'{shells(2, 3, m)}'
            f'<div class="body" style="margin-top:16px">{nxt}</div>'
            + ('' if m else f'<div class="foot"><a class="lnk">{ic("undo")}Toch nog niet klaar</a><span class="sp"></span><a class="btn pri">Op naar stap 3{ic("right")}</a></div>'))

# ---------------------------------------------------------------- desktop screens
def d1():
    card = empty_card() + f'<div class="foot"><span class="sp"></span><a class="btn pri">Maak mijn groeiplan{ic("right")}</a></div>'
    climb = climb_desk(0, ("Met mijn antennes voel ik al een paar stenen die bij je passen. Welke wil jij?", (150, 470, 240)), mode="empty")
    body = f'<main class="stage">{rail_empty()}<section class="card step">{card}</section>{climb}</main>'
    note = ('<p class="note tr"><b>Ontwerpnotitie</b> · Suggesties komen uit het paspoort (Beroepen-test, werkveld), niet uit een vaste lijst. '
            'Zelf zoeken gaat in de beroepenlijst, geen vrije tekst naar de AI.</p>')
    return page("Kies je droombaan", body, False, note)

def rail_empty():
    h = ('<ol class="route up" aria-label="Jouw groeireis"><li class="zone" aria-hidden="true">In de diepte · waar je nu bent</li>'
         f'<li class="st now" aria-current="step"><span class="mk">{lobster(30, 2)}</span><div><b>{NOW[0]}</b><small>Je startsteen</small></div></li>'
         '<li class="zone" aria-hidden="true">De klim</li>'
         f'<li class="st todo"><span class="dot">?</span><div><b>Je stappen</b><small>Komen na je keuze</small></div></li>'
         '<li class="zone" aria-hidden="true">Naar het licht</li>'
         f'<li class="st end todo"><span class="dot">{ic("star")}</span><div><b>Jouw droombaan</b><small>Kies hem hiernaast</small></div></li></ol>')
    return (f'<aside class="card rail"><h2>Jouw groeireis</h2><p class="cnt">Nog geen droombaan gekozen</p>{h}'
            f'<p class="saved" style="color:var(--muted)">{ic("lock")}Je plan is alleen voor jou.</p></aside>')

def d2():
    climb = climb_desk(2, ("Ik zit nu op steen 2. Mijn schaal wordt al krap. Dat is goed: dan groei ik.", (196, 392, 204)))
    return desk(2, 1, overview_card(), climb, "Mijn groeireis")

def d3():
    climb = climb_desk(2, ("Twee klauwen laten we nu groeien. Begin met de gratis cursus.", (196, 392, 204)))
    return desk(2, 1, detail_card(), climb, "Stap 2")

def d4():
    climb = climb_desk(2, ("Voel je dat? Mijn oude schaal was te krap. Deze nieuwe past precies.", (196, 372, 204)), done=1, celebrate=True)
    note = ('<p class="note"><b>Ontwerpnotitie</b> · Het schaalstuk valt één keer (1,2 s) en de gouden rand verschijnt. Bij “minder beweging”: geen val, '
            'de nieuwe schaal staat er meteen. Geen toast: dit moment vervangt de stapkaart. “Toch nog niet klaar” zet de stap terug.</p>')
    body = f'<main class="stage">{rail(3, 2)}<section class="card step">{done_card()}</section>{climb}</main>'
    return page("Stap klaar", body, False, note)

def change_dialog(m=False):
    sug = [("Doktersassistent", "Past redelijk · uit je paspoort", 1, "clip"), ("Begeleider gehandicaptenzorg", "Past goed · uit je paspoort", 0, "heart")]
    s = "".join(f'<li class="sg{" on" if on else ""}" role="radio" aria-checked="{"true" if on else "false"}"><span class="ico">{ic(i)}</span><div><b>{a}</b><small>{b}</small></div><span class="sp"></span><span class="rd"></span></li>' for a, b, on, i in sug)
    return (f'<div class="scrim" aria-hidden="true"></div><section class="card dlg" role="dialog" aria-modal="true" aria-labelledby="dt">'
            f'<div class="hdr">{lobster(56, 6)}<h2 id="dt">Een andere droombaan kiezen?</h2><a class="cls" aria-label="Sluiten">{ic("x")}</a></div>'
            f'<p class="sm">Soms past een andere steen beter. Dat is helemaal goed.</p>'
            f'<ul class="sugg" role="radiogroup" aria-label="Nieuwe droombaan">{s}</ul>'
            f'<label class="fld" style="font-weight:400"><span class="inp ph">{ic("search")}Of zoek een ander beroep</span></label>'
            f'<div class="what"><h3>Wat gebeurt er met wat je al deed?</h3><ul>'
            f'<li>{ic("check")}Wat je haalde, blijft in je paspoort: 1 cursus en je werkervaring.</li>'
            f'<li>{ic("check")}Je nieuwe plan telt dat mee. Je begint niet opnieuw.</li>'
            f'<li>{ic("clock","i m")}Je oude plan bewaren we 30 dagen. Terugzetten kan.</li></ul></div>'
            f'<div class="foot" style="margin-top:0"><a class="btn">Blijf bij {DREAM if not m else "mijn plan"}</a><span class="sp"></span><a class="btn pri">Maak nieuw plan</a></div></section>')

def d5():
    climb = climb_desk(2, ("Ik zit nu op steen 2. Mijn schaal wordt al krap. Dat is goed: dan groei ik.", (196, 392, 204)))
    body = f'<main class="stage">{rail(2, 1)}<section class="card step">{overview_card()}</section>{climb}</main>'
    return page("Droombaan wijzigen", body, False, change_dialog())

# ---------------------------------------------------------------- mobile screens
def mob(card, foot, climb, bubble, title, extra=""):
    card = '<p class="demo cdemo">Voorbeelddata</p>' + card
    climb = climb.replace('</div></div>', f'</div><p class="bubble mbub">{bubble}</p></div>', 1) if False else climb[:-6] + f'<p class="bubble mbub">{bubble}</p></div>'
    body = f'<main class="mstage">{climb}<section class="card step">{card}</section></main>' + (f'<div class="mfoot">{foot}</div>' if foot else '')
    return page(title, body, True, extra)
def m1():
    return mob(empty_card(True), f'<a class="btn pri">Maak mijn groeiplan{ic("right")}</a>', climb_mob(0, mode="empty"),
               "Welke steen past bij jou?", "Kies je droombaan")
def m2():
    return mob(overview_card(True), f'<a class="btn" aria-label="Droombaan wijzigen">{ic("edit")}</a><a class="btn pri">Bekijk stap 2{ic("right")}</a>', climb_mob(2),
               "Steen 2. Mijn schaal wordt krap: ik groei!", "Mijn groeireis")
def m3():
    return mob(detail_card(True), f'<a class="btn" aria-label="Bewijs toevoegen">{ic("file")}</a><a class="btn pri">Deze stap is klaar{ic("check")}</a>', climb_mob(2),
               "Begin met de gratis cursus. Dan groeit je klauw.", "Stap 2")
def m4():
    return mob(done_card(True), f'<a class="btn" aria-label="Toch nog niet klaar">{ic("undo")}</a><a class="btn pri">Op naar stap 3{ic("right")}</a>', climb_mob(2, done=1, celebrate=True),
               "Voel je dat? Deze nieuwe schaal past precies.", "Stap klaar")
def m5():
    return mob(overview_card(True), f'<a class="btn pri">Bekijk stap 2{ic("right")}</a>', climb_mob(2),
               "Steen 2. Mijn schaal wordt krap: ik groei!", "Droombaan wijzigen", change_dialog(True))

D = [("cr-d1-geen-droombaan", d1), ("cr-d2-reis-overzicht", d2), ("cr-d3-stap-detail-opleidingen", d3), ("cr-d4-stap-klaar", d4), ("cr-d5-droombaan-wijzigen", d5)]
M = [("cr-m1-geen-droombaan", m1), ("cr-m2-reis-overzicht", m2), ("cr-m3-stap-detail-opleidingen", m3), ("cr-m4-stap-klaar", m4), ("cr-m5-droombaan-wijzigen", m5)]

# ---------------------------------------------------------------- contactverzoeken + hoe werkt lobsy
def stones_desk(labels, cur, bubble, done_upto):
    W, H = 404, 748
    svg = [f'<svg viewBox="0 0 {W} {H}" width="{W}" height="{H}" aria-hidden="true" style="overflow:visible">', trail(DPTS, done_upto)]
    for i, ((x, y), rx) in enumerate(zip(DPTS, DRX)):
        svg.append(stone(x, y, rx, gold=(i == 4 and labels[4][1] == "dream")))
    svg.append('</svg>')
    lab = ""
    for i, (t, cls) in enumerate(labels):
        x, y = DPTS[i]
        lab += f'<span class="rl {cls if cls!="dream" else "dream"}" style="left:{x}px;top:{y+22}px">{ic("check") if cls=="done" else ""}{t}</span>'
    x, y = DPTS[cur]; size = 130
    lob = f'<div class="clob" style="left:{x-size/2:.0f}px;top:{y-size+8:.0f}px">{lobster(size, 6, False, None, "Lobsy de kreeft")}</div>'
    bx, by, bw = bubble[1]
    return f'<div class="climb">{"".join(svg)}{lab}{lob}<p class="bubble cbub" style="left:{bx}px;top:{by}px;max-width:{bw}px">{bubble[0]}</p></div>'

def antenna_desk(bubble):
    W, H = 404, 748
    x, y = DPTS[0][0] + 90, 640
    svg = (f'<svg viewBox="0 0 {W} {H}" width="{W}" height="{H}" aria-hidden="true" style="overflow:visible">'
           f'{stone(x, y, 96)}{stone(60, 700, 60, op=.6)}{stone(340, 690, 56, op=.6)}'
           f'<circle cx="{x}" cy="{y-190}" r="70" style="fill:none;stroke:var(--surface);opacity:.35" stroke-width="2" stroke-dasharray="3 8"/>'
           f'<circle cx="{x}" cy="{y-190}" r="110" style="fill:none;stroke:var(--surface);opacity:.22" stroke-width="2" stroke-dasharray="3 10"/></svg>')
    size = 150
    arcs = (f'<svg style="position:absolute;left:{x-50}px;top:{y-size-30}px" width="100" height="40" viewBox="0 0 80 34" aria-hidden="true">'
            '<path d="M14 26q-8-12 2-22M24 28q-6-9 0-17M66 26q8-12-2-22M56 28q6-9 0-17" style="fill:none;stroke:var(--surface);opacity:.9" stroke-width="2.4" stroke-linecap="round"/></svg>')
    lob = f'<div class="clob" style="left:{x-size/2:.0f}px;top:{y-size+8:.0f}px">{lobster(size, 6, False, None, "Lobsy de kreeft luistert met zijn antennes")}</div>'
    bx, by, bw = bubble[1]
    return f'<div class="climb">{svg}{arcs}{lob}<p class="bubble cbub" style="left:{bx}px;top:{by}px;max-width:{bw}px">{bubble[0]}</p></div>'

def how_contact_card():
    return (f'<aside class="card rail"><h2>Zo werkt het</h2><p class="cnt">Jij beslist, altijd.</p><ul class="how" style="margin-top:10px">'
            f'<li>{ic("antenna")}<span>Een werkgever ziet je paspoort, maar niet je naam, e-mail of telefoon.</span></li>'
            f'<li>{ic("shield")}<span>Pas als jij <b>ja</b> zegt, krijgt die werkgever je naam, e-mail en telefoon.</span></li>'
            f'<li>{ic("clock")}<span>Reageer het liefst binnen 2 dagen. Zeg je niets, dan wordt er niets gedeeld.</span></li>'
            f'<li>{ic("eyeoff")}<span>Nee zeggen mag. De werkgever ziet alleen “geen interesse”.</span></li></ul></aside>')

def requests(m=False):
    r1 = (f'<article class="req open" aria-labelledby="rq1"><div class="top"><span class="ico">Z</span><b id="rq1">Zorghuis Wateringen</b><span class="sp"></span><span class="pill wait">{ic("clock")}Wacht op jou</span></div>'
          f'<q>“Hoi! We zoeken een helpende voor 24 uur per week. Je paspoort past goed bij ons team. Zullen we kennismaken?”</q>'
          f'<p class="when">{ic("clock")}Reageer het liefst vóór do 2 okt, 14:30</p>'
          f'<div class="acts"><a class="btn pri">{ic("check")}Ja, deel mijn gegevens</a><a class="btn">Ik heb al werk</a>' + ('' if m else '<span class="sp"></span>') + '<a class="lnk">Geen interesse</a></div></article>')
    r2 = (f'<article class="req"><div class="top"><span class="ico">T</span><b>Thuiszorg Westland</b><span class="sp"></span><span class="pill ok">{ic("check")}Je zei ja</span></div>'
          f'<p class="sm muted">28 sep · Je naam, e-mail en telefoon zijn gedeeld. De werkgever neemt contact met je op.</p></article>')
    r3 = (f'<article class="req"><div class="top"><span class="ico">B</span><b>Bakkerij De Korenaar</b><span class="sp"></span><span class="pill neutral">Je zei nee</span></div>'
          f'<p class="sm muted">21 sep · Er is niets gedeeld.</p></article>')
    return f'<div class="body" style="gap:10px">{r1}{r2}{r3}</div>'

def contacts_card(m=False):
    return (f'<p class="eb">Contactverzoeken</p><h1>Een werkgever wil je spreken</h1>'
            f'<p class="lead">Werkgevers zien je naam, e-mail en telefoon pas als jij ja zegt. Nee zeggen mag altijd.</p>{requests(m)}')

def d6():
    climb = antenna_desk(("Mijn antennes trillen: iemand vindt dat je past. Jij beslist of je ja zegt.", (150, 300, 230)))
    body = f'<main class="stage">{how_contact_card()}<section class="card step">{contacts_card()}</section>{climb}</main>'
    return page("Contactverzoeken", body, False)

def share_dialog(m=False):
    return (f'<div class="scrim" aria-hidden="true"></div><section class="card dlg" role="dialog" aria-modal="true" aria-labelledby="st">'
            f'<div class="hdr">{lobster(56, 6)}<h2 id="st">Je gegevens delen met Zorghuis Wateringen?</h2><a class="cls" aria-label="Sluiten">{ic("x")}</a></div>'
            f'<p class="sm">Dit krijgt Zorghuis Wateringen als je ja zegt:</p>'
            f'<dl class="shared"><dt>Naam</dt><dd>Samira El Amrani</dd><dt>E-mail</dt><dd>samira@voorbeeld.nl</dd><dt>Telefoon</dt><dd>06 12345678</dd></dl>'
            f'<p class="hint">{ic("shield")}Delen kun je niet terugdraaien. De werkgever mag je daarna bellen of mailen over dit werk.</p>'
            f'<div class="foot" style="margin-top:0"><a class="btn">Nog niet</a><span class="sp"></span><a class="btn pri">{ic("check")}Ja, deel mijn gegevens</a></div></section>')

def d7():
    climb = antenna_desk(("Mijn antennes trillen: iemand vindt dat je past. Jij beslist of je ja zegt.", (150, 300, 230)))
    body = f'<main class="stage">{how_contact_card()}<section class="card step">{contacts_card()}</section>{climb}</main>'
    return page("Gegevens delen", body, False, share_dialog())

HOW = [("De ontdekkingsreis", "Ontdek wie je bent. 10 korte stappen.", "done", "compass", "Kijk terug"),
       ("Mijn Paspoort", "Alles over jou op één plek. Jij kiest wie het ziet.", "done", "book", "Open"),
       ("Carrière", "Kies je droombaan en groei steen voor steen.", "now", "sun", "Ga verder"),
       ("Banenkaart", "Vind werk dichtbij huis, op de kaart.", "", "pin", "Open"),
       ("Sollicitaties", "Zie waar je solliciteerde en wat er gebeurt.", "", "clip", "Open")]
def how_card(m=False):
    li = ""
    for k, (t, sub, st, icn, act) in enumerate(HOW):
        nr = ic("check") if st == "done" else str(k + 1)
        cur = ' aria-current="step"' if st == "now" else ''
        li += (f'<li class="m5 {st}"{cur}><span class="nr">{nr}</span><div><b>{t}</b><small>{sub}</small></div><span class="sp"></span>'
               + ('' if m else f'<a class="lnk">{act}{ic("right")}</a>') + '</li>')
    return (f'<p class="eb">Hoe werkt Lobsy?</p><h1>Vijf stenen, in je eigen tempo</h1>'
            f'<p class="lead">Je hoeft niet alles tegelijk. Begin waar je wilt. Alles wordt bewaard.</p>'
            f'<ol class="map5 body" aria-label="Zo werkt Lobsy">{li}</ol>'
            + ('' if m else f'<div class="foot"><a class="lnk">{ic("check")}Ik snap het</a><span class="sp"></span><a class="btn pri">Verder met Carrière{ic("right")}</a></div>'))
def safe_card():
    return (f'<aside class="card rail"><h2>Veilig en rustig</h2><p class="cnt">Goed om te weten</p><ul class="how" style="margin-top:10px">'
            f'<li>{ic("shield")}<span>Werkgevers zien je naam pas als jij ja zegt.</span></li>'
            f'<li>{ic("check")}<span>Alles is bewaard. Stoppen mag altijd.</span></li>'
            f'<li>{ic("wave")}<span>Lobsy in jouw taal: Nederlands, English, Polski, Română, العربية.</span></li>'
            f'<li>{ic("antenna")}<span>Vragen? Tik op <b>Assistent</b>, rechts op het scherm.</span></li></ul></aside>')
def d8():
    labels = [("Reis", "done"), ("Paspoort", "done"), ("Carrière", "now"), ("Banenkaart", ""), ("Sollicitaties", "")]
    climb = stones_desk(labels, 2, ("Kijk: twee stenen heb je al. Nu kies je waar je naartoe groeit.", (196, 392, 204)), 2)
    body = f'<main class="stage">{safe_card()}<section class="card step">{how_card()}</section>{climb}</main>'
    return page("Hoe werkt Lobsy", body, False)

def band_simple(cur_idx, mode="how"):
    W, H = 390, 150
    s = [f'<svg viewBox="0 0 {W} {H}" width="100%" height="{H}" aria-hidden="true" style="overflow:visible">', trail(MPTS, cur_idx, 2.4)]
    for i, ((x, y), rx) in enumerate(zip(MPTS, MRX)):
        s.append(stone(x, y, rx, 28))
        s.append(f'<text x="{x}" y="{y+19}" text-anchor="middle" style="fill:var(--surface);font:600 12px var(--font)">{i+1}</text>')
    s.append('</svg>')
    x, y = MPTS[cur_idx]; size = 70
    return f'<div class="mclimb">{"".join(s)}<div class="clob" style="left:{x-size/2+12:.0f}px;top:{y-size+6:.0f}px">{lobster(size, 6, False, None, "Lobsy de kreeft")}</div></div>'
def band_antenna():
    W, H = 390, 150
    s = (f'<svg viewBox="0 0 {W} {H}" width="100%" height="{H}" aria-hidden="true" style="overflow:visible">{stone(300, 124, 46, 28)}'
         f'<circle cx="300" cy="70" r="44" style="fill:none;stroke:var(--surface);opacity:.4" stroke-width="2" stroke-dasharray="3 8"/></svg>')
    size = 84
    return f'<div class="mclimb">{s}<div class="clob" style="left:{300-size/2:.0f}px;top:{124-size+6:.0f}px">{lobster(size, 6, False, None, "Lobsy de kreeft")}</div></div>'
def m6():
    return mob(contacts_card(True), '', band_antenna(),
               "Iemand vindt dat je past. Jij beslist.", "Contactverzoeken")
def m7():
    return mob(how_card(True), f'<a class="lnk">Ik snap het</a><a class="btn pri">Verder met Carrière{ic("right")}</a>', band_simple(2),
               "Twee stenen heb je al. Op naar steen 3.", "Hoe werkt Lobsy")

D += [("cr-d6-contactverzoeken", d6), ("cr-d7-contact-delen-bevestigen", d7), ("cr-d8-hoe-werkt-lobsy", d8)]
M += [("cr-m6-contactverzoeken", m6), ("cr-m7-hoe-werkt-lobsy", m7)]

if __name__ == "__main__":
    with sync_playwright() as pw:
        b = pw.chromium.launch(executable_path="/usr/bin/google-chrome")
        pg = b.new_page(viewport={"width": 1440, "height": 900}, reduced_motion="reduce")
        for n, fn in D:
            ACTIVE[0] = "Mijn Paspoort" if "contact" in n else ("" if "hoe-werkt" in n else "Carrière")
            f = d / f"{n}.html"; f.write_text(fn()); pg.goto(f.as_uri()); pg.wait_for_timeout(300)
            print(n, pg.evaluate(ob.CHECK), f"{f.stat().st_size//1024}kB"); pg.screenshot(path=str(d / f"{n}.png"))
        pm = b.new_page(viewport={"width": 390, "height": 844}, device_scale_factor=2, reduced_motion="reduce")
        for n, fn in M:
            ACTIVE[0] = "Mijn Paspoort" if "contact" in n else ("" if "hoe-werkt" in n else "Carrière")
            f = d / f"{n}.html"; f.write_text(fn()); pm.goto(f.as_uri()); pm.wait_for_timeout(300)
            print(n, pm.evaluate(ob.CHECK), f"{f.stat().st_size//1024}kB"); pm.screenshot(path=str(d / f"{n}.png"))
        b.close()
