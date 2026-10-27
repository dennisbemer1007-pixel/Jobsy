# Lobsy kandidaatreis · jij bent de kreeft

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

| # | Hoofdstuk | Scherm | Desktop | Mobiel |
|---|---|---|---|---|
| 01 | Start en inloggen | Landingspagina (uitgelogd), moment 0 | `renders/desktop/01-landing-desktop-1440.png` | `renders/mobile/01-landing-mobiel-390.png` |
| 02 | Start en inloggen | Inloggen en account maken | `renders/desktop/02-inloggen-desktop-1440.png` | `renders/mobile/02-inloggen-mobiel-390.png` |
| 03 | Onboarding | Strand · Start | `renders/desktop/03-start-strand-desktop-1440.png` | `renders/mobile/03-start-strand-mobiel-390.png` |
| 04 | Onboarding | Kust · Stap 1 · Even kennismaken | `renders/desktop/04-kust-kennismaken-desktop-1440.png` | `renders/mobile/04-kust-kennismaken-mobiel-390.png` |
| 05 | Onboarding | Kust · Stap 2 · Wanneer kun je werken? | `renders/desktop/05-kust-wanneer-desktop-1440.png` | `renders/mobile/05-kust-wanneer-mobiel-390.png` |
| 06 | Onboarding | Kust · Stap 3 · Hoe kom je op je werk? | `renders/desktop/06-kust-vervoer-desktop-1440.png` | `renders/mobile/06-kust-vervoer-mobiel-390.png` |
| 07 | Onboarding | Rotsen · Stap 4 · Heb je al gewerkt? | `renders/desktop/07-rotsen-werk-desktop-1440.png` | `renders/mobile/07-rotsen-werk-mobiel-390.png` |
| 08 | Onboarding | Rotsen · Stap 5 · Wat is je opleiding? | `renders/desktop/08-rotsen-leren-desktop-1440.png` | `renders/mobile/08-rotsen-leren-mobiel-390.png` |
| 09 | Onboarding | Rotsen · Stap 6 · Wat is je droombaan? | `renders/desktop/09-rotsen-droom-desktop-1440.png` | `renders/mobile/09-rotsen-droom-mobiel-390.png` |
| 10 | Tests | Diepte · Test 1 · Hoe werk jij? | `renders/desktop/10-diepte-hoe-werk-jij-desktop-1440.png` | `renders/mobile/10-diepte-hoe-werk-jij-mobiel-390.png` |
| 11 | Tests | Diepte · Test 2 · Wat vind je leuk? | `renders/desktop/11-diepte-leuk-desktop-1440.png` | `renders/mobile/11-diepte-leuk-mobiel-390.png` |
| 12 | Tests | Diepte · Even rusten | `renders/desktop/12-diepte-pauze-desktop-1440.png` | `renders/mobile/12-diepte-pauze-mobiel-390.png` |
| 13 | Tests | Diepte · Test 3 · Waar voel je je thuis? | `renders/desktop/13-diepte-thuis-desktop-1440.png` | `renders/mobile/13-diepte-thuis-mobiel-390.png` |
| 14 | Tests | Diepte · Test 4 · Wat vind je belangrijk? | `renders/desktop/14-diepte-waarden-desktop-1440.png` | `renders/mobile/14-diepte-waarden-mobiel-390.png` |
| 15 | Resultaat | Licht · Jouw Kompas | `renders/desktop/15-licht-kompas-desktop-1440.png` | `renders/mobile/15-licht-kompas-mobiel-390.png` |
| 16 | Resultaat | Licht · Paspoort | `renders/desktop/16-paspoort-desktop-1440.png` | `renders/mobile/16-paspoort-mobiel-390.png` |
| 17 | Mijn Lobsy | Mijn Lobsy | `renders/desktop/17-mijn-lobsy-desktop-1440.png` | `renders/mobile/17-mijn-lobsy-mobiel-390.png` |
| 18 | Match en vacatures | Match: vacature | `renders/desktop/18-match-vacature-desktop-1440.png` | `renders/mobile/18-match-vacature-mobiel-390.png` |
| 19 | Banenkaart | Banenkaart | `renders/desktop/19-banenkaart-desktop-1440.png` | `renders/mobile/19-banenkaart-mobiel-390.png` |
| 20 | Bedrijvenkaart | Bedrijvenkaart | `renders/desktop/20-bedrijvenkaart-desktop-1440.png` | `renders/mobile/20-bedrijvenkaart-mobiel-390.png` |
| 21 | Bewaard | Bewaard | `renders/desktop/21-bewaard-desktop-1440.png` | `renders/mobile/21-bewaard-mobiel-390.png` |
| 22 | Bewaard | Bewaard · leeg | `renders/desktop/22-bewaard-leeg-desktop-1440.png` | `renders/mobile/22-bewaard-leeg-mobiel-390.png` |
| 23 | Sollicitaties | Sollicitaties | `renders/desktop/23-sollicitaties-desktop-1440.png` | `renders/mobile/23-sollicitaties-mobiel-390.png` |
| 24 | Sollicitaties | Sollicitaties · leeg | `renders/desktop/24-sollicitaties-leeg-desktop-1440.png` | `renders/mobile/24-sollicitaties-leeg-mobiel-390.png` |
| 25 | Sollicitaties | Sollicitatie toevoegen | `renders/desktop/25-sollicitatie-toevoegen-desktop-1440.png` | `renders/mobile/25-sollicitatie-toevoegen-mobiel-390.png` |
| 26 | UWV: Mijn activiteiten | Mijn activiteiten (UWV) | `renders/desktop/26-mijn-activiteiten-desktop-1440.png` | `renders/mobile/26-mijn-activiteiten-mobiel-390.png` |
| 27 | UWV: Mijn activiteiten | UWV-overzicht (pdf) | `renders/desktop/27-uwv-overzicht-desktop-1440.png` | `renders/mobile/27-uwv-overzicht-mobiel-390.png` |
| 28 | Na de match | Na de match: wat nu | `renders/desktop/28-na-de-match-wat-nu-desktop-1440.png` | `renders/mobile/28-na-de-match-wat-nu-mobiel-390.png` |
| 29 | Na de match | Na de match: moment kiezen | `renders/desktop/29-na-de-match-moment-desktop-1440.png` | `renders/mobile/29-na-de-match-moment-mobiel-390.png` |
| 30 | Na de match | Berichten | `renders/desktop/30-berichten-desktop-1440.png` | `renders/mobile/30-berichten-mobiel-390.png` |
| 31 | Na de match | Na de match: nu niet | `renders/desktop/31-na-de-match-nu-niet-desktop-1440.png` | `renders/mobile/31-na-de-match-nu-niet-mobiel-390.png` |
| 32 | Carrière en omscholen | Carrière | `renders/desktop/32-carriere-desktop-1440.png` | `renders/mobile/32-carriere-mobiel-390.png` |
| 33 | Carrière en omscholen | Omscholen | `renders/desktop/33-omscholen-desktop-1440.png` | `renders/mobile/33-omscholen-mobiel-390.png` |
| 34 | Wat drijft jou | Wat drijft jou | `renders/desktop/34-wat-drijft-jou-desktop-1440.png` | `renders/mobile/34-wat-drijft-jou-mobiel-390.png` |
| 35 | Ondernemerscheck | Ondernemerscheck | `renders/desktop/35-ondernemerscheck-desktop-1440.png` | `renders/mobile/35-ondernemerscheck-mobiel-390.png` |
| 36 | Adviseur | Adviseur | `renders/desktop/36-adviseur-desktop-1440.png` | `renders/mobile/36-adviseur-mobiel-390.png` |
| 37 | Profiel en instellingen | Profiel | `renders/desktop/37-profiel-desktop-1440.png` | `renders/mobile/37-profiel-mobiel-390.png` |
| 38 | Profiel en instellingen | Instellingen | `renders/desktop/38-instellingen-desktop-1440.png` | `renders/mobile/38-instellingen-mobiel-390.png` |
| 39 | Lege staten en hulp | Geen matches | `renders/desktop/39-matches-leeg-desktop-1440.png` | `renders/mobile/39-matches-leeg-mobiel-390.png` |
| 40 | Lege staten en hulp | Lobsy-hulp (paneel open) | `renders/desktop/40-lobsy-hulp-desktop-1440.png` | `renders/mobile/40-lobsy-hulp-mobiel-390.png` |
| 41 | Lege staten en hulp | 404: pagina niet gevonden | `renders/desktop/41-404-desktop-1440.png` | `renders/mobile/41-404-mobiel-390.png` |
| 42 | Lege staten en hulp | Foutpagina | `renders/desktop/42-fout-desktop-1440.png` | `renders/mobile/42-fout-mobiel-390.png` |

