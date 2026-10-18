# Gedeelde stijl voor de kandidaatpagina's (zelfde tokens als Kompas/banenkaart)
AI.update({"share":ic('<circle cx="18" cy="5" r="2.5"/><circle cx="6" cy="12" r="2.5"/><circle cx="18" cy="19" r="2.5"/><path d="M8.5 13.5 15.5 17.5M15.5 6.5 8.5 10.5"/>'),
"more":ic('<circle cx="5" cy="12" r="1.3"/><circle cx="12" cy="12" r="1.3"/><circle cx="19" cy="12" r="1.3"/>',2),
"clock":ic('<circle cx="12" cy="12" r="8"/><path d="M12 8v4l3 2"/>'),"euro":ic('<path d="M17 7a6 6 0 1 0 0 10M5 10h8M5 14h8"/>'),
"cal":ic('<rect x="4" y="5" width="16" height="15" rx="2"/><path d="M4 10h16M9 3v4M15 3v4"/>'),"cam":ic('<path d="M4 8h3l2-2h6l2 2h3v11H4z"/><circle cx="12" cy="13" r="3.5"/>'),
"mail":ic('<rect x="3" y="5" width="18" height="14" rx="2"/><path d="m4 7 8 6 8-6"/>'),"lock":ic('<rect x="5" y="11" width="14" height="9" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/>'),
"info":ic('<circle cx="12" cy="12" r="9"/><path d="M12 11v5M12 8h.01"/>'),"chev":ic('<path d="m9 6 6 6-6 6"/>',2),"down":ic('<path d="m6 9 6 6 6-6"/>',2),
"spark":ic('<path d="M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M6 18l2.5-2.5M15.5 8.5 18 6"/>'),
"route":ic('<circle cx="6" cy="18" r="2"/><circle cx="18" cy="6" r="2"/><path d="M8 18h6a3 3 0 0 0 0-6h-4a3 3 0 0 1 0-6h6"/>'),
"chat":ic('<path d="M5 18 3 21V6a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2z"/>'),"map":ic('<path d="m3 6 6-2 6 2 6-2v14l-6 2-6-2-6 2z"/><path d="M9 4v14M15 6v14"/>'),
"whats":ic('<path d="M4 20l1.3-4A8 8 0 1 1 8 18.7z"/>'),"doc":ic('<path d="M7 3h7l5 5v13H7z"/><path d="M14 3v5h5"/>'),"gear":ic('<circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3M5 5l2 2M17 17l2 2M5 19l2-2M17 7l2-2"/>'),
"plus":ic('<path d="M12 5v14M5 12h14"/>',2)})
PCSS=APPCSS+"""
.wrap{max-width:1180px;margin:0 auto;padding:24px 28px 44px}
.card{background:var(--surf);border:1px solid var(--border);border-radius:18px}
.band{display:flex;align-items:stretch;justify-content:space-between;gap:24px;background:#fbf2e3;border:1px solid #efe3d1;border-radius:20px;padding:0 0 0 30px;overflow:hidden;min-height:132px}
.band .bt{padding:22px 0;align-self:center}.band h1{font-size:1.65rem}.band p{color:#3d4e63;margin-top:6px;max-width:560px}
.band .sart{width:380px;flex-shrink:0;background:no-repeat center/cover;-webkit-mask-image:linear-gradient(90deg,transparent,#000 24%);mask-image:linear-gradient(90deg,transparent,#000 24%)}
.eyb{display:block;font-size:.72rem;font-weight:700;color:var(--muted);letter-spacing:.05em;text-transform:uppercase}
.eyb.c{color:#b4461f;letter-spacing:.02em;text-transform:none;font-size:.82rem}
.back{display:inline-flex;gap:6px;align-items:center;color:var(--brand);font-weight:600;font-size:.9rem;margin-bottom:14px}
.photo{position:relative;background:linear-gradient(140deg,#efe5d8 0%,#e9e0d3 55%,#e3dbcf 100%);display:grid;place-items:center;color:#a39583;overflow:hidden}
.photo:before{content:"";position:absolute;inset:0;background:radial-gradient(circle at 78% 22%,rgba(255,255,255,.45),transparent 45%),radial-gradient(circle at 15% 90%,rgba(214,196,172,.35),transparent 50%)}
.photo span{position:relative;display:flex;flex-direction:column;align-items:center;gap:6px;font-size:.78rem;font-weight:600}.photo svg{width:26px;height:26px}
.fitp{display:inline-flex;gap:6px;align-items:center;font-size:.82rem;font-weight:700;color:#1f5135;background:#e8f3ec;border:1px solid #cfe6d7;border-radius:99px;padding:3px 10px;white-space:nowrap}.fitp svg{width:14px;height:14px}
.fitp.gate{color:#7a5513;background:#fdf3dc;border-color:#ecd49c}
.chip2{display:inline-flex;align-items:center;gap:6px;font-size:.8rem;font-weight:650;color:var(--brand);background:var(--soft);border-radius:99px;padding:3px 10px;white-space:nowrap}
.st{display:inline-flex;gap:6px;align-items:center;font-size:.78rem;font-weight:700;border-radius:99px;padding:3px 10px;white-space:nowrap;background:#eef2f7;color:#2c4a73}.st:before{content:"";width:7px;height:7px;border-radius:50%;background:currentColor}
.st.inv{background:#fdf0e3;color:#a2501d}.st.ok{background:#e8f3ec;color:#1f5135}.st.no{background:#f1ede8;color:#6b6259}.st.warn{background:#fdf3dc;color:#7a5513}
.segtabs{display:inline-flex;gap:2px;background:var(--surf);border:1px solid var(--border);border-radius:14px;padding:4px}
.segtabs span{display:inline-flex;gap:7px;align-items:center;padding:8px 16px;border-radius:10px;font-weight:650;font-size:.9rem;color:var(--brand)}.segtabs span.on{background:var(--brand);color:#fff}
.segtabs i{font-style:normal;font-size:.72rem;background:#f8ecd9;color:#8f6232;border-radius:99px;padding:1px 7px}
.ibtn2{width:40px;height:40px;border-radius:12px;border:1px solid #cfd8e4;background:#fff;display:grid;place-items:center;color:var(--brand);flex-shrink:0}.ibtn2 svg{width:18px;height:18px}
.btn svg{width:17px;height:17px}
.lk{color:var(--brand);font-weight:650;text-decoration:underline;text-underline-offset:3px}
.muted{color:var(--muted)}
.sw{width:40px;height:23px;border-radius:99px;background:#d9d1c6;position:relative;flex-shrink:0}.sw:after{content:"";position:absolute;top:3px;left:3px;width:17px;height:17px;border-radius:50%;background:#fff;box-shadow:0 1px 2px rgba(0,0,0,.15)}
.sw.on{background:var(--brand)}.sw.on:after{left:20px}
.cbx{width:20px;height:20px;border-radius:6px;border:1.5px solid #9aa6b5;background:#fff;flex-shrink:0;display:grid;place-items:center;color:#fff}.cbx.on{background:var(--brand);border-color:var(--brand)}.cbx svg{width:14px;height:14px}
@media(max-width:600px){.wrap{padding:14px 14px 26px}.band{padding:0;flex-direction:column-reverse;gap:0;min-height:0}.band .sart{width:auto;height:118px;-webkit-mask-image:none;mask-image:none}.band .bt{padding:14px 18px 18px}.band h1{font-size:1.4rem}}
"""
def photo(h,label="Foto van de vacature",cls=""):
  return f'<div class="photo {cls}" style="height:{h}"><span>{AI["cam"]}{label}</span></div>'
