"""Hand-authored Loopbaan-APK Basis items (40); elke situatie is uniek en concreet."""
from __future__ import annotations

from bank_schema import KOP_AB, RIASEC, SCHWARTZ, Item

__all__ = ["load_basis_items"]

INTERESSE: list[tuple[str, str, str, str, str, str]] = [
    (
        "logistiek",
        "Bij het laden schuift een zware pallet scheef op de helling.",
        "R",
        "Zet de pallet met de heftruck recht.",
        "I",
        "Bekijk eerst het laadplan op papier.",
    ),
    (
        "zorg",
        "Tijdens de ronde vraagt een cliënt waarom zijn thee zo laat komt.",
        "S",
        "Ga even zitten en leg het rustig uit.",
        "A",
        "Maak een leuk kaartje op het dienblad.",
    ),
    (
        "horeca",
        "Het terras loopt vol terwijl de reserveringenlijst vol staat.",
        "E",
        "Stel een extra tafel buiten voor en start met serveren.",
        "C",
        "Werk de wachtlijst eerst netjes bij.",
    ),
    (
        "retail",
        "Een klant twijfelt tussen twee jassen in de spiegel.",
        "I",
        "Vraag waar hij warm of koud last van heeft.",
        "R",
        "Laat hem de dikke jas even aanraken.",
    ),
    (
        "kantoor",
        "Het team moet een poster maken voor de personeelsdag.",
        "A",
        "Schets met kleur een speelse opzet.",
        "S",
        "Vraag iedereen welk thema ze leuk vinden.",
    ),
    (
        "bouw",
        "De opdrachtgever wil de planning voor volgende week alvast.",
        "C",
        "Zet de werkzaamheden rij voor rij in de planning.",
        "E",
        "Bel meteen twee onderaannemers voor extra manschappen.",
    ),
    (
        "transport",
        "Onderweg knippert het waarschuwingslampje van de vrachtwagen.",
        "R",
        "Stop veilig en controleer het peil van de olie.",
        "I",
        "Zoek in de handleiding wat het lampje betekent.",
    ),
    (
        "productie",
        "Op de lijn blijven kleine krasjes op een metalen onderdeel zichtbaar.",
        "S",
        "Overleg met collega's over de acceptatiegrens.",
        "A",
        "Bedenk een nieuwe polish-stap aan de band.",
    ),
    (
        "onderwijs",
        "Voor de excursie moet de klas morgen buiten de deur.",
        "E",
        "Regel snel een extra bus bij de vervoerder.",
        "C",
        "Controleer de aanwezigheidslijst en groepsindeling.",
    ),
    (
        "logistiek",
        "In het gangpad ligt een open doos met kleine onderdelen.",
        "I",
        "Tel de stukjes en vergelijk met de pakbon.",
        "R",
        "Plak de doos dicht en zet hem op een kar.",
    ),
    (
        "zorg",
        "Een familielid belt boos omdat niemand terugbelt over medicatie.",
        "A",
        "Schrijf een korte samenvatting voor op de balie.",
        "S",
        "Bel terug en luister eerst naar hun zorgen.",
    ),
    (
        "horeca",
        "De keuken krijgt een nieuwe allergenenlijst voor het menu.",
        "C",
        "Zet alle gerechten opnieuw in het systeem.",
        "E",
        "Stel voor het wisselende weekmenu zelf samen te stellen.",
    ),
]