## Mapping: pagina, scène, schildlaag en licht

| # | Scherm | Scène | Kreeft (jij) | Schild | Licht |
|---|---|---|---|---|---|
| 01 | Landingspagina (uitgelogd), moment 0 | Strand bij zonsopgang, het pad loopt over de duinen naar de vuurtoren in de verte. | In profiel, nog met zeepokken op zijn schild, loopt naar rechts het duinpad op richting de vuurtoren. | 10 lagen, heel schild | dageraad, roze en koel |
| 02 | Inloggen en account maken | Strandpaviljoen met open deur, lamp en kapstok. | Driekwart van achteren, op weg naar de open deur. | 10 lagen | dageraad |
| 03 | Strand · Start | De routekaart: strand, kust, rotsen, diepte en vuurtoren. | Heel klein op het strand, aan het begin van het pad, van achteren. | 10 lagen | dageraad |
| 04 | Kust · Stap 1 · Even kennismaken | Dorpsstraat aan de kust met verschillende voordeuren. | Profiel, loopt naar rechts langs de deuren. | laag 1 laat los | vroege ochtend |
| 05 | Kust · Stap 2 · Wanneer kun je werken? | Getijdenpaal en strandhuisjes onder een dagboog. | Profiel, loopt naar rechts langs de getijdenpaal. | laag 2 laat los | ochtend |
| 06 | Kust · Stap 3 · Hoe kom je op je werk? | Duinen met fiets, bus, auto, veerboot en trein. | Profiel, loopt naar rechts over het duinpad. | laag 3 laat los | ochtend |
| 07 | Rotsen · Stap 4 · Heb je al gewerkt? | Schuurtje met net, boot en kruiwagen tussen de rotsen. | Profiel, stapt naar rechts over de rotsen langs het schuurtje. | laag 4 laat los | helder daglicht |
| 08 | Rotsen · Stap 5 · Wat is je opleiding? | Stenen schooltje in de rotsen met boeken, lei en sextant. | Klimt naar rechtsboven tegen de stapel boeken. | laag 5 laat los | helder daglicht |
| 09 | Rotsen · Stap 6 · Wat is je droombaan? | Bankje met verrekijker op een klif boven zee; boven zee stijgen zes droomwolkjes op met dezelfde kreeft als piloot in een propellervliegtuigje, schilder bij een ezel, dokter in witte jas, bakker met brood, tuinier met gieter en bouwvakker met kruiwagen. | Van achteren, naast (niet achter) de verrekijker, schaar boven de ogen, kijkt naar rechts over zee; kleine belletjes lopen van zijn hoofd naar de wolkjes. | laag 6 laat los | helder, weids |
| 10 | Diepte · Test 1 · Hoe werk jij? | School vissen, een paar samen, één alleen. | Zwemt de diepte in, naar rechts, schuin omlaag. | laag 7 laat los | blauw onder water |
| 11 | Diepte · Test 2 · Wat vind je leuk? | Rif waar elk dier iets anders doet. | Zwemt naar rechts langs het rif. | laag 8 laat los | blauw |
| 12 | Diepte · Even rusten | Stille grot met een steen op de bodem. | Zit op de steen, zijaanzicht, rust uit. | 8 lagen los (pauze) | blauw, gedempt |
| 13 | Diepte · Test 3 · Waar voel je je thuis? | Grot, druk rif en open zeegrasveld naast elkaar. | Zwemt naar rechts tussen de leefplekken. | laag 9 laat los | blauw |
| 14 | Diepte · Test 4 · Wat vind je belangrijk? | Oesters met parels, anker en kompas op de zeebodem. | Duikt omlaag naar de parel, naar rechts. | laag 10 laat los | blauw, iets lichter |
| 15 | Licht · Jouw Kompas | Vuurtoren met lichtbundel in gouden avondlicht. | Driekwart van achteren bij de vuurtoren, één kompas in de schaar, oude schildstukjes bij zijn poten. | nieuw schild | goud |
| 16 | Licht · Paspoort | Tafel aan de haven met een leren paspoort en stempel. | Profiel, loopt met zijn post naar rechts, weg van de tafel, de haven in. | nieuw schild | goud, warm |
| 17 | Mijn Lobsy | Havenkantoortje aan de promenade met boten. | Profiel, loopt naar het havenkantoortje. | nieuw schild | haven, daglicht |
| 18 | Match: vacature | Zeilbootje dat in een passende ligplaats aanlegt. | Profiel op de steiger, loopt naar de boot. | nieuw schild | haven |
| 19 | Banenkaart | Polder met dorp, kassen, loods en zorgcentrum. | Profiel, staat aan de rand en kijkt omlaag op zijn kaart. | nieuw schild | haven |
| 20 | Bedrijvenkaart | Winkelstraat met bakkerij, fietsenmaker, garage en bloemist. | Profiel, loopt door de straat langs de winkels. | nieuw schild | haven |
| 21 | Bewaard | Plank in een strandhuisje met verzamelde schelpen. | Heel klein op de plank, loopt langs de schelpen. | nieuw schild | haven |
| 22 | Bewaard · leeg | Zelfde strip als Bewaard; leeg bewaardoosje met één schelp. | In de strip langs de plank; in de lege staat geen kreeft. | nieuw schild | haven |
| 23 | Sollicitaties | Postkantoortje aan de kade met een postboot vol enveloppen. | Profiel, loopt met zijn brieven naar het postkantoor. | nieuw schild | haven |
| 24 | Sollicitaties · leeg | Zelfde strip als Sollicitaties; brievenbus op de steiger zonder post. | In de strip op weg naar het postkantoor; in de lege staat geen kreeft. | nieuw schild | haven |
| 25 | Sollicitatie toevoegen | Stapel brieven op een tafel aan de kade. | Profiel, loopt over de tafel naar de stapel. | nieuw schild | haven |
| 26 | Mijn activiteiten (UWV) | Havenmeestersbureau met open logboek, kalender en stempel. | Zijaanzicht, kijkt omlaag in zijn papier naast het logboek. | nieuw schild | haven |
| 27 | UWV-overzicht (pdf) | Geen (formeel document). | Geen. |  | geen |
| 28 | Na de match: wat nu | Haventerras met een tafeltje voor twee en twee kopjes. | Profiel, loopt naar het tafeltje. | nieuw schild | haven, middag |
| 29 | Na de match: moment kiezen | Klokkentorentje aan de kade en een bord met lege kaartjes. | Van achteren op de kade bij de klokkentoren, kijkt naar het bord. | nieuw schild | haven |
| 30 | Berichten | Twee vuurtorens aan weerszijden van de haveningang die elkaar lichtsignalen geven. | Van achteren bij de vuurtoren, kijkt naar het licht aan de overkant. | nieuw schild | haven, avond |
| 31 | Na de match: nu niet | Haven bij avondlicht waar bootjes terugkomen. | Zit op een paal en kijkt uit over de haven. | nieuw schild | avond, zacht |
| 32 | Carrière | Kustpad dat langs wegwijzers omhoog slingert naar de vuurtoren. | Profiel, loopt het pad op. | nieuw schild | haven |
| 33 | Omscholen | Botenwerf waar een oude boot een nieuw zeil krijgt. | Profiel, kijkt omlaag op zijn plan bij de werf. | nieuw schild | haven |
| 34 | Wat drijft jou | Zeilbootje met wind in het zeil en een windzak. | Van achteren, kijkt naar de wind en het zeil. | nieuw schild | haven |
| 35 | Ondernemerscheck | Kraampje aan de kade dat voor het eerst opengaat. | Profiel, loopt naar het kraampje. | nieuw schild | haven |
| 36 | Adviseur | Twee fauteuils bij een patrijspoort met thee. | Zit in de fauteuil, zijaanzicht. | nieuw schild | haven, binnen |
| 37 | Profiel | Opgeruimde gang met jas, sjaal, laarzen en een deur met naambordje. | Profiel, loopt naar de deur met je naam. | nieuw schild | binnen |
| 38 | Instellingen | Werkplaatsje in de vuurtoren met wijzers en hendels. | Driekwart, staat bij het bord met wijzers. | nieuw schild | binnen |
| 39 | Geen matches | Zelfde strip als de match; vuurtoren en brandende lantaarn als lege staat. | In de strip op de steiger; in de lege staat geen kreeft. | nieuw schild | haven |
| 40 | Lobsy-hulp (paneel open) | Zelfde strip als de match; paneel met het Lobsy-merkteken. | In de strip op de steiger; de hulp heeft geen kreeft. | nieuw schild | haven |
| 41 | 404: pagina niet gevonden | Duinpad met een lege wegwijzer. | Zijaanzicht, kijkt omlaag op zijn kaart. | nieuw schild | haven |
| 42 | Foutpagina | Steiger die wordt gerepareerd met een gereedschapskist. | Zit op een paal en wacht rustig. | nieuw schild | haven |

