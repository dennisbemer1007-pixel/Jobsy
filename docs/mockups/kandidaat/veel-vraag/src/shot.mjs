import {chromium} from '/workspace/bedrijvenkaart-r21/src/node_modules/playwright-core/index.mjs';
const b=await chromium.launch({executablePath:'/opt/google/chrome/chrome'});
const dir='/workspace/veel-vraag-mockup/src/',out='/workspace/veel-vraag-mockup/';
const J=[['01-matchkaart','01-matchkaart-label'],['04-carriere','04-carriere-veel-vraag'],['05-omscholen','05-omscholen']];
for(const [f,n] of J)for(const [k,w,h] of [['desktop',1440,900],['mobile',390,844]]){
 const p=await b.newPage({viewport:{width:w,height:h},deviceScaleFactor:2});const bad=[];p.on('requestfailed',r=>bad.push(r.url()));
 await p.goto('file://'+dir+f+'.html');await p.evaluate(()=>document.fonts.ready);await p.waitForTimeout(400);
 const br=await p.evaluate(()=>[...document.images].filter(i=>i.offsetParent&&(!i.complete||!i.naturalWidth)).map(i=>i.src));
 const sw=await p.evaluate(()=>document.documentElement.scrollWidth);
 await p.screenshot({path:`${out}${k}/${n}-${k=='desktop'?'desktop-1440':'mobiel-390'}.png`,fullPage:true});console.log(n,k,'broken:',br.join(','),bad.join(','),'sw',sw);await p.close();}
{const p=await b.newPage({viewport:{width:1440,height:900},deviceScaleFactor:2});await p.goto('file://'+dir+'06-uwv-pdf.html');await p.evaluate(()=>document.fonts.ready);await p.waitForTimeout(400);
const el=await p.$('.sheet');const bb=await el.boundingBox();const t=await p.$('h2.t');const tb=await t.boundingBox();const n=await p.$('h2.t:nth-of-type(2)');
const fn=await p.evaluate(()=>{const ps=[...document.querySelectorAll('.sheet p.small')];const e=ps[ps.length-1];const r=e.getBoundingClientRect();return r.bottom+window.scrollY});
await p.screenshot({path:out+'desktop/06-uwv-pdf-uitsnede-desktop-1440.png',fullPage:true,clip:{x:bb.x,y:tb.y-16,width:bb.width,height:fn-tb.y+32}});console.log('06 ok');await p.close();}
await b.close();
