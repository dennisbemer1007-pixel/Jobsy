"""Renders the base maps (OpenFreeMap 'liberty' via MapLibre) and projects the REAL bike isochrones
(Valhalla, FOSSGIS public instance, 10/20/30 min from Herenstraat, Wateringen) + example cluster points
into pixel space. Output: src/map-<scene>.png + src/proj.json. Run once: python3 basemap.py"""
import pathlib, json, urllib.request
from playwright.sync_api import sync_playwright
d = pathlib.Path(__file__).parent
HOME = [4.2745, 52.0235]
ISO = d / "src/iso-fiets-wateringen.json"
if not ISO.exists():
    q = json.dumps({"locations": [{"lat": HOME[1], "lon": HOME[0]}], "costing": "bicycle",
                    "contours": [{"time": 10}, {"time": 20}, {"time": 30}], "polygons": True, "denoise": .5, "generalize": 50})
    req = urllib.request.Request("https://valhalla1.openstreetmap.de/isochrone", q.encode(), {"Content-Type": "application/json", "User-Agent": "lobsy-mockup"})
    ISO.write_bytes(urllib.request.urlopen(req, timeout=30).read())
iso = json.loads(ISO.read_text())
RINGS = {int(f["properties"]["contour"]): f["geometry"]["coordinates"][0] for f in iso["features"]}
# example vacancy clusters / pins (lng, lat) - Voorbeelddata
PTS = {"wateringen": [4.279, 52.026], "kwintsheul": [4.257, 52.012], "honsel": [4.232, 51.985], "naaldwijk": [4.206, 51.994],
       "poeldijk": [4.22, 52.025], "moerwijk": [4.29, 52.056], "rijswijk": [4.325, 52.037], "delft": [4.357, 52.012],
       "denhoorn": [4.33, 51.99], "monster": [4.175, 52.025], "delier": [4.245, 51.973], "maasdijk": [4.2, 51.957],
       "loosduinen": [4.255, 52.055], "denhaag": [4.305, 52.078], "schipluiden": [4.315, 51.972], "pijnacker": [4.43, 52.016],
       "voorburg": [4.36, 52.07], "gravenzande": [4.165, 52.0], "p1": [4.268, 52.018], "p2": [4.288, 52.031], "p3": [4.2605, 52.0305],
       "p4": [4.296, 52.015], "home": HOME}
SCENES = {  # name: (W, H, zoom, home pixel x, home pixel y)
    "d1": (980, 708, 11.25, 600, 350),
    "m1": (390, 620, 10.55, 190, 250),
    "det": (420, 236, 10.2, 210, 118),
    "mdet": (358, 150, 9.75, 179, 75),
}
BEARINGS = {10: 285, 20: 205, 30: 100}
with sync_playwright() as pw:
    b = pw.chromium.launch(executable_path="/usr/bin/google-chrome", args=["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader"])
    out = {}
    for name, (W, H, Z, HX, HY) in SCENES.items():
        html = f'''<!doctype html><html><head><meta charset="utf-8">
<link href="https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.css" rel="stylesheet">
<script src="https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.js"></script>
<style>html,body{{margin:0}}#m{{width:{W}px;height:{H}px}}.maplibregl-ctrl{{display:none}}</style></head>
<body><div id="m"></div><script>
window.map=new maplibregl.Map({{container:'m',style:'https://tiles.openfreemap.org/styles/liberty',center:{HOME},zoom:{Z},attributionControl:false,preserveDrawingBuffer:true}});
map.once('load',()=>{{map.getStyle().layers.forEach(l=>{{if(l.type==='symbol'&&l.layout&&l.layout['text-field']&&JSON.stringify(l.layout['text-field']).includes('name')&&!l.id.includes('shield'))map.setLayoutProperty(l.id,'text-field',['coalesce',['get','name:nl'],['get','name']]);if(l.id.includes('poi'))map.setLayoutProperty(l.id,'visibility','none')}});map.panBy([{W/2-HX},{H/2-HY}],{{animate:false}});map.once('idle',()=>{{window.done=true}})}});
</script></body></html>'''
        f = d / f"src/basemap-{name}.html"; f.write_text(html)
        pg = b.new_page(viewport={"width": W, "height": H}, device_scale_factor=2)
        pg.goto(f.as_uri()); pg.wait_for_function("window.done===true", timeout=90000); pg.wait_for_timeout(1200)
        pg.screenshot(path=str(d / f"src/map-{name}.png"))
        res = pg.evaluate("""([rings,pts,home,bear])=>{
          const P=v=>{const q=map.project(v);return [Math.round(q.x*10)/10,Math.round(q.y*10)/10]};
          const out={rings:{},pts:{},labels:{}};
          for(const [m,coords] of Object.entries(rings)){ out.rings[m]=coords.map(P);
            const tb=bear[m]; let best=null,bd=1e9;
            for(const c of coords){ const dx=(c[0]-home[0])*Math.cos(home[1]*Math.PI/180), dy=c[1]-home[1];
              let a=(Math.atan2(dx,dy)*180/Math.PI+360)%360; let dd=Math.abs(((a-tb+540)%360)-180); if(dd<bd){bd=dd;best=c;} }
            out.labels[m]=P(best); }
          for(const [k,v] of Object.entries(pts)) out.pts[k]=P(v);
          return out; }""", [{str(k): v for k, v in RINGS.items()}, PTS, HOME, {str(k): v for k, v in BEARINGS.items()}])
        out[name] = res; print(name, "ok", res["pts"]["home"]); pg.close()
    json.dump(out, open(d / "src/proj.json", "w"))
    b.close()
