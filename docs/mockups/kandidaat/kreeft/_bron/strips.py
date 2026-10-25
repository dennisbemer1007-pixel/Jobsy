# Maakt dunne header-strips uit de illustraties: desktop 1440 breed (~225 px hoog) en mobiel 780 breed @2x (~105 px).
# Altijd volledige breedte; figuren (de kreeft met voelsprieten en poten) vallen altijd binnen de band.
import sys; sys.path.insert(0,'_bron')
from PIL import Image
from strip_fit import fit
import os, json
B='_bron/'; O='src/strips/'
os.makedirs(O,exist_ok=True)
# naam: (cy, band) in bronpixels (1280x720); band 200 -> 225 px desktop
S={'diepte-leuk':(380,200),'diepte-thuis':(390,200),'match':(372,200),'banenkaart':(362,200),
'bedrijvenkaart':(372,200),'bewaard':(375,200),'sollicitaties':(350,200),'activiteiten':(462,200),'toevoegen':(440,200),
'na-de-match':(375,200),'berichten':(350,200),'nu-niet':(385,200),'carriere':(330,200),'omscholen':(440,200),
'wat-drijft':(325,200),'ondernemer':(345,200),'adviseur':(432,200),'profiel':(395,200),'instellingen':(365,200),'moment':(380,200),
'kust-kennismaken':(375,200),'kust-wanneer':(370,200),'kust-vervoer':(345,200),'rotsen-werk':(330,200),'rotsen-leren':(375,200),
'rotsen-droom':(420,200),'diepte-werken':(380,200),'diepte-pauze':(365,200),'diepte-waarden':(410,200),'licht-kompas':(395,200),
'licht-paspoort':(345,200),'mijn-lobsy':(375,200),'route-kaart':(345,200)}
# te hoge content: hele band houden en verkleinen, zijkanten lopen uit in papier
F={'landing':(230,486),'inloggen':(170,480)}
MB=345
meta={}
for n,(cy,band) in S.items():
    im=Image.open(B+n+'.jpg').convert('RGB'); W,H=im.size
    t=max(0,min(H-band,cy-band//2)); d=im.crop((0,t,W,t+band)).resize((1440,round(band*1440/W)),Image.LANCZOS)
    d.save(O+n+'-desk.jpg',quality=80,optimize=True,progressive=True)
    t2=max(0,min(H-MB,cy-MB//2))
    m=im.crop((0,t2,W,t2+MB)).resize((780,round(780*MB/W)),Image.LANCZOS)
    m.save(O+n+'-mob.jpg',quality=80,optimize=True,progressive=True)
    meta[n]={'dh':d.height,'mh':round(m.height/2)}
for n,(y0,y1) in F.items():
    fit(B+n+'.jpg',y0,y1,1440,225).save(O+n+'-desk.jpg',quality=80,optimize=True,progressive=True)
    mh=round(780*(y1-y0)/1280)
    fit(B+n+'.jpg',y0,y1,780,mh).save(O+n+'-mob.jpg',quality=80,optimize=True,progressive=True)
    meta[n]={'dh':225,'mh':round(mh/2)}
json.dump(meta,open(O+'meta.json','w'),indent=0)
print(meta)
