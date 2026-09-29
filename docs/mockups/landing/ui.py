"""Shared parts for the Lobsy landing mockups (lp-*). Tokens 1:1 from app.css :root + design-system.mdc.
Images: brand logo (lobster-pin) and mascot copied from Jobsy.Web/wwwroot/images/brand."""
import pathlib, base64
d = pathlib.Path(__file__).parent
def b64(name, mime="image/png"):
    return f"data:{mime};base64," + base64.b64encode((d / "src" / name).read_bytes()).decode()
LOGO = b64("lobsy-128.png")
LOBSTER = b64("lobsy.png")
MASCOT = b64("mascot-256.png")

P = dict(
 arrow='<path d="M5 12h14M13 6l6 6-6 6"/>',
 check='<path d="m5 12 5 5 9-10"/>',
 lock='<rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/>',
 user='<circle cx="12" cy="8" r="4"/><path d="M4 21v-1a6 6 0 0 1 6-6h4a6 6 0 0 1 6 6v1"/>',
 building='<rect x="4" y="3" width="11" height="18" rx="1"/><path d="M15 9h4a1 1 0 0 1 1 1v11H15M8 7h3M8 11h3M8 15h3"/>',
 grad='<path d="M2 9 12 4l10 5-10 5z"/><path d="M6 11v5c3 2 9 2 12 0v-5"/>',
 handshake='<path d="M3 11l4-4 4 2 3-2 7 5"/><path d="M7 7 3 11l7 7 2-2M14 7l4 4-5 5"/>',
 map='<path d="M9 4 3 6v14l6-2 6 2 6-2V4l-6 2z"/><path d="M9 4v14M15 6v14"/>',
 pin='<path d="M12 21s-7-6.2-7-12a7 7 0 0 1 14 0c0 5.8-7 12-7 12z"/><circle cx="12" cy="9" r="2.5"/>',
 bike='<circle cx="6" cy="16" r="3.5"/><circle cx="18" cy="16" r="3.5"/><path d="M6 16l4-7h5l3 7M10 9 8.5 6H7M15 9l-3 7"/>',
 bus='<rect x="5" y="3" width="14" height="15" rx="2"/><path d="M5 11h14M8 21v-3M16 21v-3M8 15h.01M16 15h.01"/>',
 car='<path d="M5 16V12l2-5h10l2 5v4M3 16h18v3H3zM7 19v2M17 19v2"/>',
 test='<path d="M9 3h6M10 3v6L5 18a2 2 0 0 0 1.8 3h10.4A2 2 0 0 0 19 18l-5-9V3"/><path d="M7.5 14h9"/>',
 passport='<rect x="5" y="3" width="14" height="18" rx="2"/><circle cx="12" cy="10" r="3"/><path d="M9 16h6"/>',
 compass='<circle cx="12" cy="12" r="9"/><path d="m15.5 8.5-2 5-5 2 2-5z"/>',
 heart='<path d="M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10z"/>',
 x='<path d="M6 6l12 12M18 6 6 18"/>',
 report='<path d="M6 3h9l4 4v14H6z"/><path d="M14 3v5h5M9 13h6M9 17h4"/>',
 shield='<path d="M12 3 4 6v6c0 5 3.5 8 8 9 4.5-1 8-4 8-9V6z"/><path d="m9 12 2 2 4-4"/>',
 device='<rect x="7" y="2.5" width="10" height="19" rx="2"/><path d="M11 18.5h2"/>',
 eyeoff='<path d="M3 3l18 18M10.6 5.1A9.8 9.8 0 0 1 12 5c5 0 8.5 4.5 9.5 7-.4 1-1.2 2.3-2.4 3.6M6.4 6.4C4.5 7.8 3.2 9.8 2.5 12c1 2.5 4.5 7 9.5 7 1.6 0 3-.4 4.3-1.1"/>',
 trash='<path d="M4 7h16M9 7V4h6v3M6 7l1 14h10l1-14"/>',
 globe='<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3c3 3.5 3 14.5 0 18M12 3c-3 3.5-3 14.5 0 18"/>',
 chev='<path d="m6 9 6 6 6-6"/>',
 chevr='<path d="m9 6 6 6-6 6"/>',
 chevl='<path d="m15 6-6 6 6 6"/>',
 plus='<path d="M12 5v14M5 12h14"/>',
 menu='<path d="M4 7h16M4 12h16M4 17h16"/>',
 wave='<path d="M2 15c2.5 0 2.5-2 5-2s2.5 2 5 2 2.5-2 5-2 2.5 2 5 2"/><path d="M2 19c2.5 0 2.5-2 5-2s2.5 2 5 2 2.5-2 5-2 2.5 2 5 2"/><path d="M12 3v7M9 7l3 3 3-3"/>',
 antenna='<path d="M8 21c0-5 1-9 4-12M16 21c0-5-1-9-4-12"/><path d="M12 9 7 3M12 9l5-6"/>',
 rock='<path d="M3 19 7 10l4 2 3-5 7 12z"/>',
 claw='<path d="M7 21v-6a5 5 0 0 1 5-5"/><path d="M12 10c0-4 3-7 7-7-1 3-2 5-5 6M12 10c2 0 5 1 7 4-3 1-6 0-7-2"/>',
 layers='<path d="m12 3 9 5-9 5-9-5z"/><path d="m3 13 9 5 9-5"/>',
 sparkle='<path d="M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M6 18l2.5-2.5M15.5 8.5 18 6"/>',
 coin='<circle cx="12" cy="12" r="9"/><path d="M15 9.5c-.5-1-1.6-1.5-3-1.5-1.7 0-3 .9-3 2s1.3 1.7 3 2 3 .9 3 2-1.3 2-3 2c-1.4 0-2.5-.5-3-1.5M12 6v2M12 16v2"/>',
 share='<circle cx="6" cy="12" r="2.5"/><circle cx="18" cy="6" r="2.5"/><circle cx="18" cy="18" r="2.5"/><path d="m8.2 10.8 7.6-3.6M8.2 13.2l7.6 3.6"/>',
 clock='<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
 mail='<rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 7 9 6 9-6"/>',
 star='<path d="m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1L3.2 9.5l6.1-.9z"/>',
)
def ic(n, cls="i"):
    return f'<svg class="{cls}" viewBox="0 0 24 24" aria-hidden="true">{P[n]}</svg>'

