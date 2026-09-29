import pathlib, math, sys
from playwright.sync_api import sync_playwright
d=pathlib.Path(__file__).parent
src=(d/"v1-build.py").read_text(); src=src[:src.index("CSS='''")]
exec(src)  # P, ic, mascot, say, b64, MASCOT, LOGO
_v1=(d/'v1-build.py').read_text(); exec(_v1[_v1.index('def helix'):_v1.index('def dnaring')])
P.update(dict(grid='<rect x="4" y="4" width="7" height="7" rx="1.5"/><rect x="13" y="4" width="7" height="7" rx="1.5"/><rect x="4" y="13" width="7" height="7" rx="1.5"/><rect x="13" y="13" width="7" height="7" rx="1.5"/>',
 antenna='<path d="M7 20a5 5 0 0 1 10 0z"/><path d="M10 15.4C9.4 10.5 7.3 6.4 3.5 3.5M14 15.4c.6-4.9 2.7-9 6.5-11.9"/>',
 claw='<path d="M4 21l5.5-5.5"/><path d="M9.5 15.5C8 10.5 11 5 17 3.5c.6 3-1.2 5.8-4.2 6.8"/><path d="M9.5 15.5c4.6 1.3 9.6-.8 11-5.2-3-.9-6 .1-7.7 2.3"/>',
 stone='<path d="M3.5 16.5c0-4.2 4-7.5 9-7.5 4.6 0 8 2.6 8 6 0 2.9-3.1 4.8-8.3 4.8-5.3 0-8.7-1-8.7-3.3z"/><path d="M8 12.5c1.2-.8 2.6-1.2 4-1.2"/>',
 wave='<path d="M3 8c3-2 6 2 9 0s6 2 9 0M3 13c3-2 6 2 9 0s6 2 9 0M3 18c3-2 6 2 9 0s6 2 9 0"/>',
 shell='<path d="M12 3c5 0 8.5 3 8.5 7.8 0 5.6-4 9.4-8.5 10.7-4.5-1.3-8.5-5.1-8.5-10.7C3.5 6 7 3 12 3z"/><path d="M6.5 8.5c3.5 1.8 7.5 1.8 11 0M5.5 13c4 2.2 9 2.2 13 0"/>',
 dna='<path d="M7 3c0 6 10 6 10 12M17 3c0 6-10 6-10 12M7 21c0-2 1-3 2-4M17 21c0-2-1-3-2-4M8 7h8M8 11h8"/>'))

