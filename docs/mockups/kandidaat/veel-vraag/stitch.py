# Stitch meerdere 16:9 panelen tot een doorlopende headerstrip 2880x300 (+ mobiel 1170x270)
from PIL import Image, ImageFilter
import numpy as np, sys
BG=(250,246,239)
def build(name, panels, ov, mobx):
    n=len(panels); W=2880; H=300
    wf=(W+(n-1)*ov)//n + 1
    canvas=np.zeros((H,W,3),float); wsum=np.zeros((H,W,1),float)
    x=0
    for i,(f,x0,x1,y0) in enumerate(panels):
        im=Image.open('illustraties/'+f).convert('RGB')
        # preview coords (640 breed) -> bron (1280)
        X0,X1,Y0=x0*2,x1*2,y0*2
        bh=round((X1-X0)*H/wf)
        band=im.crop((X0,Y0,X1,Y0+bh)).resize((wf,H),Image.LANCZOS)
        a=np.asarray(band,float)
        m=np.ones(wf)
        if i>0: m[:ov]=np.linspace(0,1,ov)
        if i<n-1: m[-ov:]=np.minimum(m[-ov:],np.linspace(1,0,ov))
        xs=min(x,W-wf); xe=xs+wf
        canvas[:,xs:xe]+=a*m[None,:,None]; wsum[:,xs:xe]+=m[None,:,None]
        x+=wf-ov
    out=canvas/np.maximum(wsum,1e-6)
    # zachte onderrand naar creme + heel smalle zijrand
    bg=np.array(BG,float)
    yy=np.linspace(0,1,H)[:,None,None]
    fb=np.clip((yy-0.80)/0.20,0,1)**1.3
    xx=np.arange(W)[None,:,None]
    fs=np.clip(1-np.minimum(xx,W-1-xx)/24,0,1)
    f=np.maximum(fb,fs)
    out=out*(1-f)+bg*f
    img=Image.fromarray(out.clip(0,255).astype('uint8'))
    img.save(f'src/img/hero-{name}-desk.jpg',quality=90)
    mob=img.crop((mobx,0,mobx+1300,300)).resize((1170,270),Image.LANCZOS)
    mob.save(f'src/img/hero-{name}-mob.jpg',quality=90)
    img.save(f'illustraties/hero-{name}-desk.jpg',quality=90); mob.save(f'illustraties/hero-{name}-mob.jpg',quality=90)
build('carriere',[('car-p1.jpg',8,632,140),('car-p3.jpg',8,632,145),('car-p2.jpg',8,632,130)],120,int(sys.argv[1]) if len(sys.argv)>1 else 0)
build('omscholen',[('om-p1.jpg',12,628,42),('om-p2.jpg',12,628,47),('om-p3.jpg',12,628,40),('om-p4.jpg',12,628,50)],40,int(sys.argv[2]) if len(sys.argv)>2 else 0)
