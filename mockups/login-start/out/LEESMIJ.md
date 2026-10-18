# Mockups inlog, start, Kompas, banenkaart (v2, na feedback Dennis) — alleen mockups, geen repo-wijzigingen
Opnieuw renderen:
  cd /workspace/mockups/login-start/out/src && python3 build.py && /workspace/.venv-pw/bin/python render.py [login|start|kompas|banenkaart]
  (kaart-placeholder opnieuw: python3 map.py)
Bronnen: login.py, start.py, kompas.py, banenkaart.py, app.inc.py (app-header/tabbar), base.css, foot.inc, map.py -> map.svg
render.py doet ook de overflow/overlap-check. Daarnaast is elke PNG visueel gecontroleerd.
Huidige staat: *-huidig-desktop/mobiel.png (Playwright, niet ingelogd; ook in ../huidig/)
Illustraties: alleen bestaande art uit ../../onboarding-illustraties/*.jpg; uitsneden in src/spots/
Back-up van de 8 renders: /workspace/mockups-backup/login-start-v2/

## Update 18-10 14:00 — banenkaart-blok startpagina
- Kaart heeft nu één achtergrond (#fef1e1 = de crème van de illustratie), dus geen naad meer tussen tekst- en beeldhelft.
- Beeld: volledige scène uit vervoer.jpg (fietsers, trein, bushalte, auto) als `spots/vervoer-blok.jpg`; randen zachtjes uitgefaded naar de kaartkleur, fietswiel en auto blijven heel. Met binnenmarge, verticaal gecentreerd naast de tekst.
- Close-ups: `start-banenkaart-blok-desktop.png` en `start-banenkaart-blok-mobiel.png`.
