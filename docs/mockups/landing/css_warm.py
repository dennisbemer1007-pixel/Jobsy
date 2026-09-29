"""Warm/sunny theme overrides (Dennis 29-09 22:10). Only existing tokens, mixed via color-mix; deviations listed in README."""
W = r'''
:root{--cream:var(--warn-soft);--sun:color-mix(in srgb,var(--gold) 26%,var(--surface));--sun-2:color-mix(in srgb,var(--gold) 42%,var(--surface));
--peach:color-mix(in srgb,var(--coral) 12%,var(--surface));--peach-2:color-mix(in srgb,var(--coral) 22%,var(--surface));
--sky:var(--accent-soft);--sky-2:color-mix(in srgb,var(--brand) 16%,var(--surface));--mint:var(--success-soft);--mint-2:color-mix(in srgb,var(--success) 18%,var(--surface));
/* PROPOSED landing-only radii */ --radius-lg:24px;--radius-xl:36px;--shadow-soft:0 10px 30px color-mix(in srgb,var(--coral) 10%,transparent),0 2px 6px rgba(15,45,92,.05)}
body{background:var(--bg)}
.emo{display:inline-grid;place-items:center;width:48px;height:48px;border-radius:50%;font-size:24px;line-height:1;flex:none;font-family:"Noto Color Emoji",sans-serif}
.emo.s{width:36px;height:36px;font-size:18px}.emo.l{width:60px;height:60px;font-size:30px}
.t-peach{background:var(--peach)}.t-sun{background:var(--sun)}.t-sky{background:var(--sky)}.t-mint{background:var(--mint)}.t-cream{background:var(--cream)}
.e{font-family:"Noto Color Emoji",sans-serif;font-style:normal}
.btn{border-radius:var(--radius-pill);flex-shrink:0}
.btn.pri{box-shadow:0 6px 16px color-mix(in srgb,var(--brand) 25%,transparent)}
.card{border:0;border-radius:var(--radius-lg);box-shadow:var(--shadow-soft)}
.pill{background:var(--peach);color:var(--text)}
.pill.ok{background:var(--mint);color:var(--success)}.pill.wn{background:var(--sun);color:var(--warn)}.pill.line{background:var(--surface);border-color:var(--border)}
.eyebrow{color:var(--coral);font-weight:600}
h2.sec{font-size:2.25rem;font-weight:700;letter-spacing:-.02em}
/* header on warm */
.hdr{background:var(--cream);color:var(--text);height:76px}
.hdr .brand img{background:var(--surface);box-shadow:var(--shadow)}
.nav a{color:var(--muted)}.nav a.on{color:var(--text);background:var(--surface)}
.lang{color:var(--muted)}
.hdr .btn.ondark{border-color:var(--border);color:var(--text);background:var(--surface)}
.hdr .btn.onpri{background:var(--brand);border-color:var(--brand);color:var(--surface)}
/* hero: sunny */
.hero{background:var(--cream);color:var(--text);padding:24px 0 120px}
.hero h1{color:var(--text);font-size:3.25rem}
.hero .lead{color:var(--muted)}
.hero .lead b{color:var(--text)!important}
.hero .btn.onpri{background:var(--brand);border-color:var(--brand);color:var(--surface);box-shadow:0 8px 20px color-mix(in srgb,var(--brand) 25%,transparent)}
.hero .btn.ondark{background:var(--surface);border-color:var(--border);color:var(--text)}
.hero .micro{color:var(--muted)}
.hero .pill.dark{background:var(--surface);color:var(--text);box-shadow:var(--shadow);height:32px;padding:0 14px;font-size:var(--text-sm)}
.wave{display:block;width:100%;height:80px}
.wave-wrap{position:relative;margin-top:-80px;line-height:0}
.blob{position:absolute;pointer-events:none}
.bubble{position:absolute;background:var(--surface);color:var(--text);border-radius:22px;padding:12px 16px;font-weight:600;box-shadow:var(--shadow-soft);white-space:nowrap;font-size:var(--text-md)}
.bubble::after{content:"";position:absolute;bottom:-9px;inset-inline-start:30px;border:10px solid transparent;border-bottom:0;border-top-color:var(--surface)}
.bubble.r::after{inset-inline-start:auto;inset-inline-end:30px}
.chip{border-radius:18px;box-shadow:var(--shadow-soft)}
.dib{border-radius:var(--radius-lg);box-shadow:var(--shadow-soft)}
.bar b{background:var(--pearl-mid)}.bar b::after{background:var(--coral)}
.bar.t2 b::after{background:var(--gold)}.bar.t3 b::after{background:var(--success)}.bar.t4 b::after{background:var(--brand)}
/* strip -> friendly chips */
.hi-for{display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin-top:26px}
.hi-for>span{display:inline-flex;align-items:center;gap:8px;height:40px;padding:0 14px 0 6px;border-radius:var(--radius-pill);background:var(--surface);box-shadow:var(--shadow);font-size:var(--text-sm);font-weight:600}
.hi-for>span .emo{width:30px;height:30px;font-size:16px}
.hi-for>b{font-size:var(--text-sm);color:var(--muted);font-weight:600;margin-inline-end:4px}
section{position:relative}
/* steps */
.step{padding:28px}.step h3{font-size:var(--text-xl)}
/* kreeft = the sea */
.kreeft{background:var(--sky);padding:40px 0 110px}
.story{background:var(--peach);color:var(--text);border-radius:var(--radius-xl);min-height:520px;padding:36px}
.story h3{font-size:2rem;font-weight:700;letter-spacing:-.01em}
.story p{color:var(--text);opacity:.82}
.story .q::before{border-top-color:var(--coral)}
.mt{flex-direction:column;gap:0;padding:0;overflow:hidden}
.mt .mtb{padding:16px 18px 18px}
.mt h4{font-size:var(--text-lg)}
.mt .lnk{color:var(--brand)}
.msc{position:relative;height:118px;overflow:hidden}
.msc svg{position:absolute;inset:0;width:100%;height:100%}
.msc img{position:absolute}
/* krijgt */
.showmap,.showpp{border-radius:var(--radius-xl);box-shadow:var(--shadow-soft)}
.fc .demo{background:var(--cream);border-radius:18px}
.fc h4{font-size:var(--text-lg)}
.mapc{background:var(--cream)}
.tabs span{border-radius:var(--radius-pill)}
.tabs span.on{background:var(--coral);border-color:var(--coral)}
.seg{border-radius:var(--radius-pill)}.seg span{border-radius:var(--radius-pill)}.seg span.on{background:var(--peach);color:var(--text)}
.swipe .sc.front{border-radius:20px}
.pc{color:var(--coral)!important}
/* who */
.wc{padding:0;overflow:hidden}.wc .wtop{height:92px;position:relative;display:flex;align-items:flex-end;padding:0 22px}
.wc .wtop .emo{transform:translateY(24px);box-shadow:0 0 0 5px var(--surface);background:var(--surface)}
.wc .wb{padding:34px 22px 22px;display:flex;flex-direction:column;gap:10px;flex:1}
.wc.me{border:0;outline:3px solid var(--coral);outline-offset:-3px}
/* trust */
.trust{background:var(--surface)}
.tg{background:var(--cream);border-radius:var(--radius-lg);padding:22px}
/* faq */
.fq{border-bottom:0;background:var(--surface);border-radius:18px;padding:16px 20px;margin-bottom:10px;box-shadow:var(--shadow)}
.fq.open{background:var(--cream);box-shadow:none}
/* final + footer */
.final{background:var(--sun);color:var(--text);border-radius:var(--radius-xl);padding:40px 48px;min-height:220px}
.final p{color:var(--muted)}
.final h2{font-size:2rem;font-weight:700}
.final .btn.onpri{background:var(--brand);border-color:var(--brand);color:var(--surface)}
.final .btn.ondark{background:var(--surface);border-color:var(--surface);color:var(--text)}
.ftr{background:var(--pearl-mid);color:var(--muted);padding-top:36px}
.fg h5{color:var(--text)}
.ftr .brand{color:var(--text)!important}
.fb{border-top-color:var(--border)}
/* test flow */
.thdr{background:var(--cream);border-bottom:0}
.tmain{background:var(--cream);min-height:calc(100vh - 64px)}
.tcard,.qcard,.tile,.signup{border-radius:var(--radius-xl)}
.blk{background:var(--cream);border-radius:18px}
.side-scene{background:var(--sky);color:var(--text);border-radius:var(--radius-xl)}
.side-scene .after{background:var(--surface);box-shadow:var(--shadow)}
.side-scene .after p{color:var(--muted)}
.lik span{border-radius:20px;height:84px;display:flex;flex-direction:column;gap:4px;justify-content:center;align-items:center}
.lik span .e{font-size:22px}
.lik span.sel{border-color:var(--coral);background:var(--peach);color:var(--text)}
.prog b{height:10px}.prog b::after{background:var(--coral)}.prog .on{color:var(--text)}
.tip{background:var(--sky);border-radius:18px}
.tile{padding:0;overflow:hidden}.tile .ttop{padding:16px 20px 12px;display:flex;align-items:center;gap:12px}.tile .tb2{padding:0 20px 20px}
.tile small{font-size:var(--text-sm);color:var(--text)}
.carry{border-radius:var(--radius-pill)}
.prov .btn{border-radius:var(--radius-pill);flex-shrink:0}
.chk .b.on{background:var(--coral);border-color:var(--coral)}
.lockd{background:var(--surface)}
/* mobile */
.m .mh{background:var(--cream);color:var(--text)}
.m .mhero{background:var(--cream);color:var(--text);padding-bottom:10px}
.m .mhero h1{color:var(--text)}
.m .mhero .lead{color:var(--muted)}.m .mhero .lead b{color:var(--text)!important}
.m .mhero .btn.onpri{background:var(--brand);border-color:var(--brand);color:var(--surface)}
.m .mhero .btn.ondark{background:var(--surface);border-color:var(--border);color:var(--text)}
.m .mmicro{color:var(--muted)}
.m .mt2{background:var(--cream);border-bottom:0}
.m .mq,.m .mr{background:var(--cream);min-height:calc(100vh - 56px)}
.m .lik span{height:70px;border-radius:16px}
.m .sheet{border-radius:28px 28px 0 0}
.m .peek{background:var(--bg)}
.m .mhero{padding-bottom:70px}
.m .mhero .pill.dark{background:var(--surface);color:var(--text);box-shadow:var(--shadow);height:32px}
.ppc-cc{display:flex;flex-direction:column;gap:6px;align-items:flex-start;min-width:0;flex:1 1 auto}
.ppc-cc .pill{max-width:100%;white-space:nowrap}
.m-z .ppc-cc .pill{font-size:11px;padding:3px 8px}
.m .ppc-h b{font-size:var(--text-sm)}
'''
W += r'''
.wscene{position:relative;height:540px}
.wscene > img{position:absolute}
.wscene > img.hm{filter:drop-shadow(0 14px 18px color-mix(in srgb,var(--coral) 25%,transparent))}
.wscene .shell-tag{position:absolute;font-size:var(--text-xs);color:var(--muted);font-weight:600;background:var(--surface);border-radius:var(--radius-pill);padding:3px 10px;box-shadow:var(--shadow)}
.wscene .zpp{position:absolute}
.wscene .chip{position:absolute}
.wscene .dib{position:absolute;width:250px}
.wscene.m{height:320px;margin:10px calc(-1 * var(--space-4)) 0}
.wscene.m .chip{padding:8px 12px;font-size:var(--text-xs)}
.ppc{border-radius:var(--radius-lg);box-shadow:var(--shadow-soft)}
.ppc-h{background:var(--peach);color:var(--text)}
'''
W += r'''
.story{position:relative;min-height:640px}
.story .sscene{position:absolute;inset:0;pointer-events:none}
.story .sscene img{position:absolute}.story .sscene .blob{position:absolute}
.story .sscene .bubble{font-size:var(--text-sm)}
.story p{max-width:360px}
'''
W += r'''
.side-scene{padding:28px;min-height:560px}
.ss-art{position:relative;height:330px}.ss-art .blob,.ss-art img{position:absolute}
.ss-art .bb{position:absolute;border-radius:50%;border:2px solid var(--surface)}
.side-scene .after h4{display:flex;align-items:center;gap:6px}
.rh-art{position:relative;width:84px;height:84px;display:inline-block;flex:none}
.rh-art img{position:relative;width:84px!important}
.rhead h1{font-size:3rem}
.unl li .e{font-size:16px;width:20px;text-align:center}
.qtop .e{font-size:15px}
.m .mr h1 img{width:56px}
.m .sheet .unl{gap:7px}
.m .mt2 b .e{font-size:14px}
'''
W += '.wheel{flex:none}'
