def crow(c,open_=False):
  t,title,dur,vorm,cost,by,why=c
  vi=AI["monitor"] if vorm.startswith("Online") else AI["pin"]
  return f'<div class="crow"><div class="cl"><div class="top">{ttag(t)}{price(cost)}</div><b>{title}</b><div class="meta"><span>{AI["clock"]}{dur}</span><span>{vi}{vorm}</span><span>{AI["build"]}{by}</span></div></div><span class="go">Bekijk {AI["chev"]}</span></div>'
def comp(name,tag,tcls,icon,text,extra=""):
  return f'<article class="comp {tcls}"><div class="ch"><span class="ci">{AI[icon]}</span><h3>{name}</h3><span class="ctag {tcls}">{tag}</span></div><p class="ct">{text}</p>{extra}</article>'
grow1=f'''<div class="grow"><div class="gh"><img class="spot" src="spots/c-peach-sq.png" alt=""><div><h4>Hier kun je in groeien</h4><p>Rust onder druk kun je leren. Deze twee passen bij jou en bij je route naar dierenarts.</p></div></div>{crow(C["rust"])}{crow(C["balie"])}</div>'''
grow2=f'''<div class="grow sm"><div class="gh"><div><h4>Hier kun je in groeien</h4></div></div>{crow(C["plan"])}</div>'''
more=f'<p class="more">{AI["sprout"]}<span>Wil je nog verder? <b>Feedback geven en vragen</b> · workshop · gratis</span></p>'
S=OCSS+PROGCSS+"""
.hero{background-image:url(spots/bg-hoe-je-werkt.jpg);background-size:1440px auto;background-position:center bottom -30px;height:400px;background-color:#eef2f7}
.hero:before{content:"";position:absolute;inset:0;background:radial-gradient(ellipse 720px 240px at 24% 20%,rgba(246,248,251,.85),rgba(246,248,251,0) 70%)}
.hero .hin{padding-top:46px}
.wrap{padding-top:22px}
.ptabs{display:inline-flex;gap:2px;background:var(--surf);border:1px solid var(--border);border-radius:14px;padding:4px}.ptabs span{padding:8px 15px;border-radius:10px;font-weight:650;font-size:.9rem;color:var(--brand)}.ptabs span.on{background:var(--brand);color:#fff}
.ptsel{display:none}
.tg{display:grid;grid-template-columns:1fr 360px;gap:22px;align-items:start;margin-top:4px}.tg>*{min-width:0}
.cols{display:grid;gap:14px}
.comp{background:var(--surf);border:1px solid var(--border);border-radius:18px;padding:18px 22px}
.ch{display:flex;gap:12px;align-items:center}.ch h3{font-size:1.08rem}.ci{width:36px;height:36px;border-radius:11px;display:grid;place-items:center;background:#eef2f7;color:var(--brand);flex-shrink:0}.ci svg{width:19px;height:19px}
.ctag{margin-left:auto;font-size:.76rem;font-weight:700;border-radius:99px;padding:3px 10px;white-space:nowrap}.ctag.ok{background:#e8f3ec;color:#1f5135}.ctag.gr{background:#fdf0e3;color:#a2501d}.ctag.mid{background:#fdf3dc;color:#7a5513}.ctag.bal{background:#eef2f7;color:#2c4a73}
.comp.gr .ci{background:#fdf0e3;color:#a2501d}.comp.ok .ci{background:#e8f3ec;color:#1f5135}.comp.mid .ci{background:#fdf3dc;color:#7a5513}
.ct{color:#34465c;font-size:.94rem;margin-top:8px}
.more{display:flex;gap:8px;align-items:center;margin-top:10px;font-size:.86rem;color:#4a5a6e}.more svg{width:15px;height:15px;color:#2f7a4f;flex-shrink:0}.more b{color:var(--brand);font-weight:650}
.grow{margin-top:14px;background:#fdf8f1;border:1px solid #f1e5d3;border-radius:14px;padding:14px 16px;display:grid;gap:10px}
.gh{display:flex;gap:14px;align-items:center}.gh .spot{width:58px;height:58px}.gh h4{font-size:.98rem;color:#a2501d}.gh p{font-size:.88rem;color:#3d4e63;margin-top:2px}
.crow{display:flex;gap:14px;align-items:center;justify-content:space-between;background:#fff;border:1px solid #eee3d5;border-radius:12px;padding:11px 14px}
.crow .cl{min-width:0;display:grid;gap:4px}.crow .top{display:flex;gap:6px;flex-wrap:wrap}.crow b{color:var(--brand);font-size:.95rem}
.crow .go{display:inline-flex;gap:4px;align-items:center;font-weight:650;color:var(--brand);font-size:.88rem;white-space:nowrap}.crow .go svg{width:15px;height:15px}
.side2{display:grid;gap:14px;position:sticky;top:16px}
.lg{overflow:hidden;padding:0}.lg .im{height:150px;background:url(spots/opl-band.jpg) center 62%/cover}.lg .lb{padding:16px 20px 18px}.lg h3{font-size:1.08rem}.lg p{font-size:.9rem;color:#3d4e63;margin-top:4px}.lg .btn{margin-top:12px;width:100%}
.lg ul{list-style:none;margin:10px 0 0;padding:0;display:grid;gap:6px;font-size:.88rem;color:#34465c}.lg li{display:flex;gap:8px;align-items:center}.lg li svg{width:15px;height:15px;color:#2f7a4f}
.priv{display:flex;gap:12px;align-items:flex-start;padding:14px 16px;background:#f4f7fb;border-color:#dfe7f1;font-size:.86rem;color:#34465c}.priv svg{width:20px;height:20px;color:var(--brand);flex-shrink:0}.priv b{color:var(--brand);display:block}
.how2{padding:16px 18px}.how2 h3{font-size:.98rem}.how2 p{font-size:.86rem;color:#3d4e63;margin-top:4px}
.intro{display:flex;justify-content:space-between;align-items:flex-end;gap:16px;margin:18px 0 12px}.intro h2{font-size:1.25rem}.intro p{color:var(--muted);font-size:.9rem}
@media(max-width:600px){
 .hero{height:auto;background-size:900px auto;background-position:left -12px bottom -10px}.hero .hin{padding-bottom:150px}
 .hero:before{background:linear-gradient(180deg,rgba(246,248,251,.9) 0%,rgba(246,248,251,.5) 45%,rgba(246,248,251,0) 65%)}
 .ptabs{display:none}.ptsel{display:flex;justify-content:space-between;align-items:center;background:var(--surf);border:1px solid var(--border);border-radius:12px;padding:10px 14px;font-weight:650;color:var(--brand)}.ptsel svg{width:18px;height:18px}
 .intro{flex-direction:column;align-items:flex-start;gap:4px;margin:16px 0 10px}
 .tg{grid-template-columns:1fr;gap:14px}.side2{position:static}
 .comp{padding:16px}.ch{flex-wrap:wrap;row-gap:4px}.ch h3{flex:1 1 calc(100% - 60px)}.ch .ctag{order:3;margin-left:48px}
 .grow{padding:12px}.gh .spot{width:48px;height:48px}
 .crow{flex-direction:column;align-items:flex-start;gap:8px}
}"""
B=header("Paspoort")+f"""<section class="hero"><div class="hin"><span class="eyb c" style="color:#2c4a73">Paspoort · Mijn tests</span><h1 style="margin-top:4px">Hoe jij werkt</h1><p class="lead">Wat je test over je laat zien, in gewone woorden. Bij elk groeipunt staat meteen wat je kunt doen.</p></div></section>
<main class="wrap"><div class="ptabs"><span>Wie ik ben</span><span class="on">Mijn tests</span><span>Past deze baan?</span><span>Carrière</span><span>Bewijzen</span><span>Mijn gegevens</span></div><div class="ptsel">Mijn tests {AI["down"]}</div>
<div class="intro"><div><h2>Je sterke punten en groeipunten</h2><p>Uit je Quick-Scan en Verdieping, 35 vragen.</p></div><span class="btn line sm">Rapport downloaden</span></div>
<div class="tg"><div class="cols">
{comp("Samenwerken","Sterk punt","ok","users","Samenwerken is een sterke kant van jou. Je trekt anderen mee en houdt rekening met het team.",more)}
{comp("Rustig blijven onder druk","Hier kun je in groeien","gr","leaf","Drukte of onverwachte wendingen raken je soms. Dat is heel gewoon, en het is iets wat je kunt leren.",grow1)}
{comp("Klantcontact","Sterk punt","ok","chat","Je maakt makkelijk contact en voelt aan wat iemand nodig heeft. Fijn aan een balie of in de winkel.")}
{comp("Plannen en afmaken","Groeit nog","mid","target","Je pakt dingen graag op. Het afronden als het lang duurt, kan nog makkelijker worden.",grow2)}
{comp("Nieuwe dingen proberen","In balans","bal","spark","Je houdt van wat werkt, en staat open voor iets nieuws als je het nut ziet.")}
</div>
<div class="side2"><section class="card lg"><div class="im"></div><div class="lb"><h3>Leren &amp; groeien</h3><p>3 workshops passen bij je groeipunten.</p><ul><li>{AI["check"]}Allemaal gratis</li><li>{AI["check"]}Dichtbij in Zaandam of online</li><li>{AI["check"]}Afgerond? Dan staat het in je paspoort</li></ul><span class="btn">Bekijk alles</span></div></section>
{prog_mini().replace("Jouw voortgang op deze route","Jouw voortgang")}
<section class="card how2"><h3>Hoe kiezen we dit?</h3><p>We kijken naar je groeipunten, je route en wat dichtbij is. Gratis staat altijd bovenaan. Een partnerlink zie je altijd aan het label.</p></section>
<section class="card priv">{AI["shield"]}<span><b>Alleen jij ziet je groeipunten</b>Werkgevers zien alleen wat jij deelt en wat anderen hebben bevestigd.</span></section></div></div>
</main>"""+LF+tabbar("Paspoort")
page("testresultaten-groeien",S,B)
