const Q=new URLSearchParams(location.search);const MOB=innerWidth<700;const STATE=Q.get('s')||'none';
document.body.classList.add('s-'+STATE);const POPS=['popup'];const ISPOP=POPS.includes(STATE);if(ISPOP)document.body.classList.add('s-anypop');
const C=[4.2085,51.9935];const K=(dx,dy)=>[C[0]+dx/68.55,C[1]+dy/111.2];
const BIKE='<svg class="ic" viewBox="0 0 24 24"><circle cx="6" cy="16" r="3.5"/><circle cx="18" cy="16" r="3.5"/><path d="M6 16l4-7h5l3 7M10 9 8.5 6H7M12.5 16 15 9"/></svg>';
// pin shapes (zelfde vorm in kaart, lijst en legenda)
function pinW(s=1,sel){const w=40*s,h=50*s;return `<svg width="${w}" height="${h}" viewBox="0 0 40 50"><path d="M20 47C20 47 4 31 4 19A16 16 0 0 1 36 19C36 31 20 47 20 47Z" fill="#1F2F55" stroke="#fff" stroke-width="${sel?3.5:2.5}"/><g fill="none" stroke="#fff" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><rect x="13.5" y="11" width="13" height="16" rx="1.5"/><path d="M17 15h1.5M21.5 15H23M17 19h1.5M21.5 19H23M18.5 27v-3.5h3V27"/></g></svg>`}
function pinI(s=1,sel,badge=true){const w=46*s,h=50*s;return `<svg width="${w}" height="${h}" viewBox="0 0 46 50"><path d="M10 4H34A7 7 0 0 1 41 11V32A7 7 0 0 1 34 39H28L22 47L16 39H10A7 7 0 0 1 3 32V11A7 7 0 0 1 10 4Z" fill="#B4500F" stroke="#fff" stroke-width="${sel?3.5:2.5}"/><g fill="none" stroke="#fff" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><circle cx="17" cy="16" r="3.2"/><circle cx="28" cy="16" r="3.2"/><path d="M11 30c.6-4 3-6 6-6s5.4 2 6 6M22 30c.6-4 3-6 6-6s5.4 2 6 6"/></g>${badge?'<g><rect x="27" y="0" width="19" height="13" rx="6.5" fill="#fff" stroke="#B4500F" stroke-width="1.5"/><text x="36.5" y="9.8" text-anchor="middle" font-family="Noto Sans" font-weight="800" font-size="8.5" fill="#8A3B08">via</text></g>':''}</svg>`}
const miniW=()=>pinW(.5),miniI=()=>pinI(.5,0,false);
const TMW=`<span class="tm w">${miniW()}Bij werkgever</span>`,TMI=`<span class="tm i">${miniI()}Via intermediair</span>`;
const ICO={truck:'<path d="M3 6.5h11v10H3zM14 10h4l3 3.5v3h-7"/><circle cx="7" cy="17.5" r="1.8"/><circle cx="17.5" cy="17.5" r="1.8"/>',leaf:'<path d="M5 19c0-8 5-13 14-14 0 9-5 14-13 14"/><path d="M5 19 13 11"/>',care:'<path d="M12 20s-7.5-4.6-7.5-10.2A4.3 4.3 0 0 1 12 7.4a4.3 4.3 0 0 1 7.5 2.4C19.5 15.4 12 20 12 20z"/>',box:'<path d="M4 8l8-4 8 4v8l-8 4-8-4z"/><path d="M4 8l8 4 8-4M12 12v8"/>',cup:'<path d="M5 8h11v5a5 5 0 0 1-5 5h-1a5 5 0 0 1-5-5z"/><path d="M16 9.5h1.5a2.5 2.5 0 0 1 0 5H16"/>',shop:'<path d="M4 9l1.5-4.5h13L20 9M4 9h16v10H4zM9 19v-5h6v5"/>'};
const V=[['Teeltmedewerker kas','Kwekerij De Voorbeeldkas',0,'leaf','Tuinbouw','#E3EEDD','#3F6E31','9 min','36 tot 40 uur'],
['Orderpicker','Uitzendbureau Voorbeeld',1,'truck','Logistiek','#F3EADB','#7E5E2C','12 min','32 tot 40 uur'],
['Verzorgende IG','Zorgcentrum Het Voorbeeld',0,'care','Zorg','#E5EEE9','#2F7D5B','14 min','24 tot 32 uur'],
['Inpakker bloemen','Flexwerk Westland (voorbeeld)',1,'box','Productie','#F5E8E1','#97563F','7 min','32 tot 38 uur']];
document.getElementById('list').innerHTML=V.map(v=>`<div class="vc"><div class="vimg" style="background:linear-gradient(135deg,${v[5]},#fff 150%);color:${v[6]}"><svg class="big" viewBox="0 0 24 24">${ICO[v[3]]}</svg><span class="vtag">${v[4]}</span></div><div class="vb"><b>${v[0]}</b><div class="who">${v[2]?TMI:TMW}</div><div class="who" style="margin-top:4px">${v[1]}</div><div class="meta"><span>${BIKE.replace('class="ic"','class="ic s"')}${v[7]}</span><span>${v[8]}</span></div></div></div>`).join('');
document.getElementById('strip').innerHTML=V.map(v=>`<div class="sc"><span class="ico" style="background:${v[5]};color:${v[6]}"><svg viewBox="0 0 24 24">${ICO[v[3]]}</svg></span><div><b>${v[0]}</b><small>${v[2]?miniI():miniW()}${v[1]}</small></div></div>`).join('');
const LEG=(big)=>`${big?`<div class="lg"><span class="sym"><i class="ln"></i></span><div>10 min fietsen</div></div>
<div class="lg"><span class="sym"><i class="ln d"></i></span><div>20 min fietsen</div></div>
<div class="lg"><span class="sym"><i class="ln dt"></i></span><div>30 min fietsen</div></div>`:`<h4>Wat zie je op de kaart?</h4><div class="lgr"><span><i class="ln"></i>10</span><span><i class="ln d"></i>20</span><span><i class="ln dt"></i>30 min fietsen</span></div>`}
<div class="lg"><span class="sym"><span class="cl" style="width:32px;height:32px;font-size:13px;border-width:2px;box-shadow:none">5</span></span><div>Groep vacatures<small>${MOB?'Tik':'Klik'} om in te zoomen</small></div></div>
<div class="lg"><span class="sym">${pinW(.72)}</span><div>Vacature bij werkgever</div></div>
<div class="lg"><span class="sym">${pinI(.72)}</span><div>Vacature via intermediair<small>zoals een uitzendbureau</small></div></div>`;
document.getElementById('legend').innerHTML=LEG(0);
const X='<svg class="ic" viewBox="0 0 24 24"><path d="M6 6l12 12M18 6 6 18"/></svg>';
const POP={t:'Orderpicker',org:'Uitzendbureau Voorbeeld',rt:'12 min fietsen',ll:K(2.5,-0.7)};
const popHTML=`<span class="x">${X}</span>${TMI}<h3>${POP.t}</h3><div class="org">${POP.org}<br>Werk bij een kwekerij in Honselersdijk</div><div class="rt">${BIKE}${POP.rt}</div><div class="go">Bekijk vacature</div>`;
const sheet=document.getElementById('sheet');
if(STATE=='legend')sheet.innerHTML=`<div class="grab"></div><span class="x">${X}</span><h2>Uitleg kaart</h2>${LEG(1)}<div class="close">Sluiten</div>`;
const HEART='<svg class="ic" viewBox="0 0 24 24"><path d="M12 20s-7.5-4.6-7.5-10.2A4.3 4.3 0 0 1 12 7.4a4.3 4.3 0 0 1 7.5 2.4C19.5 15.4 12 20 12 20z"/></svg>';
const CHEV='<svg class="ic s" viewBox="0 0 24 24"><path d="M10 7l5 5-5 5"/></svg>';
const CHL='<svg class="ic" viewBox="0 0 24 24"><path d="M14.5 6l-6 6 6 6"/></svg>',CHR='<svg class="ic" viewBox="0 0 24 24"><path d="M9.5 6l6 6-6 6"/></svg>';
const CLOCK='<svg class="ic s" viewBox="0 0 24 24"><circle cx="12" cy="12" r="8"/><path d="M12 8v4.5l3 2"/></svg>',DOC='<svg class="ic s" viewBox="0 0 24 24"><path d="M7 3.5h7l4 4V20H7z"/><path d="M14 3.5V8h4M10 12h5M10 15.5h5"/></svg>';
const FV={emp:{ll:K(1.2,0.9),img:'foto-kas.jpg',t:'Teeltmedewerker kas',org:'Kwekerij De Voorbeeldkas',pl:'Naaldwijk',rt:'9 min',hr:'36 tot 40 uur',ct:'Vast contract',i:0},
 int:{ll:K(2.5,-0.7),img:'foto-magazijn.jpg',t:'Orderpicker',org:'Uitzendbureau Voorbeeld',pl:'Honselersdijk',rt:'12 min',hr:'32 tot 40 uur',ct:'Uitzendcontract',i:1},
 clus:{ll:K(-1.5,2.2),img:'foto-zorg.jpg',t:'Verzorgende IG',org:'Zorgcentrum Het Voorbeeld',pl:'Monster',rt:'11 min',hr:'24 tot 32 uur',ct:'Tijdelijk contract',i:0}};
