# Statusmeldingen sollicitatie en Na de match

Specificatie voor meldingen bij elke **statuswijziging** in de sollicitatieflow en Na de match. Implementatie volgt in kleine PR’s tegen `acceptatie` (geen Blazor voor nieuwe werkgeverschermen; meldingen zelf zijn API + bestaande bel/PWA/mail).

## Bronnen

- Bouwspec §8 (PWA-meldingen): [bouwspec.md](../na-de-match-package/bouwspec.md) — bell in de app, push indien subscription, anders mail; voorkeuren per soort; stille uren 21:00–8:00 (Europe/Amsterdam); max. 3 meldingen per dag (herinneringen gesprek tellen niet mee en gaan altijd door).
- Bestaande opslag: `UserNotification` (Category = soort), optioneel `WebPushSubscription`; dispatcher + `NotificationPreference` volgen bouwspec (nog uit te rollen waar afwezig).
- Deeplink: waarde in `UserNotification.DeepLink` (pad op het web, zonder token). Klik opent dat scherm; mark read via bestaande `/notifications`-flow.

## Werkgever én kandidaat (symmetrie)

- Bij **elke** statuswijziging in het sollicitatieproces krijgt de **tegenpartij** een melding (bel, en push/mail volgens voorkeur).
- De **actor** (wie de wijziging veroorzaakt) krijgt een **eigen bevestiging in de bel**, zodat ook werkgevers in Lobsy zien dat hun actie is vastgelegd. Kandidaat idem waar hieronder aangegeven.
- **Eigen bevestiging:** minimaal **Bel** (altijd). Push/mail voor eigen bevestiging volgen dezelfde voorkeur-soort als de tegenpartij-melding, tenzij de dispatcher later een lichtere standaard kiest (bel-only blijft dan leidend).
- Werkgever-ontvangers: actieve company-users (zelfde set als `EmployerNewApplication`). Bij eigen bevestiging: de user die de actie uitvoerde (niet elke collega), behalve waar anders vermeld.

Kolommen in de tabel:

| Kolom | Betekenis |
|-------|-----------|
| **Gebeurtenis** | Productterm (Dennis-lijst) |
| **Trigger** | Code-hook |
| **Actor** | Wie de wijziging doet |
| **Melding** | `Tegenpartij` of `Eigen bevestiging` |
| **Ontvanger** | Rol + scope |
| Overige kolommen | Category, kanalen, copy NL/EN, deeplink |

## Tekstregels

- Nederlands en Engels; titel kort, body in **woorden** (geen enum-codes, geen matchscores, geen percentages).
- Geen gedachtestreepjes (geen `—` / `–`); gebruik punt of komma.
- Placeholders: `{functie}`, `{bedrijf}`, `{kandidaat}` (PII alleen waar al toegestaan).

## Kanalen

| Afkorting | Betekenis |
|-----------|-----------|
| **Bel** | Altijd: `UserNotification`. |
| **Push** | Web Push indien subscription + voorkeur + dispatcher. |
| **Mail** | Transactionele mail als voorkeur of terugval. |

Voorkeur-soort (`NotificationPreference.Soort`):

| Soort | Meldingen |
|-------|-----------|
| `Sollicitatie` | versturen, cv bekeken, wederzijds ja, ingetrokken |
| `Gesprek` | momenten, gekozen moment, ander moment, gepland, verzet, afgezegd |
| `Bericht` | nieuw bericht |
| `Uitkomst` | aanbod, nog een gesprek, nu niet |
| `Herinnering` | aparte flow |

## Deeplink-sjablonen

`{id}` = `ApplicationId`.

| Rol | Scherm | Pad |
|-----|--------|-----|
| Kandidaat | Sollicitaties, detail | `/candidate/applications?tab=sollicitaties&sel={id}` |
| Kandidaat | Na de match | `/candidate/applications/{id}/after-match` |
| Kandidaat | Moment kiezen | `/candidate/applications/{id}/after-match?panel=pick-slot` |
| Kandidaat | Berichten | `/candidate/applications/{id}/after-match?panel=messages` |
| Kandidaat | Verzet / afzeggen | `?panel=reschedule` / `?panel=cancel` |
| Kandidaat | Uitkomst | `?panel=outcome` |
| Werkgever | Sollicitatie | `/werkgever/sollicitaties/{id}` |
| Werkgever | Na de match | `/werkgever/sollicitaties/{id}/after-match` |
| Werkgever | Momenten | `?panel=propose` |
| Werkgever | Berichten | `?panel=messages` |
| Werkgever | Verzet / afzeggen | `?panel=reschedule` / `?panel=cancel` |

## Tabel meldingen (alle rijen)

