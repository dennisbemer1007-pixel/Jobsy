const {chromium}=require('playwright-core');
(async()=>{const b=await chromium.launch({executablePath:'/opt/google/chrome/chrome'});const p=await b.newPage({viewport:{width:390,height:900}});
await p.goto('file://'+process.cwd()+'/src/12-puzzel-vuurtorenlampen.html');
console.log(await p.evaluate(()=>[...document.querySelectorAll('*')].filter(e=>e.getBoundingClientRect().right>391).slice(0,8).map(e=>e.tagName+'.'+e.className+' '+Math.round(e.getBoundingClientRect().right))));await b.close()})();
