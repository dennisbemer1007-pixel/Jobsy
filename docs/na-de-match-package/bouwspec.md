# Lobsy nieuwe functies: bouwspec

Goedgekeurd door Dennis op 18 oktober 2026. Hoort bij de mockups in `/workspace/mockups/nieuw/out/` (per functie een map met desktop 1440 en mobiel 390, plus `overzicht.png`; totaal in `out/overzicht.png`; flow van Na de match in `out/na-de-match/flow.png`).

## Wat voor alle functies geldt

- **Elke functie heeft een eigen feature flag.** Die staat standaard uit, in acceptatie aan en in productie uit totdat Dennis hem aanzet. Voeg hem toe aan `PlatformFeature` (Jobsy.Core/Features) en aan de `FeatureFlagSnapshot`, en bescherm pagina's met `RequiresFeature`. Staat een flag uit, dan zie je geen spoor van de functie: geen link, geen menu-item en geen lege plek.
- **Stijl A:** Fraunces voor koppen en Nunito Sans voor tekst. Hoekradius 6, 12 of 20. Geen schaduw, behalve bij popups. De mobiele hero volgt de standaard (zie /workspace/ref737-390.png).
- **Werkgever- en beheerschermen** blijven zakelijk en hebben geen tekeningen.
- **Teksten:**
  - Nederlands en Engels.
  - Geen gedachtestreepjes in kandidaatteksten.
  - Nooit scores, percentages of sterren in teksten voor kandidaat of werkgever. Voortgang staat altijd in woorden.
  - De woorden die Dennis heeft uitgesloten (zie de vaste regels) komen nergens voor.
- **Minder klikken:** extra informatie staat in de pagina of in het paneel rechts, en niet op een aparte pagina of in een popup.
- **Geen automatische herlaadacties.** Nieuwe berichten en meldingen komen alleen binnen via push of wanneer de gebruiker zelf navigeert.
- **Testen:** ingelogd testen alleen lokaal en met nepdata.
- **Bestaande onderdelen gebruiken waar ze er al zijn** (gezien in de repo-scan op c07d8f7):
  - `UserNotification` en `WebPushSubscription` (meldingen en push)
  - `TalentContactRequest` met `TokenChargeOnAccept` (uitnodigen met token bij ja)
  - `CandidateKandidatenkaartSettings` en `CandidateProactiveMatchingSettings` (vindbaarheid en frequentie)
  - `MinimumWageRate` (minimumloon)
  - `ContentReport` (meldingen en bezwaar)
  - `CandidateReminderPreference`, `Application` en `ApplicationStatusHistory`

  Dubbel bouwen naast deze onderdelen gebeurt alleen als het echt niet anders kan.

## Flags

| Functie | Flag (`PlatformFeature`) | Hangt af van |
|---|---|---|
| 1. Vacature-alert | `VacancyAlerts` | `CandidateNotifications` voor push (zonder push werkt het met alleen mail) |
| 2. Mijn activiteiten | `ActivityLog` | `AfterMatch` voor gesprekken (zonder AfterMatch alleen sollicitaties en workshops) |
| 3. Na de match | `AfterMatch` | geen |
| 4. Begeleider meekijken | `CoachAccess` | `ActivityLog` voor het onderdeel Activiteiten (optioneel) |
| 5. Salaris en reactiesnelheid | `SalaryAndResponseTime` | geen |
| 6. Netwerk en referrals | `Referrals` | geen; deelt de toestemmingslogica met `ReferenceVerification` |
| 7. Werkgever zoekt | `EmployerTalentSearch` | uitbreiding van `Kandidatenkaart` en `ProactiveMatching` |
| 8. App met meldingen (PWA) | `CandidateNotifications` | geen |
| 9. Kandidaatervaringen | `CandidateExperiences` | `AfterMatch` (een uitkomst is de trigger) |

---

## 1. Vacature-alert

**Schermen:** `vacature-alert/01-banenkaart-alert`, `02-alerts-overzicht`, `03-mail-en-melding`.