CSS='''
:root{--bg:#f5f2ee;--surface:#fffcfa;--text:#122033;--muted:#5a6a7d;--border:#ddd5cc;--pearl:#efe9e3;--brand:#0f2d5c;--accent-hover:#163a6b;--accent-soft:#e7eef7;--coral:#f54a1b;
--success:#15803d;--success-soft:#ecfdf3;--warn:#a65b00;--warn-soft:#fff8e7;--danger:#9b1c1c;--danger-soft:#fbeeee;
--gold:#c9a227;--gold-light:#e4c65a;--gold-deep:#a6851c;--gold-ink:#5c4a0f;--gold-soft:#faf6e8;--gold-gradient:linear-gradient(135deg,var(--gold-light) 0%,var(--gold) 48%,var(--gold-deep) 100%);
--font:"Inter","Segoe UI","Helvetica Neue",Arial,sans-serif;--shadow:0 1px 2px rgba(15,45,92,.06);--shadow-lg:0 12px 32px rgba(15,45,92,.12);--radius-sm:8px;--radius:12px;
--text-xs:.75rem;--text-sm:.875rem;--text-md:1rem;--text-lg:1.125rem;--text-xl:1.375rem;--text-2xl:1.75rem}
*{box-sizing:border-box}html,body{margin:0}
body{font-family:var(--font);background:var(--bg);color:var(--text);line-height:1.45;font-size:16px}
p,h1,h2,h3,h4,ul{margin:0}ul{padding:0;list-style:none}
svg.i{width:18px;height:18px;fill:none;stroke:currentColor;stroke-linecap:round;stroke-linejoin:round;flex-shrink:0}
.vh{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0 0 0 0)}
.muted{color:var(--muted)}.sm{font-size:var(--text-sm)}.xs{font-size:var(--text-xs)}.b6{font-weight:600}
.demo{font-size:12px;color:var(--muted);border:1px dashed var(--border);border-radius:6px;padding:0 8px;background:var(--surface)}
.hd{position:sticky;top:0;z-index:40;height:64px;background:var(--surface);display:flex;align-items:center;justify-content:space-between;padding:0 24px;border-bottom:1px solid var(--pearl)}
.wm{display:flex;align-items:center;gap:10px}.wm img{width:40px;height:40px}.wm b{font-size:22px;font-weight:700;color:var(--brand)}.wm span{font-size:14px;font-weight:600;color:var(--brand);margin-left:14px}
.hr{display:flex;align-items:center;gap:12px}
.cb{width:40px;height:40px;border-radius:50%;border:1px solid var(--border);background:var(--surface);display:flex;align-items:center;justify-content:center;color:var(--brand)}
.lang{height:40px;padding:0 12px;border:1px solid var(--border);border-radius:var(--radius-sm);display:flex;align-items:center;gap:8px;font-size:14px;font-weight:600;color:var(--brand);background:var(--surface)}
.flag{width:20px;height:14px;border-radius:3px;background:linear-gradient(#ae1c28 33%,#fff 33% 66%,#21468b 66%);box-shadow:0 0 0 1px rgba(0,0,0,.08)}
.bn{position:fixed;left:0;right:0;bottom:0;height:64px;background:var(--surface);border-top:1px solid var(--pearl);display:grid;grid-template-columns:repeat(5,1fr);padding:4px 8px;z-index:50}
.bn a{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:2px;font-size:12px;font-weight:600;color:var(--muted);border-radius:var(--radius-sm)}
.bn a.on{background:var(--accent-soft);color:var(--brand)}
.card{background:var(--surface);border-radius:var(--radius);box-shadow:var(--shadow);border:1px solid var(--pearl)}
.btn{height:40px;border-radius:var(--radius-sm);display:inline-flex;align-items:center;justify-content:center;gap:8px;font-size:var(--text-sm);font-weight:600;border:1px solid var(--border);color:var(--brand);background:var(--surface);padding:0 14px;white-space:nowrap}
.btn.pri{background:var(--brand);color:#fff;border-color:var(--brand)}
.btn.gold{background:var(--gold-gradient);color:var(--gold-ink);border-color:var(--gold-deep)}
.btn.sm{height:36px;padding:0 12px}
.lnk{white-space:nowrap;font-size:var(--text-sm);font-weight:600;color:var(--brand);display:inline-flex;align-items:center;gap:4px}.lnk .i{width:15px;height:15px}
.pill{font-size:12px;font-weight:600;border-radius:999px;padding:2px 10px;background:var(--accent-soft);color:var(--brand);display:inline-flex;align-items:center;gap:5px;white-space:nowrap}
.pill.ok{background:var(--success-soft);color:var(--success)}.pill.gold{background:var(--gold-soft);color:var(--gold-ink)}
.pill .i{width:13px;height:13px}
.pills{display:flex;flex-wrap:wrap;gap:6px}
h3.ct{font-size:var(--text-md);font-weight:600;color:var(--brand)}
.say{display:flex;align-items:center;gap:10px}
.say .masc{flex:none}
.bubble{background:var(--accent-soft);border-radius:14px 14px 14px 4px;padding:8px 14px;font-size:var(--text-sm);color:var(--text)}
/* layout desktop */
.page{display:grid;grid-template-columns:330px 1fr;gap:20px;padding:16px 24px 88px}
.left{position:sticky;top:80px;align-self:start}
/* passport */
.pp{overflow:hidden}
.pp .banner{height:68px;position:relative;background:linear-gradient(135deg,var(--brand) 0%,var(--accent-hover) 100%);color:#fff;padding:12px 16px;overflow:hidden}
.pp .banner .t{font-size:12px;font-weight:600;letter-spacing:.14em;opacity:.9}
.pp .banner .no{font-size:11px;opacity:.75}
.pp .banner .helix{position:absolute;right:70px;top:-22px;opacity:.3}
.pp .stamp{position:absolute;right:14px;top:8px;width:54px;height:54px;border-radius:50%;border:2px dashed var(--gold-light);color:var(--gold-light);display:flex;flex-direction:column;align-items:center;justify-content:center;font-size:8px;font-weight:600;letter-spacing:.06em;transform:rotate(-12deg);text-align:center;line-height:1.15}
.pp .stamp b{font-size:11px}
.idrow{display:flex;gap:12px;align-items:flex-end;padding:0 16px;margin-top:-30px;position:relative}
.av{width:76px;height:76px;border-radius:50%;border:3px solid var(--surface);position:relative;background:linear-gradient(160deg,#f3d9c4,#d9a987);overflow:hidden;box-shadow:var(--shadow-lg);flex:none}
.av .hair{position:absolute;left:10px;top:7px;width:56px;height:52px;border-radius:30px 30px 14px 14px;background:#3a2418}
.av .face{position:absolute;left:21px;top:19px;width:34px;height:40px;border-radius:18px;background:#e8b896}
.av .body{position:absolute;left:7px;top:57px;width:62px;height:30px;border-radius:30px 30px 0 0;background:var(--brand)}
.av-edit{position:absolute;left:58px;bottom:0;width:26px;height:26px;border-radius:50%;background:var(--surface);border:1px solid var(--border);display:flex;align-items:center;justify-content:center;color:var(--brand)}.av-edit .i{width:13px;height:13px}
.idrow h2{font-size:var(--text-xl);font-weight:600;color:var(--brand);line-height:1.2;padding-bottom:4px}
.ppb{padding:12px 16px 16px;display:flex;flex-direction:column;gap:12px}
.comp{display:flex;align-items:center;gap:8px;font-size:12px;color:var(--muted)}
.comp .bar{flex:1;height:6px;border-radius:999px;background:var(--pearl);overflow:hidden}.comp .bar i{display:block;height:100%;width:72%;background:var(--success)}
.comp a{color:var(--brand);font-weight:600}
.qt{padding:10px 12px;border-radius:var(--radius-sm);background:var(--bg);font-size:var(--text-sm)}
.qt .lab{display:flex;align-items:center;gap:6px;font-size:11px;font-weight:600;color:var(--muted);letter-spacing:.06em;margin-bottom:2px}.qt .lab .i{width:13px;height:13px}
.boxes{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.box{border:1px solid var(--border);border-radius:var(--radius-sm);padding:7px 10px}
.box .l{display:flex;align-items:center;gap:6px;font-size:12px;color:var(--muted)}.box .l .i{width:14px;height:14px}
.box b{display:block;font-size:var(--text-sm);font-weight:600}
.kv{display:flex;gap:8px;align-items:baseline;font-size:var(--text-sm)}.kv>span:first-child{color:var(--muted);font-size:12px;min-width:74px}
.acts{display:flex;flex-direction:column;gap:8px}
.row2{display:grid;grid-template-columns:1fr 1fr;gap:8px}
/* overview */
.ov{display:grid;grid-template-columns:290px 1fr;gap:12px}
.ring-card{padding:14px 16px;display:flex;align-items:center;gap:14px}
.ring-card h3{font-size:var(--text-md);font-weight:600;color:var(--brand)}
.lg{display:flex;flex-direction:column;gap:4px;margin-top:6px}.lg span{display:flex;align-items:center;gap:6px;font-size:12px;color:var(--muted)}.lg i{width:10px;height:10px;border-radius:3px}
.stats{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}
.stat{padding:14px 16px;position:relative;overflow:hidden}
.stat::before{content:"";position:absolute;left:0;top:0;bottom:0;width:4px;background:var(--c)}
.stat .ico{width:32px;height:32px;border-radius:10px;display:flex;align-items:center;justify-content:center;background:var(--bg);color:var(--brand);margin-bottom:8px}
.stat .k{font-size:12px;color:var(--muted)}
.stat .v{font-size:var(--text-lg);font-weight:600;color:var(--brand);line-height:1.25;margin-top:2px}
.stat .t{font-size:12px;color:var(--muted);margin-top:6px;display:flex;align-items:center;gap:6px}
.stat .t .i{width:13px;height:13px;color:var(--success)}
/* tabs */
.tabs{display:flex;gap:4px;border-bottom:1px solid var(--border);margin:18px 0 16px;position:sticky;top:64px;background:var(--bg);z-index:20}
.tab{height:44px;padding:0 14px;display:flex;align-items:center;gap:8px;font-size:var(--text-sm);font-weight:600;color:var(--muted);border-bottom:3px solid transparent;margin-bottom:-1px;white-space:nowrap}
.tab .i{width:16px;height:16px}
.tab.on{color:var(--brand);border-color:var(--brand)}
.tab .cnt{font-size:11px;background:var(--gold-soft);color:var(--gold-ink);border-radius:999px;padding:0 6px}
.tabhead{display:flex;align-items:center;justify-content:space-between;gap:16px;margin-bottom:14px}
.g3{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}
.g2{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.g75{display:grid;grid-template-columns:1.3fr 1fr;gap:12px}
.pad{padding:16px 18px}
.irow{display:flex;gap:12px;align-items:flex-start;padding:8px 0}
.irow .ico{width:34px;height:34px;border-radius:10px;display:flex;align-items:center;justify-content:center;flex:none;background:var(--bg);color:var(--brand)}
.irow b{display:block;font-size:var(--text-sm);font-weight:600}.irow p{font-size:12px;color:var(--muted)}
.chips{display:flex;flex-wrap:wrap;gap:6px;margin-top:10px}
.chip{font-size:12px;border-radius:999px;padding:2px 10px;background:var(--bg);border:1px solid var(--border)}
.sl{margin-top:10px}.sl .ends{display:flex;justify-content:space-between;font-size:12px}.sl .ends b{font-weight:600}
.sl .track{height:6px;border-radius:999px;background:var(--pearl);position:relative;margin-top:6px}
.sl .knob{position:absolute;top:50%;width:14px;height:14px;border-radius:50%;background:var(--brand);border:3px solid var(--surface);box-shadow:0 0 0 1px var(--brand);transform:translate(-50%,-50%)}
.vals li{display:flex;align-items:center;gap:8px;font-size:var(--text-sm);padding:3px 0}
.vals li span{width:20px;height:20px;border-radius:50%;background:var(--accent-soft);color:var(--brand);font-size:11px;font-weight:600;display:flex;align-items:center;justify-content:center}
.stampbar{display:flex;align-items:center;gap:14px;padding:12px 18px;margin-top:12px}
.stampbar .ttl{min-width:150px}
.ps{width:64px;height:64px;border-radius:50%;border:2px dashed var(--brand);color:var(--brand);display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center;font-size:8.5px;font-weight:600;letter-spacing:.03em;line-height:1.15;flex:none}
.ps .i{width:15px;height:15px;margin-bottom:2px}
.ps.g{border-color:var(--success);color:var(--success);transform:rotate(-8deg)}.ps.y{border-color:var(--gold-deep);color:var(--gold-deep);transform:rotate(6deg)}.ps.b{transform:rotate(-3deg)}
.ps.off{border-color:var(--border);color:var(--muted);border-style:dotted}
/* tests tab */
.trow{display:grid;grid-template-columns:36px 1fr 176px 104px;gap:12px;align-items:center;padding:12px 14px}.trow .btn{width:100%}.trow b,.trow small,.trow .q{white-space:nowrap}
.trow+.trow{border-top:1px solid var(--pearl)}
.trow.sel{background:var(--accent-soft)}
.trow .ico{width:36px;height:36px;border-radius:10px;display:flex;align-items:center;justify-content:center;background:var(--bg);color:var(--brand)}
.trow b{display:block;font-size:var(--text-sm);font-weight:600}.trow small{display:block;font-size:12px;color:var(--muted)}
.mini{display:grid;grid-template-columns:repeat(3,1fr);gap:4px}
.mini span{display:flex;flex-direction:column;gap:3px;font-size:10px;white-space:nowrap;color:var(--muted)}
.mini span i{height:6px;border-radius:999px;background:var(--pearl)}
.mini span.done i{background:var(--success)}.mini span.now i{background:linear-gradient(90deg,var(--brand) var(--p,0%),var(--pearl) var(--p,0%))}.mini span.lock i{background:var(--gold-soft);box-shadow:inset 0 0 0 1px var(--gold-light)}
.mini span.done{color:var(--success)}
.dots{display:inline-flex;gap:3px;margin-right:6px;vertical-align:middle}.dots i{width:7px;height:7px;border-radius:50%;background:var(--brand)}.dots i.u{background:var(--border)}
.q{font-size:11px;color:var(--muted);margin-top:4px}
.lockp{padding:16px 18px}
.lockp .hd2{display:flex;align-items:center;justify-content:space-between;gap:10px;margin-bottom:10px}
.vpill{font-size:11px;font-weight:600;color:var(--warn);background:var(--warn-soft);border:1px dashed var(--warn);border-radius:999px;padding:1px 8px;white-space:nowrap}
.lgrid{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.lc{border:1px solid var(--border);border-radius:var(--radius-sm);padding:8px 10px;position:relative;overflow:hidden;height:62px;background:var(--surface)}
.lc b{font-size:12px;font-weight:600;display:flex;align-items:center;gap:5px}.lc b .i{width:12px;height:12px;color:var(--gold-deep)}
.lc .blur{filter:blur(2.5px);margin-top:6px;display:flex;flex-direction:column;gap:4px}.lc .blur i{display:block;height:5px;border-radius:999px;background:var(--border)}
.lc .vst{position:absolute;right:6px;bottom:6px;transform:rotate(-8deg);font-size:9px;font-weight:600;color:var(--warn);border:1.5px dashed var(--warn);border-radius:4px;padding:0 4px;background:rgba(255,248,231,.9)}
.checks{display:flex;flex-direction:column;gap:4px;margin:10px 0 12px;font-size:12px}.checks span{display:flex;align-items:center;gap:6px}.checks .i{color:var(--success);width:14px;height:14px}
/* fit */
.inp{height:42px;border:1px solid var(--border);border-radius:var(--radius-sm);display:flex;align-items:center;padding:0 12px;font-size:var(--text-sm);color:var(--muted);background:var(--surface);flex:1}
.fitres{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-top:8px}
.fr{border:1px solid var(--border);border-radius:var(--radius-sm);padding:7px 11px}
.fr .n{font-size:11px;color:var(--muted)}.fr b{display:block;font-size:var(--text-sm);font-weight:600}
.vac li{display:flex;align-items:center;gap:10px;padding:8px 0;border-top:1px solid var(--pearl)}.vac li:first-child{border-top:0}
.vac .lgo{width:34px;height:34px;border-radius:var(--radius-sm);background:var(--bg);display:flex;align-items:center;justify-content:center;font-size:12px;font-weight:600;color:var(--brand);flex:none}
.vac b{display:block;font-size:var(--text-sm);font-weight:600}.vac small{font-size:12px;color:var(--muted)}
.vac .pc{margin-left:auto;font-size:12px;font-weight:600;color:var(--success);background:var(--success-soft);border-radius:999px;padding:1px 8px;white-space:nowrap}
/* career */
.stepper{display:flex;align-items:flex-start;margin:10px 0 0}
.stn{flex:1;text-align:center;position:relative}
.stn .dot{width:30px;height:30px;border-radius:50%;margin:0 auto 6px;display:flex;align-items:center;justify-content:center;background:var(--pearl);color:var(--muted);font-size:12px;font-weight:600;position:relative;z-index:1}
.stn.done .dot{background:var(--success);color:#fff}.stn.now .dot{background:var(--brand);color:#fff;box-shadow:0 0 0 5px var(--accent-soft)}
.stn.goal .dot{background:var(--gold-soft);color:var(--gold-deep);border:2px solid var(--gold)}
.stn:not(:last-child)::after{content:"";position:absolute;top:15px;left:50%;width:100%;height:2px;background:var(--border)}
.stn.done::after{background:var(--success)}
.stn b{display:block;font-size:12px;font-weight:600}.stn span{font-size:12px;color:var(--muted)}
.gap li{display:flex;gap:8px;align-items:flex-start;font-size:var(--text-sm);padding:5px 0}.gap li .i{width:16px;height:16px;color:var(--warn);margin-top:2px}
.gap li.ok .i{color:var(--success)}
/* proofs */
.proof h3{display:flex;align-items:center;gap:8px;font-size:var(--text-md);font-weight:600;color:var(--brand);margin-bottom:10px}
.proof h3 .i{color:var(--brand)}
.tl li{position:relative;padding:0 0 10px 16px;border-left:2px solid var(--pearl);margin-left:5px}
.tl li::before{content:"";position:absolute;left:-6px;top:4px;width:10px;height:10px;border-radius:50%;background:var(--brand)}
.tl b{display:block;font-size:var(--text-sm);font-weight:600}.tl span{font-size:12px;color:var(--muted)}
.add{display:inline-flex;align-items:center;gap:6px;font-size:var(--text-sm);font-weight:600;color:var(--brand);margin-top:4px}.add .i{width:16px;height:16px}
/* settings */
.accg{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.acc{display:flex;align-items:center;gap:12px;padding:14px 16px}
.acc .ico{width:36px;height:36px;border-radius:10px;display:flex;align-items:center;justify-content:center;flex:none;background:var(--bg);color:var(--brand)}
.acc b{display:block;font-size:var(--text-sm);font-weight:600}.acc small{display:block;font-size:12px;color:var(--muted)}
.acc .sp{flex:1}
.danger{color:var(--danger)!important}
.tg{width:40px;height:24px;border-radius:999px;background:var(--border);position:relative;flex:none}.tg::after{content:"";position:absolute;left:3px;top:3px;width:18px;height:18px;border-radius:50%;background:#fff;box-shadow:var(--shadow)}
.tg.on{background:var(--success)}.tg.on::after{left:19px}
.quick{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin-bottom:12px}
.quick .card{display:flex;align-items:center;gap:12px;padding:12px 16px}
/* lobster motifs */
.shl{position:relative;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center;flex:none}
.shl>svg.sh{position:absolute;inset:0;width:100%;height:100%;overflow:visible}
.shl>*:not(svg.sh){position:relative}
.pp .stampw{position:absolute;right:12px;top:6px;width:52px;height:58px;color:var(--gold-light);font-size:7.5px;font-weight:600;letter-spacing:.06em;line-height:1.15;transform:rotate(-10deg);padding-top:6px}
.pp .stampw b{font-size:10.5px;display:block}
.layers{display:flex;align-items:center;gap:8px;font-size:12px;color:var(--muted)}
.layers .lay{flex:1;display:flex;gap:3px}
.layers .lay i{flex:1;height:9px;border-radius:9px 9px 3px 3px;background:var(--pearl)}
.layers .lay i.f1{background:color-mix(in srgb,var(--brand) 35%,var(--accent-soft))}.layers .lay i.f2{background:color-mix(in srgb,var(--brand) 55%,var(--accent-soft))}.layers .lay i.f3{background:color-mix(in srgb,var(--brand) 78%,var(--accent-soft))}.layers .lay i.f4{background:var(--brand)}
.layers a{color:var(--brand);font-weight:600}
.tagl{display:flex;align-items:center;justify-content:center;gap:6px;font-size:12px;color:var(--muted);padding:10px 16px 12px;border-top:1px solid var(--pearl)}.tagl .i{width:14px;height:14px;color:var(--coral)}
.eb{display:flex;align-items:center;gap:6px;font-size:11px;font-weight:600;letter-spacing:.08em;text-transform:uppercase;color:var(--muted);margin-bottom:4px}.eb .i{width:13px;height:13px}
.sub{font-size:12px;color:var(--muted);display:flex;align-items:center;gap:6px;margin-top:2px}.sub .i{width:14px;height:14px;flex:none}
.stg{font-size:12px;color:var(--muted);margin-top:2px}.stg b{color:var(--brand);font-weight:600}
.shb{width:64px;height:70px;font-size:8.5px;font-weight:600;letter-spacing:.03em;line-height:1.15;color:var(--c);padding-top:8px}
.shb .i{width:15px;height:15px;margin-bottom:2px}
.shb.r1{transform:rotate(-7deg)}.shb.r2{transform:rotate(5deg)}.shb.r3{transform:rotate(-3deg)}
/* depth (tests) */
.depth{display:grid;grid-template-columns:36px 1fr 176px 104px;gap:12px;align-items:center;padding:10px 14px 8px;border-bottom:1px solid var(--pearl);font-size:11px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;color:var(--muted)}
.dscale{display:flex;flex-direction:column;gap:4px}
.dscale i{display:block;height:4px;border-radius:999px;background:linear-gradient(90deg,var(--accent-soft),var(--brand))}
.dscale span{display:flex;justify-content:space-between;text-transform:none;letter-spacing:0;font-weight:500}
.tcard{background:linear-gradient(90deg,var(--surface) 55%,color-mix(in srgb,var(--accent-soft) 55%,var(--surface)))}
.tcard .trow.sel{background:var(--accent-soft)}
.mini span:nth-child(1).done i{background:color-mix(in srgb,var(--brand) 38%,var(--accent-soft))}
.mini span:nth-child(2).done i{background:color-mix(in srgb,var(--brand) 70%,var(--accent-soft))}
.mini span:nth-child(3).done i{background:var(--brand)}
.mini span.done{color:var(--brand)}
/* fit */
.fr .n{display:flex;align-items:center;gap:5px}.fr .n .i{width:13px;height:13px;color:var(--brand)}
.fr.grow .n .i{color:var(--warn)}
.cult{display:flex;align-items:center;gap:10px;margin-top:8px;padding:9px 11px;border-radius:var(--radius-sm);background:var(--gold-soft)}
.cult>.i{color:var(--gold-deep)}
.cult b{display:block;font-size:var(--text-sm);font-weight:600}.cult small{font-size:12px;color:var(--muted)}
.cult .pc{margin-left:auto;font-size:12px;font-weight:600;color:var(--success);background:var(--success-soft);border-radius:999px;padding:1px 8px;white-space:nowrap}
/* career growth shells */
.stn .dotw{height:42px;display:flex;align-items:center;justify-content:center;position:relative;z-index:1;margin-bottom:4px}
.stn .shl{font-size:12px;font-weight:600}
.stn .shl .i{width:14px;height:14px}
.stn:not(:last-child)::after{top:21px}
.gap li .i{color:var(--warn)}
.course{display:flex;flex-direction:column;gap:8px;margin-top:8px}
.co{border:1px solid var(--border);border-radius:var(--radius-sm);padding:8px 10px}
.co .tp{display:flex;align-items:center;gap:6px;margin-bottom:2px}
.co b{display:inline;font-size:var(--text-sm);font-weight:600}.co small{font-size:12px;color:var(--muted)}
.pill.free{background:var(--success-soft);color:var(--success)}
.pill.pl{background:var(--surface);color:var(--muted);border:1px solid var(--border);font-weight:500}
.co.first{border-color:var(--success);background:color-mix(in srgb,var(--success-soft) 60%,var(--surface))}
/* proofs shell strength */
.hard{display:flex;align-items:center;gap:14px;padding:12px 18px;margin-bottom:12px}
.hard .segs{display:flex;gap:4px;flex:1;max-width:340px}
.hard .segs i{flex:1;height:12px;border-radius:10px 10px 3px 3px;background:var(--pearl)}
.hard .ends{font-size:11px;color:var(--muted)}
.gblk{margin-top:12px;padding-top:12px;border-top:1px solid var(--pearl)}
.gblk .gh{display:flex;align-items:center;justify-content:space-between;gap:8px;margin-bottom:6px}
.gblk .gh .eb{margin:0;color:var(--brand);white-space:nowrap}
.gblk .gh .nt{display:flex;align-items:center;gap:8px;font-size:11px;color:var(--muted)}
.gopts{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.gopts .co{padding:7px 10px}
.gopts .co .tp{margin:0 0 1px}
.gopts .co b{white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.gopts .co small{display:block}
.why{display:flex;align-items:center;gap:5px;font-size:12px;color:var(--success);font-weight:600;margin-top:1px}.why .i{width:13px;height:13px}
.co:not(.first) .why{color:var(--brand)}
.trow.grow{display:block;padding:0 14px 10px;border-top:0}
.trow.grow .gblk{margin-top:0;padding-top:10px;border-top:1px dashed var(--border)}
.tcard .trow{padding-top:8px;padding-bottom:8px}
.tcard .depth{padding-top:8px;padding-bottom:6px}
/* ================= MOBILE ================= */
body.m .hd{padding:0 12px;height:56px}body.m .wm span{display:none}body.m .wm b{font-size:20px}body.m .wm img{width:34px;height:34px}body.m .cb,body.m .lang{height:36px}body.m .cb{width:36px}
body.m .page{display:block;padding:12px 12px 84px}
.mpp{display:none}
body.m .left{display:none}body.m .mpp{display:block}
.mpp{padding:12px;position:relative}
.mpp .top{display:flex;gap:12px;align-items:center}
.mpp .av{width:60px;height:60px;border-width:2px;box-shadow:none}
.mpp .av .hair{left:8px;top:5px;width:44px;height:42px}.mpp .av .face{left:16px;top:15px;width:28px;height:32px}.mpp .av .body{left:5px;top:45px;width:50px;height:24px}
.mpp h2{font-size:var(--text-lg);font-weight:600;color:var(--brand);line-height:1.2}
.mpp .line{font-size:12px;color:var(--muted);margin-top:3px}
.mpp .share{position:absolute;right:12px;top:12px;width:40px;height:40px;border-radius:50%;background:var(--brand);color:#fff;display:flex;align-items:center;justify-content:center}
.mpp .comp{margin-top:10px;display:block}
.mpp .mstamp{position:absolute;right:60px;top:8px;width:40px;height:44px;color:var(--gold-deep);font-size:6.5px;font-weight:600;display:flex;align-items:center;justify-content:center;text-align:center;line-height:1.1;transform:rotate(-12deg)}
body.m .ov{grid-template-columns:1fr;margin-top:10px}
body.m .ring-card{display:none}
.mdna{display:none}body.m .mdna{display:grid}
.mdna{grid-template-columns:118px 1fr;gap:10px;padding:12px;align-items:center}
.mstats{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.ms{border-left:3px solid var(--c);padding:2px 0 2px 8px}
.ms .k{font-size:11px;color:var(--muted)}.ms .v{font-size:var(--text-sm);font-weight:600;color:var(--brand);line-height:1.2}
body.m .stats{display:none}
body.m .tabs{top:56px;margin:12px -12px 12px;padding:0 12px;overflow:hidden;gap:0}
body.m .tab{padding:0 12px}
body.m .g3,body.m .g2,body.m .g75,body.m .accg,body.m .quick{grid-template-columns:minmax(0,1fr)}body.m .lockp .btn{flex:1}body.m .lgrid{grid-template-columns:minmax(0,1fr) minmax(0,1fr)}body.m .lc b{white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
body.m .trow{grid-template-columns:36px 1fr;row-gap:8px}
body.m .trow .mini{grid-column:1/-1}
body.m .trow .btn{grid-column:1/-1}
body.m .tabhead .say{width:100%}
body.m .only-d{display:none}
body.m .gopts{grid-template-columns:minmax(0,1fr)}body.m .trow.grow{padding:0 14px 12px}body.m .gblk .gh{flex-wrap:wrap}
body.m .tabs{overflow-x:auto;scrollbar-width:none;-webkit-mask-image:linear-gradient(90deg,#000 85%,transparent)}
body.m .trow{grid-template-columns:36px 1fr auto;row-gap:8px}
body.m .trow .btn{grid-column:3;grid-row:1;width:auto}body.m .trow>div:nth-child(2){min-width:0}body.m .trow>div:nth-child(3){grid-row:2;grid-column:1/-1}
body.m .trow .q{white-space:normal}
body.m .depth{grid-template-columns:1fr;padding:10px 14px}
body.m .depth>span{display:none}
body.m .stampbar{flex-wrap:wrap;gap:10px 8px;justify-content:space-between}
body.m .stampbar .ttl{min-width:0;flex-basis:100%}
body.m .shb{width:50px;height:56px;font-size:7px;padding-top:6px}
body.m .shb .i{width:12px;height:12px}
body.m .hard{flex-wrap:wrap}
.only-m{display:none}body.m .only-m{display:block}
'''