COMPETENTIE: list[tuple[str, str, str, str, str, str]] = [
    (
        "retail",
        "Aan de ingang staat een nieuwe stagiair die nog niemand kent.",
        "con",
        "Stel jezelf voor en wijs de kluisjes aan.",
        "kla",
        "Vraag welke klantenvraag hij als eerste wil doen.",
    ),
    (
        "kantoor",
        "In de vergaderzaal moet je lastig nieuws aan collega's vertellen.",
        "com",
        "Leg rustig uit wat er precies verandert.",
        "str",
        "Blijf kalm als er interrupties komen.",
    ),
    (
        "bouw",
        "Het inschietwerk op de verdieping moet voor donker klaar.",
        "res",
        "Rond vandaag dit stuk af voor de controle.",
        "ler",
        "Vraag de voorman welke methode hij adviseert.",
    ),
    (
        "transport",
        "Bij het depot staat een klant te wachten op een pakket.",
        "kla",
        "Zoek zijn zending en zet die voorop.",
        "sam",
        "Vraag een collega om de route te dekken.",
    ),
    (
        "productie",
        "De orderpicker loopt achter door storing op de band.",
        "str",
        "Houd rustig tempo als collega's gaan roepen.",
        "ler",
        "Oefen eerst even met het nieuwe scannertje.",
    ),
    (
        "onderwijs",
        "Tijdens de toets loopt een leerling steeds op en neer.",
        "str",
        "Stuur hem rustig terug zonder de klas te storen.",
        "sam",
        "Stap samen met de mentor het gesprek in.",
    ),
    (
        "logistiek",
        "Drie vrachten moeten tegelijk dezelfde laadklep op.",
        "sam",
        "Stem met beide chauffeurs de volgorde af.",
        "str",
        "Blijf kalm als de telefoon blijft gaan.",
    ),
    (
        "zorg",
        "De ochtendronde loopt uit door onverwachte hulpvragen.",
        "res",
        "Rond de visites af die nu op de lijst staan.",
        "con",
        "Sprek de wachtende cliënt bij de deur even aan.",
    ),
    (
        "horeca",
        "Een tafel klaagt dat hun bestelling nog steeds niet komt.",
        "kla",
        "Vraag door tot je snapt wat er misging.",
        "ler",
        "Noteer wat je leert voor de keukenbriefing.",
    ),
    (
        "retail",
        "De kassafile stroomt door een trage scanner.",
        "pla",
        "Zet een wachtlijst en rij-strook neer.",
        "sam",
        "Schakel een collega in bij de andere kassa.",
    ),
    (
        "kantoor",
        "Na een cursus moet je het nieuwe CRM-systeem gaan gebruiken.",
        "ler",
        "Vraag een korte demo en probeer meteen.",
        "com",
        "Leg teamgenoten uit hoe jij het doet.",
    ),
    (
        "bouw",
        "Het veiligheidsinspecteur komt onverwacht op de bouwplaats.",
        "kla",
        "Wijs hem het veilige pad voor bezoekers aan.",
        "res",
        "Pak eerst de open gevaren direct aan.",
    ),
    (
        "transport",
        "De tablet met ritgegevens toont een adres dat niet klopt.",
        "pla",
        "Schrijf het juiste adres en route op.",
        "cre",
        "Stel een tijdelijke navigatietruc voor.",
    ),
    (
        "productie",
        "Voor de audit moeten alle batches traceerbaar zijn.",
        "pla",
        "Leg etiketten en tijden per batch vast.",
        "res",
        "Zorg dat de serie vandaag af is.",
    ),
    (
        "onderwijs",
        "Een ouder wil weten hoe haar kind het op stage doet.",
        "con",
        "Begroet haar en nodig haar even binnen.",
        "com",
        "Leg bondig uit wat de stageleider zei.",
    ),
    (
        "logistiek",
        "In de scanner pakt de software een verkeerde SKU.",
        "pla",
        "Maak een korte lijst met correcte codes.",
        "cre",
        "Bedenk een sticker om verkeerde scans te voorkomen.",
    ),
    (
        "zorg",
        "Twee cliënten willen tegelijk hulp bij aankleden.",
        "cre",
        "Bedenk wie eerst met welke trick kan.",
        "con",
        "Spreek de ander vriendelijk toe bij de deur.",
    ),
    (
        "horeca",
        "Het nieuwe dessin op de kaart moet morgen in de vitrine.",
        "cre",
        "Maak een proefopstelling met restchocolade.",
        "com",
        "Licht kort uit wat er in de vitrine staat.",
    ),
]