**Besluiten**
- Iemand heeft maximaal 3 alerts. Bij 3 alerts kan er geen nieuwe bij. Je ziet dan "Je hebt 3 van de 3 alerts" en de tip om een bestaande aan te passen of te verwijderen. Een alert die uit staat, telt ook mee.
- Er zijn twee frequenties: **Eén keer per week** (maandagochtend, maximaal 3 banen) en **Twee keer per maand** (de 1e en de 15e, maximaal 5 banen). "Meteen" bestaat niet.
- De kandidaat kiest of het via mail, als melding op de telefoon of allebei komt. Push werkt alleen als `CandidateNotifications` aan staat; anders is het alleen mail.
- Het instellen gebeurt in het paneel rechts op de banenkaart, zonder aparte pagina. Beroepen worden voorgesteld vanuit het paspoort.
- Bij elke baan staat in woorden waarom die past, zonder score. Werkgevers zien niet dat iemand een alert heeft. Elke mail heeft onderaan een link om in één klik te stoppen.

**Datamodel**
- `VacancyAlert`: Id, CandidateUserId, Naam, Locatie (lat/lng en label), Vervoer (fiets/OV/auto/lopen), MaxReistijdMinuten, Beroepen (lijst met beroepscodes), Frequentie (`Weekly` of `TwiceMonthly`), ViaMail, ViaPush, IsActief, LaatstVerstuurdOpUtc, AangemaaktOpUtc. Unieke regel: maximaal 3 per kandidaat, gecontroleerd in de service en met een database-check.
- `VacancyAlertDelivery`: Id, AlertId, VerstuurdOpUtc, Kanaal, VacatureIds. Dit voorkomt dat dezelfde vacature twee keer wordt gestuurd.
- Een achtergrondtaak draait op maandag 7:00 en op de 1e en 15e om 7:00. Er is een uitschrijftoken per alert.

**Let op:** `CandidateProactiveMatchingSettings.Frequency` heeft nu `Daily`. Gebruik die frequentie niet voor alerts, anders kan "dagelijks" via een omweg terugkomen.

## 2. Mijn activiteiten (UWV)

**Schermen:** `mijn-activiteiten/01-mijn-activiteiten`, `02-pdf-overzicht`, plus de pdf `overzicht-sollicitatieactiviteiten.pdf`.

**Besluiten**
- De teller loopt per **periode van 4 weken**, met als standaard 4 activiteiten. De kandidaat kan dat aantal aanpassen aan haar eigen afspraak, maar de periode blijft 4 weken. De lijst eronder staat per week.
- **Lobsy geeft niets door aan het UWV.** Die tekst staat vast op het scherm. De kandidaat zet het overzicht zelf in Mijn UWV of in de Werkmap. Er komt geen koppeling met Mijn UWV.
- Sollicitaties, gesprekken (uit Na de match) en workshops met certificaat worden vanzelf bijgehouden. Wat de kandidaat buiten Lobsy doet, voegt ze toe in het paneel rechts, eventueel met een bijlage.
- Vacatures bekijken telt niet mee.
- De pdf bevat de velden die het UWV vraagt: datum, soort activiteit, werkgever, functie, contactwijze en resultaat.

**Datamodel**
- `JobSearchActivity`: Id, CandidateUserId, Datum, Soort (enum), Werkgever, Functie, Contactwijze, Resultaat, Bron (`Auto` of `Manual`), BronEntityType en BronEntityId (bijvoorbeeld een Application, Interview of Workshop), BijlageId.
- `ActivityAgreement`: CandidateUserId, AantalPerPeriode (standaard 4), PeriodeStart. De periode is altijd 28 dagen.
- Automatische activiteiten worden afgeleid via events (sollicitatie verstuurd, gesprek gehad, certificaat behaald) en niet met de hand gedupliceerd.

## 3. Na de match

**Schermen (13):** `na-de-match/01` t/m `13`, flow in `flow.png`. Bovenaan elk scherm staat dezelfde tijdlijn: Ja, Gesprek plannen, Gesprek, Uitkomst.