COLS=[("var(--brand)","Competenties"),("var(--success)","Beroepen"),("var(--gold)","Cultuur"),("var(--warn)","Waarden")]
def ring(size=150, fills=(0.85,0.7,0.6,0.45), masc=True):
    c=size/2; sw=max(10,size*0.075); R=c-sw/2-2
    svg=f'<svg width="{size}" height="{size}" viewBox="0 0 {size} {size}" role="img" aria-label="Jouw kreeft in fase 3 van 4: vier stukjes van jou" style="flex:none">'
    rr=R-sw-4
    svg+=f'<circle cx="{c}" cy="{c}" r="{rr:.1f}" fill="none" style="stroke:var(--gold-light)" stroke-width="1.5" stroke-dasharray="3 4"/>'
    svg+=f'<circle cx="{c}" cy="{c}" r="{rr-7:.1f}" style="fill:var(--gold-soft)"/>'
    gap=8
    for idx,((col,_),f) in enumerate(zip(COLS,fills)):
        a0=-90+idx*90+gap/2; span=90-gap
        def pt(a): return c+R*math.cos(math.radians(a)), c+R*math.sin(math.radians(a))
        x0,y0=pt(a0);x1,y1=pt(a0+span);xf,yf=pt(a0+span*f)
        svg+=f'<path d="M{x0:.1f},{y0:.1f} A{R:.1f},{R:.1f} 0 0 1 {x1:.1f},{y1:.1f}" fill="none" stroke="{col}" stroke-opacity=".16" stroke-width="{sw:.1f}" stroke-linecap="round"/>'
        svg+=f'<path d="M{x0:.1f},{y0:.1f} A{R:.1f},{R:.1f} 0 0 1 {xf:.1f},{yf:.1f}" fill="none" stroke="{col}" stroke-width="{sw:.1f}" stroke-linecap="round"/>'
    if masc:
        m=size*0.5; svg+=f'<image href="{MASCOT}" x="{c-m/2}" y="{c-m/2-2}" width="{m}" height="{m}"/>'
    return svg+'</svg>'

