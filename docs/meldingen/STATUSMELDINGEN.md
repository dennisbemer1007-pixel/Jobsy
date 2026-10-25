# Statusmeldingen sollicitatie en Na de match

Specificatie voor meldingen bij elke **statuswijziging** in de sollicitatieflow en Na de match. Implementatie volgt in kleine PR’s tegen `acceptatie` (geen Blazor voor nieuwe werkgeverschermen; meldingen zelf zijn API + bestaande bel/PWA/mail).

## Bronnen

- Bouwspec §8 (PWA-meldingen): [bouwspec.md](../na-de-match-package/bouwspec.md) — bell in de app, push indien subscription, anders mail; voorkeuren per soort; stille uren 21:00–8:00 (Europe/Amsterdam); max. 3 meldingen per dag (herinneringen gesprek tellen niet mee en gaan altijd door).
- Bestaande opslag: `UserNotification` (Category = soort), optioneel `WebPushSubscription`; dispatcher + `NotificationPreference` volgen bouwspec (nog uit te rollen waar afwezig).
- Deeplink: waarde in `UserNotification.DeepLink` (pad op het web, zonder token). Klik opent dat scherm; mark read via bestaande `/notifications`-flow.

## Tekstregels

- Nederlands en Engels; titel kort, body in **woorden** (geen enum-codes, geen matchscores, geen percentages).
- Geen gedachtestreepjes (geen `—` / `–`); gebruik punt of komma.
- Placeholders (ingevuld server-side):
  - `{functie}` — vacaturetitel
  - `{bedrijf}` — bedrijfsnaam
  - `{kandidaat}` — voornaam + achternaam of weergavenaam (alleen waar PII al mag)

## Kanalen (kolom Kanalen)

| Afkorting | Betekenis |
|-----------|-----------|
| **Bel** | Altijd: rij in `UserNotification` (app-bel). |
| **Push** | Web Push als voorkeur + subscription + dispatcher toestaat (buiten stille uren / daglimiet, tenzij type is vrijgesteld). |
| **Mail** | Transactionele mail als voorkeur aan staat, of als push niet kan (bouwspec: mail blijft altijd beschikbaar als terugval). |

Voorkeur-soort (`NotificationPreference.Soort`) — mapping voor implementatie:

| Soort | Meldingen |
|-------|-----------|
| `Sollicitatie` | versturen (werkgever), cv bekeken, wederzijds ja, ingetrokken |
| `Gesprek` | momenten voorgesteld, moment gekozen, ander moment voorgesteld, gesprek gepland, verzet, afgezegd |
| `Bericht` | nieuw bericht |
| `Uitkomst` | aanbod, nog een gesprek, nu niet |
| `Herinnering` | (aparte flow; niet in deze tabel) |

## Deeplink-sjablonen

`{id}` = `ApplicationId` (guid, lowercase hex met streepjes).

| Rol | Scherm | Pad |
|-----|--------|-----|
| Kandidaat | Sollicitaties, detail | `/candidate/applications?tab=sollicitaties&sel={id}` |
| Kandidaat | Na de match (standaard) | `/candidate/applications/{id}/after-match` |
| Kandidaat | Moment kiezen | `/candidate/applications/{id}/after-match?panel=pick-slot` |
| Kandidaat | Berichten | `/candidate/applications/{id}/after-match?panel=messages` |
| Kandidaat | Verzet / afzeggen | `/candidate/applications/{id}/after-match?panel=reschedule` of `?panel=cancel` |
| Kandidaat | Uitkomst | `/candidate/applications/{id}/after-match?panel=outcome` |
| Werkgever | Sollicitatie | `/werkgever/sollicitaties/{id}` |
| Werkgever | Na de match | `/werkgever/sollicitaties/{id}/after-match` |
| Werkgever | Momenten voorstellen | `/werkgever/sollicitaties/{id}/after-match?panel=propose` |
| Werkgever | Berichten | `/werkgever/sollicitaties/{id}/after-match?panel=messages` |
| Werkgever | Verzet / afzeggen | `/werkgever/sollicitaties/{id}/after-match?panel=reschedule` of `?panel=cancel` |
| Werkgever | Uitkomst registreren | `/werkgever/sollicitaties/{id}/after-match` (panel leeg of outcome in UI) |

Middleware herschrijft `/after-match` naar SSR-suffixen; deeplink blijft het publieke pad hierboven.

## Tabel statusovergangen

Technische trigger = waar implementatie moet haken (nu of later). **Ontvanger** = account dat de melding krijgt.

