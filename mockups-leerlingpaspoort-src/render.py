import sys,os
from playwright.sync_api import sync_playwright
D='/workspace/mockups-leerlingpaspoort'
names=sys.argv[1:] or ['groep78','middelbaar','hogeschool']
JS='''()=>{const r=[];document.querySelectorAll('.page *').forEach(e=>{if(e.closest('svg'))return;
const cs=getComputedStyle(e);if(cs.display=='inline')return;
if(e.scrollWidth>e.clientWidth+1&&cs.overflow!='visible'||e.scrollHeight>e.clientHeight+1&&cs.overflow=='hidden')r.push('OVER '+e.className+' "'+e.textContent.trim().slice(0,30)+'"');
const b=e.getBoundingClientRect(),p=e.closest('.page').getBoundingClientRect();if(b.width&&(b.right>p.right+.5||b.bottom>p.bottom+.5))r.push('OUT '+e.className+' '+Math.round(b.bottom-p.top));});
document.querySelectorAll('.page').forEach((p,i)=>{const pb=p.getBoundingClientRect();let m=0;p.querySelectorAll(':scope > *:not(.foot):not(.legend):not(svg)').forEach(c=>{m=Math.max(m,c.getBoundingClientRect().bottom-pb.top)});const f=p.querySelector('.foot');r.push('page'+i+' content bottom '+Math.round(m)+' foot top '+(f?Math.round(f.getBoundingClientRect().top-pb.top):'-'))});return r}'''
with sync_playwright() as p:
    b=p.chromium.launch(executable_path='/usr/bin/google-chrome',args=['--no-sandbox'])
    for n in names:
        pg=b.new_page(viewport={'width':794,'height':1123},device_scale_factor=2)
        pg.goto(f'file://{D}/{n}.html');pg.wait_for_timeout(300)
        pages=pg.query_selector_all('.page')
        print(n,pg.evaluate(JS))
        for i,el in enumerate(pages):
            el.screenshot(path=f'{D}/{n}{"" if i==0 else "-p"+str(i+1)}.png')
        pg.pdf(path=f'{D}/{n}.pdf',width='210mm',height='297mm',print_background=True,prefer_css_page_size=True)
        pg.close()
    b.close()
