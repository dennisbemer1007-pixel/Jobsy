from PIL import Image, ImageDraw
import numpy as np
S=1.25
def crop(src,x,y,r,dst):
    im=Image.open('illustraties/'+src).convert('RGB')
    x,y,r=x*S,y*S,r*S
    im.crop((int(x-r),int(y-r),int(x+r),int(y+r))).resize((140,140),Image.LANCZOS).save('src/img/dot-'+dst+'.jpg',quality=88)
C=[('tuin-baai.jpg',110,450,90,'tuin-1'),('tuin-koraal.jpg',860,430,100,'tuin-2'),('tuin-kaart.jpg',565,275,50,'tuin-3'),('tuin-kaart.jpg',695,180,55,'tuin-4'),('tuin-kaart.jpg',860,115,55,'tuin-5'),('tuin-kaart.jpg',800,345,60,'tuin-6'),
('sterren-duin.jpg',110,420,90,'ster-1'),('sterren-kaart.jpg',400,460,55,'ster-2'),('sterren-strand.jpg',120,285,70,'ster-3'),('sterren-kaart.jpg',622,440,50,'ster-4'),('sterren-strand.jpg',690,330,90,'ster-5'),('sterren-kaart.jpg',849,111,45,'ster-6')]
for c in C: crop(*c)
# composite header strip for choice screen
ims=[Image.open('src/img/'+f).convert('RGB').resize((1280,720)) for f in ['oceaan.jpg','tuin-baai.jpg','sterren-duin.jpg']]
W,H=3000,600
out=Image.new('RGB',(W,H))
parts=[]
for i,im in enumerate(ims):
    im=im.crop((0,60,1280,60+600)) if i!=1 else im.crop((0,40,1280,640))
    parts.append(im.resize((1280,600)))
# place at 0, 860, 1720 with feathered overlaps
xs=[0,860,1720]
acc=np.zeros((H,W,3));wt=np.zeros((H,W,1))
for x,im in zip(xs,parts):
    a=np.asarray(im,float); w=np.ones((1280,));f=420
    w[:f]=np.linspace(0.001,1,f) if x>0 else 1
    w[-f:]=np.minimum(w[-f:],np.linspace(1,0.001,f)) if x+1280<W else w[-f:]
    acc[:,x:x+1280]+=a*w[None,:,None]; wt[:,x:x+1280]+=w[None,:,None]
Image.fromarray((acc/wt).clip(0,255).astype('uint8')).save('src/img/keuze-strip.jpg',quality=88)