GOOGLE = ('<svg class="i" viewBox="0 0 24 24" aria-hidden="true" style="stroke:none"><path fill="#4285F4" d="M21.6 12.2c0-.7-.1-1.4-.2-2H12v3.8h5.4a4.6 4.6 0 0 1-2 3v2.5h3.2c1.9-1.7 3-4.3 3-7.3z"/>'
          '<path fill="#34A853" d="M12 22c2.7 0 5-.9 6.6-2.4l-3.2-2.5c-.9.6-2 1-3.4 1-2.6 0-4.8-1.8-5.6-4.1H3.1v2.6A10 10 0 0 0 12 22z"/>'
          '<path fill="#FBBC05" d="M6.4 14c-.2-.6-.3-1.3-.3-2s.1-1.4.3-2V7.4H3.1a10 10 0 0 0 0 9.2z"/>'
          '<path fill="#EA4335" d="M12 5.9c1.5 0 2.8.5 3.8 1.5l2.9-2.9A10 10 0 0 0 3.1 7.4L6.4 10c.8-2.4 3-4.1 5.6-4.1z"/></svg>')
MSFT = ('<svg class="i" viewBox="0 0 24 24" aria-hidden="true" style="stroke:none"><path fill="#F25022" d="M3 3h8.5v8.5H3z"/><path fill="#7FBA00" d="M12.5 3H21v8.5h-8.5z"/>'
        '<path fill="#00A4EF" d="M3 12.5h8.5V21H3z"/><path fill="#FFB900" d="M12.5 12.5H21V21h-8.5z"/></svg>')

def page(body, css_extra="", cls=""):
    from css_lp import CSS
    return (f'<!doctype html><html lang="nl"><head><meta charset="utf-8">'
            f'<meta name="viewport" content="width=device-width">'
            f'<style>{CSS}{css_extra}</style></head><body class="{cls}">{body}</body></html>')

def vb(txt="Voorbeelddata"):
    return f'<span class="vb">{txt}</span>'
