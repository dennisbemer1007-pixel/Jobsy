# Puzzelpauzes ontdekkingsreis groep 7/8: ontwerpnotitie

*Mockups met voorbeelddata. Ontwerpvoorstel: er is nog geen code in de repo gezet. Stand: 2 okt 2026.*

## Waar en waarom
| # | Moment | Puzzel | Denkvaardigheid | Sterkte-zin (resultaat) | Knop daarna |
|---|---|---|---|---|---|
| 1 | na vraag 15 (einde Koraalrif) | **De schelpenrij** | patronen | "Jij ziet snel patronen." | Door naar de schatgrot |
| 2 | na vraag 30 (einde Schatgrot) | **De schatkaart** | ruimtelijk | "Jij houdt een route goed in je hoofd." | Naar het Pauze-eiland |
| 3 | na vraag 45 (einde Vuurtoren) | **De vuurtorenlampen** | logisch / stap voor stap | "Jij denkt stap voor stap na." | Door naar de lagune |

- **Een speelse pauze, geen toets.** Er is geen score, geen cijfer en geen timer. Een fout antwoord heeft geen gevolg. Op het scherm staat altijd "Geen tijd · geen cijfer · fout is niet erg". "Sla over" kan altijd.
- **Puzzel 2 leidt naar het Pauze-eiland.** De volgorde wordt dan: vraag 30 → puzzel → eiland → vuurtoren.
- **Voortgang.** De puzzels tellen niet mee in "x van 60". Het label is "Puzzel n van 3 · 30 van 60 klaar". In de reis-rail staan ze als eigen stappen ("Puzzel 1 · De schelpenrij").
- **Lobsy.** De kreeft blijft de gids met een korte zin per puzzel. De vraag staat in de tekstballon, zodat er weinig te lezen is.

## Fout of overgeslagen
- Er is nooit een rood kruis. Bij een fout antwoord staat de titel "Lekker gepuzzeld!" met een rustige "Zo zat het"-onthulling: het juiste vak of de juiste lamp licht groen op.
- Daarna volgt toch een positieve sterkte-zin, bijvoorbeeld "Jij durft iets nieuws te proberen." of "Jij zoekt rustig naar een oplossing."
- Overslaan geeft geen zin en gaat gewoon verder.

## Privacy en weergave
- **Wat we opslaan, per puzzel:** `status` (klaar/overgeslagen), `correct` (bool, alleen om de zin te kiezen), `templateVersion` en `level`. Geen tijd, geen pogingen, geen score.
- **Wie wat ziet:** de leerling ziet alleen de sterkte-zin. Die mag ook in "Dit ben jij" staan.
- De leerkracht ziet hooguit "3 van 3 puzzels gedaan" en nooit goed/fout. Werkgevers zien niets, ook niet in matching of profielen.
- De puzzels gaan niet mee in de interessematching en krijgen geen gewicht in de banenkaart.

## Variatie per leerling (seed)
- `seed = sha256("{templateVersion}:{puzzelKey}:{leerlingcode}")`. De eerste 8 bytes zijn de seed voor een deterministische PRNG.
- **Deterministisch:** dezelfde leerling krijgt bij herladen of op een ander apparaat exact dezelfde puzzel. Er is geen "nieuwe puzzel"-knop, dus geen herkansingsjacht. De server kan het antwoord ook zelf narekenen.
- **Ander kind, andere opdracht:** andere vormen, kleuren, posities en route, op hetzelfde niveau.
  - Het juiste antwoord wordt over de knoppen geschud, dus "het is de derde knop!" werkt niet.
  - Zie `*-varianten.png`: K7Q-M2P en B8N-3KD.
- **De sjabloonversie zit in de seed.** Als we een sjabloon aanpassen, krijgen nieuwe sessies nieuwe varianten. Een lopende sessie bewaart haar `templateVersion`.
- **Optioneel:** controleer per klas dat twee naaste codes niet toevallig dezelfde instantie krijgen. Zo ja, voeg een zout toe (`:1`). Bij 3 parameters per sjabloon is de kans klein.
- **Eerlijk niveau.** De generator varieert alleen de "oppervlakte": vorm, kleur, positie en richting. De structuur ligt per niveau vast: het soort patroon, het aantal stappen en het aantal lege vakken.

## Sjablonen en parameters

### 1. De schelpenrij (patronen)
- **Parameters:**
  - `family`: het patroon
  - `alphabet`: 2–4 symbolen; vorm × kleur, elke vorm en elke kleur uniek
  - `offset`: waar in de cyclus de rij start
  - `foils`: afleiders, altijd een andere vorm en een andere kleur
  - de volgorde van de 4 opties
- **Weergave:** 7 getoonde plekken + 1 vraagteken. Op mobiel zijn dat 2 rijen van 4, met een slingerende stippellijn als leesrichting. Op desktop is het 1 rij van 8.

