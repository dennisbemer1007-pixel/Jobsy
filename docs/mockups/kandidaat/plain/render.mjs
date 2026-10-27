const {chromium}=await import('/workspace/bedrijvenkaart-r21/src/node_modules/playwright-core/index.mjs');
import fs from 'fs';
const S=JSON.parse(fs.readFileSync('src/screens.json','utf8'));
const b=await chromium.launch({executablePath:'/opt/google/chrome/chrome'});
for(const d of ['desktop','mobiel'])fs.mkdirSync('renders/'+d,{recursive:true});
for(const s of S)for(const mob of [false,true]){ if(s.mobileOnly&&!mob)continue;
 const p=await b.newPage(mob?{viewport:{width:390,height:844},deviceScaleFactor:2,isMobile:true}:{viewport:{width:1440,height:900}});
 await p.goto('file://'+process.cwd()+'/src/'+s.file);await p.evaluate(()=>document.fonts.ready);
 const ov=await p.evaluate(()=>{let bad=[];document.querySelectorAll('*').forEach(e=>{if(e.scrollWidth>e.clientWidth+1&&getComputedStyle(e).overflow!=='visible')bad.push(e.className)});return [document.documentElement.scrollWidth-innerWidth,bad.slice(0,3)]});
 if(ov[0]>0||ov[1].length)console.log('OV',s.file,mob,ov);
 const out=`renders/${mob?'mobiel':'desktop'}/${String(s.num).padStart(2,'0')}-${s.slug}-${mob?'mobiel-390':'desktop-1440'}.png`;
 await p.screenshot({path:out,fullPage:true});await p.close();}
await b.close();console.log('done');
