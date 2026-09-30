"""Screens for the candidate job pages (kd-). Tokens, header and nav follow ../dna-paspoort + ../ontdekkingsreis.
Map: real OpenFreeMap basemap + real Valhalla bike isochrones (src/proj.json, see basemap.py). All people, companies
and numbers are Voorbeelddata."""
import pathlib, base64, json
d = pathlib.Path(__file__).resolve().parent; s = d / "src"
def b64(p, mt): return f"data:{mt};base64," + base64.b64encode(p.read_bytes()).decode()
LOGO = b64(s / "lobsy-128.png", "image/png"); MASCOT = b64(s / "mascot-128.webp", "image/webp")
PH = {k: b64(s / f"{k}-400.webp", "image/webp") for k in ["zorg", "logistiek", "tuinbouw", "onderwijs", "winkel", "productie", "schoonmaak", "horeca", "bouw", "kantoor"]}
MAP = {k: b64(s / f"map-{k}.png", "image/png") for k in ["d1", "m1", "det", "mdet"]}
PROJ = json.loads((s / "proj.json").read_text())

P = dict(
 search='<circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/>',
 heart='<path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.6l-1-1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8z"/>',
 clip='<rect x="8" y="2" width="8" height="4" rx="1"/><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/><path d="m9 14 2 2 4-4"/>',
 sun='<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/>',
 user='<circle cx="12" cy="8" r="4"/><path d="M4 21v-1a6 6 0 0 1 6-6h4a6 6 0 0 1 6 6v1"/>',
 bell='<path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9"/><path d="M10 21a2 2 0 0 0 4 0"/>',
 chevd='<path d="m6 9 6 6 6-6"/>', right='<path d="m9 6 6 6-6 6"/>', left='<path d="m15 6-6 6 6 6"/>', check='<path d="m5 12 5 5 9-10"/>',
 clock='<circle cx="12" cy="12" r="8"/><path d="M12 8v4l3 2"/>', plus='<path d="M12 5v14M5 12h14"/>', minus='<path d="M5 12h14"/>',
 book='<path d="M5 4h11a3 3 0 0 1 3 3v13H8a3 3 0 0 1-3-3z"/><path d="M5 17a3 3 0 0 1 3-3h11"/><circle cx="12" cy="8.5" r="2"/>',
 compass='<circle cx="12" cy="12" r="9"/><path d="m15.5 8.5-2 5-5 2 2-5z"/>',
 bike='<circle cx="6" cy="16" r="3.5"/><circle cx="18" cy="16" r="3.5"/><path d="M6 16l4-7h5l3 7M10 9 8.5 6H7M15 9l-3 7"/>',
 walk='<circle cx="13" cy="4.5" r="1.8"/><path d="m10 21 2-6 3 3v3M8 12l2-4 4 1 2 3M12 15l-1-6"/>',
 bus='<rect x="5" y="3" width="14" height="15" rx="3"/><path d="M5 11h14M8 21v-3M16 21v-3"/><circle cx="8.5" cy="14.5" r=".8"/><circle cx="15.5" cy="14.5" r=".8"/>',
 car='<path d="M5 16V11l2-5h10l2 5v5M3 16h18v3H3zM5 11h14"/><circle cx="7.5" cy="16" r="1"/><circle cx="16.5" cy="16" r="1"/>',
 building='<rect x="4" y="3" width="16" height="18" rx="1"/><path d="M9 7h1M14 7h1M9 11h1M14 11h1M9 15h1M14 15h1M10 21v-3h4v3"/>',
 euro='<path d="M17 6.5A7 7 0 1 0 17 17.5M4 10h9M4 14h9"/>', info='<circle cx="12" cy="12" r="9"/><path d="M12 11v5M12 8h.01"/>',
 x='<path d="M6 6l12 12M18 6 6 18"/>', trash='<path d="M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13"/>',
 cal='<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/>',
 chat='<path d="M4 5h16v11H9l-5 4z"/>', map='<path d="M9 4 3 6v14l6-2 6 2 6-2V4l-6 2z"/><path d="M9 4v14M15 6v14"/>',
 list='<path d="M8 6h13M8 12h13M8 18h13"/><circle cx="4" cy="6" r="1"/><circle cx="4" cy="12" r="1"/><circle cx="4" cy="18" r="1"/>',
 sliders='<path d="M4 6h10M18 6h2M4 12h4M12 12h8M4 18h12"/><circle cx="16" cy="6" r="2"/><circle cx="10" cy="12" r="2"/><circle cx="18" cy="18" r="2"/>',
 home='<path d="M4 11 12 4l8 7v9h-5v-6H9v6H4z"/>', target='<circle cx="12" cy="12" r="8"/><circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3"/>',
 spark='<path d="M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M6 18l2.5-2.5M15.5 8.5 18 6"/>',
 sort='<path d="M7 4v16M4 17l3 3 3-3M17 20V4M14 7l3-3 3 3"/>', send='<path d="M21 3 10 14M21 3l-7 18-4-7-7-4z"/>',
 users='<circle cx="9" cy="8" r="3.5"/><path d="M2.5 20a6.5 6.5 0 0 1 13 0M16 4.5a3.5 3.5 0 0 1 0 7M18 14a5 5 0 0 1 4 6"/>',
 leaf='<path d="M5 19c0-9 6-14 15-14 0 9-5 15-14 15"/><path d="M5 19 13 11"/>',
 hand='<path d="M7 11V6a1.5 1.5 0 0 1 3 0v4M10 10V4.5a1.5 1.5 0 0 1 3 0V10M13 10V5.5a1.5 1.5 0 0 1 3 0V12M16 9a1.5 1.5 0 0 1 3 0v4a8 8 0 0 1-8 8h-1a6 6 0 0 1-5-3l-2.5-4.5a1.5 1.5 0 0 1 2.6-1.5L7 14"/>',
 lang='<path d="M4 5h9M8.5 3v2c0 4-2 7-5 9M6 9c1 2.5 3 4.5 6 5.5M13 21l4-9 4 9M14.5 18h5"/>',
 share='<circle cx="18" cy="5" r="2.5"/><circle cx="6" cy="12" r="2.5"/><circle cx="18" cy="19" r="2.5"/><path d="m8.2 10.8 7.6-4.4M8.2 13.2l7.6 4.4"/>',
 dots='<circle cx="5" cy="12" r="1.3"/><circle cx="12" cy="12" r="1.3"/><circle cx="19" cy="12" r="1.3"/>',
 thumbdown='<path d="M17 14V4M17 4h3v10h-3M17 14l-4 7a2 2 0 0 1-2-2v-4H5.5a2 2 0 0 1-2-2.3l1.3-7A2 2 0 0 1 6.8 4H17"/>',
)
def ic(n, c="i", sw=1.9): return f'<svg class="{c}" viewBox="0 0 24 24" style="stroke-width:{sw}" aria-hidden="true">{P[n]}</svg>'

# ------------------------------------------------------------------ Voorbeelddata
V = {
 "zorg": dict(t="Zorgmedewerker thuiszorg", c="Groenhof Zorg", pl="Wateringen", ph="zorg", fit=82, why="Je helpt graag mensen · rustig team", km="2,1 km", min=8, h="24–32 uur", w="€ 15,20 – € 17,40", day="Dag en avond", badge=("hand", "Leerwerkplek")),
 "order": dict(t="Orderpicker dagdienst", c="FlexPlus", via=True, pl="Werkplek in het Westland", ph="logistiek", fit=76, why="Je werkt graag met je handen · vaste tijden", km="3,9 km", min=14, h="32–40 uur", w="€ 14,71", day="Dag", badge=("lang", "Taal leren op het werk")),
 "kas": dict(t="Medewerker tomatenkas", c="Van der Kas Tomaten", pl="Kwintsheul", ph="tuinbouw", fit=74, why="Je bent graag buiten bezig · samen in een team", km="2,8 km", min=11, h="38 uur", w="€ 14,71 – € 15,50", day="Dag", badge=None),
 "school": dict(t="Onderwijsassistent", c="Basisschool De Regenboog", pl="Den Haag Escamp", ph="onderwijs", fit=71, why="Je legt graag dingen uit · veel structuur", km="4,6 km", min=17, h="20–24 uur", w="€ 2.450 – € 2.900 p/m", day="Ochtend en middag", badge=("users", "Werkt met statushouders")),
 "winkel": dict(t="Winkelmedewerker vers", c="Buurtsuper Wateringen", pl="Wateringen", ph="winkel", fit=64, why="Je bent graag onder de mensen", km="0,9 km", min=4, h="12–24 uur", w="€ 14,71", day="Dag en weekend", badge=None),
 "nacht": dict(t="Productiemedewerker", c="Westland Verpakkingen", pl="Honselersdijk", ph="productie", fit=69, why="Je werkt precies · vaste taken", km="3,4 km", min=13, h="36 uur", w="€ 15,80 + toeslag", day="Nachtdienst", badge=None, low="nachtdienst"),
 "schoon": dict(t="Schoonmaker kantoren", c="CleanWest", pl="Rijswijk", ph="schoonmaak", fit=61, why="Je werkt graag zelfstandig", km="4,9 km", min=18, h="15–20 uur", w="€ 14,90", day="Vroege ochtend", badge=None),
 "kok": dict(t="Hulpkok", c="Restaurant De Haven", pl="Poeldijk", ph="horeca", fit=58, why="Je werkt graag in een druk team", km="3,1 km", min=12, h="24 uur", w="€ 14,71", day="Avond en weekend", badge=None),
}
def fitclass(f): return "hi" if f >= 75 else ("mid" if f >= 60 else "lo")

