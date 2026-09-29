D = r'''
body.m{font-size:var(--text-md)}
.m .mh{height:56px;background:var(--brand-deep);color:var(--surface);display:flex;align-items:center;gap:10px;padding:0 var(--space-4)}
.m .mh .brand{font-size:var(--text-md)}.m .mh .brand img{width:30px;height:30px}
.m .mh .r{margin-inline-start:auto;display:flex;align-items:center;gap:4px}
.m .ib{width:44px;height:44px;display:grid;place-items:center;border-radius:var(--radius-sm)}
.m .mh a.t{height:44px;display:inline-flex;align-items:center;padding:0 10px;font-weight:600;font-size:var(--text-sm)}
.m .mhero{background:var(--brand-deep);color:var(--surface);padding:20px var(--space-4) 0;overflow:hidden}
.m .mhero h1{font-size:var(--text-display);font-size:2.125rem;line-height:1.12;font-weight:700;letter-spacing:-.02em;margin-top:14px}
.m .mhero h1 em{font-style:normal;color:var(--coral)}
.m .mhero .lead{color:var(--on-dark);font-size:var(--text-md);margin-top:12px}
.m .mcta{display:flex;flex-direction:column;gap:10px;margin-top:20px}
.m .mcta .btn{height:52px;font-size:var(--text-lg)}
.m .mmicro{display:flex;gap:14px;margin-top:14px;color:var(--on-dark);font-size:var(--text-xs);flex-wrap:wrap}.m .mmicro span{display:inline-flex;gap:5px;align-items:center}.m .mmicro .i{width:14px;height:14px}
.m .mscene{position:relative;height:300px;margin:8px calc(-1 * var(--space-4)) 0}
.m .mscene > svg{position:absolute;inset:0;width:100%;height:100%}
.m .mscene .lob{position:absolute;width:150px;left:50%;top:48%;transform:translate(-50%,-50%)}
.m .mscene .shell{position:absolute;width:150px;left:50%;top:52%;transform:translate(-95%,-40%) rotate(-14deg);filter:brightness(0) invert(1);opacity:.1}
.m .mscene .chip{padding:8px 12px;font-size:var(--text-xs)}
.m .peek{padding:24px var(--space-4)}
.m .peek h2{font-size:var(--text-xl);font-weight:600;margin-top:4px}
.m .mt2{background:var(--surface);border-bottom:1px solid var(--border);height:56px;display:flex;align-items:center;gap:8px;padding:0 var(--space-2) 0 var(--space-4)}
.m .mt2 img{width:28px;height:28px;border-radius:7px;border:1px solid var(--border)}
.m .mt2 b{font-size:var(--text-sm)}.m .mt2 .sp{flex:1}
.m .mq{padding:16px var(--space-4) 24px}
.m .mq .prog{margin-top:0}.m .mq .prog div{font-size:var(--text-xs)}
.m .mq .qcard{padding:22px;margin-top:18px}
.m .mq h1{font-size:var(--text-xl);font-weight:700;line-height:1.35;margin-top:14px}
.m .lik{gap:8px;margin-top:24px}.m .lik span{height:56px}
.m .tip{margin-top:20px}.m .tip img{width:40px;height:40px}
.m .mnav{display:flex;gap:10px;margin-top:18px}.m .mnav .btn{flex:1;height:48px}
.m .mr{padding:18px var(--space-4) 110px}
.m .mr h1{font-size:2.125rem;font-weight:700;line-height:1.1;letter-spacing:-.02em;display:flex;align-items:center;gap:12px;margin-top:12px}
.m .mr h1 img{width:48px}
.m .tiles{grid-template-columns:1fr;gap:10px;margin-top:18px}
.m .tile{padding:16px}.m .tile h3{font-size:var(--text-lg)}
.m .signup{margin-top:var(--space-4);padding:20px}
.m .sticky{position:fixed;inset:auto 0 0 0;background:var(--surface);border-top:1px solid var(--border);padding:12px var(--space-4) calc(12px + env(safe-area-inset-bottom));box-shadow:var(--shadow-lg)}
.m .sticky .btn{height:52px}
'''
D += r'''
.m .scrim{position:fixed;inset:0;background:color-mix(in srgb,var(--brand-deep) 45%,transparent)}
.m .sheet{position:fixed;inset:auto 0 0 0;background:var(--surface);border-radius:var(--radius) var(--radius) 0 0;padding:10px var(--space-4) calc(20px + env(safe-area-inset-bottom));box-shadow:var(--shadow-lg)}
.m .sheet .grab{display:block;width:40px;height:4px;border-radius:2px;background:var(--border);margin:0 auto 14px}
.m .sheet h2{font-size:var(--text-xl);font-weight:600;line-height:1.3}
.m .sheet .prov .btn{height:48px}
'''
