// node _bron/pdf.mjs in.html out.pdf landscape|portrait
const {chromium}=await import(process.env.PW||'/workspace/bedrijvenkaart-r21/src/node_modules/playwright-core/index.mjs');
const [,,inp,out,orient]=process.argv;
const b=await chromium.launch({executablePath:'/opt/google/chrome/chrome'});
const p=await b.newPage();await p.goto('file://'+inp,{waitUntil:'load'});await p.evaluate(()=>document.fonts.ready);
await p.pdf({path:out,format:'A4',landscape:orient=='landscape',printBackground:true,preferCSSPageSize:true});
await b.close();console.log(out);
