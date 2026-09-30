"""Shared shell for the error-page (er-*) and public/legal-page (pb-*) mockups.
Reuses the warm landing style from ../landing (tokens, header, buttons, blobs, waves, mascot) 1:1."""
import pathlib, sys
L = pathlib.Path(__file__).resolve().parent.parent / "landing"
sys.path.insert(0, str(L))
from ui import ic, LOGO, MASCOT, vb            # noqa: E402
from w_common import em, blob, wave            # noqa: E402
from css_lp import CSS as BASE                 # noqa: E402

P = r'''
body{background:var(--bg)}
.dir-rtl{font-family:"Noto Sans Arabic",var(--font)}
.hdr .nav a.on{background:var(--surface);color:var(--text)}
.crumb{font-size:var(--text-sm);color:var(--muted);display:flex;gap:8px;align-items:center}
.crumb a{color:var(--brand);font-weight:600}
.code-pill{display:inline-flex;align-items:center;gap:8px;height:32px;padding:0 14px;border-radius:999px;background:var(--surface);box-shadow:var(--shadow);font-size:var(--text-sm);font-weight:600}
.code-pill b{color:var(--coral)}
/* error hero */
.eh{background:var(--cream);padding:40px 0 120px;position:relative;overflow:hidden}
.eh .wrap{display:grid;grid-template-columns:1.05fr .95fr;gap:48px;align-items:center;position:relative}
.eh h1{font-size:3rem;line-height:1.08;font-weight:700;letter-spacing:-.02em;margin:18px 0 14px}
.eh h1 em{font-style:normal;color:var(--coral)}
.eh .lead{font-size:var(--text-lg);color:var(--muted);max-width:540px}
.eh .lead b{color:var(--text)}
.eh .cta{display:flex;gap:12px;margin-top:26px;flex-wrap:wrap}
.btn.lg{height:52px;padding:0 24px;font-size:var(--text-lg)}
.btn.onpri{background:var(--brand);border:1px solid var(--brand);color:var(--surface)}
.btn.ondark{background:var(--surface);border:1px solid var(--border);color:var(--text)}
.btn.ghost{background:transparent;border:1px solid transparent;color:var(--brand)}
.scene{position:relative;height:400px}
.scene .disc{position:absolute;inset:20px 30px;border-radius:50%;background:var(--surface);box-shadow:var(--shadow-soft)}
.scene .big{position:absolute;right:10px;top:-8px;font-size:170px;font-weight:800;letter-spacing:-.04em;color:var(--sun-2);line-height:1}
.scene .lob{position:absolute;width:210px;left:50%;top:52%;transform:translate(-50%,-50%)}
.scene .bubble{top:40px;left:40px}
.scene .tag{position:absolute;background:var(--surface);border-radius:18px;box-shadow:var(--shadow-soft);padding:10px 14px;font-size:var(--text-sm);font-weight:600;display:flex;gap:10px;align-items:center}
.scene .tag small{display:block;color:var(--muted);font-weight:500}
.meta{margin-top:18px;color:var(--muted);font-size:var(--text-sm);display:flex;gap:16px;flex-wrap:wrap}
.meta span{display:inline-flex;gap:6px;align-items:center}
.ref{display:inline-flex;align-items:center;gap:10px;margin-top:20px;background:var(--surface);border:1px dashed var(--border);border-radius:14px;padding:10px 14px;font-size:var(--text-sm)}
.ref code{font-family:ui-monospace,Menlo,monospace;font-size:var(--text-sm);background:var(--pearl-mid);padding:2px 8px;border-radius:8px}
.ref .btn{height:34px;padding:0 12px;font-size:var(--text-sm)}
/* quick links */
.ql{padding:10px 0 100px}
.ql h2{font-size:1.75rem;font-weight:700;letter-spacing:-.01em;margin-bottom:18px}
.qg{display:grid;grid-template-columns:repeat(3,1fr);gap:18px}
.qc{background:var(--surface);border-radius:var(--radius-lg);box-shadow:var(--shadow-soft);padding:22px;display:flex;gap:14px;align-items:flex-start}
.qc h3{font-size:var(--text-lg);font-weight:700}.qc p{color:var(--muted);font-size:var(--text-sm);margin-top:4px}
.qc .go{margin-top:10px;color:var(--brand);font-weight:600;font-size:var(--text-sm);display:inline-flex;gap:6px;align-items:center}
.note{margin-top:22px;color:var(--muted);font-size:var(--text-sm)}.note a{color:var(--brand);font-weight:600;text-decoration:underline}
/* simple footer */
.sft{background:var(--pearl-mid);color:var(--muted);padding:30px 0 26px;font-size:var(--text-sm)}
.sft .wrap{display:flex;gap:18px;align-items:center;flex-wrap:wrap}
.sft .brand{color:var(--text)}
.sft nav{display:flex;gap:18px;flex-wrap:wrap}
.sft .legal{flex-basis:100%;border-top:1px solid var(--border);padding-top:14px;margin-top:6px;font-size:var(--text-xs);display:flex;gap:14px;flex-wrap:wrap}
.sft .sp{flex:1}
/* annotation marks */
.mk{display:inline-grid;place-items:center;width:22px;height:22px;border-radius:11px;background:var(--coral);color:#fff;font:700 12px/22px Inter,Arial;flex:none;box-shadow:0 0 0 3px var(--surface)}
.mk.abs{position:absolute;z-index:5}
.anno{background:var(--surface);border-radius:var(--radius-lg);box-shadow:var(--shadow-soft);padding:18px 20px;font-size:var(--text-sm)}
.anno h4{font-size:var(--text-md);margin-bottom:10px}
.anno li{list-style:none;display:flex;gap:10px;margin-bottom:9px;line-height:1.45}.anno ul{padding:0}
/* public pages */
.ph{background:var(--cream);padding:34px 0 96px;position:relative;overflow:hidden}
.ph h1{font-size:2.75rem;line-height:1.1;font-weight:700;letter-spacing:-.02em;margin:14px 0 12px}
.ph h1 em{font-style:normal;color:var(--coral)}
.ph .lead{font-size:var(--text-lg);color:var(--muted);max-width:640px}
.sec{padding:40px 0}
.sec h2{font-size:2rem;font-weight:700;letter-spacing:-.01em;margin-bottom:8px}
.sec .sub{color:var(--muted);max-width:640px;margin-bottom:22px}
.eyebrow{font-size:var(--text-sm)}
.card{background:var(--surface)}
.steps{display:grid;grid-template-columns:repeat(4,1fr);gap:18px}
.stp{background:var(--surface);border-radius:var(--radius-lg);box-shadow:var(--shadow-soft);padding:22px;position:relative}
.stp .n{position:absolute;top:16px;inset-inline-end:18px;font-weight:800;font-size:28px;color:var(--pearl-mid)}
.stp h3{font-size:var(--text-lg);margin:14px 0 6px}.stp p{color:var(--muted);font-size:var(--text-sm)}
.stp .go{margin-top:12px;display:inline-flex;gap:6px;align-items:center;color:var(--brand);font-weight:600;font-size:var(--text-sm)}
.seg2{display:inline-flex;background:var(--surface);border-radius:999px;padding:4px;box-shadow:var(--shadow);gap:4px}
.seg2 span{height:38px;padding:0 16px;border-radius:999px;display:inline-flex;align-items:center;gap:8px;font-weight:600;font-size:var(--text-sm);color:var(--muted)}
.seg2 span.on{background:var(--peach);color:var(--text)}
.two{display:grid;grid-template-columns:1fr 1fr;gap:22px}
.box{background:var(--surface);border-radius:var(--radius-lg);box-shadow:var(--shadow-soft);padding:24px}
.box h3{font-size:var(--text-xl);margin-bottom:8px}.box p{color:var(--muted)}
.chkl{list-style:none;padding:0;margin-top:10px}.chkl li{display:flex;gap:10px;margin:8px 0;align-items:flex-start}.chkl .i{color:var(--success);margin-top:3px}
.faq details{background:var(--surface);border-radius:18px;box-shadow:var(--shadow);padding:16px 20px;margin-bottom:10px}
.faq summary{font-weight:600;list-style:none;display:flex;justify-content:space-between}
.faq p{color:var(--muted);margin-top:8px}
/* legal layout */
.lgl{display:grid;grid-template-columns:270px 1fr;gap:34px;align-items:start;padding:34px 0 60px}
.toc{position:sticky;top:20px;background:var(--surface);border-radius:var(--radius-lg);box-shadow:var(--shadow-soft);padding:18px}
.toc h4{font-size:var(--text-sm);color:var(--muted);text-transform:none;margin-bottom:8px}
.toc a{display:block;padding:7px 10px;border-radius:10px;font-size:var(--text-sm);color:var(--text)}
.toc a.on{background:var(--peach);font-weight:600}
.toc .sep{height:1px;background:var(--border);margin:10px 0}
.doc{display:flex;flex-direction:column;gap:18px}
.dsec{background:var(--surface);border-radius:var(--radius-lg);box-shadow:var(--shadow-soft);padding:24px 26px}
.dsec h2{font-size:var(--text-xl);font-weight:700;margin-bottom:10px;display:flex;gap:10px;align-items:center}
.dsec p,.dsec li{color:var(--text);opacity:.9;font-size:var(--text-md)}
.dsec ul{padding-inline-start:20px;margin-top:6px}.dsec li{margin:4px 0}
.tldr{background:var(--mint);border-radius:16px;padding:14px 16px;margin-bottom:14px;display:flex;gap:12px}
.tldr b{display:block;font-size:var(--text-sm);color:var(--success)}
.tldr p{font-size:var(--text-md);opacity:1}
.idc{display:grid;grid-template-columns:auto 1fr;gap:6px 18px;font-size:var(--text-md);background:var(--pearl);border-radius:14px;padding:14px 16px}
.idc dt{color:var(--muted)}.idc dd{font-weight:600}
.cfg{display:inline-flex;align-items:center;gap:6px;font-size:var(--text-xs);font-weight:600;color:var(--warn);background:var(--sun);border-radius:999px;padding:1px 8px;margin-inline-start:8px;vertical-align:middle}
.ptab{width:100%;border-collapse:separate;border-spacing:0;font-size:var(--text-sm);margin-top:8px}
.ptab th{text-align:start;color:var(--muted);font-weight:600;padding:8px 10px;border-bottom:1px solid var(--border)}
.ptab td{padding:10px;border-bottom:1px solid var(--pearl-mid);vertical-align:top}
.ptab td:first-child{font-weight:600;white-space:nowrap}
.ptab tr.new td{background:color-mix(in srgb,var(--sun) 55%,var(--surface))}
.flag{display:inline-block;font-size:var(--text-xs);font-weight:700;padding:1px 8px;border-radius:999px;background:var(--peach);color:var(--coral)}
.flag.ok{background:var(--mint);color:var(--success)}
.docnote{display:flex;gap:10px;align-items:center;background:var(--sky);border-radius:16px;padding:12px 16px;font-size:var(--text-sm)}
.upd{display:flex;gap:14px;flex-wrap:wrap;color:var(--muted);font-size:var(--text-sm);margin-top:10px}
.upd span{display:inline-flex;gap:6px;align-items:center}
.dl{display:flex;gap:14px;align-items:center;flex-wrap:wrap}
/* company page */
.cp{display:grid;grid-template-columns:1fr 420px;gap:26px;padding:26px 0 60px}
.logo-sq{width:72px;height:72px;border-radius:20px;background:var(--surface);box-shadow:var(--shadow);display:grid;place-items:center;font-size:34px}
.jc{background:var(--surface);border-radius:20px;box-shadow:var(--shadow-soft);overflow:hidden}
.jc .ph2{height:120px;position:relative}
.jc .b{padding:14px 16px}.jc h4{font-size:var(--text-md)}.jc p{color:var(--muted);font-size:var(--text-sm)}
.jg{display:grid;grid-template-columns:repeat(2,1fr);gap:16px}
.mapbox{border-radius:var(--radius-xl);overflow:hidden;box-shadow:var(--shadow-soft);height:520px;position:relative;background:var(--sky)}
.pin{position:absolute;width:30px;height:30px;border-radius:50% 50% 50% 0;transform:rotate(-45deg);background:var(--coral);box-shadow:0 4px 10px rgba(0,0,0,.18)}
/* mobile */
.m .wrap{padding:0 var(--space-4)}
.m .mh{position:relative;z-index:3}
.m .eh{padding:18px 0 130px}.m .eh .wrap{grid-template-columns:1fr;gap:6px}
.m .eh h1{font-size:2.1rem}.m .eh .lead{font-size:var(--text-md)}
.m .eh .cta{flex-direction:column}.m .eh .cta .btn{height:52px;width:100%;justify-content:center}
.m .scene{height:250px;order:-1}.m .scene .lob{width:140px}.m .scene .big{font-size:96px;right:0;top:14px}
.m .scene .disc{inset:10px 40px}
.m .qg{grid-template-columns:1fr}.m .ql h2{font-size:1.4rem}
.m .ph{padding:20px 0 70px}.m .ph h1{font-size:2rem}.m .ph .lead{font-size:var(--text-md)}
.m .steps,.m .two,.m .jg{grid-template-columns:1fr}
.m .sec h2{font-size:1.5rem}
.m .lgl{grid-template-columns:1fr;padding:16px 0 100px;gap:14px}
.m .toc{position:static;padding:0;box-shadow:none;background:none}
.m .dsec{padding:18px}.m .dsec h2{font-size:var(--text-lg)}
.m .cp{grid-template-columns:1fr;padding:16px 0 40px}
.m .ptab thead{display:none}.m .ptab tr{display:block;border-bottom:1px solid var(--pearl-mid);padding:8px 0}.m .ptab td{display:block;border:0;padding:2px 0}
.m .sft .wrap{flex-direction:column;align-items:flex-start;gap:12px}
.m .ref{flex-wrap:wrap}
.mtoc{display:flex;align-items:center;justify-content:space-between;background:var(--surface);border-radius:16px;box-shadow:var(--shadow);padding:12px 14px;font-weight:600}
'''

