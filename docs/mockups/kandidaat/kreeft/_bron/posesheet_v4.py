# Posesheet v4: kleiner in beeld, nooit naar de kijker; profiel, driekwart of van achteren, in de richting van het verhaal.
from PIL import Image, ImageDraw, ImageFont
import glob
C='_bron/v4/cut/'
P=[('strand','Strand · moment 0','van achter, kijkt de route op','10 lagen schild'),('kust','Kust · onderweg','profiel, loopt naar rechts','eerste lagen los'),
   ('rotsen','Rotsen · klimmen','klimt naar rechtsboven','het schild barst'),('uitkijk','Rotsen · droom','van achter, kijkt ver weg','schilfers vallen'),
   ('diepte','Diepte · duiken','zwemt de diepte in','laatste lagen los'),('licht','Licht · uit je schild','van achter, één kompas','nieuw schild'),
   ('haven','Haven · de app','profiel, met post','nieuw schild'),('zitten','Rusten · adviseur','zit, zijaanzicht','nieuw schild'),
   ('kaart','404 · kaart lezen','kijkt op de kaart','nieuw schild'),('wachten','Foutpagina · wachten','zit op een paal','nieuw schild')]
W,H=3000,980; bg=(249,243,231)
sh=Image.new('RGB',(W,H),bg); d=ImageDraw.Draw(sh)
fr=[f for f in glob.glob('src/*.ttf') if 'raunces' in f]; nu=[f for f in glob.glob('src/*.ttf') if 'unito' in f]
tf=ImageFont.truetype(fr[0],50); lf=ImageFont.truetype(nu[0],28); sf=ImageFont.truetype(nu[0],21)
d.text((60,40),'Lobsy kreeft v4 · jij bent de kreeft, op weg',fill=(15,45,92),font=tf)
d.text((60,108),'Hij kijkt je niet meer aan: profiel, driekwart of van achteren, in de richting die het verhaal vraagt. Elke zone een schildlaag minder.',fill=(90,100,120),font=ImageFont.truetype(nu[0],30))
cw=W//len(P); top,bot=180,760
for i,(n,l,s,z) in enumerate(P):
    im=Image.open(C+n+'.png').convert('RGBA')
    if n=='diepte': im=im.rotate(-22,resample=Image.BICUBIC,expand=True); im=im.crop(im.getchannel('A').getbbox())
    r=min((cw-30)/im.width,(bot-top)/im.height)
    im=im.resize((int(im.width*r),int(im.height*r)),Image.LANCZOS)
    x=i*cw+(cw-im.width)//2; y=bot-im.height
    sh.paste(im,(x,y),im)
    for t,f,yy,c in ((l,lf,785,(15,45,92)),(s,sf,825,(110,115,130)),(z,sf,858,(223,85,48))):
        tw=d.textlength(t,font=f); d.text((i*cw+(cw-tw)/2,yy),t,fill=c,font=f)
sh.save('poses-v4.jpg',quality=86,optimize=True,progressive=True); print('ok')
