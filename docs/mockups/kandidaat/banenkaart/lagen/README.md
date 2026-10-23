# Banenkaart: kaartlagen + vacaturepopups

## Popups (goedgekeurd door Dennis, 23-10-2026 22:43)

Hoofdset (2x, uurloon-tooltip dicht):
- desktop/03-banenkaart-lagen-desktop-1440-enkel.png: losse vacature
- desktop/04-banenkaart-lagen-desktop-1440-cluster.png: cluster met navigatie
- mobile/04-banenkaart-lagen-mobiel-390-enkel.png: losse vacature, mobiel
- mobile/05-banenkaart-lagen-mobiel-390-cluster.png: cluster met navigatie, mobiel

Extra (uurloon-tooltip open):
- extra/desktop-1440-enkel-uurloon-hover.png: hover op de euroknop (desktop)
- extra/mobiel-390-enkel-uurloon-tik.png: tik op de euroknop (mobiel)

### De kaart (Funda-stijl)
- Schone kaart zonder balk erin.
- Bovenaan een grote foto van de werkplek (geen mensen, geen tekst, geen logo's).
- Daaronder op een regel: het label "Bij werkgever" (of "Via intermediair") met het pin-icoon, en rechts het hartje (bewaren) als dun omlijnd icoon met een tikvlak van 44 px. Het hartje staat dus rechtsboven in het tekstdeel, niet in de foto.
- De functie groot (bijvoorbeeld "Teeltmedewerker kas"), de werkgever in blauw, de plaats.
- Icoonregel: reistijd op de fiets, uren per week, soort contract.
- Onderaan een regel "Bekijk vacature" met pijltje als enige actie.

### Knoppen boven de kaart
- Losse zwevende vierkante knoppen (44 x 44 px, met schaduw), buiten de kaart, net boven de bovenrand.
- Links, alleen bij een cluster: [<] [3 van 15] [>] om door de vacatures in het cluster te bladeren.
- Rechts: [€] [X], de euroknop direct links van het kruisje. Bij een losse vacature staat links niets.
- Het gekozen cluster of de gekozen pin is op de kaart groter en duidelijk gemarkeerd.

### Uurloon-tooltip
- Staat niet in de kaart. Verschijnt bij hover op de euroknop (desktop) of bij tikken op de euroknop (mobiel).
- Klein en licht: crème achtergrond, dunne rand, zachte schaduw, kleine donkergrijze tekst, klein pijltje naar de euroknop.
- Titel "Uurloon (bruto)" met compacte regels: 14, 15, 16, 17, 18, 19 en 20 jaar en 21 jaar en ouder.
- De eigen leeftijd (21, uit het profiel) is alleen vetgedrukt met een lichtgroene achtergrond.

### Mobiel
- Compacte horizontale kaart onderaan het scherm: foto links, gegevens rechts, hartje rechtsboven in het tekstdeel.
- Dezelfde zwevende knoppen boven de kaart (navigatie links bij cluster, € en X rechts).

### Let op: voorbeeldbedragen
De uurlonen in de tooltip zijn voorbeeldbedragen, geen echte cao-data. Ze volgen de staffel van het jeugdminimumloon op basis van 15,40 euro bij 21 jaar en ouder: 15 jaar 30% (4,62), 16 jaar 34,5% (5,31), 17 jaar 39,5% (6,08), 18 jaar 50% (7,70), 19 jaar 60% (9,24), 20 jaar 80% (12,32), 21 jaar en ouder 15,40. Voor 14 jaar bestaat geen wettelijk minimumloon; in de mockup staat hetzelfde bedrag als bij 15 jaar. Alle namen en de leeftijd zijn nepdata (Sam Voorbeeld).

## Overige staten
- desktop/01 en 02, mobile/01 tot en met 03: eerder goedgekeurde kaartlagen (reistijdringen, clusters, werkgever- en intermediairpins, legenda, header C).

## Renderen
cd src && node shot.mjs [deel-van-bestandsnaam]
Staten via ?s=: none, legend, popup, emp (losse vacature), clus (cluster), empeur (tooltip open), int (intermediair, niet in hoofdset).
