# Nieuwe kandidaatpagina's (goedgekeurd 23 oktober 2026, 07:31)

Mappen: `mijn-lobsy/`, `sollicitaties/`, `instellingen/`, `profiel/`, `wat-drijft-jou/`, `ondernemerscheck/`, `adviseur/`. Per map `desktop/` (1440) en `mobile/` (390), viewport en volledige pagina. Namen en bedrijven zijn nepdata.

## Afspraken
- Bouw op dezelfde manier als de onboarding: gewone JS-knoppen, same-origin fetch, opslaan per actie, volledige navigatie, zichtbare foutmelding bij een fout.
- Alle knoppen moeten werken. Laadtijd is erg belangrijk.
- Accountmenu precies 5 items: Profiel, Instellingen, Hoe werkt Lobsy, Feedback, Uitloggen. Juridische links alleen in de footer.
- Geen scores of percentages op schermen. Enige uitzondering: de UWV-pdf.

## Sollicitaties (erg belangrijk voor Dennis)
- Tabs: Sollicitaties, Mijn activiteiten, Bewaard.
- Knop "Sollicitatie toevoegen": vacaturelink plakken, uitlezen met de bestaande import (`/candidate/external/add`, `CandidateExternalVacancyService`), velden controleren en opslaan. Komt in de lijst met label "Buiten Lobsy" en telt vanzelf mee in Mijn activiteiten. Let op: externe vacatures komen nu niet in Sollicitaties (`ApplicationId` wordt nooit gevuld) en de feature flag `CandidateExternalVacancies` staat standaard uit.
- Mijn activiteiten: spec in `docs/na-de-match-package/bouwspec.md` sectie 2 (periode 4 weken, teller, lijst per week, zelf toevoegen, vaste tekst "Lobsy geeft niets door aan het UWV"). `JobSearchActivity` bestaat nog niet in de code.
- UWV-pdf: kolommen datum, soort, werkgever, functie, hoe en met wie, resultaat, "Match met vacature" (percentage, alleen voor Lobsy-vacatures, anders "niet bekend") en "Kansberoep (Haaglanden)" in woorden uit de bestaande kansberoepen-dataset ("Geen kansberoep" als het beroep niet op de lijst staat, "niet bekend" zonder beroep). Uitleg van beide kolommen onder de tabel.
