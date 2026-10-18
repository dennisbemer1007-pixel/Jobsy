def ic(p,w=1.8): return f'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="{w}" stroke-linecap="round" stroke-linejoin="round">{p}</svg>'
AI={"reis":ic('<circle cx="12" cy="12" r="9"/><path d="m15.5 8.5-2 5-5 2 2-5 5-2z"/>'),"pas":ic('<circle cx="12" cy="8" r="4"/><path d="M4 21c1.5-4 4.5-6 8-6s6.5 2 8 6"/>'),
"zoek":ic('<circle cx="11" cy="11" r="6"/><path d="m20 20-4.5-4.5"/>'),"soll":ic('<rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 4V3h6v1M9 11l2 2 4-4"/>'),
"car":ic('<circle cx="12" cy="12" r="3.5"/><path d="M12 3v2M12 19v2M3 12h2M19 12h2M5.6 5.6 7 7M17 17l1.4 1.4M5.6 18.4 7 17M17 7l1.4-1.4"/>'),
"bell":ic('<path d="M6 16V11a6 6 0 0 1 12 0v5l2 2H4l2-2z"/><path d="M10 21h4"/>'),"user":ic('<circle cx="12" cy="8" r="4"/><path d="M4 21c1.5-4 4.5-6 8-6s6.5 2 8 6"/>'),
"shield":ic('<path d="M12 3 5 6v5c0 4.5 3 8 7 10 4-2 7-5.5 7-10V6l-7-3z"/>'),"leaf":ic('<path d="M5 19c0-8 6-14 14-14 0 8-6 14-14 14z"/><path d="M5 19 13 11"/>'),
"x":ic('<path d="M6 6l12 12M18 6 6 18"/>',2),"pin":ic('<path d="M12 21s-6-5.5-6-11a6 6 0 0 1 12 0c0 5.5-6 11-6 11z"/><circle cx="12" cy="10" r="2"/>'),
"filter":ic('<path d="M4 6h16M7 12h10M10 18h4"/>'),"loc":ic('<circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3"/><circle cx="12" cy="12" r="7"/>'),
"heart":ic('<path d="M12 20s-7-4.5-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.5-7 10-7 10z"/>'),"bike":ic('<circle cx="6" cy="16" r="3.5"/><circle cx="18" cy="16" r="3.5"/><path d="M6 16l4-7h5l3 7M9 6h3"/>'),
"star":ic('<path d="m12 4 2.4 5 5.6.6-4.2 3.8 1.2 5.6L12 16.2 7 19l1.2-5.6L4 9.6 9.6 9 12 4z"/>'),"brief":ic('<rect x="3" y="7" width="18" height="13" rx="2"/><path d="M9 7V5h6v2"/>'),
"home":ic('<path d="M4 11 12 4l8 7v9H4z"/><path d="M10 20v-5h4v5"/>'),"tests":ic('<rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 9h6M9 13h6M9 17h3"/>'),
"check":ic('<path d="m5 12 4 4 10-10"/>',2.2),"ladder":ic('<path d="M8 3v18M16 3v18M8 8h8M8 13h8M8 18h8"/>'),
"target":ic('<circle cx="12" cy="12" r="8"/><circle cx="12" cy="12" r="4"/><circle cx="12" cy="12" r="1"/>'),"cloud":ic('<path d="M7 18a4 4 0 0 1 0-8 5 5 0 0 1 9.6-1.5A4 4 0 1 1 17 18z"/>')}
APPCSS="""
.ah{position:relative;z-index:5;display:flex;align-items:center;gap:22px;padding:10px 28px;background:#fbf6ef;border-bottom:1px solid var(--border)}
.ah .tag{color:#34465c;font-weight:600;font-size:.88rem;padding-left:6px}
.ah .anav{display:flex;gap:4px;margin-left:24px}.ah .anav span{padding:7px 12px;border-radius:10px;font-weight:600;font-size:.92rem;color:#34465c}.ah .anav span.on{background:#efe6da;color:var(--brand)}
.ah .ar{margin-left:auto;display:flex;gap:12px;align-items:center}
.cb{width:36px;height:36px;border-radius:50%;display:grid;place-items:center;border:1px solid var(--border);background:#fff;color:var(--brand)}.cb svg{width:18px;height:18px}
.lng{display:flex;gap:6px;align-items:center;font-weight:650;font-size:.85rem;color:var(--brand);border:1px solid var(--border);background:#fff;border-radius:10px;padding:7px 10px}
.flag{width:18px;height:12px;border-radius:2px;background:linear-gradient(#ae1c28 33%,#fff 33% 66%,#21468b 66%)}
.who{display:flex;gap:10px;align-items:center;font-weight:650;color:var(--brand);font-size:.92rem}.av{width:34px;height:34px;border-radius:50%;background:var(--soft);display:grid;place-items:center;font-weight:700;font-size:.85rem}
.tabbar{display:none}
.lf{display:flex;justify-content:space-between;gap:12px;flex-wrap:wrap;padding:18px 28px;border-top:1px solid var(--border);font-size:.82rem;color:var(--muted)}.lf .ll{display:flex;flex-wrap:wrap;gap:6px 14px}.lf .ll span{text-decoration:underline;text-underline-offset:3px}
@media(max-width:600px){
 .ah{padding:8px 14px;gap:10px}.ah .tag,.ah .anav,.who b{display:none}.ah .ar{gap:8px}.cb{width:34px;height:34px}.lng{padding:6px 8px}
 .tabbar{display:grid;grid-template-columns:repeat(5,1fr);background:#fffcfa;border-top:1px solid var(--border);padding:8px 4px 14px}
 .tabbar span{display:flex;flex-direction:column;align-items:center;gap:3px;font-size:.7rem;font-weight:600;color:var(--muted)}.tabbar span svg{width:22px;height:22px}.tabbar span.on{color:var(--brand)}
 .tabbar span.on i{background:#efe6da}.tabbar i{display:grid;place-items:center;width:44px;height:28px;border-radius:99px}
 .lf{padding:16px}
}"""
def header(active):
  nav="".join(f'<span class="{"on" if n==active else ""}">{n}</span>' for n in ["Reis","Paspoort","Zoeken","Sollicitaties","Carrière"])
  return f'<header class="ah"><div class="brand"><img src="lobsy-256.png" alt="">Lobsy</div><span class="tag">Dichtbij genoeg om het pantser te laten vallen</span><nav class="anav">{nav}</nav><div class="ar"><span class="cb">{AI["bell"]}</span><span class="lng"><i class="flag"></i>NL ▾</span><span class="who"><b>Sanne de Vries</b><span class="av">SV</span></span></div></header>'
def tabbar(active):
  return '<nav class="tabbar">'+"".join(f'<span class="{"on" if n==active else ""}"><i>{AI[k]}</i>{n}</span>' for k,n in [("reis","Reis"),("pas","Paspoort"),("zoek","Zoeken"),("soll","Sollicitaties"),("car","Carrière")])+'</nav>'
LF='<footer class="lf"><div class="ll"><span>Privacyverklaring</span><span>Algemene voorwaarden</span><span>Gebruiksvoorwaarden</span><span>Wie zijn wij</span><span>Toegankelijkheid</span><span>Bronnen</span></div><span>© 2026 Lobsy</span></footer>'
