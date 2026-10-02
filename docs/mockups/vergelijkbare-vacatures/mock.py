"""Mockups 'Meer rotsen zoals deze' (vergelijkbare vacatures).
Loads the real acceptatie vacancy detail page (public, read-only), dismisses cookies and injects
the proposed button + sheet/drawer using the live design tokens. All result data = Voorbeelddata.
Run: python3 mock.py  (playwright + /usr/bin/google-chrome)"""
import asyncio, json, pathlib
ICONS = json.load(open(pathlib.Path(__file__).parent / "navicons.json"))
from playwright.async_api import async_playwright

BASE = "https://acceptatie.lobsy.nl"
URL = BASE + "/vacancies/a1000000-0000-4000-8000-000000000049"
OUT = pathlib.Path(__file__).parent
OUT.mkdir(exist_ok=True)

BIKE = '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="5.5" cy="17" r="3.5"/><circle cx="18.5" cy="17" r="3.5"/><path d="M5.5 17 9 9h6l3.5 8M9 9l3 8 3-8M14 6h3"/></svg>'
CHEV = '<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m9 6 6 6-6 6"/></svg>'
STONES = '<svg viewBox="0 0 48 48" width="44" height="44" aria-hidden="true"><ellipse cx="13" cy="37" rx="10" ry="6" fill="#5a6a7d"/><ellipse cx="25" cy="31" rx="11" ry="6.5" fill="#0f2d5c"/><ellipse cx="37" cy="24" rx="9" ry="5.5" fill="#c9a227"/><path d="M13 33c4-6 8-9 12-8M25 26c4-5 8-7 12-6" fill="none" stroke="#c9a227" stroke-width="1.6" stroke-dasharray="2 3" stroke-linecap="round"/></svg>'

RESULTS_ME = [
    ("Allround orderpicker", "Westland Fresh Logistics", "Honselersdijk", 9, "logistiek", "🤝", "Past bij je waarde: Een hecht team"),
    ("Magazijnmedewerker", "Supermarkt Poeldijk", "Poeldijk", 6, "winkel", "🏠", "Dichter bij huis"),
    ("Hovenier-hulp", "Tuin & Groen Delft", "Delft", 18, "tuinbouw", "🌤️", "Ook buiten werken"),
    ("Medewerker kas tomaten", "Tomatenpark De Lier", "De Lier", 14, "productie", "💪", "Past bij je sterkte: Samenwerken"),
    ("Planner logistiek", "Westland Fresh Logistics", "Honselersdijk", 9, "kantoor", "🕒", "Ook flexibele tijden"),
    ("Schoonmaker distributiecentrum", "Schoon Westland", "Naaldwijk", 12, "schoonmaak", "🌱", "Geen ervaring nodig"),
]
RESULTS_ANON = [
    ("Allround orderpicker", "Westland Fresh Logistics", "Honselersdijk", None, "logistiek", "📦", "Zelfde soort werk"),
    ("Heftruckchauffeur", "Tomatenpark De Lier", "De Lier", None, "bouw", "📦", "Zelfde branche: Logistiek"),
    ("Hovenier-hulp", "Tuin & Groen Delft", "Delft", None, "tuinbouw", "🌤️", "Ook buiten werken"),
    ("Magazijnmedewerker", "Supermarkt Poeldijk", "Poeldijk", None, "winkel", "🕒", "Ook flexibele tijden"),
    ("Medewerker kas tomaten", "Tomatenpark De Lier", "De Lier", None, "productie", "🌱", "Geen ervaring nodig"),
    ("Schoonmaker distributiecentrum", "Schoon Westland", "Naaldwijk", None, "schoonmaak", "📦", "Zelfde soort werk"),
]
KM = {"Honselersdijk": 1.2, "Poeldijk": 3.4, "Delft": 11.8, "De Lier": 6.1, "Naaldwijk": 4.0}

