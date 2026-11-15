#!/usr/bin/env python3
"""Generate CONCEPT A/B question banks for Loopbaan-APK medewerkers (Basis + Uitgebreid)."""
from __future__ import annotations

import csv
import itertools
import random
import re
from collections import Counter
from dataclasses import dataclass
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DATA_DIR = ROOT / "Jobsy.Core" / "Data" / "WerkgeverApk"
DOCS_DIR = ROOT / "docs" / "werkgever-apk"

BRON = "werkgever-loopbaan-apk-concept"
KOP_AB = "Wat past het best bij jou?"
KOP_CHECK = "Hoe goed past deze zin bij jou?"
RNG = random.Random(20261115)

RIASEC = {
    "R": "R (Praktisch)",
    "I": "I (Onderzoekend)",
    "A": "A (Creatief)",
    "S": "S (Sociaal)",
    "E": "E (Ondernemend)",
    "C": "C (Geordend)",
}

RIASEC_PAIRS: list[tuple[str, str]] = [
    tuple(p)  # type: ignore[arg-type]
    for p in itertools.combinations("RIASEC", 2)
]

SCHWARTZ = {
    "autonomy": "Werkwaarde – Zelfstandigheid (Autonomy)",
    "connection": "Werkwaarde – Verbinding (Connection)",
    "achievement": "Werkwaarde – Prestatie (Achievement)",
    "stability": "Werkwaarde – Zekerheid (Stability)",
    "impact": "Werkwaarde – Betekenis (Impact)",
}

SCHWARTZ_PAIRS: list[tuple[str, str]] = [
    ("autonomy", "stability"),
    ("connection", "achievement"),
    ("achievement", "connection"),
    ("impact", "stability"),
    ("autonomy", "connection"),
    ("achievement", "stability"),
    ("impact", "achievement"),
    ("autonomy", "impact"),
    ("connection", "stability"),
    ("impact", "connection"),
]

COMPETENCES = [
    "Samenwerken",
    "Resultaatgerichtheid",
    "Stressbestendigheid",
    "Creativiteit / vernieuwen",
    "Contact maken",
    "Communiceren",
    "Plannen & organiseren",
    "Klantgerichtheid",
    "Leren & ontwikkelen",
    "Zelfstandig afronden",
    "Nauwkeurig werken",
    "Flexibel aanpassen",
]

BASIS_COMP_PLAN: list[tuple[int, int]] = [
    (4, 7),
    (2, 5),
    (1, 8),
    (0, 7),
    (2, 8),
    (0, 2),
    (0, 2),
    (0, 1),
    (7, 8),
    (5, 8),
    (1, 3),
    (6, 7),
    (3, 6),
    (1, 6),
    (4, 5),
    (5, 6),
    (3, 4),
    (3, 4),
]

COMP_PAIRS: list[tuple[int, int]] = [
    (0, 9),   # Samenwerken vs Zelfstandig afronden
    (10, 11), # Nauwkeurig vs Flexibel
    (1, 2),   # Resultaat vs Stress
    (6, 11),  # Plannen vs Flexibel
    (3, 1),   # Creativiteit vs Resultaat
    (0, 7),   # Samenwerken vs Klantgericht
    (4, 5),   # Contact vs Communiceren
    (8, 9),   # Leren vs Zelfstandig
    (2, 7),   # Stress vs Klantgericht
    (6, 10),  # Plannen vs Nauwkeurig
    (5, 7),   # Communiceren vs Klantgericht
    (1, 0),   # Resultaat vs Samenwerken
    (3, 4),   # Creativiteit vs Contact maken
    (3, 6),   # Creativiteit vs Plannen
    (3, 8),   # Creativiteit vs Leren
    (2, 3),   # Stress vs Creativiteit
    (0, 8),   # Samenwerken vs Leren
    (4, 6),   # Contact vs Plannen
    (1, 5),   # Resultaat vs Communiceren
    (2, 8),   # Stress vs Leren
]

SITUATIONS = [
    "Denk aan een gewone werkdag in het magazijn of op de vloer.",
    "Denk aan je werk in de productie of assemblage.",
    "Denk aan je dienst in de zorg of bij begeleiding.",
    "Denk aan een drukke dag in de winkel of horeca.",
    "Denk aan je werk op kantoor of achter de balie.",
    "Denk aan je werkdag op de bouw of in het onderhoud.",
    "Denk aan je ronde als chauffeur of bezorger.",
    "Denk aan je shift in een restaurant of hotel.",
]

# --- Item text templates (B1, concrete, ~15 words) ---