## Controletabel

| # | Scherm | Illustratie | Scène past bij stap omdat... |
|---|---|---|---|
| 01 | Landingspagina (uitgelogd), moment 0 | `landing.jpg` | moment 0: je staat aan het begin en ziet waar de reis heen gaat, nog voor je iets invult. |
| 02 | Inloggen en account maken | `inloggen.jpg` | inloggen is binnenstappen: de deur staat open en je wordt verwacht. |
| 03 | Strand · Start | `route-kaart.jpg` | je ziet de hele route en jezelf op het startpunt, voordat je toestemming geeft. |
| 04 | Kust · Stap 1 · Even kennismaken | `kust-kennismaken.jpg` | kennismaken gaat over wie je bent en waar je woont: je loopt door je eigen straat. |
| 05 | Kust · Stap 2 · Wanneer kun je werken? | `kust-wanneer.jpg` | de vraag gaat over tijd en ritme: eb en vloed, ochtend en avond. |
| 06 | Kust · Stap 3 · Hoe kom je op je werk? | `kust-vervoer.jpg` | de vraag gaat over hoe je ergens komt; je bent zelf onderweg tussen alle vervoer. |
| 07 | Rotsen · Stap 4 · Heb je al gewerkt? | `rotsen-werk.jpg` | alle soorten werk tellen mee, ook klein en informeel; je loopt langs het gereedschap. |
| 08 | Rotsen · Stap 5 · Wat is je opleiding? | `rotsen-leren.jpg` | leren is klimmen: op school en onderweg. |
| 09 | Rotsen · Stap 6 · Wat is je droombaan? | `rotsen-droom.jpg` | bij de droombaan stel je je voor wat je zou kunnen worden: veel verschillende banen, van piloot en dokter tot bakker en bouwvakker, allemaal even waardevol, en jij bent het in elk wolkje. |
| 10 | Diepte · Test 1 · Hoe werk jij? | `diepte-werken.jpg` | de test vraagt hoe je werkt, samen of alleen; je duikt in jezelf. |
| 11 | Diepte · Test 2 · Wat vind je leuk? | `diepte-leuk.jpg` | interesses verschillen en zijn allemaal waardevol; je kijkt rond wat jou trekt. |
| 12 | Diepte · Even rusten | `diepte-pauze.jpg` | halverwege de tests even op adem komen. |
| 13 | Diepte · Test 3 · Waar voel je je thuis? | `diepte-thuis.jpg` | je kiest waar je je thuis voelt; je verkent de drie plekken. |
| 14 | Diepte · Test 4 · Wat vind je belangrijk? | `diepte-waarden.jpg` | je kiest wat waardevol is: de parels, het anker als houvast, het kompas als koers. |
| 15 | Licht · Jouw Kompas | `licht-kompas.jpg` | je bent aangekomen bij het licht en ziet je Kompas: je bent uit je schild. |
| 16 | Licht · Paspoort | `licht-paspoort` | je paspoort is klaar voor je volgende reis: werk zoeken in de haven. |
| 17 | Mijn Lobsy | `mijn-lobsy.jpg` | Mijn Lobsy is je thuishaven, waar je steeds terugkomt. |
| 18 | Match: vacature | `match.jpg` | een match is een boot die past in jouw ligplaats. |
| 19 | Banenkaart | `banenkaart.jpg` | op de banenkaart lees je zelf de kaart van werk om je heen. |
| 20 | Bedrijvenkaart | `bedrijvenkaart.jpg` | bedrijven in je buurt: je loopt er zelf langs. |
| 21 | Bewaard | `bewaard.jpg` | bewaren is verzamelen wat je wilt onthouden. |
| 22 | Bewaard · leeg | `leeg-bewaard.jpg` | de plank staat klaar, er is nog ruimte. |
| 23 | Sollicitaties | `sollicitaties.jpg` | je sollicitaties zijn brieven die onderweg zijn. |
| 24 | Sollicitaties · leeg | `leeg-sollicitaties.jpg` | nog niets verstuurd, maar de bus staat klaar. |
| 25 | Sollicitatie toevoegen | `toevoegen.jpg` | je legt zelf een sollicitatie bij de stapel. |
| 26 | Mijn activiteiten (UWV) | `activiteiten.jpg` | je houdt per week bij wat je deed, zoals een logboek. |
| 27 | UWV-overzicht (pdf) | `geen (document)` | dit is het formele UWV-document; zakelijk en leesbaar, in de goedgekeurde bewoording. |
| 28 | Na de match: wat nu | `na-de-match.jpg` | na de match volgt het eerste gesprek; de tafel staat klaar. |
| 29 | Na de match: moment kiezen | `moment.jpg` | je kiest een moment dat past. |
| 30 | Berichten | `berichten.jpg` | berichten gaan heen en weer, zoals lichtsignalen. |
| 31 | Na de match: nu niet | `nu-niet.jpg` | dit keer niet: even rusten, morgen weer uitvaren. |
| 32 | Carrière | `carriere.jpg` | je carrièreroute loop je stap voor stap. |
| 33 | Omscholen | `omscholen.jpg` | omscholen is dezelfde boot met een nieuwe koers. |
| 34 | Wat drijft jou | `wat-drijft.jpg` | wat jou drijft is je eigen wind. |
| 35 | Ondernemerscheck | `ondernemer.jpg` | je eigen zaak beginnen: je loopt er zelf naartoe. |
| 36 | Adviseur | `adviseur.jpg` | een persoonlijk gesprek met je adviseur; je zit er al klaar voor. |
| 37 | Profiel | `profiel.jpg` | je eigen gegevens, netjes op hun plek. |
| 38 | Instellingen | `instellingen.jpg` | instellingen zijn de wijzers die je zelf afstelt. |
| 39 | Geen matches | `leeg-matches.jpg` | er is nog niets binnen, maar het licht staat aan. |
| 40 | Lobsy-hulp (paneel open) | `lobsy-merkteken.svg` | de kreeft ben jij, niet de helper; de hulp is neutraal. |
| 41 | 404: pagina niet gevonden | `fout-404.jpg + kreeft-kaart` | even niet weten waar je bent, en zelf de weg terug zoeken. |
| 42 | Foutpagina | `fout-algemeen.jpg + kreeft-wachten` | er is iets kapot en er wordt aan gewerkt; jij wacht even. |