**Besluiten**
- **Plannen:** de werkgever stelt 2 of 3 momenten voor, of deelt zijn agenda. De kandidaat kiest een moment, of stelt zelf een ander moment voor met een reden.
- **Agenda:** allebei krijgen een bevestigingsmail met knoppen voor Outlook, Google en Apple (.ics). In het agenda-item staat een link om te verzetten.
- **Herinneringen:** de dag ervoor om 18:00 (mail en melding) en 2 uur van tevoren (melding met reistijd). Een week van tevoren is optioneel. De kandidaat stelt dit zelf in.
- **Verzetten of afzeggen** kan altijd. Wie dat doet, geeft verplicht een korte reden, en de ander ziet die reden.
- **Berichten:** elke sollicitatie heeft één berichtendraad. Er staat "Gelezen door ..." in woorden, er is geen "typt..." en bestanden worden op virussen gecontroleerd. De werkgever heeft snelle antwoorden.
- **Contactgegevens:**
  - Een **telefoonnummer** wordt pas na het gesprek gedeeld.
  - Een **IBAN** pas na een aanbod.
  - Typt iemand eerder een nummer of IBAN, dan verschijnt een vriendelijke waarschuwing:
    - Een kandidaat mag zijn eigen nummer na de waarschuwing toch sturen; het is zijn keuze.
    - Een werkgever kan vóór een aanbod niet om een IBAN vragen; dat bericht wordt tegengehouden.
  - Een nep-vraag om geld of een ID kan de kandidaat melden via `ContentReport`.
- **Uitkomst:** Aanbod, Nog een gesprek of Nu niet, altijd met een verplichte reden die de kandidaat ziet. De reden wordt op toon gecontroleerd voordat hij verstuurd wordt. Bij "Nu niet" ziet de kandidaat banen die erop lijken en een volgende stap. Het gesprek telt mee in Mijn activiteiten.
- **Tekening:** de kandidaatschermen tonen nu twee vrouwen aan tafel in gesprek (gespiegelde versie van de meekijk-tekening). Voor de echte bouw komt er een eigen tekening van een kandidaat en werkgever in gesprek.
- Op scherm 06 staat nu een kaart "Wat er hierna gebeurt" op de plek die eerst leeg was.

**Datamodel**
- `Interview`: Id, ApplicationId, Status (`Proposing`, `Scheduled`, `RescheduleRequested`, `Cancelled`, `Done`), GekozenSlotId, Vorm (`OnSite`, `Online` of `Phone`), Locatie of Link, MetWie, Notitie.
- `InterviewSlot`: Id, InterviewId, Start, Eind, VoorgesteldDoor (`Employer` of `Candidate`), Reden.
- `InterviewChange`: Id, InterviewId, Soort (`Reschedule` of `Cancel`), DoorRol, Reden, OpUtc.
- `InterviewReminder`: Id, InterviewId, UserId, Moment (`DayBefore18`, `TwoHoursBefore` of `WeekBefore`), Kanaal, VerstuurdOpUtc. De voorkeuren komen uit `CandidateReminderPreference`.
- `ApplicationMessage`: Id, ApplicationId, AfzenderUserId, Tekst, BijlageId, GelezenOpUtc, GeblokkeerdReden.
- `ApplicationOutcome`: ApplicationId, Uitkomst (`Offer`, `NextRound` of `NotNow`), Reden (verplicht), ToonCheckOk, OpUtc. Leg ook vast in `ApplicationStatusHistory`.
- `ContactShareState` (afgeleid, geen tabel): telefoon vrij na Interview `Done`, IBAN vrij na `Offer`.
- Voor de detectie van telefoonnummers en IBANs komt een gedeelde regex-service (NL-mobiel, internationaal, IBAN met mod-97) die op client en server dezelfde uitkomst geeft.
- Een ICS-generator met UID per Interview, zodat een verzet gesprek het bestaande agenda-item bijwerkt.

## 4. Begeleider meekijken

**Schermen:** `begeleider-meekijken/01-kandidaat-uitnodigen`, `02-kandidaat-stoppen`, `03-begeleider-overzicht`.

**Besluiten**
- **Organisatie:** een begeleider valt altijd onder een organisatie, zoals Werkplaats Den Haag. Een losse begeleider krijgt een eigen kleine organisatie.
- **Betaling:** voor kandidaten altijd gratis. Organisaties zijn gratis tijdens de pilots; een abonnement per organisatie volgt later.
- **Wat een begeleider mag zien:** de kandidaat zet per onderdeel aan wat de begeleider ziet: Paspoort, Sollicitaties en Activiteiten.
  - **Testantwoorden zijn nooit zichtbaar.**
  - **Berichten met werkgevers zijn nooit zichtbaar.** Beide staan als vaste regel "Nooit" op het scherm, zonder schakelaar.
