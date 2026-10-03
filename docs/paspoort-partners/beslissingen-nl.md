# DNA-paspoort als CV-bijlage + partners: beslissingen (akkoord Dennis 03-10-2026)

*Status: Dennis gaf op 03-10-2026 "akkoord" op alle 19 voorstellen, precies zoals voorgesteld. Mockups staan in `docs/mockups/paspoort-partners/`.*
*Bron: branch `acceptatie` (commit 7aacb6c8). Alle voorbeelddata is fictief.*

## Kort: wat er nu is
- De enige download bij het paspoort is de **Lobsy-CV** (`LobsyCvPdfService`, QuestPDF): visitekaartje in navy/teal, **alleen in het Nederlands**. Daarin staan naam, **geboortedatum + leeftijd**, e-mail, telefoon, WhatsApp-ja/nee, over mij, motivatie, uren + dagdeel-matrix, vervoer + max. reistijd, rijbewijs, opleiding, werkervaring en certificaten. Optioneel komt daar de bijlage "Wie ben ik?" bij (verhaal, sterke punten, balkjes met competenties en cultuur in %).
- **Niet** in de PDF, terwijl het wel in het datamodel zit: talen + Nederlands-niveau, beschikbaar-per-datum, gezochte functies, leerdoelen, de 4 tests met datum, paspoortnummer. Er is geen QR, geen verificatie, geen co-branding en geen andere taal.
- Voorbeeld-PDF: `huidig-lobsy-cv-voorbeeld.pdf` (+ PNG's).

## Mockups
a `a-recruiterpagina-p1.png` · b `b-pagina2-sectoren-profiel.png` · c `c-tweetalig-pl-nl.png` · d `d-verificatiepagina-en-delen.png` · e `e-start-partnercode-toestemming.png` · f `f-cobranded-uitzendbureau.png` · g `g-partneroverzicht.png` · h `h-prijzen-partners.png` · i `i-teams-dashboard-concept-later.png` (concept, later)
Labels in de mockups: **NIEUW** = veld bestaat nog niet, **AFGELEID** = berekend uit bestaande data.

Al door jou besloten en verwerkt:
- Een partner ziet alleen eigen kandidaten (eigen code + toestemming). Er is geen zoekfunctie in de pool.
- Een partner ziet alleen het paspoort, nooit testantwoorden of ruwe scores.
- Er komen geen scores, ranking of automatische filters (AI Act).
- Werkgevers (HR) zijn een tweede partnertype met hetzelfde mechanisme.

---

## A. Inhoud van het paspoort

**1. Welke velden zijn verplicht voordat het paspoort "compleet" en deelbaar is?**
Voorstel: verplicht zijn beschikbaar per, uren per week, dagdelen/diensten, talen + Nederlands-niveau, vervoer/rijbewijs, regio (op stadsniveau) en minimaal 1 afgeronde test. Optioneel zijn werkervaring, certificaten, werkvoorkeuren en "in mijn eigen woorden".

**2. Komen er nieuwe velden voor "werkvoorkeuren"?**
Het gaat om binnen/buiten, fysiek werk (tillen), tempo/productienorm, eigen auto en contractvoorkeur.
Voorstel: ja, als **positieve keuzes die de kandidaat zelf deelt**. Die staan los van de bestaande privé-"dislikes" (nachtdiensten, zwaar tillen, kou), want die zijn in de code expliciet "never employer-facing" en dat houden we zo. Huisvesting nemen we **niet** op in v1, omdat het iets kan zeggen over herkomst of afhankelijkheid van het bureau.

**3. Wat staat bewust níet op het paspoort?**
Voorstel: geen geboortedatum/leeftijd, foto, nationaliteit, BSN, gezondheid, werkvergunning of religie. Let op: de huidige Lobsy-CV toont geboortedatum + leeftijd. Ik stel voor dat het nieuwe paspoort dat niet doet (risico op leeftijdsdiscriminatie) en hooguit "18+" toont waar dat wettelijk nodig is.

**4. Hoe brengen we sectoren en sterke punten zonder dat het een score wordt?**
Voorstel:
- Op het paspoort en in het partnerportaal komen geen percentages, balken of "match"-labels.
- Lobsy stelt sectoren voor; de kandidaat **kiest en ordent ze zelf**, met bij elke sector "waarom het bij mij past".
- Sterke punten staan er als woorden ("zelfinzicht uit test").
- De kandidaat ziet in de app zijn eigen percentages wel.
- Het blok "Zo haal je het beste uit…" (AI-tekst) komt er alleen in als de kandidaat de tekst eerst goedkeurt. Anders laten we het blok weg.

**5. Tweetalig: welke talen en hoe gaan we om met vrije tekst?**
Voorstel: de taal van de kandidaat (nl/en/pl/ro/ar) bovenaan en NL eronder; EN is te kiezen in plaats van NL. Vrije tekst wordt automatisch vertaald, met het label "automatisch vertaald", en de kandidaat ziet de vertaling voordat hij deelt. Arabisch (RTL) komt in een tweede ronde.

## B. Verificatie, QR en delen

**6. Wat betekent "Lobsy-geverifieerd" precies?**
Voorstel: alle 4 tests zijn afgerond op het eigen account (datum per test komt uit `CompletedAtUtc`) en e-mail + telefoon zijn bevestigd. **Expliciet niet** gecontroleerd: identiteit, werkvergunning, diploma's en referenties. Let op: een verificatievlag voor e-mail/telefoon van de kandidaat heb ik in het User-model niet gevonden, die moet er dus bij. Alternatieve naam als "geverifieerd" te veel belooft: "Lobsy-compleet".

**7. Hoe lang is een link geldig en heeft de kijker een account nodig?**
Voorstel:
- Partnertoegang loopt zolang de toestemming geldt, met een herbevestiging elke 6 maanden.
- Losse deellinks zijn standaard 30 dagen geldig (de kandidaat kan kiezen uit 7, 30 of 90).
- Wie de QR scant zonder toegang ziet alleen "echt, maar privé" en kan toegang aanvragen.
- Bekijken via een deellink kan **zonder account**.
- Het partnerportaal vraagt wel een account met 2FA (dat hergebruikt de bestaande employer/Intermediary-rollen).

## C. Partnermodel (uitzendbureaus + werkgevers)

**8. Mag een kandidaat aan meerdere partners gekoppeld zijn?**
Voorstel: ja, met aparte toestemming per partner en maximaal 3 actieve koppelingen. Een partner ziet nooit met welke andere partners de kandidaat deelt. De eerste code bepaalt "binnengekomen via"; later kan de kandidaat een code toevoegen via "Code toevoegen".

**9. Wanneer vragen we toestemming en wat ziet een partner van kandidaten zonder toestemming?**
Voorstel: we vragen het direct na het aanmaken van het account. "Nee" blokkeert niets en de vakjes staan standaard uit. Kandidaten zonder toestemming ziet de partner **alleen als aantal**, en pas vanaf 5 (zelfde drempel als `SchoolAnonymity.MinGroupSize`). Dus nooit namen of status per persoon.

**10. Wat gebeurt er bij intrekken, vertrek of opzegging?**
Voorstel:
- Bij intrekken of accountverwijdering stopt de toegang direct en verdwijnt de kandidaat uit het overzicht. Hij telt dan alleen nog mee bij "ingetrokken".
- Eerder gedownloade PDF's vallen onder de eigen AVG-verantwoordelijkheid van de partner (vast te leggen in de partnervoorwaarden).
- Zegt de partner op, dan houden kandidaten hun paspoort, verdwijnt het logo uit nieuwe PDF's en vervalt de toestemming.

**11. Nemen we "geen score, ranking of automatische selectie" letterlijk op in de partnervoorwaarden en de toestemmingstekst?**
Voorstel: ja (zie mockups e, g en h).
- Het portaal sorteert alleen op datum en status.
- Er komt een statusfilter, maar geen filter op geschiktheid.
- "Later matching op eigen vacatures" (Pro) vullen we in als: kandidaten zien vacatures van hun eigen partner en kiezen zelf. Lobsy rangschikt dus geen kandidaten voor het bedrijf.
- Vóór Pro laten we de AI Act-positie juridisch toetsen.

**12. Hergebruiken we schoolcodes of bouwen we iets nieuws?**
In de code zag ik het volgende:
- Schoolcodes (`PupilCode`) zijn anonieme inlogcodes per leerling: 6 tekens, HMAC-lookup, status Niet gestart/Bezig/Afgerond, zonder account. Dat zijn geen gedeelde codes voor een organisatie.
- Wat er al dichter bij komt: `PartnerAffiliateProfile` (IM-/BM-trackingcodes voor Intermediary/EnterpriseManager, nu alleen voor het doorverwijzen van bedrijven) en de toekenning van kandidaten via ambassadeurcodes (`User.ReferredByAmbassadeurUserId`).

Voorstel: een nieuwe koppeltabel `PartnerCandidateLink` (kandidaat, bedrijf, code, toestemming met datum en versie, ingetrokken-op). Daarbij hergebruiken we het codeformaat en de HMAC-aanpak van schoolcodes, het toekenningspatroon van ambassadeurs en `Company.LogoUrl` voor de co-branding.

**13. De bestaande anonieme talentpool met token-unlock per kandidaat (`TalentContactRequest`, 1 token per contact): wat doen we daarmee?**
Die botst met "partners zien alleen eigen kandidaten" en met "nooit betalen per kandidaat". Voorstel: het partnerportaal krijgt **geen** toegang tot de talentpool. Bepaal apart of de talentpool voor gewone werkgevers blijft bestaan.

## D. Prijzen (alle bedragen voorbeeld/indicatief)

**14. Kloppen de pakketten?**
- Gratis: de kandidaat deelt zelf.
- Partner: €99–€249 per vestiging per maand.
- Pro: vanaf €500 per maand, op vestigingen en volume.

Voorstel: akkoord, met dezelfde pakketten voor uitzendbureaus en werkgevers. De volumestaffel loopt op "actieve gedeelde paspoorten" in brede banden (t/m 50, 150, 300), zodat het nooit leest als betalen per kandidaat. Is zelfs dat te dichtbij, dan alleen per vestiging. We factureren via een abonnement, niet via het bestaande tokensysteem.

**15. Wat bieden we de pilot?**
Voorstel: de eerste 5 Westlandse partners krijgen 3 maanden Partner gratis, in ruil voor 2 feedbackgesprekken en een quote die we mogen gebruiken. Daarna 50% korting tot juli 2027. Met een schriftelijke pilotafspraak + afspraak over gegevensdeling vooraf.

**16. Krijgen niet-partners (Gratis) ook iets?**
Voorstel: ja, bekijken via de link of QR van de kandidaat plus de echtheidscontrole. Dus geen code, geen logo en geen overzicht. Dat is de trechter richting Partner.

## E. Concept – later: "Lobsy voor teams" (niet bouwen nu)

**17. Akkoord met de spelregels?**
Minimaal 10 deelnemers per groep/afdeling, alleen totalen, geen filters die iemand herleidbaar maken (leeftijd, schaal, contract), vrijwillig, OR-instemming waar die nodig is, en de medewerker houdt zijn eigen paspoort. Voorstel: ja. Daarbij geen koppeling met functioneren, de P&C-cyclus of het personeelsdossier, en de teamscan-resultaten komen nooit op het individuele paspoort.

**18. Doelgroep en prijs?**
Voorstel: eerst als **upsell voor gemeenten die al Lobsy-klant zijn**, voor hun eigen medewerkers (loopbaangesprekken, interne mobiliteit, teamscan per afdeling). Licentie per medewerker per jaar, bijvoorbeeld €12–€20 (indicatief). Daarna pas grotere werkgevers.

**19. Mag een medewerker zijn eigen paspoort in een loopbaangesprek gebruiken?**
Voorstel: alleen als hij dat zelf deelt, via hetzelfde mechanisme als "Delen & toegang". De werkgever kan het nooit zelf opvragen.