| Niveau | Patroonfamilies |
|---|---|
| L1 | AB, AAB |
| L2 (standaard, mockup) | ABC, ABB, AABB |
| L3 | ABCB, ABCD, AABC |

### 2. De schatkaart (ruimtelijk)
- **Parameters:**
  - `start`: een vak in het 5×5-raster
  - `runs`: een lijst van (richting, aantal)
  - `decor`: 3 stenen/wier buiten de route, puur sfeer
- **Regels:**
  - De route blijft binnen het raster en komt nergens twee keer.
  - Het eindvak ligt minstens 2 stappen (Manhattan-afstand) van de start.
  - Op L1–L2 staan opeenvolgende runs haaks op elkaar, met een zichtbare spatie tussen de runs.
- **Resultaat:** de route als stippellijn en een schatkist in het eindvak.

| Niveau | Runs | Stappen totaal |
|---|---|---|
| L1 | 2 | 3–4 |
| L2 (mockup) | 3 | 5–7 |
| L3 | 4, elke richting toegestaan | 7–9 |

### 3. De vuurtorenlampen (logisch)
- **Opzet:** een 3×3 latijns vierkant van lampvormen in de ramen van de vuurtoren. Elke rij en elke kolom bevat elke lamp 1 keer. De regel staat als mini-pictogram bij het raster.
- **Parameters:**
  - `symbols`: 3 vormen × kleuren
  - een rij-, kolom- en symboolpermutatie van het basisvierkant
  - `gap`: welk raam leeg is
  - `foil`: een 4e vorm als afleider
  - de volgorde van de opties
- Geel valt af als kleur, omdat het te zwak is op een verlicht raam.

| Niveau | Raster | Lege ramen | Opties |
|---|---|---|---|
| L1 | 3×3 | 1 | 3 |
| L2 (mockup) | 3×3 | 1 | 4 (incl. afleider) |
| L3 | 4×4 | 3 | het gevraagde raam is alleen via rij + kolom samen op te lossen |

**Alternatief sjabloon: "De weegschaal".** Twee weegschalen met schelpen tonen bijvoorbeeld 1 zeester = 2 schelpen. De vraag is welke kant van een derde weegschaal zwaarder is: links, rechts of in evenwicht (3 grote knoppen met een weegschaal-icoon). De parameters zijn de gewichten per vorm, het aantal per schaal en de positie van het antwoord. Dit sjabloon is iets taliger en wiskundiger. Gebruik het als groep 8-variant of als wissel voor puzzel 3.

## Moeilijkheid
- Standaard krijgt iedereen L2. Er is geen adaptieve moeilijkheid binnen de reis, want dat voelt als een toets.
- Eventueel kan de school kiezen: groep 7 → L1/L2, groep 8 → L2/L3.
- **Doel: ±80% lost de puzzel op.** Bij een pilot meten we alleen het geaggregeerde percentage per sjabloon en niveau, nooit per kind zichtbaar.

## Toegankelijkheid
- **Kleurenblind-veilig.** De betekenis zit altijd in de vorm. Kleur is extra:
  - Het palet is Okabe-Ito, met donkerblauwe contouren om elke vorm.
  - Opties verschillen altijd in vorm én kleur.
  - Er is nooit een puzzel die je alleen op kleur kunt oplossen.
- **Tapdoelen ≥ 44 px.** Antwoordknoppen zijn 68 px hoog. De rastervakken zijn ongeveer 60 px op mobiel en op desktop (minimaal 44 px afgedwongen). "Sla over" en "Klaar" zijn ≥ 44 px. `build.py` controleert dit automatisch.
- **Geen timer** en geen aftellen. Pauze kan altijd en de stand wordt bewaard.
- **Weinig tekst.** De vraag is 1 zin in de ballon, met een voorleesknop zoals bij de gewone vragen.
- **Schermlezer:**
  - Elke vorm heeft een label, bijvoorbeeld "roze schelp" of "lichtblauwe parel".
  - Pijlgroepen heten bijvoorbeeld "3 keer omhoog".
  - Rastervakken heten bijvoorbeeld "Rij 2, vak 4".
  - Opties zijn een `radiogroup`.
- **Toetsenbord:** pijltjestoetsen in het raster en de opties, Enter of spatie om te kiezen.
- **Bewegen:** confetti en de schatkist-animatie staan uit bij `prefers-reduced-motion`.
- **Contrast:** tekst ≥ AA, en de lege plek is gemarkeerd met een stippelrand + "?", niet alleen met kleur.

## Bestanden
- `puzzels.py` bevat de generatoren (`gen_p1`/`gen_p2`/`gen_p3`, `rng`) en de schermen. `build.py` rendert de schermen en draait de checks. `boards.py` maakt de variant- en overzichtsborden.
- `html/` bevat de HTML-bronnen. De PNG's staan in deze map: mobiel @2x, desktop 1366×768.
