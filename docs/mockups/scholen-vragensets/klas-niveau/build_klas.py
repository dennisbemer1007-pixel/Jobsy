"""Mockups: class create/edit with level picker (Lobsy voor scholen). Sample data only."""
import asyncio, pathlib
from playwright.async_api import async_playwright
OUT = pathlib.Path(__file__).parent
FONT = "/workspace/scholen/build"
CSS = f"""
@font-face{{font-family:Inter;src:url(file://{FONT}/Inter-400.ttf);font-weight:400}}
@font-face{{font-family:Inter;src:url(file://{FONT}/Inter-600.ttf);font-weight:600}}
@font-face{{font-family:Inter;src:url(file://{FONT}/Inter-700.ttf);font-weight:700}}
:root{{--bg:#f5f2ee;--surface:#fffcfa;--text:#122033;--muted:#5a6a7d;--border:#ddd5cc;--brand:#0f2d5c;--brand-deep:#0a2044;
--accent-soft:#e7eef7;--success:#15803d;--success-soft:#ecfdf3;--warn:#a65b00;--warn-soft:#fff8e7;--coral:#f54a1b;--radius:12px;--radius-sm:8px}}
*{{box-sizing:border-box}}body{{margin:0;font-family:Inter,sans-serif;color:var(--text);background:var(--bg);font-size:15px;line-height:1.45}}
.top{{height:56px;background:var(--brand-deep);color:#fff;display:flex;align-items:center;gap:16px;padding:0 20px}}
.top b{{font-size:16px}}.chip{{background:rgba(255,255,255,.12);border-radius:999px;padding:4px 12px;font-size:13px}}
.top .acc{{margin-left:auto;font-size:13px;opacity:.9}}
.wrap{{display:flex;min-height:calc(100vh - 56px)}}
.nav{{width:220px;padding:16px 12px;border-right:1px solid var(--border)}}.nav a{{display:block;padding:8px 12px;border-radius:8px;color:var(--text);text-decoration:none;font-size:14px}}
.nav a.on{{background:var(--accent-soft);font-weight:600;color:var(--brand)}}
main{{flex:1;padding:24px 28px}}h1{{font-size:22px;margin:0 0 4px}}.muted{{color:var(--muted)}}
table{{width:100%;border-collapse:collapse;background:var(--surface);border:1px solid var(--border);border-radius:var(--radius);overflow:hidden;margin-top:16px;font-size:14px}}
th,td{{text-align:left;padding:10px 12px;border-bottom:1px solid var(--border)}}th{{font-size:12px;color:var(--muted);font-weight:600;text-transform:uppercase;letter-spacing:.03em}}
.pill{{display:inline-block;border-radius:999px;padding:2px 10px;font-size:12px;font-weight:600;background:var(--accent-soft);color:var(--brand)}}
.pill.g{{background:#fdeee8;color:#9a2f10}}
.scrim{{position:fixed;inset:56px 0 0 0;background:rgba(10,32,68,.28)}}
.drawer{{position:fixed;top:56px;right:0;bottom:0;width:460px;background:var(--surface);box-shadow:-8px 0 24px rgba(10,32,68,.12);padding:22px 24px;overflow:auto}}
.drawer h2{{margin:0 0 14px;font-size:19px}}
label.f{{display:block;font-weight:600;font-size:14px;margin:14px 0 6px}}
.input{{width:100%;height:44px;border:1px solid var(--border);border-radius:var(--radius-sm);padding:0 12px;font:inherit;background:#fff;display:flex;align-items:center;justify-content:space-between}}
fieldset{{border:0;padding:0;margin:16px 0 0}}legend{{font-weight:600;font-size:14px;margin-bottom:8px;padding:0}}
.cards{{display:grid;grid-template-columns:1fr 1fr;gap:10px}}
.card{{border:1.5px solid var(--border);border-radius:var(--radius);padding:12px 12px 12px 40px;position:relative;background:#fff;min-height:84px}}
.card .r{{position:absolute;left:12px;top:14px;width:18px;height:18px;border-radius:50%;border:2px solid var(--muted)}}
.card.on{{border-color:var(--brand);box-shadow:0 0 0 2px var(--brand) inset;background:var(--accent-soft)}}
.card.on .r{{border-color:var(--brand)}}.card.on .r:after{{content:"";position:absolute;inset:3px;border-radius:50%;background:var(--brand)}}
.card b{{display:block;font-size:15px}}.card small{{color:var(--muted);font-size:13px}}
.seg{{display:flex;gap:8px}}.seg span{{flex:1;height:44px;border:1.5px solid var(--border);border-radius:var(--radius-sm);display:flex;align-items:center;justify-content:center;font-weight:600;background:#fff}}
.seg span.on{{border-color:var(--brand);background:var(--accent-soft);color:var(--brand);box-shadow:0 0 0 1px var(--brand) inset}}
.set{{margin-top:16px;border-radius:var(--radius);padding:12px 14px;background:var(--success-soft);border:1px solid #bfe6cc;font-size:14px}}
.set b{{display:block;margin-bottom:2px}}.set ul{{margin:6px 0 0 18px;padding:0}}
.warn{{margin-top:12px;border-radius:var(--radius);padding:12px 14px;background:var(--warn-soft);border:1px solid #f2d9a6;font-size:14px}}
.acts{{display:flex;justify-content:flex-end;gap:10px;margin-top:22px}}
.btn{{height:44px;padding:0 18px;border-radius:999px;border:1px solid var(--border);background:#fff;font:inherit;font-weight:600;display:inline-flex;align-items:center}}
.btn.p{{background:var(--brand);color:#fff;border-color:var(--brand)}}
.dis{{opacity:.55}}
.note{{font-size:13px;color:var(--muted);margin-top:6px}}
.ann{{position:fixed;left:250px;bottom:18px;width:520px;background:#fff;border:2px dashed var(--coral);border-radius:12px;padding:10px 14px;font-size:13px}}
.ann b{{color:var(--coral)}}
body.m .drawer{{position:static;width:auto;box-shadow:none;padding:18px 16px}}
body.m .top{{height:52px}}body.m .top .acc{{display:none}}body.m .scrim{{display:none}}
body.m .cards{{grid-template-columns:1fr}}
"""
def top(scope="Testschool Lobsy"):
    return f'<div class="top"><b>Lobsy</b><span class="chip">{scope}</span><span class="acc">Schoolbeheerder · M. de Vries</span></div>'
