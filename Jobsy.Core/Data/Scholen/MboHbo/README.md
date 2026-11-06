# Conceptvragenlijsten mbo/hbo – v2 (verwerkt besluiten Dennis 1 nov 2026)

Status: CONCEPT. Niets is ingebouwd of gecommit. v1 staat ongewijzigd in `../v1/`.
CSV: `;` als scheidingsteken, UTF-8 met BOM. Kolommen: nr, dimensie, vraag, voorbeeldscenario, antwoordopties, bron, b1_check (+ esco, esco_match bij de beroepentest).

| Test | Bestand | Vragen | Hergebruikt | Nieuw | B1 herschreven | Flesch-Douma |
|---|---|--:|--:|--:|--:|--:|
| 1a Vakkenpakket mbo | 1a-vakkenpakket-mbo.csv | 40 | 7 | 33 | 27* | 90 |
| 1b Vakkenpakket hbo | 1b-vakkenpakket-hbo.csv | 40 | 8 | 32 | 31* | 87 |
| 2 Interessetest (RIASEC) | 2-interessetest-riasec.csv | 48 | 25 | 23 | 11 | 92 |
| 3 Competentietest | 3-competentietest.csv | 40 | 20 | 20 | 4 | 96 |
| 4 Beroepentest | 4-beroepentest.csv | 50 | 0 | 50 | 2 | 78 |
| 5 Werkwaarden & werkcultuur | 5-werkwaarden-werkcultuur.csv | 41 | 33 | 8 | 12 | 93 |

*Bij de vakken komt bijna alles door de kortere vraagzin ("Het vak X." in plaats van "Het vak of vakgebied X.").
Een Flesch-Douma-score van 80 of hoger betekent "makkelijk" (de maat is benaderd: lettergrepen geteld met een klinkerregel). In het totaal scoort alles 90. De langste zin heeft 15 woorden ("Stel je voor:" telt apart).

## Verwerkte besluiten
1. **Beroepentest:** schaal Lijkt me niks / Lijkt me minder / Weet ik niet / Lijkt me leuk / Lijkt me heel leuk (1-5). Elk beroep is gekoppeld aan een ESCO-beroep uit `Jobsy.Core/Data/Occupations/occupations.nl.json` (NL-naam, URI en ISCO). `ilo_tasks_nl.json` bevat alleen ISCO-taken, geen ESCO-beroepen. 36 koppelingen zijn direct en 14 zijn een benadering (zie `esco_match`). Stratenmaker staat niet in de data en is vervangen door Tegelzetter.
2. **Vakkenpakket:** twee schalen (Hoe leuk? / Hoe goed ben ik erin?, 1-5, zoals G78). Er is een aparte lijst voor mbo (meer beroepsgericht: o.a. detailhandel, kinderopvang, afbouw) en voor hbo (meer theoretisch: o.a. statistiek, onderzoek, recht, AI).
3. **Competentie:** de 4 nieuwe dimensies zijn akkoord en ongewijzigd (Communiceren, Plannen & organiseren, Klantgerichtheid, Leren & ontwikkelen).
4. **Werkwaarden:** 8 nieuwe werkcontextvragen toegevoegd, telkens 2 over zekerheid, salaris, parttime en thuiswerken. Eén vraag is bewust de positieve tegenhanger van thuiswerken ("op een vaste plek samen met collega's").
5. **Eén schaal** (Klopt niet … Klopt helemaal) voor test 2, 3 en 5. Test 1 houdt leuk/goed en test 4 de "Lijkt me"-schaal.
6. **Geen controlevragen.** Wel dit signaal: als een leerling (bijna) overal hetzelfde antwoord geeft (bv. ≥90% zelfde keuze binnen een test), zie je dat alleen subtiel in de docentomgeving, bijvoorbeeld met een klein icoon "antwoordpatroon opvallend". De leerling krijgt geen melding en de score verandert niet.
7. **B1-check:** elke vraag en elk scenario is automatisch gecontroleerd op zinnen langer dan 15 woorden, passieve vorm en lange woorden, en daarna met de hand nagelezen. Herschreven vragen staan als `herschreven` in `b1_check`. Beroepsnamen en bekende samenstellingen (stagebegeleider, zonnepanelen) zijn bewust blijven staan.

## Resterende open punten
- 14 ESCO-koppelingen zijn een benadering (bv. jongerenwerker → coördinator jeugdwerk, contentmaker → vlogger, planner transport → taxiplanner). Akkoord, of andere beroepen kiezen?
- "Gastheer/gastvrouw" staat twee keer in ESCO. Er is nu één gekozen (a1b86a8b…).
- Drempel en vorm van het patroonsignaal voor de docent moeten nog worden bepaald (voorstel ≥90%).
- Test 5 heeft nu 41 vragen (mockup zegt 32). Mockuptekst aanpassen?
- De B1-check is automatisch plus mijn eigen controle. Een echte B1-toets met leerlingen of een taalspecialist is nog niet gedaan.
- De leesbaarheidsmaat is een benadering, geen officiële tool.
