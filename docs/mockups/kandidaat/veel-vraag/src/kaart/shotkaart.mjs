import {chromium} from 'playwright-core';
const b=await chromium.launch({executablePath:'/opt/google/chrome/chrome'});
const base='file:///workspace/veel-vraag-mockup/src/kaart/index.html',out='/workspace/veel-vraag-mockup/';
async function go(q,w,h,name,mob){const p=await b.newPage({viewport:{width:w,height:h},deviceScaleFactor:2,hasTouch:!!mob,isMobile:!!mob});
p.on('pageerror',e=>console.log('ERR',e.message));
await p.goto(base+'?s='+q);await p.waitForFunction(()=>document.body.dataset.ready,null,{timeout:45000});
await p.waitForFunction(()=>map.areTilesLoaded()&&map.loaded(),null,{timeout:30000});await p.waitForTimeout(1500);
const broken=await p.evaluate(()=>[...document.images].filter(i=>!i.complete||!i.naturalWidth).map(i=>i.src));
await p.screenshot({path:out+name});console.log(name,'broken:',broken.join(','));await p.close();}
const J=[['vv',1440,900,'desktop/02-banenkaart-popup-label-desktop-1440.png'],['vv',390,844,'mobile/02-banenkaart-popup-label-mobiel-390.png',1],['clus',1440,900,'desktop/02b-banenkaart-clusterpopup-label-desktop-1440.png'],['filter',1440,900,'desktop/03-banenkaart-filter-desktop-1440.png'],['filter',390,844,'mobile/03-banenkaart-filter-mobiel-390.png',1]];
const only=process.argv[2]||'';
for(const j of J) if(!only||j[3].includes(only)) await go(...j);
await b.close();
