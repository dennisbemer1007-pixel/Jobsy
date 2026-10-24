import { chromium } from 'playwright-core';
const dir='/workspace/na-de-match-r2/sollicitaties/src/';
const jobs=[['sollicitaties','01-sollicitaties-lijst'],['sollicitaties-detail','02-sollicitaties-detail-open'],['sollicitaties-berichten','03-sollicitaties-tab-berichten']];
const b=await chromium.launch({executablePath:'/opt/google/chrome/chrome'});
for(const [f,n] of jobs){for(const [k,w,h,l] of [['desktop',1440,900,'desktop-1440'],['mobile',390,844,'mobiel-390']]){
const p=await b.newPage({viewport:{width:w,height:h},deviceScaleFactor:2});
await p.goto('file://'+dir+f+'.html');await p.waitForLoadState('networkidle');await p.evaluate(()=>document.fonts.ready);
const broken=await p.evaluate(()=>[...document.images].filter(i=>!i.naturalWidth).map(i=>i.src));
const sw=await p.evaluate(()=>document.documentElement.scrollWidth);
await p.screenshot({path:`${k}/${n}-${l}.png`,fullPage:true});
console.log(n,k,'broken:',broken.join(','),'sw',sw);await p.close();}}
await b.close();