CSS = """
.similar-entry{display:flex;align-items:center;gap:.75rem;width:100%;min-height:64px;
  padding:.65rem .85rem;border:1px solid var(--border);border-radius:var(--radius);background:var(--surface);
  color:var(--brand);text-align:start;font:inherit;cursor:pointer;box-shadow:0 1px 0 color-mix(in srgb,var(--brand) 6%,transparent)}
.similar-entry:hover{background:var(--accent-soft)}
.similar-entry__icon{flex:0 0 44px;height:44px;display:grid;place-items:center;border-radius:10px;background:var(--accent-soft)}
.similar-entry__text{flex:1 1 auto;display:flex;flex-direction:column;gap:.1rem;min-width:0}
.similar-entry__title{font-weight:600;font-size:1rem;line-height:1.25}
.similar-entry__sub{font-size:.85rem;color:var(--muted);line-height:1.3}
.similar-entry__chev{flex:0 0 auto;color:var(--brand)}
.similar-backdrop{position:fixed;inset:0;z-index:180;background:rgba(15,23,42,.45)}
.similar-sheet{position:fixed;inset-inline:0;bottom:0;z-index:190;max-height:min(88dvh,88vh);display:flex;flex-direction:column;
  overflow:hidden;background:var(--surface);border-radius:14px 14px 0 0;box-shadow:var(--shadow-lg);
  padding-bottom:max(.35rem,env(safe-area-inset-bottom));color:var(--text)}
.similar-sheet__grab{width:40px;height:4px;border-radius:2px;background:var(--border);margin:.5rem auto 0}
.similar-sheet__header{display:flex;align-items:flex-start;gap:.65rem;padding:.55rem .9rem .4rem}
.similar-sheet__mascot{width:2.35rem;height:2.35rem;object-fit:contain;flex:0 0 auto;filter:drop-shadow(0 4px 8px color-mix(in srgb,var(--brand) 20%,transparent))}
.similar-sheet__titles{flex:1 1 auto;min-width:0}
.similar-sheet__titles h2{margin:0;font-size:1.1rem;font-weight:600;color:var(--brand);line-height:1.25}
.similar-sheet__titles p{margin:.1rem 0 0;font-size:.85rem;color:var(--muted)}
.similar-sheet__close{flex:0 0 44px;width:44px;height:44px;border-radius:999px;border:1px solid var(--border);background:var(--surface);
  font-size:1.35rem;line-height:1;color:var(--brand);cursor:pointer}
.similar-sheet__why{margin:0 .9rem .5rem;padding:.45rem .65rem;border-radius:10px;background:var(--pearl);font-size:.8rem;color:var(--muted)}
.similar-sheet__body{overflow-y:auto;padding:0 .9rem .9rem;display:flex;flex-direction:column;gap:.55rem}
.similar-list{list-style:none;margin:0;padding:0;display:flex;flex-direction:column;gap:.55rem}
.similar-row{display:flex;gap:.7rem;align-items:stretch;padding:.55rem;border:1px solid var(--border);border-radius:var(--radius);background:var(--surface);
  text-decoration:none;color:inherit;min-height:44px}
.similar-row__photo{flex:0 0 76px;width:76px;height:76px;border-radius:10px;object-fit:cover;background:var(--pearl)}
.similar-row__body{flex:1 1 auto;min-width:0;display:flex;flex-direction:column;gap:.15rem}
.similar-row__title{font-weight:600;font-size:.98rem;color:var(--brand);line-height:1.25;overflow:hidden;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical}
.similar-row__meta{font-size:.82rem;color:var(--muted);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.similar-row__foot{display:flex;flex-wrap:wrap;align-items:center;gap:.4rem;margin-top:auto}
.similar-row__travel{display:inline-flex;align-items:center;gap:.25rem;font-size:.82rem;font-weight:600;color:var(--text)}
.similar-chip{display:inline-flex;align-items:center;gap:.25rem;max-width:100%;padding:.15rem .55rem;border-radius:999px;font-size:.75rem;font-weight:600;line-height:1.3;
  border:1px solid color-mix(in srgb,var(--brand) 18%,var(--border));background:var(--accent-soft);color:var(--brand);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.similar-cta{display:flex;gap:.65rem;align-items:center;padding:.65rem .75rem;border-radius:var(--radius);border:1px dashed color-mix(in srgb,var(--brand) 30%,var(--border));background:var(--pearl)}
.similar-cta img{width:40px;height:40px;flex:0 0 auto}
.similar-cta__text{flex:1 1 auto;min-width:0;font-size:.85rem;line-height:1.3}
.similar-cta__text b{display:block;color:var(--brand);font-size:.92rem}
.similar-cta a{flex:0 0 auto;display:inline-flex;align-items:center;min-height:44px;padding:0 .85rem;border-radius:10px;background:var(--brand);color:#fff;font-weight:600;font-size:.85rem;text-decoration:none}
.similar-sheet__more{align-self:center;display:inline-flex;align-items:center;min-height:44px;color:var(--brand);font-weight:600;font-size:.9rem}
.similar-sample{align-self:flex-start;margin:0 .9rem .4rem;font-size:.7rem;color:var(--muted);border:1px dashed var(--border);border-radius:999px;padding:.05rem .45rem;background:var(--surface)}
@media (min-width:900px){
  .similar-sheet{inset-inline:auto 0;inset-block:0;bottom:0;width:460px;max-height:none;border-radius:16px 0 0 16px}
  .similar-sheet__grab{display:none}
  .similar-sheet__header{padding:1rem 1.1rem .5rem}
  .similar-sheet__why{margin:0 1.1rem .65rem}
  .similar-sample{margin-inline:1.1rem}
  .similar-sheet__body{padding:0 1.1rem 1.1rem}
  .similar-row__photo{flex-basis:88px;width:88px;height:88px}
}
"""