CSS = '''
:root{--bg:#f5f2ee;--surface:#fffcfa;--text:#122033;--muted:#5a6a7d;--border:#ddd5cc;--pearl:#efe9e3;--brand:#0f2d5c;--brand-deep:#0a2044;--accent-hover:#163a6b;--accent-soft:#e7eef7;--coral:#f54a1b;
--success:#15803d;--success-soft:#ecfdf3;--warn:#a65b00;--warn-soft:#fff8e7;--danger:#b42318;--danger-soft:#fef3f2;
--gold:#c9a227;--gold-light:#e4c65a;--gold-deep:#a6851c;--gold-ink:#5c4a0f;--gold-soft:#faf6e8;
--font:"Inter","Segoe UI","Helvetica Neue",Arial,sans-serif;--shadow:0 1px 2px rgba(15,45,92,.06);--shadow-md:0 4px 14px rgba(15,45,92,.10);--shadow-lg:0 12px 32px rgba(15,45,92,.14);--radius-sm:8px;--radius:12px;--radius-lg:16px;--radius-pill:999px;
--text-xs:.75rem;--text-sm:.875rem;--text-md:1rem;--text-lg:1.125rem;--text-xl:1.375rem;--text-2xl:1.75rem;
--ring:#1d4ed8;--ring-fill:#2563eb}
*{box-sizing:border-box}html,body{margin:0}
body{font-family:var(--font);background:var(--bg);color:var(--text);line-height:1.5;font-size:16px}
p,h1,h2,h3,ul,ol{margin:0}b,strong{font-weight:600}ul,ol{padding:0;list-style:none}
svg.i{width:18px;height:18px;fill:none;stroke:currentColor;stroke-linecap:round;stroke-linejoin:round;flex-shrink:0}
.muted{color:var(--muted)}.sm{font-size:var(--text-sm)}.xs{font-size:var(--text-xs)}.b6{font-weight:600}
.demo{font-size:var(--text-xs);color:var(--muted);border:1px dashed var(--border);border-radius:6px;padding:0 8px;background:var(--surface);white-space:nowrap}
.hd{position:sticky;top:0;z-index:40;height:64px;background:var(--surface);display:flex;align-items:center;justify-content:space-between;padding:0 24px;border-bottom:1px solid var(--pearl)}
.wm{display:flex;align-items:center;gap:10px}.wm img{width:40px;height:40px}.wm b{font-size:22px;font-weight:700;color:var(--brand)}.wm span{font-size:14px;font-weight:600;color:var(--brand);margin-inline-start:14px}
.hr{display:flex;align-items:center;gap:12px}
.cb{width:40px;height:40px;border-radius:50%;border:1px solid var(--border);background:var(--surface);display:flex;align-items:center;justify-content:center;color:var(--brand)}
.lang{height:40px;padding:0 12px;border:1px solid var(--border);border-radius:var(--radius-sm);display:flex;align-items:center;gap:8px;font-size:14px;font-weight:600;color:var(--brand);background:var(--surface)}
.flag{width:20px;height:14px;border-radius:3px;background:linear-gradient(#ae1c28 33%,#fff 33% 66%,#21468b 66%);box-shadow:0 0 0 1px rgba(0,0,0,.08)}
.who{text-align:end;margin-inline-start:4px}.who b{display:block;font-size:14px;font-weight:600;color:var(--brand)}.who small{font-size:12px;color:var(--muted)}
.jt{display:flex;gap:4px;border-bottom:1px solid var(--pearl);margin:6px 0 14px}.jt a{padding:10px 14px;font-weight:600;color:var(--muted);border-bottom:3px solid transparent;margin-bottom:-1px}.jt a.on{color:var(--brand);border-bottom-color:var(--accent,var(--brand))}.jt small{font-weight:600;background:var(--pearl);border-radius:999px;padding:1px 7px;margin-left:4px;font-size:12px}
.bn{position:fixed;left:0;right:0;bottom:0;height:64px;background:var(--surface);border-top:1px solid var(--pearl);display:grid;grid-template-columns:repeat(5,1fr);padding:4px 8px;z-index:50}
.bn a{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:2px;font-size:12px;font-weight:600;color:var(--muted);border-radius:var(--radius-sm);white-space:nowrap}
.bn a.on{background:var(--accent-soft);color:var(--brand)}
.d .bn{padding:4px calc(50% - 480px)}
.btn{height:44px;border-radius:var(--radius-sm);display:inline-flex;align-items:center;justify-content:center;gap:8px;font-size:var(--text-sm);font-weight:600;border:1px solid var(--border);color:var(--brand);background:var(--surface);padding:0 16px;white-space:nowrap;font-family:inherit}
.btn.pri{background:var(--brand);color:#fff;border-color:var(--brand)}.btn.sm{height:36px;padding:0 12px}
.btn.ic{width:44px;padding:0}.btn.ghost{border-color:transparent;background:transparent}
.lnk{white-space:nowrap;font-size:var(--text-sm);font-weight:600;color:var(--brand);display:inline-flex;align-items:center;gap:4px;min-height:32px}.lnk .i{width:16px;height:16px}
.pill{font-size:12px;font-weight:600;border-radius:var(--radius-pill);padding:2px 10px;background:var(--accent-soft);color:var(--brand);display:inline-flex;align-items:center;gap:5px;white-space:nowrap}
.pill .i{width:13px;height:13px}.pill.ok{background:var(--success-soft);color:var(--success)}.pill.gold{background:var(--gold-soft);color:var(--gold-ink)}.pill.warn{background:var(--warn-soft);color:var(--warn)}.pill.grey{background:var(--pearl);color:var(--muted)}
.card{background:var(--surface);border-radius:var(--radius);box-shadow:var(--shadow-md);border:1px solid var(--pearl)}
.fit{display:inline-flex;align-items:center;gap:6px;font-size:13px;font-weight:600;border-radius:var(--radius-pill);padding:3px 10px 3px 4px;white-space:nowrap}
.fit .dn{width:22px;height:22px;border-radius:50%;display:grid;place-items:center;background:var(--surface)}
.fit .dn .i{width:13px;height:13px}
.fit.hi{background:var(--success-soft);color:var(--success)}.fit.mid{background:var(--accent-soft);color:var(--brand)}.fit.lo{background:var(--pearl);color:var(--muted)}
.why{font-size:13px;color:var(--muted);display:flex;gap:6px;align-items:flex-start}.why .i{width:14px;height:14px;margin-top:3px;color:var(--gold-deep)}
.low{font-size:12px;color:var(--warn);background:var(--warn-soft);border-radius:6px;padding:2px 8px;display:inline-flex;gap:5px;align-items:center}.low .i{width:13px;height:13px}
.meta{display:flex;flex-wrap:wrap;gap:4px 14px;font-size:13px;color:var(--muted)}.meta span{display:inline-flex;align-items:center;gap:5px;white-space:nowrap}.meta .i{width:15px;height:15px}
.meta .tt{color:var(--ring);font-weight:600}
.co{font-size:13px;color:var(--muted);display:flex;align-items:center;gap:0 5px;flex-wrap:wrap}.co b,.co span{white-space:nowrap}.co .i{width:14px;height:14px}.co b{color:var(--text);font-weight:600}
.via{color:var(--brand)}
.lob{display:flex;gap:10px;align-items:center}.lob img{width:40px;height:40px;flex-shrink:0}
.fbar{display:flex;align-items:center;gap:8px;padding:10px 24px;background:var(--surface);border-bottom:1px solid var(--pearl);height:64px}
.fld{height:44px;border:1px solid var(--border);border-radius:var(--radius-sm);background:var(--surface);display:flex;align-items:center;gap:8px;padding:0 12px;font-size:var(--text-sm);color:var(--muted);white-space:nowrap}
.fld b{color:var(--text);font-weight:400}.fld .i{color:var(--brand)}
.chip{height:40px;border:1px solid var(--border);border-radius:var(--radius-pill);background:var(--surface);display:inline-flex;align-items:center;gap:6px;padding:0 14px;font-size:var(--text-sm);font-weight:600;color:var(--brand);white-space:nowrap}
.chip.on{background:var(--accent-soft);border-color:var(--brand)}.chip .i{width:16px;height:16px}
.chip.tt{border-color:var(--ring);color:var(--ring);background:color-mix(in srgb,var(--ring-fill) 8%,var(--surface))}
.cnt{min-width:20px;height:20px;border-radius:10px;background:var(--brand);color:#fff;font-size:12px;display:grid;place-items:center;padding:0 5px}
.seg{display:inline-flex;border:1px solid var(--border);border-radius:var(--radius-sm);overflow:hidden;height:40px}.seg a{display:flex;align-items:center;gap:6px;padding:0 14px;font-size:var(--text-sm);font-weight:600;color:var(--muted)}.seg a.on{background:var(--brand);color:#fff}
.sp{flex:1}
.mapw{position:relative;overflow:hidden;background:#eef0f2}
.mapw>img.base{position:absolute;left:0;filter:saturate(.42) brightness(1.04) contrast(.96)}
.mapw svg.rings{position:absolute;left:0;overflow:visible}
.rl{position:absolute;transform:translate(-50%,-50%);background:var(--surface);color:var(--ring);border:2px solid var(--ring);border-radius:var(--radius-pill);font-size:13px;font-weight:600;padding:2px 10px 2px 7px;display:flex;gap:5px;align-items:center;box-shadow:0 2px 6px rgba(29,78,216,.25);white-space:nowrap}
.rl .i{width:15px;height:15px}
.home{position:absolute;transform:translate(-50%,-50%);width:40px;height:40px;border-radius:50%;background:var(--brand);color:#fff;display:grid;place-items:center;box-shadow:0 0 0 6px color-mix(in srgb,var(--brand) 22%,transparent),0 0 0 14px color-mix(in srgb,var(--brand) 9%,transparent),var(--shadow-md);border:2.5px solid #fff}
.home .i{width:18px;height:18px}
.homel{position:absolute;transform:translate(-50%,0);font-size:12px;font-weight:600;color:var(--brand);background:var(--surface);border-radius:6px;padding:1px 8px;box-shadow:var(--shadow);white-space:nowrap}
.cl{position:absolute;transform:translate(-50%,-50%);border-radius:50%;background:var(--brand);color:#fff;font-size:13px;font-weight:600;display:grid;place-items:center;border:2.5px solid #fff;box-shadow:var(--shadow-md)}
.cl.out{background:color-mix(in srgb,var(--brand) 38%,var(--surface));box-shadow:none}
.fp{position:absolute;transform:translate(-50%,-100%);background:var(--surface);color:var(--brand);font-size:12px;font-weight:600;border-radius:var(--radius-pill);padding:2px 8px;border:2px solid var(--brand);box-shadow:var(--shadow-md);white-space:nowrap}
.fp:after{content:"";position:absolute;left:50%;bottom:-6px;transform:translateX(-50%) rotate(45deg);width:8px;height:8px;background:inherit;border-right:2px solid;border-bottom:2px solid;border-color:inherit}
.fp.gold{border-color:var(--gold);background:var(--gold-soft);color:var(--gold-ink)}
.fp.sel{background:var(--coral);border-color:var(--coral);color:#fff;font-size:13px;padding:3px 10px;z-index:3}
.mctl{position:absolute;display:flex;flex-direction:column;background:var(--surface);border-radius:var(--radius-sm);box-shadow:var(--shadow-md);border:1px solid var(--pearl)}.mctl a{width:40px;height:40px;display:grid;place-items:center;color:var(--brand)}.mctl a+a{border-top:1px solid var(--pearl)}
.legend{position:absolute;background:color-mix(in srgb,var(--surface) 94%,transparent);border-radius:var(--radius);box-shadow:var(--shadow-md);padding:10px 14px;font-size:12px;color:var(--muted)}
.legend b{color:var(--text);font-size:13px;display:flex;gap:6px;align-items:center}.legend .i{width:15px;height:15px;color:var(--ring)}
.sw{display:inline-block;width:18px;height:12px;border-radius:3px;border:2px solid var(--ring);vertical-align:-2px;margin-inline-end:4px}
'''