| ID | Gebeurtenis | Trigger (code) | Actor | Melding | Ontvanger | Category | Kanalen | NL titel | NL body | EN title | EN body | Doelroute |
|----|-------------|------------------|-------|---------|-----------|----------|---------|----------|---------|----------|---------|-----------|
| SM-01a | Versturen | `Created` / verified apply | Kandidaat | Tegenpartij | Werkgever (company-users) | `EmployerNewApplication` | Bel, Push, Mail | Nieuwe sollicitatie | Er is een nieuwe sollicitatie voor {functie} bij {bedrijf}. | New application | There is a new application for {functie} at {bedrijf}. | `/werkgever/sollicitaties/{id}` |
| SM-01b | Versturen | zelfde | Kandidaat | Eigen bevestiging | Kandidaat | `ApplicationConfirmation` | Bel, Mail | Sollicitatie verstuurd | Je sollicitatie voor {functie} bij {bedrijf} is verstuurd. | Application sent | Your application for {functie} at {bedrijf} has been sent. | `/candidate/applications?tab=sollicitaties&sel={id}` |
| SM-02a | CV bekeken | eerste `EmployerViewed` | Werkgever | Tegenpartij | Kandidaat | `ApplicationCvViewed` | Bel, Push, Mail | CV bekeken | {bedrijf} heeft je sollicitatie voor {functie} bekeken. | CV viewed | {bedrijf} has viewed your application for {functie}. | `/candidate/applications?tab=sollicitaties&sel={id}` |
| SM-02b | CV bekeken | zelfde | Werkgever | Eigen bevestiging | Uitvoerende werkgever | `ApplicationCvViewedSelf` | Bel | Bevestiging | Je hebt de sollicitatie van {kandidaat} voor {functie} bekeken. | Confirmation | You viewed {kandidaat}'s application for {functie}. | `/werkgever/sollicitaties/{id}` |
| SM-03a | Wederzijds ja | `Accepted` (`TryReactAsync`) | Werkgever | Tegenpartij | Kandidaat | `ApplicationMutualYes` | Bel, Push, Mail | Wederzijds ja | {bedrijf} wil verder met je sollicitatie voor {functie}. Jullie kunnen nu een gesprek plannen. | Mutual yes | {bedrijf} wants to proceed with your application for {functie}. You can plan an interview next. | `/candidate/applications/{id}/after-match` |
| SM-03b | Wederzijds ja | zelfde | Werkgever | Eigen bevestiging | Uitvoerende werkgever | `ApplicationMutualYesSelf` | Bel | Bevestiging | Je hebt {kandidaat} geaccepteerd voor {functie}. | Confirmation | You accepted {kandidaat} for {functie}. | `/werkgever/sollicitaties/{id}` |
| SM-04a | Momenten voorgesteld | `ProposeEmployerSlotsAsync` | Werkgever | Tegenpartij | Kandidaat | `InterviewSlotsProposed` | Bel, Push, Mail | Momenten voorgesteld | {bedrijf} stelde momenten voor een gesprek voor {functie}. Kies een moment dat past. | Times proposed | {bedrijf} proposed times for an interview for {functie}. Pick a time that works for you. | `/candidate/applications/{id}/after-match?panel=pick-slot` |
| SM-04b | Momenten voorgesteld | zelfde | Werkgever | Eigen bevestiging | Uitvoerende werkgever | `InterviewSlotsProposedSelf` | Bel | Bevestiging | Je stelde momenten voor het gesprek met {kandidaat} over {functie}. | Confirmation | You proposed times for the interview with {kandidaat} for {functie}. | `/werkgever/sollicitaties/{id}/after-match?panel=propose` |
| SM-05a | Moment gekozen | `SelectSlotAsync`, actor kandidaat | Kandidaat | Tegenpartij | Werkgever (company-users) | `InterviewSlotChosen` | Bel, Push, Mail | Moment gekozen | {kandidaat} koos een moment voor het gesprek over {functie}. | Time chosen | {kandidaat} chose a time for the interview about {functie}. | `/werkgever/sollicitaties/{id}/after-match` |
| SM-05b | Moment gekozen | zelfde | Kandidaat | Eigen bevestiging | Kandidaat | `InterviewSlotChosenSelf` | Bel | Bevestiging | Je koos een moment voor het gesprek over {functie} bij {bedrijf}. | Confirmation | You chose a time for your interview for {functie} at {bedrijf}. | `/candidate/applications/{id}/after-match` |
| SM-06a | Moment gekozen | `SelectSlotAsync`, actor werkgever | Werkgever | Tegenpartij | Kandidaat | `InterviewSlotChosen` | Bel, Push, Mail | Moment gekozen | {bedrijf} bevestigde het gekozen moment voor {functie}. | Time chosen | {bedrijf} confirmed the chosen time for {functie}. | `/candidate/applications/{id}/after-match` |
| SM-06b | Moment gekozen | zelfde | Werkgever | Eigen bevestiging | Uitvoerende werkgever | `InterviewSlotChosenSelf` | Bel | Bevestiging | Je bevestigde het moment voor het gesprek met {kandidaat} over {functie}. | Confirmation | You confirmed the time for the interview with {kandidaat} for {functie}. | `/werkgever/sollicitaties/{id}/after-match` |
| SM-07a | Ander moment voorgesteld | `ProposeCandidateSlotAsync` | Kandidaat | Tegenpartij | Werkgever (company-users) | `InterviewAlternativeProposed` | Bel, Push, Mail | Ander moment voorgesteld | {kandidaat} stelde een ander moment voor het gesprek over {functie}. Bekijk het voorstel. | Another time proposed | {kandidaat} proposed another time for the interview about {functie}. Review the proposal. | `/werkgever/sollicitaties/{id}/after-match?panel=propose` |
| SM-07b | Ander moment voorgesteld | zelfde | Kandidaat | Eigen bevestiging | Kandidaat | `InterviewAlternativeProposedSelf` | Bel | Bevestiging | Je stelde een ander moment voor het gesprek over {functie} bij {bedrijf}. | Confirmation | You proposed another time for your interview for {functie} at {bedrijf}. | `/candidate/applications/{id}/after-match?panel=pick-slot` |
| SM-08a | Gesprek gepland | `SelectSlotAsync` → `Scheduled` | Systeem (na actor) | Tegenpartij | Kandidaat | `InterviewScheduled` | Bel, Push, Mail | Gesprek gepland | Je gesprek voor {functie} bij {bedrijf} staat gepland. | Interview scheduled | Your interview for {functie} at {bedrijf} is scheduled. | `/candidate/applications/{id}/after-match` |
| SM-08b | Gesprek gepland | zelfde | Systeem | Tegenpartij | Werkgever (company-users) | `InterviewScheduled` | Bel, Push, Mail | Gesprek gepland | Het gesprek met {kandidaat} over {functie} staat gepland. | Interview scheduled | The interview with {kandidaat} for {functie} is scheduled. | `/werkgever/sollicitaties/{id}/after-match` |
| SM-09a | Verzet | `RescheduleAsync` (zonder nieuw slot) | Kandidaat of werkgever | Tegenpartij | Andere partij | `InterviewRescheduleRequested` | Bel, Push, Mail | Gesprek verzet | De andere partij wil het gesprek voor {functie} verzetten. Bekijk de reden en plan opnieuw. | Interview moved | The other party wants to move the interview for {functie}. See the reason and plan again. | Kandidaat: `?panel=reschedule`; werkgever: `?panel=reschedule` |
| SM-09b | Verzet | zelfde | Kandidaat of werkgever | Eigen bevestiging | Actor | `InterviewRescheduleRequestedSelf` | Bel | Bevestiging | Je vroeg om het gesprek voor {functie} te verzetten. | Confirmation | You asked to move the interview for {functie}. | Actor deeplink (zelfde panel reschedule) |
| SM-10a | Afgezegd | `CancelInterviewAsync` | Kandidaat of werkgever | Tegenpartij | Andere partij | `InterviewCancelled` | Bel, Push, Mail | Gesprek afgezegd | Het gesprek voor {functie} is afgezegd. Bekijk de reden in het dossier. | Interview cancelled | The interview for {functie} was cancelled. See the reason in the thread. | Kandidaat: `?panel=cancel`; werkgever: `?panel=cancel` |
| SM-10b | Afgezegd | zelfde | Kandidaat of werkgever | Eigen bevestiging | Actor | `InterviewCancelledSelf` | Bel | Bevestiging | Je hebt het gesprek voor {functie} afgezegd. | Confirmation | You cancelled the interview for {functie}. | Actor deeplink (zelfde panel cancel) |
| SM-11a | Nieuw bericht | `SendMessageAsync` | Kandidaat of werkgever | Tegenpartij | Andere partij | `ApplicationNewMessage` | Bel, Push, Mail | Nieuw bericht | Er is een nieuw bericht over je sollicitatie voor {functie}. | New message | There is a new message about your application for {functie}. | `?panel=messages` (ontvangerrol) |
| SM-11b | Nieuw bericht | zelfde | Kandidaat of werkgever | Eigen bevestiging | Afzender | `ApplicationNewMessageSelf` | Bel | Bevestiging | Je bericht over {functie} is verstuurd. | Confirmation | Your message about {functie} was sent. | Afzender `?panel=messages` |
| SM-12a | Uitkomst aanbod | `RecordOutcomeAsync`, `Offer` | Werkgever | Tegenpartij | Kandidaat | `ApplicationOutcomeOffer` | Bel, Push, Mail | Aanbod | {bedrijf} heeft een aanbod gedaan voor {functie}. Lees de toelichting in je dossier. | Job offer | {bedrijf} made an offer for {functie}. Read the details in your thread. | `/candidate/applications/{id}/after-match?panel=outcome` |
| SM-12b | Uitkomst aanbod | zelfde | Werkgever | Eigen bevestiging | Uitvoerende werkgever | `ApplicationOutcomeOfferSelf` | Bel | Bevestiging | Je deelde een aanbod met {kandidaat} voor {functie}. | Confirmation | You shared an offer with {kandidaat} for {functie}. | `/werkgever/sollicitaties/{id}/after-match` |
| SM-13a | Uitkomst nog een gesprek | `NextRound` | Werkgever | Tegenpartij | Kandidaat | `ApplicationOutcomeNextRound` | Bel, Push, Mail | Nog een gesprek | {bedrijf} wil nog een gesprek voor {functie}. Bekijk wat er verder gebeurt. | Another interview | {bedrijf} wants another interview for {functie}. See what happens next. | `/candidate/applications/{id}/after-match` |
| SM-13b | Uitkomst nog een gesprek | zelfde | Werkgever | Eigen bevestiging | Uitvoerende werkgever | `ApplicationOutcomeNextRoundSelf` | Bel | Bevestiging | Je gaf aan dat er nog een gesprek volgt met {kandidaat} voor {functie}. | Confirmation | You indicated another interview follows with {kandidaat} for {functie}. | `/werkgever/sollicitaties/{id}/after-match` |
| SM-14a | Uitkomst nu niet | `NotNow` | Werkgever | Tegenpartij | Kandidaat | `ApplicationOutcomeNotNow` | Bel, Push, Mail | Nu niet | {bedrijf} heeft de uitkomst voor {functie} gedeeld. Lees de toelichting in je dossier. | Not now | {bedrijf} shared the outcome for {functie}. Read the explanation in your thread. | `/candidate/applications/{id}/after-match?panel=outcome` |
| SM-14b | Uitkomst nu niet | zelfde | Werkgever | Eigen bevestiging | Uitvoerende werkgever | `ApplicationOutcomeNotNowSelf` | Bel | Bevestiging | Je deelde de uitkomst met {kandidaat} voor {functie}. | Confirmation | You shared the outcome with {kandidaat} for {functie}. | `/werkgever/sollicitaties/{id}/after-match` |
| SM-15a | Ingetrokken | `Withdrawn` | Kandidaat | Tegenpartij | Werkgever (company-users) | `CandidateWithdrawn` | Bel, Mail | Sollicitatie ingetrokken | {kandidaat} heeft de sollicitatie voor {functie} ingetrokken. | Application withdrawn | {kandidaat} withdrew the application for {functie}. | `/werkgever/sollicitaties/{id}` |
| SM-15b | Ingetrokken | zelfde | Kandidaat | Eigen bevestiging | Kandidaat | `ApplicationWithdrawnSelf` | Bel | Bevestiging | Je hebt je sollicitatie voor {functie} bij {bedrijf} ingetrokken. | Confirmation | You withdrew your application for {functie} at {bedrijf}. | `/candidate/applications?tab=sollicitaties&sel={id}` |