- De begeleider kan alleen kijken. Hij kan niets versturen of aanpassen.
- Stoppen kan met één klik. De begeleider krijgt dan alleen een neutraal bericht.
- De kandidaat ziet wanneer de begeleider keek ("Wanneer Mirjam keek"); die plek op scherm 01 was eerst leeg.
- Het portaal voor begeleiders is zakelijk. Voortgang staat in woorden, er gaat elke maandag een korte samenvatting per mail en de begeleider kan notities voor zichzelf maken.

**Datamodel**
- `CoachOrganisation`: Id, Naam, Soort, IsPilot, AbonnementStatus.
- `CoachUser`: UserId, OrganisationId, Rol. De rol `Begeleider` staat apart van werkgever en beheer.
- `CoachAccessGrant`: Id, CandidateUserId, CoachUserId, Status (`Invited`, `Active` of `Stopped`), ZietPaspoort, ZietSollicitaties, ZietActiviteiten, UitgenodigdOpUtc, GestoptOpUtc, StopReden (optioneel en alleen voor Lobsy). Er zijn **geen** velden voor berichten of testantwoorden; die zijn per definitie uitgesloten, ook in de query-laag.
- `CoachViewLog`: GrantId, Onderdeel, OpUtc. De kandidaat kan dit inzien.
- `CoachNote`: CoachUserId, CandidateUserId, Tekst. Alleen de begeleider zelf ziet de notitie.
- Autorisatie gaat via een aparte policy. Een begeleider leest alleen via een read-only projectie, nooit via de gewone kandidaat-services.

## 5. Salaris en reactiesnelheid

**Schermen:** `salaris-reactiesnelheid/01-vacature-salaris-reactie`, `02-bedrijfspagina`, `03-werkgever-reactiesnelheid`.

**Besluiten**
- **Salaris:** de bandbreedte is **verplicht**. Zonder minimum en maximum kun je geen vacature plaatsen. Kandidaten zien "Uit de vacature", in euro's per maand en per uur.
- **Minimumloon:** ligt het bedrag onder het minimumloon (`MinimumWageRate`, op basis van leeftijd en uren), dan **kan de vacature niet geplaatst worden**.
- **Cao:** komt later. Ligt het bedrag dan onder de cao, dan krijgt de werkgever alleen een waarschuwing en blijft de vacature staan. Er komt nu geen cao-dataset.
- **Reactiesnelheid:** gebaseerd op de laatste 3 maanden en pas zichtbaar na minstens 5 reacties. Kandidaten zien het in woorden: "Reageert meestal binnen 3 dagen" of "binnen een week".
- **Uitleg:** "Wat betekent dit?" staat als vaste tekst in de pagina, en niet als popup.
- **Werkgever:** in zijn dashboard ziet de werkgever zijn eigen reactiesnelheid, de sollicitaties die op hem wachten en een tip. Een kort "Nu niet" telt ook als reactie.
- **Geen foto:** heeft de vacature geen foto, dan toont Lobsy een tekening op crème. Er is geen grijs vlak.

**Datamodel**
- Op `Vacancy` komen erbij: SalarisMin, SalarisMax, SalarisEenheid (`PerMaand` of `PerUur`) en UrenBasis. `HourlyWage` blijft voor backwards compatibility en wordt afgeleid van het minimum. Een migratie vult bestaande vacatures zonder bandbreedte met min = max = HourlyWage en vraagt de werkgever bij de volgende bewerking om het aan te vullen.
- Validatie in de domain-service: min ≤ max, min ≥ `MinimumWageRate`. Er komt een haakje voor de latere cao-check, die alleen een waarschuwing geeft.
- `EmployerResponseStats` wordt berekend: CompanyId, MediaanReactieDagen, AantalReacties, PeriodeStart. Ververs het elke nacht. Toon het alleen als AantalReacties ≥ 5. Een reactie is de eerste statuswijziging na binnenkomst (`ApplicationStatusHistory`).

## 6. Netwerk en referrals

**Schermen:** `netwerk-referrals/01-kandidaat-voordragen`, `02-toestemming-bekende`, `03-werkgever-aanbevelingen`.