def header(m):
    if m:
        return (f'<header class="hd" style="height:56px;padding:0 12px"><div class="wm"><img src="{LOGO}" alt="" style="width:34px;height:34px"><b style="font-size:20px">Lobsy</b></div>'
                f'<span class="demo">Voorbeelddata</span><div class="hr" style="gap:8px"><div class="cb">{ic("bell")}</div><div class="cb">{ic("user")}</div></div></header>')
    return (f'<header class="hd"><div class="wm"><img src="{LOGO}" alt=""><b>Lobsy</b><span>Dichtbij genoeg om het pantser te laten vallen</span></div><span class="demo">Voorbeelddata</span>'
            f'<div class="hr"><div class="cb">{ic("bell")}</div><div class="lang"><i class="flag"></i>NL {ic("chevd")}</div>'
            f'<div class="who"><b>Samira El Amrani</b><small>Kandidaat</small></div><div class="cb">{ic("user")}</div></div></header>')
def nav(m, on):
    # Nav order per Dennis (add-on prompt after Mijn Paspoort 08, Werkgevers ON). Bewaard is a tab inside Sollicitaties, never a nav item.
    it = [("compass", "De ontdekkingsreis", "Reis"), ("book", "Mijn Paspoort", "Paspoort"), ("sun", "Carrière", "Carrière"),
          ("map", "Banenkaart", "Banenkaart"), ("clip", "Sollicitaties", "Sollicitaties")]
    return '<nav class="bn" aria-label="Hoofdmenu">' + "".join(f'<a class="{"on" if t == on else ""}">{ic(i)}{ms if m else t}</a>' for i, t, ms in it) + '</nav>'
def page(title, body, m, on="Banenkaart", css="", fixed=False):
    ov = "html,body{height:100%;overflow:hidden}" if fixed else ""
    return (f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><title>{title} · Lobsy</title><style>{CSS}{ov}{css}</style></head>'
            f'<body class="{"m" if m else "d"}">{header(m)}{body}{nav(m, on)}</body></html>')
def jtabs(on, n_app=3, n_saved=6):
    t = [("Sollicitaties", n_app), ("Bewaard", n_saved)]  # CandidateJobListTabs (paspoort D8)
    return ('<div class="jt" role="tablist">' + "".join(f'<a role="tab" class="{"on" if a == on else ""}">{a} <small>{c}</small></a>' for a, c in t) + '</div>')
def fitb(f):
    return f'<span class="fit {fitclass(f)}"><span class="dn">{ic("spark", sw=2.2)}</span>{f}% past bij jou</span>'
def company(v):
    if v.get("via"):
        return f'<p class="co">{ic("building")}<span>via uitzendbureau <b class="via">{v["c"]}</b></span></p>'
    return f'<p class="co">{ic("building")}<b>{v["c"]}</b><span>· {v["pl"]}</span></p>'

# ------------------------------------------------------------------ map layer
def inside(pt, poly):
    x, y = pt; c = False
    for i in range(len(poly)):
        x1, y1 = poly[i]; x2, y2 = poly[i - 1]
        if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1: c = not c
    return c
# clusters/pins in d1 pixel space (980x772 image), placed on the real towns of the basemap. Voorbeelddata.
CL = [((478, 352), 9), ((282, 480), 6), ((200, 562), 14), ((238, 396), 4), ((455, 290), 12), ((300, 238), 7), ((590, 280), 11),
      ((740, 505), 18), ((628, 528), 3), ((338, 652), 5), ((560, 100), 26), ((80, 402), 4), ((572, 642), 2), ((712, 132), 9),
      ((862, 292), 5), ((955, 470), 7), ((42, 540), 3), ((880, 600), 4), ((150, 690), 2)]
FP = [((336, 472), "74%", "gold"), ((500, 434), "64%", ""), ((392, 352), "76%", "gold"), ((484, 332), "71%", ""), ((262, 430), "69%", "")]
SEL = (384, 446)
SC = {"d1": (1, 980, 708), "m1": (2 ** -0.7, 390, 620), "det": (2 ** -1.05, 420, 236), "mdet": (2 ** -1.5, 358, 150)}
def tf(pt, scene):
    k = SC[scene][0]; h = PROJ[scene]["pts"]["home"]
    return (h[0] + (pt[0] - 430) * k, h[1] + (pt[1] - 400) * k)
def mapview(scene, W, H, dy=0, clusters=True, sel=True, labels=True, fp=True, extra=None):
    pj = PROJ[scene]; _, iw, ih = SC[scene]
    rings = pj["rings"]; r30 = rings["30"]
    def path(pts): return "M" + " L".join(f"{x:.1f},{y:.1f}" for x, y in pts) + "Z"
    # graduated soft fill (stacks: centre strongest) + white halo + strong line; 30 min dashed
    svg = f'<svg class="rings" width="{iw}" height="{ih}" style="top:{dy}px" viewBox="0 0 {iw} {ih}">'
    for m_, op in (("30", .07), ("20", .08), ("10", .10)):
        svg += f'<path d="{path(rings[m_])}" fill="var(--ring-fill)" fill-opacity="{op}"/>'
    for m_, w_ in (("30", 2.4), ("20", 2.8), ("10", 3.2)):
        svg += f'<path d="{path(rings[m_])}" fill="none" stroke="#fff" stroke-width="{w_ + 4}" stroke-opacity=".9" stroke-linejoin="round"/>'
        dash = ' stroke-dasharray="10 6"' if m_ == "30" else ""
        svg += f'<path d="{path(rings[m_])}" fill="none" stroke="var(--ring)" stroke-width="{w_}" stroke-linejoin="round"{dash}/>'
    svg += '</svg>'
    h = f'<img class="base" src="{MAP[scene]}" style="top:{dy}px;width:{iw}px;height:{ih}px" alt="">{svg}'
    if clusters:
        for pt, n in CL:
            x, y = tf(pt, scene); y += dy
            if not (-10 < x < W + 10 and -10 < y < H + 10): continue
            out = not inside(tf(pt, scene), r30)
            sz = (26 + min(n, 26) * .9) * (1 if scene == "d1" else .8)
            h += f'<span class="cl{" out" if out else ""}" style="left:{x:.0f}px;top:{y:.0f}px;width:{sz:.0f}px;height:{sz:.0f}px">{n}</span>'
        for pt, lab, cls in (FP if fp else [f for f in FP if f[2]]):
            x, y = tf(pt, scene); h += f'<span class="fp {cls}" style="left:{x:.0f}px;top:{y + dy:.0f}px">{lab}</span>'
    if sel:
        x, y = tf(SEL, scene); h += f'<span class="fp sel" style="left:{x:.0f}px;top:{y + dy:.0f}px">82%</span>'
    if extra:
        x, y = tf(extra[0], scene); h += f'<span class="fp" style="left:{x:.0f}px;top:{y + dy:.0f}px">{extra[1]}</span>'
    hx, hy = pj["pts"]["home"]
    h += f'<span class="home" style="left:{hx}px;top:{hy + dy}px">{ic("home", sw=2.2)}</span>'
    if labels:
        h += f'<span class="homel" style="left:{hx}px;top:{hy + dy + 26}px">Thuis</span>'
        for m_, (x, y) in pj["labels"].items():
            if -20 < y + dy < H - 10 and 20 < x < W - 20:
                h += f'<span class="rl" style="left:{x}px;top:{y + dy}px">{ic("bike", sw=2)}{m_} min</span>'
    return h

# ------------------------------------------------------------------ desktop: banenkaart
def badge2(v, long=False):
    if v.get("low"):
        return f'<span class="low">{ic("thumbdown")}Staat lager: {v["low"]}{". Je gaf aan dat je dit liever niet doet." if long else ""}</span>'
    return f'<span class="pill gold">{ic(v["badge"][0])}{v["badge"][1]}</span>' if v.get("badge") else ""
def jobrow(k, sel=False):
    v = V[k]
    return (f'<li class="jr{" sel" if sel else ""}"><img src="{PH[v["ph"]]}" alt=""><div class="jb">'
            f'<div class="jt"><b>{v["t"]}</b><span class="hrt">{ic("heart")}</span></div>{company(v)}'
            f'<div class="meta"><span class="tt">{ic("bike")}{v["min"]} min</span><span>{ic("clock")}{v["h"]}</span><span>{ic("euro")}{v["w"].split(" –")[0]}</span></div>'
            f'<div class="fr">{fitb(v["fit"])}{badge2(v)}</div><p class="why">{ic("spark", sw=2.2)}{v["why"]}</p></div></li>')