RIASEC_TEXT: dict[tuple[str, str], list[tuple[str, str]]] = {
    ("R", "I"): [
        ("Ik repareer graag machines of apparaten met gereedschap.", "Ik zoek eerst uit waarom een storing optreedt."),
        ("Ik til, sorteer of bouw liever dan lang puzzelen achter een scherm.", "Ik lees graag handleidingen om iets precies te begrijpen."),
        ("Ik werk het liefst buiten of op de werkvloer met mijn handen.", "Ik vergelijk graag gegevens om een oorzaak te vinden."),
    ],
    ("R", "A"): [
        ("Ik zet liever pallets klaar dan posters of teksten ontwerpen.", "Ik bedenk graag een nieuwe indeling of presentatie."),
        ("Ik monteer liever onderdelen volgens een duidelijk voorbeeld.", "Ik maak graag iets dat er ook mooi uitziet."),
        ("Ik werk het liefst met machines, gereedschap of zware spullen.", "Ik ontwerp graag een slimmere werkwijze of lay-out."),
    ],
    ("R", "S"): [
        ("Ik pak liever dozen of bouw iets op dan uren met klanten praten.", "Ik help graag een collega of bewoner die vastloopt."),
        ("Ik voel me thuis op de werkvloer met fysiek werk.", "Ik luister graag naar iemand die iets kwijt wil."),
        ("Ik laad liever een vrachtwagen dan een groep leiden in overleg.", "Ik zorg graag dat een klant zich gehoord voelt."),
    ],
    ("R", "E"): [
        ("Ik werk liever aan een vast stuk van de lopende band.", "Ik neem graag het voortouw bij een lastige dag."),
        ("Ik repareer liever iets zelf dan een team aansturen.", "Ik overtuig graag collega's van een snellere aanpak."),
        ("Ik til en sorteer liever dan targets bespreken met anderen.", "Ik regel graag extra capaciteit als het druk wordt."),
    ],
    ("R", "C"): [
        ("Ik bouw liever iets tastbaars dan hele dag lijsten bijwerken.", "Ik werk graag met vaste stappen, bonnen en duidelijke afspraken."),
        ("Ik zit liever op de heftruck dan uren administratie doen.", "Ik houd graag ordners, codes en voorraad netjes bij."),
        ("Ik maak liever een muur of meubel dan rapporten typen.", "Ik volg graag het vaste protocol bij elke handeling."),
    ],
    ("I", "A"): [
        ("Ik meet en test graag voordat ik iets verander.", "Ik bedenk graag een nieuw idee zonder eerst alles te meten."),
        ("Ik zoek liever feiten in cijfers dan een creatief verhaal.", "Ik maak graag een ontwerp of filmpje om iets uit te leggen."),
        ("Ik analyseer liever een storing dan direct een wild idee proberen.", "Ik pas graag kleur, vorm of tekst aan voor een frisse uitstraling."),
    ],
    ("I", "S"): [
        ("Ik zoek liever de oorzaak van een probleem in data.", "Ik vraag liever hoe het met iemand gaat dan meteen analyseren."),
        ("Ik lees liever een rapport dan uren met collega's overleggen.", "Ik begeleid graag iemand stap voor stap bij een nieuwe taak."),
        ("Ik puzzel liever alleen dan in een kring problemen bespreken.", "Ik leg graag rustig uit tot iedereen het snapt."),
    ],
    ("I", "E"): [
        ("Ik onderzoek liever eerst alle opties voordat ik beslis.", "Ik duw graag een besluit door als de tijd dringt."),
        ("Ik vergelijk liever cijfers dan mensen overtuigen in een pitch.", "Ik pak graag commerciële kansen aan met het team."),
        ("Ik test liever een hypothese dan direct een verkoopactie starten.", "Ik onderhandel graag met leveranciers of klanten."),
    ],
    ("I", "C"): [
        ("Ik wil eerst weten waarom iets misgaat voor ik invul.", "Ik vul liever direct formulieren in volgens het vaste format."),
        ("Ik duik liever in een storing dan in een urenstaat.", "Ik werk graag met tabellen, codes en vaste regels."),
        ("Ik zoek liever een technische verklaring dan een routekaart volgen.", "Ik controleer graag elk vakje op een checklist."),
    ],
    ("A", "S"): [
        ("Ik ontwerp liever een nieuwe poster dan een gespreksronde leiden.", "Ik praat graag met collega's over hoe zij zich voelen."),
        ("Ik maak liever iets moois dan iemand persoonlijk begeleiden.", "Ik help graag een nieuwe collega zijn weg vinden."),
        ("Ik bedenk liever een creatieve oplossing in stilte.", "Ik werk graag in een warm team waar mensen omkijken naar elkaar."),
    ],
    ("A", "E"): [
        ("Ik maak liever een ontwerp dan de commerciële kant regelen.", "Ik stel graag een actieplan op om meer omzet te halen."),
        ("Ik focus liever op ideeën dan op overtuigen van de baas.", "Ik presenteer graag ons plan aan klanten of leiding."),
        ("Ik teken liever een nieuwe indeling dan targets halen.", "Ik zoek graag naar groeikansen voor de afdeling."),
    ],
    ("A", "C"): [
        ("Ik probeer liever iets nieuws dan het oude schema volgen.", "Ik werk liever volgens een vast stappenplan en sjabloon."),
        ("Ik verander liever kleur of vorm dan alles identiek houden.", "Ik zorg graag dat elk document dezelfde opmaak heeft."),
        ("Ik bedenk liever een andere aanpak dan de checklist afwerken.", "Ik archiveer graag alles netjes op de juiste plek."),
    ],
    ("S", "E"): [
        ("Ik luister liever naar iemand dan meteen een besluit nemen.", "Ik neem graag snel besluiten als het team wacht."),
        ("Ik coach liever een collega dan de verkoop pushen.", "Ik regel graag snel extra personeel bij piekdrukte."),
        ("Ik bied liever een luisterend oor dan een harde deadline stellen.", "Ik pitche graag ons idee aan een nieuwe klant."),
    ],
    ("S", "C"): [
        ("Ik help liever persoonlijk dan alleen formulieren bijhouden.", "Ik werk graag met vaste roosters en duidelijke regels."),
        ("Ik praat liever met een klant dan cijfers in Excel zetten.", "Ik sorteer graag bonnen en dossiers op volgnummer."),
        ("Ik begeleid liever iemand dan uren tellen volgens schema.", "Ik check graag of elke handtekening op de juiste plek staat."),
    ],
    ("E", "C"): [
        ("Ik start liever een nieuwe actie dan het oude proces volgen.", "Ik hou liever vast aan afspraken en standaard werkwijzen."),
        ("Ik wil graag sneller groeien dan rustig alles dubbel controleren.", "Ik werk graag met vaste protocollen die al bewezen werken."),
        ("Ik onderhandel liever over targets dan bonnen archiveren.", "Ik bewaak graag dat elk document het juiste nummer krijgt."),
    ],
}

