"""Render Lobsy e-mail mockups (em-*) to PNG. Run: python3 build.py [filter]
Desktop = 600px mail column (viewport 680, annotated base 1120); mobile = 375px viewport. Both at 2x."""
import os, pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from playwright.sync_api import sync_playwright
from em_layout import document, T, D
import em_mails as ms
from em_current import current_confirmation

out = pathlib.Path(__file__).parent; (out / "html").mkdir(exist_ok=True)

FRAME_CSS = f"""
.fr{{max-width:600px;margin:0 auto;padding:18px 16px 0 16px;font-family:Inter,Arial,sans-serif}}
.fr-in{{display:flex;gap:12px;align-items:flex-start;background:#ffffff;border:1px solid {T['border']};border-radius:14px;padding:12px 14px}}
.fr-av{{width:40px;height:40px;border-radius:20px;background:{T['cream']};display:grid;place-items:center;flex:none}} .fr-av img{{width:28px;height:28px}}
.fr-top{{font-size:14px;color:{T['text']};display:flex;gap:8px;align-items:center;flex-wrap:wrap}} .fr-top span{{color:{T['muted']}}}
.fr-sub{{font-size:15px;font-weight:700;color:{T['text']};margin-top:2px}} .fr-pre{{font-size:14px;color:{T['muted']};white-space:nowrap;overflow:hidden;text-overflow:ellipsis;max-width:480px}}
.tag{{margin-inline-start:auto;background:{T['sun']};color:{T['text']}!important;font-size:12px;font-weight:700;padding:2px 10px;border-radius:999px}}
.fr-cap{{font-size:12px;color:{T['muted']};margin:6px 4px 0}}
.mkb{{display:inline-block;width:22px;height:22px;border-radius:11px;background:{T['coral']};color:#fff!important;font:700 12px/22px Inter,Arial;text-align:center;margin-inline-end:6px;vertical-align:middle}}
@media (max-width:620px){{.fr{{padding:10px 8px 0}} .fr-pre{{max-width:250px}}}}
@media (prefers-color-scheme:dark){{.fr-in{{background:{D['surface']};border-color:{D['border']}}} .fr-top,.fr-sub{{color:{D['text']}}} .fr-top span,.fr-pre,.fr-cap{{color:{D['muted']}}} .fr-av{{background:{D['sky']}}} .tag{{background:{D['sun']};color:{D['text']}!important}}}}
.notes{{font-family:Inter,Arial,sans-serif;color:{T['text']};padding:24px 28px 24px 0}} .notes h3{{font-size:18px;margin:0 0 12px}}
.notes .mkb{{flex:none}} .mkb.in{{position:absolute;margin-inline-start:-58px}} .mkb.lg{{position:absolute;margin-inline-start:-34px;margin-top:7px}} .mkb.st{{position:absolute;left:4px;top:34px}}
.notes li{{list-style:none;display:flex;gap:10px;margin:0 0 12px;font-size:14px;line-height:20px}} .notes ul{{padding:0;margin:0}}
.notes .tech{{margin-top:18px;padding-top:14px;border-top:1px solid {T['border']};font-size:13px;color:{T['muted']};line-height:19px}} .notes .tech p{{margin:0 0 6px}}
"""

def strip(m, rtl=False):
    cap = "Zo ziet het eruit in de inbox" if not rtl else "معاينة صندوق الوارد"
    return (f'<div class="fr" dir="{"rtl" if rtl else "ltr"}"><div class="fr-in"><div class="fr-av"><img src="../src/lobsy-128.png" alt=""></div><div style="min-width:0;flex:1">'
            f'<div class="fr-top"><b>{ms.FROM[0]}</b><span>{ms.FROM[1]}</span><span class="tag">Voorbeelddata</span></div>'
            f'<div class="fr-sub">{m["subject"]}</div><div class="fr-pre">{m["pre"]}</div></div></div><div class="fr-cap">{cap}</div></div>')

