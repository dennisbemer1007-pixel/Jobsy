"""Proposed Lobsy e-mail layout (phase 1 mockup). Email-grade HTML: tables, inline light styles,
one <style> block for mobile + dark overrides, MSO ghost table + VML button. Image paths are relative
for the mockup; production uses absolute https URLs on lobsy.nl."""
from html import escape as E

# Brand tokens (Jobsy.Web design tokens + warm landing tints, pre-mixed to hex because mail clients lack color-mix)
T = dict(bg="#f5f2ee", surface="#fffcfa", text="#122033", muted="#5a6a7d", border="#ddd5cc", brand="#0f2d5c",
         brand_deep="#0a2044", coral="#f54a1b", peach="#fee7df", sun="#f1e5c3", sky="#e7eef7", mint="#ecfdf3",
         success="#15803d", warn="#a65b00", cream="#fff8e7")
# Dark palette (proposed)
D = dict(bg="#0d1726", surface="#15233a", text="#eef2f7", muted="#a9b6c6", border="#2a3a52", sky="#1f3150",
         peach="#3b2621", sun="#3a3220", mint="#16352a", btn="#fffcfa", btn_text="#0f2d5c")
FONT = "Inter,-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif"
FONT_AR = "'Noto Sans Arabic',Tahoma,'Segoe UI',Arial,sans-serif"

TONES = {"peach": (T["peach"], D["peach"]), "sun": (T["sun"], D["sun"]), "sky": (T["sky"], D["sky"]), "mint": (T["mint"], D["mint"])}

def css(rtl=False):
    s = "right" if rtl else "left"
    return f"""
:root{{color-scheme:light dark;supported-color-schemes:light dark}}
body{{margin:0!important;padding:0!important;width:100%!important;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%}}
table{{border-collapse:collapse;mso-table-lspace:0;mso-table-rspace:0}} img{{border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic}}
a[x-apple-data-detectors]{{color:inherit!important;text-decoration:none!important}}
@media (max-width:620px){{
 .outer{{padding:18px 0 24px 0!important}} .card{{border-radius:0!important;border-left:0!important;border-right:0!important}}
 .px{{padding-left:20px!important;padding-right:20px!important}} .h1{{font-size:24px!important;line-height:30px!important}}
 .btn-a{{display:block!important;text-align:center!important}} .btn-t{{width:100%!important}}
 .kv-l,.kv-v{{display:block!important;width:auto!important;padding-{s}:0!important}} .kv-l{{padding-bottom:0!important}} .kv-v{{padding-top:2px!important}}
 .otp{{font-size:34px!important;letter-spacing:6px!important}} .ft{{padding-left:20px!important;padding-right:20px!important}}
}}
@media (prefers-color-scheme:dark){{
 .bg{{background:{D['bg']}!important}} .card{{background:{D['surface']}!important;border-color:{D['border']}!important}}
 .t,.t a{{color:{D['text']}!important}} .m,.m a{{color:{D['muted']}!important}} .ln{{border-color:{D['border']}!important}}
 .tone-peach{{background:{D['peach']}!important}} .tone-sun{{background:{D['sun']}!important}} .tone-sky{{background:{D['sky']}!important}} .tone-mint{{background:{D['mint']}!important}}
 .btn-c{{background:{D['btn']}!important}} .btn-a{{color:{D['btn_text']}!important;background:{D['btn']}!important}}
 .num{{background:{D['sky']}!important;color:{D['text']}!important}} .logo-img{{background:transparent!important}}
}}
/* Outlook.com / Outlook app dark mode */
[data-ogsc] .t{{color:{D['text']}!important}} [data-ogsc] .m{{color:{D['muted']}!important}} [data-ogsb] .bg{{background:{D['bg']}!important}} [data-ogsb] .card{{background:{D['surface']}!important}}
"""

def preheader(text):
    filler = "&#8199;&#65279;&#847; " * 60
    return (f'<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;mso-hide:all;">{E(text)}</div>'
            f'<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;mso-hide:all;">{filler}</div>')

def eyebrow(label, tone="peach"):
    bg = TONES[tone][0]
    return (f'<table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 0 14px 0;"><tr>'
            f'<td class="tone-{tone}" style="background:{bg};border-radius:999px;padding:5px 12px;font-size:13px;line-height:18px;font-weight:600;color:{T["text"]};">'
            f'<span class="t" style="color:{T["text"]};">{E(label)}</span></td></tr></table>')

