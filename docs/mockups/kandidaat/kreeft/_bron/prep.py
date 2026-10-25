# Maakt de kreeft-uitsnedes (v4, rembg) als WebP en geoptimaliseerde illustraties (headers met kreeft erin gecomponeerd).
import glob, os, shutil
from PIL import Image
B='_bron/'; O='illustraties/'
os.makedirs(O,exist_ok=True)
for f in glob.glob(O+'kreeft-*'): os.remove(f)
for n in ['strand','kust','rotsen','uitkijk','diepte','licht','haven','zitten','kaart','wachten']:
    im=Image.open(B+'v4/cut/kreeft-'+n+'.png' if os.path.exists(B+'v4/cut/kreeft-'+n+'.png') else B+'v4/cut/'+n+'.png').convert('RGBA')
    im=im.crop(im.getchannel('A').getbbox())
    r=min(1,480/im.height); im=im.resize((int(im.width*r),int(im.height*r)),Image.LANCZOS)
    im.save(O+'kreeft-'+n+'.webp','WEBP',quality=80,method=6)
shutil.copy('poses-v4.jpg',O+'kreeft-poses.jpg')
for f in glob.glob(B+'*.jpg'):
    n=os.path.basename(f)[:-4]
    if n.startswith('kreeft'): continue
    Image.open(f).convert('RGB').save(O+n+'.jpg',quality=76,optimize=True,progressive=True)
print(sorted(os.listdir(O)))
