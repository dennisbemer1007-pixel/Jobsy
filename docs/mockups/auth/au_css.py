"""Auth mockups (au-*): tokens 1:1 from Jobsy.Web/wwwroot/css/app.css :root + warm public theme (docs/landing css_warm)."""
CSS = r'''
:root{--pearl:#f7f4f0;--pearl-mid:#efe9e3;--bg:#f5f2ee;--surface:#fffcfa;--text:#122033;--muted:#5a6a7d;--border:#ddd5cc;
--brand:#0f2d5c;--brand-deep:#0a2044;--accent-soft:#e7eef7;--coral:#f54a1b;--coral-deep:#d93a12;--coral-soft:#ffe8e0;
--danger:#9b1c1c;--danger-soft:#fef2f2;--success:#15803d;--success-soft:#ecfdf3;--gold:#c9a227;--gold-ink:#5c4a0f;--warn:#a65b00;--warn-soft:#fff8e7;
--radius:12px;--radius-sm:8px;--radius-lg:24px;--radius-xl:36px;--radius-pill:999px;
--text-xs:.75rem;--text-sm:.875rem;--text-base:1rem;--text-lg:1.125rem;--text-xl:1.375rem;--text-2xl:1.75rem;
--cream:var(--warn-soft);--sun:color-mix(in srgb,var(--gold) 26%,var(--surface));--peach:color-mix(in srgb,var(--coral) 12%,var(--surface));
--peach-2:color-mix(in srgb,var(--coral) 22%,var(--surface));--sky:var(--accent-soft);--mint:var(--success-soft);
--shadow:0 1px 2px rgba(15,45,92,.06);
--shadow-soft:0 10px 30px color-mix(in srgb,var(--coral) 10%,transparent),0 2px 6px rgba(15,45,92,.05);
--font:Inter,"Segoe UI","Helvetica Neue",Arial,sans-serif}
*{box-sizing:border-box}html,body{margin:0}
body{font-family:var(--font);color:var(--text);background:var(--cream);font-size:16px;line-height:1.5;-webkit-font-smoothing:antialiased}
[dir=rtl] body,body[dir=rtl]{font-family:"Noto Sans Arabic",Inter,sans-serif}
.i{width:18px;height:18px;fill:none;stroke:currentColor;stroke-width:2;stroke-linecap:round;stroke-linejoin:round;flex:none}
a{color:var(--brand);font-weight:600;text-decoration:underline;text-underline-offset:.15em}
/* header */
.pub-header{height:76px;display:flex;align-items:center;gap:16px;padding:0 48px;background:var(--cream);position:relative;z-index:2}
.pub-header .brand{display:flex;align-items:center;gap:10px;font-weight:700;font-size:1.25rem;color:var(--text);text-decoration:none}
.pub-header .brand img{width:36px;height:36px;border-radius:10px;background:var(--surface);box-shadow:var(--shadow)}
.pub-header .sp{flex:1}
.pub-lang{display:inline-flex;align-items:center;gap:6px;height:40px;padding:0 14px;border-radius:var(--radius-pill);background:transparent;color:var(--text);font-weight:600;font-size:var(--text-sm);border:0}
.pub-btn{display:inline-flex;align-items:center;justify-content:center;gap:8px;min-height:44px;padding:0 20px;border-radius:var(--radius-pill);font-weight:600;font-size:var(--text-base);text-decoration:none;border:1.5px solid transparent;font-family:inherit;cursor:pointer}
.pub-btn--primary{background:var(--brand);color:var(--surface);box-shadow:0 6px 16px color-mix(in srgb,var(--brand) 25%,transparent)}
.pub-btn--secondary{background:var(--surface);color:var(--text);border-color:var(--border)}
.pub-btn--ghost{background:transparent;color:var(--brand);padding:0 8px}
.pub-btn--block{width:100%;min-height:52px}
.pub-btn--sm{min-height:40px;padding:0 16px;font-size:var(--text-sm)}
.pub-btn[disabled]{opacity:.45;box-shadow:none}
/* stage */
.stage{position:relative;min-height:calc(100vh - 76px);overflow:hidden;display:flex;justify-content:center;align-items:flex-start;padding:84px 24px 60px}
.blob{position:absolute;z-index:0}
.col{position:relative;z-index:1;width:460px}
.col.wide{width:760px}
.col.duo{width:960px;display:grid;grid-template-columns:1fr 1fr;gap:28px}
.pub-card{position:relative;background:var(--surface);border-radius:var(--radius-xl);box-shadow:var(--shadow-soft);padding:36px 40px 32px}
.peek{position:absolute;top:-64px;inset-inline-end:36px;width:88px;filter:drop-shadow(0 8px 10px color-mix(in srgb,var(--coral) 22%,transparent))}
.peek.l{inset-inline-end:auto;inset-inline-start:30px}
.topline{display:flex;gap:8px;align-items:center;margin-bottom:10px;flex-wrap:wrap}
.vb{display:inline-flex;align-items:center;height:22px;padding:0 10px;border-radius:var(--radius-pill);border:1.5px dashed color-mix(in srgb,var(--brand) 45%,transparent);color:var(--brand);font-size:var(--text-xs);font-weight:600;background:transparent}
.pub-eyebrow{color:var(--coral-deep);font-weight:600;font-size:var(--text-sm)}
h1{font-size:2rem;line-height:1.2;margin:0 0 6px;font-weight:700;letter-spacing:-.01em}
.lead{color:var(--muted);margin:0 0 22px;font-size:var(--text-base)}
.stack{display:flex;flex-direction:column;gap:12px}
.prov{min-height:52px;justify-content:flex-start;padding:0 22px;gap:14px;font-size:var(--text-base)}
.prov .i{width:20px;height:20px}
.divider{display:flex;align-items:center;gap:12px;color:var(--muted);font-size:var(--text-sm);margin:20px 0 16px}
.divider::before,.divider::after{content:"";flex:1;height:1px;background:var(--border)}
.field{display:flex;flex-direction:column;gap:6px;margin-bottom:14px}
.field label,.lbl{font-weight:600;font-size:var(--text-sm)}
.lblrow{display:flex;justify-content:space-between;align-items:baseline}
.lblrow a{font-size:var(--text-sm)}
.inp{height:52px;border:1.5px solid var(--border);border-radius:16px;background:var(--surface);padding:0 16px;font-size:var(--text-base);font-family:inherit;color:var(--text);display:flex;align-items:center;gap:10px}
.inp .ph{color:color-mix(in srgb,var(--muted) 80%,transparent)}
.inp .eye{margin-inline-start:auto;color:var(--muted);display:flex;align-items:center;gap:6px;font-size:var(--text-sm);font-weight:600}
.inp.focus{border-color:var(--brand);box-shadow:0 0 0 3px color-mix(in srgb,var(--brand) 18%,transparent)}
.inp.err{border-color:var(--danger);background:color-mix(in srgb,var(--danger-soft) 60%,var(--surface))}
.ferr{color:var(--danger);font-size:var(--text-sm);display:flex;gap:6px;align-items:center}
.help{color:var(--muted);font-size:var(--text-sm)}
.chk{display:flex;gap:10px;align-items:flex-start;font-size:var(--text-sm);margin:4px 0 18px}
.chk .b{width:22px;height:22px;border-radius:7px;border:1.5px solid var(--border);background:var(--surface);flex:none;display:flex;align-items:center;justify-content:center;margin-top:-1px}
.chk .b.on{background:var(--brand);border-color:var(--brand);color:var(--surface)}
.chk .b.on .i{width:14px;height:14px;stroke-width:3}
.chk small{display:block;color:var(--muted)}
.alert{display:flex;gap:12px;align-items:flex-start;border-radius:18px;padding:14px 16px;margin-bottom:18px;font-size:var(--text-sm)}
.alert b{display:block;font-size:var(--text-base)}
.alert.err{background:var(--danger-soft);color:var(--danger)}
.alert.err b{color:var(--danger)}
.alert.warn{background:var(--sun);color:var(--gold-ink)}
.alert.info{background:var(--sky);color:var(--text)}
.alert.ok{background:var(--mint);color:var(--success)}
.alert .emo{font-size:20px;line-height:1}
.foot{border-top:1px solid var(--border);margin-top:22px;padding-top:18px;display:flex;flex-direction:column;gap:8px;align-items:center;font-size:var(--text-sm);color:var(--muted);text-align:center}
.below{margin-top:18px;text-align:center;color:var(--muted);font-size:var(--text-sm)}
.emo-c{width:56px;height:56px;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;font-size:28px;background:var(--peach);margin-bottom:12px}
.emo-c.sky{background:var(--sky)}.emo-c.sun{background:var(--sun)}.emo-c.mint{background:var(--mint)}
.who{display:inline-flex;align-items:center;gap:8px;background:var(--pearl-mid);border-radius:var(--radius-pill);padding:4px 12px 4px 4px;font-size:var(--text-sm);font-weight:600;margin-bottom:18px}
.who .av{width:26px;height:26px;border-radius:50%;background:var(--peach-2);display:flex;align-items:center;justify-content:center;font-size:12px}
.who a{font-weight:600;margin-inline-start:6px;font-size:var(--text-xs)}
.otp{height:64px;border:1.5px solid var(--brand);border-radius:18px;box-shadow:0 0 0 3px color-mix(in srgb,var(--brand) 16%,transparent);display:grid;grid-template-columns:repeat(6,1fr);align-items:center;padding:0 10px;background:var(--surface)}
.otp span{text-align:center;font-size:1.75rem;font-weight:600;font-variant-numeric:tabular-nums;border-inline-end:1px dashed var(--border);line-height:40px}
.otp span:last-child{border:0}
.otp span.cur{color:transparent;position:relative}
.otp span.cur::after{content:"";position:absolute;left:50%;top:6px;width:2px;height:28px;background:var(--brand)}
.otp span.e{color:color-mix(in srgb,var(--muted) 45%,transparent)}
.disc{border:1.5px solid var(--border);border-radius:18px;padding:12px 16px;display:flex;align-items:center;gap:10px;font-weight:600;font-size:var(--text-sm);color:var(--brand)}
.disc .i{margin-inline-start:auto}
.steps{display:flex;gap:8px;margin:0 0 20px;list-style:none;padding:0}
.steps li{flex:1;display:flex;flex-direction:column;gap:6px;font-size:var(--text-xs);color:var(--muted);font-weight:600}
.steps li::before{content:"";height:6px;border-radius:6px;background:var(--pearl-mid)}
.steps li.done::before{background:var(--success)}
.steps li.on{color:var(--text)}.steps li.on::before{background:var(--coral)}
.setup{display:grid;grid-template-columns:230px 1fr;gap:28px;align-items:start}
.qr{background:var(--surface);border:1.5px solid var(--border);border-radius:24px;padding:14px;display:flex;flex-direction:column;align-items:center;gap:8px}
.qr img{width:196px;height:196px;image-rendering:pixelated}
.qr small{color:var(--muted);font-size:var(--text-xs);text-align:center}
.num{display:flex;gap:12px;margin-bottom:14px}
.num .n{width:28px;height:28px;border-radius:50%;background:var(--peach);color:var(--coral-deep);font-weight:700;font-size:var(--text-sm);display:flex;align-items:center;justify-content:center;flex:none}
.num .n.done{background:var(--mint);color:var(--success)}
.num p{margin:2px 0 0;font-size:var(--text-sm);color:var(--muted)}
.num b{font-size:var(--text-base)}
.apps{display:flex;gap:8px;margin-top:8px;flex-wrap:wrap}
.pub-chip{display:inline-flex;align-items:center;gap:6px;height:30px;padding:0 12px;border-radius:var(--radius-pill);background:var(--pearl-mid);font-size:var(--text-xs);font-weight:600;color:var(--text)}
.pub-chip.ok{background:var(--mint);color:var(--success)}
.key{display:flex;align-items:center;gap:10px;background:var(--pearl);border:1.5px dashed var(--border);border-radius:16px;padding:10px 12px 10px 16px;margin-top:8px}
.key code{font-family:ui-monospace,Menlo,Consolas,monospace;font-size:1rem;letter-spacing:.06em;flex:1;direction:ltr;unicode-bidi:isolate}
.codes{display:grid;grid-template-columns:1fr 1fr;gap:10px;background:var(--pearl);border-radius:24px;padding:18px;margin:0 0 16px;list-style:none;counter-reset:c;direction:ltr}
.codes li{counter-increment:c;background:var(--surface);border-radius:12px;padding:10px 12px;font-family:ui-monospace,Menlo,Consolas,monospace;font-size:1rem;letter-spacing:.04em;display:flex;gap:10px;align-items:center;box-shadow:var(--shadow)}
.codes li::before{content:counter(c);color:var(--muted);font-size:var(--text-xs);font-family:var(--font);width:16px;text-align:end}
.row{display:flex;gap:10px;flex-wrap:wrap}
.toast{display:inline-flex;align-items:center;gap:6px;background:var(--mint);color:var(--success);font-weight:600;font-size:var(--text-xs);border-radius:var(--radius-pill);padding:4px 10px}
.note{position:absolute;z-index:3;background:var(--brand);color:var(--surface);font-size:12px;font-weight:600;border-radius:12px;padding:6px 10px;max-width:220px;line-height:1.35;box-shadow:0 6px 16px rgba(15,45,92,.2)}
.note::before{content:attr(data-n);display:inline-flex;width:18px;height:18px;border-radius:50%;background:var(--coral);color:#fff;align-items:center;justify-content:center;font-size:11px;margin-inline-end:6px}
.pill-sec{display:inline-flex;align-items:center;gap:6px;font-size:var(--text-xs);color:var(--muted);font-weight:600}
.timer{font-variant-numeric:tabular-nums;font-weight:700}
/* mobile */
.m .pub-header{height:60px;padding:0 16px}
.m .pub-header .brand img{width:32px;height:32px}
.m .pub-header .brand{font-size:1.1rem}
.m .stage{padding:64px 14px 32px;min-height:calc(100vh - 60px)}
.m .col,.m .col.wide{width:100%}
.m .pub-card{padding:28px 20px 22px;border-radius:28px}
.m h1{font-size:1.625rem}
.m .peek{width:72px;top:-52px;inset-inline-end:20px}
.m .setup{grid-template-columns:1fr;gap:16px}
.m .codes{grid-template-columns:1fr 1fr;padding:12px;gap:8px}
.m .key code{font-size:.9rem;letter-spacing:.02em;white-space:nowrap}
.m .codes li{font-size:.85rem;padding:8px;gap:6px;letter-spacing:.02em}
.m .codes li::before{width:12px}
.m .otp{height:60px}
.m .row .pub-btn{flex:1}
'''
