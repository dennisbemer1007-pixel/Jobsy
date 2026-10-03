from screens import wrap, NOTE
def bars(items, color="var(--grad)"):
    return ''.join(f'<div style="margin-top:9px"><div class="row"><span style="font-weight:600;color:var(--ink)">{a}</span><b style="color:var(--purple)">{p}%</b></div><div class="bar" style="height:9px"><i style="width:{p}%;background:{color}"></i></div></div>' for a,p in items)
kpi = lambda n,l,s,cl='p': f'<div class="card {cl}" style="padding:14px 16px"><div class="disp" style="font-size:28px;font-weight:900;color:var(--ink)">{n}</div><b style="color:var(--ink)">{l}</b><div class="sub">{s}</div></div>'
i = f'''<div style="height:900px;background:#f7f5fb;position:relative;padding:0 0 30px">
<div style="background:repeating-linear-gradient(45deg,#fff3cd,#fff3cd 14px,#ffeaa0 14px,#ffeaa0 28px);padding:9px 28px;font:900 15px Nunito;color:#7a5200;letter-spacing:.04em;text-align:center">CONCEPT – LATER · Lobsy voor teams · niet gepland voor bouw · ter bespreking</div>
<div style="padding:18px 28px">
<div class="row" style="align-items:center"><div style="display:flex;align-items:center;gap:12px"><img src="lobsy.png" style="width:34px"><div><div class="disp" style="font-size:22px;font-weight:900;color:var(--ink)">Teamscan najaar 2026 <span class="chip o" style="font-size:10px">Lobsy voor teams</span></div><div class="sub">Gemeente Voorbeeld (fictief) · voor eigen medewerkers · loopbaangesprekken & interne mobiliteit · 1 – 31 okt 2026</div></div></div>
<div style="display:flex;gap:6px"><span class="chip">Teamcode GEM-VBD</span><span class="chip o">Rapport (PDF) ↓</span></div></div>
<div style="margin-top:14px;background:linear-gradient(90deg,#f4effb,#fff1ec);border:1.5px solid #dccdf2;border-radius:14px;padding:12px 16px;display:flex;gap:20px;font-size:12px">
<div><b style="color:var(--purple)">👥 Minimaal 10 per groep</b><div class="sub" style="font-size:11px">Kleinere groepen blijven verborgen.</div></div>
<div><b style="color:var(--purple)">∑ Alleen totalen</b><div class="sub" style="font-size:11px">Geen namen, antwoorden of filters die iemand herleidbaar maken.</div></div>
<div><b style="color:var(--purple)">🙋 Vrijwillig</b><div class="sub" style="font-size:11px">Niet meedoen heeft geen gevolgen; niemand ziet wie meedeed.</div></div>
<div><b style="color:var(--purple)">🏛 OR-instemming</b><div class="sub" style="font-size:11px">✓ OR gemeente, 12 sep 2026</div></div>
<div><b style="color:var(--purple)">🦞 Eigen paspoort</b><div class="sub" style="font-size:11px">Medewerkers houden hun paspoort; het bedrijf ziet het niet.</div></div></div>
<div style="display:flex;gap:6px;margin-top:14px;align-items:center"><span class="sub" style="margin-right:4px">Afdeling:</span>
<span class="pill" style="background:var(--purple);color:#fff;padding:5px 12px">Hele gemeente · 70</span><span class="pill" style="background:#fff;border:1px solid var(--line);padding:5px 12px">Sociaal domein · 22</span><span class="pill" style="background:#fff;border:1px solid var(--line);padding:5px 12px">Publiekszaken · 16</span><span class="pill" style="background:#fff;border:1px solid var(--line);padding:5px 12px">Openbare ruimte · 14</span><span class="pill" style="background:#fff;border:1px solid var(--line);padding:5px 12px">Bedrijfsvoering · 11</span><span class="pill" style="background:#f1eef5;color:#b3abc2;padding:5px 12px">🔒 ICT · &lt;10, verborgen</span>
<span class="sub" style="margin-left:auto">Geen extra filters (leeftijd, schaal, contract) — die maken mensen herleidbaar.</span></div>
<div class="grid g4" style="margin-top:14px">{kpi(112,"Uitgenodigd","via teamcode, vrijwillig")}{kpi(70,"Meegedaan","63% · minimaal 10 per afdeling","m")}{kpi("4 / 5","Afdelingen zichtbaar","ICT onder de drempel","c")}{kpi("4,2 / 5","Ervaren nut","“leerde iets over mezelf”","")}</div>
<div class="grid g3" style="margin-top:14px;gap:14px">
<div class="card" style="padding:16px"><b class="disp" style="font-size:16px;color:var(--ink)">🦞 Talenten in het team</b><div class="sub">% deelnemers met dit talent in hun top 3</div>
{bars([("Inwoners helpen / klantgericht",64),("Zorgvuldig werken",57),("Samenwerken",52),("Problemen oplossen",38),("Anderen iets leren",17)])}</div>
<div class="card" style="padding:16px"><b class="disp" style="font-size:16px;color:var(--ink)">⚡ Energiebronnen</b><div class="sub">waar het team energie van krijgt</div>
{bars([("Iets betekenen voor inwoners",71),("Fijne collega's",60),("Afwisseling in taken",44),("Zelf mogen beslissen",36),("Buiten werken",20)],"linear-gradient(90deg,#f5503a,#ff9a6b)")}</div>
<div class="card" style="padding:16px"><b class="disp" style="font-size:16px;color:var(--ink)">🌱 Groeiwensen</b><div class="sub">wat medewerkers willen leren</div>
{bars([("Digitale vaardigheden",48),("Andere afdeling / rol (mobiliteit)",31),("Gesprekstechnieken",27),("Projectmatig werken",22),("Leidinggeven",15)],"linear-gradient(90deg,#127a5c,#4cc59a)")}</div></div>
<div class="grid g2" style="margin-top:14px;gap:14px;grid-template-columns:1.4fr 1fr">
<div class="card p" style="padding:14px 16px"><b class="disp" style="font-size:15px;color:var(--ink)">💡 Gespreksstarters voor het team</b><ul class="why" style="font-size:12px;margin-top:6px"><li>31% overweegt een andere afdeling of rol: zet interne vacatures eerst intern open (interne mobiliteit).</li><li>48% wil digitaal sterker worden: past een opleidingsbudget per afdeling?</li><li>Loopbaangesprek: medewerkers kunnen hun eigen paspoort meenemen — alleen als zíj dat willen.</li></ul></div>
<div class="card" style="padding:14px 16px"><b class="disp" style="font-size:15px;color:var(--ink)">Licentie (voorbeeld)</b><div class="sub" style="font-size:12px;margin-top:4px">Per medewerker per jaar, bijv. <b style="color:var(--purple)">€12–€20</b> (indicatief, excl. btw). Medewerkers betalen nooit. <b>Upsell</b> voor gemeenten die al Lobsy-klant zijn (bijv. voor werkzoekenden).</div><div class="sub" style="margin-top:6px">Teamscan staat los van beoordeling en P&amp;C-cyclus: geen koppeling met functioneren, sollicitaties of personeelsdossier.</div></div></div>
</div>{NOTE}</div>'''
open('i.html','w').write(wrap(i,1440,900))
print('ok')