D1CSS = '''
.wrap{display:grid;grid-template-columns:460px 1fr;height:calc(100vh - 192px)}
.side{background:var(--bg);border-inline-end:1px solid var(--pearl);overflow:hidden;display:flex;flex-direction:column}
.sh{padding:14px 20px 10px;display:flex;align-items:center;gap:8px}.sh h1{font-size:var(--text-lg);font-weight:600;color:var(--brand);line-height:1.3}.sh p{font-size:13px;color:var(--muted)}
.jl{padding:0 12px 12px;display:flex;flex-direction:column;gap:8px}
.jr{display:grid;grid-template-columns:92px 1fr;gap:12px;background:var(--surface);border:1px solid var(--pearl);border-radius:var(--radius);padding:10px;box-shadow:var(--shadow)}
.jr img{width:92px;height:100%;min-height:104px;object-fit:cover;border-radius:var(--radius-sm)}
.jr.sel{border-color:var(--brand);box-shadow:0 0 0 1px var(--brand),var(--shadow-md)}
.jr .jb{display:flex;flex-direction:column;gap:4px;min-width:0}.jt{display:flex;justify-content:space-between;gap:8px}.jt b{font-size:15px;color:var(--brand);line-height:1.3}
.hrt{color:var(--muted)}.fr{display:flex;gap:6px;align-items:center;flex-wrap:wrap;margin-top:2px}
.tip{margin:0 12px 10px;background:var(--gold-soft);border-radius:var(--radius);padding:8px 12px;font-size:13px;color:var(--gold-ink);display:flex;gap:10px;align-items:center}.tip img{width:34px;height:34px}
.car{position:absolute;left:16px;top:14px;display:flex;gap:10px;z-index:5}
.tm{display:flex;gap:10px;align-items:center;background:var(--surface);border-radius:var(--radius);padding:8px 12px 8px 8px;box-shadow:var(--shadow-lg);border:2px solid var(--gold-light);width:300px}
.tm img.ph{width:56px;height:56px;border-radius:var(--radius-sm);object-fit:cover}.tm small{font-size:12px;font-weight:600;color:var(--gold-ink);display:flex;gap:4px;align-items:center}.tm small .i{width:14px;height:14px}
.tm b{display:block;font-size:14px;color:var(--brand);line-height:1.3}.tm .sub{font-size:12px;color:var(--muted)}
.hl{display:flex;gap:10px;align-items:center;background:var(--surface);border-radius:var(--radius);padding:8px 12px 8px 8px;box-shadow:var(--shadow-md);width:236px}
.hl img{width:48px;height:48px;border-radius:var(--radius-sm);object-fit:cover}.hl b{display:block;font-size:13px;color:var(--brand);line-height:1.3}.hl span{font-size:12px;color:var(--muted)}
.selc{position:absolute;left:16px;bottom:16px;width:420px;z-index:6;padding:12px;display:grid;grid-template-columns:96px 1fr;gap:12px}
.selc>img{width:96px;height:96px;border-radius:var(--radius-sm);object-fit:cover}
.selc .x{position:absolute;right:8px;top:8px;width:32px;height:32px;border-radius:50%;display:grid;place-items:center;color:var(--muted)}
.selc .bd{display:flex;flex-direction:column;gap:5px;min-width:0}.selc h2{font-size:var(--text-lg);font-weight:600;color:var(--brand);line-height:1.3;padding-inline-end:28px}
.selc .ac{grid-column:1/-1;display:flex;gap:8px}
'''
def filterbar(view="Kaart"):
    return (f'<div class="fbar"><span class="fld" style="width:260px">{ic("search")}Wat voor werk zoek je?</span>'
            f'<span class="fld" style="width:250px">{ic("home")}<b>Herenstraat 20, Wateringen</b></span>'
            f'<span class="chip tt">{ic("bike")}Fiets · 20 min{ic("chevd")}</span><span class="chip">{ic("clock")}Uren{ic("chevd")}</span>'
            f'<span class="chip">{ic("building")}Branche{ic("chevd")}</span><span class="chip on">{ic("spark")}Past bij mij</span>'
            f'<span class="chip">{ic("sliders")}Meer<span class="cnt">1</span></span><span class="sp"></span>'
            f'<span class="seg"><a class="{"on" if view == "Kaart" else ""}">{ic("map")}Kaart</a><a class="{"on" if view == "Lijst" else ""}">{ic("list")}Lijst</a></span></div>')
def d1_body():
    z = V["zorg"]
    side = (f'<aside class="side"><div class="sh"><div><h1>34 banen · 20 min fietsen</h1><p>Beste match bovenaan. Je “liever niet” staat lager.</p></div><span class="sp"></span>'
            f'<span class="lnk">{ic("sort")}Past het best{ic("chevd")}</span></div>'
            f'<div class="tip"><img src="{MASCOT}" alt=""><span><b>Tip van Lobsy:</b> zet je reistijd op 30 min. Dan zie je 21 banen meer.</span></div>'
            f'<ul class="jl">{jobrow("zorg", True)}{jobrow("order")}{jobrow("kas")}{jobrow("nacht")}</ul></aside>')
    car = (f'<div class="car"><div class="tm"><img class="ph" src="{PH["zorg"]}" alt=""><div><small>{ic("spark", sw=2.2)}Jouw top-match</small><b>Zorgmedewerker thuiszorg</b><span class="sub">82% past bij jou · 8 min</span></div>{ic("right")}</div>'
           f'<div class="hl"><img src="{PH["tuinbouw"]}" alt=""><div><b>Nieuw vandaag: 6 banen</b><span>In jouw 20 minuten</span></div></div></div>')
    selc = (f'<article class="card selc"><img src="{PH["zorg"]}" alt=""><span class="x">{ic("x")}</span><div class="bd">'
            f'<h2>{z["t"]}</h2>{company(z)}<div class="meta"><span class="tt">{ic("bike")}8 min fietsen</span><span>{ic("clock")}{z["h"]}</span><span>{ic("euro")}€ 15,20 per uur</span></div>'
            f'<div class="fr">{fitb(82)}<span class="pill gold">{ic("hand")}Leerwerkplek</span></div>'
            f'<p class="why">{ic("spark", sw=2.2)}Je helpt graag mensen en je houdt van een rustig team.</p></div>'
            f'<div class="ac"><span class="btn pri" style="flex:1">Bekijk vacature{ic("right")}</span><span class="btn">{ic("heart")}Bewaar</span></div></article>')
    ctl = f'<div class="mctl" style="right:16px;top:16px"><a>{ic("plus")}</a><a>{ic("minus")}</a><a>{ic("target")}</a></div>'
    leg = (f'<div class="legend" style="right:16px;bottom:16px;width:236px"><b>{ic("bike", sw=2)}Reistijd met de fiets</b>'
           f'<div style="display:flex;gap:10px;margin:6px 0 4px"><span><i class="sw" style="background:color-mix(in srgb,var(--ring-fill) 30%,#fff)"></i>10</span><span><i class="sw" style="background:color-mix(in srgb,var(--ring-fill) 18%,#fff)"></i>20</span><span><i class="sw" style="background:color-mix(in srgb,var(--ring-fill) 8%,#fff);border-style:dashed"></i>30 min</span></div>'
           f'Over echte wegen en fietspaden, geen cirkel.</div>')
    mp = f'<section class="mapw">{mapview("d1", 980, 708, dy=0)}{car}{selc}{ctl}{leg}</section>'
    return filterbar() + f'<main class="wrap">{side}{mp}</main>'
def d1():
    return page("Banenkaart", d1_body(), False, css=D1CSS, fixed=True)

# ------------------------------------------------------------------ desktop: lijst
D2CSS = '''
.lw{max-width:1240px;margin:0 auto;padding:20px 24px 96px;display:grid;grid-template-columns:1fr 330px;gap:24px;align-items:start}
.lh{display:flex;align-items:end;gap:12px;margin-bottom:12px}.lh h1{font-size:var(--text-xl);font-weight:600;color:var(--brand);line-height:1.3}.lh p{font-size:14px;color:var(--muted)}
.rows{display:flex;flex-direction:column;gap:12px}
.row{display:grid;grid-template-columns:200px 1fr 170px;gap:18px;padding:12px;background:var(--surface);border:1px solid var(--pearl);border-radius:var(--radius);box-shadow:var(--shadow)}
.row img{width:200px;height:150px;object-fit:cover;border-radius:var(--radius-sm)}
.row .c{display:flex;flex-direction:column;gap:6px;min-width:0}.row h2{font-size:var(--text-lg);font-weight:600;color:var(--brand);line-height:1.3}
.row .r{display:flex;flex-direction:column;align-items:flex-end;justify-content:space-between;text-align:end;padding:4px 4px 2px 0}
.big{font-size:22px;font-weight:600;color:var(--ring);display:flex;gap:6px;align-items:center}.big .i{width:22px;height:22px}
.row.lowr{background:color-mix(in srgb,var(--surface) 60%,var(--bg))}
.fr{display:flex;gap:6px;flex-wrap:wrap;align-items:center}.hrt{color:var(--muted)}
.rail{display:flex;flex-direction:column;gap:16px;position:sticky;top:84px}
.rc{padding:16px}.rc h3{font-size:var(--text-md);font-weight:600;color:var(--brand);margin-bottom:8px}
.mini{height:190px;border-radius:var(--radius-sm);margin-bottom:10px}
'''
def row(k):
    v = V[k]
    return (f'<article class="row{" lowr" if v.get("low") else ""}"><img src="{PH[v["ph"]]}" alt=""><div class="c"><h2>{v["t"]}</h2>{company(v)}'
            f'<div class="fr">{fitb(v["fit"])}{"" if v.get("low") else badge2(v)}</div><p class="why">{ic("spark", sw=2.2)}{v["why"]}</p>'
            f'<div class="meta"><span>{ic("clock")}{v["h"]}</span><span>{ic("sun")}{v["day"]}</span><span>{ic("euro")}{v["w"]}</span></div>'
            f'{badge2(v, True) if v.get("low") else ""}</div>'
            f'<div class="r"><span class="hrt">{ic("heart")}</span><div><div class="big">{ic("bike", sw=2)}{v["min"]} min</div><p class="xs muted">{v["km"]} fietsen</p></div>'
            f'<span class="btn sm">Bekijk{ic("right")}</span></div></article>')
def d2():
    rail = (f'<aside class="rail"><div class="card rc"><h3>Jouw reistijd</h3><div class="mapw mini">{mapview("mdet", 298, 190, dy=20, clusters=False, sel=False, labels=False)}</div>'
            f'<p class="sm">Binnen <b>20 min fietsen</b> van huis. Pas het aan met de knop “Fiets · 20 min”.</p></div>'
            f'<div class="card rc"><h3>Zo sorteren we</h3><div class="lob" style="align-items:flex-start"><img src="{MASCOT}" alt="" style="width:36px;height:36px">'
            f'<p class="sm">Banen die het best bij je paspoort passen, staan bovenaan. Werk dat je liever niet doet, staat lager. Je ziet het nog wel.</p></div>'
            f'<span class="lnk" style="margin-top:6px">{ic("book")}Mijn Paspoort bekijken</span></div></aside>')
    main = (f'<section><div class="lh"><div><h1>34 banen binnen 20 min fietsen</h1><p>Vanaf Herenstraat 20, Wateringen</p></div><span class="sp"></span><span class="lnk">{ic("sort")}Sorteer: Past het best{ic("chevd")}</span></div>'
            f'<div class="rows">{row("zorg")}{row("order")}{row("kas")}{row("school")}{row("nacht")}</div></section>')
    return page("Banen in een lijst", filterbar("Lijst") + f'<main class="lw">{main}{rail}</main>', False, css=D2CSS)

