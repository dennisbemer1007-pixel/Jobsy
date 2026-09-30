B = r'''
/* wat is lobsy */
.steps{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:var(--space-4);margin-top:36px}
.step{padding:24px}
.step .n{display:flex;align-items:center;gap:12px}
.ico{width:44px;height:44px;border-radius:var(--radius-sm);background:var(--accent-soft);color:var(--brand);display:grid;place-items:center;flex:none}
.ico .i{width:22px;height:22px}
.step .n small{font-size:var(--text-xs);font-weight:600;color:var(--muted)}
.step h3{font-size:var(--text-lg);font-weight:600;margin-top:16px}
.step p{color:var(--muted);margin-top:6px;font-size:var(--text-md)}
.step .tag{margin-top:14px;display:flex;gap:6px;flex-wrap:wrap}
/* kreeft */
.kreeft{background:var(--pearl-mid)}
.kgrid{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1.25fr);gap:var(--space-5);margin-top:36px;align-items:stretch}
.story{background:var(--brand);color:var(--surface);border-radius:var(--radius);padding:32px;position:relative;overflow:hidden;min-height:420px}
.story h3{font-size:var(--text-2xl);font-weight:600;line-height:1.25;max-width:330px}
.story p{color:var(--on-dark);margin-top:14px;font-size:var(--text-md);max-width:340px}
.story .molt{position:absolute;right:-10px;bottom:-6px;width:250px;height:250px}
.story .molt img{position:absolute;width:150px}
.story .molt .old{left:10px;top:40px;filter:brightness(0) invert(1);opacity:.14;transform:rotate(-18deg)}
.story .molt .new{left:92px;top:70px}
.story .q{margin-top:22px;display:flex;align-items:center;gap:10px;font-weight:600}
.story .q::before{content:"";width:24px;border-top:2px solid var(--coral)}
.meta{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:var(--space-3)}
.mt{padding:20px;display:flex;gap:14px}
.mt h4{font-size:var(--text-md);font-weight:600}
.mt p{color:var(--muted);font-size:var(--text-sm);margin-top:2px}
.mt .lnk{margin-top:8px;font-size:var(--text-xs);font-weight:600;color:var(--brand);display:inline-flex;align-items:center;gap:6px}
.mt .lnk .i{width:14px;height:14px}
/* wat je krijgt */
.shead{display:flex;align-items:flex-end;gap:var(--space-4)}.shead .sp{flex:1}
.showmap{margin-top:32px;display:grid;grid-template-columns:340px minmax(0,1fr);overflow:hidden;box-shadow:var(--shadow-lg);border:0}
.ml{padding:22px;border-inline-end:1px solid var(--border)}
.ml h3{font-size:var(--text-lg);font-weight:600;display:flex;align-items:center;gap:8px}
.seg{display:inline-flex;border:1px solid var(--border);border-radius:var(--radius-sm);padding:2px;gap:2px;margin-top:14px}
.seg span{height:32px;padding:0 10px;display:inline-flex;align-items:center;gap:6px;border-radius:6px;color:var(--muted);font-weight:600;font-size:var(--text-xs)}
.seg span.on{background:var(--accent-soft);color:var(--brand)}.seg .i{width:15px;height:15px}
.job{display:flex;gap:12px;padding:12px 0;border-bottom:1px solid color-mix(in srgb,var(--border) 60%,transparent)}
.job:last-child{border-bottom:0}
.job .lg{width:40px;height:40px;border-radius:var(--radius-sm);background:var(--pearl-mid);display:grid;place-items:center;font-weight:600;font-size:var(--text-sm);color:var(--brand);flex:none}
.job h5{font-size:var(--text-sm);font-weight:600}.job small{display:block;color:var(--muted);font-size:var(--text-xs)}
.job .tt{margin-inline-start:auto;text-align:end;font-size:var(--text-xs);color:var(--muted);white-space:nowrap}
.job .tt b{display:block;color:var(--text);font-size:var(--text-sm)}
.mapc{position:relative;min-height:440px;background:var(--pearl)}
.mapc svg{position:absolute;inset:0;width:100%;height:100%}
.homelob{position:absolute;width:84px;left:328px;top:170px;filter:drop-shadow(0 6px 10px rgba(15,45,92,.25))}
.maplbl{position:absolute;top:16px;inset-inline-start:16px;display:flex;gap:8px}
.ringl{position:absolute;font-size:var(--text-xs);font-weight:600;color:var(--brand);background:var(--surface);border-radius:var(--radius-pill);padding:2px 8px;box-shadow:var(--shadow)}
.four{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:var(--space-4);margin-top:var(--space-4)}
.fc{padding:20px;display:flex;flex-direction:column;gap:12px}
.fc .top{display:flex;align-items:center;gap:10px}.fc .top .sp{flex:1}
.fc h4{font-size:var(--text-md);font-weight:600}
.fc p{font-size:var(--text-sm);color:var(--muted)}
.fc .demo{background:var(--pearl);border-radius:var(--radius-sm);padding:12px;flex:1}
.tabs{display:flex;gap:4px;flex-wrap:wrap}
.tabs span{font-size:var(--text-xs);font-weight:600;padding:3px 8px;border-radius:var(--radius-pill);background:var(--surface);color:var(--muted);border:1px solid var(--border)}
.tabs span.on{background:var(--brand);border-color:var(--brand);color:var(--surface)}
.jr{display:flex;flex-direction:column;gap:6px}
.jr div{display:flex;align-items:center;gap:8px;font-size:var(--text-xs);color:var(--muted)}
.jr i{width:22px;height:22px;border-radius:50%;display:grid;place-items:center;font-style:normal;font-weight:600;background:var(--surface);border:1px solid var(--border);color:var(--muted);flex:none}
.jr .done i{background:var(--success);border-color:var(--success);color:var(--surface)}
.jr .cur{color:var(--text);font-weight:600}.jr .cur i{border-color:var(--brand);color:var(--brand)}
.swipe{position:relative;height:150px}
.swipe .sc{position:absolute;inset:8px 18px 0 18px;background:var(--surface);border:1px solid var(--border);border-radius:var(--radius);transform:rotate(4deg)}
.swipe .sc.front{inset:0 10px 8px 10px;transform:rotate(-2deg);box-shadow:var(--shadow-lg);border:0;padding:12px}
.swipe .pc{font-size:var(--text-xl);font-weight:600;color:var(--brand)}
.sw-act{display:flex;justify-content:center;gap:14px}
.sw-act span{width:40px;height:40px;border-radius:50%;display:grid;place-items:center;border:1px solid var(--border);background:var(--surface);color:var(--muted)}
.sw-act span.y{background:var(--brand);border-color:var(--brand);color:var(--surface)}
.rep{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius-sm);padding:12px;transform:rotate(-1.5deg);box-shadow:var(--shadow)}
.rep h6{font-size:var(--text-sm);font-weight:600}.rep .ln{height:6px;border-radius:3px;background:var(--pearl-mid);margin-top:7px}
.price{display:flex;align-items:baseline;gap:8px}.price b{font-size:var(--text-xl);white-space:nowrap}
/* voor wie */
.who{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:var(--space-4);margin-top:36px}
.wc{padding:24px;display:flex;flex-direction:column;gap:10px}
.wc h3{font-size:var(--text-lg);font-weight:600;margin-top:6px}
.wc p{color:var(--muted);font-size:var(--text-sm);flex:1}
.wc ul{list-style:none;display:flex;flex-direction:column;gap:6px;font-size:var(--text-sm)}
.wc li{display:flex;gap:8px;align-items:flex-start}.wc li .i{width:16px;height:16px;color:var(--success);margin-top:2px}
.wc .go{margin-top:8px}
.wc.me{border:2px solid var(--brand)}
/* trust */
.trust{background:var(--surface)}
.tgrid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:var(--space-5);margin-top:32px}
.tg h4{font-size:var(--text-md);font-weight:600;margin-top:14px}.tg p{color:var(--muted);font-size:var(--text-sm);margin-top:4px}
/* faq */
.faqg{display:grid;grid-template-columns:minmax(0,.8fr) minmax(0,1.2fr);gap:var(--space-7)}
.faq{display:flex;flex-direction:column}
.fq{border-bottom:1px solid var(--border);padding:18px 4px}
.fq h4{display:flex;align-items:center;gap:12px;font-size:var(--text-md);font-weight:600}.fq h4 .i{margin-inline-start:auto;color:var(--muted)}
.fq p{color:var(--muted);margin-top:8px;max-width:620px}
.fq.open h4 .i{transform:rotate(180deg)}
.help{margin-top:24px;padding:20px;display:flex;gap:14px;align-items:center}
.help img{width:64px;height:64px}
/* final cta + footer */
.final{background:var(--brand);color:var(--surface);border-radius:var(--radius);padding:40px;display:flex;align-items:center;gap:32px;position:relative;overflow:hidden}
.final h2{font-size:var(--text-2xl);font-weight:600}.final p{color:var(--on-dark);margin-top:6px}
.final .sp{flex:1}.final img{width:110px}
.ftr{background:var(--brand-deep);color:var(--on-dark);padding:48px 0 28px;font-size:var(--text-sm)}
.fg{display:grid;grid-template-columns:1.4fr repeat(4,1fr);gap:var(--space-6)}
.fg h5{color:var(--surface);font-size:var(--text-sm);font-weight:600;margin-bottom:12px}
.fg a{display:block;padding:4px 0}.fg a.brand{display:flex;padding:0}
.fb{display:flex;align-items:center;gap:16px;margin-top:36px;padding-top:20px;border-top:1px solid color-mix(in srgb,var(--surface) 14%,transparent);font-size:var(--text-xs)}
.fb .sp{flex:1}
'''
