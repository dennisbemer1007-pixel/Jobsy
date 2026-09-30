Z = r'''
.zscene{position:relative;height:540px}
.zscene .zbg{position:absolute;inset:0;width:100%;height:100%}
.zold{position:absolute;width:190px;left:0;top:10px;transform:rotate(-22deg);filter:brightness(0) invert(1);opacity:.12}
.zold-l{position:absolute;left:18px;top:212px;font-size:var(--text-xs);color:var(--on-dark);display:flex;align-items:center;gap:6px}
.zold-l::before{content:"";width:22px;border-top:1px dashed var(--on-dark)}
.zpp{position:absolute;left:190px;top:92px;transform:rotate(-2.5deg)}
.znew{position:absolute;width:210px;left:40px;top:262px;filter:drop-shadow(0 16px 24px rgba(0,0,0,.35))}
.ppc{background:var(--surface);color:var(--text);border-radius:var(--radius);box-shadow:var(--shadow-lg);overflow:hidden}
.ppc-h{display:flex;align-items:center;gap:10px;padding:12px 16px;background:var(--accent-soft);color:var(--brand);font-size:var(--text-sm)}
.ppc-h img{width:24px;height:24px;border-radius:6px;background:var(--surface);padding:1px}.ppc-h .sp{flex:1}
.ppc-b{padding:16px}
.ppc-g{display:flex;align-items:center;gap:16px}
.dlg{display:flex;flex-direction:column;gap:8px;flex:1;min-width:0}
.lg-r{display:flex;align-items:center;gap:8px;font-size:var(--text-xs);color:var(--muted)}
.lg-r i{width:10px;height:10px;border-radius:3px;flex:none}.lg-r span{flex:1;white-space:nowrap}.lg-r b{color:var(--text)}
.ppc-c{display:flex;gap:6px;flex-wrap:wrap;margin-top:14px;padding-top:12px;border-top:1px solid color-mix(in srgb,var(--border) 60%,transparent)}
.zscene .chip .i{color:var(--success)}
.zscene .chip:last-child .i{color:var(--brand)}
.m-z{height:330px;margin:8px calc(-1 * var(--space-4)) 0}
.m-z .zold{width:120px;left:6px;top:6px}
.m-z .zpp{left:118px;top:36px}
.m-z .znew{width:130px;left:14px;top:150px}
.m-z .ppc-h{padding:8px 12px}.m-z .ppc-b{padding:12px}.m-z .ppc-g{gap:10px}
.m-z .chip{padding:8px 12px;font-size:var(--text-xs)}
/* paspoort showcase (replaces the map showpiece) */
.showpp{margin-top:32px;display:grid;grid-template-columns:260px minmax(0,1fr);overflow:hidden;box-shadow:var(--shadow-lg);border:0}
.spnav{padding:20px;border-inline-end:1px solid var(--border);display:flex;flex-direction:column;gap:4px}
.spnav h3{font-size:var(--text-lg);font-weight:600;display:flex;align-items:center;gap:8px;margin-bottom:10px}
.spnav a{height:40px;display:flex;align-items:center;gap:10px;padding:0 12px;border-radius:var(--radius-sm);font-size:var(--text-sm);color:var(--text)}
.spnav a .i{color:var(--muted)}
.spnav a.on{background:var(--accent-soft);color:var(--brand);font-weight:600}.spnav a.on .i{color:var(--brand)}
.spnav .spnote{margin-top:auto;font-size:var(--text-xs);color:var(--muted);padding:12px 4px 0}
.spmain{padding:24px;display:grid;grid-template-columns:auto minmax(0,1fr);gap:28px;align-items:start;background:var(--pearl)}
.spmain .sum h4{font-size:var(--text-xl);font-weight:600}
.spmain .sum p{color:var(--muted);margin-top:4px}
.spmain .rows{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:var(--space-3);margin-top:16px}
.spmain .rw{background:var(--surface);border-radius:var(--radius-sm);padding:14px}
.spmain .rw small{font-size:var(--text-xs);font-weight:600;color:var(--muted);display:flex;align-items:center;gap:6px}.spmain .rw small .i{width:14px;height:14px}
.spmain .rw b{display:block;margin-top:4px}
.fit{display:flex;flex-direction:column;gap:8px}
.fit .occ{display:flex;align-items:center;gap:10px;background:var(--surface);border-radius:var(--radius-sm);padding:8px 10px;font-size:var(--text-sm)}
.fit .occ b{margin-inline-start:auto;color:var(--brand)}
.fit .occ .m{flex:1;height:6px;border-radius:3px;background:var(--pearl-mid);position:relative;overflow:hidden;max-width:60px}
.fit .occ .m::after{content:"";position:absolute;inset:0 auto 0 0;width:var(--w);background:var(--brand)}
.path{display:flex;flex-direction:column;gap:0}
.path div{display:flex;gap:10px;align-items:flex-start;font-size:var(--text-sm);position:relative;padding-bottom:12px}
.path div::before{content:"";position:absolute;inset-inline-start:10px;top:22px;bottom:0;border-inline-start:2px dashed var(--border)}
.path div:last-child::before{display:none}.path div:last-child{padding-bottom:0}
.path i{width:22px;height:22px;border-radius:50%;flex:none;display:grid;place-items:center;background:var(--surface);border:2px solid var(--border);font-style:normal}
.path .now i{border-color:var(--brand);background:var(--brand)}
.path .goal i{border-color:var(--brand);color:var(--brand)}.path .goal i .i{width:12px;height:12px}
.path small{display:block;color:var(--muted);font-size:var(--text-xs)}
'''
