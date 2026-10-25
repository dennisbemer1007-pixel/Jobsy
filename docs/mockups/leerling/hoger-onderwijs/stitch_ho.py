import sys; sys.path.insert(0,'/workspace/mocklib'); from stitch import build
from alpha import cut
from PIL import Image
S='/workspace/ho-mockup/illustraties'; O='/workspace/ho-mockup/src/img'; BG=(244,242,238)
build(S,O,'duin',[('duin-p1.jpg',0,1024,335),('duin-p2.jpg',0,1024,335)],mobx=300,bg=BG)
build(S,O,'horizon',[('horizon2.jpg',0,1024,338)],mobx=800,bg=BG)
cut(f'{S}/merk.jpg',f'{O}/merk.png',(280,120,750,870),(188,300),thr=22)
for n,w in [('welkom',1800),('reiskaart',1800),('gracht',1600),('park',1600),('haven',1600),('rivier',1600),('wegen',1600)]:
    im=Image.open(f'{S}/{n}.jpg').convert('RGB'); im.resize((w,round(im.height*w/im.width)),Image.LANCZOS).save(f'{O}/big-{n}.jpg',quality=90)
# thumbs (1280-space coords)
k=1.25
for n,f,(cx,cy,r) in [('duin','duin-p1.jpg',(500,360,170)),('gracht','gracht.jpg',(512,330,190)),('park','park.jpg',(512,300,220)),('haven','haven.jpg',(720,280,230)),('rivier','rivier.jpg',(780,270,210))]:
    im=Image.open(f'{S}/{f}').convert('RGB'); cx,cy,r=cx*k,cy*k,r*k
    im.crop((int(cx-r),int(cy-r),int(cx+r),int(cy+r))).resize((200,200),Image.LANCZOS).save(f'{O}/th-{n}.jpg',quality=88)
