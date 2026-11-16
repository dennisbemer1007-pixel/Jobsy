"""Hand-authored Loopbaan-APK Basis items (40): bipolaire A/B-keuzes met echte tegenpolen.

Interesse:   alleen R<->S, I<->E, A<->C (12 items, 4 per paar).
Competentie: één competentie per item; optie_a = sterke pool, optie_b = tegenpool (18 items, 2 per competentie).
Werkwaarden: alleen Zekerheid<->Zelfstandigheid, Prestatie<->Verbinding, Betekenis<->Zekerheid (10 items).
"""
from __future__ import annotations

from bank_schema import BASIS_COMPETENCES, KOP_AB, RIASEC, SCHWARTZ, Item

__all__ = ["load_basis_items"]

# (sector, situatie, optie eerste letter, optie tweede letter); richting wisselt per item.
RIASEC_PAREN: dict[tuple[str, str], list[tuple[str, str, str, str]]] = {
    ("R", "S"): [
        ("magazijn", "Een collega in het magazijn heeft last van zijn rug.",
         "Zet de zware dozen op de onderste stelling.",
         "Vraag hoe het gaat en regel een rustpauze."),
        ("horeca", "Een gast op het terras zit aan een wiebelende tafel.",
         "Pak gereedschap en maak de tafelpoot recht.",
         "Vraag of hij liever verhuist en praat even bij."),
        ("zorg", "Een bewoner kan zijn rolstoel niet meer goed besturen.",
         "Controleer de banden en stel de besturing bij.",
         "Ga naast hem zitten en vraag wat hij nodig heeft."),
        ("bouw", "Een nieuwe leerling staat voor het eerst op de bouwplaats.",
         "Geef hem een hamer en laat hem de eerste plank spijkeren.",
         "Loop met hem rond en stel hem voor aan het team."),
    ],
    ("I", "E"): [
        ("winkel", "De verkoop van thee in de winkel loopt achter.",
         "Zoek in de kassadata uit welke soorten niet verkopen.",
         "Bedenk een actie en overtuig de eigenaar om te starten."),
        ("logistiek", "Een klant wil sneller geleverd krijgen dan nu kan.",
         "Reken uit waar in de planning tijd te winnen is.",
         "Bel de klant en verkoop hem een duurdere snelle levering."),
        ("kantoor", "De afdeling moet een nieuw telefoonsysteem kiezen.",
         "Vergelijk drie systemen op prijs, functies en storingen.",
         "Onderhandel met een leverancier over een flinke korting."),
        ("productie", "De fabriek houdt veel afval over bij het snijden van plaat.",
         "Meet na welke machine-instelling het meeste afval geeft.",
         "Vraag een offerte voor een zuinigere machine en pitch die."),
    ],
    ("A", "C"): [
        ("schoonmaak", "De personeelsruimte van het schoonmaakbedrijf is kaal en rommelig.",
         "Verf een muur en hang zelfgemaakte posters op.",
         "Zet alles in genummerde kasten en maak een lijst."),
        ("horeca", "De menukaart van het restaurant moet vernieuwd worden.",
         "Teken de kaart zelf met eigen illustraties.",
         "Zet alle gerechten, prijzen en allergenen in een tabel."),
        ("kantoor", "Voor de personeelsdag moet de zaal worden ingericht.",
         "Versier de zaal met bloemen en gekleurd licht.",
         "Maak een plattegrond en een tijdschema voor de opbouw."),
        ("winkel", "De nieuwe kerstspullen komen binnen in de winkel.",
         "Bouw een sfeervolle kerstetalage bij de ingang.",
         "Boek alles in en zet het per soort in de schappen."),
    ],
}