# ------------------------------------------------------------------ desktop: vacature-detail
D3CSS = '''
.dw{max-width:1200px;margin:0 auto;padding:16px 24px 110px;display:grid;grid-template-columns:1fr 400px;gap:28px;align-items:start}
.hero{height:250px;border-radius:var(--radius-lg);overflow:hidden;position:relative;margin-top:6px}.hero img{width:100%;height:100%;object-fit:cover}
.h1{font-size:var(--text-2xl);font-weight:700;color:var(--brand);line-height:1.25;margin-top:16px}
.facts{display:grid;grid-template-columns:repeat(4,1fr);gap:10px;margin:14px 0 4px}
.fact{background:var(--surface);border:1px solid var(--pearl);border-radius:var(--radius);padding:10px 12px;display:flex;gap:10px;align-items:center}.fact .i{width:22px;height:22px;color:var(--brand)}
.fact small{display:block;font-size:12px;color:var(--muted)}.fact b{font-size:14px;color:var(--text);line-height:1.3;display:block}
.sec{margin-top:22px}.sec h2{font-size:var(--text-lg);font-weight:600;color:var(--brand);margin-bottom:8px;display:flex;gap:8px;align-items:center}
.todo li{display:flex;gap:10px;align-items:flex-start;padding:5px 0;font-size:15px}.todo .i{color:var(--success);margin-top:3px}
.emp{padding:18px}
.kv{display:flex;gap:8px;flex-wrap:wrap;margin:6px 0 12px}.kv span{border:1px solid var(--border);border-radius:var(--radius-pill);padding:4px 12px;font-size:14px;font-weight:600;color:var(--brand);background:var(--surface)}
.bt{display:grid;grid-template-columns:1fr 1fr 1fr;gap:10px}.bt div{background:var(--gold-soft);border-radius:var(--radius);padding:10px 12px;font-size:13px;color:var(--gold-ink);display:flex;gap:8px;align-items:flex-start}.bt .i{margin-top:2px}
.by{font-size:12px;color:var(--muted);display:inline-flex;gap:4px;align-items:center;border:1px dashed var(--border);border-radius:6px;padding:0 8px;font-weight:400}.by .i{width:13px;height:13px}
.side{display:flex;flex-direction:column;gap:14px;position:sticky;top:84px}
.fc{padding:18px}.fc .hd2{display:flex;gap:12px;align-items:center}.ring{width:64px;height:64px;border-radius:50%;display:grid;place-items:center;flex-shrink:0}
.ring span{width:50px;height:50px;border-radius:50%;background:var(--surface);display:grid;place-items:center;font-weight:700;color:var(--success);font-size:17px}
.bars{display:flex;flex-direction:column;gap:10px;margin-top:14px}.bar b{font-size:14px;display:flex;justify-content:space-between}.bar b span{color:var(--muted);font-weight:400}
.bar i{display:block;height:8px;border-radius:4px;background:var(--pearl);margin:4px 0 3px;overflow:hidden}.bar i em{display:block;height:100%;background:var(--success);border-radius:4px}
.bar p{font-size:13px;color:var(--muted)}
.gap{margin-top:12px;background:var(--accent-soft);border-radius:var(--radius-sm);padding:8px 12px;font-size:13px;display:flex;gap:8px}.gap .i{color:var(--brand);margin-top:2px}
.tc{padding:14px}.tc .mapw{height:170px;border-radius:var(--radius-sm);margin:8px 0}
.tseg{display:grid;grid-template-columns:repeat(4,1fr);border:1px solid var(--border);border-radius:var(--radius-sm);overflow:hidden}.tseg a{display:flex;flex-direction:column;align-items:center;padding:6px 0;font-size:12px;font-weight:600;color:var(--muted)}.tseg a b{font-size:13px}.tseg a.on{background:var(--accent-soft);color:var(--brand);box-shadow:inset 0 -3px 0 var(--brand)}
.apply{padding:16px;display:flex;flex-direction:column;gap:8px}
'''
def ringc(p): return f'<div class="ring" style="background:conic-gradient(var(--success) 0 {p}%,var(--pearl) {p}% 100%)"><span>{p}%</span></div>'
def dims(D):
    return '<div class="bars">' + "".join(f'<div class="bar"><b>{a}<span>{p}%</span></b><i><em style="width:{p}%"></em></i><p>{w}</p></div>' for a, p, w in D) + '</div>'
DZ = [("Cultuur", 90, "Rustig team. Je werkt veel samen."), ("Waarden", 85, "Mensen helpen vind je belangrijk."),
      ("Competenties", 78, "Je luistert goed en je kunt plannen."), ("Interesses", 70, "Je zorgt graag voor anderen.")]
DO = [("Cultuur", 80, "Duidelijke taken en vaste tijden."), ("Waarden", 72, "Zekerheid vind je belangrijk."),
      ("Competenties", 78, "Je werkt netjes en snel."), ("Interesses", 70, "Je werkt graag met je handen.")]
def d3():
    z = V["zorg"]
    facts = [("clock", "Uren per week", z["h"]), ("euro", "Loon per uur", z["w"]), ("sun", "Wanneer", z["day"]), ("cal", "Begin", "In overleg")]
    main = (f'<section><span class="lnk">{ic("left")}Terug naar de kaart</span><div class="hero"><img src="{PH["zorg"]}" alt=""></div>'
            f'<h1 class="h1">{z["t"]}</h1><div style="display:flex;gap:12px;align-items:center;margin-top:4px">{company(z)}<span class="pill grey">{ic("building")}Branche: Zorg en welzijn</span></div>'
            f'<div class="facts">' + "".join(f'<div class="fact">{ic(i)}<div><small>{a}</small><b>{b}</b></div></div>' for i, a, b in facts) + '</div>'
            f'<div class="sec"><h2>Wat ga je doen?</h2><ul class="todo">'
            f'<li>{ic("check", sw=2.4)}Je helpt ouderen thuis met wassen, aankleden en eten.</li><li>{ic("check", sw=2.4)}Je maakt een praatje en let op hoe het met ze gaat.</li>'
            f'<li>{ic("check", sw=2.4)}Je fietst van huis naar huis in Wateringen en Kwintsheul.</li></ul></div>'
            f'<div class="sec card emp"><h2>{ic("building")}Over Groenhof Zorg <span class="sp"></span><span class="by">{ic("info")}door werkgever opgegeven</span></h2>'
            f'<p class="sm muted">Kernwaarden</p><div class="kv"><span>Samen</span><span>Eerlijk</span><span>Rust</span><span>Groei</span></div>'
            f'<p class="sm muted" style="margin-bottom:6px">Maatschappelijk betrokken</p><div class="bt"><div>{ic("hand")}<span><b>Leerwerkplek</b><br>Je leert het vak terwijl je werkt.</span></div>'
            f'<div>{ic("lang")}<span><b>Taal leren op het werk</b><br>Een taalmaatje helpt je.</span></div><div>{ic("leaf")}<span><b>Werkt duurzaam</b><br>Wijkteams op de e-bike.</span></div></div></div>'
            f'<div class="sec"><h2>Wat vragen we?</h2><ul class="todo"><li>{ic("check", sw=2.4)}Je spreekt een beetje Nederlands (niveau A2 is genoeg).</li><li>{ic("check", sw=2.4)}Diploma? Fijn, maar niet nodig. Je leert het hier.</li></ul></div></section>')
    tseg = "".join(f'<a class="{"on" if o else ""}">{ic(i)}<b>{t}</b></a>' for i, t, o in [("walk", "25 min", 0), ("bike", "8 min", 1), ("bus", "19 min", 0), ("car", "7 min", 0)])
    side = (f'<aside class="side"><div class="card fc"><div class="hd2">{ringc(82)}<div><b style="font-size:17px;color:var(--brand)">Past goed bij jou</b>'
            f'<p class="sm muted">We vergelijken de baan met je paspoort.</p></div></div>{dims(DZ)}'
            f'<div class="gap">{ic("info")}<span><b>Nieuw voor jou:</b> werken met een tillift. Dat leer je in de eerste week.</span></div></div>'
            f'<div class="card tc"><b style="color:var(--brand)">Hoe kom je er?</b><div class="mapw">{mapview("det", 372, 170, dy=-33, clusters=False, sel=True, labels=False)}</div>'
            f'<div class="tseg">{tseg}</div><p class="xs muted" style="margin-top:6px">Vanaf Herenstraat 20 · 2,1 km · binnen je 10 minuten</p></div>'
            f'<div class="card apply"><span class="btn pri" style="height:48px;font-size:16px">{ic("send")}Solliciteer</span><span class="btn">{ic("heart")}Bewaar</span>'
            f'<p class="xs muted" style="text-align:center">Je paspoort gaat mee. Een brief is niet nodig.</p></div></aside>')
    return page("Vacature", f'<main class="dw">{main}{side}</main>', False, css=D3CSS)