def level_cards(sel):
    a = "on" if sel == "po" else ""; b = "on" if sel == "vo" else ""
    return f'''<fieldset><legend>Soort klas</legend><div class="cards" role="radiogroup" aria-label="Soort klas">
<div class="card {a}" role="radio" aria-checked="{str(sel=='po').lower()}"><span class="r"></span><b>Basisschool</b><small>Groep 7 of groep 8</small></div>
<div class="card {b}" role="radio" aria-checked="{str(sel=='vo').lower()}"><span class="r"></span><b>Middelbare school</b><small>Vmbo, mavo, havo, vwo</small></div>
</div></fieldset>'''
def po_fields():
    return '''<fieldset><legend>Groep</legend><div class="seg" role="radiogroup"><span class="on">Groep 7</span><span>Groep 8</span></div></fieldset>
<div class="set" role="status"><b>Vragenlijst: Groep 7/8</b>60 vragen (Nee … Ja!) · 3 puzzelpauzes · ongeveer 30 minuten.
<ul><li>Elke leerlingcode van deze klas krijgt deze vragenlijst. Leerlingen kiezen zelf niets.</li></ul></div>'''
def vo_fields():
    return '''<label class="f">Niveau</label><div class="input">Havo <span class="muted">▾</span></div>
<p class="note">Vmbo-b · Vmbo-k · Vmbo-gt · Mavo · Havo · Vwo · Gemengd · Anders</p>
<label class="f">Leerjaar</label><div class="seg" role="radiogroup"><span>1</span><span class="on">2</span><span>3</span><span>4</span><span>5</span><span>6</span></div>
<div class="set" role="status"><b>Vragenlijst: Middelbare school</b>100 vragen (Klopt niet … Klopt helemaal) · 2 lesdelen · ongeveer 40–45 minuten.
<ul><li>Elke leerlingcode van deze klas krijgt deze vragenlijst. Leerlingen kiezen zelf niets.</li><li>Pauze na het Pauze-eiland: daar kan de les stoppen.</li></ul></div>'''
def common(name, count):
    return f'''<label class="f">Klasnaam</label><div class="input">{name}</div>'''