const FK=STATE=='empeur'?'emp':STATE;const EUR=STATE=='empeur';const FD=FV[FK];
const WAGE=[['14 jaar','€ 4,62'],['15 jaar','€ 4,62'],['16 jaar','€ 5,31'],['17 jaar','€ 6,08'],['18 jaar','€ 7,70'],['19 jaar','€ 9,24'],['20 jaar','€ 12,32'],['21 jaar en ouder','€ 15,40',1]];
function fcard(d,clus,eur){const lab=d.i?`<span class="flab i">${pinI(.46,0,false)}Via intermediair</span>`:`<span class="flab w">${pinW(.46)}Bij werkgever</span>`;
 const nav=clus?`<div class="fnav"><span class="fb" aria-label="Vorige vacature">${CHL}</span><span class="fcount">3 van 15</span><span class="fb" aria-label="Volgende vacature">${CHR}</span></div>`:'';
 const tip=eur?`<div class="tip" role="tooltip"><b>Uurloon (bruto)</b>${WAGE.map(w=>`<div class="tr2${w[2]?' me':''}"><span>${w[0]}</span><span>${w[1]}</span></div>`).join('')}</div>`:'';
 return `<div class="fcw${MOB?' m':''}"><div class="fctl">${nav}<span class="sp"></span><span class="fb euro${eur?' on':''}" aria-label="Uurloon bekijken">€${tip}</span><span class="fb fx" aria-label="Sluiten">${X}</span></div>
 <div class="fc${MOB?' m':''}"><div class="fbody"><div class="fph"><img src="${d.img}" alt=""></div>
 <div class="finfo"><div class="flrow">${lab}<span class="hrt" aria-label="Bewaren">${HEART}</span></div><h3>${d.t}</h3><div class="femp">${d.i?'via ':''}${d.org}</div><div class="fpl">${d.pl}</div>
 <div class="ffacts"><span>${BIKE.replace('class="ic"','class="ic s"')}${d.rt}</span><span>${CLOCK}${d.hr}</span><span>${DOC}${d.ct}</span></div></div></div>
 <div class="fgo"><span>Bekijk vacature</span>${CHR}</div></div></div>`}