COMP_TEXT: dict[tuple[int, int], list[tuple[str, str]]] = {
    (0, 9): [
        ("Ik overleg graag met collega's voordat ik iets alleen beslis.", "Ik werk een taak het liefst rustig zelf af tot het klaar is."),
        ("Ik stem graag af in het team wie wat doet.", "Ik pak liever mijn eigen deeltje zonder veel overleg."),
    ],
    (10, 11): [
        ("Ik loop mijn werk aan het eind nog eens extra na.", "Ik schakel snel door als de planning ineens verandert."),
        ("Ik controleer liever twee keer dan iets verkeerd afgeven.", "Ik pas mijn aanpak aan zodra er nieuwe info komt."),
    ],
    (1, 2): [
        ("Ik focus op het halen van mijn doelen voor vandaag.", "Ik blijf rustig als het onverwacht druk en chaotisch wordt."),
        ("Ik werk doelgericht door tot de taak af is.", "Ik houd mijn hoofd koel als alles tegelijk speelt."),
    ],
    (6, 11): [
        ("Ik werk het liefst volgens een vast plan dat ik vooraf maak.", "Ik pas mijn planning aan als er iets verandert op de werkvloer."),
        ("Ik maak graag een lijstje wat ik wanneer doe.", "Ik schuif taken makkelijk op als er spoed is."),
    ],
    (3, 1): [
        ("Ik bedenk graag een andere manier als iets niet soepel loopt.", "Ik focus op afronden van wat op de planning staat."),
        ("Ik probeer graag een nieuw hulpmiddel of werkwijze uit.", "Ik zet door tot het afgesproken resultaat er is."),
    ],
    (0, 7): [
        ("Ik stem af met collega's wie een klant helpt.", "Ik voel graag aan wat een klant of bewoner nodig heeft."),
        ("Ik los samen een probleem op met het team.", "Ik let op signalen dat iemand niet tevreden is."),
    ],
    (4, 5): [
        ("Ik begin graag een gesprek met mensen die ik minder ken.", "Ik zorg dat mijn boodschap helder en duidelijk overkomt."),
        ("Ik maak snel contact op een nieuwe werkplek.", "Ik formuleer helder wat ik bedoel in een mail of gesprek."),
    ],
    (8, 9): [
        ("Ik vraag feedback en leer daarvan voor een volgende keer.", "Ik pak liever zelf een taak op zonder eerst te vragen."),
        ("Ik zoek uitleg als ik iets nog niet snap.", "Ik werk door tot ik het zelf kan zonder hulp."),
    ],
    (2, 7): [
        ("Ik blijf kalm als een klant boos of ongeduldig is.", "Ik neem de tijd om te vragen wat iemand precies wil."),
        ("Ik raak niet in paniek bij piekdrukte.", "Ik kijk eerst naar de wens van de klant of bewoner."),
    ],
    (6, 10): [
        ("Ik plan vooraf welke stap ik wanneer zet.", "Ik controleer details voordat ik iets doorgeef of verstuur."),
        ("Ik houd overzicht over meerdere taken tegelijk.", "Ik let op kleine foutjes in getallen of namen."),
    ],
    (5, 7): [
        ("Ik leg rustig uit wat we gaan doen.", "Ik let op of de ander zich gehoord voelt."),
        ("Ik stel een vraag als iets onduidelijk is.", "Ik vraag door tot ik snap wat iemand nodig heeft."),
    ],
    (1, 0): [
        ("Ik pak een taak zelf op en werk die af.", "Ik schakel collega's in als dat sneller of beter is."),
        ("Ik wil het eindresultaat zelf kunnen laten zien.", "Ik deel het werk zodat we samen sneller klaar zijn."),
    ],
    (3, 4): [
        ("Ik bedenk graag een slimmere werkwijze op de vloer.", "Ik maak makkelijk contact met collega's die ik net ontmoet."),
        ("Ik probeer een nieuwe aanpak als iets stroef loopt.", "Ik praat graag met mensen op een nieuwe plek."),
    ],
    (3, 6): [
        ("Ik zoek een creatieve oplossing als plannen struikelen.", "Ik maak graag eerst een duidelijk stappenplan."),
        ("Ik verzin graag een andere route als de lijn vol zit.", "Ik houd van vaste routines op de werkvloer."),
    ],
    (3, 8): [
        ("Ik test graag een nieuwe werkwijze in de praktijk.", "Ik zoek uitleg voordat ik iets nieuws probeer."),
        ("Ik probeer snel een ander hulpmiddel uit.", "Ik vraag een collega om te helpen uitleggen."),
    ],
    (2, 3): [
        ("Ik blijf rustig als machines of processen vastlopen.", "Ik bedenk graag een andere aanpak op dat moment."),
        ("Bij drukte houd ik mijn hoofd koel.", "Ik zoek snel een nieuw idee om verder te gaan."),
    ],
    (0, 8): [
        ("Ik help collega's en verdeel werk in het team.", "Ik leer liever eerst hoe iets werkt voordat ik help."),
        ("Ik stem af wie welke taak oppakt.", "Ik oefen graag tot ik het zelfstandig kan."),
    ],
    (4, 6): [
        ("Ik maak snel contact met onbekenden op de werkvloer.", "Ik plan graag mijn dag voordat ik begin."),
        ("Ik start makkelijk een gesprek met een nieuwe collega.", "Ik werk liever met een vast stappenplan."),
    ],
    (1, 5): [
        ("Ik focus op het resultaat dat vandaag nodig is.", "Ik zorg dat mijn uitleg helder en rustig overkomt."),
        ("Ik werk door tot de taak echt klaar is.", "Ik leg duidelijk uit wat ik ga doen."),
    ],
    (2, 8): [
        ("Ik blijf rustig als alles tegelijk moet.", "Ik leer graag hoe ik volgende keer sneller werk."),
        ("Druk van klanten kan ik goed hebben.", "Ik vraag tips en pas ze daarna toe."),
    ],
    (4, 7): [
        ("Ik praat graag met nieuwe collega's op de werkvloer.", "Ik let eerst op wat een klant of bewoner nodig heeft."),
        ("Ik maak snel contact in een druk team.", "Ik luister eerst goed naar de vraag van de klant."),
    ],
    (2, 5): [
        ("Ik blijf rustig als het druk en onvoorspelbaar wordt.", "Ik leg rustig uit wat ik ga doen en waarom."),
        ("Onder tijdsdruk raak ik niet in paniek.", "Ik zorg dat mijn boodschap helder overkomt."),
    ],
    (1, 8): [
        ("Ik rond graag taken af waar ik mee ben begonnen.", "Ik leer graag bij als iets nog nieuw voor mij is."),
        ("Ik focus op het doel voor vandaag.", "Ik oefen tot ik iets steeds beter kan."),
    ],
    (0, 2): [
        ("Ik overleg met collega's hoe we het werk verdelen.", "Ik blijf rustig als er veel tegelijk gebeurt."),
        ("Samen lossen we het liever op dan ieder apart.", "Ik houd mijn hoofd koel bij spoed."),
    ],
    (7, 8): [
        ("Ik vraag door tot ik snap wat een klant wil.", "Ik zoek uitleg om een taak beter te leren."),
        ("Ik wil dat iemand tevreden wegloopt.", "Ik pak een nieuwe taak stap voor stap op."),
    ],
    (5, 8): [
        ("Ik stel een vraag als iets niet duidelijk is.", "Ik leer snel door te oefenen op de werkvloer."),
        ("Ik geef heldere uitleg aan collega's of klanten.", "Ik vraag feedback en pas dat daarna toe."),
    ],
    (6, 7): [
        ("Ik maak vooraf een kort plan voor mijn dienst.", "Ik kijk eerst wat een klant of collega nodig heeft."),
        ("Ik houd mijn taken graag overzichtelijk in een lijst.", "Ik help snel als iemand een praktisch probleem heeft."),
    ],
    (1, 6): [
        ("Ik werk door tot mijn afgesproken resultaat klaar is.", "Ik plan welke stap ik eerst en daarna zet."),
        ("Ik focus op afmaken vóór het einde van de dienst.", "Ik deel groot werk in kleine haalbare stappen."),
    ],
    (5, 6): [
        ("Ik leg uit wat ik ga doen voordat ik start.", "Ik maak een kort overzicht van mijn taken."),
        ("Ik stuur een duidelijke mail of app naar het team.", "Ik hou mijn planning graag op één plek."),
    ],
}

