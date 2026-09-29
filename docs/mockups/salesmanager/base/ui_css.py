CSS = r'''
:root{--pearl:#f7f4f0;--pearl-mid:#efe9e3;--pearl-cool:#e8eef5;--bg:#f5f2ee;--surface:#fffcfa;--text:#122033;--muted:#5a6a7d;--border:#ddd5cc;
--brand:#0f2d5c;--brand-deep:#0a2044;--accent-soft:#e7eef7;--accent-hover:#163a6b;--coral:#f54a1b;--danger:#9b1c1c;--danger-soft:#fef2f2;
--success:#15803d;--success-soft:#ecfdf3;--warn:#a65b00;--warn-soft:#fff8e7;--gold:#c9a227;--gold-soft:#faf6e8;--hover:#f0ebe6;
--shadow:0 1px 2px rgba(15,45,92,.06);--shadow-lg:0 12px 32px rgba(15,45,92,.12);--radius:12px;--radius-sm:8px;--radius-pill:999px;
--space-1:.25rem;--space-2:.5rem;--space-3:.75rem;--space-4:1rem;--space-5:1.5rem;--space-6:2rem;
--text-xs:.75rem;--text-sm:.875rem;--text-md:1rem;--text-lg:1.125rem;--text-xl:1.375rem;--text-2xl:1.75rem;
--font:"Inter","Segoe UI","Helvetica Neue",Arial,sans-serif}
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:var(--font);color:var(--text);background:var(--bg);font-size:var(--text-sm);line-height:1.45;-webkit-font-smoothing:antialiased}
.i{width:16px;height:16px;fill:none;stroke:currentColor;stroke-width:1.8;stroke-linecap:round;stroke-linejoin:round;flex:none}
b,strong,th{font-weight:600}
a{color:inherit;text-decoration:none}
.muted{color:var(--muted)}.sm{font-size:var(--text-xs)}.mono{font-family:ui-monospace,Menlo,monospace;font-size:var(--text-xs);color:var(--muted)}
.top{position:fixed;inset:0 0 auto 0;height:56px;background:var(--brand-deep);color:var(--surface);display:flex;align-items:center;gap:var(--space-4);padding:0 var(--space-5) 0 var(--space-4);z-index:40}
.brandm{display:flex;align-items:center;gap:10px;width:216px}
.brandm img{width:26px;height:26px;border-radius:6px;background:var(--surface);padding:2px}
.brandm b{font-size:var(--text-md)}.brandm span{color:color-mix(in srgb,var(--surface) 70%,transparent)}
.env{display:inline-flex;align-items:center;gap:6px;height:24px;padding:0 10px;border-radius:var(--radius-pill);font-size:var(--text-xs);font-weight:600;background:var(--warn-soft);color:var(--warn);white-space:nowrap}
.env i{width:7px;height:7px;border-radius:50%;background:var(--warn)}
.env.prod{background:var(--danger-soft);color:var(--danger)}.env.prod i{background:var(--danger)}
.gs{flex:1;max-width:560px;margin-inline-start:var(--space-4);height:36px;border-radius:var(--radius-sm);background:color-mix(in srgb,var(--surface) 12%,transparent);display:flex;align-items:center;gap:10px;padding:0 12px;color:color-mix(in srgb,var(--surface) 72%,transparent)}
.gs span{flex:1}.kbd{flex:none!important;font-size:var(--text-xs);border:1px solid color-mix(in srgb,var(--surface) 30%,transparent);border-radius:5px;padding:1px 6px}
.tr{margin-inline-start:auto;display:flex;align-items:center;gap:var(--space-2)}
.tb{width:36px;height:36px;border-radius:var(--radius-sm);display:grid;place-items:center;position:relative;color:color-mix(in srgb,var(--surface) 85%,transparent)}
.tb .dot{position:absolute;top:8px;inset-inline-end:8px;width:7px;height:7px;border-radius:50%;background:var(--coral);border:2px solid var(--brand-deep)}
.me{display:flex;align-items:center;gap:10px;padding-inline-start:var(--space-3);margin-inline-start:var(--space-2);border-inline-start:1px solid color-mix(in srgb,var(--surface) 18%,transparent)}
.av{width:32px;height:32px;border-radius:50%;display:grid;place-items:center;font-size:var(--text-xs);font-weight:600;background:var(--accent-soft);color:var(--brand);flex:none}
.me small{display:block;font-size:var(--text-xs);color:color-mix(in srgb,var(--surface) 70%,transparent)}
.side{position:fixed;top:56px;bottom:0;inset-inline-start:0;width:248px;background:var(--surface);border-inline-end:1px solid var(--border);padding:var(--space-3) var(--space-3) var(--space-4);overflow:hidden}
.grp{margin-bottom:4px}
.grp h4{font-size:var(--text-xs);letter-spacing:.03em;text-transform:uppercase;color:var(--muted);font-weight:600;padding:8px 10px 3px}
.grp.col h4{display:flex;align-items:center;justify-content:space-between;padding:6px 10px;text-transform:none;letter-spacing:0;font-size:var(--text-sm);color:var(--text);font-weight:400}
.grp.col h4 .i{color:var(--muted);width:14px;height:14px}
.it{white-space:nowrap;display:flex;align-items:center;gap:10px;height:30px;padding:0 10px;border-radius:var(--radius-sm);color:var(--text);position:relative}
.it .i{color:var(--muted)}
.it.on{background:var(--accent-soft);color:var(--brand);font-weight:600}.it.on .i{color:var(--brand)}
.it.on::before{content:"";position:absolute;inset-inline-start:-12px;top:6px;bottom:6px;width:3px;border-radius:0 3px 3px 0;background:var(--brand)}
.cnt{margin-inline-start:auto;font-size:var(--text-xs);font-weight:600;min-width:20px;height:18px;padding:0 6px;border-radius:var(--radius-pill);background:var(--pearl-mid);color:var(--text);display:inline-grid;place-items:center}
.it.on .cnt{background:var(--surface)}
.main{margin-inline-start:248px;padding:calc(56px + var(--space-4)) var(--space-6) var(--space-5)}
.crumb{display:flex;align-items:center;gap:6px;color:var(--muted);font-size:var(--text-xs);margin-bottom:4px}.crumb .i{width:12px;height:12px}.crumb b{color:var(--text)}
.ph{display:flex;align-items:flex-end;gap:var(--space-4);margin-bottom:var(--space-4)}
.ph h1{font-size:var(--text-2xl);font-weight:700;letter-spacing:-.01em;line-height:1.2}
.ph p{color:var(--muted);margin-top:2px}
.ph .act{margin-inline-start:auto;display:flex;gap:var(--space-2);align-items:center}
.btn{height:34px;display:inline-flex;align-items:center;gap:7px;padding:0 12px;border-radius:var(--radius-sm);border:1px solid var(--border);background:var(--surface);color:var(--text);font-weight:600;font-size:var(--text-sm);white-space:nowrap}
.btn.pri{background:var(--brand);border-color:var(--brand);color:var(--surface)}
.btn.dng{color:var(--danger);border-color:color-mix(in srgb,var(--danger) 45%,var(--border))}
.btn.dngp{background:var(--danger);border-color:var(--danger);color:var(--surface)}
.btn.ghost{border-color:transparent;background:transparent;color:var(--brand)}
.btn.sm{height:28px;padding:0 9px;font-size:var(--text-xs)}
.seg{display:inline-flex;border:1px solid var(--border);border-radius:var(--radius-sm);background:var(--surface);padding:2px;gap:2px}
.seg span{height:28px;padding:0 10px;display:grid;place-items:center;border-radius:6px;color:var(--muted);font-weight:600;font-size:var(--text-xs)}
.seg span.on{background:var(--accent-soft);color:var(--brand)}
.card{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius)}
.ch{display:flex;align-items:center;gap:var(--space-2);padding:12px 16px 10px}
.ch h2{font-size:var(--text-md);font-weight:600}.ch .sp{flex:1}.ch a{color:var(--brand);font-weight:600;font-size:var(--text-xs);display:inline-flex;align-items:center;gap:3px;white-space:nowrap}.ch a .i{width:13px;height:13px}
.kpis{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:var(--space-3);margin-bottom:var(--space-4)}
.kpi{padding:12px 16px}.kpi small{color:var(--muted);font-size:var(--text-xs);font-weight:600;display:flex;align-items:center;gap:6px}
.kpi .v{font-size:var(--text-xl);font-weight:600;margin-top:4px;font-variant-numeric:tabular-nums;letter-spacing:-.01em;white-space:nowrap}
.kpi .dl{display:flex;align-items:center;gap:6px;margin-top:2px;font-size:var(--text-xs);color:var(--muted);white-space:nowrap}
.up{color:var(--success);font-weight:600}.down{color:var(--danger);font-weight:600}
.spark{margin-inline-start:auto;width:64px;height:22px;flex:none}.spark polyline{fill:none;stroke:var(--brand);stroke-width:1.6;opacity:.5}
.pill{display:inline-flex;align-items:center;gap:5px;height:22px;padding:0 8px;border-radius:var(--radius-pill);font-size:var(--text-xs);font-weight:600;background:var(--pearl-mid);color:var(--text);white-space:nowrap}
.pill .i{width:12px;height:12px}
.pill.ok{background:var(--success-soft);color:var(--success)}.pill.wn{background:var(--warn-soft);color:var(--warn)}.pill.bad{background:var(--danger-soft);color:var(--danger)}.pill.info{background:var(--accent-soft);color:var(--brand)}
.pill.line{background:transparent;border:1px solid var(--border);color:var(--muted)}
.dotst{display:inline-flex;align-items:center;gap:6px;white-space:nowrap}.dotst::before{content:"";width:8px;height:8px;border-radius:50%;background:var(--success);flex:none}
.dotst.wn::before{background:var(--warn)}.dotst.bad::before{background:var(--danger)}.dotst.off::before{background:var(--border)}
table{width:100%;border-collapse:collapse;font-variant-numeric:tabular-nums}
th{font-size:var(--text-xs);color:var(--muted);text-align:start;padding:8px 10px;border-bottom:1px solid var(--border);white-space:nowrap;background:var(--surface)}
td{padding:0 10px;height:44px;border-bottom:1px solid color-mix(in srgb,var(--border) 60%,transparent);white-space:nowrap}
tr:last-child td{border-bottom:0}
td.num,th.num{text-align:end}
tr.sel td{background:var(--accent-soft)}
tr.hl td{background:color-mix(in srgb,var(--pearl) 70%,var(--surface))}
.cb{width:16px;height:16px;border:1.5px solid var(--border);border-radius:4px;display:inline-grid;place-items:center;background:var(--surface);vertical-align:middle}
.cb.on{background:var(--brand);border-color:var(--brand);color:var(--surface)}.cb.on .i{width:12px;height:12px;stroke-width:2.6}
.cb.mid{background:var(--surface);border-color:var(--surface)}.cb.mid::after{content:"";width:8px;height:2px;background:var(--brand)}
.who{display:flex;align-items:center;gap:10px}.who .av{width:28px;height:28px}.who small{display:block;color:var(--muted);font-size:var(--text-xs)}
.ra{color:var(--muted);width:28px;height:28px;display:inline-grid;place-items:center;border-radius:6px}
.fbar{display:flex;align-items:center;gap:var(--space-2);padding:10px 12px;border-bottom:1px solid var(--border)}
.inp{height:32px;border:1px solid var(--border);border-radius:var(--radius-sm);background:var(--surface);display:flex;align-items:center;gap:8px;padding:0 10px;color:var(--muted);white-space:nowrap}
.inp.w{width:260px}
.dd{height:32px;border:1px solid var(--border);border-radius:var(--radius-sm);background:var(--surface);display:inline-flex;align-items:center;gap:6px;padding:0 10px;font-weight:600;font-size:var(--text-xs);color:var(--text);white-space:nowrap}
.dd em{font-style:normal;color:var(--muted);font-weight:400}.dd .i{width:13px;height:13px;color:var(--muted)}
.dd.on{border-color:var(--brand);background:var(--accent-soft);color:var(--brand)}
.bulk{display:flex;align-items:center;gap:var(--space-2);padding:7px 12px;background:var(--brand);color:var(--surface)}
.bulk .btn{background:transparent;border-color:color-mix(in srgb,var(--surface) 35%,transparent);color:var(--surface);height:28px;font-size:var(--text-xs)}
.pag{display:flex;align-items:center;gap:var(--space-2);padding:8px 12px;border-top:1px solid var(--border);color:var(--muted);font-size:var(--text-xs)}
.pag .sp{flex:1}
.tabs{display:flex;gap:var(--space-5);border-bottom:1px solid var(--border);margin-bottom:var(--space-4)}
.tabs span{padding:0 2px 10px;color:var(--muted);font-weight:600;display:flex;align-items:center;gap:6px;position:relative}
.tabs span.on{color:var(--brand)}.tabs span.on::after{content:"";position:absolute;inset-inline:0;bottom:-1px;height:2px;background:var(--brand);border-radius:2px}
.tabs .cnt{margin:0;font-weight:600}
.note{display:flex;align-items:center;gap:8px;color:var(--muted);font-size:var(--text-xs);padding:8px 12px;border-bottom:1px solid var(--border);background:var(--pearl)}
.note .i{width:14px;height:14px;color:var(--brand)}.note a{color:var(--brand);font-weight:600}
.row{display:grid;gap:var(--space-4)}
.list li{list-style:none;display:flex;align-items:center;gap:10px;padding:9px 16px;border-top:1px solid color-mix(in srgb,var(--border) 60%,transparent)}
.list li .sp{flex:1}
.ico{width:30px;height:30px;border-radius:8px;display:grid;place-items:center;background:var(--pearl-mid);color:var(--brand);flex:none}
.ico.wn{background:var(--warn-soft);color:var(--warn)}.ico.bad{background:var(--danger-soft);color:var(--danger)}.ico.ok{background:var(--success-soft);color:var(--success)}
.scrim{position:fixed;inset:56px 0 0 248px;background:color-mix(in srgb,var(--brand-deep) 22%,transparent);z-index:60}
.drawer{position:fixed;top:56px;bottom:0;inset-inline-end:0;width:460px;background:var(--surface);box-shadow:var(--shadow-lg);z-index:61;display:flex;flex-direction:column}
.dh{padding:16px 20px 0;display:flex;gap:12px;align-items:flex-start}
.dh .av{width:44px;height:44px;font-size:var(--text-sm)}
.dh h2{font-size:var(--text-lg);font-weight:600}.dh .x{margin-inline-start:auto;color:var(--muted)}
.dtabs{display:flex;gap:var(--space-4);padding:0 20px;border-bottom:1px solid var(--border);margin-top:14px}
.dtabs span{padding-bottom:9px;color:var(--muted);font-weight:600;position:relative}.dtabs span.on{color:var(--brand)}
.dtabs span.on::after{content:"";position:absolute;inset-inline:0;bottom:-1px;height:2px;background:var(--brand)}
.db{padding:16px 20px;overflow:hidden;flex:1;display:flex;flex-direction:column;gap:16px}
.sec h3{font-size:var(--text-sm);font-weight:600;margin-bottom:8px;display:flex;align-items:center;gap:8px}
.box{border:1px solid var(--border);border-radius:var(--radius-sm);padding:12px 14px}
.kv{display:grid;grid-template-columns:120px 1fr;row-gap:7px;column-gap:10px}.kv dt{color:var(--muted)}
.df{border-top:1px solid var(--border);padding:12px 20px;display:flex;gap:var(--space-2);align-items:center}
.warnbox{display:flex;gap:10px;align-items:flex-start;background:var(--warn-soft);border-radius:var(--radius-sm);padding:10px 12px;font-size:var(--text-xs)}
.warnbox .i{color:var(--warn);margin-top:1px}
.dangerbox{display:flex;gap:10px;align-items:flex-start;background:var(--danger-soft);border-radius:var(--radius-sm);padding:10px 12px;font-size:var(--text-xs)}
.dangerbox .i{color:var(--danger);margin-top:1px}
.sess{list-style:none}.sess li{display:flex;align-items:center;gap:10px;padding:8px 0;border-top:1px solid color-mix(in srgb,var(--border) 60%,transparent)}.sess li:first-child{border-top:0}.sess .sp{flex:1}
.set{display:grid;grid-template-columns:1fr auto;gap:3px 24px;padding:13px 16px;border-top:1px solid color-mix(in srgb,var(--border) 60%,transparent)}
.set h3{font-size:var(--text-sm);font-weight:600;display:flex;align-items:center;gap:8px}
.set p{color:var(--muted);font-size:var(--text-xs);grid-column:1}
.set .meta{font-size:var(--text-xs);color:var(--muted);grid-column:1;display:flex;gap:6px;align-items:center}.set .meta .i{width:12px;height:12px}
.set .ctl{grid-row:1/span 3;grid-column:2;align-self:start}
.set .imp{grid-column:1/-1;margin-top:6px}
.set.chg{background:color-mix(in srgb,var(--accent-soft) 60%,var(--surface))}
.sw{width:40px;height:22px;border-radius:11px;background:var(--border);position:relative;display:inline-block}
.sw::after{content:"";position:absolute;top:3px;inset-inline-start:3px;width:16px;height:16px;border-radius:50%;background:var(--surface);box-shadow:var(--shadow)}
.sw.on{background:var(--brand)}.sw.on::after{inset-inline-start:21px}
.sw.dis{opacity:.45}
.field{height:32px;border:1px solid var(--border);border-radius:var(--radius-sm);display:inline-flex;align-items:center;padding:0 10px;gap:8px;background:var(--surface);font-variant-numeric:tabular-nums;white-space:nowrap}
.savebar{position:fixed;bottom:18px;inset-inline-start:calc(248px + 32px);width:740px;background:var(--brand-deep);color:var(--surface);border-radius:var(--radius);box-shadow:var(--shadow-lg);display:flex;align-items:center;gap:12px;padding:10px 12px 10px 16px;z-index:50}
.savebar .btn{height:32px}.savebar .btn.ghost{color:var(--surface)}
.savebar .btn.w{background:var(--surface);color:var(--brand);border-color:var(--surface)}
.chgl{display:flex;align-items:center;gap:8px}.chgl s{opacity:.7}
.demo.l{inset-inline-end:auto;inset-inline-start:16px}
.demo{position:fixed;bottom:14px;inset-inline-end:16px;font-size:var(--text-xs);font-weight:600;color:var(--warn);background:var(--warn-soft);border:1px solid color-mix(in srgb,var(--warn) 30%,transparent);border-radius:var(--radius-pill);padding:3px 10px;z-index:70}
.hist li{list-style:none;padding:10px 16px;border-top:1px solid color-mix(in srgb,var(--border) 60%,transparent);font-size:var(--text-xs)}
.hist li b{font-size:var(--text-sm)}.hist li p{color:var(--muted);margin-top:2px}
.indent{display:inline-block;width:14px;height:14px;border-inline-start:1.5px solid var(--border);border-bottom:1.5px solid var(--border);border-end-start-radius:5px;margin-inline:8px 8px;transform:translateY(-4px)}
.caret{color:var(--muted);display:inline-grid;place-items:center;width:18px;margin-inline-end:4px;vertical-align:middle}
.caret .i{width:14px;height:14px}
body.m{background:var(--bg)}
.mtop{position:sticky;top:0;height:56px;background:var(--brand-deep);color:var(--surface);display:flex;align-items:center;gap:8px;padding:0 10px 0 4px;z-index:40}
.mtop .tb{width:44px;height:44px}
.mtop img{width:24px;height:24px;border-radius:6px;background:var(--surface);padding:2px}
.mmain{padding:14px 16px 24px}
.mmain h1{font-size:var(--text-xl);font-weight:700}
.mkpis{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:12px 0 16px}
.mkpis .kpi{padding:10px 12px}.mkpis .v{font-size:var(--text-lg)}
.mlist li{list-style:none;display:flex;gap:10px;align-items:center;padding:10px 14px;border-top:1px solid color-mix(in srgb,var(--border) 60%,transparent);min-height:56px}
.mlist li:first-child{border-top:0}.mlist li .sp{flex:1}.mlist small{display:block;color:var(--muted);font-size:var(--text-xs)}
.mlist li>.i{color:var(--muted)}
.sheet{position:fixed;inset:auto 0 0 0;background:var(--surface);border-radius:16px 16px 0 0;box-shadow:var(--shadow-lg);padding:8px 16px 20px;z-index:81}
.sheet .grab{width:40px;height:4px;border-radius:2px;background:var(--border);margin:0 auto 12px}
.mscrim{position:fixed;inset:0;background:color-mix(in srgb,var(--brand-deep) 45%,transparent);z-index:80}
.otp{display:flex;gap:8px}.otp span{width:44px;height:48px;border:1px solid var(--border);border-radius:var(--radius-sm);display:grid;place-items:center;font-size:var(--text-lg);font-weight:600}
.otp span.f{border-color:var(--brand);box-shadow:0 0 0 2px var(--accent-soft)}
.ta{border:1px solid var(--border);border-radius:var(--radius-sm);padding:10px 12px;min-height:60px}
.lbl{font-size:var(--text-xs);font-weight:600;margin:12px 0 6px;display:block}
'''