NOTES = """<div class="notes"><h3>Basislayout (voorstel)</h3><ul>
<li><span class="mkb">1</span><span><b>Afzender, onderwerp, preheader.</b> De preheader is een eigen zin en herhaalt het onderwerp niet. Er staat opvulling achter, zodat er geen losse tekst uit de mail achter komt.</span></li>
<li><span class="mkb">2</span><span><b>Logo.</b> Klein merkteken (36px, https + alt). "Lobsy" staat er als echte tekst naast, dus het blijft leesbaar als afbeeldingen uit staan.</span></li>
<li><span class="mkb">3</span><span><b>Label.</b> Het soort mail in 1–2 woorden, in een warme tint (peach, zon, lucht, mint).</span></li>
<li><span class="mkb">4</span><span><b>Kop.</b> Eén zin in B1 die zegt wat er is gebeurd. 26px, 24px op mobiel.</span></li>
<li><span class="mkb">5</span><span><b>Feitenkaart.</b> Alleen de belangrijkste gegevens. Op mobiel staat het label boven de waarde.</span></li>
<li><span class="mkb">6</span><span><b>Eén knop.</b> Pilvorm, 50px hoog, marine. Werkt in Outlook (VML) en is op mobiel even breed als het scherm.</span></li>
<li><span class="mkb">7</span><span><b>Footer.</b> Waarom je deze mail krijgt, dan Hulp · Privacy · Mail-instellingen (alleen bij meldingen), dan de bedrijfsgegevens en het KvK-nummer.</span></li></ul>
<div class="tech"><p><b>Techniek</b> (geldt voor elke mail)</p><p>• Mail van 600px met een ghost table voor Outlook, tabellen en inline stijl. Eén &lt;style&gt; voor mobiel en donker.</p>
<p>• color-scheme light dark, met eigen donkere kleuren (ook [data-ogsc] voor Outlook).</p><p>• lang en dir per taal. Bij AR: rtl en &lt;bdi&gt; om Nederlandse namen.</p>
<p>• Elke mail krijgt ook een platte-tekstversie, plus Reply-To. Meldingen krijgen List-Unsubscribe (+ One-Click).</p><p>• Geen trackingpixel en geen herschreven links.</p></div></div>"""

def page(head, body, m, lang="nl", rtl=False, notes=False):
    inner = strip(m, rtl) + body
    if notes:
        inner = f'<div style="display:flex;align-items:flex-start"><div style="width:710px;flex:none;padding-left:30px;position:relative"><span class="mkb st">1</span>{inner}</div>{NOTES}</div>'
    return document(head + f"<style>{FRAME_CSS}</style>", inner, lang=lang, rtl=rtl)

def build_list():
    L = []
    s, pre, head, body = current_confirmation()
    cur = dict(subject=s, pre=pre)
    L.append(("em-00-huidig-sollicitatie", lambda: page(head, body, cur), {}, False))
    def mail(fn, annot=False, **pkw):
        m = fn(annot) if annot else fn()
        h, b = ms.render(m)
        lang = m["kw"].get("lang", "nl"); rtl = m["kw"].get("rtl", False)
        return m, h, b, lang, rtl
    def mk(fn, annot=False):
        def f(annot_on=annot):
            m, h, b, lang, rtl = mail(fn, annot_on)
            return page(h, b, m, lang, rtl, notes=annot_on)
        return f
    def prod(fn):
        m, h, b, lang, rtl = mail(fn)
        return document(h, b, lang, rtl)
    for name, fn in [("em-01-basis-layout", ms.basis), ("em-02-verificatiecode", ms.code), ("em-03-sollicitatie-verstuurd", ms.verstuurd),
                     ("em-04-nieuwe-sollicitatie", ms.nieuw_werkgever), ("em-05-reactie-geaccepteerd", ms.geaccepteerd),
                     ("em-06-uitnodiging", ms.uitnodiging), ("em-07-overnameverzoek", ms.overname),
                     ("em-08-donker", ms.geaccepteerd), ("em-09-arabisch-rtl", ms.verstuurd_ar)]:
        L.append((name, mk(fn), {"annot": name.startswith("em-01"), "dark": name.startswith("em-08"), "prod": lambda fn=fn: prod(fn)}, False))
    return L

CHECK = """() => { const r=[]; const W=document.documentElement.clientWidth;
 document.querySelectorAll('body *').forEach(e=>{const b=e.getBoundingClientRect();
  if(b.width&&b.right>W+1) r.push('overflowX '+e.tagName+'.'+e.className+' '+Math.round(b.right));
  if(e.childNodes.length&&[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim())&&parseFloat(getComputedStyle(e).fontSize)<12) r.push('small '+e.textContent.slice(0,20));
  if(e.tagName==='IMG'&&!e.complete) r.push('img not loaded '+e.src);});
 return {issues:[...new Set(r)].slice(0,12), h:document.documentElement.scrollHeight}; }"""

only = sys.argv[1:]
with sync_playwright() as pw:
    br = pw.chromium.launch(executable_path="/usr/bin/google-chrome")
    for name, fn, opt, _ in build_list():
        if only and not any(o in name for o in only): continue
        if opt.get("prod"):
            (out / "html" / f"{name}.mail.html").write_text(opt["prod"]())
        for kind in ("d", "m"):
            annot = opt.get("annot") and kind == "d"
            html = fn(annot) if opt.get("annot") else fn()
            f = out / "html" / f"{name.replace('em-', 'em-' + kind)}.html"; f.write_text(html)
            width = (1150 if annot else 680) if kind == "d" else 375
            ctx = br.new_context(viewport=dict(width=width, height=800), device_scale_factor=2,
                                 color_scheme="dark" if opt.get("dark") else "light", reduced_motion="reduce")
            pg = ctx.new_page(); pg.goto(f.as_uri()); pg.wait_for_timeout(250)
            res = pg.evaluate(CHECK)
            png = out / f"{name.replace('em-', 'em-' + kind)}.png"
            pg.screenshot(path=str(png), full_page=True)
            print(png.name, res["h"], res["issues"] or "ok")
            ctx.close()
    br.close()
