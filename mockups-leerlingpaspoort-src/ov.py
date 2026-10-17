from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.launch(executable_path='/usr/bin/google-chrome',args=['--no-sandbox'])
    pg=b.new_page(viewport={'width':1840,'height':800},device_scale_factor=2)
    pg.goto('file:///workspace/mockups-leerlingpaspoort/overview.html');pg.wait_for_timeout(500)
    pg.screenshot(path='/workspace/mockups-leerlingpaspoort/overview.png',full_page=True);b.close()