def rows_html(results, me=True):
    out = []
    for (t, comp, place, mins, img, emo, chip) in results:
        travel = f'<span class="similar-row__travel">{BIKE}{mins} min</span>' if mins else f'<span class="similar-row__travel">{KM[place]:.1f} km</span>'.replace('.', ',', 1)
        out.append(f'''<li><a class="similar-row" href="#"><img class="similar-row__photo" src="{BASE}/images/vacancies/{img}-400.webp" alt="">
<span class="similar-row__body"><span class="similar-row__title">{t}</span><span class="similar-row__meta">{comp} · {place}</span>
<span class="similar-row__foot">{travel}<span class="similar-chip"><span aria-hidden="true">{emo}</span>{chip}</span></span></span></a></li>''')
    return "\n".join(out)

RESULTS_FALLBACK = [
    ("Allround orderpicker", "Westland Fresh Logistics", "Honselersdijk", 9, "logistiek", "📦", "Zelfde soort werk"),
    ("Magazijnmedewerker", "Supermarkt Poeldijk", "Poeldijk", 6, "winkel", "🏠", "Dichter bij huis"),
    ("Heftruckchauffeur", "Tomatenpark De Lier", "De Lier", 14, "bouw", "📦", "Zelfde soort werk"),
    ("Planner logistiek", "Westland Fresh Logistics", "Honselersdijk", 9, "kantoor", "📦", "Zelfde soort werk"),
]

def sheet_html(me=True, fallback=False):
    sub = "Lijkt hierop en past bij jou" if me else "Lijkt op deze baan"
    why = ("Gekozen op het werk, je waarden en je reistijd. Alleen echte bedrijven."
           if me else "Gekozen op het soort werk. Alleen echte bedrijven.")
    if fallback:
        why = "Zelfde soort werk in de buurt. Alleen echte bedrijven."
    cta = "" if me else f'''<div class="similar-cta"><img src="{BASE}/images/brand/lobsy-128.webp" alt="">
<span class="similar-cta__text"><b>Wil je banen die bij jóu passen?</b>Doe de ontdekkingsreis. Gratis.</span><a href="#">Start</a></div>'''
    return f'''<div class="similar-backdrop"></div>
<div class="similar-sheet" role="dialog" aria-modal="true" aria-labelledby="similar-title">
<div class="similar-sheet__grab" aria-hidden="true"></div>
<div class="similar-sheet__header"><img class="similar-sheet__mascot" src="{BASE}/images/brand/lobsy-128.webp" alt="">
<div class="similar-sheet__titles"><h2 id="similar-title">Meer rotsen zoals deze</h2><p>{sub}</p></div>
<button class="similar-sheet__close" aria-label="Sluiten">×</button></div>
<p class="similar-sheet__why">{why}</p><span class="similar-sample">Voorbeelddata</span>
<div class="similar-sheet__body">{cta}<ul class="similar-list">{rows_html(RESULTS_FALLBACK if fallback else (RESULTS_ME if me else RESULTS_ANON), me)}</ul>
<a class="similar-sheet__more" href="#">Bekijk meer op de banenkaart</a></div></div>'''