SHP="M32 4C46 4 58 12 58 28 58 46 45 58 32 64 19 58 6 46 6 28 6 12 18 4 32 4Z"
def shellsvg(stroke="var(--brand)",fill="none",dash="",ridges=True,sw=2):
    d=f' stroke-dasharray="{dash}"' if dash else ''
    r=f'<path d="M15 15Q32 23 49 15" fill="none" style="stroke:{stroke}" stroke-width="1.3" opacity=".55"/>' if ridges else ''
    return f'<svg class="sh" viewBox="0 0 64 68" preserveAspectRatio="none" aria-hidden="true"><path d="{SHP}" style="fill:{fill};stroke:{stroke}" stroke-width="{sw}"{d}/>{r}</svg>'
def shellbadge(col,icon,txt,cls="",off=False):
    return f'<div class="shl shb {cls}" style="--c:{"var(--muted)" if off else col}">{shellsvg("var(--border)" if off else col,"none","2 3" if off else "4 2.5")}{ic(icon)}<span>{txt}</span></div>'
def layers(): return '<div class="layers" aria-label="Paspoort 72% compleet"><span class="lay"><i class="f1"></i><i class="f2"></i><i class="f3"></i><i class="f4"></i><i></i></span><span>4 van 5 lagen · <a>aanvullen</a></span></div>'

