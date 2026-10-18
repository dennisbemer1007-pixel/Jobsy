import os,sys
D=os.path.dirname(os.path.abspath(__file__))
css=open(D+"/base.css").read()
def page(name,style,body):
  open(f"{D}/{name}.html","w").write(f'<!doctype html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><style>{css}{style}</style></head><body>{body}</body></html>')
exec(open(D+"/app.inc.py").read())
exec(open(D+"/common.inc.py").read())
exec(open(D+"/opl.inc.py").read())
only=sys.argv[1:]
for m in ["carriere-leren","testresultaten-groeien","leren-groeien"]:
  if (not only or m in only) and os.path.exists(f"{D}/{m}.py"): exec(open(f"{D}/{m}.py").read())
