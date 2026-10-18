import os
D=os.path.dirname(os.path.abspath(__file__));A="../../../onboarding-illustraties/"
css=open(D+"/base.css").read();foot=open(D+"/foot.inc").read()
def page(name,style,body,f=True):
  open(f"{D}/{name}.html","w").write(f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><style>{css}{style}</style></head><body>{body}{foot if f else ""}</body></html>')
for m in ["login","start","kompas","banenkaart"]:
  if os.path.exists(f"{D}/{m}.py"): exec(open(f"{D}/{m}.py").read())