def header(m):
    who='' if m else '<div style="text-align:right;margin-left:4px"><b style="display:block;font-size:14px;font-weight:600;color:var(--brand)">Samira El Amrani</b><small style="font-size:12px;color:var(--muted)">Kandidaat</small></div>'
    demo='' if m else '<span class="demo">Voorbeelddata</span>'
    return f'''<header class="hd"><div class="wm"><img src="{LOGO}" alt=""><b>Lobsy</b><span>Dichtbij genoeg om het pantser te laten vallen</span></div>{demo}
<div class="hr"><div class="cb" aria-label="Meldingen">{ic("bell")}</div><div class="lang" aria-label="Taal: Nederlands"><i class="flag"></i>NL {ic("chevd")}</div>{who}<div class="cb">{ic("user")}</div></div></header>'''
def nav():
    it=[("search","Zoeken",0),("heart","Bewaard",0),("clip","Sollicitaties",0),("sun","Carrière",0),("user","Profiel",1)]
    return '<nav class="bn">'+"".join(f'<a class="{"on" if o else ""}">{ic(i)}{t}</a>' for i,t,o in it)+'</nav>'

def passport():
    return f'''<aside class="left"><div class="card pp">
<div class="banner"><p class="t">LOBSY PASPOORT <span class="no" style="letter-spacing:0;font-weight:400;margin-left:6px">LB-04817</span></p>{helix(170,112)}<div class="shl stampw">{shellsvg("var(--gold-light)","none","3.5 2.5",True,1.8)}<span>GESTART<b>SEP ’26</b></span></div></div>
<div class="idrow"><div class="av"><div class="hair"></div><div class="face"></div><div class="body"></div></div><span class="av-edit" aria-label="Foto wijzigen">{ic("edit")}</span>
<div><h2>Samira El Amrani</h2></div></div>
<div class="ppb">
<div class="pills"><span class="pill ok">{ic("check")}Beschikbaar voor werk</span><span class="pill">Zorgzaam</span><span class="pill">Leert snel</span></div>
{layers()}
<div class="qt"><p class="lab">{ic("quote")}MIJN VERHAAL</p>“Ik help graag mensen en werk het liefst in een warm team. Ik wil groeien in de zorg.”</div>
<div class="boxes">
<div class="box"><p class="l">{ic("pin")}Ik woon in</p><b>Wateringen</b></div>
<div class="box"><p class="l">{ic("bike")}Reizen</p><b>Max 30 min · fiets</b></div>
<div class="box"><p class="l">{ic("clock")}Uren per week</p><b>16–24 uur</b></div>
<div class="box"><p class="l">{ic("cal")}Beschikbaar</p><b>Direct · avonden</b></div></div>
<div class="kv"><span>Wat ik zoek</span><span class="pills"><span class="pill">Zorg & welzijn</span><span class="pill">Horeca</span></span></div>
<div class="kv"><span>Rijbewijs</span><span class="pills"><span class="pill">B</span></span></div>
<div class="acts"><div class="btn pri">{ic("share")}Deel mijn paspoort</div>
<div class="row2"><div class="btn sm">{ic("doc")}Lobsy-CV</div><div class="btn sm">{ic("edit")}Aanpassen</div></div></div>
</div><p class="tagl">{ic("shell")}Lobsy: ontdek wie je bent onder de schaal</p></div></aside>'''

def mpassport():
    return f'''<div class="card mpp"><div class="top"><div class="av"><div class="hair"></div><div class="face"></div><div class="body"></div></div>
<div style="min-width:0"><h2>Samira El Amrani</h2><p class="line">Wateringen · fiets 30 min · 16–24 u</p><div class="pills" style="margin-top:6px"><span class="pill ok">{ic("check")}Beschikbaar</span><span class="pill">Zorgzaam</span></div></div></div>
<span class="mstamp shl">{shellsvg("var(--gold)","none","3 2.5",True,1.6)}<span>LOBSY<br>PASPOORT</span></span><span class="share" aria-label="Deel mijn paspoort">{ic("share")}</span>
<div class="comp">{layers()}</div></div>'''

