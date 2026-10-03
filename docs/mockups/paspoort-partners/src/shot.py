# Re-render mockups: python3 shot.py a:794:1123:a-recruiterpagina-p1 ...  (name:width:height:outfile)
# Run pages.py / screens.py / teams.py first to regenerate the HTML. Needs playwright + a Chrome/Chromium.
import sys, pathlib
from playwright.sync_api import sync_playwright
here = pathlib.Path(__file__).resolve().parent
jobs = [a.split(':') for a in sys.argv[1:]]
with sync_playwright() as p:
    b = p.chromium.launch(executable_path="/usr/bin/google-chrome")
    for name, w, h, out in jobs:
        pg = b.new_page(viewport={"width": int(w), "height": int(h)}, device_scale_factor=2)
        pg.goto((here / f"{name}.html").as_uri()); pg.wait_for_timeout(400)
        pg.screenshot(path=str(here.parent / f"{out}.png"), full_page=False)
        pg.close()
    b.close()