### Notities

- **Wederzijds ja:** kandidaat gaf al ja door te solliciteren; werkgever zet `Accepted`.
- **SM-05/06 + SM-08:** na `SelectSlotAsync` kunnen moment-gekozen én gesprek-gepland rijen na elkaar vuren; dedupe op Category + `RelatedEntityId` + titel (24 uur).
- **SM-07 vs SM-09:** concreet slot-voorstel kandidaat = SM-07; alleen verzet-verzoek = SM-09. Werkgever opnieuw slots = SM-04.
- **SM-02:** alleen eerste `EmployerViewed`.
- **Legacy IDs** SM-01…SM-17 uit eerdere versie mappen op SM-01a…SM-15b zoals hierboven.

## Voorgestelde implementatie-PR’s (stap 2)

| PR | Rijen | Hoofd-wijzigingen |
|----|-------|-------------------|
| 1 | SM-02a/b, SM-03a/b | CV bekeken + wederzijds ja (+ werkgever bel-bevestiging) |
| 2 | SM-04–SM-10 | `AfterMatchService`, werkgever + kandidaat + self rows |
| 3 | SM-11 | Berichten |
| 4 | SM-12–SM-14 | Uitkomst |
| 5 | SM-01, SM-15 | Versturen / intrekken + deeplinks werkgever |
| 6 | Dispatcher + `NotificationPreference` | Bouwspec §8 |

## Acceptatie

- Elke gebeurtenis levert minimaal één tegenpartij-melding; actor krijgt eigen bevestiging in de bel (werkgever expliciet bij eigen acties).
- Push/mail volgen voorkeur; eigen bevestiging minimaal bel.
- Deeplink opent het scherm uit de tabel op acceptatie.
- Geen scores of gedachtestreepjes in titel/body.