ENTRY = f'''<button type="button" class="similar-entry" aria-haspopup="dialog" data-testid="similar-open">
<span class="similar-entry__icon">{STONES}</span><span class="similar-entry__text"><span class="similar-entry__title">Meer rotsen zoals deze</span>
<span class="similar-entry__sub">Lijkt hierop en past bij jou</span></span><span class="similar-entry__chev">{CHEV}</span></button>'''

PREP = """async ([css, entry]) => {
  for (const b of document.querySelectorAll('button')) { if (/Alleen noodzakelijk/.test(b.textContent)) { b.click(); } }
  await new Promise(r => setTimeout(r, 600));
  const st = document.createElement('style'); st.textContent = css; document.head.appendChild(st);
  const actions = document.querySelector('.kb-detail__rail-actions');
  const holder = document.createElement('div'); holder.innerHTML = entry;
  actions.insertAdjacentElement('afterend', holder.firstElementChild);
  if (document.activeElement) document.activeElement.blur();
  document.querySelectorAll('.reconnect-toast, #components-reconnect-modal').forEach(e => e.remove());
}"""

CHECK = """() => {
  const vw = document.documentElement.clientWidth; const bad = [];
  for (const e of document.querySelectorAll('.similar-entry, .similar-entry *, .similar-sheet, .similar-sheet *')) {
    const r = e.getBoundingClientRect(); if (r.width && (r.right > vw + 0.5 || r.left < -0.5)) bad.push((e.className && e.className.baseVal === undefined ? e.className : e.tagName) + ' ' + Math.round(r.left) + '..' + Math.round(r.right));
  }
  const small = [...document.querySelectorAll('.similar-sheet button, .similar-sheet a, .similar-entry')].filter(e => { const r = e.getBoundingClientRect(); return r.width && r.height < 44; }).map(e => e.className + ' h=' + Math.round(e.getBoundingClientRect().height));
  return { vw, scrollW: document.documentElement.scrollWidth, bad, small };
}"""

SHELL_ME = """([bell, user, nav]) => {
  const login = document.querySelector('.auth-link--login');
  const help = document.querySelector('.page-help__btn');
  if (login && help) {
    const mk = (svg, label) => { const b = help.cloneNode(false); b.innerHTML = svg; const cs=getComputedStyle(help); b.style.display=cs.display; b.style.alignItems='center'; b.style.justifyContent='center'; const sv=b.querySelector('svg'); if (sv) { sv.setAttribute('width','20'); sv.setAttribute('height','20'); sv.style.width='20px'; sv.style.height='20px'; } b.setAttribute('aria-label', label); b.removeAttribute('title'); return b; };
    login.replaceWith(mk(user, 'Account'));
    help.insertAdjacentElement('afterend', mk(bell, 'Meldingen'));
  }
  const shell = document.querySelector('.app-shell');
  if (shell && innerWidth < 900 && !document.querySelector('.bottom-nav')) {
    shell.classList.add('has-bottom-nav');
    const d = document.createElement('div'); d.innerHTML = nav; shell.appendChild(d.firstElementChild);
  }
  document.querySelectorAll('.site-footer, footer').forEach(f => { if (innerWidth < 900) f.style.display = 'none'; });
}"""

def nav_html():
    items = [("Zoeken", "Search", True), ("Bewaard", "Liked", False), ("Sollicitaties", "Applications", False), ("Carrière", "Career", False), ("Profiel", "Profile", False)]
    out = ['<nav class="bottom-nav" aria-label="Hoofdmenu">']
    for (t, ic, act) in items:
        out.append(f'<a class="bottom-nav__item{" is-active" if act else ""}" href="#"{" aria-current=page" if act else ""}><span class="bottom-nav__icon" aria-hidden="true">{ICONS[ic]}</span><span class="bottom-nav__label bottom-nav__label--full">{t}</span><span class="bottom-nav__label bottom-nav__label--short">{t}</span></a>')
    out.append('</nav>')
    return "".join(out)