| ID | Trigger (code) | Ontvanger | Category | Kanalen | NL titel | NL body | EN title | EN body | Doelroute |
|----|------------------|-----------|----------|---------|----------|---------|----------|---------|-----------|
| SM-01 | Sollicitatie geverifieerd / aangemaakt (`ApplicationStatusEventKind.Created`, status `Pending`) | Werkgever (actieve company-users) | `EmployerNewApplication` | Bel, Push, Mail | Nieuwe sollicitatie | Er is een nieuwe sollicitatie voor {functie} bij {bedrijf}. | New application | There is a new application for {functie} at {bedrijf}. | `/werkgever/sollicitaties/{id}` |
| SM-02 | Zelfde als SM-01 | Kandidaat | `ApplicationConfirmation` | Bel, Mail | Sollicitatie verstuurd | Je sollicitatie voor {functie} bij {bedrijf} is verstuurd. | Application sent | Your application for {functie} at {bedrijf} has been sent. | `/candidate/applications?tab=sollicitaties&sel={id}` |
| SM-03 | Eerste werkgever-view (`ApplicationStatusEventKind.EmployerViewed`, idempotent) | Kandidaat | `ApplicationCvViewed` | Bel, Push, Mail | CV bekeken | {bedrijf} heeft je sollicitatie voor {functie} bekeken. | CV viewed | {bedrijf} has viewed your application for {functie}. | `/candidate/applications?tab=sollicitaties&sel={id}` |
| SM-04 | Werkgever accepteert (`ApplicationStatus` → `Accepted`, `TryReactAsync`) | Kandidaat | `ApplicationMutualYes` | Bel, Push, Mail | Wederzijds ja | {bedrijf} wil verder met je sollicitatie voor {functie}. Jullie kunnen nu een gesprek plannen. | Mutual yes | {bedrijf} wants to proceed with your application for {functie}. You can plan an interview next. | `/candidate/applications/{id}/after-match` |
| SM-05 | Werkgever stelt 2–3 slots voor (`AfterMatchService.ProposeEmployerSlotsAsync`, `InterviewStatus.Proposing`) | Kandidaat | `InterviewSlotsProposed` | Bel, Push, Mail | Momenten voorgesteld | {bedrijf} stelde momenten voor een gesprek voor {functie}. Kies een moment dat past. | Times proposed | {bedrijf} proposed times for an interview for {functie}. Pick a time that works for you. | `/candidate/applications/{id}/after-match?panel=pick-slot` |
| SM-06 | Kandidaat kiest slot (`SelectSlotAsync`, kandidaat) | Werkgever | `InterviewSlotChosen` | Bel, Push, Mail | Moment gekozen | {kandidaat} koos een moment voor het gesprek over {functie}. | Time chosen | {kandidaat} chose a time for the interview about {functie}. | `/werkgever/sollicitaties/{id}/after-match` |
| SM-07 | Werkgever kiest slot (`SelectSlotAsync`, werkgever) | Kandidaat | `InterviewSlotChosen` | Bel, Push, Mail | Moment gekozen | {bedrijf} bevestigde het gekozen moment voor {functie}. | Time chosen | {bedrijf} confirmed the chosen time for {functie}. | `/candidate/applications/{id}/after-match` |
| SM-08 | Kandidaat stelt ander moment voor (`ProposeCandidateSlotAsync`, `InterviewStatus.RescheduleRequested`) | Werkgever | `InterviewAlternativeProposed` | Bel, Push, Mail | Ander moment voorgesteld | {kandidaat} stelde een ander moment voor het gesprek over {functie}. Bekijk het voorstel. | Another time proposed | {kandidaat} proposed another time for the interview about {functie}. Review the proposal. | `/werkgever/sollicitaties/{id}/after-match?panel=propose` |
| SM-09 | Slot gekozen / gesprek vast (`SelectSlotAsync`, `InterviewStatus.Scheduled`) | Kandidaat | `InterviewScheduled` | Bel, Push, Mail | Gesprek gepland | Je gesprek voor {functie} bij {bedrijf} staat gepland. | Interview scheduled | Your interview for {functie} at {bedrijf} is scheduled. | `/candidate/applications/{id}/after-match` |
| SM-10 | Zelfde als SM-09 | Werkgever | `InterviewScheduled` | Bel, Push, Mail | Gesprek gepland | Het gesprek met {kandidaat} over {functie} staat gepland. | Interview scheduled | The interview with {kandidaat} for {functie} is scheduled. | `/werkgever/sollicitaties/{id}/after-match` |
| SM-11 | Verzet aangevraagd (`RescheduleAsync`, `InterviewStatus.RescheduleRequested`, zonder nieuw slot) | Andere partij | `InterviewRescheduleRequested` | Bel, Push, Mail | Gesprek verzet | De andere partij wil het gesprek voor {functie} verzetten. Bekijk de reden en plan opnieuw. | Interview moved | The other party wants to move the interview for {functie}. See the reason and plan again. | Ontvanger kandidaat: `/candidate/applications/{id}/after-match?panel=reschedule`; werkgever: `/werkgever/sollicitaties/{id}/after-match?panel=reschedule` |
| SM-12 | Gesprek afgezegd (`CancelInterviewAsync`, `InterviewStatus.Cancelled`) | Andere partij | `InterviewCancelled` | Bel, Push, Mail | Gesprek afgezegd | Het gesprek voor {functie} is afgezegd. Bekijk de reden in het dossier. | Interview cancelled | The interview for {functie} was cancelled. See the reason in the thread. | Ontvanger kandidaat: `/candidate/applications/{id}/after-match?panel=cancel`; werkgever: `/werkgever/sollicitaties/{id}/after-match?panel=cancel` |
| SM-13 | Nieuw bericht (`SendMessageAsync`, ontvanger ≠ afzender) | Andere partij | `ApplicationNewMessage` | Bel, Push, Mail | Nieuw bericht | Er is een nieuw bericht over je sollicitatie voor {functie}. | New message | There is a new message about your application for {functie}. | Afzender werkgever → kandidaat: `/candidate/applications/{id}/after-match?panel=messages`; andersom → `/werkgever/sollicitaties/{id}/after-match?panel=messages` |
| SM-14 | Uitkomst aanbod (`RecordOutcomeAsync`, `AfterMatchOutcomeKind.Offer`, status → `Hired`) | Kandidaat | `ApplicationOutcomeOffer` | Bel, Push, Mail | Aanbod | {bedrijf} heeft een aanbod gedaan voor {functie}. Lees de toelichting in je dossier. | Job offer | {bedrijf} made an offer for {functie}. Read the details in your thread. | `/candidate/applications/{id}/after-match?panel=outcome` |
| SM-15 | Uitkomst nog een gesprek (`RecordOutcomeAsync`, `NextRound`, status → `EmployerContacting`) | Kandidaat | `ApplicationOutcomeNextRound` | Bel, Push, Mail | Nog een gesprek | {bedrijf} wil nog een gesprek voor {functie}. Bekijk wat er verder gebeurt. | Another interview | {bedrijf} wants another interview for {functie}. See what happens next. | `/candidate/applications/{id}/after-match` |
| SM-16 | Uitkomst nu niet (`RecordOutcomeAsync`, `NotNow`, status → `Rejected`) | Kandidaat | `ApplicationOutcomeNotNow` | Bel, Push, Mail | Nu niet | {bedrijf} heeft de uitkomst voor {functie} gedeeld. Lees de toelichting in je dossier. | Not now | {bedrijf} shared the outcome for {functie}. Read the explanation in your thread. | `/candidate/applications/{id}/after-match?panel=outcome` |
| SM-17 | Intrekken (`ApplicationStatus` → `Withdrawn`, kandidaat) | Werkgever (company-users) | `CandidateWithdrawn` | Bel, Mail | Sollicitatie ingetrokken | {kandidaat} heeft de sollicitatie voor {functie} ingetrokken. | Application withdrawn | {kandidaat} withdrew the application for {functie}. | `/werkgever/sollicitaties/{id}` |

