# Bedrijvenkaart en bedrijfspagina (r21, goedgekeurd 23 oktober 2026, 07:15)

Vervangt r20. Desktop 1440 en mobiel 390, van elk het eerste scherm (viewport) en de volledige pagina.

Wat er anders is dan r20:
- Header: smalle aquarelstrook (winkelstraat) die over de hele breedte doorloopt, zonder kader of harde randen. Hoogte 150 px op desktop en 90 px op mobiel, gelijk aan het Paspoort. Op kaartpagina en bedrijfspagina. Bronbeeld: `header-panorama.jpg`.
- Warme crèmelook op de hele pagina: warme kaartkleuren, ronde pins in terracotta en blauw, zachte lijstkaarten, knoppen en filters.
- Op de kaart een pop-up bij het gekozen bedrijf met "Bekijk bedrijf" en "Route".
- Bedrijfsnamen in de mockup zijn nepdata.

Alle functies van r20 blijven: wissel Banenkaart/Bedrijvenkaart, filters, teller "3 van 3 deze week over", labels "Staat open voor jou" en "Past goed", knoppen voor 3D, zoom en locatie, en de hele bedrijfspagina met "Ik wil hier werken".

Eisen:
- Bouw op dezelfde manier als de onboarding: gewone JS-knoppen, same-origin fetch, opslaan per actie, volledige navigatie, zichtbare foutmelding bij een fout.
- Alle knoppen moeten werken.
- Laadtijd is erg belangrijk.