SCHWARTZ_TEXT: dict[tuple[str, str], list[tuple[str, str]]] = {
    ("autonomy", "stability"): [
        ("Ik wil zelf kiezen hoe ik een taak aanpak.", "Ik wil weten wat er van mij verwacht wordt vandaag."),
        ("Ik werk graag op mijn eigen manier binnen de afspraken.", "Ik wil een vast rooster en vaste taken."),
    ],
    ("connection", "achievement"): [
        ("Ik vind een goede sfeer met collega's heel belangrijk.", "Ik wil laten zien wat ik kan en groeien."),
        ("Ik wil bij een team horen waar mensen omkijken naar elkaar.", "Ik wil trots zijn op wat ik aflever."),
    ],
    ("achievement", "connection"): [
        ("Ik wil doelen halen en merken dat ik beter word.", "Ik wil samen optrekken met collega's die ik vertrouw."),
        ("Ik wil resultaat zien van wat ik doe.", "Ik wil graag horen hoe het met collega's gaat."),
    ],
    ("impact", "stability"): [
        ("Ik wil werk doen dat ertoe doet voor anderen of het milieu.", "Ik wil een vaste baan met voorspelbare weken."),
        ("Ik wil merken dat mijn werk ergens toe dient.", "Ik wil zekerheid over inkomen en contract."),
    ],
    ("autonomy", "connection"): [
        ("Ik beslis graag zelf binnen het team wanneer ik wat doe.", "Ik volg liever afspraken die we samen hebben gemaakt."),
        ("Ik plan mijn eigen werkdag als dat mag.", "Ik stem mijn dag af met collega's in het overleg."),
    ],
    ("achievement", "stability"): [
        ("Ik wil doorgroeien en meer verantwoordelijkheid krijgen.", "Ik wil jarenlang bij een stabiele werkgever blijven."),
        ("Ik zoek uitdaging om mijn skills te verbeteren.", "Ik kies graag voor zekerheid boven snelle verandering."),
    ],
    ("impact", "achievement"): [
        ("Ik wil anderen helpen of de wereld een beetje beter maken.", "Ik wil meetbare successen behalen op mijn werk."),
        ("Ik kies werk met maatschappelijke betekenis.", "Ik wil targets halen waar mijn naam aan hangt."),
    ],
    ("autonomy", "impact"): [
        ("Ik bepaal graag zelf de volgorde van mijn taken.", "Ik kies werk waarbij ik iets betekenisvols doe."),
        ("Ik zoek ruimte om mijn eigen ideeën te proberen.", "Ik wil weten dat mijn bijdrage echt iets oplost."),
    ],
    ("connection", "stability"): [
        ("Een hecht team is voor mij belangrijker dan een perfect schema.", "Vaste afspraken geven mij meer rust dan elke dag iets nieuws."),
        ("Ik blijf graag bij collega's die ik goed ken.", "Ik wil weten wat ik volgende week op de planning staat."),
    ],
    ("impact", "connection"): [
        ("Ik wil dat mijn werk anderen helpt.", "Ik wil vooral een fijne band met collega's."),
        ("Ik kies taken met zichtbare waarde voor anderen.", "Ik kies werk waar ik prettig samenwerk met mensen."),
    ],
}