# ------------------------------------------------------------------ desktop: sollicitaties
D4CSS = '''
.sw2{max-width:1200px;margin:0 auto;padding:22px 24px 100px;display:grid;grid-template-columns:1fr 320px;gap:24px;align-items:start}
.pt{font-size:var(--text-2xl);font-weight:700;color:var(--brand);line-height:1.25}
.cnts{display:flex;gap:10px;margin:14px 0}.cn{background:var(--surface);border:1px solid var(--pearl);border-radius:var(--radius);padding:10px 16px;min-width:140px}.cn b{font-size:22px;color:var(--brand);display:block;line-height:1.2}.cn span{font-size:13px;color:var(--muted)}
.cn.on{border-color:var(--brand);box-shadow:inset 0 -3px 0 var(--brand)}
.ap{padding:16px 18px;margin-bottom:12px}.ap .t{display:grid;grid-template-columns:64px 1fr auto;gap:14px;align-items:center}.ap img{width:64px;height:64px;border-radius:var(--radius-sm);object-fit:cover}
.ap h2{font-size:var(--text-md);font-weight:600;color:var(--brand)}
.tl{display:grid;grid-template-columns:repeat(4,1fr);margin:16px 4px 4px}
.tl li{position:relative;padding-top:32px;font-size:13px;color:var(--muted);padding-inline-end:8px}
.tl li:before{content:"";position:absolute;top:10px;left:0;right:0;height:3px;background:var(--pearl)}
.tl li.done:before{background:var(--success)}.tl li.now:before{background:linear-gradient(90deg,var(--brand) 12%,var(--pearl) 12%)}
.tl li:last-child:before{right:auto;width:24px}
.tl li i{position:absolute;top:0;left:0;width:24px;height:24px;border-radius:50%;background:var(--surface);border:2.5px solid var(--pearl);display:grid;place-items:center}
.tl li.done i{background:var(--success);border-color:var(--success);color:#fff}.tl li.now i{border-color:var(--brand);box-shadow:0 0 0 4px var(--accent-soft)}.tl li.now i:after{content:"";width:8px;height:8px;border-radius:50%;background:var(--brand)}
.tl li.stop i{background:var(--pearl);border-color:var(--muted);color:var(--muted)}
.tl li i .i{width:13px;height:13px;stroke-width:3}.tl li b{display:block;color:var(--text);font-size:14px}.tl li.now b{color:var(--brand)}
.nx{margin-top:12px;border-radius:var(--radius-sm);padding:10px 12px;background:var(--accent-soft);font-size:14px;display:flex;gap:10px;align-items:center}.nx .i{color:var(--brand)}
.nx.soft{background:var(--bg)}
.rc{padding:16px}.rc h3{font-size:var(--text-md);font-weight:600;color:var(--brand);margin-bottom:8px}
'''
def tl(steps):
    return '<ol class="tl">' + "".join(f'<li class="{c}"><i>{ic("check") if c == "done" else (ic("x") if c == "stop" else "")}</i><b>{a}</b>{b}</li>' for c, a, b in steps) + '</ol>'
def appcard(k, status, pc, steps, nx):
    v = V[k]
    return (f'<article class="card ap"><div class="t"><img src="{PH[v["ph"]]}" alt=""><div><h2>{v["t"]}</h2>{company(v)}</div>'
            f'<div style="display:flex;gap:8px;align-items:center"><span class="pill {pc}">{status}</span><span class="btn ic ghost">{ic("dots")}</span></div></div>{tl(steps)}{nx}</article>')
def d4():
    a1 = appcard("zorg", "Gesprek gepland", "ok", [("done", "Verstuurd", "ma 21 sep"), ("done", "Gezien door werkgever", "wo 23 sep"), ("now", "Gesprek", "ma 5 okt · 10:00"), ("", "Uitslag", "Na het gesprek")],
                 f'<div class="nx">{ic("chat")}<span><b>Wat nu?</b> Oefen je gesprek met Lobsy. Het duurt 10 minuten.</span><span class="sp"></span><span class="btn sm">Oefenen</span></div>')
    a2 = appcard("order", "Gezien", "", [("done", "Verstuurd", "do 24 sep"), ("now", "Gezien door uitzendbureau", "vr 25 sep"), ("", "Gesprek", "Nog niet gepland"), ("", "Uitslag", "")],
                 f'<div class="nx soft">{ic("clock")}<span>FlexPlus reageert meestal binnen 5 werkdagen. Je hoort het hier en per mail.</span></div>')
    a3 = appcard("school", "Niet gekozen", "grey", [("done", "Verstuurd", "di 8 sep"), ("done", "Gezien door werkgever", "do 10 sep"), ("done", "Gesprek", "ma 14 sep"), ("stop", "Niet gekozen", "vr 18 sep")],
                 f'<div class="nx soft">{ic("heart")}<span>Jammer. Dit zegt niets over wie jij bent. Er zijn 3 banen die hierop lijken.</span><span class="sp"></span><span class="lnk">Bekijk ze{ic("right")}</span></div>')
    main = (f'<section><h1 class="pt">Mijn sollicitaties</h1>{jtabs("Sollicitaties")}<div class="cnts"><div class="cn on"><b>3</b><span>Alles</span></div><div class="cn"><b>2</b><span>Loopt nog</span></div><div class="cn"><b>1</b><span>Afgerond</span></div></div>{a1}{a2}{a3}</section>')
    rail = (f'<aside style="display:flex;flex-direction:column;gap:16px;position:sticky;top:84px"><div class="card rc"><div class="lob" style="align-items:flex-start"><img src="{MASCOT}" alt="">'
            f'<div><h3>Goed bezig, Samira</h3><p class="sm">Je gesprek bij Groenhof Zorg is maandag. Neem je ID mee en kom 10 minuten eerder.</p></div></div></div>'
            f'<div class="card rc"><h3>Wat betekenen de stappen?</h3><ul class="sm" style="display:flex;flex-direction:column;gap:8px">'
            f'<li><b>Verstuurd</b> · de werkgever heeft je sollicitatie.</li><li><b>Gezien</b> · de werkgever heeft hem geopend.</li><li><b>Gesprek</b> · je maakt kennis.</li><li><b>Uitslag</b> · je hoort of je de baan krijgt.</li></ul></div></aside>')
    return page("Mijn sollicitaties", f'<main class="sw2">{main}{rail}</main>', False, on="Sollicitaties", css=D4CSS)

# ------------------------------------------------------------------ desktop: bewaard
D5CSS = '''
.bw{max-width:1200px;margin:0 auto;padding:22px 24px 100px}
.pt{font-size:var(--text-2xl);font-weight:700;color:var(--brand);line-height:1.25}
.bt2{display:flex;align-items:center;gap:10px;margin:14px 0 16px}
.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}
.bc{overflow:hidden;display:flex;flex-direction:column}.bc .ph{height:140px;position:relative}.bc .ph img{width:100%;height:100%;object-fit:cover}
.bc .st{position:absolute;left:10px;top:10px}.bc .rm{position:absolute;right:10px;top:10px;width:36px;height:36px;border-radius:50%;background:var(--surface);display:grid;place-items:center;color:var(--brand);box-shadow:var(--shadow)}
.bc .rm svg{fill:currentColor}
.bc .bd{padding:12px 14px 14px;display:flex;flex-direction:column;gap:6px;flex:1}.bc h2{font-size:var(--text-md);font-weight:600;color:var(--brand)}
.bc .ac{display:flex;gap:8px;margin-top:auto;padding-top:8px;align-items:center}
.bc.off .ph img{filter:grayscale(.85) opacity(.7)}.bc.off h2{color:var(--muted)}
.fr{display:flex;gap:6px;flex-wrap:wrap}
'''
def bcard(k, st, stc, off=False, applied=False):
    v = V[k]
    act = (f'<span class="pill ok">{ic("check")}Je hebt gesolliciteerd</span><span class="sp"></span><span class="lnk">Bekijk{ic("right")}</span>' if applied else
           (f'<span class="lnk">{ic("search")}Zoek banen die hierop lijken</span><span class="sp"></span><span class="btn sm ghost">{ic("trash")}Weg</span>' if off else
            f'<span class="btn sm">{ic("send")}Solliciteer</span><span class="sp"></span><span class="lnk">Bekijk{ic("right")}</span>'))
    return (f'<article class="card bc{" off" if off else ""}"><div class="ph"><img src="{PH[v["ph"]]}" alt=""><span class="pill {stc} st">{st}</span><span class="rm" aria-label="Uit bewaard halen">{ic("heart")}</span></div>'
            f'<div class="bd"><h2>{v["t"]}</h2>{company(v)}<div class="meta"><span class="tt">{ic("bike")}{v["min"]} min</span><span>{ic("clock")}{v["h"]}</span><span>{ic("cal")}Bewaard {v.get("sv", "24 sep")}</span></div>'
            f'<div class="fr">{fitb(v["fit"])}</div><p class="why">{ic("spark", sw=2.2)}{v["why"]}</p><div class="ac">{act}</div></div></article>')
def d5():
    g = (bcard("zorg", "Sluit over 5 dagen", "warn", applied=True) + bcard("kas", "Open", "ok") + bcard("school", "Open", "ok") +
         bcard("winkel", "Open", "ok") + bcard("kok", "Baan is al vergeven", "grey", off=True) + bcard("schoon", "Gesloten", "grey", off=True))
    body = (f'<main class="bw"><h1 class="pt">Mijn sollicitaties</h1>{jtabs("Bewaard")}'
            f'<p class="muted">Banen die je wilt onthouden. Ook de banen die je in Match bewaarde.</p><div class="bt2"><span class="chip on">Alles · 6</span><span class="chip">Nog open · 4</span><span class="chip">Gesloten · 2</span>'
            f'<span class="sp"></span><span class="lnk">{ic("sort")}Past het best{ic("chevd")}</span></div><div class="grid">{g}</div></main>')
    return page("Bewaard", body, False, on="Sollicitaties", css=D5CSS)