### Notities bij triggers

- **SM-04 (wederzijds ja):** Kandidaat had al ja gegeven door te solliciteren; werkgever zet status op `Accepted`. Geen aparte kandidaat-bevestiging in code; tekst benoemt wederzijdse voortgang in mensentaal.
- **SM-06 / SM-07 / SM-09 / SM-10:** Bij `SelectSlotAsync` worden SM-06 of SM-07 (wie koos) én SM-09/SM-10 (gepland) getriggerd; dedupe in de dispatcher op Category + `RelatedEntityId` + titel binnen 24 uur (bestaand patroon in `UserNotificationService`).
- **SM-08 vs SM-11:** Kandidaat-voorstel met concreet slot = SM-08. Alleen verzet-verzoek met reden zonder nieuw voorstel = SM-11. Werkgever die opnieuw slots plaatst na verzet = opnieuw SM-05.
- **SM-03:** Alleen eerste `EmployerViewed`; geen herhaling bij vervolgbezoeken.
- **SM-01 / SM-17:** Meerdere ontvangers (company-users); zelfde copy, eigen `UserNotification` per user.
- **EmployerContacting** (contact-knop zonder Na de match): geen aparte rij in Dennis’ lijst; valt buiten deze catalogus tenzij product later uitbreidt.

## Voorgestelde implementatie-PR’s (stap 2)

| PR | Rijen | Hoofd-wijzigingen |
|----|-------|-------------------|
| 1 | SM-03, SM-04 (copy/deeplink align) | `TryRecordEmployerViewedAsync`, `React` notificaties |
| 2 | SM-05–SM-12 | `AfterMatchService` + push/mail hooks |
| 3 | SM-13 | `SendMessageAsync` |
| 4 | SM-14–SM-16 | `RecordOutcomeAsync` |
| 5 | SM-01, SM-02, SM-17 | Deeplink `/werkgever/sollicitaties/{id}` i.p.v. alleen `/branch/applicants` waar passend |
| 6 | Dispatcher + `NotificationPreference` | Bouwspec §8 afdwingen op alle rijen |

## Acceptatie

- Elke rij produceert bij trigger een bel-item met NL/EN copy conform tabel (UI taal volgt gebruikersvoorkeur).
- Push/mail respecteren voorkeur en stille uren, behalve types die bouwspec vrijstelt.
- Deeplink opent het scherm uit kolom Doelroute zonder 404 voor demo-IDs op acceptatie.
- Geen scores of gedachtestreepjes in titel/body.
