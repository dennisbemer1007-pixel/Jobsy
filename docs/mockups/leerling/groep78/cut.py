from PIL import Image, ImageFilter
import numpy as np
def cut(src,dst,box=None,size=None):
    im=Image.open('illustraties/'+src).convert('RGB')
    if box: im=im.crop(box)
    a=np.asarray(im,float)
    bg=np.median(np.concatenate([a[:8].reshape(-1,3),a[-8:].reshape(-1,3),a[:,:8].reshape(-1,3),a[:,-8:].reshape(-1,3)]),axis=0)
    d=np.sqrt(((a-bg)**2).sum(2))
    al=np.clip((d-10)/30,0,1)
    m=Image.fromarray((al*255).astype('uint8')).filter(ImageFilter.GaussianBlur(0.8))
    out=im.convert('RGBA'); out.putalpha(m)
    bb=out.getbbox(); out=out.crop((max(bb[0]-6,0),max(bb[1]-6,0),min(bb[2]+6,out.width),min(bb[3]+6,out.height)))
    if size: out.thumbnail((size,size),Image.LANCZOS)
    out.save('src/img/'+dst); print(dst,out.size)
for n in []: cut(f'kreeft-{n}.jpg',f'kreeft-{n}.png',size=700)
#cut('kreeft-portret.jpg','kreeft-portret.png',size=400)
#cut('embleem.jpg','embleem.png',size=160)
for i,(x0,x1) in enumerate([(19,338),(356,656),(669,912),(931,1256)]):
    cut('schelpen.jpg',f'schelp-{i+1}.png',box=(x0,162,x1,525),size=220)