# ------------------------------------------------------------------ desktop: Match dialog (variant B)
D6CSS = D1CSS + '''
.scrim{position:fixed;inset:0;background:rgba(10,32,68,.45);z-index:60}
.dlg{position:fixed;left:50%;top:50%;transform:translate(-50%,-50%);width:980px;z-index:61;display:grid;grid-template-columns:1fr 290px;overflow:hidden;border-radius:var(--radius-lg)}
.dlg .l{padding:20px 24px 20px;display:flex;flex-direction:column;gap:12px}.dlg .dh{display:flex;align-items:center;gap:10px}.dlg .dh h2{font-size:var(--text-xl);font-weight:600;color:var(--brand)}
.sc{border:1px solid var(--pearl);border-radius:var(--radius-lg);overflow:hidden;box-shadow:var(--shadow-lg);background:var(--surface);display:grid;grid-template-columns:250px 1fr}
.sc>img{width:250px;height:100%;object-fit:cover}.sc .b{padding:16px 18px;display:flex;flex-direction:column;gap:8px}.sc h3{font-size:var(--text-xl);font-weight:600;color:var(--brand);line-height:1.25}
.wy li{display:flex;gap:8px;font-size:14px;padding:2px 0}.wy li .i{color:var(--success);margin-top:3px}.wy li b{min-width:108px;color:var(--brand)}
.acts{display:flex;justify-content:center;gap:12px}.acts .btn{height:52px;padding:0 22px;font-size:15px}
.kh{font-size:12px;color:var(--muted);text-align:center}
.nxt{background:var(--bg);border-inline-start:1px solid var(--pearl);padding:20px 16px}.nxt h3{font-size:var(--text-md);font-weight:600;color:var(--brand);margin-bottom:10px}
.nxt li{display:flex;gap:10px;align-items:center;background:var(--surface);border-radius:var(--radius-sm);padding:8px;margin-bottom:8px;border:1px solid var(--pearl)}.nxt img{width:44px;height:44px;border-radius:6px;object-fit:cover}.nxt b{font-size:13px;color:var(--brand);display:block;line-height:1.3}.nxt span{font-size:12px;color:var(--muted)}
'''
WY = [("Cultuur", "Rustig team, je werkt veel samen"), ("Waarden", "Mensen helpen vind je belangrijk"), ("Competenties", "Je luistert goed en plant je dag"), ("Interesses", "Je zorgt graag voor anderen")]
def d6():
    z = V["zorg"]
    card = (f'<div class="sc"><img src="{PH["zorg"]}" alt=""><div class="b"><div class="fr" style="display:flex;gap:6px">{fitb(82)}<span class="pill gold">{ic("hand")}Leerwerkplek</span></div>'
            f'<h3>{z["t"]}</h3>{company(z)}<div class="meta"><span class="tt">{ic("bike")}8 min fietsen</span><span>{ic("clock")}{z["h"]}</span><span>{ic("euro")}{z["w"]}</span></div>'
            f'<p class="b6 sm" style="color:var(--brand);margin-top:4px">Waarom jij past</p><ul class="wy">' + "".join(f'<li>{ic("check", sw=2.4)}<b>{a}</b><span>{b}</span></li>' for a, b in WY) + '</ul>'
            f'<p class="sm muted">Kernwaarden van Groenhof Zorg: Samen · Eerlijk · Rust</p></div></div>')
    nxt = "".join(f'<li><img src="{PH[V[k]["ph"]]}" alt=""><div><b>{V[k]["t"]}</b><span>{V[k]["fit"]}% past · {V[k]["min"]} min</span></div></li>' for k in ["order", "kas", "school", "winkel"])
    dlg = (f'<div class="scrim"></div><div class="card dlg" role="dialog" aria-label="Jouw top-matches"><div class="l"><div class="dh"><img src="{MASCOT}" alt="" style="width:40px;height:40px"><div><h2>Jouw top-matches van vandaag</h2>'
           f'<p class="sm muted">1 van 8 · Kies wat je wilt. Je kunt niets fout doen.</p></div><span class="sp"></span><span class="btn ic">{ic("x")}</span></div>{card}'
           f'<div class="acts"><span class="btn">{ic("x")}Laten schieten</span><span class="btn">{ic("heart")}Bewaar</span><span class="btn pri">{ic("chat")}Snel kennismaken</span></div>'
           f'<p class="kh">Toetsen: ← laten schieten · ↓ bewaren · → kennismaken</p></div>'
           f'<div class="nxt"><h3>Hierna</h3><ul>{nxt}</ul><p class="xs muted">Laten schieten zet een baan lager op de kaart. Hij verdwijnt niet.</p></div></div>')
    return page("Match", d1_body() + dlg, False, css=D6CSS, fixed=True)

# ------------------------------------------------------------------ mobile
MCSS = '''
.m .bn{height:64px;padding:4px 2px}.m .bn a{font-size:12px}
.mbar{padding:8px 12px;background:var(--surface);display:flex;gap:8px}
.mbar .fld{flex:1;height:44px}
.mchips{display:flex;gap:8px;padding:0 12px 8px;overflow:hidden;background:var(--surface);border-bottom:1px solid var(--pearl)}.mchips .chip{height:36px;padding:0 12px}
.fr{display:flex;gap:6px;flex-wrap:wrap;align-items:center}.hrt{color:var(--muted)}
.tm{display:flex;gap:10px;align-items:center;background:var(--surface);border-radius:var(--radius);box-shadow:var(--shadow-lg);border:2px solid var(--gold-light)}
.tm small{font-size:12px;font-weight:600;color:var(--gold-ink);display:flex;gap:4px;align-items:center}.tm small .i{width:14px;height:14px}.tm b{display:block;font-size:14px;color:var(--brand);line-height:1.3}.tm .sub{font-size:12px;color:var(--muted)}
.tm img.ph{border-radius:var(--radius-sm);object-fit:cover}
'''
def mchips():
    return (f'<div class="mbar"><span class="fld">{ic("search")}Wat voor werk zoek je?</span><span class="btn ic">{ic("sliders")}</span></div>'
            f'<div class="mchips"><span class="chip tt">{ic("bike")}20 min{ic("chevd")}</span><span class="chip on">{ic("spark")}Past bij mij</span><span class="chip">{ic("clock")}Uren</span><span class="chip">Branche</span></div>')
M1CSS = MCSS + '''
.mm{position:absolute;left:0;right:0;top:164px;bottom:64px}
.sheet{position:absolute;left:0;right:0;bottom:64px;height:236px;background:var(--surface);border-radius:20px 20px 0 0;box-shadow:0 -8px 24px rgba(15,45,92,.16);z-index:8;padding:6px 12px 0;overflow:hidden}
.grab{width:40px;height:5px;border-radius:3px;background:var(--border);margin:4px auto 8px}
.sheet h2{font-size:var(--text-md);font-weight:600;color:var(--brand)}
.mt{position:absolute;left:12px;right:12px;top:10px;z-index:5}.mt .tm{padding:6px 10px 6px 6px}.mt .tm img.ph{width:44px;height:44px}
.jr{display:grid;grid-template-columns:76px 1fr;gap:10px;border:1px solid var(--pearl);border-radius:var(--radius);padding:8px;margin-top:8px;background:var(--surface)}
.jr img{width:76px;height:100%;min-height:92px;object-fit:cover;border-radius:var(--radius-sm)}
.jr .jb{display:flex;flex-direction:column;gap:3px;min-width:0}.jt{display:flex;justify-content:space-between;gap:6px}.jt b{font-size:15px;color:var(--brand);line-height:1.3}
.jr.sel{border-color:var(--brand);box-shadow:0 0 0 1px var(--brand)}.jr .why{display:none}
.lf{position:absolute;right:12px;bottom:312px;z-index:7}
'''
def m1():
    body = (mchips() + f'<section class="mapw mm">{mapview("m1", 390, 616, dy=-40, fp=False)}'
            f'<div class="mt"><div class="tm"><img class="ph" src="{PH["zorg"]}" alt=""><div style="flex:1"><small>{ic("spark", sw=2.2)}Jouw top-match</small><b>Zorgmedewerker thuiszorg</b><span class="sub">82% past bij jou · 8 min</span></div>{ic("right")}</div></div></section>'
            f'<div class="mctl lf"><a>{ic("target")}</a></div>'
            f'<section class="sheet"><div class="grab"></div><div style="display:flex;align-items:center"><div><h2>34 banen binnen 20 min</h2><p class="xs muted">Beste match bovenaan · veeg omhoog voor meer</p></div><span class="sp"></span>'
            f'<span class="seg" style="height:36px"><a class="on">{ic("map")}</a><a>{ic("list")}</a></span></div>'
            f'<ul>{jobrow("zorg", True)}{jobrow("order")}</ul></section>')
    return page("Banenkaart", body, True, css=M1CSS, fixed=True)
M2CSS = MCSS + '''
.ml{padding:10px 12px 140px;display:flex;flex-direction:column;gap:10px}
.ml h1{font-size:var(--text-lg);font-weight:600;color:var(--brand);line-height:1.3}
.mc{overflow:hidden}.mc .ph{height:120px;position:relative}.mc .ph img{width:100%;height:100%;object-fit:cover}
.mc .ph .tt2{position:absolute;left:10px;bottom:10px;background:var(--surface);color:var(--ring);font-weight:600;font-size:13px;border-radius:var(--radius-pill);padding:2px 10px;display:flex;gap:5px;align-items:center}
.mc .ph .h{position:absolute;right:10px;top:10px;width:36px;height:36px;border-radius:50%;background:var(--surface);display:grid;place-items:center;color:var(--brand)}
.mc .bd{padding:10px 12px 12px;display:flex;flex-direction:column;gap:5px}.mc h2{font-size:var(--text-md);font-weight:600;color:var(--brand);line-height:1.3}
.kaart{position:fixed;left:50%;bottom:80px;transform:translateX(-50%);z-index:30;box-shadow:var(--shadow-lg)}
'''
def mcard(k):
    v = V[k]
    return (f'<article class="card mc"><div class="ph"><img src="{PH[v["ph"]]}" alt=""><span class="tt2">{ic("bike")}{v["min"]} min</span><span class="h">{ic("heart")}</span></div>'
            f'<div class="bd"><h2>{v["t"]}</h2>{company(v)}<div class="fr">{fitb(v["fit"])}{badge2(v)}</div><p class="why">{ic("spark", sw=2.2)}{v["why"]}</p>'
            f'<div class="meta"><span>{ic("clock")}{v["h"]}</span><span>{ic("euro")}{v["w"].split(" –")[0]}</span></div></div></article>')
def m2():
    body = (mchips() + f'<main class="ml"><div style="display:flex;align-items:center"><h1>34 banen · 20 min fietsen</h1><span class="sp"></span><span class="lnk">{ic("sort")}Past het best</span></div>'
            f'{mcard("zorg")}{mcard("order")}{mcard("nacht")}</main><span class="btn pri kaart">{ic("map")}Kaart</span>')
    return page("Banen in een lijst", body, True, css=M2CSS)