WAARDEN: list[tuple[str, str, str, str, str, str]] = [
    (
        "retail",
        "Je baas vraagt of je zondag wilt invallen in de winkel.",
        "sta",
        "Kies liever voor vaste weekenden zoals afgesproken.",
        "aut",
        "Kies zelf of je die zondag wilt draaien.",
    ),
    (
        "kantoor",
        "Het team viert een grote deal met gebak op kantoor.",
        "con",
        "Wil liever samen lachen dan alleen scoren.",
        "ach",
        "Wil het resultaat op het bord zien staan.",
    ),
    (
        "bouw",
        "Er is onduidelijkheid over wie het eindcontroleformulier invult.",
        "sta",
        "Houd je aan de vaste stappen op papier.",
        "imp",
        "Wil dat de klant veilig naar huis gaat.",
    ),
    (
        "transport",
        "Collega's appen elkaar buiten werktijd over ritten.",
        "con",
        "Wil graag betrokken blijven bij het team.",
        "ach",
        "Wil vooral zien dat ritten op tijd zijn.",
    ),
    (
        "productie",
        "Je mag zelf bedenken hoe je de kwaliteit controleert.",
        "imp",
        "Wil dat goede producten de klant bereiken.",
        "aut",
        "Wil zelf je controlelijst kunnen kiezen.",
    ),
    (
        "onderwijs",
        "De school wil dat je vaste mentoruren houdt dit jaar.",
        "aut",
        "Plant je mentoruur zelf in de week.",
        "sta",
        "Houdt liever vaste spreekuren elke maand.",
    ),
    (
        "logistiek",
        "Het management deelt een prijs uit voor nul incidenten.",
        "ach",
        "Streeft naar de beloning met het team.",
        "con",
        "Wil vooral samen veilig werken op de vloer.",
    ),
    (
        "zorg",
        "Je krijgt ruimte om een cliëntproject te starten.",
        "imp",
        "Wil dat de cliënt echt baat heeft.",
        "sta",
        "Wil dat alles binnen de regels blijft.",
    ),
    (
        "horeca",
        "De chef laat je de lunchspecial zelf bedenken.",
        "aut",
        "Kiest zelf het gerecht van de dag.",
        "imp",
        "Kiest iets waar gasten blij van worden.",
    ),
    (
        "retail",
        "In de personeelswinkel hangt een wedstrijd voor meeste service.",
        "ach",
        "Wil bovenaan het leaderboard komen.",
        "con",
        "Wil vooral met het team meedoen.",
    ),
]

COMP = {
    "sam": "Samenwerken",
    "res": "Resultaatgerichtheid",
    "str": "Stressbestendigheid",
    "cre": "Creativiteit / vernieuwen",
    "con": "Contact maken",
    "com": "Communiceren",
    "pla": "Plannen & organiseren",
    "kla": "Klantgerichtheid",
    "ler": "Leren & ontwikkelen",
}

WAARDE = {
    "aut": SCHWARTZ["autonomy"],
    "con": SCHWARTZ["connection"],
    "ach": SCHWARTZ["achievement"],
    "sta": SCHWARTZ["stability"],
    "imp": SCHWARTZ["impact"],
}


def load_basis_items() -> list[Item]:
    items: list[Item] = []
    for sector, situatie, la, oa, lb, ob in INTERESSE:
        items.append(
            Item(
                blok="interesse",
                display_blok=1,
                soort="ab",
                situatie=situatie,
                kop=KOP_AB,
                optie_a=oa,
                dimensie_a=RIASEC[la],
                optie_b=ob,
                dimensie_b=RIASEC[lb],
                sector=sector,
            )
        )
    for sector, situatie, ka, oa, kb, ob in COMPETENTIE:
        items.append(
            Item(
                blok="competentie",
                display_blok=2,
                soort="ab",
                situatie=situatie,
                kop=KOP_AB,
                optie_a=oa,
                dimensie_a=COMP[ka],
                optie_b=ob,
                dimensie_b=COMP[kb],
                sector=sector,
            )
        )
    for sector, situatie, ka, oa, kb, ob in WAARDEN:
        items.append(
            Item(
                blok="werkwaarden",
                display_blok=3,
                soort="ab",
                situatie=situatie,
                kop=KOP_AB,
                optie_a=oa,
                dimensie_a=WAARDE[ka],
                optie_b=ob,
                dimensie_b=WAARDE[kb],
                sector=sector,
            )
        )
    return items


if __name__ == "__main__":
    from bank_schema import BASIS_COMPETENCES, count_coverage, validate_bank

    data = load_basis_items()
    validate_bank("handcrafted_basis", data, 4, 4, 4, BASIS_COMPETENCES)
    cov = count_coverage(data)
    print("handcrafted_basis ok", len(data), dict(cov["meta"]))