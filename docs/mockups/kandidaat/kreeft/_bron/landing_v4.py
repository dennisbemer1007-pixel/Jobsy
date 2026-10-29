# Moment 0: de kreeft uit de gegenereerde scene halen (hij keek naar zee) en hem in profiel
# naar rechts laten lopen, het pad op naar de vuurtoren. Bron: _bron/v4/scene-landing.jpg -> _bron/landing.jpg
import sys; sys.path.insert(0,'_bron')
import numpy as np
from PIL import Image, ImageFilter, ImageDraw
from kreeft_comp import place
im=Image.open('_bron/v4/scene-landing.jpg').convert('RGB'); a=np.asarray(im).astype(float)
x0,x1,y0,y1=292,398,268,470
src=a[y0:y1,x0-(x1-x0)-6:x0-6].copy()          # zelfde rijen links ernaast: lucht, zee en zand lopen horizontaal door
L=a[y0:y1,x0-4:x0].mean(1,keepdims=True); R=a[y0:y1,x1:x1+4].mean(1,keepdims=True)
t=np.linspace(0,1,x1-x0)[None,:,None]
ramp=L*(1-t)+R*t
fill=src-src.mean(1,keepdims=True)+ramp            # textuur van links, kleur aansluitend op beide randen
m=Image.new('L',im.size,0); ImageDraw.Draw(m).rectangle((x0+6,y0+6,x1-6,y1-6),fill=255)
m=np.asarray(m.filter(ImageFilter.GaussianBlur(5))).astype(float)[...,None]/255
b=a.copy(); b[y0:y1,x0:x1]=fill
out=a*(1-m)+b*m
im=Image.fromarray(np.clip(out,0,255).astype('uint8'))
im.save('_bron/v4/scene-landing-leeg.jpg',quality=94)
im=place(im,'kust',370,452,150,flip=False,tint=(236,190,206),tmix=.07,shadow=True)
im.save('_bron/landing.jpg',quality=92); print('ok')