M3CSS = MCSS + D3CSS + '''
.mh{height:210px;position:relative}.mh img{width:100%;height:100%;object-fit:cover}.mh .b{position:absolute;top:10px;width:40px;height:40px;border-radius:50%;background:var(--surface);display:grid;place-items:center;color:var(--brand);box-shadow:var(--shadow)}
.mw{padding:14px 16px 150px;display:flex;flex-direction:column;gap:12px}.mw .h1{font-size:var(--text-xl);margin-top:0}
.facts{grid-template-columns:1fr 1fr;margin:0}
.hid{background:var(--accent-soft);border-radius:var(--radius-sm);padding:8px 12px;font-size:13px;display:flex;gap:8px}.hid .i{color:var(--brand);margin-top:2px}
.abar{position:fixed;left:0;right:0;bottom:64px;background:var(--surface);border-top:1px solid var(--pearl);padding:10px 12px;display:flex;gap:8px;z-index:45;box-shadow:0 -4px 12px rgba(15,45,92,.08)}
.bt{grid-template-columns:1fr}.tc .mapw{height:150px}
'''
def m3():
    o = V["order"]
    fc = (f'<div class="card fc" style="padding:14px"><div class="hd2">{ringc(76)}<div><b style="font-size:16px;color:var(--brand)">Past goed bij jou</b><p class="xs muted">Vergeleken met je paspoort</p></div></div>{dims(DO)}</div>')
    body = (f'<div class="mh"><img src="{PH["logistiek"]}" alt=""><span class="b" style="left:12px">{ic("left")}</span><span class="b" style="right:60px">{ic("share")}</span><span class="b" style="right:12px">{ic("heart")}</span></div>'
            f'<main class="mw"><h1 class="h1">{o["t"]}</h1>{company(o)}'
            f'<div class="hid">{ic("info")}<span>Bij welk bedrijf je gaat werken, hoor je van FlexPlus na je sollicitatie. De kaart toont het kantoor van FlexPlus in Naaldwijk.</span></div>'
            f'<div class="facts">' + "".join(f'<div class="fact">{ic(i)}<div><small>{a}</small><b>{b}</b></div></div>' for i, a, b in [("clock", "Uren", o["h"]), ("euro", "Per uur", o["w"]), ("sun", "Wanneer", o["day"]), ("bike", "Naar kantoor", "14 min")]) + '</div>'
            f'<div class="card tc"><b style="color:var(--brand)">Hoe kom je er?</b><div class="mapw">{mapview("mdet", 326, 150, dy=0, clusters=False, sel=False, labels=False, extra=((200, 562), "Kantoor FlexPlus"))}</div>'
            f'<p class="xs muted">14 min fietsen naar het kantoor. De echte werkplek kan anders zijn.</p></div>'
            f'{fc}<div class="card emp" style="padding:14px"><h2 style="font-size:var(--text-md);font-weight:600;color:var(--brand);display:flex;gap:8px;align-items:center">{ic("building")}Over FlexPlus</h2>'
            f'<span class="by" style="margin:4px 0 8px">{ic("info")}door werkgever opgegeven</span><p class="sm muted">Kernwaarden</p><div class="kv"><span>Eerlijk</span><span>Veilig</span><span>Samen</span></div>'
            f'<div class="bt"><div>{ic("lang")}<span><b>Taal leren op het werk</b> · les onder werktijd, gewoon betaald.</span></div></div></div></main>'
            f'<div class="abar"><span class="btn pri" style="flex:1;height:48px;font-size:16px">{ic("send")}Solliciteer</span><span class="btn ic" style="height:48px;width:48px">{ic("heart")}</span></div>')
    return page("Vacature", body, True, css=M3CSS)
M4CSS = MCSS + D4CSS + '''
.mw4{padding:14px 12px 90px}.m .pt{font-size:var(--text-xl)}
.cn{min-width:0;flex:1;padding:8px 10px}.cn b{font-size:18px}
.ap{padding:12px}.ap .t{grid-template-columns:52px 1fr auto;gap:10px}.ap img{width:52px;height:52px}
.vt{margin:12px 0 0 4px}.vt li{position:relative;padding:0 0 14px 34px;font-size:13px;color:var(--muted)}
.vt li:before{content:"";position:absolute;left:11px;top:22px;bottom:-2px;width:3px;background:var(--pearl)}.vt li:last-child:before{display:none}
.vt li.done:before{background:var(--success)}
.vt li i{position:absolute;left:0;top:0;width:24px;height:24px;border-radius:50%;background:var(--surface);border:2.5px solid var(--pearl);display:grid;place-items:center}
.vt li.done i{background:var(--success);border-color:var(--success);color:#fff}.vt li.now i{border-color:var(--brand);box-shadow:0 0 0 4px var(--accent-soft)}.vt li.now i:after{content:"";width:8px;height:8px;border-radius:50%;background:var(--brand)}
.vt li i .i{width:13px;height:13px;stroke-width:3}.vt li b{display:block;color:var(--text);font-size:14px}.vt li.now b{color:var(--brand)}
.mini{display:flex;gap:4px;margin-top:10px}.mini i{flex:1;height:5px;border-radius:3px;background:var(--pearl)}.mini i.d{background:var(--success)}.mini i.n{background:var(--brand)}.mini i.x{background:var(--muted)}
.ms{font-size:13px;color:var(--muted);margin-top:6px}
'''
def m4():
    v = V["zorg"]
    a1 = (f'<article class="card ap"><div class="t"><img src="{PH["zorg"]}" alt=""><div><h2>{v["t"]}</h2>{company(v)}</div><span class="pill ok">Gesprek</span></div>'
          f'<ol class="vt"><li class="done"><i>{ic("check")}</i><b>Verstuurd</b>ma 21 sep</li><li class="done"><i>{ic("check")}</i><b>Gezien door werkgever</b>wo 23 sep</li>'
          f'<li class="now"><i></i><b>Gesprek</b>ma 5 okt · 10:00 · Groenhof, Wateringen</li><li><i></i><b>Uitslag</b>Na het gesprek</li></ol>'
          f'<div class="nx">{ic("chat")}<span><b>Wat nu?</b> Oefen je gesprek met Lobsy.</span></div></article>')
    def small(k, st, pc, seg, line):
        w = V[k]; bars = "".join(f'<i class="{c}"></i>' for c in seg)
        return (f'<article class="card ap"><div class="t"><img src="{PH[w["ph"]]}" alt=""><div><h2>{w["t"]}</h2>{company(w)}</div><span class="pill {pc}">{st}</span></div>'
                f'<div class="mini">{bars}</div><p class="ms">{line}</p></article>')
    body = (f'<main class="mw4"><h1 class="pt">Mijn sollicitaties</h1>{jtabs("Sollicitaties")}<div class="cnts"><div class="cn on"><b>3</b><span>Alles</span></div><div class="cn"><b>2</b><span>Loopt nog</span></div><div class="cn"><b>1</b><span>Afgerond</span></div></div>'
            f'{a1}{small("order", "Gezien", "", ["d", "n", "", ""], "Gezien op vr 25 sep · tik voor alle stappen")}'
            f'{small("school", "Niet gekozen", "grey", ["d", "d", "d", "x"], "Afgerond op vr 18 sep")}</main>')
    return page("Mijn sollicitaties", body, True, on="Sollicitaties", css=M4CSS)
M5CSS = MCSS + '''
.mw5{padding:10px 12px 90px;display:flex;flex-direction:column;gap:10px}
.mh5{display:flex;gap:10px;align-items:center}.mh5 h1{font-size:var(--text-lg);font-weight:600;color:var(--brand);line-height:1.3}
.prog{display:flex;gap:4px}.prog i{flex:1;height:4px;border-radius:2px;background:var(--pearl)}.prog i.d{background:var(--brand)}
.stack{position:relative;margin-top:8px}.stack .bk{position:absolute;left:14px;right:14px;top:-8px;height:40px;border-radius:var(--radius-lg);background:var(--surface);border:1px solid var(--pearl)}
.swc{position:relative;overflow:hidden;border-radius:var(--radius-lg)}.swc .ph{height:190px;position:relative}.swc .ph img{width:100%;height:100%;object-fit:cover}
.swc .ph .fitx{position:absolute;left:12px;bottom:12px}.swc .bd{padding:12px 14px 14px;display:flex;flex-direction:column;gap:6px}.swc h2{font-size:var(--text-xl);font-weight:600;color:var(--brand);line-height:1.25}
.wy li{display:flex;gap:8px;font-size:14px;padding:1px 0}.wy li .i{color:var(--success);margin-top:3px}.wy li b{min-width:104px;color:var(--brand)}
.acts{display:grid;grid-template-columns:1.25fr .9fr 1.25fr;gap:6px}.acts .btn{height:52px;padding:0 6px;gap:5px;min-width:0}
'''
def m5():
    z = V["zorg"]
    wy = [("Cultuur", "Rustig team"), ("Waarden", "Mensen helpen"), ("Competenties", "Goed luisteren"), ("Interesses", "Zorgen voor anderen")]
    body = (f'<main class="mw5"><div class="mh5"><img src="{MASCOT}" alt="" style="width:36px;height:36px"><div><h1>Jouw top-matches</h1><p class="xs muted">1 van 8 · je kunt niets fout doen</p></div></div>'
            f'<div class="prog"><i class="d"></i>' + '<i></i>' * 7 + '</div>'
            f'<div class="stack"><div class="bk"></div><article class="card swc"><div class="ph"><img src="{PH["zorg"]}" alt=""><span class="fitx">{fitb(82)}</span></div><div class="bd">'
            f'<h2>{z["t"]}</h2>{company(z)}<div class="meta"><span class="tt">{ic("bike")}8 min</span><span>{ic("clock")}{z["h"]}</span><span>{ic("euro")}€ 15,20</span></div>'
            f'<p class="b6 sm" style="color:var(--brand)">Waarom jij past</p><ul class="wy">' + "".join(f'<li>{ic("check", sw=2.4)}<b>{a}</b><span>{b}</span></li>' for a, b in wy) + '</ul>'
            f'<span class="pill gold" style="align-self:flex-start">{ic("hand")}Leerwerkplek</span></div></article></div>'
            f'<div class="acts"><span class="btn">{ic("x")}Laten schieten</span><span class="btn">{ic("heart")}Bewaar</span><span class="btn pri">{ic("chat")}Kennismaken</span></div>'
            f'<p class="xs muted" style="text-align:center">Veeg naar links of rechts. Laten schieten zet de baan lager. Hij verdwijnt niet.</p></main>')
    return page("Match", body, True, css=M5CSS)

SCREENS = [("kd-d1-banenkaart", d1, "d"), ("kd-d2-lijst", d2, "d"), ("kd-d3-vacature", d3, "d"), ("kd-d4-sollicitaties", d4, "d"),
           ("kd-d5-bewaard", d5, "d"), ("kd-d6-match", d6, "d"),
           ("kd-m1-kaart", m1, "m"), ("kd-m2-lijst", m2, "m"), ("kd-m3-vacature", m3, "m"), ("kd-m4-sollicitaties", m4, "m"), ("kd-m5-match", m5, "m")]