async def shoot(b, name, w, h, mobile, sheet=None, scroll_to_entry=False, me=True, fallback=False):
    c = await b.new_context(viewport={"width": w, "height": h}, device_scale_factor=2 if mobile else 1,
                            is_mobile=mobile, has_touch=mobile, locale="nl-NL")
    pg = await c.new_page()
    await pg.goto(URL, wait_until="networkidle", timeout=90000)
    await pg.wait_for_timeout(1500)
    await pg.evaluate(PREP, [CSS, ENTRY])
    if me:
        await pg.evaluate(SHELL_ME, [ICONS["Notifications"], ICONS["User"], nav_html()])
    if scroll_to_entry:
        await pg.evaluate("""() => { const sc = document.querySelector('.panel-page--vacancy'); const e = document.querySelector('.similar-entry');
          const r = e.getBoundingClientRect(); const sr = sc.getBoundingClientRect(); sc.scrollTop += (r.top - sr.top) - 300; }""")
    if sheet is not None:
        await pg.evaluate("(html) => { const d = document.createElement('div'); d.innerHTML = html; document.body.appendChild(d); }", sheet_html(sheet, fallback))
    await pg.wait_for_timeout(1200)
    chk = await pg.evaluate(CHECK)
    await pg.screenshot(path=str(OUT / f"{name}.png"))
    dbg = await pg.evaluate("() => { const sc=document.querySelector('.panel-page--vacancy'); const e=document.querySelector('.similar-entry'); return {top: Math.round(e.getBoundingClientRect().top), st: sc.scrollTop, sh: sc.scrollHeight, doc: document.scrollingElement.scrollTop, dsh: document.scrollingElement.scrollHeight}; }")
    print(name, json.dumps(chk), dbg)
    await c.close()

TOKENS = """:root{--brand:#0f2d5c;--accent:#0f2d5c;--accent-soft:#e7eef7;--surface:#fffcfa;--bg:#f5f2ee;--text:#122033;
--muted:#5a6a7d;--border:#ddd5cc;--pearl:#f7f4f0;--gold:#c9a227;--shadow-lg:0 12px 32px #0f2d5c1f;--radius:12px}
body{margin:0;font-family:"Segoe UI","Helvetica Neue",Arial,sans-serif;background:var(--bg);color:var(--text)}
.demo{max-width:420px;margin:1rem auto;padding:0 1rem}"""

def standalone(name, me, fallback=False):
    """Clean component HTML (no live-page scripts): entry button + open sheet. Mockup only."""
    html = f"""<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Meer rotsen zoals deze · mockup</title><style>{TOKENS}{CSS}</style></head>
<body><!-- Mockup: inline CSS + raw hex for speed. Rebuild with design tokens/classes (see 03-ui-knop-sheet.md). Voorbeelddata. -->
<div class="demo">{ENTRY.replace("Lijkt hierop en past bij jou", "Lijkt hierop en past bij jou" if me else "Lijkt op deze baan")}</div>
{sheet_html(me, fallback)}</body></html>"""
    (OUT / f"{name}.html").write_text(html)

async def main():
    standalone("vv-sheet-paspoort", True)
    standalone("vv-sheet-uitgelogd", False)
    standalone("vv-sheet-terugval", True, True)
    async with async_playwright() as p:
        b = await p.chromium.launch(executable_path="/usr/bin/google-chrome")
        await shoot(b, "vv-mobiel-1-knop", 390, 844, True, scroll_to_entry=True)
        await shoot(b, "vv-mobiel-2-sheet-paspoort", 390, 844, True, sheet=True, scroll_to_entry=True)
        await shoot(b, "vv-mobiel-3-sheet-uitgelogd", 390, 844, True, sheet=False, scroll_to_entry=True, me=False)
        await shoot(b, "vv-mobiel-4-sheet-terugval", 390, 844, True, sheet=True, scroll_to_entry=True, fallback=True)
        await shoot(b, "vv-desktop-1-knop", 1440, 900, False)
        await shoot(b, "vv-desktop-2-paneel", 1440, 900, False, sheet=True)
        await b.close()

asyncio.run(main())