CHECK_TEXT: dict[str, list[str]] = {
    "Samenwerken": [
        "Als een collega vastloopt, help ik mee zonder dat het gevraagd wordt.",
        "In een druk team deel ik graag werk eerlijk met anderen.",
    ],
    "Resultaatgerichtheid": [
        "Ik maak af waar ik aan begin, ook als het tegenzit.",
        "Ik controleer mijn werk voordat ik het overdraag.",
    ],
    "Stressbestendigheid": [
        "Als het druk is, blijf ik rustig en werk ik stap voor stap door.",
        "Kritiek op mijn werk kan ik goed verwerken.",
    ],
    "Communiceren": [
        "Ik kan goed uitleggen wat ik bedoel in een gesprek of mail.",
        "Ik stel een vraag als een instructie niet duidelijk is.",
    ],
    "Klantgerichtheid": [
        "Ik vind het belangrijk dat een klant tevreden weggaat.",
        "Ik blijf vriendelijk als iemand ongeduldig reageert.",
    ],
    "Plannen & organiseren": [
        "Ik weet meestal goed wat ik eerst moet doen op een drukke dag.",
        "Ik houd mijn afspraken en deadlines goed bij.",
    ],
    "Leren & ontwikkelen": [
        "Ik leer graag van feedback van een leidinggevende of collega.",
        "Ik durf iets nieuws te proberen, ook als het nog niet perfect gaat.",
    ],
    "Nauwkeurig werken": [
        "Voor ik iets doorgeef, kijk ik of gegevens en namen kloppen.",
        "Ik merk snel kleine fouten in een formulier of bon.",
    ],
}


@dataclass
class Item:
    blok: str
    display_blok: int
    soort: str
    situatie: str
    kop: str
    optie_a: str
    dimensie_a: str
    optie_b: str
    dimensie_b: str


def word_count(text: str) -> int:
    return len(re.findall(r"\S+", text))


def assert_b1(text: str, label: str) -> None:
    if word_count(text) > 15:
        raise ValueError(f"{label}: meer dan 15 woorden: {text!r}")


def pick_variant(templates: list[tuple[str, str]], idx: int) -> tuple[str, str]:
    a, b = templates[idx % len(templates)]
    assert_b1(a, "optie_a")
    assert_b1(b, "optie_b")
    return a, b


