// Rendert alle schermen uit src/screens.json: desktop 1440 (scale 1) en mobiel 390 (scale 2), full page.
const {chromium}=await import(process.env.PW||'/workspace/bedrijvenkaart-r21/src/node_modules/playwright-core/index.mjs');
import fs from 'fs';
const S=JSON.parse(fs.readFileSync('src/screens.json','utf8'));
const only=process.argv[2]||'';
const b=await chromium.launch({executablePath:'/opt/google/chrome/chrome'});
fs.mkdirSync('renders/desktop',{recursive:true});fs.mkdirSync('renders/mobile',{recursive:true});
const report=[];
async function shot(s,mob){
  const p=await b.newPage(mob?{viewport:{width:390,height:844},deviceScaleFactor:2,isMobile:true,hasTouch:true}:{viewport:{width:1440,height:900},deviceScaleFactor:1});
  p.on('pageerror',e=>console.log('ERR',s.num,e.message));
  await p.goto('file://'+process.cwd()+'/src/'+s.file,{waitUntil:'load'});
  try{await p.waitForLoadState('networkidle',{timeout:15000})}catch(e){}
  if(s.wait){try{await p.waitForFunction(()=>typeof map!=='undefined'&&map.loaded()&&map.areTilesLoaded(),null,{timeout:25000})}catch(e){console.log('map wait timeout',s.num)}; await p.waitForTimeout(2500)}
  // fixed knop/tabbar onderaan de pagina zetten zodat ze in een full-page render niet midden in de inhoud zweven
  await p.addStyleTag({content:'body{position:relative;min-height:100vh}.kask{position:absolute!important}'});
  await p.evaluate(()=>document.fonts.ready); await p.waitForTimeout(400);
  const ov=await p.evaluate(()=>document.documentElement.scrollWidth-window.innerWidth);
  const nn=String(s.num).padStart(2,'0');
  const out=`renders/${mob?'mobile':'desktop'}/${nn}-${s.slug}-${mob?'mobiel-390':'desktop-1440'}.png`;
  await p.screenshot({path:out,fullPage:s.slug!=='lobsy-hulp'});
  report.push({num:s.num,mob,ov,out});if(ov>0)console.log('OVERFLOW',out,ov);
  await p.close();return out;
}
for(const s of S){ if(only&&!only.split(',').includes(String(s.num)))continue; await shot(s,false); await shot(s,true); console.log('ok',s.num,s.slug);}
fs.writeFileSync('renders/report.json',JSON.stringify(report,null,1));
await b.close();