def page(body, cls="", lang="nl", rtl=False, css=""):
    d = "rtl" if rtl else "ltr"
    return (f'<!doctype html><html lang="{lang}" dir="{d}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width">'
            f'<style>{BASE}{P}{css}</style></head><body class="{cls} {"dir-rtl" if rtl else ""}">{body}</body></html>')

NAV = ["Hoe het werkt", "Banenkaart", "Werkgevers", "Scholen", "Partners"]
def dhdr(active=None, logged=None, lang="NL", nav=None, login="Inloggen", cta="Doe de gratis test"):
    items = "".join(f'<a class="{"on" if n == active else ""}">{n}</a>' for n in (nav or NAV))
    right = (f'<span class="lang">{ic("globe")}{lang}{ic("chev")}</span>' +
             (f'<span class="code-pill">{em("🙂","peach","s")}{logged}</span>' if logged else f'<a class="btn sm ondark">{login}</a><a class="btn sm onpri">{cta}</a>'))
    return f'<header class="hdr"><div class="wrap"><a class="brand"><img src="{LOGO}" alt="">Lobsy</a><nav class="nav">{items}</nav><div class="r">{right}</div></div></header>'

def mhdr(login="Inloggen", logged=None):
    r = f'<span class="code-pill" style="height:34px">{logged}</span>' if logged else f'<a class="t">{login}</a>'
    return f'<header class="mh"><a class="brand"><img src="{LOGO}" alt="">Lobsy</a><div class="r">{r}<span class="ib" aria-label="Menu">{ic("menu")}</span></div></header>'

