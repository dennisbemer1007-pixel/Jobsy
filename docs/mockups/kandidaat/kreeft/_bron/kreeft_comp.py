# Plaatst de kleine kreeft (uitsnede uit een v4-pose, _bron/v4/cut/*.png) in een header-scene,
# en kleurt de scene naar het licht van de zone (dageraad op het strand tot goud bij de vuurtoren).
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
C='_bron/v4/cut/'
def cut(pose): return Image.open(C+pose+'.png').convert('RGBA')
def grade(im, col, s, top=True):
    """zacht licht over de scene: kleur col met sterkte s, sterker naar boven (lucht)"""
    a=np.asarray(im.convert('RGB')).astype(float)/255; H,W,_=a.shape
    c=np.array(col,float)/255
    g=np.linspace(1.0,0.45,H)[:,None,None] if top else np.ones((H,1,1))
    sl=np.where(c<=.5, a-(1-2*c)*a*(1-a), a+(2*c-1)*(np.where(a<=.25,((16*a-12)*a+4)*a,np.sqrt(a))-a))
    o=a*(1-s*g)+sl*(s*g)
    return Image.fromarray((np.clip(o,0,1)*255).astype('uint8'))
def place(scene, pose, x, foot, h, flip=False, rot=0, tint=None, tmix=0.15, shadow=True, sh_col=(80,60,45)):
    f=cut(pose)
    if flip: f=f.transpose(Image.FLIP_LEFT_RIGHT)
    if rot: f=f.rotate(rot,resample=Image.BICUBIC,expand=True); f=f.crop(f.getchannel('A').point(lambda v:255 if v>30 else 0).getbbox())
    r=h/f.height; f=f.resize((max(1,round(f.width*r)),round(h)),Image.LANCZOS)
    if tint is not None:
        arr=np.asarray(f).astype(float); t=np.array(tint,float)
        arr[...,:3]=arr[...,:3]*(1-tmix)+(arr[...,:3]*t/255)*tmix
        f=Image.fromarray(np.clip(arr,0,255).astype('uint8'),'RGBA')
    sc=scene.convert('RGBA')
    if shadow:
        sh=Image.new('L',sc.size,0); d=ImageDraw.Draw(sh)
        sw=f.width*0.8; d.ellipse((x-sw/2,foot-h*0.04,x+sw/2,foot+h*0.03),fill=255)
        sh=sh.filter(ImageFilter.GaussianBlur(max(1,h*0.035))).point(lambda v:int(v*0.35))
        sc=Image.composite(Image.new('RGBA',sc.size,sh_col+(255,)),sc,sh)
    sc.alpha_composite(f,(int(x-f.width/2),int(foot-f.height)))
    return sc.convert('RGB')