**Besluiten**
- Een kandidaat draagt een bekende voor via het paneel rechts bij een vacature. De bekende beslist zelf op een eigen pagina. Pas na een ja ziet de werkgever het paspoort.
- **Bewaartermijn:** komt er na **14 dagen** geen antwoord, dan worden naam en e-mail gewist. Dat is dezelfde termijn als bij referenties.
- **Geen beloning in Lobsy.** Het scherm zegt dat ook: een werkgever die iets wil geven, regelt dat zelf.
- **Vaste link voor collega's:** elke werkgever krijgt een vaste link (`lobsy.nl/aanbevelen/<bedrijf>`) met een QR-code. Wie via die link wordt aangeraden, krijgt eerst zelf de vraag of het mag.
- De kandidaat ziet alleen of iemand ja of nee zei, en verder niets. Er is geen sociale feed.

**Datamodel**
- `Referral`: Id, VacancyId, DoorUserId (kandidaat) of DoorLinkId (collega-link), NaamBekende, EmailBekende, Toelichting, Status (`Pending`, `Accepted`, `Declined` of `Expired`), ToestemmingOpUtc, VerlooptOpUtc (+14 dagen), CandidateUserId (na een ja).
- `CompanyReferralLink`: CompanyId, Slug, IsActief, AangemaaktOpUtc.
- Een nachttaak wist de persoonsgegevens van referrals die `Expired` of `Declined` zijn. De toestemmingsflow en tokens worden gedeeld met `ReferenceConfirmationToken` (zelfde patroon en dezelfde misbruikmelding).

## 7. Werkgever zoekt in kandidaten (met vindbaarheid)

**Schermen:** `werkgever-zoekt/01-werkgever-zoeken`, `02-werkgever-uitnodigingen`, `03-kandidaat-uitnodiging`, en nieuw `04-vindbaarheid-onboarding`.

**Besluiten**
- **Zoeken:** de werkgever zoekt in paspoorten zonder naam, foto of leeftijd. De filters staan in woorden, zoals "Zorgvuldig" of "Tot 20 min fietsen".
- **Token:** een uitnodiging kost **1 token, en alleen als de kandidaat ja zegt**.
- **Limiet:** een werkgever heeft **maximaal 10 openstaande uitnodigingen** tegelijk. Op het scherm staat bijvoorbeeld "Nog 7 van 10 uitnodigingen open" en "3 van de 10 uitnodigingen staan open". Een uitnodiging zonder antwoord sluit na 7 dagen, en dan komt er weer een plek vrij.
- **Antwoord van de kandidaat:** de kandidaat ziet waarom de werkgever aan haar dacht en kiest Ja of Nee. Bij Nee mag ze een reden geven, maar dat hoeft niet. Pas na een ja ziet de werkgever haar naam, en dan start Na de match.
- **Vindbaarheid:** dit wordt **één keer duidelijk gevraagd aan het eind van de onboarding** ("Mogen werkgevers je paspoort vinden?"), op scherm 04 in de stijl van "Je Kompas staat klaar". Daarna wijzigt de kandidaat het altijd met één schakelaar. Een kandidaat die die vraag nog nooit kreeg, is niet vindbaar.

**Datamodel**
- Hergebruik `TalentContactRequest` (zet TokenChargeOnAccept = true, Message en VacancyId) en de bestaande statusflow. Daarbij komen:
  - `DeclineReason` (optioneel, uit een vaste lijst)
  - `ExpiresAtUtc` (+7 dagen; de bestaande enum noemt nu 48 uur, dus dat moet gelijkgetrokken worden)
  - een servicecheck van maximaal 10 met status `Pending` per bedrijf
- Vindbaarheid komt in `CandidateKandidatenkaartSettings` (of `CandidateProactiveMatchingSettings`), met de velden `Discoverable` (bool), `DiscoverableAskedAtUtc`, `DiscoverableConsentVersion`.
- Een zoekindex op de anonieme paspoortprojectie (competenties die onderschreven zijn volgens Besluit 21, reistijd, uren, beschikbaarheid). Leeftijd, naam en foto staan er niet in.

**Let op:** `Kandidatenkaart` en `ProactiveMatching` bestaan al. Bouw hierop voort, zodat er geen tweede uitnodigingssysteem naast komt.

## 8. App met meldingen (PWA)

**Schermen:** `app-meldingen/01-telefoon-en-meldingen`, `02-meldingen-instellen`.