def h1(text, mk=""):
    return f'<h1 class="h1 t" style="margin:0 0 12px 0;font-size:26px;line-height:32px;font-weight:700;letter-spacing:-0.01em;color:{T["text"]};">{mk}{text}</h1>'

def p(html, muted=False, size=16, mb=14):
    c = T["muted"] if muted else T["text"]
    return f'<p class="{"m" if muted else "t"}" style="margin:0 0 {mb}px 0;font-size:{size}px;line-height:{int(size*1.55)}px;color:{c};">{html}</p>'

def h2(text):
    return f'<h2 class="t" style="margin:22px 0 10px 0;font-size:17px;line-height:24px;font-weight:700;color:{T["text"]};">{text}</h2>'

def kv(rows, tone="sky", mk=""):
    """Fact card: label / value rows. Stacks on mobile."""
    bg = TONES[tone][0]
    tr = ""
    for i, (k, v) in enumerate(rows):
        bt = f"border-top:1px solid {T['border']};" if i else ""
        tr += (f'<tr><td class="kv-l m ln" width="150" valign="top" style="width:150px;{bt}padding:10px 0;font-size:14px;line-height:20px;color:{T["muted"]};">{E(k)}</td>'
               f'<td class="kv-v t ln" valign="top" style="{bt}padding:10px 0 10px 12px;font-size:16px;line-height:22px;font-weight:600;color:{T["text"]};">{v}</td></tr>')
    return (f'{mk}<table role="presentation" width="100%" cellpadding="0" cellspacing="0" class="tone-{tone}" style="background:{bg};border-radius:14px;margin:6px 0 18px 0;">'
            f'<tr><td style="padding:6px 18px;"><table role="presentation" width="100%" cellpadding="0" cellspacing="0">{tr}</table></td></tr></table>')

def steps(items):
    rows = ""
    for i, t in enumerate(items, 1):
        rows += (f'<tr><td width="36" valign="top" style="width:36px;padding:0 0 10px 0;">'
                 f'<div class="num" style="width:26px;height:26px;border-radius:13px;background:{T["sky"]};color:{T["brand"]};font-size:14px;line-height:26px;font-weight:700;text-align:center;">{i}</div></td>'
                 f'<td class="t" valign="top" style="padding:2px 0 10px 0;font-size:16px;line-height:22px;color:{T["text"]};">{t}</td></tr>')
    return f'<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 8px 0;">{rows}</table>'

def button(href, label, mk=""):
    """Bulletproof pill button: VML for Outlook desktop, <a> with padding for everyone else. Full width on mobile."""
    w = max(220, 12 * len(label) + 64)
    return (f'{mk}<table role="presentation" cellpadding="0" cellspacing="0" class="btn-t" style="margin:8px 0 18px 0;"><tr><td class="btn-c" align="center" style="border-radius:999px;background:{T["brand"]};">'
            f'<!--[if mso]><v:roundrect xmlns:v="urn:schemas-microsoft-com:vml" xmlns:w="urn:schemas-microsoft-com:office:word" href="{E(href)}" style="height:50px;v-text-anchor:middle;width:{w}px;" arcsize="50%" stroke="f" fillcolor="{T["brand"]}"><w:anchorlock/><center style="color:#ffffff;font-family:Arial,sans-serif;font-size:16px;font-weight:bold;">{E(label)}</center></v:roundrect><![endif]-->'
            f'<!--[if !mso]><!--><a class="btn-a" href="{E(href)}" style="display:inline-block;background:{T["brand"]};color:#ffffff;font-size:16px;line-height:20px;font-weight:700;text-decoration:none;padding:15px 30px;border-radius:999px;mso-hide:all;">{E(label)}</a><!--<![endif]-->'
            f'</td></tr></table>')

def otp(code, mk=""):
    return (f'{mk}<table role="presentation" width="100%" cellpadding="0" cellspacing="0" class="tone-sky" style="background:{T["sky"]};border-radius:14px;margin:6px 0 14px 0;">'
            f'<tr><td align="center" style="padding:22px 12px;">'
            f'<div class="otp t" data-lobsy-otp="{code}" style="font-family:\'SF Mono\',Menlo,Consolas,\'Roboto Mono\',monospace;font-size:40px;line-height:46px;font-weight:700;letter-spacing:10px;color:{T["text"]};">{code}</div>'
            f'</td></tr></table>')

def note(html):
    return (f'<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:4px 0 6px 0;"><tr>'
            f'<td class="m ln" style="border-top:1px solid {T["border"]};padding:14px 0 0 0;font-size:14px;line-height:21px;color:{T["muted"]};">{html}</td></tr></table>')

