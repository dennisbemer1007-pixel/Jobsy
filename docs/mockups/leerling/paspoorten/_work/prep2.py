import numpy as np
from PIL import Image, ImageOps
O='src/img/'
def fade(img,bg,start=0.9):
    a=np.asarray(img,float);H=a.shape[0];yy=np.linspace(0,1,H)[:,None,None]
    fb=np.clip((yy-start)/(1-start),0,1)**1.3;a=a*(1-fb)+np.array(bg,float)*fb
    return Image.fromarray(a.clip(0,255).astype('uint8'))
def stitch(src_dir,panels,W,H,ov=260):
    n=len(panels); wf=min(W,(W+(n-1)*ov)//n+1)
    canvas=np.zeros((H,W,3)); wsum=np.zeros((H,W,1)); x=0
    for i,p in enumerate(panels):
        f,x0,x1,yc=p[:4]; flip=len(p)>4 and p[4]
        im=Image.open(f'{src_dir}/{f}').convert('RGB')
        if flip: im=ImageOps.mirror(im)
        s=im.width/1024; X0,X1=x0*s,x1*s; bh=(X1-X0)*H/wf; Y0=max(0,min(im.height-bh,yc*s-bh/2))
        band=im.crop((round(X0),round(Y0),round(X1),round(Y0+bh))).resize((wf,H),Image.LANCZOS)
        a=np.asarray(band,float); m=np.ones(wf)
        if i>0: m[:ov]=np.linspace(0,1,ov)
        if i<n-1: m[-ov:]=np.minimum(m[-ov:],np.linspace(1,0,ov))
        xs=min(x,W-wf); canvas[:,xs:xs+wf]+=a*m[None,:,None]; wsum[:,xs:xs+wf]+=m[None,:,None]; x+=wf-ov
    return Image.fromarray((canvas/np.maximum(wsum,1e-6)).clip(0,255).astype('uint8'))
# G78: new wide-composition image, thin band around horizon (ratio ~6.3)
im=Image.open('_work/g78-pano-raw.jpg').convert('RGB').crop((0,236,1280,440)).resize((2200,round(204*2200/1280)),Image.LANCZOS)
fade(im,(250,246,239),0.93).save(O+'g78-strip.jpg',quality=84,optimize=True,progressive=True); print('g78',im.size)
# VO: lagune panorama, taller band (ratio 7.5)
V='/workspace/vo-mockup/illustraties'
im=stitch(V,[('lagune-p1.jpg',0,1024,345),('lagune-p2.jpg',0,1024,330)],2880,330,ov=320)
fade(im,(246,243,236),0.88).resize((2200,252),Image.LANCZOS).save(O+'vo-strip.jpg',quality=84,optimize=True,progressive=True); print('vo')
H='/workspace/ho-mockup/illustraties'
im=stitch(H,[('horizon2.jpg',0,1024,338)],2880,254)
fade(im,(244,242,238),0.88).resize((2200,194),Image.LANCZOS).save(O+'ho-strip.jpg',quality=84,optimize=True,progressive=True); print('ho')
