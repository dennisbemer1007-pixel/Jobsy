# Getekende kaart-placeholder (geen echte kaartdata)
import random,math,os
random.seed(4);W,H=840,860
s=[f'<svg class="map" viewBox="0 0 {W} {H}" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg"><rect width="{W}" height="{H}" fill="#f1efe8"/>']
for x,y,w,h in [(40,60,180,120),(600,90,200,160),(80,620,220,180),(640,600,170,200),(330,40,120,80)]:
  s.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="18" fill="#e3eadb"/>')
s.append('<path d="M520 -10 C 470 150, 560 260, 500 400 S 430 640, 480 880" stroke="#cfdfe6" stroke-width="34" fill="none"/>')
s.append('<path d="M-10 300 C 120 320, 200 280, 300 300" stroke="#d6e4ea" stroke-width="14" fill="none"/>')
for i in range(28):
  x1,y1=random.randint(0,W),random.randint(0,H);a=random.choice([0.2,1.6,0.9,2.4]);L=random.randint(120,320);sw=random.choice([3,4,6])
  s.append(f'<path d="M{x1} {y1} l{L*math.cos(a):.0f} {L*math.sin(a):.0f}" stroke="#faf8f3" stroke-width="{max(2,sw-2)}" stroke-linecap="round"/>')
s.append('<path d="M0 520 C 200 500, 420 560, 840 480" stroke="#fdf7ea" stroke-width="10" fill="none"/><path d="M0 520 C 200 500, 420 560, 840 480" stroke="#efdcb5" stroke-width="2" fill="none"/>')
s.append('<path d="M260 0 C 280 300, 240 600, 300 860" stroke="#fdf7ea" stroke-width="9" fill="none"/>')
cx,cy=420,430
def poly(r,j):
  return " ".join(f'{cx+r*(1+j*math.sin(3*a)+j*.6*math.cos(5*a))*math.cos(a):.0f},{cy+r*(1+j*math.sin(3*a)+j*.6*math.cos(5*a))*.92*math.sin(a):.0f}' for a in [k/36*2*math.pi for k in range(36)])
s.append(f'<polygon points="{poly(240,.07)}" fill="rgba(15,45,92,.03)" stroke="#0f2d5c" stroke-width="1.6" stroke-dasharray="2 4"/>')
s.append(f'<polygon points="{poly(170,.08)}" fill="rgba(15,45,92,.05)" stroke="#0f2d5c" stroke-width="1.8" stroke-dasharray="7 5"/>')
s.append(f'<polygon points="{poly(95,.06)}" fill="rgba(15,45,92,.06)" stroke="#0f2d5c" stroke-width="2"/>')
for (x,y,t) in [(330,80,"Wormerveer"),(300,250,"Koog a/d Zaan"),(400,520,"Zaandam"),(620,700,"Oost")]:
  s.append(f'<text x="{x}" y="{y}" font-family="Segoe UI,sans-serif" font-size="13" fill="#7c8696" font-weight="600">{t}</text>')
for x,y,n in [(560,250,7),(330,330,3),(200,150,9),(470,610,5),(250,520,2),(630,335,4),(380,700,6)]:
  s.append(f'<circle cx="{x}" cy="{y}" r="17" fill="#2f7a4f" stroke="#fff" stroke-width="3"/><text x="{x}" y="{y+5}" text-anchor="middle" font-family="Segoe UI,sans-serif" font-size="13" font-weight="700" fill="#fff">{n}</text>')
s.append(f'<circle cx="{cx}" cy="{cy}" r="9" fill="#2563eb" stroke="#fff" stroke-width="3"/></svg>')
open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'map.svg'),'w').write("".join(s))
