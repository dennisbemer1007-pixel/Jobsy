import {chromium} from 'playwright-core';
const b=await chromium.launch({executablePath:'/opt/google/chrome/chrome'});
const base='file:///workspace/banenkaart-lagen/src/index.html',out='/workspace/banenkaart-lagen/';
async function go(q,w,h,name,mob){const p=await b.newPage({viewport:{width:w,height:h},deviceScaleFactor:2,hasTouch:!!mob,isMobile:!!mob});
p.on('pageerror',e=>console.log('ERR',e.message));
await p.goto(base+'?s='+q);await p.waitForFunction(()=>document.body.dataset.ready,null,{timeout:45000});
await p.waitForFunction(()=>map.areTilesLoaded()&&map.loaded(),null,{timeout:30000});await p.waitForTimeout(1500);
await p.screenshot({path:out+name});console.log(name);await p.close();}
const only=process.argv[2]||'';
const J=[['popup',1440,900,'desktop/01-banenkaart-lagen-desktop-1440-popup.png'],['none',1440,900,'desktop/02-banenkaart-lagen-desktop-1440.png'],['none',390,844,'mobile/01-banenkaart-lagen-mobiel-390.png',1],['legend',390,844,'mobile/02-banenkaart-lagen-mobiel-390-uitleg.png',1],['popup',390,844,'mobile/03-banenkaart-lagen-mobiel-390-vacature.png',1]];
for(const j of J) if(!only||j[3].includes(only)) await go(...j);
await b.close();
