import json
S=json.load(open('src/screens.json')); C=json.load(open('src/copy.json'))
def rp(s,m): return f"renders/{'mobile' if m else 'desktop'}/{s['num']:02d}-{s['slug']}-{'mobiel-390' if m else 'desktop-1440'}.png"
esc=lambda t:str(t).replace('|','/').replace('\n',' ')
L=[]
L.append('''# Lobsy kandidaatreis · jij bent de kreeft

Mockupset voor de hele kandidaatreis van Lobsy. De kreeft in aquarel is geen gids of mascotte: **de kandidaat is de kreeft**. Vanaf moment 0 op het strand loop je zelf de reis: langs de kust, over de rotsen, de diepte in voor de tests, en naar het licht bij de vuurtoren, waar je uit je schild bent. Daarna kom je aan in de haven: de app zelf.

Bedoeld voor `docs/mockups/kandidaat/kreeft/` in PR #878. Alle namen, adressen en bedrijven zijn verzonnen (kandidaat: Sam de Wit, Delft).

## Zo bekijk je het

| Wat | Bestand |
|---|---|
| Walkthrough desktop (vorige/volgende, pijltjestoetsen, keuzelijst) | `walkthrough-desktop.html` |
| Walkthrough mobiel (telefoonframe van 390 breed, hele scherm scrollbaar) | `walkthrough-mobiel.html` |
| PDF desktop, A4 liggend, één scherm per pagina | `kreeft-desktop.pdf` |
| PDF mobiel, A4 staand, lange schermen in kolommen | `kreeft-mobiel.pdf` |
| Posesheet van de kreeft (v4) | `poses-v4.jpg` (ook `illustraties/kreeft-poses.jpg`) |
| Overzicht van alle header-strips | `headers-overzicht.jpg` |
| Renders desktop 1440 / mobiel 390 (2x) | `renders/desktop/`, `renders/mobile/` |
| Bron-HTML | `src/` (eigen schermen) en `src/bestaand/` (goedgekeurde layouts met nieuwe strips) |

Een direct scherm openen kan met een hash, bijvoorbeeld `walkthrough-mobiel.html#15`.

## Karakter en toon

- **Jij bent de kreeft.** Hij is de hoofdpersoon van de reis, maar klein in het landschap: een deel van de scène, nooit een poster. Hij past altijd helemaal in de strip, met voelsprieten en poten.
- **Hij kijkt je niet aan.** Je ziet hem in profiel, driekwart of van achteren, en hij beweegt de kant op die logisch is: over het pad naar de vuurtoren (van links naar rechts), omhoog over de rotsen, omlaag de diepte in, in de verte kijkend bij Droom, met zijn brieven naar het postkantoor bij Sollicitaties. Alleen bij de 404 kijkt hij omlaag naar zijn kaart.
- **Rustig en volwassen.** Geen springen, juichen of zwaaien. Altijd dezelfde kreeft met marineblauwe sjaal, leren riem en koperen kompas (één kompas).
- **Stijl:** zachte aquarel, veel papier, warme kleuren. Headers zijn dunne strips over de volle breedte: ongeveer 225 px op desktop, ongeveer 105 px op mobiel.
- **Taal:** korte, kalme zinnen in je-vorm. De tekst praat tegen jou, niet namens een coach: geen "ik" of "we" van een kreeft, geen tekstballonnen. Geen uitroeptekens, geen haast, geen oordeel. Altijd een uitweg: pauzeren kan, overslaan kan, je adviseur helpt.
- **Spelregels copy:** NL en EN (zie copydeck), geen gedachtestreepjes als leesteken, geen scores of percentages, kansberoep heet altijd "Veel vraag in jouw regio". Het UWV-overzicht houdt de goedgekeurde bewoording.
- **Hulp is neutraal.** De knop en het paneel heten **Lobsy-hulp**, met het Lobsy-merkteken en de ondertitel "Hulp bij de app en je reis". De kreeft is de kandidaat, dus niet de helper. Notities in de pagina heten "Goed om te weten", zonder portret.
- **Voor iedereen:** teksten gaan uit van "met of zonder diploma", "jong of wat ouder".

## De reis van moment 0

1. **Moment 0 (landing):** je loopt bij zonsopgang over het strand, in profiel naar rechts, nog in je zware schild met zeepokken, het duinpad op naar de vuurtoren. "Dit is jouw reis. Stap voor stap kom je uit je schild." De knop heet "Stap op het strand".
2. **Strand:** inloggen en toestemming. Je loopt naar het strandpaviljoen en ziet de hele route op de kaart.
3. **Kust (stap 1 tot en met 3):** je loopt in profiel naar rechts door de straat, langs de getijdenpaal en over het duinpad. Lagen 1 tot en met 3 laten los.
4. **Rotsen (stap 4 tot en met 6):** je klimt, en bij Droom sta je naast de verrekijker en kijk je over zee: in zachte droomwolkjes zie je jezelf als piloot, schilder, dokter, bakker, tuinier en bouwvakker. Het schild barst. Lagen 4 tot en met 6.
5. **Diepte (test 1 tot en met 4):** je duikt en zwemt naar rechts, schuin omlaag; halverwege zit je even op een steen. Lagen 7 tot en met 10.
6. **Licht (Kompas en paspoort):** gouden licht bij de vuurtoren. Je staat er van achteren, één kompas in je schaar, met een fris schild; de oude stukjes liggen bij je poten.
7. **Haven (de app):** na de reis ben je in de haven. Je loopt in profiel naar het havenkantoortje (Mijn Lobsy), met brieven naar het postkantoor (Sollicitaties), naar het tafeltje voor twee (Na de match).

**Licht loopt mee:** strand in roze dageraad, kust in vroege ochtend, rotsen in helder daglicht, diepte in blauw, Licht in goud, de haven in warm daglicht.

**Reiskaart:** op elk reisscherm (1 tot en met 16) staat een kaart met de vijf zones als bolletjes (afgerond, nu, nog te gaan), het schild in tien lagen en een zin als "Nog 7 lagen schild. Na deze stap laat laag 4 los." Op mobiel staat die kaart boven de vraag.

## Schillagen

| Zone | Stappen | Lagen die loslaten | Hoe de kreeft eruitziet |
|---|---|---|---|
| Strand | landing, inloggen, start | geen (nog 10) | zwaar schild met zeepokken |
| Kust | 1 tot en met 3 | 1, 2, 3 | nog zeepokken, loopt mee |
| Rotsen | 4 tot en met 6 | 4, 5, 6 | schild barst, schilfers |
| Diepte | test 1 tot en met 4 | 7, 8, 9, 10 | laatste lagen los |
| Licht | Kompas, paspoort | alle tien los | nieuw, fris schild |

## Poses (v4)

Posesheet: `poses-v4.jpg`. Losse uitsnedes (WebP met transparantie) in `illustraties/kreeft-*.webp`. In de headers zijn deze uitsnedes klein in de bestaande, goedgekeurde scènes gezet, met het licht van de zone eroverheen (bron: `_bron/headers_v4.py`).

| Pose | Hoe | Waar |
|---|---|---|
| strand | van achteren, kijkt de route op, zwaar schild | inloggen, start |
| kust | profiel, loopt naar rechts | landing (moment 0), Kust, Rotsen · werk |
| rotsen | klimt naar rechtsboven | Rotsen · opleiding |
| uitkijk | van achteren, schaar boven de ogen | Droom, moment, berichten, wat drijft jou |
| diepte | zwemt, schuin omlaag | Diepte |
| zitten | zit, zijaanzicht | Even rusten, adviseur |
| licht | van achteren, één kompas | Jouw Kompas, instellingen |
| haven | profiel, met post en rugzak | paspoort en de app |
| kaart | kijkt omlaag op zijn kaart | 404, banenkaart, activiteiten, omscholen |
| wachten | zit op een paal | foutpagina, nu niet |
| droombanen | zes kleine vignetten in droomwolkjes, allemaal in profiel naar rechts: piloot, schilder, dokter, bakker, tuinier, bouwvakker (`_bron/v4/droom/`, ingezet met `_bron/droom.py`) | Rotsen · Droom |

## Shell-lagen van de pagina

Van boven naar onder op elk scherm:

1. App-header (ingelogd of uitgelogd).
2. Header-strip over de volle breedte met de scène van de stap (desktop ongeveer 225 px, mobiel ongeveer 105 px). Landing en inloggen tonen de hele scène kleiner, de zijkanten lopen uit in het papier.
3. Inhoud. In de reis: links de zijbalk met zones (desktop), midden de vraag, rechts de reiskaart (op mobiel boven de vraag).
4. Notitie "Goed om te weten" waar dat helpt (geen portret).
5. Knop Lobsy-hulp rechtsonder (niet op landing, inloggen en het UWV-document).

## Schermen in volgorde van de reis
''')
L.append('| # | Hoofdstuk | Scherm | Desktop | Mobiel |\n|---|---|---|---|---|')
for s in S: L.append(f"| {s['num']:02d} | {esc(s['chapter'])} | {esc(s['title'])} | `{rp(s,0)}` | `{rp(s,1)}` |")
L.append('\n## Mapping: pagina, scène, schildlaag en licht\n\n| # | Scherm | Scène | Kreeft (jij) | Schild | Licht |\n|---|---|---|---|---|---|')
for s in S: L.append(f"| {s['num']:02d} | {esc(s['title'])} | {esc(s['scene'])} | {esc(s['kreeft'])} | {esc(s['laag'])} | {esc(s['licht'])} |")
L.append('\n## Controletabel\n\n| # | Scherm | Illustratie | Scène past bij stap omdat... |\n|---|---|---|---|')
for s in S: L.append(f"| {s['num']:02d} | {esc(s['title'])} | `{esc(s['illus'])}` | {esc(s['past'])} |")
L.append('\n## Copydeck NL en EN\n\nTitels, intro\'s, tips, reiskaartzinnen, notities en zonezinnen. Overige UI-tekst in de mockups is alleen NL.\n\n| # | Soort | NL | EN |\n|---|---|---|---|')
for r in C: L.append(f"| {r[0]} | {esc(r[1])} | {esc(r[2])} | {esc(r[3]) if len(r)>3 else ''} |")
L.append('''
## Opnieuw maken

Vanuit de map `kreeft-kandidaat-mockup/`:

1. `python3 _bron/landing_v4.py` maakt de landing (moment 0). `python3 _bron/headers_v4.py` zet de kreeft (uitsnedes in `_bron/v4/cut/`, gemaakt met rembg) in de scènes uit `_bron/scenes/` en schrijft `_bron/<naam>.jpg`. Met namen als argument alleen die headers.
2. `python3 _bron/strips.py` maakt de header-strips (`src/strips/`).
3. `python3 _bron/posesheet_v4.py` maakt `poses-v4.jpg`; `python3 _bron/prep.py` maakt de WebP-uitsnedes en de illustraties.
4. `cd src && python3 gen.py` schrijft alle schermen, `screens.json` en `copy.json`.
5. `node _bron/render.mjs` rendert desktop en mobiel (optioneel alleen enkele nummers: `node _bron/render.mjs 3,15`). De kaarten laden tegels van OpenFreeMap, dus internet is nodig.
6. `python3 _bron/walk_pdf.py` schrijft de walkthroughs en print-HTML; daarna `node _bron/pdf.mjs /tmp/kreeft-pdf/d.html $PWD/kreeft-desktop.pdf landscape` en `node _bron/pdf.mjs /tmp/kreeft-pdf/m.html $PWD/kreeft-mobiel.pdf portrait`.

Bronnen van de bestaande layouts: kandidaat-mockups-nieuw, na-de-match-r2, veel-vraag-mockup, bedrijvenkaart-r21 en banenkaart-lagen.

## Open punten

- De kreeft is als losse uitsnede in de goedgekeurde scènes gezet, niet in één keer mee geschilderd. Licht en schaduw zijn bijgekleurd per zone; van dichtbij kan een rand net iets scherper zijn dan de aquarel eromheen.
- Op mobiel is de kreeft in de strip klein (ongeveer 25 tot 30 px hoog). Hij past, maar is een detail.
- Match, Geen matches en Lobsy-hulp delen één strip; Bewaard en Bewaard leeg ook; Sollicitaties en Sollicitaties leeg ook.
- De lege staten (bewaard, sollicitaties, matches) en de foutbeelden hebben zelf geen kreeft in de scène; 404 en foutpagina tonen de losse uitsnede ernaast.
- Paspoort, match/vacature, bewaard en de lege staten hadden geen HTML-bron; die zijn vereenvoudigd nagebouwd in dezelfde stijl.
- De veel-vraag-pagina's, de bedrijvenkaart en de banenkaart noemen Haaglanden, Den Haag en Naaldwijk; de kandidaat woont in Delft. Zelfde regio, niet overal gelijkgetrokken.
- De goedgekeurde bestaande layouts (src/bestaand) houden hun eigen teksten; daar is alleen de hulpknop, de notitie en de strip vervangen.
- In de desktop-pdf staan lange schermen (Profiel, Instellingen) verkleind op één pagina; de walkthrough en de renders tonen ze op ware grootte.
- EN staat in het copydeck, niet in de schermen zelf.
''')
open('README.md','w').write('\n'.join(L)); print('ok')