def hero_with_mascot(eyebrow_html, h1_html, mascot_src, rtl=False):
    side = "left" if rtl else "right"
    return (f'<table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr><td valign="top">{eyebrow_html}{h1_html}</td>'
            f'<td width="72" valign="top" align="{side}" style="width:72px;"><img src="{mascot_src}" width="64" height="64" alt="" style="display:block;width:64px;height:64px;"></td></tr></table>')

def wrap(inner, *, subject, pre, reason, lang="nl", rtl=False, prefs=True, src="../src", mk=None, labels=None):
    """Full document. mk = dict of mockup-only annotation badges (None in production)."""
    mk = mk or {}
    L = labels or dict(help="Hulp", privacy="Privacy", prefs="Mail-instellingen", sig="Team Lobsy",
                       legal="Lobsy B.V. · Voorbeeldstraat 1, 2611 AA Delft · KvK 00000000")
    d = "rtl" if rtl else "ltr"; al = "right" if rtl else "left"
    font = FONT_AR if rtl else FONT
    links = [(L["help"], "https://lobsy.nl/hulp"), (L["privacy"], "https://lobsy.nl/privacy")]
    if prefs: links.append((L["prefs"], "https://lobsy.nl/account/meldingen"))
    link_html = " &nbsp;·&nbsp; ".join(f'<a href="{h}" style="color:{T["muted"]};text-decoration:underline;">{E(t)}</a>' for t, h in links)
    head = (f'<meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
            f'<meta name="x-apple-disable-message-reformatting"><meta name="format-detection" content="telephone=no,date=no,address=no,email=no,url=no">'
            f'<meta name="color-scheme" content="light dark"><meta name="supported-color-schemes" content="light dark"><title>{E(subject)}</title>'
            f'<!--[if mso]><noscript><xml><o:OfficeDocumentSettings><o:PixelsPerInch>96</o:PixelsPerInch></o:OfficeDocumentSettings></xml></noscript><![endif]-->'
            f'<style>{css(rtl)}</style>')
    body = f"""{preheader(pre)}
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" class="bg" style="background:{T['bg']};" dir="{d}">
<tr><td align="center" class="outer" style="padding:28px 16px 32px 16px;">
<!--[if mso]><table role="presentation" width="600" align="center" cellpadding="0" cellspacing="0"><tr><td><![endif]-->
<div style="max-width:600px;margin:0 auto;font-family:{font};" dir="{d}">
 <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr><td class="px" align="{al}" style="padding:0 4px 16px 4px;">
  {mk.get('logo','')}<a href="https://lobsy.nl" style="text-decoration:none;"><img class="logo-img" src="{src}/lobsy-128.png" width="36" height="36" alt="Lobsy" style="display:inline-block;vertical-align:middle;width:36px;height:36px;"><span class="t" style="display:inline-block;vertical-align:middle;margin-{'right' if rtl else 'left'}:8px;font-size:20px;line-height:36px;font-weight:700;letter-spacing:-0.01em;color:{T['brand']};">Lobsy</span></a>
 </td></tr></table>
 <table role="presentation" width="100%" cellpadding="0" cellspacing="0" class="card" style="background:{T['surface']};border:1px solid {T['border']};border-radius:18px;border-collapse:separate;">
  <tr><td class="px" align="{al}" style="padding:30px 36px 20px 36px;text-align:{al};font-family:{font};">
   {inner}
   <p class="m" style="margin:18px 0 0 0;font-size:14px;line-height:20px;color:{T['muted']};">{mk.get('sig','')}{E(L['sig'])}</p>
  </td></tr>
 </table>
 <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr><td class="ft m" align="{al}" style="padding:20px 8px 0 8px;font-size:13px;line-height:20px;color:{T['muted']};text-align:{al};font-family:{font};">
  {mk.get('foot','')}<p class="m" style="margin:0 0 8px 0;">{reason}</p>
  <p class="m" style="margin:0 0 8px 0;">{link_html}</p>
  <p class="m" style="margin:0;">{E(L['legal'])}</p>
 </td></tr></table>
</div>
<!--[if mso]></td></tr></table><![endif]-->
</td></tr></table>"""
    return head, body

def document(head, body, lang="nl", rtl=False):
    return (f'<!DOCTYPE html><html lang="{lang}" dir="{"rtl" if rtl else "ltr"}" xmlns:v="urn:schemas-microsoft-com:vml" xmlns:o="urn:schemas-microsoft-com:office:office">'
            f'<head>{head}</head><body class="bg" style="margin:0;padding:0;background:{T["bg"]};">{body}</body></html>')
