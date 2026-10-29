# v4: de kandidaat is de kreeft. Per stap dezelfde scene als v2 (_bron/scenes/), met de kleine kreeft erin,
# in de richting die logisch is, en het licht van de zone. Schrijft _bron/<naam>.jpg.
import sys; sys.path.insert(0,'_bron')
from kreeft_comp import place, grade
from droom import dreams
from PIL import Image
# zone-licht: (kleur, sterkte) en tint voor de kreeft
L={'strand':((236,190,206),.30,(250,215,220)),'kust':((250,222,186),.16,(255,232,205)),'rotsen':((255,236,200),.08,(255,240,215)),
   'diepte':((150,200,220),.0,(170,215,230)),'licht':((255,196,110),.28,(255,215,150)),'haven':((255,238,215),.06,(255,240,220))}
# naam: zone, pose, x, voet-y, hoogte, flip, rotatie, schaduw
P={
'inloggen':('strand','strand',1010,468,112,False,0,True),
'route-kaart':('strand','strand',165,398,64,False,0,True),
'kust-kennismaken':('kust','kust',600,468,96,False,0,True),
'kust-wanneer':('kust','kust',845,452,86,False,0,True),
'kust-vervoer':('kust','kust',420,437,86,False,0,True),
'rotsen-werk':('rotsen','kust',368,402,92,False,0,True),
'rotsen-leren':('rotsen','rotsen',300,468,100,False,0,False),
'rotsen-droom':('rotsen','uitkijk',482,503,86,False,0,True),
'diepte-werken':('diepte','diepte',560,385,80,True,-28,False),
'diepte-leuk':('diepte','diepte',640,420,74,True,-12,False),
'diepte-pauze':('diepte','zitten',640,438,92,False,0,True),
'diepte-thuis':('diepte','diepte',610,420,74,True,-6,False),
'diepte-waarden':('diepte','diepte',1010,432,96,True,30,False),
'licht-kompas':('licht','licht',800,466,86,False,0,True),
'licht-paspoort':('licht','haven',205,430,90,False,0,True),
'mijn-lobsy':('haven','haven',540,472,90,True,0,True),
'match':('haven','haven',545,445,82,False,0,True),
'banenkaart':('haven','kaart',1150,452,86,False,0,True),
'bedrijvenkaart':('haven','haven',795,466,90,False,0,True),
'bewaard':('haven','haven',650,433,84,True,0,True),
'sollicitaties':('haven','haven',795,438,86,True,0,True),
'toevoegen':('haven','haven',725,482,96,True,0,True),
'activiteiten':('haven','kaart',505,538,100,False,0,True),
'na-de-match':('haven','haven',448,462,84,True,0,True),
'moment':('haven','uitkijk',820,394,70,False,0,True),
'berichten':('haven','uitkijk',1028,442,70,True,0,True),
'nu-niet':('haven','wachten',120,418,86,False,0,True),
'carriere':('haven','haven',470,402,82,False,0,True),
'omscholen':('haven','kaart',600,528,100,True,0,True),
'wat-drijft':('haven','uitkijk',205,396,82,False,0,True),
'ondernemer':('haven','haven',1215,440,90,True,0,True),
'adviseur':('haven','zitten',1000,528,100,True,0,False),
'profiel':('haven','haven',905,478,100,False,0,True),
'instellingen':('haven','licht',470,440,96,True,0,True),
}
# zitten: voet-y is onderkant van de poten; de stoel/steen ligt hoger. Dat is in de waarden verrekend.
only=sys.argv[1:]
for n,(z,pose,x,foot,h,fl,rot,sh) in P.items():
    if only and n not in only: continue
    col,s,tint=L[z]
    im=Image.open('_bron/scenes/'+n+'.jpg').convert('RGB')
    if s: im=grade(im,col,s)
    im=place(im,pose,x,foot,h,flip=fl,rot=rot,tint=tint,shadow=sh,sh_col=(40,70,90) if z=='diepte' else (80,60,45))
    if n=='rotsen-droom': im=dreams(im,head=(500,428))
    im.save('_bron/'+n+'.jpg',quality=92)
    print(n)
