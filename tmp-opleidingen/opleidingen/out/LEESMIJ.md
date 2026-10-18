# Mockups 'Workshops en opleidingen' (ronde 1), alleen mockups, niets in de repo
Basis: origin/acceptatie d4a714e. Zelfde tokens, header, tabbar en footer als ../kandidaat-paginas.
Nepdata: Sanne de Vries, Zaandam, droombaan dierenarts. Aanbieders zijn generiek ("ROC (regio)", "Leerwerkloket (regio)", "Online cursus").
Pagina's:
  carriere-leren          /carriere: route + per stap passende workshops/opleidingen, details in zijpaneel (mobiel: inline uitklap)
  testresultaten-groeien  Paspoort > Mijn tests: per competentie in woorden, bij groeipunten "Hier kun je in groeien"
  leren-groeien           Carrière > Leren & groeien: filters + lijsten + zijkaart "Jouw voortgang"
Opnieuw renderen: cd src && python3 build.py && /workspace/.venv-pw/bin/python render.py   (render doet ook overflow/overlap-check: alles ok)
Art: alleen bestaande illustraties (pad.jpg, bg-hoe-je-werkt.jpg, bg-sfeer.jpg, opleiding.jpg-uitsneden, dna.jpg-cirkel).
