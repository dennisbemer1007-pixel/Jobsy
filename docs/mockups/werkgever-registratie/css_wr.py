"""Werkgever-registratie (wr-*) styles on top of the landing warm pub- theme (css_lp). Only existing tokens + the landing-proposed radii."""
R = r'''
body{background:var(--cream)}
.wh{height:72px;background:var(--cream);position:relative;z-index:5}
.wh .wrap{height:100%;display:flex;align-items:center;gap:16px}
.wh .brand img{background:var(--surface);box-shadow:var(--shadow)}
.wh .r{margin-inline-start:auto;display:flex;align-items:center;gap:10px}
.wh .ctx{font-size:var(--text-sm);color:var(--muted);font-weight:600;padding-inline-start:16px;border-inline-start:1px solid var(--border)}
.wz{display:grid;grid-template-columns:300px minmax(0,1fr);gap:28px;align-items:start;padding-top:8px;padding-bottom:56px}
.guide{position:sticky;top:16px;display:flex;flex-direction:column;gap:16px}
.gart{position:relative;height:210px;background:var(--sun);border-radius:var(--radius-xl);overflow:hidden}
.gart .blob{position:absolute}
.gart img.lob{position:absolute;left:50%;bottom:-8px;width:150px;transform:translateX(-50%) rotate(-6deg);filter:drop-shadow(0 12px 16px color-mix(in srgb,var(--coral) 25%,transparent))}
.gart .wv{position:absolute;right:58px;top:34px;font-size:30px;font-family:"Noto Color Emoji";transform:rotate(14deg)}
.gb{background:var(--surface);border-radius:22px;padding:14px 16px;box-shadow:var(--shadow-soft);font-size:var(--text-sm);line-height:1.5;position:relative}
.gb b{display:block;font-size:var(--text-md);margin-bottom:2px}
.gb::before{content:"";position:absolute;top:-9px;inset-inline-start:40px;border:10px solid transparent;border-top:0;border-bottom-color:var(--surface)}
.stp{list-style:none;background:var(--surface);border-radius:var(--radius-lg);padding:14px;box-shadow:var(--shadow);display:flex;flex-direction:column;gap:2px}
.stp li{display:flex;align-items:center;gap:10px;padding:8px 8px;border-radius:12px;font-size:var(--text-sm);font-weight:600;color:var(--muted)}
.stp li .n{width:26px;height:26px;border-radius:50%;display:grid;place-items:center;font-size:var(--text-xs);background:var(--pearl-mid);color:var(--muted);flex:none}
.stp li .n .i{width:14px;height:14px;stroke-width:2.6}
.stp li.done{color:var(--text)}.stp li.done .n{background:var(--mint);color:var(--success)}
.stp li.on{background:var(--peach);color:var(--text)}.stp li.on .n{background:var(--coral);color:var(--surface)}
.stp li small{display:block;font-weight:400;font-size:var(--text-xs);color:var(--muted)}
.stp li .opt{margin-inline-start:auto;font-size:var(--text-xs);font-weight:600;color:var(--muted)}
.wcard{background:var(--surface);border-radius:var(--radius-xl);box-shadow:var(--shadow-soft);padding:32px 36px}
.wcard h1{font-size:1.875rem;font-weight:700;letter-spacing:-.015em;line-height:1.2;margin-top:12px}
.wcard .lead{font-size:var(--text-md);margin-top:8px;max-width:680px}
.toprow{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.sec2{margin-top:26px}
.sec2 h3{font-size:var(--text-lg);font-weight:600;display:flex;align-items:center;gap:8px}
.sec2 > p{font-size:var(--text-sm);color:var(--muted);margin-top:2px}
.tg2{display:inline-flex;background:var(--pearl);border-radius:var(--radius-pill);padding:4px;gap:4px;margin-top:22px}
.tg2 span{height:40px;display:inline-flex;align-items:center;gap:8px;padding:0 18px;border-radius:var(--radius-pill);font-weight:600;font-size:var(--text-sm);color:var(--muted)}
.tg2 span.on{background:var(--surface);color:var(--text);box-shadow:var(--shadow)}
.fld{display:flex;flex-direction:column;gap:6px;margin-top:16px;min-width:0}
.lbl{font-size:var(--text-sm);font-weight:600}
.lbl em{font-style:normal;color:var(--muted);font-weight:400}
.inp{height:52px;border:1.5px solid var(--border);border-radius:16px;background:var(--surface);display:flex;align-items:center;gap:10px;padding:0 16px;font-size:var(--text-md);color:var(--text);min-width:0}
.inp .i{color:var(--muted)}
.inp .ph{color:var(--muted)}
.inp.focus{border-color:var(--brand);box-shadow:0 0 0 4px color-mix(in srgb,var(--brand) 12%,transparent)}
.inp.ok{border-color:var(--success)}
.inp.big{height:60px;font-size:var(--text-lg)}
.inp .sp{flex:1}
.inp .kbd{font-size:var(--text-xs);color:var(--muted);border:1px solid var(--border);border-radius:6px;padding:1px 6px}
.hint{font-size:var(--text-xs);color:var(--muted)}
.grid2{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:0 16px}
.grid3{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:14px}
.acts{display:flex;align-items:center;gap:12px;margin-top:28px;padding-top:20px;border-top:1px solid var(--border)}
.acts .sp{flex:1}
.acts .skip{font-size:var(--text-sm);color:var(--muted);font-weight:600}
.nb{display:flex;gap:12px;align-items:flex-start;border-radius:18px;padding:14px 16px;font-size:var(--text-sm);background:var(--sky)}
.nb.w{background:var(--warn-soft)}.nb.s{background:var(--mint)}.nb.p{background:var(--peach)}.nb.c{background:var(--cream)}
.nb .e{font-size:20px;line-height:1.2}
.nb > div > b:first-child{display:block;font-size:var(--text-md)}
.nb p{color:var(--muted)}
.rlist{display:flex;flex-direction:column;gap:10px;margin-top:14px}
.rrow{display:grid;grid-template-columns:48px minmax(0,1fr) auto 20px;gap:14px;align-items:center;padding:14px 16px;border-radius:20px;background:var(--surface);border:1.5px solid var(--border)}
.rrow.sel{border-color:var(--coral);background:var(--peach)}
.rrow.dim{background:var(--pearl)}
.rrow h4{font-size:var(--text-md);font-weight:600;display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.rrow .meta{font-size:var(--text-sm);color:var(--muted);display:flex;gap:14px;flex-wrap:wrap}
.rrow .meta span{display:inline-flex;gap:5px;align-items:center}.rrow .meta .i{width:14px;height:14px}
.rrow .tags{display:flex;gap:6px;flex-wrap:wrap;justify-content:flex-end}
.rrow > .i{color:var(--muted)}
mark{background:var(--sun-2);color:inherit;border-radius:4px;padding:0 2px}
.cobox{display:flex;gap:16px;align-items:center;padding:18px 20px;border-radius:22px;background:var(--cream)}
.cobox h3{font-size:var(--text-xl);font-weight:700}
.cobox .meta{font-size:var(--text-sm);color:var(--muted);display:flex;gap:14px;flex-wrap:wrap;margin-top:2px}
.cobox .sp{flex:1}
.optc{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px;margin-top:14px}
.oc{border:1.5px solid var(--border);border-radius:22px;padding:18px;display:flex;flex-direction:column;gap:8px;background:var(--surface);position:relative}
.oc.on{border-color:var(--coral);background:var(--peach)}
.oc .rd{position:absolute;top:18px;right:18px;width:22px;height:22px;border-radius:50%;border:2px solid var(--border);background:var(--surface)}
.oc.on .rd{border:7px solid var(--coral)}
.oc h4{font-size:var(--text-lg);font-weight:600;padding-inline-end:28px}
.oc p{font-size:var(--text-sm);color:var(--muted)}
.oc ul{list-style:none;display:flex;flex-direction:column;gap:4px;font-size:var(--text-sm)}
.oc ul li{display:flex;gap:6px;align-items:flex-start}.oc ul .i{width:16px;height:16px;color:var(--success);margin-top:2px}
.oc ul li.no .i{color:var(--muted)}
.vl{display:flex;flex-direction:column;margin-top:10px;border:1.5px solid var(--border);border-radius:20px;overflow:hidden}
.vr{display:grid;grid-template-columns:26px minmax(0,1fr) auto;gap:12px;align-items:center;padding:12px 16px;border-top:1px solid var(--border);background:var(--surface)}
.vr:first-child{border-top:0}
.vr.on{background:var(--peach)}
.vr.used{background:var(--pearl)}
.vr b{font-size:var(--text-md)}
.vr .a{font-size:var(--text-sm);color:var(--muted)}
.rb{width:22px;height:22px;border-radius:50%;border:2px solid var(--border);background:var(--surface)}
.rb.on{border:7px solid var(--coral)}
.cb{width:22px;height:22px;border-radius:6px;border:1.5px solid var(--border);background:var(--surface);display:grid;place-items:center;flex:none}
.cb.on{background:var(--coral);border-color:var(--coral);color:var(--surface)}
.cb.inc{background:var(--mint-2);border-color:var(--mint-2);color:var(--success)}
.cb .i{width:14px;height:14px;stroke-width:2.8}
.lnk{color:var(--brand);font-weight:600}
.prov2{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px;margin-top:12px}
.prov2 .btn{border-radius:var(--radius-pill);height:50px}
.prov2 .btn.on{border:2px solid var(--coral);background:var(--peach)}
.or{display:flex;align-items:center;gap:12px;color:var(--muted);font-size:var(--text-xs);font-weight:600;margin-top:18px}
.or::before,.or::after{content:"";flex:1;border-top:1px solid var(--border)}
.chk{font-size:var(--text-sm)}
.chk .b.on{background:var(--coral);border-color:var(--coral)}
.otp{display:flex;gap:10px;margin-top:10px}
.otp span{width:56px;height:64px;border-radius:16px;border:1.5px solid var(--border);display:grid;place-items:center;font-size:1.5rem;font-weight:700;background:var(--surface)}
.otp span.f{border-color:var(--brand);box-shadow:0 0 0 4px color-mix(in srgb,var(--brand) 12%,transparent)}
.otp.w8{gap:8px}.otp.w8 span{width:40px;height:56px;font-size:1.25rem}
.otp .dash{width:14px;border:0;background:none;box-shadow:none;color:var(--muted)}
.mc{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px;margin-top:18px}
.mcard{border:1.5px solid var(--border);border-radius:24px;padding:22px;display:flex;flex-direction:column;gap:10px;position:relative;background:var(--surface)}
.mcard.on{border-color:var(--coral);background:var(--peach)}
.mcard .rd{position:absolute;top:20px;right:20px;width:22px;height:22px;border-radius:50%;border:2px solid var(--border);background:var(--surface)}
.mcard.on .rd{border:7px solid var(--coral)}
.mcard h3{font-size:var(--text-xl);font-weight:700;padding-inline-end:30px}
.mcard p{font-size:var(--text-sm);color:var(--muted)}
.mcard .facts2{display:flex;gap:6px;flex-wrap:wrap}
.mcard .how{font-size:var(--text-sm);display:flex;flex-direction:column;gap:6px;background:var(--surface);border-radius:16px;padding:12px 14px}
.mcard.on .how{background:color-mix(in srgb,var(--surface) 75%,transparent)}
.mcard .how div{display:flex;gap:8px;align-items:flex-start}
.mcard .how .n{width:20px;height:20px;border-radius:50%;background:var(--sky);display:grid;place-items:center;font-size:var(--text-xs);font-weight:700;flex:none;margin-top:1px}
.dm{display:inline-flex;align-items:center;gap:6px;background:var(--surface);border-radius:var(--radius-pill);padding:4px 10px 4px 4px;font-size:var(--text-sm);font-weight:600;border:1px solid var(--border)}
.dm .e{width:24px;height:24px;border-radius:50%;background:var(--mint);display:grid;place-items:center;font-size:13px}
.env{position:relative;height:220px;border-radius:24px;background:var(--sky);overflow:hidden}
.env .paper{position:absolute;left:50%;top:18px;z-index:1;width:250px;transform:translateX(-50%) rotate(-3deg);background:var(--surface);border-radius:10px;box-shadow:var(--shadow-lg);padding:16px 18px;font-size:var(--text-xs);color:var(--muted)}
.env .paper b{display:block;color:var(--text);font-size:var(--text-sm)}
.env .paper .code{margin-top:10px;font-size:1.25rem;letter-spacing:.2em;font-weight:700;color:var(--text);background:var(--cream);border-radius:8px;padding:6px 10px;text-align:center}
.env .flap{position:absolute;left:50%;bottom:-70px;width:330px;height:110px;transform:translateX(-50%);background:var(--sun-2);border-radius:16px}
.env .flap::before{content:"";position:absolute;inset:0;background:linear-gradient(160deg,transparent 49%,color-mix(in srgb,var(--gold) 45%,var(--surface)) 50%,transparent 51%),linear-gradient(200deg,transparent 49%,color-mix(in srgb,var(--gold) 45%,var(--surface)) 50%,transparent 51%);border-radius:16px}
.env img{position:absolute;z-index:2;right:26px;bottom:6px;width:96px;transform:rotate(8deg)}
.tl{display:flex;flex-direction:column;gap:0;margin-top:8px}
.tl div{display:grid;grid-template-columns:28px minmax(0,1fr) auto;gap:10px;align-items:start;padding:8px 0;font-size:var(--text-sm)}
.tl .d{width:24px;height:24px;border-radius:50%;display:grid;place-items:center;background:var(--pearl-mid);color:var(--muted)}
.tl .d .i{width:14px;height:14px;stroke-width:2.6}
.tl .d.ok{background:var(--mint);color:var(--success)}.tl .d.now{background:var(--coral);color:var(--surface)}
.tl small{color:var(--muted);font-size:var(--text-xs)}
.canlist{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px 18px;font-size:var(--text-sm);margin-top:8px}
.canlist div{display:flex;gap:8px;align-items:flex-start}
.canlist .i{width:16px;height:16px;margin-top:2px;color:var(--success)}
.canlist .no .i{color:var(--muted)}.canlist .no{color:var(--muted)}
.chs{display:flex;flex-wrap:wrap;gap:10px;margin-top:12px}
.chs > span{height:48px;display:inline-flex;align-items:center;gap:10px;padding:0 16px 0 8px;border-radius:var(--radius-pill);border:1.5px solid var(--border);background:var(--surface);font-weight:600;font-size:var(--text-md)}
.chs span .e{width:34px;height:34px;border-radius:50%;background:var(--pearl);display:grid;place-items:center;font-size:17px}
.chs > span.on{border-color:var(--coral);background:var(--peach)}
.chs span.on .e{background:var(--surface)}
.chs span .t{font-size:var(--text-xs);font-weight:600;color:var(--success);background:var(--mint);border-radius:var(--radius-pill);padding:1px 8px}
.chs span .ck{width:20px;height:20px;border-radius:50%;background:var(--coral);color:var(--surface);display:grid;place-items:center}
.chs span .ck .i{width:12px;height:12px;stroke-width:3}
.sbi{display:flex;gap:10px;align-items:center;font-size:var(--text-sm);background:var(--cream);border-radius:16px;padding:10px 14px;margin-top:12px}
.sbi code{font-family:inherit;font-weight:700;background:var(--surface);border-radius:6px;padding:1px 6px}
.sl{display:flex;flex-direction:column;gap:14px;margin-top:14px}
.slr{display:grid;grid-template-columns:minmax(0,1fr) 250px minmax(0,1fr);gap:16px;align-items:center;font-size:var(--text-sm)}
.slr .l{text-align:end}
.slr .l,.slr .r2{display:flex;gap:8px;align-items:center}
.slr .l{justify-content:flex-end}
.slr .e{font-size:18px}
.trk{display:flex;justify-content:space-between;align-items:center;position:relative;height:30px}
.trk::before{content:"";position:absolute;left:10px;right:10px;top:50%;border-top:3px solid var(--pearl-mid);border-radius:3px}
.trk i{position:relative;width:16px;height:16px;border-radius:50%;background:var(--surface);border:2px solid var(--border)}
.trk i.on{width:28px;height:28px;background:var(--coral);border:4px solid var(--surface);box-shadow:0 2px 8px color-mix(in srgb,var(--coral) 40%,transparent)}
.slr .dim{grid-column:1/-1;display:none}
.vcards{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:12px;margin-top:14px}
.vc{border:1.5px solid var(--border);border-radius:20px;padding:14px 12px;display:flex;flex-direction:column;gap:6px;align-items:flex-start;background:var(--surface);position:relative;min-height:138px}
.vc .e{font-size:26px}
.vc b{font-size:var(--text-sm);line-height:1.3}
.vc small{font-size:var(--text-xs);color:var(--muted);margin-top:auto}
.vc.on{border-color:var(--coral);background:var(--peach)}
.vc .ck{position:absolute;top:10px;right:10px;width:22px;height:22px;border-radius:50%;background:var(--coral);color:var(--surface);display:grid;place-items:center}
.vc .ck .i{width:13px;height:13px;stroke-width:3}
.cnt{margin-inline-start:auto;font-size:var(--text-sm);font-weight:600;color:var(--coral)}
.prof{display:grid;grid-template-columns:repeat(6,minmax(0,1fr));gap:10px;margin-top:10px}
.prof div{background:var(--cream);border-radius:14px;padding:10px;font-size:var(--text-xs);color:var(--muted)}
.prof div b{display:block;color:var(--text);font-size:var(--text-sm)}
.eng{display:flex;flex-direction:column;gap:10px;margin-top:14px}
.er{border:1.5px solid var(--border);border-radius:20px;padding:14px 16px;background:var(--surface)}
.er.on{border-color:var(--coral);background:var(--peach)}
.er .top{display:grid;grid-template-columns:22px 40px minmax(0,1fr) auto;gap:12px;align-items:center}
.er .top .e{font-size:22px;width:40px;height:40px;border-radius:50%;background:var(--pearl);display:grid;place-items:center}
.er.on .top .e{background:var(--surface)}
.er h4{font-size:var(--text-md);font-weight:600}
.er .top p{font-size:var(--text-sm);color:var(--muted)}
.er .proof{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:10px;align-items:center;margin:12px 0 0 74px}
.er .proof .inp{height:44px;border-radius:14px;font-size:var(--text-sm)}
.bdg{display:inline-flex;align-items:center;gap:6px;height:30px;padding:0 12px 0 6px;border-radius:var(--radius-pill);font-size:var(--text-sm);font-weight:600;background:var(--mint);color:var(--text)}
.bdg .e{font-size:15px}
.bdg.ver::after{content:"✓";color:var(--success);font-weight:700}
.bsrc{font-size:var(--text-xs);color:var(--muted)}
.pill.self{background:var(--pearl-mid);color:var(--muted)}
.pill.chkd{background:var(--mint);color:var(--success)}
.pill.coral{background:var(--coral);color:var(--surface)}
/* app shell (werkgever) */
.app{display:grid;grid-template-columns:248px minmax(0,1fr);min-height:100vh;background:var(--bg)}
.side{background:var(--surface);border-inline-end:1px solid var(--border);padding:16px 12px;display:flex;flex-direction:column;gap:2px}
.side .brand{padding:6px 8px 16px}
.side .co{display:flex;gap:10px;align-items:center;padding:10px;border-radius:12px;background:var(--pearl);margin-bottom:12px;font-size:var(--text-sm)}
.side .co b{display:block}
.side .co small{color:var(--muted);font-size:var(--text-xs)}
.side a{display:flex;align-items:center;gap:10px;height:40px;padding:0 10px;border-radius:10px;font-size:var(--text-sm);font-weight:600;color:var(--muted)}
.side a.on{background:var(--accent-soft);color:var(--brand)}
.side a .lk{margin-inline-start:auto;width:14px;height:14px}
.side h5{font-size:var(--text-xs);color:var(--muted);font-weight:600;padding:14px 10px 4px;text-transform:uppercase;letter-spacing:.04em}
.amain{padding:0 32px 40px}
.atop{height:64px;display:flex;align-items:center;gap:12px}
.atop h1{font-size:var(--text-2xl);font-weight:700}
.atop .r{margin-inline-start:auto;display:flex;gap:10px;align-items:center}
.ban{display:flex;gap:16px;align-items:center;padding:16px 20px;border-radius:20px;background:var(--warn-soft);border:1.5px solid color-mix(in srgb,var(--warn) 30%,transparent)}
.ban .e{font-size:26px}
.ban b{font-size:var(--text-md)}
.ban p{font-size:var(--text-sm);color:var(--muted)}
.ban .sp{flex:1}
.dgrid{display:grid;grid-template-columns:minmax(0,1.35fr) minmax(0,1fr);gap:20px;margin-top:20px;align-items:start}
.pan{background:var(--surface);border-radius:var(--radius-lg);box-shadow:var(--shadow-soft);padding:22px}
.pan h3{font-size:var(--text-lg);font-weight:600;display:flex;align-items:center;gap:8px}
.pan h3 .sp{flex:1}
.cl{display:flex;flex-direction:column;gap:8px;margin-top:12px}
.cli{display:grid;grid-template-columns:28px minmax(0,1fr) auto;gap:12px;align-items:center;padding:10px 12px;border-radius:14px;background:var(--pearl)}
.cli.done{background:var(--mint)}
.cli .d{width:26px;height:26px;border-radius:50%;border:2px solid var(--border);background:var(--surface);display:grid;place-items:center}
.cli.done .d{background:var(--success);border-color:var(--success);color:var(--surface)}
.cli .d .i{width:14px;height:14px;stroke-width:3}
.cli b{font-size:var(--text-sm)}
.cli small{display:block;font-size:var(--text-xs);color:var(--muted)}
.prog2{height:10px;border-radius:var(--radius-pill);background:var(--pearl-mid);overflow:hidden;margin-top:10px}
.prog2 i{display:block;height:100%;background:var(--coral);border-radius:inherit}
.vt{width:100%;border-collapse:collapse;margin-top:10px;font-size:var(--text-sm)}
.vt td{padding:10px 8px;border-top:1px solid var(--border);vertical-align:middle}
.vt td:last-child{text-align:end}
.lockrow{display:flex;gap:10px;align-items:center;padding:10px 12px;border-radius:14px;background:var(--pearl);font-size:var(--text-sm);color:var(--muted);margin-top:8px}
.lockrow .i{color:var(--muted)}
.lockrow b{color:var(--text)}
.welc{display:flex;gap:16px;align-items:center;background:var(--sun);border-radius:var(--radius-lg);padding:14px 20px;margin-top:16px;overflow:hidden;position:relative}
.welc img{width:84px;margin:-6px 0 -18px}
.welc h2{font-size:var(--text-xl);font-weight:700}
.welc p{font-size:var(--text-sm);color:var(--muted)}
.vgrid{display:grid;grid-template-columns:minmax(0,1.3fr) minmax(0,1fr);gap:20px;margin-top:8px;align-items:start}
.inh{display:flex;gap:12px;align-items:center;padding:12px 14px;border-radius:16px;background:var(--cream);font-size:var(--text-sm)}
.tgl{width:44px;height:26px;border-radius:13px;background:var(--pearl-mid);position:relative;flex:none}
.tgl::after{content:"";position:absolute;left:3px;top:3px;width:20px;height:20px;border-radius:50%;background:var(--surface);box-shadow:var(--shadow)}
.tgl.on{background:var(--coral)}.tgl.on::after{left:21px}
.pv{background:var(--surface);border-radius:var(--radius-lg);box-shadow:var(--shadow-soft);overflow:hidden}
.pv .ph{background:var(--sky);padding:16px 18px;display:flex;gap:12px;align-items:center}
.pv .ph .lg{width:44px;height:44px;border-radius:12px;background:var(--surface);display:grid;place-items:center;font-weight:700;color:var(--success)}
.pv .ph h4{font-size:var(--text-lg);font-weight:700}
.pv .pb{padding:16px 18px;display:flex;flex-direction:column;gap:12px}
.fitr{display:grid;grid-template-columns:130px minmax(0,1fr) 42px;gap:10px;align-items:center;font-size:var(--text-sm)}
.fitr b.bar2{display:block;height:8px;border-radius:var(--radius-pill);background:var(--pearl-mid);position:relative;overflow:hidden}
.fitr b.bar2::after{content:"";position:absolute;inset:0 auto 0 0;width:var(--w);background:var(--coral);border-radius:inherit}
.fitr.v b.bar2::after{background:var(--success)}
.why{font-size:var(--text-sm);background:var(--cream);border-radius:14px;padding:10px 12px}
.pillars{display:flex;flex-wrap:wrap;gap:8px;margin-top:10px}
.pillars span{height:38px;display:inline-flex;align-items:center;gap:8px;padding:0 14px;border-radius:var(--radius-pill);border:1.5px solid var(--border);font-size:var(--text-sm);font-weight:600}
.pillars span.on{border-color:var(--coral);background:var(--peach)}
.pillars.off span{opacity:.55}
/* mobile */
.m{background:var(--cream)}
.m .mw{padding:0 16px 16px}
.m .mhd{height:56px;display:flex;align-items:center;gap:10px;padding:0 8px 0 16px}
.m .mhd .brand{font-size:var(--text-md)}.m .mhd .brand img{width:30px;height:30px;background:var(--surface)}
.m .mhd .r{margin-inline-start:auto;display:flex;align-items:center}
.m .mhd .ib{width:44px;height:44px;display:grid;place-items:center}
.m .mpr{display:flex;gap:6px;padding:4px 16px 0}
.m .mpr i{flex:1;height:6px;border-radius:3px;background:var(--pearl-mid)}
.m .mpr i.d{background:var(--success)}.m .mpr i.on{background:var(--coral)}
.m .mst{font-size:var(--text-xs);color:var(--muted);font-weight:600;padding:8px 16px 0}
.m .mg{display:flex;gap:10px;align-items:flex-end;margin:10px 0 12px}
.m .mg img{width:64px;flex:none;transform:rotate(-6deg)}
.m .mg .gb{flex:1;font-size:var(--text-sm);padding:10px 12px}
.m .mg .gb::before{top:auto;bottom:12px;inset-inline-start:-9px;border:9px solid transparent;border-inline-start:0;border-inline-end-color:var(--surface)}
.m .wcard{padding:20px 16px;border-radius:28px}
.m .wcard h1{font-size:1.5rem;margin-top:0}
.m .tg2{display:flex;margin-top:14px}.m .tg2 span{flex:1;justify-content:center;padding:0 10px}
.m .inp{height:52px}
.m .rrow{grid-template-columns:40px minmax(0,1fr) 18px;padding:12px}
.m .rrow .tags{grid-column:2;justify-content:flex-start}
.m .emo{width:40px;height:40px;font-size:20px}
.m .optc{grid-template-columns:1fr;gap:10px}
.m .oc{padding:14px}
.m .oc h4{font-size:var(--text-md)}
.m .mc{grid-template-columns:1fr;gap:12px}
.m .mcard{padding:16px}
.m .mcard h3{font-size:var(--text-lg)}
.m .otp{gap:6px}.m .otp span{width:44px;height:54px;font-size:1.25rem}
.m .otp.w8 span{width:34px}
.m .otp .dash{width:8px}
.m .vcards{grid-template-columns:repeat(2,minmax(0,1fr));gap:10px}
.m .vc{min-height:112px}
.m .slr{grid-template-columns:1fr;gap:4px}
.m .slr .l,.m .slr .r2{display:none}
.m .slr .dim{display:flex;justify-content:space-between;font-size:var(--text-xs);color:var(--muted);font-weight:600}
.m .chs > span{height:44px;font-size:var(--text-sm)}
.m .er .top{grid-template-columns:22px 36px minmax(0,1fr)}
.m .er .top .e{width:36px;height:36px;font-size:18px}
.m .er .top .pill{grid-column:3;justify-self:start}
.m .er .proof{margin:10px 0 0;grid-template-columns:1fr}
.m .env{height:190px}
.m .env .paper{width:220px;left:40%}.m .env img{width:80px;right:14px}
.m .canlist{grid-template-columns:1fr}
.mbar{position:sticky;bottom:0;z-index:8;margin-top:8px;background:var(--surface);box-shadow:0 -6px 20px rgba(15,45,92,.08);padding:12px 16px 20px;display:flex;gap:10px;align-items:center;border-radius:24px 24px 0 0}
.mbar .btn{height:52px;flex:1}
.mbar .btn.ghost{flex:none}
.vbt{position:fixed;right:12px;bottom:92px;z-index:9}
.dvb{position:absolute;right:24px;top:84px}
.bw{display:inline-flex;flex-direction:column;gap:2px;align-items:flex-start;margin-inline-end:8px}
.bdgs{align-items:flex-start;row-gap:10px}
.er .top .pill{white-space:normal;height:auto;min-height:26px;padding:4px 10px;line-height:1.3}
.m .vr{grid-template-columns:26px minmax(0,1fr)}
.m .vr > .toprow{grid-column:2}
.m .bw{margin-inline-end:4px}
'''
