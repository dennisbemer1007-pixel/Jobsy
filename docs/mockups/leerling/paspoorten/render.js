const {chromium}=require('playwright-core');const fs=require('fs');const {execSync}=require('child_process');
(async()=>{const b=await chromium.launch({executablePath:'/opt/google/chrome/chrome'});
const only=process.argv[2];
const files=fs.readdirSync('src').filter(f=>f.endsWith('.html')&&(!only||f.startsWith(only)));
const p=await b.newPage();
for(const f of files){await p.goto('file://'+process.cwd()+'/src/'+f);await p.waitForLoadState('networkidle');await p.evaluate(()=>document.fonts.ready);
 await p.emulateMedia({media:'print'});
 const rep=await p.evaluate(()=>[...document.querySelectorAll('.page')].map((pg,i)=>{const pr=pg.getBoundingClientRect();const ft=pg.querySelector('.foot').getBoundingClientRect();
   let maxB=0,over=[];pg.querySelectorAll('.wrap *').forEach(e=>{const r=e.getBoundingClientRect();if(r.bottom>maxB)maxB=r.bottom;if(r.right>pr.right-1)over.push(e.className)});
   return {page:i+1,free_mm:((ft.top-maxB)*25.4/96).toFixed(1),overR:[...new Set(over)].slice(0,5)}}));
 console.log(f,JSON.stringify(rep));
 const out='pdf/'+f.replace('.html','.pdf');await p.pdf({path:out,format:'A4',printBackground:true,preferCSSPageSize:true,margin:{top:0,right:0,bottom:0,left:0}});
 execSync(`rm -f png/${f.replace('.html','')}-*.png; pdftoppm -r 110 -png ${out} png/${f.replace('.html','')}`);
}
await b.close();})();
