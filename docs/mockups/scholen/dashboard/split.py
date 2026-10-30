import sys
from PIL import Image
f=sys.argv[1]; im=Image.open(f); h=int(sys.argv[2]) if len(sys.argv)>2 else 1500
n=0
for y in range(0,im.height,h):
    im.crop((0,y,im.width,min(im.height,y+h))).save(f'/tmp/'+f.split('/')[-1][:2]+f'-p{n}.png'); n+=1
print(n, im.size)
