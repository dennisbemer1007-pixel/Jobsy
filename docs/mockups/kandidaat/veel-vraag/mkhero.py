from PIL import Image, ImageFilter, ImageDraw
import sys
BG=(250,246,239)
def band(src,y0,y1):
    im=Image.open(src).convert('RGB'); return im.crop((0,y0,im.width,y1))
def comp(b,W,H,pad,out,feather_x):
    h=H-2*pad; w=round(b.width*h/b.height)
    if w>W: w=W; h=round(b.height*w/b.width)
    b=b.resize((w,h),Image.LANCZOS)
    m=Image.new('L',(w,h),0); d=ImageDraw.Draw(m)
    fx=int(w*feather_x); fy=int(h*0.10)
    m=Image.new('L',(w,h),255)
    px=m.load()
    for x in range(w):
        for y in range(h):
            a=1.0
            if x<fx:a*=x/fx
            if x>w-fx:a*=(w-x)/fx
            if y<fy:a*=y/fy
            if y>h-fy:a*=(h-y)/fy
            px[x,y]=int(255*a)
    c=Image.new('RGB',(W,H),BG); c.paste(b,((W-w)//2,(H-h)//2),m); c.save(out,quality=90); print(out,w,h)
name,y0,y1=sys.argv[1],int(sys.argv[2]),int(sys.argv[3])
b=band(f'illustraties/{name}-panorama-bron.jpg',y0,y1)
comp(b,2880,300,22,f'src/img/hero-{name}-desk.jpg',.12)
comp(b,1170,270,18,f'src/img/hero-{name}-mob.jpg',.06)
