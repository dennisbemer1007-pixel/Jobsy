# headers-overzicht.jpg: alle desktop-strips, 2 kolommen x 18 rijen
import os
from PIL import Image, ImageDraw, ImageFont
F='/usr/share/fonts/truetype/sand-box/google/Fraunces/Fraunces-VariableFont_SOFT,WONK,opsz,wght.ttf'
names=sorted({f.rsplit('-',1)[0] for f in os.listdir('src/strips') if f.endswith('-desk.jpg')})
W=880; H=int(W*225/1440); LB=30; PAD=24; TOP=90
rows=(len(names)+1)//2
cv=Image.new('RGB',(PAD*3+W*2,TOP+rows*(H+LB+14)+PAD),(250,246,238)); d=ImageDraw.Draw(cv)
d.text((PAD,26),'Lobsy kandidaatreis · header-strips v4 · jij bent de kreeft, klein en op weg',font=ImageFont.truetype(F,34),fill=(20,40,80))
f=ImageFont.truetype(F,20)
for i,n in enumerate(names):
    c,r=i%2,i//2; x=PAD+c*(W+PAD); y=TOP+r*(H+LB+14)
    d.text((x,y+2),n,font=f,fill=(190,80,40))
    cv.paste(Image.open(f'src/strips/{n}-desk.jpg').convert('RGB').resize((W,H),Image.LANCZOS),(x,y+LB))
cv.save('headers-overzicht.jpg',quality=86); print(cv.size,len(names))