STATS=[("var(--brand)","claw","Jouw sterkste klauw","Zorgzaam","Uitgebreid"),("var(--success)","compass","Werk dat bij je past","Helpen & maken","S-A-C"),("var(--gold)","home","Hier voel je je thuis","Een warm team","Quick-Scan"),("var(--warn)","heart","Dit vind je belangrijk","Verbinding","Voorlopig")]
def overview():
    st="".join(f'<div class="card stat" style="--c:{c}"><div class="ico">{ic(i)}</div><p class="k">{k}</p><p class="v">{v}</p><p class="t">{"" if t=="Voorlopig" else ic("check")}{t}</p></div>' for c,i,k,v,t in STATS)
    lg="".join(f'<span><i style="background:{c}"></i>{n}</span>' for c,n in COLS)
    ms="".join(f'<div class="ms" style="--c:{c}"><p class="k">{k}</p><p class="v">{v}</p></div>' for c,i,k,v,t in STATS)
    return f'''<section aria-labelledby="dbj"><h2 id="dbj" class="vh">Dit ben jij</h2><div class="ov">
<div class="card ring-card">{ring(150)}<div><h3>Dit ben jij</h3><p class="stg">Jouw kreeft<br><b>fase 3 van 4</b></p><div class="lg">{lg}</div></div></div>
<div class="stats">{st}</div>
<div class="card mdna"><div style="text-align:center">{ring(118)}<p class="stg">Jouw kreeft · <b>fase 3</b></p></div><div class="mstats">{ms}</div></div></div></section>'''

TABS=[("dna","dna","Mijn DNA"),("tests","clip","Mijn tests"),("fit","target","Past deze baan?"),("career","leaf","Carrière"),("proof","award","Bewijzen"),("data","gear","Mijn gegevens")]
def tabs(active):
    h=""
    for k,i,t in TABS:
        extra=' <span class="cnt">1</span>' if k=="tests" else ""
        h+=f'<a class="tab{" on" if k==active else ""}" role="tab" aria-selected="{"true" if k==active else "false"}">{ic(i)}{t}{extra}</a>'
    return f'<nav class="tabs" role="tablist" aria-label="Onderdelen van je paspoort">{h}</nav>'

def head(title, quote, right=""):
    return f'<div class="tabhead"><h2 class="vh">{title}</h2>{say(quote,40)}{right}</div>'

def dots(used): return "".join(f'<i class="{"u" if k<used else ""}"></i>' for k in range(3))

def tab_dna():
    return head("Mijn DNA","Je hebt net een laag afgeworpen. Dit is wie eronder zat.")+f'''
<div class="g3">
<div class="card pad"><p class="eb">{ic("shell")}Onder je schaal</p><h3 class="ct">Wat maakt jou jou</h3>
<div class="irow"><div class="ico">{ic("star")}</div><div><b>Je ziet wat anderen nodig hebben</b><p>En je doet er ook echt iets mee.</p></div></div>
<div class="irow"><div class="ico">{ic("bolt")}</div><div><b>Energie van werken met je handen</b><p>Praktisch werk waarmee je iemand helpt.</p></div></div>
<div class="irow"><div class="ico">{ic("people")}</div><div><b>Fijne collega’s zijn belangrijk</b><p>Een team waar je elkaar kent en helpt.</p></div></div></div>
<div class="card pad"><h3 class="ct">Jouw verhaal</h3><p class="sm" style="margin-top:8px;line-height:1.55">Je zorgt graag voor anderen en werkt het fijnst in een klein, warm team. Je houdt van duidelijke afspraken, maar denkt ook graag mee. Werk waarin je ziet dat je het verschil maakt, past bij jou.</p>
<div class="chips"><span class="chip">Zorgzaam</span><span class="chip">Praktisch</span><span class="chip">Betrouwbaar</span></div>
<p class="xs muted" style="margin-top:10px">Door Lobsy geschreven uit je tests · 24 sep 2026 · <a class="lnk" style="font-size:12px">Lees meer</a></p></div>
<div class="card pad"><h3 class="ct">Waar voel jij je thuis</h3>
<div class="sl"><div class="ends"><b>Samen</b><span class="muted">Zelfstandig</span></div><div class="track"><span class="knob" style="left:28%"></span></div></div>
<div class="sl"><div class="ends"><b>Vaste planning</b><span class="muted">Afwisseling</span></div><div class="track"><span class="knob" style="left:35%"></span></div></div>
<p class="sm b6" style="margin-top:14px">Waarden op werk <span class="xs muted" style="font-weight:400">· voorlopig</span></p>
<ul class="vals" style="margin-top:4px"><li><span>1</span>Verbinding & zorg</li><li><span>2</span>Zekerheid</li><li><span>3</span>Leren & groeien</li></ul></div></div>
<div class="card stampbar"><div class="ttl"><h3 class="ct">Mijn schalen</h3><p class="xs muted">Hier werp je je oude schaal af · 4 van 6</p></div>
{shellbadge("var(--success)","check","EERSTE<br>TEST","r1")}{shellbadge("var(--gold-deep)","file","EERSTE<br>RAPPORT","r2")}{shellbadge("var(--brand)","doc","CV<br>ERBIJ","r3")}{shellbadge("var(--success)","star","3 TESTS<br>GEDAAN","r1")}{shellbadge("","heart","DNA<br>COMPLEET","",True)}{shellbadge("","brief","EERSTE<br>BAAN","",True)}
<div class="only-d" style="margin-left:auto;max-width:210px"><p class="xs b6" style="color:var(--brand)">Groot worden doe je door je schaal af te werpen.</p><p class="xs muted">Nog 8 vragen tot je volgende schaal.</p></div></div>'''

def trow(ico,name,sub,steps,quota,cta,sel=False):
    mini="".join(f'<span class="{c}" style="--p:{p}%"><i></i>{l}</span>' for c,l,p in steps)
    q=f'<p class="q"><span class="dots">{dots(quota)}</span>{3-quota} van 3 aanpassingen over</p>' if quota is not None else '<p class="q">Nog 8 van 20 vragen</p>'
    return f'<div class="trow{" sel" if sel else ""}"><div class="ico">{ic(ico)}</div><div><b>{name}</b><small>{sub}</small></div><div><div class="mini">{mini}</div>{q}</div>{cta}</div>'

def courses(eyebrow,icon,opts,for_txt=""):
    o=""
    for k,(typ,title,meta,why) in enumerate(opts):
        pill=f'<span class="pill free">{ic("check")}Gratis</span>' if k==0 else '<span class="pill pl">Partnerlink</span>'
        o+=f'<div class="co{" first" if k==0 else ""}"><p class="tp">{pill}<b>{title}</b></p><small>{typ} · {meta}</small><p class="why">{ic("claw" if k==0 else "check")}{why}</p></div>'
    return f'<div class="gblk"><div class="gh"><p class="eb">{ic(icon)}{eyebrow}</p><p class="nt"><span style="white-space:nowrap">Partnerlink: Lobsy kan een vergoeding krijgen</span><span class="vpill">Voorbeelddata</span></p></div><div class="gopts">{o}</div></div>'

