# Strip voor headers met kreeft in beeld: content-band (y0..y1) volledig houden,
# verkleinen tot doelhoogte; is de band dan smaller dan de strip, dan lopen de zijkanten
# als aquarel zacht uit in papierkleur (geen spiegeling, geen dubbele objecten).
from PIL import Image, ImageFilter
import numpy as np, sys
PAPER=np.array([249,241,226],float)
def fit(src, y0, y1, outW, outH):
    im=Image.open(src).convert('RGB'); W,H=im.size
    s=outH/(y1-y0); w=round(W*s)
    band=im.crop((0,y0,W,y1)).resize((w,outH),Image.LANCZOS)
    if w>=outW:
        x=(w-outW)//2; return band.crop((x,0,x+outW,outH))
    b=np.asarray(band).astype(float); x0=(outW-w)//2
    out=np.zeros((outH,outW,3)); out[:]=PAPER
    # aquarel-vignet: band loopt aan beide kanten breed en onregelmatig uit in papier
    pad=(outW-w)//2; fe=int(min(w*0.22, 0.9*pad+40)); m=np.ones((outH,w))
    rng=np.random.default_rng(7)
    wob=np.asarray(Image.fromarray((rng.random((outH//8+1,4))*255).astype('uint8')).resize((4,outH),Image.BICUBIC)).astype(float)/255
    for side in (0,1):
        off=(wob[:,side]*0.5*fe).astype(int)
        for y in range(outH):
            f=fe-off[y]; r=np.clip(np.linspace(-0.15,1,f),0,1)**1.4
            if side==0: m[y,:f]=r
            else: m[y,w-f:]=r[::-1]
    m=np.asarray(Image.fromarray((m*255).astype('uint8')).filter(ImageFilter.GaussianBlur(6))).astype(float)/255
    out[:,x0:x0+w]=b*m[...,None]+PAPER*(1-m[...,None])
    n=rng.normal(0,2.0,(outH,outW,1))
    out=np.clip(out+n,0,255).astype('uint8')
    return Image.fromarray(out)
if __name__=='__main__':
    src,y0,y1,W,H,dst=sys.argv[1],int(sys.argv[2]),int(sys.argv[3]),int(sys.argv[4]),int(sys.argv[5]),sys.argv[6]
    fit(src,y0,y1,W,H).save(dst,quality=82,optimize=True,progressive=True)
