exec(open(D+"/app.inc.py").read())
import math
def arc(start,frac,color,w=10,r=46):
  a0=math.radians(start*360-90);a1=math.radians((start+frac)*360-90)
  x0,y0=60+r*math.cos(a0),60+r*math.sin(a0);x1,y1=60+r*math.cos(a1),60+r*math.sin(a1)
  return f'<path d="M{x0:.1f} {y0:.1f} A{r} {r} 0 {1 if frac>.5 else 0} 1 {x1:.1f} {y1:.1f}" stroke="{color}" stroke-width="{w}" fill="none" stroke-linecap="round"/>'
ring='<svg viewBox="0 0 120 120" class="ring"><circle cx="60" cy="60" r="46" stroke="#ece4d9" stroke-width="10" fill="none"/>'+arc(.01,.23,"#0f2d5c")+arc(.26,.23,"#2f7a4f")+arc(.51,.23,"#c99a2e")+arc(.76,.10,"#9a5b2a")+'<image href="lobsy-256.png" x="34" y="34" width="52" height="52"/></svg>'
Q=[("star","#0f2d5c","Jouw sterkste stap","Samen &amp; aardig","Volledig",""),("brief","#2f7a4f","Werk dat bij je past","Zorg en dieren","Volledig",""),
("home","#c99a2e","Hier voel je je thuis","Rustig en zelfstandig","Volledig",""),("heart","#9a5b2a","Dit vind je belangrijk","Nog niet helemaal ontdekt","","Maak de test af")]
quad="".join(f'<div class="q" style="--c:{c}"><span class="qi">{AI[k]}</span><small>{t}</small><b class="{"" if s else "muted"}">{v}</b>{f"<em>{s}</em>" if s else f"<span class=lnk2>{a} →</span>"}</div>' for k,c,t,v,s,a in Q)
FIELDS=[("Ik woon in","Zaandam"),("Reizen","30 min fietsen"),("Uren per week","24 uur"),("Beschikbaar","Per direct"),("Talen","Nederlands, Engels"),("Rijbewijs","Nee")]
fields="".join(f'<div><small>{a}</small><b>{b}</b></div>' for a,b in FIELDS)
TABS=[("ladder","Wie ik ben",1,""),("tests","Mijn tests",0,"1"),("target","Past deze baan?",0,""),("car","Carrière",0,""),("cloud","Bewijzen",0,""),("user","Mijn gegevens",0,"")]
tabs="".join(f'<span class="tb{" on" if o else ""}">{AI[k]}{n}{f"<i>{b}</i>" if b else ""}</span>' for k,n,o,b in TABS)
ST=[("Eerste test",1),("Eerste rapport",0),("CV erbij",0),("3 tests gedaan",1),("Wie ik ben, klaar",1),("Eerste baan",0)]
stamps="".join(f'<div class="sp{" got" if g else ""}"><span>{AI["check"] if g else AI["leaf"]}</span><small>{n}</small></div>' for n,g in ST)
S=APPCSS+"""
.wrap{max-width:1180px;margin:0 auto;padding:24px 28px 48px}
.band{display:flex;align-items:center;justify-content:space-between;gap:24px;background:#fbf2e3;border:1px solid #efe3d1;border-radius:20px;padding:0 0 0 30px;overflow:hidden;min-height:150px}
.band h1{font-size:1.7rem}.band p{color:#3d4e63;margin-top:6px;max-width:520px}.band .bt{padding:24px 0}
.band .sart{align-self:stretch;width:380px;flex-shrink:0;background:url(spots/dna.jpg) no-repeat center 55%/cover;-webkit-mask-image:linear-gradient(90deg,transparent,#000 22%);mask-image:linear-gradient(90deg,transparent,#000 22%)}
.grid{display:grid;grid-template-columns:330px 1fr;gap:22px;margin-top:22px;align-items:start}
.card{background:var(--surf);border:1px solid var(--border);border-radius:18px}
.pp .ph{background:var(--brand);color:#fff;border-radius:17px 17px 0 0;padding:14px 20px}.pp .ph small{display:block;font-weight:700;letter-spacing:.06em;font-size:.72rem}.pp .ph span{font-size:.85rem;color:#c9d5e8}
.pp .pb{padding:18px 20px 20px}.pid{display:flex;gap:14px;align-items:center}.pid .big{width:50px;height:50px;border-radius:50%;background:var(--brand);color:#fff;display:grid;place-items:center;font-weight:700}
.pid b{display:block;color:var(--brand);font-size:1.08rem}.avail{display:inline-block;margin-top:4px;font-size:.75rem;font-weight:700;color:#1f5135;background:#e8f3ec;border:1px solid #bfe0cb;border-radius:99px;padding:2px 9px}
.prog{display:flex;gap:4px;margin:16px 0 6px}.prog i{flex:1;height:5px;border-radius:99px;background:#e3dbd0}.prog i.on{background:var(--brand)}
.pmeta{font-size:.85rem;color:var(--muted)}.pmeta b{color:var(--brand)}
.ver{margin-top:16px;padding-top:14px;border-top:1px solid var(--border)}.ver small,.flds small{display:block;font-size:.72rem;font-weight:700;color:var(--muted);letter-spacing:.04em;text-transform:uppercase}.ver p{margin-top:4px;color:#34465c;font-size:.92rem}
.flds{display:grid;grid-template-columns:1fr 1fr;gap:12px 14px;margin-top:16px;padding-top:14px;border-top:1px solid var(--border)}.flds b{font-size:.92rem;color:var(--text);font-weight:600}
.seek{margin-top:14px}.seek small{display:block;font-size:.72rem;font-weight:700;color:var(--muted);letter-spacing:.04em;text-transform:uppercase;margin-bottom:6px}.seek span{display:inline-block;font-size:.82rem;font-weight:600;color:var(--brand);background:var(--soft);border-radius:99px;padding:3px 10px;margin:0 4px 4px 0}
.pbtn{display:flex;gap:10px;margin-top:18px;flex-wrap:wrap}.pbtn .btn{padding:10px 16px;font-size:.92rem}
.pfoot{margin-top:14px;font-size:.8rem;color:var(--muted)}
.dbj{padding:20px 24px}.priv{display:flex;gap:8px;align-items:center;color:var(--muted);font-size:.85rem}.priv svg{width:16px;height:16px}
.dtop{display:flex;gap:22px;align-items:center;margin:14px 0 18px}.ring{width:120px;height:120px;flex-shrink:0}
.dtop h2{font-size:1.4rem}.dtop p{color:#3d4e63;margin:2px 0 10px}.leg{display:flex;flex-wrap:wrap;gap:6px 14px;font-size:.82rem;color:#3d4e63}.leg span:before{content:"";display:inline-block;width:9px;height:9px;border-radius:3px;background:var(--c);margin-right:6px}
.quad{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.q{position:relative;background:#fbf7f1;border:1px solid #eee5da;border-radius:14px;padding:14px 16px 14px 60px;min-height:92px}
.qi{position:absolute;left:14px;top:14px;width:34px;height:34px;border-radius:10px;background:#fff;border:1px solid #eee5da;color:var(--c);display:grid;place-items:center}.qi svg{width:18px;height:18px}
.q small{display:block;color:var(--muted);font-size:.8rem;font-weight:600}.q b{display:block;color:var(--brand);font-size:1.02rem;margin:2px 0}.q b.muted{color:#5a6a7d}.q em{font-style:normal;font-size:.75rem;font-weight:700;color:#1f5135}
.lnk2{font-size:.85rem;font-weight:650;color:var(--brand);text-decoration:underline;text-underline-offset:3px}
.tip.vt{flex-direction:column;align-items:stretch;padding:0;gap:0;margin-top:22px}.tip.vt .tart{width:auto;height:120px;border-radius:17px 17px 0 0;background-position:30% 25%}.tip.vt .tt{padding:14px 20px 12px}.tip.vt .btn{margin:0 20px 18px}
.tip{display:flex;gap:18px;align-items:center;overflow:hidden}
.tip .tart{width:150px;align-self:stretch;flex-shrink:0;background:url(spots/vragen.jpg) no-repeat 30% 30%/cover;border-radius:17px 0 0 17px}
.tip .tt{flex:1;padding:18px 0}.tip small{color:#b4461f;font-weight:700;font-size:.8rem}.tip p{color:#34465c;margin-top:2px}.tip .btn{padding:10px 16px;font-size:.92rem}
.tabs{display:flex;gap:2px;margin-top:14px;background:var(--surf);border:1px solid var(--border);border-radius:14px;padding:4px}.tb{flex:1 1 auto;display:inline-flex;gap:6px;align-items:center;justify-content:center;padding:8px 9px;border-radius:10px;font-weight:650;font-size:.86rem;color:var(--brand);white-space:nowrap}
.tb svg{width:16px;height:16px;flex-shrink:0}.tb.on{background:var(--brand);color:#fff;border-color:var(--brand)}.tb i{font-style:normal;font-size:.72rem;background:#f8ecd9;color:#8f6232;border-radius:99px;padding:1px 7px}
.three{display:grid;grid-template-columns:repeat(3,1fr);gap:14px;margin-top:14px}.three .card{display:flex;flex-direction:column;padding:18px 20px}.three .card .btn{margin-top:auto;align-self:flex-start}.three .card p+.btn{margin-top:14px}
.three small{display:block;font-size:.72rem;font-weight:700;color:var(--muted);letter-spacing:.05em;text-transform:uppercase}.three h3{font-size:1.05rem;margin:4px 0 8px}.three p{color:#3d4e63;font-size:.92rem}
.str{display:flex;gap:10px;align-items:center;margin-top:10px;background:#f4f7fb;border-radius:12px;padding:10px 12px}.str span{width:30px;height:30px;border-radius:50%;background:#fff;color:var(--brand);display:grid;place-items:center}.str svg{width:16px;height:16px}.str b{display:block;color:var(--brand);font-size:.95rem}.str em{font-style:normal;font-size:.8rem;color:var(--muted)}
.sl{margin-top:10px}.sl div{display:flex;justify-content:space-between;font-size:.8rem;color:var(--muted)}.sl div b{color:var(--brand);font-weight:650}.sl i{display:block;height:6px;border-radius:99px;background:#ece4d9;margin:4px 0 0;position:relative}.sl i:after{content:"";position:absolute;top:-3px;width:12px;height:12px;border-radius:50%;background:var(--brand);left:var(--p)}
.more{list-style:none;margin:10px 0 0;padding:0;display:grid;gap:6px;font-size:.9rem;color:#34465c}.more li:before{content:"";display:inline-block;width:6px;height:6px;border-radius:50%;background:#c9a27a;margin-right:9px;vertical-align:2px}
.vw{margin-top:12px;font-size:.85rem}.vw b{color:var(--brand)}
.three .btn{padding:9px 16px;font-size:.9rem}
.stc{margin-top:14px;padding:18px 20px}.sth{display:flex;justify-content:space-between;gap:20px;align-items:flex-start}.sth h3{font-size:1.1rem}.sth p{color:var(--muted);font-size:.88rem;margin-top:2px}.sth .r{color:#3d4e63;font-size:.86rem;background:#fbf7f1;border-radius:10px;padding:8px 12px;max-width:300px}
.stamps{display:grid;grid-template-columns:repeat(6,1fr);gap:10px;margin-top:16px}.sp{text-align:center}.sp span{display:grid;place-items:center;width:52px;height:52px;margin:0 auto;border-radius:50%;border:1.5px dashed #d7c9b7;color:#b9a68f}.sp svg{width:20px;height:20px}
.sp.got span{border:0;background:#e8f3ec;color:#1f5135}.sp small{display:block;margin-top:6px;font-size:.78rem;font-weight:650;color:#3d4e63}
@media(max-width:600px){
 .wrap{padding:14px 14px 28px}.band{padding:0;flex-direction:column-reverse;align-items:stretch;gap:0;min-height:0}.band .sart{width:auto;height:120px;-webkit-mask-image:none;mask-image:none}.band .bt{padding:16px 18px 18px}.band h1{font-size:1.4rem}
 .grid{display:flex;flex-direction:column;gap:14px;margin-top:14px}.lcol{display:contents}.pp{order:1}.grid>div:last-child{order:2}.tip.vt{order:3;margin-top:0}
 .dbj{padding:16px}.dtop{gap:14px}.ring{width:88px;height:88px}.dtop h2{font-size:1.2rem}
 .quad{grid-template-columns:1fr}.q{min-height:0}
 .tabs{display:grid;grid-template-columns:1fr 1fr;gap:2px}.tb{justify-content:flex-start;padding:9px 12px;font-size:.84rem}
 .three{grid-template-columns:1fr}.sth{flex-direction:column}.sth .r{max-width:none}.stamps{grid-template-columns:repeat(3,1fr);row-gap:14px}
}"""
B=header("Paspoort")+f"""<main class="wrap">
<section class="band"><div class="bt"><h1>Goedemiddag, Sanne</h1><p>Dit is jouw Kompas: wie je bent, wat je kunt en wat bij je past. Alleen jij ziet dit, jij kiest wat je deelt.</p></div><div class="sart"></div></section>
<div class="grid"><div class="lcol"><aside class="card pp"><div class="ph"><small>LOBSY PASPOORT</small><span>LB-60388</span></div><div class="pb">
<div class="pid"><span class="big">SV</span><div><b>Sanne de Vries</b><span class="avail">Beschikbaar voor werk</span></div></div>
<div class="prog"><i class="on"></i><i class="on"></i><i class="on"></i><i class="on"></i><i></i><i></i></div><div class="pmeta">Profiel · 4 van 6 · <b>aanvullen</b></div>
<div class="ver"><small>Mijn verhaal</small><p>Ik werk graag met mensen en dieren en wil later dierenarts worden. Ik ben rustig, zorgvuldig en een harde werker.</p></div>
<div class="flds">{fields}</div>
<div class="seek"><small>Wat ik zoek</small><span>Dierenverzorging</span><span>Zorg</span><span>Retail</span></div>
<div class="pbtn"><span class="btn line">DNA-paspoort (PDF)</span><span class="btn">Aanpassen</span></div>
<p class="pfoot">Lobsy: ontdek wie je bent onder de schaal</p></div></aside>
<section class="card tip vt"><div class="tart"></div><div class="tt"><small>Tip van Lobsy</small><p>Vergroot je kans op een gesprek: maak je profiel compleet.</p></div><span class="btn">Paspoort aanvullen</span></section></div>
<div><section class="card dbj"><div class="priv">{AI["shield"]}Alleen jij ziet je paspoort. Jij kiest wat je deelt.</div>
<div class="dtop">{ring}<div><h2>Dit ben jij</h2><p>Jouw kreeft · fase 3 van 4</p><div class="leg"><span style="--c:#0f2d5c">Wat je kunt</span><span style="--c:#2f7a4f">Beroepen</span><span style="--c:#c99a2e">Cultuur</span><span style="--c:#9a5b2a">Waarden</span></div></div></div>
<div class="quad">{quad}</div></section>
<div class="tabs">{tabs}</div>
<div class="three"><div class="card"><small>Onder je schaal</small><h3>Wat maakt jou jou</h3><div class="str"><span>{AI["star"]}</span><div><b>Samen &amp; aardig</b><em>Sterkste punt</em></div></div><ul class="more"><li>Afmaken en netjes werken</li><li>Kalm blijven onder druk</li></ul></div>
<div class="card"><small>Jouw verhaal</small><h3>Je tests zijn bijna klaar</h3><p>Nog één onderdeel: Waarden. Maak die af, dan schrijven we jouw verhaal in gewone woorden.</p><span class="btn">Naar de tests</span></div>
<div class="card"><small>Cultuur</small><h3>Waar voel jij je thuis</h3><div class="sl"><div><span>Veel sturing</span><b>Zelfstandig</b></div><i style="--p:72%"></i></div><div class="sl"><div><span>Formeel</span><b>Informeel</b></div><i style="--p:64%"></i></div><p class="vw"><b>Waarden op werk</b> · nog niet klaar</p></div></div>
<section class="card stc"><div class="sth"><div><h3>Mijn schalen</h3><p>Hier werp je je oude schaal af · 3 van 6<br>Stempels voor test, rapport en CV. De reis telt apart.</p></div><p class="r">Groot worden doe je door je schaal af te werpen. Nog 6 vragen tot je volgende schaal.</p></div><div class="stamps">{stamps}</div></section>
</div></div></main>"""+LF+tabbar("Paspoort")
page("kompas",S,B,False)
