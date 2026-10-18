exec(open(D+"/app.inc.py").read())
JOBS=[("Logistiek","Magazijnmedewerker","Westland Fresh Logistics","Zaandam-Oost","15 min","€ 14,06/u","69%","logistiek"),
("Zorg","Zorgmedewerker dagbesteding","Kasteelhof Zorg","Zaandam","12 min","€ 15,20/u","86%","zorg"),
("Dieren","Medewerker dierenpension","De Kooger Dierenopvang","Koog a/d Zaan","9 min","€ 13,90/u","81%","dieren"),
("Retail","Verkoopmedewerker","Bakkerij Jansen","Wormerveer","17 min","€ 13,68/u","72%","retail")]
def card(j):
  t,ti,c,pl,m,p,sc,k=j
  return f'<article class="job"><div class="jtop"><span class="th" style="background-image:url(spots/job-{k}.jpg)"></span><div class="jtt"><span class="jt">{t}</span><h3>{ti}</h3></div><span class="fav">{AI["heart"]}</span></div><p class="co">{c} · {pl}</p><div class="jm"><span>{AI["bike"]}± {m}</span><span>{p}</span></div><div class="fit"><b>{sc} past bij jou</b><span>Waarom?</span></div></article>'
cards="".join(card(j) for j in JOBS)
EXT=f'<div class="ext2"><span class="th" style="background-image:url(spots/job-flex.jpg)"></span><div><small>Vacature van een andere site</small><b>Flex: Kassamedewerker</b><span>Supermarkt Zaandam-Oost · ± 14 min fietsen</span></div><span class="lnk" style="font-size:.85rem;white-space:nowrap">Bekijk ›</span></div>'
mapsvg=open(D+"/map.svg").read()
S=APPCSS+"""
body{background:#f7f1e8}
.bk{display:grid;grid-template-columns:600px 1fr;min-height:843px}
.lp{background:#fbf6ef;border-right:1px solid var(--border);padding:18px 22px 24px}
.lh{display:flex;justify-content:space-between;align-items:center;gap:16px;background:#fbf2e3;border:1px solid #efe3d1;border-radius:16px;overflow:hidden;padding-left:18px;margin-bottom:16px}
.lh h1{font-size:1.25rem}.lh p{color:#3d4e63;font-size:.88rem;margin-top:2px}.lh .lhart{width:170px;height:82px;flex-shrink:0;background:url(spots/kaart.jpg) no-repeat 8% 70%/auto 150px;-webkit-mask-image:linear-gradient(90deg,transparent,#000 30%);mask-image:linear-gradient(90deg,transparent,#000 30%)}
.lbl{font-weight:650;font-size:.88rem;margin-bottom:6px;display:block}
.row{display:flex;gap:8px;align-items:center}.inp{flex:1;display:flex;align-items:center;gap:10px;background:#fff;border:1px solid #cdc5bc;border-radius:12px;padding:10px 12px;font-size:.95rem;min-width:0}.inp svg{width:18px;height:18px;color:var(--muted);flex-shrink:0}.inp .ph{color:#8a96a5}
.ibtn{width:40px;height:40px;border-radius:12px;border:1px solid #cdc5bc;background:#fff;display:grid;place-items:center;color:var(--brand);flex-shrink:0}.ibtn svg{width:18px;height:18px}
.obtn{display:inline-flex;gap:7px;align-items:center;border-radius:12px;border:1px solid #cfd8e4;background:#fff;color:var(--brand);font-weight:650;font-size:.9rem;padding:9px 14px;white-space:nowrap}.obtn svg{width:16px;height:16px}.obtn.match{background:#fdf3dc;border-color:#ecd49c;color:#7a5513}
.chips{display:flex;gap:8px;align-items:center;margin-top:12px}.chip{display:inline-flex;gap:6px;align-items:center;background:var(--brand);color:#fff;border-radius:99px;padding:5px 8px 5px 12px;font-size:.85rem;font-weight:650}.chip svg{width:13px;height:13px}
.rh{display:flex;justify-content:space-between;align-items:center;margin:16px 0 12px;padding-top:14px;border-top:1px solid var(--border)}.rh b{color:var(--brand);font-size:1.02rem}.rh small{display:block;color:var(--muted);font-size:.82rem}
.sel{border:1px solid #cdc5bc;background:#fff;border-radius:10px;padding:7px 12px;font-size:.88rem;font-weight:600;color:var(--brand)}
.seg{display:inline-flex;border:1px solid #cfd8e4;border-radius:10px;overflow:hidden;background:#fff}.seg span{padding:7px 14px;font-weight:650;font-size:.88rem;color:var(--brand)}.seg span.on{background:var(--brand);color:#fff}
.jobs{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px}
.job{background:var(--surf);border:1px solid var(--border);border-radius:16px;padding:14px}
.jtop{display:flex;gap:12px;align-items:flex-start}.th{width:60px;height:60px;border-radius:14px;background:#fbf2e3 center/cover;flex-shrink:0;border:1px solid #efe3d1}.jtt{flex:1;min-width:0}.jtt h3{font-size:1rem;margin-top:4px}
.jt{display:inline-block;font-size:.72rem;font-weight:700;color:var(--brand);background:var(--soft);border-radius:8px;padding:2px 8px}
.fav{width:30px;height:30px;border-radius:50%;border:1px solid var(--border);display:grid;place-items:center;color:var(--brand);flex-shrink:0}.fav svg{width:15px;height:15px}
.co{color:var(--muted);font-size:.85rem;margin-top:10px}.jm{display:flex;gap:12px;margin-top:8px;font-size:.85rem;color:#34465c}.jm span{display:inline-flex;gap:5px;align-items:center}.jm svg{width:15px;height:15px}
.fit{display:flex;justify-content:space-between;align-items:center;margin-top:10px;background:#f1f6f2;border-radius:10px;padding:6px 10px}.fit b{color:#1f5135;font-size:.82rem}.fit span{font-size:.8rem;font-weight:650;color:var(--brand);text-decoration:underline;text-underline-offset:3px}
.ext2{display:flex;gap:12px;align-items:center;margin:0 0 14px;padding:12px 14px;border-radius:16px;border:1px dashed #e0c9a6;background:#fbf2e3}.ext2 .th{width:48px;height:48px;border-radius:12px}.ext2 div{flex:1;min-width:0}.ext2 small{display:block;font-size:.75rem;font-weight:700;color:#8f6232}.ext2 b{display:block;color:var(--brand);font-size:.95rem}.ext2 span{font-size:.83rem;color:var(--muted)}
.mp{position:relative;overflow:hidden;background:#eef0ea}.mpw{position:absolute;inset:0}.mp svg.map{position:absolute;inset:0;width:100%;height:100%}
.legend{position:absolute;left:18px;bottom:18px;background:rgba(255,252,250,.97);border:1px solid var(--border);border-radius:14px;padding:12px 14px;font-size:.8rem;color:#34465c;box-shadow:0 6px 18px rgba(15,45,92,.08)}
.legend b{display:block;color:var(--brand);margin-bottom:6px;font-size:.85rem}.legend div{display:flex;gap:8px;align-items:center}.legend i{display:inline-block;width:22px;border-top:2px solid var(--brand)}.legend i.d{border-top-style:dashed}.legend i.dd{border-top-style:dotted}
.zoom{position:absolute;right:16px;bottom:18px;display:grid;gap:8px}.zoom span{width:36px;height:36px;border-radius:10px;background:#fff;border:1px solid var(--border);display:grid;place-items:center;font-weight:700;color:var(--brand)}
.pop{position:absolute;right:18px;top:16px;width:300px;background:var(--surf);border:1px solid var(--border);border-radius:16px;box-shadow:0 14px 34px rgba(15,45,92,.16);padding:14px}
.poph{display:flex;justify-content:space-between;align-items:center;font-size:.85rem;font-weight:650;color:var(--brand)}.poph span{color:var(--muted);font-weight:600}
.popj{display:flex;gap:12px;margin:10px 0 12px}.popj i{width:56px;height:56px;border-radius:10px;flex-shrink:0}.popj b{display:block;color:var(--brand);font-size:.95rem}.popj small{color:var(--muted);font-size:.82rem}
.popj em{display:inline-block;margin-top:4px;font-style:normal;font-size:.78rem;font-weight:700;color:#1f5135;background:#f1f6f2;border-radius:8px;padding:2px 8px}
.pop .btn{width:100%;padding:10px}
@media(max-width:600px){
 .bk{display:block;min-height:0}.lp{padding:12px 14px 14px;border-right:0}
 .lh{display:none}.srow{flex-wrap:wrap}.srow .inp{flex-basis:100%}.srow .obtn{flex:1;justify-content:center}
 .rh{margin:12px 0 0;padding-top:12px}.rh .sel{display:none}
 .mp{overflow:visible;background:#fbf6ef;padding-bottom:4px}.mpw{position:relative;height:420px;overflow:hidden}.legend{display:none}
 .pop{position:static;width:auto;margin:12px 14px 0;box-shadow:none}.zoom{top:12px;bottom:auto;right:12px}
 .sheet{background:#fbf6ef;border-top:1px solid var(--border);padding:12px 14px 18px}.sheet .ext2{margin:12px 0 0}.grab{width:40px;height:4px;border-radius:99px;background:#d6cec4;margin:0 auto 12px}
 .sheet .rh2{display:flex;justify-content:space-between;align-items:center;margin-bottom:12px}.sheet .rh2 b{color:var(--brand)}
 .jobs{grid-template-columns:1fr;gap:12px}
}
@media(min-width:601px){.sheet{display:none}}
@media(max-width:600px){.dl{display:none}}
"""
B=header("Zoeken")+f"""<div class="bk"><section class="lp">
<div class="lh"><div><h1>Banen bij jou in de buurt</h1><p>Binnen 20 minuten fietsen vanaf Zaandam</p></div><div class="lhart"></div></div>
<span class="lbl">Jouw adres</span><div class="row"><div class="inp">{AI["pin"]}Westzijde 120, 1506 EK Zaandam</div><span class="ibtn">{AI["loc"]}</span><span class="ibtn">{AI["x"]}</span></div>
<div class="row srow" style="margin-top:10px"><div class="inp">{AI["zoek"]}<span class="ph">Wat voor werk zoek je?</span></div><span class="obtn">{AI["filter"]}Filters</span><span class="obtn match">{AI["heart"]}Match</span></div>
<div class="chips"><span class="chip">20 min fietsen {AI["x"]}</span><span class="chip">18 jaar {AI["x"]}</span><span class="lnk" style="font-size:.88rem">Wissen</span></div>
<div class="rh"><div><b>16 vacatures</b><small>binnen 20 min fietsen</small></div><div class="row"><span class="sel">Past het best ▾</span><span class="seg"><span class="on">Kaart</span><span>Lijst</span></span></div></div>
<div class="dl">{EXT}<div class="jobs">{cards}</div></div>
</section>
<section class="mp"><div class="mpw">{mapsvg}
<div class="legend"><b>Reistijd met de fiets</b><div><i></i>10 min</div><div><i class="d"></i>20 min</div><div><i class="dd"></i>30 min</div></div>
<div class="zoom"><span>3D</span><span>+</span><span>−</span><span>◎</span></div></div>
<div class="pop"><div class="poph">6 banen in Zaandam <span>1 / 6 ›</span></div><div class="popj"><i style="background:#fbf2e3 url(spots/job-zorg.jpg) center/cover"></i><div><b>Zorgmedewerker dagbesteding</b><small>Kasteelhof Zorg · ± 12 min fietsen</small><br><em>86% past bij jou</em></div></div><span class="btn">Bekijk deze baan ›</span></div>
</section><section class="sheet"><div class="grab"></div><div class="rh2"><b>16 vacatures</b><span class="sel">Past het best ▾</span></div><div class="jobs">{cards}</div>{EXT}</section></div>"""+tabbar("Zoeken")
page("banenkaart",S,B,False)
