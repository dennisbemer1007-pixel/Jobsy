A = r'''
:root{--bg:#f5f2ee;--surface:#fffcfa;--text:#122033;--muted:#5a6a7d;--border:#ddd5cc;--brand:#0f2d5c;--brand-deep:#0a2044;--accent-soft:#e7eef7;
--accent-hover:#163a6b;--coral:#f54a1b;--gold:#c9a227;--gold-soft:#faf6e8;--success:#15803d;--success-soft:#ecfdf3;--warn:#a65b00;--warn-soft:#fff8e7;--map-pin:#22c55e;
--pearl:#f7f4f0;--pearl-mid:#efe9e3;--shadow:0 1px 2px rgba(15,45,92,.06);--shadow-lg:0 12px 32px rgba(15,45,92,.12);
--radius-sm:8px;--radius:12px;--radius-pill:999px;--space-1:.25rem;--space-2:.5rem;--space-3:.75rem;--space-4:1rem;--space-5:1.5rem;--space-6:2rem;--space-7:3rem;
--text-xs:.75rem;--text-sm:.875rem;--text-md:1rem;--text-lg:1.125rem;--text-xl:1.375rem;--text-2xl:1.75rem;
/* PROPOSED (landing only): one display size for the hero h1 */ --text-display:2.75rem;
--font:"Inter","Segoe UI","Helvetica Neue",Arial,sans-serif;--on-dark:color-mix(in srgb,var(--surface) 78%,transparent)}
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:var(--font);color:var(--text);background:var(--bg);font-size:var(--text-md);line-height:1.5;-webkit-font-smoothing:antialiased}
b,strong{font-weight:600}a{color:inherit;text-decoration:none}
.i{width:18px;height:18px;fill:none;stroke:currentColor;stroke-width:1.8;stroke-linecap:round;stroke-linejoin:round;flex:none}
.wrap{max-width:1200px;margin:0 auto;padding:0 var(--space-6)}
.muted{color:var(--muted)}.sm{font-size:var(--text-sm)}.xs{font-size:var(--text-xs)}
.btn{height:44px;display:inline-flex;align-items:center;justify-content:center;gap:8px;padding:0 18px;border-radius:var(--radius-sm);border:1px solid var(--border);background:var(--surface);color:var(--text);font-weight:600;font-size:var(--text-md);white-space:nowrap}
.btn.pri{background:var(--brand);border-color:var(--brand);color:var(--surface)}
.btn.lg{height:52px;padding:0 24px;font-size:var(--text-lg)}
.btn.onpri{background:var(--surface);border-color:var(--surface);color:var(--brand)}
.btn.ondark{background:transparent;border-color:color-mix(in srgb,var(--surface) 40%,transparent);color:var(--surface)}
.btn.ghost{background:transparent;border-color:transparent;color:var(--brand);padding:0 8px}
.btn.sm{height:36px;padding:0 12px;font-size:var(--text-sm)}
.btn.full{width:100%}
.pill{display:inline-flex;align-items:center;gap:6px;height:26px;padding:0 10px;border-radius:var(--radius-pill);font-size:var(--text-xs);font-weight:600;background:var(--accent-soft);color:var(--brand);white-space:nowrap}
.pill .i{width:14px;height:14px}
.pill.ok{background:var(--success-soft);color:var(--success)}.pill.wn{background:var(--warn-soft);color:var(--warn)}
.pill.line{background:transparent;border:1px solid var(--border);color:var(--muted)}
.pill.dark{background:color-mix(in srgb,var(--surface) 12%,transparent);color:var(--surface)}
.vb{display:inline-flex;align-items:center;gap:5px;height:22px;padding:0 8px;border-radius:var(--radius-pill);font-size:var(--text-xs);font-weight:600;border:1px dashed color-mix(in srgb,var(--muted) 60%,transparent);color:var(--muted);white-space:nowrap;background:var(--surface)}
.card{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius)}
.eyebrow{font-size:var(--text-sm);font-weight:600;color:var(--brand);letter-spacing:.02em;display:flex;align-items:center;gap:8px}
h2.sec{font-size:var(--text-2xl);font-weight:600;letter-spacing:-.01em;line-height:1.25;margin-top:6px}
.lead{font-size:var(--text-lg);color:var(--muted);max-width:640px;margin-top:10px}
section{padding:72px 0}
/* header */
.hdr{height:68px;background:var(--brand-deep);color:var(--surface);position:relative;z-index:40}
.hdr .wrap{height:100%;display:flex;align-items:center;gap:var(--space-5)}
.brand{display:flex;align-items:center;gap:10px;font-size:var(--text-lg);font-weight:600}
.brand img{width:34px;height:34px;border-radius:8px;background:var(--surface);padding:2px}
.nav{display:flex;gap:4px;margin-inline-start:var(--space-5)}
.nav a{height:36px;display:inline-flex;align-items:center;padding:0 12px;border-radius:var(--radius-sm);color:var(--on-dark);font-size:var(--text-sm);font-weight:600}
.nav a.on{color:var(--surface);background:color-mix(in srgb,var(--surface) 10%,transparent)}
.hdr .r{margin-inline-start:auto;display:flex;align-items:center;gap:var(--space-2)}
.lang{display:inline-flex;align-items:center;gap:6px;height:36px;padding:0 10px;border-radius:var(--radius-sm);color:var(--on-dark);font-size:var(--text-sm);font-weight:600}
.lang .i{width:16px;height:16px}
.hdr .btn.sm{height:38px}
/* hero */
.hero{background:var(--brand-deep);color:var(--surface);padding:40px 0 40px;position:relative;overflow:hidden}
.hero .wrap{display:grid;grid-template-columns:minmax(0,1.05fr) minmax(0,1fr);gap:var(--space-6);align-items:center}
.hero h1{font-size:var(--text-display);line-height:1.1;font-weight:700;letter-spacing:-.02em;margin-top:18px}
.hero h1 em{font-style:normal;color:var(--coral)}
.hero .lead{color:var(--on-dark);font-size:var(--text-lg);margin-top:18px;max-width:540px}
.hero .ctas{display:flex;gap:var(--space-3);margin-top:28px;align-items:center}
.hero .micro{display:flex;gap:18px;margin-top:18px;color:var(--on-dark);font-size:var(--text-sm);flex-wrap:wrap}
.hero .micro span{display:inline-flex;align-items:center;gap:6px}.hero .micro .i{width:16px;height:16px}
.scene{position:relative;height:540px}
.scene svg.rings{position:absolute;inset:0;width:100%;height:100%}
.scene .lob{position:absolute;width:250px;left:50%;top:50%;transform:translate(-50%,-56%)}
.scene .shell{position:absolute;width:250px;left:50%;top:50%;transform:translate(-86%,-44%) rotate(-14deg);filter:brightness(0) invert(1);opacity:.1}
.scene .shell-l{position:absolute;left:10px;top:386px;font-size:var(--text-xs);color:var(--on-dark);display:flex;align-items:center;gap:6px}
.scene .shell-l::before{content:"";width:22px;border-top:1px dashed var(--on-dark)}
.chip{position:absolute;background:var(--surface);color:var(--text);border-radius:var(--radius);box-shadow:var(--shadow-lg);padding:10px 14px;font-size:var(--text-sm);display:flex;align-items:center;gap:10px;white-space:nowrap}
.chip .dot{width:10px;height:10px;border-radius:50%;background:var(--map-pin);flex:none}
.chip small{display:block;color:var(--muted);font-size:var(--text-xs)}
.chip .i{color:var(--brand)}
.dib{position:absolute;right:0;bottom:36px;width:260px;background:var(--surface);color:var(--text);border-radius:var(--radius);box-shadow:var(--shadow-lg);padding:16px}
.dib h4{font-size:var(--text-md);font-weight:600;display:flex;align-items:center;gap:8px}
.dib h4 i{width:8px;height:8px;border-radius:50%;background:var(--coral);display:inline-block}
.bar{display:grid;grid-template-columns:96px 1fr;align-items:center;gap:10px;font-size:var(--text-xs);color:var(--muted);margin-top:8px}
.bar b{display:block;height:8px;border-radius:var(--radius-pill);background:var(--pearl-mid);position:relative;overflow:hidden}
.bar b::after{content:"";position:absolute;inset:0 auto 0 0;width:var(--w);background:var(--brand);border-radius:inherit}
.strip{background:var(--brand);color:var(--on-dark)}
.strip .wrap{display:flex;align-items:center;gap:28px;height:64px;font-size:var(--text-sm);font-weight:600}
.strip span{display:inline-flex;align-items:center;gap:8px;white-space:nowrap}.strip .i{width:18px;height:18px;color:var(--surface)}
'''