def tab_tests(mobile=False):
    rows=trow("star","Competentietest","Wat kun jij goed?",[("done","Quick-Scan",0),("done","Uitgebreid",0),("done","Rapport",0)],1,f'<a class="btn sm">{ic("dl")}Rapport</a>')
    rows+='<div class="trow grow">'+courses("Groei verder","leaf",[("Workshop","Slim plannen","2 uur · online · LeerPlein","Laat je klauw ‘Plannen’ groeien"),("Cursus","Plannen in de zorg","4 weken · Den Haag · ZorgStart","Met certificaat voor je Bewijzen")])+'</div>'
    rows+=trow("compass","Beroepentest","Welk werk past bij jou?",[("done","Quick-Scan",0),("now","Uitgebreid",0),("lock","Rapport",0)],0,'<a class="btn pri sm">Ga verder</a>',True)
    rows+=trow("home","Cultuur","Waar voel jij je thuis?",[("done","Quick-Scan",0),("todo","Uitgebreid",0),("lock","Rapport",0)],1,'<a class="btn sm">Uitgebreid</a>')
    rows+=trow("heart","Waarden op werk","Wat vind jij belangrijk?",[("now","Quick-Scan",60),("todo","Uitgebreid",0),("lock","Rapport",0)],None,'<a class="btn pri sm">Maak af</a>')
    names=["Radar vs gemiddelde","Holland-code uitgelegd","Beroepen die passen","Vergelijking met anderen","Jouw actieplan","Sterke punten & valkuilen"]
    cards="".join(f'<div class="lc"><b>{ic("lock")}{n}</b><div class="blur" aria-hidden="true"><i style="width:80%"></i></div><span class="vst">Voorbeelddata</span></div>' for n in names)
    lock=f'''<div class="card lockp"><div class="hd2"><h3 class="ct">Beroepentest · uitgebreid rapport</h3><span class="vpill">Voorbeelddata</span></div>
<p class="xs muted" style="margin-bottom:10px">Zo ziet je rapport eruit als je dieper duikt (120 vragen).</p>
<div class="lgrid">{cards}</div>
<div class="checks"><span>{ic("check")}14 pagina’s als PDF</span><span>{ic("check")}Gebruik bij sollicitaties</span><span>{ic("check")}Scherpere matches met vacatures</span></div>
<div style="display:flex;gap:8px"><a class="btn gold sm">{ic("wave")}Doe de uitgebreide test</a><a class="btn sm">{ic("file")}Voorbeeld-PDF</a></div></div>'''
    return head("Mijn tests","Niet zoeken aan de oppervlakte. Duik dieper.",'<a class="lnk only-d">Wat kost uitgebreid? '+ic("right")+'</a>')+f'<div class="g75"><div class="card tcard" style="overflow:hidden"><div class="depth"><span></span><span>Test</span><div class="dscale"><i></i><span>Oppervlakte<b style="font-weight:600;color:var(--brand)">Diep</b></span></div><span></span></div>{rows}</div>{lock}</div>'

def tab_fit():
    rows=[("ZW","Zorghulp thuiszorg","Zorghuis Wateringen · 8 min","Past goed"),("HZ","Helpende in opleiding (BBL)","Haagse Zorg · 18 min","Past goed"),("CS","Medewerker bediening","Café Stadshart · 22 min","Past")]
    li="".join(f'<li><span class="lgo">{a}</span><div><b>{b}</b><small>{c}</small></div><span class="pc">{d}</span></li>' for a,b,c,d in rows)
    return head("Past deze baan?","Je antennes wijzen deze kant op. Twijfel je? Typ een baan, ik kijk mee.")+f'''<div class="g75">
<div class="card pad"><div style="display:flex;justify-content:space-between;align-items:center"><h3 class="ct">Past deze baan bij mij?</h3><a class="lnk">Hele uitslag {ic("right")}</a></div><p class="sub">{ic("antenna")}Vind de omgeving waar jouw antennes tot rust komen.</p>
<div style="display:flex;gap:8px;margin-top:10px"><div class="inp">Bijv. verpleegkundige, juf, chauffeur…</div><a class="btn pri" style="height:42px">Check</a></div>
<div style="display:flex;justify-content:space-between;align-items:center;gap:8px 14px;flex-wrap:wrap;margin-top:10px"><p class="sm muted">Laatste check: <b class="b6" style="color:var(--text)">Helpende zorg & welzijn</b></p><a class="lnk">Vergelijkbare functies {ic("right")}</a></div>
<div class="fitres"><div class="fr"><p class="n">{ic("antenna")}1 · Wat je antennes zeggen</p><b>Past goed</b></div><div class="fr"><p class="n">{ic("claw")}2 · Klauwen die je al hebt</p><b>Zorgzaam, geduldig</b></div><div class="fr grow"><p class="n">{ic("claw")}3 · Klauw die nog groeit</p><b>Diploma niveau 2</b></div><div class="fr"><p class="n">{ic("leaf")}4 · Wat je kunt doen</p><b>Leren en werken (BBL)</b></div></div>
{courses("Laat je klauw groeien","claw",[("Cursus","Basis zorg & hygiëne","6 weken · online · LeerPlein","Eerste stap naar niveau 2"),("Opleiding","Helpende (BBL)","1 jaar · Den Haag · ZorgStart","Hiermee haal je diploma niveau 2")])}</div>
<div style="display:flex;flex-direction:column;gap:12px">
<div class="card cult" style="margin:0;padding:12px 16px">{ic("antenna")}<div><b>Cultuur: klein, warm team</b><small>Hier komen jouw antennes tot rust</small></div><span class="pc">Past bij jou</span></div>
<div class="card pad"><div style="display:flex;justify-content:space-between;align-items:center"><div><h3 class="ct">Vacatures die bij jou passen</h3><p class="sub">{ic("stone")}Niet de grootste steen, maar die bij jouw formaat past.</p></div><a class="lnk">Top 10 {ic("right")}</a></div><ul class="vac" style="margin-top:6px">{li}</ul>
<div style="display:flex;align-items:center;gap:12px;margin-top:6px;padding-top:10px;border-top:1px solid var(--pearl)"><div style="flex:1;min-width:0"><p class="sm b6" style="color:var(--brand)">Werkgevers die je willen spreken <span class="pill gold" style="margin-left:4px">1 nieuw</span></p><p class="xs muted">Zonder je naam of 06 · reageer binnen 48 uur</p></div><a class="btn sm">{ic("msg")}Bekijk</a></div></div>
</div></div>'''

def node(sz,fill,stroke,dash,inner,col,cls=""):
    return f'<div class="stn {cls}"><div class="dotw"><div class="shl" style="width:{sz}px;height:{sz*1.06:.0f}px;color:{col}">{shellsvg(stroke,fill,dash,sz>=34,2)}<span style="padding-top:2px">{inner}</span></div></div>'
def tab_career():
    return head("Carrière","Elke stap is een stukje nieuwe schaal dat aangroeit.",f'<a class="btn sm">{ic("edit")}Droombaan wijzigen</a>')+f'''
<div class="card pad"><div style="display:flex;justify-content:space-between;align-items:flex-start"><div><p class="xs muted">Mijn droombaan</p><p style="font-size:var(--text-xl);font-weight:600;color:var(--brand)">MBO-verpleegkundige</p></div><a class="lnk">Open mijn hele plan {ic("right")}</a></div>
<div class="stepper">{node(28,"var(--brand)","var(--brand)","",ic("check"),"#fff")}<b>Nu</b><span>Zorghulp</span></div>{node(34,"var(--accent-soft)","var(--brand)","4 2.5","2","var(--brand)","now")}<b>Groeit nu</b><span>Helpende niveau 2</span></div>{node(38,"var(--surface)","var(--border)","2 3","3","var(--muted)","")}<b>Stap 3</b><span>Verzorgende IG</span></div>{node(42,"var(--gold-soft)","var(--gold)","",ic("star"),"var(--gold-deep)","goal")}<b>Doel</b><span>Verpleegkundige</span></div></div></div>
<div class="g3" style="margin-top:12px">
<div class="card pad"><h3 class="ct">Wat je nog mist</h3><p class="sub">{ic("claw")}Welke klauwen je al hebt, en welke je nog laat groeien.</p><ul class="gap" style="margin-top:6px"><li>{ic("claw")}Diploma Helpende (niveau 2)</li><li>{ic("claw")}1 jaar ervaring in de zorg</li><li class="ok">{ic("check")}Zorgzaam, geduldig: heb je al</li></ul></div>
<div class="card pad"><div style="display:flex;justify-content:space-between;align-items:center;gap:8px"><h3 class="ct">Opleiding die past</h3><span class="vpill">Voorbeelddata</span></div>
<div class="course"><div class="co first"><p class="tp"><span class="pill free">{ic("check")}Gratis</span><b>Kennismaken met de zorg</b></p><small>Voorbeeld-Leerplein · online · 4 weken</small></div>
<div class="co"><p class="tp"><span class="pill pl">Partnerlink</span><b>Helpende (BBL)</b></p><small>Voorbeeld Opleidingen · leren en werken · 1 jaar</small></div></div>
<p class="xs muted" style="margin-top:6px">Partnerlink: Lobsy kan een vergoeding krijgen.</p></div>
<div class="card pad"><p class="eb">{ic("leaf")}Groei eerst. Match daarna.</p><h3 class="ct">Match op deze stap</h3><p class="sub" style="margin-top:6px">{ic("stone")}Deze steen past al bij jouw formaat.</p><p class="sm" style="margin-top:8px">Je past al <b class="b6">goed</b> bij Helpende. Met het diploma worden je matches sterker.</p><a class="lnk" style="margin-top:10px">Vacatures voor deze stap {ic("right")}</a></div></div>'''

