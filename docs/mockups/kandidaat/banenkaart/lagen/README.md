# Banenkaart met kaartlagen (goedgekeurd door Dennis op 23-10-2026 om 14:42)

Een uitbreiding op de goedgekeurde filterlade-versie, met de nieuwe header (variant C, zie ../header) bovenaan. Het uitgangspunt is maximale gebruiksvriendelijkheid.

## Kaartlagen
- Reistijdlijnen: drie ringen rond "Jouw adres". 10 min is doorgetrokken met een lichte vulling, 20 min is gestreept en 30 min is gestippeld, elk met een wit label met een fietsje. Er lopen geen lijnen van het adres naar de pins.
- Clusters: groene ronde bollen met een wit getal. Bij de eerste weergave staat er een hint naast: "Tik om in te zoomen", of "Klik om in te zoomen" op desktop.
- Vacature bij werkgever: een donkerblauwe druppel-pin met een wit gebouwtje.
- Vacature via intermediair: een oranjebruin vierkant wolkje met poppetjes en het label "via". De vorm en het icoon zijn anders, niet alleen de kleur.
- Legenda "Wat zie je op de kaart?": op desktop altijd zichtbaar linksonder, op mobiel één tik weg via de knop "Uitleg kaart" (opent als blad onderaan).
- Popup: een label, de functie, de werkgever of het intermediair, de reistijd en één grote knop "Bekijk vacature". Op mobiel verschijnt hij als blad onderaan en wordt de gekozen pin groter.

## Gebruiksvriendelijkheid
- Alles wat je kunt aanraken is minstens 44 px groot.
- Hoog contrast.
- Kleurenblindvriendelijk: vorm en icoon verschillen, niet alleen de kleur.
- Hetzelfde type-icoontje staat op de kaartjes in de lijst, in de kaartjesbalk en in de legenda.
- Eenvoudige taal en weinig elementen; op mobiel staan minder losse pins.
- Geen scores of percentages. Alle data is nep.

Opnieuw renderen: `cd src && node shot.mjs`.
