RT=[("d","Nu","Vmbo-diploma, bijbaan"),("d","Stap 1","Vrijwilliger dierenopvang"),("c","Stap 2","Mbo 4 paraveterinair"),("","Stap 3","Werken in een kliniek"),("","Stap 4","Studie diergeneeskunde"),("g","Doel","Dierenarts")]
rt="".join(f'<li class="{k}"><span class="dot">{AI["check"] if k=="d" else (AI["star"] if k=="g" else "")}</span><small>{a}</small><b>{b}</b></li>' for k,a,b in RT)
NEED=[("ok","Vmbo-diploma","staat in je paspoort"),("ok","Ervaring met dieren","vrijwilliger bij de dierenopvang"),("grow","Rustig blijven bij drukte","hier kun je in groeien")]
need="".join(f'<li class="{k}"><span class="ni">{AI["check"] if k=="ok" else AI["sprout"]}</span><div><b>{a}</b><small>{b}</small></div></li>' for k,a,b in NEED)
S=OCSS+PROGCSS+"""
.hero{background-image:url(spots/pad.jpg);background-size:1440px auto;background-position:right -10px bottom -128px;height:380px}
.hero:before{content:"";position:absolute;inset:0;background:radial-gradient(ellipse 760px 260px at 22% 22%,rgba(250,243,233,.9),rgba(250,243,233,0) 70%)}
.hero .hin{padding-top:44px}
.wrap{padding-top:22px}
.route{padding:20px 24px}
.rt{list-style:none;margin:0;padding:0;display:grid;grid-template-columns:repeat(6,1fr)}
.rt li{position:relative;text-align:center;padding:0 6px}.rt li:before{content:"";position:absolute;top:16px;left:-50%;right:50%;height:3px;background:#e3dbd0}.rt li:first-child:before{display:none}.rt li.d:before,.rt li.c:before{background:#2f7a4f}
.rt .dot{position:relative;z-index:1;width:34px;height:34px;margin:0 auto;border-radius:50%;display:grid;place-items:center;background:#fff;border:2px dashed #d7c9b7;color:#b07a12}.rt .dot svg{width:16px;height:16px}
.rt .d .dot{background:#2f7a4f;border:0;color:#fff}.rt .c .dot{border:2.5px solid #a2501d;background:#fdf0e3;box-shadow:0 0 0 5px #fdf6ec}.rt .c .dot:after{content:"";width:11px;height:11px;border-radius:50%;background:#a2501d}.rt .g .dot{background:#fdf3dc;border:0}
.rt small{display:block;margin-top:8px;font-size:.74rem;font-weight:700;color:#8a7a68;text-transform:uppercase;letter-spacing:.04em}.rt b{display:block;font-size:.88rem;color:var(--brand);font-weight:650;line-height:1.3}.rt .c small{color:#a2501d}
.cg2{display:grid;grid-template-columns:1fr 400px;gap:22px;margin-top:20px;align-items:start}.cg2>*{min-width:0}
.step{padding:22px 24px}
.step .e{font-weight:700;color:#b4461f;font-size:.82rem}.step h2{font-size:1.35rem;margin-top:2px}.step p.sm{color:#3d4e63;margin-top:6px;font-size:.95rem}
.need{list-style:none;margin:16px 0 0;padding:0;display:grid;grid-template-columns:repeat(3,1fr);gap:10px}
.need li{display:flex;gap:10px;align-items:flex-start;border:1px solid #eee5da;background:#fbf7f1;border-radius:12px;padding:10px 12px}.need b{display:block;font-size:.88rem;color:var(--brand);line-height:1.3}.need small{font-size:.8rem;color:var(--muted)}
.ni{width:26px;height:26px;border-radius:50%;display:grid;place-items:center;flex-shrink:0;background:#e8f3ec;color:#1f5135}.ni svg{width:14px;height:14px}.need li.grow{background:#fdf6ec;border-color:#f1e2cc}.need li.grow .ni{background:#fdf0e3;color:#a2501d}.need li.grow small{color:#a2501d;font-weight:650}
.list{display:grid;gap:12px}
.later{margin-top:16px;padding:18px 22px}.later h3{font-size:1.02rem}
.lr{display:grid;grid-template-columns:92px 1fr auto;gap:14px;align-items:center;padding:12px 0;border-top:1px solid #efe6db}.lr:first-of-type{border-top:0;padding-top:10px}
.lr .s{font-size:.74rem;font-weight:700;color:#8a7a68;text-transform:uppercase;letter-spacing:.04em}.lr b{display:block;color:var(--brand);font-size:.95rem}.lr .chips{display:flex;flex-wrap:wrap;gap:6px;margin-top:5px}.lr .chips span{font-size:.78rem;font-weight:600;color:#34465c;background:#f4efe8;border-radius:99px;padding:3px 10px}
.lr .go{font-weight:650;color:var(--brand);font-size:.88rem;display:inline-flex;gap:4px;align-items:center;white-space:nowrap}.lr .go svg{width:15px;height:15px}
.all{display:flex;justify-content:space-between;align-items:center;gap:16px;margin-top:16px;background:#fbf2e3;border:1px solid #efe3d1;border-radius:16px;padding:14px 18px}
.all .l{display:flex;gap:14px;align-items:center}.all img{width:58px;height:58px}.all b{color:var(--brand);display:block}.all small{color:#3d4e63;font-size:.88rem}
.side{position:sticky;top:16px;display:grid;gap:12px}
.inl{display:none}
@media(max-width:600px){
 .hero{height:auto;background-size:860px auto;background-position:right -170px bottom -70px}.hero .hin{padding-bottom:230px}
 .hero:before{background:linear-gradient(180deg,rgba(250,243,233,.94) 0%,rgba(250,243,233,.7) 40%,rgba(250,243,233,0) 62%)}
 .route{padding:16px 10px}.rt li{padding:0 2px}.rt b{display:none}.rt .c b{display:block;font-size:.72rem}.rt small{font-size:.62rem}.rt .dot{width:28px;height:28px}.rt li:before{top:13px}
 .cg2{grid-template-columns:1fr;gap:14px;margin-top:14px}.step{padding:18px 16px}.step h2{font-size:1.2rem}.need{grid-template-columns:1fr;gap:8px}
 .side{display:none}.inl{display:block;margin-top:-4px}
 .inl .panel{border-radius:0 0 16px 16px;border:1px solid var(--brand);border-top:0;box-shadow:none}.inl .panel .ph{display:none}
 .later{padding:16px}.lr{grid-template-columns:1fr;gap:4px}.lr .go{justify-self:start;margin-top:4px}
 .all{flex-direction:column;align-items:flex-start}.all .btn{width:100%}
}"""
selfoot_d=f'<div class="ft"><span class="lk2 desk">Details staan rechts {AI["chev"]}</span><span class="lk2 mob">Details verbergen {AI["down"]}</span><small>Leerwerkloket (regio)</small></div>'
B=header("Carrière")+f"""<section class="hero"><div class="hin"><span class="eyb c">Mijn carrière · stap 2 van 4</span><h1 style="margin-top:4px">Op weg naar dierenarts</h1><p class="lead">Elke stap een beetje verder. Bij elke stap zie je wat je al hebt en wat je kunt leren, met workshops en opleidingen die bij jou passen.</p><div class="hact"><span class="btn line sm">Droombaan wijzigen</span></div></div></section>
<main class="wrap"><div class="subnav"><div class="seg2"><span class="on">{AI["route"]}Mijn route</span><span>{AI["sprout"]}Leren &amp; groeien <i>9</i></span></div><span class="note">Alles is bewaard. Wisselen mag altijd.</span></div>
<section class="card route"><ol class="rt">{rt}</ol></section>
<div class="cg2"><div>
<section class="card step"><span class="e">Nu aan de beurt · stap 2</span><h2>Mbo 4 Dierenartsassistent paraveterinair</h2><p class="sm">Met deze opleiding mag je in een kliniek meehelpen bij behandelingen. Het is de brug naar de studie diergeneeskunde.</p>
<ul class="need">{need}</ul>
<div class="ttl"><h3>Workshops en opleidingen voor deze stap</h3><small>Gratis staat altijd bovenaan</small></div>
<div class="list">{card(C["rust"],sel=True,foot=selfoot_d,after='<div class="inb">'+panel_body()+'</div>')}{card(C["mbo"])}{card(C["basis"],partner=True)}</div>
<p class="disc" style="margin-top:10px">Partnerlink: Lobsy kan een kleine vergoeding krijgen. Dat verandert nooit wat bovenaan staat.</p></section>
<section class="card later"><h3>Later op je route</h3>
<div class="lr"><span class="s">Stap 3</span><div><b>Werken in een kliniek</b><div class="chips"><span>Lastige gesprekken aan de balie</span><span>Eerste hulp bij dieren</span></div></div><span class="go">Bekijk 2 {AI["chev"]}</span></div>
<div class="lr"><span class="s">Stap 4</span><div><b>Studie diergeneeskunde</b><div class="chips"><span>Open dagen en meeloopdagen</span><span>Voorbereidend jaar biologie</span></div></div><span class="go">Bekijk 2 {AI["chev"]}</span></div></section>
<div class="all"><div class="l"><img class="spot" src="spots/boeken.jpg" alt=""><div><b>Meer leren, ook buiten je route?</b><small>In Leren &amp; groeien staat alles bij elkaar, met je voortgang erbij.</small></div></div><span class="btn sm">Naar Leren &amp; groeien</span></div>
</div>
<div class="side">{panel()}{prog_mini()}</div></div>
</main>"""+LF+tabbar("Carrière")
page("carriere-leren",S,B)