def tab_proof():
    return head("Bewijzen","Dit is je nieuwe, sterkere schaal. Alles wat je deed telt, ook mantelzorg.")+f'''<div class="card hard"><div style="min-width:170px"><h3 class="ct">Je nieuwe schaal</h3><p class="xs muted">6 bewijzen · steeds steviger</p></div>
<span class="ends">Zacht</span><div class="segs" aria-label="6 van 8 bewijzen"><i class="f1" style="background:color-mix(in srgb,var(--brand) 25%,var(--accent-soft))"></i><i style="background:color-mix(in srgb,var(--brand) 40%,var(--accent-soft))"></i><i style="background:color-mix(in srgb,var(--brand) 55%,var(--accent-soft))"></i><i style="background:color-mix(in srgb,var(--brand) 70%,var(--accent-soft))"></i><i style="background:color-mix(in srgb,var(--brand) 85%,var(--accent-soft))"></i><i style="background:var(--brand)"></i><i></i><i></i></div><span class="ends">Hard</span>
<p class="xs muted only-d" style="margin-left:auto">Nog 1 recensie en 1 certificaat, dan is je schaal hard.</p></div>
<div class="g3">
<div class="card pad proof"><h3>{ic("brief")}Ervaring</h3><ul class="tl"><li><b>Zorghulp (vrijwillig)</b><span>Zorghuis Wateringen · 2025 – nu</span></li><li><b>Bediening</b><span>Restaurant Al Bahr, Casablanca · 2021 – 2024</span></li><li><b>Mantelzorg</b><span>Voor mijn oma · 2019 – 2021</span></li></ul><a class="add">{ic("plus")}Werkgever toevoegen</a></div>
<div class="card pad proof"><h3>{ic("cap")}Opleiding & certificaten</h3><div class="pills" style="margin-bottom:10px"><span class="pill">MBO niveau 1</span><span class="pill">Inburgering A2</span></div><ul class="tl"><li><b>Nederlands B1</b><span>Taalcursus · 2025</span></li><li><b>BHV / EHBO</b><span>2025</span></li></ul><a class="add">{ic("plus")}Certificaat toevoegen</a></div>
<div class="card pad proof"><h3>{ic("award")}Recensies & CV</h3><ul class="tl"><li><b>Marja de Vries</b><span>Teamleider · Zorghuis Wateringen</span></li></ul><a class="add">{ic("plus")}Recensie toevoegen (max 3)</a>
<div style="margin-top:12px;padding-top:12px;border-top:1px solid var(--pearl)"><p class="sm b6">Eigen CV</p><p class="xs muted">cv-samira-2026.pdf</p><div class="row2" style="margin-top:8px"><a class="btn sm">{ic("up")}Vervangen</a><a class="btn sm">{ic("dl")}Download</a></div></div></div></div>
<p class="xs muted" style="margin-top:10px">Je Lobsy-CV (PDF) maken we automatisch van je bewijzen en je tests.</p>'''

def tab_data():
    rows=[("user","Persoonlijk","Naam, telefoon, WhatsApp, geboortedatum, adres, apparaten",""),("bike","Voorkeuren & reistijd","Reistijd, vervoer, interesses, rijbewijzen",""),
          ("cal","Beschikbaarheid","Uren per week, snelkeuzes, dagdelen",""),("msg","Mijn motivatie","Over jezelf, algemene motivatie",""),
          ("shield","Privacy & toestemming","Tests en AI, talentpool, ouder/voogd",""),("trash","Account verwijderen","Je gegevens definitief wissen","danger")]
    acc="".join(f'<div class="card acc" role="button" aria-expanded="false"><span class="ico">{ic(i)}</span><div><b class="{c}">{t}</b><small>{s}</small></div><span class="sp"></span>{ic("chevd")}</div>' for i,t,s,c in rows)
    return head("Mijn gegevens","Jouw schaal, jouw regels. Jij bepaalt wie wat ziet.")+f'''
<div class="quick"><div class="card"><div style="flex:1"><b class="sm b6">Beschikbaar voor werk</b><p class="xs muted">Werkgevers zien dat je zoekt</p></div><span class="tg on" role="switch" aria-checked="true"></span></div>
<div class="card"><div style="flex:1"><b class="sm b6">Anoniem in de talentpool</b><p class="xs muted">Staat standaard uit</p></div><span class="tg" role="switch" aria-checked="false"></span></div>
<div class="card"><div style="flex:1"><b class="sm b6">Tests en AI-analyse</b><p class="xs muted">Toestemming sinds 12-09-2026</p></div><span class="pill ok">{ic("check")}Aan</span></div></div>
<div class="accg">{acc}</div>'''

TABF={"dna":tab_dna,"tests":tab_tests,"fit":tab_fit,"career":tab_career,"proof":tab_proof,"data":tab_data}
def page(active, mobile):
    body=f'''<div class="page">{passport()}<main>{mpassport()}{overview()}{tabs(active)}<section role="tabpanel">{TABF[active]()}</section></main></div>'''
    demo='<p class="demo" style="position:fixed;right:12px;bottom:72px;z-index:60">Voorbeelddata</p>' if mobile else ''
    return f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><title>Mijn Lobsy-paspoort</title><style>{CSS}</style></head><body class="{"m" if mobile else "d"}"><h1 class="vh">Mijn Lobsy-paspoort</h1>{header(mobile)}{demo}{body}{nav()}</body></html>'

D=[("pp-d1-mijn-dna","dna"),("pp-d2-mijn-tests","tests"),("pp-d3-past-deze-baan","fit"),("pp-d4-carriere","career"),("pp-d5-bewijzen","proof"),("pp-d6-mijn-gegevens","data")]
M=[("pp-m1-overzicht","dna",False),("pp-m2-mijn-tests","tests",True),("pp-m3-past-deze-baan","fit",True),("pp-m4-mijn-gegevens","data",True)]
with sync_playwright() as pw:
    b=pw.chromium.launch(executable_path="/usr/bin/google-chrome")
    pg=b.new_page(viewport={"width":1440,"height":900})
    for n,t in D:
        f=d/f"{n}.html"; f.write_text(page(t,False)); pg.goto(f.as_uri()); pg.wait_for_timeout(250)
        h=pg.evaluate("document.documentElement.scrollHeight"); tb=pg.evaluate("document.querySelector('.tabs').getBoundingClientRect().bottom")
        ov=pg.evaluate("()=>[...document.querySelectorAll('main *')].filter(e=>e.scrollWidth>e.clientWidth+1&&getComputedStyle(e).overflow!=='hidden'&&!['svg','path'].includes(e.tagName)).map(e=>e.className).slice(0,5)")
        full=h>905
        pg.screenshot(path=str(d/f"{n}.png"),full_page=full); print(n,"h",h,"tabsBottom",round(tb),ov)
    pm=b.new_page(viewport={"width":390,"height":844},device_scale_factor=2)
    for n,t,scroll in M:
        f=d/f"{n}.html"; f.write_text(page(t,True)); pm.goto(f.as_uri()); pm.wait_for_timeout(250)
        tb=pm.evaluate("document.querySelector('.tabs').getBoundingClientRect().top")
        if scroll: pm.evaluate("window.scrollTo(0, document.querySelector('.tabs').getBoundingClientRect().top+scrollY-56-1)")
        pm.evaluate("()=>{const t=document.querySelector('.tabs'),o=t.querySelector('.tab.on');t.scrollLeft=Math.max(0,o.offsetLeft-t.offsetLeft-60)}")
        pm.wait_for_timeout(100)
        W=pm.evaluate("()=>[...document.querySelectorAll('body *')].filter(e=>{const r=e.getBoundingClientRect();return r.right>391&&!e.closest('.tabs')&&!e.closest('.banner')}).map(e=>e.className).slice(0,5)")
        pm.screenshot(path=str(d/f"{n}.png")); print(n,"tabsTop",round(tb),W)
    b.close()