# (sector, situatie, optie sterke pool, optie tegenpool) per competentie
COMPETENTIE_ITEMS: dict[str, list[tuple[str, str, str, str]]] = {
    "Samenwerken": [
        ("magazijn", "Er staan vanochtend tien pallets klaar om uit te laden.",
         "Verdeel het werk met je collega's en laad samen.",
         "Pak je eigen pallets en laad alleen door."),
        ("zorg", "De afdeling moet het weekrooster nog invullen.",
         "Overleg met het team wie welke dienst wil.",
         "Maak zelf een voorstel en leg het voor."),
    ],
    "Resultaatgerichtheid": [
        ("bouw", "Het dak moet vandaag dicht zijn voor de regen.",
         "Werk door tot het dak helemaal dicht is.",
         "Stop op de afgesproken tijd en maak morgen af."),
        ("winkel", "Het is koopjesdag en het doel staat op het bord.",
         "Zet alles op de verkoop en haal het doel.",
         "Houd de rust en blijf in je gewone tempo."),
    ],
    "Stressbestendigheid": [
        ("horeca", "Op vrijdagavond hangen er twintig bonnen in de keuken.",
         "Blijf rustig en werk de bonnen een voor een af.",
         "Zeg dat het te druk is en vraag extra handen."),
        ("transport", "Je staat in de file en de klant belt steeds.",
         "Blijf kalm rijden en bel hem later terug.",
         "Zet de auto aan de kant en regel eerst een nieuwe afspraak."),
    ],
    "Creativiteit / vernieuwen": [
        ("productie", "De verpakking van de lijn past niet meer in de doos.",
         "Bedenk een nieuwe manier om in te pakken.",
         "Bestel dezelfde doos in een andere maat."),
        ("kinderopvang", "Het regent en de kinderen kunnen niet naar buiten.",
         "Verzin een nieuw spel met wat je hebt.",
         "Speel het bekende spel dat altijd goed gaat."),
    ],
    "Contact maken": [
        ("kantoor", "Er begint vandaag een nieuwe collega op de afdeling.",
         "Loop naar hem toe en stel jezelf voor.",
         "Laat hem eerst rustig op zijn plek aankomen."),
        ("bouw", "Op de bouwplaats werkt vandaag een ploeg van een ander bedrijf.",
         "Groet de ploeg en vraag hoe zij werken.",
         "Werk door en laat iedereen zijn eigen werk doen."),
    ],
    "Communiceren": [
        ("kantoor", "Een collega maakt steeds dezelfde fout in het systeem.",
         "Bespreek het met haar en leg het rustig uit.",
         "Leg een korte checklist op haar bureau."),
        ("zorg", "Een familie wil weten hoe het met vader gaat.",
         "Nodig hen uit en vertel alles rustig na.",
         "Geef een kort overzicht en wijs naar het dossier."),
    ],
    "Plannen & organiseren": [
        ("transport", "Morgen moet je veertig pakketten in drie wijken bezorgen.",
         "Maak vanavond een route met tijden per wijk.",
         "Begin morgen en kies onderweg de beste volgorde."),
        ("horeca", "Zaterdag komt er een bruiloft met honderd gasten.",
         "Schrijf een tijdschema voor keuken en bediening.",
         "Houd de dag globaal en stuur bij waar nodig."),
    ],
    "Klantgerichtheid": [
        ("winkel", "Een klant wil een jas ruilen zonder bonnetje.",
         "Zoek een oplossing zodat hij tevreden naar huis gaat.",
         "Houd je aan de ruilregels van de winkel."),
        ("schoonmaak", "Een klant vraagt of je ook de ramen wilt zemen.",
         "Doe het extra, want zij vraagt het zelf.",
         "Wijs op de afgesproken lijst met taken."),
    ],
    "Leren & ontwikkelen": [
        ("productie", "De fabriek schaft een nieuwe machine aan.",
         "Meld je aan om als eerste ermee te leren werken.",
         "Blijf werken met de machine die je goed kent."),
        ("kantoor", "Het bedrijf stapt over op een nieuw boekhoudprogramma.",
         "Volg de training en probeer het meteen uit.",
         "Werk in het oude programma tot de overgang echt komt."),
    ],
}