def riasec_pair_item(pair: tuple[str, str], variant: int, display: int) -> Item:
    k1, k2 = pair
    swap = variant % 2 == 1
    templates = RIASEC_TEXT[pair]
    ta, tb = pick_variant(templates, variant // 2)
    if swap:
        ta, tb = tb, ta
        d_a, d_b = RIASEC[k2], RIASEC[k1]
    else:
        d_a, d_b = RIASEC[k1], RIASEC[k2]
    situatie = SITUATIONS[variant % len(SITUATIONS)]
    return Item(
        blok="interesse",
        display_blok=display,
        soort="ab",
        situatie=situatie,
        kop=KOP_AB,
        optie_a=ta,
        dimensie_a=d_a,
        optie_b=tb,
        dimensie_b=d_b,
    )


def comp_pair_item(pair: tuple[int, int], variant: int, display: int) -> Item:
    swap = variant % 2 == 1
    templates = COMP_TEXT.get(pair) or COMP_TEXT[pair[1], pair[0]]
    ta, tb = pick_variant(templates, variant // 2)
    i, j = pair
    if swap:
        ta, tb = tb, ta
        d_a, d_b = COMPETENCES[j], COMPETENCES[i]
    else:
        d_a, d_b = COMPETENCES[i], COMPETENCES[j]
    situatie = SITUATIONS[(variant + 3) % len(SITUATIONS)]
    return Item(
        blok="competentie",
        display_blok=display,
        soort="ab",
        situatie=situatie,
        kop=KOP_AB,
        optie_a=ta,
        dimensie_a=d_a,
        optie_b=tb,
        dimensie_b=d_b,
    )


def schwartz_pair_item(pair: tuple[str, str], variant: int, display: int) -> Item:
    swap = variant % 2 == 1
    templates = SCHWARTZ_TEXT[pair]
    ta, tb = pick_variant(templates, variant // 2)
    k1, k2 = pair
    if swap:
        ta, tb = tb, ta
        d_a, d_b = SCHWARTZ[k2], SCHWARTZ[k1]
    else:
        d_a, d_b = SCHWARTZ[k1], SCHWARTZ[k2]
    situatie = SITUATIONS[(variant + 5) % len(SITUATIONS)]
    return Item(
        blok="werkwaarden",
        display_blok=display,
        soort="ab",
        situatie=situatie,
        kop=KOP_AB,
        optie_a=ta,
        dimensie_a=d_a,
        optie_b=tb,
        dimensie_b=d_b,
    )


def check_item(dim: str, variant: int, display: int) -> Item:
    texts = CHECK_TEXT[dim]
    stmt = texts[variant % len(texts)]
    assert_b1(stmt, "check optie_a")
    situatie = SITUATIONS[(variant + 1) % len(SITUATIONS)]
    return Item(
        blok="competentie",
        display_blok=display,
        soort="check",
        situatie=situatie,
        kop=KOP_CHECK,
        optie_a=stmt,
        dimensie_a=dim,
        optie_b="",
        dimensie_b="",
    )


def balanced_competence_plan(
    count: int,
    *,
    active: int | None = None,
    min_per_dim: int = 4,
) -> list[tuple[int, int]]:
    active = active or len(COMPETENCES)
    plan: list[tuple[int, int]] = []
    use = Counter({i: 0 for i in range(active)})
    eligible = [p for p in COMP_PAIRS if p[0] < active and p[1] < active]
    target = min(min_per_dim, (count * 2 + active - 1) // active)
    while len(plan) < count:
        needy = [i for i in range(active) if use[i] < target]
        if needy:
            focus = min(needy, key=lambda i: use[i])
            choices = [p for p in eligible if focus in p]
            best = min(choices, key=lambda p: (max(use[p[0]], use[p[1]]), use[p[0]] + use[p[1]]))
        else:
            best = min(
                eligible,
                key=lambda p: (max(use[p[0]] + 1, use[p[1]] + 1), use[p[0]] + use[p[1]]),
            )
        plan.append(best)
        use[best[0]] += 1
        use[best[1]] += 1

    def tally() -> Counter[int]:
        c: Counter[int] = Counter()
        for p in plan:
            c[p[0]] += 1
            c[p[1]] += 1
        return c

    use = tally()

    def tally_after(idx: int, repl: tuple[int, int]) -> Counter[int]:
        c = tally()
        for i in plan[idx]:
            c[i] -= 1
        for i in repl:
            c[i] += 1
        return c

    for _ in range(count * 4):
        if not any(use[i] < target for i in range(active)):
            break
        focus = min((i for i in range(active) if use[i] < target), key=lambda i: use[i])
        rich = max(range(active), key=lambda i: use[i])
        swapped = False
        best_gain: tuple[int, int, int, int] | None = None
        best_update: tuple[int, tuple[int, int]] | None = None
        for idx, p in enumerate(plan):
            if rich not in p or focus in p:
                continue
            for repl in eligible:
                if focus not in repl or rich in repl:
                    continue
                nu = tally_after(idx, repl)
                if nu[focus] <= use[focus]:
                    continue
                if nu[rich] < target:
                    continue
                if min(nu[i] for i in range(active)) < min(use[i] for i in range(active)):
                    continue
                gain = (nu[focus], -max(nu.values()), -nu[rich], idx)
                if best_gain is None or gain > best_gain:
                    best_gain = gain
                    best_update = (idx, repl)
        if best_update:
            idx, repl = best_update
            plan[idx] = repl
            swapped = True
        if not swapped:
            break
        use = tally()
    return plan


def balanced_schwartz_plan(count: int) -> list[tuple[str, str]]:
    plan: list[tuple[str, str]] = []
    use = Counter({k: 0 for k in SCHWARTZ})
    while len(plan) < count:
        best = None
        best_score = None
        for pair in SCHWARTZ_PAIRS:
            score = use[pair[0]] + use[pair[1]]
            if best is None or score < best_score:
                best, best_score = pair, score
        assert best is not None
        plan.append(best)
        use[best[0]] += 1
        use[best[1]] += 1
    return plan


def balanced_riasec_plan(count: int) -> list[tuple[str, str]]:
    """Pick `count` pair keys so each RIASEC letter appears ~equally in forced-choice items."""
    plan: list[tuple[str, str]] = []
    letter_use = Counter({k: 0 for k in RIASEC})
    vi = 0
    while len(plan) < count:
        best = None
        best_score = None
        for pair in RIASEC_PAIRS:
            score = letter_use[pair[0]] + letter_use[pair[1]]
            if best is None or score < best_score:
                best, best_score = pair, score
        assert best is not None
        plan.append(best)
        letter_use[best[0]] += 1
        letter_use[best[1]] += 1
        vi += 1
        if vi > count * 30:
            break
    return plan


def pair_repeat_plan(pairs: list, count: int) -> list:
    plan: list = []
    idx = 0
    while len(plan) < count:
        plan.append(pairs[idx % len(pairs)])
        idx += 1
    return plan


def display_for(blok: str, phase: str, index_in_blok: int, total_in_blok: int) -> int:
    """Basis: 1-3 per blok; Uitgebreid: split each blok over two display blocks."""
    if phase == "basis":
        return {"interesse": 1, "competentie": 2, "werkwaarden": 3}[blok]
    half = max(1, total_in_blok // 2)
    base = {"interesse": 1, "competentie": 3, "werkwaarden": 5}[blok]
    return base if index_in_blok < half else base + 1


def build_basis() -> list[Item]:
    items: list[Item] = []
    variant = 0

    ri_plan = balanced_riasec_plan(12)
    for i, pair in enumerate(ri_plan):
        items.append(riasec_pair_item(pair, variant, display_for("interesse", "basis", i, 12)))
        variant += 1

    comp_plan = BASIS_COMP_PLAN
    for i, pair in enumerate(comp_plan):
        items.append(comp_pair_item(pair, variant, display_for("competentie", "basis", i, 18)))
        variant += 1

    sw_plan = balanced_schwartz_plan(10)
    for i, pair in enumerate(sw_plan):
        items.append(schwartz_pair_item(pair, variant, display_for("werkwaarden", "basis", i, 10)))
        variant += 1

    RNG.shuffle(items)
    return items


def build_uitgebreid() -> list[Item]:
    items: list[Item] = []
    variant = 0

    interesse_count = 102
    ri_plan = []
    per_pair = (interesse_count + len(RIASEC_PAIRS) - 1) // len(RIASEC_PAIRS)
    for pair in RIASEC_PAIRS:
        ri_plan.extend([pair] * per_pair)
    ri_plan = ri_plan[:interesse_count]
    RNG.shuffle(ri_plan)
    for i, pair in enumerate(ri_plan):
        items.append(
            riasec_pair_item(pair, variant, display_for("interesse", "uitgebreid", i, interesse_count))
        )
        variant += 1

    comp_ab = 60
    comp_plan = balanced_competence_plan(comp_ab, active=10, min_per_dim=12)
    for i, pair in enumerate(comp_plan):
        items.append(
            comp_pair_item(pair, variant, display_for("competentie", "uitgebreid", i, comp_ab + 8))
        )
        variant += 1

    check_dims = list(CHECK_TEXT.keys())[:8]
    check_indices = sorted(RNG.sample(range(comp_ab), len(check_dims)))
    check_by_slot = dict(zip(check_indices, check_dims))
    insert_checks: list[tuple[int, Item]] = []
    for slot, dim in sorted(check_by_slot.items()):
        insert_checks.append(
            (slot, check_item(dim, variant, display_for("competentie", "uitgebreid", slot, comp_ab + 8)))
        )
        variant += 1

    werk_count = 30
    sw_plan = balanced_schwartz_plan(werk_count)
    werk_items: list[Item] = []
    for i, pair in enumerate(sw_plan):
        werk_items.append(
            schwartz_pair_item(pair, variant, display_for("werkwaarden", "uitgebreid", i, werk_count))
        )
        variant += 1

    # Interleave competentie checks into competentie block portion
    comp_segment = [it for it in items if it.blok == "competentie"]
    interesse_segment = [it for it in items if it.blok == "interesse"]
    merged: list[Item] = []
    merged.extend(interesse_segment)
    ci = 0
    for slot, chk in insert_checks:
        while ci < slot:
            merged.append(comp_segment[ci])
            ci += 1
        merged.append(chk)
    while ci < len(comp_segment):
        merged.append(comp_segment[ci])
        ci += 1
    merged.extend(werk_items)

    assert len(merged) == interesse_count + comp_ab + len(check_dims) + werk_count
    RNG.shuffle(merged)
    return merged


def count_coverage(items: list[Item]) -> dict[str, Counter]:
    riasec = Counter()
    comp = Counter()
    schwartz = Counter()
    ri_pairs = Counter()
    checks = 0
    ab = 0

    for it in items:
        if it.soort == "check":
            checks += 1
            comp[it.dimensie_a] += 1
            continue
        ab += 1
        if it.blok == "interesse":
            riasec[it.dimensie_a] += 1
            riasec[it.dimensie_b] += 1
            k_a = it.dimensie_a.split()[0]
            k_b = it.dimensie_b.split()[0]
            key = tuple(sorted([k_a, k_b]))
            ri_pairs[key] += 1
        elif it.blok == "competentie":
            comp[it.dimensie_a] += 1
            comp[it.dimensie_b] += 1
        elif it.blok == "werkwaarden":
            schwartz[it.dimensie_a] += 1
            schwartz[it.dimensie_b] += 1

    return {
        "riasec": riasec,
        "competentie": comp,
        "schwartz": schwartz,
        "riasec_pairs": ri_pairs,
        "meta": Counter({"ab": ab, "check": checks, "total": len(items)}),
    }


def write_csv(path: Path, items: list[Item]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f, delimiter=";", lineterminator="\n", quoting=csv.QUOTE_MINIMAL)
        w.writerow(
            [
                "nr",
                "blok",
                "display_blok",
                "soort",
                "situatie",
                "kop",
                "optie_a",
                "dimensie_a",
                "optie_b",
                "dimensie_b",
                "bron",
            ]
        )
        for nr, it in enumerate(items, start=1):
            w.writerow(
                [
                    nr,
                    it.blok,
                    it.display_blok,
                    it.soort,
                    it.situatie,
                    it.kop,
                    it.optie_a,
                    it.dimensie_a,
                    it.optie_b,
                    it.dimensie_b,
                    BRON,
                ]
            )


def write_dekking(path: Path, name: str, items: list[Item], cov: dict[str, Counter]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    lines = [
        f"# Dekking {name} (CONCEPT)",
        "",
        f"Gegenereerd op {date.today().isoformat()} door `tools/werkgever-apk/generate_ab_question_bank.py`.",
        "",
        "## Samenvatting",
        "",
        f"- Totaal items: **{len(items)}**",
        f"- Forced choice (ab): **{cov['meta']['ab']}**",
        f"- Competentie-check (check): **{cov['meta']['check']}**",
        "",
        "## RIASEC (interesse)",
        "",
        "| Dimensie | Aantal meetpunten |",
        "| --- | ---: |",
    ]
    for dim, n in sorted(cov["riasec"].items()):
        lines.append(f"| {dim} | {n} |")

    lines.extend(["", "## RIASEC-paren (forced choice)", "", "| Paar | Aantal |", "| --- | ---: |"])
    for key, n in sorted(cov["riasec_pairs"].items()):
        lines.append(f"| {' – '.join(key)} | {n} |")

    lines.extend(["", "## Competenties", "", "| Dimensie | Aantal meetpunten |", "| --- | ---: |"])
    for dim, n in sorted(cov["competentie"].items()):
        lines.append(f"| {dim} | {n} |")

    lines.extend(
        ["", "## Werkwaarden (Schwartz)", "", "| Dimensie | Aantal meetpunten |", "| --- | ---: |"]
    )
    for dim, n in sorted(cov["schwartz"].items()):
        lines.append(f"| {dim} | {n} |")

    lines.extend(["", "## Blokken (display_blok)", "", "| display_blok | Aantal |", "| ---: | ---: |"])
    by_display = Counter(it.display_blok for it in items)
    for db in sorted(by_display):
        lines.append(f"| {db} | {by_display[db]} |")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def copy_templates() -> None:
    tpl_dir = ROOT / "Jobsy.Web" / "wwwroot" / "werkgever-apk" / "templates"
    src_dir = Path("/tmp/pakketten")
    mapping = {
        "11-pakket-kiezen.html": "11-pakket-kiezen.html",
        "12-werkgever-pakket.html": "12-werkgever-pakket.html",
        "13-ab-vraag.html": "13-ab-vraag.html",
        "13b-ab-pauze.html": "13b-ab-pauze.html",
    }
    subs = {
        'href="../src/shared.css"': 'href="/werkgever-apk/css/shared.css"',
        'href="../src/extra.css"': 'href="/werkgever-apk/css/extra.css"',
        'href="functie.css"': 'href="/werkgever-apk/css/functie.css"',
        'href="pakketten.css"': 'href="/werkgever-apk/css/pakketten.css"',
        'src="../src/img/': 'src="/werkgever-apk/img/',
    }
    for src_name, dst_name in mapping.items():
        raw = (src_dir / src_name).read_text(encoding="utf-8")
        for old, new in subs.items():
            raw = raw.replace(old, new)
        (tpl_dir / dst_name).write_text(raw, encoding="utf-8")


def validate_bank(
    name: str,
    items: list[Item],
    min_ri: int,
    min_comp: int,
    min_sw: int,
    *,
    comp_dims: list[str],
) -> None:
    cov = count_coverage(items)
    for dim in RIASEC.values():
        n = cov["riasec"].get(dim, 0)
        if n < min_ri:
            raise SystemExit(f"{name}: RIASEC {dim} heeft {n}, min {min_ri}")
    for dim in comp_dims:
        n = cov["competentie"].get(dim, 0)
        if n < min_comp:
            raise SystemExit(f"{name}: competentie {dim} heeft {n}, min {min_comp}")
    for dim in SCHWARTZ.values():
        n = cov["schwartz"].get(dim, 0)
        if n < min_sw:
            raise SystemExit(f"{name}: Schwartz {dim} heeft {n}, min {min_sw}")
    for it in items:
        if it.soort == "ab":
            assert_b1(it.optie_a, f"{name} nr")
            assert_b1(it.optie_b, f"{name} nr")
        else:
            assert_b1(it.optie_a, f"{name} check")


def main() -> None:
    basis = build_basis()
    uitgebreid = build_uitgebreid()

    basis_comp = COMPETENCES[:9]
    validate_bank("Basis", basis, min_ri=4, min_comp=4, min_sw=4, comp_dims=basis_comp)
    validate_bank(
        "Uitgebreid",
        uitgebreid,
        min_ri=12,
        min_comp=12,
        min_sw=12,
        comp_dims=COMPETENCES[:10],
    )

    basis_path = DATA_DIR / "ab-basis-CONCEPT.csv"
    uit_path = DATA_DIR / "ab-uitgebreid-CONCEPT.csv"
    write_csv(basis_path, basis)
    write_csv(uit_path, uitgebreid)

    cov_basis = count_coverage(basis)
    cov_uit = count_coverage(uitgebreid)
    dekking_path = DOCS_DIR / "ab-dekking-CONCEPT.md"
    body = []
    for section_name, subset, cov in (
        ("Basis (~40)", basis, cov_basis),
        ("Uitgebreid (~200)", uitgebreid, cov_uit),
    ):
        part = DOCS_DIR / f".{section_name}.md.part"
        write_dekking(part, section_name, subset, cov)
        body.append(part.read_text(encoding="utf-8"))
        part.unlink()
    dekking_path.write_text(
        "# Loopbaan-APK A/B question banks — dekking (CONCEPT)\n\n"
        + "\n".join(body),
        encoding="utf-8",
    )

    copy_templates()

    print(f"Basis: {len(basis)} rows -> {basis_path}")
    print(f"Uitgebreid: {len(uitgebreid)} rows -> {uit_path}")
    print(f"Dekking: {dekking_path}")
    print("Template assets: Jobsy.Web/wwwroot/werkgever-apk/css + templates")


if __name__ == "__main__":
    main()