def tail(count):
    return f'''<label class="f">Aantal leerlingen</label><div class="input">{count}</div>
<p class="note">Lobsy maakt evenveel codes. Namen vul je zelf in op de codelijst.</p>
<fieldset><legend>Leraar(en)</legend><label><input type="checkbox" checked> J. Bakker</label><br><label><input type="checkbox"> S. Yilmaz</label></fieldset>
<div class="acts"><span class="btn">Annuleren</span><span class="btn p">Opslaan</span></div>'''
def list_page():
    return '''<div class="wrap"><nav class="nav"><a>Overzicht</a><a>Te doen</a><a class="on">Klassen &amp; codes</a><a>Leraren</a><a>Resultaten</a><a>Privacy</a></nav>
<main><h1>Klassen &amp; codes</h1><p class="muted">Schooljaar 2026–2027 · 3 klassen</p>
<table><tr><th>Klas</th><th>Soort</th><th>Leerjaar</th><th>Vragenlijst</th><th>Codes</th><th>Ouders</th><th>Testvenster</th></tr>
<tr><td>7A</td><td>Basisschool</td><td>Groep 7</td><td><span class="pill g">Groep 7/8</span></td><td>24</td><td>Bevestigd</td><td>Open</td></tr>
<tr><td>2B</td><td>Havo</td><td>Klas 2</td><td><span class="pill">VO</span></td><td>28</td><td>Bevestigd</td><td>Dicht</td></tr>
<tr><td>3K</td><td>Vmbo-k</td><td>Klas 3</td><td><span class="pill">VO</span></td><td>19</td><td>Nog doen</td><td>Nog niet open</td></tr></table>
</main></div>'''
def doc(body, m=False):
    return f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><style>{CSS}</style></head><body class="{"m" if m else ""}">{body}</body></html>'
pages = {
 "kl-d01-nieuwe-klas-basisschool": (doc(top() + list_page() + '<div class="scrim"></div><aside class="drawer" role="dialog" aria-label="Nieuwe klas"><h2>Nieuwe klas</h2>'
     + common("7A", 24) + level_cards("po") + po_fields() + tail(24) + '</aside>'
     + '<div class="ann"><b>Mockup:</b> de kolom <i>Vragenlijst</i> is nieuw. Niveau en groep bepalen de vragenlijst; de leerling kiest niets.</div>'), (1366, 900), 1),
 "kl-m01-nieuwe-klas-vo": (doc(top("Testschool") + '<aside class="drawer"><h2>Nieuwe klas</h2>' + common("2B", 28) + level_cards("vo") + vo_fields() + tail(28) + '</aside>', True), (390, 1180), 2),
 "kl-m02-klas-bewerken-vergrendeld": (doc(top("Testschool") + '<aside class="drawer"><h2>Klas 2B bewerken</h2>' + common("2B", 28)
     + '<div class="dis">' + level_cards("vo") + '</div>'
     + '<div class="warn" role="note"><b>Soort klas ligt vast.</b> Er zijn al leerlingen van deze klas begonnen. Wisselen tussen basisschool en middelbare school kan niet meer, want dan passen hun antwoorden niet bij de vragenlijst. Niveau en leerjaar binnen de middelbare school kun je wel aanpassen.</div>'
     + vo_fields() + '<div class="acts"><span class="btn">Annuleren</span><span class="btn p">Opslaan</span></div></aside>', True), (390, 1080), 2),
}
async def main():
    async with async_playwright() as p:
        b = await p.chromium.launch(executable_path="/usr/bin/google-chrome")
        for name, (html, (w, h), scale) in pages.items():
            f = OUT / "html" / f"{name}.html"; f.write_text(html, encoding="utf-8")
            pg = await b.new_page(viewport={"width": w, "height": h}, device_scale_factor=scale)
            await pg.goto(f.as_uri()); await pg.evaluate("document.fonts.ready")
            small = await pg.evaluate("""() => [...document.querySelectorAll('.btn,.seg span,.input,.card')].filter(e => e.getBoundingClientRect().height < 44).length""")
            await pg.screenshot(path=str(OUT / f"{name}.png"), full_page=True)
            print(name, "tap<44:", small); await pg.close()
        await b.close()
asyncio.run(main())
