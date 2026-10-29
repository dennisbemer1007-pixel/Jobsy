from PIL import Image
import numpy as np
S='illustraties'; O='src/img'; BG=np.array((246,244,240),float); W,H,OV=2880,300,200
def strip(name,panels,mobc,ov=200):
    OV=ov
    ims=[]
    for f,x0,x1,y0,y1 in panels:
        k=1.25; im=Image.open(f'{S}/{f}.jpg').convert('RGB').crop((round(x0*k),round(y0*k),round(x1*k),round(y1*k)))
        w=round(im.width*H/im.height); ims.append(np.asarray(im.resize((w,H),Image.LANCZOS),float))
    tot=sum(a.shape[1] for a in ims)-OV*(len(ims)-1)
    can=np.zeros((H,tot,3)); ws=np.zeros((H,tot,1)); x=0
    for i,a in enumerate(ims):
        w=a.shape[1]; m=np.ones(w)
        if i>0: m[:OV]=np.linspace(0,1,OV)
        if i<len(ims)-1: m[-OV:]=np.linspace(1,0,OV)
        can[:,x:x+w]+=a*m[None,:,None]; ws[:,x:x+w]+=m[None,:,None]; x+=w-OV
    img=Image.fromarray((can/np.maximum(ws,1e-6)).clip(0,255).astype('uint8'))
    if tot<W:
        s=W/tot; img=img.resize((W,round(H*s)),Image.LANCZOS); t=(img.height-H)//2; img=img.crop((0,t,W,t+H))
    else:
        l=(tot-W)//2; img=img.crop((l,0,l+W,H))
    a=np.asarray(img,float); yy=np.linspace(0,1,H)[:,None,None]; fb=np.clip((yy-0.86)/0.14,0,1)**1.3
    a=a*(1-fb)+BG*fb; img=Image.fromarray(a.clip(0,255).astype('uint8')); img.save(f'{O}/hdr-{name}-desk.jpg',quality=86)
    mw=round(1170*H/270); mx=int(max(0,min(W-mw,mobc*W-mw/2)))
    img.crop((mx,0,mx+mw,H)).resize((1170,270),Image.LANCZOS).save(f'{O}/hdr-{name}-mob.jpg',quality=86)
strip('school',[('school',0,1024,196,392),('school2',0,1024,196,392)],0.22)
strip('boten',[('boten',0,1024,250,376),('boten2',640,1024,262,392)],0.4)
strip('beroepen',[('beroepen',0,1024,200,385),('beroepen2',140,1024,200,405)],0.3,ov=80)
strip('huisjes',[('huisjes',0,1024,225,345),('huisjes2',0,600,225,345)],0.5)
strip('ouderavond',[('ouderavond',0,1024,170,420),('ouderavond2',0,1024,226,362)],0.3)
strip('klas',[('klas',0,1024,235,410),('klas2',0,1024,225,395)],0.4)
strip('pieren',[('pieren',0,1024,140,345),('pieren2',0,600,265,378)],0.4)
strip('leerling',[('leerling',0,1024,255,362)],0.58)
strip('les',[('les',0,1024,282,398)],0.47)
strip('veer',[('veer',0,1024,210,365),('pieren2',0,600,265,378)],0.35)