**Besluiten**
- **Eerst een PWA:** Lobsy wordt een webapp die je op je beginscherm zet, met manifest, service worker en Web Push. De App Store en Google Play komen later. Op het scherm staat een korte uitleg "Zet Lobsy op je beginscherm" voor iPhone (Safari, Delen, Zet op beginscherm) en Android (Installeren).
- **Mail als terugval:** de mail blijft altijd aan. Kan er geen push worden verstuurd, bijvoorbeeld op een iPhone zonder Lobsy op het beginscherm, dan gaat het bericht per mail.
- **App-icoon:** het icoon uit logopakket E (`/workspace/mockups/logo/final-E/png/lobsy-apple-touch-icon-180.png`, `lobsy-android-192.png`, `lobsy-android-512.png`, `lobsy-android-maskable-512.png`).
- **Soorten melding:** een nieuwe baan in je alert, een uitnodiging van een werkgever, een herinnering voor een gesprek, een nieuw bericht, je UWV-activiteiten, en tips (standaard uit). Per soort kiest de kandidaat App en/of Mail.
- **Rust:** tussen 21:00 en 8:00 geen meldingen, en hooguit 3 per dag. Herinneringen voor een gesprek tellen niet mee voor dat maximum en gaan altijd voor.

**Datamodel**
- Hergebruik `WebPushSubscription` en `UserNotification` (Category is de soort).
- Nieuw: `NotificationPreference` met UserId, Soort, ViaPush, ViaMail, NietStorenVan (standaard 21:00), NietStorenTot (standaard 8:00) en MaxPerDag (standaard 3).
- Een dispatcher kiest per melding: push als dat mag en er een subscription is, anders mail. Hij houdt het dagelijkse maximum en de stille uren bij in de tijdzone Europe/Amsterdam.
- PWA: `manifest.webmanifest` (naam, iconen, theme #0f2d5c, background #f7f1e8) en een service worker voor push en offline-shell. **Geen** automatische herlaadacties of update-banners die de pagina verversen.

## 9. Kandidaatervaringen

**Schermen:** `kandidaatervaring/01-bedrijfspagina-ervaringen`, `02-kandidaat-ervaring-delen`, `03-werkgever-reageren`, `04-beheer-controle`.

**Besluiten**
- Alleen wie echt via Lobsy solliciteerde, kan een ervaring delen. Dat gebeurt na een uitkomst, in woorden: thema-chips en een optionele tekst. Er zijn geen sterren of cijfers. De naam van de kandidaat staat er niet bij, alleen de functie en de maand.
- **AI leest elke ervaring vooraf.** AI markeert namen van medewerkers, scheldwoorden en beledigingen, persoonsgegevens en tekst die niet over de sollicitatie gaat. Elke markering heeft een reden, die in het beheerscherm staat als **"AI ziet: ..."**. **Een mens beslist**: AI plaatst en weigert nooit zelf.
- **Weigeren:** namen van medewerkers, scheldwoorden of beledigingen, persoonsgegevens, en tekst die niet over de sollicitatie gaat. Bij discriminatie verwijzen we naar een meldpunt in plaats van de ervaring te plaatsen.
- **Negatief mag**, zolang het over het sollicitatieproces gaat en zakelijk blijft.
- **Reactie werkgever:** de werkgever kan openbaar reageren.
- **Bezwaar werkgever:** de werkgever kan **bezwaar maken met een reden**. Lobsy kijkt de ervaring dan opnieuw na tegen dezelfde regels. Tijdens het nakijken blijft de ervaring staan, tenzij hij duidelijk een regel overtreedt. De werkgever kan een ervaring niet zelf verwijderen.

**Datamodel**
- `CandidateExperience`: Id, ApplicationId (verplicht en uniek), CompanyId, Themas (lijst), Tekst, Status (`Pending`, `NeedsEdit`, `Published`, `Rejected` of `UnderReview`), GeplaatstOpUtc, BesluitDoorUserId, BesluitReden.
- `ExperienceAiFlag`: ExperienceId, Soort (`EmployeeName`, `Insult`, `PersonalData` of `OffTopic`), Uitleg, Fragment, ModelVersie. Dit is alleen advies.
- `ExperienceEmployerReply`: ExperienceId, Tekst, OpUtc (openbaar).
- `ExperienceObjection`: hergebruik `ContentReport` met TargetType = `CandidateExperience`, Reason en Details. Status `Open` zet de ervaring op `UnderReview` maar laat hem zichtbaar.
- De AI-check draait asynchroon na het insturen. Gaat die check mis, dan komt de ervaring zonder markeringen in de wachtrij; er wordt dan niets automatisch geplaatst.

---

## Voorgestelde bouwvolgorde

Ik volg grotendeels jouw voorstel, met twee verschuivingen vanwege afhankelijkheden: Meldingen/PWA komt eerder, en Mijn activiteiten komt na Na de match.

1. **Na de match** (`AfterMatch`). Dit is de kern waar de meeste andere functies op leunen: gesprekken voor Mijn activiteiten, de uitkomst als trigger voor Kandidaatervaringen, en herinneringen. Het heeft ook de meeste waarde voor Dennis. De herinneringen werken eerst per mail; push komt erbij zodra stap 3 er is.
2. **Werkgever zoekt + vindbaarheid** (`EmployerTalentSearch`). Dit bouwt op het bestaande `TalentContactRequest` en de Kandidatenkaart, en een ja leidt direct naar Na de match. De vindbaarheidsvraag moet zo vroeg mogelijk in de onboarding staan, zodat er vindbare paspoorten zijn tegen de tijd dat werkgevers gaan zoeken.
3. **Meldingen/PWA** (`CandidateNotifications`), naar voren gehaald. Vacature-alert, uitnodigingen en herinneringen hebben allemaal de dispatcher en de voorkeuren nodig. `WebPushSubscription` en `UserNotification` bestaan al, dus dit is een relatief kleine stap. Het voorkomt dat de alert eigen meldinglogica krijgt die later weer weg moet.
4. **Vacature-alert** (`VacancyAlerts`). Gebruikt de dispatcher uit stap 3, is daarna vooral een achtergrondtaak met een scherm, en is goed los te testen.
5. **Salaris en reactiesnelheid** (`SalaryAndResponseTime`). Heeft een migratie op `Vacancy` en een validatie in het werkgeversformulier nodig. Het is onafhankelijk, maar raakt het plaatsen van vacatures; daarom komt het na de flows hierboven, zodat die niet tegelijk wijzigen. De reactiesnelheid wordt beter met de uitkomsten uit stap 1.
6. **Mijn activiteiten** (`ActivityLog`). Wordt pas compleet met de gesprekken uit stap 1. Daarna is het vooral een projectie van events, een paneel en de pdf.
7. **Begeleider meekijken** (`CoachAccess`). Nieuwe rol, nieuwe autorisatie en een nieuw portaal. Het toont pas waarde als Sollicitaties en Activiteiten (stap 1 en 6) gevuld zijn. Een pilotorganisatie moet klaarstaan.
8. **Netwerk en referrals** (`Referrals`). Kan het toestemmingspatroon van `ReferenceVerification` (#724) overnemen. Bouw dit dus pas als #724 gemerged is.
9. **Kandidaatervaringen** (`CandidateExperiences`). Heeft echte uitkomsten (stap 1) nodig, plus een moderatiewachtrij en een AI-check. Als laatste, omdat er pas iets te delen is als er sollicitaties met een uitkomst zijn, en omdat de regels over wat we weigeren eerst juridisch nagelopen moeten worden.

**Wat parallel kan:** stap 5 (Salaris) kan naast stap 2 en 3 door een andere agent worden gebouwd, omdat het andere bestanden raakt. Stap 8 kan naast stap 7 lopen zodra #724 erin zit.

## Nog open en buiten deze ronde

- Een eigen tekening "kandidaat en werkgever in gesprek" voor Na de match. Nu staat er een gespiegelde versie van de meekijk-tekening, waardoor Begeleider meekijken en Na de match op elkaar lijken.
- Een cao-dataset voor salaris (bewust later; nu alleen een haakje voor de waarschuwing).
- De prijs per organisatie voor Begeleider meekijken na de pilots.
- De lijst met meldpunten voor discriminatie bij Kandidaatervaringen, en een juridische check van de weigerregels.
- De termijn van `TalentContactStatus.Pending` in de code (48 uur) gelijktrekken met de 7 dagen in de mockup.