# (sector, situatie, optie eerste waarde, optie tweede waarde); richting wisselt per item.
WERKWAARDE_PAREN: dict[tuple[str, str], list[tuple[str, str, str, str]]] = {
    ("stability", "autonomy"): [
        ("magazijn", "Je mag kiezen hoe je volgend jaar in het magazijn werkt.",
         "Neem het vaste contract met vaste dagen en uren.",
         "Neem het flexcontract en kies zelf je werkdagen."),
        ("schoonmaak", "Je kunt je schoonmaakroute vast laten of zelf plannen.",
         "Houd je vaste route met vaste tijden per gebouw.",
         "Bepaal zelf in welke volgorde je de gebouwen doet."),
        ("kantoor", "Je kunt je werkweek op twee manieren inrichten.",
         "Werk elke dag van negen tot vijf op kantoor.",
         "Verdeel je uren zelf en werk deels thuis."),
    ],
    ("achievement", "connection"): [
        ("winkel", "De winkel start een verkoopwedstrijd voor de beste medewerker.",
         "Ga voor de eerste plaats en de bonus.",
         "Help collega's zodat het hele team het goed doet."),
        ("bouw", "Het bouwteam krijgt een bonus als het project eerder klaar is.",
         "Werk extra uren om de bonus binnen te halen.",
         "Zorg dat de ploeg samen pauzeert en iedereen meekomt."),
        ("horeca", "De chef kiest aan het eind van de maand de beste medewerker.",
         "Werk hard om met je naam op het bord te staan.",
         "Zet je in voor een goede sfeer in de keuken."),
        ("transport", "De vervoerder meet welke chauffeur de meeste ritten op tijd rijdt.",
         "Probeer bovenaan de ranglijst te komen.",
         "Neem een nieuwe collega mee en leer hem de route."),
    ],
    ("impact", "stability"): [
        ("zorg", "Je kunt kiezen tussen twee werkplekken in de zorg.",
         "Werk in een nieuw project voor eenzame ouderen.",
         "Werk op een vaste afdeling met duidelijke taken."),
        ("groen", "Een hoveniersbedrijf zoekt iemand voor twee soorten klussen.",
         "Leg schooltuinen aan waar kinderen groenten leren kweken.",
         "Onderhoud vaste tuinen bij bedrijven met vaste afspraken."),
        ("winkel", "De winkelketen zoekt een medewerker voor twee taken.",
         "Zet een inzamelactie op voor de voedselbank.",
         "Doe de vaste kassataken met duidelijke werktijden."),
    ],
}


def _flip(idx: int) -> bool:
    return idx % 2 == 1


def _interesse_items() -> list[Item]:
    rounds: list[list[Item]] = []
    for k in range(4):
        rnd: list[Item] = []
        for (first, second), rows in RIASEC_PAREN.items():
            sector, sit, o1, o2 = rows[k]
            d1, d2 = RIASEC[first], RIASEC[second]
            if _flip(k):
                o1, o2, d1, d2 = o2, o1, d2, d1
            rnd.append(
                Item("interesse", 1, "ab", sit, KOP_AB, o1, d1, o2, d2, sector)
            )
        rounds.append(rnd)
    return [it for rnd in rounds for it in rnd]


def _competentie_items() -> list[Item]:
    items: list[Item] = []
    for k in range(2):
        for comp in BASIS_COMPETENCES:
            sector, sit, hoog, laag = COMPETENTIE_ITEMS[comp][k]
            items.append(
                Item("competentie", 2, "ab", sit, KOP_AB, hoog, comp, laag, comp, sector)
            )
    return items


def _werkwaarden_items() -> list[Item]:
    items: list[Item] = []
    n = 0
    for k in range(4):
        for (first, second), rows in WERKWAARDE_PAREN.items():
            if k >= len(rows):
                continue
            sector, sit, o1, o2 = rows[k]
            d1, d2 = SCHWARTZ[first], SCHWARTZ[second]
            if _flip(n):
                o1, o2, d1, d2 = o2, o1, d2, d1
            n += 1
            items.append(Item("werkwaarden", 3, "ab", sit, KOP_AB, o1, d1, o2, d2, sector))
    return items


def load_basis_items() -> list[Item]:
    return _interesse_items() + _competentie_items() + _werkwaarden_items()


if __name__ == "__main__":
    from collections import Counter

    from bank_schema import count_coverage, validate_bank

    data = load_basis_items()
    validate_bank("handcrafted_basis", data, 4, 4, 3, BASIS_COMPETENCES)
    counts = Counter(it.blok for it in data)
    if dict(counts) != {"interesse": 12, "competentie": 18, "werkwaarden": 10}:
        raise SystemExit(f"handcrafted_basis: onverwachte blokken {dict(counts)}")
    cov = count_coverage(data)
    print("handcrafted_basis ok", len(data), dict(cov["meta"]))
    print("riasec", dict(sorted(cov["riasec"].items())))
    print("competentie", dict(sorted(cov["competentie"].items())))
    print("schwartz", dict(sorted(cov["schwartz"].items())))
