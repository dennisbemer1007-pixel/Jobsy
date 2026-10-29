# Droombanen voor Rotsen · Droom: de kreeft kijkt uit over zee en droomt; zachte aquarelwolkjes
# stijgen op en tonen dezelfde kreeft in verschillende banen (piloot, schilder, dokter, bakker, tuinier, bouwvakker).
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
D='_bron/v4/droom/'
JOBS=['piloot','schilder','dokter','bakker','tuinier','bouwvakker']
def _cloud_mask(W,H,cx,cy,w,h,seed):
    rng=np.random.default_rng(seed); m=Image.new('L',(W,H),0); d=ImageDraw.Draw(m)
    d.ellipse((cx-w*.42,cy-h*.30,cx+w*.42,cy+h*.42),fill=255)
    for k in range(7):
        a=k/7*2*np.pi; rx=w*(.20+.06*rng.random()); ry=h*(.20+.05*rng.random())
        px=cx+np.cos(a)*w*.32; py=cy+np.sin(a)*h*.26-h*.04
        d.ellipse((px-rx,py-ry,px+rx,py+ry),fill=255)
    return m
def _paper_noise(W,H,seed):
    rng=np.random.default_rng(seed); n=rng.random((H//6+1,W//6+1))
    n=Image.fromarray((n*255).astype('uint8')).resize((W,H),Image.BICUBIC).filter(ImageFilter.GaussianBlur(3))
    return np.asarray(n).astype(float)/255
def cloud(sc,cx,cy,w,h,seed):
    W,H=sc.size; m=_cloud_mask(W,H,cx,cy,w,h,seed).filter(ImageFilter.GaussianBlur(2.2))
    a=np.asarray(m).astype(float)/255; nz=_paper_noise(W,H,seed)
    body=np.clip(a*(0.80+0.18*nz),0,1)
    edge=np.clip(a-np.asarray(m.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.GaussianBlur(1.5))).astype(float)/255,0,1)
    s=np.asarray(sc).astype(float)
    fill=np.array([253,249,240.]); line=np.array([214,186,160.])
    s=s*(1-body[...,None])+fill*body[...,None]
    s=s*(1-0.55*edge[...,None])+line*(0.55*edge[...,None])
    return Image.fromarray(np.clip(s,0,255).astype('uint8'))
def bubble(sc,x,y,r,seed):
    return cloud(sc,x,y,r*2.4,r*2.2,seed)
def vignette(sc,job,cx,bottom,h):
    f=Image.open(D+job+'.png').convert('RGBA')
    from scipy import ndimage
    A=np.asarray(f).copy(); lab,n=ndimage.label(A[...,3]>30)
    if n>1:
        sz=ndimage.sum(np.ones_like(lab),lab,range(1,n+1)); keep=[i+1 for i,v in enumerate(sz) if v>=0.03*sz.max()]
        A[...,3]=np.where(np.isin(lab,keep)|(A[...,3]<=30)&(ndimage.binary_dilation(np.isin(lab,keep),iterations=3)),A[...,3],0)
        f=Image.fromarray(A,'RGBA'); f=f.crop(f.getchannel('A').getbbox())
    r=h/f.height
    f=f.resize((max(1,round(f.width*r)),round(h)),Image.LANCZOS)
    arr=np.asarray(f).astype(float)
    # zachte rand en iets van de wolkkleur erdoor: het blijft een droom
    al=Image.fromarray(arr[...,3].astype('uint8')).filter(ImageFilter.GaussianBlur(0.7))
    arr[...,3]=np.asarray(al).astype(float)*0.93
    arr[...,:3]=arr[...,:3]*0.88+np.array([253,249,240.])*0.12
    f=Image.fromarray(np.clip(arr,0,255).astype('uint8'),'RGBA')
    s=sc.convert('RGBA'); s.alpha_composite(f,(int(cx-f.width/2),int(bottom-f.height))); return s.convert('RGB')
def dreams(sc, head=(500,418)):
    hx,hy=head
    for i,(dx,dy,r) in enumerate([(26,-10,4),(46,-22,6),(72,-34,8.5)]):
        sc=bubble(sc,hx+dx,hy+dy,r,10+i)
    # zes wolken in een rustige golf boven zee, van links (dichtbij) naar rechts (ver)
    xs=[640,752,866,980,1094,1206]; ys=[402,396,404,394,402,396]
    for i,(job,x,y) in enumerate(zip(JOBS,xs,ys)):
        w=124 if job in('piloot','schilder') else 106
        sc=cloud(sc,x,y,w,104,30+i)
        sc=vignette(sc,job,x+2,y+40,70 if job!='piloot' else 58)
    return sc