## Copydeck NL en EN

Titels, intro's, tips, reiskaartzinnen, notities en zonezinnen. Overige UI-tekst in de mockups is alleen NL.

| # | Soort | NL | EN |
|---|---|---|---|
| 01 | titel | Werk dat bij je past. Rustig gevonden. | Work that suits you. Found calmly. |
| 01 | moment 0 | Dit is jouw reis. Stap voor stap kom je uit je schild. | This is your journey. Step by step you come out of your shell. |
| 01 | intro | Je begint op het strand, nog helemaal in je schild. Je vertelt eerst wie je bent en wat je kunt, daarna pas kijk je naar vacatures. Voor iedereen, met of zonder diploma, jong of wat ouder, net begonnen of al jaren aan het werk. | You start on the beach, still fully in your shell. First you tell who you are and what you can do, only then do you look at vacancies. For everyone, with or without a diploma, young or a bit older, just starting or working for years. |
| 01 | knop | Stap op het strand | Step onto the beach |
| 01 | zone | Strand · Je begint: Je maakt een account en stapt op het strand. Je leest wat er met je gegevens gebeurt. Nog alle 10 lagen. | Strand: You create an account and step onto the beach. You read what happens with your data. All 10 layers still on. |
| 01 | zone | Kust · Over jou: Wanneer en waar je kunt werken, en hoe je er komt. Lagen 1 tot en met 3. | Kust: When and where you can work, and how you get there. Layers 1 to 3. |
| 01 | zone | Rotsen · Achtergrond en droom: Wat je al deed, wat je leerde en waar je heen wilt. Lagen 4 tot en met 6. | Rotsen: What you did, what you learned and where you want to go. Layers 4 to 6. |
| 01 | zone | Diepte · Wie ben jij: Vier korte tests, zonder goed of fout. Je kijkt naar binnen. Lagen 7 tot en met 10. | Diepte: Four short tests, no right or wrong. You look inward. Layers 7 to 10. |
| 01 | zone | Licht · Jouw Kompas: Je ziet wat bij je past. Je bent uit je schild en je paspoort is klaar. Nieuw schild. | Licht: You see what suits you. You are out of your shell and your passport is ready. New shell. |
| 01 | reiskaart | Nog 10 lagen schild. Bij elke stap laat er een los. In het Licht ben je uit je schild. | Still 10 layers of shell. With every step one comes off. In the Light you are out of your shell. |
| 02 | titel | Fijn dat je er bent | Good to have you here |
| 02 | intro | Log in met je e-mailadres. Nog geen account? Dat maak je in een minuut. | Log in with your email address. No account yet? You create one in a minute. |
| 02 | reiskaart | Nog 10 lagen schild. Bij elke stap laat er een los. In het Licht ben je uit je schild. | Still 10 layers of shell. With every step one comes off. In the Light you are out of your shell. |
| 02 | tip | Na het inloggen begint je reis op het strand. | After logging in your journey starts on the beach. |
| 03 | titel | Welkom bij je reis | Welcome to your journey |
| 03 | intro | Je loopt in vijf rustige stukken van het strand naar het licht. Onderweg leer je jezelf beter kennen. | You walk in five calm stretches from the beach to the light. Along the way you get to know yourself better. |
| 03 | zone Strand · Start | Hier begin je. Je leest wat er met je gegevens gebeurt. | This is where you start. You read what happens with your data. |
| 03 | zone Kust · Over jou | Een paar basisvragen: wanneer en waar je kunt werken. | A few basic questions: when and where you can work. |
| 03 | zone Rotsen · Achtergrond en Droom | Wat je al hebt gedaan en waar je naartoe wilt. | What you have done so far and where you want to go. |
| 03 | zone Diepte · Wie ben jij | Vier korte tests over hoe je werkt en wat je belangrijk vindt. | Four short tests about how you work and what matters to you. |
| 03 | zone Licht · Jouw Kompas | Je ziet wat bij je past en je paspoort is klaar. | You see what suits you and your passport is ready. |
| 03 | reiskaart | Nog 10 lagen schild. Bij elke stap laat er een los. In het Licht ben je uit je schild. | Still 10 layers of shell. With every step one comes off. In the Light you are out of your shell. |
| 03 | tip | Je oude schild zit wat krap. Onderweg ontdek je wie eronder zit. | Your old shell is a bit tight. Along the way you find out who is underneath. |
| 04 | titel | Even kennismaken | Let's get acquainted |
| 04 | intro | Eerst wat basisgegevens. | First some basic details. |
| 04 | reiskaart | Nog 10 lagen schild. Na deze stap laat laag 1 los. | Still 10 layers of shell. After this step layer 1 comes off. |
| 04 | tip | Je begint rustig: alleen je naam en waar je woont. | You start calmly: just your name and where you live. |
| 05 | titel | Wanneer kun je werken? | When can you work? |
| 05 | intro | Kies wat nu bij je leven past. Je kunt het later altijd aanpassen. | Choose what fits your life now. You can always change it later. |
| 05 | reiskaart | Nog 9 lagen schild. Na deze stap laat laag 2 los. | Still 9 layers of shell. After this step layer 2 comes off. |
| 05 | tip | Er is geen goed of fout. Kies wat nu bij je leven past. | There is no right or wrong. Choose what fits your life now. |
| 06 | titel | Hoe kom je op je werk? | How do you get to work? |
| 06 | intro | Dan zie je werk binnen een afstand die voor jou haalbaar is. | Then you see work within a distance that works for you. |
| 06 | reiskaart | Nog 8 lagen schild. Na deze stap laat laag 3 los. | Still 8 layers of shell. After this step layer 3 comes off. |
| 06 | tip | Kies hoe ver je wilt reizen. Je ziet dan alleen werk binnen die afstand. | Choose how far you want to travel. You then only see work within that distance. |
| 07 | titel | Heb je al gewerkt? | Have you worked before? |
| 07 | intro | Bijbaan, vrijwilligerswerk of stage telt ook mee. | A side job, volunteering or an internship also counts. |
| 07 | reiskaart | Nog 7 lagen schild. Na deze stap laat laag 4 los. | Still 7 layers of shell. After this step layer 4 comes off. |
| 07 | tip | Alles telt mee. Ook vrijwilligerswerk en zorgen voor anderen. | Everything counts. Volunteering and caring for others too. |
| 08 | titel | Wat is je opleiding? | What is your education? |
| 08 | intro | Ook cursussen en dingen die je zelf hebt geleerd tellen mee. | Courses and things you taught yourself count too. |
| 08 | reiskaart | Nog 6 lagen schild. Na deze stap laat laag 5 los. | Still 6 layers of shell. After this step layer 5 comes off. |
| 08 | tip | Een diploma is fijn, maar wat je kunt telt net zo zwaar. | A diploma is nice, but what you can do counts just as much. |
| 09 | titel | Wat is je droombaan? | What is your dream job? |
| 09 | intro | Denk groot. In Carrière zie je later een route ernaartoe. | Think big. In Career you will later see a route to it. |
| 09 | reiskaart | Nog 5 lagen schild. Na deze stap laat laag 6 los. | Still 5 layers of shell. After this step layer 6 comes off. |
| 09 | tip | Denk gerust groot. De weg ernaartoe zoek je later stap voor stap uit. | Feel free to dream big. You work out the way there later, step by step. |
| 10 | titel | Hoe werk jij? | How do you work? |
| 10 | intro | Kies bij elke zin wat het meest op je lijkt. | For each sentence, choose what is most like you. |
| 10 | reiskaart | Nog 4 lagen schild. Na deze stap laat laag 7 los. | Still 4 layers of shell. After this step layer 7 comes off. |
| 10 | tip | Kies wat het meest op je lijkt. Je eerste gevoel is meestal goed. | Choose what is most like you. Your first feeling is usually right. |
| 11 | titel | Wat vind je leuk? | What do you like? |
| 11 | intro | Kies de dingen die je graag doet. Meerdere mag. | Choose the things you enjoy doing. More than one is fine. |
| 11 | reiskaart | Nog 3 lagen schild. Na deze stap laat laag 8 los. | Still 3 layers of shell. After this step layer 8 comes off. |
| 11 | tip | Denk aan dagen waarop de tijd voorbij vloog. | Think of days when time flew by. |
| 12 | titel | Even rusten | A short rest |
| 12 | reiskaart | Nog 2 lagen schild. Er zijn er al 8 los. | Still 2 layers of shell. 8 have already come off. |
| 12 | tip | Je bent halverwege de diepte. Ga verder wanneer je wilt. | You are halfway through the deep. Continue whenever you like. |
| 13 | titel | Waar voel je je thuis? | Where do you feel at home? |
| 13 | intro | Kies de werkplek die het best bij je past. | Choose the workplace that suits you best. |
| 13 | reiskaart | Nog 2 lagen schild. Na deze stap laat laag 9 los. | Still 2 layers of shell. After this step layer 9 comes off. |
| 13 | tip | Sommige mensen houden van drukte, anderen van rust. Beide is goed. | Some people like a busy place, others like quiet. Both are fine. |
| 14 | titel | Wat vind je belangrijk? | What matters to you? |
| 14 | intro | Kies er drie. Wat moet je werk je echt geven? | Choose three. What should your work really give you? |
| 14 | reiskaart | Nog 1 laag schild. Na deze stap laat laag 10 los. | Still 1 layer of shell. After this step layer 10 comes off. |
| 14 | tip | Laatste test. Kies wat je echt belangrijk vindt, niet wat hoort. | Last test. Choose what really matters to you, not what you think you should. |
| 15 | titel | Jouw Kompas | Your Compass |
| 15 | reiskaart | Je bent uit je schild. Alle tien lagen zijn los en je nieuwe schild zit goed. | You are out of your shell. All ten layers are off and your new shell fits well. |
| 15 | tip | Kijk hoe ver je gekomen bent. Dit is je nieuwe schild. | Look how far you have come. This is your new shell. |
| 16 | titel | Je paspoort | Your passport |
| 16 | reiskaart | Je bent uit je schild. Alle tien lagen zijn los en je nieuwe schild zit goed. | You are out of your shell. All ten layers are off and your new shell fits well. |
| 16 | tip | Je paspoort is klaar. Werkgevers zien wie je bent, niet alleen je cv. | Your passport is ready. Employers see who you are, not just your CV. |
| 17 | notitie | Welkom terug. Kijk rustig wat er voor je klaarstaat. | Welcome back. Take your time to see what is waiting for you. |
| 18 | titel | Medewerker thuiszorg | Home care worker |
| 18 | notitie | Deze vacature past bij wat je in je reis vertelde. Lees rustig verder. | This vacancy fits what you shared on your journey. Read on at your own pace. |
| 21 | titel | Bewaard | Saved |
| 22 | titel | Nog niets bewaard | Nothing saved yet |
| 22 | notitie | Neem je tijd. Wat je mooi vindt, bewaar je hier. | Take your time. What you like, you keep here. |
| 24 | titel | Nog geen sollicitaties | No applications yet |
| 24 | notitie | Dat komt nog. Begin bij een vacature die je aanspreekt. | That will come. Start with a vacancy that appeals to you. |
| 26 | notitie | Je hoeft niets te onthouden. Je activiteiten staan per week voor je klaar. | You do not have to remember anything. Your activities are ready for you per week. |
| 28 | notitie | Mooi nieuws. Hieronder lees je stap voor stap wat er nu gebeurt. | Good news. Below you read step by step what happens now. |
| 29 | notitie | Kies een moment dat bij je past. Verzetten kan altijd. | Choose a moment that suits you. You can always reschedule. |
| 31 | notitie | Dit keer niet. Dat zegt niets over wie je bent. Morgen kijk je verder. | Not this time. It says nothing about who you are. Tomorrow you look further. |
| 32 | notitie | Je droom is een richting. Je zet er kleine stappen naartoe. | Your dream is a direction. You take small steps toward it. |
| 33 | notitie | Wat je al kunt neem je mee. Je begint niet opnieuw. | You take what you can already do with you. You are not starting over. |
| 34 | notitie | Wat geeft je energie? Daar zit vaak je richting. | What gives you energy? That is often where your direction is. |
| 35 | notitie | Zelf iets beginnen? Met deze check zie je of het bij je past. | Starting something yourself? This check shows you whether it suits you. |
| 36 | notitie | Soms helpt een mens om mee te praten. Je adviseur denkt met je mee. | Sometimes it helps to talk to a person. Your advisor thinks along with you. |
| 39 | titel | Nog geen matches | No matches yet |
| 39 | notitie | Maak je reis af, dan zie je meer vacatures. Het licht staat al aan. | Finish your journey and you will see more vacancies. The light is already on. |
| 40 | titel | Lobsy-hulp | Lobsy help |
| 40 | paneel | Hulp bij de app en je reis | Help with the app and your journey |
| 40 | hulp | Hoi Sam. Waar wil je hulp bij? | Hi Sam. What would you like help with? |
| 40 | hulp | Dat betekent dat werkgevers bij jou in de buurt veel mensen zoeken voor dit werk. Je maakt daar vaak sneller kans op een gesprek. | It means employers near you are looking for many people for this work. You often get an interview sooner there. |
| 40 | paneel | Lobsy-hulp is een digitale hulp. Voor persoonlijk advies kun je altijd je adviseur vragen. | Lobsy help is a digital helper. For personal advice you can always ask your advisor. |
| 41 | titel | Deze pagina bestaat niet (meer) | This page does not exist (anymore) |
| 41 | notitie | Deze pagina is er niet. Met de knoppen hieronder vind je de weg terug. | This page is not here. The buttons below help you find your way back. |
| 42 | titel | Er ging iets mis | Something went wrong |
| 42 | notitie | Er wordt aan gewerkt. Probeer het zo nog eens. | It is being worked on. Please try again shortly. |

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
