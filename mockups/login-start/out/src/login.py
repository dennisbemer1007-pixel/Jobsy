S=f"""
.lg{{position:relative;overflow:hidden;background:#fef1e1;padding-bottom:44px;border-bottom:1px solid var(--border)}}
.art{{position:absolute;bottom:0;height:680px;width:491px;background:no-repeat center bottom/auto 100%;-webkit-mask-image:linear-gradient(transparent,#000 18%);mask-image:linear-gradient(transparent,#000 18%)}}.art.l{{left:0;background-image:url(spots/login-links.jpg)}}.art.r{{right:0;background-image:url(spots/login-rechts.jpg)}}
.card{{position:relative;z-index:2;width:430px;margin:28px auto 0;background:var(--surf);border:1px solid var(--border);border-radius:20px;box-shadow:0 18px 50px rgba(15,45,92,.10);padding:32px 34px 26px}}
.eyebrow{{color:#b4461f;font-weight:650;font-size:.85rem}}
.card h1{{font-size:1.75rem;margin:4px 0 4px}}.card .sub{{color:var(--muted)}}
.ms{{display:flex;align-items:center;justify-content:center;gap:10px;border:1px solid #d6cec4;border-radius:12px;padding:11px;font-weight:600;margin-top:20px;background:#fff}}
.ms i{{display:grid;grid-template-columns:7px 7px;gap:1px}}.ms i b{{width:7px;height:7px}}
.or{{display:flex;align-items:center;gap:12px;color:var(--muted);font-size:.85rem;margin:18px 0 6px}}.or:before,.or:after{{content:"";flex:1;height:1px;background:var(--border)}}
label{{display:flex;justify-content:space-between;font-weight:650;font-size:.92rem;margin:14px 0 6px}}label a{{font-weight:600;color:var(--brand);font-size:.88rem;text-decoration:underline;text-underline-offset:3px}}
.in{{display:flex;align-items:center;background:#fff;border:1px solid #cdc5bc;border-radius:12px;padding:12px 14px;font-size:1rem}}.in .ph{{color:#8a96a5}}.in em{{margin-left:auto;font-style:normal;color:var(--muted);font-size:.88rem;font-weight:600}}
.stay{{display:flex;gap:10px;align-items:flex-start;margin:16px 0 18px;font-size:.92rem}}.box{{width:18px;height:18px;border:1.5px solid #9aa6b5;border-radius:5px;flex-shrink:0;margin-top:2px;background:#fff}}.stay small{{display:block;color:var(--muted);font-size:.82rem}}
.card .btn{{width:100%}}
.legal{{color:var(--muted);font-size:.8rem;margin-top:12px;text-align:center}}
.new{{border-top:1px solid var(--border);margin-top:18px;padding-top:16px;text-align:center;font-size:.95rem}}
.under{{margin:16px -34px -26px;padding:14px 20px 16px;border-top:1px solid var(--border);background:#faf5ee;border-radius:0 0 20px 20px;text-align:center;font-size:.88rem;color:#34465c;display:grid;gap:4px}}
.mband{{display:none}}
@media(max-width:600px){{
 .lg{{background:none;border-bottom:0;padding-bottom:28px}}.art{{display:none}}
 .mband{{display:block;height:190px;margin:0 16px;border-radius:16px;background:var(--cream) url({A}kennismaken.jpg) no-repeat;background-size:auto 340px;background-position:0 100%}}
 .card{{width:auto;margin:18px 16px 0;padding:24px 20px 20px;border-radius:18px}}.card h1{{font-size:1.5rem}}
 .under{{margin:16px -20px -20px;border-radius:0 0 18px 18px}}
}}"""
B="""<div class="lg"><div class="art l"></div><div class="art r"></div><nav class="nav"><div class="brand"><img src="lobsy-256.png" alt="">Lobsy</div><div class="right"><span class="lang">NL ▾</span><span class="btn sm">Account maken</span></div></nav>
<div class="mband"></div>
<div class="card"><div class="eyebrow">Welkom terug</div><h1>Inloggen bij Lobsy</h1><p class="sub">Kies hoe je wilt inloggen.</p>
<div class="ms"><i><b style="background:#f25022"></b><b style="background:#7fba00"></b><b style="background:#00a4ef"></b><b style="background:#ffb900"></b></i>Inloggen met Microsoft</div>
<div class="or">of met je e-mailadres</div>
<label>E-mailadres</label><div class="in"><span class="ph">naam@voorbeeld.nl</span></div>
<label>Wachtwoord <a>Wachtwoord vergeten?</a></label><div class="in"><span class="ph">••••••••</span><em>Toon</em></div>
<div class="stay"><span class="box"></span><div>Blijf ingelogd op dit apparaat<small>Niet aanvinken op een gedeelde computer.</small></div></div>
<span class="btn">Inloggen</span>
<p class="legal">Door verder te gaan ga je akkoord met de <u>voorwaarden</u> en de <u>privacyverklaring</u>.</p>
<p class="new">Nog geen account? <span class="lnk">Account maken</span></p>
<div class="under"><span>Voor werkgevers: <span class="lnk">Bedrijf registreren (KvK)</span></span><span>Eerst je werk-DNA ontdekken? <span class="lnk">Doe de gratis test</span></span></div></div>
</div>"""
page("login",S,B)