const NEWST=['emp','empeur','int','clus'].includes(STATE);
if(NEWST&&MOB){const w=document.createElement('div');w.className='fcwrap';w.innerHTML=fcard(FD,STATE=='clus',EUR);document.body.appendChild(w)}
// isochronen (nepdata, onregelmatige ringen)
function ring(R,seed){const p=[];for(let i=0;i<=120;i++){const a=i/120*2*Math.PI;const r=R*(1+.11*Math.sin(3*a+seed)+.06*Math.sin(7*a+seed*2)+.035*Math.sin(13*a+seed*3));p.push(K(r*Math.cos(a),r*Math.sin(a)))}return p}
function ringPt(R,seed,a){const r=R*(1+.11*Math.sin(3*a+seed)+.06*Math.sin(7*a+seed*2)+.035*Math.sin(13*a+seed*3));return K(r*Math.cos(a),r*Math.sin(a))}
const RINGS=[[1.8,.4],[3.6,1.3],[5.4,2.1]];
const map=new maplibregl.Map({container:'map',style:'https://tiles.openfreemap.org/styles/positron',center:(MOB&&ISPOP)?K(1.6,0.2):C,zoom:MOB?10.9:11.8,attributionControl:{compact:false},preserveDrawingBuffer:true,
 padding:MOB?{top:ISPOP?80:70,bottom:ISPOP?280:20,left:0,right:0}:{top:80,left:150,right:0,bottom:0}});
