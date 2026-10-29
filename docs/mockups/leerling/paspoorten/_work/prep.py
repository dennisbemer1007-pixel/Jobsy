from PIL import Image, ImageOps
G='/workspace/groep78-zee-mockup/src/img/'; V='/workspace/vo-mockup/'; H='/workspace/ho-mockup/'
O='src/img/'
def crop(src,box,out,w,q=80):
    im=Image.open(src).convert('RGB').crop(box)
    h=round(im.height*w/im.width); im=im.resize((w,h),Image.LANCZOS); im.save(O+out,quality=q,optimize=True,progressive=True); print(out,im.size)
# G78
crop('_work/g78-hdr-raw.jpg',(0,87,1280,525),'g78-hdr.jpg',2000)
crop(G+'oceaan.jpg',(0,360,2048,750),'g78-hdr2.jpg',2000)
for n in ['rif','grot','vuurtoren','lagune']:
    im=Image.open(G+n+'.jpg'); crop(G+n+'.jpg',(0,150,2048,1050),'g78-'+n+'.jpg',560,78)
for n in ['embleem','kreeft-kijker','kreeft-schild']:
    im=Image.open(G+n+'.png'); im.thumbnail((360,360)); im.save(O+'g78-'+n+'.png',optimize=True); print(n,im.size)
# VO
crop(V+'illustraties/lagune-p1.jpg',(0,140,1280,590),'vo-hdr.jpg',1800)
crop(V+'src/img/hdr-route-desk.jpg',(0,0,2880,300),'vo-hdr2.jpg',2200)
for n in ['rif','grot','toren','lagune']:
    crop(V+'src/img/th-'+n+'.jpg',(0,0,200,200),'vo-th-'+n+'.jpg',200,82)
im=Image.open(V+'src/img/merk.png'); im.thumbnail((120,150)); im.save(O+'vo-merk.png',optimize=True)
# HO
crop(H+'illustraties/horizon.jpg',(0,130,1280,500),'ho-hdr.jpg',1800)
crop(H+'src/img/hdr-horizon-desk.jpg',(0,0,2880,300),'ho-hdr2.jpg',2200)
for n in ['duin','gracht','park','rivier','haven']:
    crop(H+'src/img/th-'+n+'.jpg',(0,0,200,200),'ho-th-'+n+'.jpg',200,82)
im=Image.open(H+'src/img/merk.png'); im.thumbnail((100,150)); im.save(O+'ho-merk.png',optimize=True)
for n in ['duin-p1','gracht','park','rivier','haven']:
    src=H+'illustraties/'+n+'.jpg'
    crop(src,(0,160,1280,560),'ho-l-'+n+'.jpg',520,78)
