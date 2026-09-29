C = r'''
.thdr{height:64px;background:var(--surface);border-bottom:1px solid var(--border)}
.thdr .wrap{height:100%;display:flex;align-items:center;gap:var(--space-4)}
.thdr .brand{color:var(--text)}.thdr .brand img{border:1px solid var(--border)}
.thdr .r{margin-inline-start:auto;display:flex;gap:var(--space-2);align-items:center}.thdr .lang{color:var(--muted)}
.tmain{padding:40px 0 56px}
.tgrid2{display:grid;grid-template-columns:minmax(0,1.2fr) minmax(0,.8fr);gap:var(--space-5);align-items:start}
.tcard{padding:32px}
.tcard h1{font-size:var(--text-2xl);font-weight:700;line-height:1.25;margin-top:14px}
.facts{display:flex;gap:10px;margin-top:18px;flex-wrap:wrap}
.blocks{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:var(--space-3);margin-top:24px}
.blk{display:flex;gap:12px;padding:14px;border-radius:var(--radius-sm);background:var(--pearl)}
.blk h4{font-size:var(--text-md);font-weight:600}.blk p{font-size:var(--text-sm);color:var(--muted)}
.chk{display:flex;gap:12px;align-items:flex-start;margin-top:14px;font-size:var(--text-md)}
.chk .b{width:22px;height:22px;border-radius:6px;border:1.5px solid var(--border);background:var(--surface);flex:none;display:grid;place-items:center;margin-top:1px}
.chk .b.on{background:var(--brand);border-color:var(--brand);color:var(--surface)}.chk .b .i{width:15px;height:15px;stroke-width:2.6}
.hr{height:1px;background:var(--border);margin:24px 0 10px}
.side-scene{background:var(--brand-deep);color:var(--surface);border-radius:var(--radius);padding:28px;position:relative;overflow:hidden;min-height:520px}
.side-scene .bub{background:var(--surface);color:var(--text);border-radius:var(--radius);padding:14px 16px;font-weight:600;max-width:260px;position:relative;box-shadow:var(--shadow-lg)}
.side-scene .bub::after{content:"";position:absolute;bottom:-8px;inset-inline-start:36px;border:8px solid transparent;border-bottom:0;border-top-color:var(--surface)}
.side-scene .masc{width:190px;margin-top:18px;margin-inline-start:20px}
.side-scene .after{position:absolute;inset-inline:28px;bottom:28px;background:color-mix(in srgb,var(--surface) 8%,transparent);border-radius:var(--radius);padding:16px}
.side-scene .after h4{font-size:var(--text-sm);font-weight:600}.side-scene .after p{font-size:var(--text-sm);color:var(--on-dark);margin-top:4px}
.qwrap{max-width:760px;margin:0 auto}
.qtop{display:flex;align-items:center;gap:var(--space-3);font-size:var(--text-sm);color:var(--muted);font-weight:600}
.qtop .sp{flex:1}.qtop a{display:inline-flex;align-items:center;gap:6px;color:var(--brand)}
.prog{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;margin-top:14px}
.prog div{font-size:var(--text-xs);color:var(--muted);font-weight:600}
.prog b{display:block;height:8px;border-radius:var(--radius-pill);background:var(--pearl-mid);margin-bottom:6px;position:relative;overflow:hidden}
.prog b::after{content:"";position:absolute;inset:0 auto 0 0;width:var(--w);background:var(--brand);border-radius:inherit}
.prog .on{color:var(--brand)}
.qcard{margin-top:28px;padding:40px}
.qcard .blab{display:flex;align-items:center;gap:10px}
.qcard h1{font-size:var(--text-2xl);font-weight:700;line-height:1.3;margin-top:18px}
.lik{display:grid;grid-template-columns:repeat(5,1fr);gap:10px;margin-top:32px}
.lik span{height:64px;border-radius:var(--radius-sm);border:1.5px solid var(--border);background:var(--surface);display:grid;place-items:center;font-size:var(--text-xl);font-weight:600;color:var(--text)}
.lik span.sel{border-color:var(--brand);background:var(--accent-soft);color:var(--brand)}
.likl{display:flex;justify-content:space-between;margin-top:10px;font-size:var(--text-sm);color:var(--muted);font-weight:600}
.tip{display:flex;align-items:center;gap:14px;margin-top:28px;padding:14px 16px;background:var(--pearl);border-radius:var(--radius-sm);font-size:var(--text-sm)}
.tip img{width:48px;height:48px}
.qfoot{display:flex;align-items:center;gap:8px;justify-content:center;margin-top:18px;font-size:var(--text-sm);color:var(--muted)}.qfoot .i{width:16px;height:16px}
.rgrid{display:grid;grid-template-columns:minmax(0,1fr) 380px;gap:var(--space-5);align-items:start}
.rhead h1{font-size:var(--text-display);font-weight:700;line-height:1.1;letter-spacing:-.02em;margin-top:12px;display:flex;align-items:center;gap:16px}
.rhead h1 img{width:64px}
.tiles{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:var(--space-3);margin-top:24px}
.tile{padding:20px}
.tile small{font-size:var(--text-xs);font-weight:600;color:var(--muted);display:flex;align-items:center;gap:8px}
.tile h3{font-size:var(--text-xl);font-weight:600;margin-top:6px}
.tile p{font-size:var(--text-sm);color:var(--muted);margin-top:4px}
.tile .chips{display:flex;gap:6px;margin-top:12px;flex-wrap:wrap}
.lockd{margin-top:var(--space-4);padding:20px;position:relative;overflow:hidden}
.lockd .blurred{filter:blur(3px);opacity:.55;pointer-events:none}
.lockd .ov{position:absolute;inset:0;display:grid;place-items:center}
.lockd .ov span{display:inline-flex;align-items:center;gap:8px;background:var(--surface);border:1px solid var(--border);border-radius:var(--radius-pill);padding:8px 14px;font-weight:600;font-size:var(--text-sm);box-shadow:var(--shadow-lg)}
.signup{padding:24px;box-shadow:var(--shadow-lg);border:0}
.signup h2{font-size:var(--text-xl);font-weight:600;line-height:1.3}
.unl{list-style:none;margin-top:14px;display:flex;flex-direction:column;gap:9px;font-size:var(--text-sm)}
.unl li{display:flex;gap:10px;align-items:center}.unl .i{width:16px;height:16px;color:var(--brand)}
.prov{display:flex;flex-direction:column;gap:10px;margin-top:18px}
.prov .btn{justify-content:flex-start;gap:12px}
.carry{display:flex;gap:10px;align-items:center;margin-top:14px;padding:10px 12px;border-radius:var(--radius-sm);background:var(--success-soft);color:var(--success);font-size:var(--text-sm);font-weight:600}
.carry .i{width:16px;height:16px}
.rfoot{display:flex;gap:10px;align-items:center;margin-top:var(--space-4);flex-wrap:wrap}
'''