map.on('style.load',()=>{for(const l of map.getStyle().layers){if(l.type=='symbol'&&/poi|housenumber|transit/.test(l.id))map.setLayoutProperty(l.id,'visibility','none')}});
map.on('load',()=>{
 if(NEWST){const H0=map.getContainer().clientHeight;if(MOB){const ch=document.querySelector('.fcwrap').offsetHeight;map.jumpTo({center:FD.ll,padding:{top:0,bottom:Math.max(0,ch+24-(innerHeight-map.getContainer().getBoundingClientRect().bottom))+(STATE=='clus'?10:STATE=='empeur'?30:30),left:0,right:0}})}else{map.jumpTo({center:FD.ll,padding:{top:Math.max(0,2*(H0-(STATE=='clus'?44:60))-H0),left:150,right:0,bottom:0}})}}
 const fc={type:'FeatureCollection',features:RINGS.map(([R,s],i)=>({type:'Feature',properties:{i},geometry:{type:'Polygon',coordinates:[ring(R,s)]}}))};
 map.addSource('iso',{type:'geojson',data:fc});
 map.addLayer({id:'isof',type:'fill',source:'iso',paint:{'fill-color':'#1F2F55','fill-opacity':['match',['get','i'],0,.10,1,.06,.035]}});
 [[0,null,3],[1,[3,1.6],2.6],[2,[.6,1.4],3]].forEach(([i,d,w])=>{const p={'line-color':'#1F2F55','line-width':w,'line-opacity':.9};if(d)p['line-dasharray']=d;map.addLayer({id:'isol'+i,type:'line',source:'iso',filter:['==',['get','i'],i],layout:{'line-cap':d&&i==2?'round':'butt','line-join':'round'},paint:p})});
 const add=(el,ll,anchor='center')=>new maplibregl.Marker({element:el,anchor}).setLngLat(ll).addTo(map);
 const mk=(cls,html)=>{const e=document.createElement('div');e.className='mk '+cls;e.innerHTML=html;return e};
 // ringlabels: kies een zichtbaar punt
 const W=map.getContainer().clientWidth,H=map.getContainer().clientHeight;
 const free=(x,y)=>x>40&&x<W-80&&y>(MOB?110:110)&&y<H-(MOB?80:40)&&!( !MOB&&x<330&&y>H-250)&&!(MOB&&x<170&&y>H-90);
 RINGS.forEach(([R,s],i)=>{for(const a of [-Math.PI/2+.5,-Math.PI/2-.4,-.5,.5,Math.PI-.4,-Math.PI/2+1.1,-Math.PI/2,Math.PI/2-.6,-Math.PI/2+.25,-2.3,-0.9,2.4,-2.6]){const ll=ringPt(R,s,a);const p=map.project(ll);if(free(p.x,p.y)){add(mk('rlw',`<span class="rl">${BIKE}${[10,20,30][i]} min</span>`),ll);break}}});
 add(mk('home','<span class="hd"><svg class="ic" viewBox="0 0 24 24"><path d="M4 11l8-6.5 8 6.5M6.5 9.5V19h11V9.5M10 19v-5h4v5"/></svg></span><span class="hl">Jouw adres</span>'),C,'top').setOffset([0,-20]);
 let CL=[[-1.5,2.2,15],[3.0,1.9,10],[0.4,-2.6,7],[-3.0,-0.9,6],[1.6,3.3,8]];
 let EMP=[[1.2,0.9],[-0.9,-1.5],[-4.2,2.0],[3.6,-1.9]];
 let INT=[[2.5,-0.7],[-0.6,1.2],[-3.1,3.0],[4.2,0.6]];
 if(MOB){EMP=[[1.2,0.9],[-1.7,-3.3],[3.6,-1.9],[-4.2,2.0]];INT=[[2.5,-0.7],[-3.1,3.0],[1.9,-3.0]];}
 CL.forEach(([x,y,n],j)=>add(mk('',`<span class="cl${n>12?' big':''}${j==0&&STATE=='clus'?' sel':''}">${n}</span>`),K(x,y)));
 EMP.forEach(([x,y],j)=>{const sel=j==0&&FK=='emp';add(mk('pinw',pinW(sel?1.2:1,sel)),K(x,y),'bottom')});
 INT.forEach(([x,y],j)=>{const sel=j==0&&(STATE=='popup'||STATE=='int');add(mk('pini',pinI(sel?1.2:1,sel)),K(x,y),'bottom')});
 const mw=map.getContainer();
 // hint bij eerste bezoek
 if(STATE!='legend'||true){const p=map.project(K(...CL[0].slice(0,2)));const h=document.createElement('div');h.className='hint';h.innerHTML=`<svg class="ic" viewBox="0 0 24 24"><path d="M9 11V5.5a1.5 1.5 0 0 1 3 0V11M12 10V8.5a1.5 1.5 0 0 1 3 0V11M15 10.5a1.5 1.5 0 0 1 3 0V15a6 6 0 0 1-6 6h-1a6 6 0 0 1-5-2.7L4 15a1.6 1.6 0 0 1 2.5-2L9 15"/></svg>${MOB?'Tik':'Klik'} om in te zoomen`;if(MOB){h.classList.add('below');h.style.left=(p.x-34)+'px';h.style.top=(p.y+38)+'px'}else{h.style.left=(p.x+40)+'px';h.style.top=(p.y-21)+'px'}mw.parentNode.appendChild(h);if(ISPOP||NEWST)h.remove()}
 if(STATE=='popup'&&!MOB){const p=map.project(POP.ll);const e=document.createElement('div');e.className='pop';e.innerHTML=popHTML;mw.parentNode.appendChild(e);const ph=e.offsetHeight;let left=p.x-150;left=Math.min(Math.max(left,12),W-312);e.style.left=left+'px';e.style.setProperty('--ax',(p.x-left)+'px');e.style.top=(p.y-62-ph-12)+'px'}
 if(NEWST&&!MOB){const p=map.project(FD.ll);const e=document.createElement('div');e.className='pop fpop';e.innerHTML=fcard(FD,STATE=='clus',EUR);mw.parentNode.appendChild(e);const ph=e.offsetHeight;const cw=e.offsetWidth;let left=p.x-cw/2;left=Math.min(Math.max(left,12),W-cw-12);e.style.left=left+'px';e.style.setProperty('--ax',(p.x-left)+'px');e.style.top=(p.y-(STATE=='clus'?44:72)-ph-12)+'px'}
 if(MOB&&STATE=='popup')map.jumpTo({center:POP.ll,padding:{top:0,bottom:70,left:0,right:0}});
 map.once('idle',()=>document.body.dataset.ready=1);
});