LEGAL = "Lobsy · [adres uit config] · KvK [uit config]"
def footer(links=("Hoe het werkt", "Wie zijn wij", "Privacy", "Cookies", "Algemene voorwaarden", "Gebruiksvoorwaarden"), legal=LEGAL, lang="Nederlands", copy="© 2026 Lobsy"):
    ls = "".join(f"<a>{a}</a>" for a in links)
    return (f'{wave("var(--bg)", "var(--pearl-mid)")}<footer class="sft"><div class="wrap"><a class="brand"><img src="{LOGO}" alt="">Lobsy</a><nav>{ls}</nav><div class="sp"></div>'
            f'<span class="lang" style="height:auto">{ic("globe")}{lang}{ic("chev")}</span><div class="legal"><span>{copy}</span><span>{legal}</span></div></div></footer>')

def mascot_img(style=""):
    return f'<img class="lob" src="{MASCOT}" alt="" style="{style}">'

def anno(title, items):
    li = "".join(f'<li><span class="mk">{i+1}</span><span>{t}</span></li>' for i, t in enumerate(items))
    return f'<div class="anno"><h4>{title}</h4><ul>{li}</ul></div>'

def mk(n, x, y):
    return f'<span class="mk abs" style="left:{x}px;top:{y}px">{n}</span>'
P += "[dir=rtl] .go svg,[dir=rtl] .btn svg{transform:scaleX(-1)} .sec{padding-bottom:90px}"
P += ".seg2 span{white-space:nowrap} .m .seg2{display:flex;max-width:100%;overflow-x:auto} .m .seg2 span{padding:0 12px} .m .ph{padding-bottom:110px}"
