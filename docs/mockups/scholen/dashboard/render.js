const {chromium}=require('playwright-core');const fs=require('fs');const path=require('path');
(async()=>{const b=await chromium.launch({executablePath:'/opt/google/chrome/chrome'});
const only=process.argv[2];
const files=fs.readdirSync('src').filter(f=>f.endsWith('.html')&&(!only||f.includes(only))).sort();
for(const [vp,dir,w,dpr] of [['d','desktop',1440,1],['m','mobile',390,2]]){
 const ctx=await b.newContext({viewport:{width:w,height:900},deviceScaleFactor:dpr});const p=await ctx.newPage();
 for(const f of files){await p.goto('file://'+path.resolve('src',f));await p.waitForLoadState('networkidle');await p.evaluate(()=>document.fonts.ready);
  const ov=await p.evaluate(()=>{const W=document.documentElement.clientWidth;const bad=[];document.querySelectorAll('body *').forEach(e=>{const r=e.getBoundingClientRect();if(r.right>W+1&&r.width>0)bad.push(e.tagName+'.'+e.className+' '+Math.round(r.right))});return bad.slice(0,5)});
  if(ov.length)console.log('OVERFLOW',dir,f,ov.join(' | '));
  await p.screenshot({path:`renders/${dir}/${f.replace('.html','')}-${dir}-${w}.png`,fullPage:true});}
 await ctx.close();}
await b.close();console.log('done')})();
