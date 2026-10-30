const {chromium}=require('playwright-core');const fs=require('fs');
(async()=>{const b=await chromium.launch({executablePath:'/opt/google/chrome/chrome'});
const only=process.argv[2];
const files=fs.readdirSync('src').filter(f=>f.endsWith('.html')&&(!only||f.startsWith(only)));
for(const [dir,w,s] of [['desktop',1440,1],['mobile',390,2]]){const p=await b.newPage({viewport:{width:w,height:900},deviceScaleFactor:s});
for(const f of files){await p.goto('file://'+process.cwd()+'/src/'+f);await p.waitForLoadState('networkidle');await p.evaluate(()=>document.fonts.ready);
const ov=await p.evaluate(()=>document.documentElement.scrollWidth);if(ov>window_w(w))console.log('OVERFLOW',dir,f,ov);
await p.screenshot({path:`renders/${dir}/${f.replace('.html','.png')}`,fullPage:true});}await p.close();}
await b.close();})();
function window_w(w){return w}
