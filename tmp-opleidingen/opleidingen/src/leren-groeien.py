TOP=["Alles","Rust en weerbaarheid","Samenwerken","Klantcontact","Plannen","Digitaal","Taal","Vakkennis"]
tops="".join(f'<span class="fc{" on" if i==0 else ""}">{t}</span>' for i,t in enumerate(TOP))
def sec(icon,title,sub,cards):
  return f'<section class="lsec"><div class="lh"><span class="li">{AI[icon]}</span><div><h3>{title}</h3><small>{sub}</small></div></div><div class="g2">{cards}</div></section>'
S=OCSS+PROGCSS+"""
.hero{background-image:url(spots/bg-sfeer.jpg);background-size:1440px auto;background-position:center bottom -22px;height:480px;background-color:#f9efd9}
.hero:before{content:"";position:absolute;inset:0;background:radial-gradient(ellipse 760px 250px at 26% 26%,rgba(251,245,234,.85),rgba(251,245,234,0) 72%)}
.hero .hin{padding-top:36px}.hero p.lead{max-width:720px}
.srch{display:flex;gap:8px;margin-top:18px;max-width:600px}.srch .in{flex:1;display:flex;gap:10px;align-items:center;background:#fff;border:1px solid #e0d4c3;border-radius:12px;padding:0 14px;height:48px;color:#7b8796;font-size:.95rem;box-shadow:0 4px 14px rgba(15,45,92,.06)}.srch .in svg{width:18px;height:18px;color:var(--brand)}.srch .btn{height:48px}
.wrap{padding-top:22px}
.lg2{display:grid;grid-template-columns:1fr 350px;gap:22px;align-items:start}.lg2>*{min-width:0}
.flt{padding:16px 18px;display:grid;gap:12px}
.fr{display:flex;flex-wrap:wrap;gap:8px;align-items:center}.fr .fl{font-size:.76rem;font-weight:700;color:#8a7a68;text-transform:uppercase;letter-spacing:.04em;margin-right:4px}
.fc{font-size:.86rem;font-weight:650;color:var(--brand);background:#fff;border:1px solid #d9cfc2;border-radius:99px;padding:6px 13px;white-space:nowrap}.fc.on{background:var(--brand);border-color:var(--brand);color:#fff}
.fr2{display:flex;flex-wrap:wrap;gap:10px 22px;align-items:center;border-top:1px solid #efe6db;padding-top:12px}
.tgl{display:inline-flex;gap:10px;align-items:center;font-weight:650;color:var(--brand);font-size:.9rem}
.sg{display:inline-flex;gap:2px;background:#f4efe8;border-radius:10px;padding:3px}.sg span{padding:5px 11px;border-radius:8px;font-size:.84rem;font-weight:650;color:#4a5a6e;white-space:nowrap}.sg span.on{background:#fff;color:var(--brand);box-shadow:0 1px 3px rgba(0,0,0,.08)}
.fgrp{display:inline-flex;gap:8px;align-items:center}.fgrp small{font-size:.8rem;color:var(--muted);font-weight:650}
.cnt{margin-left:auto;font-size:.86rem;color:var(--muted)}
.lsec{margin-top:22px}.lh{display:flex;gap:12px;align-items:center;margin-bottom:12px}.li{width:36px;height:36px;border-radius:11px;background:#fdf0e3;color:#a2501d;display:grid;place-items:center;flex-shrink:0}.li svg{width:19px;height:19px}.lh h3{font-size:1.1rem}.lh small{color:var(--muted);font-size:.86rem}
.g2{display:grid;grid-template-columns:1fr 1fr;gap:14px}
.side3{display:grid;gap:14px;position:sticky;top:16px}
.vg{padding:18px 20px}.vg h3{font-size:1.08rem}
.ctr{display:grid;grid-template-columns:repeat(3,1fr);gap:8px;margin:12px 0 4px}.ctr div{background:#fbf7f1;border:1px solid #f0e7dc;border-radius:12px;padding:9px 8px;text-align:center}.ctr b{display:block;font-size:1.25rem;color:var(--brand);line-height:1.1}.ctr small{font-size:.76rem;color:var(--muted);font-weight:650}
.vg .pi{display:flex;gap:12px;align-items:flex-start;padding:11px 0;border-top:1px solid #efe6db}
.vg .pic{width:30px;height:30px;border-radius:50%;display:grid;place-items:center;flex-shrink:0}.vg .pic svg{width:15px;height:15px}.vg .pic.ok{background:#e8f3ec;color:#1f5135}.vg .pic.go{background:#fdf0e3;color:#a2501d}
.vg .pi b{display:block;font-size:.9rem;color:var(--brand);line-height:1.3}.vg .pi small{font-size:.8rem;color:var(--muted)}.vg .pi small.g{color:#1f5135;font-weight:650}
.vg .mini{margin-top:8px}.vg .mini .btn{padding:7px 12px;font-size:.84rem;border-radius:9px}
.yield{margin-top:12px;background:#f4f7fb;border:1px solid #dfe7f1;border-radius:14px;padding:12px 14px}.yield h4{font-size:.9rem;margin-bottom:6px}.yield ul{list-style:none;margin:0;padding:0;display:grid;gap:7px;font-size:.86rem;color:#34465c}.yield li{display:flex;gap:9px;align-items:flex-start}.yield svg{width:16px;height:16px;color:var(--brand);flex-shrink:0;margin-top:1px}
.help{display:flex;gap:14px;align-items:center;padding:14px 16px}.help img{width:64px;height:64px}.help b{display:block;color:var(--brand)}.help p{font-size:.86rem;color:#3d4e63}.help .lk2{display:inline-flex;gap:4px;align-items:center;font-weight:650;color:var(--brand);font-size:.86rem;margin-top:4px}.help .lk2 svg{width:15px;height:15px}
.mfl{display:none}
.sv{padding:16px 20px}.sv h3{font-size:1.02rem}.si{display:flex;gap:12px;align-items:flex-start;padding:10px 0;border-top:1px solid #efe6db}.si:first-of-type{border-top:0}.bmi{width:30px;height:30px;border-radius:9px;background:#fdf0e3;color:var(--coral);display:grid;place-items:center;flex-shrink:0}.bmi svg{width:15px;height:15px}.si b{display:block;font-size:.9rem;color:var(--brand);line-height:1.3}.si small{font-size:.8rem;color:var(--muted)}
.how2{padding:16px 18px}.how2 h3{font-size:.98rem}.how2 p{font-size:.86rem;color:#3d4e63;margin-top:4px}
@media(max-width:600px){
 .hero{height:auto;background-size:1000px auto;background-position:left -40px bottom -14px}.hero .hin{padding-bottom:178px}
 .hero:before{background:linear-gradient(180deg,rgba(251,245,234,.92) 0%,rgba(251,245,234,.5) 50%,rgba(251,245,234,0) 70%)}
 .srch .btn{display:none}
 .lg2{display:flex;flex-direction:column;align-items:stretch;gap:14px}.srch .in .dk{display:none}.side3{display:contents}.vg{order:-1}.help{order:5}
 .flt{padding:14px}.fr .fl{width:100%}.fr2{display:none}.mfl{display:flex;justify-content:space-between;align-items:center;border-top:1px solid #efe6db;padding-top:12px;font-weight:650;color:var(--brand);font-size:.9rem}.mfl span{display:inline-flex;gap:8px;align-items:center}.mfl svg{width:17px;height:17px}
 .g2{grid-template-columns:1fr;gap:12px}.lsec{margin-top:18px}
 .yield{display:none}.how2{order:6}.sv{order:4}
}"""
VG=f"""<section class="card vg"><h3>Jouw voortgang</h3><div class="ctr"><div><b>1</b><small>Bezig</small></div><div><b>1</b><small>Afgerond</small></div><div><b>2</b><small>Bewaard</small></div></div>
<div class="pi" style="border-top:0"><span class="pic go">{AI["play"]}</span><div><b>Lastige gesprekken aan de balie</b><small>Bezig · week 2 van 4</small><div class="wk"><i class="on"></i><i class="on"></i><i></i><i></i></div><div class="mini"><span class="btn line">Klaar? Zet het in je paspoort</span></div></div></div>
<div class="pi"><span class="pic ok">{AI["check"]}</span><div><b>Eerste hulp bij dieren</b><small class="g">Afgerond · staat in je Bewijzen</small></div></div>
<div class="yield"><h4>Wat het je oplevert</h4><ul><li>{AI["award"]}<span>Wat je afrondt, staat als bewijs in je paspoort.</span></li><li>{AI["heart"]}<span>Vacatures die erom vragen, zie je eerder in je matches.</span></li><li>{AI["route"]}<span>Je route schuift op: stap 2 komt dichterbij.</span></li></ul></div></section>"""
HELP=f"""<section class="card help"><img class="spot" src="spots/boeken.jpg" alt=""><div><b>Twijfel je wat past?</b><p>Lobsy denkt met je mee, ook over geld en tijd.</p><span class="lk2">Stel je vraag {AI["chev"]}</span></div></section>"""
SAVED=f"""<section class="card sv"><h3>Bewaard</h3><div class="si"><span class="bmi">{AI["bmon"]}</span><div><b>Rustig blijven als het druk wordt</b><small>Workshop · gratis · start 12 november</small></div></div><div class="si"><span class="bmi">{AI["bmon"]}</span><div><b>Mbo 4 Dierenartsassistent (BBL)</b><small>Opleiding · aanmelden kan tot 1 april</small></div></div></section>"""
HOW=f"""<section class="card how2"><h3>Hoe kiezen we dit?</h3><p>We kijken naar je route, je groeipunten en wat dichtbij is. Gratis staat altijd bovenaan. Een partnerlink zie je altijd aan het label.</p></section>"""
B=header("Carrière")+f"""<section class="hero"><div class="hin"><span class="eyb c">Carrière · Leren &amp; groeien</span><h1 style="margin-top:4px">Leren en groeien, op jouw manier</h1><p class="lead">Workshops en opleidingen die passen bij jouw route en je test. Gratis staat altijd bovenaan.</p><div class="srch"><span class="in">{AI["search"]}<span>Zoek op onderwerp<span class="dk">, bijvoorbeeld klantcontact</span></span></span><span class="btn">Zoeken</span></div></div></section>
<main class="wrap"><div class="subnav"><div class="seg2"><span>{AI["route"]}Mijn route</span><span class="on">{AI["sprout"]}Leren &amp; groeien <i>9</i></span></div><span class="note">Alles wat je bewaart of start, blijft hier staan.</span></div>
<div class="lg2"><div>
<section class="card flt"><div class="fr"><span class="fl">Onderwerp</span>{tops}</div>
<div class="fr2"><span class="tgl"><span class="sw"></span>Alleen gratis</span><span class="fgrp"><small>Vorm</small><span class="sg"><span class="on">Alles</span><span>Online</span><span>Klassikaal</span></span></span><span class="fgrp"><small>Duur</small><span class="sg"><span class="on">Alles</span><span>Kort</span><span>Weken</span><span>Opleiding</span></span></span></div>
<div class="mfl"><span>{AI["filter"]}Gratis · vorm · duur</span><span>Filters {AI["chev"]}</span></div></section>
{sec("route","Voor je volgende stap","Mbo 4 Dierenartsassistent paraveterinair",card(C["mbo"])+card(C["ehbd"]))}
{sec("sprout","Voor je groeipunten","Uit je test: rust onder druk en plannen",card(C["rust"],bm=True)+card(C["balie"])+card(C["plan"])+card(C["samen"]))}
{sec("users","Voor iedereen","Handig in bijna elke baan",card(C["digi"])+card(C["taal"]))}
</div>
<div class="side3">{VG}{SAVED}{HOW}{HELP}</div></div>
</main>"""+LF+tabbar("Carrière")
page("leren-groeien",S,B)
